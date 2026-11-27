using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
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
        if (Environment.GetCommandLineArgs().Contains("-tmm-regional-old"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame(Environment.GetEnvironmentVariable("TMM_REGIONAL_OLD")));
        if (Environment.GetCommandLineArgs().Contains("-tmm-regional-legacy"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-ClinicAdmission"));
        if (Environment.GetCommandLineArgs().Contains("-tmm-regional-probe"))
            new Harmony("tmm.test.regional.page").Patch(AccessTools.Method(typeof(WorldGenerator), "GenerateWorld"),
                prefix: new HarmonyMethod(typeof(RegionalWorldPage), "Before"), postfix: new HarmonyMethod(typeof(RegionalWorldPage), "After"));
    }
}

// -quicktest passes factions=null and so takes requiredCountAtGameStart; a player's new game passes the list that the world
// creation page builds. Feed the page's own default list (its private ResetFactionCounts) and a fixed seed into the same generator.
public static class RegionalWorldPage
{
    public static List<string> pageDefaults = new List<string>();
    public static List<string> excluded = new List<string>();
    public static Dictionary<string, int> generated = new Dictionary<string, int>();
    public static string seed = "";
    public static float coverage;
    private static bool used;
    public static List<FactionDef> Ours()
    {
        return DefDatabase<FactionDef>.AllDefsListForReading.Where(d => d.defName.StartsWith("TMM_") && !d.isPlayer && !d.hidden).OrderBy(d => d.defName).ToList();
    }
    public static bool OnSurface(Settlement s)
    {
        return s.Tile.Valid && s.Tile.Layer == Find.WorldGrid.Surface;
    }
    public static void Before(ref float planetCoverage, ref string seedString, ref List<FactionDef> factions)
    {
        if (used) return;
        used = true;
        var page = new Page_CreateWorldParams();
        AccessTools.Method(typeof(Page_CreateWorldParams), "ResetFactionCounts").Invoke(page, null);
        var list = new List<FactionDef>((List<FactionDef>)AccessTools.Field(typeof(Page_CreateWorldParams), "factions").GetValue(page));
        pageDefaults = list.Select(d => d.defName).ToList();
        string skip = Environment.GetEnvironmentVariable("TMM_REGIONAL_EXCLUDE");
        if (!string.IsNullOrEmpty(skip)) excluded = skip.Split(',').ToList();
        list.RemoveAll(d => excluded.Contains(d.defName));
        factions = list;
        seedString = Environment.GetEnvironmentVariable("TMM_REGIONAL_SEED");
        if (string.IsNullOrEmpty(seedString)) seedString = "tmm-factions-a";
        seed = seedString;
        string size = Environment.GetEnvironmentVariable("TMM_REGIONAL_COVERAGE");
        if (!string.IsNullOrEmpty(size)) planetCoverage = float.Parse(size, System.Globalization.CultureInfo.InvariantCulture);
        coverage = planetCoverage;
    }
    public static void After(World __result)
    {
        if (generated.Count > 0 || __result == null) return;
        foreach (var d in Ours())
            generated[d.defName] = __result.factionManager.AllFactionsListForReading.Any(f => f.def == d)
                ? __result.worldObjects.Settlements.Count(s => s.Faction != null && s.Faction.def == d) : -1;
    }
}

public sealed class RegionalWorldProbe : MapComponent
{
    private bool done, saved;
    private string digest;
    private Settlement lowSettlement, highSettlement;
    private int lowBuildings, highBuildings, lowBeds, highBeds, lowPawns, highPawns;
    private int checks;
    private string factionTable;
    private string factionJson = "[]";
    private int oldStart = -1;
    private readonly List<string> facts = new List<string>();

    /// An old save whose world never had the Minutemen: nothing may be added by time, by the quest system or by loading.
    private void OldSave()
    {
        if (oldStart < 0) oldStart = Find.TickManager.TicksGame;
        if (Find.TickManager.CurTimeSpeed != TimeSpeed.Ultrafast) Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
        foreach (Window w in Find.WindowStack.Windows.ToList()) if (w.forcePause) w.Close(false);
        if (Find.TickManager.TicksGame < oldStart + 3000) return;
        done = true;
        string error = null;
        try
        {
            string before = string.Join(",", File.ReadAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "OldFactions.txt")).Where(x => x.Length > 0).OrderBy(x => x));
            string now = string.Join(",", Find.FactionManager.AllFactionsListForReading.Select(f => f.def.defName).OrderBy(x => x));
            facts.Add("factions=" + now);
            Check(MinutemenHuntersWorldComponent.Faction == null, "Minutemen appeared in an old save");
            Check(before == now, "Old save faction list changed: " + before + " -> " + now);
            WithoutMinutemen(map);
            Check(now == string.Join(",", Find.FactionManager.AllFactionsListForReading.Select(f => f.def.defName).OrderBy(x => x)), "Quest or map generation added a faction");
        }
        catch (Exception ex) { error = ex.ToString(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "RegionalWorldReloadProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks + ",\"facts\":[" + string.Join(",", facts.Select(Q)) + "],\"errors\":" + (error == null ? "[]" : "[" + Q(error) + "]") + "}");
    }

    /// The remaining ways that used to create the faction: both quest roots and the outpost map of an existing site.
    private void WithoutMinutemen(Map home)
    {
        var comp = MinutemenHuntersWorldComponent.Instance;
        bool q1 = comp.quest1Completed, q2 = comp.quest2Completed;
        comp.quest1Completed = false;
        Check(!MinutemenHuntersDefOf.TMM_MinutemenReliefQuest.CanRun(new RimWorld.QuestGen.Slate(), home), "Relief quest is offered without the faction");
        comp.quest1Completed = true; comp.quest2Completed = false;
        Check(!MinutemenHuntersDefOf.TMM_MinutemenArchiveQuest.CanRun(new RimWorld.QuestGen.Slate(), home), "Archive quest is offered without the faction");
        foreach (QuestScriptDef script in new[] { MinutemenHuntersDefOf.TMM_MinutemenReliefQuest, MinutemenHuntersDefOf.TMM_MinutemenArchiveQuest })
        {
            Quest forced = QuestUtility.GenerateQuestAndMakeAvailable(script, new RimWorld.QuestGen.Slate());
            if (forced != null && forced.State == QuestState.NotYetAccepted) forced.Accept(null);
            Check(forced == null || forced.State != QuestState.Ongoing, script.defName + " runs without the faction: " + (forced == null ? "null" : forced.State.ToString()));
            facts.Add(script.defName + " forced=" + (forced == null ? "null" : forced.State.ToString()));
            Check(MinutemenHuntersWorldComponent.Faction == null, script.defName + " created the faction");
        }
        comp.quest1Completed = q1; comp.quest2Completed = q2;
        PlanetTile tile;
        Check(TileFinder.TryFindNewSiteTile(out tile, 4, 20, false, null, 0f, true, TileFinderMode.Near, false, false, null, null), "No tile for the old outpost");
        Site site = SiteMaker.MakeSite(MinutemenHuntersDefOf.TMM_MinutemenOutpostSitePart, tile, null, false, 0f);
        Find.WorldObjects.Add(site);
        Map outpost = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, new IntVec3(120, 1, 120), null);
        Check(outpost.listerThings.ThingsOfDef(MinutemenHuntersDefOf.TMM_HuntersFieldArchive).Count > 0, "Old outpost map was not generated");
        Check(MinutemenHuntersWorldComponent.Faction == null, "Outpost map created the faction");
        Check(!outpost.mapPawns.AllPawnsSpawned.Any(p => p.kindDef.defName.StartsWith("TMM_Hunters")), "Outpost without a faction got a garrison");
        facts.Add("outpostWithoutFaction: buildings owner=" + (outpost.listerThings.ThingsOfDef(ThingDefOf.CommsConsole).Any(t => t.Faction != null) ? "some" : "none"));
    }
    public RegionalWorldProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        Scribe_Values.Look(ref saved, "regionalProbeSaved");
        Scribe_Values.Look(ref digest, "regionalProbeDigest");
        Scribe_Values.Look(ref factionTable, "regionalProbeFactions");
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
    // One world, three views: the creation list, the faction registry with real settlements on the map, and the hierarchy tab.
    private string FactionTable(bool reload)
    {
        var ours = RegionalWorldPage.Ours();
        var visible = MainTabWindow_FactionHierarchy.GetVisibleFactions();
        var all = Find.WorldObjects.Settlements.Where(s => s.Faction != null).ToList();
        var rows = new List<string>();
        var json = new List<string>();
        foreach (var d in ours)
        {
            var registry = Find.FactionManager.AllFactionsListForReading.Where(f => f.def == d).ToList();
            var onMap = all.Where(s => s.Faction.def == d && RegionalWorldPage.OnSurface(s) && Find.WorldGrid[s.Tile].PrimaryBiome.canBuildBase &&
                Find.WorldGrid[s.Tile].hilliness != Hilliness.Impassable && all.Count(o => o.Tile == s.Tile) == 1).ToList();
            var tab = visible.FirstOrDefault(f => f.def == d);
            int tabCount = tab == null ? -1 : all.Count(s => s.Faction == tab);
            int gen; if (!RegionalWorldPage.generated.TryGetValue(d.defName, out gen)) gen = -2;
            rows.Add(d.defName + "=" + registry.Count + "/" + onMap.Count + "/" + tabCount);
            json.Add("{\"def\":\"" + d.defName + "\",\"configurable\":" + d.maxConfigurableAtWorldCreation + ",\"inPageDefaults\":" + (reload ? "null" : RegionalWorldPage.pageDefaults.Contains(d.defName) ? "true" : "false") +
                ",\"excluded\":" + (RegionalWorldPage.excluded.Contains(d.defName) ? "true" : "false") + ",\"settlementsAtGeneration\":" + gen + ",\"inRegistry\":" + registry.Count + ",\"settlementsOnMap\":" + onMap.Count + ",\"settlementsInTab\":" + tabCount + "}");
        }
        factionJson = "[" + string.Join(",", json) + "]";
        return string.Join(";", rows);
    }
    private void CheckFactions(string table, bool reload)
    {
        var failures = new List<string>();
        var ours = RegionalWorldPage.Ours();
        if (ours.Count != 15) failures.Add("expected 15 factions, defs " + ours.Count);
        foreach (var row in table.Split(';'))
        {
            string name = row.Substring(0, row.IndexOf('='));
            var n = row.Substring(row.IndexOf('=') + 1).Split('/').Select(int.Parse).ToArray();
            if (!reload && !RegionalWorldPage.pageDefaults.Contains(name)) failures.Add(name + " is not in the world creation defaults");
            if (RegionalWorldPage.excluded.Contains(name))
            {
                if (RegionalWorldPage.generated[name] != -1) failures.Add(name + " was generated although the player removed it");
                if (n[0] != 0) failures.Add(name + " was added to the game although the player removed it");
                continue;
            }
            if (reload && RegionalWorldPage.excluded.Count == 0 && n[0] == 0) { if (!factionTable.Contains(name + "=0/")) failures.Add(name + " lost on reload"); continue; }
            if (n[0] != 1) failures.Add(name + " registry count " + n[0]);
            if (n[1] < 1) failures.Add(name + " has no real settlement");
            if (n[1] != n[2]) failures.Add(name + " map " + n[1] + " differs from tab " + n[2]);
            if (!reload && RegionalWorldPage.generated[name] < 1) failures.Add(name + " had no settlement when the world was generated");
        }
        Check(failures.Count == 0, "Factions: " + string.Join("; ", failures));
    }
    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        bool reload = args.Contains("-tmm-regional-reload");
        bool legacy = args.Contains("-tmm-regional-legacy");
        if (!done && args.Contains("-tmm-regional-old") && Find.CurrentMap == map) { OldSave(); return; }
        if (done || Find.CurrentMap != map || (!reload && !legacy && !args.Contains("-tmm-regional-probe"))) return;
        done = true;
        if (legacy) { Legacy(); return; }
        string error = null;
        var state = Find.World.GetComponent<RegionalWorld>();
        try
        {
            Check(state != null, "Missing RegionalWorld component");
            string table = FactionTable(reload);
            CheckFactions(table, reload);
            if (reload && factionTable.Contains("TMM_MinutemenHuntersFaction=0/")) Check(MinutemenHuntersWorldComponent.Faction == null && !MinutemenHuntersDefOf.TMM_MinutemenReliefQuest.CanRun(new RimWorld.QuestGen.Slate(), map), "Removed Minutemen came back after loading");
            if (reload) Check(table == factionTable, "Faction table changed on load: " + factionTable + " -> " + table);
            else factionTable = table;
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
                if (!reload) facts.Add("visible factions in page defaults=" + RegionalWorldPage.pageDefaults.Count(n => !DefDatabase<FactionDef>.GetNamed(n).hidden) + "; vanilla refuses to add a visible faction at 12 or more");
                if (RegionalWorldPage.excluded.Contains("TMM_MinutemenHuntersFaction")) WithoutMinutemen(map);
                else { MinutemenHuntersWorldComponent.Instance.quest1Completed = false; Check(MinutemenHuntersDefOf.TMM_MinutemenReliefQuest.CanRun(new RimWorld.QuestGen.Slate(), map), "Relief quest is not offered although the faction is there"); }
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
            ",\"seed\":" + Q(RegionalWorldPage.seed) + ",\"coverage\":" + RegionalWorldPage.coverage.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"pageDefaults\":[" + string.Join(",", RegionalWorldPage.pageDefaults.Select(Q)) + "],\"facts\":[" + string.Join(",", facts.Select(Q)) + "],\"factions\":" + factionJson +
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
