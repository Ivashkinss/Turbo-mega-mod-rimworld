using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using Verse;

// In-game acceptance of HeroRegistry stage 1 (Documentation/HERO-REGISTRY-DESIGN.md).
// Fresh: -tmm-hero-registry-probe. Reload: -tmm-hero-registry-reload (loads save TMM-HeroRegistry).
[StaticConstructorOnStartup]
public static class HeroRegistryProbeLaunch
{
    public const string TestFaction = "OutlanderCivil";

    static HeroRegistryProbeLaunch()
    {
        // Test heroes bound to a vanilla faction, registered before any world loads so reload resolves them.
        AddHero("TMMTest_Leader", 4, true, "Lead", 12);
        AddHero("TMMTest_Second", 3, false, "Second", 11);
        AddHero("TMMTest_Third", 2, false, "Third", 10);
        AddHero("TMMTest_Fourth", 1, false, "Fourth", 8);
        AddHero("TMMTest_Fifth", 1, false, "Fifth", 8);
        AddHero("TMMTest_Sixth", 1, false, "Sixth", 8);
        AddHeroWithFaction("TMMTest_AbsentFactionHero", "TMM_NoSuchFaction", 1, false, "Ghost", 5);
        DefDatabase<HeroDef>.GetNamed("TMMTest_Leader").successor = DefDatabase<HeroDef>.GetNamed("TMMTest_Second");

        string[] args = Environment.GetCommandLineArgs();
        if (args.Contains("-tmm-hero-registry-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-HeroRegistry"));
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps == null) return;
            Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
            if (home != null && home.GetComponent<HeroRegistryProbe>() == null) home.components.Add(new HeroRegistryProbe(home));
        });
    }

    private static void AddHero(string name, int rank, bool leader, string first, int shooting)
    {
        AddHeroWithFaction(name, TestFaction, rank, leader, first, shooting);
    }

    private static void AddHeroWithFaction(string name, string factionDef, int rank, bool leader, string first, int shooting)
    {
        if (DefDatabase<HeroDef>.GetNamedSilentFail(name) != null) return;
        SkillDef shootDef = DefDatabase<SkillDef>.GetNamedSilentFail("Shooting") ?? SkillDefOf.Shooting;
        var d = new HeroDef
        {
            defName = name, label = name, factionDef = factionDef, rank = rank, isLeader = leader,
            pawnKind = PawnKindDefOf.Villager,
            nameFirst = first, nameNick = first, nameLast = "Probe",
            gender = Gender.Male, ageRange = new FloatRange(30f, 40f),
            skills = new List<HeroSkillMin> { new HeroSkillMin { skill = shootDef, level = shooting } }
        };
        DefDatabase<HeroDef>.Add(d);
    }
}

public sealed class HeroRegistryProbe : MapComponent
{
    private sealed class Check { public string name, expected, actual; public bool passed; }

    private readonly List<Check> checks = new List<Check>();
    private readonly List<string> facts = new List<string>();
    private bool done;
    private int stage;
    private int waitUntil = -1;
    private Pawn leader, second, third, fourth, fifth, sixth;
    private ActiveTransporterInfo flight;

    public HeroRegistryProbe(Map map) : base(map) { }

    private void C(string name, bool ok, object expected, object actual)
    {
        checks.Add(new Check { name = name, passed = ok, expected = Convert.ToString(expected), actual = Convert.ToString(actual) });
    }

    private static HeroDef H(string n) { return DefDatabase<HeroDef>.GetNamed(n); }

    private static Faction TestFaction()
    {
        FactionDef fd = DefDatabase<FactionDef>.GetNamedSilentFail(HeroRegistryProbeLaunch.TestFaction);
        return fd == null ? null : Find.FactionManager.FirstFactionOfDef(fd);
    }

    private static string CriticalReasonOf(Pawn p)
    {
        var m = AccessTools.Method(typeof(WorldPawnGC), "GetCriticalPawnReason");
        return m == null ? "method missing" : (string)m.Invoke(Find.WorldPawns.gc, new object[] { p });
    }

    private static void ForceWorldPawnGC()
    {
        var gc = Find.WorldPawns.gc;
        var m = AccessTools.Method(typeof(WorldPawnGC), "RunGC");
        if (m != null) m.Invoke(gc, null);
        else throw new Exception("WorldPawnGC.RunGC not found");
    }

    public override void MapComponentUpdate()
    {
        string[] args = Environment.GetCommandLineArgs();
        bool reload = args.Contains("-tmm-hero-registry-reload");
        bool fresh = args.Contains("-tmm-hero-registry-probe");
        if (done || !map.IsPlayerHome || (!reload && !fresh)) return;
        if (Find.TickManager.Paused || Find.TickManager.CurTimeSpeed != TimeSpeed.Ultrafast) Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
        int now = Find.TickManager.TicksGame;
        if (waitUntil >= 0 && now < waitUntil) return;
        try
        {
            if (reload) Reload(now); else Fresh(now);
        }
        catch (Exception ex) { Finish(ex, reload); }
    }

