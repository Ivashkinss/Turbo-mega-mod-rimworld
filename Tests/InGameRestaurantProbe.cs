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
public static class RestaurantReloadLaunch
{
    public static readonly List<string> JobEnds = new List<string>();
    static RestaurantReloadLaunch()
    {
        new Harmony("tmm.test.restaurant.job.trace").Patch(AccessTools.Method(typeof(Pawn_JobTracker), "EndCurrentJob"),
            prefix: new HarmonyMethod(typeof(RestaurantReloadLaunch), "TraceEnd"));
        new Harmony("tmm.test.restaurant.delivery.trace").Patch(AccessTools.Method(typeof(RestaurantService), "Deliver"),
            prefix: new HarmonyMethod(typeof(RestaurantReloadLaunch), "TraceDelivery"));
        if (Environment.GetCommandLineArgs().Contains("-tmm-restaurant-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Restaurant"));
    }
    private static void TraceDelivery(RestaurantService __instance, Pawn waiter, Pawn guest)
    {
        var o = __instance.ForGuest(guest); var meal = waiter.carryTracker.CarriedThing;
        JobEnds.Add("DELIVERY order=" + (o != null) + " waiter=" + (o != null && o.waiter == waiter) + " valid=" +
            (o == null ? false : (bool)AccessTools.Method(typeof(RestaurantService), "Valid").Invoke(__instance, new object[] { o })) +
            " carry=" + meal + " edible=" + (meal != null && RestaurantMeals.Edible(guest, meal)) + " positions=" + waiter.Position + "/" + guest.Position +
            " money=" + HotelBillingHooks.Money(guest) + " quote=" + (o == null ? -1 : o.price));
    }
    private static void TraceEnd(Pawn_JobTracker __instance, JobCondition condition)
    {
        var job = __instance.curJob;
        if (job == null || (job.def.defName != "TMM_WaitForMeal" && job.def.defName != "TMM_ServeMeal" && job.def != JobDefOf.Ingest)) return;
        if (JobEnds.Count < 12) JobEnds.Add(job.def.defName + " " + condition + " " + Environment.StackTrace);
    }
}
public sealed class RestaurantProbe : MapComponent
{
    private bool reported, uiReported, saved;
    private int stage, checks, stageTick, initialFloor, firstPrice, savedPrice, savedMoney, savedFloor, savedRevenue;
    private readonly List<string> errors = new List<string>();
    private Building_ServiceCounter counter;
    private Pawn guest, poor, waiter, pendingGuest;
    private Thing savedCarriedMeal;
    private Area hotelArea;
    private RestaurantService Service { get { return map.GetComponent<RestaurantService>(); } }
    public RestaurantProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_Values.Look(ref saved, "restaurantSaved"); Scribe_References.Look(ref counter, "restaurantCounter");
        Scribe_References.Look(ref waiter, "restaurantWaiter"); Scribe_References.Look(ref pendingGuest, "restaurantPendingGuest");
        Scribe_References.Look(ref savedCarriedMeal, "restaurantCarriedMeal");
        Scribe_Values.Look(ref savedPrice, "restaurantPrice"); Scribe_Values.Look(ref savedMoney, "restaurantMoney");
        Scribe_Values.Look(ref savedFloor, "restaurantFloor"); Scribe_Values.Look(ref savedRevenue, "restaurantRevenue");
    }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Set(object obj, string field, object value) { AccessTools.Field(obj.GetType(), field).SetValue(obj, value); }
    private int FloorSilver() { return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t => t.Spawned).Sum(t => t.stackCount); }
    private Pawn MakeGuest(int money, bool split = true)
    {
        var faction = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, faction); GenSpawn.Spawn(pawn, new IntVec3(44, 0, 43), map);
        pawn.inventory.innerContainer.ClearAndDestroyContents();
        foreach (int count in new[] { split ? Math.Min(money, 6) : money, split ? Math.Max(0, money - 6) : 0 }) if (count > 0) {
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver); silver.stackCount = count; pawn.inventory.innerContainer.TryAdd(silver, false);
        }
        pawn.needs.food.CurLevelPercentage = 1f; pawn.needs.rest.CurLevelPercentage = 1f;
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"), "CreateLord").Invoke(null,
            new object[] { faction, new IntVec3(44, 0, 44), new List<Pawn> { pawn }, map, false, false, 600000 });
        pawn.GetLord().ReceiveMemo("TravelArrived");
        var wait = JobMaker.MakeJob(JobDefOf.Wait); wait.expiryInterval = 600000; pawn.jobs.StartJob(wait, JobCondition.InterruptForced);
        return pawn;
    }
    private void StartServing()
    {
        var giver = new WorkGiver_Restaurant();
        Check(giver.HasJobOnThing(waiter, counter), "Waiter work giver found no order");
        var job = giver.JobOnThing(waiter, counter); Check(job != null, "No real waiter job");
        waiter.jobs.StartJob(job, JobCondition.InterruptForced);
    }
    private void MealStock(int count)
    {
        var meal = ThingMaker.MakeThing(ThingDefOf.MealSimple); meal.stackCount = count;
        GenSpawn.Spawn(meal, new IntVec3(42, 0, 42), map);
    }
    private void Setup()
    {
        foreach (var colonist in map.mapPawns.FreeColonistsSpawned) {
            var wait = JobMaker.MakeJob(JobDefOf.Wait); wait.expiryInterval = 600000; colonist.jobs.StartJob(wait, JobCondition.InterruptForced);
        }
        waiter = map.mapPawns.FreeColonistsSpawned.First(p => !p.WorkTypeIsDisabled(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService"))); waiter.drafter.Drafted = false;
        for (int hour = 0; hour < 24; hour++) waiter.timetable.SetAssignment(hour, TimeAssignmentDefOf.Work);
        waiter.needs.food.CurLevelPercentage = 1f; waiter.needs.rest.CurLevelPercentage = 1f;
        if (waiter.needs.joy != null) waiter.needs.joy.CurLevelPercentage = 1f;
        Area_Allowed hotel, dining, stock, shop; map.areaManager.TryMakeNewAllowed(out hotel); map.areaManager.TryMakeNewAllowed(out dining); map.areaManager.TryMakeNewAllowed(out stock); map.areaManager.TryMakeNewAllowed(out shop);
        hotelArea = hotel;
        var rect = new CellRect(40, 40, 12, 12);
        foreach (var c in rect) {
            foreach (var thing in c.GetThingList(map).ToArray()) if (thing is Building || thing is Plant) thing.Destroy();
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil); map.roofGrid.SetRoof(c, null); hotel[c] = true; map.areaManager.Home[c] = true;
        }
        var nativeMap = map.components.Single(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
        Set(nativeMap, "defaultAreaRestriction", hotel); Set(nativeMap, "defaultAreaShopping", shop); Set(nativeMap, "askForSafety", false);
        Set(nativeMap, "guestsCanTakeFoodForFree", false);
        var table = ThingMaker.MakeThing(ThingDefOf.Table2x2c, ThingDefOf.Steel); table.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(table, new IntVec3(46, 0, 45), map);
        var chairCell = GenAdj.CellsAdjacentCardinal(table).First(c => c.InBounds(map) && c.Standable(map));
        var chair = ThingMaker.MakeThing(ThingDefOf.DiningChair, ThingDefOf.Steel); chair.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(chair, chairCell, map, Rot4.East);
        dining[chair.Position] = true; stock[new IntVec3(42, 0, 42)] = true;
        counter = (Building_ServiceCounter)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ServiceCounter"));
        counter.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(counter, new IntVec3(42, 0, 46), map);
        Check(ContentFinder<Texture2D>.Get("Things/Restaurant/ServiceCounter", false) != null && ContentFinder<Texture2D>.Get("UI/Restaurant/Restaurant", false) != null &&
            ContentFinder<Texture2D>.Get("UI/Restaurant/Menu", false) != null, "Original restaurant artwork failed to load");
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        waiter.pather.StopDead(); waiter.Position = new IntVec3(42, 0, 43);
        MealStock(2); guest = MakeGuest(50);
        Check(Service.TryOrder(guest, counter) == null, "Unassigned restaurant sold food");
        counter.serviceArea = dining; counter.foodArea = stock;
        Check(RestaurantMeals.Seat(chair, counter), "Dining fixture has no table beside chair");
        hotel[chair.Position] = false;
        Check(Service.TryOrder(guest, counter) == null, "Restaurant ignored guest area restriction"); hotel[chair.Position] = true;
        counter.open = false; Check(Service.TryOrder(guest, counter) == null, "Closed restaurant accepted an order"); counter.open = true;
        poor = MakeGuest(0); Check(Service.TryOrder(poor, counter) == null, "Unfunded order accepted");
        counter.mealDef = ThingDefOf.MealFine; Check(Service.TryOrder(guest, counter) == null, "Unavailable meal sold"); counter.mealDef = ThingDefOf.MealSimple;
        var mealOnFloor = map.listerThings.ThingsOfDef(ThingDefOf.MealSimple).First(t => t.Spawned);
        Set(nativeMap, "guestsCanTakeFoodForFree", true);
        Check(RestaurantMeals.Quote(counter, mealOnFloor) == 0, "Hospitality free-food option was ignored"); Set(nativeMap, "guestsCanTakeFoodForFree", false);
        Check(!RestaurantMeals.Prepared(DefDatabase<ThingDef>.GetNamed("RawRice")), "Raw ingredients included as prepared meals");
        Check(!RestaurantMeals.Prepared(DefDatabase<ThingDef>.GetNamed("Milk")), "Tiny milk units offered as whole restaurant portions");
        var cancelledGuest = MakeGuest(30, false);
        var cancelled = Service.TryOrder(cancelledGuest, counter); Check(cancelled != null, "No cancellation fixture order");
        var cancellationCoin = cancelledGuest.inventory.innerContainer.First(t => t.def == ThingDefOf.Silver);
        Check(cancelledGuest.inventory.innerContainer.TryTransferToContainer(cancellationCoin, cancelled.GetDirectlyHeldThings(), 4, false) == 4, "Cancellation escrow fixture failed");
        counter.open = false; Service.ProcessOrders(false);
        Check(HotelBillingHooks.Money(cancelledGuest) == 30 && cancelledGuest.inventory.innerContainer.Single(t => t.def == ThingDefOf.Silver).stackCount == 30 &&
            cancelled.GetDirectlyHeldThings().Count == 0 && Service.ForGuest(cancelledGuest) == null && counter.earned == 0,
            "Closing lost or charged pending money");
        cancelledGuest.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
        cancelledGuest.Position = new IntVec3(50, 0, 50);
        var stopped = JobMaker.MakeJob(JobDefOf.Wait); stopped.expiryInterval = 600000; stopped.playerForced = true;
        cancelledGuest.jobs.StartJob(stopped, JobCondition.InterruptForced); counter.open = true;
        guest.needs.food.CurLevelPercentage = 0.35f;
        initialFloor = FloorSilver();
        var order = Service.TryOrder(guest, counter); Check(order != null, "No diner order created: guest=" + RestaurantMeals.Guest(guest) +
            " seat=" + guest.CanReserveAndReach(chair, PathEndMode.OnCell, Danger.Some) + " food=" + mealOnFloor.Spawned); firstPrice = order.price;
        Check(firstPrice > 6 && HotelBillingHooks.Money(guest) == 50, "Order charged before delivery or split-currency fixture invalid");
        Check(Service.TryOrder(poor, counter) == null, "Two guests received the same seat/portion");
        counter.pricePercent = 200; counter.mealDef = ThingDefOf.MealFine;
        Check(order.price == firstPrice && order.mealDef == ThingDefOf.MealSimple, "Accepted order changed with the menu");
        var owned = ThingMaker.MakeThing(ThingDefOf.MealSimple); owned.stackCount = 2; guest.inventory.innerContainer.TryAdd(owned, false);
        // The waiter may accept work only once the customer reaches the seat.
        stageTick = Find.TickManager.TicksGame;
    }
    public override void MapComponentUpdate()
    {
        bool reload = Environment.GetCommandLineArgs().Contains("-tmm-restaurant-reload");
        if (reported || Find.CurrentMap != map || (!reload && !Environment.GetCommandLineArgs().Contains("-tmm-restaurant-probe"))) return;
        try {
            if (reload && stage == 0) {
                Check(saved && counter != null && pendingGuest != null && waiter != null, "Restaurant save references lost");
                Check(savedCarriedMeal != null && waiter.carryTracker.CarriedThing == savedCarriedMeal && waiter.CurJobDef == RestaurantMeals.ServeJob, "In-transit meal or waiter job lost on reload");
                var pending = Service.ForGuest(pendingGuest); Check(pending != null && pending.price == savedPrice, "Pending quote lost on reload");
                Check(pending.GetDirectlyHeldThings().Sum(t => t.stackCount) == 4, "Pending physical cash lost on reload");
                Check(HotelBillingHooks.Money(pendingGuest) == savedMoney && FloorSilver() == savedFloor && counter.earned == savedRevenue, "Load changed money or revenue");
                Service.ProcessOrders(false); Check(HotelBillingHooks.Money(pendingGuest) == savedMoney, "Load charged undelivered order");
                waiter.drafter.Drafted = false; stage = 10; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 10) {
                if (counter.earned == savedRevenue && waiter.CurJobDef != RestaurantMeals.ServeJob && Service.ForWaiter(waiter, counter) != null) StartServing();
                if (counter.earned == savedRevenue && Find.TickManager.TicksGame - stageTick < 2500) return;
                Check(counter.earned == savedRevenue + savedPrice, "Saved order was not completed");
                Check(HotelBillingHooks.Money(pendingGuest) == savedMoney - (savedPrice - 4), "Partial escrow charged twice or ignored");
                Check(FloorSilver() == savedFloor + savedPrice, "Saved sale did not release exact physical payment");
                Service.ProcessOrders(false); Service.ProcessOrders(false);
                Check(!Service.Deliver(waiter, pendingGuest) && counter.earned == savedRevenue + savedPrice, "Completed sale repeated");
                Check(pendingGuest.needs.food.CurLevelPercentage > 0.35f || pendingGuest.CurJobDef == JobDefOf.Ingest, "Delivered saved meal was not eaten");
                Finish(); return;
            }
            if (stage == 0) { Setup(); stage = 1; return; }
            if (stage == 1) {
                if (counter.earned == 0 && waiter.CurJobDef != RestaurantMeals.ServeJob && Service.ForWaiter(waiter, counter) != null) StartServing();
                if ((counter.earned == 0 || guest.needs.food.CurLevelPercentage <= 0.35f) && Find.TickManager.TicksGame - stageTick < 2500) return;
                var current = Service.ForGuest(guest);
                Check(counter.earned == firstPrice, "Waiter did not complete real paid delivery: " + waiter.CurJobDef + ", guest " + guest.CurJobDef +
                    " positions " + guest.Position + "/" + (current == null ? "none" : current.chair.Position.ToString()) +
                    " stock " + (current == null ? "none" : current.food + "/" + current.food.Spawned) + "\n" + string.Join("\n", RestaurantReloadLaunch.JobEnds));
                Check(HotelBillingHooks.Money(guest) == 50 - firstPrice, "Wrong payment across multiple silver stacks");
                Check(FloorSilver() == initialFloor + firstPrice, "Restaurant created or lost silver");
                Check(guest.needs.food.CurLevelPercentage > 0.35f, "Guest never ate delivered food: " + guest.CurJobDef + "\n" + string.Join("\n", RestaurantReloadLaunch.JobEnds));
                Check(guest.inventory.innerContainer.Where(t => t.def == ThingDefOf.MealSimple).Sum(t => t.stackCount) == 2, "Delivery consumed the guest's pre-existing meal stack");
                Check(!Service.Deliver(waiter, guest), "Same meal sold twice");
                counter.pricePercent = 0; counter.mealDef = ThingDefOf.MealSimple; poor.needs.food.CurLevelPercentage = 0.35f;
                foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading) waiter.workSettings.SetPriority(work, work.defName == "TMM_RestaurantService" ? 1 : 0);
                var free = Service.TryOrder(poor, counter); Check(free != null && free.price == 0, "Free meal refused");
                waiter.jobs.EndCurrentJob(JobCondition.InterruptForced);
                stage = 2; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 2) {
                if (poor.needs.food.CurLevelPercentage <= 0.35f && Find.TickManager.TicksGame - stageTick < 2500) return;
                Check(poor.needs.food.CurLevelPercentage > 0.35f, "Free guest did not eat: waiter=" + waiter.CurJobDef + " priority=" +
                    waiter.workSettings.GetPriority(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService")) + " disabled=" +
                    waiter.WorkTypeIsDisabled(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService")) + " guest=" + poor.CurJobDef +
                    " available=" + (Service.ForWaiter(waiter, counter) != null));
                Check(HotelBillingHooks.Money(poor) == 0 && counter.earned == firstPrice && FloorSilver() == initialFloor + firstPrice, "Free meal altered money");
                // Use a farther table so a real carried portion can be saved in transit.
                foreach (var cell in counter.serviceArea.ActiveCells.ToArray()) counter.serviceArea[cell] = false;
                var remoteTable = ThingMaker.MakeThing(ThingDefOf.Table2x2c, ThingDefOf.Steel); remoteTable.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(remoteTable, new IntVec3(49, 0, 49), map);
                var remoteChair = ThingMaker.MakeThing(ThingDefOf.DiningChair, ThingDefOf.Steel); remoteChair.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(remoteChair, new IntVec3(48, 0, 49), map, Rot4.East); counter.serviceArea[remoteChair.Position] = true;
                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                counter.pricePercent = 100; MealStock(2); pendingGuest = MakeGuest(30); pendingGuest.needs.food.CurLevelPercentage = 0.35f;
                var pending = Service.TryOrder(pendingGuest, counter); Check(pending != null, "No pending save fixture");
                // Simulate an interrupted physical payment to verify persisted escrow.
                var coin = pendingGuest.inventory.innerContainer.First(t => t.def == ThingDefOf.Silver);
                Check(pendingGuest.inventory.innerContainer.TryTransferToContainer(coin, pending.GetDirectlyHeldThings(), 4) == 4, "Could not seed partial payment");
                stage = 3; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 3) {
                if (waiter.carryTracker.CarriedThing == null && Find.TickManager.TicksGame - stageTick < 2500) return;
                var pending = Service.ForGuest(pendingGuest);
                Check(pending != null && waiter.CurJobDef == RestaurantMeals.ServeJob && waiter.carryTracker.CarriedThing != null, "Automatic waiter did not carry pending meal");
                savedCarriedMeal = waiter.carryTracker.CarriedThing;
                saved = true; savedPrice = pending.price; savedMoney = HotelBillingHooks.Money(pendingGuest);
                savedFloor = FloorSilver(); savedRevenue = counter.earned;
                GameDataSaveLoader.SaveGame("TMM-Restaurant"); Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Restaurant")), "Restaurant fixture save missing"); Finish();
            }
        } catch (Exception ex) { errors.Add(ex.ToString()); Finish(); }
    }
    private void Finish()
    {
        reported = true;
        string file = Environment.GetCommandLineArgs().Contains("-tmm-restaurant-reload") ? "RestaurantReloadProbe.json" : "RestaurantProbe.json";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, file), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || counter == null || Event.current.type != EventType.Repaint || !Environment.GetCommandLineArgs().Contains("-tmm-restaurant-probe")) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(20, 20, 1100, 700));
        try { int x = 0; foreach (var gizmo in counter.GetGizmos()) { gizmo.GizmoOnGUI(new Vector2(x, 20), 150f, default(GizmoRenderParms)); x += 155; } }
        catch (Exception ex) { error = ex.ToString(); } finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "RestaurantUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"controlsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
