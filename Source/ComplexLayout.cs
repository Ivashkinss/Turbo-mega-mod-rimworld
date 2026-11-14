using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    public sealed class ComplexLayout : MapComponent
    {
        public Area_Allowed guestAccess;
        public List<Building_Bed> managedBeds = new List<Building_Bed>();
        private int nextCheck;
        internal static bool changing;
        private static readonly Type guestBedType = AccessTools.TypeByName("Hospitality.Building_GuestBed");
        private static readonly MethodInfo swap = guestBedType == null ? null : AccessTools.Method(guestBedType, "Swap");
        public ComplexLayout(Map map) : base(map) { }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_References.Look(ref guestAccess, "TMM_autoGuestArea");
            Scribe_Collections.Look(ref managedBeds, "TMM_autoGuestBeds", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && managedBeds == null) managedBeds = new List<Building_Bed>();
        }
        public static bool IsGuest(Building_Bed bed) { return bed != null && guestBedType != null && guestBedType.IsInstanceOfType(bed); }
        public static Building_ComplexDesk Desk(Map map)
        {
            var desk = map == null ? null : map.GetComponent<ComplexMap>().desk;
            return desk != null && desk.Active && desk.automaticServices ? desk : null;
        }
        public static bool StaffOnly(Building_ComplexDesk desk, IntVec3 cell)
        {
            return desk != null && desk.staffArea != null && desk.Map.areaManager.AllAreas.Contains(desk.staffArea) && desk.staffArea[cell];
        }
        public static bool StaffBed(Building_ComplexDesk desk, Building_Bed bed)
        {
            return StaffOnly(desk, bed.Position) || desk.employees.Any(e => e.resident && e.managedBed == bed);
        }
        public static bool MedicalRoom(Room room)
        {
            return room != null && !room.PsychologicallyOutdoors && !room.UsesOutdoorTemperature && room.ContainedBeds.Any(b => b.Medical && !b.ForPrisoners);
        }
        private Building_Bed Convert(Building_Bed bed, bool guest)
        {
            if (IsGuest(bed) == guest || swap == null) return bed;
            string target = guest ? bed.def.defName + "Guest" : bed.def.defName.Replace("Guest", "");
            if (DefDatabase<ThingDef>.GetNamedSilentFail(target) == null) return bed;
            var pos = bed.Position; bool selected = Find.Selector.IsSelected(bed);
            changing = true;
            try { swap.Invoke(null, new object[] { bed }); } finally { changing = false; }
            var result = pos.GetThingList(map).OfType<Building_Bed>().FirstOrDefault(b => b.def.defName == target);
            if (result != null && !selected) Find.Selector.Deselect(result);
            managedBeds.Remove(bed);
            return result ?? bed;
        }
        public Building_Bed PrepareStaffBed(Building_Bed bed) { return Convert(bed, false); }
        public void Synchronize()
        {
            var desk = Desk(map);
            if (desk == null) {
                var existing = map.GetComponent<ComplexMap>().desk;
                if (existing != null && !existing.automaticServices) return;
                foreach (var bed in managedBeds.ToArray()) if (bed != null && bed.Spawned) Convert(bed, false);
                managedBeds.Clear();
                if (guestAccess != null) foreach (var c in guestAccess.ActiveCells.ToArray()) guestAccess[c] = false;
                return;
            }
            if (guestAccess == null || !map.areaManager.AllAreas.Contains(guestAccess)) {
                map.areaManager.TryMakeNewAllowed(out guestAccess);
                if (guestAccess == null) return;
                guestAccess.SetLabel("TMM.Complex.AutoGuests".Translate());
            }
            foreach (var c in guestAccess.ActiveCells.ToArray()) if (!desk.area[c] || StaffOnly(desk, c)) guestAccess[c] = false;
            foreach (var c in desk.area.ActiveCells) if (!StaffOnly(desk, c) && !guestAccess[c]) guestAccess[c] = true;
            var native = map.components.FirstOrDefault(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
            if (native != null) AccessTools.Field(native.GetType(), "defaultAreaRestriction").SetValue(native, guestAccess);
            foreach (var bed in managedBeds.ToArray()) {
                if (bed == null || !bed.Spawned) { managedBeds.Remove(bed); continue; }
                if (!desk.area[bed.Position] || StaffBed(desk, bed)) { Convert(bed, false); managedBeds.Remove(bed); }
            }
            var beds = map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Where(b => b.def.building.bed_humanlike && desk.area[b.Position] && !b.ForPrisoners).ToArray();
            foreach (var room in beds.Select(b => b.GetRoom()).Where(r => r != null).Distinct().ToArray()) {
                bool medical = MedicalRoom(room);
                foreach (var original in room.ContainedBeds.Where(b => b.Faction == Faction.OfPlayer && b.def.building.bed_humanlike && !b.ForPrisoners && desk.area[b.Position]).ToArray()) {
                    if (StaffBed(desk, original)) continue;
                    var bed = Convert(original, !medical);
                    if (medical) { changing = true; try { bed.Medical = true; } finally { changing = false; } }
                    else if (IsGuest(bed)) {
                        if (!managedBeds.Contains(bed)) managedBeds.Add(bed);
                        if (HotelBillingHooks.Fee(bed) != desk.roomRate) AccessTools.Method(guestBedType, "SetRentalFee").Invoke(bed, new object[] { desk.roomRate });
                    }
                }
            }
        }
        public void SetMedicalRoom(Building_Bed bed, bool medical)
        {
            var desk = Desk(map); if (desk == null || !desk.area[bed.Position]) return;
            var room = bed.GetRoom();
            var beds = room == null || room.UsesOutdoorTemperature ? new[] { bed } : room.ContainedBeds.Where(b => b.Faction == Faction.OfPlayer && desk.area[b.Position] && !b.ForPrisoners).ToArray();
            foreach (var original in beds) {
                var current = Convert(original, false);
                changing = true; try { current.Medical = medical; } finally { changing = false; }
            }
            Synchronize();
        }
        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame < nextCheck) return;
            nextCheck = Find.TickManager.TicksGame + 250; Synchronize();
        }
    }

    [StaticConstructorOnStartup]
    public static class ComplexLayoutHooks
    {
        static ComplexLayoutHooks()
        {
            var h = new Harmony("turbomegamod.complex.automatic.layout");
            h.CreateReversePatcher(AccessTools.DeclaredMethod(typeof(Building_Bed), "GetGizmos"), new HarmonyMethod(typeof(ComplexLayoutHooks), "BaseBedGizmos")).Patch();
            h.Patch(AccessTools.PropertySetter(typeof(Building_Bed), "Medical"), prefix: new HarmonyMethod(typeof(ComplexLayoutHooks), "Medical"));
            h.Patch(AccessTools.Method(typeof(Building_Bed), "GetGizmos"), postfix: new HarmonyMethod(typeof(ComplexLayoutHooks), "BedGizmos") { priority = Priority.Last });
            var guestBed = AccessTools.TypeByName("Hospitality.Building_GuestBed");
            if (guestBed != null) h.Patch(AccessTools.DeclaredMethod(guestBed, "GetGizmos"), prefix: new HarmonyMethod(typeof(ComplexLayoutHooks), "GuestBedGizmos"),
                postfix: new HarmonyMethod(typeof(ComplexLayoutHooks), "BedGizmos") { priority = Priority.Last });
            var comp = AccessTools.TypeByName("Hospitality.CompGuest");
            if (comp != null) h.Patch(AccessTools.PropertyGetter(comp, "GuestArea"), postfix: new HarmonyMethod(typeof(ComplexLayoutHooks), "GuestArea") { priority = Priority.Last });
        }
        private static bool Medical(Building_Bed __instance, bool value)
        {
            if (ComplexLayout.changing || !__instance.Spawned) return true;
            var desk = ComplexLayout.Desk(__instance.Map);
            if (desk == null || !desk.area[__instance.Position] || ComplexLayout.StaffBed(desk, __instance)) return true;
            __instance.Map.GetComponent<ComplexLayout>().SetMedicalRoom(__instance, value); return false;
        }
        private static void GuestArea(ThingComp __instance, ref Area __result)
        {
            var pawn = __instance.parent as Pawn; var desk = pawn == null ? null : ComplexLayout.Desk(pawn.MapHeld);
            if (desk == null) return;
            var access = pawn.MapHeld.GetComponent<ComplexLayout>().guestAccess;
            if (access != null) __result = access;
        }
        private static void BedGizmos(Building_Bed __instance, ref IEnumerable<Gizmo> __result) { __result = FilterBed(__instance, __result); }
        private static bool GuestBedGizmos(Building_Bed __instance, ref IEnumerable<Gizmo> __result)
        {
            var desk = ComplexLayout.Desk(__instance.Map);
            if (desk == null || !desk.area[__instance.Position]) return true;
            // Do not enumerate the native price widget merely to hide it: its
            // constructor expects selected guest beds and performs extra work.
            __result = BaseBedGizmos(__instance); return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<Gizmo> BaseBedGizmos(Building_Bed instance) { throw new NotImplementedException("Reverse patch was not installed"); }
        private static IEnumerable<Gizmo> FilterBed(Building_Bed bed, IEnumerable<Gizmo> gizmos)
        {
            var desk = ComplexLayout.Desk(bed.Map);
            bool managed = desk != null && desk.area[bed.Position] && !ComplexLayout.StaffBed(desk, bed) && !bed.ForPrisoners;
            foreach (var gizmo in gizmos) {
                string name = gizmo.GetType().FullName;
                var command = gizmo as Command;
                if (managed && name == "Hospitality.Gizmo_GuestBed") continue;
                if (managed && name == "Verse.Command_SetBedOwnerType") continue;
                if (managed && command != null && (command.defaultLabel == "CommandBedSetAsGuestLabel".Translate().ToString() ||
                    command.defaultLabel == "CommandThingSetOwnerLabel".Translate().ToString() || command.defaultLabel == "CommandBedSetForPrisonersLabel".Translate().ToString() ||
                    command.defaultLabel == "CommandBedSetAsMedicalLabel".Translate().ToString() || command.defaultLabel == "CommandBedSetAsHospitalLabel".Translate().ToString() ||
                    command.defaultLabel == "CommandBedSetAsSurgeryLabel".Translate().ToString())) continue;
                yield return gizmo;
            }
            if (managed && bed.def.building.bed_humanlike) yield return new Command_Toggle {
                defaultLabel = "CommandBedSetAsMedicalLabel".Translate(), defaultDesc = "TMM.Complex.MedicalHelp".Translate(),
                icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/AsMedical"), isActive = () => bed.Medical,
                toggleAction = () => bed.Map.GetComponent<ComplexLayout>().SetMedicalRoom(bed, !bed.Medical) };
        }
    }
}
