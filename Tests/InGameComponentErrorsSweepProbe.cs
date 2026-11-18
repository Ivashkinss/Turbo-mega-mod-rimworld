using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public static class ComponentErrorsSweepProbeLaunch
{
    static ComponentErrorsSweepProbeLaunch()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps == null) return;
            Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
            if (home != null && home.GetComponent<ComponentErrorsSweepProbe>() == null)
            {
                home.components.Add(new ComponentErrorsSweepProbe(home));
            }
        });
    }
}

public sealed class ComponentErrorsSweepProbe : MapComponent
{
    private sealed class Check
    {
        public string name, expected, actual;
        public bool passed;
    }

    private readonly List<Check> checks = new List<Check>();
    private readonly List<string> facts = new List<string>();
    private bool done;
    private int frames;

    public ComponentErrorsSweepProbe(Map map) : base(map) { }

    private void C(string name, bool ok, object expected, object actual)
    {
        checks.Add(new Check
        {
            name = name,
            passed = ok,
            expected = Convert.ToString(expected),
            actual = Convert.ToString(actual)
        });
    }

    public override void MapComponentUpdate()
    {
        if (done || !map.IsPlayerHome || !Environment.GetCommandLineArgs().Contains("-tmm-component-errors-sweep-probe")) return;
        if (++frames < 60) return;
        try
        {
            VerifyItem1_VefWorkGiverDesync();
            VerifyItem2_PickUpAndHaulCount();
            VerifyItem3_MapPreviewToolbarInPlay();
            VerifyItem4_FactionSettlementCounts();
            Finish(null);
        }
        catch (Exception ex)
        {
            Finish(ex);
        }
    }