    private void Fresh(int now)
    {
        HeroRegistry reg = HeroRegistry.Instance;
        if (stage == 0)
        {
            C("RegistryComponentExists", reg != null, true, reg != null);
            Faction f = TestFaction();
            C("TestFactionPresent", f != null, HeroRegistryProbeLaunch.TestFaction, f);
            HeroDef preston = DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_preston_garvey");
            C("MinutemenHeroesLoadedFromXml", preston != null && DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_sam_winchester") != null, true, preston != null);

            HeroDef absentHero = DefDatabase<HeroDef>.GetNamedSilentFail("TMMTest_AbsentFactionHero");
            C("AbsentFactionGivesNull", reg.GetOrCreate(absentHero) == null, "null", "checked");
            C("AbsentFactionCreatesNoRecord", reg.RecordFor(absentHero) == null, "no record", reg.RecordFor(absentHero) == null ? "no record" : "record");

            Pawn prestonPawn = reg.GetOrCreate(preston);
            Faction minutemenFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamedSilentFail("TMM_MinutemenHuntersFaction"));
            C("PrestonCreated", prestonPawn != null, "pawn", prestonPawn);
            C("PrestonFactionMatches", prestonPawn != null && minutemenFaction != null && prestonPawn.Faction == minutemenFaction, minutemenFaction != null ? minutemenFaction.Name : "null", prestonPawn != null && prestonPawn.Faction != null ? prestonPawn.Faction.Name : "null");
            C("PrestonIsFactionLeader", minutemenFaction != null && minutemenFaction.leader == prestonPawn, prestonPawn, minutemenFaction != null ? minutemenFaction.leader : null);

            HeroDef leadDef = H("TMMTest_Leader");
            if (leadDef != null && leadDef.skills != null && leadDef.skills.Count > 0 && leadDef.skills[0].skill == null)
                leadDef.skills[0].skill = DefDatabase<SkillDef>.GetNamedSilentFail("Shooting") ?? SkillDefOf.Shooting;

            leader = reg.GetOrCreate(leadDef);
            Pawn again = reg.GetOrCreate(H("TMMTest_Leader"));
            C("LeaderCreated", leader != null, "pawn", leader);
            C("SecondCallSamePawn", leader != null && ReferenceEquals(leader, again), leader == null ? "" : leader.ThingID, again == null ? "null" : again.ThingID);
            C("NameApplied", leader != null && leader.Name.ToStringFull.Contains("Lead") && leader.Name.ToStringFull.Contains("Probe"), "Lead ... Probe", leader == null ? "" : leader.Name.ToStringFull);
            int shoot = leader == null ? -1 : leader.skills.GetSkill(SkillDefOf.Shooting).Level;
            C("SkillMinimumApplied", shoot >= 12, ">=12", shoot);
            C("LeaderInFactionAndWorld", leader != null && leader.Faction == f && Find.WorldPawns.Contains(leader), "faction+world", leader == null ? "" : leader.Faction + "/" + Find.WorldPawns.Contains(leader));
            C("FactionLeaderIsHero", f != null && f.leader == leader, leader, f == null ? null : f.leader);

            second = reg.GetOrCreate(H("TMMTest_Second"));
            third = reg.GetOrCreate(H("TMMTest_Third"));
            fourth = reg.GetOrCreate(H("TMMTest_Fourth"));
            fifth = reg.GetOrCreate(H("TMMTest_Fifth"));
            sixth = reg.GetOrCreate(H("TMMTest_Sixth"));
            C("SevenRecords", reg.Records.Count() == 7, 7, reg.Records.Count());
            int distinct = new[] { leader, second, third, fourth, fifth }.Where(p => p != null).Select(p => p.thingIDNumber).Distinct().Count();
            C("FiveDistinctPawns", distinct == 5, 5, distinct);
            // F3: fourth is a plain world pawn with no vanilla reason to keep it; only the registry postfix protects it.
            C("FourthNotForceKept", fourth != null && !Find.WorldPawns.ForcefullyKeptPawns.Contains(fourth), "not force-kept", fourth == null ? "null" : Find.WorldPawns.ForcefullyKeptPawns.Contains(fourth).ToString());
            string reason = CriticalReasonOf(fourth);
            C("FourthCriticalReasonIsRegistry", reason == "TMM named hero", "TMM named hero", reason ?? "null");
            // F1: fifth goes into a cryptosleep casket on the player's map: held, not spawned, not at home.
            ThingDef casketDef = DefDatabase<ThingDef>.GetNamed("CryptosleepCasket");
            IntVec3 cc = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            var casket = (Building_CryptosleepCasket)GenSpawn.Spawn(ThingMaker.MakeThing(casketDef, GenStuff.DefaultStuffFor(casketDef)), cc, map);
            if (Find.WorldPawns.Contains(fifth)) Find.WorldPawns.RemovePawn(fifth);
            C("FifthAcceptedByCasket", casket.TryAcceptThing(fifth, false), true, fifth.ParentHolder);
            // F1 (flight): sixth stays a world pawn but sits inside a transporter holder, as in a pod in flight.
            flight = new ActiveTransporterInfo();
            bool boarded = flight.innerContainer.TryAdd(sixth);
            C("SixthInFlightHolder", boarded && Find.WorldPawns.Contains(sixth) && sixth.MapHeld == null && sixth.ParentHolder != null, "world pawn, held, no map", boarded + "/" + Find.WorldPawns.Contains(sixth) + "/" + (sixth.ParentHolder != null));
            // N1: an explicit Discard of a living hero must not destroy it.
            Find.WorldPawns.RemovePawn(fourth);
            Find.WorldPawns.PassToWorld(fourth, PawnDiscardDecideMode.Discard);
            C("ExplicitDiscardKeepsHero", !fourth.Discarded && !fourth.Destroyed && Find.WorldPawns.Contains(fourth), "kept", fourth.Discarded ? "discarded" : (Find.WorldPawns.Contains(fourth) ? "kept" : "not world pawn"));

            // Prisoner of the colony: spawn the third hero on the home map and capture it.
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 8);
            if (Find.WorldPawns.Contains(third)) Find.WorldPawns.RemovePawn(third);
            GenSpawn.Spawn(third, cell, map);
            third.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);

