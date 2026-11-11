// Regression test of the production postfix with real Harmony and localized stages.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Verse
{
    public sealed class StaticConstructorOnStartupAttribute : Attribute { }
    public static class LoadedModManager { public static readonly List<object> ModHandles = new List<object>(); }
    public static class DefDatabase<T> { public static T Definition; public static T GetNamedSilentFail(string name) { return Definition; } }
    public static class Log { public static int Warnings; public static void Warning(string text) { Warnings++; } }
}
namespace RimWorld
{
    public sealed class Stage { public string label; public float minScore; }
    public sealed class RoomStatDef { public List<Stage> scoreStages = new List<Stage>(); }
}
namespace RealisticRoomsRewritten
{
    public sealed class Settings
    {
        public float minSpaceRatherTight = 8.5f, minSpaceAverageSized = 18.5f, minSpaceSomewhatSpacious = 30.5f,
            minSpaceQuiteSpacious = 52.5f, minSpaceVerySpacious = 88.5f, minSpaceExtremelySpacious = 180.5f;
    }
    public sealed class RealisticRoomsMod { public readonly Settings settings = new Settings(); }
    public static class RealisticRoomsModOnStartup
    {
        public static readonly string[] Labels = { "rather tight", "average-sized", "somewhat spacious", "quite spacious", "very spacious", "extremely spacious" };
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ApplySettings(Settings settings)
        {
            // The installed DLL compares each stage label with these exact English strings.
            foreach (var stage in Verse.DefDatabase<RimWorld.RoomStatDef>.Definition.scoreStages)
                for (int i = 0; i < Labels.Length; i++)
                    if (stage.label == Labels[i]) stage.minScore = (float)typeof(Settings)
                        .GetField(TurboMegaMod.RealisticRoomsCompatibility.SettingFields[i]).GetValue(settings);
        }
    }
}
public static class RealisticRoomsCompatibilityTests
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs eventArgs) {
            return new AssemblyName(eventArgs.Name).Name == "0Harmony" ? Assembly.LoadFrom(args[0]) : null;
        };
        Run();
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static float[] Thresholds(RimWorld.RoomStatDef def) { return def.scoreStages.Skip(1).Select(s => s.minScore).ToArray(); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        var def = new RimWorld.RoomStatDef();
        string[] labels = { "тесно", "тесновато", "средний размер", "просторно", "очень просторно", "огромная", "колоссальная" };
        foreach (string label in labels) def.scoreStages.Add(new RimWorld.Stage { label = label, minScore = -99 });
        Verse.DefDatabase<RimWorld.RoomStatDef>.Definition = def;
        var mod = new RealisticRoomsRewritten.RealisticRoomsMod();
        Verse.LoadedModManager.ModHandles.Add(mod);
        var expected = new[] { 8.5f, 18.5f, 30.5f, 52.5f, 88.5f, 180.5f };
        RealisticRoomsRewritten.RealisticRoomsModOnStartup.ApplySettings(mod.settings);
        Check(Thresholds(def).All(x => x == -99), "Original localized-label regression was not reproduced");
        TurboMegaMod.RealisticRoomsCompatibility.Apply();
        Check(Thresholds(def).SequenceEqual(expected), "Startup reapplication failed for the six translated stages");
        Check(def.scoreStages.Select(s => s.label).SequenceEqual(labels) && def.scoreStages[0].minScore == -99, "Stage labels or the lowest stage changed");
        mod.settings.minSpaceRatherTight = 9.5f;
        RealisticRoomsRewritten.RealisticRoomsModOnStartup.ApplySettings(mod.settings);
        Check(def.scoreStages[1].minScore == 9.5f, "Subsequent setting changes were ignored");
        for (int i = 0; i < 6; i++) def.scoreStages[i + 1].label = RealisticRoomsRewritten.RealisticRoomsModOnStartup.Labels[i];
        RealisticRoomsRewritten.RealisticRoomsModOnStartup.ApplySettings(mod.settings);
        Check(def.scoreStages[1].minScore == 9.5f && def.scoreStages[6].minScore == 180.5f, "English stages regressed");
        TurboMegaMod.RealisticRoomsCompatibility.Apply();
        var info = HarmonyLib.Harmony.GetPatchInfo(typeof(RealisticRoomsRewritten.RealisticRoomsModOnStartup).GetMethod("ApplySettings"));
        Check(info.Postfixes.Count(p => p.owner == "turbomegamod.realisticrooms") == 1, "Repeated initialization installed duplicate postfixes");
        def.scoreStages.RemoveAt(6);
        foreach (var stage in def.scoreStages) { stage.label = "локализовано"; stage.minScore = -99; }
        RealisticRoomsRewritten.RealisticRoomsModOnStartup.ApplySettings(mod.settings);
        Check(def.scoreStages.All(s => s.minScore == -99) && Verse.Log.Warnings == 1, "Unexpected stage layouts were overwritten");
        Check(mod.settings.minSpaceRatherTight == 9.5f && mod.settings.minSpaceExtremelySpacious == 180.5f, "Settings were mutated by the compatibility patch");
        Console.WriteLine("{\"regressionReproducedBeforePatch\":true,\"runtimeChecks\":8,\"realHarmony\":true,\"passed\":true}");
    }
}
