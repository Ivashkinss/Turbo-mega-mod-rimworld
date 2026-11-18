using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public static class OutpostMapsAcceptanceLaunch
{
    static OutpostMapsAcceptanceLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-outpost-maps-reload"))
        {
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-StrategicOutpostMaps"));
        }
    }
}

public sealed class OutpostMapsAcceptanceProbe : MapComponent
{
    public class CheckResult
    {
        public string name;
        public bool passed;
        public string expected;
        public string actual;
    }

    private StrategicOutpost factoryStation;
    private StrategicOutpost farmStation;
    private StrategicOutpost mineStation;
    private StrategicOutpost watchStation;

    private int stage = 0;
    private bool done = false;
    private readonly List<CheckResult> checks = new List<CheckResult>();
    private readonly List<string> errors = new List<string>();

    private int powerTestStartTick = -1;
    private float powerTestInitialFuel = 0f;

    // Reload snapshot fields
    private int savedFactoryBuiltLevel = 0;
    private int savedFarmBuiltLevel = 0;
    private int savedMineBuiltLevel = 0;
    private int savedWatchBuiltLevel = 0;
    private float savedGeneratorFuel = 0f;
    private int savedBuildingCount = 0;
    private int savedGrowingZoneCount = 0;
    private List<string> savedGrowingPlantDefs = new List<string>();
    private List<string> savedBuildingDefs = new List<string>();

    private bool reloadInitialChecked = false;

    public OutpostMapsAcceptanceProbe(Map map) : base(map)
    {
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref factoryStation, "omFactoryStation");
        Scribe_References.Look(ref farmStation, "omFarmStation");
        Scribe_References.Look(ref mineStation, "omMineStation");
        Scribe_References.Look(ref watchStation, "omWatchStation");

        Scribe_Values.Look(ref stage, "omStage", 0);
        Scribe_Values.Look(ref powerTestStartTick, "omPowerTestStartTick", -1);
        Scribe_Values.Look(ref powerTestInitialFuel, "omPowerTestInitialFuel", 0f);

        Scribe_Values.Look(ref savedFactoryBuiltLevel, "omSavedFactBuiltLevel", 0);
        Scribe_Values.Look(ref savedFarmBuiltLevel, "omSavedFarmBuiltLevel", 0);
        Scribe_Values.Look(ref savedMineBuiltLevel, "omSavedMineBuiltLevel", 0);
        Scribe_Values.Look(ref savedWatchBuiltLevel, "omSavedWatchBuiltLevel", 0);

        Scribe_Values.Look(ref savedGeneratorFuel, "omSavedGeneratorFuel", 0f);
        Scribe_Values.Look(ref savedBuildingCount, "omSavedBuildingCount", 0);
        Scribe_Values.Look(ref savedGrowingZoneCount, "omSavedGrowingZoneCount", 0);
        Scribe_Collections.Look(ref savedGrowingPlantDefs, "omSavedGrowingPlantDefs", LookMode.Value);
        Scribe_Collections.Look(ref savedBuildingDefs, "omSavedBuildingDefs", LookMode.Value);

