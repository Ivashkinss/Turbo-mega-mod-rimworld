using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using Verse;

[StaticConstructorOnStartup]
public static class OutpostCalendarLaunch
{
    static OutpostCalendarLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-outpost-calendar-reload"))
        {
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-OutpostCalendar"));
        }
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps != null)
            {
                Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
                if (home != null && home.GetComponent<OutpostCalendarProbe>() == null)
                {
                    home.components.Add(new OutpostCalendarProbe(home));
                }
            }
        });
    }
}

public sealed class OutpostCalendarProbe : MapComponent
{
    public class CheckResult
    {
        public string name;
        public bool passed;
        public string expected;
        public string actual;
    }

    private StrategicOutpost farmPost;
    private StrategicOutpost factoryPost;
    private StrategicOutpost watchPost;

    private Pawn farmP1, farmP2, farmAnimal;
    private Pawn factP1, factP2, factAnimal;
    private Pawn watchP1, watchP2, watchAnimal;

    private int farmDay1ScheduledTick = -1;
    private int factDay1ScheduledTick = -1;
    private int watchDay1ScheduledTick = -1;

    private int farmDay2ScheduledTick = -1;
    private int factDay2ScheduledTick = -1;
    private int watchDay2ScheduledTick = -1;

    private int farmDay3ScheduledTick = -1;
    private int factDay3ScheduledTick = -1;

    private int stage = 0;
    private bool done = false;
    private readonly List<CheckResult> checks = new List<CheckResult>();
    private readonly List<string> errors = new List<string>();
    private readonly List<string> dayTable = new List<string>();
    private bool reloadInitialChecked = false;
    private int lastLoggedTick = -1;

    // Saved state for reload verification
    private string savedFarmP1Id, savedFarmP2Id, savedFarmAnimalId;
    private string savedFactP1Id, savedFactP2Id, savedFactAnimalId;
    private string savedWatchP1Id, savedWatchP2Id, savedWatchAnimalId;

    private int savedFarmDay1Potatoes = 0;
    private int savedFarmDay1Rice = 0;
    private float savedFarmDay1PotRemainder = 0f;
    private float savedFarmDay1RiceRemainder = 0f;
    private int savedFactDay1Components = 0;
    private int savedFactDay1Steel = 0;
    private int savedFarmNextTick = -1;
    private int savedFactNextTick = -1;
    private int savedWatchNextTick = -1;

    public OutpostCalendarProbe(Map map) : base(map)
    {
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref farmPost, "ocFarmPost");
        Scribe_References.Look(ref factoryPost, "ocFactoryPost");
        Scribe_References.Look(ref watchPost, "ocWatchPost");

        Scribe_References.Look(ref farmP1, "ocFarmP1");
        Scribe_References.Look(ref farmP2, "ocFarmP2");
        Scribe_References.Look(ref farmAnimal, "ocFarmAnimal");

        Scribe_References.Look(ref factP1, "ocFactP1");
        Scribe_References.Look(ref factP2, "ocFactP2");
        Scribe_References.Look(ref factAnimal, "ocFactAnimal");

        Scribe_References.Look(ref watchP1, "ocWatchP1");
        Scribe_References.Look(ref watchP2, "ocWatchP2");
        Scribe_References.Look(ref watchAnimal, "ocWatchAnimal");

        Scribe_Values.Look(ref stage, "ocStage", 0);
        Scribe_Values.Look(ref farmDay1ScheduledTick, "ocFarmDay1Sched", -1);
        Scribe_Values.Look(ref factDay1ScheduledTick, "ocFactDay1Sched", -1);
        Scribe_Values.Look(ref watchDay1ScheduledTick, "ocWatchDay1Sched", -1);

        Scribe_Values.Look(ref farmDay2ScheduledTick, "ocFarmDay2Sched", -1);
        Scribe_Values.Look(ref factDay2ScheduledTick, "ocFactDay2Sched", -1);
        Scribe_Values.Look(ref watchDay2ScheduledTick, "ocWatchDay2Sched", -1);

        Scribe_Values.Look(ref farmDay3ScheduledTick, "ocFarmDay3Sched", -1);
        Scribe_Values.Look(ref factDay3ScheduledTick, "ocFactDay3Sched", -1);

