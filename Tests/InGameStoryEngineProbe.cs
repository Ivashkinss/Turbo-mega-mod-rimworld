using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;

// Story engine E1 (+ Codex review fixes F1–F7). Fresh: -tmm-story-probe. Reload: -tmm-story-reload (save TMM-Story,
// taken while Castiel's choice letter is pending). Real actions only: real letters answered through their DiaOptions,
// real ticks, repeated and stale clicks, a quest whose Def is missing, heroes held on a map versus free world heroes.
[StaticConstructorOnStartup]
public static class StoryProbeLaunch
{
    static StoryProbeLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-story-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Story"));
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps == null) return;
            Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
            if (home != null && home.GetComponent<StoryEngineProbe>() == null) home.components.Add(new StoryEngineProbe(home));
        });
    }
}

public sealed class StoryEngineProbe : MapComponent
{
    private sealed class Check { public string name, expected, actual; public bool passed; }
    private readonly List<Check> checks = new List<Check>();
    private readonly List<string> facts = new List<string>();
    private bool done, uiChecked;
    private string uiError;
    private int stage, frames, waitStart, heavenBefore;

    private const string Signal = "TMM_Story_A1_Signal";
    private const string Castiel = "TMM_Story_A1_Castiel";

    public StoryEngineProbe(Map map) : base(map) { }

    private void C(string name, bool ok, object expected, object actual)
    {
        checks.Add(new Check { name = name, passed = ok, expected = Convert.ToString(expected), actual = Convert.ToString(actual) });
    }

    private static StoryChoiceLetter LetterFor(string quest, string kind)
    {
        return Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().FirstOrDefault(l => l.storyQuest == quest && l.kind == kind);
    }

    private static Action ActionOf(StoryChoiceLetter letter, int index) { return letter.Choices.ToList()[index].action; }
    private static Faction Heaven { get { return StoryUtil.FactionOf("TMM_HeavenFaction"); } }
    private static int HeavenGoodwill { get { return Heaven == null ? 0 : Heaven.GoodwillWith(Faction.OfPlayer); } }

    public override void MapComponentUpdate()
    {
        string[] args = Environment.GetCommandLineArgs();
        bool reload = args.Contains("-tmm-story-reload");
        if (done || !map.IsPlayerHome || (!reload && !args.Contains("-tmm-story-probe"))) return;
        if (Find.TickManager.CurTimeSpeed != TimeSpeed.Ultrafast) Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
        foreach (Window w in Find.WindowStack.Windows.ToList()) if (w.forcePause) w.Close(false);
        if (++frames < 30) return;
        SparkStory.MinOfferGapTicks = 0; // test pacing: offers may follow each other at once
        try { if (reload) Reload(); else Fresh(); }
        catch (Exception ex) { Finish(ex, reload); }
    }

