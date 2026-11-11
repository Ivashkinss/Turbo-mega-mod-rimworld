using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    internal static class CharacterEditorCompatibility
    {
        private static string root;
        internal static void Apply()
        {
            var pack = LoadedModManager.RunningModsListForReading.Find(m => m.PackageId == "colonyproject.integrated");
            var mod = AccessTools.TypeByName("CharacterEditor.CharacterEditorMod");
            if (pack == null || mod == null) return;
            string component = Path.Combine(pack.RootDir, "Components", "CharacterEditor");
            if (!string.Equals(Path.GetFullPath(mod.Assembly.Location), Path.GetFullPath(Path.Combine(component, "Assemblies", "CharacterEditor.dll")), StringComparison.OrdinalIgnoreCase)) return;
            root = component;
            var harmony = new Harmony("turbomegamod.charactereditor.resources");
            var data = AccessTools.TypeByName("CharacterEditor.CEditor+ModData");
            harmony.Patch(AccessTools.GetDeclaredConstructors(data).Single(), transpiler: new HarmonyMethod(typeof(CharacterEditorCompatibility), "RedirectRoot"));
            harmony.Patch(AccessTools.Method(mod, "SettingsCategory"), postfix: new HarmonyMethod(typeof(CharacterEditorCompatibility), "SettingsTitle"));
        }
        private static string ResourceRoot(ModContentPack pack) { return pack.PackageId == "colonyproject.integrated" ? root : pack.RootDir; }
        private static void SettingsTitle(ref string __result) { __result = Translator.Translate("TMM.CharacterEditorSettings").RawText; }
        private static IEnumerable<CodeInstruction> RedirectRoot(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0; var getter = AccessTools.PropertyGetter(typeof(ModContentPack), "RootDir");
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(getter)) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(CharacterEditorCompatibility), "ResourceRoot"); replaced++; }
                yield return instruction;
            }
            if (replaced != 1) throw new InvalidOperationException("Character Editor resource loader changed: " + replaced);
        }
    }
}