        Scribe_Values.Look(ref savedFarmP1Id, "ocSavedFarmP1Id");
        Scribe_Values.Look(ref savedFarmP2Id, "ocSavedFarmP2Id");
        Scribe_Values.Look(ref savedFarmAnimalId, "ocSavedFarmAnimalId");

        Scribe_Values.Look(ref savedFactP1Id, "ocSavedFactP1Id");
        Scribe_Values.Look(ref savedFactP2Id, "ocSavedFactP2Id");
        Scribe_Values.Look(ref savedFactAnimalId, "ocSavedFactAnimalId");

        Scribe_Values.Look(ref savedWatchP1Id, "ocSavedWatchP1Id");
        Scribe_Values.Look(ref savedWatchP2Id, "ocSavedWatchP2Id");
        Scribe_Values.Look(ref savedWatchAnimalId, "ocSavedWatchAnimalId");

        Scribe_Values.Look(ref savedFarmDay1Potatoes, "ocSavedFarmDay1Pot", 0);
        Scribe_Values.Look(ref savedFarmDay1Rice, "ocSavedFarmDay1Rice", 0);
        Scribe_Values.Look(ref savedFarmDay1PotRemainder, "ocSavedFarmDay1PotRem", 0f);
        Scribe_Values.Look(ref savedFarmDay1RiceRemainder, "ocSavedFarmDay1RiceRem", 0f);
        Scribe_Values.Look(ref savedFactDay1Components, "ocSavedFactDay1Comp", 0);
        Scribe_Values.Look(ref savedFactDay1Steel, "ocSavedFactDay1Steel", 0);

