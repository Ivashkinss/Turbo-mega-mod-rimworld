// Test-only: exercises an actual social-log entry, its bubble and GUI draw path.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

public sealed class BubblesBehaviorProbe : MapComponent
{
    private static PlayLogEntry_Interaction entry;
    private static int drawCalls;
    private Stopwatch watch;
    private bool reported;
    private int firstTick;
    private readonly List<string> errors = new List<string>();

    public BubblesBehaviorProbe(Map map) : base(map) { }

    private static void Drawn(object __instance)
    {
        if (AccessTools.Property(__instance.GetType(), "Entry").GetValue(__instance, null) == entry)
            drawCalls++;
    }

    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-bubbles-probe")) return;
        try
        {
            if (watch == null)
            {
                var pawns = map.mapPawns.FreeColonistsSpawned.Where(p => !p.Dead && !p.Downed).Take(2).ToArray();
                if (pawns.Length < 2) return;
                watch = Stopwatch.StartNew();
                firstTick = Find.TickManager.TicksGame;
                var type = AccessTools.TypeByName("Bubbles.Core.Bubble");
                new Harmony("tmm.diagnostic.bubbles").Patch(AccessTools.Method(type, "Draw"),
                    postfix: new HarmonyMethod(typeof(BubblesBehaviorProbe), "Drawn"));
                CameraJumper.TryJumpAndSelect(pawns[0]);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                entry = new PlayLogEntry_Interaction(InteractionDefOf.Chitchat, pawns[0], pawns[1], new List<RulePackDef>());
                Find.PlayLog.Add(entry);
                var bubbler = AccessTools.TypeByName("Bubbles.Core.Bubbler");
                var cache = (IDictionary)bubbler.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single(f => typeof(IDictionary).IsAssignableFrom(f.FieldType)).GetValue(null);
                bool created = cache.Values.Cast<IEnumerable>().SelectMany(list => list.Cast<object>())
                    .Any(b => AccessTools.Property(type, "Entry").GetValue(b, null) == entry);
                if (!created) errors.Add("Social log entry did not create a bubble");
                var mod = LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "Bubbles.Mod");
                string expectedPackage = ModsConfig.IsActive("jaxe.bubbles") ? "jaxe.bubbles" : "colonyproject.integrated";
                if (mod.Content.PackageId != expectedPackage) errors.Add("Wrong ModContentPack: " + mod.Content.PackageId);
                if (mod.SettingsCategory() != "Речевые пузыри") errors.Add("Settings title was not translated");
                foreach (string texture in new[] { "Icon", "Inner", "Outer" })
                    if (ContentFinder<Texture2D>.Get("Bubbles/" + texture, false) == null) errors.Add("Missing texture: " + texture);
                var dialog = Activator.CreateInstance(AccessTools.TypeByName("Bubbles.Configuration.SettingsEditor+Dialog"), true);
                if ((string)AccessTools.Field(dialog.GetType(), "optionalTitle").GetValue(dialog) != "<b>Речевые пузыри</b>")
                    errors.Add("In-game dialog title did not preserve translated markup");
                Log.Message("[TMM bubbles probe] Social entry injected; bubble created: " + created);
            }
            if (watch.Elapsed.TotalSeconds < 5) return;
            if (drawCalls == 0) errors.Add("The injected bubble was never drawn");
            if (Find.TickManager.TicksGame <= firstTick) errors.Add("Simulation did not advance");
            WriteReport();
        }
        catch (Exception exception)
        {
            errors.Add(exception.ToString());
            WriteReport();
        }
    }

    private void WriteReport()
    {
        reported = true;
        string report = "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"bubbleDrawCalls\":" + drawCalls
            + ",\"simulationTicksAdvanced\":" + (Find.TickManager.TicksGame - firstTick)
            + ",\"errors\":[" + string.Join(",", errors.Select(e => "\"" + e.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\r", "\\r").Replace("\n", "\\n") + "\"")) + "]}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "BubblesProbe.json"), report);
        Log.Message("[TMM bubbles probe] " + report);
    }
}
