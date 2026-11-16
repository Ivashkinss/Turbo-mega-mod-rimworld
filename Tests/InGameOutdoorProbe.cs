using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class OutdoorProbeLaunch
{
    static OutdoorProbeLaunch(){
        var args=Environment.GetCommandLineArgs();
        if(args.Contains("-tmm-outdoor-probe")||args.Contains("-tmm-outdoor-reload"))new Harmony("tmm.test.outdoor.temperature").Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(OutdoorProbeLaunch),"Temperature"));
        if(Environment.GetCommandLineArgs().Contains("-tmm-outdoor-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-Outdoor"));}
    private static bool Temperature(ref float __result){__result=21;return false;}
}
public sealed class OutdoorProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Pawn guest;
    private Area_Allowed staff;
    private int checks,stage,start;
    private float savedLevel;
    private bool saved,loaded,reported,ui;
    private readonly List<string> errors=new List<string>();
    public OutdoorProbe(Map map):base(map){}
    public override void ExposeData()
    {
        base.ExposeData();Scribe_References.Look(ref desk,"outdoorDesk");Scribe_References.Look(ref guest,"outdoorGuest");Scribe_Values.Look(ref saved,"outdoorSaved");Scribe_Values.Look(ref savedLevel,"outdoorLevel");
        if(Scribe.mode==LoadSaveMode.PostLoadInit&&guest!=null)loaded=Need!=null&&Math.Abs(Need.CurLevelPercentage-savedLevel)<0.0001f&&guest.CurJobDef==JobDef&&desk.outdoorBreaks;
    }
    private Need Need {get{return guest.needs.TryGetNeed(GuestOutdoor.Outdoors);}}
    private JobDef JobDef {get{return DefDatabase<JobDef>.GetNamed("TMM_GuestOutdoorBreak");}}
    private void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    private static string Escape(string text){return "\""+text.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private bool Should(Pawn p,NeedDef def){return (bool)AccessTools.Method(typeof(Pawn_NeedsTracker),"ShouldHaveNeed").Invoke(p.needs,new object[]{def});}
    private void Setup()
    {
        map.fogGrid.ClearAllFog();map.weatherManager.TransitionTo(WeatherDefOf.Clear);
        AccessTools.Field(typeof(WeatherManager),"lastWeather").SetValue(map.weatherManager,WeatherDefOf.Clear);AccessTools.Field(typeof(WeatherManager),"curWeatherAge").SetValue(map.weatherManager,0);
        foreach(var c in new CellRect(38,38,20,20)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
        }
        // Connect the carved rectangle to actual outdoor regions even when
        // quicktest placed it within a mountain. This corridor is outside desk.area.
        foreach(var c in new CellRect(0,43,38,3)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);
        }
        desk=(Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk"));desk.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(desk,new IntVec3(42,0,42),map);
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);map.areaManager.TryMakeNewAllowed(out staff);foreach(var c in new CellRect(38,38,20,20))area[c]=true;
        desk.area=area;desk.ApplyServiceAreas();
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        guest=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false,forcedXenotype:XenotypeDefOf.Baseliner));
        foreach(var t in guest.story.traits.allTraits.Where(t=>t.def==TraitDefOf.Undergrounder).ToArray())guest.story.traits.RemoveTrait(t);
        GenSpawn.Spawn(guest,new IntVec3(44,0,44),map);guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=1;
        // Quicktest can generate an ancient hostile group elsewhere on its map.
        // Remove only hostile targets from this disposable test map.
        foreach(var target in map.attackTargetsCache.TargetsHostileToFaction(faction).Select(t=>t.Thing).Where(t=>t.Spawned).ToArray())target.DeSpawn();
        Check(!Should(guest,GuestOutdoor.Outdoors),"Nonvisiting foreign pawn acquired outdoor need");
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(44,0,44),new List<Pawn>{guest},map,false,false,600000});guest.GetLord().ReceiveMemo("TravelArrived");GuestOutdoor.Refresh(guest);
        guest.needs.joy.CurLevelPercentage=0.99f;
        Check(GuestOutdoor.Eligible(guest)&&Should(guest,GuestOutdoor.Outdoors)&&Need!=null,"Native hosted-guest rules did not permit/create outdoors need: eligible="+GuestOutdoor.Eligible(guest)+" should="+Should(guest,GuestOutdoor.Outdoors)+" need="+(Need!=null)+" host="+guest.HostFaction+" arrived="+GuestDossiers.Arrived(guest)+" genes="+string.Join(",",guest.genes.GenesListForReading.Select(g=>g.def.defName))+" hediffs="+string.Join(",",guest.health.hediffSet.hediffs.Select(h=>h.def.defName)));
        var colonist=map.mapPawns.FreeColonistsSpawned.First();Check(Should(colonist,GuestOutdoor.Outdoors),"Colony need eligibility was changed");
        var learning=DefDatabase<NeedDef>.GetNamedSilentFail("Learning");if(learning!=null)Check(!Should(guest,learning),"Outdoor bypass enabled unrelated learning need");
        // A real native health restriction must survive the faction-gate bypass.
        var def=new HediffDef{defName="TMM_TestNoOutdoor",hediffClass=typeof(HediffWithComps),stages=new List<HediffStage>{new HediffStage{disablesNeeds=new List<NeedDef>{GuestOutdoor.Outdoors}}}};
        DefDatabase<HediffDef>.Add(def);var hediff=HediffMaker.MakeHediff(def,guest);guest.health.AddHediff(hediff);GuestOutdoor.Refresh(guest);
        Check(!Should(guest,GuestOutdoor.Outdoors)&&Need==null,"Native health need restriction was bypassed");guest.health.RemoveHediff(hediff);GuestOutdoor.Refresh(guest);Check(Need!=null,"Need did not return after native restriction cleared");
        float retained=Need.CurLevelPercentage;desk.outdoorBreaks=false;GuestOutdoor.Refresh(guest);Check(Need!=null&&Need.CurLevelPercentage==retained&&Should(guest,GuestOutdoor.Outdoors)&&GuestOutdoor.MakeJob(guest)==null,"Disabling breaks changed native need level or still offered a break");
        desk.outdoorBreaks=true;GuestOutdoor.Refresh(guest);Need.CurLevelPercentage=0.1f;
        Check(!GuestOutdoor.FindSpot(guest).IsValid,"Roofed complex offered outdoor break");
        var yard=new IntVec3(50,0,44);map.roofGrid.SetRoof(yard,null);map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        // Only the isolated fixture overrides OutdoorTemp to21; no native defs
        // or user weather/biome data are changed.
        var weatherReason=new System.Text.StringBuilder();bool pleasant=JoyUtility.EnjoyableOutsideNow(guest,weatherReason);
        Check(GuestOutdoor.Weather(guest),"Fixture clear weather not enjoyable; temperature="+yard.GetTemperature(map)+" pleasant="+pleasant+" reason="+weatherReason+" threats="+GenHostility.AnyHostileActiveThreatTo(map,guest.Faction));
        Check(GuestOutdoor.Spot(guest,yard),"Fixture yard rejected: temp="+yard.GetTemperature(map)+" public="+GuestOutdoor.PublicStep(guest,yard)+" outdoors="+yard.UsesOutdoorTemperature(map)+" range="+GenTemperature.ComfortableTemperatureRange(guest));
        var toxic=GameConditionMaker.MakeCondition(GameConditionDefOf.ToxicFallout,2500);map.gameConditionManager.RegisterCondition(toxic);
        Check(!GuestOutdoor.Weather(guest)&&GuestOutdoor.MakeJob(guest)==null,"Toxic fallout did not prevent outdoor breaks");toxic.End();
        foreach(var c in new CellRect(47,38,1,20))staff[c]=true;desk.staffArea=staff;desk.ApplyServiceAreas();
        Check(!GuestOutdoor.FindSpot(guest).IsValid,"Outdoor search crossed a staff-only corridor");
        desk.staffArea=null;desk.ApplyServiceAreas();Check(GuestOutdoor.FindSpot(guest)==yard,"Public reachable courtyard not found");
        guest.needs.rest.CurLevelPercentage=0.1f;Check(GuestOutdoor.MakeJob(guest)==null,"Exhausted guest was sent walking");guest.needs.rest.CurLevelPercentage=1;
        guest.needs.food.CurLevelPercentage=0.1f;Check(GuestOutdoor.MakeJob(guest)==null,"Hungry guest was sent walking");
        guest.needs.food.CurLevelPercentage=0.5f;Check(GuestOutdoor.MakeJob(guest)==null,"Guest needing a restaurant meal was sent walking");guest.needs.food.CurLevelPercentage=1;
        var dataType=AccessTools.TypeByName("Hospital.PatientData");var kind=AccessTools.TypeByName("Hospital.PatientType");
        Clinic.Patients(map)[guest]=Activator.CreateInstance(dataType,new object[]{GuestDossiers.Now,guest.MarketValue,0.5f,Enum.ToObject(kind,1)});
        float before=Need.CurLevelPercentage;Need.NeedInterval();Check(Need.CurLevelPercentage==before&&GuestOutdoor.MakeJob(guest)==null,"Clinical guest lost outdoors level or left treatment");
        var visit=new ClinicVisit{pawn=guest,lord=guest.GetLord()};map.GetComponent<ClinicGuests>().visits.Add(visit);
        Check(colonist.carryTracker.TryStartCarry(guest,1,false)==1,"Could not carry clinical guest in fixture");Need.NeedInterval();Check(GuestOutdoor.Clinical(guest)&&Need.CurLevelPercentage==before,"Carried clinical guest's need was not paused");
        Thing dropped;Check(colonist.carryTracker.TryDropCarriedThing(new IntVec3(44,0,44),ThingPlaceMode.Near,out dropped),"Could not restore carried guest");
        map.GetComponent<ClinicGuests>().visits.Remove(visit);Clinic.Patients(map).Remove(guest);
        Check((bool)AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.GuestUtility"),"IsGuest").Invoke(null,new object[]{guest}),"Native guest registry did not recover after temporary transport");
        Need.CurLevelPercentage=0.35f;Check(GuestOutdoor.MakeJob(guest)==null,"Outdoor break ignored exact low threshold");Need.CurLevelPercentage=0.1f;
        guest.jobs.EndCurrentJob(JobCondition.InterruptForced,false);
        var result=DefDatabase<DutyDef>.GetNamed("Relax").thinkNode.TryIssueJobPackage(guest,default(JobIssueParams));
        Check(result.Job!=null&&result.Job.def==JobDef,"Native guest think tree did not issue outdoor job");
        guest.jobs.StartJob(result.Job,JobCondition.InterruptForced);start=GuestDossiers.Now;stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-outdoor-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-outdoor-probe")))return;
        try{
            if(reload){
                Check(saved&&loaded,"Native need level, outdoor job or reception option was not restored before simulation");
                Check(Need!=null&&guest.CurJobDef==JobDef,"Saved outdoor break did not resume");
                float retained=Need.CurLevelPercentage;desk.outdoorBreaks=false;GuestOutdoor.Refresh(guest);Check(Need!=null&&Need.CurLevelPercentage==retained&&GuestOutdoor.MakeJob(guest)==null,"Disabling restored break changed native need or offered a new break");
                Check(Should(map.mapPawns.FreeColonistsSpawned.First(),GuestOutdoor.Outdoors),"Disabling guest option changed colony needs");
                Finish(true);return;
            }
            if(stage==0){Setup();return;}
            if(GuestDossiers.Now%150==0)File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"OutdoorProgress.log"),GuestDossiers.Now+" "+guest.Position+" "+guest.CurJob+" fit="+GuestOutdoor.Fit(guest)+" weather="+GuestOutdoor.Weather(guest)+" need="+(Need==null?-1:Need.CurLevelPercentage)+Environment.NewLine);
            if(GuestDossiers.Now-start<1000&&(Need==null||Need.CurLevelPercentage<0.14f||guest.CurJob==null||guest.Position!=guest.CurJob.targetA.Cell))return;
            Check(guest.CurJobDef==JobDef&&guest.Position==guest.CurJob.targetA.Cell,"Guest did not physically reach courtyard: position="+guest.Position+" job="+guest.CurJob+" fit="+GuestOutdoor.Fit(guest)+" weather="+GuestOutdoor.Weather(guest)+" needs="+(Need==null?-1:Need.CurLevelPercentage));
            Check(guest.Position.GetRoof(map)==null&&GuestOutdoor.Spot(guest,guest.Position),"Guest rested at an invalid destination");
            Check(Need.CurLevelPercentage>0.1f&&Need.CurLevelPercentage<1f,"Native outdoor need did not recover naturally or was auto-filled");
            saved=true;savedLevel=Need.CurLevelPercentage;GameDataSaveLoader.SaveGame("TMM-Outdoor");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Outdoor")),"Outdoor fixture save missing");Finish(false);
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    public override void MapComponentOnGUI()
    {
        if(ui||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-outdoor-probe"))return;
        ui=true;string error=null;try{new Dialog_GuestNeeds(desk).DoWindowContents(new Rect(10,10,880,710));new Dialog_ComplexServices(desk){page=1}.DoWindowContents(new Rect(10,10,740,720));}catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"OutdoorUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"OutdoorReloadProbe.json":"OutdoorProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
}
