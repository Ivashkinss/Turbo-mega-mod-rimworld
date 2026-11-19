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
