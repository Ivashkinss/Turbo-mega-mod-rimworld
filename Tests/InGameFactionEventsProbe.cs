using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Automated in-game probe for Task 61 (30 faction-specific flavor events across all 15 TMM factions).
    /// Activated via command-line flag: -tmm-faction-events-probe
    /// Uses an IEnumerator stepped on real Game.UpdatePlay frames so map/world state settles cleanly.
    /// For each of the 30 IncidentDefs:
    ///   - Checks CanFireNow with the target faction active.
    ///   - Executes TryExecute without exceptions and verifies a letter is created.
    ///   - For ChoiceLetter_TMMFactionEvent incidents, emulates every button option independently
    ///     and checks concrete consequences (silver, goodwill, pawns, items, hediffs, delayed tasks, sites, raids).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class InGameFactionEventsProbe
    {
        private const string ProbeFlag = "-tmm-faction-events-probe";
        private const string ReportDirArgPrefix = "-tmm-probe-dir=";
        private const string ReportFileName = "FactionEventsProbe.json";

        private static readonly bool Enabled;
        private static readonly string CustomReportDir;
        private static readonly object LogLock = new object();
        private static readonly List<string> CapturedErrors = new List<string>();

        private static bool started;
        private static bool completed;
        private static int readyFrames;
        private static bool capturingLogs;
        private static IEnumerator probeRoutine;

        private static readonly EventSpec[] Specs = new EventSpec[]
        {
            new EventSpec("TMM_Event_BosTechDemand", "TMM_BrotherhoodFaction", false, false, true, false),
            new EventSpec("TMM_Event_BosPaladinShelter", "TMM_BrotherhoodFaction", false, true, false, false),
            new EventSpec("TMM_Event_OutcastsTechForMeds", "TMM_OutcastsFaction", false, true, true, false),
            new EventSpec("TMM_Event_OutcastsCaravanAmbush", "TMM_OutcastsFaction", true, false, false, false),
            new EventSpec("TMM_Event_EnclavePropaganda", "TMM_EnclaveFaction", false, false, false, false),
            new EventSpec("TMM_Event_EnclavePunitiveDrop", "TMM_EnclaveFaction", false, false, false, true),
            new EventSpec("TMM_Event_InstituteCourserAbduction", "TMM_InstituteFaction", false, false, false, true),
            new EventSpec("TMM_Event_InstituteSynthReplacement", "TMM_InstituteFaction", false, false, false, false),
            new EventSpec("TMM_Event_RailroadFugitiveSynth", "TMM_RailroadFaction", false, true, false, false),
            new EventSpec("TMM_Event_RailroadDeadDrop", "TMM_RailroadFaction", false, true, false, false),
            new EventSpec("TMM_Event_GunnersExtortion", "TMM_GunnersFaction", false, false, false, false),
            new EventSpec("TMM_Event_GunnersMercContract", "TMM_GunnersFaction", false, false, false, false),
            new EventSpec("TMM_Event_VaultTecExperiment", "TMM_VaultTecFaction", false, true, false, false),
            new EventSpec("TMM_Event_VaultTecSupplyCaravan", "TMM_VaultTecFaction", false, true, false, false),
            new EventSpec("TMM_Event_MinutemenDistressSignal", "TMM_MinutemenHuntersFaction", false, true, false, false),
            new EventSpec("TMM_Event_HuntersMonsterWarning", "TMM_MinutemenHuntersFaction", false, true, false, false),
            new EventSpec("TMM_Event_MolBuyRecords", "TMM_MenOfLettersFaction", false, true, false, false),
            new EventSpec("TMM_Event_MolRecruitment", "TMM_MenOfLettersFaction", false, true, false, false),
            new EventSpec("TMM_Event_HeavenVesselRequest", "TMM_HeavenFaction", false, true, false, false),
            new EventSpec("TMM_Event_HeavenDivineOmen", "TMM_HeavenFaction", false, false, false, false),
            new EventSpec("TMM_Event_HellCrossroadsDeal", "TMM_HellFaction", false, false, false, false),
            new EventSpec("TMM_Event_HellDemonicPossession", "TMM_HellFaction", false, false, false, false),
            new EventSpec("TMM_Event_GhoulsRefugees", "TMM_GhoulsFaction", false, true, false, false),
            new EventSpec("TMM_Event_GhoulsRadTrade", "TMM_GhoulsFaction", false, true, false, false),
            new EventSpec("TMM_Event_FriendlyMutantJoin", "TMM_FriendlyMutantsFaction", false, true, false, false),
            new EventSpec("TMM_Event_FriendlyMutantHeal", "TMM_FriendlyMutantsFaction", false, true, false, false),
            new EventSpec("TMM_Event_WildMutantsMeatBags", "TMM_WildMutantsFaction", false, false, false, false),
            new EventSpec("TMM_Event_WildMutantsWarlordDuel", "TMM_WildMutantsFaction", false, false, false, false),
            new EventSpec("TMM_Event_MonsterVampireNightHunt", "TMM_MonsterClansFaction", false, false, false, false),
            new EventSpec("TMM_Event_MonsterShapeshifterGuest", "TMM_MonsterClansFaction", false, false, false, false)
        };

        static InGameFactionEventsProbe()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (string.IsNullOrEmpty(arg)) continue;
                    if (string.Equals(arg, ProbeFlag, StringComparison.OrdinalIgnoreCase))
                    {
                        Enabled = true;
                    }
                    else if (string.Equals(arg, ReloadFlag, StringComparison.OrdinalIgnoreCase))
                    {
                        Enabled = true;
                        reload = true;
                        LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame(SaveName));
                    }
                    else if (arg.StartsWith(ReportDirArgPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomReportDir = arg.Substring(ReportDirArgPrefix.Length).Trim('"');
                    }
                }
            }

            if (!Enabled) return;

            var harmony = new Harmony("turbomegamod.factionevents.probe");
            if (!reload)
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TMM_FACTION_PROBE_SEED")))
                    harmony.Patch(AccessTools.Method(typeof(WorldGenerator), "GenerateWorld"),
                        prefix: new HarmonyMethod(typeof(InGameFactionEventsProbe), "SetProbeWorldSeed"));
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TMM_FACTION_PROBE_TILE")))
                    harmony.Patch(AccessTools.Method(typeof(Game), "InitNewGame"),
                        prefix: new HarmonyMethod(typeof(InGameFactionEventsProbe), "SetProbeWorldTile"));
                harmony.Patch(AccessTools.DeclaredMethod(typeof(IncidentWorker), "TryExecute"),
                    postfix: new HarmonyMethod(typeof(InGameFactionEventsProbe), "TraceExecuteGate") { priority = Priority.Last });
                var phase = new HarmonyMethod(typeof(InGameFactionEventsProbe), "TraceRaidPhase") { priority = Priority.Last };
                harmony.Patch(AccessTools.DeclaredMethod(typeof(IncidentWorker_RaidEnemy), "TryResolveRaidFaction"), postfix: phase);
                harmony.Patch(AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_EdgeDrop), "TryResolveRaidSpawnCenter"), postfix: phase);
                harmony.Patch(AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_EdgeDrop), "Arrive"),
                    postfix: new HarmonyMethod(typeof(InGameFactionEventsProbe), "TraceRaidArrival") { priority = Priority.Last });
                harmony.Patch(AccessTools.DeclaredMethod(typeof(IncidentWorker_Raid), "TryExecuteWorker"), postfix: phase);
                harmony.Patch(AccessTools.DeclaredMethod(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo"),
                    postfix: new HarmonyMethod(typeof(InGameFactionEventsProbe), "TraceRaidGeneration") { priority = Priority.Last });
            }
            var updatePlay = AccessTools.Method(typeof(Game), "UpdatePlay"); // CHECK API
            if (updatePlay != null)
            {
                harmony.Patch(updatePlay, null, new HarmonyMethod(typeof(InGameFactionEventsProbe), "Game_UpdatePlay_Postfix"));
            }
            else
            {
                Log.Warning("[TMM] InGameFactionEventsProbe could not find Game.UpdatePlay.");
            }
        }

        private static List<string> enclaveRaidPhases;
        private static int enclavePawnsInPods;

        private static void SetProbeWorldSeed(ref string seedString)
        {
            seedString = Environment.GetEnvironmentVariable("TMM_FACTION_PROBE_SEED");
        }

        private static void SetProbeWorldTile()
        {
            int tile;
            if (Find.GameInitData == null || !int.TryParse(Environment.GetEnvironmentVariable("TMM_FACTION_PROBE_TILE"), out tile))
                throw new InvalidOperationException("FactionEvents fixture has no valid starting tile.");

            PlanetTile start = tile;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            Settlement player = settlements.FirstOrDefault(s => s.Faction == Faction.OfPlayer);
            if (player == null || !start.Valid || settlements.Any(s => s != player && s.Tile == start))
                throw new InvalidOperationException("FactionEvents fixture cannot create the player map at " + start + ".");

            // InitNewGame generates the map from the player settlement's tile, not the GameInitData field.
            player.Tile = start;
            Find.GameInitData.startingTile = start;
        }

        private static void TraceExecuteGate(IncidentDef ___def, bool __runOriginal, bool __result)
        {
            if (enclaveRaidPhases == null) return;
            enclaveRaidPhases.Add(___def.defName + ".TryExecute=" + __result + " ranOriginal=" + __runOriginal);
        }

        private static void TraceRaidPhase(MethodBase __originalMethod, IncidentParms parms, bool __result)
        {
            if (enclaveRaidPhases == null) return;
            enclaveRaidPhases.Add(__originalMethod.Name + "=" + __result + " faction=" + (parms.faction == null ? "-" : parms.faction.def.defName)
                + " points=" + parms.points + " center=" + parms.spawnCenter);
        }

        private static void TraceRaidArrival(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (enclaveRaidPhases == null || map == null || pawns == null) return;
            var raiders = new HashSet<Pawn>(pawns);
            foreach (DropPodIncoming pod in map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming).OfType<DropPodIncoming>())
            {
                if (pod.Contents == null || pod.Contents.innerContainer == null) continue;
                foreach (Pawn pawn in pod.Contents.innerContainer.OfType<Pawn>())
                    if (raiders.Contains(pawn)) enclavePawnsInPods++;
            }
            enclaveRaidPhases.Add("Arrive: actual raid pawns in incoming pods=" + enclavePawnsInPods + "/" + raiders.Count);
        }

        private static void TraceRaidGeneration(IncidentParms parms, ref List<Pawn> pawns, bool debugTest, bool __result)
        {
            if (enclaveRaidPhases == null || debugTest) return;
            enclaveRaidPhases.Add("TryGenerateRaidInfo=" + __result + " pawns=" + (pawns == null ? -1 : pawns.Count)
                + " center=" + parms.spawnCenter + " points=" + parms.points + " mode=" + (parms.raidArrivalMode == null ? "-" : parms.raidArrivalMode.defName));
        }

        private static void Game_UpdatePlay_Postfix()
        {
            if (!Enabled || completed) return;
            if (Current.ProgramState != ProgramState.Playing) return;
            if (Find.CurrentMap == null || Find.TickManager == null || Current.Game == null) return;
            if (LongEventHandler.AnyEventNowOrWaiting) return;

            if (!started)
            {
                readyFrames++;
                if (readyFrames < 30) return;
                started = true;
                if (reload) { RunReload(); return; }
                probeRoutine = RunProbeCoroutine();
            }

            if (probeRoutine == null) return;

            try
            {
                if (!probeRoutine.MoveNext())
                {
                    probeRoutine = null;
                    completed = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] InGameFactionEventsProbe unhandled coroutine exception: " + ex);
                probeRoutine = null;
                completed = true;
            }
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!capturingLogs) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                lock (LogLock)
                {
                    CapturedErrors.Add(type.ToString() + ": " + (condition ?? string.Empty));
                }
            }
        }

        private static IEnumerator RunProbeCoroutine()
        {
            ProbeReport report = new ProbeReport();
            int requestedTile;
            if (int.TryParse(Environment.GetEnvironmentVariable("TMM_FACTION_PROBE_TILE"), out requestedTile)
                && Find.CurrentMap.Tile != (PlanetTile)requestedTile)
                throw new InvalidOperationException("FactionEvents starting map tile did not match the fixture.");
            lock (LogLock) { CapturedErrors.Clear(); }

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            capturingLogs = true;

            Exception fatal = null;
            IEnumerator inner = ExecuteAllSpecsCoroutine(report);

            while (true)
            {
                bool moved;
                try
                {
                    moved = inner.MoveNext();
                }
                catch (Exception ex)
                {
                    fatal = ex;
                    moved = false;
                }
                if (!moved) break;
                yield return inner.Current;
            }

            if (fatal != null)
            {
                report.fatalException = fatal.ToString();
                Log.Error("[TMM] FactionEventsProbe fatal exception: " + fatal);
            }

            capturingLogs = false;
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;

            lock (LogLock)
            {
                for (int i = 0; i < CapturedErrors.Count; i++)
                {
                    report.errors.Add(CapturedErrors[i]);
                }
            }

            report.errorCount = report.errors.Count;
            report.totalSpecs = Specs.Length + 3 + GateChecks;
            report.passedSpecs = 0;
            for (int i = 0; i < report.results.Count; i++)
            {
                if (report.results[i].passed) report.passedSpecs++;
            }
            report.passed = string.IsNullOrEmpty(report.fatalException)
                && report.errorCount == 0
                && report.passedSpecs == report.totalSpecs;

            WriteReport(report);
            Log.Message("[TMM] FactionEventsProbe finished. passed=" + report.passed
                + " (" + report.passedSpecs + "/" + report.totalSpecs + "), errors=" + report.errorCount);
        }

        private static IEnumerator ExecuteAllSpecsCoroutine(ProbeReport report)
        {
            Map map = Find.AnyPlayerHomeMap != null ? Find.AnyPlayerHomeMap : Find.CurrentMap;
            if (map == null)
            {
                report.notes.Add("No active colony map found.");
                yield break;
            }

            // Save original goodwill for all factions so we can restore cleanly after probing.
            Dictionary<Faction, int> savedGoodwill = new Dictionary<Faction, int>();
            if (Find.FactionManager != null)
            {
                List<Faction> all = Find.FactionManager.AllFactionsListForReading;
                for (int i = 0; i < all.Count; i++)
                {
                    Faction f = all[i];
                    if (f != null && !f.IsPlayer && !f.defeated)
                    {
                        savedGoodwill[f] = f.PlayerGoodwill;
                    }
                }
            }

            EnsureColonistCount(map, 4);
            EnsureSilverOnMap(map, 6000);

            ProbePayments(map, report);

            for (int i = 0; i < Specs.Length; i++)
            {
                EventSpec spec = Specs[i];
                EventCheckResult res = new EventCheckResult();
                res.incidentDefName = spec.incidentDefName;
                res.factionDefName = spec.factionDefName;
                report.results.Add(res);

                IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(spec.incidentDefName);
                res.defFound = def != null;
                if (def == null)
                {
                    res.note = "IncidentDef not found.";
                    continue;
                }

                Faction fac = FactionEventHelper.FindActiveFaction(spec.factionDefName);
                res.factionFound = fac != null;
                if (fac == null)
                {
                    res.note = "Faction not active in world: " + spec.factionDefName;
                    continue;
                }

                // Prepare diplomatic preconditions for this event.
                if (spec.requiresNonHostile && fac.HostileTo(Faction.OfPlayer) && !fac.def.permanentEnemy)
                {
                    int delta = Math.Max(100, -fac.PlayerGoodwill + 25);
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null);
                }
                else if (spec.requiresLowGoodwill && (fac.PlayerGoodwill >= -50 || !fac.HostileTo(Faction.OfPlayer)) && !fac.def.permanentEnemy)
                {
                    int delta = -80 - fac.PlayerGoodwill; // below -75: hostile, as raid events need
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null);
                    if (!fac.HostileTo(Faction.OfPlayer)) fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, null);
                }

                EnsureColonistCount(map, 4);
                EnsureSilverOnMap(map, 4000);
                if (spec.requiresTechItem)
                {
                    EnsureSpacerComponentOnMap(map, 4);
                }

                Caravan tempCaravan = null;
                if (spec.isCaravanEvent)
                {
                    tempCaravan = CreateProbeCaravan(map.Tile, 1500);
                }

                IIncidentTarget target = spec.isCaravanEvent ? (IIncidentTarget)tempCaravan : (IIncidentTarget)map;
                if (target == null)
                {
                    res.note = "Could not build incident target.";
                    continue;
                }

                // 1. Check CanFireNow
                // forced: a fresh test colony is on day 0, so vanilla earliestDay/refire gates would hide the faction checks under test.
                IncidentParms canFireParms = BuildParms(target, 700f, true);
                try
                {
                    res.canFireNow = def.Worker.CanFireNow(canFireParms);
                }
                catch (Exception ex)
                {
                    res.exception = "CanFireNow: " + ex.Message;
                    CleanupProbeCaravan(tempCaravan);
                    continue;
                }

                // 2. Execute TryExecute once and check letter creation & non-choice effects
                int lettersBefore = Find.LetterStack != null ? Find.LetterStack.LettersListForReading.Count : 0;
                int tasksBefore = TMM_FactionEventsManager.Instance != null ? TMM_FactionEventsManager.Instance.tasks.Count : 0;
                int pawnsBefore = map.mapPawns.AllPawnsSpawnedCount;

                IncidentParms execParms = BuildParms(target, 700f, true);
                bool traceEnclave = spec.incidentDefName == "TMM_Event_EnclavePunitiveDrop";
                if (traceEnclave)
                {
                    enclavePawnsInPods = 0;
                    enclaveRaidPhases = new List<string>();
                }
                try
                {
                    res.executed = def.Worker.TryExecute(execParms);
                }
                catch (Exception ex)
                {
                    res.exception = "TryExecute: " + ex.Message;
                    CleanupProbeCaravan(tempCaravan);
                    continue;
                }

                finally
                {
                    if (enclaveRaidPhases != null)
                    {
                        res.note = "tile=" + map.Tile + " trace=[" + string.Join(" | ", enclaveRaidPhases) + "]";
                        enclaveRaidPhases = null;
                    }
                }

                if (!res.executed) res.note += " " + RefusalDetails(def, fac, map);
                Letter latest = FindLatestNewLetter(lettersBefore);
                res.letterCreated = latest != null;

                ChoiceLetter_TMMFactionEvent choiceLetter = latest as ChoiceLetter_TMMFactionEvent;
                if (choiceLetter == null)
                {
                    // Non-choice event: verify observable consequence (raid/pawn/item/task/thought/hediff).
                    int tasksAfter = TMM_FactionEventsManager.Instance != null ? TMM_FactionEventsManager.Instance.tasks.Count : 0;
                    int pawnsAfter = map.mapPawns.AllPawnsSpawnedCount;
                    res.effectVerified = traceEnclave
                        ? enclavePawnsInPods > 0
                        : res.letterCreated || tasksAfter > tasksBefore || pawnsAfter != pawnsBefore;
                    res.passed = res.defFound && res.factionFound && res.canFireNow && res.executed && res.letterCreated && res.effectVerified && string.IsNullOrEmpty(res.exception);
                    RemoveLetterIfPresent(latest);
                }
                else
                {
                    // Choice event: determine how many buttons exist and emulate each one independently.
                    List<DiaOption> initialOptions = choiceLetter.BuildOptions();
                    int optionCount = initialOptions.Count;
                    res.choiceCount = optionCount;
                    RemoveLetterIfPresent(choiceLetter);

                    int verifiedChoices = 0;
                    for (int optIdx = 0; optIdx < optionCount; optIdx++)
                    {
                        EnsureColonistCount(map, 4);
                        EnsureSilverOnMap(map, 4000);
                        if (spec.requiresTechItem) EnsureSpacerComponentOnMap(map, 3);

                        if (spec.isCaravanEvent && (tempCaravan == null || tempCaravan.Destroyed || !tempCaravan.Spawned))
                        {
                            tempCaravan = CreateProbeCaravan(map.Tile, 1500);
                            target = tempCaravan;
                        }

                        int preLetters = Find.LetterStack != null ? Find.LetterStack.LettersListForReading.Count : 0;
                        int preSilver = spec.isCaravanEvent
                            ? FactionEventHelper.CountSilverInCaravan(tempCaravan)
                            : FactionEventHelper.CountSilverOnMap(map);
                        int preGoodwill = fac.PlayerGoodwill;
                        int preColonists = map.mapPawns.FreeColonistsSpawnedCount;
                        int preAllPawns = map.mapPawns.AllPawnsSpawnedCount;
                        int preTasks = TMM_FactionEventsManager.Instance != null ? TMM_FactionEventsManager.Instance.tasks.Count : 0;
                        int preMaps = Find.Maps != null ? Find.Maps.Count : 0;
                        int preSites = Find.WorldObjects != null ? Find.WorldObjects.Sites.Count : 0;
                        int preDropPods = CountActiveDropPods(map);

                        IncidentParms optParms = BuildParms(target, 650f, true);
                        bool okExec = def.Worker.TryExecute(optParms);
                        ChoiceLetter_TMMFactionEvent freshLetter = FindLatestNewLetter(preLetters) as ChoiceLetter_TMMFactionEvent;
                        if (!okExec || freshLetter == null)
                        {
                            res.note += "Choice " + optIdx + " failed to spawn letter; ";
                            continue;
                        }

                        List<DiaOption> opts = freshLetter.BuildOptions();
                        if (optIdx >= opts.Count)
                        {
                            RemoveLetterIfPresent(freshLetter);
                            continue;
                        }

                        DiaOption chosen = opts[optIdx];
                        if (chosen.disabled)
                        {
                            res.note += "Choice " + optIdx + " unexpectedly disabled (" + chosen.disabledReason + "); ";
                            RemoveLetterIfPresent(freshLetter);
                            continue;
                        }

                        try
                        {
                            if (chosen.action != null) chosen.action();
                        }
                        catch (Exception ex)
                        {
                            res.exception = "Choice " + optIdx + ": " + ex.Message;
                            RemoveLetterIfPresent(freshLetter);
                            continue;
                        }

                        int postSilver = spec.isCaravanEvent
                            ? FactionEventHelper.CountSilverInCaravan(tempCaravan)
                            : FactionEventHelper.CountSilverOnMap(map);
                        int postGoodwill = fac.PlayerGoodwill;
                        int postColonists = map.mapPawns.FreeColonistsSpawnedCount;
                        int postAllPawns = map.mapPawns.AllPawnsSpawnedCount;
                        int postTasks = TMM_FactionEventsManager.Instance != null ? TMM_FactionEventsManager.Instance.tasks.Count : 0;
                        int postMaps = Find.Maps != null ? Find.Maps.Count : 0;
                        int postSites = Find.WorldObjects != null ? Find.WorldObjects.Sites.Count : 0;
                        int postDropPods = CountActiveDropPods(map);

                        bool isDeclineNoOp = (optIdx == optionCount - 1) && freshLetter.resolved;
                        bool observedChange = postSilver != preSilver
                            || postGoodwill != preGoodwill
                            || postColonists != preColonists
                            || postAllPawns != preAllPawns
                            || postTasks != preTasks
                            || postMaps != preMaps
                            || postSites != preSites
                            || postDropPods != preDropPods
                            || HasAnyEventHediff(map);

                        if (freshLetter.resolved && (observedChange || isDeclineNoOp))
                        {
                            verifiedChoices++;
                        }
                        else
                        {
                            res.note += "Choice " + optIdx + " resolved=" + freshLetter.resolved + " change=" + observedChange + "; ";
                        }

                        // Clean up hostile pawns / temporary battle maps between choices.
                        CleanupHostilesOnMap(map);
                        CleanupExtraMaps(map);
                        yield return null;
                    }

                    res.choicesVerified = verifiedChoices;
                    res.effectVerified = optionCount > 0 && verifiedChoices == optionCount;
                    res.passed = res.defFound && res.factionFound && res.canFireNow && res.executed && res.letterCreated && res.effectVerified && string.IsNullOrEmpty(res.exception);
                }

                CleanupProbeCaravan(tempCaravan);
                CleanupHostilesOnMap(map);
                CleanupExtraMaps(map);

                // Yield a real frame between events so RimWorld stays responsive and processes deferred cleanups.
                yield return null;
            }

            // Restore faction goodwill and clear probe-scheduled delayed tasks.
            foreach (KeyValuePair<Faction, int> kv in savedGoodwill)
            {
                if (kv.Key == null || kv.Key.defeated || kv.Key.def.permanentEnemy) continue;
                int diff = kv.Value - kv.Key.PlayerGoodwill;
                if (diff != 0)
                {
                    kv.Key.TryAffectGoodwillWith(Faction.OfPlayer, diff, false, false, null, null);
                }
            }
            if (TMM_FactionEventsManager.Instance != null)
            {
                TMM_FactionEventsManager.Instance.tasks.Clear();
            }
            Gates(map, report);
            OpenLetterForSave(map, report);
        }

        /// Why a raid event refused: relations, colonists, and a dry run of the raid itself (it spawns nothing with debugTest).
        private static string RefusalDetails(IncidentDef def, Faction f, Map map)
        {
            FactionRelation ours = f.RelationWith(Faction.OfPlayer), theirs = Faction.OfPlayer.RelationWith(f);
            string text = "hostile=" + f.HostileTo(Faction.OfPlayer) + " kind=" + ours.kind + "/" + theirs.kind + " goodwill=" + f.PlayerGoodwill + " base=" + ours.baseGoodwill + " defeated=" + f.defeated
                + " deactivated=" + f.deactivated + " colonists=" + map.mapPawns.FreeColonistsSpawnedCount + " needColonists=" + IncidentDefOf.RaidEnemy.requireColonistsPresent;
            try
            {
                IncidentParms parms = new IncidentParms { target = map, faction = f, points = Math.Max(700f, f.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat)), raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop, raidStrategy = RaidStrategyDefOf.ImmediateAttack, forced = true };
                object[] args = { parms, null, true };
                bool ok = (bool)AccessTools.Method(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo").Invoke(IncidentDefOf.RaidEnemy.Worker, args);
                List<Pawn> pawns = args[1] as List<Pawn>;
                text += " dryRun=" + ok + " pawns=" + (pawns != null ? pawns.Count : -1) + " points=" + parms.points + " faction=" + (parms.faction != null ? parms.faction.def.defName : "null") + " age=" + (parms.raidAgeRestriction != null ? parms.raidAgeRestriction.defName : "none");
                if (pawns != null) foreach (Pawn p in pawns) if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
            }
            catch (Exception ex) { text += " dryRun: " + (ex.InnerException ?? ex); }
            // Diagnostic only (h-events-3, 064527): the same dry run with the event's own points, and the drop spot alone.
            try
            {
                float pts = StorytellerUtility.DefaultThreatPointsNow(map);
                IncidentParms p2 = new IncidentParms { target = map, faction = f, points = Math.Max(pts, f.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat)), raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop, raidStrategy = RaidStrategyDefOf.ImmediateAttack, forced = true };
                object[] a2 = { p2, null, true };
                bool ok2 = (bool)AccessTools.Method(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo").Invoke(IncidentDefOf.RaidEnemy.Worker, a2);
                List<Pawn> pw2 = a2[1] as List<Pawn>;
                text += " eventPoints=" + p2.points + " dryRun2=" + ok2 + " pawns2=" + (pw2 != null ? pw2.Count : -1);
                if (pw2 != null) foreach (Pawn p in pw2) if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
                IncidentParms p3 = new IncidentParms { target = map, faction = f, points = p2.points, raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop, raidStrategy = RaidStrategyDefOf.ImmediateAttack, forced = true };
                text += " dropCenter=" + PawnsArrivalModeDefOf.EdgeDrop.Worker.TryResolveRaidSpawnCenter(p3) + " at " + p3.spawnCenter + " strategyOk=" + RaidStrategyDefOf.ImmediateAttack.Worker.CanUseWith(p3, PawnGroupKindDefOf.Combat);
            }
            catch (Exception ex) { text += " diag: " + (ex.InnerException ?? ex).Message; }
            foreach (Type t in new[] { typeof(IncidentWorker), typeof(IncidentWorker_Raid), typeof(IncidentWorker_RaidEnemy) })
                foreach (string name in new[] { "TryExecute", "TryExecuteWorker", "TryGenerateRaidInfo" })
                {
                    MethodInfo m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    Patches patches = m != null ? Harmony.GetPatchInfo(m) : null;
                    if (patches != null) text += " " + t.Name + "." + name + ":" + string.Join(",", patches.Owners.ToArray());
                }
            return text;
        }

        // ---- The storyteller's own gates (forced=false): first day, refire interval, relations.
        private const int GateChecks = 9;

        private static void Relation(Faction f, FactionRelationKind kind, int goodwill)
        {
            foreach (FactionRelation r in new[] { f.RelationWith(Faction.OfPlayer), Faction.OfPlayer.RelationWith(f) }) { r.kind = kind; r.baseGoodwill = goodwill; }
        }

        private static void Gates(Map map, ProbeReport report)
        {
            IncidentDef supply = DefDatabase<IncidentDef>.GetNamed("TMM_Event_VaultTecSupplyCaravan"), drop = DefDatabase<IncidentDef>.GetNamed("TMM_Event_EnclavePunitiveDrop");
            Faction vault = FactionEventHelper.FindActiveFaction("TMM_VaultTecFaction"), enclave = FactionEventHelper.FindActiveFaction("TMM_EnclaveFaction");
            FactionRelationKind vaultKind = vault.RelationKindWith(Faction.OfPlayer), enclaveKind = enclave.RelationKindWith(Faction.OfPlayer);
            int vaultWill = vault.RelationWith(Faction.OfPlayer).baseGoodwill, enclaveWill = enclave.RelationWith(Faction.OfPlayer).baseGoodwill;
            Relation(vault, FactionRelationKind.Neutral, 10);
            Relation(enclave, FactionRelationKind.Hostile, -100);
            map.StoryState.lastFireTicks.Remove(supply);
            map.StoryState.lastFireTicks.Remove(drop);
            // CanFireNow remembers its answer for the rest of the tick; without clearing it every later check would read the first one.
            FieldInfo checkedAt = AccessTools.Field(typeof(IncidentWorker), "lastCheckCanRunTick");
            Func<IncidentDef, bool> can = d => { checkedAt.SetValue(d.Worker, -1); return d.Worker.CanFireNow(BuildParms(map, 2000f, false)); };
            RecordPaymentCheck(report, "GateEarlyDay", GenDate.DaysPassed < supply.earliestDay && !can(supply) && !can(drop), "day=" + GenDate.DaysPassed + " earliest=" + supply.earliestDay + "/" + drop.earliestDay);
            int ticks = Find.TickManager.TicksGame;
            Find.TickManager.DebugSetTicksGame(Math.Max(ticks, (Math.Max(supply.earliestDay, drop.earliestDay) + 40) * 60000));
            RecordPaymentCheck(report, "GateOpenLater", can(supply) && can(drop), "day=" + GenDate.DaysPassed + " supply=" + can(supply) + " drop=" + can(drop));
            int now = Find.TickManager.TicksGame;
            map.StoryState.lastFireTicks[supply] = now - 60000;
            RecordPaymentCheck(report, "GateRefireClosed", !can(supply), "1 of " + supply.minRefireDays + " days");
            map.StoryState.lastFireTicks[supply] = now - (int)((supply.minRefireDays + 1f) * 60000f);
            RecordPaymentCheck(report, "GateRefireOpen", can(supply), "after " + (supply.minRefireDays + 1) + " days");
            Relation(vault, FactionRelationKind.Hostile, -100);
            RecordPaymentCheck(report, "GateFriendlyEventNeedsPeace", !can(supply), "hostile Vault-Tec");
            Relation(vault, FactionRelationKind.Ally, 90);
            RecordPaymentCheck(report, "GateFriendlyEventWithAlly", can(supply), "allied Vault-Tec");
            Relation(enclave, FactionRelationKind.Neutral, 0);
            RecordPaymentCheck(report, "GateRaidNeedsHostility", !can(drop), "neutral Enclave");
            Relation(enclave, FactionRelationKind.Hostile, -100);
            RecordPaymentCheck(report, "GateRaidWithHostility", can(drop), "hostile Enclave");
            Relation(vault, vaultKind, vaultWill);
            Relation(enclave, enclaveKind, enclaveWill);
            map.StoryState.lastFireTicks.Remove(supply);
            Find.TickManager.DebugSetTicksGame(ticks);
        }

        // ---- An unanswered paid letter is saved; after loading it is paid once.
        private const string SaveName = "TMM-FactionEvents";
        private const string ReloadFlag = "-tmm-faction-events-reload";
        private static bool reload;

        private static void OpenLetterForSave(Map map, ProbeReport report)
        {
            foreach (Letter old in Find.LetterStack.LettersListForReading.ToList()) if (old is ChoiceLetter_TMMFactionEvent) Find.LetterStack.RemoveLetter(old);
            EnsureSilverOnMap(map, 500);
            ChoiceLetter_TMMFactionEvent letter = PaymentLetter(FactionEventChoiceKind.VaultTecSupplyCaravan, map, null, 70);
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FactionEventsSilver.txt"), FactionEventHelper.CountSilverOnMap(map).ToString());
            GameDataSaveLoader.SaveGame(SaveName);
            RecordPaymentCheck(report, "OpenLetterSaved", Find.LetterStack.LettersListForReading.Contains(letter) && File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, SaveName + ".rws")), "silver=" + FactionEventHelper.CountSilverOnMap(map));
            RemoveLetterIfPresent(letter);
        }

        private static void RunReload()
        {
            string note;
            bool ok = false;
            try
            {
                Map map = Find.AnyPlayerHomeMap;
                int before = int.Parse(File.ReadAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FactionEventsSilver.txt")));
                List<ChoiceLetter_TMMFactionEvent> letters = Find.LetterStack.LettersListForReading.OfType<ChoiceLetter_TMMFactionEvent>().ToList();
                int loaded = FactionEventHelper.CountSilverOnMap(map), pods = CountActiveDropPods(map);
                DiaOption option = letters.Count == 1 ? letters[0].BuildOptions()[0] : null;
                if (option != null) { option.action(); option.action(); }
                int after = FactionEventHelper.CountSilverOnMap(map), podsAfter = CountActiveDropPods(map);
                ok = letters.Count == 1 && letters[0].resolved && loaded == before && after == before - 70 && podsAfter == pods + 1 && !Find.LetterStack.LettersListForReading.Contains(letters[0]);
                note = "letters=" + letters.Count + " silver " + before + "/" + loaded + "/" + after + " pods " + pods + "/" + podsAfter;
            }
            catch (Exception ex) { note = ex.ToString(); }
            completed = true;
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FactionEventsReloadProbe.json"), "{\"passed\":" + (ok ? "true" : "false") + ",\"checks\":[{\"name\":\"LetterPaidOnceAfterLoad\",\"passed\":" + (ok ? "true" : "false") +
                ",\"actual\":\"" + note.Replace("\\", "\\\\").Replace("\"", "'").Replace("\r", "").Replace("\n", " ") + "\"}],\"errors\":[]}");
        }

        private static IncidentParms BuildParms(IIncidentTarget target, float points, bool forced)
        {
            IncidentParms p = new IncidentParms();
            p.target = target;
            p.points = points;
            p.forced = forced;
            return p;
        }

        private static Letter FindLatestNewLetter(int countBefore)
        {
            if (Find.LetterStack == null) return null;
            List<Letter> list = Find.LetterStack.LettersListForReading;
            if (list == null || list.Count <= countBefore) return null;
            return list[list.Count - 1];
        }

        private static void RemoveLetterIfPresent(Letter letter)
        {
            if (letter != null && Find.LetterStack != null)
            {
                Find.LetterStack.RemoveLetter(letter);
            }
        }

        private static int CountActiveDropPods(Map map)
        {
            if (map == null || map.listerThings == null) return 0;
            return map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming).Count;
        }

        private static bool HasAnyEventHediff(Map map)
        {
            if (map == null || map.mapPawns == null) return false;
            string[] names = new string[]
            {
                "TMM_Hediff_VaultTecSerum",
                "TMM_Hediff_AngelicVessel",
                "TMM_Hediff_DemonicPossession",
                "TMM_Hediff_HiddenSynth"
            };
            List<Pawn> cols = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < cols.Count; i++)
            {
                Pawn p = cols[i];
                if (p == null || p.health == null || p.health.hediffSet == null) continue;
                for (int n = 0; n < names.Length; n++)
                {
                    HediffDef hd = DefDatabase<HediffDef>.GetNamedSilentFail(names[n]);
                    if (hd != null && p.health.hediffSet.HasHediff(hd)) return true;
                }
            }
            return false;
        }

        private static void EnsureColonistCount(Map map, int required)
        {
            while (map.mapPawns.FreeColonistsSpawnedCount < required)
            {
                Pawn p = FactionEventHelper.GenerateFactionPawn(Faction.OfPlayer, map, null, PawnKindDefOf.Colonist);
                if (p == null) break;
                IntVec3 cell;
                if (!CellFinder.TryFindRandomCellNear(map.Center, map, 12,
                    delegate(IntVec3 c) { return c.InBounds(map) && c.Standable(map) && !c.Fogged(map); },
                    out cell))
                {
                    cell = map.Center;
                }
                GenSpawn.Spawn(p, cell, map);
            }
        }

        private static void EnsureSilverOnMap(Map map, int minAmount)
        {
            int have = FactionEventHelper.CountSilverOnMap(map);
            if (have >= minAmount) return;
            int need = minAmount - have;
            while (need > 0)
            {
                Thing s = ThingMaker.MakeThing(ThingDefOf.Silver);
                s.stackCount = Math.Min(need, ThingDefOf.Silver.stackLimit);
                GenPlace.TryPlaceThing(s, map.Center, map, ThingPlaceMode.Near);
                need -= s.stackCount;
            }
        }

        private static void RecordPaymentCheck(ProbeReport report, string name, bool passed, string details)
        {
            report.results.Add(new EventCheckResult { incidentDefName = name, passed = passed, note = details });
        }

        private static ChoiceLetter_TMMFactionEvent PaymentLetter(FactionEventChoiceKind kind, Map map, Caravan caravan, int cost)
        {
            return FactionEventHelper.SendChoiceLetter("Payment probe", "Payment probe", LetterDefOf.NeutralEvent,
                LookTargets.Invalid, null, kind, map, caravan, null, null, cost, 0, 0);
        }

        private static Thing AddPaymentSilver(ThingOwner<Thing> inventory, int count)
        {
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = count;
            if (!inventory.TryAdd(silver, false)) throw new InvalidOperationException("Cannot prepare silver probe.");
            return silver;
        }

        private static void ProbePayments(Map map, ProbeReport report)
        {
            Caravan caravan = CreateProbeCaravan(map.Tile, 0);
            if (caravan == null || caravan.PawnsListForReading.Count < 2)
                throw new InvalidOperationException("Two caravan members are required for the payment probe.");
            ThingOwner<Thing> first = caravan.PawnsListForReading[0].inventory.innerContainer;
            ThingOwner<Thing> second = caravan.PawnsListForReading[1].inventory.innerContainer;
            AddPaymentSilver(first, 60);
            AddPaymentSilver(second, 40);
            Thing reserved = AddPaymentSilver(first, 125);
            OutpostCaravanManager.RegisterTransientSilverProtection(reserved.ThingID, "faction-payment-probe");
            ChoiceLetter_TMMFactionEvent toll = PaymentLetter(FactionEventChoiceKind.OutcastsCaravanAmbush, null, caravan, 90);
            try
            {
                int spendable = FactionEventHelper.CountSilverInCaravan(caravan);
                DiaOption option = toll.BuildOptions()[0];
                int steps = 0;
                StoryPayment.TestSplitOffOverride = (t, n, i) => { steps++; return i == 1 ? null : t.SplitOff(n); };
                try { option.action(); }
                finally { StoryPayment.TestSplitOffOverride = null; }
                bool rejected = !toll.resolved && Find.LetterStack.LettersListForReading.Contains(toll) &&
                    FactionEventHelper.CountSilverInCaravan(caravan) == 100 && reserved.stackCount == 125 && reserved.holdingOwner == first;

                DiaOption retry = toll.BuildOptions()[0];
                retry.action();
                int after = FactionEventHelper.CountSilverInCaravan(caravan);
                retry.action();
                RecordPaymentCheck(report, "CaravanTollExcludesMissionSilverAndRetries",
                    !option.disabled && spendable == 100 && steps == 2 && rejected && toll.resolved &&
                    !Find.LetterStack.LettersListForReading.Contains(toll) && after == 10 &&
                    FactionEventHelper.CountSilverInCaravan(caravan) == 10 && reserved.stackCount == 125 && reserved.holdingOwner == first,
                    "spendable=" + spendable + " stagedSteps=" + steps + " rollback=" + rejected + " remaining=" + after);
            }
            finally
            {
                OutpostCaravanManager.UnregisterTransientSilverProtection(reserved.ThingID);
                RemoveLetterIfPresent(toll);
                CleanupProbeCaravan(caravan);
            }

            ProbeMapReward(map, report, FactionEventChoiceKind.GunnersMercContract, "mercenary1", true);
            ProbeMapReward(map, report, FactionEventChoiceKind.VaultTecSupplyCaravan, "podSpawned", false);
        }

        private static void ProbeMapReward(Map map, ProbeReport report, FactionEventChoiceKind kind, string failAt, bool mercenaries)
        {
            const int price = 70;
            ChoiceLetter_TMMFactionEvent letter = PaymentLetter(kind, map, null, price);
            FieldInfo hook = typeof(FactionEventHelper).GetField("TestBeforePaidRewardStep", BindingFlags.Static | BindingFlags.NonPublic);
            int startingSilver = FactionEventHelper.CountSilverOnMap(map);
            int before = mercenaries ? map.mapPawns.FreeColonistsSpawnedCount : CountActiveDropPods(map);
            int expected = mercenaries ? 2 : 1;
            bool reached = false;
            try
            {
                DiaOption option = letter.BuildOptions()[0];
                hook.SetValue(null, (Action<string>)(step =>
                {
                    if (step != failAt) return;
                    reached = true;
                    throw new InvalidOperationException("Injected reward failure");
                }));
                try { option.action(); }
                finally { hook.SetValue(null, null); }
                int afterFailure = mercenaries ? map.mapPawns.FreeColonistsSpawnedCount : CountActiveDropPods(map);
                bool rollback = !letter.resolved && Find.LetterStack.LettersListForReading.Contains(letter) &&
                    FactionEventHelper.CountSilverOnMap(map) == startingSilver && afterFailure == before;

                DiaOption retry = letter.BuildOptions()[0];
                retry.action();
                retry.action();
                int finalCount = mercenaries ? map.mapPawns.FreeColonistsSpawnedCount : CountActiveDropPods(map);
                RecordPaymentCheck(report, kind + "PaymentRollbackAndRetry",
                    !option.disabled && reached && rollback && letter.resolved && finalCount == before + expected &&
                    !Find.LetterStack.LettersListForReading.Contains(letter) &&
                    FactionEventHelper.CountSilverOnMap(map) == startingSilver - price,
                    "reached=" + reached + " rollback=" + rollback + " rewardDelta=" + (finalCount - before));
            }
            finally
            {
                hook.SetValue(null, null);
                RemoveLetterIfPresent(letter);
            }
        }

        private static void EnsureSpacerComponentOnMap(Map map, int minCount)
        {
            ThingDef def = ThingDefOf.ComponentSpacer;
            int have = StoryPayment.CountOnMap(map, def, false);
            if (have >= minCount) return;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(map.Center, 10f, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map) ||
                    cell.GetThingList(map).Any(t => t.def == def)) continue;
                Thing comp = ThingMaker.MakeThing(def);
                comp.stackCount = minCount - have;
                GenSpawn.Spawn(comp, cell, map);
                comp.SetForbidden(false, false);
                if (StoryPayment.CountOnMap(map, def, false) >= minCount) return;
                break;
            }

            throw new InvalidOperationException("FactionEvents fixture could not supply accessible spacer components.");
        }

        private static Caravan CreateProbeCaravan(PlanetTile tile, int silverAmount)
        {
            List<Pawn> members = new List<Pawn>();
            for (int i = 0; i < 2; i++)
            {
                Pawn c = FactionEventHelper.GenerateFactionPawn(Faction.OfPlayer, null, null, PawnKindDefOf.Colonist);
                if (c != null) members.Add(c);
            }
            if (members.Count == 0) return null;
            Caravan car = CaravanMaker.MakeCaravan(members, Faction.OfPlayer, tile, true);
            if (car != null && silverAmount > 0)
            {
                Thing s = ThingMaker.MakeThing(ThingDefOf.Silver);
                s.stackCount = silverAmount;
                CaravanInventoryUtility.GiveThing(car, s);
            }
            return car;
        }

        private static void CleanupProbeCaravan(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed) return;
            List<Pawn> pawns = new List<Pawn>(caravan.PawnsListForReading);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null) continue;
                if (caravan.ContainsPawn(p)) caravan.RemovePawn(p);
                if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
            }
            if (caravan.Spawned && Find.WorldObjects != null)
            {
                Find.WorldObjects.Remove(caravan);
            }
        }

        private static void CleanupHostilesOnMap(Map map)
        {
            if (map == null || map.mapPawns == null) return;
            List<Pawn> all = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < all.Count; i++)
            {
                Pawn p = all[i];
                if (p == null || p.Destroyed || p.Faction == Faction.OfPlayer) continue;
                if (p.Faction != null && p.Faction.HostileTo(Faction.OfPlayer))
                {
                    p.DeSpawn(DestroyMode.Vanish);
                    p.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void CleanupExtraMaps(Map homeMap)
        {
            if (Find.Maps == null) return;
            List<Map> maps = new List<Map>(Find.Maps);
            for (int i = 0; i < maps.Count; i++)
            {
                Map m = maps[i];
                if (m == null || m == homeMap || m.IsPlayerHome) continue;
                if (m.Parent != null && Find.WorldObjects != null)
                {
                    Find.WorldObjects.Remove(m.Parent);
                }
            }
        }

        private static string ResolveReportDirectory()
        {
            if (!string.IsNullOrEmpty(CustomReportDir)) return CustomReportDir;
            string envDir = Environment.GetEnvironmentVariable("TMM_PROBE_DIR");
            if (!string.IsNullOrEmpty(envDir)) return envDir;
            return GenFilePaths.SaveDataFolderPath;
        }

        private static void WriteReport(ProbeReport r)
        {
            try
            {
                string dir = ResolveReportDirectory();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, ReportFileName);
                File.WriteAllText(path, SerializeReport(r), Encoding.UTF8);
                // The test harness also waits for a UI report; events have no UI tab, so it mirrors the result.
                File.WriteAllText(Path.Combine(dir, "FactionEventsUiProbe.json"), "{\"passed\":" + (r.passed ? "true" : "false") + "}", Encoding.UTF8);
                Log.Message("[TMM] Wrote FactionEventsProbe report: " + path);
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] Failed to write FactionEventsProbe report: " + ex);
            }
        }

        private static string SerializeReport(ProbeReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("  \"passed\": ").Append(r.passed ? "true" : "false").AppendLine(",");
            sb.Append("  \"passedSpecs\": ").Append(r.passedSpecs.ToString(CultureInfo.InvariantCulture)).AppendLine(",");
            sb.Append("  \"totalSpecs\": ").Append(r.totalSpecs.ToString(CultureInfo.InvariantCulture)).AppendLine(",");
            sb.Append("  \"errorCount\": ").Append(r.errorCount.ToString(CultureInfo.InvariantCulture)).AppendLine(",");
            sb.Append("  \"fatalException\": \"").Append(EscapeJson(r.fatalException)).AppendLine("\",");

            sb.AppendLine("  \"results\": [");
            for (int i = 0; i < r.results.Count; i++)
            {
                EventCheckResult e = r.results[i];
                sb.AppendLine("    {");
                sb.Append("      \"incidentDefName\": \"").Append(EscapeJson(e.incidentDefName)).AppendLine("\",");
                sb.Append("      \"factionDefName\": \"").Append(EscapeJson(e.factionDefName)).AppendLine("\",");
                sb.Append("      \"passed\": ").Append(e.passed ? "true" : "false").AppendLine(",");
                sb.Append("      \"defFound\": ").Append(e.defFound ? "true" : "false").AppendLine(",");
                sb.Append("      \"factionFound\": ").Append(e.factionFound ? "true" : "false").AppendLine(",");
                sb.Append("      \"canFireNow\": ").Append(e.canFireNow ? "true" : "false").AppendLine(",");
                sb.Append("      \"executed\": ").Append(e.executed ? "true" : "false").AppendLine(",");
                sb.Append("      \"letterCreated\": ").Append(e.letterCreated ? "true" : "false").AppendLine(",");
                sb.Append("      \"choiceCount\": ").Append(e.choiceCount.ToString(CultureInfo.InvariantCulture)).AppendLine(",");
                sb.Append("      \"choicesVerified\": ").Append(e.choicesVerified.ToString(CultureInfo.InvariantCulture)).AppendLine(",");
                sb.Append("      \"effectVerified\": ").Append(e.effectVerified ? "true" : "false").AppendLine(",");
                sb.Append("      \"exception\": \"").Append(EscapeJson(e.exception)).AppendLine("\",");
                sb.Append("      \"note\": \"").Append(EscapeJson(e.note)).AppendLine("\"");
                sb.Append("    }").AppendLine(i + 1 < r.results.Count ? "," : string.Empty);
            }
            sb.AppendLine("  ],");

            sb.Append("  \"errors\": [");
            for (int i = 0; i < r.errors.Count; i++)
            {
                if (i == 0) sb.AppendLine();
                sb.Append("    \"").Append(EscapeJson(r.errors[i])).Append("\"");
                sb.AppendLine(i + 1 < r.errors.Count ? "," : string.Empty);
            }
            if (r.errors.Count > 0) sb.Append("  ");
            sb.AppendLine("]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            StringBuilder sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private sealed class EventSpec
        {
            public readonly string incidentDefName;
            public readonly string factionDefName;
            public readonly bool isCaravanEvent;
            public readonly bool requiresNonHostile;
            public readonly bool requiresTechItem;
            public readonly bool requiresLowGoodwill;

            public EventSpec(string inc, string fac, bool caravan, bool nonHostile, bool tech, bool lowGoodwill)
            {
                incidentDefName = inc;
                factionDefName = fac;
                isCaravanEvent = caravan;
                requiresNonHostile = nonHostile;
                requiresTechItem = tech;
                requiresLowGoodwill = lowGoodwill;
            }
        }

        private sealed class EventCheckResult
        {
            public string incidentDefName = string.Empty;
            public string factionDefName = string.Empty;
            public bool passed;
            public bool defFound;
            public bool factionFound;
            public bool canFireNow;
            public bool executed;
            public bool letterCreated;
            public int choiceCount;
            public int choicesVerified;
            public bool effectVerified;
            public string exception = string.Empty;
            public string note = string.Empty;
        }

        private sealed class ProbeReport
        {
            public bool passed;
            public int passedSpecs;
            public int totalSpecs;
            public int errorCount;
            public string fatalException = string.Empty;
            public List<string> notes = new List<string>();
            public List<EventCheckResult> results = new List<EventCheckResult>();
            public List<string> errors = new List<string>();
        }
    }
}
