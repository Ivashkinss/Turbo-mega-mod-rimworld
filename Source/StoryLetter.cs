using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Letter used by the story engine for quest offers (Accept / Later) and for Choice stages.
    /// Each letter carries the run's generation: stale, archived or repeated clicks are refused by SparkStory,
    /// and only the current letter of a run may react to being dismissed.
    public class StoryChoiceLetter : ChoiceLetter
    {
        public const string KindOffer = "offer";
        public const string KindChoice = "choice";

        public string storyQuest;
        public int stage = -1;
        public int gen;
        public string kind = KindOffer;
        private bool resolved;

        public static void Send(StoryContext ctx, int stage, string kind)
        {
            StoryQuestDef d = ctx.def;
            if (d == null) return;
            string label, text;
            if (kind == KindOffer) { label = d.letterLabel.NullOrEmpty() ? d.label : d.letterLabel; text = d.letterText; }
            else { StoryStage st = d.stages[stage]; label = st.label.NullOrEmpty() ? d.label : st.label; text = st.text; }
            LetterDef ld = DefDatabase<LetterDef>.GetNamed("TMM_StoryChoice");
            var let = (StoryChoiceLetter)LetterMaker.MakeLetter(StoryUtil.Format(label, ctx), StoryUtil.Format(text, ctx), ld);
            let.storyQuest = d.defName;
            let.stage = stage;
            let.gen = ctx.run.gen;
            let.kind = kind;
            Find.LetterStack.ReceiveLetter(let);
        }

        private bool InStack { get { return Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(this); } }

        /// Still the letter the run is waiting for?
        private bool Current(StoryQuestRun run)
        {
            if (resolved || run == null || run.gen != gen || !InStack) return false;
            return kind == KindOffer ? run.status == StoryQuestStatus.Offered
                                     : run.status == StoryQuestStatus.Active && run.stage == stage && run.pendingChoice;
        }

        /// Closes only the dialog: an archived or outdated letter must never touch the letter stack or the quest.
        private static DiaOption CloseOnly()
        {
            var close = new DiaOption("Close".Translate());
            close.resolveTree = true;
            return close;
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                SparkStory s = SparkStory.Instance;
                StoryQuestRun run = s == null ? null : s.RunFor(storyQuest);
                StoryQuestDef def = run == null ? null : run.Def;
                if (def == null || !Current(run)) { yield return CloseOnly(); yield break; }

                if (kind == KindOffer)
                {
                    var accept = new DiaOption("TMM.Story.Accept".Translate());
                    accept.action = delegate { if (Current(run)) { Resolve(); s.Accept(run, gen); } };
                    accept.resolveTree = true;
                    yield return accept;
                    var later = new DiaOption("TMM.Story.Later".Translate());
                    later.action = delegate { if (Current(run)) { Resolve(); s.Decline(run, gen); } };
                    later.resolveTree = true;
                    yield return later;
                    yield break;
                }

                var stageDef = stage >= 0 && stage < def.stages.Count ? def.stages[stage] as StoryStage_Choice : null;
                if (stageDef == null) { yield return CloseOnly(); yield break; }
                StoryContext ctx = s.Context(run);
                foreach (StoryChoice c in stageDef.choices)
                {
                    StoryChoice choice = c;
                    var opt = new DiaOption(StoryUtil.Format(choice.label, ctx));
                    opt.action = delegate
                    {
                        if (!Current(run)) return;
                        int letterGen = gen;
                        Resolve();
                        // The reply is sent only when the engine really accepted the transition.
                        if (s.Choose(run, stage, letterGen, choice.outcome) && !choice.resultText.NullOrEmpty())
                            Find.LetterStack.ReceiveLetter(def.LabelCap.ToString(), StoryUtil.Format(choice.resultText, ctx), LetterDefOf.NeutralEvent);
                    };
                    opt.resolveTree = true;
                    if (!StoryConditions.Holds(s, choice.requires, null)) opt.Disable("TMM.Story.ChoiceLocked".Translate());
                    yield return opt;
                }
            }
        }

        private void Resolve()
        {
            resolved = true; // before RemoveLetter: it calls Removed() synchronously
            if (InStack) Find.LetterStack.RemoveLetter(this);
        }

        /// Dismissed without an answer. Handled once and only for the current letter of the run.
        public override void Removed()
        {
            base.Removed();
            if (resolved) return;
            resolved = true;
            SparkStory s = SparkStory.Instance;
            StoryQuestRun run = s == null ? null : s.RunFor(storyQuest);
            if (run != null) s.Notify_LetterLost(run, kind, gen);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref storyQuest, "storyQuest");
            Scribe_Values.Look(ref stage, "stage", -1);
            Scribe_Values.Look(ref gen, "gen", 0);
            Scribe_Values.Look(ref kind, "kind", KindOffer);
            Scribe_Values.Look(ref resolved, "resolved", false);
        }
    }
}
