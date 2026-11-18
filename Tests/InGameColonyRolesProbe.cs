using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using TurboMegaMod;
using Verse;

// Colony roles, stage 1. Fresh: -tmm-colony-roles-probe. Reload: -tmm-colony-roles-reload (save TMM-ColonyRoles).
[StaticConstructorOnStartup]
public static class ColonyRolesProbeLaunch
{
    static ColonyRolesProbeLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-colony-roles-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-ColonyRoles"));
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

        var colonists = map.mapPawns.FreeColonistsSpawned.ToList();
        Pawn cook = colonists.FirstOrDefault(p => !p.WorkTypeIsDisabled(CookingWork) && !p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling));
        Pawn trooper = colonists.FirstOrDefault(p => p != cook && !p.WorkTypeIsDisabled(WorkTypeDefOf.Hunting));
        Pawn third = colonists.FirstOrDefault(p => p != cook && p != trooper);
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
        Command_Action giz = cook.GetGizmos().OfType<Command_Action>().FirstOrDefault(g => g.defaultLabel != null && g.defaultLabel.Contains(R("cook").LabelCap));
        C("RoleGizmoShowsRole", giz != null, "gizmo with role label", giz == null ? "none" : giz.defaultLabel);

        cookId = cook.ThingID; trooperId = trooper.ThingID;
        GameDataSaveLoader.SaveGame("TMM-ColonyRoles");
        C("SaveWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-ColonyRoles.rws")), true, "checked");
        Finish(null, false);
    }

    private void Reload()
    {
        ColonyRoles roles = ColonyRoles.Instance;
        C("AssignmentsLoaded", roles != null && roles.All.Count() == 2, 2, roles == null ? -1 : roles.All.Count());
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
