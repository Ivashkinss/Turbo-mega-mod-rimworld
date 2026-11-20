`Source/TestMode.cs`
```csharp
using System;

namespace TurboMegaMod
{
    /// <summary>
    /// Cached check for the -tmm-story-test launch argument.
    /// Every test-only hook across the mod MUST check TestMode.Enabled so test switches
    /// are completely inert during normal gameplay.
    /// </summary>
    public static class TestMode
    {
        private static readonly bool s_enabled = DetectTestFlag();

        public static bool Enabled
        {
            get { return s_enabled; }
        }

        private static bool DetectTestFlag()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-tmm-story-test", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
```

`Source/StoryRelationRestorer.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Tracks a single active raid hostility lease OR hotel visit truce (neutrality) lease
    /// on a faction's relation override.
    /// </summary>
    public sealed class StoryHostilityLease : IExposable
    {
        public const string ModeHostile = "hostile";
        public const string ModeNeutral = "neutral";

        public string leaseId;
        public string quest;
        public string stageKey;
        public int runGen;
        public bool restoreRelations;
        public string mode = ModeHostile;

        public void ExposeData()
        {
            Scribe_Values.Look(ref leaseId, "leaseId");
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref restoreRelations, "restoreRelations", false);
            Scribe_Values.Look(ref mode, "mode", ModeHostile);
            if (mode.NullOrEmpty()) mode = ModeHostile;
        }
    }

    /// <summary>
    /// Stores original faction goodwill &amp; relation kind before concurrent story raids (Hostile)
    /// or hotel visits (Neutral truce) altered relations. Restoration runs ONLY when the final
    /// lease for that faction is released/aborted or orphaned, and the backup is removed ONLY
    /// after symmetric restoration on both FactionRelations is verified.
    /// </summary>
    public sealed class StoryFactionBackup : IExposable
    {
        public int factionId = -1;
        public int origGoodwill;
        public int origKind = (int)FactionRelationKind.Neutral;
        public bool origHasGoodwill;
        public int postForceGoodwill = -100;
        public bool restoreWanted;
        public bool anyLeaseCompletedKeepHostile;
        public string activeMode = StoryHostilityLease.ModeHostile;
        public List<StoryHostilityLease> leases = new List<StoryHostilityLease>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionId, "factionId", -1);
            Scribe_Values.Look(ref origGoodwill, "origGoodwill", 0);
            Scribe_Values.Look(ref origKind, "origKind", (int)FactionRelationKind.Neutral);
            Scribe_Values.Look(ref origHasGoodwill, "origHasGoodwill", true);
            Scribe_Values.Look(ref postForceGoodwill, "postForceGoodwill", -100);
            Scribe_Values.Look(ref restoreWanted, "restoreWanted", false);
            Scribe_Values.Look(ref anyLeaseCompletedKeepHostile, "anyLeaseCompletedKeepHostile", false);
            Scribe_Values.Look(ref activeMode, "activeMode", StoryHostilityLease.ModeHostile);
            Scribe_Collections.Look(ref leases, "leases", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (leases == null) leases = new List<StoryHostilityLease>();
                if (activeMode.NullOrEmpty()) activeMode = StoryHostilityLease.ModeHostile;
            }
        }
    }

    /// <summary>
    /// Protected unique Retrieve item record tied to a specific quest run so stack merging is blocked
    /// across save/reload and automatically released when the stage ends or is disabled.
    /// </summary>
    public sealed class StoryProtectedItem : IExposable
    {
        public string thingId;
        public string quest;
        public string stageKey;
        public int runGen;

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingId, "thingId");
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref runGen, "runGen", 0);
        }
    }

    /// <summary>
    /// Tracks a registered hotel visit guest so Pawn.ExitMap can distinguish a genuine peaceful exit
    /// (confirmed by actual despawn from the visit map) from disappearance, container insertion,
    /// imprisonment, sentAway, or map deletion across reload.
    /// </summary>
    public sealed class StoryTrackedGuestRecord : IExposable
    {
        public string runKey;
        public string quest;
        public string stageKey;
        public int runGen;
        public int mapId = -1;
        public int pawnIdNum = -1;
        public int visitLordId = -1;
        public int exitLordId = -1;
        public bool exitedPeacefully;
        public bool exitedHostileOrForced;

        public void ExposeData()
        {
            Scribe_Values.Look(ref runKey, "runKey");
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref mapId, "mapId", -1);
            Scribe_Values.Look(ref pawnIdNum, "pawnIdNum", -1);
            Scribe_Values.Look(ref visitLordId, "visitLordId", -1);
            Scribe_Values.Look(ref exitLordId, "exitLordId", -1);
            Scribe_Values.Look(ref exitedPeacefully, "exitedPeacefully", false);
            Scribe_Values.Look(ref exitedHostileOrForced, "exitedHostileOrForced", false);
        }
    }

    /// <summary>
    /// WorldComponent managing:
    /// 1) Symmetric temporary raid hostility and hotel-visit truce (neutrality) leases with
    ///    verified restoration (both NPC and player FactionRelations + vanilla notifications),
    ///    retry on failed restoration, reference-counted overlapping visits/raids, and guaranteed
    ///    rollback via AbortLease whenever a raid fails to spawn regardless of restoreRelations.
    /// 2) Retrieve quest item anti-stack-merge protection.
    /// 3) Confirmed hotel guest ExitMap tracking.
    /// </summary>
    public sealed class StoryRelationRestorer : WorldComponent
    {
        private const int CheckInterval = 250;

        /// <summary>
        /// Test-only hook (active ONLY when TestMode.Enabled) that simulates a blocked restoration
        /// attempt to verify that StoryFactionBackup is retained and retried on subsequent audits.
        /// </summary>
        public static Func<Faction, bool> TestBlockRestoreOverride;

        private List<StoryFactionBackup> backups = new List<StoryFactionBackup>();
        private List<StoryProtectedItem> protectedItems = new List<StoryProtectedItem>();
        private List<StoryTrackedGuestRecord> trackedGuests = new List<StoryTrackedGuestRecord>();

        private static readonly HashSet<string> ActiveProtectedIds = new HashSet<string>();
        private static readonly HashSet<int> ActiveTrackedGuestPawnIds = new HashSet<int>();

        public StoryRelationRestorer(World world) : base(world)
        {
        }

        public static StoryRelationRestorer Instance
        {
            get { return Find.World == null ? null : Find.World.GetComponent<StoryRelationRestorer>(); }
        }

        public static void CheckOrphansIfPresent()
        {
            StoryRelationRestorer inst = Instance;
            if (inst != null) inst.CheckOrphansNow();
        }

        public static string MakeLeaseId(StoryQuestRun run)
        {
            if (run == null) return string.Empty;
            return string.Format("{0}|{1}|{2}", run.quest ?? string.Empty, run.stageKey ?? string.Empty, run.gen);
        }

        public bool HasActiveTruceBackup(Faction fac)
        {
            if (fac == null) return false;
            StoryFactionBackup b = FindBackup(fac.loadID);
            return b != null && b.activeMode == StoryHostilityLease.ModeNeutral && b.leases != null && b.leases.Count > 0;
        }

        public bool HasPendingBackupForFaction(Faction fac)
        {
            if (fac == null) return false;
            return FindBackup(fac.loadID) != null;
        }

        // =====================================================================
        // HOSTILITY LEASES (COLONY RAID)
        // =====================================================================

        public bool AcquireHostilityLease(Faction fac, StoryQuestRun run, bool restoreRelations, out string leaseId)
        {
            leaseId = null;
            if (fac == null || fac.IsPlayer || fac.defeated || run == null) return false;

            StoryFactionBackup existing = ResolveOrRetryStaleBackup(fac.loadID);

            if (existing == null && fac.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            // Refuse conflicting concurrent override (e.g. active hotel truce or unrecovered pending backup)
            if (existing != null && (existing.activeMode != StoryHostilityLease.ModeHostile || existing.leases.Count == 0))
            {
                return false;
            }

            leaseId = MakeLeaseId(run);

            if (existing == null)
            {
                existing = new StoryFactionBackup();
                existing.factionId = fac.loadID;
                existing.origGoodwill = fac.PlayerGoodwill;
                existing.origKind = (int)fac.RelationKindWith(Faction.OfPlayer);
                existing.origHasGoodwill = fac.HasGoodwill;
                existing.restoreWanted = restoreRelations;
                existing.anyLeaseCompletedKeepHostile = false;
                existing.activeMode = StoryHostilityLease.ModeHostile;
                backups.Add(existing);

                AddOrUpdateLease(existing, leaseId, run, restoreRelations, StoryHostilityLease.ModeHostile);

                if (!ForceFactionHostile(fac))
                {
                    RemoveLease(existing, leaseId);
                    existing.restoreWanted = true;
                    if (FinalizeBackup(existing))
                    {
                        backups.Remove(existing);
                    }
                    leaseId = null;
                    return false;
                }

                existing.postForceGoodwill = fac.PlayerGoodwill;
                return true;
            }

            if (restoreRelations) existing.restoreWanted = true;
            AddOrUpdateLease(existing, leaseId, run, restoreRelations, StoryHostilityLease.ModeHostile);

            if (!fac.HostileTo(Faction.OfPlayer))
            {
                if (!ForceFactionHostile(fac))
                {
                    RemoveLease(existing, leaseId);
                    leaseId = null;
                    return false;
                }
            }
            return true;
        }

        // =====================================================================
        // TRUCE / TEMPORARY NEUTRALITY LEASES (HOTEL VISIT)
        // =====================================================================

        public static bool CanFactionEnterTruce(Faction fac)
        {
            if (fac == null || fac.IsPlayer || fac.defeated) return false;
            if (!fac.HostileTo(Faction.OfPlayer)) return true;
            if (fac.def != null && fac.def.permanentEnemy) return false;

            if (fac.HasGoodwill)
            {
                int needed = 0 - fac.PlayerGoodwill;
                if (needed > 0 && !fac.CanChangeGoodwillFor(Faction.OfPlayer, needed)) // CHECK API
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Safe to call unconditionally for every hotel visit faction:
        /// - If the faction is naturally non-hostile and has NO active temporary truce backup,
        ///   returns true with leaseId = null.
        /// - If the faction is currently hostile OR ALREADY has an active temporary neutrality backup
        ///   from an earlier overlapping visit, increments the lease list and returns a non-null leaseId.
        ///   Hostility is restored ONLY when the final truce lease is released/aborted/orphaned.
        /// </summary>
        public bool AcquireTruceLease(Faction fac, StoryQuestRun run, out string leaseId)
        {
            leaseId = null;
            if (fac == null || fac.IsPlayer || fac.defeated || run == null) return false;

            StoryFactionBackup existing = ResolveOrRetryStaleBackup(fac.loadID);

            if (existing == null && !fac.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            // Refuse conflicting concurrent override (e.g. active colony raid lease or stuck backup)
            if (existing != null && (existing.activeMode != StoryHostilityLease.ModeNeutral || existing.leases.Count == 0))
            {
                return false;
            }

            if (existing == null && !CanFactionEnterTruce(fac))
            {
                return false;
            }

            leaseId = MakeLeaseId(run);

            if (existing == null)
            {
                existing = new StoryFactionBackup();
                existing.factionId = fac.loadID;
                existing.origGoodwill = fac.PlayerGoodwill;
                existing.origKind = (int)fac.RelationKindWith(Faction.OfPlayer);
                existing.origHasGoodwill = fac.HasGoodwill;
                existing.restoreWanted = true;
                existing.anyLeaseCompletedKeepHostile = false;
                existing.activeMode = StoryHostilityLease.ModeNeutral;
                backups.Add(existing);

                AddOrUpdateLease(existing, leaseId, run, true, StoryHostilityLease.ModeNeutral);

                if (!ForceFactionNeutral(fac))
                {
                    RemoveLease(existing, leaseId);
                    existing.restoreWanted = true;
                    if (FinalizeBackup(existing))
                    {
                        backups.Remove(existing);
                    }
                    leaseId = null;
                    return false;
                }

                existing.postForceGoodwill = fac.PlayerGoodwill;
                return true;
            }

            // Second or subsequent overlapping visit on a temporarily-neutral faction:
            // ALWAYS record a counted lease so the first visit cannot prematurely restore hostility.
            existing.restoreWanted = true;
            AddOrUpdateLease(existing, leaseId, run, true, StoryHostilityLease.ModeNeutral);

            if (fac.HostileTo(Faction.OfPlayer))
            {
                if (!ForceFactionNeutral(fac))
                {
                    RemoveLease(existing, leaseId);
                    leaseId = null;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Normal release when a raid or hotel visit finishes.
        /// Respects each lease's restoreRelations flag; removes backup ONLY after verified restoration.
        /// </summary>
        public void ReleaseLease(string leaseId)
        {
            ReleaseLeaseInternal(leaseId, false);
        }

        /// <summary>
        /// Forced rollback release when a raid/visit failed to spawn or threw before starting.
        /// ALWAYS restores forced hostility/truce when no other active lease remains and no prior
        /// concurrent raid actually completed with restoreRelations=false.
        /// </summary>
        public void AbortLease(string leaseId)
        {
            ReleaseLeaseInternal(leaseId, true);
        }

        private void ReleaseLeaseInternal(string leaseId, bool abortedBeforeStart)
        {
            if (leaseId.NullOrEmpty() || backups == null) return;

            for (int i = backups.Count - 1; i >= 0; i--)
            {
                StoryFactionBackup b = backups[i];
                if (b == null)
                {
                    backups.RemoveAt(i);
                    continue;
                }

                StoryHostilityLease removedLease;
                if (!RemoveLease(b, leaseId, out removedLease)) continue;

                if (abortedBeforeStart)
                {
                    // Aborted raid/visit MUST roll back unless another raid actually completed keeping hostility
                    if (!b.anyLeaseCompletedKeepHostile)
                    {
                        b.restoreWanted = true;
                    }
                }
                else if (removedLease != null)
                {
                    if (removedLease.restoreRelations || removedLease.mode == StoryHostilityLease.ModeNeutral)
                    {
                        b.restoreWanted = true;
                    }
                    else
                    {
                        b.anyLeaseCompletedKeepHostile = true;
                    }
                }

                if (b.leases.Count == 0)
                {
                    if (FinalizeBackup(b))
                    {
                        backups.RemoveAt(i);
                    }
                    // On failure, backup remains in backups and is retried on the next audit tick.
                }
            }
        }

        public void CheckOrphansNow()
        {
            SparkStory story = SparkStory.Instance;

            if (backups != null)
            {
                for (int i = backups.Count - 1; i >= 0; i--)
                {
                    StoryFactionBackup b = backups[i];
                    if (b == null)
                    {
                        backups.RemoveAt(i);
                        continue;
                    }

                    if (b.leases == null) b.leases = new List<StoryHostilityLease>();
                    for (int l = b.leases.Count - 1; l >= 0; l--)
                    {
                        StoryHostilityLease lease = b.leases[l];
                        if (!IsLeaseStillActive(story, lease))
                        {
                            if (lease != null && (lease.restoreRelations || lease.mode == StoryHostilityLease.ModeNeutral))
                            {
                                b.restoreWanted = true;
                            }
                            b.leases.RemoveAt(l);
                        }
                    }

                    if (b.leases.Count == 0)
                    {
                        if (FinalizeBackup(b))
                        {
                            backups.RemoveAt(i);
                        }
                    }
                }
            }

            if (protectedItems != null)
            {
                bool changed = false;
                for (int i = protectedItems.Count - 1; i >= 0; i--)
                {
                    StoryProtectedItem item = protectedItems[i];
                    if (!IsProtectedItemStillActive(story, item))
                    {
                        protectedItems.RemoveAt(i);
                        changed = true;
                    }
                }
                if (changed) RebuildProtectedCache();
            }

            if (trackedGuests != null)
            {
                bool changed = false;
                for (int i = trackedGuests.Count - 1; i >= 0; i--)
                {
                    StoryTrackedGuestRecord rec = trackedGuests[i];
                    if (!IsGuestRecordStillActive(story, rec))
                    {
                        trackedGuests.RemoveAt(i);
                        changed = true;
                    }
                }
                if (changed) RebuildTrackedGuestCache();
            }
        }

        private StoryFactionBackup ResolveOrRetryStaleBackup(int factionId)
        {
            StoryFactionBackup b = FindBackup(factionId);
            if (b != null && (b.leases == null || b.leases.Count == 0))
            {
                if (FinalizeBackup(b))
                {
                    backups.Remove(b);
                    return null;
                }
            }
            return b;
        }

        private static bool IsLeaseStillActive(SparkStory story, StoryHostilityLease lease)
        {
            if (story == null || lease == null || lease.quest.NullOrEmpty()) return false;
            StoryQuestRun run = story.RunFor(lease.quest);
            if (run == null || run.status != StoryQuestStatus.Active) return false;
            if (run.gen != lease.runGen) return false;
            if (!lease.stageKey.NullOrEmpty() && run.stageKey != lease.stageKey) return false;

            StoryQuestDef def = run.Def;
            if (def == null || run.stage < 0 || run.stage >= def.stages.Count) return false;

            if (lease.mode == StoryHostilityLease.ModeNeutral)
            {
                return def.stages[run.stage] is StoryStage_HotelVisit;
            }
            return def.stages[run.stage] is StoryStage_ColonyRaid;
        }

        private static bool IsProtectedItemStillActive(SparkStory story, StoryProtectedItem item)
        {
            if (story == null || item == null || item.thingId.NullOrEmpty() || item.quest.NullOrEmpty()) return false;
            StoryQuestRun run = story.RunFor(item.quest);
            if (run == null || run.status != StoryQuestStatus.Active) return false;
            if (run.gen != item.runGen) return false;
            if (!item.stageKey.NullOrEmpty() && run.stageKey != item.stageKey) return false;

            StoryQuestDef def = run.Def;
            if (def == null || run.stage < 0 || run.stage >= def.stages.Count) return false;
            return def.stages[run.stage] is StoryStage_Site;
        }

        private static bool IsGuestRecordStillActive(SparkStory story, StoryTrackedGuestRecord rec)
        {
            if (story == null || rec == null || rec.quest.NullOrEmpty()) return false;
            StoryQuestRun run = story.RunFor(rec.quest);
            if (run == null || run.status != StoryQuestStatus.Active) return false;
            if (run.gen != rec.runGen) return false;
            if (!rec.stageKey.NullOrEmpty() && run.stageKey != rec.stageKey) return false;

            StoryQuestDef def = run.Def;
            if (def == null || run.stage < 0 || run.stage >= def.stages.Count) return false;
            return def.stages[run.stage] is StoryStage_HotelVisit;
        }

        /// <summary>
        /// Attempts to finalize a backup whose lease count reached 0.
        /// Returns true ONLY if no restoration is needed, faction no longer exists,
        /// or symmetric restoration was confirmed on BOTH FactionRelations.
        /// </summary>
        private bool FinalizeBackup(StoryFactionBackup b)
        {
            if (b == null) return true;
            if (!b.restoreWanted) return true;

            Faction fac = FindFactionById(b.factionId);
            if (fac == null || fac.defeated) return true;

            if (TestMode.Enabled && TestBlockRestoreOverride != null && TestBlockRestoreOverride(fac))
            {
                return false;
            }

            return RestoreFactionRelations(fac, b);
        }

        // =====================================================================
        // SYMMETRIC RELATION MUTATION, NOTIFICATIONS & VERIFICATION
        // =====================================================================

        private static bool ForceFactionHostile(Faction fac)
        {
            Faction player = Faction.OfPlayer;
            if (fac == null || player == null || fac.IsPlayer) return false;
            if (fac.HostileTo(player) && player.HostileTo(fac)) return true;

            if (fac.HasGoodwill)
            {
                int drop = -100 - fac.PlayerGoodwill;
                if (drop < 0 && fac.CanChangeGoodwillFor(player, drop)) // CHECK API
                {
                    fac.TryAffectGoodwillWith(player, drop, false, false, null, null); // CHECK API
                }
            }
            else
            {
                fac.SetRelationDirect(player, FactionRelationKind.Hostile, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }

            return fac.HostileTo(player) && player.HostileTo(fac);
        }

        private static bool ForceFactionNeutral(Faction fac)
        {
            Faction player = Faction.OfPlayer;
            if (fac == null || player == null || fac.IsPlayer || fac.defeated) return false;
            if (!fac.HostileTo(player) && !player.HostileTo(fac)) return true;
            if (fac.def != null && fac.def.permanentEnemy) return false;

            if (fac.HasGoodwill)
            {
                int lift = 0 - fac.PlayerGoodwill;
                if (lift > 0 && fac.CanChangeGoodwillFor(player, lift)) // CHECK API
                {
                    fac.TryAffectGoodwillWith(player, lift, false, false, null, null); // CHECK API
                }
            }
            else
            {
                fac.SetRelationDirect(player, FactionRelationKind.Neutral, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }

            return !fac.HostileTo(player) && !player.HostileTo(fac);
        }

        private static bool RestoreFactionRelations(Faction fac, StoryFactionBackup b)
        {
            Faction player = Faction.OfPlayer;
            if (fac == null || player == null || b == null) return false;

            FactionRelationKind origKind = (FactionRelationKind)b.origKind;

            int externalDelta = fac.HasGoodwill ? (fac.PlayerGoodwill - b.postForceGoodwill) : 0;
            int targetGoodwill = Mathf.Clamp(b.origGoodwill + externalDelta, -100, 100);

            if (origKind == FactionRelationKind.Neutral && targetGoodwill < -74)
            {
                targetGoodwill = -74;
            }
            else if (origKind == FactionRelationKind.Ally && targetGoodwill < 0)
            {
                targetGoodwill = 0;
            }
            else if (origKind == FactionRelationKind.Hostile && targetGoodwill > -1)
            {
                targetGoodwill = -1;
            }

            try
            {
                if (fac.HasGoodwill)
                {
                    // Step 1: Try vanilla TryAffectGoodwillWith with hysteresis threshold crossing
                    if (fac.HostileTo(player) && origKind != FactionRelationKind.Hostile)
                    {
                        int liftTarget = origKind == FactionRelationKind.Ally ? Mathf.Max(75, targetGoodwill) : Mathf.Max(0, targetGoodwill);
                        int liftDelta = liftTarget - fac.PlayerGoodwill;
                        if (liftDelta > 0)
                        {
                            fac.TryAffectGoodwillWith(player, liftDelta, false, false, null, null); // CHECK API
                        }
                    }
                    else if (!fac.HostileTo(player) && origKind == FactionRelationKind.Hostile)
                    {
                        int dropTarget = Mathf.Min(-75, targetGoodwill);
                        int dropDelta = dropTarget - fac.PlayerGoodwill;
                        if (dropDelta < 0)
                        {
                            fac.TryAffectGoodwillWith(player, dropDelta, false, false, null, null); // CHECK API
                        }
                    }

                    int finalDelta = targetGoodwill - fac.PlayerGoodwill;
                    if (finalDelta != 0)
                    {
                        fac.TryAffectGoodwillWith(player, finalDelta, false, false, null, null); // CHECK API
                    }

                    // Step 2: Symmetric fallback matching vanilla Faction.TryAffectGoodwillWith
                    // (synchronizes BOTH NPC and player FactionRelations + fires vanilla notifications)
                    if (!VerifyRestored(fac, player, origKind, targetGoodwill, true))
                    {
                        FactionRelationKind prevNpcKind = fac.RelationKindWith(player);
                        FactionRelationKind prevPlayerKind = player.RelationKindWith(fac);

                        FactionRelation relNpc = fac.RelationWith(player, true); // CHECK API
                        FactionRelation relPlayer = player.RelationWith(fac, true); // CHECK API
                        if (relNpc == null || relPlayer == null) return false;

                        relNpc.baseGoodwill = targetGoodwill; // CHECK API
                        relNpc.kind = origKind; // CHECK API
                        relPlayer.baseGoodwill = targetGoodwill; // CHECK API
                        relPlayer.kind = origKind; // CHECK API

                        if (prevNpcKind != origKind || prevPlayerKind != origKind)
                        {
                            NotifyRelationKindChangedSymmetrically(fac, player, prevNpcKind, prevPlayerKind);
                        }
                    }

                    return VerifyRestored(fac, player, origKind, targetGoodwill, true);
                }
                else
                {
                    if (!VerifyRestored(fac, player, origKind, 0, false))
                    {
                        FactionRelationKind prevNpcKind = fac.RelationKindWith(player);
                        FactionRelationKind prevPlayerKind = player.RelationKindWith(fac);

                        fac.SetRelationDirect(player, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API

                        if (!VerifyRestored(fac, player, origKind, 0, false))
                        {
                            FactionRelation relNpc = fac.RelationWith(player, true); // CHECK API
                            FactionRelation relPlayer = player.RelationWith(fac, true); // CHECK API
                            if (relNpc == null || relPlayer == null) return false;

                            relNpc.kind = origKind; // CHECK API
                            relPlayer.kind = origKind; // CHECK API

                            if (prevNpcKind != origKind || prevPlayerKind != origKind)
                            {
                                NotifyRelationKindChangedSymmetrically(fac, player, prevNpcKind, prevPlayerKind);
                            }
                        }
                    }

                    return VerifyRestored(fac, player, origKind, 0, false);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] StoryRelationRestorer: failed restoring relations for " + fac.Name + ": " + ex);
                return false;
            }
        }

        private static void NotifyRelationKindChangedSymmetrically(Faction fac, Faction player, FactionRelationKind prevNpcKind, FactionRelationKind prevPlayerKind)
        {
            bool sentLetter;
            // Mirrors vanilla TryAffectGoodwillWith / SetRelationDirect notifications on both factions
            // (updates AttackTargetsCache, LordManager, Pawns, and Goodwill/Hostility state).
            fac.Notify_RelationKindChanged(player, prevNpcKind, false, null, GlobalTargetInfo.Invalid, out sentLetter); // CHECK API
            player.Notify_RelationKindChanged(fac, prevPlayerKind, false, null, GlobalTargetInfo.Invalid, out sentLetter); // CHECK API
        }

        private static bool VerifyRestored(Faction fac, Faction player, FactionRelationKind expectedKind, int expectedGoodwill, bool checkGoodwill)
        {
            if (fac == null || player == null) return false;

            FactionRelation relNpc = fac.RelationWith(player, true); // CHECK API
            FactionRelation relPlayer = player.RelationWith(fac, true); // CHECK API
            if (relNpc == null || relPlayer == null) return false;

            if (relNpc.kind != expectedKind || relPlayer.kind != expectedKind) return false;
            if (fac.RelationKindWith(player) != expectedKind || player.RelationKindWith(fac) != expectedKind) return false;

            if (checkGoodwill)
            {
                if (relNpc.baseGoodwill != expectedGoodwill || relPlayer.baseGoodwill != expectedGoodwill) return false;
                if (fac.PlayerGoodwill != expectedGoodwill || player.GoodwillWith(fac) != expectedGoodwill) return false; // CHECK API
            }

            return true;
        }

        private StoryFactionBackup FindBackup(int factionId)
        {
            if (backups == null) return null;
            for (int i = 0; i < backups.Count; i++)
            {
                if (backups[i] != null && backups[i].factionId == factionId) return backups[i];
            }
            return null;
        }

        private static void AddOrUpdateLease(StoryFactionBackup b, string leaseId, StoryQuestRun run, bool restoreRelations, string mode)
        {
            if (b.leases == null) b.leases = new List<StoryHostilityLease>();
            for (int i = 0; i < b.leases.Count; i++)
            {
                if (b.leases[i] != null && b.leases[i].leaseId == leaseId)
                {
                    b.leases[i].restoreRelations = restoreRelations;
                    b.leases[i].mode = mode;
                    return;
                }
            }
            StoryHostilityLease lease = new StoryHostilityLease();
            lease.leaseId = leaseId;
            lease.quest = run.quest;
            lease.stageKey = run.stageKey;
            lease.runGen = run.gen;
            lease.restoreRelations = restoreRelations;
            lease.mode = mode;
            b.leases.Add(lease);
        }

        private static bool RemoveLease(StoryFactionBackup b, string leaseId)
        {
            StoryHostilityLease unused;
            return RemoveLease(b, leaseId, out unused);
        }

        private static bool RemoveLease(StoryFactionBackup b, string leaseId, out StoryHostilityLease removedLease)
        {
            removedLease = null;
            if (b == null || b.leases == null) return false;
            bool removed = false;
            for (int i = b.leases.Count - 1; i >= 0; i--)
            {
                if (b.leases[i] != null && b.leases[i].leaseId == leaseId)
                {
                    removedLease = b.leases[i];
                    b.leases.RemoveAt(i);
                    removed = true;
                }
            }
            return removed;
        }

        private static Faction FindFactionById(int loadId)
        {
            if (loadId < 0 || Find.FactionManager == null) return null;
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId) return all[i];
            }
            return null;
        }

        // =====================================================================
        // PROTECTED UNIQUE RETRIEVE ITEMS (ANTI-STACK-MERGE)
        // =====================================================================

        public void ProtectRetrieveItem(string thingId, StoryQuestRun run)
        {
            if (thingId.NullOrEmpty() || run == null) return;
            if (protectedItems == null) protectedItems = new List<StoryProtectedItem>();

            for (int i = 0; i < protectedItems.Count; i++)
            {
                if (protectedItems[i] != null && protectedItems[i].thingId == thingId)
                {
                    protectedItems[i].quest = run.quest;
                    protectedItems[i].stageKey = run.stageKey;
                    protectedItems[i].runGen = run.gen;
                    ActiveProtectedIds.Add(thingId);
                    return;
                }
            }

            StoryProtectedItem entry = new StoryProtectedItem();
            entry.thingId = thingId;
            entry.quest = run.quest;
            entry.stageKey = run.stageKey;
            entry.runGen = run.gen;
            protectedItems.Add(entry);
            ActiveProtectedIds.Add(thingId);
        }

        public void UnprotectRetrieveItem(string thingId)
        {
            if (thingId.NullOrEmpty()) return;
            ActiveProtectedIds.Remove(thingId);
            if (protectedItems == null) return;
            for (int i = protectedItems.Count - 1; i >= 0; i--)
            {
                if (protectedItems[i] != null && protectedItems[i].thingId == thingId)
                {
                    protectedItems.RemoveAt(i);
                }
            }
        }

        public static bool IsProtectedThing(Thing t)
        {
            if (t == null || ActiveProtectedIds.Count == 0) return false;
            return ActiveProtectedIds.Contains(t.ThingID);
        }

        private void RebuildProtectedCache()
        {
            ActiveProtectedIds.Clear();
            if (protectedItems == null) return;
            for (int i = 0; i < protectedItems.Count; i++)
            {
                if (protectedItems[i] != null && !protectedItems[i].thingId.NullOrEmpty())
                {
                    ActiveProtectedIds.Add(protectedItems[i].thingId);
                }
            }
        }

        // =====================================================================
        // TRACKED HOTEL VISIT GUESTS & CONFIRMED EXITMAP VERIFICATION
        // =====================================================================

        public void RegisterHotelGuests(StoryQuestRun run, int mapId, int visitLordId, List<int> pawnIds)
        {
            if (run == null || pawnIds == null) return;
            if (trackedGuests == null) trackedGuests = new List<StoryTrackedGuestRecord>();

            string runKey = MakeLeaseId(run);
            ClearHotelGuests(runKey);

            for (int i = 0; i < pawnIds.Count; i++)
            {
                StoryTrackedGuestRecord rec = new StoryTrackedGuestRecord();
                rec.runKey = runKey;
                rec.quest = run.quest;
                rec.stageKey = run.stageKey;
                rec.runGen = run.gen;
                rec.mapId = mapId;
                rec.pawnIdNum = pawnIds[i];
                rec.visitLordId = visitLordId;
                rec.exitLordId = -1;
                trackedGuests.Add(rec);
                ActiveTrackedGuestPawnIds.Add(pawnIds[i]);
            }
        }

        public void SetHotelExitLordId(string runKey, int exitLordId)
        {
            if (runKey.NullOrEmpty() || trackedGuests == null) return;
            for (int i = 0; i < trackedGuests.Count; i++)
            {
                if (trackedGuests[i] != null && trackedGuests[i].runKey == runKey)
                {
                    trackedGuests[i].exitLordId = exitLordId;
                }
            }
        }

        public void ClearHotelGuests(string runKey)
        {
            if (runKey.NullOrEmpty() || trackedGuests == null) return;
            bool removed = false;
            for (int i = trackedGuests.Count - 1; i >= 0; i--)
            {
                if (trackedGuests[i] != null && trackedGuests[i].runKey == runKey)
                {
                    trackedGuests.RemoveAt(i);
                    removed = true;
                }
            }
            if (removed) RebuildTrackedGuestCache();
        }

        public StoryTrackedGuestRecord FindTrackedGuest(string runKey, int pawnIdNum)
        {
            if (runKey.NullOrEmpty() || trackedGuests == null) return null;
            for (int i = 0; i < trackedGuests.Count; i++)
            {
                StoryTrackedGuestRecord r = trackedGuests[i];
                if (r != null && r.runKey == runKey && r.pawnIdNum == pawnIdNum) return r;
            }
            return null;
        }

        public static bool IsTrackedGuestPawn(int pawnIdNum)
        {
            return ActiveTrackedGuestPawnIds.Count > 0 && ActiveTrackedGuestPawnIds.Contains(pawnIdNum);
        }

        internal void NotifyGuestExitedMap(Pawn pawn, int mapId, int preExitLordId, bool badState)
        {
            if (pawn == null || mapId < 0 || trackedGuests == null) return;

            for (int i = 0; i < trackedGuests.Count; i++)
            {
                StoryTrackedGuestRecord rec = trackedGuests[i];
                if (rec == null || rec.pawnIdNum != pawn.thingIDNumber || rec.mapId != mapId) continue;

                bool lordMatches = (preExitLordId >= 0 && (preExitLordId == rec.visitLordId || preExitLordId == rec.exitLordId));
                if (!badState && lordMatches)
                {
                    rec.exitedPeacefully = true;
                }
                else
                {
                    rec.exitedHostileOrForced = true;
                }
            }
        }

        private void RebuildTrackedGuestCache()
        {
            ActiveTrackedGuestPawnIds.Clear();
            if (trackedGuests == null) return;
            for (int i = 0; i < trackedGuests.Count; i++)
            {
                if (trackedGuests[i] != null && trackedGuests[i].pawnIdNum >= 0)
                {
                    ActiveTrackedGuestPawnIds.Add(trackedGuests[i].pawnIdNum);
                }
            }
        }

        // =====================================================================
        // WORLD TICK & SCRIBE
        // =====================================================================

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;
            CheckOrphansNow();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref backups, "TMM_raidRelationBackups", LookMode.Deep);
            Scribe_Collections.Look(ref protectedItems, "TMM_protectedRetrieveItems", LookMode.Deep);
            Scribe_Collections.Look(ref trackedGuests, "TMM_trackedHotelGuests", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (backups == null) backups = new List<StoryFactionBackup>();
                if (protectedItems == null) protectedItems = new List<StoryProtectedItem>();
                if (trackedGuests == null) trackedGuests = new List<StoryTrackedGuestRecord>();
                RebuildProtectedCache();
                RebuildTrackedGuestCache();
            }
        }
    }

    public struct HotelExitCaptureState
    {
        public bool tracked;
        public int mapId;
        public int preExitLordId;
        public bool badState;
    }

    // =========================================================================
    // HARMONY PATCHES: ANTI-STACK-MERGE & CONFIRMED HOTEL GUEST EXITMAP
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class StoryRetrieveStackPatch
    {
        static StoryRetrieveStackPatch()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.storystages");

            MethodInfo canStack = AccessTools.Method(typeof(Thing), "CanStackWith", new Type[] { typeof(Thing) }); // CHECK API
            if (canStack != null)
            {
                harmony.Patch(canStack, postfix: new HarmonyMethod(typeof(StoryRetrieveStackPatch), "Postfix_CanStackWith"));
            }

            MethodInfo exitMap = AccessTools.Method(typeof(Pawn), "ExitMap", new Type[] { typeof(bool), typeof(Rot4) }); // CHECK API
            if (exitMap != null)
            {
                harmony.Patch(exitMap,
                    prefix: new HarmonyMethod(typeof(StoryRetrieveStackPatch), "Prefix_Pawn_ExitMap"),
                    postfix: new HarmonyMethod(typeof(StoryRetrieveStackPatch), "Postfix_Pawn_ExitMap"));
            }
        }

        public static void Postfix_CanStackWith(Thing __instance, Thing other, ref bool __result)
        {
            if (!__result) return;
            if (StoryRelationRestorer.IsProtectedThing(__instance) || StoryRelationRestorer.IsProtectedThing(other))
            {
                __result = false;
            }
        }

        public static void Prefix_Pawn_ExitMap(Pawn __instance, out HotelExitCaptureState __state)
        {
            __state = new HotelExitCaptureState();
            __state.tracked = false;
            __state.mapId = -1;
            __state.preExitLordId = -1;

            if (__instance == null || !__instance.Spawned || __instance.Map == null) return;
            if (!StoryRelationRestorer.IsTrackedGuestPawn(__instance.thingIDNumber)) return;

            Lord curLord = __instance.GetLord();
            __state.tracked = true;
            __state.mapId = __instance.Map.uniqueID;
            __state.preExitLordId = curLord != null ? curLord.loadID : -1;
            __state.badState = __instance.Dead || __instance.Destroyed || __instance.Downed ||
                               __instance.IsPrisonerOfColony || __instance.IsSlaveOfColony ||
                               __instance.HostileTo(Faction.OfPlayer) || __instance.InMentalState ||
                               StoryStage_HotelVisit.IsSentAwayByHospitality(__instance);
        }

        public static void Postfix_Pawn_ExitMap(Pawn __instance, HotelExitCaptureState __state)
        {
            if (!__state.tracked || __instance == null) return;
            // Confirm pawn actually left the map (in case another patch aborted ExitMap)
            if (__instance.Spawned && __instance.Map != null && __instance.Map.uniqueID == __state.mapId) return;

            StoryRelationRestorer inst = StoryRelationRestorer.Instance;
            if (inst != null)
            {
                inst.NotifyGuestExitedMap(__instance, __state.mapId, __state.preExitLordId, __state.badState);
            }
        }
    }
}
```

