using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// The integrated Useful Marks component keeps pawn-keyed dictionaries in a WorldComponent and saves them as-is.
    /// Pawns that were destroyed or discarded by world-pawn GC stay as keys, are saved as dangling references and
    /// produce "Null key while loading dictionary" errors on load. Before saving we drop such keys.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class UsefulMarksSaveHygiene
    {
        private static readonly string[] DictionaryFields = { "PawnMarkLabelColors", "PawnMarkLabelInWorld", "UserAssignedMarkers", "solveForCache" };
        private static FieldInfo[] fields;

        static UsefulMarksSaveHygiene()
        {
            Type t = AccessTools.TypeByName("UsefulMarks.PawnLabelCustomColors_WorldComponent");
            if (t == null) return;
            MethodInfo expose = AccessTools.Method(t, "ExposeData");
            if (expose == null) return;
            List<FieldInfo> found = new List<FieldInfo>();
            for (int i = 0; i < DictionaryFields.Length; i++)
            {
                FieldInfo f = AccessTools.Field(t, DictionaryFields[i]);
                if (f != null) found.Add(f);
            }
            fields = found.ToArray();
            new Harmony("colonyproject.integrated.usefulmarks.savehygiene").Patch(expose, prefix: new HarmonyMethod(typeof(UsefulMarksSaveHygiene), "Prefix"));
        }

        public static void Prefix(object __instance)
        {
            if (Scribe.mode != LoadSaveMode.Saving || fields == null || __instance == null) return;
            try
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    IDictionary dict = fields[i].GetValue(__instance) as IDictionary;
                    if (dict == null || dict.Count == 0) continue;
                    List<object> dead = new List<object>();
                    foreach (object key in dict.Keys)
                    {
                        Pawn p = key as Pawn;
                        if (p == null || p.Discarded || p.Destroyed) dead.Add(key);
                    }
                    for (int j = 0; j < dead.Count; j++) dict.Remove(dead[j]);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] Useful Marks save hygiene skipped: " + ex.Message);
            }
        }
    }
}
