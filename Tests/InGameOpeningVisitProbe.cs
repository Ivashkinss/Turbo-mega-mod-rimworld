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

[StaticConstructorOnStartup]
public static class OpeningLaunch
{
    static OpeningLaunch(){var args=Environment.GetCommandLineArgs();if(!args.Any(a=>a=="-tmm-opening-probe"||a=="-tmm-opening-reload"))return;var h=new Harmony("tmm.test.opening.weather");h.Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(OpeningLaunch),"Temp"));if(args.Contains("-tmm-opening-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-OpeningVisit"));}
    private static bool Temp(ref float __result){__result=21;return false;}
}
public sealed class OpeningVisitProbe:MapComponent
{
    private Building_ComplexDesk desk;private Building_Bed bed;private Faction home;private bool saved,done,ui;private int checks,farTick,originalDue,stage,start;private string error;
    private object Native{get{return map.components.First(c=>c.GetType().FullName=="Hospitality.Hospitality_MapComponent");}}
    private IncidentQueue Queue{get{return (IncidentQueue)AccessTools.Field(Native.GetType(),"incidentQueue").GetValue(Native);}}
    private ComplexOpeningVisit Opening{get{return map.GetComponent<ComplexOpeningVisit>();}}
    public OpeningVisitProbe(Map map):base(map){}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private static void Set(object o,string field,object value){AccessTools.Field(o.GetType(),field).SetValue(o,value);}
    private Building Build(string name,IntVec3 c){var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,c,map);}
    private void Room(CellRect rect){foreach(var c in rect.EdgeCells)Build(c==new IntVec3(rect.CenterCell.x,0,rect.minZ)?"Door":"Wall",c);foreach(var c in rect.ContractedBy(1))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);}
    private List<QueuedIncident> Visits(){var list=new List<QueuedIncident>();foreach(QueuedIncident q in Queue)list.Add(q);return list;}
    private void AddVisit(float days){var method=AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.GenericUtility"),"PlanNewVisit");method.Invoke(null,new object[]{map,days,home});}
    private void ResetOpening(){Opening.completed=false;Opening.scheduledTick=-1;Opening.invitedFaction=null;}
    private void Fresh(){
        foreach(var p in map.mapPawns.FreeColonistsSpawned){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
        map.fogGrid.ClearAllFog();foreach(var c in new CellRect(35,35,34,27)){foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant||t is Filth){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);}
        Queue.Clear();Set(Native,"nextQueueInspection",Find.TickManager.TicksGame+60000);Set(Native,"askForSafety",false);
        Check(Opening!=null&&!Opening.completed&&!Opening.TryPlanOpeningVisit()&&Queue.Count==0,"Missing desk scheduled a visit");
        Area_Allowed area,staff;map.areaManager.TryMakeNewAllowed(out area);map.areaManager.TryMakeNewAllowed(out staff);foreach(var c in new CellRect(36,36,30,24))area[c]=true;
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(40,0,37));desk.area=area;desk.staffArea=staff;desk.vipChance=0;desk.roomRate=0;
        Check(!Opening.TryPlanOpeningVisit()&&!Opening.completed&&Queue.Count==0,"Bedless desk scheduled a visit");
        Room(new CellRect(40,40,9,9));bed=(Building_Bed)Build("BedGuest",new IntVec3(43,0,43));map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();desk.ApplyServiceAreas();
        home=Find.FactionManager.AllFactionsVisible.First(f=>!f.IsPlayer&&!f.defeated&&!f.HostileTo(Faction.OfPlayer)&&f.def.pawnGroupMakers!=null&&f.def.pawnGroupMakers.Any(g=>g.kindDef==PawnGroupKindDefOf.Peaceful));
        AddVisit(14);farTick=Visits().Single().FireTick;
        Set(Native,"guestsAreWelcome",false);Check(!Opening.TryPlanOpeningVisit()&&!Opening.completed&&Queue.Count==1,"Disabled admission created invitation");Set(Native,"guestsAreWelcome",true);
        var settings=AccessTools.TypeByName("Hospitality.ModSettings_Hospitality");AccessTools.Field(settings,"disableGuests").SetValue(null,true);Check(!Opening.TryPlanOpeningVisit()&&!Opening.completed,"Globally disabled guests created invitation");AccessTools.Field(settings,"disableGuests").SetValue(null,false);
        staff[bed.Position]=true;Check(!Opening.TryPlanOpeningVisit()&&!Opening.completed,"Staff bed attracted guests");staff[bed.Position]=false;
        var owner=map.mapPawns.FreeColonistsSpawned.First();owner.ownership.ClaimBedIfNonMedical(bed);Check(!Opening.TryPlanOpeningVisit()&&!Opening.completed,"Owned bed attracted unrelated guests");owner.ownership.UnclaimBed();
        Queue.Clear();AddVisit(0.5f);int soon=Visits().Single().FireTick;Check(!Opening.TryPlanOpeningVisit()&&Opening.completed&&Queue.Count==1&&Visits().Single().FireTick==soon,"Soon existing visit was duplicated or shifted");
        ResetOpening();Queue.Clear();AddVisit(14);farTick=Visits().Single().FireTick;
        Set(Opening,"nextCheck",0);Opening.MapComponentTick();originalDue=Opening.scheduledTick;
        Check(Opening.completed&&Opening.invitedFaction==home&&originalDue>=Find.TickManager.TicksGame+60000&&originalDue<=Find.TickManager.TicksGame+120000,"Tick planner did not schedule 1–2 days");
        Check(Queue.Count==2&&Visits().Any(q=>q.FireTick==farTick)&&Visits().Any(q=>q.FireTick==originalDue),"Existing long queue was lost or edited");
        for(int i=0;i<3;i++){Opening.MapComponentTick();Opening.TryPlanOpeningVisit();}Check(Queue.Count==2&&Opening.scheduledTick==originalDue,"Repeated checks duplicated opening visit");
        desk.DeSpawn();GenSpawn.Spawn(desk,new IntVec3(40,0,37),map);Check(Opening.completed&&!Opening.TryPlanOpeningVisit()&&Queue.Count==2,"Desk reinstall restarted opening invitation");
        Room(new CellRect(52,40,9,9));var ward=(Building_Bed)Build("Bed",new IntVec3(55,0,43));ward.Medical=true;map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        var hospital=Clinic.Hospital(map);for(int h=0;h<24;h++)ComplexReceptionUI.SetHour(hospital,h,true);Set(hospital,"openForBusiness",false);
        int free=(int)AccessTools.Method(hospital.GetType(),"FreeMedicalBeds").Invoke(hospital,null);Check(free==1&&Clinic.WardBed(ward),"Actual hospital did not count the single clinical bed: "+free);
        var can=AccessTools.Method(AccessTools.TypeByName("Hospital.IncidentHelper"),"CanSpawnPatient");Check(!(bool)can.Invoke(null,new object[]{map}),"Closed clinic accepted patients");Set(hospital,"openForBusiness",true);Check((bool)can.Invoke(null,new object[]{map}),"Open clinic with free bed and edge route rejected patients");
        ComplexReceptionUI.SetHour(hospital,GenLocalDate.HourOfDay(map),false);Check(!(bool)can.Invoke(null,new object[]{map}),"Clinic hours ignored");ComplexReceptionUI.SetHour(hospital,GenLocalDate.HourOfDay(map),true);Set(hospital,"openForBusiness",false);
        saved=true;GameDataSaveLoader.SaveGame("TMM-OpeningVisit");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-OpeningVisit")),"Opening save missing");Finish(false);
    }
    private void Reload(){
        if(stage==0){
            Check(saved&&Opening!=null&&Opening.completed&&Opening.scheduledTick==originalDue&&Opening.invitedFaction==home,"Opening marker/faction/time failed save/load");
            Check(Queue.Count==2&&Visits().Any(q=>q.FireTick==farTick)&&Visits().Any(q=>q.FireTick==originalDue),"Native queue dates changed on load");Opening.TryPlanOpeningVisit();Opening.MapComponentTick();Check(Queue.Count==2,"Load duplicated invitation");
            // Accelerate only our own test invitation; production's planned interval was checked above.
            var dueVisit=Visits().Single(v=>v.FireTick==originalDue);AccessTools.Field(typeof(QueuedIncident),"fireTick").SetValue(dueVisit,Find.TickManager.TicksGame+1);
            Set(Native,"askForSafety",false);Set(Native,"nextQueueInspection",Find.TickManager.TicksGame+60000);start=Find.TickManager.TicksGame;stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;return;
        }
        var type=AccessTools.TypeByName("Hospitality.LordJob_VisitColony");var groups=map.lordManager.lords.Where(l=>type.IsInstanceOfType(l.LordJob)&&l.ownedPawns.Any(p=>p.Spawned&&!p.Dead)).ToList();
        if(groups.Count==0){if(Find.TickManager.TicksGame-start>2500)throw new Exception("Due native queued visitor group never spawned; remaining="+Queue.Count);return;}
        Check(groups.Count==1&&groups[0].faction==home,"Wrong or duplicate visitor groups spawned");Check(Visits().Any(q=>q.FireTick==farTick),"Due invitation consumed original long-term visit");
        int count=Queue.Count;Opening.TryPlanOpeningVisit();Check(Queue.Count==count,"Arrival repeated opening invitation");
        var fresh=new ComplexOpeningVisit(map);Check(!fresh.TryPlanOpeningVisit()&&fresh.completed&&Queue.Count==count,"Existing actual visitors attracted an extra opening group");Finish(true);
    }
    public override void ExposeData(){base.ExposeData();Scribe_References.Look(ref desk,"ovDesk");Scribe_References.Look(ref bed,"ovBed");Scribe_References.Look(ref home,"ovHome");Scribe_Values.Look(ref saved,"ovSaved");Scribe_Values.Look(ref farTick,"ovFar");Scribe_Values.Look(ref originalDue,"ovDue");}
    public override void MapComponentUpdate(){bool reload=Environment.GetCommandLineArgs().Contains("-tmm-opening-reload");if(done||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-opening-probe")))return;try{if(reload)Reload();else Fresh();}catch(Exception e){error=e.ToString();Finish(reload);}}
    private void Finish(bool reload){done=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"OpeningVisitReloadProbe.json":"OpeningVisitProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"errors\":"+(error==null?"[]":"["+Q(error)+"]")+"}");}
    public override void MapComponentOnGUI(){if(ui||!done||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-opening-probe"))return;ui=true;string failure=null;try{new Dialog_ComplexServices(desk){page=1}.DoWindowContents(new Rect(0,0,744,726));new Dialog_ServiceReadiness(desk).DoWindowContents(new Rect(0,0,744,696));}catch(Exception e){failure=e.ToString();}File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"OpeningVisitUiProbe.json"),"{\"passed\":"+(failure==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(failure==null?"null":Q(failure))+"}");}
}
