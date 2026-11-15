using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    public sealed class ComplexRecruit : IExposable
    {
        public Pawn pawn;
        public Building_ComplexDesk employer;
        public int firstAssignedTick = -1;
        internal int lastPenalty = int.MinValue, lastBonus = int.MinValue;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref employer, "employer");
            Scribe_Values.Look(ref firstAssignedTick, "firstAssignedTick", -1);
        }
    }

    public sealed class ComplexRecruitment : GameComponent
    {
        public List<ComplexRecruit> recruits = new List<ComplexRecruit>();
        private int nextCheck;
        public ComplexRecruitment(Game game) { }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref recruits, "TMM_complexRecruits", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                if (recruits == null) recruits = new List<ComplexRecruit>();
                recruits.RemoveAll(r => r == null || r.pawn == null);
            }
        }
        public ComplexRecruit Entry(Pawn pawn) { return recruits.FirstOrDefault(r => r.pawn == pawn); }
        public static bool Eligible(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.Faction == Faction.OfPlayer &&
                pawn.RaceProps.Humanlike && !pawn.IsPrisoner && !pawn.IsSlave;
        }
        public static bool Assignment(Building_ComplexDesk desk, Pawn pawn)
        {
            if (desk == null || !desk.Active || pawn == null || pawn.workSettings == null) return false;
            var employee = desk.Employee(pawn);
            if (employee == null || employee.mode == ComplexWorkMode.ColonyOnly) return false;
            var work = Building_ComplexDesk.Work(employee.role);
            return work != null && !pawn.WorkTypeIsDisabled(work) && pawn.workSettings.GetPriority(work) > 0;
        }
        public ComplexRecruit Register(Pawn pawn, Building_ComplexDesk employer)
        {
            if (!Eligible(pawn)) return null;
            var entry = Entry(pawn);
            if (entry == null) { entry = new ComplexRecruit { pawn = pawn, employer = employer }; recruits.Add(entry); }
            Observe(entry); return entry;
        }
        private Building_ComplexDesk Employer(ComplexRecruit entry)
        {
            // Employment survives a caravan or a visit to another map. A
            // saved home assignment still counts; the worker need not be
            // physically serving a guest at this instant.
            if (Assignment(entry.employer, entry.pawn)) return entry.employer;
            return Find.Maps.Select(m => m.GetComponent<ComplexMap>().desk)
                .Where(d => Assignment(d, entry.pawn))
                .OrderByDescending(d => d.Map == entry.pawn.MapHeld).FirstOrDefault();
        }
        public RecruitWorkState Observe(ComplexRecruit entry)
        {
            var employer = Employer(entry);
            var state = RecruitmentProgress.Evaluate(true, Eligible(entry.pawn), employer != null,
                Find.TickManager.TicksGame, entry.firstAssignedTick);
            entry.firstAssignedTick = state.firstAssignedTick;
            if (employer != null) entry.employer = employer;
            if (state.waitingPenalty != entry.lastPenalty || state.adaptationBonus != entry.lastBonus) {
                entry.lastPenalty = state.waitingPenalty; entry.lastBonus = state.adaptationBonus;
                if (entry.pawn.needs != null && entry.pawn.needs.mood != null)
                    entry.pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            }
            return state;
        }
        public static void Notify(Pawn pawn)
        {
            if (Current.Game == null) return;
            var book = Current.Game.GetComponent<ComplexRecruitment>();
            var entry = book.Entry(pawn);
            if (entry != null) book.Observe(entry);
        }
        public static string Status(Pawn pawn)
        {
            if (Current.Game == null || !Eligible(pawn)) return null;
            var book = Current.Game.GetComponent<ComplexRecruitment>(); var entry = book.Entry(pawn);
            if (entry == null) return null;
            var state = book.Observe(entry);
            return (state.waitingPenalty < 0 ? "TMM.Recruit.StatusWaiting" :
                state.adaptationBonus > 0 ? "TMM.Recruit.StatusWorking" : "TMM.Recruit.StatusAdapted").Translate();
        }
        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame < nextCheck) return;
            nextCheck = Find.TickManager.TicksGame + 250;
            foreach (var entry in recruits) if (entry.pawn != null && !entry.pawn.Destroyed) Observe(entry);
        }
    }

    public sealed class ThoughtWorker_ComplexRecruitWaiting : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            var book = Current.Game.GetComponent<ComplexRecruitment>(); var entry = book.Entry(pawn);
            return entry != null && book.Observe(entry).waitingPenalty < 0 ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }
    public sealed class ThoughtWorker_ComplexRecruitAdaptation : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            var book = Current.Game.GetComponent<ComplexRecruitment>(); var entry = book.Entry(pawn);
            int bonus = entry == null ? 0 : book.Observe(entry).adaptationBonus;
            return bonus > 0 ? ThoughtState.ActiveAtStage(bonus == 15 ? 0 : bonus == 10 ? 1 : 2) : ThoughtState.Inactive;
        }
    }
}
