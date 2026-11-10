// Test-only startup timing, real map activity and optional copied-save loading.
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Verse;
using UnityEngine;
[StaticConstructorOnStartup]
public static class GpuLoadSaveProbe {
    static GpuLoadSaveProbe() {
        if (!Environment.GetCommandLineArgs().Contains("-tmm-load-repro")) return;
        LongEventHandler.ExecuteWhenFinished(delegate {
            Log.Message("[TMM GPU repro] Loading isolated copy of user save.");
            GameDataSaveLoader.LoadGame("CrashRepro");
        });
    }
}
public sealed class GpuMapProbe : MapComponent {
    private Stopwatch watch;
    private int firstFrame;
    private int firstTick;
    private bool reported;
    public GpuMapProbe(Map map) : base(map) { }
    public override void MapComponentUpdate() {
        if (!Environment.GetCommandLineArgs().Contains("-tmm-render-probe") || Find.CurrentMap != map) return;
        if (watch == null) {
            watch = Stopwatch.StartNew(); firstFrame = Time.frameCount; firstTick = Find.TickManager.TicksGame;
            Log.Message("[TMM GPU repro] Map active: " + map.Size + "; frame " + firstFrame + "; tick " + firstTick);
        }
        if (!reported && watch.Elapsed.TotalSeconds >= 30) {
            reported = true;
            string report = "{\"mapLoaded\":true,\"observedActiveSeconds\":30,\"framesAdvanced\":" + (Time.frameCount-firstFrame) + ",\"ticksAdvanced\":" + (Find.TickManager.TicksGame-firstTick) + "}";
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"GpuMapProbe.json"),report);
            Log.Message("[TMM GPU repro] " + report);
        }
    }
}

public sealed class GpuInitTimingMod : Mod {
    public GpuInitTimingMod(ModContentPack content) : base(content) {
        if (!Environment.GetCommandLineArgs().Contains("-tmm-startup-profile")) return;
        var target = AccessTools.Method(typeof(StaticConstructorOnStartupUtility), "CallAll");
        new Harmony("tmm.diagnostic.startup").Patch(target, transpiler:new HarmonyMethod(typeof(GpuInitTimingMod),"Trace"));
    }
    private static IEnumerable<CodeInstruction> Trace(IEnumerable<CodeInstruction> instructions) {
        foreach (var instruction in instructions) {
            var method = instruction.operand as MethodInfo;
            if (instruction.opcode == OpCodes.Call && method != null && method.DeclaringType == typeof(RuntimeHelpers) && method.Name == "RunClassConstructor")
                instruction.operand = AccessTools.Method(typeof(GpuInitTimingMod),"RunTimed");
            yield return instruction;
        }
    }
    private static void RunTimed(RuntimeTypeHandle handle) {
        var watch = Stopwatch.StartNew();
        try { RuntimeHelpers.RunClassConstructor(handle); }
        finally {
            if (watch.ElapsedMilliseconds >= 100) Log.Message("[TMM startup] " + Type.GetTypeFromHandle(handle).FullName + ": " + watch.ElapsedMilliseconds + " ms");
        }
    }
}
