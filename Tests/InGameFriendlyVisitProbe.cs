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
        SteelLedger.Apply();
    }
}

/// Follows the identity of the purchased steel: every part split from it stays "bought", and a merge between bought
/// and any other steel is counted. Without this an unmarked stack of the same size cannot be told from the purchase.
public static class SteelLedger
{
    public static HashSet<int> Bought = new HashSet<int>();
    public static Thing Selling;
    public static int Mixed;
    public static void Apply()
    {
        var harmony = new Harmony("tmm.probe.steel");
        harmony.Patch(AccessTools.Method(typeof(Thing), "SplitOff"), postfix: new HarmonyMethod(typeof(SteelLedger), "Split"));
        harmony.Patch(AccessTools.Method(typeof(Thing), "TryAbsorbStack"), prefix: new HarmonyMethod(typeof(SteelLedger), "AbsorbStart"), postfix: new HarmonyMethod(typeof(SteelLedger), "AbsorbEnd"));
    }
    private static void Split(Thing __instance, Thing __result)
    {
        if (__result == null || __result.def != ThingDefOf.Steel) return;
        if (Selling == __instance || Bought.Contains(__instance.thingIDNumber)) if (__result != __instance) Bought.Add(__result.thingIDNumber);
    }
    private static void AbsorbStart(Thing other, out int __state) { __state = other == null ? 0 : other.stackCount; }
    private static void AbsorbEnd(Thing __instance, Thing other, int __state)
    {
        if (other == null || other.def != ThingDefOf.Steel || other.stackCount == __state) return;
        if (Bought.Contains(__instance.thingIDNumber) != Bought.Contains(other.thingIDNumber)) Mixed += __state - other.stackCount;
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
    // The purchased steel, the town's steel next to it and a third party's steel.
    private Thing townSteel, strangerSteel;
    private Pawn stranger;
    private HashSet<int> bought = new HashSet<int>();
    private bool waiting; private int nextFact;
    private int markedSteel, foreignSteel, mixed, loadDeadline;
    private string mode;
    private const int BoughtCount = 10;
    private static string Control { get { return Environment.GetEnvironmentVariable("TMM_STEEL_CONTROL") ?? ""; } }
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
        Scribe_References.Look(ref townSteel, "fvTownSteel");
        Scribe_References.Look(ref strangerSteel, "fvStrangerSteel");
        Scribe_References.Look(ref stranger, "fvStranger");
        Scribe_Values.Look(ref markedSteel, "fvMarkedSteel");
        Scribe_Values.Look(ref foreignSteel, "fvForeignSteel");
        Scribe_Values.Look(ref mode, "fvSteelMode");
        if (Scribe.mode == LoadSaveMode.Saving) { bought = SteelLedger.Bought; mixed = SteelLedger.Mixed; }
        Scribe_Collections.Look(ref bought, "fvBought", LookMode.Value);
        Scribe_Values.Look(ref mixed, "fvMixed");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && saved) { SteelLedger.Bought = bought ?? new HashSet<int>(); SteelLedger.Mixed = mixed; }
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
            foreach (Window w in Find.WindowStack.Windows.ToList()) if (w.forcePause) facts.Add("pausing window: " + ProbeWindows.Dismiss(w));
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
        }
        try
        {
            if (stage == 1)
            {
                // The map picture first (fresh run), then both exits: on foot and by vehicle.
                if (Find.TickManager.TicksGame < readyTick || (!Reload && frames < 24)) return;
                LeaveAndCompare();
                if (waiting) return;
            }
            else if (stage == 3)
            {
                // The load order is under way.
                Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
                if (Find.TickManager.TicksGame >= nextFact) { nextFact = Find.TickManager.TicksGame + 1000; facts.Add("t=" + Find.TickManager.TicksGame + " first " + first.Position + " " + first.CurJob + " / second " + second.Position + " " + second.CurJob + " truck " + truck.Position + " cargo " + InCargo().Sum(t => t.stackCount)); }
                if (!LoadReached()) { if (Find.TickManager.TicksGame > loadDeadline) throw new Exception("The ordered load did not finish: " + string.Join(" | ", SteelRows().ToArray())); return; }
                Steel(mode == "carry" && !Reload ? "carried to the vehicle" : "after the load into the vehicle");
                CheckCargo(mode == "carry" && !Reload ? 0 : BoughtCount);
                SaveAndShow();
                return;
            }
            else if (initialized)
            {
                if (Find.TickManager.TicksGame < readyTick) return;
                AfterAttempts();
                Check(settlement.Spawned && settlement.Map.Parent == settlement, "Native ticks falsely destroyed friendly settlement");
                Check(!SettlementDefeatUtility.IsDefeated(settlement.Map, settlement.Faction), "Living peaceful garrison marked defeated");
                Check(!settlement.GetComponent<TimedDetectionRaids>().NextRaidCountdownActiveAndVisible, "Friendly visit starts punitive reinforcement timer");
                Check(settlement.Faction.PlayerGoodwill == relation, "Native ticks changed diplomacy");
                Steel("after 1500 ticks of ordinary work");
                bool order = Reload ? mode == "reload" || mode == "carry" : mode == "fresh" || mode == "carry";
                if (order && InCargo().Sum(t => t.stackCount) < BoughtCount)
                {
                    OrderLoad();
                    stage = 3; loadDeadline = Find.TickManager.TicksGame + 15000;
                    return;
                }
                SaveAndShow();
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
                facts.Add("mode=" + mode + " control=" + Control);
                Steel("right after loading the save");
                Check(townSteel != null && SettlementProperty.Protected(townSteel), "Town steel lost its owner on load");
                Check(strangerSteel != null && stranger != null && stranger.inventory.innerContainer.Contains(strangerSteel), "Stranger's steel is not with its holder after load");
                if (mode == "fresh") CheckCargo(BoughtCount);
                if (mode == "reload") CheckCargo(0);
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
                Check(settlement.GetComponent<FormCaravanComp>() != null && settlement.GetComponent<FormCaravanComp>().CanFormOrReformCaravanNow, "No normal reform exit from friendly base; hostile: " + string.Join(", ", settlement.Map.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer).Select(x => x.Thing.ThingID + " " + x.Thing.def.defName + " of " + (x.Thing.Faction == null ? "none" : x.Thing.Faction.def.defName) + " at " + x.Thing.Position + " fogged=" + x.Thing.Position.Fogged(settlement.Map) + " active=" + GenHostility.IsActiveThreatToPlayer(x) + " disabled=" + x.ThreatDisabled(null) + " dormant=" + (x.Thing.TryGetComp<CompCanBeDormant>() == null ? "no" : (!x.Thing.TryGetComp<CompCanBeDormant>().Awake).ToString())).ToArray()) + "; minds: " + first.MentalState + "/" + second.MentalState);
                mode = Environment.GetEnvironmentVariable("TMM_STEEL_MODE") ?? "natural";
                facts.Add("mode=" + mode + " control=" + Control);
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

    private static bool IsBought(Thing t) { return SteelLedger.Bought.Contains(t.thingIDNumber) && !SettlementProperty.IsMarked(t); }
    private List<Thing> AllSteel()
    {
        var all = new List<Thing>();
        ThingOwnerUtility.GetAllThingsRecursively(Town, ThingRequest.ForDef(ThingDefOf.Steel), all, true, null, true);
        return all.Where(t => !t.Destroyed).Distinct().ToList();
    }
    private List<Thing> InCargo() { return truck == null ? new List<Thing>() : truck.inventory.innerContainer.Where(t => t.def == ThingDefOf.Steel).ToList(); }
    private string Place(Thing t)
    {
        if (t.Spawned) return "ground " + t.Position;
        Pawn p = t.ParentHolder == null ? null : (t.ParentHolder is Pawn_InventoryTracker ? ((Pawn_InventoryTracker)t.ParentHolder).pawn : t.ParentHolder is Pawn_CarryTracker ? ((Pawn_CarryTracker)t.ParentHolder).pawn : null);
        if (p == null) return t.ParentHolder == null ? "nowhere" : t.ParentHolder.GetType().Name;
        string who = p == truck ? "vehicle cargo" : p == first ? "first visitor" : p == second ? "second visitor" : p == stranger ? "stranger" : p.ThingID;
        return who + (t.ParentHolder is Pawn_CarryTracker ? " hands" : "") + " of " + (p.Faction == null ? "none" : p.Faction.Name);
    }
    private IEnumerable<string> SteelRows()
    {
        foreach (Thing t in AllSteel())
            yield return t.ThingID + " x" + t.stackCount + " owner=" + (SettlementProperty.IsMarked(t) ? Town.ParentFaction.Name : IsBought(t) ? "player(bought)" : "not bought") +
                " mark=" + SettlementProperty.IsMarked(t) + " protected=" + SettlementProperty.Protected(t) + " forbidden=" + (t.Spawned && t.IsForbidden(Faction.OfPlayer)) + (t.Spawned && t.Position.Fogged(Town) ? " FOGGED" : "") + " at " + Place(t);
    }
    /// Quantity, owner and settlement mark of every piece of steel on the map. Each class must keep its own total.
    private void Steel(string when, int boughtExpected = BoughtCount)
    {
        List<Thing> all = AllSteel();
        int mine = all.Where(IsBought).Sum(t => t.stackCount), town = all.Where(SettlementProperty.IsMarked).Sum(t => t.stackCount);
        int other = all.Where(t => !IsBought(t) && !SettlementProperty.IsMarked(t)).Sum(t => t.stackCount);
        string rows = string.Join(" | ", SteelRows().ToArray());
        facts.Add("STEEL " + when + " tick=" + Find.TickManager.TicksGame + ": bought=" + mine + " town=" + town + " other=" + other + " mixed=" + SteelLedger.Mixed + " cargo=" + InCargo().Sum(t => t.stackCount) + " :: " + rows);
        Check(SteelLedger.Mixed == 0, "Bought steel was merged with other steel (" + SteelLedger.Mixed + ") " + when);
        Check(mine == boughtExpected, "Bought steel " + mine + ", expected " + boughtExpected + " " + when + ": " + rows);
        Check(town == markedSteel, "Town steel " + town + ", expected " + markedSteel + " " + when + ": " + rows);
        Check(other == foreignSteel, "Other steel " + other + ", expected " + foreignSteel + " " + when + ": " + rows);
        Check(InCargo().All(IsBought), "Steel in the vehicle that was not bought, " + when + ": " + rows);
        Check(!all.Any(t => SteelLedger.Bought.Contains(t.thingIDNumber) && SettlementProperty.IsMarked(t)), "Bought steel carries the town's mark " + when + ": " + rows);
    }
    private void CheckCargo(int expected)
    {
        Check(truck.inventory.innerContainer.All(IsBought) && InCargo().Sum(t => t.stackCount) == expected, "Vehicle cargo is not " + expected + " bought steel: " + string.Join(" | ", SteelRows().ToArray()));
    }
    private void OrderLoad()
    {
        foreach (Thing t in AllSteel().Where(t => t.Spawned && IsBought(t))) t.SetForbidden(false, false);
        if (!Reload && Control != "") BreakForControl();
        if (OnVisitors().Count > 0) { facts.Add("no new order: the steel is already on its way, jobs " + first.CurJob + " / " + second.CurJob); return; }
        var def = DefDatabase<WorkGiverDef>.AllDefs.First(d => d.giverClass != null && d.giverClass.Name == "WorkGiver_RefuelVehicleTurret");
        var giver = (WorkGiver_Scanner)def.Worker;
        foreach (Pawn p in new[] { first, second })
        {
            var defs = (System.Collections.IEnumerable)AccessTools.Method(AccessTools.TypeByName("Vehicles.JobDriver_GiveItemToVehicle"), "FindThingDefsToPack").Invoke(null, new object[] { truck, p });
            facts.Add("giver for " + p.ThingID + ": available=" + AccessTools.Method(giver.GetType(), "JobAvailable").Invoke(giver, new object[] { p, truck }) + " reachTruck=" + p.CanReach(truck.Position, giver.PathEndMode, Danger.Deadly) + " reachTruckGiver=" + p.CanReach(truck.Position, giver.PathEndMode, p.NormalMaxDanger()) + " forbidden=" + truck.IsForbidden(p) + " overloaded=" + MassUtility.IsOverEncumbered(p) + " wanted=" + string.Join(",", defs.Cast<object>().Select(x => x.ToString()).ToArray()) + " truckAt=" + truck.Position + " region=" + (truck.Position.GetRegion(Town) == null ? "none" : "ok") + " maxDanger=" + p.NormalMaxDanger() + " stealReachAtMaxDanger=" + string.Join(",", AllSteel().Where(t => t.Spawned && IsBought(t)).Select(t => p.CanReach(t, PathEndMode.Touch, p.NormalMaxDanger()).ToString()).ToArray()) + " temp=" + p.AmbientTemperature.ToString("F0") + " quota=" + Quota());
            Job job = giver.JobOnThing(p, truck, true);
            if (job == null) continue;
            facts.Add("order: " + p.ThingID + " " + job.def.defName + " A=" + job.targetA + " B=" + job.targetB + " count=" + job.count);
            job.playerForced = true;
            Check(p.jobs.TryTakeOrderedJob(job, JobTag.Misc), "The load order was not accepted");
            return;
        }
        throw new Exception("VF offers no turret load job for the bought steel: " + string.Join(",", AllSteel().Where(t => t.Spawned).Select(t => t.ThingID + " reach=" + first.CanReach(t, PathEndMode.Touch, Danger.Deadly) + "/" + second.CanReach(t, PathEndMode.Touch, Danger.Deadly) + " reserve=" + first.CanReserve(t) + "/" + second.CanReserve(t)).ToArray()) + " " + string.Join(" | ", SteelRows().ToArray()));
    }
    private string Quota()
    {
        object comp = AccessTools.Property(truck.GetType(), "CompVehicleTurrets").GetValue(truck, null);
        var turrets = ((System.Collections.IEnumerable)AccessTools.Property(comp.GetType(), "Turrets").GetValue(comp, null)).Cast<object>();
        return string.Join(",", turrets.Select(x => AccessTools.Method(comp.GetType(), "GetQuotaLevel").Invoke(comp, new[] { x }) + " loaded=" + AccessTools.Field(x.GetType(), "loadedAmmo").GetValue(x)).ToArray());
    }
    private List<Thing> OnVisitors()
    {
        return new[] { first, second }.SelectMany(p => p.inventory.innerContainer.Concat(p.carryTracker.CarriedThing == null ? new Thing[0] : new[] { p.carryTracker.CarriedThing })).Where(t => t.def == ThingDefOf.Steel).ToList();
    }
    private bool LoadReached()
    {
        if (mode == "carry" && !Reload) return OnVisitors().Sum(t => t.stackCount) == BoughtCount;
        return InCargo().Sum(t => t.stackCount) >= BoughtCount && OnVisitors().Count == 0;
    }
    /// Control runs: each makes a situation the old check (unmarked steel, ten at most) lets through.
    private void BreakForControl()
    {
        if (Control == "relabel")
        {
            // The town's stack loses its mark and is loaded in place of the purchase.
            foreach (Thing t in AllSteel().Where(t => t.Spawned && IsBought(t))) t.SetForbidden(true, false);
            SettlementProperty.Release(townSteel); townSteel.SetForbidden(false, false);
            markedSteel -= townSteel.stackCount; foreignSteel += townSteel.stackCount;
        }
        else if (Control == "swap")
        {
            foreach (Thing t in AllSteel().Where(IsBought).ToList()) t.Destroy();
            Thing alien = ThingMaker.MakeThing(ThingDefOf.Steel); alien.stackCount = BoughtCount;
            truck.inventory.innerContainer.TryAdd(alien, false);
        }
        else if (Control == "merge")
        {
            SettlementProperty.Release(townSteel);
            Thing mine = AllSteel().First(t => t.Spawned && IsBought(t));
            Check(mine.TryAbsorbStack(townSteel, false), "Control could not merge");
        }
        else if (Control == "mark") Town.GetComponent<SettlementProperty>().Mark(AllSteel().First(t => t.Spawned && IsBought(t)));
        facts.Add("control " + Control + " done; old check: unmarked<=10 in cargo");
        if (Control == "swap" || Control == "merge" || Control == "mark")
        {
            facts.Add("old check passes=" + (truck.inventory.innerContainer.All(t => t.def == ThingDefOf.Steel && !SettlementProperty.IsMarked(t)) && truck.inventory.innerContainer.Sum(t => t.stackCount) <= 10));
            Steel("control " + Control);
        }
    }
    private void SaveAndShow()
    {
        if (!Reload)
        {
            Steel("right before saving");
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
    }
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
        Pawn seller = Town.mapPawns.SpawnedPawnsInFaction(settlement.Faction).Where(p => p.RaceProps.Humanlike && !p.Downed && !p.Position.Fogged(Town) && first.CanReach(p, PathEndMode.Touch, Danger.Deadly) && second.CanReach(p, PathEndMode.Touch, Danger.Deadly)).OrderBy(p => p.Position.DistanceToSquared(first.Position)).FirstOrDefault();
        if (seller == null)
        {
            // Nobody of this town can be seen and walked up to (fog or locked doors): one of them comes out, as a trader does.
            seller = Town.mapPawns.SpawnedPawnsInFaction(settlement.Faction).First(p => p.RaceProps.Humanlike && !p.Downed);
            facts.Add("no resident is visible and reachable for the visitors; " + seller.ThingID + " comes out to them");
            seller.Position = CellFinder.RandomClosewalkCellNear(first.Position, Town, 12);
            seller.Notify_Teleported(true, false);
        }
        bool wanted = seller.mindState.wantsToTradeWithColony;
        seller.mindState.wantsToTradeWithColony = true;
        PawnComponentsUtility.AddAndRemoveDynamicComponents(seller);
        seller.trader.traderKind = DefDatabase<TraderKindDef>.AllDefs.First(k => k.defName == "Caravan_Outlander_BulkGoods");
        Thing goods = ThingMaker.MakeThing(ThingDefOf.Steel); goods.stackCount = 30;
        Check(seller.inventory.innerContainer.TryAdd(goods, false), "Seller could not hold goods");
        Town.GetComponent<SettlementProperty>().Mark(goods);
        Check(SettlementProperty.Protected(goods), "Trader stock is not protected before the sale");
        // The town's own steel lies next to the visitors, and a third pawn carries unmarked steel of its own: both look like the purchase.
        townSteel = ThingMaker.MakeThing(ThingDefOf.Steel); townSteel.stackCount = BoughtCount;
        GenPlace.TryPlaceThing(townSteel, CellFinder.RandomClosewalkCellNear(first.Position, Town, 4), Town, ThingPlaceMode.Near, out townSteel);
        list.Mark(townSteel);
        stranger = Town.mapPawns.SpawnedPawnsInFaction(settlement.Faction).First(p => p != seller && p.RaceProps.Humanlike && !p.Downed);
        strangerSteel = ThingMaker.MakeThing(ThingDefOf.Steel); strangerSteel.stackCount = BoughtCount;
        Check(stranger.inventory.innerContainer.TryAdd(strangerSteel, false), "Stranger could not hold steel");
        SteelLedger.Bought.Clear(); SteelLedger.Mixed = 0;
        markedSteel = AllSteel().Where(SettlementProperty.IsMarked).Sum(t => t.stackCount);
        foreignSteel = AllSteel().Where(t => !SettlementProperty.IsMarked(t)).Sum(t => t.stackCount);
        Steel("before the purchase", 0);
        SteelLedger.Selling = goods;
        int steelBefore = Town.listerThings.ThingsOfDef(ThingDefOf.Steel).Where(t => !SettlementProperty.IsMarked(t)).Sum(t => t.stackCount);
        seller.trader.GiveSoldThingToPlayer(goods, 10, first);
        SteelLedger.Selling = null;
        markedSteel -= BoughtCount;
        Steel("after the purchase");
        int steelAfter = Town.listerThings.ThingsOfDef(ThingDefOf.Steel).Where(t => !SettlementProperty.IsMarked(t)).Sum(t => t.stackCount) +
            first.inventory.innerContainer.Where(t => t.def == ThingDefOf.Steel).Sum(t => t.stackCount);
        Check(steelAfter - steelBefore == 10 && goods.stackCount == 20 && SettlementProperty.Protected(goods), "Purchase did not transfer exactly the sold part: +" + (steelAfter - steelBefore) + ", left " + goods.stackCount);
        Town.GetComponent<SettlementProperty>();
        SettlementProperty.Release(goods); goods.Destroy(); markedSteel -= 20;
        // In the ordered modes the purchase waits for its order.
        if (mode != "natural") foreach (Thing t in AllSteel().Where(IsBought)) t.SetForbidden(true, false);
        seller.mindState.wantsToTradeWithColony = wanted;
        PawnComponentsUtility.AddAndRemoveDynamicComponents(seller);
        // The player's vehicle stands in the town.
        Type vehicleDef = AccessTools.TypeByName("Vehicles.VehicleDef");
        Def def = GenDefDatabase.GetDef(vehicleDef, "TMM_Vehicle_HunterTruck", false);
        IntVec3 cell;
        if (!CellFinder.TryFindRandomCellNear(first.Position, Town, 30, c => first.CanReach(c, PathEndMode.OnCell, Danger.Deadly) && second.CanReach(c, PathEndMode.OnCell, Danger.Deadly) && GenAdj.OccupiedRect(c, Rot4.North, new IntVec2(3, 5)).All(x => x.InBounds(Town) && x.Standable(Town) && x.GetEdifice(Town) == null && x.GetFirstPawn(Town) == null), out cell)) throw new Exception("No place for the vehicle");
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
        Thing inHands = first.carryTracker.CarriedThing;   // in a save made mid-carry it is the bought steel
        Check(inHands == null || IsBought(inHands), "First visitor carries something that was not bought: " + inHands);
        Check(first.carryTracker.TryStartCarry(ownedSilver, 5, false) == 0 && first.carryTracker.CarriedThing == inHands, "Visitor picked settlement silver up by hand");
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
        // The pickup's gun takes steel: a visitor may load the purchase on their own, and only the purchase.
        Check(truck.inventory.innerContainer.All(IsBought), "Vehicle cargo holds something that was not bought: " + string.Join(" | ", SteelRows().ToArray()) + " / " + truck.inventory.innerContainer.ContentsString);
        Check(settlement.Faction.PlayerGoodwill == relation, "Refused attempts changed goodwill");
        Check(Town.GetComponent<SettlementProperty>().Count == ownedCount, "Owner list changed: " + ownedCount + " -> " + Town.GetComponent<SettlementProperty>().Count);
    }

    private void LeaveAndCompare()
    {
        Faction faction = settlement.Faction;
        PlanetTile tile = settlement.Tile;
        // On a save made mid-carry the pawn finishes the load before the vehicle leaves.
        if (mode == "carry" && OnVisitors().Count > 0)
        {
            if (loadDeadline == 0 || Find.TickManager.TicksGame < loadDeadline) { Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast; if (loadDeadline == 0) loadDeadline = Find.TickManager.TicksGame + 15000; waiting = true; return; }
            throw new Exception("A visitor never put the steel down");
        }
        waiting = false;
        Steel("right before leaving");
        int cargoSteel = InCargo().Sum(t => t.stackCount);
        if (mode == "fresh" || mode == "carry" || (mode == "reload" && Reload)) CheckCargo(BoughtCount);
        // The visitor's own cargo goes into the vehicle and leaves in it.
        Thing own = second.inventory.innerContainer.First(t => t.def == ThingDefOf.Silver);
        Check(second.inventory.innerContainer.TryTransferToContainer(own, truck.inventory.innerContainer, 25) == 25, "Own silver was not loaded into the vehicle");
        bool boarded = (bool)AccessTools.Method(truck.GetType(), "TryAddPawn", new[] { typeof(Pawn) }).Invoke(truck, new object[] { second });
        Check(boarded, "Second visitor could not board");
        var exit = AccessTools.Method(AccessTools.TypeByName("Vehicles.CaravanHelper"), "ExitMapAndCreateVehicleCaravan", new[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(PlanetTile), typeof(PlanetTile), typeof(PlanetTile), typeof(bool) });
        var byCar = (Caravan)exit.Invoke(null, new object[] { new[] { truck }, Faction.OfPlayer, tile, tile, tile, false });
        Check(byCar != null && !truck.Spawned, "Vehicle did not leave");
        Check(truck.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount) == 25 && SilverOf(second) == 15, "Own silver changed on the vehicle exit: " + truck.inventory.innerContainer.ContentsString + " / " + SilverOf(second));
        Check(InCargo().Sum(t => t.stackCount) == cargoSteel && InCargo().All(IsBought), "Steel in the vehicle changed on the exit: " + truck.inventory.innerContainer.ContentsString);
        facts.Add("STEEL on the world map: vehicle cargo=" + string.Join(",", InCargo().Select(t => t.ThingID + " x" + t.stackCount + " bought=" + IsBought(t) + " mark=" + SettlementProperty.IsMarked(t)).ToArray()));
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