`Source/StoryPayment.cs`
```csharp
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
                    Log.Error("[TMM] StoryPayment: error while rolling back map journal entry: " + ex);
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
                    Log.Error("[TMM] StoryPayment: error while rolling back caravan journal entry: " + ex);
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
                    Log.Error("[TMM] StoryPayment: exception destroying committed chunk " + t.ThingID + " (payment already confirmed): " + ex);
                    if (staging.innerContainer.Contains(t))
                    {
                        staging.innerContainer.Remove(t);
                    }
                }
            }
        }
    }
}
```

`Source/OutpostPodLogistics.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public static class OutpostPodLogistics
    {
        public const float PodMassCapacity = 150f;
        public const float FuelPerTilePerPod = 2.25f;

        public static Map GetHomeMap()
        {
            if (Find.Maps == null) return null;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m != null && m.IsPlayerHome) return m;
            }
            return Find.AnyPlayerHomeMap;
        }

        /// <summary>
        /// Counts unforbidden spawned resources on map using the exact same selector policy
        /// as DeductResource and TryPayLaunchCost (via StoryPayment).
        /// </summary>
        public static int CountResource(Map map, ThingDef def)
        {
            return StoryPayment.CountOnMap(map, def, false);
        }

        /// <summary>
        /// Atomically deducts count of def from map via StoryPayment.
        /// On any failure, rolls back completely and returns 0 — NO non-atomic fallback loop.
        /// </summary>
        public static int DeductResource(Map map, ThingDef def, int count)
        {
            if (map == null || def == null || count <= 0) return 0;
            int deducted;
            if (StoryPayment.TryDeductFromMap(map, def, count, false, out deducted))
            {
                return deducted;
            }
            return 0;
        }

        public static int CalculatePodCount(float mass)
        {
            if (mass <= 0f) return 1;
            return Mathf.Max(1, Mathf.CeilToInt(mass / PodMassCapacity));
        }

        public static int CalculateTotalFuelNeeded(int originTile, int destTile, int podCount)
        {
            float dist = 1f;
            if (Find.WorldGrid != null && originTile >= 0 && destTile >= 0)
            {
                dist = Find.WorldGrid.ApproxDistanceInTiles(originTile, destTile);
            }
            dist = Mathf.Max(1f, dist);
            int fuelPerPod = Mathf.CeilToInt(dist * FuelPerTilePerPod);
            return fuelPerPod * Mathf.Max(1, podCount);
        }

        public static void CalculateCost(int originTile, int destTile, float mass, Map homeMap,
            out int totalFuelNeeded, out int chemfuelToDeduct, out int silverToDeduct, out bool canAfford, out string costSummary)
        {
            int pods = CalculatePodCount(mass);
            totalFuelNeeded = CalculateTotalFuelNeeded(originTile, destTile, pods);

            int availFuel = CountResource(homeMap, ThingDefOf.Chemfuel);
            int availSilver = CountResource(homeMap, ThingDefOf.Silver);

            chemfuelToDeduct = Mathf.Min(totalFuelNeeded, availFuel);
            int missingFuel = totalFuelNeeded - chemfuelToDeduct;

            float fuelPrice = ThingDefOf.Chemfuel != null ? ThingDefOf.Chemfuel.BaseMarketValue : 2.3f;
            silverToDeduct = missingFuel > 0 ? Mathf.CeilToInt((float)missingFuel * fuelPrice) : 0;

            canAfford = availSilver >= silverToDeduct;

            if (silverToDeduct > 0)
            {
                costSummary = "StrategicOutpost_PodCostWithSilver".Translate(totalFuelNeeded, chemfuelToDeduct, silverToDeduct, availFuel, availSilver);
            }
            else
            {
                costSummary = "StrategicOutpost_PodCostFuelOnly".Translate(totalFuelNeeded, availFuel);
            }
        }

        /// <summary>
        /// Atomically pays both chemfuel and silver launch costs in a single StoryPayment transaction.
        /// On any failure, deducts nothing and returns false with failReason.
        /// </summary>
        public static bool TryPayLaunchCost(Map homeMap, int originTile, int destTile, float mass, out string failReason)
        {
            if (homeMap == null)
            {
                failReason = "StrategicOutpost_NoHomeMapAvailable".Translate();
                return false;
            }

            int totalFuel, fuelDeduct, silverDeduct;
            bool canAfford;
            string summary;
            CalculateCost(originTile, destTile, mass, homeMap, out totalFuel, out fuelDeduct, out silverDeduct, out canAfford, out summary);

            if (!canAfford)
            {
                failReason = summary;
                return false;
            }

            List<StoryPayment.ResourceRequirement> reqs = new List<StoryPayment.ResourceRequirement>();
            if (fuelDeduct > 0)
            {
                reqs.Add(new StoryPayment.ResourceRequirement(ThingDefOf.Chemfuel, fuelDeduct));
            }
            if (silverDeduct > 0)
            {
                reqs.Add(new StoryPayment.ResourceRequirement(ThingDefOf.Silver, silverDeduct));
            }

            if (reqs.Count > 0 && !StoryPayment.TryDeductMultipleFromMap(homeMap, reqs, false))
            {
                failReason = summary;
                return false;
            }

            failReason = null;
            return true;
        }

        public static TravellingTransporters Launch(PlanetTile originTile, PlanetTile destTile, List<Thing> cargo, TransportersArrivalAction arrivalAction)
        {
            if (cargo == null || cargo.Count == 0) return null;

            List<ActiveTransporterInfo> infos = new List<ActiveTransporterInfo>();
            ActiveTransporterInfo currentInfo = new ActiveTransporterInfo();
            currentInfo.openDelay = 110;
            currentInfo.leaveSlag = true;
            currentInfo.savePawnsWithReferenceMode = true;
            float currentMass = 0f;

            for (int i = 0; i < cargo.Count; i++)
            {
                Thing t = cargo[i];
                if (t == null) continue;
                float itemMass = t.GetStatValue(StatDefOf.Mass) * (float)t.stackCount;

                if (currentMass + itemMass > PodMassCapacity && currentInfo.innerContainer.Count > 0)
                {
                    infos.Add(currentInfo);
                    currentInfo = new ActiveTransporterInfo();
                    currentInfo.openDelay = 110;
                    currentInfo.leaveSlag = true;
                    currentInfo.savePawnsWithReferenceMode = true;
                    currentMass = 0f;
                }

                currentInfo.innerContainer.TryAdd(t, false);
                currentMass += itemMass;
            }

            if (currentInfo.innerContainer.Count > 0)
            {
                infos.Add(currentInfo);
            }

            TravellingTransporters worldObj = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravellingTransporters);
            worldObj.SetFaction(Faction.OfPlayer);
            worldObj.destinationTile = destTile;
            worldObj.arrivalAction = arrivalAction;
            worldObj.Tile = originTile;
            Find.WorldObjects.Add(worldObj);

            for (int i = 0; i < infos.Count; i++)
            {
                worldObj.AddTransporter(infos[i], true);
            }

            return worldObj;
        }
    }

    public class TransportersArrivalAction_OutpostArrival : TransportersArrivalAction
    {
        public int targetTile = -1;
        public bool autoUpgrade = false;

        public TransportersArrivalAction_OutpostArrival()
        {
        }

        public TransportersArrivalAction_OutpostArrival(int targetTile, bool autoUpgrade = false)
        {
            this.targetTile = targetTile;
            this.autoUpgrade = autoUpgrade;
        }

        public override bool GeneratesMap
        {
            get { return false; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetTile, "targetTile", -1);
            Scribe_Values.Look(ref autoUpgrade, "autoUpgrade", false);
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            StrategicOutpost outpost = StrategicOutpostCaravanUtility.GetOutpostAtTile(destinationTile);
            if (outpost != null && outpost.Spawned) return true;
            return OutpostPodLogistics.GetHomeMap() != null;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            StrategicOutpost outpost = StrategicOutpostCaravanUtility.GetOutpostAtTile(tile);
            if (outpost == null || !outpost.Spawned)
            {
                Map home = OutpostPodLogistics.GetHomeMap();
                if (home == null) return;
                new TransportersArrivalAction_ColonyRecall(home.Tile).Arrived(transporters, home.Tile);
                Messages.Message("StrategicOutpost_PodsReturnedOutpostGone".Translate(), MessageTypeDefOf.NeutralEvent);
                return;
            }

            int colonistsAdded = 0;
            int silverAdded = 0;

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo info = transporters[i];
                if (info == null || info.innerContainer == null) continue;

                List<Thing> things = new List<Thing>();
                for (int k = 0; k < info.innerContainer.Count; k++)
                {
                    things.Add(info.innerContainer[k]);
                }

                for (int j = 0; j < things.Count; j++)
                {
                    Thing item = things[j];
                    if (item == null || item.Destroyed) continue;

                    info.innerContainer.Remove(item);

                    Pawn p = item as Pawn;
                    if (p != null)
                    {
                        bool added = p.RaceProps.Animal
                            ? outpost.Animals.TryAdd(p, false)
                            : outpost.Workers.TryAdd(p, false);
                        if (added)
                        {
                            if (!p.RaceProps.Animal)
                            {
                                if (p.ownership != null) p.ownership.UnclaimBed();
                                colonistsAdded++;
                            }
                            StrategicOutpost.SetStoredPawnProtection(p, true);
                        }
                        else
                        {
                            // Never leave pawn unowned if outpost refused TryAdd
                            Map fallbackHome = OutpostPodLogistics.GetHomeMap();
                            if (fallbackHome != null)
                            {
                                StoryPayment.TryReturnThingToMap(p, null, fallbackHome, DropCellFinder.TradeDropSpot(fallbackHome));
                            }
                            else if (Find.WorldPawns != null && !Find.WorldPawns.Contains(p))
                            {
                                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                            }
                        }
                    }
                    else
                    {
                        if (outpost.RawMaterials.TryAdd(item, false))
                        {
                            if (item.def == ThingDefOf.Silver)
                            {
                                silverAdded += item.stackCount;
                            }
                        }
                        else
                        {
                            Map fallbackHome = OutpostPodLogistics.GetHomeMap();
                            StoryPayment.TryReturnThingToMap(item, null, fallbackHome, fallbackHome != null ? DropCellFinder.TradeDropSpot(fallbackHome) : IntVec3.Invalid);
                        }
                    }
                }
            }

            Messages.Message("StrategicOutpost_PodsDeliveredToOutpost".Translate(outpost.LabelCap, colonistsAdded, silverAdded), outpost, MessageTypeDefOf.PositiveEvent);

            if (autoUpgrade && outpost.CanUpgrade)
            {
                string failReason;
                outpost.TryUpgrade(out failReason, null);
            }
        }
    }

    public class TransportersArrivalAction_ColonyRecall : TransportersArrivalAction
    {
        public int targetTile = -1;

        public TransportersArrivalAction_ColonyRecall()
        {
        }

        public TransportersArrivalAction_ColonyRecall(int targetTile)
        {
            this.targetTile = targetTile;
        }

        public override bool GeneratesMap
        {
            get { return false; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetTile, "targetTile", -1);
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            Map map = Current.Game != null ? Current.Game.FindMap(destinationTile) : null;
            if (map == null) map = OutpostPodLogistics.GetHomeMap();
            if (map == null)
            {
                return false;
            }
            return true;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            Map map = Current.Game != null ? Current.Game.FindMap(tile) : null;
            if (map == null) map = OutpostPodLogistics.GetHomeMap();
            if (map == null)
            {
                Log.Warning("TransportersArrivalAction_ColonyRecall: Target home map not found.");
                return;
            }

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            if (!dropSpot.IsValid)
            {
                dropSpot = DropCellFinder.RandomDropSpot(map);
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo info = transporters[i];
                if (info != null)
                {
                    for (int k = 0; k < info.innerContainer.Count; k++)
                    {
                        Pawn p = info.innerContainer[k] as Pawn;
                        if (p != null)
                        {
                            if (Find.WorldPawns != null && Find.WorldPawns.Contains(p))
                            {
                                Find.WorldPawns.RemovePawn(p);
                            }
                            if (p.Faction != Faction.OfPlayer)
                            {
                                p.SetFaction(Faction.OfPlayer);
                            }
                        }
                    }
                    DropPodUtility.MakeDropPodAt(dropSpot, map, info, Faction.OfPlayer);
                }
            }

            Messages.Message("StrategicOutpost_PodsArrivedAtColony".Translate(), new GlobalTargetInfo(dropSpot, map), MessageTypeDefOf.PositiveEvent);
        }
    }
}
```

