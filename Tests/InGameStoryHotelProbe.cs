using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class StoryHotelProbeBootstrap
    {
        public const string SaveName = "TMM-StoryHotel";

        // Runtime probe quests (registered identically on fresh & reload so Scribe resolves Defs cleanly)
        public const string QNoHotel = "TMM_Probe_E3_NoHotel";
        public const string QHeroColonistSkip = "TMM_Probe_E3_HeroColonistSkip";
        public const string QHeroCompanionException = "TMM_Probe_E3_HeroCompanionException";
        public const string QPermanentEnemy = "TMM_Probe_E3_PermanentEnemy";
        public const string QTruceOverlapA = "TMM_Probe_E3_TruceOverlapA";
        public const string QTruceOverlapB = "TMM_Probe_E3_TruceOverlapB";
        public const string QTruceStageRemovedOrphan = "TMM_Probe_E3_TruceStageRemovedOrphan";
        public const string QHostKilled = "TMM_Probe_E3_HostKilled";
        public const string QHostImprisoned = "TMM_Probe_E3_HostImprisoned";
        public const string QHostVanishedNoExit = "TMM_Probe_E3_HostVanishedNoExit";
        public const string QInvestigateStrict = "TMM_Probe_E3_InvestigateStrict";
        public const string QHideOwnedRaid = "TMM_Probe_E3_HideOwnedRaid";
        public const string QHideFailedRaidDeadline = "TMM_Probe_E3_HideFailedRaidDeadline";
        public const string QDeliverExpiredClick = "TMM_Probe_E3_DeliverExpiredClick";
        public const string QDeliverUnknownDeadline = "TMM_Probe_E3_DeliverUnknownDeadline";
        public const string QDeliverImmutableContract = "TMM_Probe_E3_DeliverImmutableContract";
        public const string QDeliverStageKeyRemoved = "TMM_Probe_E3_DeliverStageKeyRemoved";
        public const string QDeliverSecondStackFail = "TMM_Probe_E3_DeliverSecondStackFail";
        public const string QDeliverCommitDestroyEx = "TMM_Probe_E3_DeliverCommitDestroyEx";
        public const string QDeliverCaravanSettlement = "TMM_Probe_E3_DeliverCaravanSettlement";
        public const string QReloadActiveVisit = "TMM_Probe_E3_ReloadActiveVisit";
        public const string QReloadPaidDeliver = "TMM_Probe_E3_ReloadPaidDeliver";

        public static bool FreshMode
        {
            get { return HasArg("-tmm-story-hotel-probe"); }
        }

        public static bool ReloadMode
        {
            get { return HasArg("-tmm-story-hotel-reload"); }
        }

        static StoryHotelProbeBootstrap()
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
            HeroDef heroA = PickHero(0);
            HeroDef heroB = PickHero(1) ?? heroA;
            HeroDef heroC = PickHero(2) ?? heroB;

            AddQuestDef(BuildHotelQuest(QNoHotel, "probe.e3.noHotel", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHeroColonistSkip, "probe.e3.heroColonist", null, heroA, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHeroCompanionException, "probe.e3.heroCompEx", null, heroC, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QPermanentEnemy, "probe.e3.permEnemy", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QTruceOverlapA, "probe.e3.truceOverlapA", null, null, 1, 1f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QTruceOverlapB, "probe.e3.truceOverlapB", null, null, 1, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QTruceStageRemovedOrphan, "probe.e3.truceStageRemoved", null, null, 1, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHostKilled, "probe.e3.hostKilled", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHostImprisoned, "probe.e3.hostImprisoned", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHostVanishedNoExit, "probe.e3.hostVanished", null, null, 1, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QInvestigateStrict, "probe.e3.investigate", null, null, 1, 2f, 1f, StoryHotelObjective.Investigate, null, -1f));
            AddQuestDef(BuildHotelQuest(QHideOwnedRaid, "probe.e3.hideOwned", null, null, 1, 1f, 1f, StoryHotelObjective.Hide, null, 2f));
            AddQuestDef(BuildHotelQuest(QHideFailedRaidDeadline, "probe.e3.hideFailed", null, null, 1, 0.5f, 0.25f, StoryHotelObjective.Hide, null, 1f));

            AddQuestDef(BuildDeliverQuest(QDeliverExpiredClick, "probe.e3.deliverExpired", null, ThingDefOf.Steel, 30, StoryDeliverTarget.colony, 0.2f));
            AddQuestDef(BuildDeliverQuest(QDeliverUnknownDeadline, "probe.e3.deliverUnknownDl", null, ThingDefOf.Steel, 25, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverImmutableContract, "probe.e3.deliverImmutable", null, ThingDefOf.Steel, 30, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverStageKeyRemoved, "probe.e3.deliverKeyRemoved", null, ThingDefOf.Steel, 30, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverSecondStackFail, "probe.e3.deliverAtomic", null, ThingDefOf.Steel, 50, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverCommitDestroyEx, "probe.e3.deliverCommitEx", null, ThingDefOf.Steel, 20, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverCaravanSettlement, "probe.e3.deliverCaravan", null, ThingDefOf.Steel, 40, StoryDeliverTarget.giverSettlement, 10f));

            AddQuestDef(BuildHotelQuest(QReloadActiveVisit, "probe.e3.reloadVisit", null, heroB, 2, 3f, 2f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildDeliverQuest(QReloadPaidDeliver, "probe.e3.reloadPaid", null, ThingDefOf.Steel, 50, StoryDeliverTarget.colony, 10f));
        }

        private static HeroDef PickHero(int idx)
        {
            List<HeroDef> all = DefDatabase<HeroDef>.AllDefsListForReading;
            if (all == null || all.Count == 0) return null;
            return all[idx % all.Count];
        }

        private static StoryQuestDef BuildHotelQuest(
            string defName,
            string flag,
            string facDef,
            HeroDef h,
            int groupSize,
            float stayDays,
            float overdueDays,
            StoryHotelObjective obj,
            string hunterFac,
            float hunterDelayHours)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.faction = facDef;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_HotelVisit st = new StoryStage_HotelVisit();
            st.key = "hotel";
            st.label = defName;
            st.text = defName;
            st.faction = facDef;
            st.hero = h;
            st.groupSize = groupSize;
            st.stayDays = stayDays;
            st.overdueDays = overdueDays;
            st.objective = obj;
            st.hunterFaction = hunterFac;
            st.hunterRaidDelayHours = hunterDelayHours;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static StoryQuestDef BuildDeliverQuest(string defName, string flag, string facDef, ThingDef thing, int count, StoryDeliverTarget to, float timeoutDays)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.faction = facDef;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_Deliver st = new StoryStage_Deliver();
            st.key = "deliver";
            st.label = defName;
            st.text = defName;
            st.thing = thing;
            st.count = count;
            st.to = to;
            st.timeoutDays = timeoutDays;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static void AddQuestDef(StoryQuestDef d)
        {
            if (d == null || DefDatabase<StoryQuestDef>.GetNamedSilentFail(d.defName) != null) return;
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
    /// Round 2 in-game verification probe for E3 (StoryStage_HotelVisit &amp; StoryStage_Deliver).
    /// Covers every Round 2 finding:
    /// - Hero untouched in WorldPawns when companion generation throws (via TestMode hook);
    /// - Overlapping truce visits on the SAME hostile faction keep Neutral until the LAST visit ends,
    ///   then restore Hostile symmetrically on both FactionRelations;
    /// - Orphan truce rollback via REAL stage removal on StoryQuestDef (not manual status mutation);
    /// - Guest vanished without Pawn.ExitMap -> failed (never false success);
    /// - Hide ignores unrelated hostile lords of hunterFaction and launches/tracks its OWN bound raid,
    ///   reaching hard deadline (not success) when the hunter raid fails to spawn;
    /// - Deliver refuses payment when deadlineTick is missing/unknown;
    /// - Deliver enforces immutable contract terms even if letter fields are tampered with;
    /// - Deliver refuses payment when stageKey was removed from def.stages (zero stale index fallback);
    /// - Deliver atomic rollback on second-stack SplitOff failure AND commit-destroy exception safety;
    /// - Passing-by caravan at giverSettlement ignored, explicit CaravanArrivalAction_StoryDeliver deducted;
    /// - Save/reload preserves active HotelVisit (lord + hero without duplication + peaceful ExitMap)
    ///   and completes paid Deliver without double deduction;
    /// - Honest JSON &amp; UI reports (passed computed from checks/errors) and zero global relation sanitization.
    /// </summary>
    public sealed class InGameStoryHotelProbe : MapComponent
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

        public InGameStoryHotelProbe(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (s_completed) return;
            if (!StoryHotelProbeBootstrap.FreshMode && !StoryHotelProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                if (StoryHotelProbeBootstrap.FreshMode)
                {
                    RunFreshProbe();
                }
                else if (StoryHotelProbeBootstrap.ReloadMode)
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
                StoryStage_HotelVisit.TestCompanionGenerateOverride = null;
                StoryStage_HotelVisit.TestHunterRaidExecuteOverride = null;
                StoryPayment.TestSplitOffOverride = null;
                StoryPayment.TestBeforeDestroyOverride = null;
                StoryPawnTransfer.TestSpawnOverride = null;
                Application.logMessageReceived -= OnUnityLog;

                bool allPassed = WriteReport(StoryHotelProbeBootstrap.ReloadMode ? "StoryHotelReloadProbe.json" : "StoryHotelProbe.json");
                if (!StoryHotelProbeBootstrap.ReloadMode)
                {
                    WriteUiReport(allPassed);
                }
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                // Expected errors from deliberately injected test failures are not regressions.
                if (condition != null && condition.Contains("Simulated ")) return;
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

            // ---------------- 1. NO HOTEL IMMEDIATE ON START ----------------
            SetGuestsWelcome(map, false);
            story.SetFlag("probe.e3.noHotel", "true");
            story.DirectorTick();
            StoryQuestRun noHotelRun = story.RunFor(StoryHotelProbeBootstrap.QNoHotel);
            bool noHotelImmediate = noHotelRun != null && GetVar(noHotelRun, "noHotel") == "true" &&
                                    GetVar(noHotelRun, "immediateOutcome") == "noHotel" &&
                                    GetVar(noHotelRun, "arrived") == "false";
            story.DirectorTick();
            bool noHotelFinished = noHotelRun != null && HasOutcome(noHotelRun, "noHotel");
            AddCheck("Compat_NoHotelImmediateOnStart", noHotelImmediate && noHotelFinished,
                "immediate noHotel & 0 spawns",
                string.Format("immediate={0}, finished={1}", noHotelImmediate, noHotelFinished));

            // ---------------- BUILD HOTEL & PLAYER STORAGE ZONE ----------------
            bool hotelBuilt = BuildTestHotel(map);
            SetGuestsWelcome(map, true);
            bool hotelOpen = StoryStage_HotelVisit.HasOpenHotel(map);
            AddCheck("Setup_HotelBuiltAndOpen", hotelBuilt && hotelOpen, "built=true, open=true",
                string.Format("built={0}, open={1}", hotelBuilt, hotelOpen));

            Zone_Stockpile stockpile = EnsureTestStockpile(map);
            AddCheck("Setup_StockpileReady", stockpile != null && stockpile.cells.Count >= 9, ">=9 cells",
                stockpile != null ? stockpile.cells.Count.ToString() : "null");

            // ---------------- 2. HERO-COLONIST IS NOT STOLEN ----------------
            StoryQuestDef heroSkipDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHeroColonistSkip);
            StoryStage_HotelVisit heroSkipStage = heroSkipDef != null ? heroSkipDef.stages[0] as StoryStage_HotelVisit : null;
            Pawn colonistHero = (heroSkipStage != null && heroSkipStage.hero != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(heroSkipStage.hero) : null;
            if (colonistHero != null)
            {
                if (colonistHero.Spawned) colonistHero.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns.Contains(colonistHero)) Find.WorldPawns.RemovePawn(colonistHero);
                colonistHero.SetFaction(Faction.OfPlayer);
                IntVec3 origCell = FindStandable(map);
                GenSpawn.Spawn(colonistHero, origCell, map, WipeMode.Vanish);

                story.SetFlag("probe.e3.heroColonist", "true");
                story.DirectorTick();
                StoryQuestRun skipRun = story.RunFor(StoryHotelProbeBootstrap.QHeroColonistSkip);

                bool heroSkipped = skipRun != null && GetVar(skipRun, "heroIncluded") == "false";
                bool heroUntouched = colonistHero.Faction == Faction.OfPlayer &&
                                     colonistHero.Spawned && colonistHero.Map == map &&
                                     colonistHero.Position == origCell &&
                                     !ParseIntList(GetVar(skipRun, "guestIds")).Contains(colonistHero.thingIDNumber);
                AddCheck("P1_HeroColonistNotMoved", heroSkipped && heroUntouched,
                    "heroIncluded=false & colonist untouched",
                    string.Format("skipped={0}, reason={1}, untouched={2}", heroSkipped, GetVar(skipRun, "heroSkipReason"), heroUntouched));

                CleanupActiveHotelRun(story, heroSkipDef, skipRun);
            }
            else
            {
                AddCheck("P1_HeroColonistNotMoved", false, "hero exists", "hero null");
            }

            // ---------------- 3. HERO UNTOUCHED ON COMPANION GENERATION EXCEPTION (TestMode) ----------------
            if (TestMode.Enabled)
            {
                StoryQuestDef compExDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHeroCompanionException);
                StoryStage_HotelVisit compExStage = compExDef != null ? compExDef.stages[0] as StoryStage_HotelVisit : null;
                Pawn worldHero = (compExStage != null && compExStage.hero != null && HeroRegistry.Instance != null)
                    ? HeroRegistry.Instance.GetOrCreate(compExStage.hero) : null;

                if (worldHero != null && Find.WorldPawns.Contains(worldHero) && !worldHero.Spawned)
                {
                    Faction heroOrigFac = worldHero.Faction;
                    if (heroOrigFac != null && !heroOrigFac.IsPlayer && !heroOrigFac.defeated &&
                        (heroOrigFac.def == null || !heroOrigFac.def.permanentEnemy))
                    {
                        compExStage.faction = heroOrigFac.def.defName;
                    }

                    StoryStage_HotelVisit.TestCompanionGenerateOverride = delegate(PawnGenerationRequest req)
                    {
                        throw new InvalidOperationException("Simulated companion generation exception");
                    };

                    story.SetFlag("probe.e3.heroCompEx", "true");
                    story.DirectorTick();
                    StoryStage_HotelVisit.TestCompanionGenerateOverride = null;
                    story.DirectorTick();

                    StoryQuestRun exRun = story.RunFor(StoryHotelProbeBootstrap.QHeroCompanionException);
                    bool heroStillInWorld = Find.WorldPawns.Contains(worldHero) && !worldHero.Spawned && worldHero.Faction == heroOrigFac;
                    bool failedCleanly = exRun != null && HasOutcome(exRun, "failed") && GetVar(exRun, "arrived") == "false";

                    AddCheck("R2_HotelCompanionException_HeroUntouchedInWorldPawns",
                        heroStillInWorld && failedCleanly,
                        "hero in WorldPawns with origFaction, outcome=failed",
                        string.Format("inWorld={0}, facMatch={1}, hist={2}",
                            Find.WorldPawns.Contains(worldHero), worldHero.Faction == heroOrigFac,
                            exRun != null ? string.Join(",", exRun.history.ToArray()) : "null"));
                }
                else
                {
                    AddCheck("R2_HotelCompanionException_HeroUntouchedInWorldPawns", false, "free world hero exists", "not free");
                }
            }

            // ---------------- 4. PERMANENT ENEMY FACTION -> FAILED WITHOUT SPAWN ----------------
            Faction permEnemy = FindPermanentEnemyHumanlikeFaction();
            StoryQuestDef permDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QPermanentEnemy);
            StoryStage_HotelVisit permStage = permDef != null ? permDef.stages[0] as StoryStage_HotelVisit : null;
            if (permEnemy != null && permStage != null)
            {
                permStage.faction = permEnemy.def.defName;
                int pawnsBefore = map.mapPawns.AllPawnsSpawnedCount;

                story.SetFlag("probe.e3.permEnemy", "true");
                story.DirectorTick();
                story.DirectorTick();

                StoryQuestRun permRun = story.RunFor(StoryHotelProbeBootstrap.QPermanentEnemy);
                int pawnsAfter = map.mapPawns.AllPawnsSpawnedCount;
                bool failedNoSpawn = permRun != null && HasOutcome(permRun, "failed") &&
                                     GetVar(permRun, "arrived") == "false" && pawnsAfter == pawnsBefore;
                AddCheck("P1_PermanentEnemyFailsWithoutSpawn", failedNoSpawn,
                    "outcome=failed, arrived=false, 0 spawned",
                    string.Format("hist={0}, arrived={1}, pawns={2}->{3}",
                        permRun != null ? string.Join(",", permRun.history.ToArray()) : "null",
                        GetVar(permRun, "arrived"), pawnsBefore, pawnsAfter));
            }
            else
            {
                AddCheck("P1_PermanentEnemyFailsWithoutSpawn", false, "permanentEnemy faction exists", "missing");
            }

            // ---------------- 5. OVERLAPPING TRUCE VISITS ON THE SAME HOSTILE FACTION ----------------
            Faction goodwillFac = FindGoodwillHumanlikeFaction();
            StoryQuestDef overlapADef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceOverlapA);
            StoryQuestDef overlapBDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceOverlapB);
            StoryStage_HotelVisit overlapAStage = overlapADef != null ? overlapADef.stages[0] as StoryStage_HotelVisit : null;
            StoryStage_HotelVisit overlapBStage = overlapBDef != null ? overlapBDef.stages[0] as StoryStage_HotelVisit : null;

            if (goodwillFac != null && overlapAStage != null && overlapBStage != null)
            {
                ForceHostileWithGoodwill(goodwillFac, -85);
                overlapAStage.faction = goodwillFac.def.defName;
                overlapBStage.faction = goodwillFac.def.defName;

                // Start Visit A -> faction enters truce (Neutral)
                story.SetFlag("probe.e3.truceOverlapA", "true");
                story.DirectorTick();
                StoryQuestRun runA = story.RunFor(StoryHotelProbeBootstrap.QTruceOverlapA);
                bool aTruceActive = !goodwillFac.HostileTo(Faction.OfPlayer) && !GetVar(runA, "truceLeaseId").NullOrEmpty();

                // Start Visit B while faction ALREADY appears Neutral -> MUST acquire its own counted lease!
                story.SetFlag("probe.e3.truceOverlapB", "true");
                story.DirectorTick();
                StoryQuestRun runB = story.RunFor(StoryHotelProbeBootstrap.QTruceOverlapB);
                bool bGotLease = runB != null && GetVar(runB, "arrived") == "true" && !GetVar(runB, "truceLeaseId").NullOrEmpty();

                // End Visit A peacefully via real ExitMap -> faction MUST STILL BE NEUTRAL for Visit B!
                List<int> guestsA = ParseIntList(GetVar(runA, "guestIds"));
                ExitGuestsViaRealExitMap(map, guestsA);
                story.DirectorTick();
                bool aSucceeded = runA != null && HasOutcome(runA, "success");
                bool stillNeutralAfterA = !goodwillFac.HostileTo(Faction.OfPlayer) && !Faction.OfPlayer.HostileTo(goodwillFac) &&
                                          StoryRelationRestorer.Instance != null && StoryRelationRestorer.Instance.HasActiveTruceBackup(goodwillFac);

                // Now end Visit B peacefully via real ExitMap -> faction MUST RESTORE HOSTILE symmetrically!
                List<int> guestsB = ParseIntList(GetVar(runB, "guestIds"));
                ExitGuestsViaRealExitMap(map, guestsB);
                story.DirectorTick();
                bool bSucceeded = runB != null && HasOutcome(runB, "success");
                bool restoredSymHostile = goodwillFac.HostileTo(Faction.OfPlayer) && Faction.OfPlayer.HostileTo(goodwillFac) &&
                                          goodwillFac.PlayerGoodwill <= -80 && goodwillFac.PlayerGoodwill >= -90 &&
                                          Faction.OfPlayer.GoodwillWith(goodwillFac) == goodwillFac.PlayerGoodwill;

                AddCheck("R2_OverlappingTruceVisits_StaysNeutralUntilLastEnds",
                    aTruceActive && bGotLease && aSucceeded && stillNeutralAfterA && bSucceeded && restoredSymHostile,
                    "A+B leased, neutral after A, symmetric Hostile(~-85) after B",
                    string.Format("aTruce={0}, bLease={1}, aSucc={2}, neutralAfterA={3}, bSucc={4}, symHostile={5}, gw={6}/{7}",
                        aTruceActive, bGotLease, aSucceeded, stillNeutralAfterA, bSucceeded, restoredSymHostile,
                        goodwillFac.PlayerGoodwill, Faction.OfPlayer.GoodwillWith(goodwillFac)));
            }
            else
            {
                AddCheck("R2_OverlappingTruceVisits_StaysNeutralUntilLastEnds", false, "goodwill faction & defs exist", "missing");
            }

            // ---------------- 6. ORPHAN TRUCE ROLLBACK VIA REAL STAGE REMOVAL ----------------
            StoryQuestDef orphanDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceStageRemovedOrphan);
            StoryStage_HotelVisit orphanStage = orphanDef != null && orphanDef.stages.Count > 0 ? orphanDef.stages[0] as StoryStage_HotelVisit : null;
            if (goodwillFac != null && orphanDef != null && orphanStage != null)
            {
                ForceHostileWithGoodwill(goodwillFac, -88);
                orphanStage.faction = goodwillFac.def.defName;

                story.SetFlag("probe.e3.truceStageRemoved", "true");
                story.DirectorTick();
                StoryQuestRun orphRun = story.RunFor(StoryHotelProbeBootstrap.QTruceStageRemovedOrphan);
                bool enteredTruce = !goodwillFac.HostileTo(Faction.OfPlayer) && GetVar(orphRun, "arrived") == "true";
                List<int> orphGuests = ParseIntList(GetVar(orphRun, "guestIds"));

                // REAL stage removal: empty orphanDef.stages and tick SparkStory!
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
                bool restoredSym = goodwillFac.HostileTo(Faction.OfPlayer) && Faction.OfPlayer.HostileTo(goodwillFac) &&
                                   goodwillFac.PlayerGoodwill <= -83 && goodwillFac.PlayerGoodwill >= -93;

                AddCheck("R2_TruceOrphan_RealStageRemovalRollback",
                    enteredTruce && disabledNaturally && restoredSym,
                    "truce->Disable(stage missing)->Symmetric Hostile(~-88)",
                    string.Format("truce={0}, disabled={1}, symHostile={2}, gw={3}",
                        enteredTruce, disabledNaturally, restoredSym, goodwillFac.PlayerGoodwill));

                CleanupPawnsByIds(map, orphGuests);
                ForceNeutralWithGoodwill(goodwillFac, 10);
            }
            else
            {
                AddCheck("R2_TruceOrphan_RealStageRemovalRollback", false, "goodwill faction & def exist", "missing");
            }

            // ---------------- 7. GUEST KILLED, IMPRISONED, OR VANISHED WITHOUT EXITMAP -> FAILED ----------------
            // 7A. Killed
            story.SetFlag("probe.e3.hostKilled", "true");
            story.DirectorTick();
            StoryQuestRun killRun = story.RunFor(StoryHotelProbeBootstrap.QHostKilled);
            List<int> killGuests = ParseIntList(GetVar(killRun, "guestIds"));
            KillFirstGuest(map, killGuests);
            story.DirectorTick();
            AddCheck("P1_GuestKilled_NotSuccess",
                killRun != null && HasOutcome(killRun, "failed") && !HasOutcome(killRun, "success"),
                "failed", killRun != null ? string.Join(",", killRun.history.ToArray()) : "null");
            CleanupPawnsByIds(map, killGuests);

            // 7B. Imprisoned
            story.SetFlag("probe.e3.hostImprisoned", "true");
            story.DirectorTick();
            StoryQuestRun prisRun = story.RunFor(StoryHotelProbeBootstrap.QHostImprisoned);
            List<int> prisGuests = ParseIntList(GetVar(prisRun, "guestIds"));
            Pawn firstPrisGuest = FindGuestOnMap(map, prisGuests);
            if (firstPrisGuest != null && firstPrisGuest.guest != null)
            {
                // A registered Hospitality guest of a peaceful faction cannot be turned into a prisoner by a bare status change
                // (Hospitality rewrites it back to Guest). Capture the way the game does it.
                firstPrisGuest.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                AddCheck("P1_BareStatusChangeDoesNotImprisonGuest", !firstPrisGuest.IsPrisonerOfColony && !HasOutcome(prisRun, "failed"), "still a guest", firstPrisGuest.GuestStatus.ToString());
                firstPrisGuest.guest.CapturedBy(Faction.OfPlayer, map.mapPawns.FreeColonistsSpawned[0]);
            }
            Faction prisFaction = firstPrisGuest != null ? firstPrisGuest.Faction : null;
            string prisState = firstPrisGuest == null ? "null" : "prisoner=" + firstPrisGuest.IsPrisonerOfColony + " hostile=" + (prisFaction != null && prisFaction.HostileTo(Faction.OfPlayer));
            story.DirectorTick();
            AddCheck("P1_GuestImprisoned_NotSuccess",
                prisRun != null && HasOutcome(prisRun, "failed") && !HasOutcome(prisRun, "success"),
                "failed", (prisRun != null ? string.Join(",", prisRun.history.ToArray()) : "null") + " " + GetVar(prisRun, "failReason") + " " + prisState);
            CleanupPawnsByIds(map, prisGuests);
            if (prisFaction != null && prisFaction.HasGoodwill && prisFaction.HostileTo(Faction.OfPlayer)) ForceNeutralWithGoodwill(prisFaction, 10); // later checks expect the peaceful faction

            // 7C. Vanished without Pawn.ExitMap -> MUST fail (guest_missing), NEVER success
            story.SetFlag("probe.e3.hostVanished", "true");
            story.DirectorTick();
            StoryQuestRun vanRun = story.RunFor(StoryHotelProbeBootstrap.QHostVanishedNoExit);
            List<int> vanGuests = ParseIntList(GetVar(vanRun, "guestIds"));
            Pawn vanGuest = FindGuestOnMap(map, vanGuests);
            if (vanGuest != null && vanGuest.Spawned)
            {
                vanGuest.DeSpawn(DestroyMode.Vanish);
                if (!Find.WorldPawns.Contains(vanGuest)) Find.WorldPawns.PassToWorld(vanGuest, PawnDiscardDecideMode.Discard);
            }
            story.DirectorTick();
            AddCheck("R2_GuestVanishedWithoutExitMap_FailedNotSuccess",
                vanRun != null && HasOutcome(vanRun, "failed") && !HasOutcome(vanRun, "success") && GetVar(vanRun, "failReason") == "guest_missing",
                "failed (guest_missing)",
                string.Format("hist={0}, reason={1}", vanRun != null ? string.Join(",", vanRun.history.ToArray()) : "null", GetVar(vanRun, "failReason")));

            // ---------------- 8. INVESTIGATE REQUIRES REAL PlayLogEntry_Interaction ----------------
            StoryQuestDef invDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QInvestigateStrict);
            story.SetFlag("probe.e3.investigate", "true");
            story.DirectorTick();
            StoryQuestRun invRun = story.RunFor(StoryHotelProbeBootstrap.QInvestigateStrict);
            List<int> invGuests = ParseIntList(GetVar(invRun, "guestIds"));
            Pawn invGuest = FindGuestOnMap(map, invGuests);

            Pawn talker = SpawnCleanColonist(map);
            if (talker.skills != null)
            {
                SkillRecord soc = talker.skills.GetSkill(SkillDefOf.Social);
                if (soc != null) soc.Level = 15;
            }
            if (invGuest != null) talker.Position = invGuest.Position;
            story.DirectorTick();
            bool notInvestigatedWithoutChat = GetVar(invRun, "investigated") == "false";

            bool realChatDetected = false;
            if (invGuest != null && Find.PlayLog != null)
            {
                // Real vanilla interaction: the game itself writes the PlayLogEntry_Interaction.
                bool talked = talker.interactions != null && talker.interactions.TryInteractWith(invGuest, InteractionDefOf.Chitchat);
                if (!talked) Log.Warning("[TMM] StoryHotel probe: TryInteractWith(Chitchat) returned false");
                story.DirectorTick();
                realChatDetected = GetVar(invRun, "investigated") == "true";
            }
            AddCheck("P1_InvestigateRequiresRealInteraction", notInvestigatedWithoutChat && realChatDetected,
                "withoutChat=false, afterRealChat=true",
                string.Format("noChatFalse={0}, afterChat={1}", notInvestigatedWithoutChat, realChatDetected));
            CleanupActiveHotelRun(story, invDef, invRun);

            // ---------------- 9. HIDE: STAGE-OWNED HUNTER RAID IGNORES UNRELATED HOSTILE LORDS ----------------
            Faction hunterFac = permEnemy ?? FindAnotherHostileFaction(goodwillFac);
            StoryQuestDef hideOwnedDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHideOwnedRaid);
            StoryStage_HotelVisit hideOwnedStage = hideOwnedDef != null ? hideOwnedDef.stages[0] as StoryStage_HotelVisit : null;
            if (hunterFac != null && hideOwnedStage != null)
            {
                hideOwnedStage.hunterFaction = hunterFac.def.defName;
                story.SetFlag("probe.e3.hideOwned", "true");
                story.DirectorTick();
                StoryQuestRun hideRun = story.RunFor(StoryHotelProbeBootstrap.QHideOwnedRaid);
                List<int> hideGuests = ParseIntList(GetVar(hideRun, "guestIds"));

                // Spawn an UNRELATED hostile lord of hunterFac BEFORE hunterRaidTick and kill it!
                Pawn unrelatedRaider = SpawnCleanNpc(map, hunterFac);
                Lord unrelatedLord = LordMaker.MakeNewLord(hunterFac, new LordJob_DefendPoint(unrelatedRaider.Position), map, new List<Pawn> { unrelatedRaider });
                story.DirectorTick();
                bool unrelatedNotBound = GetVar(hideRun, "hunterRaidBound") == "false" && GetVar(hideRun, "hunterRaidLaunched") == "false";
                if (!unrelatedRaider.Dead) unrelatedRaider.Kill(null);
                story.DirectorTick();
                bool unrelatedKillNotCleared = GetVar(hideRun, "hunterRaidCleared") == "false";

                // Advance to hunterRaidTick -> Hide launches and binds ITS OWN raid!
                int raidTick = ParseInt(hideRun, "hunterRaidTick", Find.TickManager.TicksGame);
                Find.TickManager.DebugSetTicksGame(raidTick + 10); // CHECK API
                story.DirectorTick();

                bool ownRaidBound = GetVar(hideRun, "hunterRaidLaunched") == "true" &&
                                    GetVar(hideRun, "hunterRaidBound") == "true" &&
                                    ParseIntList(GetVar(hideRun, "hunterRaidPawnIds")).Count > 0;
                List<int> boundRaiders = ParseIntList(GetVar(hideRun, "hunterRaidPawnIds"));

                // Kill ONLY the stage's bound raiders -> hunterRaidCleared becomes true!
                KillPawnsByIds(map, boundRaiders);
                story.DirectorTick();
                bool ownRaidCleared = GetVar(hideRun, "hunterRaidCleared") == "true";

                // Now guests depart peacefully via ExitMap -> Hide succeeds!
                ExitGuestsViaRealExitMap(map, hideGuests);
                story.DirectorTick();
                bool hideSucceeded = hideRun != null && HasOutcome(hideRun, "success");

                AddCheck("R2_Hide_IgnoresUnrelatedAndOwnsItsHunterRaid",
                    unrelatedNotBound && unrelatedKillNotCleared && ownRaidBound && ownRaidCleared && hideSucceeded,
                    "unrelated ignored, own raid bound->cleared->success",
                    string.Format("unrelIgnored={0}/{1}, ownBound={2}({3}), cleared={4}, succ={5}",
                        unrelatedNotBound, unrelatedKillNotCleared, ownRaidBound, boundRaiders.Count, ownRaidCleared, hideSucceeded));
            }
            else
            {
                AddCheck("R2_Hide_IgnoresUnrelatedAndOwnsItsHunterRaid", false, "hunterFac exists", "missing");
            }

            // ---------------- 10. HIDE: FAILED HUNTER RAID REACHES HARD DEADLINE (TestMode) ----------------
            if (TestMode.Enabled && hunterFac != null)
            {
                StoryQuestDef hideFailDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHideFailedRaidDeadline);
                StoryStage_HotelVisit hideFailStage = hideFailDef != null ? hideFailDef.stages[0] as StoryStage_HotelVisit : null;
                if (hideFailStage != null)
                {
                    hideFailStage.hunterFaction = hunterFac.def.defName;
                    StoryStage_HotelVisit.TestHunterRaidExecuteOverride = delegate(IncidentParms p) { return false; };

                    story.SetFlag("probe.e3.hideFailed", "true");
                    story.DirectorTick();
                    StoryQuestRun hfRun = story.RunFor(StoryHotelProbeBootstrap.QHideFailedRaidDeadline);
                    List<int> hfGuests = ParseIntList(GetVar(hfRun, "guestIds"));

                    int rTick = ParseInt(hfRun, "hunterRaidTick", Find.TickManager.TicksGame);
                    Find.TickManager.DebugSetTicksGame(rTick + 10);
                    story.DirectorTick();
                    StoryStage_HotelVisit.TestHunterRaidExecuteOverride = null;

                    int hardDl = ParseInt(hfRun, "hardDeadlineTick", Find.TickManager.TicksGame + GenDate.TicksPerDay);
                    Find.TickManager.DebugSetTicksGame(hardDl + 50);
                    story.DirectorTick();

                    bool failedAtDeadline = hfRun != null && !HasOutcome(hfRun, "success") &&
                                            (HasOutcome(hfRun, "failed") || HasOutcome(hfRun, "timeout")) &&
                                            GetVar(hfRun, "hunterRaidBound") == "false";
                    AddCheck("R2_Hide_FailedRaidReachesDeadlineNotSuccess", failedAtDeadline,
                        "failed/timeout & hunterRaidBound=false",
                        string.Format("hist={0}, bound={1}, reason={2}",
                            hfRun != null ? string.Join(",", hfRun.history.ToArray()) : "null",
                            GetVar(hfRun, "hunterRaidBound"), GetVar(hfRun, "failReason")));
                    CleanupPawnsByIds(map, hfGuests);
                }
            }

            // ---------------- 11. DELIVER: EXPIRED LETTER CLICK DEDUCTS NOTHING ----------------
            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 40, 0);
            int steelBeforeExpired = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            story.SetFlag("probe.e3.deliverExpired", "true");
            story.DirectorTick();
            StoryQuestRun expRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverExpiredClick);
            ChoiceLetter_StoryDeliver expLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverExpiredClick);

            int expDl = ParseInt(expRun, "deadlineTick", Find.TickManager.TicksGame + GenDate.TicksPerDay);
            Find.TickManager.DebugSetTicksGame(expDl + 50); // CHECK API

            bool uiExpiredRefused = false;
            if (expLetter != null)
            {
                ClickFirstEnabledOption(expLetter);
                uiExpiredRefused = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel) == steelBeforeExpired &&
                                   GetVar(expRun, "delivered") != "true";
            }
            story.DirectorTick();
            bool expTimedOut = expRun != null && HasOutcome(expRun, "timeout");
            AddCheck("P1_DeliverExpiredClick_DeductsNothing",
                expLetter != null && uiExpiredRefused && expTimedOut,
                "steel unchanged, delivered!=true, timeout",
                string.Format("letter={0}, refused={1}, timeout={2}", expLetter != null, uiExpiredRefused, expTimedOut));

            // ---------------- 12. DELIVER: UNKNOWN/MISSING deadlineTick ALWAYS REFUSES PAYMENT ----------------
            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 40, 0);
            story.SetFlag("probe.e3.deliverUnknownDl", "true");
            story.DirectorTick();
            StoryQuestRun unkRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverUnknownDeadline);
            ChoiceLetter_StoryDeliver unkLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverUnknownDeadline);
            if (unkRun != null && unkLetter != null)
            {
                // Remove deadlineTick from run.vars while letter still carries a future deadlineTick!
                unkRun.vars.Remove("deadlineTick");
                unkLetter.deadlineTick = Find.TickManager.TicksGame + GenDate.TicksPerDay * 5;

                StoryQuestRun vRun;
                Map vMap;
                ThingDef vDef;
                int vCount;
                string vReason;
                bool validated = StoryStage_Deliver.ValidateColonyHandIn(unkLetter, out vRun, out vMap, out vDef, out vCount, out vReason);
                ClickFirstEnabledOption(unkLetter);
                int steelAfter = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                story.DirectorTick();

                bool refusedAndTimedOut = !validated && vReason == "deadline_missing_or_passed" &&
                                          steelAfter == 40 && HasOutcome(unkRun, "timeout");
                AddCheck("R2_Deliver_UnknownDeadlineAlwaysRefuses", refusedAndTimedOut,
                    "validated=false(deadline_missing_or_passed), steel=40, timeout",
                    string.Format("val={0}({1}), steel={2}, hist={3}",
                        validated, vReason ?? "null", steelAfter, string.Join(",", unkRun.history.ToArray())));
            }
            else
            {
                AddCheck("R2_Deliver_UnknownDeadlineAlwaysRefuses", false, "run & letter created", "missing");
            }

            // ---------------- 13. DELIVER: IMMUTABLE CONTRACT IGNORES TAMPERED LETTER FIELDS ----------------
            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 50, 0);
            story.SetFlag("probe.e3.deliverImmutable", "true");
            story.DirectorTick();
            StoryQuestRun immRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverImmutableContract);
            ChoiceLetter_StoryDeliver immLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverImmutableContract);
            if (immRun != null && immLetter != null)
            {
                // Tamper with letter fields (1 WoodLog instead of 30 Steel)
                immLetter.requiredCount = 1;
                immLetter.thingDefName = ThingDefOf.WoodLog.defName;
                immLetter.deliverMapId = 999999;

                bool clicked = ClickFirstEnabledOption(immLetter);
                int steelAfter = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                story.DirectorTick();

                bool enforcedContract = clicked && steelAfter == 20 && HasOutcome(immRun, "success");
                AddCheck("R2_Deliver_ImmutableContractIgnoresLetterTamper", enforcedContract,
                    "deducted 30 Steel (50->20), outcome=success",
                    string.Format("clicked={0}, steelAfter={1}, hist={2}", clicked, steelAfter, string.Join(",", immRun.history.ToArray())));
            }
            else
            {
                AddCheck("R2_Deliver_ImmutableContractIgnoresLetterTamper", false, "run & letter created", "missing");
            }

            // ---------------- 14. DELIVER: REMOVED stageKey REFUSES (NO STALE INDEX FALLBACK) ----------------
            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 50, 0);
            StoryQuestDef keyRemDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QDeliverStageKeyRemoved);
            StoryStage origDeliverStage = keyRemDef != null && keyRemDef.stages.Count > 0 ? keyRemDef.stages[0] : null;
            story.SetFlag("probe.e3.deliverKeyRemoved", "true");
            story.DirectorTick();
            StoryQuestRun keyRemRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverStageKeyRemoved);
            ChoiceLetter_StoryDeliver keyRemLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverStageKeyRemoved);
            if (keyRemDef != null && origDeliverStage != null && keyRemRun != null && keyRemLetter != null)
            {
                StoryStage_Deliver replacement = new StoryStage_Deliver();
                replacement.key = "different_stage_key";
                replacement.thing = ThingDefOf.Steel;
                replacement.count = 10;
                replacement.to = StoryDeliverTarget.colony;

                bool refusedOnRemovedKey = false;
                string valReason = null;
                int steelAfterClick = -1;
                try
                {
                    // Replace index 0 with a DIFFERENT stageKey (run.stage is still 0, but "deliver" is gone!)
                    keyRemDef.stages[0] = replacement;
                    StoryQuestRun vRun;
                    Map vMap;
                    ThingDef vDef;
                    int vCount;
                    bool valid = StoryStage_Deliver.ValidateColonyHandIn(keyRemLetter, out vRun, out vMap, out vDef, out vCount, out valReason);
                    ClickFirstEnabledOption(keyRemLetter);
                    steelAfterClick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                    refusedOnRemovedKey = !valid && valReason == "stage_key_removed" && steelAfterClick == 50 && GetVar(keyRemRun, "delivered") != "true";
                }
                finally
                {
                    keyRemDef.stages[0] = origDeliverStage;
                    // End the test quest cleanly
                    if (keyRemLetter.IsInLetterStack) Find.LetterStack.RemoveLetter(keyRemLetter);
                    int dl = ParseInt(keyRemRun, "deadlineTick", Find.TickManager.TicksGame);
                    Find.TickManager.DebugSetTicksGame(dl + 10);
                    story.DirectorTick();
                }

                AddCheck("R2_Deliver_RemovedStageKeyRefusesWithoutIndexFallback", refusedOnRemovedKey,
                    "valid=false(stage_key_removed), steel=50",
                    string.Format("refused={0}, reason={1}, steel={2}", refusedOnRemovedKey, valReason ?? "null", steelAfterClick));
            }
            else
            {
                AddCheck("R2_Deliver_RemovedStageKeyRefusesWithoutIndexFallback", false, "def/run/letter exist", "missing");
            }

            // ---------------- 15. DELIVER: ATOMIC ROLLBACK ON SECOND STACK FAILURE (TestMode) ----------------
            if (TestMode.Enabled)
            {
                ClearStockpileResources(map, ThingDefOf.Steel);
                PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 30, 0);
                PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 30, 1);
                int steelBeforeAtomic = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

                story.SetFlag("probe.e3.deliverAtomic", "true");
                story.DirectorTick();
                StoryQuestRun atomicRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverSecondStackFail);
                ChoiceLetter_StoryDeliver atomicLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverSecondStackFail);

                StoryPayment.TestSplitOffOverride = delegate(Thing src, int count, int stepIndex)
                {
                    if (stepIndex == 1) return null;
                    return src.SplitOff(count);
                };

                bool clickedAtomic = atomicLetter != null && ClickFirstEnabledOption(atomicLetter);
                StoryPayment.TestSplitOffOverride = null;

                int steelAfterAtomicFail = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                bool atomicRolledBack = clickedAtomic && steelBeforeAtomic == 60 && steelAfterAtomicFail == 60 &&
                                        GetVar(atomicRun, "delivered") != "true" &&
                                        atomicLetter != null && atomicLetter.IsInLetterStack;
                AddCheck("P1_DeliverAtomicRollback_SecondStackFail", atomicRolledBack,
                    "steel=60->60, delivered!=true, letter stays",
                    string.Format("clicked={0}, steel={1}->{2}, delivered={3}",
                        clickedAtomic, steelBeforeAtomic, steelAfterAtomicFail, GetVar(atomicRun, "delivered")));

                // Also test commit-phase Destroy exception safety (onPaymentSecured already recorded delivered=true)
                ClearStockpileResources(map, ThingDefOf.Steel);
                PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 40, 0);
                story.SetFlag("probe.e3.deliverCommitEx", "true");
                story.DirectorTick();
                StoryQuestRun commitRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverCommitDestroyEx);
                ChoiceLetter_StoryDeliver commitLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverCommitDestroyEx);

                StoryPayment.TestBeforeDestroyOverride = delegate(Thing t, int idx)
                {
                    throw new InvalidOperationException("Simulated Destroy exception after onPaymentSecured");
                };
                bool clickedCommit = commitLetter != null && ClickFirstEnabledOption(commitLetter);
                StoryPayment.TestBeforeDestroyOverride = null;
                story.DirectorTick();

                bool commitSafe = clickedCommit && commitRun != null && HasOutcome(commitRun, "success") &&
                                  StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel) == 20;
                AddCheck("R2_Deliver_CommitDestroyExceptionKeepsRecordedPayment", commitSafe,
                    "outcome=success, remaining steel=20",
                    string.Format("clicked={0}, steel={1}, hist={2}",
                        clickedCommit, StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel),
                        commitRun != null ? string.Join(",", commitRun.history.ToArray()) : "null"));

                // 15b. StoryPayment.ExecuteSplitOff exception safety:
                // Case A: Handler throws before piece was handed off -> source refunded exactly.
                Thing srcA = ThingMaker.MakeThing(ThingDefOf.Silver);
                srcA.stackCount = 50;
                bool threwA = false;
                StoryPayment.TestSimulatePostSplitOffHandler = delegate(Thing s, Thing p)
                {
                    threwA = true;
                    throw new InvalidOperationException("Simulated PostSplitOff handler exception before handoff");
                };
                try
                {
                    StoryPayment.ExecuteSplitOff(srcA, 20, 0);
                }
                catch (InvalidOperationException) { }
                StoryPayment.TestSimulatePostSplitOffHandler = null;
                int finalStackA = srcA.stackCount;
                bool caseAOk = threwA && finalStackA == 50;

                // Case B: Handler hands off piece to a holder and then throws -> source NOT refunded (no double silver)
                Thing srcB = ThingMaker.MakeThing(ThingDefOf.Silver);
                srcB.stackCount = 50;
                bool threwB = false;
                Thing pieceHolder = null;
                StoryPayment.TestSimulatePostSplitOffHandler = delegate(Thing s, Thing p)
                {
                    GenPlace.TryPlaceThing(p, map.Center, map, ThingPlaceMode.Near);
                    pieceHolder = p;
                    threwB = true;
                    throw new InvalidOperationException("Simulated PostSplitOff handler exception after handoff");
                };
                try
                {
                    StoryPayment.ExecuteSplitOff(srcB, 20, 0);
                }
                catch (InvalidOperationException) { }
                StoryPayment.TestSimulatePostSplitOffHandler = null;
                int finalStackB = srcB.stackCount;
                bool caseBOk = threwB && finalStackB == 30 && pieceHolder != null && pieceHolder.Spawned;
                if (pieceHolder != null && pieceHolder.Spawned) pieceHolder.Destroy(DestroyMode.Vanish);
                if (!srcA.Destroyed) srcA.Destroy(DestroyMode.Vanish);
                if (!srcB.Destroyed) srcB.Destroy(DestroyMode.Vanish);

                AddCheck("StoryPayment_SplitOffException_RefundsOnlyUnheldPiece", caseAOk && caseBOk,
                    "caseA=50->50 (refunded), caseB=50->30 (held on map, not refunded)",
                    string.Format("caseA={0}(stack={1}), caseB={2}(stack={3})",
                        caseAOk, finalStackA, caseBOk, finalStackB));

                // 15c. StoryPaymentLimbo: drop failure preserves item in held container
                StoryPaymentLimbo limbo = Find.World != null ? Find.World.GetComponent<StoryPaymentLimbo>() : null;
                if (limbo != null)
                {
                    Thing limboItem = ThingMaker.MakeThing(ThingDefOf.Gold);
                    limboItem.stackCount = 77;
                    StoryPaymentLimbo.Hold(limboItem);
                    bool heldInitially = limbo.GetDirectlyHeldThings().Contains(limboItem);

                    StoryPaymentLimbo.TestLimboDropOverride = delegate(Thing item, IntVec3 pos, Map m)
                    {
                        return false; // Simulate placement failure
                    };

                    limbo.TryDropAllHeldToMap(map, map.Center);
                    StoryPaymentLimbo.TestLimboDropOverride = null;

                    bool stillHeldAfterFail = limbo.GetDirectlyHeldThings().Contains(limboItem) && !limboItem.Spawned;

                    // Now drop normally
                    limbo.TryDropAllHeldToMap(map, map.Center);
                    bool spawnedAfterSuccess = limboItem.Spawned && limboItem.Map == map && !limbo.GetDirectlyHeldThings().Contains(limboItem);

                    AddCheck("StoryPaymentLimbo_FailedDropPreservesItemInHolder",
                        heldInitially && stillHeldAfterFail && spawnedAfterSuccess,
                        "heldInit=true, stillHeldAfterFail=true, spawnedAfterSuccess=true",
                        string.Format("heldInit={0}, stillHeld={1}, spawned={2}",
                            heldInitially, stillHeldAfterFail, spawnedAfterSuccess));

                    if (limboItem.Spawned) limboItem.Destroy(DestroyMode.Vanish);
                }
            }

            // ---------------- 16. DELIVER giverSettlement: PASSING IGNORED, ARRIVAL ACTION DEDUCTED ----------------
            Settlement targetSettlement = FindAnyNonPlayerSettlement();
            StoryQuestDef carDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QDeliverCaravanSettlement);
            if (targetSettlement != null && targetSettlement.Faction != null && carDef != null)
            {
                carDef.faction = targetSettlement.Faction.def.defName;
                story.SetFlag("probe.e3.deliverCaravan", "true");
                story.DirectorTick();
                StoryQuestRun carRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverCaravanSettlement);

                int pinnedSettlementId = ParseInt(carRun, "settlementId", -1);
                Settlement exactSettlement = FindSettlementById(pinnedSettlementId);

                if (exactSettlement != null)
                {
                    Pawn carrier = SpawnCleanColonist(map);
                    Thing cargo = ThingMaker.MakeThing(ThingDefOf.Steel);
                    cargo.stackCount = 40;
                    carrier.inventory.innerContainer.TryAdd(cargo);
                    carrier.DeSpawn(DestroyMode.Vanish);

                    Caravan car = CaravanMaker.MakeCaravan(new List<Pawn> { carrier }, Faction.OfPlayer, exactSettlement.Tile, true); // CHECK API
                    story.DirectorTick();
                    story.DirectorTick();

                    int cargoAfterPassing = StoryPayment.CountInCaravanDirectInventory(car, ThingDefOf.Steel);
                    bool passingNotDeducted = cargoAfterPassing == 40 && carRun.status == StoryQuestStatus.Active &&
                                              GetVar(carRun, "delivered") != "true";

                    CaravanArrivalAction_StoryDeliver action = new CaravanArrivalAction_StoryDeliver(
                        exactSettlement, carRun.quest, carRun.stageKey, carRun.gen);
                    bool stillValid = action.StillValid(car, exactSettlement.Tile);
                    action.Arrived(car);
                    story.DirectorTick();

                    int cargoAfterArrival = StoryPayment.CountInCaravanDirectInventory(car, ThingDefOf.Steel);
                    bool arrivedDeducted = stillValid && cargoAfterArrival == 0 && HasOutcome(carRun, "success");

                    AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted",
                        passingNotDeducted && arrivedDeducted,
                        "passing=40(active), arrived=0(success)",
                        string.Format("passingOk={0}(cargo={1}), valid={2}, afterCargo={3}, hist={4}",
                            passingNotDeducted, cargoAfterPassing, stillValid, cargoAfterArrival,
                            string.Join(",", carRun.history.ToArray())));
                    DestroyTestCaravanAndPawns(car);
                }
                else
                {
                    AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted", false, "exact settlement pinned", "settlement null");
                }
            }
            else
            {
                AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted", false, "non-player settlement exists", "missing");
            }

            // ---------------- 17. PREPARE SAVE FOR RELOAD: ACTIVE VISIT + PAID DELIVER ----------------
            story.SetFlag("probe.e3.reloadVisit", "true");
            story.DirectorTick();
            StoryQuestRun reloadVisitRun = story.RunFor(StoryHotelProbeBootstrap.QReloadActiveVisit);
            bool visitSavedReady = reloadVisitRun != null && reloadVisitRun.status == StoryQuestStatus.Active &&
                                   GetVar(reloadVisitRun, "arrived") == "true" &&
                                   ParseInt(reloadVisitRun, "visitLordId", -1) >= 0;
            AddCheck("ReloadPrep_ActiveHotelVisitSaved", visitSavedReady, "active visit with lordId>=0",
                string.Format("arrived={0}, lordId={1}, heroIncluded={2}",
                    GetVar(reloadVisitRun, "arrived"), GetVar(reloadVisitRun, "visitLordId"), GetVar(reloadVisitRun, "heroIncluded")));

            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 100, 0);
            story.SetFlag("probe.e3.reloadPaid", "true");
            story.DirectorTick();
            StoryQuestRun reloadPaidRun = story.RunFor(StoryHotelProbeBootstrap.QReloadPaidDeliver);
            ChoiceLetter_StoryDeliver reloadPaidLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QReloadPaidDeliver);

            // Click Deliver (deducts 50 -> leaves 50, sets delivered=true) BEFORE DirectorTick and save!
            bool paidBeforeSave = reloadPaidLetter != null && ClickFirstEnabledOption(reloadPaidLetter);
            int steelLeftAtSave = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            bool paidSavedReady = paidBeforeSave && steelLeftAtSave == 50 && GetVar(reloadPaidRun, "delivered") == "true";
            AddCheck("ReloadPrep_PaidDeliverSavedBeforeTransition", paidSavedReady, "paid=true, steelLeft=50, delivered=true",
                string.Format("paid={0}, steel={1}, delivered={2}", paidBeforeSave, steelLeftAtSave, GetVar(reloadPaidRun, "delivered")));

            GameDataSaveLoader.SaveGame(StoryHotelProbeBootstrap.SaveName); // CHECK API
        }

        // =====================================================================
        // RELOAD PROBE
        // =====================================================================

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. ACTIVE HOTEL VISIT AFTER RELOAD -> PEACEFUL EXIT ONCE ----------------
            StoryQuestRun visitRun = story.RunFor(StoryHotelProbeBootstrap.QReloadActiveVisit);
            if (visitRun != null && visitRun.status == StoryQuestStatus.Active)
            {
                int savedMapId = ParseInt(visitRun, "visitMapId", -1);
                int savedLordId = ParseInt(visitRun, "visitLordId", -1);
                List<int> savedGuestIds = ParseIntList(GetVar(visitRun, "guestIds"));
                string heroThingId = GetVar(visitRun, "heroThingId");

                Lord restoredLord = FindLordById(map, savedLordId);
                bool mapMatches = savedMapId == map.uniqueID;
                bool lordRestored = restoredLord != null && restoredLord.ownedPawns != null &&
                                    restoredLord.ownedPawns.Count == savedGuestIds.Count;

                int heroMatchesOnMap = 0;
                if (!heroThingId.NullOrEmpty())
                {
                    IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
                    for (int i = 0; i < allPawns.Count; i++)
                    {
                        if (allPawns[i] != null && allPawns[i].ThingID == heroThingId) heroMatchesOnMap++;
                    }
                }
                bool heroNoDuplicate = heroThingId.NullOrEmpty() || heroMatchesOnMap == 1;

                // Complete the stay peacefully via real Pawn.ExitMap and verify single trigger
                ExitGuestsViaRealExitMap(map, savedGuestIds);
                story.DirectorTick();
                story.DirectorTick();
                bool completedOnce = CountOutcome(visitRun, "success") == 1;

                AddCheck("Reload_ActiveVisitLordHeroAndPeacefulExit",
                    mapMatches && lordRestored && heroNoDuplicate && completedOnce,
                    "mapMatches, lord & guest count match, heroCount<=1, successCount=1",
                    string.Format("map={0}, lord={1}({2}/{3}), heroCount={4}, succCount={5}",
                        mapMatches, restoredLord != null,
                        restoredLord != null && restoredLord.ownedPawns != null ? restoredLord.ownedPawns.Count : -1,
                        savedGuestIds.Count, heroMatchesOnMap, CountOutcome(visitRun, "success")));
            }
            else
            {
                AddCheck("Reload_ActiveVisitLordHeroAndPeacefulExit", false, "active visit restored", "missing");
            }

            // ---------------- 2. PAID DELIVER COMPLETES ONCE WITHOUT DOUBLE DEDUCTION ----------------
            StoryQuestRun paidRun = story.RunFor(StoryHotelProbeBootstrap.QReloadPaidDeliver);
            int steelBeforeTick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            story.DirectorTick();
            story.DirectorTick();
            int steelAfterTick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            bool completedOnceNoRededuct = paidRun != null && CountOutcome(paidRun, "success") == 1 &&
                                           steelBeforeTick == steelAfterTick && steelBeforeTick + SteelInHands(map) == 50;
            AddCheck("Reload_PaidDeliverNoDoubleDeduction", completedOnceNoRededuct,
                "successCount=1, steel=50->50",
                string.Format("count={0}, steel={1}->{2}, in hands={3}",
                    CountOutcome(paidRun, "success"), steelBeforeTick, steelAfterTick, SteelInHands(map)));
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static bool BuildTestHotel(Map targetMap)
        {
            if (targetMap == null) return false;

            ThingDef guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("BedGuest") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("DoubleBedGuest") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("SleepingSpotGuest") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("GuestBed") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("GuestDoubleBed") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("GuestSleepingSpot"); // CHECK API
            ThingDef receptionDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_HotelReception"); // CHECK API

            IntVec3 baseCell = FindStandable(targetMap);

            if (receptionDef != null)
            {
                Thing rec = ThingMaker.MakeThing(receptionDef, receptionDef.MadeFromStuff ? ThingDefOf.WoodLog : null);
                rec.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(rec, baseCell, targetMap, WipeMode.Vanish);
            }

            int spawnedBeds = 0;
            if (guestBedDef != null)
            {
                ThingDef stuff = guestBedDef.MadeFromStuff ? ThingDefOf.WoodLog : null;
                for (int i = 0; i < 3; i++)
                {
                    IntVec3 cell;
                    if (CellFinder.TryFindRandomCellNear(baseCell, targetMap, 8, delegate(IntVec3 c)
                    {
                        return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
                    }, out cell))
                    {
                        Thing bed = ThingMaker.MakeThing(guestBedDef, stuff);
                        bed.SetFaction(Faction.OfPlayer);
                        GenSpawn.Spawn(bed, cell, targetMap, WipeMode.Vanish);
                        spawnedBeds++;
                    }
                }
            }

            if (spawnedBeds == 0)
            {
                ThingDef standardBed = ThingDefOf.Bed ?? DefDatabase<ThingDef>.GetNamedSilentFail("SleepingSpot");
                if (standardBed != null)
                {
                    ThingDef stuff = standardBed.MadeFromStuff ? ThingDefOf.WoodLog : null;
                    for (int i = 0; i < 3; i++)
                    {
                        IntVec3 cell;
                        if (CellFinder.TryFindRandomCellNear(baseCell, targetMap, 8, delegate(IntVec3 c)
                        {
                            return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
                        }, out cell))
                        {
                            Building_Bed b = ThingMaker.MakeThing(standardBed, stuff) as Building_Bed;
                            if (b != null)
                            {
                                b.SetFaction(Faction.OfPlayer);
                                GenSpawn.Spawn(b, cell, targetMap, WipeMode.Vanish);
                                MethodInfo swapMethod = AccessTools.Method("Hospitality.Building_GuestBed:Swap", new Type[] { typeof(Building_Bed) });
                                if (swapMethod != null)
                                {
                                    swapMethod.Invoke(null, new object[] { b });
                                }
                                spawnedBeds++;
                            }
                        }
                    }
                }
            }
            return spawnedBeds > 0;
        }

        private static void SetGuestsWelcome(Map targetMap, bool welcome)
        {
            Type compType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
            if (compType == null || targetMap == null) return;
            MapComponent comp = HotelVisitGuarantee.HospitalityOf(targetMap);
            if (comp == null) return;
            FieldInfo field = AccessTools.Field(compType, "guestsAreWelcome");
            if (field != null) field.SetValue(comp, welcome);
        }

        private static Zone_Stockpile EnsureTestStockpile(Map targetMap)
        {
            if (targetMap == null || targetMap.zoneManager == null) return null;
            Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, targetMap.zoneManager); // CHECK API
            targetMap.zoneManager.RegisterZone(zone);
            IntVec3 origin = FindStandable(targetMap);
            for (int dx = -1; dx <= 2; dx++)
            {
                for (int dz = -1; dz <= 2; dz++)
                {
                    IntVec3 c = origin + new IntVec3(dx, 0, dz);
                    if (c.InBounds(targetMap) && c.Standable(targetMap) && targetMap.zoneManager.ZoneAt(c) == null)
                    {
                        zone.AddCell(c);
                    }
                }
            }
            return zone;
        }

        private static void ClearStockpileResources(Map targetMap, ThingDef def)
        {
            if (targetMap == null || def == null) return;
            List<Thing> snapshot = new List<Thing>(targetMap.listerThings.ThingsOfDef(def));
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing t = snapshot[i];
                if (t != null && !t.Destroyed && StoryPayment.IsInPlayerStorage(targetMap, t))
                {
                    t.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void PlaceInStockpile(Map targetMap, Zone_Stockpile zone, ThingDef def, int count, int cellIndex)
        {
            if (targetMap == null || zone == null || zone.cells.Count == 0 || def == null || count <= 0) return;
            int remaining = count;
            int limit = def.stackLimit > 0 ? def.stackLimit : 75;
            int idx = cellIndex;
            while (remaining > 0)
            {
                int toSpawn = Math.Min(remaining, limit);
                IntVec3 cell = zone.cells[idx % zone.cells.Count];
                idx++;
                Thing t = ThingMaker.MakeThing(def);
                t.stackCount = toSpawn;
                GenSpawn.Spawn(t, cell, targetMap, WipeMode.Vanish);
                t.SetForbidden(false, false);
                remaining -= toSpawn;
            }
        }

        private static ChoiceLetter_StoryDeliver FindDeliverLetter(string questDefName)
        {
            if (Find.LetterStack == null) return null;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                ChoiceLetter_StoryDeliver dl = letters[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == questDefName) return dl;
            }
            return null;
        }

        private static bool ClickFirstEnabledOption(ChoiceLetter_StoryDeliver letter)
        {
            if (letter == null) return false;
            List<DiaOption> opts = new List<DiaOption>(letter.Choices);
            for (int i = 0; i < opts.Count; i++)
            {
                if (opts[i] != null && opts[i].action != null && !opts[i].disabled)
                {
                    opts[i].action();
                    return true;
                }
            }
            return false;
        }

        private static void ExitGuestsViaRealExitMap(Map targetMap, List<int> guestIds)
        {
            if (targetMap == null || guestIds == null || guestIds.Count == 0) return;
            HashSet<int> set = new HashSet<int>(guestIds);
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && set.Contains(p.thingIDNumber) && p.Spawned)
                {
                    p.ExitMap(false, Rot4.South); // CHECK API
                }
            }
        }

        private void CleanupActiveHotelRun(SparkStory story, StoryQuestDef def, StoryQuestRun run)
        {
            if (run == null) return;
            List<int> ids = ParseIntList(GetVar(run, "guestIds"));
            if (def != null && def.stages != null && def.stages.Count > 0)
            {
                List<StoryStage> backup = new List<StoryStage>(def.stages);
                try
                {
                    def.stages.Clear();
                    story.DirectorTick();
                }
                finally
                {
                    def.stages.AddRange(backup);
                }
            }
            CleanupPawnsByIds(map, ids);
        }

        private static void CleanupPawnsByIds(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return;
            HashSet<int> set = new HashSet<int>(ids);
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && set.Contains(p.thingIDNumber))
                {
                    if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                    if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void KillPawnsByIds(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return;
            HashSet<int> set = new HashSet<int>(ids);
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && !p.Dead && set.Contains(p.thingIDNumber))
                {
                    p.Kill(null);
                }
            }
        }

        private static Pawn FindGuestOnMap(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return null;
            HashSet<int> set = new HashSet<int>(ids);
            IReadOnlyList<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && set.Contains(pawns[i].thingIDNumber)) return pawns[i];
            }
            return null;
        }

        private static void KillFirstGuest(Map targetMap, List<int> ids)
        {
            Pawn p = FindGuestOnMap(targetMap, ids);
            if (p != null && !p.Dead) p.Kill(null);
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

        private static void DestroyTestCaravanAndPawns(Caravan car)
        {
            if (car == null) return;
            List<Pawn> pawns = new List<Pawn>(car.PawnsListForReading);
            car.RemoveAllPawns();
            if (!car.Destroyed) car.Destroy();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null) continue;
                if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns != null && Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
                if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
            }
        }

        private static Faction FindPermanentEnemyHumanlikeFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && f.def != null && f.def.permanentEnemy && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return null;
        }

        private static Faction FindGoodwillHumanlikeFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HasGoodwill && f.def != null &&
                    !f.def.permanentEnemy && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return null;
        }

        private static Faction FindAnotherHostileFaction(Faction exclude)
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && f != exclude && !f.IsPlayer && !f.defeated && f.HostileTo(Faction.OfPlayer) && f.def != null && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return null;
        }

        private static Settlement FindAnyNonPlayerSettlement()
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].Destroyed && all[i].Faction != null && !all[i].Faction.IsPlayer)
                {
                    return all[i];
                }
            }
            return null;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0) return null;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].Destroyed && all[i].ID == id) return all[i];
            }
            return null;
        }

        private static Lord FindLordById(Map targetMap, int lordId)
        {
            if (targetMap == null || lordId < 0 || targetMap.lordManager == null) return null;
            List<Lord> lords = targetMap.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null && lords[i].loadID == lordId) return lords[i];
            }
            return null;
        }

        private static void ForceHostileWithGoodwill(Faction fac, int gw)
        {
            if (fac == null) return;
            int drop = -100 - fac.PlayerGoodwill;
            if (drop < 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null);
            int adj = gw - fac.PlayerGoodwill;
            if (adj != 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, adj, false, false, null, null);
        }

        private static void ForceNeutralWithGoodwill(Faction fac, int gw)
        {
            if (fac == null) return;
            if (fac.HostileTo(Faction.OfPlayer))
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, 0 - fac.PlayerGoodwill, false, false, null, null);
            }
            int adj = gw - fac.PlayerGoodwill;
            if (adj != 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, adj, false, false, null, null);
        }

        private static IntVec3 FindStandable(Map targetMap)
        {
            IntVec3 cell;
            if (CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 12, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
            }, out cell))
            {
                return cell;
            }
            return targetMap.Center;
        }

        private static bool HasOutcome(StoryQuestRun run, string outcome)
        {
            return CountOutcome(run, outcome) > 0;
        }

        /// Steel a colonist or a hauling animal is carrying: between the load and the check it may be on its way to the stockpile.
        private static int SteelInHands(Map map)
        {
            int n = 0;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                if (p.Faction == Faction.OfPlayer && p.carryTracker != null && p.carryTracker.CarriedThing != null && p.carryTracker.CarriedThing.def == ThingDefOf.Steel) n += p.carryTracker.CarriedThing.stackCount;
            return n;
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
                string uiPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "StoryHotelUiProbe.json");
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

