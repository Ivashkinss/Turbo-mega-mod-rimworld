using System;
using System.Collections;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    internal static class VefPawnCacheLoadCompatibility
    {
        static VefPawnCacheLoadCompatibility()
        {
            var load = AccessTools.DeclaredMethod(typeof(Game), "LoadGame", Type.EmptyTypes);
            if (load == null) return;
            new Harmony("turbomegamod.vef.pawn-cache-load").Patch(load,
                prefix: new HarmonyMethod(typeof(VefPawnCacheLoadCompatibility), "ClearPreviousGamePawnCache"));
        }

        private static void ClearPreviousGamePawnCache()
        {
            Type generic = AccessTools.TypeByName("VEF.AestheticScaling.DictCache`2");
            Type data = AccessTools.TypeByName("VEF.AestheticScaling.CachedPawnData");
            if (generic == null || data == null) return;

            // VEF's caches are process-wide and Pawn equality matches Def and ID across loads.
            // Invalidate them before the incoming game's pawns can look up data from the old objects.
            Type pawnCache = generic.MakeGenericType(typeof(Pawn), data);
            var primary = AccessTools.Property(pawnCache, "Cache");
            var fallback = AccessTools.Field(pawnCache, "JunkCache");
            IDictionary cache = primary == null ? null : primary.GetValue(null, null) as IDictionary;
            IDictionary junk = fallback == null ? null : fallback.GetValue(null) as IDictionary;
            int cleared = 0;
            if (cache != null && !cache.IsReadOnly)
            {
                cleared += cache.Count;
                cache.Clear();
            }
            if (junk != null && !junk.IsReadOnly)
            {
                cleared += junk.Count;
                junk.Clear();
            }

            if (cleared > 0) Log.Message("[TMM] Cleared " + cleared + " VEF pawn cache entries before loading a game.");
        }
    }
}
