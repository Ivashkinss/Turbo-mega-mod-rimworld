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
public static class AddictionProbeLaunch {
    static AddictionProbeLaunch(){var args=Environment.GetCommandLineArgs();if(args.Contains("-tmm-addiction-probe")||args.Contains("-tmm-addiction-reload"))new Harmony("tmm.test.addiction.temp").Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(AddictionProbeLaunch),"Temp"));if(args.Contains("-tmm-addiction-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-Addiction"));}
    private static bool Temp(ref float __result){__result=21;return false;}
}
public sealed class AddictionProbe : MapComponent {
    private Pawn guest,worker;private Building_ComplexDesk desk;private Building shelf,staffShelf;private DrugPolicy policy;
    private SaleOrder activeOrder;
    private int stage,start,checks,orderId,price,beforeMoney,beforeSilver,savedMoney;private long beforeEarned,savedEarned;private bool done,ui,saved;private string error;
    public AddictionProbe(Map map):base(map){}
    private Need_Chemical Need {get{return guest.needs.AllNeeds.OfType<Need_Chemical>().Single(n=>n.def.defName=="Chemical_Alcohol");}}
    private Hediff_Addiction Addiction {get{return guest.health.hediffSet.hediffs.OfType<Hediff_Addiction>().Single(h=>h.def.defName=="AlcoholAddiction");}}
    private ComplexSales Sales {get{return map.GetComponent<ComplexSales>();}}
    private SaleOrder Order {get{return Sales.ById(orderId);}}
    private int Money {get{return HotelBillingHooks.Money(guest);}}
    private int Silver {get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount)+map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction==Faction.OfPlayer).Sum(p=>(p.inventory==null?0:p.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).Sum(t=>t.stackCount))+(p.carryTracker!=null&&p.carryTracker.CarriedThing!=null&&p.carryTracker.CarriedThing.def==ThingDefOf.Silver?p.carryTracker.CarriedThing.stackCount:0));}}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private static void Wait(Pawn p){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
    private void Reset(){((Dictionary<int,int>)AccessTools.Field(typeof(GuestAddictionSearch),"retry").GetValue(map.GetComponent<GuestAddictionSearch>())).Clear();}
    private Job Native(Pawn p){return (Job)AccessTools.Method(typeof(JobGiver_SatisfyChemicalNeed),"TryGiveJob").Invoke(new JobGiver_SatisfyChemicalNeed(),new object[]{p});}
    private Building Build(string name,IntVec3 cell){var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);}
    private Thing Beer(int count,IntVec3 cell){var t=ThingMaker.MakeThing(ThingDefOf.Beer);t.stackCount=count;GenSpawn.Spawn(t,cell,map);t.SetForbidden(false,false);return t;}
    private void Cash(int count){foreach(var t in guest.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray())t.Destroy();if(count>0){var t=ThingMaker.MakeThing(ThingDefOf.Silver);t.stackCount=count;guest.inventory.innerContainer.TryAdd(t);}}
    private void HungryAgain(){Wait(guest);Need.CurLevel=0.05f;guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=1;foreach(var h in guest.health.hediffSet.hediffs.Where(h=>h.def.defName=="AlcoholHigh"||h.def.defName=="Hangover").ToArray())guest.health.RemoveHediff(h);Reset();}
    private void Setup(){
        Check(GuestAreaCacheHooks.Enabled&&GuestDutyTrees.PatchedCopies>0&&GuestDutyTrees.ExistingKeysPreserved,"Area/duty hooks unavailable");
        map.fogGrid.ClearAllFog();foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var c in new CellRect(35,35,35,25)){foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);}
        Area_Allowed area,staff;map.areaManager.TryMakeNewAllowed(out area);map.areaManager.TryMakeNewAllowed(out staff);foreach(var c in new CellRect(36,36,32,22)){area[c]=true;if(c.x>=54)staff[c]=true;}
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(41,0,44));desk.area=area;desk.staffArea=staff;desk.vipChance=0;desk.ApplyServiceAreas();shelf=Build("Shelf",new IntVec3(48,0,44));staffShelf=Build("Shelf",new IntVec3(61,0,44));
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        guest=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false));GenSpawn.Spawn(guest,new IntVec3(44,0,44),map);guest.inventory.innerContainer.ClearAndDestroyContents();Cash(1000);guest.health.AddHediff(DefDatabase<HediffDef>.GetNamed("AlcoholAddiction")).Severity=0.4f;
        foreach(var t in map.attackTargetsCache.TargetsHostileToFaction(faction).Select(t=>t.Thing).Where(t=>t.Spawned).ToArray())t.DeSpawn();
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(44,0,44),new List<Pawn>{guest},map,false,false,600000});guest.GetLord().ReceiveMemo("TravelArrived");guest.needs.joy.CurLevelPercentage=1;HungryAgain();
        policy=Current.Game.drugPolicyDatabase.MakeNewDrugPolicy();policy[ThingDefOf.Beer].allowedForAddiction=true;desk.dependencySupplyPolicy=policy;guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingDefOf.Beer,true);
        worker=map.mapPawns.FreeColonistsSpawned.First(p=>!p.WorkTypeIsDisabled(ComplexSales.SalesWork));worker.Position=new IntVec3(43,0,45);worker.Notify_Teleported();foreach(var p in map.mapPawns.FreeColonistsSpawned)p.workSettings.SetPriority(ComplexSales.SalesWork,0);
        var own=ThingMaker.MakeThing(ThingDefOf.Beer);guest.inventory.innerContainer.TryAdd(own);Beer(5,new IntVec3(45,0,46));desk.ordinaryDrugPurchases=false;
        Need.CurLevel=1;Check(!GuestAddictionSupply.Due(guest).Any()&&Native(guest)==null,"Satisfied addiction sought a dose");Need.CurLevel=0.1f;Check(GuestAddictionSupply.Due(guest).Any(),"Exact desire boundary rejected");Need.CurLevel=0.05f;
        policy[ThingDefOf.Beer].allowedForAddiction=false;Check(Native(guest)==null,"Policy bypassed for own dose");policy[ThingDefOf.Beer].allowedForAddiction=true;
        guest.needs.rest.CurLevelPercentage=0.1f;Check(Native(guest)==null,"Exhausted guest sought dose");guest.needs.rest.CurLevelPercentage=1;
        var visit=new ClinicVisit{pawn=guest,lord=guest.GetLord()};map.GetComponent<ClinicGuests>().visits.Add(visit);Check(Native(guest)==null,"Clinical guest sought dose");map.GetComponent<ClinicGuests>().visits.Remove(visit);
        var job=Native(guest);Check(job!=null&&job.def==JobDefOf.Ingest&&job.targetA.Thing==own&&job.count==1&&job.takeExtraIngestibles==0,"Personal/native dose wrong");guest.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    private void Request(bool delivery){Reset();var pack=guest.thinker.MainThinkNodeRoot.TryIssueJobPackage(guest,default(JobIssueParams));var job=pack.Job;Check(job!=null&&job.def==(delivery?ComplexSales.WaitJob:ComplexSales.BuyJob),"Real guest tree did not select addiction sale");orderId=job.count;activeOrder=Order;price=Order.price;Check(Order.count==1&&Order.delivery==delivery,"Wrong dose/order kind");beforeMoney=Money;beforeSilver=Silver;beforeEarned=Sales.earned;guest.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;}
    private void Fresh(){
        if(stage==0){Setup();return;}
        if(stage==1){if(Need.CurLevel<=0.1f){if(GuestDossiers.Now-start>2500)throw new Exception("Own dose timeout");return;}
            Check(Addiction.Severity>0.59f,"Dose incorrectly cured/did not update native addiction");Check(Money==1000&&Sales.earned==0&&!guest.inventory.innerContainer.Any(t=>t.def==ThingDefOf.Beer),"Personal dose changed cash or was not consumed");
            HungryAgain();Check(Native(guest)==null&&Sales.orders.Count==0,"Unpaid loose colony stock taken");Beer(1,shelf.Position);shelf.GetComp<CompComplexSale>().forSale=true;desk.ordinaryDrugPurchases=true;
            Cash(0);Check(Native(guest)==null,"Dose bought without money");Cash(1000);Reset();guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingDefOf.Beer,false);Check(Native(guest)==null,"Food restriction bypassed");guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingDefOf.Beer,true);Reset();
            map.GetComponent<ComplexPricing>().salePercent=200;Request(false);Check(price==SaleStock.Price((double)ThingDefOf.Beer.GetStatValueAbstract(StatDefOf.MarketValue)*2,1),"Dose ignored shop percentage");stage=2;return;
        }
        if(stage==2){
            if(!saved&&guest.CurJobDef==JobDefOf.Ingest&&guest.CurJob.jobGiver is JobGiver_GuestAddictionSupply&&Need.CurLevel<=0.1f){saved=true;desk.ordinaryDrugPurchases=false;savedMoney=Money;savedEarned=Sales.earned;GameDataSaveLoader.SaveGame("TMM-Addiction");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Addiction")),"Active ingest save missing");}
            if(Need.CurLevel<=0.1f){if(GuestDossiers.Now-start>3000)throw new Exception("Paid public dose timeout: "+guest.CurJob);return;}
            Check(saved,"Automatic addiction job/giver not observed");Check(Money==beforeMoney-price&&Sales.earned==beforeEarned+price&&Silver==beforeSilver+price,"Public dose money not conserved");
            HungryAgain();shelf.GetComp<CompComplexSale>().forSale=false;Beer(1,staffShelf.Position);staffShelf.GetComp<CompComplexSale>().forSale=true;desk.ordinaryDrugPurchases=true;
            Check(Native(guest)==null,"Staff dose offered without seller");Check(desk.SetRole(worker,ComplexRole.Seller),"Seller role rejected");desk.Employee(worker).mode=ComplexWorkMode.ColonyOnly;Reset();Check(Native(guest)==null,"Colony-only seller supplied dose");desk.Employee(worker).mode=ComplexWorkMode.Both;
            Request(true);var giver=(WorkGiver_Scanner)DefDatabase<WorkGiverDef>.GetNamed("TMM_ComplexSalesDelivery").Worker;Check(giver.HasJobOnThing(worker,desk),"Seller has no delivery");worker.jobs.StartJob(giver.JobOnThing(worker,desk),JobCondition.InterruptForced);stage=3;return;
        }
        if(stage==3){if(Need.CurLevel<=0.1f){if(GuestDossiers.Now-start>3500)throw new Exception("Staff dose timeout: "+guest.CurJob);return;}
            Check(activeOrder!=null&&activeOrder.completed&&Money==beforeMoney-price&&Sales.earned==beforeEarned+price&&Silver==beforeSilver+price,"Staff payment not conserved");Check(!desk.staffArea[guest.Position],"Guest entered staff stock");long earned=Sales.earned;Sales.Process();Sales.Process();Check(Sales.earned==earned,"Repeated processing created money");Check(Addiction!=null,"Dose removed addiction");Finish(false);
        }
    }
    private void Reload(){if(stage==0){Check(saved&&guest.CurJobDef==JobDefOf.Ingest&&guest.CurJob.jobGiver is JobGiver_GuestAddictionSupply,"Active dose/giver failed load");var job=guest.CurJob;var args=new object[]{job.jobGiver.UniqueSaveKey,null};Check(job.jobGiverThinkTree!=null&&(bool)AccessTools.Method(typeof(ThinkTreeDef),"TryGetThinkNodeWithSaveKey").Invoke(job.jobGiverThinkTree,args)&&args[1]==job.jobGiver,"Saved duty key failed resolution");Check(!desk.ordinaryDrugPurchases&&desk.dependencySupplyPolicy==policy&&policy[ThingDefOf.Beer].allowedForAddiction,"Preference/policy failed load");Check(Money==savedMoney&&Sales.earned==savedEarned,"Load changed money");stage=1;start=GuestDossiers.Now;}
        if(Need.CurLevel<=0.1f){if(GuestDossiers.Now-start>2500)throw new Exception("Loaded dose timeout");return;}Check(Addiction!=null&&Money==savedMoney&&Sales.earned==savedEarned,"Loaded ingestion cured addiction or charged again");Finish(true);}
    public override void ExposeData(){base.ExposeData();Scribe_References.Look(ref guest,"adGuest");Scribe_References.Look(ref worker,"adWorker");Scribe_References.Look(ref desk,"adDesk");Scribe_References.Look(ref shelf,"adShelf");Scribe_References.Look(ref staffShelf,"adStaff");Scribe_References.Look(ref policy,"adPolicy");Scribe_Values.Look(ref saved,"adSaved");Scribe_Values.Look(ref savedMoney,"adMoney");Scribe_Values.Look(ref savedEarned,"adEarned");}
    public override void MapComponentUpdate(){bool reload=Environment.GetCommandLineArgs().Contains("-tmm-addiction-reload");if(done||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-addiction-probe")))return;try{if(reload)Reload();else Fresh();}catch(Exception e){error=e.ToString();Finish(reload);}}
    private void Finish(bool reload){done=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"AddictionReloadProbe.json":"AddictionProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"errors\":"+(error==null?"[]":"["+Q(error)+"]")+"}");}
    public override void MapComponentOnGUI(){if(ui||!done||desk==null||Event.current.type!=EventType.Repaint)return;ui=true;string e=null;try{new Dialog_ComplexServices(desk){page=1}.DoWindowContents(new Rect(0,0,740,720));}catch(Exception ex){e=ex.ToString();}File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"AddictionUiProbe.json"),"{\"passed\":"+(e==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(e==null?"null":Q(e))+"}");}
}
