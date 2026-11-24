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
    private static bool worldDone;
    private static void NoHeaven(ref List<FactionDef> factions)
    {
        if (worldDone) return;
        worldDone = true;
        var page = new Page_CreateWorldParams();
        AccessTools.Method(typeof(Page_CreateWorldParams), "ResetFactionCounts").Invoke(page, null);
        factions = ((List<FactionDef>)AccessTools.Field(typeof(Page_CreateWorldParams), "factions").GetValue(page)).Where(d => d.defName != "TMM_HeavenFaction").ToList();
    }
    static StoryProbeLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-story-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame(StoryEngineProbe.SaveName));
        if (StoryEngineProbe.Case == "noheaven" && Environment.GetCommandLineArgs().Contains("-tmm-story-probe"))
            new Harmony("tmm.test.story.noheaven").Patch(AccessTools.Method(typeof(RimWorld.Planet.WorldGenerator), "GenerateWorld"), prefix: new HarmonyMethod(typeof(StoryProbeLaunch), "NoHeaven"));
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps == null) return;
            Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
            if (home == null) return;
            if (Environment.GetCommandLineArgs().Contains("-tmm-story-content-probe"))
            {
                if (home.GetComponent<StoryContentProbe>() == null) home.components.Add(new StoryContentProbe(home));
            }
            else
            {
                if (home.GetComponent<StoryEngineProbe>() == null) home.components.Add(new StoryEngineProbe(home));
            }
        });
    }
}

