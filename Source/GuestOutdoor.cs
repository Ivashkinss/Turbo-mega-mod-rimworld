using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public sealed class GuestOutdoorSearch : MapComponent
    {
        // IDs, not Pawn references: discarded visitors cannot be retained by a
        // search cache. This is only a performance backoff, not saved state.
        private readonly Dictionary<int,int> retry=new Dictionary<int,int>();
        public GuestOutdoorSearch(Map map):base(map){}
        public bool Waiting(Pawn pawn){int until;return retry.TryGetValue(pawn.thingIDNumber,out until)&&GuestDossiers.Now<until;}
        public void Failed(Pawn pawn){retry[pawn.thingIDNumber]=GuestDossiers.Now+1000;}
        public override void MapComponentTick(){if(GuestDossiers.Now%500==0)foreach(var pawn in map.mapPawns.AllPawnsSpawned.Where(GuestDossiers.Arrived))GuestOutdoor.Refresh(pawn);if(GuestDossiers.Now%2500==0)foreach(var key in retry.Where(p=>p.Value<=GuestDossiers.Now).Select(p=>p.Key).ToArray())retry.Remove(key);}
    }
    [StaticConstructorOnStartup]
    public static class GuestOutdoor
    {
        public const float Low=0.35f,Enough=0.7f;
        public const int MaxBreakTicks=2500,SearchLimit=6000,SearchRadius=60;
        public static NeedDef Outdoors {get{return DefDatabase<NeedDef>.GetNamed("Outdoors");}}
        private static readonly MethodInfo shouldHave=AccessTools.Method(typeof(Pawn_NeedsTracker),"ShouldHaveNeed");
        static GuestOutdoor()
        {
            var h=new Harmony("turbomegamod.guest.outdoors");
            h.Patch(AccessTools.Method(typeof(Need_Outdoors),"NeedInterval"),prefix:new HarmonyMethod(typeof(GuestOutdoor),"ClinicalPause"));
            Install(DefDatabase<DutyDef>.GetNamed("Relax").thinkNode);
        }
        private static bool Install(ThinkNode node)
        {
            if(node is ThinkNode_PrioritySorter&&node.subNodes.Any(n=>n.GetType().FullName=="Hospitality.JobGiver_Relax")){
                if(!node.subNodes.Any(n=>n is JobGiver_GuestOutdoor))node.subNodes.Insert(node.subNodes.FindIndex(n=>n.GetType().FullName=="Hospitality.JobGiver_Relax"),new JobGiver_GuestOutdoor());
                return true;
            }
            foreach(var child in node.subNodes)if(Install(child))return true;return false;
        }
        public static bool Eligible(Pawn pawn)
        {
            if(pawn==null||!pawn.Spawned||pawn.Dead||pawn.Faction==Faction.OfPlayer||pawn.IsPrisoner||pawn.IsSlave||!GuestDossiers.Arrived(pawn))return false;
            var desk=GuestDossiers.Desk(pawn.Map);return desk!=null&&desk.outdoorBreaks;
        }
        public static void Refresh(Pawn pawn)
        {
            if(pawn==null||pawn.needs==null)return;
            bool should=(bool)shouldHave.Invoke(pawn.needs,new object[]{GuestOutdoor.Outdoors}),has=pawn.needs.TryGetNeed(GuestOutdoor.Outdoors)!=null;
            if(should!=has)pawn.needs.AddOrRemoveNeedsAsAppropriate();
        }
        public static bool Clinical(Pawn pawn){return pawn!=null&&pawn.MapHeld!=null&&GuestDossiers.Desk(pawn.MapHeld)!=null&&Clinic.ActivePatient(pawn)&&(GuestDossiers.Arrived(pawn)||ClinicGuests.Visit(pawn)!=null);}
        private static bool ClinicalPause(Pawn ___pawn){return !Clinical(___pawn);}
        public static bool Fit(Pawn pawn)
        {
            return Eligible(pawn)&&GuestProfiles.HotelGuest(pawn)&&!pawn.Downed&&!pawn.Drafted&&!pawn.InMentalState&&pawn.Awake()&&
                (pawn.needs.rest==null||pawn.needs.rest.CurLevelPercentage>=0.4f)&&
                (pawn.needs.food==null||pawn.needs.food.CurLevelPercentage>=0.55f)&&
                pawn.CurJobDef!=RestaurantMeals.WaitJob&&pawn.CurJobDef!=CompanyRules.GuestJob&&
                (pawn.CurJobDef==null||pawn.CurJobDef.defName!="ClaimGuestBed")&&!HealthAIUtility.ShouldSeekMedicalRest(pawn);
        }
        public static bool Weather(Pawn pawn){return JoyUtility.EnjoyableOutsideNow(pawn,null)&&!GenHostility.AnyHostileActiveThreatTo(pawn.Map,pawn.Faction);}
        public static bool PublicStep(Pawn pawn,IntVec3 cell)
        {
            var desk=GuestDossiers.Desk(pawn.Map);
            return cell.InBounds(pawn.Map)&&desk!=null&&desk.area[cell]&&!ComplexLayout.StaffOnly(desk,cell)&&RestaurantMeals.AllowedCell(pawn,cell)&&
                cell.Walkable(pawn.Map)&&cell.GetDangerFor(pawn,pawn.Map)==Danger.None;
        }
        public static bool Spot(Pawn pawn,IntVec3 cell)
        {
            if(!PublicStep(pawn,cell)||!cell.Standable(pawn.Map)||cell.GetRoof(pawn.Map)!=null||!cell.UsesOutdoorTemperature(pawn.Map)||
                cell.GetThingList(pawn.Map).Any(t=>t is Fire))return false;
            float temperature=cell.GetTemperature(pawn.Map);
            return temperature>=pawn.GetStatValue(StatDefOf.ComfyTemperatureMin)&&temperature<=pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);
        }
        public static IntVec3 FindSpot(Pawn pawn)
        {
            if(!Fit(pawn)||!Weather(pawn))return IntVec3.Invalid;
            var start=pawn.Position;var open=new Queue<IntVec3>();var seen=new HashSet<IntVec3>();open.Enqueue(start);seen.Add(start);
            int checkedCells=0;
            while(open.Count>0&&checkedCells++<SearchLimit){
                var cell=open.Dequeue();
                if(Spot(pawn,cell)&&pawn.CanReserveAndReach(cell,PathEndMode.OnCell,Danger.None))return cell;
                foreach(var offset in GenAdj.CardinalDirections){var next=cell+offset;
                    if(next.DistanceToSquared(start)<=SearchRadius*SearchRadius&&seen.Add(next)&&PublicStep(pawn,next))open.Enqueue(next);
                }
            }
            return IntVec3.Invalid;
        }
        public static Job MakeJob(Pawn pawn)
        {
            var need=pawn==null||pawn.needs==null?null:pawn.needs.TryGetNeed(GuestOutdoor.Outdoors);
            if(need==null||need.CurLevelPercentage>=Low||!Fit(pawn)||!Weather(pawn))return null;
            var search=pawn.Map.GetComponent<GuestOutdoorSearch>();if(search.Waiting(pawn))return null;
            var cell=FindSpot(pawn);if(!cell.IsValid){search.Failed(pawn);return null;}
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TMM_GuestOutdoorBreak"),cell);
        }
        public static string Status(Pawn pawn)
        {
            string key;
            if(Clinical(pawn))key="Clinical";
            else if(!Eligible(pawn))key="Unavailable";
            else if(pawn.needs.TryGetNeed(GuestOutdoor.Outdoors)==null)key="NotApplicable";
            else if(pawn.needs.TryGetNeed(GuestOutdoor.Outdoors).CurLevelPercentage>=Low)key="Enough";
            else if(!Fit(pawn))key="BasicNeedsFirst";
            else if(!Weather(pawn))key="UnsafeWeather";
            else key=FindSpot(pawn).IsValid?"Ready":"NoCourtyard";
            return ("TMM.Outdoor."+key).Translate();
        }
    }
    public sealed class JobGiver_GuestOutdoor : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn){var n=pawn.needs==null?null:pawn.needs.TryGetNeed(GuestOutdoor.Outdoors);return n!=null&&n.CurLevelPercentage<GuestOutdoor.Low&&GuestOutdoor.Fit(pawn)?4f:0f;}
        protected override Job TryGiveJob(Pawn pawn){return GuestOutdoor.MakeJob(pawn);}
    }
    public sealed class JobDriver_GuestOutdoor : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed){return pawn.Reserve(job.targetA,job,1,-1,null,errorOnFailed);}
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(()=>!GuestOutdoor.Fit(pawn)||!GuestOutdoor.Weather(pawn)||!GuestOutdoor.Spot(pawn,job.targetA.Cell));
            yield return Toils_Goto.GotoCell(TargetIndex.A,PathEndMode.OnCell);
            var rest=Toils_General.Wait(GuestOutdoor.MaxBreakTicks);rest.tickAction=delegate{
                var need=pawn.needs.TryGetNeed(GuestOutdoor.Outdoors);
                if(need==null||need.CurLevelPercentage>=GuestOutdoor.Enough)EndJobWith(JobCondition.Succeeded);
            };rest.socialMode=RandomSocialMode.Normal;yield return rest;
        }
    }
}
