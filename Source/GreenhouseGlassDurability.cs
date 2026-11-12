using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    public sealed class CompGreenhouseGlassIntegrity : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // Old saves can retain the former 120/150 HP above the new maximum.
            if (parent.HitPoints > parent.MaxHitPoints) parent.HitPoints = parent.MaxHitPoints;
        }
    }

    [StaticConstructorOnStartup]
    public static class GreenhouseGlassDurability
    {
        static GreenhouseGlassDurability()
        {
            // These are our unstuffy glass panels, not fortifications. Remove
            // inherited/XML-added material multipliers, including mining armour.
            foreach (string name in new[] { "TMM_GreenhouseGlassWall", "TMM_GreenhouseGlassDoor" })
                DefDatabase<ThingDef>.GetNamed(name).damageMultipliers = new List<DamageMultiplier>();
            new Harmony("turbomegamod.greenhouse.glass.durability").Patch(
                AccessTools.Method(typeof(DamageWorker), "Apply"),
                transpiler: new HarmonyMethod(typeof(GreenhouseGlassDurability), "GlassFactors"));
        }

        public static bool IsGlass(Thing victim)
        {
            return victim != null && (victim.def.defName == "TMM_GreenhouseGlassWall" || victim.def.defName == "TMM_GreenhouseGlassDoor");
        }

        private static float BuildingFactor(float factor, Thing victim, DamageInfo info, bool impassable)
        {
            if (!IsGlass(victim)) return factor;
            // Walls Are Solid changes DamageDefs globally. Opt only our glass
            // out at the point of use, also when its reduction slider is zero.
            // Keep vanilla blast vulnerability (4 for walls, 2 for doors).
            return info.Def.isExplosive && !info.Def.scaleDamageToBuildingsBasedOnFlammability ? (impassable ? 4f : 2f) : 1f;
        }

        private static IEnumerable<CodeInstruction> GlassFactors(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                var field = instruction.operand as FieldInfo;
                if (instruction.opcode != OpCodes.Ldfld || field == null || field.DeclaringType != typeof(DamageDef)) continue;
                bool solid = field.Name == "buildingDamageFactorImpassable";
                if (!solid && field.Name != "buildingDamageFactorPassable") continue;
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(solid ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GreenhouseGlassDurability), "BuildingFactor"));
                replaced++;
            }
            if (replaced != 2) throw new InvalidOperationException("Expected both native building damage factors; found " + replaced);
        }
    }
}
