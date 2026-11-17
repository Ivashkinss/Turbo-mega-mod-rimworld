// Compiled separately only for an isolated -tmm-localization-probe game run.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using Verse;

[StaticConstructorOnStartup]
public static class InGameLocalizationProbe
{
    static InGameLocalizationProbe()
    {
        if (!Environment.GetCommandLineArgs().Contains("-tmm-localization-probe")) return;
        LongEventHandler.ExecuteWhenFinished(Run);
    }

    private static string Escape(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + "\"";
    }

    private static string Normalize(string value) { return (value ?? "").Replace("\\n", "\n").Replace("\r\n", "\n").Trim(); }

    private static object ReadPath(object value, string[] path)
    {
        foreach (string part in path)
        {
            if (value == null) return null;
            int index;
            if (int.TryParse(part, out index)) { value = ((IList)value)[index]; continue; }
            var list = value as IList;
            if (list != null)
            {
                value = list.Cast<object>().FirstOrDefault(item =>
                {
                    var original = AccessTools.Field(item.GetType(), "untranslatedLabel");
                    var label = AccessTools.Field(item.GetType(), "label");
                    string text = original == null ? null : original.GetValue(item) as string;
                    if (string.IsNullOrEmpty(text) && label != null) text = label.GetValue(item) as string;
                    return text != null && text.ToLowerInvariant().Replace(' ', '_') == part;
                });
                continue;
            }
            FieldInfo field = AccessTools.Field(value.GetType(), part);
            value = field != null ? field.GetValue(value) : AccessTools.Property(value.GetType(), part).GetValue(value, null);
        }
        return value;
    }

