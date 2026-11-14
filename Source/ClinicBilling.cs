using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public sealed class ClinicReceipt : IExposable, IThingHolder
    {
        public string token;
        public IntVec3 cell;
        private ThingOwner<Thing> cash;
        public ClinicReceipt() { cash = new ThingOwner<Thing>(this); }
        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return cash; }
        public void GetChildHolders(List<IThingHolder> holders) { ThingOwnerUtility.AppendThingHoldersFromThings(holders, cash); }
        public void ExposeData()
        {
            Scribe_Values.Look(ref token, "token"); Scribe_Values.Look(ref cell, "cell"); Scribe_Deep.Look(ref cash, "cash", this);
        }
    }

    public sealed class ClinicAccounts : MapComponent
    {
        public List<string> settled = new List<string>();
        public List<ClinicReceipt> receipts = new List<ClinicReceipt>();
        private int nextAttempt;
        public ClinicAccounts(Map map) : base(map) { }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Collections.Look(ref settled, "TMM_clinicSettled", LookMode.Value);
            Scribe_Collections.Look(ref receipts, "TMM_clinicReceipts", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) { if (settled == null) settled = new List<string>(); if (receipts == null) receipts = new List<ClinicReceipt>(); }
        }
        public void Queue(string token, IntVec3 cell, int coins)
        {
            settled.Add(token);
            if (coins <= 0) return;
            var receipt = new ClinicReceipt { token = token, cell = cell };
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver); silver.stackCount = coins;
            receipt.GetDirectlyHeldThings().TryAdd(silver, false); receipts.Add(receipt);
        }
        public void PlaceReceipts()
        {
            foreach (var receipt in receipts.ToArray()) {
                var cash = receipt.GetDirectlyHeldThings();
                foreach (var silver in cash.ToArray()) {
                    int before = cash.Sum(t => t.stackCount); Thing dropped;
                    cash.TryDrop(silver, receipt.cell, map, ThingPlaceMode.Near, silver.stackCount, out dropped);
                    int placed = before - cash.Sum(t => t.stackCount);
                    if (placed > 0) ComplexWorkPolicy.Record(map, receipt.cell, "clinic", placed);
                }
                if (cash.Count == 0) receipts.Remove(receipt);
            }
        }
        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame < nextAttempt) return;
            nextAttempt = Find.TickManager.TicksGame + 250; PlaceReceipts();
        }
    }

    public static class ClinicBilling
    {
        public static Action<Pawn> Discharged;
        private static readonly Type utility = AccessTools.TypeByName("Hospital.Utilities.PatientUtility");
        public static void Install(Harmony h)
        {
            h.Patch(AccessTools.Method(Clinic.HospitalType, "PatientLeaves"), prefix: new HarmonyMethod(typeof(ClinicBilling), "Discharge"));
            var feed = AccessTools.TypeByName("Hospital.Patches.Billing_Feed_Patch+BillPatientsForFood");
            h.Patch(AccessTools.Method(feed, "Postfix"), prefix: new HarmonyMethod(typeof(ClinicBilling), "FeedBilling"));
        }
        private static bool FeedBilling(JobDriver_FoodFeedPatient __0)
        {
            // Replace the upstream unconditional finish callback. Resolving
            // targets here would be too early during save restoration.
            var driver = __0;
            driver.AddFinishAction(condition => {
                if (condition != JobCondition.Succeeded) return;
                var patient = AccessTools.Property(driver.GetType(), "Deliveree").GetValue(driver, null) as Pawn;
                var food = AccessTools.Property(driver.GetType(), "Food").GetValue(driver, null) as Thing;
                if (patient == null || food == null || !Clinic.ActivePatient(patient)) return;
                AccessTools.Method(utility, "AddToBill").Invoke(null, new object[] { patient, Clinic.Hospital(patient.Map), Math.Min(70f, Math.Max(0f, food.MarketValue * 1.1f)) });
            });
            return false;
        }
        private static bool Discharge(object __instance, Pawn __0)
        {
            var pawn = __0; var map = Clinic.MapOf(__instance);
            var patients = AccessTools.Field(Clinic.HospitalType, "Patients").GetValue(__instance) as IDictionary;
            if (pawn == null || patients == null || !patients.Contains(pawn)) return true;
            var data = patients[pawn]; var dismissed = AccessTools.Field(data.GetType(), "Dismissed");
            if ((bool)dismissed.GetValue(data)) return false;
            var accounts = map.GetComponent<ClinicAccounts>();
            string token = pawn.GetUniqueLoadID() + "@" + AccessTools.Field(data.GetType(), "ArrivedAtTick").GetValue(data);
            if (accounts.settled.Contains(token)) { dismissed.SetValue(data, true); return false; }
            float amount = (float)AccessTools.Method(utility, "CalculateSilverToReceive").Invoke(null, new[] { (object)pawn, data });
            int coins = (int)Math.Min(4000f, Math.Max(0f, amount));
            int goodwill = pawn.Faction == null ? 0 : (int)AccessTools.Method(utility, "CalculateGoodwillToGain").Invoke(null, new[] { (object)pawn, data });
            var desk = Clinic.Desk(map); var cell = desk == null ? pawn.PositionHeld : desk.Position;
            accounts.Queue(token, cell, coins);
            dismissed.SetValue(data, true);
            AccessTools.Method(Clinic.HospitalType, "RemoveFromPatientList").Invoke(__instance, new object[] { pawn });
            if (pawn.Faction != null) pawn.Faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, false);
            accounts.PlaceReceipts();
            bool pending = accounts.receipts.Any(r => r.token == token);
            Messages.Message((pending ? "TMM.Clinic.DischargePending" : "TMM.Clinic.DischargePaid").Translate(pawn.LabelShortCap, coins, goodwill), MessageTypeDefOf.NeutralEvent, false);
            if (Discharged != null) Discharged(pawn);
            return false;
        }
    }
}
