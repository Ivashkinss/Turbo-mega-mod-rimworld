using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;

/// Writes down what Vehicle Framework does with the paths of raid and visit vehicles.
[StaticConstructorOnStartup]
public static class VehicleTrace
{
    private static Type pather = AccessTools.TypeByName("Vehicles.VehiclePathFollower");
    private static readonly object gate = new object();
    static VehicleTrace()
    {
        var harmony = new Harmony("tmm.probe.vehicle.trace");
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TMM_WORLD_SEED"))) harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.WorldGenerator), "GenerateWorld"), prefix: new HarmonyMethod(typeof(VehicleTrace), "Seed"));
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TMM_WORLD_TILE"))) harmony.Patch(AccessTools.Method(typeof(Game), "InitNewGame"), prefix: new HarmonyMethod(typeof(VehicleTrace), "Tile"));
        harmony.Patch(AccessTools.Method(pather, "StartPath"), postfix: new HarmonyMethod(typeof(VehicleTrace), "Start"));
        harmony.Patch(AccessTools.Method(pather, "PatherFailed"), prefix: new HarmonyMethod(typeof(VehicleTrace), "Failed"));
        harmony.Patch(AccessTools.Method(pather, "StopDead"), prefix: new HarmonyMethod(typeof(VehicleTrace), "Stopped"));
        harmony.Patch(AccessTools.Method(pather, "PatherArrived"), prefix: new HarmonyMethod(typeof(VehicleTrace), "Arrived"));
        harmony.Patch(AccessTools.Method(pather, "RequestNewPath"), postfix: new HarmonyMethod(typeof(VehicleTrace), "Request"));
        harmony.Patch(AccessTools.Method(pather, "FindPath"), prefix: new HarmonyMethod(typeof(VehicleTrace), "FindStart"), postfix: new HarmonyMethod(typeof(VehicleTrace), "Found"));
        harmony.Patch(AccessTools.Method(typeof(Messages), "Message", new[] { typeof(string), typeof(MessageTypeDef), typeof(bool) }), prefix: new HarmonyMethod(typeof(VehicleTrace), "Said"));
    }
    private static void Seed(ref string seedString) { seedString = Environment.GetEnvironmentVariable("TMM_WORLD_SEED"); }
    private static void Tile() { int t; if (int.TryParse(Environment.GetEnvironmentVariable("TMM_WORLD_TILE"), out t) && Find.GameInitData != null) Find.GameInitData.startingTile = t; }
    public static void Write(string text)
    {
        lock (gate) File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VehicleTrace.txt"), DateTime.Now.ToString("HH:mm:ss.fff") + " tick=" + (Find.TickManager == null ? -1 : Find.TickManager.TicksGame) + " frame=" + Frame + " " + text + Environment.NewLine);
    }
    public static int Frame;
    private static object Get(object o, string n) { var f = AccessTools.Field(o.GetType(), n); if (f != null) return f.GetValue(o); var p = AccessTools.Property(o.GetType(), n); return p == null ? null : p.GetValue(o, null); }
    public static Pawn Vehicle(object follower) { return (Pawn)Get(follower, "vehicle"); }
    public static string State(object f)
    {
        Pawn v = Vehicle(f); var task = Get(f, "curPathTask") as System.Threading.Tasks.Task; object path = Get(f, "curPath");
        return v.def.defName + v.thingIDNumber + " at " + v.Position + " dest=" + Get(f, "destination") + " moving=" + Get(f, "moving") + " status=" + Get(f, "RequestStatus") + " task=" + (task == null ? "null" : task.Status.ToString()) + " path=" + (path == null ? "null" : "nodes " + Get(path, "NodesLeft")) + " wait=" + Get(f, "waitTicks") + " drafted=" + v.Drafted + " canMove=" + Get(v, "CanMoveFinal") + " cost=" + Get(f, "nextCellCostLeft");
    }
    private static string Caller() { return string.Join(" < ", new StackTrace(2, false).GetFrames().Select(x => x.GetMethod()).Where(x => x != null).Select(x => (x.DeclaringType == null ? "" : x.DeclaringType.Name) + "." + x.Name).Take(12).ToArray()); }
    private static bool Ours(object f) { Pawn v = Vehicle(f); return v != null && v.Faction != null && (!v.Faction.IsPlayer || Environment.GetEnvironmentVariable("TMM_TRACE_PLAYER") == "1"); }
    private static void Start(object __instance, LocalTargetInfo __0) { if (Ours(__instance)) { Write("START " + State(__instance) + " asked=" + __0 + " :: " + Reach(Vehicle(__instance), __0.Cell)); } }
    private static void Failed(object __instance) { if (Ours(__instance)) Write("FAILED " + State(__instance) + " :: " + Caller()); }
    private static void Stopped(object __instance) { if (Ours(__instance) && Vehicle(__instance).Spawned) Write("STOP " + State(__instance) + " :: " + Caller()); }
    private static void Arrived(object __instance) { if (Ours(__instance)) Write("ARRIVED " + State(__instance) + " :: " + Caller()); }
    private static void Request(object __instance) { if (Ours(__instance)) Write("REQUEST " + State(__instance)); }
    private static void FindStart(object __instance) { if (Ours(__instance)) Write("FIND " + State(__instance)); }
    private static void Found(object __instance, object __result) { if (Ours(__instance)) Write("FOUND " + (__result == null ? "null" : "found=" + Get(__result, "Found") + " nodes=" + Get(__result, "NodesLeft")) + " " + State(__instance)); }
    private static void Said(string text) { Write("MESSAGE " + text); }
    /// What the framework's own tests say about the trip, asked on the main thread.
    public static string Reach(Pawn v, IntVec3 dest)
    {
        try
        {
            Type ext = AccessTools.TypeByName("Vehicles.Ext_Vehicles"), reach = AccessTools.TypeByName("Vehicles.VehicleReachabilityUtility"), grid = AccessTools.TypeByName("Vehicles.GenGridVehicles");
            string s = "";
            foreach (var m in AccessTools.GetDeclaredMethods(ext).Where(x => x.Name == "DrivableRectOnCell")) { var ps = m.GetParameters(); object[] a = ps.Select(x => x.ParameterType.IsAssignableFrom(v.GetType()) ? (object)v : x.ParameterType == typeof(IntVec3) ? (object)dest : x.ParameterType.IsValueType ? Activator.CreateInstance(x.ParameterType) : null).ToArray(); s += " fits=" + m.Invoke(null, a); }
            foreach (var m in AccessTools.GetDeclaredMethods(reach).Where(x => x.Name == "CanReachVehicle" && x.GetParameters().Length == 5)) s += " reach=" + m.Invoke(null, new object[] { v, new LocalTargetInfo(dest), PathEndMode.OnCell, Danger.Deadly, TraverseMode.PassDoors });
            foreach (var m in AccessTools.GetDeclaredMethods(grid).Where(x => x.Name == "Walkable" && x.GetParameters().Length == 3)) if (m.GetParameters()[2].ParameterType == typeof(Map)) s += " walkHere=" + m.Invoke(null, new object[] { v.Position, v.def, v.Map }) + " walkThere=" + m.Invoke(null, new object[] { dest, v.def, v.Map });
            object sys = v.Map.components.First(c => c.GetType().FullName == "Vehicles.VehiclePathingSystem");
            object data = AccessTools.Method(sys.GetType(), "get_Item").Invoke(sys, new object[] { v.def });
            s += " gridEnabled=" + Get(Get(data, "VehiclePathGrid"), "Enabled") + " suspended=" + Get(data, "Suspended") + " thread=" + Get(sys, "ThreadAvailable");
            return s;
        }
        catch (Exception ex) { return "reach error " + ex.GetBaseException().Message; }
    }
}
public sealed class VehicleTraceMap : MapComponent
{
    private int last;
    public VehicleTraceMap(Map map) : base(map) { }
    public override void MapComponentUpdate()
    {
        VehicleTrace.Frame++;
        if (Find.TickManager.TicksGame - last < 40) return;
        last = Find.TickManager.TicksGame;
        foreach (Pawn v in map.mapPawns.AllPawnsSpawned.Where(p => VfBridge.IsVehiclePawn(p) && p.Faction != null && (!p.Faction.IsPlayer || Environment.GetEnvironmentVariable("TMM_TRACE_PLAYER") == "1")).ToList())
            VehicleTrace.Write("STATE " + VehicleTrace.State(AccessTools.Field(v.GetType(), "vehiclePather").GetValue(v)) + " pending=" + VfBridge.PathPending(v));
    }
}