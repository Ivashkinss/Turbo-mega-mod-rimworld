using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public enum ComplexWorkMode { Both, ColonyOnly, ComplexOnly }
    public enum ComplexRole { None, Waiter, Cook, Doctor, Host, Cleaner }

    public sealed class ComplexEmployee : IExposable
    {
        public Pawn pawn;
        public ComplexWorkMode mode;
        public ComplexRole role;
        public bool resident;
        public Building_Bed originalBed, managedBed;
        public WorkTypeDef managedWork;
        public int originalPriority;
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn"); Scribe_Values.Look(ref mode, "mode"); Scribe_Values.Look(ref role, "role");
            Scribe_Values.Look(ref resident, "resident"); Scribe_References.Look(ref originalBed, "originalBed"); Scribe_References.Look(ref managedBed, "managedBed");
            Scribe_Defs.Look(ref managedWork, "managedWork"); Scribe_Values.Look(ref originalPriority, "originalPriority");
        }
    }

    public sealed class ComplexMap : MapComponent
    {
        public Building_ComplexDesk desk;
        public ComplexMap(Map map) : base(map) { }
        public override void ExposeData() { base.ExposeData(); Scribe_References.Look(ref desk, "TMM_complexDesk"); }
    }

    public sealed class Building_ComplexDesk : Building
    {
        private static readonly FieldInfo priorityField = AccessTools.Field(typeof(Pawn_WorkSettings), "priorities");
        private static DefMap<WorkTypeDef, int> Priorities(Pawn pawn) { return pawn == null || pawn.workSettings == null || priorityField == null ? null : priorityField.GetValue(pawn.workSettings) as DefMap<WorkTypeDef, int>; }
        public Area area, guestArea, foodArea;
        public List<ComplexEmployee> employees = new List<ComplexEmployee>();
        public int hotelIncome, restaurantIncome, clinicIncome;
        public bool Active { get { return Spawned && area != null && Map.areaManager.AllAreas.Contains(area) && Map.GetComponent<ComplexMap>().desk == this; } }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var component = map.GetComponent<ComplexMap>(); if (component != null && (component.desk == null || !component.desk.Spawned)) component.desk = this;
        }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_References.Look(ref area, "complexArea"); Scribe_References.Look(ref guestArea, "guestArea"); Scribe_References.Look(ref foodArea, "foodArea");
            Scribe_Collections.Look(ref employees, "employees", LookMode.Deep);
            Scribe_Values.Look(ref hotelIncome, "hotelIncome"); Scribe_Values.Look(ref restaurantIncome, "restaurantIncome"); Scribe_Values.Look(ref clinicIncome, "clinicIncome");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && employees == null) employees = new List<ComplexEmployee>();
        }
        public ComplexEmployee Employee(Pawn pawn, bool create = false)
        {
            var result = employees.FirstOrDefault(e => e.pawn == pawn);
            if (result == null && create) { result = new ComplexEmployee { pawn = pawn }; employees.Add(result); }
            return result;
        }
        public ComplexWorkMode Mode(Pawn pawn) { var employee = Employee(pawn); return employee == null ? ComplexWorkMode.Both : employee.mode; }
        public static WorkTypeDef Work(ComplexRole role)
        {
            string name = role == ComplexRole.Waiter ? "TMM_RestaurantService" : role == ComplexRole.Cook ? "Cooking" : role == ComplexRole.Doctor ? "Doctor" :
                role == ComplexRole.Host ? "Diplomat" : role == ComplexRole.Cleaner ? "Cleaning" : null;
            return name == null ? null : DefDatabase<WorkTypeDef>.GetNamed(name);
        }
        public bool SetRole(Pawn pawn, ComplexRole role)
        {
            var work = Work(role);
            var priorities = Priorities(pawn);
            if (priorities == null || (work != null && pawn.WorkTypeIsDisabled(work))) return false;
            var employee = Employee(pawn, true);
            if (employee.role == role) return true;
            RestorePriority(employee);
            employee.role = role; employee.managedWork = work;
            if (work != null) { employee.originalPriority = priorities[work]; pawn.workSettings.SetPriority(work, 1); }
            return true;
        }
        private static void RestorePriority(ComplexEmployee employee)
        {
            var priorities = Priorities(employee.pawn);
            // GetPriority normalizes enabled work to 3 in checkbox mode. Use
            // stored values so toggling the work-tab UI does not lose choices.
            if (priorities != null && employee.managedWork != null && priorities[employee.managedWork] == 1)
                employee.pawn.workSettings.SetPriority(employee.managedWork, employee.originalPriority);
            employee.managedWork = null;
        }
        public bool SetResidence(Pawn pawn, bool value)
        {
            var employee = Employee(pawn, true);
            if (employee.resident == value) return true;
            if (!value) { RestoreBed(employee); return true; }
            if (!Active || pawn.Map != Map) return false;
            var guestBed = AccessTools.TypeByName("Hospitality.Building_GuestBed");
            var beds = Map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Where(b => area[b.Position] && !b.Medical && !b.ForPrisoners && b.def.building.bed_humanlike &&
                (guestBed == null || !guestBed.IsInstanceOfType(b))).ToArray();
            var original = pawn.ownership.OwnedBed;
            var bed = beds.FirstOrDefault(b => b == original) ?? beds.Where(b => b.AnyUnownedSleepingSlot)
                .OrderBy(b => b.CompAssignableToPawn.AssignedPawnsForReading.Count).ThenBy(b => b.Position.DistanceToSquared(pawn.Position)).FirstOrDefault();
            if (bed == null) return false;
            employee.originalBed = original;
            if (pawn.ownership.OwnedBed != bed && !pawn.ownership.ClaimBedIfNonMedical(bed)) return false;
            employee.managedBed = bed; employee.resident = true; return true;
        }
        private static void RestoreBed(ComplexEmployee employee)
        {
            var pawn = employee.pawn;
            if (pawn != null && pawn.ownership != null && pawn.ownership.OwnedBed == employee.managedBed) {
                pawn.ownership.UnclaimBed();
                if (employee.originalBed != null && employee.originalBed.Spawned && !employee.originalBed.Medical && !employee.originalBed.ForPrisoners && employee.originalBed.AnyUnownedSleepingSlot)
                    pawn.ownership.ClaimBedIfNonMedical(employee.originalBed);
            }
            employee.resident = false; employee.managedBed = null; employee.originalBed = null;
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (var employee in employees) { RestorePriority(employee); RestoreBed(employee); }
            employees.Clear();
            var component = Map.GetComponent<ComplexMap>(); if (component.desk == this) component.desk = null;
            base.DeSpawn(mode);
        }
        public void ApplyServiceAreas()
        {
            if (!Active) return;
            foreach (var counter in Map.listerBuildings.allBuildingsColonist.OfType<Building_ServiceCounter>().Where(c => area[c.Position])) {
                if (counter.serviceArea == null) counter.serviceArea = guestArea;
                if (counter.foodArea == null) counter.foodArea = foodArea;
            }
            var native = Map.components.FirstOrDefault(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
            if (native != null && guestArea != null) AccessTools.Field(native.GetType(), "defaultAreaRestriction").SetValue(native, guestArea);
        }
        private Command_Action AreaButton(string key, Func<Area> getter, Action<Area> setter)
        {
            return new Command_Action { defaultLabel = key.Translate() + ": " + (getter() == null ? "TMM.Restaurant.Unassigned".Translate().ToString() : getter().Label),
                defaultDesc = "TMM.Complex.AreasHelp".Translate(), icon = ContentFinder<Texture2D>.Get("UI/Complex/Workplaces"), action = delegate {
                    var options = new List<FloatMenuOption> { new FloatMenuOption("TMM.Restaurant.Unassigned".Translate(), () => setter(null)) };
                    foreach (var candidate in Map.areaManager.AllAreas.Where(a => a is Area_Allowed)) { var selected = candidate; options.Add(new FloatMenuOption(candidate.Label, () => setter(selected))); }
                    Find.WindowStack.Add(new FloatMenu(options));
                } };
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            yield return AreaButton("TMM.Complex.Area", () => area, value => area = value);
            yield return AreaButton("TMM.Complex.GuestArea", () => guestArea, value => guestArea = value);
            yield return AreaButton("TMM.Complex.FoodArea", () => foodArea, value => foodArea = value);
            yield return new Command_Action { defaultLabel = "TMM.Complex.Employees".Translate(), defaultDesc = "TMM.Complex.EmployeesHelp".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Complex/Staff"), action = () => Find.WindowStack.Add(new Dialog_ComplexEmployees(this)) };
            yield return new Command_Action { defaultLabel = "TMM.Complex.Apply".Translate(), defaultDesc = "TMM.Complex.ApplyHelp".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Restaurant/Menu"), action = ApplyServiceAreas };
        }
        public override string GetInspectString()
        {
            return base.GetInspectString() + "\n" + "TMM.Complex.Income".Translate(hotelIncome, restaurantIncome, clinicIncome) + "\n" + "TMM.Complex.StaffCount".Translate(employees.Count(e => e.pawn != null && !e.pawn.Dead));
        }
    }

    public sealed class PlaceWorker_ComplexDesk : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            bool duplicate = map.listerThings.AllThings.Any(t => t != thingToIgnore && t != thing &&
                (t is Building_ComplexDesk || (t.def.entityDefToBuild == checkingDef && t.Position != loc)));
            return duplicate ? (AcceptanceReport)"TMM.Complex.OneDesk".Translate().ToString() : (AcceptanceReport)true;
        }
    }

    public sealed class Dialog_ComplexEmployees : Window
    {
        private readonly Building_ComplexDesk desk;
        private Vector2 scroll;
        public Dialog_ComplexEmployees(Building_ComplexDesk desk) { this.desk = desk; doCloseX = true; doCloseButton = true; forcePause = true; absorbInputAroundWindow = true; }
        public override Vector2 InitialSize { get { return new Vector2(1000, 650); } }
        public override void DoWindowContents(Rect rect)
        {
            string help = "TMM.Complex.EmployeesHelp".Translate(); float helpHeight = Mathf.Max(50, Text.CalcHeight(help, rect.width));
            Widgets.Label(new Rect(0, 0, rect.width, helpHeight), help);
            var pawns = desk.Map.mapPawns.FreeColonistsSpawned.Concat(desk.employees.Select(e => e.pawn).Where(p => p != null && !p.Dead)).Distinct().OrderBy(p => p.LabelShort).ToArray();
            var outer = new Rect(0, helpHeight + 10, rect.width, rect.height - helpHeight - 65); var inner = new Rect(0, 0, rect.width - 20, pawns.Length * 48);
            Widgets.BeginScrollView(outer, ref scroll, inner);
            for (int i = 0; i < pawns.Length; i++) {
                var pawn = pawns[i]; var employee = desk.Employee(pawn, true); float y = i * 48;
                Widgets.Label(new Rect(0, y, 180, 40), pawn.LabelShortCap);
                if (Widgets.ButtonText(new Rect(185, y, 210, 38), ("TMM.Complex.Mode." + employee.mode).Translate())) {
                    Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(ComplexWorkMode)).Cast<ComplexWorkMode>()
                        .Select(m => new FloatMenuOption(("TMM.Complex.Mode." + m).Translate(), () => employee.mode = m)).ToList()));
                }
                if (Widgets.ButtonText(new Rect(405, y, 190, 38), ("TMM.Complex.Role." + employee.role).Translate())) {
                    Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(ComplexRole)).Cast<ComplexRole>().Select(r => new FloatMenuOption(("TMM.Complex.Role." + r).Translate(), delegate {
                        if (!desk.SetRole(pawn, r)) Messages.Message("TMM.Complex.Incapable".Translate(pawn.LabelShortCap), MessageTypeDefOf.RejectInput, false);
                    })).ToList()));
                }
                if (Widgets.ButtonText(new Rect(605, y, 280, 38), (employee.resident ? "TMM.Complex.Resident" : "TMM.Complex.NotResident").Translate())) {
                    if (!desk.SetResidence(pawn, !employee.resident)) Messages.Message("TMM.Complex.NoBed".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
            Widgets.EndScrollView();
        }
    }

    [StaticConstructorOnStartup]
    public static class ComplexWorkPolicy
    {
        [ThreadStatic] private static Pawn scanningPawn;
        [ThreadStatic] private static bool essential;
        private struct Context { public Pawn pawn; public bool essential; }
        public static bool Patched { get; private set; }
        static ComplexWorkPolicy()
        {
            var harmony = new Harmony("turbomegamod.complex.workplaces");
            var giver = AccessTools.TypeByName("RimWorld.JobGiver_Work");
            var method = AccessTools.Method(giver, "TryIssueJobPackage");
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(ComplexWorkPolicy), "Begin"), finalizer: new HarmonyMethod(typeof(ComplexWorkPolicy), "End"));
            var methods = new HashSet<MethodInfo>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] types;
                try { types = assembly.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                foreach (var type in types.Where(t => !t.ContainsGenericParameters && typeof(WorkGiver).IsAssignableFrom(t))) {
                    var skip = AccessTools.Method(type, "ShouldSkip");
                    if (skip != null) skip = AccessTools.DeclaredMethod(skip.DeclaringType, skip.Name, skip.GetParameters().Select(p => p.ParameterType).ToArray());
                    if (skip != null && !skip.IsStatic && !skip.IsAbstract && !skip.ContainsGenericParameters) methods.Add(skip);
                }
            }
            foreach (var skip in methods) harmony.Patch(skip, prefix: new HarmonyMethod(typeof(ComplexWorkPolicy), "Considering"));
            harmony.Patch(AccessTools.Method(typeof(ForbidUtility), "InAllowedArea"), postfix: new HarmonyMethod(typeof(ComplexWorkPolicy), "Allowed"));
            Patched = true;
        }
        private static void Begin(Pawn __0, out Context __state) { __state = new Context { pawn = scanningPawn, essential = essential }; scanningPawn = __0; essential = false; }
        private static Exception End(Exception __exception, Context __state) { scanningPawn = __state.pawn; essential = __state.essential; return __exception; }
        private static void Considering(WorkGiver __instance)
        {
            var work = __instance.def == null ? null : __instance.def.workType;
            essential = work != null && (work.defName == "Firefighter" || work.defName == "Patient" || work.defName == "BedRest");
        }
        public static bool Allows(Pawn pawn, IntVec3 cell)
        {
            if (pawn == null || pawn.Map == null || pawn.Faction != Faction.OfPlayer) return true;
            var component = pawn.Map.GetComponent<ComplexMap>(); var desk = component == null ? null : component.desk;
            if (desk == null || !desk.Active) return true;
            var mode = desk.Mode(pawn); bool inside = cell.InBounds(pawn.Map) && desk.area[cell];
            return mode == ComplexWorkMode.Both || (mode == ComplexWorkMode.ComplexOnly ? inside : !inside);
        }
        private static void Allowed(IntVec3 __0, Pawn __1, ref bool __result)
        {
            if (__result && __1 == scanningPawn && !essential && !Allows(__1, __0)) __result = false;
        }
        public static void Record(Map map, IntVec3 cell, string service, int amount)
        {
            if (map == null || amount <= 0) return;
            var component = map.GetComponent<ComplexMap>(); var desk = component == null ? null : component.desk;
            if (desk == null || !desk.Active || !desk.area[cell]) return;
            if (service == "hotel") desk.hotelIncome += amount;
            else if (service == "restaurant") desk.restaurantIncome += amount;
            else if (service == "clinic") desk.clinicIncome += amount;
        }
    }
}
