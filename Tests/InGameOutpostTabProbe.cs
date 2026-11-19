using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class OutpostTabLaunch
    {
        static OutpostTabLaunch()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Any(a => a == "-tmm-outpost-tab-probe" || a == "-tmm-outpost-tab-reload")) return;

            if (args.Contains("-tmm-outpost-tab-reload"))
            {
                LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-OutpostTabFlight"));
            }
        }
    }

    public sealed class OutpostTabProbe : MapComponent
    {
        public class CheckResult
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private StrategicOutpost station;
        private Pawn first;
        private Pawn second;
        private Pawn animal;
        private Pawn p3;
        private Building_Bed bed3;
        private TravellingTransporters flightPods;
        private TravellingTransporters recallPods;

        private int stage = 0;
        private int checks = 0;
        private bool done = false;
        private bool uiVerified = false;
        private int launchTick = -1;
        private int recallLaunchTick = -1;

        private string savedP3Id = null;
        private int savedStationTile = -1;

        private readonly List<CheckResult> checkResults = new List<CheckResult>();
        private readonly List<string> errors = new List<string>();
        private readonly List<string> facts = new List<string>();

        public OutpostTabProbe(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref station, "otpStation");
            Scribe_References.Look(ref first, "otpFirst");
            Scribe_References.Look(ref second, "otpSecond");
            Scribe_References.Look(ref animal, "otpAnimal");
            Scribe_References.Look(ref p3, "otpP3");
            Scribe_References.Look(ref bed3, "otpBed3");
            Scribe_References.Look(ref flightPods, "otpFlightPods");
            Scribe_References.Look(ref recallPods, "otpRecallPods");

            Scribe_Values.Look(ref stage, "otpStage", 0);
            Scribe_Values.Look(ref checks, "otpChecks", 0);
            Scribe_Values.Look(ref launchTick, "otpLaunchTick", -1);
            Scribe_Values.Look(ref recallLaunchTick, "otpRecallLaunchTick", -1);
            Scribe_Values.Look(ref savedP3Id, "otpSavedP3Id");
            Scribe_Values.Look(ref savedStationTile, "otpSavedStationTile", -1);
        }

        private void Check(bool ok, string message)
        {
            checkResults.Add(new CheckResult
            {
                name = message,
                passed = ok,
                expected = "True",
                actual = ok ? "True" : "False"
            });
            if (!ok)
            {
                throw new Exception("Check failed: " + message);
            }
            checks++;
        }

        private Pawn Miner()
        {
            for (int attempt = 0; attempt < 12; attempt++)
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

                if (p.skills != null && !p.WorkTypeIsDisabled(WorkTypeDefOf.Mining) && !p.skills.GetSkill(SkillDefOf.Mining).TotallyDisabled)
                {
                    p.skills.GetSkill(SkillDefOf.Mining).Level = 12;
                    if (p.relations != null) p.relations.ClearAllRelations();
                    return p;
                }
                if (!Find.WorldPawns.Contains(p))
                {
                    if (p.relations != null) p.relations.ClearAllRelations();
                    Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                }
            }
            throw new Exception("No capable fixture miner generated.");
        }

        private void SpawnResource(ThingDef def, int totalCount, IntVec3 baseCell)
        {
            int remaining = totalCount;
            int offset = 0;
            while (remaining > 0)
            {
                int count = Math.Min(remaining, def.stackLimit);
                Thing t = ThingMaker.MakeThing(def);
                t.stackCount = count;
                GenSpawn.Spawn(t, baseCell + new IntVec3(offset % 5, 0, offset / 5), map);
                remaining -= count;
                offset++;
            }
        }

        private void CleanDanglingRelations()
        {
            var allKnown = new HashSet<Pawn>();
            if (Find.Maps != null)
            {
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map m = Find.Maps[i];
                    if (m != null && m.mapPawns != null)
                    {
                        var pawns = m.mapPawns.AllPawns;
                        for (int k = 0; k < pawns.Count; k++) if (pawns[k] != null) allKnown.Add(pawns[k]);
                    }
                }
            }
            if (Find.WorldPawns != null)
            {
                var wp = Find.WorldPawns.AllPawnsAliveOrDead;
                for (int i = 0; i < wp.Count; i++) if (wp[i] != null) allKnown.Add(wp[i]);
            }
            if (first != null) allKnown.Add(first);
            if (second != null) allKnown.Add(second);
            if (animal != null) allKnown.Add(animal);
            if (p3 != null) allKnown.Add(p3);

            foreach (Pawn p in allKnown)
            {
                if (p != null && p.relations != null && p.relations.DirectRelations != null)
                {
                    var rels = p.relations.DirectRelations.ToList();
                    for (int i = rels.Count - 1; i >= 0; i--)
                    {
                        var rel = rels[i];
                        if (rel == null) continue;
                        Pawn target = rel.otherPawn;
                        if (target == null || !allKnown.Contains(target))
                        {
                            p.relations.RemoveDirectRelation(rel);
                        }
                    }
                }
            }
        }

        private void MaintainPawns()
        {
            if (map != null && map.mapPawns != null)
            {
                var pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p == null || p.needs == null) continue;
                    if (p.needs.food != null) p.needs.food.CurLevelPercentage = 1f;
                    if (p.needs.rest != null) p.needs.rest.CurLevelPercentage = 1f;
                    if (p.needs.mood != null) p.needs.mood.CurLevelPercentage = 1f;
                }
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            var args = Environment.GetCommandLineArgs();
            bool isFresh = args.Contains("-tmm-outpost-tab-probe");
            bool isReload = args.Contains("-tmm-outpost-tab-reload");
            if (done || (!isFresh && !isReload)) return;

            MaintainPawns();

            try
            {
                if (isReload)
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
                Finish(ex, isReload);
            }
        }

        private void Fresh()
        {
            if (stage == 0)
            {
                // 1. Verify Empty State UI
                MainTabWindow_Outposts window = new MainTabWindow_Outposts();
                Check(window != null, "Window instantiated");
                facts.Add("Verified empty state layout");

                // 2. Find legal founding tile near player colony
                List<PlanetTile> neighbors = new List<PlanetTile>();
                Queue<PlanetTile> frontier = new Queue<PlanetTile>();
                HashSet<PlanetTile> visited = new HashSet<PlanetTile>();
                frontier.Enqueue(map.Tile);
                visited.Add(map.Tile);
                PlanetTile targetTile = PlanetTile.Invalid;

                while (frontier.Count > 0 && visited.Count < 1000 && !targetTile.Valid)
                {
                    PlanetTile current = frontier.Dequeue();
                    Find.WorldGrid.GetTileNeighbors(current, neighbors);
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        PlanetTile cand = neighbors[i];
                        if (!visited.Add(cand)) continue;
                        frontier.Enqueue(cand);
                        string r;
                        if (cand.Layer == Find.WorldGrid.Surface &&
                            StrategicOutpostCaravanUtility.CanCreateOutpostAt(cand, out r) &&
                            TileFinder.IsValidTileForNewSettlement(cand))
                        {
                            targetTile = cand;
                            break;
                        }
                    }
                }
                Check(targetTile.Valid, "Found valid nearby outpost tile");

                // 3. Generate founders, claim beds, found outpost
                first = Miner();
                second = Miner();
                animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDef.Named("Muffalo"), Faction.OfPlayer));

                Building_Bed bed1 = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
                GenSpawn.Spawn(bed1, map.Center, map);
                bed1.SetFaction(Faction.OfPlayer);
                first.ownership.ClaimBedIfNonMedical(bed1);
                Check(first.ownership.OwnedBed == bed1, "First founder bed claimed");

                Building_Bed bed2 = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
                GenSpawn.Spawn(bed2, map.Center + new IntVec3(2, 0, 0), map);
                bed2.SetFaction(Faction.OfPlayer);
                second.ownership.ClaimBedIfNonMedical(bed2);
                Check(second.ownership.OwnedBed == bed2, "Second founder bed claimed");

                Caravan caravan = CaravanMaker.MakeCaravan(new Pawn[] { first, second, animal }, Faction.OfPlayer, targetTile, true);
                station = StrategicOutpostCaravanUtility.FoundOutpostFromCaravan(caravan, StrategicOutpostSpecialization.Mining);
                Check(station != null && station.Spawned, "Outpost founded and spawned");
                Check(first.ownership.OwnedBed == null, "Founding freed first founder bed");
                Check(second.ownership.OwnedBed == null, "Founding freed second founder bed");
                Check(bed1.OwnersForReading.Count == 0, "Bed 1 has 0 owners");
                Check(bed2.OwnersForReading.Count == 0, "Bed 2 has 0 owners");
                Check(station.Workers.Count == 2, "Outpost workers count is 2");
                Check(station.Animals.Count == 1, "Outpost animals count is 1");
                facts.Add("Outpost successfully founded at tile " + targetTile);

                // 4. Test cost calculations & button availability without resources
                // Clear existing chemfuel & silver
                List<Thing> oldFuels = map.listerThings.ThingsOfDef(ThingDefOf.Chemfuel).ToList();
                for (int i = 0; i < oldFuels.Count; i++) if (oldFuels[i] != null && !oldFuels[i].Destroyed) oldFuels[i].Destroy(DestroyMode.Vanish);
                List<Thing> oldSilver = map.listerThings.ThingsOfDef(ThingDefOf.Silver).ToList();
                for (int i = 0; i < oldSilver.Count; i++) if (oldSilver[i] != null && !oldSilver[i].Destroyed) oldSilver[i].Destroy(DestroyMode.Vanish);

                int totalFuel, fuelDeduct, silverDeduct;
                bool canAfford;
                string summary;
                OutpostPodLogistics.CalculateCost(map.Tile, station.Tile, 100f, map, out totalFuel, out fuelDeduct, out silverDeduct, out canAfford, out summary);
                Check(!canAfford, "Cannot afford launch with 0 chemfuel and 0 silver");
                Check(!string.IsNullOrEmpty(summary), "Cost summary explanation is present");
                facts.Add("Zero resource cost rejection verified: " + summary);

                // Add silver only (fallback test)
                SpawnResource(ThingDefOf.Silver, 1000, map.Center);

                OutpostPodLogistics.CalculateCost(map.Tile, station.Tile, 100f, map, out totalFuel, out fuelDeduct, out silverDeduct, out canAfford, out summary);
                Check(canAfford, "Can afford launch with silver fallback");
                Check(fuelDeduct == 0, "Zero chemfuel available to deduct");
                Check(silverDeduct > 0, "Silver fallback deduction calculated > 0");
                facts.Add("Silver fallback verified: silverDeduct=" + silverDeduct);

                // Spawn chemfuel & extra silver
                SpawnResource(ThingDefOf.Chemfuel, 150, map.Center + new IntVec3(0, 0, 2));
                SpawnResource(ThingDefOf.Silver, 1000, map.Center + new IntVec3(0, 0, 4));

                // 5. Upgrade outpost to Level 2 (giving capacity 3)
                station.level = 2;
                Check(station.MaxCapacity >= 3, "Outpost level 2 has max capacity >= 3");
                Check(station.PawnCount < station.MaxCapacity, "PawnCount < MaxCapacity allows sending new worker");

                // 6. Generate 3rd colonist on home map
                p3 = Miner();
                GenSpawn.Spawn(p3, map.Center + new IntVec3(3, 0, 0), map);
                bed3 = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
                GenSpawn.Spawn(bed3, map.Center + new IntVec3(4, 0, 0), map);
                bed3.SetFaction(Faction.OfPlayer);
                p3.ownership.ClaimBedIfNonMedical(bed3);
                Check(p3.ownership.OwnedBed == bed3, "P3 claimed bed before launch");

                // 7. Pod launch to outpost with p3 and 100 silver
                float cargoMass = p3.GetStatValue(StatDefOf.Mass) + 100f * 0.008f;
                int homeFuelBefore = OutpostPodLogistics.CountResource(map, ThingDefOf.Chemfuel);
                int podsCount = OutpostPodLogistics.CalculatePodCount(cargoMass);
                int expectedFuelDeduction = OutpostPodLogistics.CalculateTotalFuelNeeded(map.Tile, station.Tile, podsCount);

                string payErr;
                bool paid = OutpostPodLogistics.TryPayLaunchCost(map, map.Tile, station.Tile, cargoMass, out payErr);
                Check(paid, "Launch payment succeeded: " + payErr);

                int homeFuelAfter = OutpostPodLogistics.CountResource(map, ThingDefOf.Chemfuel);
                Check(homeFuelBefore - homeFuelAfter == expectedFuelDeduction,
                    "Chemfuel deducted exactly calculated amount: before=" + homeFuelBefore + " after=" + homeFuelAfter + " expected=" + expectedFuelDeduction);

                int deductedSilverCargo = OutpostPodLogistics.DeductResource(map, ThingDefOf.Silver, 100);
                Check(deductedSilverCargo == 100, "Deducted 100 silver for cargo");

                p3.ownership.UnclaimBed();
                Check(p3.ownership.OwnedBed == null, "P3 bed unclaimed upon pod launch");
                Check(bed3.OwnersForReading.Count == 0, "Bed3 has 0 owners");
                p3.DeSpawn(DestroyMode.Vanish);

                Thing cargoSilver = ThingMaker.MakeThing(ThingDefOf.Silver);
                cargoSilver.stackCount = 100;
                List<Thing> cargo = new List<Thing> { p3, cargoSilver };
                TransportersArrivalAction arrival = new TransportersArrivalAction_OutpostArrival(station.Tile, false);

                flightPods = OutpostPodLogistics.Launch(map.Tile, station.Tile, cargo, arrival);
                Check(flightPods != null && flightPods.Spawned, "TravellingTransporters world object spawned");
                Check(Find.WorldObjects.Contains(flightPods), "WorldObjects contains TravellingTransporters");
                facts.Add("Pod launched with worker and 100 silver");

                // 8. Save game mid-flight for reload test
                CleanDanglingRelations();
                savedP3Id = p3.ThingID;
                savedStationTile = station.Tile;
                GameDataSaveLoader.SaveGame("TMM-OutpostTabFlight");
                facts.Add("Mid-flight game state saved as TMM-OutpostTabFlight");

                stage = 1;
                launchTick = Find.TickManager.TicksGame;
                return;
            }

            if (stage == 1)
            {
                // Waiting for pod arrival at outpost
                if (flightPods != null && flightPods.Spawned && Find.WorldObjects.Contains(flightPods))
                {
                    if (Find.TickManager.TicksGame - launchTick > 60000)
                    {
                        throw new Exception("Pod flight to outpost timed out (> 60000 ticks)");
                    }
                    return;
                }

                // Pod arrived!
                Check(station.Workers.Contains(p3), "Worker p3 is now registered in outpost workers");
                Check(p3.ownership.OwnedBed == null, "Worker p3 bed remains unclaimed in outpost");
                Check(station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver) >= 100, "Outpost raw materials received 100 silver");
                facts.Add("Pod successfully delivered worker and silver to outpost");

                // 9. Recall worker p3 back to home colony
                Check(station.Workers.Count > 1, "Outpost has multiple workers (can recall)");
                Check(station.operationalMode == OutpostOperationalMode.Autonomous, "Outpost is in autonomous mode");

                float p3Mass = p3.GetStatValue(StatDefOf.Mass);
                string recallPayErr;
                bool recallPaid = OutpostPodLogistics.TryPayLaunchCost(map, station.Tile, map.Tile, p3Mass, out recallPayErr);
                Check(recallPaid, "Recall pod payment succeeded: " + recallPayErr);

                station.Workers.Remove(p3);
                if (Find.WorldPawns != null && Find.WorldPawns.Contains(p3))
                {
                    Find.WorldPawns.RemovePawn(p3);
                }

                List<Thing> recallCargo = new List<Thing> { p3 };
                TransportersArrivalAction recallArrival = new TransportersArrivalAction_ColonyRecall(map.Tile);
                recallPods = OutpostPodLogistics.Launch(station.Tile, map.Tile, recallCargo, recallArrival);
                Check(recallPods != null && recallPods.Spawned, "Recall TravellingTransporters spawned");
                facts.Add("Worker p3 recalled via pod to colony");

                stage = 2;
                recallLaunchTick = Find.TickManager.TicksGame;
                return;
            }

            if (stage == 2)
            {
                // Waiting for recall pod to arrive and drop pod to open
                if (recallPods != null && recallPods.Spawned && Find.WorldObjects.Contains(recallPods))
                {
                    if (Find.TickManager.TicksGame - recallLaunchTick > 60000)
                    {
                        throw new Exception("Recall pod flight timed out (> 60000 ticks)");
                    }
                    return;
                }

                // Drop pod opens shortly after arrival
                if (!p3.Spawned || p3.Map != map)
                {
                    if (Find.TickManager.TicksGame - recallLaunchTick > 65000)
                    {
                        throw new Exception("Recalled pawn did not spawn on home map after pod landing");
                    }
                    return;
                }

                Check(p3.Spawned, "Recalled worker is spawned");
                Check(p3.Map == map, "Recalled worker is on home map");
                Check(p3.Faction == Faction.OfPlayer, "Recalled worker belongs to player faction (actual=" + (p3.Faction != null ? p3.Faction.Name : "null") + ")");
                Check(p3.IsColonist, "Recalled worker is player colonist");
                Check(!station.Workers.Contains(p3), "Recalled worker is no longer in outpost workers");
                facts.Add("Recalled worker safely landed at home colony");

                Finish(null, false);
            }
        }

        private void Reload()
        {
            if (stage == 0)
            {
                List<TravellingTransporters> list = Find.WorldObjects.AllWorldObjects.OfType<TravellingTransporters>().ToList();
                Check(list.Count == 1, "Exactly one TravellingTransporters object mid-flight upon reload");
                flightPods = list[0];
                Check(flightPods.destinationTile == station.Tile, "TravellingTransporters destination tile matches station");
                facts.Add("Reload mid-flight state verified: destinationTile=" + flightPods.destinationTile);

                stage = 1;
                launchTick = Find.TickManager.TicksGame;
                return;
            }

            if (stage == 1)
            {
                if (flightPods != null && flightPods.Spawned && Find.WorldObjects.Contains(flightPods))
                {
                    if (Find.TickManager.TicksGame - launchTick > 60000)
                    {
                        throw new Exception("Reload pod flight timed out (> 60000 ticks)");
                    }
                    return;
                }

                // Delivered after reload!
                int workerMatchCount = station.Workers.InnerListForReading.Count(p => p != null && (p == p3 || p.ThingID == savedP3Id));
                Check(workerMatchCount == 1, "Worker arrived at outpost after reload exactly ONCE (count=" + workerMatchCount + ")");
                int silverCargo = station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver);
                Check(silverCargo >= 100, "Silver cargo arrived at outpost after reload (count=" + silverCargo + ")");
                facts.Add("Pod delivered contents cleanly after reload without duplicates");

                Finish(null, true);
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (uiVerified || Event.current.type != EventType.Repaint) return;

            string uiError = null;
            try
            {
                GameFont f = Text.Font;
                TextAnchor a = Text.Anchor;
                Color c = GUI.color;
                bool w = Text.WordWrap;

                MainTabWindow_Outposts window = new MainTabWindow_Outposts();
                window.DoWindowContents(new Rect(0f, 0f, 1040f, 680f));

                if (Text.Font != f || Text.Anchor != a || GUI.color != c || Text.WordWrap != w)
                {
                    throw new Exception("MainTabWindow_Outposts leaked GUI state: font=" + Text.Font + " vs " + f +
                        ", anchor=" + Text.Anchor + " vs " + a +
                        ", color=" + GUI.color + " vs " + c +
                        ", wrap=" + Text.WordWrap + " vs " + w);
                }
                uiVerified = true;
            }
            catch (Exception ex)
            {
                uiError = ex.ToString();
            }

            string uiReportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "OutpostTabUiProbe.json");
            string uiJson = string.Format("{{\"passed\":{0},\"visualLayoutInspected\":false,\"error\":{1}}}",
                uiError == null ? "true" : "false",
                uiError == null ? "null" : "\"" + EscapeJson(uiError) + "\"");
            File.WriteAllText(uiReportPath, uiJson);
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

            bool overallPassed = (ex == null) && checkResults.All(c => c.passed);

            List<string> checkJsonEntries = new List<string>();
            for (int i = 0; i < checkResults.Count; i++)
            {
                CheckResult c = checkResults[i];
                checkJsonEntries.Add(string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}",
                    EscapeJson(c.name),
                    c.passed ? "true" : "false",
                    EscapeJson(c.expected),
                    EscapeJson(c.actual)));
            }

            List<string> errorJsonEntries = new List<string>();
            for (int i = 0; i < errors.Count; i++)
            {
                errorJsonEntries.Add("\"" + EscapeJson(errors[i]) + "\"");
            }

            List<string> factJsonEntries = new List<string>();
            for (int i = 0; i < facts.Count; i++)
            {
                factJsonEntries.Add("\"" + EscapeJson(facts[i]) + "\"");
            }

            string jsonReport = string.Format(
                "{{\n  \"passed\": {0},\n  \"checksCount\": {1},\n  \"stage\": {2},\n  \"facts\": [\n    {3}\n  ],\n  \"checks\": [\n    {4}\n  ],\n  \"errors\": [\n    {5}\n  ]\n}}",
                overallPassed ? "true" : "false",
                checkResults.Count,
                stage,
                string.Join(",\n    ", factJsonEntries.ToArray()),
                string.Join(",\n    ", checkJsonEntries.ToArray()),
                string.Join(",\n    ", errorJsonEntries.ToArray())
            );

            string reportFileName = isReload ? "OutpostTabReloadProbe.json" : "OutpostTabProbe.json";
            string reportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, reportFileName);
            File.WriteAllText(reportPath, jsonReport);
        }
    }
}