`Source/StoryPawnTransfer.cs`
```csharp
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
                        Log.Error("[TMM] StoryPawnTransfer: rollback step " + step + " failed for " + pawn.LabelShort + ": " + ex);
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
                Log.Error("[TMM] StoryPawnTransfer: failed restoring faction leader: " + ex);
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
```

---

## Краткая сводка изменений (Задача 44)

1. **`Source/TestMode.cs` (новый):**
   - Добавлен кешированный флаг `TestMode.Enabled`, проверяющий аргумент запуска `-tmm-story-test`. Все тестовые хуки ядра (`StoryRelationRestorer.TestBlockRestoreOverride`, `StoryPayment.TestSplitOffOverride`, `TestRejectTryAddOverride`, `TestBeforeDestroyOverride`, `StoryPawnTransfer.TestSpawnOverride`) работают **только** при `TestMode.Enabled == true`.
2. **`Source/StoryRelationRestorer.cs`:**
   - **Симметричный запасной откат + уведомления:** когда `TryAffectGoodwillWith` заблокирован, обновляются обе стороны (`fac.RelationWith(player, true)` и `player.RelationWith(fac, true)` — и `baseGoodwill`, и `kind`), а при смене `kind` вызывается `Notify_RelationKindChanged` на обеих фракциях.
   - **Удаление backup только после проверки:** `FinalizeBackup` возвращает `bool` после `VerifyRestored` по обеим сторонам; при сбое запись остаётся в `backups` и повторяется при следующей сверке `CheckOrphansNow()`.
   - **Перекрывающиеся аренды перемирия:** `AcquireTruceLease` безопасно вызывается для любой фракции визита; если для фракции уже есть временная запись нейтралитета (`ModeNeutral`), второй визит гарантированно добавляет свою аренду в `leases`, и вражда возвращается только при освобождении последней аренды. Добавлены `HasActiveTruceBackup` и `HasPendingBackupForFaction`.
   - **Откат несостоявшегося рейда независимо от `restoreRelations`:** добавлен `AbortLease(string leaseId)` и флаг `anyLeaseCompletedKeepHostile` на `StoryFactionBackup`. При провале спавна/исключении `AbortLease` всегда откатывает принудительную враждебность (если другая параллельная аренда не завершилась штатно с `restoreRelations=false`).
   - **Подтверждённый `ExitMap`:** патч `Pawn.ExitMap` переведён на пару `Prefix` + `Postfix` — мирный уход гостя фиксируется только после фактического снятия пешки с карты визита.
