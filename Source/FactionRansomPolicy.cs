using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class FactionRansomOverride : IExposable
    {
        public Faction faction;
        public int chance = 50;

        public FactionRansomOverride() { }

        public FactionRansomOverride(Faction faction, int chance)
        {
            this.faction = faction;
            this.chance = Mathf.Clamp(chance, 0, 100);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref chance, "chance", 50);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                chance = Mathf.Clamp(chance, 0, 100);
            }
        }
    }

    public sealed class Dialog_FactionRansomPolicy : Window
    {
        private Vector2 scroll;
        private string searchText = "";

        public Dialog_FactionRansomPolicy()
        {
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize { get { return new Vector2(720, 720); } }

        public override void DoWindowContents(Rect rect)
        {
            var ransoms = Current.Game != null ? Current.Game.GetComponent<FactionRansoms>() : null;
            if (ransoms == null)
            {
                Close();
                return;
            }

            var prevFont = Text.Font;
            var prevAnchor = Text.Anchor;
            var prevColor = GUI.color;
            var prevWordWrap = Text.WordWrap;

            try
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                Text.WordWrap = true;

                // Title
                Widgets.Label(new Rect(0, 0, rect.width, 32), "TMM.RansomPolicy.Title".Translate());

                Text.Font = GameFont.Small;

                // Help/Note box
                string note = "TMM.RansomPolicy.Note".Translate();
                float noteH = Text.CalcHeight(note, rect.width - 24);
                var noteBox = new Rect(0, 36, rect.width, noteH + 12);
                Widgets.DrawBoxSolid(noteBox, new Color(0.2f, 0.25f, 0.32f, 0.4f));
                GUI.color = new Color(0.85f, 0.9f, 0.95f);
                Widgets.Label(new Rect(10, 42, rect.width - 20, noteH), note);
                GUI.color = Color.white;

                float y = 36 + noteBox.height + 12;

                // Section 1: Default chance control
                var defaultBox = new Rect(0, y, rect.width, 42);
                Widgets.DrawBoxSolid(defaultBox, new Color(0.18f, 0.2f, 0.24f, 0.5f));

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;
                string defText = "TMM.RansomPolicy.DefaultChance".Translate(ransoms.defaultChance);
                Widgets.Label(new Rect(12, y + 4, Math.Max(80,rect.width-338), 34), defText);

                float btnX = rect.width - 320;
                if (Widgets.ButtonText(new Rect(btnX, y + 5, 55, 32), "−5%"))
                {
                    ransoms.SetDefaultChance(ransoms.defaultChance - 5);
                }
                if (Widgets.ButtonText(new Rect(btnX + 60, y + 5, 55, 32), "+5%"))
                {
                    ransoms.SetDefaultChance(ransoms.defaultChance + 5);
                }
                if (Widgets.ButtonText(new Rect(btnX + 125, y + 5, 55, 32), "0%"))
                {
                    ransoms.SetDefaultChance(0);
                }
                if (Widgets.ButtonText(new Rect(btnX + 185, y + 5, 55, 32), "50%"))
                {
                    ransoms.SetDefaultChance(50);
                }
                if (Widgets.ButtonText(new Rect(btnX + 245, y + 5, 55, 32), "100%"))
                {
                    ransoms.SetDefaultChance(100);
                }

                y += 50;

                // Section 2: Search bar
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(0, y + 2, 120, 30), "TMM.RansomPolicy.SearchLabel".Translate());
                searchText = Widgets.TextField(new Rect(125, y + 2, rect.width - 130, 30), searchText);
                y += 38;

                // Header for list
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(new Rect(0, y, rect.width, 24), "TMM.RansomPolicy.FactionsListHeader".Translate());
                y += 28;

                // Section 3: Factions Scroll View
                float listTop = y;
                float listH = rect.height - listTop - 45;
                var viewRect = new Rect(0, listTop, rect.width, listH);

                var factions = (Find.FactionManager != null ? Find.FactionManager.AllFactionsListForReading : null) ?? new List<Faction>();
                var filteredFactions = factions
                    .Where(f => f != null && !f.IsPlayer && f != Faction.OfPlayer &&
                                !(f.def != null && f.def.hidden && !f.HasName))
                    .Where(f => string.IsNullOrEmpty(searchText) || f.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(f => f.Name)
                    .ToList();

                float itemH = 54f;
                float totalContentH = Math.Max(viewRect.height, filteredFactions.Count * itemH + 20f);
                var innerRect = new Rect(0, 0, viewRect.width - 20, totalContentH);

                Widgets.BeginScrollView(viewRect, ref scroll, innerRect);
                try
                {
                    if (filteredFactions.Count == 0)
                    {
                        Text.Anchor = TextAnchor.MiddleCenter;
                        GUI.color = new Color(0.7f, 0.7f, 0.7f);
                        Widgets.Label(new Rect(0, 20, innerRect.width, 30), "TMM.RansomPolicy.NoFactionsFound".Translate());
                        GUI.color = Color.white;
                    }
                    else
                    {
                        float rowY = 0f;
                        for (int i = 0; i < filteredFactions.Count; i++)
                        {
                            var faction = filteredFactions[i];
                            var rowRect = new Rect(0, rowY, innerRect.width, itemH - 2);

                            if (i % 2 == 1)
                            {
                                Widgets.DrawBoxSolid(rowRect, new Color(0.15f, 0.17f, 0.2f, 0.35f));
                            }
                            if (Mouse.IsOver(rowRect))
                            {
                                Widgets.DrawHighlight(rowRect);
                            }

                            // Line 1: Faction color box & full faction name (width: innerRect.width - 34)
                            var colorBox = new Rect(4, rowY + 5, 16, 16);
                            Widgets.DrawBoxSolid(colorBox, faction.Color);

                            string nameText = faction.Name;
                            if (faction.defeated)
                            {
                                nameText += " (" + "TMM.RansomPolicy.Defeated".Translate() + ")";
                            }

                            Text.Anchor = TextAnchor.MiddleLeft;
                            Text.Font = GameFont.Small;
                            var nameRect = new Rect(26, rowY + 2, innerRect.width - 32, 22);
                            Text.WordWrap=false;
                            Widgets.Label(nameRect, nameText.Truncate(nameRect.width));
                            Text.WordWrap=true;
                            TooltipHandler.TipRegion(nameRect, nameText);

                            // Line 2: Chance status on the left, action buttons on the right
                            var ov = ransoms.overrides.FirstOrDefault(o => o != null && o.faction == faction);
                            bool hasOverride = ov != null;
                            int currentChance = hasOverride ? ov.chance : ransoms.defaultChance;

                            string chanceStr = hasOverride
                                ? "TMM.RansomPolicy.CurrentOverride".Translate(currentChance).ToString()
                                : "TMM.RansomPolicy.CurrentDefault".Translate(currentChance).ToString();

                            var chanceRect = new Rect(26, rowY + 26, Math.Max(80,innerRect.width-274), 24);
                            GUI.color = hasOverride ? new Color(1f, 0.85f, 0.45f) : Color.white;
                            Text.WordWrap=false;
                            Widgets.Label(chanceRect, chanceStr.Truncate(chanceRect.width));
                            TooltipHandler.TipRegion(chanceRect,chanceStr);
                            Text.WordWrap=true;
                            GUI.color = Color.white;

                            // Buttons on Line 2 starting at innerRect.width - 240
                            float btnRightX = innerRect.width - 240;
                            if (Widgets.ButtonText(new Rect(btnRightX, rowY + 25, 45, 24), "−5%"))
                            {
                                ransoms.SetOverride(faction, currentChance - 5);
                            }
                            if (Widgets.ButtonText(new Rect(btnRightX + 50, rowY + 25, 45, 24), "+5%"))
                            {
                                ransoms.SetOverride(faction, currentChance + 5);
                            }

                            if (Widgets.ButtonText(new Rect(btnRightX + 100, rowY + 25, 45, 24), "..."))
                            {
                                var options = new List<FloatMenuOption>
                                {
                                    new FloatMenuOption("TMM.RansomPolicy.PresetAlwaysRefuse".Translate(), () => ransoms.SetOverride(faction, 0)),
                                    new FloatMenuOption("25%", () => ransoms.SetOverride(faction, 25)),
                                    new FloatMenuOption("50%", () => ransoms.SetOverride(faction, 50)),
                                    new FloatMenuOption("75%", () => ransoms.SetOverride(faction, 75)),
                                    new FloatMenuOption("TMM.RansomPolicy.PresetAlwaysAccept".Translate(), () => ransoms.SetOverride(faction, 100))
                                };
                                Find.WindowStack.Add(new FloatMenu(options));
                            }

                            // Reset button
                            if (hasOverride)
                            {
                                if (Widgets.ButtonText(new Rect(btnRightX + 150, rowY + 25, 85, 24), "TMM.RansomPolicy.ResetOverride".Translate()))
                                {
                                    ransoms.RemoveOverride(faction);
                                }
                            }

                            rowY += itemH;
                        }
                    }
                }
                finally
                {
                    Widgets.EndScrollView();
                }
            }
            finally
            {
                Text.Font = prevFont;
                Text.Anchor = prevAnchor;
                GUI.color = prevColor;
                Text.WordWrap = prevWordWrap;
            }
        }
    }
}
