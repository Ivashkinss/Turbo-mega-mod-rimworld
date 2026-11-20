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
