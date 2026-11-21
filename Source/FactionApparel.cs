using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    // =========================================================================
    // DEF MOD EXTENSIONS
    // =========================================================================

    /// <summary>
    /// Controls race restrictions (TMM_Angel, TMM_SuperMutant), wing friendliness,
    /// and supernatural damage reduction on faction apparel ThingDefs.
    /// </summary>
    public sealed class FactionApparelExtension : DefModExtension
    {
        public string requiredXenotype;
        public bool wingFriendly;
        public float supernaturalDamageReduction;
        public bool demonAnomalyOnlyReduction;
    }

    /// <summary>
    /// Controls story-flag unlocking on quest-gated faction apparel RecipeDefs.
    /// Story quests set either requiredStoryFlag ("unlock.<item_id>=true") or any alias in altStoryFlags.
    /// </summary>
    public sealed class FactionApparelRecipeExtension : DefModExtension
    {
        public string requiredStoryFlag;
        public List<string> altStoryFlags = new List<string>();
        public bool hideWhenAnomalyActive;
    }

    // =========================================================================
    // LIVING BIOFERRITE CARAPACE SELF-REPAIR COMP
    // =========================================================================

    public sealed class CompProperties_LivingCarapaceRepair : CompProperties
    {
        public int repairIntervalTicks = 600;
        public int hitPointsPerRepair = 1;

        public CompProperties_LivingCarapaceRepair()
        {
            compClass = typeof(CompLivingCarapaceRepair);
        }
    }

    public sealed class CompLivingCarapaceRepair : ThingComp
    {
        private int tickCounter;

        public CompProperties_LivingCarapaceRepair Props
        {
            get { return (CompProperties_LivingCarapaceRepair)props; }
        }

        public override void CompTick()
        {
            base.CompTick();
            StepRepair(1);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            StepRepair(250);
        }

        public bool StepRepair(int ticks)
        {
            if (parent == null || parent.Destroyed || !parent.def.useHitPoints) return false;
            if (parent.HitPoints >= parent.MaxHitPoints)
            {
                tickCounter = 0;
                return false;
            }

            tickCounter += ticks;
            int interval = Mathf.Max(60, Props.repairIntervalTicks);
            if (tickCounter < interval) return false;

            tickCounter -= interval;
            parent.HitPoints = Mathf.Min(parent.MaxHitPoints, parent.HitPoints + Mathf.Max(1, Props.hitPointsPerRepair));
            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }

    // =========================================================================
    // RACE RESTRICTION & STORY FLAG UTILITIES
    // =========================================================================

    public static class FactionApparelUtil
    {
        private static bool s_storyChecked;
        private static Type s_storyType;
        private static PropertyInfo s_storyInstanceProp;
        private static MethodInfo s_storyGetFlagMethod;
        private static FieldInfo s_storyFlagsField;

        public static bool CanPawnWearApparel(Pawn pawn, ThingDef apparelDef, out string failReason)
        {
            failReason = null;
            if (pawn == null || apparelDef == null) return true;

            FactionApparelExtension ext = apparelDef.GetModExtension<FactionApparelExtension>();
            if (ext == null || ext.requiredXenotype.NullOrEmpty()) return true;

            if (MatchesRequiredRace(pawn, ext.requiredXenotype)) return true;

            if (ext.requiredXenotype == "TMM_Angel")
            {
                failReason = "Only Angels can wear winged celestial apparel.";
            }
            else if (ext.requiredXenotype == "TMM_SuperMutant")
            {
                failReason = "Only Super Mutants fit this oversized armor.";
            }
            else
            {
                failReason = "Incompatible race/xenotype (" + ext.requiredXenotype + ").";
            }
            return false;
        }

        public static bool MatchesRequiredRace(Pawn pawn, string requiredXenotype)
        {
            if (pawn == null || requiredXenotype.NullOrEmpty()) return true;

            // Check Biotech xenotype first
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == requiredXenotype)
                {
                    return true;
                }

                // Gene fallback for hybrid/custom pawns carrying core signature genes
                if (requiredXenotype == "TMM_Angel")
                {
                    GeneDef g = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_AngelCelestialGrace");
                    if (g != null && pawn.genes.HasActiveGene(g)) return true;
                }
                else if (requiredXenotype == "TMM_SuperMutant")
                {
                    GeneDef g = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_MutantFevMusculature");
                    if (g != null && pawn.genes.HasActiveGene(g)) return true;
                }
            }

            // BodyType fallback for Super Mutants (body TMM_SuperMutant)
            if (requiredXenotype == "TMM_SuperMutant" && pawn.story != null && pawn.story.bodyType != null)
            {
                if (pawn.story.bodyType.defName == "TMM_SuperMutant") return true;
            }

            return false;
        }

        public static bool IsQuestRecipeUnlocked(FactionApparelRecipeExtension ext)
        {
            if (ext == null) return true;
            if (ext.hideWhenAnomalyActive && ModsConfig.AnomalyActive) return false;

            if (!ext.requiredStoryFlag.NullOrEmpty() && IsStoryFlagTrue(ext.requiredStoryFlag))
            {
                return true;
            }

            if (ext.altStoryFlags != null)
            {
                for (int i = 0; i < ext.altStoryFlags.Count; i++)
                {
                    if (!ext.altStoryFlags[i].NullOrEmpty() && IsStoryFlagTrue(ext.altStoryFlags[i]))
                    {
                        return true;
                    }
                }
            }

            return ext.requiredStoryFlag.NullOrEmpty() && (ext.altStoryFlags == null || ext.altStoryFlags.Count == 0);
        }

        public static bool IsStoryFlagTrue(string flagName)
        {
            if (flagName.NullOrEmpty()) return false;

            if (!s_storyChecked)
            {
                s_storyChecked = true;
                s_storyType = AccessTools.TypeByName("TurboMegaMod.SparkStory");
                if (s_storyType != null)
                {
                    s_storyInstanceProp = AccessTools.Property(s_storyType, "Instance");
                    s_storyGetFlagMethod = AccessTools.Method(s_storyType, "GetFlag", new Type[] { typeof(string) }); // CHECK API
                    s_storyFlagsField = AccessTools.Field(s_storyType, "flags"); // CHECK API
                }
            }

            if (s_storyType == null || s_storyInstanceProp == null) return false;

            try
            {
                object inst = s_storyInstanceProp.GetValue(null, null);
                if (inst == null) return false;

                if (s_storyGetFlagMethod != null)
                {
                    object val = s_storyGetFlagMethod.Invoke(inst, new object[] { flagName });
                    string str = val != null ? val.ToString() : null;
                    return string.Equals(str, "true", StringComparison.OrdinalIgnoreCase) || str == "1";
                }

                if (s_storyFlagsField != null)
                {
                    IDictionary dict = s_storyFlagsField.GetValue(inst) as IDictionary;
                    if (dict != null && dict.Contains(flagName))
                    {
                        object val = dict[flagName];
                        string str = val != null ? val.ToString() : null;
                        return string.Equals(str, "true", StringComparison.OrdinalIgnoreCase) || str == "1";
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public static bool IsSupernaturalSource(DamageInfo dinfo, bool demonAndAnomalyOnly)
        {
            if (dinfo.Def != null)
            {
                string dn = dinfo.Def.defName;
                if (!demonAndAnomalyOnly &&
                    (dn == "TMM_HolyWater" || dn == "TMM_RockSalt" || dn == "TMM_Enochian" || dn == "TMM_BoraxBurn"))
                {
                    return true;
                }
            }

            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null) return false;

            if (ModsConfig.AnomalyActive && attacker.RaceProps != null && attacker.RaceProps.IsAnomalyEntity) // CHECK API
            {
                return true;
            }

            if (ModsConfig.BiotechActive && attacker.genes != null && attacker.genes.Xenotype != null)
            {
                string x = attacker.genes.Xenotype.defName;
                if (x == "TMM_Demon") return true;
                if (!demonAndAnomalyOnly && (x == "TMM_Angel" || x == "TMM_Leviathan" || x == "TMM_Ghoul"))
                {
                    return true;
                }
            }

            if (attacker.Faction != null && attacker.Faction.def != null)
            {
                string fd = attacker.Faction.def.defName;
                if (fd == "TMM_HellFaction") return true;
                if (!demonAndAnomalyOnly && (fd == "TMM_HeavenFaction" || fd == "TMM_MonsterClansFaction"))
                {
                    return true;
                }
            }

            return false;
        }
    }

    // =========================================================================
    // HARMONY PATCHES (EXPLICIT REGISTRATION)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class FactionApparelHarmony
    {
        static FactionApparelHarmony()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.factionapparel");

            // 1. ApparelUtility.HasPartsToWear: blocks NPC generation & AI outfits for wrong race
            MethodInfo hasParts = AccessTools.Method(typeof(ApparelUtility), "HasPartsToWear", new Type[] { typeof(Pawn), typeof(ThingDef) }); // CHECK API
            if (hasParts != null)
            {
                harmony.Patch(hasParts, postfix: new HarmonyMethod(typeof(FactionApparelHarmony), "Postfix_HasPartsToWear"));
            }

            // 2. EquipmentUtility.CanEquip: blocks right-click "Wear" UI with clear reason
            MethodInfo canEquip = AccessTools.Method(typeof(EquipmentUtility), "CanEquip",
                new Type[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }); // CHECK API
            if (canEquip != null)
            {
                harmony.Patch(canEquip, postfix: new HarmonyMethod(typeof(FactionApparelHarmony), "Postfix_CanEquip"));
            }

            // 3. Pawn_ApparelTracker.Wear: hard runtime guard so direct Wear() calls also enforce race
            MethodInfo wear = AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear",
                new Type[] { typeof(Apparel), typeof(bool), typeof(bool) }); // CHECK API
            if (wear != null)
            {
                harmony.Patch(wear, prefix: new HarmonyMethod(typeof(FactionApparelHarmony), "Prefix_ApparelTracker_Wear"));
            }

            // 4. RecipeDef.AvailableNow: enforces quest unlock.<id> story flags on faction apparel recipes
            PropertyInfo availProp = AccessTools.Property(typeof(RecipeDef), "AvailableNow"); // CHECK API
            if (availProp != null && availProp.GetGetMethod() != null)
            {
                harmony.Patch(availProp.GetGetMethod(), postfix: new HarmonyMethod(typeof(FactionApparelHarmony), "Postfix_RecipeDef_AvailableNow"));
            }

            // 5. Thing.TakeDamage: supernatural damage reduction for Angel Vestment (-35%) & London Trenchcoat (-25%)
            MethodInfo takeDamage = AccessTools.Method(typeof(Thing), "TakeDamage", new Type[] { typeof(DamageInfo) });
            if (takeDamage != null)
            {
                harmony.Patch(takeDamage, prefix: new HarmonyMethod(typeof(FactionApparelHarmony), "Prefix_Thing_TakeDamage"));
            }
        }

        public static void Postfix_HasPartsToWear(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result || p == null || apparel == null) return;
            string reason;
            if (!FactionApparelUtil.CanPawnWearApparel(p, apparel, out reason))
            {
                __result = false;
            }
        }

        public static void Postfix_CanEquip(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (!__result || thing == null || pawn == null) return;
            if (!(thing is Apparel)) return;
            string reason;
            if (!FactionApparelUtil.CanPawnWearApparel(pawn, thing.def, out reason))
            {
                cantReason = reason;
                __result = false;
            }
        }

        public static bool Prefix_ApparelTracker_Wear(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (__instance == null || __instance.pawn == null || newApparel == null) return true;
            string reason;
            if (!FactionApparelUtil.CanPawnWearApparel(__instance.pawn, newApparel.def, out reason))
            {
                return false;
            }
            return true;
        }

        public static void Postfix_RecipeDef_AvailableNow(RecipeDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            FactionApparelRecipeExtension ext = __instance.GetModExtension<FactionApparelRecipeExtension>();
            if (ext == null) return;

            if (!FactionApparelUtil.IsQuestRecipeUnlocked(ext))
            {
                __result = false;
            }
        }

        public static void Prefix_Thing_TakeDamage(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn victim = __instance as Pawn;
            if (victim == null || victim.Dead || victim.apparel == null) return;

            float bestReduction = 0f;
            List<Apparel> worn = victim.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                Apparel ap = worn[i];
                if (ap == null || ap.def == null) continue;
                FactionApparelExtension ext = ap.def.GetModExtension<FactionApparelExtension>();
                if (ext == null || ext.supernaturalDamageReduction <= 0f) continue;

                if (FactionApparelUtil.IsSupernaturalSource(dinfo, ext.demonAnomalyOnlyReduction))
                {
                    if (ext.supernaturalDamageReduction > bestReduction)
                    {
                        bestReduction = ext.supernaturalDamageReduction;
                    }
                }
            }

            if (bestReduction > 0f)
            {
                float before = dinfo.Amount;
                dinfo.SetAmount(dinfo.Amount * Mathf.Clamp01(1f - bestReduction));
                if (TestMode.Enabled) Log.Message("[TMM diag] supernatural reduction " + bestReduction + ": " + before + " -> " + dinfo.Amount + " on " + victim.LabelShort);
            }
        }
    }
}
