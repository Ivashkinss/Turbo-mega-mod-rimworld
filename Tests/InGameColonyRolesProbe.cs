using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using TurboMegaMod;
using Verse;
using Verse.AI;

// Colony roles, stage 1. Fresh: -tmm-colony-roles-probe. Reload: -tmm-colony-roles-reload (save TMM-ColonyRoles).
[StaticConstructorOnStartup]
public static class ColonyRolesProbeLaunch
{
    static ColonyRolesProbeLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-colony-roles-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-ColonyRoles"));
        if (Environment.GetCommandLineArgs().Contains("-tmm-colony-roles-save"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-RolesSave"));
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps == null) return;
            Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
            if (home != null && home.GetComponent<ColonyRolesProbe>() == null) home.components.Add(new ColonyRolesProbe(home));
        });
    }
}

public sealed class ColonyRolesProbe : MapComponent
{
    private sealed class Check { public string name, expected, actual; public bool passed; }
    private readonly List<Check> checks = new List<Check>();
    private bool done;
    private string cookId, trooperId;

    public ColonyRolesProbe(Map map) : base(map) { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cookId, "crCook");
        Scribe_Values.Look(ref trooperId, "crTrooper");
        Scribe_Values.Look(ref uniformId, "crUniform");
    }

    private void C(string name, bool ok, object expected, object actual)
    {
        checks.Add(new Check { name = name, passed = ok, expected = Convert.ToString(expected), actual = Convert.ToString(actual) });
    }

    private static WorkTypeDef CookingWork { get { return DefDatabase<WorkTypeDef>.GetNamed("Cooking"); } }
    private static ColonyRoleDef R(string id) { return DefDatabase<ColonyRoleDef>.GetNamed("TMM_Role_" + id); }

    public override void MapComponentUpdate()
    {
        string[] a = Environment.GetCommandLineArgs();
        bool reload = a.Contains("-tmm-colony-roles-reload"), fresh = a.Contains("-tmm-colony-roles-probe");
        if (done || !map.IsPlayerHome || (!reload && !fresh)) return;
        try { if (reload) Reload(); else Fresh(); }
        catch (Exception ex) { Finish(ex, reload); }
    }

    // A copy of a real save is played on, and every change of worn apparel is written down with its tick and job.
    // TMM_ROLES_WATCH = ThingID of the watched colonist, TMM_ROLES_TICKS = how long.
    private readonly Dictionary<Pawn, List<Apparel>> wornBefore = new Dictionary<Pawn, List<Apparel>>();
    private readonly List<string> changes = new List<string>();
    private List<string> watchedAtStart;
    private Pawn lastWatched; private string goneNote;
    private int saveStart = -1;
    private static string Worn(Pawn p) { return string.Join(",", p.apparel.WornApparel.Select(x => x.ThingID).ToArray()); }
    public override void MapComponentTick()
    {
        if (done || !map.IsPlayerHome || !Environment.GetCommandLineArgs().Contains("-tmm-colony-roles-save")) return;
        if (saveStart < 0) saveStart = Find.TickManager.TicksGame;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
        foreach (Window w in Find.WindowStack.Windows.ToList()) if (w.forcePause) w.Close(false);
        foreach (Pawn p in map.mapPawns.FreeColonists)
        {
            List<Apparel> old;
            List<Apparel> now = p.apparel.WornApparel.ToList();
            if (wornBefore.TryGetValue(p, out old))
                foreach (Apparel a in now.Except(old).Concat(old.Except(now)))
                    changes.Add((Find.TickManager.TicksGame - saveStart) + ":" + p.LabelShort + (now.Contains(a) ? " +" : " -") + a.ThingID + " job=" + p.CurJobDef + " policy=" + p.outfits.CurrentApparelPolicy.label);
            wornBefore[p] = now;
        }
        Pawn watched = map.mapPawns.AllPawns.FirstOrDefault(p => p.ThingID == Environment.GetEnvironmentVariable("TMM_ROLES_WATCH"));
        if (watchedAtStart == null && watched != null) watchedAtStart = watched.apparel.WornApparel.Select(x => x.ThingID).ToList();
        // Diagnostic only: where the watched colonist went if it left the map (Old017, 053531: "no pawn").
        if (watched != null) lastWatched = watched;
        else if (lastWatched != null && goneNote == null)
            goneNote = "gone at " + (Find.TickManager.TicksGame - saveStart) + ": dead=" + lastWatched.Dead + " destroyed=" + lastWatched.Destroyed + " spawned=" + lastWatched.Spawned + " downed=" + lastWatched.Downed + " holder=" + (lastWatched.ParentHolder == null ? "null" : lastWatched.ParentHolder.GetType().Name) + " mapHeld=" + (lastWatched.MapHeld == null ? "null" : lastWatched.MapHeld.ToString()) + " faction=" + lastWatched.Faction + " job=" + lastWatched.CurJobDef + " mental=" + lastWatched.MentalStateDef + " world=" + Find.WorldPawns.Contains(lastWatched) + " lastPos=" + lastWatched.PositionHeld;
        int ticks = int.Parse(Environment.GetEnvironmentVariable("TMM_ROLES_TICKS") ?? "15000");
        if (Find.TickManager.TicksGame - saveStart < ticks) return;
        C("Changes", true, "info", string.Join(" | ", changes.ToArray()));
        C("WatchedWhereabouts", true, "info", goneNote ?? (watched == null ? "never seen" : "on the map at " + watched.Position));
        C("Colonists", true, "info", string.Join(" | ", map.mapPawns.FreeColonists.Select(p => p.LabelShort + " [" + p.outfits.CurrentApparelPolicy.label + "] " + Worn(p)).ToArray()));
        C("WatchedKeepsWornApparel", watched != null && watchedAtStart.All(x => watched.apparel.WornApparel.Any(a => a.ThingID == x)), watchedAtStart == null ? "?" : string.Join(",", watchedAtStart.ToArray()), watched == null ? "no pawn" : Worn(watched));
        Finish(null, true);
        File.Copy(Path.Combine(GenFilePaths.SaveDataFolderPath, "ColonyRolesReloadProbe.json"), Path.Combine(GenFilePaths.SaveDataFolderPath, "ColonyRolesSaveReloadProbe.json"), true);
    }

    private void Fresh()
    {
        ColonyRoles roles = ColonyRoles.Instance;
        C("ComponentExists", roles != null, true, roles != null);
        var defs = DefDatabase<ColonyRoleDef>.AllDefsListForReading;
        C("SevenRoles", defs.Count == 7, 7, defs.Count);
        int ranks = defs.Sum(d => d.ranks.Count);
        C("TwentyFourRanks", ranks == 24, 24, ranks);
        C("TrooperHasSixRanks", R("trooper").ranks.Count == 6, 6, R("trooper").ranks.Count);
        C("LieutenantHasAura", R("trooper").ranks[2].aura != null && R("trooper").ranks[2].aura.radius > 0, "aura", R("trooper").ranks[2].aura == null ? "none" : R("trooper").ranks[2].aura.radius.ToString());
        C("ChefLabelTranslated", !R("cook").ranks[2].label.NullOrEmpty(), "label", R("cook").ranks[2].label);

        List<Pawn> colonists = null;
        Pawn cook = null, trooper = null, third = null;
        // Random starting colonists may be incapable of the needed work; add capable ones instead of failing.
        for (int attempt = 0; attempt < 6; attempt++)
        {
            colonists = map.mapPawns.FreeColonistsSpawned.ToList();
            cook = colonists.FirstOrDefault(p => !p.WorkTypeIsDisabled(CookingWork) && !p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling));
            trooper = colonists.FirstOrDefault(p => p != cook && !p.WorkTypeIsDisabled(WorkTypeDefOf.Hunting) && !p.WorkTagIsDisabled(WorkTags.Violent));
            third = colonists.FirstOrDefault(p => p != cook && p != trooper && !p.WorkTagIsDisabled(WorkTags.Violent));
            if (cook != null && trooper != null && third != null) break;
            Pawn extra = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true));
            GenSpawn.Spawn(extra, CellFinder.RandomClosewalkCellNear(map.Center, map, 8), map);
        }
        if (cook == null || trooper == null || third == null) throw new Exception("need 3 suitable colonists, have " + colonists.Count);

        cook.workSettings.EnableAndInitialize();
        cook.workSettings.SetPriority(CookingWork, 3);
        cook.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        roles.Assign(cook, R("cook"));
        C("CookingToPriority1", cook.workSettings.GetPriority(CookingWork) == 1, 1, cook.workSettings.GetPriority(CookingWork));
        C("OtherTopWorkDropsTo2", cook.workSettings.GetPriority(WorkTypeDefOf.Hauling) == 2, 2, cook.workSettings.GetPriority(WorkTypeDefOf.Hauling));
        C("ManualPrioritiesEnabled", Current.Game.playSettings.useWorkPriorities, true, Current.Game.playSettings.useWorkPriorities);
        C("CookRankZero", roles.For(cook) != null && roles.For(cook).rank == 0 && roles.For(cook).Rank == R("cook").ranks[0], "rank 0", roles.For(cook) == null ? "none" : roles.For(cook).rank.ToString());

        roles.Assign(trooper, R("trooper"));
        C("HuntingToPriority1", trooper.workSettings.GetPriority(WorkTypeDefOf.Hunting) == 1, 1, trooper.workSettings.GetPriority(WorkTypeDefOf.Hunting));

        roles.Assign(third, R("farmer"));
        roles.Assign(third, null);
        C("ClearRemovesRole", roles.For(third) == null, "none", roles.For(third) == null ? "none" : roles.For(third).role.defName);

        // Stage 2: colonist bar grouping on the real ColonistBar entries.
        roles.Assign(third, R("farmer"));
        roles.For(cook).rank = 2;
        int all = Find.ColonistBar.Entries.Count;
        ColonistBarGroups.Set(true, ColonistBarGroups.Civil);
        var civil = Find.ColonistBar.Entries.Select(e => e.pawn).ToList();
        C("CivilGroupOnlyCivil", civil.Count == 2 && civil.Contains(cook) && civil.Contains(third), "cook+farmer", civil.Count);
        C("CivilSortedByRank", civil.Count == 2 && civil[0] == cook, "chef first", civil.Count == 0 ? "empty" : civil[0].LabelShort);
        ColonistBarGroups.Set(true, ColonistBarGroups.Military);
        var mil = Find.ColonistBar.Entries.Select(e => e.pawn).ToList();
        C("MilitaryGroupOnlyTrooper", mil.Count == 1 && mil[0] == trooper, "trooper", mil.Count);
        ColonistBarGroups.Set(true, null);
        C("GroupedWithoutSelectionHidesPortraits", Find.ColonistBar.Entries.Count == 0, 0, Find.ColonistBar.Entries.Count);
        ColonistBarGroups.Set(false, null);
        C("ShowAllRestores", Find.ColonistBar.Entries.Count == all, all, Find.ColonistBar.Entries.Count);
        roles.Assign(third, null);
        roles.For(cook).rank = 0;
        // Stage 4: rank stat bonus, pride thought, commander aura (uncached stat worker values).
        StatDef cookSpeed = DefDatabase<StatDef>.GetNamed("CookSpeed");
        roles.Assign(cook, null);
        float baseCook = cookSpeed.Worker.GetValue(StatRequest.For(cook));
        roles.Assign(cook, R("cook"));
        float rankCook = cookSpeed.Worker.GetValue(StatRequest.For(cook));
        C("DiagCookPartRegistered", cookSpeed.parts != null && cookSpeed.parts.Any(x => x is StatPart_ColonyRank), true, cookSpeed.parts == null ? "null" : string.Join(",", cookSpeed.parts.Select(x => x.GetType().Name).ToArray()));
        C("DiagRankStat", R("cook").ranks[0].stats.Count > 0, "stats", string.Join(",", R("cook").ranks[0].stats.Select(m => (m.stat == null ? "null" : m.stat.defName) + "=" + m.value).ToArray()));
        C("DiagExplanation", true, "", cookSpeed.Worker.GetExplanationFull(StatRequest.For(cook), ToStringNumberSense.Absolute, rankCook).Replace("\n"," | "));
        C("RankAddsCookSpeed", Math.Abs(rankCook - baseCook - 0.05f) < 0.001f, "+0.05", rankCook - baseCook);
        C("PrideThoughtActive", DefDatabase<ThoughtDef>.GetNamed("TMM_ProudOfRank").Worker.CurrentState(cook).Active, true, "checked");
        StatDef acc = DefDatabase<StatDef>.GetNamed("ShootingAccuracyPawn");
        roles.Assign(third, R("trooper"));
        float soloAcc = acc.Worker.GetValue(StatRequest.For(third));
        roles.For(trooper).rank = 2; // lieutenant: aura radius 9, +0.03 accuracy, +2 mood
        third.Position = CellFinder.RandomClosewalkCellNear(trooper.Position, map, 2);
        third.Notify_Teleported();
        C("AuraCoversNearbyTrooper", ColonyRankEffects.AuraFor(third) != null, "aura", ColonyRankEffects.AuraFor(third) == null ? "none" : "aura");
        float auraAcc = acc.Worker.GetValue(StatRequest.For(third));
        C("AuraAddsAccuracy", Math.Abs(auraAcc - soloAcc - 0.03f) < 0.001f, "+0.03", auraAcc - soloAcc);
        C("CommanderThoughtActive", DefDatabase<ThoughtDef>.GetNamed("TMM_CommanderNearby").Worker.CurrentState(third).Active, true, "checked");
        C("NoAuraForCommanderHimself", ColonyRankEffects.AuraFor(trooper) == null, "none", ColonyRankEffects.AuraFor(trooper) == null ? "none" : "aura");
        roles.Assign(third, null);
        roles.For(trooper).rank = 0;
        // Stage 3a: promotion requirements and the promote gizmo.
        // If Ideology has the ceremony precept, temporarily remove it to test the instant promotion flow
        // (which gives TMM_PromotedCivil and TMM_WitnessedPromotion), exactly as InGameColonyRanksProbe does.
        PreceptDef ceremonyPreceptDef = DefDatabase<PreceptDef>.GetNamedSilentFail("TMM_PromotionCeremony");
        Precept_Ritual removedPrecept = null;
        Ideo pIdeo = Faction.OfPlayer != null && Faction.OfPlayer.ideos != null ? Faction.OfPlayer.ideos.PrimaryIdeo : null;
        if (pIdeo != null && ceremonyPreceptDef != null)
        {
            removedPrecept = pIdeo.PreceptsListForReading.OfType<Precept_Ritual>().FirstOrDefault(p => p.def == ceremonyPreceptDef);
            if (removedPrecept != null) pIdeo.RemovePrecept(removedPrecept, false);
        }
        if (cook.Ideo != null && cook.Ideo != pIdeo && ceremonyPreceptDef != null)
        {
            Precept_Ritual pawnPrecept = cook.Ideo.PreceptsListForReading.OfType<Precept_Ritual>().FirstOrDefault(p => p.def == ceremonyPreceptDef);
            if (pawnPrecept != null) cook.Ideo.RemovePrecept(pawnPrecept, false);
        }

        SkillRecord cookSkill = cook.skills.GetSkill(SkillDefOf.Cooking);
        RecordDef meals = DefDatabase<RecordDef>.GetNamed("MealsCooked");
        cookSkill.Level = 5;
        Command_Action notReady = cook.GetGizmos().OfType<Command_Action>().FirstOrDefault(g => g.defaultLabel != null && g.defaultLabel.IndexOf(R("cook").ranks[1].label, StringComparison.OrdinalIgnoreCase) >= 0);
        C("PromoteGizmoDisabledWhenNotReady", notReady != null && notReady.Disabled, "disabled", notReady == null ? "none" : notReady.Disabled.ToString());
        cookSkill.Level = 15;
        float needMeals = R("cook").ranks[1].recordMin - cook.records.GetValue(meals);
        if (needMeals > 0) cook.records.AddTo(meals, needMeals);
        C("ReadyWhenRequirementsMet", ColonyPromotion.Ready(cook, roles.For(cook)), true, ColonyPromotion.MissingFor(cook, roles.For(cook)) ?? "ready");
        Command_Action ready = cook.GetGizmos().OfType<Command_Action>().FirstOrDefault(g => g.defaultLabel != null && g.defaultLabel.IndexOf(R("cook").ranks[1].label, StringComparison.OrdinalIgnoreCase) >= 0);
        C("PromoteGizmoEnabledWhenReady", ready != null && !ready.Disabled, "enabled", ready == null ? "none" : ready.Disabled.ToString());
        if (ready != null) ready.action();
        C("PromotedToRank1", roles.For(cook).rank == 1, 1, roles.For(cook).rank);
        C("PromotedThoughtGained", cook.needs.mood.thoughts.memories.GetFirstMemoryOfDef(DefDatabase<ThoughtDef>.GetNamed("TMM_PromotedCivil")) != null, "memory", "checked");
        Pawn witness = colonists.FirstOrDefault(p => p != cook);
        C("WitnessThoughtGained", witness != null && witness.needs.mood.thoughts.memories.GetFirstMemoryOfDef(DefDatabase<ThoughtDef>.GetNamed("TMM_WitnessedPromotion")) != null, "memory", "checked");
        roles.For(cook).rank = 0;
        if (pIdeo != null) ColonyRankCeremony.EnsurePlayerIdeoPrecept(pIdeo);
        if (cook.Ideo != null && cook.Ideo != pIdeo) ColonyRankCeremony.EnsurePlayerIdeoPrecept(cook.Ideo);
        Command_Action giz = cook.GetGizmos().OfType<Command_Action>().FirstOrDefault(g => g.defaultLabel != null && g.defaultLabel.Contains(R("cook").LabelCap));
        C("RoleGizmoShowsRole", giz != null, "gizmo with role label", giz == null ? "none" : giz.defaultLabel);

        Uniforms(roles);
        cookId = cook.ThingID; trooperId = trooper.ThingID;
        GameDataSaveLoader.SaveGame("TMM-ColonyRoles");
        C("SaveWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-ColonyRoles.rws")), true, "checked");
        Finish(null, false);
    }

    // ---- Rank uniform and the clothes a colonist already wears (the playtest save: a signature costume and a better helmet).
    private string uniformId;
    private static Job Optimize(Pawn p)
    {
        p.mindState.nextApparelOptimizeTick = 0;
        return (Job)HarmonyLib.AccessTools.Method(typeof(JobGiver_OptimizeApparel), "TryGiveJob").Invoke(new JobGiver_OptimizeApparel(), new object[] { p });
    }
    private static string Txt(Job j) { return j == null ? "no job" : j.def.defName + " " + (j.targetA.Thing == null ? "?" : j.targetA.Thing.def.defName); }
    private static Apparel Dress(Pawn p, string def)
    {
        ThingDef d = ThingDef.Named(def);
        Apparel a = (Apparel)ThingMaker.MakeThing(d, d.MadeFromStuff ? GenStuff.DefaultStuffFor(d) : null);
        p.apparel.Wear(a, false);
        return a;
    }
    private Pawn Colonist(string nick)
    {
        Pawn p;
        do { p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false)); } while (p.WorkTagIsDisabled(WorkTags.Violent) || p.DevelopmentalStage != DevelopmentalStage.Adult);
        p.Name = new NameTriple("TMM", nick, "Uniform");
        p.apparel.DestroyAll(DestroyMode.Vanish);
        GenSpawn.Spawn(p, CellFinder.RandomClosewalkCellNear(map.Center, map, 8), map);
        return p;
    }
    private void Uniforms(ColonyRoles roles)
    {
        if (Environment.GetEnvironmentVariable("TMM_UNIFORM_CONTROL") == "1") new HarmonyLib.Harmony("tmm.test.uniform").UnpatchAll("colonyproject.integrated.rankuniforms");
        foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel).ToList()) t.Destroy();
        Pawn p = Colonist("Uniformed"), other = Colonist("Other");
        uniformId = p.ThingID;
        ApparelPolicy any = Current.Game.outfitDatabase.DefaultOutfit();
        p.outfits.CurrentApparelPolicy = any; other.outfits.CurrentApparelPolicy = any;
        Apparel costume = Dress(p, "TMM_Sig_silver_shroud_costume"), helmet = Dress(p, "Apparel_AdvancedHelmet");
        roles.Assign(p, R("trooper"));
        C("UniformPolicyAppliedFromDefault", ColonyRankUniforms.IsManagedRankPolicy(p.outfits.CurrentApparelPolicy), "rank policy", p.outfits.CurrentApparelPolicy.label);
        C("UniformPolicyForbidsBoth", !p.outfits.CurrentApparelPolicy.filter.Allows(costume) && !p.outfits.CurrentApparelPolicy.filter.Allows(helmet), "not allowed by the list", "checked");
        Job j = Optimize(p);
        C("NothingIsTakenOffWithoutUniformPiece", j == null || j.def != JobDefOf.RemoveApparel, "no RemoveApparel", Txt(j));

        // A uniform helmet ready in storage replaces the ordinary helmet; the signature costume stays.
        Danger maxDanger = p.NormalMaxDanger();
        IntVec3 cell = GenRadial.RadialCellsAround(p.Position, 30f, true).Where(c =>
            c.InBounds(map) && c.Standable(map) && c.GetEdifice(map) == null && map.zoneManager.ZoneAt(c) == null
            && c.GetFirstItem(map) == null && p.CanReach(c, PathEndMode.OnCell, maxDanger)).DefaultIfEmpty(IntVec3.Invalid).First();
        if (!cell.IsValid)
            throw new InvalidOperationException("Uniform fixture has no free, reachable stockpile cell near " + p.Position + " at danger level " + maxDanger + ".");
        Zone_Stockpile store = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(store);
        store.AddCell(cell);
        Thing simple = ThingMaker.MakeThing(ThingDef.Named("Apparel_SimpleHelmet"), ThingDefOf.Steel);
        simple.TryGetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        GenSpawn.Spawn(simple, cell, map);
        simple.SetForbidden(false, false);
        j = Optimize(p);
        C("OrdinaryHelmetGivesWayToUniform", j != null && (j.def == JobDefOf.RemoveApparel && j.targetA.Thing == helmet || j.def == JobDefOf.Wear && j.targetA.Thing == simple), "helmet off or uniform helmet on", Txt(j) + " score=" + JobGiver_OptimizeApparel.ApparelScoreRaw(p, (Apparel)simple) + " reach=" + p.CanReserveAndReach(simple, PathEndMode.OnCell, Danger.Deadly) + " danger=" + p.NormalMaxDanger() + " storage=" + simple.IsInAnyStorage() + " forbidden=" + simple.IsForbidden(p) + " traits=" + string.Join("/", p.story.traits.allTraits.Select(x => x.def.defName).ToArray()) + " quality=" + simple.TryGetComp<CompQuality>().Quality + " ideo=" + (p.Ideo == null ? "-" : p.Ideo.name) + " lent=" + p.IsQuestLodger());
        if (j != null && j.def == JobDefOf.RemoveApparel) { Apparel off; p.apparel.TryDrop(helmet, out off, p.Position, false); }
        j = Optimize(p);
        C("UniformHelmetIsChosen", j != null && j.def == JobDefOf.Wear && j.targetA.Thing == simple, "Wear simple helmet", Txt(j));
        if (p.apparel.WornApparel.Contains(helmet)) p.apparel.Remove(helmet); else if (helmet.Spawned) helmet.DeSpawn();
        simple.DeSpawn(); p.apparel.Wear((Apparel)simple, false);
        j = Optimize(p);
        C("SignatureStaysOnAfterChange", p.apparel.WornApparel.Contains(costume) && (j == null || j.def != JobDefOf.RemoveApparel), "costume worn, no RemoveApparel", Txt(j));

        // Another colonist cannot get it, and an ordinary policy chosen by the player still works as in vanilla.
        Job oj = Optimize(other);
        C("OtherColonistDoesNotTakeIt", oj == null || oj.targetA.Thing != costume, "not the costume", Txt(oj));
        ApparelPolicy strict = Current.Game.outfitDatabase.MakeNewOutfit();
        strict.label = "TMM probe strict"; strict.filter.SetDisallowAll();
        ApparelPolicy rankPolicy = p.outfits.CurrentApparelPolicy;
        p.outfits.CurrentApparelPolicy = strict;
        j = Optimize(p);
        C("PlayersOwnPolicyWorksAsVanilla", j != null && j.def == JobDefOf.RemoveApparel, "RemoveApparel", Txt(j));

        // Promotion keeps the player's own choice, including a return to the default policy.
        ColonyRoleAssignment a = roles.For(p);
        ColonyRank first = a.Rank, second = a.role.ranks[1];
        bool changed = ColonyRankUniforms.ApplyUniformPolicy(p, a.role, second, first);
        C("PromotionKeepsOwnPolicy", !changed && p.outfits.CurrentApparelPolicy == strict, strict.label, p.outfits.CurrentApparelPolicy.label);
        p.outfits.CurrentApparelPolicy = any;
        changed = ColonyRankUniforms.ApplyUniformPolicy(p, a.role, second, first);
        C("PromotionKeepsReturnToDefault", !changed && p.outfits.CurrentApparelPolicy == any, any.label, p.outfits.CurrentApparelPolicy.label);
        p.outfits.CurrentApparelPolicy = rankPolicy;
        changed = ColonyRankUniforms.ApplyUniformPolicy(p, a.role, second, first);
        C("PromotionMovesRankPolicy", changed && p.outfits.CurrentApparelPolicy != rankPolicy, "next rank policy", p.outfits.CurrentApparelPolicy.label);
        p.outfits.CurrentApparelPolicy = rankPolicy;

        // The player edits the rank policy: the edit survives a resync, and (below) loading.
        rankPolicy.filter.SetAllow(ThingDef.Named("Apparel_CowboyHat"), true);
        rankPolicy.filter.SetAllow(ThingDef.Named("Apparel_FlakVest"), false);
        ColonyRankUniforms.SyncAllRankPolicies();
        C("PolicyEditSurvivesResync", rankPolicy.filter.Allows(ThingDef.Named("Apparel_CowboyHat")) && !rankPolicy.filter.Allows(ThingDef.Named("Apparel_FlakVest")), "hat allowed, vest forbidden", "checked");
        other.Destroy();
    }

    private void Reload()
    {
        ColonyRoles roles = ColonyRoles.Instance;
        C("AssignmentsLoaded", roles != null && roles.All.Count() == 3, 3, roles == null ? -1 : roles.All.Count());
        Pawn dressed = map.mapPawns.AllPawns.FirstOrDefault(p => p.ThingID == uniformId);
        ApparelPolicy policy = dressed == null ? null : dressed.outfits.CurrentApparelPolicy;
        C("UniformPolicyKept", policy != null && ColonyRankUniforms.IsManagedRankPolicy(policy), "rank policy", policy == null ? "none" : policy.label);
        C("PolicyEditSurvivesLoading", policy != null && policy.filter.Allows(ThingDef.Named("Apparel_CowboyHat")) && !policy.filter.Allows(ThingDef.Named("Apparel_FlakVest")), "hat allowed, vest forbidden", "checked");
        C("CostumeStillWorn", dressed != null && dressed.apparel.WornApparel.Any(x => x.def.defName == "TMM_Sig_silver_shroud_costume"), "worn", dressed == null ? "none" : Worn(dressed));
        Job after = dressed == null ? null : Optimize(dressed);
        C("NothingIsTakenOffAfterLoading", dressed != null && (after == null || after.def != JobDefOf.RemoveApparel), "no RemoveApparel", Txt(after));
        Pawn cook = map.mapPawns.AllPawns.FirstOrDefault(p => p.ThingID == cookId);
        Pawn trooper = map.mapPawns.AllPawns.FirstOrDefault(p => p.ThingID == trooperId);
        C("CookRoleKept", cook != null && roles.For(cook) != null && roles.For(cook).role == R("cook"), "cook", cook == null || roles.For(cook) == null ? "none" : roles.For(cook).role.defName);
        C("TrooperRoleKept", trooper != null && roles.For(trooper) != null && roles.For(trooper).role == R("trooper"), "trooper", trooper == null || roles.For(trooper) == null ? "none" : roles.For(trooper).role.defName);
        C("CookPriorityKept", cook != null && cook.workSettings.GetPriority(CookingWork) == 1, 1, cook == null ? -1 : cook.workSettings.GetPriority(CookingWork));
        Finish(null, true);
    }

    private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    private void Finish(Exception ex, bool reload)
    {
        done = true;
        bool passed = ex == null && checks.Count > 0 && checks.All(c => c.passed);
        var cj = checks.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}", J(c.name), c.passed ? "true" : "false", J(c.expected), J(c.actual)));
        string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks.Count + ",\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
            "\n  ],\n  \"errors\": [" + (ex == null ? "" : "\"" + J(ex.ToString()) + "\"") + "]\n}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, reload ? "ColonyRolesReloadProbe.json" : "ColonyRolesProbe.json"), json);
        if (!reload) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "ColonyRolesUiProbe.json"), "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":false}");
    }
}
