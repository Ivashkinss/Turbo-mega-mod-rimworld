using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;

// Story engine stage E1. Fresh: -tmm-story-probe. Reload: -tmm-story-reload (save TMM-Story, taken mid-wait).
// Real actions only: the director starts the opening quest, the probe answers real letters through their DiaOptions,
// waits real ticks, declines and re-receives an offer, and checks goodwill, flags and the Chronicle model.
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
    private int stage, frames, waitStart;
    private int heavenBefore;

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

    private static void Pick(StoryChoiceLetter letter, int index)
    {
        List<DiaOption> opts = letter.Choices.ToList();
        opts[index].action();
    }

    private static Faction Heaven { get { return StoryUtil.FactionOf("TMM_HeavenFaction"); } }

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
            C("ChoiceLetterInStack", letter != null, "letter", letter == null ? "none" : letter.Label.ToString());
            C("ColonyNameSubstituted", letter != null && !letter.Text.ToString().Contains("{COLONY}"), "no placeholder", letter == null ? "" : "checked");
            C("TwoChoices", letter != null && letter.Choices.Count() == 2, 2, letter == null ? 0 : letter.Choices.Count());
            Pick(letter, 0); // force the hatch open
            C("ChoiceFlagSet", s.GetFlag("story.anchorOpened") == "true", "true", s.GetFlag("story.anchorOpened"));
            C("ChoiceLetterRemoved", LetterFor(Signal, StoryChoiceLetter.KindChoice) == null, "removed", LetterFor(Signal, StoryChoiceLetter.KindChoice) == null ? "removed" : "still there");
            C("AdvancedToWait", run.stage == 1 && run.stageUntilTick > now, "stage 1 with deadline", run.stage + "/" + run.stageUntilTick);
            List<string> lines = MainTabWindow_Chronicle.BuildLines(s);
            C("ChronicleShowsActive", lines.Any(l => l.Contains(DefDatabase<StoryQuestDef>.GetNamed(Signal).LabelCap)), "active quest line", lines.Count);
            CleanDanglingRelations();
            GameDataSaveLoader.SaveGame("TMM-Story");
            C("SaveWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-Story.rws")), true, "checked");
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
            C("GiverNameInText", offer != null && offer.Text.ToString().Contains("Castiel") && !offer.Text.ToString().Contains("{GIVER}"), "Castiel", offer == null ? "" : offer.Text.ToString().Substring(0, Math.Min(80, offer.Text.ToString().Length)));
            Pick(offer, 1); // later
            StoryQuestRun run = s.RunFor(Castiel);
            C("DeclinedWithCooldown", run.status == StoryQuestStatus.Declined && run.cooldownUntilTick > now, "Declined", run.status + "/" + (run.cooldownUntilTick - now));
            s.DirectorTick();
            C("NotReofferedDuringCooldown", LetterFor(Castiel, StoryChoiceLetter.KindOffer) == null, "no offer", LetterFor(Castiel, StoryChoiceLetter.KindOffer) == null ? "no offer" : "offered");
            run.cooldownUntilTick = now; // the cooldown has passed
            s.DirectorTick();
            offer = LetterFor(Castiel, StoryChoiceLetter.KindOffer);
            C("ReofferedAfterCooldown", offer != null && run.status == StoryQuestStatus.Offered, "Offered", run.status);
            heavenBefore = Heaven == null ? 0 : Heaven.GoodwillWith(Faction.OfPlayer);
            Pick(offer, 0); // accept
            C("Accepted", run.status == StoryQuestStatus.Active && run.stage == 0, "Active/0", run.status + "/" + run.stage);
            StoryChoiceLetter choice = LetterFor(Castiel, StoryChoiceLetter.KindChoice);
            C("CastielChoiceLetter", choice != null, "letter", choice == null ? "none" : "ok");
            Pick(choice, 0); // trust him
            int heavenAfter = Heaven == null ? 0 : Heaven.GoodwillWith(Faction.OfPlayer);
            C("HeavenGoodwillPlus15", Heaven != null && heavenAfter - heavenBefore == Math.Min(15, 100 - heavenBefore), "+15", heavenAfter - heavenBefore);
            C("TrustRaised", s.GetInt("zeroSynth.trust") == 10, 10, s.GetInt("zeroSynth.trust"));
            C("HolodiskOne", s.GetInt("holodisk") == 1, 1, s.GetInt("holodisk"));
            C("CastielCompleted", s.GetFlag("q." + Castiel) == "trust", "trust", s.GetFlag("q." + Castiel));
            C("ReplyLetterSent", Find.LetterStack.LettersListForReading.Any(l => string.Equals(l.Label.ToString(), DefDatabase<StoryQuestDef>.GetNamed(Castiel).LabelCap.ToString(), StringComparison.OrdinalIgnoreCase)), "reply", string.Join(" | ", Find.LetterStack.LettersListForReading.Select(l => l.Label.ToString()).ToArray()));
            List<string> lines = MainTabWindow_Chronicle.BuildLines(s);
            C("ChronicleLogsDecisions", lines.Any(l => l.Contains("→ trust")) && lines.Any(l => l.Contains("→ done")), "two log lines", string.Join(" | ", lines.ToArray()));
            stage = 2;
            return;
        }
        if (stage == 2 && uiChecked)
        {
            C("ChronicleDrawsClean", uiError == null, "null", uiError);
            Finish(null, false);
        }
    }

    private void Reload()
    {
        SparkStory s = SparkStory.Instance;
        int now = Find.TickManager.TicksGame;
        if (stage == 0)
        {
            StoryQuestRun run = s == null ? null : s.RunFor(Signal);
            C("RunSurvivedLoad", run != null && run.status == StoryQuestStatus.Active && run.stage == 1, "Active/1", run == null ? "none" : run.status + "/" + run.stage);
            C("FlagSurvivedLoad", s != null && s.GetFlag("story.anchorOpened") == "true", "true", s == null ? "" : s.GetFlag("story.anchorOpened"));
            C("HistorySurvivedLoad", run != null && run.history.Count == 1 && run.history[0] == "0:opened", "0:opened", run == null ? "" : string.Join(",", run.history.ToArray()));
            stage = 1; waitStart = now;
            return;
        }
        if (stage == 1)
        {
            if (!s.IsDone(Signal))
            {
                if (now - waitStart > 90000) throw new Exception("wait stage did not finish after reload");
                return;
            }
            StoryQuestRun run = s.RunFor(Signal);
            C("CompletedOnceAfterLoad", run.history.Count == 2 && s.Runs.Count(r => r.quest == Signal) == 1, "2 history, 1 run", run.history.Count + "/" + s.Runs.Count(r => r.quest == Signal));
            s.DirectorTick();
            int offers = Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().Count(l => l.storyQuest == Castiel && l.kind == StoryChoiceLetter.KindOffer);
            C("CastielOfferedOnceAfterLoad", offers == 1, 1, offers);
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
