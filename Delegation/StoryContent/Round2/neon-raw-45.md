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

            string letterTitle = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Site"), ctx);
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
            if (objective == StorySiteObjective.Clear) return "Clear hostile site";
            if (objective == StorySiteObjective.Rescue) return "Rescue captive hero";
            if (objective == StorySiteObjective.Retrieve) return "Retrieve target item";
            if (objective == StorySiteObjective.Hold) return "Hold site";
            if (objective == StorySiteObjective.KillHero) return "Eliminate target hero";
            return "Complete site objective";
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
                Log.Error("[TMM] StoryStage_ColonyRaid: TryExecute threw exception: " + ex);
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
```

`Tests/InGameStoryStagesProbe.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        // Runtime probe quest defNames (registered identically on fresh & reload)
        public const string QuestDownedTimeout = "TMM_Probe_Site_DownedTimeout";
        public const string QuestKillRecruited = "TMM_Probe_Site_KillRecruited";
        public const string QuestKillTransferException = "TMM_Probe_Site_KillTransferException";
        public const string QuestRetrieveDestroyed = "TMM_Probe_Site_RetrieveDestroyed";
        public const string QuestRetrieveTransitAndNpc = "TMM_Probe_Site_RetrieveTransitAndNpc";
        public const string QuestRaidNeutralTwoColonies = "TMM_Probe_Raid_NeutralTwoColonies";
        public const string QuestRaidStageRemovedOrphan = "TMM_Probe_Raid_StageRemovedOrphan";
        public const string QuestRaidEmptyCohort = "TMM_Probe_Raid_EmptyCohort";
        public const string QuestRaidFailedSpawnKeepHostile = "TMM_Probe_Raid_FailedSpawnKeepHostile";
        public const string QuestForceOutcomeActive = "TMM_Probe_Site_ForceOutcomeActive";
        public const string QuestHoldAbsenceGap = "TMM_Probe_Site_HoldAbsenceGap";
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
            HeroDef heroC = PickAvailableHero(2) ?? heroB;

            AddQuestDef(BuildSiteQuest(QuestDownedTimeout, "probe.downedTimeout", StorySiteObjective.Clear, null, null, 0.2f, 4f));
            AddQuestDef(BuildSiteQuest(QuestKillRecruited, "probe.killRecruited", StorySiteObjective.KillHero, heroA, null, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestKillTransferException, "probe.killTransferEx", StorySiteObjective.KillHero, heroC, null, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestRetrieveDestroyed, "probe.retrieveDestroyed", StorySiteObjective.Retrieve, null, ThingDefOf.ComponentIndustrial, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestRetrieveTransitAndNpc, "probe.retrieveTransitNpc", StorySiteObjective.Retrieve, null, ThingDefOf.ComponentIndustrial, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestForceOutcomeActive, "probe.forceOutcomeActive", StorySiteObjective.Clear, null, null, 5f, 4f));
            AddQuestDef(BuildSiteQuest(QuestHoldAbsenceGap, "probe.holdAbsenceGap", StorySiteObjective.Hold, null, null, 5f, 4f));

            AddQuestDef(BuildRaidQuest(QuestRaidNeutralTwoColonies, "probe.raidNeutral", true));
            AddQuestDef(BuildRaidQuest(QuestRaidStageRemovedOrphan, "probe.raidStageRemoved", true));
            AddQuestDef(BuildRaidQuest(QuestRaidEmptyCohort, "probe.raidEmptyCohort", true));
            AddQuestDef(BuildRaidQuest(QuestRaidFailedSpawnKeepHostile, "probe.raidFailedKeepHostile", false));

            AddQuestDef(BuildSiteQuest(QuestReloadRescue, "probe.reloadRescue", StorySiteObjective.Rescue, heroB, null, 10f, 4f));
            AddQuestDef(BuildSiteQuest(QuestReloadRetrieve, "probe.reloadRetrieve", StorySiteObjective.Retrieve, null, ThingDefOf.ComponentIndustrial, 10f, 4f));
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
    /// Round 2 in-game verification probe for E2 (StoryStage_Site &amp; StoryStage_ColonyRaid).
    /// Covers all Round 2 findings:
    /// - Hero mid-transfer exception (via TestMode hook) -> hero remains in WorldPawns with original faction;
    /// - Raid with empty confirmed raiders -> timeout (NEVER success);
    /// - Failed raid spawn with restoreRelations=false -> hostility rolled back via AbortLease;
    /// - Orphan cleanup via REAL stage removal on StoryQuestDef (not manual status mutation);
    /// - Symmetric relation restore verification + blocked-restore backup retention &amp; retry;
    /// - Retrieve: NPC inventory on home map != delivered, flying TravellingTransporters == transit (not lost),
    ///   player stockpile == delivered, and 250-tick scan throttle;
    /// - Hold: absence/downed gap sampled by 60-tick monitor is NOT credited;
    /// - forceOutcome &amp; legacy vars["forceOutcome"] tested on an ACTIVE run;
    /// - Honest UI report passed value (computed from actual checks and errors, never hardcoded true);
    /// - Clean probe pawn generation without global SanitizeDanglingPawnRelations before SaveGame.
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
                StoryPawnTransfer.TestSpawnOverride = null;
                StoryStage_ColonyRaid.TestRaidExecuteOverride = null;
                StoryRelationRestorer.TestBlockRestoreOverride = null;
                Application.logMessageReceived -= OnUnityLog;

                bool allPassed = WriteReport(StoryStagesProbeBootstrap.ReloadMode ? "StoryStagesReloadProbe.json" : "StoryStagesProbe.json");
                if (!StoryStagesProbeBootstrap.ReloadMode)
                {
                    WriteUiReport(allPassed);
                }
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
        // FRESH PROBE
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
                Pawn scout = SpawnCleanColonist(map);
                Caravan car = EnterSiteWithScout(scout, downedSite);
                if (downedSite.HasMap && downedSite.Map != null)
                {
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

            // ---------------- 3. HERO MID-TRANSFER EXCEPTION ROLLBACK (TestMode) ----------------
            if (TestMode.Enabled)
            {
                StoryQuestDef exDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestKillTransferException);
                StoryStage_Site exStage = exDef != null ? exDef.stages[0] as StoryStage_Site : null;
                Pawn exHero = (exStage != null && exStage.hero != null && HeroRegistry.Instance != null)
                    ? HeroRegistry.Instance.GetOrCreate(exStage.hero) : null;

                if (exHero != null)
                {
                    Faction origFac = exHero.Faction;
                    bool origInWorld = Find.WorldPawns.Contains(exHero);

                    story.SetFlag("probe.killTransferEx", "true");
                    story.DirectorTick();
                    StoryQuestRun exRun = story.RunFor(StoryStagesProbeBootstrap.QuestKillTransferException);
                    Site exSite = FindSite(ParseInt(exRun, "siteId", -1));

                    if (exSite != null && origInWorld)
                    {
                        Pawn scout = SpawnCleanColonist(map);
                        Caravan car = EnterSiteWithScout(scout, exSite);

                        // Inject exception AFTER StoryPawnTransfer has called SetFaction & RemovePawn!
                        StoryPawnTransfer.TestSpawnOverride = delegate(Pawn p, Map m, IntVec3 c)
                        {
                            throw new InvalidOperationException("Simulated GenSpawn failure mid-transfer");
                        };

                        story.DirectorTick();
                        StoryPawnTransfer.TestSpawnOverride = null;

                        bool backInWorld = Find.WorldPawns.Contains(exHero) && !exHero.Spawned;
                        bool origFacPreserved = exHero.Faction == origFac;
                        bool endedUnavailable = exRun != null && HasOutcome(exRun, "unavailable");

                        AddCheck("R2_HeroTransferException_FullRollbackToWorldPawns",
                            backInWorld && origFacPreserved && endedUnavailable,
                            "inWorldPawns=true, origFaction preserved, outcome=unavailable",
                            string.Format("inWorld={0}, facMatch={1}, hist={2}",
                                backInWorld, origFacPreserved, exRun != null ? string.Join(",", exRun.history.ToArray()) : "null"));

                        CleanupScoutAndSite(scout, car, exSite);
                    }
                    else
                    {
                        AddCheck("R2_HeroTransferException_FullRollbackToWorldPawns", false, "free world hero & site created",
                            string.Format("site={0}, inWorld={1}", exSite != null, origInWorld));
                    }
                }
                else
                {
                    AddCheck("R2_HeroTransferException_FullRollbackToWorldPawns", false, "hero exists", "hero null");
                }
            }

            // ---------------- 4. RETRIEVE: DESTROYED ITEM -> FAILED (NO SUBSTITUTION) ----------------
            story.SetFlag("probe.retrieveDestroyed", "true");
            story.DirectorTick();
            StoryQuestRun retDestRun = story.RunFor(StoryStagesProbeBootstrap.QuestRetrieveDestroyed);
            Site retDestSite = FindSite(ParseInt(retDestRun, "siteId", -1));
            if (retDestSite != null)
            {
                Pawn scout = SpawnCleanColonist(map);
                Caravan car = EnterSiteWithScout(scout, retDestSite);
                story.DirectorTick();

                string realId = GetVar(retDestRun, "targetThingId");
                Thing realThing = FindThingOnMapById(retDestSite.Map, realId);

                Pawn decoyPawn = SpawnCleanColonist(map);
                Thing decoyItem = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                decoyPawn.inventory.innerContainer.TryAdd(decoyItem);
                decoyPawn.DeSpawn(DestroyMode.Vanish);
                Caravan decoyCaravan = CaravanMaker.MakeCaravan(new List<Pawn> { decoyPawn }, Faction.OfPlayer, map.Tile, true); // CHECK API

                if (realThing != null && !realThing.Destroyed) realThing.Destroy(DestroyMode.Vanish);

                // Verify 250-tick throttle: ticking immediately (+50 ticks) does NOT rescan yet
                AdvanceTicks(50);
                story.DirectorTick();
                bool throttledBefore250 = retDestRun != null && retDestRun.status == StoryQuestStatus.Active;

                // After 250 ticks total: rescan runs and detects Lost -> failed
                AdvanceTicks(250);
                story.DirectorTick();

                bool gotFailed = retDestRun != null && HasOutcome(retDestRun, "failed") && !HasOutcome(retDestRun, "success");
                AddCheck("P1_RetrieveDestroyed_ThrottledAndFailedNoSubstitution",
                    realThing != null && throttledBefore250 && gotFailed,
                    "throttled=true, outcome=failed",
                    string.Format("realFound={0}, throttled={1}, hist={2}",
                        realThing != null, throttledBefore250, retDestRun != null ? string.Join(",", retDestRun.history.ToArray()) : "null"));

                DestroyTestCaravanAndPawns(decoyCaravan);
                CleanupScoutAndSite(scout, car, retDestSite);
            }
            else
            {
                AddCheck("P1_RetrieveDestroyed_ThrottledAndFailedNoSubstitution", false, "site created", "site null");
            }

            // ---------------- 5. RETRIEVE P2: NPC ON HOME MAP != DELIVERED, FLYING POD == TRANSIT, STOCKPILE == DELIVERED ----------------
            Faction neutralFac = FindNeutralHumanlikeFaction();
            Zone_Stockpile stockpile = EnsureTestStockpile(map);
            story.SetFlag("probe.retrieveTransitNpc", "true");
            story.DirectorTick();
            StoryQuestRun retNpcRun = story.RunFor(StoryStagesProbeBootstrap.QuestRetrieveTransitAndNpc);
            Site retNpcSite = FindSite(ParseInt(retNpcRun, "siteId", -1));
            if (retNpcSite != null && neutralFac != null && stockpile != null)
            {
                Pawn scout = SpawnCleanColonist(map);
                Caravan car = EnterSiteWithScout(scout, retNpcSite);
                story.DirectorTick();

                string realId = GetVar(retNpcRun, "targetThingId");
                Thing realItem = FindThingOnMapById(retNpcSite.Map, realId);
                bool npcNotDelivered = false;
                bool flyingPodIsTransit = false;
                bool stockpileDelivered = false;

                if (realItem != null)
                {
                    // Step 5A: Move exact item into an NPC's inventory on the home map -> MUST NOT complete!
                    realItem.DeSpawn(DestroyMode.Vanish);
                    Pawn npcOnHome = SpawnCleanNpc(map, neutralFac);
                    npcOnHome.inventory.innerContainer.TryAdd(realItem, false);

                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();
                    npcNotDelivered = retNpcRun.status == StoryQuestStatus.Active && !HasOutcome(retNpcRun, "success");

                    // Step 5B: Move exact item into a flying player TravellingTransporters pod -> MUST stay Active (transit, NOT failed!)
                    npcOnHome.inventory.innerContainer.Remove(realItem);
                    CleanupCleanPawn(npcOnHome);

                    TravellingTransporters flyingPods = LaunchTestFlyingPod(map.Tile, retNpcSite.Tile, realItem);
                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();
                    flyingPodIsTransit = flyingPods != null && retNpcRun.status == StoryQuestStatus.Active &&
                                         !HasOutcome(retNpcRun, "failed") && !HasOutcome(retNpcRun, "success");

                    // Step 5C: Unload exact item into player stockpile on home map -> MUST complete with success!
                    ExtractItemFromFlyingPod(flyingPods, realItem);
                    if (flyingPods != null && !flyingPods.Destroyed) flyingPods.Destroy();

                    IntVec3 stockCell = stockpile.cells[0];
                    GenSpawn.Spawn(realItem, stockCell, map, WipeMode.Vanish);
                    realItem.SetForbidden(false, false);

                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();
                    stockpileDelivered = HasOutcome(retNpcRun, "success");
                }

                AddCheck("R2_Retrieve_NpcNotDelivered_FlyingPodTransit_StockpileSuccess",
                    npcNotDelivered && flyingPodIsTransit && stockpileDelivered,
                    "npcWait=true, podTransit=true, stockpileSuccess=true",
                    string.Format("npcWait={0}, podTransit={1}, stockpile={2}", npcNotDelivered, flyingPodIsTransit, stockpileDelivered));

                CleanupScoutAndSite(scout, car, retNpcSite);
            }
            else
            {
                AddCheck("R2_Retrieve_NpcNotDelivered_FlyingPodTransit_StockpileSuccess", false, "site, neutralFac & stockpile exist", "missing");
            }

            // ---------------- 6. HOLD P2: ABSENCE / DOWNED GAP NOT CREDITED (60-TICK MONITOR) ----------------
            story.SetFlag("probe.holdAbsenceGap", "true");
            story.DirectorTick();
            StoryQuestRun gapRun = story.RunFor(StoryStagesProbeBootstrap.QuestHoldAbsenceGap);
            Site gapSite = FindSite(ParseInt(gapRun, "siteId", -1));
            if (gapSite != null)
            {
                Pawn scout = SpawnCleanColonist(map);
                Caravan car = EnterSiteWithScout(scout, gapSite);
                story.DirectorTick();
                // Sample at entry (adds 0)
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                int held0 = ParseInt(gapRun, "heldTicks", -1);

                // Stand 1 hour -> 2500 ticks
                AdvanceTicks(GenDate.TicksPerHour);
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                int held1h = ParseInt(gapRun, "heldTicks", -1);

                // Down the scout and sample 60 ticks later -> absence detected, holdActive=false
                Hediff anesth = scout.health.AddHediff(HediffDefOf.Anesthetic);
                AdvanceTicks(StoryStage_Site.HoldSampleInterval);
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                bool markedInactive = GetVar(gapRun, "holdActive") == "false";

                // 3 hours pass while downed
                AdvanceTicks(3 * GenDate.TicksPerHour);

                // Scout recovers and returns -> first sample marks active without crediting the 3h gap
                if (anesth != null) scout.health.RemoveHediff(anesth);
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                int heldAfterReturn = ParseInt(gapRun, "heldTicks", -1);

                // Stand 1 more hour -> total 5000 ticks (2h, NOT 5h!)
                AdvanceTicks(GenDate.TicksPerHour);
                StoryStage_Site.SampleHoldPresenceForSiteMap(gapSite.Map, Find.TickManager.TicksGame);
                int heldFinal = ParseInt(gapRun, "heldTicks", -1);

                bool gapExcluded = held0 == 0 && held1h == GenDate.TicksPerHour && markedInactive &&
                                   heldAfterReturn == GenDate.TicksPerHour && heldFinal == 2 * GenDate.TicksPerHour &&
                                   gapRun.status == StoryQuestStatus.Active;
                AddCheck("R2_Hold_AbsenceGapExcludedBy60TickSampler", gapExcluded,
                    "0->2500->(3h absent)->2500->5000",
                    string.Format("0={0}, 1h={1}, inactive={2}, afterGap={3}, final={4}",
                        held0, held1h, markedInactive, heldAfterReturn, heldFinal));

                CleanupScoutAndSite(scout, car, gapSite);
            }
            else
            {
                AddCheck("R2_Hold_AbsenceGapExcludedBy60TickSampler", false, "site created", "site null");
            }

            // ---------------- 7. RAID: NEUTRAL FACTION (-50) + SECOND HOME MAP + SYMMETRIC RESTORE ----------------
            StoryQuestDef raidNeutralDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidNeutralTwoColonies);
            StoryStage_ColonyRaid raidNeutralStage = raidNeutralDef != null ? raidNeutralDef.stages[0] as StoryStage_ColonyRaid : null;
            if (neutralFac != null && raidNeutralStage != null)
            {
                ForceNeutralWithGoodwill(neutralFac, -50);
                raidNeutralStage.faction = neutralFac.def.defName;

                story.SetFlag("probe.raidNeutral", "true");
                story.DirectorTick();
                StoryQuestRun raidRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidNeutralTwoColonies);

                Settlement secondHomeObj = null;
                Map secondHomeMap = CreateTemporarySecondHomeMap(out secondHomeObj);

                AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                story.DirectorTick();

                bool hostileDuringRaid = neutralFac.HostileTo(Faction.OfPlayer);
                int pinnedMapId = ParseInt(raidRun, "raidMapId", -1);
                List<int> raidPawnIds = ParseIntList(GetVar(raidRun, "raidPawnIds"));

                story.DirectorTick();
                bool stillActiveWithTwoHomes = raidRun != null && raidRun.status == StoryQuestStatus.Active && pinnedMapId == map.uniqueID;

                int killed = KillTrackedPawns(map, raidPawnIds);

                // In TestMode, also verify blocked-restore retention & retry before final symmetric verification
                bool blockRetryVerified = true;
                if (TestMode.Enabled)
                {
                    StoryRelationRestorer.TestBlockRestoreOverride = delegate(Faction f) { return f == neutralFac; };
                    story.DirectorTick();
                    bool retainedOnBlock = StoryRelationRestorer.Instance != null &&
                                           StoryRelationRestorer.Instance.HasPendingBackupForFaction(neutralFac);
                    StoryRelationRestorer.TestBlockRestoreOverride = null;
                    StoryRelationRestorer.CheckOrphansIfPresent();
                    bool clearedOnRetry = StoryRelationRestorer.Instance != null &&
                                          !StoryRelationRestorer.Instance.HasPendingBackupForFaction(neutralFac);
                    blockRetryVerified = retainedOnBlock && clearedOnRetry;
                }
                else
                {
                    story.DirectorTick();
                }

                bool raidSucceeded = raidRun != null && HasOutcome(raidRun, "success");
                bool symmetricNonHostile = !neutralFac.HostileTo(Faction.OfPlayer) && !Faction.OfPlayer.HostileTo(neutralFac);
                bool symmetricGoodwill = neutralFac.PlayerGoodwill >= -55 && neutralFac.PlayerGoodwill <= -45 &&
                                         Faction.OfPlayer.GoodwillWith(neutralFac) == neutralFac.PlayerGoodwill;

                AddCheck("P1_NeutralRaid_TwoHomesAndSymmetricRestore",
                    hostileDuringRaid && stillActiveWithTwoHomes && killed > 0 && raidSucceeded &&
                    symmetricNonHostile && symmetricGoodwill && blockRetryVerified,
                    "hostile->success->Symmetric Neutral(~-50)",
                    string.Format("hostile={0}, active2={1}, killed={2}, succ={3}, symNonH={4}, gw={5}/{6}, retry={7}",
                        hostileDuringRaid, stillActiveWithTwoHomes, killed, raidSucceeded, symmetricNonHostile,
                        neutralFac.PlayerGoodwill, Faction.OfPlayer.GoodwillWith(neutralFac), blockRetryVerified));

                CleanupSecondHomeMap(secondHomeMap, secondHomeObj);
            }
            else
            {
                AddCheck("P1_NeutralRaid_TwoHomesAndSymmetricRestore", false, "neutral faction & def exist", "missing");
            }

            // ---------------- 8. ORPHAN CLEANUP VIA REAL STAGE REMOVAL (NO DIRECT STATUS MUTATION) ----------------
            StoryQuestDef orphanDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidStageRemovedOrphan);
            StoryStage_ColonyRaid orphanStage = orphanDef != null && orphanDef.stages.Count > 0 ? orphanDef.stages[0] as StoryStage_ColonyRaid : null;
            if (neutralFac != null && orphanDef != null && orphanStage != null)
            {
                ForceNeutralWithGoodwill(neutralFac, -40);
                orphanStage.faction = neutralFac.def.defName;

                story.SetFlag("probe.raidStageRemoved", "true");
                story.DirectorTick();
                StoryQuestRun orphRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidStageRemovedOrphan);

                AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                story.DirectorTick();
                bool becameHostile = neutralFac.HostileTo(Faction.OfPlayer);
                List<int> orphPawnIds = ParseIntList(GetVar(orphRun, "raidPawnIds"));

                // REAL stage removal: clear stages on the Def and let SparkStory.DirectorTick disable the quest naturally!
                try
                {
                    orphanDef.stages.Clear();
                    story.DirectorTick();
                }
                finally
                {
                    orphanDef.stages.Add(orphanStage);
                }

                bool disabledNaturally = orphRun != null && orphRun.status == StoryQuestStatus.Disabled &&
                                         HasOutcome(orphRun, "stage missing");
                bool rolledBackSym = !neutralFac.HostileTo(Faction.OfPlayer) && !Faction.OfPlayer.HostileTo(neutralFac) &&
                                     neutralFac.PlayerGoodwill >= -45 && neutralFac.PlayerGoodwill <= -35;

                AddCheck("R2_OrphanCleanup_RealStageRemovalRollback",
                    becameHostile && disabledNaturally && rolledBackSym,
                    "hostile->SparkStory Disable(stage missing)->Symmetric Neutral(~-40)",
                    string.Format("hostile={0}, disabled={1}, symNonH={2}, gw={3}",
                        becameHostile, disabledNaturally, !neutralFac.HostileTo(Faction.OfPlayer), neutralFac.PlayerGoodwill));

                KillTrackedPawns(map, orphPawnIds);
            }
            else
            {
                AddCheck("R2_OrphanCleanup_RealStageRemovalRollback", false, "neutral faction & def exist", "missing");
            }

            // ---------------- 9. RAID WITHOUT CONFIRMED RAIDERS -> TIMEOUT (NEVER SUCCESS) ----------------
            StoryQuestDef emptyRaidDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidEmptyCohort);
            StoryStage_ColonyRaid emptyRaidStage = emptyRaidDef != null ? emptyRaidDef.stages[0] as StoryStage_ColonyRaid : null;
            if (neutralFac != null && emptyRaidStage != null)
            {
                ForceNeutralWithGoodwill(neutralFac, -35);
                emptyRaidStage.faction = neutralFac.def.defName;

                if (TestMode.Enabled)
                {
                    // Simulate RaidEnemy returning true while spawning ZERO pawns
                    StoryStage_ColonyRaid.TestRaidExecuteOverride = delegate(IncidentParms p) { return true; };
                    story.SetFlag("probe.raidEmptyCohort", "true");
                    story.DirectorTick();
                    AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                    story.DirectorTick();
                    StoryStage_ColonyRaid.TestRaidExecuteOverride = null;

                    StoryQuestRun emptyRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidEmptyCohort);
                    bool endedTimeoutNotSuccess = emptyRun != null && HasOutcome(emptyRun, "timeout") && !HasOutcome(emptyRun, "success");
                    bool rolledBack = !neutralFac.HostileTo(Faction.OfPlayer);
                    AddCheck("R2_RaidZeroRaiders_TimeoutAndRollback",
                        endedTimeoutNotSuccess && rolledBack,
                        "timeout (not success) & hostility rolled back",
                        string.Format("hist={0}, nonHostile={1}",
                            emptyRun != null ? string.Join(",", emptyRun.history.ToArray()) : "null", rolledBack));
                }
                else
                {
                    // Without TestMode: test active spawned raid with empty raidPawnIds & vanished lordId -> MUST timeout, NEVER success
                    story.SetFlag("probe.raidEmptyCohort", "true");
                    story.DirectorTick();
                    StoryQuestRun emptyRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidEmptyCohort);
                    if (emptyRun != null)
                    {
                        emptyRun.vars["spawned"] = "true";
                        emptyRun.vars["raidMapId"] = map.uniqueID.ToString();
                        emptyRun.vars["raidLordId"] = "999999";
                        emptyRun.vars["raidPawnIds"] = string.Empty;
                        emptyRun.vars["confirmedRaiderCount"] = "0";
                        story.DirectorTick();
                    }
                    bool endedTimeoutNotSuccess = emptyRun != null && HasOutcome(emptyRun, "timeout") && !HasOutcome(emptyRun, "success");
                    AddCheck("R2_RaidEmptyCohortVanishedLord_TimeoutNotSuccess",
                        endedTimeoutNotSuccess,
                        "timeout (not success)",
                        emptyRun != null ? string.Join(",", emptyRun.history.ToArray()) : "null");
                }
            }

            // ---------------- 10. FAILED RAID SPAWN WITH restoreRelations=false -> STILL ROLLS BACK HOSTILITY (TestMode) ----------------
            if (TestMode.Enabled && neutralFac != null)
            {
                StoryQuestDef keepHostileDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryStagesProbeBootstrap.QuestRaidFailedSpawnKeepHostile);
                StoryStage_ColonyRaid keepHostileStage = keepHostileDef != null ? keepHostileDef.stages[0] as StoryStage_ColonyRaid : null;
                if (keepHostileStage != null)
                {
                    ForceNeutralWithGoodwill(neutralFac, -30);
                    keepHostileStage.faction = neutralFac.def.defName;
                    keepHostileStage.restoreRelations = false;

                    StoryStage_ColonyRaid.TestRaidExecuteOverride = delegate(IncidentParms p) { return false; };
                    story.SetFlag("probe.raidFailedKeepHostile", "true");
                    story.DirectorTick();
                    AdvanceTicks(Mathf.CeilToInt(1.5f * GenDate.TicksPerHour));
                    story.DirectorTick();
                    StoryStage_ColonyRaid.TestRaidExecuteOverride = null;

                    StoryQuestRun failRun = story.RunFor(StoryStagesProbeBootstrap.QuestRaidFailedSpawnKeepHostile);
                    bool rolledBackEvenWhenRestoreFalse = failRun != null && HasOutcome(failRun, "timeout") &&
                                                          !neutralFac.HostileTo(Faction.OfPlayer) &&
                                                          !Faction.OfPlayer.HostileTo(neutralFac) &&
                                                          neutralFac.PlayerGoodwill >= -35 && neutralFac.PlayerGoodwill <= -25;
                    AddCheck("R2_FailedRaidSpawn_RollsBackEvenWhenRestoreRelationsFalse",
                        rolledBackEvenWhenRestoreFalse,
                        "timeout & restored Neutral(~-30) despite restoreRelations=false",
                        string.Format("hist={0}, nonHostile={1}, gw={2}",
                            failRun != null ? string.Join(",", failRun.history.ToArray()) : "null",
                            !neutralFac.HostileTo(Faction.OfPlayer), neutralFac.PlayerGoodwill));
                }
            }

            // ---------------- 11. FORCEOUTCOME ON AN ACTIVE RUN + LEGACY VARS["forceOutcome"] TEST ----------------
            story.SetFlag("probe.forceOutcomeActive", "true");
            story.DirectorTick();
            StoryQuestRun activeForceRun = story.RunFor(StoryStagesProbeBootstrap.QuestForceOutcomeActive);
            Site activeForceSite = FindSite(ParseInt(activeForceRun, "siteId", -1));
            bool wasActiveBefore = activeForceRun != null && activeForceRun.status == StoryQuestStatus.Active;

            if (!TestMode.Enabled)
            {
                bool methodRefused = wasActiveBefore && !story.TestForceOutcome(activeForceRun, activeForceRun.gen, "success");
                bool stillActiveAfterMethod = activeForceRun != null && activeForceRun.status == StoryQuestStatus.Active;

                // Separate test of legacy vars["forceOutcome"] key on the ACTIVE run without -tmm-story-test
                if (activeForceRun != null) activeForceRun.vars["forceOutcome"] = "success";
                story.DirectorTick();
                bool legacyRefusedAndCleaned = activeForceRun != null &&
                                               activeForceRun.status == StoryQuestStatus.Active &&
                                               !activeForceRun.vars.ContainsKey("forceOutcome");

                AddCheck("R2_ForceOutcomeAndLegacyVarRefused_OnActiveRun",
                    wasActiveBefore && methodRefused && stillActiveAfterMethod && legacyRefusedAndCleaned,
                    "active=true, TestForceOutcome=false, legacyVar ignored & stripped",
                    string.Format("wasActive={0}, methodRefused={1}, stillActive={2}, legacyCleaned={3}",
                        wasActiveBefore, methodRefused, stillActiveAfterMethod, legacyRefusedAndCleaned));
            }
            else
            {
                // In TestMode: wrong generation must still be refused on an active run, matching generation works
                bool wrongGenRefused = wasActiveBefore && !story.TestForceOutcome(activeForceRun, activeForceRun.gen + 99, "success") &&
                                       activeForceRun.status == StoryQuestStatus.Active;
                bool rightGenWorked = wasActiveBefore && story.TestForceOutcome(activeForceRun, activeForceRun.gen, "timeout") &&
                                      HasOutcome(activeForceRun, "timeout");
                AddCheck("R2_ForceOutcome_ActiveRunGenCheckedInTestMode",
                    wrongGenRefused && rightGenWorked,
                    "wrongGen=false(Active), rightGen=true(timeout)",
                    string.Format("wrongGenRefused={0}, rightGenWorked={1}", wrongGenRefused, rightGenWorked));
            }
            if (activeForceSite != null && !activeForceSite.Destroyed && !activeForceSite.HasMap)
            {
                activeForceSite.Destroy();
            }

            // ---------------- 12. PREPARE RELOAD SAVE: RESCUE + RETRIEVE + PARTIAL HOLD ----------------
            // 12A. Rescue
            story.SetFlag("probe.reloadRescue", "true");
            story.DirectorTick();
            StoryQuestRun rescueRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRescue);
            Site rescueSite = FindSite(ParseInt(rescueRun, "siteId", -1));
            bool rescueReady = false;
            if (rescueSite != null)
            {
                Pawn rScout = SpawnCleanColonist(map);
                EnterSiteWithScout(rScout, rescueSite);
                story.DirectorTick();
                rescueReady = GetVar(rescueRun, "heroImprisonedOnSite") == "true" && !GetVar(rescueRun, "targetPawnId").NullOrEmpty();
            }
            AddCheck("ReloadPrep_RescueCaptiveOnSite", rescueReady, "imprisoned=true", rescueReady.ToString());

            // 12B. Retrieve
            story.SetFlag("probe.reloadRetrieve", "true");
            story.DirectorTick();
            StoryQuestRun retRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadRetrieve);
            Site retSite = FindSite(ParseInt(retRun, "siteId", -1));
            bool retrieveReady = false;
            if (retSite != null)
            {
                Pawn iScout = SpawnCleanColonist(map);
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

            // 12C. Hold with partial confirmed progress
            story.SetFlag("probe.reloadHold", "true");
            story.DirectorTick();
            StoryQuestRun holdRun = story.RunFor(StoryStagesProbeBootstrap.QuestReloadHold);
            Site holdSite = FindSite(ParseInt(holdRun, "siteId", -1));
            if (holdSite != null)
            {
                AdvanceTicks(3 * GenDate.TicksPerHour);
                Pawn hScout = SpawnCleanColonist(map);
                EnterSiteWithScout(hScout, holdSite);
                story.DirectorTick();
                int heldAfterEntry = ParseInt(holdRun, "heldTicks", -1);

                AdvanceTicks(2 * GenDate.TicksPerHour);
                story.DirectorTick();
                int heldAfter2h = ParseInt(holdRun, "heldTicks", -1);

                bool holdPrepOk = heldAfterEntry == 0 && heldAfter2h == 2 * GenDate.TicksPerHour && holdRun.status == StoryQuestStatus.Active;
                AddCheck("P2_Hold_FirstPollZeroAndPartialSaved", holdPrepOk,
                    "afterEntry=0, after2h=5000, active",
                    string.Format("entry={0}, after2h={1}, status={2}", heldAfterEntry, heldAfter2h, holdRun.status));
            }
            else
            {
                AddCheck("P2_Hold_FirstPollZeroAndPartialSaved", false, "site created", "site null");
            }

            // Save directly WITHOUT any global SanitizeDanglingPawnRelations mask!
            GameDataSaveLoader.SaveGame(StoryStagesProbeBootstrap.SaveName); // CHECK API
        }

        // =====================================================================
        // RELOAD PROBE
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
                    DestroyTestCaravanAndPawns(car);
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

                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();
                    AdvanceTicks(StoryStage_Site.RetrieveScanInterval);
                    story.DirectorTick();

                    bool once = CountOutcome(retRun, "success") == 1;
                    AddCheck("Reload_RetrieveCompletesOnce", stillProtected && once,
                        "protected=true, successCount=1",
                        string.Format("protected={0}, count={1}", stillProtected, CountOutcome(retRun, "success")));
                    DestroyTestCaravanAndPawns(car);
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

        private static Zone_Stockpile EnsureTestStockpile(Map targetMap)
        {
            if (targetMap == null || targetMap.zoneManager == null) return null;
            Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, targetMap.zoneManager); // CHECK API
            targetMap.zoneManager.RegisterZone(zone);
            IntVec3 origin = FindStandable(targetMap);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    IntVec3 c = origin + new IntVec3(dx, 0, dz);
                    if (c.InBounds(targetMap) && c.Standable(targetMap) && targetMap.zoneManager.ZoneAt(c) == null)
                    {
                        zone.AddCell(c);
                    }
                }
            }
            return zone.cells.Count > 0 ? zone : null;
        }

        private static TravellingTransporters LaunchTestFlyingPod(PlanetTile originTile, PlanetTile destTile, Thing item)
        {
            if (item == null || Find.WorldObjects == null) return null;
            ActiveTransporterInfo info = new ActiveTransporterInfo();
            info.openDelay = 110;
            info.leaveSlag = false;
            info.innerContainer.TryAdd(item, false);

            TravellingTransporters worldObj = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravellingTransporters); // CHECK API
            worldObj.SetFaction(Faction.OfPlayer);
            worldObj.Tile = originTile;
            worldObj.destinationTile = destTile;
            Find.WorldObjects.Add(worldObj);
            worldObj.AddTransporter(info, true);
            return worldObj;
        }

        private static void ExtractItemFromFlyingPod(TravellingTransporters pods, Thing item)
        {
            if (pods == null || item == null) return;
            if (item.holdingOwner != null)
            {
                item.holdingOwner.Remove(item);
            }
        }

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

        private static Pawn SpawnCleanColonist(Map targetMap)
        {
            PawnGenerationRequest req = new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                canGeneratePawnRelations: false,
                allowAddictions: false); // CHECK API
            Pawn p = PawnGenerator.GeneratePawn(req);
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static Pawn SpawnCleanNpc(Map targetMap, Faction fac)
        {
            PawnKindDef kind = fac != null ? (fac.RandomPawnKind() ?? PawnKindDefOf.SpaceRefugee) : PawnKindDefOf.SpaceRefugee;
            PawnGenerationRequest req = new PawnGenerationRequest(
                kind,
                fac,
                PawnGenerationContext.NonPlayer,
                canGeneratePawnRelations: false,
                allowAddictions: false); // CHECK API
            Pawn p = PawnGenerator.GeneratePawn(req);
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static void CleanupCleanPawn(Pawn p)
        {
            if (p == null) return;
            if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
            if (Find.WorldPawns != null && Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
            if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
        }

        private static Caravan EnterSiteWithScout(Pawn scout, Site site)
        {
            if (scout.Spawned) scout.DeSpawn(DestroyMode.Vanish);
            Caravan c = CaravanMaker.MakeCaravan(new List<Pawn> { scout }, Faction.OfPlayer, site.Tile, true); // CHECK API
            Map siteMap = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null); // CHECK API
            CaravanEnterMapUtility.Enter(c, siteMap, CaravanEnterMode.Edge); // CHECK API
            return c;
        }

        private static void DestroyTestCaravanAndPawns(Caravan car)
        {
            if (car == null) return;
            List<Pawn> pawns = new List<Pawn>(car.PawnsListForReading);
            car.RemoveAllPawns();
            if (!car.Destroyed) car.Destroy();
            for (int i = 0; i < pawns.Count; i++)
            {
                CleanupCleanPawn(pawns[i]);
            }
        }

        private static void CleanupScoutAndSite(Pawn scout, Caravan car, Site site)
        {
            if (car != null && !car.Destroyed) car.Destroy();
            CleanupCleanPawn(scout);
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

        private bool WriteReport(string fileName)
        {
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
            sb.Append(",\"testModeEnabled\":");
            sb.Append(TestMode.Enabled ? "true" : "false");
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
            return allPassed;
        }

        private void WriteUiReport(bool actualPassed)
        {
            try
            {
                string uiPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "StoryStagesUiProbe.json");
                string payload = string.Format(
                    "{{\"passed\":{0},\"checksCount\":{1},\"errorsCount\":{2},\"visualLayoutInspected\":false}}",
                    actualPassed ? "true" : "false",
                    m_checks.Count,
                    m_errors.Count);
                File.WriteAllText(uiPath, payload, Encoding.UTF8);
            }
            catch
            {
            }
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

## Что изменено по каждой находке (Задача 45)

1. **Перенос героя (`StoryStage_Site` — KillHero / Rescue):**
   - Убран ручной перенос (`RemovePawn` / `SetFaction` / `GenSpawn` / `RollbackHeroToWorld`).
   - Предварительная проверка в `Start` идёт через `StoryPawnTransfer.IsFreeWorldPawn`, а сам перенос в `TryPlaceHeroForKill` и `TryPlaceHeroForRescue` — исключительно через `StoryPawnTransfer.TrySpawnFreeWorldPawn` (с `postSpawnSetup` для привязки к враждебному лорду и `imprisonForFaction` для Rescue).
   - Исключение на любом шаге после `RemovePawn` откатывает пешку обратно в `WorldPawns` с исходной фракцией (включая `null`), статусом гостя и лидерством.

2. **Рейд (`StoryStage_ColonyRaid`):**
   - В `SpawnRaid` обязательно требуется непустой подтверждённый состав рейдеров (`trackedIds.Count > 0`), сохраняется `confirmedRaiderCount`.
   - При пустом составе рейдеров (в том числе при исчезнувшем `lordId`) этап завершается строго с `timeout`, а не `success`. `IsRaidClearedSinglePass` также отвергает пустой `trackedSet`.
   - При любом провале спавна (исключение, `TryExecute == false` или `trackedIds.Count == 0`) вызывается `AbortHostilityLease` → `StoryRelationRestorer.Instance.AbortLease(leaseId)`, что откатывает принудительную враждебность **независимо от `restoreRelations`**.
   - Добавлен тестовый хук `TestRaidExecuteOverride`, строго закрытый за `TestMode.Enabled`.
   - Обход `ThingHolder` дополнительно проверяет `ThingComp`-держатели (например, `CompTransporter`).

3. **Retrieve (`StoryStage_Site` — P2):**
   - Введено явное разделение состояний предмета (`PlayerOwned`, `InTransitOrOnSite`, `Lost`):
     - **Доставлен (`PlayerOwned` → `success`):** предмет в караване игрока, либо на домашней карте у пешки игрока (не пленника), в зоне склада/хранилище игрока или контейнере-постройке игрока (не капсуле).
     - **Транзит / ещё не сдан (`InTransitOrOnSite` → ждём):** предмет в летящей транспортной капсуле игрока (`TravellingTransporters`), в `Skyfaller`/`ActiveTransporter`/`CompTransporter`, в инвентаре NPC на домашней карте, на земле вне склада или на карте сайта/другой карте.
     - **Потерян (`Lost` → `failed`):** предмет с точным `ThingID` отсутствует везде.
   - Сканирование караванов, карт, капсул и `ThingHolder` ограничено по времени интервалом `RetrieveScanInterval = 250` тиков (`lastRetrieveScanTick` в `ctx.run.vars`).

4. **Hold (`StoryStage_Site` — P2):**
   - Выбран **частый опрос присутствия каждые 60 тиков** (явный шаг дискретизации `HoldSampleInterval = 60` ≈ 1 сек реального времени на 1x) через легковесный `StorySiteHoldMonitor : MapComponent`.
   - На картах, не являющихся `Site`, компонент мгновенно выходит; на карте `Site` каждые 60 тиков вызывает `StoryStage_Site.SampleHoldPresenceForSiteMap`. Если все колонисты ушли или упали, `holdActive` сразу сбрасывается в `false` с сохранением уже накопленных `heldTicks`, поэтому интервал отсутствия не засчитывается.

5. **Проба (`Tests/InGameStoryStagesProbe.cs`):**
   - Тест `forceOutcome` выполняется на **активном** `StoryQuestRun` (`QuestForceOutcomeActive`), плюс добавлен отдельный тест старого ключа `vars["forceOutcome"]` без `-tmm-story-test`.
   - Тест orphan cleanup выполнен **реальным удалением этапа** (`orphanDef.stages.Clear()` + `story.DirectorTick()`), чтобы `SparkStory.Disable` сработал штатно, без прямой подстановки `status`.
   - Добавлен тест исключения посреди переноса героя через `StoryPawnTransfer.TestSpawnOverride` (при `TestMode.Enabled`) с проверкой возврата в `WorldPawns` и сохранения исходной фракции.
   - Добавлены тесты рейда без подтверждённых рейдеров (`timeout`, не `success`), провала спавна рейда с `restoreRelations=false` (откат враждебности через `AbortLease`), удержания/повтора backup при заблокированном откате, `Retrieve` с NPC на домашней карте и летящей капсулой `TravellingTransporters`, а также `Hold` с паузой отсутствия.
   - `StoryStagesUiProbe.json` пишет `passed` по фактическому результату пробы, а глобальная зачистка `SanitizeDanglingPawnRelations` перед сохранением удалена (тестовые пешки создаются с `canGeneratePawnRelations: false`).

## Изменения публичного API

- Прежний публичный API `StoryStage_Site` и `StoryStage_ColonyRaid` полностью сохранён.
- Добавлены:
  - `StoryStage_Site.RetrieveScanInterval`, `StoryStage_Site.HoldSampleInterval`, `StoryStage_Site.SampleHoldPresenceForSiteMap(Map, int)`.
  - `StorySiteHoldMonitor : MapComponent` (в `Source/StoryStage_Site.cs`).
  - `StoryStage_ColonyRaid.TestRaidExecuteOverride` (работает только при `TestMode.Enabled`).