using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public static class FriendlyVisitLaunch
{
    static FriendlyVisitLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-friendly-visit-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-FriendlyVisit"));
    }
}
public sealed class FriendlyVisitProbe : MapComponent
{
    private static FriendlyVisitProbe active;
    private static Map captureMap;
    private static int frames;
    private Settlement settlement;
    private Pawn first, second;
    private Thing cash;
    private bool saved, done, initialized;
    private int checks, buildingCount, relation, readyTick;
    private Thing ownedSilver, ownedShells, ownDropped;
    private Pawn truck;
    private int ownedCount, silverCount, shellCount, stage, refusals;
    private readonly List<string> facts = new List<string>();
    public FriendlyVisitProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        Scribe_References.Look(ref settlement, "fvSettlement");
        Scribe_References.Look(ref first, "fvFirst");
        Scribe_References.Look(ref second, "fvSecond");
        Scribe_References.Look(ref cash, "fvCash");
        Scribe_Values.Look(ref saved, "fvSaved");
        Scribe_Values.Look(ref buildingCount, "fvBuildings");
        Scribe_Values.Look(ref relation, "fvRelation");
        Scribe_References.Look(ref ownedSilver, "fvOwnedSilver");
        Scribe_References.Look(ref ownedShells, "fvOwnedShells");
        Scribe_References.Look(ref truck, "fvTruck");
        Scribe_Values.Look(ref ownedCount, "fvOwnedCount");
        Scribe_Values.Look(ref silverCount, "fvSilverCount");
        Scribe_Values.Look(ref shellCount, "fvShellCount");
    }
    private void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; }
    private bool Reload { get { return Environment.GetCommandLineArgs().Contains("-tmm-friendly-visit-reload"); } }
    public override void MapComponentUpdate()
    {
        if (done || (active == null && Find.CurrentMap != map) || (active != null && active != this)) return;
        if (!Reload && !Environment.GetCommandLineArgs().Contains("-tmm-friendly-visit-probe")) return;
        active = this; string error = null;
        if (initialized && Find.TickManager.Paused)
        {
            // A random letter or window may pause the game; the probe waits for ticks, so it keeps the clock running.
            foreach (Window w in Find.WindowStack.Windows.ToList()) if (w.forcePause) { facts.Add("closed pausing window " + w.GetType().Name); w.Close(false); }
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
        }
        try
        {
            if (stage == 1)
            {
                // The map picture first (fresh run), then both exits: on foot and by vehicle.
                if (Find.TickManager.TicksGame < readyTick || (!Reload && frames < 24)) return;
                LeaveAndCompare();
            }
            else if (initialized)
            {
                if (Find.TickManager.TicksGame < readyTick) return;
                AfterAttempts();
                Check(settlement.Spawned && settlement.Map.Parent == settlement, "Native ticks falsely destroyed friendly settlement");
                Check(!SettlementDefeatUtility.IsDefeated(settlement.Map, settlement.Faction), "Living peaceful garrison marked defeated");
                Check(!settlement.GetComponent<TimedDetectionRaids>().NextRaidCountdownActiveAndVisible, "Friendly visit starts punitive reinforcement timer");
                Check(settlement.Faction.PlayerGoodwill == relation, "Native ticks changed diplomacy");
                if (!Reload)
                {
                    saved = true; GameDataSaveLoader.SaveGame("TMM-FriendlyVisit");
                    Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-FriendlyVisit.rws")), "No actual visit save");
                }
                captureMap = settlement.Map;
                captureMap.fogGrid.ClearAllFog();
                Current.Game.CurrentMap = captureMap;
                CameraJumper.TryJump(new GlobalTargetInfo(captureMap.Center, captureMap));
                Find.CameraDriver.SetRootPosAndSize(captureMap.Center.ToVector3Shifted(), 52f);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                stage = 1; readyTick = Find.TickManager.TicksGame + 300;
                return;
            }
            else
            {
            if (Reload)
            {
                Check(saved && settlement != null && settlement.HasMap, "Real visited map missing");
                Check(first != null && second != null && first.Spawned && second.Spawned && first.Map == settlement.Map && second.Map == settlement.Map, "Visitor identities/placement lost");
                Check(first.inventory.innerContainer.Contains(cash) && cash.stackCount == 19, "Visitor cargo lost/duplicated");
                Check(settlement.Map.GetComponent<SettlementMapDevelopment>().generatedLevel == 10, "Map tier lost");
                Check(settlement.Map.GetComponent<SettlementMapDevelopment>().initialBuildings == buildingCount, "Map was regenerated");
                Check(settlement.Faction.PlayerGoodwill == relation && !settlement.Faction.HostileTo(Faction.OfPlayer), "Visit changed diplomacy");
                Check(settlement.GetComponent<FormCaravanComp>().CanFormOrReformCaravanNow, "Visitors cannot leave normally after reload");
                SettlementProperty kept = settlement.Map.GetComponent<SettlementProperty>();
                Check(kept.Count == ownedCount, "Owner list changed on load: " + ownedCount + " -> " + kept.Count);
                Check(ownedSilver != null && ownedSilver.stackCount == silverCount && SettlementProperty.Protected(ownedSilver), "Settlement silver lost its owner on load");
                Check(ownedShells != null && ownedShells.stackCount == shellCount && SettlementProperty.Protected(ownedShells), "Settlement shells lost their owner on load");
                Check(!SettlementProperty.IsMarked(cash), "Visitor's own silver became settlement property on load");
                TryToTake();
            }
            else
            {
                settlement = Find.WorldObjects.Settlements.First(s => s.Faction != null && !s.Faction.IsPlayer && !s.Faction.HostileTo(Faction.OfPlayer) && s.Faction.def.humanlikeFaction && s.Faction.def.techLevel >= TechLevel.Industrial && !s.HasMap);
                Find.World.GetComponent<RegionalWorld>().For(settlement).level = 10;
                relation = settlement.Faction.PlayerGoodwill;
                first = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                second = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                cash = ThingMaker.MakeThing(ThingDefOf.Silver); cash.stackCount = 19; first.inventory.innerContainer.TryAdd(cash, false);
                var caravan = CaravanMaker.MakeCaravan(new[] { first, second }, Faction.OfPlayer, settlement.Tile, true);
                Check(FriendlySettlementVisits.CanVisit(caravan, settlement), "Friendly visit guards rejected real caravan");
                var command = settlement.GetCaravanGizmos(caravan).OfType<Command_Action>().FirstOrDefault(c => c.defaultLabel == "TMM.World.EnterFriendly".Translate());
                Check(command != null, "Actual patched caravan entry command absent");
                command.action();
                Check(settlement.HasMap, "Native entry did not generate map");
                Check(first.Spawned && second.Spawned && first.Map == settlement.Map && second.Map == settlement.Map, "Native entry lost actual visitors");
                Check(!caravan.Spawned && !caravan.ContainsPawn(first), "Caravan still holds spawned visitor");
                Check(first.inventory.innerContainer.Contains(cash) && cash.stackCount == 19, "Native entry changed physical cargo");
                Check(settlement.Faction.PlayerGoodwill == relation && !settlement.Faction.HostileTo(Faction.OfPlayer), "Entry triggered war");
                var development = settlement.Map.GetComponent<SettlementMapDevelopment>();
                buildingCount = development.initialBuildings;
                Check(development.generatedLevel == 10 && buildingCount > 0 && development.initialBeds > 0, "Visited base has no actual developed infrastructure");
                Check(settlement.GetComponent<FormCaravanComp>() != null && settlement.GetComponent<FormCaravanComp>().CanFormOrReformCaravanNow, "No normal reform exit from friendly base");
                SetUpProperty();
                // Control run: with the owner list forgotten the same attempts must be caught by these checks.
                if (Environment.GetEnvironmentVariable("TMM_THEFT_CONTROL") == "1") { SettlementProperty.ClearCache(); ownDropped = null; }
                TryToTake();
            }
            initialized = true; readyTick = Find.TickManager.TicksGame + 1500;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            return;
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        done = true;
        string name = Reload ? "FriendlyVisitReloadProbe.json" : "FriendlyVisitProbe.json";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, name), "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks + ",\"facts\":[" + string.Join(",", facts.Select(Q)) + "],\"errors\":" + (error == null ? "[]" : "[" + Q(error) + "]") + "}");
    }
    private Map Town { get { return settlement.Map; } }
    private static int SilverOf(Pawn p) { return p.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount); }

    private Thing OwnedStack(SettlementProperty list, ThingDef def, int count)
    {
        Thing t = list.Things.FirstOrDefault(x => x.def == def && x.Spawned && x.stackCount >= 10);
        if (t != null) { facts.Add(def.defName + ": from map generation, stack " + t.stackCount); return t; }
        t = ThingMaker.MakeThing(def); t.stackCount = count;
        GenPlace.TryPlaceThing(t, CellFinder.RandomClosewalkCellNear(first.Position, Town, 6), Town, ThingPlaceMode.Near);
        list.Mark(t);
        facts.Add(def.defName + ": the generated town has none, the probe placed and marked a stack");
        return t;
    }

    private void SetUpProperty()
    {
        SettlementProperty list = Town.GetComponent<SettlementProperty>();
        Check(list != null && list.Count > 0, "Generated town has no owner list");
        facts.Add("ownedAtGeneration=" + list.Count);
        Check(!SettlementProperty.IsMarked(cash), "Visitor's own silver was marked");
        ownedSilver = OwnedStack(list, ThingDefOf.Silver, 60);
        ownedShells = OwnedStack(list, ThingDef.Named("Shell_Incendiary"), 12);
        ownDropped = ThingMaker.MakeThing(ThingDefOf.Silver); ownDropped.stackCount = 40;
        GenPlace.TryPlaceThing(ownDropped, CellFinder.RandomClosewalkCellNear(second.Position, Town, 4), Town, ThingPlaceMode.Near, out ownDropped);
        ownDropped.SetForbidden(false, false);
        Check(!SettlementProperty.IsMarked(ownDropped) && ownDropped.stackCount == 40, "Visitor's dropped silver merged with settlement silver or was marked");
        // Split and merge keep the owner.
        int whole = ownedSilver.stackCount;
        Thing part = ownedSilver.SplitOff(5);
        Check(SettlementProperty.IsMarked(part), "Split part lost its owner");
        Check(!part.CanStackWith(ownDropped) && !ownDropped.CanStackWith(part) && !ownDropped.TryAbsorbStack(part, true), "Settlement silver merged into visitor silver");
        Check(ownedSilver.TryAbsorbStack(part, true) && ownedSilver.stackCount == whole, "Settlement silver does not merge back");
        // A legal purchase from a trader of this settlement frees exactly what was sold.
        Pawn seller = Town.mapPawns.SpawnedPawnsInFaction(settlement.Faction).First(p => p.RaceProps.Humanlike && !p.Downed);
        bool wanted = seller.mindState.wantsToTradeWithColony;
        seller.mindState.wantsToTradeWithColony = true;
        PawnComponentsUtility.AddAndRemoveDynamicComponents(seller);
        seller.trader.traderKind = DefDatabase<TraderKindDef>.AllDefs.First(k => k.defName == "Caravan_Outlander_BulkGoods");
        Thing goods = ThingMaker.MakeThing(ThingDefOf.Steel); goods.stackCount = 30;
        Check(seller.inventory.innerContainer.TryAdd(goods, false), "Seller could not hold goods");
        Town.GetComponent<SettlementProperty>().Mark(goods);
        Check(SettlementProperty.Protected(goods), "Trader stock is not protected before the sale");
        int steelBefore = Town.listerThings.ThingsOfDef(ThingDefOf.Steel).Where(t => !SettlementProperty.IsMarked(t)).Sum(t => t.stackCount);
        seller.trader.GiveSoldThingToPlayer(goods, 10, first);
        int steelAfter = Town.listerThings.ThingsOfDef(ThingDefOf.Steel).Where(t => !SettlementProperty.IsMarked(t)).Sum(t => t.stackCount) +
            first.inventory.innerContainer.Where(t => t.def == ThingDefOf.Steel).Sum(t => t.stackCount);
        Check(steelAfter - steelBefore == 10 && goods.stackCount == 20 && SettlementProperty.Protected(goods), "Purchase did not transfer exactly the sold part: +" + (steelAfter - steelBefore) + ", left " + goods.stackCount);
        Town.GetComponent<SettlementProperty>();
        SettlementProperty.Release(goods); goods.Destroy();
        seller.mindState.wantsToTradeWithColony = wanted;
        PawnComponentsUtility.AddAndRemoveDynamicComponents(seller);
        // The player's vehicle stands in the town.
        Type vehicleDef = AccessTools.TypeByName("Vehicles.VehicleDef");
        Def def = GenDefDatabase.GetDef(vehicleDef, "TMM_Vehicle_HunterTruck", false);
        IntVec3 cell;
        if (!CellFinder.TryFindRandomCellNear(first.Position, Town, 30, c => GenAdj.OccupiedRect(c, Rot4.North, new IntVec2(3, 5)).All(x => x.InBounds(Town) && x.Standable(Town) && x.GetEdifice(Town) == null && x.GetFirstPawn(Town) == null), out cell)) throw new Exception("No place for the vehicle");
        truck = (Pawn)AccessTools.Method(AccessTools.TypeByName("Vehicles.VehicleSpawner"), "SpawnVehicleRandomized").Invoke(null, new object[] { def, cell, Town, Faction.OfPlayer, (Rot4?)Rot4.North, false });
        var fuel = truck.AllComps.First(c => c.GetType().Name == "CompFueledTravel");
        AccessTools.Method(fuel.GetType(), "Refuel", new[] { typeof(float) }).Invoke(fuel, new object[] { 60f });
        ownedCount = list.Count; silverCount = ownedSilver.stackCount; shellCount = ownedShells.stackCount;
    }

    /// Every ordinary way to take the town's things, twice over the whole test (fresh and after loading).
    private void TryToTake()
    {
        List<Thing> listed = CaravanFormingUtility.AllReachableColonyItems(Town, true, true, false);
        Check(!listed.Any(SettlementProperty.Protected), "Caravan forming lists settlement things");
        if (ownDropped != null) Check(listed.Contains(ownDropped), "Caravan forming no longer lists the visitor's own silver");
        Type dialogType = AccessTools.TypeByName("Vehicles.Dialog_LoadCargo");
        object dialog = Activator.CreateInstance(dialogType, new object[] { truck });
        // By default VF lists only the home area and storage; its own option lists the whole map, which is the harder case.
        object vfSettings = AccessTools.Field(AccessTools.TypeByName("Vehicles.VehicleMod"), "settings").GetValue(null);
        var showAll = AccessTools.Field(vfSettings.GetType(), "showAllCargoItems");
        object oldShow = showAll.GetValue(vfSettings);
        showAll.SetValue(vfSettings, true);
        try { AccessTools.Method(dialogType, "CalculateAndRecacheTransferables").Invoke(dialog, null); }
        finally { showAll.SetValue(vfSettings, oldShow); }
        var rows = ((System.Collections.IEnumerable)AccessTools.Field(dialogType, "transferables").GetValue(dialog)).Cast<TransferableOneWay>().SelectMany(t => t.things).ToList();
        Check(!rows.Any(SettlementProperty.Protected), "VF Load Cargo lists settlement things");
        if (ownDropped != null) Check(rows.Contains(ownDropped), "VF Load Cargo no longer lists the visitor's own silver");
        Check(!first.CanReserve(ownedSilver) && !second.CanReserve(ownedShells), "Visitor can reserve settlement things");
        Check(first.carryTracker.TryStartCarry(ownedSilver, 5, false) == 0 && first.carryTracker.CarriedThing == null, "Visitor picked settlement silver up by hand");
        foreach (var m in AccessTools.GetDeclaredMethods(AccessTools.TypeByName("Vehicles.VehiclePawn")).Where(m => m.Name == "AddOrTransfer" && m.ReturnType == typeof(int)))
        {
            object[] args = m.GetParameters().Length == 1 ? new object[] { ownedShells } : new object[] { ownedShells, 5 };
            Check((int)m.Invoke(truck, args) == 0, "Vehicle accepted settlement shells");
        }
        // Real forced orders.
        var take = JobMaker.MakeJob(JobDefOf.TakeInventory, ownedSilver); take.count = 10; take.playerForced = true;
        first.jobs.TryTakeOrderedJob(take, JobTag.Misc);
        JobDef load = DefDatabase<JobDef>.AllDefs.First(j => j.driverClass != null && j.driverClass.Name == "JobDriver_LoadVehicle");
        var transfer = new TransferableOneWay(); transfer.things.Add(ownedShells); transfer.AdjustTo(5);
        var cargo = (List<TransferableOneWay>)AccessTools.Field(truck.GetType(), "cargoToLoad").GetValue(truck);
        cargo.Add(transfer);
        var loadJob = JobMaker.MakeJob(load, ownedShells, truck); loadJob.count = 5; loadJob.playerForced = true;
        second.jobs.TryTakeOrderedJob(loadJob, JobTag.Misc);
        cargo.Remove(transfer);
        refusals++;
        if (ownDropped != null)
        {
            // The visitor's own silver goes the ordinary way.
            var own = JobMaker.MakeJob(JobDefOf.TakeInventory, ownDropped); own.count = 40; own.playerForced = true;
            second.jobs.TryTakeOrderedJob(own, JobTag.Misc);
        }
    }

    private void AfterAttempts()
    {
        Check(ownedSilver.Spawned && ownedSilver.stackCount == silverCount, "Settlement silver was taken: " + ownedSilver.stackCount + " of " + silverCount);
        Check(ownedShells.Spawned && ownedShells.stackCount == shellCount, "Settlement shells were taken: " + ownedShells.stackCount + " of " + shellCount);
        Check(SilverOf(first) == 19, "First visitor's silver " + SilverOf(first));
        Check(SilverOf(second) == 40, "Second visitor did not pick their own silver up: " + SilverOf(second));
        Check(!truck.inventory.innerContainer.Any(), "Vehicle cargo is not empty: " + truck.inventory.innerContainer.ContentsString);
        Check(settlement.Faction.PlayerGoodwill == relation, "Refused attempts changed goodwill");
        Check(Town.GetComponent<SettlementProperty>().Count == ownedCount, "Owner list changed: " + ownedCount + " -> " + Town.GetComponent<SettlementProperty>().Count);
    }

    private void LeaveAndCompare()
    {
        Faction faction = settlement.Faction;
        PlanetTile tile = settlement.Tile;
        // The visitor's own cargo goes into the vehicle and leaves in it.
        Thing own = second.inventory.innerContainer.First(t => t.def == ThingDefOf.Silver);
        Check(second.inventory.innerContainer.TryTransferToContainer(own, truck.inventory.innerContainer, 25) == 25, "Own silver was not loaded into the vehicle");
        bool boarded = (bool)AccessTools.Method(truck.GetType(), "TryAddPawn", new[] { typeof(Pawn) }).Invoke(truck, new object[] { second });
        Check(boarded, "Second visitor could not board");
        var exit = AccessTools.Method(AccessTools.TypeByName("Vehicles.CaravanHelper"), "ExitMapAndCreateVehicleCaravan", new[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(PlanetTile), typeof(PlanetTile), typeof(PlanetTile), typeof(bool) });
        var byCar = (Caravan)exit.Invoke(null, new object[] { new[] { truck }, Faction.OfPlayer, tile, tile, tile, false });
        Check(byCar != null && !truck.Spawned, "Vehicle did not leave");
        Check(truck.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount) == 25 && SilverOf(second) == 15, "Own silver changed on the vehicle exit: " + truck.inventory.innerContainer.ContentsString + " / " + SilverOf(second));
        Check(!truck.inventory.innerContainer.Any(SettlementProperty.IsMarked) && !second.inventory.innerContainer.Any(SettlementProperty.IsMarked), "Vehicle took settlement things away");
        Check(SettlementProperty.Protected(ownedSilver) && ownedSilver.stackCount == silverCount && ownedShells.stackCount == shellCount, "Town stock changed after the vehicle left");
        Caravan onFoot = CaravanExitMapUtility.ExitMapAndCreateCaravan(new[] { first }, Faction.OfPlayer, tile, Direction8Way.North, tile, false);
        Check(onFoot != null && !first.Spawned && SilverOf(first) == 19, "Own silver changed on the exit on foot");
        Check(!CaravanInventoryUtility.AllInventoryItems(onFoot).Any(SettlementProperty.IsMarked), "Caravan took settlement things away");
        Check(faction.PlayerGoodwill == relation && !faction.HostileTo(Faction.OfPlayer), "Leaving changed goodwill: " + relation + " -> " + faction.PlayerGoodwill);
        facts.Add("attempts=" + refusals + " goodwill=" + relation);
    }

    public override void MapComponentOnGUI()
    {
        if (Reload || captureMap != map || Event.current.type != EventType.Repaint || frames >= 24) return;
        frames++;
        if (frames == 12) AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"), "CaptureScreenshot", new[] { typeof(string) }).Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, "FriendlyCapital.png") });
        if (frames == 24) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FriendlyVisitUiProbe.json"), "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
    }
    private static string Q(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
}
