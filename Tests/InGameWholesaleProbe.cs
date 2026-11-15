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
public static class WholesaleLaunch
{
    public static bool blockReceipts;
    public static Pawn blockGoodsTo;
    public static bool runScheduler;
    public static ThingOwner blockStaging;
    static WholesaleLaunch()
    {
        var flags=Environment.GetCommandLineArgs();
        if(flags.Contains("-tmm-wholesale-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-Wholesale"));
        if(!flags.Any(a=>a=="-tmm-wholesale-probe"||a=="-tmm-wholesale-reload"))return;
        var h=new Harmony("tmm.test.wholesale");
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"WholesaleTrace.log"),"");
        h.Patch(AccessTools.Method(typeof(JobDriver),"EndJobWith"),prefix:new HarmonyMethod(typeof(WholesaleLaunch),"TraceEnd"));
        h.Patch(AccessTools.Method(typeof(WholesaleVisits),"MapComponentTick"),prefix:new HarmonyMethod(typeof(WholesaleLaunch),"ManualTicks"));
        h.Patch(AccessTools.Method(typeof(ThingOwner<Thing>),"TryAdd",new[]{typeof(Thing),typeof(bool)}),prefix:new HarmonyMethod(typeof(WholesaleLaunch),"RejectGoods"));
        foreach(var m in typeof(GenPlace).GetMethods().Where(m=>m.Name=="TryPlaceThing"&&m.ReturnType==typeof(bool)&&m.GetParameters()[0].ParameterType==typeof(Thing)))
            h.Patch(m,prefix:new HarmonyMethod(typeof(WholesaleLaunch),"Block"));
    }
    private static bool ManualTicks(){return runScheduler;}
    private static void TraceEnd(JobDriver __instance,JobCondition __0)
    {
        if(!(__instance is JobDriver_BuyComplexGoods)||__instance.pawn.Map==null)return;
        var p=__instance.pawn;var group=WholesaleTrade.Group(p);if(group==null)return;
        var sales=p.Map.GetComponent<ComplexSales>();var o=sales.ById(p.CurJob.count);
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"WholesaleTrace.log"),"tick="+Find.TickManager.TicksGame+" end="+__0+" pos="+p.Position+" spent="+group.spent+
            (o==null?" order=null":" order="+o.id+" cancelled="+o.cancelled+" valid="+sales.Valid(o)+" count="+o.count+" price="+o.price+" quote="+WholesaleTrade.Plan(p,o.item,o.count,o.unitPrice).Quote+" packing="+WholesaleTrade.Plan(p,o.item,o.count,o.unitPrice).Status+
            " source="+SaleStock.Contains(o.storage,o.item)+" free="+sales.FreeCount(o.item,o)+" space="+SaleStock.Space(p,o.item)+" nearbyMoney="+WholesaleTrade.Money(p))+
            " crew="+string.Join(";",group.Members.Select(m=>m.GetUniqueLoadID()+":"+m.Position+":"+m.CurJob+":duty="+m.mindState.duty+":mass="+MassUtility.GearAndInventoryMass(m)+"/"+MassUtility.Capacity(m)))+Environment.NewLine);
    }
    private static bool RejectGoods(ThingOwner __instance,Thing __0,ref bool __result)
    {
        if(__instance==blockStaging){__result=false;return false;}
        if(blockGoodsTo==null||__instance!=blockGoodsTo.inventory.innerContainer||__0.def!=ThingDefOf.Steel)return true;
        __result=false;return false;
    }
    private static bool Block(Thing __0,ref bool __result){if(!blockReceipts||__0.def!=ThingDefOf.Silver)return true;__result=false;return false;}
}
public sealed class WholesaleProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building shelf,bookcase;
    private Thing steel,book,looseItem;
    private Pawn buyer,partner,worker;
    private Lord visitLord;
    private List<Pawn> generated=new List<Pawn>();
    private int stage,started,checks,orderId,paidId,price,bookPrice,initialFloor,bookId,automaticPrice,looseId;
    private bool reported,uiReported,saved;
    private string bookLabel;
    private readonly List<string> errors=new List<string>();
    private ComplexSales Sales {get{return map.GetComponent<ComplexSales>();}}
    private WholesaleVisits Visits {get{return map.GetComponent<WholesaleVisits>();}}
    private LordJob_WholesaleVisit Group {get{return visitLord==null?null:visitLord.LordJob as LordJob_WholesaleVisit;}}
    private SaleOrder Order {get{return Sales.ById(orderId);}}
    public WholesaleProbe(Map map):base(map){}
    public override void ExposeData()
    {
        base.ExposeData();Scribe_References.Look(ref desk,"wpDesk");Scribe_References.Look(ref shelf,"wpShelf");Scribe_References.Look(ref bookcase,"wpBookcase");
        Scribe_References.Look(ref steel,"wpSteel");Scribe_References.Look(ref book,"wpBook");Scribe_References.Look(ref buyer,"wpBuyer");
        Scribe_References.Look(ref partner,"wpPartner");Scribe_References.Look(ref worker,"wpWorker");Scribe_References.Look(ref visitLord,"wpLord");
        Scribe_Values.Look(ref orderId,"wpOrder");Scribe_Values.Look(ref paidId,"wpPaid");Scribe_Values.Look(ref price,"wpPrice");Scribe_Values.Look(ref bookPrice,"wpBookPrice");
        Scribe_Values.Look(ref initialFloor,"wpFloor");Scribe_Values.Look(ref saved,"wpSaved");Scribe_Values.Look(ref bookId,"wpBookId");Scribe_Values.Look(ref bookLabel,"wpBookLabel");
        Scribe_References.Look(ref looseItem,"wpLoose");Scribe_Values.Look(ref looseId,"wpLooseId");
    }
    private void Check(bool condition,string text){if(!condition)throw new Exception(text);checks++;}
    private void Wait(Pawn p){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
    private int Money(){return HotelBillingHooks.Money(buyer)+HotelBillingHooks.Money(partner);}
    private int Floor(){return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Sum(t=>t.stackCount);}
    private int Stock(){return map.listerThings.ThingsOfDef(ThingDefOf.Steel).Where(t=>t.Position.DistanceTo(shelf.Position)<4).Sum(t=>t.stackCount);}
    private int Bought(){return new[]{buyer,partner}.Sum(p=>p.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Steel).Sum(t=>t.stackCount));}
    private Building Spawn(string name,IntVec3 cell)
    {
        var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);
        b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);
    }
    private Pawn Member(Faction faction,int coins)
    {
        var p=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,allowDead:false,allowDowned:false,
            canGeneratePawnRelations:false,fixedBiologicalAge:30f,allowAddictions:false,allowPregnant:false));
        p.inventory.innerContainer.ClearAndDestroyContents();var cash=ThingMaker.MakeThing(ThingDefOf.Silver);cash.stackCount=coins;p.inventory.innerContainer.TryAdd(cash,false);
        var wood=ThingMaker.MakeThing(ThingDefOf.WoodLog);wood.stackCount=(int)Math.Floor((MassUtility.FreeSpace(p)-10)/wood.GetStatValue(StatDefOf.Mass));
        Check(wood.stackCount>0&&p.inventory.innerContainer.TryAdd(wood,false),"Could not prepare per-member cargo weight");
        p.needs.food.CurLevelPercentage=1;p.needs.rest.CurLevelPercentage=1;return p;
    }
    private void Start(SaleOrder o)
    {
        var j=o.delivery?JobMaker.MakeJob(ComplexSales.WaitJob,o.waitCell):JobMaker.MakeJob(ComplexSales.BuyJob,o.storage);
        j.count=o.id;o.buyer.jobs.StartJob(j,JobCondition.InterruptForced);orderId=o.id;started=Find.TickManager.TicksGame;
    }
    private bool Pending(){return Order!=null&&!Order.completed&&!Order.cancelled&&Find.TickManager.TicksGame-started<3000;}
    private void Setup()
    {
        foreach(var p in map.mapPawns.FreeColonistsSpawned){Wait(p);p.workSettings.SetPriority(ComplexSales.SalesWork,0);}
        foreach(var c in new CellRect(38,38,36,22)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);
        }
        Area_Allowed area,staff;map.areaManager.TryMakeNewAllowed(out area);map.areaManager.TryMakeNewAllowed(out staff);
        foreach(var c in new CellRect(40,40,31,17)){area[c]=true;if(c.x>=64)staff[c]=true;}
        desk=(Building_ComplexDesk)Spawn("TMM_ComplexDesk",new IntVec3(42,0,45));desk.area=area;desk.staffArea=staff;desk.ApplyServiceAreas();
        shelf=Spawn("Shelf",new IntVec3(58,0,44));bookcase=Spawn("Bookcase",new IntVec3(67,0,44));
        foreach(var rect in new[]{new CellRect(40,42,6,6),new CellRect(65,42,6,6)}){
            var door=new IntVec3(rect.minX==40?45:65,0,45);
            foreach(var c in rect.EdgeCells)Spawn(c==door?"Door":"Wall",c);
            foreach(var c in rect.ContractedBy(1))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
        }
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        steel=ThingMaker.MakeThing(ThingDefOf.Steel);steel.stackCount=75;GenSpawn.Spawn(steel,shelf.Position,map);
        book=ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefsListForReading.First(d=>d.category==ThingCategory.Item&&typeof(Book).IsAssignableFrom(d.thingClass)));
        Check(((ISearchableContents)bookcase).SearchableContents.TryAdd(book,false),"Book fixture failed");bookId=book.thingIDNumber;bookLabel=book.Label;
        shelf.GetComp<CompComplexSale>().forSale=true;bookcase.GetComp<CompComplexSale>().forSale=true;
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&!f.defeated&&!f.HostileTo(Faction.OfPlayer)&&WholesaleVisits.Makers(f).Any());
        buyer=Member(faction,500);partner=Member(faction,500);
        var payers=new[]{buyer,partner}.OrderBy(p=>p.GetUniqueLoadID(),StringComparer.Ordinal).ToArray();
        var firstCoins=payers[0].inventory.innerContainer.First(t=>t.def==ThingDefOf.Silver);
        payers[0].inventory.innerContainer.TryTransferToContainer(firstCoins,payers[1].inventory.innerContainer,480,false);
        var g=Visits.StartGroup(faction,new IntVec3(46,0,45),new IntVec3(46,0,45),new List<Pawn>{buyer,partner},0,false);
        visitLord=buyer.GetLord();Check(g!=null&&visitLord!=null,"Own wholesale lord not created");visitLord.ReceiveMemo("TravelArrived");
        Check(g.Shopping&&WholesaleTrade.Buyer(buyer)&&!RestaurantMeals.Guest(buyer),"Wholesale buyer used hotel AI or failed to enter shopping");
        Check(!ForbidUtility.InAllowedArea(bookcase.Position,buyer)&&ForbidUtility.InAllowedArea(desk.Position,buyer),"Wholesale area restrictions differ from public/staff zones");
        Check(buyer.CanReach(desk.Position,PathEndMode.OnCell,Danger.None),"Neutral buyer cannot enter a public room through its closed door");
        Check(HotelBillingHooks.Bed(buyer)==null&&HotelBillingHooks.Bed(partner)==null,"Wholesale group claimed hotel beds");
        Check(WholesaleVisits.Delay(0)==240000&&WholesaleVisits.Delay(70000)==45000&&WholesaleVisits.Budget(1000)==1000&&WholesaleVisits.Budget(60000)==30000,"Scaling boundaries differ");
        for(int n=0;n<WholesaleDelayCases.Values.Length;n++)Check(WholesaleVisits.Delay(WholesaleDelayCases.Values[n])==WholesaleDelayCases.Expected[n],"Independent delay fixture differs at index "+n);
        Check(!WholesaleVisits.Makers(null).Any(),"Unsupported faction was not skipped");
        var plan=WholesaleTrade.Plan(buyer,steel,30,SaleStock.UnitPrice(steel));
        Check(plan.Status==TurboMegaMod.Wholesale.PackingStatus.Ready&&plan.Allocations.Count(a=>a.GoodsCount>0)==2,"Fixture does not split cargo across two people");
        string blocked=plan.Allocations.Where(a=>a.GoodsCount>0).Last().CarrierId;WholesaleLaunch.blockGoodsTo=new[]{buyer,partner}.Single(p=>p.GetUniqueLoadID()==blocked);
        initialFloor=Floor();Wait(buyer);var order=Sales.Create(buyer,shelf,steel,30);Check(order!=null&&order.count==30,"Wholesale sale could not be booked");price=order.price;Start(order);stage=1;
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-wholesale-reload");
        if(reported||Find.CurrentMap!=map||!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-wholesale-probe"))return;
        try{
            if(!reload&&stage==0){Setup();return;}
            if(!reload&&stage==1){
                if(Pending())return;Sales.Process();
                Check(Money()==1000&&Bought()==0&&Stock()==75,"Rejected partial transfer lost money or goods: money="+Money()+" bought="+Bought()+" stock="+Stock()+" job="+buyer.CurJob);
                Check(Sales.earned==0&&Floor()==initialFloor&&Group.spent==0,"Failed transfer created income");
                Check(!Sales.orders.Any(o=>o.buyer==buyer),"Rollback retained pending financial state");
                WholesaleLaunch.blockGoodsTo=null;WholesaleLaunch.blockReceipts=true;Wait(buyer);
                var retry=Sales.Create(buyer,shelf,steel,30);Check(retry!=null,"Sale unavailable after rollback");Start(retry);paidId=retry.id;stage=2;return;
            }
            if(!reload&&stage==2){
                if(Pending())return;
                Check(Order!=null&&Order.completed&&Bought()==30&&Stock()==45,"Real group sale failed: "+buyer.CurJob);
                Check(new[]{buyer,partner}.All(p=>p.inventory.innerContainer.Any(t=>t.def==ThingDefOf.Steel)),"Cargo was not distributed across both inventories");
                Check(Money()==1000-price&&Group.spent==price&&Sales.earned==0&&Floor()==initialFloor,"Unplaced receipts counted as income");
                Check(Order.GetDirectlyHeldThings().Sum(t=>t.stackCount)==price,"Actual silver did not enter saved escrow");
                Check(Order.wholesaleDebits.Count(d=>d.remaining>0)==2&&Order.wholesaleDebits.Sum(d=>d.remaining)==price,"Runtime payment did not debit both group members");
                Check(buyer.carryTracker.CarriedThing==null&&partner.carryTracker.CarriedThing==null,"Purchased cargo remained in hands");
                Wait(buyer);looseItem=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_BasicShirt"),ThingDefOf.Cloth);
                looseItem.TryGetComp<CompQuality>().SetQuality(QualityCategory.Excellent,ArtGenerationContext.Colony);looseId=looseItem.thingIDNumber;
                GenSpawn.Spawn(looseItem,shelf.Position,map);
                var reject=Sales.Create(buyer,shelf,looseItem,1);Check(reject!=null,"Loose-quality fixture could not book a sale");
                WholesaleLaunch.blockStaging=reject.returnedGoods;Start(reject);stage=21;return;
            }
            if(!reload&&stage==21){
                if(Pending())return;
                Check(Order!=null&&Order.cancelled&&Order.unplacedGoods.Contains(looseItem)&&!looseItem.Spawned&&looseItem.holdingOwner==null,"Rejected staging did not preserve the loose item for save");
                Check(Money()==1000-price&&Group.spent==price,"Rejected staging charged the group");
                Wait(buyer);worker=map.mapPawns.FreeColonistsSpawned.First(p=>!p.WorkTypeIsDisabled(ComplexSales.SalesWork));
                Check(desk.SetRole(worker,ComplexRole.Seller),"Seller fixture role failed");
                var o=Sales.Create(buyer,bookcase,book,1);Check(o!=null&&o.delivery,"Private wholesale order unavailable");bookPrice=o.price;Start(o);
                var giver=DefDatabase<WorkGiverDef>.GetNamed("TMM_ComplexSalesDelivery").Worker as WorkGiver_Scanner;
                Check(giver.HasJobOnThing(worker,desk),"Seller did not see wholesale customer");worker.jobs.StartJob(giver.JobOnThing(worker,desk),JobCondition.InterruptForced);stage=3;return;
            }
            if(!reload&&stage==3){
                if(Order!=null&&Order.goods==null&&Pending())return;
                Check(Order!=null&&Order.goods==book&&worker.carryTracker.CarriedThing==book,"Private pickup failed: "+worker.CurJob);
                Check(!ComplexLayout.StaffOnly(desk,buyer.Position),"Wholesale buyer entered staff-only stock");
                saved=true;GameDataSaveLoader.SaveGame("TMM-Wholesale");Finish(false);return;
            }
            if(reload&&stage==0){
                Check(saved&&Group!=null&&Group.Shopping&&Group.Members.Count()==2,"Group state did not survive load");
                Check(Group.spent==price&&Money()==1000-price&&Bought()==30,"Saved cargo or funds lost");
                Check(Order!=null&&Order.goods==book&&Order.worker==worker&&worker.carryTracker.CarriedThing==book&&Order.price==bookPrice,"Saved delivery references or quote lost");
                Check(book.thingIDNumber==bookId&&book.Label==bookLabel,"Saved book metadata changed");
                QualityCategory quality;Check(looseItem!=null&&!looseItem.Destroyed&&looseItem.thingIDNumber==looseId&&looseItem.TryGetQuality(out quality)&&quality==QualityCategory.Excellent,"Deep-saved loose item reference/quality was lost");
                Sales.Process();Check(looseItem.Spawned,"Loose saved item was not returned after rejection cleared");
                Check(Sales.earned+Sales.orders.Sum(o=>o.GetDirectlyHeldThings().Sum(t=>t.stackCount))==price,"Receipt lost or duplicated across reload");
                started=Find.TickManager.TicksGame;stage=4;return;
            }
            if(reload&&stage==4){
                if(!new[]{buyer,partner}.Any(p=>p.inventory.innerContainer.Contains(book))&&Find.TickManager.TicksGame-started<3000)return;
                Check(new[]{buyer,partner}.Any(p=>p.inventory.innerContainer.Contains(book))&&!SaleStock.Contents(bookcase).Contains(book),"Saved private delivery did not finish: "+worker.CurJob);
                Check(book.thingIDNumber==bookId&&book.Label==bookLabel,"Purchased book metadata changed");
                Sales.Process();Sales.Process();
                Check(Money()==1000-price-bookPrice&&Group.spent==price+bookPrice&&Sales.earned==price+bookPrice&&Floor()==initialFloor+price+bookPrice,"Group payment or income duplicated");
                Check(!Sales.Deliver(Order,worker),"Completed private sale accepted twice");
                Wait(buyer);Group.lastAction=Find.TickManager.TicksGame-120;
                started=Find.TickManager.TicksGame;stage=5;return;
            }
            if(reload&&stage==5){
                Visits.ProcessVisits();Sales.Process();
                if(Group.spent==price+bookPrice&&Find.TickManager.TicksGame-started<4000)return;
                automaticPrice=Group.spent-price-bookPrice;
                Check(automaticPrice>0&&Money()+Group.spent==1000&&Sales.earned==Group.spent&&Stock()+Bought()==75,"Automatic shopping failed or lost silver: "+buyer.CurJob);
                Check(Floor()==initialFloor+Sales.earned,"Automatic receipts not physical silver");
                Group.deadline=Find.TickManager.TicksGame;Visits.ProcessVisits();Check(Group.leaving&&Group.aborted,"Absolute visit deadline did not initiate exit");
                Check(HotelBillingHooks.Bed(buyer)==null&&!RestaurantMeals.Guest(buyer),"Buyer entered hotel lifecycle");
                started=Find.TickManager.TicksGame;stage=51;return;
            }
            if(reload&&stage==51){
                // Cancellation ends the trade immediately; vanilla releases
                // its job reservations on subsequent pawn ticks. Recheck free
                // stock rather than demanding a spawn in the same callback.
                if(Visits.StockValue()<=0&&Find.TickManager.TicksGame-started<1200)return;
                bool spawned=Visits.TryVisit();
                Check(spawned,"Dynamic faction visit could not be generated: V="+Visits.StockValue()+" groups="+Visits.Groups.Count()+" stock="+Stock());
                var generatedGroup=Visits.Groups.First(g=>g!=Group);generated=generatedGroup.Members.ToList();
                Check(generated.Count>=3&&generated.All(p=>p.Spawned&&p.GetLord()!=visitLord),"Generated group was incomplete");
                if(Visits.Groups.Count()==2)Check(!Visits.TryVisit(),"More than two concurrent wholesale groups spawned");
                else Check(Visits.Groups.Count()<2,"Wholesale group count exceeded cap");
                Check(generated.All(p=>!RestaurantMeals.Guest(p)&&HotelBillingHooks.Bed(p)==null),"Generated group booked rooms");
                started=Find.TickManager.TicksGame;stage=6;return;
            }
            if(reload&&stage==6){
                var g=generated.Select(WholesaleTrade.Group).FirstOrDefault(x=>x!=null);
                if(g!=null&&!g.Shopping&&Find.TickManager.TicksGame-started<4000)return;
                Check(g!=null&&g.Shopping,"Generated group did not physically reach the public point: "+string.Join(" | ",generated.Select(p=>
                    p.LabelShort+" spawned="+p.Spawned+" downed="+p.Downed+" pos="+p.Position+" job="+p.CurJob+" duty="+p.mindState.duty+
                    " lord="+(p.GetLord()==null?"null":p.GetLord().CurLordToil.GetType().FullName)+" reach="+(p.Spawned&&g!=null?p.CanReach(g.spot,PathEndMode.OnCell,Danger.Deadly).ToString():"n/a")+" speed="+p.GetStatValue(StatDefOf.MoveSpeed))));
                Check(g.Members.All(p=>p.Position.DistanceTo(g.spot)<15),"TravelArrived accepted distant members");
                g.deadline=Find.TickManager.TicksGame;Visits.ProcessVisits();Check(g.leaving,"Generated visit failed to initiate departure");
                started=Find.TickManager.TicksGame;stage=7;return;
            }
            if(reload&&stage==7){
                if((buyer.Spawned||partner.Spawned||generated.Any(p=>p.Spawned))&&Find.TickManager.TicksGame-started<5000)return;
                Check(!buyer.Spawned&&!partner.Spawned&&generated.All(p=>!p.Spawned),"Groups did not physically leave the map");
                Check(!Sales.orders.Any(o=>!o.completed&&!o.cancelled),"Departed group retained pending orders");
                Check(Sales.earned==price+bookPrice+automaticPrice&&Floor()==initialFloor+Sales.earned,"Departure duplicated income");
                shelf.GetComp<CompComplexSale>().forSale=false;Visits.nextVisitTick=Find.TickManager.TicksGame-1;Visits.lastVisitTick=-1;Visits.retryAfterTick=0;
                SchedulerOnce();Check(!Visits.Groups.Any()&&Visits.nextVisitTick==-1,"Scheduler created a group without stock");
                shelf.GetComp<CompComplexSale>().forSale=true;
                int delay=WholesaleVisits.Delay(Visits.StockValue());Visits.lastVisitTick=0;Visits.nextVisitTick=Find.TickManager.TicksGame+delay+1000;
                SchedulerOnce();Check(Visits.nextVisitTick==delay&&!Visits.Groups.Any(),"Scheduler treated legitimate lastVisitTick=0 as absent");
                Visits.lastVisitTick=-1;Visits.nextVisitTick=Find.TickManager.TicksGame+delay+1000;
                SchedulerOnce();Check(Visits.nextVisitTick==Find.TickManager.TicksGame+delay,"Growing stock did not bring the first planned visit forward");
                Visits.nextVisitTick=Find.TickManager.TicksGame-1;Visits.retryAfterTick=Find.TickManager.TicksGame+1000;
                SchedulerOnce();Check(!Visits.Groups.Any(),"Scheduler ignored failed-attempt cooldown");
                Visits.nextVisitTick=Find.TickManager.TicksGame;Visits.retryAfterTick=Find.TickManager.TicksGame;SchedulerOnce();
                Check(Visits.Groups.Count()==1&&Visits.lastVisitTick==Find.TickManager.TicksGame&&Visits.nextVisitTick>Visits.lastVisitTick,"Automatic due-visit scheduler did not create a group or advance its timer: groups="+Visits.Groups.Count()+" stock="+Visits.StockValue()+" next="+Visits.nextVisitTick+" last="+Visits.lastVisitTick+" retry="+Visits.retryAfterTick+" now="+Find.TickManager.TicksGame);
                Visits.Groups.First().Leave(false);Check(Sales.earned==price+bookPrice+automaticPrice,"New visit generated passive income");
                Check(GuestDossiers.Book.records.All(r=>r.id!=buyer.GetUniqueLoadID())&&GuestDossiers.Book.records.Sum(r=>r.sales)==0,"Wholesale buyers were registered as hotel guests");Finish(true);
            }
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    private void SchedulerOnce(){AccessTools.Field(typeof(WholesaleVisits),"nextCheck").SetValue(Visits,0);WholesaleLaunch.runScheduler=true;try{Visits.MapComponentTick();}finally{WholesaleLaunch.runScheduler=false;}}
    private static string Escape(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"WholesaleReloadProbe.json":"WholesaleProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
    public override void MapComponentOnGUI()
    {
        if(uiReported||!reported||buyer==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-wholesale-probe"))return;
        uiReported=true;string error=null;
        try{if(!buyer.GetInspectString().Contains("TMM.Wholesale.Inspect".Translate(0,0).ToString().Split(':')[0]))throw new Exception("Wholesale inspection text missing");}
        catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"WholesaleUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
}
