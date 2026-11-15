using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    public static class ClinicMedicalHooks
    {
        public sealed class Procedure
        {
            public Pawn patient, worker;
            public float before, materials;
            public bool consumables;
        }
        public static void Install(Harmony h)
        {
            var upstream=AccessTools.TypeByName("Hospital.Patches.Billing_Tend_Patch+BillPatientsForMedicine");
            h.Patch(AccessTools.Method(upstream,"Prefix"),prefix:new HarmonyMethod(typeof(ClinicMedicalHooks),"SkipEarlyTendCharge"));
            h.Patch(AccessTools.Method(typeof(TendUtility),"DoTend"),
                prefix:new HarmonyMethod(typeof(ClinicMedicalHooks),"BeforeTend"),postfix:new HarmonyMethod(typeof(ClinicMedicalHooks),"AfterTend"));
            h.Patch(AccessTools.Method(typeof(Bill),"Notify_IterationCompleted"),
                prefix:new HarmonyMethod(typeof(ClinicMedicalHooks),"BeforeSurgery"),
                postfix:new HarmonyMethod(typeof(ClinicMedicalHooks),"AfterSurgery"){priority=Priority.Last,after=new[]{"Adamas.Hospital"}});
        }
        private static bool SkipEarlyTendCharge() { return false; }
        private static void BeforeTend(Pawn doctor,Pawn patient,Medicine medicine,out Procedure __state)
        {
            __state=null;
            if(patient==null||doctor==null||doctor==patient||doctor.Faction!=Faction.OfPlayer||medicine==null||
                !Clinic.ActivePatient(patient)||!patient.health.HasHediffsNeedingTend())return;
            __state=new Procedure{patient=patient,worker=doctor,before=ClinicContributions.Bill(patient),materials=Math.Min(70f,Math.Max(0f,medicine.MarketValue*1.2f))};
        }
        private static void AfterTend(Procedure __state)
        {
            if(__state==null||!Clinic.ActivePatient(__state.patient))return;
            // Charge only after DoTend returned, then attribute its real
            // clamped increment. Bare-hand/self tending keeps upstream price0.
            var utility=AccessTools.TypeByName("Hospital.Utilities.PatientUtility");
            AccessTools.Method(utility,"AddToBill").Invoke(null,new object[]{__state.patient,Clinic.Hospital(__state.patient.MapHeld),__state.materials});
            ClinicContributions.Record(__state.patient,__state.worker,__state.before,0,__state.materials);
        }
        private static void BeforeSurgery(Bill __instance,Pawn billDoer,List<Thing> ingredients,out Procedure __state)
        {
            __state=null;
            var patient=__instance is Bill_Medical&&__instance.billStack!=null?__instance.billStack.billGiver as Pawn:null;
            if(patient==null||!Clinic.ActivePatient(patient)||__instance.recipe==null)return;
            // Attribute the actual upstream increment, not another estimate of
            // its pricing formula. A service requiring consumables gets the
            // 50% rate on the whole service, including its labour charge.
            __state=new Procedure{patient=patient,worker=billDoer,before=ClinicContributions.Bill(patient),
                consumables=(ingredients!=null&&ingredients.Count>0)||(__instance.recipe.ingredients!=null&&__instance.recipe.ingredients.Count>0)};
        }
        private static void AfterSurgery(Procedure __state)
        {
            if(__state!=null)ClinicContributions.Record(__state.patient,__state.worker,__state.before,__state.consumables?0:1,__state.consumables?1:0);
        }
    }
}
