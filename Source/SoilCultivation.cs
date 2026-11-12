using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class SoilCultivation
    {
        private static readonly TerrainAffordanceDef GrowingSoil = DefDatabase<TerrainAffordanceDef>.GetNamed("GrowSoil");
        static SoilCultivation()
        {
            new Harmony("turbomegamod.soil.cultivation").Patch(AccessTools.Method(typeof(FertilityGrid), "CalculateFertilityAt"),
                postfix: new HarmonyMethod(typeof(SoilCultivation), "GreenhouseFertility"));
        }
        public static bool IsTillable(TerrainDef terrain)
        {
            return terrain != null && !terrain.layerable && terrain.fertility > 0f && terrain.affordances.Contains(GrowingSoil);
        }
        private static void GreenhouseFertility(IntVec3 __0, Map ___map, ref float __result)
        {
            var zone = ControlledFarming.ZoneAt(___map, __0);
            if (zone == null || !zone.planned || !zone.Enclosed || __result <= 0f) return;
            var edifice = __0.GetEdifice(___map);
            if (edifice != null) return;
            var soil = __0.GetTerrain(___map);
            if (soil.defName != "TMM_TilledSoil" && !IsTillable(soil)) return;
            if (ModsConfig.BiotechActive && ___map.pollutionGrid.IsPolluted(__0)) return;
            __result = Mathf.Max(__result, soil.defName == "TMM_TilledSoil" ? 2.22f : 2f);
        }
    }
    public sealed class PlaceWorker_TillableSoil : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            return SoilCultivation.IsTillable(loc.GetTerrain(map)) ? (AcceptanceReport)true : (AcceptanceReport)"TMM.Farming.TillSoilOnly".Translate().RawText;
        }
    }
}
