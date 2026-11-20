using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    // Appended values only: saves store the enum by name.
    public enum StoryQuestStatus { Offered, Active, Completed, Failed, Declined, Disabled }

    /// Runtime record of one quest. Saved by defName so a removed StoryQuestDef never breaks a save.
    public sealed class StoryQuestRun : IExposable
    {
        public string quest;
        public StoryQuestStatus status;
        public int stage = -1;
        public string stageKey;          // stable key of the current stage (StoryStage.key or its index); survives reordered XML
        public int gen;                  // generation: bumped on every offer and stage entry; letters carry it to reject stale clicks
        public string outcome;
        public string chosenOutcome;     // set by the choice letter, consumed by StoryStage_Choice.Tick
        public bool pendingChoice;
        public bool needsLetter;         // a lost offer/choice letter must be re-sent once by the director
        public int stageUntilTick = -1;
        public int offeredTick = -1;
        public int cooldownUntilTick = -1;
        public int finishedTick = -1;
        public List<string> history = new List<string>(); // "stage:outcome" for the Chronicle log
        public Dictionary<string, string> vars = new Dictionary<string, string>(); // per-stage worker data (E2/E3)

        public StoryQuestDef Def { get { return DefDatabase<StoryQuestDef>.GetNamedSilentFail(quest); } }

        public void ExposeData()
        {
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref status, "status", StoryQuestStatus.Offered);
            Scribe_Values.Look(ref stage, "stage", -1);
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref gen, "gen", 0);
            Scribe_Values.Look(ref outcome, "outcome");
            Scribe_Values.Look(ref chosenOutcome, "chosenOutcome");
            Scribe_Values.Look(ref pendingChoice, "pendingChoice", false);
            Scribe_Values.Look(ref needsLetter, "needsLetter", false);
            Scribe_Values.Look(ref stageUntilTick, "stageUntilTick", -1);
            Scribe_Values.Look(ref offeredTick, "offeredTick", -1);
            Scribe_Values.Look(ref cooldownUntilTick, "cooldownUntilTick", -1);
            Scribe_Values.Look(ref finishedTick, "finishedTick", -1);
            Scribe_Collections.Look(ref history, "history", LookMode.Value);
            Scribe_Collections.Look(ref vars, "vars", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (history == null) history = new List<string>();
                if (vars == null) vars = new Dictionary<string, string>();
            }
        }
    }

    /// The whole story state: flags, quest runs and the director (pacing). Design: Documentation/STORY-ENGINE-DESIGN.md.
    public sealed class SparkStory : WorldComponent
    {
        public const int TickInterval = 250;
        public static int MaxActive = 2;                 // non-main quests at once (offered faction quests and non-main autoStart)
        public static int MinOfferGapTicks = 30000;      // half a day between offers

        private Dictionary<string, string> flags = new Dictionary<string, string>();
        private List<StoryQuestRun> runs = new List<StoryQuestRun>();
        public int lastOfferTick = -999999;
        public string lastMechanic = "";
        private bool reconciled;                         // transient: reconcile once after load/start
        private Dictionary<string, string> transientForcedOutcomes = new Dictionary<string, string>(); // never serialized to saves

        public SparkStory(World world) : base(world) { }

        public static SparkStory Instance { get { return Find.World == null ? null : Find.World.GetComponent<SparkStory>(); } }
        public IEnumerable<StoryQuestRun> Runs { get { return runs; } }
        public IEnumerable<KeyValuePair<string, string>> Flags { get { return flags; } }

        private static bool IsStoryTestMode
        {
            get { return Environment.GetCommandLineArgs().Contains("-tmm-story-test"); }
        }

        // ------------------------------------------------ flags
        public string GetFlag(string key) { string v; return key != null && flags.TryGetValue(key, out v) ? v : ""; }
        public int GetInt(string key) { int v; return int.TryParse(GetFlag(key), out v) ? v : 0; }
        public void SetFlag(string key, string value) { if (!key.NullOrEmpty()) flags[key] = value ?? ""; }

        // ------------------------------------------------ queries
        public StoryQuestRun RunFor(string quest) { return runs.FirstOrDefault(r => r.quest == quest); }
        public bool IsDone(string quest) { StoryQuestRun r = RunFor(quest); return r != null && (r.status == StoryQuestStatus.Completed || r.status == StoryQuestStatus.Failed); }
        public bool IsActive(string quest) { StoryQuestRun r = RunFor(quest); return r != null && r.status == StoryQuestStatus.Active; }

        public bool IsAvailable(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r != null && r.status != StoryQuestStatus.Declined) return false;
            if (r != null && Find.TickManager.TicksGame < r.cooldownUntilTick) return false;
            int act = Math.Max(1, GetInt("act"));
            if (d.act > act) return false;
            if (!d.faction.NullOrEmpty() && StoryUtil.FactionOf(d.faction) == null) return false;
            return StoryConditions.Holds(this, d.requires, d.anyOf);
        }

        public StoryContext Context(StoryQuestRun r) { return new StoryContext { state = this, run = r, def = r.Def }; }

        private int NonMainBusy()
        {
            return runs.Count(x => (x.status == StoryQuestStatus.Active || x.status == StoryQuestStatus.Offered) && x.Def != null && !x.Def.main);
        }

        // ------------------------------------------------ lifecycle
        public StoryQuestRun Offer(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r == null) { r = new StoryQuestRun { quest = d.defName }; runs.Add(r); }
            r.status = StoryQuestStatus.Offered;
            r.gen++;
            r.offeredTick = Find.TickManager.TicksGame;
            r.needsLetter = false;
            lastOfferTick = r.offeredTick;
            StoryChoiceLetter.Send(Context(r), -1, StoryChoiceLetter.KindOffer);
            return r;
        }

        /// Accept/Decline/Choose take the letter's generation and refuse stale or repeated clicks.
        public bool Accept(StoryQuestRun r, int letterGen)
        {
            if (r == null || r.status != StoryQuestStatus.Offered || r.gen != letterGen || r.Def == null) return false;
            r.status = StoryQuestStatus.Active;
            StartStage(r, 0);
            return true;
        }

        public bool Decline(StoryQuestRun r, int letterGen)
        {
            if (r == null || r.status != StoryQuestStatus.Offered || r.gen != letterGen) return false;
            StoryQuestDef d = r.Def;
            r.status = StoryQuestStatus.Declined;
            r.gen++;
            r.cooldownUntilTick = Find.TickManager.TicksGame + (int)((d == null ? 3f : d.offerCooldownDays) * GenDate.TicksPerDay);
            return true;
        }

        public bool Choose(StoryQuestRun r, int stage, int letterGen, string outcome)
        {
            if (r == null || r.status != StoryQuestStatus.Active || r.stage != stage || r.gen != letterGen || !r.pendingChoice) return false;
            StoryQuestDef d = r.Def;
            var st = d == null || stage < 0 || stage >= d.stages.Count ? null : d.stages[stage] as StoryStage_Choice;
            StoryChoice c = st == null ? null : st.ChoiceFor(outcome);
            if (c == null || !StoryConditions.Holds(this, c.requires, c.anyOf)) return false; // re-check requires at click time
            r.chosenOutcome = outcome;
            SafeTick(r); // resolve immediately so the player sees the consequence at once
            return true;
        }

        /// A letter of the current generation was dismissed unanswered: an offer counts as "later",
        /// a choice letter is re-sent once by the director (deduplicated by generation).
        public void Notify_LetterLost(StoryQuestRun r, string kind, int letterGen)
        {
            if (r == null || r.gen != letterGen) return;
            if (kind == StoryChoiceLetter.KindOffer) { Decline(r, letterGen); return; }
            if (r.status == StoryQuestStatus.Active && r.pendingChoice) r.needsLetter = true;
        }

        private void StartQuestDirect(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r == null) { r = new StoryQuestRun { quest = d.defName }; runs.Add(r); }
            r.status = StoryQuestStatus.Active;
            StartStage(r, 0);
        }

        private void StartStage(StoryQuestRun r, int index)
        {
            StoryQuestDef d = r.Def;
            if (d == null) { Disable(r, "def missing"); return; }
            if (index < 0 || index >= d.stages.Count) { Finish(r, StoryQuestStatus.Completed); return; }
            r.stage = index;
            r.stageKey = d.stages[index].Key(index);
            r.gen++;
            r.stageUntilTick = -1;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.chosenOutcome = null;
            r.vars.Clear();
            StoryStage st = d.stages[index];
            lastMechanic = st.Mechanic;
            st.Start(Context(r));
        }

        /// One stage tick with a single transition point: the outcome is recorded before any effect runs,
        /// each effect is isolated, so a failing effect can never make earlier effects run twice.
        private void TickRun(StoryQuestRun r)
        {
            StoryQuestDef d = r.Def;
            if (d == null || r.stage < 0 || r.stage >= d.stages.Count) { Disable(r, "stage missing"); return; }
            StoryStage st = d.stages[r.stage];
            StoryContext ctx = Context(r);

            string o = null;
            string forcedFromVars;
            if (r.vars.TryGetValue("forceOutcome", out forcedFromVars))
            {
                r.vars.Remove("forceOutcome");
                if (IsStoryTestMode && !forcedFromVars.NullOrEmpty())
                {
                    o = forcedFromVars;
                }
            }

            string transientForced;
            if (o == null && transientForcedOutcomes != null && transientForcedOutcomes.TryGetValue(r.quest, out transientForced))
            {
                transientForcedOutcomes.Remove(r.quest);
                if (IsStoryTestMode && !transientForced.NullOrEmpty())
                {
                    o = transientForced;
                }
            }

            if (o == null)
            {
                o = st.Tick(ctx);
            }

            if (o == null) return;
            int from = r.stage;
            r.outcome = o;
            r.history.Add(from + ":" + o);
            r.stage = -1; // transition boundary: nothing re-enters this stage
            StoryRelationRestorer.CheckOrphansIfPresent(); // ensure stage leases/protections are cleaned up even on forced outcomes
            foreach (StoryEffect e in st.EffectsFor(o).ToList()) ApplySafe(e, ctx);
            if (o == "failed") { Finish(r, StoryQuestStatus.Failed); return; }
            int next = st.NextFor(o);
            StartStage(r, next == -2 ? from + 1 : next);
        }

        private void SafeTick(StoryQuestRun r)
        {
            try { TickRun(r); }
            catch (Exception ex)
            {
                Log.ErrorOnce("[TMM] Story: quest " + r.quest + " failed in stage " + r.stage + ": " + ex, ("TMMStory" + r.quest).GetHashCode());
                Disable(r, "exception");
            }
        }

        private static void ApplySafe(StoryEffect e, StoryContext ctx)
        {
            if (e == null) return;
            try { e.Apply(ctx); }
            catch (Exception ex) { Log.Error("[TMM] Story: effect " + e.GetType().Name + " in " + ctx.run.quest + " failed: " + ex); }
        }

        private void Finish(StoryQuestRun r, StoryQuestStatus status)
        {
            StoryQuestDef d = r.Def;
            r.status = status;
            r.stage = -1;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.finishedTick = Find.TickManager.TicksGame;
            StoryRelationRestorer.CheckOrphansIfPresent();
            SetFlag("q." + r.quest, r.outcome ?? (status == StoryQuestStatus.Failed ? "failed" : "done"));
            if (d != null && status == StoryQuestStatus.Completed)
                foreach (StoryEffect e in d.onComplete) ApplySafe(e, Context(r));
        }

        /// A quest whose Def or stage disappeared (mod update): keep history and flags, give no rewards, free all limits.
        private void Disable(StoryQuestRun r, string why)
        {
            if (r.status == StoryQuestStatus.Disabled) return;
            r.status = StoryQuestStatus.Disabled;
            r.stage = -1;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.gen++;
            r.finishedTick = Find.TickManager.TicksGame;
            r.history.Add("disabled:" + why);
            StoryRelationRestorer.CheckOrphansIfPresent();
        }

        /// Once after load: drop runs of removed Defs, remap stages by stable key, resend exactly one letter
        /// for a valid pending offer/choice that has no live letter.
        private void Reconcile()
        {
            reconciled = true;
            var live = Find.LetterStack == null ? new List<StoryChoiceLetter>() : Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().ToList();
            foreach (StoryQuestRun r in runs.ToList())
            {
                if (r.status != StoryQuestStatus.Offered && r.status != StoryQuestStatus.Active) continue;
                StoryQuestDef d = r.Def;
                if (d == null) { Disable(r, "def missing"); continue; }
                if (r.status == StoryQuestStatus.Active)
                {
                    int byKey = r.stageKey.NullOrEmpty() ? r.stage : d.stages.FindIndex(s => s.Key(d.stages.IndexOf(s)) == r.stageKey);
                    if (byKey < 0) { Disable(r, "stage missing"); continue; }
                    r.stage = byKey;
                    if (r.pendingChoice && !live.Any(l => l.storyQuest == r.quest && l.gen == r.gen)) r.needsLetter = true;
                }
                else if (!live.Any(l => l.storyQuest == r.quest && l.gen == r.gen)) r.needsLetter = true;
            }
            StoryRelationRestorer.CheckOrphansIfPresent();
        }

        private void ResendLostLetters()
        {
            foreach (StoryQuestRun r in runs.Where(x => x.needsLetter).ToList())
            {
                r.needsLetter = false;
                if (r.Def == null) continue;
                if (r.status == StoryQuestStatus.Offered) StoryChoiceLetter.Send(Context(r), -1, StoryChoiceLetter.KindOffer);
                else if (r.status == StoryQuestStatus.Active && r.pendingChoice) StoryChoiceLetter.Send(Context(r), r.stage, StoryChoiceLetter.KindChoice);
            }
        }

        // ------------------------------------------------ test hook
        /// Test-only: finish the current stage of an Active quest with a declared outcome, through the normal transition.
        /// Refused unless the game runs with -tmm-story-test and the generation matches. Kept outside Scribe vars.
        public bool TestForceOutcome(StoryQuestRun r, int expectedGen, string outcome)
        {
            if (!IsStoryTestMode) return false;
            if (r == null || r.status != StoryQuestStatus.Active || r.gen != expectedGen || outcome.NullOrEmpty()) return false;
            if (transientForcedOutcomes == null) transientForcedOutcomes = new Dictionary<string, string>();
            transientForcedOutcomes[r.quest] = outcome;
            SafeTick(r);
            return true;
        }

        // ------------------------------------------------ director
        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % TickInterval != 0) return;
            DirectorTick();
        }

        public void DirectorTick()
        {
            if (!reconciled) Reconcile();
            if (GetFlag("act").NullOrEmpty()) SetFlag("act", "1");
            foreach (StoryQuestRun r in runs.Where(x => x.status == StoryQuestStatus.Active).ToList()) SafeTick(r);
            ResendLostLetters();
            if (Find.AnyPlayerHomeMap == null) return; // new quests need a home; active ones keep running

            foreach (StoryQuestDef d in DefDatabase<StoryQuestDef>.AllDefs.Where(x => x.autoStart).OrderBy(x => x.act).ThenByDescending(x => x.priority).ToList())
            {
                if (!IsAvailable(d)) continue;
                if (!d.main && NonMainBusy() >= MaxActive) continue;
                StartQuestDirect(d);
            }

            if (Find.TickManager.TicksGame - lastOfferTick < MinOfferGapTicks) return;
            bool mainOfferOpen = runs.Any(x => x.status == StoryQuestStatus.Offered && x.Def != null && x.Def.main);
            bool factionOfferOpen = runs.Any(x => x.status == StoryQuestStatus.Offered && x.Def != null && !x.Def.main);
            int busy = NonMainBusy();
            StoryQuestDef pick = DefDatabase<StoryQuestDef>.AllDefs
                .Where(x => !x.autoStart && IsAvailable(x) && (x.main ? !mainOfferOpen : !factionOfferOpen && busy < MaxActive))
                .OrderByDescending(x => x.FirstMechanic != lastMechanic)
                .ThenByDescending(x => x.main)
                .ThenByDescending(x => x.priority)
                .ThenBy(x => x.act)
                .ThenBy(x => x.defName)
                .FirstOrDefault();
            if (pick != null) Offer(pick);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref flags, "TMM_storyFlags", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref runs, "TMM_storyRuns", LookMode.Deep);
            Scribe_Values.Look(ref lastOfferTick, "lastOfferTick", -999999);
            Scribe_Values.Look(ref lastMechanic, "lastMechanic", "");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (flags == null) flags = new Dictionary<string, string>();
                if (runs == null) runs = new List<StoryQuestRun>();
                runs.RemoveAll(r => r == null || r.quest.NullOrEmpty());
                if (transientForcedOutcomes == null) transientForcedOutcomes = new Dictionary<string, string>();
                transientForcedOutcomes.Clear();
                reconciled = false;
            }
        }
    }
}
