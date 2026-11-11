using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    // Only reviewed display strings in selected components are localized.
    public sealed class TurboMegaMod : Mod
    {
        public TurboMegaMod(ModContentPack content) : base(content)
        {
            RuntimeLocalization.Apply();
            MeleeCompatibility.Apply();
        }
    }

    internal static class RuntimeLocalization
    {
        private static readonly Dictionary<ushort, OpCode> Codes = MakeCodes();

        private static Dictionary<ushort, OpCode> MakeCodes()
        {
            var result = new Dictionary<ushort, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                OpCode code = (OpCode)field.GetValue(null);
                result[unchecked((ushort)code.Value)] = code;
            }
            return result;
        }

        private static bool IsDisplayType(Type type)
        {
            string name = type.FullName ?? "";
            return name.StartsWith("AM.AMSettings.", StringComparison.Ordinal)
                || name.StartsWith("DynamicTradeInterface.UserInterface.Columns.ColumnQuantity", StringComparison.Ordinal)
                || name.StartsWith("DynamicTradeInterface.UserInterface.Columns.ColumnExtraIconTypes.BookDrawable", StringComparison.Ordinal)
                || name == "DynamicTradeInterface.Mod.DynamicTradeInterfaceMod"
                || name == "SmartSpeed.SmartSpeed_Mod"
                || name == "Bubbles.Mod"
                || name == "Bubbles.Configuration.SettingsEditor"
                || name == "Bubbles.Configuration.SettingsEditor+Dialog"
                || name == "CaravanDontRest.AlmostThereMod"
                || name == "SnapOut.SOMod"
                || name == "PickUpAndHaul.Modbase"
                || name == "NiceBillTab.NiceBillTabMod";
        }

        private static bool ContainsLiteral(MethodBase method)
        {
            MethodBody body = method.GetMethodBody();
            if (body == null) return false;
            byte[] bytes = body.GetILAsByteArray();
            for (int index = 0; index < bytes.Length;)
            {
                ushort number = bytes[index++];
                if (number == 0xfe) number = (ushort)(0xfe00 | bytes[index++]);
                OpCode code = Codes[number];
                if (code == OpCodes.Ldstr &&
                    RuntimeTranslationKeys.Literals.ContainsKey(method.Module.ResolveString(BitConverter.ToInt32(bytes, index))))
                    return true;
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineVar: index++; break;
                    case OperandType.InlineVar: index += 2; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: index += 8; break;
                    case OperandType.InlineSwitch: index += 4 + BitConverter.ToInt32(bytes, index) * 4; break;
                    default: index += 4; break;
                }
            }
            return false;
        }

        private static bool IsEnumDisplay(MethodBase method)
        {
            string type = method.DeclaringType.FullName;
            // The real settings UI uses this shared formatter for both the
            // selected value and menu entries, rather than ToString in DrawEnum.
            return (type == "AM.AMSettings.SimpleSettings+MemberWrapper" && method.Name == "ValueToString")
                || (type == "AM.AMSettings.SimpleSettings" && method.Name == "DrawWindow");
        }

        internal static void Apply()
        {
            var harmony = new Harmony("turbomegamod.runtime.localization");
            var patch = new HarmonyMethod(typeof(RuntimeLocalization), "Transpile");
            int patched = 0;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name != "zAnimationMod" && name != "DynamicTradeInterface" && name != "SmartSpeed" && name != "Bubbles" && name != "CaravanDontRest" && name != "SnapOut" && name != "PickUpAndHaul" && name != "NiceBillTab") continue;
                foreach (Type type in assembly.GetTypes())
                {
                    if (!IsDisplayType(type)) continue;
                    var methods = new List<MethodBase>(type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                    methods.AddRange(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
                    foreach (MethodBase method in methods)
                    {
                        if (method.IsAbstract || method.ContainsGenericParameters || method.GetMethodBody() == null) continue;
                        bool enumDisplay = IsEnumDisplay(method);
                        if (!enumDisplay && !ContainsLiteral(method)) continue;
                        harmony.Patch(method, transpiler: patch);
                        patched++;
                    }
                }
            }
            Log.Message("[Turbo mega mod] Localized display methods: " + patched);
        }

        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            bool enumDisplay = IsEnumDisplay(__originalMethod);
            MethodInfo translate = AccessTools.Method(typeof(RuntimeLocalization), "Translate");
            MethodInfo enumLabel = AccessTools.Method(typeof(RuntimeLocalization), "EnumLabel");
            foreach (CodeInstruction instruction in instructions)
            {
                string key;
                if (instruction.opcode == OpCodes.Ldstr &&
                    RuntimeTranslationKeys.Literals.TryGetValue((string)instruction.operand, out key))
                {
                    // Keep the original instruction's branch labels and exception blocks.
                    instruction.operand = key;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, translate);
                    continue;
                }
                MethodInfo called = instruction.operand as MethodInfo;
                if (enumDisplay && instruction.opcode == OpCodes.Callvirt && called != null
                    && called.DeclaringType == typeof(object) && called.Name == "ToString" && called.GetParameters().Length == 0)
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = enumLabel;
                }
                yield return instruction;
            }
        }

        private static string Translate(string key) { return Translator.Translate(key).RawText; }

        private static string EnumLabel(object value)
        {
            if (value == null) return "";
            string text = value.ToString();
            string key;
            return value.GetType().FullName == "AM.AMSettings.CorpseOffsetMode"
                && RuntimeTranslationKeys.CorpseOffset.TryGetValue(text, out key) ? Translate(key) : text;
        }
    }
}
