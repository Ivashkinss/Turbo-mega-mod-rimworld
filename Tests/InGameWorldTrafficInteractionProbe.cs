using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class InGameWorldTrafficInteractionProbe
    {
        private const string ProbeFlag = "-tmm-traffic-interaction-probe";
        private const string ReloadFlag = "-tmm-traffic-interaction-reload";
        private static bool reload;
        private const string ReportDirArgPrefix = "-tmm-probe-dir=";
        private const string MainReportFileName = "WorldTrafficInteractionProbe.json";
        private const string UiReportFileName = "WorldTrafficInteractionUiProbe.json";

        private static readonly bool Enabled;
        private static readonly string CustomReportDir;
        private static readonly object LogLock = new object();
        private static readonly List<string> CapturedErrors = new List<string>();

        private static bool started;
        private static bool completed;
        private static int readyFrames;
        private static bool capturingLogs;

        static InGameWorldTrafficInteractionProbe()
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
                        LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-TrafficInteraction"));
                    }
                    else if (arg.StartsWith(ReportDirArgPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomReportDir = arg.Substring(ReportDirArgPrefix.Length).Trim('"');
                    }
                }
            }

            if (!Enabled) return;

            var harmony = new Harmony("colonyproject.integrated.traffic.interaction.probe");
            var updatePlay = AccessTools.Method(typeof(Game), "UpdatePlay"); // CHECK API
            if (updatePlay != null)
            {
                harmony.Patch(updatePlay, null, new HarmonyMethod(typeof(InGameWorldTrafficInteractionProbe), "Game_UpdatePlay_Postfix"));
            }
            else
            {
                Log.Warning("[Turbo mega mod] InGameWorldTrafficInteractionProbe could not find Game.UpdatePlay.");
            }
        }

        private static void Game_UpdatePlay_Postfix()
        {
            if (!Enabled || started || completed) return;
            if (Current.ProgramState != ProgramState.Playing) return;
            if (Find.World == null || Find.WorldObjects == null || Find.TickManager == null) return;
            if (LongEventHandler.AnyEventNowOrWaiting) return;

            readyFrames++;
            if (readyFrames < (reload ? 1 : 30)) return;

            started = true;
            if (reload) { RunReload(); return; }
            RunProbe();
        }

        private static void RunReload()
        {
            List<string> wrong = new List<string>();
            string[] rows = File.ReadAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "TrafficHoldings.txt"));
            foreach (string row in rows)
            {
                string id = row.Substring(0, row.IndexOf(';')), held = row.Substring(row.IndexOf(';') + 1);
                Pawn p = Find.Maps.SelectMany(m => m.mapPawns.AllPawns).FirstOrDefault(x => x.ThingID == id);
                if (p == null || Holdings(p) != held) wrong.Add(id + ": " + held + " -> " + (p == null ? "no pawn" : Holdings(p)));
            }
            WorldTrafficCustody custody = WorldTrafficCustody.Instance;
            Thing steel = custody == null || custody.HeldCount != 1 ? null : custody.GetDirectlyHeldThings()[0];
            bool kept = steel != null && steel.def == ThingDefOf.Steel && steel.stackCount == 7;
            if (custody != null) custody.RetryNow();
            bool delivered = kept && custody.HeldCount == 0 && (steel.Destroyed || steel.Spawned || steel.holdingOwner != null);
            if (!kept || !delivered) wrong.Add("custody kept=" + kept + " delivered=" + delivered);
            bool ok = rows.Length > 0 && wrong.Count == 0;
            completed = true;
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "WorldTrafficInteractionReloadProbe.json"), "{\"passed\":" + (ok ? "true" : "false") + ",\"checks\":[{\"name\":\"EnemyGoodsAfterLoad\",\"passed\":" + (ok ? "true" : "false") +
                ",\"actual\":\"pawns=" + rows.Length + " " + string.Join(" | ", wrong.ToArray()).Replace("\\", "").Replace("\"", "'") + "\"}],\"errors\":[]}");
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

        private static void RunProbe()
        {
            ProbeReport report = new ProbeReport();
            UiProbeReport uiReport = new UiProbeReport();

            lock (LogLock)
            {
                CapturedErrors.Clear();
            }

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            capturingLogs = true;

            try
            {
                ExecuteProbeChecks(report, uiReport);
            }
            catch (Exception ex)
            {
                report.fatalException = ex.ToString();
                uiReport.fatalException = ex.Message;
                Log.Error("[Turbo mega mod] WorldTrafficInteractionProbe exception: " + ex);
            }
            finally
            {
                capturingLogs = false;
                Application.logMessageReceivedThreaded -= OnLogMessageReceived;

                // Ensure any lingering dialogs or sessions are cleaned up without shutting down the game.
                try
                {
                    if (Find.WindowStack != null)
                    {
                        Find.WindowStack.TryRemove(typeof(Dialog_Trade), false);
                        Find.WindowStack.TryRemove(typeof(Dialog_MessageBox), false);
                    }
                    if (TradeSession.Active)
                    {
                        TradeSession.Close();
                    }
                }
                catch
                {
                    // Cleanup must not mask probe results.
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
                completed = true;

                Log.Message("[Turbo mega mod] WorldTrafficInteractionProbe finished. passed="
                    + report.passed + ", uiPassed=" + uiReport.passed
                    + ", errors=" + report.errorCount);
            }
        }

        private static void ExecuteProbeChecks(ProbeReport report, UiProbeReport uiReport)
        {
            WorldTraffic traffic = Find.World.GetComponent<WorldTraffic>();
            if (traffic == null)
            {
                report.notes.Add("WorldTraffic component not found on Find.World.");
                return;
            }

            // 1. Start or create a real traffic caravan and ensure its faction is non-hostile for trade testing.
            string startMode;
            WorldTrafficRoute route = AcquireOrCreateTrafficRoute(traffic, out startMode);
            report.trafficStartMode = startMode;
            if (route == null || route.caravan == null)
            {
                report.notes.Add("Failed to create a WorldTrafficRoute for probing.");
                return;
            }

            Caravan trafficCaravan = route.caravan;
            if (trafficCaravan.pather != null)
            {
                trafficCaravan.pather.StopDead();
            }

            Faction npcFaction = trafficCaravan.Faction;
            report.factionName = npcFaction != null ? npcFaction.Name : "null";
            report.trafficCaravanId = trafficCaravan.ID;
            report.tileId = trafficCaravan.Tile.tileId;

            if (npcFaction != null && npcFaction.HostileTo(Faction.OfPlayer))
            {
                int restoreDelta = Math.Max(100, -npcFaction.PlayerGoodwill + 25);
                npcFaction.TryAffectGoodwillWith(Faction.OfPlayer, restoreDelta, false, false, null, null);
            }

            Pawn traderLeader = WorldTrafficInteraction.GetOrPrepareTraderLeader(trafficCaravan);
            if (traderLeader == null || traderLeader.trader == null || traderLeader.trader.traderKind == null)
            {
                report.notes.Add("Could not prepare trader leader or TraderKindDef for traffic caravan.");
                return;
            }

            TraderKindDef traderKind = traderLeader.trader.traderKind;
            report.traderKindDefName = traderKind.defName;

            // Choose two distinct tradable ThingDefs accepted by this TraderKindDef.
            ThingDef buyDef;
            ThingDef sellDef;
            PickTradableDefs(traderKind, out buyDef, out sellDef);
            if (buyDef == null || sellDef == null)
            {
                report.notes.Add("Could not find two tradable ThingDefs accepted by " + traderKind.defName);
                return;
            }

            report.buyDefName = buyDef.defName;
            report.sellDefName = sellDef.defName;

            // Ensure traffic caravan has real items in its pawn inventories (both buyDef and silver).
            int buyTargetCount = 6;
            int trafficBuyInitial = CountItemInCaravan(trafficCaravan, buyDef);
            if (trafficBuyInitial < buyTargetCount)
            {
                AddRealItemToCaravan(trafficCaravan, buyDef, buyTargetCount - trafficBuyInitial);
            }

            int trafficSilverInitial = CountItemInCaravan(trafficCaravan, ThingDefOf.Silver);
            if (trafficSilverInitial < 2500)
            {
                AddRealItemToCaravan(trafficCaravan, ThingDefOf.Silver, 2500 - trafficSilverInitial);
            }

            // 2. Spawn player caravan on the exact same tile.
            Caravan playerCaravan = CreatePlayerCaravanAt(trafficCaravan.Tile, sellDef, 6, 2500);
            if (playerCaravan == null || !playerCaravan.Spawned)
            {
                report.notes.Add("Failed to create player caravan on traffic tile.");
                return;
            }

            report.playerCaravanCreated = true;
            report.sameTileVerified = playerCaravan.Tile == trafficCaravan.Tile;

            // 3. Verify gizmos and UI localization.
            List<Gizmo> gizmos = playerCaravan.GetGizmos().ToList();
            string expectedTradeLabel = "TMM_TrafficTrade".Translate(report.factionName).RawText;
            string expectedAttackLabel = "TMM_TrafficAttack".Translate(report.factionName).RawText;
            string legacyTradeLabel = "TMM.WorldTraffic.Trade".Translate().RawText;

            Command_Action tradeGizmo = FindCommandByLabel(gizmos, expectedTradeLabel);
            Command_Action attackGizmo = FindCommandByLabel(gizmos, expectedAttackLabel);
            Command_Action legacyGizmo = FindCommandByLabel(gizmos, legacyTradeLabel);

            uiReport.gizmoCount = gizmos.Count;
            uiReport.tradeGizmoFound = tradeGizmo != null;
            uiReport.tradeGizmoEnabled = tradeGizmo != null && !tradeGizmo.Disabled;
            uiReport.tradeGizmoLabel = tradeGizmo != null ? tradeGizmo.defaultLabel : string.Empty;
            uiReport.tradeGizmoDesc = tradeGizmo != null ? tradeGizmo.defaultDesc : string.Empty;

            uiReport.attackGizmoFound = attackGizmo != null;
            uiReport.attackGizmoEnabled = attackGizmo != null && !attackGizmo.Disabled;
            uiReport.attackGizmoLabel = attackGizmo != null ? attackGizmo.defaultLabel : string.Empty;
            uiReport.attackGizmoDesc = attackGizmo != null ? attackGizmo.defaultDesc : string.Empty;
            uiReport.legacyDuplicateSuppressed = legacyGizmo == null;

            int expectedCrew = WorldTrafficInteraction.CountLivingHumanlikeMembers(trafficCaravan);
            if (expectedCrew <= 0) expectedCrew = WorldTrafficInteraction.CountLivingMembers(trafficCaravan);
            uiReport.tooltipHasFactionAndCrew =
                !string.IsNullOrEmpty(uiReport.tradeGizmoDesc)
                && uiReport.tradeGizmoDesc.IndexOf(report.factionName, StringComparison.OrdinalIgnoreCase) >= 0
                && uiReport.tradeGizmoDesc.IndexOf(expectedCrew.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) >= 0;

            uiReport.localizationResolved =
                !string.Equals(expectedTradeLabel, "TMM_TrafficTrade", StringComparison.Ordinal)
                && !string.Equals(expectedAttackLabel, "TMM_TrafficAttack", StringComparison.Ordinal)
                && !string.Equals("TMM_TrafficAttackConfirm".Translate(report.factionName).RawText, "TMM_TrafficAttackConfirm", StringComparison.Ordinal)
                && !string.Equals("TMM_TrafficTooltip".Translate(report.factionName, expectedCrew, "1").RawText, "TMM_TrafficTooltip", StringComparison.Ordinal);

            report.gizmosAppeared = uiReport.tradeGizmoFound && uiReport.tradeGizmoEnabled
                && uiReport.attackGizmoFound && uiReport.attackGizmoEnabled;

            if (!report.gizmosAppeared)
            {
                report.notes.Add("Expected trade and/or attack gizmos were missing or disabled.");
                return;
            }

            // 4. Open trade via the actual gizmo action and verify real cargo binding.
            int playerBuyBefore = CountItemInCaravan(playerCaravan, buyDef);
            int trafficBuyBefore = CountItemInCaravan(trafficCaravan, buyDef);
            int playerSellBefore = CountItemInCaravan(playerCaravan, sellDef);
            int trafficSellBefore = CountItemInCaravan(trafficCaravan, sellDef);
            int playerSilverBefore = CountItemInCaravan(playerCaravan, ThingDefOf.Silver);
            int trafficSilverBefore = CountItemInCaravan(trafficCaravan, ThingDefOf.Silver);

            HashSet<int> realTrafficThingIdsBefore = CollectCaravanInventoryThingIds(trafficCaravan);

            tradeGizmo.action();

            uiReport.tradeDialogOpened = FindOpenTradeDialog() != null;
            report.tradeSessionActive = TradeSession.Active;
            report.tradeLeaderMatches = TradeSession.trader == traderLeader;
            report.negotiatorMatches = TradeSession.playerNegotiator == WorldTrafficInteraction.FindBestNegotiator(playerCaravan);

            if (!TradeSession.Active || TradeSession.deal == null)
            {
                report.notes.Add("TradeSession or TradeSession.deal was not active after clicking trade gizmo.");
                return;
            }

            // Verify every non-empty item offered by the trader actually lives inside trafficCaravan's real inventories.
            bool allTraderThingsAreReal = true;
            int traderOfferedStacks = 0;
            List<Tradeable> allTradeables = TradeSession.deal.AllTradeables;
            for (int i = 0; i < allTradeables.Count; i++)
            {
                Tradeable tr = allTradeables[i];
                if (tr == null || tr.thingsTrader == null) continue;
                for (int j = 0; j < tr.thingsTrader.Count; j++)
                {
                    Thing t = tr.thingsTrader[j];
                    if (t == null || t.Destroyed) continue;
                    traderOfferedStacks++;
                    if (!realTrafficThingIdsBefore.Contains(t.thingIDNumber))
                    {
                        allTraderThingsAreReal = false;
                        report.notes.Add("Phantom trader item detected (not in real traffic inventory): " + t.LabelCap);
                    }
                }
            }

            report.traderOfferedStacks = traderOfferedStacks;
            report.realCargoVerified = allTraderThingsAreReal && traderOfferedStacks > 0;

            // 5. Perform real buy & sell via Tradeable API and verify conservation (no duplication).
            int buyAmount = 2;
            int sellAmount = 2;

            Tradeable buyTradeable = FindTradeable(allTradeables, buyDef, Transactor.Trader, buyAmount);
            Tradeable sellTradeable = FindTradeable(allTradeables, sellDef, Transactor.Colony, sellAmount);

            if (buyTradeable == null || sellTradeable == null)
            {
                report.notes.Add("Could not locate buyTradeable or sellTradeable in TradeSession.deal.AllTradeables.");
                return;
            }

            bool buyAdjusted = TryAdjustTradeable(buyTradeable, buyAmount, TradeAction.PlayerBuys);
            bool sellAdjusted = TryAdjustTradeable(sellTradeable, sellAmount, TradeAction.PlayerSells);

            TradeSession.deal.UpdateCurrencyCount(); // CHECK API

            bool actuallyTraded = false;
            bool tradeExecuted = false;
            if (buyAdjusted && sellAdjusted)
            {
                tradeExecuted = TradeSession.deal.TryExecute(out actuallyTraded); // CHECK API
            }

            CloseOpenTradeDialogs();
            if (TradeSession.Active)
            {
                TradeSession.Close();
            }

            int playerBuyAfter = CountItemInCaravan(playerCaravan, buyDef);
            int trafficBuyAfter = CountItemInCaravan(trafficCaravan, buyDef);
            int playerSellAfter = CountItemInCaravan(playerCaravan, sellDef);
            int trafficSellAfter = CountItemInCaravan(trafficCaravan, sellDef);
            int playerSilverAfter = CountItemInCaravan(playerCaravan, ThingDefOf.Silver);
            int trafficSilverAfter = CountItemInCaravan(trafficCaravan, ThingDefOf.Silver);

            bool buyMovedCorrectly = (playerBuyAfter == playerBuyBefore + buyAmount)
                && (trafficBuyAfter == trafficBuyBefore - buyAmount);
            bool sellMovedCorrectly = (playerSellAfter == playerSellBefore - sellAmount)
                && (trafficSellAfter == trafficSellBefore + sellAmount);
            bool silverConserved = (playerSilverAfter + trafficSilverAfter) == (playerSilverBefore + trafficSilverBefore);
            bool noSharedThingIds = !HasSharedThingIds(playerCaravan, trafficCaravan);

            report.buyAdjusted = buyAdjusted;
            report.sellAdjusted = sellAdjusted;
            report.tradeExecuted = tradeExecuted && actuallyTraded;
            report.buyTransferredWithoutDuplication = buyMovedCorrectly;
            report.sellTransferredWithoutDuplication = sellMovedCorrectly;
            report.silverConserved = silverConserved;
            report.noDuplicateThingIdsAfterTrade = noSharedThingIds;

            if (!report.tradeExecuted || !buyMovedCorrectly || !sellMovedCorrectly || !silverConserved || !noSharedThingIds)
            {
                report.notes.Add("Trade transfer check mismatch: executed=" + report.tradeExecuted
                    + " buy(" + playerBuyBefore + "/" + trafficBuyBefore + " -> " + playerBuyAfter + "/" + trafficBuyAfter + ")"
                    + " sell(" + playerSellBefore + "/" + trafficSellBefore + " -> " + playerSellAfter + "/" + trafficSellAfter + ")"
                    + " silver(" + playerSilverBefore + "+" + trafficSilverBefore + " -> " + playerSilverAfter + "+" + trafficSilverAfter + ")");
            }

            TradeFaults(report, traffic, playerCaravan, trafficCaravan, traderLeader, buyDef, sellDef);
            playerCaravan = AttackFaults(report, traffic, route, playerCaravan, ref trafficCaravan, expectedAttackLabel);
            // Combat drugs, whose fate on entering the battle is followed below pawn by pawn.
            for (int i = 0; i < trafficCaravan.PawnsListForReading.Count && i < 3; i++)
            {
                Pawn user = trafficCaravan.PawnsListForReading[i];
                if (user.inventory == null || !user.RaceProps.Humanlike) continue;
                foreach (string drug in new[] { "GoJuice", "Yayo" })
                {
                    Thing dose = ThingMaker.MakeThing(ThingDef.Named(drug));
                    dose.stackCount = 2;
                    user.inventory.innerContainer.TryAdd(dose, true);
                }
            }
            Dictionary<Pawn, string> holdingsBefore = new Dictionary<Pawn, string>();
            foreach (Pawn holder in trafficCaravan.PawnsListForReading) holdingsBefore[holder] = Holdings(holder);

            // 6. Test Attack confirmation dialog and battle map execution.
            List<Pawn> expectedEnemyPawns = new List<Pawn>();
            HashSet<int> expectedEnemyIds = new HashSet<int>();
            int preAttackEnemyCargoStacks = 0;
            Dictionary<string, int> preItems = new Dictionary<string, int>();

            List<Pawn> trafficMembers = trafficCaravan.PawnsListForReading;
            for (int i = 0; i < trafficMembers.Count; i++)
            {
                Pawn p = trafficMembers[i];
                if (p == null || p.Destroyed || p.Dead) continue;
                expectedEnemyPawns.Add(p);
                expectedEnemyIds.Add(p.thingIDNumber);
                if (p.inventory != null && p.inventory.innerContainer != null)
                {
                    IList<Thing> inv = p.inventory.innerContainer.InnerListForReading;
                    for (int j = 0; j < inv.Count; j++)
                    {
                        Thing item = inv[j];
                        if (item != null && !item.Destroyed)
                        {
                            preAttackEnemyCargoStacks += item.stackCount;
                            string dName = item.def != null ? item.def.defName : "unknown";
                            int cur;
                            preItems.TryGetValue(dName, out cur);
                            preItems[dName] = cur + item.stackCount;
                        }
                    }
                }
            }

            int preAttackGoodwill = npcFaction != null ? npcFaction.PlayerGoodwill : 0;
            int mapsBeforeAttack = Find.Maps != null ? Find.Maps.Count : 0;

            // P1 (Codex): mission silver must never be offered to a traffic trader.
            {
                Thing reserved = ThingMaker.MakeThing(ThingDefOf.Silver);
                reserved.stackCount = 37;
                Pawn holder = WorldTrafficInteraction.FindBestNegotiator(playerCaravan);
                if (holder != null && holder.inventory != null && holder.inventory.innerContainer.TryAdd(reserved, false))
                {
                    OutpostCaravanManager.RegisterTransientSilverProtection(reserved.ThingID, "probe-mission");
                    Pawn leaderForCheck = WorldTrafficInteraction.GetOrPrepareTraderLeader(trafficCaravan);
                    bool offered = false;
                    if (leaderForCheck != null && leaderForCheck.trader != null)
                    {
                        foreach (Thing t in leaderForCheck.trader.ColonyThingsWillingToBuy(holder))
                            if (t == reserved) { offered = true; break; }
                    }
                    report.missionSilverNotOffered = leaderForCheck != null && !offered;
                    OutpostCaravanManager.UnregisterTransientSilverProtection(reserved.ThingID);
                    holder.inventory.innerContainer.Remove(reserved);
                    reserved.Destroy(DestroyMode.Vanish);
                }
                else report.notes.Add("Could not stage mission silver check.");
            }

            // P1 (Codex): a failed battle generation leaves the route, the pawns and the player caravan intact.
            {
                PlanetTile tileBefore = playerCaravan.Tile;
                WorldTrafficInteraction.TestFailAttackMap = true;
                Map failed = null;
                try { failed = WorldTrafficInteraction.ExecuteAttack(playerCaravan, trafficCaravan, false); }
                finally { WorldTrafficInteraction.TestFailAttackMap = false; }
                WorldTrafficRoute stillRoute = traffic.routes.Contains(route) ? route : null;
                Caravan holderCaravan = stillRoute != null ? stillRoute.caravan : null;
                int back = 0;
                if (holderCaravan != null)
                    foreach (Pawn ep in expectedEnemyPawns) if (holderCaravan.ContainsPawn(ep)) back++;
                report.failedAttackRolledBack = failed == null && stillRoute != null && holderCaravan != null
                    && back == expectedEnemyPawns.Count && playerCaravan.Tile == tileBefore
                    && WorldTrafficInteraction.IsValidTrafficCaravan(holderCaravan);
                if (!report.failedAttackRolledBack)
                    report.notes.Add("Failed attack rollback: route=" + (stillRoute != null) + " pawnsBack=" + back + "/" + expectedEnemyPawns.Count + " tileRestored=" + (playerCaravan.Tile == tileBefore));
                if (holderCaravan != null && holderCaravan != trafficCaravan) trafficCaravan = holderCaravan;
            }

            // Refresh gizmos before clicking Attack.
            List<Gizmo> preAttackGizmos = playerCaravan.GetGizmos().ToList();
            Command_Action freshAttackGizmo = FindCommandByLabel(preAttackGizmos, expectedAttackLabel);
            if (freshAttackGizmo == null)
            {
                report.notes.Add("Attack gizmo disappeared unexpectedly after trade.");
                return;
            }

            freshAttackGizmo.action();

            Dialog_MessageBox confirmBox = FindOpenConfirmDialog();
            uiReport.attackConfirmDialogOpened = confirmBox != null;
            if (confirmBox != null)
            {
                string boxText = confirmBox.text != null ? confirmBox.text.RawText : string.Empty;
                uiReport.attackConfirmHasFactionName = boxText.IndexOf(report.factionName, StringComparison.OrdinalIgnoreCase) >= 0;

                Action confirmAction = confirmBox.buttonAAction; // CHECK API
                if (Find.WindowStack != null)
                {
                    Find.WindowStack.TryRemove(confirmBox, false);
                }
                if (confirmAction != null)
                {
                    confirmAction();
                }
                else
                {
                    WorldTrafficInteraction.ExecuteAttack(playerCaravan, trafficCaravan, false);
                }
            }
            else
            {
                WorldTrafficInteraction.ExecuteAttack(playerCaravan, trafficCaravan, false);
            }

            // Locate the newly generated caravan attack map.
            Map battleMap = FindBattleMapForTile(trafficCaravan.Tile, expectedEnemyIds);
            int mapsAfterAttack = Find.Maps != null ? Find.Maps.Count : 0;

            report.battleMapCreated = battleMap != null && mapsAfterAttack >= mapsBeforeAttack;
            report.routeRemovedAfterAttack = traffic.Route(trafficCaravan) == null && !traffic.routes.Contains(route);
            report.trafficCaravanRemovedAfterAttack = !trafficCaravan.Spawned || trafficCaravan.Destroyed;
            report.markedEngagedAfterAttack = WorldTrafficInteraction.IsEngaged(trafficCaravan)
                && !WorldTrafficInteraction.IsValidTrafficCaravan(trafficCaravan);

            if (npcFaction != null)
            {
                report.goodwillDroppedOrHostile = npcFaction.HostileTo(Faction.OfPlayer)
                    || npcFaction.PlayerGoodwill < preAttackGoodwill;
            }

            // Verify exact same enemy pawns are spawned on battleMap with cargo retained.
            int matchedEnemies = 0;
            int postAttackEnemyCargoStacks = 0;
            Dictionary<string, int> postItems = new Dictionary<string, int>();
            if (battleMap != null)
            {
                IReadOnlyList<Pawn> mapPawns = battleMap.mapPawns.AllPawnsSpawned;
                HashSet<int> seenEnemyIds = new HashSet<int>();
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    Pawn mp = mapPawns[i];
                    if (mp == null || mp.Destroyed) continue;
                    if (expectedEnemyIds.Contains(mp.thingIDNumber) && seenEnemyIds.Add(mp.thingIDNumber))
                    {
                        matchedEnemies++;
                        // A raider may start a combat-drug job at once, which moves the dose from inventory to its hands.
                        Thing carried = mp.carryTracker != null ? mp.carryTracker.CarriedThing : null;
                        if (carried != null && !carried.Destroyed)
                        {
                            postAttackEnemyCargoStacks += carried.stackCount;
                            string cName = carried.def != null ? carried.def.defName : "unknown";
                            int curC;
                            postItems.TryGetValue(cName, out curC);
                            postItems[cName] = curC + carried.stackCount;
                        }
                        if (mp.inventory != null && mp.inventory.innerContainer != null)
                        {
                            IList<Thing> inv = mp.inventory.innerContainer.InnerListForReading;
                            for (int j = 0; j < inv.Count; j++)
                            {
                                Thing item = inv[j];
                                if (item != null && !item.Destroyed)
                                {
                                    postAttackEnemyCargoStacks += item.stackCount;
                                    string dName = item.def != null ? item.def.defName : "unknown";
                                    int cur;
                                    postItems.TryGetValue(dName, out cur);
                                    postItems[dName] = cur + item.stackCount;
                                }
                            }
                        }
                    }
                }
            }

            // Strict: every enemy holds exactly what it held before the battle, in its inventory and hands together.
            List<string> changedHoldings = new List<string>();
            foreach (KeyValuePair<Pawn, string> kv in holdingsBefore)
                if (Holdings(kv.Key) != kv.Value) changedHoldings.Add(kv.Key.LabelShort + ": " + kv.Value + " -> " + Holdings(kv.Key));
            Extra(report, "EveryEnemyKeepsExactGoods", changedHoldings.Count == 0 && holdingsBefore.Values.Any(x => x.Contains("GoJuice=2") && x.Contains("Yayo=2")), string.Join(" | ", changedHoldings.ToArray()));
            File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "TrafficHoldings.txt"), holdingsBefore.Keys.Where(p => p.Spawned).Select(p => p.ThingID + ";" + Holdings(p)).ToArray());
            // Something waits in custody while the game is saved.
            Thing waiting = ThingMaker.MakeThing(ThingDefOf.Steel);
            waiting.stackCount = 7;
            WorldTrafficCustody.Keep(waiting, null, true);
            GameDataSaveLoader.SaveGame("TMM-TrafficInteraction");
            WorldTrafficCustody.Instance.RetryNow();
            Extra(report, "CustodyDelivers", WorldTrafficCustody.Instance.HeldCount == 0 && !waiting.Destroyed && (waiting.Spawned || waiting.holdingOwner != null), "held=" + WorldTrafficCustody.Instance.HeldCount);

            report.expectedEnemyCount = expectedEnemyIds.Count;
            report.matchedEnemyCount = matchedEnemies;
            report.sameEnemiesOnBattleMap = expectedEnemyIds.Count > 0 && matchedEnemies == expectedEnemyIds.Count;
            report.enemyCargoRetained = preAttackEnemyCargoStacks > 0 && postAttackEnemyCargoStacks >= preAttackEnemyCargoStacks;
            if (!report.enemyCargoRetained)
            {
                report.notes.Add("Cargo retention mismatch: pre=" + preAttackEnemyCargoStacks + ", post=" + postAttackEnemyCargoStacks);
                foreach (KeyValuePair<string, int> kvp in preItems)
                {
                    int postCount;
                    postItems.TryGetValue(kvp.Key, out postCount);
                    if (postCount != kvp.Value)
                    {
                        report.notes.Add("Diff item " + kvp.Key + ": pre=" + kvp.Value + ", post=" + postCount);
                    }
                }
            }

            // Verify duplicate click / re-attack cannot duplicate pawns and gizmos no longer include this caravan.
            int spawnedCountBeforeRetry = battleMap != null ? battleMap.mapPawns.AllPawnsSpawnedCount : 0;
            Map retryMap = WorldTrafficInteraction.ExecuteAttack(playerCaravan, trafficCaravan, false);
            int spawnedCountAfterRetry = battleMap != null ? battleMap.mapPawns.AllPawnsSpawnedCount : 0;

            report.reattackBlockedNoDuplicates = (retryMap == null) && (spawnedCountAfterRetry == spawnedCountBeforeRetry);
            uiReport.gizmosClearedAfterAttack = !WorldTrafficInteraction.GetInteractableTrafficCaravans(playerCaravan).Contains(trafficCaravan);
        }

        // ---- Faults in the middle of a deal or an attack: nothing may be lost, doubled or left without an owner.

        private static void Extra(ProbeReport report, string name, bool passed, string detail)
        {
            report.extra.Add(name + ": " + (passed ? "ok" : "FAIL") + (string.IsNullOrEmpty(detail) ? "" : " " + detail));
        }

        private static string Holdings(Pawn p)
        {
            SortedDictionary<string, int> sum = new SortedDictionary<string, int>(StringComparer.Ordinal);
            List<Thing> all = new List<Thing>();
            if (p.inventory != null) all.AddRange(p.inventory.innerContainer.InnerListForReading);
            if (p.carryTracker != null && p.carryTracker.CarriedThing != null) all.Add(p.carryTracker.CarriedThing);
            foreach (Thing t in all) { int n; sum.TryGetValue(t.def.defName, out n); sum[t.def.defName] = n + t.stackCount; }
            return string.Join(",", sum.Select(kv => kv.Key + "=" + kv.Value).ToArray());
        }

        private static string State(Caravan c)
        {
            if (c == null || c.Destroyed || !c.Spawned) return "gone";
            return c.Tile + "{" + string.Join(" ", c.PawnsListForReading.OrderBy(p => p.thingIDNumber).Select(p => p.ThingID + "/" + (p.Faction == null ? "-" : p.Faction.loadID.ToString()) + "[" + Holdings(p) + "]").ToArray()) + "}";
        }

        private static string Relations(Faction f)
        {
            FactionRelation a = f.RelationWith(Faction.OfPlayer, true), b = Faction.OfPlayer.RelationWith(f, true);
            return a.baseGoodwill + "/" + a.kind + "/" + b.baseGoodwill + "/" + b.kind;
        }

        private static bool Deal(Caravan player, Caravan npc, ThingDef buyDef, int buy, ThingDef sellDef, int sell, Action<TradeDeal> beforeExecute, out string note)
        {
            note = "";
            if (!WorldTrafficInteraction.SetupTradeSession(player, npc)) { note = "no session"; return false; }
            List<Tradeable> all = TradeSession.deal.AllTradeables;
            Tradeable b = FindTradeable(all, buyDef, Transactor.Trader, buy), s = FindTradeable(all, sellDef, Transactor.Colony, sell);
            bool ready = b != null && s != null && TryAdjustTradeable(b, buy, TradeAction.PlayerBuys) && TryAdjustTradeable(s, sell, TradeAction.PlayerSells);
            TradeSession.deal.UpdateCurrencyCount();
            if (!ready) { note = "not adjusted"; TradeSession.Close(); return false; }
            if (beforeExecute != null) beforeExecute(TradeSession.deal);
            bool traded;
            bool executed = TradeSession.deal.TryExecute(out traded);
            note = "executed=" + executed + " traded=" + traded;
            TradeSession.Close();
            return executed || traded;
        }

        private static void TradeFaults(ProbeReport report, WorldTraffic traffic, Caravan player, Caravan npc, Pawn trader, ThingDef buyDef, ThingDef sellDef)
        {
            WorldTrafficCustody custody = WorldTrafficCustody.Instance;
            if (CountItemInCaravan(player, sellDef) < 6) AddRealItemToCaravan(player, sellDef, 6);
            if (CountItemInCaravan(npc, buyDef) < 4) AddRealItemToCaravan(npc, buyDef, 6);
            Func<string> state = () => State(player) + State(npc) + Relations(npc.Faction) + " custody=" + custody.HeldCount;
            string before = state(), note;
            bool went;
            Action<string, Func<Thing, Pawn, bool>> refused = delegate(string name, Func<Thing, Pawn, bool> refuse)
            {
                WorldTrafficInteraction.TestRefuseReceive = refuse;
                try { went = Deal(player, npc, buyDef, 2, sellDef, 2, null, out note); } finally { WorldTrafficInteraction.TestRefuseReceive = null; }
                string after = state();
                Extra(report, name, !went && note.StartsWith("executed") && after == before, note + (after == before ? "" : " before " + before + " after " + after));
            };
            // Whichever way the silver goes, somewhere in this deal money has to change hands; each side is refused in turn.
            refused("CurrencyRefused", (t, p) => t.def == ThingDefOf.Silver);
            refused("SoldGoodsRefused", (t, p) => t.def == sellDef);
            refused("BoughtGoodsRefused", (t, p) => t.def == buyDef);

            // Refused both ways: the whole sold stack can neither stay with the buyer nor go back; it waits in custody and is saved there.
            int whole = CountItemInCaravan(player, sellDef), npcHad = CountItemInCaravan(npc, sellDef);
            WorldTrafficInteraction.TestRefuseReceive = (t, p) => t.def == ThingDefOf.Silver || t.def == buyDef;
            WorldTrafficInteraction.TestRefuseReturn = (t, p) => true;
            try { went = Deal(player, npc, buyDef, 2, sellDef, whole, null, out note); }
            finally { WorldTrafficInteraction.TestRefuseReceive = null; WorldTrafficInteraction.TestRefuseReturn = null; }
            int held = custody.GetDirectlyHeldThings().Where(t => t.def == sellDef).Sum(t => t.stackCount);
            bool conserved = CountItemInCaravan(player, sellDef) + CountItemInCaravan(npc, sellDef) + held == whole + npcHad;
            Extra(report, "RefusedTwiceKeptInCustody", !went && held > 0 && conserved, note + " held=" + held + " whole=" + whole + " player=" + CountItemInCaravan(player, sellDef) + " npc=" + CountItemInCaravan(npc, sellDef));
            custody.RetryNow();
            string returned = state();
            Extra(report, "CustodyReturnsToOwner", returned == before, returned == before ? "" : "before " + before + " after " + returned);

            WorldTrafficInteraction.TestRefuseReceive = (t, p) => t.def == ThingDefOf.Silver || t.def == buyDef;
            WorldTrafficInteraction.TestRefuseReturn = (t, p) => true;
            try { went = Deal(player, npc, buyDef, 2, sellDef, whole - 1, null, out note); }
            finally { WorldTrafficInteraction.TestRefuseReceive = null; WorldTrafficInteraction.TestRefuseReturn = null; }
            int heldPartial = custody.GetDirectlyHeldThings().Where(t => t.def == sellDef).Sum(t => t.stackCount);
            int keptByPlayer = CountItemInCaravan(player, sellDef);
            Extra(report, "PartialSplitRefusalConservesExactGoods", !went && keptByPlayer >= 1 && heldPartial > 0 &&
                keptByPlayer + heldPartial + CountItemInCaravan(npc, sellDef) == whole + npcHad,
                note + " left=" + keptByPlayer + " held=" + heldPartial);
            custody.RetryNow();
            Extra(report, "PartialSplitReturnsToOwner", state() == before, state());

            // Stale sessions: the route or the trader is gone between opening and confirming.
            WorldTrafficRoute route = traffic.Route(npc);
            went = Deal(player, npc, buyDef, 2, sellDef, 2, d => traffic.routes.Remove(route), out note);
            traffic.routes.Add(route);
            Extra(report, "RouteGoneCancels", !went && note.StartsWith("executed") && state() == before, note);
            went = Deal(player, npc, buyDef, 2, sellDef, 2, d => npc.RemovePawn(trader), out note);
            if (!npc.ContainsPawn(trader)) npc.AddPawn(trader, true);
            Extra(report, "TraderGoneCancels", !went && note.StartsWith("executed") && state() == before, note + (state() == before ? "" : " after " + state()));

            // A traded animal that the other side does not accept stays, with its faction.
            Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Muffalo, Faction.OfPlayer, forceGenerateNewPawn: true));
            player.AddPawn(animal, true);
            string withAnimal = state();
            Pawn negotiator = WorldTrafficInteraction.FindBestNegotiator(player);
            WorldTrafficInteraction.SetupTradeSession(player, npc);
            WorldTrafficInteraction.TestRefuseReceive = (t, p) => t == animal;
            try { trader.trader.GiveSoldThingToTrader(animal, 1, negotiator); } finally { WorldTrafficInteraction.TestRefuseReceive = null; }
            Extra(report, "RefusedAnimalStays", state() == withAnimal, state() == withAnimal ? "" : state());
            trader.trader.GiveSoldThingToTrader(animal, 1, negotiator);
            Extra(report, "SoldAnimalMoves", npc.ContainsPawn(animal) && !player.ContainsPawn(animal) && animal.Faction != Faction.OfPlayer, "faction=" + animal.Faction);
            TradeSession.Close();
            if (npc.ContainsPawn(animal)) npc.RemovePawn(animal);
            if (!animal.Destroyed) animal.Destroy(DestroyMode.Vanish);

            // And the same deal without faults still goes through, once.
            int silver = CountItemInCaravan(player, ThingDefOf.Silver) + CountItemInCaravan(npc, ThingDefOf.Silver), sold = CountItemInCaravan(player, sellDef), bought = CountItemInCaravan(player, buyDef);
            went = Deal(player, npc, buyDef, 2, sellDef, 2, null, out note);
            Extra(report, "CleanDealAfterFaults", went && CountItemInCaravan(player, sellDef) == sold - 2 && CountItemInCaravan(player, buyDef) == bought + 2 &&
                CountItemInCaravan(player, ThingDefOf.Silver) + CountItemInCaravan(npc, ThingDefOf.Silver) == silver && custody.HeldCount == 0 && !HasSharedThingIds(player, npc), note);
        }


        private static Caravan AttackFaults(ProbeReport report, WorldTraffic traffic, WorldTrafficRoute route, Caravan player, ref Caravan npc, string attackLabel)
        {
            Pawn leader = player.PawnsListForReading[0];
            int enemies = npc.PawnsListForReading.Count(p => !p.Dead);
            Faction faction = npc.Faction;
            foreach (string step in new[] { "map", "enemy0", "enemy" + (enemies - 1), "diplomacy", "player" })
            {
                string before = State(player) + State(npc) + Relations(faction) + " maps=" + Find.Maps.Count + " objects=" + Find.WorldObjects.AllWorldObjects.Count(o => o.Tile == player.Tile) + " routes=" + traffic.routes.Count;
                WorldTrafficInteraction.TestFailAttackStep = step;
                Map map;
                try { map = WorldTrafficInteraction.ExecuteAttack(player, npc, false); } finally { WorldTrafficInteraction.TestFailAttackStep = null; }
                player = leader.GetCaravan();
                npc = route.caravan;
                string after = State(player) + State(npc) + Relations(faction) + " maps=" + Find.Maps.Count + " objects=" + (player == null ? -1 : Find.WorldObjects.AllWorldObjects.Count(o => o.Tile == player.Tile)) + " routes=" + traffic.routes.Count;
                Extra(report, "AttackFailsAt_" + step, map == null && before == after && traffic.routes.Contains(route) && WorldTrafficInteraction.IsValidTrafficCaravan(npc), before == after ? "" : "before " + before + " after " + after);
                if (player == null) throw new InvalidOperationException("player caravan lost after a failed attack at " + step);
            }

            // A story truce blocks the attack with its reason on the button.
            System.Collections.IList backups = (System.Collections.IList)AccessTools.Field(typeof(StoryRelationRestorer), "backups").GetValue(StoryRelationRestorer.Instance);
            StoryFactionBackup truce = new StoryFactionBackup { factionId = faction.loadID };
            backups.Add(truce);
            Blocked(report, "TruceBlocks", player, npc, attackLabel, "TMM_TrafficAttackBlockedTruce".Translate(faction.Name));
            backups.Remove(truce);

            // No free tile: the shared tile and all its neighbours are taken.
            List<WorldObject> fill = new List<WorldObject>();
            List<PlanetTile> tiles = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(player.Tile, tiles);
            tiles.Add(player.Tile);
            foreach (PlanetTile t in tiles)
                if (Find.WorldObjects.MapParentAt(t) == null)
                {
                    WorldObject o = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Ambush);
                    o.Tile = t;
                    Find.WorldObjects.Add(o);
                    fill.Add(o);
                }
            Blocked(report, "NoFreeTileBlocks", player, npc, attackLabel, "TMM_TrafficAttackBlockedNoTile".Translate());
            foreach (WorldObject o in fill) o.Destroy();
            return player;
        }

        private static void Blocked(ProbeReport report, string name, Caravan player, Caravan npc, string attackLabel, string reason)
        {
            string before = State(player) + State(npc) + Relations(npc.Faction) + Find.Maps.Count;
            Command_Action attack = FindCommandByLabel(player.GetGizmos().ToList(), attackLabel);
            Map map = WorldTrafficInteraction.ExecuteAttack(player, npc, false);
            string after = State(player) + State(npc) + Relations(npc.Faction) + Find.Maps.Count;
            Extra(report, name, attack != null && attack.Disabled && attack.disabledReason == reason && map == null && before == after, attack == null ? "no button" : attack.Disabled + " " + attack.disabledReason);
        }

        private static WorldTrafficRoute AcquireOrCreateTrafficRoute(WorldTraffic traffic, out string mode)
        {
            mode = "none";

            // Prefer an existing active non-hostile route if one is already present on surface.
            if (traffic.routes != null)
            {
                for (int i = 0; i < traffic.routes.Count; i++)
                {
                    WorldTrafficRoute existing = traffic.routes[i];
                    if (existing != null && existing.caravan != null && existing.caravan.Spawned && !existing.caravan.Destroyed
                        && existing.caravan.Faction != null && !existing.caravan.Faction.IsPlayer
                        && !existing.caravan.Faction.HostileTo(Faction.OfPlayer)
                        && WorldTrafficInteraction.CountLivingMembers(existing.caravan) > 0)
                    {
                        mode = "existing_route";
                        return existing;
                    }
                }
            }

            // Try starting a real traffic route via WorldTraffic.TryStart.
            List<Settlement> settlements = Find.WorldObjects.Settlements
                .Where(s => s != null && s.Spawned && WorldTraffic.Eligible(s.Faction)
                    && s.Tile.Layer == Find.WorldGrid.Surface && !s.HasMap)
                .ToList();

            // Prefer non-hostile settlements first.
            List<Settlement> orderedSources = settlements
                .OrderBy(s => s.Faction.HostileTo(Faction.OfPlayer) ? 1 : 0)
                .ToList();

            for (int i = 0; i < orderedSources.Count; i++)
            {
                Settlement origin = orderedSources[i];
                for (int j = 0; j < settlements.Count; j++)
                {
                    Settlement dest = settlements[j];
                    if (dest == origin || origin.Faction.HostileTo(dest.Faction)) continue;
                    if (!Find.World.reachability.CanReach(origin.Tile, dest.Tile)) continue;

                    WorldTrafficRoute startedRoute = traffic.TryStart(origin, dest, false);
                    if (startedRoute != null && startedRoute.caravan != null)
                    {
                        PlanetTile wilderness = FindWildernessEncounterTile(startedRoute, origin.Tile, dest.Tile);
                        if (wilderness.Valid)
                        {
                            startedRoute.caravan.Tile = wilderness;
                        }
                        mode = "WorldTraffic.TryStart";
                        return startedRoute;
                    }
                }
            }

            // Fallback: construct a valid traffic caravan and register its WorldTrafficRoute manually.
            Faction fallbackFaction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(
                f => f != null && !f.IsPlayer && !f.defeated && f.def.humanlikeFaction && !f.def.hidden && !f.HostileTo(Faction.OfPlayer));
            if (fallbackFaction == null)
            {
                fallbackFaction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(
                    f => f != null && !f.IsPlayer && !f.defeated && f.def.humanlikeFaction && !f.def.hidden);
            }
            if (fallbackFaction == null) return null;

            Settlement originSettlement = Find.WorldObjects.Settlements.FirstOrDefault(
                s => s != null && s.Spawned && s.Faction == fallbackFaction && s.Tile.Layer == Find.WorldGrid.Surface);
            PlanetTile spawnTile = originSettlement != null
                ? FindWildernessEncounterTile(null, originSettlement.Tile, PlanetTile.Invalid)
                : FindSafeSurfaceTile();
            if (!spawnTile.Valid && originSettlement != null) spawnTile = originSettlement.Tile;
            if (!spawnTile.Valid) return null;

            List<Pawn> npcPawns = new List<Pawn>();
            PawnKindDef pawnKind = fallbackFaction.RandomPawnKind();
            if (pawnKind == null) pawnKind = PawnKindDefOf.Colonist;

            for (int k = 0; k < 3; k++)
            {
                Pawn npc = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    pawnKind, fallbackFaction, PawnGenerationContext.NonPlayer, -1, true, false, false, false, true, 1f, false, true, false, false, false, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult)); // CHECK API
                if (npc != null && !npc.Dead && !npc.Downed)
                {
                    npcPawns.Add(npc);
                }
            }

            if (npcPawns.Count == 0) return null;

            Caravan manualCaravan = CaravanMaker.MakeCaravan(npcPawns, fallbackFaction, spawnTile, true);
            if (manualCaravan == null || !manualCaravan.Spawned) return null;

            int now = Find.TickManager.TicksGame;
            WorldTrafficRoute manualRoute = new WorldTrafficRoute
            {
                caravan = manualCaravan,
                origin = originSettlement,
                destination = originSettlement,
                patrol = false,
                created = now,
                deadline = now + 60000 * 6,
                retryAt = now + 60000
            };
            traffic.routes.Add(manualRoute);
            mode = "manual_fallback";
            return manualRoute;
        }

        private static PlanetTile FindWildernessEncounterTile(WorldTrafficRoute route, PlanetTile originTile, PlanetTile destTile)
        {
            if (route != null && route.caravan != null && route.caravan.pather != null && route.caravan.pather.curPath != null)
            {
                List<PlanetTile> nodes = route.caravan.pather.curPath.NodesReversed;
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    PlanetTile t = nodes[i];
                    if (t != originTile && t != destTile && Find.WorldObjects.MapParentAt(t) == null && !Find.World.Impassable(t))
                    {
                        return t;
                    }
                }
            }

            if (originTile.Valid && Find.WorldGrid != null)
            {
                List<PlanetTile> neighbors = new List<PlanetTile>();
                Find.WorldGrid.GetTileNeighbors(originTile, neighbors);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    PlanetTile nt = neighbors[i];
                    if (nt != originTile && nt != destTile && Find.WorldObjects.MapParentAt(nt) == null && !Find.World.Impassable(nt))
                    {
                        return nt;
                    }
                }
            }

            return PlanetTile.Invalid;
        }

        private static PlanetTile FindSafeSurfaceTile()
        {
            if (Find.CurrentMap != null && Find.CurrentMap.Tile.Valid)
                return Find.CurrentMap.Tile;

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s != null && s.Spawned && s.Tile.Valid && s.Tile.Layer == Find.WorldGrid.Surface)
                    return s.Tile;
            }
            return PlanetTile.Invalid;
        }

        private static Caravan CreatePlayerCaravanAt(PlanetTile tile, ThingDef sellDef, int sellCount, int silverCount)
        {
            List<Pawn> colonists = new List<Pawn>();
            for (int i = 0; i < 2; i++)
            {
                Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    true,
                    false,
                    false,
                    false,
                    true,
                    1f,
                    false,
                    true,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    0f,
                    0f,
                    null,
                    1f,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    0f,
                    DevelopmentalStage.Adult)); // CHECK API

                if (colonist != null && !colonist.Dead && !colonist.Downed)
                {
                    if (colonist.skills != null)
                    {
                        SkillRecord social = colonist.skills.GetSkill(SkillDefOf.Social);
                        if (social != null)
                        {
                            social.Level = i == 0 ? 12 : 4;
                        }
                    }
                    colonists.Add(colonist);
                }
            }

            if (colonists.Count == 0) return null;

            Caravan caravan = CaravanMaker.MakeCaravan(colonists, Faction.OfPlayer, tile, true);
            if (caravan == null) return null;

            AddRealItemToCaravan(caravan, ThingDefOf.Silver, silverCount);
            AddRealItemToCaravan(caravan, sellDef, sellCount);
            return caravan;
        }

        private static void PickTradableDefs(TraderKindDef traderKind, out ThingDef buyDef, out ThingDef sellDef)
        {
            buyDef = null;
            sellDef = null;

            string[] preferredDefNames = new string[]
            {
                "MedicineIndustrial",
                "Pemmican",
                "Cloth",
                "Steel",
                "ComponentIndustrial",
                "MealSurvivalPack",
                "MedicineHerbal",
                "WoodLog"
            };

            List<ThingDef> accepted = new List<ThingDef>();
            for (int i = 0; i < preferredDefNames.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(preferredDefNames[i]);
                if (IsGoodProbeTradeDef(traderKind, def) && !accepted.Contains(def))
                {
                    accepted.Add(def);
                }
            }

            if (accepted.Count < 2)
            {
                List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int i = 0; i < allDefs.Count && accepted.Count < 2; i++)
                {
                    ThingDef def = allDefs[i];
                    if (IsGoodProbeTradeDef(traderKind, def) && !accepted.Contains(def))
                    {
                        accepted.Add(def);
                    }
                }
            }

            if (accepted.Count >= 1) buyDef = accepted[0];
            if (accepted.Count >= 2) sellDef = accepted[1];
        }

        private static bool IsGoodProbeTradeDef(TraderKindDef traderKind, ThingDef def)
        {
            if (traderKind == null || def == null) return false;
            if (def == ThingDefOf.Silver) return false;
            if (def.category != ThingCategory.Item) return false;
            if (def.tradeability != Tradeability.All) return false;
            if (def.MadeFromStuff) return false;
            if (def.stackLimit < 5) return false;
            if (def.BaseMarketValue <= 0.5f || def.BaseMarketValue > 150f) return false;
            return traderKind.WillTrade(def);
        }

        private static void AddRealItemToCaravan(Caravan caravan, ThingDef def, int count)
        {
            if (caravan == null || def == null || count <= 0) return;
            Pawn carrier = caravan.PawnsListForReading.FirstOrDefault(
                p => p != null && !p.Dead && !p.Destroyed && p.inventory != null && p.inventory.innerContainer != null);
            if (carrier == null) return;

            int remaining = count;
            while (remaining > 0)
            {
                Thing thing = ThingMaker.MakeThing(def);
                thing.stackCount = Math.Min(remaining, def.stackLimit);
                int added = thing.stackCount;
                if (!carrier.inventory.innerContainer.TryAdd(thing, true))
                {
                    thing.Destroy();
                    break;
                }
                remaining -= added;
            }
        }

        private static int CountItemInCaravan(Caravan caravan, ThingDef def)
        {
            if (caravan == null || caravan.Destroyed || def == null) return 0;
            int total = 0;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                IList<Thing> inv = p.inventory.innerContainer.InnerListForReading;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed && t.def == def)
                        total += t.stackCount;
                }
            }
            return total;
        }

        private static HashSet<int> CollectCaravanInventoryThingIds(Caravan caravan)
        {
            HashSet<int> ids = new HashSet<int>();
            if (caravan == null || caravan.Destroyed) return ids;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                IList<Thing> inv = p.inventory.innerContainer.InnerListForReading;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed)
                        ids.Add(t.thingIDNumber);
                }
            }
            return ids;
        }

        private static bool HasSharedThingIds(Caravan a, Caravan b)
        {
            HashSet<int> aIds = CollectCaravanInventoryThingIds(a);
            HashSet<int> bIds = CollectCaravanInventoryThingIds(b);
            foreach (int id in aIds)
            {
                if (bIds.Contains(id)) return true;
            }
            return false;
        }

        private static Command_Action FindCommandByLabel(List<Gizmo> gizmos, string label)
        {
            if (gizmos == null || string.IsNullOrEmpty(label)) return null;
            for (int i = 0; i < gizmos.Count; i++)
            {
                Command_Action cmd = gizmos[i] as Command_Action;
                if (cmd != null && string.Equals(cmd.defaultLabel, label, StringComparison.Ordinal))
                    return cmd;
            }
            return null;
        }

        private static Tradeable FindTradeable(List<Tradeable> list, ThingDef def, Transactor holder, int minCount)
        {
            if (list == null || def == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                Tradeable tr = list[i];
                if (tr == null || tr.IsCurrency || tr.ThingDef != def) continue;
                if (tr.CountHeldBy(holder) >= minCount)
                    return tr;
            }
            return null;
        }

        private static bool TryAdjustTradeable(Tradeable tradeable, int quantity, TradeAction desiredAction)
        {
            if (tradeable == null || quantity <= 0) return false;

            int target = desiredAction == TradeAction.PlayerBuys ? -quantity : quantity;
            if (tradeable.CanAdjustBy(target - tradeable.CountToTransfer).Accepted)
            {
                tradeable.AdjustTo(target);
                if (tradeable.ActionToDo == desiredAction && Math.Abs(tradeable.CountToTransfer) == quantity)
                    return true;
            }

            int alt = -target;
            if (tradeable.CanAdjustBy(alt - tradeable.CountToTransfer).Accepted)
            {
                tradeable.AdjustTo(alt);
                if (tradeable.ActionToDo == desiredAction && Math.Abs(tradeable.CountToTransfer) == quantity)
                    return true;
            }

            tradeable.AdjustTo(0);
            return false;
        }

        private static Window FindOpenTradeDialog()
        {
            if (Find.WindowStack == null) return null;
            if (Find.WindowStack.IsOpen<Dialog_Trade>())
            {
                return Find.WindowStack.WindowOfType<Dialog_Trade>();
            }
            IList<Window> windows = Find.WindowStack.Windows;
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                Window w = windows[i];
                if (w == null) continue;
                if (w is Dialog_Trade) return w;
                if (w.GetType().Name.IndexOf("Trade", StringComparison.OrdinalIgnoreCase) >= 0)
                    return w;
            }
            return null;
        }

        private static void CloseOpenTradeDialogs()
        {
            if (Find.WindowStack == null) return;
            Find.WindowStack.TryRemove(typeof(Dialog_Trade), false);
            IList<Window> windows = Find.WindowStack.Windows;
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                Window w = windows[i];
                if (w != null && (w is Dialog_Trade || w.GetType().Name.IndexOf("Trade", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Find.WindowStack.TryRemove(w, false);
                }
            }
        }

        private static Dialog_MessageBox FindOpenConfirmDialog()
        {
            if (Find.WindowStack == null) return null;
            IList<Window> windows = Find.WindowStack.Windows;
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                Dialog_MessageBox box = windows[i] as Dialog_MessageBox;
                if (box != null) return box;
            }
            return null;
        }

        private static Map FindBattleMapForTile(PlanetTile tile, HashSet<int> expectedEnemyIds)
        {
            if (Find.Maps == null) return null;
            for (int i = Find.Maps.Count - 1; i >= 0; i--)
            {
                Map m = Find.Maps[i];
                if (m == null) continue;
                if (tile.Valid && m.Tile == tile) return m;
            }
            for (int i = Find.Maps.Count - 1; i >= 0; i--)
            {
                Map m = Find.Maps[i];
                if (m == null || m.mapPawns == null) continue;
                IReadOnlyList<Pawn> pawns = m.mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn p = pawns[j];
                    if (p != null && expectedEnemyIds.Contains(p.thingIDNumber))
                        return m;
                }
            }
            return null;
        }

        private static bool EvaluateMainPassed(ProbeReport r)
        {
            return string.IsNullOrEmpty(r.fatalException)
                && r.errorCount == 0
                && r.extra.Count >= 18 && !r.extra.Any(x => x.Contains(": FAIL"))
                && r.playerCaravanCreated
                && r.sameTileVerified
                && r.gizmosAppeared
                && r.tradeSessionActive
                && r.tradeLeaderMatches
                && r.negotiatorMatches
                && r.realCargoVerified
                && r.buyAdjusted
                && r.sellAdjusted
                && r.tradeExecuted
                && r.buyTransferredWithoutDuplication
                && r.sellTransferredWithoutDuplication
                && r.silverConserved
                && r.noDuplicateThingIdsAfterTrade
                && r.battleMapCreated
                && r.routeRemovedAfterAttack
                && r.trafficCaravanRemovedAfterAttack
                && r.markedEngagedAfterAttack
                && r.goodwillDroppedOrHostile
                && r.sameEnemiesOnBattleMap
                && r.enemyCargoRetained
                && r.reattackBlockedNoDuplicates
                && r.missionSilverNotOffered
                && r.failedAttackRolledBack;
        }

        private static bool EvaluateUiPassed(UiProbeReport ui, int errorCount)
        {
            return string.IsNullOrEmpty(ui.fatalException)
                && errorCount == 0
                && ui.tradeGizmoFound
                && ui.tradeGizmoEnabled
                && ui.attackGizmoFound
                && ui.attackGizmoEnabled
                && ui.legacyDuplicateSuppressed
                && ui.tooltipHasFactionAndCrew
                && ui.localizationResolved
                && ui.tradeDialogOpened
                && ui.attackConfirmDialogOpened
                && ui.attackConfirmHasFactionName
                && ui.gizmosClearedAfterAttack;
        }

        private static string ResolveReportDirectory()
        {
            if (!string.IsNullOrEmpty(CustomReportDir))
                return CustomReportDir;

            string envDir = Environment.GetEnvironmentVariable("TMM_PROBE_DIR");
            if (!string.IsNullOrEmpty(envDir))
                return envDir;

            return GenFilePaths.SaveDataFolderPath;
        }

        private static void WriteReports(ProbeReport report, UiProbeReport uiReport)
        {
            try
            {
                string dir = ResolveReportDirectory();
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string mainPath = Path.Combine(dir, MainReportFileName);
                string uiPath = Path.Combine(dir, UiReportFileName);

                File.WriteAllText(mainPath, SerializeMainReport(report), Encoding.UTF8);
                File.WriteAllText(uiPath, SerializeUiReport(uiReport), Encoding.UTF8);

                Log.Message("[Turbo mega mod] Wrote probe reports: " + mainPath + " | " + uiPath);
            }
            catch (Exception ex)
            {
                Log.Error("[Turbo mega mod] Failed to write WorldTrafficInteraction probe reports: " + ex);
            }
        }

        private static string SerializeMainReport(ProbeReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            AppendJsonBool(sb, "passed", r.passed, true);
            AppendJsonString(sb, "trafficStartMode", r.trafficStartMode, true);
            AppendJsonString(sb, "factionName", r.factionName, true);
            AppendJsonInt(sb, "trafficCaravanId", r.trafficCaravanId, true);
            AppendJsonInt(sb, "tileId", r.tileId, true);
            AppendJsonString(sb, "traderKindDefName", r.traderKindDefName, true);
            AppendJsonString(sb, "buyDefName", r.buyDefName, true);
            AppendJsonString(sb, "sellDefName", r.sellDefName, true);
            AppendJsonBool(sb, "playerCaravanCreated", r.playerCaravanCreated, true);
            AppendJsonBool(sb, "sameTileVerified", r.sameTileVerified, true);
            AppendJsonBool(sb, "gizmosAppeared", r.gizmosAppeared, true);
            AppendJsonBool(sb, "tradeSessionActive", r.tradeSessionActive, true);
            AppendJsonBool(sb, "tradeLeaderMatches", r.tradeLeaderMatches, true);
            AppendJsonBool(sb, "negotiatorMatches", r.negotiatorMatches, true);
            AppendJsonInt(sb, "traderOfferedStacks", r.traderOfferedStacks, true);
            AppendJsonBool(sb, "realCargoVerified", r.realCargoVerified, true);
            AppendJsonBool(sb, "buyAdjusted", r.buyAdjusted, true);
            AppendJsonBool(sb, "sellAdjusted", r.sellAdjusted, true);
            AppendJsonBool(sb, "tradeExecuted", r.tradeExecuted, true);
            AppendJsonBool(sb, "buyTransferredWithoutDuplication", r.buyTransferredWithoutDuplication, true);
            AppendJsonBool(sb, "sellTransferredWithoutDuplication", r.sellTransferredWithoutDuplication, true);
            AppendJsonBool(sb, "silverConserved", r.silverConserved, true);
            AppendJsonBool(sb, "noDuplicateThingIdsAfterTrade", r.noDuplicateThingIdsAfterTrade, true);
            AppendJsonBool(sb, "battleMapCreated", r.battleMapCreated, true);
            AppendJsonBool(sb, "routeRemovedAfterAttack", r.routeRemovedAfterAttack, true);
            AppendJsonBool(sb, "trafficCaravanRemovedAfterAttack", r.trafficCaravanRemovedAfterAttack, true);
            AppendJsonBool(sb, "markedEngagedAfterAttack", r.markedEngagedAfterAttack, true);
            AppendJsonBool(sb, "goodwillDroppedOrHostile", r.goodwillDroppedOrHostile, true);
            AppendJsonInt(sb, "expectedEnemyCount", r.expectedEnemyCount, true);
            AppendJsonInt(sb, "matchedEnemyCount", r.matchedEnemyCount, true);
            AppendJsonBool(sb, "sameEnemiesOnBattleMap", r.sameEnemiesOnBattleMap, true);
            AppendJsonBool(sb, "enemyCargoRetained", r.enemyCargoRetained, true);
            AppendJsonBool(sb, "reattackBlockedNoDuplicates", r.reattackBlockedNoDuplicates, true);
            AppendJsonBool(sb, "missionSilverNotOffered", r.missionSilverNotOffered, true);
            AppendJsonBool(sb, "failedAttackRolledBack", r.failedAttackRolledBack, true);
            AppendJsonInt(sb, "errorCount", r.errorCount, true);
            AppendJsonString(sb, "fatalException", r.fatalException, true);
            AppendJsonStringList(sb, "extra", r.extra, true);
            AppendJsonStringList(sb, "notes", r.notes, true);
            AppendJsonStringList(sb, "errors", r.errors, false);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string SerializeUiReport(UiProbeReport ui)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            AppendJsonBool(sb, "passed", ui.passed, true);
            AppendJsonInt(sb, "gizmoCount", ui.gizmoCount, true);
            AppendJsonBool(sb, "tradeGizmoFound", ui.tradeGizmoFound, true);
            AppendJsonBool(sb, "tradeGizmoEnabled", ui.tradeGizmoEnabled, true);
            AppendJsonString(sb, "tradeGizmoLabel", ui.tradeGizmoLabel, true);
            AppendJsonString(sb, "tradeGizmoDesc", ui.tradeGizmoDesc, true);
            AppendJsonBool(sb, "attackGizmoFound", ui.attackGizmoFound, true);
            AppendJsonBool(sb, "attackGizmoEnabled", ui.attackGizmoEnabled, true);
            AppendJsonString(sb, "attackGizmoLabel", ui.attackGizmoLabel, true);
            AppendJsonString(sb, "attackGizmoDesc", ui.attackGizmoDesc, true);
            AppendJsonBool(sb, "legacyDuplicateSuppressed", ui.legacyDuplicateSuppressed, true);
            AppendJsonBool(sb, "tooltipHasFactionAndCrew", ui.tooltipHasFactionAndCrew, true);
            AppendJsonBool(sb, "localizationResolved", ui.localizationResolved, true);
            AppendJsonBool(sb, "tradeDialogOpened", ui.tradeDialogOpened, true);
            AppendJsonBool(sb, "attackConfirmDialogOpened", ui.attackConfirmDialogOpened, true);
            AppendJsonBool(sb, "attackConfirmHasFactionName", ui.attackConfirmHasFactionName, true);
            AppendJsonBool(sb, "gizmosClearedAfterAttack", ui.gizmosClearedAfterAttack, true);
            AppendJsonInt(sb, "errorCount", ui.errorCount, true);
            AppendJsonString(sb, "fatalException", ui.fatalException, false);
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
                        if (c < 32)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        private sealed class ProbeReport
        {
            public bool passed;
            public readonly List<string> extra = new List<string>();
            public string trafficStartMode = string.Empty;
            public string factionName = string.Empty;
            public int trafficCaravanId;
            public int tileId;
            public string traderKindDefName = string.Empty;
            public string buyDefName = string.Empty;
            public string sellDefName = string.Empty;
            public bool playerCaravanCreated;
            public bool sameTileVerified;
            public bool gizmosAppeared;
            public bool tradeSessionActive;
            public bool tradeLeaderMatches;
            public bool negotiatorMatches;
            public int traderOfferedStacks;
            public bool realCargoVerified;
            public bool buyAdjusted;
            public bool sellAdjusted;
            public bool tradeExecuted;
            public bool buyTransferredWithoutDuplication;
            public bool sellTransferredWithoutDuplication;
            public bool silverConserved;
            public bool noDuplicateThingIdsAfterTrade;
            public bool battleMapCreated;
            public bool routeRemovedAfterAttack;
            public bool trafficCaravanRemovedAfterAttack;
            public bool markedEngagedAfterAttack;
            public bool goodwillDroppedOrHostile;
            public int expectedEnemyCount;
            public int matchedEnemyCount;
            public bool sameEnemiesOnBattleMap;
            public bool enemyCargoRetained;
            public bool reattackBlockedNoDuplicates;
            public bool missionSilverNotOffered;
            public bool failedAttackRolledBack;
            public int errorCount;
            public string fatalException = string.Empty;
            public List<string> notes = new List<string>();
            public List<string> errors = new List<string>();
        }

        private sealed class UiProbeReport
        {
            public bool passed;
            public int gizmoCount;
            public bool tradeGizmoFound;
            public bool tradeGizmoEnabled;
            public string tradeGizmoLabel = string.Empty;
            public string tradeGizmoDesc = string.Empty;
            public bool attackGizmoFound;
            public bool attackGizmoEnabled;
            public string attackGizmoLabel = string.Empty;
            public string attackGizmoDesc = string.Empty;
            public bool legacyDuplicateSuppressed;
            public bool tooltipHasFactionAndCrew;
            public bool localizationResolved;
            public bool tradeDialogOpened;
            public bool attackConfirmDialogOpened;
            public bool attackConfirmHasFactionName;
            public bool gizmosClearedAfterAttack;
            public int errorCount;
            public string fatalException = string.Empty;
        }
    }
}
