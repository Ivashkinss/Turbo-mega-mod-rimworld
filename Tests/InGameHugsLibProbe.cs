using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

public sealed class HugsIntegrationListener : HugsLib.ModBase
{
    public static bool DefsReceived, MapReceived;
    public static int TicksReceived;
    public override string ModIdentifier { get { return "TMMIntegrationProbe"; } }
    protected override bool HarmonyAutoPatch { get { return false; } }
    public override void DefsLoaded() { DefsReceived = true; }
    public override void MapLoaded(Map map) { MapReceived = true; }
    public override void Tick(int tick) { TicksReceived++; }
}

public sealed class HugsLibProbe : MapComponent
{
    private bool scheduled, nextUpdate, nextTick, delayed, reported, uiReported;
    private int startTick;
    private object pack;
    public HugsLibProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static object Get(object target, string name) { return AccessTools.Property(target.GetType(), name).GetValue(target, null); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-hugs-probe")) return;
        var controller = HugsLib.HugsLibController.Instance;
        if (!scheduled)
        {
            scheduled = true; startTick = Find.TickManager.TicksGame;
            var later = Get(controller, "DoLater");
            AccessTools.Method(later.GetType(), "DoNextUpdate").Invoke(later, new object[] { (Action)(() => nextUpdate = true) });
            AccessTools.Method(later.GetType(), "DoNextTick").Invoke(later, new object[] { (Action)(() => nextTick = true) });
            var scheduler = Get(controller, "TickDelayScheduler");
            AccessTools.Method(scheduler.GetType(), "ScheduleCallback").Invoke(scheduler, new object[] { (Action)(() => delayed = true), 5, null, false });
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            return;
        }
        if (Find.TickManager.TicksGame - startTick < 15) return;
        reported = true; int checks = 0, patches = 0; var errors = new List<string>();
        try
        {
            var assembly = typeof(HugsLib.ModBase).Assembly;
            bool original = ModsConfig.IsActive("unlimitedhugs.hugslib");
            var owner = (ModContentPack)AccessTools.Property(controller.GetType(), "OwnContentPack").GetValue(null, null);
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "HugsLib") == 1, "Duplicate HugsLib"); checks++;
            Check(owner.PackageId == (original ? "unlimitedhugs.hugslib" : "colonyproject.integrated"), "Wrong library content owner"); checks++;
            string expected = Path.Combine(owner.RootDir, original ? "v1.6/Assemblies/HugsLib.dll" : "Components/HugsLib/Assemblies/HugsLib.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong library DLL path"); checks++;
            Check(((Version)AccessTools.Property(controller.GetType(), "LibraryVersion").GetValue(null, null)).Major == 12, "Wrong HugsLib library version"); checks++;
            if (!original)
            {
                var compatibility = AccessTools.TypeByName("TurboMegaMod.HugsLibCompatibility");
                var normalize = AccessTools.Method(compatibility, "AuditOwnerName");
                Check((string)normalize.Invoke(null, new object[] { owner }) == "HugsLib", "Integrated audit owner not recognized"); checks++;
                var other = LoadedModManager.RunningModsListForReading.First(m => m.PackageId == "ludeon.rimworld");
                Check((string)normalize.Invoke(null, new object[] { other }) == other.Name, "Audit changed an unrelated pack name"); checks++;
            }
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 5, "Missing library patches"); checks++;
            Check(HugsIntegrationListener.DefsReceived && HugsIntegrationListener.MapReceived && HugsIntegrationListener.TicksReceived > 0, "ModBase callbacks were not dispatched"); checks++;
            Check(nextUpdate && nextTick && delayed, "Deferred callbacks were not dispatched"); checks++;
            var mods = ((IEnumerable)Get(controller, "InitializedMods")).Cast<object>().ToList();
            Check(mods.Any(m => m.GetType().Assembly.GetName().Name == "RunAndGun"), "Integrated RunAndGun not registered with HugsLib"); checks++;
            var manager = Get(controller, "Settings");
            pack = AccessTools.Method(manager.GetType(), "GetModSettings").Invoke(manager, new object[] { "TMMIntegrationProbe", "Проверка библиотеки" });
            var method = pack.GetType().GetMethods().Single(m => m.Name == "GetHandle" && m.IsGenericMethodDefinition && m.GetParameters().Length == 6).MakeGenericMethod(typeof(int));
            var handle = method.Invoke(pack, new object[] { "probeValue", "Число", "Проверка сохранения", 5, null, null });
            AccessTools.Property(handle.GetType(), "Value").SetValue(handle, 19, null);
            AccessTools.Method(manager.GetType(), "SaveChanges").Invoke(manager, null);
            var reloaded = Activator.CreateInstance(manager.GetType(), true);
            var restoredPack = AccessTools.Method(reloaded.GetType(), "GetModSettings").Invoke(reloaded, new object[] { "TMMIntegrationProbe", "Проверка библиотеки" });
            var restoredHandle = method.Invoke(restoredPack, new object[] { "probeValue", "Число", "Проверка сохранения", 5, null, null });
            Check((int)Get(restoredHandle, "Value") == 19, "HugsLib settings did not survive disk reload"); checks++;
            Check(DefDatabase<KeyBindingDef>.GetNamed("HLOpenModSettings").modContentPack == owner, "Wrong shortcut owner"); checks++;
            Check(DefDatabase<KeyBindingDef>.GetNamed("HLOpenModSettings").label == "Открыть настройки модов", "Shortcut translation missing"); checks++;
            foreach (string icon in new[] { "HLModIcon", "HLMenuIcon", "HLMenuIconPlus", "HLInfoIcon", "quickstartIcon" }) { Check(ContentFinder<Texture2D>.Get(icon, false) != null, "Missing library texture " + icon); checks++; }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HugsProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"listenerTicks\":" + HugsIntegrationListener.TicksReceived + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || pack == null || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-hugs-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(25, 25, 1150, 750));
        try
        {
            var window = (Window)Activator.CreateInstance(AccessTools.TypeByName("HugsLib.Settings.Dialog_ModSettings"), new object[] { pack });
            window.DoWindowContents(new Rect(0, 0, 1100, 700));
        }
        catch (Exception e) { error = e.ToString(); }
        finally { GUI.EndGroup(); Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HugsUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
