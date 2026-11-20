using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Shared journaled, atomic resource payment and transfer engine for StoryStage_Deliver,
    /// OutpostPodLogistics, and OutpostCaravanLogistics.
    ///
    /// ELIGIBILITY POLICIES:
    /// 1) Colony Storage mode (requirePlayerStorage = true):
    ///    - ONLY counts/deducts items directly spawned on the pinned Map, unforbidden, and inside a
    ///      player Zone_Stockpile or player-owned storage building. Nested holders/pawns excluded.
    /// 2) Map Unforbidden mode (requirePlayerStorage = false, used by OutpostPodLogistics):
    ///    - Counts/deducts unforbidden items directly spawned on the pinned Map (excluding nested
    ///      pawns/containers) with the exact same atomic journaled StagingHolder.
    /// 3) Caravan Direct-Inventory mode:
    ///    - ONLY counts/deducts/transfers items directly inside living pawns' inventory.innerContainer
    ///      for that specific player Caravan. Equipped weapons, apparel, and nested holders excluded.
    ///
    /// ATOMICITY &amp; JOURNAL GUARANTEE:
    /// - Every mutation (SplitOff, Remove, TryAdd) is recorded in the operation journal BEFORE the call.
    /// - On any failure or exception, the journal rolls back in strict reverse order, verifying EVERY
    ///   TryAdd / GenPlace call and falling back to an available holder (nearby map cell, other pawn
    ///   in the same caravan, or home map) so no item is ever left ownerless.
    /// - Full staged quantities are independently summed and verified BEFORE the irreversible phase.
    /// - Optional onPaymentSecured callback runs AFTER full staging confirmation and BEFORE Destroy:
    ///   if onPaymentSecured throws, 100% of items roll back cleanly; once it succeeds, each staged
    ///   chunk is destroyed in an isolated try/catch so a Destroy exception never leaves partial loss
    ///   without recording the payment.
    /// - All test hooks are strictly gated behind TestMode.Enabled (-tmm-story-test).
    /// </summary>
    public static class StoryPayment
    {
        /// <summary>
        /// Test-only hook (ONLY active when TestMode.Enabled) to override SplitOff during probes.
        /// Parameters: (sourceStack, requestedCount, zeroBasedStepIndex) -> split Thing.
        /// </summary>
        public static Func<Thing, int, int, Thing> TestSplitOffOverride;

        /// <summary>
        /// Test-only hook (ONLY active when TestMode.Enabled) to simulate a holder refusing TryAdd
        /// during rollback or target transfer. Parameters: (targetHolder, item) -> true to force reject.
        /// </summary>
        public static Func<ThingOwner, Thing, bool> TestRejectTryAddOverride;

        /// <summary>
        /// Test-only hook (ONLY active when TestMode.Enabled) invoked before destroying a staged chunk
        /// during commit. Can throw to test commit-exception safety.
        /// </summary>
        public static Action<Thing, int> TestBeforeDestroyOverride;

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

        private sealed class JournalEntry
        {
            public Thing sourceStack;
            public int origStackCount;
            public int requestedTake;
            public bool tookWhole;
            public Thing splitChunk;
            public bool movedToStaging;
            public bool movedToTarget;
            public Map origMap;
            public IntVec3 origPos;
            public Pawn origPawn;
            public Caravan origCaravan;
            public ThingOwner targetOwner;
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
        // MAP / COLONY STORAGE COUNTING & ATOMIC DEDUCTION
        // =====================================================================

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

        public static bool IsEligibleMapItem(Map map, Thing t, bool requirePlayerStorage)
        {
            if (requirePlayerStorage) return IsInPlayerStorage(map, t);
            if (map == null || t == null || t.Destroyed || !t.Spawned || t.Map != map) return false;
            return t.stackCount > 0 && !t.IsForbidden(Faction.OfPlayer);
        }

        public static int CountInColonyStockpiles(Map map, ThingDef def)
        {
            return CountOnMap(map, def, true);
        }

        public static int CountOnMap(Map map, ThingDef def, bool requirePlayerStorage)
        {
            if (map == null || def == null || map.listerThings == null) return 0;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (IsEligibleMapItem(map, t, requirePlayerStorage))
                {
                    total += t.stackCount;
                }
            }
            return total;
        }

        public static bool TryDeductFromColonyStockpiles(Map map, ThingDef def, int requiredCount, out int deducted)
        {
            return TryDeductFromColonyStockpiles(map, def, requiredCount, out deducted, null);
        }

        public static bool TryDeductFromColonyStockpiles(Map map, ThingDef def, int requiredCount, out int deducted, Action onPaymentSecured)
        {
            return TryDeductFromMap(map, def, requiredCount, true, out deducted, onPaymentSecured);
        }

        public static bool TryDeductFromMap(Map map, ThingDef def, int requiredCount, bool requirePlayerStorage, out int deducted, Action onPaymentSecured = null)
        {
            deducted = 0;
            if (map == null || def == null || requiredCount <= 0) return false;

            List<ResourceRequirement> reqs = new List<ResourceRequirement>();
            reqs.Add(new ResourceRequirement(def, requiredCount));
            if (TryDeductMultipleFromMap(map, reqs, requirePlayerStorage, onPaymentSecured))
            {
                deducted = requiredCount;
                return true;
            }
            return false;
        }

        public static bool TryDeductMultipleFromColonyStockpiles(Map map, List<ResourceRequirement> requirements)
        {
            return TryDeductMultipleFromMap(map, requirements, true, null);
        }

        public static bool TryDeductMultipleFromColonyStockpiles(Map map, List<ResourceRequirement> requirements, Action onPaymentSecured)
        {
            return TryDeductMultipleFromMap(map, requirements, true, onPaymentSecured);
        }

        public static bool TryDeductMultipleFromMap(Map map, List<ResourceRequirement> requirements, bool requirePlayerStorage, Action onPaymentSecured = null)
        {
            if (map == null || map.listerThings == null || requirements == null || requirements.Count == 0) return false;

            // Step 1: Collect eligible stacks & aggregate required totals per ThingDef BEFORE mutating anything
            Dictionary<ThingDef, int> requiredByDef = new Dictionary<ThingDef, int>();
            List<ThingDef> defOrder = new List<ThingDef>();
            for (int r = 0; r < requirements.Count; r++)
            {
                ResourceRequirement req = requirements[r];
                if (req == null || req.def == null || req.count <= 0) continue;
                int prev;
                if (requiredByDef.TryGetValue(req.def, out prev))
                {
                    requiredByDef[req.def] = prev + req.count;
                }
                else
                {
                    requiredByDef[req.def] = req.count;
                    defOrder.Add(req.def);
                }
            }
            if (defOrder.Count == 0) return false;

            Dictionary<ThingDef, List<Thing>> plans = new Dictionary<ThingDef, List<Thing>>();
            for (int dIdx = 0; dIdx < defOrder.Count; dIdx++)
            {
                ThingDef def = defOrder[dIdx];
                int need = requiredByDef[def];
                List<Thing> raw = map.listerThings.ThingsOfDef(def);
                List<Thing> eligible = new List<Thing>();
                int sum = 0;
                for (int i = 0; i < raw.Count; i++)
                {
                    Thing t = raw[i];
                    if (IsEligibleMapItem(map, t, requirePlayerStorage))
                    {
                        eligible.Add(t);
                        sum += t.stackCount;
                    }
                }
                if (sum < need) return false;
                plans[def] = eligible;
            }

            // Step 2: Journal every SplitOff & TryAdd BEFORE executing it
            StagingHolder staging = new StagingHolder();
            List<JournalEntry> journal = new List<JournalEntry>();
            int stepIndex = 0;
            bool ok = true;

            try
            {
                for (int dIdx = 0; dIdx < defOrder.Count && ok; dIdx++)
                {
                    ThingDef def = defOrder[dIdx];
                    int remaining = requiredByDef[def];
                    List<Thing> stacks = plans[def];

                    for (int i = 0; i < stacks.Count && remaining > 0; i++)
                    {
                        Thing src = stacks[i];
                        if (!IsEligibleMapItem(map, src, requirePlayerStorage))
                        {
                            ok = false;
                            break;
                        }

                        int take = Mathf.Min(remaining, src.stackCount);
                        JournalEntry entry = new JournalEntry();
                        entry.sourceStack = src;
                        entry.origStackCount = src.stackCount;
                        entry.requestedTake = take;
                        entry.tookWhole = (take == src.stackCount);
                        entry.origMap = map;
                        entry.origPos = src.Position;
                        // Record BEFORE SplitOff so even a mid-SplitOff exception has a journal entry!
                        journal.Add(entry);

                        Thing split = ExecuteSplitOff(src, take, stepIndex);
                        stepIndex++;
                        entry.splitChunk = split;

                        if (split == null || split.Destroyed || split.stackCount != take)
                        {
                            ok = false;
                            break;
                        }

                        if (!TryAddToOwnerChecked(staging.innerContainer, split, false))
                        {
                            ok = false;
                            break;
                        }

                        entry.movedToStaging = true;
                        remaining -= split.stackCount;
                    }

                    if (remaining > 0) ok = false;
                }

                // Step 3: Independently verify complete staged totals BEFORE irreversible commit
                if (ok && !VerifyStagedTotals(staging, requiredByDef))
                {
                    ok = false;
                }

                // Step 4: Invoke caller commit hook BEFORE destroying anything
                // (if the hook throws, 100% of items are still intact in staging and roll back!)
                if (ok && onPaymentSecured != null)
                {
                    onPaymentSecured();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryPayment: map deduction failed before commit, rolling back: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                RollbackMapJournal(staging, journal);
                return false;
            }

            // Step 5: Irreversible destruction (isolated per chunk so an exception never invalidates a recorded payment)
            CommitDestroyStaged(staging);
            return true;
        }

        // =====================================================================
        // CARAVAN DIRECT-INVENTORY COUNTING, DEDUCTION & ATOMIC TRANSFER
        // =====================================================================

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

        public static bool TryDeductFromCaravan(Caravan caravan, ThingDef def, int requiredCount, out int deducted)
        {
            return TryDeductFromCaravan(caravan, def, requiredCount, out deducted, null);
        }

        public static bool TryDeductFromCaravan(Caravan caravan, ThingDef def, int requiredCount, out int deducted, Action onPaymentSecured)
        {
            deducted = 0;
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer || def == null || requiredCount <= 0)
            {
                return false;
            }

            // Step 1: Collect eligible (pawn, stack) pairs and verify sum BEFORE mutating anything
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

            // Step 2: Journal every mutation BEFORE executing it
            StagingHolder staging = new StagingHolder();
            List<JournalEntry> journal = new List<JournalEntry>();
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
                    JournalEntry entry = new JournalEntry();
                    entry.sourceStack = src;
                    entry.origStackCount = src.stackCount;
                    entry.requestedTake = take;
                    entry.tookWhole = (take == src.stackCount);
                    entry.origPawn = p;
                    entry.origCaravan = caravan;
                    journal.Add(entry);

                    Thing split = ExecuteSplitOff(src, take, stepIndex);
                    stepIndex++;
                    entry.splitChunk = split;

                    if (split == null || split.Destroyed || split.stackCount != take)
                    {
                        ok = false;
                        break;
                    }

                    if (split.holdingOwner != null && split.holdingOwner != staging.innerContainer)
                    {
                        split.holdingOwner.Remove(split);
                    }

                    if (!TryAddToOwnerChecked(staging.innerContainer, split, false))
                    {
                        ok = false;
                        break;
                    }

                    entry.movedToStaging = true;
                    remaining -= split.stackCount;
                }

                Dictionary<ThingDef, int> expected = new Dictionary<ThingDef, int>();
                expected[def] = requiredCount;
                if (remaining > 0 || (ok && !VerifyStagedTotals(staging, expected)))
                {
                    ok = false;
                }

                if (ok && onPaymentSecured != null)
                {
                    onPaymentSecured();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryPayment: caravan deduction failed before commit, rolling back: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                RollbackCaravanJournal(staging, journal);
                return false;
            }

            CommitDestroyStaged(staging);
            deducted = requiredCount;
            return true;
        }

        /// <summary>
        /// Atomically transfers exact requiredCount of def from living pawns in caravan directly into
        /// targetOwner (e.g., StrategicOutpost.RawMaterials), optionally enforcing per-pawn maximum
        /// take caps (keyed by pawn.thingIDNumber).
        /// Rolls back EVERY chunk (including chunks already placed into targetOwner) on any failure
        /// or exception, verifying every return TryAdd.
        /// </summary>
        public static bool TryTransferFromCaravanToOwner(
            Caravan caravan,
            ThingDef def,
            int requiredCount,
            ThingOwner targetOwner,
            Dictionary<int, int> maxTakeByPawnId,
            out int transferred,
            out string failReason,
            Action onTransferCommitted = null)
        {
            transferred = 0;
            failReason = null;

            if (requiredCount <= 0) return true;
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer || def == null || targetOwner == null)
            {
                failReason = "invalid_transfer_args";
                return false;
            }

            // Step 1: Collect eligible stacks respecting maxTakeByPawnId BEFORE mutating anything
            List<Pawn> pawns = caravan.PawnsListForReading;
            Dictionary<int, int> pawnAllowance = new Dictionary<int, int>();
            int totalEligible = 0;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;

                int have = 0;
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed && t.def == def && t.stackCount > 0) have += t.stackCount;
                }

                int cap = have;
                if (maxTakeByPawnId != null)
                {
                    int allowed;
                    cap = maxTakeByPawnId.TryGetValue(p.thingIDNumber, out allowed) ? Mathf.Min(have, Mathf.Max(0, allowed)) : 0;
                }

                if (cap > 0)
                {
                    pawnAllowance[p.thingIDNumber] = cap;
                    totalEligible += cap;
                }
            }

            if (totalEligible < requiredCount)
            {
                failReason = "insufficient_eligible_cargo";
                return false;
            }

            // Step 2: Stage chunks into StagingHolder FIRST (so targetOwner is untouched if SplitOff fails)
            StagingHolder staging = new StagingHolder();
            List<JournalEntry> journal = new List<JournalEntry>();
            int remaining = requiredCount;
            int stepIndex = 0;
            bool ok = true;

            try
            {
                for (int i = 0; i < pawns.Count && remaining > 0 && ok; i++)
                {
                    Pawn p = pawns[i];
                    if (p == null || p.Dead || p.inventory == null || p.inventory.innerContainer == null) continue;

                    int pawnLimit;
                    if (!pawnAllowance.TryGetValue(p.thingIDNumber, out pawnLimit) || pawnLimit <= 0) continue;

                    List<Thing> snapshot = new List<Thing>();
                    for (int j = 0; j < p.inventory.innerContainer.Count; j++)
                    {
                        Thing t = p.inventory.innerContainer[j];
                        if (t != null && !t.Destroyed && t.def == def && t.stackCount > 0) snapshot.Add(t);
                    }

                    for (int j = 0; j < snapshot.Count && remaining > 0 && pawnLimit > 0; j++)
                    {
                        Thing src = snapshot[j];
                        if (src == null || src.Destroyed || !p.inventory.innerContainer.Contains(src))
                        {
                            ok = false;
                            break;
                        }

                        int take = Mathf.Min(remaining, Mathf.Min(pawnLimit, src.stackCount));
                        JournalEntry entry = new JournalEntry();
                        entry.sourceStack = src;
                        entry.origStackCount = src.stackCount;
                        entry.requestedTake = take;
                        entry.tookWhole = (take == src.stackCount);
                        entry.origPawn = p;
                        entry.origCaravan = caravan;
                        entry.targetOwner = targetOwner;
                        journal.Add(entry);

                        Thing split = ExecuteSplitOff(src, take, stepIndex);
                        stepIndex++;
                        entry.splitChunk = split;

                        if (split == null || split.Destroyed || split.stackCount != take)
                        {
                            ok = false;
                            break;
                        }

                        if (split.holdingOwner != null && split.holdingOwner != staging.innerContainer)
                        {
                            split.holdingOwner.Remove(split);
                        }

                        if (!TryAddToOwnerChecked(staging.innerContainer, split, false))
                        {
                            ok = false;
                            break;
                        }

                        entry.movedToStaging = true;
                        remaining -= split.stackCount;
                        pawnLimit -= split.stackCount;
                    }
                }

                Dictionary<ThingDef, int> expected = new Dictionary<ThingDef, int>();
                expected[def] = requiredCount;
                if (remaining > 0 || (ok && !VerifyStagedTotals(staging, expected)))
                {
                    ok = false;
                }

                // Step 3: Move staged chunks into targetOwner with canMergeWithExistingStacks = false
                // so every chunk remains individually reversible until the entire transfer succeeds!
                if (ok)
                {
                    for (int i = 0; i < journal.Count; i++)
                    {
                        JournalEntry entry = journal[i];
                        Thing chunk = entry.splitChunk;
                        if (chunk == null || chunk.Destroyed || !staging.innerContainer.Contains(chunk))
                        {
                            ok = false;
                            break;
                        }

                        staging.innerContainer.Remove(chunk);
                        entry.movedToStaging = false;

                        if (!TryAddToOwnerChecked(targetOwner, chunk, false))
                        {
                            ok = false;
                            break;
                        }

                        entry.movedToTarget = true;
                    }
                }

                if (ok && onTransferCommitted != null)
                {
                    onTransferCommitted();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryPayment: caravan transfer failed, rolling back: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                RollbackCaravanJournal(staging, journal);
                failReason = "storage_transfer_failed";
                return false;
            }

            transferred = requiredCount;
            return true;
        }

        // =====================================================================
        // VERIFIED RETURN HELPERS (ZERO OWNERLESS ITEMS)
        // =====================================================================

        /// <summary>
        /// Returns a Thing to its original stack/cell on map, verifying every step and falling back
        /// to nearby standable cells, a colonist inventory on the map, or another player home map.
        /// </summary>
        public static bool TryReturnThingToMap(Thing chunk, Thing originalStack, Map map, IntVec3 pos)
        {
            if (chunk == null || chunk.Destroyed || chunk.stackCount <= 0) return true;

            if (chunk.holdingOwner != null)
            {
                chunk.holdingOwner.Remove(chunk);
            }

            // 1. Absorb back into original stack if still alive on map
            if (originalStack != null && originalStack != chunk && !originalStack.Destroyed &&
                originalStack.Spawned && originalStack.Map == map)
            {
                if (originalStack.TryAbsorbStack(chunk, false) && (chunk.Destroyed || chunk.stackCount <= 0))
                {
                    return true;
                }
            }

            // 2. Place at or near original cell on map
            if (map != null && Find.Maps != null && Find.Maps.Contains(map))
            {
                if (pos.IsValid && pos.InBounds(map))
                {
                    Thing placed;
                    if (GenPlace.TryPlaceThing(chunk, pos, map, ThingPlaceMode.Near, out placed) &&
                        (chunk.Destroyed || (placed != null && placed.Spawned && placed.Map == map)))
                    {
                        return true;
                    }
                }

                // 3. Fallback: trade drop spot / standable cell on same map
                IntVec3 fallbackCell = DropCellFinder.TradeDropSpot(map); // CHECK API
                if (!fallbackCell.IsValid || !fallbackCell.InBounds(map)) fallbackCell = map.Center;
                Thing placedFallback;
                if (GenPlace.TryPlaceThing(chunk, fallbackCell, map, ThingPlaceMode.Near, out placedFallback) &&
                    (chunk.Destroyed || (placedFallback != null && placedFallback.Spawned && placedFallback.Map == map)))
                {
                    return true;
                }

                // 4. Fallback: any living colonist inventory on same map
                if (map.mapPawns != null)
                {
                    IReadOnlyList<Pawn> cols = map.mapPawns.FreeColonistsSpawned;
                    for (int i = 0; i < cols.Count; i++)
                    {
                        Pawn c = cols[i];
                        if (c != null && !c.Dead && c.inventory != null && c.inventory.innerContainer != null)
                        {
                            if (TryAddToOwnerChecked(c.inventory.innerContainer, chunk, true))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // 5. Last resort: any active player home map
            if (Find.Maps != null)
            {
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map home = Find.Maps[i];
                    if (home == null || !home.IsPlayerHome || home == map) continue;
                    IntVec3 spot = DropCellFinder.TradeDropSpot(home);
                    if (!spot.IsValid || !spot.InBounds(home)) spot = home.Center;
                    Thing placedHome;
                    if (GenPlace.TryPlaceThing(chunk, spot, home, ThingPlaceMode.Near, out placedHome) &&
                        (chunk.Destroyed || (placedHome != null && placedHome.Spawned)))
                    {
                        return true;
                    }
                }
            }

            Log.Error("[TMM] StoryPayment: failed to return thing " + chunk.LabelShort + " to any map or holder during rollback!");
            return false;
        }

        /// <summary>
        /// Returns a Thing to its original pawn stack/inventory, verifying TryAdd and falling back
        /// to any other living pawn in the same caravan, then to an active map if needed.
        /// </summary>
        public static bool TryReturnThingToCaravan(Thing chunk, Thing originalStack, Pawn origPawn, Caravan caravan)
        {
            if (chunk == null || chunk.Destroyed || chunk.stackCount <= 0) return true;

            if (chunk.holdingOwner != null)
            {
                chunk.holdingOwner.Remove(chunk);
            }

            // 1. Absorb back into original stack inside origPawn's inventory
            if (originalStack != null && originalStack != chunk && !originalStack.Destroyed &&
                origPawn != null && !origPawn.Dead && origPawn.inventory != null &&
                origPawn.inventory.innerContainer != null && origPawn.inventory.innerContainer.Contains(originalStack))
            {
                if (originalStack.TryAbsorbStack(chunk, false) && (chunk.Destroyed || chunk.stackCount <= 0))
                {
                    return true;
                }
            }

            // 2. Add back to origPawn's inventory (verified)
            if (origPawn != null && !origPawn.Dead && origPawn.inventory != null && origPawn.inventory.innerContainer != null)
            {
                if (TryAddToOwnerChecked(origPawn.inventory.innerContainer, chunk, true))
                {
                    return true;
                }
            }

            // 3. Fallback: ANY other living pawn in the same caravan (verified)
            if (caravan != null && !caravan.Destroyed && caravan.PawnsListForReading != null)
            {
                List<Pawn> members = caravan.PawnsListForReading;
                for (int i = 0; i < members.Count; i++)
                {
                    Pawn other = members[i];
                    if (other == null || other == origPawn || other.Dead || other.Destroyed) continue;
                    if (other.inventory != null && other.inventory.innerContainer != null)
                    {
                        if (TryAddToOwnerChecked(other.inventory.innerContainer, chunk, true))
                        {
                            return true;
                        }
                    }
                }
            }

            // 4. Last resort: drop on any player home map so item is never orphaned
            Map home = Find.AnyPlayerHomeMap;
            if (home != null)
            {
                return TryReturnThingToMap(chunk, null, home, DropCellFinder.TradeDropSpot(home));
            }

            Log.Error("[TMM] StoryPayment: failed to return thing " + chunk.LabelShort + " to caravan or any fallback holder!");
            return false;
        }

        // =====================================================================
        // INTERNAL JOURNAL, VERIFICATION, ROLLBACK & COMMIT HELPERS
        // =====================================================================

        private static Thing ExecuteSplitOff(Thing src, int count, int stepIndex)
        {
            if (TestMode.Enabled && TestSplitOffOverride != null)
            {
                return TestSplitOffOverride(src, count, stepIndex);
            }
            return src.SplitOff(count);
        }

        private static bool TryAddToOwnerChecked(ThingOwner owner, Thing item, bool canMerge)
        {
            if (owner == null || item == null || item.Destroyed) return false;
            if (TestMode.Enabled && TestRejectTryAddOverride != null && TestRejectTryAddOverride(owner, item))
            {
                return false;
            }

            bool added = owner.TryAdd(item, canMerge);
            if (!added) return false;

            // When canMerge == true, TryAdd may absorb item into an existing stack and destroy item.
            if (canMerge)
            {
                return item.Destroyed || item.stackCount <= 0 || owner.Contains(item);
            }
            return owner.Contains(item);
        }

        private static bool VerifyStagedTotals(StagingHolder staging, Dictionary<ThingDef, int> requiredByDef)
        {
            if (staging == null || staging.innerContainer == null || requiredByDef == null) return false;

            Dictionary<ThingDef, int> actual = new Dictionary<ThingDef, int>();
            ThingOwner<Thing> cont = staging.innerContainer;
            for (int i = 0; i < cont.Count; i++)
            {
                Thing t = cont[i];
                if (t == null || t.Destroyed || t.def == null || t.stackCount <= 0) continue;
                int prev;
                actual.TryGetValue(t.def, out prev);
                actual[t.def] = prev + t.stackCount;
            }

            foreach (KeyValuePair<ThingDef, int> kv in requiredByDef)
            {
                int have;
                if (!actual.TryGetValue(kv.Key, out have) || have != kv.Value)
                {
                    return false;
                }
            }
            return true;
        }

        private static void RollbackMapJournal(StagingHolder staging, List<JournalEntry> journal)
        {
            for (int i = journal.Count - 1; i >= 0; i--)
            {
                JournalEntry entry = journal[i];
                if (entry == null) continue;

                try
                {
                    Thing chunk = entry.splitChunk;
                    if (chunk == null && entry.sourceStack != null && !entry.sourceStack.Destroyed &&
                        !entry.sourceStack.Spawned && entry.sourceStack.holdingOwner == null)
                    {
                        // A throwing SplitOff detached the whole sourceStack before returning
                        chunk = entry.sourceStack;
                    }

                    if (chunk == null || chunk.Destroyed) continue;

                    if (staging != null && staging.innerContainer != null && staging.innerContainer.Contains(chunk))
                    {
                        staging.innerContainer.Remove(chunk);
                    }

                    TryReturnThingToMap(chunk, entry.tookWhole ? null : entry.sourceStack, entry.origMap, entry.origPos);
                }
                catch (Exception ex)
                {
                    TestMode.ReportException("[TMM] StoryPayment: error while rolling back map journal entry: ", ex);
                }
            }

            // Defensive sweep: ensure nothing somehow remains in staging
            SweepStrayStagingItems(staging, journal.Count > 0 ? journal[0].origMap : null, journal.Count > 0 ? journal[0].origPos : IntVec3.Invalid, null);
        }

        private static void RollbackCaravanJournal(StagingHolder staging, List<JournalEntry> journal)
        {
            for (int i = journal.Count - 1; i >= 0; i--)
            {
                JournalEntry entry = journal[i];
                if (entry == null) continue;

                try
                {
                    Thing chunk = entry.splitChunk;
                    if (chunk == null && entry.sourceStack != null && !entry.sourceStack.Destroyed &&
                        !entry.sourceStack.Spawned && entry.sourceStack.holdingOwner == null)
                    {
                        chunk = entry.sourceStack;
                    }

                    if (chunk == null || chunk.Destroyed) continue;

                    if (entry.movedToTarget && entry.targetOwner != null && entry.targetOwner.Contains(chunk))
                    {
                        entry.targetOwner.Remove(chunk);
                        entry.movedToTarget = false;
                    }

                    if (staging != null && staging.innerContainer != null && staging.innerContainer.Contains(chunk))
                    {
                        staging.innerContainer.Remove(chunk);
                        entry.movedToStaging = false;
                    }

                    TryReturnThingToCaravan(chunk, entry.tookWhole ? null : entry.sourceStack, entry.origPawn, entry.origCaravan);
                }
                catch (Exception ex)
                {
                    TestMode.ReportException("[TMM] StoryPayment: error while rolling back caravan journal entry: ", ex);
                }
            }

            SweepStrayStagingItems(staging, null, IntVec3.Invalid, journal.Count > 0 ? journal[0].origCaravan : null);
        }

        private static void SweepStrayStagingItems(StagingHolder staging, Map fallbackMap, IntVec3 fallbackPos, Caravan fallbackCaravan)
        {
            if (staging == null || staging.innerContainer == null || staging.innerContainer.Count == 0) return;

            List<Thing> leftovers = new List<Thing>();
            for (int i = 0; i < staging.innerContainer.Count; i++)
            {
                if (staging.innerContainer[i] != null) leftovers.Add(staging.innerContainer[i]);
            }

            for (int i = 0; i < leftovers.Count; i++)
            {
                Thing t = leftovers[i];
                if (t == null || t.Destroyed) continue;
                if (staging.innerContainer.Contains(t)) staging.innerContainer.Remove(t);

                if (fallbackCaravan != null)
                {
                    TryReturnThingToCaravan(t, null, null, fallbackCaravan);
                }
                else
                {
                    TryReturnThingToMap(t, null, fallbackMap, fallbackPos);
                }
            }
        }

        private static void CommitDestroyStaged(StagingHolder staging)
        {
            if (staging == null || staging.innerContainer == null) return;

            List<Thing> toDestroy = new List<Thing>();
            for (int i = 0; i < staging.innerContainer.Count; i++)
            {
                if (staging.innerContainer[i] != null) toDestroy.Add(staging.innerContainer[i]);
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                Thing t = toDestroy[i];
                if (t == null || t.Destroyed) continue;
                try
                {
                    if (TestMode.Enabled && TestBeforeDestroyOverride != null)
                    {
                        TestBeforeDestroyOverride(t, i);
                    }
                    if (staging.innerContainer.Contains(t))
                    {
                        staging.innerContainer.Remove(t);
                    }
                    if (!t.Destroyed)
                    {
                        t.Destroy(DestroyMode.Vanish);
                    }
                }
                catch (Exception ex)
                {
                    TestMode.ReportException("[TMM] StoryPayment: exception destroying committed chunk " + t.ThingID + " (payment already confirmed): ", ex);
                    if (staging.innerContainer.Contains(t))
                    {
                        staging.innerContainer.Remove(t);
                    }
                }
            }
        }
    }
}
