using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    internal static class BoundPawnTextureCompatibility
    {
        private const string TextureDirectory = "AM/BoundPawns/";
        private static readonly Dictionary<BodyTypeDef, BodyTypeDef> ResolvedBodyTypes =
            new Dictionary<BodyTypeDef, BodyTypeDef>();

        static BoundPawnTextureCompatibility()
        {
            var utility = AccessTools.TypeByName("AM.Grappling.GrabUtility");
            if (utility == null) return;

            MethodInfo lookup = AccessTools.Method(utility, "GetBoundPawnTexture",
                new[] { typeof(Pawn), typeof(BodyTypeDef) });
            if (lookup == null || lookup.ReturnType != typeof(Texture2D))
            {
                Log.Warning("[TMM] Could not find the Melee Animation bound pawn texture lookup.");
                return;
            }

            new Harmony("turbomegamod.melee.boundtextures").Patch(lookup,
                prefix: new HarmonyMethod(typeof(BoundPawnTextureCompatibility), "UseAvailableBodyType"));
        }

        private static void UseAvailableBodyType(ref BodyTypeDef __1)
        {
            BodyTypeDef bodyType = __1;
            if (bodyType == null) return;

            BodyTypeDef resolved;
            if (!ResolvedBodyTypes.TryGetValue(bodyType, out resolved))
            {
                BodyTypeDef fallback = FallbackFor(bodyType);
                if (fallback == null) return;
                resolved = ContentFinder<Texture2D>.Get(TextureDirectory + bodyType.defName, false) != null
                    ? bodyType : fallback;
                ResolvedBodyTypes.Add(bodyType, resolved);
            }
            __1 = resolved;
        }

        private static BodyTypeDef FallbackFor(BodyTypeDef bodyType)
        {
            switch (bodyType.defName)
            {
                case "TMM_SuperMutant": return BodyTypeDefOf.Hulk;
                case "TMM_SuperMutantFemale": return BodyTypeDefOf.Female;
                case "TMM_Leviathan": return BodyTypeDefOf.Hulk;
                case "TMM_Ghoul": return BodyTypeDefOf.Thin;
                case "TMM_SynthGen1": return BodyTypeDefOf.Thin;
                case "TMM_NickValentine": return BodyTypeDefOf.Male;
                case "TMM_RobotChassis": return BodyTypeDefOf.Hulk;
                default: return null;
            }
        }
    }
}
