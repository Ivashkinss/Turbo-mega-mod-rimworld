using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Letter used by the story engine for quest offers (Accept / Later) and for Choice stages.
    /// Saved with the letter stack; resolving it calls back into SparkStory by quest defName.
    public class StoryChoiceLetter : ChoiceLetter
    {
        public const string KindOffer = "offer";
        public const string KindChoice = "choice";

        public string storyQuest;
        public int stage = -1;
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
            let.kind = kind;
            Find.LetterStack.ReceiveLetter(let);
        }

        private bool InStack { get { return Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(this); } }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                SparkStory s = SparkStory.Instance;
                StoryQuestRun run = s == null ? null : s.RunFor(storyQuest);
                StoryQuestDef def = run == null ? null : run.Def;
                if (!InStack || def == null) { yield return Option_Close; yield break; }

                if (kind == KindOffer)
                {
                    var accept = new DiaOption("TMM.Story.Accept".Translate());
                    accept.action = delegate { Resolve(); s.Accept(run); };
                    accept.resolveTree = true;
                    yield return accept;
                    var later = new DiaOption("TMM.Story.Later".Translate());
                    later.action = delegate { Resolve(); s.Decline(run); };
                    later.resolveTree = true;
                    yield return later;
                    yield break;
                }

                var stageDef = stage >= 0 && stage < def.stages.Count ? def.stages[stage] as StoryStage_Choice : null;
                if (stageDef == null) { yield return Option_Close; yield break; }
                StoryContext ctx = s.Context(run);
                foreach (StoryChoice c in stageDef.choices)
                {
                    StoryChoice choice = c;
                    var opt = new DiaOption(StoryUtil.Format(choice.label, ctx));
                    opt.action = delegate
                    {
                        Resolve();
                        s.Choose(run, stage, choice.outcome);
                        if (!choice.resultText.NullOrEmpty())
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
            resolved = true;
            if (InStack) Find.LetterStack.RemoveLetter(this);
        }

        /// A letter dismissed without an answer: an offer counts as "later", a choice is re-sent by the state.
        public override void Removed()
        {
            base.Removed();
            if (resolved) return;
            SparkStory s = SparkStory.Instance;
            StoryQuestRun run = s == null ? null : s.RunFor(storyQuest);
            if (run == null) return;
            if (kind == KindOffer) s.Decline(run);
            else s.Notify_ChoiceLetterLost(run, stage);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref storyQuest, "storyQuest");
            Scribe_Values.Look(ref stage, "stage", -1);
            Scribe_Values.Look(ref kind, "kind", KindOffer);
            Scribe_Values.Look(ref resolved, "resolved", false);
        }
    }
}
