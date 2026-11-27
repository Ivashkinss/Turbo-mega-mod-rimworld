using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

            MethodInfo draw = AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawColonist",
                new[] { typeof(Rect), typeof(Pawn), typeof(Map), typeof(bool), typeof(bool) });
            if (draw != null) harmony.Patch(draw,
                transpiler: new HarmonyMethod(typeof(GiantDrawScale), "FitColonistBarPortrait"));
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

        private static IEnumerable<CodeInstruction> FitColonistBarPortrait(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();
            FieldInfo offset = AccessTools.Field(typeof(ColonistBarColonistDrawer), "PawnTextureCameraOffset");
            if (offset == null) return code;

            for (int i = 0; i + 1 < code.Count; i++)
            {
                if (code[i].opcode != OpCodes.Ldsfld || !Equals(code[i].operand, offset) || code[i + 1].opcode != OpCodes.Ldc_R4)
                    continue;
                int portrait = code.FindIndex(i + 2, instruction =>
                {
                    var method = instruction.operand as MethodInfo;
                    return instruction.opcode == OpCodes.Call && method != null &&
                        method.DeclaringType == typeof(PortraitsCache) && method.Name == "Get";
                });
                if (portrait < 0 || portrait - i > 20) continue;

                // Keep the normal camera for every other portrait and preserve the pawn's world scale.
                code.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_2));
                code.Insert(i + 3, new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(GiantDrawScale), "ColonistBarZoom")));
                return code;
            }

            Log.Warning("[TMM] GiantDrawScale: could not locate the colonist bar portrait camera.");
            return code;
        }

        private static float ColonistBarZoom(float zoom, Pawn pawn)
        {
            return zoom / Mathf.Max(1f, FactorFor(pawn));
        }
    }
}
