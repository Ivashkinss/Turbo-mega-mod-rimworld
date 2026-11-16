using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    // This state belongs to the dossier identity, not the map or a generated name.
    public sealed class GuestProfile : IExposable
    {
        public bool evaluated,vip,arrivalPrepared;
        public int completed,good,grantTarget,grantAdded;
        public float lastScore=-1;
        public bool Regular {get{return GuestProfileRules.Regular(completed,good);}}
        public void ExposeData()
        {
            Scribe_Values.Look(ref evaluated,"evaluated");Scribe_Values.Look(ref vip,"vip");Scribe_Values.Look(ref arrivalPrepared,"arrivalPrepared");
            Scribe_Values.Look(ref completed,"completed");Scribe_Values.Look(ref good,"good");Scribe_Values.Look(ref lastScore,"lastScore",-1);
            Scribe_Values.Look(ref grantTarget,"grantTarget");Scribe_Values.Look(ref grantAdded,"grantAdded");
        }
    }
    public sealed class GuestStayExperience : IExposable
    {
        public int firstClaim=-1,lastSample=-1,lodgingTicks,nextBedCheck=-1,meal=-1;
        public float score=-1;
        public bool settled,observingLodging;
        public void ExposeData()
        {
            Scribe_Values.Look(ref firstClaim,"firstClaim",-1);Scribe_Values.Look(ref lastSample,"lastSample",-1);Scribe_Values.Look(ref lodgingTicks,"lodgingTicks");
            Scribe_Values.Look(ref nextBedCheck,"nextBedCheck",-1);Scribe_Values.Look(ref meal,"meal",-1);Scribe_Values.Look(ref score,"score",-1);Scribe_Values.Look(ref settled,"settled");Scribe_Values.Look(ref observingLodging,"observingLodging");
        }
    }
    [StaticConstructorOnStartup]
    public static class GuestProfiles
    {
        static GuestProfiles()
        {
            var worker=AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup");if(worker==null)return;
            var h=new Harmony("turbomegamod.guest.profiles");
            h.Patch(AccessTools.Method(worker,"GiveItems"),postfix:new HarmonyMethod(typeof(GuestProfiles),"ItemsGiven"));
            h.Patch(AccessTools.Method(worker,"GetPawnsToSpawn"),postfix:new HarmonyMethod(typeof(GuestProfiles),"PreferRegular"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.JobGiver_ClaimBed"),"TryGiveJob"),prefix:new HarmonyMethod(typeof(GuestProfiles),"ClaimBed"));
            var utility=AccessTools.TypeByName("Hospitality.Utilities.BedUtility");
            var score=AccessTools.GetDeclaredMethods(utility).Where(m=>m.Name=="CalculateBedValue").OrderByDescending(m=>m.GetParameters().Length).First();
            h.Patch(score,postfix:new HarmonyMethod(typeof(GuestProfiles),"BedValue"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.CompGuest"),"Leave"),prefix:new HarmonyMethod(typeof(GuestProfiles),"BeforeLeave"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.LordToil_VisitPoint"),"GetVisitScore"),postfix:new HarmonyMethod(typeof(GuestProfiles),"VisitScore"));
        }
        public static GuestDossier Row(Pawn pawn){return pawn==null||GuestDossiers.Book==null?null:GuestDossiers.Book.Find(pawn.GetUniqueLoadID());}
        public static bool HotelGuest(Pawn pawn)
        {
            var r=Row(pawn);return pawn!=null&&pawn.Spawned&&!pawn.Dead&&pawn.Faction!=Faction.OfPlayer&&!pawn.IsSlave&&!pawn.IsPrisoner&&GuestDossiers.Arrived(pawn)&&!Clinic.ActivePatient(pawn)&&
                r!=null&&r.Current!=null&&r.Current.mapId==pawn.Map.uniqueID&&!r.Current.leaving&&GuestDossiers.Desk(pawn.Map)!=null;
        }
        public static bool Vip(Pawn pawn){var row=Row(pawn);return row!=null&&row.profile.vip;}
        public static void Prepare(Pawn pawn)
        {
            if(pawn==null||!pawn.Spawned||pawn.Dead||pawn.Faction==Faction.OfPlayer||pawn.IsSlave||pawn.IsPrisoner||
                pawn.ageTracker.AgeBiologicalYearsFloat<18||Clinic.ActivePatient(pawn)||WholesaleTrade.Group(pawn)!=null||GuestDossiers.Blocked(pawn))return;
            var desk=GuestDossiers.Desk(pawn.Map);if(desk==null)return;
            var row=GuestDossiers.Book.Observe(pawn);var profile=row.profile;
            if(profile.arrivalPrepared||row.Current!=null)return;
            profile.arrivalPrepared=true;
            if(!profile.evaluated){profile.evaluated=true;profile.vip=GuestProfileRules.RollVip(desk.vipChance,Rand.Value);}
            profile.grantAdded=profile.grantTarget=0;
            if(!profile.vip)return;
            profile.grantTarget=Rand.RangeInclusive(1500,3000);
            int extra=GuestProfileRules.CoinsToAdd(HotelBillingHooks.Money(pawn),profile.grantTarget,MassUtility.FreeSpace(pawn),ThingDefOf.Silver.GetStatValueAbstract(StatDefOf.Mass));
            if(extra>0){var silver=ThingMaker.MakeThing(ThingDefOf.Silver);silver.stackCount=extra;
                if(pawn.inventory.innerContainer.TryAdd(silver,false))profile.grantAdded=extra;else silver.Destroy();}
            Consolidate(pawn);
        }
        private static void ItemsGiven(IEnumerable<Pawn> __0){foreach(var pawn in __0)Prepare(pawn);}
        // Hospitality searches and charges only the first silver stack. Its own
        // generated purse also exceeds the floor stack limit. Preserve all coins.
        public static void Consolidate(Pawn pawn)
        {
            var coins=pawn.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray();
            if(coins.Length<2)return;foreach(var other in coins.Skip(1))coins[0].TryAbsorbStack(other,false);
        }
        public static IEnumerable<Pawn> SelectRegular(IEnumerable<Pawn> original,IncidentParms parms,bool choose)
        {
            var list=original.ToList();var map=parms.target as Map;var desk=GuestDossiers.Desk(map);
            if(!choose||desk==null||!desk.preferRegulars||parms.faction==null)return list;
            var method=AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.SpawnGroupUtility"),"GetKnownPawns");
            var known=(IEnumerable<Pawn>)method.Invoke(null,new object[]{parms});
            var candidates=known.Where(p=>!list.Contains(p)&&Row(p)!=null&&Row(p).profile.Regular&&!GuestDossiers.Blocked(p)).ToList();
            if(candidates.Count==0)return list;
            int replace=list.FindIndex(p=>p!=null&&!p.Spawned&&!Find.WorldPawns.Contains(p)&&Row(p)==null);
            if(replace<0)return list;
            var unused=list[replace];list[replace]=candidates.RandomElement();Find.WorldPawns.PassToWorld(unused,PawnDiscardDecideMode.Discard);return list;
        }
        private static void PreferRegular(IncidentParms parms,ref IEnumerable<Pawn> __result){var desk=GuestDossiers.Desk(parms.target as Map);if(desk!=null&&desk.preferRegulars)__result=SelectRegular(__result,parms,Rand.Chance(0.5f));}
        private static bool ClaimBed(Pawn __0,ref Job __result)
        {
            if(!Vip(__0)||!HotelGuest(__0))return true;
            __result=null;var stay=Row(__0).Current.experience;
            if(HotelBillingHooks.Bed(__0)!=null)return false;
            if(stay.nextBedCheck<0)stay.nextBedCheck=Row(__0).Current.started+GuestProfileRules.FirstBedDelay;
            if(GuestDossiers.Now<stay.nextBedCheck)return false;
            stay.nextBedCheck=GuestDossiers.Now+GuestProfileRules.BedRetryDelay;Consolidate(__0);
            var bed=(Building_Bed)AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),"FindBedFor").Invoke(null,new object[]{__0});
            if(bed!=null)__result=new Job(DefDatabase<JobDef>.GetNamed("ClaimGuestBed"),bed){takeExtraIngestibles=__0.Map.GetComponent<HotelBilling>().EntryFee(bed)};
            return false;
        }
        public static bool GoodRoom(Building_Bed bed)
        {
            var room=bed==null?null:bed.GetRoom();return room!=null&&!room.PsychologicallyOutdoors&&!bed.Medical&&
                room.ContainedBeds.Count(b=>b.def.building.bed_humanlike)==1&&room.GetStat(RoomStatDefOf.Impressiveness)>=85;
        }
        private static bool Ascetic(Pawn pawn){return pawn.story!=null&&pawn.story.traits.HasTrait(TraitDefOf.Ascetic);}
        private static void BedValue(Building_Bed __0,Pawn __1,ref float __result){if(Vip(__1)&&!Ascetic(__1)&&GoodRoom(__0))__result+=250;}
        public static void Claimed(Pawn pawn)
        {
            var row=Row(pawn);if(row==null||row.Current==null)return;var stay=row.Current.experience;
            if(stay.firstClaim<0)stay.firstClaim=GuestDossiers.Now;stay.lastSample=GuestDossiers.Now;stay.observingLodging=true;
        }
        public static void Meal(Pawn pawn,ThingDef meal)
        {
            var row=Row(pawn);if(row==null||row.Current==null)return;
            row.Current.experience.meal=meal.ingestible!=null&&meal.ingestible.preferability>=FoodPreferability.MealFine?1:0;
        }
        public static void Sample(GuestDossier row)
        {
            var visit=row.Current;if(visit==null)return;var state=visit.experience;int now=GuestDossiers.Now;
            bool lodging=HotelGuest(row.pawn)&&HotelBillingHooks.Bed(row.pawn)!=null;
            if(lodging){
                if(state.firstClaim<0)state.firstClaim=now;
                // A missed observation never invents hours of lodging.
                if(state.observingLodging&&state.lastSample>=0)state.lodgingTicks+=Math.Max(0,Math.Min(500,now-state.lastSample));
                var lord=row.pawn.GetLord();var toil=lord==null?null:lord.CurLordToil;
                if(toil!=null&&toil.GetType().FullName=="Hospitality.LordToil_VisitPoint")
                    AccessTools.Method(toil.GetType(),"GetVisitScore").Invoke(toil,new object[]{row.pawn});
            }
            state.lastSample=now;state.observingLodging=lodging;
        }
        private static void BeforeLeave(ThingComp __instance){var row=Row(__instance.parent as Pawn);if(row!=null)Sample(row);}
        private static void VisitScore(Pawn __0,float __result)
        {
            var row=Row(__0);if(row!=null&&row.Current!=null&&!float.IsNaN(__result)&&!float.IsInfinity(__result))row.Current.experience.score=Math.Max(0,Math.Min(1,__result));
        }
        public static void Settle(GuestDossier row,string reason)
        {
            var visit=row.Current;if(visit==null||visit.experience.settled)return;var state=visit.experience;state.settled=true;
            if(visit.leaving&&GuestProfileRules.Qualifies(reason,visit.medical&&state.firstClaim<0,state.lodgingTicks)){
                row.profile.completed++;row.profile.lastScore=state.score;if(GuestProfileRules.Good(state.score))row.profile.good++;
            }
            row.profile.arrivalPrepared=false;
        }
        public static ThoughtState Mood(Pawn pawn,int kind)
        {
            if(!HotelGuest(pawn))return ThoughtState.Inactive;var row=Row(pawn);
            if(kind==2)return row.profile.Regular?ThoughtState.ActiveAtStage(0):ThoughtState.Inactive;
            if(!row.profile.vip||Ascetic(pawn))return ThoughtState.Inactive;
            if(kind==1){int meal=row.Current.experience.meal;return meal<0?ThoughtState.Inactive:ThoughtState.ActiveAtStage(meal);}
            var bed=HotelBillingHooks.Bed(pawn);return bed==null?ThoughtState.Inactive:ThoughtState.ActiveAtStage(GoodRoom(bed)?1:0);
        }
    }
    public sealed class ThoughtWorker_VipRoom : ThoughtWorker{protected override ThoughtState CurrentStateInternal(Pawn p){return GuestProfiles.Mood(p,0);}}
    public sealed class ThoughtWorker_VipFood : ThoughtWorker{protected override ThoughtState CurrentStateInternal(Pawn p){return GuestProfiles.Mood(p,1);}}
    public sealed class ThoughtWorker_RegularGuest : ThoughtWorker{protected override ThoughtState CurrentStateInternal(Pawn p){return GuestProfiles.Mood(p,2);}}
}
