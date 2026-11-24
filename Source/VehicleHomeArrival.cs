using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    /// A player's land vehicle that comes home with a caravan: the crew gets out by VF's own command, then haulers carry the cargo
    /// to storage by the vanilla "unload carrier" work. VF only blocks the property that requests this work for vehicles (a parked
    /// vehicle may be a store), so it is requested here, for one returning vehicle, until its cargo is empty.
    /// Aircraft, other maps and other factions are never touched.
    public sealed class VehicleHomeArrival : MapComponent
    {
        private const int Interval = 60;
        private List<Pawn> returning = new List<Pawn>();   // vehicles being unloaded
        private List<Pawn> ordered = new List<Pawn>();     // unload even under threat: the player gave the order

        public VehicleHomeArrival(Map map) : base(map) { }

        public bool Unloading(Pawn vehicle) { return returning.Contains(vehicle); }

        public void Begin(Pawn vehicle, bool byPlayer)
        {
            if (!VehicleHome.Eligible(vehicle, map)) return;
            if (!returning.Contains(vehicle)) returning.Add(vehicle);
            if (byPlayer && !ordered.Contains(vehicle)) ordered.Add(vehicle);
            Process(vehicle);
        }

        private void End(Pawn vehicle)
        {
            returning.Remove(vehicle);
            ordered.Remove(vehicle);
            if (vehicle != null && vehicle.inventory != null) VehicleHome.Request(vehicle, false);
        }

        private void Process(Pawn vehicle)
        {
            if (!VehicleHome.Eligible(vehicle, map)) { End(vehicle); return; }
            if (VehicleHome.CrewCount(vehicle) > 0)
            {
                // Nobody leaves the vehicle into a fight unless the player says so; the vehicle keeps waiting.
                if (!ordered.Contains(vehicle) && GenHostility.AnyHostileActiveThreatToPlayer(map)) return;
                if (VehicleHome.Requested(vehicle)) { End(vehicle); return; } // boarded again during unloading: the trip is starting
                VehicleHome.Disembark(vehicle);
                if (VehicleHome.CrewCount(vehicle) > 0) return; // no room to get out: try again
            }
            if (VehicleHome.LoadingNow(vehicle)) { End(vehicle); return; } // the player is loading it: not a place to unload
            if (!vehicle.inventory.innerContainer.Any) { End(vehicle); return; }
            VehicleHome.Request(vehicle, true);
        }

        public override void MapComponentTick()
        {
            if (returning.Count == 0 || Find.TickManager.TicksGame % Interval != 0) return;
            foreach (Pawn vehicle in returning.ToList()) Process(vehicle);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref returning, "TMM_returningVehicles", LookMode.Reference);
            Scribe_Collections.Look(ref ordered, "TMM_unloadOrders", LookMode.Reference);
            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
            returning = (returning ?? new List<Pawn>()).Where(p => p != null).ToList();
            ordered = (ordered ?? new List<Pawn>()).Where(p => p != null).ToList();
        }
    }

    /// The only place that knows VF: everything through reflection, nothing happens when VF is absent or differs.
    [StaticConstructorOnStartup]
    public static class VehicleHome
    {
        private static readonly Type VehicleType = AccessTools.TypeByName("Vehicles.VehiclePawn");
        private static readonly Type DefType = AccessTools.TypeByName("Vehicles.VehicleDef");
        private static readonly FieldInfo Kind = DefType == null ? null : AccessTools.Field(DefType, "type");
        private static readonly PropertyInfo Aboard = VehicleType == null ? null : AccessTools.Property(VehicleType, "AllPawnsAboard");
        private static readonly MethodInfo Out = VehicleType == null ? null : AccessTools.Method(VehicleType, "DisembarkAll", Type.EmptyTypes);
        private static readonly MethodInfo OutOfCargo = VehicleType == null ? null : AccessTools.Method(VehicleType, "DisembarkAllFromInventory", Type.EmptyTypes);
        private static readonly FieldInfo CargoToLoad = VehicleType == null ? null : AccessTools.Field(VehicleType, "cargoToLoad");
        private static readonly FieldInfo UnloadFlag = AccessTools.Field(typeof(Pawn_InventoryTracker), "unloadEverything");
        public static bool Available { get { return Kind != null && Aboard != null && Out != null && UnloadFlag != null; } }

        static VehicleHome()
        {
            if (!Available) return;
            var harmony = new Harmony("turbomegamod.vehicle.home");
            MethodInfo spawn = AccessTools.Method(AccessTools.TypeByName("Vehicles.World.EnterMapUtilityVehicles"), "SpawnCaravanPawns");
            if (spawn != null)
                harmony.Patch(spawn, prefix: new HarmonyMethod(typeof(VehicleHome), "BeforeEnter"), postfix: new HarmonyMethod(typeof(VehicleHome), "AfterEnter"));
            MethodInfo gizmos = AccessTools.Method(VehicleType, "GetGizmos");
            if (gizmos != null) harmony.Patch(gizmos, postfix: new HarmonyMethod(typeof(VehicleHome), "Gizmos"));
        }

        public static bool IsVehicle(Thing t) { return VehicleType != null && VehicleType.IsInstanceOfType(t); }
        public static bool Eligible(Pawn vehicle, Map map)
        {
            return Available && IsVehicle(vehicle) && vehicle.Spawned && !vehicle.Dead && vehicle.Map == map && map.IsPlayerHome &&
                vehicle.Faction == Faction.OfPlayer && Convert.ToString(Kind.GetValue(vehicle.def)) == "Land";
        }
        public static int CrewCount(Pawn vehicle)
        {
            var crew = Aboard.GetValue(vehicle, null) as ICollection;
            return (crew == null ? 0 : crew.Count) + (vehicle.inventory == null ? 0 : vehicle.inventory.innerContainer.Count(t => t is Pawn));
        }
        public static bool LoadingNow(Pawn vehicle)
        {
            var list = CargoToLoad == null ? null : CargoToLoad.GetValue(vehicle) as IEnumerable<TransferableOneWay>;
            return list != null && list.Any(t => t != null && t.CountToTransfer > 0);
        }
        public static bool Requested(Pawn vehicle) { return (bool)UnloadFlag.GetValue(vehicle.inventory); }
        public static void Request(Pawn vehicle, bool on) { UnloadFlag.SetValue(vehicle.inventory, on); }
        public static void Disembark(Pawn vehicle)
        {
            Out.Invoke(vehicle, null);
            if (OutOfCargo != null && vehicle.inventory.innerContainer.Any(t => t is Pawn)) OutOfCargo.Invoke(vehicle, null);
        }

        private static void BeforeEnter(Caravan __0, out List<Pawn> __state)
        {
            __state = __0 == null || !__0.IsPlayerControlled ? null : __0.PawnsListForReading.Where(IsVehicle).ToList();
        }
        private static void AfterEnter(Map __1, bool draftColonists, List<Pawn> __state)
        {
            if (__state == null || __1 == null || draftColonists || !__1.IsPlayerHome) return;
            VehicleHomeArrival comp = __1.GetComponent<VehicleHomeArrival>();
            if (comp != null) foreach (Pawn v in __state) if (v.Spawned && v.Map == __1) comp.Begin(v, false);
        }
        private static IEnumerable<Gizmo> Gizmos(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo g in __result) yield return g;
            if (!__instance.Spawned || !Eligible(__instance, __instance.Map)) yield break;
            if (CrewCount(__instance) == 0 && !__instance.inventory.innerContainer.Any) yield break;
            VehicleHomeArrival comp = __instance.Map.GetComponent<VehicleHomeArrival>();
            if (comp == null) yield break;
            Pawn vehicle = __instance;
            yield return new Command_Action
            {
                defaultLabel = "TMM_VehicleUnloadHome".Translate(),
                defaultDesc = (comp.Unloading(vehicle) ? "TMM_VehicleUnloadHomeActive" : "TMM_VehicleUnloadHomeDesc").Translate(),
                icon = TexButton.Drop,
                action = delegate { comp.Begin(vehicle, true); }
            };
        }
    }
}