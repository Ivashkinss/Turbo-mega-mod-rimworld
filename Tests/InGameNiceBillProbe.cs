using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

// Test-only recipe search, actual bill insertion/serialization and Repaint.
public sealed class NiceBillProbe : MapComponent
{
    private bool reported, uiReported;
    private Building_WorkTable bench;
    private RecipeDef recipe;
    public NiceBillProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static Verse.Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "NiceBillTab.NiceBillTabMod"); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-nicebill-probe")) return;
        reported = true; int checks = 0, patches = 0; var errors = new List<string>();
        try
        {
            var mod = Mod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "NiceBillTab") == 1, "Duplicate NiceBillTab"); checks++;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "FloatSubMenu") == 1, "Duplicate FloatSubMenu runtime assembly"); checks++;
            bool original = ModsConfig.IsActive("andromeda.nicebilltab"); string id = original ? "andromeda.nicebilltab" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong owner"); checks++;
            string expected = Path.Combine(mod.Content.RootDir, original ? "1.6/Assemblies/NiceBillTab.dll" : "Components/NiceBillTab/Assemblies/NiceBillTab.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong DLL origin"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 4, "Missing bill hooks"); checks++;
            Check(mod.SettingsCategory() == "Удобные производственные задания", "Untranslated settings title"); checks++;
            Type settingsType = AccessTools.TypeByName("NiceBillTab.Settings");
            var settings = (ModSettings)typeof(Verse.Mod).GetMethods().Single(m => m.Name == "GetSettings" && m.IsGenericMethodDefinition).MakeGenericMethod(settingsType).Invoke(mod, null);
            var field = AccessTools.Field(settingsType, "CompareBarsWithColors"); object old = field.GetValue(field.IsStatic ? null : settings);
            try
            {
                field.SetValue(field.IsStatic ? null : settings, true); string file = Path.Combine(GenFilePaths.SaveDataFolderPath, "NiceBillSettings.xml");
                Scribe.saver.InitSaving(file, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); field.SetValue(field.IsStatic ? null : settings, false);
                Scribe.loader.InitLoading(file); settings.ExposeData(); Scribe.loader.FinalizeLoading(); Check((bool)field.GetValue(field.IsStatic ? null : settings), "Settings Scribe failed"); checks++;
            }
            finally { field.SetValue(field.IsStatic ? null : settings, old); }
            bench = (Building_WorkTable)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("ElectricStove")); bench.SetFaction(Faction.OfPlayer);
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 15); GenSpawn.Spawn(bench, cell, map);
            recipe = bench.def.AllRecipes.First(r => r.ProducedThingDef == ThingDefOf.MealSimple && r.AvailableNow);
            Type utils = AccessTools.TypeByName("NiceBillTab.Utils"), drawer = AccessTools.TypeByName("NiceBillTab.TabBillsDrawer");
            var available = ((IEnumerable)AccessTools.Method(utils, "GetAvailableRecipes").Invoke(null, new object[] { bench })).Cast<object>().ToList();
            Check(available.Any(x => (RecipeDef)AccessTools.Field(x.GetType(), "recipe").GetValue(x) == recipe), "Simple meal recipe missing"); checks++;
            FieldInfo filter = AccessTools.Field(drawer, "filterText"); object beforeFilter = filter.GetValue(null);
            try
            {
                var matches = AccessTools.Method(drawer, "FilterFlatText");
                filter.SetValue(null, recipe.label.ToUpperInvariant()); Check((bool)matches.Invoke(null, new object[] { recipe.LabelCap.ToString() }), "Russian case-insensitive name filter failed"); checks++;
                filter.SetValue(null, "zzzz_no_matching_recipe_zzzz"); Check(!(bool)matches.Invoke(null, new object[] { recipe.LabelCap.ToString() }), "Unmatched recipe passed filter"); checks++;
                filter.SetValue(null, ""); Check((bool)matches.Invoke(null, new object[] { recipe.LabelCap.ToString() }), "Empty filter hid recipe"); checks++;
            }
            finally { filter.SetValue(null, beforeFilter); }
            AccessTools.Method(utils, "TryAddBillToQueue").Invoke(null, new object[] { bench, recipe, 3, map });
            Check(bench.BillStack.Count == 1 && bench.BillStack.Bills[0].recipe == recipe, "Recipe did not enter the real bill stack"); checks++;
            var production = (Bill_Production)bench.BillStack.Bills[0]; production.repeatCount = 3; production.suspended = true;
            Bill saved = production; string bp = Path.Combine(GenFilePaths.SaveDataFolderPath, "NiceBill.xml");
            Scribe.saver.InitSaving(bp, "Bill"); Scribe_Deep.Look(ref saved, "bill"); Scribe.saver.FinalizeSaving(); saved = null;
            Scribe.loader.InitLoading(bp); Scribe_Deep.Look(ref saved, "bill"); Scribe.loader.FinalizeLoading();
            Check(saved != null && saved.recipe == recipe && saved.suspended && ((Bill_Production)saved).repeatCount == 3, "Bill Scribe failed"); checks++;
            AccessTools.Field(drawer, "LastSelTable").SetValue(null, bench);
            var selection = AccessTools.Method(drawer, "SelectRecipe").Invoke(null, new object[] { recipe, null, false, null, null }); Check(selection != null, "Recipe preview selection failed"); checks++;
            foreach (string name in new[] { "Settings", "NoIconRecipe", "Pause", "Play", "Medicine", "Nutrition" }) { Check(ContentFinder<Texture2D>.Get("NiceBillTab/" + name, false) != null, "Missing icon " + name); checks++; }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "NiceBillProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || bench == null || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-nicebill-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; bool tabDrawn = false; GUI.BeginGroup(new Rect(25, 25, 1200, 750));
        try
        {
            Mod().DoSettingsWindowContents(new Rect(0, 0, 1100, 650));
            Find.Selector.ClearSelection(); Find.Selector.Select(bench);
            var tab = new ITab_Bills(); AccessTools.Method(typeof(ITab_Bills), "FillTab").Invoke(tab, null); tabDrawn = true;
        }
        catch (Exception e) { error = e.ToString(); }
        finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "NiceBillUiProbe.json"), "{\"passed\":" + (error == null && tabDrawn ? "true" : "false") + ",\"settingsAndPatchedBillTabDrawInvokedInRepaint\":" + (tabDrawn ? "true" : "false") + ",\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
