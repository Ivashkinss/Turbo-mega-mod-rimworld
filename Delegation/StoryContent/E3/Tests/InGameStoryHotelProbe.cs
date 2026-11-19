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

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class StoryHotelProbeBootstrap
    {
        public const string SaveName = "TMM-StoryHotel";

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
    /// In-game verification probe for StoryStage_HotelVisit (noHotel, Host with HeroRegistry identity,
    /// Save/Reload mid-stay) and StoryStage_Deliver (colony stockpile deduction).
    /// Activated via -tmm-story-hotel-probe (fresh) or -tmm-story-hotel-reload (reload).
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
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(StoryHotelProbeBootstrap.ReloadMode ? "StoryHotelReloadProbe.json" : "StoryHotelProbe.json");
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

            StoryQuestDef hotelDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Hotel");
            StoryQuestDef deliverDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Deliver");
            AddCheck("SampleE3Defs_Loaded", hotelDef != null && deliverDef != null, "both loaded",
                string.Format("hotel={0}, deliver={1}", hotelDef != null, deliverDef != null));
            if (hotelDef == null || deliverDef == null) return;

            story.SetFlag("story.e3samples", "true"); // CHECK API

            // ---------------- TEST 1: NO HOTEL ----------------
            SetGuestsWelcome(map, false);
            int lettersBeforeNoHotel = Find.LetterStack.LettersListForReading.Count;

            story.DirectorTick(); // CHECK API
            bool acceptedNoHotel = AcceptOfferLetter(hotelDef.letterLabel);
            AddCheck("NoHotel_OfferAccepted", acceptedNoHotel, "true", acceptedNoHotel.ToString());
            story.DirectorTick();

            StoryQuestRun noHotelRun = FindQuestRun(story, "TMM_Story_Sample_Hotel");
            bool gotNoHotelOutcome = noHotelRun != null && HasHistoryEntry(noHotelRun, "noHotel");
            bool gotGuidanceLetter = Find.LetterStack.LettersListForReading.Count >= lettersBeforeNoHotel;
            AddCheck("NoHotel_OutcomeNoHotel", gotNoHotelOutcome, "noHotel",
                noHotelRun != null ? string.Join(",", noHotelRun.history.ToArray()) : "null");
            AddCheck("NoHotel_GuidanceLetterSent", gotGuidanceLetter, "letter present",
                Find.LetterStack.LettersListForReading.Count.ToString());

            // ---------------- BUILD READY HOTEL ----------------
            bool hotelBuilt = BuildTestHotel(map);
            SetGuestsWelcome(map, true);
            bool openHotel = StoryStage_HotelVisit.HasOpenHotel(map);
            AddCheck("Hotel_BuiltAndOpen", hotelBuilt && openHotel, "built=true, open=true",
                string.Format("built={0}, open={1}", hotelBuilt, openHotel));

            // ---------------- TEST 2: HOST WITH REGISTERED HERO ----------------
            StoryStage_HotelVisit hotelStage = hotelDef.stages[0] as StoryStage_HotelVisit;
            Pawn expectedHero = (hotelStage != null && hotelStage.hero != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(hotelStage.hero) : null;
            string expectedHeroId = expectedHero != null ? expectedHero.ThingID : string.Empty;

            ResetQuestRun(story, "TMM_Story_Sample_Hotel");
            story.DirectorTick();
            bool acceptedHost = AcceptOfferLetter(hotelDef.letterLabel);
            AddCheck("Host_OfferAccepted", acceptedHost, "true", acceptedHost.ToString());
            story.DirectorTick();

            StoryQuestRun hostRun = FindQuestRun(story, "TMM_Story_Sample_Hotel");
            bool guestsArrived = hostRun != null && GetVar(hostRun, "arrived") == "true";
            string actualHeroId = GetVar(hostRun, "heroThingId");
            bool heroIsRegistryPawn = !expectedHeroId.NullOrEmpty() && actualHeroId == expectedHeroId &&
                                      expectedHero != null && expectedHero.Spawned && expectedHero.Map == map;

            AddCheck("Host_GuestsArrived", guestsArrived, "arrived=true", GetVar(hostRun, "arrived"));
            AddCheck("Host_HeroMatchesRegistry", heroIsRegistryPawn, expectedHeroId,
                string.Format("var={0}, spawned={1}", actualHeroId, expectedHero != null && expectedHero.Spawned));

            float stay = hotelStage != null ? hotelStage.stayDays : 2f;
            AdvanceTicks(Mathf.CeilToInt((stay + 0.25f) * GenDate.TicksPerDay));
            story.DirectorTick();

            bool departureTriggered = GetVar(hostRun, "departureTriggered") == "true";
            AddCheck("Host_DepartureTriggeredAfterStay", departureTriggered, "true", GetVar(hostRun, "departureTriggered"));

            ExitTrackedGuestsCleanly(map, hostRun);
            story.DirectorTick();

            bool hostSucceeded = hostRun != null && HasHistoryEntry(hostRun, "success");
            AddCheck("Host_OutcomeSuccess", hostSucceeded, "success",
                hostRun != null ? string.Join(",", hostRun.history.ToArray()) : "null");

            // ---------------- TEST 3: DELIVER COLONY (50 STEEL) ----------------
            SpawnUnforbiddenResource(map, ThingDefOf.Steel, 50);
            int steelBefore = OutpostPodLogistics.CountResource(map, ThingDefOf.Steel); // CHECK API

            story.DirectorTick();
            bool acceptedDeliver = AcceptOfferLetter(deliverDef.letterLabel);
            AddCheck("Deliver_OfferAccepted", acceptedDeliver, "true", acceptedDeliver.ToString());
            story.DirectorTick();

            ChoiceLetter_StoryDeliver deliverLetter = FindDeliverLetter("TMM_Story_Sample_Deliver");
            AddCheck("Deliver_HandInLetterFound", deliverLetter != null, "found", deliverLetter != null ? "found" : "missing");

            bool handedIn = deliverLetter != null && ClickFirstChoice(deliverLetter);
            AddCheck("Deliver_HandInClicked", handedIn, "true", handedIn.ToString());
            story.DirectorTick();

            int steelAfter = OutpostPodLogistics.CountResource(map, ThingDefOf.Steel); // CHECK API
            StoryQuestRun deliverRun = FindQuestRun(story, "TMM_Story_Sample_Deliver");
            bool steelDeducted = (steelBefore - steelAfter) == 50;
            bool deliverSucceeded = deliverRun != null && HasHistoryEntry(deliverRun, "success");

            AddCheck("Deliver_SteelDeducted50", steelDeducted, (steelBefore - 50).ToString(), steelAfter.ToString());
            AddCheck("Deliver_OutcomeSuccess", deliverSucceeded, "success",
                deliverRun != null ? string.Join(",", deliverRun.history.ToArray()) : "null");

            // ---------------- TEST 4: SAVE MID-STAY FOR RELOAD ----------------
            ResetQuestRun(story, "TMM_Story_Sample_Hotel");
            story.DirectorTick();
            bool acceptedReload = AcceptOfferLetter(hotelDef.letterLabel);
            story.DirectorTick();

            StoryQuestRun saveRun = FindQuestRun(story, "TMM_Story_Sample_Hotel");
            bool activeMidStay = acceptedReload && saveRun != null && saveRun.stage == 0 && GetVar(saveRun, "arrived") == "true";
            AddCheck("ReloadPrep_ActiveStaySaved", activeMidStay, "stage=0, arrived=true",
                saveRun != null ? string.Format("stage={0}, arrived={1}", saveRun.stage, GetVar(saveRun, "arrived")) : "null");

            GameDataSaveLoader.SaveGame(StoryHotelProbeBootstrap.SaveName); // CHECK API
        }

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            StoryQuestDef hotelDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Hotel");
            StoryQuestRun run = FindQuestRun(story, "TMM_Story_Sample_Hotel");
            AddCheck("Reload_RunRestoredMidStay", run != null && run.stage == 0 && GetVar(run, "arrived") == "true",
                "stage=0, arrived=true",
                run != null ? string.Format("stage={0}, arrived={1}", run.stage, GetVar(run, "arrived")) : "null");
            if (run == null || hotelDef == null) return;

            StoryStage_HotelVisit stage = hotelDef.stages[0] as StoryStage_HotelVisit;
            float stay = stage != null ? stage.stayDays : 2f;

            AdvanceTicks(Mathf.CeilToInt((stay + 0.25f) * GenDate.TicksPerDay));
            story.DirectorTick();

            ExitTrackedGuestsCleanly(map, run);
            story.DirectorTick();

            bool completedOnce = CountHistoryEntries(run, "success") == 1;
            AddCheck("Reload_CompletesOnce", completedOnce, "successCount=1",
                "successCount=" + CountHistoryEntries(run, "success"));

            story.DirectorTick();
            story.DirectorTick();
            story.DirectorTick();

            bool stillOnce = CountHistoryEntries(run, "success") == 1;
            AddCheck("Reload_NoDuplicateTrigger", stillOnce, "successCount=1",
                "successCount=" + CountHistoryEntries(run, "success"));
        }

        private static bool BuildTestHotel(Map targetMap)
        {
            if (targetMap == null) return false;

            // Locate Hospitality guest bed def and optional reception desk def
            ThingDef guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("GuestBed"); // CHECK API
            if (guestBedDef == null) guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("GuestDoubleBed"); // CHECK API
            if (guestBedDef == null) guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("GuestSleepingSpot"); // CHECK API
            ThingDef receptionDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_HotelReception"); // CHECK API

            IntVec3 baseCell;
            if (!CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 12, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
            }, out baseCell))
            {
                baseCell = targetMap.Center;
            }

            if (receptionDef != null)
            {
                ThingDef stuff = receptionDef.MadeFromStuff ? ThingDefOf.WoodLog : null;
                Thing rec = ThingMaker.MakeThing(receptionDef, stuff);
                rec.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(rec, baseCell, targetMap, WipeMode.Vanish);
            }

            int spawnedBeds = 0;
            if (guestBedDef != null)
            {
                ThingDef bedStuff = guestBedDef.MadeFromStuff ? ThingDefOf.WoodLog : null;
                for (int i = 0; i < 3; i++)
                {
                    IntVec3 cell;
                    if (CellFinder.TryFindRandomCellNear(baseCell, targetMap, 8, delegate(IntVec3 c)
                    {
                        return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
                    }, out cell))
                    {
                        Thing bed = ThingMaker.MakeThing(guestBedDef, bedStuff);
                        bed.SetFaction(Faction.OfPlayer);
                        GenSpawn.Spawn(bed, cell, targetMap, WipeMode.Vanish);
                        spawnedBeds++;
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
            if (field != null)
            {
                field.SetValue(comp, welcome);
            }
        }

        private static void ExitTrackedGuestsCleanly(Map targetMap, StoryQuestRun run)
        {
            if (targetMap == null || run == null) return;
            string raw = GetVar(run, "guestIds");
            if (raw.NullOrEmpty()) return;

            string[] parts = raw.Split(',');
            HashSet<int> idSet = new HashSet<int>();
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) idSet.Add(val);
            }

            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p == null || !idSet.Contains(p.thingIDNumber)) continue;
                p.ExitMap(false, Rot4.South); // CHECK API
                if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                if (!p.Dead && !Find.WorldPawns.Contains(p))
                {
                    Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever); // CHECK API
                }
            }
        }

        private static void SpawnUnforbiddenResource(Map targetMap, ThingDef def, int count)
        {
            Thing t = ThingMaker.MakeThing(def);
            t.stackCount = count;
            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 10, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap);
            }, out cell))
            {
                cell = targetMap.Center;
            }
            GenPlace.TryPlaceThing(t, cell, targetMap, ThingPlaceMode.Near);
            t.SetForbidden(false, false);
        }

        private static bool AcceptOfferLetter(string expectedLabel)
        {
            if (Find.LetterStack == null) return false;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                ChoiceLetter cl = letters[i] as ChoiceLetter;
                if (cl == null) continue;
                if (expectedLabel.NullOrEmpty() || string.Equals(cl.Label.ToString(), expectedLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return ClickFirstChoice(cl);
                }
            }
            return false;
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
                    if (Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(letter))
                    {
                        Find.LetterStack.RemoveLetter(letter);
                    }
                    return true;
                }
            }
            return false;
        }

        private static void AdvanceTicks(int deltaTicks)
        {
            if (deltaTicks <= 0) return;
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + deltaTicks); // CHECK API
        }

        private static StoryQuestRun FindQuestRun(SparkStory story, string questDefName)
        {
            // CHECK API: verify exact SparkStory run lookup method
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
            // CHECK API: remove previous run from SparkStory for re-testing
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
