using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Compatibility patch for Melee Animation (zAnimationMod).
    /// Fixes "Failed to find random verb for weapon '{0}' on pawn {1}":
    /// When a pawn uses an improvised or toolless weapon (like a beer bottle or wood log),
    /// or when a weapon's melee tools have zero selection weight in RimWorld's verb selector,
    /// Melee Animation's 1000-iteration loop in OutcomeUtility.Damage would fail and log an ERROR.
    /// This patch gracefully handles such weapons by falling back to the pawn's natural melee attack (fist/kick)
    /// and silently skipping weapon animation selection, preventing errors and combat breakage.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MeleeAnimationCompat
    {
        static MeleeAnimationCompat()
        {
            try
            {
                Type coreType = AccessTools.TypeByName("AM.Core");
                if (coreType == null) return;

                Harmony harmony = new Harmony("turbomegamod.melee.improvised");

                // 1. Patch AM.Extensions.GetFirstMeleeWeapon(Pawn) and (Pawn, out ItemTweakData)
                Type extensionsType = AccessTools.TypeByName("AM.Extensions");
                if (extensionsType != null)
                {
                    MethodInfo getFirstMelee1 = AccessTools.Method(extensionsType, "GetFirstMeleeWeapon", new Type[] { typeof(Pawn) });
                    if (getFirstMelee1 != null)
                    {
                        harmony.Patch(getFirstMelee1, postfix: new HarmonyMethod(typeof(MeleeAnimationCompat), "GetFirstMeleeWeapon_Postfix"));
                    }

                    Type tweakType = AccessTools.TypeByName("AM.Tweaks.ItemTweakData");
                    if (tweakType != null)
                    {
                        MethodInfo getFirstMelee2 = AccessTools.Method(extensionsType, "GetFirstMeleeWeapon", new Type[] { typeof(Pawn), tweakType.MakeByRefType() });
                        if (getFirstMelee2 != null)
                        {
                            harmony.Patch(getFirstMelee2, postfix: new HarmonyMethod(typeof(MeleeAnimationCompat), "GetFirstMeleeWeapon_Postfix"));
                        }
                    }
                }

                // 2. Patch AM.Idle.IdleControllerComp.GetMeleeWeapon()
                Type idleCompType = AccessTools.TypeByName("AM.Idle.IdleControllerComp");
                if (idleCompType != null)
                {
                    MethodInfo getMeleeWeapon = AccessTools.Method(idleCompType, "GetMeleeWeapon");
                    if (getMeleeWeapon != null)
                    {
                        harmony.Patch(getMeleeWeapon, postfix: new HarmonyMethod(typeof(MeleeAnimationCompat), "Idle_GetMeleeWeapon_Postfix"));
                    }
                }

                // 3. Patch AM.Outcome.OutcomeUtility.Damage
                Type outcomeUtilType = AccessTools.TypeByName("AM.Outcome.OutcomeUtility");
                if (outcomeUtilType != null)
                {
                    MethodInfo damageMethod = AccessTools.Method(outcomeUtilType, "Damage");
                    if (damageMethod != null)
                    {
                        harmony.Patch(damageMethod, prefix: new HarmonyMethod(typeof(MeleeAnimationCompat), "OutcomeUtility_Damage_Prefix"));
                    }
                }

                Log.Message("[Turbo mega mod] MeleeAnimationCompat initialized successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("[Turbo mega mod] Error initializing MeleeAnimationCompat: " + ex);
            }
        }

        /// Melee Animation asks for a pawn's melee weapon from its worker threads too (friendly duels, automatic throws).
        /// Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList rebuilds the pawn's shared verb lists and must stay on the main thread:
        /// called from a worker it broke the lists the game was reading ("Collection was modified", hundreds of errors per
        /// minute with many pawns). Off the main thread the answer last worked out on the main thread for this pawn and weapon is
        /// used; without one, a weapon with melee tools counts as usable, as it would without this check.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, bool> usableCache = new System.Collections.Concurrent.ConcurrentDictionary<long, bool>();

        public static bool HasUsableWeaponVerb(Pawn pawn, ThingWithComps weapon)
        {
            if (pawn == null || weapon == null) return false;
            if (weapon.def == null || weapon.def.tools.NullOrEmpty()) return false;
            if (pawn.meleeVerbs == null) return false;
            long key = ((long)pawn.thingIDNumber << 32) | (uint)weapon.thingIDNumber;
            if (!UnityData.IsInMainThread)
            {
                bool known;
                return usableCache.TryGetValue(key, out known) ? known : true;
            }
            bool usable = false;
            List<VerbEntry> list = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(false);
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Verb v = list[i].verb;
                    if (v != null && v.EquipmentSource == weapon && list[i].GetSelectionWeight(null) > 0f) { usable = true; break; }
                }
            }
            if (usableCache.Count > 4096) usableCache.Clear();
            usableCache[key] = usable;
            return usable;
        }

        public static void GetFirstMeleeWeapon_Postfix(Pawn pawn, ref ThingWithComps __result)
        {
            if (__result != null && !HasUsableWeaponVerb(pawn, __result))
            {
                __result = null;
            }
        }

        public static void Idle_GetMeleeWeapon_Postfix(object __instance, ref Thing __result)
        {
            ThingWithComps twc = __result as ThingWithComps;
            if (twc != null)
            {
                ThingComp comp = __instance as ThingComp;
                Pawn pawn = comp != null ? comp.parent as Pawn : null;
                if (!HasUsableWeaponVerb(pawn, twc))
                {
                    __result = null;
                }
            }
        }

        public static bool OutcomeUtility_Damage_Prefix(Pawn attacker, Pawn pawn, ref bool __result)
        {
            ThingWithComps weapon = (attacker != null && attacker.equipment != null) ? attacker.equipment.Primary : null;

            if (weapon != null && !HasUsableWeaponVerb(attacker, weapon))
            {
                // Improvised or toolless weapon without usable melee verbs:
                // Deal normal melee damage with pawn's basic/unarmed verb without triggering Melee Animation's 1000-iteration loop.
                Verb verb = (attacker != null && attacker.meleeVerbs != null) ? attacker.meleeVerbs.TryGetMeleeVerb(pawn) : null;
                if (verb != null && pawn != null && pawn.Spawned && !pawn.Dead)
                {
                    DamageDef def = VerbUtility.GetDamageDef(verb);
                    if (def == null) def = DamageDefOf.Blunt;
                    float dmg = verb.verbProps.AdjustedMeleeDamageAmount(verb, attacker);
                    float pen = verb.verbProps.AdjustedArmorPenetration(verb, attacker);
                    BodyPartGroupDef group = (verb.tool != null) ? verb.verbProps.AdjustedLinkedBodyPartsGroup(verb.tool) : null;
                    BodyPartRecord part = (pawn.health != null && pawn.health.hediffSet != null) ? pawn.health.hediffSet.GetRandomNotMissingPart(def, BodyPartHeight.Undefined, BodyPartDepth.Outside) : null;
                    DamageInfo dinfo = new DamageInfo(def, dmg, pen, -1f, attacker, part, weapon.def);
                    if (group != null) dinfo.SetWeaponBodyPartGroup(group);
                    pawn.TakeDamage(dinfo);
                }
                __result = true;
                return false;
            }
            return true;
        }
    }
}