3. **`Source/StoryPayment.cs`:**
   - Введён журнал `JournalEntry` до каждой мутации (`SplitOff`, `Remove`, `TryAdd`) с обратным откатом.
   - Каждый шаг отката проверяется (`TryReturnThingToMap` / `TryReturnThingToCaravan`): при отказе `TryAdd` вещь переносится в резервный holder (ближайшая клетка карты / инвентарь колониста / другая пешка того же каравана / домашняя карта).
   - Полная сумма в `StagingHolder` перепроверяется перед commit; опциональный колбэк `onPaymentSecured` вызывается до необратимого `Destroy` (его исключение даёт 100% откат), а само уничтожение обёрнуто поэлементно в `try/catch`, чтобы исключение в `Destroy` не оставило частично списанную оплату без фиксации.
   - Добавлены общие методы для карты (`CountOnMap`, `TryDeductFromMap`, `TryDeductMultipleFromMap`) и атомарного переноса из каравана в целевой `ThingOwner` (`TryTransferFromCaravanToOwner`) для задач 45–47.
   - `TestSplitOffOverride` (и вспомогательные тестовые хуки) работают только при `TestMode.Enabled`.
4. **`Source/OutpostPodLogistics.cs`:**
   - Убран неатомарный обходной цикл в `DeductResource` и `TryPayLaunchCost`.
   - `CountResource`, `DeductResource` и `TryPayLaunchCost` используют единую политику отбора (`StoryPayment.CountOnMap` / `TryDeductFromMap` / `TryDeductMultipleFromMap` с `requirePlayerStorage = false`): при любом отказе — полный откат и `0` / `false` без частичного списания.
   - В `TransportersArrivalAction_OutpostArrival` добавлена проверка возврата `TryAdd` с безопасным возвратом при отказе контейнера.
