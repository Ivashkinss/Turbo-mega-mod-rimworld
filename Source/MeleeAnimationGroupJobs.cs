using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// Melee Animation keeps every pawn of an animation on its own job until the animation ends. A vanilla group (lord) that
    /// changes its state resets the jobs of all its members at once (TransitionAction_EndAllJobs, the exit and assault toils).
    /// A wound dealt by the animation is itself such a signal, so a visitor on the way to a colony lost the animation job to
    /// the answer to that wound: "X has bad job: Y. Cancelling animation". A group's state change leaves a member inside a
    /// running animation alone; its duty is changed as usual, and the pawn follows it as soon as the animation is over.
    /// Orders, damage, death, downing and despawning are not touched.
    [StaticConstructorOnStartup]
    public static class MeleeAnimationGroupJobs
    {
        private static readonly Func<Pawn, object> animatorOf;
        private static readonly PropertyInfo destroyed;
        private static readonly FieldInfo trackerPawn = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");
        private static readonly FieldInfo hasEnsnared;
        private static readonly PropertyInfo grappledPawn, targetDestination;
        private static readonly MethodInfo endGrabAttempt;
        private static int depth;   // nested group state changes being carried out right now

        static MeleeAnimationGroupJobs()
        {
            Type renderer = AccessTools.TypeByName("AM.AnimRenderer");
            if (renderer == null) return;
            Type me = typeof(MeleeAnimationGroupJobs);
            Harmony harmony = new Harmony("turbomegamod.melee.groupjobs");
            MethodInfo find = AccessTools.Method(renderer, "TryGetAnimator", new[] { typeof(Pawn) });
            destroyed = AccessTools.Property(renderer, "IsDestroyed");
            if (find == null || destroyed == null) Log.Warning("[TMM] Melee Animation: the running animation lookup was not found.");
            else
            {
                animatorOf = (Func<Pawn, object>)Delegate.CreateDelegate(typeof(Func<Pawn, object>), find);
                foreach (MethodInfo change in new[] { AccessTools.Method(typeof(Transition), "Execute"), AccessTools.Method(typeof(Lord), "GotoToil") })
                    harmony.Patch(change, prefix: new HarmonyMethod(me, "ChangeStart"), finalizer: new HarmonyMethod(me, "ChangeEnd"));
                harmony.Patch(AccessTools.Method(typeof(Pawn_JobTracker), "EndCurrentJob"), prefix: new HarmonyMethod(me, "EndCurrentJob"));
                harmony.Patch(AccessTools.Method(typeof(Pawn_JobTracker), "CheckForJobOverride"), prefix: new HarmonyMethod(me, "CheckForJobOverride"));
            }
            // A lasso thrown at a pawn that already stands on the spot it is pulled to: there is no flight, Melee Animation took
            // that for a failed flyer. The pull is simply over, and the job goes on to its animation on the next tick.
            Type grapple = AccessTools.TypeByName("AM.Grappling.JobDriver_GrapplePawn"), grab = AccessTools.TypeByName("AM.Grappling.GrabUtility");
            if (grapple == null || grab == null) return;
            hasEnsnared = AccessTools.Field(grapple, "HasEnsnared");
            grappledPawn = AccessTools.Property(grapple, "GrappledPawn");
            targetDestination = AccessTools.Property(grapple, "TargetDestination");
            endGrabAttempt = AccessTools.Method(grab, "EndGrabAttempt", new[] { typeof(Pawn) });
            MethodInfo ensnare = AccessTools.Method(grapple, "Ensnare");
            if (hasEnsnared == null || grappledPawn == null || targetDestination == null || endGrabAttempt == null || ensnare == null)
                Log.Warning("[TMM] Melee Animation: the lasso job was not found.");
            else harmony.Patch(ensnare, prefix: new HarmonyMethod(me, "Ensnare"));
        }

        public static bool InRunningAnimation(Pawn pawn)
        {
            if (animatorOf == null || pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed) return false;
            object renderer = animatorOf(pawn);
            return renderer != null && !(bool)destroyed.GetValue(renderer, null);
        }

        private static void ChangeStart() { depth++; }
        private static Exception ChangeEnd(Exception __exception) { if (depth > 0) depth--; return __exception; }

        private static bool EndCurrentJob(Pawn_JobTracker __instance, JobCondition condition)
        {
            if (depth == 0 || condition != JobCondition.InterruptForced && condition != JobCondition.InterruptOptional) return true;
            return !InRunningAnimation((Pawn)trackerPawn.GetValue(__instance));
        }

        private static bool CheckForJobOverride(Pawn_JobTracker __instance)
        {
            return depth == 0 || !InRunningAnimation((Pawn)trackerPawn.GetValue(__instance));
        }

        private static bool Ensnare(object __instance)
        {
            Pawn target = (Pawn)grappledPawn.GetValue(__instance, null);
            if (target == null || !target.Spawned || (bool)hasEnsnared.GetValue(__instance) || target.Position != (IntVec3)targetDestination.GetValue(__instance, null)) return true;
            hasEnsnared.SetValue(__instance, true);
            endGrabAttempt.Invoke(null, new object[] { target });
            return false;
        }
    }
}