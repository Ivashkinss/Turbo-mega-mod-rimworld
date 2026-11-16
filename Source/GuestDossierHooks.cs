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
    public sealed class LordJob_RefusedGuest : LordJob
    {
        public override bool CanOpenAnyDoor(Pawn pawn){return false;}
        public override StateGraph CreateGraph(){var graph=new StateGraph();graph.AddToil(new LordToil_ExitMap(LocomotionUrgency.Walk,false,true));return graph;}
    }
    [StaticConstructorOnStartup]
    public static class GuestDossierHooks
    {
        static GuestDossierHooks()
        {
            var comp=AccessTools.TypeByName("Hospitality.CompGuest");if(comp==null)return;
            var h=new Harmony("turbomegamod.guest.dossiers");
            h.Patch(AccessTools.Method(typeof(Pawn),"SpawnSetup"),postfix:new HarmonyMethod(typeof(GuestDossierHooks),"Respawned"));
            h.Patch(AccessTools.Method(comp,"Arrive"),postfix:new HarmonyMethod(typeof(GuestDossierHooks),"Arrived"));
            h.Patch(AccessTools.Method(comp,"Leave"),postfix:new HarmonyMethod(typeof(GuestDossierHooks),"Leaving"));
            h.Patch(AccessTools.Method(comp,"PostDeSpawn"),postfix:new HarmonyMethod(typeof(GuestDossierHooks),"Despawned"));
            var worker=AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup");var core=AccessTools.Method(worker,"CreateLordCore");
            h.Patch(core??AccessTools.Method(worker,"CreateLord"),
                prefix:new HarmonyMethod(typeof(GuestDossierHooks),core==null?"FilterLegacy":"Filter"),
                postfix:new HarmonyMethod(typeof(GuestDossierHooks),"SendRefused"));
        }
        private static void Respawned(Pawn __instance)
        {
            // CompGuest.PostDeSpawn removes a carried visitor from PresentGuests.
            // Its accepted lord and arrived flag survive clinical transport.
            // Restore the native registry before guest AI runs on the map again.
            if(!__instance.Spawned||!GuestDossiers.Arrived(__instance))return;
            var native=__instance.Map.components.FirstOrDefault(c=>c.GetType().FullName=="Hospitality.Hospitality_MapComponent");
            if(native!=null)AccessTools.Method(native.GetType(),"RefreshGuestListTotal").Invoke(native,null);
        }
        private static Pawn PawnOf(ThingComp comp){return comp.parent as Pawn;}
        private static void Arrived(ThingComp __instance)
        {
            var pawn=PawnOf(__instance);if(pawn!=null&&pawn.MapHeld!=null&&GuestDossiers.Book!=null)
                GuestDossiers.Book.Begin(pawn,pawn.MapHeld,false,Clinic.ActivePatient(pawn));
        }
        private static void Leaving(ThingComp __instance){if(GuestDossiers.Book!=null)GuestDossiers.Book.Leaving(PawnOf(__instance));}
        private static void Despawned(ThingComp __instance,Map __0)
        {
            if(GuestDossiers.Book!=null&&__0!=null)GuestDossiers.Book.Despawned(PawnOf(__instance),__0);
        }
        private static bool FilterLegacy(ref List<Pawn> pawns,Map map,out List<Pawn> __state){return Filter(ref pawns,map,false,out __state);}
        private static bool Filter(ref List<Pawn> pawns,Map map,bool recoveredOnMap,out List<Pawn> __state)
        {
            __state=new List<Pawn>();
            if(recoveredOnMap||pawns==null||map==null||GuestDossiers.Desk(map)==null||GuestDossiers.Book==null)return true;
            __state=pawns.Where(p=>p!=null&&p.Spawned&&p.Map==map&&GuestDossiers.Blocked(p)&&
                !GuestDossiers.HasCurrent(p,map)&&!Clinic.ActivePatient(p)&&ClinicGuests.Visit(p)==null).ToList();
            if(__state.Count==0)return true;
            // Replace the argument, not the caller's list of physically
            // spawned pawns; its cleanup still needs to know every pawn.
            var denied=__state;pawns=pawns.Where(p=>!denied.Contains(p)).ToList();return pawns.Count>0;
        }
        private static void SendRefused(List<Pawn> __state,Map map)
        {
            if(__state==null||__state.Count==0||map==null)return;
            foreach(var group in __state.Where(p=>p!=null&&p.Spawned&&p.Map==map).GroupBy(p=>p.Faction)){
                var pawns=group.ToList();
                foreach(var pawn in pawns){
                    var old=pawn.GetLord();if(old!=null)old.Notify_PawnLost(pawn,PawnLostCondition.Undefined);
                    var comp=HotelBillingHooks.Guest(pawn);if(comp!=null)AccessTools.Method(comp.GetType(),"Leave").Invoke(comp,new object[]{true});
                    var row=GuestDossiers.Book.Observe(pawn);row.rejectedCount++;
                    Messages.Message("TMM.Dossier.Denied".Translate(pawn.LabelShortCap),pawn,MessageTypeDefOf.NeutralEvent,false);
                }
                LordMaker.MakeNewLord(group.Key,new LordJob_RefusedGuest(),map,pawns);
            }
        }
    }
}
