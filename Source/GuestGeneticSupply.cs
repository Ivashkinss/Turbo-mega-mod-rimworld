using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public sealed class GuestDependencySearch : MapComponent
    {
        private readonly Dictionary<int,int> retry=new Dictionary<int,int>();
        public GuestDependencySearch(Map map):base(map){}
        public bool Waiting(Pawn p){int until;return retry.TryGetValue(p.thingIDNumber,out until)&&GuestDossiers.Now<until;}
        public void Failed(Pawn p){retry[p.thingIDNumber]=GuestDossiers.Now+1000;}
        public override void MapComponentTick(){if(GuestDossiers.Now%2500==0)foreach(var id in retry.Where(k=>k.Value<=GuestDossiers.Now).Select(k=>k.Key).ToArray())retry.Remove(id);}
    }
    [StaticConstructorOnStartup]
    public static class GuestGeneticSupply
    {
        static GuestGeneticSupply()
        {
            if(!ModsConfig.BiotechActive||!HotelBillingHooks.Enabled)return;
            new Harmony("turbomegamod.guest.genetic.supply").Patch(AccessTools.Method(typeof(JobGiver_SatifyChemicalDependency),"TryGiveJob"),
                prefix:new HarmonyMethod(typeof(GuestGeneticSupply),"NativeJob"));
            Install(DefDatabase<DutyDef>.GetNamed("Relax").thinkNode);
        }
        private static bool Install(ThinkNode node)
        {
            if(node is ThinkNode_PrioritySorter&&node.subNodes.Any(n=>n.GetType().FullName=="Hospitality.JobGiver_Relax")){
                node.subNodes.Insert(0,new JobGiver_GuestGeneticSupply());return true;}
            foreach(var child in node.subNodes)if(Install(child))return true;return false;
        }
        public static IEnumerable<Hediff_ChemicalDependency> Dependencies(Pawn p)
        {
            if(!ModsConfig.BiotechActive||p==null||p.genes==null)return Enumerable.Empty<Hediff_ChemicalDependency>();
            return p.genes.GenesListForReading.OfType<Gene_ChemicalDependency>().Where(g=>g.Active).Select(g=>g.LinkedHediff)
                .Where(h=>h!=null&&h.chemical!=null).Distinct();
        }
        public static bool Fit(Pawn p)
        {
            return GuestDeathrest.Visiting(p)&&!p.Downed&&!p.Drafted&&!p.InMentalState&&p.Awake()&&p.inventory!=null&&
                !Clinic.ActivePatient(p)&&ClinicGuests.Visit(p)==null&&GuestDeathrest.Stay(p)==null&&
                !HealthAIUtility.ShouldSeekMedicalRest(p)&&!p.health.HasHediffsNeedingTend()&&
                (p.needs.food==null||p.needs.food.CurLevelPercentage>=0.55f)&&(p.needs.rest==null||p.needs.rest.CurLevelPercentage>=0.4f)&&
                p.CurJobDef!=ComplexSales.BuyJob&&p.CurJobDef!=ComplexSales.WaitJob&&p.CurJobDef!=RestaurantMeals.WaitJob&&p.CurJobDef!=CompanyRules.GuestJob;
        }
        public static DrugPolicy SupplyPolicy(Building_ComplexDesk desk)
        {
            if(desk.dependencySupplyPolicy!=null&&!Current.Game.drugPolicyDatabase.AllPolicies.Contains(desk.dependencySupplyPolicy))desk.dependencySupplyPolicy=null;
            return desk.dependencySupplyPolicy;
        }
        public static bool PolicyAllows(Pawn p,ThingDef drug)
        {
            // This service never runs during mental states, so native policy
            // exceptions for mental breaks/recreational traits do not apply.
            if(p.drugs!=null){var policy=p.drugs.CurrentPolicy;if(policy==null||!policy[drug].allowedForAddiction)return false;}
            var desk=GuestDossiers.Desk(p.MapHeld);var extra=desk==null?null:SupplyPolicy(desk);
            return extra==null||extra[drug].allowedForAddiction;
        }
        public static bool Matches(Pawn p,Hediff_ChemicalDependency need,Thing item)
        {
            if(item==null||item.Destroyed||item.stackCount<=0||!item.def.IsDrug||!item.IngestibleNow)return false;
            var drug=item.TryGetComp<CompDrug>();
            return drug!=null&&drug.Props.chemical==need.chemical&&PolicyAllows(p,item.def);
        }
        public static Job MakeJob(Pawn p)
        {
            if(!Fit(p))return null;
            var needs=Dependencies(p).Where(h=>h.ShouldSatify).OrderByDescending(h=>h.Severity).ToArray();
            // Search all personal stocks before considering any paid dose.
            foreach(var need in needs){
                var carried=p.carryTracker==null?null:p.carryTracker.CarriedThing;
                var item=Matches(p,need,carried)?carried:p.inventory.innerContainer.FirstOrDefault(t=>Matches(p,need,t));
                if(item!=null)return new Job(JobDefOf.Ingest,item){count=1,ingestTotalCount=true,takeExtraIngestibles=0};
            }
            if(needs.Length==0)return null;
            var desk=GuestDossiers.Desk(p.Map);if(!desk.geneticDrugPurchases)return null;
            var search=p.Map.GetComponent<GuestDependencySearch>();if(search.Waiting(p))return null;
            var sales=p.Map.GetComponent<ComplexSales>();
            foreach(var need in needs)
                foreach(var storage in SaleStock.Stores(p.Map).OrderBy(s=>s.Position.DistanceToSquared(p.Position)))
                    foreach(var item in SaleStock.Contents(storage).Where(t=>Matches(p,need,t))){
                        var order=sales.Create(p,storage,item,1);if(order==null)continue;
                        var job=order.delivery?JobMaker.MakeJob(ComplexSales.WaitJob,order.waitCell):JobMaker.MakeJob(ComplexSales.BuyJob,storage);
                        job.count=order.id;return job;
                    }
            search.Failed(p);return null;
        }
        private static bool NativeJob(Pawn pawn,ref Job __result)
        {
            if(!GuestDeathrest.Visiting(pawn))return true;
            __result=MakeJob(pawn);return false;
        }
        public static string Status(Pawn p)
        {
            var needs=Dependencies(p).ToArray();if(needs.Length==0)return "";
            return "TMM.GeneticSupply.Status".Translate(string.Join("; ",needs.Select(h=>h.chemical.LabelCap+": "+
                (h.ShouldSatify?"TMM.GeneticSupply.Due":"TMM.GeneticSupply.NotDue").Translate())));
        }
    }
    public sealed class JobGiver_GuestGeneticSupply : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn p){return GuestGeneticSupply.Fit(p)&&GuestGeneticSupply.Dependencies(p).Any(h=>h.ShouldSatify)?9.15f:0f;}
        protected override Job TryGiveJob(Pawn pawn){return GuestGeneticSupply.MakeJob(pawn);}
    }
}
