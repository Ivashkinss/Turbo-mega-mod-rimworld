using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    internal static class MeleeResourceCompatibility
    {
        private static string root;
        internal static void Apply()
        {
            var pack = LoadedModManager.RunningModsListForReading.Find(m => m.PackageId == "colonyproject.integrated");
            var core = AccessTools.TypeByName("AM.Core");
            if (pack == null || core == null) return;
            string component = Path.Combine(pack.RootDir, "Components", "MeleeAnimation");
            string expected = Path.Combine(component, "Assemblies", "zAnimationMod.dll");
            if (!string.Equals(Path.GetFullPath(core.Assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase)) return;
            root = component;
            var harmony = new Harmony("turbomegamod.melee.resources");
            foreach (string target in new[] {
                "AM.Content|LoadBundle", "AM.Data.AnimDataSourceManager|ScanForDataFiles",
                "AM.Tweaks.TweakDataManager|GetFileForTweak", "AM.Tweaks.TweakDataManager|MakeOverrideTweakDataFileMap",
                "AM.Tweaks.TweakDataManager|GetTweakDataFileCount" })
            {
                string[] parts = target.Split('|');
                harmony.Patch(AccessTools.Method(AccessTools.TypeByName(parts[0]), parts[1]),
                    transpiler: new HarmonyMethod(typeof(MeleeResourceCompatibility), "RedirectRoots"));
            }
            // Only this renderer reads the package-id-derived flag. Preserve
            // its two original branches while recognizing our embedded DLL.
            var hands = AccessTools.TypeByName("ShowMeYourHands.HandDrawer");
            if (hands != null) harmony.Patch(hands.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single(m => m.Name == "DrawHandsOnWeapon" && m.GetParameters().Length == 6),
                transpiler: new HarmonyMethod(typeof(MeleeResourceCompatibility), "RecognizeAnimation"));
            harmony.Patch(AccessTools.Method(core, "SettingsCategory"),
                postfix: new HarmonyMethod(typeof(MeleeResourceCompatibility), "SettingsTitle"));
        }
        private static string ResourceRoot(ModContentPack pack)
        {
            return root != null && pack.PackageId == "colonyproject.integrated" ? root : pack.RootDir;
        }
        private static void SettingsTitle(ref string __result) { __result = Translator.Translate("TMM.MeleeSettings").RawText; }
        private static IEnumerable<CodeInstruction> RedirectRoots(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            var getter = AccessTools.PropertyGetter(typeof(ModContentPack), "RootDir");
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(getter))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(MeleeResourceCompatibility), "ResourceRoot");
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced != 1) throw new InvalidOperationException("Melee resource loader changed: " + replaced);
        }
        private static IEnumerable<CodeInstruction> RecognizeAnimation(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            var flag = AccessTools.Field(AccessTools.TypeByName("ShowMeYourHands.ShowMeYourHandsMain"), "MeleeAnimationsLoaded");
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldsfld && Equals(instruction.operand, flag))
                {
                    instruction.opcode = OpCodes.Ldc_I4_1;
                    instruction.operand = null;
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced != 2) throw new InvalidOperationException("Hands animation detection changed: " + replaced);
        }
    }
}