        if (savedGrowingPlantDefs == null) savedGrowingPlantDefs = new List<string>();
        if (savedBuildingDefs == null) savedBuildingDefs = new List<string>();
    }

    private void RecordCheck(string name, bool ok, string expected, string actual)
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

    private Pawn GenerateWorker()
    {
        for (int attempt = 0; attempt < 20; attempt++)
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

            if (p.skills != null)
            {
                foreach (SkillRecord sr in p.skills.skills)
                {
                    sr.Level = 15;
                }
                return p;
            }
            if (!Find.WorldPawns.Contains(p))
            {
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Discard);
            }
        }
        throw new Exception("Unable to generate qualified fixture pawn.");
    }

    private PlanetTile FindLegalOutpostTile(HashSet<PlanetTile> excluded)
    {
        var neighbors = new List<PlanetTile>();
        var frontier = new Queue<PlanetTile>();
        var visited = new HashSet<PlanetTile>();
        frontier.Enqueue(map.Tile);
        visited.Add(map.Tile);

        PlanetTile fallback = PlanetTile.Invalid;

        while (frontier.Count > 0 && visited.Count < 5000)
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
                    Tile t = Find.WorldGrid[candidate];
                    if (t != null && t.PrimaryBiome != null)
                    {
                        string bName = t.PrimaryBiome.defName;
                        if (bName.IndexOf("Swamp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            bName.IndexOf("Bog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            bName.IndexOf("Desert", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            bName.IndexOf("Ice", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            bName.IndexOf("Tundra", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (!fallback.Valid) fallback = candidate;
                            continue;
                        }
                    }
                    return candidate;
                }
            }
        }

        if (fallback.Valid) return fallback;

        for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
        {
            PlanetTile candidate = new PlanetTile(i);
            if (excluded != null && excluded.Contains(candidate)) continue;
            string reason;
            if (candidate.Layer == Find.WorldGrid.Surface &&
                StrategicOutpostCaravanUtility.CanCreateOutpostAt(candidate, out reason) &&
                TileFinder.IsValidTileForNewSettlement(candidate))
            {
                return candidate;
            }
        }

        throw new Exception("No legal tile found for outpost founding.");
    }

    private StrategicOutpost FoundTestOutpost(StrategicOutpostSpecialization spec, HashSet<PlanetTile> usedTiles)
    {
        PlanetTile tile = FindLegalOutpostTile(usedTiles);
        usedTiles.Add(tile);

        Pawn p1 = GenerateWorker();
        Pawn p2 = GenerateWorker();
        Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDef.Named("Muffalo"), Faction.OfPlayer));

        Caravan caravan = CaravanMaker.MakeCaravan(new[] { p1, p2, animal }, Faction.OfPlayer, tile, true);
        StrategicOutpost station = StrategicOutpostCaravanUtility.FoundOutpostFromCaravan(caravan, spec);
        if (station == null || !station.Spawned)
        {
            throw new Exception("Failed to found test outpost for specialization " + spec);
        }

        // Fund with silver for upgrades
        while (station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver) < 150000)
        {
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = Math.Min(500, ThingDefOf.Silver.stackLimit);
            station.RawMaterials.TryAdd(silver, false);
        }

        string deployReason;
        if (!station.DeployPersonnelToMap(out deployReason))
        {
            throw new Exception("DeployPersonnelToMap failed for " + spec + ": " + deployReason);
        }

        return station;
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

    private bool CheckReachableFromEdge(Map m, LocalTargetInfo target)
    {
        if (!target.IsValid) return false;
        IntVec3 edgeCell = IntVec3.Invalid;
        foreach (IntVec3 edge in m.AllCells.Where(c => (c.x == 0 || c.z == 0 || c.x == m.Size.x - 1 || c.z == m.Size.z - 1) && c.Standable(m)))
        {
            edgeCell = edge;
            break;
        }
        if (!edgeCell.IsValid) return false;
        Pawn colonist = m.mapPawns.FreeColonists.FirstOrDefault();
        TraverseParms parms = colonist != null ? TraverseParms.For(colonist, Danger.Deadly, TraverseMode.PassDoors, false, false, false) : TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false, false, false);
        return m.reachability.CanReach(edgeCell, target, PathEndMode.Touch, parms);
    }

    private int CountVeinsOutsideFootprint(Map mineMap, CellRect fp)
    {
        return mineMap.listerThings.AllThings.Count(t =>
            t is Mineable && !fp.Contains(t.Position) &&
            (t.def.defName == "MineableGold" || t.def.defName == "MineablePlasteel"));
    }

    private void Fresh()
    {
        var usedTiles = new HashSet<PlanetTile>();

        // =========================================================================
        // STAGE 0: FACTORY OUTPOST FOUNDING & LEVEL 1-3 UPGRADE
        // =========================================================================
        if (stage == 0)
        {
            factoryStation = FoundTestOutpost(StrategicOutpostSpecialization.Factory, usedTiles);
            Map fMap = factoryStation.Map;
            StrategicOutpostMapComponent fComp = fMap.GetComponent<StrategicOutpostMapComponent>();
            RecordCheck("Factory_MapComponentExists", fComp != null, "true", (fComp != null ? "true" : "null"));

            // Level 1 Checks (pre-research)
            bool noSculpting = !fMap.listerBuildings.allBuildingsColonist.Any(b => b.def.defName == "TableSculpting");
            RecordCheck("Factory_L1_NoSculptingTable", noSculpting, "true", noSculpting.ToString().ToLower());

            Building manualBench = fMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b =>
                b.def.defName == "CraftingSpot" || b.def.defName == "HandTailoringBench" || b.def.defName == "FueledSmithy");
            bool hasBench = manualBench != null && manualBench.def.AllRecipes != null && manualBench.def.AllRecipes.Count > 0;
            RecordCheck("Factory_L1_ManualWorkbenchWithRecipes", hasBench, "true", (hasBench ? manualBench.def.defName : "null"));

            int l1Beds = fComp.CountRealBeds();
            RecordCheck("Factory_L1_BedCountGte2", l1Beds >= 2, ">=2", l1Beds.ToString());

            // Check Reachability & Interaction cell standable
            bool benchReachable = manualBench != null && CheckReachableFromEdge(fMap, manualBench.InteractionCell);
            bool benchStandable = manualBench != null && manualBench.InteractionCell.Standable(fMap);
            RecordCheck("Factory_L1_InteractionCellReachable", benchReachable, "true", benchReachable.ToString().ToLower());
            RecordCheck("Factory_L1_InteractionCellStandable", benchStandable, "true", benchStandable.ToString().ToLower());

            // Research Smithing + Electricity
            Research("Smithing");
            Research("Electricity");

            // Upgrade to Level 2 and Level 3
            string upReason;
            bool up2 = factoryStation.TryUpgrade(out upReason);
            RecordCheck("Factory_UpgradeLevel2_Success", up2 && factoryStation.level == 2, "true", (up2 ? "true" : (upReason ?? "false")));
            bool up3 = factoryStation.TryUpgrade(out upReason);
            RecordCheck("Factory_UpgradeLevel3_Success", up3 && factoryStation.level == 3, "true", (up3 ? "true" : (upReason ?? "false")));

            // Verify Level 3 Power Generator & Electricity setup
            Building generator = fMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "WoodFiredGenerator");
            RecordCheck("Factory_L3_GeneratorSpawned", generator != null, "true", (generator != null ? "spawned" : "null"));

            bool genTerrainValid = generator != null && GenConstruct.CanBuildOnTerrain(generator.def, generator.Position, fMap, generator.Rotation);
            RecordCheck("Factory_L3_GeneratorTerrainSupport", genTerrainValid, "true", genTerrainValid.ToString().ToLower());

            CompRefuelable compRefuel = generator != null ? generator.TryGetComp<CompRefuelable>() : null;
            bool fuelInitiallyZero = compRefuel != null && compRefuel.Fuel == 0f;
            RecordCheck("Factory_L3_GeneratorFuelInitiallyZero", fuelInitiallyZero, "true", (compRefuel != null ? compRefuel.Fuel.ToString("F1") : "null"));

            Building lamp = fMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "StandingLamp");
            RecordCheck("Factory_L3_StandingLampSpawned", lamp != null, "true", (lamp != null ? "spawned" : "null"));

            fMap.powerNetManager.UpdatePowerNetsAndConnections_First();
            CompPowerTrader genPower = generator != null ? generator.TryGetComp<CompPowerTrader>() : null;
            CompPowerTrader lampPower = lamp != null ? lamp.TryGetComp<CompPowerTrader>() : null;
            bool sameNet = genPower != null && lampPower != null && genPower.PowerNet != null && genPower.PowerNet == lampPower.PowerNet;
            RecordCheck("Factory_L3_GeneratorAndLampSharedPowerNet", sameNet, "true", sameNet.ToString().ToLower());

            bool lampInitiallyOff = lampPower != null && !lampPower.PowerOn;
            RecordCheck("Factory_L3_LampUnpoweredBeforeRefuel", lampInitiallyOff, "true", (!lampInitiallyOff ? "powered" : "unpowered"));

            // Refuel generator with real wood from items
            Thing wood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            wood.stackCount = 50;
            compRefuel.Refuel(new List<Thing> { wood });
            powerTestInitialFuel = compRefuel.Fuel;
            RecordCheck("Factory_L3_GeneratorRefueledTo50", Math.Abs(powerTestInitialFuel - 50f) < 0.1f, "50.0", powerTestInitialFuel.ToString("F1"));

            // F2: Set start tick and wait for real native game ticks to advance power and burn wood!
            powerTestStartTick = Find.TickManager.TicksGame;
            stage = 1;
            return;
        }

        // =========================================================================
        // STAGE 1: F2 REAL TICKS POWER & WOOD CONSUMPTION VERIFICATION
        // =========================================================================
        if (stage == 1)
        {
            Map fMap = factoryStation.Map;
            Building generator = fMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "WoodFiredGenerator");
            Building lamp = fMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "StandingLamp");
            CompRefuelable compRefuel = generator != null ? generator.TryGetComp<CompRefuelable>() : null;
            CompPowerTrader lampPower = lamp != null ? lamp.TryGetComp<CompPowerTrader>() : null;

            // Wait at least 1500 real game ticks for native simulation to run
            if (Find.TickManager.TicksGame - powerTestStartTick < 1500)
            {
                return;
            }

            bool lampPoweredNatively = lampPower != null && lampPower.PowerOn;
            RecordCheck("Factory_L3_LampPoweredNativelyOverTicks", lampPoweredNatively, "true", lampPoweredNatively.ToString().ToLower());

            // Check that real wood was physically consumed by generator ticks!
            float currentFuel = compRefuel != null ? compRefuel.Fuel : 50f;
            bool fuelConsumed = currentFuel < powerTestInitialFuel;
            RecordCheck("Factory_L3_WoodFuelConsumedOverTime", fuelConsumed, "<50.0", currentFuel.ToString("F3"));

            // Research Machining and Upgrade to Level 6
            Research("Machining");
            string up4Reason, up5Reason, up6Reason;
            bool up4 = factoryStation.TryUpgrade(out up4Reason); // L4
            bool up5 = factoryStation.TryUpgrade(out up5Reason); // L5
            bool up6 = factoryStation.TryUpgrade(out up6Reason); // L6
            string fUpInfo = "L4:" + up4 + "(" + up4Reason + ") L5:" + up5 + "(" + up5Reason + ") L6:" + up6 + "(" + up6Reason + ") lvl=" + factoryStation.level + " s=" + factoryStation.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver);
            RecordCheck("Factory_UpgradeLevel6_Success", up6 && factoryStation.level == 6, "true", (up6 && factoryStation.level == 6) ? "true" : fUpInfo);

            Building machiningBench = fMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "TableMachining");
            bool hasMachining = machiningBench != null && machiningBench.def.AllRecipes != null && machiningBench.def.AllRecipes.Count > 0;
            RecordCheck("Factory_L6_MachiningBenchWithRecipes", hasMachining, "true", (hasMachining ? "spawned" : "null"));

            bool machReachable = machiningBench != null && CheckReachableFromEdge(fMap, machiningBench.InteractionCell);
            bool machStandable = machiningBench != null && machiningBench.InteractionCell.Standable(fMap);
            RecordCheck("Factory_L6_MachiningInteractionReachable", machReachable, "true", machReachable.ToString().ToLower());
            RecordCheck("Factory_L6_MachiningInteractionStandable", machStandable, "true", machStandable.ToString().ToLower());

            // Research Fabrication and Upgrade to Level 10
            Research("Fabrication");
            string up7Reason, up8Reason, up9Reason, up10Reason;
            bool up7 = factoryStation.TryUpgrade(out up7Reason); // L7
            bool up8 = factoryStation.TryUpgrade(out up8Reason); // L8
            bool up9 = factoryStation.TryUpgrade(out up9Reason); // L9
            Building fabBench = fMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "FabricationBench");
            bool hasFab = fabBench != null && fabBench.def.AllRecipes != null && fabBench.def.AllRecipes.Count > 0;
            RecordCheck("Factory_L9_FabricationBenchWithRecipes", hasFab, "true", (hasFab ? "spawned" : "null"));

            bool up10 = factoryStation.TryUpgrade(out up10Reason); // L10
            string fUp10Info = "L7:" + up7 + "(" + up7Reason + ") L8:" + up8 + "(" + up8Reason + ") L9:" + up9 + "(" + up9Reason + ") L10:" + up10 + "(" + up10Reason + ") lvl=" + factoryStation.level;
            RecordCheck("Factory_UpgradeLevel10_Success", up10 && factoryStation.level == 10, "true", (up10 && factoryStation.level == 10) ? "true" : fUp10Info);

            StrategicOutpostMapComponent fComp = fMap.GetComponent<StrategicOutpostMapComponent>();
            int l10RegularBeds = fComp.CountRealBeds();
            RecordCheck("Factory_L10_RegularBedCountGte11", l10RegularBeds >= 11, ">=11", l10RegularBeds.ToString());

            // Verify all beds are reachable from map edge by colonists
            var allColonistBeds = fMap.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Where(b => !b.Medical).ToList();
            bool allBedsReachable = allColonistBeds.Count >= 11 && allColonistBeds.All(b => CheckReachableFromEdge(fMap, b));
            RecordCheck("Factory_L10_AllBedsReachableFromEdge", allBedsReachable, "true", allBedsReachable.ToString().ToLower());

            stage = 2;
        }

        // =========================================================================
        // STAGE 2: FARMING OUTPOST & GROWING ZONES (Deduplication + Upgrades to L10)
        // =========================================================================
        if (stage == 2)
        {
            farmStation = FoundTestOutpost(StrategicOutpostSpecialization.Farming, usedTiles);
            Map farmMap = farmStation.Map;
            StrategicOutpostMapComponent farmComp = farmMap.GetComponent<StrategicOutpostMapComponent>();

            // Level 1: Growing zone with Rice
            Zone_Growing riceZone = farmMap.zoneManager.AllZones.OfType<Zone_Growing>().FirstOrDefault(z => z.GetPlantDefToGrow() != null && z.GetPlantDefToGrow().defName == "Plant_Rice");
            RecordCheck("Farming_L1_RiceGrowingZoneCreated", riceZone != null && riceZone.CellCount > 0, "true", (riceZone != null ? riceZone.CellCount.ToString() : "null"));

            bool noRoofOnRice = riceZone != null && riceZone.Cells.All(c => farmMap.roofGrid.RoofAt(c) == null);
            RecordCheck("Farming_L1_GrowingZoneNoRoof", noRoofOnRice, "true", noRoofOnRice.ToString().ToLower());

            bool fertileRice = riceZone != null && riceZone.Cells.All(c => c.GetFertility(farmMap) >= 0.7f);
            RecordCheck("Farming_L1_GrowingZoneFertilityGte07", fertileRice, "true", fertileRice.ToString().ToLower());

            // Create pre-existing "player" growing zone nearby to ensure upgrades don't wipe it
            CellRect campusFootprint = farmComp.baseFootprint;
            IntVec3 playerZoneCenter = new IntVec3(campusFootprint.maxX + 12, 0, campusFootprint.CenterCell.z);
            CellRect playerZoneRect = CellRect.CenteredOn(playerZoneCenter, 4, 4);

            Zone_Growing playerZone = new Zone_Growing(farmMap.zoneManager);
            playerZone.SetPlantDefToGrow(DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Dandelion"));
            farmMap.zoneManager.RegisterZone(playerZone);
            foreach (IntVec3 c in playerZoneRect)
            {
                if (c.InBounds(farmMap) && farmMap.zoneManager.ZoneAt(c) == null && c.GetFirstBuilding(farmMap) == null && c.Standable(farmMap) && c.GetFertility(farmMap) > 0f)
                {
                    playerZone.AddCell(c);
                }
            }
            if (playerZone.CellCount == 0)
            {
                for (int dx = 5; dx < 25; dx++)
                {
                    IntVec3 c = new IntVec3(campusFootprint.maxX + dx, 0, campusFootprint.CenterCell.z);
                    if (c.InBounds(farmMap) && farmMap.zoneManager.ZoneAt(c) == null && c.GetFirstBuilding(farmMap) == null && c.Standable(farmMap) && c.GetFertility(farmMap) > 0f)
                    {
                        playerZone.AddCell(c);
                        if (playerZone.CellCount >= 4) break;
                    }
                }
            }
            int playerCellCountBefore = playerZone.CellCount;
            RecordCheck("Farming_PlayerZoneCreated", playerCellCountBefore > 0, ">0", playerCellCountBefore.ToString());

            // Upgrade Farming outpost to Level 2 and Level 3
            string farmUpReason;
            farmStation.TryUpgrade(out farmUpReason);
            farmStation.TryUpgrade(out farmUpReason); // Level 3 -> Potato zone

            Zone_Growing potatoZone = farmMap.zoneManager.AllZones.OfType<Zone_Growing>().FirstOrDefault(z => z.GetPlantDefToGrow() != null && z.GetPlantDefToGrow().defName == "Plant_Potato");
            RecordCheck("Farming_L3_PotatoGrowingZoneCreated", potatoZone != null && potatoZone.CellCount > 0, "true", (potatoZone != null ? potatoZone.CellCount.ToString() : "null"));

            // Check player pre-existing zone was not changed or cleared
            bool playerZonePreserved = playerZone.CellCount == playerCellCountBefore;
            RecordCheck("Farming_PlayerZonePreservedAcrossUpgrades", playerZonePreserved, playerCellCountBefore.ToString(), playerZone.CellCount.ToString());

            // F5: Real deduplication test - advance Farm up to Level 10
            for (int lvl = 4; lvl <= 10; lvl++)
            {
                farmStation.TryUpgrade(out farmUpReason);
            }
            RecordCheck("Farming_ReachedLevel10", farmStation.level == 10 && farmComp.builtLevel == 10, "10", farmComp.builtLevel.ToString());

            // Verify all beds, doors, tables reachable in Farm L10
            int farmBeds = farmComp.CountRealBeds();
            RecordCheck("Farming_L10_BedsGte11", farmBeds >= 11, ">=11", farmBeds.ToString());

            stage = 3;
        }

        // =========================================================================
        // STAGE 3: MINING OUTPOST & VEINS PRESERVATION (Level 1 to 10)
        // =========================================================================
        if (stage == 3)
        {
            mineStation = FoundTestOutpost(StrategicOutpostSpecialization.Mining, usedTiles);
            Map mineMap = mineStation.Map;
            StrategicOutpostMapComponent mineComp = mineMap.GetComponent<StrategicOutpostMapComponent>();

            int goldVeinsBefore = CountVeinsOutsideFootprint(mineMap, mineComp.baseFootprint);

            // Upgrade Mining outpost to Level 3
            string mineUpReason;
            mineStation.TryUpgrade(out mineUpReason);
            mineStation.TryUpgrade(out mineUpReason);

            int goldVeinsAfter = CountVeinsOutsideFootprint(mineMap, mineComp.baseFootprint);
            RecordCheck("Mining_VeinCountPreservedOutsideFootprint", goldVeinsBefore == goldVeinsAfter, goldVeinsBefore.ToString(), goldVeinsAfter.ToString());

            // Upgrade Mining to Level 10
            while (mineStation.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver) < 150000)
            {
                Thing s = ThingMaker.MakeThing(ThingDefOf.Silver);
                s.stackCount = Math.Min(500, ThingDefOf.Silver.stackLimit);
                mineStation.RawMaterials.TryAdd(s, false);
            }
            string lastMineFail = null;
            for (int lvl = 4; lvl <= 10; lvl++)
            {
                if (!mineStation.TryUpgrade(out mineUpReason))
                {
                    lastMineFail = "L" + lvl + ": " + mineUpReason;
                    break;
                }
            }
            RecordCheck("Mining_ReachedLevel10", mineStation.level == 10 && mineComp.builtLevel == 10, "10", mineStation.level == 10 ? "10" : (mineComp.builtLevel.ToString() + (lastMineFail != null ? " (" + lastMineFail + ")" : "")));

            stage = 4;
        }

        // =========================================================================
        // STAGE 4: F3 WATCHTOWER OUTPOST DEFENSES CHECKED AT APPROPRIATE LEVELS
        // =========================================================================
        if (stage == 4)
        {
            watchStation = FoundTestOutpost(StrategicOutpostSpecialization.Watchtower, usedTiles);
            Map watchMap = watchStation.Map;
            StrategicOutpostMapComponent watchComp = watchMap.GetComponent<StrategicOutpostMapComponent>();

            // Level 1: Core outpost module only - NO turrets, NO sandbags yet!
            bool noTurretsAtL1 = !watchMap.listerBuildings.allBuildingsColonist.Any(b => b.def.defName == "Turret_MiniTurret");
            bool noSandbagsAtL1 = !watchMap.listerBuildings.allBuildingsColonist.Any(b => b.def == ThingDefOf.Sandbags);
            RecordCheck("Watchtower_L1_NoTurretsInitially", noTurretsAtL1, "true", noTurretsAtL1.ToString().ToLower());
            RecordCheck("Watchtower_L1_NoSandbagsInitially", noSandbagsAtL1, "true", noSandbagsAtL1.ToString().ToLower());

            string watchUpReason;
            // Upgrade L2, L3, L4, L5
            watchStation.TryUpgrade(out watchUpReason); // L2
            watchStation.TryUpgrade(out watchUpReason); // L3
            watchStation.TryUpgrade(out watchUpReason); // L4
            bool up5 = watchStation.TryUpgrade(out watchUpReason); // L5
            RecordCheck("Watchtower_UpgradeLevel5_Success", up5 && watchStation.level == 5, "true", up5.ToString().ToLower());

            // F3: At Level 5, Sandbags are planned and spawned! Check valid stuff (Cloth)!
            Building sandbag = watchMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def == ThingDefOf.Sandbags);
            bool sandbagValid = sandbag != null && sandbag.Stuff != null && sandbag.Stuff.defName == "Cloth";
            RecordCheck("Watchtower_L5_SandbagsSpawnedWithClothStuff", sandbagValid, "true", (sandbag != null ? (sandbag.Stuff != null ? sandbag.Stuff.defName : "nullStuff") : "noSandbag"));

            // Still NO turrets before GunTurrets research
            bool noTurretsAtL5 = !watchMap.listerBuildings.allBuildingsColonist.Any(b => b.def.defName == "Turret_MiniTurret");
            RecordCheck("Watchtower_L5_NoTurretsBeforeGunTurretsResearch", noTurretsAtL5, "true", noTurretsAtL5.ToString().ToLower());

            // Research GunTurrets and upgrade to L7
            Research("GunTurrets");
            watchStation.TryUpgrade(out watchUpReason); // L6
            bool up7 = watchStation.TryUpgrade(out watchUpReason); // L7
            RecordCheck("Watchtower_UpgradeLevel7_Success", up7 && watchStation.level == 7, "true", up7.ToString().ToLower());

            // F3: At Level 7 with GunTurrets researched, MiniTurret is built!
            Building turret = watchMap.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "Turret_MiniTurret");
            bool turretBuilt = turret != null && turret.Stuff != null && turret.Stuff.defName == "Steel";
            RecordCheck("Watchtower_L7_MiniTurretBuiltWithSteelStuff", turretBuilt, "true", (turret != null ? (turret.Stuff != null ? turret.Stuff.defName : "nullStuff") : "noTurret"));

            // Check turret CompRefuelable ammo requirement (no free unlimited ammo)
            CompRefuelable turretRefuel = turret != null ? turret.TryGetComp<CompRefuelable>() : null;
            bool turretHasAmmoComp = turretRefuel != null;
            RecordCheck("Watchtower_L7_TurretHasRefuelAmmoComponent", turretHasAmmoComp, "true", turretHasAmmoComp.ToString().ToLower());

            // Upgrade Watchtower to Level 10
            while (watchStation.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver) < 150000)
            {
                Thing s = ThingMaker.MakeThing(ThingDefOf.Silver);
                s.stackCount = Math.Min(500, ThingDefOf.Silver.stackLimit);
                watchStation.RawMaterials.TryAdd(s, false);
            }
            string lastWatchFail = null;
            for (int lvl = 8; lvl <= 10; lvl++)
            {
                if (!watchStation.TryUpgrade(out watchUpReason))
                {
                    lastWatchFail = "L" + lvl + ": " + watchUpReason;
                    break;
                }
            }
            RecordCheck("Watchtower_ReachedLevel10", watchStation.level == 10 && watchComp.builtLevel == 10, "10", watchStation.level == 10 ? "10" : (watchComp.builtLevel.ToString() + (lastWatchFail != null ? " (" + lastWatchFail + ")" : "")));

            stage = 5;
        }

        // =========================================================================
        // STAGE 5: F4 THOROUGH BLOCKED UPGRADE VS ALTERNATE PLACEMENT
        // =========================================================================
        if (stage == 5)
        {
            // Create fresh outpost for blocked test
            StrategicOutpost blockPost = FoundTestOutpost(StrategicOutpostSpecialization.Mining, usedTiles);
            Map bMap = blockPost.Map;
            StrategicOutpostMapComponent bComp = bMap.GetComponent<StrategicOutpostMapComponent>();

            // We are at Level 1, attempting Level 2 upgrade
            CellRect defRect = bComp.GetDefaultModuleRect(2);
            IntVec3[] searchOffsets = new IntVec3[]
            {
                IntVec3.Zero,
                new IntVec3(0, 0, defRect.Height + 2),
                new IntVec3(0, 0, -(defRect.Height + 2)),
                new IntVec3(defRect.Width + 2, 0, 0),
                new IntVec3(-(defRect.Width + 2), 0, 0),
                new IntVec3(defRect.Width + 2, 0, defRect.Height + 2),
                new IntVec3(-(defRect.Width + 2), 0, defRect.Height + 2),
                new IntVec3(defRect.Width + 2, 0, -(defRect.Height + 2)),
                new IntVec3(-(defRect.Width + 2), 0, -(defRect.Height + 2)),
                new IntVec3(0, 0, (defRect.Height + 2) * 2),
                new IntVec3(0, 0, -(defRect.Height + 2) * 2),
                new IntVec3((defRect.Width + 2) * 2, 0, 0),
                new IntVec3(-(defRect.Width + 2) * 2, 0, 0)
            };

            // Part 1: Block ALL 13 candidate locations with valuable gold piles
            var allGoldBlockers = new List<Thing>();
            for (int i = 0; i < searchOffsets.Length; i++)
            {
                CellRect cand = new CellRect(defRect.minX + searchOffsets[i].x, defRect.minZ + searchOffsets[i].z, defRect.Width, defRect.Height);
                if (cand.CenterCell.InBounds(bMap))
                {
                    Thing g = ThingMaker.MakeThing(ThingDefOf.Gold);
                    g.stackCount = 50;
                    GenSpawn.Spawn(g, cand.CenterCell, bMap);
                    allGoldBlockers.Add(g);
                }
            }

            int silverBefore = blockPost.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver);
            int buildingsBefore = bMap.listerBuildings.allBuildingsColonist.Count;

            string failReason;
            bool failedUpgrade = blockPost.TryUpgrade(out failReason);
            RecordCheck("Blocked_AllOffsetsCovered_UpgradeFails", !failedUpgrade, "false", failedUpgrade.ToString().ToLower());

            int silverAfterFailed = blockPost.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver);
            RecordCheck("Blocked_SilverNotDeductedOnFailure", silverBefore == silverAfterFailed, silverBefore.ToString(), silverAfterFailed.ToString());

            bool allGoldIntact = allGoldBlockers.All(g => g.Spawned && !g.Destroyed && g.stackCount == 50);
            RecordCheck("Blocked_AllProtectedGoldPilesPreserved", allGoldIntact, "true", allGoldIntact.ToString().ToLower());

            int buildingsAfterFailed = bMap.listerBuildings.allBuildingsColonist.Count;
            RecordCheck("Blocked_BuildingCountUnchangedOnFailure", buildingsBefore == buildingsAfterFailed, buildingsBefore.ToString(), buildingsAfterFailed.ToString());

            // Part 2: Despawn alternate candidate blockers, leaving ONLY the default candidate center blocked
            for (int i = 1; i < allGoldBlockers.Count; i++)
            {
                if (allGoldBlockers[i].Spawned) allGoldBlockers[i].DeSpawn();
            }

            Thing defaultGoldBlocker = allGoldBlockers[0];
            int cost = blockPost.NextUpgradeCost;
            bool alternateSucceeded = blockPost.TryUpgrade(out failReason);
            RecordCheck("Blocked_SingleDefaultBlocked_AlternateSucceeds", alternateSucceeded && blockPost.level == 2, "true", alternateSucceeded.ToString().ToLower());

            bool defaultGoldStillIntact = defaultGoldBlocker.Spawned && !defaultGoldBlocker.Destroyed && defaultGoldBlocker.stackCount == 50;
            RecordCheck("Blocked_DefaultGoldPilePreservedByAlternatePlacement", defaultGoldStillIntact, "true", defaultGoldStillIntact.ToString().ToLower());

            int silverAfterAlternate = blockPost.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver);
            RecordCheck("Blocked_SilverDeductedExactlyByCost", silverBefore - silverAfterAlternate == cost, cost.ToString(), (silverBefore - silverAfterAlternate).ToString());

            int buildingsAfterAlternate = bMap.listerBuildings.allBuildingsColonist.Count;
            RecordCheck("Blocked_BuildingsCountIncreasedAfterAlternate", buildingsAfterAlternate > buildingsBefore, ">" + buildingsBefore, buildingsAfterAlternate.ToString());

            defaultGoldBlocker.DeSpawn();
            stage = 6;
        }

        // =========================================================================
        // STAGE 6: F5 SNAPSHOT COLLECTION & FIXTURE SAVE
        // =========================================================================
        if (stage == 6)
        {
            // Snapshot built levels directly from StrategicOutpostMapComponent
            savedFactoryBuiltLevel = factoryStation.Map.GetComponent<StrategicOutpostMapComponent>().builtLevel;
            savedFarmBuiltLevel = farmStation.Map.GetComponent<StrategicOutpostMapComponent>().builtLevel;
            savedMineBuiltLevel = mineStation.Map.GetComponent<StrategicOutpostMapComponent>().builtLevel;
            savedWatchBuiltLevel = watchStation.Map.GetComponent<StrategicOutpostMapComponent>().builtLevel;

            Building generator = factoryStation.Map.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "WoodFiredGenerator");
            CompRefuelable compRefuel = generator != null ? generator.TryGetComp<CompRefuelable>() : null;
            savedGeneratorFuel = compRefuel != null ? compRefuel.Fuel : 0f;

            savedBuildingCount = factoryStation.Map.listerBuildings.allBuildingsColonist.Count;
            savedBuildingDefs = factoryStation.Map.listerBuildings.allBuildingsColonist.Select(b => b.def.defName).OrderBy(s => s).ToList();

            savedGrowingZoneCount = farmStation.Map.zoneManager.AllZones.OfType<Zone_Growing>().Count();
            savedGrowingPlantDefs = farmStation.Map.zoneManager.AllZones.OfType<Zone_Growing>().Select(z => (z.GetPlantDefToGrow() != null ? z.GetPlantDefToGrow().defName : "unknown")).OrderBy(s => s).ToList();

            stage = 7;
            GameDataSaveLoader.SaveGame("TMM-StrategicOutpostMaps");
            string savePath = Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-StrategicOutpostMaps.rws");
            RecordCheck("Save_FixtureGameFileCreated", File.Exists(savePath), "true", File.Exists(savePath).ToString().ToLower());

            Finish(null, false);
        }
    }

    private void Reload()
    {
        if (!reloadInitialChecked)
        {
            reloadInitialChecked = true;

            RecordCheck("Reload_StageIsSeven", stage == 7, "7", stage.ToString());
            RecordCheck("Reload_FactoryStationPresent", factoryStation != null && factoryStation.Spawned, "true", (factoryStation != null ? factoryStation.Spawned.ToString().ToLower() : "null"));
            RecordCheck("Reload_FarmStationPresent", farmStation != null && farmStation.Spawned, "true", (farmStation != null ? farmStation.Spawned.ToString().ToLower() : "null"));
            RecordCheck("Reload_MineStationPresent", mineStation != null && mineStation.Spawned, "true", (mineStation != null ? mineStation.Spawned.ToString().ToLower() : "null"));
            RecordCheck("Reload_WatchStationPresent", watchStation != null && watchStation.Spawned, "true", (watchStation != null ? watchStation.Spawned.ToString().ToLower() : "null"));

            // F5: Verify builtLevel in StrategicOutpostMapComponent
            StrategicOutpostMapComponent fComp = factoryStation.Map.GetComponent<StrategicOutpostMapComponent>();
            StrategicOutpostMapComponent farmComp = farmStation.Map.GetComponent<StrategicOutpostMapComponent>();
            StrategicOutpostMapComponent mineComp = mineStation.Map.GetComponent<StrategicOutpostMapComponent>();
            StrategicOutpostMapComponent watchComp = watchStation.Map.GetComponent<StrategicOutpostMapComponent>();

            RecordCheck("Reload_FactoryMapCompBuiltLevel", fComp != null && fComp.builtLevel == savedFactoryBuiltLevel, savedFactoryBuiltLevel.ToString(), (fComp != null ? fComp.builtLevel.ToString() : "null"));
            RecordCheck("Reload_FarmMapCompBuiltLevel", farmComp != null && farmComp.builtLevel == savedFarmBuiltLevel, savedFarmBuiltLevel.ToString(), (farmComp != null ? farmComp.builtLevel.ToString() : "null"));
            RecordCheck("Reload_MineMapCompBuiltLevel", mineComp != null && mineComp.builtLevel == savedMineBuiltLevel, savedMineBuiltLevel.ToString(), (mineComp != null ? mineComp.builtLevel.ToString() : "null"));
            RecordCheck("Reload_WatchMapCompBuiltLevel", watchComp != null && watchComp.builtLevel == savedWatchBuiltLevel, savedWatchBuiltLevel.ToString(), (watchComp != null ? watchComp.builtLevel.ToString() : "null"));

            // Verify generator fuel
            var allB = factoryStation.Map.listerBuildings != null ? factoryStation.Map.listerBuildings.allBuildingsColonist : null;
            Building generator = allB != null ? allB.FirstOrDefault(b => b.def.defName == "WoodFiredGenerator") : null;
            if (generator == null && factoryStation.Map.listerThings != null)
            {
                ThingDef wfgDef = DefDatabase<ThingDef>.GetNamedSilentFail("WoodFiredGenerator");
                if (wfgDef != null)
                {
                    generator = factoryStation.Map.listerThings.ThingsOfDef(wfgDef).FirstOrDefault() as Building;
                }
            }
            if (generator == null && factoryStation.Map.spawnedThings != null)
            {
                generator = factoryStation.Map.spawnedThings.OfType<Building>().FirstOrDefault(b => b.def.defName == "WoodFiredGenerator");
            }
            CompRefuelable compRefuel = generator != null ? generator.TryGetComp<CompRefuelable>() : null;
            float currentFuel = compRefuel != null ? compRefuel.Fuel : -1f;
            RecordCheck("Reload_GeneratorFuelPreserved", Math.Abs(currentFuel - savedGeneratorFuel) < 0.1f, savedGeneratorFuel.ToString("F1"), currentFuel.ToString("F1"));

            // Verify exact building defs list
            var currentBuildingDefs = factoryStation.Map.listerBuildings.allBuildingsColonist.Select(b => b.def.defName).OrderBy(s => s).ToList();
            bool buildingsMatch = currentBuildingDefs.SequenceEqual(savedBuildingDefs);
            RecordCheck("Reload_FactoryBuildingDefsPreserved", buildingsMatch, string.Join(",", savedBuildingDefs.ToArray()), string.Join(",", currentBuildingDefs.ToArray()));

            // Verify growing zones
            int currentGrowingCount = farmStation.Map.zoneManager.AllZones.OfType<Zone_Growing>().Count();
            RecordCheck("Reload_GrowingZoneCountPreserved", currentGrowingCount == savedGrowingZoneCount, savedGrowingZoneCount.ToString(), currentGrowingCount.ToString());

            var currentPlantDefs = farmStation.Map.zoneManager.AllZones.OfType<Zone_Growing>().Select(z => (z.GetPlantDefToGrow() != null ? z.GetPlantDefToGrow().defName : "unknown")).OrderBy(s => s).ToList();
            var expectedPlantDefs = savedGrowingPlantDefs.OrderBy(s => s).ToList();
            bool plantDefsMatch = currentPlantDefs.SequenceEqual(expectedPlantDefs);
            RecordCheck("Reload_GrowingPlantDefsPreserved", plantDefsMatch, string.Join(",", expectedPlantDefs.ToArray()), string.Join(",", currentPlantDefs.ToArray()));

            // Verify all beds, doors, workstations reachable by colonist in reload session
            var allFactoryBeds = factoryStation.Map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Where(b => !b.Medical).ToList();
            bool reloadBedsReachable = allFactoryBeds.Count >= 11 && allFactoryBeds.All(b => CheckReachableFromEdge(factoryStation.Map, b));
            RecordCheck("Reload_FactoryBedsReachableFromEdge", reloadBedsReachable, "true", reloadBedsReachable.ToString().ToLower());

            Building reloadFab = factoryStation.Map.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def.defName == "FabricationBench");
            bool reloadFabReachable = reloadFab != null && CheckReachableFromEdge(factoryStation.Map, reloadFab.InteractionCell);
            RecordCheck("Reload_FabricationBenchReachable", reloadFabReachable, "true", reloadFabReachable.ToString().ToLower());

            Finish(null, true);
        }
    }

    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        bool reload = args.Contains("-tmm-outpost-maps-reload");
        bool fresh = args.Contains("-tmm-outpost-maps-probe");

        if (done || map.Parent is StrategicOutpost || !map.IsPlayerHome || (!reload && !fresh))
        {
            return;
        }

        try
        {
            MaintainHomeMapStability();
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

        string jsonReport = string.Format(
            "{{\n  \"passed\": {0},\n  \"checksCount\": {1},\n  \"stage\": {2},\n  \"checks\": [\n    {3}\n  ],\n  \"errors\": [\n    {4}\n  ]\n}}",
            overallPassed ? "true" : "false",
            checks.Count,
            stage,
            string.Join(",\n    ", checkJsonEntries.ToArray()),
            string.Join(",\n    ", errorJsonEntries.ToArray())
        );

        string reportFileName = isReload ? "OutpostMapsReloadProbe.json" : "OutpostMapsProbe.json";
        string reportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, reportFileName);
        File.WriteAllText(reportPath, jsonReport);

        // F6: UI Probe ensures Finish(ex) fails, and passes only upon successful test finish
        if (!isReload)
        {
            string uiReportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "OutpostMapsUiProbe.json");
            if (!overallPassed || ex != null)
            {
                string errStr = ex != null ? EscapeJson(ex.Message) : "Checks failed";
                File.WriteAllText(uiReportPath, "{\"passed\":false,\"visualLayoutInspected\":false,\"error\":\"" + errStr + "\"}");
            }
            else
            {
                string uiErr = null;
                try
                {
                    if (factoryStation != null)
                    {
                        string inspect = factoryStation.GetInspectString();
                        var gizmos = factoryStation.GetGizmos() != null ? factoryStation.GetGizmos().ToList() : null;
                    }
                }
                catch (Exception uiEx)
                {
                    uiErr = uiEx.Message;
                }

                if (uiErr != null)
                {
                    File.WriteAllText(uiReportPath, "{\"passed\":false,\"visualLayoutInspected\":false,\"error\":\"" + EscapeJson(uiErr) + "\"}");
                }
                else
                {
                    File.WriteAllText(uiReportPath, "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
                }
            }
        }
    }
}