// Fresh cases (TMM_STORY_CASE): delayed (default), open (the hotel is open before the dream), fallback (Castiel is dead, Naomi comes),
// noheaven (a world without Heaven). Saves (TMM_STORY_SAVE for the reload): before the hotel, stay (default), after the choice.
public sealed class StoryEngineProbe : MapComponent
{
    public static string Case { get { string s = Environment.GetEnvironmentVariable("TMM_STORY_CASE"); return string.IsNullOrEmpty(s) ? "delayed" : s; } }
    public static string SavePoint { get { string s = Environment.GetEnvironmentVariable("TMM_STORY_SAVE"); return string.IsNullOrEmpty(s) ? "stay" : s; } }
    public static string SaveName { get { return SavePoint == "stay" ? "TMM-Story" : "TMM-Story-" + SavePoint; } }
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
            if (Case == "open") { BuildHotel(); C("HotelReadyBeforeDream", StoryHotelGate.ReadyHome(true) == map, "ready", StoryHotelGate.MissingKey(map, true)); }
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
            StoryQuestRun run = s.RunFor(Castiel);
            C("CastielGoalStarted", run != null && run.status == StoryQuestStatus.Active && run.Def.stages[run.stage].key == (Case == "open" && run.stage == 1 ? "stay" : "openHotel"), "Active, hotel goal", run == null ? "none" : run.status + "/" + run.stageKey);
            C("NoOfferLetterAnyMore", LetterFor(Castiel, StoryChoiceLetter.KindOffer) == null, "no offer", "checked");
            C("GoalLetterSentOnce", GoalLetters() == (Case == "open" ? 0 : 1), Case == "open" ? 0 : 1, GoalLetters());
            if (Case != "open")
            {
                string objective = run.Def.stages[run.stage].Objective(s.Context(run));
                C("GoalNamesMissingThing", objective.Contains("TMM.Story.Hotel.MissingDesk".Translate().ToString()), "desk is missing", objective);
                C("GoalInChronicle", MainTabWindow_Chronicle.BuildLines(s).Any(l => l.Contains(run.Def.stages[0].label)), run.Def.stages[0].label, "checked");
            }
            DisabledDefCheck(s);
            HeroJoinsChecks(s, run);
            if (Case == "fallback")
            {
                Pawn cas = HeroRegistry.Instance.GetOrCreate(Hero("TMM_Hero_castiel"));
                if (cas != null) cas.Kill(null);
                C("CastielDeadForFallback", HeroRegistry.Instance.GetOrCreate(Hero("TMM_Hero_castiel")) == null, "dead", "checked");
            }
            CleanDanglingRelations(); // fixture only: removes world-gen relations to discarded pawns that are unrelated to the engine
            facts.Add("case=" + Case);
            facts.Add("fixtureRelationsCleaned=true");
            GameDataSaveLoader.SaveGame("TMM-Story-before");
            stage = 2; waitStart = now;
            return;
        }
        if (stage == 2)
        {
            StoryQuestRun run = s.RunFor(Castiel);
            if (Case != "open")
            {
                if (now - waitStart < 4000) return;
                C("GoalWaitsWithoutHotel", run.status == StoryQuestStatus.Active && run.Def.stages[run.stage].key == "openHotel", "openHotel", run.stageKey);
                C("GoalLetterNotRepeated", GoalLetters() == 1, 1, GoalLetters());
                BuildHotel();
                C("HotelReadyByServiceReadiness", StoryHotelGate.ReadyHome(true) == map, "ready", StoryHotelGate.MissingKey(map, true));
            }
            C("SaveBeforeWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-Story-before.rws")), true, "checked");
            stage = 3; waitStart = now;
            return;
        }
        if (stage == 3)
        {
            StoryQuestRun run = s.RunFor(Castiel);
            if (Case == "noheaven")
            {
                if (now - waitStart < 6000) return;
                CheckNoHeaven(s, run, "Fresh");
                GameDataSaveLoader.SaveGame("TMM-Story");
                stage = 6; waitStart = now;
                return;
            }
            if (LetterFor(Castiel, StoryChoiceLetter.KindChoice) == null)
            {
                if (now - waitStart > 60000) throw new Exception("the guest never checked in: stage=" + run.stageKey + " vars=" + string.Join(",", run.vars.Select(v => v.Key + "=" + v.Value).ToArray()) + " objective=" + run.Def.stages[run.stage].Objective(s.Context(run)));
                string early; // the choice must not be offered before the guest has a room
                Pawn walking = Visitor(run);
                if (run.pendingChoice && (walking == null || HotelBillingHooks.Bed(walking) == null)) { early = "offered without a room"; throw new Exception(early); }
                return;
            }
            Pawn guest = Visitor(run);
            string want = Case == "fallback" ? "TMM_Hero_naomi" : "TMM_Hero_castiel";
            string visitor; run.vars.TryGetValue("visitorHero", out visitor);
            C("RightGuestCame", visitor == want && guest != null && guest == HeroRegistry.Instance.RecordFor(Hero(want)).pawn, want, visitor);
            C("GuestHasRoom", guest != null && HotelBillingHooks.Bed(guest) != null && map.GetComponent<HotelBilling>().stays.Any(x => x.guest == guest), "claimed bed and a hotel stay", guest == null ? "none" : "bed=" + (HotelBillingHooks.Bed(guest) != null) + " stays=" + map.GetComponent<HotelBilling>().stays.Count);
            string mode; run.vars.TryGetValue("visitMode", out mode);
            C("HospitalityVisit", mode == "hospitality", "hospitality", mode);
            C("OneGuestOnly", map.mapPawns.AllPawnsSpawned.Count(p => p.Faction == Heaven) == 1, 1, map.mapPawns.AllPawnsSpawned.Count(p => p.Faction == Heaven));
            StoryChoiceLetter choice = LetterFor(Castiel, StoryChoiceLetter.KindChoice);
            C("ChoicePendingDuringStay", run.pendingChoice && choice.gen == run.gen && run.Def.stages[run.stage].key == "stay", "pending at stay", run.stageKey + "/" + run.pendingChoice);
            string name = Hero(want).label;
            C("GiverNameInText", choice.Text.ToString().Contains(name) && !choice.Text.ToString().Contains("{GIVER}"), name, "checked");
            // A hotel opened long before the dream invites its ordinary first group as always; one opened for the story must not.
            if (Case != "open") C("OpeningVisitNotDoubled", map.GetComponent<ComplexOpeningVisit>().scheduledTick < 0, "no separate opening group", map.GetComponent<ComplexOpeningVisit>().scheduledTick);
            facts.Add("guestWalkTicks=" + (now - waitStart));
            CleanDanglingRelations();
            GameDataSaveLoader.SaveGame("TMM-Story");
            stage = 4; waitStart = now;
            return;
        }
        if (stage == 4)
        {
            if (!File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-Story.rws")) || now - waitStart < 300) return;
            C("SaveWritten", true, true, "checked");
            StoryQuestRun run = s.RunFor(Castiel);
            C("OneChoiceLetterWhileWaiting", Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().Count(l => l.storyQuest == Castiel) == 1, 1, "checked");
            StoryChoiceLetter choice = LetterFor(Castiel, StoryChoiceLetter.KindChoice);
            heavenBefore = HeavenGoodwill;
            int expected = Heaven.CalculateAdjustedGoodwillChange(Faction.OfPlayer, 15);
            Action trust = ActionOf(choice, 0);
            trust();
            int afterOnce = HeavenGoodwill;
            trust(); // repeated click
            facts.Add("goodwill before=" + heavenBefore + " natural=" + Heaven.NaturalGoodwill + " expected=" + expected + " real=" + (afterOnce - heavenBefore));
            C("HeavenGoodwillExact", afterOnce - heavenBefore == Math.Min(expected, 100 - heavenBefore), expected, afterOnce - heavenBefore);
            C("RepeatedChoiceNoEffect", HeavenGoodwill == afterOnce && s.GetInt("zeroSynth.trust") == 10, "0", HeavenGoodwill - afterOnce);
            AfterTrust(s, run);
            GameDataSaveLoader.SaveGame("TMM-Story-after");
            stage = 5; waitStart = now;
            return;
        }
        if (stage == 5)
        {
            if (!File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-Story-after.rws")) || now - waitStart < 600) return;
            string want = Case == "fallback" ? "TMM_Hero_naomi" : "TMM_Hero_castiel";
            Pawn guest = HeroRegistry.Instance.RecordFor(Hero(want)).pawn;
            C("GuestStaysAfterTrust", guest != null && guest.Spawned && !guest.Dead && !guest.HostileTo(Faction.OfPlayer), "still a peaceful guest", guest == null ? "none" : guest.Spawned + "/" + guest.Dead);
            stage = 6; waitStart = now;
            return;
        }
        if (stage == 6 && uiChecked && File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-Story.rws")))
        {
            C("ChronicleDrawsClean", uiError == null, "null", uiError);
            Finish(null, false);
        }
    }

    private static HeroDef Hero(string name) { return DefDatabase<HeroDef>.GetNamed(name); }

    private Pawn Visitor(StoryQuestRun run)
    {
        string id; int n;
        if (!run.vars.TryGetValue("heroIdNum", out id) || !int.TryParse(id, out n)) return null;
        return map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.thingIDNumber == n);
    }

    private static int GoalLetters()
    {
        string label = DefDatabase<StoryQuestDef>.GetNamed(Castiel).stages[0].label;
        return Find.Archive.ArchivablesListForReading.OfType<Letter>().Count(l => l.Label.ToString() == label);
    }

    private Building Build(string name, IntVec3 c)
    {
        var d = DefDatabase<ThingDef>.GetNamed(name);
        var b = (Building)ThingMaker.MakeThing(d, d.MadeFromStuff ? ThingDefOf.Steel : null);
        b.SetFaction(Faction.OfPlayer);
        return (Building)GenSpawn.Spawn(b, c, map);
    }

    private void BuildHotel()
    {
        map.fogGrid.ClearAllFog();
        IntVec3 at = map.Center;
        CellRect ground = CellRect.CenteredOn(at, 12);
        foreach (IntVec3 c in ground)
        {
            foreach (Thing t in c.GetThingList(map).ToArray()) if (t is Building || t is Plant || t is Filth) { if (t.def.destroyable) t.Destroy(); else t.DeSpawn(); }
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil);
            map.roofGrid.SetRoof(c, null);
        }
        Area_Allowed area, staff;
        map.areaManager.TryMakeNewAllowed(out area);
        map.areaManager.TryMakeNewAllowed(out staff);
        foreach (IntVec3 c in ground) area[c] = true;
        var desk = (Building_ComplexDesk)Build("TMM_ComplexDesk", at + new IntVec3(-8, 0, -8));
        desk.area = area; desk.staffArea = staff; desk.vipChance = 0;
        CellRect room = new CellRect(at.x - 4, at.z - 4, 9, 9);
        foreach (IntVec3 c in room.EdgeCells) Build(c == new IntVec3(room.CenterCell.x, 0, room.minZ) ? "Door" : "Wall", c);
        foreach (IntVec3 c in room.ContractedBy(1)) map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
        Build("BedGuest", at + new IntVec3(-1, 0, 1));
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        desk.ApplyServiceAreas();
        object native = map.components.First(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
        AccessTools.Field(native.GetType(), "guestsAreWelcome").SetValue(native, true);
    }

    private void CheckNoHeaven(SparkStory s, StoryQuestRun run, string when)
    {
        C(when + "_NoHeavenFaction", Heaven == null, "absent", Heaven == null ? "absent" : "present");
        C(when + "_StoryWaitsAtHotel", run != null && run.status == StoryQuestStatus.Active && run.stageKey == "stay" && run.vars.ContainsKey("waitingForGuest"), "Active at stay, waiting", run == null ? "none" : run.status + "/" + run.stageKey);
        string objective = run.Def.stages[run.stage].Objective(s.Context(run));
        C(when + "_ReasonShown", objective.Contains(DefDatabase<FactionDef>.GetNamed("TMM_HeavenFaction").LabelCap.ToString()), "names the missing faction", objective);
        C(when + "_NoHeroInvented", HeroRegistry.Instance.RecordFor(Hero("TMM_Hero_castiel")) == null && HeroRegistry.Instance.RecordFor(Hero("TMM_Hero_naomi")) == null, "no records", "checked");
        C(when + "_OrdinaryHotelNotBlocked", !StoryHotelGate.StoryGuestExpected, "false", StoryHotelGate.StoryGuestExpected);
        C(when + "_GoalLetterOnce", GoalLetters() == 1, 1, GoalLetters());
    }

    private void AfterTrust(SparkStory s, StoryQuestRun run)
    {
        C("TrustRaised", s.GetInt("zeroSynth.trust") == 10, 10, s.GetInt("zeroSynth.trust"));
        C("HolodiskOne", s.GetInt("holodisk") == 1, 1, s.GetInt("holodisk"));
        C("CastielCompleted", s.GetFlag("q." + Castiel) == "trust" && run.status == StoryQuestStatus.Completed, "trust", s.GetFlag("q." + Castiel) + "/" + run.status);
        C("ReplyLetterSent", Find.LetterStack.LettersListForReading.Any(l => string.Equals(l.Label.ToString(), DefDatabase<StoryQuestDef>.GetNamed(Castiel).LabelCap.ToString(), StringComparison.OrdinalIgnoreCase)), "reply", "checked");
        StoryQuestDef uninvited = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_A1_UninvitedGuests");
        StoryQuestRun next = s.RunFor("TMM_Story_A1_UninvitedGuests");
        C("UninvitedGuestsAvailable", uninvited != null && (next != null || s.IsAvailable(uninvited)), true, next == null ? "available" : next.status.ToString());
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
            C("GhostStillDisabled", s.Runs.Any(r => r.quest == "TMM_Story_RemovedInUpdate" && r.status == StoryQuestStatus.Disabled), "Disabled", "checked");
            stage = 1; waitStart = Find.TickManager.TicksGame;
            return;
        }
        if (stage != 1) return;
        StoryQuestRun run = s.RunFor(Castiel);
        bool heavenless = Heaven == null;
        // Let the loaded game run: nothing may be repeated by time alone.
        if (Find.TickManager.TicksGame - waitStart < 1500) return;
        facts.Add("save=" + SaveName);
        if (SavePoint == "before")
        {
            C("GoalSurvived", run != null && run.status == StoryQuestStatus.Active && run.stageKey == "openHotel", "openHotel", run == null ? "none" : run.stageKey);
            C("GoalLetterNotRepeatedAfterLoad", GoalLetters() == 1, 1, GoalLetters());
            Finish(null, true);
            return;
        }
        if (SavePoint == "legacy")
        {
            // A real save made before the hotel stages existed, with the old choice letter open.
            C("LegacyStageRemapped", run != null && run.status == StoryQuestStatus.Active && run.stageKey == "#0" && run.stage == 2 && run.pendingChoice, "Active #0 at index 2", run == null ? "none" : run.status + "/" + run.stageKey + "/" + run.stage);
            List<StoryChoiceLetter> old = Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().Where(l => l.storyQuest == Castiel && l.kind == StoryChoiceLetter.KindChoice).ToList();
            C("LegacyOneLetterStillOpen", old.Count == 1 && old[0].stage == 2 && old[0].Choices.Count() == 2, "one live letter", old.Count);
            C("LegacyNoHotelAsked", GoalLetters() == 0, 0, GoalLetters());
            int was = HeavenGoodwill, want15 = Heaven.CalculateAdjustedGoodwillChange(Faction.OfPlayer, 15);
            Action answer = ActionOf(old[0], 0);
            answer();
            int got = HeavenGoodwill - was;
            answer();
            C("LegacyChoiceOnce", got == want15 && HeavenGoodwill - was == got && s.GetFlag("q." + Castiel) == "trust" && run.status == StoryQuestStatus.Completed && s.GetInt("holodisk") == 1, want15 + "/trust/Completed/1", got + "/" + s.GetFlag("q." + Castiel) + "/" + run.status + "/" + s.GetInt("holodisk"));
            Finish(null, true);
            return;
        }
        if (heavenless) { CheckNoHeaven(s, run, "Reload"); Finish(null, true); return; }
        string want = Case == "fallback" ? "TMM_Hero_naomi" : "TMM_Hero_castiel";
        int onMap = map.mapPawns.AllPawnsSpawned.Count(p => p.Faction == Heaven);
        Pawn hero = HeroRegistry.Instance.RecordFor(Hero(want)).pawn;
        if (SavePoint == "after")
        {
            C("ChoiceKeptAfterLoad", run.status == StoryQuestStatus.Completed && s.GetFlag("q." + Castiel) == "trust" && s.GetInt("zeroSynth.trust") == 10 && s.GetInt("holodisk") == 1, "Completed/trust/10/1", run.status + "/" + s.GetFlag("q." + Castiel) + "/" + s.GetInt("zeroSynth.trust") + "/" + s.GetInt("holodisk"));
            C("NoChoiceLetterAfterLoad", LetterFor(Castiel, StoryChoiceLetter.KindChoice) == null, "none", "checked");
            C("NoSecondGuestAfterLoad", onMap <= 1 && hero != null && !hero.Dead, "0-1", onMap);
            StoryQuestRun next = s.RunFor("TMM_Story_A1_UninvitedGuests");
            C("NextQuestStarts", next != null && next.status == StoryQuestStatus.Offered, "Offered", next == null ? "none" : next.status.ToString());
            Finish(null, true);
            return;
        }
        C("PendingChoiceSurvived", run != null && run.status == StoryQuestStatus.Active && run.pendingChoice && run.stageKey == "stay", "Active + pending at stay", run == null ? "none" : run.status + "/" + run.pendingChoice + "/" + run.stageKey);
        int letters = Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().Count(l => l.storyQuest == Castiel && l.kind == StoryChoiceLetter.KindChoice);
        C("ExactlyOneChoiceLetter", letters == 1, 1, letters);
        C("SameGuestAfterLoad", onMap == 1 && hero != null && hero.Spawned && Visitor(run) == hero && HotelBillingHooks.Bed(hero) != null, "one guest with a room", onMap);
        StoryChoiceLetter choice = LetterFor(Castiel, StoryChoiceLetter.KindChoice);
        C("LetterGenMatches", choice != null && choice.gen == run.gen, run == null ? -1 : run.gen, choice == null ? -1 : choice.gen);
        heavenBefore = HeavenGoodwill;
        int expected = Heaven.CalculateAdjustedGoodwillChange(Faction.OfPlayer, 15);
        Action trust = ActionOf(choice, 0);
        trust();
        int afterOnce = HeavenGoodwill;
        trust(); // repeated after load
        C("ChoiceAppliedOnceAfterLoad", HeavenGoodwill == afterOnce && afterOnce - heavenBefore == Math.Min(expected, 100 - heavenBefore), expected + " once", (afterOnce - heavenBefore) + "/" + (HeavenGoodwill - afterOnce));
        C("CastielCompletedAfterLoad", s.GetFlag("q." + Castiel) == "trust", "trust", s.GetFlag("q." + Castiel));
        C("HolodiskOneAfterLoad", s.GetInt("holodisk") == 1, 1, s.GetInt("holodisk"));
        Finish(null, true);
    }
    public override void MapComponentOnGUI()
    {
        if (done || uiChecked || stage != 6 || Event.current.type != EventType.Repaint) return;
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

public sealed class StoryContentProbe : MapComponent
{
    private sealed class Check { public string name, expected, actual; public bool passed; }
    private readonly List<Check> checks = new List<Check>();
    private readonly List<string> facts = new List<string>();
    private bool done, uiChecked;
    private string uiError;
    private int stage, frames;

    public StoryContentProbe(Map map) : base(map) { }

    private void C(string name, bool ok, object expected, object actual)
    {
        checks.Add(new Check { name = name, passed = ok, expected = Convert.ToString(expected), actual = Convert.ToString(actual) });
    }

    private static StoryChoiceLetter LetterFor(string quest, string kind)
    {
        return Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().FirstOrDefault(l => l.storyQuest == quest && l.kind == kind);
    }

    private static Action ActionOf(StoryChoiceLetter letter, int index)
    {
        if (letter == null) return null;
        List<DiaOption> choices = letter.Choices.ToList();
        return (index >= 0 && index < choices.Count) ? choices[index].action : null;
    }

    private static Faction EnsureFaction(string defName)
    {
        Faction f = StoryUtil.FactionOf(defName);
        if (f != null) return f;
        FactionDef fd = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
        if (fd == null) return null;
        f = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(fd));
        Find.FactionManager.Add(f);
        return f;
    }

    public override void MapComponentUpdate()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (done || !map.IsPlayerHome || !args.Contains("-tmm-story-content-probe")) return;
        if (Find.TickManager.CurTimeSpeed != TimeSpeed.Ultrafast) Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
        foreach (Window w in Find.WindowStack.Windows.ToList()) if (w.forcePause) w.Close(false);
        if (++frames < 30) return;
        SparkStory.MinOfferGapTicks = 0;
        SparkStory.MaxActive = 10;
        try
        {
            if (stage == 0)
            {
                RunFreshQuests();
                stage = 1;
                return;
            }
            if (stage == 1 && uiChecked)
            {
                C("ChronicleDrawsClean", uiError == null, "null", uiError);
                Finish(null);
            }
        }
        catch (Exception ex)
        {
            Finish(ex);
        }
    }

    public override void MapComponentOnGUI()
    {
        if (done || uiChecked || stage != 1 || Event.current.type != EventType.Repaint) return;
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

    private void RunFreshQuests()
    {
        SparkStory s = SparkStory.Instance;
        C("SparkStoryExists", s != null, true, s != null);
        if (s == null) return;

        EnsureFaction("TMM_InstituteFaction");
        EnsureFaction("TMM_MinutemenHuntersFaction");
        EnsureFaction("TMM_RailroadFaction");
        EnsureFaction("TMM_BrotherhoodFaction");
        EnsureFaction("TMM_HellFaction");
        EnsureFaction("TMM_HeavenFaction");
        EnsureFaction("TMM_MonsterClansFaction");
        EnsureFaction("TMM_GunnersFaction");
        EnsureFaction("TMM_WildMutantsFaction");

        // Act 2 starts
        s.SetFlag("act", "2");
        C("ActSetTo2", s.GetInt("act") == 2, 2, s.GetInt("act"));

        // ==========================================
        // 1. INSTITUTE FACTION QUEST LINE
        // ==========================================

        // Q1: Cold Scanner (HotelVisit -> Choice)
        StoryQuestDef instQ1 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_institute_Q1");
        C("Inst_Q1_Exists", instQ1 != null, true, instQ1 != null);
        C("Inst_Q1_Available", s.IsAvailable(instQ1), true, s.IsAvailable(instQ1));
        s.Offer(instQ1);
        StoryChoiceLetter offer = LetterFor("TMM_Story_institute_Q1", StoryChoiceLetter.KindOffer);
        C("Inst_Q1_OfferLetter", offer != null, true, offer != null);
        Action accept = ActionOf(offer, 0);
        if (accept != null) accept();
        StoryQuestRun runQ1 = s.RunFor("TMM_Story_institute_Q1");
        C("Inst_Q1_Active", runQ1 != null && runQ1.status == StoryQuestStatus.Active && runQ1.stage == 0, true, runQ1 != null ? (object)runQ1.status : "null");

        // Stage 0 is HotelVisit - force success
        if (runQ1 != null) runQ1.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("Inst_Q1_Stage1_ChoicePending", runQ1 != null && runQ1.stage == 1 && runQ1.pendingChoice, true, runQ1 != null ? (runQ1.stage + "/" + runQ1.pendingChoice) : "null");
        StoryChoiceLetter choice1 = LetterFor("TMM_Story_institute_Q1", StoryChoiceLetter.KindChoice);
        C("Inst_Q1_ChoiceLetter", choice1 != null, true, choice1 != null);
        Action actChoice1 = ActionOf(choice1, 0); // handed_over
        if (actChoice1 != null) actChoice1();
        C("Inst_Q1_Completed", s.IsDone("TMM_Story_institute_Q1"), true, s.IsDone("TMM_Story_institute_Q1"));
        C("Inst_Q1_FlagHandedOver", s.GetFlag("story.inst_q1") == "handed_over", "handed_over", s.GetFlag("story.inst_q1"));

        // Q2: Firmware Glitch (Site -> Wait -> Choice)
        StoryQuestDef instQ2 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_institute_Q2");
        C("Inst_Q2_Exists", instQ2 != null, true, instQ2 != null);
        C("Inst_Q2_Available", s.IsAvailable(instQ2), true, s.IsAvailable(instQ2));
        s.Offer(instQ2);
        Action acceptQ2 = ActionOf(LetterFor("TMM_Story_institute_Q2", StoryChoiceLetter.KindOffer), 0);
        if (acceptQ2 != null) acceptQ2();
        StoryQuestRun runQ2 = s.RunFor("TMM_Story_institute_Q2");
        C("Inst_Q2_Active", runQ2 != null && runQ2.status == StoryQuestStatus.Active && runQ2.stage == 0, true, runQ2 != null ? (object)runQ2.status : "null");

        // Stage 0 is Site - force success
        if (runQ2 != null) runQ2.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("Inst_Q2_Stage1_Wait", runQ2 != null && runQ2.stage == 1, 1, runQ2 != null ? runQ2.stage : -1);
        if (runQ2 != null) runQ2.stageUntilTick = Find.TickManager.TicksGame - 1;
        s.DirectorTick();
        C("Inst_Q2_Stage2_ChoicePending", runQ2 != null && runQ2.stage == 2 && runQ2.pendingChoice, true, runQ2 != null ? (runQ2.stage + "/" + runQ2.pendingChoice) : "null");
        Action actChoice2 = ActionOf(LetterFor("TMM_Story_institute_Q2", StoryChoiceLetter.KindChoice), 0); // preserved
        if (actChoice2 != null) actChoice2();
        C("Inst_Q2_Completed", s.IsDone("TMM_Story_institute_Q2"), true, s.IsDone("TMM_Story_institute_Q2"));
        C("Inst_Q2_FlagPreserved", s.GetFlag("story.inst_q2") == "preserved", "preserved", s.GetFlag("story.inst_q2"));

        // Q3: Reactor on the Brink (Site -> Choice)
        StoryQuestDef instQ3 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_institute_Q3");
        C("Inst_Q3_Exists", instQ3 != null, true, instQ3 != null);
        C("Inst_Q3_Available", s.IsAvailable(instQ3), true, s.IsAvailable(instQ3));
        s.Offer(instQ3);
        Action acceptQ3 = ActionOf(LetterFor("TMM_Story_institute_Q3", StoryChoiceLetter.KindOffer), 0);
        if (acceptQ3 != null) acceptQ3();
        StoryQuestRun runQ3 = s.RunFor("TMM_Story_institute_Q3");
        C("Inst_Q3_Active", runQ3 != null && runQ3.status == StoryQuestStatus.Active && runQ3.stage == 0, true, runQ3 != null ? (object)runQ3.status : "null");

        // Stage 0 is Site - force success
        if (runQ3 != null) runQ3.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("Inst_Q3_Stage1_ChoicePending", runQ3 != null && runQ3.stage == 1 && runQ3.pendingChoice, true, runQ3 != null ? (runQ3.stage + "/" + runQ3.pendingChoice) : "null");
        Action actChoice3 = ActionOf(LetterFor("TMM_Story_institute_Q3", StoryChoiceLetter.KindChoice), 0); // institute
        if (actChoice3 != null) actChoice3();
        C("Inst_Q3_Completed", s.IsDone("TMM_Story_institute_Q3"), true, s.IsDone("TMM_Story_institute_Q3"));
        C("Inst_Q3_FlagInstitute", s.GetFlag("story.inst_q3") == "institute", "institute", s.GetFlag("story.inst_q3"));

        // Q4 (Faction Point): Ghost in the Machine (Site -> Choice -> onComplete)
        StoryQuestDef instQ4 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_institute_Q4");
        C("Inst_Q4_Exists", instQ4 != null, true, instQ4 != null);
        C("Inst_Q4_Available", s.IsAvailable(instQ4), true, s.IsAvailable(instQ4));
        s.Offer(instQ4);
        Action acceptQ4 = ActionOf(LetterFor("TMM_Story_institute_Q4", StoryChoiceLetter.KindOffer), 0);
        if (acceptQ4 != null) acceptQ4();
        StoryQuestRun runQ4 = s.RunFor("TMM_Story_institute_Q4");
        C("Inst_Q4_Active", runQ4 != null && runQ4.status == StoryQuestStatus.Active && runQ4.stage == 0, true, runQ4 != null ? (object)runQ4.status : "null");

        // Stage 0 is Site - force success
        if (runQ4 != null) runQ4.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("Inst_Q4_Stage1_ChoicePending", runQ4 != null && runQ4.stage == 1 && runQ4.pendingChoice, true, runQ4 != null ? (runQ4.stage + "/" + runQ4.pendingChoice) : "null");
        // Choice 1 is pro_spark
        StoryChoiceLetter choice4 = LetterFor("TMM_Story_institute_Q4", StoryChoiceLetter.KindChoice);
        Action actChoice4 = ActionOf(choice4, 1);
        C("Inst_Q4_ProSparkAction", actChoice4 != null, true, actChoice4 != null);
        if (actChoice4 != null) actChoice4();
        C("Inst_Q4_Completed", s.IsDone("TMM_Story_institute_Q4"), true, s.IsDone("TMM_Story_institute_Q4"));
        C("Inst_ChoiceFlagProSpark", s.GetFlag("choice.institute") == "pro_spark", "pro_spark", s.GetFlag("choice.institute"));
        C("Inst_ArcFlag4", s.GetFlag("arc.institute") == "4", "4", s.GetFlag("arc.institute"));
        C("ArcPointsIncremented", s.GetInt("arcPoints") >= 1, true, s.GetInt("arcPoints"));

        // Q5 (Defector Quest): Escape from Below (ColonyRaid -> Choice)
        StoryQuestDef instQ5 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_institute_Q5");
        C("Inst_Q5_Exists", instQ5 != null, true, instQ5 != null);
        C("Inst_Q5_RequiresAct3", !s.IsAvailable(instQ5), true, !s.IsAvailable(instQ5));
        s.SetFlag("act", "3");
        C("Inst_Q5_UnlockedInAct3", s.IsAvailable(instQ5), true, s.IsAvailable(instQ5));
        s.Offer(instQ5);
        Action acceptQ5 = ActionOf(LetterFor("TMM_Story_institute_Q5", StoryChoiceLetter.KindOffer), 0);
        if (acceptQ5 != null) acceptQ5();
        StoryQuestRun runQ5 = s.RunFor("TMM_Story_institute_Q5");
        C("Inst_Q5_Active", runQ5 != null && runQ5.status == StoryQuestStatus.Active && runQ5.stage == 0, true, runQ5 != null ? (object)runQ5.status : "null");

        // Stage 0 is ColonyRaid - force success
        if (runQ5 != null) runQ5.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("Inst_Q5_Stage1_ChoicePending", runQ5 != null && runQ5.stage == 1 && runQ5.pendingChoice, true, runQ5 != null ? (runQ5.stage + "/" + runQ5.pendingChoice) : "null");
        Action actChoice5 = ActionOf(LetterFor("TMM_Story_institute_Q5", StoryChoiceLetter.KindChoice), 0); // accept_defectors
        if (actChoice5 != null) actChoice5();
        C("Inst_Q5_Completed", s.IsDone("TMM_Story_institute_Q5"), true, s.IsDone("TMM_Story_institute_Q5"));
        C("Inst_DefectorAlan", s.GetFlag("defector.TMM_Hero_alan_binet") == "true", "true", s.GetFlag("defector.TMM_Hero_alan_binet"));
        C("Inst_DefectorMadison", s.GetFlag("defector.TMM_Hero_madison_li") == "true", "true", s.GetFlag("defector.TMM_Hero_madison_li"));

        // ==========================================
        // 2. MINUTEMEN & HUNTERS FACTION QUEST LINE
        // ==========================================

        s.SetFlag("act", "2");

        // Q1: Salt and Shotgun (HotelVisit -> ColonyRaid -> Choice)
        StoryQuestDef mhQ1 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_minutemen_hunters_Q1");
        C("MH_Q1_Exists", mhQ1 != null, true, mhQ1 != null);
        C("MH_Q1_Available", s.IsAvailable(mhQ1), true, s.IsAvailable(mhQ1));
        s.Offer(mhQ1);
        Action acceptMH1 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q1", StoryChoiceLetter.KindOffer), 0);
        if (acceptMH1 != null) acceptMH1();
        StoryQuestRun runMH1 = s.RunFor("TMM_Story_minutemen_hunters_Q1");
        C("MH_Q1_Active", runMH1 != null && runMH1.status == StoryQuestStatus.Active && runMH1.stage == 0, true, runMH1 != null ? (object)runMH1.status : "null");

        // Stage 0 is HotelVisit
        if (runMH1 != null) runMH1.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("MH_Q1_Stage1_ColonyRaid", runMH1 != null && runMH1.stage == 1, 1, runMH1 != null ? runMH1.stage : -1);
        // Stage 1 is ColonyRaid
        if (runMH1 != null) runMH1.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("MH_Q1_Stage2_ChoicePending", runMH1 != null && runMH1.stage == 2 && runMH1.pendingChoice, true, runMH1 != null ? (runMH1.stage + "/" + runMH1.pendingChoice) : "null");
        Action actMH1 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q1", StoryChoiceLetter.KindChoice), 0); // clean
        if (actMH1 != null) actMH1();
        C("MH_Q1_Completed", s.IsDone("TMM_Story_minutemen_hunters_Q1"), true, s.IsDone("TMM_Story_minutemen_hunters_Q1"));
        C("MH_Q1_FlagClean", s.GetFlag("story.mh_q1") == "clean", "clean", s.GetFlag("story.mh_q1"));

        // Q2: Blood and Moonlight (Site -> Choice)
        StoryQuestDef mhQ2 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_minutemen_hunters_Q2");
        C("MH_Q2_Exists", mhQ2 != null, true, mhQ2 != null);
        C("MH_Q2_Available", s.IsAvailable(mhQ2), true, s.IsAvailable(mhQ2));
        s.Offer(mhQ2);
        Action acceptMH2 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q2", StoryChoiceLetter.KindOffer), 0);
        if (acceptMH2 != null) acceptMH2();
        StoryQuestRun runMH2 = s.RunFor("TMM_Story_minutemen_hunters_Q2");
        C("MH_Q2_Active", runMH2 != null && runMH2.status == StoryQuestStatus.Active && runMH2.stage == 0, true, runMH2 != null ? (object)runMH2.status : "null");

        if (runMH2 != null) runMH2.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("MH_Q2_Stage1_ChoicePending", runMH2 != null && runMH2.stage == 1 && runMH2.pendingChoice, true, runMH2 != null ? (runMH2.stage + "/" + runMH2.pendingChoice) : "null");
        Action actMH2 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q2", StoryChoiceLetter.KindChoice), 0); // cure_alpha
        if (actMH2 != null) actMH2();
        C("MH_Q2_Completed", s.IsDone("TMM_Story_minutemen_hunters_Q2"), true, s.IsDone("TMM_Story_minutemen_hunters_Q2"));

        // Q3: Guns of the Castle (Deliver -> Site -> Choice)
        StoryQuestDef mhQ3 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_minutemen_hunters_Q3");
        C("MH_Q3_Exists", mhQ3 != null, true, mhQ3 != null);
        C("MH_Q3_Available", s.IsAvailable(mhQ3), true, s.IsAvailable(mhQ3));
        s.Offer(mhQ3);
        Action acceptMH3 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q3", StoryChoiceLetter.KindOffer), 0);
        if (acceptMH3 != null) acceptMH3();
        StoryQuestRun runMH3 = s.RunFor("TMM_Story_minutemen_hunters_Q3");
        C("MH_Q3_Active", runMH3 != null && runMH3.status == StoryQuestStatus.Active && runMH3.stage == 0, true, runMH3 != null ? (object)runMH3.status : "null");

        // Stage 0 is Deliver
        if (runMH3 != null) runMH3.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("MH_Q3_Stage1_Site", runMH3 != null && runMH3.stage == 1, 1, runMH3 != null ? runMH3.stage : -1);
        // Stage 1 is Site (Hold)
        if (runMH3 != null) runMH3.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("MH_Q3_Stage2_ChoicePending", runMH3 != null && runMH3.stage == 2 && runMH3.pendingChoice, true, runMH3 != null ? (runMH3.stage + "/" + runMH3.pendingChoice) : "null");
        Action actMH3 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q3", StoryChoiceLetter.KindChoice), 0); // victory
        if (actMH3 != null) actMH3();
        C("MH_Q3_Completed", s.IsDone("TMM_Story_minutemen_hunters_Q3"), true, s.IsDone("TMM_Story_minutemen_hunters_Q3"));
        C("MH_Q3_FlagVictory", s.GetFlag("story.mh_q3") == "victory", "victory", s.GetFlag("story.mh_q3"));

        // Q4 (Faction Point): Night at the Roadhouse (Site -> Choice -> onComplete)
        StoryQuestDef mhQ4 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_minutemen_hunters_Q4");
        C("MH_Q4_Exists", mhQ4 != null, true, mhQ4 != null);
        C("MH_Q4_Available", s.IsAvailable(mhQ4), true, s.IsAvailable(mhQ4));
        s.Offer(mhQ4);
        Action acceptMH4 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q4", StoryChoiceLetter.KindOffer), 0);
        if (acceptMH4 != null) acceptMH4();
        StoryQuestRun runMH4 = s.RunFor("TMM_Story_minutemen_hunters_Q4");
        C("MH_Q4_Active", runMH4 != null && runMH4.status == StoryQuestStatus.Active && runMH4.stage == 0, true, runMH4 != null ? (object)runMH4.status : "null");

        if (runMH4 != null) runMH4.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("MH_Q4_Stage1_ChoicePending", runMH4 != null && runMH4.stage == 1 && runMH4.pendingChoice, true, runMH4 != null ? (runMH4.stage + "/" + runMH4.pendingChoice) : "null");
        Action actMH4 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q4", StoryChoiceLetter.KindChoice), 0); // pro_spark
        if (actMH4 != null) actMH4();
        C("MH_Q4_Completed", s.IsDone("TMM_Story_minutemen_hunters_Q4"), true, s.IsDone("TMM_Story_minutemen_hunters_Q4"));
        C("MH_ChoiceFlagProSpark", s.GetFlag("choice.minutemen_hunters") == "pro_spark", "pro_spark", s.GetFlag("choice.minutemen_hunters"));
        C("MH_ArcFlag4", s.GetFlag("arc.minutemen_hunters") == "4", "4", s.GetFlag("arc.minutemen_hunters"));
        C("ArcPointsTwo", s.GetInt("arcPoints") >= 2, true, s.GetInt("arcPoints"));

        // Q5 (Defector Quest): Cold Pragmatism (Site -> Choice)
        StoryQuestDef mhQ5 = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_minutemen_hunters_Q5");
        C("MH_Q5_Exists", mhQ5 != null, true, mhQ5 != null);
        C("MH_Q5_RequiresAct3", !s.IsAvailable(mhQ5), true, !s.IsAvailable(mhQ5));
        s.SetFlag("act", "3");
        C("MH_Q5_UnlockedInAct3", s.IsAvailable(mhQ5), true, s.IsAvailable(mhQ5));
        s.Offer(mhQ5);
        Action acceptMH5 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q5", StoryChoiceLetter.KindOffer), 0);
        if (acceptMH5 != null) acceptMH5();
        StoryQuestRun runMH5 = s.RunFor("TMM_Story_minutemen_hunters_Q5");
        C("MH_Q5_Active", runMH5 != null && runMH5.status == StoryQuestStatus.Active && runMH5.stage == 0, true, runMH5 != null ? (object)runMH5.status : "null");

        if (runMH5 != null) runMH5.vars["forceOutcome"] = "success";
        s.DirectorTick();
        C("MH_Q5_Stage1_ChoicePending", runMH5 != null && runMH5.stage == 1 && runMH5.pendingChoice, true, runMH5 != null ? (runMH5.stage + "/" + runMH5.pendingChoice) : "null");
        Action actMH5 = ActionOf(LetterFor("TMM_Story_minutemen_hunters_Q5", StoryChoiceLetter.KindChoice), 0); // recruit_rufus
        if (actMH5 != null) actMH5();
        C("MH_Q5_Completed", s.IsDone("TMM_Story_minutemen_hunters_Q5"), true, s.IsDone("TMM_Story_minutemen_hunters_Q5"));
        C("MH_DefectorRufus", s.GetFlag("defector.TMM_Hero_rufus_turner") == "true", "true", s.GetFlag("defector.TMM_Hero_rufus_turner"));

        // Chronicle Lines
        List<string> lines = MainTabWindow_Chronicle.BuildLines(s);
        C("ChronicleHasEntries", lines.Count > 0, true, lines.Count > 0);
    }

    private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    private void Finish(Exception ex)
    {
        done = true;
        bool passed = ex == null && checks.Count > 0 && checks.All(c => c.passed);
        var cj = checks.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}", J(c.name), c.passed ? "true" : "false", J(c.expected), J(c.actual)));
        string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks.Count + ",\n  \"facts\": [" +
            string.Join(",", facts.Select(f => "\"" + J(f) + "\"").ToArray()) + "],\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
            "\n  ],\n  \"errors\": [" + (ex == null ? "" : "\"" + J(ex.ToString()) + "\"") + "]\n}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "StoryContentProbe.json"), json);
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "StoryContentUiProbe.json"), "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":false}");
    }
}
