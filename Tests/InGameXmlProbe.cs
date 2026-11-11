using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

public sealed class XmlProbe : MapComponent
{
    private bool reported, uiReported;
    public XmlProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static Verse.Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "XmlExtensions.XmlMod"); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-xml-probe")) return;
        reported = true; int checks = 0, patches = 0; var errors = new List<string>();
        try
        {
            var mod = Mod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "XmlExtensions") == 1, "Duplicate XML library"); checks++;
            bool original = ModsConfig.IsActive("imranfish.xmlextensions"); string id = original ? "imranfish.xmlextensions" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong XML owner"); checks++;
            string expected = Path.Combine(mod.Content.RootDir, original ? "1.6/Assemblies/XmlExtensions.dll" : "Components/XmlExtensions/Assemblies/XmlExtensions.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong XML DLL path"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 3, "Missing XML hooks"); checks++;
            Check(mod.SettingsCategory() == "XML Extensions — настройки модов", "Untranslated settings category"); checks++;
            var button = DefDatabase<MainButtonDef>.GetNamed("XmlExtensions_MainButton_ModSettings");
            Check(button.modContentPack.PackageId == id && button.label == "Настройки модов", "Wrong/untranslated main button"); checks++;
            var settings = (ModSettings)AccessTools.Field(mod.GetType(), "allSettings").GetValue(null);
            Type manager = AccessTools.TypeByName("XmlExtensions.SettingsManager"); var get = AccessTools.Method(manager, "GetSetting"); var set = AccessTools.Method(manager, "SetSetting");
            string old = (string)get.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable" });
            try
            {
                set.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable", "0.45" });
                string file = Path.Combine(GenFilePaths.SaveDataFolderPath, "XmlSettings.xml"); Scribe.saver.InitSaving(file, "XmlSettings"); settings.ExposeData(); Scribe.saver.FinalizeSaving();
                set.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable", "0.6" });
                Scribe.loader.InitLoading(file); settings.ExposeData(); Scribe.loader.FinalizeLoading();
                Check((string)get.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable" }) == "0.45", "XML settings Scribe failed"); checks++;
            }
            finally { set.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable", old }); }
            var registry = (IDictionary)AccessTools.Field(mod.GetType(), "settingsPerMod").GetValue(null);
            Check(registry.Contains("Victor.WallsAreSolid"), "Embedded walls settings lost their namespace"); checks++;
            XmlDocument document = new XmlDocument(); document.LoadXml("<Defs><Probe><value>1</value></Probe></Defs>");
            XmlDocument operationXml = new XmlDocument(); operationXml.LoadXml("<Operation Class=\"XmlExtensions.PatchOperationSafeAdd\"><xpath>Defs/Probe</xpath><value><added>2</added></value></Operation>");
            var operation = DirectXmlToObject.ObjectFromXml<PatchOperation>(operationXml.DocumentElement, false);
            Check(operation.Apply(document) && document.SelectSingleNode("Defs/Probe/added").InnerText == "2", "Extended XML operation failed"); checks++;
            Check(operation.Apply(document) && document.SelectNodes("Defs/Probe/added").Count == 1, "SafeAdd duplicated an existing node"); checks++;
            foreach (string icon in new[] { "UI/Icons/XMLEIcon", "UI/Buttons/MainButtons/MainButton_ModSettings" }) { Check(ContentFinder<Texture2D>.Get(icon, false) != null, "Missing XML icon " + icon); checks++; }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "XmlProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-xml-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; var before = Find.WindowStack.Windows.ToList(); GUI.BeginGroup(new Rect(25, 25, 1150, 750));
        try
        {
            Mod().DoSettingsWindowContents(new Rect(0, 0, 1100, 700));
            var window = Find.WindowStack.Windows.Single(w => !before.Contains(w) && w.GetType().FullName == "XmlExtensions.XmlExtensionsMenuModSettings");
            window.DoWindowContents(new Rect(0, 0, 1100, 700));
        }
        catch (Exception e) { error = e.ToString(); }
        finally { foreach (var window in Find.WindowStack.Windows.Where(w => !before.Contains(w)).ToList()) window.Close(false); Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "XmlUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"xmlSettingsMenuDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
