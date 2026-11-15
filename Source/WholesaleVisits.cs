using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public sealed class LordToil_WholesaleTrading : LordToil
    {
        public override bool AllowRestingInBed { get { return false; } }
        public override void UpdateAllDuties()
        {
            var visit=lord.LordJob as LordJob_WholesaleVisit;
            if(visit==null)return;
            foreach(var p in lord.ownedPawns)p.mindState.duty=p==visit.leader?
                new PawnDuty(DutyDefOf.Defend,visit.spot,3f):new PawnDuty(DutyDefOf.Follow,visit.leader,3f);
        }
    }
    public sealed class LordJob_WholesaleVisit : LordJob
    {
        public Pawn leader;
        public IntVec3 spot;
        public int id,started,deadline,spent,lastAction,noOrderSince=-1;
        public bool leaving,aborted,reported;
        public LordJob_WholesaleVisit() { }
        public IEnumerable<Pawn> Members { get { return lord==null?Enumerable.Empty<Pawn>():lord.ownedPawns.Where(p=>p!=null); } }
        public Faction Faction { get { return lord==null?null:lord.faction; } }
        public bool Shopping { get { return lord!=null&&!leaving&&lord.CurLordToil is LordToil_WholesaleTrading; } }
        public override bool CanOpenAnyDoor(Pawn p) { return false; }
        public override StateGraph CreateGraph()
        {
            var graph=new StateGraph();var approach=new LordToil_Travel(spot);var shop=new LordToil_WholesaleTrading();
            var exit=new LordToil_ExitMap(LocomotionUrgency.Jog,false,true);
            graph.AddToil(approach);graph.AddToil(shop);graph.AddToil(exit);
            var arrived=new Transition(approach,shop);arrived.AddTrigger(new Trigger_Memo("TravelArrived"));graph.AddTransition(arrived);
            var done=new Transition(approach,exit);done.AddSource(shop);done.AddTrigger(new Trigger_Memo("TMM_WholesaleExit"));
            done.AddPreAction(new TransitionAction_Custom((Action)delegate { leaving=true; }));graph.AddTransition(done);
            return graph;
        }
        public void Leave(bool abort)
        {
            if(leaving||lord==null)return;
            aborted=abort;leaving=true;lord.ReceiveMemo("TMM_WholesaleExit");
            if(!reported){reported=true;Messages.Message("TMM.Wholesale.VisitReport".Translate(lord.faction.Name,spent),leader,MessageTypeDefOf.NeutralEvent,false);}
            foreach(var p in Members.ToArray()){
                var map=p.Map;if(map==null)continue;
                var sales=map.GetComponent<ComplexSales>();
                foreach(var o in sales.orders.Where(o=>o.buyer==p&&!o.completed).ToArray())sales.Cancel(o);
                sales.Process();
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();Scribe_References.Look(ref leader,"leader");Scribe_Values.Look(ref spot,"spot");
            Scribe_Values.Look(ref id,"id");Scribe_Values.Look(ref started,"started");Scribe_Values.Look(ref deadline,"deadline");
            Scribe_Values.Look(ref spent,"spent");Scribe_Values.Look(ref lastAction,"lastAction");Scribe_Values.Look(ref noOrderSince,"noOrderSince",-1);
            Scribe_Values.Look(ref leaving,"leaving");Scribe_Values.Look(ref aborted,"aborted");Scribe_Values.Look(ref reported,"reported");
        }
    }
    public sealed class WholesaleVisits : MapComponent
    {
        public int nextVisitTick=-1,lastVisitTick=-1,nextId,retryAfterTick;
        private int nextCheck;
        public WholesaleVisits(Map map):base(map) { }
        public IEnumerable<LordJob_WholesaleVisit> Groups { get { return map.lordManager.lords.Select(l=>l.LordJob).OfType<LordJob_WholesaleVisit>(); } }
        public override void ExposeData()
        {
            base.ExposeData();Scribe_Values.Look(ref nextVisitTick,"TMM_wholesaleNext",-1);Scribe_Values.Look(ref lastVisitTick,"TMM_wholesaleLast",-1);Scribe_Values.Look(ref nextId,"TMM_wholesaleId");
            Scribe_Values.Look(ref retryAfterTick,"TMM_wholesaleRetry");
        }
        public static int Delay(double value) { return (int)Math.Ceiling(60000*Math.Max(0.75,4/Math.Sqrt(1+Math.Max(0,value)/2500))); }
        public static int Budget(double value) { return value>=60000?30000:value<=2000?1000:(int)Math.Ceiling(value/2); }
        public double StockValue()
        {
            var sales=map.GetComponent<ComplexSales>();var seen=new HashSet<int>();double total=0;
            foreach(var store in SaleStock.Stores(map)){
                if(!SaleStock.Public(store)&&!map.mapPawns.AllPawnsSpawned.Any(p=>sales.WorkerEnabled(p)&&!store.IsForbidden(p)&&p.CanReach(store,PathEndMode.Touch,Danger.None)))continue;
                foreach(var item in SaleStock.Contents(store).Where(SaleStock.Tradeable))if(seen.Add(item.thingIDNumber)){
                    int count=sales.FreeCount(item);if(count<=0)continue;
                    double value=(double)item.MarketValue*count;if(double.IsNaN(value)||double.IsInfinity(value)||value<=0)continue;total+=value;
                }
            }
            return total;
        }
        public static IEnumerable<PawnGroupMaker> Makers(Faction faction)
        {
            return faction==null||faction.def.pawnGroupMakers==null?Enumerable.Empty<PawnGroupMaker>():
                faction.def.pawnGroupMakers.Where(m=>m!=null&&m.kindDef==PawnGroupKindDefOf.Trader&&m.commonality>0&&!float.IsInfinity(m.commonality)&&
                    m.traders!=null&&m.traders.Any(o=>Option(o)&&Human(o.kind)));
        }
        private static bool Human(PawnKindDef kind) { return kind!=null&&kind.race!=null&&kind.race.race!=null&&kind.RaceProps.Humanlike&&kind.RaceProps.ToolUser; }
        private static bool Option(PawnGenOption o) { return o!=null&&o.kind!=null&&o.selectionWeight>0&&!float.IsInfinity(o.selectionWeight); }
        private static PawnKindDef Pick(IEnumerable<PawnGenOption> options,Func<PawnKindDef,bool> valid)
        {
            if(options==null)return null;var values=options.Where(o=>Option(o)&&valid(o.kind)).ToList();
            return values.Count==0?null:values.RandomElementByWeight(o=>o.selectionWeight).kind;
        }
        private Pawn Generate(PawnKindDef kind,Faction faction)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind,faction,forceGenerateNewPawn:true,allowDead:false,allowDowned:false,
                canGeneratePawnRelations:false,allowAddictions:false,allowPregnant:false,developmentalStages:DevelopmentalStage.Adult));
        }
        public LordJob_WholesaleVisit StartGroup(Faction faction,IntVec3 entry,IntVec3 point,List<Pawn> pawns,int silverToAdd,bool notify)
        {
            if(pawns==null||faction==null||faction.IsPlayer||faction.HostileTo(Faction.OfPlayer))return null;
            var leader=pawns.FirstOrDefault(p=>p!=null&&p.RaceProps.Humanlike&&!p.Dead&&!p.Downed&&p.inventory!=null&&MassUtility.CanEverCarryAnything(p));
            if(leader==null||!entry.InBounds(map)||!point.InBounds(map)||pawns.Any(p=>p==null||p.Spawned||Find.WorldPawns.Contains(p)))return null;
            int remaining=Math.Max(0,silverToAdd);
            foreach(var p in pawns.Where(p=>p.inventory!=null&&MassUtility.CanEverCarryAnything(p))){
                float unit=ThingDefOf.Silver.GetStatValueAbstract(StatDefOf.Mass);
                int count=unit>0?Math.Min(remaining,(int)Math.Max(0,Math.Floor(Math.Max(0,MassUtility.FreeSpace(p)-5f)/unit))):0;
                while(count>0){var cash=ThingMaker.MakeThing(ThingDefOf.Silver);cash.stackCount=Math.Min(count,ThingDefOf.Silver.stackLimit);
                    int coins=cash.stackCount;if(!p.inventory.innerContainer.TryAdd(cash,false)){cash.Destroy();break;}remaining-=coins;count-=coins;}
            }
            if(!pawns.Any(p=>p.inventory!=null&&HotelBillingHooks.Money(p)>0))return null;
            foreach(var p in pawns)GenSpawn.Spawn(p,entry,map);
            if(pawns.Any(p=>!p.CanReach(point,PathEndMode.OnCell,Danger.Deadly))){foreach(var p in pawns)p.DeSpawn();return null;}
            var visit=new LordJob_WholesaleVisit{leader=leader,spot=point,id=++nextId,started=Find.TickManager.TicksGame,deadline=Find.TickManager.TicksGame+60000};
            LordMaker.MakeNewLord(faction,visit,map,pawns);
            if(notify)Find.LetterStack.ReceiveLetter("TMM.Wholesale.ArrivalLabel".Translate(),"TMM.Wholesale.ArrivalText".Translate(faction.Name,
                pawns.Where(p=>p.inventory!=null).Sum(p=>HotelBillingHooks.Money(p))),LetterDefOf.NeutralEvent,leader);
            return visit;
        }
        public bool TryVisit()
        {
            var desk=ComplexLayout.Desk(map);if(desk==null||!map.IsPlayerHome||Groups.Count()>=2)return false;
            var space=AccessTools.TypeByName("Hospitality.Utilities.SpaceMapUtility");if(space!=null&&(bool)AccessTools.Method(space,"IsSpaceMap").Invoke(null,new object[]{map}))return false;
            double value=StockValue();if(value<=0)return false;
            var point=GenRadial.RadialCellsAround(desk.Position,4f,true).Where(c=>c.InBounds(map)&&c.Standable(map)&&desk.area[c]&&!ComplexLayout.StaffOnly(desk,c)).DefaultIfEmpty(IntVec3.Invalid).First();
            if(!point.IsValid)return false;
            var edge=new CellRect(0,0,map.Size.x,map.Size.z).EdgeCells.OrderBy(c=>c.DistanceToSquared(point)).Where(c=>c.Standable(map)&&
                map.reachability.CanReach(c,point,PathEndMode.OnCell,TraverseParms.For(TraverseMode.PassDoors))).DefaultIfEmpty(IntVec3.Invalid).First();
            if(!edge.IsValid)return false;
            var factions=Find.FactionManager.AllFactionsListForReading.Where(f=>!f.IsPlayer&&!f.defeated&&!f.HostileTo(Faction.OfPlayer)&&Makers(f).Any()).ToList();
            if(factions.Count==0)return false;var faction=factions.RandomElement();var maker=Makers(faction).RandomElementByWeight(m=>m.commonality);var pawns=new List<Pawn>();
            try{
                var kind=Pick(maker.traders,Human);if(kind==null)return false;pawns.Add(Generate(kind,faction));
                for(int n=0;n<2;n++){var guard=Pick(maker.guards,Human)??kind;pawns.Add(Generate(guard,faction));}
                int wanted=Budget(value);
                for(int n=0;n<4&&pawns.Sum(p=>MassUtility.FreeSpace(p))<wanted*ThingDefOf.Silver.GetStatValueAbstract(StatDefOf.Mass)+50;n++){
                    var carrier=Pick(maker.carriers,k=>k!=null&&k.RaceProps.packAnimal)??kind;pawns.Add(Generate(carrier,faction));
                }
                return StartGroup(faction,edge,point,pawns,wanted,true)!=null;
            }finally{foreach(var p in pawns.Where(p=>!p.Spawned))Find.WorldPawns.PassToWorld(p,PawnDiscardDecideMode.Discard);}
        }
        public void ProcessVisits()
        {
            int now=Find.TickManager.TicksGame;var desk=ComplexLayout.Desk(map);var sales=map.GetComponent<ComplexSales>();
            foreach(var g in Groups.ToArray()){
                if(g.leaving)continue;
                if(desk==null||g.leader==null||!g.leader.Spawned||g.leader.Downed||g.leader.InMentalState||g.leader.IsPrisoner||g.leader.IsSlave||
                    g.leader.Faction!=g.Faction||g.leader.Faction.HostileTo(Faction.OfPlayer)||
                    now>=g.deadline||!g.spot.InBounds(map)||!desk.area[g.spot]||ComplexLayout.StaffOnly(desk,g.spot)){g.Leave(true);continue;}
                if(!g.Shopping){if(now-g.started>=15000)g.Leave(true);continue;}
                if(sales.orders.Any(o=>o.buyer==g.leader&&!o.completed&&!o.cancelled))continue;
                if(now-g.lastAction<120||!g.leader.Awake()||g.leader.mindState.enemyTarget!=null)continue;
                var current=g.leader.CurJob;
                if(current!=null&&!current.def.casualInterruptible&&current.def!=JobDefOf.Wait&&current.def.defName!="Wait_Combat")continue;
                g.lastAction=now;var job=sales.ShoppingJob(g.leader,t=>true);
                if(job!=null){g.noOrderSince=-1;g.leader.jobs.StartJob(job,JobCondition.InterruptOptional);}
                else{if(g.noOrderSince<0)g.noOrderSince=now;if(now-g.noOrderSince>=1200)g.Leave(false);}
            }
        }
        public override void MapComponentTick()
        {
            if(Find.TickManager.TicksGame<nextCheck)return;nextCheck=Find.TickManager.TicksGame+120;ProcessVisits();
            if(ComplexLayout.Desk(map)==null||!map.IsPlayerHome)return;
            int now=Find.TickManager.TicksGame;double value=StockValue();if(value<=0){nextVisitTick=-1;return;}
            int delay=Delay(value);if(nextVisitTick<0)nextVisitTick=now+delay;
            if(lastVisitTick>=0)nextVisitTick=Math.Min(nextVisitTick,lastVisitTick+delay);
            else nextVisitTick=Math.Min(nextVisitTick,now+delay);
            if(now<nextVisitTick||now<retryAfterTick)return;
            if(TryVisit()){lastVisitTick=now;nextVisitTick=now+delay;retryAfterTick=0;}else retryAfterTick=now+2500;
        }
    }
    [StaticConstructorOnStartup]
    public static class WholesaleVisitHooks
    {
        static WholesaleVisitHooks(){var h=new Harmony("turbomegamod.wholesale.visits");h.Patch(AccessTools.Method(typeof(Pawn),"GetInspectString"),postfix:new HarmonyMethod(typeof(WholesaleVisitHooks),"Inspect"));
            h.Patch(AccessTools.Method(typeof(ForbidUtility),"InAllowedArea"),postfix:new HarmonyMethod(typeof(WholesaleVisitHooks),"Allowed"));}
        private static void Allowed(IntVec3 __0,Pawn __1,ref bool __result)
        {
            if(__result&&WholesaleTrade.Group(__1)!=null&&__1.Map!=null&&ComplexLayout.StaffOnly(ComplexLayout.Desk(__1.Map),__0))__result=false;
        }
        private static void Inspect(Pawn __instance,ref string __result)
        {
            var group=WholesaleTrade.Group(__instance);if(group==null)return;
            string status=group.leaving?(group.aborted?"Aborted":"Leaving"):group.Shopping?"Shopping":"Approaching";
            __result+="\n"+("TMM.Wholesale."+status).Translate()+"\n"+"TMM.Wholesale.Inspect".Translate(group.Members.Where(p=>p.inventory!=null).Sum(p=>HotelBillingHooks.Money(p)),group.spent);
        }
    }
}
