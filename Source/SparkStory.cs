using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    public enum StoryQuestStatus { Offered, Active, Completed, Failed, Declined }

    /// Runtime record of one quest. Saved by defName so a removed StoryQuestDef never breaks a save.
    public sealed class StoryQuestRun : IExposable
    {
        public string quest;
        public StoryQuestStatus status;
        public int stage = -1;
        public string outcome;
        public string chosenOutcome;     // set by the choice letter, consumed by StoryStage_Choice.Tick
        public bool pendingChoice;
        public int stageUntilTick = -1;
        public int offeredTick = -1;
        public int cooldownUntilTick = -1;
        public int finishedTick = -1;
        public List<string> history = new List<string>(); // "stage:outcome" for the Chronicle log

        public StoryQuestDef Def { get { return DefDatabase<StoryQuestDef>.GetNamedSilentFail(quest); } }

        public void ExposeData()
        {
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref status, "status", StoryQuestStatus.Offered);
            Scribe_Values.Look(ref stage, "stage", -1);
            Scribe_Values.Look(ref outcome, "outcome");
            Scribe_Values.Look(ref chosenOutcome, "chosenOutcome");
            Scribe_Values.Look(ref pendingChoice, "pendingChoice", false);
            Scribe_Values.Look(ref stageUntilTick, "stageUntilTick", -1);
            Scribe_Values.Look(ref offeredTick, "offeredTick", -1);
            Scribe_Values.Look(ref cooldownUntilTick, "cooldownUntilTick", -1);
            Scribe_Values.Look(ref finishedTick, "finishedTick", -1);
            Scribe_Collections.Look(ref history, "history", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && history == null) history = new List<string>();
        }
    }

    /// The whole story state: flags, quest runs and the director (pacing). Design: Documentation/STORY-ENGINE-DESIGN.md.
    public sealed class SparkStory : WorldComponent
    {
        public const int TickInterval = 250;
        public static int MaxActive = 2;                 // faction quests at once; main quests are extra
        public static int MinOfferGapTicks = 30000;      // half a day between offers

        private Dictionary<string, string> flags = new Dictionary<string, string>();
        private List<StoryQuestRun> runs = new List<StoryQuestRun>();
        public int lastOfferTick = -999999;
        public string lastMechanic = "";

        public SparkStory(World world) : base(world) { }

        public static SparkStory Instance { get { return Find.World == null ? null : Find.World.GetComponent<SparkStory>(); } }
        public IEnumerable<StoryQuestRun> Runs { get { return runs; } }
        public IEnumerable<KeyValuePair<string, string>> Flags { get { return flags; } }

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
            if (GetInt("act") < 1 && d.act > 1) return false;
            if (GetInt("act") > 0 && d.act > GetInt("act")) return false;
            if (!d.faction.NullOrEmpty() && StoryUtil.FactionOf(d.faction) == null) return false;
            return StoryConditions.Holds(this, d.requires, d.anyOf);
        }

        public StoryContext Context(StoryQuestRun r) { return new StoryContext { state = this, run = r, def = r.Def }; }

        // ------------------------------------------------ lifecycle
        public StoryQuestRun Offer(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r == null) { r = new StoryQuestRun { quest = d.defName }; runs.Add(r); }
            r.status = StoryQuestStatus.Offered;
            r.offeredTick = Find.TickManager.TicksGame;
            lastOfferTick = r.offeredTick;
            StoryChoiceLetter.Send(Context(r), -1, StoryChoiceLetter.KindOffer);
            return r;
        }

        public void Accept(StoryQuestRun r)
        {
            if (r == null || r.status != StoryQuestStatus.Offered) return;
            r.status = StoryQuestStatus.Active;
            StartStage(r, 0);
        }

        public void Decline(StoryQuestRun r)
        {
            if (r == null || r.status != StoryQuestStatus.Offered) return;
            StoryQuestDef d = r.Def;
            r.status = StoryQuestStatus.Declined;
            r.cooldownUntilTick = Find.TickManager.TicksGame + (int)((d == null ? 3f : d.offerCooldownDays) * GenDate.TicksPerDay);
        }

        /// Called by the choice letter.
        public void Choose(StoryQuestRun r, int stage, string outcome)
        {
            if (r == null || r.status != StoryQuestStatus.Active || r.stage != stage || !r.pendingChoice) return;
            r.chosenOutcome = outcome;
            TickRun(r); // resolve immediately so the player sees the consequence at once
        }

        /// The choice letter was dismissed unanswered: re-send it so the quest never gets stuck.
        public void Notify_ChoiceLetterLost(StoryQuestRun r, int stage)
        {
            if (r == null || r.status != StoryQuestStatus.Active || r.stage != stage || !r.pendingChoice) return;
            StoryChoiceLetter.Send(Context(r), stage, StoryChoiceLetter.KindChoice);
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
            if (d == null) return;
            if (index < 0 || index >= d.stages.Count) { Finish(r, StoryQuestStatus.Completed); return; }
            r.stage = index;
            r.stageUntilTick = -1;
            r.pendingChoice = false;
            r.chosenOutcome = null;
            StoryStage st = d.stages[index];
            lastMechanic = st.Mechanic;
            st.Start(Context(r));
        }

        private void TickRun(StoryQuestRun r)
        {
            StoryQuestDef d = r.Def;
            if (d == null || r.stage < 0 || r.stage >= d.stages.Count) return;
            StoryStage st = d.stages[r.stage];
            StoryContext ctx = Context(r);
            string o = st.Tick(ctx);
            if (o == null) return;
            r.outcome = o;
            r.history.Add(r.stage + ":" + o);
            foreach (StoryEffect e in st.EffectsFor(o)) if (e != null) e.Apply(ctx);
            if (o == "failed") { Finish(r, StoryQuestStatus.Failed); return; }
            int next = st.NextFor(o);
            StartStage(r, next == -2 ? r.stage + 1 : next);
        }

        private void Finish(StoryQuestRun r, StoryQuestStatus status)
        {
            StoryQuestDef d = r.Def;
            r.status = status;
            r.finishedTick = Find.TickManager.TicksGame;
            SetFlag("q." + r.quest, r.outcome ?? (status == StoryQuestStatus.Failed ? "failed" : "done"));
            if (d != null && status == StoryQuestStatus.Completed)
                foreach (StoryEffect e in d.onComplete) if (e != null) e.Apply(Context(r));
        }

        // ------------------------------------------------ director
        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % TickInterval != 0) return;
            DirectorTick();
        }

        public void DirectorTick()
        {
            if (Find.AnyPlayerHomeMap == null) return;
            if (GetFlag("act").NullOrEmpty()) SetFlag("act", "1");
            foreach (StoryQuestRun r in runs.Where(x => x.status == StoryQuestStatus.Active).ToList()) TickRun(r);

            foreach (StoryQuestDef d in DefDatabase<StoryQuestDef>.AllDefs.Where(x => x.autoStart).OrderBy(x => x.act).ThenByDescending(x => x.priority).ToList())
                if (IsAvailable(d)) StartQuestDirect(d);

            if (runs.Any(x => x.status == StoryQuestStatus.Offered)) return; // one open offer at a time
            if (Find.TickManager.TicksGame - lastOfferTick < MinOfferGapTicks) return;
            int activeFaction = runs.Count(x => x.status == StoryQuestStatus.Active && x.Def != null && !x.Def.main);
            StoryQuestDef pick = DefDatabase<StoryQuestDef>.AllDefs
                .Where(x => !x.autoStart && IsAvailable(x) && (x.main || activeFaction < MaxActive))
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
            }
        }
    }
}
