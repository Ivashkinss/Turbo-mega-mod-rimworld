using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace TurboMegaMod
{
    public static class RecruitOffers
    {
        public const string DefName = "TMM_ComplexRecruit";
        public const int OfferDays = 3;
        public static Building_ComplexDesk Desk(Map map)
        {
            var desk = map == null ? null : map.GetComponent<ComplexMap>().desk;
            return desk != null && desk.Active ? desk : null;
        }
        public static bool CanDoAnyRole(Pawn pawn)
        {
            return pawn != null && Enum.GetValues(typeof(ComplexRole)).Cast<ComplexRole>()
                .Where(r => r != ComplexRole.None).Any(r => !pawn.WorkTypeIsDisabled(Building_ComplexDesk.Work(r)));
        }
        public static bool CanOffer(Map map)
        {
            if (map == null || !map.IsPlayerHome || Desk(map) == null) return false;
            var space = AccessTools.TypeByName("Hospitality.Utilities.SpaceMapUtility");
            if (space != null && (bool)AccessTools.Method(space, "IsSpaceMap").Invoke(null, new object[] { map })) return false;
            return !Find.QuestManager.QuestsListForReading.Any(q => q.root != null && q.root.defName == DefName &&
                (q.State == QuestState.NotYetAccepted || q.State == QuestState.Ongoing) &&
                q.PartsListForReading.OfType<QuestPart_ComplexRecruitArrival>().Any(p => p.mapParent == map.Parent));
        }
    }

    public sealed class QuestNode_Root_ComplexRecruit : QuestNode
    {
        protected override bool TestRunInt(Slate slate) { return RecruitOffers.CanOffer(slate.Get<Map>("map")); }
        protected override void RunInt()
        {
            var map = QuestGen.slate.Get<Map>("map"); var desk = RecruitOffers.Desk(map);
            if (desk == null) throw new InvalidOperationException("TMM.Recruit.NoComplex".Translate());
            var faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f =>
                !f.IsPlayer && !f.def.hidden && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
            var request = new PawnGenerationRequest(PawnKindDefOf.Colonist, faction,
                forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: false, allowAddictions: false, allowPregnant: false,
                fixedBiologicalAge: Rand.Range(20f, 55f), validatorPreGear: RecruitOffers.CanDoAnyRole);
            // The native quest helper registers the unspawned candidate in
            // WorldPawns. PawnsArrive saves REFERENCES (LookMode=3), not a
            // second deep copy. It only joins/spawns after player acceptance.
            var candidate = QuestGen_Pawns.GeneratePawn(QuestGen.quest, request, true);
            QuestGen.slate.Set("tmmRecruit", candidate);
            QuestGen.quest.AddPart(new QuestPart_ComplexRecruitRequirement { employer = desk, candidate = candidate });
            QuestGen.quest.AddPart(new QuestPart_ComplexRecruitArrival {
                employer = desk, mapParent = map.Parent, pawns = new List<Pawn> { candidate },
                inSignal = QuestGen.quest.InitiateSignal, joinPlayer = true,
                arrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn, spawnNear = IntVec3.Invalid,
                sendStandardLetter = false
            });
            QuestGen.AddQuestNameRules(new List<Rule> { new Rule_String("questName", "TMM.Recruit.QuestName".Translate()) });
            QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("questDescription",
                "TMM.Recruit.QuestDescription".Translate(candidate.LabelShortCap, RecruitOffers.OfferDays)) });
        }
    }

    public sealed class QuestPart_ComplexRecruitRequirement : QuestPart_RequirementsToAccept
    {
        public Building_ComplexDesk employer;
        public Pawn candidate;
        public override AcceptanceReport CanAccept()
        {
            if (employer == null || !employer.Active) return "TMM.Recruit.NoComplex".Translate().ToString();
            var arrival = quest == null ? null : quest.PartsListForReading.OfType<QuestPart_ComplexRecruitArrival>()
                .FirstOrDefault(p => p.employer == employer);
            if (arrival == null || arrival.mapParent == null || !arrival.mapParent.HasMap || arrival.mapParent.Map != employer.Map)
                return "TMM.Recruit.CandidateUnavailable".Translate().ToString();
            if (candidate == null || candidate.Dead || candidate.Destroyed || candidate.Spawned || candidate.Faction == Faction.OfPlayer)
                return "TMM.Recruit.CandidateUnavailable".Translate().ToString();
            return RecruitOffers.CanDoAnyRole(candidate) ? (AcceptanceReport)true : "TMM.Recruit.NoSuitableWork".Translate().ToString();
        }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_References.Look(ref employer, "employer"); Scribe_References.Look(ref candidate, "candidate");
        }
        public override void Notify_PawnDiscarded(Pawn pawn)
        {
            base.Notify_PawnDiscarded(pawn); if (candidate == pawn) candidate = null;
        }
        public override void ReplacePawnReferences(Pawn replaceWhat, Pawn replaceWith)
        {
            base.ReplacePawnReferences(replaceWhat, replaceWith);
            if (candidate == replaceWhat) candidate = replaceWith;
        }
    }

    public sealed class QuestPart_ComplexRecruitArrival : QuestPart_PawnsArrive
    {
        public Building_ComplexDesk employer;
        public bool completed;
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_References.Look(ref employer, "employer"); Scribe_Values.Look(ref completed, "TMM_recruitArrived");
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignal || completed) return;
            var candidate = pawns.FirstOrDefault();
            // Recheck here as well as in the acceptance UI: a stale/debug
            // accept signal must not create a colonist at a vanished complex.
            if (employer == null || !employer.Active || mapParent == null || !mapParent.HasMap ||
                employer.Map != mapParent.Map || candidate == null || candidate.Dead || candidate.Destroyed ||
                candidate.Spawned || candidate.Faction == Faction.OfPlayer || !RecruitOffers.CanDoAnyRole(candidate)) {
                completed = true; quest.End(QuestEndOutcome.Unknown, false); return;
            }
            var originalFaction = candidate.Faction;
            base.Notify_QuestSignalReceived(signal);
            completed = true;
            if (candidate.Spawned && candidate.Map == employer.Map && ComplexRecruitment.Eligible(candidate)) {
                Current.Game.GetComponent<ComplexRecruitment>().Register(candidate, employer);
                Messages.Message("TMM.Recruit.Arrived".Translate(candidate.LabelShortCap), candidate, MessageTypeDefOf.PositiveEvent, false);
                quest.End(QuestEndOutcome.Success, false);
            } else {
                if (!candidate.Spawned && candidate.Faction == Faction.OfPlayer) candidate.SetFaction(originalFaction);
                Messages.Message("TMM.Recruit.CandidateUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                quest.End(QuestEndOutcome.Unknown, false);
            }
        }
    }

    public sealed class IncidentWorker_ComplexRecruit : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms) { return base.CanFireNowSub(parms) && RecruitOffers.CanOffer(parms.target as Map); }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = parms.target as Map;
            if (!RecruitOffers.CanOffer(map)) return false;
            var slate = new Slate(); slate.Set("map", map); slate.Set("points", parms.points);
            var quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed(RecruitOffers.DefName), slate);
            if (quest != null) QuestUtility.SendLetterQuestAvailable(quest, null);
            return quest != null;
        }
    }
}
