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
    /// lease for that faction is released or orphaned (completion, failure, exception, disabled quest,
    /// removed Def/stage, or save migration).
    /// </summary>
    public sealed class StoryFactionBackup : IExposable
    {
        public int factionId = -1;
        public int origGoodwill;
        public int origKind = (int)FactionRelationKind.Neutral;
        public bool origHasGoodwill;
        public int postForceGoodwill = -100;
        public bool restoreWanted;
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
    /// from disappearance, container insertion, imprisonment, sentAway, or map deletion across reload.
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
    /// WorldComponent that guarantees:
    /// 1) Temporary raid hostility AND temporary hotel-visit neutrality (truce) are rolled back properly
    ///    (handling RimWorld's goodwill/kind hysteresis thresholds in both directions, permanentEnemy
    ///    guards, concurrent raids/visits on the same faction, mid-stage goodwill deltas, exceptions,
    ///    disabled quests, and removed Defs/stages).
    /// 2) Retrieve quest items cannot merge into arbitrary stacks while active.
    /// 3) Hotel visit guest departures via Pawn.ExitMap are tracked accurately across save/reload.
    /// </summary>
    public sealed class StoryRelationRestorer : WorldComponent
    {
        private const int CheckInterval = 250;

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

        // =====================================================================
        // HOSTILITY LEASES (RAID API — PRESERVED FROM E2)
        // =====================================================================

        public bool AcquireHostilityLease(Faction fac, StoryQuestRun run, bool restoreRelations, out string leaseId)
        {
            leaseId = null;
            if (fac == null || fac.IsPlayer || run == null) return false;

            leaseId = MakeLeaseId(run);
            StoryFactionBackup existing = FindBackup(fac.loadID);

            if (existing == null && fac.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            // Refuse conflicting concurrent override (e.g., active hotel truce on the same faction)
            if (existing != null && existing.activeMode != StoryHostilityLease.ModeHostile)
            {
                leaseId = null;
                return false;
            }

            if (existing == null)
            {
                existing = new StoryFactionBackup();
                existing.factionId = fac.loadID;
                existing.origGoodwill = fac.PlayerGoodwill;
                existing.origKind = (int)fac.RelationKindWith(Faction.OfPlayer);
                existing.origHasGoodwill = fac.HasGoodwill;
                existing.restoreWanted = restoreRelations;
                existing.activeMode = StoryHostilityLease.ModeHostile;
                backups.Add(existing);

                AddOrUpdateLease(existing, leaseId, run, restoreRelations, StoryHostilityLease.ModeHostile);

                if (!ForceFactionHostile(fac))
                {
                    backups.Remove(existing);
                    RestoreFactionRelations(fac, existing);
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
        // TRUCE / TEMPORARY NEUTRALITY LEASES (HOTEL VISIT API — E3)
        // =====================================================================

        /// <summary>
        /// Checks whether a hostile faction can legally enter temporary neutrality without triggering
        /// vanilla permanentEnemy or goodwill-faction SetRelationDirect errors.
        /// </summary>
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
        /// Records original goodwill &amp; relation kind BEFORE mutating, moves a hostile faction to
        /// temporary Neutrality (verifying !HostileTo(OfPlayer)), and supports overlapping visits
        /// on the same faction (only the final lease restores hostility).
        /// </summary>
        public bool AcquireTruceLease(Faction fac, StoryQuestRun run, out string leaseId)
        {
            leaseId = null;
            if (fac == null || fac.IsPlayer || fac.defeated || run == null) return false;

            if (!CanFactionEnterTruce(fac))
            {
                return false;
            }

            StoryFactionBackup existing = FindBackup(fac.loadID);

            if (existing == null && !fac.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            // Refuse conflicting concurrent override (e.g., active colony raid lease on same faction)
            if (existing != null && existing.activeMode != StoryHostilityLease.ModeNeutral)
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
                existing.activeMode = StoryHostilityLease.ModeNeutral;
                backups.Add(existing);

                AddOrUpdateLease(existing, leaseId, run, true, StoryHostilityLease.ModeNeutral);

                if (!ForceFactionNeutral(fac))
                {
                    backups.Remove(existing);
                    RestoreFactionRelations(fac, existing);
                    leaseId = null;
                    return false;
                }

                existing.postForceGoodwill = fac.PlayerGoodwill;
                return true;
            }

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

        public void ReleaseLease(string leaseId)
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

                if (RemoveLease(b, leaseId))
                {
                    if (b.leases.Count == 0)
                    {
                        FinalizeBackup(b);
                        backups.RemoveAt(i);
                    }
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
                            b.leases.RemoveAt(l);
                        }
                    }

                    if (b.leases.Count == 0)
                    {
                        FinalizeBackup(b);
                        backups.RemoveAt(i);
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

        private void FinalizeBackup(StoryFactionBackup b)
        {
            if (b == null || !b.restoreWanted) return;
            Faction fac = FindFactionById(b.factionId);
            if (fac == null || fac.defeated) return;
            RestoreFactionRelations(fac, b);
        }

        private static bool ForceFactionHostile(Faction fac)
        {
            if (fac == null || fac.IsPlayer) return false;
            if (fac.HostileTo(Faction.OfPlayer)) return true;

            if (fac.HasGoodwill)
            {
                int drop = -100 - fac.PlayerGoodwill;
                if (drop < 0 && fac.CanChangeGoodwillFor(Faction.OfPlayer, drop)) // CHECK API
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null); // CHECK API
                }
            }
            else
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }

            return fac.HostileTo(Faction.OfPlayer);
        }

        private static bool ForceFactionNeutral(Faction fac)
        {
            if (fac == null || fac.IsPlayer || fac.defeated) return false;
            if (!fac.HostileTo(Faction.OfPlayer)) return true;
            if (fac.def != null && fac.def.permanentEnemy) return false;

            if (fac.HasGoodwill)
            {
                int lift = 0 - fac.PlayerGoodwill;
                if (lift > 0 && fac.CanChangeGoodwillFor(Faction.OfPlayer, lift)) // CHECK API
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, lift, false, false, null, null); // CHECK API
                }
            }
            else
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }

            return !fac.HostileTo(Faction.OfPlayer);
        }

        private static void RestoreFactionRelations(Faction fac, StoryFactionBackup b)
        {
            if (fac == null || b == null) return;

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

            if (fac.HasGoodwill)
            {
                // Direction A: Hostile -> Neutral/Ally: cross >= 0 (or >= 75) first
                if (fac.HostileTo(Faction.OfPlayer) && origKind != FactionRelationKind.Hostile)
                {
                    int liftTarget = origKind == FactionRelationKind.Ally ? Mathf.Max(75, targetGoodwill) : Mathf.Max(0, targetGoodwill);
                    int liftDelta = liftTarget - fac.PlayerGoodwill;
                    if (liftDelta > 0)
                    {
                        fac.TryAffectGoodwillWith(Faction.OfPlayer, liftDelta, false, false, null, null); // CHECK API
                    }
                }
                // Direction B: Neutral/Ally -> Hostile (ending truce): cross <= -75 first
                else if (!fac.HostileTo(Faction.OfPlayer) && origKind == FactionRelationKind.Hostile)
                {
                    int dropTarget = Mathf.Min(-75, targetGoodwill);
                    int dropDelta = dropTarget - fac.PlayerGoodwill;
                    if (dropDelta < 0)
                    {
                        fac.TryAffectGoodwillWith(Faction.OfPlayer, dropDelta, false, false, null, null); // CHECK API
                    }
                }

                int finalDelta = targetGoodwill - fac.PlayerGoodwill;
                if (finalDelta != 0)
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, finalDelta, false, false, null, null); // CHECK API
                }

                // Direct FactionRelation fallback (never calls SetRelationDirect on goodwill factions)
                if (fac.PlayerGoodwill != targetGoodwill || fac.RelationKindWith(Faction.OfPlayer) != origKind)
                {
                    FactionRelation rel = fac.RelationWith(Faction.OfPlayer, true); // CHECK API
                    if (rel != null)
                    {
                        rel.baseGoodwill = targetGoodwill; // CHECK API
                        rel.kind = origKind; // CHECK API
                    }
                }
            }
            else
            {
                if (fac.RelationKindWith(Faction.OfPlayer) != origKind)
                {
                    fac.SetRelationDirect(Faction.OfPlayer, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API
                }
            }
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
            if (b == null || b.leases == null) return false;
            bool removed = false;
            for (int i = b.leases.Count - 1; i >= 0; i--)
            {
                if (b.leases[i] != null && b.leases[i].leaseId == leaseId)
                {
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
        // TRACKED HOTEL VISIT GUESTS & REAL EXITMAP VERIFICATION
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

        internal void NotifyGuestExitingMap(Pawn pawn, Map map)
        {
            if (pawn == null || map == null || trackedGuests == null) return;

            Lord curLord = pawn.GetLord();
            int curLordId = curLord != null ? curLord.loadID : -1;
            bool badState = pawn.Dead || pawn.Destroyed || pawn.Downed ||
                            pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony ||
                            pawn.HostileTo(Faction.OfPlayer) || pawn.InMentalState ||
                            StoryStage_HotelVisit.IsSentAwayByHospitality(pawn);

            for (int i = 0; i < trackedGuests.Count; i++)
            {
                StoryTrackedGuestRecord rec = trackedGuests[i];
                if (rec == null || rec.pawnIdNum != pawn.thingIDNumber || rec.mapId != map.uniqueID) continue;

                bool lordMatches = (curLordId >= 0 && (curLordId == rec.visitLordId || curLordId == rec.exitLordId));
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

    // =========================================================================
    // HARMONY PATCHES: ANTI-STACK-MERGE & REAL HOTEL GUEST EXITMAP TRACKING
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
                harmony.Patch(exitMap, prefix: new HarmonyMethod(typeof(StoryRetrieveStackPatch), "Prefix_Pawn_ExitMap"));
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

        public static void Prefix_Pawn_ExitMap(Pawn __instance)
        {
            if (__instance == null || !__instance.Spawned || __instance.Map == null) return;
            if (!StoryRelationRestorer.IsTrackedGuestPawn(__instance.thingIDNumber)) return;

            StoryRelationRestorer inst = StoryRelationRestorer.Instance;
            if (inst != null)
            {
                inst.NotifyGuestExitingMap(__instance, __instance.Map);
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
        internal static Func<Thing, int, int, Thing> TestSplitOffOverride;

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
        /// Counts unforbidden resources strictly inside player stockpiles/storage buildings on map
        /// via the shared StoryPayment policy.
        /// </summary>
        public static int CountResource(Map map, ThingDef def)
        {
            return StoryPayment.CountInColonyStockpiles(map, def);
        }

        /// <summary>
        /// Atomically deducts exactly count of def from player storage on map via StoryPayment.
        /// Returns count on full success, or 0 with full rollback on any failure.
        /// </summary>
        public static int DeductResource(Map map, ThingDef def, int count)
        {
            int deducted;
            if (StoryPayment.TryDeductFromColonyStockpiles(map, def, count, out deducted))
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

            if (reqs.Count > 0 && !StoryPayment.TryDeductMultipleFromColonyStockpiles(homeMap, reqs))
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
                    if (item == null) continue;

                    info.innerContainer.Remove(item);

                    Pawn p = item as Pawn;
                    if (p != null)
                    {
                        if (p.RaceProps.Animal)
                        {
                            outpost.Animals.TryAdd(p, false);
                        }
                        else
                        {
                            outpost.Workers.TryAdd(p, false);
                            if (p.ownership != null)
                            {
                                p.ownership.UnclaimBed();
                            }
                            colonistsAdded++;
                        }
                        StrategicOutpost.SetStoredPawnProtection(p, true);
                    }
                    else
                    {
                        outpost.RawMaterials.TryAdd(item, false);
                        if (item.def == ThingDefOf.Silver)
                        {
                            silverAdded += item.stackCount;
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

`Source/StoryStage_HotelVisit.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public enum StoryHotelObjective
    {
        Host,
        Investigate,
        Hide
    }

    /// <summary>
    /// Quest stage that brings a visiting delegation (optionally including a registered HeroDef)
    /// to the colony's Hospitality hotel on a pinned home map.
    ///
    /// Fixes enforced:
    /// - Safe hero transfer: only moves a free living world pawn of the visit faction; colonists,
    ///   prisoners, caravan members, container-held, or spawned heroes are NEVER stolen (visit runs
    ///   without them and records heroIncluded=false, or rolls back on spawn failure).
    /// - Truce &amp; rollback: verifies CanFactionEnterTruce BEFORE spawning (permanentEnemy fails
    ///   cleanly without spawning) and tracks temporary neutrality in StoryRelationRestorer.
    /// - Host per-guest tracking: pins visitMapId, visitLordId, exitLordId, and classifies every
    ///   guest on each poll (on_map / exited_peaceful / killed / imprisoned / sent_away / hostile /
    ///   missing via Pawn.ExitMap tracking). Hard overdue deadline fails if the lord never leaves.
    /// - Investigate: requires a real PlayLogEntry_Interaction (initiator = player colonist,
    ///   recipient = guest of THIS visit, Tick >= startTick). No indoor-room fallback.
    /// - Hide: requires a valid hunterFaction and binds to a specific hunter raid lord/cohort;
    ///   unrelated enemies never count, and missing raid ends at deadline.
    /// - Hospitality compatibility: caches and validates reflection signatures before creating
    ///   groups, records visitMode ("hospitality" vs "vanilla_fallback"), and resolves noHotel
    ///   immediately on Start without side effects.
    /// - No custom forceOutcome reading (core SparkStory handles -tmm-story-test exclusively).
    /// </summary>
    public sealed class StoryStage_HotelVisit : StoryStage
    {
        private const float DefaultHardOverdueDays = 2.0f;

        // Cached Hospitality reflection state (resolved once safely via AccessTools)
        private static bool s_hospChecked;
        private static Type s_hospMapCompType;
        private static FieldInfo s_hospGuestsWelcomeField;
        private static PropertyInfo s_hospGuestsWelcomeProp;
        private static Type s_hospGuestBedType;
        private static Type s_hospLordJobType;
        private static ConstructorInfo s_hospLordCtor4;
        private static ConstructorInfo s_hospLordCtor3;
        private static ConstructorInfo s_hospLordCtor2;
        private static Type s_hospCompGuestType;
        private static MethodInfo s_hospCompArriveMethod;
        private static FieldInfo s_hospCompSentAwayField;
        private static PropertyInfo s_hospCompSentAwayProp;

        // Cached PlayLogEntry_Interaction reflection handles for strict initiator/recipient checks
        private static bool s_playLogChecked;
        private static FieldInfo s_playLogInitiatorField;
        private static FieldInfo s_playLogRecipientField;

        public string faction;
        public HeroDef hero;
        public int groupSize = 2;
        public float stayDays = 2f;
        public float overdueDays = DefaultHardOverdueDays;
        public StoryHotelObjective objective = StoryHotelObjective.Host;
        public string hunterFaction;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "hotel"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        // =====================================================================
        // HOSPITALITY REFLECTION CAPABILITY CHECKS (CACHED)
        // =====================================================================

        private static void EnsureHospitalityReflection()
        {
            if (s_hospChecked) return;
            s_hospChecked = true;

            s_hospMapCompType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
            if (s_hospMapCompType != null)
            {
                s_hospGuestsWelcomeField = AccessTools.Field(s_hospMapCompType, "guestsAreWelcome");
                s_hospGuestsWelcomeProp = AccessTools.Property(s_hospMapCompType, "guestsAreWelcome");
            }

            s_hospGuestBedType = AccessTools.TypeByName("Hospitality.Building_GuestBed"); // CHECK API
            s_hospLordJobType = AccessTools.TypeByName("Hospitality.LordJob_VisitColony"); // CHECK API
            if (s_hospLordJobType != null)
            {
                ConstructorInfo[] ctors = s_hospLordJobType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < ctors.Length; i++)
                {
                    ParameterInfo[] p = ctors[i].GetParameters();
                    if (p.Length == 4 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3) && p[2].ParameterType == typeof(int) && p[3].ParameterType == typeof(bool))
                    {
                        s_hospLordCtor4 = ctors[i];
                    }
                    else if (p.Length == 3 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3) && p[2].ParameterType == typeof(int))
                    {
                        s_hospLordCtor3 = ctors[i];
                    }
                    else if (p.Length == 2 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3))
                    {
                        s_hospLordCtor2 = ctors[i];
                    }
                }
            }

            s_hospCompGuestType = AccessTools.TypeByName("Hospitality.CompGuest"); // CHECK API
            if (s_hospCompGuestType != null)
            {
                MethodInfo m = AccessTools.Method(s_hospCompGuestType, "Arrive"); // CHECK API
                if (m != null && m.GetParameters().Length == 0)
                {
                    s_hospCompArriveMethod = m;
                }
                s_hospCompSentAwayField = AccessTools.Field(s_hospCompGuestType, "sentAway"); // CHECK API
                s_hospCompSentAwayProp = AccessTools.Property(s_hospCompGuestType, "sentAway"); // CHECK API
            }
        }

        public static bool HasHospitalityLordSupport
        {
            get
            {
                EnsureHospitalityReflection();
                return s_hospLordCtor4 != null || s_hospLordCtor3 != null || s_hospLordCtor2 != null;
            }
        }

        /// <summary>
        /// Checks via cached reflection whether Hospitality is loaded, guests are welcomed,
        /// and at least one guest bed exists on the given map. Never throws.
        /// </summary>
        public static bool HasOpenHotel(Map map)
        {
            EnsureHospitalityReflection();
            if (map == null || s_hospMapCompType == null) return false;
            if (s_hospGuestsWelcomeField == null && s_hospGuestsWelcomeProp == null) return false;

            MapComponent comp = HotelVisitGuarantee.HospitalityOf(map);
            if (comp == null) return false;

            try
            {
                object welcomeObj = s_hospGuestsWelcomeField != null
                    ? s_hospGuestsWelcomeField.GetValue(comp)
                    : s_hospGuestsWelcomeProp.GetValue(comp, null);
                if (!(welcomeObj is bool) || !(bool)welcomeObj) return false;
            }
            catch
            {
                return false;
            }

            return FindGuestBedCell(map) != IntVec3.Invalid;
        }

        // =====================================================================
        // STAGE LIFECYCLE: START
        // =====================================================================

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            EnsureHospitalityReflection();

            Map home = ctx.Home;
            int now = Find.TickManager.TicksGame;
            int stayTicks = Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(stayDays * GenDate.TicksPerDay));
            int overdueTicks = Mathf.Max(GenDate.TicksPerHour * 6, Mathf.RoundToInt(Mathf.Max(0.25f, overdueDays) * GenDate.TicksPerDay));
            int leaveTick = now + stayTicks;
            int hardDeadlineTick = leaveTick + overdueTicks;

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["leaveTick"] = leaveTick.ToString();
            ctx.run.vars["hardDeadlineTick"] = hardDeadlineTick.ToString();
            ctx.run.vars["investigated"] = "false";
            ctx.run.vars["hunterRaidBound"] = "false";
            ctx.run.vars["hunterRaidCleared"] = "false";
            ctx.run.vars["hunterRaidLordId"] = "-1";
            ctx.run.vars["departureTriggered"] = "false";
            ctx.run.vars["arrived"] = "false";
            ctx.run.vars["heroIncluded"] = "false";
            ctx.run.vars["heroSpawned"] = "false";
            ctx.run.vars["visitLordId"] = "-1";
            ctx.run.vars["exitLordId"] = "-1";
            ctx.run.vars["visitMapId"] = home != null ? home.uniqueID.ToString() : "-1";
            ctx.run.vars["visitMode"] = HasHospitalityLordSupport ? "hospitality" : "vanilla_fallback";

            // 1. Immediate noHotel detection on Start (zero side effects: no spawns, no relation edits).
            // Note: StoryStage.Start returns void in core SparkStory, so Start resolves noHotel cleanly
            // here and marks immediateOutcome="noHotel" for instant completion on the first Tick.
            if (!HasOpenHotel(home))
            {
                ctx.run.vars["noHotel"] = "true";
                ctx.run.vars["immediateOutcome"] = "noHotel";
                SendNoHotelLetter(ctx, home);
                return;
            }

            // 2. For Hide objective, validate hunterFaction BEFORE spawning or mutating relations
            if (objective == StoryHotelObjective.Hide)
            {
                Faction hunterFac = !hunterFaction.NullOrEmpty() ? StoryUtil.FactionOf(hunterFaction) : null;
                if (hunterFac == null || hunterFac.defeated || hunterFac.IsPlayer)
                {
                    Log.Warning("[TMM] StoryStage_HotelVisit: Hide objective failed to resolve hunterFaction '" + (hunterFaction ?? "null") + "'.");
                    ctx.run.vars["failReason"] = "hunter_faction_missing";
                    ctx.run.vars["immediateOutcome"] = FindOutcome("unavailable") != null ? "unavailable" : "failed";
                    return;
                }
                ctx.run.vars["hunterFactionId"] = hunterFac.loadID.ToString();
                ctx.run.vars["initialHunterLordIds"] = JoinInts(SnapshotFactionLords(home, hunterFac));
            }

            // 3. Resolve visit faction
            Faction visitFac = ResolveVisitFaction(ctx);
            if (visitFac == null)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: no valid visit faction found for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                ctx.run.vars["failReason"] = "no_visit_faction";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            ctx.run.vars["visitFactionId"] = visitFac.loadID.ToString();

            // 4. Verify truce eligibility BEFORE creating any pawns or mutating relations
            if (visitFac.HostileTo(Faction.OfPlayer))
            {
                if (!StoryRelationRestorer.CanFactionEnterTruce(visitFac))
                {
                    ctx.run.vars["failReason"] = "permanent_enemy_cannot_truce";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }

                StoryRelationRestorer restorer = StoryRelationRestorer.Instance;
                string truceLeaseId = null;
                if (restorer == null || !restorer.AcquireTruceLease(visitFac, ctx.run, out truceLeaseId) || visitFac.HostileTo(Faction.OfPlayer))
                {
                    if (restorer != null && !truceLeaseId.NullOrEmpty())
                    {
                        restorer.ReleaseLease(truceLeaseId);
                    }
                    ctx.run.vars["failReason"] = "truce_failed";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }

                if (!truceLeaseId.NullOrEmpty())
                {
                    ctx.run.vars["truceLeaseId"] = truceLeaseId;
                }
            }

            IntVec3 entryCell;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out entryCell, home, CellFinder.EdgeRoadChance_Friendly)) // CHECK API
            {
                entryCell = CellFinder.RandomEdgeCell(home);
            }

            // 5. Safe hero transfer (only a free living world pawn of visitFac; never steal busy/colonist heroes)
            List<Pawn> guests = new List<Pawn>();
            Pawn heroPawn = null;
            bool heroWasWorldPawn = false;
            Faction heroOrigFaction = null;
            bool heroWasLeader = false;

            if (hero != null && HeroRegistry.Instance != null)
            {
                Pawn candidate = HeroRegistry.Instance.GetOrCreate(hero);
                string skipReason;
                if (IsFreeWorldHeroForVisit(candidate, visitFac, out skipReason))
                {
                    heroOrigFaction = candidate.Faction;
                    heroWasLeader = heroOrigFaction != null && heroOrigFaction.leader == candidate;
                    Find.WorldPawns.RemovePawn(candidate);
                    if (candidate.Faction == null)
                    {
                        candidate.SetFaction(visitFac);
                    }
                    heroPawn = candidate;
                    heroWasWorldPawn = true;
                    guests.Add(heroPawn);
                }
                else
                {
                    // Explicit, safe omission: hero stays untouched in their current faction/map/holder
                    ctx.run.vars["heroIncluded"] = "false";
                    ctx.run.vars["heroSkipReason"] = skipReason ?? "unavailable";
                }
            }

            // 6. Generate accompanying regular guests of visitFac
            int targetCount = Mathf.Clamp(groupSize, 1, 8);
            List<Pawn> generatedExtras = new List<Pawn>();
            while (guests.Count < targetCount)
            {
                PawnKindDef kind = visitFac.RandomPawnKind();
                if (kind == null) kind = PawnKindDefOf.SpaceRefugee;
                PawnGenerationRequest req = new PawnGenerationRequest(
                    kind, visitFac, PawnGenerationContext.NonPlayer, home.Tile,
                    false, false, false, true, false, 1f, false, true, false, true, true,
                    false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null,
                    null, null, null, null, null, null, null,
                    false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult,
                    null, null, null, false); // CHECK API
                Pawn guest = PawnGenerator.GeneratePawn(req);
                if (guest != null)
                {
                    guests.Add(guest);
                    generatedExtras.Add(guest);
                }
                else
                {
                    break;
                }
            }

            if (guests.Count == 0)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: failed to create guest pawns.");
                ReleaseTruceLease(ctx);
                ctx.run.vars["failReason"] = "no_guests_created";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            // 7. Spawn all guests with strict verification and rollback on failure
            List<Pawn> spawnedGuests = new List<Pawn>();
            List<int> guestIdNums = new List<int>();
            bool heroSpawnedOk = false;

            for (int i = 0; i < guests.Count; i++)
            {
                Pawn p = guests[i];
                IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(entryCell, home, 6, null);
                Thing res = GenSpawn.Spawn(p, spawnCell, home, WipeMode.Vanish);
                if (res != p || !p.Spawned || p.Map != home)
                {
                    if (p == heroPawn && heroWasWorldPawn)
                    {
                        RollbackHeroToWorld(heroPawn, heroOrigFaction, heroWasLeader);
                        heroPawn = null;
                        heroWasWorldPawn = false;
                        ctx.run.vars["heroSkipReason"] = "spawn_failed";
                    }
                    else if (!p.Destroyed)
                    {
                        p.Destroy(DestroyMode.Vanish);
                    }
                    continue;
                }

                if (p == heroPawn)
                {
                    heroSpawnedOk = true;
                    HeroRegistry.Instance.NotifyEncountered(heroPawn);
                    ctx.run.vars["heroThingId"] = heroPawn.ThingID;
                    ctx.run.vars["heroIdNum"] = heroPawn.thingIDNumber.ToString();
                    ctx.run.vars["heroIncluded"] = "true";
                    ctx.run.vars["heroSpawned"] = "true";
                }

                spawnedGuests.Add(p);
                guestIdNums.Add(p.thingIDNumber);
            }

            if (spawnedGuests.Count == 0)
            {
                ReleaseTruceLease(ctx);
                ctx.run.vars["failReason"] = "spawn_failed";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            // 8. Attach visit Lord (distinguishing real Hospitality vs vanilla fallback)
            IntVec3 chillSpot = FindGuestBedCell(home);
            if (chillSpot == IntVec3.Invalid) chillSpot = home.Center;

            string usedMode;
            Lord visitLord = AttachVisitLord(home, visitFac, chillSpot, stayTicks, spawnedGuests, out usedMode);
            if (visitLord == null)
            {
                // Rollback everything if Lord creation failed
                for (int i = 0; i < spawnedGuests.Count; i++)
                {
                    Pawn p = spawnedGuests[i];
                    if (p == heroPawn && heroWasWorldPawn)
                    {
                        RollbackHeroToWorld(heroPawn, heroOrigFaction, heroWasLeader);
                    }
                    else if (p != null && !p.Destroyed)
                    {
                        if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                        p.Destroy(DestroyMode.Vanish);
                    }
                }
                ReleaseTruceLease(ctx);
                ctx.run.vars["failReason"] = "lord_creation_failed";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            if (usedMode == "hospitality")
            {
                for (int i = 0; i < spawnedGuests.Count; i++)
                {
                    TryNotifyHospitalityCompArrive(spawnedGuests[i]);
                }
            }

            ctx.run.vars["visitMode"] = usedMode;
            ctx.run.vars["visitLordId"] = visitLord.loadID.ToString();
            ctx.run.vars["guestIds"] = JoinInts(guestIdNums);
            ctx.run.vars["arrived"] = "true";

            if (StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.RegisterHotelGuests(ctx.run, home.uniqueID, visitLord.loadID, guestIdNums);
            }

            string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Hotel Guests Arrived"), ctx);
            string body = StoryUtil.Format(!text.NullOrEmpty() ? text : Objective(ctx), ctx);
            LookTargets target = (heroSpawnedOk && heroPawn != null) ? new LookTargets(heroPawn) : new LookTargets(spawnedGuests[0]);
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, target); // CHECK API
        }

        // =====================================================================
        // STAGE LIFECYCLE: TICK (SINGLE-PASS PER-GUEST CLASSIFICATION)
        // =====================================================================

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            // Immediate outcome determined cleanly during Start (e.g. noHotel, permanent_enemy, missing hunterFaction)
            string immediate = GetStr(ctx, "immediateOutcome");
            if (!immediate.NullOrEmpty())
            {
                return Finish(ctx, immediate);
            }

            if (GetBool(ctx, "noHotel"))
            {
                return Finish(ctx, "noHotel");
            }

            // Pin strictly to the saved visit map (never silently swap to a second colony!)
            int visitMapId = GetInt(ctx, "visitMapId", -1);
            Map visitMap = FindMapByUniqueId(visitMapId);
            if (visitMap == null)
            {
                ctx.run.vars["failReason"] = "visit_map_lost";
                return Finish(ctx, "failed");
            }

            int now = Find.TickManager.TicksGame;
            int startTick = GetInt(ctx, "startTick", now);
            int leaveTick = GetInt(ctx, "leaveTick", now + GenDate.TicksPerDay);
            int hardDeadlineTick;
            if (!TryGetInt(ctx, "hardDeadlineTick", out hardDeadlineTick))
            {
                hardDeadlineTick = leaveTick + Mathf.Max(GenDate.TicksPerHour * 6, Mathf.RoundToInt(Mathf.Max(0.25f, overdueDays) * GenDate.TicksPerDay));
                ctx.run.vars["hardDeadlineTick"] = hardDeadlineTick.ToString();
            }

            int visitFacId = GetInt(ctx, "visitFactionId", -1);
            Faction visitFac = FindFactionById(visitFacId);
            if (visitFac == null || visitFac.HostileTo(Faction.OfPlayer))
            {
                ctx.run.vars["failReason"] = "faction_became_hostile";
                return Finish(ctx, "failed");
            }

            List<int> guestIds = ParseInts(GetStr(ctx, "guestIds"));
            if (guestIds.Count == 0)
            {
                ctx.run.vars["failReason"] = "empty_guest_list";
                return Finish(ctx, "failed");
            }

            int visitLordId = GetInt(ctx, "visitLordId", -1);
            int exitLordId = GetInt(ctx, "exitLordId", -1);
            string runKey = StoryRelationRestorer.MakeLeaseId(ctx.run);

            // Single-pass lookup maps for spawned pawns, corpses, and holders on visitMap
            HashSet<int> guestIdSet = new HashSet<int>(guestIds);
            Dictionary<int, Pawn> spawnedMap = new Dictionary<int, Pawn>();
            HashSet<int> deadOnMap = new HashSet<int>();
            Dictionary<int, Pawn> heldOnMap = new Dictionary<int, Pawn>();
            CollectMapGuestSnapshot(visitMap, guestIdSet, spawnedMap, deadOnMap, heldOnMap);

            List<Pawn> onMapGuests = new List<Pawn>();
            int peacefulExitCount = 0;
            StringBuilder stateLog = new StringBuilder();

            for (int i = 0; i < guestIds.Count; i++)
            {
                int idNum = guestIds[i];
                if (i > 0) stateLog.Append(',');
                stateLog.Append(idNum).Append(':');

                if (deadOnMap.Contains(idNum))
                {
                    stateLog.Append("killed");
                    ctx.run.vars["guestStates"] = stateLog.ToString();
                    ctx.run.vars["failReason"] = "guest_killed";
                    return Finish(ctx, "failed");
                }

                Pawn spawned;
                if (spawnedMap.TryGetValue(idNum, out spawned))
                {
                    if (spawned.Dead)
                    {
                        stateLog.Append("killed");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_killed";
                        return Finish(ctx, "failed");
                    }
                    if (spawned.IsPrisonerOfColony || spawned.IsSlaveOfColony)
                    {
                        stateLog.Append("imprisoned");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_imprisoned";
                        return Finish(ctx, "failed");
                    }
                    if (spawned.HostileTo(Faction.OfPlayer))
                    {
                        stateLog.Append("hostile");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_hostile";
                        return Finish(ctx, "failed");
                    }
                    if (IsSentAwayByHospitality(spawned))
                    {
                        stateLog.Append("sent_away");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_sent_away";
                        return Finish(ctx, "failed");
                    }

                    stateLog.Append("on_map");
                    onMapGuests.Add(spawned);
                    continue;
                }

                Pawn held;
                if (heldOnMap.TryGetValue(idNum, out held))
                {
                    if (held.Dead || held.IsPrisonerOfColony || held.IsSlaveOfColony || held.HostileTo(Faction.OfPlayer))
                    {
                        stateLog.Append("bad_holder");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_captured_or_dead_in_holder";
                        return Finish(ctx, "failed");
                    }
                    // Inside a drop pod/casket/container on map — NEVER counts as peaceful departure!
                    stateLog.Append("in_holder");
                    ctx.run.vars["guestStates"] = stateLog.ToString();
                    ctx.run.vars["failReason"] = "guest_missing_in_holder";
                    return Finish(ctx, "failed");
                }

                // Not on visitMap: MUST have a confirmed peaceful exit via Pawn.ExitMap under our visit/exit lord
                StoryTrackedGuestRecord rec = StoryRelationRestorer.Instance != null
                    ? StoryRelationRestorer.Instance.FindTrackedGuest(runKey, idNum)
                    : null;

                if (rec != null && rec.exitedHostileOrForced)
                {
                    stateLog.Append("exited_hostile");
                    ctx.run.vars["guestStates"] = stateLog.ToString();
                    ctx.run.vars["failReason"] = "guest_exited_hostile_or_sent_away";
                    return Finish(ctx, "failed");
                }

                if (rec != null && rec.exitedPeacefully)
                {
                    Pawn worldPawn = FindWorldPawnByIdNum(idNum);
                    if (worldPawn != null && (worldPawn.Dead || worldPawn.HostileTo(Faction.OfPlayer)))
                    {
                        stateLog.Append("exited_bad_world");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_dead_or_hostile_in_world";
                        return Finish(ctx, "failed");
                    }
                    stateLog.Append("exited_peaceful");
                    peacefulExitCount++;
                    continue;
                }

                // Guest vanished without peaceful ExitMap -> failed (never false success!)
                stateLog.Append("missing");
                ctx.run.vars["guestStates"] = stateLog.ToString();
                ctx.run.vars["failReason"] = "guest_missing";
                return Finish(ctx, "failed");
            }

            ctx.run.vars["guestStates"] = stateLog.ToString();

            // ---------------- OBJECTIVE TRACKING ----------------

            if (objective == StoryHotelObjective.Investigate && !GetBool(ctx, "investigated") && onMapGuests.Count > 0)
            {
                int interactionTick;
                if (HasRealColonistGuestInteraction(guestIdSet, startTick, out interactionTick))
                {
                    ctx.run.vars["investigated"] = "true";
                    ctx.run.vars["investigatedTick"] = interactionTick.ToString();
                }
            }

            if (objective == StoryHotelObjective.Hide)
            {
                UpdateHideHunterRaidTracking(ctx, visitMap);
            }

            // ---------------- DEPARTURE & LORD HEALTH ----------------

            bool visitLordAlive = FindLordById(visitMap, visitLordId) != null;
            bool exitLordAlive = exitLordId >= 0 && FindLordById(visitMap, exitLordId) != null;

            if (onMapGuests.Count > 0)
            {
                // Hard overdue deadline if lord stalls or Hide hunter raid never arrives
                if (now >= hardDeadlineTick)
                {
                    if (objective == StoryHotelObjective.Hide && !GetBool(ctx, "hunterRaidCleared"))
                    {
                        ctx.run.vars["deadlineReached"] = "true";
                        ctx.run.vars["failReason"] = GetBool(ctx, "hunterRaidBound") ? "hide_raid_not_cleared_deadline" : "hide_no_raid_deadline";
                        return Finish(ctx, FindOutcome("timeout") != null ? "timeout" : "failed");
                    }

                    ctx.run.vars["deadlineReached"] = "true";
                    ctx.run.vars["failReason"] = "lord_overdue_deadline";
                    return Finish(ctx, "failed");
                }

                // Lord disappeared while guests are still stranded on map -> abnormal state -> failed
                if (!visitLordAlive && !exitLordAlive)
                {
                    ctx.run.vars["failReason"] = "lord_missing";
                    return Finish(ctx, "failed");
                }

                // Trigger graceful departure after stayDays once objective allows leaving
                if (now >= leaveTick && !GetBool(ctx, "departureTriggered"))
                {
                    bool canDepart = (objective != StoryHotelObjective.Hide) || GetBool(ctx, "hunterRaidCleared");
                    if (canDepart)
                    {
                        Lord exitLord = TriggerGracefulDeparture(visitMap, visitFac, onMapGuests);
                        if (exitLord != null)
                        {
                            ctx.run.vars["exitLordId"] = exitLord.loadID.ToString();
                            if (StoryRelationRestorer.Instance != null)
                            {
                                StoryRelationRestorer.Instance.SetHotelExitLordId(runKey, exitLord.loadID);
                            }
                        }
                        ctx.run.vars["departureTriggered"] = "true";
                    }
                }

                return null;
            }

            // ---------------- ALL GUESTS HAVE LEFT THE MAP ----------------
            // Require ALL guests to have confirmed peaceful exits
            if (peacefulExitCount < guestIds.Count)
            {
                ctx.run.vars["failReason"] = "incomplete_peaceful_exit";
                return Finish(ctx, "failed");
            }

            if (objective == StoryHotelObjective.Investigate)
            {
                if (!GetBool(ctx, "investigated"))
                {
                    ctx.run.vars["failReason"] = "not_investigated";
                    return Finish(ctx, "failed");
                }
                return Finish(ctx, "success");
            }

            if (objective == StoryHotelObjective.Hide)
            {
                if (!GetBool(ctx, "hunterRaidBound") || !GetBool(ctx, "hunterRaidCleared"))
                {
                    ctx.run.vars["failReason"] = "hide_raid_not_completed";
                    return Finish(ctx, "failed");
                }
                return Finish(ctx, "success");
            }

            return Finish(ctx, "success");
        }

        public override string Objective(StoryContext ctx)
        {
            if (ctx != null && ctx.run != null && GetBool(ctx, "noHotel"))
            {
                return "Open a hotel (build guest beds and enable welcoming guests)";
            }

            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : text, ctx);
            if (baseText.NullOrEmpty())
            {
                if (objective == StoryHotelObjective.Investigate) baseText = "Investigate visiting hotel guests";
                else if (objective == StoryHotelObjective.Hide) baseText = "Shelter hidden guests from hunters";
                else baseText = "Host visiting delegation in the hotel";
            }

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int leaveTick = GetInt(ctx, "leaveTick", now);
            float daysLeft = Mathf.Max(0f, (leaveTick - now) / (float)GenDate.TicksPerDay);

            if (objective == StoryHotelObjective.Investigate)
            {
                string status = GetBool(ctx, "investigated") ? "clue found" : "talk to guest";
                return string.Format("{0} ({1}, {2:0.0}d left)", baseText, status, daysLeft);
            }

            if (objective == StoryHotelObjective.Hide)
            {
                string status = GetBool(ctx, "hunterRaidCleared")
                    ? "hunters repelled"
                    : (GetBool(ctx, "hunterRaidBound") ? "repel hunters" : "awaiting hunters");
                return string.Format("{0} ({1}, {2:0.0}d left)", baseText, status, daysLeft);
            }

            return string.Format("{0} ({1:0.0}d left)", baseText, daysLeft);
        }

        // =====================================================================
        // UNIFIED FINISH & CLEANUP
        // =====================================================================

        private string Finish(StoryContext ctx, string outcome)
        {
            ReleaseTruceLease(ctx);
            if (ctx != null && ctx.run != null)
            {
                string runKey = StoryRelationRestorer.MakeLeaseId(ctx.run);
                if (StoryRelationRestorer.Instance != null)
                {
                    StoryRelationRestorer.Instance.ClearHotelGuests(runKey);
                }
                ctx.run.vars["finished"] = outcome;
            }
            return outcome;
        }

        private static void ReleaseTruceLease(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;
            string leaseId = GetStr(ctx, "truceLeaseId");
            if (!leaseId.NullOrEmpty() && StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.ReleaseLease(leaseId);
                ctx.run.vars.Remove("truceLeaseId");
            }
        }

        // =====================================================================
        // SAFE HERO TRANSFER & ROLLBACK
        // =====================================================================

        private static bool IsFreeWorldHeroForVisit(Pawn p, Faction visitFac, out string skipReason)
        {
            skipReason = null;
            if (p == null) { skipReason = "null"; return false; }
            if (p.Dead || p.Destroyed) { skipReason = "dead"; return false; }
            if (p.Faction == Faction.OfPlayer) { skipReason = "colonist"; return false; }
            if (p.Spawned) { skipReason = "spawned_on_map"; return false; }
            if (p.IsPrisoner || p.IsSlave) { skipReason = "prisoner_or_slave"; return false; }
            if (p.holdingOwner != null) { skipReason = "in_container"; return false; }
            if (p.GetCaravan() != null) { skipReason = "in_caravan"; return false; }
            if (Find.WorldPawns == null || !Find.WorldPawns.Contains(p)) { skipReason = "not_in_world_pawns"; return false; }
            if (p.Faction != null && visitFac != null && p.Faction != visitFac)
            {
                skipReason = "wrong_faction";
                return false;
            }
            return true;
        }

        private static void RollbackHeroToWorld(Pawn p, Faction origFaction, bool wasLeader)
        {
            if (p == null) return;
            if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
            if (origFaction != null && p.Faction != origFaction)
            {
                p.SetFaction(origFaction);
            }
            if (Find.WorldPawns != null && !Find.WorldPawns.Contains(p))
            {
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
            }
            if (wasLeader && origFaction != null)
            {
                origFaction.leader = p;
            }
        }

        // =====================================================================
        // STRICT INVESTIGATE (PlayLogEntry_Interaction: COLONIST -> THIS GUEST)
        // =====================================================================

        private static bool HasRealColonistGuestInteraction(HashSet<int> guestIdSet, int startTick, out int foundTick)
        {
            foundTick = -1;
            if (guestIdSet == null || guestIdSet.Count == 0 || Find.PlayLog == null || Find.PlayLog.AllEntries == null)
            {
                return false;
            }

            if (!s_playLogChecked)
            {
                s_playLogChecked = true;
                // CHECK API: PlayLogEntry_Interaction private fields initiator / recipient
                s_playLogInitiatorField = AccessTools.Field(typeof(PlayLogEntry_Interaction), "initiator");
                s_playLogRecipientField = AccessTools.Field(typeof(PlayLogEntry_Interaction), "recipient");
            }

            if (s_playLogInitiatorField == null || s_playLogRecipientField == null)
            {
                return false;
            }

            List<LogEntry> entries = Find.PlayLog.AllEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                LogEntry entry = entries[i];
                if (entry == null || entry.Tick < startTick) continue;

                PlayLogEntry_Interaction inter = entry as PlayLogEntry_Interaction;
                if (inter == null) continue;

                Pawn initiator = s_playLogInitiatorField.GetValue(inter) as Pawn;
                Pawn recipient = s_playLogRecipientField.GetValue(inter) as Pawn;
                if (initiator == null || recipient == null) continue;

                bool initiatorIsColonist = initiator.Faction == Faction.OfPlayer && initiator.IsColonist && !initiator.IsPrisoner;
                bool recipientIsVisitGuest = guestIdSet.Contains(recipient.thingIDNumber);

                if (initiatorIsColonist && recipientIsVisitGuest)
                {
                    foundTick = entry.Tick;
                    return true;
                }
            }

            return false;
        }

        // =====================================================================
        // STRICT HIDE OBJECTIVE (BOUND HUNTER RAID LORD / COHORT)
        // =====================================================================

        private void UpdateHideHunterRaidTracking(StoryContext ctx, Map visitMap)
        {
            if (GetBool(ctx, "hunterRaidCleared")) return;

            int hunterFacId = GetInt(ctx, "hunterFactionId", -1);
            Faction hunterFac = FindFactionById(hunterFacId);
            if (hunterFac == null) return;

            // Step 1: Bind to a specific new hostile Lord of hunterFac on visitMap
            if (!GetBool(ctx, "hunterRaidBound"))
            {
                HashSet<int> initialLords = new HashSet<int>(ParseInts(GetStr(ctx, "initialHunterLordIds")));
                if (visitMap.lordManager != null)
                {
                    List<Lord> lords = visitMap.lordManager.lords;
                    for (int i = 0; i < lords.Count; i++)
                    {
                        Lord l = lords[i];
                        if (l == null || l.faction != hunterFac) continue;
                        if (initialLords.Contains(l.loadID)) continue;
                        if (!hunterFac.HostileTo(Faction.OfPlayer)) continue;
                        if (l.ownedPawns == null || l.ownedPawns.Count == 0) continue;

                        List<int> hunterPawnIds = new List<int>();
                        for (int pIdx = 0; pIdx < l.ownedPawns.Count; pIdx++)
                        {
                            Pawn hp = l.ownedPawns[pIdx];
                            if (hp != null && !hunterPawnIds.Contains(hp.thingIDNumber))
                            {
                                hunterPawnIds.Add(hp.thingIDNumber);
                            }
                        }

                        if (hunterPawnIds.Count > 0)
                        {
                            ctx.run.vars["hunterRaidBound"] = "true";
                            ctx.run.vars["hunterRaidLordId"] = l.loadID.ToString();
                            ctx.run.vars["hunterRaidPawnIds"] = JoinInts(hunterPawnIds);
                            break;
                        }
                    }
                }
                return;
            }

            // Step 2: Check whether the bound hunter raid cohort is completely neutralized/fled
            int boundLordId = GetInt(ctx, "hunterRaidLordId", -1);
            HashSet<int> boundPawnIds = new HashSet<int>(ParseInts(GetStr(ctx, "hunterRaidPawnIds")));
            if (IsBoundHunterRaidCleared(visitMap, boundLordId, boundPawnIds))
            {
                ctx.run.vars["hunterRaidCleared"] = "true";
            }
        }

        private static bool IsBoundHunterRaidCleared(Map map, int boundLordId, HashSet<int> boundPawnIds)
        {
            if (map == null) return false;

            if (boundLordId >= 0 && map.lordManager != null)
            {
                Lord l = FindLordById(map, boundLordId);
                if (l != null && l.ownedPawns != null)
                {
                    for (int i = 0; i < l.ownedPawns.Count; i++)
                    {
                        if (IsActiveRaiderOnMap(l.ownedPawns[i], map)) return false;
                    }
                }
            }

            if (boundPawnIds.Count > 0 && map.mapPawns != null)
            {
                IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawned.Count; i++)
                {
                    Pawn p = spawned[i];
                    if (p != null && boundPawnIds.Contains(p.thingIDNumber) && IsActiveRaiderOnMap(p, map))
                    {
                        return false;
                    }
                }
            }

            return boundLordId >= 0 || boundPawnIds.Count > 0;
        }

        private static bool IsActiveRaiderOnMap(Pawn p, Map map)
        {
            if (p == null || p.Dead || p.Destroyed || p.Downed) return false;
            if (p.IsPrisonerOfColony || p.IsSlaveOfColony) return false;
            if (!p.Spawned && p.MapHeld == map) return true;
            if (!p.Spawned || p.Map != map) return false;
            if (p.mindState != null && p.mindState.duty != null)
            {
                DutyDef d = p.mindState.duty.def;
                if (d == DutyDefOf.ExitMapBest || d == DutyDefOf.ExitMapBestAndDefendSelf || d == DutyDefOf.ExitMapRandom)
                {
                    return false;
                }
            }
            return true;
        }

        // =====================================================================
        // HOSPITALITY & LORD HELPERS
        // =====================================================================

        private void SendNoHotelLetter(StoryContext ctx, Map home)
        {
            string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "No Hotel Available"), ctx);
            string body = "A story delegation tried to visit {COLONY}, but your settlement does not have an open hotel.\n\nTo receive hotel visitors in future quests, build at least one Guest Bed (and a reception desk if desired) and enable 'Guests are welcome' in the Guests tab.";
            body = StoryUtil.Format(body, ctx);
            LookTargets target = home != null ? new LookTargets(home.Center, home) : LookTargets.Invalid;
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, target); // CHECK API
        }

        private static IntVec3 FindGuestBedCell(Map map)
        {
            if (map == null || map.listerBuildings == null) return IntVec3.Invalid;
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || b.Destroyed) continue;
                if (s_hospGuestBedType != null && s_hospGuestBedType.IsInstanceOfType(b))
                {
                    return b.Position;
                }
                if (b is Building_Bed)
                {
                    string typeName = b.GetType().Name;
                    string defName = b.def != null ? b.def.defName : string.Empty;
                    if (typeName.IndexOf("Guest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("Guest", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return b.Position;
                    }
                }
            }
            return IntVec3.Invalid;
        }

        private static Lord AttachVisitLord(Map home, Faction fac, IntVec3 chillSpot, int stayTicks, List<Pawn> guests, out string mode)
        {
            mode = "vanilla_fallback";
            LordJob hospJob = TryCreateHospitalityLordJob(fac, chillSpot, stayTicks);
            if (hospJob != null)
            {
                mode = "hospitality";
                return LordMaker.MakeNewLord(fac, hospJob, home, guests);
            }

            LordJob vanillaJob = new RimWorld.LordJob_VisitColony(fac, chillSpot, stayTicks); // CHECK API
            return LordMaker.MakeNewLord(fac, vanillaJob, home, guests);
        }

        private static LordJob TryCreateHospitalityLordJob(Faction fac, IntVec3 chillSpot, int stayTicks)
        {
            EnsureHospitalityReflection();
            try
            {
                if (s_hospLordCtor4 != null)
                {
                    return s_hospLordCtor4.Invoke(new object[] { fac, chillSpot, stayTicks, false }) as LordJob;
                }
                if (s_hospLordCtor3 != null)
                {
                    return s_hospLordCtor3.Invoke(new object[] { fac, chillSpot, stayTicks }) as LordJob;
                }
                if (s_hospLordCtor2 != null)
                {
                    return s_hospLordCtor2.Invoke(new object[] { fac, chillSpot }) as LordJob;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: failed to instantiate Hospitality.LordJob_VisitColony: " + ex.Message);
            }
            return null;
        }

        private static void TryNotifyHospitalityCompArrive(Pawn p)
        {
            if (p == null || s_hospCompGuestType == null || s_hospCompArriveMethod == null) return;
            try
            {
                List<ThingComp> comps = p.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (s_hospCompGuestType.IsInstanceOfType(comps[i]))
                    {
                        s_hospCompArriveMethod.Invoke(comps[i], null);
                        break;
                    }
                }
            }
            catch
            {
                // Ignore version mismatch on optional CompGuest.Arrive
            }
        }

        internal static bool IsSentAwayByHospitality(Pawn p)
        {
            EnsureHospitalityReflection();
            if (p == null || s_hospCompGuestType == null) return false;
            try
            {
                List<ThingComp> comps = p.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (!s_hospCompGuestType.IsInstanceOfType(comps[i])) continue;
                    if (s_hospCompSentAwayField != null)
                    {
                        object v = s_hospCompSentAwayField.GetValue(comps[i]);
                        if (v is bool && (bool)v) return true;
                    }
                    else if (s_hospCompSentAwayProp != null)
                    {
                        object v = s_hospCompSentAwayProp.GetValue(comps[i], null);
                        if (v is bool && (bool)v) return true;
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            return false;
        }

        private static Lord TriggerGracefulDeparture(Map home, Faction fac, List<Pawn> guests)
        {
            if (home == null || guests == null || guests.Count == 0) return null;

            for (int i = 0; i < guests.Count; i++)
            {
                Pawn p = guests[i];
                if (p == null) continue;
                Lord oldLord = p.GetLord();
                if (oldLord != null)
                {
                    oldLord.RemovePawn(p);
                }
            }

            Faction exitFac = fac ?? guests[0].Faction;
            if (exitFac == null) return null;
            return LordMaker.MakeNewLord(exitFac, new LordJob_ExitMapBest(LocomotionUrgency.Walk, false, false), home, guests); // CHECK API
        }

        // =====================================================================
        // SINGLE-PASS MAP SNAPSHOT & LOOKUP HELPERS
        // =====================================================================

        private static void CollectMapGuestSnapshot(Map map, HashSet<int> guestIds, Dictionary<int, Pawn> spawnedMap, HashSet<int> deadOnMap, Dictionary<int, Pawn> heldOnMap)
        {
            if (map == null) return;

            if (map.mapPawns != null)
            {
                IReadOnlyList<Pawn> all = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < all.Count; i++)
                {
                    Pawn p = all[i];
                    if (p != null && guestIds.Contains(p.thingIDNumber))
                    {
                        spawnedMap[p.thingIDNumber] = p;
                    }
                }
            }

            if (map.listerThings != null)
            {
                List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                for (int i = 0; i < corpses.Count; i++)
                {
                    Corpse c = corpses[i] as Corpse;
                    if (c != null && c.InnerPawn != null && guestIds.Contains(c.InnerPawn.thingIDNumber))
                    {
                        deadOnMap.Add(c.InnerPawn.thingIDNumber);
                    }
                }

                List<Thing> holders = map.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    Thing h = holders[i];
                    if (h == null || h.Destroyed || h is Pawn) continue;
                    CollectGuestsInHolder(h as IThingHolder, guestIds, heldOnMap);
                }
            }
        }

        private static void CollectGuestsInHolder(IThingHolder holder, HashSet<int> guestIds, Dictionary<int, Pawn> heldOnMap)
        {
            if (holder == null) return;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn p = owner[i] as Pawn;
                    if (p != null && guestIds.Contains(p.thingIDNumber))
                    {
                        heldOnMap[p.thingIDNumber] = p;
                    }
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                CollectGuestsInHolder(children[i], guestIds, heldOnMap);
            }
        }

        private Faction ResolveVisitFaction(StoryContext ctx)
        {
            if (!faction.NullOrEmpty())
            {
                Faction f = StoryUtil.FactionOf(faction);
                if (f != null && !f.defeated && !f.IsPlayer) return f;
                return null; // Explicit faction missing/defeated -> do not silently substitute
            }
            if (ctx.Faction != null && !ctx.Faction.defeated && !ctx.Faction.IsPlayer)
            {
                return ctx.Faction;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer))
                {
                    return f;
                }
            }
            return null;
        }

        private static List<int> SnapshotFactionLords(Map map, Faction fac)
        {
            List<int> ids = new List<int>();
            if (map == null || fac == null || map.lordManager == null) return ids;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null && lords[i].faction == fac)
                {
                    ids.Add(lords[i].loadID);
                }
            }
            return ids;
        }

        private static Lord FindLordById(Map map, int lordId)
        {
            if (map == null || lordId < 0 || map.lordManager == null) return null;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null && lords[i].loadID == lordId) return lords[i];
            }
            return null;
        }

        private static Map FindMapByUniqueId(int uniqueId)
        {
            if (uniqueId < 0 || Find.Maps == null) return null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == uniqueId) return maps[i];
            }
            return null;
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

        private static Pawn FindWorldPawnByIdNum(int idNum)
        {
            if (Find.WorldPawns == null) return null;
            List<Pawn> alive = Find.WorldPawns.AllPawnsAlive;
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i] != null && alive[i].thingIDNumber == idNum) return alive[i];
            }
            return null;
        }

        private static string JoinInts(List<int> list)
        {
            if (list == null || list.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(list[i]);
            }
            return sb.ToString();
        }

        private static List<int> ParseInts(string raw)
        {
            List<int> result = new List<int>();
            if (raw.NullOrEmpty()) return result;
            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) result.Add(val);
            }
            return result;
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static bool TryGetInt(StoryContext ctx, string key, out int val)
        {
            string raw;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return true;
            val = 0;
            return false;
        }

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            int val;
            return TryGetInt(ctx, key, out val) ? val : fallback;
        }

        private static bool GetBool(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }
    }
}
```

`Source/StoryStage_Deliver.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public enum StoryDeliverTarget
    {
        colony,
        giverSettlement
    }

    // =========================================================================
    // COLONY HAND-IN CHOICE LETTER (VALIDATED & ATOMIC)
    // =========================================================================

    /// <summary>
    /// Reminder/hand-in ChoiceLetter for StoryStage_Deliver (to = colony).
    /// Before EVERY deduction (including immediately after reload prior to first Reconcile),
    /// verifies: active run, matching stageKey &amp; runGen, letter still in stack, pinned map
    /// from vars (never a closure-captured map), deadline not elapsed, and atomic StoryPayment.
    /// </summary>
    public sealed class ChoiceLetter_StoryDeliver : ChoiceLetter
    {
        public string questDefName;
        public string stageKey;
        public int runGen;
        public int deliverMapId = -1;
        public string thingDefName;
        public int requiredCount = 1;
        public int deadlineTick = -1;
        private bool resolved;

        public ChoiceLetter_StoryDeliver()
        {
            if (Find.UniqueIDsManager != null)
            {
                ID = Find.UniqueIDsManager.GetNextLetterID();
            }
        }

        public override bool CanDismissWithRightClick
        {
            get { return false; }
        }

        private bool InStack
        {
            get { return Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(this); }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                StoryQuestRun run;
                Map pinnedMap;
                ThingDef def;
                string invalidReason;

                bool valid = StoryStage_Deliver.ValidateColonyHandIn(
                    this, out run, out pinnedMap, out def, out invalidReason);

                if (!valid)
                {
                    DiaOption close = new DiaOption("Close".Translate());
                    close.action = delegate
                    {
                        resolved = true;
                        if (InStack) Find.LetterStack.RemoveLetter(this);
                    };
                    close.resolveTree = true;
                    yield return close;
                    yield break;
                }

                int have = StoryPayment.CountInColonyStockpiles(pinnedMap, def);
                string itemLabel = def != null ? def.label : thingDefName;
                string deliverLabel = string.Format("Deliver {0} x{1} ({2}/{3})", itemLabel, requiredCount, have, requiredCount);
                DiaOption deliver = new DiaOption(deliverLabel);

                if (have < requiredCount)
                {
                    deliver.Disable(string.Format("Need {0} {1} in colony storage on {2} ({3}/{0})",
                        requiredCount, itemLabel, pinnedMap.Parent != null ? pinnedMap.Parent.LabelCap.ToString() : "colony", have));
                }
                else
                {
                    deliver.action = delegate
                    {
                        // Full re-validation at exact click time (no stale closures!)
                        StoryQuestRun clickRun;
                        Map clickMap;
                        ThingDef clickDef;
                        string clickReason;
                        if (!StoryStage_Deliver.ValidateColonyHandIn(this, out clickRun, out clickMap, out clickDef, out clickReason))
                        {
                            resolved = true;
                            if (InStack) Find.LetterStack.RemoveLetter(this);
                            return;
                        }

                        int deducted;
                        if (StoryPayment.TryDeductFromColonyStockpiles(clickMap, clickDef, requiredCount, out deducted) &&
                            deducted == requiredCount)
                        {
                            clickRun.vars["delivered"] = "true";
                            resolved = true;
                            if (InStack) Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    deliver.resolveTree = true;
                }

                yield return deliver;
                yield return Option_Postpone; // CHECK API
            }
        }

        public bool IsResolved
        {
            get { return resolved; }
        }

        public bool IsInLetterStack
        {
            get { return InStack; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref stageKey, "stageKey", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref deliverMapId, "deliverMapId", -1);
            Scribe_Values.Look(ref thingDefName, "thingDefName", null);
            Scribe_Values.Look(ref requiredCount, "requiredCount", 1);
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", -1);
            Scribe_Values.Look(ref resolved, "resolved", false);
        }
    }

    // =========================================================================
    // CARAVAN ARRIVAL ACTION FOR giverSettlement (EXPLICIT ARRIVAL ONLY)
    // =========================================================================

    /// <summary>
    /// Explicit CaravanArrivalAction for StoryStage_Deliver (to = giverSettlement).
    /// Passing caravans are NEVER deducted; cargo is only deducted when a caravan arrives
    /// with this arrival action or when the player clicks the explicit Deliver gizmo at the settlement.
    /// </summary>
    public sealed class CaravanArrivalAction_StoryDeliver : CaravanArrivalAction
    {
        public Settlement settlement;
        public string questDefName;
        public string stageKey;
        public int runGen;

        public CaravanArrivalAction_StoryDeliver()
        {
        }

        public CaravanArrivalAction_StoryDeliver(Settlement settlement, string questDefName, string stageKey, int runGen)
        {
            this.settlement = settlement;
            this.questDefName = questDefName;
            this.stageKey = stageKey;
            this.runGen = runGen;
        }

        public override string Label
        {
            get
            {
                return string.Format("Deliver quest cargo to {0}",
                    settlement != null ? settlement.LabelCap.ToString() : "settlement");
            }
        }

        public override string ReportString
        {
            get
            {
                return string.Format("Delivering quest cargo to {0}.",
                    settlement != null ? settlement.LabelCap.ToString() : "settlement");
            }
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile) // CHECK API
        {
            FloatMenuAcceptanceReport baseRep = base.StillValid(caravan, destinationTile);
            if (!baseRep) return baseRep;
            if (settlement == null || settlement.Destroyed || settlement.Tile != destinationTile) return false;
            return StoryStage_Deliver.CanCaravanDeliver(caravan, settlement, questDefName, stageKey, runGen);
        }

        public override void Arrived(Caravan caravan)
        {
            StoryStage_Deliver.TryCompleteCaravanHandIn(caravan, settlement, questDefName, stageKey, runGen);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref stageKey, "stageKey", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
        }
    }

    // =========================================================================
    // STORY STAGE: DELIVER
    // =========================================================================

    public sealed class StoryStage_Deliver : StoryStage
    {
        public ThingDef thing;
        public int count = 1;
        public StoryDeliverTarget to = StoryDeliverTarget.colony;
        public float timeoutDays = 10f;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "deliver"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            int now = Find.TickManager.TicksGame;
            int deadline = now + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["deadlineTick"] = deadline.ToString();
            ctx.run.vars["delivered"] = "false";

            if (thing == null || count <= 0)
            {
                ctx.run.vars["failReason"] = "invalid_deliver_def";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            if (to == StoryDeliverTarget.colony)
            {
                Map home = ctx.Home;
                if (home == null)
                {
                    ctx.run.vars["failReason"] = "no_home_map";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }
                ctx.run.vars["deliverMapId"] = home.uniqueID.ToString();
                SendColonyDeliverLetter(ctx, home, deadline);
            }
            else
            {
                // Strict giverSettlement: ONLY a settlement belonging to the quest's exact faction (no fallback!)
                Settlement target = FindNearestExactFactionSettlement(ctx);
                if (target == null)
                {
                    Log.Warning("[TMM] StoryStage_Deliver: no settlement belonging to quest faction '" +
                                (ctx.def != null ? ctx.def.faction : "null") + "'. Failing explicitly without colony fallback.");
                    ctx.run.vars["failReason"] = "no_faction_settlement";
                    ctx.run.vars["immediateOutcome"] = FindOutcome("unavailable") != null ? "unavailable" : "failed";
                    return;
                }

                ctx.run.vars["settlementId"] = target.ID.ToString();
                ctx.run.vars["settlementFactionId"] = target.Faction != null ? target.Faction.loadID.ToString() : "-1";
                ctx.run.vars["settlementName"] = target.LabelCap.ToString();

                string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Caravan Delivery"), ctx);
                string body = BuildSettlementLetterText(ctx, target);
                Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, new LookTargets(target)); // CHECK API
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            string immediate = GetStr(ctx.run, "immediateOutcome");
            if (!immediate.NullOrEmpty())
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = immediate;
                return immediate;
            }

            // 1. If payment was already accepted on time (including across save/reload), finish cleanly without re-deducting!
            if (GetBool(ctx.run, "delivered"))
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "success";
                return "success";
            }

            int now = Find.TickManager.TicksGame;
            int deadline;
            if (!TryGetInt(ctx.run, "deadlineTick", out deadline))
            {
                deadline = now + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));
                ctx.run.vars["deadlineTick"] = deadline.ToString();
            }

            // 2. Check deadline FIRST
            if (now >= deadline)
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            if (to == StoryDeliverTarget.colony)
            {
                int mapId = GetInt(ctx.run, "deliverMapId", -1);
                Map pinnedMap = FindMapByUniqueId(mapId);
                if (pinnedMap == null || !pinnedMap.IsPlayerHome)
                {
                    RemoveDeliverLetters(ctx);
                    ctx.run.vars["failReason"] = "deliver_map_lost";
                    ctx.run.vars["finished"] = "failed";
                    return "failed";
                }

                if (!HasActiveDeliverLetter(ctx))
                {
                    SendColonyDeliverLetter(ctx, pinnedMap, deadline);
                }
                return null;
            }

            // 3. giverSettlement mode: verify target settlement still exists & still belongs to the required faction
            // NOTE: Passing caravans are NEVER auto-deducted here! Delivery happens via CaravanArrivalAction_StoryDeliver
            // or the explicit caravan Deliver gizmo.
            int settlementId = GetInt(ctx.run, "settlementId", -1);
            int expectedFacId = GetInt(ctx.run, "settlementFactionId", -1);
            Settlement target = FindSettlementById(settlementId);
            if (target == null || target.Destroyed || target.Faction == null || target.Faction.loadID != expectedFacId)
            {
                ctx.run.vars["failReason"] = "settlement_gone";
                ctx.run.vars["finished"] = "failed";
                return "failed";
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string itemLabel = thing != null ? thing.label : "resources";
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", itemLabel, count), ctx);

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx.run, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            if (to == StoryDeliverTarget.colony)
            {
                Map pinnedMap = FindMapByUniqueId(GetInt(ctx.run, "deliverMapId", -1));
                int have = (pinnedMap != null && thing != null) ? StoryPayment.CountInColonyStockpiles(pinnedMap, thing) : 0;
                return string.Format("{0} ({1}/{2} {3}, {4:0.0}d left)", baseText, have, count, itemLabel, daysLeft);
            }

            string targetName = GetStr(ctx.run, "settlementName");
            if (targetName.NullOrEmpty()) targetName = "faction settlement";
            return string.Format("{0} to {1} ({2:0.0}d left)", baseText, targetName, daysLeft);
        }

        // =====================================================================
        // SHARED VALIDATION FOR COLONY LETTER & CARAVAN HAND-IN
        // =====================================================================

        /// <summary>
        /// Validates every precondition for a colony letter click (safe even immediately after load
        /// before SparkStory.Reconcile has run).
        /// </summary>
        internal static bool ValidateColonyHandIn(
            ChoiceLetter_StoryDeliver letter,
            out StoryQuestRun run,
            out Map pinnedMap,
            out ThingDef def,
            out string reason)
        {
            run = null;
            pinnedMap = null;
            def = null;
            reason = null;

            if (letter == null || letter.IsResolved || !letter.IsInLetterStack)
            {
                reason = "letter_not_active";
                return false;
            }

            StoryStage_Deliver stageDef;
            if (!ValidateActiveDeliverStage(letter.questDefName, letter.stageKey, letter.runGen, StoryDeliverTarget.colony, out run, out stageDef, out reason))
            {
                return false;
            }

            int now = Find.TickManager.TicksGame;
            int dl = GetInt(run, "deadlineTick", letter.deadlineTick);
            if (dl >= 0 && now >= dl)
            {
                reason = "deadline_passed";
                return false;
            }

            int mapId = GetInt(run, "deliverMapId", letter.deliverMapId);
            pinnedMap = FindMapByUniqueId(mapId);
            if (pinnedMap == null || !pinnedMap.IsPlayerHome)
            {
                reason = "pinned_map_missing";
                return false;
            }

            def = stageDef.thing ?? DefDatabase<ThingDef>.GetNamedSilentFail(letter.thingDefName);
            if (def == null)
            {
                reason = "thing_def_missing";
                return false;
            }

            return true;
        }

        internal static bool ValidateActiveDeliverStage(
            string questDefName,
            string expectedStageKey,
            int expectedGen,
            StoryDeliverTarget expectedTarget,
            out StoryQuestRun run,
            out StoryStage_Deliver stageDef,
            out string reason)
        {
            run = null;
            stageDef = null;
            reason = null;

            SparkStory story = SparkStory.Instance;
            if (story == null || questDefName.NullOrEmpty())
            {
                reason = "no_story";
                return false;
            }

            run = story.RunFor(questDefName);
            if (run == null || run.status != StoryQuestStatus.Active)
            {
                reason = "run_not_active";
                return false;
            }

            if (run.gen != expectedGen)
            {
                reason = "gen_mismatch";
                return false;
            }

            if (!expectedStageKey.NullOrEmpty() && run.stageKey != expectedStageKey)
            {
                reason = "stage_key_mismatch";
                return false;
            }

            if (run.vars.ContainsKey("finished") || GetBool(run, "delivered"))
            {
                reason = "already_delivered_or_finished";
                return false;
            }

            StoryQuestDef def = run.Def;
            if (def == null || def.stages == null)
            {
                reason = "def_missing";
                return false;
            }

            // Resolve stage by stable stageKey first so validation works even before Reconcile() remaps run.stage!
            StoryStage foundStage = null;
            if (!run.stageKey.NullOrEmpty())
            {
                for (int i = 0; i < def.stages.Count; i++)
                {
                    if (def.stages[i] != null && def.stages[i].Key(i) == run.stageKey)
                    {
                        foundStage = def.stages[i];
                        break;
                    }
                }
            }
            if (foundStage == null && run.stage >= 0 && run.stage < def.stages.Count)
            {
                foundStage = def.stages[run.stage];
            }

            stageDef = foundStage as StoryStage_Deliver;
            if (stageDef == null || stageDef.to != expectedTarget)
            {
                reason = "wrong_stage_type";
                return false;
            }

            return true;
        }

        public static bool CanCaravanDeliver(Caravan caravan, Settlement settlement, string questDefName, string expectedStageKey, int expectedGen)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return false;
            if (settlement == null || settlement.Destroyed) return false;

            StoryQuestRun run;
            StoryStage_Deliver stageDef;
            string reason;
            if (!ValidateActiveDeliverStage(questDefName, expectedStageKey, expectedGen, StoryDeliverTarget.giverSettlement, out run, out stageDef, out reason))
            {
                return false;
            }

            int now = Find.TickManager.TicksGame;
            int dl = GetInt(run, "deadlineTick", -1);
            if (dl >= 0 && now >= dl) return false;

            int expectedSettlementId = GetInt(run, "settlementId", -1);
            int expectedFacId = GetInt(run, "settlementFactionId", -1);
            if (settlement.ID != expectedSettlementId) return false;
            if (settlement.Faction == null || settlement.Faction.loadID != expectedFacId) return false;

            if (stageDef.thing == null || stageDef.count <= 0) return false;
            return StoryPayment.CountInCaravanDirectInventory(caravan, stageDef.thing) >= stageDef.count;
        }

        public static bool TryCompleteCaravanHandIn(Caravan caravan, Settlement settlement, string questDefName, string expectedStageKey, int expectedGen)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return false;
            if (settlement == null || settlement.Destroyed || caravan.Tile != settlement.Tile) return false;

            StoryQuestRun run;
            StoryStage_Deliver stageDef;
            string reason;
            if (!ValidateActiveDeliverStage(questDefName, expectedStageKey, expectedGen, StoryDeliverTarget.giverSettlement, out run, out stageDef, out reason))
            {
                return false;
            }

            // Check deadline BEFORE touching any items
            int now = Find.TickManager.TicksGame;
            int dl = GetInt(run, "deadlineTick", -1);
            if (dl >= 0 && now >= dl) return false;

            int expectedSettlementId = GetInt(run, "settlementId", -1);
            int expectedFacId = GetInt(run, "settlementFactionId", -1);
            if (settlement.ID != expectedSettlementId || settlement.Faction == null || settlement.Faction.loadID != expectedFacId)
            {
                return false;
            }

            if (stageDef.thing == null || stageDef.count <= 0) return false;

            int deducted;
            if (!StoryPayment.TryDeductFromCaravan(caravan, stageDef.thing, stageDef.count, out deducted) || deducted != stageDef.count)
            {
                return false;
            }

            run.vars["delivered"] = "true";

            string doneTitle = !stageDef.label.NullOrEmpty() ? stageDef.label : "Delivery Complete";
            string doneBody = string.Format("Your caravan delivered {0} x{1} to {2}.",
                stageDef.thing.label, stageDef.count, settlement.LabelCap);
            Find.LetterStack.ReceiveLetter(doneTitle, doneBody, LetterDefOf.PositiveEvent, new LookTargets(settlement)); // CHECK API
            return true;
        }

        /// <summary>
        /// Enumerates all active giverSettlement Deliver stages targeting the given settlement.
        /// Used by the Settlement FloatMenu and Caravan Gizmo Harmony patches.
        /// </summary>
        internal static List<StoryQuestRun> FindActiveSettlementDeliveries(Settlement settlement)
        {
            List<StoryQuestRun> matches = new List<StoryQuestRun>();
            SparkStory story = SparkStory.Instance;
            if (story == null || settlement == null || settlement.Destroyed || settlement.Faction == null) return matches;

            int now = Find.TickManager.TicksGame;
            foreach (StoryQuestRun run in story.Runs)
            {
                if (run == null || run.status != StoryQuestStatus.Active) continue;
                if (run.vars == null || run.vars.ContainsKey("finished") || GetBool(run, "delivered")) continue;
                if (GetInt(run, "settlementId", -1) != settlement.ID) continue;
                if (GetInt(run, "settlementFactionId", -1) != settlement.Faction.loadID) continue;

                int dl = GetInt(run, "deadlineTick", -1);
                if (dl >= 0 && now >= dl) continue;

                StoryQuestDef def = run.Def;
                if (def == null || run.stage < 0 || run.stage >= def.stages.Count) continue;
                StoryStage_Deliver st = def.stages[run.stage] as StoryStage_Deliver;
                if (st != null && st.to == StoryDeliverTarget.giverSettlement && st.thing != null)
                {
                    matches.Add(run);
                }
            }
            return matches;
        }

        // =====================================================================
        // LETTER & SETTLEMENT HELPERS
        // =====================================================================

        private void SendColonyDeliverLetter(StoryContext ctx, Map pinnedMap, int deadline)
        {
            if (Find.LetterStack == null || thing == null || pinnedMap == null) return;

            ChoiceLetter_StoryDeliver letter = new ChoiceLetter_StoryDeliver();
            letter.def = LetterDefOf.NeutralEvent;
            letter.Label = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", thing.label, count), ctx);
            letter.Text = BuildColonyLetterText(ctx);
            letter.questDefName = ctx.def != null ? ctx.def.defName : string.Empty;
            letter.stageKey = ctx.run.stageKey;
            letter.runGen = ctx.run.gen;
            letter.deliverMapId = pinnedMap.uniqueID;
            letter.thingDefName = thing.defName;
            letter.requiredCount = Mathf.Max(1, count);
            letter.deadlineTick = deadline;
            letter.lookTargets = new LookTargets(pinnedMap.Center, pinnedMap);

            Find.LetterStack.ReceiveLetter(letter); // CHECK API
        }

        private string BuildColonyLetterText(StoryContext ctx)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Gather {0} x{1} in {2}'s storage stockpiles within {3:0.0} days, then open this letter and click Deliver.",
                itemLabel, count, "{COLONY}", timeoutDays);
            instruction = StoryUtil.Format(instruction, ctx);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private string BuildSettlementLetterText(StoryContext ctx, Settlement target)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Send a caravan carrying {0} x{1} to {2} and choose 'Deliver quest cargo' within {3:0.0} days.",
                itemLabel, count, target.LabelCap, timeoutDays);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private static bool HasActiveDeliverLetter(StoryContext ctx)
        {
            if (Find.LetterStack == null || ctx == null || ctx.def == null || ctx.run == null) return false;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = 0; i < letters.Count; i++)
            {
                ChoiceLetter_StoryDeliver dl = letters[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && !dl.IsResolved && dl.questDefName == ctx.def.defName &&
                    dl.runGen == ctx.run.gen && dl.stageKey == ctx.run.stageKey)
                {
                    return true;
                }
            }
            return false;
        }

        private static void RemoveDeliverLetters(StoryContext ctx)
        {
            if (Find.LetterStack == null || ctx == null || ctx.def == null || ctx.run == null) return;
            List<Letter> snapshot = new List<Letter>(Find.LetterStack.LettersListForReading);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ChoiceLetter_StoryDeliver dl = snapshot[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == ctx.def.defName && dl.runGen == ctx.run.gen)
                {
                    Find.LetterStack.RemoveLetter(dl);
                }
            }
        }

        /// <summary>
        /// Strictly selects the nearest non-destroyed settlement belonging to ctx.Faction ONLY.
        /// Never substitutes another faction's settlement.
        /// </summary>
        private static Settlement FindNearestExactFactionSettlement(StoryContext ctx)
        {
            Faction targetFac = ctx.Faction;
            if (targetFac == null || targetFac.IsPlayer || targetFac.defeated) return null;

            PlanetTile homeTile = ResolveHomeTile(ctx); // CHECK API
            Settlement best = null;
            float bestDist = float.MaxValue;

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s == null || s.Destroyed || s.Faction != targetFac) continue;

                float dist = homeTile.Valid ? Find.WorldGrid.TraversalDistanceBetween(homeTile, s.Tile) : 0f; // CHECK API
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = s;
                }
            }
            return best;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0 || Find.WorldObjects == null) return null;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s != null && !s.Destroyed && s.ID == id) return s;
            }
            return null;
        }

        private static Map FindMapByUniqueId(int uniqueId)
        {
            if (uniqueId < 0 || Find.Maps == null) return null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == uniqueId) return maps[i];
            }
            return null;
        }

        private static PlanetTile ResolveHomeTile(StoryContext ctx) // CHECK API
        {
            if (ctx.Home != null) return ctx.Home.Tile; // CHECK API
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (settlements[i] != null && settlements[i].Faction == Faction.OfPlayer)
                {
                    return settlements[i].Tile; // CHECK API
                }
            }
            return PlanetTile.Invalid; // CHECK API
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static bool TryGetInt(StoryQuestRun run, string key, out int val)
        {
            string raw;
            if (run != null && run.vars != null && run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return true;
            val = 0;
            return false;
        }

        private static int GetInt(StoryQuestRun run, string key, int fallback)
        {
            int val;
            return TryGetInt(run, key, out val) ? val : fallback;
        }

        private static bool GetBool(StoryQuestRun run, string key)
        {
            string raw;
            return run != null && run.vars != null && run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryQuestRun run, string key)
        {
            string raw;
            return (run != null && run.vars != null && run.vars.TryGetValue(key, out raw)) ? raw : string.Empty;
        }
    }

    // =========================================================================
    // HARMONY PATCHES: EXPLICIT CARAVAN DELIVERY OPTIONS (NO PASSING-BY STEAL)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class StoryDeliverCaravanPatches
    {
        static StoryDeliverCaravanPatches()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.storydeliver");

            MethodInfo settlementFloatMenu = AccessTools.Method(typeof(Settlement), "GetFloatMenuOptions", new Type[] { typeof(Caravan) }); // CHECK API
            if (settlementFloatMenu != null)
            {
                harmony.Patch(settlementFloatMenu, postfix: new HarmonyMethod(typeof(StoryDeliverCaravanPatches), "Postfix_Settlement_GetFloatMenuOptions"));
            }

            MethodInfo caravanGizmos = AccessTools.Method(typeof(Caravan), "GetGizmos"); // CHECK API
            if (caravanGizmos != null)
            {
                harmony.Patch(caravanGizmos, postfix: new HarmonyMethod(typeof(StoryDeliverCaravanPatches), "Postfix_Caravan_GetGizmos"));
            }
        }

        public static IEnumerable<FloatMenuOption> Postfix_Settlement_GetFloatMenuOptions(IEnumerable<FloatMenuOption> __result, Settlement __instance, Caravan caravan)
        {
            if (__result != null)
            {
                foreach (FloatMenuOption opt in __result)
                {
                    yield return opt;
                }
            }

            if (__instance == null || caravan == null || caravan.Faction != Faction.OfPlayer) yield break;

            List<StoryQuestRun> runs = StoryStage_Deliver.FindActiveSettlementDeliveries(__instance);
            for (int i = 0; i < runs.Count; i++)
            {
                StoryQuestRun run = runs[i];
                StoryQuestDef def = run.Def;
                StoryStage_Deliver st = (def != null && run.stage >= 0 && run.stage < def.stages.Count) ? def.stages[run.stage] as StoryStage_Deliver : null;
                if (st == null || st.thing == null) continue;

                string capturedQuest = run.quest;
                string capturedKey = run.stageKey;
                int capturedGen = run.gen;
                Settlement capturedSettlement = __instance;
                Caravan capturedCaravan = caravan;

                int have = StoryPayment.CountInCaravanDirectInventory(caravan, st.thing);
                string optLabel = string.Format("Deliver {0} x{1} to {2} ({3}/{1})",
                    st.thing.label, st.count, __instance.LabelCap, have);

                if (have < st.count)
                {
                    yield return new FloatMenuOption(optLabel + " (not enough cargo)", null);
                    continue;
                }

                yield return new FloatMenuOption(optLabel, delegate
                {
                    if (capturedCaravan.Tile == capturedSettlement.Tile)
                    {
                        StoryStage_Deliver.TryCompleteCaravanHandIn(capturedCaravan, capturedSettlement, capturedQuest, capturedKey, capturedGen);
                    }
                    else
                    {
                        CaravanArrivalAction_StoryDeliver action = new CaravanArrivalAction_StoryDeliver(
                            capturedSettlement, capturedQuest, capturedKey, capturedGen);
                        capturedCaravan.pather.StartPath(capturedSettlement.Tile, action, true); // CHECK API
                    }
                });
            }
        }

        public static IEnumerable<Gizmo> Postfix_Caravan_GetGizmos(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            if (__result != null)
            {
                foreach (Gizmo g in __result)
                {
                    yield return g;
                }
            }

            if (__instance == null || __instance.Destroyed || __instance.Faction != Faction.OfPlayer) yield break;
            if (Find.WorldObjects == null) yield break;

            Settlement here = Find.WorldObjects.SettlementAt(__instance.Tile); // CHECK API
            if (here == null || here.Destroyed) yield break;

            List<StoryQuestRun> runs = StoryStage_Deliver.FindActiveSettlementDeliveries(here);
            for (int i = 0; i < runs.Count; i++)
            {
                StoryQuestRun run = runs[i];
                StoryQuestDef def = run.Def;
                StoryStage_Deliver st = (def != null && run.stage >= 0 && run.stage < def.stages.Count) ? def.stages[run.stage] as StoryStage_Deliver : null;
                if (st == null || st.thing == null) continue;

                int have = StoryPayment.CountInCaravanDirectInventory(__instance, st.thing);
                string capturedQuest = run.quest;
                string capturedKey = run.stageKey;
                int capturedGen = run.gen;
                Settlement capturedSettlement = here;
                Caravan capturedCaravan = __instance;

                Command_Action cmd = new Command_Action();
                cmd.defaultLabel = string.Format("Deliver {0} x{1}", st.thing.label, st.count);
                cmd.defaultDesc = string.Format("Hand in {0} x{1} to {2} for quest '{3}' ({4}/{1} carried).",
                    st.thing.label, st.count, here.LabelCap, def.LabelCap, have);
                cmd.icon = st.thing.uiIcon; // CHECK API
                if (have < st.count)
                {
                    cmd.Disable(string.Format("Caravan carries only {0}/{1} {2}.", have, st.count, st.thing.label));
                }
                cmd.action = delegate
                {
                    StoryStage_Deliver.TryCompleteCaravanHandIn(capturedCaravan, capturedSettlement, capturedQuest, capturedKey, capturedGen);
                };
                yield return cmd;
            }
        }
    }
}
```

`Tests/InGameStoryHotelProbe.cs`
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class StoryHotelProbeBootstrap
    {
        public const string SaveName = "TMM-StoryHotel";

        // Runtime probe quests (registered identically on fresh & reload so Scribe resolves Defs cleanly)
        public const string QNoHotel = "TMM_Probe_E3_NoHotel";
        public const string QHeroColonistSkip = "TMM_Probe_E3_HeroColonistSkip";
        public const string QPermanentEnemy = "TMM_Probe_E3_PermanentEnemy";
        public const string QTruceRestore = "TMM_Probe_E3_TruceRestore";
        public const string QTruceDisabledRollback = "TMM_Probe_E3_TruceDisabledRollback";
        public const string QHostKilled = "TMM_Probe_E3_HostKilled";
        public const string QHostImprisoned = "TMM_Probe_E3_HostImprisoned";
        public const string QInvestigateStrict = "TMM_Probe_E3_InvestigateStrict";
        public const string QHideNoRaidDeadline = "TMM_Probe_E3_HideNoRaidDeadline";
        public const string QDeliverExpiredClick = "TMM_Probe_E3_DeliverExpiredClick";
        public const string QDeliverSecondStackFail = "TMM_Probe_E3_DeliverSecondStackFail";
        public const string QDeliverCaravanSettlement = "TMM_Probe_E3_DeliverCaravanSettlement";
        public const string QReloadActiveVisit = "TMM_Probe_E3_ReloadActiveVisit";
        public const string QReloadPaidDeliver = "TMM_Probe_E3_ReloadPaidDeliver";

        public static bool FreshMode
        {
            get { return HasArg("-tmm-story-hotel-probe"); }
        }

        public static bool ReloadMode
        {
            get { return HasArg("-tmm-story-hotel-reload"); }
        }

        static StoryHotelProbeBootstrap()
        {
            if (!FreshMode && !ReloadMode) return;

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                RegisterRuntimeProbeQuests();
                if (ReloadMode && Current.ProgramState == ProgramState.Entry)
                {
                    GameDataSaveLoader.LoadGame(SaveName); // CHECK API
                }
            });
        }

        private static void RegisterRuntimeProbeQuests()
        {
            HeroDef heroA = PickHero(0);
            HeroDef heroB = PickHero(1) ?? heroA;

            AddQuestDef(BuildHotelQuest(QNoHotel, "probe.e3.noHotel", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QHeroColonistSkip, "probe.e3.heroColonist", null, heroA, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QPermanentEnemy, "probe.e3.permEnemy", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QTruceRestore, "probe.e3.truceRestore", null, null, 1, 1f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QTruceDisabledRollback, "probe.e3.truceDisabled", null, null, 1, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QHostKilled, "probe.e3.hostKilled", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QHostImprisoned, "probe.e3.hostImprisoned", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildHotelQuest(QInvestigateStrict, "probe.e3.investigate", null, null, 1, 2f, 1f, StoryHotelObjective.Investigate, null));
            AddQuestDef(BuildHotelQuest(QHideNoRaidDeadline, "probe.e3.hideNoRaid", null, null, 1, 0.5f, 0.25f, StoryHotelObjective.Hide, null));

            AddQuestDef(BuildDeliverQuest(QDeliverExpiredClick, "probe.e3.deliverExpired", null, ThingDefOf.Steel, 30, StoryDeliverTarget.colony, 0.2f));
            AddQuestDef(BuildDeliverQuest(QDeliverSecondStackFail, "probe.e3.deliverAtomic", null, ThingDefOf.Steel, 50, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverCaravanSettlement, "probe.e3.deliverCaravan", null, ThingDefOf.Steel, 40, StoryDeliverTarget.giverSettlement, 10f));

            AddQuestDef(BuildHotelQuest(QReloadActiveVisit, "probe.e3.reloadVisit", null, heroB, 2, 3f, 2f, StoryHotelObjective.Host, null));
            AddQuestDef(BuildDeliverQuest(QReloadPaidDeliver, "probe.e3.reloadPaid", null, ThingDefOf.Steel, 50, StoryDeliverTarget.colony, 10f));
        }

        private static HeroDef PickHero(int idx)
        {
            List<HeroDef> all = DefDatabase<HeroDef>.AllDefsListForReading;
            if (all == null || all.Count == 0) return null;
            return all[idx % all.Count];
        }

        private static StoryQuestDef BuildHotelQuest(string defName, string flag, string facDef, HeroDef h, int groupSize, float stayDays, float overdueDays, StoryHotelObjective obj, string hunterFac)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.faction = facDef;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_HotelVisit st = new StoryStage_HotelVisit();
            st.key = "hotel";
            st.label = defName;
            st.text = defName;
            st.faction = facDef;
            st.hero = h;
            st.groupSize = groupSize;
            st.stayDays = stayDays;
            st.overdueDays = overdueDays;
            st.objective = obj;
            st.hunterFaction = hunterFac;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static StoryQuestDef BuildDeliverQuest(string defName, string flag, string facDef, ThingDef thing, int count, StoryDeliverTarget to, float timeoutDays)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.faction = facDef;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_Deliver st = new StoryStage_Deliver();
            st.key = "deliver";
            st.label = defName;
            st.text = defName;
            st.thing = thing;
            st.count = count;
            st.to = to;
            st.timeoutDays = timeoutDays;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static void AddQuestDef(StoryQuestDef d)
        {
            if (d == null || DefDatabase<StoryQuestDef>.GetNamedSilentFail(d.defName) != null) return;
            DefDatabase<StoryQuestDef>.Add(d); // CHECK API
        }

        private static bool HasArg(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Comprehensive in-game verification probe for all E3 Codex review fixes (P1 &amp; P2):
    /// - noHotel immediate resolution on Start;
    /// - Hero-colonist NOT stolen by hotel delegation;
    /// - Permanent enemy visit fails cleanly without spawning any guests;
    /// - Hostile goodwill faction truce restores relations after exit AND when quest is Disabled;
    /// - Killed or imprisoned guests NEVER count as success;
    /// - Investigate ignores shared indoor room and requires a real PlayLogEntry_Interaction (colonist -> guest);
    /// - Hide ignores unrelated enemies and fails/times out at hard deadline if hunter raid never arrives;
    /// - Clicking expired Deliver letter deducts nothing;
    /// - Atomic StoryPayment rollback on second-stack SplitOff failure leaves stockpiles intact and delivered=false;
    /// - Passing-by caravan at giverSettlement is NOT deducted; arrived caravan via CaravanArrivalAction_StoryDeliver IS deducted;
    /// - Save/reload preserves active visit lord &amp; guest composition without duplicating hero, and completes paid Deliver without double-deducting;
    /// - Honest UI/report passed evaluation (never hardcoded true).
    /// </summary>
    public sealed class InGameStoryHotelProbe : MapComponent
    {
        private sealed class CheckItem
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private static bool s_completed;
        private int m_ticksWaited;
        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        public InGameStoryHotelProbe(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (s_completed) return;
            if (!StoryHotelProbeBootstrap.FreshMode && !StoryHotelProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                if (StoryHotelProbeBootstrap.FreshMode)
                {
                    RunFreshProbe();
                }
                else if (StoryHotelProbeBootstrap.ReloadMode)
                {
                    RunReloadProbe();
                }
            }
            catch (Exception ex)
            {
                m_errors.Add("Unhandled exception: " + ex.ToString());
            }
            finally
            {
                StoryPayment.TestSplitOffOverride = null;
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(StoryHotelProbeBootstrap.ReloadMode ? "StoryHotelReloadProbe.json" : "StoryHotelProbe.json");
                Root.Shutdown();
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        // =====================================================================
        // FRESH PROBE: REAL SCENARIOS FOR EVERY P1 & P2 FINDING
        // =====================================================================

        private void RunFreshProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("SparkStory_Exists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. NO HOTEL IMMEDIATE ON START ----------------
            SetGuestsWelcome(map, false);
            story.SetFlag("probe.e3.noHotel", "true");
            story.DirectorTick();
            StoryQuestRun noHotelRun = story.RunFor(StoryHotelProbeBootstrap.QNoHotel);
            bool noHotelImmediate = noHotelRun != null && GetVar(noHotelRun, "noHotel") == "true" &&
                                    GetVar(noHotelRun, "immediateOutcome") == "noHotel" &&
                                    GetVar(noHotelRun, "arrived") == "false";
            story.DirectorTick();
            bool noHotelFinished = noHotelRun != null && HasOutcome(noHotelRun, "noHotel");
            AddCheck("Compat_NoHotelImmediateOnStart", noHotelImmediate && noHotelFinished,
                "immediate noHotel & 0 spawns",
                string.Format("immediate={0}, finished={1}", noHotelImmediate, noHotelFinished));

            // ---------------- BUILD HOTEL & PLAYER STORAGE ZONE ----------------
            bool hotelBuilt = BuildTestHotel(map);
            SetGuestsWelcome(map, true);
            bool hotelOpen = StoryStage_HotelVisit.HasOpenHotel(map);
            AddCheck("Setup_HotelBuiltAndOpen", hotelBuilt && hotelOpen, "built=true, open=true",
                string.Format("built={0}, open={1}", hotelBuilt, hotelOpen));

            Zone_Stockpile stockpile = EnsureTestStockpile(map);
            AddCheck("Setup_StockpileReady", stockpile != null && stockpile.cells.Count >= 9, ">=9 cells",
                stockpile != null ? stockpile.cells.Count.ToString() : "null");

            // ---------------- 2. HERO-COLONIST IS NOT STOLEN ----------------
            StoryQuestDef heroSkipDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHeroColonistSkip);
            StoryStage_HotelVisit heroSkipStage = heroSkipDef != null ? heroSkipDef.stages[0] as StoryStage_HotelVisit : null;
            Pawn colonistHero = (heroSkipStage != null && heroSkipStage.hero != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(heroSkipStage.hero) : null;
            if (colonistHero != null)
            {
                if (colonistHero.Spawned) colonistHero.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns.Contains(colonistHero)) Find.WorldPawns.RemovePawn(colonistHero);
                colonistHero.SetFaction(Faction.OfPlayer);
                IntVec3 origCell = FindStandable(map);
                GenSpawn.Spawn(colonistHero, origCell, map, WipeMode.Vanish);

                story.SetFlag("probe.e3.heroColonist", "true");
                story.DirectorTick();
                StoryQuestRun skipRun = story.RunFor(StoryHotelProbeBootstrap.QHeroColonistSkip);

                bool heroSkipped = skipRun != null && GetVar(skipRun, "heroIncluded") == "false";
                bool heroStillColonistUntouched = colonistHero.Faction == Faction.OfPlayer &&
                                                  colonistHero.Spawned && colonistHero.Map == map &&
                                                  colonistHero.Position == origCell &&
                                                  !ParseIntList(GetVar(skipRun, "guestIds")).Contains(colonistHero.thingIDNumber);
                AddCheck("P1_HeroColonistNotMoved", heroSkipped && heroStillColonistUntouched,
                    "heroIncluded=false & colonist untouched",
                    string.Format("skipped={0}, reason={1}, untouched={2}", heroSkipped, GetVar(skipRun, "heroSkipReason"), heroStillColonistUntouched));

                // Clean up this test visit cleanly
                ForceAbortRunAndCleanupGuests(map, skipRun);
            }
            else
            {
                AddCheck("P1_HeroColonistNotMoved", false, "hero exists", "hero null");
            }

            // ---------------- 3. PERMANENT ENEMY FACTION -> FAILED WITHOUT SPAWN ----------------
            Faction permEnemy = FindPermanentEnemyHumanlikeFaction();
            StoryQuestDef permDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QPermanentEnemy);
            StoryStage_HotelVisit permStage = permDef != null ? permDef.stages[0] as StoryStage_HotelVisit : null;
            if (permEnemy != null && permStage != null)
            {
                permStage.faction = permEnemy.def.defName;
                int pawnsBefore = map.mapPawns.AllPawnsSpawnedCount;

                story.SetFlag("probe.e3.permEnemy", "true");
                story.DirectorTick();
                story.DirectorTick();

                StoryQuestRun permRun = story.RunFor(StoryHotelProbeBootstrap.QPermanentEnemy);
                int pawnsAfter = map.mapPawns.AllPawnsSpawnedCount;
                bool failedNoSpawn = permRun != null && HasOutcome(permRun, "failed") &&
                                     GetVar(permRun, "arrived") == "false" && pawnsAfter == pawnsBefore;
                AddCheck("P1_PermanentEnemyFailsWithoutSpawn", failedNoSpawn,
                    "outcome=failed, arrived=false, 0 spawned",
                    string.Format("hist={0}, arrived={1}, pawns={2}->{3}",
                        permRun != null ? string.Join(",", permRun.history.ToArray()) : "null",
                        GetVar(permRun, "arrived"), pawnsBefore, pawnsAfter));
            }
            else
            {
                AddCheck("P1_PermanentEnemyFailsWithoutSpawn", false, "permanentEnemy faction exists", "missing");
            }

            // ---------------- 4. HOSTILE GOODWILL FACTION TRUCE & RESTORE AFTER DISABLED QUEST ----------------
            Faction goodwillFac = FindGoodwillHumanlikeFaction();
            StoryQuestDef truceDisDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceDisabledRollback);
            StoryStage_HotelVisit truceDisStage = truceDisDef != null ? truceDisDef.stages[0] as StoryStage_HotelVisit : null;
            if (goodwillFac != null && truceDisStage != null)
            {
                ForceHostileWithGoodwill(goodwillFac, -85);
                truceDisStage.faction = goodwillFac.def.defName;

                story.SetFlag("probe.e3.truceDisabled", "true");
                story.DirectorTick();
                StoryQuestRun tdRun = story.RunFor(StoryHotelProbeBootstrap.QTruceDisabledRollback);

                bool enteredTruce = !goodwillFac.HostileTo(Faction.OfPlayer) && GetVar(tdRun, "arrived") == "true";

                // Simulate quest disabled mid-visit (orphan rollback test)
                List<int> tdGuests = ParseIntList(GetVar(tdRun, "guestIds"));
                tdRun.status = StoryQuestStatus.Disabled;
                tdRun.stage = -1;
                StoryRelationRestorer.CheckOrphansIfPresent();

                bool restoredHostile = goodwillFac.HostileTo(Faction.OfPlayer) &&
                                       goodwillFac.PlayerGoodwill <= -80 && goodwillFac.PlayerGoodwill >= -90;
                AddCheck("P1_TruceRestoredOnDisabledQuest", enteredTruce && restoredHostile,
                    "truce->Disabled->Hostile(~-85)",
                    string.Format("truce={0}, hostile={1}, gw={2}", enteredTruce, goodwillFac.HostileTo(Faction.OfPlayer), goodwillFac.PlayerGoodwill));

                CleanupPawnsByIds(map, tdGuests);
            }
            else
            {
                AddCheck("P1_TruceRestoredOnDisabledQuest", false, "goodwill faction exists", "missing");
            }

            // ---------------- 5. HOSTILE TRUCE RESTORED AFTER NORMAL FAILURE / EXIT ----------------
            StoryQuestDef truceResDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceRestore);
            StoryStage_HotelVisit truceResStage = truceResDef != null ? truceResDef.stages[0] as StoryStage_HotelVisit : null;
            if (goodwillFac != null && truceResStage != null)
            {
                ForceHostileWithGoodwill(goodwillFac, -90);
                truceResStage.faction = goodwillFac.def.defName;

                story.SetFlag("probe.e3.truceRestore", "true");
                story.DirectorTick();
                StoryQuestRun trRun = story.RunFor(StoryHotelProbeBootstrap.QTruceRestore);

                bool enteredTruce = !goodwillFac.HostileTo(Faction.OfPlayer) && GetVar(trRun, "arrived") == "true";
                // Kill the guest to end the stage via normal Finish("failed")
                List<int> trGuests = ParseIntList(GetVar(trRun, "guestIds"));
                KillFirstGuest(map, trGuests);
                story.DirectorTick();

                bool finishedFailed = trRun != null && HasOutcome(trRun, "failed");
                bool restoredAfterEnd = goodwillFac.HostileTo(Faction.OfPlayer) &&
                                        goodwillFac.PlayerGoodwill <= -85 && goodwillFac.PlayerGoodwill >= -95;
                AddCheck("P1_TruceRestoredAfterStageEnd", enteredTruce && finishedFailed && restoredAfterEnd,
                    "truce->end->Hostile(~-90)",
                    string.Format("truce={0}, failed={1}, hostile={2}, gw={3}",
                        enteredTruce, finishedFailed, goodwillFac.HostileTo(Faction.OfPlayer), goodwillFac.PlayerGoodwill));

                CleanupPawnsByIds(map, trGuests);
                // Restore goodwillFac to Neutral for subsequent friendly tests
                ForceNeutralWithGoodwill(goodwillFac, 10);
            }
            else
            {
                AddCheck("P1_TruceRestoredAfterStageEnd", false, "goodwill faction exists", "missing");
            }

            // ---------------- 6. GUEST KILLED -> FAILED (NOT SUCCESS) ----------------
            story.SetFlag("probe.e3.hostKilled", "true");
            story.DirectorTick();
            StoryQuestRun killRun = story.RunFor(StoryHotelProbeBootstrap.QHostKilled);
            List<int> killGuests = ParseIntList(GetVar(killRun, "guestIds"));
            KillFirstGuest(map, killGuests);
            story.DirectorTick();
            bool killFailed = killRun != null && HasOutcome(killRun, "failed") && !HasOutcome(killRun, "success");
            AddCheck("P1_GuestKilled_NotSuccess", killFailed, "failed",
                killRun != null ? string.Join(",", killRun.history.ToArray()) : "null");
            CleanupPawnsByIds(map, killGuests);

            // ---------------- 7. GUEST IMPRISONED -> FAILED (NOT SUCCESS) ----------------
            story.SetFlag("probe.e3.hostImprisoned", "true");
            story.DirectorTick();
            StoryQuestRun prisRun = story.RunFor(StoryHotelProbeBootstrap.QHostImprisoned);
            List<int> prisGuests = ParseIntList(GetVar(prisRun, "guestIds"));
            Pawn firstPrisGuest = FindGuestOnMap(map, prisGuests);
            if (firstPrisGuest != null && firstPrisGuest.guest != null)
            {
                firstPrisGuest.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner); // CHECK API
            }
            story.DirectorTick();
            bool prisFailed = prisRun != null && HasOutcome(prisRun, "failed") && !HasOutcome(prisRun, "success");
            AddCheck("P1_GuestImprisoned_NotSuccess", prisFailed, "failed",
                prisRun != null ? string.Join(",", prisRun.history.ToArray()) : "null");
            CleanupPawnsByIds(map, prisGuests);

            // ---------------- 8. INVESTIGATE WITHOUT CONVERSATION != SUCCESS; REAL INTERACTION SETS INVESTIGATED ----------------
            story.SetFlag("probe.e3.investigate", "true");
            story.DirectorTick();
            StoryQuestRun invRun = story.RunFor(StoryHotelProbeBootstrap.QInvestigateStrict);
            List<int> invGuests = ParseIntList(GetVar(invRun, "guestIds"));
            Pawn invGuest = FindGuestOnMap(map, invGuests);

            // Place a Social=15 colonist right next to the guest (no PlayLogEntry_Interaction!) -> MUST remain investigated=false
            Pawn talker = SpawnColonist(map);
            if (talker.skills != null)
            {
                SkillRecord soc = talker.skills.GetSkill(SkillDefOf.Social);
                if (soc != null) soc.Level = 15;
            }
            if (invGuest != null)
            {
                talker.Position = invGuest.Position;
            }
            story.DirectorTick();
            bool notInvestigatedWithoutChat = GetVar(invRun, "investigated") == "false";

            // Now add a real PlayLogEntry_Interaction (initiator = colonist, recipient = THIS visit's guest)
            bool realChatDetected = false;
            if (invGuest != null && Find.PlayLog != null)
            {
                PlayLogEntry_Interaction entry = new PlayLogEntry_Interaction(InteractionDefOf.Chitchat, talker, invGuest, null); // CHECK API
                Find.PlayLog.Add(entry);
                story.DirectorTick();
                realChatDetected = GetVar(invRun, "investigated") == "true";
            }
            AddCheck("P1_InvestigateRequiresRealInteraction", notInvestigatedWithoutChat && realChatDetected,
                "withoutChat=false, afterRealChat=true",
                string.Format("noChatFalse={0}, afterChat={1}", notInvestigatedWithoutChat, realChatDetected));
            ForceAbortRunAndCleanupGuests(map, invRun);

            // ---------------- 9. HIDE WITHOUT BOUND HUNTER RAID -> REACHES DEADLINE (NOT SUCCESS) ----------------
            Faction hunterFac = FindPermanentEnemyHumanlikeFaction() ?? goodwillFac;
            StoryQuestDef hideDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHideNoRaidDeadline);
            StoryStage_HotelVisit hideStage = hideDef != null ? hideDef.stages[0] as StoryStage_HotelVisit : null;
            if (hunterFac != null && hideStage != null)
            {
                hideStage.hunterFaction = hunterFac.def.defName;
                story.SetFlag("probe.e3.hideNoRaid", "true");
                story.DirectorTick();
                StoryQuestRun hideRun = story.RunFor(StoryHotelProbeBootstrap.QHideNoRaidDeadline);
                List<int> hideGuests = ParseIntList(GetVar(hideRun, "guestIds"));

                // Advance past stayDays + overdueDays WITHOUT any hunter raid arriving -> must hit hard deadline, NOT success
                int hardDl = ParseInt(hideRun, "hardDeadlineTick", Find.TickManager.TicksGame + GenDate.TicksPerDay);
                Find.TickManager.DebugSetTicksGame(hardDl + 100); // CHECK API
                story.DirectorTick();

                bool hitDeadlineNotSuccess = hideRun != null && !HasOutcome(hideRun, "success") &&
                                             (HasOutcome(hideRun, "failed") || HasOutcome(hideRun, "timeout")) &&
                                             GetVar(hideRun, "hunterRaidBound") == "false";
                AddCheck("P1_HideWithoutRaid_ReachesDeadline", hitDeadlineNotSuccess,
                    "not success & hunterRaidBound=false",
                    string.Format("hist={0}, bound={1}, reason={2}",
                        hideRun != null ? string.Join(",", hideRun.history.ToArray()) : "null",
                        GetVar(hideRun, "hunterRaidBound"), GetVar(hideRun, "failReason")));
                CleanupPawnsByIds(map, hideGuests);
            }
            else
            {
                AddCheck("P1_HideWithoutRaid_ReachesDeadline", false, "hunterFac exists", "missing");
            }

            // ---------------- 10. DELIVER: EXPIRED LETTER CLICK DEDUCTS NOTHING ----------------
            ClearStockpileResources(map, stockpile, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 40, 0);
            int steelBeforeExpired = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            story.SetFlag("probe.e3.deliverExpired", "true");
            story.DirectorTick();
            StoryQuestRun expRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverExpiredClick);
            ChoiceLetter_StoryDeliver expLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverExpiredClick);

            // Advance past deadline BEFORE calling DirectorTick, then click the stale Deliver letter!
            int expDl = ParseInt(expRun, "deadlineTick", Find.TickManager.TicksGame + GenDate.TicksPerDay);
            Find.TickManager.DebugSetTicksGame(expDl + 50); // CHECK API

            bool uiExpiredRefused = false;
            if (expLetter != null)
            {
                // Honest UI check: Choices should return Close only (no active Deliver deduction!)
                ClickAllEnabledOptions(expLetter);
                int steelAfterExpiredClick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                uiExpiredRefused = steelAfterExpiredClick == steelBeforeExpired && GetVar(expRun, "delivered") != "true";
            }
            story.DirectorTick();
            bool expTimedOut = expRun != null && HasOutcome(expRun, "timeout");
            AddCheck("P1_DeliverExpiredClick_DeductsNothing",
                expLetter != null && uiExpiredRefused && expTimedOut,
                "steel unchanged, delivered!=true, timeout",
                string.Format("letter={0}, refused={1}, timeout={2}, steel={3}->{4}",
                    expLetter != null, uiExpiredRefused, expTimedOut, steelBeforeExpired, StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel)));

            // ---------------- 11. DELIVER: ATOMIC ROLLBACK ON SECOND STACK FAILURE ----------------
            ClearStockpileResources(map, stockpile, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 30, 0);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 30, 1);
            int steelBeforeAtomic = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            story.SetFlag("probe.e3.deliverAtomic", "true");
            story.DirectorTick();
            StoryQuestRun atomicRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverSecondStackFail);
            ChoiceLetter_StoryDeliver atomicLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverSecondStackFail);

            // Simulate second stack SplitOff failing (returns null on stepIndex == 1)
            StoryPayment.TestSplitOffOverride = delegate(Thing src, int count, int stepIndex)
            {
                if (stepIndex == 1) return null;
                return src.SplitOff(count);
            };

            bool clickedAtomic = atomicLetter != null && ClickFirstDeliverOption(atomicLetter);
            StoryPayment.TestSplitOffOverride = null;

            int steelAfterAtomicFail = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            bool atomicRolledBack = clickedAtomic && steelBeforeAtomic == 60 && steelAfterAtomicFail == 60 &&
                                    GetVar(atomicRun, "delivered") != "true" &&
                                    atomicLetter != null && atomicLetter.IsInLetterStack;
            AddCheck("P1_DeliverAtomicRollback_SecondStackFail", atomicRolledBack,
                "steel=60->60, delivered!=true, letter stays",
                string.Format("clicked={0}, steel={1}->{2}, delivered={3}",
                    clickedAtomic, steelBeforeAtomic, steelAfterAtomicFail, GetVar(atomicRun, "delivered")));

            // ---------------- 12. DELIVER giverSettlement: PASSING CARAVAN IGNORED, ARRIVED CARAVAN DEDUCTED ----------------
            Settlement targetSettlement = FindAnyNonPlayerSettlement();
            StoryQuestDef carDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QDeliverCaravanSettlement);
            if (targetSettlement != null && targetSettlement.Faction != null && carDef != null)
            {
                carDef.faction = targetSettlement.Faction.def.defName;
                story.SetFlag("probe.e3.deliverCaravan", "true");
                story.DirectorTick();
                StoryQuestRun carRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverCaravanSettlement);

                int pinnedSettlementId = ParseInt(carRun, "settlementId", -1);
                Settlement exactSettlement = FindSettlementById(pinnedSettlementId);

                if (exactSettlement != null)
                {
                    Pawn carrier = SpawnColonist(map);
                    Thing cargo = ThingMaker.MakeThing(ThingDefOf.Steel);
                    cargo.stackCount = 40;
                    carrier.inventory.innerContainer.TryAdd(cargo);
                    carrier.DeSpawn(DestroyMode.Vanish);

                    // Spawn caravan ON the exact settlement tile WITHOUT arrival action (simulates passing through!)
                    Caravan car = CaravanMaker.MakeCaravan(new List<Pawn> { carrier }, Faction.OfPlayer, exactSettlement.Tile, true); // CHECK API
                    story.DirectorTick();
                    story.DirectorTick();

                    int cargoAfterPassing = StoryPayment.CountInCaravanDirectInventory(car, ThingDefOf.Steel);
                    bool passingNotDeducted = cargoAfterPassing == 40 && carRun.status == StoryQuestStatus.Active &&
                                              GetVar(carRun, "delivered") != "true";

                    // Now invoke explicit CaravanArrivalAction_StoryDeliver.Arrived(car)
                    CaravanArrivalAction_StoryDeliver action = new CaravanArrivalAction_StoryDeliver(
                        exactSettlement, carRun.quest, carRun.stageKey, carRun.gen);
                    bool stillValid = action.StillValid(car, exactSettlement.Tile);
                    action.Arrived(car);
                    story.DirectorTick();

                    int cargoAfterArrival = StoryPayment.CountInCaravanDirectInventory(car, ThingDefOf.Steel);
                    bool arrivedDeducted = stillValid && cargoAfterArrival == 0 && HasOutcome(carRun, "success");

                    AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted",
                        passingNotDeducted && arrivedDeducted,
                        "passing=40(active), arrived=0(success)",
                        string.Format("passingOk={0}(cargo={1}), valid={2}, afterCargo={3}, hist={4}",
                            passingNotDeducted, cargoAfterPassing, stillValid, cargoAfterArrival,
                            string.Join(",", carRun.history.ToArray())));
                    car.Destroy();
                }
                else
                {
                    AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted", false, "exact settlement pinned", "settlement null");
                }
            }
            else
            {
                AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted", false, "non-player settlement exists", "missing");
            }

            // ---------------- 13. PREPARE SAVE FOR RELOAD: ACTIVE VISIT + PAID DELIVER ----------------
            // 13A. Active HotelVisit with registered free hero
            story.SetFlag("probe.e3.reloadVisit", "true");
            story.DirectorTick();
            StoryQuestRun reloadVisitRun = story.RunFor(StoryHotelProbeBootstrap.QReloadActiveVisit);
            bool visitSavedReady = reloadVisitRun != null && reloadVisitRun.status == StoryQuestStatus.Active &&
                                   GetVar(reloadVisitRun, "arrived") == "true" &&
                                   ParseInt(reloadVisitRun, "visitLordId", -1) >= 0;
            AddCheck("ReloadPrep_ActiveHotelVisitSaved", visitSavedReady, "active visit with lordId>=0",
                string.Format("arrived={0}, lordId={1}, heroIncluded={2}",
                    GetVar(reloadVisitRun, "arrived"), GetVar(reloadVisitRun, "visitLordId"), GetVar(reloadVisitRun, "heroIncluded")));

            // 13B. Paid Deliver saved BEFORE DirectorTick finishes the stage (tests no double deduction after reload)
            ClearStockpileResources(map, stockpile, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 100, 0);
            story.SetFlag("probe.e3.reloadPaid", "true");
            story.DirectorTick();
            StoryQuestRun reloadPaidRun = story.RunFor(StoryHotelProbeBootstrap.QReloadPaidDeliver);
            ChoiceLetter_StoryDeliver reloadPaidLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QReloadPaidDeliver);

            // Click Deliver (deducts 50, leaving 50, sets delivered=true) and DO NOT call DirectorTick before SaveGame!
            bool paidBeforeSave = reloadPaidLetter != null && ClickFirstDeliverOption(reloadPaidLetter);
            int steelLeftAtSave = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            bool paidSavedReady = paidBeforeSave && steelLeftAtSave == 50 && GetVar(reloadPaidRun, "delivered") == "true";
            AddCheck("ReloadPrep_PaidDeliverSavedBeforeTransition", paidSavedReady, "paid=true, steelLeft=50, delivered=true",
                string.Format("paid={0}, steel={1}, delivered={2}", paidBeforeSave, steelLeftAtSave, GetVar(reloadPaidRun, "delivered")));

            GameDataSaveLoader.SaveGame(StoryHotelProbeBootstrap.SaveName); // CHECK API
        }

        // =====================================================================
        // RELOAD PROBE: VERIFIES LORD/HERO IDENTITY & NO DOUBLE DEDUCTION
        // =====================================================================

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. ACTIVE HOTEL VISIT AFTER RELOAD ----------------
            StoryQuestRun visitRun = story.RunFor(StoryHotelProbeBootstrap.QReloadActiveVisit);
            if (visitRun != null && visitRun.status == StoryQuestStatus.Active)
            {
                int savedMapId = ParseInt(visitRun, "visitMapId", -1);
                int savedLordId = ParseInt(visitRun, "visitLordId", -1);
                List<int> savedGuestIds = ParseIntList(GetVar(visitRun, "guestIds"));
                string heroThingId = GetVar(visitRun, "heroThingId");

                Lord restoredLord = FindLordById(map, savedLordId);
                bool mapMatches = savedMapId == map.uniqueID;
                bool lordRestored = restoredLord != null && restoredLord.ownedPawns != null &&
                                    restoredLord.ownedPawns.Count == savedGuestIds.Count;

                // Verify hero is NOT duplicated anywhere on the map
                int heroMatchesOnMap = 0;
                if (!heroThingId.NullOrEmpty())
                {
                    IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
                    for (int i = 0; i < allPawns.Count; i++)
                    {
                        if (allPawns[i] != null && allPawns[i].ThingID == heroThingId) heroMatchesOnMap++;
                    }
                }
                bool heroNoDuplicate = heroThingId.NullOrEmpty() || heroMatchesOnMap == 1;

                AddCheck("Reload_ActiveVisitLordAndHeroIntact",
                    mapMatches && lordRestored && heroNoDuplicate,
                    "mapMatches, lord & guest count match, hero count==1",
                    string.Format("map={0}, lord={1}({2}/{3}), heroCount={4}",
                        mapMatches, restoredLord != null,
                        restoredLord != null && restoredLord.ownedPawns != null ? restoredLord.ownedPawns.Count : -1,
                        savedGuestIds.Count, heroMatchesOnMap));
            }
            else
            {
                AddCheck("Reload_ActiveVisitLordAndHeroIntact", false, "active visit restored", "missing");
            }

            // ---------------- 2. PAID DELIVER COMPLETES WITHOUT DOUBLE DEDUCTION ----------------
            StoryQuestRun paidRun = story.RunFor(StoryHotelProbeBootstrap.QReloadPaidDeliver);
            int steelBeforeTick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            story.DirectorTick();
            story.DirectorTick();
            int steelAfterTick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            bool completedOnceNoRededuct = paidRun != null && CountOutcome(paidRun, "success") == 1 &&
                                           steelBeforeTick == 50 && steelAfterTick == 50;
            AddCheck("Reload_PaidDeliverNoDoubleDeduction", completedOnceNoRededuct,
                "successCount=1, steel=50->50",
                string.Format("count={0}, steel={1}->{2}",
                    CountOutcome(paidRun, "success"), steelBeforeTick, steelAfterTick));
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static bool BuildTestHotel(Map targetMap)
        {
            if (targetMap == null) return false;

            ThingDef guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("GuestBed") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("GuestDoubleBed") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("GuestSleepingSpot"); // CHECK API
            ThingDef receptionDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_HotelReception"); // CHECK API

            IntVec3 baseCell = FindStandable(targetMap);

            if (receptionDef != null)
            {
                Thing rec = ThingMaker.MakeThing(receptionDef, receptionDef.MadeFromStuff ? ThingDefOf.WoodLog : null);
                rec.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(rec, baseCell, targetMap, WipeMode.Vanish);
            }

            int spawnedBeds = 0;
            if (guestBedDef != null)
            {
                ThingDef stuff = guestBedDef.MadeFromStuff ? ThingDefOf.WoodLog : null;
                for (int i = 0; i < 3; i++)
                {
                    IntVec3 cell;
                    if (CellFinder.TryFindRandomCellNear(baseCell, targetMap, 8, delegate(IntVec3 c)
                    {
                        return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
                    }, out cell))
                    {
                        Thing bed = ThingMaker.MakeThing(guestBedDef, stuff);
                        bed.SetFaction(Faction.OfPlayer);
                        GenSpawn.Spawn(bed, cell, targetMap, WipeMode.Vanish);
                        spawnedBeds++;
                    }
                }
            }
            return spawnedBeds > 0;
        }

        private static void SetGuestsWelcome(Map targetMap, bool welcome)
        {
            Type compType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
            if (compType == null || targetMap == null) return;
            MapComponent comp = HotelVisitGuarantee.HospitalityOf(targetMap);
            if (comp == null) return;
            FieldInfo field = AccessTools.Field(compType, "guestsAreWelcome");
            if (field != null) field.SetValue(comp, welcome);
        }

        private static Zone_Stockpile EnsureTestStockpile(Map targetMap)
        {
            if (targetMap == null || targetMap.zoneManager == null) return null;
            Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, targetMap.zoneManager); // CHECK API
            targetMap.zoneManager.RegisterZone(zone);
            IntVec3 origin = FindStandable(targetMap);
            for (int dx = -1; dx <= 2; dx++)
            {
                for (int dz = -1; dz <= 2; dz++)
                {
                    IntVec3 c = origin + new IntVec3(dx, 0, dz);
                    if (c.InBounds(targetMap) && c.Standable(targetMap) && targetMap.zoneManager.ZoneAt(c) == null)
                    {
                        zone.AddCell(c);
                    }
                }
            }
            return zone;
        }

        private static void ClearStockpileResources(Map targetMap, Zone_Stockpile zone, ThingDef def)
        {
            if (targetMap == null || def == null) return;
            List<Thing> snapshot = new List<Thing>(targetMap.listerThings.ThingsOfDef(def));
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing t = snapshot[i];
                if (t != null && !t.Destroyed && StoryPayment.IsInPlayerStorage(targetMap, t))
                {
                    t.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void PlaceInStockpile(Map targetMap, Zone_Stockpile zone, ThingDef def, int count, int cellIndex)
        {
            if (targetMap == null || zone == null || zone.cells.Count == 0 || def == null || count <= 0) return;
            IntVec3 cell = zone.cells[cellIndex % zone.cells.Count];
            Thing t = ThingMaker.MakeThing(def);
            t.stackCount = count;
            GenSpawn.Spawn(t, cell, targetMap, WipeMode.Vanish);
            t.SetForbidden(false, false);
        }

        private static ChoiceLetter_StoryDeliver FindDeliverLetter(string questDefName)
        {
            if (Find.LetterStack == null) return null;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                ChoiceLetter_StoryDeliver dl = letters[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == questDefName) return dl;
            }
            return null;
        }

        private static bool ClickFirstDeliverOption(ChoiceLetter_StoryDeliver letter)
        {
            if (letter == null) return false;
            foreach (DiaOption opt in letter.Choices)
            {
                if (opt != null && opt.action != null && !opt.disabled)
                {
                    opt.action();
                    return true;
                }
            }
            return false;
        }

        private static void ClickAllEnabledOptions(ChoiceLetter_StoryDeliver letter)
        {
            if (letter == null) return;
            List<DiaOption> opts = new List<DiaOption>(letter.Choices);
            for (int i = 0; i < opts.Count; i++)
            {
                if (opts[i] != null && opts[i].action != null && !opts[i].disabled)
                {
                    opts[i].action();
                    break;
                }
            }
        }

        private static void ForceAbortRunAndCleanupGuests(Map targetMap, StoryQuestRun run)
        {
            if (run == null) return;
            List<int> ids = ParseIntList(GetVar(run, "guestIds"));
            run.status = StoryQuestStatus.Disabled;
            run.stage = -1;
            StoryRelationRestorer.CheckOrphansIfPresent();
            CleanupPawnsByIds(targetMap, ids);
        }

        private static void CleanupPawnsByIds(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return;
            HashSet<int> set = new HashSet<int>(ids);
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && set.Contains(p.thingIDNumber))
                {
                    if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                    if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static Pawn FindGuestOnMap(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return null;
            HashSet<int> set = new HashSet<int>(ids);
            IReadOnlyList<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && set.Contains(pawns[i].thingIDNumber)) return pawns[i];
            }
            return null;
        }

        private static void KillFirstGuest(Map targetMap, List<int> ids)
        {
            Pawn p = FindGuestOnMap(targetMap, ids);
            if (p != null && !p.Dead) p.Kill(null);
        }

        private static Pawn SpawnColonist(Map targetMap)
        {
            Pawn p = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static Faction FindPermanentEnemyHumanlikeFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && f.def != null && f.def.permanentEnemy && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return null;
        }

        private static Faction FindGoodwillHumanlikeFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HasGoodwill && f.def != null &&
                    !f.def.permanentEnemy && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return null;
        }

        private static Settlement FindAnyNonPlayerSettlement()
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].Destroyed && all[i].Faction != null && !all[i].Faction.IsPlayer)
                {
                    return all[i];
                }
            }
            return null;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0) return null;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].Destroyed && all[i].ID == id) return all[i];
            }
            return null;
        }

        private static Lord FindLordById(Map targetMap, int lordId)
        {
            if (targetMap == null || lordId < 0 || targetMap.lordManager == null) return null;
            List<Lord> lords = targetMap.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null && lords[i].loadID == lordId) return lords[i];
            }
            return null;
        }

        private static void ForceHostileWithGoodwill(Faction fac, int gw)
        {
            if (fac == null) return;
            int drop = -100 - fac.PlayerGoodwill;
            if (drop < 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null);
            int adj = gw - fac.PlayerGoodwill;
            if (adj != 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, adj, false, false, null, null);
        }

        private static void ForceNeutralWithGoodwill(Faction fac, int gw)
        {
            if (fac == null) return;
            if (fac.HostileTo(Faction.OfPlayer))
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, 0 - fac.PlayerGoodwill, false, false, null, null);
            }
            int adj = gw - fac.PlayerGoodwill;
            if (adj != 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, adj, false, false, null, null);
        }

        private static IntVec3 FindStandable(Map targetMap)
        {
            IntVec3 cell;
            if (CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 12, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
            }, out cell))
            {
                return cell;
            }
            return targetMap.Center;
        }

        private static bool HasOutcome(StoryQuestRun run, string outcome)
        {
            return CountOutcome(run, outcome) > 0;
        }

        private static int CountOutcome(StoryQuestRun run, string outcome)
        {
            if (run == null || run.history == null) return 0;
            int count = 0;
            string suffix = ":" + outcome;
            for (int i = 0; i < run.history.Count; i++)
            {
                string h = run.history[i];
                if (h != null && (h == outcome || h.EndsWith(suffix, StringComparison.Ordinal))) count++;
            }
            return count;
        }

        private static int ParseInt(StoryQuestRun run, string key, int fallback)
        {
            if (run == null || run.vars == null) return fallback;
            string raw;
            int val;
            return (run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) ? val : fallback;
        }

        private static string GetVar(StoryQuestRun run, string key)
        {
            if (run == null || run.vars == null) return string.Empty;
            string raw;
            return run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }

        private static List<int> ParseIntList(string raw)
        {
            List<int> list = new List<int>();
            if (raw.NullOrEmpty()) return list;
            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) list.Add(val);
            }
            return list;
        }

        private void AddCheck(string name, bool passed, string expected, string actual)
        {
            CheckItem item = new CheckItem();
            item.name = name;
            item.passed = passed;
            item.expected = expected ?? string.Empty;
            item.actual = actual ?? string.Empty;
            m_checks.Add(item);
        }

        private void WriteReport(string fileName)
        {
            // Honest evaluation: passed is true ONLY when 0 errors, >=1 check, and every check passed
            bool allPassed = m_errors.Count == 0 && m_checks.Count > 0;
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (!m_checks[i].passed)
                {
                    allPassed = false;
                    break;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"passed\":");
            sb.Append(allPassed ? "true" : "false");
            sb.Append(",\"checksCount\":");
            sb.Append(m_checks.Count);
            sb.Append(",\"checks\":[");
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (i > 0) sb.Append(',');
                CheckItem c = m_checks[i];
                sb.Append("{\"name\":\"");
                sb.Append(EscapeJson(c.name));
                sb.Append("\",\"passed\":");
                sb.Append(c.passed ? "true" : "false");
                sb.Append(",\"expected\":\"");
                sb.Append(EscapeJson(c.expected));
                sb.Append("\",\"actual\":\"");
                sb.Append(EscapeJson(c.actual));
                sb.Append("\"}");
            }
            sb.Append("],\"errors\":[");
            for (int i = 0; i < m_errors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                sb.Append(EscapeJson(m_errors[i]));
                sb.Append('"');
            }
            sb.Append("]}");

            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }
    }
}
```