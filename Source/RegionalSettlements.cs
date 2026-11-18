using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    public sealed class SettlementDevelopment : IExposable
    {
        public Settlement settlement;
        public int level = 1;
        public bool capital, regionalCenter;
        public void ExposeData()
        {
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref capital, "capital");
            Scribe_Values.Look(ref regionalCenter, "regionalCenter");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) level = Math.Max(1, Math.Min(10, level));
        }
    }

    // No relocation in Tick/FinalizeInit/ExposeData. Old worlds only acquire metadata on access.
    public sealed class RegionalWorld : WorldComponent
    {
        public List<SettlementDevelopment> settlements = new List<SettlementDevelopment>();
        public bool clustered;
        public int relocated, retained, evaluated;
        public float distanceBefore, distanceAfter;
        public RegionalWorld(World world) : base(world) { }
        public SettlementDevelopment For(Settlement settlement)
        {
            if (settlement == null || settlement.Faction == null || settlement.Faction.IsPlayer) return null;
            var entry = settlements.FirstOrDefault(s => s.settlement == settlement);
            if (entry != null) return entry;
            // Stable without changing the world's random stream; old worlds are not promoted to capitals.
            int seed = Gen.HashCombineInt(settlement.ID, settlement.Faction.loadID);
            Rand.PushState(seed);
            int level;
            try { level = Rand.RangeInclusive(1, 9); } finally { Rand.PopState(); }
            entry = new SettlementDevelopment { settlement = settlement, level = level };
            settlements.Add(entry);
            return entry;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
                settlements.RemoveAll(s => s == null || s.settlement == null || !s.settlement.Spawned ||
                    s.settlement.Faction == null || s.settlement.Faction.IsPlayer);
            Scribe_Collections.Look(ref settlements, "TMM_settlementDevelopment", LookMode.Deep);
            Scribe_Values.Look(ref clustered, "TMM_regionalGeneration");
            Scribe_Values.Look(ref relocated, "TMM_regionalRelocated");
            Scribe_Values.Look(ref retained, "TMM_regionalRetained");
            Scribe_Values.Look(ref evaluated, "TMM_regionalEvaluated");
            Scribe_Values.Look(ref distanceBefore, "TMM_regionalDistanceBefore");
            Scribe_Values.Look(ref distanceAfter, "TMM_regionalDistanceAfter");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                settlements = (settlements ?? new List<SettlementDevelopment>())
                    .Where(s => s != null && s.settlement != null && s.settlement.Faction != null && !s.settlement.Faction.IsPlayer)
                    .GroupBy(s => s.settlement).Select(g => g.First()).ToList();
        }
    }

    // Fresh generation only, between Factions (500) and Roads (600).
    // Loading a world calls the explicit no-op methods below, including worlds without our saved data.
    public sealed class WorldGenStep_RegionalFactions : WorldGenStep
    {
        public override int SeedPart { get { return 136493821; } }
        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            if (layer == null || !layer.IsRootSurface) return;
            var state = Find.World.GetComponent<RegionalWorld>();
            if (state == null || state.clustered) return;
            RegionalPlacement.Generate(layer, state);
        }
        public override void GenerateFromScribe(string seed, PlanetLayer layer) { }
        public override void GenerateWithoutWorldData(string seed, PlanetLayer layer) { }
    }

    public static class RegionalPlacement
    {
        public const int SearchLimit = 18000;
        public static bool Suitable(PlanetTile tile, Faction faction)
        {
            if (!tile.Valid || faction == null || !TileFinder.IsValidTileForNewSettlement(tile)) return false;
            var biome = Find.WorldGrid[tile].PrimaryBiome;
            if (biome.settlementSelectionWeight <= 0) return false;
            var curve = faction.def.minSettlementTemperatureChanceCurve;
            return curve == null || curve.Evaluate(GenTemperature.MinTemperatureAtTile(tile)) > 0;
        }
        private static float Distance(PlanetTile a, PlanetTile b)
        {
            return Find.WorldGrid.ApproxDistanceInTiles(a, b);
        }
        private static float MeanDistance(List<Settlement> members, Settlement anchor)
        {
            return members.Count <= 1 ? 0 : members.Where(s => s != anchor).Average(s => Distance(s.Tile, anchor.Tile));
        }
        public static void EnsureMinimumSettlements(PlanetLayer layer)
        {
            if (layer == null || layer != Find.WorldGrid.Surface) return;
            var def = layer.Def.SettlementWorldObjectDef;
            if (def == null) return;
            var factions = Find.FactionManager.AllFactionsListForReading
                .Where(f => f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.def != null && f.def.settlementGenerationWeight > 0 &&
                    (f.def.layerWhitelist.NullOrEmpty() || f.def.layerWhitelist.Contains(layer.Def)))
                .OrderBy(f => f.loadID)
                .ToList();
            foreach (var faction in factions)
            {
                int current = Find.WorldObjects.Settlements.Count(s => s.Faction == faction && s.Tile.Layer == layer);
                int needed = 2 - current;
                for (int i = 0; i < needed; i++)
                {
                    var tile = TileFinder.RandomSettlementTileFor(layer, faction, false, null);
                    if (!tile.Valid) break;
                    var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(def);
                    settlement.SetFaction(faction);
                    settlement.Tile = tile;
                    settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, null);
                    Find.WorldObjects.Add(settlement);
                }
            }
        }
        public static void Generate(PlanetLayer layer, RegionalWorld state)
        {
            if (state.clustered || layer != Find.WorldGrid.Surface) return;
            EnsureMinimumSettlements(layer);
            var groups = Find.WorldObjects.Settlements.Where(s => s.Faction != null && !s.Faction.IsPlayer &&
                !s.HasMap && s.Tile.Layer == layer).GroupBy(s => s.Faction).OrderBy(g => g.Key.loadID)
                .Select(g => g.OrderBy(s => s.ID).ToList()).ToList();
            var anchors = new List<Settlement>();
            // Choose separated anchors from each faction's native valid positions.
            foreach (var members in groups)
            {
                var anchor = anchors.Count == 0 ? members[0] : members.OrderByDescending(s =>
                    anchors.Min(a => Distance(a.Tile, s.Tile))).ThenBy(s => s.ID).First();
                anchors.Add(anchor);
                state.distanceBefore += MeanDistance(members, anchor);
            }
            var rows = new List<Settlement>();
            var rowGroups = new List<int>();
            for (int index = 0; index < groups.Count; index++)
            {
                var anchorData = state.For(anchors[index]);
                anchorData.capital = true; anchorData.level = 10;
                foreach (var settlement in groups[index])
                {
                    state.For(settlement);
                    if (settlement == anchors[index]) continue;
                    rows.Add(settlement); rowGroups.Add(index);
                }
            }
            var sites = rows.Select(s => s.Tile).ToArray();
            var costs = new double[groups.Count, sites.Length];
            state.distanceBefore = 0;
            for (int index = 0; index < groups.Count; index++)
                for (int site = 0; site < sites.Length; site++)
                {
                    state.evaluated++;
                    var terrain = Find.WorldGrid[sites[site]];
                    var curve = anchors[index].Faction.def.minSettlementTemperatureChanceCurve;
                    bool legal = terrain.PrimaryBiome.canBuildBase && terrain.PrimaryBiome.implemented &&
                        terrain.PrimaryBiome.settlementSelectionWeight > 0 && terrain.hilliness != Hilliness.Impassable &&
                        (curve == null || curve.Evaluate(GenTemperature.MinTemperatureAtTile(sites[site])) > 0);
                    costs[index, site] = legal ? Distance(anchors[index].Tile, sites[site]) : WorldRegionAllocation.Forbidden;
                }
            for (int i = 0; i < rows.Count; i++) state.distanceBefore += Distance(anchors[rowGroups[i]].Tile, sites[i]);
            int[] assignment = WorldRegionAllocation.Assign(costs, rowGroups.ToArray());
            // Native positions form a known legal fallback if another world generator imposed
            // conditions that are not represented by our biome/temperature validation.
            if (assignment.Where((site, row) => costs[rowGroups[row], site] >= WorldRegionAllocation.Forbidden).Any())
                for (int i = 0; i < assignment.Length; i++) assignment[i] = i;
            var moving = rows.Where((settlement, row) => assignment[row] != row).ToList();
            foreach (var settlement in moving) Find.WorldObjects.Remove(settlement);
            for (int i = 0; i < rows.Count; i++) rows[i].Tile = sites[assignment[i]];
            foreach (var settlement in moving) Find.WorldObjects.Add(settlement);
            state.relocated = moving.Count;
            state.retained = rows.Count - moving.Count;
            state.distanceAfter = 0;
            for (int i = 0; i < rows.Count; i++) state.distanceAfter += Distance(anchors[rowGroups[i]].Tile, rows[i].Tile);
            if (rows.Count > 0) { state.distanceBefore /= rows.Count; state.distanceAfter /= rows.Count; }
            // Key cities are metadata on existing settlements, never extra world objects.
            for (int index = 0; index < groups.Count; index++)
            {
                var members = groups[index];
                foreach (var settlement in members)
                {
                    var data = state.For(settlement);
                    if (!data.capital) data.level = Math.Min(7, data.level);
                }
                var hubs = new List<Settlement> { anchors[index] };
                int centerCount = members.Count >= 6 ? 2 : members.Count >= 2 ? 1 : 0;
                for (int center = 0; center < centerCount; center++)
                {
                    var selected = members.Where(s => !hubs.Contains(s)).OrderByDescending(s =>
                        hubs.Min(h => Distance(h.Tile, s.Tile))).ThenBy(s => s.ID).First();
                    var data = state.For(selected);
                    data.regionalCenter = true;
                    data.level = 9 - center;
                    hubs.Add(selected);
                }
            }
            state.clustered = true;
        }
    }

    [StaticConstructorOnStartup]
    public static class SettlementDevelopmentInspect
    {
        static SettlementDevelopmentInspect()
        {
            new Harmony("turbomegamod.settlement.development").Patch(
                AccessTools.Method(typeof(Settlement), "GetInspectString"),
                postfix: new HarmonyMethod(typeof(SettlementDevelopmentInspect), "Inspect"));
        }
        private static void Inspect(Settlement __instance, ref string __result)
        {
            var state = Find.World == null ? null : Find.World.GetComponent<RegionalWorld>();
            var entry = state == null ? null : state.For(__instance);
            if (entry == null) return;
            __result += "\n" + "TMM.World.SettlementLevel".Translate(entry.level);
            if (entry.capital) __result += "\n" + "TMM.World.Capital".Translate();
            else if (entry.regionalCenter) __result += "\n" + "TMM.World.RegionalCenter".Translate();
        }
    }
}
