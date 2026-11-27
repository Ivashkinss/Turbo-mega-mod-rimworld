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
            var harmony = new Harmony("colonyproject.integrated.savehygiene");
            harmony.Patch(save, prefix: new HarmonyMethod(typeof(SaveHygiene), "Prefix_SaveGame"));
            // Archived letters point at caravans and sites that are long gone: vanilla writes the reference, the object is not saved.
            harmony.Patch(AccessTools.Method(typeof(LookTargets), "ExposeData"), prefix: new HarmonyMethod(typeof(SaveHygiene), "KeepTileOfGoneObjects"));
            // Vanilla drops a generation attempt "incapable of required skill" without discarding it, after it has already become
            // somebody's parent or spouse. Every other failed attempt is discarded; this one stayed as a relation to an unsaved pawn.
            var attempt = AccessTools.Method(typeof(PawnGenerator), "TryGenerateNewPawnInternal");
            var relate = AccessTools.Method(typeof(PawnGenerator), "GeneratePawnRelations");
            if (attempt == null || relate == null) Log.Warning("[TMM] SaveHygiene: PawnGenerator.TryGenerateNewPawnInternal not found; dropped generation attempts keep their relatives.");
            else
            {
                harmony.Patch(attempt, prefix: new HarmonyMethod(typeof(SaveHygiene), "AttemptStart") { priority = Priority.First }, finalizer: new HarmonyMethod(typeof(SaveHygiene), "AttemptEnd"));
                harmony.Patch(relate, prefix: new HarmonyMethod(typeof(SaveHygiene), "AttemptPawn") { priority = Priority.First });
            }
            // The source of the broken references: vanilla invents the author of a generated book (settlement and caravan stock)
            // as a full pawn with relatives, then neither saves nor discards that pawn. An invented author gets no relatives.
            var invent = AccessTools.Method(typeof(TaleData_Pawn), "GenerateRandom");
            var generate = AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) });
            if (invent == null || generate == null) { Log.Warning("[TMM] SaveHygiene: TaleData_Pawn.GenerateRandom not found; invented book authors keep their relatives."); return; }
            harmony.Patch(invent, prefix: new HarmonyMethod(typeof(SaveHygiene), "InventStart"), finalizer: new HarmonyMethod(typeof(SaveHygiene), "InventEnd"));
            harmony.Patch(generate, prefix: new HarmonyMethod(typeof(SaveHygiene), "InventedPawn") { priority = Priority.First });
        }

        private static void KeepTileOfGoneObjects(LookTargets __instance)
        {
            if (Scribe.mode != LoadSaveMode.Saving || __instance.targets == null) return;
            for (int i = 0; i < __instance.targets.Count; i++)
            {
                RimWorld.Planet.GlobalTargetInfo target = __instance.targets[i];
                RimWorld.Planet.WorldObject gone = target.HasWorldObject ? target.WorldObject : null;
                // A site may wait unspawned inside a quest that saves it; a caravan never does.
                if (gone != null && (gone.Destroyed || gone is RimWorld.Planet.Caravan && !gone.Spawned)) __instance.targets[i] = new RimWorld.Planet.GlobalTargetInfo(target.WorldObject.Tile);
            }
        }

        private static readonly Stack<Pawn> attempts = new Stack<Pawn>();
        public static int DroppedAttempts;
        private static void AttemptStart(out bool __state) { attempts.Push(null); __state = true; }
        private static void AttemptPawn(Pawn pawn)
        {
            if (attempts.Count == 0) return;
            attempts.Pop();
            attempts.Push(pawn);
        }
        private static Exception AttemptEnd(Exception __exception, Pawn __result, bool __state)
        {
            Pawn pawn = __state && attempts.Count > 0 ? attempts.Pop() : null;
            if (__exception == null && __result == null && pawn != null && !pawn.Destroyed && !pawn.Discarded && !pawn.Spawned && pawn.ParentHolder == null && !Find.WorldPawns.Contains(pawn))
            {
                DroppedAttempts++;
                Find.WorldPawns.PassToWorld(pawn, RimWorld.Planet.PawnDiscardDecideMode.Discard);
            }
            return __exception;
        }

        private static int inventing;
        private static void InventStart() { inventing++; }
        private static Exception InventEnd(Exception __exception) { inventing--; return __exception; }
        private static void InventedPawn(ref PawnGenerationRequest request)
        {
            if (inventing > 0) request.CanGeneratePawnRelations = false;
        }

        public static void Prefix_SaveGame()
        {
            try { CleanDanglingRelations(); }
            catch (Exception ex) { Log.Warning("[TMM] SaveHygiene skipped: " + ex.Message); }
        }

        /// Gets rid of a pawn that was generated but is not going to be used, the way the game's own generator does. Destroy alone
        /// leaves the relations other pawns got to it, and they would be saved as references to an unsaved pawn. A named hero is kept.
        public static void DiscardUnused(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return;
            if (pawn.Spawned) pawn.DeSpawn(DestroyMode.Vanish);
            if (pawn.holdingOwner != null) pawn.holdingOwner.Remove(pawn);
            if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
            Find.WorldPawns.PassToWorld(pawn, RimWorld.Planet.PawnDiscardDecideMode.Discard);
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
            if (Find.QuestManager != null)
            {
                foreach (Quest q in Find.QuestManager.QuestsListForReading)
                {
                    if (q == null || q.PartsListForReading == null) continue;
                    foreach (QuestPart qp in q.PartsListForReading)
                    {
                        QuestPart_DropPods dp = qp as QuestPart_DropPods;
                        if (dp != null && dp.Things != null)
                        {
                            foreach (Thing t in dp.Things)
                            {
                                Pawn qpawn = t as Pawn;
                                if (qpawn != null) saved.Add(qpawn);
                            }
                        }
                    }
                }
            }
            // Anomaly keeps pawns of a closed pocket map in a plain list until they return; vanilla saves that list deeply.
            foreach (Pawn p in AnomalyReturnees()) saved.Add(p);
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
                    if (r == null) continue;
                    Pawn other = r.otherPawn;
                    if (other == null || other.Discarded) { dead.Add(r); continue; }
                    if (saved.Contains(other)) continue;
                    // Not found in the traversal is not proof that the pawn is unsaved: quest parts and other mods
                    // can save a pawn deeply without any holder. Only null and Discarded are removed (Codex, 22.11).
                    if (Prefs.DevMode)
                    {
                        Log.Message("[TMM] SaveHygiene: relation of " + p.ThingID + " " + p.LabelShort + " to " + Describe(other) + " not seen in known holders (kept).");
                    }
                }
                for (int j = 0; j < dead.Count; j++)
                {
                    try { p.relations.RemoveDirectRelation(dead[j]); removed++; }
                    catch (Exception ex) { Log.Warning("[TMM] SaveHygiene: could not remove a relation of " + p.LabelShort + ": " + ex.Message); }
                }
            }
            removed += CleanRelationshipRecords(saved);
            if (removed > 0) Log.Message("[TMM] SaveHygiene: removed " + removed + " relation(s) to pawns that are not saved.");
            return removed;
        }

        private static string Describe(Pawn p)
        {
            return p.ThingID + " " + p.LabelShort + " (" + (p.kindDef != null ? p.kindDef.defName : "?") + ", " + (p.Faction != null ? p.Faction.def.defName : "no faction") + ", destroyed=" + p.Destroyed + ", dead=" + p.Dead
                + ", holder=" + (p.ParentHolder != null ? p.ParentHolder.GetType().Name : "none") + ", relations=" + (p.relations != null ? p.relations.DirectRelations.Count : -1) + ")";
        }

        private static IEnumerable<Pawn> AnomalyReturnees()
        {
            System.Reflection.FieldInfo list = AccessTools.Field(typeof(GameComponent_Anomaly), "metalHellPawns");
            GameComponent_Anomaly comp = Current.Game == null ? null : Current.Game.GetComponent<GameComponent_Anomaly>();
            List<Pawn> pawns = list == null || comp == null ? null : list.GetValue(comp) as List<Pawn>;
            if (pawns == null) yield break;
            foreach (Pawn p in pawns) if (p != null) yield return p;
        }
        /// RimWorld 1.6+ keeps a world-level journal of relationships (Find.RelationshipRecords) that references pawns
        /// too. Drop references to pawns that will not be saved and let vanilla purge records left without references.
        /// Reflection keeps this compiling against older references.
        private static int CleanRelationshipRecords(HashSet<Pawn> saved)
        {
            int removed = 0;
            try
            {
                System.Reflection.PropertyInfo prop = AccessTools.Property(typeof(Find), "RelationshipRecords");
                object records = prop != null ? prop.GetValue(null, null) : null;
                if (records == null) return 0;
                System.Collections.IEnumerable dict = AccessTools.Property(records.GetType(), "Records").GetValue(records, null) as System.Collections.IEnumerable;
                if (dict == null) return 0;
                List<object> recs = new List<object>();
                foreach (object kv in dict) recs.Add(kv.GetType().GetProperty("Value").GetValue(kv, null));
                foreach (object rec in recs)
                {
                    if (rec == null) continue;
                    System.Collections.IEnumerable refs = AccessTools.Property(rec.GetType(), "References").GetValue(rec, null) as System.Collections.IEnumerable;
                    if (refs == null) continue;
                    List<Pawn> gone = new List<Pawn>();
                    foreach (object o in refs)
                    {
                        Pawn rp = o as Pawn;
                        if (rp == null) continue;
                        if (rp.Discarded) { gone.Add(rp); continue; }
                        if (saved.Contains(rp)) continue;

                        if (Prefs.DevMode) Log.Message("[TMM] SaveHygiene: journal reference to " + Describe(rp) + " not seen in known holders (kept).");
                    }
                    System.Reflection.MethodInfo remove = AccessTools.Method(rec.GetType(), "RemoveReference");
                    foreach (Pawn g in gone) { remove.Invoke(rec, new object[] { g }); removed++; }
                }
            }
            catch (Exception ex) { Log.Warning("[TMM] SaveHygiene: relationship records skipped: " + ex.Message); }
            return removed;
        }
    }
}
