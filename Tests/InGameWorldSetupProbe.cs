using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using UnityEngine;
using Verse;

[StaticConstructorOnStartup]
public static class WorldSetupProbe
{
    private static bool ready, started, reported;
    private static string[] nativeSites;
    private static Page_SelectStartingSite page;
    private static PlanetTile first, second;
    private static Vector3 firstCamera;
    private static int frames, checks;
    static WorldSetupProbe()
    {
        if (!Environment.GetCommandLineArgs().Contains("-tmm-world-setup-probe")) return;
        var harmony = new Harmony("tmm.test.world.setup");
        harmony.Patch(AccessTools.Method(typeof(UIRoot_Entry), "UIRootOnGUI"), postfix: new HarmonyMethod(typeof(WorldSetupProbe), "Frame"));
        harmony.Patch(AccessTools.Method(typeof(WorldGenStep_Factions), "GenerateFresh"), postfix: new HarmonyMethod(typeof(WorldSetupProbe), "NativeSites"));
        LongEventHandler.ExecuteWhenFinished(() => ready = true);
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    private static string[] Sites()
    {
        return Find.WorldObjects.Settlements.Where(s => s.Faction != null && !s.Faction.IsPlayer && s.Tile.Layer == Find.WorldGrid.Surface)
            .Select(s => s.Tile.ToString()).OrderBy(s => s).ToArray();
    }
    private static void NativeSites() { nativeSites = Sites(); }
    private static void Begin()
    {
        try
        {
            var scenario = DefDatabase<ScenarioDef>.GetNamed("Crashlanded").scenario;
            Page_SelectScenario.BeginScenarioConfiguration(scenario, new Page_SelectScenario());
            Current.Game.storyteller = new Storyteller(DefDatabase<StorytellerDef>.GetNamed("Cassandra"), DefDatabase<DifficultyDef>.GetNamed("Rough"));
            page = new Page_SelectStartingSite();
            var parameters = new Page_CreateWorldParams { next = page };
            page.prev = parameters;
            Find.WindowStack.Add(parameters);
            var seedArg = Environment.GetCommandLineArgs().FirstOrDefault(s => s.StartsWith("-tmm-world-seed="));
            AccessTools.Field(typeof(Page_CreateWorldParams), "seedString").SetValue(parameters, seedArg == null ? "tmm-regions-wide-20261117" : seedArg.Substring(16));
            AccessTools.Method(typeof(Page_CreateWorldParams), "CanDoNext").Invoke(parameters, null);
            started = true;
        }
        catch (Exception ex) { Finish(ex); }
    }
    private static void Frame()
    {
        if (ready && !started && !reported && Event.current.type == EventType.Repaint) { Begin(); return; }
        if (!started || reported || Event.current.type != EventType.Repaint ||
            Current.Game == null || Current.Game.World == null || !Find.WindowStack.IsOpen<Page_SelectStartingSite>()) return;
        try
        {
            frames++;
            if (frames == 1)
            {
                Check(Find.UIRoot is UIRoot_Entry && Current.ProgramState != ProgramState.Playing && Find.CurrentMap == null, "Not the genuine entry world-selection screen");
                Check(PawnOverview.Selected == null && !PawnOverview.Active, "HUD accessed a map selector during entry");
                Check(nativeSites != null && nativeSites.Length > 10 && nativeSites.SequenceEqual(Sites()), "Regional generation compressed or lost the native world site distribution");
                var region = Find.World.GetComponent<RegionalWorld>();
                Check(region.clustered && region.relocated > 0, "Regional assignment did not execute");
                Check(region.distanceAfter < region.distanceBefore, "Regional assignment failed to improve faction geography");
                var sites = Find.WorldObjects.Settlements.Where(s => s.Faction != null && !s.Faction.IsPlayer).ToArray();
                Check(sites.Select(s => s.Tile).Distinct().Count() == sites.Length, "Overlapping settlements after reassignment");
                first = Find.GameInitData.startingTile;
                Check(first.Valid && TileFinder.IsValidTileForNewSettlement(first), "Native landing site is not usable");
                second = sites.OrderByDescending(s => Find.WorldGrid.ApproxDistanceInTiles(first, s.Tile)).First().Tile;
                Find.WorldCameraDriver.JumpTo(first);
                Find.WorldSelector.ClearSelection();
                Find.WorldSelector.SelectedTile = first;
            }
            if (frames == 20)
            {
                Check(Find.WorldInterface.SelectedTile == first, "First site selection failed");
                Check(Find.WindowStack.IsOpen<WorldInspectPane>(), "Real world inspector did not open");
                firstCamera = Find.WorldCamera.transform.position;
                Capture("WorldSetup-First.png");
                Find.WorldCameraDriver.JumpTo(second);
                Find.WorldSelector.ClearSelection();
                Find.WorldSelector.SelectedTile = second;
            }
            if (frames == 80)
            {
                Check(Find.WorldInterface.SelectedTile == second, "Second site selection failed");
                Check((Find.WorldCamera.transform.position - firstCamera).sqrMagnitude > 0.001f, "World camera did not move");
                Check(Find.WindowStack.IsOpen<Page_SelectStartingSite>() && Find.WindowStack.IsOpen<WorldInspectPane>(), "World selection UI closed unexpectedly");
                Capture("WorldSetup-Second.png");
            }
            if (frames == 120) Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }
    private static void Capture(string name)
    {
        AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"), "CaptureScreenshot", new[] { typeof(string) }).Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, name) });
    }
    private static string Q(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Finish(Exception error)
    {
        reported = true;
        var region = Current.Game == null || Current.Game.World == null ? null : Find.World.GetComponent<RegionalWorld>();
        string json = "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks + ",\"entryUiFrames\":" + frames +
            ",\"nativeSiteCount\":" + (nativeSites == null ? 0 : nativeSites.Length) + ",\"meanDistanceBefore\":" + (region == null ? 0 : region.distanceBefore).ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"meanDistanceAfter\":" + (region == null ? 0 : region.distanceAfter).ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"errors\":" + (error == null ? "[]" : "[" + Q(error.ToString()) + "]") + "}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "WorldSetupProbe.json"), json);
    }
}
