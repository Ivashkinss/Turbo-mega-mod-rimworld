using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class StoryStagesProbeBootstrap
    {
        public const string SaveName = "TMM-StoryStages";

        // Runtime probe quest defNames (registered identically on fresh & reload)
        public const string QuestDownedTimeout = "TMM_Probe_Site_DownedTimeout";
        public const string QuestKillRecruited = "TMM_Probe_Site_KillRecruited";
        public const string QuestKillTransferException = "TMM_Probe_Site_KillTransferException";
        public const string QuestRetrieveDestroyed = "TMM_Probe_Site_RetrieveDestroyed";
        public const string QuestRetrieveTransitAndNpc = "TMM_Probe_Site_RetrieveTransitAndNpc";
        public const string QuestRaidNeutralTwoColonies = "TMM_Probe_Raid_NeutralTwoColonies";
        public const string QuestRaidStageRemovedOrphan = "TMM_Probe_Raid_StageRemovedOrphan";
        public const string QuestRaidEmptyCohort = "TMM_Probe_Raid_EmptyCohort";
        public const string QuestRaidFailedSpawnKeepHostile = "TMM_Probe_Raid_FailedSpawnKeepHostile";
        public const string QuestForceOutcomeActive = "TMM_Probe_Site_ForceOutcomeActive";
        public const string QuestHoldAbsenceGap = "TMM_Probe_Site_HoldAbsenceGap";
        public const string QuestReloadRescue = "TMM_Probe_Reload_Rescue";
        public const string QuestReloadRetrieve = "TMM_Probe_Reload_Retrieve";
        public const string QuestReloadHold = "TMM_Probe_Reload_Hold";

        public static bool FreshMode
        {
            get { return HasArg("-tmm-story-stages-probe"); }
        }

        public static bool ReloadMode
        {
            get { return HasArg("-tmm-story-stages-reload"); }
        }

        static StoryStagesProbeBootstrap()
        {
            if (!FreshMode && !ReloadMode) return;

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                RegisterRuntimeProbeQuests();
                if (ReloadMode && Current.ProgramState == ProgramState.Entry)
                {
                    GameDataSaveLoader.LoadGame(SaveName); // CHECK API
                }
            });
        }

        private static void RegisterRuntimeProbeQuests()
        {
            HeroDef heroA = PickAvailableHero(0);
            HeroDef heroB = PickAvailableHero(1) ?? heroA;
            HeroDef heroC = PickAvailableHero(2) ?? heroB;

            AddQuestDef(BuildSiteQuest(QuestDownedTimeout, "probe.downedTimeout", StorySiteObjective.Clear, null, null, 0.2f, 4f));
            AddQuestDef(BuildSiteQuest(QuestKillRecruited, "probe.killRecruited", StorySiteObjective.KillHero, heroA, null, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestKillTransferException, "probe.killTransferEx", StorySiteObjective.KillHero, heroC, null, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestRetrieveDestroyed, "probe.retrieveDestroyed", StorySiteObjective.Retrieve, null, ThingDefOf.ComponentIndustrial, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestRetrieveTransitAndNpc, "probe.retrieveTransitNpc", StorySiteObjective.Retrieve, null, ThingDefOf.ComponentIndustrial, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestForceOutcomeActive, "probe.forceOutcomeActive", StorySiteObjective.Clear, null, null, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestHoldAbsenceGap, "probe.holdAbsenceGap", StorySiteObjective.Hold, null, null, 5f, 4f));

            AddQuestDef(BuildRaidQuest(QuestRaidNeutralTwoColonies, "probe.raidNeutral", true));
            AddQuestDef(BuildRaidQuest(QuestRaidStageRemovedOrphan, "probe.raidStageRemoved", true));
            AddQuestDef(BuildRaidQuest(QuestRaidEmptyCohort, "probe.raidEmptyCohort", true));
            AddQuestDef(BuildRaidQuest(QuestRaidFailedSpawnKeepHostile, "probe.raidFailedKeepHostile", false));

            AddQuestDef(BuildSiteQuest(QuestReloadRescue, "probe.reloadRescue", StorySiteObjective.Rescue, heroB, null, 10f, 4f));
            AddQuestDef(BuildSiteQuest(QuestReloadRetrieve, "probe.reloadRetrieve", StorySiteObjective.Retrieve, null, ThingDefOf.ComponentIndustrial, 10f, 4f));
            AddQuestDef(BuildSiteQuest(QuestReloadHold, "probe.reloadHold", StorySiteObjective.Hold, null, null, 10f, 4f));
        }

        private static HeroDef PickAvailableHero(int index)
        {
            List<HeroDef> all = DefDatabase<HeroDef>.AllDefsListForReading;
            if (all == null || all.Count == 0) return null;
            return all[index % all.Count];
        }

        private static StoryQuestDef BuildSiteQuest(string defName, string flag, StorySiteObjective obj, HeroDef h, ThingDef t, float timeoutDays, float holdHours)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_Site st = new StoryStage_Site();
            st.key = "site";
            st.label = defName;
            st.text = defName;
            st.minDays = 2f;
            st.maxDays = 5f;
            st.objective = obj;
            st.hero = h;
            st.thing = t;
            st.timeoutDays = timeoutDays;
            st.holdHours = holdHours;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static StoryQuestDef BuildRaidQuest(string defName, string flag, bool restore)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_ColonyRaid st = new StoryStage_ColonyRaid();
            st.key = "raid";
            st.label = defName;
            st.text = defName;
            st.pointsFactor = 0.7f;
            st.delayHours = 1f;
            st.timeoutDays = 3f;
            st.restoreRelations = restore;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static void AddQuestDef(StoryQuestDef d)
        {
            if (d == null) return;
            if (DefDatabase<StoryQuestDef>.GetNamedSilentFail(d.defName) != null) return;
            DefDatabase<StoryQuestDef>.Add(d); // CHECK API
        }

        private static bool HasArg(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Round 2 in-game verification probe for E2 (StoryStage_Site &amp; StoryStage_ColonyRaid).
    /// Covers all Round 2 findings:
    /// - Hero mid-transfer exception (via TestMode hook) -> hero remains in WorldPawns with original faction;
    /// - Raid with empty confirmed raiders -> timeout (NEVER success);
    /// - Failed raid spawn with restoreRelations=false -> hostility rolled back via AbortLease;
    /// - Orphan cleanup via REAL stage removal on StoryQuestDef (not manual status mutation);
    /// - Symmetric relation restore verification + blocked-restore backup retention &amp; retry;
    /// - Retrieve: NPC inventory on home map != delivered, flying TravellingTransporters == transit (not lost),
    ///   player stockpile == delivered, and 250-tick scan throttle;
    /// - Hold: absence/downed gap sampled by 60-tick monitor is NOT credited;
    /// - forceOutcome &amp; legacy vars["forceOutcome"] tested on an ACTIVE run;
    /// - Honest UI report passed value (computed from actual checks and errors, never hardcoded true);
    /// - Clean probe pawn generation without global SanitizeDanglingPawnRelations before SaveGame.
    /// </summary>
    public sealed class InGameStoryStagesProbe : MapComponent
    {
        private sealed class CheckItem
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private static bool s_completed;
        private int m_ticksWaited;
        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        public InGameStoryStagesProbe(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (s_completed) return;
            if (!StoryStagesProbeBootstrap.FreshMode && !StoryStagesProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                if (StoryStagesProbeBootstrap.FreshMode)
                {
                    RunFreshProbe();
                }
                else if (StoryStagesProbeBootstrap.ReloadMode)
                {
                    RunReloadProbe();
                }
            }
            catch (Exception ex)
            {
                m_errors.Add("Unhandled exception: " + ex.ToString());
            }
            finally
            {
                StoryPawnTransfer.TestSpawnOverride = null;
                StoryStage_ColonyRaid.TestRaidExecuteOverride = null;
                StoryRelationRestorer.TestBlockRestoreOverride = null;
                Application.logMessageReceived -= OnUnityLog;

                bool allPassed = WriteReport(StoryStagesProbeBootstrap.ReloadMode ? "StoryStagesReloadProbe.json" : "StoryStagesProbe.json");
                if (!StoryStagesProbeBootstrap.ReloadMode)
                {
                    WriteUiReport(allPassed);
                }
                Root.Shutdown();
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        // =====================================================================
        // FRESH PROBE
        // =====================================================================

        private void RunFreshProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("SparkStory_Exists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. TIMEOUT WITH DOWNED COLONIST ON SITE MAP ----------------
            story.SetFlag("probe.downedTimeout", "true");
            story.DirectorTick();
            StoryQuestRun downedRun = story.RunFor(StoryStagesProbeBootstrap.QuestDownedTimeout);
            Site downedSite = FindSite(ParseInt(downedRun, "siteId", -1));
            if (downedSite != null)
            {
                Pawn scout = SpawnCleanColonist(map);
                Caravan car = EnterSiteWithScout(scout, downedSite);
                if (downedSite.HasMap && downedSite.Map != null)
                {
                    scout.health.AddHediff(HediffDefOf.Anesthetic);
                }
                AdvanceTicks(Mathf.CeilToInt(0.3f * GenDate.TicksPerDay));
                story.DirectorTick();

                bool timedOut = downedRun != null && HasOutcome(downedRun, "timeout");
                bool mapPreserved = downedSite.HasMap && downedSite.Map != null && !downedSite.Destroyed;
                bool pawnAliveOnMap = scout != null && !scout.Dead && scout.Downed && scout.Spawned && scout.Map == downedSite.Map;
                AddCheck("P1_DownedColonistTimeout_MapAndPawnPreserved",
                    timedOut && mapPreserved && pawnAliveOnMap,
                    "timeout=true, mapPreserved=true, pawnAliveDowned=true",
                    string.Format("timeout={0}, map={1}, pawn={2}", timedOut, mapPreserved, pawnAliveOnMap));

                CleanupScoutAndSite(scout, car, downedSite);
            }
            else
            {
                AddCheck("P1_DownedColonistTimeout_MapAndPawnPreserved", false, "site created", "site null");
            }

            // ---------------- 2. KILLHERO FOR HERO ALREADY RECRUITED BY PLAYER ----------------
            StoryQuestDef killRecDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestKillRecruited);
            StoryStage_Site killRecStage = killRecDef != null ? killRecDef.stages[0] as StoryStage_Site : null;
            Pawn recruitedHero = (killRecStage != null && killRecStage.hero != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(killRecStage.hero) : null;
            if (recruitedHero != null)
            {
                if (recruitedHero.Spawned) recruitedHero.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns.Contains(recruitedHero)) Find.WorldPawns.RemovePawn(recruitedHero);
                recruitedHero.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(recruitedHero, FindStandable(map), map, WipeMode.Vanish);

                story.SetFlag("probe.killRecruited", "true");
                story.DirectorTick();
                story.DirectorTick();
                StoryQuestRun krRun = story.RunFor(StoryStagesProbeBootstrap.QuestKillRecruited);

                bool unavailable = krRun != null && HasOutcome(krRun, "unavailable");
                bool heroUntouched = !recruitedHero.Dead && recruitedHero.Faction == Faction.OfPlayer &&
                                     recruitedHero.Spawned && recruitedHero.Map == map;
                AddCheck("P1_KillHeroRecruited_UnavailableAndUntouched",
                    unavailable && heroUntouched,
                    "unavailable=true, heroUntouched=true",
                    string.Format("unavailable={0}, untouched={1}", unavailable, heroUntouched));
            }
            else
            {
                AddCheck("P1_KillHeroRecruited_UnavailableAndUntouched", false, "hero exists", "hero null");
            }

            // ---------------- 3. HERO MID-TRANSFER EXCEPTION ROLLBACK (TestMode) ----------------
            if (TestMode.Enabled)
            {
                StoryQuestDef exDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestKillTransferException);
                StoryStage_Site exStage = exDef != null ? exDef.stages[0] as StoryStage_Site : null;
                Pawn exHero = (exStage != null && exStage.hero != null && HeroRegistry.Instance != null)
                    ? HeroRegistry.Instance.GetOrCreate(exStage.hero) : null;

                if (exHero != null)
                {
                    Faction origFac = exHero.Faction;
                    bool origInWorld = Find.WorldPawns.Contains(exHero);

                    story.SetFlag("probe.killTransferEx", "true");
                    story.DirectorTick();
                    StoryQuestRun exRun = story.RunFor(StoryStagesProbeBootstrap.QuestKillTransferException);
                    Site exSite = FindSite(ParseInt(exRun, "siteId", -1));

                    if (exSite != null && origInWorld)
                    {
                        Pawn scout = SpawnCleanColonist(map);
                        Caravan car = EnterSiteWithScout(scout, exSite);

                        // Inject exception AFTER StoryPawnTransfer has called SetFaction & RemovePawn!
                        StoryPawnTransfer.TestSpawnOverride = delegate(Pawn p, Map m, IntVec3 c)
                        {
                            throw new InvalidOperationException("Simulated GenSpawn failure mid-transfer");
                        };

                        story.DirectorTick();
                        StoryPawnTransfer.TestSpawnOverride = null;

                        bool backInWorld = Find.WorldPawns.Contains(exHero) && !exHero.Spawned;
                        bool origFacPreserved = exHero.Faction == origFac;
                        bool endedUnavailable = exRun != null && HasOutcome(exRun, "unavailable");

                        AddCheck("R2_HeroTransferException_FullRollbackToWorldPawns",
                            backInWorld && origFacPreserved && endedUnavailable,
                            "inWorldPawns=true, origFaction preserved, outcome=unavailable",
                            string.Format("inWorld={0}, facMatch={1}, hist={2}",
                                backInWorld, origFacPreserved, exRun != null ? string.Join(",", exRun.history.ToArray()) : "null"));

                        CleanupScoutAndSite(scout, car, exSite);
                    }
                    else
                    {
                        AddCheck("R2_HeroTransferException_FullRollbackToWorldPawns", false, "free world hero & site created",
                            string.Format("site={0}, inWorld={1}", exSite != null, origInWorld));
                    }
                }
                else
                {
                    AddCheck("R2_HeroTransferException_FullRollbackToWorldPawns", false, "hero exists", "hero null");
                }
            }

            // ---------------- 4. RETRIEVE: DESTROYED ITEM -> FAILED (NO SUBSTITUTION) ----------------
            story.SetFlag("probe.retrieveDestroyed", "true");
            story.DirectorTick();
            StoryQuestRun retDestRun = story.RunFor(StoryStagesProbeBootstrap.QuestRetrieveDestroyed);
            Site retDestSite = FindSite(ParseInt(retDestRun, "siteId", -1));
            if (retDestSite != null)
            {
                Pawn scout = SpawnCleanColonist(map);
                Caravan car = EnterSiteWithScout(scout, retDestSite);
                story.DirectorTick();

                string realId = GetVar(retDestRun, "targetThingId");
                Thing realThing = FindThingOnMapById(retDestSite.Map, realId);

                Pawn decoyPawn = SpawnCleanColonist(map);
                Thing decoyItem = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                decoyPawn.inventory.innerContainer.TryAdd(decoyItem);
                decoyPawn.DeSpawn(DestroyMode.Vanish);
                Caravan decoyCaravan = CaravanMaker.MakeCaravan(new List<Pawn> { decoyPawn }, Faction.OfPlayer, map.Tile, true); // CHECK API

                if (realThing != null && !realThing.Destroyed) realThing.Destroy(DestroyMode.Vanish);

                // Verify 250-tick throttle: ticking immediately (+50 ticks) does NOT rescan yet
                AdvanceTicks(50);
                story.DirectorTick();
                bool throttledBefore250 = retDestRun != null && retDestRun.status == StoryQuestStatus.Active;

                // After 250 ticks total: rescan runs and detects Lost -> failed
                AdvanceTicks(250);
                story.DirectorTick();

                bool gotFailed = retDestRun != null && HasOutcome(retDestRun, "failed") && !HasOutcome(retDestRun, "success");
                AddCheck("P1_RetrieveDestroyed_ThrottledAndFailedNoSubstitution",
                    realThing != null && throttledBefore250 && gotFailed,
                    "throttled=true, outcome=failed",
                    string.Format("realFound={0}, throttled={1}, hist={2}",
                        realThing != null, throttledBefore250, retDestRun != null ? string.Join(",", retDestRun.history.ToArray()) : "null"));

                DestroyTestCaravanAndPawns(decoyCaravan);
                CleanupScoutAndSite(scout, car, retDestSite);
            }
            else
            {
                AddCheck("P1_RetrieveDestroyed_ThrottledAndFailedNoSubstitution", false, "site created", "site null");
            }

            // ---------------- 5. RETRIEVE P2: NPC ON HOME MAP != DELIVERED, FLYING POD == TRANSIT, STOCKPILE == DELIVERED ----------------
            Faction neutralFac = FindNeutralHumanlikeFaction();
            Zone_Stockpile stockpile = EnsureTestStockpile(map);
            story.SetFlag("probe.retrieveTransitNpc", "true");
            story.DirectorTick();
            StoryQuestRun retNpcRun = story.RunFor(StoryStagesProbeBootstrap.QuestRetrieveTransitAndNpc);
            Site retNpcSite = FindSite(ParseInt(retNpcRun, "siteId", -1));
            if (retNpcSite != null && neutralFac != null && stockpile != null)
            {
                Pawn scout = SpawnCleanColonist(map);
                Caravan car = EnterSiteWithScout(scout, retNpcSite);
                story.DirectorTick();

                string realId = GetVar(retNpcRun, "targetThingId");
                Thing realItem = FindThingOnMapById(retNpcSite.Map, realId);
                bool npcNotDelivered = false;
                bool flyingPodIsTransit = false;
                bool stockpileDelivered = false;

                if (realItem != null)
                {
                    // Step 5A: Move exact item into an NPC's inventory on the home map -> MUST NOT complete!
                    realItem.DeSpawn(DestroyMode.Vanish);
                    Pawn npcOnHome = SpawnCleanNpc(map, neutralFac);
                    npcOnHome.inventory.innerContainer.TryAdd(realItem, false);

                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();
                    npcNotDelivered = retNpcRun.status == StoryQuestStatus.Active && !HasOutcome(retNpcRun, "success");

                    // Step 5B: Move exact item into a flying player TravellingTransporters pod -> MUST stay Active (transit, NOT failed!)
                    npcOnHome.inventory.innerContainer.Remove(realItem);
                    CleanupCleanPawn(npcOnHome);

                    TravellingTransporters flyingPods = LaunchTestFlyingPod(map.Tile, retNpcSite.Tile, realItem);
                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();
                    flyingPodIsTransit = flyingPods != null && retNpcRun.status == StoryQuestStatus.Active &&
                                         !HasOutcome(retNpcRun, "failed") && !HasOutcome(retNpcRun, "success");

                    // Step 5C: Unload exact item into player stockpile on home map -> MUST complete with success!
                    ExtractItemFromFlyingPod(flyingPods, realItem);
                    if (flyingPods != null && !flyingPods.Destroyed) flyingPods.Destroy();

                    IntVec3 stockCell = stockpile.cells[0];
                    GenSpawn.Spawn(realItem, stockCell, map, WipeMode.Vanish);
                    realItem.SetForbidden(false, false);

                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();
                    stockpileDelivered = HasOutcome(retNpcRun, "success");
                }

                AddCheck("R2_Retrieve_NpcNotDelivered_FlyingPodTransit_StockpileSuccess",
                    npcNotDelivered && flyingPodIsTransit && stockpileDelivered,
                    "npcWait=true, podTransit=true, stockpileSuccess=true",
                    string.Format("npcWait={0}, podTransit={1}, stockpile={2}", npcNotDelivered, flyingPodIsTransit, stockpileDelivered));

                CleanupScoutAndSite(scout, car, retNpcSite);
            }
            else
            {
                AddCheck("R2_Retrieve_NpcNotDelivered_FlyingPodTransit_StockpileSuccess", false, "site, neutralFac & stockpile exist", "missing");
            }

            // ---------------- 6. HOLD P2: ABSENCE / DOWNED GAP NOT CREDITED (60-TICK MONITOR) ----------------
            story.SetFlag("probe.holdAbsenceGap", "true");
            story.DirectorTick();
            StoryQuestRun gapRun = story.RunFor(StoryStagesProbeBootstrap.QuestHoldAbsenceGap);
            Site gapSite = FindSite(ParseInt(gapRun, "siteId", -1));
            if (gapSite != null)
            {
                Pawn scout = SpawnCleanColonist(map);
                Caravan car = EnterSiteWithScout(scout, gapSite);
                story.DirectorTick();
                // Sample at entry (adds 0)
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                int held0 = ParseInt(gapRun, "heldTicks", -1);

                // Stand 1 hour -> 2500 ticks
                AdvanceTicks(GenDate.TicksPerHour);
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                int held1h = ParseInt(gapRun, "heldTicks", -1);

                // Down the scout and sample 60 ticks later -> absence detected, holdActive=false
                Hediff anesth = scout.health.AddHediff(HediffDefOf.Anesthetic);
                AdvanceTicks(StoryStage_Site.HoldSampleInterval);
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                bool markedInactive = GetVar(gapRun, "holdActive") == "false";

                // 3 hours pass while downed
                AdvanceTicks(3 * GenDate.TicksPerHour);

                // Scout recovers and returns -> first sample marks active without crediting the 3h gap
                if (anesth != null) scout.health.RemoveHediff(anesth);
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                int heldAfterReturn = ParseInt(gapRun, "heldTicks", -1);

                // Stand 1 more hour -> total 5000 ticks (2h, NOT 5h!)
                AdvanceTicks(GenDate.TicksPerHour);
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                int heldFinal = ParseInt(gapRun, "heldTicks", -1);

                bool gapExcluded = held0 == 0 && held1h == GenDate.TicksPerHour && markedInactive &&
                                   heldAfterReturn == GenDate.TicksPerHour && heldFinal == 2 * GenDate.TicksPerHour &&
                                   gapRun.status == StoryQuestStatus.Active;
                AddCheck("R2_Hold_AbsenceGapExcludedBy60TickSampler", gapExcluded,
                    "0->2500->(3h absent)->2500->5000",
                    string.Format("0={0}, 1h={1}, inactive={2}, afterGap={3}, final={4}",
                        held0, held1h, markedInactive, heldAfterReturn, heldFinal));

                CleanupScoutAndSite(scout, car, gapSite);
            }
            else
            {
                AddCheck("R2_Hold_AbsenceGapExcludedBy60TickSampler", false, "site created", "site null");
            }

            // ---------------- 7. RAID: NEUTRAL FACTION (-50) + SECOND HOME MAP + SYMMETRIC RESTORE ----------------
            StoryQuestDef raidNeutralDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidNeutralTwoColonies);
            StoryStage_ColonyRaid raidNeutralStage = raidNeutralDef != null ? raidNeutralDef.stages[0] as StoryStage_ColonyRaid : null;
            if (neutralFac != null && raidNeutralStage != null)
            {
                ForceNeutralWithGoodwill(neutralFac, -50);
                raidNeutralStage.faction = neutralFac.def.defName;

                story.SetFlag("probe.raidNeutral", "true");
                story.DirectorTick();
                StoryQuestRun raidRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidNeutralTwoColonies);

                Settlement secondHomeObj = null;
                Map secondHomeMap = CreateTemporarySecondHomeMap(out secondHomeObj);

                AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                story.DirectorTick();

                bool hostileDuringRaid = neutralFac.HostileTo(Faction.OfPlayer);
                int pinnedMapId = ParseInt(raidRun, "raidMapId", -1);
                List<int> raidPawnIds = ParseIntList(GetVar(raidRun, "raidPawnIds"));

                story.DirectorTick();
                bool stillActiveWithTwoHomes = raidRun != null && raidRun.status == StoryQuestStatus.Active && pinnedMapId == map.uniqueID;

                int killed = KillTrackedPawns(map, raidPawnIds);

                // In TestMode, also verify blocked-restore retention & retry before final symmetric verification
                bool blockRetryVerified = true;
                if (TestMode.Enabled)
                {
                    StoryRelationRestorer.TestBlockRestoreOverride = delegate(Faction f) { return f == neutralFac; };
                    story.DirectorTick();
                    bool retainedOnBlock = StoryRelationRestorer.Instance != null &&
                                           StoryRelationRestorer.Instance.HasPendingBackupForFaction(neutralFac);
                    StoryRelationRestorer.TestBlockRestoreOverride = null;
                    StoryRelationRestorer.CheckOrphansIfPresent();
                    bool clearedOnRetry = StoryRelationRestorer.Instance != null &&
                                          !StoryRelationRestorer.Instance.HasPendingBackupForFaction(neutralFac);
                    blockRetryVerified = retainedOnBlock && clearedOnRetry;
                }
                else
                {
                    story.DirectorTick();
                }

                bool raidSucceeded = raidRun != null && HasOutcome(raidRun, "success");
                bool symmetricNonHostile = !neutralFac.HostileTo(Faction.OfPlayer) && !Faction.OfPlayer.HostileTo(neutralFac);
                bool symmetricGoodwill = neutralFac.PlayerGoodwill >= -55 && neutralFac.PlayerGoodwill <= -45 &&
                                         Faction.OfPlayer.GoodwillWith(neutralFac) == neutralFac.PlayerGoodwill;

                AddCheck("P1_NeutralRaid_TwoHomesAndSymmetricRestore",
                    hostileDuringRaid && stillActiveWithTwoHomes && killed > 0 && raidSucceeded &&
                    symmetricNonHostile && symmetricGoodwill && blockRetryVerified,
                    "hostile->success->Symmetric Neutral(~-50)",
                    string.Format("hostile={0}, active2={1}, killed={2}, succ={3}, symNonH={4}, gw={5}/{6}, retry={7}",
                        hostileDuringRaid, stillActiveWithTwoHomes, killed, raidSucceeded, symmetricNonHostile,
                        neutralFac.PlayerGoodwill, Faction.OfPlayer.GoodwillWith(neutralFac), blockRetryVerified));

                CleanupSecondHomeMap(secondHomeMap, secondHomeObj);
            }
            else
            {
                AddCheck("P1_NeutralRaid_TwoHomesAndSymmetricRestore", false, "neutral faction & def exist", "missing");
            }

            // ---------------- 8. ORPHAN CLEANUP VIA REAL STAGE REMOVAL (NO DIRECT STATUS MUTATION) ----------------
            StoryQuestDef orphanDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidStageRemovedOrphan);
            StoryStage_ColonyRaid orphanStage = orphanDef != null && orphanDef.stages.Count > 0 ? orphanDef.stages[0] as StoryStage_ColonyRaid : null;
            if (neutralFac != null && orphanDef != null && orphanStage != null)
            {
                ForceNeutralWithGoodwill(neutralFac, -40);
                orphanStage.faction = neutralFac.def.defName;

                story.SetFlag("probe.raidStageRemoved", "true");
                story.DirectorTick();
                StoryQuestRun orphRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidStageRemovedOrphan);

                AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                story.DirectorTick();
                bool becameHostile = neutralFac.HostileTo(Faction.OfPlayer);
                List<int> orphPawnIds = ParseIntList(GetVar(orphRun, "raidPawnIds"));

                // REAL stage removal: clear stages on the Def and let SparkStory.DirectorTick disable the quest naturally!
                try
                {
                    orphanDef.stages.Clear();
                    story.DirectorTick();
                }
                finally
                {
                    orphanDef.stages.Add(orphanStage);
                }

                bool disabledNaturally = orphRun != null && orphRun.status == StoryQuestStatus.Disabled &&
                                         HasOutcome(orphRun, "stage missing");
                bool rolledBackSym = !neutralFac.HostileTo(Faction.OfPlayer) && !Faction.OfPlayer.HostileTo(neutralFac) &&
                                     neutralFac.PlayerGoodwill >= -45 && neutralFac.PlayerGoodwill <= -35;

                AddCheck("R2_OrphanCleanup_RealStageRemovalRollback",
                    becameHostile && disabledNaturally && rolledBackSym,
                    "hostile->SparkStory Disable(stage missing)->Symmetric Neutral(~-40)",
                    string.Format("hostile={0}, disabled={1}, symNonH={2}, gw={3}",
                        becameHostile, disabledNaturally, !neutralFac.HostileTo(Faction.OfPlayer), neutralFac.PlayerGoodwill));

                KillTrackedPawns(map, orphPawnIds);
            }
            else
            {
                AddCheck("R2_OrphanCleanup_RealStageRemovalRollback", false, "neutral faction & def exist", "missing");
            }

            // ---------------- 9. RAID WITHOUT CONFIRMED RAIDERS -> TIMEOUT (NEVER SUCCESS) ----------------
            StoryQuestDef emptyRaidDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidEmptyCohort);
            StoryStage_ColonyRaid emptyRaidStage = emptyRaidDef != null ? emptyRaidDef.stages[0] as StoryStage_ColonyRaid : null;
            if (neutralFac != null && emptyRaidStage != null)
            {
                ForceNeutralWithGoodwill(neutralFac, -35);
                emptyRaidStage.faction = neutralFac.def.defName;

                if (TestMode.Enabled)
                {
                    // Simulate RaidEnemy returning true while spawning ZERO pawns
                    StoryStage_ColonyRaid.TestRaidExecuteOverride = delegate(IncidentParms p) { return true; };
                    story.SetFlag("probe.raidEmptyCohort", "true");
                    story.DirectorTick();
                    AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                    story.DirectorTick();
                    StoryStage_ColonyRaid.TestRaidExecuteOverride = null;

                    StoryQuestRun emptyRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidEmptyCohort);
                    bool endedTimeoutNotSuccess = emptyRun != null && HasOutcome(emptyRun, "timeout") && !HasOutcome(emptyRun, "success");
                    bool rolledBack = !neutralFac.HostileTo(Faction.OfPlayer);
                    AddCheck("R2_RaidZeroRaiders_TimeoutAndRollback",
                        endedTimeoutNotSuccess && rolledBack,
                        "timeout (not success) & hostility rolled back",
                        string.Format("hist={0}, nonHostile={1}",
                            emptyRun != null ? string.Join(",", emptyRun.history.ToArray()) : "null", rolledBack));
                }
                else
                {
                    // Without TestMode: test active spawned raid with empty raidPawnIds & vanished lordId -> MUST timeout, NEVER success
                    story.SetFlag("probe.raidEmptyCohort", "true");
                    story.DirectorTick();
                    StoryQuestRun emptyRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidEmptyCohort);
                    if (emptyRun != null)
                    {
                        emptyRun.vars["spawned"] = "true";
                        emptyRun.vars["raidMapId"] = map.uniqueID.ToString();
                        emptyRun.vars["raidLordId"] = "999999";
                        emptyRun.vars["raidPawnIds"] = string.Empty;
                        emptyRun.vars["confirmedRaiderCount"] = "0";
                        story.DirectorTick();
                    }
                    bool endedTimeoutNotSuccess = emptyRun != null && HasOutcome(emptyRun, "timeout") && !HasOutcome(emptyRun, "success");
                    AddCheck("R2_RaidEmptyCohortVanishedLord_TimeoutNotSuccess",
                        endedTimeoutNotSuccess,
                        "timeout (not success)",
                        emptyRun != null ? string.Join(",", emptyRun.history.ToArray()) : "null");
                }
            }

            // ---------------- 10. FAILED RAID SPAWN WITH restoreRelations=false -> STILL ROLLS BACK HOSTILITY (TestMode) ----------------
            if (TestMode.Enabled && neutralFac != null)
            {
                StoryQuestDef keepHostileDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidFailedSpawnKeepHostile);
                StoryStage_ColonyRaid keepHostileStage = keepHostileDef != null ? keepHostileDef.stages[0] as StoryStage_ColonyRaid : null;
                if (keepHostileStage != null)
                {
                    ForceNeutralWithGoodwill(neutralFac, -30);
                    keepHostileStage.faction = neutralFac.def.defName;
                    keepHostileStage.restoreRelations = false;

                    StoryStage_ColonyRaid.TestRaidExecuteOverride = delegate(IncidentParms p) { return false; };
                    story.SetFlag("probe.raidFailedKeepHostile", "true");
                    story.DirectorTick();
                    AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                    story.DirectorTick();
                    StoryStage_ColonyRaid.TestRaidExecuteOverride = null;

                    StoryQuestRun failRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidFailedSpawnKeepHostile);
                    bool rolledBackEvenWhenRestoreFalse = failRun != null && HasOutcome(failRun, "timeout") &&
                                                          !neutralFac.HostileTo(Faction.OfPlayer) &&
                                                          !Faction.OfPlayer.HostileTo(neutralFac) &&
                                                          neutralFac.PlayerGoodwill >= -35 && neutralFac.PlayerGoodwill <= -25;
                    AddCheck("R2_FailedRaidSpawn_RollsBackEvenWhenRestoreRelationsFalse",
                        rolledBackEvenWhenRestoreFalse,
                        "timeout & restored Neutral(~-30) despite restoreRelations=false",
                        string.Format("hist={0}, nonHostile={1}, gw={2}",
                            failRun != null ? string.Join(",", failRun.history.ToArray()) : "null",
                            !neutralFac.HostileTo(Faction.OfPlayer), neutralFac.PlayerGoodwill));
                }
            }

            // ---------------- 11. FORCEOUTCOME ON AN ACTIVE RUN + LEGACY VARS["forceOutcome"] TEST ----------------
            story.SetFlag("probe.forceOutcomeActive", "true");
            story.DirectorTick();
            StoryQuestRun activeForceRun = story.RunFor(StoryStagesProbeBootstrap.QuestForceOutcomeActive);
            Site activeForceSite = FindSite(ParseInt(activeForceRun, "siteId", -1));
            bool wasActiveBefore = activeForceRun != null && activeForceRun.status == StoryQuestStatus.Active;

            if (!TestMode.Enabled)
            {
                bool methodRefused = wasActiveBefore && !story.TestForceOutcome(activeForceRun, activeForceRun.gen, "success");
                bool stillActiveAfterMethod = activeForceRun != null && activeForceRun.status == StoryQuestStatus.Active;

                // Separate test of legacy vars["forceOutcome"] key on the ACTIVE run without -tmm-story-test
                if (activeForceRun != null) activeForceRun.vars["forceOutcome"] = "success";
                story.DirectorTick();
                bool legacyRefusedAndCleaned = activeForceRun != null &&
                                               activeForceRun.status == StoryQuestStatus.Active &&
                                               !activeForceRun.vars.ContainsKey("forceOutcome");

                AddCheck("R2_ForceOutcomeAndLegacyVarRefused_OnActiveRun",
                    wasActiveBefore && methodRefused && stillActiveAfterMethod && legacyRefusedAndCleaned,
                    "active=true, TestForceOutcome=false, legacyVar ignored & stripped",
                    string.Format("wasActive={0}, methodRefused={1}, stillActive={2}, legacyCleaned={3}",
                        wasActiveBefore, methodRefused, stillActiveAfterMethod, legacyRefusedAndCleaned));
            }
            else
            {
                // In TestMode: wrong generation must still be refused on an active run, matching generation works
                bool wrongGenRefused = wasActiveBefore && !story.TestForceOutcome(activeForceRun, activeForceRun.gen + 99, "success") &&
                                       activeForceRun.status == StoryQuestStatus.Active;
                bool rightGenWorked = wasActiveBefore && story.TestForceOutcome(activeForceRun, activeForceRun.gen, "timeout") &&
                                      HasOutcome(activeForceRun, "timeout");
                AddCheck("R2_ForceOutcome_ActiveRunGenCheckedInTestMode",
                    wrongGenRefused && rightGenWorked,
                    "wrongGen=false(Active), rightGen=true(timeout)",
                    string.Format("wrongGenRefused={0}, rightGenWorked={1}", wrongGenRefused, rightGenWorked));
            }
            if (activeForceSite != null && !activeForceSite.Destroyed && !activeForceSite.HasMap)
            {
                activeForceSite.Destroy();
            }

            // ---------------- 12. PREPARE RELOAD SAVE: RESCUE + RETRIEVE + PARTIAL HOLD ----------------
            // 12A. Rescue
            story.SetFlag("probe.reloadRescue", "true");
            story.DirectorTick();
            StoryQuestRun rescueRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRescue);
            Site rescueSite = FindSite(ParseInt(rescueRun, "siteId", -1));
            bool rescueReady = false;
            if (rescueSite != null)
            {
                Pawn rScout = SpawnCleanColonist(map);
                EnterSiteWithScout(rScout, rescueSite);
                story.DirectorTick();
                rescueReady = GetVar(rescueRun, "heroImprisonedOnSite") == "true" && !GetVar(rescueRun, "targetPawnId").NullOrEmpty();
            }
            AddCheck("ReloadPrep_RescueCaptiveOnSite", rescueReady, "imprisoned=true", rescueReady.ToString());

            // 12B. Retrieve
            story.SetFlag("probe.reloadRetrieve", "true");
            story.DirectorTick();
            StoryQuestRun retRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRetrieve);
            Site retSite = FindSite(ParseInt(retRun, "siteId", -1));
            bool retrieveReady = false;
            if (retSite != null)
            {
                Pawn iScout = SpawnCleanColonist(map);
                EnterSiteWithScout(iScout, retSite);
                story.DirectorTick();
                string tid = GetVar(retRun, "targetThingId");
                Thing spawnedItem = FindThingOnMapById(retSite.Map, tid);
                Thing otherStack = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                bool cannotMerge = spawnedItem != null && !spawnedItem.CanStackWith(otherStack);
                otherStack.Destroy(DestroyMode.Vanish);
                retrieveReady = spawnedItem != null && cannotMerge;
            }
            AddCheck("ReloadPrep_RetrieveUniqueItemProtected", retrieveReady, "item spawned & non-stackable", retrieveReady.ToString());

            // 12C. Hold with partial confirmed progress
            story.SetFlag("probe.reloadHold", "true");
            story.DirectorTick();
            StoryQuestRun holdRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadHold);
            Site holdSite = FindSite(ParseInt(holdRun, "siteId", -1));
            if (holdSite != null)
            {
                AdvanceTicks(3 * GenDate.TicksPerHour);
                Pawn hScout = SpawnCleanColonist(map);
                EnterSiteWithScout(hScout, holdSite);
                story.DirectorTick();
                int heldAfterEntry = ParseInt(holdRun, "heldTicks", -1);

                AdvanceTicks(2 * GenDate.TicksPerHour);
                story.DirectorTick();
                int heldAfter2h = ParseInt(holdRun, "heldTicks", -1);

                bool holdPrepOk = heldAfterEntry == 0 && heldAfter2h == 2 * GenDate.TicksPerHour && holdRun.status == StoryQuestStatus.Active;
                AddCheck("P2_Hold_FirstPollZeroAndPartialSaved", holdPrepOk,
                    "afterEntry=0, after2h=5000, active",
                    string.Format("entry={0}, after2h={1}, status={2}", heldAfterEntry, heldAfter2h, holdRun.status));
            }
            else
            {
                AddCheck("P2_Hold_FirstPollZeroAndPartialSaved", false, "site created", "site null");
            }

            // Save directly WITHOUT any global SanitizeDanglingPawnRelations mask!
            GameDataSaveLoader.SaveGame(StoryStagesProbeBootstrap.SaveName); // CHECK API
        }

        // =====================================================================
        // RELOAD PROBE
        // =====================================================================

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. RESCUE RELOAD ----------------
            StoryQuestRun rescueRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRescue);
            Site rescueSite = FindSite(ParseInt(rescueRun, "siteId", -1));
            string captiveId = GetVar(rescueRun, "targetPawnId");
            if (rescueRun != null && rescueSite != null && rescueSite.HasMap && !captiveId.NullOrEmpty())
            {
                Pawn captive = FindPawnOnMapById(rescueSite.Map, captiveId);
                Pawn rescuer = FindFirstColonistOnMap(rescueSite.Map);
                bool captiveRestored = captive != null && captive.IsPrisoner;

                if (captive != null && rescuer != null)
                {
                    if (captive.guest != null) captive.guest.SetGuestStatus(null, GuestStatus.Guest);
                    captive.DeSpawn(DestroyMode.Vanish);
                    rescuer.DeSpawn(DestroyMode.Vanish);
                    List<Pawn> group = new List<Pawn> { rescuer, captive };
                    Caravan car = CaravanMaker.MakeCaravan(group, Faction.OfPlayer, map.Tile, true); // CHECK API
                    Current.Game.DeinitAndRemoveMap(rescueSite.Map, false); // CHECK API
                    story.DirectorTick();
                    story.DirectorTick();

                    bool once = CountOutcome(rescueRun, "success") == 1;
                    AddCheck("Reload_RescueCompletesOnce", captiveRestored && once,
                        "restored=true, successCount=1",
                        string.Format("restored={0}, count={1}", captiveRestored, CountOutcome(rescueRun, "success")));
                    DestroyTestCaravanAndPawns(car);
                }
                else
                {
                    AddCheck("Reload_RescueCompletesOnce", false, "captive & rescuer on site", "missing");
                }
            }
            else
            {
                AddCheck("Reload_RescueCompletesOnce", false, "rescue run & site restored", "missing");
            }

            // ---------------- 2. RETRIEVE RELOAD ----------------
            StoryQuestRun retRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRetrieve);
            Site retSite = FindSite(ParseInt(retRun, "siteId", -1));
            string thingId = GetVar(retRun, "targetThingId");
            if (retRun != null && retSite != null && retSite.HasMap && !thingId.NullOrEmpty())
            {
                Thing uniqueItem = FindThingOnMapById(retSite.Map, thingId);
                Pawn carrier = FindFirstColonistOnMap(retSite.Map);
                Thing otherStack = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                bool stillProtected = uniqueItem != null && !uniqueItem.CanStackWith(otherStack);
                otherStack.Destroy(DestroyMode.Vanish);

                if (uniqueItem != null && carrier != null)
                {
                    uniqueItem.DeSpawn(DestroyMode.Vanish);
                    carrier.inventory.innerContainer.TryAdd(uniqueItem);
                    carrier.DeSpawn(DestroyMode.Vanish);
                    Caravan car = CaravanMaker.MakeCaravan(new List<Pawn> { carrier }, Faction.OfPlayer, map.Tile, true); // CHECK API
                    Current.Game.DeinitAndRemoveMap(retSite.Map, false); // CHECK API

                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();
                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();

                    bool once = CountOutcome(retRun, "success") == 1;
                    AddCheck("Reload_RetrieveCompletesOnce", stillProtected && once,
                        "protected=true, successCount=1",
                        string.Format("protected={0}, count={1}", stillProtected, CountOutcome(retRun, "success")));
                    DestroyTestCaravanAndPawns(car);
                }
                else
                {
                    AddCheck("Reload_RetrieveCompletesOnce", false, "item & carrier on site", "missing");
                }
            }
            else
            {
                AddCheck("Reload_RetrieveCompletesOnce", false, "retrieve run & site restored", "missing");
            }

            // ---------------- 3. PARTIAL HOLD RELOAD ----------------
            StoryQuestRun holdRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadHold);
            Site holdSite = FindSite(ParseInt(holdRun, "siteId", -1));
            if (holdRun != null && holdSite != null && holdSite.HasMap)
            {
                int restoredTicks = ParseInt(holdRun, "heldTicks", 0);
                AdvanceTicks(2 * GenDate.TicksPerHour + 250);
                story.DirectorTick();
                story.DirectorTick();

                bool once = CountOutcome(holdRun, "success") == 1;
                AddCheck("Reload_PartialHoldCompletesOnce", restoredTicks >= 2 * GenDate.TicksPerHour && once,
                    "restored>=5000, successCount=1",
                    string.Format("restored={0}, count={1}", restoredTicks, CountOutcome(holdRun, "success")));
            }
            else
            {
                AddCheck("Reload_PartialHoldCompletesOnce", false, "hold run & site restored", "missing");
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static Zone_Stockpile EnsureTestStockpile(Map targetMap)
        {
            if (targetMap == null || targetMap.zoneManager == null) return null;
            Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, targetMap.zoneManager); // CHECK API
            targetMap.zoneManager.RegisterZone(zone);
            IntVec3 origin = FindStandable(targetMap);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    IntVec3 c = origin + new IntVec3(dx, 0, dz);
                    if (c.InBounds(targetMap) && c.Standable(targetMap) && targetMap.zoneManager.ZoneAt(c) == null)
                    {
                        zone.AddCell(c);
                    }
                }
            }
            return zone.cells.Count > 0 ? zone : null;
        }

        private static TravellingTransporters LaunchTestFlyingPod(PlanetTile originTile, PlanetTile destTile, Thing item)
        {
            if (item == null || Find.WorldObjects == null) return null;
            ActiveTransporterInfo info = new ActiveTransporterInfo();
            info.openDelay = 110;
            info.leaveSlag = false;
            info.innerContainer.TryAdd(item, false);

            TravellingTransporters worldObj = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravellingTransporters); // CHECK API
            worldObj.SetFaction(Faction.OfPlayer);
            worldObj.Tile = originTile;
            worldObj.destinationTile = destTile;
            Find.WorldObjects.Add(worldObj);
            worldObj.AddTransporter(info, true);
            return worldObj;
        }

        private static void ExtractItemFromFlyingPod(TravellingTransporters pods, Thing item)
        {
            if (pods == null || item == null) return;
            if (item.holdingOwner != null)
            {
                item.holdingOwner.Remove(item);
            }
        }

        private static Faction FindNeutralHumanlikeFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HasGoodwill &&
                    f.def.humanlikeFaction && !f.def.permanentEnemy)
                {
                    return f;
                }
            }
            return null;
        }

        private static void ForceNeutralWithGoodwill(Faction fac, int goodwill)
        {
            if (fac == null) return;
            if (fac.HostileTo(Faction.OfPlayer))
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, 0 - fac.PlayerGoodwill, false, false, null, null);
            }
            int delta = goodwill - fac.PlayerGoodwill;
            if (delta != 0)
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null);
            }
        }

        private Map CreateTemporarySecondHomeMap(out Settlement settlement)
        {
            settlement = null;
            PlanetTile tile;
            if (!TileFinder.TryFindNewSiteTile(out tile, map.Tile, 2, 10, false, null, 0f, false, TileFinderMode.Near, false, false, null, null)) // CHECK API
            {
                return null;
            }
            settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            settlement.Tile = tile;
            settlement.SetFaction(Faction.OfPlayer);
            settlement.Name = "ProbeSecondHome";
            Find.WorldObjects.Add(settlement);
            return GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(75, 1, 75), null); // CHECK API
        }

        private static void CleanupSecondHomeMap(Map m, Settlement s)
        {
            if (m != null && Find.Maps.Contains(m))
            {
                Current.Game.DeinitAndRemoveMap(m, false); // CHECK API
            }
            if (s != null && !s.Destroyed) s.Destroy();
        }

        private static Pawn SpawnCleanColonist(Map targetMap)
        {
            PawnGenerationRequest req = new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                canGeneratePawnRelations: false,
                allowAddictions: false); // CHECK API
            Pawn p = PawnGenerator.GeneratePawn(req);
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static Pawn SpawnCleanNpc(Map targetMap, Faction fac)
        {
            PawnKindDef kind = fac != null ? (fac.RandomPawnKind() ?? PawnKindDefOf.SpaceRefugee) : PawnKindDefOf.SpaceRefugee;
            PawnGenerationRequest req = new PawnGenerationRequest(
                kind,
                fac,
                PawnGenerationContext.NonPlayer,
                canGeneratePawnRelations: false,
                allowAddictions: false); // CHECK API
            Pawn p = PawnGenerator.GeneratePawn(req);
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static void CleanupCleanPawn(Pawn p)
        {
            if (p == null) return;
            if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
            if (Find.WorldPawns != null && Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
            if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
        }

        private static Caravan EnterSiteWithScout(Pawn scout, Site site)
        {
            if (scout.Spawned) scout.DeSpawn(DestroyMode.Vanish);
            Caravan c = CaravanMaker.MakeCaravan(new List<Pawn> { scout }, Faction.OfPlayer, site.Tile, true); // CHECK API
            Map siteMap = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null); // CHECK API
            CaravanEnterMapUtility.Enter(c, siteMap, CaravanEnterMode.Edge); // CHECK API
            return c;
        }

        private static void DestroyTestCaravanAndPawns(Caravan car)
        {
            if (car == null) return;
            List<Pawn> pawns = new List<Pawn>(car.PawnsListForReading);
            car.RemoveAllPawns();
            if (!car.Destroyed) car.Destroy();
            for (int i = 0; i < pawns.Count; i++)
            {
                CleanupCleanPawn(pawns[i]);
            }
        }

        private static void CleanupScoutAndSite(Pawn scout, Caravan car, Site site)
        {
            if (car != null && !car.Destroyed) car.Destroy();
            CleanupCleanPawn(scout);
            if (site != null && !site.Destroyed)
            {
                if (site.HasMap && site.Map != null)
                {
                    Current.Game.DeinitAndRemoveMap(site.Map, false); // CHECK API
                }
                if (!site.Destroyed) site.Destroy();
            }
        }

        private static int KillTrackedPawns(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return 0;
            HashSet<int> set = new HashSet<int>(ids);
            int killed = 0;
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && !p.Dead && set.Contains(p.thingIDNumber))
                {
                    p.Kill(null);
                    killed++;
                }
            }
            return killed;
        }

        private static Thing FindThingOnMapById(Map targetMap, string thingId)
        {
            if (targetMap == null || thingId.NullOrEmpty()) return null;
            List<Thing> all = targetMap.listerThings.AllThings;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].Destroyed && all[i].ThingID == thingId) return all[i];
            }
            return null;
        }

        private static Pawn FindPawnOnMapById(Map targetMap, string pawnId)
        {
            if (targetMap == null || pawnId.NullOrEmpty()) return null;
            IReadOnlyList<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].ThingID == pawnId) return pawns[i];
            }
            return null;
        }

        private static Pawn FindFirstColonistOnMap(Map targetMap)
        {
            if (targetMap == null || targetMap.mapPawns == null) return null;
            IReadOnlyList<Pawn> cols = targetMap.mapPawns.FreeColonistsSpawned;
            return cols.Count > 0 ? cols[0] : null;
        }

        private static Site FindSite(int siteId)
        {
            if (siteId < 0) return null;
            List<Site> sites = Find.WorldObjects.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i] != null && !sites[i].Destroyed && sites[i].ID == siteId) return sites[i];
            }
            return null;
        }

        private static IntVec3 FindStandable(Map targetMap)
        {
            IntVec3 cell;
            if (CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 10, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap);
            }, out cell))
            {
                return cell;
            }
            return targetMap.Center;
        }

        private static void AdvanceTicks(int delta)
        {
            if (delta > 0) Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + delta); // CHECK API
        }

        private static bool HasOutcome(StoryQuestRun run, string outcome)
        {
            return CountOutcome(run, outcome) > 0;
        }

        private static int CountOutcome(StoryQuestRun run, string outcome)
        {
            if (run == null || run.history == null) return 0;
            int count = 0;
            string suffix = ":" + outcome;
            for (int i = 0; i < run.history.Count; i++)
            {
                string h = run.history[i];
                if (h != null && (h == outcome || h.EndsWith(suffix, StringComparison.Ordinal))) count++;
            }
            return count;
        }

        private static int ParseInt(StoryQuestRun run, string key, int fallback)
        {
            if (run == null || run.vars == null) return fallback;
            string raw;
            int val;
            return (run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) ? val : fallback;
        }

        private static string GetVar(StoryQuestRun run, string key)
        {
            if (run == null || run.vars == null) return string.Empty;
            string raw;
            return run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }

        private static List<int> ParseIntList(string raw)
        {
            List<int> list = new List<int>();
            if (raw.NullOrEmpty()) return list;
            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) list.Add(val);
            }
            return list;
        }

        private void AddCheck(string name, bool passed, string expected, string actual)
        {
            CheckItem item = new CheckItem();
            item.name = name;
            item.passed = passed;
            item.expected = expected ?? string.Empty;
            item.actual = actual ?? string.Empty;
            m_checks.Add(item);
        }

        private bool WriteReport(string fileName)
        {
            bool allPassed = m_errors.Count == 0 && m_checks.Count > 0;
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (!m_checks[i].passed)
                {
                    allPassed = false;
                    break;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"passed\":");
            sb.Append(allPassed ? "true" : "false");
            sb.Append(",\"testModeEnabled\":");
            sb.Append(TestMode.Enabled ? "true" : "false");
            sb.Append(",\"checksCount\":");
            sb.Append(m_checks.Count);
            sb.Append(",\"checks\":[");
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (i > 0) sb.Append(',');
                CheckItem c = m_checks[i];
                sb.Append("{\"name\":\"");
                sb.Append(EscapeJson(c.name));
                sb.Append("\",\"passed\":");
                sb.Append(c.passed ? "true" : "false");
                sb.Append(",\"expected\":\"");
                sb.Append(EscapeJson(c.expected));
                sb.Append("\",\"actual\":\"");
                sb.Append(EscapeJson(c.actual));
                sb.Append("\"}");
            }
            sb.Append("],\"errors\":[");
            for (int i = 0; i < m_errors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                sb.Append(EscapeJson(m_errors[i]));
                sb.Append('"');
            }
            sb.Append("]}");

            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return allPassed;
        }

        private void WriteUiReport(bool actualPassed)
        {
            try
            {
                string uiPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "StoryStagesUiProbe.json");
                string payload = string.Format(
                    "{{\"passed\":{0},\"checksCount\":{1},\"errorsCount\":{2},\"visualLayoutInspected\":false}}",
                    actualPassed ? "true" : "false",
                    m_checks.Count,
                    m_errors.Count);
                File.WriteAllText(uiPath, payload, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }
    }
}
