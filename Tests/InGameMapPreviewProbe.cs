using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

public sealed class MapPreviewProbe : MapComponent
{
    private bool started, reported, uiReported;
    private int checks, patches, mapCount, pixels;
    private Stopwatch watch;
    private static volatile object preview;
    private static volatile Exception previewError;
    private readonly List<string> errors = new List<string>();
    public MapPreviewProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "MapPreview.MapPreviewMod"); }
    private static void Ready(object result) { preview = result; }
    private static object Read(object target, string name) { var f = AccessTools.Field(target.GetType(), name); return f != null ? f.GetValue(target) : AccessTools.Property(target.GetType(), name).GetValue(target, null); }
    private static void Write(object target, string name, object value) { var f = AccessTools.Field(target.GetType(), name); if (f != null) f.SetValue(target, value); else AccessTools.Property(target.GetType(), name).SetValue(target, value, null); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-preview-probe")) return;
        try
        {
            if (!started)
            {
                started = true; watch = Stopwatch.StartNew(); mapCount = Find.Maps.Count;
                var mod = Mod(); var assembly = mod.GetType().Assembly;
                bool original = ModsConfig.IsActive("m00nl1ght.mappreview");
                Check(mod.Content.PackageId == (original ? "m00nl1ght.mappreview" : "colonyproject.integrated"), "Wrong preview owner");
                foreach (string name in new[] { "LunarLoader", "LunarFramework", "MapPreview", "MapPreviewMod", "HugsLogPublisher" }) Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == name) == 1, "Missing/duplicate Lunar module " + name);
                Check(mod.Content.assemblies.loadedAssemblies.Contains(assembly), "Map Preview module not registered with its owner");
                Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "HarmonyLib") && AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "0Harmony") == 1, "Lunar loaded a duplicate Harmony fallback");
                string root = original ? mod.Content.RootDir : Path.Combine(mod.Content.RootDir, "Components/MapPreview");
                foreach (string type in new[] { "LunarLoader.LunarLoader", "LunarFramework.Bootstrap.LunarMod" })
                {
                    string path = (string)AccessTools.Method(AccessTools.TypeByName(type), "VersionFileIn").Invoke(null, new object[] { mod.Content });
                    Check(string.Equals(Path.GetFullPath(path), Path.GetFullPath(Path.Combine(root, "About/Version.txt")), StringComparison.OrdinalIgnoreCase), "Wrong Lunar version path " + type);
                }
                patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly.GetName().Name.StartsWith("MapPreview")); });
                Check(patches > 5, "Missing preview hooks");
                if (!original) Check(mod.SettingsCategory() == "Предпросмотр карты", "Preview settings title missing");
                var sf = AccessTools.Field(mod.GetType(), "Settings"); var settings = (ModSettings)sf.GetValue(sf.IsStatic ? null : mod);
                object entry = AccessTools.Field(settings.GetType(), "IncludeCaves").GetValue(settings); bool old = (bool)Read(entry, "Value");
                try
                {
                    Write(entry, "Value", !old); string file = Path.Combine(GenFilePaths.SaveDataFolderPath, "PreviewSettings.xml");
                    Scribe.saver.InitSaving(file, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); Write(entry, "Value", old);
                    Scribe.loader.InitLoading(file); settings.ExposeData(); Scribe.loader.FinalizeLoading();
                    Check((bool)Read(entry, "Value") == !old, "Lunar preview settings Scribe failed");
                }
                finally { Write(entry, "Value", old); }
                foreach (string icon in new[] { "ConfirmMP", "RerollWorldSeedMP", "UIPreviewLoadingMP", "UIPreviewResetMP" }) Check(ContentFinder<Texture2D>.Get(icon, false) != null, "Missing preview texture " + icon);
                Type requestType = AccessTools.TypeByName("MapPreview.MapPreviewRequest");
                object request = Activator.CreateInstance(requestType, new object[] { 123456, map.Tile, new IntVec2(75, 75) });
                Write(request, "UseTrueTerrainColors", false);
                var generatorType = AccessTools.TypeByName("MapPreview.MapPreviewGenerator");
                Check((bool)AccessTools.Property(AccessTools.TypeByName("MapPreview.MapPreviewAPI"), "IsReady").GetValue(null, null), "Native preview API not ready");
                var generator = AccessTools.Method(generatorType, "Init").Invoke(null, null);
                Check(generator != null, "Native preview worker did not initialize");
                object promise = AccessTools.Method(generatorType, "QueuePreviewRequest").Invoke(generator, new object[] { request });
                Type actionType = typeof(Action<>).MakeGenericType(AccessTools.TypeByName("MapPreview.MapPreviewResult"));
                var ready = Delegate.CreateDelegate(actionType, AccessTools.Method(typeof(MapPreviewProbe), "Ready"));
                var then = promise.GetType().GetMethods().Single(m => m.Name == "Then" && !m.IsGenericMethodDefinition && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == actionType);
                then.Invoke(promise, new object[] { ready, (Action<Exception>)(e => previewError = e) });
                return;
            }
            if (previewError != null) throw new Exception("Preview worker failed", previewError);
            if (preview == null) { if (watch.Elapsed.TotalSeconds > 80) throw new Exception("Preview worker timed out"); return; }
            var colors = (Color[])Read(preview, "Pixels"); var size = (IntVec2)Read(preview, "TextureSize"); pixels = colors.Length;
            Check(size.x > 0 && size.z > 0 && colors.Length == size.x * size.z, "Wrong preview pixel buffer");
            Check(colors.Any(c => c.a > 0 && c.r + c.g + c.b > 0), "Empty preview colors");
            Check((int)Read(preview, "InvalidCells") == 0, "Preview contains invalid cells");
            Check(Find.Maps.Count == mapCount && Find.CurrentMap == map, "Preview replaced the live map");
            Finish();
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    private void Finish()
    {
        reported = true;
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "PreviewProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"previewPixels\":" + pixels + ",\"patchedMethods\":" + patches + ",\"realWorkerRequest\":true,\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-preview-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(25, 25, 1150, 750));
        try { Mod().DoSettingsWindowContents(new Rect(0, 0, 1100, 700)); }
        catch (Exception e) { error = e.ToString(); }
        finally { GUI.EndGroup(); Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "PreviewUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
