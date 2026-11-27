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
        HeroDef fourthDef = DefDatabase<HeroDef>.GetNamedSilentFail("TMMTest_Fourth");
        if (fourthDef != null)
        {
            fourthDef.appearance = new HeroAppearance
            {
                hair = DefDatabase<HairDef>.GetNamedSilentFail("Shaved") ?? DefDatabase<HairDef>.AllDefs.First()
            };
        }

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
    private int dialogsClosed;
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

    private static bool crowleyBs(HeroRegistry reg) { Pawn p = reg.GetOrCreate(H("TMM_Hero_crowley")); return p != null && p.story.Childhood != null && p.story.Childhood.defName == "TMM_BS_crowley_child" && p.story.Adulthood != null && p.story.Adulthood.defName == "TMM_BS_crowley_adult"; }
    private static string crowleyBsText(HeroRegistry reg) { Pawn p = reg.GetOrCreate(H("TMM_Hero_crowley")); return p == null ? "null" : (p.story.Childhood == null ? "none" : p.story.Childhood.defName) + "/" + (p.story.Adulthood == null ? "none" : p.story.Adulthood.defName) + " | " + p.story.Adulthood.TitleCapFor(p.gender); }
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
        // During the two-day wait a random event can open a force-pause dialog (quest offer, ideology prompt),
        // which stops ticking and made the reload run time out. Close such dialogs in the test only.
        foreach (Window w in Find.WindowStack.Windows.ToList())
            if (w.forcePause) { w.Close(false); dialogsClosed++; }
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
            // Hell heroes: seven unique demon-xenotype heroes; Crowley leads; Azazel keeps his yellow eyes.
            FactionDef hellDef = DefDatabase<FactionDef>.GetNamedSilentFail("TMM_HellFaction");
            Faction hell = hellDef == null ? null : Find.FactionManager.FirstFactionOfDef(hellDef);
            C("HellFactionPresent", hell != null, "TMM_HellFaction", hell);
            C("HellFactionFixedName", hell != null && (hell.Name == "Демоны Ада" || hell.Name == "Demons of Hell"), "Демоны Ада", hell == null ? "null" : hell.Name);
            foreach (string hn in new[] { "crowley", "azazel", "lilith", "abaddon", "cain", "meg", "ruby" })
            {
                Pawn hp = reg.GetOrCreate(H("TMM_Hero_" + hn));
                bool ok = hp != null && hp.Faction == hell && hp.genes != null && hp.genes.Xenotype != null && hp.genes.Xenotype.defName == "TMM_Demon";
                C("HellHero_" + hn, ok, "demon of Hell", hp == null ? "null" : (hp.Faction + "/" + (hp.genes == null || hp.genes.Xenotype == null ? "no xeno" : hp.genes.Xenotype.defName)));
            }
            Pawn crowley = reg.GetOrCreate(H("TMM_Hero_crowley"));
            C("CrowleyBackstories", crowleyBs(reg), "TMM_BS_crowley_child/TMM_BS_crowley_adult", crowleyBsText(reg));
            FactionDef heavenDef = DefDatabase<FactionDef>.GetNamedSilentFail("TMM_HeavenFaction");
            Faction heaven = heavenDef == null ? null : Find.FactionManager.FirstFactionOfDef(heavenDef);
            C("HeavenFactionPresent", heaven != null && !heaven.def.permanentEnemy, "TMM_HeavenFaction, not permanent enemy", heaven);
            foreach (string an in new[] { "michael", "lucifer", "castiel", "gabriel", "zachariah", "naomi" })
            {
                Pawn ap = reg.GetOrCreate(H("TMM_Hero_" + an));
                bool aok = ap != null && ap.Faction == heaven && ap.genes != null && ap.genes.Xenotype != null && ap.genes.Xenotype.defName == "TMM_Angel" && ap.story.Adulthood != null && ap.story.Adulthood.defName == "TMM_BS_" + an + "_adult";
                C("HeavenHero_" + an, aok, "angel of Heaven with own backstory", ap == null ? "null" : (ap.Faction + "/" + (ap.genes == null || ap.genes.Xenotype == null ? "no xeno" : ap.genes.Xenotype.defName) + "/" + (ap.story.Adulthood == null ? "none" : ap.story.Adulthood.defName)));
            }
            Pawn michael = reg.GetOrCreate(H("TMM_Hero_michael"));
            C("MichaelLeadsHeaven", heaven != null && michael != null && heaven.leader == michael, "Michael", heaven == null ? null : heaven.leader);            // Further groups: Minutemen veterans (humans), friendly and wild super mutants (giant xenotype).
            var groups = new[] {
                new { fac = "TMM_MinutemenHuntersFaction", xeno = (string)null, ids = new[] { "bobby_singer", "ronnie_shaw", "rufus_turner", "ellen_harvelle" } },
                new { fac = "TMM_FriendlyMutantsFaction", xeno = "TMM_SuperMutant", ids = new[] { "fox", "uncle_leo", "strong", "erickson", "virgil" } },
                new { fac = "TMM_BrotherhoodFaction", xeno = (string)null, ids = new[] { "arthur_maxson", "owen_lyons", "sarah_lyons", "paladin_danse", "captain_kells", "proctor_ingram" } },
                new { fac = "TMM_GunnersFaction", xeno = (string)null, ids = new[] { "captain_wes", "clint", "tessa", "captain_bridget", "baker" } },
                new { fac = "TMM_EnclaveFaction", xeno = (string)null, ids = new[] { "colonel_autumn", "dr_curling", "sergeant_dornan", "captain_richards" } },
                new { fac = "TMM_InstituteFaction", xeno = (string)null, ids = new[] { "father_shaun", "justin_ayo", "x6_88", "madison_li", "kellogg", "alan_binet" } },
                new { fac = "TMM_RailroadFaction", xeno = (string)null, ids = new[] { "desdemona", "deacon", "tinker_tom", "glory" } },                new { fac = "TMM_VaultTecFaction", xeno = (string)null, ids = new[] { "alphonse_almodovar", "gwen_mcnamara", "stanislaus_braun", "vaulttec_rep", "security_chief_kendall" } },
                new { fac = "TMM_MenOfLettersFaction", xeno = (string)null, ids = new[] { "dr_hess", "arthur_ketch", "mick_davies", "antonia_bevell", "kendricks" } },
                new { fac = "TMM_GhoulsFaction", xeno = "TMM_Ghoul", ids = new[] { "john_hancock", "kent_connolly", "charon", "edward_deegan", "daisy", "gob" } },
                new { fac = "TMM_MonsterClansFaction", xeno = (string)null, ids = new[] { "alpha_vampire", "dick_roman", "eve", "benny_lafitte", "edgar" } },
                new { fac = "TMM_WildMutantsFaction", xeno = "TMM_SuperMutant", ids = new[] { "swan", "fist", "dead_eye", "mad_racer", "master_butcher" } },
                new { fac = "TMM_OutcastsFaction", xeno = (string)null, ids = new[] { "henry_casdin", "specialist_olin", "scribe_silas", "defender_morrigan", "paladin_vargas", "scout_kessler" } } };
            foreach (var grp in groups)
            {
                FactionDef gd = DefDatabase<FactionDef>.GetNamedSilentFail(grp.fac);
                Faction gf = gd == null ? null : Find.FactionManager.FirstFactionOfDef(gd);
                C("FactionPresent_" + grp.fac, gf != null, grp.fac, gf);
                foreach (string gid in grp.ids)
                {
                    Pawn gp = reg.GetOrCreate(H("TMM_Hero_" + gid));
                    string xn = gp == null || gp.genes == null || gp.genes.Xenotype == null ? "none" : gp.genes.Xenotype.defName;
                    string wantX = grp.xeno ?? H("TMM_Hero_" + gid).xenotype;
                    bool xok = wantX.NullOrEmpty() || xn == wantX;
                    bool bs = gp != null && gp.story.Adulthood != null && gp.story.Adulthood.defName == "TMM_BS_" + gid + "_adult";
                    C("Hero_" + gid, gp != null && gp.Faction == gf && xok && bs, grp.fac + "/" + (grp.xeno ?? "any") + "/own backstory", gp == null ? "null" : gp.Faction + "/" + xn + "/" + bs);
                    if (grp.xeno == "TMM_SuperMutant" && gp != null)
                        C("Giant_" + gid, Math.Abs(GiantDrawScale.FactorFor(gp) - 2.2f) < 0.01f && gp.story.headType != null && gp.story.headType.defName == "TMM_SuperMutantHead", "scale 2.2, mutant head", GiantDrawScale.FactorFor(gp) + "/" + (gp.story.headType == null ? "null" : gp.story.headType.defName));
                }
            }
            Pawn foxP = reg.GetOrCreate(H("TMM_Hero_fox")); Pawn swanP = reg.GetOrCreate(H("TMM_Hero_swan"));
            C("FoxLeadsFriendlyMutants", foxP != null && foxP.Faction != null && foxP.Faction.leader == foxP, "Fox", foxP == null || foxP.Faction == null ? null : foxP.Faction.leader);
            C("SwanLeadsWildMutants", swanP != null && swanP.Faction != null && swanP.Faction.leader == swanP, "Swan", swanP == null || swanP.Faction == null ? null : swanP.Faction.leader);            Pawn vtLeader = null; { FactionDef vd = DefDatabase<FactionDef>.GetNamedSilentFail("TMM_VaultTecFaction"); Faction vf = vd == null ? null : Find.FactionManager.FirstFactionOfDef(vd); vtLeader = vf == null ? null : vf.leader; }
            C("VaultTecCouncilMemberLeads", vtLeader != null && reg.Records.Any(r => r.pawn == vtLeader && DefDatabase<HeroDef>.GetNamedSilentFail(r.heroDefName) != null && DefDatabase<HeroDef>.GetNamedSilentFail(r.heroDefName).councilMember), "a council member", vtLeader);
            C("CrowleyIsKingOfHell", hell != null && crowley != null && hell.leader == crowley, "Crowley", hell == null ? null : hell.leader);
            Pawn azazel = reg.GetOrCreate(H("TMM_Hero_azazel"));
            bool yellow = azazel != null && azazel.genes.GenesListForReading.Any(x => x.def.defName == "TMM_Eyes_Devil" && x.Active);
            bool blackOff = azazel != null && !azazel.genes.GenesListForReading.Any(x => x.def.defName == "TMM_Eyes_DemonBlack" && x.Active);
            C("AzazelYellowEyes", yellow && blackOff, "devil eyes active, black eyes overridden", yellow + "/" + blackOff);

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
            C("AllHeroRecords", reg.Records.Count() == 86, 86, reg.Records.Count());
            // Synth heroes (Neon 57): Nick Valentine is a Gen 2 synth with his own drawn head and body.
            Pawn nick = reg.GetOrCreate(H("TMM_Hero_nick_valentine"));
            string nickXeno = nick == null || nick.genes == null || nick.genes.Xenotype == null ? "none" : nick.genes.Xenotype.defName;
            string nickHead = nick == null || nick.story == null || nick.story.headType == null ? "none" : nick.story.headType.defName;
            string nickBody = nick == null || nick.story == null || nick.story.bodyType == null ? "none" : nick.story.bodyType.defName;
            C("SynthNickValentineModel", nickXeno == "TMM_SynthGen2" && nickHead == "TMM_NickValentineHead" && nickBody == "TMM_NickValentine",
                "TMM_SynthGen2/TMM_NickValentineHead/TMM_NickValentine", nickXeno + "/" + nickHead + "/" + nickBody);
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
            IntVec3 cell;
            // The map centre can be solid rock: then the prisoner stood in a cell without a region and the game's own jobs failed on it.
            IntVec3 free = map.mapPawns.FreeColonistsSpawned[0].Position;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 12, c => c.Standable(map) && c.GetDistrict(map) != null && map.reachability.CanReach(free, c, Verse.AI.PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)), out cell)) cell = CellFinder.RandomClosewalkCellNear(free, map, 6);
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
            // World generation in this mod set can leave relations of random world pawns pointing at discarded pawns.
            // Remove those before saving (they are not ours), but fail if a hero or a player pawn has one.
            // The mod itself now drops relations to pawns the GC removed before every save (SaveHygiene); after it
            // no hero or player pawn may keep one. The reload test checks the save has no unresolved references.
            facts.Add("saveHygieneRemoved=" + SaveHygiene.CleanDanglingRelations());
            // A generation attempt that the game drops halfway must not stay a relative of anybody (it is never saved).
            int dropped = SaveHygiene.DroppedAttempts, made = 0;
            while (made < 1200 && SaveHygiene.DroppedAttempts - dropped < 3)
            {
                Pawn soldier = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.AncientSoldier, Faction.OfAncientsHostile, forceGenerateNewPawn: true));
                made++;
                Find.WorldPawns.PassToWorld(soldier, PawnDiscardDecideMode.Discard);
            }
            List<string> lost = new List<string>();
            foreach (Pawn p in Find.WorldPawns.AllPawnsAliveOrDead.Concat(map.mapPawns.AllPawns).Distinct())
                if (p.relations != null)
                    foreach (DirectPawnRelation rel in p.relations.DirectRelations)
                        if (rel.otherPawn != null && !rel.otherPawn.Spawned && rel.otherPawn.ParentHolder == null && !Find.WorldPawns.Contains(rel.otherPawn)) lost.Add(p.ThingID + "->" + rel.otherPawn.ThingID + " " + rel.otherPawn.kindDef.defName);
            C("DroppedAttemptsAreDiscarded", SaveHygiene.DroppedAttempts - dropped >= 1 && lost.Count == 0, "some dropped, no relation to a lost pawn", "dropped=" + (SaveHygiene.DroppedAttempts - dropped) + " of " + made + " since start=" + SaveHygiene.DroppedAttempts + " lost=" + string.Join(", ", lost.Take(5).ToArray()));
            HeroDef deanDef = DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_dean_winchester");
            if (deanDef != null && deanDef.appearance != null)
            {
                // A throwaway pawn: the check must not add a registry record (counts are verified after reload).
                Pawn dean = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, null, PawnGenerationContext.NonPlayer, -1, true, false, false, false, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, Gender.Male));
                deanDef.appearance.ApplyTo(dean);
                C("HeroAppearanceApplied", dean != null && dean.story != null && dean.story.hairDef == deanDef.appearance.hair && dean.style != null && dean.style.beardDef == deanDef.appearance.beard,
                    "hair=" + deanDef.appearance.hair + ", beard=" + deanDef.appearance.beard,
                    dean == null || dean.story == null ? "null" : ("hair=" + dean.story.hairDef + ", beard=" + (dean.style != null ? dean.style.beardDef.ToStringSafe() : "-")));
            }
            // Player customizes fourth hero: haircut, custom rename, and custom weapon.
            HairDef shaved = DefDatabase<HairDef>.GetNamedSilentFail("Shaved") ?? DefDatabase<HairDef>.AllDefs.First();
            HairDef customHair = DefDatabase<HairDef>.GetNamedSilentFail("Messy") ?? DefDatabase<HairDef>.AllDefs.FirstOrDefault(h => h != shaved) ?? shaved;
            if (fourth != null && fourth.story != null) fourth.story.hairDef = customHair;
            if (fourth != null) fourth.Name = new NameTriple("CustomFirst", "PlayerNick", "CustomLast");
            if (fourth != null && fourth.equipment != null)
            {
                fourth.equipment.DestroyAllEquipment();
                ThingWithComps customGun = (ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Gun_Autopistol"));
                fourth.equipment.AddEquipment(customGun);
            }

            // Old-save migration test scenario (SaveHygieneFix2):
            // 1. Fourth hero (with player custom name and haircut) has recordVersion, lastAutoName, appearanceApplied zeroed out.
            HeroRecord r4rec = reg.RecordFor(H("TMMTest_Fourth"));
            if (r4rec != null)
            {
                r4rec.recordVersion = 0;
                r4rec.lastAutoName = null;
                r4rec.appearanceApplied = false;
            }

            // 2. Arthur Maxson has English auto-name forced and migration fields zeroed out to simulate an old English save.
            HeroRecord rMaxson = reg.RecordFor(H("TMM_Hero_arthur_maxson"));
            if (rMaxson != null && rMaxson.pawn != null)
            {
                rMaxson.pawn.Name = new NameTriple("Arthur", "Maxson", "Maxson");
                rMaxson.recordVersion = 0;
                rMaxson.lastAutoName = null;
                rMaxson.appearanceApplied = false;
                // An old save has no signatureGearApplied either; the player gave Maxson a plain autopistol.
                rMaxson.signatureGearApplied = false;
                if (rMaxson.pawn.equipment != null)
                {
                    rMaxson.pawn.equipment.DestroyAllEquipment();
                    rMaxson.pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Gun_Autopistol")));
                }
            }

            facts.Add("saveHygieneRemoved=" + SaveHygiene.CleanDanglingRelations());
            int ownDangling = CleanDanglingRelations(reg);
            C("NoDanglingRelationsOnHeroesOrPlayer", ownDangling == 0, 0, ownDangling + " " + string.Join("; ", ownDanglingNames.ToArray()));
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
            C("RegistryLoaded", reg != null && reg.Records.Count() == 87, 87, reg == null ? -1 : reg.Records.Count());
            HeroRecord rl = reg.RecordFor(H("TMMTest_Leader"));
            HeroRecord rs = reg.RecordFor(H("TMMTest_Second"));
            HeroRecord rt = reg.RecordFor(H("TMMTest_Third"));
            HeroRecord r4 = reg.RecordFor(H("TMMTest_Fourth"));
            HairDef shaved = DefDatabase<HairDef>.GetNamedSilentFail("Shaved") ?? DefDatabase<HairDef>.AllDefs.First();
            bool hairOk = r4 != null && r4.pawn != null && r4.pawn.story != null && r4.pawn.story.hairDef != null && r4.pawn.story.hairDef != shaved;
            C("PlayerHaircutPreservedAfterReload", hairOk, "not Shaved", r4 == null || r4.pawn == null || r4.pawn.story == null || r4.pawn.story.hairDef == null ? "null" : r4.pawn.story.hairDef.defName);
            bool nameOk = r4 != null && r4.pawn != null && r4.pawn.Name != null && r4.pawn.Name.ToStringFull.Contains("PlayerNick");
            C("PlayerRenamePreservedAfterReload", nameOk, "contains PlayerNick", r4 == null || r4.pawn == null || r4.pawn.Name == null ? "null" : r4.pawn.Name.ToStringFull);
            C("OldRecordMigratedToVersion1", r4 != null && r4.recordVersion == 1, 1, r4 == null ? 0 : r4.recordVersion);

            HeroRecord rm = reg.RecordFor(H("TMM_Hero_arthur_maxson"));
            bool maxsonTranslated = rm != null && rm.pawn != null && rm.pawn.Name != null && ((NameTriple)rm.pawn.Name).First == "Артур";
            C("EnglishAutoNameMigratedToRussian", maxsonTranslated, "Артур", rm == null || rm.pawn == null || rm.pawn.Name == null ? "null" : ((NameTriple)rm.pawn.Name).First);
            C("HeroAppearanceApplied", rm != null && rm.appearanceApplied, true, rm == null ? false : rm.appearanceApplied);
            // Requesting the hero again (what quests and factions do) must not swap the player's weapon for signature gear.
            if (rm != null) reg.GetOrCreate(H("TMM_Hero_arthur_maxson"));
            string maxsonGun = rm == null || rm.pawn == null || rm.pawn.equipment == null || rm.pawn.equipment.Primary == null ? "none" : rm.pawn.equipment.Primary.def.defName;
            C("OldSaveSignatureGearNotReissued", rm != null && rm.signatureGearApplied && maxsonGun == "Gun_Autopistol", "applied=true, Gun_Autopistol",
                (rm == null ? "null" : rm.signatureGearApplied.ToString()) + ", " + maxsonGun);
            HairDef wantHair = H("TMM_Hero_arthur_maxson").appearance != null && H("TMM_Hero_arthur_maxson").appearance.hair != null ? H("TMM_Hero_arthur_maxson").appearance.hair : DefDatabase<HairDef>.GetNamedSilentFail("Shaved");
            C("HeroAppearancePreservedAfterReload", rm != null && rm.pawn != null && rm.pawn.story != null && rm.pawn.story.hairDef == wantHair, wantHair != null ? wantHair.defName : "null", rm == null || rm.pawn == null || rm.pawn.story == null || rm.pawn.story.hairDef == null ? "null" : rm.pawn.story.hairDef.defName);

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
            C("FourthHaircutStillPreserved", r4 != null && r4.pawn != null && r4.pawn.story != null && r4.pawn.story.hairDef != null && r4.pawn.story.hairDef.defName != "Shaved", "not Shaved", r4 == null || r4.pawn == null || r4.pawn.story == null || r4.pawn.story.hairDef == null ? "null" : r4.pawn.story.hairDef.defName);
            C("FourthRenameStillPreserved", r4 != null && r4.pawn != null && r4.pawn.Name != null && r4.pawn.Name.ToStringFull.Contains("PlayerNick"), "PlayerNick", r4 == null || r4.pawn == null || r4.pawn.Name == null ? "null" : r4.pawn.Name.ToStringFull);
            C("FourthCustomWeaponStillPreserved", r4 != null && r4.pawn != null && r4.pawn.equipment != null && r4.pawn.equipment.Primary != null && r4.pawn.equipment.Primary.def.defName == "Gun_Autopistol", "Gun_Autopistol", r4 == null || r4.pawn == null || r4.pawn.equipment == null || r4.pawn.equipment.Primary == null ? "null" : r4.pawn.equipment.Primary.def.defName);
            C("SecondSurvivesDayAndGC", rs != null && rs.pawn != null && !rs.pawn.Discarded && rs.pawn.ThingID == savedSecondId, savedSecondId, rs == null || rs.pawn == null ? "null" : rs.pawn.ThingID);
            C("NoDuplicateRecords", reg.Records.Select(r => r.heroDefName).Distinct().Count() == reg.Records.Count(), "distinct", reg.Records.Count());
            facts.Add("forcePauseDialogsClosed=" + dialogsClosed);
            Finish(null, true);
        }
    }

    private static readonly List<string> ownDanglingNames = new List<string>();

    private static int CleanDanglingRelations(HeroRegistry reg)
    {
        var heroes = new HashSet<Pawn>(reg.Records.Where(r => r.pawn != null).Select(r => r.pawn));
        int own = 0;
        foreach (Map m in Find.Maps)
        {
            if (m.mapPawns == null) continue;
            foreach (Pawn p in m.mapPawns.AllPawns)
            {
                if (p.relations == null) continue;
                if (!heroes.Contains(p) && p.Faction != Faction.OfPlayer) continue;
                foreach (DirectPawnRelation rel in p.relations.DirectRelations.ToList())
                {
                    if (rel.otherPawn == null || rel.otherPawn.Discarded)
                    {
                        own++;
                        ownDanglingNames.Add(p.LabelShort + " -> " + (rel.otherPawn == null ? "null" : "Discarded") + " (" + rel.def.defName + ")");
                        p.relations.RemoveDirectRelation(rel);
                    }
                }
            }
        }
        return own;
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
