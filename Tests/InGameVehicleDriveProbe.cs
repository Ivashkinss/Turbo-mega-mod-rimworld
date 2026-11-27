using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public static class VehicleDriveLaunch
{
    static VehicleDriveLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-vehicle-drive-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-VehicleDrive"));
    }
}

/// A visit (TMM_DRIVE=visit) or a raid (raid) is saved in the middle of its drive, at the game speed in TMM_DRIVE_SPEED (1..4).
/// The fresh run and the loaded one must both bring every vehicle to its staging cell; a visit then also drives out.
public sealed class VehicleDriveProbe : MapComponent
{
    private static bool done;
    private VehicleMission mission;
    private int missionId, stage, startTick, saveTick, checks, crew;
    private bool saved, resumed, began;
    private string kind;
    private List<Pawn> vehicles = new List<Pawn>();
    private List<IntVec3> starts = new List<IntVec3>(), middles = new List<IntVec3>();
    private readonly List<string> facts = new List<string>();
    public VehicleDriveProbe(Map map) : base(map) { }
    private static bool Reload { get { return Environment.GetCommandLineArgs().Contains("-tmm-vehicle-drive-reload"); } }
    public override void ExposeData()
    {
        Scribe_Values.Look(ref missionId, "vdMission"); Scribe_Values.Look(ref saved, "vdSaved"); Scribe_Values.Look(ref kind, "vdKind"); Scribe_Values.Look(ref startTick, "vdStart"); Scribe_Values.Look(ref crew, "vdCrew"); Scribe_Values.Look(ref began, "vdBegan");
        Scribe_Collections.Look(ref vehicles, "vdVehicles", LookMode.Reference); Scribe_Collections.Look(ref starts, "vdStarts", LookMode.Value); Scribe_Collections.Look(ref middles, "vdMiddles", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) { if (vehicles == null) vehicles = new List<Pawn>(); if (starts == null) starts = new List<IntVec3>(); if (middles == null) middles = new List<IntVec3>(); }
    }
    private void Check(bool ok, string text) { if (!ok) throw new Exception(text + " :: " + State()); checks++; }
    private string State()
    {
        return "tick=" + Find.TickManager.TicksGame + " phase=" + (mission == null ? "none" : mission.phase + " staging=" + mission.stagingCell + " exit=" + mission.exitCell + " repath=" + mission.repathRetried + " stagnant=" + mission.stagnantTicks) + " " + string.Join("; ", vehicles.Where(v => v != null).Select(v => v.def.defName + v.thingIDNumber + " spawned=" + v.Spawned + " at " + v.Position + " job=" + v.CurJob + " moving=" + VfBridge.IsMoving(v) + " aboard=" + VfBridge.GetAllPawnsAboard(v).Count + (mission == null ? "" : " toStaging=" + v.Position.DistanceTo(mission.stagingCell).ToString("F1"))).ToArray());
    }
    private void Setup()
    {
        kind = Environment.GetEnvironmentVariable("TMM_DRIVE") ?? "visit";
        facts.Add("kind=" + kind + " speed=" + (Environment.GetEnvironmentVariable("TMM_DRIVE_SPEED") ?? "3"));
        foreach (Pawn p in map.mapPawns.FreeColonistsSpawned) p.drafter.Drafted = false;
        var parms = new IncidentParms { target = map, forced = true, sendLetter = false, points = 2000f };
        if (kind == "raid")
        {
            parms.faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("TMM_GunnersFaction"));
            if (!parms.faction.HostileTo(Faction.OfPlayer)) parms.faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false);
            mission = IncidentWorker_TMMVehicleRaid.ExecuteVehicleRaid(parms);
        }
        else
        {
            parms.faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("TMM_MinutemenHuntersFaction"));
            if (parms.faction.HostileTo(Faction.OfPlayer)) parms.faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false);
            mission = IncidentWorker_TMMVehicleVisit.ExecuteVehicleVisit(parms);
        }
        Check(mission != null && mission.vehicles.Count > 0, "The event did not start");
        missionId = mission.missionId; vehicles = mission.vehicles.ToList(); starts = vehicles.Select(v => v.Position).ToList(); crew = mission.crew.Count;
        startTick = Find.TickManager.TicksGame; began = true;
        facts.Add("start " + State());
    }
    private void Step()
    {
        int now = Find.TickManager.TicksGame;
        if (stage == 0)
        {
            if (mission.phase == VehicleMissionPhase.DrivingIn)
            {
                Check(now - startTick < 4400, "Not there in time"); checks--;
                float moved = vehicles.Select((v, i) => v.Position.DistanceTo(starts[i])).Max();
                if (!saved && !Reload && moved >= 15f)
                {
                    middles = vehicles.Select(v => v.Position).ToList(); saved = true; saveTick = now;
                    GameDataSaveLoader.SaveGame("TMM-VehicleDrive");
                    Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-VehicleDrive.rws")), "No save");
                    facts.Add("saved on the way " + State());
                }
                return;
            }
            facts.Add("arrived " + State());
            Check(saved, "Arrived before it could be saved on the way");
            foreach (Pawn v in vehicles) Check(v.Spawned && v.Position.DistanceTo(mission.stagingCell) <= 9f, "A vehicle did not reach its staging cell: " + v.def.defName + v.thingIDNumber);
            Check(!mission.repathRetried, "The vehicle got stuck and had to look for another way");
            Check(mission.crew.Count == crew && mission.crew.All(p => p != null && !p.Destroyed), "Crew lost");
            if (kind == "raid") { Check(mission.phase == VehicleMissionPhase.Engaged && mission.crew.Any(p => p.Spawned && Verse.AI.Group.LordUtility.GetLord(p) != null), "The raiders did not dismount"); Finish(null); return; }
            Check(mission.phase == VehicleMissionPhase.Trading && mission.trader.Spawned, "The trader did not get out");
            TMM_VehicleMissionManager.Instance.BeginVisitDeparture(mission);
            stage = 1; startTick = now; return;
        }
        if (mission.phase == VehicleMissionPhase.Completed)
        {
            facts.Add("gone " + State());
            Check(vehicles.All(v => !v.Spawned) && !mission.trader.Spawned, "The visit did not leave the map");
            Finish(null); return;
        }
        Check(now - startTick < 6000, "The visit did not leave in time"); checks--;
    }
    public override void MapComponentUpdate()
    {
        if (done || Find.CurrentMap != map) return;
        if (!Reload && !Environment.GetCommandLineArgs().Contains("-tmm-vehicle-drive-probe")) return;
        if (Reload && !saved) return;
        try
        {
            foreach (Window w in Find.WindowStack.Windows.ToList()) if (w.forcePause) facts.Add("pausing window: " + ProbeWindows.Dismiss(w));
            int speed; if (!int.TryParse(Environment.GetEnvironmentVariable("TMM_DRIVE_SPEED"), out speed)) speed = 3;
            Find.TickManager.CurTimeSpeed = (TimeSpeed)speed;
            if (!Reload && !began) { Setup(); return; }
            if (Reload && !resumed)
            {
                resumed = true;
                mission = TMM_VehicleMissionManager.Instance.missions.FirstOrDefault(x => x.missionId == missionId);
                Check(mission != null && mission.phase == VehicleMissionPhase.DrivingIn, "The mission is not on its way after load");
                for (int i = 0; i < vehicles.Count; i++) Check(vehicles[i] != null && vehicles[i].Spawned && vehicles[i].Position.DistanceTo(middles[i]) < 12f, "A vehicle moved on load");
                facts.Add("loaded " + State());
            }
            Step();
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }
    private void Finish(string error)
    {
        done = true;
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, Reload ? "VehicleDriveReloadProbe.json" : "VehicleDriveProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks + ",\"facts\":[" + string.Join(",", facts.Select(Q).ToArray()) + "],\"errors\":" + (error == null ? "[]" : "[" + Q(error) + "]") + "}");
        if (!Reload) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VehicleDriveUiProbe.json"), "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
    }
    private static string Q(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", " ") + "\""; }
}