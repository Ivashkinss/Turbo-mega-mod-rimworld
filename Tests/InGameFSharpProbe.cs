using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;
using RimWorld;
using Verse;

// Pure library contracts plus the actual Poet translation/Pawn/Scribe bridges.
public sealed class FSharpProbe : MapComponent
{
    private bool reported;
    public FSharpProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-fsharp-probe")) return;
        var pawn = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Violent)); if (pawn == null) return;
        reported = true; int checks = 0; var errors = new List<string>();
        try
        {
            bool original = ModsConfig.IsActive("latta.fsharp.core"); string id = original ? "latta.fsharp.core" : "colonyproject.integrated";
            var pack = LoadedModManager.RunningModsListForReading.Single(p => p.PackageId == id);
            foreach (string name in new[] { "FSharp.Core", "Poet" })
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == name).ToArray(); Check(assemblies.Length == 1, "Duplicate/missing " + name); checks++;
                string expected = Path.Combine(pack.RootDir, (original ? "Assemblies/" : "Components/FSharpCore/Assemblies/") + name + ".dll");
                Check(string.Equals(Path.GetFullPath(assemblies[0].Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong library origin " + name); checks++;
                Check(assemblies[0].GetTypes().Length > 0, "Library types could not be resolved " + name); checks++;
            }
            Check(typeof(FSharpOption<int>).Assembly.GetName().Version == new Version(4, 7, 0, 0), "Unexpected FSharp version"); checks++;
            Check(FSharpOption<int>.Some(17).Value == 17 && FSharpOption<int>.None == null, "Option bridge failed"); checks++;
            var list = ListModule.OfSeq(new[] { 1, 2, 3 }); Check(list.Length == 3 && list.Head == 1 && list.Tail.Head == 2, "FSharp list bridge failed"); checks++;
            var function = FSharpFunc<int, int>.FromConverter(new Converter<int, int>(n => n + 3)); Check(function.Invoke(4) == 7, "Function bridge failed"); checks++;
            Type translation = AccessTools.TypeByName("Poet.Lyric.Translation");
            Check((string)AccessTools.Method(translation, "translate").Invoke(null, new object[] { "AcceptButton" }) == Translator.Translate("AcceptButton").RawText, "Poet translation bridge failed"); checks++;
            pawn.jobs.StopAll(); pawn.equipment.DestroyAllEquipment(); var gun = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol")); pawn.equipment.AddEquipment(gun);
            var result = (FSharpOption<ThingWithComps>)AccessTools.Method(AccessTools.TypeByName("Poet.Lyric.Pawn"), "getPrimaryEquipment").Invoke(null, new object[] { pawn });
            Check(result != null && result.Value == gun, "Poet pawn equipment bridge failed"); checks++;
            var scribe = AccessTools.Method(AccessTools.TypeByName("Poet.Lyric.Scribe"), "value").MakeGenericMethod(typeof(int));
            string sp = Path.Combine(GenFilePaths.SaveDataFolderPath, "PoetScribe.xml");
            Scribe.saver.InitSaving(sp, "PoetState"); scribe.Invoke(null, new object[] { "testNumber", 37 }); Scribe.saver.FinalizeSaving();
            Scribe.loader.InitLoading(sp); var loaded = (FSharpOption<int>)scribe.Invoke(null, new object[] { "testNumber", 0 }); Scribe.loader.FinalizeLoading();
            Check(loaded != null && loaded.Value == 37, "Poet Scribe bridge failed"); checks++;
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FSharpProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FSharpUiProbe.json"), "{\"passed\":true,\"settingsUiApplicable\":false,\"reason\":\"Pure libraries have no mod settings window\"}");
    }
}
