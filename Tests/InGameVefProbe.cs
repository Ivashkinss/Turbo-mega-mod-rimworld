using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

public sealed class VefProbe : MapComponent
{
    private bool reported, uiReported;
    private int checks, patches, cachePawns;
    private readonly List<string> errors = new List<string>();
    public VefProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "VEF.VFEGlobal"); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-vef-probe")) return;
        reported = true;
        try
        {
            var mod = Mod(); bool original = ModsConfig.IsActive("oskarpotocki.vanillafactionsexpanded.core");
            Check(mod.Content.PackageId == (original ? "oskarpotocki.vanillafactionsexpanded.core" : "colonyproject.integrated"), "Wrong VEF owner");
            // The 1.7 engine consumes PrepatcherAPI metadata before runtime;
            // that API shim is shipped and hash-checked, not a runtime module.
            foreach (string name in new[] { "0ModSettingsFramework", "KCSG", "MVCF", "Outposts", "PipeSystem", "VEF" })
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == name).ToArray();
                Check(assemblies.Length == 1, "Missing/duplicate VEF module " + name);
                Check(mod.Content.assemblies.loadedAssemblies.Contains(assemblies[0]), "Wrong module owner " + name);
                if (!original) Check(assemblies[0].Location.Replace('\\', '/').Contains("/Components/VanillaExpandedFramework/1.7/Assemblies/"), "Wrong VEF version folder " + name);
            }
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "0Harmony") == 1, "Multiple Harmony copies");
            foreach (string name in new[] { "VEF.VEF_Mod", "ModSettingsFramework.ModSettingsFrameworkMod", "MVCF.MVCF", "Outposts.OutpostsMod" })
                Check(LoadedModManager.ModHandles.Count(m => m.GetType().FullName == name) == 1, "Missing/duplicate native Mod " + name);
            var cache = AccessTools.TypeByName("VEF.AestheticScaling.CachedPawnDataExtensions");
            var initializer = AccessTools.Method(cache, "GetDefaultCache");
            Check(original ? initializer == null : initializer != null && initializer.IsStatic && initializer.GetParameters().Length == 1, "Unexpected cache initializer");
            var fallback = AccessTools.Method(AccessTools.TypeByName("VEF.AestheticScaling.PawnDataCache"), "GetCacheUltraSpeed");
            var accessor = AccessTools.Method(cache, "GetCachePrePatched");
            if (!original)
            {
                var sentinel = AccessTools.Field(AccessTools.TypeByName("VEF.AestheticScaling.CachedPawnData"), "defaultCache").GetValue(null);
                var uninitialized = new Pawn();
                Check(ReferenceEquals(initializer.Invoke(null, new object[] { uninitialized }), sentinel), "Initializer must be safe before Pawn setup");
                Check(ReferenceEquals(accessor.Invoke(null, new object[] { uninitialized }), sentinel), "New pawn did not receive neutral cache");
            }
            var pawns = map.mapPawns.AllPawnsSpawned.Where(p => p.RaceProps.Humanlike).Take(3).ToArray();
            Check(pawns.Length >= 2, "Not enough real pawns"); object previous = null;
            foreach (var pawn in pawns)
            {
                var expected = fallback.Invoke(null, new object[] { pawn, false });
                var actual = accessor.Invoke(null, new object[] { pawn });
                Check(actual != null && ReferenceEquals(actual, expected), "Injected cache differs from fallback");
                Check(ReferenceEquals(actual, accessor.Invoke(null, new object[] { pawn })), "Cache not stable");
                Check(!ReferenceEquals(previous, actual), "Distinct pawns share cache");
                previous = actual; cachePawns++;
            }
            var settingsField = AccessTools.Field(mod.GetType(), "settings");
            var settings = (ModSettings)settingsField.GetValue(null);
            var flag = AccessTools.Field(settings.GetType(), "enableVerboseLogging"); bool old = (bool)flag.GetValue(settings);
            try
            {
                flag.SetValue(settings, !old); string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "VefSettings.xml");
                Scribe.saver.InitSaving(path, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); flag.SetValue(settings, old);
                Scribe.loader.InitLoading(path); settings.ExposeData(); Scribe.loader.FinalizeLoading();
                Check((bool)flag.GetValue(settings) == !old, "VEF settings roundtrip failed");
            }
            finally { flag.SetValue(settings, old); }
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => new[] { "VEF", "MVCF", "Outposts", "PipeSystem", "KCSG" }.Contains(x.PatchMethod.Module.Assembly.GetName().Name)); });
            Check(patches > 30, "VEF hooks missing");
            var sidearms = AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Compat.VFECore");
            Check((bool)AccessTools.Field(sidearms, "active").GetValue(null), "Sidearms did not recognize VEF");
            Check(AccessTools.Field(sidearms, "offHandShield").GetValue(null) is Delegate, "Sidearms shield delegate missing");
            var usable = (Delegate)AccessTools.Field(sidearms, "usableWithShields").GetValue(null);
            var pistol = DefDatabase<ThingDef>.GetNamed("Gun_Autopistol");
            var shieldApi = AccessTools.Method(AccessTools.TypeByName("VEF.Apparels.ShieldUtility"), "UsableWithShields");
            var expectedShield = shieldApi.Invoke(null, new object[] { pistol });
            Check(usable != null && Equals(usable.DynamicInvoke(pistol), expectedShield), "Sidearms shield integration differs");
            var bills = AccessTools.TypeByName("NiceBillTab.ModIntegration");
            Check((bool)AccessTools.Field(bills, "VEFActive").GetValue(null), "Nice Bill Tab did not recognize VEF");
            var billVef = AccessTools.TypeByName("NiceBillTab.VanillaExpandedFrameworkIntegration");
            Check(ReferenceEquals(AccessTools.Field(billVef, "VEFAss").GetValue(null), mod.GetType().Assembly), "Nice Bill Tab VEF assembly missing");
            Check(Equals(AccessTools.Method(billVef, "UsableWithShields").Invoke(null, new object[] { pistol }), expectedShield), "Nice Bill Tab shield integration differs");
            Check(DefDatabase<RimWorld.JoyGiverDef>.GetNamedSilentFail("TakeDrugPatient") != null, "Common Sense joy giver overwritten");
            Check(DefDatabase<JobDef>.GetNamedSilentFail("EquipSecondary") != null, "Sidearms jobs overwritten");
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VefProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"realPawnCaches\":" + cachePawns + ",\"patchedMethods\":" + patches + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-vef-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(25, 25, 1150, 750));
        try { Mod().DoSettingsWindowContents(new Rect(0, 0, 1100, 700)); }
        catch (Exception e) { error = e.ToString(); }
        finally { GUI.EndGroup(); Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VefUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
