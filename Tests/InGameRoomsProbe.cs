// Test-only: actual settings, localized room defs, filth toggle and Scribe roundtrip.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RealisticRoomsRewritten;
using RimWorld;
using Verse;
using UnityEngine;

[StaticConstructorOnStartup]
public static class InGameRoomsProbe
{
    private static readonly string[] Fields = {
        "minSpaceRatherTight", "minSpaceAverageSized", "minSpaceSomewhatSpacious",
        "minSpaceQuiteSpacious", "minSpaceVerySpacious", "minSpaceExtremelySpacious"
    };
    static InGameRoomsProbe()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-rooms-probe")) LongEventHandler.ExecuteWhenFinished(Run);
    }
    private static float[] Values(RealisticRoomsSettings settings)
    { return Fields.Select(f => (float)AccessTools.Field(typeof(RealisticRoomsSettings), f).GetValue(settings)).ToArray(); }
    private static void SetValues(RealisticRoomsSettings settings, float[] values)
    { for (int i = 0; i < Fields.Length; i++) AccessTools.Field(typeof(RealisticRoomsSettings), Fields[i]).SetValue(settings, values[i]); }
    private static float[] Thresholds()
    { return DefDatabase<RoomStatDef>.GetNamed("Space").scoreStages.Skip(1).Select(s => s.minScore).ToArray(); }
    private static bool FilthBeauty(float expected)
    { return new[] { "Filth_Dirt", "Filth_Sand" }.All(name => DefDatabase<ThingDef>.GetNamed(name).statBases.Single(s => s.stat == StatDefOf.Beauty).value == expected); }
    private static string Escape(string value)
    { return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }

    private static void Run()
    {
        var errors = new List<string>();
        bool reproduced = false, roundtrip = false;
        var mod = LoadedModManager.GetMod<RealisticRoomsMod>();
        var settings = mod.GetSettings<RealisticRoomsSettings>();
        float[] original = Values(settings);
        bool originalFilth = settings.filthTweakEnabled;
        MethodInfo apply = AccessTools.Method(typeof(RealisticRoomsModOnStartup), "ApplySettings");
        Type fix = AccessTools.TypeByName("TurboMegaMod.RealisticRoomsCompatibility");
        try
        {
            var assembly = typeof(RealisticRoomsMod).Assembly;
            if (AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == assembly.GetName().Name) != 1)
                errors.Add("Duplicate room assembly");
            bool originalActive = ModsConfig.IsActive("lucifer.realisticrooms");
            string expectedId = originalActive ? "lucifer.realisticrooms" : "colonyproject.integrated";
            if (mod.Content.PackageId != expectedId) errors.Add("Wrong ModContentPack: " + mod.Content.PackageId);
            string root = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == expectedId).RootDir;
            string expectedDll = Path.Combine(root, originalActive ? "1.4/Assemblies/RealisticRoomsRewritten.dll"
                : "Components/RealisticRooms/Assemblies/RealisticRoomsRewritten.dll");
            if (!string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expectedDll), StringComparison.OrdinalIgnoreCase))
                errors.Add("Unexpected room DLL origin");
            if (mod.SettingsCategory() != "Размеры комнат") errors.Add("Settings title was not translated");
            if (!Thresholds().SequenceEqual(original)) errors.Add("Settings were not applied at startup");
            string[] labels = DefDatabase<RoomStatDef>.GetNamed("Space").scoreStages.Select(s => s.label).ToArray();
            if (labels.Length != 7 || labels.Skip(1).Any(s => !s.Any(c => c >= 'А' && c <= 'я')))
                errors.Add("Room-stage labels were not Russian");
            float[] custom = { 8.5f, 18.5f, 30.5f, 52.5f, 88.5f, 180.5f };
            SetValues(settings, custom);
            new Harmony("turbomegamod.realisticrooms").Unpatch(apply, HarmonyPatchType.Postfix, "turbomegamod.realisticrooms");
            apply.Invoke(null, new object[] { settings });
            reproduced = Thresholds().SequenceEqual(original);
            if (!reproduced) errors.Add("Actual DLL localized-label regression was not reproduced");
            AccessTools.Method(fix, "Apply").Invoke(null, null);
            if (!Thresholds().SequenceEqual(custom)) errors.Add("The six custom thresholds were not applied");
            if (!DefDatabase<RoomStatDef>.GetNamed("Space").scoreStages.Select(s => s.label).SequenceEqual(labels))
                errors.Add("Translated room labels changed");
            settings.filthTweakEnabled = false;
            apply.Invoke(null, new object[] { settings });
            if (!FilthBeauty(-12)) errors.Add("Filth toggle off did not restore -12");
            var saving = settings;
            string savePath = Path.Combine(GenFilePaths.SaveDataFolderPath, "RoomsSettingsProbe.xml");
            Scribe.saver.InitSaving(savePath, "RoomsSettingsProbe");
            Scribe_Deep.Look(ref saving, "settings");
            Scribe.saver.FinalizeSaving();
            RealisticRoomsSettings loaded = null;
            Scribe.loader.InitLoading(savePath);
            Scribe_Deep.Look(ref loaded, "settings");
            Scribe.loader.FinalizeLoading();
            roundtrip = loaded != null && Values(loaded).SequenceEqual(custom) && !loaded.filthTweakEnabled;
            if (!roundtrip) errors.Add("Settings failed Scribe roundtrip");
            settings.filthTweakEnabled = true;
            apply.Invoke(null, new object[] { settings });
            if (!FilthBeauty(-4)) errors.Add("Filth toggle on did not restore -4");
            var info = Harmony.GetPatchInfo(apply);
            if (info == null || info.Postfixes.Count(p => p.owner == "turbomegamod.realisticrooms") != 1)
                errors.Add("Expected exactly one room compatibility postfix");
        }
        catch (Exception exception) { errors.Add(exception.ToString()); }
        finally
        {
            SetValues(settings, original);
            settings.filthTweakEnabled = originalFilth;
            AccessTools.Method(fix, "Apply").Invoke(null, null);
        }
        string report = "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"actualDllRegressionReproduced\":" + (reproduced ? "true" : "false")
            + ",\"settingsScribeRoundtrip\":" + (roundtrip ? "true" : "false")
            + ",\"sixThresholdsAndBothFilthStatesChecked\":true,\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "RoomsProbe.json"), report);
        Log.Message("[TMM rooms probe] " + report);
    }
}

public sealed class RoomsSettingsUiProbe : MapComponent
{
    private bool reported;
    public RoomsSettingsUiProbe(Map map) : base(map) { }
    public override void MapComponentOnGUI()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-rooms-probe")) return;
        if (Event.current.type != EventType.Repaint) return;
        reported = true;
        string error = null;
        GUI.BeginGroup(new Rect(50, 50, 900, 650));
        try { LoadedModManager.GetMod<RealisticRoomsMod>().DoSettingsWindowContents(new Rect(0, 0, 900, 650)); }
        catch (Exception exception) { error = exception.ToString(); }
        finally { GUI.EndGroup(); }
        string escaped = error == null ? "null" : "\"" + error.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "RoomsUiProbe.json"),
            "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + escaped + "}");
    }
}
