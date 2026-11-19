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
