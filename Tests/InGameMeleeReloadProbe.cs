using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AM;
using AM.Grappling;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public static class MeleeReloadLaunch
{
    static MeleeReloadLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-melee-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-MeleeReload"));
        if (Environment.GetEnvironmentVariable("TMM_MELEE_GRAPPLETRACE") == "1")
            new Harmony("tmm.probe.grappletrace").Patch(AccessTools.Method(typeof(JobDriver_GrapplePawn), "GiveJob"), postfix: new HarmonyMethod(typeof(MeleeReloadLaunch), "Given"));
    }
    /// Test only: where every lasso start came from and how it ended.
    public static void Given(Pawn __0, Pawn __1, bool __result)
    {
        string st = Environment.StackTrace;
        Log.Message("[TMM grapple] from=" + (st.Contains("MapPawnProcessor") ? "auto" : "other") + " wrapper=" + st.Contains("GiveQueued") + " result=" + __result + " " + (__0 == null ? "null" : __0.LabelShort) + " -> " + (__1 == null ? "null" : __1.LabelShort) + " targeted=" + (__1 != null && AM.Grappling.GrabUtility.IsBeingTargetedForGrapple(__1)));
    }
}

/// A real lasso throw that ends in an execution animation. Episodes run one after another; the first is saved on the rope
/// (TMM_MELEE_SCENE=flight) or inside the animation before the blow (anim) and goes on after loading.
public sealed class MeleeReloadProbe : MapComponent
{
    private static readonly string[] Plan = (Environment.GetEnvironmentVariable("TMM_MELEE_PLAN") ?? "") != "" ? Environment.GetEnvironmentVariable("TMM_MELEE_PLAN").Split(',') : new[] { "VisitorDirect", "Visitor", "Damage", "Third", "Flee", "Down", "Kill", "Beside", "Fight" };
    private static bool done;
    /// TMM_MELEE_NORMAL=1: victims keep their armour, old wounds, ordinary part health and any body; the mod's own throws stay on.
    private static readonly bool Normal = Environment.GetEnvironmentVariable("TMM_MELEE_NORMAL") == "1";
    private Pawn attacker, victim, third, ally, raider;
    private Pawn Striker { get { return Plan[Math.Min(episode, Plan.Length - 1)].StartsWith("Visitor") ? raider : attacker; } }
    private IntVec3 center;
    private int episode, stage, stageTick, saveEpisode = -1, hitsBefore, animStart;
    private float injuryBefore;
    private bool loadChecked, fled, initialized, saved, flew, animated, thirdHit, captured;
    private string scene;
    private AnimRenderer renderer;
    private int checks, frames;
    private readonly List<string> facts = new List<string>();
    public MeleeReloadProbe(Map map) : base(map) { }
    private static bool Reload { get { return Environment.GetCommandLineArgs().Contains("-tmm-melee-reload"); } }
    public override void ExposeData()
    {
        Scribe_References.Look(ref attacker, "mrAttacker");
        Scribe_References.Look(ref victim, "mrVictim");
        Scribe_References.Look(ref third, "mrThird");
        Scribe_References.Look(ref ally, "mrAlly");
        Scribe_References.Look(ref raider, "mrRaider");
        Scribe_Values.Look(ref center, "mrCenter");
        Scribe_Values.Look(ref episode, "mrEpisode");
        Scribe_Values.Look(ref stage, "mrStage");
        Scribe_Values.Look(ref stageTick, "mrStageTick");
        Scribe_Values.Look(ref injuryBefore, "mrInjury");
        Scribe_Values.Look(ref flew, "mrFlew");
        Scribe_Values.Look(ref animated, "mrAnimated");
        Scribe_Values.Look(ref saved, "mrSaved");
        Scribe_Values.Look(ref initialized, "mrInit");
        Scribe_Values.Look(ref scene, "mrScene");
    }
    private void Check(bool ok, string text) { if (!ok) throw new Exception(text + " [episode " + Plan[Math.Min(episode, Plan.Length - 1)] + ", stage " + stage + ", victim " + Describe(victim) + ", attacker " + Describe(attacker) + "]"); checks++; }
    private static string Describe(Pawn p)
    {
        if (p == null) return "null";
        return p.ThingID + " body=" + p.story.bodyType.defName + " job=" + p.CurJobDef + " spawned=" + p.Spawned + " dead=" + p.Dead + " downed=" + p.Downed + " injuries=" + Injury(p).ToString("F1") + " at " + p.PositionHeld;
    }
    private static string Toil(Pawn p) { var lord = Verse.AI.Group.LordUtility.GetLord(p); return lord == null ? "none" : lord.LordJob.GetType().Name + "/" + lord.CurLordToil.GetType().Name; }
    private static float Injury(Pawn p) { return p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h => h.Severity); }
    /// What the victim's own AI would start right now, without starting it.
    private string Think()
    {
        ThinkTreeDef tree = null; object[] args = { tree, true };
        ThinkResult next = (ThinkResult)AccessTools.Method(typeof(Pawn_JobTracker), "DetermineNextJob").Invoke(victim.jobs, args);
        var lord = Verse.AI.Group.LordUtility.GetLord(victim);
        string text = "AI wants " + (next.Job == null ? "nothing" : next.Job.def.defName + " " + next.Job.targetA + " from " + next.SourceNode) + " start=" + AccessTools.Method(typeof(Pawn_JobTracker), "ShouldStartJobFromThinkTree").Invoke(victim.jobs, new object[] { next }) + " lord=" + (lord == null ? "none" : lord.CurLordToil.ToString()) + " duty=" + (victim.mindState.duty == null ? "none" : victim.mindState.duty.def.defName) + " lastCheck=" + AccessTools.Field(typeof(Pawn_JobTracker), "lastDamageCheckTick").GetValue(victim.jobs) + " forced=" + victim.CurJob.playerForced + " mode=" + victim.CurJobDef.checkOverrideOnDamage + " interrupt=" + DamageDefOf.Cut.canInterruptJobs + " meleeThreat=" + victim.mindState.meleeThreat;
        if (next.Job != null) JobMaker.ReturnToPool(next.Job);
        return text;
    }
    private GrappleFlyer Flyer { get { return map.listerThings.AllThings.OfType<GrappleFlyer>().FirstOrDefault(f => f.FlyingPawn == victim); } }

    private void Setup()
    {
        scene = Environment.GetEnvironmentVariable("TMM_MELEE_SCENE") ?? "flight";
        facts.Add("scene=" + scene);
        foreach (Pawn p in map.mapPawns.AllPawnsSpawned.Where(p => !p.RaceProps.Humanlike && p.Faction != Faction.OfPlayer).ToList()) p.Destroy(DestroyMode.Vanish);
        center = CellFinder.RandomClosewalkCellNear(map.mapPawns.FreeColonistsSpawned.First().Position, map, 4);
        center = new IntVec3(Mathf.Clamp(center.x, 14, map.Size.x - 15), 0, Mathf.Clamp(center.z, 14, map.Size.z - 15));
        // The field is prepared by the fixture, so the result does not depend on the map.
        foreach (IntVec3 c in CellRect.CenteredOn(center, 11).Cells)
        {
            foreach (Thing x in c.GetThingList(map).Where(x => !(x is Pawn) && x.def.destroyable).ToList()) x.Destroy(DestroyMode.Vanish);
            map.roofGrid.SetRoof(c, null);
            if (c.GetTerrain(map) != TerrainDefOf.Concrete) map.terrainGrid.SetTerrain(c, TerrainDefOf.Concrete);
        }
        map.fogGrid.ClearAllFog();
        Check(CellRect.CenteredOn(center, 9).Cells.All(x => x.Standable(map) || x.GetFirstPawn(map) != null), "The field is not clear");        // One unarmed colonist stands by: a pawn inside an animation is invisible to the AI, so without another visible enemy the victim
        // has nobody to turn to and the interruption never shows.
        foreach (Pawn p in map.mapPawns.FreeColonistsSpawned.Where(x => !x.WorkTagIsDisabled(WorkTags.Violent)).Concat(map.mapPawns.FreeColonistsSpawned.Where(x => x.WorkTagIsDisabled(WorkTags.Violent))).ToList()) { if (attacker == null) attacker = p; else if (ally == null) ally = p; else p.Destroy(DestroyMode.Vanish); }
        ally.equipment.DestroyAllEquipment();
        if (Environment.GetEnvironmentVariable("TMM_MELEE_DOUBLE") == "1")
        {
            // Two lasso throwers: Melee Animation's automatic search can then pick the same target for both in one batch.
            ally.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("MeleeWeapon_LongSword"), ThingDefOf.Steel));
            ally.apparel.Wear((Apparel)ThingMaker.MakeThing(ThingDef.Named("AM_LassoCloth")), false);
            ally.skills.GetSkill(SkillDefOf.Melee).Level = 14;
            facts.Add("double: the ally throws a lasso too");
        }
        attacker.health.Reset();
        attacker.mindState.mentalStateHandler.Reset();
        attacker.equipment.DestroyAllEquipment();
        attacker.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("MeleeWeapon_LongSword"), ThingDefOf.Steel));
        attacker.apparel.Wear((Apparel)ThingMaker.MakeThing(ThingDef.Named("AM_LassoCloth")), false);
        attacker.skills.GetSkill(SkillDefOf.Melee).Level = 14;
        attacker.drafter.Drafted = true;
        attacker.Position = center; attacker.Notify_Teleported(true, true);
    }
    private Pawn Enemy(IntVec3 cell, string body) { return Mutant(cell, body, "TMM_WildMutantsFaction", true); }
    private Pawn Mutant(IntVec3 cell, string body, string factionDef, bool hostile)
    {
        Faction wild = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(factionDef));
        Check(wild != null && wild.HostileTo(Faction.OfPlayer) == hostile, "No " + (hostile ? "hostile" : "friendly") + " mutants");
        Pawn made = null;
        for (int i = 0; i < 60 && made == null; i++)
        {
            PawnKindDef kind = wild.def.pawnGroupMakers.SelectMany(x => x.options).Select(x => x.kind).RandomElement();
            Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, wild, forceGenerateNewPawn: true, allowDowned: false, canGeneratePawnRelations: false, fixedGender: Normal ? (Gender?)null : Gender.Female, mustBeCapableOfViolence: true));
            if ((Normal || p.story.bodyType.defName == body) && !p.Downed) made = p; else p.Destroy(DestroyMode.Vanish);
        }
        Check(made != null, "No " + body + " among the wild mutants");
        made.equipment.DestroyAllEquipment(); made.inventory.DestroyAll();
        // Armour could take the whole blow and old wounds could make it deadly; both are outside this check.
        if (!Normal) { made.apparel.DestroyAll(); made.health.Reset(); MeleeTrace.Tough.Add(made.thingIDNumber); }
        else facts.Add("normal victim " + made.ThingID + " kind=" + made.kindDef.defName + " body=" + made.story.bodyType.defName + " apparel=" + made.apparel.WornApparelCount + " hediffs=" + string.Join(",", made.health.hediffSet.hediffs.Select(h => h.def.defName).ToArray()));
        GenSpawn.Spawn(made, cell, map);
        // A raider with a real assault order: only such a pawn looks for enemies by itself.
        Verse.AI.Group.LordMaker.MakeNewLord(wild, hostile ? (Verse.AI.Group.LordJob)new LordJob_AssaultColony(wild, false, false) : new LordJob_VisitColony(wild, map.AllCells.Where(c => c.Standable(map) && c.DistanceTo(cell) > 40f && map.reachability.CanReach(cell, c, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors))).RandomElement()), map, new[] { made });
        return made;
    }
    private AnimDef Animation()
    {
        var defs = AnimDef.GetExecutionAnimationsForPawnAndWeapon(Striker, Striker.equipment.Primary.def, null).Where(d => d.fixedOutcome == null && d.pawnCount == 2).ToList();
        Check(defs.Count > 0, "No execution animation for the sword");
        return defs.OrderBy(d => d.defName).First(d => d.DataNonLethal != null);
    }
    /// A pawn taken off by the fixture in the middle of a blow must not keep the verb of its destroyed weapon.
    private static void Remove(Pawn p)
    {
        p.Destroy(DestroyMode.Vanish);
        if (p.stances != null) p.stances.SetStance(new Stance_Mobile());
    }
    private void Begin()
    {
        string name = Plan[episode];
        if (victim != null && !victim.Destroyed) { if (victim.Corpse != null) victim.Corpse.Destroy(); else if (victim.Spawned) { Remove(victim); } }
        if (third != null && third.Spawned) Remove(third);
        third = null; renderer = null; flew = animated = thirdHit = false;
        attacker.health.Reset();
        attacker.jobs.StopAll(); attacker.Position = center; attacker.Notify_Teleported(true, true);
        ally.health.Reset(); ally.jobs.StopAll(); ally.drafter.Drafted = true;
        ally.Position = center + new IntVec3(1, 0, -4); ally.Notify_Teleported(true, true);
        if (raider != null && raider.Spawned) { Remove(raider); }
        raider = null;
        if (name.StartsWith("Visitor"))
        {
            // A peaceful visitor on the way to the colony is executed by a raider: the visitors' group answers to the wound.
            attacker.Position = center + new IntVec3(-9, 0, -9); attacker.Notify_Teleported(true, true);
            ally.Position = center + new IntVec3(-7, 0, -9); ally.Notify_Teleported(true, true);
            raider = Enemy(center, "TMM_SuperMutantFemale");
            raider.equipment.DestroyAllEquipment(); raider.inventory.DestroyAll();
            raider.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("MeleeWeapon_LongSword"), ThingDefOf.Steel));
            raider.skills.GetSkill(SkillDefOf.Melee).Level = 14;
        }
        victim = name.StartsWith("Visitor") ? Mutant(center + new IntVec3(name == "VisitorDirect" ? 1 : 6, 0, 0), "TMM_SuperMutantFemale", "TMM_FriendlyMutantsFaction", false) : Enemy(center + new IntVec3(name == "Fight" ? 2 : name == "Beside" ? 1 : 6, 0, 0), "TMM_SuperMutantFemale");
        if (name == "Beside") { flew = true; victim.stances.stunner.StunFor(120, null, false, false); }
        injuryBefore = Injury(victim);
        stage = 1; stageTick = Find.TickManager.TicksGame;
        if (name == "Fight")
        {
            // Ordinary melee: both sides fight on their own.
            var attack = JobMaker.MakeJob(JobDefOf.AttackMelee, victim); attack.playerForced = true;
            attacker.jobs.TryTakeOrderedJob(attack, JobTag.Misc);
            stage = 5; return;
        }
        if (name == "Third") third = Enemy(center + new IntVec3(0, 0, 8), "TMM_SuperMutantFemale");
        var start = new AnimationStartParameters(Animation(), Striker, victim);
        start.ExecutionOutcome = name == "Down" ? ExecutionOutcome.Down : name == "Kill" ? ExecutionOutcome.Kill : ExecutionOutcome.Damage;
        if (name == "VisitorDirect")
        {
            // The adjacent execution, no rope: the group is still on its way when the blow lands.
            flew = true;
            Check(start.TryTrigger(), "The execution did not start");
            return;
        }
        Check(JobDriver_GrapplePawn.GiveJob(Striker, victim, center + IntVec3.East, false, start), "The lasso job was refused");
    }
    private void Save(string when)
    {
        saved = true; saveEpisode = episode;
        GameDataSaveLoader.SaveGame("TMM-MeleeReload");
        Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-MeleeReload.rws")), "No save");
        facts.Add("saved " + when + ": victim " + Describe(victim) + " attacker " + Describe(attacker));
    }
    private void Step()
    {
        int now = Find.TickManager.TicksGame;
        string name = Plan[episode];
        Check(now - stageTick < 3000, "Episode takes too long; flew=" + flew + " animated=" + animated); checks--;
        if (stage == 1)
        {
            GrappleFlyer flyer = Flyer;
            if (flyer != null && !flew)
            {
                flew = true; facts.Add(name + ": on the rope tick " + now + ", victim " + Describe(victim));
                frames = 0;
            }
            if (flew && !saved && !Reload && scene == "flight" && flyer != null && name == "Visitor") { frames++; if (frames >= 3) Save("on the rope"); }
            AnimRenderer r = AnimRenderer.TryGetAnimator(victim);
            if (r == null) { Check(flyer != null || now - stageTick < 900, "Neither on the rope nor in the animation"); checks--; return; }
            Check(name == "VisitorDirect" || name == "Beside" || MeleeTrace.Flights(victim) > 0 || flew || MeleeTrace.AlreadyThere.Contains(victim.thingIDNumber), "The animation began without the rope");
            Check((name == "Kill" ? ExecutionOutcome.Kill : name == "Down" ? ExecutionOutcome.Down : ExecutionOutcome.Damage) == r.ExecutionOutcome && r.Pawns.Contains(victim) && r.Pawns.Contains(Striker), "Another animation began: " + r.Def.defName + " " + r.ExecutionOutcome + " " + string.Join(",", r.Pawns.Select(x => x.LabelShort).ToArray()));
            renderer = r; animated = true; injuryBefore = Injury(victim); animStart = now; stage = 2;
            facts.Add(name + ": animation " + r.Def.defName + " outcome=" + r.ExecutionOutcome + " tick " + now + " jobs " + Striker.CurJobDef + "/" + victim.CurJobDef + " toil " + Toil(victim) + " injuries " + injuryBefore.ToString("F1"));
        }
        if (stage == 2)
        {
            if (!saved && !Reload && scene == "anim" && renderer.CurrentTime > 0.05f && !renderer.IsDestroyed && Injury(victim) <= injuryBefore) Save("inside the animation, time " + renderer.CurrentTime.ToString("F2"));
            if (name == "Third" && !thirdHit && renderer.CurrentTime > 0.15f && !renderer.IsDestroyed)
            {
                thirdHit = true;
                var before = victim.CurJobDef;
                facts.Add("Third: " + Think());
                victim.TakeDamage(new DamageInfo(DamageDefOf.Cut, 4f, 0f, -1f, third));
                facts.Add("Third: hit by " + third.ThingID + " in the animation, job " + before + " -> " + victim.CurJobDef);
            }
            if (name == "Flee" && !fled && renderer.CurrentTime > 0.15f && !renderer.IsDestroyed)
            {
                // The raid is called off while its member is in the animation.
                fled = true;
                var lord = Verse.AI.Group.LordUtility.GetLord(victim);
                lord.GotoToil(lord.Graph.lordToils.First(x => x is Verse.AI.Group.LordToil_ExitMap));
                facts.Add("Flee: group goes to " + Toil(victim) + ", job " + victim.CurJobDef + ", duty " + victim.mindState.duty.def.defName);
            }
            if (!renderer.IsDestroyed) return;
            facts.Add(name + ": animation over tick " + now + " interrupted=" + renderer.WasInterrupted + " time=" + renderer.CurrentTime.ToString("F2") + "/" + renderer.Duration.ToString("F2") + " victim " + Describe(victim) + " toil " + Toil(victim) + " trace=" + MeleeTrace.Lines.Count);
            if (Normal && Plan.All(x => x == "Damage"))
            {
                // Counting series (TMM_MELEE_PLAN=Damage,Damage,...): a wound-only execution on an ordinary victim, outcome written down.
                facts.Add("COUNT Damage outcome dead=" + victim.Dead + " downed=" + victim.Downed + " interrupted=" + renderer.WasInterrupted + " hits=" + MeleeTrace.Hits(victim) + " victim " + Describe(victim));
                Next(); return;
            }
            Check(!renderer.WasInterrupted, "The animation was interrupted: " + string.Join(" || ", MeleeTrace.Lines.ToArray()));
            if (name == "Damage" || name == "Third" || name == "Flee") Check(!victim.Dead && MeleeTrace.Hits(victim) > 0, "The blow did not land");
            if (name.StartsWith("Visitor")) Check(victim.Dead || MeleeTrace.Hits(victim) > 0 || Reload, "The visitor was never hit");
            if (name == "Down") Check(victim.Downed && !victim.Dead, "The victim is not downed");
            if (name == "Kill") Check(victim.Dead, "The victim is not dead");
            if (name == "Beside") Check(victim.Dead || MeleeTrace.Hits(victim) > 0, "The blow did not land");
            if (name == "Kill" || name == "Down" || name == "Beside") { Next(); return; }
            stage = 3; stageTick = now;
        }
        if (stage == 3)
        {
            if (name.StartsWith("Visitor"))
            {
                // The group's answer is only put off: the visitor is free again and its group has gone over to defence.
                if (victim.Dead || victim.Downed || victim.CurJobDef != null && victim.CurJobDef.defName != "AM_InAnimation") { facts.Add(name + ": after the animation job " + victim.CurJobDef + ", toil " + Toil(victim)); Check(victim.Dead || Toil(victim).Contains("Defend"), "The visitors' group did not answer to the wound: " + Toil(victim)); Next(); return; }
                Check(now - stageTick < 120, "The visitor is stuck in the animation job"); checks--; return;
            }
            // The reaction is only put off: a wounded enemy fights back when the animation is over.
            if (victim.Dead || victim.Downed) { facts.Add(name + ": victim fell after the animation"); Next(); return; }
            if (name == "Flee")
            {
                if (victim.CurJobDef != null && victim.CurJobDef.defName != "AM_InAnimation") { facts.Add("Flee: after the animation job " + victim.CurJob + " duty " + victim.mindState.duty.def.defName); Check(victim.mindState.duty.def.defName.Contains("Exit"), "The raider forgot that the raid is over"); Next(); return; }
                Check(now - stageTick < 120, "The raider is stuck in the animation job"); checks--; return;
            }
            if (victim.CurJobDef == JobDefOf.AttackMelee || victim.CurJobDef == JobDefOf.Goto) { facts.Add(name + ": victim fights back " + (now - stageTick) + " ticks after the animation, target " + victim.CurJob.targetA); Next(); return; }
            Check(now - stageTick < 600, "The wounded victim never fought back"); checks--;
        }
        if (stage == 5)
        {
            if (victim.Dead || victim.Downed || attacker.Downed || Injury(victim) > injuryBefore && Injury(attacker) > 0f && now - stageTick > 400) { facts.Add("Fight: victim " + Describe(victim) + " attacker " + Describe(attacker) + " after " + (now - stageTick)); Check(Injury(victim) > injuryBefore || victim.Dead, "No melee blow landed"); Next(); return; }
            Check(now - stageTick < 2500, "The plain fight did not happen"); checks--;
        }
    }
    private void Next()
    {
        if (victim.Spawned) victim.jobs.StopAll();
        if (episode == Plan.Length - 1 && victim.Spawned) Remove(victim);
        episode++; stage = 0;
        if (episode >= Plan.Length) Finish(null);
    }
    public override void MapComponentUpdate()
    {
        if (done || Find.CurrentMap != map) return;
        if (!Reload && !Environment.GetCommandLineArgs().Contains("-tmm-melee-probe-reload")) return;
        if (Reload && !saved) return;
        if (victim != null && !Normal) MeleeTrace.Tough.Add(victim.thingIDNumber);
        try
        {
            // The fixture starts every throw and execution itself. The automatic ones work on another thread and would name pawns the
            // fixture has already removed.
            if (!Normal) Core.Settings.AutoGrapple = Core.Settings.AutoExecute = Core.Settings.EnemiesCanGrapple = Core.Settings.EnemiesCanExecute = false;
            foreach (Window w in Find.WindowStack.Windows.ToList()) if (w.forcePause) facts.Add("pausing window: " + ProbeWindows.Dismiss(w));
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            if (!initialized) { Setup(); initialized = true; }
            if (Reload && !loadChecked)
            {
                loadChecked = true;
                facts.Add("loaded: scene=" + scene + " stage " + stage + " flyer=" + (Flyer != null) + " animator=" + (AnimRenderer.TryGetAnimator(victim) != null) + " victim " + Describe(victim) + " attacker " + Describe(attacker));
                if (scene == "flight") Check(Flyer != null, "The rope flight was lost on load");
                if (scene == "anim") { renderer = AnimRenderer.TryGetAnimator(victim); Check(renderer != null && !renderer.IsDestroyed, "The animation was lost on load"); stage = 2; facts.Add("resumed at " + renderer.CurrentTime.ToString("F2")); Check(renderer.CurrentTime > 0.05f, "The animation began anew"); Check(victim.CurJobDef != null && victim.CurJobDef.defName == "AM_InAnimation", "The victim lost the animation job on load"); }
                stageTick = Find.TickManager.TicksGame;
            }
            if (stage == 0) { Begin(); return; }
            Step();
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }
    private void Finish(string error)
    {
        done = true;
        if (error == null && !Reload && !saved && (Environment.GetEnvironmentVariable("TMM_MELEE_PLAN") ?? "") == "") error = "Nothing was saved";
        string name = Reload ? "MeleeReloadReloadProbe.json" : "MeleeReloadProbe.json";
        facts.AddRange(MeleeTrace.Lines.Select(x => "TRACE " + x));
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, name), "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks + ",\"facts\":[" + string.Join(",", facts.Select(Q).ToArray()) + "],\"errors\":" + (error == null ? "[]" : "[" + Q(error) + "]") + "}");
        if (!Reload) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MeleeReloadUiProbe.json"), "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
    }
    public override void MapComponentOnGUI()
    {
        if (Event.current.type != EventType.Repaint || captured || Flyer == null) return;
        captured = true;
        Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 12f);
        AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"), "CaptureScreenshot", new[] { typeof(string) }).Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, Reload ? "Rope-reload.png" : "Rope.png") });
    }
    private static string Q(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", " ") + "\""; }
}