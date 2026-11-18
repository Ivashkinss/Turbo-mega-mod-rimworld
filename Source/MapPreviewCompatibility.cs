using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    internal static class MapPreviewCompatibility
    {
        private static string root;
        private static readonly Harmony Harmony = new Harmony("turbomegamod.mappreview.paths");
        internal static void Apply()
        {
            var pack = LoadedModManager.RunningModsListForReading.Find(m => m.PackageId == "colonyproject.integrated");
            var loader = AccessTools.TypeByName("LunarLoader.LunarLoader");
            if (pack == null || loader == null) return;
            string component = Path.Combine(pack.RootDir, "Components", "MapPreview");
            if (!string.Equals(Path.GetFullPath(loader.Assembly.Location), Path.GetFullPath(Path.Combine(component, "Assemblies", "LunarLoader.dll")), StringComparison.OrdinalIgnoreCase)) return;
            root = component;
            PatchVersion(loader);
            var mAdd = AccessTools.Method(typeof(Verse.WindowStack), "Add", new[] { typeof(Verse.Window) });
            if (mAdd != null) Harmony.Patch(mAdd, prefix: new HarmonyMethod(typeof(MapPreviewCompatibility), "BlockToolbarAdd"));
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssembly;
            AppDomain.CurrentDomain.AssemblyLoad += OnAssembly;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) PatchModule(assembly);
        }
        private static void OnAssembly(object sender, AssemblyLoadEventArgs args) { PatchModule(args.LoadedAssembly); }
        private static void PatchModule(Assembly assembly)
        {
            if (root == null) return;
            if (assembly.GetName().Name == "LunarFramework") PatchVersion(assembly.GetType("LunarFramework.Bootstrap.LunarMod", true));
            if (assembly.GetName().Name == "MapPreviewMod")
            {
                var target = AccessTools.Method(assembly.GetType("MapPreview.MapPreviewMod", true), "SettingsCategory");
                var info = HarmonyLib.Harmony.GetPatchInfo(target);
                if (info == null || !info.Owners.Contains(Harmony.Id)) Harmony.Patch(target, postfix: new HarmonyMethod(typeof(MapPreviewCompatibility), "SettingsTitle"));

                var settingsType = assembly.GetType("MapPreview.MapPreviewSettings", false);
                if (settingsType != null)
                {
                    var toolbarProp = AccessTools.PropertyGetter(settingsType, "ToolbarEnabledNow");
                    if (toolbarProp != null) Harmony.Patch(toolbarProp, prefix: new HarmonyMethod(typeof(MapPreviewCompatibility), "DisableToolbarInPlay"));
                }
            }
        }
        private static void PatchVersion(Type type)
        {
            var target = AccessTools.Method(type, "VersionFileIn");
            if (target == null || target.ReturnType != typeof(string) || target.GetParameters().Length != 1) throw new InvalidOperationException("Lunar version lookup changed");
            var info = HarmonyLib.Harmony.GetPatchInfo(target);
            if (info == null || !info.Owners.Contains(Harmony.Id)) Harmony.Patch(target, postfix: new HarmonyMethod(typeof(MapPreviewCompatibility), "VersionPath"));
        }
        private static void VersionPath(ModContentPack __0, ref string __result)
        {
            if (root != null && __0.PackageId == "colonyproject.integrated") __result = Path.Combine(root, "About", "Version.txt");
        }
        private static void SettingsTitle(Mod __instance, ref string __result)
        {
            if (__instance.Content.PackageId == "colonyproject.integrated") __result = Translator.Translate("TMM.MapPreviewSettings").RawText;
        }
        private static bool DisableToolbarInPlay(ref bool __result)
        {
            if (Current.ProgramState != ProgramState.Entry)
            {
                __result = false;
                return false;
            }
            return true;
        }
        private static bool BlockToolbarAdd(Window window)
        {
            if (window != null && window.GetType().FullName == "MapPreview.MapPreviewToolbar" && Current.ProgramState != ProgramState.Entry)
            {
                return false;
            }
            return true;
        }
    }
}
