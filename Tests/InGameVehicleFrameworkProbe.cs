using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    /// <summary>
    /// Checks the integrated Vehicle Framework (SmashPhil, MIT) on RimWorld 1.7 and our first vehicle on it.
    /// Fresh (-tmm-vehicle-framework-probe): assemblies, defs, Harmony patches; spawn TMM_Vehicle_HunterTruck through
    /// VF's own spawner, let the map run with it, then save TMM-Vehicles.
    /// Reload (-tmm-vehicle-framework-reload): load TMM-Vehicles and find the same vehicle intact.
    /// The probe uses reflection only, so it compiles without referencing Vehicles.dll.
    /// </summary>
    public sealed class InGameVehicleFrameworkProbe : MapComponent
    {
        private static bool s_done;
        private static bool s_loadRequested;
        private int m_ticks;
        private int m_spawnTick = -1;
        private Thing m_vehicle;
        private IntVec3 m_startPos = IntVec3.Invalid;
        private IntVec3 m_dest = IntVec3.Invalid;
        private string m_driveNote = "not started";
        private readonly List<string> m_checks = new List<string>();
        private bool m_allPassed = true;
        private const string SaveName = "TMM-Vehicles";
        private const string TruckDef = "TMM_Vehicle_HunterTruck";

        public InGameVehicleFrameworkProbe(Map map) : base(map) { }

        private static bool Has(string flag)
        {
            foreach (string a in Environment.GetCommandLineArgs()) if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static bool Fresh { get { return Has("-tmm-vehicle-framework-probe"); } }
        private static bool Reload { get { return Has("-tmm-vehicle-framework-reload"); } }

        [StaticConstructorOnStartup]
        private static class Loader
        {
            static Loader()
            {
                if (!Reload) return;
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (s_loadRequested || Current.ProgramState != ProgramState.Entry) return;
                    s_loadRequested = true;
                    GameDataSaveLoader.LoadGame(SaveName);
                });
            }
        }

        private void Check(string name, bool ok, string actual)
        {
            if (!ok) m_allPassed = false;
            m_checks.Add("{\"name\":\"" + name + "\",\"passed\":" + (ok ? "true" : "false") + ",\"actual\":\"" + (actual ?? "").Replace("\\", "/").Replace("\"", "'") + "\"}");
        }

        private static Def TruckVehicleDef()
        {
            Type t = AccessTools.TypeByName("Vehicles.VehicleDef");
            return t == null ? null : GenDefDatabase.GetDef(t, TruckDef, false);
        }

        private Thing FindTruck()
        {
            foreach (Thing t in map.listerThings.AllThings) if (t.def != null && t.def.defName == TruckDef) return t;
            foreach (Pawn p in map.mapPawns.AllPawns) if (p.def != null && p.def.defName == TruckDef) return p;
            return null;
        }

        public override void MapComponentTick()
        {
            if (s_done || map != Find.AnyPlayerHomeMap) return;
            if (!Fresh && !Reload) return;
            m_ticks++;
            if (Reload) { if (m_ticks >= 300) FinishReload(); return; }
            if (m_ticks == 60) SpawnTruck();
            if (m_ticks == 90) StartDrive();
            if (m_ticks >= 900) FinishFresh();
        }

        /// Refuel, put a colonist in the driver's seat, start the engine and drive about 15 cells.
        private void StartDrive()
        {
            try
            {
                if (m_vehicle == null) { m_driveNote = "no vehicle"; return; }
                ThingWithComps v = (ThingWithComps)m_vehicle;
                ThingComp fuel = v.AllComps.FirstOrDefault(c => c.GetType().Name == "CompFueledTravel");
                if (fuel != null) AccessTools.Method(fuel.GetType(), "Refuel", new Type[] { typeof(float) }).Invoke(fuel, new object[] { 80f });
                Pawn driver = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                GenSpawn.Spawn(driver, m_vehicle.Position + new IntVec3(3, 0, 0), map);
                bool boarded = (bool)AccessTools.Method(m_vehicle.GetType(), "TryAddPawn", new Type[] { typeof(Pawn) }).Invoke(m_vehicle, new object[] { driver });
                object ignition = AccessTools.Field(m_vehicle.GetType(), "ignition").GetValue(m_vehicle);
                AccessTools.Property(ignition.GetType(), "Drafted").SetValue(ignition, true, null);
                m_startPos = m_vehicle.Position;
                IntVec3 dest;
                if (!CellFinder.TryFindRandomCellNear(m_startPos + new IntVec3(15, 0, 0), map, 6, c => c.Standable(map) && c.GetEdifice(map) == null, out dest)) dest = m_startPos + new IntVec3(15, 0, 0);
                m_dest = dest;
                object pather = AccessTools.Field(m_vehicle.GetType(), "vehiclePather").GetValue(m_vehicle);
                AccessTools.Method(pather.GetType(), "StartPath").Invoke(pather, new object[] { new LocalTargetInfo(dest), PathEndMode.OnCell, false });
                bool canMove = (bool)AccessTools.Property(m_vehicle.GetType(), "CanMoveFinal").GetValue(m_vehicle, null);
                m_driveNote = "boarded=" + boarded + " canMove=" + canMove + " dest=" + dest;
            }
            catch (Exception ex) { m_driveNote = "exception: " + ex.GetBaseException().Message; }
        }

        private void SpawnTruck()
        {
            try
            {
                Def def = TruckVehicleDef();
                Type spawner = AccessTools.TypeByName("Vehicles.VehicleSpawner");
                MethodInfo spawn = spawner == null ? null : AccessTools.Method(spawner, "SpawnVehicleRandomized");
                if (def == null || spawn == null) return;
                IntVec3 cell;
                if (!CellFinder.TryFindRandomCellNear(map.Center, map, 20, c => GenAdj.OccupiedRect(c, Rot4.North, new IntVec2(2, 4)).All(x => x.InBounds(map) && x.Standable(map) && x.GetFirstPawn(map) == null && x.GetEdifice(map) == null), out cell)) cell = map.Center;
                m_vehicle = (Thing)spawn.Invoke(null, new object[] { def, cell, map, Faction.OfPlayer, (Rot4?)Rot4.North, false });
                m_spawnTick = Find.TickManager.TicksGame;
            }
            catch (Exception ex) { Check("SpawnTruckNoException", false, ex.GetBaseException().Message); }
        }

        private void CommonChecks()
        {
            Type vehicleDef = AccessTools.TypeByName("Vehicles.VehicleDef");
            Check("VehiclesAssemblyLoaded", vehicleDef != null && AccessTools.TypeByName("Vehicles.VehicleMod") != null, vehicleDef != null ? vehicleDef.Assembly.GetName().Version.ToString() : "null");
            Check("SmashToolsLoaded", AccessTools.TypeByName("SmashTools.SmashLog") != null, "checked");
            int vfDefs = 0;
            foreach (Type t in GenTypes.AllSubclasses(typeof(Def)))
            {
                if (t.Namespace == null || !t.Namespace.StartsWith("Vehicles")) continue;
                object list = typeof(DefDatabase<>).MakeGenericType(t).GetProperty("AllDefsListForReading").GetValue(null, null);
                vfDefs += ((System.Collections.IList)list).Count;
            }
            Check("VehicleFrameworkDefsLoaded", vfDefs > 0, vfDefs.ToString());
            int patched = 0;
            foreach (MethodBase m in Harmony.GetAllPatchedMethods())
            {
                Patches p = Harmony.GetPatchInfo(m);
                // Integrated, VF registers its patches under our package id, so count patches whose code lives in its namespaces.
                if (p != null && p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Concat(p.Finalizers).Any(x => x.PatchMethod != null && x.PatchMethod.DeclaringType != null && x.PatchMethod.DeclaringType.Namespace != null && (x.PatchMethod.DeclaringType.Namespace.StartsWith("Vehicles") || x.PatchMethod.DeclaringType.Namespace.StartsWith("SmashTools")))) patched++;
            }
            Check("VehicleFrameworkPatchesApplied", patched > 20, patched.ToString());
            Def truck = TruckVehicleDef();
            Check("HunterTruckDefLoaded", truck != null, truck != null ? truck.LabelCap.ToString() : "null");
        }

        private void FinishFresh()
        {
            s_done = true;
            CommonChecks();
            Thing truck = m_vehicle ?? FindTruck();
            Check("HunterTruckSpawned", truck != null && truck.Spawned && truck.Faction == Faction.OfPlayer, truck == null ? "null" : (truck.Spawned + " " + truck.Position));
            Check("HunterTruckSurvivesTicks", truck != null && !truck.Destroyed && m_spawnTick >= 0 && Find.TickManager.TicksGame - m_spawnTick >= 600, (Find.TickManager.TicksGame - m_spawnTick).ToString());
            int moved = (truck != null && m_startPos.IsValid) ? (int)truck.Position.DistanceTo(m_startPos) : -1;
            Check("HunterTruckDrives", moved >= 5, m_driveNote + " moved=" + moved + " now=" + (truck != null ? truck.Position.ToString() : "-"));
            ThingWithComps twc = truck as ThingWithComps;
            Check("HunterTruckHasFuelComp", twc != null && twc.AllComps.Any(c => c.GetType().Name == "CompFueledTravel"), twc == null ? "null" : string.Join(",", twc.AllComps.Select(c => c.GetType().Name).ToArray()));
            try { GameDataSaveLoader.SaveGame(SaveName); Check("SaveWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, SaveName + ".rws")), "checked"); }
            catch (Exception ex) { Check("SaveWritten", false, ex.GetBaseException().Message); }
            Write("VehicleFrameworkProbe.json", true);
        }

        private void FinishReload()
        {
            s_done = true;
            CommonChecks();
            Thing truck = FindTruck();
            Check("HunterTruckRestored", truck != null && truck.Spawned && truck.Faction == Faction.OfPlayer, truck == null ? "null" : truck.Position.ToString());
            Write("VehicleFrameworkReloadProbe.json", false);
        }

        private void Write(string file, bool ui)
        {
            string json = "{\"passed\":" + (m_allPassed ? "true" : "false") + ",\"checksCount\":" + m_checks.Count + ",\"checks\":[" + string.Join(",", m_checks.ToArray()) + "]}";
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, file), json);
            if (ui) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VehicleFrameworkUiProbe.json"), "{\"passed\":" + (m_allPassed ? "true" : "false") + "}");
        }
    }
}
