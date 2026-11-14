// Installed only in a disposable test profile; no user profile is changed.
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
public static class HotelReloadLaunch
{
    static HotelReloadLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-hotel-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Hotel"));
    }
}
public sealed class HospitalityProbe : MapComponent
{
    private bool reported, uiReported, saved;
    private int stage, checks, stageTick, initialSilver, savedMoney, savedNextDue, savedFloor;
    private readonly List<string> errors = new List<string>();
    private Mod hotel;
    private Building_Bed bed, spareBed;
    private Pawn guest, poorGuest;
    private Area hotelArea, shoppingArea;
    private Building_ComplexDesk reception;
    private object nativeMap;
    public HospitalityProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_Values.Look(ref saved, "hotelSaved"); Scribe_References.Look(ref reception, "complexReception");
        Scribe_References.Look(ref bed, "hotelBed"); Scribe_References.Look(ref guest, "hotelGuest");
        Scribe_Values.Look(ref savedMoney, "hotelMoney"); Scribe_Values.Look(ref savedNextDue, "hotelNextDue"); Scribe_Values.Look(ref savedFloor, "hotelFloor");
    }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static string Escape(string value) { return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static object Call(object target, string method, params object[] args) { return AccessTools.Method(target.GetType(), method).Invoke(target, args); }
    private static void Set(object target, string field, object value) { AccessTools.Field(target.GetType(), field).SetValue(target, value); }
    private int FloorSilver() { return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t => t.Spawned).Sum(t => t.stackCount); }
    private HotelBilling Ledger { get { return map.GetComponent<HotelBilling>(); } }
    private HotelStay Stay(Pawn pawn) { return Ledger.stays.Single(s => s.guest == pawn); }
    private Building_Bed MakeBed(IntVec3 cell, int fee)
    {
        var result = (Building_Bed)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("BedGuest"), ThingDefOf.Steel);
        result.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(result, cell, map, Rot4.North); Call(result, "SetRentalFee", fee); return result;
    }
    private Pawn MakeGuest(IntVec3 cell, int money)
    {
        var faction = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, faction); GenSpawn.Spawn(pawn, cell, map);
        pawn.inventory.innerContainer.ClearAndDestroyContents();
        Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver); silver.stackCount = money; pawn.inventory.innerContainer.TryAdd(silver);
        if (pawn.needs.food != null) pawn.needs.food.CurLevelPercentage = 1f;
        if (pawn.needs.rest != null) pawn.needs.rest.CurLevelPercentage = 0.5f;
        if (pawn.needs.joy != null) pawn.needs.joy.CurLevelPercentage = 1f;
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"), "CreateLord").Invoke(null,
            new object[] { faction, new IntVec3(44, 0, 44), new List<Pawn> { pawn }, map, false, false, 600000 });
        pawn.GetLord().ReceiveMemo("TravelArrived");
        var comp = HotelBillingHooks.Guest(pawn); Set(comp, "lastBedCheckTick", Find.TickManager.TicksGame - 10000);
        return pawn;
    }
    private void StartCheckIn(Pawn pawn)
    {
        var giver = Activator.CreateInstance(AccessTools.TypeByName("Hospitality.JobGiver_ClaimBed"));
        var job = (Job)Call(giver, "TryGiveJob", pawn); Check(job != null, "Native guest AI did not offer a check-in job");
        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
    }
    private void Setup()
    {
        hotel = LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "Hospitality.Mod_Hospitality");
        var assembly = hotel.GetType().Assembly;
        Check(hotel.SettingsCategory() == "TMM.Hotel.Settings".Translate().ToString(), "Hotel settings category was not translated");
        Check(Harmony.GetAllPatchedMethods().Count(m => m.DeclaringType.Assembly == assembly &&
            Harmony.GetPatchInfo(m).Transpilers.Any(p => p.owner == "turbomegamod.runtime.localization")) >= 2,
            "Hotel hardcoded UI translation patches missing");
        Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "Hospitality") == 1, "Duplicate hotel assembly");
        Check(assembly.Location.Replace('\\', '/').Contains("Components/Hospitality/1.7/Assemblies/"), "Wrong hotel version/path");
        Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "0MultiplayerAPI") == 1, "Duplicate shared Multiplayer API");
        nativeMap = map.components.Single(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
        Check(nativeMap != null && HotelBillingHooks.Enabled, "Missing hotel map component or daily billing hooks");
        foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
        {
            var wait = JobMaker.MakeJob(JobDefOf.Wait); wait.expiryInterval = 600000; colonist.jobs.StartJob(wait, JobCondition.InterruptForced);
        }
        var first = map.mapPawns.FreeColonistsSpawned.First();
        Check(first.AllComps.Count(c => c.GetType().FullName == "Hospitality.CompGuest") == 1, "Guest pawn component missing/duplicated");
        Check(DefDatabase<MainButtonDef>.GetNamed("Guests") != null, "Guests tab missing");
        Check(DefDatabase<ThingDef>.GetNamed("BedGuest").thingClass.FullName == "Hospitality.Building_GuestBed", "Guest beds not generated");
        Area_Allowed allowed, shop; map.areaManager.TryMakeNewAllowed(out allowed); map.areaManager.TryMakeNewAllowed(out shop);
        hotelArea = allowed; shoppingArea = shop;
        var room = new CellRect(40, 40, 10, 10);
        foreach (var c in room)
        {
            foreach (var thing in c.GetThingList(map).ToArray()) if (thing is Building || thing is Plant) thing.Destroy();
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil); map.roofGrid.SetRoof(c, null); hotelArea[c] = true; map.areaManager.Home[c] = true;
        }
        foreach (var c in room.EdgeCells)
        {
            Thing wall = ThingMaker.MakeThing(c == new IntVec3(44, 0, 40) ? ThingDefOf.Door : ThingDefOf.Wall, ThingDefOf.Steel);
            wall.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(wall, c, map);
        }
        foreach (var c in room.ContractedBy(1)) map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        Set(nativeMap, "defaultAreaRestriction", hotelArea); Set(nativeMap, "defaultAreaShopping", shoppingArea); Set(nativeMap, "askForSafety", false);
        var convertCell = new IntVec3(42, 0, 42);
        var ordinary = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.Steel);
        ordinary.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(ordinary, convertCell, map);
        ordinary.HitPoints = ordinary.MaxHitPoints - 10;
        int oldHitPoints = ordinary.HitPoints;
        var swap = AccessTools.Method(AccessTools.TypeByName("Hospitality.Building_GuestBed"), "Swap");
        swap.Invoke(null, new object[] { ordinary });
        var converted = convertCell.GetThingList(map).OfType<Building_Bed>().Single();
        Check(converted.def.defName == "BedGuest" && converted.HitPoints == oldHitPoints, "Ordinary bed conversion failed or repaired the bed");
        swap.Invoke(null, new object[] { converted });
        var restored = convertCell.GetThingList(map).OfType<Building_Bed>().Single();
        Check(restored.def == ThingDefOf.Bed && restored.HitPoints == oldHitPoints, "Returning a guest bed to normal failed");
        restored.Destroy();
        reception = (Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk")); reception.automaticServices = false;
        reception.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(reception, new IntVec3(42, 0, 42), map); reception.area = hotelArea;
        bed = MakeBed(new IntVec3(45, 0, 45), 20); bed.Position.GetRoom(map).Temperature = 21f;
        Check(HotelBillingHooks.Fee(bed) == 20, "Room rate did not change");
        Check(bed.GetInspectString().Contains("20"), "Daily rate not shown in bed inspect");
        Check(bed.GetGizmos().Any(), "Guest bed controls missing");
        initialSilver = FloorSilver(); guest = MakeGuest(new IntVec3(44, 0, 44), 100);
        hotelArea[bed.Position] = false;
        var bedFinder = AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.BedUtility"), "FindBedFor");
        Check(bedFinder.Invoke(null, new object[] { guest }) == null, "Guest selected a bed outside its allowed area");
        hotelArea[bed.Position] = true;
        Check(bedFinder.Invoke(null, new object[] { guest }) == bed, "Guest did not select the bed inside its allowed area");
        StartCheckIn(guest);
    }
    public override void MapComponentUpdate()
    {
        bool reload = Environment.GetCommandLineArgs().Contains("-tmm-hotel-reload");
        if (reported || Find.CurrentMap != map || (!reload && !Environment.GetCommandLineArgs().Contains("-tmm-hospitality-probe"))) return;
        try
        {
            if (reload)
            {
                Check(saved && guest != null && bed != null, "Saved hotel fixture missing");
                Check(HotelBillingHooks.Bed(guest) == bed, "Native guest bed ownership did not restore");
                Check(HotelBillingHooks.Money(guest) == savedMoney && FloorSilver() == savedFloor, "Save/load created or removed money");
                Check(reception != null && reception.hotelIncome == 70, "Reception income did not survive save/load");
                Check(Stay(guest).nextChargeTick == savedNextDue, "Saved billing deadline changed");
                Ledger.ProcessPayments(); Ledger.ProcessPayments();
                Check(HotelBillingHooks.Money(guest) == savedMoney, "Reload charged the paid day again");
                Stay(guest).nextChargeTick = Find.TickManager.TicksGame; Ledger.ProcessPayments();
                Check(HotelBillingHooks.Money(guest) == savedMoney - 20 && FloorSilver() == savedFloor + 20, "Next day after reload did not transfer real silver");
                Ledger.ProcessPayments(); Check(HotelBillingHooks.Money(guest) == savedMoney - 20, "Post-load billing repeated");
                Check(reception.hotelIncome == 90, "Reception missed post-load room payment or counted it twice");
                bed.Destroy(); Ledger.ProcessPayments(); Check(!Ledger.stays.Any(s => s.guest == guest), "Destroyed bed kept a billing account");
                Finish(); return;
            }
            if (stage == 0) { Setup(); stage = 1; stageTick = Find.TickManager.TicksGame; return; }
            if (stage == 1)
            {
                if (HotelBillingHooks.Bed(guest) != bed && Find.TickManager.TicksGame - stageTick < 2000) return;
                Check(HotelBillingHooks.Bed(guest) == bed, "Guest did not complete native check-in: " + guest.CurJobDef);
                Check(HotelBillingHooks.Money(guest) == 80 && FloorSilver() == initialSilver + 20, "First day did not transfer exactly 20 silver");
                Check(reception.hotelIncome == 20, "Reception did not record real check-in silver: income=" + reception.hotelIncome + " active=" + reception.Active + " spawned=" + reception.Spawned + " included=" + reception.area[bed.Position]);
                Check(Stay(guest).nextChargeTick > Find.TickManager.TicksGame + 59000, "First day deadline is not one game day");
                Ledger.ProcessPayments(); Check(HotelBillingHooks.Money(guest) == 80, "Paid day charged again");
                Call(bed, "SetRentalFee", 30); Ledger.ProcessPayments(); Check(HotelBillingHooks.Money(guest) == 80, "Price change retroactively charged a paid day");
                Stay(guest).nextChargeTick = Find.TickManager.TicksGame; int due = Stay(guest).nextChargeTick; Ledger.ProcessPayments();
                Check(HotelBillingHooks.Money(guest) == 50 && FloorSilver() == initialSilver + 50, "Renewal did not transfer the new 30-silver rate");
                Check(reception.hotelIncome == 50, "Reception did not record renewed room rate");
                Check(Stay(guest).nextChargeTick == due + HotelBilling.DayTicks, "Daily period is not 60000 ticks");
                Ledger.ProcessPayments(); Check(HotelBillingHooks.Money(guest) == 50, "Renewal billed twice"); Call(bed, "SetRentalFee", 20);
                spareBed = MakeBed(new IntVec3(47, 0, 45), 20); poorGuest = MakeGuest(new IntVec3(46, 0, 43), 25); StartCheckIn(poorGuest);
                stage = 2; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 2)
            {
                if (HotelBillingHooks.Bed(poorGuest) != spareBed && Find.TickManager.TicksGame - stageTick < 2000) return;
                Check(HotelBillingHooks.Bed(poorGuest) == spareBed && HotelBillingHooks.Money(poorGuest) == 5, "Second guest did not pay its first day");
                Stay(poorGuest).nextChargeTick = Find.TickManager.TicksGame; Ledger.ProcessPayments();
                Check(HotelBillingHooks.Bed(poorGuest) == null && !Ledger.stays.Any(s => s.guest == poorGuest), "Unaffordable renewal did not release the bed");
                Check(HotelBillingHooks.Money(poorGuest) == 5 && FloorSilver() == initialSilver + 70, "Failed renewal took money");
                Check(reception.hotelIncome == 70, "Failed renewal invented reception revenue");
                Call(spareBed, "SetRentalFee", 0); Set(HotelBillingHooks.Guest(poorGuest), "lastBedCheckTick", Find.TickManager.TicksGame - 10000); StartCheckIn(poorGuest);
                stage = 3; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 3)
            {
                if (HotelBillingHooks.Bed(poorGuest) != spareBed && Find.TickManager.TicksGame - stageTick < 2000) return;
                Check(HotelBillingHooks.Bed(poorGuest) == spareBed, "Guest could not claim a free bed");
                Stay(poorGuest).nextChargeTick = Find.TickManager.TicksGame; Ledger.ProcessPayments();
                Check(HotelBillingHooks.Bed(poorGuest) == spareBed && HotelBillingHooks.Money(poorGuest) == 5, "Free bed charged money or expelled its guest");
                Check(reception.hotelIncome == 70, "Free room invented reception revenue");
                Set(HotelBillingHooks.Guest(poorGuest), "sentAway", true); stage = 4; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 4)
            {
                if (poorGuest.Spawned && Find.TickManager.TicksGame - stageTick < 4000) return;
                Check(!poorGuest.Spawned, "Sent-away guest did not leave the map: job=" + poorGuest.CurJob + " position=" + poorGuest.Position + " downed=" + poorGuest.Downed + " mental=" + poorGuest.InMentalState + " lord=" + (poorGuest.GetLord() == null ? "none" : poorGuest.GetLord().CurLordToil.GetType().FullName));
                Ledger.ProcessPayments(); Check(!Ledger.stays.Any(s => s.guest == poorGuest), "Departed guest kept a billing account");
                Check(HotelBillingHooks.Money(guest) == 50 && FloorSilver() == initialSilver + 70, "Departure changed another guest's payment");
                saved = true; savedMoney = HotelBillingHooks.Money(guest); savedNextDue = Stay(guest).nextChargeTick; savedFloor = FloorSilver();
                GameDataSaveLoader.SaveGame("TMM-Hotel"); Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Hotel")), "Hotel save not written"); Finish();
            }
        }
        catch (Exception exception) { errors.Add(exception.ToString()); Finish(); }
    }
    private void Finish()
    {
        reported = true;
        string file = Environment.GetCommandLineArgs().Contains("-tmm-hotel-reload") ? "HospitalityReloadProbe.json" : "HospitalityProbe.json";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, file), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || Event.current.type != EventType.Repaint || !Environment.GetCommandLineArgs().Contains("-tmm-hospitality-probe")) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(20, 20, 1100, 700));
        try
        {
            if (hotel == null || bed == null) throw new Exception("Hotel setup failed");
            hotel.DoSettingsWindowContents(new Rect(0, 0, 1050, 650));
            Find.Selector.ClearSelection(); Find.Selector.Select(bed);
            foreach (var gizmo in bed.GetGizmos().Where(g => g.GetType().FullName == "Hospitality.Gizmo_GuestBed"))
                gizmo.GizmoOnGUI(new Vector2(10, 20), 350f, default(GizmoRenderParms));
        }
        catch (Exception exception) { error = exception.ToString(); }
        finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HospitalityUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsAndBedControlsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
