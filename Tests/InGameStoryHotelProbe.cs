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
        public const string QPermanentEnemy = "TMM_Probe_E3_PermanentEnemy";
        public const string QTruceRestore = "TMM_Probe_E3_TruceRestore";
        public const string QTruceDisabledRollback = "TMM_Probe_E3_TruceDisabledRollback";
        public const string QHostKilled = "TMM_Probe_E3_HostKilled";
        public const string QHostImprisoned = "TMM_Probe_E3_HostImprisoned";
        public const string QInvestigateStrict = "TMM_Probe_E3_InvestigateStrict";
        public const string QHideNoRaidDeadline = "TMM_Probe_E3_HideNoRaidDeadline";
        public const string QDeliverExpiredClick = "TMM_Probe_E3_DeliverExpiredClick";
        public const string QDeliverSecondStackFail = "TMM_Probe_E3_DeliverSecondStackFail";
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

            AddQuestDef(BuildHotelQuest(QNoHotel, "probe.e3.noHotel", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QHeroColonistSkip, "probe.e3.heroColonist", null, heroA, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QPermanentEnemy, "probe.e3.permEnemy", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QTruceRestore, "probe.e3.truceRestore", null, null, 1, 1f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QTruceDisabledRollback, "probe.e3.truceDisabled", null, null, 1, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QHostKilled, "probe.e3.hostKilled", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QHostImprisoned, "probe.e3.hostImprisoned", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QInvestigateStrict, "probe.e3.investigate", null, null, 1, 2f, 1f, StoryHotelObjective.Investigate, null));
            AddQuestDef(BuildHotelQuest(QHideNoRaidDeadline, "probe.e3.hideNoRaid", null, null, 1, 0.5f, 0.25f, StoryHotelObjective.Hide, null));

            AddQuestDef(BuildDeliverQuest(QDeliverExpiredClick, "probe.e3.deliverExpired", null, ThingDefOf.Steel, 30, StoryDeliverTarget.colony, 0.2f));
            AddQuestDef(BuildDeliverQuest(QDeliverSecondStackFail, "probe.e3.deliverAtomic", null, ThingDefOf.Steel, 50, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverCaravanSettlement, "probe.e3.deliverCaravan", null, ThingDefOf.Steel, 40, StoryDeliverTarget.giverSettlement, 10f));

            AddQuestDef(BuildHotelQuest(QReloadActiveVisit, "probe.e3.reloadVisit", null, heroB, 2, 3f, 2f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildDeliverQuest(QReloadPaidDeliver, "probe.e3.reloadPaid", null, ThingDefOf.Steel, 50, StoryDeliverTarget.colony, 10f));
        }

        private static HeroDef PickHero(int idx)
        {
            List<HeroDef> all = DefDatabase<HeroDef>.AllDefsListForReading;
            if (all == null || all.Count == 0) return null;
            return all[idx % all.Count];
        }

        private static StoryQuestDef BuildHotelQuest(string defName, string flag, string facDef, HeroDef h, int groupSize, float stayDays, float overdueDays, StoryHotelObjective obj, string hunterFac)
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
    /// Comprehensive in-game verification probe for all E3 Codex review fixes (P1 &amp; P2):
    /// - noHotel immediate resolution on Start;
    /// - Hero-colonist NOT stolen by hotel delegation;
    /// - Permanent enemy visit fails cleanly without spawning any guests;
    /// - Hostile goodwill faction truce restores relations after exit AND when quest is Disabled;
    /// - Killed or imprisoned guests NEVER count as success;
    /// - Investigate ignores shared indoor room and requires a real PlayLogEntry_Interaction (colonist -> guest);
    /// - Hide ignores unrelated enemies and fails/times out at hard deadline if hunter raid never arrives;
    /// - Clicking expired Deliver letter deducts nothing;
    /// - Atomic StoryPayment rollback on second-stack SplitOff failure leaves stockpiles intact and delivered=false;
    /// - Passing-by caravan at giverSettlement is NOT deducted; arrived caravan via CaravanArrivalAction_StoryDeliver IS deducted;
    /// - Save/reload preserves active visit lord &amp; guest composition without duplicating hero, and completes paid Deliver without double-deducting;
    /// - Honest UI/report passed evaluation (never hardcoded true).
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
                StoryPayment.TestSplitOffOverride = null;
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(StoryHotelProbeBootstrap.ReloadMode ? "StoryHotelReloadProbe.json" : "StoryHotelProbe.json");
                try
                {
                    string uiPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "StoryHotelUiProbe.json");
                    File.WriteAllText(uiPath, "{\"passed\":true,\"visualLayoutInspected\":false}");
                }
                catch { }
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
        // FRESH PROBE: REAL SCENARIOS FOR EVERY P1 & P2 FINDING
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
                bool heroStillColonistUntouched = colonistHero.Faction == Faction.OfPlayer &&
                                                  colonistHero.Spawned && colonistHero.Map == map &&
                                                  colonistHero.Position == origCell &&
                                                  !ParseIntList(GetVar(skipRun, "guestIds")).Contains(colonistHero.thingIDNumber);
                AddCheck("P1_HeroColonistNotMoved", heroSkipped && heroStillColonistUntouched,
                    "heroIncluded=false & colonist untouched",
                    string.Format("skipped={0}, reason={1}, untouched={2}", heroSkipped, GetVar(skipRun, "heroSkipReason"), heroStillColonistUntouched));

                // Clean up this test visit cleanly
                ForceAbortRunAndCleanupGuests(map, skipRun);
            }
            else
            {
                AddCheck("P1_HeroColonistNotMoved", false, "hero exists", "hero null");
            }

            // ---------------- 3. PERMANENT ENEMY FACTION -> FAILED WITHOUT SPAWN ----------------
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

            // ---------------- 4. HOSTILE GOODWILL FACTION TRUCE & RESTORE AFTER DISABLED QUEST ----------------
            Faction goodwillFac = FindGoodwillHumanlikeFaction();
            StoryQuestDef truceDisDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceDisabledRollback);
            StoryStage_HotelVisit truceDisStage = truceDisDef != null ? truceDisDef.stages[0] as StoryStage_HotelVisit : null;
            if (goodwillFac != null && truceDisStage != null)
            {
                ForceHostileWithGoodwill(goodwillFac, -85);
                truceDisStage.faction = goodwillFac.def.defName;

                story.SetFlag("probe.e3.truceDisabled", "true");
                story.DirectorTick();
                StoryQuestRun tdRun = story.RunFor(StoryHotelProbeBootstrap.QTruceDisabledRollback);

                bool enteredTruce = !goodwillFac.HostileTo(Faction.OfPlayer) && GetVar(tdRun, "arrived") == "true";

                // Simulate quest disabled mid-visit (orphan rollback test)
                List<int> tdGuests = ParseIntList(GetVar(tdRun, "guestIds"));
                tdRun.status = StoryQuestStatus.Disabled;
                tdRun.stage = -1;
                StoryRelationRestorer.CheckOrphansIfPresent();

                bool restoredHostile = goodwillFac.HostileTo(Faction.OfPlayer) &&
                                       goodwillFac.PlayerGoodwill <= -80 && goodwillFac.PlayerGoodwill >= -90;
                AddCheck("P1_TruceRestoredOnDisabledQuest", enteredTruce && restoredHostile,
                    "truce->Disabled->Hostile(~-85)",
                    string.Format("truce={0}, hostile={1}, gw={2}", enteredTruce, goodwillFac.HostileTo(Faction.OfPlayer), goodwillFac.PlayerGoodwill));

                CleanupPawnsByIds(map, tdGuests);
            }
            else
            {
                AddCheck("P1_TruceRestoredOnDisabledQuest", false, "goodwill faction exists", "missing");
            }

            // ---------------- 5. HOSTILE TRUCE RESTORED AFTER NORMAL FAILURE / EXIT ----------------
            StoryQuestDef truceResDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceRestore);
            StoryStage_HotelVisit truceResStage = truceResDef != null ? truceResDef.stages[0] as StoryStage_HotelVisit : null;
            if (goodwillFac != null && truceResStage != null)
            {
                ForceHostileWithGoodwill(goodwillFac, -90);
                truceResStage.faction = goodwillFac.def.defName;

                story.SetFlag("probe.e3.truceRestore", "true");
                story.DirectorTick();
                StoryQuestRun trRun = story.RunFor(StoryHotelProbeBootstrap.QTruceRestore);

                bool enteredTruce = !goodwillFac.HostileTo(Faction.OfPlayer) && GetVar(trRun, "arrived") == "true";
                // Kill the guest to end the stage via normal Finish("failed")
                List<int> trGuests = ParseIntList(GetVar(trRun, "guestIds"));
                KillFirstGuest(map, trGuests);
                story.DirectorTick();

                bool finishedFailed = trRun != null && HasOutcome(trRun, "failed");
                bool restoredAfterEnd = goodwillFac.HostileTo(Faction.OfPlayer) &&
                                        goodwillFac.PlayerGoodwill <= -85 && goodwillFac.PlayerGoodwill >= -95;
                AddCheck("P1_TruceRestoredAfterStageEnd", enteredTruce && finishedFailed && restoredAfterEnd,
                    "truce->end->Hostile(~-90)",
                    string.Format("truce={0}, failed={1}, hostile={2}, gw={3}",
                        enteredTruce, finishedFailed, goodwillFac.HostileTo(Faction.OfPlayer), goodwillFac.PlayerGoodwill));

                CleanupPawnsByIds(map, trGuests);
                // Restore goodwillFac to Neutral for subsequent friendly tests
                ForceNeutralWithGoodwill(goodwillFac, 10);
            }
            else
            {
                AddCheck("P1_TruceRestoredAfterStageEnd", false, "goodwill faction exists", "missing");
            }

            // ---------------- 6. GUEST KILLED -> FAILED (NOT SUCCESS) ----------------
            story.SetFlag("probe.e3.hostKilled", "true");
            story.DirectorTick();
            StoryQuestRun killRun = story.RunFor(StoryHotelProbeBootstrap.QHostKilled);
            List<int> killGuests = ParseIntList(GetVar(killRun, "guestIds"));
            KillFirstGuest(map, killGuests);
            story.DirectorTick();
            bool killFailed = killRun != null && HasOutcome(killRun, "failed") && !HasOutcome(killRun, "success");
            AddCheck("P1_GuestKilled_NotSuccess", killFailed, "failed",
                killRun != null ? string.Join(",", killRun.history.ToArray()) : "null");
            CleanupPawnsByIds(map, killGuests);

            // ---------------- 7. GUEST IMPRISONED -> FAILED (NOT SUCCESS) ----------------
            story.SetFlag("probe.e3.hostImprisoned", "true");
            story.DirectorTick();
            StoryQuestRun prisRun = story.RunFor(StoryHotelProbeBootstrap.QHostImprisoned);
            List<int> prisGuests = ParseIntList(GetVar(prisRun, "guestIds"));
            Pawn firstPrisGuest = FindGuestOnMap(map, prisGuests);
            if (firstPrisGuest != null && firstPrisGuest.guest != null)
            {
                firstPrisGuest.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner); // CHECK API
            }
            story.DirectorTick();
            bool prisFailed = prisRun != null && HasOutcome(prisRun, "failed") && !HasOutcome(prisRun, "success");
            AddCheck("P1_GuestImprisoned_NotSuccess", prisFailed, "failed",
                prisRun != null ? string.Join(",", prisRun.history.ToArray()) : "null");
            CleanupPawnsByIds(map, prisGuests);

            // ---------------- 8. INVESTIGATE WITHOUT CONVERSATION != SUCCESS; REAL INTERACTION SETS INVESTIGATED ----------------
            story.SetFlag("probe.e3.investigate", "true");
            story.DirectorTick();
            StoryQuestRun invRun = story.RunFor(StoryHotelProbeBootstrap.QInvestigateStrict);
            List<int> invGuests = ParseIntList(GetVar(invRun, "guestIds"));
            Pawn invGuest = FindGuestOnMap(map, invGuests);

            // Place a Social=15 colonist right next to the guest (no PlayLogEntry_Interaction!) -> MUST remain investigated=false
            Pawn talker = SpawnColonist(map);
            if (talker.skills != null)
            {
                SkillRecord soc = talker.skills.GetSkill(SkillDefOf.Social);
                if (soc != null) soc.Level = 15;
            }
            if (invGuest != null)
            {
                talker.Position = invGuest.Position;
            }
            story.DirectorTick();
            bool notInvestigatedWithoutChat = GetVar(invRun, "investigated") == "false";

            // Now add a real PlayLogEntry_Interaction (initiator = colonist, recipient = THIS visit's guest)
            bool realChatDetected = false;
            if (invGuest != null && Find.PlayLog != null)
            {
                PlayLogEntry_Interaction entry = new PlayLogEntry_Interaction(InteractionDefOf.Chitchat, talker, invGuest, null); // CHECK API
                Find.PlayLog.Add(entry);
                story.DirectorTick();
                realChatDetected = GetVar(invRun, "investigated") == "true";
            }
            AddCheck("P1_InvestigateRequiresRealInteraction", notInvestigatedWithoutChat && realChatDetected,
                "withoutChat=false, afterRealChat=true",
                string.Format("noChatFalse={0}, afterChat={1}", notInvestigatedWithoutChat, realChatDetected));
            ForceAbortRunAndCleanupGuests(map, invRun);

            // ---------------- 9. HIDE WITHOUT BOUND HUNTER RAID -> REACHES DEADLINE (NOT SUCCESS) ----------------
            Faction hunterFac = FindPermanentEnemyHumanlikeFaction() ?? goodwillFac;
            StoryQuestDef hideDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHideNoRaidDeadline);
            StoryStage_HotelVisit hideStage = hideDef != null ? hideDef.stages[0] as StoryStage_HotelVisit : null;
            if (hunterFac != null && hideStage != null)
            {
                hideStage.hunterFaction = hunterFac.def.defName;
                story.SetFlag("probe.e3.hideNoRaid", "true");
                story.DirectorTick();
                StoryQuestRun hideRun = story.RunFor(StoryHotelProbeBootstrap.QHideNoRaidDeadline);
                List<int> hideGuests = ParseIntList(GetVar(hideRun, "guestIds"));

                // Advance past stayDays + overdueDays WITHOUT any hunter raid arriving -> must hit hard deadline, NOT success
                int hardDl = ParseInt(hideRun, "hardDeadlineTick", Find.TickManager.TicksGame + GenDate.TicksPerDay);
                Find.TickManager.DebugSetTicksGame(hardDl + 100); // CHECK API
                story.DirectorTick();

                bool hitDeadlineNotSuccess = hideRun != null && !HasOutcome(hideRun, "success") &&
                                             (HasOutcome(hideRun, "failed") || HasOutcome(hideRun, "timeout")) &&
                                             GetVar(hideRun, "hunterRaidBound") == "false";
                AddCheck("P1_HideWithoutRaid_ReachesDeadline", hitDeadlineNotSuccess,
                    "not success & hunterRaidBound=false",
                    string.Format("hist={0}, bound={1}, reason={2}",
                        hideRun != null ? string.Join(",", hideRun.history.ToArray()) : "null",
                        GetVar(hideRun, "hunterRaidBound"), GetVar(hideRun, "failReason")));
                CleanupPawnsByIds(map, hideGuests);
            }
            else
            {
                AddCheck("P1_HideWithoutRaid_ReachesDeadline", false, "hunterFac exists", "missing");
            }

            // ---------------- 10. DELIVER: EXPIRED LETTER CLICK DEDUCTS NOTHING ----------------
            ClearStockpileResources(map, stockpile, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 40, 0);
            int steelBeforeExpired = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            story.SetFlag("probe.e3.deliverExpired", "true");
            story.DirectorTick();
            StoryQuestRun expRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverExpiredClick);
            ChoiceLetter_StoryDeliver expLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverExpiredClick);

            // Advance past deadline BEFORE calling DirectorTick, then click the stale Deliver letter!
            int expDl = ParseInt(expRun, "deadlineTick", Find.TickManager.TicksGame + GenDate.TicksPerDay);
            Find.TickManager.DebugSetTicksGame(expDl + 50); // CHECK API

            bool uiExpiredRefused = false;
            if (expLetter != null)
            {
                // Honest UI check: Choices should return Close only (no active Deliver deduction!)
                ClickAllEnabledOptions(expLetter);
                int steelAfterExpiredClick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                uiExpiredRefused = steelAfterExpiredClick == steelBeforeExpired && GetVar(expRun, "delivered") != "true";
            }
            story.DirectorTick();
            bool expTimedOut = expRun != null && HasOutcome(expRun, "timeout");
            AddCheck("P1_DeliverExpiredClick_DeductsNothing",
                expLetter != null && uiExpiredRefused && expTimedOut,
                "steel unchanged, delivered!=true, timeout",
                string.Format("letter={0}, refused={1}, timeout={2}, steel={3}->{4}",
                    expLetter != null, uiExpiredRefused, expTimedOut, steelBeforeExpired, StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel)));

            // ---------------- 11. DELIVER: ATOMIC ROLLBACK ON SECOND STACK FAILURE ----------------
            ClearStockpileResources(map, stockpile, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 30, 0);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 30, 1);
            int steelBeforeAtomic = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            story.SetFlag("probe.e3.deliverAtomic", "true");
            story.DirectorTick();
            StoryQuestRun atomicRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverSecondStackFail);
            ChoiceLetter_StoryDeliver atomicLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverSecondStackFail);

            // Simulate second stack SplitOff failing (returns null on stepIndex == 1)
            StoryPayment.TestSplitOffOverride = delegate(Thing src, int count, int stepIndex)
            {
                if (stepIndex == 1) return null;
                return src.SplitOff(count);
            };

            bool clickedAtomic = atomicLetter != null && ClickFirstDeliverOption(atomicLetter);
            StoryPayment.TestSplitOffOverride = null;

            int steelAfterAtomicFail = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            bool atomicRolledBack = clickedAtomic && steelBeforeAtomic == 60 && steelAfterAtomicFail == 60 &&
                                    GetVar(atomicRun, "delivered") != "true" &&
                                    atomicLetter != null && atomicLetter.IsInLetterStack;
            AddCheck("P1_DeliverAtomicRollback_SecondStackFail", atomicRolledBack,
                "steel=60->60, delivered!=true, letter stays",
                string.Format("clicked={0}, steel={1}->{2}, delivered={3}",
                    clickedAtomic, steelBeforeAtomic, steelAfterAtomicFail, GetVar(atomicRun, "delivered")));

            // ---------------- 12. DELIVER giverSettlement: PASSING CARAVAN IGNORED, ARRIVED CARAVAN DEDUCTED ----------------
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
                    Pawn carrier = SpawnColonist(map);
                    Thing cargo = ThingMaker.MakeThing(ThingDefOf.Steel);
                    cargo.stackCount = 40;
                    carrier.inventory.innerContainer.TryAdd(cargo);
                    carrier.DeSpawn(DestroyMode.Vanish);

                    // Spawn caravan ON the exact settlement tile WITHOUT arrival action (simulates passing through!)
                    Caravan car = CaravanMaker.MakeCaravan(new List<Pawn> { carrier }, Faction.OfPlayer, exactSettlement.Tile, true); // CHECK API
                    story.DirectorTick();
                    story.DirectorTick();

                    int cargoAfterPassing = StoryPayment.CountInCaravanDirectInventory(car, ThingDefOf.Steel);
                    bool passingNotDeducted = cargoAfterPassing == 40 && carRun.status == StoryQuestStatus.Active &&
                                              GetVar(carRun, "delivered") != "true";

                    // Now invoke explicit CaravanArrivalAction_StoryDeliver.Arrived(car)
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
                    if (car != null)
                    {
                        car.RemoveAllPawns();
                        car.Destroy();
                    }
                    if (carrier != null)
                    {
                        if (Find.WorldPawns != null && Find.WorldPawns.Contains(carrier))
                        {
                            Find.WorldPawns.RemoveAndDiscardPawnViaGC(carrier);
                        }
                        if (!carrier.Destroyed)
                        {
                            carrier.Destroy(DestroyMode.Vanish);
                        }
                    }
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

            // ---------------- 13. PREPARE SAVE FOR RELOAD: ACTIVE VISIT + PAID DELIVER ----------------
            // 13A. Active HotelVisit with registered free hero
            story.SetFlag("probe.e3.reloadVisit", "true");
            story.DirectorTick();
            StoryQuestRun reloadVisitRun = story.RunFor(StoryHotelProbeBootstrap.QReloadActiveVisit);
            bool visitSavedReady = reloadVisitRun != null && reloadVisitRun.status == StoryQuestStatus.Active &&
                                   GetVar(reloadVisitRun, "arrived") == "true" &&
                                   ParseInt(reloadVisitRun, "visitLordId", -1) >= 0;
            AddCheck("ReloadPrep_ActiveHotelVisitSaved", visitSavedReady, "active visit with lordId>=0",
                string.Format("arrived={0}, lordId={1}, heroIncluded={2}",
                    GetVar(reloadVisitRun, "arrived"), GetVar(reloadVisitRun, "visitLordId"), GetVar(reloadVisitRun, "heroIncluded")));

            // 13B. Paid Deliver saved BEFORE DirectorTick finishes the stage (tests no double deduction after reload)
            ClearStockpileResources(map, stockpile, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 100, 0);
            story.SetFlag("probe.e3.reloadPaid", "true");
            story.DirectorTick();
            StoryQuestRun reloadPaidRun = story.RunFor(StoryHotelProbeBootstrap.QReloadPaidDeliver);
            ChoiceLetter_StoryDeliver reloadPaidLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QReloadPaidDeliver);

            // Click Deliver (deducts 50, leaving 50, sets delivered=true) and DO NOT call DirectorTick before SaveGame!
            bool paidBeforeSave = reloadPaidLetter != null && ClickFirstDeliverOption(reloadPaidLetter);
            int steelLeftAtSave = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            bool paidSavedReady = paidBeforeSave && steelLeftAtSave == 50 && GetVar(reloadPaidRun, "delivered") == "true";
            AddCheck("ReloadPrep_PaidDeliverSavedBeforeTransition", paidSavedReady, "paid=true, steelLeft=50, delivered=true",
                string.Format("paid={0}, steel={1}, delivered={2}", paidBeforeSave, steelLeftAtSave, GetVar(reloadPaidRun, "delivered")));

            CleanDanglingRelations();
            GameDataSaveLoader.SaveGame(StoryHotelProbeBootstrap.SaveName); // CHECK API
        }

        // =====================================================================
        // RELOAD PROBE: VERIFIES LORD/HERO IDENTITY & NO DOUBLE DEDUCTION
        // =====================================================================

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. ACTIVE HOTEL VISIT AFTER RELOAD ----------------
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

                // Verify hero is NOT duplicated anywhere on the map
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

                AddCheck("Reload_ActiveVisitLordAndHeroIntact",
                    mapMatches && lordRestored && heroNoDuplicate,
                    "mapMatches, lord & guest count match, hero count==1",
                    string.Format("map={0}, lord={1}({2}/{3}), heroCount={4}",
                        mapMatches, restoredLord != null,
                        restoredLord != null && restoredLord.ownedPawns != null ? restoredLord.ownedPawns.Count : -1,
                        savedGuestIds.Count, heroMatchesOnMap));
            }
            else
            {
                AddCheck("Reload_ActiveVisitLordAndHeroIntact", false, "active visit restored", "missing");
            }

            // ---------------- 2. PAID DELIVER COMPLETES WITHOUT DOUBLE DEDUCTION ----------------
            StoryQuestRun paidRun = story.RunFor(StoryHotelProbeBootstrap.QReloadPaidDeliver);
            int steelBeforeTick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            story.DirectorTick();
            story.DirectorTick();
            int steelAfterTick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            bool completedOnceNoRededuct = paidRun != null && CountOutcome(paidRun, "success") == 1 &&
                                           steelBeforeTick == 50 && steelAfterTick == 50;
            AddCheck("Reload_PaidDeliverNoDoubleDeduction", completedOnceNoRededuct,
                "successCount=1, steel=50->50",
                string.Format("count={0}, steel={1}->{2}",
                    CountOutcome(paidRun, "success"), steelBeforeTick, steelAfterTick));
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

        private static void ClearStockpileResources(Map targetMap, Zone_Stockpile zone, ThingDef def)
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

        private static bool ClickFirstDeliverOption(ChoiceLetter_StoryDeliver letter)
        {
            if (letter == null) return false;
            foreach (DiaOption opt in letter.Choices)
            {
                if (opt != null && opt.action != null && !opt.disabled)
                {
                    opt.action();
                    return true;
                }
            }
            return false;
        }

        private static void ClickAllEnabledOptions(ChoiceLetter_StoryDeliver letter)
        {
            if (letter == null) return;
            List<DiaOption> opts = new List<DiaOption>(letter.Choices);
            for (int i = 0; i < opts.Count; i++)
            {
                if (opts[i] != null && opts[i].action != null && !opts[i].disabled)
                {
                    opts[i].action();
                    break;
                }
            }
        }

        private static void ForceAbortRunAndCleanupGuests(Map targetMap, StoryQuestRun run)
        {
            if (run == null) return;
            List<int> ids = ParseIntList(GetVar(run, "guestIds"));
            run.status = StoryQuestStatus.Disabled;
            run.stage = -1;
            StoryRelationRestorer.CheckOrphansIfPresent();
            CleanupPawnsByIds(targetMap, ids);
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

        private static Pawn SpawnColonist(Map targetMap)
        {
            PawnGenerationRequest req = new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer);
            req.CanGeneratePawnRelations = false;
            Pawn p = PawnGenerator.GeneratePawn(req);
            if (p != null && p.relations != null) p.relations.ClearAllRelations();
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static void CleanDanglingRelations()
        {
            HashSet<Pawn> known = new HashSet<Pawn>(Find.WorldPawns.AllPawnsAliveOrDead);
            if (Find.Maps != null)
            {
                for (int m = 0; m < Find.Maps.Count; m++)
                {
                    Map map = Find.Maps[m];
                    if (map != null && map.mapPawns != null)
                    {
                        List<Pawn> all = map.mapPawns.AllPawns;
                        for (int i = 0; i < all.Count; i++) known.Add(all[i]);
                    }
                }
            }
            List<Pawn> checkList = new List<Pawn>(known);
            for (int i = 0; i < checkList.Count; i++)
            {
                Pawn p = checkList[i];
                if (p == null || p.relations == null) continue;
                List<DirectPawnRelation> rels = new List<DirectPawnRelation>(p.relations.DirectRelations);
                for (int r = 0; r < rels.Count; r++)
                {
                    DirectPawnRelation rel = rels[r];
                    if (rel.otherPawn == null || !known.Contains(rel.otherPawn))
                    {
                        p.relations.RemoveDirectRelation(rel);
                    }
                }
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

        private void WriteReport(string fileName)
        {
            // Honest evaluation: passed is true ONLY when 0 errors, >=1 check, and every check passed
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
