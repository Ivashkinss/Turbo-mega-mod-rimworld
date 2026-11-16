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
public static class PricingProbeHooks
{
    public static Pawn blockedCash;
    public static bool resume;
    static PricingProbeHooks()
    {
        var args=Environment.GetCommandLineArgs();if(!args.Contains("-tmm-pricing-probe")&&!args.Contains("-tmm-pricing-reload"))return;
        resume=!args.Contains("-tmm-pricing-reload");var h=new Harmony("tmm.test.pricing");
        h.Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(PricingProbeHooks),"Temp"));
        h.Patch(AccessTools.Method(typeof(HotelBilling),"ProcessPayments"),prefix:new HarmonyMethod(typeof(PricingProbeHooks),"Billing"));
        var drop=AccessTools.Method(typeof(ThingOwner<Thing>),"TryDrop",new[]{typeof(Thing),typeof(IntVec3),typeof(Map),typeof(ThingPlaceMode),typeof(int),typeof(Thing).MakeByRefType(),typeof(Action<Thing,int>),typeof(Predicate<IntVec3>)});
        h.Patch(drop,prefix:new HarmonyMethod(typeof(PricingProbeHooks),"Drop"));
        if(args.Contains("-tmm-pricing-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-Pricing"));
    }
    private static bool Temp(ref float __result){__result=21;return false;}
    private static bool Billing(){return resume;}
    private static bool Drop(ThingOwner<Thing> __instance,ref bool __result)
    {
        if(blockedCash==null||__instance!=blockedCash.inventory.innerContainer)return true;__result=false;return false;
    }
}
public sealed class PricingProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building_Bed bed,other,sibling;
    private Building shelf;
    private Pawn first,second,carrier;
    private SaleOrder order;
    private int stage,start,checks,initialSilver,tradeQuote,tradeMoney,savedDue,savedFloor,savedIncome;
    private bool saved,reported,ui,sleepIssued,sleepVerified;
    private readonly List<string> errors=new List<string>();
    public PricingProbe(Map map):base(map){}
    private ComplexPricing Prices {get{return map.GetComponent<ComplexPricing>();}}
    private HotelBilling Bills {get{return map.GetComponent<HotelBilling>();}}
    private HotelStay Stay(Pawn p){return Bills.stays.Single(s=>s.guest==p);}
    private int Money(Pawn p){return HotelBillingHooks.Money(p);}
    private int Silver {get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount)+map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction==Faction.OfPlayer).Sum(p=>(p.inventory==null?0:p.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).Sum(t=>t.stackCount))+(p.carryTracker!=null&&p.carryTracker.CarriedThing!=null&&p.carryTracker.CarriedThing.def==ThingDefOf.Silver?p.carryTracker.CarriedThing.stackCount:0));}}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Escape(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private static void Wait(Pawn p){var job=JobMaker.MakeJob(JobDefOf.Wait);job.expiryInterval=600000;p.jobs.StartJob(job,JobCondition.InterruptForced);}
    private Building Build(string name,IntVec3 cell)
    {
        var def=DefDatabase<ThingDef>.GetNamed(name);var t=(Building)ThingMaker.MakeThing(def,def.MadeFromStuff?ThingDefOf.Steel:null);t.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(t,cell,map);
    }
    private void Room(CellRect rect)
    {
        var door=new IntVec3(rect.CenterCell.x,0,rect.minZ);foreach(var c in rect.EdgeCells)Build(c==door?"Door":"Wall",c);
        foreach(var c in rect.ContractedBy(1))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
    }
    private void SetMoney(Pawn p,int amount)
    {
        foreach(var t in p.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray())t.Destroy();
        if(amount>0){var t=ThingMaker.MakeThing(ThingDefOf.Silver);t.stackCount=amount;p.inventory.innerContainer.TryAdd(t);}
    }
    private Pawn Guest(Faction faction,IntVec3 cell)
    {
        var p=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false));
        p.story.traits.allTraits.Clear();var colonist=map.mapPawns.FreeColonistsSpawned.First();if(p.ideo!=null&&colonist.ideo!=null)p.ideo.SetIdeo(colonist.ideo.Ideo);
        GenSpawn.Spawn(p,cell,map);p.inventory.innerContainer.ClearAndDestroyContents();SetMoney(p,500);p.needs.food.CurLevelPercentage=1;p.needs.rest.CurLevelPercentage=1;return p;
    }
    private void CheckIn(Pawn p)
    {
        Wait(p);AccessTools.Field(HotelBillingHooks.Guest(p).GetType(),"lastBedCheckTick").SetValue(HotelBillingHooks.Guest(p),GuestDossiers.Now-100000);
        var giver=Activator.CreateInstance(AccessTools.TypeByName("Hospitality.JobGiver_ClaimBed"));var job=(Job)AccessTools.Method(giver.GetType(),"TryGiveJob").Invoke(giver,new object[]{p});
        Check(job!=null&&job.targetA.Thing==bed,"Native check-in did not choose intended bed: "+job);p.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;
    }
    private void Due(int tick){foreach(var stay in Bills.stays.Where(s=>s.bed==bed)){stay.nextChargeTick=tick;stay.remaining=-1;}}
    private void PairPeriod(int due,int remaining,int fee)
    {
        Check(Stay(first).nextChargeTick==due&&Stay(second).nextChargeTick==due&&Stay(first).remaining==remaining&&Stay(second).remaining==remaining&&Stay(first).periodFee==fee&&Stay(second).periodFee==fee,"Shared period inconsistent");
    }
    private void Setup()
    {
        map.fogGrid.ClearAllFog();foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var c in new CellRect(30,30,40,32)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);
        }
        Room(new CellRect(39,39,10,10));Room(new CellRect(52,40,8,8));
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var c in new CellRect(35,34,30,25))area[c]=true;
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(40,0,36));desk.area=area;desk.vipChance=0;desk.roomRate=30;
        bed=(Building_Bed)Build("DoubleBedGuest",new IntVec3(43,0,43));sibling=(Building_Bed)Build("BedGuest",new IntVec3(46,0,44));other=(Building_Bed)Build("BedGuest",new IntVec3(55,0,43));shelf=Build("Shelf",new IntVec3(51,0,36));
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();desk.ApplyServiceAreas();
        Check(Prices.salePercent==55&&HotelBillingHooks.Fee(bed)==30&&HotelBillingHooks.Fee(other)==30,"Default prices changed");
        Prices.Set(bed,80);desk.roomRate=40;desk.ApplyServiceAreas();Check(HotelBillingHooks.Fee(bed)==80&&HotelBillingHooks.Fee(other)==40,"Individual tariff overwritten by default");
        Prices.ApplyRoom(desk,bed);Check(HotelBillingHooks.Fee(sibling)==80&&HotelBillingHooks.Fee(other)==40,"Room action affected unrelated beds");
        Prices.Set(bed,null);Check(HotelBillingHooks.Fee(bed)==40&&Prices.Override(bed)==null,"Reset did not restore default");Prices.Set(bed,80);
        var pos=bed.Position;var siblingPos=sibling.Position;map.GetComponent<ComplexLayout>().SetMedicalRoom(bed,true);
        bed=pos.GetThingList(map).OfType<Building_Bed>().Single();sibling=siblingPos.GetThingList(map).OfType<Building_Bed>().Single();
        Check(!ComplexLayout.IsGuest(bed)&&bed.Medical&&Prices.Override(bed)!=null&&Prices.Override(bed).rate==80,"Guest-to-medical swap lost tariff");
        map.GetComponent<ComplexLayout>().SetMedicalRoom(bed,false);bed=pos.GetThingList(map).OfType<Building_Bed>().Single();sibling=siblingPos.GetThingList(map).OfType<Building_Bed>().Single();
        Check(ComplexLayout.IsGuest(bed)&&HotelBillingHooks.Fee(bed)==80&&Prices.Override(sibling).rate==80,"Medical-to-guest swap lost tariff");
        // Price/room/swap comparisons are complete. Remove competing beds
        // from this disposable fixture before testing native shared check-in.
        bed=map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Single(b=>b.Spawned&&!b.Destroyed&&b.Position==pos&&b.def.defName=="DoubleBedGuest");
        Check(Prices.Override(bed)!=null&&Prices.Override(bed).rate==80,"Live swapped double bed lost its actual tariff reference");
        var medical=map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Single(b=>b.Spawned&&!b.Destroyed&&b.Position==new IntVec3(55,0,43));
        map.GetComponent<ComplexLayout>().SetMedicalRoom(medical,true);
        Check(map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Any(b=>b.Position==new IntVec3(55,0,43)&&b.Medical),"Medical comparison bed not prepared");
        foreach(var comparison in map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Where(b=>b.Spawned&&!b.Destroyed&&b!=bed).ToArray())comparison.Destroy();
        Check(map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Count(b=>b.Spawned&&!b.Destroyed&&b!=bed)==0,"Destroy recreated a managed comparison bed");
        sibling=null;other=null;
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        first=Guest(faction,new IntVec3(43,0,37));second=Guest(faction,new IntVec3(44,0,37));first.relations.AddDirectRelation(PawnRelationDefOf.Spouse,second);
        foreach(var t in map.attackTargetsCache.TargetsHostileToFaction(faction).Select(t=>t.Thing).Where(t=>t.Spawned).ToArray())t.DeSpawn();
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(43,0,37),new List<Pawn>{first,second},map,false,false,600000});first.GetLord().ReceiveMemo("TravelArrived");
        first.needs.joy.CurLevelPercentage=1;second.needs.joy.CurLevelPercentage=1;Wait(first);Wait(second);
        Check(RimWorld.BedUtility.WillingToShareBed(first,second)&&RimWorld.BedUtility.WillingToShareBed(second,first)&&first.relations.OpinionOf(second)>=15&&second.relations.OpinionOf(first)>=15,"Fixture partners not eligible for native shared bed");
        initialSilver=Silver;stage=1;start=GuestDossiers.Now;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    private void Fresh()
    {
        if(stage==0){Setup();return;}
        if(stage==1){if(GuestDossiers.Now<=start+1)return;CheckIn(first);stage=2;return;}
        if(stage==2){
            if(HotelBillingHooks.Bed(first)!=bed){if(GuestDossiers.Now-start>2000)throw new Exception("First check-in timeout");return;}
            Check(Money(first)==420&&Silver==initialSilver+80&&desk.hotelIncome==80,"First whole-bed payment wrong");
            Check(HotelBillingHooks.Fee(bed)==80&&Bills.EntryFee(bed)==0,"Paid entry quote changed stored tariff");
            Check(HotelBillingHooks.ShareAllowed(bed,second)&&!HotelBillingHooks.ShareAllowed(bed,map.mapPawns.FreeColonistsSpawned.First()),"Shared double-bed relationship restriction failed");
            Prices.Set(bed,120);Bills.ProcessPayments();Check(Money(first)==420&&desk.hotelIncome==80,"Paid period repriced");
            CheckIn(second);stage=3;return;
        }
        if(stage==3){
            if(HotelBillingHooks.Bed(second)!=bed){if(GuestDossiers.Now-start>2000)throw new Exception("Second check-in timeout: "+second.CurJob);return;}
            if(!sleepVerified){
                if(!sleepIssued){
                    first.needs.rest.CurLevelPercentage=0.25f;second.needs.rest.CurLevelPercentage=0.25f;
                    first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown,bed),JobCondition.InterruptForced);
                    second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown,bed),JobCondition.InterruptForced);sleepIssued=true;start=GuestDossiers.Now;return;
                }
                if(first.CurrentBed()!=bed||second.CurrentBed()!=bed){if(GuestDossiers.Now-start>1500)throw new Exception("Partners failed actual shared sleeping: "+first.CurJob+" / "+second.CurJob);return;}
                Check(first.Position!=second.Position,"Partners occupy the same sleeping slot");sleepVerified=true;
                Wait(first);Wait(second);first.needs.rest.CurLevelPercentage=1;second.needs.rest.CurLevelPercentage=1;
            }
            Check(Money(second)==500&&Money(first)==420&&desk.hotelIncome==80&&Silver==initialSilver+80,"Second occupant charged bed again");PairPeriod(Stay(first).nextChargeTick,-1,80);
            Check((int)AccessTools.Property(AccessTools.TypeByName("Hospitality.Building_GuestBed"),"RentalFee").GetValue(bed,null)==120,"Quote scope leaked into ordinary bed price widget");
            var goods=ThingMaker.MakeThing(ThingDefOf.Steel);goods.stackCount=10;GenSpawn.Spawn(goods,shelf.Position,map);shelf.GetComp<CompComplexSale>().forSale=true;
            var sales=map.GetComponent<ComplexSales>();order=sales.Create(first,shelf,goods,3);Check(order!=null,"Could not quote shop order");tradeQuote=order.price;tradeMoney=Money(first);
            Prices.salePercent=200;Check(order.price==tradeQuote&&SaleStock.Price(SaleStock.UnitPrice(goods),3)>tradeQuote,"New market percentage rewrote accepted quote");
            var job=JobMaker.MakeJob(ComplexSales.BuyJob,shelf);job.count=order.id;first.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;stage=4;return;
        }
        if(stage==4){
            if(!order.completed){if(GuestDossiers.Now-start>2500)throw new Exception("Quoted order did not finish: "+first.CurJob);return;}
            Check(Money(first)==tradeMoney-tradeQuote&&map.GetComponent<ComplexSales>().earned==tradeQuote&&first.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Steel).Sum(t=>t.stackCount)==3,"Accepted quote not honored in real sale");
            Wait(first);Wait(second);Due(GuestDossiers.Now);int due=GuestDossiers.Now,before=Money(first)+Money(second);Bills.ProcessPayments();Bills.ProcessPayments();
            Check(Money(first)+Money(second)==before-120&&desk.hotelIncome==200,"Renewal charged fee per occupant");PairPeriod(due+HotelBilling.DayTicks,-1,120);
            Stay(first).nextChargeTick=GuestDossiers.Now+4000;Stay(second).nextChargeTick=GuestDossiers.Now+5000;Bills.ProcessPayments();Check(Stay(first).nextChargeTick==GuestDossiers.Now+5000&&Stay(second).nextChargeTick==GuestDossiers.Now+5000&&desk.hotelIncome==200,"Legacy paid deadlines not merged conservatively");
            carrier=map.mapPawns.FreeColonistsSpawned.First(p=>p.carryTracker.CarriedThing==null);Check(carrier.carryTracker.TryStartCarry(first,1,false)==1,"Could not hold shared occupant");
            Due(GuestDossiers.Now);before=Money(first)+Money(second);Bills.ProcessPayments();Check(Bills.stays.Count(s=>s.bed==bed)==2&&Money(first)+Money(second)==before&&Stay(second).nextChargeTick==GuestDossiers.Now,"Carry lost shared period or collected payment");
            Thing dropped;Check(carrier.carryTracker.TryDropCarriedThing(new IntVec3(43,0,37),ThingPlaceMode.Near,out dropped),"Could not restore held occupant");
            Prices.Set(bed,80);SetMoney(first,20);SetMoney(second,100);PricingProbeHooks.blockedCash=second;Bills.ProcessPayments();PairPeriod(due,60,80);
            Check(Money(first)==0&&Money(second)==100&&desk.hotelIncome==220,"Partial bed payment not physical/exact");
            Prices.Set(bed,180);Bills.ProcessPayments();PairPeriod(due,60,80);Check(desk.hotelIncome==220,"Pending debt repriced or duplicated");
            saved=true;savedDue=due;savedFloor=Silver;savedIncome=desk.hotelIncome;GameDataSaveLoader.SaveGame("TMM-Pricing");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Pricing")),"Pricing fixture save missing");Finish(false);
        }
    }
    private void Reload()
    {
        if(stage==0){
            Check(saved&&HotelBillingHooks.Bed(first)==bed&&HotelBillingHooks.Bed(second)==bed,"Shared native ownership failed load");
            Check(Prices.salePercent==200&&Prices.Override(bed)!=null&&Prices.Override(bed).rate==180&&HotelBillingHooks.Fee(bed)==180,"Pricing preferences failed load");PairPeriod(savedDue,60,80);
            Check(Money(first)==0&&Money(second)==100&&Silver==savedFloor&&desk.hotelIncome==savedIncome,"Load changed pending payment");
            PricingProbeHooks.resume=true;Bills.ProcessPayments();Bills.ProcessPayments();PairPeriod(savedDue+HotelBilling.DayTicks,-1,80);
            Check(Money(first)==0&&Money(second)==40&&Silver==savedFloor+60&&desk.hotelIncome==savedIncome+60,"Pending shared payment not completed once");
            Check(GuestDossiers.Book.records.Sum(r=>r.hotel)==desk.hotelIncome,"Dossier double-counted shared rent");
            int due=Stay(second).nextChargeTick;first.ownership.UnclaimBed();Bills.ProcessPayments();Check(!Bills.stays.Any(s=>s.guest==first)&&Stay(second).nextChargeTick==due&&Bills.EntryFee(bed)==0,"One departure reset remaining guest's paid bed");
            second.ownership.UnclaimBed();Bills.ProcessPayments();Check(!Bills.stays.Any(s=>s.bed==bed)&&Bills.EntryFee(bed)==180,"Empty bed retained free booking");
            SetMoney(first,500);SetMoney(second,0);CheckIn(first);stage=1;return;
        }
        if(stage==1){if(HotelBillingHooks.Bed(first)!=bed){if(GuestDossiers.Now-start>2000)throw new Exception("New first check-in timed out");return;}Check(Money(first)==320&&desk.hotelIncome==savedIncome+240,"New empty-bed booking did not pay current tariff");CheckIn(second);stage=2;return;}
        if(stage==2){
            if(HotelBillingHooks.Bed(second)!=bed){if(GuestDossiers.Now-start>2000)throw new Exception("Zero-money partner could not join paid bed");return;}
            Check(Money(second)==0&&desk.hotelIncome==savedIncome+240,"Zero-money partner charged");Wait(first);Wait(second);SetMoney(first,20);SetMoney(second,30);Due(GuestDossiers.Now);int floor=Silver,income=desk.hotelIncome;Bills.ProcessPayments();
            Check(HotelBillingHooks.Bed(first)==null&&HotelBillingHooks.Bed(second)==null&&!Bills.stays.Any(s=>s.bed==bed),"Insufficient shared budget did not release both occupants");
            Check(Money(first)==20&&Money(second)==30&&Silver==floor&&desk.hotelIncome==income&&!first.HostileTo(Faction.OfPlayer),"Failed renewal took coins/invented income/started war");
            Wait(second);Prices.Set(bed,0);CheckIn(first);stage=3;return;
        }
        if(stage==3){
            if(HotelBillingHooks.Bed(first)!=bed){if(GuestDossiers.Now-start>2000)throw new Exception("Free bed check-in timed out");return;}
            int income=desk.hotelIncome;Due(GuestDossiers.Now);Bills.ProcessPayments();Check(Money(first)==20&&desk.hotelIncome==income&&Stay(first).remaining==-1,"Free bed created money/debt");
            Prices.Set(bed,10);SetMoney(first,100);Due(GuestDossiers.Now-5*HotelBilling.DayTicks);int oldDue=Stay(first).nextChargeTick;Bills.ProcessPayments();
            Check(Money(first)==60&&Stay(first).nextChargeTick==oldDue+4*HotelBilling.DayTicks&&desk.hotelIncome==income+40,"Catch-up not bounded to four bed periods");Bills.ProcessPayments();Bills.ProcessPayments();Check(Money(first)==40&&desk.hotelIncome==income+60,"Catch-up repeated an already paid period");Finish(true);
        }
    }
    public override void ExposeData()
    {
        base.ExposeData();Scribe_References.Look(ref desk,"prDesk");Scribe_References.Look(ref bed,"prBed");Scribe_References.Look(ref other,"prOther");Scribe_References.Look(ref sibling,"prSibling");Scribe_References.Look(ref first,"prFirst");Scribe_References.Look(ref second,"prSecond");
        Scribe_Values.Look(ref saved,"prSaved");Scribe_Values.Look(ref savedDue,"prDue");Scribe_Values.Look(ref savedFloor,"prFloor");Scribe_Values.Look(ref savedIncome,"prIncome");
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-pricing-reload");if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-pricing-probe")))return;
        try{if(reload)Reload();else Fresh();}catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    public override void MapComponentOnGUI()
    {
        if(ui||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-pricing-probe"))return;
        ui=true;string error=null;try{new Dialog_ComplexPrices(desk).DoWindowContents(new Rect(0,0,1000,740));new Dialog_ComplexServices(desk){page=0}.DoWindowContents(new Rect(0,0,740,720));}catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"PricingUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"PricingReloadProbe.json":"PricingProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
}
