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
    /// Quest stage that launches a vanilla IncidentDefOf.RaidEnemy raid against the player's home map
    /// after delayHours, temporarily turning non-hostile factions hostile if required, and completes
    /// when all raiders are dead, downed, captured, or have fled (or on timeoutDays).
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

            if (!text.NullOrEmpty() && delayHours > 0f)
            {
                string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Incoming Raid"), ctx);
                string body = StoryUtil.Format(text, ctx);
                Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.ThreatSmall, ctx.Home != null ? new LookTargets(ctx.Home.Center, ctx.Home) : LookTargets.Invalid); // CHECK API
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            int now = Find.TickManager.TicksGame;
            int spawnTick = GetInt(ctx, "spawnTick", now);
            int deadlineTick = GetInt(ctx, "deadlineTick", now + GenDate.TicksPerDay);
            Map home = ctx.Home;

            if (!GetBool(ctx, "spawned"))
            {
                if (now < spawnTick) return null;

                if (home == null)
                {
                    if (now >= deadlineTick)
                    {
                        return Finish(ctx, "timeout");
                    }
                    return null;
                }

                if (!SpawnRaid(ctx, home))
                {
                    Log.Warning("[TMM] StoryStage_ColonyRaid: failed to spawn raid for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                    return Finish(ctx, "timeout");
                }

                ctx.run.vars["spawned"] = "true";
                return null;
            }

            if (now >= deadlineTick)
            {
                return Finish(ctx, "timeout");
            }

            if (home == null)
            {
                return Finish(ctx, "timeout");
            }

            if (IsRaidCleared(ctx, home))
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

        private bool SpawnRaid(StoryContext ctx, Map home)
        {
            Faction raidFac = ResolveRaidFaction(ctx);
            if (raidFac == null) return false;

            ctx.run.vars["raidFactionId"] = raidFac.loadID.ToString();

            if (!raidFac.HostileTo(Faction.OfPlayer))
            {
                ctx.run.vars["wasNonHostile"] = "true";
                ctx.run.vars["origGoodwill"] = raidFac.PlayerGoodwill.ToString();
                ctx.run.vars["origKind"] = ((int)raidFac.RelationKindWith(Faction.OfPlayer)).ToString();
                ForceHostile(raidFac);
            }
            else
            {
                ctx.run.vars["wasNonHostile"] = "false";
            }

            HashSet<int> prevPawns = SnapshotFactionPawns(home, raidFac);
            HashSet<int> prevLords = SnapshotLords(home);

            float rawPoints = StorytellerUtility.DefaultThreatPointsNow(home) * Mathf.Max(0.1f, pointsFactor);
            float minPoints = 150f;
            if (raidFac.def != null)
            {
                float facMin = raidFac.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat); // CHECK API
                if (facMin > minPoints) minPoints = facMin;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, home);
            parms.target = home;
            parms.faction = raidFac;
            parms.points = Mathf.Max(minPoints, rawPoints);
            parms.forced = true;

            bool executed = IncidentDefOf.RaidEnemy.Worker.TryExecute(parms); // CHECK API
            if (!executed)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                executed = IncidentDefOf.RaidEnemy.Worker.TryExecute(parms); // CHECK API
            }

            if (!executed)
            {
                RestoreRelationsIfNeeded(ctx);
                return false;
            }

            List<int> trackedIds = new List<int>();
            int newLordId = -1;

            List<Lord> lords = home.lordManager.lords;
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
                            if (p != null && !trackedIds.Contains(p.thingIDNumber))
                            {
                                trackedIds.Add(p.thingIDNumber);
                            }
                        }
                    }
                    break;
                }
            }

            List<Pawn> spawned = home.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawned.Count; i++)
            {
                Pawn p = spawned[i];
                if (p == null || p.Faction != raidFac) continue;
                if (!prevPawns.Contains(p.thingIDNumber) && !trackedIds.Contains(p.thingIDNumber))
                {
                    trackedIds.Add(p.thingIDNumber);
                }
            }

            ctx.run.vars["raidLordId"] = newLordId.ToString();
            ctx.run.vars["raidPawnIds"] = JoinInts(trackedIds);
            return true;
        }

        private bool IsRaidCleared(StoryContext ctx, Map home)
        {
            List<int> trackedIds = ParseInts(GetStr(ctx, "raidPawnIds"));
            int lordId = GetInt(ctx, "raidLordId", -1);
            int facId = GetInt(ctx, "raidFactionId", -1);

            Lord activeLord = null;
            if (lordId >= 0)
            {
                List<Lord> lords = home.lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    if (lords[i] != null && lords[i].loadID == lordId)
                    {
                        activeLord = lords[i];
                        break;
                    }
                }
            }

            if (trackedIds.Count > 0)
            {
                for (int i = 0; i < trackedIds.Count; i++)
                {
                    Pawn p = FindPawnOnMapOrPods(home, trackedIds[i]);
                    if (p == null) continue;
                    if (IsActiveRaider(p, home)) return false;
                }
                return true;
            }

            if (lordId >= 0)
            {
                if (activeLord == null) return true;
                if (activeLord.ownedPawns != null)
                {
                    for (int i = 0; i < activeLord.ownedPawns.Count; i++)
                    {
                        Pawn p = activeLord.ownedPawns[i];
                        if (IsActiveRaider(p, home)) return false;
                    }
                }
                return true;
            }

            Faction raidFac = FindFactionById(facId);
            if (raidFac != null)
            {
                List<Pawn> pawns = home.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p != null && p.Faction == raidFac && IsActiveRaider(p, home)) return false;
                }
            }
            return true;
        }

        private static bool IsActiveRaider(Pawn p, Map home)
        {
            if (p == null || p.Dead || p.Destroyed) return false;
            if (p.IsPrisonerOfColony) return false;

            // Still inside an incoming drop pod on the home map.
            if (!p.Spawned && p.MapHeld == home) return true;
            if (!p.Spawned || p.Map != home) return false;
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

        private static Pawn FindPawnOnMapOrPods(Map home, int thingIdNumber)
        {
            List<Pawn> spawned = home.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawned.Count; i++)
            {
                Pawn p = spawned[i];
                if (p != null && p.thingIDNumber == thingIdNumber) return p;
            }

            List<Thing> skyfallers = home.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod);
            for (int i = 0; i < skyfallers.Count; i++)
            {
                Pawn found = FindPawnInHolder(skyfallers[i] as IThingHolder, thingIdNumber);
                if (found != null) return found;
            }

            List<Thing> allThings = home.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                DropPodIncoming incoming = allThings[i] as DropPodIncoming;
                if (incoming != null)
                {
                    Pawn found = FindPawnInHolder(incoming.Contents, thingIdNumber);
                    if (found != null) return found;
                }
            }

            return null;
        }

        private static Pawn FindPawnInHolder(IThingHolder holder, int thingIdNumber)
        {
            if (holder == null) return null;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn p = owner[i] as Pawn;
                    if (p != null && p.thingIDNumber == thingIdNumber) return p;
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                Pawn found = FindPawnInHolder(children[i], thingIdNumber);
                if (found != null) return found;
            }
            return null;
        }

        private string Finish(StoryContext ctx, string outcome)
        {
            RestoreRelationsIfNeeded(ctx);
            ctx.run.vars["finished"] = outcome;
            return outcome;
        }

        private void RestoreRelationsIfNeeded(StoryContext ctx)
        {
            if (!restoreRelations) return;
            if (!GetBool(ctx, "wasNonHostile")) return;

            int facId = GetInt(ctx, "raidFactionId", -1);
            Faction raidFac = FindFactionById(facId);
            if (raidFac == null) return;

            int origGoodwill = GetInt(ctx, "origGoodwill", 0);
            int origKindInt = GetInt(ctx, "origKind", (int)FactionRelationKind.Neutral);
            FactionRelationKind origKind = (FactionRelationKind)origKindInt;

            int delta = origGoodwill - raidFac.PlayerGoodwill;
            if (delta != 0)
            {
                raidFac.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null); // CHECK API
            }
            if (raidFac.RelationKindWith(Faction.OfPlayer) != origKind)
            {
                raidFac.SetRelationDirect(Faction.OfPlayer, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }
            ctx.run.vars["wasNonHostile"] = "false";
        }

        private static void ForceHostile(Faction fac)
        {
            if (fac == null || fac.HostileTo(Faction.OfPlayer)) return;
            int drop = -100 - fac.PlayerGoodwill;
            if (drop < 0)
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null); // CHECK API
            }
            if (!fac.HostileTo(Faction.OfPlayer))
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }
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

        private static Faction FindFactionById(int loadId)
        {
            if (loadId < 0) return null;
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId) return all[i];
            }
            return null;
        }

        private static HashSet<int> SnapshotFactionPawns(Map map, Faction fac)
        {
            HashSet<int> ids = new HashSet<int>();
            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && p.Faction == fac) ids.Add(p.thingIDNumber);
            }
            return ids;
        }

        private static HashSet<int> SnapshotLords(Map map)
        {
            HashSet<int> ids = new HashSet<int>();
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

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            string raw;
            int val;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
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