            // Kill the leader (world pawn, not spawned).
            leader.Kill(null);
            // F2: vanilla Notify_LeaderDied ran synchronously inside Kill; the successor must already be leader,
            // before any polling, so the vanilla letter and quest signal name the hero.
            Faction ff = TestFaction();
            C("SuccessorChosenSynchronously", ff != null && ff.leader == second, second, ff == null ? null : ff.leader);
            // N2: the leader-death letter must exist and name the hero successor.
            string ltText = "";
            foreach (Letter lt in Find.LetterStack.LettersListForReading)
            {
                ChoiceLetter cl = lt as ChoiceLetter;
                if (cl == null) continue;
                string s = cl.Label.ToString() + " " + cl.Text.ToString();
                if (s.Contains("Lead Probe")) ltText = s;
            }
            facts.Add("leaderLetter=" + ltText.Replace("\n", " "));
            C("LeaderLetterNamesSuccessor", ltText.Length > 0 && ltText.Contains("Second Probe"), "letter about Lead Probe naming Second Probe", ltText.Length == 0 ? "no letter" : (ltText.Length > 120 ? ltText.Substring(0, 120) : ltText));
            stage = 1; waitUntil = now + 600;
            return;
        }
        if (stage == 1)
        {
            Faction f = TestFaction();
            HeroRecord rl = reg.RecordFor(H("TMMTest_Leader"));
            C("DeadLeaderStateDead", rl != null && rl.state == HeroState.Dead, "Dead", rl == null ? "none" : rl.state.ToString());
            C("DeadLeaderDeathTick", rl != null && rl.deathTick > 0, ">0", rl == null ? -1 : rl.deathTick);
            C("DeadHeroNotResurrected", reg.GetOrCreate(H("TMMTest_Leader")) == null, "null", "checked");
            C("SuccessorBecameLeader", f != null && f.leader == second, second, f == null ? null : f.leader);
            HeroRecord rt = reg.RecordFor(H("TMMTest_Third"));
            C("CapturedHeroStateCaptive", rt != null && rt.state == HeroState.Captive, "Captive", rt == null ? "none" : rt.state.ToString());
            C("CapturedHeroSamePawn", ReferenceEquals(reg.GetOrCreate(H("TMMTest_Third")), third), third.ThingID, "checked");

            HeroRecord r5 = reg.RecordFor(H("TMMTest_Fifth"));
            C("CasketHeroStateAway", r5 != null && r5.state == HeroState.Away, "Away", r5 == null ? "none" : r5.state.ToString());
            HeroRecord r6 = reg.RecordFor(H("TMMTest_Sixth"));
            C("FlightHeroStateAway", r6 != null && r6.state == HeroState.Away, "Away", r6 == null ? "none" : r6.state.ToString());
            flight.innerContainer.Remove(sixth); // land the test pod: back to a plain world pawn
            HeroRecord r4d = reg.RecordFor(H("TMMTest_Fourth"));
            C("DiscardedHeroNotMissing", r4d != null && r4d.state != HeroState.Missing && r4d.pawn == fourth, "alive record", r4d == null ? "none" : r4d.state.ToString());
            ForceWorldPawnGC();
            C("FourthSurvivesGCOnlyByPostfix", !fourth.Discarded && Find.WorldPawns.Contains(fourth), "kept", fourth.Discarded ? "discarded" : (Find.WorldPawns.Contains(fourth) ? "kept" : "not world pawn"));
            C("SecondSurvivesGC", !second.Discarded && Find.WorldPawns.Contains(second), "kept", second.Discarded ? "discarded" : (Find.WorldPawns.Contains(second) ? "kept" : "not world pawn"));
            facts.Add("secondId=" + second.ThingID);
            facts.Add("thirdId=" + third.ThingID);
            GameDataSaveLoader.SaveGame("TMM-HeroRegistry");
            C("SaveWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-HeroRegistry.rws")), true, "checked");
            Finish(null, false);
        }
    }

    private string savedSecondId;

    private void Reload(int now)
    {
        HeroRegistry reg = HeroRegistry.Instance;
        if (stage == 0)
        {
            Faction f = TestFaction();
            C("RegistryLoaded", reg != null && reg.Records.Count() == 7, 7, reg == null ? -1 : reg.Records.Count());
            HeroRecord rl = reg.RecordFor(H("TMMTest_Leader"));
            HeroRecord rs = reg.RecordFor(H("TMMTest_Second"));
            HeroRecord rt = reg.RecordFor(H("TMMTest_Third"));
            C("LeaderStillDead", rl != null && rl.state == HeroState.Dead, "Dead", rl == null ? "none" : rl.state.ToString());
            C("NoResurrectionAfterLoad", reg.GetOrCreate(H("TMMTest_Leader")) == null, "null", "checked");
            C("SecondAtHome", rs != null && rs.state == HeroState.AtHome && rs.pawn != null, "AtHome", rs == null ? "none" : rs.state.ToString());
            C("ThirdStillCaptive", rt != null && rt.state == HeroState.Captive && rt.pawn != null && rt.pawn.IsPrisonerOfColony, "Captive", rt == null ? "none" : rt.state.ToString());
            C("FactionLeaderStillSecond", f != null && rs != null && f.leader == rs.pawn, rs == null ? null : rs.pawn, f == null ? null : f.leader);
            Pawn again = reg.GetOrCreate(H("TMMTest_Second"));
            C("SameSecondAfterLoad", rs != null && ReferenceEquals(again, rs.pawn), rs == null || rs.pawn == null ? "" : rs.pawn.ThingID, again == null ? "null" : again.ThingID);
            savedSecondId = rs == null || rs.pawn == null ? null : rs.pawn.ThingID;
            ForceWorldPawnGC();
            stage = 1; waitUntil = now + 120000; // two real in-game days with normal world pawn GC
            return;
        }
        if (stage == 1)
        {
            HeroRecord rs = reg.RecordFor(H("TMMTest_Second"));
            HeroRecord r4 = reg.RecordFor(H("TMMTest_Fourth"));
            C("FourthSurvivesTwoDays", r4 != null && r4.pawn != null && !r4.pawn.Discarded, "alive record", r4 == null || r4.pawn == null ? "null" : r4.pawn.ThingID);
            C("SecondSurvivesDayAndGC", rs != null && rs.pawn != null && !rs.pawn.Discarded && rs.pawn.ThingID == savedSecondId, savedSecondId, rs == null || rs.pawn == null ? "null" : rs.pawn.ThingID);
            C("NoDuplicateRecords", reg.Records.Select(r => r.heroDefName).Distinct().Count() == reg.Records.Count(), "distinct", reg.Records.Count());
            Finish(null, true);
        }
    }

    private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    private void Finish(Exception ex, bool reload)
    {
        done = true;
        bool passed = ex == null && checks.Count > 0 && checks.All(c => c.passed);
        var cj = checks.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}", J(c.name), c.passed ? "true" : "false", J(c.expected), J(c.actual)));
        string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks.Count + ",\n  \"stage\": " + stage +
            ",\n  \"facts\": [" + string.Join(",", facts.Select(f => "\"" + J(f) + "\"").ToArray()) + "],\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
            "\n  ],\n  \"errors\": [" + (ex == null ? "" : "\"" + J(ex.ToString()) + "\"") + "]\n}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, reload ? "HeroRegistryReloadProbe.json" : "HeroRegistryProbe.json"), json);
        if (!reload)
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HeroRegistryUiProbe.json"),
                "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":false,\"note\":\"no UI in stage 1\"}");
    }
}
