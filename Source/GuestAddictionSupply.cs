using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public sealed class GuestAddictionSearch : MapComponent
    {
        private readonly Dictionary<int,int> retry=new Dictionary<int,int>();
        public GuestAddictionSearch(Map map):base(map){}
        public bool Waiting(Pawn p){int tick;return retry.TryGetValue(p.thingIDNumber,out tick)&&GuestDossiers.Now<tick;}
        public void Failed(Pawn p){retry[p.thingIDNumber]=GuestDossiers.Now+1000;}
        public override void MapComponentTick(){if(GuestDossiers.Now%2500==0)foreach(var id in retry.Where(k=>k.Value<=GuestDossiers.Now).Select(k=>k.Key).ToArray())retry.Remove(id);}
    }
    [StaticConstructorOnStartup]
    public static class GuestAddictionSupply
    {
        private static readonly System.Reflection.PropertyInfo addiction=AccessTools.Property(typeof(Need_Chemical),"AddictionHediff");
        static GuestAddictionSupply()
        {
            if(!HotelBillingHooks.Enabled)return;
            new Harmony("turbomegamod.guest.addiction.supply").Patch(AccessTools.Method(typeof(JobGiver_SatisfyChemicalNeed),"TryGiveJob"),
                prefix:new HarmonyMethod(typeof(GuestAddictionSupply),"NativeJob"));
            Install(DefDatabase<DutyDef>.GetNamed("Relax").thinkNode);
        }
        private static bool Install(ThinkNode n)
        {
            if(n is ThinkNode_PrioritySorter&&n.subNodes.Any(c=>c.GetType().FullName=="Hospitality.JobGiver_Relax")){
                n.subNodes.Insert(0,new JobGiver_GuestAddictionSupply());return true;}
            foreach(var child in n.subNodes)if(Install(child))return true;return false;
        }
        public static IEnumerable<Hediff_Addiction> Due(Pawn p)
        {
            if(p==null||p.needs==null)return Enumerable.Empty<Hediff_Addiction>();
            return p.needs.AllNeeds.OfType<Need_Chemical>().Where(n=>n.CurCategory<=DrugDesireCategory.Desire)
                .OrderBy(n=>n.CurLevel).Select(n=>addiction.GetValue(n,null) as Hediff_Addiction).Where(h=>h!=null).Distinct();
        }
        private static bool Matches(Pawn p,Hediff_Addiction need,Thing item)
        {
            if(item==null||item.Destroyed||item.stackCount<=0||!item.def.IsDrug||!item.IngestibleNow)return false;
            var drug=item.TryGetComp<CompDrug>();return drug!=null&&drug.Props.chemical!=null&&
                drug.Props.chemical.addictionHediff==need.def&&GuestGeneticSupply.PolicyAllows(p,item.def);
        }
        public static Job MakeJob(Pawn p)
        {
            if(!GuestGeneticSupply.Fit(p))return null;var needs=Due(p).ToArray();
            foreach(var need in needs){
                var carried=p.carryTracker==null?null:p.carryTracker.CarriedThing;
                var own=Matches(p,need,carried)?carried:p.inventory.innerContainer.FirstOrDefault(t=>Matches(p,need,t));
                if(own!=null)return new Job(JobDefOf.Ingest,own){count=1,ingestTotalCount=true,takeExtraIngestibles=0};
            }
            if(needs.Length==0)return null;var desk=GuestDossiers.Desk(p.Map);
            if(desk==null||!desk.ordinaryDrugPurchases)return null;
            var search=p.Map.GetComponent<GuestAddictionSearch>();if(search.Waiting(p))return null;
            var sales=p.Map.GetComponent<ComplexSales>();
            foreach(var need in needs)foreach(var store in SaleStock.Stores(p.Map).OrderBy(s=>s.Position.DistanceToSquared(p.Position)))
                foreach(var item in SaleStock.Contents(store).Where(t=>Matches(p,need,t))){
                    var order=sales.Create(p,store,item,1);if(order==null)continue;
                    var job=order.delivery?JobMaker.MakeJob(ComplexSales.WaitJob,order.waitCell):JobMaker.MakeJob(ComplexSales.BuyJob,store);job.count=order.id;return job;
                }
            search.Failed(p);return null;
        }
        private static bool NativeJob(Pawn pawn,ref Job __result){if(!GuestDeathrest.Visiting(pawn))return true;__result=MakeJob(pawn);return false;}
    }
    public sealed class JobGiver_GuestAddictionSupply : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn p){return GuestGeneticSupply.Fit(p)&&GuestAddictionSupply.Due(p).Any()?9.25f:0;}
        protected override Job TryGiveJob(Pawn p){return GuestAddictionSupply.MakeJob(p);}
    }
}
