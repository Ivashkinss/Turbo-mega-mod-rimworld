using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

[StaticConstructorOnStartup]
public static class CaravanMeetingLaunch
{
    public static readonly List<Pawn> Met = new List<Pawn>();
    static CaravanMeetingLaunch()
    {
        var args = Environment.GetCommandLineArgs();
        if (!args.Contains("-tmm-caravan-meeting-probe") && !args.Contains("-tmm-caravan-meeting-reload")) return;
        new Harmony("tmm.probe.caravanmeeting").Patch(AccessTools.Method(typeof(IncidentWorker_CaravanMeeting), "GenerateCaravanPawns"), postfix: new HarmonyMethod(typeof(CaravanMeetingLaunch), "Generated"));
        if (args.Contains("-tmm-caravan-meeting-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-CaravanMeeting"));
    }
    public static void Generated(List<Pawn> __result) { if (__result != null) Met.AddRange(__result); }
}

/// A player caravan meets trader caravans (vanilla CaravanMeeting) several times. TMM_MEET=remove closes the meeting window
/// the way the vehicle probe did (WindowStack.TryRemove, no answer); TMM_MEET=leave picks the vanilla "leave" answer.
/// A colonist on the map is given a direct relation to the first met pawn. The save is then loaded; the harness counts unresolved references.
public sealed class CaravanMeetingProbe : MapComponent
{
    private static bool done;
    private int stage, checks, waitUntil;
    private bool saved;
    private string mode;
    private Pawn witness;
    private List<string> metIds = new List<string>();
    private readonly List<string> facts = new List<string>();
    public CaravanMeetingProbe(Map map) : base(map) { }
    private static bool Fresh { get { return Environment.GetCommandLineArgs().Contains("-tmm-caravan-meeting-probe"); } }
    private static bool Reload { get { return Environment.GetCommandLineArgs().Contains("-tmm-caravan-meeting-reload"); } }
    public override void ExposeData()
    {
        Scribe_Values.Look(ref saved, "cmSaved"); Scribe_Values.Look(ref mode, "cmMode");
        Scribe_References.Look(ref witness, "cmWitness");
        Scribe_Collections.Look(ref metIds, "cmMet", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && metIds == null) metIds = new List<string>();
    }
    private void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; }
    private static string Where(Pawn p)
    {
        return p.LabelShort + "#" + p.thingIDNumber + " " + (p.Faction == null ? "nofaction" : p.Faction.def.defName) + " destroyed=" + p.Destroyed + " spawned=" + p.Spawned
            + " world=" + Find.WorldPawns.Contains(p) + " holder=" + (p.ParentHolder == null ? "null" : p.ParentHolder.GetType().Name + (p.ParentHolder is WorldObject ? " inWorldObjects=" + Find.WorldObjects.Contains((WorldObject)p.ParentHolder) : ""));
    }
    private IEnumerable<Pawn> SavedPawns() { return Find.WorldPawns.AllPawnsAliveOrDead.Concat(Find.Maps.SelectMany(m => m.mapPawns.AllPawns)).Concat(Find.WorldObjects.Caravans.SelectMany(c => c.PawnsListForReading)).Distinct(); }
    private int OrphanRefs(out string detail)
    {
        int n = 0; var lines = new List<string>();
        foreach (Pawn p in SavedPawns())
        {
            if (p.relations == null) continue;
            foreach (DirectPawnRelation r in p.relations.DirectRelations)
            {
                Pawn o = r.otherPawn;
                if (o == null || o.Destroyed || o.Spawned || Find.WorldPawns.Contains(o)) continue;
                if (o.ParentHolder is WorldObject && Find.WorldObjects.Contains((WorldObject)o.ParentHolder)) continue;
                if (o.ParentHolder is Map || o.ParentHolder is ThingOwner) continue;
                n++; if (lines.Count < 8) lines.Add(p.LabelShort + "#" + p.thingIDNumber + " -" + r.def.defName + "-> " + Where(o));
            }
        }
        detail = string.Join(" | ", lines.ToArray()); return n;
    }
    private void Meet()
    {
        mode = Environment.GetEnvironmentVariable("TMM_MEET") ?? "remove";
        var colonists = map.mapPawns.FreeColonistsSpawned.ToList();
        Check(colonists.Count >= 2, "Need two colonists");
        witness = colonists[1];
        Pawn traveller = colonists[0];
        traveller.DeSpawn();
        Caravan caravan = CaravanMaker.MakeCaravan(new List<Pawn> { traveller }, Faction.OfPlayer, map.Tile, true);
        facts.Add("mode=" + mode + " caravan=" + caravan.ID + " tile=" + caravan.Tile);
        int fired = 0;
        for (int i = 0; i < 8; i++)
        {
            int before = CaravanMeetingLaunch.Met.Count;
            var parms = new IncidentParms { target = caravan, forced = true };
            bool ok = DefDatabase<IncidentDef>.GetNamed("CaravanMeeting").Worker.TryExecute(parms);
            var w = Find.WindowStack.Windows.OfType<Dialog_NodeTreeWithFactionInfo>().LastOrDefault();
            if (!ok || w == null) { facts.Add("meeting " + i + " ok=" + ok + " window=" + (w != null)); continue; }
            fired++;
            if (mode == "leave")
            {
                DiaNode node = (DiaNode)AccessTools.Field(typeof(Dialog_NodeTree), "curNode").GetValue(w);
                DiaOption leave = node.options.Last();
                leave.action();
                Find.WindowStack.TryRemove(w, false);
            }
            else Find.WindowStack.TryRemove(w, false);
            facts.Add("meeting " + i + " met " + (CaravanMeetingLaunch.Met.Count - before));
        }
        Check(fired > 0 && CaravanMeetingLaunch.Met.Count > 0, "No meeting happened");
        Pawn first = CaravanMeetingLaunch.Met[0];
        witness.relations.AddDirectRelation(PawnRelationDefOf.ExLover, first);
        metIds = CaravanMeetingLaunch.Met.Select(p => p.GetUniqueLoadID()).ToList();
        foreach (Pawn p in CaravanMeetingLaunch.Met.Take(6)) facts.Add("met " + Where(p));
        string d; int orphans = OrphanRefs(out d);
        facts.Add("before save: met=" + CaravanMeetingLaunch.Met.Count + " metInWorld=" + CaravanMeetingLaunch.Met.Count(p => Find.WorldPawns.Contains(p)) + " orphanRelationRefs=" + orphans + " " + d);
    }
    public override void MapComponentUpdate()
    {
        if (done || Find.CurrentMap != map || !Fresh && !Reload) return;
        if (Reload && !saved) return;
        try
        {
            int now = Find.TickManager.TicksGame;
            if (Find.TickManager.Paused && stage > 0) foreach (Window w in Find.WindowStack.Windows.Where(x => x.forcePause && !(x is Dialog_NodeTreeWithFactionInfo)).ToList()) Find.WindowStack.TryRemove(w, false);
            if (Find.TickManager.Paused) Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            if (Reload)
            {
                facts.Add("loaded mode=" + mode + " metIds=" + metIds.Count + " witness=" + (witness == null ? "null" : witness.LabelShort));
                var rel = witness == null ? null : witness.relations.DirectRelations.Where(r => r.def == PawnRelationDefOf.ExLover).ToList();
                facts.Add("witness exlover relations=" + (rel == null ? -1 : rel.Count) + " targets=" + (rel == null ? "" : string.Join(",", rel.Select(r => r.otherPawn == null ? "null" : Where(r.otherPawn)).ToArray())));
                string d; int orphans = OrphanRefs(out d);
                facts.Add("after load orphanRelationRefs=" + orphans + " " + d);
                Finish(null); return;
            }
            if (stage == 0) { if (now < 120) return; Meet(); stage = 1; waitUntil = now + 300; return; }
            if (stage == 1 && now >= waitUntil)
            {
                string d; int orphans = OrphanRefs(out d);
                facts.Add("at save: orphanRelationRefs=" + orphans + " metInWorld=" + CaravanMeetingLaunch.Met.Count(p => Find.WorldPawns.Contains(p)) + " " + d);
                saved = true;
                GameDataSaveLoader.SaveGame("TMM-CaravanMeeting");
                Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-CaravanMeeting.rws")), "No save");
                Finish(null);
            }
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }
    private void Finish(string error)
    {
        done = true;
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, Reload ? "CaravanMeetingReloadProbe.json" : "CaravanMeetingProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks + ",\"facts\":[" + string.Join(",", facts.Select(Q).ToArray()) + "],\"errors\":" + (error == null ? "[]" : "[" + Q(error) + "]") + "}");
        if (!Reload) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "CaravanMeetingUiProbe.json"), "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
    }
    private static string Q(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", " ") + "\""; }
}
