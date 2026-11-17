using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    // Original compact inspector, using game data and vanilla UI assets.
    public sealed class PawnOverviewSettings : ModSettings
    {
        public bool enabled=true;
        public override void ExposeData(){Scribe_Values.Look(ref enabled,"compactPawnOverview",true);}
    }
    [StaticConstructorOnStartup]
    public static class PawnOverview
    {
        public const float Height=220;
        private static Vector2 needScroll,skillScroll;
        private static Pawn scrollPawn;
        public static PawnOverviewSettings Settings;
        public static bool ExternalHUD {get{return ModsConfig.IsActive("jaxe.rimhud");}}
        public static Pawn Selected {get{
            // Find.Selector casts the active UI root to UIRoot_Play. World selection also draws
            // inspect panes, while UIRoot_Entry is still active and no map selector exists.
            if(Current.ProgramState!=ProgramState.Playing||!(Find.UIRoot is UIRoot_Play)||Find.CurrentMap==null)return null;
            return Find.Selector==null?null:Find.Selector.SingleSelectedThing as Pawn;
        }}
        public static bool Active {get{var p=Selected;return Settings!=null&&Settings.enabled&&!ExternalHUD&&p!=null&&!p.Destroyed;}}
        public static List<Need> Needs(Pawn pawn){return pawn.needs==null?new List<Need>():pawn.needs.AllNeeds.Where(n=>n is Need_Mood||n.ShowOnNeedList).OrderBy(n=>n is Need_Mood?0:1).ToList();}
        static PawnOverview()
        {
            var h=new Harmony("turbomegamod.pawn.overview");
            h.Patch(AccessTools.Method(typeof(MainTabWindow_Inspect),"DoPaneContents"),prefix:new HarmonyMethod(typeof(PawnOverview),"Contents"));
            h.Patch(AccessTools.Method(typeof(MainTabWindow_Inspect),"DoInspectPaneButtons"),postfix:new HarmonyMethod(typeof(PawnOverview),"Buttons"));
            h.Patch(AccessTools.Method(typeof(InspectPaneUtility),"PaneSizeFor"),postfix:new HarmonyMethod(typeof(PawnOverview),"Size"));
            h.Patch(AccessTools.PropertyGetter(typeof(MainTabWindow_Inspect),"PaneTopY"),postfix:new HarmonyMethod(typeof(PawnOverview),"Top"));
            h.Patch(AccessTools.Method(typeof(InspectPaneUtility),"InspectPaneOnGUI"),postfix:new HarmonyMethod(typeof(PawnOverview),"Recent"));
        }
        public static float HeightFor(Pawn pawn){return pawn.skills!=null?Height:Math.Max(165,Math.Min(Height,92+Math.Min(6,Needs(pawn).Count)*14));}
        private static void Size(IInspectPane pane,ref Vector2 __result){if(pane is MainTabWindow_Inspect&&Active){__result.x=Math.Max(__result.x,Math.Min(460,UI.screenWidth-40));__result.y=HeightFor(Selected);}}
        private static void Top(ref float __result){if(Active)__result=UI.screenHeight-35-HeightFor(Selected);}
        private static void Recent(IInspectPane pane){if(pane is MainTabWindow_Inspect&&Active)pane.RecentHeight=HeightFor(Selected);}
        private static bool Contents(Rect rect){if(!Active)return true;Draw(rect,Selected);return false;}
        private static Rect NextIcon(Rect canvas,ref float used){used+=21;return new Rect(canvas.width-used,0,19,19);}
        private static void Buttons(Rect __0,ref float __1)
        {
            if(!Active)return;var pawn=Selected;var old=GUI.color;
            try{
                if(pawn.Faction==Faction.OfPlayer&&pawn.playerSettings!=null){
                    if(pawn.RaceProps.IsFlesh){var r=NextIcon(__0,ref __1);MedicalCareUtility.MedicalCareSelectButton(r,pawn);TooltipHandler.TipRegion(r,"TMM.HUD.Medical".Translate(pawn.playerSettings.medCare.GetLabel()));}
                    if(pawn.IsColonist){
                        var r=NextIcon(__0,ref __1);GUI.color=pawn.playerSettings.selfTend?Color.white:Color.gray;
                        if(Widgets.ButtonText(r,"+",false)&&!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))pawn.playerSettings.selfTend=!pawn.playerSettings.selfTend;
                        GUI.color=old;TooltipHandler.TipRegion(r,"TMM.HUD.SelfTend".Translate(pawn.playerSettings.selfTend?"On".Translate():"Off".Translate()));
                    }
                }
                if(pawn.Faction!=null)FactionUIUtility.DrawFactionIconWithTooltip(NextIcon(__0,ref __1),pawn.Faction);
                if(ModsConfig.IdeologyActive&&pawn.Ideo!=null){var r=NextIcon(__0,ref __1);IdeoUIUtility.DoIdeoIcon(r,pawn.Ideo,false,()=>IdeoUIUtility.OpenIdeoInfo(pawn.Ideo));}
                if(ModsConfig.BiotechActive&&pawn.genes!=null&&pawn.genes.Xenotype!=null){var r=NextIcon(__0,ref __1);if(Widgets.ButtonImage(r,pawn.genes.XenotypeIcon))InspectPaneUtility.OpenTab(typeof(ITab_Genes));TooltipHandler.TipRegion(r,pawn.genes.XenotypeLabelCap);}
            }finally{GUI.color=old;}
        }
        private static void Bar(Rect rect,string name,float value,string tip,bool warning=false)
        {
            Color color=warning||value<0.2f?new Color(0.8f,0.25f,0.18f):new Color(0.27f,0.55f,0.2f);
            Widgets.DrawBoxSolid(rect,new Color(0.13f,0.14f,0.15f));Widgets.DrawBoxSolid(new Rect(rect.x,rect.y,rect.width*Mathf.Clamp01(value),rect.height),color);
            Text.Anchor=TextAnchor.MiddleLeft;Widgets.Label(new Rect(rect.x+3,rect.y,rect.width-36,rect.height),name);
            Text.Anchor=TextAnchor.MiddleRight;Widgets.Label(new Rect(rect.x,rect.y,rect.width-2,rect.height),value.ToStringPercent());Text.Anchor=TextAnchor.UpperLeft;
            if(!string.IsNullOrEmpty(tip))TooltipHandler.TipRegion(rect,tip);
        }
        private static void Marker(Rect rect,string text,Action click)
        {
            Widgets.DrawBoxSolid(rect,new Color(0.16f,0.17f,0.18f));Widgets.Label(rect,text.Truncate(rect.width));TooltipHandler.TipRegion(rect,text);
            if(click!=null){Widgets.DrawHighlightIfMouseover(rect);if(Widgets.ButtonInvisible(rect))click();}
        }
        private static void Policies(Rect rect,Pawn p)
        {
            bool own=p.Faction==Faction.OfPlayer;float width=(rect.width-3)/2;
            var food=p.foodRestriction==null?null:p.foodRestriction.CurrentFoodPolicy;
            var apparel=p.outfits==null?null:p.outfits.CurrentApparelPolicy;
            var area=p.playerSettings==null?null:p.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
            string no="—",zone=area==null?"TMM.HUD.Unrestricted".Translate().ToString():area.Label;
            if(apparel!=null)Marker(new Rect(rect.x,rect.y,width,13),"TMM.HUD.Apparel".Translate(apparel.label),own?(Action)(()=>Find.WindowStack.Add(new FloatMenu(Current.Game.outfitDatabase.AllOutfits.Select(a=>new FloatMenuOption(a.label,()=>p.outfits.CurrentApparelPolicy=a)).ToList()))):null);
            if(food!=null)Marker(new Rect(rect.x+width+3,rect.y,width,13),"TMM.HUD.FoodPolicy".Translate(food.label),own?(Action)(()=>Find.WindowStack.Add(new FloatMenu(Current.Game.foodRestrictionDatabase.AllFoodRestrictions.Select(f=>new FloatMenuOption(f.label,()=>p.foodRestriction.CurrentFoodPolicy=f)).ToList()))):null);
            Marker(new Rect(rect.x,rect.y+14,width,13),"TMM.HUD.Schedule".Translate(p.timetable==null?no:p.timetable.CurrentAssignment.LabelCap.ToString()),own&&p.timetable!=null?(Action)(()=>Find.MainTabsRoot.SetCurrentTab(DefDatabase<MainButtonDef>.GetNamed("Schedule"))):null);
            Marker(new Rect(rect.x+width+3,rect.y+14,width,13),"TMM.HUD.Area".Translate(zone),own&&p.playerSettings!=null&&p.playerSettings.SupportsAllowedAreas?(Action)(()=>{
                var options=new List<FloatMenuOption>{new FloatMenuOption("TMM.HUD.Unrestricted".Translate(),()=>p.playerSettings.AreaRestrictionInPawnCurrentMap=null)};
                if(p.Map!=null)foreach(var a in p.Map.areaManager.AllAreas.OfType<Area_Allowed>()){var choice=a;options.Add(new FloatMenuOption(choice.Label,()=>p.playerSettings.AreaRestrictionInPawnCurrentMap=choice));}
                Find.WindowStack.Add(new FloatMenu(options));
            }):null);
        }
        public static void Draw(Rect rect,Pawn pawn)
        {
            if(scrollPawn!=pawn){needScroll=skillScroll=Vector2.zero;scrollPawn=pawn;}
            var font=Text.Font;var anchor=Text.Anchor;bool wrap=Text.WordWrap;var color=GUI.color;
            try{
                Text.Font=GameFont.Tiny;Text.WordWrap=false;Text.Anchor=TextAnchor.UpperLeft;GUI.color=Color.white;
                float half=(rect.width-10)/2,left=rect.y,right=rect.y;
                string title=pawn.story==null?pawn.KindLabel:pawn.story.TitleCap;
                Widgets.Label(new Rect(rect.x,left,half,13),title);TooltipHandler.TipRegion(new Rect(rect.x,left,half,13),title);left+=13;
                string bio="TMM.HUD.Bio".Translate(pawn.GetGenderLabel(),pawn.ageTracker.AgeNumberString);
                Widgets.Label(new Rect(rect.x,left,half,13),bio);TooltipHandler.TipRegion(new Rect(rect.x,left,half,13),pawn.ageTracker.AgeTooltipString);left+=14;
                if(pawn.health!=null){
                    bool bleeding=pawn.health.hediffSet.BleedRateTotal>0;
                    Bar(new Rect(rect.x,left,half,13),"TMM.HUD.Health".Translate(),pawn.health.summaryHealth.SummaryHealthPercent,bleeding?"TMM.HUD.Bleeding".Translate():"TMM.HUD.HealthTip".Translate(),bleeding);left+=14;
                    Widgets.Label(new Rect(rect.x,left,half,12),HealthUtility.GetGeneralConditionLabel(pawn,false));left+=12;
                }
                var needs=Needs(pawn);float needed=needs.Count*14+(pawn.needs!=null&&pawn.needs.mood!=null?12:0);
                var bounds=new Rect(rect.x,left,half,Math.Max(20,rect.yMax-left-16));
                var view=new Rect(0,0,half-(needed>bounds.height?18:0),Math.Max(bounds.height,needed));
                Widgets.BeginScrollView(bounds,ref needScroll,view);
                try{float y=0;foreach(var need in needs){
                    Bar(new Rect(0,y,view.width,13),need.LabelCap,need.CurLevelPercentage,need.GetTipString());
                    if(need is Need_Mood&&pawn.mindState!=null){var b=pawn.mindState.mentalBreaker;if(b!=null)foreach(float threshold in new float[]{b.BreakThresholdMinor,b.BreakThresholdMajor,b.BreakThresholdExtreme})Widgets.DrawBoxSolid(new Rect(view.width*Mathf.Clamp01(threshold),y,1,13),Color.black);}
                    y+=14;if(need is Need_Mood){Widgets.Label(new Rect(0,y,view.width,12),pawn.needs.mood.MoodString);y+=12;}
                }}finally{Widgets.EndScrollView();}
                string activity=pawn.CurJob==null?"TMM.HUD.Idle".Translate().ToString():pawn.GetJobReport();
                var jobRect=new Rect(rect.x,rect.yMax-15,half,15);Widgets.Label(jobRect,activity);TooltipHandler.TipRegion(jobRect,activity);
                if(pawn.skills!=null){
                    Policies(new Rect(rect.x+half+10,right,half,28),pawn);right+=31;
                    var skills=pawn.skills.skills;int rows=(skills.Count+1)/2;
                    var box=new Rect(rect.x+half+10,right,half,Math.Max(20,rect.yMax-right));
                    var content=new Rect(0,0,half-(rows*14>box.height?18:0),Math.Max(box.height,rows*14));
                    Widgets.BeginScrollView(box,ref skillScroll,content);
                    try{float colWidth=(content.width-5)/2;for(int i=0;i<skills.Count;i++){
                        var skill=skills[i];var line=new Rect((i%2)*(colWidth+5),(i/2)*14,colWidth,13);
                        GUI.color=skill.TotallyDisabled?Color.gray:Color.white;
                        Widgets.Label(new Rect(line.x,line.y,line.width-(skill.passion==Passion.None?12:24),line.height),skill.def.LabelCap);
                        if(skill.passion!=Passion.None)GUI.DrawTexture(new Rect(line.xMax-27,line.y+1,12,12),skill.passion==Passion.Major?SkillUI.PassionMajorIcon:SkillUI.PassionMinorIcon);
                        Text.Anchor=TextAnchor.MiddleRight;Widgets.Label(line,skill.TotallyDisabled?"—":skill.Level.ToString());Text.Anchor=TextAnchor.UpperLeft;GUI.color=Color.white;
                        TooltipHandler.TipRegion(line,skill.def.LabelCap+"\n"+skill.def.description+"\n"+skill.LevelDescriptor);
                    }}finally{Widgets.EndScrollView();}
                }else {Text.WordWrap=true;Widgets.Label(new Rect(rect.x+half+10,right,half,rect.yMax-right),pawn.GetInspectString());}
            }finally{Text.Font=font;Text.Anchor=anchor;Text.WordWrap=wrap;GUI.color=color;}
        }
    }
}
