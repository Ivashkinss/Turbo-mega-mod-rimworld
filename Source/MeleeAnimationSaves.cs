using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    /// A save made while Melee Animation is at work goes on after loading.
    /// On the rope: the animation that follows the pull is a struct saved by value. Its pawns and map were resolved on a copy and
    /// lost, the victim and the outcome were never saved, and the game cannot read a Matrix4x4 at all.
    /// Inside an animation: the running animation came back only after the pawns had ticked once, so they had already dropped
    /// the animation job, and the resumed animation was taken for one started twice.
    [StaticConstructorOnStartup]
    public static class MeleeAnimationSaves
    {
        private static readonly FieldInfo startField, hasStarted;
        private static readonly FieldInfo main, second, extra, map, outcome, customJob, unregistered, flipX, flipY;
        private static readonly PropertyInfo grappledPawn;
        private static readonly MethodInfo addFromPostLoad;
        private static bool resuming;

        static MeleeAnimationSaves()
        {
            if (!ParseHelper.HandlesType(typeof(Matrix4x4))) ParseHelper.Parsers<Matrix4x4>.Register(ParseMatrix);
            Type driver = AccessTools.TypeByName("AM.Grappling.JobDriver_GrapplePawn"), start = AccessTools.TypeByName("AM.AnimationStartParameters");
            Type renderer = AccessTools.TypeByName("AM.AnimRenderer"), manager = AccessTools.TypeByName("AM.AnimationManager");
            if (driver == null || start == null || renderer == null || manager == null) return;
            Type me = typeof(MeleeAnimationSaves);
            Harmony harmony = new Harmony("turbomegamod.melee.saves");
            startField = AccessTools.Field(driver, "AnimationStartParameters");
            main = AccessTools.Field(start, "MainPawn"); second = AccessTools.Field(start, "SecondPawn"); extra = AccessTools.Field(start, "ExtraPawns");
            map = AccessTools.Field(start, "Map"); outcome = AccessTools.Field(start, "ExecutionOutcome"); customJob = AccessTools.Field(start, "CustomJobDef");
            unregistered = AccessTools.Field(start, "DoNotRegisterPawns"); flipX = AccessTools.Field(start, "FlipX"); flipY = AccessTools.Field(start, "FlipY");
            grappledPawn = AccessTools.Property(driver, "GrappledPawn");
            MethodInfo exposeDriver = AccessTools.Method(driver, "ExposeData");
            if (new object[] { startField, main, second, extra, map, outcome, customJob, unregistered, grappledPawn, exposeDriver }.Any(x => x == null)) Log.Warning("[TMM] Melee Animation: the lasso job's save data was not found.");
            else harmony.Patch(exposeDriver, postfix: new HarmonyMethod(me, "ExposeFollowUp"));
            hasStarted = AccessTools.Field(renderer, "hasStarted");
            addFromPostLoad = AccessTools.Method(renderer, "AddFromPostLoad");
            MethodInfo onStart = AccessTools.Method(renderer, "OnStart"), exposeManager = AccessTools.Method(manager, "ExposeData");
            if (hasStarted == null || addFromPostLoad == null || onStart == null || exposeManager == null) { Log.Warning("[TMM] Melee Animation: the running animations' save data was not found."); return; }
            harmony.Patch(addFromPostLoad, prefix: new HarmonyMethod(me, "ResumeStart"), finalizer: new HarmonyMethod(me, "ResumeEnd"));
            harmony.Patch(onStart, prefix: new HarmonyMethod(me, "OnStart"));
            harmony.Patch(exposeManager, postfix: new HarmonyMethod(me, "ManagerLoaded"));
        }

        private static Matrix4x4 ParseMatrix(string text)
        {
            string[] parts = text.Split(new[] { '[', ']', ';', ',', '\t', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 16) throw new FormatException("Not a 4x4 matrix: " + text);
            Matrix4x4 matrix = default(Matrix4x4);
            for (int i = 0; i < 16; i++) matrix[i / 4, i % 4] = float.Parse(parts[i], CultureInfo.InvariantCulture);
            return matrix;
        }

        private static void ExposeFollowUp(JobDriver __instance)
        {
            object start = startField.GetValue(__instance);   // a boxed copy of the struct, null when nothing follows the pull
            if (start == null) return;
            Pawn first = null, other = null; List<Pawn> more = null; Map where = null; JobDef job = null; int result = -1; bool loose = false;
            bool saving = Scribe.mode == LoadSaveMode.Saving;
            if (saving)
            {
                first = (Pawn)main.GetValue(start); other = (Pawn)second.GetValue(start); more = (List<Pawn>)extra.GetValue(start);
                where = (Map)map.GetValue(start); job = (JobDef)customJob.GetValue(start); result = Convert.ToInt32(outcome.GetValue(start)); loose = (bool)unregistered.GetValue(start);
            }
            Scribe_References.Look(ref first, "TMM_animMain");
            Scribe_References.Look(ref other, "TMM_animSecond");
            Scribe_Collections.Look(ref more, "TMM_animExtra", LookMode.Reference);
            Scribe_References.Look(ref where, "TMM_animMap");
            Scribe_Defs.Look(ref job, "TMM_animJob");
            Scribe_Values.Look(ref result, "TMM_animOutcome", -1);
            Scribe_Values.Look(ref loose, "TMM_animUnregistered");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Saves made without these fields get the default outcome of the original.
                outcome.SetValue(start, Enum.ToObject(outcome.FieldType, result < 0 ? 4 : result));
                customJob.SetValue(start, job);
                unregistered.SetValue(start, loose);
                startField.SetValue(__instance, start);
            }
            else if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                main.SetValue(start, first); second.SetValue(start, other); extra.SetValue(start, more); map.SetValue(start, where);
                startField.SetValue(__instance, start);
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (main.GetValue(start) == null) main.SetValue(start, __instance.pawn);
                if (second.GetValue(start) == null && __instance.job != null) second.SetValue(start, __instance.job.targetB.Pawn ?? __instance.job.targetA.Pawn);
                if (map.GetValue(start) == null) map.SetValue(start, __instance.pawn.MapHeld);
                startField.SetValue(__instance, start);
            }
        }

        private static void ManagerLoaded()
        {
            // Before the first tick: a pawn whose animation is not back yet ends its animation job.
            if (Scribe.mode == LoadSaveMode.PostLoadInit) LongEventHandler.ExecuteWhenFinished(() => addFromPostLoad.Invoke(null, null));
        }
        private static void ResumeStart() { resuming = true; }
        private static Exception ResumeEnd(Exception __exception) { resuming = false; return __exception; }
        private static bool OnStart(object __instance)
        {
            // A resumed animation has started already: its pawns stand where they stood and have their job from the save.
            return !resuming || !(bool)hasStarted.GetValue(__instance);
        }
    }
}