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
    public sealed class ClinicVisit : IExposable
    {
        public Pawn pawn;
        public Lord lord;
        public Faction host;
        public bool selfTend;
        public FoodPolicy originalFood, managedFood;
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn,"pawn");
            if(Scribe.mode==LoadSaveMode.Saving && lord!=null && !Find.Maps.Any(m=>m.lordManager.lords.Contains(lord)))lord=null;
            Scribe_References.Look(ref lord,"lord");Scribe_References.Look(ref host,"host");Scribe_Values.Look(ref selfTend,"selfTend");
            if(Scribe.mode==LoadSaveMode.Saving){
                var policies=Current.Game.foodRestrictionDatabase.AllFoodRestrictions;
                if(originalFood!=null&&!policies.Contains(originalFood))originalFood=null;
                if(managedFood!=null&&!policies.Contains(managedFood))managedFood=null;
            }
            Scribe_References.Look(ref originalFood,"originalFood");Scribe_References.Look(ref managedFood,"managedFood");
        }
    }

    public sealed class ClinicGuests : MapComponent
    {
        public List<ClinicVisit> visits=new List<ClinicVisit>();
        private int nextCheck;
        public ClinicGuests(Map map):base(map){}
        public override void ExposeData()
        {
            base.ExposeData();Scribe_Collections.Look(ref visits,"TMM_clinicGuests",LookMode.Deep);
            if(Scribe.mode==LoadSaveMode.PostLoadInit && visits==null)visits=new List<ClinicVisit>();
        }
        public static ClinicVisit Visit(Pawn pawn)
        {
            var map=pawn==null?null:pawn.MapHeld;
            return map==null?null:map.GetComponent<ClinicGuests>().visits.FirstOrDefault(v=>v.pawn==pawn);
        }
        public static bool NeedsCare(Pawn pawn)
        {
            var data=Clinic.Data(pawn);
            bool pending=data!=null && (bool)AccessTools.Field(data.GetType(),"HasPendingSurgeryBill").GetValue(data);
            return pawn.Downed || pawn.health.HasHediffsNeedingTend() || HealthAIUtility.ShouldSeekMedicalRest(pawn) || pawn.health.surgeryBills.Count>0 || pending;
        }
        private static bool Leaving(Lord lord)
        {
            return lord==null || lord.LordJob==null || lord.LordJob.GetType().FullName!="Hospitality.LordJob_VisitColony" || (bool)AccessTools.Field(lord.LordJob.GetType(),"leaving").GetValue(lord.LordJob);
        }
        public static bool Eligible(Pawn pawn)
        {
            if(pawn==null||!pawn.Spawned||pawn.Dead||pawn.Faction==Faction.OfPlayer||pawn.IsPrisoner||pawn.IsSlave||pawn.InMentalState||GuestDeathrest.Stay(pawn)!=null||Leaving(pawn.GetLord()))return false;
            var comp=HotelBillingHooks.Guest(pawn);
            return comp!=null && (bool)AccessTools.Field(comp.GetType(),"arrived").GetValue(comp);
        }
        public bool Admit(Pawn pawn)
        {
            if(!Eligible(pawn)||Clinic.Desk(map)==null||Visit(pawn)!=null||!NeedsCare(pawn))return false;
            var hospital=Clinic.Hospital(map);
            if((int)AccessTools.Method(Clinic.HospitalType,"FreeMedicalBeds").Invoke(hospital,null)<=0)return false;
            var previous=Clinic.Data(pawn);
            if(previous!=null){
                if(Clinic.ActivePatient(pawn))return false;
                AccessTools.Method(Clinic.HospitalType,"PatientLeftTheMap").Invoke(hospital,new object[]{pawn});
            }
            var visit=new ClinicVisit {pawn=pawn,lord=pawn.GetLord(),host=pawn.HostFaction,selfTend=pawn.playerSettings.selfTend,
                originalFood=pawn.foodRestriction==null?null:pawn.foodRestriction.CurrentFoodPolicy};
            var dataType=AccessTools.TypeByName("Hospital.PatientData");var kind=AccessTools.TypeByName("Hospital.PatientType");
            var data=Activator.CreateInstance(dataType,new object[]{Find.TickManager.TicksGame,pawn.MarketValue,pawn.needs.mood==null?0.5f:pawn.needs.mood.CurInstantLevel,Enum.ToObject(kind,pawn.health.surgeryBills.Count>0?3:1)});
            AccessTools.Field(dataType,"Diagnosis").SetValue(data,string.Join(", ",pawn.health.hediffSet.hediffs.Where(h=>h.Visible).Select(h=>h.LabelCap.ToString()).Distinct().ToArray()));
            AccessTools.Field(dataType,"Cure").SetValue(data,"TMM.Clinic.Care".Translate().ToString());
            visits.Add(visit);AccessTools.Method(Clinic.HospitalType,"PatientArrived").Invoke(hospital,new[]{(object)pawn,data});
            var bed=RestUtility.FindBedFor(pawn,pawn,false,false,null);
            if(bed==null||!Clinic.WardBed(bed)){
                visits.Remove(visit);AccessTools.Method(Clinic.HospitalType,"PatientLeftTheMap").Invoke(hospital,new object[]{pawn});return false;
            }
            pawn.guest.SetGuestStatus(Faction.OfPlayer);pawn.playerSettings.selfTend=false;
            visit.managedFood=AccessTools.Field(Clinic.HospitalType,"PatientFoodPolicy").GetValue(hospital) as FoodPolicy;
            if(visit.managedFood!=null){if(pawn.foodRestriction==null)pawn.foodRestriction=new Pawn_FoodRestrictionTracker(pawn);pawn.foodRestriction.CurrentFoodPolicy=visit.managedFood;}
            map.GetComponent<RestaurantService>().Cancel(pawn);pawn.jobs.StopAll();
            Messages.Message("TMM.Clinic.Admitted".Translate(pawn.LabelShortCap),MessageTypeDefOf.NeutralEvent,false);return true;
        }
        public void Restore(Pawn pawn,bool continuingVisit)
        {
            var visit=visits.FirstOrDefault(v=>v.pawn==pawn);if(visit==null)return;
            visits.Remove(visit);
            var patients=Clinic.Patients(map);if(pawn!=null&&patients!=null&&patients.Contains(pawn))patients.Remove(pawn);
            if(pawn==null||pawn.Dead)return;
            pawn.playerSettings.selfTend=visit.selfTend;
            if(pawn.foodRestriction!=null&&pawn.foodRestriction.CurrentFoodPolicy==visit.managedFood)
                pawn.foodRestriction.CurrentFoodPolicy=visit.originalFood??Current.Game.foodRestrictionDatabase.DefaultFoodRestriction();
            if(continuingVisit&&pawn.Spawned&&pawn.Faction!=Faction.OfPlayer&&pawn.GetLord()==visit.lord&&!Leaving(visit.lord)){
                pawn.guest.SetGuestStatus(visit.host);pawn.jobs.StopAll();
            }
        }
        public override void MapComponentTick()
        {
            if(Find.TickManager.TicksGame<nextCheck)return;nextCheck=Find.TickManager.TicksGame+250;
            foreach(var visit in visits.ToArray()){
                var pawn=visit.pawn;
                if(pawn==null||pawn.Dead||pawn.MapHeld!=map||pawn.Faction==Faction.OfPlayer){Restore(pawn,false);continue;}
                if(Leaving(visit.lord)||pawn.GetLord()!=visit.lord){
                    AccessTools.Method(Clinic.HospitalType,"DismissPatient").Invoke(Clinic.Hospital(map),new object[]{pawn});Restore(pawn,false);continue;
                }
                if(!Clinic.ActivePatient(pawn)){Restore(pawn,true);continue;}
                if(!NeedsCare(pawn))AccessTools.Method(Clinic.HospitalType,"PatientLeaves").Invoke(Clinic.Hospital(map),new object[]{pawn});
            }
        }
        public static void Install(Harmony h)
        {
            var comp=AccessTools.TypeByName("Hospitality.CompGuest");
            h.Patch(AccessTools.PropertyGetter(comp,"GuestArea"),postfix:new HarmonyMethod(typeof(ClinicGuests),"GuestArea"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.Trigger_TicksPassedAndOkayToLeave"),"ActivateOn"),postfix:new HarmonyMethod(typeof(ClinicGuests),"HoldVisit"));
            h.Patch(AccessTools.Method(typeof(LordJob),"ShouldRemovePawn"),postfix:new HarmonyMethod(typeof(ClinicGuests),"KeepClinicalGuest"));
            var food=AccessTools.TypeByName("Hospitality.Patches.FoodUtility_Patch+IsFoodSourceOnMapSociallyProperPatch");
            h.Patch(AccessTools.Method(food,"Postfix"),prefix:new HarmonyMethod(typeof(ClinicGuests),"ClinicalFood"));
            h.Patch(AccessTools.Method(Clinic.HospitalType,"RemoveFromPatientList"),prefix:new HarmonyMethod(typeof(ClinicGuests),"KeepBoardingStatus"));
            var duty=DefDatabase<DutyDef>.GetNamed("Relax");
            int at=duty.thinkNode.subNodes.FindIndex(n=>n.GetType().FullName=="Hospitality.ThinkNode_InGuestArea");
            var tag=new ThinkNode_Tagger();AccessTools.Field(typeof(ThinkNode_Tagger),"tagToGive").SetValue(tag,JobTag.RestingForMedicalReasons);tag.subNodes.Add(new JobGiver_ClinicPatient());
            duty.thinkNode.subNodes.Insert(at<0?0:at,tag);
            ClinicBilling.Discharged+=pawn=>{var component=pawn.MapHeld==null?null:pawn.MapHeld.GetComponent<ClinicGuests>();if(component!=null)component.Restore(pawn,true);};
        }
        private static void GuestArea(ThingComp __instance,ref Area __result)
        {
            var pawn=__instance.parent as Pawn;
            if(Visit(pawn)==null||!Clinic.ActivePatient(pawn))return;
            var desk=Clinic.Desk(pawn.MapHeld);if(desk!=null)__result=desk.area;
        }
        private static void HoldVisit(Lord __0,ref bool __result)
        {
            if(__result&&__0.ownedPawns.Any(p=>Visit(p)!=null&&Clinic.ActivePatient(p)))__result=false;
        }
        private static void KeepClinicalGuest(LordJob __instance,Pawn __0,PawnLostCondition __1,ref bool __result)
        {
            // Ordinary visitor groups discard incapacitated pawns. During
            // clinical care (including anesthesia) that would also unclaim
            // their paid room and dissolve a one-person visit. Keep only this
            // medical state; death, capture, faction change and exit remain
            // normal reasons to leave the group.
            if(__1!=PawnLostCondition.Incapped||!Clinic.ActivePatient(__0))return;
            var visit=Visit(__0);
            if(visit!=null&&visit.lord!=null&&visit.lord.LordJob==__instance)__result=false;
        }
        private static bool ClinicalFood(Pawn getter,Pawn eater)
        {
            // Guest shopping rules reject ordinary colony meals even when a
            // doctor is bringing them to a hospitalized visitor. Keep vanilla
            // food suitability, diet, reservations and access checks for this
            // care task; self-service guests retain Hospitality's rules.
            return getter==null||getter==eater||getter.Faction!=Faction.OfPlayer||(GuestDeathrest.Stay(eater)==null&&(Visit(eater)==null||!Clinic.ActivePatient(eater)));
        }
        private static bool KeepBoardingStatus(Pawn __0)
        {
            if(Visit(__0)==null)return true;
            // The upstream removal also clears HostFaction, which releases
            // Hospitality's bed and invalidates the paid stay. A discharged
            // boarder is still visiting. Keep upstream clinical cleanup and
            // let Restore return the saved care settings without checking out.
            var data=Clinic.Data(__0);
            if(data!=null)AccessTools.Field(data.GetType(),"Dismissed").SetValue(data,true);
            __0.playerSettings.selfTend=true;
            MainTabWindowUtility.NotifyAllPawnTables_PawnsChanged();
            return false;
        }
        public static void ShowAdmission(Building_ComplexDesk desk)
        {
            var candidates=desk.Map.mapPawns.AllPawnsSpawned.Where(p=>Eligible(p)&&Visit(p)==null&&NeedsCare(p)).ToArray();
            if(candidates.Length==0){Messages.Message("TMM.Clinic.NoCandidates".Translate(),MessageTypeDefOf.RejectInput,false);return;}
            Find.WindowStack.Add(new FloatMenu(candidates.Select(p=>new FloatMenuOption(p.LabelShortCap,delegate{
                if(!desk.Map.GetComponent<ClinicGuests>().Admit(p))Messages.Message("TMM.Clinic.CannotAdmit".Translate(),MessageTypeDefOf.RejectInput,false);
            })).ToList()));
        }
    }
    public sealed class JobGiver_ClinicPatient : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if(ClinicGuests.Visit(pawn)==null||!Clinic.ActivePatient(pawn)||pawn.Downed||pawn.InMentalState)return null;
            var bed=RestUtility.FindBedFor(pawn,pawn,false,false,null);
            if(bed==null||!Clinic.WardBed(bed)||!pawn.CanReserveAndReach(bed,PathEndMode.OnCell,Danger.Some))return null;
            var job=JobMaker.MakeJob(JobDefOf.LayDown,bed);job.restUntilHealed=true;return job;
        }
    }
}
