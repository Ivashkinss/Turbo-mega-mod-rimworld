using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{

    /// <summary>
    /// Gene for robotic chassis (P.A.M., Eden) that cannot wear human apparel.
    /// Strips any generated apparel on creation and blocks wearing/equipping apparel via Harmony patches.
    /// </summary>
    public sealed class Gene_NoApparel : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            StripAllApparel();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                StripAllApparel();
            }
        }

        private void StripAllApparel()
        {
            if (pawn == null || pawn.apparel == null) return;
            List<Apparel> worn = pawn.apparel.WornApparel;
            if (worn == null || worn.Count == 0) return;

            if (pawn.Spawned && pawn.Map != null)
            {
                pawn.apparel.DropAll(pawn.Position, false, true);
            }
            else
            {
                pawn.apparel.DestroyAll(DestroyMode.Vanish);
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class SynthGenePatches
    {
        static SynthGenePatches()
        {
            if (!ModsConfig.BiotechActive) return;

            var harmony = new Harmony("colonyproject.integrated.synth.models");

            var hasParts = AccessTools.Method(typeof(ApparelUtility), "HasPartsToWear", new Type[] { typeof(Pawn), typeof(ThingDef) }); // CHECK API
            if (hasParts != null)
            {
                harmony.Patch(hasParts, null, new HarmonyMethod(typeof(SynthGenePatches), "HasPartsToWear_Postfix"));
            }
            else
            {
                Log.Warning("[TMM] SynthGenePatches: ApparelUtility.HasPartsToWear not found.");
            }

            var canEquip = AccessTools.Method(typeof(EquipmentUtility), "CanEquip",
                new Type[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }); // CHECK API
            if (canEquip != null)
            {
                harmony.Patch(canEquip, null, new HarmonyMethod(typeof(SynthGenePatches), "CanEquip_Postfix"));
            }
            else
            {
                Log.Warning("[TMM] SynthGenePatches: EquipmentUtility.CanEquip not found.");
            }

            var wear = AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear",
                new Type[] { typeof(Apparel), typeof(bool), typeof(bool) }); // CHECK API
            if (wear != null)
            {
                harmony.Patch(wear, new HarmonyMethod(typeof(SynthGenePatches), "ApparelTracker_Wear_Prefix"), null);
            }
        }

        public static bool BlocksApparel(Pawn pawn)
        {
            if (pawn == null || pawn.genes == null) return false;
            List<Gene> genes = pawn.genes.GenesListForReading;
            if (genes == null) return false;
            for (int i = 0; i < genes.Count; i++)
            {
                Gene g = genes[i];
                if (g != null && g.Active && g is Gene_NoApparel) return true;
            }
            return false;
        }

        private static void HasPartsToWear_Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result) return;
            if (BlocksApparel(p)) __result = false;
        }

        private static void CanEquip_Postfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (!__result || thing == null || thing.def == null || !thing.def.IsApparel) return;
            if (BlocksApparel(pawn))
            {
                __result = false;
                cantReason = "TMM.Synths.CannotWearApparel".Translate();
            }
        }

        private static bool ApparelTracker_Wear_Prefix(Pawn_ApparelTracker __instance)
        {
            if (__instance == null || __instance.pawn == null) return true;
            return !BlocksApparel(__instance.pawn);
        }
    }
}
