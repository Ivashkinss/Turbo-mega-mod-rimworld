using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

public sealed class MarksProbe : MapComponent
{
    private bool reported, uiReported;
    public MarksProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static Verse.Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "UsefulMarks.UsefulMarksMod"); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static object Call(object obj, string method, params object[] args) { return AccessTools.Method(obj.GetType(), method).Invoke(obj, args); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-marks-probe")) return;
        var pawn = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Violent)); if (pawn == null) return;
        reported = true; int checks = 0, patches = 0; var errors = new List<string>();
        try
        {
            var mod = Mod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "UsefulColonistBar") == 1, "Duplicate markers assembly"); checks++;
            var menus = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "FloatSubMenu").ToArray();
            Check(menus.Length == 1 && menus[0].GetType("KathanonUtil.BoolIndexedSet`1") != null, "New shared menu API missing or duplicated"); checks++;
            bool original = ModsConfig.IsActive("andromeda.usefulmarks"); string id = original ? "andromeda.usefulmarks" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong owner"); checks++;
            string expected = Path.Combine(mod.Content.RootDir, original ? "1.6/Assemblies/UsefulColonistBar.dll" : "Components/UsefulMarks/Assemblies/UsefulColonistBar.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong markers DLL path"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 5, "Missing label hooks"); checks++;
            Check(mod.SettingsCategory() == "Полезные маркеры", "Untranslated title"); checks++;
            var sf = AccessTools.Field(mod.GetType(), "settings"); var settings = (ModSettings)sf.GetValue(sf.IsStatic ? null : mod);
            var flag = AccessTools.Field(settings.GetType(), "DrawMarksInWorld"); object old = flag.GetValue(flag.IsStatic ? null : settings);
            try
            {
                flag.SetValue(flag.IsStatic ? null : settings, true); string sp = Path.Combine(GenFilePaths.SaveDataFolderPath, "MarkSettings.xml");
                Scribe.saver.InitSaving(sp, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); flag.SetValue(flag.IsStatic ? null : settings, false);
                Scribe.loader.InitLoading(sp); settings.ExposeData(); Scribe.loader.FinalizeLoading(); Check((bool)flag.GetValue(flag.IsStatic ? null : settings), "Settings Scribe failed"); checks++;
            }
            finally { flag.SetValue(flag.IsStatic ? null : settings, old); }
            var world = Find.World.components.Single(c => c.GetType().FullName == "UsefulMarks.PawnLabelCustomColors_WorldComponent");
            Check(Find.World.components.Count(c => c.GetType() == world.GetType()) == 1, "Duplicate marker world state"); checks++;
            var markers = (IList)AccessTools.Field(world.GetType(), "UserMarkers").GetValue(world); var before = markers.Cast<object>().ToList(); Call(world, "CreateNewMarker");
            Check(markers.Count == before.Count + 1, "Marker not created"); checks++;
            object marker = markers.Cast<object>().Single(m => !before.Contains(m)); int index = markers.IndexOf(marker);
            Call(world, "SetMarkerDescription", index, "Проверка маркера"); Call(world, "SetMarkerIcon", index, "Star");
            Check(AccessTools.Field(marker.GetType(), "icon").GetValue(marker) != null, "Marker icon not loaded"); checks++;
            Call(world, "ToggleAssign", pawn, index); Check((bool)Call(world, "IsUserAssigned", index, pawn) && (bool)Call(world, "HasUserTags", pawn), "Marker not assigned"); checks++;
            Call(world, "ToggleAssign", pawn, index); Check(!(bool)Call(world, "IsUserAssigned", index, pawn), "Marker not unassigned"); checks++;
            Type meleeType = AccessTools.TypeByName("UsefulMarks.MarkerConditionMelee"), rangedType = AccessTools.TypeByName("UsefulMarks.MarkerConditionRanged");
            object[] ctorArgs = { 0, true, false, false, false }; Type[] ctorTypes = { typeof(int), typeof(bool), typeof(bool), typeof(bool), typeof(bool) };
            object melee = AccessTools.Constructor(meleeType, ctorTypes).Invoke(ctorArgs), ranged = AccessTools.Constructor(rangedType, ctorTypes).Invoke(ctorArgs);
            pawn.jobs.StopAll(); pawn.equipment.DestroyAllEquipment(); var knife = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MeleeWeapon_Knife"), ThingDefOf.Steel); pawn.equipment.AddEquipment(knife);
            Check((bool)Call(melee, "SolveFor", pawn) && !(bool)Call(ranged, "SolveFor", pawn), "Melee weapon conditions failed"); checks++;
            pawn.equipment.DestroyAllEquipment(); var gun = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol")); pawn.equipment.AddEquipment(gun);
            Check(!(bool)Call(melee, "SolveFor", pawn) && (bool)Call(ranged, "SolveFor", pawn), "Ranged weapon conditions failed"); checks++;
            var conditionsField = AccessTools.Field(marker.GetType(), "conditions"); var conditions = (IList)conditionsField.GetValue(marker);
            if (conditions == null) { conditions = (IList)Activator.CreateInstance(conditionsField.FieldType); conditionsField.SetValue(marker, conditions); }
            conditions.Add(melee); IExposable saved = (IExposable)marker; string mp = Path.Combine(GenFilePaths.SaveDataFolderPath, "SavedMarker.xml");
            Scribe.saver.InitSaving(mp, "Marker"); Scribe_Deep.Look(ref saved, "marker"); Scribe.saver.FinalizeSaving(); saved = null;
            Scribe.loader.InitLoading(mp); Scribe_Deep.Look(ref saved, "marker"); Scribe.loader.FinalizeLoading();
            Check(saved != null && (string)AccessTools.Field(saved.GetType(), "description").GetValue(saved) == "Проверка маркера" && (string)AccessTools.Field(saved.GetType(), "iconName").GetValue(saved) == "Star", "Marker data Scribe failed"); checks++;
            var restored = (IList)AccessTools.Field(saved.GetType(), "conditions").GetValue(saved);
            Check(restored.Count == 1 && restored[0].GetType() == meleeType && (int)AccessTools.Field(meleeType, "SelectedParam").GetValue(restored[0]) == 0, "Condition Scribe failed"); checks++;
            foreach (string icon in new[] { "Star", "Melee", "Range", "Worker" }) { Check(ContentFinder<Texture2D>.Get("Marks/" + icon, false) != null, "Missing marker texture " + icon); checks++; }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MarksProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-marks-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(30, 30, 1150, 740));
        try { Mod().DoSettingsWindowContents(new Rect(0, 0, 1000, 650)); var dialog = (Window)Activator.CreateInstance(AccessTools.TypeByName("UsefulColonistBar.Dialog_EditMarkers"), true); dialog.DoWindowContents(new Rect(0, 0, 1100, 700)); }
        catch (Exception e) { error = e.ToString(); } finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MarksUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsAndMarkerEditorDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
