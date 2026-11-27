using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    /// An execution that is meant only to wound (Melee Animation, OutcomeUtility.Damage) picks a body part, checks that the
    /// wound would neither kill, down nor take the part off, and then deals the blow. The blow was sent without that part, so
    /// the game hit a part of its own choosing; and the check used the damage's plain wound even where the game puts another
    /// one (a crack in a bone). "Accidentally killed or downed X when attempting to just injure" followed. The blow now goes to
    /// the checked part, and the check uses the wound the game will really put there.
    [StaticConstructorOnStartup]
    public static class MeleeAnimationDamagePart
    {
        private static readonly FieldInfo plainHediff = AccessTools.Field(typeof(DamageDef), "hediff");
        private static readonly ConstructorInfo damageInfo = AccessTools.Constructor(typeof(DamageInfo), new[] { typeof(DamageDef), typeof(float), typeof(float), typeof(float), typeof(Thing), typeof(BodyPartRecord), typeof(ThingDef), typeof(DamageInfo.SourceCategory), typeof(Thing), typeof(bool), typeof(bool), typeof(RimWorld.QualityCategory), typeof(bool), typeof(bool) });
        private static int patched;

        static MeleeAnimationDamagePart()
        {
            Type outcome = AccessTools.TypeByName("AM.Outcome.OutcomeUtility");
            MethodInfo damage = outcome == null ? null : AccessTools.Method(outcome, "Damage");
            if (damage == null) return;
            new Harmony("turbomegamod.melee.damagepart").Patch(damage, transpiler: new HarmonyMethod(typeof(MeleeAnimationDamagePart), "Transpiler"));
            if (patched != 2) Log.Warning("[TMM] Melee Animation: the wound-only blow was not recognised (" + patched + "/2), left as it is.");
        }

        public static HediffDef WoundFor(DamageDef def, Pawn pawn, BodyPartRecord part)
        {
            if (def == null) return null;
            if (pawn == null || part == null || pawn.health == null) return def.hediff;
            return Verse.HealthUtility.GetHediffDefFromDamage(def, pawn, part) ?? def.hediff;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source, MethodBase original)
        {
            var code = new List<CodeInstruction>(source);
            patched = 0;
            // The part is the local that GetRandomNotMissingPart is stored in.
            object partLocal = null; int partAt = -1;
            for (int i = 0; i + 1 < code.Count; i++)
            {
                var called = code[i].operand as MethodInfo;
                if ((code[i].opcode == OpCodes.Callvirt || code[i].opcode == OpCodes.Call) && called != null && called.Name == "GetRandomNotMissingPart" && code[i + 1].IsStloc())
                { partLocal = code[i + 1].operand; partAt = i + 1; break; }
            }
            if (partLocal == null) return code;
            int ctor = code.FindIndex(c => c.opcode == OpCodes.Call && c.operand as ConstructorInfo == damageInfo);
            // The check: "ldfld DamageDef::hediff" right before the part is pushed for WouldBeInvalidResult.
            for (int i = partAt; i + 1 < code.Count && (ctor < 0 || i < ctor); i++)
            {
                if (code[i].opcode == OpCodes.Ldfld && code[i].operand as FieldInfo == plainHediff)
                {
                    code[i] = new CodeInstruction(OpCodes.Ldarg_1) { labels = code[i].labels, blocks = code[i].blocks };
                    code.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc_S, partLocal));
                    code.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MeleeAnimationDamagePart), "WoundFor")));
                    patched++;
                    ctor += 2;
                    break;
                }
            }
            // The blow: the sixth argument of DamageInfo (hitPart) was a plain null.
            if (ctor > 0)
            {
                // Eight arguments follow hitPart (weapon, category, intendedTarget and five flags), so it is the ninth push before the call.
                int hit = ctor - 9;
                if (hit > 0 && code[hit].opcode == OpCodes.Ldnull)
                {
                    code[hit] = new CodeInstruction(OpCodes.Ldloc_S, partLocal) { labels = code[hit].labels, blocks = code[hit].blocks };
                    patched++;
                }
            }
            return code;
        }

    }
}
