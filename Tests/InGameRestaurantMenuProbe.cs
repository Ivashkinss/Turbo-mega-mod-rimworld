using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class MenuLaunch
{
    public static bool blockReceipts;
    static MenuLaunch(){var args=Environment.GetCommandLineArgs();if(!args.Any(a=>a=="-tmm-menu-probe"||a=="-tmm-menu-reload"))return;var h=new Harmony("tmm.test.menu");if(args.Contains("-tmm-menu-reload"))blockReceipts=true;h.Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(MenuLaunch),"Temp"));foreach(var m in typeof(GenPlace).GetMethods().Where(m=>m.Name=="TryPlaceThing"&&m.ReturnType==typeof(bool)&&m.GetParameters()[0].ParameterType==typeof(Thing)))h.Patch(m,prefix:new HarmonyMethod(typeof(MenuLaunch),"Place"));if(args.Contains("-tmm-menu-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-RestaurantMenu"));}
    private static bool Temp(ref float __result){__result=21;return false;}
    private static bool Place(Thing __0,ref bool __result){if(!blockReceipts||__0.def!=ThingDefOf.Silver)return true;__result=false;return false;}
}
public sealed class RestaurantMenuProbe:MapComponent
{
    private Building_ComplexDesk desk;private Building_ServiceCounter counter;private Pawn guest,waiter;private Thing chair,carried;private FoodPolicy policy;private IEnumerator<int> setupFlow;private int flowTick=-1;
    private int checks,stage,start,baselineFloor,quote,baselineMoney,savedId,refundMoney;private RestaurantOrder activeOrder;private Thing cancelledMeal;private bool done,ui,saved;private string error;private Dialog_RestaurantMenu dialog;private int renderFrames;
    public RestaurantMenuProbe(Map map):base(map){}
    private RestaurantService Service{get{return map.GetComponent<RestaurantService>();}}
    private int Floor{get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private static void Wait(Pawn p){var job=JobMaker.MakeJob(JobDefOf.Wait);job.expiryInterval=600000;p.jobs.StartJob(job,JobCondition.InterruptForced);}
    private Building Build(string name,IntVec3 cell){var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);}
    private void Cash(int amount){foreach(var t in guest.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray())t.Destroy();if(amount>0){var s=ThingMaker.MakeThing(ThingDefOf.Silver);s.stackCount=amount;guest.inventory.innerContainer.TryAdd(s,false);}}
    private Thing Food(ThingDef d,int x,int z,int count=1){var t=ThingMaker.MakeThing(d);t.stackCount=count;return GenSpawn.Spawn(t,new IntVec3(x,0,z),map);}
    private void ClearFood(){foreach(var t in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree).ToArray())if(t.def.ingestible!=null&&RestaurantMeals.Prepared(t.def))t.Destroy();}
    private void Cancel(){Service.Cancel(guest);Service.ProcessOrders(false);guest.jobs.StopAll();Wait(guest);}
    private Building_ComplexDesk LoadLegacy(string legacy,bool removeMenu=true){
        var def=DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk");var data=(Building_ComplexDesk)ThingMaker.MakeThing(def,def.MadeFromStuff?ThingDefOf.Steel:null);data.restaurantMeal=ThingDefOf.MealFine;
        string path=Path.Combine(GenFilePaths.SaveDataFolderPath,"MenuMigration.xml");Scribe.saver.InitSaving(path,"Test");Scribe_Deep.Look(ref data,"desk");Scribe.saver.FinalizeSaving();
        var xml=new XmlDocument();xml.Load(path);var node=xml.DocumentElement["desk"];if(removeMenu){if(node["restaurantMenu"]!=null)node.RemoveChild(node["restaurantMenu"]);if(node["restaurantMenuConfigured"]!=null)node.RemoveChild(node["restaurantMenuConfigured"]);}node["restaurantMeal"].InnerText=legacy;xml.Save(path);data=null;
        Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref data,"desk");Scribe.loader.FinalizeLoading();return data;
    }
    private IEnumerable<int> SetupSteps(){
        map.fogGrid.ClearAllFog();foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var cell in new CellRect(35,35,50,30)){foreach(var t in cell.GetThingList(map).ToArray())if(t is Building||t is Plant||t is Filth){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);map.roofGrid.SetRoof(cell,null);}
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var cell in new CellRect(36,36,25,24))area[cell]=true;
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(38,0,38));desk.area=area;desk.vipChance=0;counter=(Building_ServiceCounter)Build("TMM_ServiceCounter",new IntVec3(42,0,46));
        Build("sbz_SingleChestFridge",new IntVec3(42,0,42));Build("sbz_SingleChestFridge",new IntVec3(43,0,42));Build("sbz_SingleChestFridge",new IntVec3(44,0,42));Build("sbz_SingleChestFridge",new IntVec3(75,0,42));
        var table=Build("Table2x2c",new IntVec3(48,0,46));var cellChair=GenAdj.CellsAdjacentCardinal(table).First(c=>c.Standable(map)&&c.GetEdifice(map)==null);chair=Build("DiningChair",cellChair);map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();desk.ApplyServiceAreas();
        Check(desk.restaurantMenuConfigured&&desk.restaurantMenu.SequenceEqual(new[]{ThingDefOf.MealSimple}),"New desk default menu invalid");
        var legacy=LoadLegacy("MealFine");Check(legacy.restaurantMenuConfigured&&legacy.restaurantMenu.SequenceEqual(new[]{ThingDefOf.MealFine}),"Real legacy Scribe migration did not preserve meal");
        var gone=LoadLegacy("TMM_MissingDishFixture");Check(gone.restaurantMenuConfigured&&gone.restaurantMenu.Count==0,"Missing legacy Def silently became Simple");
        legacy.restaurantMenu.Clear();legacy.restaurantMeal=null;GenSpawn.Spawn(legacy,new IntVec3(72,0,48),map);legacy.DeSpawn();GenSpawn.Spawn(legacy,new IntVec3(73,0,48),map);Check(legacy.restaurantMenu.Count==0&&legacy.restaurantMeal==null,"Respawn restored intentionally empty menu");legacy.Destroy();gone.Destroy();
        var home=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        guest=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,home,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowAddictions:false));GenSpawn.Spawn(guest,new IntVec3(44,0,45),map);guest.inventory.innerContainer.ClearAndDestroyContents();
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{home,new IntVec3(48,0,45),new List<Pawn>{guest},map,false,false,600000});guest.GetLord().ReceiveMemo("TravelArrived");guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=1;Wait(guest);
        policy=Current.Game.foodRestrictionDatabase.MakeNewFoodRestriction();policy.filter.CopyAllowancesFrom(guest.foodRestriction.CurrentFoodPolicy.filter);guest.foodRestriction.CurrentFoodPolicy=policy;policy.filter.SetAllow(ThingDefOf.MealSimple,true);policy.filter.SetAllow(ThingDefOf.MealFine,true);policy.filter.SetAllow(DefDatabase<ThingDef>.GetNamed("MealLavish"),true);
        waiter=map.mapPawns.FreeColonistsSpawned.First(p=>!p.WorkTypeIsDisabled(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService")));foreach(var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)waiter.workSettings.SetPriority(w,0);Wait(waiter);desk.SetRole(waiter,ComplexRole.Waiter);waiter.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService"),0);desk.Employee(waiter).mode=ComplexWorkMode.ComplexOnly;waiter.pather.StopDead();waiter.Position=new IntVec3(41,0,43);waiter.Notify_Teleported();
        AccessTools.Field(map.components.First(c=>c.GetType().FullName=="Hospitality.Hospitality_MapComponent").GetType(),"guestsCanTakeFoodForFree").SetValue(map.components.First(c=>c.GetType().FullName=="Hospitality.Hospitality_MapComponent"),false);
        desk.restaurantMenu=new List<ThingDef>{ThingDefOf.MealSimple,ThingDefOf.MealFine};desk.restaurantPercent=100;ClearFood();var simple=Food(ThingDefOf.MealSimple,42,42,1);var fine=Food(ThingDefOf.MealFine,43,42,1);
        Check(RestaurantMeals.Stock(counter,simple)&&RestaurantMeals.Edible(guest,simple)&&RestaurantMeals.SeatForGuest(guest,chair,counter),"Restaurant scope/diet/seat fixture invalid");
        var stockDialog=new Dialog_RestaurantMenu(desk);simple.stackCount=75;Check((int)AccessTools.Method(typeof(Dialog_RestaurantMenu),"StockCount").Invoke(stockDialog,new object[]{ThingDefOf.MealSimple})==75,"Real UI stock count reports stacks rather than portions");simple.stackCount=1;
        int cheap=RestaurantMeals.Quote(counter,simple),expensive=RestaurantMeals.Quote(counter,fine);Check(expensive>cheap&&cheap>0,"Price fixture does not distinguish meals");Cash(cheap);var order=Service.TryOrder(guest,counter);Check(order!=null&&order.mealDef==ThingDefOf.MealSimple&&order.price==cheap,"Unaffordable preferred dish blocked cheaper meal");Cancel();yield return 0;
        Cash(100);policy.filter.SetAllow(ThingDefOf.MealFine,false);order=Service.TryOrder(guest,counter);Check(order!=null&&order.food==simple,"Forbidden menu item selected");Cancel();yield return 0;policy.filter.SetAllow(ThingDefOf.MealFine,true);
        order=Service.TryOrder(guest,counter);Check(order!=null&&order.food==fine,"Higher native preferability not selected");Cancel();yield return 0;
        desk.restaurantMenu.Clear();Check(Service.TryOrder(guest,counter)==null,"Empty menu accepted new order");desk.restaurantMenu.Add(ThingDefOf.MealSimple);fine.Destroy();var outside=Food(ThingDefOf.MealSimple,75,42);order=Service.TryOrder(guest,counter);Check(order!=null&&order.food==simple,"Colony fallback outranked stocked complex fridge");Cancel();yield return 0;simple.Destroy();order=Service.TryOrder(guest,counter);Check(order!=null&&order.food==outside,"Missing complex dish did not use colony fridge");Cancel();yield return 0;outside.Destroy();
        simple=Food(ThingDefOf.MealSimple,42,42);order=Service.TryOrder(guest,counter);Check(order!=null,"Pinned-order fixture unavailable");quote=order.price;desk.restaurantMenu=new List<ThingDef>{ThingDefOf.MealFine};desk.restaurantPercent=200;carried=simple;start=GuestDossiers.Now;stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    private void Fresh(){
        if(stage==0){if(setupFlow==null)setupFlow=SetupSteps().GetEnumerator();if(GuestDossiers.Now<=flowTick)return;flowTick=GuestDossiers.Now;setupFlow.MoveNext();return;}
        if(stage==1){var order=Service.ForGuest(guest);if(order==null)throw new Exception("Pinned order cancelled before replacement fixture reached its chair");if(guest.Position!=order.chair.Position){if(GuestDossiers.Now-start>2000)throw new Exception("Guest did not reach chair");return;}
            carried.Destroy();carried=Food(ThingDefOf.MealSimple,43,42);Food(ThingDefOf.MealFine,44,42);
            var ready=Service.ForWaiter(waiter,counter);Check(ready==order&&order.food==carried&&order.mealDef==ThingDefOf.MealSimple&&order.price==quote,"Replacement changed captured meal or price");
            var giver=new WorkGiver_Restaurant();var job=giver.JobOnThing(waiter,counter);Check(job!=null&&job.count==order.id,"Real waiter job not assigned");waiter.jobs.StartJob(job,JobCondition.InterruptForced);desk.restaurantMenu.Clear();start=GuestDossiers.Now;stage=2;return;}
        if(stage==2){if(waiter.carryTracker.CarriedThing==null){if(GuestDossiers.Now-start>2000)throw new Exception("Waiter never picked up meal");return;}
            var order=Service.ForGuest(guest);Check(order!=null&&waiter.CurJobDef==RestaurantMeals.ServeJob&&waiter.carryTracker.CarriedThing.def==order.mealDef,"In-transit real meal/job missing");
            var coin=guest.inventory.innerContainer.First(t=>t.def==ThingDefOf.Silver);Check(guest.inventory.innerContainer.TryTransferToContainer(coin,order.GetDirectlyHeldThings(),4,false)==4,"Could not stage interrupted physical prepayment");
            baselineMoney=HotelBillingHooks.Money(guest);baselineFloor=Floor;savedId=order.id;carried=waiter.carryTracker.CarriedThing;
            Current.Game.GetComponent<WorkBuyouts>().entries.Add(new WorkBuyoutEntry{pawn=waiter,target=1000,active=true,freeContract=true,contractOffered=true});
            MenuLaunch.blockReceipts=true;saved=true;GameDataSaveLoader.SaveGame("TMM-RestaurantMenu");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-RestaurantMenu")),"Real active-menu-order save missing");Finish(false);
        }
    }
    private void Reload(){
        if(stage==0){
            Check(saved&&desk.restaurantMenuConfigured&&desk.restaurantMenu.Count==0,"Explicit empty menu did not survive real game load");
            activeOrder=Service.ById(savedId);Check(activeOrder!=null&&activeOrder.mealDef==ThingDefOf.MealSimple&&activeOrder.price==quote&&activeOrder.GetDirectlyHeldThings().Sum(t=>t.stackCount)==4,"Pinned order/prepayment lost on game load");
            Check(waiter.carryTracker.CarriedThing==carried&&waiter.CurJobDef==RestaurantMeals.ServeJob&&waiter.CurJob.count==savedId,"Real carried food/job ID lost on game load");
            Check(HotelBillingHooks.Money(guest)==baselineMoney&&Floor==baselineFloor&&counter.earned==0,"Loading changed physical money");MenuLaunch.blockReceipts=true;start=GuestDossiers.Now;stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;return;
        }
        if(stage==1){
            if(!activeOrder.delivered){if(GuestDossiers.Now-start>2500)throw new Exception("Saved real waiter job did not deliver: "+waiter.CurJob+" / "+guest.CurJob);return;}
            var credit=Current.Game.GetComponent<WorkBuyouts>().Entry(waiter);
            Check(HotelBillingHooks.Money(guest)==baselineMoney-(quote-4)&&counter.earned==0&&Floor==baselineFloor&&activeOrder.GetDirectlyHeldThings().Sum(t=>t.stackCount)==quote&&credit.credited==0,"Blocked receipts lost coins or credited phantom income");
            MenuLaunch.blockReceipts=false;Service.ProcessOrders(false);Service.ProcessOrders(false);
            Check(counter.earned==quote&&desk.restaurantIncome==quote&&Floor==baselineFloor+quote&&credit.credited==quote/2&&credit.halfRemainder==quote%2,"Native paid meal did not settle exactly once at consumable credit rate");
            Check(Service.ForGuest(guest)==null,"Delivered order still blocks guest");guest.jobs.StopAll();Wait(guest);Wait(waiter);guest.pather.StopDead();guest.Position=chair.Position;guest.needs.food.CurLevelPercentage=1;ClearFood();
            desk.restaurantMenu.Add(ThingDefOf.MealSimple);desk.restaurantPercent=100;Food(ThingDefOf.MealSimple,42,42);activeOrder=Service.TryOrder(guest,counter);Check(activeOrder!=null,"Diet-change cancellation fixture missing");
            waiter.jobs.StartJob(new WorkGiver_Restaurant().JobOnThing(waiter,counter),JobCondition.InterruptForced);refundMoney=HotelBillingHooks.Money(guest);start=GuestDossiers.Now;stage=2;return;
        }
        if(stage==2){
            if(waiter.carryTracker.CarriedThing==null){if(GuestDossiers.Now-start>2500)throw new Exception("Diet-change waiter did not pick up food");return;}
            cancelledMeal=waiter.carryTracker.CarriedThing;guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingDefOf.MealSimple,false);Check(!RestaurantMeals.Edible(guest,cancelledMeal),"Actual current food policy did not forbid the carried meal");
            var coin=guest.inventory.innerContainer.First(t=>t.def==ThingDefOf.Silver);Check(guest.inventory.innerContainer.TryTransferToContainer(coin,activeOrder.GetDirectlyHeldThings(),2,false)==2,"Could not stage refundable cancellation payment");start=GuestDossiers.Now;stage=3;return;
        }
        if(stage==3){
            if(!activeOrder.cancelled){if(GuestDossiers.Now-start>2500)throw new Exception("Changed guest diet did not cancel delivery: delivered="+activeOrder.delivered+" guest="+guest.CurJob+" waiter="+waiter.CurJob+" carry="+waiter.carryTracker.CarriedThing+" samePolicy="+(policy==guest.foodRestriction.CurrentFoodPolicy)+" edible="+RestaurantMeals.Edible(guest,cancelledMeal));return;}
            Service.ProcessOrders(false);Check(!activeOrder.delivered&&HotelBillingHooks.Money(guest)==refundMoney&&Floor==baselineFloor+quote&&counter.earned==quote,"Diet cancellation charged guest or lost refund");Check(!cancelledMeal.Destroyed&&(cancelledMeal.Spawned||cancelledMeal.ParentHolder!=null),"Cancelled meal disappeared");
            guest.foodRestriction.CurrentFoodPolicy.filter.SetAllow(ThingDefOf.MealSimple,true);guest.jobs.StopAll();Wait(guest);Wait(waiter);guest.pather.StopDead();guest.Position=chair.Position;guest.needs.food.CurLevelPercentage=1;ClearFood();Food(ThingDefOf.MealSimple,43,42);desk.restaurantPercent=0;
            activeOrder=Service.TryOrder(guest,counter);Check(activeOrder!=null&&activeOrder.price==0,"Free menu order missing");waiter.jobs.StartJob(new WorkGiver_Restaurant().JobOnThing(waiter,counter),JobCondition.InterruptForced);start=GuestDossiers.Now;stage=4;return;
        }
        if(stage==4){if(!activeOrder.delivered){if(GuestDossiers.Now-start>2500)throw new Exception("Free meal did not deliver");return;}
            var credit=Current.Game.GetComponent<WorkBuyouts>().Entry(waiter);Check(HotelBillingHooks.Money(guest)==refundMoney&&Floor==baselineFloor+quote&&counter.earned==quote&&credit.credited==quote/2,"Free meal generated money/credit");
            desk.restaurantPercent=100;desk.restaurantMenu=new List<ThingDef>{ThingDefOf.MealSimple,ThingDefOf.MealFine};Finish(true);
        }
    }
    public override void ExposeData(){base.ExposeData();Scribe_References.Look(ref desk,"mpDesk");Scribe_References.Look(ref counter,"mpCounter");Scribe_References.Look(ref guest,"mpGuest");Scribe_References.Look(ref waiter,"mpWaiter");Scribe_References.Look(ref chair,"mpChair");Scribe_References.Look(ref carried,"mpCarried");Scribe_References.Look(ref policy,"mpPolicy");Scribe_Values.Look(ref saved,"mpSaved");Scribe_Values.Look(ref quote,"mpQuote");Scribe_Values.Look(ref savedId,"mpOrderId");Scribe_Values.Look(ref baselineFloor,"mpFloor");Scribe_Values.Look(ref baselineMoney,"mpMoney");}
    public override void MapComponentUpdate(){bool reload=Environment.GetCommandLineArgs().Contains("-tmm-menu-reload");if(done||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-menu-probe")))return;try{if(reload)Reload();else Fresh();}catch(Exception e){error=e.ToString();Finish(reload);}}
    private void Finish(bool reload){done=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"RestaurantMenuReloadProbe.json":"RestaurantMenuProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"errors\":"+(error==null?"[]":"["+Q(error)+"]")+"}");}
    public override void MapComponentOnGUI(){if(!done||desk==null||Event.current.type!=EventType.Repaint)return;
        if(ui){renderFrames++;if(renderFrames==2){dialog=new Dialog_RestaurantMenu(desk);Find.WindowStack.Add(dialog);}if(renderFrames==12){AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"),"CaptureScreenshot",new Type[]{typeof(string)}).Invoke(null,new object[]{Path.Combine(GenFilePaths.SaveDataFolderPath,"RestaurantMenu.png")});}if(renderFrames==24){dialog.Close();Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;}return;}
        ui=true;string e=null;try{var f=Text.Font;var a=Text.Anchor;var c=GUI.color;bool w=Text.WordWrap;new Dialog_RestaurantMenu(desk).DoWindowContents(new Rect(0,0,544,586));if(Text.Font!=f||Text.Anchor!=a||GUI.color!=c||Text.WordWrap!=w)throw new Exception("Menu leaked GUI state");}catch(Exception x){e=x.ToString();}File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"RestaurantMenuUiProbe.json"),"{\"passed\":"+(e==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(e==null?"null":Q(e))+"}");}
}
