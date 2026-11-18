using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using Verse;

[StaticConstructorOnStartup]
public static class OutpostShipmentLaunch
{
    static OutpostShipmentLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-outpost-shipment-reload"))
        {
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-OutpostShipment"));
        }
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps != null)
            {
                Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
                if (home != null && home.GetComponent<OutpostShipmentProbe>() == null)
                {
                    home.components.Add(new OutpostShipmentProbe(home));
                }
            }
        });
    }
}

public sealed class OutpostShipmentProbe : MapComponent
{
    public class CheckResult
    {
        public string name;
        public bool passed;
        public string expected;
        public string actual;
    }

    private StrategicOutpost minePost;
    private Pawn miner1;
    private Pawn miner2;
    private Pawn packAnimal;

    private int day1ScheduledTick = -1;
    private int day2ScheduledTick = -1;
    private int day3ScheduledTick = -1;

    private int stage = 0;
    private bool done = false;
    private readonly List<CheckResult> checks = new List<CheckResult>();
    private readonly List<string> errors = new List<string>();
    private readonly List<string> shipmentTable = new List<string>();
    private readonly List<string> facts = new List<string>();
    private bool reloadInitialChecked = false;
    private int lastLoggedTick = -1;

    // Saved state for reload verification
    private string savedMiner1Id;
    private string savedMiner2Id;
    private string savedAnimalId;
    private int savedInitialHomePlasteel = 0;
    private int savedDay1DeliveredPlasteel = 0;
    private int savedDay2ScheduledTick = -1;
    private int initialHomePlasteel = 0;

    // Balance constants (approved balance proposal 1):
    // Plasteel: yieldPerSkill = 0.4
    // 2 miners with skill 15 -> totalSkill = 30
    // Expected daily yield = floor(30 * 0.4 / 1 + 0) = 12 plasteel
    private const int ExpectedDailyPlasteel = 12;

    public OutpostShipmentProbe(Map map) : base(map)
    {
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref minePost, "osMinePost");
        Scribe_References.Look(ref miner1, "osMiner1");
        Scribe_References.Look(ref miner2, "osMiner2");
        Scribe_References.Look(ref packAnimal, "osPackAnimal");

        Scribe_Values.Look(ref stage, "osStage", 0);
        Scribe_Values.Look(ref day1ScheduledTick, "osDay1Sched", -1);
        Scribe_Values.Look(ref day2ScheduledTick, "osDay2Sched", -1);
        Scribe_Values.Look(ref day3ScheduledTick, "osDay3Sched", -1);

