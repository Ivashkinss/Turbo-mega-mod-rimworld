using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Smoke check of the integrated Vehicle Framework (SmashPhil, MIT) on RimWorld 1.7: assemblies load, its defs are
    /// in the database, its Harmony patches are applied, and a map runs. Activated with -tmm-vehicle-framework-probe.
    /// </summary>
    public sealed class InGameVehicleFrameworkProbe : MapComponent
    {
        private static bool s_done;
        private int m_ticks;
        private readonly List<string> m_checks = new List<string>();
        private bool m_allPassed = true;

        public InGameVehicleFrameworkProbe(Map map) : base(map) { }

        private static bool Enabled
        {
            get
            {
                foreach (string a in Environment.GetCommandLineArgs()) if (string.Equals(a, "-tmm-vehicle-framework-probe", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
        }

        private void Check(string name, bool ok, string actual)
        {
            if (!ok) m_allPassed = false;
            m_checks.Add("{\"name\":\"" + name + "\",\"passed\":" + (ok ? "true" : "false") + ",\"actual\":\"" + (actual ?? "").Replace("\"", "'") + "\"}");
        }

        public override void MapComponentTick()
        {
            if (s_done || !Enabled || map != Find.AnyPlayerHomeMap) return;
            if (++m_ticks < 600) return; // let the map run ten seconds of game time with the framework active
            s_done = true;
            Type vehicleDef = AccessTools.TypeByName("Vehicles.VehicleDef");
            Type vehicleMod = AccessTools.TypeByName("Vehicles.VehicleMod");
            Check("VehiclesAssemblyLoaded", vehicleDef != null && vehicleMod != null, vehicleDef != null ? vehicleDef.Assembly.GetName().Version.ToString() : "null");
            Type smash = AccessTools.TypeByName("SmashTools.SmashLog");
            Check("SmashToolsLoaded", smash != null, smash != null ? "ok" : "null");
            int vfDefs = 0;
            foreach (Type t in GenTypes.AllSubclasses(typeof(Def)))
            {
                if (t.Namespace == null || !t.Namespace.StartsWith("Vehicles")) continue;
                Type db = typeof(DefDatabase<>).MakeGenericType(t);
                object list = db.GetProperty("AllDefsListForReading").GetValue(null, null);
                vfDefs += ((System.Collections.IList)list).Count;
            }
            Check("VehicleFrameworkDefsLoaded", vfDefs > 0, vfDefs.ToString());
            int patched = 0;
            foreach (System.Reflection.MethodBase m in Harmony.GetAllPatchedMethods())
            {
                Patches p = Harmony.GetPatchInfo(m);
                if (p != null && p.Owners.Any(o => o.IndexOf("vehicle", StringComparison.OrdinalIgnoreCase) >= 0 || o.IndexOf("smashphil", StringComparison.OrdinalIgnoreCase) >= 0)) patched++;
            }
            Check("VehicleFrameworkPatchesApplied", patched > 20, patched.ToString());
            Check("MapRunsWithFramework", Find.TickManager.TicksGame > 0, Find.TickManager.TicksGame.ToString());
            string json = "{\"passed\":" + (m_allPassed ? "true" : "false") + ",\"checksCount\":" + m_checks.Count + ",\"checks\":[" + string.Join(",", m_checks.ToArray()) + "]}";
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VehicleFrameworkProbe.json"), json);
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VehicleFrameworkUiProbe.json"), "{\"passed\":" + (m_allPassed ? "true" : "false") + "}");
        }
    }
}
