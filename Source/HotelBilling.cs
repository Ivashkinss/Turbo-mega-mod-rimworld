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
                            var previous = Bed(pawn);
                            int before = Money(pawn);
                            original();
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
        private static void BedInspect(Building_Bed __instance, ref string __result)
        {
            __result += "\n" + "TMM.Hotel.DailyRate".Translate(Fee(__instance));
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
            return stay.guest != null && stay.guest.Spawned && !stay.guest.Dead && stay.guest.Map == map &&
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
        public void RecordClaim(Pawn guest, Building_Bed bed, int amountPaid, string receiptToken = null)
        {
            stays.RemoveAll(s => s.guest == guest);
            int fee = HotelBillingHooks.Fee(bed);
            ComplexWorkPolicy.Record(map, bed.Position, "hotel", Math.Min(fee, amountPaid));
            var link=GuestDossiers.Capture(guest,map);
            if(link!=null)link.Received("hotel",Math.Min(fee,amountPaid),receiptToken??GuestDossiers.Book.ReceiptToken("hotel",map));
            GuestProfiles.Claimed(guest);
            stays.Add(new HotelStay { guest = guest, bed = bed,
                nextChargeTick = Find.TickManager.TicksGame + (amountPaid >= fee ? DayTicks : 0),
                remaining = amountPaid >= fee ? -1 : fee - amountPaid, periodFee = fee });
        }
        public override void MapComponentTick()
        {
            if (!HotelBillingHooks.Enabled || Find.TickManager.TicksGame < nextCheck) return;
            nextCheck = Find.TickManager.TicksGame + 250;
            ProcessPayments();
        }
        public void ProcessPayments()
        {
            if (!HotelBillingHooks.Enabled) return;
            stays.RemoveAll(s => !Valid(s));
            // Existing Hospitality saves have already paid for their current
            // stay. Start a fresh day without charging that initial payment again.
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Faction == Faction.OfPlayer || pawn.IsPrisoner || pawn.IsSlave || stays.Any(s => s.guest == pawn)) continue;
                var bed = HotelBillingHooks.Bed(pawn);
                if (bed != null) stays.Add(new HotelStay { guest = pawn, bed = bed, nextChargeTick = Find.TickManager.TicksGame + DayTicks });
            }
            foreach (var stay in stays.ToArray())
            {
                // Bounded catch-up for controlled time jumps; ordinary play
                // processes one period at a time. Each period keeps its own rate.
                for (int periods = 0; periods < 4 && Find.TickManager.TicksGame >= stay.nextChargeTick; periods++)
                {
                    if (stay.remaining < 0) { stay.periodFee = HotelBillingHooks.Fee(stay.bed); stay.remaining = stay.periodFee; }
                    if (HotelBillingHooks.Money(stay.guest) < stay.remaining)
                    {
                        stay.guest.ownership.UnclaimBed();
                        stays.Remove(stay);
                        Messages.Message("TMM.Hotel.CannotExtend".Translate(stay.guest.LabelShortCap), stay.guest, MessageTypeDefOf.NeutralEvent, false);
                        break;
                    }
                    foreach (var silver in stay.guest.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).ToArray())
                    {
                        if (stay.remaining == 0) break;
                        int count = Math.Min(stay.remaining, silver.stackCount);
                        int beforeMoney=HotelBillingHooks.Money(stay.guest),beforeRemaining=stay.remaining;
                        Thing dropped;
                        if (!stay.guest.inventory.innerContainer.TryDrop(silver, stay.guest.Position, map, ThingPlaceMode.Near, count, out dropped)) break;
                        stay.remaining -= count;
                        ComplexWorkPolicy.Record(map, stay.bed.Position, "hotel", count);
                        var link=GuestDossiers.Capture(stay.guest,map);
                        if(link!=null)link.Received("hotel",beforeMoney-HotelBillingHooks.Money(stay.guest),"hotel-day:"+map.uniqueID+":"+stay.guest.GetUniqueLoadID()+":"+stay.nextChargeTick+":"+beforeRemaining);
                    }
                    if (stay.remaining != 0) break;
                    stay.remaining = -1;
                    stay.nextChargeTick += DayTicks;
                }
            }
        }
    }
}
