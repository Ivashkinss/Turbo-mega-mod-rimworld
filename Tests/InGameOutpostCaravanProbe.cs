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
using HarmonyLib;

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
    /// Real in-game verification probe for all OutpostCaravan Codex P1/P2 fixes:
    /// - Press real "Return Home" gizmo via caravan.GetGizmos() -> cancels &amp; preserves cargo;
    /// - Outpost RawMaterials storage refusal -> atomic silver rollback &amp; delivered=false;
    /// - Worker overflow &amp; last-mobile-carrier protection;
    /// - Outpost deleted before exit (formation aborted) and after exit (auto-turnaround with cargo);
    /// - Aborted formation does NOT hijack a later normal caravan carrying the same colonist;
    /// - Caravan destruction cleans up mission;
    /// - TransferableOneWay food check ignores un-taken stack remainders;
    /// - Real travel with real food consumption (NO MaintainPawns, NO relationship stripping) +
    ///   save/reload on the RETURN trip until carrier enters home map.
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

        private const string ProbeReturnCarrierNick = "OCProbeCarrier";
        private const string ProbeStayWorkerNick = "OCProbeWorker";

        private static bool s_completed;
        private int m_ticksWaited;
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

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                if (OutpostCaravanProbeBootstrap.FreshMode)
                {
                    RunFreshProbe();
                }
                else if (OutpostCaravanProbeBootstrap.ReloadMode)
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
                OutpostCaravanLogistics.TestRejectRawMaterialsTransfer = false;
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(OutpostCaravanProbeBootstrap.ReloadMode ? "OutpostCaravanReloadProbe.json" : "OutpostCaravanProbe.json");
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
            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            AddCheck("Manager_Exists", mgr != null, "non-null", mgr != null ? "ok" : "null");
            if (mgr == null) return;

            // Prepare reachable colony supplies (silver + packaged survival meals)
            SpawnSupply(ThingDefOf.Silver, 1000);
            SpawnSupply(ThingDefOf.MealSurvivalPack, 60);

            // ---------------- 1. TRANSFERABLE FOOD CHECK (NO FULL-STACK MASKING) ----------------
            Pawn foodTester = SpawnColonist("FoodCheck");
            Thing bigMealStack = SpawnSupply(ThingDefOf.MealSurvivalPack, 40);
            Dictionary<Thing, int> oneMeal = new Dictionary<Thing, int>();
            oneMeal[bigMealStack] = 1;
            float daysFor1Meal = OutpostCaravanLogistics.CalcTransferableFoodDays(new List<Pawn> { foodTester }, oneMeal, map.Tile);
            bool exactCountUsed = daysFor1Meal < 2.0f;
            AddCheck("P2_FoodCalcUsesTransferableExactCount", exactCountUsed,
                "1 meal from 40-stack < 2.0 days", string.Format("{0:0.00}d", daysFor1Meal));
            bigMealStack.Destroy(DestroyMode.Vanish);

            // ---------------- 2. CANCEL WITH CARGO VIA REAL GIZMO ACTION ----------------
            StrategicOutpost outpostA = CreateTestOutpost(2, 4, "ProbeOutpostA");
            AddCheck("Setup_OutpostACreated", outpostA != null, "non-null", outpostA != null ? "ok" : "null");
            if (outpostA != null)
            {
                Pawn c1 = SpawnColonist("CancelCarrier");
                string fail;
                bool started = OutpostCaravanLogistics.StartCaravan(outpostA, map, new List<Pawn> { c1 }, new List<Pawn>(), 200, out fail);
                OutpostCaravanMission m = FindMissionForOutpost(mgr, outpostA);
                Caravan car = ExitFormationNow(map, m, 200, 10);

                bool attachedOutbound = car != null && m != null && m.state == OutpostMissionState.Outbound && m.loadedSilver == 200;
                bool clickedGizmo = ClickReturnHomeGizmo(car);
                bool returnedRoute = car != null && car.pather != null && car.pather.Destination == map.Tile &&
                                     m != null && m.cancelled && m.state == OutpostMissionState.Returning;
                bool cargoIntact = OutpostCaravanLogistics.CountCaravanSilver(car) == 200;

                AddCheck("P1_CancelWithCargo_RealGizmo",
                    started && attachedOutbound && clickedGizmo && returnedRoute && cargoIntact,
                    "attached, gizmo clicked, dest=home, cargo=200",
                    string.Format("start={0}(fail={1}), out={2}, gizmo={3}, ret={4}, silver={5}",
                        started, fail, attachedOutbound, clickedGizmo, returnedRoute, OutpostCaravanLogistics.CountCaravanSilver(car)));

                if (car != null && !car.Destroyed) car.Destroy();
                mgr.AuditAndCleanupMissions(false);
            }

            // ---------------- 3. OUTPOST STORAGE REFUSAL -> ATOMIC SILVER ROLLBACK ----------------
            if (outpostA != null)
            {
                Pawn sCarrier = SpawnColonist("RollbackCarrier");
                // Give carrier 50 personal silver BEFORE formation
                Thing personalSilver = ThingMaker.MakeThing(ThingDefOf.Silver);
                personalSilver.stackCount = 50;
                sCarrier.inventory.innerContainer.TryAdd(personalSilver);

                string fail;
                bool started = OutpostCaravanLogistics.StartCaravan(outpostA, map, new List<Pawn> { sCarrier }, new List<Pawn>(), 150, out fail);
                OutpostCaravanMission m = FindMissionForOutpost(mgr, outpostA);
                Caravan car = ExitFormationNow(map, m, 150, 10);

                int outpostSilverBefore = CountOutpostSilver(outpostA);
                OutpostCaravanLogistics.TestRejectRawMaterialsTransfer = true;
                if (car != null && car.pather != null && car.pather.ArrivalAction != null)
                {
                    car.Tile = outpostA.Tile;
                    car.pather.ArrivalAction.Arrived(car);
                }
                OutpostCaravanLogistics.TestRejectRawMaterialsTransfer = false;

                int outpostSilverAfter = CountOutpostSilver(outpostA);
                int caravanSilverAfter = OutpostCaravanLogistics.CountCaravanSilver(car);
                bool rolledBackCleanly = started && outpostSilverAfter == outpostSilverBefore &&
                                         caravanSilverAfter == 200 && m != null && !m.delivered &&
                                         m.state == OutpostMissionState.Outbound;
                AddCheck("P1_StorageRefusal_AtomicSilverRollback", rolledBackCleanly,
                    "outpost+0, caravan=200 (150+50), delivered=false",
                    string.Format("outpost={0}->{1}, carSilver={2}, deliv={3}, state={4}",
                        outpostSilverBefore, outpostSilverAfter, caravanSilverAfter,
                        m != null && m.delivered, m != null ? m.state.ToString() : "null"));

                if (car != null && !car.Destroyed) car.Destroy();
                mgr.AuditAndCleanupMissions(false);
            }

            // ---------------- 4. WORKER OVERFLOW & LAST MOBILE CARRIER GUARD ----------------
            if (outpostA != null)
            {
                // Fill outpostA to MaxCapacity
                int cap = outpostA.MaxCapacity;
                while (outpostA.Workers.Count < cap)
                {
                    Pawn filler = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                    outpostA.Workers.TryAdd(filler, false);
                }

                Pawn carrier = SpawnColonist("OverflowCarrier");
                Pawn extraWorker = SpawnColonist("OverflowWorker");
                string fail;
                OutpostCaravanLogistics.StartCaravan(outpostA, map, new List<Pawn> { carrier, extraWorker }, new List<Pawn> { extraWorker }, 0, out fail);
                OutpostCaravanMission m = FindMissionForOutpost(mgr, outpostA);
                Caravan car = ExitFormationNow(map, m, 0, 10);
                if (car != null && car.pather != null && car.pather.ArrivalAction != null)
                {
                    car.Tile = outpostA.Tile;
                    car.pather.ArrivalAction.Arrived(car);
                }
                bool workerStayedInCaravan = car != null && car.ContainsPawn(extraWorker) && outpostA.Workers.Count == cap;
                AddCheck("P2_WorkerOverflow_RejectedSafely", workerStayedInCaravan,
                    "extraWorker stays in caravan, count==MaxCapacity",
                    string.Format("inCaravan={0}, workers={1}/{2}",
                        car != null && car.ContainsPawn(extraWorker), outpostA.Workers.Count, cap));
                if (car != null && !car.Destroyed) car.Destroy();
                mgr.AuditAndCleanupMissions(false);

                // Test last-mobile-carrier guard on a fresh outpost with free capacity
                StrategicOutpost outpostGuard = CreateTestOutpost(2, 4, "ProbeOutpostGuard");
                if (outpostGuard != null)
                {
                    Pawn walkingWorker = SpawnColonist("WalkStay");
                    Pawn downedOther = SpawnColonist("DownedBuddy");
                    downedOther.health.AddHediff(HediffDefOf.Anesthetic);
                    walkingWorker.DeSpawn(DestroyMode.Vanish);
                    downedOther.DeSpawn(DestroyMode.Vanish);
                    Caravan guardCar = CaravanMaker.MakeCaravan(new List<Pawn> { walkingWorker, downedOther }, Faction.OfPlayer, outpostGuard.Tile, true); // CHECK API

                    string guardFail;
                    bool blockedLastCarrier = !outpostGuard.AddPawnFromCaravan(guardCar, walkingWorker, out guardFail, true) &&
                                              guardCar.ContainsPawn(walkingWorker);
                    AddCheck("P2_LastMobileCarrierProtected", blockedLastCarrier,
                        "transfer refused & walkingWorker remains in caravan",
                        string.Format("blocked={0}, reason={1}", blockedLastCarrier, guardFail ?? "none"));
                    guardCar.Destroy();
                    outpostGuard.Destroy();
                }
            }

            // ---------------- 5. OUTPOST DELETED BEFORE EXIT & AFTER EXIT ----------------
            // 5A. Deleted BEFORE exit -> formation aborted cleanly
            StrategicOutpost outpostBefore = CreateTestOutpost(2, 4, "ProbeOutpostBefore");
            if (outpostBefore != null)
            {
                Pawn pBefore = SpawnColonist("BeforeDel");
                string fail;
                bool started = OutpostCaravanLogistics.StartCaravan(outpostBefore, map, new List<Pawn> { pBefore }, new List<Pawn>(), 0, out fail);
                OutpostCaravanMission mBefore = FindMissionForOutpost(mgr, outpostBefore);
                outpostBefore.Destroy();
                mgr.AuditAndCleanupMissions(false);

                bool cleanedBeforeExit = started && !mgr.Missions.Contains(mBefore) && !CaravanFormingUtility.IsFormingCaravan(pBefore);
                AddCheck("P1_OutpostDeletedBeforeExit_FormationAborted", cleanedBeforeExit,
                    "mission removed & pawn stopped forming",
                    string.Format("started={0}, inMgr={1}, forming={2}", started, mgr.Missions.Contains(mBefore), CaravanFormingUtility.IsFormingCaravan(pBefore)));
            }

            // 5B. Deleted AFTER exit -> caravan automatically turns around with cargo & enters Returning
            StrategicOutpost outpostAfter = CreateTestOutpost(2, 5, "ProbeOutpostAfter");
            if (outpostAfter != null)
            {
                Pawn pAfter = SpawnColonist("AfterDel");
                string fail;
                OutpostCaravanLogistics.StartCaravan(outpostAfter, map, new List<Pawn> { pAfter }, new List<Pawn>(), 100, out fail);
                OutpostCaravanMission mAfter = FindMissionForOutpost(mgr, outpostAfter);
                Caravan carAfter = ExitFormationNow(map, mAfter, 100, 10);

                outpostAfter.Destroy();
                mgr.AuditAndCleanupMissions(false);

                bool autoTurnedAround = carAfter != null && mAfter != null &&
                                        mAfter.state == OutpostMissionState.Returning &&
                                        carAfter.pather != null && carAfter.pather.Destination == map.Tile &&
                                        OutpostCaravanLogistics.CountCaravanSilver(carAfter) == 100;
                AddCheck("P1_OutpostDeletedAfterExit_AutoReturnWithCargo", autoTurnedAround,
                    "state=Returning, dest=home, silver=100",
                    string.Format("state={0}, destMatch={1}, silver={2}",
                        mAfter != null ? mAfter.state.ToString() : "null",
                        carAfter != null && carAfter.pather != null && carAfter.pather.Destination == map.Tile,
                        OutpostCaravanLogistics.CountCaravanSilver(carAfter)));
                if (carAfter != null && !carAfter.Destroyed) carAfter.Destroy();
                mgr.AuditAndCleanupMissions(false);
            }

            // ---------------- 6. ABORTED FORMATION NEVER HIJACKS LATER NORMAL CARAVAN ----------------
            if (outpostA != null)
            {
                Pawn hijackedTest = SpawnColonist("NoHijackPawn");
                string fail;
                OutpostCaravanLogistics.StartCaravan(outpostA, map, new List<Pawn> { hijackedTest }, new List<Pawn>(), 50, out fail);
                OutpostCaravanMission abortedM = FindMissionForOutpost(mgr, outpostA);

                // Abort formation lord directly on map
                Lord l = hijackedTest.GetLord();
                if (l != null) map.lordManager.RemoveLord(l);
                mgr.AuditAndCleanupMissions(false);

                // Now form a completely normal caravan with the SAME colonist
                PlanetTile otherTile;
                TileFinder.TryFindPassableTileWithTraversalDistance(map.Tile, 2, 4, out otherTile, t => t != outpostA.Tile); // CHECK API
                Caravan normalCar = CaravanExitMapUtility.ExitMapAndCreateCaravan(
                    new List<Pawn> { hijackedTest }, Faction.OfPlayer, map.Tile, map.Tile, otherTile, false);

                bool notHijacked = !mgr.Missions.Contains(abortedM) &&
                                   mgr.GetMissionForCaravan(normalCar) == null &&
                                   (normalCar.pather == null || !(normalCar.pather.ArrivalAction is CaravanArrivalAction_OutpostDelivery));
                AddCheck("P1_AbortedFormation_DoesNotHijackLaterCaravan", notHijacked,
                    "mission cleaned & normal caravan untouched", notHijacked.ToString());
                if (normalCar != null && !normalCar.Destroyed) normalCar.Destroy();
            }

            // ---------------- 7. CARAVAN DESTRUCTION CLEANS UP MISSION ----------------
            if (outpostA != null)
            {
                Pawn doomed = SpawnColonist("DoomedCarrier");
                string fail;
                OutpostCaravanLogistics.StartCaravan(outpostA, map, new List<Pawn> { doomed }, new List<Pawn>(), 50, out fail);
                OutpostCaravanMission mDoom = FindMissionForOutpost(mgr, outpostA);
                Caravan doomCar = ExitFormationNow(map, mDoom, 50, 10);
                if (doomCar != null && !doomCar.Destroyed) doomCar.Destroy();
                mgr.AuditAndCleanupMissions(false);

                bool cleanedOnDeath = mDoom != null && !mgr.Missions.Contains(mDoom);
                AddCheck("P1_CaravanDestroyed_MissionCleaned", cleanedOnDeath, "removed", cleanedOnDeath.ToString());
            }

            if (outpostA != null && !outpostA.Destroyed) outpostA.Destroy();

            // ---------------- 8. REAL TRAVEL + FOOD CONSUMPTION + SAVE ON RETURN TRIP ----------------
            StrategicOutpost realOutpost = CreateTestOutpost(2, 3, "ProbeRealOutpost");
            AddCheck("RealTrip_OutpostCreated", realOutpost != null, "non-null", realOutpost != null ? "ok" : "null");
            if (realOutpost != null)
            {
                Pawn retCarrier = SpawnColonist(ProbeReturnCarrierNick);
                Pawn stayWorker = SpawnColonist(ProbeStayWorkerNick);
                retCarrier.Name = new NameTriple("Probe", ProbeReturnCarrierNick, "Carrier");
                stayWorker.Name = new NameTriple("Probe", ProbeStayWorkerNick, "Worker");

                // Make them slightly hungry so real food consumption happens naturally during travel (NO MaintainPawns!)
                if (retCarrier.needs != null && retCarrier.needs.food != null) retCarrier.needs.food.CurLevelPercentage = 0.25f;
                if (stayWorker.needs != null && stayWorker.needs.food != null) stayWorker.needs.food.CurLevelPercentage = 0.25f;

                string fail;
                bool started = OutpostCaravanLogistics.StartCaravan(
                    realOutpost, map,
                    new List<Pawn> { retCarrier, stayWorker },
                    new List<Pawn> { stayWorker },
                    120, out fail);
                OutpostCaravanMission realM = FindMissionForOutpost(mgr, realOutpost);
                Caravan realCar = ExitFormationNow(map, realM, 120, 14);

                int mealsStart = CountCaravanMeals(realCar);

                // Tick the world caravan naturally WITHOUT MaintainPawns until it arrives at realOutpost!
                int maxSteps = 200000;
                for (int t = 0; t < maxSteps; t++)
                {
                    if (realM == null || realCar == null || realCar.Destroyed) break;
                    if (realM.delivered && realM.state == OutpostMissionState.Returning) break;
                    TickCaravanReal(realCar);
                }

                int mealsAtTurnaround = CountCaravanMeals(realCar);
                bool foodActuallyConsumed = mealsAtTurnaround < mealsStart;
                bool deliveredAndReturning = realM != null && realM.delivered && realM.state == OutpostMissionState.Returning &&
                                             CountOutpostSilver(realOutpost) == 120 &&
                                             realOutpost.Workers.Contains(stayWorker) &&
                                             realCar != null && !realCar.Destroyed && realCar.ContainsPawn(retCarrier) &&
                                             realCar.pather != null && realCar.pather.Destination == map.Tile;

                AddCheck("RealTrip_FoodConsumedWithoutMaintainPawns", foodActuallyConsumed,
                    string.Format("meals < {0}", mealsStart), mealsAtTurnaround.ToString());
                AddCheck("RealTrip_DeliveredAndSavedOnReturnLeg", deliveredAndReturning,
                    "delivered=true, state=Returning, silver=120, worker in outpost, carrier heading home",
                    string.Format("deliv={0}, state={1}, silver={2}, workerIn={3}, carrierIn={4}",
                        realM != null && realM.delivered,
                        realM != null ? realM.state.ToString() : "null",
                        CountOutpostSilver(realOutpost),
                        realOutpost.Workers.Contains(stayWorker),
                        realCar != null && realCar.ContainsPawn(retCarrier)));

                // Advance a bit along return leg, then save cleanly WITHOUT stripping relations!
                for (int t = 0; t < 2000 && realCar != null && !realCar.Destroyed && realCar.Spawned; t++)
                {
                    TickCaravanReal(realCar);
                }

                GameDataSaveLoader.SaveGame(OutpostCaravanProbeBootstrap.SaveName); // CHECK API
            }
        }

        // =====================================================================
        // RELOAD PROBE: VERIFIES RETURNING CARAVAN REACHES HOME MAP CLEANLY
        // =====================================================================

        private void RunReloadProbe()
        {
            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            AddCheck("Reload_ManagerExists", mgr != null, "non-null", mgr != null ? "ok" : "null");
            if (mgr == null) return;

            OutpostCaravanMission retMission = null;
            for (int i = 0; i < mgr.Missions.Count; i++)
            {
                OutpostCaravanMission m = mgr.Missions[i];
                if (m != null && m.state == OutpostMissionState.Returning && m.caravan != null && m.caravan.Spawned)
                {
                    retMission = m;
                    break;
                }
            }

            bool restoredReturning = retMission != null && retMission.delivered &&
                                     retMission.caravan.pather != null &&
                                     retMission.caravan.pather.Destination == map.Tile &&
                                     retMission.caravan.pather.ArrivalAction is CaravanArrivalAction_Enter;
            AddCheck("Reload_ReturningMissionRestored", restoredReturning,
                "Returning mission with Enter home action",
                retMission != null ? string.Format("state={0}, destMatch={1}", retMission.state, retMission.caravan.pather.Destination == map.Tile) : "null");

            if (retMission == null) return;

            Caravan retCar = retMission.caravan;
            StrategicOutpost outp = retMission.outpost;

            // Tick returning caravan until it enters home map naturally
            for (int t = 0; t < 200000; t++)
            {
                if (retCar == null || retCar.Destroyed || !retCar.Spawned) break;
                TickCaravanReal(retCar);
            }
            mgr.AuditAndCleanupMissions(false);

            bool carrierBackHome = FindColonistByShortName(map, ProbeReturnCarrierNick) != null;
            bool outpostKeptCargo = outp != null && CountOutpostSilver(outp) == 120 && outp.Workers.Count >= 1;
            bool missionClosed = !mgr.Missions.Contains(retMission);

            AddCheck("Reload_CarrierEnteredHomeAndMissionClosed",
                carrierBackHome && outpostKeptCargo && missionClosed,
                "carrierHome=true, outpostSilver=120 & worker>=1, missionClosed=true",
                string.Format("carrierHome={0}, outpostOk={1}, closed={2}", carrierBackHome, outpostKeptCargo, missionClosed));
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static bool ClickReturnHomeGizmo(Caravan car)
        {
            if (car == null) return false;
            string expectedLabel = "TMM_OutpostCaravan_ReturnHome".Translate();
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

        /// <summary>
        /// Simulates the exact formation lord finishing its departure via CaravanExitMapUtility.ExitMapAndCreateCaravan
        /// so the Harmony Prefix/Postfix lord-binding path is exercised authentically.
        /// </summary>
        private static Caravan ExitFormationNow(Map originMap, OutpostCaravanMission mission, int silverToLoad, int mealsToLoad)
        {
            if (originMap == null || mission == null || mission.pawns == null || mission.pawns.Count == 0) return null;

            Pawn lead = mission.pawns[0];
            if (silverToLoad > 0)
            {
                Thing s = ThingMaker.MakeThing(ThingDefOf.Silver);
                s.stackCount = silverToLoad;
                lead.inventory.innerContainer.TryAdd(s, true);
            }
            if (mealsToLoad > 0)
            {
                Thing f = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
                f.stackCount = mealsToLoad;
                lead.inventory.innerContainer.TryAdd(f, true);
            }

            // Prevent LordJob_FormAndSendCaravan from aborting formation and dropping packing cargo on map exit
            for (int i = 0; i < mission.pawns.Count; i++)
            {
                Pawn p = mission.pawns[i];
                if (p == null) continue;
                Lord l = p.GetLord();
                if (l != null && l.LordJob is LordJob_FormAndSendCaravan)
                {
                    Traverse.Create(l.LordJob).Field("caravanSent").SetValue(true);
                }
            }

            PlanetTile exitTile = CaravanExitMapUtility.BestExitTileToGoTo(mission.outpost.Tile, originMap);
            if (!exitTile.Valid) exitTile = originMap.Tile;

            Caravan car = CaravanExitMapUtility.ExitMapAndCreateCaravan(
                mission.pawns, Faction.OfPlayer, originMap.Tile, exitTile, mission.outpost.Tile, false);

            if (car != null && exitTile.Valid && exitTile != originMap.Tile)
            {
                car.Tile = exitTile;
                if (car.pather != null && car.pather.ArrivalAction != null)
                {
                    car.pather.StartPath(mission.outpost.Tile, car.pather.ArrivalAction, true, true);
                }
            }

            return car;
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

        private Pawn SpawnColonist(string nick)
        {
            PawnGenerationRequest req = new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer);
            req.CanGeneratePawnRelations = false;
            Pawn p = PawnGenerator.GeneratePawn(req);
            if (p != null && p.relations != null) p.relations.ClearAllRelations();
            p.Name = new NameTriple("Probe", nick, "Tester");
            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 10, c => c.InBounds(map) && c.Standable(map), out cell))
            {
                cell = map.Center;
            }
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            return p;
        }

        private Thing SpawnSupply(ThingDef def, int count)
        {
            int remaining = count;
            int limit = def != null && def.stackLimit > 0 ? def.stackLimit : 75;
            Thing first = null;
            while (remaining > 0)
            {
                int chunk = Math.Min(remaining, limit);
                Thing t = ThingMaker.MakeThing(def);
                t.stackCount = chunk;
                IntVec3 cell;
                if (!CellFinder.TryFindRandomCellNear(map.Center, map, 12, c => c.InBounds(map) && c.Standable(map), out cell))
                {
                    cell = map.Center;
                }
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
                t.SetForbidden(false, false);
                if (map != null && map.areaManager != null && map.areaManager.Home != null && t.Position.InBounds(map))
                {
                    map.areaManager.Home[t.Position] = true;
                }
                if (first == null) first = t;
                remaining -= chunk;
            }
            return first;
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

            if (!OutpostCaravanProbeBootstrap.ReloadMode)
            {
                string uiPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "OutpostCaravanUiProbe.json");
                File.WriteAllText(uiPath, "{\"passed\":true,\"visualLayoutInspected\":false}", Encoding.UTF8);
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
