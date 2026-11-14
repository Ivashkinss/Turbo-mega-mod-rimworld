using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class Dialog_ComplexServices : Window
    {
        private readonly Building_ComplexDesk desk;
        private Vector2 scroll;
        public Dialog_ComplexServices(Building_ComplexDesk desk) { this.desk = desk; doCloseX = true; doCloseButton = true; forcePause = true; absorbInputAroundWindow = true; }
        public override Vector2 InitialSize { get { return new Vector2(720, 720); } }
        public override void DoWindowContents(Rect rect)
        {
            if (desk == null || !desk.Spawned) { Close(); return; }
            var inner = new Rect(0, 0, rect.width - 25, 900);
            Widgets.BeginScrollView(new Rect(0, 0, rect.width, rect.height - 45), ref scroll, inner);
            float y = 0;
            string help = "TMM.Complex.ServicesHelp".Translate(); float height = Text.CalcHeight(help, inner.width);
            Widgets.Label(new Rect(0, y, inner.width, height), help); y += height + 18;
            Widgets.Label(new Rect(0, y, 400, 32), "TMM.Complex.RoomRate".Translate(desk.roomRate));
            if (Widgets.ButtonText(new Rect(420, y, 70, 32), "−5")) { desk.roomRate = Math.Max(0, desk.roomRate - 5); desk.ApplyServiceAreas(); }
            if (Widgets.ButtonText(new Rect(500, y, 70, 32), "+5")) { desk.roomRate = Math.Min(10000, desk.roomRate + 5); desk.ApplyServiceAreas(); } y += 45;
            Widgets.CheckboxLabeled(new Rect(0, y, inner.width, 32), "TMM.Restaurant.Open".Translate(), ref desk.restaurantOpen); y += 42;
            if (desk.restaurantMeal == null) desk.restaurantMeal = ThingDefOf.MealSimple;
            if (Widgets.ButtonText(new Rect(0, y, inner.width, 32), "TMM.Restaurant.Meal".Translate() + ": " + desk.restaurantMeal.LabelCap))
                Find.WindowStack.Add(new FloatMenu(DefDatabase<ThingDef>.AllDefsListForReading.Where(RestaurantMeals.Prepared).OrderBy(d => d.label)
                    .Select(d => new FloatMenuOption(d.LabelCap, () => desk.restaurantMeal = d)).ToList())); y += 42;
            if (Widgets.ButtonText(new Rect(0, y, inner.width, 32), "TMM.Restaurant.Price".Translate(desk.restaurantPercent)))
                Find.WindowStack.Add(new FloatMenu(new[] {0, 50, 75, 100, 125, 150, 200}.Select(p => new FloatMenuOption(p + "%", () => desk.restaurantPercent = p)).ToList())); y += 42;
            Widgets.CheckboxLabeled(new Rect(0, y, inner.width, 32), "TMM.Complex.Surgery".Translate(), ref desk.surgeryAllowed); y += 42;
            if (Widgets.ButtonText(new Rect(0, y, inner.width, 32), "TMM.Complex.ClinicSettings".Translate())) {
                var type = AccessTools.TypeByName("Hospital.MainTab.Dialog_MapSettings");
                if (type != null) Find.WindowStack.Add((Window)Activator.CreateInstance(type, new object[] { desk.Map }));
            } y += 50;
            if(Widgets.ButtonText(new Rect(0,y,inner.width,36),"TMM.Buyout.Menu".Translate()))Dialog_WorkBuyout.Show(desk);
            y+=46;
            foreach (var gizmo in CompanyMenus.Gizmos(desk)) {
                var toggle = gizmo as Command_Toggle; var action = gizmo as Command_Action;
                var command = (Command)gizmo;
                if (toggle != null) {
                    bool current = toggle.isActive(), selected = current;
                    Widgets.CheckboxLabeled(new Rect(0, y, inner.width, 36), command.defaultLabel, ref selected);
                    if (selected != current) toggle.toggleAction();
                } else if (Widgets.ButtonText(new Rect(0, y, inner.width, 36), command.defaultLabel) && action != null) action.action();
                TooltipHandler.TipRegion(new Rect(0, y, inner.width, 36), command.defaultDesc); y += 46;
            }
            Widgets.EndScrollView();
        }
    }
}