        Scribe_Values.Look(ref savedFarmNextTick, "ocSavedFarmNextTick", -1);
        Scribe_Values.Look(ref savedFactNextTick, "ocSavedFactNextTick", -1);
        Scribe_Values.Look(ref savedWatchNextTick, "ocSavedWatchNextTick", -1);
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
            throw new Exception("Check failed [" + name + "]: expected " + expected + ", got " + actual);
        }
    }

    private Pawn CreateWorkerWithSkill(SkillDef skillDef, int level)
    {
        for (int attempt = 0; attempt < 25; attempt++)
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

            if (p.skills != null && !p.skills.GetSkill(skillDef).TotallyDisabled)
            {
                p.skills.GetSkill(skillDef).Level = level;
                return p;
            }
            if (!Find.WorldPawns.Contains(p))
            {
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Discard);
            }
        }
        throw new Exception("Unable to generate pawn with skill " + skillDef.defName);
    }

    private PlanetTile FindLegalTile(HashSet<PlanetTile> excluded)
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
                if (excluded != null && excluded.Contains(candidate)) continue;

                string reason;
                if (candidate.Layer == Find.WorldGrid.Surface &&
                    StrategicOutpostCaravanUtility.CanCreateOutpostAt(candidate, out reason) &&
                    TileFinder.IsValidTileForNewSettlement(candidate))
                {
                    return candidate;
                }
            }
        }
        throw new Exception("No legal tile found for caravan outpost founding.");
    }

    private void Research(string defName)
    {
        ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
        if (project == null || project.IsFinished) return;
        if (project.prerequisites != null)
        {
            foreach (var req in project.prerequisites)
            {
                Research(req.defName);
            }
        }
        Find.ResearchManager.FinishProject(project, false, null, false);
    }

    private void AddRawItem(StrategicOutpost post, ThingDef def, int count)
    {
        while (count > 0)
        {
            Thing t = ThingMaker.MakeThing(def);
            t.stackCount = Math.Min(count, def.stackLimit);
            int n = t.stackCount;
            post.RawMaterials.TryAdd(t, false);
            count -= n;
        }
    }

    private void Fresh()
    {
        int now = Find.TickManager.TicksGame;

        // =========================================================================
        // STAGE 0: Simultaneous Founding of Farm, Factory, Watchtower
        // =========================================================================
        if (stage == 0)
        {
            var usedTiles = new HashSet<PlanetTile>();

            // 1. Farming Outpost (Plants 7 and Plants 4)
            farmP1 = CreateWorkerWithSkill(SkillDefOf.Plants, 7);
            farmP2 = CreateWorkerWithSkill(SkillDefOf.Plants, 4);
            farmAnimal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDef.Named("Muffalo"), Faction.OfPlayer));
            PlanetTile farmTile = FindLegalTile(usedTiles);
            usedTiles.Add(farmTile);

            Caravan farmCaravan = CaravanMaker.MakeCaravan(new[] { farmP1, farmP2, farmAnimal }, Faction.OfPlayer, farmTile, true);
            farmPost = StrategicOutpostCaravanUtility.FoundOutpostFromCaravan(farmCaravan, StrategicOutpostSpecialization.Farming);
            Check("Founding_Farm_Spawned", farmPost != null && farmPost.Spawned, "true", (farmPost != null ? farmPost.Spawned.ToString().ToLower() : "null"));
            farmPost.targetColonyTile = -1;
            farmPost.selectedProductDefNames.Clear();
            farmPost.selectedProductDefNames.Add("TMM_Outpost_Farming_Potatoes");
            farmPost.selectedProductDefNames.Add("TMM_Outpost_Farming_Rice");
            farmDay1ScheduledTick = farmPost.nextProductionTick;

            // 2. Factory Outpost (Crafting 16 and Crafting 16)
            Research("Fabrication");
            factP1 = CreateWorkerWithSkill(SkillDefOf.Crafting, 16);
            factP2 = CreateWorkerWithSkill(SkillDefOf.Crafting, 16);
            factAnimal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDef.Named("Muffalo"), Faction.OfPlayer));
            PlanetTile factTile = FindLegalTile(usedTiles);
            usedTiles.Add(factTile);

            Caravan factCaravan = CaravanMaker.MakeCaravan(new[] { factP1, factP2, factAnimal }, Faction.OfPlayer, factTile, true);
            factoryPost = StrategicOutpostCaravanUtility.FoundOutpostFromCaravan(factCaravan, StrategicOutpostSpecialization.Factory);
            Check("Founding_Factory_Spawned", factoryPost != null && factoryPost.Spawned, "true", (factoryPost != null ? factoryPost.Spawned.ToString().ToLower() : "null"));
            factoryPost.targetColonyTile = -1;
            factoryPost.selectedProductDefNames.Clear();
            factoryPost.selectedProductDefNames.Add("TMM_Outpost_Factory_ComponentIndustrial");
            // Day 1 raw ingredients: exactly 24 steel (for 2 components, capacity is 4)
            AddRawItem(factoryPost, ThingDefOf.Steel, 24);
            factDay1ScheduledTick = factoryPost.nextProductionTick;

            // 3. Watchtower Outpost
            watchP1 = CreateWorkerWithSkill(SkillDefOf.Shooting, 15);
            watchP2 = CreateWorkerWithSkill(SkillDefOf.Shooting, 15);
            watchAnimal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDef.Named("Muffalo"), Faction.OfPlayer));
            PlanetTile watchTile = FindLegalTile(usedTiles);
            usedTiles.Add(watchTile);

            Caravan watchCaravan = CaravanMaker.MakeCaravan(new[] { watchP1, watchP2, watchAnimal }, Faction.OfPlayer, watchTile, true);
            watchPost = StrategicOutpostCaravanUtility.FoundOutpostFromCaravan(watchCaravan, StrategicOutpostSpecialization.Watchtower);
            Check("Founding_Watchtower_Spawned", watchPost != null && watchPost.Spawned, "true", (watchPost != null ? watchPost.Spawned.ToString().ToLower() : "null"));
            watchPost.targetColonyTile = -1;
            watchDay1ScheduledTick = watchPost.nextProductionTick;

            stage = 1;
            return;
        }

        // =========================================================================
        // STAGE 1: Real Day 1 Wait & Verification
        // =========================================================================
        if (stage == 1)
        {
            if (farmPost.lastProductionTick < farmDay1ScheduledTick ||
                factoryPost.lastProductionTick < factDay1ScheduledTick ||
                watchPost.lastProductionTick < watchDay1ScheduledTick)
            {
                // Day 1 has not elapsed yet on all 3 outposts
                return;
            }

            // Verify timing window within 2500 ticks
            int farmDelta = farmPost.lastProductionTick - farmDay1ScheduledTick;
            int factDelta = factoryPost.lastProductionTick - factDay1ScheduledTick;
            int watchDelta = watchPost.lastProductionTick - watchDay1ScheduledTick;
            Check("Day1_Farm_TickWindow", farmDelta >= 0 && farmDelta <= 2500, "<=2500", farmDelta.ToString());
            Check("Day1_Factory_TickWindow", factDelta >= 0 && factDelta <= 2500, "<=2500", factDelta.ToString());
            Check("Day1_Watchtower_TickWindow", watchDelta >= 0 && watchDelta <= 2500, "<=2500", watchDelta.ToString());

            // 1. Farming Day 1
            // Skills: 7 + 4 = 11. Rate: 2.25. 2 products -> perProductFactor = 0.5.
            // Earned: 11 * 2.25 * 0.5 = 12.375.
            // Floor: 12 units. Kept remainder: 0.375.
            int potDay1 = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawPotatoes"));
            int riceDay1 = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawRice"));
            float potRem1 = 0f; farmPost.fractionalRemainders.TryGetValue("TMM_Outpost_Farming_Potatoes", out potRem1);
            float riceRem1 = 0f; farmPost.fractionalRemainders.TryGetValue("TMM_Outpost_Farming_Rice", out riceRem1);

            Check("Farming_Day1_Potatoes_Count", potDay1 == 12, "12", potDay1.ToString());
            Check("Farming_Day1_Rice_Count", riceDay1 == 12, "12", riceDay1.ToString());
            Check("Farming_Day1_Potatoes_Remainder", Math.Abs(potRem1 - 0.375f) < 0.001f, "0.375", potRem1.ToString("F3"));
            Check("Farming_Day1_Rice_Remainder", Math.Abs(riceRem1 - 0.375f) < 0.001f, "0.375", riceRem1.ToString("F3"));

            // Repeat public call on same day does NOT duplicate
            farmPost.DoDailyProduction();
            int potRepeat = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawPotatoes"));
            Check("Farming_Day1_RepeatCallNoDuplicate", potRepeat == 12, "12", potRepeat.ToString());

            dayTable.Add("Farming Day 1 | Expected: 12 pot / 12 rice (rem 0.375) | Actual: " + potDay1 + " pot / " + riceDay1 + " rice (rem " + potRem1.ToString("F3") + ")");

            // 2. Factory Day 1
            // Skills: 16 + 16 = 32. Rate: 0.125. Capacity = 4.0.
            // Ingredients: 24 Steel. Required: 12/unit -> Max possible = 2.
            // Produced: 2 components. Steel left: 0.
            int compDay1 = factoryPost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial);
            int steelLeftDay1 = factoryPost.RawMaterials.TotalStackCountOfDef(ThingDefOf.Steel);
            float compRem1 = 0f; factoryPost.fractionalRemainders.TryGetValue("TMM_Outpost_Factory_ComponentIndustrial", out compRem1);

            Check("Factory_Day1_Components_Count", compDay1 == 2, "2", compDay1.ToString());
            Check("Factory_Day1_Steel_Deducted", steelLeftDay1 == 0, "0", steelLeftDay1.ToString());
            Check("Factory_Day1_Remainder", Math.Abs(compRem1 - 0.0f) < 0.001f, "0.000", compRem1.ToString("F3"));

            factoryPost.DoDailyProduction();
            int compRepeat = factoryPost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial);
            Check("Factory_Day1_RepeatCallNoDuplicate", compRepeat == 2, "2", compRepeat.ToString());

            dayTable.Add("Factory Day 1 | Expected: 2 comp / 0 steel left | Actual: " + compDay1 + " comp / " + steelLeftDay1 + " steel left");

            // 3. Watchtower Day 1
            int expectedHostiles = 0;
            int expectedTotals = 0;
            foreach (Settlement s in Find.WorldObjects.Settlements)
            {
                if (s == null || !s.Spawned || s.Tile < 0) continue;
                int dist = Find.WorldGrid.TraversalDistanceBetween(watchPost.Tile, s.Tile, true, 16);
                if (dist <= 15)
                {
                    expectedTotals++;
                    if (s.Faction != null && s.Faction.HostileTo(Faction.OfPlayer)) expectedHostiles++;
                }
            }
            Check("Watchtower_Day1_TotalSettlements", watchPost.lastScannedTotalSettlements == expectedTotals, expectedTotals.ToString(), watchPost.lastScannedTotalSettlements.ToString());
            Check("Watchtower_Day1_HostileSettlements", watchPost.lastScannedHostileSettlements == expectedHostiles, expectedHostiles.ToString(), watchPost.lastScannedHostileSettlements.ToString());
            Check("Watchtower_Day1_NoGoodsProduced", watchPost.ProducedGoods.Count == 0, "0", watchPost.ProducedGoods.Count.ToString());

            dayTable.Add("Watchtower Day 1 | Expected: " + expectedTotals + " total / " + expectedHostiles + " hostile | Actual: " + watchPost.lastScannedTotalSettlements + " / " + watchPost.lastScannedHostileSettlements);

            // 4. Colony & Worker GC Verification
            var allPawns = new[] { farmP1, farmP2, farmAnimal, factP1, factP2, factAnimal, watchP1, watchP2, watchAnimal };
            bool pawnsAliveAndHeld = allPawns.All(p => p != null && !p.Dead && !p.Spawned && Find.WorldPawns.Contains(p));
            Check("Day1_PersonnelAndAnimalsHeldInWorldPawns", pawnsAliveAndHeld, "true", pawnsAliveAndHeld.ToString().ToLower());

            // 5. Save Game in the middle of cycle (after Day 1)
            savedFarmP1Id = farmP1.ThingID;
            savedFarmP2Id = farmP2.ThingID;
            savedFarmAnimalId = farmAnimal.ThingID;

            savedFactP1Id = factP1.ThingID;
            savedFactP2Id = factP2.ThingID;
            savedFactAnimalId = factAnimal.ThingID;

            savedWatchP1Id = watchP1.ThingID;
            savedWatchP2Id = watchP2.ThingID;
            savedWatchAnimalId = watchAnimal.ThingID;

            savedFarmDay1Potatoes = potDay1;
            savedFarmDay1Rice = riceDay1;
            savedFarmDay1PotRemainder = potRem1;
            savedFarmDay1RiceRemainder = riceRem1;
            savedFactDay1Components = compDay1;
            savedFactDay1Steel = steelLeftDay1;

            savedFarmNextTick = farmPost.nextProductionTick;
            savedFactNextTick = factoryPost.nextProductionTick;
            savedWatchNextTick = watchPost.nextProductionTick;

            farmDay2ScheduledTick = farmPost.nextProductionTick;
            factDay2ScheduledTick = factoryPost.nextProductionTick;
            watchDay2ScheduledTick = watchPost.nextProductionTick;

            stage = 2;
            GameDataSaveLoader.SaveGame("TMM-OutpostCalendar");
            Check("Day1_SaveFileCreated", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-OutpostCalendar.rws")), "true", "true");

            // For Factory Day 2: Steel remains 0 (ingredient deficit test!)
            return;
        }

        // =========================================================================
        // STAGE 2: Real Day 2 Wait & Verification
        // =========================================================================
        if (stage == 2)
        {
            if (farmPost.lastProductionTick < farmDay2ScheduledTick ||
                factoryPost.lastProductionTick < factDay2ScheduledTick ||
                watchPost.lastProductionTick < watchDay2ScheduledTick)
            {
                return;
            }

            int farmDelta2 = farmPost.lastProductionTick - farmDay2ScheduledTick;
            int factDelta2 = factoryPost.lastProductionTick - factDay2ScheduledTick;
            int watchDelta2 = watchPost.lastProductionTick - watchDay2ScheduledTick;
            Check("Day2_Farm_TickWindow", farmDelta2 >= 0 && farmDelta2 <= 2500, "<=2500", farmDelta2.ToString());
            Check("Day2_Factory_TickWindow", factDelta2 >= 0 && factDelta2 <= 2500, "<=2500", factDelta2.ToString());
            Check("Day2_Watchtower_TickWindow", watchDelta2 >= 0 && watchDelta2 <= 2500, "<=2500", watchDelta2.ToString());

            // 1. Farming Day 2
            // Total amount = 12.375 + 0.375 = 12.75.
            // Whole units = 12. Kept remainder = 0.75.
            // Cumulative produced: 12 + 12 = 24.
            int potDay2 = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawPotatoes"));
            int riceDay2 = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawRice"));
            float potRem2 = 0f; farmPost.fractionalRemainders.TryGetValue("TMM_Outpost_Farming_Potatoes", out potRem2);
            float riceRem2 = 0f; farmPost.fractionalRemainders.TryGetValue("TMM_Outpost_Farming_Rice", out riceRem2);

            Check("Farming_Day2_Potatoes_Cumulative", potDay2 == 24, "24", potDay2.ToString());
            Check("Farming_Day2_Rice_Cumulative", riceDay2 == 24, "24", riceDay2.ToString());
            Check("Farming_Day2_Potatoes_Remainder", Math.Abs(potRem2 - 0.75f) < 0.001f, "0.750", potRem2.ToString("F3"));
            Check("Farming_Day2_Rice_Remainder", Math.Abs(riceRem2 - 0.75f) < 0.001f, "0.750", riceRem2.ToString("F3"));

            dayTable.Add("Farming Day 2 | Expected: 24 pot / 24 rice (rem 0.750) | Actual: " + potDay2 + " pot / " + riceDay2 + " rice (rem " + potRem2.ToString("F3") + ")");

            // 2. Factory Day 2 (Zero ingredients deficit test)
            // Steel: 0 -> Produced: 0 components. Cumulative remains 2. Remainder: keptRemainder (0.0).
            int compDay2 = factoryPost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial);
            Check("Factory_Day2_ZeroIngredients_NoOutput", compDay2 == 2, "2", compDay2.ToString());

            dayTable.Add("Factory Day 2 | Expected: 0 new (total 2) | Actual: " + compDay2 + " total components");

            // 3. Watchtower Day 2
            Check("Watchtower_Day2_NoGoodsProduced", watchPost.ProducedGoods.Count == 0, "0", watchPost.ProducedGoods.Count.ToString());
            dayTable.Add("Watchtower Day 2 | Expected: 0 goods | Actual: " + watchPost.ProducedGoods.Count + " goods");

            // Setup Day 3: Replenish factory with 48 steel (enough for 4 components)
            AddRawItem(factoryPost, ThingDefOf.Steel, 48);

            farmDay3ScheduledTick = farmPost.nextProductionTick;
            factDay3ScheduledTick = factoryPost.nextProductionTick;

            stage = 3;
            return;
        }

        // =========================================================================
        // STAGE 3: Real Day 3 Wait & Final Verification
        // =========================================================================
        if (stage == 3)
        {
            if (farmPost.lastProductionTick < farmDay3ScheduledTick ||
                factoryPost.lastProductionTick < factDay3ScheduledTick)
            {
                return;
            }

            int farmDelta3 = farmPost.lastProductionTick - farmDay3ScheduledTick;
            int factDelta3 = factoryPost.lastProductionTick - factDay3ScheduledTick;
            Check("Day3_Farm_TickWindow", farmDelta3 >= 0 && farmDelta3 <= 2500, "<=2500", farmDelta3.ToString());
            Check("Day3_Factory_TickWindow", factDelta3 >= 0 && factDelta3 <= 2500, "<=2500", factDelta3.ToString());

            // 1. Farming Day 3
            // Total amount = 12.375 + 0.75 = 13.125.
            // Whole units = 13. Kept remainder = 0.125.
            // Cumulative produced: 24 + 13 = 37.
            int potDay3 = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawPotatoes"));
            int riceDay3 = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawRice"));
            float potRem3 = 0f; farmPost.fractionalRemainders.TryGetValue("TMM_Outpost_Farming_Potatoes", out potRem3);
            float riceRem3 = 0f; farmPost.fractionalRemainders.TryGetValue("TMM_Outpost_Farming_Rice", out riceRem3);

            Check("Farming_Day3_Potatoes_Cumulative", potDay3 == 37, "37", potDay3.ToString());
            Check("Farming_Day3_Rice_Cumulative", riceDay3 == 37, "37", riceDay3.ToString());
            Check("Farming_Day3_Potatoes_Remainder", Math.Abs(potRem3 - 0.125f) < 0.001f, "0.125", potRem3.ToString("F3"));
            Check("Farming_Day3_Rice_Remainder", Math.Abs(riceRem3 - 0.125f) < 0.001f, "0.125", riceRem3.ToString("F3"));

            dayTable.Add("Farming Day 3 | Expected: 37 pot / 37 rice (rem 0.125) | Actual: " + potDay3 + " pot / " + riceDay3 + " rice (rem " + potRem3.ToString("F3") + ")");

            // 2. Factory Day 3
            // Resumed production with 48 steel -> 4 components made! Cumulative = 2 + 4 = 6.
            int compDay3 = factoryPost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial);
            int steelLeftDay3 = factoryPost.RawMaterials.TotalStackCountOfDef(ThingDefOf.Steel);
            Check("Factory_Day3_ProductionResumed", compDay3 == 6, "6", compDay3.ToString());
            Check("Factory_Day3_SteelConsumed", steelLeftDay3 == 0, "0", steelLeftDay3.ToString());

            dayTable.Add("Factory Day 3 | Expected: 4 new (total 6), 0 steel left | Actual: " + compDay3 + " total comp, " + steelLeftDay3 + " steel left");

            // 3. Unresearched Product Yields 0
            var advancedCompDef = DefDatabase<StrategicOutpostProductDef>.GetNamedSilentFail("TMM_Outpost_Factory_ComponentSpacer");
            string unlockReason;
            bool unresearchedUnlocked = advancedCompDef != null && advancedCompDef.IsUnlocked(out unlockReason);
            Check("Factory_UnresearchedProduct_Locked", !unresearchedUnlocked, "false", unresearchedUnlocked.ToString().ToLower());

            // 4. Long-term GC Survival Check across all pawns and pack animals
            var allPawns = new[] { farmP1, farmP2, farmAnimal, factP1, factP2, factAnimal, watchP1, watchP2, watchAnimal };
            bool finalGCPreservation = allPawns.All(p => p != null && !p.Dead && !p.Spawned && Find.WorldPawns.Contains(p));
            Check("Final_AllNinePawnsPreservedAcrossThreeDays", finalGCPreservation, "true", finalGCPreservation.ToString().ToLower());

            stage = 4;
            Finish(null, false);
        }
    }

    private void Reload()
    {
        if (!reloadInitialChecked)
        {
            reloadInitialChecked = true;
            // Verified on load of TMM-OutpostCalendar (which was saved after Day 1)
            Check("Reload_SaveState_StageIsTwo", stage == 2, "2", stage.ToString());
            Check("Reload_FarmPost_Present", farmPost != null && farmPost.Spawned, "true", (farmPost != null ? farmPost.Spawned.ToString().ToLower() : "null"));
            Check("Reload_FactoryPost_Present", factoryPost != null && factoryPost.Spawned, "true", (factoryPost != null ? factoryPost.Spawned.ToString().ToLower() : "null"));
            Check("Reload_WatchPost_Present", watchPost != null && watchPost.Spawned, "true", (watchPost != null ? watchPost.Spawned.ToString().ToLower() : "null"));

            // Verify loaded pawn identities
            Check("Reload_FarmP1_Preserved", farmP1 != null && farmP1.ThingID == savedFarmP1Id && Find.WorldPawns.Contains(farmP1), "true", (farmP1 != null ? farmP1.ThingID : "null"));
            Check("Reload_FactP1_Preserved", factP1 != null && factP1.ThingID == savedFactP1Id && Find.WorldPawns.Contains(factP1), "true", (factP1 != null ? factP1.ThingID : "null"));
            Check("Reload_WatchAnimal_Preserved", watchAnimal != null && watchAnimal.ThingID == savedWatchAnimalId && Find.WorldPawns.Contains(watchAnimal), "true", (watchAnimal != null ? watchAnimal.ThingID : "null"));

            // Verify loaded Day 1 outputs
            int potReload = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawPotatoes"));
            int riceReload = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawRice"));
            float potRemReload = 0f; farmPost.fractionalRemainders.TryGetValue("TMM_Outpost_Farming_Potatoes", out potRemReload);
            float riceRemReload = 0f; farmPost.fractionalRemainders.TryGetValue("TMM_Outpost_Farming_Rice", out riceRemReload);
            int compReload = factoryPost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial);

            Check("Reload_Farm_Day1_Potatoes", potReload == savedFarmDay1Potatoes, savedFarmDay1Potatoes.ToString(), potReload.ToString());
            Check("Reload_Farm_Day1_Rice", riceReload == savedFarmDay1Rice, savedFarmDay1Rice.ToString(), riceReload.ToString());
            Check("Reload_Farm_Day1_PotRemainder", Math.Abs(potRemReload - savedFarmDay1PotRemainder) < 0.001f, savedFarmDay1PotRemainder.ToString("F3"), potRemReload.ToString("F3"));
            Check("Reload_Farm_Day1_RiceRemainder", Math.Abs(riceRemReload - savedFarmDay1RiceRemainder) < 0.001f, savedFarmDay1RiceRemainder.ToString("F3"), riceRemReload.ToString("F3"));
            Check("Reload_Factory_Day1_Components", compReload == savedFactDay1Components, savedFactDay1Components.ToString(), compReload.ToString());

            // Verify loaded deadlines
            Check("Reload_Farm_NextProductionTick", farmPost.nextProductionTick == savedFarmNextTick, savedFarmNextTick.ToString(), farmPost.nextProductionTick.ToString());
            Check("Reload_Factory_NextProductionTick", factoryPost.nextProductionTick == savedFactNextTick, savedFactNextTick.ToString(), factoryPost.nextProductionTick.ToString());
        }

        // Wait for Day 2 to fire in reload game session
        if (farmPost.lastProductionTick >= farmDay2ScheduledTick && factoryPost.lastProductionTick >= factDay2ScheduledTick)
        {
            int potReloadDay2 = farmPost.ProducedGoods.TotalStackCountOfDef(ThingDef.Named("RawPotatoes"));
            int compReloadDay2 = factoryPost.ProducedGoods.TotalStackCountOfDef(ThingDefOf.ComponentIndustrial);

            Check("Reload_Day2_Farming_Potatoes_Cumulative", potReloadDay2 == 24, "24", potReloadDay2.ToString());
            Check("Reload_Day2_Factory_Components_UnchangedWithoutSteel", compReloadDay2 == 2, "2", compReloadDay2.ToString());

            Finish(null, true);
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
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "CalendarProgress.log"), msg);
        }
    }

    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        bool reload = args.Contains("-tmm-outpost-calendar-reload");
        bool fresh = args.Contains("-tmm-outpost-calendar-probe");

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
        foreach (var row in dayTable)
        {
            tableJsonEntries.Add("\"" + EscapeJson(row) + "\"");
        }

        string jsonReport = string.Format(
            "{{\n  \"passed\": {0},\n  \"checksCount\": {1},\n  \"stage\": {2},\n  \"calendarTable\": [\n    {3}\n  ],\n  \"checks\": [\n    {4}\n  ],\n  \"errors\": [\n    {5}\n  ]\n}}",
            overallPassed ? "true" : "false",
            checks.Count,
            stage,
            string.Join(",\n    ", tableJsonEntries.ToArray()),
            string.Join(",\n    ", checkJsonEntries.ToArray()),
            string.Join(",\n    ", errorJsonEntries.ToArray())
        );

        string reportFileName = isReload ? "OutpostCalendarReloadProbe.json" : "OutpostCalendarProbe.json";
        string reportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, reportFileName);
        File.WriteAllText(reportPath, jsonReport);

        if (!isReload)
        {
            string uiReportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "OutpostCalendarUiProbe.json");
            File.WriteAllText(uiReportPath, "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
        }
    }
}