    private void VerifyItem1_VefWorkGiverDesync()
    {
        // 1. VEF.Pawns.WorkGiver_ConstructionSkill_DeliverResourcesToBlueprints desync fix
        Type wgBlueprintsType = GenTypes.GetTypeInAnyAssembly("VEF.Pawns.WorkGiver_ConstructionSkill_DeliverResourcesToBlueprints");
        Type wgFramesType = GenTypes.GetTypeInAnyAssembly("VEF.Pawns.WorkGiver_ConstructionSkill_DeliverResourcesToFrames");
        Type wgFinishType = GenTypes.GetTypeInAnyAssembly("VEF.Pawns.WorkGiver_ConstructionSkill_FinishFrames");

        C("VEF_TypesLoaded", wgBlueprintsType != null && wgFramesType != null && wgFinishType != null, true, wgBlueprintsType != null);

        Pawn pawn = map.mapPawns.FreeColonists.FirstOrDefault(p => !p.WorkTypeIsDisabled(WorkTypeDefOf.Construction));
        if (pawn == null)
        {
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true));
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(map.Center, map, 5), map);
        }
        if (pawn.workSettings != null)
        {
            pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
            pawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
        }

        // Spawn a normal wall blueprint (which has NO VEF ThingDefExtension)
        ThingDef wallDef = ThingDefOf.Wall;
        ThingDef stuff = ThingDefOf.WoodLog;
        IntVec3 cell1 = CellFinder.RandomClosewalkCellNear(map.Center, map, 4);
        Blueprint_Build blueprint = GenConstruct.PlaceBlueprintForBuild(wallDef, cell1, map, Rot4.North, Faction.OfPlayer, stuff);
        C("BlueprintSpawned", blueprint != null, true, blueprint != null);

        // Spawn wood logs nearby so construction workgiver can find materials
        Thing wood = ThingMaker.MakeThing(stuff);
        wood.stackCount = 50;
        GenSpawn.Spawn(wood, CellFinder.RandomClosewalkCellNear(cell1, map, 2), map);

        if (wgBlueprintsType != null && blueprint != null)
        {
            WorkGiverDef wgDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail("VEF_Art_ConstructDeliverResourcesToBlueprints");
            WorkGiver_Scanner wg = (WorkGiver_Scanner)Activator.CreateInstance(wgBlueprintsType);
            if (wgDef != null) wg.def = wgDef;

            bool hasJob = wg.HasJobOnThing(pawn, blueprint, false);
            Job job = wg.JobOnThing(pawn, blueprint, false);

            C("VEF_Blueprints_HasJobOnThing_False", !hasJob, false, hasJob);
            C("VEF_Blueprints_JobOnThing_Null", job == null, "null", job == null ? "null" : job.def.defName);
            C("VEF_Blueprints_Synchronized", hasJob == (job != null), true, hasJob == (job != null));
            facts.Add("VEF Blueprints: HasJobOnThing=" + hasJob + ", JobOnThing=" + (job == null ? "null" : job.def.defName));
        }

        // Spawn a normal wall frame
        IntVec3 cell2 = CellFinder.RandomClosewalkCellNear(map.Center, map, 4);
        Frame frame = (Frame)ThingMaker.MakeThing(wallDef.frameDef, stuff);
        frame.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(frame, cell2, map);
        C("FrameSpawned", frame != null, true, frame != null);

        if (wgFramesType != null && frame != null)
        {
            WorkGiverDef wgDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail("VEF_Art_ConstructDeliverResourcesToFrames");
            WorkGiver_Scanner wg = (WorkGiver_Scanner)Activator.CreateInstance(wgFramesType);
            if (wgDef != null) wg.def = wgDef;

            bool hasJob = wg.HasJobOnThing(pawn, frame, false);
            Job job = wg.JobOnThing(pawn, frame, false);

            C("VEF_Frames_HasJobOnThing_False", !hasJob, false, hasJob);
            C("VEF_Frames_JobOnThing_Null", job == null, "null", job == null ? "null" : job.def.defName);
            C("VEF_Frames_Synchronized", hasJob == (job != null), true, hasJob == (job != null));
        }

        if (wgFinishType != null && frame != null)
        {
            WorkGiverDef wgDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail("VEF_Art_ConstructFinishFrames");
            WorkGiver_Scanner wg = (WorkGiver_Scanner)Activator.CreateInstance(wgFinishType);
            if (wgDef != null) wg.def = wgDef;

            bool hasJob = wg.HasJobOnThing(pawn, frame, false);
            Job job = wg.JobOnThing(pawn, frame, false);

            C("VEF_Finish_HasJobOnThing_False", !hasJob, false, hasJob);
            C("VEF_Finish_JobOnThing_Null", job == null, "null", job == null ? "null" : job.def.defName);
            C("VEF_Finish_Synchronized", hasJob == (job != null), true, hasJob == (job != null));
        }

        // Control check: vanilla WorkGiver_ConstructDeliverResourcesToBlueprints still works
        WorkGiver_ConstructDeliverResourcesToBlueprints vanillaWg = new WorkGiver_ConstructDeliverResourcesToBlueprints();
        vanillaWg.def = DefDatabase<WorkGiverDef>.GetNamedSilentFail("ConstructDeliverResourcesToBlueprints");
        if (vanillaWg.def != null && blueprint != null)
        {
            bool vanillaHasJob = vanillaWg.HasJobOnThing(pawn, blueprint, true);
            C("Vanilla_ConstructDeliverResources_Unbroken", vanillaHasJob, true, vanillaHasJob);
        }

        if (blueprint != null && !blueprint.Destroyed) blueprint.Destroy(DestroyMode.Vanish);
        if (frame != null && !frame.Destroyed) frame.Destroy(DestroyMode.Vanish);
        if (wood != null && !wood.Destroyed) wood.Destroy(DestroyMode.Vanish);
    }

    private void VerifyItem2_PickUpAndHaulCount()
    {
        // 2. PickUpAndHaul Invalid count 0 fix
        ThingDef riceDef = DefDatabase<ThingDef>.GetNamedSilentFail("RawRice") ?? ThingDefOf.WoodLog;
        Thing rice = ThingMaker.MakeThing(riceDef);
        rice.stackCount = 75;
        GenSpawn.Spawn(rice, CellFinder.RandomClosewalkCellNear(map.Center, map, 3), map);

        Pawn pawn = map.mapPawns.FreeColonists.FirstOrDefault();
        if (pawn == null)
        {
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true));
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(map.Center, map, 3), map);
        }

        JobDef haulJobDef = DefDatabase<JobDef>.GetNamedSilentFail("HaulToInventory") ?? JobDefOf.HaulToCell;
        Job testJob = new Job(haulJobDef, rice, CellFinder.RandomClosewalkCellNear(map.Center, map, 6));
        testJob.count = 0; // Simulate 0 count trigger
        pawn.jobs.curJob = testJob;

        // Calling ErrorCheckForCarry must not log or fail, and count must be silently corrected >= 1
        bool carryResult = Toils_Haul.ErrorCheckForCarry(pawn, rice, false);
        C("PUAH_CarryCountCorrected", pawn.jobs.curJob.count >= 1, ">=1", pawn.jobs.curJob.count);
        C("PUAH_ErrorCheck_Handled", !carryResult, false, carryResult);
        facts.Add("PUAH testJob.count adjusted from 0 to " + pawn.jobs.curJob.count);

        // Check WorkGiver_HaulToInventory Postfix if available
        Type puahWgType = GenTypes.GetTypeInAnyAssembly("PickUpAndHaul.WorkGiver_HaulToInventory");
        if (puahWgType != null)
        {
            WorkGiver_Scanner puahWg = (WorkGiver_Scanner)Activator.CreateInstance(puahWgType);
            puahWg.def = DefDatabase<WorkGiverDef>.GetNamedSilentFail("HaulToInventory");
            if (puahWg.def != null)
            {
                Job puahJob = puahWg.JobOnThing(pawn, rice, false);
                if (puahJob != null)
                {
                    C("PUAH_JobOnThing_CountPositive", puahJob.count > 0, true, puahJob.count > 0);
                    bool allQueuePositive = puahJob.countQueue == null || puahJob.countQueue.All(c => c > 0);
                    C("PUAH_JobOnThing_CountQueuePositive", allQueuePositive, true, allQueuePositive);
                }
            }
        }
    }

    private void VerifyItem3_MapPreviewToolbarInPlay()
    {
        // 3. MapPreviewToolbar disabled during active gameplay
        C("ProgramState_IsPlaying", Current.ProgramState == ProgramState.Playing, true, Current.ProgramState == ProgramState.Playing);

        Type settingsType = GenTypes.GetTypeInAnyAssembly("MapPreview.MapPreviewSettings");
        Type modType = GenTypes.GetTypeInAnyAssembly("MapPreview.MapPreviewMod");
        Type toolbarType = GenTypes.GetTypeInAnyAssembly("MapPreview.MapPreviewToolbar");
        Type wimType = GenTypes.GetTypeInAnyAssembly("MapPreview.WorldInterfaceManager");

        C("MapPreview_TypesLoaded", settingsType != null && toolbarType != null, true, settingsType != null);

        if (settingsType != null && modType != null)
        {
            FieldInfo fi = modType.GetField("Settings", BindingFlags.Public | BindingFlags.Static);
            object settings = fi != null ? fi.GetValue(null) : null;
            if (settings != null)
            {
                PropertyInfo pi = settingsType.GetProperty("ToolbarEnabledNow", BindingFlags.Public | BindingFlags.Instance);
                bool toolbarEnabled = pi != null && (bool)pi.GetValue(settings, null);
                C("MapPreview_ToolbarEnabledNow_FalseInPlay", !toolbarEnabled, false, toolbarEnabled);
                facts.Add("MapPreview ToolbarEnabledNow in play=" + toolbarEnabled);
            }
        }

        if (wimType != null)
        {
            MethodInfo mi = wimType.GetMethod("get_ToolbarEnabledNow", BindingFlags.NonPublic | BindingFlags.Static);
            if (mi != null)
            {
                bool wimToolbar = (bool)mi.Invoke(null, null);
                C("MapPreview_WIM_ToolbarEnabledNow_FalseInPlay", !wimToolbar, false, wimToolbar);
            }
        }

        // Toolbar instance must not exist in window stack
        Window existing = Find.WindowStack.Windows.FirstOrDefault(w => w.GetType().FullName == "MapPreview.MapPreviewToolbar");
        C("MapPreview_ToolbarNotInWindowStack", existing == null, true, existing == null);

        // Attempting to open it must be blocked by PreOpen
        if (toolbarType != null)
        {
            Window tb = (Window)Activator.CreateInstance(toolbarType);
            Find.WindowStack.Add(tb);
            Window afterAdd = Find.WindowStack.Windows.FirstOrDefault(w => w.GetType().FullName == "MapPreview.MapPreviewToolbar");
            C("MapPreview_ToolbarBlockedFromOpen", afterAdd == null, true, afterAdd == null);
        }
    }

    private void VerifyItem4_FactionSettlementCounts()
    {
        // 4. Every visible surface faction has at least 2 settlements
        List<Faction> visibleFactions = Find.FactionManager.AllFactionsListForReading
            .Where(f => f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.def != null && f.def.settlementGenerationWeight > 0 &&
                (f.def.layerWhitelist.NullOrEmpty() || f.def.layerWhitelist.Contains(Find.WorldGrid.Surface.Def)))
            .OrderBy(f => f.loadID)
            .ToList();

        C("VisibleFactions_Present", visibleFactions.Count >= 5, ">=5", visibleFactions.Count);

        List<Settlement> allSettlements = Find.WorldObjects.Settlements
            .Where(s => s.Faction != null && !s.Faction.IsPlayer && s.Tile.Layer == Find.WorldGrid.Surface)
            .ToList();

        int underpopulated = 0;
        foreach (Faction faction in visibleFactions)
        {
            int count = allSettlements.Count(s => s.Faction == faction);
            facts.Add("Faction " + faction.Name + " (" + faction.def.defName + "): " + count + " settlements");
            if (count < 2) underpopulated++;
        }

        C("AllVisibleFactions_Min2Settlements", underpopulated == 0, 0, underpopulated);

        RegionalWorld rw = Find.World.GetComponent<RegionalWorld>();
        C("RegionalWorld_Clustered", rw != null && rw.clustered, true, rw != null && rw.clustered);

        if (rw != null)
        {
            int capitals = allSettlements.Count(s => rw.For(s).capital);
            C("CapitalsCount_MatchesFactions", capitals == visibleFactions.Count, visibleFactions.Count, capitals);
            facts.Add("RegionalWorld: " + allSettlements.Count + " settlements total, " + capitals + " capitals across " + visibleFactions.Count + " factions");
        }
    }

    private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    private void Finish(Exception ex)
    {
        done = true;
        bool passed = ex == null && checks.Count > 0 && checks.All(c => c.passed);
        var cj = checks.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}", J(c.name), c.passed ? "true" : "false", J(c.expected), J(c.actual)));
        string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks.Count +
            ",\n  \"facts\": [" + string.Join(",", facts.Select(f => "\"" + J(f) + "\"").ToArray()) + "],\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
            "\n  ],\n  \"errors\": [" + (ex == null ? "" : "\"" + J(ex.ToString()) + "\"") + "]\n}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "ComponentErrorsSweepProbe.json"), json);
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "ComponentErrorsSweepUiProbe.json"),
            "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":false,\"note\":\"ComponentErrorsSweep verified\"}");
    }
}
