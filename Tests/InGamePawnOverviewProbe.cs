using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;

public sealed class PawnOverviewProbe : MapComponent
{
    private bool done,ui;private int checks,renderFrames;private string error;private Pawn human,animal;private MainTabWindow_Inspect pane;
    public PawnOverviewProbe(Map map):base(map){}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Select(Thing t){Find.Selector.ClearSelection();Find.Selector.Select(t);}
    private static bool ExternalMock(string __0,ref bool __result){if(__0=="jaxe.rimhud"){__result=true;return false;}return true;}
    public override void MapComponentUpdate()
    {
        if(done||Find.CurrentMap!=map||!Environment.GetCommandLineArgs().Contains("-tmm-hud-probe"))return;
        try{
            human=map.mapPawns.FreeColonistsSpawned.First();foreach(var p in map.mapPawns.FreeColonistsSpawned){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
            map.fogGrid.ClearAllFog();Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
            pane=(MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
            Check(PawnOverview.Settings!=null&&PawnOverview.Settings.enabled&&!PawnOverview.ExternalHUD,"HUD settings/defaults unavailable");
            Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(MainTabWindow_Inspect),"DoPaneContents")).Owners.Contains("turbomegamod.pawn.overview"),"Native inspector not patched");
            Check(human.needs.mood!=null&&PawnOverview.Needs(human).Contains(human.needs.mood),"Mood omitted because vanilla hides it from ordinary need list");
            Select(human);Check(PawnOverview.Active&&PawnOverview.Selected==human,"Single human selection not active");var size=InspectPaneUtility.PaneSizeFor(pane);Check(size.y==PawnOverview.Height&&size.x>=460,"Expanded native panel size wrong");Check(Math.Abs(pane.PaneTopY-(UI.screenHeight-35-PawnOverview.Height))<1,"Panel anchor wrong");
            Check(human.needs.AllNeeds.Count(n=>n.ShowOnNeedList)>0&&human.skills.skills.Count==12,"Human live data unavailable");
            PawnOverview.Settings.enabled=false;Check(!PawnOverview.Active&&InspectPaneUtility.PaneSizeFor(pane).y==165,"Disabled overview did not return native height");PawnOverview.Settings.enabled=true;
            var building=(Building)GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall,ThingDefOf.Steel),human.Position+IntVec3.North,map);Select(building);Check(!PawnOverview.Active&&InspectPaneUtility.PaneSizeFor(pane).y==165,"Building selection changed native pane");
            Select(human);Find.Selector.Select(map.mapPawns.FreeColonistsSpawned.First(p=>p!=human));Check(!PawnOverview.Active,"Multi selection incorrectly uses single pawn overview");Find.Selector.ClearSelection();Check(!PawnOverview.Active,"Empty selection kept pawn overview");
            animal=PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo);GenSpawn.Spawn(animal,human.Position+IntVec3.East,map);Select(animal);Check(PawnOverview.Active&&animal.skills==null,"Animal fallback unavailable");
            Check(InspectPaneUtility.PaneSizeFor(pane).y==PawnOverview.HeightFor(animal)&&PawnOverview.HeightFor(animal)<PawnOverview.Height,"Animal panel retained oversized human height");
            Select(human);
            var testHarmony=new Harmony("tmm.test.externalhud");var active=AccessTools.Method(typeof(ModsConfig),"IsActive",new Type[]{typeof(string)});var mock=AccessTools.Method(typeof(PawnOverviewProbe),"ExternalMock");
            testHarmony.Patch(active,prefix:new HarmonyMethod(mock));try{Check(PawnOverview.ExternalHUD&&!PawnOverview.Active&&InspectPaneUtility.PaneSizeFor(pane).y==165,"External HUD guard did not yield native pane");}finally{testHarmony.Unpatch(active,mock);}
            var settings=PawnOverview.Settings;settings.enabled=false;string file=Path.Combine(GenFilePaths.SaveDataFolderPath,"PawnOverviewSettings.xml");Scribe.saver.InitSaving(file,"Settings");settings.ExposeData();Scribe.saver.FinalizeSaving();settings.enabled=true;Scribe.loader.InitLoading(file);settings.ExposeData();Scribe.loader.FinalizeLoading();Check(!settings.enabled&&!PawnOverview.Active,"Disabled setting failed Scribe round-trip");settings.enabled=true;
            Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Inspect);done=true;
        }catch(Exception e){error=e.ToString();done=true;}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"PawnOverviewProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"errors\":"+(error==null?"[]":"["+Q(error)+"]")+"}");
    }
    public override void MapComponentOnGUI()
    {
        if(!done||human==null||Event.current.type!=EventType.Repaint)return;
        if(ui){
            renderFrames++;
            if(renderFrames==10||renderFrames==40){var capture=AccessTools.TypeByName("UnityEngine.ScreenCapture");AccessTools.Method(capture,"CaptureScreenshot",new Type[]{typeof(string)}).Invoke(null,new object[]{Path.Combine(GenFilePaths.SaveDataFolderPath,renderFrames==10?"PawnOverview-Human.png":"PawnOverview-Animal.png")});}
            if(renderFrames==25)Select(animal);if(renderFrames==55)Select(human);return;
        }
        ui=true;string fault=null;int beforeUi=checks;
        try{
            var f=Text.Font;var a=Text.Anchor;var c=GUI.color;bool w=Text.WordWrap;
            PawnOverview.Draw(new Rect(0,0,436,180),human);Check(Text.Font==f&&Text.Anchor==a&&GUI.color==c&&Text.WordWrap==w,"HUD leaked GUI state");
            PawnOverview.Draw(new Rect(0,0,360,125),animal);Check(Text.Font==f&&Text.Anchor==a&&GUI.color==c&&Text.WordWrap==w,"Animal HUD leaked GUI state");
            LoadedModManager.GetMod<TurboMegaMod.TurboMegaMod>().DoSettingsWindowContents(new Rect(0,0,500,160));
            Check(pane.CurTabs.Any(),"Standard character tabs disappeared");
        }catch(Exception e){fault=e.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"PawnOverviewUiProbe.json"),"{\"passed\":"+(fault==null?"true":"false")+",\"checks\":"+(checks-beforeUi)+",\"visualLayoutInspected\":false,\"error\":"+(fault==null?"null":Q(fault))+"}");
    }
}
