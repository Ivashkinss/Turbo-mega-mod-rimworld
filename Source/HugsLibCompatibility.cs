using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    internal static class HugsLibCompatibility
    {
        private static Assembly library;
        internal static void Apply()
        {
            Type controller = AccessTools.TypeByName("HugsLib.HugsLibController");
            if (controller == null) return;
            library = controller.Assembly;
            var root = LoadedModManager.RunningModsListForReading.Find(m => m.PackageId == "colonyproject.integrated");
            if (root == null || !IsIntegratedLibrary(root)) return;
            var target = AccessTools.Method(controller, "CheckForIncludedHugsLibAssembly");
            new Harmony("turbomegamod.hugslib.owner").Patch(target,
                transpiler: new HarmonyMethod(typeof(HugsLibCompatibility), "AuditOwner"));
        }
        private static bool IsIntegratedLibrary(ModContentPack pack)
        {
            string expected = Path.Combine(pack.RootDir, "Components", "HugsLib", "Assemblies", "HugsLib.dll");
            return pack.PackageId == "colonyproject.integrated" && library != null
                && string.Equals(Path.GetFullPath(library.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase);
        }
        // The original audit recognizes its own DLL by a display name of
        // "HugsLib". Accept only our exact packaged DLL as that owner; continue
        // auditing every other pack using its real display name.
        private static string AuditOwnerName(ModContentPack pack)
        {
            return IsIntegratedLibrary(pack) ? "HugsLib" : pack.Name;
        }
        private static IEnumerable<CodeInstruction> AuditOwner(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            var name = AccessTools.PropertyGetter(typeof(ModContentPack), "Name");
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(name))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(HugsLibCompatibility), "AuditOwnerName");
                    replaced++;
                }
                yield return instruction;
            }
            // Both the comparison and its error message read Name.
            if (replaced != 2) throw new InvalidOperationException("HugsLib ownership audit changed: " + replaced);
        }
    }
}
