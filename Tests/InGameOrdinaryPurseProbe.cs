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
public static class OrdinaryPurseProbeLaunch
{
    static OrdinaryPurseProbeLaunch()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("-tmm-ordinary-purse-reload"))
        {
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-OrdinaryPurse"));
        }
    }
}

public sealed class OrdinaryPurseProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Pawn ordinaryGuest, richGuest, encumberedGuest, vipGuest;
    private string ordinaryId, vipId;
    private int checks, savedTarget, savedAdded, savedMoney;
    private bool saved, reported;
    private readonly List<string> errors = new List<string>();

    public OrdinaryPurseProbe(Map map) : base(map) { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref desk, "ordinaryDesk");
        Scribe_References.Look(ref ordinaryGuest, "ordinaryGuest");
        Scribe_References.Look(ref richGuest, "richGuest");
        Scribe_References.Look(ref encumberedGuest, "encumberedGuest");
        Scribe_References.Look(ref vipGuest, "vipGuest");
        Scribe_Values.Look(ref ordinaryId, "ordinaryId");
        Scribe_Values.Look(ref vipId, "vipId");
        Scribe_Values.Look(ref saved, "ordinarySaved");
        Scribe_Values.Look(ref savedTarget, "savedTarget");
        Scribe_Values.Look(ref savedAdded, "savedAdded");
        Scribe_Values.Look(ref savedMoney, "savedMoney");
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    private static string Escape(string text)
    {
        return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
    }

    private Pawn SpawnPawn(Faction faction, IntVec3 cell, int age = 30)
    {
        var p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist, faction, forceGenerateNewPawn: true, canGeneratePawnRelations: false,
            allowDead: false, allowDowned: false, allowAddictions: false, fixedBiologicalAge: age
        ));
        foreach (var trait in p.story.traits.allTraits.Where(t => t.def == TraitDefOf.Ascetic).ToArray())
            p.story.traits.RemoveTrait(trait);
        GenSpawn.Spawn(p, cell, map);
        p.inventory.innerContainer.ClearAndDestroyContents();
        return p;
    }

    private void AddCoins(Pawn p, int count)
    {
        if (count <= 0) return;
        var t = ThingMaker.MakeThing(ThingDefOf.Silver);
        t.stackCount = count;
        Check(p.inventory.innerContainer.TryAdd(t, false), "AddCoins failed");
    }

    private void CallGiveItems(Pawn p)
    {
        var workerType = AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup");
        Check(workerType != null, "Hospitality.IncidentWorker_VisitorGroup type not found");
        var giveItemsMethod = AccessTools.Method(workerType, "GiveItems");
        Check(giveItemsMethod != null, "Hospitality.IncidentWorker_VisitorGroup.GiveItems method not found");
        giveItemsMethod.Invoke(null, new object[] { new List<Pawn> { p } });
    }

    private void Arrive(Pawn pawn)
    {
        var lord = pawn.GetLord();
        if (lord != null) lord.Notify_PawnLost(pawn, PawnLostCondition.Undefined);
        var createLordMethod = AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"), "CreateLord");
        Check(createLordMethod != null, "Hospitality.IncidentWorker_VisitorGroup.CreateLord method not found");
        createLordMethod.Invoke(null, new object[] { pawn.Faction, new IntVec3(40, 0, 40), new List<Pawn> { pawn }, map, false, false, 600000 });
        pawn.GetLord().ReceiveMemo("TravelArrived");
        GuestDossiers.Book.Begin(pawn, map, true, false);
    }

    private void Setup()
    {
        map.fogGrid.ClearAllFog();
        foreach (var c in new CellRect(30, 30, 30, 30))
        {
            foreach (var t in c.GetThingList(map).ToArray())
            {
                if ((t is Building || t is Plant) && t.def.defName != "VoidMonolith")
                {
                    if (t.def.destroyable) t.Destroy(); else t.DeSpawn();
                }
            }
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil);
            map.roofGrid.SetRoof(c, null);
        }

        // --- OP-01 Direct Production Method Arithmetic Verification ---
        Check(GuestTripBudget.CalculateTarget(200, 0f) == 0, "OP-01: multiplier 0 must produce 0");
        Check(GuestTripBudget.CalculateTarget(200, 0.5f) == 100, "OP-01: multiplier 0.5 must produce 100");
        Check(GuestTripBudget.CalculateTarget(200, 1.0f) == 200, "OP-01: multiplier 1.0 must produce 200");
        Check(GuestTripBudget.CalculateTarget(200, 2.0f) == 400, "OP-01: multiplier 2.0 must produce 400");
        Check(GuestTripBudget.CalculateTarget(200, float.MaxValue) == 10000, "OP-01: float.MaxValue must clamp to 10000 without overflow");
        Check(GuestTripBudget.CalculateTarget(200, float.NaN) == 0, "OP-01: NaN must produce 0");
        Check(GuestTripBudget.CalculateTarget(200, float.PositiveInfinity) == 0, "OP-01: PositiveInfinity must produce 0");
        Check(GuestTripBudget.CalculateTarget(200, float.NegativeInfinity) == 0, "OP-01: NegativeInfinity must produce 0");
        Check(GuestTripBudget.CalculateTarget(200, -1.0f) == 0, "OP-01: negative multiplier must produce 0");
        Check(GuestTripBudget.CalculateTarget(200, 0.005f) == 1, "OP-01: tiny positive 0.005 must round to 1");

        // 1. Create desk
        desk = (Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk"));
        desk.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(desk, new IntVec3(40, 0, 40), map);
        Area_Allowed area;
        map.areaManager.TryMakeNewAllowed(out area);
        foreach (var c in new CellRect(30, 30, 30, 30)) area[c] = true;
        desk.area = area;
        desk.vipChance = 0; // Ensure non-VIP for ordinary tests
        desk.ApplyServiceAreas();

        var faction = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));

        // --- Vector 1: Ordinary Tourist zero/small wallet + GiveItems ---
        ordinaryGuest = SpawnPawn(faction, new IntVec3(42, 0, 40));
        ordinaryId = ordinaryGuest.GetUniqueLoadID();

        CallGiveItems(ordinaryGuest);

        var ordRow = GuestProfiles.Row(ordinaryGuest);
        Check(ordRow != null, "Dossier row not observed for ordinary guest");
        Check(ordRow.profile.tripPrepared, "tripPrepared flag not set after GiveItems");
        Check(!ordRow.profile.vip, "Ordinary guest should not be VIP");
        Check(ordRow.profile.tripTarget >= 150 && ordRow.profile.tripTarget <= 300, "Ordinary tripTarget not in 150-300 range: " + ordRow.profile.tripTarget);
        Check(ordRow.profile.tripAdded > 0, "tripAdded must be positive for empty/native guest");
        Check(HotelBillingHooks.Money(ordinaryGuest) >= ordRow.profile.tripTarget, "Inventory money does not reach tripTarget");
        Check(MassUtility.FreeSpace(ordinaryGuest) >= 2.99f, "Mass reserve breached");

        // Register ordinary guest as genuinely arrived for lifecycle & save/load tests
        Arrive(ordinaryGuest);
        Check(ordRow.Current != null && GuestDossiers.Arrived(ordinaryGuest), "Ordinary guest did not arrive with active visit");

        // --- Vector 2: Idempotence of repeated GiveItems & Prepare ---
        int moneyAfterArrival = HotelBillingHooks.Money(ordinaryGuest);
        int addedAfterFirst = ordRow.profile.tripAdded;
        int targetAfterFirst = ordRow.profile.tripTarget;

        // Direct PrepareTrip must mint 0 coins
        GuestTripBudget.PrepareTrip(ordinaryGuest);
        Check(HotelBillingHooks.Money(ordinaryGuest) == moneyAfterArrival, "Direct PrepareTrip minted coins");

        // Direct Prepare must mint 0 coins
        GuestProfiles.Prepare(ordinaryGuest);
        Check(HotelBillingHooks.Money(ordinaryGuest) == moneyAfterArrival, "Direct Prepare minted coins");

        // Repeated GiveItems must not change our tripAdded or tripTarget
        CallGiveItems(ordinaryGuest);
        Check(ordRow.profile.tripTarget == targetAfterFirst, "Repeated GiveItems changed tripTarget");
        Check(ordRow.profile.tripAdded == addedAfterFirst, "Repeated GiveItems changed tripAdded");

        // --- Vector 3: Manual Prepare without GiveItems ---
        var manualPawn = SpawnPawn(faction, new IntVec3(43, 0, 40));
        GuestProfiles.Prepare(manualPawn);
        var manualRow = GuestProfiles.Row(manualPawn);
        Check(manualRow != null && manualRow.profile.arrivalPrepared, "Manual Prepare did not set arrivalPrepared");
        Check(!manualRow.profile.tripPrepared, "Manual Prepare set tripPrepared");
        Check(HotelBillingHooks.Money(manualPawn) == 0, "Manual Prepare minted coins for ordinary guest");
        Check(manualRow.profile.tripAdded == 0, "Manual Prepare set tripAdded");
        manualPawn.DeSpawn();
        GuestDossiers.Book.Synchronize();

        // --- Vector 4: Sufficient wallet (500 silver) ---
        richGuest = SpawnPawn(faction, new IntVec3(44, 0, 40));
        AddCoins(richGuest, 500);
        CallGiveItems(richGuest);
        var richRow = GuestProfiles.Row(richGuest);
        Check(richRow != null && richRow.profile.tripPrepared, "Rich guest tripPrepared not set");
        Check(richRow.profile.tripAdded == 0, "Rich guest received extra coins from our top-up");
        Check(HotelBillingHooks.Money(richGuest) >= 500, "Rich guest existing coins destroyed or altered");
        richGuest.DeSpawn(); richGuest = null;
        GuestDossiers.Book.Synchronize();

        // --- Vector 5: Multiplier = 0 (Hospitality silverMultiplier = 0) ---
        var settingsType = AccessTools.TypeByName("Hospitality.ModSettings_Hospitality");
        var multField = settingsType == null ? null : AccessTools.Field(settingsType, "silverMultiplier");
        float origMult = 1f;
        if (multField != null) origMult = (float)multField.GetValue(null);
        try
        {
            if (multField != null) multField.SetValue(null, 0f);
            var zeroPawn = SpawnPawn(faction, new IntVec3(45, 0, 40));
            CallGiveItems(zeroPawn);
            var zeroRow = GuestProfiles.Row(zeroPawn);
            Check(zeroRow.profile.tripTarget == 0, "Multiplier 0 did not produce target 0: " + zeroRow.profile.tripTarget);
            Check(zeroRow.profile.tripAdded == 0, "Multiplier 0 added our coins");
            zeroPawn.DeSpawn();
            GuestDossiers.Book.Synchronize();
        }
        finally
        {
            if (multField != null) multField.SetValue(null, origMult);
        }

        // --- Vector 6: Encumbered mass capacity (< 3kg free space) ---
        encumberedGuest = SpawnPawn(faction, new IntVec3(46, 0, 40));
        float freeSpace = MassUtility.FreeSpace(encumberedGuest);
        if (freeSpace > 2.5f)
        {
            var heavy = ThingMaker.MakeThing(ThingDefOf.Steel);
            heavy.stackCount = (int)Math.Max(1, Math.Floor((freeSpace - 2.0f) / ThingDefOf.Steel.GetStatValueAbstract(StatDefOf.Mass)));
            encumberedGuest.inventory.innerContainer.TryAdd(heavy, false);
        }
        Check(MassUtility.FreeSpace(encumberedGuest) < 3.0f, "Failed to encumber guest below 3kg reserve");
        CallGiveItems(encumberedGuest);
        var encRow = GuestProfiles.Row(encumberedGuest);
        Check(encRow.profile.tripPrepared, "Encumbered guest tripPrepared not set");
        Check(encRow.profile.tripAdded == 0, "Encumbered guest received coins breaching reserve");
        encumberedGuest.DeSpawn(); encumberedGuest = null;
        GuestDossiers.Book.Synchronize();

        // --- Vector 7: VIP preservation (1500-3000 silver) ---
        desk.vipChance = 100;
        vipGuest = SpawnPawn(faction, new IntVec3(47, 0, 40));
        vipId = vipGuest.GetUniqueLoadID();
        CallGiveItems(vipGuest);
        var vipRow = GuestProfiles.Row(vipGuest);
        Check(vipRow != null && vipRow.profile.vip, "VIP guest not identified as VIP");
        Check(vipRow.profile.grantAdded > 0, "VIP grantAdded is 0");
        Check(vipRow.profile.tripAdded == 0, "VIP received ordinary trip budget grant");

        // --- Vector 8: Real Lifecycle & Aborted Approach Fixture ---
        desk.vipChance = 0;
        var abortedPawn = SpawnPawn(faction, new IntVec3(48, 0, 40));
        CallGiveItems(abortedPawn);
        var abRow = GuestProfiles.Row(abortedPawn);
        Check(abRow.profile.tripPrepared && abRow.Current == null, "Aborted approach pawn not prepared");
        abortedPawn.DeSpawn();
        GuestDossiers.Book.Synchronize();
        Check(!abRow.profile.tripPrepared, "Despawn without arrival did not clear tripPrepared");

        // --- Vector 9: Blocked, Clinic, Ineligible Fixtures ---
        Check(!GuestTripBudget.Eligible(null), "Null pawn eligible");

        // Blocked pawn
        var blockedPawn = SpawnPawn(faction, new IntVec3(49, 0, 40));
        var blkRow = GuestDossiers.Book.Observe(blockedPawn);
        GuestDossiers.Book.SetBlocked(blkRow, true, "test blacklisted guest");
        Check(!GuestTripBudget.Eligible(blockedPawn), "Blocked pawn eligible");
        GuestTripBudget.PrepareTrip(blockedPawn);
        Check(blkRow.profile.tripAdded == 0 && !blkRow.profile.tripPrepared, "Blocked pawn prepared");
        blockedPawn.DeSpawn();
        GuestDossiers.Book.Synchronize();

        // Clinic patient fixture
        var clinicPawn = SpawnPawn(faction, new IntVec3(49, 0, 41));
        var pDataType = AccessTools.TypeByName("Hospital.PatientData");
        var pKindType = AccessTools.TypeByName("Hospital.PatientType");
        if (pDataType != null && pKindType != null)
        {
            var pData = Activator.CreateInstance(pDataType, new object[] { GuestDossiers.Now, clinicPawn.MarketValue, 0.5f, Enum.ToObject(pKindType, 1) });
            Clinic.Patients(map)[clinicPawn] = pData;
            Check(!GuestTripBudget.Eligible(clinicPawn), "Clinic patient was considered eligible");
            Clinic.Patients(map).Remove(clinicPawn);
        }
        clinicPawn.DeSpawn();
        GuestDossiers.Book.Synchronize();

        // Slave, prisoner, colonist
        var slave = SpawnPawn(faction, new IntVec3(49, 0, 42));
        slave.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
        Check(!GuestTripBudget.Eligible(slave), "Slave pawn eligible");
        slave.DeSpawn();

        var prisoner = SpawnPawn(faction, new IntVec3(49, 0, 43));
        prisoner.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
        Check(!GuestTripBudget.Eligible(prisoner), "Prisoner pawn eligible");
        prisoner.DeSpawn();

        var colonist = SpawnPawn(Faction.OfPlayer, new IntVec3(49, 0, 44));
        Check(!GuestTripBudget.Eligible(colonist), "Colonist eligible");
        colonist.DeSpawn();

        // Prepare for save/load verification
        savedTarget = ordRow.profile.tripTarget;
        savedAdded = ordRow.profile.tripAdded;
        savedMoney = HotelBillingHooks.Money(ordinaryGuest);
        saved = true;

        GameDataSaveLoader.SaveGame("TMM-OrdinaryPurse");
        Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-OrdinaryPurse")), "Save game not created");
        Finish(false);
    }

    private void RunReloadChecks()
    {
        Check(saved, "Probe was not saved before reload");
        Check(ordinaryGuest != null && desk != null, "Saved ordinary guest or desk missing");
        var ordRow = GuestProfiles.Row(ordinaryGuest);
        Check(ordRow != null, "Saved dossier row missing on reload");
        Check(ordRow.Current != null, "Active registered visit lost across reload");
        Check(ordRow.profile.tripPrepared, "tripPrepared lost on reload");
        Check(ordRow.profile.tripTarget == savedTarget, "tripTarget changed across reload: " + ordRow.profile.tripTarget + " vs " + savedTarget);
        Check(ordRow.profile.tripAdded == savedAdded, "tripAdded changed across reload: " + ordRow.profile.tripAdded + " vs " + savedAdded);
        Check(HotelBillingHooks.Money(ordinaryGuest) == savedMoney, "Money changed across reload");

        // Repeat GiveItems / Prepare on reload must still be idempotent
        int moneyBefore = HotelBillingHooks.Money(ordinaryGuest);
        GuestTripBudget.PrepareTrip(ordinaryGuest);
        Check(HotelBillingHooks.Money(ordinaryGuest) == moneyBefore, "Reload direct PrepareTrip added coins");
        GuestProfiles.Prepare(ordinaryGuest);
        Check(HotelBillingHooks.Money(ordinaryGuest) == moneyBefore, "Reload direct Prepare added coins");

        // Check VIP preservation on reload
        Check(vipGuest != null, "VIP guest missing on reload");
        var vipRow = GuestProfiles.Row(vipGuest);
        Check(vipRow != null && vipRow.profile.vip, "VIP identity lost on reload");
        Check(vipRow.profile.tripAdded == 0, "VIP acquired ordinary tripAdded on reload");

        // --- Real Native Departure & New Natural Trip on Reload ---
        var lord = ordinaryGuest.GetLord();
        if (lord != null) lord.Notify_PawnLost(ordinaryGuest, PawnLostCondition.Undefined);
        ordinaryGuest.DeSpawn();
        GuestDossiers.Book.Synchronize();
        Check(ordRow.Current == null, "Real despawn did not end active visit");
        Check(!ordRow.profile.tripPrepared, "Real departure via Synchronize did not reset tripPrepared");
        Find.WorldPawns.PassToWorld(ordinaryGuest, PawnDiscardDecideMode.KeepForever);

        // Spawn for genuine new natural trip
        Find.WorldPawns.RemovePawn(ordinaryGuest);
        GenSpawn.Spawn(ordinaryGuest, new IntVec3(42, 0, 40), map);
        CallGiveItems(ordinaryGuest);
        Check(ordRow.profile.tripPrepared, "Second natural trip did not set tripPrepared");
        Check(ordRow.profile.tripTarget >= 150 && ordRow.profile.tripTarget <= 300, "Second natural trip target invalid: " + ordRow.profile.tripTarget);
        Check(HotelBillingHooks.Money(ordinaryGuest) >= ordRow.profile.tripTarget, "Second natural trip did not top up purse");

        ordinaryGuest.DeSpawn();
        GuestDossiers.Book.Synchronize();
        Find.WorldPawns.PassToWorld(ordinaryGuest, PawnDiscardDecideMode.Discard);

        Finish(true);
    }

    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        bool reload = args.Contains("-tmm-ordinary-purse-reload");
        if (reported || Find.CurrentMap != map || (!reload && !args.Contains("-tmm-ordinary-purse-probe"))) return;

        try
        {
            if (reload)
            {
                RunReloadChecks();
                return;
            }

            Setup();
            return;
        }
        catch (Exception ex)
        {
            errors.Add(ex.ToString());
            Finish(reload);
        }
    }

    private bool uiDone;
    public override void MapComponentOnGUI()
    {
        if(uiDone || !reported || desk==null || Event.current.type!=EventType.Repaint || !Environment.GetCommandLineArgs().Contains("-tmm-ordinary-purse-probe"))return;
        uiDone=true;string failure=null;
        try {new Dialog_ComplexServices(desk){page=1}.DoWindowContents(new Rect(0,0,744,726));}
        catch(Exception e){failure=e.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"OrdinaryPurseUiProbe.json"),"{\"passed\":"+(failure==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(failure==null?"null":Escape(failure))+"}");
    }
    private void Finish(bool reload)
    {
        reported = true;
        string fileName = reload ? "OrdinaryPurseReloadProbe.json" : "OrdinaryPurseProbe.json";
        string filePath = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
        string json = "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}";
        File.WriteAllText(filePath, json);
        Log.Message("[TMM-Probe] Finished OrdinaryPurseProbe (reload=" + reload + "). Passed: " + (errors.Count == 0) + ", Checks: " + checks);
    }
}
