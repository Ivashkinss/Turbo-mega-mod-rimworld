using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using Verse;

// Hotel visit guarantee. Flag: -tmm-hotel-visit-probe. Empties the Hospitality visit queue, then checks that
// the guarantee plans exactly one visit by a friendly faction within the window, and does nothing twice.
[StaticConstructorOnStartup]
public static class HotelVisitProbeLaunch
{
    static HotelVisitProbeLaunch()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps == null) return;
            Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
            if (home != null && home.GetComponent<HotelVisitProbe>() == null) home.components.Add(new HotelVisitProbe(home));
        });
    }
}

public sealed class HotelVisitProbe : MapComponent
{
    private sealed class Check { public string name, expected, actual; public bool passed; }
    private readonly List<Check> checks = new List<Check>();
    private bool done;
    private int frames;

    public HotelVisitProbe(Map map) : base(map) { }

    private void C(string name, bool ok, object expected, object actual)
    {
        checks.Add(new Check { name = name, passed = ok, expected = Convert.ToString(expected), actual = Convert.ToString(actual) });
    }

    public override void MapComponentUpdate()
    {
        if (done || !map.IsPlayerHome || !Environment.GetCommandLineArgs().Contains("-tmm-hotel-visit-probe")) return;
        if (++frames < 30) return;
        try
        {
            C("GuaranteeAvailable", HotelVisitGuarantee.Available, true, HotelVisitGuarantee.Available);
            MapComponent hosp = HotelVisitGuarantee.HospitalityOf(map);
            C("HospitalityComponentOnMap", hosp != null, true, hosp != null);
            var queueField = AccessTools.Field(hosp.GetType(), "incidentQueue");
            object queue = queueField.GetValue(hosp);
            var list = AccessTools.Field(queue.GetType(), "queuedIncidents").GetValue(queue) as IList;
            list.Clear();
            C("QueueEmptied", HotelVisitGuarantee.QueuedWithin(map, 30f) == 0, 0, HotelVisitGuarantee.QueuedWithin(map, 30f));
            Faction f = HotelVisitGuarantee.CheckMap(map);
            C("VisitPlanned", f != null, "faction", f == null ? "null" : f.Name);
            C("PlannedFactionFriendly", f != null && !f.HostileTo(Faction.OfPlayer) && !f.IsPlayer, "friendly", f == null ? "null" : f.HostileTo(Faction.OfPlayer).ToString());
            int within = HotelVisitGuarantee.QueuedWithin(map, HotelVisitGuarantee.WindowDays);
            C("OneVisitWithinTwoDays", within == 1, 1, within);
            Faction again = HotelVisitGuarantee.CheckMap(map);
            C("NoDuplicateWhenQueued", again == null, "null", again == null ? "null" : again.Name);
            Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }

    private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    private void Finish(Exception ex)
    {
        done = true;
        bool passed = ex == null && checks.Count > 0 && checks.All(c => c.passed);
        var cj = checks.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}", J(c.name), c.passed ? "true" : "false", J(c.expected), J(c.actual)));
        string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks.Count + ",\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
            "\n  ],\n  \"errors\": [" + (ex == null ? "" : "\"" + J(ex.ToString()) + "\"") + "]\n}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HotelVisitProbe.json"), json);
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HotelVisitUiProbe.json"), "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":false}");
    }
}
