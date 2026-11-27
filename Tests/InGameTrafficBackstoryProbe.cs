using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;

public sealed class TrafficBackstoryProbe : MapComponent
{
    private readonly List<string> errors = new List<string>();
    private List<KeyValuePair<Faction, PawnKindDef>> fixtures;
    private MethodInfo make;
    private int index;
    private int count;
    private bool complete;

    public TrafficBackstoryProbe(Map map) : base(map) { }

    public override void MapComponentUpdate()
    {
        if (complete || !map.IsPlayerHome || !Environment.GetCommandLineArgs().Contains("-tmm-traffic-backstory-probe")) return;
        if (fixtures == null)
        {
            make = AccessTools.Method(typeof(WorldTraffic), "MakePawn");
            fixtures = BuildCases();
            count = Math.Max(300, fixtures.Count * 3);
            if (fixtures.Count == 0 || make == null) { errors.Add("No native traffic generation cases"); Finish(); return; }
            Application.logMessageReceived += Observe;
        }
        try
        {
            for (int i = 0; i < 4 && index < count; i++, index++)
            {
                var pair = fixtures[index % fixtures.Count];
                Pawn pawn = null;
                Rand.PushState(17201 + index);
                try
                {
                    pawn = (Pawn)make.Invoke(null, new object[] { pair.Value, pair.Key });
                    if (pawn == null || pawn.Dead || !pawn.DevelopmentalStage.Adult())
                        errors.Add("Unexpected pawn for " + pair.Key.def.defName + " / " + pair.Value.defName);
                }
                finally
                {
                    Rand.PopState();
                    if (pawn != null && !pawn.Destroyed && !pawn.Spawned) pawn.Destroy(DestroyMode.Vanish);
                }
            }
            if (index == count) Finish();
        }
        catch (Exception exception)
        {
            errors.Add(exception.GetType().Name + " " + exception.Message);
            Finish();
        }
    }

    private static List<KeyValuePair<Faction, PawnKindDef>> BuildCases()
    {
        var results = new List<KeyValuePair<Faction, PawnKindDef>>();
        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading.Where(WorldTraffic.Eligible))
        {
            var kinds = new HashSet<PawnKindDef>();
            foreach (PawnGroupMaker maker in faction.def.pawnGroupMakers.Where(m => m != null && m.commonality > 0))
            {
                IEnumerable<PawnGenOption> options = (maker.options ?? new List<PawnGenOption>())
                    .Concat(maker.traders ?? new List<PawnGenOption>()).Concat(maker.guards ?? new List<PawnGenOption>());
                foreach (PawnGenOption option in options)
                    if (option != null && option.selectionWeight > 0 && option.kind != null &&
                        option.kind.RaceProps.Humanlike && option.kind.RaceProps.ToolUser)
                        kinds.Add(option.kind);
            }
            foreach (PawnKindDef kind in kinds.OrderBy(kind => kind.defName))
                results.Add(new KeyValuePair<Faction, PawnKindDef>(faction, kind));
        }
        return results;
    }

    private void Observe(string message, string stack, LogType type)
    {
        if (message != null && message.StartsWith("No shuffled ", StringComparison.Ordinal))
            errors.Add(message);
    }

    private void Finish()
    {
        Application.logMessageReceived -= Observe;
        complete = true;
        string passed = errors.Count == 0 && index == count ? "true" : "false";
        string output = "{\"passed\":" + passed + ",\"generated\":" + index + ",\"pairs\":" + fixtures.Count +
            ",\"errors\":[" + string.Join(",", errors.Select(message => "\"" + message.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n") + "\"").ToArray()) + "]}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "TrafficBackstoryProbe.json"), output);
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "TrafficBackstoryUiProbe.json"), "{\"passed\":" + passed + "}");
    }
}
