// Exercises the production transpiler with real Harmony and small UI test doubles.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Verse
{
    public class ModContentPack { }
    public class Mod { public Mod(ModContentPack content) { } }
    public static class Log { public static void Message(string message) { } }
    public static class Translator
    {
        public static readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public static string TranslateSimple(string key) { return Values[key]; }
    }
}
namespace AM.AMSettings
{
    public enum CorpseOffsetMode { None, InterpolateToCorrect, KeepOffset }
    public static class SimpleSettings
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string DrawWindow() { return "Loading..."; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string DrawEnum(object value) { return value.ToString(); }
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
namespace UnrelatedMod
{
    public static class Display
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Draw() { return "Loading..."; }
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
        Check(AM.AMSettings.SimpleSettings.DrawEnum(AM.AMSettings.CorpseOffsetMode.None) == "Не смещать", "Enum label not translated");
        Check(AM.AMSettings.SimpleSettings.DrawEnum(AM.AMSettings.CorpseOffsetMode.KeepOffset) == "Сохранять смещение", "Enum value changed");
        Check(AM.AMSettings.SimpleSettings.DrawEnum(25) == "25", "Non-enum ToString changed");
        Console.WriteLine("{\"runtimeChecks\":8,\"realHarmony\":true,\"passed\":true}");
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
