// Test-only: real work giver, job driver action, defs and Scribe persistence.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

public sealed class SnapOutProbe : MapComponent
{
    private bool reported, uiReported;
    public SnapOutProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static Verse.Mod GetMod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "SnapOut.SOMod"); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-snapout-probe")) return;
        var pawns = map.mapPawns.FreeColonistsSpawned.Where(p => !p.Dead && !p.Downed && p.Awake()).Take(2).ToArray();
        if (pawns.Length != 2) return;
        reported = true;
        var errors = new List<string>(); int checks = 0;
        var originals = new Dictionary<FieldInfo, object>(); ModSettings settings = null;
        try
        {
            var mod = GetMod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "SnapOut") == 1, "Duplicate Snap Out DLL"); checks++;
            string id = ModsConfig.IsActive("weilbyte.snapout") ? "weilbyte.snapout" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong content pack"); checks++;
            string root = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == id).RootDir;
            string expected = Path.Combine(root, id == "weilbyte.snapout" ? "1.6/Assemblies/SnapOut.dll" : "Components/SnapOut/Assemblies/SnapOut.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong DLL origin"); checks++;
            Check(mod.SettingsCategory() == "Успокоение при срыве", "Settings title not translated"); checks++;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "0MultiplayerAPI" || a.GetName().Name == "MultiplayerAPI") == 1, "Expected one shared Multiplayer API"); checks++;
            Type st = AccessTools.TypeByName("SnapOut.SOSettings");
            settings = (ModSettings)typeof(Verse.Mod).GetMethods().Single(m => m.Name == "GetSettings" && m.IsGenericMethodDefinition).MakeGenericMethod(st).Invoke(mod, null);
            foreach (var f in st.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(f => !f.IsInitOnly && (f.FieldType == typeof(bool) || f.FieldType == typeof(int) || f.FieldType == typeof(float))))
                originals[f] = f.GetValue(f.IsStatic ? null : settings);
            Check(originals.Count >= 15, "Settings fields missing"); checks++;
            var always = originals.Keys.Single(f => f.Name.IndexOf("AlwaysSucceed", StringComparison.OrdinalIgnoreCase) >= 0);
            always.SetValue(always.IsStatic ? null : settings, true);
            var cooldown = originals.Keys.Single(f => f.Name.Equals("CoolDown", StringComparison.OrdinalIgnoreCase));
            cooldown.SetValue(cooldown.IsStatic ? null : settings, Convert.ChangeType(0, cooldown.FieldType));
            string file = Path.Combine(GenFilePaths.SaveDataFolderPath, "SnapOutSettings.xml");
            Scribe.saver.InitSaving(file, "SnapOutSettings"); settings.ExposeData(); Scribe.saver.FinalizeSaving();
            always.SetValue(always.IsStatic ? null : settings, false);
            Scribe.loader.InitLoading(file); settings.ExposeData(); Scribe.loader.FinalizeLoading();
            Check((bool)always.GetValue(always.IsStatic ? null : settings), "Settings did not survive Scribe"); checks++;
            foreach (string name in new[] { "SnapOut_CalmDownJob", "SnapOut_SnappingOutJob", "SnapOut_GoToSafetyJob", "SnapOut_ActuallyRecoverJob" })
            {
                var def = DefDatabase<JobDef>.GetNamed(name);
                Check(def.driverClass.Assembly == assembly, "Wrong driver for " + name); checks++;
                Job saveJob = new Job(def);
                string path = Path.Combine(GenFilePaths.SaveDataFolderPath, name + ".xml");
                Scribe.saver.InitSaving(path, "SnapOutJob"); Scribe_Deep.Look(ref saveJob, "job"); Scribe.saver.FinalizeSaving();
                saveJob = null; Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref saveJob, "job"); Scribe.loader.FinalizeLoading();
                Check(saveJob != null && saveJob.def == def, "Job template did not survive Scribe"); checks++;
            }
            Pawn actor = pawns[0], target = pawns[1];
            var giver = (WorkGiver_Scanner)Activator.CreateInstance(AccessTools.TypeByName("SnapOut.WorkGiver_CalmPawnDown"));
            giver.def = DefDatabase<WorkGiverDef>.GetNamed("SnapOut_CalmPawnDownWorkgiver");
            Check(giver.JobOnThing(actor, target, true) == null, "Healthy pawn incorrectly accepted for calming"); checks++;
            Check(target.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Sad, null, true), "Could not create test mental state"); checks++;
            var originalState = target.MentalState;
            Job job = giver.JobOnThing(actor, target, true);
            Check(job != null && job.def.defName == "SnapOut_CalmDownJob" && job.targetA.Thing == target, "Work giver did not create calm job"); checks++;
            actor.jobs.StartJob(job, JobCondition.InterruptForced);
            Check(actor.jobs.curDriver.GetType().FullName == "SnapOut.JobDriver_CalmDown", "Wrong calm job driver"); checks++;
            var attempt = AccessTools.Method(actor.jobs.curDriver.GetType(), "AttemptCalm");
            Toil action = (Toil)attempt.Invoke(actor.jobs.curDriver, new object[] { Enum.ToObject(attempt.GetParameters()[0].ParameterType, 0) });
            Check(action.initAction != null || action.tickAction != null, "Missing calming action"); checks++;
            if (action.initAction != null) action.initAction(); else action.tickAction();
            Check(target.CurJobDef != null && target.CurJobDef.defName == "SnapOut_SnappingOutJob",
                "Successful calming did not start the recovery job; current=" + (target.CurJobDef == null ? "null" : target.CurJobDef.defName)); checks++;
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        finally { foreach (var item in originals) item.Key.SetValue(item.Key.IsStatic ? null : settings, item.Value); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SnapOutProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"successForcedForTest\":true,\"jobTemplatesHaveNoPawnReferences\":true,\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-snapout-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null;
        GUI.BeginGroup(new Rect(50, 50, 900, 650));
        try { GetMod().DoSettingsWindowContents(new Rect(0, 0, 900, 650)); }
        catch (Exception e) { error = e.ToString(); }
        finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SnapOutUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
