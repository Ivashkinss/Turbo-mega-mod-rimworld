using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    internal static class TrafficBackstorySelection
    {
        [ThreadStatic] private static int trafficDepth;

        static TrafficBackstorySelection()
        {
            MethodInfo fillSlot = AccessTools.Method(typeof(PawnBioAndNameGenerator), "FillBackstorySlotShuffled");
            if (fillSlot != null) new Harmony("colonyproject.integrated.trafficbackstories").Patch(fillSlot,
                transpiler: new HarmonyMethod(typeof(TrafficBackstorySelection), "KeepCompatibleAdultsInSample"));
        }

        internal static void Enter() { trafficDepth++; }
        internal static void Leave() { trafficDepth--; }

        private static IEnumerable<CodeInstruction> KeepCompatibleAdultsInSample(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (instruction.opcode == OpCodes.Call && called != null && called.DeclaringType == typeof(GenCollection) &&
                    called.Name == "TakeRandom" && called.IsGenericMethod &&
                    called.GetGenericArguments()[0] == typeof(BackstoryDef))
                {
                    var loadSlot = new CodeInstruction(OpCodes.Ldarg_1);
                    loadSlot.labels.AddRange(instruction.labels);
                    instruction.labels.Clear();
                    yield return loadSlot;
                    instruction.operand = AccessTools.Method(typeof(TrafficBackstorySelection), "SampleBackstories");
                }
                yield return instruction;
            }
        }

        private static IEnumerable<BackstoryDef> SampleBackstories(List<BackstoryDef> candidates, int count, BackstorySlot slot)
        {
            // The game's compatibility filter runs after the sample. Evaluate the full valid pool for our
            // adult travellers so a random subset cannot overlook every compatible native backstory.
            return trafficDepth > 0 && slot == BackstorySlot.Adulthood ? candidates : candidates.TakeRandom(count);
        }
    }
}
