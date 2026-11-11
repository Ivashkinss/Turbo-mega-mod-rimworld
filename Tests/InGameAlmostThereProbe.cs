// Test-only: actual caravan comp/gizmo/save code and original night-rest postfix.
// Only vanilla rest/ETA inputs are controlled; the mod's postfix is never replaced.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CaravanDontRest;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

public sealed class AlmostThereProbe : MapComponent
{
    private bool reported, uiReported;
    private static bool baseline;
    private static int eta, restTicks;
    private static int Hours
    {
        get { return (int)AccessTools.Field(AccessTools.TypeByName("CaravanDontRest.AlmostThereSettings"), "AlmostThereHours").GetValue(null); }
        set { AccessTools.Field(AccessTools.TypeByName("CaravanDontRest.AlmostThereSettings"), "AlmostThereHours").SetValue(null, value); }
    }
    private static bool RestInput(ref bool __result) { __result = baseline; return false; }
    private static bool EtaInput(ref int __result) { __result = eta; return false; }
    private static bool RestTicksInput(ref int __result) { __result = restTicks; return false; }
    public AlmostThereProbe(Map map) : base(map) { }
    private static string Escape(string text)
    { return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-almostthere-probe")) return;
        reported = true;
        var errors = new List<string>();
        var harmony = new Harmony("tmm.diagnostic.almostthere");
        var patched = new List<MethodBase>();
        int originalHours = Hours;
        int checks = 0;
        try
        {
            Assembly assembly = typeof(AlmostThereMod).Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "CaravanDontRest") == 1, "Duplicate caravan DLL"); checks++;
            var mod = LoadedModManager.GetMod<AlmostThereMod>();
            bool originalActive = ModsConfig.IsActive("duz.almosttherefork");
            string expectedId = originalActive ? "duz.almosttherefork" : "colonyproject.integrated";
            Check(mod.Content.PackageId == expectedId, "Wrong ModContentPack"); checks++;
            string root = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == expectedId).RootDir;
            string expectedDll = Path.Combine(root, originalActive ? "1.6/Assemblies/CaravanDontRest.dll" : "Components/AlmostThere/Standard/Assemblies/CaravanDontRest.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expectedDll), StringComparison.OrdinalIgnoreCase), "Unexpected caravan DLL origin"); checks++;
            Check(mod.SettingsCategory() == "Отдых караванов", "Untranslated settings title"); checks++;
            Check(WorldObjectDefOf.Caravan.comps.Count(c => c.compClass == typeof(CompNightRestControl)) == 1, "Expected one caravan rest comp in Def"); checks++;

            Caravan caravan = (Caravan)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Caravan);
            // Use the runtime tile type, including 1.7's PlanetTile wrapper.
            AccessTools.Property(typeof(WorldObject), "Tile").SetValue(caravan, AccessTools.Property(typeof(Map), "Tile").GetValue(map, null), null);
            CompNightRestControl comp = caravan.GetComponent<CompNightRestControl>();
            Check(comp != null, "Caravan did not instantiate the rest comp"); checks++;
            Hours = 4;
            for (int mode = 0; mode < 3; mode++)
            {
                comp.AlmostThere = mode;
                Command_Toggle gizmo = comp.GetGizmos().OfType<Command_Toggle>().Single();
                Check(gizmo.defaultLabel == Translator.Translate("AlmostThereLabel" + mode).RawText && gizmo.icon != null, "Mode label/icon failed: " + mode); checks++;
                Check(!gizmo.defaultDesc.Contains("{0}") && !gizmo.defaultDesc.Contains("AlmostThereDesc"), "Unformatted mode description"); checks++;
                gizmo.toggleAction();
                Check(comp.AlmostThere == (mode + 1) % 3, "Three-mode command cycle failed"); checks++;
            }
            comp.AlmostThere = 1;
            string save = Path.Combine(GenFilePaths.SaveDataFolderPath, "CaravanRestComp.xml");
            Scribe.saver.InitSaving(save, "CaravanRestComp"); comp.PostExposeData(); Scribe.saver.FinalizeSaving();
            comp.AlmostThere = 2;
            Scribe.loader.InitLoading(save); comp.PostExposeData(); Scribe.loader.FinalizeLoading();
            Check(comp.AlmostThere == 1, "Caravan comp state failed Scribe roundtrip"); checks++;
            var settings = (ModSettings)typeof(Mod).GetMethods().Single(m => m.Name == "GetSettings" && m.IsGenericMethodDefinition)
                .MakeGenericMethod(AccessTools.TypeByName("CaravanDontRest.AlmostThereSettings")).Invoke(mod, null);
            string settingsSave = Path.Combine(GenFilePaths.SaveDataFolderPath, "CaravanRestSettings.xml");
            Scribe.saver.InitSaving(settingsSave, "CaravanRestSettings"); settings.ExposeData(); Scribe.saver.FinalizeSaving();
            Hours = 1;
            Scribe.loader.InitLoading(settingsSave); settings.ExposeData(); Scribe.loader.FinalizeLoading();
            Check(Hours == 4, "Hour threshold failed Scribe roundtrip"); checks++;

            MethodInfo night = AccessTools.PropertyGetter(typeof(Caravan), "NightResting");
            Check(Harmony.GetPatchInfo(night).Postfixes.Any(p => p.PatchMethod.Module.Assembly == assembly), "Original rest postfix missing"); checks++;
            harmony.Patch(night, prefix: new HarmonyMethod(typeof(AlmostThereProbe), "RestInput")); patched.Add(night);
            // 1.7 keeps multiple tile/compatibility overloads. Control all
            // vanilla ETA/rest inputs rather than assuming a unique overload.
            foreach (var arrival in AccessTools.GetDeclaredMethods(typeof(CaravanArrivalTimeEstimator))
                .Where(m => m.Name == "EstimatedTicksToArrive" && m.ReturnType == typeof(int)))
            { harmony.Patch(arrival, prefix: new HarmonyMethod(typeof(AlmostThereProbe), "EtaInput")); patched.Add(arrival); }
            foreach (var leftRest in AccessTools.GetDeclaredMethods(typeof(CaravanNightRestUtility))
                .Where(m => m.Name == "LeftRestTicksAt" && m.ReturnType == typeof(int)))
            { harmony.Patch(leftRest, prefix: new HarmonyMethod(typeof(AlmostThereProbe), "RestTicksInput")); patched.Add(leftRest); }
            baseline = true; restTicks = 5000;
            comp.AlmostThere = 2;
            Check((bool)night.Invoke(caravan, null), "Normal mode lost nighttime rest"); checks++;
            comp.AlmostThere = 1;
            Check(!(bool)night.Invoke(caravan, null), "Never-rest mode did not suppress rest"); checks++;
            comp.AlmostThere = 0;
            eta = 14999; Check(!(bool)night.Invoke(caravan, null), "Almost-there mode failed below the threshold"); checks++;
            eta = 15000; Check((bool)night.Invoke(caravan, null), "Exact four-hour boundary should still rest"); checks++;
            eta = 15001; Check((bool)night.Invoke(caravan, null), "Above-threshold caravan should still rest"); checks++;
            baseline = false;
            for (int mode = 0; mode < 3; mode++) { comp.AlmostThere = mode; Check(!(bool)night.Invoke(caravan, null), "Daytime rest was introduced"); checks++; }

            // Exercise the actual 1.7 LoadFolder evaluator for all four
            // original/VF combinations without altering the active mod list.
            Type folderType = AccessTools.TypeByName("Verse.LoadFolder");
            for (int original = 0; original < 2; original++)
                for (int vehicles = 0; vehicles < 2; vehicles++)
                {
                    string originalId = original == 1 ? "ludeon.rimworld" : "tmm.test.absent.original";
                    string vehicleId = vehicles == 1 ? "colonyproject.integrated" : "tmm.test.absent.vehicles";
                    object standard = Activator.CreateInstance(folderType, new object[] { "probe", null, null, new List<string> { originalId, vehicleId } });
                    object vehicle = Activator.CreateInstance(folderType, new object[] { "probe", new List<string> { vehicleId }, null, new List<string> { originalId } });
                    Check((bool)AccessTools.Property(folderType, "ShouldLoad").GetValue(standard, null) == (original == 0 && vehicles == 0), "Engine standard branch condition mismatch");
                    Check((bool)AccessTools.Property(folderType, "ShouldLoad").GetValue(vehicle, null) == (original == 0 && vehicles == 1), "Engine vehicle branch condition mismatch"); checks++;
                }
        }
        catch (Exception exception) { errors.Add(exception.ToString()); }
        finally
        {
            foreach (MethodBase method in patched) harmony.Unpatch(method, HarmonyPatchType.All, "tmm.diagnostic.almostthere");
            Hours = originalHours;
        }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "AlmostThereProbe.json"),
            "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks
            + ",\"restAndEtaInputsControlled\":true,\"vehicleFrameworkRuntimeVerified\":false,\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }

    public override void MapComponentOnGUI()
    {
        if (uiReported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-almostthere-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true;
        string error = null;
        GUI.BeginGroup(new Rect(50, 50, 900, 650));
        try { LoadedModManager.GetMod<AlmostThereMod>().DoSettingsWindowContents(new Rect(0, 0, 900, 650)); }
        catch (Exception exception) { error = exception.ToString(); }
        finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "AlmostThereUiProbe.json"),
            "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
