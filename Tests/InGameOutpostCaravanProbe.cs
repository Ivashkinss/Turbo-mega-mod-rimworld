using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class OutpostCaravanProbeBootstrap
    {
        public const string SaveName = "TMM-OutpostCaravan";

        public static bool FreshMode
        {
            get { return HasArg("-tmm-outpost-caravan-probe"); }
        }

        public static bool ReloadMode
        {
            get { return HasArg("-tmm-outpost-caravan-reload"); }
        }

        static OutpostCaravanProbeBootstrap()
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
    /// Round 2 in-game verification probe for walking outpost caravans:
    /// - REAL vanilla stockpile loading &amp; formation via LordJob_FormAndSendCaravan driven
    ///   natively across MapComponentTick frames (ZERO manual caravanSent / Tile / inventory hacks);
    /// - Save &amp; Reload on the OUTBOUND leg toward the outpost;
    /// - Storage refusal AFTER stack 1 enters RawMaterials via REAL vanilla arrival (PatherArrived
    ///   clears ArrivalAction BEFORE calling Arrived) -> full rollback, personal silver untouched
    ///   (even after shifting personal silver between pawns), state == RetryDelivery;
    /// - Real "Retry Delivery" gizmo completes delivery, preserves personal silver, consumes real
    ///   food during travel, and returns carrier home;
    /// - Real "Return Home" gizmo, worker overflow keeping confirmed silver, outpost deletion before
    ///   and after exit, aborted formation non-hijacking, verified food transfer rollback on TryAdd
    ///   refusal, legacy save migration without deduction, and honest OutpostCaravanUiProbe.json.
    /// </summary>
    public sealed class InGameOutpostCaravanProbe : MapComponent
    {
        private sealed class CheckItem
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private enum FreshPhase
        {
            WaitingStart,
            WaitFormation_CancelAndDestroy,
            WaitFormation_OutpostDeleteAfterExit,
            WaitFormation_WorkerOverflow,
            WaitFormation_MainOutboundSave,
            Done
        }

        private const int FormationTimeoutTicks = 18000;
        private const string ProbeReturnCarrierNick = "OCProbeCarrier";
        private const string ProbeStayWorkerNick = "OCProbeWorker";

        private static bool s_completed;
        private int m_ticksWaited;
        private FreshPhase m_phase = FreshPhase.WaitingStart;
        private int m_phaseTicks;
        private bool m_loggingHooked;

        // Staging state for tick-driven vanilla formations
        private IntVec3 m_edgeAnchor;
        private Zone_Stockpile m_stockpile;
        private StrategicOutpost m_activeOutpost;
        private OutpostCaravanMission m_activeMission;
        private Pawn m_mainCarrier;
        private Pawn m_mainWorker;
        private Pawn m_overflowWorker;

        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        public InGameOutpostCaravanProbe(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (s_completed) return;
            if (!OutpostCaravanProbeBootstrap.FreshMode && !OutpostCaravanProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            if (WaitInitialStartup()) return;

            if (!m_loggingHooked)
            {
                m_loggingHooked = true;
                Application.logMessageReceived += OnUnityLog;
            }

            try
            {
                if (OutpostCaravanProbeBootstrap.ReloadMode)
                {
                    s_completed = true;
                    RunReloadProbe();
                    FinishProbe("OutpostCaravanReloadProbe.json", false);
                    return;
                }

                StepFreshStateMachine();
            }
            catch (Exception ex)
            {
                m_errors.Add("Unhandled exception: " + ex.ToString());
                s_completed = true;
                FinishProbe(
                    OutpostCaravanProbeBootstrap.ReloadMode ? "OutpostCaravanReloadProbe.json" : "OutpostCaravanProbe.json",
                    !OutpostCaravanProbeBootstrap.ReloadMode);
            }
        }

        private bool WaitInitialStartup()
        {
            if (m_phase == FreshPhase.WaitingStart)
            {
                m_ticksWaited++;
                if (m_ticksWaited < 30) return true;
                if (Find.TickManager != null)
                {
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                }
            }
            return false;
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        private void FinishProbe(string reportFile, bool writeUi)
        {
            OutpostCaravanLogistics.TestRejectRawMaterialsTransfer = false;
            OutpostCaravanLogistics.TestRejectRawMaterialsTransferOnStep = -1;
            OutpostCaravanLogistics.TestRejectInventoryTryAdd = null;

            if (m_loggingHooked)
            {
                Application.logMessageReceived -= OnUnityLog;
                m_loggingHooked = false;
            }

            bool allPassed = WriteReport(reportFile);
            if (writeUi)
            {
                WriteUiReport(allPassed);
            }
        }

        // =====================================================================
        // FRESH PROBE STATE MACHINE (100% REAL VANILLA STOCKPILE FORMATION)
        // =====================================================================

        private void StepFreshStateMachine()
        {
            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            if (mgr == null)
            {
                AddCheck("Manager_Exists", false, "non-null", "null");
                s_completed = true;
                FinishProbe("OutpostCaravanProbe.json", true);
                return;
            }

            if (m_phase == FreshPhase.WaitingStart)
            {
                AddCheck("Manager_Exists", true, "non-null", "ok");
                m_edgeAnchor = FindEdgeStagingAnchor(map);
                m_stockpile = EnsureEdgeStockpile(map, m_edgeAnchor);
                AddCheck("Setup_EdgeStockpileReady", m_stockpile != null && m_stockpile.cells.Count >= 6, ">=6 cells",
                    m_stockpile != null ? m_stockpile.cells.Count.ToString() : "null");

                RunInstantChecks(mgr);
                BeginPhase_CancelAndDestroy(mgr);
                return;
            }

            m_phaseTicks++;

            if (m_phase == FreshPhase.WaitFormation_CancelAndDestroy)
            {
                if (!WaitForVanillaCaravanExit("VanillaFormation_CancelAndDestroy")) return;
                CompletePhase_CancelAndDestroy(mgr);
                BeginPhase_OutpostDeleteAfterExit(mgr);
                return;
            }

            if (m_phase == FreshPhase.WaitFormation_OutpostDeleteAfterExit)
            {
                if (!WaitForVanillaCaravanExit("VanillaFormation_OutpostDeleteAfterExit")) return;
                CompletePhase_OutpostDeleteAfterExit(mgr);
                BeginPhase_WorkerOverflow(mgr);
                return;
            }

            if (m_phase == FreshPhase.WaitFormation_WorkerOverflow)
            {
                if (!WaitForVanillaCaravanExit("VanillaFormation_WorkerOverflow")) return;
                CompletePhase_WorkerOverflow(mgr);
                BeginPhase_MainOutboundSave(mgr);
                return;
            }

            if (m_phase == FreshPhase.WaitFormation_MainOutboundSave)
            {
                if (!WaitForVanillaCaravanExit("VanillaFormation_MainOutboundSave")) return;
                CompletePhase_MainOutboundSave(mgr);
                m_phase = FreshPhase.Done;
                s_completed = true;
                FinishProbe("OutpostCaravanProbe.json", true);
            }
        }

        private bool WaitForVanillaCaravanExit(string phaseLabel)
        {
            if (m_activeMission == null)
            {
                AddCheck(phaseLabel + "_Started", false, "mission active", "mission null");
                return true;
            }

            // Wait until the caravan has actually left the home tile: cancelling or deleting the outpost while it still
            // stands on the home tile makes it re-enter the colony at once, which is correct but not what these phases test.
            if (m_activeMission.caravan != null && m_activeMission.everAttached && m_activeMission.caravan.Tile == map.Tile && m_phaseTicks < FormationTimeoutTicks)
            {
                return false;
            }
            if (m_activeMission.caravan != null && m_activeMission.everAttached)
            {
                {
                    System.Text.StringBuilder dsb = new System.Text.StringBuilder();
                    dsb.Append("[TMM probe] silver diag ").Append(phaseLabel).Append(": caravanSilver=").Append(OutpostCaravanLogistics.CountCaravanSilver(m_activeMission.caravan)).Append(" ids=");
                    if (m_activeMission.missionSilverIds != null)
                    {
                        foreach (string sid in m_activeMission.missionSilverIds)
                        {
                            dsb.Append(sid).Append('@');
                            Thing found = null;
                            foreach (Thing mt in map.listerThings.ThingsOfDef(ThingDefOf.Silver)) { if (mt.ThingID == sid) { found = mt; break; } }
                            dsb.Append(found != null ? ("map:" + found.Position + "x" + found.stackCount + (found.IsForbidden(Faction.OfPlayer) ? "F" : "")) : "notOnMap").Append(' ');
                        }
                    }
                    dsb.Append(" mapSilver=");
                    foreach (Thing mt in map.listerThings.ThingsOfDef(ThingDefOf.Silver)) dsb.Append(mt.ThingID).Append("x").Append(mt.stackCount).Append(' ');
                    foreach (Pawn cp in m_activeMission.caravan.PawnsListForReading) foreach (Thing it in cp.inventory.innerContainer) if (it.def == ThingDefOf.Silver) dsb.Append(" inv:").Append(cp.LabelShort).Append(":").Append(it.ThingID).Append("x").Append(it.stackCount);
                    Log.Message(dsb.ToString());
                }
                Log.Message(string.Format("[TMM probe] {0}: attached after {1} ticks, state={2}, pathDest={3}, moving={4}, tile={5}, outpostTile={6}", phaseLabel, m_phaseTicks, m_activeMission.state, m_activeMission.caravan.pather != null ? m_activeMission.caravan.pather.Destination.ToString() : "-", m_activeMission.caravan.pather != null && m_activeMission.caravan.pather.Moving, m_activeMission.caravan.Tile, m_activeOutpost != null ? m_activeOutpost.Tile.ToString() : "-"));
                return true;
            }

            if (m_phaseTicks >= FormationTimeoutTicks)
            {
                AddCheck(phaseLabel + "_TimedOut", false, "caravan exits map naturally",
                    string.Format("state={0}, ticks={1}", m_activeMission.state, m_phaseTicks));
                return true;
            }

            return false;
        }

        // ---------------- INSTANT CHECKS (NO FORMATION WAIT) ----------------

        private void RunInstantChecks(OutpostCaravanManager mgr)
        {
            // 1. TransferableOneWay food count check (no full-stack masking)
            Pawn foodTester = SpawnCleanColonist("FoodCheck", m_edgeAnchor);
            Thing bigMealStack = SpawnInStockpile(ThingDefOf.MealSurvivalPack, 40, 0);
            Dictionary<Thing, int> oneMeal = new Dictionary<Thing, int>();
            oneMeal[bigMealStack] = 1;
            float daysFor1Meal = OutpostCaravanLogistics.CalcTransferableFoodDays(new List<Pawn> { foodTester }, oneMeal, map.Tile);
            AddCheck("P2_FoodCalcUsesTransferableExactCount", daysFor1Meal < 2.0f,
                "< 2.0d for 1 meal from 40-stack", string.Format("{0:0.00}d", daysFor1Meal));
            if (!bigMealStack.Destroyed) bigMealStack.Destroy(DestroyMode.Vanish);
            CleanupCleanPawn(foodTester);

            // 2. Last-mobile-carrier protection
            StrategicOutpost outpostGuard = CreateTestOutpost(2, 4, "ProbeOutpostGuard");
            if (outpostGuard != null)
            {
                Pawn walkStay = SpawnCleanColonist("WalkStay", m_edgeAnchor);
                Pawn downedBuddy = SpawnCleanColonist("DownedBuddy", m_edgeAnchor);
                downedBuddy.health.AddHediff(HediffDefOf.Anesthetic);
                walkStay.DeSpawn(DestroyMode.Vanish);
                downedBuddy.DeSpawn(DestroyMode.Vanish);
                Caravan guardCar = CaravanMaker.MakeCaravan(new List<Pawn> { walkStay, downedBuddy }, Faction.OfPlayer, outpostGuard.Tile, true); // CHECK API

                string guardFail;
                bool blocked = !outpostGuard.AddPawnFromCaravan(guardCar, walkStay, out guardFail, true) &&
                               guardCar.ContainsPawn(walkStay);
                AddCheck("P2_LastMobileCarrierProtected", blocked, "refused & stays in caravan",
                    string.Format("blocked={0}, reason={1}", blocked, guardFail ?? "none"));
                DestroyCaravanAndPawns(guardCar);
                outpostGuard.Destroy();
            }

            // 3. Verified food transfer rollback when carrier inventory refuses TryAdd (TestMode)
            if (TestMode.Enabled)
            {
                Pawn worker = SpawnCleanColonist("FoodWorker", m_edgeAnchor);
                Pawn carrier = SpawnCleanColonist("FoodCarrier", m_edgeAnchor);
                Thing workerMeals = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
                workerMeals.stackCount = 10;
                worker.inventory.innerContainer.TryAdd(workerMeals, false);
                worker.DeSpawn(DestroyMode.Vanish);
                carrier.DeSpawn(DestroyMode.Vanish);
                Caravan foodCar = CaravanMaker.MakeCaravan(new List<Pawn> { worker, carrier }, Faction.OfPlayer, map.Tile, true); // CHECK API

                OutpostCaravanLogistics.TestRejectInventoryTryAdd = delegate(ThingOwner owner, Thing item)
                {
                    return owner == carrier.inventory.innerContainer;
                };

                OutpostCaravanLogistics.EnsureCarriersHaveReturnFood(
                    foodCar, null, new List<Pawn> { worker }, new List<Pawn> { carrier }, map.Tile, 2.0f);

                OutpostCaravanLogistics.TestRejectInventoryTryAdd = null;

                int totalMealsAfter = OutpostCaravanLogistics.CountPawnInventoryThing(worker, ThingDefOf.MealSurvivalPack) +
                                      OutpostCaravanLogistics.CountPawnInventoryThing(carrier, ThingDefOf.MealSurvivalPack);
                AddCheck("R2_FoodTransferTryAddRefusal_NoOrphanedMeals", totalMealsAfter == 10,
                    "10 meals preserved (returned to worker)", totalMealsAfter.ToString());
                DestroyCaravanAndPawns(foodCar);
            }

            // 4. Outpost deleted BEFORE exit -> formation aborted cleanly
            ResetStockpileSupplies(new int[] { 200 }, 30);
            StrategicOutpost outpostBefore = CreateTestOutpost(2, 4, "ProbeOutpostBefore");
            if (outpostBefore != null)
            {
                Pawn pBefore = SpawnCleanColonist("BeforeDel", m_edgeAnchor);
                string fail;
                bool started = OutpostCaravanLogistics.StartCaravan(outpostBefore, map, new List<Pawn> { pBefore }, new List<Pawn>(), 100, out fail);
                OutpostCaravanMission mBefore = FindMissionForOutpost(mgr, outpostBefore);
                outpostBefore.Destroy();
                mgr.AuditAndCleanupMissions(false);

                bool cleaned = started && !mgr.Missions.Contains(mBefore) && !CaravanFormingUtility.IsFormingCaravan(pBefore);
                AddCheck("P1_OutpostDeletedBeforeExit_FormationAborted", cleaned,
                    "mission removed & pawn stopped forming",
                    string.Format("started={0}, inMgr={1}, forming={2}", started, mgr.Missions.Contains(mBefore), CaravanFormingUtility.IsFormingCaravan(pBefore)));
                CleanupCleanPawn(pBefore);
            }

            // 5. Aborted formation NEVER hijacks a later normal caravan with same colonist
            ResetStockpileSupplies(new int[] { 200 }, 30);
            StrategicOutpost outpostHijack = CreateTestOutpost(2, 4, "ProbeOutpostHijack");
            if (outpostHijack != null)
            {
                Pawn hijackedTest = SpawnCleanColonist("NoHijackPawn", m_edgeAnchor);
                string fail;
                OutpostCaravanLogistics.StartCaravan(outpostHijack, map, new List<Pawn> { hijackedTest }, new List<Pawn>(), 50, out fail);
                OutpostCaravanMission abortedM = FindMissionForOutpost(mgr, outpostHijack);

                Lord l = hijackedTest.GetLord();
                if (l != null) map.lordManager.RemoveLord(l);
                mgr.AuditAndCleanupMissions(false);

                PlanetTile otherTile;
                TileFinder.TryFindPassableTileWithTraversalDistance(map.Tile, 2, 4, out otherTile, t => t != outpostHijack.Tile); // CHECK API
                Caravan normalCar = CaravanExitMapUtility.ExitMapAndCreateCaravan(
                    new List<Pawn> { hijackedTest }, Faction.OfPlayer, map.Tile, map.Tile, otherTile, false);

                bool notHijacked = !mgr.Missions.Contains(abortedM) &&
                                   mgr.GetMissionForCaravan(normalCar) == null &&
                                   (normalCar.pather == null || !(normalCar.pather.ArrivalAction is CaravanArrivalAction_OutpostDelivery));
                AddCheck("P1_AbortedFormation_DoesNotHijackLaterCaravan", notHijacked, "normal caravan untouched", notHijacked.ToString());
                DestroyCaravanAndPawns(normalCar);
                outpostHijack.Destroy();
            }

            // 6. Legacy save migration: incomplete legacy mission returns caravan home WITHOUT deducting silver
            StrategicOutpost outpostLegacy = CreateTestOutpost(2, 4, "ProbeOutpostLegacy");
            if (outpostLegacy != null)
            {
                Pawn legPawn = SpawnCleanColonist("LegacyPawn", m_edgeAnchor);
                Thing legSilver = ThingMaker.MakeThing(ThingDefOf.Silver);
                legSilver.stackCount = 120;
                legPawn.inventory.innerContainer.TryAdd(legSilver, false);
                legPawn.DeSpawn(DestroyMode.Vanish);

                Caravan legCar = CaravanMaker.MakeCaravan(new List<Pawn> { legPawn }, Faction.OfPlayer, outpostLegacy.Tile, true); // CHECK API
                CaravanArrivalAction_OutpostDelivery oldAct = new CaravanArrivalAction_OutpostDelivery(outpostLegacy, new List<Pawn>(), 120, map);
                legCar.pather.StartPath(outpostLegacy.Tile, oldAct, false, false);

                // Simulate old-save mission (schemaVersion=0, state=Forming, everAttached=true, no missionSilverIds)
                OutpostCaravanMission legacyM = new OutpostCaravanMission();
                legacyM.schemaVersion = 0;
                legacyM.missionId = Guid.NewGuid().ToString("N");
                legacyM.state = OutpostMissionState.Forming;
                legacyM.everAttached = true;
                legacyM.caravan = legCar;
                legacyM.outpost = outpostLegacy;
                legacyM.originMap = map;
                legacyM.silverAmount = 120;
                mgr.Missions.Add(legacyM);

                mgr.MigrateLegacyMissionsNow();

                bool migratedHomeNoDeduct = legacyM.state == OutpostMissionState.Returning &&
                                            CountOutpostSilver(outpostLegacy) == 0 &&
                                            OutpostCaravanLogistics.CountCaravanSilver(legCar) == 120 &&
                                            legCar.pather != null && legCar.pather.Destination == map.Tile;
                AddCheck("R2_LegacySaveMigration_IncompleteReturnsHomeNoDeduction", migratedHomeNoDeduct,
                    "Returning home, outpost=0, caravan=120",
                    string.Format("state={0}, outpost={1}, car={2}, destMatch={3}",
                        legacyM.state, CountOutpostSilver(outpostLegacy),
                        OutpostCaravanLogistics.CountCaravanSilver(legCar),
                        legCar.pather != null && legCar.pather.Destination == map.Tile));

                DestroyCaravanAndPawns(legCar);
                mgr.AuditAndCleanupMissions(false);
                outpostLegacy.Destroy();
            }
        }

        // ---------------- PHASE 1: VANILLA FORMATION -> REAL RETURN HOME GIZMO & DESTRUCTION ----------------

        private void BeginPhase_CancelAndDestroy(OutpostCaravanManager mgr)
        {
            ResetStockpileSupplies(new int[] { 200 }, 30);
            m_activeOutpost = CreateTestOutpost(2, 4, "ProbeOutpostCancel");
            m_activeMission = null;
            m_phaseTicks = 0;

            if (m_activeOutpost != null)
            {
                Pawn c1 = SpawnCleanColonist("CancelCarrier", m_edgeAnchor);
                string fail;
                bool started = OutpostCaravanLogistics.StartCaravan(m_activeOutpost, map, new List<Pawn> { c1 }, new List<Pawn>(), 100, out fail);
                m_activeMission = FindMissionForOutpost(mgr, m_activeOutpost);
                AddCheck("Phase1_StartCaravanAccepted", started && m_activeMission != null, "started",
                    string.Format("started={0}, fail={1}", started, fail ?? "none"));
            }
            m_phase = FreshPhase.WaitFormation_CancelAndDestroy;
        }

        private void CompletePhase_CancelAndDestroy(OutpostCaravanManager mgr)
        {
            Caravan car = m_activeMission != null ? m_activeMission.caravan : null;
            bool vanillaLoaded = car != null && m_activeMission != null &&
                                 m_activeMission.state == OutpostMissionState.Outbound &&
                                 m_activeMission.loadedSilver == 100 &&
                                 m_activeMission.missionSilverIds.Count >= 1 &&
                                 OutpostCaravanLogistics.CountCaravanSilver(car) == 100;

            bool clickedGizmo = ClickCaravanGizmo(car, "TMM_OutpostCaravan_ReturnHome".Translate());
            bool returnedRoute = car != null && car.pather != null && car.pather.Destination == map.Tile &&
                                 m_activeMission != null && m_activeMission.cancelled &&
                                 m_activeMission.state == OutpostMissionState.Returning &&
                                 OutpostCaravanLogistics.CountCaravanSilver(car) == 100;

            AddCheck("R2_VanillaLoadAndCancelGizmo_CargoIntact", vanillaLoaded && clickedGizmo && returnedRoute,
                "vanilla loaded 100, gizmo clicked, dest=home, silver=100",
                string.Format("vanilla={0}(loaded={1}), gizmo={2}, ret={3}",
                    vanillaLoaded, m_activeMission != null ? m_activeMission.loadedSilver : -1, clickedGizmo, returnedRoute));

            OutpostCaravanMission doomed = m_activeMission;
            DestroyCaravanAndPawns(car);
            mgr.AuditAndCleanupMissions(false);
            AddCheck("P1_CaravanDestroyed_MissionCleaned", doomed != null && !mgr.Missions.Contains(doomed),
                "removed", (doomed != null && !mgr.Missions.Contains(doomed)).ToString());

            if (m_activeOutpost != null && !m_activeOutpost.Destroyed) m_activeOutpost.Destroy();
            m_activeOutpost = null;
            m_activeMission = null;
        }

        // ---------------- PHASE 2: VANILLA FORMATION -> OUTPOST DELETED AFTER EXIT ----------------

        private void BeginPhase_OutpostDeleteAfterExit(OutpostCaravanManager mgr)
        {
            ResetStockpileSupplies(new int[] { 200 }, 30);
            m_activeOutpost = CreateTestOutpost(2, 5, "ProbeOutpostAfter");
            m_activeMission = null;
            m_phaseTicks = 0;

            if (m_activeOutpost != null)
            {
                Pawn p = SpawnCleanColonist("AfterDel", m_edgeAnchor);
                string fail;
                OutpostCaravanLogistics.StartCaravan(m_activeOutpost, map, new List<Pawn> { p }, new List<Pawn>(), 100, out fail);
                m_activeMission = FindMissionForOutpost(mgr, m_activeOutpost);
            }
            m_phase = FreshPhase.WaitFormation_OutpostDeleteAfterExit;
        }

        private void CompletePhase_OutpostDeleteAfterExit(OutpostCaravanManager mgr)
        {
            Caravan car = m_activeMission != null ? m_activeMission.caravan : null;
            Log.Message("[TMM probe] del diag before: silver=" + OutpostCaravanLogistics.CountCaravanSilver(car) + " destroyed=" + (car != null && car.Destroyed) + " spawned=" + (car != null && car.Spawned) + " sameCar=" + (m_activeMission != null && m_activeMission.caravan == car));
            if (m_activeOutpost != null && !m_activeOutpost.Destroyed) m_activeOutpost.Destroy();
            Log.Message("[TMM probe] del diag after destroy: silver=" + OutpostCaravanLogistics.CountCaravanSilver(car) + " destroyed=" + (car != null && car.Destroyed) + " state=" + (m_activeMission != null ? m_activeMission.state.ToString() : "null"));
            mgr.AuditAndCleanupMissions(false);
            Log.Message("[TMM probe] del diag after audit: silver=" + OutpostCaravanLogistics.CountCaravanSilver(car) + " destroyed=" + (car != null && car.Destroyed) + " state=" + (m_activeMission != null ? m_activeMission.state.ToString() : "null") + " dest=" + (car != null && car.pather != null ? car.pather.Destination.ToString() : "-") + " home=" + map.Tile + " moving=" + (car != null && car.pather != null && car.pather.Moving));

            bool autoTurnedAround = car != null && m_activeMission != null &&
                                    m_activeMission.state == OutpostMissionState.Returning &&
                                    car.pather != null && car.pather.Destination == map.Tile &&
                                    OutpostCaravanLogistics.CountCaravanSilver(car) == 100;
            AddCheck("P1_OutpostDeletedAfterExit_AutoReturnWithCargo", autoTurnedAround,
                "state=Returning, dest=home, silver=100",
                string.Format("state={0}, destMatch={1}, silver={2}",
                    m_activeMission != null ? m_activeMission.state.ToString() : "null",
                    car != null && car.pather != null && car.pather.Destination == map.Tile,
                    OutpostCaravanLogistics.CountCaravanSilver(car)));

            DestroyCaravanAndPawns(car);
            mgr.AuditAndCleanupMissions(false);
            m_activeOutpost = null;
            m_activeMission = null;
        }

        // ---------------- PHASE 3: WORKER OVERFLOW KEEPS CONFIRMED SILVER ----------------

        private void BeginPhase_WorkerOverflow(OutpostCaravanManager mgr)
        {
            ResetStockpileSupplies(new int[] { 150 }, 40);
            m_activeOutpost = CreateTestOutpost(2, 3, "ProbeOutpostOverflow");
            m_activeMission = null;
            m_overflowWorker = null;
            m_phaseTicks = 0;

            if (m_activeOutpost != null)
            {
                int cap = m_activeOutpost.MaxCapacity;
                while (m_activeOutpost.Workers.Count < cap)
                {
                    PawnGenerationRequest req = new PawnGenerationRequest(
                        PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                        canGeneratePawnRelations: false, allowAddictions: false); // CHECK API
                    m_activeOutpost.Workers.TryAdd(PawnGenerator.GeneratePawn(req), false);
                }

                Pawn carrier = SpawnCleanColonist("OverflowCarrier", m_edgeAnchor);
                m_overflowWorker = SpawnCleanColonist("OverflowWorker", m_edgeAnchor);
                string fail;
                OutpostCaravanLogistics.StartCaravan(m_activeOutpost, map,
                    new List<Pawn> { carrier, m_overflowWorker },
                    new List<Pawn> { m_overflowWorker },
                    100, out fail);
                m_activeMission = FindMissionForOutpost(mgr, m_activeOutpost);
            }
            m_phase = FreshPhase.WaitFormation_WorkerOverflow;
        }

        private void CompletePhase_WorkerOverflow(OutpostCaravanManager mgr)
        {
            Caravan car = m_activeMission != null ? m_activeMission.caravan : null;
            int cap = m_activeOutpost != null ? m_activeOutpost.MaxCapacity : 0;

            for (int t = 0; t < 200000 && car != null && !car.Destroyed && m_activeMission != null; t++)
            {
                if (m_activeMission.delivered && m_activeMission.state == OutpostMissionState.Returning) break;
                TickCaravanReal(car);
            }

            bool silverKeptAndWorkerSafe = m_activeMission != null && m_activeMission.silverDelivered && m_activeMission.delivered &&
                                           CountOutpostSilver(m_activeOutpost) == 100 &&
                                           m_activeOutpost != null && m_activeOutpost.Workers.Count == cap &&
                                           car != null && !car.Destroyed && car.ContainsPawn(m_overflowWorker);
            AddCheck("R2_WorkerOverflow_SilverConfirmedAndWorkerSafeInCaravan", silverKeptAndWorkerSafe,
                "silverDelivered=true(100), worker stays in caravan, cap unchanged",
                string.Format("silvDel={0}, outpostSilv={1}, workerInCar={2}, workers={3}/{4}",
                    m_activeMission != null && m_activeMission.silverDelivered,
                    CountOutpostSilver(m_activeOutpost),
                    car != null && car.ContainsPawn(m_overflowWorker),
                    m_activeOutpost != null ? m_activeOutpost.Workers.Count : -1, cap));

            DestroyCaravanAndPawns(car);
            mgr.AuditAndCleanupMissions(false);
            if (m_activeOutpost != null && !m_activeOutpost.Destroyed) m_activeOutpost.Destroy();
            m_activeOutpost = null;
            m_activeMission = null;
            m_overflowWorker = null;
        }

        // ---------------- PHASE 4: MAIN REAL MISSION — 2 SILVER STACKS + PERSONAL SILVER -> SAVE ON OUTBOUND LEG ----------------

        private void BeginPhase_MainOutboundSave(OutpostCaravanManager mgr)
        {
            // Place TWO separate 80-silver stacks (160 total) so sending 150 produces 2 distinct tagged mission stacks (80 + 70)!
            ResetStockpileSupplies(new int[] { 80, 80 }, 40);
            m_activeOutpost = CreateTestOutpost(2, 3, "ProbeRealOutpost");
            m_activeMission = null;
            m_phaseTicks = 0;

            if (m_activeOutpost != null)
            {
                m_mainCarrier = SpawnCleanColonist(ProbeReturnCarrierNick, m_edgeAnchor);
                m_mainWorker = SpawnCleanColonist(ProbeStayWorkerNick, m_edgeAnchor);
                m_mainCarrier.Name = new NameTriple("Probe", ProbeReturnCarrierNick, "Carrier");
                m_mainWorker.Name = new NameTriple("Probe", ProbeStayWorkerNick, "Worker");

                // Give carrier 50 PERSONAL silver BEFORE formation (must NEVER merge or be deducted!)
                Thing personal = ThingMaker.MakeThing(ThingDefOf.Silver);
                personal.stackCount = 50;
                m_mainCarrier.inventory.innerContainer.TryAdd(personal, false);

                if (m_mainCarrier.needs != null && m_mainCarrier.needs.food != null) m_mainCarrier.needs.food.CurLevelPercentage = 0.25f;
                if (m_mainWorker.needs != null && m_mainWorker.needs.food != null) m_mainWorker.needs.food.CurLevelPercentage = 0.25f;

                string fail;
                bool started = OutpostCaravanLogistics.StartCaravan(
                    m_activeOutpost, map,
                    new List<Pawn> { m_mainCarrier, m_mainWorker },
                    new List<Pawn> { m_mainWorker },
                    150, out fail);
                m_activeMission = FindMissionForOutpost(mgr, m_activeOutpost);
                AddCheck("MainOutbound_StartCaravan", started && m_activeMission != null && m_activeMission.missionSilverIds.Count == 2,
                    "started with 2 tagged mission silver stacks",
                    string.Format("started={0}, ids={1}, fail={2}",
                        started, m_activeMission != null ? m_activeMission.missionSilverIds.Count : -1, fail ?? "none"));
            }
            m_phase = FreshPhase.WaitFormation_MainOutboundSave;
        }

        private void CompletePhase_MainOutboundSave(OutpostCaravanManager mgr)
        {
            Caravan car = m_activeMission != null ? m_activeMission.caravan : null;

            bool outboundFormedCleanly = car != null && m_activeMission != null &&
                                         m_activeMission.state == OutpostMissionState.Outbound &&
                                         !m_activeMission.delivered && !m_activeMission.silverDelivered &&
                                         m_activeMission.loadedSilver == 150 &&
                                         m_activeMission.missionSilverIds.Count >= 2 &&
                                         OutpostCaravanLogistics.CountCaravanSilver(car) == 200 &&
                                         car.pather != null && car.pather.Moving &&
                                         car.pather.Destination == m_activeOutpost.Tile &&
                                         car.pather.ArrivalAction is CaravanArrivalAction_OutpostDelivery;

            // Advance 400 ticks along the OUTBOUND leg (still en route TO outpost!)
            for (int t = 0; t < 400 && car != null && !car.Destroyed && m_activeMission != null && m_activeMission.state == OutpostMissionState.Outbound; t++)
            {
                TickCaravanReal(car);
            }

            bool stillOutboundAtSave = m_activeMission != null && m_activeMission.state == OutpostMissionState.Outbound && !m_activeMission.delivered;
            AddCheck("R2_MainOutbound_VanillaFormedAndSavedEnRoute",
                outboundFormedCleanly && stillOutboundAtSave,
                "Outbound, loaded=150 across >=2 stacks, total=200 (150+50 personal), still Outbound at save",
                string.Format("formed={0}, stillOut={1}, loaded={2}, ids={3}, totalSilv={4}",
                    outboundFormedCleanly, stillOutboundAtSave,
                    m_activeMission != null ? m_activeMission.loadedSilver : -1,
                    m_activeMission != null ? m_activeMission.missionSilverIds.Count : -1,
                    OutpostCaravanLogistics.CountCaravanSilver(car)));

            // Shift 20 of carrier's PERSONAL silver onto stayWorker BEFORE saving to prove moved personal silver
            // is STILL never mistaken for mission cargo after reload!
            if (car != null && m_mainCarrier != null && m_mainWorker != null)
            {
                ShiftPersonalSilver(m_mainCarrier, m_mainWorker, m_activeMission.missionSilverIds, 20);
            }

            GameDataSaveLoader.SaveGame(OutpostCaravanProbeBootstrap.SaveName); // CHECK API
        }

        // =====================================================================
        // RELOAD PROBE: OUTBOUND RESTORE -> REAL ARRIVAL 2ND-STACK REFUSAL -> RETRY GIZMO -> RETURN HOME
        // =====================================================================

        private void RunReloadProbe()
        {
            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            AddCheck("Reload_ManagerExists", mgr != null, "non-null", mgr != null ? "ok" : "null");
            if (mgr == null) return;

            OutpostCaravanMission outMission = null;
            for (int i = 0; i < mgr.Missions.Count; i++)
            {
                OutpostCaravanMission m = mgr.Missions[i];
                if (m != null && m.state == OutpostMissionState.Outbound && m.caravan != null && m.caravan.Spawned)
                {
                    outMission = m;
                    break;
                }
            }

            bool restoredOutbound = outMission != null && !outMission.delivered && !outMission.silverDelivered &&
                                    outMission.loadedSilver == 150 &&
                                    outMission.missionSilverIds != null && outMission.missionSilverIds.Count >= 2 &&
                                    outMission.caravan.pather != null &&
                                    outMission.caravan.pather.Destination == outMission.outpost.Tile &&
                                    outMission.caravan.pather.ArrivalAction is CaravanArrivalAction_OutpostDelivery;
            AddCheck("R2_Reload_OutboundMissionRestored", restoredOutbound,
                "Outbound restored, loaded=150, >=2 tagged stacks, OutpostDelivery action",
                outMission != null
                    ? string.Format("state={0}, loaded={1}, ids={2}, act={3}",
                        outMission.state, outMission.loadedSilver,
                        outMission.missionSilverIds != null ? outMission.missionSilverIds.Count : -1,
                        outMission.caravan.pather != null && outMission.caravan.pather.ArrivalAction != null ? outMission.caravan.pather.ArrivalAction.GetType().Name : "null")
                    : "null");

            if (outMission == null) return;

            Caravan car = outMission.caravan;
            StrategicOutpost outp = outMission.outpost;
            // Make the travellers hungry so a short trip still triggers natural caravan eating.
            foreach (Pawn hp in car.PawnsListForReading) if (hp.needs != null && hp.needs.food != null) hp.needs.food.CurLevelPercentage = 0.05f;
            int mealsBeforeArrival = CountCaravanMeals(car);

            if (TestMode.Enabled)
            {
                // Reject on chunk index 1 -> chunk 0 enters RawMaterials, chunk 1 fails, chunk 0 rolls back!
                OutpostCaravanLogistics.TestRejectRawMaterialsTransferOnStep = 1;

                // Tick naturally until REAL vanilla PatherArrived clears arrivalAction and invokes Arrived(car)!
                for (int t = 0; t < 200000 && !car.Destroyed; t++)
                {
                    if (outMission.state == OutpostMissionState.RetryDelivery || outMission.delivered) break;
                    TickCaravanReal(car);
                }

                OutpostCaravanLogistics.TestRejectRawMaterialsTransferOnStep = -1;

                bool arrivalClearedAction = car.pather != null && car.pather.ArrivalAction == null;
                bool enteredRetryState = outMission.state == OutpostMissionState.RetryDelivery &&
                                         !outMission.silverDelivered && !outMission.delivered;
                bool stack1RolledBack = CountOutpostSilver(outp) == 0 &&
                                        OutpostCaravanLogistics.CountCaravanSilver(car) == 200;

                AddCheck("R2_Reload_RealArrivalSecondStackRefusal_RollbackAndRetryState",
                    arrivalClearedAction && enteredRetryState && stack1RolledBack,
                    "arrivalAction==null, state=RetryDelivery, outpost=0, caravan=200",
                    string.Format("cleared={0}, state={1}, outpost={2}, carSilv={3}",
                        arrivalClearedAction, outMission.state, CountOutpostSilver(outp), OutpostCaravanLogistics.CountCaravanSilver(car)));

                // Click the real "Retry Delivery" gizmo on the caravan!
                bool clickedRetry = ClickCaravanGizmo(car, "TMM_OutpostCaravan_RetryDelivery".Translate());
                AddCheck("R2_Reload_RetryDeliveryGizmoSucceeded",
                    clickedRetry && outMission.silverDelivered && outMission.delivered && outMission.state == OutpostMissionState.Returning,
                    "clicked=true, silverDelivered=true, state=Returning",
                    string.Format("clicked={0}, silvDel={1}, deliv={2}, state={3}",
                        clickedRetry, outMission.silverDelivered, outMission.delivered, outMission.state));
            }
            else
            {
                for (int t = 0; t < 200000 && !car.Destroyed; t++)
                {
                    if (outMission.delivered && outMission.state == OutpostMissionState.Returning) break;
                    TickCaravanReal(car);
                }
            }

            // Verify exact silver split: outpost got ONLY 150 mission silver; personal silver (30 + 20 = 50) untouched!
            int outpostSilverAfter = CountOutpostSilver(outp);
            int carrierSilverAfter = OutpostCaravanLogistics.CountCaravanSilver(car);
            int workerPersonalInOutpost = CountWorkerPersonalSilver(outp);
            bool personalUntouched = outpostSilverAfter == 150 && (carrierSilverAfter + workerPersonalInOutpost) == 50;
            AddCheck("R2_Reload_PersonalSilverUntouchedAfterShift", personalUntouched,
                "outpostRaw=150, personalTotal=50",
                string.Format("outpostRaw={0}, carSilv={1}, workerPersonal={2}", outpostSilverAfter, carrierSilverAfter, workerPersonalInOutpost));

            int mealsAtTurnaround = CountCaravanMeals(car);
            AddCheck("RealTrip_FoodConsumedNaturally", mealsAtTurnaround < mealsBeforeArrival,
                string.Format("< {0}", mealsBeforeArrival), mealsAtTurnaround.ToString());

            // Tick returning caravan naturally until carrier enters home map!
            for (int t = 0; t < 200000 && !car.Destroyed && car.Spawned; t++)
            {
                TickCaravanReal(car);
            }
            mgr.AuditAndCleanupMissions(false);

            Pawn returnedCarrier = FindColonistByShortName(map, ProbeReturnCarrierNick);
            bool carrierBackHome = returnedCarrier != null;
            bool outpostKeptCargo = outp != null && CountOutpostSilver(outp) == 150 && outp.Workers.Count >= 1;
            bool missionClosed = !mgr.Missions.Contains(outMission);

            AddCheck("Reload_CarrierEnteredHomeAndMissionClosed",
                carrierBackHome && outpostKeptCargo && missionClosed,
                "carrierHome=true, outpostSilver=150 & worker>=1, missionClosed=true",
                string.Format("carrierHome={0}, outpostOk={1}, closed={2}", carrierBackHome, outpostKeptCargo, missionClosed));
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static bool ClickCaravanGizmo(Caravan car, string expectedLabel)
        {
            if (car == null || expectedLabel.NullOrEmpty()) return false;
            foreach (Gizmo g in car.GetGizmos())
            {
                Command_Action cmd = g as Command_Action;
                if (cmd != null && cmd.defaultLabel == expectedLabel && cmd.action != null)
                {
                    cmd.action();
                    return true;
                }
            }
            return false;
        }

        private static void ShiftPersonalSilver(Pawn fromPawn, Pawn toPawn, List<string> missionSilverIds, int amount)
        {
            if (fromPawn == null || toPawn == null || amount <= 0) return;
            HashSet<string> missionSet = missionSilverIds != null ? new HashSet<string>(missionSilverIds) : new HashSet<string>();
            ThingOwner<Thing> inv = fromPawn.inventory != null ? fromPawn.inventory.innerContainer : null;
            if (inv == null) return;

            for (int i = 0; i < inv.Count; i++)
            {
                Thing t = inv[i];
                if (t != null && !t.Destroyed && t.def == ThingDefOf.Silver && !missionSet.Contains(t.ThingID) && t.stackCount >= amount)
                {
                    Thing split = t.SplitOff(amount);
                    if (split != null)
                    {
                        if (split.holdingOwner != null) split.holdingOwner.Remove(split);
                        toPawn.inventory.innerContainer.TryAdd(split, true);
                    }
                    return;
                }
            }
        }

        private static int CountWorkerPersonalSilver(StrategicOutpost o)
        {
            if (o == null || o.Workers == null) return 0;
            int sum = 0;
            for (int i = 0; i < o.Workers.Count; i++)
            {
                sum += OutpostCaravanLogistics.CountPawnInventoryThing(o.Workers[i], ThingDefOf.Silver);
            }
            return sum;
        }

        private static void TickCaravanReal(Caravan car)
        {
            if (car == null || car.Destroyed || !car.Spawned) return;
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 1); // CHECK API
            car.DoTick(); // CHECK API
            List<Pawn> pawns = car.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && !pawns[i].Dead) pawns[i].DoTick(); // CHECK API
            }
        }

        private static IntVec3 FindEdgeStagingAnchor(Map targetMap)
        {
            IntVec3 edge;
            if (CellFinder.TryFindRandomEdgeCellWith(delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap) && !c.Fogged(targetMap);
            }, targetMap, CellFinder.EdgeRoadChance_Always, out edge))
            {
                IntVec3 dir = targetMap.Center - edge;
                int dx = dir.x == 0 ? 0 : (dir.x > 0 ? 1 : -1);
                int dz = dir.z == 0 ? 0 : (dir.z > 0 ? 1 : -1);
                IntVec3 inward = edge + new IntVec3(dx * 10, 0, dz * 10);
                IntVec3 standable;
                if (CellFinder.TryFindRandomCellNear(inward, targetMap, 6, c => c.InBounds(targetMap) && c.Standable(targetMap) && !c.Fogged(targetMap), out standable))
                {
                    return standable;
                }
            }
            return targetMap.Center;
        }

        private static Zone_Stockpile EnsureEdgeStockpile(Map targetMap, IntVec3 anchor)
        {
            if (targetMap == null || targetMap.zoneManager == null) return null;
            Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, targetMap.zoneManager); // CHECK API
            targetMap.zoneManager.RegisterZone(zone);
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    IntVec3 c = anchor + new IntVec3(dx, 0, dz);
                    if (c.InBounds(targetMap) && c.Standable(targetMap) && !c.Fogged(targetMap) && targetMap.zoneManager.ZoneAt(c) == null)
                    {
                        zone.AddCell(c);
                        if (targetMap.areaManager != null && targetMap.areaManager.Home != null)
                        {
                            targetMap.areaManager.Home[c] = true;
                        }
                    }
                }
            }
            return zone;
        }

        private void ResetStockpileSupplies(int[] silverStacks, int mealCount)
        {
            ClearMapResource(ThingDefOf.Silver);
            ClearMapResource(ThingDefOf.MealSurvivalPack);

            int cellIdx = 0;
            if (silverStacks != null)
            {
                for (int i = 0; i < silverStacks.Length; i++)
                {
                    if (silverStacks[i] > 0)
                    {
                        SpawnInStockpile(ThingDefOf.Silver, silverStacks[i], cellIdx);
                        cellIdx++;
                    }
                }
            }

            if (mealCount > 0)
            {
                SpawnInStockpile(ThingDefOf.MealSurvivalPack, mealCount, cellIdx);
            }
        }

        private void ClearMapResource(ThingDef def)
        {
            if (map == null || def == null || map.listerThings == null) return;
            List<Thing> snapshot = new List<Thing>(map.listerThings.ThingsOfDef(def));
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing t = snapshot[i];
                if (t != null && !t.Destroyed && t.Spawned)
                {
                    t.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private Thing SpawnInStockpile(ThingDef def, int count, int cellIndex)
        {
            IntVec3 cell = (m_stockpile != null && m_stockpile.cells.Count > 0)
                ? m_stockpile.cells[cellIndex % m_stockpile.cells.Count]
                : m_edgeAnchor;

            // Respect stackLimit: spawn several stacks on consecutive stockpile cells instead of one oversized stack.
            Thing first = null;
            int remaining = count;
            int limit = def.stackLimit > 0 ? def.stackLimit : count;
            int idx = cellIndex;
            while (remaining > 0)
            {
                if (m_stockpile != null && m_stockpile.cells.Count > 0) cell = m_stockpile.cells[idx % m_stockpile.cells.Count];
                idx++;
                Thing t = ThingMaker.MakeThing(def);
                t.stackCount = Math.Min(remaining, limit);
                remaining -= t.stackCount;
                GenSpawn.Spawn(t, cell, map, WipeMode.Vanish);
                t.SetForbidden(false, false);
                if (map.areaManager != null && map.areaManager.Home != null && t.Position.InBounds(map))
                {
                    map.areaManager.Home[t.Position] = true;
                }
                if (first == null) first = t;
            }
            return first;
        }

        private StrategicOutpost CreateTestOutpost(int minDist, int maxDist, string label)
        {
            PlanetTile tile;
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(map.Tile, minDist, maxDist, out tile, delegate(PlanetTile t)
            {
                return !Find.WorldObjects.AnyWorldObjectAt(t);
            })) // CHECK API
            {
                return null;
            }

            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("TMM_StrategicOutpost") ??
                                 DefDatabase<WorldObjectDef>.AllDefsListForReading.FirstOrDefault(d => d.worldObjectClass == typeof(StrategicOutpost));
            if (def == null) return null;

            StrategicOutpost o = (StrategicOutpost)WorldObjectMaker.MakeWorldObject(def);
            o.Tile = tile;
            o.SetFaction(Faction.OfPlayer);
            o.Name = label;
            Find.WorldObjects.Add(o);
            return o;
        }

        private Pawn SpawnCleanColonist(string nick, IntVec3 near)
        {
            PawnGenerationRequest req = new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                canGeneratePawnRelations: false,
                allowAddictions: false); // CHECK API
            Pawn p = PawnGenerator.GeneratePawn(req);
            p.Name = new NameTriple("Probe", nick, "Tester");
            Log.Message("[TMM probe] colonist " + nick + ": haulingDisabled=" + p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling) + " manualDumbDisabled=" + p.WorkTagIsDisabled(WorkTags.ManualDumb));
            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(near, map, 4, c => c.InBounds(map) && c.Standable(map) && !c.Fogged(map), out cell))
            {
                cell = near;
            }
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            if (p.needs != null && p.needs.rest != null) p.needs.rest.CurLevel = 1f;
            if (p.needs != null && p.needs.food != null) p.needs.food.CurLevel = 0.9f;
            return p;
        }

        private static void CleanupCleanPawn(Pawn p)
        {
            if (p == null) return;
            if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
            if (Find.WorldPawns != null && Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
            if (p.relations != null) p.relations.ClearAllRelations();
            if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
        }

        private static void DestroyCaravanAndPawns(Caravan car)
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

        private static OutpostCaravanMission FindMissionForOutpost(OutpostCaravanManager mgr, StrategicOutpost o)
        {
            if (mgr == null || o == null) return null;
            for (int i = mgr.Missions.Count - 1; i >= 0; i--)
            {
                if (mgr.Missions[i] != null && mgr.Missions[i].outpost == o) return mgr.Missions[i];
            }
            return null;
        }

        private static int CountOutpostSilver(StrategicOutpost o)
        {
            if (o == null || o.RawMaterials == null) return 0;
            int sum = 0;
            for (int i = 0; i < o.RawMaterials.Count; i++)
            {
                if (o.RawMaterials[i] != null && o.RawMaterials[i].def == ThingDefOf.Silver) sum += o.RawMaterials[i].stackCount;
            }
            return sum;
        }

        private static int CountCaravanMeals(Caravan car)
        {
            if (car == null || car.Destroyed) return 0;
            int sum = 0;
            List<Pawn> pawns = car.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                sum += OutpostCaravanLogistics.CountPawnInventoryThing(pawns[i], ThingDefOf.MealSurvivalPack);
            }
            return sum;
        }

        private static Pawn FindColonistByShortName(Map m, string shortName)
        {
            if (m == null || m.mapPawns == null) return null;
            IReadOnlyList<Pawn> cols = m.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < cols.Count; i++)
            {
                if (cols[i] != null && cols[i].Name != null && cols[i].Name.ToStringShort == shortName) return cols[i];
            }
            return null;
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
                string uiPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "OutpostCaravanUiProbe.json");
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