5. **`Source/StoryPawnTransfer.cs` (новый):**
   - Реализован общий журнальный перенос свободной мировой пешки на карту с проверками владельца, полным снимком исходного состояния (фракция включая `null`, гостевой статус, лидерство, `WorldPawns`), снятием с `WorldPawns` последним шагом перед `GenSpawn.Spawn`, полным обратным откатом при `false` или исключении и отложенной финализацией лидерства через `PawnTransferHandle`.

---

## Публичный API ядра для задач 45–47

### `TestMode`
- `public static bool Enabled { get; }`

### `StoryRelationRestorer` (новые/дополненные методы; прежний API сохранён)
- `public bool AcquireHostilityLease(Faction fac, StoryQuestRun run, bool restoreRelations, out string leaseId)`
- `public bool AcquireTruceLease(Faction fac, StoryQuestRun run, out string leaseId)` *(вызывать для любой фракции визита, включая временно нейтральную)*
- `public void ReleaseLease(string leaseId)` *(штатное завершение)*
- `public void AbortLease(string leaseId)` *(откат при срыве спавна/исключении, независимо от `restoreRelations`)*
- `public bool HasActiveTruceBackup(Faction fac)`
- `public bool HasPendingBackupForFaction(Faction fac)`
- `public static Func<Faction, bool> TestBlockRestoreOverride` *(только при `TestMode.Enabled`)*