    private static void Run()
    {
        var errors = new List<string>();
        int keyed = 0, fields = 0, rules = 0, patches = 0;
        string root = LoadedModManager.RunningModsListForReading.First(m => m.PackageId == "colonyproject.integrated").RootDir;
        foreach (string file in Directory.GetFiles(Path.Combine(root, "Languages/Russian/Keyed"), "*.xml"))
        {
            var document = new XmlDocument(); document.Load(file);
            foreach (XmlElement node in document.DocumentElement.ChildNodes.OfType<XmlElement>())
            {
                if (Normalize(Translator.Translate(node.Name).RawText) != Normalize(node.InnerText))
                    errors.Add("Keyed: " + node.Name);
                keyed++;
            }
        }
        foreach (string file in Directory.GetFiles(Path.Combine(root, "Languages/Russian/DefInjected"), "*.xml", SearchOption.AllDirectories))
        {
            string typeName = Path.GetFileName(Path.GetDirectoryName(file));
            Type type = AccessTools.TypeByName(typeName);
            if (type == null) { errors.Add("Missing def type: " + typeName); continue; }
            var document = new XmlDocument(); document.Load(file);
            foreach (XmlElement node in document.DocumentElement.ChildNodes.OfType<XmlElement>())
            {
                try
                {
                    string translatedPath = node.Name;
                    var normalizer = AccessTools.Method(AccessTools.TypeByName("Verse.TKeySystem"), "TryGetNormalizedPath");
                    if (normalizer != null)
                    {
                        object[] arguments = { translatedPath, null };
                        if ((bool)normalizer.Invoke(null, arguments)) translatedPath = (string)arguments[1];
                    }
                    string[] path = translatedPath.Split('.');
                    object definition = typeof(DefDatabase<>).MakeGenericType(type).GetMethod("GetNamedSilentFail").Invoke(null, new object[] { path[0] });
                    if (definition == null) { errors.Add("Missing definition: " + node.Name); continue; }
                    object value = ReadPath(definition, path.Skip(1).ToArray());
                    if (node.HasChildNodes && node.FirstChild is XmlElement)
                    {
                        var actual = ((IEnumerable)value).Cast<object>().Select(x => Normalize(x.ToString())).ToArray();
                        var expected = node.ChildNodes.OfType<XmlElement>().Select(x => Normalize(x.InnerText)).ToArray();
                        if (!actual.SequenceEqual(expected)) errors.Add("Rule list differs: " + node.Name);
                        rules += expected.Length;
                    }
                    else
                    {
                        if (Normalize(Convert.ToString(value)) != Normalize(node.InnerText)) errors.Add("Field differs: " + node.Name);
                        fields++;
                    }
                }
                catch (Exception exception) { errors.Add(node.Name + ": " + exception.GetBaseException().Message); }
            }
        }
        string[] targets = {
            "AM.AMSettings.Settings|<DrawAnimationList>g__DrawAnim|68_0",
            "AM.AMSettings.SimpleSettings|DrawWindow",
            "AM.AMSettings.SimpleSettings+MemberWrapper|ValueToString",
            "DynamicTradeInterface.UserInterface.Columns.ColumnQuantity|PostOpen",
            "DynamicTradeInterface.Mod.DynamicTradeInterfaceMod|SettingsCategory",
            "DynamicTradeInterface.UserInterface.Columns.ColumnExtraIconTypes.BookDrawable+<GetIcons>d__1|MoveNext",
            "SmartSpeed.SmartSpeed_Mod|SettingsCategory",
            "Bubbles.Mod|SettingsCategory",
            "Bubbles.Configuration.SettingsEditor|DrawSettings",
            "CaravanDontRest.AlmostThereMod|SettingsCategory",
            "SnapOut.SOMod|DoSettingsWindowContents"
        };
        foreach (string target in targets)
        {
            int split = target.IndexOf('|');
            Type type = AccessTools.TypeByName(target.Substring(0, split));
            MethodInfo method = type == null ? null : AccessTools.Method(type, target.Substring(split + 1));
            var info = method == null ? null : Harmony.GetPatchInfo(method);
            if (info == null || !info.Owners.Contains("turbomegamod.runtime.localization")) errors.Add("Missing display patch: " + target);
            else patches++;
        }
        var bubbleDialog = AccessTools.TypeByName("Bubbles.Configuration.SettingsEditor+Dialog");
        var dialogConstructor = bubbleDialog == null ? null : AccessTools.Constructor(bubbleDialog);
        var dialogPatch = dialogConstructor == null ? null : Harmony.GetPatchInfo(dialogConstructor);
        if (dialogPatch == null || !dialogPatch.Owners.Contains("turbomegamod.runtime.localization"))
            errors.Add("Missing Bubbles dialog title patch");
        else patches++;
        var tradeType = AccessTools.TypeByName("DynamicTradeInterface.Mod.DynamicTradeInterfaceMod");
        object tradeMod = LoadedModManager.ModHandles.FirstOrDefault(m => m.GetType() == tradeType);
        if (tradeMod == null || (string)AccessTools.Method(tradeType, "SettingsCategory").Invoke(tradeMod, null) != "Динамическое окно торговли")
            errors.Add("Trade settings category was not translated");
        var localization = AccessTools.TypeByName("TurboMegaMod.RuntimeLocalization");
        if ((string)AccessTools.Method(localization, "Translate").Invoke(null, new object[] { "TMM.Literal.f5ab5f2a27ad" }) != "<i>Нет описания</i>")
            errors.Add("Runtime translation lost rich text");
        Type modeType = AccessTools.TypeByName("AM.AMSettings.CorpseOffsetMode");
        foreach (string mode in new[] { "None", "InterpolateToCorrect", "KeepOffset" })
        {
            string label = (string)AccessTools.Method(localization, "EnumLabel").Invoke(null, new object[] { Enum.Parse(modeType, mode) });
            if (label != Translator.Translate("TMM.CorpseOffset." + mode).RawText) errors.Add("Enum label: " + mode);
        }
        var duelMethod = AccessTools.Method(AccessTools.TypeByName("AM.AutoDuel.AutoFriendlyDuelMapComp"), "UpdatePawnsThatCanDuel");
        var duelPatch = Harmony.GetPatchInfo(duelMethod);
        if (duelPatch == null || !duelPatch.Owners.Contains("turbomegamod.melee.waylith")) errors.Add("Missing main-thread snapshot patch");
        foreach (string component in new[] { "SmartSpeed", "SmarterConstruction", "InteractionBubbles" })
        {
            string assemblyName = component == "InteractionBubbles" ? "Bubbles" : component;
            Assembly[] matching = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == assemblyName).ToArray();
            if (matching.Length != 1) errors.Add("Expected exactly one utility assembly: " + component + "; found " + matching.Length);
            Assembly assembly = matching.FirstOrDefault();
            bool originalActive = component == "InteractionBubbles" && ModsConfig.IsActive("jaxe.bubbles");
            string expectedPath = originalActive
                ? Path.Combine(LoadedModManager.RunningModsListForReading.First(m => m.PackageId == "jaxe.bubbles").RootDir, "Assemblies/Bubbles.dll")
                : Path.Combine(root, "Components", component, "Assemblies", assemblyName + ".dll");
            if (assembly == null || !string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expectedPath), StringComparison.OrdinalIgnoreCase))
                errors.Add("Utility assembly loaded from an unexpected location: " + component);
            else
            {
                bool patched = Harmony.GetAllPatchedMethods().Any(method => {
                    var info = Harmony.GetPatchInfo(method);
                    return info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Any(patch => patch.PatchMethod.Module.Assembly == assembly);
                });
                if (!patched) errors.Add("Utility installed no game patches: " + component);
                if (component == "InteractionBubbles")
                {
                    foreach (string route in new[] {
                        "RimWorld.PlaySettings|DoPlaySettingsGlobalControls",
                        "RimWorld.MapInterface|MapInterfaceOnGUI_BeforeMainTabs",
                        "Verse.PlayLog|Add",
                        "Verse.Profile.MemoryUtility|ClearAllMapsAndWorld"
                    })
                    {
                        string[] parts = route.Split('|');
                        var method = AccessTools.Method(AccessTools.TypeByName(parts[0]), parts[1]);
                        var info = method == null ? null : Harmony.GetPatchInfo(method);
                        if (info == null || !info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers)
                            .Any(patch => patch.PatchMethod.Module.Assembly == assembly))
                            errors.Add("Missing Bubbles gameplay patch: " + route);
                    }
                }
            }
        }
        string report = "{\"keyed\":" + keyed + ",\"scalarFields\":" + fields + ",\"grammarLines\":" + rules
            + ",\"displayPatches\":" + patches + ",\"passed\":" + (errors.Count == 0 ? "true" : "false")
            + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "LocalizationProbe.json"), report);
        var languageErrors = new List<string>();
        languageErrors.AddRange(((IEnumerable)AccessTools.Field(typeof(LoadedLanguage), "loadErrors")
            .GetValue(LanguageDatabase.activeLanguage)).Cast<object>().Select(x => x.ToString()));
        foreach (object package in (IEnumerable)AccessTools.Field(typeof(LoadedLanguage), "defInjections").GetValue(LanguageDatabase.activeLanguage))
            languageErrors.AddRange(((IEnumerable)AccessTools.Field(package.GetType(), "loadErrors").GetValue(package))
                .Cast<object>().Select(x => x.ToString()));
        File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "LanguageErrors.txt"), languageErrors);
        Log.Message("[TMM probe] Checked " + keyed + " keys, " + fields + " fields, " + rules
            + " grammar lines and " + patches + " display patches; errors: " + errors.Count);
    }
}
