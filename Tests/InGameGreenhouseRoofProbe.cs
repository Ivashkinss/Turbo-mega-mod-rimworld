using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using TurboMegaMod;
using Verse;

// Greenhouse roof rules (user report 18.11.2026). Flag: -tmm-greenhouse-roof-probe.
// A ring of glass walls must not get the vanilla automatic roof area; a ring of ordinary walls must (control).
// Designating a manual greenhouse over cells marked "no roof" must clear that mark.
[StaticConstructorOnStartup]
public static class GreenhouseRoofProbeLaunch
{
    static GreenhouseRoofProbeLaunch()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps == null) return;
            Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
            if (home != null && home.GetComponent<GreenhouseRoofProbe>() == null) home.components.Add(new GreenhouseRoofProbe(home));
        });
    }
}

public sealed class GreenhouseRoofProbe : MapComponent
{
    private sealed class Check { public string name, expected, actual; public bool passed; }
    private readonly List<Check> checks = new List<Check>();
    private bool done;
    private int stage, waitUntil = -1;
    private CellRect glassRect, stoneRect;

    public GreenhouseRoofProbe(Map map) : base(map) { }

    private void C(string name, bool ok, object expected, object actual)
    {
        checks.Add(new Check { name = name, passed = ok, expected = Convert.ToString(expected), actual = Convert.ToString(actual) });
    }

    public override void MapComponentUpdate()
    {
        if (done || !map.IsPlayerHome || !Environment.GetCommandLineArgs().Contains("-tmm-greenhouse-roof-probe")) return;
        if (Find.TickManager.Paused) Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
        if (waitUntil >= 0 && Find.TickManager.TicksGame < waitUntil) return;
        try
        {
            if (stage == 0) { Build(); stage = 1; waitUntil = Find.TickManager.TicksGame + 300; return; }
            if (stage == 1) { VerifyAutoRoof(); VerifyManual(); Finish(null); }
        }
        catch (Exception ex) { Finish(ex); }
    }

    private void Clear(CellRect r)
    {
        foreach (IntVec3 c in r.ExpandedBy(1))
        {
            if (!c.InBounds(map)) continue;
            foreach (Thing t in c.GetThingList(map).ToList()) if (!(t is Pawn)) t.Destroy(DestroyMode.Vanish);
            map.roofGrid.SetRoof(c, null);
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil);
            map.areaManager.BuildRoof[c] = false;
            map.areaManager.NoRoof[c] = false;
        }
    }

    private void Ring(CellRect r, ThingDef wall, ThingDef door)
    {
        IntVec3 doorCell = new IntVec3(r.minX + r.Width / 2, 0, r.minZ);
        foreach (IntVec3 c in r.EdgeCells)
        {
            ThingDef d = c == doorCell && door != null ? door : wall;
            Thing t = ThingMaker.MakeThing(d, d.MadeFromStuff ? GenStuff.DefaultStuffFor(d) : null);
            t.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(t, c, map);
        }
    }

    private void Build()
    {
        IntVec3 a = map.Center + new IntVec3(-12, 0, 8), b = map.Center + new IntVec3(4, 0, 8);
        glassRect = CellRect.CenteredOn(a, 3); stoneRect = CellRect.CenteredOn(b, 3);
        Clear(glassRect); Clear(stoneRect);
        Ring(glassRect, DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseGlassWall"), DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseGlassDoor"));
        Ring(stoneRect, ThingDefOf.Wall, ThingDefOf.Door);
    }

    private void VerifyAutoRoof()
    {
        var inner = glassRect.ContractedBy(1).Cells.ToList();
        var innerStone = stoneRect.ContractedBy(1).Cells.ToList();
        Room gr = inner[0].GetRoom(map), sr = innerStone[0].GetRoom(map);
        C("GlassRingIsRoom", gr != null && !gr.TouchesMapEdge, "enclosed room", gr == null ? "null" : gr.CellCount.ToString());
        C("GlassRoomEnclosedByGlass", GreenhouseRoofRules.EnclosedByGlass(gr), true, GreenhouseRoofRules.EnclosedByGlass(gr));
        int stoneMarked = innerStone.Count(c => map.areaManager.BuildRoof[c]);
        C("ControlStoneRoomGetsAutoRoof", stoneMarked == innerStone.Count, innerStone.Count, stoneMarked);
        int glassMarked = inner.Count(c => map.areaManager.BuildRoof[c]);
        C("GlassRoomGetsNoAutoRoof", glassMarked == 0, 0, glassMarked);
    }

    private void VerifyManual()
    {
        var inner = glassRect.ContractedBy(1).Cells.ToList();
        foreach (IntVec3 c in inner) map.areaManager.NoRoof[c] = true; // the user's earlier "remove roof" marks
        var zone = new Zone_Greenhouse(map.zoneManager) { manualLayout = true };
        map.zoneManager.RegisterZone(zone);
        foreach (IntVec3 c in inner) zone.AddCell(c);
        bool ok = zone.InitializeManual();
        C("ManualGreenhouseInitialized", ok, true, ok);
        int build = inner.Count(c => map.areaManager.BuildRoof[c]);
        int noRoof = inner.Count(c => map.areaManager.NoRoof[c]);
        C("ManualGreenhouseMarksRoof", build == inner.Count, inner.Count, build);
        C("ManualGreenhouseClearsNoRoof", noRoof == 0, 0, noRoof);
    }

    private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    private void Finish(Exception ex)
    {
        done = true;
        bool passed = ex == null && checks.Count > 0 && checks.All(c => c.passed);
        var cj = checks.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}", J(c.name), c.passed ? "true" : "false", J(c.expected), J(c.actual)));
        string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks.Count + ",\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
            "\n  ],\n  \"errors\": [" + (ex == null ? "" : "\"" + J(ex.ToString()) + "\"") + "]\n}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "GreenhouseRoofProbe.json"), json);
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "GreenhouseRoofUiProbe.json"), "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":false}");
    }
}
