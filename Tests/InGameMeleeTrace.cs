using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

/// Writes down who takes the job away from a pawn that is inside a Melee Animation animation.
[StaticConstructorOnStartup]
public static class MeleeTrace
{
    public static readonly List<string> Lines = new List<string>();
    private static readonly System.Reflection.FieldInfo PawnField = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");
    static MeleeTrace()
    {
        new Harmony("tmm.probe.melee.trace").Patch(AccessTools.Method(typeof(Pawn_JobTracker), "StartJob"), prefix: new HarmonyMethod(typeof(MeleeTrace), "Started"));
        new Harmony("tmm.probe.melee.trace").Patch(AccessTools.Method(AccessTools.TypeByName("AM.Grappling.GrappleFlyer"), "MakeGrappleFlyer"), postfix: new HarmonyMethod(typeof(MeleeTrace), "Flew"));
        new Harmony("tmm.probe.melee.trace").Patch(AccessTools.Method(AccessTools.TypeByName("AM.Grappling.JobDriver_GrapplePawn"), "Ensnare"), prefix: new HarmonyMethod(typeof(MeleeTrace), "Reached") { priority = Priority.First });
        new Harmony("tmm.probe.melee.trace").Patch(AccessTools.Method(typeof(BodyPartDef), "GetMaxHealth"), postfix: new HarmonyMethod(typeof(MeleeTrace), "Health"));
        new Harmony("tmm.probe.melee.trace").Patch(AccessTools.Method(typeof(Pawn), "Kill"), prefix: new HarmonyMethod(typeof(MeleeTrace), "Killed"));
        new Harmony("tmm.probe.melee.trace").Patch(AccessTools.Method(typeof(Pawn), "PostApplyDamage"), prefix: new HarmonyMethod(typeof(MeleeTrace), "Hurt"));
        new Harmony("tmm.probe.melee.trace").Patch(AccessTools.Method(typeof(Pawn_JobTracker), "EndCurrentJob"), prefix: new HarmonyMethod(typeof(MeleeTrace), "Ended"));
    }
    private static void Killed(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
    {
        if (!__instance.RaceProps.Humanlike) return;
        Write(__instance, "KILLED by " + (dinfo.HasValue ? dinfo.Value.ToString() : "none") + " culprit=" + exactCulprit + " hediffs=" + string.Join(",", __instance.health.hediffSet.hediffs.Select(h => h.def.defName + (h.Part == null ? "" : "@" + h.Part.def.defName) + "=" + h.Severity.ToString("F1")).ToArray()));
    }
    /// The victim had walked onto the destination cell by itself before the rope reached it: there is nothing to pull.
    public static readonly HashSet<int> AlreadyThere = new HashSet<int>();
    private static void Reached(object __instance)
    {
        var p = (Pawn)AccessTools.Property(__instance.GetType(), "GrappledPawn").GetValue(__instance, null);
        if (p != null && p.Spawned && p.Position == (IntVec3)AccessTools.Property(__instance.GetType(), "TargetDestination").GetValue(__instance, null)) AlreadyThere.Add(p.thingIDNumber);
    }
    /// Fixture victims with four times the body part health: "wounded" stays a wound whatever part is hit.
    public static readonly HashSet<int> Tough = new HashSet<int>();
    private static void Health(Pawn pawn, ref float __result) { if (pawn != null && Tough.Contains(pawn.thingIDNumber)) __result *= 4f; }
    private static readonly Dictionary<int, int> flights = new Dictionary<int, int>();
    public static int Flights(Pawn p) { int n; return flights.TryGetValue(p.thingIDNumber, out n) ? n : 0; }
    private static void Flew(Pawn victim, object __result) { if (__result == null || victim == null) return; int n; flights.TryGetValue(victim.thingIDNumber, out n); flights[victim.thingIDNumber] = n + 1; }
    private static readonly Dictionary<int, int> hits = new Dictionary<int, int>();
    public static int Hits(Pawn p) { int n; return hits.TryGetValue(p.thingIDNumber, out n) ? n : 0; }
    private static void Hurt(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
    {
        if (__instance.CurJobDef == null || __instance.CurJobDef.defName != "AM_InAnimation") return;
        int n; hits.TryGetValue(__instance.thingIDNumber, out n); hits[__instance.thingIDNumber] = n + 1;
        Lines.Add("tick=" + Find.TickManager.TicksGame + " " + __instance.LabelShort + " hurt " + totalDamageDealt.ToString("F1") + " by " + dinfo.Instigator);
    }
    private static System.Reflection.MethodInfo animator;
    private static void Ended(Pawn_JobTracker __instance, JobCondition condition)
    {
        if (__instance.curJob == null || __instance.curJob.def.defName != "AM_InAnimation") return;
        Pawn p = (Pawn)PawnField.GetValue(__instance);
        if (animator == null) animator = AccessTools.Method(AccessTools.TypeByName("AM.AnimRenderer"), "TryGetAnimator");
        object r = animator.Invoke(null, new object[] { p });
        if (r == null || (bool)AccessTools.Property(r.GetType(), "IsDestroyed").GetValue(r, null) || condition == JobCondition.Succeeded) return;
        Write(p, "ENDED " + condition);
    }
    private static void Write(Pawn p, string what)
    {
        string frames = string.Join(" < ", new StackTrace(2, false).GetFrames().Select(f => f.GetMethod()).Where(x => x != null).Select(x => (x.DeclaringType == null ? "" : x.DeclaringType.Name) + "." + x.Name).Take(16).ToArray());
        string line = "tick=" + Find.TickManager.TicksGame + " " + p.LabelShort + " (" + p.kindDef.defName + ") AM_InAnimation " + what + " :: " + frames;
        Lines.Add(line);
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MeleeTrace.txt"), line + Environment.NewLine);
    }
    private static void Started(Pawn_JobTracker __instance, Job newJob)
    {
        if (__instance.curJob == null || newJob == null || __instance.curJob.def.defName != "AM_InAnimation" || newJob.def.defName == "AM_InAnimation") return;
        Pawn p = (Pawn)PawnField.GetValue(__instance);
        string frames = string.Join(" < ", new StackTrace(1, false).GetFrames().Select(f => f.GetMethod()).Where(x => x != null).Select(x => (x.DeclaringType == null ? "" : x.DeclaringType.Name) + "." + x.Name).Where(x => !x.Contains("DMD")).Take(16).ToArray());
        string line = "tick=" + Find.TickManager.TicksGame + " " + p.LabelShort + " (" + p.kindDef.defName + ") AM_InAnimation -> " + newJob.def.defName + " giver=" + newJob.jobGiver + " :: " + frames;
        Lines.Add(line);
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MeleeTrace.txt"), line + Environment.NewLine);
    }
}