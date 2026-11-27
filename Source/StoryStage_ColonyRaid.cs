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
    /// after delayHours.
    ///
    /// Round 2 invariants:
    /// - Success ONLY when a NON-EMPTY confirmed raider cohort was recorded at spawn time AND all
    ///   tracked raiders are dead, downed, captured, or fleeing/exited.
    /// - Empty raider cohort (even if a lord ID was stored and later vanished) ALWAYS resolves as
    ///   "timeout", NEVER "success".
    /// - Any failed raid spawn (TryExecute false, exception, or 0 confirmed raiders) rolls back
    ///   forced hostility via StoryRelationRestorer.AbortLease REGARDLESS of restoreRelations.
    /// - Test-only raid execution override is strictly gated behind TestMode.Enabled.
    /// </summary>
    public sealed class StoryStage_ColonyRaid : StoryStage
    {
        /// <summary>
        /// Test-only hook (active ONLY when TestMode.Enabled) allowing probes to simulate
        /// a failed raid spawn or zero-raider execution to verify AbortLease and non-success timeout.
        /// </summary>
        public static Func<IncidentParms, bool> TestRaidExecuteOverride;

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
            ctx.run.vars["confirmedRaiderCount"] = "0";
            ctx.run.vars["raidMapId"] = ctx.Home != null ? ctx.Home.uniqueID.ToString() : "-1";

            if (!text.NullOrEmpty() && delayHours > 0f)
            {
                string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "TMM.Story.Raid.Title".Translate().ToString()), ctx);
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

            int deadlineTick;
            if (!TryGetInt(ctx, "deadlineTick", out deadlineTick))
            {
                deadlineTick = spawnTick + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));
                ctx.run.vars["deadlineTick"] = deadlineTick.ToString();
            }

            if (!GetBool(ctx, "spawned"))
            {
                if (now >= deadlineTick)
                {
                    return FinishAborted(ctx, "timeout");
                }

                if (now < spawnTick) return null;

                Map targetMap = ResolveTargetHomeMap(ctx);
                if (targetMap == null)
                {
                    return null;
                }

                if (!SpawnRaid(ctx, targetMap))
                {
                    Log.Warning("[TMM] StoryStage_ColonyRaid: failed to spawn confirmed raid for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                    return FinishAborted(ctx, "timeout");
                }

                ctx.run.vars["spawned"] = "true";
                return null;
            }

            // Already spawned: check deadline first
            if (now >= deadlineTick)
            {
                return Finish(ctx, "timeout");
            }

            int raidMapId = GetInt(ctx, "raidMapId", -1);
            Map raidMap = FindMapByUniqueId(raidMapId);
            if (raidMap == null)
            {
                return Finish(ctx, "timeout");
            }

            HashSet<int> trackedSet = ParseIntSet(GetStr(ctx, "raidPawnIds"));
            int confirmedCount = GetInt(ctx, "confirmedRaiderCount", trackedSet.Count);
            int lordId = GetInt(ctx, "raidLordId", -1);

            // Empty confirmed raider cohort (even if a lord ID was stored & vanished) is ALWAYS timeout, NEVER success
            if (trackedSet.Count == 0 || confirmedCount <= 0)
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
                baseText = "TMM.Story.Raid.Repel".Translate();
            }

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            if (!GetBool(ctx, "spawned"))
            {
                int spawnTick = GetInt(ctx, "spawnTick", now);
                float hoursLeft = Mathf.Max(0f, (spawnTick - now) / (float)GenDate.TicksPerHour);
                return "TMM.Story.Raid.Arriving".Translate(baseText, hoursLeft.ToString("0.0"));
            }

            int deadline = GetInt(ctx, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);
            return "TMM.Story.Hotel.GoalTime".Translate(baseText, "TMM.Story.DaysLeft".Translate(daysLeft.ToString("0.0")));
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
                executed = ExecuteRaidIncident(parms);
                if (!executed)
                {
                    parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                    executed = ExecuteRaidIncident(parms);
                }
            }
            catch (Exception ex)
            {
                TestMode.ReportException("[TMM] StoryStage_ColonyRaid: TryExecute threw exception: ", ex);
                AbortHostilityLease(ctx);
                return false;
            }

            if (!executed)
            {
                // Failed spawn MUST roll back hostility regardless of restoreRelations
                AbortHostilityLease(ctx);
                return false;
            }

            // Collect COMPLETE confirmed raider cohort (new lord ownedPawns + newly spawned pawns + pawns in pods/holders)
            List<int> trackedIds = new List<int>();
            HashSet<int> trackedUnique = new HashSet<int>();
            int newLordId = -1;

            if (targetMap.lordManager != null)
            {
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
            }

            HashSet<int> afterPawns = SnapshotAllFactionPawnsOnMap(targetMap, raidFac);
            foreach (int id in afterPawns)
            {
                if (!prevPawns.Contains(id) && trackedUnique.Add(id))
                {
                    trackedIds.Add(id);
                }
            }

            // STRICT REQUIREMENT: must have a NON-EMPTY confirmed raider cohort!
            // Zero spawned raiders (even if a lord was created) fails spawn & rolls back hostility regardless of restoreRelations.
            if (trackedIds.Count == 0)
            {
                AbortHostilityLease(ctx);
                return false;
            }

            ctx.run.vars["raidLordId"] = newLordId.ToString();
            ctx.run.vars["raidPawnIds"] = JoinInts(trackedIds);
            ctx.run.vars["confirmedRaiderCount"] = trackedIds.Count.ToString();
            return true;
        }

        private static bool ExecuteRaidIncident(IncidentParms parms)
        {
            if (TestMode.Enabled && TestRaidExecuteOverride != null)
            {
                return TestRaidExecuteOverride(parms);
            }
            return IncidentDefOf.RaidEnemy.Worker.TryExecute(parms); // CHECK API
        }

        /// <summary>
        /// Single-pass batch check across the pinned raid map's spawned pawns, ThingHolders (pods/skyfallers),
        /// and the raid's Lord. Requires non-empty trackedSet; never falls back to empty-set success.
        /// </summary>
        private static bool IsRaidClearedSinglePass(Map raidMap, HashSet<int> trackedSet, int lordId)
        {
            if (raidMap == null || trackedSet == null || trackedSet.Count == 0) return false;

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
                    if (ThingHasActiveTrackedRaider(h, trackedSet, raidMap))
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

        private static bool ThingHasActiveTrackedRaider(Thing h, HashSet<int> trackedSet, Map raidMap)
        {
            if (h == null || h.Destroyed) return false;

            IThingHolder direct = h as IThingHolder;
            if (direct != null && HolderHasActiveTrackedRaider(direct, trackedSet, raidMap))
            {
                return true;
            }

            ThingWithComps twc = h as ThingWithComps;
            if (twc != null && twc.AllComps != null)
            {
                List<ThingComp> comps = twc.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    IThingHolder ch = comps[i] as IThingHolder;
                    if (ch != null && HolderHasActiveTrackedRaider(ch, trackedSet, raidMap))
                    {
                        return true;
                    }
                }
            }
            return false;
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
                    if (p != null && trackedSet.Contains(p.thingIDNumber) && !p.Dead && !p.Destroyed && !p.IsPrisonerOfColony && !p.IsSlaveOfColony)
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

        private string FinishAborted(StoryContext ctx, string outcome)
        {
            AbortHostilityLease(ctx);
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

        private static void AbortHostilityLease(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;
            string leaseId = GetStr(ctx, "relationLeaseId");
            if (!leaseId.NullOrEmpty() && StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.AbortLease(leaseId);
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
                    CollectFactionPawnsInThing(h, fac, ids);
                }
            }

            return ids;
        }

        private static void CollectFactionPawnsInThing(Thing h, Faction fac, HashSet<int> ids)
        {
            if (h == null || h.Destroyed) return;

            IThingHolder direct = h as IThingHolder;
            if (direct != null) CollectFactionPawnsInHolder(direct, fac, ids);

            ThingWithComps twc = h as ThingWithComps;
            if (twc != null && twc.AllComps != null)
            {
                List<ThingComp> comps = twc.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    IThingHolder ch = comps[i] as IThingHolder;
                    if (ch != null) CollectFactionPawnsInHolder(ch, fac, ids);
                }
            }
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
