using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    // =========================================================================
    // DEF MOD EXTENSIONS
    // =========================================================================

    public sealed class SignatureGearExtension : DefModExtension
    {
        public string requiredXenotype;
        public bool wingFriendly;
        public bool requiresMutantOrPowerArmor;
        public bool slaysRegularDemons;
        public bool slaysRegularAngels;
    }

    public sealed class SignatureGearRecipeExtension : DefModExtension
    {
        public string requiredStoryFlag;
        public List<string> altStoryFlags = new List<string>();
        public bool hideWhenAnomalyActive;
    }

    // =========================================================================
    // DEAN'S SAW-OFF SHOTGUN PROJECTILE (ROCK SALT STUN VS DEMONS / ANOMALIES)
    // =========================================================================

    public sealed class Bullet_DeanRockSalt : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || victim.Dead) return;
            if (SignatureGearUtil.IsDemonOrAnomaly(victim))
            {
                if (victim.stances != null && victim.stances.stunner != null)
                {
                    victim.stances.stunner.StunFor(120, launcher); // CHECK API
                }
            }
        }
    }

    // =========================================================================
    // CORE UTILITIES: RACE CHECKS, HERO REGISTRY CHECK, SET BONUSES, STORY FLAGS
    // =========================================================================

    public static class SignatureGearUtil
    {
        private static bool s_storyChecked;
        private static Type s_storyType;
        private static PropertyInfo s_storyInstanceProp;
        private static MethodInfo s_storyGetFlagMethod;
        private static FieldInfo s_storyFlagsField;

        private static bool s_heroRegChecked;
        private static MethodInfo s_heroGetDefMethod;
        private static FieldInfo s_heroDictField;
        private static FieldInfo s_heroListField;

        // HeroDef -> signature apparel & weapon mapping (ensures GetOrCreate always equips signature gear)
        private static readonly Dictionary<string, string> HeroSignatureApparel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TMM_Hero_dean_winchester", "TMM_Sig_dean_leather_jacket" },
            { "dean_winchester", "TMM_Sig_dean_leather_jacket" },
            { "TMM_Hero_sam_winchester", "TMM_Sig_sam_hunting_flannel" },
            { "sam_winchester", "TMM_Sig_sam_hunting_flannel" },
            { "TMM_Hero_preston_garvey", "TMM_Sig_preston_garvey_hat" },
            { "preston_garvey", "TMM_Sig_preston_garvey_hat" },
            { "TMM_Hero_bobby_singer", "TMM_Sig_bobby_singer_cap_vest" },
            { "bobby_singer", "TMM_Sig_bobby_singer_cap_vest" },
            { "TMM_Hero_castiel", "TMM_Sig_castiel_trenchcoat" },
            { "castiel", "TMM_Sig_castiel_trenchcoat" },
            { "TMM_Hero_michael", "TMM_Sig_michael_archangel_plate" },
            { "michael", "TMM_Sig_michael_archangel_plate" },
            { "TMM_Hero_lucifer", "TMM_Sig_lucifer_white_suit" },
            { "lucifer", "TMM_Sig_lucifer_white_suit" },
            { "TMM_Hero_crowley", "TMM_Sig_crowley_king_suit" },
            { "crowley", "TMM_Sig_crowley_king_suit" },
            { "TMM_Hero_arthur_maxson", "TMM_Sig_maxson_battlecoat" },
            { "arthur_maxson", "TMM_Sig_maxson_battlecoat" },
            { "TMM_Hero_paladin_danse", "TMM_Sig_danse_recon_hood" },
            { "paladin_danse", "TMM_Sig_danse_recon_hood" },
            { "TMM_Hero_henry_casdin", "TMM_Sig_casdin_protector_helm" },
            { "henry_casdin", "TMM_Sig_casdin_protector_helm" },
            { "TMM_Hero_colonel_autumn", "TMM_Sig_autumn_enclave_greatcoat" },
            { "colonel_autumn", "TMM_Sig_autumn_enclave_greatcoat" },
            { "TMM_Hero_father_shaun", "TMM_Sig_father_director_coat" },
            { "father_shaun", "TMM_Sig_father_director_coat" },
            { "TMM_Hero_kellogg", "TMM_Sig_kellogg_cyber_jacket" },
            { "kellogg", "TMM_Sig_kellogg_cyber_jacket" },
            { "TMM_Hero_desdemona", "TMM_Sig_desdemona_railroad_coat" },
            { "desdemona", "TMM_Sig_desdemona_railroad_coat" },
            { "TMM_Hero_deacon", "TMM_Sig_deacon_sunglasses" },
            { "deacon", "TMM_Sig_deacon_sunglasses" },
            { "TMM_Hero_captain_wes", "TMM_Sig_wes_gunner_coat" },
            { "captain_wes", "TMM_Sig_wes_gunner_coat" },
            { "TMM_Hero_stanislaus_braun", "TMM_Sig_braun_white_suit" },
            { "stanislaus_braun", "TMM_Sig_braun_white_suit" },
            { "TMM_Hero_dr_hess", "TMM_Sig_dr_hess_monocle" },
            { "dr_hess", "TMM_Sig_dr_hess_monocle" },
            { "TMM_Hero_arthur_ketch", "TMM_Sig_ketch_arsenal_briefcase_coat" },
            { "arthur_ketch", "TMM_Sig_ketch_arsenal_briefcase_coat" },
            { "TMM_Hero_john_hancock", "TMM_Sig_hancock_tricorn_coat" },
            { "john_hancock", "TMM_Sig_hancock_tricorn_coat" },
            { "TMM_Hero_kent_connolly", "TMM_Sig_silver_shroud_costume" },
            { "kent_connolly", "TMM_Sig_silver_shroud_costume" },
            { "TMM_Hero_fox", "TMM_Sig_fawkes_vault87_harness" },
            { "fawkes", "TMM_Sig_fawkes_vault87_harness" },
            { "TMM_Hero_alpha_vampire", "TMM_Sig_alpha_vampire_cloak" },
            { "alpha_vampire", "TMM_Sig_alpha_vampire_cloak" },
            { "TMM_Hero_dick_roman", "TMM_Sig_dick_roman_corporate_suit" },
            { "dick_roman", "TMM_Sig_dick_roman_corporate_suit" }
        };

        private static readonly Dictionary<string, string> HeroSignatureWeapons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TMM_Hero_ruby", "TMM_Sig_ruby_demon_knife" },
            { "ruby", "TMM_Sig_ruby_demon_knife" },
            { "TMM_Hero_swan", "TMM_Sig_swan_anchor_club" },
            { "swan", "TMM_Sig_swan_anchor_club" },
            // Heroes with a signature weapon as well as signature apparel (moved here from the apparel map).
            { "TMM_Hero_dean_winchester", "TMM_Sig_dean_sawed_off_shotgun" },
            { "dean_winchester", "TMM_Sig_dean_sawed_off_shotgun" },
            { "TMM_Hero_castiel", "TMM_Sig_castiel_angel_blade" },
            { "castiel", "TMM_Sig_castiel_angel_blade" },
            { "TMM_Hero_kellogg", "TMM_Sig_kellogg_revolver" },
            { "kellogg", "TMM_Sig_kellogg_revolver" }
        };

        public static bool CanPawnUseSignatureItem(Pawn pawn, ThingDef itemDef, out string failReason)
        {
            failReason = null;
            if (pawn == null || itemDef == null) return true;

            SignatureGearExtension ext = itemDef.GetModExtension<SignatureGearExtension>();
            if (ext == null) return true;

            if (!ext.requiredXenotype.NullOrEmpty() && !MatchesRequiredRace(pawn, ext.requiredXenotype))
            {
                if (ext.requiredXenotype == "TMM_Angel") failReason = "Only Angels can wear winged angelic apparel.";
                else if (ext.requiredXenotype == "TMM_SuperMutant") failReason = "Only Super Mutants fit this oversized armor.";
                else failReason = "Incompatible race (" + ext.requiredXenotype + ").";
                return false;
            }

            if (ext.requiresMutantOrPowerArmor && !IsSuperMutantOrPowerArmored(pawn))
            {
                failReason = "Too heavy (18 kg): requires Super Mutant strength or Power Armor.";
                return false;
            }

            return true;
        }

        public static bool MatchesRequiredRace(Pawn pawn, string requiredXenotype)
        {
            if (pawn == null || requiredXenotype.NullOrEmpty()) return true;

            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == requiredXenotype) return true;

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

            if (requiredXenotype == "TMM_SuperMutant" && pawn.story != null && pawn.story.bodyType != null)
            {
                if (pawn.story.bodyType.defName == "TMM_SuperMutant") return true;
            }

            return false;
        }

        public static bool IsSuperMutantOrPowerArmored(Pawn pawn)
        {
            if (pawn == null) return false;
            if (MatchesRequiredRace(pawn, "TMM_SuperMutant")) return true;

            if (pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    Apparel ap = worn[i];
                    if (ap == null || ap.def == null || ap.def.apparel == null || ap.def.apparel.layers == null) continue;
                    for (int l = 0; l < ap.def.apparel.layers.Count; l++)
                    {
                        if (ap.def.apparel.layers[l] != null && ap.def.apparel.layers[l].defName == "TMM_PowerArmor")
                        {
                            return true;
                        }
                    }
                    if (ap.def.defName.IndexOf("PowerArmor", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }

            return false;
        }

        public static bool IsDemon(Pawn pawn)
        {
            if (pawn == null) return false;
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == "TMM_Demon") return true;
                GeneDef g = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_DemonHolyBane");
                if (g != null && pawn.genes.HasActiveGene(g)) return true;
            }
            return false;
        }

        public static bool IsAngel(Pawn pawn)
        {
            if (pawn == null) return false;
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == "TMM_Angel") return true;
                GeneDef g = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_AngelCelestialGrace");
                if (g != null && pawn.genes.HasActiveGene(g)) return true;
            }
            return false;
        }

        public static bool IsDemonOrAnomaly(Pawn pawn)
        {
            if (pawn == null) return false;
            if (IsDemon(pawn)) return true;
            if (ModsConfig.AnomalyActive && pawn.RaceProps != null && pawn.RaceProps.IsAnomalyEntity) return true; // CHECK API
            return false;
        }

        /// <summary>
        /// Returns true if the pawn is a registered hero inside HeroRegistry.
        /// Castiel's blade and Ruby's knife only one-shot REGULAR (non-hero) demons/angels.
        /// </summary>
        public static bool IsRegisteredHero(Pawn pawn)
        {
            if (pawn == null || HeroRegistry.Instance == null) return false;

            if (!s_heroRegChecked)
            {
                s_heroRegChecked = true;
                Type regType = HeroRegistry.Instance.GetType();
                // CHECK API: HeroRegistry lookup method or storage field
                s_heroGetDefMethod = AccessTools.Method(regType, "GetHeroDef", new Type[] { typeof(Pawn) }) ??
                                     AccessTools.Method(regType, "DefOf", new Type[] { typeof(Pawn) }) ??
                                     AccessTools.Method(regType, "IsHero", new Type[] { typeof(Pawn) });

                FieldInfo[] fields = regType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    Type ft = fields[i].FieldType;
                    if (typeof(IDictionary).IsAssignableFrom(ft) && s_heroDictField == null) s_heroDictField = fields[i];
                    else if (typeof(IList).IsAssignableFrom(ft) && s_heroListField == null) s_heroListField = fields[i];
                }
            }

            try
            {
                if (s_heroGetDefMethod != null)
                {
                    object res = s_heroGetDefMethod.Invoke(HeroRegistry.Instance, new object[] { pawn });
                    if (res is bool) return (bool)res;
                    if (res != null) return true;
                }

                if (s_heroDictField != null)
                {
                    IDictionary dict = s_heroDictField.GetValue(HeroRegistry.Instance) as IDictionary;
                    if (dict != null)
                    {
                        foreach (DictionaryEntry e in dict)
                        {
                            Pawn p = e.Value as Pawn;
                            if (p != null && (p == pawn || p.thingIDNumber == pawn.thingIDNumber)) return true;
                        }
                    }
                }

                if (s_heroListField != null)
                {
                    IList list = s_heroListField.GetValue(HeroRegistry.Instance) as IList;
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            Pawn p = list[i] as Pawn;
                            if (p != null && (p == pawn || p.thingIDNumber == pawn.thingIDNumber)) return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore reflection mismatch
            }

            return false;
        }

        public static bool IsWearingDef(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.apparel == null || defName.NullOrEmpty()) return false;
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] != null && worn[i].def != null && worn[i].def.defName == defName) return true;
            }
            return false;
        }

        public static bool IsWieldingDef(Pawn pawn, string defName)
        {
            return pawn != null && pawn.equipment != null && pawn.equipment.Primary != null &&
                   pawn.equipment.Primary.def != null && pawn.equipment.Primary.def.defName == defName;
        }

        /// <summary>
        /// Evaluates all 3 gear sets (Dean, Castiel, Kellogg) and adds/removes their Hediffs immediately.
        /// </summary>
        public static void RefreshGearSets(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;

            bool deanSet = IsWearingDef(pawn, "TMM_Sig_dean_leather_jacket") &&
                           IsWieldingDef(pawn, "TMM_Sig_dean_sawed_off_shotgun");
            SetHediffState(pawn, "TMM_Hediff_Set_Dean", deanSet);

            bool castielSet = IsWearingDef(pawn, "TMM_Sig_castiel_trenchcoat") &&
                              IsWieldingDef(pawn, "TMM_Sig_castiel_angel_blade");
            SetHediffState(pawn, "TMM_Hediff_Set_Castiel", castielSet);

            bool kelloggSet = IsWearingDef(pawn, "TMM_Sig_kellogg_cyber_jacket") &&
                              IsWieldingDef(pawn, "TMM_Sig_kellogg_revolver");
            SetHediffState(pawn, "TMM_Hediff_Set_Kellogg", kelloggSet);
        }

        private static void SetHediffState(Pawn pawn, string hediffDefName, bool shouldHave)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (shouldHave && existing == null)
            {
                pawn.health.AddHediff(def);
            }
            else if (!shouldHave && existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        public static bool HasSetBonus(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            return def != null && pawn.health.hediffSet.GetFirstHediffOfDef(def) != null;
        }

        public static void EnsureHeroSignatureEquipment(HeroDef heroDef, Pawn pawn)
        {
            if (heroDef == null || pawn == null || pawn.Dead) return;

            string key1 = heroDef.defName;
            string key2 = null;
            FieldInfo idField = AccessTools.Field(heroDef.GetType(), "heroId"); // CHECK API
            if (idField != null)
            {
                object v = idField.GetValue(heroDef);
                if (v != null) key2 = v.ToString();
            }

            string apparelDefName = null;
            if (!key1.NullOrEmpty() && HeroSignatureApparel.TryGetValue(key1, out apparelDefName)) { }
            else if (!key2.NullOrEmpty()) HeroSignatureApparel.TryGetValue(key2, out apparelDefName);

            if (!apparelDefName.NullOrEmpty() && pawn.apparel != null && !IsWearingDef(pawn, apparelDefName))
            {
                ThingDef apDef = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);
                string reason;
                if (apDef != null && CanPawnUseSignatureItem(pawn, apDef, out reason))
                {
                    Apparel ap = ThingMaker.MakeThing(apDef, apDef.MadeFromStuff ? GenStuff.DefaultStuffFor(apDef) : null) as Apparel;
                    if (ap != null)
                    {
                        pawn.apparel.Wear(ap, false);
                    }
                }
            }

            string weaponDefName = null;
            if (!key1.NullOrEmpty() && HeroSignatureWeapons.TryGetValue(key1, out weaponDefName)) { }
            else if (!key2.NullOrEmpty()) HeroSignatureWeapons.TryGetValue(key2, out weaponDefName);

            if (!weaponDefName.NullOrEmpty() && pawn.equipment != null && !IsWieldingDef(pawn, weaponDefName))
            {
                ThingDef wDef = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
                string reason;
                if (wDef != null && CanPawnUseSignatureItem(pawn, wDef, out reason))
                {
                    ThingWithComps w = ThingMaker.MakeThing(wDef, wDef.MadeFromStuff ? GenStuff.DefaultStuffFor(wDef) : null) as ThingWithComps;
                    if (w != null)
                    {
                        pawn.equipment.DestroyAllEquipment();
                        pawn.equipment.AddEquipment(w);
                    }
                }
            }

            RefreshGearSets(pawn);
        }

        public static bool IsRecipeUnlocked(SignatureGearRecipeExtension ext)
        {
            if (ext == null) return true;
            if (ext.hideWhenAnomalyActive && ModsConfig.AnomalyActive) return false;

            if (!ext.requiredStoryFlag.NullOrEmpty() && IsStoryFlagTrue(ext.requiredStoryFlag)) return true;

            if (ext.altStoryFlags != null)
            {
                for (int i = 0; i < ext.altStoryFlags.Count; i++)
                {
                    if (!ext.altStoryFlags[i].NullOrEmpty() && IsStoryFlagTrue(ext.altStoryFlags[i])) return true;
                }
            }
            return false;
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
    }

    // =========================================================================
    // HARMONY PATCHES (EXPLICIT REGISTRATION)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class SignatureGearHarmony
    {
        static SignatureGearHarmony()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.signaturegear");

            // 1. Race / heavy weapon guards
            MethodInfo hasParts = AccessTools.Method(typeof(ApparelUtility), "HasPartsToWear", new Type[] { typeof(Pawn), typeof(ThingDef) }); // CHECK API
            if (hasParts != null)
            {
                harmony.Patch(hasParts, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_HasPartsToWear"));
            }

            MethodInfo canEquip = AccessTools.Method(typeof(EquipmentUtility), "CanEquip",
                new Type[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }); // CHECK API
            if (canEquip != null)
            {
                harmony.Patch(canEquip, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_CanEquip"));
            }

            MethodInfo wear = AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear",
                new Type[] { typeof(Apparel), typeof(bool), typeof(bool) }); // CHECK API
            if (wear != null)
            {
                harmony.Patch(wear, prefix: new HarmonyMethod(typeof(SignatureGearHarmony), "Prefix_ApparelTracker_Wear"));
            }

            // 2. Set bonus activation & removal hooks (apparel + equipment + spawn)
            MethodInfo appAdded = AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded", new Type[] { typeof(Apparel) }); // CHECK API
            if (appAdded != null)
            {
                harmony.Patch(appAdded, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_ApparelChanged"));
            }

            MethodInfo appRemoved = AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved", new Type[] { typeof(Apparel) }); // CHECK API
            if (appRemoved != null)
            {
                harmony.Patch(appRemoved, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_ApparelChanged"));
            }

            MethodInfo eqAdded = AccessTools.Method(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded", new Type[] { typeof(ThingWithComps) }); // CHECK API
            if (eqAdded != null)
            {
                harmony.Patch(eqAdded, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_EquipmentChanged"));
            }

            MethodInfo eqRemoved = AccessTools.Method(typeof(Pawn_EquipmentTracker), "Notify_EquipmentRemoved", new Type[] { typeof(ThingWithComps) }); // CHECK API
            if (eqRemoved != null)
            {
                harmony.Patch(eqRemoved, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_EquipmentChanged"));
            }

            MethodInfo spawnSetup = AccessTools.Method(typeof(Pawn), "SpawnSetup", new Type[] { typeof(Map), typeof(bool) });
            if (spawnSetup != null)
            {
                harmony.Patch(spawnSetup, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_Pawn_SpawnSetup"));
            }

            // 3. Castiel's blade & Ruby's knife one-hit kill on REGULAR (non-hero) demons / angels
            MethodInfo postApply = AccessTools.Method(typeof(Pawn), "PostApplyDamage", new Type[] { typeof(DamageInfo), typeof(float) }); // CHECK API
            if (postApply != null)
            {
                harmony.Patch(postApply, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_Pawn_PostApplyDamage"));
            }

            // 4. Dean's Set fear/panic immunity
            MethodInfo tryMental = AccessTools.Method(typeof(MentalStateHandler), "TryStartMentalState"); // CHECK API
            if (tryMental != null)
            {
                harmony.Patch(tryMental, prefix: new HarmonyMethod(typeof(SignatureGearHarmony), "Prefix_TryStartMentalState"));
            }

            MethodInfo addHediff = AccessTools.Method(typeof(Pawn_HealthTracker), "AddHediff",
                new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) }); // CHECK API
            if (addHediff != null)
            {
                harmony.Patch(addHediff, prefix: new HarmonyMethod(typeof(SignatureGearHarmony), "Prefix_AddHediff"));
            }

            // 5. Castiel's Set -20% cooldown on angelic abilities
            MethodInfo startCooldown = AccessTools.Method(typeof(Ability), "StartCooldown", new Type[] { typeof(int) }); // CHECK API
            if (startCooldown != null)
            {
                harmony.Patch(startCooldown, prefix: new HarmonyMethod(typeof(SignatureGearHarmony), "Prefix_Ability_StartCooldown"));
            }

            // 6. Story-flag recipe gating (unlock.<id>=true)
            PropertyInfo availProp = AccessTools.Property(typeof(RecipeDef), "AvailableNow"); // CHECK API
            if (availProp != null && availProp.GetGetMethod() != null)
            {
                harmony.Patch(availProp.GetGetMethod(), postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_RecipeDef_AvailableNow"));
            }

            // 7. HeroRegistry.GetOrCreate runtime safeguard: ensures recruited/generated heroes wear their signature gear
            Type regType = AccessTools.TypeByName("TurboMegaMod.HeroRegistry");
            if (regType != null)
            {
                MethodInfo getOrCreate = AccessTools.Method(regType, "GetOrCreate", new Type[] { typeof(HeroDef) }); // CHECK API
                if (getOrCreate != null)
                {
                    harmony.Patch(getOrCreate, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_HeroRegistry_GetOrCreate"));
                }
            }
        }

        public static void Postfix_HasPartsToWear(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result || p == null || apparel == null) return;
            string reason;
            if (!SignatureGearUtil.CanPawnUseSignatureItem(p, apparel, out reason))
            {
                __result = false;
            }
        }

        public static void Postfix_CanEquip(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (!__result || thing == null || pawn == null) return;
            string reason;
            if (!SignatureGearUtil.CanPawnUseSignatureItem(pawn, thing.def, out reason))
            {
                cantReason = reason;
                __result = false;
            }
        }

        public static bool Prefix_ApparelTracker_Wear(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (__instance == null || __instance.pawn == null || newApparel == null) return true;
            string reason;
            if (!SignatureGearUtil.CanPawnUseSignatureItem(__instance.pawn, newApparel.def, out reason))
            {
                return false;
            }
            return true;
        }

        public static void Postfix_ApparelChanged(Pawn_ApparelTracker __instance)
        {
            if (__instance != null && __instance.pawn != null)
            {
                SignatureGearUtil.RefreshGearSets(__instance.pawn);
            }
        }

        public static void Postfix_EquipmentChanged(Pawn_EquipmentTracker __instance)
        {
            if (__instance != null && __instance.pawn != null)
            {
                SignatureGearUtil.RefreshGearSets(__instance.pawn);
            }
        }

        public static void Postfix_Pawn_SpawnSetup(Pawn __instance)
        {
            if (__instance != null)
            {
                SignatureGearUtil.RefreshGearSets(__instance);
            }
        }

        public static void Postfix_Pawn_PostApplyDamage(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || __instance.Dead) return;
            if (dinfo.Def != null && dinfo.Def.isRanged) return;

            Pawn attacker = dinfo.Instigator as Pawn;
            ThingDef weaponDef = dinfo.Weapon;
            if (weaponDef == null && attacker != null && attacker.equipment != null && attacker.equipment.Primary != null)
            {
                weaponDef = attacker.equipment.Primary.def;
            }
            if (weaponDef == null) return;

            SignatureGearExtension ext = weaponDef.GetModExtension<SignatureGearExtension>();
            if (ext == null || (!ext.slaysRegularDemons && !ext.slaysRegularAngels)) return;

            // Claude's rule: Heroes in HeroRegistry are NEVER one-shot by Castiel's blade or Ruby's knife.
            if (SignatureGearUtil.IsRegisteredHero(__instance)) return;

            bool shouldSlay = false;
            if (ext.slaysRegularDemons && SignatureGearUtil.IsDemon(__instance)) shouldSlay = true;
            if (ext.slaysRegularAngels && SignatureGearUtil.IsAngel(__instance)) shouldSlay = true;

            if (shouldSlay && !__instance.Dead)
            {
                __instance.Kill(dinfo);
            }
        }

        public static bool Prefix_TryStartMentalState(MentalStateHandler __instance, MentalStateDef stateDef, ref bool __result)
        {
            if (stateDef == null) return true;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null || !SignatureGearUtil.HasSetBonus(pawn, "TMM_Hediff_Set_Dean")) return true;

            if (stateDef == MentalStateDefOf.PanicFlee ||
                stateDef.defName.IndexOf("Panic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                stateDef.defName.IndexOf("Terror", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static bool Prefix_AddHediff(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (hediff == null || hediff.def == null) return true;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null || !SignatureGearUtil.HasSetBonus(pawn, "TMM_Hediff_Set_Dean")) return true;

            if (hediff.def.defName == "TMM_Hediff_WarRoarFear" ||
                hediff.def.defName.IndexOf("Fear", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }
            return true;
        }

        public static void Prefix_Ability_StartCooldown(Ability __instance, ref int ticks)
        {
            if (__instance == null || __instance.pawn == null || __instance.def == null) return;
            if (!SignatureGearUtil.HasSetBonus(__instance.pawn, "TMM_Hediff_Set_Castiel")) return;

            string dn = __instance.def.defName;
            if (dn == "TMM_Ability_HealingTouch" || dn == "TMM_Ability_TrueVisage" ||
                dn.IndexOf("Angel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ticks = Mathf.Max(60, Mathf.RoundToInt(ticks * 0.80f));
            }
        }

        public static void Postfix_RecipeDef_AvailableNow(RecipeDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            SignatureGearRecipeExtension ext = __instance.GetModExtension<SignatureGearRecipeExtension>();
            if (ext == null) return;

            if (!SignatureGearUtil.IsRecipeUnlocked(ext))
            {
                __result = false;
            }
        }

        public static void Postfix_HeroRegistry_GetOrCreate(HeroDef def, Pawn __result)
        {
            if (def != null && __result != null)
            {
                SignatureGearUtil.EnsureHeroSignatureEquipment(def, __result);
            }
        }
    }
}