### `StoryPayment` (прежний API сохранён + новые перегрузки и перенос)
- `public static bool IsInPlayerStorage(Map map, Thing t)`
- `public static bool IsEligibleMapItem(Map map, Thing t, bool requirePlayerStorage)`
- `public static int CountInColonyStockpiles(Map map, ThingDef def)`
- `public static int CountOnMap(Map map, ThingDef def, bool requirePlayerStorage)`
- `public static bool TryDeductFromColonyStockpiles(Map map, ThingDef def, int requiredCount, out int deducted)`
- `public static bool TryDeductFromColonyStockpiles(Map map, ThingDef def, int requiredCount, out int deducted, Action onPaymentSecured)`
- `public static bool TryDeductFromMap(Map map, ThingDef def, int requiredCount, bool requirePlayerStorage, out int deducted, Action onPaymentSecured = null)`
- `public static bool TryDeductMultipleFromColonyStockpiles(Map map, List<ResourceRequirement> requirements)`
- `public static bool TryDeductMultipleFromColonyStockpiles(Map map, List<ResourceRequirement> requirements, Action onPaymentSecured)`
- `public static bool TryDeductMultipleFromMap(Map map, List<ResourceRequirement> requirements, bool requirePlayerStorage, Action onPaymentSecured = null)`
- `public static int CountInCaravanDirectInventory(Caravan caravan, ThingDef def)`
- `public static bool TryDeductFromCaravan(Caravan caravan, ThingDef def, int requiredCount, out int deducted)`
- `public static bool TryDeductFromCaravan(Caravan caravan, ThingDef def, int requiredCount, out int deducted, Action onPaymentSecured)`
- `public static bool TryTransferFromCaravanToOwner(Caravan caravan, ThingDef def, int requiredCount, ThingOwner targetOwner, Dictionary<int, int> maxTakeByPawnId, out int transferred, out string failReason, Action onTransferCommitted = null)`
- `public static bool TryReturnThingToMap(Thing chunk, Thing originalStack, Map map, IntVec3 pos)`
- `public static bool TryReturnThingToCaravan(Thing chunk, Thing originalStack, Pawn origPawn, Caravan caravan)`
- Тестовые хуки (только при `TestMode.Enabled`): `TestSplitOffOverride`, `TestRejectTryAddOverride`, `TestBeforeDestroyOverride`

### `StoryPawnTransfer`
- `public static bool IsFreeWorldPawn(Pawn pawn, Faction requiredCurrentFaction, out string failReason)`
- `public static bool CanTransferFreeWorldPawn(Pawn pawn, Map targetMap, IntVec3 targetCell, Faction requiredCurrentFaction, out string failReason)`
- `public static bool TrySpawnFreeWorldPawn(Pawn pawn, Map targetMap, IntVec3 targetCell, Faction targetFaction, bool changeFaction, out string failReason)`
- `public static bool TrySpawnFreeWorldPawn(Pawn pawn, Map targetMap, IntVec3 targetCell, Faction targetFaction, bool changeFaction, Faction imprisonForFaction, Faction requiredCurrentFaction, Func<Pawn, bool> postSpawnSetup, bool autoFinalize, out PawnTransferHandle handle, out string failReason)`
- `PawnTransferHandle`: `Pawn Pawn`, `bool IsRolledBack`, `bool Rollback()`, `void FinalizeTransfer()`
- Тестовый хук (только при `TestMode.Enabled`): `TestSpawnOverride`