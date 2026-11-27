using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Automated in-game probe for Task 62 (Institute Molecular Relay: raids, retreats, local & world pad jumps, save/load).
    /// Activated via command-line flag: -tmm-institute-relay-probe
    /// Writes InstituteRelayProbe.json and InstituteRelayUiProbe.json with passed=true/false.
    /// Uses real-tick coroutine iteration with generous frame budgets (hidden game window runs few ticks per frame).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class InGameInstituteRelayProbe
    {
        private const string ProbeFlag = "-tmm-institute-relay-probe";
        private const string ReportDirArgPrefix = "-tmm-probe-dir=";
        private const string MainReportFileName = "InstituteRelayProbe.json";
        private const string UiReportFileName = "InstituteRelayUiProbe.json";
        private const string TempSaveName = "TMM_InstituteRelayProbe_MidCooldown";

        private static readonly bool Enabled;
        private static readonly bool CacheReplay;
        private static readonly string CustomReportDir;
        private static readonly object LogLock = new object();
        private static readonly List<string> CapturedErrors = new List<string>();

        private static bool started;
        private static bool completed;
        private static int readyFrames;
        private static bool capturingLogs;
        private static IEnumerator probeRoutine;

        static InGameInstituteRelayProbe()
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
                    else if (arg == "-tmm-relay-cache-replay")
                    {
                        Enabled = true;
                        CacheReplay = true;
                        LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame(TempSaveName));
                    }
                    else if (arg.StartsWith(ReportDirArgPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomReportDir = arg.Substring(ReportDirArgPrefix.Length).Trim('"');
                    }
                }
            }

            if (!Enabled) return;

            var harmony = new Harmony("turbomegamod.institute.relay.probe");
            Type cache = AccessTools.TypeByName("VEF.AestheticScaling.PawnDataCache");
            if (cache != null)
            {
                var refresh = AccessTools.DeclaredMethod(cache, "GetPawnDataCache", new[] { typeof(Pawn), typeof(bool), typeof(bool) });
                if (refresh != null)
                    harmony.Patch(refresh, finalizer: new HarmonyMethod(typeof(InGameInstituteRelayProbe), "TraceVefRefreshFailure"));
            }
            var updatePlay = AccessTools.Method(typeof(Game), "UpdatePlay"); // CHECK API
            if (updatePlay != null)
            {
                harmony.Patch(updatePlay, null, new HarmonyMethod(typeof(InGameInstituteRelayProbe), "Game_UpdatePlay_Postfix"));
            }
            else
            {
                Log.Warning("[TMM] InGameInstituteRelayProbe could not find Game.UpdatePlay.");
            }
        }

        private sealed class PawnInstanceEquality : IEqualityComparer<Pawn>
        {
            public bool Equals(Pawn left, Pawn right) { return ReferenceEquals(left, right); }
            public int GetHashCode(Pawn pawn) { return RuntimeHelpers.GetHashCode(pawn); }
        }

        private static readonly PawnInstanceEquality PawnInstances = new PawnInstanceEquality();
        private static HashSet<Pawn> beforeReloadPawns;
        private static int savedGameHash;
        private static Dictionary<Pawn, string> oldVefEntries;
        private static HashSet<Map> oldMaps;

        private static string DescribePawn(Pawn pawn)
        {
            Map held = pawn.MapHeld;
            var holder = pawn.ParentHolder;
            var holderMap = holder as Map;
            var parent = holder == null || (holderMap != null && holderMap.info == null) ? null : holder.ParentHolder;
            var index = AccessTools.Field(typeof(Thing), "mapIndexOrState");
            return "spawned=" + pawn.Spawned + " destroyed=" + pawn.Destroyed + " dead=" + pawn.Dead
                + " rawMapIndex=" + (index == null ? "?" : index.GetValue(pawn).ToString())
                + " holder=" + (holder == null ? "-" : holder.GetType().Name)
                + " parent=" + (parent == null ? "-" : parent.GetType().Name)
                + " held=" + (held == null ? "null" : RuntimeHelpers.GetHashCode(held).ToString())
                + " info=" + (held != null && held.info != null)
                + " oldMap=" + (held != null && oldMaps != null && oldMaps.Contains(held))
                + " currentMap=" + (held != null && Find.Maps.Contains(held));
        }

        private static System.Collections.IDictionary VefCache()
        {
            Type generic = AccessTools.TypeByName("VEF.AestheticScaling.DictCache`2");
            Type value = AccessTools.TypeByName("VEF.AestheticScaling.CachedPawnData");
            if (generic == null || value == null) return null;
            Type closed = generic.MakeGenericType(typeof(Pawn), value);
            var property = AccessTools.Property(closed, "Cache");
            return property == null ? null : property.GetValue(null, null) as System.Collections.IDictionary;
        }

        private static void SnapshotVefEntries()
        {
            oldMaps = new HashSet<Map>(Find.Maps);
            oldVefEntries = new Dictionary<Pawn, string>(PawnInstances);
            var entries = VefCache();
            if (entries == null) return;
            foreach (Pawn pawn in entries.Keys.OfType<Pawn>()) oldVefEntries.Add(pawn, DescribePawn(pawn));
            Log.Message("[TMM relay probe] VEF snapshot cached=" + oldVefEntries.Count + " maps=" + oldMaps.Count);
        }

        private static void AuditVefEntries(string phase)
        {
            var entries = VefCache();
            if (entries == null) return;
            var pawns = entries.Keys.OfType<Pawn>().ToArray();
            int stale = pawns.Count(pawn => oldVefEntries.ContainsKey(pawn));
            int sameIdDifferentInstance = pawns.Count(pawn => oldVefEntries.Keys.Any(oldPawn =>
                pawn.Equals(oldPawn) && !ReferenceEquals(pawn, oldPawn)));
            int oldValues = entries.Values.Cast<object>().Count(entry =>
            {
                var field = entry == null ? null : AccessTools.Field(entry.GetType(), "pawn");
                Pawn pawn = field == null ? null : field.GetValue(entry) as Pawn;
                return pawn != null && oldVefEntries.ContainsKey(pawn);
            });
            Pawn[] invalid = pawns.Where(pawn => pawn.MapHeld != null && pawn.MapHeld.info == null).ToArray();
            int spawnedInvalid = invalid.Count(pawn => pawn.SpawnedOrAnyParentSpawned);
            int liveHitsOldPawn = 0;
            foreach (Pawn live in Find.Maps.SelectMany(map => map.mapPawns.AllPawnsSpawned))
            {
                object value = entries[live];
                var field = value == null ? null : AccessTools.Field(value.GetType(), "pawn");
                Pawn cached = field == null ? null : field.GetValue(value) as Pawn;
                if (cached != null && !ReferenceEquals(cached, live) && oldVefEntries.ContainsKey(cached)) liveHitsOldPawn++;
            }
            Log.Message("[TMM relay audit] " + phase + " cache=" + pawns.Length + " oldKeys=" + stale + " oldValues=" + oldValues
                + " newInstancesWithSameId=" + sameIdDifferentInstance + " liveHitsOldPawn=" + liveHitsOldPawn
                + " invalid=" + invalid.Length + " spawnedInvalid=" + spawnedInvalid);
            foreach (Pawn pawn in invalid.Take(6))
            {
                string beforeState;
                oldVefEntries.TryGetValue(pawn, out beforeState);
                Log.Message("[TMM relay audit] invalid " + pawn.ThingID + " before=[" + beforeState
                    + "] now=[" + DescribePawn(pawn) + "]");
            }
        }

        private static void TraceVefRefreshFailure(Pawn __0, Exception __exception)
        {
            if (__exception == null || __0 == null || Current.Game == null) return;
            string oldState = null;
            bool inOldCache = oldVefEntries != null && oldVefEntries.TryGetValue(__0, out oldState);
            bool listed = Find.Maps.Any(m => m.mapPawns.AllPawnsSpawned.Any(p => ReferenceEquals(p, __0)));
            bool oldPawn = beforeReloadPawns != null && beforeReloadPawns.Contains(__0);
            Log.Message("[TMM relay probe] VEF cache exception: pawn=" + __0.ThingID
                + " afterReload=" + (savedGameHash != 0 && savedGameHash != RuntimeHelpers.GetHashCode(Current.Game))
                + " oldPawn=" + oldPawn + " listedOnCurrentMap=" + listed
                + " inOldCache=" + inOldCache + " before=[" + oldState + "] now=[" + DescribePawn(__0) + "]");
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
                probeRoutine = CacheReplay ? RunCacheReplay() : RunProbeCoroutine();
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
                Log.Error("[TMM] InGameInstituteRelayProbe unhandled coroutine exception: " + ex);
                if (CacheReplay)
                    File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "InstituteRelayCacheReloadProbe.json"),
                        "{\"passed\":false,\"checks\":[{\"name\":\"ThreeReloadsAndTicks\",\"passed\":false}]}");
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
            UiProbeReport uiReport = new UiProbeReport();
            lock (LogLock) { CapturedErrors.Clear(); }

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            capturingLogs = true;

            Exception fatal = null;
            IEnumerator inner = ExecuteChecksCoroutine(report, uiReport);

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
                uiReport.fatalException = fatal.Message;
                Log.Error("[TMM] InstituteRelayProbe fatal exception: " + fatal);
            }

            capturingLogs = false;
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;

            try
            {
                if (Environment.GetEnvironmentVariable("TMM_RELAY_KEEP_FIXTURE") != "1") CleanupTempSave();
            }
            catch
            {
                // Non-fatal cleanup.
            }

            lock (LogLock)
            {
                for (int i = 0; i < CapturedErrors.Count; i++)
                {
                    report.errors.Add(CapturedErrors[i]);
                }
            }

            report.errorCount = report.errors.Count;
            report.passed = EvaluateMainPassed(report);
            uiReport.errorCount = report.errorCount;
            uiReport.passed = EvaluateUiPassed(uiReport, report.errorCount);

            WriteReports(report, uiReport);
            Log.Message("[TMM] InstituteRelayProbe finished. passed=" + report.passed
                + ", uiPassed=" + uiReport.passed + ", errors=" + report.errorCount);
        }

        private static IEnumerator ExecuteChecksCoroutine(ProbeReport report, UiProbeReport uiReport)
        {
            Map map = Find.AnyPlayerHomeMap != null ? Find.AnyPlayerHomeMap : Find.CurrentMap;
            if (map == null)
            {
                report.notes.Add("No active colony map found.");
                yield break;
            }

            EnsureColonistCount(map, 3);

            // =====================================================================
            // 1. Research Lock / Unlock Check (TMMRelayUnlock.Unlock)
            // =====================================================================
            ResearchProjectDef relayTech = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("TMM_RelayTechnology");
            report.researchDefFound = relayTech != null;
            if (relayTech != null)
            {
                TMM_InstituteRelayManager mgr = TMM_InstituteRelayManager.Instance;
                if (mgr != null) mgr.relayTechUnlocked = false;
                report.researchLockedInitially = !relayTech.CanStartNow;
                TMMRelayUnlock.Unlock();
                report.researchUnlockedFlagSet = TMMRelayUnlock.IsUnlocked;
            }

            // =====================================================================
            // 2. Institute Relay Raid (spawns within 15..35 radius & assaults)
            // =====================================================================
            Faction institute = InstituteRelayFx.FindInstituteFaction();
            if (institute == null)
            {
                report.notes.Add("TMM_InstituteFaction not found or defeated.");
                yield break;
            }

            if (!institute.HostileTo(Faction.OfPlayer))
            {
                int delta = Math.Min(-100, institute.GoodwillToMakeHostile(Faction.OfPlayer)); // CHECK API
                institute.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null);
            }

            IncidentParms raidParms = new IncidentParms();
            raidParms.target = map;
            raidParms.faction = institute;
            raidParms.points = 1000f;
            raidParms.forced = true;
            raidParms.sendLetter = true;

            List<Pawn> colonistsBeforeRaid = new List<Pawn>(map.mapPawns.FreeColonistsSpawned);
            List<Pawn> raiders = IncidentWorker_TMMInstituteRelayRaid.ExecuteRelayRaid(raidParms);

            report.raidSpawned = raiders != null && raiders.Count > 0;
            if (report.raidSpawned)
            {
                Pawn leadRaider = raiders[0];
                float distToNearest = InstituteRelayFx.DistanceToNearestColonist(leadRaider.Position, colonistsBeforeRaid);
                report.raidDistanceToNearestColonist = distToNearest;
                // Account for +/-4 cell squad scatter around dropCenter (15..35)
                report.raidWithinRelayRadius = distToNearest >= 10f && distToNearest <= 42f;

                Lord raidLord = leadRaider.GetLord();
                report.raidAssaultLordActive = raidLord != null && raidLord.LordJob is LordJob_AssaultColony;

                // =================================================================
                // 3. Retreat Teleport Check (retreating raider vanishes with flash)
                // =================================================================
                TMM_InstituteRelayManager mgr = TMM_InstituteRelayManager.Instance;
                int retreatsBefore = mgr != null ? mgr.totalRetreatsTeleported : 0;

                if (raidLord != null)
                {
                    // Retreat the vanilla way: the raider leaves its assault lord for an exit-map lord, as a broken raid does.
                    if (leadRaider != null && leadRaider.Spawned)
                    {
                        raidLord.RemovePawn(leadRaider);
                        LordMaker.MakeNewLord(leadRaider.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog, false, false), leadRaider.Map, new List<Pawn> { leadRaider });
                    }
                }

                // Wait over real ticks (up to 400 ticks / 1500 frames) for retreat charge & relay extraction.
                EnsureUnpausedFast();
                int retreatStartTick = Find.TickManager.TicksGame;
                int retreatFrames = 0;
                while (leadRaider.Spawned
                    && (Find.TickManager.TicksGame - retreatStartTick) < 350
                    && retreatFrames < 1500)
                {
                    EnsureUnpausedFast();
                    retreatFrames++;
                    yield return null;
                }

                int retreatsAfter = mgr != null ? mgr.totalRetreatsTeleported : 0;
                report.retreatTeleported = !leadRaider.Spawned && retreatsAfter > retreatsBefore;

                // A broken raid: the lord's own panic toil, not a replaced lord.
                Pawn fleeing = raiders.FirstOrDefault(r => r != null && r != leadRaider && r.Spawned && !r.Downed && r.GetLord() != null && r.GetLord().Graph.lordToils.Any(t => t is LordToil_PanicFlee));
                if (fleeing != null)
                {
                    Lord broken = fleeing.GetLord();
                    int retreatsBeforePanic = mgr.totalRetreatsTeleported;
                    broken.GotoToil(broken.Graph.lordToils.First(t => t is LordToil_PanicFlee));
                    int panicStart = Find.TickManager.TicksGame, panicFrames = 0;
                    while (fleeing.Spawned && Find.TickManager.TicksGame - panicStart < 350 && panicFrames++ < 1500) { EnsureUnpausedFast(); yield return null; }
                    Extra(report, "PanicFleeTeleports", !fleeing.Spawned && mgr.totalRetreatsTeleported > retreatsBeforePanic && panicFrames > 0, "retreats " + retreatsBeforePanic + " -> " + mgr.totalRetreatsTeleported);
                }
                else Extra(report, "PanicFleeTeleports", false, "no second raider with a panic toil");
                KidnapChecks(report, map, leadRaider.Faction);

                // Clean up any remaining raiders so pad checks run cleanly.
                for (int i = 0; i < raiders.Count; i++)
                {
                    Pawn r = raiders[i];
                    if (r != null && !r.Destroyed && r.Spawned)
                    {
                        r.DeSpawn(DestroyMode.Vanish);
                    }
                }
            }

            // =====================================================================
            // 4. Local Relay Pad Jump (Pad A -> Pad B + Sickness + UI Gizmos)
            // =====================================================================
            ThingDef padDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_RelayPad");
            if (padDef == null)
            {
                report.notes.Add("TMM_RelayPad ThingDef not found.");
                yield break;
            }

            IntVec3 padACell = FindClear3x3Cell(map, map.Center, 18, IntVec3.Invalid);
            IntVec3 padBCell = FindClear3x3Cell(map, map.Center, 28, padACell);
            if (!padACell.IsValid || !padBCell.IsValid)
            {
                report.notes.Add("Could not find two clear 3x3 cells for relay pads.");
                yield break;
            }

            Building_TMMRelayPad padA = SpawnPoweredRelayPad(padDef, padACell, map);
            Building_TMMRelayPad padB = SpawnPoweredRelayPad(padDef, padBCell, map);
            if (padA == null || padB == null)
            {
                report.notes.Add("Failed to spawn powered relay pads.");
                yield break;
            }

            // Let power grid tick for a few real ticks so PowerOn becomes true.
            // The power net switches a lone consumer on only once in 200 ticks: a shorter wait failed by chance.
            int powerWaitUntil = Find.TickManager.TicksGame + 300;
            int powerFrames = 0;
            while ((!padA.PowerOn || !padB.PowerOn) && Find.TickManager.TicksGame < powerWaitUntil + 120 && powerFrames < 8000) // hidden window: few ticks per frame
            {
                EnsureUnpausedFast();
                powerFrames++;
                yield return null;
            }

            report.padsPowered = padA.PowerOn && padB.PowerOn;
            if (!report.padsPowered) report.notes.Add(PowerDetails("A", padA, map) + " | " + PowerDetails("B", padB, map) + " ticks=" + (Find.TickManager.TicksGame - powerWaitUntil + 300) + " frames=" + powerFrames
                + " conditions=" + string.Join(",", map.gameConditionManager.ActiveConditions.Select(c => c.def.defName).ToArray()));

            // Place a colonist onto Pad A.
            Pawn jumper = map.mapPawns.FreeColonistsSpawned[0];
            jumper.Position = padA.Position;
            jumper.Notify_Teleported(true, false);

            // Inspect UI gizmos on Pad A before jump.
            string localLabel = "TMM.Relay.GizmoLocalLabel".Translate().RawText;
            string worldLabel = "TMM.Relay.GizmoWorldLabel".Translate().RawText;
            // Off the pad: anything standing on the pad travels with the jump, which is intended, so it would hide the cost.
                EnsureSpacerComponentsAt(map, padA.Position + new IntVec3(0, 0, -6), 3);

            Command_Action localGizmo = FindPadCommand(padA, localLabel);
            Command_Action worldGizmo = FindPadCommand(padA, worldLabel);
            uiReport.localGizmoFound = localGizmo != null;
            uiReport.localGizmoEnabled = localGizmo != null && !localGizmo.Disabled;
            uiReport.worldGizmoFound = worldGizmo != null;
            uiReport.worldGizmoEnabled = worldGizmo != null && !worldGizmo.Disabled;
            uiReport.localizationResolved =
                !string.Equals(localLabel, "TMM.Relay.GizmoLocalLabel", StringComparison.Ordinal)
                && !string.Equals(worldLabel, "TMM.Relay.GizmoWorldLabel", StringComparison.Ordinal);

            string localFail;
            bool localOk = padA.TryLocalJump(padB, out localFail);
            report.localJumpSucceeded = localOk
                && jumper.Spawned
                && padB.OccupiedRect().Contains(jumper.Position)
                && padA.LocalCooldownRemaining > 0;

            if (!report.localJumpSucceeded)
            {
                report.notes.Add("Local jump failed: " + (localFail ?? "position/cooldown mismatch"));
            }

            // Verify cooldown disables the local gizmo and appears in inspect string.
            Command_Action localGizmoAfter = FindPadCommand(padA, localLabel);
            uiReport.localGizmoDisabledOnCooldown = localGizmoAfter != null && localGizmoAfter.Disabled;
            string inspectStr = padA.GetInspectString();
            uiReport.inspectShowsCooldown = !string.IsNullOrEmpty(inspectStr)
                && inspectStr.IndexOf("TMM.Relay.Inspect", StringComparison.Ordinal) < 0;

            // =====================================================================
            // 5. Failed World Jump (impassable/out-of-range -> 0 cost deducted)
            // =====================================================================
            Pawn worldJumper = map.mapPawns.FreeColonistsSpawned.Count > 1
                ? map.mapPawns.FreeColonistsSpawned[1]
                : jumper;
            worldJumper.Position = padA.Position;
            worldJumper.Notify_Teleported(true, false);

            int spacerBeforeFail = padA.CountAvailableSpacerComponents();
            string failReason;
            Caravan failedCaravan = padA.TryWorldJump(PlanetTile.Invalid, out failReason);
            int spacerAfterFail = padA.CountAvailableSpacerComponents();

            report.failedJumpBlocked = failedCaravan == null && !string.IsNullOrEmpty(failReason);
            report.failedJumpNoResourceDeducted = spacerAfterFail == spacerBeforeFail && worldJumper.Spawned;

            // =====================================================================
            // 6. Successful World Jump (caravan created at target tile, cost deducted ONCE)
            // =====================================================================
            PlanetTile validDestTile = FindPassableWorldTileInRange(map.Tile, 5, Building_TMMRelayPad.MaxWorldJumpTiles - 5);
            if (!validDestTile.Valid)
            {
                report.notes.Add("Could not find a passable world tile within 60 tiles.");
            }
            else
            {
                // Place a haulable steel stack on Pad A to verify cargo transfer too.
                Thing padSteel = ThingMaker.MakeThing(ThingDefOf.Steel);
                padSteel.stackCount = 25;
                // Place a separate stack, because GenPlace may merge it and destroy the test's reference.
                GenSpawn.Spawn(padSteel, padA.Position, map);
                if (!padSteel.Spawned || padSteel.Destroyed || padSteel.stackCount != 25)
                    throw new InvalidOperationException("The relay fixture has no independent steel stack.");

                IntVec3 pawnCell = worldJumper.Position;
                IntVec3 steelCell = padSteel.Position;
                int originalComponentCount = padA.CountAvailableSpacerComponents();
                int caravansBefore = Find.WorldObjects.Caravans.Count;
                foreach (string injected in new[] { "pawn", "caravan", "cargo" })
                {
                    Building_TMMRelayPad.TestWorldJumpFailure = injected;
                    Caravan rejected;
                    try { rejected = padA.TryWorldJump(validDestTile, out failReason); }
                    finally { Building_TMMRelayPad.TestWorldJumpFailure = null; }
                    bool reverted = rejected == null && !failReason.NullOrEmpty() && padA.WorldCooldownRemaining == 0 &&
                        worldJumper.Spawned && worldJumper.Map == map && worldJumper.Position == pawnCell &&
                        !padSteel.Destroyed && padSteel.Spawned && padSteel.Position == steelCell && padSteel.stackCount == 25 &&
                        padA.CountAvailableSpacerComponents() == originalComponentCount &&
                        Find.WorldObjects.Caravans.Count == caravansBefore;
                    report.failedJumpNoResourceDeducted &= reverted;
                    report.notes.Add("World relay rollback " + injected + ": " + reverted
                        + " pawn=" + worldJumper.Spawned + "/" + worldJumper.Position
                        + " steel=" + padSteel.Spawned + "/" + padSteel.Destroyed + "/" + padSteel.stackCount + "/" + padSteel.Position
                        + " components=" + padA.CountAvailableSpacerComponents() + "/" + originalComponentCount
                        + " caravans=" + Find.WorldObjects.Caravans.Count + "/" + caravansBefore);
                }

                int spacerBeforeJump = padA.CountAvailableSpacerComponents();
                string worldFail;
                Caravan createdCaravan = padA.TryWorldJump(validDestTile, out worldFail);
                int spacerAfterJump = padA.CountAvailableSpacerComponents();

                report.worldJumpCaravanCreated = createdCaravan != null
                    && createdCaravan.Spawned
                    && createdCaravan.Tile == validDestTile
                    && createdCaravan.PawnsListForReading.Contains(worldJumper)
                    && padSteel.ParentHolder is Pawn_InventoryTracker
                    && createdCaravan.ContainsPawn(((Pawn_InventoryTracker)padSteel.ParentHolder).pawn)
                    && padSteel.stackCount == 25;
                report.worldJumpCostDeductedOnce = (spacerBeforeJump - spacerAfterJump) == Building_TMMRelayPad.WorldJumpSpacerComponentCost;
                report.worldJumpCooldownSet = padA.WorldCooldownRemaining > 0;

                if (!report.worldJumpCaravanCreated || !report.worldJumpCostDeductedOnce)
                {
                    report.notes.Add("World jump mismatch: reason=" + (worldFail ?? "none")
                        + ", spacerBefore=" + spacerBeforeJump + ", spacerAfter=" + spacerAfterJump);
                }

                // Clean up temporary world caravan so save stays tidy.
                if (createdCaravan != null && createdCaravan.Spawned && Find.WorldObjects != null)
                {
                    Find.WorldObjects.Remove(createdCaravan);
                }
            }

            // =====================================================================
            // 7. Save & Reload mid-cooldown — cooldowns & unlock state must persist
            // =====================================================================
            // An Institute caravan charging its jump is saved in the middle of it.
            WorldTraffic traffic = Find.World.GetComponent<WorldTraffic>();
            Faction jumpFaction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => f.def.defName == "TMM_InstituteFaction");
            WorldTrafficRoute jumping = null;
            List<Settlement> bases = Find.WorldObjects.Settlements.Where(s => s.Faction == jumpFaction && !s.HasMap && s.Tile.Layer == Find.WorldGrid.Surface).ToList();
            foreach (Settlement from in bases)
            {
                foreach (Settlement to in Find.WorldObjects.Settlements.Where(s => s != from && s.Faction != null && !s.Faction.IsPlayer && !s.HasMap && s.Tile.Layer == from.Tile.Layer && !s.Faction.HostileTo(jumpFaction)))
                    if (jumping == null && Find.World.reachability.CanReach(from.Tile, to.Tile)) jumping = traffic.TryStart(from, to, false);
                if (jumping != null) break;
            }
            int jumpFrames = 0;
            while (jumping != null && jumping.relayJumpAt <= 0 && jumping.caravan != null && jumping.caravan.Spawned && jumpFrames++ < 3000) { EnsureUnpausedFast(); yield return null; }
            bool charging = jumping != null && jumping.relayJumpAt > 0;
            int jumpCaravanId = charging ? jumping.caravan.ID : -1, jumpAt = charging ? jumping.relayJumpAt : 0;
            string jumpCargo = charging ? Cargo(jumping.caravan) : "";
            Extra(report, "InstituteCaravanCharges", charging, jumping == null ? "no route" : "relayJumpAt=" + jumping.relayJumpAt + " cargo=" + jumpCargo.Length);
            string victimId = kidnappedVictim == null ? "" : kidnappedVictim.ThingID;

            int savedPadId = padA.thingIDNumber;
            int savedLocalCd = padA.localCooldownUntilTick;
            int savedWorldCd = padA.worldCooldownUntilTick;
            int prevGameHash = RuntimeHelpers.GetHashCode(Current.Game);

            bool saveOk = false;
            try
            {
                GameDataSaveLoader.SaveGame(TempSaveName); // CHECK API
                saveOk = true;
            }
            catch (Exception ex)
            {
                report.notes.Add("SaveGame failed: " + ex.Message);
            }

            if (saveOk)
            {
                for (int f = 0; f < 10; f++) yield return null;

                bool loadOk = false;
                try
                {
                    beforeReloadPawns = new HashSet<Pawn>(Find.Maps.SelectMany(currentMap => currentMap.mapPawns.AllPawnsSpawned), PawnInstances);
                    SnapshotVefEntries();
                    savedGameHash = prevGameHash;
                    GameDataSaveLoader.LoadGame(TempSaveName); // CHECK API
                    loadOk = true;
                }
                catch (Exception ex)
                {
                    report.notes.Add("LoadGame failed: " + ex.Message);
                }

                if (loadOk)
                {
                    int waitLoadFrames = 0;
                    while (RuntimeHelpers.GetHashCode(Current.Game) == prevGameHash && waitLoadFrames < 2400)
                    {
                        waitLoadFrames++;
                        yield return null;
                    }
                    for (int f = 0; f < 25; f++) yield return null;

                    AuditVefEntries("first reload");
                    report.saveLoadCompleted = RuntimeHelpers.GetHashCode(Current.Game) != prevGameHash;
                    report.reloadUnlockPersisted = TMMRelayUnlock.IsUnlocked;

                    Map reloadedMap = Find.AnyPlayerHomeMap != null ? Find.AnyPlayerHomeMap : Find.CurrentMap;
                    Building_TMMRelayPad reloadedPad = FindPadById(reloadedMap, savedPadId);
                    report.reloadCooldownPersisted = reloadedPad != null
                        && reloadedPad.localCooldownUntilTick == savedLocalCd
                        && reloadedPad.worldCooldownUntilTick == savedWorldCd
                        && reloadedPad.WorldCooldownRemaining > 0;

                    Faction instituteAfter = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => f.def.defName == "TMM_InstituteFaction");
                    Extra(report, "KidnappedAfterLoad", victimId.Length > 0 && instituteAfter.kidnapped.KidnappedPawnsListForReading.Count(p => p.ThingID == victimId) == 1, "victim=" + victimId);
                    WorldTrafficRoute again = Find.World.GetComponent<WorldTraffic>().routes.FirstOrDefault(r => r.caravan != null && r.caravan.ID == jumpCaravanId);
                    bool sameState = again != null && Cargo(again.caravan) == jumpCargo && (again.relayJumpAt == jumpAt || Find.TickManager.TicksGame >= jumpAt);
                    Extra(report, "JumpStateAfterLoad", sameState, again == null ? "route gone" : "relayJumpAt " + jumpAt + " -> " + again.relayJumpAt + " tick=" + Find.TickManager.TicksGame);
                    PlanetTile wanted = again == null || again.destination == null ? PlanetTile.Invalid : again.destination.Tile;
                    Caravan relayCaravan = again == null ? null : again.caravan;
                    bool landed = false;
                    string landedCargo = "";
                    int landFrames = 0;
                    while (relayCaravan != null && !landed && relayCaravan.Spawned && landFrames++ < 4000)
                    {
                        if (relayCaravan.Tile == wanted) { landed = true; landedCargo = Cargo(relayCaravan); break; }
                        EnsureUnpausedFast();
                        yield return null;
                    }
                    Extra(report, "SameCaravanLandsWithCargo", landed && relayCaravan.ID == jumpCaravanId && landedCargo == jumpCargo, "landed=" + landed + (landedCargo == jumpCargo ? "" : " cargo " + jumpCargo + " -> " + landedCargo));
                }
            }
        }

        private static IEnumerator RunCacheReplay()
        {
            int startTick = Find.TickManager.TicksGame;
            for (int frame = 0; Find.TickManager.TicksGame - startTick < 6500 && frame < 4800; frame++)
            {
                EnsureUnpausedFast();
                yield return null;
            }
            if (Find.TickManager.TicksGame - startTick < 6500)
                throw new InvalidOperationException("The saved relay fixture did not advance before the replay.");

            for (int cycle = 1; cycle <= 3; cycle++)
            {
                beforeReloadPawns = new HashSet<Pawn>(Find.Maps.SelectMany(currentMap => currentMap.mapPawns.AllPawnsSpawned), PawnInstances);
                SnapshotVefEntries();
                Game before = Current.Game;
                savedGameHash = RuntimeHelpers.GetHashCode(before);
                if (Environment.GetEnvironmentVariable("TMM_RELAY_RESET_VEF_CACHE") == "1")
                {
                    var entries = VefCache();
                    if (entries != null) entries.Clear();
                    Log.Message("[TMM relay audit] cleared old VEF cache before reload " + cycle);
                }
                GameDataSaveLoader.LoadGame(TempSaveName);
                for (int frame = 0; ReferenceEquals(Current.Game, before) && frame < 2400; frame++) yield return null;
                if (ReferenceEquals(Current.Game, before)) throw new InvalidOperationException("Repeated relay save load did not complete.");
                AuditVefEntries("reload " + cycle + " start");
                startTick = Find.TickManager.TicksGame;
                for (int frame = 0; Find.TickManager.TicksGame - startTick < 7500 && frame < 4800; frame++)
                {
                    EnsureUnpausedFast();
                    yield return null;
                }
                if (Find.TickManager.TicksGame - startTick < 7500)
                    throw new InvalidOperationException("The saved relay fixture did not advance after reload " + cycle + ".");
                AuditVefEntries("reload " + cycle + " end");
            }

            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "InstituteRelayCacheReloadProbe.json"),
                "{\"passed\":true,\"checks\":[{\"name\":\"ThreeReloadsAndTicks\",\"passed\":true}]}");
        }

        private static Pawn kidnappedVictim;

        private static void Extra(ProbeReport report, string name, bool passed, string detail)
        {
            report.extra.Add(name + ": " + (passed ? "ok" : "FAIL") + " " + detail);
        }

        private static string Cargo(Caravan caravan)
        {
            return string.Join(";", caravan.PawnsListForReading.OrderBy(p => p.thingIDNumber).Select(p => p.ThingID + "[" + string.Join(",", p.inventory.innerContainer.InnerListForReading.OrderBy(t => t.thingIDNumber).Select(t => t.ThingID + "x" + t.stackCount).ToArray()) + "]").ToArray());
        }

        /// A real captor carrying a real colonist: refused separation, refused departure, then a completed abduction.
        private static void KidnapChecks(ProbeReport report, Map map, Faction institute)
        {
            Pawn victim = FactionEventHelper.GenerateFactionPawn(Faction.OfPlayer, map, null, PawnKindDefOf.Colonist);
            Pawn captor = null;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Pawn candidate = FactionEventHelper.GenerateFactionPawn(institute, map, null, PawnKindDefOf.Colonist);
                if (candidate == null) continue;
                if (candidate.carryTracker != null && candidate.carryTracker.AvailableStackSpace(victim.def) >= 1)
                {
                    captor = candidate;
                    break;
                }
                candidate.Destroy(DestroyMode.Vanish);
            }
            if (captor == null) throw new InvalidOperationException("The relay fixture could not generate a carrier for the victim.");
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);
            GenSpawn.Spawn(captor, cell, map);
            GenSpawn.Spawn(victim, CellFinder.RandomClosewalkCellNear(cell, map, 3), map);
            victim.health.AddHediff(HediffDefOf.Anesthetic);
            Lord lord = LordMaker.MakeNewLord(institute, new LordJob_AssaultColony(institute), map, new List<Pawn> { captor });
            Func<bool> listed = () => institute.kidnapped.KidnappedPawnsListForReading.Contains(victim);

            bool carried = captor.carryTracker.TryStartCarry(victim, 1, false) == 1; // the count overload takes the pawn off the map first, as the kidnap job does
            InstituteRelayFx.TestNpcTeleportFailure = "drop";
            bool went = InstituteRelayFx.TeleportNpcOffMap(captor, institute);
            InstituteRelayFx.TestNpcTeleportFailure = null;
            Extra(report, "RefusedSeparationKeepsVictim", carried && !went && captor.Spawned && captor.carryTracker.CarriedThing == victim && !listed() && captor.GetLord() == lord, "carried=" + carried + " went=" + went);

            InstituteRelayFx.TestNpcTeleportFailure = "leave";
            went = InstituteRelayFx.TeleportNpcOffMap(captor, institute);
            InstituteRelayFx.TestNpcTeleportFailure = null;
            Extra(report, "RefusedDepartureLeavesBothOnMap", !went && captor.Spawned && captor.Map == map && victim.Spawned && victim.Map == map && !listed() && !Find.WorldPawns.Contains(victim) && !Find.WorldPawns.Contains(captor) && captor.GetLord() == lord,
                "went=" + went + " captor=" + captor.Spawned + " victim=" + victim.Spawned + " lord=" + (captor.GetLord() == lord));

            carried = captor.carryTracker.CarriedThing == victim || captor.carryTracker.TryStartCarry(victim, 1, false) == 1;
            went = InstituteRelayFx.TeleportNpcOffMap(captor, institute);
            Extra(report, "AbductionCompletes", carried && went && !captor.Spawned && !victim.Spawned && !victim.Destroyed && listed() && Find.WorldPawns.Contains(victim), "carried=" + carried + " went=" + went + " listed=" + listed());
            kidnappedVictim = listed() ? victim : null;
        }

        private static string PowerDetails(string name, Building_TMMRelayPad pad, Map map)
        {
            CompPowerTrader power = pad.GetComp<CompPowerTrader>();
            string text = name + ": on=" + pad.PowerOn + " spawned=" + pad.Spawned + " net=" + (power != null && power.PowerNet != null ? power.PowerNet.CurrentEnergyGainRate().ToString("F3") + " of " + power.PowerNet.powerComps.Count : "none")
                + " flick=" + FlickUtility.WantsToBeOn(pad) + " broken=" + pad.IsBrokenDown() + " cell=" + pad.Position + " terrain=" + pad.Position.GetTerrain(map).defName;
            foreach (IntVec3 c in pad.OccupiedRect().ExpandedBy(2)) if (c.InBounds(map)) { Building b = c.GetEdifice(map); if (b != null && b != pad && text.IndexOf(b.ThingID, StringComparison.Ordinal) < 0) { CompPowerTrader o = b.GetComp<CompPowerTrader>(); text += " " + b.ThingID + (o != null ? " on=" + o.PowerOn + " out=" + o.PowerOutput : ""); } }
            return text;
        }

        private static Building_TMMRelayPad SpawnPoweredRelayPad(ThingDef padDef, IntVec3 center, Map map)
        {
            Building_TMMRelayPad pad = ThingMaker.MakeThing(padDef) as Building_TMMRelayPad;
            if (pad == null) return null;
            pad.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(pad, center, map, Rot4.North);

            // Spawn an adjacent vanometric/solar/wood power plant (chemfuel/vanometric preferred for instant power).
            ThingDef genDef = DefDatabase<ThingDef>.GetNamedSilentFail("VanometricPowerCell");
            if (genDef == null) genDef = DefDatabase<ThingDef>.GetNamedSilentFail("WoodFiredGenerator");
            if (genDef != null)
            {
                IntVec3 genCell = new IntVec3(center.x + 2, 0, center.z);
                if (!genCell.InBounds(map) || genCell.GetEdifice(map) != null)
                {
                    genCell = new IntVec3(center.x - 2, 0, center.z);
                }
                if (genCell.InBounds(map))
                {
                    Building gen = ThingMaker.MakeThing(genDef) as Building;
                    if (gen != null)
                    {
                        gen.SetFaction(Faction.OfPlayer);
                        GenSpawn.Spawn(gen, genCell, map, Rot4.North);
                        CompRefuelable refuel = gen.GetComp<CompRefuelable>();
                        if (refuel != null) refuel.Refuel(refuel.Props.fuelCapacity);
                    }
                }
            }

            return pad;
        }

        private static IntVec3 FindClear3x3Cell(Map map, IntVec3 near, int radius, IntVec3 avoid)
        {
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(near, map, radius,
                delegate(IntVec3 c)
                {
                    if (avoid.IsValid && c.DistanceTo(avoid) < 7f) return false;
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dz = -2; dz <= 2; dz++)
                        {
                            IntVec3 t = new IntVec3(c.x + dx, 0, c.z + dz);
                            if (!t.InBounds(map) || t.Fogged(map) || !t.Standable(map) || t.GetEdifice(map) != null)
                            {
                                return false;
                            }
                            TerrainDef terr = t.GetTerrain(map);
                            if (terr != null && terr.IsWater) return false;
                        }
                    }
                    return true;
                },
                out found))
            {
                return found;
            }
            return IntVec3.Invalid;
        }

        private static PlanetTile FindPassableWorldTileInRange(PlanetTile origin, int minDist, int maxDist)
        {
            if (Find.World == null || Find.WorldGrid == null || !origin.Valid) return PlanetTile.Invalid;
            int totalTiles = Find.WorldGrid.TilesCount;
            int startIdx = Math.Max(0, origin.tileId - 400);
            int endIdx = Math.Min(totalTiles - 1, origin.tileId + 400);

            for (int id = startIdx; id <= endIdx; id++)
            {
                PlanetTile candidate = new PlanetTile(id, origin.Layer); // CHECK API
                if (!candidate.Valid || candidate == origin) continue;
                if (Find.World.Impassable(candidate)) continue;
                int dist = Find.WorldGrid.TraversalDistanceBetween(origin, candidate, true, maxDist + 1); // CHECK API
                if (dist >= minDist && dist <= maxDist)
                {
                    return candidate;
                }
            }
            return PlanetTile.Invalid;
        }

        private static void EnsureSpacerComponentsAt(Map map, IntVec3 cell, int count)
        {
            Thing comp = ThingMaker.MakeThing(ThingDefOf.ComponentSpacer);
            comp.stackCount = count;
            GenPlace.TryPlaceThing(comp, cell, map, ThingPlaceMode.Near);
        }

        private static Command_Action FindPadCommand(Building_TMMRelayPad pad, string label)
        {
            if (pad == null || string.IsNullOrEmpty(label)) return null;
            foreach (Gizmo g in pad.GetGizmos())
            {
                Command_Action cmd = g as Command_Action;
                if (cmd != null && string.Equals(cmd.defaultLabel, label, StringComparison.Ordinal))
                {
                    return cmd;
                }
            }
            return null;
        }

        private static Building_TMMRelayPad FindPadById(Map map, int thingId)
        {
            if (map == null || map.listerBuildings == null) return null;
            List<Building> list = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < list.Count; i++)
            {
                Building_TMMRelayPad pad = list[i] as Building_TMMRelayPad;
                if (pad != null && pad.thingIDNumber == thingId) return pad;
            }
            return null;
        }

        private static void EnsureColonistCount(Map map, int minCount)
        {
            while (map.mapPawns.FreeColonistsSpawnedCount < minCount)
            {
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer, map.Tile,
                    true, false, false, false, true, 1f, false, true, false, false,
                    false, false, false, false, false, 0f, 0f, null, 1f,
                    null, null, null, null, null, null, null, null, null, null, null, null,
                    false, false, false, false, null, null, null, null, null, 0f,
                    DevelopmentalStage.Adult)); // CHECK API
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

        private static void EnsureUnpausedFast()
        {
            if (Find.TickManager == null) return;
            if (Find.TickManager.Paused) Find.TickManager.TogglePaused();
            if (Find.TickManager.CurTimeSpeed != TimeSpeed.Ultrafast)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
            }
        }

        private static void CleanupTempSave()
        {
            string path = GenFilePaths.FilePathForSavedGame(TempSaveName); // CHECK API
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static bool EvaluateMainPassed(ProbeReport r)
        {
            return string.IsNullOrEmpty(r.fatalException)
                && r.errorCount == 0
                && r.extra.Count >= 8 && !r.extra.Any(x => x.Contains(": FAIL"))
                && r.researchDefFound
                && r.researchLockedInitially
                && r.researchUnlockedFlagSet
                && r.raidSpawned
                && r.raidWithinRelayRadius
                && r.raidAssaultLordActive
                && r.retreatTeleported
                && r.padsPowered
                && r.localJumpSucceeded
                && r.failedJumpBlocked
                && r.failedJumpNoResourceDeducted
                && r.worldJumpCaravanCreated
                && r.worldJumpCostDeductedOnce
                && r.worldJumpCooldownSet
                && r.saveLoadCompleted
                && r.reloadUnlockPersisted
                && r.reloadCooldownPersisted;
        }

        private static bool EvaluateUiPassed(UiProbeReport ui, int errorCount)
        {
            return string.IsNullOrEmpty(ui.fatalException)
                && errorCount == 0
                && ui.localGizmoFound
                && ui.localGizmoEnabled
                && ui.worldGizmoFound
                && ui.worldGizmoEnabled
                && ui.localGizmoDisabledOnCooldown
                && ui.inspectShowsCooldown
                && ui.localizationResolved;
        }

        private static string ResolveReportDirectory()
        {
            if (!string.IsNullOrEmpty(CustomReportDir)) return CustomReportDir;
            string envDir = Environment.GetEnvironmentVariable("TMM_PROBE_DIR");
            if (!string.IsNullOrEmpty(envDir)) return envDir;
            return GenFilePaths.SaveDataFolderPath;
        }

        private static void WriteReports(ProbeReport report, UiProbeReport uiReport)
        {
            try
            {
                string dir = ResolveReportDirectory();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, MainReportFileName), SerializeMain(report), Encoding.UTF8);
                File.WriteAllText(Path.Combine(dir, UiReportFileName), SerializeUi(uiReport), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] Failed to write InstituteRelayProbe reports: " + ex);
            }
        }

        private static string SerializeMain(ProbeReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            AppendBool(sb, "passed", r.passed, true);
            AppendBool(sb, "researchDefFound", r.researchDefFound, true);
            AppendBool(sb, "researchLockedInitially", r.researchLockedInitially, true);
            AppendBool(sb, "researchUnlockedFlagSet", r.researchUnlockedFlagSet, true);
            AppendBool(sb, "raidSpawned", r.raidSpawned, true);
            AppendFloat(sb, "raidDistanceToNearestColonist", r.raidDistanceToNearestColonist, true);
            AppendBool(sb, "raidWithinRelayRadius", r.raidWithinRelayRadius, true);
            AppendBool(sb, "raidAssaultLordActive", r.raidAssaultLordActive, true);
            AppendBool(sb, "retreatTeleported", r.retreatTeleported, true);
            AppendBool(sb, "padsPowered", r.padsPowered, true);
            AppendBool(sb, "localJumpSucceeded", r.localJumpSucceeded, true);
            AppendBool(sb, "failedJumpBlocked", r.failedJumpBlocked, true);
            AppendBool(sb, "failedJumpNoResourceDeducted", r.failedJumpNoResourceDeducted, true);
            AppendBool(sb, "worldJumpCaravanCreated", r.worldJumpCaravanCreated, true);
            AppendBool(sb, "worldJumpCostDeductedOnce", r.worldJumpCostDeductedOnce, true);
            AppendBool(sb, "worldJumpCooldownSet", r.worldJumpCooldownSet, true);
            AppendBool(sb, "saveLoadCompleted", r.saveLoadCompleted, true);
            AppendBool(sb, "reloadUnlockPersisted", r.reloadUnlockPersisted, true);
            AppendBool(sb, "reloadCooldownPersisted", r.reloadCooldownPersisted, true);
            AppendInt(sb, "errorCount", r.errorCount, true);
            AppendString(sb, "fatalException", r.fatalException, true);
            AppendList(sb, "extra", r.extra, true);
            AppendList(sb, "notes", r.notes, true);
            AppendList(sb, "errors", r.errors, false);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string SerializeUi(UiProbeReport ui)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            AppendBool(sb, "passed", ui.passed, true);
            AppendBool(sb, "localGizmoFound", ui.localGizmoFound, true);
            AppendBool(sb, "localGizmoEnabled", ui.localGizmoEnabled, true);
            AppendBool(sb, "worldGizmoFound", ui.worldGizmoFound, true);
            AppendBool(sb, "worldGizmoEnabled", ui.worldGizmoEnabled, true);
            AppendBool(sb, "localGizmoDisabledOnCooldown", ui.localGizmoDisabledOnCooldown, true);
            AppendBool(sb, "inspectShowsCooldown", ui.inspectShowsCooldown, true);
            AppendBool(sb, "localizationResolved", ui.localizationResolved, true);
            AppendInt(sb, "errorCount", ui.errorCount, true);
            AppendString(sb, "fatalException", ui.fatalException, false);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void AppendBool(StringBuilder sb, string k, bool v, bool comma)
        {
            sb.Append("  \"").Append(Escape(k)).Append("\": ").Append(v ? "true" : "false").AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendInt(StringBuilder sb, string k, int v, bool comma)
        {
            sb.Append("  \"").Append(Escape(k)).Append("\": ").Append(v.ToString(CultureInfo.InvariantCulture)).AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendFloat(StringBuilder sb, string k, float v, bool comma)
        {
            sb.Append("  \"").Append(Escape(k)).Append("\": ").Append(v.ToString("0.##", CultureInfo.InvariantCulture)).AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendString(StringBuilder sb, string k, string v, bool comma)
        {
            sb.Append("  \"").Append(Escape(k)).Append("\": \"").Append(Escape(v ?? string.Empty)).Append("\"").AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendList(StringBuilder sb, string k, List<string> list, bool comma)
        {
            sb.Append("  \"").Append(Escape(k)).Append("\": [");
            if (list != null && list.Count > 0)
            {
                sb.AppendLine();
                for (int i = 0; i < list.Count; i++)
                {
                    sb.Append("    \"").Append(Escape(list[i] ?? string.Empty)).Append("\"").AppendLine(i + 1 < list.Count ? "," : string.Empty);
                }
                sb.Append("  ]");
            }
            else
            {
                sb.Append("]");
            }
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static string Escape(string s)
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

        private sealed class ProbeReport
        {
            public readonly List<string> extra = new List<string>();
            public bool passed;
            public bool researchDefFound;
            public bool researchLockedInitially;
            public bool researchUnlockedFlagSet;
            public bool raidSpawned;
            public float raidDistanceToNearestColonist;
            public bool raidWithinRelayRadius;
            public bool raidAssaultLordActive;
            public bool retreatTeleported;
            public bool padsPowered;
            public bool localJumpSucceeded;
            public bool failedJumpBlocked;
            public bool failedJumpNoResourceDeducted;
            public bool worldJumpCaravanCreated;
            public bool worldJumpCostDeductedOnce;
            public bool worldJumpCooldownSet;
            public bool saveLoadCompleted;
            public bool reloadUnlockPersisted;
            public bool reloadCooldownPersisted;
            public int errorCount;
            public string fatalException = string.Empty;
            public List<string> notes = new List<string>();
            public List<string> errors = new List<string>();
        }

        private sealed class UiProbeReport
        {
            public bool passed;
            public bool localGizmoFound;
            public bool localGizmoEnabled;
            public bool worldGizmoFound;
            public bool worldGizmoEnabled;
            public bool localGizmoDisabledOnCooldown;
            public bool inspectShowsCooldown;
            public bool localizationResolved;
            public int errorCount;
            public string fatalException = string.Empty;
        }
    }
}
