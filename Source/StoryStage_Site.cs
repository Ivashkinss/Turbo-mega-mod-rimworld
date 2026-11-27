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
    ///
    /// Round 2 invariants:
    /// - Hero transfer (KillHero / Rescue) goes EXCLUSIVELY through StoryPawnTransfer (journaled,
    ///   exception-safe rollback of WorldPawns membership, faction including null, guest status,
    ///   and leadership).
    /// - NEVER calls Site.Destroy() when a site map is generated.
    /// - Retrieve distinguishes PlayerOwned (player caravan, player colonist/animal/mech on home map,
    ///   or player stockpile/storage building on home map) vs InTransitOrOnSite (flying player pods /
    ///   TravellingTransporters, incoming/leaving pods, NPC inventory on home map, loose ground, site map)
    ///   vs Lost (-> failed). World/map/holder scan is throttled to once every 250 ticks.
    /// - Hold uses a 60-tick live presence sampler (StorySiteHoldMonitor, explicit 60-tick discretization)
    ///   so any absence/downed gap is excluded immediately, while partial confirmed progress is preserved.
    /// </summary>
    public sealed class StoryStage_Site : StoryStage
    {
        public const float TilesPerDay = 4f;
        public const int RetrieveScanInterval = 250;
        public const int HoldSampleInterval = 60;

        private enum RetrieveItemState
        {
            PlayerOwned,
            InTransitOrOnSite,
            Lost
        }

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

        // Cached reflection fallback for TravellingTransporters.transporters
        private static bool s_transportersFieldChecked;
        private static FieldInfo s_transportersListField;

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
            ctx.run.vars["lastRetrieveScanTick"] = "-1";
            ctx.run.vars["heroSpawned"] = "false";
            ctx.run.vars["heroImprisonedOnSite"] = "false";
            ctx.run.vars["heroUnavailable"] = "false";
            ctx.run.vars["thingSpawned"] = "false";
            ctx.run.vars["thingSpawnFailed"] = "false";

            // Pre-check hero availability via StoryPawnTransfer BEFORE creating a Site on the world map
            if (objective == StorySiteObjective.KillHero || objective == StorySiteObjective.Rescue)
            {
                string preFail;
                if (!IsHeroEligibleForSite(hero, out preFail))
                {
                    ctx.run.vars["heroUnavailable"] = "true";
                    ctx.run.vars["heroFailReason"] = preFail ?? "unavailable";
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
                // Buffer so StoryStage_Site handles timeout deterministically while vanilla removes map-less sites safely
                timeoutComp.StartTimeout(Mathf.CeilToInt((timeoutDays + 2f) * GenDate.TicksPerDay)); // CHECK API
            }

            Find.WorldObjects.Add(site);
            ctx.run.vars["siteId"] = site.ID.ToString();

            string letterTitle = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "TMM.Story.Site.Title".Translate().ToString()), ctx);
            string letterBody = StoryUtil.Format(!text.NullOrEmpty() ? text : Objective(ctx), ctx);
            Find.LetterStack.ReceiveLetter(letterTitle, letterBody, LetterDefOf.NeutralEvent, new LookTargets(site)); // CHECK API
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            int now = Find.TickManager.TicksGame;

            // Ensure deadlineTick is persisted once if missing after a save migration (never slides forward)
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

            // 2. Check deadline FIRST before objective completion
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
                    InitializeSiteMap(ctx, site, now);
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

            // 4. Evaluate objective
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

            // 5. If the site vanished without fulfilling the objective (except Retrieve in transit / home map), timeout
            if (site == null)
            {
                if (objective == StorySiteObjective.Retrieve && GetBool(ctx, "thingSpawned"))
                {
                    // Item may still be in transit inside a flying pod or un-hauled on home map; let Retrieve timer/scan decide
                    return null;
                }
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
                return "TMM.Story.Site.HoldGoal".Translate(baseText, hoursDone.ToString("0.0"), holdHours.ToString("0.0"), "TMM.Story.DaysLeft".Translate(daysLeft.ToString("0.0")));
            }

            return "TMM.Story.Hotel.GoalTime".Translate(baseText, "TMM.Story.DaysLeft".Translate(daysLeft.ToString("0.0")));
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

            site.customLabel = null;
            if (timedOut)
            {
                TimeoutComp tc = site.GetComponent<TimeoutComp>();
                if (tc != null)
                {
                    tc.StartTimeout(1); // CHECK API
                }
            }
        }

        private string DefaultObjectiveText()
        {
            if (objective == StorySiteObjective.Clear) return "TMM.Story.Site.Clear".Translate();
            if (objective == StorySiteObjective.Rescue) return "TMM.Story.Site.Rescue".Translate();
            if (objective == StorySiteObjective.Retrieve) return "TMM.Story.Site.Retrieve".Translate();
            if (objective == StorySiteObjective.Hold) return "TMM.Story.Site.Hold".Translate();
            if (objective == StorySiteObjective.KillHero) return "TMM.Story.Site.KillHero".Translate();
            return "TMM.Story.Site.Complete".Translate();
        }

        // =====================================================================
        // SAFE HERO & ITEM INITIALIZATION (EXCLUSIVELY VIA StoryPawnTransfer)
        // =====================================================================

        private static bool IsHeroEligibleForSite(HeroDef heroDef, out string failReason)
        {
            failReason = null;
            if (heroDef == null || HeroRegistry.Instance == null)
            {
                failReason = "hero_def_or_registry_null";
                return false;
            }
            Pawn p = HeroRegistry.Instance.GetOrCreate(heroDef);
            return StoryPawnTransfer.IsFreeWorldPawn(p, null, out failReason);
        }

        private void InitializeSiteMap(StoryContext ctx, Site site, int now)
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
                if (!TryPlaceRetrieveItem(ctx, map, now))
                {
                    ctx.run.vars["thingSpawnFailed"] = "true";
                }
            }
        }

        private bool TryPlaceHeroForKill(StoryContext ctx, Site site, Map map)
        {
            if (hero == null || HeroRegistry.Instance == null || site.Faction == null) return false;

            Pawn target = HeroRegistry.Instance.GetOrCreate(hero);
            IntVec3 cell = FindSpawnCellNearCenter(map);

            PawnTransferHandle handle;
            string failReason;
            bool ok = StoryPawnTransfer.TrySpawnFreeWorldPawn(
                target,
                map,
                cell,
                site.Faction,
                true,
                null,
                null,
                delegate(Pawn spawnedPawn)
                {
                    return AttachToHostileLord(map, site.Faction, spawnedPawn, cell);
                },
                true,
                out handle,
                out failReason);

            if (!ok)
            {
                ctx.run.vars["heroFailReason"] = failReason ?? "transfer_failed";
                return false;
            }

            ctx.run.vars["targetPawnId"] = target.ThingID;
            ctx.run.vars["targetPawnIdNum"] = target.thingIDNumber.ToString();
            ctx.run.vars["heroSpawned"] = "true";
            return true;
        }

        private bool TryPlaceHeroForRescue(StoryContext ctx, Site site, Map map)
        {
            if (hero == null || HeroRegistry.Instance == null || site.Faction == null) return false;

            Pawn captive = HeroRegistry.Instance.GetOrCreate(hero);
            IntVec3 cell = FindSpawnCellNearCenter(map);

            bool needClearFaction = (captive != null && captive.Faction == site.Faction);
            Faction targetFaction = needClearFaction ? null : (captive != null ? captive.Faction : null);

            PawnTransferHandle handle;
            string failReason;
            bool ok = StoryPawnTransfer.TrySpawnFreeWorldPawn(
                captive,
                map,
                cell,
                targetFaction,
                needClearFaction,
                site.Faction,
                null,
                null,
                true,
                out handle,
                out failReason);

            if (!ok)
            {
                ctx.run.vars["heroFailReason"] = failReason ?? "transfer_failed";
                return false;
            }

            ctx.run.vars["targetPawnId"] = captive.ThingID;
            ctx.run.vars["targetPawnIdNum"] = captive.thingIDNumber.ToString();
            ctx.run.vars["heroSpawned"] = "true";
            ctx.run.vars["heroImprisonedOnSite"] = "true";
            return true;
        }

        private bool TryPlaceRetrieveItem(StoryContext ctx, Map map, int now)
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
            // Record spawn tick as last scan tick so we don't rescan immediately on the spawn tick
            ctx.run.vars["lastRetrieveScanTick"] = now.ToString();
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

                // Throttle world/caravan/map/holder scan to once every RetrieveScanInterval (250 ticks)
                int lastScan = GetInt(ctx, "lastRetrieveScanTick", -1);
                if (lastScan >= 0 && now >= lastScan && (now - lastScan) < RetrieveScanInterval)
                {
                    return null;
                }
                ctx.run.vars["lastRetrieveScanTick"] = now.ToString();

                RetrieveItemState itemState = LocateRetrieveItem(thingId, thing, site);
                if (itemState == RetrieveItemState.PlayerOwned)
                {
                    return "success";
                }
                if (itemState == RetrieveItemState.InTransitOrOnSite)
                {
                    return null;
                }
                return "failed";
            }

            if (objective == StorySiteObjective.Hold)
            {
                if (UpdateHoldProgress(ctx, site, now, deadline))
                {
                    return "success";
                }
                return null;
            }

            return null;
        }

        // =====================================================================
        // HOLD PROGRESS (60-TICK LIVE SAMPLING VIA StorySiteHoldMonitor)
        // =====================================================================

        /// <summary>
        /// Updates confirmed Hold ticks on an active Hold stage.
        /// Called every 60 ticks by StorySiteHoldMonitor on the site map (so absences/downed gaps
        /// are detected at 60-tick resolution and never credited) and also on StoryStage_Site.Tick.
        /// Never completes the stage directly when called from monitor; returns true if target reached.
        /// </summary>
        internal bool UpdateHoldProgress(StoryContext ctx, Site site, int now, int deadline)
        {
            if (ctx == null || ctx.run == null) return false;
            if (ctx.run.vars.ContainsKey("finished")) return false;
            if (now >= deadline) return false;

            bool presentNow = site != null && site.HasMap && site.Map != null && HasStandingPlayerPawns(site.Map);
            bool wasPresent = GetBool(ctx, "holdActive");
            int lastTick = GetInt(ctx, "lastHoldCheckTick", -1);

            if (!presentNow)
            {
                ctx.run.vars["holdActive"] = "false";
                ctx.run.vars["lastHoldCheckTick"] = "-1";
                return false;
            }

            if (!wasPresent || lastTick < 0)
            {
                // First sample after entry or re-entry: mark active without adding prior elapsed ticks
                ctx.run.vars["holdActive"] = "true";
                ctx.run.vars["lastHoldCheckTick"] = now.ToString();
                return false;
            }

            int held = GetInt(ctx, "heldTicks", 0);
            if (now > lastTick)
            {
                int cappedNow = Mathf.Min(now, deadline);
                int dt = Mathf.Max(0, cappedNow - lastTick);
                held += dt;
                ctx.run.vars["heldTicks"] = held.ToString();
                ctx.run.vars["lastHoldCheckTick"] = now.ToString();
            }

            int targetTicks = Mathf.Max(1, Mathf.RoundToInt(holdHours * GenDate.TicksPerHour));
            return held >= targetTicks;
        }

        /// <summary>
        /// Invoked every 60 ticks by StorySiteHoldMonitor on maps whose Parent is a Site.
        /// </summary>
        public static void SampleHoldPresenceForSiteMap(Map siteMap, int now)
        {
            if (siteMap == null) return;
            Site site = siteMap.Parent as Site;
            if (site == null) return;

            SparkStory story = SparkStory.Instance;
            if (story == null) return;

            foreach (StoryQuestRun run in story.Runs)
            {
                if (run == null || run.status != StoryQuestStatus.Active || run.vars == null) continue;
                if (run.vars.ContainsKey("finished")) continue;

                StoryQuestDef def = run.Def;
                if (def == null || run.stage < 0 || run.stage >= def.stages.Count) continue;

                StoryStage_Site st = def.stages[run.stage] as StoryStage_Site;
                if (st == null || st.objective != StorySiteObjective.Hold) continue;

                string rawSiteId;
                int runSiteId;
                if (!run.vars.TryGetValue("siteId", out rawSiteId) || !int.TryParse(rawSiteId, out runSiteId) || runSiteId != site.ID)
                {
                    continue;
                }

                StoryContext ctx = story.Context(run);
                int deadline;
                if (!TryGetInt(ctx, "deadlineTick", out deadline)) continue;

                st.UpdateHoldProgress(ctx, site, now, deadline);
            }
        }

        // =====================================================================
        // RETRIEVE OWNERSHIP & TRANSIT CLASSIFICATION
        // =====================================================================

        /// <summary>
        /// Classifies exact thingId across:
        /// 1) Player caravans -> PlayerOwned (success)
        /// 2) Player home maps:
        ///    - Player stockpile / player storage building -> PlayerOwned (success)
        ///    - Player-owned pawn (colonist/animal/mech, not prisoner) -> PlayerOwned (success)
        ///    - Player-owned non-pod storage container building -> PlayerOwned (success)
        ///    - NPC inventory/equipment/carry, loose ground outside stockpile, or incoming/leaving pods -> InTransitOrOnSite (wait)
        /// 3) World map TravellingTransporters (flying player pods) -> InTransitOrOnSite (wait)
        /// 4) Site map or any other non-home map -> InTransitOrOnSite (wait)
        /// 5) Nowhere -> Lost (failed)
        /// </summary>
        private static RetrieveItemState LocateRetrieveItem(string thingId, ThingDef defHint, Site site)
        {
            if (thingId.NullOrEmpty()) return RetrieveItemState.Lost;

            bool foundInTransitOrElsewhere = false;

            // 1. Player caravans on world map -> confirmed PlayerOwned
            if (IsExactThingInPlayerCaravan(thingId))
            {
                return RetrieveItemState.PlayerOwned;
            }

            // 2. Player home maps: strict ownership verification (NPC inventory != delivered!)
            if (Find.Maps != null)
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    Map m = maps[i];
                    if (m == null || !m.IsPlayerHome) continue;

                    RetrieveItemState homeState = CheckExactThingOnHomeMap(m, thingId, defHint);
                    if (homeState == RetrieveItemState.PlayerOwned)
                    {
                        return RetrieveItemState.PlayerOwned;
                    }
                    if (homeState == RetrieveItemState.InTransitOrOnSite)
                    {
                        foundInTransitOrElsewhere = true;
                    }
                }
            }

            // 3. Flying transport pods (TravellingTransporters) & other world-map holders -> InTransit
            if (IsExactThingInWorldTransporters(thingId))
            {
                foundInTransitOrElsewhere = true;
            }

            // 4. Site map & any other active non-home map -> InTransitOrOnSite
            if (!foundInTransitOrElsewhere && site != null && site.HasMap && site.Map != null)
            {
                if (IsExactThingAnywhereOnMap(site.Map, thingId, defHint))
                {
                    foundInTransitOrElsewhere = true;
                }
            }

            if (!foundInTransitOrElsewhere && Find.Maps != null)
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    Map m = maps[i];
                    if (m == null || m.IsPlayerHome || (site != null && m == site.Map)) continue;
                    if (IsExactThingAnywhereOnMap(m, thingId, defHint))
                    {
                        foundInTransitOrElsewhere = true;
                        break;
                    }
                }
            }

            return foundInTransitOrElsewhere ? RetrieveItemState.InTransitOrOnSite : RetrieveItemState.Lost;
        }

        private static RetrieveItemState CheckExactThingOnHomeMap(Map home, string thingId, ThingDef defHint)
        {
            if (home == null || thingId.NullOrEmpty()) return RetrieveItemState.Lost;

            bool foundNonOwned = false;

            // A. Directly spawned on home map (stockpile/player storage -> PlayerOwned; loose ground -> wait)
            if (defHint != null && home.listerThings != null)
            {
                List<Thing> byDef = home.listerThings.ThingsOfDef(defHint);
                for (int i = 0; i < byDef.Count; i++)
                {
                    Thing t = byDef[i];
                    if (t != null && !t.Destroyed && t.ThingID == thingId)
                    {
                        if (IsInPlayerStorageSlot(home, t))
                        {
                            return RetrieveItemState.PlayerOwned;
                        }
                        foundNonOwned = true;
                    }
                }
            }

            // B. Pawns on home map: ONLY player-owned non-prisoner pawns count as delivered!
            // NPC visitors, traders, raiders, or prisoners carrying the item do NOT count as delivered.
            if (home.mapPawns != null)
            {
                IReadOnlyList<Pawn> pawns = home.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p == null || !PawnHasExactThing(p, thingId)) continue;

                    if (p.Faction == Faction.OfPlayer && !p.IsPrisoner)
                    {
                        return RetrieveItemState.PlayerOwned;
                    }
                    foundNonOwned = true;
                }
            }

            // C. ThingHolders on home map (incoming/leaving pods -> transit; player container buildings -> PlayerOwned)
            if (home.listerThings != null)
            {
                List<Thing> holders = home.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    Thing h = holders[i];
                    if (h == null || h.Destroyed || h is Pawn) continue;
                    if (!ThingContainsExactThing(h, thingId)) continue;

                    if (IsPodOrSkyfaller(h))
                    {
                        foundNonOwned = true;
                    }
                    else if (h is Building && h.Faction == Faction.OfPlayer)
                    {
                        return RetrieveItemState.PlayerOwned;
                    }
                    else
                    {
                        foundNonOwned = true;
                    }
                }
            }

            return foundNonOwned ? RetrieveItemState.InTransitOrOnSite : RetrieveItemState.Lost;
        }

        private static bool IsInPlayerStorageSlot(Map map, Thing t)
        {
            if (map == null || t == null || t.Destroyed || !t.Spawned || t.Map != map) return false;
            if (map.haulDestinationManager == null) return false;

            SlotGroup sg = map.haulDestinationManager.SlotGroupAt(t.Position); // CHECK API
            if (sg == null || sg.parent == null) return false;

            if (sg.parent is Zone_Stockpile) return true;

            Building b = sg.parent as Building;
            if (b != null && b.Faction == Faction.OfPlayer) return true;

            return false;
        }

        private static bool IsPodOrSkyfaller(Thing t)
        {
            if (t == null) return false;
            if (t is Skyfaller || t is ActiveTransporter) return true; // CHECK API
            ThingWithComps twc = t as ThingWithComps;
            if (twc != null && twc.GetComp<CompTransporter>() != null) return true; // CHECK API
            return false;
        }

        private static bool IsExactThingInWorldTransporters(string thingId)
        {
            if (thingId.NullOrEmpty() || Find.WorldObjects == null) return false;

            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < all.Count; i++)
            {
                WorldObject wo = all[i];
                if (wo == null || wo.Destroyed || wo is Caravan) continue;

                TravellingTransporters pods = wo as TravellingTransporters; // CHECK API
                if (pods != null)
                {
                    if (TravellingTransportersContainsExactThing(pods, thingId))
                    {
                        return true;
                    }
                    continue;
                }

                // Other world-map IThingHolder objects (e.g. player outposts)
                IThingHolder holder = wo as IThingHolder;
                if (holder != null && HolderContainsExactThing(holder, thingId))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TravellingTransportersContainsExactThing(TravellingTransporters pods, string thingId)
        {
            if (pods == null || thingId.NullOrEmpty()) return false;

            IThingHolder asHolder = pods as IThingHolder;
            if (asHolder != null && HolderContainsExactThing(asHolder, thingId))
            {
                return true;
            }

            if (!s_transportersFieldChecked)
            {
                s_transportersFieldChecked = true;
                s_transportersListField = AccessTools.Field(typeof(TravellingTransporters), "transporters"); // CHECK API
            }

            if (s_transportersListField != null)
            {
                IList list = s_transportersListField.GetValue(pods) as IList;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        IThingHolder h = list[i] as IThingHolder;
                        if (h != null && HolderContainsExactThing(h, thingId))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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

        private static bool IsTrackedHeroConfirmedDead(HeroDef heroDef, string expectedThingId, Map siteMap)
        {
            if (expectedThingId.NullOrEmpty()) return false;

            if (HasCorpseWithId(siteMap, expectedThingId)) return true;

            if (heroDef != null && HeroRegistry.Instance != null)
            {
                bool confirmed;
                if (TryCheckHeroRegistryDead(heroDef, expectedThingId, out confirmed) && confirmed)
                {
                    return true;
                }
            }

            if (Find.Maps != null)
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    Map m = maps[i];
                    if (m != null && m.IsPlayerHome && m != siteMap && HasCorpseWithId(m, expectedThingId))
                    {
                        return true;
                    }
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
                    Pawn directPawn = rec as Pawn;
                    if (directPawn != null)
                    {
                        isConfirmedDead = directPawn.ThingID == expectedThingId && directPawn.Dead;
                        return true;
                    }

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
            if (pawnId.NullOrEmpty() || Find.Maps == null) return false;
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

        private static bool IsExactThingAnywhereOnMap(Map map, string thingId, ThingDef defHint)
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
                    Thing h = holders[i];
                    if (h == null || h.Destroyed || h is Pawn) continue;
                    if (ThingContainsExactThing(h, thingId)) return true;
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

        private static bool ThingContainsExactThing(Thing h, string thingId)
        {
            if (h == null || h.Destroyed) return false;
            if (h.ThingID == thingId) return true;

            IThingHolder directHolder = h as IThingHolder;
            if (directHolder != null && HolderContainsExactThing(directHolder, thingId))
            {
                return true;
            }

            // Also inspect ThingComp IThingHolders (e.g. CompTransporter on Building_TransportPod)
            ThingWithComps twc = h as ThingWithComps;
            if (twc != null && twc.AllComps != null)
            {
                List<ThingComp> comps = twc.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    IThingHolder compHolder = comps[i] as IThingHolder;
                    if (compHolder != null && HolderContainsExactThing(compHolder, thingId))
                    {
                        return true;
                    }
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

        private static bool AttachToHostileLord(Map map, Faction fac, Pawn pawn, IntVec3 cell)
        {
            if (map == null || fac == null || pawn == null || map.lordManager == null) return false;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord l = lords[i];
                if (l != null && l.faction == fac)
                {
                    l.AddPawn(pawn);
                    return pawn.GetLord() == l;
                }
            }
            List<Pawn> group = new List<Pawn>();
            group.Add(pawn);
            Lord created = LordMaker.MakeNewLord(fac, new LordJob_DefendPoint(cell), map, group);
            return created != null && pawn.GetLord() == created;
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

    /// <summary>
    /// Lightweight MapComponent that samples standing-colonist presence every 60 ticks
    /// (explicit 60-tick discretization step) ONLY on maps belonging to a Site.
    /// Instantly returns on non-Site maps. Prevents crediting absence/downed intervals
    /// between 250-tick SparkStory director polls.
    /// </summary>
    public sealed class StorySiteHoldMonitor : MapComponent
    {
        public StorySiteHoldMonitor(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (map == null || !(map.Parent is Site)) return;
            int now = Find.TickManager.TicksGame;
            if (now % StoryStage_Site.HoldSampleInterval != 0) return;
            StoryStage_Site.SampleHoldPresenceForSiteMap(map, now);
        }
    }
}
