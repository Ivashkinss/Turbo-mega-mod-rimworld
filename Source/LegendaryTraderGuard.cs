using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Random trader stock must not pick a unique item whose original is already reserved. The duplicate used to be created and
    /// destroyed at once in PostPostMake, after the game had put it into the stock ("generated destroyed thing"), and it took the
    /// place of a real item. The item is excluded from the choice itself; a trader's readiness to buy it (HandlesThingDef) is untouched,
    /// and a free unique item can still be stocked once.
    [StaticConstructorOnStartup]
    public static class LegendaryTraderGuard
    {
        public const string HarmonyId = "colonyproject.integrated.legendarytraders";

        static LegendaryTraderGuard()
        {
            Harmony harmony = new Harmony(HarmonyId);
            HarmonyMethod weight = new HarmonyMethod(typeof(LegendaryTraderGuard), "Postfix_SelectionWeight");
            foreach (Type type in typeof(StockGenerator).AllSubclasses())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    ParameterInfo[] args = method.GetParameters();
                    if (method.Name != "SelectionWeight" || method.IsAbstract || method.ContainsGenericParameters || method.ReturnType != typeof(float) || args.Length == 0 || args[0].ParameterType != typeof(ThingDef)) continue;
                    harmony.Patch(method, postfix: weight);
                }
            }
            // Generators that choose by tag, category or list end here; every caller of TryMakeForStock skips null.
            // StockGenerator_MiscItems calls it directly, but its choice above can no longer give a reserved item.
            MethodInfo single = AccessTools.Method(typeof(StockGeneratorUtility), "TryMakeForStockSingle");
            if (single != null) harmony.Patch(single, prefix: new HarmonyMethod(typeof(LegendaryTraderGuard), "Prefix_TryMakeForStockSingle"));
            else Log.Warning("[TMM] Legendary trader guard: TryMakeForStockSingle not found.");
        }

        public static void Postfix_SelectionWeight(ThingDef __0, ref float __result)
        {
            if (__result > 0f && Reserved(__0)) __result = 0f;
        }

        public static bool Prefix_TryMakeForStockSingle(ThingDef __0, ref Thing __result)
        {
            if (!Reserved(__0)) return true;
            __result = null;
            return false;
        }

        public static bool Reserved(ThingDef def)
        {
            if (def == null || def.comps == null || def.GetCompProperties<CompProperties_LegendaryItem>() == null) return false;
            LegendaryRegistry registry = LegendaryRegistry.Instance;
            return registry != null && registry.ReservedForStock(def);
        }
    }
}
