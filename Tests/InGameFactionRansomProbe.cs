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
public static class FactionRansomLaunch
{
    // Fault injection only controls GenPlace availability; the escrow, coins,
    // placement, Scribe and release all use the real production implementation.
    public static int dropMode,allowedSilver=-1;
    static FactionRansomLaunch(){
        var args=Environment.GetCommandLineArgs();if(!args.Any(a=>a=="-tmm-ransom-probe"||a=="-tmm-ransom-reload"))return;
        var h=new Harmony("tmm.test.ransom.placement");
        h.Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(FactionRansomLaunch),"Temp"));
        foreach(var m in typeof(GenPlace).GetMethods().Where(m=>m.Name=="TryPlaceThing"&&m.ReturnType==typeof(bool)&&m.GetParameters()[0].ParameterType==typeof(Thing)))h.Patch(m,prefix:new HarmonyMethod(typeof(FactionRansomLaunch),"Place"));
        if(args.Contains("-tmm-ransom-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-FactionRansom"));
    }
    private static bool Temp(ref float __result){__result=21;return false;}
    private static bool Place(Thing __0,ref bool __result){if(__0.def!=ThingDefOf.Silver||dropMode==2)return true;if(dropMode==1){if(allowedSilver<0)allowedSilver=__0.thingIDNumber;if(allowedSilver==__0.thingIDNumber)return true;}__result=false;return false;}
}
public sealed class FactionRansomProbe:MapComponent
{
    private Pawn accepted,refused,deceived;private Faction home;private Building_ComplexDesk desk;
    private bool done,ui,saved,afterDeception;private int checks,stage,start,initialSilver,deceptionTick;private string error;
    public FactionRansomProbe(Map map):base(map){}
    private FactionRansoms Book{get{return Current.Game.GetComponent<FactionRansoms>();}}
    private WorkBuyouts Labour{get{return Current.Game.GetComponent<WorkBuyouts>();}}
    private int Floor{get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private static void Wait(Pawn p){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
    private Building Build(string name,IntVec3 cell){var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);}
    private Pawn Slave(bool yes,IntVec3 cell,bool workerYes=false){
        Pawn p=null;for(int i=0;i<256;i++){
            var c=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,home,forceGenerateNewPawn:true,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false));
            if(FactionRansoms.WillFactionAccept(c)==yes&&(!workerYes||WorkContracts.WillAccept(c))){p=c;break;}c.Destroy();
        }
        if(p==null)throw new Exception("Could not generate response fixture");GenSpawn.Spawn(p,cell,map);p.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);
        foreach(var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)p.workSettings.SetPriority(w,0);p.inventory.innerContainer.ClearAndDestroyContents();var steel=ThingMaker.MakeThing(ThingDefOf.Steel);steel.stackCount=3;p.inventory.innerContainer.TryAdd(steel);p.needs.food.CurLevelPercentage=1;p.needs.rest.CurLevelPercentage=1;Wait(p);return p;
    }
    private void Setup(){
        map.fogGrid.ClearAllFog();foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var cell in new CellRect(0,0,65,65)){foreach(var t in cell.GetThingList(map).ToArray())if(t is Building||t is Plant||t is Filth){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);map.roofGrid.SetRoof(cell,null);}
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var cell in new CellRect(36,36,24,21))area[cell]=true;
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(40,0,37));desk.area=area;desk.vipChance=0;map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();desk.ApplyServiceAreas();
        home=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        accepted=Slave(true,new IntVec3(41,0,43));refused=Slave(false,new IntVec3(44,0,46),true);deceived=Slave(true,new IntVec3(48,0,43));
        initialSilver=Floor;var old=Labour.Begin(accepted,1200);Labour.Credit(accepted,"legacy-earned",100,false);
        FactionRansomLaunch.dropMode=1;var a=FactionRansoms.Offer(accepted,9999);
        Check(a!=null&&a.agreed&&a.targetAmount==1200&&a.originalFaction==home,"Legacy terms or accepted response changed");
        Check(a.placedAmount==500&&a.EscrowCoins==700&&a.TotalFundedCoins==1200&&Floor==initialSilver+500&&Book.totalRansomIncome==500,"Native partial escrow/physical coin accounting incorrect");
        Check(!old.active&&old.target==1200&&old.credited==100&&old.receipts.Contains("legacy-earned"),"Accepted ransom lost legacy history");
        Labour.Credit(accepted,"blocked-credit",500,false);Check(old.credited==100&&!old.receipts.Contains("blocked-credit")&&!WorkContracts.Working(old),"Accepted ransom still earned legacy credit");
        Check(!Labour.Release(accepted)&&WorkContracts.Offer(accepted,1)==null&&!FactionRansoms.Deceive(accepted)&&!FactionRansoms.Release(accepted),"Incomplete payment permitted bypass or deception/release");
        Check(FactionRansoms.Offer(accepted,1)==a&&a.targetAmount==1200&&a.TotalFundedCoins==1200&&Floor==initialSilver+500,"Duplicate offer generated coins or changed quote");
        var before=a.EscrowCoins;Check(a.ProcessEscrow(null,null)==0&&a.EscrowCoins==before,"Null map redirected pending money");var original=a.map;a.map=null;Check(a.ProcessEscrow(map,desk)==0&&a.EscrowCoins==before,"Lost transaction map fell back to a foreign map");a.map=original;
        var b=Labour.Begin(refused,1600);Labour.Credit(refused,"before-refusal",200,false);var no=FactionRansoms.Offer(refused,9999);
        Check(no!=null&&!no.agreed&&no.targetAmount==1600&&b.active&&b.credited==200&&b.receipts.Contains("before-refusal"),"Faction refusal altered old obligation");
        Check(FactionRansoms.Offer(refused,1)==no&&no.targetAmount==1600&&Floor==initialSilver+500,"Faction refusal rerolled or paid");
        var worker=WorkContracts.Offer(refused,1);Check(worker==b&&worker.freeContract&&worker.active&&worker.target==1600&&worker.credited==200&&!refused.IsSlave,"Refusal did not preserve free labour path and locked terms");
        FactionRansomLaunch.dropMode=0;var c=FactionRansoms.Offer(deceived,350);Check(c!=null&&c.agreed&&c.EscrowCoins==350&&c.placedAmount==0&&!c.IsFullyPaid,"Blocked new payment was lost or counted");
        Book.ProcessPendingEscrow(map);Check(Book.totalRansomIncome==500&&Floor==initialSilver+500&&a.EscrowCoins+c.EscrowCoins==1050,"Blocked retry counted phantom income");
        var pos=desk.Position;desk.DeSpawn();Book.ProcessPendingEscrow(map);Check(a.EscrowCoins+c.EscrowCoins==1050,"Reception despawn destroyed pending money");GenSpawn.Spawn(desk,pos,map);desk.area=area;
        Check(map.GetComponent<HousekeepingLedger>().received==0&&desk.hotelIncome==0,"Faction money entered hotel/cleaning budget");
        saved=true;GameDataSaveLoader.SaveGame("TMM-FactionRansom");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-FactionRansom")),"Partial-escrow save missing");Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;Finish(false);
    }
    private void Reload(){
        var a=Book.Entry(accepted);var c=Book.Entry(deceived);
        if(!afterDeception){
            Check(saved&&a!=null&&c!=null&&a.originalFaction==home&&a.map==map&&a.targetAmount==1200&&a.EscrowCoins==700&&c.EscrowCoins==350,"Pending escrow/terms/faction failed actual reload");
            Check(Book.totalRansomIncome==500&&Floor==initialSilver+500&&!Labour.Entry(accepted).active&&Labour.Entry(accepted).credited==100,"Payment or legacy state failed reload");
            Check(Labour.Entry(refused).freeContract&&!refused.IsSlave&&Labour.Entry(refused).credited==200&&!Book.Entry(refused).agreed,"Refused faction/free worker failed reload");
            Check(FactionRansoms.Offer(accepted,1)==a&&a.TotalFundedCoins==1200,"Reload duplicate payment mutated terms");
            FactionRansomLaunch.dropMode=2;Book.ProcessPendingEscrow(map);Book.ProcessPendingEscrow(map);
            Check(a.IsFullyPaid&&c.IsFullyPaid&&a.EscrowCoins+c.EscrowCoins==0&&Book.totalRansomIncome==1550&&Floor==initialSilver+1550,"Actual post-load receipt retry lost/duplicated coins");
            Check(map.GetComponent<HousekeepingLedger>().received==0&&desk.hotelIncome==0,"Retry money leaked to room funding");
            deceived.guest.SetGuestStatus(null,GuestStatus.Guest);deceived.SetFaction(Faction.OfPlayer);
            Check(!FactionRansoms.Deceive(deceived)&&!FactionRansoms.Release(deceived)&&!c.deceived&&!c.released,"Manually recruited pawn was forcibly held/released");
            deceived.SetFaction(home);deceived.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);Wait(deceived);
            var walls=new List<Building>();foreach(var cell in new CellRect(accepted.Position.x-1,accepted.Position.z-1,3,3).EdgeCells){foreach(var t in cell.GetThingList(map).ToArray())if(t is Building||t is Plant||t is Filth)t.Destroy();walls.Add(Build("Wall",cell));}map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            Check(!FactionRansoms.Release(accepted)&&!a.released&&accepted.IsSlaveOfColony,"Blocked exit falsely marked release");foreach(var w in walls)w.Destroy();map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            var operation=new Bill_Medical(DefDatabase<RecipeDef>.GetNamed("Anesthetize"),null);accepted.BillStack.AddBill(operation);Check(!FactionRansoms.Release(accepted)&&accepted.BillStack.Bills.Contains(operation),"Release erased pending operation");accepted.BillStack.Clear();
            int goodwill=home.GoodwillWith(Faction.OfPlayer);Check(FactionRansoms.Deceive(deceived)&&c.deceived&&c.State==FactionRansomState.DeceivedHeld&&home.GoodwillWith(Faction.OfPlayer)==goodwill,"Deception/history or no-goodwill-change guard failed");
            deceptionTick=c.deceptionTick;Check(!FactionRansoms.Deceive(deceived)&&c.deceptionTick==deceptionTick&&Book.totalRansomIncome==1550,"Repeat deception changed history/income");
            afterDeception=true;GameDataSaveLoader.SaveGame("TMM-FactionRansom");Finish(true);return;
        }
        if(stage==0){
            Check(c.deceived&&c.deceptionTick==deceptionTick&&c.originalFaction==home&&c.targetAmount==350&&c.IsFullyPaid&&!c.released,"Deception history failed actual load");
            Check(a.IsFullyPaid&&!a.deceived&&!a.released&&Book.totalRansomIncome==1550&&Floor==initialSilver+1550,"Paid pending release failed actual load");
            var cargo=ThingMaker.MakeThing(ThingDefOf.Steel);cargo.stackCount=5;GenSpawn.Spawn(cargo,accepted.Position,map);Check(accepted.carryTracker.TryStartCarry(cargo,5,false)==5,"Release cargo fixture failed");
            Check(FactionRansoms.Release(accepted)&&a.released&&!a.deceived&&accepted.Faction==home,"Honest native release failed");
            Check(cargo.Spawned&&cargo.stackCount==5&&accepted.carryTracker.CarriedThing==null&&accepted.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Steel).Sum(t=>t.stackCount)==3,"Native release lost held cargo/personal inventory");
            Check(FactionRansoms.Release(deceived)&&c.released&&c.deceived&&c.deceptionTick==deceptionTick&&deceived.Faction==home,"Late honest release erased prior deception");
            Check(!FactionRansoms.Release(accepted)&&!FactionRansoms.Release(deceived)&&Book.totalRansomIncome==1550&&Floor==initialSilver+1550,"Repeat release paid or ran twice");
            start=GuestDossiers.Now;stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;return;
        }
        if(accepted.Spawned||deceived.Spawned){if(GuestDossiers.Now-start>8000)throw new Exception("Native released pawns did not actually leave");return;}
        Check(a.released&&c.released&&c.deceived&&accepted.Faction==home&&deceived.Faction==home,"Real map exit lost history or original faction");Finish(true);
    }
    public override void ExposeData(){base.ExposeData();Scribe_References.Look(ref accepted,"rpAccepted");Scribe_References.Look(ref refused,"rpRefused");Scribe_References.Look(ref deceived,"rpDeceived");Scribe_References.Look(ref home,"rpHome");Scribe_References.Look(ref desk,"rpDesk");Scribe_Values.Look(ref saved,"rpSaved");Scribe_Values.Look(ref afterDeception,"rpAfterDeception");Scribe_Values.Look(ref initialSilver,"rpFloor");Scribe_Values.Look(ref deceptionTick,"rpDeceptionTick");}
    public override void MapComponentUpdate(){bool reload=Environment.GetCommandLineArgs().Contains("-tmm-ransom-reload");if(done||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-ransom-probe")))return;try{if(reload)Reload();else Setup();}catch(Exception e){error=e.ToString();Finish(reload);}}
    private void Finish(bool reload){done=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"FactionRansomReloadProbe.json":"FactionRansomProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"afterDeception\":"+(afterDeception?"true":"false")+",\"errors\":"+(error==null?"[]":"["+Q(error)+"]")+"}");}
    public override void MapComponentOnGUI(){if(ui||!done||accepted==null||Event.current.type!=EventType.Repaint)return;ui=true;string e=null;try{new Dialog_WorkBuyout(accepted).DoWindowContents(new Rect(0,0,660,610));new Dialog_WorkBuyout(refused).DoWindowContents(new Rect(0,0,660,610));}catch(Exception x){e=x.ToString();}File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"FactionRansomUiProbe.json"),"{\"passed\":"+(e==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(e==null?"null":Q(e))+"}");}
}
