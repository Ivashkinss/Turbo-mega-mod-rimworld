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
public static class SalesDeliveryLaunch
{
    public static bool blockReceipts;
    static SalesDeliveryLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-delivery-reload")) LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Delivery"));
        if (!Environment.GetCommandLineArgs().Any(a => a == "-tmm-delivery-probe" || a == "-tmm-delivery-reload")) return;
        var h = new Harmony("tmm.test.delivery.receipts");
        foreach (var m in typeof(GenPlace).GetMethods().Where(m => m.Name == "TryPlaceThing" && m.ReturnType == typeof(bool) && m.GetParameters()[0].ParameterType == typeof(Thing)))
            h.Patch(m, prefix:new HarmonyMethod(typeof(SalesDeliveryLaunch), "Block"));
    }
    private static bool Block(Thing __0, ref bool __result) { if (!blockReceipts || __0.def != ThingDefOf.Silver) return true; __result = false; return false; }
}
public sealed class SalesDeliveryProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building shelf, bookcase, outfit;
    private Pawn buyer, other, worker;
    private Thing steel, book, shirt;
    private int stage, started, checks, orderId, price, floorBefore, bookId;
    private string bookLabel;
    private bool reported, uiReported, saved;
    private readonly List<string> errors = new List<string>();
    private ComplexSales Sales { get { return map.GetComponent<ComplexSales>(); } }
    private SaleOrder Order { get { return Sales.ById(orderId); } }
    public SalesDeliveryProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_References.Look(ref desk,"dpDesk"); Scribe_References.Look(ref shelf,"dpShelf");
        Scribe_References.Look(ref bookcase,"dpBookcase"); Scribe_References.Look(ref outfit,"dpOutfit");
        Scribe_References.Look(ref buyer,"dpBuyer"); Scribe_References.Look(ref other,"dpOther"); Scribe_References.Look(ref worker,"dpWorker");
        Scribe_References.Look(ref steel,"dpSteel"); Scribe_References.Look(ref book,"dpBook"); Scribe_References.Look(ref shirt,"dpShirt");
        Scribe_Values.Look(ref orderId,"dpOrder"); Scribe_Values.Look(ref price,"dpPrice"); Scribe_Values.Look(ref floorBefore,"dpFloor");
        Scribe_Values.Look(ref bookId,"dpBookId"); Scribe_Values.Look(ref bookLabel,"dpBookLabel"); Scribe_Values.Look(ref saved,"dpSaved");
    }
    private void Check(bool ok,string message) { if(!ok)throw new Exception(message);checks++; }
    private int Floor() { return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Sum(t=>t.stackCount); }
    private void Wait(Pawn p) { var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced); }
    private Building Spawn(string name,IntVec3 cell)
    {
        var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);
        b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);
    }
    private Pawn Guest(IntVec3 cell)
    {
        var f=Find.FactionManager.AllFactionsListForReading.First(x=>!x.IsPlayer&&x.def.humanlikeFaction&&!x.HostileTo(Faction.OfPlayer));
        var p=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,f,forceGenerateNewPawn:true,allowDead:false,allowDowned:false,allowAddictions:false,allowPregnant:false,fixedBiologicalAge:30));
        GenSpawn.Spawn(p,cell,map);p.inventory.innerContainer.ClearAndDestroyContents();var silver=ThingMaker.MakeThing(ThingDefOf.Silver);silver.stackCount=1000;p.inventory.innerContainer.TryAdd(silver,false);
        p.needs.food.CurLevelPercentage=1;p.needs.rest.CurLevelPercentage=1;
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{f,new IntVec3(44,0,44),new List<Pawn>{p},map,false,false,600000});
        p.GetLord().ReceiveMemo("TravelArrived");Wait(p);return p;
    }
    private void Request(Building source,Thing item,int count)
    {
        Wait(buyer);Wait(worker);
        var o=Sales.Create(buyer,source,item,count);Check(o!=null&&o.delivery,"Private order unavailable");
        orderId=o.id;price=o.price;var j=JobMaker.MakeJob(ComplexSales.WaitJob,o.waitCell);j.count=o.id;buyer.jobs.StartJob(j,JobCondition.InterruptForced);
        Check(Sales.Valid(o),"Fresh waiting order invalid");
        var giver=DefDatabase<WorkGiverDef>.GetNamed("TMM_ComplexSalesDelivery").Worker as WorkGiver_Scanner;
        Check(giver.HasJobOnThing(worker,desk),"Work giver missed waiting customer");
        var delivery=giver.JobOnThing(worker,desk);Check(delivery!=null&&delivery.count==o.id,"Work giver lost order ID");
        if(stage>=4){
            var automatic=new JobGiver_Work().TryIssueJobPackage(worker,default(JobIssueParams)).Job;
            Check(automatic!=null&&automatic.def==ComplexSales.DeliverJob&&automatic.count==o.id,"Normal work search did not select delivery: "+automatic);
            delivery=automatic;
        }
        worker.jobs.StartJob(delivery,JobCondition.InterruptForced);started=Find.TickManager.TicksGame;
        if(!item.Spawned){
            var reader=map.mapPawns.FreeColonistsSpawned.First(p=>p!=worker);
            Check(worker.HasReserved(item)&&!reader.CanReserve(item),"Container item was not reserved against another colonist");
        }
    }
    private bool StillWalking() { return Order!=null&&!Order.completed&&!Order.cancelled&&Find.TickManager.TicksGame-started<2000; }
    private void Setup()
    {
        foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var c in new CellRect(38,38,34,22)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);
        }
        Area_Allowed a,s;map.areaManager.TryMakeNewAllowed(out a);map.areaManager.TryMakeNewAllowed(out s);
        foreach(var c in new CellRect(40,40,28,17)){a[c]=true;if(c.x>=55)s[c]=true;}
        desk=(Building_ComplexDesk)Spawn("TMM_ComplexDesk",new IntVec3(42,0,45));desk.area=a;desk.staffArea=s;desk.ApplyServiceAreas();
        shelf=Spawn("Shelf",new IntVec3(58,0,44));bookcase=Spawn("Bookcase",new IntVec3(62,0,44));outfit=Spawn("Building_OutfitStand",new IntVec3(64,0,48));
        steel=ThingMaker.MakeThing(ThingDefOf.Steel);steel.stackCount=30;GenSpawn.Spawn(steel,shelf.Position,map);
        book=ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefsListForReading.First(d=>d.category==ThingCategory.Item&&typeof(Book).IsAssignableFrom(d.thingClass)));
        Check(((ISearchableContents)bookcase).SearchableContents.TryAdd(book,false),"Book fixture rejected");bookId=book.thingIDNumber;bookLabel=book.Label;
        shirt=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_BasicShirt"),ThingDefOf.Cloth);shirt.TryGetComp<CompQuality>().SetQuality(QualityCategory.Excellent,ArtGenerationContext.Colony);
        Check((bool)AccessTools.Method(outfit.GetType(),"AddApparel").Invoke(outfit,new object[]{shirt}),"Outfit fixture rejected");
        foreach(var b in new[]{shelf,bookcase,outfit})b.GetComp<CompComplexSale>().forSale=true;
        buyer=Guest(new IntVec3(46,0,45));other=Guest(new IntVec3(46,0,46));desk.ApplyServiceAreas();
        worker=map.mapPawns.FreeColonistsSpawned.First(p=>!p.WorkTypeIsDisabled(ComplexSales.SalesWork));
        Find.PlaySettings.useWorkPriorities=true;
        worker.Position=new IntVec3(48,0,44);worker.Notify_Teleported();
        foreach(var p in map.mapPawns.FreeColonistsSpawned)p.workSettings.SetPriority(ComplexSales.SalesWork,0);
        Check(Sales.Create(buyer,shelf,steel,3)==null,"Private stock sold without staff");
        Check(desk.SetRole(worker,ComplexRole.Seller)&&worker.workSettings.GetPriority(ComplexSales.SalesWork)==1,"Seller role did not enable sales");
        desk.Employee(worker).mode=ComplexWorkMode.ColonyOnly;
        Check(Sales.Create(buyer,shelf,steel,3)==null,"Colony-only worker accepted private order");
        desk.Employee(worker).mode=ComplexWorkMode.ComplexOnly;
        Check(Sales.WorkerEnabled(worker)&&Sales.Requestable(buyer,shelf,steel),"Complex-only worker rejected complex stock");
        Check(!RestaurantMeals.AllowedCell(buyer,shelf.Position)&&!SaleStock.Allowed(buyer,shelf,steel),"Guest gained staff-room access");
        worker.Position=new IntVec3(58,0,45);worker.Notify_Teleported();
        var walls=new CellRect(56,42,6,6).EdgeCells.Select(c=>Spawn("Wall",c)).ToArray();map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        Check(Sales.CanCollect(worker,shelf,steel,1)&&!Sales.Requestable(buyer,shelf,steel),"Worker trapped with stock accepted an unreachable handover");
        foreach(var wall in walls)wall.Destroy();map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        worker.Position=new IntVec3(48,0,44);worker.Notify_Teleported();
        worker.drafter.Drafted=true;Check(!Sales.Requestable(buyer,shelf,steel),"Drafted worker accepted order");worker.drafter.Drafted=false;Wait(worker);
        var load=ThingMaker.MakeThing(ThingDefOf.WoodLog);GenSpawn.Spawn(load,worker.Position,map);worker.carryTracker.TryStartCarry(load,1,false);
        Check(!Sales.Requestable(buyer,shelf,steel),"Worker with occupied hands accepted order");Thing dropped;worker.carryTracker.TryDropCarriedThing(worker.Position,ThingPlaceMode.Near,out dropped);
        var fake=JobMaker.MakeJob(JobDefOf.Wait);Check(worker.Reserve(book,fake),"Container item cannot be reserved");
        Check(Sales.Create(buyer,bookcase,book,1)==null,"Reserved container item sold twice");map.reservationManager.ReleaseAllForTarget(book);
        floorBefore=Floor();Request(shelf,steel,3);
        Check(Sales.Create(other,shelf,steel,1)==null,"Competing order accepted same stack");
        Check(orderId!=Order.count,"Fixture does not distinguish ID and quantity");stage=1;
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-delivery-reload");
        if(reported||Find.CurrentMap!=map||!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-delivery-probe"))return;
        try{
            if(!reload&&stage==0){Setup();return;}
            if(!reload&&stage==1){
                if(ComplexLayout.StaffOnly(desk,buyer.Position))throw new Exception("Buyer walked into staff rooms");
                if(StillWalking())return;
                Check(buyer.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Steel).Sum(t=>t.stackCount)==3&&steel.stackCount==27,"Ground delivery lost/duplicated goods: "+worker.CurJob);
                Check(HotelBillingHooks.Money(buyer)==1000-price&&Sales.earned==price&&Floor()==floorBefore+price,"Ground delivery did not conserve silver");
                Wait(buyer);shelf.GetComp<CompComplexSale>().forSale=false;
                Request(bookcase,book,1);stage=2;return;
            }
            if(!reload&&stage==2){
                if(ComplexLayout.StaffOnly(desk,buyer.Position))throw new Exception("Waiting buyer entered private stock");
                if(Order!=null&&Order.goods==null&&StillWalking())return;
                Check(Order!=null&&Order.goods==book&&worker.carryTracker.CarriedThing==book,"Container pickup failed: "+worker.CurJob);
                Check(worker.CurJob.count==orderId&&Order.count==1,"Pickup mutated order ID");
                Check(!other.CanReserve(book),"Another pawn could take carried reserved book");
                Check(HotelBillingHooks.Money(buyer)==1000-Sales.earned,"Pickup charged before handover");
                worker.drafter.Drafted=true;Sales.Process();worker.drafter.Drafted=false;Wait(worker);Wait(buyer);Sales.Process();
                Check(!book.Destroyed&&worker.carryTracker.CarriedThing==null&&SaleStock.Contents(bookcase).Contains(book),"Cancelled carry lost book");
                Check(HotelBillingHooks.Money(buyer)==1000-Sales.earned,"Cancellation charged buyer");
                Request(bookcase,book,1);stage=3;return;
            }
            if(!reload&&stage==3){
                if(Order!=null&&Order.goods==null&&StillWalking())return;
                Check(Order!=null&&Order.goods==book&&worker.carryTracker.CarriedThing==book,"Save fixture has no carried book");
                Check(worker.CurJob.count==orderId&&buyer.CurJobDef==ComplexSales.WaitJob,"Save fixture jobs invalid");
                saved=true;GameDataSaveLoader.SaveGame("TMM-Delivery");Finish(false);return;
            }
            if(reload&&stage==0){
                Check(saved&&Order!=null&&Order.delivery&&Order.worker==worker,"Saved order references lost");
                Check(Order.goods==book&&worker.carryTracker.CarriedThing==book&&worker.CurJob.count==orderId,"Saved carried goods or ID lost");
                Check(book.thingIDNumber==bookId&&book.Label==bookLabel&&Order.price==price,"Saved book identity or quote changed");
                SalesDeliveryLaunch.blockReceipts=true;started=Find.TickManager.TicksGame;stage=4;return;
            }
            if(reload&&stage==4){
                if(StillWalking())return;
                Check(Order!=null&&Order.completed&&buyer.inventory.innerContainer.Contains(book),"Saved delivery did not resume: "+worker.CurJob);
                Check(book.thingIDNumber==bookId&&book.Label==bookLabel&&!SaleStock.Contents(bookcase).Contains(book),"Delivered book identity changed");
                long prior=Sales.earned;Check(Order.GetDirectlyHeldThings().Sum(t=>t.stackCount)==price,"Blocked receipts disappeared");
                Check(!Sales.Deliver(Order,worker),"Completed delivery repeated");
                Sales.Process();Check(Sales.earned==prior,"Blocked receipt counted as income");
                SalesDeliveryLaunch.blockReceipts=false;Sales.Process();Sales.Process();
                Check(Sales.earned==prior+price&&Floor()==floorBefore+Sales.earned&&HotelBillingHooks.Money(buyer)==1000-Sales.earned,"Retried receipt duplicated money");
                Wait(buyer);Wait(worker);Request(outfit,shirt,1);stage=5;return;
            }
            if(reload&&stage==5){
                if(StillWalking())return;
                QualityCategory q;Check(buyer.inventory.innerContainer.Contains(shirt)&&shirt.TryGetQuality(out q)&&q==QualityCategory.Excellent,"Outfit delivery lost quality/identity");
                Check(HotelBillingHooks.Money(buyer)==1000-Sales.earned&&Floor()==floorBefore+Sales.earned,"Outfit delivery lost silver");
                Wait(buyer);Wait(worker);shelf.GetComp<CompComplexSale>().forSale=true;Request(shelf,steel,2);stage=6;return;
            }
            if(reload&&stage==6){
                if(Order!=null&&Order.goods==null&&StillWalking())return;
                Check(Order!=null&&Order.goods!=null&&Order.goods.stackCount==2,"Partial ground pickup failed");
                var carried=Order.goods;var money=HotelBillingHooks.Money(buyer);var earned=Sales.earned;
                shelf.GetComp<CompComplexSale>().forSale=false;Sales.Process();Wait(buyer);Wait(worker);Sales.Process();
                Check(worker.carryTracker.CarriedThing==null,"Flag-off cancellation retained goods in employee's hands");
                Check(HotelBillingHooks.Money(buyer)==money&&Sales.earned==earned,"Flag-off cancellation made money");
                Check(map.listerThings.ThingsOfDef(ThingDefOf.Steel).Where(t=>t.Position.DistanceTo(shelf.Position)<4).Sum(t=>t.stackCount)==27,"Cancellation did not conserve source quantity");
                desk.Employee(worker).mode=ComplexWorkMode.Both;shelf.GetComp<CompComplexSale>().forSale=true;
                Request(shelf,steel,2);stage=7;return;
            }
            if(reload&&stage==7){
                if(Order!=null&&Order.goods==null&&StillWalking())return;
                Check(Order!=null&&Order.goods!=null,"Money-change fixture did not pick up goods");
                foreach(var silver in buyer.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray())buyer.inventory.innerContainer.TryTransferToContainer(silver,other.inventory.innerContainer,silver.stackCount,false);
                started=Find.TickManager.TicksGame;stage=8;return;
            }
            if(reload&&stage==8){
                if(StillWalking())return;
                Check(buyer.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Steel).Sum(t=>t.stackCount)==3,"Buyer without money received goods");
                Wait(buyer);Wait(worker);Sales.Process();
                Check(HotelBillingHooks.Money(buyer)==0&&HotelBillingHooks.Money(other)+Sales.earned==2000&&Floor()==floorBefore+Sales.earned,"Insufficient-funds cancellation lost silver");
                Check(worker.carryTracker.CarriedThing==null&&map.listerThings.ThingsOfDef(ThingDefOf.Steel).Where(t=>t.Position.DistanceTo(shelf.Position)<4).Sum(t=>t.stackCount)==27,"Insufficient-funds cancellation lost goods");
                foreach(var silver in other.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray())other.inventory.innerContainer.TryTransferToContainer(silver,buyer.inventory.innerContainer,silver.stackCount,false);
                worker.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);
                Check(worker.IsSlaveOfColony&&Sales.Requestable(buyer,shelf,steel),"Ordinary slave employee was excluded from sales work");
                var source=SaleStock.Contents(shelf).First(t=>t.def==ThingDefOf.Steel);Request(shelf,source,1);
                Order.created=Find.TickManager.TicksGame-6000;Sales.Process();Wait(buyer);Wait(worker);
                Check(Order==null&&worker.carryTracker.CarriedThing==null,"6000-tick boundary did not cancel pending order");
                Check(Floor()==floorBefore+Sales.earned&&HotelBillingHooks.Money(buyer)+Sales.earned==2000,"Timeout created income");
                Finish(true);
            }
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    private static string Escape(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"SalesDeliveryReloadProbe.json":"SalesDeliveryProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
    public override void MapComponentOnGUI()
    {
        if(uiReported||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-delivery-probe"))return;
        uiReported=true;string error=null;
        try{if(!"TMM.Complex.Role.Seller".CanTranslate()||ComplexSales.SalesWork.labelShort.NullOrEmpty())throw new Exception("Seller localization missing");}
        catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"SalesDeliveryUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
}
