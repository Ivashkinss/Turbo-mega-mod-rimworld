using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    public sealed class SettlementMapDevelopment : MapComponent
    {
        public int generatedLevel;
        public CellRect footprint;
        public float defenseBudget;
        public int initialBuildings, initialBeds, initialPawns;
        public SettlementMapDevelopment(Map map) : base(map) { }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref generatedLevel, "TMM_generatedSettlementLevel");
            Scribe_Values.Look(ref footprint, "TMM_settlementFootprint");
            Scribe_Values.Look(ref defenseBudget, "TMM_settlementDefenseBudget");
            Scribe_Values.Look(ref initialBuildings, "TMM_initialSettlementBuildings");
            Scribe_Values.Look(ref initialBeds, "TMM_initialSettlementBeds");
            Scribe_Values.Look(ref initialPawns, "TMM_initialSettlementPawns");
        }
        public void Record()
        {
            if (generatedLevel == 0) return;
            initialBuildings = map.listerBuildings.allBuildingsNonColonist.Count();
            initialBeds = map.listerBuildings.allBuildingsNonColonist.OfType<Building_Bed>().Count();
            initialPawns = map.mapPawns.AllPawnsSpawned.Count(p => p.Faction == map.ParentFaction);
        }
    }

    [StaticConstructorOnStartup]
    public static class SettlementMapGeneration
    {
        public static int Size(int level) { return 32 + Math.Max(1, Math.Min(10, level)) * 7; }
        public static float DefensePoints(int level) { return 250 + (float)Math.Pow(Math.Max(1, Math.Min(10, level)), 1.8) * 90; }
        static SettlementMapGeneration()
        {
            var harmony = new Harmony("turbomegamod.settlement.maps");
            harmony.Patch(AccessTools.Method(typeof(SymbolStack), "Push", new[] { typeof(string), typeof(ResolveParams), typeof(string) }),
                prefix: new HarmonyMethod(typeof(SettlementMapGeneration), "BeforePush"));
            harmony.Patch(AccessTools.Method(typeof(GenStep_Settlement), "ScatterAt"), postfix: new HarmonyMethod(typeof(SettlementMapGeneration), "AfterScatter"));
        }
        private static void BeforePush(string __0, ref ResolveParams __1)
        {
            if (__0 != "settlement") return;
            var map = BaseGen.globalSettings.map;
            var settlement = map == null ? null : map.Parent as Settlement;
            if (settlement == null || settlement.Faction == null || settlement.Faction.IsPlayer) return;
            var data = map.GetComponent<SettlementMapDevelopment>();
            if (data.generatedLevel != 0) return;
            var state = Find.World.GetComponent<RegionalWorld>();
            var development = state == null ? null : state.For(settlement);
            if (development == null) return;
            int level = development.level;
            int size = Math.Min(Size(level), Math.Min(map.Size.x, map.Size.z) - 36);
            if (size < 25) return;
            var center = __1.rect.CenterCell;
            var rect = new CellRect(center.x - size / 2, center.z - size / 2, size, size);
            rect = rect.ClipInsideRect(new CellRect(18, 18, map.Size.x - 36, map.Size.z - 36));
            data.generatedLevel = level;
            data.footprint = rect;
            data.defenseBudget = DefensePoints(level);
            __1.rect = rect;
            __1.settlementPawnGroupPoints = data.defenseBudget;
            __1.bedCount = 3 + 3 * level;
            __1.edgeDefenseWidth = level >= 4 ? 3 : 2;
            __1.edgeDefenseGuardsCount = 1 + level / 2;
            if (settlement.Faction.def.techLevel >= TechLevel.Industrial)
            {
                __1.edgeDefenseTurretsCount = Math.Max(0, level - 3);
                __1.edgeDefenseMortarsCount = level >= 7 ? 1 + (level - 7) / 3 : 0;
            }
            else { __1.edgeDefenseTurretsCount = 0; __1.edgeDefenseMortarsCount = 0; }
            BaseGen.globalSettings.minBuildings = Math.Max(BaseGen.globalSettings.minBuildings, 2 + level);
            BaseGen.globalSettings.minBarracks = Math.Max(BaseGen.globalSettings.minBarracks, 1 + level / 3);
            MapGenerator.SetVar("SettlementRect", rect);
        }
        private static void AfterScatter(Map __1)
        {
            if (__1 != null) __1.GetComponent<SettlementMapDevelopment>().Record();
        }
    }
}
