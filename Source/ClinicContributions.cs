using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    public sealed class ClinicContribution : IExposable
    {
        public Pawn worker;
        public string workerId;
        public bool consumables;
        public float weight;
        public void ExposeData()
        {
            Scribe_References.Look(ref worker,"worker"); Scribe_Values.Look(ref workerId,"workerId");
            Scribe_Values.Look(ref consumables,"consumables"); Scribe_Values.Look(ref weight,"weight");
        }
    }
    public sealed class ClinicEpisode : IExposable
    {
        public Pawn patient;
        public string token;
        public List<ClinicContribution> contributions = new List<ClinicContribution>();
        public void ExposeData()
        {
            Scribe_References.Look(ref patient,"patient"); Scribe_Values.Look(ref token,"token");
            Scribe_Collections.Look(ref contributions,"contributions",LookMode.Deep);
            if(Scribe.mode==LoadSaveMode.PostLoadInit){if(contributions==null)contributions=new List<ClinicContribution>();contributions.RemoveAll(c=>c==null);}
        }
    }
    public sealed class ClinicFrozenShare : IExposable
    {
        public Pawn worker;
        public string workerId;
        public bool consumables;
        public int coins;
        public void ExposeData()
        {
            Scribe_References.Look(ref worker,"worker"); Scribe_Values.Look(ref workerId,"workerId");
            Scribe_Values.Look(ref consumables,"consumables"); Scribe_Values.Look(ref coins,"coins");
        }
    }
    public static class ClinicContributions
    {
        public static string Token(Pawn patient)
        {
            var data=Clinic.Data(patient);
            return data==null?null:patient.GetUniqueLoadID()+"@"+AccessTools.Field(data.GetType(),"ArrivedAtTick").GetValue(data);
        }
        public static float Bill(Pawn patient)
        {
            var data=Clinic.Data(patient);
            return data==null?0f:(float)AccessTools.Field(data.GetType(),"Bill").GetValue(data);
        }
        private static bool Finite(float value) { return !float.IsNaN(value)&&!float.IsInfinity(value); }
        private static void Add(ClinicEpisode episode,Pawn worker,bool consumables,float value)
        {
            if(value<=0||!Finite(value))return;
            if(worker!=null&&worker.Faction!=Faction.OfPlayer)worker=null;
            string id=worker==null?null:worker.GetUniqueLoadID();
            var claim=episode.contributions.FirstOrDefault(c=>c.workerId==id&&c.consumables==consumables);
            if(claim==null){claim=new ClinicContribution{worker=worker,workerId=id,consumables=consumables};episode.contributions.Add(claim);}
            claim.weight+=value;
        }
        public static ClinicEpisode Episode(ClinicAccounts accounts,Pawn patient,float currentBill)
        {
            string token=Token(patient);if(token==null)return null;
            var episode=accounts.episodes.FirstOrDefault(e=>e.token==token);
            if(episode==null){episode=new ClinicEpisode{patient=patient,token=token};accounts.episodes.Add(episode);}
            // Legacy bills and amounts from unrecognised providers stay
            // unattributed instead of rewarding the next doctor for them.
            Add(episode,null,false,Math.Max(0,currentBill-episode.contributions.Sum(c=>c.weight)));
            return episode;
        }
        public static void Record(Pawn patient,Pawn worker,float before,float labor,float materials)
        {
            if(patient==null||patient.MapHeld==null||!Clinic.ActivePatient(patient)||!Finite(before))return;
            float after=Bill(patient),delta=after-before;
            if(!Finite(delta)||delta<=0||!Finite(labor)||!Finite(materials)||labor<0||materials<0)return;
            var episode=Episode(patient.MapHeld.GetComponent<ClinicAccounts>(),patient,before);
            if(episode==null)return;
            float total=labor+materials;
            if(total<=0){Add(episode,null,false,delta);return;}
            float pure=delta*(labor/total);
            Add(episode,worker,false,pure);Add(episode,worker,true,delta-pure);
        }
        public static List<ClinicFrozenShare> Freeze(ClinicAccounts accounts,Pawn patient,int coins)
        {
            var episode=Episode(accounts,patient,Bill(patient));
            var claims=episode==null?new List<ClinicContribution>():episode.contributions;
            var plan=MedicalAllocation.Freeze(coins,claims.Where(c=>Finite(c.weight)&&c.weight>0).Select(c=>
                new MedicalClaim{workerId=c.workerId,consumables=c.consumables,weight=(decimal)c.weight}));
            var result=plan.Select(s=>new ClinicFrozenShare{workerId=s.workerId,consumables=s.consumables,coins=s.coins,
                worker=claims.Where(c=>c.workerId==s.workerId).Select(c=>c.worker).FirstOrDefault()}).ToList();
            if(episode!=null)accounts.episodes.Remove(episode);
            return result;
        }
        public static void Credit(ClinicReceipt receipt,int placed)
        {
            var payment=new MedicalPayment{token=receipt.token,paid=receipt.paid,processed=receipt.subreceipts,
                shares=receipt.shares.Select(s=>new MedicalShare{workerId=s.workerId,consumables=s.consumables,coins=s.coins}).ToList()};
            string part=receipt.paid.ToString(System.Globalization.CultureInfo.InvariantCulture);
            List<MedicalShare> portions;
            if(payment.Accept(receipt.token,part,placed,out portions)!=MedicalPaymentStatus.Accepted)return;
            receipt.paid=payment.paid;
            var buyouts=Current.Game.GetComponent<WorkBuyouts>();
            foreach(var portion in portions){
                var share=receipt.shares.First(s=>s.workerId==portion.workerId&&s.consumables==portion.consumables);
                if(share.worker!=null&&share.workerId!=null)buyouts.Credit(share.worker,"clinic:"+receipt.token+":"+part+":"+(share.consumables?"c":"l"),portion.coins,share.consumables);
            }
        }
    }
}
