using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Automated in-game probe for Task 60 (Vehicle Raids & Peaceful Truck Visits).
    /// Activated via command-line flag: -tmm-vehicle-raid-probe
    /// Uses an IEnumerator stepped on real Game.UpdatePlay frames/ticks because RimWorld 1.7
    /// vehicle pathfinding runs asynchronously and cannot advance inside a synchronous tick loop.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class InGameVehicleRaidProbe
    {
        private const string ProbeFlag = "-tmm-vehicle-raid-probe";
        private const string ReportDirArgPrefix = "-tmm-probe-dir=";
        private const string ReportFileName = "VehicleRaidProbe.json";
        private const string TempSaveName = "TMM_VehicleRaidProbe_MidRaid";

        private static readonly bool Enabled;
        private static readonly string CustomReportDir;
        private static readonly object LogLock = new object();
        private static readonly List<string> CapturedErrors = new List<string>();

        private static bool started;
        private static bool completed;
        private static int readyFrames;
        private static bool capturingLogs;
        private static IEnumerator probeRoutine;

        static InGameVehicleRaidProbe()
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
                    else if (arg.StartsWith(ReportDirArgPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomReportDir = arg.Substring(ReportDirArgPrefix.Length).Trim('"');
                    }
                }
            }

            if (!Enabled) return;

            var harmony = new Harmony("turbomegamod.vehicleraids.probe");
            var updatePlay = AccessTools.Method(typeof(Game), "UpdatePlay"); // CHECK API
            if (updatePlay != null)
            {
                harmony.Patch(updatePlay, null, new HarmonyMethod(typeof(InGameVehicleRaidProbe), "Game_UpdatePlay_Postfix"));
            }
            else
            {
                Log.Warning("[TMM] InGameVehicleRaidProbe could not find Game.UpdatePlay.");
            }
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
                probeRoutine = RunProbeCoroutine();
            }

            if (probeRoutine == null) return;

            try
            {
                bool hasMore = probeRoutine.MoveNext();
                if (!hasMore)
                {
                    probeRoutine = null;
                    completed = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] InGameVehicleRaidProbe unhandled coroutine exception: " + ex);
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

            lock (LogLock)
            {
                CapturedErrors.Clear();
            }

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            capturingLogs = true;

            Exception fatal = null;
            IEnumerator inner = ExecuteChecksCoroutine(report);

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
                Log.Error("[TMM] VehicleRaidProbe exception: " + fatal);
            }

            capturingLogs = false;
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;

            try
            {
                if (TradeSession.Active) TradeSession.Close();
                CleanupTempSaveFile();
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
            report.passed = EvaluatePassed(report);
            WriteReport(report);

            Log.Message("[TMM] VehicleRaidProbe finished. passed=" + report.passed + ", errors=" + report.errorCount);
        }

        private static IEnumerator ExecuteChecksCoroutine(ProbeReport report)
        {
            report.vfAvailable = VfBridge.IsAvailable;
            if (!report.vfAvailable)
            {
                report.notes.Add("Vehicle Framework is not loaded/available.");
                yield break;
            }

            Map map = Find.AnyPlayerHomeMap != null ? Find.AnyPlayerHomeMap : Find.CurrentMap;
            if (map == null)
            {
                report.notes.Add("No active colony map found.");
                yield break;
            }

            EnsureColonyHasColonists(map, 2);
            IntVec3 colonyCenter = VehicleRaidPlacement.FindColonyCenter(map);

            // =====================================================================
            // PART 1: Gunner 2000-pt Vehicle Raid (spawn -> async drive -> dismount)
            // =====================================================================
            Faction gunners = FindFactionByDefName("TMM_GunnersFaction");
            if (gunners == null)
            {
                report.notes.Add("Faction TMM_GunnersFaction not found in world.");
                yield break;
            }

            if (!gunners.HostileTo(Faction.OfPlayer))
            {
                int delta = Math.Min(-100, gunners.GoodwillToMakeHostile(Faction.OfPlayer)); // CHECK API
                gunners.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null);
            }

            float requestedPoints = 2000f;
            IncidentParms raidParms = new IncidentParms();
            raidParms.target = map;
            raidParms.faction = gunners;
            raidParms.points = requestedPoints;
            raidParms.forced = true;
            raidParms.sendLetter = true;

            raidParms.points = 100f;
            VehicleMission insufficient = IncidentWorker_TMMVehicleRaid.ExecuteVehicleRaid(raidParms);
            report.insufficientBudgetRejected = insufficient == null;
            if (insufficient != null) CleanupMissionForNextPhase(insufficient);
            raidParms.points = requestedPoints;

            VehicleMission raidMission = IncidentWorker_TMMVehicleRaid.ExecuteVehicleRaid(raidParms);
            if (raidMission == null || raidMission.PrimaryVehicle == null)
            {
                report.notes.Add("ExecuteVehicleRaid returned null or spawned no vehicles.");
                yield break;
            }

            Pawn raidVehicle = raidMission.PrimaryVehicle;
            report.raidVehicleDefName = raidVehicle.def != null ? raidVehicle.def.defName : string.Empty;
            report.raidVehicleCount = raidMission.vehicles.Count;
            report.raidSpawnedAtEdge = raidVehicle.Spawned && raidVehicle.Position.DistanceToEdge(map) <= 16;

            int aboardAtSpawn = 0;
            for (int i = 0; i < raidMission.vehicles.Count; i++)
            {
                aboardAtSpawn += VfBridge.GetAllPawnsAboard(raidMission.vehicles[i]).Count;
            }
            report.raidCrewAboardAtSpawn = aboardAtSpawn;
            report.raidHasCrewAtSpawn = raidMission.vehicles.All(v => VfBridge.GetAllPawnsAboard(v).Count >= 2);

            // Compute total effective raid points = vehicle points spent + crew combatPower.
            float pawnCombatPowerSum = 0f;
            for (int i = 0; i < raidMission.crew.Count; i++)
            {
                Pawn c = raidMission.crew[i];
                if (c != null && c.kindDef != null)
                {
                    pawnCombatPowerSum += c.kindDef.combatPower;
                }
            }
            report.raidRequestedPoints = requestedPoints;
            report.raidVehiclePointsSpent = raidMission.vehiclePointsSpent;
            report.raidPawnCombatPowerSum = pawnCombatPowerSum;
            report.raidTotalEffectivePoints = raidMission.vehiclePointsSpent + pawnCombatPowerSum;
            report.raidPointsBudgetReasonable = report.raidVehiclePointsSpent <= requestedPoints * 0.45f &&
                report.raidTotalEffectivePoints <= requestedPoints;

            float initialDistToColony = raidVehicle.Position.DistanceTo(colonyCenter);
            float minDistToColony = initialDistToColony;
            report.raidInitialDistToColony = initialDistToColony;

            // Step real game ticks (async VF pathfinding requires real frames + real ticks).
            EnsureUnpausedFast();
            int raidWaitStartTick = Find.TickManager.TicksGame;
            int raidFrames = 0;
            const int MaxRaidWaitTicks = 4800;
            const int MaxRaidWaitFrames = 20000;

            while (raidMission.phase == VehicleMissionPhase.DrivingIn
                && (Find.TickManager.TicksGame - raidWaitStartTick) < MaxRaidWaitTicks
                && raidFrames < MaxRaidWaitFrames)
            {
                EnsureUnpausedFast();
                if (raidVehicle.Spawned && !raidVehicle.Destroyed)
                {
                    float d = raidVehicle.Position.DistanceTo(colonyCenter);
                    if (d < minDistToColony) minDistToColony = d;
                }
                raidFrames++;
                yield return null;
            }

            // Give 60 more real ticks for post-dismount assault lord registration if just transitioned.
            int settleUntil = Find.TickManager.TicksGame + 60;
            int settleFrames = 0;
            while (Find.TickManager.TicksGame < settleUntil && settleFrames < 180)
            {
                EnsureUnpausedFast();
                if (raidVehicle.Spawned && !raidVehicle.Destroyed)
                {
                    float d = raidVehicle.Position.DistanceTo(colonyCenter);
                    if (d < minDistToColony) minDistToColony = d;
                }
                settleFrames++;
                yield return null;
            }

            report.raidFinalDistToColony = minDistToColony;
            report.raidVehicleMovedCloser = (initialDistToColony - minDistToColony) >= 3f;
            // Three cells also pass for a vehicle that gave up. Every vehicle must reach its staging cell or the colony.
            foreach (Pawn v in raidMission.vehicles)
            {
                float toStaging = v.Position.DistanceTo(raidMission.stagingCell), toColony = v.Position.DistanceTo(colonyCenter);
                report.notes.Add("ROUTE raid " + v.def.defName + v.thingIDNumber + " spawned=" + v.Spawned + " at " + v.Position + " staging=" + raidMission.stagingCell + " toStaging=" + toStaging.ToString("0.0") + " toColony=" + toColony.ToString("0.0") + " ticks=" + (Find.TickManager.TicksGame - raidWaitStartTick) + " repath=" + raidMission.repathRetried);
                if (toStaging > 9f && toColony > 24f) { report.raidVehicleMovedCloser = false; report.notes.Add("Raid vehicle did not arrive: " + v.def.defName + v.thingIDNumber); }
            }

            int dismountedInAssaultLord = 0;
            for (int i = 0; i < raidMission.crew.Count; i++)
            {
                Pawn r = raidMission.crew[i];
                if (r == null || r.Dead || !r.Spawned || r.Map != map) continue;
                Lord lord = r.GetLord();
                if (lord != null && lord.LordJob is LordJob_AssaultColony)
                {
                    dismountedInAssaultLord++;
                }
            }

            report.raidDismounted = raidMission.dismounted && dismountedInAssaultLord > 0;
            report.raidDismountedInAssaultLord = dismountedInAssaultLord;
            report.raidAssaultLordActive = raidMission.assaultLord != null
                && map.lordManager != null
                && map.lordManager.lords.Contains(raidMission.assaultLord)
                && raidMission.assaultLord.LordJob is LordJob_AssaultColony;

            if (!report.raidVehicleMovedCloser || !report.raidDismounted || !report.raidAssaultLordActive)
            {
                report.notes.Add("Raid movement/dismount check incomplete: initialDist="
                    + initialDistToColony.ToString("0.0", CultureInfo.InvariantCulture)
                    + ", minDist=" + minDistToColony.ToString("0.0", CultureInfo.InvariantCulture)
                    + ", phase=" + raidMission.phase
                    + ", dismountedInLord=" + dismountedInAssaultLord);
            }

            // =====================================================================
            // PART 2: Save mid-raid & Reload — vehicle, crew, and lord must survive
            // =====================================================================
            int savedMissionId = raidMission.missionId;
            int savedVehicleId = raidVehicle.thingIDNumber;
            int savedLordId = raidMission.assaultLord != null ? raidMission.assaultLord.loadID : -1;
            int prevGameHash = RuntimeHelpers.GetHashCode(Current.Game);

            bool saveTriggered = false;
            try
            {
                GameDataSaveLoader.SaveGame(TempSaveName); // CHECK API
                saveTriggered = true;
            }
            catch (Exception ex)
            {
                report.notes.Add("Mid-raid SaveGame failed: " + ex.Message);
            }

            if (saveTriggered)
            {
                for (int f = 0; f < 10; f++) yield return null;

                bool loadTriggered = false;
                try
                {
                    GameDataSaveLoader.LoadGame(TempSaveName); // CHECK API
                    loadTriggered = true;
                }
                catch (Exception ex)
                {
                    report.notes.Add("Mid-raid LoadGame failed: " + ex.Message);
                }

                if (loadTriggered)
                {
                    // Wait until RimWorld finishes loading the new Game instance and returns to Playing.
                    int loadWaitFrames = 0;
                    while (RuntimeHelpers.GetHashCode(Current.Game) == prevGameHash && loadWaitFrames < 1800)
                    {
                        loadWaitFrames++;
                        yield return null;
                    }

                    // Give post-load initialization 20 real frames.
                    for (int f = 0; f < 20; f++) yield return null;

                    report.saveLoadCompleted = RuntimeHelpers.GetHashCode(Current.Game) != prevGameHash;

                    TMM_VehicleMissionManager reloadedMgr = TMM_VehicleMissionManager.Instance;
                    VehicleMission reloadedMission = FindMissionById(reloadedMgr, savedMissionId);
                    report.reloadMissionPresent = reloadedMission != null;

                    if (reloadedMission != null)
                    {
                        Pawn reloadedVeh = reloadedMission.PrimaryVehicle;
                        report.reloadVehiclePresent = reloadedVeh != null
                            && reloadedVeh.Spawned
                            && !reloadedVeh.Destroyed
                            && reloadedVeh.thingIDNumber == savedVehicleId;

                        int aliveCrew = 0;
                        for (int i = 0; i < reloadedMission.crew.Count; i++)
                        {
                            Pawn c = reloadedMission.crew[i];
                            if (c != null && !c.Destroyed && !c.Dead) aliveCrew++;
                        }
                        report.reloadCrewPresent = aliveCrew > 0;

                        report.reloadLordPresent = reloadedMission.assaultLord != null
                            && reloadedMission.map != null
                            && reloadedMission.map.lordManager != null
                            && reloadedMission.map.lordManager.lords.Contains(reloadedMission.assaultLord)
                            && (savedLordId < 0 || reloadedMission.assaultLord.loadID == savedLordId);

                        // Clean up raid entities on reloaded map so Part 3 peaceful visit is uninterrupted.
                        CleanupMissionForNextPhase(reloadedMission);
                    }
                    else
                    {
                        report.notes.Add("Reloaded TMM_VehicleMissionManager did not contain missionId=" + savedMissionId);
                    }
                }
            }

            // Refresh map reference after reload.
            map = Find.AnyPlayerHomeMap != null ? Find.AnyPlayerHomeMap : Find.CurrentMap;
            if (map == null)
            {
                report.notes.Add("Map missing after reload.");
                yield break;
            }
            colonyCenter = VehicleRaidPlacement.FindColonyCenter(map);

            // =====================================================================
            // PART 3: Minutemen Peaceful Trade Visit on Cargo Hauler
            // =====================================================================
            // The raid above is still on the map after its own checks; its survivors used to shoot the visitors and the colonists,
            // so the visit part depended on that fight. The visit is checked on a quiet map.
            foreach (Pawn raider in map.mapPawns.AllPawnsSpawned.Where(p => p.Faction != null && p.Faction.def == gunners.def).ToList()) raider.Destroy(DestroyMode.Vanish);
            EnsureColonyHasColonists(map, map.mapPawns.FreeColonistsSpawnedCount + 1);
            Faction minutemen = FindFactionByDefName("TMM_MinutemenHuntersFaction");
            if (minutemen == null)
            {
                report.notes.Add("Faction TMM_MinutemenHuntersFaction not found in world.");
                yield break;
            }

            if (minutemen.HostileTo(Faction.OfPlayer))
            {
                int restore = Math.Max(100, -minutemen.PlayerGoodwill + 40);
                minutemen.TryAffectGoodwillWith(Faction.OfPlayer, restore, false, false, null, null);
            }

            // Deterministically prefer TMM_Vehicle_CargoHauler for this probe check, restoring extension right after spawn.
            FactionVehiclesExtension mmExt = minutemen.def.GetModExtension<FactionVehiclesExtension>();
            List<bool> savedForVisitsFlags = new List<bool>();
            if (mmExt != null && mmExt.vehicles != null)
            {
                for (int i = 0; i < mmExt.vehicles.Count; i++)
                {
                    FactionVehicleEntry e = mmExt.vehicles[i];
                    savedForVisitsFlags.Add(e != null && e.forVisits);
                    if (e != null)
                    {
                        e.forVisits = string.Equals(e.defName, "TMM_Vehicle_CargoHauler", StringComparison.Ordinal);
                    }
                }
            }

            VehicleMission visitMission = null;
            try
            {
                IncidentParms visitParms = new IncidentParms();
                visitParms.target = map;
                visitParms.faction = minutemen;
                visitParms.forced = true;
                visitParms.sendLetter = true;
                visitMission = IncidentWorker_TMMVehicleVisit.ExecuteVehicleVisit(visitParms);
            }
            finally
            {
                if (mmExt != null && mmExt.vehicles != null)
                {
                    for (int i = 0; i < mmExt.vehicles.Count && i < savedForVisitsFlags.Count; i++)
                    {
                        if (mmExt.vehicles[i] != null) mmExt.vehicles[i].forVisits = savedForVisitsFlags[i];
                    }
                }
            }

            if (visitMission == null || visitMission.PrimaryVehicle == null)
            {
                report.notes.Add("ExecuteVehicleVisit returned null or spawned no truck.");
                yield break;
            }

            Pawn truck = visitMission.PrimaryVehicle;
            report.visitVehicleDefName = truck.def != null ? truck.def.defName : string.Empty;
            report.visitIsCargoHauler = string.Equals(report.visitVehicleDefName, "TMM_Vehicle_CargoHauler", StringComparison.Ordinal);
            report.visitTrunkStockCount = (truck.inventory != null && truck.inventory.innerContainer != null)
                ? truck.inventory.innerContainer.Count
                : 0;

            float visitStartDist = truck.Position.DistanceTo(colonyCenter);
            int visitArriveStartTick = Find.TickManager.TicksGame;
            int visitArriveFrames = 0;
            const int MaxVisitWaitTicks = 4800;
            const int MaxVisitWaitFrames = 3600;

            while (visitMission.phase == VehicleMissionPhase.DrivingIn
                && (Find.TickManager.TicksGame - visitArriveStartTick) < MaxVisitWaitTicks
                && visitArriveFrames < MaxVisitWaitFrames)
            {
                EnsureUnpausedFast();
                visitArriveFrames++;
                yield return null;
            }

            report.visitTruckArrived = visitMission.phase == VehicleMissionPhase.Trading
                && truck.Spawned
                && truck.Position.DistanceTo(colonyCenter) < visitStartDist - 2f
                && truck.Position.DistanceTo(visitMission.stagingCell) <= 8f;
            report.notes.Add("ROUTE visit at " + truck.Position + " staging=" + visitMission.stagingCell + " toStaging=" + truck.Position.DistanceTo(visitMission.stagingCell).ToString("0.0") + " ticks=" + (Find.TickManager.TicksGame - visitArriveStartTick) + " repath=" + visitMission.repathRetried);

            if (!report.visitTruckArrived) report.notes.Add("Truck did not arrive: phase=" + visitMission.phase + " at " + truck.Position + " from distance " + visitStartDist.ToString("0.0") + " now " + truck.Position.DistanceTo(colonyCenter).ToString("0.0")
                + " staging=" + visitMission.stagingCell + " ticks=" + (Find.TickManager.TicksGame - visitArriveStartTick) + " frames=" + visitArriveFrames + " stagnant=" + visitMission.stagnantTicks + " repath=" + visitMission.repathRetried + " pending=" + VfBridge.PathPending(truck));
            Pawn trader = visitMission.trader;
            report.visitTraderAvailable = trader != null
                && trader.Spawned
                && !trader.Dead
                && !trader.Downed
                && trader.CanTradeNow;

            // Verify trader exposes goods directly from the cargo truck's trunk and TradeSession opens cleanly.
            if (trader != null && trader.trader != null && truck.inventory != null && truck.inventory.innerContainer != null)
            {
                HashSet<int> trunkIds = new HashSet<int>();
                IList<Thing> trunkItems = truck.inventory.innerContainer.InnerListForReading;
                for (int i = 0; i < trunkItems.Count; i++)
                {
                    if (trunkItems[i] != null && !trunkItems[i].Destroyed)
                    {
                        trunkIds.Add(trunkItems[i].thingIDNumber);
                    }
                }

                int matchedTrunkGoods = 0;
                IEnumerable<Thing> exposedGoods = trader.trader.Goods;
                if (exposedGoods != null)
                {
                    foreach (Thing g in exposedGoods)
                    {
                        if (g != null && trunkIds.Contains(g.thingIDNumber))
                        {
                            matchedTrunkGoods++;
                        }
                    }
                }
                report.visitTrunkGoodsExposed = matchedTrunkGoods > 0;

                Pawn negotiator = FindBestColonyNegotiator(map);
                if (negotiator == null) report.notes.Add("No colonist able to negotiate: " + string.Join("; ", map.mapPawns.FreeColonists.Select(p => p.LabelShort + " spawned=" + p.Spawned + " downed=" + p.Downed + " dead=" + p.Dead + " social=" + !p.WorkTagIsDisabled(WorkTags.Social)).ToArray()));
                if (negotiator != null && report.visitTraderAvailable)
                {
                    TradeSession.SetupWith(trader, negotiator, false);
                    report.visitTradeSessionOpened = TradeSession.Active && TradeSession.trader == trader;
                    Thing sold = ThingMaker.MakeThing(ThingDefOf.Gold);
                    sold.stackCount = 1;
                    FieldInfo hook = typeof(VehicleRaidPatches).GetField("TestAcceptSoldCargo", BindingFlags.Static | BindingFlags.NonPublic);
                    int refusals = 0;
                    ThingOwner custody = WorldTrafficCustody.Instance.GetDirectlyHeldThings();
                    hook.SetValue(null, (Func<ThingOwner, Thing, bool>)((owner, item) => { refusals++; return false; }));
                    try { trader.trader.GiveSoldThingToTrader(sold, 1, negotiator); }
                    finally
                    {
                        hook.SetValue(null, null);
                        if (TradeSession.Active) TradeSession.Close();
                    }
                    report.cargoSecuredAfterBothRefused = refusals == 2 && !sold.Destroyed && sold.holdingOwner == custody;
                    WorldTrafficCustody.Instance.RetryNow();
                    report.cargoReturnedToBuyer = !sold.Destroyed && sold.holdingOwner == trader.inventory.innerContainer;
                }
            }

            Pawn captive = FactionEventHelper.GenerateFactionPawn(minutemen, map, null, PawnKindDefOf.Villager);
            GenSpawn.Spawn(captive, CellFinder.RandomClosewalkCellNear(truck.Position, map, 3), map);
            captive.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            visitMission.crew.Add(captive);

            // Trigger departure and wait over real ticks for crew to board and truck to leave the map.
            TMM_VehicleMissionManager activeMgr = TMM_VehicleMissionManager.Instance;
            if (activeMgr != null)
            {
                activeMgr.BeginVisitDeparture(visitMission);
            }

            int departStartTick = Find.TickManager.TicksGame;
            int departFrames = 0;
            const int MaxDepartTicks = 5400;
            const int MaxDepartFrames = 4000;

            while (visitMission.phase != VehicleMissionPhase.Completed
                && truck.Spawned
                && !truck.Destroyed
                && (Find.TickManager.TicksGame - departStartTick) < MaxDepartTicks
                && departFrames < MaxDepartFrames)
            {
                EnsureUnpausedFast();
                departFrames++;
                yield return null;
            }

            report.visitTruckDeparted = visitMission.phase == VehicleMissionPhase.Completed
                && (!truck.Spawned || truck.Destroyed);

            report.captiveRemained = captive.IsPrisonerOfColony && captive.Spawned && captive.Map == map &&
                !Find.WorldPawns.Contains(captive);
            report.visitorDeparted = trader != null && !trader.Spawned && Find.WorldPawns.Contains(trader);
            if (!report.visitTruckDeparted)
            {
                report.notes.Add("Visit truck did not finish departing: phase=" + visitMission.phase
                    + ", spawned=" + truck.Spawned + ", hostile=" + minutemen.HostileTo(Faction.OfPlayer) + ", goodwill=" + minutemen.PlayerGoodwill + ", ticks=" + (Find.TickManager.TicksGame - departStartTick)
                    + ", aboard=" + VfBridge.GetAllPawnsAboard(truck).Count + ", crew: " + string.Join("; ", visitMission.crew.Select(p => p.LabelShort + " dead=" + p.Dead + " downed=" + p.Downed + " spawned=" + p.Spawned + " prisoner=" + p.IsPrisonerOfColony + " dist=" + (p.Spawned ? p.Position.DistanceTo(truck.Position) : -1f)).ToArray())
                    + ", hostiles on map=" + map.mapPawns.AllPawnsSpawned.Count(p => p.HostileTo(Faction.OfPlayer)) + " in fight with visitors=" + map.mapPawns.AllPawnsSpawned.Count(p => p.HostileTo(minutemen)));
            }
        }

        private static void EnsureUnpausedFast()
        {
            if (Find.TickManager == null) return;
            if (Find.TickManager.Paused)
            {
                Find.TickManager.TogglePaused();
            }
            if (Find.TickManager.CurTimeSpeed != TimeSpeed.Ultrafast)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
            }
        }

        private static Faction FindFactionByDefName(string defName)
        {
            if (Find.FactionManager == null || string.IsNullOrEmpty(defName)) return null;
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            return Find.FactionManager.FirstFactionOfDef(def);
        }

        private static VehicleMission FindMissionById(TMM_VehicleMissionManager mgr, int missionId)
        {
            if (mgr == null || mgr.missions == null) return null;
            for (int i = 0; i < mgr.missions.Count; i++)
            {
                VehicleMission m = mgr.missions[i];
                if (m != null && m.missionId == missionId) return m;
            }
            return null;
        }

        private static void CleanupMissionForNextPhase(VehicleMission m)
        {
            if (m == null) return;
            if (m.assaultLord != null && m.map != null && m.map.lordManager != null)
            {
                m.map.lordManager.RemoveLord(m.assaultLord);
                m.assaultLord = null;
            }
            for (int i = 0; i < m.vehicles.Count; i++)
            {
                Pawn v = m.vehicles[i];
                if (v == null || v.Destroyed) continue;
                VfBridge.DisembarkAll(v);
                if (v.Spawned) v.DeSpawn(DestroyMode.Vanish);
                if (!v.Destroyed) v.Destroy(DestroyMode.Vanish);
            }
            for (int i = 0; i < m.crew.Count; i++)
            {
                Pawn p = m.crew[i];
                if (p == null || p.Destroyed) continue;
                if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
            }
            m.phase = VehicleMissionPhase.Completed;
        }

        private static void EnsureColonyHasColonists(Map map, int minCount)
        {
            if (map == null || map.mapPawns == null) return;
            int current = map.mapPawns.FreeColonistsSpawnedCount;
            while (current < minCount)
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
                current++;
            }
        }

        private static Pawn FindBestColonyNegotiator(Map map)
        {
            if (map == null || map.mapPawns == null) return null;
            List<Pawn> list = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < list.Count; i++)
            {
                Pawn p = list[i];
                if (p != null && !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Social))
                {
                    return p;
                }
            }
            return null;
        }

        private static void CleanupTempSaveFile()
        {
            string path = GenFilePaths.FilePathForSavedGame(TempSaveName); // CHECK API
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static bool EvaluatePassed(ProbeReport r)
        {
            return string.IsNullOrEmpty(r.fatalException)
                && r.errorCount == 0
                && r.vfAvailable
                && r.raidSpawnedAtEdge
                && r.raidHasCrewAtSpawn
                && r.raidPointsBudgetReasonable
                && r.raidVehicleMovedCloser
                && r.raidDismounted
                && r.raidAssaultLordActive
                && r.saveLoadCompleted
                && r.reloadMissionPresent
                && r.reloadVehiclePresent
                && r.reloadCrewPresent
                && r.reloadLordPresent
                && r.visitIsCargoHauler
                && r.visitTrunkStockCount > 0
                && r.visitTruckArrived
                && r.visitTraderAvailable
                && r.visitTrunkGoodsExposed
                && r.visitTradeSessionOpened
                && r.visitTruckDeparted
                && r.insufficientBudgetRejected
                && r.captiveRemained
                && r.visitorDeparted
                && r.cargoSecuredAfterBothRefused
                && r.cargoReturnedToBuyer;
        }

        private static string ResolveReportDirectory()
        {
            if (!string.IsNullOrEmpty(CustomReportDir)) return CustomReportDir;
            string envDir = Environment.GetEnvironmentVariable("TMM_PROBE_DIR");
            if (!string.IsNullOrEmpty(envDir)) return envDir;
            return GenFilePaths.SaveDataFolderPath;
        }

        private static void WriteReport(ProbeReport report)
        {
            try
            {
                string dir = ResolveReportDirectory();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, ReportFileName);
                File.WriteAllText(path, SerializeReport(report), Encoding.UTF8);
                // The test harness also waits for a UI report; raids have no UI of their own, so it mirrors the result.
                File.WriteAllText(Path.Combine(dir, "VehicleRaidUiProbe.json"), "{\"passed\":" + (report.passed ? "true" : "false") + "}", Encoding.UTF8);
                Log.Message("[TMM] Wrote VehicleRaidProbe report: " + path);
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] Failed to write VehicleRaidProbe report: " + ex);
            }
        }

        private static string SerializeReport(ProbeReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            AppendJsonBool(sb, "passed", r.passed, true);
            AppendJsonBool(sb, "vfAvailable", r.vfAvailable, true);
            AppendJsonString(sb, "raidVehicleDefName", r.raidVehicleDefName, true);
            AppendJsonInt(sb, "raidVehicleCount", r.raidVehicleCount, true);
            AppendJsonBool(sb, "raidSpawnedAtEdge", r.raidSpawnedAtEdge, true);
            AppendJsonInt(sb, "raidCrewAboardAtSpawn", r.raidCrewAboardAtSpawn, true);
            AppendJsonBool(sb, "raidHasCrewAtSpawn", r.raidHasCrewAtSpawn, true);
            AppendJsonFloat(sb, "raidRequestedPoints", r.raidRequestedPoints, true);
            AppendJsonFloat(sb, "raidVehiclePointsSpent", r.raidVehiclePointsSpent, true);
            AppendJsonFloat(sb, "raidPawnCombatPowerSum", r.raidPawnCombatPowerSum, true);
            AppendJsonFloat(sb, "raidTotalEffectivePoints", r.raidTotalEffectivePoints, true);
            AppendJsonBool(sb, "raidPointsBudgetReasonable", r.raidPointsBudgetReasonable, true);
            AppendJsonFloat(sb, "raidInitialDistToColony", r.raidInitialDistToColony, true);
            AppendJsonFloat(sb, "raidFinalDistToColony", r.raidFinalDistToColony, true);
            AppendJsonBool(sb, "raidVehicleMovedCloser", r.raidVehicleMovedCloser, true);
            AppendJsonBool(sb, "raidDismounted", r.raidDismounted, true);
            AppendJsonInt(sb, "raidDismountedInAssaultLord", r.raidDismountedInAssaultLord, true);
            AppendJsonBool(sb, "raidAssaultLordActive", r.raidAssaultLordActive, true);
            AppendJsonBool(sb, "saveLoadCompleted", r.saveLoadCompleted, true);
            AppendJsonBool(sb, "reloadMissionPresent", r.reloadMissionPresent, true);
            AppendJsonBool(sb, "reloadVehiclePresent", r.reloadVehiclePresent, true);
            AppendJsonBool(sb, "reloadCrewPresent", r.reloadCrewPresent, true);
            AppendJsonBool(sb, "reloadLordPresent", r.reloadLordPresent, true);
            AppendJsonString(sb, "visitVehicleDefName", r.visitVehicleDefName, true);
            AppendJsonBool(sb, "visitIsCargoHauler", r.visitIsCargoHauler, true);
            AppendJsonInt(sb, "visitTrunkStockCount", r.visitTrunkStockCount, true);
            AppendJsonBool(sb, "visitTruckArrived", r.visitTruckArrived, true);
            AppendJsonBool(sb, "visitTraderAvailable", r.visitTraderAvailable, true);
            AppendJsonBool(sb, "visitTrunkGoodsExposed", r.visitTrunkGoodsExposed, true);
            AppendJsonBool(sb, "visitTradeSessionOpened", r.visitTradeSessionOpened, true);
            AppendJsonBool(sb, "visitTruckDeparted", r.visitTruckDeparted, true);
            AppendJsonBool(sb, "insufficientBudgetRejected", r.insufficientBudgetRejected, true);
            AppendJsonBool(sb, "captiveRemained", r.captiveRemained, true);
            AppendJsonBool(sb, "visitorDeparted", r.visitorDeparted, true);
            AppendJsonBool(sb, "cargoSecuredAfterBothRefused", r.cargoSecuredAfterBothRefused, true);
            AppendJsonBool(sb, "cargoReturnedToBuyer", r.cargoReturnedToBuyer, true);
            AppendJsonInt(sb, "errorCount", r.errorCount, true);
            AppendJsonString(sb, "fatalException", r.fatalException, true);
            AppendJsonStringList(sb, "notes", r.notes, true);
            AppendJsonStringList(sb, "errors", r.errors, false);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void AppendJsonBool(StringBuilder sb, string key, bool value, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": ").Append(value ? "true" : "false");
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendJsonInt(StringBuilder sb, string key, int value, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendJsonFloat(StringBuilder sb, string key, float value, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": ").Append(value.ToString("0.##", CultureInfo.InvariantCulture));
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendJsonString(StringBuilder sb, string key, string value, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": \"").Append(EscapeJson(value ?? string.Empty)).Append("\"");
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendJsonStringList(StringBuilder sb, string key, List<string> values, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": [");
            if (values != null && values.Count > 0)
            {
                sb.AppendLine();
                for (int i = 0; i < values.Count; i++)
                {
                    sb.Append("    \"").Append(EscapeJson(values[i] ?? string.Empty)).Append("\"");
                    sb.AppendLine(i + 1 < values.Count ? "," : string.Empty);
                }
                sb.Append("  ]");
            }
            else
            {
                sb.Append("]");
            }
            sb.AppendLine(comma ? "," : string.Empty);
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

        private sealed class ProbeReport
        {
            public bool passed;
            public bool vfAvailable;
            public string raidVehicleDefName = string.Empty;
            public int raidVehicleCount;
            public bool raidSpawnedAtEdge;
            public int raidCrewAboardAtSpawn;
            public bool raidHasCrewAtSpawn;
            public float raidRequestedPoints;
            public float raidVehiclePointsSpent;
            public float raidPawnCombatPowerSum;
            public float raidTotalEffectivePoints;
            public bool raidPointsBudgetReasonable;
            public float raidInitialDistToColony;
            public float raidFinalDistToColony;
            public bool raidVehicleMovedCloser;
            public bool raidDismounted;
            public int raidDismountedInAssaultLord;
            public bool raidAssaultLordActive;
            public bool saveLoadCompleted;
            public bool reloadMissionPresent;
            public bool reloadVehiclePresent;
            public bool reloadCrewPresent;
            public bool reloadLordPresent;
            public string visitVehicleDefName = string.Empty;
            public bool visitIsCargoHauler;
            public int visitTrunkStockCount;
            public bool visitTruckArrived;
            public bool visitTraderAvailable;
            public bool visitTrunkGoodsExposed;
            public bool visitTradeSessionOpened;
            public bool visitTruckDeparted;
            public bool insufficientBudgetRejected;
            public bool captiveRemained;
            public bool visitorDeparted;
            public bool cargoSecuredAfterBothRefused;
            public bool cargoReturnedToBuyer;
            public int errorCount;
            public string fatalException = string.Empty;
            public List<string> notes = new List<string>();
            public List<string> errors = new List<string>();
        }
    }
}
