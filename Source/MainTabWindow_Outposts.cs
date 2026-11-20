using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class MainTabWindow_Outposts : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize
        {
            get { return new Vector2(1040f, 680f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont origFont = Text.Font;
            TextAnchor origAnchor = Text.Anchor;
            Color origColor = GUI.color;
            bool origWrap = Text.WordWrap;

            try
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, 0f, 400f, 35f), "StrategicOutpost_TabTitle".Translate());
                Text.Font = GameFont.Small;

                List<StrategicOutpost> outposts = new List<StrategicOutpost>();
                if (Find.WorldObjects != null)
                {
                    List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
                    for (int i = 0; i < all.Count; i++)
                    {
                        StrategicOutpost o = all[i] as StrategicOutpost;
                        if (o != null && o.Spawned)
                        {
                            outposts.Add(o);
                        }
                    }
                }

                Map homeMap = OutpostPodLogistics.GetHomeMap();
                float headerY = 40f;

                if (homeMap != null)
                {
                    int homeFuel = OutpostPodLogistics.CountResource(homeMap, ThingDefOf.Chemfuel);
                    int homeSilver = OutpostPodLogistics.CountResource(homeMap, ThingDefOf.Silver);
                    string homeName = homeMap.Parent != null ? homeMap.Parent.LabelCap.ToString() : "Colony".Translate().ToString();
                    string homeSummary = "StrategicOutpost_HomeColonyBalance".Translate(homeName, homeFuel, homeSilver);
                    GUI.color = Color.gray;
                    Widgets.Label(new Rect(0f, headerY, inRect.width, 24f), homeSummary);
                    GUI.color = Color.white;
                }

                float mainY = headerY + 28f;
                Rect listBounds = new Rect(0f, mainY, inRect.width, inRect.height - mainY);

                if (outposts.Count == 0)
                {
                    DrawEmptyState(listBounds);
                }
                else
                {
                    DrawOutpostsList(listBounds, outposts, homeMap);
                }
            }
            finally
            {
                Text.Font = origFont;
                Text.Anchor = origAnchor;
                GUI.color = origColor;
                Text.WordWrap = origWrap;
            }
        }

        private void DrawEmptyState(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(20f);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 35f), "StrategicOutpost_NoOutpostsFound".Translate());
            Text.Font = GameFont.Small;

            string guide = "StrategicOutpost_EmptyGuideText".Translate();
            Widgets.Label(new Rect(inner.x, inner.y + 45f, inner.width, inner.height - 45f), guide);
        }

        private void DrawOutpostsList(Rect rect, List<StrategicOutpost> outposts, Map homeMap)
        {
            float rowHeight = 64f;
            float totalHeight = (float)outposts.Count * (rowHeight + 6f);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(rect.height, totalHeight));

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            float curY = 0f;

            for (int i = 0; i < outposts.Count; i++)
            {
                StrategicOutpost outpost = outposts[i];
                Rect rowRect = new Rect(0f, curY, viewRect.width, rowHeight);
                DrawOutpostRow(rowRect, outpost, homeMap);
                curY += rowHeight + 6f;
            }

            Widgets.EndScrollView();
        }

        private void DrawOutpostRow(Rect rect, StrategicOutpost outpost, Map homeMap)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);

            // Left block: Name, level, specialization
            float col1Width = 260f;
            Rect col1 = new Rect(inner.x, inner.y, col1Width, inner.height);
            string title = outpost.LabelCap + " (" + "StrategicOutpost_InspectLevelShort".Translate(outpost.level, 10) + ")";
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(col1.x, col1.y, col1.width, 22f), title);

            string specText = ("StrategicOutpost_Spec_" + outpost.specialization.ToString()).Translate();
            GUI.color = Color.cyan;
            Widgets.Label(new Rect(col1.x, col1.y + 22f, col1.width, 20f), specText);
            GUI.color = Color.white;

            // Distance from home map
            if (homeMap != null && Find.WorldGrid != null)
            {
                float dist = Find.WorldGrid.ApproxDistanceInTiles(homeMap.Tile, outpost.Tile);
                float days = dist / 3.3f; // Approximate caravan travel days
                string distStr = "StrategicOutpost_DistanceInfo".Translate(dist.ToString("F1"), days.ToString("F1"));
                GUI.color = Color.gray;
                Widgets.Label(new Rect(col1.x, col1.y + 40f, col1.width, 18f), distStr);
                GUI.color = Color.white;
            }

            // Col 2: Workers & Silver
            float col2X = col1.xMax + 10f;
            float col2Width = 190f;
            Rect col2 = new Rect(col2X, inner.y, col2Width, inner.height);

            string modeStr = outpost.operationalMode == OutpostOperationalMode.Physical
                ? "StrategicOutpost_Mode_Physical".Translate()
                : "StrategicOutpost_Mode_Autonomous".Translate();
            string workersStr = "StrategicOutpost_InspectWorkers".Translate(outpost.PawnCount, outpost.MaxCapacity) + "\n(" + modeStr + ")";
            Widgets.Label(new Rect(col2.x, col2.y, col2.width, 36f), workersStr);

            int silverCount = outpost.RawMaterials != null ? outpost.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver) : 0;
            string silverStr = "StrategicOutpost_SilverCount".Translate(silverCount);
            GUI.color = Color.yellow;
            Widgets.Label(new Rect(col2.x, col2.y + 38f, col2.width, 20f), silverStr);
            GUI.color = Color.white;

            // Col 3: Action Buttons
            float btnX = col2.xMax + 10f;
            float btnW = 90f;
            float btnH = 26f;

            // 1. Manage dialog
            Rect btnManage = new Rect(btnX, inner.y + 2f, btnW, btnH);
            if (Widgets.ButtonText(btnManage, "StrategicOutpost_ManageBtn".Translate()))
            {
                Find.WindowStack.Add(new Dialog_StrategicOutpost(outpost, null));
            }

            // 2. Jump to map
            Rect btnJump = new Rect(btnX, inner.y + 32f, btnW, btnH);
            if (Widgets.ButtonText(btnJump, "StrategicOutpost_ViewMapBtnShort".Translate()))
            {
                CameraJumper.TryJump(outpost);
            }

            btnX += btnW + 6f;
            btnW = 105f;

            // 3. Recall worker
            Rect btnRecall = new Rect(btnX, inner.y + 2f, btnW, btnH);
            bool canRecallAny = outpost.Workers.Count > 1 && outpost.operationalMode == OutpostOperationalMode.Autonomous;
            if (Widgets.ButtonText(btnRecall, "StrategicOutpost_RecallBtn".Translate(), true, true, canRecallAny))
            {
                ShowRecallWorkerMenu(outpost, homeMap);
            }
            if (outpost.operationalMode == OutpostOperationalMode.Physical)
            {
                TooltipHandler.TipRegion(btnRecall, "StrategicOutpost_MustReturnToAutonomousFirst".Translate());
            }
            else if (outpost.Workers.Count <= 1)
            {
                TooltipHandler.TipRegion(btnRecall, "StrategicOutpost_CannotRemoveLastWorker".Translate());
            }

            // 4. Send workers
            Rect btnSendWorker = new Rect(btnX, inner.y + 32f, btnW, btnH);
            bool canSendWorker = outpost.PawnCount < outpost.MaxCapacity && homeMap != null;
            if (Widgets.ButtonText(btnSendWorker, "StrategicOutpost_SendWorkerBtn".Translate(), true, true, canSendWorker))
            {
                ShowSendWorkerMenu(outpost, homeMap);
            }
            if (outpost.PawnCount >= outpost.MaxCapacity)
            {
                TooltipHandler.TipRegion(btnSendWorker, "StrategicOutpost_CapacityFull".Translate(outpost.MaxCapacity));
            }

            btnX += btnW + 6f;
            btnW = 105f;

            // 5. Send silver
            Rect btnSendSilver = new Rect(btnX, inner.y + 2f, btnW, btnH);
            bool canSendSilver = homeMap != null && OutpostPodLogistics.CountResource(homeMap, ThingDefOf.Silver) > 0;
            if (Widgets.ButtonText(btnSendSilver, "StrategicOutpost_SendSilverBtn".Translate(), true, true, canSendSilver))
            {
                ShowSendSilverMenu(outpost, homeMap);
            }

            // 6. Upgrade / Top-up
            Rect btnUpgrade = new Rect(btnX, inner.y + 32f, btnW, btnH);
            if (outpost.CanUpgrade)
            {
                int cost = outpost.NextUpgradeCost;
                if (silverCount >= cost)
                {
                    if (Widgets.ButtonText(btnUpgrade, "StrategicOutpost_UpgradeNowBtn".Translate()))
                    {
                        string reason;
                        if (!outpost.TryUpgrade(out reason, null))
                        {
                            Messages.Message(reason, outpost, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                }
                else
                {
                    int needed = cost - silverCount;
                    float mass = (float)needed * (ThingDefOf.Silver != null ? ThingDefOf.Silver.BaseMass : 0.008f);
                    int totalFuel, fuelDeduct, silverDeduct;
                    bool canAfford;
                    string summary;
                    if (homeMap != null)
                    {
                        OutpostPodLogistics.CalculateCost(homeMap.Tile, outpost.Tile, mass, homeMap,
                            out totalFuel, out fuelDeduct, out silverDeduct, out canAfford, out summary);
                        int totalHomeSilverNeeded = needed + silverDeduct;
                        bool canAffordTotal = OutpostPodLogistics.CountResource(homeMap, ThingDefOf.Silver) >= totalHomeSilverNeeded;

                        if (Widgets.ButtonText(btnUpgrade, "StrategicOutpost_PodTopUpBtn".Translate(), true, true, canAffordTotal))
                        {
                            ExecutePodTopUp(outpost, homeMap, needed, mass);
                        }
                        string tip = "StrategicOutpost_PodTopUpTip".Translate(needed, fuelDeduct, silverDeduct, totalHomeSilverNeeded);
                        TooltipHandler.TipRegion(btnUpgrade, tip);
                    }
                    else
                    {
                        Widgets.ButtonText(btnUpgrade, "StrategicOutpost_PodTopUpBtn".Translate(), true, true, false);
                    }
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.ButtonText(btnUpgrade, "StrategicOutpost_MaxLevelShort".Translate(), true, true, false);
                GUI.color = Color.white;
            }
        }

        private void ShowRecallWorkerMenu(StrategicOutpost outpost, Map homeMap)
        {
            if (outpost == null || homeMap == null) return;
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            List<Pawn> workers = outpost.Workers.InnerListForReading.ToList();
            for (int i = 0; i < workers.Count; i++)
            {
                Pawn p = workers[i];
                float mass = p.GetStatValue(StatDefOf.Mass);
                int totalFuel, fuelDeduct, silverDeduct;
                bool canAfford;
                string summary;
                OutpostPodLogistics.CalculateCost(outpost.Tile, homeMap.Tile, mass, homeMap,
                    out totalFuel, out fuelDeduct, out silverDeduct, out canAfford, out summary);

                string label = p.LabelShort + " (" + summary + ")";
                Action action = delegate
                {
                    string payError;
                    if (!OutpostPodLogistics.TryPayLaunchCost(homeMap, outpost.Tile, homeMap.Tile, mass, out payError))
                    {
                        Messages.Message(payError, outpost, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    outpost.Workers.Remove(p);
                    if (Find.WorldPawns != null && Find.WorldPawns.Contains(p))
                    {
                        Find.WorldPawns.RemovePawn(p);
                    }

                    List<Thing> cargo = new List<Thing> { p };
                    TransportersArrivalAction arrival = new TransportersArrivalAction_ColonyRecall(homeMap.Tile);
                    OutpostPodLogistics.Launch(outpost.Tile, homeMap.Tile, cargo, arrival);
                    Messages.Message("StrategicOutpost_WorkerRecalledPodLaunched".Translate(p.LabelShort, outpost.LabelCap), MessageTypeDefOf.PositiveEvent);
                };

                FloatMenuOption opt = new FloatMenuOption(label, canAfford ? action : null);
                options.Add(opt);
            }

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private void ShowSendWorkerMenu(StrategicOutpost outpost, Map homeMap)
        {
            if (outpost == null || homeMap == null) return;
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // 1. Transport pod
            options.Add(new FloatMenuOption("StrategicOutpost_ByTransportPod".Translate(), delegate
            {
                ShowSendWorkerPodMenu(outpost, homeMap);
            }));

            // 2. Walking caravan
            options.Add(new FloatMenuOption("StrategicOutpost_ByWalkingCaravan".Translate(), delegate
            {
                Find.WindowStack.Add(new Dialog_OutpostCaravan(outpost, homeMap, false));
            }));

            // 3. Transport vehicle (soon)
            FloatMenuOption vehicleOpt = new FloatMenuOption("StrategicOutpost_ByTransportVehicle".Translate(), null, MenuOptionPriority.Default, delegate(Rect r)
            {
                TooltipHandler.TipRegion(r, "StrategicOutpost_TransportVehicleSoonTip".Translate());
            }, null, 0f, null, null, true, 0);
            vehicleOpt.Disabled = true;
            options.Add(vehicleOpt);

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowSendWorkerPodMenu(StrategicOutpost outpost, Map homeMap)
        {
            if (outpost == null || homeMap == null) return;
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            List<Pawn> colonists = homeMap.mapPawns.FreeColonistsSpawned
                .Where(p => StrategicOutpostCaravanUtility.IsSuitableColonist(p) && !p.Drafted && !p.InMentalState)
                .ToList();

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                float mass = p.GetStatValue(StatDefOf.Mass);
                int totalFuel, fuelDeduct, silverDeduct;
                bool canAfford;
                string summary;
                OutpostPodLogistics.CalculateCost(homeMap.Tile, outpost.Tile, mass, homeMap,
                    out totalFuel, out fuelDeduct, out silverDeduct, out canAfford, out summary);

                string label = p.LabelShort + " (" + summary + ")";
                Action action = delegate
                {
                    if (outpost.PawnCount >= outpost.MaxCapacity)
                    {
                        Messages.Message("StrategicOutpost_CapacityFull".Translate(outpost.MaxCapacity), outpost, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    string payError;
                    if (!OutpostPodLogistics.TryPayLaunchCost(homeMap, homeMap.Tile, outpost.Tile, mass, out payError))
                    {
                        Messages.Message(payError, outpost, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    if (p.ownership != null)
                    {
                        p.ownership.UnclaimBed();
                    }
                    p.DeSpawn(DestroyMode.Vanish);

                    List<Thing> cargo = new List<Thing> { p };
                    TransportersArrivalAction arrival = new TransportersArrivalAction_OutpostArrival(outpost.Tile, false);
                    OutpostPodLogistics.Launch(homeMap.Tile, outpost.Tile, cargo, arrival);
                    Messages.Message("StrategicOutpost_WorkerSentPodLaunched".Translate(p.LabelShort, outpost.LabelCap), MessageTypeDefOf.PositiveEvent);
                };

                FloatMenuOption opt = new FloatMenuOption(label, canAfford ? action : null);
                options.Add(opt);
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("StrategicOutpost_NoEligibleColonists".Translate(), null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowSendSilverMenu(StrategicOutpost outpost, Map homeMap)
        {
            if (outpost == null || homeMap == null) return;
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // 1. Transport pod
            options.Add(new FloatMenuOption("StrategicOutpost_ByTransportPod".Translate(), delegate
            {
                ShowSendSilverPodMenu(outpost, homeMap);
            }));

            // 2. Walking caravan
            options.Add(new FloatMenuOption("StrategicOutpost_ByWalkingCaravan".Translate(), delegate
            {
                Find.WindowStack.Add(new Dialog_OutpostCaravan(outpost, homeMap, true));
            }));

            // 3. Transport vehicle (soon)
            FloatMenuOption vehicleOpt = new FloatMenuOption("StrategicOutpost_ByTransportVehicle".Translate(), null, MenuOptionPriority.Default, delegate(Rect r)
            {
                TooltipHandler.TipRegion(r, "StrategicOutpost_TransportVehicleSoonTip".Translate());
            }, null, 0f, null, null, true, 0);
            vehicleOpt.Disabled = true;
            options.Add(vehicleOpt);

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowSendSilverPodMenu(StrategicOutpost outpost, Map homeMap)
        {
            if (outpost == null || homeMap == null) return;
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            int[] amounts = new[] { 100, 250, 500, 1000, 2000 };
            int availSilver = OutpostPodLogistics.CountResource(homeMap, ThingDefOf.Silver);

            for (int i = 0; i < amounts.Length; i++)
            {
                int amount = amounts[i];
                float mass = (float)amount * (ThingDefOf.Silver != null ? ThingDefOf.Silver.BaseMass : 0.008f);

                int totalFuel, fuelDeduct, silverDeduct;
                bool canAffordLaunch;
                string summary;
                OutpostPodLogistics.CalculateCost(homeMap.Tile, outpost.Tile, mass, homeMap,
                    out totalFuel, out fuelDeduct, out silverDeduct, out canAffordLaunch, out summary);

                int totalSilverNeeded = amount + silverDeduct;
                bool canAfford = availSilver >= totalSilverNeeded;

                string label = amount + " " + "StrategicOutpost_SilverLabelShort".Translate() +
                    " (" + "StrategicOutpost_PodCostSummaryBrief".Translate(fuelDeduct, silverDeduct) + ")";

                Action action = delegate
                {
                    if (OutpostPodLogistics.CountResource(homeMap, ThingDefOf.Silver) < totalSilverNeeded)
                    {
                        Messages.Message("StrategicOutpost_NotEnoughSilverToSend".Translate(totalSilverNeeded), MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    string payError;
                    if (!OutpostPodLogistics.TryPayLaunchCost(homeMap, homeMap.Tile, outpost.Tile, mass, out payError))
                    {
                        Messages.Message(payError, outpost, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    // Deduct cargo silver
                    OutpostPodLogistics.DeductResource(homeMap, ThingDefOf.Silver, amount);

                    Thing silverCargo = ThingMaker.MakeThing(ThingDefOf.Silver);
                    silverCargo.stackCount = amount;

                    List<Thing> cargo = new List<Thing> { silverCargo };
                    TransportersArrivalAction arrival = new TransportersArrivalAction_OutpostArrival(outpost.Tile, false);
                    OutpostPodLogistics.Launch(homeMap.Tile, outpost.Tile, cargo, arrival);
                    Messages.Message("StrategicOutpost_SilverSentPodLaunched".Translate(amount, outpost.LabelCap), MessageTypeDefOf.PositiveEvent);
                };

                FloatMenuOption opt = new FloatMenuOption(label, canAfford ? action : null);
                options.Add(opt);
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ExecutePodTopUp(StrategicOutpost outpost, Map homeMap, int neededSilver, float mass)
        {
            if (outpost == null || homeMap == null || neededSilver <= 0) return;

            int totalFuel, fuelDeduct, silverDeduct;
            bool canAffordLaunch;
            string summary;
            OutpostPodLogistics.CalculateCost(homeMap.Tile, outpost.Tile, mass, homeMap,
                out totalFuel, out fuelDeduct, out silverDeduct, out canAffordLaunch, out summary);

            int totalSilverNeeded = neededSilver + silverDeduct;
            int homeSilver = OutpostPodLogistics.CountResource(homeMap, ThingDefOf.Silver);

            if (homeSilver < totalSilverNeeded)
            {
                Messages.Message("StrategicOutpost_NotEnoughSilverForTopUp".Translate(totalSilverNeeded, homeSilver), MessageTypeDefOf.RejectInput, false);
                return;
            }

            string payError;
            if (!OutpostPodLogistics.TryPayLaunchCost(homeMap, homeMap.Tile, outpost.Tile, mass, out payError))
            {
                Messages.Message(payError, outpost, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Deduct the needed silver to be transported
            OutpostPodLogistics.DeductResource(homeMap, ThingDefOf.Silver, neededSilver);

            Thing silverCargo = ThingMaker.MakeThing(ThingDefOf.Silver);
            silverCargo.stackCount = neededSilver;

            List<Thing> cargo = new List<Thing> { silverCargo };
            TransportersArrivalAction arrival = new TransportersArrivalAction_OutpostArrival(outpost.Tile, true);
            OutpostPodLogistics.Launch(homeMap.Tile, outpost.Tile, cargo, arrival);
            Messages.Message("StrategicOutpost_TopUpPodLaunched".Translate(neededSilver, outpost.LabelCap), MessageTypeDefOf.PositiveEvent);
        }
    }
}
