using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using UnityEngine;
using Verse;

public sealed class OvernightPolishProbe : MapComponent
{
    private int frames;
    private bool done;
    private readonly List<string> results = new List<string>();
    private readonly List<string> errors = new List<string>();

    public OvernightPolishProbe(Map map) : base(map) { }

    public override void MapComponentUpdate()
    {
        if (done || Find.CurrentMap != map || !map.IsPlayerHome ||
            !Environment.GetCommandLineArgs().Contains("-tmm-overnight-polish-probe")) return;

        try
        {
            switch (++frames)
            {
                case 30:
                    VerifyFactionMaterials();
                    SpawnPortraitPair();
                    break;
                case 55:
                    Capture("ColonistBar-Giant.png");
                    VerifyResearchTab();
                    break;
                case 85:
                    Capture("Research-Wasteland.png");
                    break;
                case 95:
                    Finish();
                    break;
            }
        }
        catch (Exception exception)
        {
            errors.Add(exception.GetType().Name + " " + exception.Message.Replace("\"", "'").Replace("\r", "").Replace("\n", " "));
            Finish();
        }
    }

    private void VerifyFactionMaterials()
    {
        List<Settlement> settlements = Find.WorldObjects.Settlements.Where(s => s.Faction != null &&
            s.Faction.def.defName.StartsWith("TMM_", StringComparison.Ordinal)).ToList();
        Check("TmmSettlementsAvailable", settlements.Count > 0);
        Check("FaintTerritoryIconsHaveReadableTint", settlements.All(s => s.Material.color.a >= 0.99f &&
            Luminance(s.Material.color) >= 0.43f));
        Check("FactionTranslationKeysLoaded", Translator.CanTranslate("TMM_FactionHierarchy_Location_World"));
    }

    private static float Luminance(Color color) { return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f; }

    private void SpawnPortraitPair()
    {
        var drawer = AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawColonist");
        Patches patches = Harmony.GetPatchInfo(drawer);
        Check("ColonistBarHasOwnCameraPatch", patches != null &&
            patches.Transpilers.Any(p => p.PatchMethod.Name == "FitColonistBarPortrait"));

        MethodInfo zoom = AccessTools.Method(typeof(GiantDrawScale), "ColonistBarZoom");
        foreach (string name in new[] { "Baseliner", "TMM_SuperMutant" })
        {
            XenotypeDef xenotype = DefDatabase<XenotypeDef>.GetNamed(name);
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false, forcedXenotype: xenotype,
                developmentalStages: DevelopmentalStage.Adult));
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(map.Center, map, 6), map);
            float factor = GiantDrawScale.FactorFor(pawn);
            float fitted = (float)zoom.Invoke(null, new object[] { 1.28205f, pawn });
            Check(name + "WorldScale", name == "Baseliner" ? Mathf.Approximately(factor, 1f) : factor > 2f);
            Check(name + "PortraitFits", Mathf.Approximately(fitted * factor, 1.28205f));
        }
    }

    private void VerifyResearchTab()
    {
        var tab = DefDatabase<ResearchTabDef>.GetNamed("TMM_Wasteland");
        List<ResearchProjectDef> projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(p => p.tab == tab).ToList();
        Check("WastelandProjectsPresent", projects.Count >= 16);
        Check("WastelandRowsWithinViewport", projects.All(p => p.researchViewY >= 0f && p.researchViewY <= 4.4f));
        Check("WastelandNodesDistinct", projects.Select(p => new Vector2(p.researchViewX, p.researchViewY)).Distinct().Count() == projects.Count);
        MainButtonDef research = DefDatabase<MainButtonDef>.GetNamed("Research");
        Find.MainTabsRoot.SetCurrentTab(research, false);
        ((MainTabWindow_Research)research.TabWindow).CurTab = tab;
    }

    private static void Capture(string name)
    {
        Type capture = AccessTools.TypeByName("UnityEngine.ScreenCapture");
        MethodInfo snapshot = capture == null ? null : AccessTools.Method(capture, "CaptureScreenshot", new[] { typeof(string) });
        if (snapshot != null) snapshot.Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, name) });
    }

    private void Check(string name, bool pass)
    {
        results.Add("{\"name\":\"" + name + "\",\"passed\":" + (pass ? "true" : "false") + "}");
        if (!pass) errors.Add(name);
    }

    private void Finish()
    {
        done = true;
        string passed = errors.Count == 0 ? "true" : "false";
        string report = "{\"passed\":" + passed + ",\"checks\":[" + string.Join(",", results.ToArray()) +
            "],\"errors\":[" + string.Join(",", errors.Select(error => "\"" + error + "\"").ToArray()) + "]}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "OvernightPolishProbe.json"), report);
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "OvernightPolishUiProbe.json"), "{\"passed\":" + passed + "}");
    }
}
