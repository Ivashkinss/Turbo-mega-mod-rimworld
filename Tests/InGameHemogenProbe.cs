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
public static class HemogenProbeLaunch
{
    static HemogenProbeLaunch()
    {
        var args=Environment.GetCommandLineArgs();
        if(args.Contains("-tmm-hemogen-probe")||args.Contains("-tmm-hemogen-reload")||args.Contains("-tmm-hemogen-ingest-reload"))new Harmony("tmm.test.hemogen.temperature").Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(HemogenProbeLaunch),"Temp"));
        if(args.Contains("-tmm-hemogen-reload")||args.Contains("-tmm-hemogen-ingest-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame(args.Contains("-tmm-hemogen-ingest-reload")?"TMM-Hemogen-Ingest":"TMM-Hemogen"));
    }
    private static bool Temp(ref float __result){__result=21;return false;}
}
public sealed class HemogenProbe : MapComponent
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
    public HemogenProbe(Map map):base(map){}
    private Gene_Hemogen Gene {get{return GuestHemogen.Gene(guest);}}
    private ComplexSales Sales {get{return map.GetComponent<ComplexSales>();}}
    private SaleOrder Order {get{return Sales.ById(orderId);}}
    private int Money {get{return HotelBillingHooks.Money(guest);}}
    private int MapPacks {get{return map.listerThings.ThingsOfDef(ThingDefOf.HemogenPack).Where(t=>t.Spawned).Sum(t=>t.stackCount);}}
    private int ColonySilver {get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount)+
        map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction==Faction.OfPlayer).Sum(p=>(p.inventory==null?0:p.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).Sum(t=>t.stackCount))+
        (p.carryTracker!=null&&p.carryTracker.CarriedThing!=null&&p.carryTracker.CarriedThing.def==ThingDefOf.Silver?p.carryTracker.CarriedThing.stackCount:0));}}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Escape(string t){return "\""+t.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void ResetSearch(){((Dictionary<int,int>)AccessTools.Field(typeof(GuestHemogenSearch),"retry").GetValue(map.GetComponent<GuestHemogenSearch>())).Clear();}
    private static void Wait(Pawn p){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
    private Job Native(Pawn p){return (Job)AccessTools.Method(typeof(JobGiver_GetHemogen),"TryGiveJob").Invoke(new JobGiver_GetHemogen(),new object[]{p});}
    private Thing Packs(int count,IntVec3 cell)
    {
        var t=ThingMaker.MakeThing(ThingDefOf.HemogenPack);t.stackCount=count;GenSpawn.Spawn(t,cell,map);t.SetForbidden(false,false);return t;
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
        if(Scribe.mode==LoadSaveMode.PostLoadInit){loaded=saved&&Gene!=null&&Math.Abs(Gene.Value-savedValue)<0.00001f&&Gene.hemogenPacksAllowed&&!desk.hemogenPurchases;
            ingestLoaded=ingestSaved&&Gene!=null&&Math.Abs(Gene.Value-savedIngestValue)<0.00001f&&guest.CurJobDef==JobDefOf.Ingest;}
    }
    private void Vectors()
    {
        var root=LoadedModManager.RunningModsListForReading.Single(m=>m.PackageIdPlayerFacing=="colonyproject.integrated").RootDir;
        var rows=XDocument.Load(Path.Combine(root,"Tests","HemogenWantedCases.xml")).Root.Elements("case").ToList();Check(rows.Count==16,"Expected16 independent count vectors");
        foreach(var row in rows){
            Func<string,float> f=k=>float.Parse(row.Attribute(k).Value,CultureInfo.InvariantCulture);
            Check(GuestHemogen.Wanted(f("value"),f("max"),f("gain"))==int.Parse(row.Attribute("expected").Value),"Count vector mismatch "+row.Attribute("id").Value);
        }
    }
    private void Setup()
    {
        Vectors();Check(GuestDutyTrees.PatchedCopies>0&&GuestDutyTrees.ExistingKeysPreserved,"Live duty copies not patched safely");
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
        guest.genes.AddGene(DefDatabase<GeneDef>.GetNamed("Hemogenic"),false);guest.genes.AddGene(DefDatabase<GeneDef>.GetNamed("Bloodfeeder"),false);
        GenSpawn.Spawn(guest,new IntVec3(44,0,44),map);guest.inventory.innerContainer.ClearAndDestroyContents();SetMoney(1000);
        guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=1;
        foreach(var t in map.attackTargetsCache.TargetsHostileToFaction(faction).Select(t=>t.Thing).Where(t=>t.Spawned).ToArray())t.DeSpawn();
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(44,0,44),new List<Pawn>{guest},map,false,false,600000});guest.GetLord().ReceiveMemo("TravelArrived");guest.needs.joy.CurLevelPercentage=1;
        Check(GuestHemogen.Managed(guest)&&Gene!=null&&Gene.Active,"Fixture hemogenic guest unavailable");
        Gene.SetTargetValuePct(0.4f);Gene.Value=0.1f;Gene.hemogenPacksAllowed=true;
        var own=ThingMaker.MakeThing(ThingDefOf.HemogenPack);own.stackCount=2;guest.inventory.innerContainer.TryAdd(own);
        loose=Packs(6,new IntVec3(45,0,46));
        Gene.hemogenPacksAllowed=false;Check(GuestHemogen.MakeJob(guest)==null,"Disabled packs were consumed");Gene.hemogenPacksAllowed=true;
        var job=Native(guest);Check(job!=null&&job.def==JobDefOf.Ingest&&job.targetA.Thing==own&&job.count==2&&job.takeExtraIngestibles==0,"Native guest giver did not restrict ingestion to personal stock");
        worker=map.mapPawns.FreeColonistsSpawned.First(p=>!p.WorkTypeIsDisabled(ComplexSales.SalesWork));
        worker.Position=new IntVec3(43,0,45);worker.Notify_Teleported();
        if(GuestHemogen.Gene(worker)==null)worker.genes.AddGene(DefDatabase<GeneDef>.GetNamed("Hemogenic"),false);
        var wg=GuestHemogen.Gene(worker);wg.Value=0.1f;wg.hemogenPacksAllowed=true;
        Check(!GuestHemogen.Managed(worker)&&Native(worker)!=null,"Player hemogen behavior was blocked");wg.Value=wg.Max;
        foreach(var p in map.mapPawns.FreeColonistsSpawned)p.workSettings.SetPriority(ComplexSales.SalesWork,0);
        guest.jobs.StartJob(job,JobCondition.InterruptForced);stage=1;start=GuestDossiers.Now;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    private void StartRequest(bool privateStock)
    {
        ResetSearch();Wait(guest);var job=guest.thinker.MainThinkNodeRoot.TryIssueJobPackage(guest,default(JobIssueParams)).Job;
        Check(job!=null&&job.def==(privateStock?ComplexSales.WaitJob:ComplexSales.BuyJob),"Native guest tree missed hemogen order: "+job);
        orderId=job.count;var o=Order;Check(o!=null&&o.count==4&&o.price==11&&o.delivery==privateStock,"Unexpected physical pack quote/count");
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
            if(Gene.Value<0.45f){if(GuestDossiers.Now-start>2500)throw new Exception("Personal ingest timed out: "+guest.CurJob);return;}
            Check(GuestHemogen.Personal(guest)==null&&Money==1000&&Sales.earned==0&&MapPacks==6,"Personal ingestion spent money or took map stock");
            Gene.Value=0.1f;desk.hemogenPurchases=false;ResetSearch();Check(Native(guest)==null&&Sales.orders.Count==0,"Guest stole loose colony pack or used unrestricted native fallback");
            stock=Packs(4,shelf.Position);shelf.GetComp<CompComplexSale>().forSale=true;desk.hemogenPurchases=true;
            guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingDefOf.HemogenPack,false);ResetSearch();Check(GuestHemogen.MakeJob(guest)==null,"Purchase bypassed food policy");
            guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingDefOf.HemogenPack,true);SetMoney(0);ResetSearch();Check(GuestHemogen.MakeJob(guest)==null,"Guest bought packs without money");
            SetMoney(1000);shelf.GetComp<CompComplexSale>().forSale=false;ResetSearch();Check(GuestHemogen.MakeJob(guest)==null,"Unmarked colony shelf sold packs");shelf.GetComp<CompComplexSale>().forSale=true;
            StartRequest(false);stage=2;return;
        }
        if(stage==2){
            if(!ingestSaved&&guest.CurJobDef==JobDefOf.Ingest&&guest.CurJob.jobGiver is JobGiver_GuestHemogen&&Gene.Value<0.2f){
                ingestSaved=true;savedIngestValue=Gene.Value;GameDataSaveLoader.SaveGame("TMM-Hemogen-Ingest");
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Hemogen-Ingest")),"Automatic ingest save missing");
            }
            if(Gene.Value<0.85f){if(GuestDossiers.Now-start>3500)throw new Exception("Public purchase/ingest timed out: "+guest.CurJob+" value="+Gene.Value+" money="+Money+" suitable="+SaleStock.Suitable(guest,stock));return;}
            Check(Money==beforeMoney-price&&ColonySilver==beforeFloor+price&&Sales.earned==beforeEarned+price,"Public hemogen money failed conservation: money="+Money+" expected="+(beforeMoney-price)+" colonySilver="+ColonySilver+" expected="+(beforeFloor+price)+" earned="+Sales.earned+" expected="+(beforeEarned+price));
            Check(MapPacks==6&&GuestHemogen.Personal(guest)==null,"Protected colony quantity or owned consumption changed: map="+MapPacks+" personal="+GuestHemogen.Personal(guest));
            Gene.Value=0.1f;shelf.GetComp<CompComplexSale>().forSale=false;stock=Packs(4,staffShelf.Position);staffShelf.GetComp<CompComplexSale>().forSale=true;ResetSearch();
            Check(GuestHemogen.MakeJob(guest)==null,"Staff packs offered without seller");
            Check(desk.SetRole(worker,ComplexRole.Seller),"Could not assign seller");desk.Employee(worker).mode=ComplexWorkMode.ColonyOnly;ResetSearch();
            Check(GuestHemogen.MakeJob(guest)==null,"Colony-only seller accepted staff delivery");desk.Employee(worker).mode=ComplexWorkMode.Both;
            StartRequest(true);StartWorker();stage=3;return;
        }
        if(stage==3){
            if(Order==null||Order.goods==null){if(GuestDossiers.Now-start>2500)throw new Exception("Seller pickup timed out");return;}
            Check(Order.worker==worker&&worker.carryTracker.CarriedThing==Order.goods&&Order.goods.stackCount==4,"Pack courier lost physical cargo");
            Check(!desk.staffArea[guest.Position]&&guest.CurJobDef==ComplexSales.WaitJob,"Buyer entered staff stock instead of waiting");
            Check(Money==beforeMoney&&Sales.earned==beforeEarned,"Pending delivery charged early");
            desk.hemogenPurchases=false;saved=true;savedValue=Gene.Value;GameDataSaveLoader.SaveGame("TMM-Hemogen");
            Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Hemogen")),"Pending hemogen save missing");Finish(false);
        }
    }
    private void Reload()
    {
        if(stage==0){
            Check(loaded,"Native gene/pack permission or reception preference failed load");
            Check(Order!=null&&Order.goods!=null&&worker.carryTracker.CarriedThing==Order.goods&&guest.CurJobDef==ComplexSales.WaitJob&&guest.CurJob.count==orderId,"Pending courier/order references failed load");
            Check(Money==beforeMoney&&Sales.earned==beforeEarned,"Loading created an early payment");start=GuestDossiers.Now;stage=1;
        }
        if(stage==1){
            if(Gene.Value<0.85f){if(GuestDossiers.Now-start>3500)throw new Exception("Loaded delivery/ingest timed out: "+guest.CurJob+" value="+Gene.Value);return;}
            Check(!desk.hemogenPurchases&&Money==beforeMoney-price&&ColonySilver==beforeFloor+price&&Sales.earned==beforeEarned+price,"Accepted delivery was cancelled by purchase toggle or payment failed");
            long earned=Sales.earned;int money=Money;Sales.Process();Sales.Process();Check(Sales.earned==earned&&Money==money,"Repeated processing duplicated hemogen payment");
            Check(GuestDossiers.Book.Find(guest.GetUniqueLoadID()).sales==22&&MapPacks==6,"Dossier sale income or protected colony stock incorrect");
            // A separate unpaid order is cancelled while its actual courier carries it.
            Gene.Value=0.1f;stock=Packs(4,staffShelf.Position);desk.hemogenPurchases=true;StartRequest(true);desk.hemogenPurchases=false;StartWorker();stage=2;return;
        }
        if(stage==2){
            if(Order==null||Order.goods==null){if(GuestDossiers.Now-start>2500)throw new Exception("Cancellation courier pickup timed out");return;}
            var o=Order;worker.drafter.Drafted=true;Sales.Process();
            int packs=map.listerThings.ThingsOfDef(ThingDefOf.HemogenPack).Where(t=>t.Spawned).Sum(t=>t.stackCount)+
                guest.inventory.innerContainer.Where(t=>t.def==ThingDefOf.HemogenPack).Sum(t=>t.stackCount)+
                (worker.carryTracker.CarriedThing!=null&&worker.carryTracker.CarriedThing.def==ThingDefOf.HemogenPack?worker.carryTracker.CarriedThing.stackCount:0)+
                Sales.orders.Sum(x=>x.returnedGoods.Where(t=>t.def==ThingDefOf.HemogenPack).Sum(t=>t.stackCount));
            Check(o.cancelled&&!o.completed&&Money==beforeMoney&&Sales.earned==beforeEarned,"Cancelled pack delivery created payment");
            Check(packs==10,"Cancelled pack cargo lost or duplicated: "+packs);Finish(true);
        }
    }
    private void IngestReload()
    {
        if(stage==0){
            Check(ingestLoaded&&guest.CurJob.jobGiver is JobGiver_GuestHemogen,"Automatic ingest/giver did not restore");
            var job=guest.CurJob;var lookup=new object[]{job.jobGiver.UniqueSaveKey,null};
            Check(job.jobGiverThinkTree!=null&&(bool)AccessTools.Method(typeof(ThinkTreeDef),"TryGetThinkNodeWithSaveKey").Invoke(job.jobGiverThinkTree,lookup)&&lookup[1]==job.jobGiver,"New duty key does not resolve to original live node after reload");
            Check(Money==beforeMoney-price&&Sales.earned==beforeEarned+price,"Ingest reload altered paid sale");start=GuestDossiers.Now;stage=1;
        }
        if(Gene.Value<0.85f){if(GuestDossiers.Now-start>2500)throw new Exception("Restored native ingest did not finish");return;}
        Check(GuestHemogen.Personal(guest)==null&&Money==beforeMoney-price,"Restored ingest left owned cargo or charged again");
        Check(Sales.earned==beforeEarned+price&&ColonySilver==beforeFloor+price,"Restored ingest duplicated receipt");Finish(true);
    }
    public override void MapComponentUpdate()
    {
        bool ingest=Environment.GetCommandLineArgs().Contains("-tmm-hemogen-ingest-reload");
        bool reload=ingest||Environment.GetCommandLineArgs().Contains("-tmm-hemogen-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-hemogen-probe")))return;
        try{
            if(guest!=null&&GuestDossiers.Now>=nextTrace){nextTrace=GuestDossiers.Now+100;
                File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"HemogenTrace.log"),GuestDossiers.Now+" stage="+stage+" job="+guest.CurJob+" gene="+Gene.Value+" allowed="+Gene.hemogenPacksAllowed+" personal="+GuestHemogen.Personal(guest)+" inv="+string.Join(";",guest.inventory.innerContainer.Select(t=>t.ToString()))+" managed="+GuestHemogen.Managed(guest)+" fit="+GuestHemogen.Fit(guest)+" lord="+guest.GetLord()+" duty="+guest.mindState.duty+" pos="+guest.Position+" earned="+Sales.earned+" money="+Money+Environment.NewLine);
            }
            if(ingest)IngestReload();else if(reload)Reload();else Fresh();}catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    public override void MapComponentOnGUI()
    {
        if(ui||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-hemogen-probe"))return;
        ui=true;string error=null;try{new Dialog_GuestNeeds(desk).DoWindowContents(new Rect(10,10,880,710));new Dialog_ComplexServices(desk){page=1}.DoWindowContents(new Rect(10,10,740,720));}catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"HemogenUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,Environment.GetCommandLineArgs().Contains("-tmm-hemogen-ingest-reload")?"HemogenIngestReloadProbe.json":reload?"HemogenReloadProbe.json":"HemogenProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
}