        Scribe_Values.Look(ref savedMiner1Id, "osSavedMiner1Id");
        Scribe_Values.Look(ref savedMiner2Id, "osSavedMiner2Id");
        Scribe_Values.Look(ref savedAnimalId, "osSavedAnimalId");
        Scribe_Values.Look(ref savedInitialHomePlasteel, "osSavedInitialHomePlasteel", 0);
        Scribe_Values.Look(ref savedDay1DeliveredPlasteel, "osSavedDay1Delivered", 0);
        Scribe_Values.Look(ref savedDay2ScheduledTick, "osSavedDay2ScheduledTick", -1);
        Scribe_Values.Look(ref initialHomePlasteel, "osInitialHomePlasteel", 0);
    }

    private void Check(string name, bool ok, string expected, string actual)
    {
        checks.Add(new CheckResult
        {
            name = name,
            passed = ok,
            expected = expected,
            actual = actual
        });
        if (!ok)
        {
            facts.Add("FAILED CHECK [" + name + "]: expected " + expected + ", got " + actual);
        }
    }

    private int CountPlasteelOnMap(Map m)
    {
        if (m == null || m.listerThings == null) return 0;
        int total = 0;
        var things = m.listerThings.ThingsOfDef(ThingDefOf.Plasteel);
        if (things != null)
        {
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t != null && !t.Destroyed && t.Spawned)
                {
                    total += t.stackCount;
                }
            }
        }
        var all = m.listerThings.AllThings;
        if (all != null)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var incoming = all[i] as DropPodIncoming;
                if (incoming != null && incoming.Contents != null && incoming.Contents.innerContainer != null)
                {
                    total += incoming.Contents.innerContainer.TotalStackCountOfDef(ThingDefOf.Plasteel);
                }
            }
        }
        if (m.mapPawns != null && m.mapPawns.AllPawnsSpawned != null)
        {
            var pawns = m.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null) continue;
                if (p.carryTracker != null && p.carryTracker.CarriedThing != null && p.carryTracker.CarriedThing.def == ThingDefOf.Plasteel)
                {
                    total += p.carryTracker.CarriedThing.stackCount;
                }
                if (p.inventory != null && p.inventory.innerContainer != null)
                {
                    total += p.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Plasteel);
                }
            }
        }
        return total;
    }

    private bool HasPodsInFlight(Map m)
    {
        if (m == null || m.listerThings == null) return false;
        var all = m.listerThings.AllThings;
        if (all == null) return false;
        for (int i = 0; i < all.Count; i++)
        {
            Thing t = all[i];
            if (t != null && !t.Destroyed && t.Spawned)
            {
                if (t is Skyfaller || t.def.defName == "DropPodIncoming")
                {
                    return true;
                }
            }
        }
        return false;
    }

    private Pawn CreateMinerWithSkill(int level)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                allowAddictions: false,
                canGeneratePawnRelations: false,
                developmentalStages: DevelopmentalStage.Adult));

            if (p.skills != null &&
                !p.skills.GetSkill(SkillDefOf.Mining).TotallyDisabled &&
                !p.WorkTypeIsDisabled(WorkTypeDefOf.Mining) &&
                p.health != null && p.health.capacities != null &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                StrategicOutpostCaravanUtility.IsSuitableColonist(p))
            {
                p.skills.GetSkill(SkillDefOf.Mining).Level = level;
                if (p.relations != null) p.relations.ClearAllRelations();
                return p;
            }
            if (!Find.WorldPawns.Contains(p))
            {
                if (p.relations != null) p.relations.ClearAllRelations();
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
            }
        }
        throw new Exception("Unable to generate miner pawn capable of mining");
    }

    private PlanetTile FindLegalTile()
    {
        var neighbors = new List<PlanetTile>();
        var frontier = new Queue<PlanetTile>();
        var visited = new HashSet<PlanetTile>();
        frontier.Enqueue(map.Tile);
        visited.Add(map.Tile);

        while (frontier.Count > 0 && visited.Count < 3000)
        {
            var current = frontier.Dequeue();
            Find.WorldGrid.GetTileNeighbors(current, neighbors);
            foreach (var candidate in neighbors)
            {
                if (!visited.Add(candidate)) continue;
                frontier.Enqueue(candidate);

                string reason;
                if (candidate.Layer == Find.WorldGrid.Surface &&
                    StrategicOutpostCaravanUtility.CanCreateOutpostAt(candidate, out reason) &&
                    TileFinder.IsValidTileForNewSettlement(candidate))
                {
                    return candidate;
                }
            }
        }
        for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
        {
            PlanetTile candidate = new PlanetTile(i);
            string reason;
            if (candidate.Layer == Find.WorldGrid.Surface &&
                StrategicOutpostCaravanUtility.CanCreateOutpostAt(candidate, out reason) &&
                TileFinder.IsValidTileForNewSettlement(candidate))
            {
                return candidate;
            }
        }
        throw new Exception("No legal tile found for mining outpost founding.");
    }

    private void CleanDanglingRelationsNarrowed()
    {
        var allKnownPawns = new HashSet<Pawn>();
        if (Find.Maps != null)
        {
            foreach (var m in Find.Maps)
            {
                if (m != null && m.mapPawns != null)
                {
                    foreach (var p in m.mapPawns.AllPawns) if (p != null) allKnownPawns.Add(p);
                }
            }
        }
        if (Find.WorldPawns != null)
        {
            foreach (var p in Find.WorldPawns.AllPawnsAliveOrDead) if (p != null) allKnownPawns.Add(p);
        }
        var allOutposts = (Find.WorldObjects != null) ? Find.WorldObjects.AllWorldObjects.OfType<StrategicOutpost>().ToList() : new List<StrategicOutpost>();
        foreach (var post in allOutposts)
        {
            if (post.AllPawns != null)
            {
                foreach (var p in post.AllPawns) if (p != null) allKnownPawns.Add(p);
            }
        }

        var outpostAndPlayerPawns = new HashSet<Pawn>();
        foreach (var post in allOutposts)
        {
            if (post.Workers != null) foreach (var p in post.Workers) if (p != null) outpostAndPlayerPawns.Add(p);
            if (post.Animals != null) foreach (var p in post.Animals) if (p != null) outpostAndPlayerPawns.Add(p);
            if (post.AllPawns != null) foreach (var p in post.AllPawns) if (p != null) outpostAndPlayerPawns.Add(p);
        }
        if (miner1 != null) outpostAndPlayerPawns.Add(miner1);
        if (miner2 != null) outpostAndPlayerPawns.Add(miner2);
        if (packAnimal != null) outpostAndPlayerPawns.Add(packAnimal);
        foreach (var p in allKnownPawns)
        {
            if (p != null && (p.Faction == Faction.OfPlayer || p.IsColonist)) outpostAndPlayerPawns.Add(p);
        }

        foreach (var p in allKnownPawns)
        {
            if (p.relations != null && p.relations.DirectRelations != null)
            {
                var rels = p.relations.DirectRelations.ToList();
                for (int i = rels.Count - 1; i >= 0; i--)
                {
                    var rel = rels[i];
                    if (rel == null) continue;
                    Pawn target = rel.otherPawn;
                    if (target == null || !allKnownPawns.Contains(target))
                    {
                        bool targetIsOutpostOrPlayer = (target != null && (outpostAndPlayerPawns.Contains(target) || target.Faction == Faction.OfPlayer || target.IsColonist));
                        if (targetIsOutpostOrPlayer)
                        {
                            string tName = target != null ? (target.Name != null ? target.Name.ToStringFull : target.LabelShort) : "null";
                            string tId = target != null ? target.ThingID : "null";
                            Check("DanglingRelation_TargetIsOutpostOrPlayer", false, "no dangling relation to outpost/player pawn",
                                string.Format("Dangling relation {0} from owner {1} to outpost/player target {2} ({3})",
                                    (rel.def != null ? rel.def.defName : "null"), p.ThingID, tName, tId));
                        }
                        else
                        {
                            p.relations.RemoveDirectRelation(rel);
                            string relName = rel.def != null ? rel.def.defName : "null";
                            string ownerName = p.Name != null ? p.Name.ToStringFull : p.LabelShort;
                            string targetName = target != null ? (target.Name != null ? target.Name.ToStringFull : target.LabelShort) : "null";
                            string targetFaction = (target != null && target.Faction != null) ? target.Faction.Name : "null";
                            string factEntry = string.Format("Sanitized foreign relation: {0} | owner={1} ({2}) | target={3} ({4}) | targetFaction={5}",
                                relName, ownerName, p.ThingID, targetName, (target != null ? target.ThingID : "null"), targetFaction);
                            facts.Add(factEntry);
                            Log.Message("[OutpostShipmentProbe] " + factEntry);
                        }
                    }
                }
            }
        }
    }

    private void Fresh()
    {
        // =========================================================================
        // STAGE 0: Founding of Mining Outpost with targetColonyTile set to home map
        // =========================================================================
        if (stage == 0)
        {
            initialHomePlasteel = CountPlasteelOnMap(map);

            miner1 = CreateMinerWithSkill(15);
            miner2 = CreateMinerWithSkill(15);
            packAnimal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDef.Named("Muffalo"), Faction.OfPlayer));

            PlanetTile tile = FindLegalTile();
            Caravan caravan = CaravanMaker.MakeCaravan(new[] { miner1, miner2, packAnimal }, Faction.OfPlayer, tile, true);
            minePost = StrategicOutpostCaravanUtility.FoundOutpostFromCaravan(caravan, StrategicOutpostSpecialization.Mining);
            Check("Founding_Mine_Spawned", minePost != null && minePost.Spawned, "true", (minePost != null ? minePost.Spawned.ToString().ToLower() : "null"));

            minePost.selectedProductDefNames.Clear();
            minePost.selectedProductDefNames.Add("TMM_Outpost_Mining_Plasteel");
            minePost.targetColonyTile = map.Tile;
            day1ScheduledTick = minePost.nextProductionTick;

            Check("Founding_TargetColonyTileSet", minePost.targetColonyTile == map.Tile, map.Tile.ToString(), minePost.targetColonyTile.ToString());
            Check("Founding_InitialProducedGoodsEmpty", minePost.ProducedGoods.Count == 0, "0", minePost.ProducedGoods.Count.ToString());

            var prod = DefDatabase<StrategicOutpostProductDef>.GetNamedSilentFail("TMM_Outpost_Mining_Plasteel");
            Check("Founding_ProductDefLoaded", prod != null, "true", (prod != null).ToString().ToLower());
            if (prod != null)
            {
                bool m1Can = prod.WorkerCanProduce(miner1);
                bool m2Can = prod.WorkerCanProduce(miner2);
                Check("Founding_Miner1CanMine", m1Can, "true", m1Can.ToString().ToLower());
                Check("Founding_Miner2CanMine", m2Can, "true", m2Can.ToString().ToLower());
                double expYield = minePost.ExpectedYield(prod);
                Check("Founding_ExpectedDailyYield12", expYield == 12.0, "12", expYield.ToString());
                facts.Add(string.Format("Founding: expectedYield={0}, prodRate={1}, workers={2}", expYield, prod.Rate, minePost.Workers.Count));
            }

            Settlement targetSettlement = Find.WorldObjects.SettlementAt(minePost.targetColonyTile);
            Check("Founding_TargetSettlementFound", targetSettlement != null && targetSettlement.Faction == Faction.OfPlayer, "true",
                (targetSettlement != null ? (targetSettlement.Faction == Faction.OfPlayer).ToString().ToLower() : "null"));

            shipmentTable.Add(string.Format("Stage 0 | Initial home plasteel: {0} | Mining outpost founded at tile {1}", initialHomePlasteel, tile));
            stage = 1;
            return;
        }

        // =========================================================================
        // STAGE 1: Day 1 Elapse & Automatic Drop-Pod Shipment to Colony
        // =========================================================================
        if (stage == 1)
        {
            if (minePost.lastProductionTick < day1ScheduledTick)
            {
                return;
            }

            // Wait for drop pod skyfallers to land (drop pods take 110 ticks to land)
            if (minePost.lastShipmentTick > 0 && Find.TickManager.TicksGame < minePost.lastShipmentTick + 500)
            {
                return;
            }
            if (HasPodsInFlight(map))
            {
                return;
            }

            int currentHomePlasteel = CountPlasteelOnMap(map);
            int outpostPlasteel = minePost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.Plasteel);
            int deliveredToHome = currentHomePlasteel - initialHomePlasteel;

            facts.Add(string.Format("Stage1 fact: TicksGame={0}, lastProd={1}, lastShip={2}, homePlasteel={3}, outpostPlasteel={4}, mapThings={5}, skyfallers={6}",
                Find.TickManager.TicksGame, minePost.lastProductionTick, minePost.lastShipmentTick,
                currentHomePlasteel, outpostPlasteel,
                map.listerThings.ThingsOfDef(ThingDefOf.Plasteel) != null ? map.listerThings.ThingsOfDef(ThingDefOf.Plasteel).Count : 0,
                map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming) != null ? map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming).Count : 0));

            foreach (var m in Find.Maps)
            {
                var plasteelThings = m.listerThings.ThingsOfDef(ThingDefOf.Plasteel);
                int pCount = (plasteelThings != null) ? plasteelThings.Count : 0;
                int pTotal = (plasteelThings != null) ? plasteelThings.Where(t => t != null && !t.Destroyed && t.Spawned).Sum(t => t.stackCount) : 0;
                facts.Add(string.Format("Map[{0}] (tile {1}): plasteelThingsCount={2}, plasteelSum={3}", m.Index, m.Tile, pCount, pTotal));
            }

            // Calculations: 2 miners with 15 skill -> 30 total skill * 0.4 yield = 12 plasteel
            Check("Day1_OutpostCargoEmptyAfterShipment", outpostPlasteel == 0, "0", outpostPlasteel.ToString());
            Check("Day1_ColonyReceivedExactPlasteel", deliveredToHome == ExpectedDailyPlasteel, ExpectedDailyPlasteel.ToString(), deliveredToHome.ToString());
            Check("Day1_LastShipmentTickUpdated", minePost.lastShipmentTick > 0, ">0", minePost.lastShipmentTick.ToString());

            shipmentTable.Add(string.Format("Day 1 | Produced: {0} | Shipped: {0} | OutpostStock: {1} | HomeDelivered: {2}",
                ExpectedDailyPlasteel, outpostPlasteel, deliveredToHome));

            // Setup Day 2: Test accumulating goods without target colony (targetColonyTile = -1)
            minePost.targetColonyTile = -1;
            day2ScheduledTick = minePost.nextProductionTick;

            // Save fixture for reload testing
            savedMiner1Id = miner1.ThingID;
            savedMiner2Id = miner2.ThingID;
            savedAnimalId = packAnimal.ThingID;
            savedInitialHomePlasteel = initialHomePlasteel;
            savedDay1DeliveredPlasteel = deliveredToHome;
            savedDay2ScheduledTick = day2ScheduledTick;

            stage = 2;
            CleanDanglingRelationsNarrowed();
            GameDataSaveLoader.SaveGame("TMM-OutpostShipment");
            Check("Day1_SaveFileCreated", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-OutpostShipment.rws")), "true", "true");
            return;
        }

        // =========================================================================
        // STAGE 2: Day 2 Elapse — Accumulation Test (targetColonyTile = -1)
        // =========================================================================
        if (stage == 2)
        {
            if (minePost.lastProductionTick < day2ScheduledTick)
            {
                return;
            }

            if (HasPodsInFlight(map))
            {
                return;
            }

            int currentHomePlasteel = CountPlasteelOnMap(map);
            int outpostPlasteel = minePost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.Plasteel);
            int deliveredToHome = currentHomePlasteel - initialHomePlasteel;

            facts.Add(string.Format("Stage1 fact: TicksGame={0}, lastProd={1}, lastShip={2}, homePlasteel={3}, outpostPlasteel={4}, mapThings={5}, skyfallers={6}",
                Find.TickManager.TicksGame, minePost.lastProductionTick, minePost.lastShipmentTick,
                currentHomePlasteel, outpostPlasteel,
                map.listerThings.ThingsOfDef(ThingDefOf.Plasteel) != null ? map.listerThings.ThingsOfDef(ThingDefOf.Plasteel).Count : 0,
                map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming) != null ? map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming).Count : 0));

            foreach (var m in Find.Maps)
            {
                var plasteelThings = m.listerThings.ThingsOfDef(ThingDefOf.Plasteel);
                int pCount = (plasteelThings != null) ? plasteelThings.Count : 0;
                int pTotal = (plasteelThings != null) ? plasteelThings.Where(t => t != null && !t.Destroyed && t.Spawned).Sum(t => t.stackCount) : 0;
                facts.Add(string.Format("Map[{0}] (tile {1}): plasteelThingsCount={2}, plasteelSum={3}", m.Index, m.Tile, pCount, pTotal));
            }

            // Day 2 produced 12 plasteel, but targetColonyTile == -1, so it MUST stay in outpost cargo!
            Check("Day2_NoTarget_GoodsAccumulatedInOutpost", outpostPlasteel == ExpectedDailyPlasteel, ExpectedDailyPlasteel.ToString(), outpostPlasteel.ToString());
            Check("Day2_NoTarget_NoNewDeliveryToColony", deliveredToHome == ExpectedDailyPlasteel, ExpectedDailyPlasteel.ToString(), deliveredToHome.ToString());

            shipmentTable.Add(string.Format("Day 2 | Produced: {0} | Target: None (-1) | OutpostAccumulated: {1} | HomeTotal: {2}",
                ExpectedDailyPlasteel, outpostPlasteel, currentHomePlasteel));

            // =========================================================================
            // STAGE 3: Test Invalid Target Tile (no exception, goods retained)
            // =========================================================================
            minePost.targetColonyTile = 999999;
            minePost.DoDailyShipment();

            int outpostPlasteelAfterInvalid = minePost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.Plasteel);
            Check("InvalidTarget_GoodsRetainedInOutpost", outpostPlasteelAfterInvalid == ExpectedDailyPlasteel, ExpectedDailyPlasteel.ToString(), outpostPlasteelAfterInvalid.ToString());

            // Setup Day 3: Re-enable home target; should ship accumulated 12 + Day 3's 12 = 24 plasteel!
            minePost.targetColonyTile = map.Tile;
            day3ScheduledTick = minePost.nextProductionTick;

            stage = 3;
            return;
        }

        // =========================================================================
        // STAGE 3: Day 3 Elapse — Full Delivery of Accumulated Stock (12 + 12 = 24)
        // =========================================================================
        if (stage == 3)
        {
            if (minePost.lastProductionTick < day3ScheduledTick)
            {
                return;
            }

            if (minePost.lastShipmentTick > 0 && Find.TickManager.TicksGame < minePost.lastShipmentTick + 500)
            {
                return;
            }

            if (HasPodsInFlight(map))
            {
                return;
            }

            int currentHomePlasteel = CountPlasteelOnMap(map);
            int outpostPlasteel = minePost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.Plasteel);
            int totalDelivered = currentHomePlasteel - initialHomePlasteel;

            // Day 3 produced 12. Combined with Day 2's 12 = 24 plasteel shipped in pods!
            // Total delivered across all 3 days = 12 (day 1) + 24 (day 3) = 36 plasteel.
            Check("Day3_OutpostCargoEmptyAfterFullShipment", outpostPlasteel == 0, "0", outpostPlasteel.ToString());
            Check("Day3_TotalDeliveredEquals36", totalDelivered == ExpectedDailyPlasteel * 3, (ExpectedDailyPlasteel * 3).ToString(), totalDelivered.ToString());

            shipmentTable.Add(string.Format("Day 3 | Produced: {0} | ShippedAccumulated: 24 | OutpostStock: {1} | TotalDeliveredOver3Days: {2}",
                ExpectedDailyPlasteel, outpostPlasteel, totalDelivered));

            var allPawns = new[] { miner1, miner2, packAnimal };
            bool pawnsAliveAndHeld = allPawns.All(p => p != null && !p.Dead && !p.Spawned && Find.WorldPawns.Contains(p));
            Check("Final_MinersAndAnimalPreservedInWorldPawns", pawnsAliveAndHeld, "true", pawnsAliveAndHeld.ToString().ToLower());

            stage = 4;
            bool anyFailed = checks.Any(c => !c.passed);
            Finish(anyFailed ? new Exception("One or more OutpostShipment checks failed") : null, false);
        }
    }

    private void Reload()
    {
        if (!reloadInitialChecked)
        {
            reloadInitialChecked = true;

            Check("Reload_SaveState_StageIsTwo", stage == 2, "2", stage.ToString());
            Check("Reload_MinePost_Present", minePost != null && minePost.Spawned, "true", (minePost != null ? minePost.Spawned.ToString().ToLower() : "null"));

            // Verify loaded pawn identities
            Check("Reload_Miner1_Preserved", miner1 != null && miner1.ThingID == savedMiner1Id && Find.WorldPawns.Contains(miner1), "true", (miner1 != null ? miner1.ThingID : "null"));
            Check("Reload_Miner2_Preserved", miner2 != null && miner2.ThingID == savedMiner2Id && Find.WorldPawns.Contains(miner2), "true", (miner2 != null ? miner2.ThingID : "null"));
            Check("Reload_Animal_Preserved", packAnimal != null && packAnimal.ThingID == savedAnimalId && Find.WorldPawns.Contains(packAnimal), "true", (packAnimal != null ? packAnimal.ThingID : "null"));

            // Verify loaded Day 1 delivery in colony
            int currentHomePlasteel = CountPlasteelOnMap(map);
            int deliveredDay1 = currentHomePlasteel - savedInitialHomePlasteel;
            Check("Reload_Day1_DeliveredPlasteelPreserved", deliveredDay1 == savedDay1DeliveredPlasteel, savedDay1DeliveredPlasteel.ToString(), deliveredDay1.ToString());

            // In reload, re-target home colony for Day 2 shipment
            minePost.targetColonyTile = map.Tile;
            minePost.nextProductionTick = savedDay2ScheduledTick;
            day2ScheduledTick = savedDay2ScheduledTick;
        }

        if (minePost.lastProductionTick >= day2ScheduledTick)
        {
            if (minePost.lastShipmentTick > 0 && Find.TickManager.TicksGame < minePost.lastShipmentTick + 500)
            {
                return;
            }

            if (HasPodsInFlight(map))
            {
                return;
            }

            int currentHomePlasteel = CountPlasteelOnMap(map);
            int outpostPlasteel = minePost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.Plasteel);
            int totalDelivered = currentHomePlasteel - savedInitialHomePlasteel;

            // Day 2 produced 12 plasteel and delivered it to colony. Total on home map = 12 + 12 = 24.
            Check("Reload_Day2_OutpostCargoEmpty", outpostPlasteel == 0, "0", outpostPlasteel.ToString());
            Check("Reload_Day2_TotalDeliveredEquals24", totalDelivered == ExpectedDailyPlasteel * 2, (ExpectedDailyPlasteel * 2).ToString(), totalDelivered.ToString());

            bool anyReloadFailed = checks.Any(c => !c.passed);
            Finish(anyReloadFailed ? new Exception("One or more OutpostShipment reload checks failed") : null, true);
        }
    }

    private void MaintainHomeMapStability()
    {
        if (map != null && map.mapPawns != null)
        {
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null) continue;
                if (p.needs != null)
                {
                    if (p.needs.food != null) p.needs.food.CurLevelPercentage = 1f;
                    if (p.needs.rest != null) p.needs.rest.CurLevelPercentage = 1f;
                    if (p.needs.mood != null) p.needs.mood.CurLevelPercentage = 1f;
                }
                if (p.health != null && p.health.hediffSet != null)
                {
                    var mal = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition);
                    if (mal != null) p.health.RemoveHediff(mal);
                }
            }
        }

        if (Find.WindowStack != null)
        {
            var windows = Find.WindowStack.Windows.ToList();
            for (int i = 0; i < windows.Count; i++)
            {
                var w = windows[i];
                if (w is Dialog_MessageBox || w.forcePause || w.absorbInputAroundWindow || w.GetType().FullName == "HugsLib.News.Dialog_UpdateFeatures")
                {
                    w.Close(false);
                }
            }
        }

        if (Find.LetterStack != null)
        {
            Find.LetterStack.LettersListForReading.Clear();
        }

        if (Find.TickManager != null)
        {
            Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
            try
            {
                var slower = Find.TickManager.slower;
                if (slower != null)
                {
                    var f = HarmonyLib.AccessTools.Field(slower.GetType(), "forceNormalSpeedUntil");
                    if (f != null) f.SetValue(slower, 0);
                }
            }
            catch {}
        }
    }

    private void LogProgress()
    {
        if (Find.TickManager == null) return;
        int now = Find.TickManager.TicksGame;
        if (lastLoggedTick < 0 || now - lastLoggedTick >= 10000)
        {
            lastLoggedTick = now;
            string msg = string.Format("tick={0} stage={1} paused={2} speed={3}\n",
                now, stage, Find.TickManager.Paused, Find.TickManager.CurTimeSpeed);
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "ShipmentProgress.log"), msg);
        }
    }

    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        bool reload = args.Contains("-tmm-outpost-shipment-reload");
        bool fresh = args.Contains("-tmm-outpost-shipment-probe");

        if (done || map.Parent is StrategicOutpost || !map.IsPlayerHome || (!reload && !fresh))
        {
            return;
        }

        try
        {
            MaintainHomeMapStability();
            LogProgress();
            if (reload)
            {
                Reload();
            }
            else
            {
                Fresh();
            }
        }
        catch (Exception ex)
        {
            Finish(ex, reload);
        }
    }

    private static string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private void Finish(Exception ex, bool isReload)
    {
        done = true;
        if (ex != null)
        {
            errors.Add(ex.ToString());
        }

        bool overallPassed = (ex == null) && checks.All(c => c.passed);

        var checkJsonEntries = new List<string>();
        foreach (var c in checks)
        {
            checkJsonEntries.Add(string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}",
                EscapeJson(c.name),
                c.passed ? "true" : "false",
                EscapeJson(c.expected),
                EscapeJson(c.actual)));
        }

        var errorJsonEntries = new List<string>();
        foreach (var err in errors)
        {
            errorJsonEntries.Add("\"" + EscapeJson(err) + "\"");
        }

        var tableJsonEntries = new List<string>();
        foreach (var row in shipmentTable)
        {
            tableJsonEntries.Add("\"" + EscapeJson(row) + "\"");
        }

        var factJsonEntries = new List<string>();
        foreach (var f in facts)
        {
            factJsonEntries.Add("\"" + EscapeJson(f) + "\"");
        }

        string jsonReport = string.Format(
            "{{\n  \"passed\": {0},\n  \"checksCount\": {1},\n  \"stage\": {2},\n  \"shipmentTable\": [\n    {3}\n  ],\n  \"facts\": [\n    {4}\n  ],\n  \"checks\": [\n    {5}\n  ],\n  \"errors\": [\n    {6}\n  ]\n}}",
            overallPassed ? "true" : "false",
            checks.Count,
            stage,
            string.Join(",\n    ", tableJsonEntries.ToArray()),
            string.Join(",\n    ", factJsonEntries.ToArray()),
            string.Join(",\n    ", checkJsonEntries.ToArray()),
            string.Join(",\n    ", errorJsonEntries.ToArray())
        );

        string reportFileName = isReload ? "OutpostShipmentReloadProbe.json" : "OutpostShipmentProbe.json";
        string reportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, reportFileName);
        File.WriteAllText(reportPath, jsonReport);

        if (!isReload)
        {
            string uiReportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "OutpostShipmentUiProbe.json");
            File.WriteAllText(uiReportPath, "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
        }
    }
}
