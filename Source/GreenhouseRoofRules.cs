using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Greenhouse roof rules (user report 18.11.2026):
    /// 1. Rooms enclosed by greenhouse glass walls never get the vanilla automatic "build roof" area;
    ///    only the greenhouse itself decides where its glass roof goes.
    /// 2. Whenever the greenhouse marks cells for roofing it also clears "no roof" on them, otherwise
    ///    pawns endlessly build and remove the roof on cells carrying both marks.
    [StaticConstructorOnStartup]
    public static class GreenhouseRoofRules
    {
        static GreenhouseRoofRules()
        {
            var target = AccessTools.Method(typeof(AutoBuildRoofAreaSetter), "TryGenerateAreaNow");
            if (target == null) { Log.Error("[TMM] Greenhouse: AutoBuildRoofAreaSetter.TryGenerateAreaNow not found; glass rooms may get automatic roofs."); return; }
            new Harmony("turbomegamod.greenhouse.roof.rules").Patch(target, prefix: new HarmonyMethod(typeof(GreenhouseRoofRules), "SkipGlassRooms"));
        }

        public static bool IsGlassBuilding(Thing t)
        {
            if (t == null || t.def == null) return false;
            string n = t.def.defName;
            return n == "TMM_GreenhouseGlassWall" || n == "TMM_GreenhouseGlassDoor" || n == "TMM_GreenhouseClimateUnit";
        }

        public static bool EnclosedByGlass(Room room)
        {
            if (room == null || room.Map == null || room.TouchesMapEdge) return false;
            Map map = room.Map;
            return room.BorderCells.Any(c => c.InBounds(map) && IsGlassBuilding(c.GetEdifice(map)));
        }

        public static bool SkipGlassRooms(Room room)
        {
            return !EnclosedByGlass(room);
        }

        public static void MarkRoof(Map map, IntVec3 c)
        {
            map.areaManager.BuildRoof[c] = true;
            map.areaManager.NoRoof[c] = false;
        }
    }
}
