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
        public int paid;
        public List<ClinicFrozenShare> shares;
        public List<string> subreceipts = new List<string>();
        private ThingOwner<Thing> cash;
        public ClinicReceipt() { cash = new ThingOwner<Thing>(this); }
        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return cash; }
        public void GetChildHolders(List<IThingHolder> holders) { ThingOwnerUtility.AppendThingHoldersFromThings(holders, cash); }
        public void ExposeData()
        {
            Scribe_Values.Look(ref token, "token"); Scribe_Values.Look(ref cell, "cell"); Scribe_Deep.Look(ref cash, "cash", this);
            Scribe_Values.Look(ref paid,"paid");
            Scribe_Collections.Look(ref shares,"shares",LookMode.Deep);
            Scribe_Collections.Look(ref subreceipts,"subreceipts",LookMode.Value);
            if(Scribe.mode==LoadSaveMode.PostLoadInit){
                if(cash==null)cash=new ThingOwner<Thing>(this);
                if(subreceipts==null)subreceipts=new List<string>();
                // Old pending receipts have no known performer. Preserve
                // their real money and do not invent retroactive labour.
                if(shares==null)shares=new List<ClinicFrozenShare>{new ClinicFrozenShare{coins=cash.Sum(t=>t.stackCount)}};
                shares.RemoveAll(s=>s==null);
            }
        }
    }

    public sealed class ClinicAccounts : MapComponent
    {
        public List<string> settled = new List<string>();
        public List<ClinicReceipt> receipts = new List<ClinicReceipt>();
        public List<ClinicEpisode> episodes = new List<ClinicEpisode>();
        private int nextAttempt;
        public ClinicAccounts(Map map) : base(map) { }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Collections.Look(ref settled, "TMM_clinicSettled", LookMode.Value);
            Scribe_Collections.Look(ref receipts, "TMM_clinicReceipts", LookMode.Deep);
            Scribe_Collections.Look(ref episodes,"TMM_clinicContributions",LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) { if (settled == null) settled = new List<string>(); if (receipts == null) receipts = new List<ClinicReceipt>(); if(episodes==null)episodes=new List<ClinicEpisode>();
                receipts.RemoveAll(r=>r==null);episodes.RemoveAll(e=>e==null); }
        }
        public void Queue(string token, IntVec3 cell, int coins, List<ClinicFrozenShare> shares = null)
        {
            if(settled.Contains(token))return;
            settled.Add(token);
            if (coins <= 0) return;
            var receipt = new ClinicReceipt { token = token, cell = cell, shares=shares??new List<ClinicFrozenShare>{new ClinicFrozenShare{coins=coins}} };
            for(int remaining=coins;remaining>0;){
                var silver = ThingMaker.MakeThing(ThingDefOf.Silver); silver.stackCount = Math.Min(remaining,ThingDefOf.Silver.stackLimit);
                receipt.GetDirectlyHeldThings().TryAdd(silver, false);remaining-=silver.stackCount;
            }
            receipts.Add(receipt);
        }
        public void PlaceReceipts()
        {
            foreach (var receipt in receipts.ToArray()) {
                var cash = receipt.GetDirectlyHeldThings();
                foreach (var silver in cash.ToArray()) {
                    int before = cash.Sum(t => t.stackCount); Thing dropped;
                    cash.TryDrop(silver, receipt.cell, map, ThingPlaceMode.Near, silver.stackCount, out dropped);
                    int placed = before - cash.Sum(t => t.stackCount);
                    if (placed > 0) { ComplexWorkPolicy.Record(map, receipt.cell, "clinic", placed); ClinicContributions.Credit(receipt,placed); }
                }
                if (cash.Count == 0) receipts.Remove(receipt);
            }
        }
        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame < nextAttempt) return;
            nextAttempt = Find.TickManager.TicksGame + 250; PlaceReceipts();
            episodes.RemoveAll(e=>e.patient==null||!Clinic.ActivePatient(e.patient)||ClinicContributions.Token(e.patient)!=e.token);
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
            ClinicMedicalHooks.Install(h);
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
                float before=ClinicContributions.Bill(patient),cost=Math.Min(70f,Math.Max(0f,food.MarketValue*1.1f));
                AccessTools.Method(utility, "AddToBill").Invoke(null, new object[] { patient, Clinic.Hospital(patient.Map), cost });
                ClinicContributions.Record(patient,driver.pawn,before,0,cost);
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
            accounts.Queue(token, cell, coins,ClinicContributions.Freeze(accounts,pawn,coins));
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
