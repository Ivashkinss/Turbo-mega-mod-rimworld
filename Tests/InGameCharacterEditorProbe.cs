using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

public sealed class CharacterEditorProbe : MapComponent
{
    private bool reported, uiReported;
    public CharacterEditorProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "CharacterEditor.CharacterEditorMod"); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-editor-probe")) return;
        reported = true; int checks = 0, patches = 0; var errors = new List<string>();
        try
        {
            var mod = Mod(); var assembly = mod.GetType().Assembly;
            bool original = ModsConfig.IsActive("void.charactereditor");
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "CharacterEditor") == 1, "Duplicate editor"); checks++;
            Check(mod.Content.PackageId == (original ? "void.charactereditor" : "colonyproject.integrated"), "Wrong editor owner"); checks++;
            string root = original ? mod.Content.RootDir : Path.Combine(mod.Content.RootDir, "Components/CharacterEditor");
            string expected = Path.Combine(root, original ? "v1.6/Assemblies/CharacterEditor.dll" : "Assemblies/CharacterEditor.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong editor DLL path"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches > 10, "Missing editor hooks"); checks++;
            Type editor = AccessTools.TypeByName("CharacterEditor.CEditor");
            object api = AccessTools.Property(editor, "API").GetValue(null, null);
            Check(api != null, "Editor API not initialized"); checks++;
            foreach (string file in new[] { "FemaleNames.txt", "MaleNames.txt", "SampleLabelNames.txt" }) { Check(File.ReadAllLines(Path.Combine(root, "Defs", file)).Length > 0, "Missing editor name data " + file); checks++; }
            AccessTools.Method(AccessTools.TypeByName("CharacterEditor.Label"), "AddNames").Invoke(null, new object[] { root }); checks++;
            string age = (string)AccessTools.Field(AccessTools.TypeByName("CharacterEditor.Label"), "BIOAGE").GetValue(null);
            Check(!string.IsNullOrEmpty(age) && Regex.IsMatch(age, "[А-Яа-я]"), "Native editor Russian localization missing"); checks++;
            Check(DefDatabase<JobDef>.GetNamed("EnterZGrave").driverClass.Assembly == assembly, "Editor job definition missing"); checks++;
            var optionsType = AccessTools.TypeByName("CharacterEditor.CEditor+ModOptions");
            var settingKey = Enum.Parse(AccessTools.TypeByName("CharacterEditor.EType"), "Settings");
            var settings = (ModSettings)AccessTools.Method(editor, "Get").MakeGenericMethod(optionsType).Invoke(api, new object[] { settingKey });
            Check(settings != null && settings.GetType().Name == "ModOptions", "Editor settings instance missing"); checks++;
            var settingsType = settings.GetType();
            string settingsFile = (string)AccessTools.Property(settingsType, "PATH_SETTINGS").GetValue(settings, null);
            Check(Path.GetFullPath(settingsFile).StartsWith(Path.GetFullPath(GenFilePaths.SaveDataFolderPath), StringComparison.OrdinalIgnoreCase), "Settings test would leave isolated profile"); checks++;
            var boolsField = AccessTools.Field(settingsType, "dicBool");
            var bools = (IDictionary)boolsField.GetValue(settings); object key = bools.Keys.Cast<object>().First();
            var valueField = AccessTools.Field(bools[key].GetType(), "Value"); bool old = (bool)valueField.GetValue(bools[key]);
            try
            {
                valueField.SetValue(bools[key], !old); AccessTools.Method(settingsType, "SaveSettings").Invoke(settings, null);
                valueField.SetValue(bools[key], old); AccessTools.Method(settingsType, "Load").Invoke(settings, new object[] { settingsFile });
                bools = (IDictionary)boolsField.GetValue(settings);
                Check((bool)valueField.GetValue(bools[key]) == !old, "Native editor settings did not survive disk reload"); checks++;
            }
            finally { bools = (IDictionary)boolsField.GetValue(settings); valueField.SetValue(bools[key], old); AccessTools.Method(settingsType, "SaveSettings").Invoke(settings, null); }
            if (!original)
            {
                Check(mod.SettingsCategory() == "Редактор персонажей", "Editor settings title missing"); checks++;
                var helper = AccessTools.Method(AccessTools.TypeByName("TurboMegaMod.CharacterEditorCompatibility"), "ResourceRoot");
                Check(string.Equals(Path.GetFullPath((string)helper.Invoke(null, new object[] { mod.Content })), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase), "Wrong custom editor root"); checks++;
                var core = LoadedModManager.RunningModsListForReading.First(m => m.PackageId == "ludeon.rimworld");
                Check((string)helper.Invoke(null, new object[] { core }) == core.RootDir, "Unrelated resource root changed"); checks++;
            }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "EditorProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-editor-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; int windows = 0; var before = Find.WindowStack.Windows.ToList(); GUI.BeginGroup(new Rect(25, 25, 1150, 750));
        try
        {
            Type editor = AccessTools.TypeByName("CharacterEditor.CEditor"); object api = AccessTools.Property(editor, "API").GetValue(null, null);
            var pawn = map.mapPawns.FreeColonistsSpawned.First();
            AccessTools.Method(editor, "StartEditor").Invoke(api, new object[] { pawn });
            if (AccessTools.Property(editor, "Pawn").GetValue(api, null) != pawn) throw new Exception("Editor did not select test pawn");
            foreach (var window in Find.WindowStack.Windows.Where(w => !before.Contains(w)).ToList()) { window.DoWindowContents(new Rect(0, 0, 1100, 700)); windows++; }
            if (windows == 0) throw new Exception("Editor did not open a window");
            foreach (var window in Find.WindowStack.Windows.Where(w => !before.Contains(w)).ToList()) window.Close(false);
            Mod().DoSettingsWindowContents(new Rect(0, 0, 1100, 700));
            var settingsWindows = Find.WindowStack.Windows.Where(w => !before.Contains(w)).ToList();
            if (settingsWindows.Count == 0) throw new Exception("Editor settings did not open");
            foreach (var window in settingsWindows) window.DoWindowContents(new Rect(0, 0, 1100, 700));
        }
        catch (Exception e) { error = e.ToString(); }
        finally { foreach (var window in Find.WindowStack.Windows.Where(w => !before.Contains(w)).ToList()) window.Close(false); GUI.EndGroup(); Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "EditorUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"editorWindowsDrawn\":" + windows + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
