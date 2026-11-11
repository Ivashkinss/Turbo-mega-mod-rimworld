using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

public sealed class AdaptiveStorageProbe : MapComponent
{
    private bool reported, uiReported;
    public AdaptiveStorageProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "AdaptiveStorage.AdaptiveStorageFrameworkMod"); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-storage-probe")) return;
        reported = true; int checks = 0, storages = 0, patches = 0; var errors = new List<string>();
        try
        {
            Mod mod = Mod(); var assembly = mod.GetType().Assembly;
            bool original = ModsConfig.IsActive("adaptive.storage.framework");
            string id = original ? "adaptive.storage.framework" : "colonyproject.integrated";
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "AdaptiveStorageFramework") == 1, "Duplicate storage framework"); checks++;
            Check(mod.Content.PackageId == id, "Wrong storage owner"); checks++;
            string expected = Path.Combine(mod.Content.RootDir, original ? "1.6/Assemblies/AdaptiveStorageFramework.dll" : "Components/AdaptiveStorage/Assemblies/AdaptiveStorageFramework.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong storage DLL path"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches > 5, "Missing storage hooks"); checks++;
            Type storageType = AccessTools.TypeByName("AdaptiveStorage.ThingClass");
            var defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => !d.IsBlueprint && !d.IsFrame && d.thingClass != null && storageType.IsAssignableFrom(d.thingClass) && d.modContentPack.PackageId == "colonyproject.integrated").ToList();
            Check(defs.Count == 34, "Expected 27 storages and 7 fridges, got " + defs.Count); checks++;
            foreach (var def in defs)
            {
                Building building = null; var items = new List<Thing>();
                try
                {
                    building = (Building)ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
                    GenSpawn.Spawn(building, new IntVec3(20 + (storages % 8) * 12, 0, 20 + (storages / 8) * 12), map);
                    Check((int)AccessTools.Property(storageType, "TotalSlots").GetValue(building, null) == def.size.x * def.size.z * def.building.maxItemsInCell, "Wrong total capacity: " + def.defName); checks++;
                    Check((int)AccessTools.Property(storageType, "CurrentSlotLimit").GetValue(building, null) > 0, "Empty slot limit: " + def.defName); checks++;
                    if (def.defName == "sbz_Crate")
                    {
                        foreach (var itemDef in new[] { ThingDefOf.Steel, ThingDefOf.WoodLog })
                        {
                            Thing item = ThingMaker.MakeThing(itemDef); item.stackCount = 3; items.Add(item); GenSpawn.Spawn(item, building.Position, map);
                        }
                        var contents = ((IEnumerable)AccessTools.Property(storageType, "StoredThings").GetValue(building, null)).Cast<object>().ToList();
                        Check(items.All(item => contents.Contains(item)), "Spawned items missing from adaptive storage index"); checks++;
                        items[0].DeSpawn();
                        contents = ((IEnumerable)AccessTools.Property(storageType, "StoredThings").GetValue(building, null)).Cast<object>().ToList();
                        Check(!contents.Contains(items[0]) && contents.Contains(items[1]), "Despawn did not update storage index"); checks++;
                    }
                    storages++;
                }
                finally { foreach (var item in items) if (!item.Destroyed) item.Destroy(); if (building != null && !building.Destroyed) building.Destroy(); }
            }
            var settings = (ModSettings)AccessTools.Property(mod.GetType(), "Settings").GetValue(null, null);
            var field = AccessTools.Field(settings.GetType(), "_hideLabelsUntilMouseOver"); bool old = (bool)field.GetValue(null);
            try
            {
                field.SetValue(null, !old); string file = Path.Combine(GenFilePaths.SaveDataFolderPath, "StorageSettings.xml");
                Scribe.saver.InitSaving(file, "StorageSettings"); settings.ExposeData(); Scribe.saver.FinalizeSaving();
                field.SetValue(null, old); Scribe.loader.InitLoading(file); settings.ExposeData(); Scribe.loader.FinalizeLoading();
                Check((bool)field.GetValue(null) == !old, "Storage settings Scribe failed"); checks++;
            }
            finally { field.SetValue(null, old); }
            Check(ContentFinder<Texture2D>.Get("AdaptiveStorageFramework/ModIcon", false) != null, "Missing framework texture"); checks++;
            Check(Translator.Translate("ASF_Empty").RawText == "пусто", "Storage Russian translation missing"); checks++;
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "StorageProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"storagesSpawned\":" + storages + ",\"patchedMethods\":" + patches + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-storage-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(25, 25, 1150, 750));
        try { Mod().DoSettingsWindowContents(new Rect(0, 0, 1100, 700)); }
        catch (Exception e) { error = e.ToString(); }
        finally { GUI.EndGroup(); Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "StorageUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
