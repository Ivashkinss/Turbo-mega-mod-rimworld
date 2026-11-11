using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    internal static class VefCompatibility
    {
        private static Assembly embedded;
        internal static void Apply()
        {
            if (ModsConfig.IsActive("oskarpotocki.vanillafactionsexpanded.core")) return;
            var pack = LoadedModManager.RunningModsListForReading.Find(m => m.PackageId == "colonyproject.integrated");
            if (pack == null) return;
            embedded = pack.assemblies.loadedAssemblies.Find(a => a.GetName().Name == "VEF");
            if (embedded == null || !Path.GetFullPath(embedded.Location).StartsWith(Path.GetFullPath(Path.Combine(pack.RootDir, "Components", "VanillaExpandedFramework")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;
            var harmony = new Harmony("turbomegamod.vef.compatibility");
            var sidearms = AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Compat.VFECore");
            if (sidearms != null) harmony.Patch(sidearms.TypeInitializer, transpiler: new HarmonyMethod(typeof(VefCompatibility), "SidearmsOwner"));
            var bills = AccessTools.TypeByName("NiceBillTab.ModIntegration");
            if (bills != null) harmony.Patch(AccessTools.Method(bills, "TryGetExternalAssembly"), prefix: new HarmonyMethod(typeof(VefCompatibility), "FindAssembly"));
        }
        private static bool FindAssembly(string __0, string __1, ref Assembly __result)
        {
            if (__0 != "oskarpotocki.vanillafactionsexpanded.core" || __1 != "VEF" || embedded == null) return true;
            __result = embedded;
            return false;
        }
        private static IEnumerable<CodeInstruction> SidearmsOwner(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && string.Equals(instruction.operand as string, "OskarPotocki.VanillaFactionsExpanded.Core", StringComparison.OrdinalIgnoreCase))
                {
                    instruction.operand = "colonyproject.integrated";
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced != 1) throw new InvalidOperationException("Unexpected Simple Sidearms VEF owner check: " + replaced);
        }
    }
}
