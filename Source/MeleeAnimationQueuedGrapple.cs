using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    /// Melee Animation looks for automatic lasso throws for all pawns at once, on worker threads, and then starts them one
    /// by one (MapPawnProcessor.Tick). A throw found in the same batch can be out of date by its turn: an earlier throw has
    /// already taken the target or the thrower, or the target fell. The start is refused and the mod reports "Failed to give
    /// grapple job to X" as an error. Such a throw is now dropped with a plain note; any other refusal is still reported.
    [StaticConstructorOnStartup]
    public static class MeleeAnimationQueuedGrapple
    {
        private static MethodInfo giveJob, targeted;
        private static JobDef grappleJob;

        static MeleeAnimationQueuedGrapple()
        {
            Type processor = AccessTools.TypeByName("AM.Processing.MapPawnProcessor");
            Type driver = AccessTools.TypeByName("AM.Grappling.JobDriver_GrapplePawn");
            Type grab = AccessTools.TypeByName("AM.Grappling.GrabUtility");
            if (processor == null || driver == null || grab == null) return;
            giveJob = AccessTools.Method(driver, "GiveJob");
            targeted = AccessTools.Method(grab, "IsBeingTargetedForGrapple", new[] { typeof(Pawn) });
            MethodInfo tick = AccessTools.Method(processor, "Tick");
            if (giveJob == null || targeted == null || tick == null) { Log.Warning("[TMM] Melee Animation: the automatic lasso queue was not found."); return; }
            new Harmony("turbomegamod.melee.queuedgrapple").Patch(tick, transpiler: new HarmonyMethod(typeof(MeleeAnimationQueuedGrapple), "Transpiler"));
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            int n = 0;
            foreach (CodeInstruction c in code)
            {
                if (c.opcode == OpCodes.Call && c.operand as MethodInfo == giveJob) { c.operand = Wrapper(); n++; }
                yield return c;
            }
            if (n == 0) Log.Warning("[TMM] Melee Animation: no automatic lasso start was found in the queue.");
        }

        private static MethodInfo wrapperMethod;
        private static MethodInfo Wrapper()
        {
            if (wrapperMethod != null) return wrapperMethod;
            // GiveJob(Pawn grappler, Pawn target, IntVec3 destination, bool ignoreDowned, AnimationStartParameters? start): the
            // last parameter is a Nullable of a Melee Animation struct, so the wrapper is made generic over it.
            Type startType = giveJob.GetParameters()[4].ParameterType.GetGenericArguments()[0];
            wrapperMethod = AccessTools.Method(typeof(MeleeAnimationQueuedGrapple), "GiveQueued").MakeGenericMethod(startType);
            return wrapperMethod;
        }

        public static bool GiveQueued<T>(Pawn grappler, Pawn target, IntVec3 destination, bool ignoreDowned, T? start) where T : struct
        {
            string stale = Stale(grappler, target, ignoreDowned);
            if (stale != null)
            {
                Log.Message("[TMM] Melee Animation: an automatic lasso throw of " + (grappler == null ? "null" : grappler.LabelShort) + " at " + (target == null ? "null" : target.LabelShort) + " was dropped: " + stale + ".");
                return true;
            }
            return (bool)giveJob.Invoke(null, new object[] { grappler, target, destination, ignoreDowned, start });
        }

        private static string Stale(Pawn grappler, Pawn target, bool ignoreDowned)
        {
            if (grappler == null || !grappler.Spawned || grappler.Dead || grappler.Downed) return "the thrower can no longer throw";
            if (grappleJob == null) grappleJob = DefDatabase<JobDef>.GetNamedSilentFail("AM_GrapplePawn");
            if (grappleJob != null && grappler.CurJobDef == grappleJob) return "the thrower is already throwing";
            if (target == null || !target.Spawned || !ignoreDowned && (target.Dead || target.Downed)) return "the target is gone or down";
            if ((bool)targeted.Invoke(null, new object[] { target })) return "another lasso is already on the target";
            return null;
        }
    }
}
