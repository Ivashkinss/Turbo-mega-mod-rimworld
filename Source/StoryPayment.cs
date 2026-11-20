using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Shared atomic resource counting and deduction helper for StoryStage_Deliver and OutpostPodLogistics.
    ///
    /// EXPLICIT NESTED-ITEMS &amp; ELIGIBILITY POLICY:
    /// 1) Colony Map mode:
    ///    - ONLY counts/deducts items directly spawned on the pinned Map, unforbidden, and located inside
    ///      a valid player storage zone (Zone_Stockpile) or player-owned storage building (Building_Storage).
    ///    - Loose items outside storage, items inside pawns' inventories, drop pods, graves, or other nested
    ///      IThingHolders are EXCLUDED.
    /// 2) Caravan mode:
    ///    - ONLY counts/deducts items directly held in inventory.innerContainer of living pawns belonging
    ///      to that specific player Caravan.
    ///    - Equipped weapons, worn apparel, and items nested inside other items are EXCLUDED.
    ///
    /// ATOMICITY GUARANTEE:
    /// All required stacks are collected and summed first, then each requested amount is split into a
    /// temporary IThingHolder with strict verification of every SplitOff result. If ANY split returns null,
    /// returns the wrong count, fails TryAdd, or throws an exception, EVERY previously separated chunk is
    /// rolled back to its original stack/cell (on map) or original pawn inventory (in caravan), leaving
    /// inventory completely unchanged. Only after the full amount is confirmed in the staging holder
    /// are the staged chunks destroyed.
    /// </summary>
    public static class StoryPayment
    {
        /// <summary>
        /// Test-only hook allowing in-game probes to simulate a mid-transaction SplitOff failure
        /// (e.g. returning null or throwing on the second stack). Always null in normal play.
        /// Parameters: (sourceStack, requestedCount, zeroBasedStepIndex) -> split Thing.
        /// </summary>
        public static Func<Thing, int, int, Thing> TestSplitOffOverride;

        private sealed class StagingHolder : IThingHolder
        {
            public ThingOwner<Thing> innerContainer;

            public StagingHolder()
            {
                innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
            }

            public IThingHolder ParentHolder
            {
                get { return null; }
            }

            public void GetChildHolders(List<IThingHolder> outChildren)
            {
            }

            public ThingOwner GetDirectlyHeldThings()
            {
                return innerContainer;
            }
        }

        private sealed class ReservedChunk
        {
            public Thing originalStack;
            public Thing stagedChunk;
            public Map origMap;
            public IntVec3 origPos;
            public Pawn origPawn;
            public bool tookWhole;
        }

        public sealed class ResourceRequirement
        {
            public ThingDef def;
            public int count;

            public ResourceRequirement(ThingDef def, int count)
            {
                this.def = def;
                this.count = count;
            }
        }

        // =====================================================================
        // COLONY STORAGE COUNTING & ATOMIC DEDUCTION
        // =====================================================================

        /// <summary>
        /// Returns true iff the thing is directly spawned on map, unforbidden, and sits inside a
        /// player-owned stockpile zone or player-owned storage building.
        /// </summary>
        public static bool IsInPlayerStorage(Map map, Thing t)
        {
            if (map == null || t == null || t.Destroyed || !t.Spawned || t.Map != map) return false;
            if (t.stackCount <= 0 || t.IsForbidden(Faction.OfPlayer)) return false;
            if (map.haulDestinationManager == null) return false;

            SlotGroup sg = map.haulDestinationManager.SlotGroupAt(t.Position); // CHECK API
            if (sg == null || sg.parent == null) return false;

            if (sg.parent is Zone_Stockpile) return true;

            Building b = sg.parent as Building;
            if (b != null && b.Faction == Faction.OfPlayer) return true;

            return false;
        }

        public static int CountInColonyStockpiles(Map map, ThingDef def)
        {
            if (map == null || def == null || map.listerThings == null) return 0;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (IsInPlayerStorage(map, t))
                {
                    total += t.stackCount;
                }
            }
            return total;
        }

        public static bool TryDeductFromColonyStockpiles(Map map, ThingDef def, int requiredCount, out int deducted)
        {
            deducted = 0;
            if (map == null || def == null || requiredCount <= 0) return false;

            List<ResourceRequirement> reqs = new List<ResourceRequirement>();
            reqs.Add(new ResourceRequirement(def, requiredCount));
            if (TryDeductMultipleFromColonyStockpiles(map, reqs))
            {
                deducted = requiredCount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Atomically deducts multiple resource requirements from player storage on a single pinned Map.
        /// If any requirement lacks stock or any SplitOff fails, EVERY earlier chunk is rolled back.
        /// </summary>
        public static bool TryDeductMultipleFromColonyStockpiles(Map map, List<ResourceRequirement> requirements)
        {
            if (map == null || map.listerThings == null || requirements == null || requirements.Count == 0) return false;

            // Step 1: Collect eligible stacks and verify complete sums BEFORE mutating anything
            List<List<Thing>> plans = new List<List<Thing>>();
            for (int r = 0; r < requirements.Count; r++)
            {
                ResourceRequirement req = requirements[r];
                if (req == null || req.def == null || req.count <= 0)
                {
                    plans.Add(new List<Thing>());
                    continue;
                }

                List<Thing> eligible = new List<Thing>();
                List<Thing> raw = map.listerThings.ThingsOfDef(req.def);
                int sum = 0;
                for (int i = 0; i < raw.Count; i++)
                {
                    Thing t = raw[i];
                    if (IsInPlayerStorage(map, t))
                    {
                        eligible.Add(t);
                        sum += t.stackCount;
                    }
                }

                if (sum < req.count) return false;
                plans.Add(eligible);
            }

            // Step 2: Stage all requested chunks in a temporary holder, checking EVERY SplitOff
            StagingHolder staging = new StagingHolder();
            List<ReservedChunk> reserved = new List<ReservedChunk>();
            int stepIndex = 0;
            bool ok = true;

            try
            {
                for (int r = 0; r < requirements.Count && ok; r++)
                {
                    ResourceRequirement req = requirements[r];
                    if (req == null || req.def == null || req.count <= 0) continue;

                    int remaining = req.count;
                    List<Thing> stacks = plans[r];

                    for (int i = 0; i < stacks.Count && remaining > 0; i++)
                    {
                        Thing src = stacks[i];
                        if (!IsInPlayerStorage(map, src))
                        {
                            ok = false;
                            break;
                        }

                        int take = Mathf.Min(remaining, src.stackCount);
                        bool takingWhole = (take == src.stackCount);
                        IntVec3 pos = src.Position;

                        Thing split = ExecuteSplitOff(src, take, stepIndex);
                        stepIndex++;

                        if (split == null || split.Destroyed || split.stackCount != take)
                        {
                            // Put back partial unexpected split if any
                            if (split != null && !split.Destroyed && split != src)
                            {
                                ReturnChunkToMap(split, src, map, pos);
                            }
                            ok = false;
                            break;
                        }

                        if (!staging.innerContainer.TryAdd(split, false))
                        {
                            ReturnChunkToMap(split, takingWhole ? null : src, map, pos);
                            ok = false;
                            break;
                        }

                        ReservedChunk rc = new ReservedChunk();
                        rc.originalStack = takingWhole ? null : src;
                        rc.stagedChunk = split;
                        rc.origMap = map;
                        rc.origPos = pos;
                        rc.tookWhole = takingWhole;
                        reserved.Add(rc);

                        remaining -= split.stackCount;
                    }

                    if (remaining > 0) ok = false;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryPayment: colony deduction failed mid-transaction, rolling back: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                RollbackMapChunks(staging, reserved);
                return false;
            }

            // Step 3: Commit — destroy staged chunks only after 100% reservation succeeded
            DestroyStaged(staging);
            return true;
        }

        // =====================================================================
        // CARAVAN DIRECT-INVENTORY COUNTING & ATOMIC DEDUCTION
        // =====================================================================

        /// <summary>
        /// Counts items matching def directly inside living caravan pawns' inventory.innerContainer.
        /// Equipped weapons, worn apparel, and nested items are excluded.
        /// </summary>
        public static int CountInCaravanDirectInventory(Caravan caravan, ThingDef def)
        {
            if (caravan == null || caravan.Destroyed || def == null) return 0;
            List<Pawn> pawns = caravan.PawnsListForReading;
            int total = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed && t.def == def && t.stackCount > 0)
                    {
                        total += t.stackCount;
                    }
                }
            }
            return total;
        }

        /// <summary>
        /// Atomically deducts requiredCount of def from the direct inventories of living pawns
        /// belonging to a specific player Caravan. Rolls back every chunk on any failure.
        /// </summary>
        public static bool TryDeductFromCaravan(Caravan caravan, ThingDef def, int requiredCount, out int deducted)
        {
            deducted = 0;
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer || def == null || requiredCount <= 0)
            {
                return false;
            }

            // Step 1: Collect (pawn, stack) pairs and verify full sum first
            List<Pawn> owners = new List<Pawn>();
            List<Thing> stacks = new List<Thing>();
            int total = 0;

            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed && t.def == def && t.stackCount > 0)
                    {
                        owners.Add(p);
                        stacks.Add(t);
                        total += t.stackCount;
                    }
                }
            }

            if (total < requiredCount) return false;

            // Step 2: Stage chunks with strict SplitOff validation
            StagingHolder staging = new StagingHolder();
            List<ReservedChunk> reserved = new List<ReservedChunk>();
            int remaining = requiredCount;
            int stepIndex = 0;
            bool ok = true;

            try
            {
                for (int i = 0; i < stacks.Count && remaining > 0; i++)
                {
                    Pawn p = owners[i];
                    Thing src = stacks[i];
                    if (p == null || p.Dead || p.inventory == null || p.inventory.innerContainer == null ||
                        src == null || src.Destroyed || src.stackCount <= 0 || !p.inventory.innerContainer.Contains(src))
                    {
                        ok = false;
                        break;
                    }

                    int take = Mathf.Min(remaining, src.stackCount);
                    bool takingWhole = (take == src.stackCount);

                    Thing split = ExecuteSplitOff(src, take, stepIndex);
                    stepIndex++;

                    if (split == null || split.Destroyed || split.stackCount != take)
                    {
                        if (split != null && !split.Destroyed && split != src)
                        {
                            ReturnChunkToPawn(split, src, p);
                        }
                        ok = false;
                        break;
                    }

                    // Ensure split is removed from pawn inventory before adding to staging
                    if (split.holdingOwner != null && split.holdingOwner != staging.innerContainer)
                    {
                        split.holdingOwner.Remove(split);
                    }

                    if (!staging.innerContainer.TryAdd(split, false))
                    {
                        ReturnChunkToPawn(split, takingWhole ? null : src, p);
                        ok = false;
                        break;
                    }

                    ReservedChunk rc = new ReservedChunk();
                    rc.originalStack = takingWhole ? null : src;
                    rc.stagedChunk = split;
                    rc.origPawn = p;
                    rc.tookWhole = takingWhole;
                    reserved.Add(rc);

                    remaining -= split.stackCount;
                }

                if (remaining > 0) ok = false;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryPayment: caravan deduction failed mid-transaction, rolling back: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                RollbackCaravanChunks(staging, reserved);
                return false;
            }

            // Step 3: Commit
            DestroyStaged(staging);
            deducted = requiredCount;
            return true;
        }

        // =====================================================================
        // INTERNAL SPLIT & ROLLBACK HELPERS
        // =====================================================================

        private static Thing ExecuteSplitOff(Thing src, int count, int stepIndex)
        {
            if (TestSplitOffOverride != null)
            {
                return TestSplitOffOverride(src, count, stepIndex);
            }
            return src.SplitOff(count);
        }

        private static void RollbackMapChunks(StagingHolder staging, List<ReservedChunk> reserved)
        {
            for (int i = reserved.Count - 1; i >= 0; i--)
            {
                ReservedChunk rc = reserved[i];
                if (rc == null || rc.stagedChunk == null || rc.stagedChunk.Destroyed) continue;
                if (staging.innerContainer.Contains(rc.stagedChunk))
                {
                    staging.innerContainer.Remove(rc.stagedChunk);
                }
                ReturnChunkToMap(rc.stagedChunk, rc.originalStack, rc.origMap, rc.origPos);
            }
            staging.innerContainer.Clear();
        }

        private static void ReturnChunkToMap(Thing chunk, Thing originalStack, Map map, IntVec3 pos)
        {
            if (chunk == null || chunk.Destroyed) return;
            if (originalStack != null && !originalStack.Destroyed && originalStack.Spawned && originalStack.Map == map)
            {
                if (originalStack.TryAbsorbStack(chunk, false))
                {
                    return;
                }
            }
            if (map != null && pos.InBounds(map))
            {
                GenPlace.TryPlaceThing(chunk, pos, map, ThingPlaceMode.Near);
            }
        }

        private static void RollbackCaravanChunks(StagingHolder staging, List<ReservedChunk> reserved)
        {
            for (int i = reserved.Count - 1; i >= 0; i--)
            {
                ReservedChunk rc = reserved[i];
                if (rc == null || rc.stagedChunk == null || rc.stagedChunk.Destroyed) continue;
                if (staging.innerContainer.Contains(rc.stagedChunk))
                {
                    staging.innerContainer.Remove(rc.stagedChunk);
                }
                ReturnChunkToPawn(rc.stagedChunk, rc.originalStack, rc.origPawn);
            }
            staging.innerContainer.Clear();
        }

        private static void ReturnChunkToPawn(Thing chunk, Thing originalStack, Pawn pawn)
        {
            if (chunk == null || chunk.Destroyed) return;
            if (originalStack != null && !originalStack.Destroyed)
            {
                if (originalStack.TryAbsorbStack(chunk, false))
                {
                    return;
                }
            }
            if (pawn != null && pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                pawn.inventory.innerContainer.TryAdd(chunk, true);
            }
        }

        private static void DestroyStaged(StagingHolder staging)
        {
            if (staging == null || staging.innerContainer == null) return;
            staging.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
        }
    }
}
