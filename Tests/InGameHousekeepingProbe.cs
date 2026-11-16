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
public static class HousekeepingProbeLaunch {
    static HousekeepingProbeLaunch(){var args=Environment.GetCommandLineArgs();if(args.Contains("-tmm-housekeeping-probe")||args.Contains("-tmm-housekeeping-reload"))new Harmony("tmm.test.housekeeping.temp").Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(HousekeepingProbeLaunch),"Temp"));if(args.Contains("-tmm-housekeeping-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-Housekeeping"));}
    private static bool Temp(ref float __result){__result=21;return false;}
}
public sealed class HousekeepingProbe : MapComponent {
    private Pawn first,second,guest;private Building_ComplexDesk desk;private Building_Bed bed;private List<Filth> dirt=new List<Filth>();private Filth interrupted;
    private int stage,start,checks,initialSilver;private float slowWork;private bool done,ui,saved;private string error;
    public HousekeepingProbe(Map map):base(map){}
    private HousekeepingLedger Ledger {get{return map.GetComponent<HousekeepingLedger>();}}
    private WorkBuyouts Book {get{return Current.Game.GetComponent<WorkBuyouts>();}}
    private int Silver {get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}}
    private void Check(bool ok,string s){if(!ok)throw new Exception(s);checks++;}
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private static void Wait(Pawn p){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
    private Building Build(string name,IntVec3 c){var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,c,map);}
    private Pawn Worker(IntVec3 c,int target){var p=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,Faction.OfPlayer,forceGenerateNewPawn:true,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false,validatorPreGear:x=>!x.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning)));GenSpawn.Spawn(p,c,map);p.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);p.needs.food.CurLevelPercentage=1;p.needs.rest.CurLevelPercentage=1;foreach(var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)p.workSettings.SetPriority(w,0);Wait(p);Check(Book.Begin(p,target)!=null,"Could not start worker contract");return p;}
    private Filth Dirt(IntVec3 c){foreach(var f in c.GetThingList(map).OfType<Filth>().ToArray())f.Destroy();var f1=(Filth)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Filth_Dirt"));GenSpawn.Spawn(f1,c,map);AccessTools.Field(typeof(Filth),"growTick").SetValue(f1,GuestDossiers.Now);return f1;}
    private void Clean(Pawn p,int count,bool outside=false){Wait(p);dirt.Clear();for(int i=0;i<count;i++)dirt.Add(Dirt(new IntVec3((outside?20:42)+i%8,0,(outside?20:42)+i/8)));var job=JobMaker.MakeJob(JobDefOf.Clean);job.targetQueueA=dirt.Select(f=>new LocalTargetInfo(f)).ToList();p.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;}
    private bool Cleaned(){if(dirt.All(f=>f.Destroyed))return true;if(GuestDossiers.Now-start>4500)throw new Exception("Cleaning timed out: "+first.CurJob+" / "+second.CurJob);return false;}
    private void CheckIn(){Wait(guest);AccessTools.Field(HotelBillingHooks.Guest(guest).GetType(),"lastBedCheckTick").SetValue(HotelBillingHooks.Guest(guest),GuestDossiers.Now-100000);var giver=Activator.CreateInstance(AccessTools.TypeByName("Hospitality.JobGiver_ClaimBed"));var j=(Job)AccessTools.Method(giver.GetType(),"TryGiveJob").Invoke(giver,new object[]{guest});Check(j!=null&&j.targetA.Thing==bed,"Native hotel check-in unavailable");guest.jobs.StartJob(j,JobCondition.InterruptForced);start=GuestDossiers.Now;}
    private void Setup(){
        foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);map.fogGrid.ClearAllFog();
        foreach(var c in new CellRect(15,15,55,45)){foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant||t is Filth){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);map.areaManager.Home[c]=true;}
        var room=new CellRect(39,39,13,12);foreach(var c in room.EdgeCells)Build(c==new IntVec3(44,0,39)?"Door":"Wall",c);foreach(var c in room.ContractedBy(1))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var c in new CellRect(36,36,24,21))area[c]=true;
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(40,0,37));desk.area=area;desk.roomRate=4;desk.vipChance=0;
        bed=(Building_Bed)Build("BedGuest",new IntVec3(48,0,48));map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();desk.ApplyServiceAreas();
        first=Worker(new IntVec3(41,0,43),2);second=Worker(new IntVec3(41,0,45),10);
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));guest=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false));GenSpawn.Spawn(guest,new IntVec3(43,0,37),map);guest.inventory.innerContainer.ClearAndDestroyContents();var cash=ThingMaker.MakeThing(ThingDefOf.Silver);cash.stackCount=500;guest.inventory.innerContainer.TryAdd(cash);guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=1;
        foreach(var t in map.attackTargetsCache.TargetsHostileToFaction(faction).Select(t=>t.Thing).Where(t=>t.Spawned).ToArray())t.DeSpawn();
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(44,0,44),new List<Pawn>{guest},map,false,false,600000});guest.GetLord().ReceiveMemo("TravelArrived");guest.needs.joy.CurLevelPercentage=1;Wait(guest);
        initialSilver=Silver;Check(Ledger.received==0&&Ledger.allocated==0,"Historical income was imported");var loose=Dirt(new IntVec3(45,0,45));loose.ThinFilth();Check(Ledger.Account(first)==null,"Non-worker filth removal earned credit");
        Clean(first,8);stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    private void Fresh(){
        if(stage==0){Setup();return;}
        if(stage==1){if(!Cleaned())return;Wait(first);Check(Ledger.Account(first).workUnits==280000&&Book.Entry(first).credited==0&&Silver==initialSilver,"Cleaning created coins/credit without hotel receipts");
            interrupted=Dirt(new IntVec3(42,0,42));slowWork=interrupted.def.filth.cleaningWorkToReduceThickness;interrupted.def.filth.cleaningWorkToReduceThickness=10000;var job=JobMaker.MakeJob(JobDefOf.Clean);job.targetQueueA=new List<LocalTargetInfo>{interrupted};first.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;stage=2;return;}
        if(stage==2){var driver=first.jobs.curDriver as JobDriver_CleanFilth;float progress=driver==null?0:(float)AccessTools.Field(typeof(JobDriver_CleanFilth),"cleaningWorkDone").GetValue(driver);if(progress<=0){if(GuestDossiers.Now-start>1000)throw new Exception("No partial cleaning observed: destroyed="+interrupted.Destroyed+" job="+first.CurJob+" position="+first.Position);return;}Check(!interrupted.Destroyed&&interrupted.thickness==1,"Interrupted fixture completed too early");Wait(first);interrupted.def.filth.cleaningWorkToReduceThickness=slowWork;slowWork=0;Check(Ledger.Account(first).workUnits==280000,"Interrupted cleaning earned completed-work credit");interrupted.Destroy();CheckIn();stage=3;return;}
        if(stage==3){if(HotelBillingHooks.Bed(guest)!=bed){if(GuestDossiers.Now-start>2000)throw new Exception("Check-in timed out");return;}
            Wait(guest);Check(HotelBillingHooks.Money(guest)==496&&Silver==initialSilver+4&&desk.hotelIncome==4,"Room payment was not physical");Check(Ledger.received==4&&Ledger.allocated==1&&Ledger.Balance==3&&Book.Entry(first).credited==1&&Ledger.Account(first).workUnits==30000,"Room payment did not fund only completed cleaning");Ledger.Receive(bed.Position,4,Ledger.revenueReceipts.Single());Check(Ledger.received==4&&Ledger.allocated==1,"Duplicate hotel receipt funded twice");Clean(second,8);stage=4;return;}
        if(stage==4){if(!Cleaned())return;Wait(second);Check(Book.Entry(second).credited==1&&Ledger.Account(second).workUnits==30000&&Ledger.allocated==2&&Silver==initialSilver+4,"Second worker share created money or used wrong work amount");Clean(first,7);stage=5;return;}
        if(stage==5){if(!Cleaned())return;Wait(first);Ledger.Settle();Check(Book.Entry(first).credited==2&&Book.Entry(first).Remaining==0&&Ledger.Account(first).workUnits==0&&Ledger.allocated==3&&Ledger.Balance==1,"Target cap or completed account cleanup failed");Clean(second,1,true);stage=6;return;}
        if(stage==6){if(!Cleaned())return;Wait(second);Check(Ledger.Account(second).workUnits==30000&&Book.Entry(second).credited==1,"Colony cleaning outside complex earned hotel credit");saved=true;GameDataSaveLoader.SaveGame("TMM-Housekeeping");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Housekeeping")),"Ledger save missing");Finish(false);}
    }
    private void Reload(){
        if(stage==0){Check(saved&&first!=null&&second!=null&&desk!=null,"Saved housekeeping fixture missing");Check(Ledger.received==4&&Ledger.allocated==3&&Ledger.Balance==1&&Ledger.revenueReceipts.Count==1,"Funding/receipt state failed reload");Check(Book.Entry(first).credited==2&&Book.Entry(second).credited==1&&Ledger.Account(second).workUnits==30000,"Contract/fractional pending work failed reload");Ledger.Receive(bed.Position,4,Ledger.revenueReceipts[0]);Check(Ledger.received==4&&Ledger.allocated==3&&Silver==initialSilver+4,"Reload accepted duplicate hotel income");Clean(second,7);stage=1;return;}
        if(stage==1){if(!Cleaned())return;Wait(second);Check(Book.Entry(second).credited==2&&Ledger.allocated==4&&Ledger.Balance==0&&Silver==initialSilver+4,"Post-load fractional work not paid exactly once");Clean(second,8);stage=2;return;}
        if(stage==2){if(!Cleaned())return;Wait(second);Check(Book.Entry(second).credited==2&&Ledger.allocated==4&&Ledger.Account(second).workUnits==305000,"Cleaning exceeded real hotel funding");var bills=map.GetComponent<HotelBilling>();var stay=bills.stays.Single(s=>s.guest==guest);stay.nextChargeTick=GuestDossiers.Now;stay.remaining=-1;bills.ProcessPayments();bills.ProcessPayments();Check(HotelBillingHooks.Money(guest)==492&&Silver==initialSilver+8&&Ledger.received==8&&Ledger.allocated==5&&Book.Entry(second).credited==3,"New actual hotel payment did not settle pending cleaning once");long funded=Ledger.received,spent=Ledger.allocated;Ledger.Settle();Ledger.Settle();Check(Ledger.received==funded&&Ledger.allocated==spent&&Silver==initialSilver+8,"Repeated settlement generated credit/coins");Check(Book.Release(first)&&!Book.Entry(first).active,"Completed worker release failed");Ledger.Completed(first,bed.Position,1000);Check(Book.Entry(first).credited==2&&Ledger.allocated==spent,"Released worker earned further credit");Finish(true);}
    }
    public override void ExposeData(){base.ExposeData();Scribe_References.Look(ref first,"hkFirst");Scribe_References.Look(ref second,"hkSecond");Scribe_References.Look(ref guest,"hkGuest");Scribe_References.Look(ref desk,"hkDesk");Scribe_References.Look(ref bed,"hkBed");Scribe_Values.Look(ref saved,"hkSaved");Scribe_Values.Look(ref initialSilver,"hkInitialSilver");}
    public override void MapComponentUpdate(){bool reload=Environment.GetCommandLineArgs().Contains("-tmm-housekeeping-reload");if(done||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-housekeeping-probe")))return;try{if(reload)Reload();else Fresh();}catch(Exception e){error=e.ToString();Finish(reload);}}
    private void Finish(bool reload){if(slowWork>0&&interrupted!=null)interrupted.def.filth.cleaningWorkToReduceThickness=slowWork;done=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"HousekeepingReloadProbe.json":"HousekeepingProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"errors\":"+(error==null?"[]":"["+Q(error)+"]")+"}");}
    public override void MapComponentOnGUI(){if(ui||!done||first==null||Event.current.type!=EventType.Repaint)return;ui=true;string e=null;try{new Dialog_WorkBuyout(first).DoWindowContents(new Rect(0,0,610,480));}catch(Exception ex){e=ex.ToString();}File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"HousekeepingUiProbe.json"),"{\"passed\":"+(e==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(e==null?"null":Q(e))+"}");}
}
