using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class Dialog_RestaurantMenu : Window
    {
        private readonly Building_ComplexDesk desk;
        private Vector2 scroll;
        private string searchFilter = "";
        private List<ThingDef> preparedDefs;

        public Dialog_RestaurantMenu(Building_ComplexDesk desk)
        {
            this.desk = desk;
            if (this.desk != null && this.desk.restaurantMenu == null)
            {
                this.desk.restaurantMenu = new List<ThingDef>();
            }
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize { get { return new Vector2(580, 640); } }

        private List<ThingDef> PreparedDefs
        {
            get
            {
                if (preparedDefs == null)
                {
                    preparedDefs = DefDatabase<ThingDef>.AllDefsListForReading
                        .Where(RestaurantMeals.Prepared)
                        .OrderBy(d => d.label)
                        .ToList();
                }
                return preparedDefs;
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            if (desk == null || !desk.Spawned || !desk.Active)
            {
                Close();
                return;
            }

            var previousFont = Text.Font;
            var previousAnchor = Text.Anchor;
            var previousColor = GUI.color;
            var previousWordWrap = Text.WordWrap;

            try
            {
                // Explicitly normalize GUI state on window entry
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                Text.WordWrap = true;

                // Header
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0, 0, rect.width, 36), "TMM.Restaurant.MenuTitle".Translate());
                Text.Font = GameFont.Small;

                // Description help
                string helpText = "TMM.Restaurant.MenuDialogHelp".Translate();
                float helpH = Text.CalcHeight(helpText, rect.width);
                Widgets.Label(new Rect(0, 40, rect.width, helpH), helpText);
                float y = 40 + helpH + 10;

                // Search box
                float searchLabelW = 70;
                Widgets.Label(new Rect(0, y + 2, searchLabelW, 28), "TMM.Restaurant.Search".Translate());
                searchFilter = Widgets.TextField(new Rect(searchLabelW, y, 220, 28), searchFilter);

                // Quick buttons: Select All / Clear All
                if (Widgets.ButtonText(new Rect(rect.width - 240, y, 115, 28), "TMM.Restaurant.SelectAll".Translate()))
                {
                    foreach (var def in FilteredDefs())
                    {
                        if (!desk.restaurantMenu.Contains(def))
                            desk.restaurantMenu.Add(def);
                    }
                    desk.restaurantMenuConfigured = true;
                    SyncLegacyMeal();
                }
                if (Widgets.ButtonText(new Rect(rect.width - 120, y, 115, 28), "TMM.Restaurant.ClearAll".Translate()))
                {
                    desk.restaurantMenu.Clear();
                    desk.restaurantMenuConfigured = true;
                    SyncLegacyMeal();
                }
                y += 36;

                // Menu empty alert
                if (desk.restaurantMenu.Count == 0)
                {
                    GUI.color = Color.yellow;
                    Widgets.Label(new Rect(0, y, rect.width, 24), "TMM.Restaurant.MenuEmptyWarning".Translate());
                    GUI.color = Color.white;
                    y += 28;
                }
                else
                {
                    Widgets.Label(new Rect(0, y, rect.width, 24), "TMM.Restaurant.SelectedCount".Translate(desk.restaurantMenu.Count));
                    y += 28;
                }

                // Scrollable Dish List
                var filtered = FilteredDefs();
                float rowHeight = 36;
                float viewHeight = filtered.Count * rowHeight;
                Rect outRect = new Rect(0, y, rect.width, rect.height - y - 48);
                Rect viewRect = new Rect(0, 0, rect.width - 20, viewHeight);

                Widgets.BeginScrollView(outRect, ref scroll, viewRect);
                try
                {
                    float rowY = 0;
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        var def = filtered[i];
                        Rect rowRect = new Rect(0, rowY, viewRect.width, rowHeight);
                        if (i % 2 == 1)
                        {
                            Widgets.DrawLightHighlight(rowRect);
                        }

                        // Dish icon
                        if (def.uiIcon != null)
                        {
                            GUI.DrawTexture(new Rect(4, rowY + 2, 32, 32), def.uiIcon);
                        }

                        // Checkbox
                        bool isSelected = desk.restaurantMenu.Contains(def);
                        bool wasSelected = isSelected;
                        Widgets.Checkbox(new Vector2(rowRect.width - 32, rowY + 6), ref isSelected, 24);
                        if (isSelected != wasSelected)
                        {
                            if (isSelected) desk.restaurantMenu.Add(def);
                            else desk.restaurantMenu.Remove(def);
                            desk.restaurantMenuConfigured = true;
                            SyncLegacyMeal();
                        }

                        // Dish title - clean without raw English enum string
                        Text.WordWrap=false;Widgets.Label(new Rect(42,rowY+6,rowRect.width-80,24),def.LabelCap.ToString().Truncate(rowRect.width-80));Text.WordWrap=true;

                        // Tooltip clearly distinguishing physical stock from individual guest availability
                        int stock = StockCount(def);
                        string tip;
                        if (stock > 0)
                        {
                            tip = "TMM.Restaurant.StockTooltip".Translate(def.LabelCap+"\n\n"+def.description, stock);
                        }
                        else
                        {
                            tip = "TMM.Restaurant.NoStockTooltip".Translate(def.LabelCap+"\n\n"+def.description);
                        }
                        TooltipHandler.TipRegion(rowRect, tip);

                        rowY += rowHeight;
                    }
                }
                finally
                {
                    Widgets.EndScrollView();
                }
            }
            finally
            {
                Text.Font = previousFont;
                Text.Anchor = previousAnchor;
                GUI.color = previousColor;
                Text.WordWrap = previousWordWrap;
            }
        }

        private List<ThingDef> FilteredDefs()
        {
            var all = PreparedDefs;
            if (string.IsNullOrEmpty(searchFilter)) return all;
            string filter = searchFilter.Trim().ToLowerInvariant();
            return all.Where(d => d.label.ToLowerInvariant().Contains(filter) || d.defName.ToLowerInvariant().Contains(filter)).ToList();
        }

        private int StockCount(ThingDef def)
        {
            if (desk == null || desk.Map == null || def == null) return 0;
            var counter = desk.Map.listerBuildings.allBuildingsColonist.OfType<Building_ServiceCounter>().FirstOrDefault(c => c.Complex == desk);
            if (counter == null)
            {
                return desk.Map.listerThings.ThingsOfDef(def)
                    .Where(t => t.Spawned && RestaurantMeals.FridgeAt(desk.Map, t.Position))
                    .Sum(t => t.stackCount);
            }
            return desk.Map.listerThings.ThingsOfDef(def)
                .Where(t => t.Spawned && RestaurantMeals.Stock(counter, t))
                .Sum(t => t.stackCount);
        }

        private void SyncLegacyMeal()
        {
            if (desk.restaurantMenu != null && desk.restaurantMenu.Count > 0)
            {
                desk.restaurantMeal = desk.restaurantMenu[0];
            }
            else
            {
                desk.restaurantMeal = null;
            }
        }
    }
}
