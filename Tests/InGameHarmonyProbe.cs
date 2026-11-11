using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Verse;

public sealed class HarmonyProbe : MapComponent
{
    private bool reported, uiReported;
    private int checks, patched;
    private string runtimeStatus, version;
    private readonly List<string> errors = new List<string>();
    public HarmonyProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Target(int value) { return value + 1; }
    private static bool Prefix(int value, ref int __result) { if (value >= 0) return true; __result = -10; return false; }
    private static void Postfix(ref int __result) { __result *= 2; }
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (var i in instructions) { if (i.opcode == OpCodes.Ldc_I4_1) { i.opcode = OpCodes.Ldc_I4_3; replaced++; } yield return i; }
        if (replaced != 1) throw new Exception("Unexpected probe IL");
    }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-harmony-probe")) return;
        reported = true;
        try
        {
            bool original = ModsConfig.IsActive("brrainz.harmony");
            var mod = LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "HarmonyMod.Main");
            Check(mod.Content.PackageId == (original ? "brrainz.harmony" : "colonyproject.integrated"), "Wrong Harmony owner");
            var assembly = typeof(Harmony).Assembly; version = assembly.GetName().Version.ToString();
            foreach (string name in new[] { "0Harmony", "HarmonyMod", "HarmonyMod.RuntimeIL" })
            {
                var loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == name).ToArray();
                Check(loaded.Length == 1, "Missing/duplicate Harmony module " + name);
                string folder = original ? "1.7/Assemblies/" : "Components/Harmony/1.7/Assemblies/";
                Check(string.Equals(Path.GetFullPath(loaded[0].Location), Path.GetFullPath(Path.Combine(mod.Content.RootDir, folder, name + ".dll")), StringComparison.OrdinalIgnoreCase), "Wrong Harmony version/path " + name);
            }
            Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "HarmonyLib" || a.GetName().Name == "Lib.Harmony"), "Unexpected alternative Harmony provider");
            Check(AccessTools.Field(mod.GetType(), "loadingError").GetValue(null) == null, "Harmony's own audit failed");
            runtimeStatus = ((Func<string>)AccessTools.Field(AccessTools.TypeByName("HarmonyMod.RuntimeILRegistration"), "status").GetValue(null))();
            Check(!string.IsNullOrEmpty(runtimeStatus) && runtimeStatus.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) < 0, "RuntimeIL provider disabled");
            Check(System.Text.RegularExpressions.Regex.IsMatch(runtimeStatus, @"broken=0(?:,|$)"), "RuntimeIL reports broken patches");
            var instance = new Harmony("turbomegamod.test.harmony"); var target = AccessTools.Method(typeof(HarmonyProbe), "Target");
            Check(Target(3) == 4, "Probe baseline failed");
            try
            {
                instance.Patch(target, prefix: new HarmonyMethod(typeof(HarmonyProbe), "Prefix"), postfix: new HarmonyMethod(typeof(HarmonyProbe), "Postfix"), transpiler: new HarmonyMethod(typeof(HarmonyProbe), "Transpiler"));
                Check(Target(3) == 12, "Real prefix/postfix/transpiler chain failed");
                Check(Target(-1) == -20, "Prefix skip-original failed");
                Check(Harmony.GetPatchInfo(target).Owners.Contains(instance.Id), "Patch owner not registered");
            }
            finally { instance.Unpatch(target, HarmonyPatchType.All, instance.Id); }
            Check(Target(3) == 4 && Target(-1) == 0, "Unpatch did not restore original method");
            Check(Harmony.GetPatchInfo(target) == null || !Harmony.GetPatchInfo(target).Owners.Contains(instance.Id), "Probe patches leaked");
            var settings = (ModSettings)AccessTools.Field(mod.GetType(), "settings").GetValue(null);
            var flag = AccessTools.Field(settings.GetType(), "noStacktraceCaching"); bool old = (bool)flag.GetValue(settings);
            try
            {
                flag.SetValue(settings, !old); string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "HarmonySettings.xml");
                Scribe.saver.InitSaving(path, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); flag.SetValue(settings, old);
                Scribe.loader.InitLoading(path); settings.ExposeData(); Scribe.loader.FinalizeLoading();
                Check((bool)flag.GetValue(settings) == !old, "Harmony settings roundtrip failed");
            }
            finally { flag.SetValue(settings, old); }
            patched = Harmony.GetAllPatchedMethods().Count(); Check(patched > 100, "Consumers did not install their patches");
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HarmonyProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"version\":" + Escape(version) + ",\"runtimeProviderStatus\":" + Escape(runtimeStatus) + ",\"patchedMethods\":" + patched + ",\"realPatchAndUnpatch\":true,\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-harmony-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null;
        try { AccessTools.Method(AccessTools.TypeByName("HarmonyMod.VersionControl_DrawInfoInCorner_Patch"), "Postfix").Invoke(null, null); }
        catch (Exception e) { error = e.ToString(); }
        finally { Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HarmonyUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"nativeVersionInfoDrawInvokedInRepaint\":true,\"hasCustomSettingsWindow\":false,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
