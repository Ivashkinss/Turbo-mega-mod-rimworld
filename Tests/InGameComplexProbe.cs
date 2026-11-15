using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class ComplexReloadLaunch
{
    public static readonly List<string> JobEnds = new List<string>();
    static ComplexReloadLaunch()
    {
        new Harmony("tmm.test.complex.trace").Patch(AccessTools.Method(typeof(Pawn_JobTracker), "EndCurrentJob"), prefix: new HarmonyMethod(typeof(ComplexReloadLaunch), "TraceEnd"));
        if (Environment.GetCommandLineArgs().Contains("-tmm-complex-reload")) LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Complex"));
    }
    private static void TraceEnd(Pawn_JobTracker __instance, JobCondition condition)
    {
        var job = __instance.curJob;
        if (job == null || (job.def != RestaurantMeals.WaitJob && job.def != RestaurantMeals.ServeJob && job.def != JobDefOf.Ingest)) return;
        if (JobEnds.Count < 12) JobEnds.Add(job.def.defName + " " + condition + " " + Environment.StackTrace);
    }
}
public sealed class ComplexProbe : MapComponent
{
    private bool reported, uiReported, saved;
    private int stage, checks, startTick;
    private readonly List<string> errors = new List<string>();
    private Building_ComplexDesk desk;
    private Building_ServiceCounter counter;
    private Pawn actor, guest;
    private Thing wood;
    private Building_Bed originalBed, staffBed;
    public ComplexProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_Values.Look(ref saved, "complexSaved"); Scribe_References.Look(ref desk, "complexDesk"); Scribe_References.Look(ref actor, "complexActor");
        Scribe_References.Look(ref originalBed, "complexOriginalBed"); Scribe_References.Look(ref staffBed, "complexStaffBed");
    }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Set(object obj, string field, object value) { AccessTools.Field(obj.GetType(), field).SetValue(obj, value); }
    private Job FindWork(bool emergency = false) { return new JobGiver_Work { emergency = emergency }.TryIssueJobPackage(actor, default(JobIssueParams)).Job; }
    private bool HaulsWood(Job job) { return job != null && (job.targetA.Thing == wood || (job.targetQueueA != null && job.targetQueueA.Any(t => t.Thing == wood))); }
    private Building_Bed Bed(IntVec3 cell)
    {
        var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.Steel); bed.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(bed, cell, map); return bed;
    }
    private void Setup()
    {
        map.fogGrid.ClearAllFog();
        Check(ComplexWorkPolicy.Patched, "Workplace policy hooks not installed");
        Check(Building_ComplexDesk.Work(ComplexRole.Host) == DefDatabase<WorkGiverDef>.GetNamed("Diplomat").workType,
            "Host role does not enable Hospitality's actual guest service work");
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned) { var wait = JobMaker.MakeJob(JobDefOf.Wait); wait.expiryInterval = 600000; pawn.jobs.StartJob(wait, JobCondition.InterruptForced); }
        var restaurantWork = DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService");
        Func<Pawn, bool> capable = p => !p.WorkTypeIsDisabled(restaurantWork) && !p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor) && !p.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter);
        actor = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(capable);
        // Random starting backstories can disable every eligible worker. Build
        // a suitable test actor rather than weakening the job-selection checks.
        for (int attempt = 0; actor == null && attempt < 32; attempt++) {
            var candidate = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (capable(candidate)) { GenSpawn.Spawn(candidate, new IntVec3(35, 0, 35), map); actor = candidate; }
            else candidate.Destroy();
        }
        if (actor == null) throw new Exception("Could not generate a capable complex fixture worker");
        var rect = new CellRect(40, 40, 30, 16);
        Area_Allowed complex, publicArea, stock, shopping;
        map.areaManager.TryMakeNewAllowed(out complex); map.areaManager.TryMakeNewAllowed(out publicArea); map.areaManager.TryMakeNewAllowed(out stock); map.areaManager.TryMakeNewAllowed(out shopping);
        foreach (var cell in rect) {
            foreach (var t in cell.GetThingList(map).ToArray()) if (t is Building || t is Plant) {if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil); map.roofGrid.SetRoof(cell, null); map.areaManager.Home[cell] = true;
            if (cell.x <= 51 && cell.z <= 51) { complex[cell] = true; publicArea[cell] = true; }
        }
        actor.pather.StopDead(); actor.Position = new IntVec3(42, 0, 43);
        actor.Notify_Teleported();
        actor.needs.food.CurLevelPercentage = 1; actor.needs.rest.CurLevelPercentage = 1;
        if (actor.needs.joy != null) actor.needs.joy.CurLevelPercentage = 1;
        for (int hour = 0; hour < 24; hour++) actor.timetable.SetAssignment(hour, TimeAssignmentDefOf.Work);
        foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading) actor.workSettings.SetPriority(work, 0);
        actor.workSettings.SetPriority(WorkTypeDefOf.Hauling, 2);
        originalBed = Bed(new IntVec3(60, 0, 48)); staffBed = Bed(new IntVec3(43, 0, 48));
        publicArea[staffBed.Position] = false; actor.ownership.ClaimBedIfNonMedical(originalBed);
        desk = (Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk")); desk.automaticServices = false; desk.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(desk, new IntVec3(42, 0, 48), map); desk.area = complex; desk.guestArea = publicArea; desk.foodArea = stock;
        Check(desk.Active && map.GetComponent<ComplexMap>().desk == desk, "Reception registration failed");
        Check(!new PlaceWorker_ComplexDesk().AllowsPlacing(desk.def, new IntVec3(65, 0, 45), Rot4.North, map).Accepted, "Second reception was allowed");
        counter = (Building_ServiceCounter)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ServiceCounter")); counter.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(counter, new IntVec3(42, 0, 46), map); stock[new IntVec3(42, 0, 42)] = true;
        desk.ApplyServiceAreas(); Check(counter.serviceArea == publicArea && counter.foodArea == stock, "Service defaults not applied");
        var native = map.components.Single(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
        Set(native, "defaultAreaShopping", shopping); Set(native, "askForSafety", false); Set(native, "guestsCanTakeFoodForFree", false);
        var shelf = (Building_Storage)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Shelf"), ThingDefOf.Steel); shelf.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(shelf, new IntVec3(62, 0, 44), map); shelf.GetStoreSettings().filter.SetDisallowAll(); shelf.GetStoreSettings().filter.SetAllow(ThingDefOf.WoodLog, true);
        shelf.GetStoreSettings().Priority = StoragePriority.Critical;
        wood = ThingMaker.MakeThing(ThingDefOf.WoodLog); wood.stackCount = 10; GenSpawn.Spawn(wood, new IntVec3(60, 0, 43), map);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        Find.PlaySettings.useWorkPriorities = false;
        Check(desk.SetRole(actor, ComplexRole.Doctor), "Doctor role rejected");
        Find.PlaySettings.useWorkPriorities = true;
        Check(actor.workSettings.GetPriority(WorkTypeDefOf.Hauling) == 2, "Doctor role erased colony work");
        Find.PlaySettings.useWorkPriorities = false; desk.SetRole(actor, ComplexRole.None);
        Check(actor.workSettings.GetPriority(WorkTypeDefOf.Doctor) == 0, "Checkbox mode lost original disabled-work setting");
        Find.PlaySettings.useWorkPriorities = true; desk.SetRole(actor, ComplexRole.Doctor);
        var fallback = FindWork(); Check(HaulsWood(fallback), "Doctor without patients did not fall back to colony work: " + fallback+" reach="+actor.CanReach(wood,PathEndMode.Touch,Danger.Deadly)+" forbidden="+wood.IsForbidden(actor)+" hauling="+actor.workSettings.GetPriority(WorkTypeDefOf.Hauling));
        actor.workSettings.SetPriority(WorkTypeDefOf.Doctor, 3); desk.SetRole(actor, ComplexRole.None);
        Check(actor.workSettings.GetPriority(WorkTypeDefOf.Doctor) == 3, "Removing role overwrote manual priorities"); actor.workSettings.SetPriority(WorkTypeDefOf.Doctor, 0);
        Check(desk.SetRole(actor, ComplexRole.Waiter), "Waiter role rejected");
        Check(desk.SetResidence(actor, true) && actor.ownership.OwnedBed == staffBed, "Employee not assigned an ordinary bed inside complex");
        var employee = desk.Employee(actor); employee.mode = ComplexWorkMode.ColonyOnly;
        Check(actor.ownership.OwnedBed == staffBed && ForbidUtility.InAllowedArea(staffBed.Position, actor), "Work mode blocked residence or ordinary movement");
        Check(desk.SetResidence(actor, false) && actor.ownership.OwnedBed == originalBed, "Previous bed not restored");
        Check(desk.SetResidence(actor, true), "Could not restore residence for save test");
        var table = ThingMaker.MakeThing(ThingDefOf.Table2x2c, ThingDefOf.Steel); table.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(table, new IntVec3(46, 0, 45), map);
        var seatCell = GenAdj.CellsAdjacentCardinal(table).First(c => c.Standable(map));
        var chair = ThingMaker.MakeThing(ThingDefOf.DiningChair, ThingDefOf.Steel); chair.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(chair, seatCell, map);
        var meal = ThingMaker.MakeThing(ThingDefOf.MealSimple); meal.stackCount = 2; GenSpawn.Spawn(meal, new IntVec3(42, 0, 42), map);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        var faction = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
        guest = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, faction); GenSpawn.Spawn(guest, seatCell, map); guest.inventory.innerContainer.ClearAndDestroyContents();
        var silver = ThingMaker.MakeThing(ThingDefOf.Silver); silver.stackCount = 30; guest.inventory.innerContainer.TryAdd(silver); guest.needs.food.CurLevelPercentage = 0.35f;
        guest.needs.rest.CurLevelPercentage = 1;
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"), "CreateLord").Invoke(null, new object[] { faction, new IntVec3(44, 0, 44), new List<Pawn> { guest }, map, false, false, 600000 });
        guest.GetLord().ReceiveMemo("TravelArrived");
        var eating = JobMaker.MakeJob(JobDefOf.Ingest, meal); eating.count = 1;
        guest.jobs.StartJob(eating, JobCondition.InterruptForced);
        map.GetComponent<RestaurantService>().ProcessOrders(true);
        Check(guest.CurJob == eating && map.GetComponent<RestaurantService>().ForGuest(guest) == null, "Customer search interrupted an unfinished meal");
        Check(map.GetComponent<RestaurantService>().TryOrder(guest, counter) != null, "No restaurant customer for workplace test");
        employee.mode = ComplexWorkMode.Both; var both = FindWork(); Check(both != null && both.def == RestaurantMeals.ServeJob, "Both-places worker did not choose hotel work: " + both);
        employee.mode = ComplexWorkMode.ComplexOnly; var only = FindWork(); Check(only != null && only.def == RestaurantMeals.ServeJob, "Complex-only worker did not choose hotel work: " + only);
        employee.mode = ComplexWorkMode.ColonyOnly; var colony = FindWork(); Check(HaulsWood(colony), "Colony-only worker took hotel work or failed fallback: " + colony);
        Check(ForbidUtility.InAllowedArea(chair.Position, actor), "Work-search scope leaked into normal movement");
        actor.workSettings.SetPriority(WorkTypeDefOf.Firefighter, 1);
        var fire = (Fire)ThingMaker.MakeThing(ThingDefOf.Fire); fire.fireSize = 0.4f; GenSpawn.Spawn(fire, new IntVec3(44, 0, 50), map);
        var emergency = FindWork(true); Check(emergency != null && emergency.def == JobDefOf.BeatFire, "Colony-only mode blocked emergency fire inside hotel: " + emergency);
        fire.Destroy(); actor.workSettings.SetPriority(WorkTypeDefOf.Firefighter, 0);
        employee.mode = ComplexWorkMode.Both; actor.jobs.StartJob(both, JobCondition.InterruptForced);
        startTick = Find.TickManager.TicksGame;
    }
    public override void MapComponentUpdate()
    {
        bool reload = Environment.GetCommandLineArgs().Contains("-tmm-complex-reload");
        if (reported || Find.CurrentMap != map || (!reload && !Environment.GetCommandLineArgs().Contains("-tmm-complex-probe"))) return;
        // This isolated automated fixture must actually simulate its jobs.
        Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
        try {
            if (reload) {
                Check(saved && desk != null && actor != null && desk.Active, "Complex save references missing");
                var employee = desk.Employee(actor); Check(employee != null && employee.mode == ComplexWorkMode.ComplexOnly && employee.role == ComplexRole.Waiter, "Employee mode or role lost");
                Check(employee.resident && actor.ownership.OwnedBed == staffBed && employee.originalBed == originalBed, "Staff residence snapshot lost");
                Check(desk.restaurantIncome == 15 && actor.workSettings.GetPriority(WorkTypeDefOf.Hauling) == 2, "Revenue or fallback priorities changed on load");
                Check(ForbidUtility.InAllowedArea(new IntVec3(60, 0, 43), actor), "Residence/mode imposed a permanent movement restriction");
                desk.SetResidence(actor, false); Check(actor.ownership.OwnedBed == originalBed, "Previous bed not restored after load");
                desk.SetRole(actor, ComplexRole.None); Check(actor.workSettings.GetPriority(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService")) == 0, "Previous work priority not restored after load");
                desk.Destroy(); Check(map.GetComponent<ComplexMap>().desk == null, "Removed desk left a workplace controller"); Finish(); return;
            }
            if (stage == 0) { Setup(); stage = 1; return; }
            if (stage == 1) {
                if ((counter.earned == 0 || guest.needs.food.CurLevelPercentage <= 0.35f) && Find.TickManager.TicksGame - startTick < 2500) return;
                var pending = map.GetComponent<RestaurantService>().ForGuest(guest);
                Check(counter.earned == 15 && desk.restaurantIncome == 15, "Real meal receipt not counted in complex dashboard: earned=" + counter.earned + " dashboard=" + desk.restaurantIncome +
                    " actor=" + actor.CurJob + " guest=" + guest.CurJob + " pending=" + (pending == null ? "none" : pending.id + "/" + pending.price + "/waiter=" + pending.waiter) + " active=" + desk.Active + " food=" + guest.needs.food.CurLevelPercentage + "\n" + string.Join("\n", ComplexReloadLaunch.JobEnds));
                Check(guest.needs.food.CurLevelPercentage > 0.35f && HotelBillingHooks.Money(guest) == 15, "Guest did not eat/pay in complex");
                // During the customer's real eating animation the waiter may
                // already have finished the original colony haul. Seed new work.
                var idle = JobMaker.MakeJob(JobDefOf.Wait); idle.expiryInterval = 600000; actor.jobs.StartJob(idle, JobCondition.InterruptForced);
                wood = ThingMaker.MakeThing(ThingDefOf.WoodLog); wood.stackCount = 10; GenSpawn.Spawn(wood, new IntVec3(60, 0, 43), map);
                var employee = desk.Employee(actor); employee.mode = ComplexWorkMode.ComplexOnly;
                var onlyIdle = FindWork(); Check(onlyIdle == null, "Complex-only worker took colony work without hotel orders: " + onlyIdle);
                employee.mode = ComplexWorkMode.Both; var fallback = FindWork(); Check(HaulsWood(fallback), "Both-places worker did not fall back after service: " + fallback);
                employee.mode = ComplexWorkMode.ComplexOnly; saved = true; GameDataSaveLoader.SaveGame("TMM-Complex");
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Complex")), "Complex fixture save missing"); Finish();
            }
        } catch (Exception ex) { errors.Add(ex.ToString()); Finish(); }
    }
    private void Finish()
    {
        reported = true; string file = Environment.GetCommandLineArgs().Contains("-tmm-complex-reload") ? "ComplexReloadProbe.json" : "ComplexProbe.json";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, file), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || desk == null || Event.current.type != EventType.Repaint || !Environment.GetCommandLineArgs().Contains("-tmm-complex-probe")) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(10, 10, 1050, 700));
        try { new Dialog_ComplexEmployees(desk).DoWindowContents(new Rect(0, 0, 990, 620)); }
        catch (Exception ex) { error = ex.ToString(); } finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "ComplexUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"staffWindowDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
