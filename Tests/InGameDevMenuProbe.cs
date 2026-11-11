// Test-only: integrated ownership, live Harmony patches, labels and settings persistence.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

public sealed class DevMenuProbe : MapComponent
{
    private bool reported, uiReported;
    public DevMenuProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static Verse.Mod GetMod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "DevInYourLanguage.Mod"); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-devmenu-probe")) return;
        reported = true;
        var errors = new List<string>();
        int patched = 0, checks = 0;
        var originals = new Dictionary<FieldInfo, bool>();
        ModSettings settings = null;
        try
        {
            var mod = GetMod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "DevInYourLanguage") == 1, "Duplicate developer-menu DLL"); checks++;
            bool active = ModsConfig.IsActive("latta.devl10n");
            string id = active ? "latta.devl10n" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong developer-menu content pack"); checks++;
            string root = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == id).RootDir;
            string expected = Path.Combine(root, active ? "1.6/Assemblies/DevInYourLanguage.dll" : "Components/DevInYourLanguage/Assemblies/DevInYourLanguage.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Unexpected DLL origin"); checks++;
            var targets = Harmony.GetAllPatchedMethods().Where(method => {
                var info = Harmony.GetPatchInfo(method);
                return info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Any(p => p.PatchMethod.Module.Assembly == assembly);
            }).ToArray();
            patched = targets.Length;
            Check(patched >= 10, "Developer-menu patch set incomplete"); checks++;
            var label = AccessTools.Method(AccessTools.TypeByName("DevInYourLanguage.H_DebugTabMenu_Settings"), "LegibleFieldName");
            object[] labelArgs = label.GetParameters().Select(p => p.ParameterType == typeof(FieldInfo)
                ? (object)AccessTools.Field(typeof(DebugSettings), "godMode") : null).ToArray();
            object labelResult = label.Invoke(null, labelArgs);
            string actual = labelResult as string ?? labelArgs.OfType<string>().Single();
            Check(actual == Translator.Translate("GodMode").RawText && actual != "God mode", "Developer setting label was not Russian"); checks++;
            Type settingsType = AccessTools.TypeByName("DevInYourLanguage.Settings");
            settings = (ModSettings)typeof(Verse.Mod).GetMethods().Single(m => m.Name == "GetSettings" && m.IsGenericMethodDefinition).MakeGenericMethod(settingsType).Invoke(mod, null);
            foreach (var field in settingsType.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(f => f.FieldType == typeof(bool) && !f.IsInitOnly))
                originals[field] = (bool)field.GetValue(field.IsStatic ? null : settings);
            Check(originals.Count > 0, "Missing settings fields"); checks++;
            foreach (var item in originals) item.Key.SetValue(item.Key.IsStatic ? null : settings, !item.Value);
            string file = Path.Combine(GenFilePaths.SaveDataFolderPath, "DevMenuSettings.xml");
            Scribe.saver.InitSaving(file, "DevMenuSettings"); settings.ExposeData(); Scribe.saver.FinalizeSaving();
            foreach (var item in originals) item.Key.SetValue(item.Key.IsStatic ? null : settings, item.Value);
            Scribe.loader.InitLoading(file); settings.ExposeData(); Scribe.loader.FinalizeLoading();
            Check(originals.All(item => (bool)item.Key.GetValue(item.Key.IsStatic ? null : settings) == !item.Value), "Settings failed Scribe roundtrip"); checks++;
        }
        catch (Exception exception) { errors.Add(exception.ToString()); }
        finally { foreach (var item in originals) item.Key.SetValue(item.Key.IsStatic ? null : settings, item.Value); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DevMenuProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"livePatchedMethods\":" + patched + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-devmenu-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null;
        GUI.BeginGroup(new Rect(50, 50, 900, 650));
        try { GetMod().DoSettingsWindowContents(new Rect(0, 0, 900, 650)); }
        catch (Exception exception) { error = exception.ToString(); }
        finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DevMenuUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
