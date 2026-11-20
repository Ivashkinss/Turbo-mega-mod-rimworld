using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class Dialog_OutpostCaravan : Window
    {
        private StrategicOutpost outpost;
        private Map homeMap;
        private List<Pawn> availableColonists = new List<Pawn>();
        private HashSet<Pawn> selectedPawns = new HashSet<Pawn>();
        private HashSet<Pawn> stayPawns = new HashSet<Pawn>();
        private int silverAmount = 0;
        private int maxSilver = 0;
        private Vector2 scrollPosition = Vector2.zero;

        // Cached feasibility & path calculation (recomputed ONLY when selection or cargo changes)
        private bool cacheDirty = true;
        private float cachedDaysOneWay = 0f;
        private float cachedDaysRoundTrip = 0f;
        private float cachedFoodDays = 0f;
        private bool cachedCanSend = false;
        private string cachedCantSendReason = null;

        public override Vector2 InitialSize
        {
            get { return new Vector2(620f, 620f); }
        }

        public Dialog_OutpostCaravan(StrategicOutpost outpost, Map homeMap, bool focusOnSilver = false)
        {
            this.outpost = outpost;
            this.homeMap = homeMap;
            this.doCloseButton = false;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;

            if (homeMap != null && homeMap.mapPawns != null)
            {
                availableColonists = homeMap.mapPawns.FreeColonistsSpawned
                    .Where(p => StrategicOutpostCaravanUtility.IsSuitableColonist(p) && !p.Drafted && !p.InMentalState)
                    .OrderBy(p => p.LabelShort)
                    .ToList();
            }

            // Use the exact same formable silver pool that StartFormingCaravan loads
            maxSilver = homeMap != null ? OutpostCaravanLogistics.CountFormableSilver(homeMap) : 0;

            if (focusOnSilver)
            {
                silverAmount = Mathf.Min(200, maxSilver);
                if (availableColonists.Count > 0)
                {
                    selectedPawns.Add(availableColonists[0]);
                }
            }
            else
            {
                if (availableColonists.Count > 0)
                {
                    selectedPawns.Add(availableColonists[0]);
                    stayPawns.Add(availableColonists[0]);
                }
            }

            cacheDirty = true;
        }

        private void RefreshFeasibilityCache()
        {
            cacheDirty = false;
            List<Pawn> selectedList = selectedPawns.ToList();
            List<Pawn> stayList = stayPawns.ToList();
            OutpostCaravanLogistics.CheckCaravanFeasibility(
                outpost, homeMap, selectedList, stayList, silverAmount,
                out cachedDaysOneWay, out cachedDaysRoundTrip, out cachedFoodDays,
                out cachedCanSend, out cachedCantSendReason);
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont origFont = Text.Font;
            TextAnchor origAnchor = Text.Anchor;
            Color origColor = GUI.color;
            bool origWrap = Text.WordWrap;

            try
            {
                if (outpost == null || !outpost.Spawned || homeMap == null)
                {
                    Close();
                    return;
                }

                float curY = 0f;

                // 1. Header Title
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, curY, inRect.width - 30f, 32f), "TMM_OutpostCaravan_Title".Translate(outpost.LabelCap));
                Text.Font = GameFont.Small;
                curY += 38f;

                // 2. Colonists Section
                Widgets.Label(new Rect(0f, curY, inRect.width, 22f), "TMM_OutpostCaravan_SelectColonists".Translate());
                curY += 24f;

                float colonistListHeight = 220f;
                Rect outRect = new Rect(0f, curY, inRect.width, colonistListHeight);
                float viewHeight = Mathf.Max(colonistListHeight, (float)availableColonists.Count * 34f + 10f);
                Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, viewHeight);

                Widgets.DrawMenuSection(outRect);
                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
                float itemY = 4f;

                for (int i = 0; i < availableColonists.Count; i++)
                {
                    Pawn p = availableColonists[i];
                    Rect rowRect = new Rect(4f, itemY, viewRect.width - 8f, 30f);

                    if (i % 2 == 1)
                    {
                        Widgets.DrawAltRect(rowRect);
                    }

                    bool inCaravan = selectedPawns.Contains(p);
                    Rect chkRect = new Rect(rowRect.x + 4f, rowRect.y + 3f, 24f, 24f);
                    bool newInCaravan = inCaravan;
                    Widgets.Checkbox(chkRect.position, ref newInCaravan);
                    if (newInCaravan != inCaravan)
                    {
                        if (newInCaravan)
                        {
                            selectedPawns.Add(p);
                        }
                        else
                        {
                            selectedPawns.Remove(p);
                            stayPawns.Remove(p);
                        }
                        cacheDirty = true;
                    }

                    Rect nameRect = new Rect(chkRect.xMax + 8f, rowRect.y + 4f, 250f, 22f);
                    string colonistTitle = p.story != null && !p.story.TitleShort.NullOrEmpty() ? (" (" + p.story.TitleShort + ")") : "";
                    Widgets.Label(nameRect, p.LabelShort + colonistTitle);

                    Rect stayRect = new Rect(rowRect.xMax - 180f, rowRect.y + 4f, 175f, 24f);
                    bool stay = stayPawns.Contains(p);
                    bool newStay = stay;
                    if (newInCaravan)
                    {
                        Widgets.CheckboxLabeled(stayRect, "TMM_OutpostCaravan_StayAsWorker".Translate(), ref newStay);
                        if (newStay != stay)
                        {
                            if (newStay) stayPawns.Add(p);
                            else stayPawns.Remove(p);
                            cacheDirty = true;
                        }
                    }
                    else
                    {
                        GUI.color = Color.gray;
                        bool dummy = false;
                        Widgets.CheckboxLabeled(stayRect, "TMM_OutpostCaravan_StayAsWorker".Translate(), ref dummy, true);
                        GUI.color = Color.white;
                    }

                    itemY += 34f;
                }

                if (availableColonists.Count == 0)
                {
                    Widgets.Label(new Rect(10f, 10f, 300f, 25f), "StrategicOutpost_NoEligibleColonists".Translate());
                }

                Widgets.EndScrollView();
                curY += colonistListHeight + 12f;

                // 3. Silver Slider Section
                Widgets.Label(new Rect(0f, curY, inRect.width, 22f), "TMM_OutpostCaravan_SilverToSend".Translate(silverAmount, maxSilver));
                curY += 24f;

                Rect sliderRect = new Rect(0f, curY + 2f, inRect.width - 240f, 26f);
                int newSilver = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, silverAmount, 0, maxSilver, false, null, null, null, 10));

                float qBtnX = sliderRect.xMax + 8f;
                float qBtnW = 50f;
                if (Widgets.ButtonText(new Rect(qBtnX, curY, qBtnW, 26f), "0")) newSilver = 0;
                qBtnX += qBtnW + 4f;
                if (Widgets.ButtonText(new Rect(qBtnX, curY, qBtnW, 26f), "+100")) newSilver = Mathf.Min(maxSilver, newSilver + 100);
                qBtnX += qBtnW + 4f;
                if (Widgets.ButtonText(new Rect(qBtnX, curY, qBtnW, 26f), "+500")) newSilver = Mathf.Min(maxSilver, newSilver + 500);
                qBtnX += qBtnW + 4f;
                if (Widgets.ButtonText(new Rect(qBtnX, curY, qBtnW, 26f), "Max")) newSilver = maxSilver;

                if (newSilver != silverAmount)
                {
                    silverAmount = newSilver;
                    cacheDirty = true;
                }

                curY += 34f;

                // 4. Cached Logistics & Food Assessment (zero pathfinding on idle Layout/Repaint)
                if (cacheDirty)
                {
                    RefreshFeasibilityCache();
                }

                Rect infoRect = new Rect(0f, curY, inRect.width, 74f);
                Widgets.DrawMenuSection(infoRect);
                Rect innerInfo = infoRect.ContractedBy(6f);

                string tripStr = "TMM_OutpostCaravan_TripEstimate".Translate(cachedDaysRoundTrip.ToString("F1"), cachedDaysOneWay.ToString("F1"));
                Widgets.Label(new Rect(innerInfo.x, innerInfo.y + 4f, innerInfo.width, 22f), tripStr);

                string foodStr = "TMM_OutpostCaravan_FoodEstimate".Translate(cachedFoodDays.ToString("F1"), cachedDaysRoundTrip.ToString("F1"));
                if (cachedFoodDays < cachedDaysRoundTrip && selectedPawns.Count > 0)
                {
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                }
                else
                {
                    GUI.color = Color.green;
                }
                Widgets.Label(new Rect(innerInfo.x, innerInfo.y + 30f, innerInfo.width, 22f), foodStr);
                GUI.color = Color.white;

                curY += 86f;

                // 5. Bottom Buttons
                float btnHeight = 36f;
                Rect btnCancel = new Rect(0f, inRect.height - btnHeight, 130f, btnHeight);
                if (Widgets.ButtonText(btnCancel, "Cancel".Translate()))
                {
                    Close();
                }

                Rect btnSend = new Rect(inRect.width - 180f, inRect.height - btnHeight, 180f, btnHeight);
                if (Widgets.ButtonText(btnSend, "TMM_OutpostCaravan_SendBtn".Translate(), true, true, cachedCanSend))
                {
                    string failReason;
                    if (OutpostCaravanLogistics.StartCaravan(outpost, homeMap, selectedPawns.ToList(), stayPawns.ToList(), silverAmount, out failReason))
                    {
                        Close();
                    }
                    else if (!failReason.NullOrEmpty())
                    {
                        cacheDirty = true;
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }
                }

                if (!cachedCanSend && !cachedCantSendReason.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(btnSend, cachedCantSendReason);
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
    }
}
