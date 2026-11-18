using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// Draw-size multiplier carried by a gene (e.g. TMM_Body_Giant for super mutants).
    public sealed class GeneDrawScaleExtension : DefModExtension
    {
        public float scale = 1f;
    }

    /// Makes pawns with a draw-scale gene render larger: every render node's scale and offset are multiplied,
    /// so the whole pawn grows uniformly. Patches PawnRenderNodeWorker.ScaleFor and all its overrides;
    /// a per-thread depth counter applies the factor once even when an override calls base.
    [StaticConstructorOnStartup]
    public static class GiantDrawScale
    {
        [ThreadStatic] private static int scaleDepth;

        static GiantDrawScale()
        {
            if (!ModsConfig.BiotechActive) return;
            var harmony = new Harmony("turbomegamod.giant.drawscale");
            int patched = 0;
            foreach (Type t in AccessTools.AllTypes().Where(t => typeof(PawnRenderNodeWorker).IsAssignableFrom(t)))
            {
                var s = AccessTools.DeclaredMethod(t, "ScaleFor");
                if (s != null && !s.IsAbstract) { harmony.Patch(s, prefix: new HarmonyMethod(typeof(GiantDrawScale), "ScaleEnter"), postfix: new HarmonyMethod(typeof(GiantDrawScale), "ScaleExit")); patched++; }
            }
            if (patched == 0) Log.Error("[TMM] GiantDrawScale: no PawnRenderNodeWorker.ScaleFor/OffsetFor found; giants render at normal size.");
        }

        public static float FactorFor(Pawn pawn)
        {
            if (pawn == null || pawn.genes == null) return 1f;
            float f = 1f;
            var genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                Gene g = genes[i];
                if (!g.Active) continue;
                var ext = g.def.GetModExtension<GeneDrawScaleExtension>();
                if (ext != null) f *= ext.scale;
            }
            return f;
        }

        public static void ScaleEnter() { scaleDepth++; }
        /// Only the root node is scaled: child transforms compose with their parents, so the whole pawn
        /// (body, head, attachments, apparel) grows exactly once. Offsets follow automatically.
        public static void ScaleExit(PawnRenderNode __0, PawnDrawParms __1, ref Vector3 __result)
        {
            if (scaleDepth == 1 && __0 != null && __0.parent == null) { float f = FactorFor(__1.pawn); if (f != 1f) __result *= f; }
            scaleDepth--;
        }
    }
}
