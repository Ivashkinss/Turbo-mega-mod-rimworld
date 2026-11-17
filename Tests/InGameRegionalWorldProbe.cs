using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using Verse;

[StaticConstructorOnStartup]
public static class RegionalWorldLaunch
{
    static RegionalWorldLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-regional-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-RegionalWorld"));
        if (Environment.GetCommandLineArgs().Contains("-tmm-regional-legacy"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-ClinicAdmission"));
    }
}

public sealed class RegionalWorldProbe : MapComponent
{
    private bool done, saved;
    private string digest;
    private Settlement lowSettlement, highSettlement;
    private int lowBuildings, highBuildings, lowBeds, highBeds, lowPawns, highPawns;
    private int checks;
    public RegionalWorldProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        Scribe_Values.Look(ref saved, "regionalProbeSaved");
        Scribe_Values.Look(ref digest, "regionalProbeDigest");
        Scribe_References.Look(ref lowSettlement, "regionalProbeLow");
        Scribe_References.Look(ref highSettlement, "regionalProbeHigh");
        Scribe_Values.Look(ref lowBuildings, "regionalProbeLowBuildings");
        Scribe_Values.Look(ref highBuildings, "regionalProbeHighBuildings");
        Scribe_Values.Look(ref lowBeds, "regionalProbeLowBeds");
        Scribe_Values.Look(ref highBeds, "regionalProbeHighBeds");
        Scribe_Values.Look(ref lowPawns, "regionalProbeLowPawns");
        Scribe_Values.Look(ref highPawns, "regionalProbeHighPawns");
    }
    private void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    public static string Fingerprint()
    {
        var state = Find.World.GetComponent<RegionalWorld>();
        var values = Find.WorldObjects.Settlements.Where(s => s.Faction != null && !s.Faction.IsPlayer).OrderBy(s => s.ID)
            .Select(s => s.ID + ":" + s.Faction.loadID + ":" + s.Tile + ":" + state.For(s).level + ":" + state.For(s).capital);
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("|", values))));
    }
    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        bool reload = args.Contains("-tmm-regional-reload");
        bool legacy = args.Contains("-tmm-regional-legacy");
        if (done || Find.CurrentMap != map || (!reload && !legacy && !args.Contains("-tmm-regional-probe"))) return;
        done = true;
        if (legacy) { Legacy(); return; }
        string error = null;
        var state = Find.World.GetComponent<RegionalWorld>();
        try
        {
            Check(state != null, "Missing RegionalWorld component");
            Check(state.clustered, "Fresh world-generation step did not run");
            var settlements = Find.WorldObjects.Settlements.Where(s => s.Faction != null && !s.Faction.IsPlayer).ToList();
            Check(settlements.Count >= 10, "Test world has too few settlements");
            Check(state.relocated > 0, "No actual settlement object relocated");
            Check(state.distanceAfter < state.distanceBefore, "Clustering did not reduce mean distance");
            Check(state.evaluated <= RegionalPlacement.SearchLimit * settlements.Select(s => s.Faction).Distinct().Count(), "Unbounded search");
            Check(settlements.Select(s => s.Tile).Distinct().Count() == settlements.Count, "Duplicate occupied tiles");
            Check(settlements.All(s => state.For(s) != null), "Development records do not cover generated settlements");
            Check(state.settlements.All(s => s.level >= 1 && s.level <= 10), "Tier outside1..10");
            Check(state.settlements.Where(s => s.capital).All(s => s.level == 10), "Capital tier wrong");
            Check(state.settlements.Where(s => s.capital).GroupBy(s => s.settlement.Faction).All(g => g.Count() == 1), "Capital duplication");
            Check(settlements.All(s => Find.WorldGrid[s.Tile].PrimaryBiome.canBuildBase && Find.WorldGrid[s.Tile].hilliness != Hilliness.Impassable), "Invalid terrain");
            string before = Fingerprint();
            new WorldGenStep_RegionalFactions().GenerateFromScribe("probe", Find.WorldGrid.Surface);
            new WorldGenStep_RegionalFactions().GenerateWithoutWorldData("probe", Find.WorldGrid.Surface);
            Check(before == Fingerprint(), "Loading/missing-data path relocated settlements");
            RegionalPlacement.Generate(Find.WorldGrid.Surface, state);
            Check(before == Fingerprint(), "Repeated generation moved/leveled twice");
            var sample = settlements[0];
            var existing = state.For(sample);
            state.settlements.Remove(existing);
            Rand.PushState(192617);
            int expected = Rand.Int;
            Rand.PopState();
            Rand.PushState(192617);
            var temporary = state.For(sample);
            int observed = Rand.Int;
            Rand.PopState();
            state.settlements.Remove(temporary); state.settlements.Add(existing);
            Check(observed == expected, "Metadata initialization altered random stream");
            Check(sample.GetInspectString().Contains("10") || sample.GetInspectString().Contains(state.For(sample).level.ToString()), "Tier inspection absent");
            if (reload)
            {
                Check(saved && !string.IsNullOrEmpty(digest), "No real saved probe state");
                Check(before == digest, "Actual save/load changed world positions/tiers/identities");
                Check(lowSettlement != null && highSettlement != null && lowSettlement.HasMap && highSettlement.HasMap, "Real visited maps lost");
                Check(lowSettlement.Map.GetComponent<SettlementMapDevelopment>().generatedLevel == 1 &&
                    highSettlement.Map.GetComponent<SettlementMapDevelopment>().generatedLevel == 10, "Saved map tiers lost");
                Check(lowSettlement.Map.GetComponent<SettlementMapDevelopment>().initialBuildings == lowBuildings &&
                    highSettlement.Map.GetComponent<SettlementMapDevelopment>().initialBuildings == highBuildings, "Saved generation records lost");
                var sameMap = GetOrGenerateMapUtility.GetOrGenerateMap(highSettlement.Tile, new IntVec3(150, 1, 150), null);
                Check(sameMap == highSettlement.Map, "Visit regenerated existing map");
            }
            else
            {
                var group = settlements.Where(s => !s.HasMap && s.Faction.def.humanlikeFaction && s.Faction.def.techLevel >= TechLevel.Industrial)
                    .GroupBy(s => s.Faction).First(g => g.Count() >= 2).Take(2).ToArray();
                lowSettlement = group[0]; highSettlement = group[1];
                state.For(lowSettlement).level = 1; state.For(lowSettlement).capital = false;
                state.For(highSettlement).level = 10;
                var lowMap = GetOrGenerateMapUtility.GetOrGenerateMap(lowSettlement.Tile, new IntVec3(150, 1, 150), null);
                var highMap = GetOrGenerateMapUtility.GetOrGenerateMap(highSettlement.Tile, new IntVec3(150, 1, 150), null);
                var low = lowMap.GetComponent<SettlementMapDevelopment>(); var high = highMap.GetComponent<SettlementMapDevelopment>();
                Check(low.generatedLevel == 1 && high.generatedLevel == 10, "Native settlement generator ignored tiers");
                Check(high.footprint.Area > low.footprint.Area, "Higher tier did not enlarge actual settlement footprint");
                lowBuildings = low.initialBuildings; highBuildings = high.initialBuildings;
                lowBeds = low.initialBeds; highBeds = high.initialBeds; lowPawns = low.initialPawns; highPawns = high.initialPawns;
                Check(highBuildings > lowBuildings && lowBuildings > 0, "Higher tier did not add actual buildings: " + lowBuildings + "/" + highBuildings);
                Check(highBeds > lowBeds && lowBeds > 0, "Higher tier did not add beds: " + lowBeds + "/" + highBeds);
                Check(highPawns > lowPawns && lowPawns > 0, "Higher tier did not strengthen actual garrison: " + lowPawns + "/" + highPawns);
                digest = Fingerprint(); saved = true;
                GameDataSaveLoader.SaveGame("TMM-RegionalWorld");
                Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-RegionalWorld.rws")), "No actual save file");
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        string json = "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks +
            ",\"relocated\":" + (state == null ? 0 : state.relocated) + ",\"retained\":" + (state == null ? 0 : state.retained) +
            ",\"distanceBefore\":" + (state == null ? 0 : state.distanceBefore).ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"distanceAfter\":" + (state == null ? 0 : state.distanceAfter).ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"lowBuildings\":" + lowBuildings + ",\"highBuildings\":" + highBuildings + ",\"lowBeds\":" + lowBeds + ",\"highBeds\":" + highBeds +
            ",\"lowPawns\":" + lowPawns + ",\"highPawns\":" + highPawns +
            ",\"errors\":" + (error == null ? "[]" : "[" + Q(error) + "]") + "}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, reload ? "RegionalWorldReloadProbe.json" : "RegionalWorldProbe.json"), json);
        if (!reload) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "RegionalWorldUiProbe.json"), "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
    }
    private void Legacy()
    {
        string error = null;
        try
        {
            var state = Find.World.GetComponent<RegionalWorld>();
            Check(state != null && !state.clustered && state.relocated == 0, "Old world was re-clustered");
            var expected = File.ReadAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "LegacySettlementPositions.txt"));
            var settlements = Find.WorldObjects.Settlements;
            Check(expected.Length == settlements.Count, "Legacy settlement count changed");
            foreach (var row in expected)
            {
                var parts = row.Split('|');
                int id = int.Parse(parts[0]);
                var s = settlements.SingleOrDefault(x => x.ID == id);
                if (s == null || s.Tile.ToString() != parts[1]) throw new Exception("Old settlement moved or lost: " + row);
            }
            Check(true, "Old positions verified");
            var tiles = string.Join("|", settlements.Select(s => s.ID + ":" + s.Tile));
            foreach (var s in settlements) state.For(s);
            Check(tiles == string.Join("|", settlements.Select(s => s.ID + ":" + s.Tile)), "Metadata access moved old bases");
            Check(state.settlements.All(s => !s.capital), "Old bases silently promoted to capitals");
            Check(state.settlements.All(s => s.level >= 1 && s.level <= 9), "Old metadata tier invalid");
        }
        catch (Exception ex) { error = ex.ToString(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "RegionalWorldLegacyProbe.json"),
            "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":" + (error == null ? "[]" : "[" + Q(error) + "]") + "}");
    }
    private static string Q(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
}
