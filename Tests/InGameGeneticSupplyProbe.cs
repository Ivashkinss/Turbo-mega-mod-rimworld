using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class GeneticSupplyProbeLaunch
{
    static GeneticSupplyProbeLaunch()
    {
        var args=Environment.GetCommandLineArgs();
        if(args.Contains("-tmm-genetic-supply-probe")||args.Contains("-tmm-genetic-supply-reload")||args.Contains("-tmm-genetic-supply-ingest-reload"))new Harmony("tmm.test.geneticSupply.temperature").Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(GeneticSupplyProbeLaunch),"Temp"));
        if(args.Contains("-tmm-genetic-supply-reload")||args.Contains("-tmm-genetic-supply-ingest-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame(args.Contains("-tmm-genetic-supply-ingest-reload")?"TMM-GeneticSupply-Ingest":"TMM-GeneticSupply"));
    }
    private static bool Temp(ref float __result){__result=21;return false;}
}
public sealed class GeneticSupplyProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building shelf,staffShelf;
    private Pawn guest,worker;
    private Thing loose,stock;
    private int stage,start,checks,orderId,price,beforeMoney,beforeFloor,nextTrace;
    private long beforeEarned;
    private float savedValue,savedIngestValue;
    private bool saved,loaded,reported,ui,ingestSaved,ingestLoaded;
    private readonly List<string> errors=new List<string>();
    public GeneticSupplyProbe(Map map):base(map){}
    private Gene_ChemicalDependency Gene {get{return guest.genes.GenesListForReading.OfType<Gene_ChemicalDependency>().First(g=>g.def.chemical.defName=="Psychite");}}
    private Hediff_ChemicalDependency Need {get{return Gene.LinkedHediff;}}
    private ThingDef Drug {get{return DefDatabase<ThingDef>.GetNamed("PsychiteTea");}}
    private DrugPolicy selectedPolicy;
    private Thing Personal(Pawn p){return p.inventory.innerContainer.FirstOrDefault(t=>t.def==Drug);}
    private ComplexSales Sales {get{return map.GetComponent<ComplexSales>();}}
    private SaleOrder Order {get{return Sales.ById(orderId);}}
    private int Money {get{return HotelBillingHooks.Money(guest);}}
    private int MapPacks {get{return map.listerThings.ThingsOfDef(Drug).Where(t=>t.Spawned).Sum(t=>t.stackCount);}}
    private int ColonySilver {get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount)+
        map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction==Faction.OfPlayer).Sum(p=>(p.inventory==null?0:p.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).Sum(t=>t.stackCount))+
        (p.carryTracker!=null&&p.carryTracker.CarriedThing!=null&&p.carryTracker.CarriedThing.def==ThingDefOf.Silver?p.carryTracker.CarriedThing.stackCount:0));}}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Escape(string t){return "\""+t.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void ResetSearch(){((Dictionary<int,int>)AccessTools.Field(typeof(GuestDependencySearch),"retry").GetValue(map.GetComponent<GuestDependencySearch>())).Clear();}
    private static void Wait(Pawn p){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
    private Job Native(Pawn p){return (Job)AccessTools.Method(typeof(JobGiver_SatifyChemicalDependency),"TryGiveJob").Invoke(new JobGiver_SatifyChemicalDependency(),new object[]{p});}
    private Thing Packs(int count,IntVec3 cell)
    {
        var t=ThingMaker.MakeThing(Drug);t.stackCount=count;GenSpawn.Spawn(t,cell,map);t.SetForbidden(false,false);return t;
    }
    private Building Build(string name,IntVec3 cell)
    {
        var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);
    }
    private void SetMoney(int amount)
    {
        foreach(var t in guest.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray())t.Destroy();
        if(amount>0){var cash=ThingMaker.MakeThing(ThingDefOf.Silver);cash.stackCount=amount;guest.inventory.innerContainer.TryAdd(cash);}
    }
    public override void ExposeData()
    {
        base.ExposeData();Scribe_References.Look(ref desk,"hpDesk");Scribe_References.Look(ref shelf,"hpShelf");Scribe_References.Look(ref staffShelf,"hpStaffShelf");
        Scribe_References.Look(ref guest,"hpGuest");Scribe_References.Look(ref worker,"hpWorker");Scribe_References.Look(ref loose,"hpLoose");Scribe_References.Look(ref stock,"hpStock");
        Scribe_Values.Look(ref orderId,"hpOrder");Scribe_Values.Look(ref price,"hpPrice");Scribe_Values.Look(ref beforeMoney,"hpMoney");Scribe_Values.Look(ref beforeFloor,"hpFloor");
        Scribe_Values.Look(ref beforeEarned,"hpEarned");Scribe_Values.Look(ref savedValue,"hpValue");Scribe_Values.Look(ref saved,"hpSaved");
        Scribe_Values.Look(ref ingestSaved,"hpIngestSaved");Scribe_Values.Look(ref savedIngestValue,"hpIngestValue");
        Scribe_References.Look(ref selectedPolicy,"gpPolicy");
        if(Scribe.mode==LoadSaveMode.PostLoadInit){loaded=saved&&Gene!=null&&Math.Abs((1f-Need.Severity)-savedValue)<0.00001f&&guest.drugs.CurrentPolicy[Drug].allowedForAddiction&&!desk.geneticDrugPurchases;
            ingestLoaded=ingestSaved&&Gene!=null&&Math.Abs((1f-Need.Severity)-savedIngestValue)<0.00001f&&guest.CurJobDef==JobDefOf.Ingest;}
    }
    private void Setup()
    {
        Check(GuestDutyTrees.PatchedCopies>0&&GuestDutyTrees.ExistingKeysPreserved,"Live duty copies not patched safely");
        var nodes=DefDatabase<ThinkTreeDef>.AllDefsListForReading.SelectMany(d=>d.thinkRoot.ThisAndChildrenRecursive).Distinct().ToList();
        var keys=nodes.ToDictionary(n=>n,n=>n.UniqueSaveKey);int added=GuestDutyTrees.AddedNodes;GuestDutyTrees.Synchronize();
        Check(GuestDutyTrees.AddedNodes==added&&keys.All(k=>k.Key.UniqueSaveKey==k.Value),"Repeated duty synchronization changed nodes or save keys");
        map.fogGrid.ClearAllFog();foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var c in new CellRect(35,35,35,25)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);
        }
        Area_Allowed area,staff;map.areaManager.TryMakeNewAllowed(out area);map.areaManager.TryMakeNewAllowed(out staff);
        foreach(var c in new CellRect(36,36,32,22)){area[c]=true;if(c.x>=54)staff[c]=true;}
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(41,0,44));desk.area=area;desk.staffArea=staff;desk.vipChance=0;desk.ApplyServiceAreas();
        shelf=Build("Shelf",new IntVec3(48,0,44));staffShelf=Build("Shelf",new IntVec3(61,0,44));
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        guest=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false));
        guest.genes.AddGene(DefDatabase<GeneDef>.GetNamed("ChemicalDependency_Psychite"),false);
        GenSpawn.Spawn(guest,new IntVec3(44,0,44),map);guest.inventory.innerContainer.ClearAndDestroyContents();SetMoney(1000);
        guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=1;
        foreach(var t in map.attackTargetsCache.TargetsHostileToFaction(faction).Select(t=>t.Thing).Where(t=>t.Spawned).ToArray())t.DeSpawn();
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(44,0,44),new List<Pawn>{guest},map,false,false,600000});guest.GetLord().ReceiveMemo("TravelArrived");guest.needs.joy.CurLevelPercentage=1;
        Check(GuestDeathrest.Visiting(guest)&&Gene!=null&&Gene.Active,"Fixture genetic dependency unavailable");
        Need.Severity=0.89999f;Check(!Need.ShouldSatify&&GuestGeneticSupply.MakeJob(guest)==null,"Native lower due boundary ignored");
        Need.Severity=0.9f;Check(Need.ShouldSatify,"Native exact due boundary rejected");Need.Severity=1.2f;
        Check(GuestGeneticSupply.PolicyAllows(guest,Drug),"Default policy unexpectedly blocked dependency dose");
        if(guest.drugs==null)guest.drugs=new Pawn_DrugPolicyTracker(guest);
        guest.drugs.CurrentPolicy=Current.Game.drugPolicyDatabase.MakeNewDrugPolicy();guest.drugs.CurrentPolicy[Drug].allowedForAddiction=true;
        selectedPolicy=Current.Game.drugPolicyDatabase.MakeNewDrugPolicy();selectedPolicy[Drug].allowedForAddiction=true;desk.dependencySupplyPolicy=selectedPolicy;
        guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(Drug,true);
        var own=ThingMaker.MakeThing(Drug);own.stackCount=1;guest.inventory.innerContainer.TryAdd(own);
        loose=Packs(6,new IntVec3(45,0,46));
        guest.drugs.CurrentPolicy[Drug].allowedForAddiction=false;Check(GuestGeneticSupply.MakeJob(guest)==null,"Disabled packs were consumed");guest.drugs.CurrentPolicy[Drug].allowedForAddiction=true;
        selectedPolicy[Drug].allowedForAddiction=false;Check(GuestGeneticSupply.MakeJob(guest)==null,"Additional policy bypassed for personal dose");selectedPolicy[Drug].allowedForAddiction=true;
        var removed=Current.Game.drugPolicyDatabase.MakeNewDrugPolicy();desk.dependencySupplyPolicy=removed;Current.Game.drugPolicyDatabase.AllPolicies.Remove(removed);Check(GuestGeneticSupply.SupplyPolicy(desk)==null,"Deleted supply policy retained");desk.dependencySupplyPolicy=selectedPolicy;
        var wrong=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("SmokeleafJoint"));Check(!GuestGeneticSupply.Matches(guest,Need,wrong),"Wrong chemical matched");wrong.Destroy();
        guest.needs.rest.CurLevelPercentage=0.1f;Check(GuestGeneticSupply.MakeJob(guest)==null,"Exhausted guest sought drug");guest.needs.rest.CurLevelPercentage=1;
        guest.needs.food.CurLevelPercentage=0.1f;Check(GuestGeneticSupply.MakeJob(guest)==null,"Hungry guest sought drug");guest.needs.food.CurLevelPercentage=1;
        var visit=new ClinicVisit{pawn=guest,lord=guest.GetLord()};map.GetComponent<ClinicGuests>().visits.Add(visit);Check(GuestGeneticSupply.MakeJob(guest)==null,"Clinical guest sought drug");map.GetComponent<ClinicGuests>().visits.Remove(visit);
        desk.geneticDrugPurchases=false;var job=Native(guest);Check(job!=null&&job.def==JobDefOf.Ingest&&job.targetA.Thing==own&&job.count==1&&job.takeExtraIngestibles==0,"Native guest giver did not restrict ingestion to personal stock");
        worker=map.mapPawns.FreeColonistsSpawned.First(p=>!p.WorkTypeIsDisabled(ComplexSales.SalesWork));
        worker.Position=new IntVec3(43,0,45);worker.Notify_Teleported();
        if(worker.genes.GetFirstGeneOfType<Gene_ChemicalDependency>()==null)worker.genes.AddGene(DefDatabase<GeneDef>.GetNamed("ChemicalDependency_Psychite"),false);
        var wg=worker.genes.GetFirstGeneOfType<Gene_ChemicalDependency>();wg.LinkedHediff.Severity=1.2f;worker.drugs.CurrentPolicy[Drug].allowedForAddiction=true;
        Check(!GuestDeathrest.Visiting(worker)&&Native(worker)!=null,"Player geneticSupply behavior was blocked");wg.LinkedHediff.Severity=0f;
        foreach(var p in map.mapPawns.FreeColonistsSpawned)p.workSettings.SetPriority(ComplexSales.SalesWork,0);
        guest.jobs.StartJob(job,JobCondition.InterruptForced);stage=1;start=GuestDossiers.Now;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    private void StartRequest(bool privateStock)
    {
        ResetSearch();Wait(guest);var job=guest.thinker.MainThinkNodeRoot.TryIssueJobPackage(guest,default(JobIssueParams)).Job;
        Check(job!=null&&job.def==(privateStock?ComplexSales.WaitJob:ComplexSales.BuyJob),"Native guest tree missed geneticSupply order: "+job);
        orderId=job.count;var o=Order;Check(o!=null&&o.count==1&&o.price==SaleStock.Price(SaleStock.UnitPrice(stock),1)&&o.delivery==privateStock,"Unexpected physical pack quote/count");
        price=o.price;beforeMoney=Money;beforeFloor=ColonySilver;beforeEarned=Sales.earned;
        guest.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;
    }
    private void StartWorker()
    {
        Wait(worker);var giver=(WorkGiver_Scanner)DefDatabase<WorkGiverDef>.GetNamed("TMM_ComplexSalesDelivery").Worker;
        Check(giver.HasJobOnThing(worker,desk),"Seller did not see pending pack order");
        var j=giver.JobOnThing(worker,desk);Check(j!=null&&j.count==orderId,"Seller lost order identity");worker.jobs.StartJob(j,JobCondition.InterruptForced);
    }
    private void Fresh()
    {
        if(stage==0){Setup();return;}
        if(stage==1){
            if(Need.Severity>=0.1f){if(GuestDossiers.Now-start>2500)throw new Exception("Personal ingest timed out: "+guest.CurJob);return;}
            Check(Personal(guest)==null&&Money==1000&&Sales.earned==0&&MapPacks==6,"Personal ingestion spent money or took map stock");
            Need.Severity=1.2f;desk.geneticDrugPurchases=false;ResetSearch();Check(Native(guest)==null&&Sales.orders.Count==0,"Guest stole loose colony pack or used unrestricted native fallback");
            stock=Packs(1,shelf.Position);shelf.GetComp<CompComplexSale>().forSale=true;desk.geneticDrugPurchases=true;
            guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(Drug,false);ResetSearch();Check(GuestGeneticSupply.MakeJob(guest)==null,"Purchase bypassed food policy");
            guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(Drug,true);SetMoney(0);ResetSearch();Check(GuestGeneticSupply.MakeJob(guest)==null,"Guest bought packs without money");
            SetMoney(1000);shelf.GetComp<CompComplexSale>().forSale=false;ResetSearch();Check(GuestGeneticSupply.MakeJob(guest)==null,"Unmarked colony shelf sold packs");shelf.GetComp<CompComplexSale>().forSale=true;
            var alcohol=(Gene_ChemicalDependency)guest.genes.AddGene(DefDatabase<GeneDef>.GetNamed("ChemicalDependency_Alcohol"),false);alcohol.LinkedHediff.Severity=1.0f;
            var beer=ThingMaker.MakeThing(ThingDefOf.Beer);guest.inventory.innerContainer.TryAdd(beer);guest.drugs.CurrentPolicy[beer.def].allowedForAddiction=true;selectedPolicy[beer.def].allowedForAddiction=true;
            var personal=GuestGeneticSupply.MakeJob(guest);Check(personal!=null&&personal.targetA.Thing==beer&&Sales.orders.Count==0,"Second dependency personal dose did not precede paid drug");beer.Destroy();guest.genes.RemoveGene(alcohol);
            selectedPolicy[Drug].allowedForAddiction=false;ResetSearch();Check(GuestGeneticSupply.MakeJob(guest)==null,"Additional policy bypassed for purchase");selectedPolicy[Drug].allowedForAddiction=true;
            StartRequest(false);stage=2;return;
        }
        if(stage==2){
            if(!ingestSaved&&guest.CurJobDef==JobDefOf.Ingest&&guest.CurJob.jobGiver is JobGiver_GuestGeneticSupply&&Need.Severity>=0.1f){
                ingestSaved=true;savedIngestValue=(1f-Need.Severity);GameDataSaveLoader.SaveGame("TMM-GeneticSupply-Ingest");
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-GeneticSupply-Ingest")),"Automatic ingest save missing");
            }
            if(Need.Severity>=0.1f){if(GuestDossiers.Now-start>3500)throw new Exception("Public purchase/ingest timed out: "+guest.CurJob+" value="+(1f-Need.Severity)+" money="+Money+" suitable="+SaleStock.Suitable(guest,stock));return;}
            Check(ingestSaved,"Purchased dose was not automatically issued by actual genetic giver");
            Check(Money==beforeMoney-price&&ColonySilver==beforeFloor+price&&Sales.earned==beforeEarned+price,"Public geneticSupply money failed conservation: money="+Money+" expected="+(beforeMoney-price)+" colonySilver="+ColonySilver+" expected="+(beforeFloor+price)+" earned="+Sales.earned+" expected="+(beforeEarned+price));
            Check(MapPacks==6&&Personal(guest)==null,"Protected colony quantity or owned consumption changed: map="+MapPacks+" personal="+Personal(guest));
            Need.Severity=1.2f;shelf.GetComp<CompComplexSale>().forSale=false;stock=Packs(1,staffShelf.Position);staffShelf.GetComp<CompComplexSale>().forSale=true;ResetSearch();
            Check(GuestGeneticSupply.MakeJob(guest)==null,"Staff packs offered without seller");
            Check(desk.SetRole(worker,ComplexRole.Seller),"Could not assign seller");desk.Employee(worker).mode=ComplexWorkMode.ColonyOnly;ResetSearch();
            Check(GuestGeneticSupply.MakeJob(guest)==null,"Colony-only seller accepted staff delivery");desk.Employee(worker).mode=ComplexWorkMode.Both;
            StartRequest(true);StartWorker();stage=3;return;
        }
        if(stage==3){
            if(Order==null||Order.goods==null){if(GuestDossiers.Now-start>2500)throw new Exception("Seller pickup timed out");return;}
            Check(Order.worker==worker&&worker.carryTracker.CarriedThing==Order.goods&&Order.goods.stackCount==1,"Pack courier lost physical cargo");
            Check(!desk.staffArea[guest.Position]&&guest.CurJobDef==ComplexSales.WaitJob,"Buyer entered staff stock instead of waiting");
            Check(Money==beforeMoney&&Sales.earned==beforeEarned,"Pending delivery charged early");
            desk.geneticDrugPurchases=false;saved=true;savedValue=(1f-Need.Severity);GameDataSaveLoader.SaveGame("TMM-GeneticSupply");
            Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-GeneticSupply")),"Pending geneticSupply save missing");Finish(false);
        }
    }
    private void Reload()
    {
        if(stage==0){
            Check(loaded,"Native gene/policy or reception preference failed load");
            Check(selectedPolicy!=null&&desk.dependencySupplyPolicy==selectedPolicy&&Current.Game.drugPolicyDatabase.AllPolicies.Contains(selectedPolicy),"Additional supply policy reference failed load");
            Check(Order!=null&&Order.goods!=null&&worker.carryTracker.CarriedThing==Order.goods&&guest.CurJobDef==ComplexSales.WaitJob&&guest.CurJob.count==orderId,"Pending courier/order references failed load");
            Check(Money==beforeMoney&&Sales.earned==beforeEarned,"Loading created an early payment");start=GuestDossiers.Now;stage=1;
        }
        if(stage==1){
            if(Need.Severity>=0.1f){if(GuestDossiers.Now-start>3500)throw new Exception("Loaded delivery/ingest timed out: "+guest.CurJob+" value="+(1f-Need.Severity));return;}
            Check(!desk.geneticDrugPurchases&&Money==beforeMoney-price&&ColonySilver==beforeFloor+price&&Sales.earned==beforeEarned+price,"Accepted delivery was cancelled by purchase toggle or payment failed");
            long earned=Sales.earned;int money=Money;Sales.Process();Sales.Process();Check(Sales.earned==earned&&Money==money,"Repeated processing duplicated geneticSupply payment");
            Check(GuestDossiers.Book.Find(guest.GetUniqueLoadID()).sales==2*price&&MapPacks==6,"Dossier sale income or protected colony stock incorrect");
            // A separate unpaid order is cancelled while its actual courier carries it.
            Need.Severity=1.2f;stock=Packs(1,staffShelf.Position);desk.geneticDrugPurchases=true;StartRequest(true);desk.geneticDrugPurchases=false;StartWorker();stage=2;return;
        }
        if(stage==2){
            if(Order==null||Order.goods==null){if(GuestDossiers.Now-start>2500)throw new Exception("Cancellation courier pickup timed out");return;}
            var o=Order;worker.drafter.Drafted=true;Sales.Process();
            int packs=map.listerThings.ThingsOfDef(Drug).Where(t=>t.Spawned).Sum(t=>t.stackCount)+
                guest.inventory.innerContainer.Where(t=>t.def==Drug).Sum(t=>t.stackCount)+
                (worker.carryTracker.CarriedThing!=null&&worker.carryTracker.CarriedThing.def==Drug?worker.carryTracker.CarriedThing.stackCount:0)+
                Sales.orders.Sum(x=>x.returnedGoods.Where(t=>t.def==Drug).Sum(t=>t.stackCount));
            Check(o.cancelled&&!o.completed&&Money==beforeMoney&&Sales.earned==beforeEarned,"Cancelled pack delivery created payment");
            Check(packs==7,"Cancelled pack cargo lost or duplicated: "+packs);Finish(true);
        }
    }
    private void IngestReload()
    {
        if(stage==0){
            Check(ingestLoaded&&guest.CurJob.jobGiver is JobGiver_GuestGeneticSupply,"Automatic ingest/giver did not restore");
            var job=guest.CurJob;var lookup=new object[]{job.jobGiver.UniqueSaveKey,null};
            Check(job.jobGiverThinkTree!=null&&(bool)AccessTools.Method(typeof(ThinkTreeDef),"TryGetThinkNodeWithSaveKey").Invoke(job.jobGiverThinkTree,lookup)&&lookup[1]==job.jobGiver,"New duty key does not resolve to original live node after reload");
            Check(Money==beforeMoney-price&&Sales.earned==beforeEarned+price,"Ingest reload altered paid sale");start=GuestDossiers.Now;stage=1;
        }
        if(Need.Severity>=0.1f){if(GuestDossiers.Now-start>2500)throw new Exception("Restored native ingest did not finish");return;}
        Check(Personal(guest)==null&&Money==beforeMoney-price,"Restored ingest left owned cargo or charged again");
        Check(Sales.earned==beforeEarned+price&&ColonySilver==beforeFloor+price,"Restored ingest duplicated receipt");Finish(true);
    }
    public override void MapComponentUpdate()
    {
        bool ingest=Environment.GetCommandLineArgs().Contains("-tmm-genetic-supply-ingest-reload");
        bool reload=ingest||Environment.GetCommandLineArgs().Contains("-tmm-genetic-supply-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-genetic-supply-probe")))return;
        try{
            if(guest!=null&&GuestDossiers.Now>=nextTrace){nextTrace=GuestDossiers.Now+100;
                File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"GeneticSupplyTrace.log"),GuestDossiers.Now+" stage="+stage+" job="+guest.CurJob+" gene="+(1f-Need.Severity)+" allowed="+guest.drugs.CurrentPolicy[Drug].allowedForAddiction+" personal="+Personal(guest)+" inv="+string.Join(";",guest.inventory.innerContainer.Select(t=>t.ToString()))+" managed="+GuestDeathrest.Visiting(guest)+" fit="+GuestGeneticSupply.Fit(guest)+" lord="+guest.GetLord()+" duty="+guest.mindState.duty+" pos="+guest.Position+" earned="+Sales.earned+" money="+Money+Environment.NewLine);
            }
            if(ingest)IngestReload();else if(reload)Reload();else Fresh();}catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    public override void MapComponentOnGUI()
    {
        if(ui||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-genetic-supply-probe"))return;
        ui=true;string error=null;try{new Dialog_GuestNeeds(desk).DoWindowContents(new Rect(10,10,880,710));new Dialog_ComplexServices(desk){page=1}.DoWindowContents(new Rect(10,10,740,720));}catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"GeneticSupplyUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,Environment.GetCommandLineArgs().Contains("-tmm-genetic-supply-ingest-reload")?"GeneticSupplyIngestReloadProbe.json":reload?"GeneticSupplyReloadProbe.json":"GeneticSupplyProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
}
