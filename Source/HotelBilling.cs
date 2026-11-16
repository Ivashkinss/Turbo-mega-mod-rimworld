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
    [StaticConstructorOnStartup]
    public static class HotelBillingHooks
    {
        [ThreadStatic]private static int quoteDepth;
        private sealed class QuoteScope { public int previous; }
        private static readonly Type guestType = AccessTools.TypeByName("Hospitality.CompGuest");
        private static readonly Type bedType = AccessTools.TypeByName("Hospitality.Building_GuestBed");
        private static readonly FieldInfo bedField = guestType == null ? null : AccessTools.Field(guestType, "bed");
        private static readonly PropertyInfo hasBed = guestType == null ? null : AccessTools.Property(guestType, "HasBed");
        private static readonly MethodInfo feeMethod = bedType == null ? null : AccessTools.Method(bedType, "GetRentalFee");
        public static bool Enabled { get { return guestType != null && bedType != null; } }

        static HotelBillingHooks()
        {
            if (!Enabled) return;
            var driver = AccessTools.TypeByName("Hospitality.JobDriver_ClaimBed");
            var harmony = new Harmony("turbomegamod.hotel.daily.billing");
            harmony.Patch(AccessTools.Method(driver, "MakeNewToils"), postfix: new HarmonyMethod(typeof(HotelBillingHooks), "ClaimToils"));
            harmony.Patch(AccessTools.PropertyGetter(bedType,"RentalFee"),postfix:new HarmonyMethod(typeof(HotelBillingHooks),"EntryQuote"));
            foreach(var method in new[]{AccessTools.Method(AccessTools.TypeByName("Hospitality.JobGiver_ClaimBed"),"TryGiveJob"),
                AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),"FindBedFor"),
                AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),"IsAvailableBed")})
                harmony.Patch(method,prefix:new HarmonyMethod(typeof(HotelBillingHooks),"QuoteStart"){priority=Priority.First},finalizer:new HarmonyMethod(typeof(HotelBillingHooks),"QuoteEnd"));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),"IsAvailableBed"),postfix:new HarmonyMethod(typeof(HotelBillingHooks),"Sharing"));
            harmony.Patch(AccessTools.Method(bedType, "GetInspectString"), postfix: new HarmonyMethod(typeof(HotelBillingHooks), "BedInspect"));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.Mod_Hospitality"), "SettingsCategory"), postfix: new HarmonyMethod(typeof(HotelBillingHooks), "SettingsCategory"));
            var departure = AccessTools.Method(AccessTools.TypeByName("Hospitality.LordJob_VisitColony"), "OnLeaveTriggered");
            if (departure != null) harmony.Patch(departure, postfix: new HarmonyMethod(typeof(HotelBillingHooks), "RefreshDepartingGuests"));
        }
        private static void RefreshDepartingGuests(object __instance)
        {
            // Hospitality caches PresentGuests for up to 15000 ticks. Its path
            // patch otherwise keeps departing visitors inside the guest area.
            var lord = AccessTools.Field(__instance.GetType(), "lord").GetValue(__instance) as Verse.AI.Group.Lord;
            if (lord == null || lord.Map == null) return;
            var native = lord.Map.components.FirstOrDefault(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
            if (native != null) AccessTools.Method(native.GetType(), "RefreshGuestListTotal").Invoke(native, null);
            // Leaving removes guest-only needs (including joy). Finish the old
            // relaxation job before it ticks again against removed needs.
            foreach (var pawn in lord.ownedPawns.ToArray())
                if (pawn.Spawned && !pawn.Dead) pawn.jobs.StopAll();
        }
        public static ThingComp Guest(Pawn pawn) { return pawn == null || !Enabled ? null : pawn.AllComps.FirstOrDefault(c => guestType.IsInstanceOfType(c)); }
        public static Building_Bed Bed(Pawn pawn)
        {
            var guest = Guest(pawn);
            return guest == null || !(bool)hasBed.GetValue(guest, null) ? null : bedField.GetValue(guest) as Building_Bed;
        }
        public static int Fee(Building_Bed bed) { return bed == null || !Enabled || !bedType.IsInstanceOfType(bed) ? 0 : (int)feeMethod.Invoke(bed, null); }
        public static int Money(Pawn pawn) { return pawn.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount); }
        private static void ClaimToils(JobDriver __instance, ref IEnumerable<Toil> __result) { __result = WatchClaims(__instance, __result); }
        private static IEnumerable<Toil> WatchClaims(JobDriver driver, IEnumerable<Toil> toils)
        {
            // Starting a path while already touching the bed can run the next
            // toil inside this initAction. Observe that nested check-in once.
            bool observing = false;
            foreach (var toil in toils)
            {
                var original = toil.initAction;
                if (original != null)
                {
                    toil.initAction = delegate
                    {
                        if (observing) { original(); return; }
                        observing = true;
                        try
                        {
                            var pawn = driver.pawn;
                            // Completing the native toil can clear driver.job.
                            // Capture its identity before invoking it.
                            var claimJob=driver.job;
                            string receipt=claimJob==null?null:"hotel:"+pawn.Map.uniqueID+":"+claimJob.GetUniqueLoadID();
                            var target=claimJob==null?null:claimJob.targetA.Thing as Building_Bed;
                            if(target!=null&&!ShareAllowed(target,pawn)){driver.EndJobWith(JobCondition.Incompletable);return;}
                            var previous = Bed(pawn);
                            int before = Money(pawn);
                            int previousQuote=quoteDepth;quoteDepth++;
                            try{original();}finally{quoteDepth=previousQuote;}
                            var assigned = Bed(pawn);
                            if (assigned != null && assigned != previous && pawn.Spawned)
                                pawn.Map.GetComponent<HotelBilling>().RecordClaim(pawn, assigned, Math.Max(0, before - Money(pawn)),receipt);
                        }
                        finally { observing = false; }
                    };
                }
                yield return toil;
            }
        }
        public static bool ShareAllowed(Building_Bed bed,Pawn pawn)
        {
            return bed==null||bed.SleepingSlotsCount<=1||bed.CompAssignableToPawn.AssignedPawnsForReading
                .Where(other=>other!=null&&other!=pawn&&!other.Dead&&!other.Destroyed)
                .All(other=>LovePartnerRelationUtility.LovePartnerRelationExists(pawn,other));
        }
        private static void Sharing(Building_Bed __0,Pawn __1,ref bool __result){if(__result&&!ShareAllowed(__0,__1))__result=false;}
        private static void QuoteStart(out QuoteScope __state){__state=new QuoteScope{previous=quoteDepth};quoteDepth++;}
        private static Exception QuoteEnd(QuoteScope __state,Exception __exception){if(__state!=null)quoteDepth=__state.previous;return __exception;}
        private static void EntryQuote(Building_Bed __instance,ref int __result)
        {
            if(quoteDepth>0&&__instance.Spawned)__result=__instance.Map.GetComponent<HotelBilling>().EntryFee(__instance);
        }
        private static void BedInspect(Building_Bed __instance, ref string __result)
        {
            __result += "\n" + "TMM.Prices.BedDaily".Translate(Fee(__instance));
        }
        private static void SettingsCategory(ref string __result) { __result = "TMM.Hotel.Settings".Translate(); }
    }

    public sealed class HotelStay : IExposable
    {
        public Pawn guest;
        public Building_Bed bed;
        public int nextChargeTick;
        // A partially transferred physical payment must never be collected twice.
        public int remaining = -1;
        public int periodFee;
        public void ExposeData()
        {
            Scribe_References.Look(ref guest, "guest"); Scribe_References.Look(ref bed, "bed");
            Scribe_Values.Look(ref nextChargeTick, "nextChargeTick"); Scribe_Values.Look(ref remaining, "remaining", -1);
            Scribe_Values.Look(ref periodFee, "periodFee");
        }
    }

    public sealed class HotelBilling : MapComponent
    {
        public const int DayTicks = 60000;
        public List<HotelStay> stays = new List<HotelStay>();
        private int nextCheck;
        public HotelBilling(Map map) : base(map) { }
        private bool Valid(HotelStay stay)
        {
            return stay.guest != null && !stay.guest.Destroyed && !stay.guest.Dead && stay.guest.MapHeld == map &&
                stay.guest.Faction != Faction.OfPlayer && !stay.guest.IsPrisoner && !stay.guest.IsSlave &&
                stay.bed != null && stay.bed.Spawned && stay.bed.Map == map && HotelBillingHooks.Bed(stay.guest) == stay.bed;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving) stays.RemoveAll(s => !Valid(s));
            Scribe_Collections.Look(ref stays, "TMM_hotelStays", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && stays == null) stays = new List<HotelStay>();
        }
        private List<HotelStay> Group(Building_Bed bed){return stays.Where(s=>s.bed==bed&&Valid(s)).ToList();}
        private static HotelStay Canonical(IEnumerable<HotelStay> group)
        {
            // Previously per-guest saves may contain different paid deadlines.
            // Preserve the latest one, without inventing refunds or new revenue.
            return group.OrderByDescending(s=>s.nextChargeTick).ThenBy(s=>s.remaining>=0?1:0).ThenBy(s=>s.guest.thingIDNumber).FirstOrDefault();
        }
        private static void CopyPeriod(HotelStay source,IEnumerable<HotelStay> group)
        {
            foreach(var stay in group){stay.nextChargeTick=source.nextChargeTick;stay.remaining=source.remaining;stay.periodFee=source.periodFee;}
        }
        public int EntryFee(Building_Bed bed)
        {
            var period=Canonical(Group(bed));if(period==null)return HotelBillingHooks.Fee(bed);
            if(period.remaining>=0)return period.remaining;
            return Find.TickManager.TicksGame<period.nextChargeTick?0:HotelBillingHooks.Fee(bed);
        }
        public int SharedMoney(Building_Bed bed)
        {
            return (int)Math.Min(int.MaxValue,Group(bed).Sum(s=>(long)HotelBillingHooks.Money(s.guest)));
        }
        private void RecordPayment(Pawn guest,Building_Bed bed,int amount,string receipt)
        {
            if(amount<=0)return;ComplexWorkPolicy.Record(map,bed.Position,"hotel",amount);
            var link=GuestDossiers.Capture(guest,map);if(link!=null)link.Received("hotel",amount,receipt);
        }
        public void RecordClaim(Pawn guest, Building_Bed bed, int amountPaid, string receiptToken = null)
        {
            stays.RemoveAll(s=>s.guest==guest);var group=Group(bed);var period=Canonical(group);int now=Find.TickManager.TicksGame;
            var added=new HotelStay{guest=guest,bed=bed};
            if(period==null){
                int fee=HotelBillingHooks.Fee(bed);added.periodFee=fee;added.remaining=amountPaid>=fee?-1:fee-amountPaid;
                added.nextChargeTick=now+(amountPaid>=fee?DayTicks:0);
            }else{
                if(period.remaining<0&&now>=period.nextChargeTick){period.periodFee=HotelBillingHooks.Fee(bed);period.remaining=period.periodFee;}
                if(period.remaining>=0){
                    period.remaining=Math.Max(0,period.remaining-amountPaid);
                    if(period.remaining==0){period.remaining=-1;period.nextChargeTick+=DayTicks;}
                }
                CopyPeriod(period,group);CopyPeriod(period,new[]{added});
            }
            RecordPayment(guest,bed,amountPaid,receiptToken??GuestDossiers.Book.ReceiptToken("hotel",map));
            GuestProfiles.Claimed(guest);stays.Add(added);
        }
        public override void MapComponentTick()
        {
            if(!HotelBillingHooks.Enabled||Find.TickManager.TicksGame<nextCheck)return;
            nextCheck=Find.TickManager.TicksGame+250;ProcessPayments();
        }
        public void ProcessPayments()
        {
            if(!HotelBillingHooks.Enabled)return;stays.RemoveAll(s=>!Valid(s));
            // A pre-existing native assignment has already paid on check-in.
            foreach(var pawn in map.mapPawns.AllPawnsSpawned){
                if(pawn.Faction==Faction.OfPlayer||pawn.IsPrisoner||pawn.IsSlave||stays.Any(s=>s.guest==pawn))continue;
                var bed=HotelBillingHooks.Bed(pawn);if(bed==null)continue;
                var imported=new HotelStay{guest=pawn,bed=bed,nextChargeTick=Find.TickManager.TicksGame+DayTicks};
                var existing=Canonical(Group(bed));if(existing!=null)CopyPeriod(existing,new[]{imported});stays.Add(imported);
            }
            foreach(var group in stays.GroupBy(s=>s.bed).Select(g=>g.OrderBy(s=>s.guest.thingIDNumber).ToList()).ToArray()){
                var period=Canonical(group);if(period==null)continue;CopyPeriod(period,group);
                // Preserve one shared deadline/debt while any occupant is held.
                if(group.Any(s=>!s.guest.Spawned||s.guest.Map!=map))continue;
                for(int days=0;days<4&&Find.TickManager.TicksGame>=period.nextChargeTick;days++){
                    if(period.remaining<0){period.periodFee=HotelBillingHooks.Fee(period.bed);period.remaining=period.periodFee;CopyPeriod(period,group);}
                    if(group.Sum(s=>(long)HotelBillingHooks.Money(s.guest))<period.remaining){
                        foreach(var stay in group){
                            GuestDeathrest.Cancel(stay.guest);stay.guest.ownership.UnclaimBed();stays.Remove(stay);
                            Messages.Message("TMM.Hotel.CannotExtend".Translate(stay.guest.LabelShortCap),stay.guest,MessageTypeDefOf.NeutralEvent,false);
                        }
                        break;
                    }
                    // Each actual coin contributes once to the shared bed bill.
                    // Stable pawn IDs determine who contributes first.
                    foreach(var stay in group){
                        foreach(var silver in stay.guest.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray()){
                            if(period.remaining==0)break;int count=Math.Min(period.remaining,silver.stackCount);
                            int beforeMoney=HotelBillingHooks.Money(stay.guest),beforeRemaining=period.remaining;Thing dropped;
                            if(!stay.guest.inventory.innerContainer.TryDrop(silver,stay.guest.Position,map,ThingPlaceMode.Near,count,out dropped))break;
                            int paid=beforeMoney-HotelBillingHooks.Money(stay.guest);if(paid<=0)break;
                            period.remaining=Math.Max(0,period.remaining-paid);CopyPeriod(period,group);
                            RecordPayment(stay.guest,period.bed,paid,"hotel-bed-day:"+map.uniqueID+":"+period.bed.GetUniqueLoadID()+":"+period.nextChargeTick+":"+stay.guest.GetUniqueLoadID()+":"+beforeRemaining);
                        }
                        if(period.remaining==0)break;
                    }
                    if(period.remaining!=0)break;period.remaining=-1;period.nextChargeTick+=DayTicks;CopyPeriod(period,group);
                }
            }
        }
    }
}
