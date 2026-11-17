using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class AcceptanceLaunch
{
    static AcceptanceLaunch()
    {
        var args = Environment.GetCommandLineArgs();
        if (!args.Any(a => a == "-tmm-service-acceptance-probe" || a == "-tmm-service-acceptance-reload")) return;
        var h = new Harmony("tmm.test.service.acceptance");
        h.Patch(AccessTools.PropertyGetter(typeof(MapTemperature), "OutdoorTemp"), prefix: new HarmonyMethod(typeof(AcceptanceLaunch), "Temp"));
        if (args.Contains("-tmm-service-acceptance-reload"))
        {
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-ServiceAcceptance"));
        }
    }
    private static bool Temp(ref float __result) { __result = 21f; return false; }
}

public sealed class VisitorServiceAcceptanceProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building_ServiceCounter counter;
    private Building_Bed bed;
    private Thing chair;
    private Pawn guest, waiter;
    private Faction home;
    private int checks, stage, start, orderId;
    private int initialPurse, baselineMoney, mealPrice, floorBefore, due;
    private bool saved, reported, uiReported, reloadInitialized;
    private readonly List<string> errors = new List<string>();

    public VisitorServiceAcceptanceProbe(Map map) : base(map) { }

    private RestaurantService Service { get { return map.GetComponent<RestaurantService>(); } }
    private GuestDossier Row { get { return guest == null ? null : GuestProfiles.Row(guest); } }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref desk, "vsaDesk");
        Scribe_References.Look(ref counter, "vsaCounter");
        Scribe_References.Look(ref bed, "vsaBed");
        Scribe_References.Look(ref chair, "vsaChair");
        Scribe_References.Look(ref guest, "vsaGuest");
        Scribe_References.Look(ref waiter, "vsaWaiter");
        Scribe_References.Look(ref home, "vsaHome");
        Scribe_Values.Look(ref saved, "vsaSaved");
        Scribe_Values.Look(ref checks, "vsaChecks");
        Scribe_Values.Look(ref stage, "vsaStage");
        Scribe_Values.Look(ref orderId, "vsaOrderId");
        Scribe_Values.Look(ref initialPurse, "vsaInitialPurse");
        Scribe_Values.Look(ref baselineMoney, "vsaBaselineMoney");
        Scribe_Values.Look(ref mealPrice, "vsaMealPrice"); Scribe_Values.Look(ref floorBefore,"vsaFloor"); Scribe_Values.Look(ref due,"vsaDue");
    }

    private int Floor(){return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Sum(t=>t.stackCount);}
    private void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++;
    }

    private static string Q(string s)
    {
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
    }

    private static void Wait(Pawn p)
    {
        var job = JobMaker.MakeJob(JobDefOf.Wait);
        job.expiryInterval = 600000;
        p.jobs.StartJob(job, JobCondition.InterruptForced);
    }

    private Building Build(string name, IntVec3 c)
    {
        var d = DefDatabase<ThingDef>.GetNamed(name);
        var b = (Building)ThingMaker.MakeThing(d, d.MadeFromStuff ? ThingDefOf.Steel : null);
        b.SetFaction(Faction.OfPlayer);
        return (Building)GenSpawn.Spawn(b, c, map);
    }

    private void Room(CellRect rect)
    {
        foreach (var c in rect.EdgeCells)
            Build(c == new IntVec3(rect.CenterCell.x, 0, rect.minZ) ? "Door" : "Wall", c);
        foreach (var c in rect.ContractedBy(1))
            map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
    }

    private Thing Food(ThingDef d, int x, int z, int count = 1)
    {
        var t = ThingMaker.MakeThing(d);
        t.stackCount = count;
        return GenSpawn.Spawn(t, new IntVec3(x, 0, z), map);
    }

    private void CallGiveItems(Pawn p)
    {
        var workerType = AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup");
        Check(workerType != null, "Hospitality.IncidentWorker_VisitorGroup type not found");
        var giveItemsMethod = AccessTools.Method(workerType, "GiveItems");
        Check(giveItemsMethod != null, "Hospitality.IncidentWorker_VisitorGroup.GiveItems method not found");
        giveItemsMethod.Invoke(null, new object[] { new List<Pawn> { p } });
    }

    private void Setup()
    {
        map.fogGrid.ClearAllFog();
        foreach (var p in map.mapPawns.FreeColonistsSpawned) Wait(p);

        foreach (var cell in new CellRect(35, 35, 30, 30))
        {
            foreach (var t in cell.GetThingList(map).ToArray())
            {
                if (t.def.defName == "VoidMonolith") continue;
                if (t is Building || t is Plant || t is Filth)
                {
                    if (t.def.destroyable) t.Destroy(); else t.DeSpawn();
                }
            }
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            map.roofGrid.SetRoof(cell, null);
        }

        Area_Allowed area;
        map.areaManager.TryMakeNewAllowed(out area);
        foreach (var cell in new CellRect(36, 36, 28, 28)) area[cell] = true;

        // 1. Complex Desk
        desk = (Building_ComplexDesk)Build("TMM_ComplexDesk", new IntVec3(40, 0, 38));
        desk.area = area;
        desk.vipChance = 0; // Ordinary guest strictly VIP 0
        desk.roomRate = 30; // 30 silver / day

        // 2. Bedroom with guest bed
        Room(new CellRect(37, 42, 7, 7));
        bed = (Building_Bed)Build("BedGuest", new IntVec3(40, 0, 45));
        var setRentalFeeMethod = AccessTools.Method(bed.GetType(), "SetRentalFee");
        if (setRentalFeeMethod != null) setRentalFeeMethod.Invoke(bed, new object[] { 30 });

        // 3. Restaurant service counter, fridge, table, chair, food
        counter = (Building_ServiceCounter)Build("TMM_ServiceCounter", new IntVec3(46, 0, 45));
        Build("sbz_SingleChestFridge", new IntVec3(46, 0, 43));
        Build("Table2x2c", new IntVec3(50, 0, 46));
        chair = Build("DiningChair", new IntVec3(49, 0, 46));
        Food(ThingDefOf.MealSimple, 46, 43, 5);

        desk.restaurantOpen = true;
        desk.restaurantMenu = new List<ThingDef> { ThingDefOf.MealSimple }; desk.restaurantMenuConfigured=true;
        desk.restaurantPercent = 100;
        desk.ApplyServiceAreas();
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();

        Check(counter.Ready, "Service counter not Ready in complex area");

        // 4. Assigned waiter
        waiter = map.mapPawns.FreeColonistsSpawned.First(p => !p.WorkTypeIsDisabled(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService")));
        foreach (var w in DefDatabase<WorkTypeDef>.AllDefsListForReading) waiter.workSettings.SetPriority(w, 0);
        waiter.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService"), 1);
        desk.SetRole(waiter, ComplexRole.Waiter);
        desk.Employee(waiter).mode = ComplexWorkMode.ComplexOnly;
        waiter.Position = new IntVec3(47, 0, 45);
        waiter.Notify_Teleported();
        for(int hour=0;hour<24;hour++)waiter.timetable.SetAssignment(hour,TimeAssignmentDefOf.Work); Wait(waiter);

        // 5. Visitor pawn generated via native incident chain
        home = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
        guest = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist, home, canGeneratePawnRelations: false, forcedXenotype: XenotypeDefOf.Baseliner,
            fixedBiologicalAge: 30, allowAddictions: false
        ));
        foreach (var trait in guest.story.traits.allTraits.Where(t => t.def == TraitDefOf.Ascetic).ToArray())
            guest.story.traits.RemoveTrait(trait);

        GenSpawn.Spawn(guest, new IntVec3(40, 0, 36), map);
        guest.inventory.innerContainer.ClearAndDestroyContents();

        // Native item generator (multiplier = 1.0)
        CallGiveItems(guest);

        guest.needs.food.CurLevelPercentage=1f; floorBefore=Floor(); initialPurse = HotelBillingHooks.Money(guest);
        Check(initialPurse >= 150 && initialPurse <= 300, "Ordinary guest purse not in 150..300 range: " + initialPurse);
        var row = GuestProfiles.Row(guest);
        Check(row != null, "Dossier row not observed");
        Check(!row.profile.vip, "Guest must be ordinary, not VIP");
        Check(row.profile.tripPrepared, "tripPrepared flag not set after GiveItems");

        // Native CreateLord / Arrive
        var createLordMethod = AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"), "CreateLord");
        Check(createLordMethod != null, "Hospitality CreateLord not found");
        createLordMethod.Invoke(null, new object[] { home, new IntVec3(40, 0, 38), new List<Pawn> { guest }, map, false, false, 600000 });
        guest.GetLord().ReceiveMemo("TravelArrived");
        GuestDossiers.Book.Begin(guest, map, true, false);
        Check(row.Current != null && GuestDossiers.Arrived(guest), "Guest did not register active visit");

        // Let the real guest think tree choose and execute its bed-claim job.
        start = GuestDossiers.Now;
        stage = 1;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
    }

    private void TickFresh()
    {
        if (stage == 0)
        {
            Setup();
            return;
        }

        if (stage == 1)
        {
            // Query the full guest think tree at controlled intervals; execute its chosen bed job.
            if(GuestDossiers.Now-start>1000 && GuestDossiers.Now%250==0 && HotelBillingHooks.Bed(guest)==null){
                var package=guest.thinker.MainThinkNodeRoot.TryIssueJobPackage(guest,default(JobIssueParams));
                var candidate=(Building_Bed)AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),"FindBedFor").Invoke(null,new object[]{guest});
                File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"AcceptanceProgress.log"),GuestDossiers.Now+" job="+guest.CurJob+" tree="+package.Job+" bed="+candidate+" duty="+guest.mindState.duty+Environment.NewLine);
                if(package.Job!=null&&package.Job.def.defName=="ClaimGuestBed")guest.jobs.StartJob(package.Job,JobCondition.InterruptForced);
            }
            // Wait for bed claim completion
            if (HotelBillingHooks.Bed(guest) != bed)
            {
                if (GuestDossiers.Now - start > 5000)
                    throw new Exception("Guest did not claim bed in time");
                return;
            }

            // Verify first 30 silver deducted
            int moneyAfterBed = HotelBillingHooks.Money(guest);
            Check(moneyAfterBed == initialPurse - 30, "Bed claim did not deduct exactly 30 silver: " + moneyAfterBed + " vs " + (initialPurse - 30));

            var stay = map.GetComponent<HotelBilling>().stays.FirstOrDefault(s => s.guest == guest);
            Check(stay != null && stay.bed == bed, "HotelStay not recorded in HotelBilling");
            Check(Row.hotel == 30, "Dossier hotel revenue not 30: " + Row.hotel);
            Check(Row.profile.tripPrepared, "tripPrepared altered during bed claim");

            // Save game checkpoint between bed payment and restaurant order
            Check(Floor()==floorBefore+30,"Bed payment did not place30 physical silver"); due=stay.nextChargeTick; baselineMoney = moneyAfterBed;
            saved = true;
            GameDataSaveLoader.SaveGame("TMM-ServiceAcceptance");
            Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-ServiceAcceptance")), "TMM-ServiceAcceptance save not created");
            Finish(false);
        }
    }

    private void TickReload()
    {
        if(!reloadInitialized){reloadInitialized=true;stage=0;}
        if (stage == 0)
        {
            Check(saved && guest != null && desk != null && bed != null && counter != null && waiter != null, "Saved objects missing on reload");
            Check(HotelBillingHooks.Money(guest) == baselineMoney, "Silver count altered across reload: " + HotelBillingHooks.Money(guest) + " vs " + baselineMoney);
            Check(HotelBillingHooks.Bed(guest) == bed, "Bed assignment lost on reload");

            var stay = map.GetComponent<HotelBilling>().stays.FirstOrDefault(s => s.guest == guest);
            Check(stay != null && stay.bed == bed && stay.nextChargeTick==due, "Hotel stay/deadline lost on reload");
            Check(Row != null && Row.Current != null, "Dossier active visit lost on reload");
            Check(Row.profile.tripPrepared, "tripPrepared lost on reload");
            Check(Row.hotel == 30, "Dossier hotel revenue changed on reload: " + Row.hotel);

            // Verify no repeat grant on reload
            int moneyBefore = HotelBillingHooks.Money(guest);
            GuestTripBudget.PrepareTrip(guest);
            Check(HotelBillingHooks.Money(guest) == moneyBefore, "PrepareTrip minted coins on reload");
            GuestProfiles.Prepare(guest);
            Check(HotelBillingHooks.Money(guest) == moneyBefore, "Prepare minted coins on reload");

            // Make guest hungry to trigger restaurant order
            guest.needs.food.CurLevelPercentage = 0.40f;
            Service.ProcessOrders(true);

            var order = Service.ForGuest(guest);
            Check(order != null && order.chair == chair && order.counter == counter, "Restaurant order not placed");
            orderId = order.id;
            mealPrice = order.price;
            Check(mealPrice > 0, "Meal price not positive: " + mealPrice);
            Check(guest.CurJobDef == RestaurantMeals.WaitJob, "Guest did not receive WaitJob for meal");

            waiter.jobs.EndCurrentJob(JobCondition.InterruptForced); // Normal JobGiver_Work now chooses the paid service.
            start = GuestDossiers.Now;
            stage = 1;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            return;
        }

        if (stage == 1)
        {
            var order = Service.ById(orderId);
            if (order != null && !order.delivered)
            {
                if (GuestDossiers.Now - start > 2500)
                    throw new Exception("Waiter did not deliver meal in time: waiter=" + waiter.CurJob + ", guest=" + guest.CurJob);
                return;
            }

            // Verify delivery & payment deduction from common purse
            int moneyAfterMeal = HotelBillingHooks.Money(guest);
            int expectedMoney = baselineMoney - mealPrice;
            Check(moneyAfterMeal == expectedMoney, "Guest silver not deducted by meal price: " + moneyAfterMeal + " vs " + expectedMoney);
            Check(moneyAfterMeal == initialPurse - 30 - mealPrice, "Single common purse invariant breached: initial=" + initialPurse + ", hotel=30, meal=" + mealPrice);

            // Verify counter earned & dossier restaurant revenue
            Check(counter.earned == mealPrice, "Counter earned does not match meal price: " + counter.earned + " vs " + mealPrice);
            Check(Row.restaurant == mealPrice, "Dossier restaurant revenue does not match meal price: " + Row.restaurant + " vs " + mealPrice);
            Check(Row.hotel == 30, "Hotel revenue corrupted after meal payment: " + Row.hotel);

            // Verify physical silver deposited at counter receipt cell
            var counterCoins = Floor()-floorBefore-30;
            Check(counterCoins == mealPrice, "Physical silver not deposited at counter cell: " + counterCoins + " vs " + mealPrice);

            // Verify guest transitioned to eating meal
            Check(guest.CurJobDef == JobDefOf.Ingest, "Guest did not begin eating delivered meal");

            // Verify paid hotel room remains valid after restaurant sale
            Check(HotelBillingHooks.Bed(guest) == bed, "Bed claim dropped after restaurant sale");
            var stay = map.GetComponent<HotelBilling>().stays.FirstOrDefault(s => s.guest == guest);
            Check(stay != null && stay.bed == bed, "Hotel stay dropped after restaurant sale");

            AccessTools.Field(HotelBillingHooks.Guest(guest).GetType(),"sentAway").SetValue(HotelBillingHooks.Guest(guest),true);
            start=GuestDossiers.Now;stage=2;return;
        }
        if(stage==2){
            if(guest.Spawned){if(GuestDossiers.Now-start>6000)throw new Exception("Native exit timed out: "+guest.CurJob);return;}
            GuestDossiers.Book.Synchronize();
            Check(Row.Current==null,"Native exit did not close visit");
            Check(!Row.profile.tripPrepared,"Native exit did not reset trip budget guard");
            Check(Row.profile.completed==0,"Short test visit fabricated loyal stays");
            Check(Row.hotel==30&&Row.restaurant==mealPrice,"Exit changed financial totals");
            Finish(true);
        }
    }

    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        bool isProbe = args.Contains("-tmm-service-acceptance-probe");
        bool isReload = args.Contains("-tmm-service-acceptance-reload");
        if (reported || Find.CurrentMap != map || (!isProbe && !isReload)) return;

        try
        {
            if (isReload)
            {
                TickReload();
            }
            else
            {
                TickFresh();
            }
        }
        catch (Exception ex)
        {
            errors.Add(ex.ToString());
            Finish(isReload);
        }
    }

    public override void MapComponentOnGUI()
    {
        var args = Environment.GetCommandLineArgs();
        if (uiReported || !reported || desk == null || Event.current.type != EventType.Repaint || !args.Contains("-tmm-service-acceptance-probe")) return;
        uiReported = true;
        string error = null;
        try
        {
            var servicesDialog = new Dialog_ComplexServices(desk) { page = 1 };
            servicesDialog.DoWindowContents(new Rect(10, 10, 740, 720));
            var readinessDialog = new Dialog_ServiceReadiness(desk);
            readinessDialog.DoWindowContents(new Rect(10, 10, 740, 696));
        }
        catch (Exception ex)
        {
            error = ex.ToString();
        }
        string outPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "VisitorServiceAcceptanceUiProbe.json");
        string json = "{\"passed\":" + (error == null ? "true" : "false") + ",\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Q(error)) + "}";
        File.WriteAllText(outPath, json);
    }

    private void Finish(bool reload)
    {
        reported = true;
        string fileName = reload ? "VisitorServiceAcceptanceReloadProbe.json" : "VisitorServiceAcceptanceProbe.json";
        string filePath = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
        string json = "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":" + (errors.Count == 0 ? "[]" : "[" + string.Join(",", errors.Select(Q)) + "]") + "}";
        File.WriteAllText(filePath, json);
        Log.Message("[TMM-Probe] Finished VisitorServiceAcceptanceProbe (reload=" + reload + "). Passed: " + (errors.Count == 0) + ", Checks: " + checks);
    }
}
