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
        if (DefDatabase<HeroDef>.GetNamedSilentFail(name) != null) return;
        var d = new HeroDef
        {
            defName = name, label = name, factionDef = TestFaction, rank = rank, isLeader = leader,
            pawnKind = PawnKindDefOf.Villager, nameFirst = first, nameNick = first, nameLast = "Probe",
            gender = Gender.Male, ageRange = new FloatRange(30f, 40f),
            skills = new List<HeroSkillMin> { new HeroSkillMin { skill = SkillDefOf.Shooting, level = shooting } }
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
    private Pawn leader, second, third;

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
            C("AbsentFactionGivesNull", reg.GetOrCreate(preston) == null, "null", "checked");
            C("AbsentFactionCreatesNoRecord", reg.RecordFor(preston) == null, "no record", reg.RecordFor(preston) == null ? "no record" : "record");

            leader = reg.GetOrCreate(H("TMMTest_Leader"));
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
            C("ThreeRecords", reg.Records.Count() == 3, 3, reg.Records.Count());
            int distinct = new[] { leader, second, third }.Where(p => p != null).Select(p => p.thingIDNumber).Distinct().Count();
            C("ThreeDistinctPawns", distinct == 3, 3, distinct);

            // Prisoner of the colony: spawn the third hero on the home map and capture it.
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 8);
            if (Find.WorldPawns.Contains(third)) Find.WorldPawns.RemovePawn(third);
            GenSpawn.Spawn(third, cell, map);
            third.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);

            // Kill the leader (world pawn, not spawned).
            leader.Kill(null);
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

            ForceWorldPawnGC();
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
            C("RegistryLoaded", reg != null && reg.Records.Count() == 3, 3, reg == null ? -1 : reg.Records.Count());
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
            stage = 1; waitUntil = now + 60000; // one real in-game day with normal world pawn GC
            return;
        }
        if (stage == 1)
        {
            HeroRecord rs = reg.RecordFor(H("TMMTest_Second"));
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
