// Exercises the production transpiler with real Harmony and small UI test doubles.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Verse
{
    public class ModContentPack { public string PackageId, RootDir; public string Name { get; set; } }
    public static class LoadedModManager { public static readonly List<ModContentPack> RunningModsListForReading = new List<ModContentPack>(); }
    public class Pawn { }
    public struct PawnDrawParms { public Pawn pawn; public int flags; }
    public class Mod { public ModContentPack Content { get; private set; } public Mod(ModContentPack content) { Content = content; } }
    public static class Log { public static void Message(string message) { } }
    public static class Translator
    {
        public static readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public static string TranslateSimple(string key) { return System.Text.RegularExpressions.Regex.Replace(Values[key], "<[^>]*>", ""); }
        public static TaggedString Translate(string key) { return new TaggedString { RawText = Values[key] }; }
    }
    public struct TaggedString { public string RawText; }
}
namespace AM.AMSettings
{
    public enum CorpseOffsetMode { None, InterpolateToCorrect, KeepOffset }
    public static class SimpleSettings
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string DrawWindow() { return "Loading..."; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Description() { return "<i>No description</i>"; }
        public class MemberWrapper
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ValueToString(object value)
            {
                if (value is float) return string.Format("{0:F0}%", (float)value * 100);
                return value == null ? "<null>" : value.ToString();
            }
        }
    }
}
namespace AM.AutoDuel
{
    public class AutoFriendlyDuelMapComp
    {
        private readonly int mainThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
        public int Count;
        public IEnumerable<Verse.Pawn> EnumeratePossiblePawns()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != mainThread)
                throw new InvalidOperationException("Map pawns enumerated off main thread");
            yield return new Verse.Pawn();
            yield return new Verse.Pawn();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UpdatePawnsThatCanDuel(bool parallel)
        {
            if (parallel)
            {
                var source = EnumeratePossiblePawns();
                System.Threading.Tasks.Task.Run(delegate
                {
                    foreach (var pawn in source) Count++;
                }).Wait();
            }
            else
            {
                foreach (var pawn in EnumeratePossiblePawns()) Count++;
            }
        }
    }
}
namespace AM.Patches
{
    public static class Patch_PawnRenderer_RenderPawnAt
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void MakeDrawArgs(object renderer, Verse.Pawn pawn, ref Verse.PawnDrawParms parms) { }
    }
}
namespace DynamicTradeInterface.UserInterface.Columns
{
    public static class ColumnQuantity
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Tooltip() { return string.Format("{0} available for trading.{1}{2} total on map.", 5, "\n", 12); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Branched(bool loading) { return loading ? "Loading..." : "untouched"; }
    }
}
namespace DynamicTradeInterface.Mod
{
    public static class DynamicTradeInterfaceMod
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string SettingsCategory() { return "Dynamic trade interface"; }
    }
}
namespace SmartSpeed
{
    public static class SmartSpeed_Mod
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string SettingsCategory() { return "Smart Speed"; }
    }
}
namespace DynamicTradeInterface.UserInterface.Columns.ColumnExtraIconTypes
{
    public static class BookDrawable
    {
        public static IEnumerable<string> GetIcons()
        {
            yield return string.Format("{0}/{1} projects researched.", 2, 5);
        }
    }
}
namespace UnrelatedMod
{
    public static class Display
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Draw() { return "Loading..."; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string BubbleTitle() { return "Interaction Bubbles"; }
    }
}
namespace SnapOut
{
    public static class SOMod
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Duration() { return "{0}: {1:F1}h"; }
    }
}
namespace Bubbles
{
    public static class Mod
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string SettingsCategory() { return "Interaction Bubbles"; }
    }
}
namespace CaravanDontRest
{
    public static class AlmostThereMod
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string SettingsCategory() { return "Almost There!"; }
    }
}
namespace Bubbles.Configuration
{
    public static class SettingsEditor
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string DrawSettings() { return " (Experimental)"; }
        public sealed class Dialog
        {
            public readonly string Title;
            [MethodImpl(MethodImplOptions.NoInlining)]
            public Dialog() { Title = "<b>Interaction Bubbles</b>"; }
        }
    }
}
public static class RuntimeLocalizationTests
{
    public static void Main(string[] args)
    {
        string harmony = args[0];
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs eventArgs)
        { return new AssemblyName(eventArgs.Name).Name == "0Harmony" ? Assembly.LoadFrom(harmony) : null; };
        Run(args[1]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string language)
    {
        var xml = new XmlDocument();
        xml.Load(language);
        foreach (XmlElement entry in xml.DocumentElement.ChildNodes)
            Verse.Translator.Values[entry.Name] = entry.InnerText;
        Check(AM.AMSettings.SimpleSettings.DrawWindow() == "Loading...", "Unexpected initial state");
        TurboMegaMod.RuntimeLocalization.Apply();
        Check(AM.AMSettings.SimpleSettings.DrawWindow() == "Загрузка...", "Static display text not translated");
        Check(DynamicTradeInterface.UserInterface.Columns.ColumnQuantity.Tooltip() ==
            "Доступно для торговли: 5.\nВсего на карте: 12.", "Formatting values were damaged");
        Check(DynamicTradeInterface.UserInterface.Columns.ColumnQuantity.Branched(true) == "Загрузка...", "Branch label damaged");
        Check(DynamicTradeInterface.UserInterface.Columns.ColumnQuantity.Branched(false) == "untouched", "Unrelated branch changed");
        Check(UnrelatedMod.Display.Draw() == "Loading...", "Patch leaked into another component");
        var formatter = new AM.AMSettings.SimpleSettings.MemberWrapper();
        Check(formatter.ValueToString(AM.AMSettings.CorpseOffsetMode.None) == "Не смещать", "Enum label not translated");
        Check(formatter.ValueToString(AM.AMSettings.CorpseOffsetMode.KeepOffset) == "Сохранять смещение", "Enum value changed");
        Check(formatter.ValueToString(25) == "25", "Non-enum ToString changed");
        Check(formatter.ValueToString(AM.AMSettings.CorpseOffsetMode.InterpolateToCorrect)
            == "Плавно возвращать на место", "Third enum label not translated");
        Check(DynamicTradeInterface.Mod.DynamicTradeInterfaceMod.SettingsCategory()
            == "Динамическое окно торговли", "Actual settings type not patched");
        foreach (string icon in DynamicTradeInterface.UserInterface.Columns.ColumnExtraIconTypes.BookDrawable.GetIcons())
            Check(icon == "Изучено технологий: 2/5.", "Iterator display text not translated");
        Check(formatter.ValueToString(0.5f) == "50%", "Percent formatting changed");
        Check(formatter.ValueToString(null) == "<null>", "Null formatting changed");
        Check(AM.AMSettings.SimpleSettings.Description() == "<i>Нет описания</i>", "Display markup was lost");
        bool reproduced = false;
        try { new AM.AutoDuel.AutoFriendlyDuelMapComp().UpdatePawnsThatCanDuel(true); }
        catch (AggregateException) { reproduced = true; }
        Check(reproduced, "Threading regression was not reproduced before patching");
        TurboMegaMod.MeleeCompatibility.ApplySnapshotPatch();
        var parallel = new AM.AutoDuel.AutoFriendlyDuelMapComp();
        parallel.UpdatePawnsThatCanDuel(true);
        Check(parallel.Count == 2, "Snapshot did not preserve both pawns in parallel processing");
        var serial = new AM.AutoDuel.AutoFriendlyDuelMapComp();
        serial.UpdatePawnsThatCanDuel(false);
        Check(serial.Count == 2, "Serial processing changed");
        var drawingPawn = new Verse.Pawn();
        var parms = new Verse.PawnDrawParms { flags = 42 };
        AM.Patches.Patch_PawnRenderer_RenderPawnAt.MakeDrawArgs(null, drawingPawn, ref parms);
        Check(parms.pawn == null, "Missing render pawn was not reproduced before patching");
        TurboMegaMod.MeleeCompatibility.ApplyRenderPatch();
        AM.Patches.Patch_PawnRenderer_RenderPawnAt.MakeDrawArgs(null, drawingPawn, ref parms);
        Check(parms.pawn == drawingPawn, "Render pawn identity was not filled");
        Check(parms.flags == 42, "Other render arguments changed");
        Check(SmartSpeed.SmartSpeed_Mod.SettingsCategory() == "Smart Speed — скорость игры", "Smart Speed title not translated");
        Check(Bubbles.Mod.SettingsCategory() == "Речевые пузыри", "Bubbles settings title not translated");
        Check(Bubbles.Configuration.SettingsEditor.DrawSettings() == " (экспериментально)", "Bubbles suffix not translated");
        Check(new Bubbles.Configuration.SettingsEditor.Dialog().Title == "<b>Речевые пузыри</b>", "Dialog title lost its markup");
        Check(UnrelatedMod.Display.BubbleTitle() == "Interaction Bubbles", "Bubbles title patch leaked into unrelated mod");
        Check(CaravanDontRest.AlmostThereMod.SettingsCategory() == "Отдых караванов", "Caravan settings title not translated");
        Check(SnapOut.SOMod.Duration() == "{0}: {1:F1} ч", "Snap Out duration label lost formatting or translation");
        Console.WriteLine("{\"runtimeChecks\":27,\"realHarmony\":true,\"passed\":true}");
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
