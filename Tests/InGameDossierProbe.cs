using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class DossierProbeLaunch
{
    public static bool blockSilver=true;
    static DossierProbeLaunch()
    {
        var args=Environment.GetCommandLineArgs();
        if(args.Contains("-tmm-dossier-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-Dossiers"));
        if(!args.Contains("-tmm-dossier-probe")&&!args.Contains("-tmm-dossier-reload"))return;
        var method=typeof(GenPlace).GetMethods().First(m=>m.Name=="TryPlaceThing"&&m.GetParameters().Any(p=>p.ParameterType==typeof(Thing).MakeByRefType()));
        new Harmony("tmm.test.dossiers.silver").Patch(method,prefix:new HarmonyMethod(typeof(DossierProbeLaunch),"Block"));
    }
    private static bool Block(Thing __0,ref bool __result){if(!blockSilver||__0==null||__0.def!=ThingDefOf.Silver)return true;__result=false;return false;}
}
public sealed class DossierProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Pawn first,second,patient;
    private string firstId,secondId,lostId;
    private int stage,started,checks,savedFloor,startFrame;
    private bool saved,reported,uiReported;
    private readonly List<string> errors=new List<string>();
    private GuestDossiers Book {get{return GuestDossiers.Book;}}
    public DossierProbe(Map map):base(map){}
    public override void ExposeData()
    {
        base.ExposeData();Scribe_References.Look(ref desk,"dossierDesk");Scribe_References.Look(ref first,"dossierFirst");
        Scribe_References.Look(ref second,"dossierSecond");Scribe_References.Look(ref patient,"dossierPatient");
        Scribe_Values.Look(ref firstId,"dossierFirstId");Scribe_Values.Look(ref secondId,"dossierSecondId");Scribe_Values.Look(ref lostId,"dossierLostId");
        Scribe_Values.Look(ref saved,"dossierSaved");Scribe_Values.Look(ref savedFloor,"dossierFloor");
    }
    private void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    private int Floor(){return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}
    private Pawn Spawn(Faction faction,IntVec3 cell)
    {
        var p=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,
            canGeneratePawnRelations:false,allowDead:false,allowDowned:false,fixedBiologicalAge:25));
        GenSpawn.Spawn(p,cell,map);return p;
    }
    private void Create(Faction faction,params Pawn[] pawns)
    {
        foreach(var pawn in pawns){var old=pawn.GetLord();if(old!=null)old.Notify_PawnLost(pawn,PawnLostCondition.Undefined);}
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,
            new object[]{faction,new IntVec3(44,0,44),pawns.ToList(),map,false,false,600000});
    }
    private void Arrive(Pawn pawn){pawn.GetLord().ReceiveMemo("TravelArrived");}
    private void Setup()
    {
        foreach(var cell in new CellRect(38,38,20,20)){
            foreach(var t in cell.GetThingList(map).ToArray())if(t is Building||t is Plant)t.Destroy();
            map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);map.roofGrid.SetRoof(cell,null);
        }
        var doorPath=new CellRect(0,47,58,7);
        foreach(var cell in doorPath){foreach(var t in cell.GetThingList(map).ToArray())if(t is Building||t is Plant)t.Destroy();map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);}
        desk=(Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk"));desk.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(desk,new IntVec3(43,0,43),map);
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var c in new CellRect(38,38,20,20))area[c]=true;desk.area=area;desk.ApplyServiceAreas();
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        first=Spawn(faction,new IntVec3(3,0,50));second=Spawn(faction,new IntVec3(44,0,45));first.Name=new NameSingle("Тёзка");second.Name=new NameSingle("Тёзка");
        firstId=first.GetUniqueLoadID();secondId=second.GetUniqueLoadID();Create(faction,first);Arrive(first);
        Check(Book.Find(firstId)!=null&&Book.Find(firstId).visitCount==1,"Native arrival not journalled");
        Book.Begin(first,map,false,false);Check(Book.Find(firstId).visitCount==1,"Repeated arrival duplicated visit");
        var firstRow=Book.Find(firstId);Book.SetBlocked(firstRow,true,"test decision");Check(GuestDossiers.HasCurrent(first,map)&&GuestDossiers.Blocked(first),"Blacklist interrupted the current visit");
        AccessTools.Method(HotelBillingHooks.Guest(first).GetType(),"Leave").Invoke(HotelBillingHooks.Guest(first),new object[]{false});
        Check(firstRow.Current!=null&&firstRow.Current.leaving,"Leave was treated as physical departure");
        first.DeSpawn();Book.Synchronize();Check(firstRow.Current==null&&firstRow.visits.Last().endReason=="departed","Confirmed map departure not recorded");
        GenSpawn.Spawn(first,new IntVec3(3,0,50),map);Book.SetBlocked(firstRow,false,"");Create(faction,first);Arrive(first);
        Check(firstRow.visitCount==2&&first.GetLord().LordJob.GetType().FullName=="Hospitality.LordJob_VisitColony","Unblocking did not permit a later visit");
        AccessTools.Method(HotelBillingHooks.Guest(first).GetType(),"Leave").Invoke(HotelBillingHooks.Guest(first),new object[]{false});first.DeSpawn();Book.Synchronize();GenSpawn.Spawn(first,new IntVec3(3,0,50),map);Book.SetBlocked(firstRow,true,"second refusal");
        Create(faction,first,second);Arrive(second);
        Check(first.GetLord().LordJob is LordJob_RefusedGuest&&second.GetLord().LordJob.GetType().FullName=="Hospitality.LordJob_VisitColony","Mixed group admission did not split the blocked pawn");
        Check(first.Spawned&&firstRow.visitCount==2&&firstRow.rejectedCount==1,"Refusal teleported pawn or counted an accepted visit");
        var secondRow=Book.Find(secondId);Check(secondRow!=null&&secondRow.id!=firstRow.id&&secondRow.name==firstRow.name,"Same-name pawns merged");
        Book.SetBlocked(secondRow,true,"may finish current visit");Create(faction,second);Arrive(second);
        Check(secondRow.visitCount==1&&!(second.GetLord().LordJob is LordJob_RefusedGuest),"Existing visitor grace was lost");
        foreach(var service in new[]{"hotel","restaurant","company","sales","clinic"}){
            Check(Book.Payment(secondId,secondRow.Current.id,service,10,"ledger:"+service),"Ledger entry rejected");
            Check(!Book.Payment(secondId,secondRow.Current.id,service,10,"ledger:"+service),"Duplicate receipt increased totals");
        }
        Check(secondRow.PersonalTotal==40&&secondRow.clinic==10,"Faction medical reward counted as personal spending");
        Check(!Book.Payment(secondId,0,"hotel",0,"zero")&&!Book.Payment(secondId,0,"hotel",-5,"negative"),"Nonpositive payment accepted");
        var carrier=Spawn(Faction.OfPlayer,new IntVec3(45,0,45));
        int visit=secondRow.Current.id;carrier.carryTracker.TryStartCarry(second,1,false);Check(carrier.carryTracker.CarriedThing==second,"Carry fixture could not take guest");
        Book.Synchronize();Check(secondRow.Current!=null&&secondRow.Current.id==visit&&second.MapHeld==map,"Temporary carry ended the visit");
        Thing placed;Check(carrier.carryTracker.TryDropCarriedThing(new IntVec3(44,0,45),ThingPlaceMode.Near,out placed),"Carry fixture could not restore guest");
        patient=Spawn(faction,new IntVec3(46,0,45));Book.SetBlocked(Book.Observe(patient),true,"medical exception");
        var dataType=AccessTools.TypeByName("Hospital.PatientData");var kind=AccessTools.TypeByName("Hospital.PatientType");
        var data=Activator.CreateInstance(dataType,new object[]{Find.TickManager.TicksGame,patient.MarketValue,0.5f,Enum.ToObject(kind,1)});
        AccessTools.Method(Clinic.HospitalType,"PatientArrived").Invoke(Clinic.Hospital(map),new[]{(object)patient,data});
        Create(faction,patient);Arrive(patient);Check(Clinic.ActivePatient(patient)&&!(patient.GetLord().LordJob is LordJob_RefusedGuest),"Blacklist blocked an existing clinical patient");
        var lost=Spawn(faction,new IntVec3(47,0,45));var link=GuestDossiers.Capture(lost,map);lostId=lost.GetUniqueLoadID();
        map.GetComponent<ClinicAccounts>().Queue("dossier-saved-reward",desk.Position,17,null,link);lost.Destroy();
        map.GetComponent<ClinicAccounts>().PlaceReceipts();Check(Book.Find(lostId).clinic==0&&map.GetComponent<ClinicAccounts>().receipts.Any(),"Blocked escrow generated a dossier payment");
        started=Find.TickManager.TicksGame;startFrame=Time.frameCount;stage=1;
        Trace();Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-dossier-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-dossier-probe")))return;
        try{
            if(reload){
                Check(saved&&desk!=null&&second!=null,"Saved dossier fixture missing");
                var a=Book.Find(firstId);var b=Book.Find(secondId);var lost=Book.Find(lostId);
                Check(a!=null&&a.blocked&&a.reason=="second refusal"&&a.visitCount==2,"Blacklist or lifetime visit counts lost");
                Check(b!=null&&b.blocked&&b.PersonalTotal==40&&b.clinic==10,"Saved ledger totals lost");
                Check(!Book.Payment(secondId,0,"hotel",10,"ledger:hotel")&&b.hotel==10,"Saved deduplication lost");
                Check(lost!=null&&lost.pawn==null&&!string.IsNullOrEmpty(lost.name)&&lost.clinic==0,"Detached identity did not survive independently");
                DossierProbeLaunch.blockSilver=false;map.GetComponent<ClinicAccounts>().PlaceReceipts();
                Check(lost.clinic==17&&lost.PersonalTotal==0&&Floor()==savedFloor+17,"Late faction reward lost its saved guest identity or physical coins");
                map.GetComponent<ClinicAccounts>().PlaceReceipts();Check(lost.clinic==17&&Floor()==savedFloor+17,"Repeated placement duplicated dossier receipt");
                Book.SetBlocked(b,false,"");Check(!GuestDossiers.Blocked(second),"Unblocking after reload failed");Finish(true);return;
            }
            if(stage==0){Setup();return;}
            if(stage==1){
                if(Time.frameCount%120==0)Trace();
                if(Find.TickManager.TicksGame==started&&Time.frameCount-startFrame>=900)throw new Exception("Fixture time remained paused: "+string.Join(",",Find.WindowStack.Windows.Select(w=>w.GetType().FullName)));
                if(first.Spawned&&Find.TickManager.TicksGame-started<3000)return;
                Check(!first.Spawned,"Refused visitor did not physically leave via native exit");Book.Synchronize();
                Check(Book.Find(firstId).Current==null&&Book.Find(firstId).visitCount==2,"Refused exit altered accepted visit count");
                savedFloor=Floor();saved=true;GameDataSaveLoader.SaveGame("TMM-Dossiers");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Dossiers")),"Dossier save not created");Finish(false);
            }
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    public override void MapComponentOnGUI()
    {
        if(uiReported||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-dossier-probe"))return;
        uiReported=true;string error=null;
        try{var window=new Dialog_GuestDossiers(desk);AccessTools.Field(typeof(Dialog_GuestDossiers),"selected").SetValue(window,secondId);window.DoWindowContents(new Rect(10,10,1000,710));}
        catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"DossierUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
    private static string Escape(string text){return "\""+text.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Trace(){File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"DossierProgress.log"),"tick="+Find.TickManager.TicksGame+" speed="+Find.TickManager.CurTimeSpeed+" paused="+Find.TickManager.Paused+" windows="+string.Join(",",Find.WindowStack.Windows.Select(w=>w.GetType().FullName))+" first="+(first==null?"null":first.Spawned+" "+first.CurJob)+Environment.NewLine);}
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"DossierReloadProbe.json":"DossierProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
}
