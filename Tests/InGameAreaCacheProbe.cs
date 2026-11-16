using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

public sealed class AreaCacheProbe : MapComponent
{
    private bool done,ui;private int checks;private string error;private Building_ComplexDesk desk;
    public AreaCacheProbe(Map map):base(map){}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static object Field(object x,string name){return AccessTools.Field(x.GetType(),name).GetValue(x);}
    private static object Call(object x,string name,params object[] args){return AccessTools.Method(x.GetType(),name).Invoke(x,args);}
    private object FindPath(Pawn p,IntVec3 cell){var finder=Field(map,"pathFinder");var m=finder.GetType().GetMethods().Single(x=>x.Name=="FindPathNow"&&x.GetParameters().Length==5&&x.GetParameters()[2].ParameterType==typeof(Pawn));return m.Invoke(finder,new object[]{p.Position,new LocalTargetInfo(cell),p,null,PathEndMode.OnCell});}
    private void Found(Pawn p,IntVec3 cell){var path=FindPath(p,cell);Check((bool)AccessTools.Property(path.GetType(),"Found").GetValue(path,null),"Native path not found");Call(path,"ReleaseToPool");}
    public override void MapComponentUpdate(){
        var args=Environment.GetCommandLineArgs();bool baseline=args.Contains("-tmm-area-baseline");
        if(done||Find.CurrentMap!=map||(!baseline&&!args.Contains("-tmm-area-cache-probe")))return;
        try{
            foreach(var p in map.mapPawns.FreeColonistsSpawned){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
            foreach(var c in new CellRect(35,35,30,20)){foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);}
            map.fogGrid.ClearAllFog();map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            var pf=Field(map,"pathFinder");Call(pf,"ForceCompleteScheduledJobs");var data=Field(pf,"mapData");
            var requestType=AccessTools.TypeByName("Verse.PathRequest");Call(data,"GatherData",Array.CreateInstance(requestType,0));
            var areaSource=Field(data,"areas");var bits=(IDictionary)Field(areaSource,"areaToBits");
            Area_Allowed area,staff;map.areaManager.TryMakeNewAllowed(out area);map.areaManager.TryMakeNewAllowed(out staff);
            foreach(var c in new CellRect(36,36,27,17)){area[c]=true;if(c.x>=55)staff[c]=true;}
            desk=(Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk"));desk.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(desk,new IntVec3(40,0,40),map);desk.area=area;desk.staffArea=staff;desk.ApplyServiceAreas();
            var access=map.GetComponent<ComplexLayout>().guestAccess;Check(access!=null&&map.areaManager.AllAreas.Contains(access),"Auto guest area not registered");Check(!bits.Contains(access),"Fixture cache was not primed before new area");
            var f=Find.FactionManager.AllFactionsListForReading.First(x=>!x.IsPlayer&&x.def.humanlikeFaction&&!x.HostileTo(Faction.OfPlayer));
            var guest=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,f,forceGenerateNewPawn:true,canGeneratePawnRelations:false,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false));GenSpawn.Spawn(guest,new IntVec3(43,0,44),map);
            AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{f,new IntVec3(44,0,44),new System.Collections.Generic.List<Pawn>{guest},map,false,false,600000});guest.GetLord().ReceiveMemo("TravelArrived");
            bool missing=false;try{Found(guest,new IntVec3(49,0,44));}catch(TargetInvocationException e){if(e.InnerException is System.Collections.Generic.KeyNotFoundException)missing=true;else throw;}
            Check(missing==baseline,"Unexpected missing area result, baseline="+baseline+" actual="+missing);
            if(baseline){AccessTools.Field(data.GetType(),"lastGatherTick").SetValue(data,Find.TickManager.TicksGame-1);Found(guest,new IntVec3(49,0,44));}
            Check(bits.Contains(access),"Area not registered by native refresh");Check(!access[new IntVec3(58,0,44)]&&access[new IntVec3(49,0,44)],"Guest area lost staff isolation");
            Found(guest,new IntVec3(50,0,44));
            var old=access;old.Delete();map.GetComponent<ComplexLayout>().Synchronize();access=map.GetComponent<ComplexLayout>().guestAccess;
            Check(access!=null&&access!=old,"Deleted auto area not recreated");
            if(baseline)AccessTools.Field(data.GetType(),"lastGatherTick").SetValue(data,Find.TickManager.TicksGame-1);
            Found(guest,new IntVec3(49,0,44));Check(bits.Contains(access)&&!access[new IntVec3(58,0,44)],"Recreated area is unavailable or admits staff cell");
            Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
        }catch(Exception e){error=e.ToString();}
        done=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"AreaCacheProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"expectedBaselineFailure\":"+(baseline?"true":"false")+",\"errors\":"+(error==null?"[]":"["+Quote(error)+"]")+"}");
    }
    private static string Quote(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    public override void MapComponentOnGUI(){if(ui||!done||desk==null||Event.current.type!=EventType.Repaint)return;ui=true;string e=null;try{new Dialog_ComplexServices(desk).DoWindowContents(new Rect(0,0,740,720));}catch(Exception ex){e=ex.ToString();}File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"AreaCacheUiProbe.json"),"{\"passed\":"+(e==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(e==null?"null":Quote(e))+"}");}
}
