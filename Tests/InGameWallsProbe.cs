using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

public sealed class WallsProbe : MapComponent
{
    private bool reported, uiReported;
    private Def menu;
    public WallsProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-walls-probe")) return;
        reported = true; int checks = 0, patches = 0, bluntLost = 0, crushingLost = 0; var errors = new List<string>();
        try
        {
            var assembly = AccessTools.TypeByName("WallsAreSolid.HarmonyPatches").Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "WallsAreSolid") == 1, "Duplicate walls assembly"); checks++;
            bool original = ModsConfig.IsActive("victor.wallsaresolid"); string id = original ? "victor.wallsaresolid" : "colonyproject.integrated";
            var pack = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == id);
            string expected = Path.Combine(pack.RootDir, original ? "Assemblies/WallsAreSolid.dll" : "Components/WallsAreSolid/Assemblies/WallsAreSolid.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong walls DLL path"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 1, "Missing escape job patch"); checks++;
            Type mt = AccessTools.TypeByName("XmlExtensions.SettingsMenuDef");
            // XML Extensions keeps initialized menus in its own registry.
            var registry = (IDictionary)AccessTools.Field(AccessTools.TypeByName("XmlExtensions.XmlMod"), "settingsPerMod").GetValue(null);
            object registered = registry["Victor.WallsAreSolid"];
            var menus = (IDictionary)AccessTools.Field(registered.GetType(), "menus").GetValue(registered);
            // The mod has one menu in its namespaced registry.
            menu = menus.Values.Cast<Def>().Single();
            Check(menu != null && menu.GetType() == mt, "Settings menu not registered"); checks++;
            Check((string)AccessTools.Field(mt, "modId").GetValue(menu) == "Victor.WallsAreSolid", "Changed settings namespace"); checks++;
            Type manager = AccessTools.TypeByName("XmlExtensions.SettingsManager"); var get = AccessTools.Method(manager, "GetSetting"); var set = AccessTools.Method(manager, "SetSetting");
            string old = (string)get.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable" });
            try { set.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable", "0.45" }); Check((string)get.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable" }) == "0.45", "Settings round trip failed"); checks++; }
            finally { set.Invoke(null, new object[] { "Victor.WallsAreSolid", "SharpMeleePassable", old }); }
            var translate = AccessTools.Method(AccessTools.TypeByName("XmlExtensions.Helpers"), "TryTranslate");
            string key = (string)AccessTools.Field(mt, "tKey").GetValue(menu);
            Check((string)translate.Invoke(null, new object[] { menu.label, key }) == "Прочные стены", "Menu translation key failed"); checks++;
            var settings = ((IEnumerable)AccessTools.Field(mt, "settings").GetValue(menu)).Cast<object>().ToList(); int translated = 0;
            foreach (var setting in settings)
            {
                var fk = AccessTools.Field(setting.GetType(), "tKey"); if (fk == null) continue;
                string tk = (string)fk.GetValue(setting); if (string.IsNullOrEmpty(tk)) continue;
                var text = AccessTools.Field(setting.GetType(), setting.GetType().Name == "Text" ? "text" : "label");
                Check((string)translate.Invoke(null, new object[] { (string)text.GetValue(setting), tk }) == Translator.Translate(tk).RawText, "Setting translation failed " + tk); translated++;
            }
            Check(translated == 18, "Missing translated controls: " + translated); checks++;
            var blunt = DamageDefOf.Blunt; var crushing = DefDatabase<DamageDef>.GetNamed("BluntCrushing");
            float fromSetting = float.Parse((string)get.Invoke(null, new object[] { "Victor.WallsAreSolid", "BluntImpassable" }), CultureInfo.InvariantCulture);
            Check(Math.Abs(blunt.buildingDamageFactorImpassable - fromSetting) < 0.001f, "XML damage setting not applied"); checks++;
            Check(Math.Abs(crushing.buildingDamageFactorImpassable - 1.5f) < 0.001f, "Crushing wall factor changed"); checks++;
            var capacity = DefDatabase<ToolCapacityDef>.GetNamed("BluntCrushing"); Check(capacity.label == "разрушительный удар", "Crushing capacity untranslated"); checks++;
            var maneuver = DefDatabase<ManeuverDef>.GetNamed("SmashCrushing"); Check(maneuver.requiredCapacity == capacity && maneuver.verb.meleeDamageDef == crushing, "Crushing maneuver is incomplete"); checks++;
            var wallDef = ThingDefOf.Wall; var stuff = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
            Thing first = ThingMaker.MakeThing(wallDef, stuff), second = ThingMaker.MakeThing(wallDef, stuff);
            GenSpawn.Spawn(first, CellFinder.RandomClosewalkCellNear(map.Center, map, 15), map); GenSpawn.Spawn(second, CellFinder.RandomClosewalkCellNear(map.Center, map, 15), map);
            int hp = first.HitPoints; first.TakeDamage(new DamageInfo(blunt, 100, 999)); bluntLost = hp - first.HitPoints;
            hp = second.HitPoints; second.TakeDamage(new DamageInfo(crushing, 100, 999)); crushingLost = hp - second.HitPoints;
            Check(crushingLost > 0 && (blunt.buildingDamageFactorImpassable == 0 ? bluntLost == 0 : bluntLost > 0), "Wall damage was not applied"); checks++;
            // Other vanilla/material multipliers affect both identical walls.
            // Compare their ratio, which must preserve the mod's damage factors.
            if (bluntLost > 0) Check(Math.Abs((float)crushingLost / bluntLost - crushing.buildingDamageFactorImpassable / blunt.buildingDamageFactorImpassable) < 0.1f, "Actual relative crushing damage differs from factors");
            else Check(blunt.buildingDamageFactorImpassable == 0, "Unexpected zero blunt damage"); checks++;
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "WallsProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"bluntWallHitPointsLost\":" + bluntLost + ",\"crushingWallHitPointsLost\":" + crushingLost + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || menu == null || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-walls-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(25, 25, 1100, 750));
        try { AccessTools.Method(menu.GetType(), "PreOpen").Invoke(menu, null); AccessTools.Method(menu.GetType(), "DrawSettings").Invoke(menu, new object[] { new Rect(0, 0, 1050, 740) }); }
        catch (Exception e) { error = e.ToString(); } finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "WallsUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"xmlExtensionsMenuDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
