using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public sealed class GuestDeathrestStay : IExposable
    {
        public Pawn pawn;
        public Building_Bed bed;
        public Lord lord;
        public int started;
        public long deadline;
        public bool slept, temporaryBinding;
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn,"pawn");Scribe_References.Look(ref bed,"bed");
            if(Scribe.mode==LoadSaveMode.Saving&&lord!=null&&!Find.Maps.Any(m=>m.lordManager.lords.Contains(lord)))lord=null;
            Scribe_References.Look(ref lord,"lord");Scribe_Values.Look(ref started,"started");Scribe_Values.Look(ref deadline,"deadline");
            Scribe_Values.Look(ref slept,"slept");Scribe_Values.Look(ref temporaryBinding,"temporaryBinding");
        }
    }
    public sealed class GuestDeathrest : MapComponent
    {
        public List<GuestDeathrestStay> stays=new List<GuestDeathrestStay>();
        private int nextCheck;
        public GuestDeathrest(Map map):base(map){}
        public override void ExposeData()
        {
            base.ExposeData();Scribe_Collections.Look(ref stays,"TMM_guestDeathrest",LookMode.Deep);
            if(Scribe.mode==LoadSaveMode.PostLoadInit){if(stays==null)stays=new List<GuestDeathrestStay>();stays.RemoveAll(s=>s==null);}
        }
        public static Gene_Deathrest Gene(Pawn p){return !ModsConfig.BiotechActive||p==null||p.genes==null?null:p.genes.GetFirstGeneOfType<Gene_Deathrest>();}
        public static GuestDeathrestStay Stay(Pawn p){return p==null||p.MapHeld==null?null:p.MapHeld.GetComponent<GuestDeathrest>().stays.FirstOrDefault(s=>s!=null&&s.pawn==p);}
        public static bool Sleeping(Pawn p){return p!=null&&p.health.hediffSet.HasHediff(HediffDefOf.Deathrest);}
        public static bool Visiting(Pawn p)
        {
            return p!=null&&p.Spawned&&!p.Dead&&p.Faction!=Faction.OfPlayer&&!p.IsPrisoner&&!p.IsSlave&&
                GuestDossiers.Arrived(p)&&!(bool)AccessTools.Field(p.GetLord().LordJob.GetType(),"leaving").GetValue(p.GetLord().LordJob)&&
                !p.HostileTo(Faction.OfPlayer)&&GuestDossiers.Desk(p.Map)!=null;
        }
        public static bool RoomSafe(Pawn p,Building_Bed bed)
        {
            if(bed==null||!bed.Spawned||bed.Map!=p.MapHeld||bed.Faction!=Faction.OfPlayer||bed.Medical||bed.IsBurning()||HotelBillingHooks.Bed(p)!=bed)return false;
            var desk=GuestDossiers.Desk(bed.Map);
            if(desk==null||!bed.OccupiedRect().All(c=>desk.area[c]&&!ComplexLayout.StaffOnly(desk,c)&&c.Roofed(bed.Map)))return false;
            float t=bed.Position.GetTemperature(bed.Map);
            return t>=p.GetStatValue(StatDefOf.ComfyTemperatureMin)&&t<=p.GetStatValue(StatDefOf.ComfyTemperatureMax);
        }
        public static bool Ready(Pawn p)
        {
            var gene=Gene(p);if(!Visiting(p)||gene==null||!gene.Active||p.Downed||p.Drafted||p.InMentalState||!GuestDossiers.Desk(p.Map).deathrestGuests||
                ClinicGuests.Visit(p)!=null||Clinic.ActivePatient(p)||HealthAIUtility.ShouldSeekMedicalRest(p)||p.health.HasHediffsNeedingTend()||
                (p.needs.food!=null&&p.needs.food.CurLevelPercentage<0.55f)||p.CurJobDef==CompanyRules.GuestJob||p.CurJobDef==RestaurantMeals.WaitJob)return false;
            var need=gene.DeathrestNeed;var bed=HotelBillingHooks.Bed(p);
            if(need==null||need.CurLevelPercentage>GuestDeathrestRules.StartLevel||!RoomSafe(p,bed)||!p.CanReserveAndReach(bed,PathEndMode.OnCell,Danger.None))return false;
            var paid=p.Map.GetComponent<HotelBilling>().stays.FirstOrDefault(s=>s.guest==p&&s.bed==bed);
            if(paid==null||paid.remaining>=0)return false;
            return GuestDeathrestRules.Affordable(p.Map.GetComponent<HotelBilling>().SharedMoney(bed),GuestDeathrestRules.Budget(GuestDossiers.Now,paid.nextChargeTick,gene.MinDeathrestTicks,HotelBillingHooks.Fee(bed)));
        }
        public static Job MakeJob(Pawn p){return Ready(p)?JobMaker.MakeJob(JobDefOf.Deathrest,HotelBillingHooks.Bed(p)):null;}
        public bool Begin(Pawn p,Building_Bed bed)
        {
            if(Stay(p)!=null)return Stay(p).bed==bed;
            if(!Ready(p)||HotelBillingHooks.Bed(p)!=bed)return false;
            stays.Add(new GuestDeathrestStay{pawn=p,bed=bed,lord=p.GetLord(),started=GuestDossiers.Now,
                deadline=(long)GuestDossiers.Now+Math.Max(600000L,(long)Gene(p).MinDeathrestTicks*2+60000)});return true;
        }
        public void End(GuestDeathrestStay s)
        {
            if(s.temporaryBinding&&s.bed!=null){
                var comp=s.bed.TryGetComp<CompDeathrestBindable>();var gene=Gene(s.pawn);
                if(gene!=null)gene.Notify_BoundBuildingDeSpawned(s.bed);
                if(comp!=null&&comp.BoundPawn==s.pawn)comp.Notify_DeathrestGeneRemoved();
            }
            stays.Remove(s);
        }
        public static void Cancel(Pawn p)
        {
            var s=Stay(p);if(s==null)return;
            var component=p.MapHeld.GetComponent<GuestDeathrest>();var gene=Gene(p);
            if(gene!=null&&Sleeping(p))gene.Wake();
            component.End(s);
            if(p.Spawned&&!p.Dead&&p.CurJobDef==JobDefOf.Deathrest)p.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
        public void Process()
        {
            foreach(var s in stays.ToArray()){
                var p=s.pawn;var gene=Gene(p);
                if(p==null||p.Destroyed||p.Dead){End(s);continue;}
                // Faction change/capture hands control back to the new owner.
                if(p.Faction==Faction.OfPlayer||p.IsPrisoner||p.IsSlave){End(s);continue;}
                if(p.MapHeld!=map){if(gene!=null&&Sleeping(p))gene.Wake();End(s);continue;}
                if(Sleeping(p))s.slept=true;
                if(gene==null||!gene.Active||!Visiting(p)||p.GetLord()!=s.lord||!RoomSafe(p,s.bed)||p.health.HasHediffsNeedingTend()||
                    GuestDossiers.Now>s.deadline||(!s.slept&&GuestDossiers.Now-s.started>2500)) {Cancel(p);continue;}
                if(Sleeping(p)&&gene.DeathrestPercent>=1f){gene.Wake();continue;}
                if(!Sleeping(p)&&(s.slept||p.CurJobDef!=JobDefOf.Deathrest))End(s);
            }
        }
        public override void MapComponentTick(){if(!ModsConfig.BiotechActive||GuestDossiers.Now<nextCheck)return;nextCheck=GuestDossiers.Now+60;Process();}
        public static void Show(Building_ComplexDesk desk)
        {
            var active=desk.Map.GetComponent<GuestDeathrest>().stays.Where(s=>s.pawn!=null).ToList();
            if(active.Count==0){Messages.Message("TMM.Deathrest.None".Translate(),MessageTypeDefOf.NeutralEvent,false);return;}
            Find.WindowStack.Add(new FloatMenu(active.Select(s=>new FloatMenuOption(s.pawn.LabelShortCap+": "+Status(s.pawn),()=>
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("TMM.Deathrest.Interrupt".Translate(s.pawn.LabelShortCap),()=>Cancel(s.pawn))))).ToList()));
        }
        public static string Status(Pawn p)
        {
            var gene=Gene(p);if(gene==null||!gene.Active)return "";
            if(Stay(p)!=null)return "TMM.Deathrest.Active".Translate((gene.DeathrestPercent*100).ToString("0"));
            return (Ready(p)?"TMM.Deathrest.Ready":"TMM.Deathrest.Waiting").Translate();
        }
    }
    public sealed class JobGiver_GuestDeathrest : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn){return GuestDeathrest.Ready(pawn)?7f:0f;}
        protected override Job TryGiveJob(Pawn pawn){return GuestDeathrest.MakeJob(pawn);}
    }
}
