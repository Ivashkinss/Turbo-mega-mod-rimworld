using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class StrategicOutpostCaravanHooks
    {
        static StrategicOutpostCaravanHooks()
        {
            Harmony harmony = new Harmony("turbomegamod.strategicoutposts");
            MethodInfo targetMethod = AccessTools.Method(typeof(Caravan), "GetGizmos");
            MethodInfo postfixMethod = AccessTools.Method(typeof(StrategicOutpostCaravanHooks), "GetGizmosPostfix");

            if (targetMethod != null && postfixMethod != null)
            {
                harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
            }
        }

        public static IEnumerable<Gizmo> GetGizmosPostfix(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            if (__result != null)
            {
                foreach (Gizmo g in __result)
                {
                    yield return g;
                }
            }

            if (__instance == null || __instance.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            foreach (Gizmo cmd in StrategicOutpostCaravanUtility.GetCaravanCommands(__instance))
            {
                yield return cmd;
            }
        }
    }
}
