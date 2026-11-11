using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

public sealed class TradeProbe : MapComponent
{
    private bool reported, uiReported;
    private Pawn negotiator;
    private TradeShip trader;
    public TradeProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static Verse.Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "DynamicTradeInterface.Mod.DynamicTradeInterfaceMod"); }
    private static ModSettings Settings() { return (ModSettings)typeof(Verse.Mod).GetMethods().Single(m => m.Name == "GetSettings" && m.IsGenericMethodDefinition).MakeGenericMethod(AccessTools.TypeByName("DynamicTradeInterface.Mod.DynamicTradeInterfaceSettings")).Invoke(Mod(), null); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-trade-probe")) return;
        negotiator = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Social)); if (negotiator == null) return;
        reported = true; int checks = 0, patches = 0; var errors = new List<string>();
        try
        {
            var mod = Mod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "DynamicTradeInterface") == 1, "Duplicate trade assembly"); checks++;
            bool original = ModsConfig.IsActive("zeracronius.dynamictradeinterface"); string id = original ? "zeracronius.dynamictradeinterface" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong owner"); checks++;
            string expected = Path.Combine(mod.Content.RootDir, original ? "1.6/Assemblies/DynamicTradeInterface.dll" : "Components/DynamicTradeInterface/Assemblies/DynamicTradeInterface.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong DLL path"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 2, "Missing trade hooks"); checks++;
            Check(mod.SettingsCategory() == "Динамическое окно торговли", "Untranslated title"); checks++;
            var settings = Settings(); var field = AccessTools.Field(settings.GetType(), "ExcludeUnwillingItems"); object old = field.GetValue(settings);
            try
            {
                field.SetValue(settings, true); string sp = Path.Combine(GenFilePaths.SaveDataFolderPath, "TradeSettings.xml");
                Scribe.saver.InitSaving(sp, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); field.SetValue(settings, false);
                Scribe.loader.InitLoading(sp); settings.ExposeData(); Scribe.loader.FinalizeLoading(); Check((bool)field.GetValue(settings), "Settings Scribe failed"); checks++;
            }
            finally { field.SetValue(settings, old); }
            var columns = ((IEnumerable)AccessTools.Method(settings.GetType(), "GetVisibleTradeColumns").Invoke(settings, null)).Cast<object>().ToList();
            Check(columns.Count >= 3, "Missing trade columns"); checks++;
            foreach (var column in columns) { Check(((Def)column).modContentPack.PackageId == id, "Wrong column owner"); checks++; }
            var faction = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
            trader = new TradeShip(DefDatabase<TraderKindDef>.GetNamed("Orbital_BulkGoods"), faction); map.passingShipManager.AddShip(trader);
            if (!trader.Goods.Any()) trader.GenerateThings();
            Check(trader.Goods.Any(), "No test trader wares"); checks++;
            foreach (string icon in new[] { "Settings", "Unlocked", "Locked", "Filter_presets" }) { Check(ContentFinder<Texture2D>.Get("Icons/" + icon, false) != null, "Missing trade texture " + icon); checks++; }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "TradeProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || trader == null || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-trade-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; bool replaced = false, drawn = false; int rows = 0;
        var before = Find.WindowStack.Windows.ToList(); var settings = Settings(); var flag = AccessTools.Field(settings.GetType(), "OpenAsDefault"); object old = flag.GetValue(settings);
        GUI.BeginGroup(new Rect(10, 10, 1250, 770));
        try
        {
            Mod().DoSettingsWindowContents(new Rect(0, 0, 1200, 700)); flag.SetValue(settings, true);
            Find.WindowStack.Add(new Dialog_Trade(negotiator, trader, false));
            var window = Find.WindowStack.Windows.Single(w => w.GetType().FullName == "DynamicTradeInterface.UserInterface.Window_DynamicTrade"); replaced = true;
            Check(!Find.WindowStack.Windows.Any(w => w is Dialog_Trade), "Vanilla trade window was not replaced");
            var table = AccessTools.Property(window.GetType(), "TraderTable").GetValue(window, null);
            rows = ((IEnumerable)AccessTools.Property(table.GetType(), "RowItems").GetValue(table, null)).Cast<object>().Count(); Check(rows > 0, "Trader table is empty");
            window.DoWindowContents(new Rect(0, 0, 1200, 700)); drawn = true;
        }
        catch (Exception e) { error = e.ToString(); }
        finally
        {
            foreach (var window in Find.WindowStack.Windows.Where(w => !before.Contains(w)).ToList()) window.Close(false);
            flag.SetValue(settings, old); Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; GUI.EndGroup();
        }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "TradeUiProbe.json"), "{\"passed\":" + (error == null && replaced && drawn && rows > 0 ? "true" : "false") + ",\"vanillaDialogReplaced\":" + (replaced ? "true" : "false") + ",\"tradeWindowDrawInvokedInRepaint\":" + (drawn ? "true" : "false") + ",\"traderRows\":" + rows + ",\"transactionExecuted\":false,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