    private void Fresh()
    {
        SparkStory s = SparkStory.Instance;
        int now = Find.TickManager.TicksGame;
        if (stage == 0)
        {
            C("StateExists", s != null, true, s != null);
            s.DirectorTick();
            StoryQuestRun run = s.RunFor(Signal);
            C("OpeningAutoStarted", run != null && run.status == StoryQuestStatus.Active && run.stage == 0, "Active/0", run == null ? "none" : run.status + "/" + run.stage);
            C("ActIsOne", s.GetInt("act") == 1, 1, s.GetInt("act"));
            StoryChoiceLetter letter = LetterFor(Signal, StoryChoiceLetter.KindChoice);
            C("ChoiceLetterInStack", letter != null && letter.gen == run.gen, "letter of current gen", letter == null ? "none" : letter.gen + "/" + run.gen);
            C("ColonyNameSubstituted", letter != null && !letter.Text.ToString().Contains("{COLONY}"), "no placeholder", "checked");
            C("TwoChoices", letter != null && letter.Choices.Count() == 2, 2, letter == null ? 0 : letter.Choices.Count());
            Action open = ActionOf(letter, 0);
            open(); // force the hatch open
            C("ChoiceFlagSet", s.GetFlag("story.anchorOpened") == "true", "true", s.GetFlag("story.anchorOpened"));
            int genAfter = run.gen;
            open(); // the same action again (double click / reopened window)
            C("RepeatClickRefused", run.stage == 1 && run.gen == genAfter && run.history.Count == 1, "stage 1, one history entry", run.stage + "/" + run.history.Count);
            C("ChoiceLetterRemoved", LetterFor(Signal, StoryChoiceLetter.KindChoice) == null, "removed", "checked");
            C("AdvancedToWait", run.stage == 1 && run.stageUntilTick > now && run.stageKey == "#1", "stage #1 with deadline", run.stageKey + "/" + run.stageUntilTick);
            C("StaleLetterOnlyCloses", letter.Choices.Count() == 1 && letter.Choices.First().action == null, "one close option", letter.Choices.Count());
            stage = 1; waitStart = now;
            return;
        }
        if (stage == 1)
        {
            if (!s.IsDone(Signal))
            {
                if (now - waitStart > 90000) throw new Exception("wait stage did not finish within 1.5 days");
                return;
            }
            C("OpeningCompleted", s.GetFlag("q." + Signal) == "done", "done", s.GetFlag("q." + Signal));
            C("OnCompleteFlag", s.GetFlag("story.anchorAwakened") == "true", "true", s.GetFlag("story.anchorAwakened"));
            s.DirectorTick();
            StoryChoiceLetter offer = LetterFor(Castiel, StoryChoiceLetter.KindOffer);
            C("CastielOffered", offer != null, "offer letter", offer == null ? "none" : "ok");
            C("GiverNameInText", offer != null && offer.Text.ToString().Contains("Castiel") && !offer.Text.ToString().Contains("{GIVER}"), "Castiel", "checked");
            ActionOf(offer, 1)(); // later
            StoryQuestRun run = s.RunFor(Castiel);
            C("DeclinedWithCooldown", run.status == StoryQuestStatus.Declined && run.cooldownUntilTick > now, "Declined", run.status + "/" + (run.cooldownUntilTick - now));
            s.DirectorTick();
            C("NotReofferedDuringCooldown", LetterFor(Castiel, StoryChoiceLetter.KindOffer) == null, "no offer", "checked");
            run.cooldownUntilTick = now; // the cooldown has passed
            s.DirectorTick();
            StoryChoiceLetter offer2 = LetterFor(Castiel, StoryChoiceLetter.KindOffer);
            C("ReofferedAfterCooldown", offer2 != null && run.status == StoryQuestStatus.Offered && offer2.gen == run.gen, "Offered", run.status);
            ActionOf(offer, 0); // the old, declined offer letter must not react
            C("OldOfferLetterInert", offer.Choices.Count() == 1 && run.status == StoryQuestStatus.Offered, "close only", offer.Choices.Count());
            Action accept = ActionOf(offer2, 0);
            accept();
            int g = run.gen;
            accept(); // repeated accept
            C("AcceptedOnce", run.status == StoryQuestStatus.Active && run.stage == 0 && run.gen == g, "Active/0, same gen", run.status + "/" + run.stage);
            StoryChoiceLetter choice = LetterFor(Castiel, StoryChoiceLetter.KindChoice);
            C("CastielChoicePending", choice != null && run.pendingChoice && choice.gen == run.gen, "pending letter", choice == null ? "none" : "ok");

            DisabledDefCheck(s);
            HeroJoinsChecks(s, run);

            heavenBefore = HeavenGoodwill;
            facts.Add("heavenBefore=" + heavenBefore);
            CleanDanglingRelations(); // fixture only: removes world-gen relations to discarded pawns that are unrelated to the engine
            facts.Add("fixtureRelationsCleaned=true");
            GameDataSaveLoader.SaveGame("TMM-Story");
            C("SaveWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-Story.rws")), true, "checked");
            ActionOf(choice, 0)(); // trust him (the reload answers the same letter from the save)
            AfterTrust(s, run);
            stage = 2;
            return;
        }
        if (stage == 2 && uiChecked)
        {
            C("ChronicleDrawsClean", uiError == null, "null", uiError);
            Finish(null, false);
        }
    }

    private void AfterTrust(SparkStory s, StoryQuestRun run)
    {
        int after = HeavenGoodwill;
        C("HeavenGoodwillPlus15", Heaven != null && after - heavenBefore == Math.Min(15, 100 - heavenBefore), "+15", after - heavenBefore);
        C("TrustRaised", s.GetInt("zeroSynth.trust") == 10, 10, s.GetInt("zeroSynth.trust"));
        C("HolodiskOne", s.GetInt("holodisk") == 1, 1, s.GetInt("holodisk"));
        C("CastielCompleted", s.GetFlag("q." + Castiel) == "trust" && run.status == StoryQuestStatus.Completed, "trust", s.GetFlag("q." + Castiel));
        C("ReplyLetterSent", Find.LetterStack.LettersListForReading.Any(l => string.Equals(l.Label.ToString(), DefDatabase<StoryQuestDef>.GetNamed(Castiel).LabelCap.ToString(), StringComparison.OrdinalIgnoreCase)), "reply", "checked");
        List<string> lines = MainTabWindow_Chronicle.BuildLines(s);
        C("ChronicleLogsDecisions", lines.Any(l => l.Contains("→ trust")) && lines.Any(l => l.Contains("→ done")), "two log lines", string.Join(" | ", lines.ToArray()));
    }

    /// F1: a saved run whose StoryQuestDef no longer exists must be disabled and must not block offers.
    private void DisabledDefCheck(SparkStory s)
    {
        var runs = (List<StoryQuestRun>)AccessTools.Field(typeof(SparkStory), "runs").GetValue(s);
        var ghost = new StoryQuestRun { quest = "TMM_Story_RemovedInUpdate", status = StoryQuestStatus.Offered, gen = 1 };
        runs.Add(ghost);
        AccessTools.Field(typeof(SparkStory), "reconciled").SetValue(s, false);
        s.DirectorTick();
        C("MissingDefDisabled", ghost.status == StoryQuestStatus.Disabled && ghost.history.Contains("disabled:def missing"), "Disabled", ghost.status);
    }

    /// F3: a hero standing on a map is not teleported; a free world hero arrives once; joining again is idempotent.
    private void HeroJoinsChecks(SparkStory s, StoryQuestRun anyRun)
    {
        HeroRegistry reg = HeroRegistry.Instance;
        StoryContext ctx = s.Context(anyRun);
        HeroDef deanDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_dean_winchester");
        Pawn dean = reg.GetOrCreate(deanDef);
        if (Find.WorldPawns.Contains(dean)) Find.WorldPawns.RemovePawn(dean);
        GenSpawn.Spawn(dean, CellFinder.RandomClosewalkCellNear(map.Center, map, 10), map);
        Faction deanFaction = dean.Faction;
        bool joined = StoryEffect_HeroJoins.TryJoin(deanDef, ctx);
        C("SpawnedHeroNotStolen", !joined && dean.Spawned && dean.Faction == deanFaction && s.GetFlag("defector." + deanDef.defName) != "true", "refused, unchanged", joined + "/" + dean.Faction);

        HeroDef samDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_sam_winchester");
        Pawn sam = reg.GetOrCreate(samDef);
        bool samJoined = StoryEffect_HeroJoins.TryJoin(samDef, ctx);
        C("FreeHeroJoins", samJoined && sam.Faction == Faction.OfPlayer && sam.holdingOwner != null && s.GetFlag("defector." + samDef.defName) == "true", "in drop pod, player faction", samJoined + "/" + sam.Faction + "/" + (sam.holdingOwner != null));
        bool again = StoryEffect_HeroJoins.TryJoin(samDef, ctx);
        C("JoinIdempotent", again && reg.Records.Count(r => r.pawn == sam) == 1, "true, one record", again);
    }

    private void Reload()
    {
        SparkStory s = SparkStory.Instance;
        if (stage == 0)
        {
            s.DirectorTick();
            StoryQuestRun sig = s.RunFor(Signal);
            C("OpeningStillCompleted", sig != null && sig.status == StoryQuestStatus.Completed && sig.history.Count == 2, "Completed, 2 entries", sig == null ? "none" : sig.status + "/" + sig.history.Count);
            StoryQuestRun run = s.RunFor(Castiel);
            C("PendingChoiceSurvived", run != null && run.status == StoryQuestStatus.Active && run.pendingChoice, "Active + pending", run == null ? "none" : run.status + "/" + run.pendingChoice);
            int letters = Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().Count(l => l.storyQuest == Castiel && l.kind == StoryChoiceLetter.KindChoice);
            C("ExactlyOneChoiceLetter", letters == 1, 1, letters);
            StoryChoiceLetter choice = LetterFor(Castiel, StoryChoiceLetter.KindChoice);
            C("LetterGenMatches", choice != null && choice.gen == run.gen, run == null ? -1 : run.gen, choice == null ? -1 : choice.gen);
            C("GhostStillDisabled", s.Runs.Any(r => r.quest == "TMM_Story_RemovedInUpdate" && r.status == StoryQuestStatus.Disabled), "Disabled", "checked");
            heavenBefore = HeavenGoodwill;
            Action trust = ActionOf(choice, 0);
            trust();
            int afterOnce = HeavenGoodwill;
            trust(); // repeated after load
            C("ChoiceAppliedOnceAfterLoad", HeavenGoodwill == afterOnce && afterOnce - heavenBefore == Math.Min(15, 100 - heavenBefore), "+15 once", (afterOnce - heavenBefore) + "/" + (HeavenGoodwill - afterOnce));
            C("CastielCompletedAfterLoad", s.GetFlag("q." + Castiel) == "trust", "trust", s.GetFlag("q." + Castiel));
            C("HolodiskOneAfterLoad", s.GetInt("holodisk") == 1, 1, s.GetInt("holodisk"));
            Finish(null, true);
        }
    }

    public override void MapComponentOnGUI()
    {
        if (done || uiChecked || stage != 2 || Event.current.type != EventType.Repaint) return;
        try
        {
            var w = new MainTabWindow_Chronicle();
            Rect r = new Rect(0f, 0f, 760f, 600f);
            GUI.BeginGroup(r);
            try { w.DoWindowContents(r); } finally { GUI.EndGroup(); }
        }
        catch (Exception ex) { uiError = ex.GetType().Name + ": " + ex.Message; }
        uiChecked = true;
    }

    private static void CleanDanglingRelations()
    {
        var known = new HashSet<Pawn>();
        foreach (Map m in Find.Maps) foreach (Pawn p in m.mapPawns.AllPawns) known.Add(p);
        foreach (Pawn p in Find.WorldPawns.AllPawnsAliveOrDead) known.Add(p);
        foreach (Pawn p in known.ToList())
        {
            if (p.relations == null || p.Faction == Faction.OfPlayer) continue;
            foreach (DirectPawnRelation rel in p.relations.DirectRelations.ToList())
                if (rel.otherPawn == null || !known.Contains(rel.otherPawn)) p.relations.RemoveDirectRelation(rel);
        }
    }

    private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    private void Finish(Exception ex, bool reload)
    {
        done = true;
        bool passed = ex == null && checks.Count > 0 && checks.All(c => c.passed);
        var cj = checks.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}", J(c.name), c.passed ? "true" : "false", J(c.expected), J(c.actual)));
        string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks.Count + ",\n  \"facts\": [" +
            string.Join(",", facts.Select(f => "\"" + J(f) + "\"").ToArray()) + "],\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
            "\n  ],\n  \"errors\": [" + (ex == null ? "" : "\"" + J(ex.ToString()) + "\"") + "]\n}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, reload ? "StoryReloadProbe.json" : "StoryProbe.json"), json);
        if (!reload) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "StoryUiProbe.json"), "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":false}");
    }
}
