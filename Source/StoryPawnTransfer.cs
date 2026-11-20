using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Snapshot of a free world pawn's state captured BEFORE any mutation during StoryPawnTransfer.
    /// Supports null original faction, guest/host status, faction leadership, and WorldPawns membership.
    /// </summary>
    public sealed class PawnTransferSnapshot
    {
        public Pawn pawn;
        public Faction origFaction;
        public bool origWasLeader;
        public bool origInWorldPawns;
        public bool origHasGuest;
        public Faction origHostFaction;
        public GuestStatus origGuestStatus;
    }

    /// <summary>
    /// Live handle for a completed StoryPawnTransfer, allowing callers (such as HotelVisit creating
    /// a shared Lord after spawning the hero) to roll back the hero cleanly if a subsequent step fails,
    /// or to finalize irreversible leadership replacement once the whole group is confirmed.
    /// </summary>
    public sealed class PawnTransferHandle
    {
        private readonly PawnTransferSnapshot snapshot;
        private readonly List<StoryPawnTransfer.TransferStep> journal;
        private bool rolledBack;
        private bool leadershipFinalized;

        internal PawnTransferHandle(PawnTransferSnapshot snapshot, List<StoryPawnTransfer.TransferStep> journal)
        {
            this.snapshot = snapshot;
            this.journal = journal;
        }

        public Pawn Pawn
        {
            get { return snapshot != null ? snapshot.pawn : null; }
        }

        public bool IsRolledBack
        {
            get { return rolledBack; }
        }

        /// <summary>
        /// Rolls back every journaled step in reverse order, returning the pawn to WorldPawns
        /// with its exact original faction (including null), guest status, and leadership.
        /// Safe to call more than once (idempotent).
        /// </summary>
        public bool Rollback()
        {
            if (rolledBack) return true;
            rolledBack = true;
            return StoryPawnTransfer.ExecuteRollback(snapshot, journal);
        }

        /// <summary>
        /// Finalizes irreversible post-confirmation side effects (replacing vacant faction leadership
        /// if the pawn changed factions and notifying HeroRegistry). Safe to call once after the caller's
        /// own Lord/group setup succeeds.
        /// </summary>
        public void FinalizeTransfer()
        {
            if (rolledBack || leadershipFinalized || snapshot == null || snapshot.pawn == null) return;
            leadershipFinalized = true;
            StoryPawnTransfer.FinalizeLeadershipAndRegistry(snapshot);
        }
    }

    /// <summary>
    /// Shared journaled, exception-safe transfer of a free world pawn onto a map cell.
    /// Used by StoryStage_Site (KillHero / Rescue) and StoryStage_HotelVisit.
    ///
    /// Guarantees:
    /// 1) Strict ownership checks matching TryJoin (living, non-player, unspawned, not prisoner/slave,
    ///    no holdingOwner, not in any caravan, present in Find.WorldPawns, optional requiredCurrentFaction).
    /// 2) Full pre-mutation snapshot (faction including null, guest/host status, leadership, WorldPawns).
    /// 3) Step journal recorded BEFORE each mutation; any false result or exception runs reverse rollback
    ///    returning the pawn to WorldPawns with its exact original state.
    /// 4) Removal from WorldPawns happens as the LAST step immediately before GenSpawn.Spawn, AFTER
    ///    companion groups / preconditions are already ready.
    /// </summary>
    public static class StoryPawnTransfer
    {
        internal enum TransferStep
        {
            FactionChanged,
            RemovedFromWorldPawns,
            SpawnedOnMap,
            GuestStatusChanged,
            PostSpawnExecuted
        }

        /// <summary>
        /// Test-only hook (ONLY active when TestMode.Enabled) allowing probes to simulate a
        /// GenSpawn failure or exception after RemovePawn / SetFaction to verify full rollback.
        /// </summary>
        public static Func<Pawn, Map, IntVec3, Thing> TestSpawnOverride;

        /// <summary>
        /// Validates that pawn is a free, living world pawn eligible for transfer onto targetMap at targetCell.
        /// Never mutates any state.
        /// </summary>
        public static bool CanTransferFreeWorldPawn(
            Pawn pawn,
            Map targetMap,
            IntVec3 targetCell,
            Faction requiredCurrentFaction,
            out string failReason)
        {
            failReason = null;

            if (pawn == null)
            {
                failReason = "pawn_null";
                return false;
            }
            if (pawn.Dead || pawn.Destroyed)
            {
                failReason = "pawn_dead_or_destroyed";
                return false;
            }
            if (pawn.Faction == Faction.OfPlayer)
            {
                failReason = "pawn_is_colonist";
                return false;
            }
            if (pawn.Spawned)
            {
                failReason = "pawn_already_spawned";
                return false;
            }
            if (pawn.IsPrisoner || pawn.IsSlave)
            {
                failReason = "pawn_prisoner_or_slave";
                return false;
            }
            if (pawn.holdingOwner != null)
            {
                failReason = "pawn_in_container";
                return false;
            }
            if (pawn.GetCaravan() != null || pawn.IsCaravanMember())
            {
                failReason = "pawn_in_caravan";
                return false;
            }
            if (Find.WorldPawns == null || !Find.WorldPawns.Contains(pawn))
            {
                failReason = "pawn_not_in_world_pawns";
                return false;
            }
            if (requiredCurrentFaction != null && pawn.Faction != null && pawn.Faction != requiredCurrentFaction)
            {
                failReason = "pawn_wrong_faction";
                return false;
            }
            if (targetMap == null || Find.Maps == null || !Find.Maps.Contains(targetMap))
            {
                failReason = "target_map_invalid";
                return false;
            }
            if (!targetCell.IsValid || !targetCell.InBounds(targetMap))
            {
                failReason = "target_cell_invalid";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks only pawn ownership/free-world eligibility (useful for pre-checks before a Site is created).
        /// </summary>
        public static bool IsFreeWorldPawn(Pawn pawn, Faction requiredCurrentFaction, out string failReason)
        {
            failReason = null;
            if (pawn == null) { failReason = "pawn_null"; return false; }
            if (pawn.Dead || pawn.Destroyed) { failReason = "pawn_dead_or_destroyed"; return false; }
            if (pawn.Faction == Faction.OfPlayer) { failReason = "pawn_is_colonist"; return false; }
            if (pawn.Spawned) { failReason = "pawn_already_spawned"; return false; }
            if (pawn.IsPrisoner || pawn.IsSlave) { failReason = "pawn_prisoner_or_slave"; return false; }
            if (pawn.holdingOwner != null) { failReason = "pawn_in_container"; return false; }
            if (pawn.GetCaravan() != null || pawn.IsCaravanMember()) { failReason = "pawn_in_caravan"; return false; }
            if (Find.WorldPawns == null || !Find.WorldPawns.Contains(pawn)) { failReason = "pawn_not_in_world_pawns"; return false; }
            if (requiredCurrentFaction != null && pawn.Faction != null && pawn.Faction != requiredCurrentFaction)
            {
                failReason = "pawn_wrong_faction";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Simple one-call API: takes a free world pawn, optionally assigns targetFaction, and spawns
        /// it at targetCell on targetMap. Automatically finalizes leadership replacement on success,
        /// or rolls back completely on any failure/exception.
        /// </summary>
        public static bool TrySpawnFreeWorldPawn(
            Pawn pawn,
            Map targetMap,
            IntVec3 targetCell,
            Faction targetFaction,
            bool changeFaction,
            out string failReason)
        {
            PawnTransferHandle handle;
            if (!TrySpawnFreeWorldPawn(pawn, targetMap, targetCell, targetFaction, changeFaction,
                    null, null, null, true, out handle, out failReason))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Full journaled transfer API supporting:
        /// - changeFaction + targetFaction (including targetFaction = null);
        /// - optional imprisonForFaction (sets GuestStatus.Prisoner &amp; verifies HostFaction);
        /// - optional requiredCurrentFaction guard;
        /// - optional postSpawnSetup callback (e.g. attaching to a Lord; returning false or throwing triggers full rollback);
        /// - autoFinalize = false returns an unfinalized PawnTransferHandle so callers can finish creating
        ///   a shared Lord for a group and call handle.FinalizeTransfer() or handle.Rollback().
        /// </summary>
        public static bool TrySpawnFreeWorldPawn(
            Pawn pawn,
            Map targetMap,
            IntVec3 targetCell,
            Faction targetFaction,
            bool changeFaction,
            Faction imprisonForFaction,
            Faction requiredCurrentFaction,
            Func<Pawn, bool> postSpawnSetup,
            bool autoFinalize,
            out PawnTransferHandle handle,
            out string failReason)
        {
            handle = null;
            if (!CanTransferFreeWorldPawn(pawn, targetMap, targetCell, requiredCurrentFaction, out failReason))
            {
                return false;
            }

            // Capture complete pre-mutation snapshot
            PawnTransferSnapshot snapshot = CaptureSnapshot(pawn);
            List<TransferStep> journal = new List<TransferStep>();

            try
            {
                // 1. Optional faction change (before removing from WorldPawns)
                if (changeFaction && pawn.Faction != targetFaction)
                {
                    journal.Add(TransferStep.FactionChanged);
                    pawn.SetFaction(targetFaction);
                    if (pawn.Faction != targetFaction)
                    {
                        failReason = "set_faction_failed";
                        ExecuteRollback(snapshot, journal);
                        return false;
                    }
                }

                // 2. Remove from WorldPawns as the LAST step immediately before GenSpawn.Spawn
                journal.Add(TransferStep.RemovedFromWorldPawns);
                Find.WorldPawns.RemovePawn(pawn);
                if (Find.WorldPawns.Contains(pawn))
                {
                    failReason = "remove_world_pawn_failed";
                    ExecuteRollback(snapshot, journal);
                    return false;
                }

                // 3. Spawn on targetMap at targetCell
                journal.Add(TransferStep.SpawnedOnMap);
                Thing spawned = ExecuteSpawn(pawn, targetMap, targetCell);
                if (spawned != pawn || !pawn.Spawned || pawn.Map != targetMap)
                {
                    failReason = "spawn_failed";
                    ExecuteRollback(snapshot, journal);
                    return false;
                }

                // 4. Optional imprisonment (e.g. StoryStage_Site Rescue)
                if (imprisonForFaction != null)
                {
                    if (pawn.guest == null)
                    {
                        failReason = "no_guest_tracker";
                        ExecuteRollback(snapshot, journal);
                        return false;
                    }

                    journal.Add(TransferStep.GuestStatusChanged);
                    pawn.guest.SetGuestStatus(imprisonForFaction, GuestStatus.Prisoner); // CHECK API
                    if (!pawn.IsPrisoner || pawn.HostFaction != imprisonForFaction)
                    {
                        failReason = "imprison_failed";
                        ExecuteRollback(snapshot, journal);
                        return false;
                    }
                }

                // 5. Optional caller post-spawn hook (e.g. attach to Lord)
                if (postSpawnSetup != null)
                {
                    journal.Add(TransferStep.PostSpawnExecuted);
                    if (!postSpawnSetup(pawn))
                    {
                        failReason = "post_spawn_setup_failed";
                        ExecuteRollback(snapshot, journal);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryPawnTransfer: exception during pawn transfer, rolling back: " + ex.Message);
                failReason = "transfer_exception";
                ExecuteRollback(snapshot, journal);
                return false;
            }

            handle = new PawnTransferHandle(snapshot, journal);
            if (autoFinalize)
            {
                handle.FinalizeTransfer();
            }

            failReason = null;
            return true;
        }

        private static PawnTransferSnapshot CaptureSnapshot(Pawn pawn)
        {
            PawnTransferSnapshot snap = new PawnTransferSnapshot();
            snap.pawn = pawn;
            snap.origFaction = pawn.Faction;
            snap.origWasLeader = pawn.Faction != null && pawn.Faction.leader == pawn;
            snap.origInWorldPawns = Find.WorldPawns != null && Find.WorldPawns.Contains(pawn);
            snap.origHasGuest = pawn.guest != null;
            snap.origHostFaction = pawn.guest != null ? pawn.guest.HostFaction : null;
            snap.origGuestStatus = pawn.guest != null ? pawn.guest.GuestStatus : GuestStatus.Guest; // CHECK API
            return snap;
        }

        private static Thing ExecuteSpawn(Pawn pawn, Map map, IntVec3 cell)
        {
            if (TestMode.Enabled && TestSpawnOverride != null)
            {
                return TestSpawnOverride(pawn, map, cell);
            }
            return GenSpawn.Spawn(pawn, cell, map, WipeMode.Vanish);
        }

        internal static bool ExecuteRollback(PawnTransferSnapshot snapshot, List<TransferStep> journal)
        {
            if (snapshot == null || snapshot.pawn == null) return false;
            Pawn pawn = snapshot.pawn;
            bool allOk = true;

            if (journal != null)
            {
                for (int i = journal.Count - 1; i >= 0; i--)
                {
                    TransferStep step = journal[i];
                    try
                    {
                        if (step == TransferStep.PostSpawnExecuted)
                        {
                            Lord lord = pawn.GetLord();
                            if (lord != null) lord.RemovePawn(pawn);
                        }
                        else if (step == TransferStep.GuestStatusChanged)
                        {
                            if (pawn.guest != null)
                            {
                                pawn.guest.SetGuestStatus(snapshot.origHostFaction, snapshot.origGuestStatus); // CHECK API
                            }
                        }
                        else if (step == TransferStep.SpawnedOnMap)
                        {
                            Lord lord = pawn.GetLord();
                            if (lord != null) lord.RemovePawn(pawn);
                            if (pawn.Spawned)
                            {
                                pawn.DeSpawn(DestroyMode.Vanish);
                            }
                            if (pawn.holdingOwner != null)
                            {
                                pawn.holdingOwner.Remove(pawn);
                            }
                        }
                        else if (step == TransferStep.FactionChanged)
                        {
                            if (pawn.Faction != snapshot.origFaction)
                            {
                                pawn.SetFaction(snapshot.origFaction);
                            }
                        }
                        else if (step == TransferStep.RemovedFromWorldPawns)
                        {
                            if (pawn.Spawned) pawn.DeSpawn(DestroyMode.Vanish);
                            if (pawn.holdingOwner != null) pawn.holdingOwner.Remove(pawn);
                            if (snapshot.origInWorldPawns && Find.WorldPawns != null && !Find.WorldPawns.Contains(pawn))
                            {
                                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TestMode.ReportException("[TMM] StoryPawnTransfer: rollback step " + step + " failed for " + pawn.LabelShort + ": ", ex);
                        allOk = false;
                    }
                }
            }

            // Always restore faction leadership if pawn was originally leader
            try
            {
                if (snapshot.origWasLeader && snapshot.origFaction != null && snapshot.origFaction.leader != pawn)
                {
                    snapshot.origFaction.leader = pawn;
                }
            }
            catch (Exception ex)
            {
                TestMode.ReportException("[TMM] StoryPawnTransfer: failed restoring faction leader: ", ex);
                allOk = false;
            }

            // Final invariant verification
            if (pawn.Spawned || pawn.Faction != snapshot.origFaction)
            {
                allOk = false;
            }
            if (snapshot.origInWorldPawns && (Find.WorldPawns == null || !Find.WorldPawns.Contains(pawn)))
            {
                allOk = false;
            }
            if (snapshot.origHasGuest && pawn.guest != null &&
                (pawn.guest.HostFaction != snapshot.origHostFaction || pawn.guest.GuestStatus != snapshot.origGuestStatus)) // CHECK API
            {
                allOk = false;
            }

            if (!allOk)
            {
                Log.Error("[TMM] StoryPawnTransfer: post-rollback verification failed for " + pawn.LabelShort);
            }
            return allOk;
        }

        internal static void FinalizeLeadershipAndRegistry(PawnTransferSnapshot snapshot)
        {
            if (snapshot == null || snapshot.pawn == null) return;
            Pawn pawn = snapshot.pawn;

            if (snapshot.origWasLeader && snapshot.origFaction != null && snapshot.origFaction != pawn.Faction)
            {
                if (HeroRegistry.Instance != null)
                {
                    Pawn next = HeroRegistry.Instance.SuccessorForVacantLeadership(snapshot.origFaction);
                    snapshot.origFaction.leader = next;
                    if (next == null) snapshot.origFaction.TryGenerateNewLeader();
                }
                else
                {
                    snapshot.origFaction.leader = null;
                    snapshot.origFaction.TryGenerateNewLeader();
                }
            }

            if (HeroRegistry.Instance != null)
            {
                HeroRegistry.Instance.NotifyEncountered(pawn);
            }
        }
    }
}
