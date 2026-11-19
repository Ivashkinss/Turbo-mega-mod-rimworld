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

namespace TurboMegaMod
{
    /// <summary>
    /// Tracks a single active raid lease on a faction's hostility override.
    /// </summary>
    public sealed class StoryHostilityLease : IExposable
    {
        public string leaseId;
        public string quest;
        public string stageKey;
        public int runGen;
        public bool restoreRelations;

        public void ExposeData()
        {
            Scribe_Values.Look(ref leaseId, "leaseId");
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref restoreRelations, "restoreRelations", false);
        }
    }

    /// <summary>
    /// Stores original faction goodwill &amp; relation kind before any concurrent story raids forced
    /// the faction Hostile. Restoration runs ONLY when the final lease for that faction is released
    /// or orphaned (e.g. quest completed, failed, disabled, stage removed, or exception thrown).
    /// </summary>
    public sealed class StoryFactionBackup : IExposable
    {
        public int factionId = -1;
        public int origGoodwill;
        public int origKind = (int)FactionRelationKind.Neutral;
        public bool origHasGoodwill;
        public int postForceGoodwill = -100;
        public bool restoreWanted;
        public List<StoryHostilityLease> leases = new List<StoryHostilityLease>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionId, "factionId", -1);
            Scribe_Values.Look(ref origGoodwill, "origGoodwill", 0);
            Scribe_Values.Look(ref origKind, "origKind", (int)FactionRelationKind.Neutral);
            Scribe_Values.Look(ref origHasGoodwill, "origHasGoodwill", true);
            Scribe_Values.Look(ref postForceGoodwill, "postForceGoodwill", -100);
            Scribe_Values.Look(ref restoreWanted, "restoreWanted", false);
            Scribe_Collections.Look(ref leases, "leases", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && leases == null)
            {
                leases = new List<StoryHostilityLease>();
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
    /// WorldComponent that guarantees:
    /// 1) Temporary raid hostility is rolled back properly (handling RimWorld's goodwill/kind
    ///    hysteresis thresholds, concurrent raids on the same faction, mid-raid goodwill changes,
    ///    exceptions, disabled quests, and removed Defs/stages).
    /// 2) Retrieve quest items cannot merge into arbitrary stacks while active.
    /// </summary>
    public sealed class StoryRelationRestorer : WorldComponent
    {
        private const int CheckInterval = 250;

        private List<StoryFactionBackup> backups = new List<StoryFactionBackup>();
        private List<StoryProtectedItem> protectedItems = new List<StoryProtectedItem>();

        // Transient fast lookup for Thing.CanStackWith patch
        private static readonly HashSet<string> ActiveProtectedIds = new HashSet<string>();

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
        // HOSTILITY LEASES & GUARANTEED ROLLBACK
        // =====================================================================

        /// <summary>
        /// Records a rollback backup BEFORE mutating faction relations, forces the faction Hostile
        /// (verifying the resulting state), and tracks parallel raids on the same faction.
        /// Returns false if hostility could not be established (rolling back any partial state).
        /// </summary>
        public bool AcquireHostilityLease(Faction fac, StoryQuestRun run, bool restoreRelations, out string leaseId)
        {
            leaseId = null;
            if (fac == null || fac.IsPlayer || run == null) return false;

            leaseId = MakeLeaseId(run);
            StoryFactionBackup existing = FindBackup(fac.loadID);

            // If faction is already hostile AND we have no active backup for it, no rollback is needed.
            if (existing == null && fac.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            if (existing == null)
            {
                // Record original state BEFORE mutating anything
                existing = new StoryFactionBackup();
                existing.factionId = fac.loadID;
                existing.origGoodwill = fac.PlayerGoodwill;
                existing.origKind = (int)fac.RelationKindWith(Faction.OfPlayer);
                existing.origHasGoodwill = fac.HasGoodwill;
                existing.restoreWanted = restoreRelations;
                backups.Add(existing);

                AddOrUpdateLease(existing, leaseId, run, restoreRelations);

                if (!ForceFactionHostile(fac))
                {
                    // Rollback backup immediately if forcing hostility failed
                    backups.Remove(existing);
                    RestoreFactionRelations(fac, existing);
                    leaseId = null;
                    return false;
                }

                existing.postForceGoodwill = fac.PlayerGoodwill;
                return true;
            }

            // Concurrent raid on a faction that already has an active backup:
            // keep the original pre-first-raid baseline and add this raid's lease so restoration
            // waits until the LAST raid finishes.
            if (restoreRelations) existing.restoreWanted = true;
            AddOrUpdateLease(existing, leaseId, run, restoreRelations);

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

        /// <summary>
        /// Releases a specific raid lease. If no leases remain for that faction and restoreWanted is true,
        /// restores its relation kind and goodwill immediately.
        /// </summary>
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

        /// <summary>
        /// Audits all active leases and protected items against SparkStory.
        /// Any lease whose quest ended, was disabled, changed generation, or lost its Def/stage
        /// is immediately released and restored if it was the last lease for that faction.
        /// </summary>
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
                        if (!IsRaidLeaseStillActive(story, lease))
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
        }

        private static bool IsRaidLeaseStillActive(SparkStory story, StoryHostilityLease lease)
        {
            if (story == null || lease == null || lease.quest.NullOrEmpty()) return false;
            StoryQuestRun run = story.RunFor(lease.quest);
            if (run == null || run.status != StoryQuestStatus.Active) return false;
            if (run.gen != lease.runGen) return false;
            if (!lease.stageKey.NullOrEmpty() && run.stageKey != lease.stageKey) return false;

            StoryQuestDef def = run.Def;
            if (def == null || run.stage < 0 || run.stage >= def.stages.Count) return false;
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

        private void FinalizeBackup(StoryFactionBackup b)
        {
            if (b == null || !b.restoreWanted) return;
            Faction fac = FindFactionById(b.factionId);
            if (fac == null || fac.defeated) return;
            RestoreFactionRelations(fac, b);
        }

        /// <summary>
        /// Forces a faction Hostile to player and verifies the resulting relation kind.
        /// </summary>
        private static bool ForceFactionHostile(Faction fac)
        {
            if (fac == null || fac.IsPlayer) return false;
            if (fac.HostileTo(Faction.OfPlayer)) return true;

            if (fac.HasGoodwill && fac.CanChangeGoodwillFor(Faction.OfPlayer, -100)) // CHECK API
            {
                int drop = -100 - fac.PlayerGoodwill;
                if (drop < 0)
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null); // CHECK API
                }
            }

            if (!fac.HostileTo(Faction.OfPlayer))
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }

            return fac.HostileTo(Faction.OfPlayer);
        }

        /// <summary>
        /// Restores BOTH relation kind and goodwill, explicitly crossing RimWorld's CheckKindThresholds
        /// hysteresis boundary (0 for exiting Hostile) and accounting for external goodwill deltas
        /// accumulated while the raid was active.
        /// </summary>
        private static void RestoreFactionRelations(Faction fac, StoryFactionBackup b)
        {
            if (fac == null || b == null) return;

            FactionRelationKind origKind = (FactionRelationKind)b.origKind;

            // Preserve any external goodwill shifts that occurred during the raid (e.g. gifts or other quests)
            int externalDelta = fac.HasGoodwill ? (fac.PlayerGoodwill - b.postForceGoodwill) : 0;
            int targetGoodwill = Mathf.Clamp(b.origGoodwill + externalDelta, -100, 100);

            // Ensure target goodwill respects the original relation kind's valid range
            if (origKind == FactionRelationKind.Neutral && targetGoodwill < -74)
            {
                targetGoodwill = -74;
            }
            else if (origKind == FactionRelationKind.Ally && targetGoodwill < 0)
            {
                targetGoodwill = 0;
            }

            if (fac.HasGoodwill)
            {
                // Step 1: If currently Hostile and restoring to non-Hostile, cross the >= 0 threshold first
                // so vanilla FactionRelation.CheckKindThresholds cleanly drops Hostile and notifies maps/lords.
                if (fac.HostileTo(Faction.OfPlayer) && origKind != FactionRelationKind.Hostile)
                {
                    int liftTarget = origKind == FactionRelationKind.Ally ? Mathf.Max(75, targetGoodwill) : Mathf.Max(0, targetGoodwill);
                    int liftDelta = liftTarget - fac.PlayerGoodwill;
                    if (liftDelta > 0)
                    {
                        fac.TryAffectGoodwillWith(Faction.OfPlayer, liftDelta, false, false, null, null); // CHECK API
                    }
                }

                // Step 2: Adjust to exact targetGoodwill (e.g. 0 -> -50 stays Neutral because -50 > -75)
                int finalDelta = targetGoodwill - fac.PlayerGoodwill;
                if (finalDelta != 0)
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, finalDelta, false, false, null, null); // CHECK API
                }

                // Step 3: Verification & hard fallback if TryAffectGoodwillWith was refused or hysteresis stuck
                if (fac.RelationKindWith(Faction.OfPlayer) != origKind)
                {
                    fac.SetRelationDirect(Faction.OfPlayer, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API
                }

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

        private static void AddOrUpdateLease(StoryFactionBackup b, string leaseId, StoryQuestRun run, bool restoreRelations)
        {
            if (b.leases == null) b.leases = new List<StoryHostilityLease>();
            for (int i = 0; i < b.leases.Count; i++)
            {
                if (b.leases[i] != null && b.leases[i].leaseId == leaseId)
                {
                    b.leases[i].restoreRelations = restoreRelations;
                    return;
                }
            }
            StoryHostilityLease lease = new StoryHostilityLease();
            lease.leaseId = leaseId;
            lease.quest = run.quest;
            lease.stageKey = run.stageKey;
            lease.runGen = run.gen;
            lease.restoreRelations = restoreRelations;
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
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (backups == null) backups = new List<StoryFactionBackup>();
                if (protectedItems == null) protectedItems = new List<StoryProtectedItem>();
                RebuildProtectedCache();
            }
        }
    }

    // =========================================================================
    // HARMONY PATCH: BLOCK STACK MERGING FOR PROTECTED RETRIEVE QUEST ITEMS
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
        }

        public static void Postfix_CanStackWith(Thing __instance, Thing other, ref bool __result)
        {
            if (!__result) return;
            if (StoryRelationRestorer.IsProtectedThing(__instance) || StoryRelationRestorer.IsProtectedThing(other))
            {
                __result = false;
            }
        }
    }
}
```

`Source/StoryStage_Site.cs`

```csharp
using System;
using System.Collections;
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
    public enum StorySiteObjective
    {
        Clear,
        Rescue,
        Retrieve,
        Hold,
        KillHero
    }

    /// <summary>
    /// Quest stage that spawns a world-map Site and tracks completion of a real-world objective
    /// (Clear, Rescue, Retrieve, Hold, KillHero), timeout, failed, or unavailable.
    /// Rules enforced:
    /// - NEVER calls Site.Destroy() when a map is generated (vanilla Site.ShouldRemoveMapNow handles it).
    /// - Hero transfer only moves a free living world pawn, with full validation and rollback.
    /// - Retrieve locks the exact ThingID against stack merges; loss returns "failed" (no substitution).
    /// - Hold only accumulates confirmed intervals while a standing colonist is present; deadline wins first.
    /// - Batch/O(1) checks via HeroRegistry and typed ThingRequestGroups (no WorldPawns.AllPawnsDead scan).
    /// </summary>
    public sealed class StoryStage_Site : StoryStage
    {
        public const float TilesPerDay = 4f;

        public SitePartDef sitePart;
        public string enemyFaction;
        public float minDays = 2f;
        public float maxDays = 6f;
        public StorySiteObjective objective = StorySiteObjective.Clear;
        public HeroDef hero;
        public ThingDef thing;
        public float holdHours = 4f;
        public float timeoutDays = 15f;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        // Cached reflection handles for O(1) HeroRegistry state/record lookup
        private static bool s_heroRegChecked;
        private static MethodInfo s_heroRecordMethod;
        private static MethodInfo s_heroIsDeadMethod;
        private static FieldInfo s_heroRecordsDictField;

        public override string Mechanic
        {
            get { return "site"; }
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
            ctx.run.vars["mapInitialized"] = "false";
            ctx.run.vars["enteredMap"] = "false";
            ctx.run.vars["heldTicks"] = "0";
            ctx.run.vars["holdActive"] = "false";
            ctx.run.vars["lastHoldCheckTick"] = "-1";
            ctx.run.vars["heroSpawned"] = "false";
            ctx.run.vars["heroImprisonedOnSite"] = "false";
            ctx.run.vars["heroUnavailable"] = "false";
            ctx.run.vars["thingSpawned"] = "false";
            ctx.run.vars["thingSpawnFailed"] = "false";

            // Pre-check hero availability BEFORE creating a Site on the world map
            if (objective == StorySiteObjective.KillHero || objective == StorySiteObjective.Rescue)
            {
                if (!IsHeroEligibleForSite(hero))
                {
                    ctx.run.vars["heroUnavailable"] = "true";
                    ctx.run.vars["siteId"] = "-1";
                    return;
                }
            }

            PlanetTile homeTile = ResolveHomeTile(ctx); // CHECK API
            int minTiles = Mathf.Max(1, Mathf.RoundToInt(minDays * TilesPerDay));
            int maxTiles = Mathf.Max(minTiles, Mathf.RoundToInt(maxDays * TilesPerDay));

            PlanetTile siteTile; // CHECK API
            bool foundTile = TryFindSiteTile(homeTile, minTiles, maxTiles, out siteTile);
            if (!foundTile)
            {
                foundTile = TryFindSiteTile(homeTile, 1, Mathf.Max(maxTiles * 2, 30), out siteTile);
            }
            if (!foundTile)
            {
                Log.Warning("[TMM] StoryStage_Site: failed to find world tile for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                ctx.run.vars["siteId"] = "-1";
                return;
            }

            Faction fac = ResolveEnemyFaction(ctx);
            SitePartDef part = ResolveSitePart();
            if (part == null)
            {
                Log.Warning("[TMM] StoryStage_Site: no valid SitePartDef found.");
                ctx.run.vars["siteId"] = "-1";
                return;
            }

            float points = ctx.Home != null ? StorytellerUtility.DefaultThreatPointsNow(ctx.Home) : 400f;
            SitePartParams parms = part.Worker.GenerateDefaultParams(points, siteTile, fac); // CHECK API
            List<SitePartDefWithParams> parts = new List<SitePartDefWithParams>();
            parts.Add(new SitePartDefWithParams(part, parms));

            Site site = SiteMaker.MakeSite(parts, siteTile, fac, true, null); // CHECK API
            if (site == null)
            {
                Log.Warning("[TMM] StoryStage_Site: SiteMaker.MakeSite returned null.");
                ctx.run.vars["siteId"] = "-1";
                return;
            }

            TimeoutComp timeoutComp = site.GetComponent<TimeoutComp>();
            if (timeoutComp != null)
            {
                // Buffer so StoryStage_Site handles its own timeout deterministically while vanilla removes map-less sites safely
                timeoutComp.StartTimeout(Mathf.CeilToInt((timeoutDays + 2f) * GenDate.TicksPerDay)); // CHECK API
            }

            Find.WorldObjects.Add(site);
            ctx.run.vars["siteId"] = site.ID.ToString();

            string letterTitle = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Site"), ctx);
            string letterBody = StoryUtil.Format(!text.NullOrEmpty() ? text : Objective(ctx), ctx);
            Find.LetterStack.ReceiveLetter(letterTitle, letterBody, LetterDefOf.NeutralEvent, new LookTargets(site)); // CHECK API
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            int now = Find.TickManager.TicksGame;

            // Ensure deadlineTick is persisted if missing after a save migration (never slides forward)
            int deadline;
            if (!TryGetInt(ctx, "deadlineTick", out deadline))
            {
                deadline = now + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));
                ctx.run.vars["deadlineTick"] = deadline.ToString();
            }

            int siteId = GetInt(ctx, "siteId", -1);
            Site site = FindSiteById(siteId);

            // 1. Immediate unavailability / spawn failure checks
            if (GetBool(ctx, "heroUnavailable"))
            {
                ReleaseSite(site, true);
                return FinishStage(ctx, "unavailable");
            }
            if (GetBool(ctx, "thingSpawnFailed"))
            {
                ReleaseSite(site, true);
                return FinishStage(ctx, "failed");
            }

            // 2. Check deadline FIRST before objective completion (prevents Hold/other objectives succeeding past deadline)
            if (now >= deadline)
            {
                ReleaseSite(site, true);
                return FinishStage(ctx, "timeout");
            }

            // 3. Initialize site map once when player enters
            if (site != null && site.HasMap && site.Map != null)
            {
                ctx.run.vars["enteredMap"] = "true";
                if (!GetBool(ctx, "mapInitialized"))
                {
                    InitializeSiteMap(ctx, site);
                    ctx.run.vars["mapInitialized"] = "true";

                    if (GetBool(ctx, "heroUnavailable"))
                    {
                        ReleaseSite(site, true);
                        return FinishStage(ctx, "unavailable");
                    }
                    if (GetBool(ctx, "thingSpawnFailed"))
                    {
                        ReleaseSite(site, true);
                        return FinishStage(ctx, "failed");
                    }
                }
            }

            // 4. Evaluate objective (note: Rescue/Retrieve can succeed even on the tick the site disappears after caravan exit)
            string objOutcome = EvaluateObjective(ctx, site, now, deadline);
            if (!objOutcome.NullOrEmpty())
            {
                if (objOutcome == "failed" || objOutcome == "unavailable")
                {
                    ReleaseSite(site, true);
                }
                else
                {
                    ReleaseSite(site, false);
                }
                return FinishStage(ctx, objOutcome);
            }

            // 5. If the site vanished without fulfilling the objective, end with timeout
            if (site == null)
            {
                return FinishStage(ctx, "timeout");
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : text, ctx);
            if (baseText.NullOrEmpty())
            {
                baseText = DefaultObjectiveText();
            }

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            if (objective == StorySiteObjective.Hold)
            {
                int heldTicks = GetInt(ctx, "heldTicks", 0);
                float hoursDone = heldTicks / (float)GenDate.TicksPerHour;
                return string.Format("{0} ({1:0.0}/{2:0.0}h, {3:0.0}d left)", baseText, hoursDone, holdHours, daysLeft);
            }

            return string.Format("{0} ({1:0.0}d left)", baseText, daysLeft);
        }

        private string FinishStage(StoryContext ctx, string outcome)
        {
            string thingId = GetStr(ctx, "targetThingId");
            if (!thingId.NullOrEmpty() && StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.UnprotectRetrieveItem(thingId);
            }
            ctx.run.vars["finished"] = outcome;
            return outcome;
        }

        /// <summary>
        /// NEVER calls Site.Destroy() if the Site has a generated map!
        /// When a map exists, unmarks quest ownership and lets vanilla Site.ShouldRemoveMapNow +
        /// TimeoutComp clean up the map and site safely only after all player pawns/pods/downed
        /// colonists have left.
        /// </summary>
        private static void ReleaseSite(Site site, bool timedOut)
        {
            if (site == null || site.Destroyed) return;

            if (!site.HasMap)
            {
                if (timedOut)
                {
                    site.Destroy();
                }
                return;
            }

            // Generated map present: leave map cleanup strictly to vanilla Site.ShouldRemoveMapNow
            site.customLabel = null;
            if (timedOut)
            {
                TimeoutComp tc = site.GetComponent<TimeoutComp>();
                if (tc != null)
                {
                    // Vanilla TimeoutComp checks !parent.HasMap before destroying the Site; setting
                    // a short timeout ensures it vanishes cleanly once the player leaves the map.
                    tc.StartTimeout(1); // CHECK API
                }
            }
        }

        private string DefaultObjectiveText()
        {
            if (objective == StorySiteObjective.Clear) return "Clear hostile site";
            if (objective == StorySiteObjective.Rescue) return "Rescue captive hero";
            if (objective == StorySiteObjective.Retrieve) return "Retrieve target item";
            if (objective == StorySiteObjective.Hold) return "Hold site";
            if (objective == StorySiteObjective.KillHero) return "Eliminate target hero";
            return "Complete site objective";
        }

        // =====================================================================
        // SAFE HERO & ITEM INITIALIZATION ON SITE MAP
        // =====================================================================

        /// <summary>
        /// Validates that a hero is a free, living world pawn not belonging to the player,
        /// not spawned on any map, not in a caravan, not inside any container, and not a prisoner/slave.
        /// </summary>
        private static bool IsHeroEligibleForSite(HeroDef heroDef)
        {
            if (heroDef == null || HeroRegistry.Instance == null) return false;
            Pawn p = HeroRegistry.Instance.GetOrCreate(heroDef);
            return IsFreeWorldHeroPawn(p);
        }

        private static bool IsFreeWorldHeroPawn(Pawn p)
        {
            if (p == null || p.Dead || p.Destroyed) return false;
            if (p.Faction == Faction.OfPlayer) return false;
            if (p.Spawned) return false;
            if (p.IsPrisoner || p.IsSlave) return false;
            if (p.holdingOwner != null) return false;
            if (p.GetCaravan() != null) return false;
            if (Find.WorldPawns == null || !Find.WorldPawns.Contains(p)) return false;
            return true;
        }

        private void InitializeSiteMap(StoryContext ctx, Site site)
        {
            Map map = site.Map;
            if (map == null) return;

            if (objective == StorySiteObjective.KillHero)
            {
                if (!TryPlaceHeroForKill(ctx, site, map))
                {
                    ctx.run.vars["heroUnavailable"] = "true";
                }
            }
            else if (objective == StorySiteObjective.Rescue)
            {
                if (!TryPlaceHeroForRescue(ctx, site, map))
                {
                    ctx.run.vars["heroUnavailable"] = "true";
                }
            }
            else if (objective == StorySiteObjective.Retrieve)
            {
                if (!TryPlaceRetrieveItem(ctx, map))
                {
                    ctx.run.vars["thingSpawnFailed"] = "true";
                }
            }
        }

        private bool TryPlaceHeroForKill(StoryContext ctx, Site site, Map map)
        {
            if (hero == null || HeroRegistry.Instance == null || site.Faction == null) return false;

            Pawn target = HeroRegistry.Instance.GetOrCreate(hero);
            if (!IsFreeWorldHeroPawn(target)) return false;

            Faction oldFaction = target.Faction;
            bool wasLeader = oldFaction != null && oldFaction.leader == target;

            Find.WorldPawns.RemovePawn(target);
            if (target.Faction != site.Faction)
            {
                target.SetFaction(site.Faction);
            }

            IntVec3 cell = FindSpawnCellNearCenter(map);
            Thing spawned = GenSpawn.Spawn(target, cell, map, WipeMode.Vanish);

            if (spawned != target || !target.Spawned || target.Map != map)
            {
                // Rollback completely on spawn failure
                RollbackHeroToWorld(target, oldFaction, wasLeader, false);
                return false;
            }

            if (wasLeader && oldFaction != target.Faction)
            {
                ReplaceVacantLeader(oldFaction);
            }

            AttachToHostileLord(map, site.Faction, target, cell);
            HeroRegistry.Instance.NotifyEncountered(target);
            ctx.run.vars["targetPawnId"] = target.ThingID;
            ctx.run.vars["targetPawnIdNum"] = target.thingIDNumber.ToString();
            ctx.run.vars["heroSpawned"] = "true";
            return true;
        }

        private bool TryPlaceHeroForRescue(StoryContext ctx, Site site, Map map)
        {
            if (hero == null || HeroRegistry.Instance == null || site.Faction == null) return false;

            Pawn captive = HeroRegistry.Instance.GetOrCreate(hero);
            if (!IsFreeWorldHeroPawn(captive)) return false;

            Faction oldFaction = captive.Faction;
            bool wasLeader = oldFaction != null && oldFaction.leader == captive;

            Find.WorldPawns.RemovePawn(captive);
            if (captive.Faction == site.Faction)
            {
                captive.SetFaction(null);
            }

            IntVec3 cell = FindSpawnCellNearCenter(map);
            Thing spawned = GenSpawn.Spawn(captive, cell, map, WipeMode.Vanish);
            bool spawnedOk = spawned == captive && captive.Spawned && captive.Map == map;

            bool imprisonedOk = false;
            if (spawnedOk && captive.guest != null)
            {
                captive.guest.SetGuestStatus(site.Faction, GuestStatus.Prisoner); // CHECK API
                imprisonedOk = captive.IsPrisoner && captive.HostFaction == site.Faction;
            }

            if (!spawnedOk || !imprisonedOk)
            {
                // Rollback completely on spawn or imprisonment failure
                RollbackHeroToWorld(captive, oldFaction, wasLeader, true);
                return false;
            }

            if (wasLeader && oldFaction != captive.Faction)
            {
                ReplaceVacantLeader(oldFaction);
            }

            HeroRegistry.Instance.NotifyEncountered(captive);
            ctx.run.vars["targetPawnId"] = captive.ThingID;
            ctx.run.vars["targetPawnIdNum"] = captive.thingIDNumber.ToString();
            ctx.run.vars["heroSpawned"] = "true";
            ctx.run.vars["heroImprisonedOnSite"] = "true";
            return true;
        }

        private static void RollbackHeroToWorld(Pawn p, Faction oldFaction, bool wasLeader, bool clearGuestStatus)
        {
            if (p == null) return;
            if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
            if (clearGuestStatus && p.guest != null)
            {
                p.guest.SetGuestStatus(null, GuestStatus.Guest); // CHECK API
            }
            if (oldFaction != null && p.Faction != oldFaction)
            {
                p.SetFaction(oldFaction);
            }
            if (Find.WorldPawns != null && !Find.WorldPawns.Contains(p))
            {
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
            }
            if (wasLeader && oldFaction != null)
            {
                oldFaction.leader = p;
            }
        }

        private static void ReplaceVacantLeader(Faction oldFaction)
        {
            if (oldFaction == null || HeroRegistry.Instance == null) return;
            Pawn next = HeroRegistry.Instance.SuccessorForVacantLeadership(oldFaction);
            oldFaction.leader = next;
            if (next == null) oldFaction.TryGenerateNewLeader();
        }

        private bool TryPlaceRetrieveItem(StoryContext ctx, Map map)
        {
            if (thing == null || map == null) return false;

            ThingDef stuff = thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null;
            Thing item = ThingMaker.MakeThing(thing, stuff);
            if (item == null) return false;
            item.stackCount = 1;

            // Protect BEFORE placing so GenPlace.TryPlaceThing cannot merge it with any nearby stack
            if (StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.ProtectRetrieveItem(item.ThingID, ctx.run);
            }

            IntVec3 cell = FindSpawnCellNearCenter(map);
            Thing placed;
            bool ok = GenPlace.TryPlaceThing(item, cell, map, ThingPlaceMode.Near, out placed);
            if (!ok || placed == null || placed.Destroyed || !placed.Spawned || placed.Map != map || placed.ThingID != item.ThingID)
            {
                if (StoryRelationRestorer.Instance != null)
                {
                    StoryRelationRestorer.Instance.UnprotectRetrieveItem(item.ThingID);
                }
                if (!item.Destroyed) item.Destroy(DestroyMode.Vanish);
                return false;
            }

            ctx.run.vars["targetThingId"] = placed.ThingID;
            ctx.run.vars["thingSpawned"] = "true";
            return true;
        }

        // =====================================================================
        // OBJECTIVE EVALUATION
        // =====================================================================

        private string EvaluateObjective(StoryContext ctx, Site site, int now, int deadline)
        {
            if (objective == StorySiteObjective.Clear)
            {
                if (site != null && site.HasMap && site.Map != null && GetBool(ctx, "enteredMap"))
                {
                    if (!HasLivingHostilePawns(site.Map))
                    {
                        return "success";
                    }
                }
                return null;
            }

            if (objective == StorySiteObjective.KillHero)
            {
                if (!GetBool(ctx, "heroSpawned")) return null;
                string pawnId = GetStr(ctx, "targetPawnId");
                if (pawnId.NullOrEmpty()) return null;

                if (IsTrackedHeroConfirmedDead(hero, pawnId, site != null ? site.Map : null))
                {
                    return "success";
                }
                return null;
            }

            if (objective == StorySiteObjective.Rescue)
            {
                // Requires confirmed imprisonment on site map first (never triggers on pre-existing caravan pawn)
                if (!GetBool(ctx, "heroImprisonedOnSite")) return null;
                string pawnId = GetStr(ctx, "targetPawnId");
                if (pawnId.NullOrEmpty()) return null;

                if (IsTrackedHeroConfirmedDead(hero, pawnId, site != null ? site.Map : null))
                {
                    return "failed";
                }

                if (IsHeroExtractedToPlayerCaravan(pawnId, site) || IsPawnRescuedAtAnyHome(pawnId))
                {
                    return "success";
                }
                return null;
            }

            if (objective == StorySiteObjective.Retrieve)
            {
                if (!GetBool(ctx, "thingSpawned")) return null;
                string thingId = GetStr(ctx, "targetThingId");
                if (thingId.NullOrEmpty()) return "failed";

                // 1. Exact ThingID in any player caravan -> success!
                if (IsExactThingInPlayerCaravan(thingId))
                {
                    return "success";
                }

                // 2. Exact ThingID arrived on any player home map -> success!
                if (IsExactThingOnAnyPlayerHomeMap(thingId, thing))
                {
                    return "success";
                }

                // 3. Still on the site map (ground, pawn, or container) -> keep waiting
                if (site != null && site.HasMap && site.Map != null && IsExactThingOnMap(site.Map, thingId, thing))
                {
                    return null;
                }

                // 4. Check any other non-home active map just in case it is carried there
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    Map m = maps[i];
                    if (m == null || m.IsPlayerHome || (site != null && m == site.Map)) continue;
                    if (IsExactThingOnMap(m, thingId, thing)) return null;
                }

                // Exact ThingID no longer exists anywhere (destroyed or lost with map) -> explicit failed (NO substitution!)
                return "failed";
            }

            if (objective == StorySiteObjective.Hold)
            {
                bool presentNow = site != null && site.HasMap && site.Map != null && HasStandingPlayerPawns(site.Map);
                bool wasPresent = GetBool(ctx, "holdActive");
                int lastTick = GetInt(ctx, "lastHoldCheckTick", -1);

                if (!presentNow)
                {
                    // Absent or downed: stop accumulating, preserve already-earned heldTicks for re-entry
                    ctx.run.vars["holdActive"] = "false";
                    ctx.run.vars["lastHoldCheckTick"] = "-1";
                    return null;
                }

                if (!wasPresent || lastTick < 0)
                {
                    // First poll after entry or re-entry: mark active without adding prior elapsed ticks
                    ctx.run.vars["holdActive"] = "true";
                    ctx.run.vars["lastHoldCheckTick"] = now.ToString();
                    return null;
                }

                int cappedNow = Mathf.Min(now, deadline);
                int dt = Mathf.Max(0, cappedNow - lastTick);
                int held = GetInt(ctx, "heldTicks", 0) + dt;
                ctx.run.vars["heldTicks"] = held.ToString();
                ctx.run.vars["lastHoldCheckTick"] = now.ToString();

                int targetTicks = Mathf.Max(1, Mathf.RoundToInt(holdHours * GenDate.TicksPerHour));
                if (held >= targetTicks)
                {
                    return "success";
                }
                return null;
            }

            return null;
        }

        // =====================================================================
        // O(1) / SINGLE-PASS LOOKUP HELPERS
        // =====================================================================

        private static bool HasLivingHostilePawns(Map map)
        {
            if (map == null || map.mapPawns == null) return false;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.Downed) continue;
                if (p.IsPrisonerOfColony) continue;
                if (p.HostileTo(Faction.OfPlayer)) return true;
            }
            return false;
        }

        private static bool HasStandingPlayerPawns(Map map)
        {
            if (map == null || map.mapPawns == null) return false;
            IReadOnlyList<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && !p.Downed && !p.Destroyed) return true;
            }
            return false;
        }

        /// <summary>
        /// Confirms death of the SPECIFIC hero ThingID using site corpses, HeroRegistry record state
        /// (Dead + ThingID, never treating Missing as Dead), and home-map corpses — never scanning
        /// all dead WorldPawns.
        /// </summary>
        private static bool IsTrackedHeroConfirmedDead(HeroDef heroDef, string expectedThingId, Map siteMap)
        {
            if (expectedThingId.NullOrEmpty()) return false;

            // 1. Check site map corpses first (fast)
            if (HasCorpseWithId(siteMap, expectedThingId)) return true;

            // 2. Check HeroRegistry record directly (O(1))
            if (heroDef != null && HeroRegistry.Instance != null)
            {
                bool confirmed;
                if (TryCheckHeroRegistryDead(heroDef, expectedThingId, out confirmed))
                {
                    if (confirmed) return true;
                }
            }

            // 3. Check player home map corpses (in case corpse was hauled back)
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map m = maps[i];
                if (m != null && m.IsPlayerHome && m != siteMap && HasCorpseWithId(m, expectedThingId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasCorpseWithId(Map map, string pawnThingId)
        {
            if (map == null || map.listerThings == null) return false;
            List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int i = 0; i < corpses.Count; i++)
            {
                Corpse c = corpses[i] as Corpse;
                if (c != null && c.InnerPawn != null && c.InnerPawn.ThingID == pawnThingId)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryCheckHeroRegistryDead(HeroDef heroDef, string expectedThingId, out bool isConfirmedDead)
        {
            isConfirmedDead = false;
            HeroRegistry reg = HeroRegistry.Instance;
            if (reg == null || heroDef == null) return false;

            if (!s_heroRegChecked)
            {
                s_heroRegChecked = true;
                Type t = reg.GetType();
                // CHECK API: HeroRegistry record/state accessors
                s_heroRecordMethod = AccessTools.Method(t, "RecordFor", new Type[] { typeof(HeroDef) }) ??
                                     AccessTools.Method(t, "GetRecord", new Type[] { typeof(HeroDef) }) ??
                                     AccessTools.Method(t, "EntryFor", new Type[] { typeof(HeroDef) });
                s_heroIsDeadMethod = AccessTools.Method(t, "IsDead", new Type[] { typeof(HeroDef) });

                FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (typeof(IDictionary).IsAssignableFrom(fields[i].FieldType))
                    {
                        s_heroRecordsDictField = fields[i];
                        break;
                    }
                }
            }

            try
            {
                object rec = null;
                if (s_heroRecordMethod != null)
                {
                    rec = s_heroRecordMethod.Invoke(reg, new object[] { heroDef });
                }
                else if (s_heroRecordsDictField != null)
                {
                    IDictionary dict = s_heroRecordsDictField.GetValue(reg) as IDictionary;
                    if (dict != null)
                    {
                        if (dict.Contains(heroDef.defName)) rec = dict[heroDef.defName];
                        else if (dict.Contains(heroDef)) rec = dict[heroDef];
                    }
                }

                if (rec != null)
                {
                    // Direct Pawn stored in dictionary?
                    Pawn directPawn = rec as Pawn;
                    if (directPawn != null)
                    {
                        isConfirmedDead = directPawn.ThingID == expectedThingId && directPawn.Dead;
                        return true;
                    }

                    // Structured HeroRecord with pawn/thingId/state fields
                    Type rt = rec.GetType();
                    Pawn recPawn = ReadFieldOrProp(rec, rt, "pawn") as Pawn;
                    object stateObj = ReadFieldOrProp(rec, rt, "state");
                    object idObj = ReadFieldOrProp(rec, rt, "thingId") ?? ReadFieldOrProp(rec, rt, "thingID") ?? ReadFieldOrProp(rec, rt, "pawnId");

                    if (recPawn != null)
                    {
                        if (recPawn.ThingID == expectedThingId && recPawn.Dead)
                        {
                            isConfirmedDead = true;
                            return true;
                        }
                        if (recPawn.ThingID != expectedThingId)
                        {
                            // Different pawn instance than the one spawned at site
                            return true;
                        }
                    }

                    if (stateObj != null)
                    {
                        string stateName = stateObj.ToString();
                        if (string.Equals(stateName, "Dead", StringComparison.OrdinalIgnoreCase))
                        {
                            if (idObj == null || string.Equals(idObj.ToString(), expectedThingId, StringComparison.Ordinal))
                            {
                                isConfirmedDead = true;
                            }
                            return true;
                        }
                        // Missing / Alive / Active -> explicitly NOT dead
                        return true;
                    }
                }

                if (s_heroIsDeadMethod != null)
                {
                    object res = s_heroIsDeadMethod.Invoke(reg, new object[] { heroDef });
                    if (res is bool)
                    {
                        isConfirmedDead = (bool)res;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static object ReadFieldOrProp(object obj, Type t, string name)
        {
            FieldInfo f = AccessTools.Field(t, name);
            if (f != null) return f.GetValue(obj);
            PropertyInfo p = AccessTools.Property(t, name);
            if (p != null) return p.GetValue(obj, null);
            return null;
        }

        private static bool IsHeroExtractedToPlayerCaravan(string pawnId, Site site)
        {
            if (pawnId.NullOrEmpty() || Find.WorldObjects == null) return false;
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Destroyed || c.Faction != Faction.OfPlayer) continue;

                // Caravan must have left the site map (in RimWorld forming a caravan removes pawns from map;
                // also ensure if site still somehow has a map that the caravan moved off or pawn isn't spawned there)
                List<Pawn> pawns = c.PawnsListForReading;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn p = pawns[j];
                    if (p != null && !p.Dead && !p.Spawned && p.ThingID == pawnId)
                    {
                        if (site == null || !site.HasMap || c.Tile != site.Tile)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool IsPawnRescuedAtAnyHome(string pawnId)
        {
            if (pawnId.NullOrEmpty()) return false;
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map home = maps[m];
                if (home == null || !home.IsPlayerHome || home.mapPawns == null) continue;
                IReadOnlyList<Pawn> pawns = home.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p != null && !p.Dead && p.ThingID == pawnId && (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsExactThingInPlayerCaravan(string thingId)
        {
            if (thingId.NullOrEmpty() || Find.WorldObjects == null) return false;
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Destroyed || c.Faction != Faction.OfPlayer) continue;

                List<Pawn> pawns = c.PawnsListForReading;
                for (int pIdx = 0; pIdx < pawns.Count; pIdx++)
                {
                    if (PawnHasExactThing(pawns[pIdx], thingId)) return true;
                }
            }
            return false;
        }

        private static bool IsExactThingOnAnyPlayerHomeMap(string thingId, ThingDef defHint)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map m = maps[i];
                if (m != null && m.IsPlayerHome && IsExactThingOnMap(m, thingId, defHint))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Single targeted search for an exact ThingID on a map:
        /// 1) listerThings.ThingsOfDef(defHint) (fast typed list),
        /// 2) pawns' inventory/equipment/apparel/carryTracker,
        /// 3) ThingRequestGroup.ThingHolder containers (pods, shelves/crates/etc.).
        /// </summary>
        private static bool IsExactThingOnMap(Map map, string thingId, ThingDef defHint)
        {
            if (map == null || thingId.NullOrEmpty()) return false;

            if (defHint != null && map.listerThings != null)
            {
                List<Thing> byDef = map.listerThings.ThingsOfDef(defHint);
                for (int i = 0; i < byDef.Count; i++)
                {
                    Thing t = byDef[i];
                    if (t != null && !t.Destroyed && t.ThingID == thingId) return true;
                }
            }

            if (map.mapPawns != null)
            {
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (PawnHasExactThing(pawns[i], thingId)) return true;
                }
            }

            if (map.listerThings != null)
            {
                List<Thing> holders = map.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    if (holders[i] is Pawn) continue; // already checked above
                    if (HolderContainsExactThing(holders[i] as IThingHolder, thingId)) return true;
                }
            }

            return false;
        }

        private static bool PawnHasExactThing(Pawn p, string thingId)
        {
            if (p == null) return false;

            if (p.carryTracker != null && p.carryTracker.CarriedThing != null &&
                !p.carryTracker.CarriedThing.Destroyed && p.carryTracker.CarriedThing.ThingID == thingId)
            {
                return true;
            }

            if (p.inventory != null && p.inventory.innerContainer != null)
            {
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int i = 0; i < inv.Count; i++)
                {
                    Thing t = inv[i];
                    if (t != null && !t.Destroyed && t.ThingID == thingId) return true;
                }
            }

            if (p.equipment != null && p.equipment.AllEquipmentListForReading != null)
            {
                List<ThingWithComps> eq = p.equipment.AllEquipmentListForReading;
                for (int i = 0; i < eq.Count; i++)
                {
                    if (eq[i] != null && !eq[i].Destroyed && eq[i].ThingID == thingId) return true;
                }
            }

            if (p.apparel != null && p.apparel.WornApparel != null)
            {
                List<Apparel> worn = p.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    if (worn[i] != null && !worn[i].Destroyed && worn[i].ThingID == thingId) return true;
                }
            }

            return false;
        }

        private static bool HolderContainsExactThing(IThingHolder holder, string thingId)
        {
            if (holder == null) return false;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Thing t = owner[i];
                    if (t != null && !t.Destroyed && t.ThingID == thingId) return true;
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                if (HolderContainsExactThing(children[i], thingId)) return true;
            }
            return false;
        }

        // =====================================================================
        // SITE CREATION HELPERS (1.7 SIGNATURES PRESERVED)
        // =====================================================================

        private static IntVec3 FindSpawnCellNearCenter(Map map)
        {
            IntVec3 center = map.Center;
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(center, map, 14, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out found))
            {
                return found;
            }
            if (CellFinder.TryFindRandomCellNear(center, map, 25, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map);
            }, out found))
            {
                return found;
            }
            return center;
        }

        private static void AttachToHostileLord(Map map, Faction fac, Pawn pawn, IntVec3 cell)
        {
            if (map == null || fac == null || pawn == null) return;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord l = lords[i];
                if (l != null && l.faction == fac)
                {
                    l.AddPawn(pawn);
                    return;
                }
            }
            List<Pawn> group = new List<Pawn>();
            group.Add(pawn);
            LordMaker.MakeNewLord(fac, new LordJob_DefendPoint(cell), map, group);
        }

        private static Site FindSiteById(int siteId)
        {
            if (siteId < 0 || Find.WorldObjects == null) return null;
            List<Site> sites = Find.WorldObjects.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                Site s = sites[i];
                if (s != null && !s.Destroyed && s.ID == siteId) return s;
            }
            return null;
        }

        private PlanetTile ResolveHomeTile(StoryContext ctx) // CHECK API
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

        private static bool TryFindSiteTile(PlanetTile rootTile, int minTiles, int maxTiles, out PlanetTile tile) // CHECK API
        {
            if (rootTile.Valid) // CHECK API
            {
                if (TileFinder.TryFindPassableTileWithTraversalDistance(rootTile, minTiles, maxTiles, out tile, delegate(PlanetTile t)
                {
                    return !Find.WorldObjects.AnyWorldObjectAt(t) && TileFinder.IsValidTileForNewSettlement(t, null, false);
                }, false, TileFinderMode.Near, false, false)) // CHECK API
                {
                    return true;
                }
            }
            return TileFinder.TryFindNewSiteTile(out tile, rootTile, minTiles, maxTiles, false, null, 0f, false, TileFinderMode.Near, false, false, null, null); // CHECK API
        }

        private Faction ResolveEnemyFaction(StoryContext ctx)
        {
            if (!enemyFaction.NullOrEmpty())
            {
                Faction explicitFac = StoryUtil.FactionOf(enemyFaction);
                if (explicitFac != null && !explicitFac.defeated) return explicitFac;
            }

            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && f.HostileTo(Faction.OfPlayer))
                {
                    return f;
                }
            }
            return Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined); // CHECK API
        }

        private SitePartDef ResolveSitePart()
        {
            if (sitePart != null) return sitePart;
            SitePartDef outpost = DefDatabase<SitePartDef>.GetNamedSilentFail("BanditCamp");
            if (outpost != null) return outpost;
            outpost = DefDatabase<SitePartDef>.GetNamedSilentFail("Outpost");
            if (outpost != null) return outpost;
            List<SitePartDef> all = DefDatabase<SitePartDef>.AllDefsListForReading;
            if (all != null && all.Count > 0) return all[0];
            return null;
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

`Source/StoryStage_ColonyRaid.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Quest stage that launches a vanilla IncidentDefOf.RaidEnemy raid against a pinned home map
    /// after delayHours, tracking the exact raid map uniqueID and complete incident pawn cohort
    /// (including incoming pods/skyfallers) in a single pass per tick, and delegating hostility
    /// rollback to StoryRelationRestorer so concurrent raids, exceptions, and disabled quests
    /// always clean up cleanly.
    /// </summary>
    public sealed class StoryStage_ColonyRaid : StoryStage
    {
        public string faction;
        public float pointsFactor = 1.0f;
        public float delayHours = 2.0f;
        public float timeoutDays = 3.0f;
        public bool restoreRelations = false;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "raid"; }
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
            int delayTicks = Mathf.Max(0, Mathf.RoundToInt(delayHours * GenDate.TicksPerHour));
            int spawnTick = now + delayTicks;
            int deadlineTick = spawnTick + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["spawnTick"] = spawnTick.ToString();
            ctx.run.vars["deadlineTick"] = deadlineTick.ToString();
            ctx.run.vars["spawned"] = "false";
            ctx.run.vars["raidLordId"] = "-1";
            ctx.run.vars["raidMapId"] = ctx.Home != null ? ctx.Home.uniqueID.ToString() : "-1";

            if (!text.NullOrEmpty() && delayHours > 0f)
            {
                string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Incoming Raid"), ctx);
                string body = StoryUtil.Format(text, ctx);
                Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.ThreatSmall,
                    ctx.Home != null ? new LookTargets(ctx.Home.Center, ctx.Home) : LookTargets.Invalid); // CHECK API
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            int now = Find.TickManager.TicksGame;

            int spawnTick;
            if (!TryGetInt(ctx, "spawnTick", out spawnTick))
            {
                spawnTick = now;
                ctx.run.vars["spawnTick"] = spawnTick.ToString();
            }

            // Persist missing deadlineTick once so it never slides forward on migration
            int deadlineTick;
            if (!TryGetInt(ctx, "deadlineTick", out deadlineTick))
            {
                deadlineTick = spawnTick + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));
                ctx.run.vars["deadlineTick"] = deadlineTick.ToString();
            }

            if (!GetBool(ctx, "spawned"))
            {
                // Always check deadline BEFORE attempting to spawn (even if a home map just appeared)
                if (now >= deadlineTick)
                {
                    return Finish(ctx, "timeout");
                }

                if (now < spawnTick) return null;

                Map targetMap = ResolveTargetHomeMap(ctx);
                if (targetMap == null)
                {
                    return null;
                }

                if (!SpawnRaid(ctx, targetMap))
                {
                    Log.Warning("[TMM] StoryStage_ColonyRaid: failed to spawn raid for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                    return Finish(ctx, "timeout");
                }

                ctx.run.vars["spawned"] = "true";
                return null;
            }

            // Already spawned: check deadline first
            if (now >= deadlineTick)
            {
                return Finish(ctx, "timeout");
            }

            // Track ONLY the exact pinned raid map (never silently switch to another colony map!)
            int raidMapId = GetInt(ctx, "raidMapId", -1);
            Map raidMap = FindMapByUniqueId(raidMapId);
            if (raidMap == null)
            {
                return Finish(ctx, "timeout");
            }

            HashSet<int> trackedSet = ParseIntSet(GetStr(ctx, "raidPawnIds"));
            int lordId = GetInt(ctx, "raidLordId", -1);

            // Empty identification -> explicit timeout, NEVER success
            if (trackedSet.Count == 0 && lordId < 0)
            {
                return Finish(ctx, "timeout");
            }

            if (IsRaidClearedSinglePass(raidMap, trackedSet, lordId))
            {
                return Finish(ctx, "success");
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : text, ctx);
            if (baseText.NullOrEmpty())
            {
                baseText = "Repel incoming raid";
            }

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            if (!GetBool(ctx, "spawned"))
            {
                int spawnTick = GetInt(ctx, "spawnTick", now);
                float hoursLeft = Mathf.Max(0f, (spawnTick - now) / (float)GenDate.TicksPerHour);
                return string.Format("{0} (arriving in {1:0.0}h)", baseText, hoursLeft);
            }

            int deadline = GetInt(ctx, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);
            return string.Format("{0} ({1:0.0}d left)", baseText, daysLeft);
        }

        private bool SpawnRaid(StoryContext ctx, Map targetMap)
        {
            Faction raidFac = ResolveRaidFaction(ctx);
            if (raidFac == null || targetMap == null) return false;

            ctx.run.vars["raidFactionId"] = raidFac.loadID.ToString();
            ctx.run.vars["raidMapId"] = targetMap.uniqueID.ToString();

            // Record baseline in StoryRelationRestorer BEFORE mutating faction hostility
            string leaseId = null;
            StoryRelationRestorer restorer = StoryRelationRestorer.Instance;
            if (restorer != null)
            {
                if (!restorer.AcquireHostilityLease(raidFac, ctx.run, restoreRelations, out leaseId))
                {
                    return false;
                }
                if (!leaseId.NullOrEmpty())
                {
                    ctx.run.vars["relationLeaseId"] = leaseId;
                }
            }
            else if (!raidFac.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            // Snapshot ALL existing pawns of raidFac on targetMap (both spawned AND inside any pods/holders)
            HashSet<int> prevPawns = SnapshotAllFactionPawnsOnMap(targetMap, raidFac);
            HashSet<int> prevLords = SnapshotLords(targetMap);

            float rawPoints = StorytellerUtility.DefaultThreatPointsNow(targetMap) * Mathf.Max(0.1f, pointsFactor);
            float minPoints = 150f;
            if (raidFac.def != null)
            {
                float facMin = raidFac.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null); // CHECK API
                if (facMin > minPoints) minPoints = facMin;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, targetMap);
            parms.target = targetMap;
            parms.faction = raidFac;
            parms.points = Mathf.Max(minPoints, rawPoints);
            parms.forced = true;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;

            bool executed = false;
            try
            {
                executed = IncidentDefOf.RaidEnemy.Worker.TryExecute(parms); // CHECK API
                if (!executed)
                {
                    parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                    executed = IncidentDefOf.RaidEnemy.Worker.TryExecute(parms); // CHECK API
                }
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] StoryStage_ColonyRaid: TryExecute threw exception: " + ex);
                ReleaseHostilityLease(ctx);
                return false;
            }

            if (!executed)
            {
                ReleaseHostilityLease(ctx);
                return false;
            }

            // Collect COMPLETE cohort: new lord ownedPawns + newly spawned pawns + pawns inside new drop pods/skyfallers
            List<int> trackedIds = new List<int>();
            HashSet<int> trackedUnique = new HashSet<int>();
            int newLordId = -1;

            List<Lord> lords = targetMap.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord l = lords[i];
                if (l == null || l.faction != raidFac) continue;
                if (!prevLords.Contains(l.loadID))
                {
                    newLordId = l.loadID;
                    if (l.ownedPawns != null)
                    {
                        for (int pIdx = 0; pIdx < l.ownedPawns.Count; pIdx++)
                        {
                            Pawn p = l.ownedPawns[pIdx];
                            if (p != null && trackedUnique.Add(p.thingIDNumber))
                            {
                                trackedIds.Add(p.thingIDNumber);
                            }
                        }
                    }
                    break;
                }
            }

            HashSet<int> afterPawns = SnapshotAllFactionPawnsOnMap(targetMap, raidFac);
            foreach (int id in afterPawns)
            {
                if (!prevPawns.Contains(id) && trackedUnique.Add(id))
                {
                    trackedIds.Add(id);
                }
            }

            // If incident spawned zero identifiable pawns and no lord, fail safely (timeout) and release lease
            if (trackedIds.Count == 0 && newLordId < 0)
            {
                ReleaseHostilityLease(ctx);
                return false;
            }

            ctx.run.vars["raidLordId"] = newLordId.ToString();
            ctx.run.vars["raidPawnIds"] = JoinInts(trackedIds);
            return true;
        }

        /// <summary>
        /// Single-pass batch check across the pinned raid map's spawned pawns, ThingHolders (pods/skyfallers),
        /// and the raid's Lord. Never falls back to unrelated pawns of the same faction.
        /// </summary>
        private static bool IsRaidClearedSinglePass(Map raidMap, HashSet<int> trackedSet, int lordId)
        {
            if (raidMap == null) return false;

            // 1. Check the raid's Lord if still active on raidMap
            if (lordId >= 0 && raidMap.lordManager != null)
            {
                List<Lord> lords = raidMap.lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    Lord l = lords[i];
                    if (l == null || l.loadID != lordId) continue;
                    if (l.ownedPawns != null)
                    {
                        for (int pIdx = 0; pIdx < l.ownedPawns.Count; pIdx++)
                        {
                            Pawn lp = l.ownedPawns[pIdx];
                            if (IsActiveRaider(lp, raidMap)) return false;
                        }
                    }
                    break;
                }
            }

            if (trackedSet.Count == 0)
            {
                // Lord was verified above and has no active pawns remaining
                return lordId >= 0;
            }

            // 2. Single pass over spawned pawns on raidMap
            if (raidMap.mapPawns != null)
            {
                IReadOnlyList<Pawn> spawned = raidMap.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawned.Count; i++)
                {
                    Pawn p = spawned[i];
                    if (p != null && trackedSet.Contains(p.thingIDNumber) && IsActiveRaider(p, raidMap))
                    {
                        return false;
                    }
                }
            }

            // 3. Single pass over ThingHolder things on raidMap (covers DropPodIncoming / Skyfallers / ActiveTransporter)
            if (raidMap.listerThings != null)
            {
                List<Thing> holders = raidMap.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    Thing h = holders[i];
                    if (h == null || h.Destroyed || h is Pawn) continue;
                    if (HolderHasActiveTrackedRaider(h as IThingHolder, trackedSet, raidMap))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsActiveRaider(Pawn p, Map raidMap)
        {
            if (p == null || p.Dead || p.Destroyed) return false;
            if (p.IsPrisonerOfColony || p.IsSlaveOfColony) return false;

            // Still inside an incoming pod / skyfaller on raidMap
            if (!p.Spawned && p.MapHeld == raidMap) return true;
            if (!p.Spawned || p.Map != raidMap) return false;
            if (p.Downed) return false;

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

        private static bool HolderHasActiveTrackedRaider(IThingHolder holder, HashSet<int> trackedSet, Map raidMap)
        {
            if (holder == null) return false;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn p = owner[i] as Pawn;
                    if (p != null && trackedSet.Contains(p.thingIDNumber) && !p.Dead && !p.Destroyed && !p.IsPrisonerOfColony)
                    {
                        return true;
                    }
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                if (HolderHasActiveTrackedRaider(children[i], trackedSet, raidMap)) return true;
            }
            return false;
        }

        private string Finish(StoryContext ctx, string outcome)
        {
            ReleaseHostilityLease(ctx);
            ctx.run.vars["finished"] = outcome;
            return outcome;
        }

        private static void ReleaseHostilityLease(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;
            string leaseId = GetStr(ctx, "relationLeaseId");
            if (!leaseId.NullOrEmpty() && StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.ReleaseLease(leaseId);
                ctx.run.vars.Remove("relationLeaseId");
            }
        }

        private static Map ResolveTargetHomeMap(StoryContext ctx)
        {
            int pinnedId = GetInt(ctx, "raidMapId", -1);
            if (pinnedId >= 0)
            {
                Map pinned = FindMapByUniqueId(pinnedId);
                if (pinned != null && pinned.IsPlayerHome) return pinned;
            }
            return ctx.Home;
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

        private Faction ResolveRaidFaction(StoryContext ctx)
        {
            if (!faction.NullOrEmpty())
            {
                Faction explicitFac = StoryUtil.FactionOf(faction);
                if (explicitFac != null && !explicitFac.defeated) return explicitFac;
            }
            if (ctx.Faction != null && !ctx.Faction.defeated && !ctx.Faction.IsPlayer)
            {
                return ctx.Faction;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined); // CHECK API
        }

        private static HashSet<int> SnapshotAllFactionPawnsOnMap(Map map, Faction fac)
        {
            HashSet<int> ids = new HashSet<int>();
            if (map == null || fac == null) return ids;

            if (map.mapPawns != null)
            {
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p != null && p.Faction == fac) ids.Add(p.thingIDNumber);
                }
            }

            if (map.listerThings != null)
            {
                List<Thing> holders = map.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    Thing h = holders[i];
                    if (h == null || h.Destroyed || h is Pawn) continue;
                    CollectFactionPawnsInHolder(h as IThingHolder, fac, ids);
                }
            }

            return ids;
        }

        private static void CollectFactionPawnsInHolder(IThingHolder holder, Faction fac, HashSet<int> ids)
        {
            if (holder == null) return;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn p = owner[i] as Pawn;
                    if (p != null && p.Faction == fac) ids.Add(p.thingIDNumber);
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                CollectFactionPawnsInHolder(children[i], fac, ids);
            }
        }

        private static HashSet<int> SnapshotLords(Map map)
        {
            HashSet<int> ids = new HashSet<int>();
            if (map == null || map.lordManager == null) return ids;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null) ids.Add(lords[i].loadID);
            }
            return ids;
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

        private static HashSet<int> ParseIntSet(string raw)
        {
            HashSet<int> result = new HashSet<int>();
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

`Source/SparkStory.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    // Appended values only: saves store the enum by name.
    public enum StoryQuestStatus { Offered, Active, Completed, Failed, Declined, Disabled }

    /// Runtime record of one quest. Saved by defName so a removed StoryQuestDef never breaks a save.
    public sealed class StoryQuestRun : IExposable
    {
        public string quest;
        public StoryQuestStatus status;
        public int stage = -1;
        public string stageKey;          // stable key of the current stage (StoryStage.key or its index); survives reordered XML
        public int gen;                  // generation: bumped on every offer and stage entry; letters carry it to reject stale clicks
        public string outcome;
        public string chosenOutcome;     // set by the choice letter, consumed by StoryStage_Choice.Tick
        public bool pendingChoice;
        public bool needsLetter;         // a lost offer/choice letter must be re-sent once by the director
        public int stageUntilTick = -1;
        public int offeredTick = -1;
        public int cooldownUntilTick = -1;
        public int finishedTick = -1;
        public List<string> history = new List<string>(); // "stage:outcome" for the Chronicle log
        public Dictionary<string, string> vars = new Dictionary<string, string>(); // per-stage worker data (E2/E3)

        public StoryQuestDef Def { get { return DefDatabase<StoryQuestDef>.GetNamedSilentFail(quest); } }

        public void ExposeData()
        {
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref status, "status", StoryQuestStatus.Offered);
            Scribe_Values.Look(ref stage, "stage", -1);
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref gen, "gen", 0);
            Scribe_Values.Look(ref outcome, "outcome");
            Scribe_Values.Look(ref chosenOutcome, "chosenOutcome");
            Scribe_Values.Look(ref pendingChoice, "pendingChoice", false);
            Scribe_Values.Look(ref needsLetter, "needsLetter", false);
            Scribe_Values.Look(ref stageUntilTick, "stageUntilTick", -1);
            Scribe_Values.Look(ref offeredTick, "offeredTick", -1);
            Scribe_Values.Look(ref cooldownUntilTick, "cooldownUntilTick", -1);
            Scribe_Values.Look(ref finishedTick, "finishedTick", -1);
            Scribe_Collections.Look(ref history, "history", LookMode.Value);
            Scribe_Collections.Look(ref vars, "vars", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (history == null) history = new List<string>();
                if (vars == null) vars = new Dictionary<string, string>();
            }
        }
    }

    /// The whole story state: flags, quest runs and the director (pacing). Design: Documentation/STORY-ENGINE-DESIGN.md.
    public sealed class SparkStory : WorldComponent
    {
        public const int TickInterval = 250;
        public static int MaxActive = 2;                 // non-main quests at once (offered faction quests and non-main autoStart)
        public static int MinOfferGapTicks = 30000;      // half a day between offers

        private Dictionary<string, string> flags = new Dictionary<string, string>();
        private List<StoryQuestRun> runs = new List<StoryQuestRun>();
        public int lastOfferTick = -999999;
        public string lastMechanic = "";
        private bool reconciled;                         // transient: reconcile once after load/start
        private Dictionary<string, string> transientForcedOutcomes = new Dictionary<string, string>(); // never serialized to saves

        public SparkStory(World world) : base(world) { }

        public static SparkStory Instance { get { return Find.World == null ? null : Find.World.GetComponent<SparkStory>(); } }
        public IEnumerable<StoryQuestRun> Runs { get { return runs; } }
        public IEnumerable<KeyValuePair<string, string>> Flags { get { return flags; } }

        private static bool IsStoryTestMode
        {
            get { return Environment.GetCommandLineArgs().Contains("-tmm-story-test"); }
        }

        // ------------------------------------------------ flags
        public string GetFlag(string key) { string v; return key != null && flags.TryGetValue(key, out v) ? v : ""; }
        public int GetInt(string key) { int v; return int.TryParse(GetFlag(key), out v) ? v : 0; }
        public void SetFlag(string key, string value) { if (!key.NullOrEmpty()) flags[key] = value ?? ""; }

        // ------------------------------------------------ queries
        public StoryQuestRun RunFor(string quest) { return runs.FirstOrDefault(r => r.quest == quest); }
        public bool IsDone(string quest) { StoryQuestRun r = RunFor(quest); return r != null && (r.status == StoryQuestStatus.Completed || r.status == StoryQuestStatus.Failed); }
        public bool IsActive(string quest) { StoryQuestRun r = RunFor(quest); return r != null && r.status == StoryQuestStatus.Active; }

        public bool IsAvailable(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r != null && r.status != StoryQuestStatus.Declined) return false;
            if (r != null && Find.TickManager.TicksGame < r.cooldownUntilTick) return false;
            int act = Math.Max(1, GetInt("act"));
            if (d.act > act) return false;
            if (!d.faction.NullOrEmpty() && StoryUtil.FactionOf(d.faction) == null) return false;
            return StoryConditions.Holds(this, d.requires, d.anyOf);
        }

        public StoryContext Context(StoryQuestRun r) { return new StoryContext { state = this, run = r, def = r.Def }; }

        private int NonMainBusy()
        {
            return runs.Count(x => (x.status == StoryQuestStatus.Active || x.status == StoryQuestStatus.Offered) && x.Def != null && !x.Def.main);
        }

        // ------------------------------------------------ lifecycle
        public StoryQuestRun Offer(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r == null) { r = new StoryQuestRun { quest = d.defName }; runs.Add(r); }
            r.status = StoryQuestStatus.Offered;
            r.gen++;
            r.offeredTick = Find.TickManager.TicksGame;
            r.needsLetter = false;
            lastOfferTick = r.offeredTick;
            StoryChoiceLetter.Send(Context(r), -1, StoryChoiceLetter.KindOffer);
            return r;
        }

        /// Accept/Decline/Choose take the letter's generation and refuse stale or repeated clicks.
        public bool Accept(StoryQuestRun r, int letterGen)
        {
            if (r == null || r.status != StoryQuestStatus.Offered || r.gen != letterGen || r.Def == null) return false;
            r.status = StoryQuestStatus.Active;
            StartStage(r, 0);
            return true;
        }

        public bool Decline(StoryQuestRun r, int letterGen)
        {
            if (r == null || r.status != StoryQuestStatus.Offered || r.gen != letterGen) return false;
            StoryQuestDef d = r.Def;
            r.status = StoryQuestStatus.Declined;
            r.gen++;
            r.cooldownUntilTick = Find.TickManager.TicksGame + (int)((d == null ? 3f : d.offerCooldownDays) * GenDate.TicksPerDay);
            return true;
        }

        public bool Choose(StoryQuestRun r, int stage, int letterGen, string outcome)
        {
            if (r == null || r.status != StoryQuestStatus.Active || r.stage != stage || r.gen != letterGen || !r.pendingChoice) return false;
            StoryQuestDef d = r.Def;
            var st = d == null || stage < 0 || stage >= d.stages.Count ? null : d.stages[stage] as StoryStage_Choice;
            StoryChoice c = st == null ? null : st.ChoiceFor(outcome);
            if (c == null || !StoryConditions.Holds(this, c.requires, c.anyOf)) return false; // re-check requires at click time
            r.chosenOutcome = outcome;
            SafeTick(r); // resolve immediately so the player sees the consequence at once
            return true;
        }

        /// A letter of the current generation was dismissed unanswered: an offer counts as "later",
        /// a choice letter is re-sent once by the director (deduplicated by generation).
        public void Notify_LetterLost(StoryQuestRun r, string kind, int letterGen)
        {
            if (r == null || r.gen != letterGen) return;
            if (kind == StoryChoiceLetter.KindOffer) { Decline(r, letterGen); return; }
            if (r.status == StoryQuestStatus.Active && r.pendingChoice) r.needsLetter = true;
        }

        private void StartQuestDirect(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r == null) { r = new StoryQuestRun { quest = d.defName }; runs.Add(r); }
            r.status = StoryQuestStatus.Active;
            StartStage(r, 0);
        }

        private void StartStage(StoryQuestRun r, int index)
        {
            StoryQuestDef d = r.Def;
            if (d == null) { Disable(r, "def missing"); return; }
            if (index < 0 || index >= d.stages.Count) { Finish(r, StoryQuestStatus.Completed); return; }
            r.stage = index;
            r.stageKey = d.stages[index].Key(index);
            r.gen++;
            r.stageUntilTick = -1;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.chosenOutcome = null;
            r.vars.Clear();
            StoryStage st = d.stages[index];
            lastMechanic = st.Mechanic;
            st.Start(Context(r));
        }

        /// One stage tick with a single transition point: the outcome is recorded before any effect runs,
        /// each effect is isolated, so a failing effect can never make earlier effects run twice.
        private void TickRun(StoryQuestRun r)
        {
            StoryQuestDef d = r.Def;
            if (d == null || r.stage < 0 || r.stage >= d.stages.Count) { Disable(r, "stage missing"); return; }
            StoryStage st = d.stages[r.stage];
            StoryContext ctx = Context(r);

            string o = null;
            string forcedFromVars;
            if (r.vars.TryGetValue("forceOutcome", out forcedFromVars))
            {
                r.vars.Remove("forceOutcome");
                if (IsStoryTestMode && !forcedFromVars.NullOrEmpty())
                {
                    o = forcedFromVars;
                }
            }

            string transientForced;
            if (o == null && transientForcedOutcomes != null && transientForcedOutcomes.TryGetValue(r.quest, out transientForced))
            {
                transientForcedOutcomes.Remove(r.quest);
                if (IsStoryTestMode && !transientForced.NullOrEmpty())
                {
                    o = transientForced;
                }
            }

            if (o == null)
            {
                o = st.Tick(ctx);
            }

            if (o == null) return;
            int from = r.stage;
            r.outcome = o;
            r.history.Add(from + ":" + o);
            r.stage = -1; // transition boundary: nothing re-enters this stage
            StoryRelationRestorer.CheckOrphansIfPresent(); // ensure stage leases/protections are cleaned up even on forced outcomes
            foreach (StoryEffect e in st.EffectsFor(o).ToList()) ApplySafe(e, ctx);
            if (o == "failed") { Finish(r, StoryQuestStatus.Failed); return; }
            int next = st.NextFor(o);
            StartStage(r, next == -2 ? from + 1 : next);
        }

        private void SafeTick(StoryQuestRun r)
        {
            try { TickRun(r); }
            catch (Exception ex)
            {
                Log.ErrorOnce("[TMM] Story: quest " + r.quest + " failed in stage " + r.stage + ": " + ex, ("TMMStory" + r.quest).GetHashCode());
                Disable(r, "exception");
            }
        }

        private static void ApplySafe(StoryEffect e, StoryContext ctx)
        {
            if (e == null) return;
            try { e.Apply(ctx); }
            catch (Exception ex) { Log.Error("[TMM] Story: effect " + e.GetType().Name + " in " + ctx.run.quest + " failed: " + ex); }
        }

        private void Finish(StoryQuestRun r, StoryQuestStatus status)
        {
            StoryQuestDef d = r.Def;
            r.status = status;
            r.stage = -1;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.finishedTick = Find.TickManager.TicksGame;
            StoryRelationRestorer.CheckOrphansIfPresent();
            SetFlag("q." + r.quest, r.outcome ?? (status == StoryQuestStatus.Failed ? "failed" : "done"));
            if (d != null && status == StoryQuestStatus.Completed)
                foreach (StoryEffect e in d.onComplete) ApplySafe(e, Context(r));
        }

        /// A quest whose Def or stage disappeared (mod update): keep history and flags, give no rewards, free all limits.
        private void Disable(StoryQuestRun r, string why)
        {
            if (r.status == StoryQuestStatus.Disabled) return;
            r.status = StoryQuestStatus.Disabled;
            r.stage = -1;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.gen++;
            r.finishedTick = Find.TickManager.TicksGame;
            r.history.Add("disabled:" + why);
            StoryRelationRestorer.CheckOrphansIfPresent();
        }

        /// Once after load: drop runs of removed Defs, remap stages by stable key, resend exactly one letter
        /// for a valid pending offer/choice that has no live letter.
        private void Reconcile()
        {
            reconciled = true;
            var live = Find.LetterStack == null ? new List<StoryChoiceLetter>() : Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().ToList();
            foreach (StoryQuestRun r in runs.ToList())
            {
                if (r.status != StoryQuestStatus.Offered && r.status != StoryQuestStatus.Active) continue;
                StoryQuestDef d = r.Def;
                if (d == null) { Disable(r, "def missing"); continue; }
                if (r.status == StoryQuestStatus.Active)
                {
                    int byKey = r.stageKey.NullOrEmpty() ? r.stage : d.stages.FindIndex(s => s.Key(d.stages.IndexOf(s)) == r.stageKey);
                    if (byKey < 0) { Disable(r, "stage missing"); continue; }
                    r.stage = byKey;
                    if (r.pendingChoice && !live.Any(l => l.storyQuest == r.quest && l.gen == r.gen)) r.needsLetter = true;
                }
                else if (!live.Any(l => l.storyQuest == r.quest && l.gen == r.gen)) r.needsLetter = true;
            }
            StoryRelationRestorer.CheckOrphansIfPresent();
        }

        private void ResendLostLetters()
        {
            foreach (StoryQuestRun r in runs.Where(x => x.needsLetter).ToList())
            {
                r.needsLetter = false;
                if (r.Def == null) continue;
                if (r.status == StoryQuestStatus.Offered) StoryChoiceLetter.Send(Context(r), -1, StoryChoiceLetter.KindOffer);
                else if (r.status == StoryQuestStatus.Active && r.pendingChoice) StoryChoiceLetter.Send(Context(r), r.stage, StoryChoiceLetter.KindChoice);
            }
        }

        // ------------------------------------------------ test hook
        /// Test-only: finish the current stage of an Active quest with a declared outcome, through the normal transition.
        /// Refused unless the game runs with -tmm-story-test and the generation matches. Kept outside Scribe vars.
        public bool TestForceOutcome(StoryQuestRun r, int expectedGen, string outcome)
        {
            if (!IsStoryTestMode) return false;
            if (r == null || r.status != StoryQuestStatus.Active || r.gen != expectedGen || outcome.NullOrEmpty()) return false;
            if (transientForcedOutcomes == null) transientForcedOutcomes = new Dictionary<string, string>();
            transientForcedOutcomes[r.quest] = outcome;
            SafeTick(r);
            return true;
        }

        // ------------------------------------------------ director
        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % TickInterval != 0) return;
            DirectorTick();
        }

        public void DirectorTick()
        {
            if (!reconciled) Reconcile();
            if (GetFlag("act").NullOrEmpty()) SetFlag("act", "1");
            foreach (StoryQuestRun r in runs.Where(x => x.status == StoryQuestStatus.Active).ToList()) SafeTick(r);
            ResendLostLetters();
            if (Find.AnyPlayerHomeMap == null) return; // new quests need a home; active ones keep running

            foreach (StoryQuestDef d in DefDatabase<StoryQuestDef>.AllDefs.Where(x => x.autoStart).OrderBy(x => x.act).ThenByDescending(x => x.priority).ToList())
            {
                if (!IsAvailable(d)) continue;
                if (!d.main && NonMainBusy() >= MaxActive) continue;
                StartQuestDirect(d);
            }

            if (Find.TickManager.TicksGame - lastOfferTick < MinOfferGapTicks) return;
            bool mainOfferOpen = runs.Any(x => x.status == StoryQuestStatus.Offered && x.Def != null && x.Def.main);
            bool factionOfferOpen = runs.Any(x => x.status == StoryQuestStatus.Offered && x.Def != null && !x.Def.main);
            int busy = NonMainBusy();
            StoryQuestDef pick = DefDatabase<StoryQuestDef>.AllDefs
                .Where(x => !x.autoStart && IsAvailable(x) && (x.main ? !mainOfferOpen : !factionOfferOpen && busy < MaxActive))
                .OrderByDescending(x => x.FirstMechanic != lastMechanic)
                .ThenByDescending(x => x.main)
                .ThenByDescending(x => x.priority)
                .ThenBy(x => x.act)
                .ThenBy(x => x.defName)
                .FirstOrDefault();
            if (pick != null) Offer(pick);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref flags, "TMM_storyFlags", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref runs, "TMM_storyRuns", LookMode.Deep);
            Scribe_Values.Look(ref lastOfferTick, "lastOfferTick", -999999);
            Scribe_Values.Look(ref lastMechanic, "lastMechanic", "");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (flags == null) flags = new Dictionary<string, string>();
                if (runs == null) runs = new List<StoryQuestRun>();
                runs.RemoveAll(r => r == null || r.quest.NullOrEmpty());
                if (transientForcedOutcomes == null) transientForcedOutcomes = new Dictionary<string, string>();
                transientForcedOutcomes.Clear();
                reconciled = false;
            }
        }
    }
}
```

`Source/StoryLetter.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Letter used by the story engine for quest offers (Accept / Later) and for Choice stages.
    /// Each letter carries the run's generation: stale, archived or repeated clicks are refused by SparkStory,
    /// and only the current letter of a run may react to being dismissed.
    public class StoryChoiceLetter : ChoiceLetter
    {
        public const string KindOffer = "offer";
        public const string KindChoice = "choice";

        public string storyQuest;
        public int stage = -1;
        public int gen;
        public string kind = KindOffer;
        private bool resolved;

        public static void Send(StoryContext ctx, int stage, string kind)
        {
            StoryQuestDef d = ctx.def;
            if (d == null) return;
            string label, text;
            if (kind == KindOffer) { label = d.letterLabel.NullOrEmpty() ? d.label : d.letterLabel; text = d.letterText; }
            else { StoryStage st = d.stages[stage]; label = st.label.NullOrEmpty() ? d.label : st.label; text = st.text; }
            LetterDef ld = DefDatabase<LetterDef>.GetNamed("TMM_StoryChoice");
            var let = (StoryChoiceLetter)LetterMaker.MakeLetter(StoryUtil.Format(label, ctx), StoryUtil.Format(text, ctx), ld);
            let.storyQuest = d.defName;
            let.stage = stage;
            let.gen = ctx.run.gen;
            let.kind = kind;
            Find.LetterStack.ReceiveLetter(let);
        }

        private bool InStack { get { return Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(this); } }

        /// Still the letter the run is waiting for?
        private bool Current(StoryQuestRun run)
        {
            if (resolved || run == null || run.gen != gen || !InStack) return false;
            return kind == KindOffer ? run.status == StoryQuestStatus.Offered
                                     : run.status == StoryQuestStatus.Active && run.stage == stage && run.pendingChoice;
        }

        /// Closes only the dialog: an archived or outdated letter must never touch the letter stack or the quest.
        private static DiaOption CloseOnly()
        {
            var close = new DiaOption("Close".Translate());
            close.resolveTree = true;
            return close;
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                SparkStory s = SparkStory.Instance;
                StoryQuestRun run = s == null ? null : s.RunFor(storyQuest);
                StoryQuestDef def = run == null ? null : run.Def;
                if (def == null || !Current(run)) { yield return CloseOnly(); yield break; }

                if (kind == KindOffer)
                {
                    var accept = new DiaOption("TMM.Story.Accept".Translate());
                    accept.action = delegate { if (Current(run)) { Resolve(); s.Accept(run, gen); } };
                    accept.resolveTree = true;
                    yield return accept;
                    var later = new DiaOption("TMM.Story.Later".Translate());
                    later.action = delegate { if (Current(run)) { Resolve(); s.Decline(run, gen); } };
                    later.resolveTree = true;
                    yield return later;
                    yield break;
                }

                var stageDef = stage >= 0 && stage < def.stages.Count ? def.stages[stage] as StoryStage_Choice : null;
                if (stageDef == null) { yield return CloseOnly(); yield break; }
                StoryContext ctx = s.Context(run);
                foreach (StoryChoice c in stageDef.choices)
                {
                    StoryChoice choice = c;
                    var opt = new DiaOption(StoryUtil.Format(choice.label, ctx));
                    opt.action = delegate
                    {
                        if (!Current(run)) return;
                        // Re-check requires/anyOf BEFORE Resolve() so a stale option never consumes the letter
                        if (!StoryConditions.Holds(s, choice.requires, choice.anyOf)) return;
                        int letterGen = gen;
                        Resolve();
                        // If Choose refuses for any reason, request a resend so the player is never left without a letter
                        if (s.Choose(run, stage, letterGen, choice.outcome))
                        {
                            if (!choice.resultText.NullOrEmpty())
                                Find.LetterStack.ReceiveLetter(def.LabelCap.ToString(), StoryUtil.Format(choice.resultText, ctx), LetterDefOf.NeutralEvent);
                        }
                        else
                        {
                            s.Notify_LetterLost(run, kind, letterGen);
                        }
                    };
                    opt.resolveTree = true;
                    if (!StoryConditions.Holds(s, choice.requires, choice.anyOf)) opt.Disable("TMM.Story.ChoiceLocked".Translate());
                    yield return opt;
                }
            }
        }

        private void Resolve()
        {
            resolved = true; // before RemoveLetter: it calls Removed() synchronously
            if (InStack) Find.LetterStack.RemoveLetter(this);
        }

        /// Dismissed without an answer. Handled once and only for the current letter of the run.
        public override void Removed()
        {
            base.Removed();
            if (resolved) return;
            resolved = true;
            SparkStory s = SparkStory.Instance;
            StoryQuestRun run = s == null ? null : s.RunFor(storyQuest);
            if (run != null) s.Notify_LetterLost(run, kind, gen);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref storyQuest, "storyQuest");
            Scribe_Values.Look(ref stage, "stage", -1);
            Scribe_Values.Look(ref gen, "gen", 0);
            Scribe_Values.Look(ref kind, "kind", KindOffer);
            Scribe_Values.Look(ref resolved, "resolved", false);
        }
    }
}
```

`Source/StoryDefs.ConfigErrors.txt`

```csharp
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors()) yield return e;

            if (stages == null || stages.Count == 0)
            {
                yield return "StoryQuestDef " + defName + " has no stages.";
            }

            foreach (string err in ValidateConditionList(requires, "quest.requires")) yield return err;
            foreach (string err in ValidateConditionList(anyOf, "quest.anyOf")) yield return err;

            if (stages != null)
            {
                for (int i = 0; i < stages.Count; i++)
                {
                    StoryStage st = stages[i];
                    if (st == null)
                    {
                        yield return "StoryQuestDef " + defName + " has null stage at index " + i + ".";
                        continue;
                    }

                    StoryStage_Choice choiceStage = st as StoryStage_Choice;
                    if (choiceStage != null && choiceStage.choices != null)
                    {
                        for (int c = 0; c < choiceStage.choices.Count; c++)
                        {
                            StoryChoice ch = choiceStage.choices[c];
                            if (ch == null) continue;
                            string prefix = string.Format("stage[{0}].choices[{1}]", i, c);
                            foreach (string err in ValidateConditionList(ch.requires, prefix + ".requires")) yield return err;
                            foreach (string err in ValidateConditionList(ch.anyOf, prefix + ".anyOf")) yield return err;
                        }
                    }

                    // Validate outcomes (List<StoryChoice>) on Site, ColonyRaid, HotelVisit, Deliver, etc.
                    System.Reflection.FieldInfo outcomesField = st.GetType().GetField("outcomes");
                    if (outcomesField != null)
                    {
                        List<StoryChoice> outs = outcomesField.GetValue(st) as List<StoryChoice>;
                        if (outs != null)
                        {
                            for (int o = 0; o < outs.Count; o++)
                            {
                                StoryChoice ch = outs[o];
                                if (ch == null) continue;
                                string prefix = string.Format("stage[{0}].outcomes[{1}]", i, o);
                                foreach (string err in ValidateConditionList(ch.requires, prefix + ".requires")) yield return err;
                                foreach (string err in ValidateConditionList(ch.anyOf, prefix + ".anyOf")) yield return err;
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> ValidateConditionList(List<string> list, string where)
        {
            if (list == null) yield break;
            for (int i = 0; i < list.Count; i++)
            {
                string raw = list[i];
                string err = CheckConditionSyntax(raw);
                if (!err.NullOrEmpty())
                {
                    yield return string.Format("{0}[{1}] ('{2}'): {3}", where, i, raw ?? "null", err);
                }
            }
        }

        private static string CheckConditionSyntax(string raw)
        {
            if (raw.NullOrEmpty() || raw.Trim().Length == 0) return "empty condition";
            string s = raw.Trim();
            if (s[0] == '!') s = s.Substring(1).Trim();
            if (s.Length == 0) return "empty negated condition";

            if (s.StartsWith("done:", StringComparison.Ordinal))
            {
                return s.Length > 5 ? null : "missing quest defName after done:";
            }
            if (s.StartsWith("heroAlive:", StringComparison.Ordinal))
            {
                return s.Length > 10 ? null : "missing HeroDef defName after heroAlive:";
            }

            string[] numOps = new string[] { ">=", "<=", ">", "<" };
            for (int i = 0; i < numOps.Length; i++)
            {
                int idx = s.IndexOf(numOps[i], StringComparison.Ordinal);
                if (idx >= 0)
                {
                    string lhs = s.Substring(0, idx).Trim();
                    string rhs = s.Substring(idx + numOps[i].Length).Trim();
                    int parsed;
                    if (lhs.Length == 0) return "missing flag name before " + numOps[i];
                    if (!int.TryParse(rhs, out parsed)) return "non-integer value after " + numOps[i];
                    return null;
                }
            }

            int eq = s.IndexOf('=');
            if (eq >= 0)
            {
                string lhs = s.Substring(0, eq).Trim();
                if (lhs.Length == 0) return "missing flag name before =";
                return null;
            }

            // Bare boolean flag token: reject whitespace
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsWhiteSpace(s[i])) return "unexpected whitespace in condition";
            }
            return null;
        }
```

`Tests/InGameStoryStagesProbe.cs`

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

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class StoryStagesProbeBootstrap
    {
        public const string SaveName = "TMM-StoryStages";

        // Runtime probe quest defNames (registered only during probe runs so reload resolves cleanly)
        public const string QuestDownedTimeout = "TMM_Probe_Site_DownedTimeout";
        public const string QuestKillRecruited = "TMM_Probe_Site_KillRecruited";
        public const string QuestRetrieveDestroyed = "TMM_Probe_Site_RetrieveDestroyed";
        public const string QuestRaidNeutralTwoColonies = "TMM_Probe_Raid_NeutralTwoColonies";
        public const string QuestRaidDisabledRollback = "TMM_Probe_Raid_DisabledRollback";
        public const string QuestReloadRescue = "TMM_Probe_Reload_Rescue";
        public const string QuestReloadRetrieve = "TMM_Probe_Reload_Retrieve";
        public const string QuestReloadHold = "TMM_Probe_Reload_Hold";

        public static bool FreshMode
        {
            get { return HasArg("-tmm-story-stages-probe"); }
        }

        public static bool ReloadMode
        {
            get { return HasArg("-tmm-story-stages-reload"); }
        }

        static StoryStagesProbeBootstrap()
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
            HeroDef heroA = PickAvailableHero(0);
            HeroDef heroB = PickAvailableHero(1) ?? heroA;

            // 1. Timeout with downed colonist on map
            AddQuestDef(BuildSiteQuest(QuestDownedTimeout, "probe.downedTimeout", StorySiteObjective.Clear, null, null, 0.2f, 4f));

            // 2. KillHero for hero already recruited by player
            AddQuestDef(BuildSiteQuest(QuestKillRecruited, "probe.killRecruited", StorySiteObjective.KillHero, heroA, null, 5f, 4f));

            // 3. Retrieve where unique item is destroyed (no substitution)
            AddQuestDef(BuildSiteQuest(QuestRetrieveDestroyed, "probe.retrieveDestroyed", StorySiteObjective.Retrieve, null, ThingDefOf.ComponentIndustrial, 5f, 4f));

            // 4. Neutral raid with -50 goodwill + second home map
            AddQuestDef(BuildRaidQuest(QuestRaidNeutralTwoColonies, "probe.raidNeutral", true));

            // 5. Neutral raid disabled mid-raid -> rollback via StoryRelationRestorer
            AddQuestDef(BuildRaidQuest(QuestRaidDisabledRollback, "probe.raidDisable", true));

            // 6. Reload Rescue
            AddQuestDef(BuildSiteQuest(QuestReloadRescue, "probe.reloadRescue", StorySiteObjective.Rescue, heroB, null, 10f, 4f));

            // 7. Reload Retrieve
            AddQuestDef(BuildSiteQuest(QuestReloadRetrieve, "probe.reloadRetrieve", StorySiteObjective.Retrieve, null, ThingDefOf.ComponentIndustrial, 10f, 4f));

            // 8. Reload Hold (4h)
            AddQuestDef(BuildSiteQuest(QuestReloadHold, "probe.reloadHold", StorySiteObjective.Hold, null, null, 10f, 4f));
        }

        private static HeroDef PickAvailableHero(int index)
        {
            List<HeroDef> all = DefDatabase<HeroDef>.AllDefsListForReading;
            if (all == null || all.Count == 0) return null;
            return all[index % all.Count];
        }

        private static StoryQuestDef BuildSiteQuest(string defName, string flag, StorySiteObjective obj, HeroDef h, ThingDef t, float timeoutDays, float holdHours)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_Site st = new StoryStage_Site();
            st.key = "site";
            st.label = defName;
            st.text = defName;
            st.minDays = 2f;
            st.maxDays = 5f;
            st.objective = obj;
            st.hero = h;
            st.thing = t;
            st.timeoutDays = timeoutDays;
            st.holdHours = holdHours;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static StoryQuestDef BuildRaidQuest(string defName, string flag, bool restore)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_ColonyRaid st = new StoryStage_ColonyRaid();
            st.key = "raid";
            st.label = defName;
            st.text = defName;
            st.pointsFactor = 0.7f;
            st.delayHours = 1f;
            st.timeoutDays = 3f;
            st.restoreRelations = restore;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static void AddQuestDef(StoryQuestDef d)
        {
            if (d == null) return;
            if (DefDatabase<StoryQuestDef>.GetNamedSilentFail(d.defName) != null) return;
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
    /// Comprehensive in-game verification probe covering every Codex review finding (P1 &amp; P2)
    /// across fresh and save/reload runs. Reports honest pass/fail (never hardcoded true).
    /// </summary>
    public sealed class InGameStoryStagesProbe : MapComponent
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

        public InGameStoryStagesProbe(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (s_completed) return;
            if (!StoryStagesProbeBootstrap.FreshMode && !StoryStagesProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                if (StoryStagesProbeBootstrap.FreshMode)
                {
                    RunFreshProbe();
                }
                else if (StoryStagesProbeBootstrap.ReloadMode)
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
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(StoryStagesProbeBootstrap.ReloadMode ? "StoryStagesReloadProbe.json" : "StoryStagesProbe.json");
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
        // FRESH PROBE: TESTS EVERY CODEX FINDING + SAVES RELOAD SNAPSHOT
        // =====================================================================

        private void RunFreshProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("SparkStory_Exists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. TIMEOUT WITH DOWNED COLONIST ON SITE MAP ----------------
            story.SetFlag("probe.downedTimeout", "true");
            story.DirectorTick();
            StoryQuestRun downedRun = story.RunFor(StoryStagesProbeBootstrap.QuestDownedTimeout);
            Site downedSite = FindSite(ParseInt(downedRun, "siteId", -1));
            if (downedSite != null)
            {
                Pawn scout = SpawnColonist(map);
                Caravan car = EnterSiteWithScout(scout, downedSite);
                if (downedSite.HasMap && downedSite.Map != null)
                {
                    // Down the colonist via Anesthetic so they are alive & downed on site map
                    scout.health.AddHediff(HediffDefOf.Anesthetic);
                }
                AdvanceTicks(Mathf.CeilToInt(0.3f * GenDate.TicksPerDay));
                story.DirectorTick();

                bool timedOut = downedRun != null && HasOutcome(downedRun, "timeout");
                bool mapPreserved = downedSite.HasMap && downedSite.Map != null && !downedSite.Destroyed;
                bool pawnAliveOnMap = scout != null && !scout.Dead && scout.Downed && scout.Spawned && scout.Map == downedSite.Map;
                AddCheck("P1_DownedColonistTimeout_MapAndPawnPreserved",
                    timedOut && mapPreserved && pawnAliveOnMap,
                    "timeout=true, mapPreserved=true, pawnAliveDowned=true",
                    string.Format("timeout={0}, map={1}, pawn={2}", timedOut, mapPreserved, pawnAliveOnMap));

                // Clean up test map/scout after verification
                CleanupScoutAndSite(scout, car, downedSite);
            }
            else
            {
                AddCheck("P1_DownedColonistTimeout_MapAndPawnPreserved", false, "site created", "site null");
            }

            // ---------------- 2. KILLHERO FOR HERO ALREADY RECRUITED BY PLAYER ----------------
            StoryQuestDef killRecDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestKillRecruited);
            StoryStage_Site killRecStage = killRecDef != null ? killRecDef.stages[0] as StoryStage_Site : null;
            Pawn recruitedHero = (killRecStage != null && killRecStage.hero != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(killRecStage.hero) : null;
            if (recruitedHero != null)
            {
                if (recruitedHero.Spawned) recruitedHero.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns.Contains(recruitedHero)) Find.WorldPawns.RemovePawn(recruitedHero);
                recruitedHero.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(recruitedHero, FindStandable(map), map, WipeMode.Vanish);

                story.SetFlag("probe.killRecruited", "true");
                story.DirectorTick();
                StoryQuestRun krRun = story.RunFor(StoryStagesProbeBootstrap.QuestKillRecruited);

                bool unavailable = krRun != null && HasOutcome(krRun, "unavailable");
                bool heroUntouched = !recruitedHero.Dead && recruitedHero.Faction == Faction.OfPlayer &&
                                     recruitedHero.Spawned && recruitedHero.Map == map;
                AddCheck("P1_KillHeroRecruited_UnavailableAndUntouched",
                    unavailable && heroUntouched,
                    "unavailable=true, heroUntouched=true",
                    string.Format("unavailable={0}, untouched={1}", unavailable, heroUntouched));
            }
            else
            {
                AddCheck("P1_KillHeroRecruited_UnavailableAndUntouched", false, "hero exists", "hero null");
            }

            // ---------------- 3. RETRIEVE DESTROYED ITEM -> FAILED (NO SUBSTITUTION) ----------------
            story.SetFlag("probe.retrieveDestroyed", "true");
            story.DirectorTick();
            StoryQuestRun retDestRun = story.RunFor(StoryStagesProbeBootstrap.QuestRetrieveDestroyed);
            Site retDestSite = FindSite(ParseInt(retDestRun, "siteId", -1));
            if (retDestSite != null)
            {
                Pawn scout = SpawnColonist(map);
                Caravan car = EnterSiteWithScout(scout, retDestSite);
                story.DirectorTick();

                string realId = GetVar(retDestRun, "targetThingId");
                Thing realThing = FindThingOnMapById(retDestSite.Map, realId);

                // Put a DECOY ComponentIndustrial into a separate player caravan on the world map
                Pawn decoyPawn = SpawnColonist(map);
                Thing decoyItem = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                decoyPawn.inventory.innerContainer.TryAdd(decoyItem);
                decoyPawn.DeSpawn(DestroyMode.Vanish);
                Caravan decoyCaravan = CaravanMaker.MakeCaravan(new List<Pawn> { decoyPawn }, Faction.OfPlayer, map.Tile, true); // CHECK API

                // Destroy the REAL quest item
                if (realThing != null && !realThing.Destroyed) realThing.Destroy(DestroyMode.Vanish);
                story.DirectorTick();

                bool gotFailed = retDestRun != null && HasOutcome(retDestRun, "failed") && !HasOutcome(retDestRun, "success");
                AddCheck("P1_RetrieveDestroyed_FailedNoSubstitution",
                    realThing != null && gotFailed,
                    "realFound=true, outcome=failed (not substituted by decoy)",
                    string.Format("realFound={0}, history={1}", realThing != null, retDestRun != null ? string.Join(",", retDestRun.history.ToArray()) : "null"));

                decoyCaravan.Destroy();
                CleanupScoutAndSite(scout, car, retDestSite);
            }
            else
            {
                AddCheck("P1_RetrieveDestroyed_FailedNoSubstitution", false, "site created", "site null");
            }

            // ---------------- 4. RAID: NEUTRAL FACTION (-50) + SECOND HOME MAP ----------------
            Faction neutralFac = FindNeutralHumanlikeFaction();
            StoryQuestDef raidNeutralDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidNeutralTwoColonies);
            StoryStage_ColonyRaid raidNeutralStage = raidNeutralDef != null ? raidNeutralDef.stages[0] as StoryStage_ColonyRaid : null;
            if (neutralFac != null && raidNeutralStage != null)
            {
                // Set exact Neutral goodwill -50 (inside RimWorld's -74..-1 hysteresis zone!)
                ForceNeutralWithGoodwill(neutralFac, -50);
                raidNeutralStage.faction = neutralFac.def.defName;

                story.SetFlag("probe.raidNeutral", "true");
                story.DirectorTick();
                StoryQuestRun raidRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidNeutralTwoColonies);

                // Spawn a SECOND player home map before raid resolves to ensure pinned raidMapId isn't confused
                Settlement secondHomeObj = null;
                Map secondHomeMap = CreateTemporarySecondHomeMap(out secondHomeObj);

                AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                story.DirectorTick();

                bool hostileDuringRaid = neutralFac.HostileTo(Faction.OfPlayer);
                int pinnedMapId = ParseInt(raidRun, "raidMapId", -1);
                List<int> raidPawnIds = ParseIntList(GetVar(raidRun, "raidPawnIds"));

                // Verify ticking while raiders are alive on map 1 does NOT falsely succeed due to secondHomeMap
                story.DirectorTick();
                bool stillActiveWithTwoHomes = raidRun != null && raidRun.status == StoryQuestStatus.Active && pinnedMapId == map.uniqueID;

                // Kill ONLY the tracked raid pawns on map 1
                int killed = KillTrackedPawns(map, raidPawnIds);
                story.DirectorTick();

                bool raidSucceeded = raidRun != null && HasOutcome(raidRun, "success");
                bool restoredNonHostile = !neutralFac.HostileTo(Faction.OfPlayer);
                bool restoredGoodwill = neutralFac.PlayerGoodwill >= -55 && neutralFac.PlayerGoodwill <= -45;

                AddCheck("P1_NeutralRaid_TwoHomesAndGoodwillRestored",
                    hostileDuringRaid && stillActiveWithTwoHomes && killed > 0 && raidSucceeded && restoredNonHostile && restoredGoodwill,
                    "hostile->success->Neutral(~-50) with 2 home maps",
                    string.Format("hostile={0}, stillActive={1}, killed={2}, succ={3}, nonHostile={4}, gw={5}",
                        hostileDuringRaid, stillActiveWithTwoHomes, killed, raidSucceeded, restoredNonHostile, neutralFac.PlayerGoodwill));

                CleanupSecondHomeMap(secondHomeMap, secondHomeObj);
            }
            else
            {
                AddCheck("P1_NeutralRaid_TwoHomesAndGoodwillRestored", false, "neutral faction & def exist", "missing");
            }

            // ---------------- 5. DISABLED QUEST MID-RAID -> RELATION ROLLBACK ----------------
            StoryQuestDef raidDisableDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidDisabledRollback);
            StoryStage_ColonyRaid raidDisableStage = raidDisableDef != null ? raidDisableDef.stages[0] as StoryStage_ColonyRaid : null;
            if (neutralFac != null && raidDisableStage != null)
            {
                ForceNeutralWithGoodwill(neutralFac, -40);
                raidDisableStage.faction = neutralFac.def.defName;

                story.SetFlag("probe.raidDisable", "true");
                story.DirectorTick();
                StoryQuestRun disRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidDisabledRollback);

                AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                story.DirectorTick();
                bool becameHostile = neutralFac.HostileTo(Faction.OfPlayer);

                // Simulate quest disabled mid-raid (e.g. removed Def/stage or exception)
                List<int> disPawnIds = ParseIntList(GetVar(disRun, "raidPawnIds"));
                disRun.status = StoryQuestStatus.Disabled;
                disRun.stage = -1;
                StoryRelationRestorer.CheckOrphansIfPresent();

                bool rolledBack = !neutralFac.HostileTo(Faction.OfPlayer) && neutralFac.PlayerGoodwill >= -45 && neutralFac.PlayerGoodwill <= -35;
                AddCheck("P1_DisabledQuestRaid_RelationsRolledBack",
                    becameHostile && rolledBack,
                    "hostile->rolledBack Neutral(~-40)",
                    string.Format("hostile={0}, nonHostile={1}, gw={2}", becameHostile, !neutralFac.HostileTo(Faction.OfPlayer), neutralFac.PlayerGoodwill));

                KillTrackedPawns(map, disPawnIds);
            }
            else
            {
                AddCheck("P1_DisabledQuestRaid_RelationsRolledBack", false, "neutral faction & def exist", "missing");
            }

            // ---------------- 6. FORCEOUTCOME IGNORED WITHOUT -tmm-story-test ----------------
            if (!Environment.GetCommandLineArgs().Contains("-tmm-story-test"))
            {
                // Verify SparkStory refuses TestForceOutcome when -tmm-story-test is absent
                bool testRefused = !story.TestForceOutcome(downedRun, downedRun != null ? downedRun.gen : 0, "success");
                AddCheck("P1_ForceOutcomeRefusedWithoutTestFlag", testRefused, "false", (!testRefused).ToString());
            }

            // ---------------- 7. PREPARE RELOAD SAVE: RESCUE + RETRIEVE + PARTIAL HOLD ----------------
            // 7A. Rescue with captive hero on site map
            story.SetFlag("probe.reloadRescue", "true");
            story.DirectorTick();
            StoryQuestRun rescueRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRescue);
            Site rescueSite = FindSite(ParseInt(rescueRun, "siteId", -1));
            bool rescueReady = false;
            if (rescueSite != null)
            {
                Pawn rScout = SpawnColonist(map);
                EnterSiteWithScout(rScout, rescueSite);
                story.DirectorTick();
                rescueReady = GetVar(rescueRun, "heroImprisonedOnSite") == "true" && !GetVar(rescueRun, "targetPawnId").NullOrEmpty();
            }
            AddCheck("ReloadPrep_RescueCaptiveOnSite", rescueReady, "imprisoned=true", rescueReady.ToString());

            // 7B. Retrieve with unique protected item on site map
            story.SetFlag("probe.reloadRetrieve", "true");
            story.DirectorTick();
            StoryQuestRun retRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRetrieve);
            Site retSite = FindSite(ParseInt(retRun, "siteId", -1));
            bool retrieveReady = false;
            if (retSite != null)
            {
                Pawn iScout = SpawnColonist(map);
                EnterSiteWithScout(iScout, retSite);
                story.DirectorTick();
                string tid = GetVar(retRun, "targetThingId");
                Thing spawnedItem = FindThingOnMapById(retSite.Map, tid);
                Thing otherStack = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                bool cannotMerge = spawnedItem != null && !spawnedItem.CanStackWith(otherStack);
                otherStack.Destroy(DestroyMode.Vanish);
                retrieveReady = spawnedItem != null && cannotMerge;
            }
            AddCheck("ReloadPrep_RetrieveUniqueItemProtected", retrieveReady, "item spawned & non-stackable", retrieveReady.ToString());

            // 7C. Hold with partial confirmed progress (first poll adds 0, second poll adds 2h of 4h)
            story.SetFlag("probe.reloadHold", "true");
            story.DirectorTick();
            StoryQuestRun holdRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadHold);
            Site holdSite = FindSite(ParseInt(holdRun, "siteId", -1));
            bool holdPrepOk = false;
            if (holdSite != null)
            {
                // Advance 3 hours BEFORE entering -> first poll after entry MUST add 0 ticks!
                AdvanceTicks(3 * GenDate.TicksPerHour);
                Pawn hScout = SpawnColonist(map);
                EnterSiteWithScout(hScout, holdSite);
                story.DirectorTick();
                int heldAfterEntry = ParseInt(holdRun, "heldTicks", -1);

                // Now advance 2 confirmed hours with standing colonist
                AdvanceTicks(2 * GenDate.TicksPerHour);
                story.DirectorTick();
                int heldAfter2h = ParseInt(holdRun, "heldTicks", -1);

                holdPrepOk = heldAfterEntry == 0 && heldAfter2h == 2 * GenDate.TicksPerHour && holdRun.status == StoryQuestStatus.Active;
                AddCheck("P2_Hold_FirstPollZeroAndPartialSaved", holdPrepOk,
                    "afterEntry=0, after2h=5000, active",
                    string.Format("entry={0}, after2h={1}, status={2}", heldAfterEntry, heldAfter2h, holdRun.status));
            }
            else
            {
                AddCheck("P2_Hold_FirstPollZeroAndPartialSaved", false, "site created", "site null");
            }

            GameDataSaveLoader.SaveGame(StoryStagesProbeBootstrap.SaveName); // CHECK API
        }

        // =====================================================================
        // RELOAD PROBE: COMPLETES RESCUE, RETRIEVE, AND PARTIAL HOLD ONCE EACH
        // =====================================================================

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. RESCUE RELOAD ----------------
            StoryQuestRun rescueRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRescue);
            Site rescueSite = FindSite(ParseInt(rescueRun, "siteId", -1));
            string captiveId = GetVar(rescueRun, "targetPawnId");
            if (rescueRun != null && rescueSite != null && rescueSite.HasMap && !captiveId.NullOrEmpty())
            {
                Pawn captive = FindPawnOnMapById(rescueSite.Map, captiveId);
                Pawn rescuer = FindFirstColonistOnMap(rescueSite.Map);
                bool captiveRestored = captive != null && captive.IsPrisoner;

                if (captive != null && rescuer != null)
                {
                    if (captive.guest != null) captive.guest.SetGuestStatus(null, GuestStatus.Guest);
                    captive.DeSpawn(DestroyMode.Vanish);
                    rescuer.DeSpawn(DestroyMode.Vanish);
                    List<Pawn> group = new List<Pawn> { rescuer, captive };
                    Caravan car = CaravanMaker.MakeCaravan(group, Faction.OfPlayer, map.Tile, true); // CHECK API
                    Current.Game.DeinitAndRemoveMap(rescueSite.Map, false); // CHECK API
                    story.DirectorTick();
                    story.DirectorTick();

                    bool once = CountOutcome(rescueRun, "success") == 1;
                    AddCheck("Reload_RescueCompletesOnce", captiveRestored && once,
                        "restored=true, successCount=1",
                        string.Format("restored={0}, count={1}", captiveRestored, CountOutcome(rescueRun, "success")));
                    car.Destroy();
                }
                else
                {
                    AddCheck("Reload_RescueCompletesOnce", false, "captive & rescuer on site", "missing");
                }
            }
            else
            {
                AddCheck("Reload_RescueCompletesOnce", false, "rescue run & site restored", "missing");
            }

            // ---------------- 2. RETRIEVE RELOAD ----------------
            StoryQuestRun retRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRetrieve);
            Site retSite = FindSite(ParseInt(retRun, "siteId", -1));
            string thingId = GetVar(retRun, "targetThingId");
            if (retRun != null && retSite != null && retSite.HasMap && !thingId.NullOrEmpty())
            {
                Thing uniqueItem = FindThingOnMapById(retSite.Map, thingId);
                Pawn carrier = FindFirstColonistOnMap(retSite.Map);
                Thing otherStack = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                bool stillProtected = uniqueItem != null && !uniqueItem.CanStackWith(otherStack);
                otherStack.Destroy(DestroyMode.Vanish);

                if (uniqueItem != null && carrier != null)
                {
                    uniqueItem.DeSpawn(DestroyMode.Vanish);
                    carrier.inventory.innerContainer.TryAdd(uniqueItem);
                    carrier.DeSpawn(DestroyMode.Vanish);
                    Caravan car = CaravanMaker.MakeCaravan(new List<Pawn> { carrier }, Faction.OfPlayer, map.Tile, true); // CHECK API
                    Current.Game.DeinitAndRemoveMap(retSite.Map, false); // CHECK API
                    story.DirectorTick();
                    story.DirectorTick();

                    bool once = CountOutcome(retRun, "success") == 1;
                    AddCheck("Reload_RetrieveCompletesOnce", stillProtected && once,
                        "protected=true, successCount=1",
                        string.Format("protected={0}, count={1}", stillProtected, CountOutcome(retRun, "success")));
                    car.Destroy();
                }
                else
                {
                    AddCheck("Reload_RetrieveCompletesOnce", false, "item & carrier on site", "missing");
                }
            }
            else
            {
                AddCheck("Reload_RetrieveCompletesOnce", false, "retrieve run & site restored", "missing");
            }

            // ---------------- 3. PARTIAL HOLD RELOAD ----------------
            StoryQuestRun holdRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadHold);
            Site holdSite = FindSite(ParseInt(holdRun, "siteId", -1));
            if (holdRun != null && holdSite != null && holdSite.HasMap)
            {
                int restoredTicks = ParseInt(holdRun, "heldTicks", 0);
                AdvanceTicks(2 * GenDate.TicksPerHour + 250);
                story.DirectorTick();
                story.DirectorTick();

                bool once = CountOutcome(holdRun, "success") == 1;
                AddCheck("Reload_PartialHoldCompletesOnce", restoredTicks >= 2 * GenDate.TicksPerHour && once,
                    "restored>=5000, successCount=1",
                    string.Format("restored={0}, count={1}", restoredTicks, CountOutcome(holdRun, "success")));
            }
            else
            {
                AddCheck("Reload_PartialHoldCompletesOnce", false, "hold run & site restored", "missing");
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static Faction FindNeutralHumanlikeFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HasGoodwill &&
                    f.def.humanlikeFaction && !f.def.permanentEnemy)
                {
                    return f;
                }
            }
            return null;
        }

        private static void ForceNeutralWithGoodwill(Faction fac, int goodwill)
        {
            if (fac == null) return;
            if (fac.HostileTo(Faction.OfPlayer))
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, 0 - fac.PlayerGoodwill, false, false, null, null);
            }
            int delta = goodwill - fac.PlayerGoodwill;
            if (delta != 0)
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null);
            }
            if (fac.HostileTo(Faction.OfPlayer))
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false, null, GlobalTargetInfo.Invalid);
            }
        }

        private Map CreateTemporarySecondHomeMap(out Settlement settlement)
        {
            settlement = null;
            PlanetTile tile;
            if (!TileFinder.TryFindNewSiteTile(out tile, map.Tile, 2, 10, false, null, 0f, false, TileFinderMode.Near, false, false, null, null)) // CHECK API
            {
                return null;
            }
            settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            settlement.Tile = tile;
            settlement.SetFaction(Faction.OfPlayer);
            settlement.Name = "ProbeSecondHome";
            Find.WorldObjects.Add(settlement);
            return GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(75, 1, 75), null); // CHECK API
        }

        private static void CleanupSecondHomeMap(Map m, Settlement s)
        {
            if (m != null && Find.Maps.Contains(m))
            {
                Current.Game.DeinitAndRemoveMap(m, false); // CHECK API
            }
            if (s != null && !s.Destroyed) s.Destroy();
        }

        private static Pawn SpawnColonist(Map targetMap)
        {
            Pawn p = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static Caravan EnterSiteWithScout(Pawn scout, Site site)
        {
            if (scout.Spawned) scout.DeSpawn(DestroyMode.Vanish);
            Caravan c = CaravanMaker.MakeCaravan(new List<Pawn> { scout }, Faction.OfPlayer, site.Tile, true); // CHECK API
            Map siteMap = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null); // CHECK API
            CaravanEnterMapUtility.Enter(c, siteMap, CaravanEnterMode.Edge); // CHECK API
            return c;
        }

        private static void CleanupScoutAndSite(Pawn scout, Caravan car, Site site)
        {
            if (scout != null && !scout.Destroyed)
            {
                if (scout.Spawned) scout.DeSpawn(DestroyMode.Vanish);
                scout.Destroy(DestroyMode.Vanish);
            }
            if (car != null && !car.Destroyed) car.Destroy();
            if (site != null && !site.Destroyed)
            {
                if (site.HasMap && site.Map != null)
                {
                    Current.Game.DeinitAndRemoveMap(site.Map, false); // CHECK API
                }
                if (!site.Destroyed) site.Destroy();
            }
        }

        private static int KillTrackedPawns(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return 0;
            HashSet<int> set = new HashSet<int>(ids);
            int killed = 0;
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && !p.Dead && set.Contains(p.thingIDNumber))
                {
                    p.Kill(null);
                    killed++;
                }
            }
            return killed;
        }

        private static Thing FindThingOnMapById(Map targetMap, string thingId)
        {
            if (targetMap == null || thingId.NullOrEmpty()) return null;
            List<Thing> all = targetMap.listerThings.AllThings;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].Destroyed && all[i].ThingID == thingId) return all[i];
            }
            return null;
        }

        private static Pawn FindPawnOnMapById(Map targetMap, string pawnId)
        {
            if (targetMap == null || pawnId.NullOrEmpty()) return null;
            IReadOnlyList<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].ThingID == pawnId) return pawns[i];
            }
            return null;
        }

        private static Pawn FindFirstColonistOnMap(Map targetMap)
        {
            if (targetMap == null || targetMap.mapPawns == null) return null;
            IReadOnlyList<Pawn> cols = targetMap.mapPawns.FreeColonistsSpawned;
            return cols.Count > 0 ? cols[0] : null;
        }

        private static Site FindSite(int siteId)
        {
            if (siteId < 0) return null;
            List<Site> sites = Find.WorldObjects.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i] != null && !sites[i].Destroyed && sites[i].ID == siteId) return sites[i];
            }
            return null;
        }

        private static IntVec3 FindStandable(Map targetMap)
        {
            IntVec3 cell;
            if (CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 10, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap);
            }, out cell))
            {
                return cell;
            }
            return targetMap.Center;
        }

        private static void AdvanceTicks(int delta)
        {
            if (delta > 0) Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + delta); // CHECK API
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