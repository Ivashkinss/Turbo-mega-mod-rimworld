using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    internal static class PickUpAndHaulCompatibility
    {
        private static readonly Harmony Harmony = new Harmony("turbomegamod.pickupandhaul.compatibility");

        internal static void Apply()
        {
            if (ModsConfig.IsActive("mehni.pickupandhaul")) return;

            var workGiverType = AccessTools.TypeByName("PickUpAndHaul.WorkGiver_HaulToInventory");
            if (workGiverType != null)
            {
                var jobOnThing = AccessTools.Method(workGiverType, "JobOnThing");
                if (jobOnThing != null)
                {
                    Harmony.Patch(jobOnThing, postfix: new HarmonyMethod(typeof(PickUpAndHaulCompatibility), "Postfix_JobOnThing"));
                }
            }

            var errorCheck = AccessTools.Method(typeof(Toils_Haul), "ErrorCheckForCarry");
            if (errorCheck != null)
            {
                Harmony.Patch(errorCheck, prefix: new HarmonyMethod(typeof(PickUpAndHaulCompatibility), "Prefix_ErrorCheckForCarry"));
            }
        }

        private static void Postfix_JobOnThing(Pawn pawn, Thing thing, Job __result)
        {
            if (__result == null) return;
            if (__result.count <= 0)
            {
                int max = (thing != null && pawn != null && pawn.carryTracker != null) ? pawn.carryTracker.MaxStackSpaceEver(thing.def) : 1;
                int stack = thing != null ? thing.stackCount : 1;
                __result.count = Math.Max(1, Math.Min(stack, max));
            }
            if (__result.countQueue != null)
            {
                for (int i = 0; i < __result.countQueue.Count; i++)
                {
                    if (__result.countQueue[i] <= 0)
                    {
                        __result.countQueue[i] = 1;
                    }
                }
            }
        }

        private static void Prefix_ErrorCheckForCarry(Pawn pawn, Thing haulThing)
        {
            if (pawn != null && pawn.jobs != null && pawn.jobs.curJob != null && pawn.jobs.curJob.count <= 0)
            {
                int max = (haulThing != null && pawn.carryTracker != null) ? pawn.carryTracker.MaxStackSpaceEver(haulThing.def) : 1;
                int stack = haulThing != null ? haulThing.stackCount : 1;
                pawn.jobs.curJob.count = Math.Max(1, Math.Min(stack, max));
            }
        }
    }
}
