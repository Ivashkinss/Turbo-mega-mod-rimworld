using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public sealed class GuestHemogenSearch : MapComponent
    {
        // Transient IDs only; never retain departed pawns through a search cache.
        private readonly Dictionary<int,int> retry=new Dictionary<int,int>();
        public GuestHemogenSearch(Map map):base(map){}
        public bool Waiting(Pawn p){int until;return retry.TryGetValue(p.thingIDNumber,out until)&&GuestDossiers.Now<until;}
        public void Failed(Pawn p){retry[p.thingIDNumber]=GuestDossiers.Now+1000;}
        public override void MapComponentTick(){if(GuestDossiers.Now%2500==0)foreach(var id in retry.Where(k=>k.Value<=GuestDossiers.Now).Select(k=>k.Key).ToArray())retry.Remove(id);}
    }
    [StaticConstructorOnStartup]
    public static class GuestHemogen
    {
        private static readonly MethodInfo packGain=AccessTools.PropertyGetter(typeof(JobGiver_GetHemogen),"HemogenPackHemogenGain");
        public static Gene_Hemogen Gene(Pawn p){return !ModsConfig.BiotechActive||p==null||p.genes==null?null:p.genes.GetFirstGeneOfType<Gene_Hemogen>();}
        public static float PackGain {get{return (float)packGain.Invoke(null,null);}}
        static GuestHemogen()
        {
            if(!ModsConfig.BiotechActive||!HotelBillingHooks.Enabled)return;
            var h=new Harmony("turbomegamod.guest.hemogen");
            h.Patch(AccessTools.Method(typeof(JobGiver_GetHemogen),"TryGiveJob"),prefix:new HarmonyMethod(typeof(GuestHemogen),"NativeJob"));
            Install(DefDatabase<DutyDef>.GetNamed("Relax").thinkNode);
        }
        private static bool Install(ThinkNode node)
        {
            if(node is ThinkNode_PrioritySorter&&node.subNodes.Any(n=>n.GetType().FullName=="Hospitality.JobGiver_Relax")){
                node.subNodes.Insert(0,new JobGiver_GuestHemogen());return true;}
            foreach(var child in node.subNodes)if(Install(child))return true;return false;
        }
        public static bool Managed(Pawn p){return GuestDeathrest.Visiting(p);}
        public static bool Needs(Pawn p)
        {
            var g=Gene(p);return g!=null&&g.Active&&g.hemogenPacksAllowed&&g.ShouldConsumeHemogenNow()&&Wanted(g.Value,g.Max,PackGain)>0;
        }
        public static int Wanted(float value,float max,float gain)
        {
            if(float.IsNaN(value)||float.IsNaN(max)||float.IsNaN(gain)||float.IsInfinity(value)||float.IsInfinity(max)||float.IsInfinity(gain)||gain<=0||value<0||max<=value)return 0;
            float amount=(max-value)/gain;
            if(float.IsInfinity(amount)||amount>=int.MaxValue)return int.MaxValue;
            return Mathf.Max(0,Mathf.FloorToInt(amount));
        }
        public static bool Fit(Pawn p)
        {
            return Managed(p)&&Needs(p)&&!p.Downed&&!p.Drafted&&!p.InMentalState&&p.Awake()&&p.inventory!=null&&
                !Clinic.ActivePatient(p)&&ClinicGuests.Visit(p)==null&&GuestDeathrest.Stay(p)==null&&
                p.CurJobDef!=ComplexSales.BuyJob&&p.CurJobDef!=ComplexSales.WaitJob&&p.CurJobDef!=RestaurantMeals.WaitJob&&p.CurJobDef!=CompanyRules.GuestJob;
        }
        public static Thing Personal(Pawn p)
        {
            var carried=p.carryTracker==null?null:p.carryTracker.CarriedThing;
            if(Pack(carried))return carried;
            return p.inventory==null?null:p.inventory.innerContainer.FirstOrDefault(Pack);
        }
        private static bool Pack(Thing t){return t!=null&&!t.Destroyed&&t.stackCount>0&&t.def==ThingDefOf.HemogenPack&&t.IngestibleNow;}
        public static Job MakeJob(Pawn p)
        {
            if(!Fit(p))return null;
            var gene=Gene(p);int wanted=Wanted(gene.Value,gene.Max,PackGain);
            var personal=Personal(p);
            if(personal!=null){
                // The native driver handles actual ingestion and resource gain.
                // Never request extra stacks from the map or a bloodfeeding job.
                return new Job(JobDefOf.Ingest,personal){count=Math.Min(wanted,personal.stackCount),ingestTotalCount=true,takeExtraIngestibles=0};
            }
            var desk=GuestDossiers.Desk(p.Map);
            if(!desk.hemogenPurchases)return null;
            var search=p.Map.GetComponent<GuestHemogenSearch>();if(search.Waiting(p))return null;
            var sales=p.Map.GetComponent<ComplexSales>();
            foreach(var storage in SaleStock.Stores(p.Map).OrderBy(s=>s.Position.DistanceToSquared(p.Position)))
                foreach(var item in SaleStock.Contents(storage).Where(Pack)){
                    if(!sales.Requestable(p,storage,item))continue;
                    double unit=SaleStock.UnitPrice(item);
                    int affordable=(int)Math.Min(int.MaxValue,Math.Floor(HotelBillingHooks.Money(p)/unit));
                    var order=sales.Create(p,storage,item,Math.Min(wanted,affordable));
                    if(order==null)continue;
                    var job=order.delivery?JobMaker.MakeJob(ComplexSales.WaitJob,order.waitCell):JobMaker.MakeJob(ComplexSales.BuyJob,storage);
                    job.count=order.id;return job;
                }
            search.Failed(p);return null;
        }
        private static bool NativeJob(Pawn pawn,ref Job __result)
        {
            if(!Managed(pawn))return true;
            __result=MakeJob(pawn);return false;
        }
        public static string Status(Pawn p)
        {
            var g=Gene(p);if(g==null||!g.Active)return "";
            int count=p.inventory==null?0:p.inventory.innerContainer.Where(Pack).Sum(t=>t.stackCount);
            if(p.carryTracker!=null&&Pack(p.carryTracker.CarriedThing))count+=p.carryTracker.CarriedThing.stackCount;
            return "TMM.Hemogen.Status".Translate((g.Value/g.Max*100).ToString("0"),count,
                (g.hemogenPacksAllowed?"TMM.Hemogen.Allowed":"TMM.Hemogen.Blocked").Translate());
        }
    }
    public sealed class JobGiver_GuestHemogen : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn p){return GuestHemogen.Fit(p)?9.1f:0f;}
        protected override Job TryGiveJob(Pawn pawn){return GuestHemogen.MakeJob(pawn);}
    }
}
