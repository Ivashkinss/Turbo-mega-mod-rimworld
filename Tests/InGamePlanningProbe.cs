using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

// Disposable map designations and separate Scribe records; no user save loaded.
public sealed class PlanningProbe : MapComponent
{
    private bool reported, uiReported;
    public PlanningProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static Verse.Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "MorePlanning.MorePlanningMod"); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-planning-probe")) return;
        reported = true; int checks = 0, patches = 0; var errors = new List<string>(); string[] apiVersions = new string[0];
        // An imported historical News entry opens HugsLib's normal first-run
        // window. Dismiss that exact window only in this disposable profile.
        var news = Find.WindowStack.Windows.Where(w => w.GetType().FullName == "HugsLib.News.Dialog_UpdateFeatures").ToList();
        foreach (var window in news) window.Close(false);
        try
        {
            var mod = Mod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "MorePlanning") == 1, "Duplicate MorePlanning"); checks++;
            var api = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "0MultiplayerAPI").ToArray();
            apiVersions = api.Select(a => a.GetName().Version.ToString()).OrderBy(v => v).ToArray();
            Check(api.Length > 0 && api.Select(a => a.FullName).Distinct().Count() == api.Length, "Duplicate Multiplayer API identity"); checks++;
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(AccessTools.TypeByName("MorePlanning.Patches.MultiplayerCompatibility").TypeHandle); checks++;
            bool original = ModsConfig.IsActive("mlie.moreplanning"); string id = original ? "mlie.moreplanning" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong owner"); checks++;
            string expected = Path.Combine(mod.Content.RootDir, original ? "1.6/Assemblies/MorePlanning.dll" : "Components/MorePlanning/Assemblies/MorePlanning.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong DLL path"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 3, "Missing planning hooks"); checks++;
            Check(mod.SettingsCategory() == "Цветная планировка", "Untranslated title"); checks++;
            var settingsField = AccessTools.Field(mod.GetType(), "Settings"); var settings = (ModSettings)settingsField.GetValue(settingsField.IsStatic ? null : mod);
            var opacity = AccessTools.Field(settings.GetType(), "PlanOpacity"); object old = opacity.GetValue(settings);
            try
            {
                opacity.SetValue(settings, 35); string file = Path.Combine(GenFilePaths.SaveDataFolderPath, "PlanningSettings.xml");
                Scribe.saver.InitSaving(file, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); opacity.SetValue(settings, 90);
                Scribe.loader.InitLoading(file); settings.ExposeData(); Scribe.loader.FinalizeLoading(); Check(Convert.ToInt32(opacity.GetValue(settings)) == 35, "Settings Scribe failed"); checks++;
            }
            finally { opacity.SetValue(settings, old); }
            var visibility = AccessTools.Property(mod.GetType(), "PlanningVisibility"); bool wasVisible = (bool)visibility.GetValue(mod, null);
            visibility.SetValue(mod, !wasVisible, null); Check((bool)visibility.GetValue(mod, null) != wasVisible, "Visibility did not toggle"); checks++;
            var wc = Find.World.components.Single(c => c.GetType().FullName == "MorePlanning.MorePlanningWorldComponent");
            string wp = Path.Combine(GenFilePaths.SaveDataFolderPath, "PlanningWorld.xml");
            Scribe.saver.InitSaving(wp, "WorldComp"); wc.ExposeData(); Scribe.saver.FinalizeSaving(); visibility.SetValue(mod, wasVisible, null);
            Scribe.loader.InitLoading(wp); wc.ExposeData(); Scribe.loader.FinalizeLoading(); Check((bool)visibility.GetValue(mod, null) != wasVisible, "Visibility Scribe failed"); checks++; visibility.SetValue(mod, wasVisible, null);
            var category = DefDatabase<DesignationCategoryDef>.GetNamed("Planning");
            // The original postfix adds its palette when Architect caches its
            // panels; an unopened category alone has only its XML commands.
            var architect = MainButtonDefOf.Architect.TabWindow;
            AccessTools.Method(typeof(MainTabWindow_Architect), "CacheDesPanels").Invoke(architect, null);
            int colorCount = category.AllResolvedDesignators.Count(d => d.GetType().FullName == "MorePlanning.Designators.SelectColorDesignator");
            Check(colorCount == 10, "Color designators missing/duplicated: " + colorCount); checks++;
            var def = DefDatabase<DesignationDef>.GetNamed("MP_Plan");
            var color = AccessTools.Field(mod.GetType(), "SelectedColor"); object oldColor = color.GetValue(color.IsStatic ? null : mod); color.SetValue(color.IsStatic ? null : mod, 3);
            IntVec3 planCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);
            var add = (Designator)Activator.CreateInstance(AccessTools.TypeByName("MorePlanning.Designators.AddDesignator"));
            try { add.DesignateSingleCell(planCell); } finally { color.SetValue(color.IsStatic ? null : mod, oldColor); }
            Designation designation = map.designationManager.DesignationAt(planCell, def); Check(designation != null && designation.GetType().FullName == "MorePlanning.PlanDesignation", "Colored plan not placed"); checks++;
            Check((int)AccessTools.Field(designation.GetType(), "Color").GetValue(designation) == 3, "Wrong plan color"); checks++;
            string dp = Path.Combine(GenFilePaths.SaveDataFolderPath, "PlanningDesignation.xml"); Designation saved = designation;
            Scribe.saver.InitSaving(dp, "Designation"); Scribe_Deep.Look(ref saved, "plan"); Scribe.saver.FinalizeSaving(); saved = null;
            Scribe.loader.InitLoading(dp); Scribe_Deep.Look(ref saved, "plan"); Scribe.loader.FinalizeLoading();
            Check(saved != null && saved.target.Cell == planCell && saved.def == def && (int)AccessTools.Field(saved.GetType(), "Color").GetValue(saved) == 3, "Colored designation Scribe failed"); checks++;
            var remove = (Designator)Activator.CreateInstance(AccessTools.TypeByName("MorePlanning.Designators.RemoveDesignator")); remove.DesignateSingleCell(planCell);
            Check(map.designationManager.DesignationAt(planCell, def) == null, "Plan not removed"); checks++;
            foreach (string icon in new[] { "PlanBase", "PlanCopy", "PlanCut", "PlanPaste", "PlanVisible", "Opacity" }) { Check(ContentFinder<Texture2D>.Get("UI/" + icon, false) != null, "Missing icon " + icon); checks++; }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "PlanningProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"loadedMultiplayerApiVersions\":[" + string.Join(",", apiVersions.Select(Escape)) + "],\"multiplayerSessionTested\":false,\"newsWindowsDismissedInTest\":" + news.Count + ",\"paused\":" + (Find.TickManager.Paused ? "true" : "false") + ",\"openWindows\":[" + string.Join(",", Find.WindowStack.Windows.Select(w => Escape(w.GetType().FullName))) + "],\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-planning-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(50, 50, 1000, 700));
        try { Mod().DoSettingsWindowContents(new Rect(0, 0, 1000, 700)); } catch (Exception e) { error = e.ToString(); } finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "PlanningUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
