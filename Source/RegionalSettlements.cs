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
        public bool capital;
        public void ExposeData()
        {
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref capital, "capital");
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
        public static void Generate(PlanetLayer layer, RegionalWorld state)
        {
            if (state.clustered || layer != Find.WorldGrid.Surface) return;
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
            for (int index = 0; index < groups.Count; index++)
            {
                var members = groups[index]; var anchor = anchors[index]; var faction = anchor.Faction;
                var frontier = new Queue<PlanetTile>(); var visited = new HashSet<PlanetTile>();
                var neighbors = new List<PlanetTile>();
                frontier.Enqueue(anchor.Tile); visited.Add(anchor.Tile);
                var available = new Queue<PlanetTile>();
                float radius = Math.Min(55, 9 + (float)Math.Sqrt(members.Count) * 2.5f);
                while (frontier.Count > 0 && visited.Count < SearchLimit)
                {
                    var current = frontier.Dequeue();
                    Find.WorldGrid.GetTileNeighbors(current, neighbors);
                    foreach (var next in neighbors)
                    {
                        if (!visited.Add(next) || Distance(anchor.Tile, next) > radius) continue;
                        frontier.Enqueue(next);
                        float ownDistance = Distance(anchor.Tile, next);
                        // Voronoi regions keep neighboring powers from mixing their new bases.
                        if (anchors.Any(a => a != anchor && Distance(a.Tile, next) + 2 < ownDistance)) continue;
                        available.Enqueue(next);
                    }
                }
                foreach (var settlement in members)
                {
                    var data = state.For(settlement);
                    if (settlement == anchor) { data.capital = true; data.level = 10; continue; }
                    bool moved = false;
                    while (available.Count > 0)
                    {
                        var tile = available.Dequeue(); state.evaluated++;
                        if (!Suitable(tile, faction)) continue;
                        // Keep the same settlement object, name, faction and ID.
                        settlement.Tile = tile;
                        moved = true; state.relocated++; break;
                    }
                    if (!moved) state.retained++; // Finite fallback for small islands/extreme climates.
                }
                state.distanceAfter += MeanDistance(members, anchor);
            }
            if (groups.Count > 0) { state.distanceBefore /= groups.Count; state.distanceAfter /= groups.Count; }
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
        }
    }
}
