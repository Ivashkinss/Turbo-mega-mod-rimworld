using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class Clinic
    {
        public static readonly Type HospitalType = AccessTools.TypeByName("Hospital.HospitalMapComponent");
        public static readonly Type BedType = AccessTools.TypeByName("Hospital.CompHospitalBed");
        public static bool Enabled { get; private set; }
        static Clinic()
        {
            if (HospitalType == null || BedType == null) return;
            var h = new Harmony("turbomegamod.clinic.integration");
            foreach (string name in new[] { "Hospital", "Surgery" }) {
                h.Patch(AccessTools.PropertyGetter(BedType, name), postfix: new HarmonyMethod(typeof(Clinic), "BedFlag"));
                h.Patch(AccessTools.PropertySetter(BedType, name), prefix: new HarmonyMethod(typeof(Clinic), "SetBedFlag"));
            }
            h.Patch(AccessTools.Method(BedType, "CompGetGizmosExtra"), postfix: new HarmonyMethod(typeof(Clinic), "BedGizmos"));
            h.Patch(AccessTools.Method(HospitalType, "IsOpen"), postfix: new HarmonyMethod(typeof(Clinic), "IsOpen"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospital.HospitalMod"), "SettingsCategory"), postfix: new HarmonyMethod(typeof(Clinic), "SettingsCategory"));
            var finder = AccessTools.Method(typeof(RestUtility), "FindBedFor", new[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus?) });
            h.Patch(finder, postfix: new HarmonyMethod(typeof(Clinic), "PatientBed") { priority = Priority.Last });
            ClinicBilling.Install(h);
            ClinicGuests.Install(h);
            Enabled = true;
        }
        public static Building_ComplexDesk Desk(Map map)
        {
            if (map == null) return null;
            var component = map.GetComponent<ComplexMap>(); var desk = component == null ? null : component.desk;
            return desk != null && desk.Active && (desk.automaticServices || (desk.clinicArea != null && map.areaManager.AllAreas.Contains(desk.clinicArea))) ? desk : null;
        }
        public static bool WardBed(Building_Bed bed)
        {
            if (bed == null || !bed.Spawned || !bed.Medical || bed.ForPrisoners || bed.Faction != Faction.OfPlayer || !bed.def.building.bed_humanlike) return false;
            var guestType = AccessTools.TypeByName("Hospitality.Building_GuestBed");
            if (guestType != null && guestType.IsInstanceOfType(bed)) return false;
            var desk = Desk(bed.Map);
            return desk != null && desk.area[bed.Position] && (desk.automaticServices ? !ComplexLayout.StaffOnly(desk, bed.Position) && ComplexLayout.MedicalRoom(bed.GetRoom()) : desk.clinicArea[bed.Position]);
        }
        public static object Hospital(Map map) { return map == null || HospitalType == null ? null : map.components.FirstOrDefault(c => HospitalType.IsInstanceOfType(c)); }
        public static Map MapOf(object hospital) { return hospital == null ? null : AccessTools.Field(typeof(MapComponent), "map").GetValue(hospital) as Map; }
        public static IDictionary Patients(Map map)
        {
            var hospital = Hospital(map); return hospital == null ? null : AccessTools.Field(HospitalType, "Patients").GetValue(hospital) as IDictionary;
        }
        public static object Data(Pawn pawn)
        {
            var patients = pawn == null ? null : Patients(pawn.MapHeld); return patients == null || !patients.Contains(pawn) ? null : patients[pawn];
        }
        public static bool ActivePatient(Pawn pawn)
        {
            var data = Data(pawn); return data != null && !(bool)AccessTools.Field(data.GetType(), "Dismissed").GetValue(data);
        }
        private static void BedFlag(ThingComp __instance, MethodBase __originalMethod, ref bool __result)
        {
            var bed = __instance.parent as Building_Bed; var desk = bed == null ? null : ComplexLayout.Desk(bed.Map);
            if (desk != null) __result = WardBed(bed) && (__originalMethod.Name != "get_Surgery" || desk.surgeryAllowed);
            else if (__result && !WardBed(bed)) __result = false;
        }
        private static bool SetBedFlag(ThingComp __instance, bool __0) { return !__0 || WardBed(__instance.parent as Building_Bed); }
        private static void BedGizmos(ThingComp __instance, ref IEnumerable<Gizmo> __result) { __result = RestrictGizmos(__instance, __result); }
        private static IEnumerable<Gizmo> RestrictGizmos(ThingComp comp, IEnumerable<Gizmo> gizmos)
        {
            foreach (var gizmo in gizmos) {
                if (!WardBed(comp.parent as Building_Bed)) gizmo.Disable("TMM.Clinic.OutsideWard".Translate());
                yield return gizmo;
            }
        }
        private static void IsOpen(object __instance, ref bool __result)
        {
            var map = MapOf(__instance);
            if (Desk(map) == null) __result = false;
            var space = AccessTools.TypeByName("Hospitality.Utilities.SpaceMapUtility");
            if (__result && space != null && (bool)AccessTools.Method(space, "IsSpaceMap").Invoke(null, new object[] { map })) __result = false;
        }
        private static void PatientBed(Pawn sleeper, ref Building_Bed __result)
        {
            if (ActivePatient(sleeper) && __result != null && !WardBed(__result)) __result = null;
        }
        private static void SettingsCategory(ref string __result) { __result = "TMM.Clinic.Settings".Translate(); }
    }
}
