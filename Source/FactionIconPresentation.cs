using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    internal static class FactionIconPresentation
    {
        static FactionIconPresentation()
        {
            var getter = AccessTools.PropertyGetter(typeof(Settlement), "Material");
            if (getter != null)
                new Harmony("colonyproject.integrated.factionicons").Patch(getter,
                    postfix: new HarmonyMethod(typeof(FactionIconPresentation), "AdjustSettlementMaterial"));
        }

        internal static Color ReadableTint(Faction faction)
        {
            Color tint = faction.Color;
            if (faction.def == null || !faction.def.defName.StartsWith("TMM_", StringComparison.Ordinal)) return tint;

            const float minimum = 0.44f;
            float luminance = 0.299f * tint.r + 0.587f * tint.g + 0.114f * tint.b;
            if (luminance < minimum)
                tint = Color.Lerp(tint, Color.white, (minimum - luminance) / (1f - luminance));
            tint.a = 1f;
            return tint;
        }

        private static void AdjustSettlementMaterial(Settlement __instance, ref Material __result)
        {
            Faction faction = __instance.Faction;
            if (__result == null || faction == null || faction.def == null ||
                !faction.def.defName.StartsWith("TMM_", StringComparison.Ordinal)) return;
            Color tint = ReadableTint(faction);
            Texture2D texture = __result.mainTexture as Texture2D;
            if (texture != null && __result.color != tint)
                __result = MaterialPool.MatFrom(texture, __result.shader, tint, __result.renderQueue);
        }
    }
}
