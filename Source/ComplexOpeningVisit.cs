using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    // One opening invitation, not a replacement for Hospitality's visit queue.
    public sealed class ComplexOpeningVisit : MapComponent
    {
        public bool completed;
        public int scheduledTick=-1;
        public Faction invitedFaction;
        private int nextCheck;
        private static readonly Type NativeMap=AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
        private static readonly Type VisitLord=AccessTools.TypeByName("Hospitality.LordJob_VisitColony");
        private static readonly FieldInfo QueueField=NativeMap==null?null:AccessTools.Field(NativeMap,"incidentQueue");
        private static readonly FieldInfo WelcomeField=NativeMap==null?null:AccessTools.Field(NativeMap,"guestsAreWelcome");
        private static readonly Type SettingsType=AccessTools.TypeByName("Hospitality.ModSettings_Hospitality");
        private static readonly Type UtilityType=AccessTools.TypeByName("Hospitality.Utilities.GenericUtility");
        private static readonly Type SpaceType=AccessTools.TypeByName("Hospitality.Utilities.SpaceMapUtility");
        private static readonly FieldInfo DisabledField=SettingsType==null?null:AccessTools.Field(SettingsType,"disableGuests");
        private static readonly MethodInfo Plan=UtilityType==null?null:AccessTools.Method(UtilityType,"PlanNewVisit");
        private static readonly MethodInfo IsSpace=SpaceType==null?null:AccessTools.Method(SpaceType,"IsSpaceMap");
        public ComplexOpeningVisit(Map map):base(map){}
        public override void ExposeData(){base.ExposeData();Scribe_Values.Look(ref completed,"TMM_openingVisitCompleted");Scribe_Values.Look(ref scheduledTick,"TMM_openingVisitTick",-1);Scribe_References.Look(ref invitedFaction,"TMM_openingVisitFaction");}
        public override void MapComponentTick(){if(completed||Find.TickManager.TicksGame<nextCheck)return;nextCheck=Find.TickManager.TicksGame+2500;TryPlanOpeningVisit();}
        private static bool Eligible(Faction faction){return faction!=null&&!faction.IsPlayer&&!faction.defeated&&!faction.HostileTo(Faction.OfPlayer)&&faction.def.pawnGroupMakers!=null&&faction.def.pawnGroupMakers.Any(g=>g.kindDef==PawnGroupKindDefOf.Peaceful);}
        public bool TryPlanOpeningVisit()
        {
            if(completed||!map.IsPlayerHome||NativeMap==null||QueueField==null||WelcomeField==null||Plan==null)return false;
            var desk=map.GetComponent<ComplexMap>().desk;
            if(desk==null||!desk.Active||map.mapPawns.ColonistCount==0||IsSpace!=null&&(bool)IsSpace.Invoke(null,new object[]{map}))return false;
            // An established hotel does not need an extra opening group after migration.
            var book=GuestDossiers.Book;
            if(VisitLord!=null&&map.lordManager.lords.Any(l=>VisitLord.IsInstanceOfType(l.LordJob)&&l.ownedPawns.Any(p=>p.Spawned&&!p.Dead))||
                book!=null&&book.records.Any(r=>r!=null&&r.visits!=null&&r.visits.Any(v=>v!=null&&v.mapId==map.uniqueID&&!v.medical))){completed=true;return false;}
            var native=map.components.FirstOrDefault(c=>NativeMap.IsInstanceOfType(c));
            if(native==null||!(bool)WelcomeField.GetValue(native)||DisabledField!=null&&(bool)DisabledField.GetValue(null))return false;
            bool freeBed=map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Any(b=>ComplexLayout.IsGuest(b)&&!b.Medical&&!b.ForPrisoners&&
                desk.area[b.Position]&&!ComplexLayout.StaffBed(desk,b)&&!b.OwnersForReading.Any()&&!b.CurOccupants.Any());
            if(!freeBed)return false;
            var queue=QueueField.GetValue(native) as IncidentQueue;if(queue==null)return false;
            var visitor=DefDatabase<IncidentDef>.GetNamedSilentFail("VisitorGroup");if(visitor==null)return false;
            var factions=Find.FactionManager.AllFactionsVisible.Where(Eligible).ToList();if(factions.Count==0)return false;
            int now=Find.TickManager.TicksGame;QueuedIncident earliest=null;
            foreach(QueuedIncident q in queue){
                var firing=q.FiringIncident;var parms=firing==null?null:firing.parms;
                if(firing==null||firing.def!=visitor||parms==null||parms.target!=map||!factions.Contains(parms.faction))continue;
                if(earliest==null||q.FireTick<earliest.FireTick)earliest=q;
            }
            if(earliest!=null&&earliest.FireTick<=now+120000){completed=true;return false;}
            var faction=earliest==null?factions.OrderBy(f=>f.loadID).First():earliest.FiringIncident.parms.faction;
            float days=Rand.Range(1f,2f);int count=queue.Count;
            Plan.Invoke(null,new object[]{map,days,faction});
            if(queue.Count<=count)return false;
            QueuedIncident planned=null;
            foreach(QueuedIncident q in queue)if(q.FiringIncident.def==visitor&&q.FiringIncident.parms.faction==faction&&q.FireTick>=now+60000&&q.FireTick<=now+120000&&
                (planned==null||q.FireTick<planned.FireTick))planned=q;
            if(planned==null)return false;
            completed=true;scheduledTick=planned.FireTick;invitedFaction=faction;
            Messages.Message("TMM.Opening.VisitPlanned".Translate(faction.Name,days.ToString("0.#")),desk,MessageTypeDefOf.NeutralEvent,false);
            return true;
        }
    }
}
