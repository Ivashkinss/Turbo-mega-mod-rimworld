using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    // WorldPawns ticks its own registry directly, bypassing IThingHolderTickable.
    // Only personnel inside our abstract holder are suspended; map/caravan pawns keep native ticks.
    [StaticConstructorOnStartup]
    public static class StrategicOutpostPawnLifecycle
    {
        static StrategicOutpostPawnLifecycle()
        {
            var harmony = new Harmony("turbomegamod.outpost.pawn.lifecycle");
            foreach (var name in new[] { "Tick", "TickInterval", "TickMothballed" })
            {
                var target = AccessTools.DeclaredMethod(typeof(Pawn), name);
                if (target != null) harmony.Patch(target, prefix: new HarmonyMethod(typeof(StrategicOutpostPawnLifecycle), "BeforeTick"));
            }
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Pawn), "Notify_PassedToWorld"), prefix: new HarmonyMethod(typeof(StrategicOutpostPawnLifecycle), "BeforeNotify"));
            var autoTend = AccessTools.DeclaredMethod(typeof(WorldPawns), "ShouldAutoTendTo");
            if (autoTend != null) harmony.Patch(autoTend, prefix: new HarmonyMethod(typeof(StrategicOutpostPawnLifecycle), "BeforeAutoTend"));
        }
        private static bool Stored(Pawn pawn)
        {
            var holder = pawn.holdingOwner == null ? null : pawn.holdingOwner.Owner as OutpostPawnHolder;
            return holder != null && holder.Parent != null && holder.Parent.Spawned;
        }
        private static bool BeforeNotify(Pawn __instance)
        {
            if (!Stored(__instance)) return true;
            // Native free-world entry randomly reassigns player pawns to an NPC faction.
            // Stationed personnel are still owned, so preserve identity/faction/comps instead.
            var timestamp = AccessTools.Field(typeof(Pawn), "becameWorldPawnTickAbs");
            if (timestamp != null) timestamp.SetValue(__instance, GenTicks.TicksAbs);
            return false;
        }
        private static bool BeforeAutoTend(Pawn __0, ref bool __result)
        {
            if (!Stored(__0)) return true;
            __result = false;
            return false;
        }
        private static bool BeforeTick(Pawn __instance)
        {
            return !Stored(__instance);
        }
    }
}
