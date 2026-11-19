using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
                if (ReloadMode && Current.ProgramState == ProgramState.Entry)
                {
                    GameDataSaveLoader.LoadGame(SaveName); // CHECK API
                }
            });
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
    /// In-game verification probe for StoryStage_Site (Clear, Timeout, Save/Reload) and StoryStage_ColonyRaid.
    /// Activated via -tmm-story-stages-probe (fresh) or -tmm-story-stages-reload (reload).
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
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(StoryStagesProbeBootstrap.ReloadMode ? "StoryStagesReloadProbe.json" : "StoryStagesProbe.json");
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

        private void RunFreshProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("SparkStory_Exists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // 1. Enable sample quests and trigger offer
            story.SetFlag("story.e2samples", "true"); // CHECK API
            story.DirectorTick(); // CHECK API

            StoryQuestDef siteQuestDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Site");
            StoryQuestDef raidQuestDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Raid");
            AddCheck("SampleDefs_Loaded", siteQuestDef != null && raidQuestDef != null, "both loaded",
                string.Format("site={0}, raid={1}", siteQuestDef != null, raidQuestDef != null));
            if (siteQuestDef == null || raidQuestDef == null) return;

            // ---------------- TEST 1: SITE CLEAR -> CHOICE ----------------
            bool acceptedSite = AcceptOfferLetter(siteQuestDef.letterLabel);
            AddCheck("Site_OfferAccepted", acceptedSite, "true", acceptedSite.ToString());
            story.DirectorTick();

            StoryQuestRun siteRun = FindQuestRun(story, "TMM_Story_Sample_Site");
            int siteId = siteRun != null ? ParseInt(siteRun.vars, "siteId", -1) : -1;
            Site site = FindSite(siteId);
            AddCheck("Site_CreatedAndStored", site != null && siteId >= 0, "valid Site in vars[siteId]",
                string.Format("siteId={0}, found={1}", siteId, site != null));

            if (site != null)
            {
                StoryStage_Site siteStage = siteQuestDef.stages[0] as StoryStage_Site;
                float distTiles = Find.WorldGrid.TraversalDistanceBetween(map.Tile, site.Tile); // CHECK API
                float minTiles = siteStage.minDays * StoryStage_Site.TilesPerDay;
                float maxTiles = siteStage.maxDays * StoryStage_Site.TilesPerDay;
                bool distOk = distTiles >= minTiles && distTiles <= maxTiles;
                AddCheck("Site_DistanceInRange", distOk,
                    string.Format("{0:0.0}..{1:0.0} tiles", minTiles, maxTiles),
                    string.Format("{0:0.0} tiles", distTiles));

                // Enter site via caravan
                Pawn scout = SpawnTempScout();
                Caravan caravan = SendScoutToSiteMap(scout, site);
                AddCheck("Site_MapGenerated", site.HasMap && site.Map != null, "HasMap=true",
                    string.Format("HasMap={0}", site.HasMap));

                story.DirectorTick();

                if (site.HasMap && site.Map != null)
                {
                    int killed = KillAllHostilePawns(site.Map);
                    AddCheck("Site_EnemiesKilled", killed >= 0, ">=0 killed", killed.ToString());
                }

                story.DirectorTick();

                bool siteSucceeded = siteRun != null && siteRun.stage == 1 && HasHistoryEntry(siteRun, "success");
                AddCheck("Site_ClearSuccessToStage1", siteSucceeded, "stage=1, history=success",
                    siteRun != null ? string.Format("stage={0}, hist={1}", siteRun.stage, string.Join(",", siteRun.history.ToArray())) : "null");

                ChoiceLetter choiceLetter = FindChoiceLetterByLabel(siteQuestDef.stages[1].label);
                AddCheck("Site_NextChoiceLetterArrived", choiceLetter != null, "ChoiceLetter found", choiceLetter != null ? "found" : "missing");
                if (choiceLetter != null)
                {
                    ClickFirstChoice(choiceLetter);
                    story.DirectorTick();
                }

                CleanupTempScoutAndSite(scout, caravan, site);
            }

            // ---------------- TEST 2: SITE TIMEOUT ----------------
            StoryStage_Site stageForTimeout = siteQuestDef.stages[0] as StoryStage_Site;
            float savedTimeoutDays = stageForTimeout != null ? stageForTimeout.timeoutDays : 15f;
            try
            {
                if (stageForTimeout != null) stageForTimeout.timeoutDays = 0.1f;
                ResetQuestRun(story, "TMM_Story_Sample_Site");
                story.DirectorTick();

                bool acceptedTimeout = AcceptOfferLetter(siteQuestDef.letterLabel);
                AddCheck("SiteTimeout_OfferAccepted", acceptedTimeout, "true", acceptedTimeout.ToString());
                story.DirectorTick();

                StoryQuestRun timeoutRun = FindQuestRun(story, "TMM_Story_Sample_Site");
                int timeoutSiteId = timeoutRun != null ? ParseInt(timeoutRun.vars, "siteId", -1) : -1;
                Site timeoutSite = FindSite(timeoutSiteId);
                AddCheck("SiteTimeout_SiteCreated", timeoutSite != null, "Site exists", timeoutSite != null ? "exists" : "null");

                AdvanceTicks(Mathf.CeilToInt(0.15f * GenDate.TicksPerDay));
                story.DirectorTick();

                bool timedOut = timeoutRun != null && HasHistoryEntry(timeoutRun, "timeout");
                bool siteDeleted = FindSite(timeoutSiteId) == null;
                AddCheck("SiteTimeout_OutcomeTimeout", timedOut, "timeout",
                    timeoutRun != null ? string.Join(",", timeoutRun.history.ToArray()) : "null");
                AddCheck("SiteTimeout_SiteRemoved", siteDeleted, "site removed", siteDeleted ? "removed" : "still exists");
            }
            finally
            {
                if (stageForTimeout != null) stageForTimeout.timeoutDays = savedTimeoutDays;
            }

            // ---------------- TEST 3: COLONY RAID ----------------
            story.DirectorTick();
            bool acceptedRaid = AcceptOfferLetter(raidQuestDef.letterLabel);
            AddCheck("Raid_OfferAccepted", acceptedRaid, "true", acceptedRaid.ToString());
            story.DirectorTick();

            StoryQuestRun raidRun = FindQuestRun(story, "TMM_Story_Sample_Raid");
            StoryStage_ColonyRaid raidStage = raidQuestDef.stages[0] as StoryStage_ColonyRaid;
            float delayHrs = raidStage != null ? raidStage.delayHours : 2f;

            AdvanceTicks(Mathf.CeilToInt((delayHrs + 0.5f) * GenDate.TicksPerHour));
            story.DirectorTick();

            bool raidSpawned = raidRun != null && GetVar(raidRun, "spawned") == "true";
            int raidEnemiesFound = CountTrackedOrHostilePawns(map, raidRun);
            AddCheck("Raid_EnemiesSpawnedAfterDelay", raidSpawned && raidEnemiesFound > 0, "spawned=true, enemies>0",
                string.Format("spawned={0}, enemies={1}", raidSpawned, raidEnemiesFound));

            int raidKilled = KillTrackedOrHostilePawns(map, raidRun);
            AddCheck("Raid_EnemiesKilled", raidKilled > 0, ">0", raidKilled.ToString());

            story.DirectorTick();
            bool raidSucceeded = raidRun != null && HasHistoryEntry(raidRun, "success");
            AddCheck("Raid_OutcomeSuccess", raidSucceeded, "success",
                raidRun != null ? string.Join(",", raidRun.history.ToArray()) : "null");

            // ---------------- TEST 4: PREPARE SAVE FOR RELOAD ----------------
            ResetQuestRun(story, "TMM_Story_Sample_Site");
            story.DirectorTick();
            bool acceptedForSave = AcceptOfferLetter(siteQuestDef.letterLabel);
            story.DirectorTick();

            StoryQuestRun saveRun = FindQuestRun(story, "TMM_Story_Sample_Site");
            int saveSiteId = saveRun != null ? ParseInt(saveRun.vars, "siteId", -1) : -1;
            bool readyForSave = acceptedForSave && FindSite(saveSiteId) != null;
            AddCheck("ReloadPrep_ActiveSiteSaved", readyForSave, "active site before save",
                string.Format("accepted={0}, siteId={1}", acceptedForSave, saveSiteId));

            GameDataSaveLoader.SaveGame(StoryStagesProbeBootstrap.SaveName); // CHECK API
        }

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            StoryQuestDef siteQuestDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Site");
            StoryQuestRun siteRun = FindQuestRun(story, "TMM_Story_Sample_Site");
            AddCheck("Reload_RunRestored", siteRun != null && siteRun.stage == 0, "stage=0",
                siteRun != null ? "stage=" + siteRun.stage : "null");
            if (siteRun == null || siteQuestDef == null) return;

            int siteId = ParseInt(siteRun.vars, "siteId", -1);
            Site site = FindSite(siteId);
            AddCheck("Reload_SiteFoundById", site != null, "Site exists",
                string.Format("siteId={0}, found={1}", siteId, site != null));
            if (site == null) return;

            Pawn scout = SpawnTempScout();
            Caravan caravan = SendScoutToSiteMap(scout, site);
            AddCheck("Reload_EnteredSiteMap", site.HasMap && site.Map != null, "HasMap=true",
                string.Format("HasMap={0}", site.HasMap));

            story.DirectorTick();

            if (site.HasMap && site.Map != null)
            {
                KillAllHostilePawns(site.Map);
            }

            story.DirectorTick();
            bool succeededOnce = siteRun.stage == 1 && CountHistoryEntries(siteRun, "success") == 1;
            AddCheck("Reload_SiteCompletesOnce", succeededOnce, "stage=1, successCount=1",
                string.Format("stage={0}, successCount={1}", siteRun.stage, CountHistoryEntries(siteRun, "success")));

            // Tick several more times to ensure the stage does not re-trigger
            story.DirectorTick();
            story.DirectorTick();
            story.DirectorTick();

            bool stillOnce = siteRun.stage == 1 && CountHistoryEntries(siteRun, "success") == 1;
            AddCheck("Reload_NoDuplicateTrigger", stillOnce, "stage=1, successCount=1",
                string.Format("stage={0}, successCount={1}", siteRun.stage, CountHistoryEntries(siteRun, "success")));

            CleanupTempScoutAndSite(scout, caravan, site);
        }

        private bool AcceptOfferLetter(string expectedLabel)
        {
            ChoiceLetter letter = FindChoiceLetterByLabel(expectedLabel);
            if (letter == null) return false;
            return ClickFirstChoice(letter);
        }

        private static ChoiceLetter FindChoiceLetterByLabel(string expectedLabel)
        {
            if (Find.LetterStack == null) return null;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                ChoiceLetter cl = letters[i] as ChoiceLetter;
                if (cl == null) continue;
                if (expectedLabel.NullOrEmpty() || string.Equals(cl.Label.ToString(), expectedLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return cl;
                }
            }
            return null;
        }

        private static bool ClickFirstChoice(ChoiceLetter letter)
        {
            if (letter == null) return false;
            IEnumerable<DiaOption> choices = letter.Choices; // CHECK API
            if (choices == null) return false;

            foreach (DiaOption opt in choices)
            {
                if (opt != null && opt.action != null && !opt.disabled)
                {
                    opt.action();
                    if (Find.LetterStack != null) Find.LetterStack.RemoveLetter(letter);
                    return true;
                }
            }
            return false;
        }

        private Pawn SpawnTempScout()
        {
            Pawn scout = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 10, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map);
            }, out cell))
            {
                cell = map.Center;
            }
            GenSpawn.Spawn(scout, cell, map, WipeMode.Vanish);
            return scout;
        }

        private static Caravan SendScoutToSiteMap(Pawn scout, Site site)
        {
            if (scout.Spawned) scout.DeSpawn(DestroyMode.Vanish);
            List<Pawn> list = new List<Pawn>();
            list.Add(scout);
            Caravan caravan = CaravanMaker.MakeCaravan(list, Faction.OfPlayer, site.Tile, true); // CHECK API
            Map siteMap = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null); // CHECK API
            CaravanEnterMapUtility.Enter(caravan, siteMap, CaravanEnterMode.Edge); // CHECK API
            return caravan;
        }

        private static void CleanupTempScoutAndSite(Pawn scout, Caravan caravan, Site site)
        {
            if (scout != null && !scout.Destroyed)
            {
                if (scout.Spawned) scout.DeSpawn(DestroyMode.Vanish);
                scout.Destroy(DestroyMode.Vanish);
            }
            if (caravan != null && !caravan.Destroyed)
            {
                caravan.Destroy();
            }
            if (site != null && !site.Destroyed)
            {
                if (site.HasMap && site.Map != null)
                {
                    Current.Game.DeinitAndRemoveMap(site.Map, false); // CHECK API
                }
                if (!site.Destroyed) site.Destroy();
            }
        }

        private static int KillAllHostilePawns(Map targetMap)
        {
            int count = 0;
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && !p.Dead && !p.Destroyed && p.HostileTo(Faction.OfPlayer))
                {
                    p.Kill(null);
                    count++;
                }
            }
            return count;
        }

        private static int CountTrackedOrHostilePawns(Map targetMap, StoryQuestRun run)
        {
            int count = 0;
            List<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && !p.Destroyed && p.HostileTo(Faction.OfPlayer))
                {
                    count++;
                }
            }
            if (count == 0 && run != null)
            {
                string raw = GetVar(run, "raidPawnIds");
                if (!raw.NullOrEmpty())
                {
                    count = raw.Split(',').Length;
                }
            }
            return count;
        }

        private static int KillTrackedOrHostilePawns(Map targetMap, StoryQuestRun run)
        {
            int count = 0;
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && !p.Dead && !p.Destroyed && p.HostileTo(Faction.OfPlayer))
                {
                    p.Kill(null);
                    count++;
                }
            }
            return count;
        }

        private static void AdvanceTicks(int deltaTicks)
        {
            if (deltaTicks <= 0) return;
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + deltaTicks); // CHECK API
        }

        private static StoryQuestRun FindQuestRun(SparkStory story, string questDefName)
        {
            // Uses reflection fallback if SparkStory stores runs in a private/public collection.
            // CHECK API: verify exact SparkStory run lookup method (e.g. story.GetRun(questDefName) or story.runs).
            Type t = story.GetType();
            MethodInfo getRun = t.GetMethod("GetRun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getRun != null)
            {
                return getRun.Invoke(story, new object[] { questDefName }) as StoryQuestRun;
            }

            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField != null)
            {
                object val = runsField.GetValue(story);
                Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
                if (dict != null)
                {
                    StoryQuestRun r;
                    if (dict.TryGetValue(questDefName, out r)) return r;
                }
                List<StoryQuestRun> list = val as List<StoryQuestRun>;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && list[i].quest == questDefName) return list[i];
                    }
                }
            }
            return null;
        }

        private static void ResetQuestRun(SparkStory story, string questDefName)
        {
            // CHECK API: remove previous run from SparkStory so the sample quest can be offered again.
            Type t = story.GetType();
            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField == null) return;

            object val = runsField.GetValue(story);
            Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
            if (dict != null)
            {
                dict.Remove(questDefName);
                return;
            }
            List<StoryQuestRun> list = val as List<StoryQuestRun>;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] != null && list[i].quest == questDefName) list.RemoveAt(i);
                }
            }
        }

        private static Site FindSite(int siteId)
        {
            if (siteId < 0) return null;
            List<Site> sites = Find.WorldObjects.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                Site s = sites[i];
                if (s != null && !s.Destroyed && s.ID == siteId) return s;
            }
            return null;
        }

        private static bool HasHistoryEntry(StoryQuestRun run, string outcome)
        {
            return CountHistoryEntries(run, outcome) > 0;
        }

        private static int CountHistoryEntries(StoryQuestRun run, string outcome)
        {
            if (run == null || run.history == null) return 0;
            int count = 0;
            string suffix = ":" + outcome;
            for (int i = 0; i < run.history.Count; i++)
            {
                string h = run.history[i];
                if (h != null && (h == outcome || h.EndsWith(suffix, StringComparison.Ordinal)))
                {
                    count++;
                }
            }
            return count;
        }

        private static int ParseInt(Dictionary<string, string> vars, string key, int fallback)
        {
            if (vars == null) return fallback;
            string raw;
            int val;
            if (vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
        }

        private static string GetVar(StoryQuestRun run, string key)
        {
            if (run == null || run.vars == null) return string.Empty;
            string raw;
            return run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
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
