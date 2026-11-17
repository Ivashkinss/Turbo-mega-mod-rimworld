using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public static class WorkContracts
    {
        public static bool WillAccept(Pawn pawn)
        {
            int seed=GenText.StableStringHash("TMM:ordinary-contract:"+Find.World.info.seedString+":"+pawn.GetUniqueLoadID());
            Rand.PushState(seed);try{return Rand.Chance(0.9f);}finally{Rand.PopState();}
        }
        public static bool Working(WorkBuyoutEntry e)
        {
            if(e==null||!e.active||e.released||e.pawn==null||e.pawn.Dead||e.pawn.Destroyed||FactionRansoms.HasAgreedRansom(e.pawn))return false;
            return e.freeContract?e.pawn.Faction==Faction.OfPlayer&&!e.pawn.IsSlave&&!e.pawn.IsPrisoner:e.pawn.IsSlaveOfColony;
        }
        public static WorkBuyoutEntry Offer(Pawn pawn,int target)
        {
            if(pawn==null)return null;var book=Current.Game.GetComponent<WorkBuyouts>();var e=book.Entry(pawn);
            if(e!=null&&e.contractOffered)return e;
            var ransoms=Current.Game.GetComponent<FactionRansoms>();
            var rEntry=ransoms==null?null:ransoms.Entry(pawn);
            if(rEntry!=null&&rEntry.agreed)return null;
            int effectiveTarget=(rEntry!=null&&!rEntry.agreed)?rEntry.targetAmount:target;
            if(pawn==null||!pawn.Spawned||pawn.Dead||pawn.Downed||pawn.InMentalState||!pawn.IsSlaveOfColony||effectiveTarget<1)return null;
            if(PendingOperations(pawn)){Messages.Message("TMM.Contract.Unavailable".Translate(),pawn,MessageTypeDefOf.RejectInput,false);return null;}
            e=e??book.Begin(pawn,effectiveTarget);if(e==null)return null;
            e.originalFaction=pawn.HomeFaction;e.contractOffered=true;e.joinRequested=!WillAccept(pawn);
            if(!e.joinRequested){e.freeContract=true;e.active=true;Free(pawn);}else e.active=false;
            Messages.Message((e.joinRequested?"TMM.Contract.JoinRequested":"TMM.Contract.Accepted").Translate(pawn.LabelShortCap),pawn,MessageTypeDefOf.NeutralEvent,false);
            return e;
        }
        private static bool PendingOperations(Pawn pawn){return pawn.BillStack!=null&&pawn.BillStack.Bills.Any();}
        private static void Free(Pawn pawn)
        {
            var map=pawn.MapHeld;var desk=map==null?null:map.GetComponent<ComplexMap>().desk;
            var employee=desk==null?null:desk.Employee(pawn);bool resident=employee!=null&&employee.resident;
            if(resident)desk.SetResidence(pawn,false);
            var field=AccessTools.Field(typeof(Pawn_WorkSettings),"priorities");
            var raw=pawn.workSettings==null?null:field.GetValue(pawn.workSettings) as DefMap<WorkTypeDef,int>;
            var before=raw==null?null:DefDatabase<WorkTypeDef>.AllDefsListForReading.ToDictionary(w=>w,w=>raw[w]);
            pawn.guest.SetGuestStatus(null,GuestStatus.Guest);
            if(pawn.Faction!=Faction.OfPlayer)pawn.SetFaction(Faction.OfPlayer);
            pawn.needs.AddOrRemoveNeedsAsAppropriate();
            if(before!=null&&pawn.workSettings!=null)foreach(var pair in before)pawn.workSettings.SetPriority(pair.Key,pawn.WorkTypeIsDisabled(pair.Key)?0:pair.Value);
            if(resident)desk.SetResidence(pawn,true);
        }
        public static bool AcceptPermanent(Pawn pawn)
        {
            var e=Current.Game.GetComponent<WorkBuyouts>().Entry(pawn);
            if(e==null||!e.contractOffered||!e.joinRequested||e.joinedPermanently||!pawn.IsSlaveOfColony||!pawn.Spawned||pawn.Dead||pawn.Downed||pawn.InMentalState||FactionRansoms.HasAgreedRansom(pawn))return false;
            if(PendingOperations(pawn)){Messages.Message("TMM.Contract.Unavailable".Translate(),pawn,MessageTypeDefOf.RejectInput,false);return false;}
            Free(pawn);e.joinedPermanently=true;e.active=false;return true;
        }
        private static void Detach(Pawn pawn)
        {
            foreach(var map in Find.Maps){
                var desk=map.GetComponent<ComplexMap>().desk;if(desk==null)continue;var employee=desk.Employee(pawn);if(employee==null)continue;
                map.GetComponent<CompanyService>().Cancel(pawn);desk.SetCompany(pawn,false);desk.SetRole(pawn,ComplexRole.None);desk.SetResidence(pawn,false);desk.employees.Remove(employee);
            }
        }
        public static bool Complete(WorkBuyoutEntry e)
        {
            if(!Working(e)||!e.freeContract||e.Remaining!=0||FactionRansoms.HasAgreedRansom(e.pawn))return false;var pawn=e.pawn;
            if(!pawn.Spawned||pawn.Downed||pawn.InMentalState||PendingOperations(pawn))return false;
            IntVec3 exit;if(!RCellFinder.TryFindBestExitSpot(pawn,out exit,TraverseMode.ByPawn,false))return false;
            if(pawn.carryTracker!=null&&pawn.carryTracker.CarriedThing!=null){Thing dropped;if(!pawn.carryTracker.TryDropCarriedThing(pawn.Position,ThingPlaceMode.Near,out dropped))return false;}
            Detach(pawn);pawn.jobs.StopAll();pawn.guest.SetGuestStatus(null,GuestStatus.Guest);
            if(pawn.Faction!=e.originalFaction)pawn.SetFaction(e.originalFaction);
            AccessTools.Method(typeof(GenGuest),"GuestRelease").Invoke(null,new object[]{pawn});
            e.released=true;e.active=false;return true;
        }
    }
}
