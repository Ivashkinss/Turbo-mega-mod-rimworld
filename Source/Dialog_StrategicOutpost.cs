using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class Dialog_StrategicOutpost : Window
    {
        private StrategicOutpost outpost;
        private Caravan localCaravan;
        private int currentTab = 0;
        private string[] tabNames = new string[] { "Production", "Personnel", "Logistics", "Evacuation" };

        private Vector2 scrollPosProducts = Vector2.zero;
        private Vector2 scrollPosWorkers = Vector2.zero;
        private Vector2 scrollPosCaravan = Vector2.zero;
        private Vector2 scrollPosRaw = Vector2.zero;
        private Vector2 scrollPosProd = Vector2.zero;

        public override Vector2 InitialSize
        {
            get { return new Vector2(740f, 560f); }
        }

        public Dialog_StrategicOutpost(StrategicOutpost outpost, Caravan localCaravan = null)
        {
            this.outpost = outpost;
            this.localCaravan = localCaravan;
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var font = Text.Font;
            var color = GUI.color;
            try
            {
                DrawContents(inRect);
            }
            finally
            {
                Text.Font = font;
                GUI.color = color;
            }
        }

        private void DrawContents(Rect inRect)
        {
            if (outpost == null || outpost.Faction != Faction.OfPlayer)
            {
                Close();
                return;
            }

            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width, 35f);
            Widgets.Label(titleRect, outpost.LabelCap);

            // Subtitle with Mode
            Text.Font = GameFont.Small;
            Rect subtitleRect = new Rect(0f, 35f, inRect.width, 24f);
            string specLabel = ("StrategicOutpost_Spec_" + outpost.specialization.ToString()).Translate();
            string modeStr = outpost.operationalMode == OutpostOperationalMode.Autonomous ?
                "StrategicOutpost_Mode_Autonomous".Translate().ToString() :
                "StrategicOutpost_Mode_Physical".Translate().ToString();
            string subText = "StrategicOutpost_HeaderInfoFull".Translate(outpost.level, 10, outpost.MaxCapacity, specLabel, modeStr);
            Widgets.Label(subtitleRect, subText);

            // Tab bar
            Rect tabRect = new Rect(0f, 65f, inRect.width, 32f);
            float tabWidth = inRect.width / (float)tabNames.Length;
            for (int i = 0; i < tabNames.Length; i++)
            {
                Rect bRect = new Rect(tabRect.x + (float)i * tabWidth, tabRect.y, tabWidth - 4f, tabRect.height);
                bool isActive = (currentTab == i);
                if (isActive)
                {
                    Widgets.DrawHighlight(bRect);
                }
                string tabLabel = ("StrategicOutpost_Tab_" + tabNames[i]).Translate();
                if (Widgets.ButtonText(bRect, tabLabel))
                {
                    currentTab = i;
                }
            }

            // Main body
            Rect mainRect = new Rect(0f, 105f, inRect.width, inRect.height - 155f);
            switch (currentTab)
            {
                case 0:
                    DrawProductionTab(mainRect);
                    break;
                case 1:
                    DrawPersonnelTab(mainRect);
                    break;
                case 2:
                    DrawLogisticsTab(mainRect);
                    break;
                case 3:
                    DrawEvacuationTab(mainRect);
                    break;
            }
        }

        private void DrawProductionTab(Rect rect)
        {
            float halfWidth = (rect.width - 10f) / 2f;
            Rect leftCol = new Rect(0f, 0f, halfWidth, rect.height);
            Rect rightCol = new Rect(halfWidth + 10f, 0f, halfWidth, rect.height);

            // Left: Settings & Overview
            Widgets.DrawMenuSection(leftCol);
            Rect leftInner = leftCol.ContractedBy(8f);

            float y = leftInner.y;
            Widgets.Label(new Rect(leftInner.x, y, leftInner.width, 24f), "StrategicOutpost_SpecializationTitle".Translate());
            y += 26f;

            string specDesc = ("StrategicOutpost_SpecDesc_" + outpost.specialization.ToString()).Translate();
            Widgets.Label(new Rect(leftInner.x, y, leftInner.width, 60f), specDesc);
            y += 65f;

            Rect changeSpecBtn = new Rect(leftInner.x, y, 200f, 30f);
            if (Widgets.ButtonText(changeSpecBtn, "StrategicOutpost_ChangeSpecBtn".Translate()))
            {
                OpenSpecializationSelector();
            }
            y += 40f;

            // Delivery Target Colony
            Widgets.Label(new Rect(leftInner.x, y, leftInner.width, 24f), "StrategicOutpost_LogisticsTitle".Translate());
            y += 26f;

            string targetName = "StrategicOutpost_NoTargetColony".Translate();
            if (outpost.targetColonyTile >= 0)
            {
                Settlement s = Find.WorldObjects.SettlementAt(outpost.targetColonyTile);
                if (s != null && s.Faction == Faction.OfPlayer)
                {
                    targetName = s.LabelCap;
                }
            }
            Widgets.Label(new Rect(leftInner.x, y, leftInner.width, 24f), "StrategicOutpost_CurrentTargetColony".Translate(targetName));
            y += 28f;

            Rect changeTargetBtn = new Rect(leftInner.x, y, 200f, 30f);
            if (Widgets.ButtonText(changeTargetBtn, "StrategicOutpost_ChangeTargetColonyBtn".Translate()))
            {
                OpenColonySelector();
            }

            // Right: Product lines or Watchtower details
            Widgets.DrawMenuSection(rightCol);
            Rect rightInner = rightCol.ContractedBy(8f);

            if (outpost.specialization == StrategicOutpostSpecialization.Watchtower)
            {
                Widgets.Label(new Rect(rightInner.x, rightInner.y, rightInner.width, 28f), "StrategicOutpost_WatchtowerHeader".Translate());
                string watchDesc = "StrategicOutpost_WatchtowerDescFull".Translate(outpost.lastScannedHostileSettlements, outpost.lastScannedTotalSettlements);
                Widgets.Label(new Rect(rightInner.x, rightInner.y + 35f, rightInner.width, 180f), watchDesc);
            }
            else
            {
                Widgets.Label(new Rect(rightInner.x, rightInner.y, rightInner.width, 24f), "StrategicOutpost_ProductLinesTitle".Translate());

                List<StrategicOutpostProductDef> available = outpost.GetAvailableProductsFor(outpost.specialization);
                Rect scrollRect = new Rect(rightInner.x, rightInner.y + 28f, rightInner.width, rightInner.height - 28f);
                Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, Mathf.Max(scrollRect.height, (float)available.Count * 44f));

                Widgets.BeginScrollView(scrollRect, ref scrollPosProducts, viewRect);
                float curY = 0f;

                for (int i = 0; i < available.Count; i++)
                {
                    StrategicOutpostProductDef p = available[i];
                    Rect pRow = new Rect(0f, curY, viewRect.width, 40f);

                    string unlockReason;
                    bool unlocked = p.IsUnlocked(out unlockReason);

                    bool isSelected = outpost.selectedProductDefNames.Contains(p.defName);
                    bool wasSelected = isSelected;

                    if (!unlocked)
                    {
                        GUI.color = Color.gray;
                    }

                    string label = "StrategicOutpost_ProductYield".Translate(p.LabelCap, outpost.ExpectedYield(p).ToString("0.#"));
                    if (!unlocked)
                    {
                        label += " [" + unlockReason + "]";
                    }

                    Widgets.CheckboxLabeled(pRow, label, ref isSelected, !unlocked);
                    GUI.color = Color.white;

                    if (unlocked && isSelected != wasSelected)
                    {
                        if (isSelected)
                        {
                            if (!outpost.selectedProductDefNames.Contains(p.defName))
                            {
                                outpost.selectedProductDefNames.Add(p.defName);
                            }
                        }
                        else
                        {
                            if (outpost.selectedProductDefNames.Count > 1)
                            {
                                outpost.selectedProductDefNames.Remove(p.defName);
                            }
                            else
                            {
                                Messages.Message("StrategicOutpost_MustKeepOneProduct".Translate(), outpost, MessageTypeDefOf.RejectInput, false);
                            }
                        }
                    }

                    curY += 42f;
                }

                Widgets.EndScrollView();
            }
        }

        private void DrawPersonnelTab(Rect rect)
        {
            float y = 0f;

            // Mode controls bar
            Rect modeBar = new Rect(0f, y, rect.width, 42f);
            Widgets.DrawMenuSection(modeBar);
            Rect mInner = modeBar.ContractedBy(6f);

            string curModeName = outpost.operationalMode == OutpostOperationalMode.Autonomous ?
                "StrategicOutpost_Mode_Autonomous".Translate().ToString() :
                "StrategicOutpost_Mode_Physical".Translate().ToString();
            Widgets.Label(new Rect(mInner.x, mInner.y + 4f, 260f, 26f), "StrategicOutpost_CurrentMode".Translate(curModeName));

            float btnX = mInner.xMax - 220f;
            if (outpost.operationalMode == OutpostOperationalMode.Autonomous)
            {
                Rect deployBtn = new Rect(btnX, mInner.y, 210f, 30f);
                if (Widgets.ButtonText(deployBtn, "StrategicOutpost_DeployPersonnel_Label".Translate()))
                {
                    string fail;
                    if (!outpost.DeployPersonnelToMap(out fail))
                    {
                        Messages.Message(fail, outpost, MessageTypeDefOf.RejectInput, false);
                    }
                }
            }
            else
            {
                Rect recallBtn = new Rect(btnX, mInner.y, 210f, 30f);
                if (Widgets.ButtonText(recallBtn, "StrategicOutpost_RecallPersonnel_Label".Translate()))
                {
                    string fail;
                    if (!outpost.RecallPersonnelToAutonomous(out fail))
                    {
                        Messages.Message(fail, outpost, MessageTypeDefOf.RejectInput, false);
                    }
                }
            }

            if (outpost.HasMap && outpost.Map != null)
            {
                Rect viewMapBtn = new Rect(btnX - 160f, mInner.y, 150f, 30f);
                if (Widgets.ButtonText(viewMapBtn, "StrategicOutpost_ViewMap_Label".Translate()))
                {
                    CameraJumper.TryJump(outpost.Map.Center, outpost.Map);
                    Close();
                    return;
                }
            }

            y += 50f;

            float remainingHeight = rect.height - y;
            float halfWidth = (rect.width - 10f) / 2f;
            Rect leftCol = new Rect(0f, y, halfWidth, remainingHeight);
            Rect rightCol = new Rect(halfWidth + 10f, y, halfWidth, remainingHeight);

            // Left: Workers inside Outpost
            Widgets.DrawMenuSection(leftCol);
            Rect leftInner = leftCol.ContractedBy(8f);
            Widgets.Label(new Rect(leftInner.x, leftInner.y, leftInner.width, 24f), "StrategicOutpost_WorkersInOutpost".Translate(outpost.PawnCount, outpost.MaxCapacity));

            Rect leftScroll = new Rect(leftInner.x, leftInner.y + 28f, leftInner.width, leftInner.height - 28f);
            List<Pawn> workers = outpost.AllPawns.ToList();
            Rect leftView = new Rect(0f, 0f, leftScroll.width - 16f, Mathf.Max(leftScroll.height, (float)workers.Count * 40f));

            Widgets.BeginScrollView(leftScroll, ref scrollPosWorkers, leftView);
            float wy = 0f;
            for (int i = 0; i < workers.Count; i++)
            {
                Pawn w = workers[i];
                Rect wRow = new Rect(0f, wy, leftView.width, 36f);
                Widgets.DrawHighlightIfMouseover(wRow);
                Widgets.Label(new Rect(wRow.x + 4f, wRow.y + 6f, wRow.width - 110f, 24f), w.LabelShortCap);

                if (outpost.CanInteract(localCaravan))
                {
                    Rect toCaravanBtn = new Rect(wRow.xMax - 100f, wRow.y + 4f, 96f, 28f);
                    bool canRemove = outpost.Animals.Contains(w) || outpost.PawnCount > 1;
                    if (Widgets.ButtonText(toCaravanBtn, "StrategicOutpost_ToCaravanBtn".Translate(), true, true, canRemove))
                    {
                        string removeError;
                        if (!outpost.RemovePawnToCaravan(localCaravan, w, out removeError))
                        {
                            Messages.Message(removeError, outpost, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                }
                wy += 38f;
            }
            Widgets.EndScrollView();

            // Right: Local Caravan Colonists
            Widgets.DrawMenuSection(rightCol);
            Rect rightInner = rightCol.ContractedBy(8f);

            if (outpost.CanInteract(localCaravan))
            {
                Widgets.Label(new Rect(rightInner.x, rightInner.y, rightInner.width, 24f), "StrategicOutpost_CaravanColonists".Translate());
                Rect rightScroll = new Rect(rightInner.x, rightInner.y + 28f, rightInner.width, rightInner.height - 28f);
                List<Pawn> cPawns = localCaravan.PawnsListForReading.ToList();
                Rect rightView = new Rect(0f, 0f, rightScroll.width - 16f, Mathf.Max(rightScroll.height, (float)cPawns.Count * 40f));

                Widgets.BeginScrollView(rightScroll, ref scrollPosCaravan, rightView);
                float cy = 0f;
                for (int i = 0; i < cPawns.Count; i++)
                {
                    Pawn cp = cPawns[i];
                    Rect cRow = new Rect(0f, cy, rightView.width, 36f);
                    Widgets.DrawHighlightIfMouseover(cRow);
                    Widgets.Label(new Rect(cRow.x + 4f, cRow.y + 6f, cRow.width - 110f, 24f), cp.LabelShortCap);

                    Rect toOutpostBtn = new Rect(cRow.xMax - 100f, cRow.y + 4f, 96f, 28f);
                    bool canAdd = cp.RaceProps.Animal || (StrategicOutpostCaravanUtility.IsSuitableColonist(cp) && outpost.PawnCount < outpost.MaxCapacity);

                    if (Widgets.ButtonText(toOutpostBtn, "StrategicOutpost_ToOutpostBtn".Translate(), true, true, canAdd))
                    {
                        string addError;
                        if (!outpost.AddPawnFromCaravan(localCaravan, cp, out addError))
                        {
                            Messages.Message(addError, outpost, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                    cy += 38f;
                }
                Widgets.EndScrollView();
            }
            else
            {
                Widgets.Label(new Rect(rightInner.x, rightInner.y, rightInner.width, 80f), "StrategicOutpost_NoCaravanPresentForTransfer".Translate());
            }
        }

        private void DrawLogisticsTab(Rect rect)
        {
            float topY = 0f;
            if (outpost.HasMap && outpost.Map != null)
            {
                Rect unloadBar = new Rect(0f, topY, rect.width, 36f);
                Widgets.DrawMenuSection(unloadBar);
                Rect uInner = unloadBar.ContractedBy(4f);

                Widgets.Label(new Rect(uInner.x + 4f, uInner.y + 4f, 350f, 24f), "StrategicOutpost_UnloadCargoPrompt".Translate());
                Rect unloadBtn = new Rect(uInner.xMax - 220f, uInner.y, 210f, 28f);
                if (Widgets.ButtonText(unloadBtn, "StrategicOutpost_UnloadCargo_Label".Translate()))
                {
                    string rep;
                    outpost.UnloadCargoToMap(out rep);
                }
                topY += 44f;
            }

            float remainingHeight = rect.height - topY;
            float halfWidth = (rect.width - 10f) / 2f;
            Rect leftCol = new Rect(0f, topY, halfWidth, remainingHeight);
            Rect rightCol = new Rect(halfWidth + 10f, topY, halfWidth, remainingHeight);

            // Left: Raw Materials Storage
            Widgets.DrawMenuSection(leftCol);
            Rect leftInner = leftCol.ContractedBy(8f);
            Widgets.Label(new Rect(leftInner.x, leftInner.y, leftInner.width, 24f), "StrategicOutpost_RawMaterialsTitle".Translate(outpost.RawMaterials.Count));
            if (outpost.CanInteract(localCaravan) && Widgets.ButtonText(new Rect(leftInner.xMax - 110f, leftInner.y, 105f, 25f), "StrategicOutpost_Deposit".Translate()))
            {
                OpenDepositMenu();
            }

            Rect leftScroll = new Rect(leftInner.x, leftInner.y + 30f, leftInner.width, leftInner.height - 30f);
            List<Thing> rawList = outpost.RawMaterials.InnerListForReading.ToList();
            Rect leftView = new Rect(0f, 0f, leftScroll.width - 16f, Mathf.Max(leftScroll.height, (float)rawList.Count * 36f));

            Widgets.BeginScrollView(leftScroll, ref scrollPosRaw, leftView);
            float ry = 0f;
            for (int i = 0; i < rawList.Count; i++)
            {
                Thing t = rawList[i];
                Rect row = new Rect(0f, ry, leftView.width, 32f);
                Widgets.DrawHighlightIfMouseover(row);
                Widgets.Label(new Rect(row.x + 4f, row.y + 4f, row.width - 80f, 24f), t.LabelCap);

                if (outpost.CanInteract(localCaravan))
                {
                    Rect takeBtn = new Rect(row.xMax - 75f, row.y + 2f, 70f, 26f);
                    if (Widgets.ButtonText(takeBtn, "StrategicOutpost_TakeItemBtn".Translate()))
                    {
                        outpost.WithdrawToCaravan(localCaravan, outpost.RawMaterials, t);
                    }
                }
                ry += 34f;
            }
            Widgets.EndScrollView();

            // Right: Produced Goods Storage
            Widgets.DrawMenuSection(rightCol);
            Rect rightInner = rightCol.ContractedBy(8f);
            Widgets.Label(new Rect(rightInner.x, rightInner.y, rightInner.width, 24f), "StrategicOutpost_ProducedGoodsTitle".Translate(outpost.ProducedGoods.Count));

            if (outpost.CanInteract(localCaravan) && outpost.ProducedGoods.Count > 0)
            {
                Rect takeAllBtn = new Rect(rightInner.xMax - 130f, rightInner.y, 125f, 24f);
                if (Widgets.ButtonText(takeAllBtn, "StrategicOutpost_TakeAllBtn".Translate()))
                {
                    List<Thing> allProd = outpost.ProducedGoods.InnerListForReading.ToList();
                    for (int i = 0; i < allProd.Count; i++)
                    {
                        outpost.WithdrawToCaravan(localCaravan, outpost.ProducedGoods, allProd[i]);
                    }
                }
            }

            Rect rightScroll = new Rect(rightInner.x, rightInner.y + 28f, rightInner.width, rightInner.height - 28f);
            List<Thing> prodList = outpost.ProducedGoods.InnerListForReading.ToList();
            Rect rightView = new Rect(0f, 0f, rightScroll.width - 16f, Mathf.Max(rightScroll.height, (float)prodList.Count * 36f));

            Widgets.BeginScrollView(rightScroll, ref scrollPosProd, rightView);
            float py = 0f;
            for (int i = 0; i < prodList.Count; i++)
            {
                Thing t = prodList[i];
                Rect row = new Rect(0f, py, rightView.width, 32f);
                Widgets.DrawHighlightIfMouseover(row);
                Widgets.Label(new Rect(row.x + 4f, row.y + 4f, row.width - 80f, 24f), t.LabelCap);

                if (outpost.CanInteract(localCaravan))
                {
                    Rect takeBtn = new Rect(row.xMax - 75f, row.y + 2f, 70f, 26f);
                    if (Widgets.ButtonText(takeBtn, "StrategicOutpost_TakeItemBtn".Translate()))
                    {
                        outpost.WithdrawToCaravan(localCaravan, outpost.ProducedGoods, t);
                    }
                }
                py += 34f;
            }
            Widgets.EndScrollView();
        }

        private void DrawEvacuationTab(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(16f);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 32f), "StrategicOutpost_EvacuateTitle".Translate());

            Text.Font = GameFont.Small;
            string evacDesc = "StrategicOutpost_EvacuateWarningFull".Translate();
            Widgets.Label(new Rect(inner.x, inner.y + 40f, inner.width, 100f), evacDesc);

            Rect evacBtn = new Rect(inner.x, inner.y + 160f, 240f, 40f);
            bool canEvac = outpost.CanInteract(localCaravan);

            if (Widgets.ButtonText(evacBtn, "StrategicOutpost_EvacuateButton".Translate(), true, true, canEvac))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "StrategicOutpost_ConfirmEvacuatePrompt".Translate(),
                    delegate
                    {
                        outpost.AbandonAndEvacuate(localCaravan);
                        Close();
                    },
                    true));
            }

            if (!canEvac)
            {
                Widgets.Label(new Rect(inner.x, inner.y + 210f, inner.width, 30f), "StrategicOutpost_EvacuateRequiresCaravan".Translate());
            }
        }

        private void OpenDepositMenu()
        {
            if (!outpost.CanInteract(localCaravan)) return;
            var choices = CaravanInventoryUtility.AllInventoryItems(localCaravan).Where(t => t.def.category == ThingCategory.Item).ToList();
            if (choices.Count == 0)
            {
                Messages.Message("StrategicOutpost_NoSupplies".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            var options = choices.Select(t => new FloatMenuOption(t.LabelCap, () => outpost.DepositFromCaravan(localCaravan, t))).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenColonySelector()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            options.Add(new FloatMenuOption("StrategicOutpost_ClearTargetColony".Translate(), delegate
            {
                outpost.targetColonyTile = -1;
            }));

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s != null && s.Faction == Faction.OfPlayer && s.Spawned && s.Tile.Layer == Find.WorldGrid.Surface)
                {
                    Settlement localS = s;
                    options.Add(new FloatMenuOption(localS.LabelCap.ToString(), delegate
                    {
                        outpost.targetColonyTile = localS.Tile;
                    }));
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenSpecializationSelector()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            var specs = new[]
            {
                StrategicOutpostSpecialization.Mining,
                StrategicOutpostSpecialization.Farming,
                StrategicOutpostSpecialization.Watchtower,
                StrategicOutpostSpecialization.Factory
            };

            foreach (StrategicOutpostSpecialization spec in specs)
            {
                StrategicOutpostSpecialization localSpec = spec;
                string labelKey = "StrategicOutpost_Spec_" + localSpec.ToString();
                options.Add(new FloatMenuOption(labelKey.Translate(), delegate
                {
                    if (outpost.specialization != localSpec)
                    {
                        outpost.specialization = localSpec;
                        outpost.selectedProductDefNames.Clear();
                        outpost.EnsureSelectedProductsInitialized();
                    }
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
