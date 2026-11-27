using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    /// A lasso throw that ends in an execution keeps the execution for the moment the pull is over
    /// (JobDriver_GrapplePawn.TickEnsnared). If the target was killed on the rope (another colonist's blow, a shot), Melee
    /// Animation still tries to start the execution with the corpse and reports three errors ("Tried to start animation with
    /// 1 or more invalid pawn", "Invalid Pawn", "AnimationOnFinish failed to trigger"). The execution is now called off with a
    /// plain note when one of its pawns is dead, destroyed or off the map; the throw itself ends as before.
    [StaticConstructorOnStartup]
    public static class MeleeAnimationRopeVictim
    {
        private static FieldInfo pending, mainPawn, secondPawn;
        private static MethodInfo flyer;

        static MeleeAnimationRopeVictim()
        {
            Type driver = AccessTools.TypeByName("AM.Grappling.JobDriver_GrapplePawn");
            Type start = AccessTools.TypeByName("AM.AnimationStartParameters");
            if (driver == null || start == null) return;
            pending = AccessTools.Field(driver, "AnimationStartParameters");
            flyer = AccessTools.PropertyGetter(driver, "Flyer");
            mainPawn = AccessTools.Field(start, "MainPawn");
            secondPawn = AccessTools.Field(start, "SecondPawn");
            MethodInfo tick = AccessTools.Method(driver, "TickEnsnared");
            if (pending == null || flyer == null || mainPawn == null || secondPawn == null || tick == null) { Log.Warning("[TMM] Melee Animation: the lasso pull was not found."); return; }
            new Harmony("turbomegamod.melee.ropevictim").Patch(tick, prefix: new HarmonyMethod(typeof(MeleeAnimationRopeVictim), "TickEnsnared"));
        }

        private static bool Gone(Pawn p) { return p == null || p.Dead || p.Destroyed || !p.Spawned; }

        public static void TickEnsnared(object __instance)
        {
            if (flyer.Invoke(__instance, null) != null) return;   // still in flight
            object boxed = pending.GetValue(__instance);          // a boxed AnimationStartParameters, or null
            if (boxed == null) return;
            Pawn a = (Pawn)mainPawn.GetValue(boxed), b = (Pawn)secondPawn.GetValue(boxed);
            if (!Gone(a) && !Gone(b)) return;
            pending.SetValue(__instance, null);
            Log.Message("[TMM] Melee Animation: the execution after the lasso pull was called off: " + (Gone(b) ? (b == null ? "the target" : b.LabelShort) : (a == null ? "the thrower" : a.LabelShort)) + " is no longer there.");
        }
    }
}
