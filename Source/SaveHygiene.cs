using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Before every save (manual and autosave go through GameDataSaveLoader.SaveGame) drop family relations that point
    /// at pawns already discarded by the world pawn GC or destroyed. Such relations are saved as references to objects
    /// that are not saved, and the game then logs "Could not resolve reference" on load. This can happen to colonists
    /// and heroes whose relatives the GC removed, so it is fixed here for every pawn, not only ours.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class SaveHygiene
    {
        static SaveHygiene()
        {
            var save = AccessTools.Method(typeof(GameDataSaveLoader), "SaveGame", new Type[] { typeof(string) });
            if (save == null) { Log.Warning("[TMM] SaveHygiene: GameDataSaveLoader.SaveGame(string) not found; dangling relations are not cleaned before saving."); return; }
            new Harmony("colonyproject.integrated.savehygiene").Patch(save, prefix: new HarmonyMethod(typeof(SaveHygiene), "Prefix_SaveGame"));
        }

        public static void Prefix_SaveGame()
        {
            try { CleanDanglingRelations(); }
            catch (Exception ex) { Log.Warning("[TMM] SaveHygiene skipped: " + ex.Message); }
        }

        /// Returns how many relations were removed. A relation is dangling when the other pawn will not be written
        /// to the save: it is not on a map, not a world pawn and not held anywhere in the world (caravans, outposts,
        /// transporters, corpses and other holders are all included).
        public static int CleanDanglingRelations()
        {
            if (Current.Game == null || Find.World == null) return 0;
            HashSet<Pawn> saved = new HashSet<Pawn>();
            foreach (Pawn p in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead) if (p != null) saved.Add(p);
            if (Find.WorldPawns != null) foreach (Pawn p in Find.WorldPawns.AllPawnsAliveOrDead) if (p != null) saved.Add(p);
            List<Thing> held = new List<Thing>();
            ThingOwnerUtility.GetAllThingsRecursively(Find.World, held, true, null);
            foreach (Map m in Find.Maps) ThingOwnerUtility.GetAllThingsRecursively(m, held, true, null);
            for (int i = 0; i < held.Count; i++)
            {
                Pawn hp = held[i] as Pawn;
                if (hp != null) { saved.Add(hp); continue; }
                Corpse c = held[i] as Corpse;
                if (c != null && c.InnerPawn != null) saved.Add(c.InnerPawn);
            }
            int removed = 0;
            List<DirectPawnRelation> dead = new List<DirectPawnRelation>();
            foreach (Pawn p in saved)
            {
                if (p.relations == null || p.Discarded) continue;
                List<DirectPawnRelation> rels = p.relations.DirectRelations;
                dead.Clear();
                for (int j = 0; j < rels.Count; j++)
                {
                    DirectPawnRelation r = rels[j];
                    if (r != null && (r.otherPawn == null || r.otherPawn.Discarded || !saved.Contains(r.otherPawn))) dead.Add(r);
                }
                for (int j = 0; j < dead.Count; j++)
                {
                    try { p.relations.RemoveDirectRelation(dead[j]); }
                    catch { rels.Remove(dead[j]); }
                    removed++;
                }
            }
            if (removed > 0) Log.Message("[TMM] SaveHygiene: removed " + removed + " relation(s) to pawns that are not saved.");
            return removed;
        }
    }
}
