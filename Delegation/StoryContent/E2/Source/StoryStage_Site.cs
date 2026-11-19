using System;
using System.Collections.Generic;
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
    /// (Clear, Rescue, Retrieve, Hold, KillHero) or timeout. All runtime state lives in ctx.run.vars.
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
            ctx.run.vars["lastHoldCheckTick"] = now.ToString();

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

            Site site = SiteMaker.MakeSite(parts, siteTile, fac, true); // CHECK API
            if (site == null)
            {
                Log.Warning("[TMM] StoryStage_Site: SiteMaker.MakeSite returned null.");
                ctx.run.vars["siteId"] = "-1";
                return;
            }

            TimeoutComp timeoutComp = site.GetComponent<TimeoutComp>();
            if (timeoutComp != null)
            {
                // Give RimWorld's own TimeoutComp a buffer so StoryStage_Site handles timeout deterministically.
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
            int deadline = GetInt(ctx, "deadlineTick", now + GenDate.TicksPerDay);
            int siteId = GetInt(ctx, "siteId", -1);
            Site site = FindSiteById(siteId);

            if (site != null && site.HasMap && site.Map != null)
            {
                ctx.run.vars["enteredMap"] = "true";
                if (!GetBool(ctx, "mapInitialized"))
                {
                    InitializeSiteMap(ctx, site);
                    ctx.run.vars["mapInitialized"] = "true";
                }
            }

            string objOutcome = EvaluateObjective(ctx, site, now);
            if (!objOutcome.NullOrEmpty())
            {
                ctx.run.vars["finished"] = objOutcome;
                return objOutcome;
            }

            if (site == null)
            {
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            if (now >= deadline)
            {
                RemoveSiteSafely(site);
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
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

        private string DefaultObjectiveText()
        {
            if (objective == StorySiteObjective.Clear) return "Clear hostile site";
            if (objective == StorySiteObjective.Rescue) return "Rescue captive hero";
            if (objective == StorySiteObjective.Retrieve) return "Retrieve target item";
            if (objective == StorySiteObjective.Hold) return "Hold site";
            if (objective == StorySiteObjective.KillHero) return "Eliminate target hero";
            return "Complete site objective";
        }

        private void InitializeSiteMap(StoryContext ctx, Site site)
        {
            Map map = site.Map;
            if (map == null) return;

            if (objective == StorySiteObjective.KillHero && hero != null)
            {
                Pawn target = HeroRegistry.Instance.GetOrCreate(hero);
                if (target != null && !target.Dead)
                {
                    if (target.Spawned) target.DeSpawn(DestroyMode.Vanish);
                    if (site.Faction != null && target.Faction != site.Faction)
                    {
                        target.SetFaction(site.Faction);
                    }
                    IntVec3 cell = FindSpawnCellNearCenter(map);
                    GenSpawn.Spawn(target, cell, map, WipeMode.Vanish);
                    AttachToHostileLord(map, site.Faction, target, cell);
                    ctx.run.vars["targetPawnId"] = target.ThingID;
                    ctx.run.vars["heroSpawned"] = "true";
                }
            }
            else if (objective == StorySiteObjective.Rescue && hero != null)
            {
                Pawn captive = HeroRegistry.Instance.GetOrCreate(hero);
                if (captive != null && !captive.Dead)
                {
                    if (captive.IsCaravanMember())
                    {
                        ctx.run.vars["targetPawnId"] = captive.ThingID;
                        ctx.run.vars["heroSpawned"] = "true";
                        return;
                    }
                    if (captive.Spawned) captive.DeSpawn(DestroyMode.Vanish);
                    if (site.Faction != null && captive.Faction == site.Faction)
                    {
                        captive.SetFaction(null);
                    }
                    IntVec3 cell = FindSpawnCellNearCenter(map);
                    GenSpawn.Spawn(captive, cell, map, WipeMode.Vanish);
                    if (captive.guest != null && site.Faction != null)
                    {
                        captive.guest.SetGuestStatus(site.Faction, GuestStatus.Prisoner); // CHECK API
                    }
                    ctx.run.vars["targetPawnId"] = captive.ThingID;
                    ctx.run.vars["heroSpawned"] = "true";
                }
            }
            else if (objective == StorySiteObjective.Retrieve && thing != null)
            {
                ThingDef stuff = thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null;
                Thing item = ThingMaker.MakeThing(thing, stuff);
                IntVec3 cell = FindSpawnCellNearCenter(map);
                GenPlace.TryPlaceThing(item, cell, map, ThingPlaceMode.Near);
                ctx.run.vars["targetThingId"] = item.ThingID;
                ctx.run.vars["thingSpawned"] = "true";
            }
        }

        private string EvaluateObjective(StoryContext ctx, Site site, int now)
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
                string pawnId = GetStr(ctx, "targetPawnId");
                if (pawnId.NullOrEmpty() && hero != null)
                {
                    Pawn existing = HeroRegistry.Instance.GetOrCreate(hero);
                    if (existing != null)
                    {
                        pawnId = existing.ThingID;
                        ctx.run.vars["targetPawnId"] = pawnId;
                    }
                }
                if (IsPawnDeadById(pawnId, site != null ? site.Map : null))
                {
                    return "success";
                }
                if (GetBool(ctx, "heroSpawned") && hero != null)
                {
                    Pawn alive = HeroRegistry.Instance.GetOrCreate(hero);
                    if (alive == null || alive.Dead)
                    {
                        return "success";
                    }
                }
                return null;
            }

            if (objective == StorySiteObjective.Rescue)
            {
                string pawnId = GetStr(ctx, "targetPawnId");
                if (pawnId.NullOrEmpty() && hero != null)
                {
                    Pawn existing = HeroRegistry.Instance.GetOrCreate(hero);
                    if (existing != null)
                    {
                        pawnId = existing.ThingID;
                        ctx.run.vars["targetPawnId"] = pawnId;
                    }
                }
                if (IsPawnDeadById(pawnId, site != null ? site.Map : null))
                {
                    return "failed";
                }
                if (IsPawnInPlayerCaravan(pawnId) || IsPawnRescuedAtHome(pawnId, ctx.Home))
                {
                    return "success";
                }
                if (GetBool(ctx, "heroSpawned") && hero != null)
                {
                    Pawn alive = HeroRegistry.Instance.GetOrCreate(hero);
                    if (alive == null || alive.Dead)
                    {
                        return "failed";
                    }
                }
                return null;
            }

            if (objective == StorySiteObjective.Retrieve)
            {
                if (!GetBool(ctx, "enteredMap")) return null;
                string thingId = GetStr(ctx, "targetThingId");
                bool stillOnSiteMap = site != null && site.HasMap && site.Map != null && IsThingOnMap(site.Map, thingId);
                if (IsTargetThingInPlayerCaravan(thingId, thing, !stillOnSiteMap))
                {
                    return "success";
                }
                return null;
            }

            if (objective == StorySiteObjective.Hold)
            {
                int lastTick = GetInt(ctx, "lastHoldCheckTick", now);
                int dt = Mathf.Max(0, now - lastTick);
                ctx.run.vars["lastHoldCheckTick"] = now.ToString();

                if (site != null && site.HasMap && site.Map != null && HasStandingPlayerPawns(site.Map))
                {
                    int held = GetInt(ctx, "heldTicks", 0) + dt;
                    ctx.run.vars["heldTicks"] = held.ToString();
                    int targetTicks = Mathf.Max(1, Mathf.RoundToInt(holdHours * GenDate.TicksPerHour));
                    if (held >= targetTicks)
                    {
                        return "success";
                    }
                }
                return null;
            }

            return null;
        }

        private static bool HasLivingHostilePawns(Map map)
        {
            if (map == null || map.mapPawns == null) return false;
            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
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
            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && !p.Downed && !p.Destroyed) return true;
            }
            return false;
        }

        private static bool IsPawnDeadById(string pawnId, Map siteMap)
        {
            if (pawnId.NullOrEmpty()) return false;

            if (siteMap != null)
            {
                List<Thing> corpses = siteMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                for (int i = 0; i < corpses.Count; i++)
                {
                    Corpse c = corpses[i] as Corpse;
                    if (c != null && c.InnerPawn != null && c.InnerPawn.ThingID == pawnId) return true;
                }
            }

            List<Pawn> deadWorld = Find.WorldPawns.AllPawnsDead;
            for (int i = 0; i < deadWorld.Count; i++)
            {
                Pawn p = deadWorld[i];
                if (p != null && p.ThingID == pawnId) return true;
            }

            return false;
        }

        private static bool IsPawnInPlayerCaravan(string pawnId)
        {
            if (pawnId.NullOrEmpty()) return false;
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Faction != Faction.OfPlayer) continue;
                List<Pawn> pawns = c.PawnsListForReading;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn p = pawns[j];
                    if (p != null && !p.Dead && p.ThingID == pawnId) return true;
                }
            }
            return false;
        }

        private static bool IsPawnRescuedAtHome(string pawnId, Map home)
        {
            if (pawnId.NullOrEmpty() || home == null || home.mapPawns == null) return false;
            List<Pawn> pawns = home.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && p.ThingID == pawnId && (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsThingOnMap(Map map, string thingId)
        {
            if (map == null || thingId.NullOrEmpty()) return false;
            List<Thing> all = map.listerThings.AllThings;
            for (int i = 0; i < all.Count; i++)
            {
                Thing t = all[i];
                if (t != null && !t.Destroyed && t.ThingID == thingId) return true;
            }
            return false;
        }

        private static bool IsTargetThingInPlayerCaravan(string thingId, ThingDef targetDef, bool allowDefMatch)
        {
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Faction != Faction.OfPlayer) continue;

                List<Thing> items = CaravanInventoryUtility.AllInventoryItems(c); // CHECK API
                if (items != null)
                {
                    for (int j = 0; j < items.Count; j++)
                    {
                        Thing t = items[j];
                        if (t == null || t.Destroyed) continue;
                        if (!thingId.NullOrEmpty() && t.ThingID == thingId) return true;
                        if (allowDefMatch && targetDef != null && t.def == targetDef && t.stackCount > 0) return true;
                    }
                }

                List<Pawn> pawns = c.PawnsListForReading;
                for (int pIdx = 0; pIdx < pawns.Count; pIdx++)
                {
                    Pawn p = pawns[pIdx];
                    if (p == null) continue;
                    if (p.equipment != null && p.equipment.Primary != null)
                    {
                        Thing eq = p.equipment.Primary;
                        if (!thingId.NullOrEmpty() && eq.ThingID == thingId) return true;
                        if (allowDefMatch && targetDef != null && eq.def == targetDef) return true;
                    }
                }
            }
            return false;
        }

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

        private static void RemoveSiteSafely(Site site)
        {
            if (site == null || site.Destroyed) return;
            if (site.HasMap && site.Map != null)
            {
                if (HasStandingPlayerPawns(site.Map)) return;
            }
            site.Destroy();
        }

        private static Site FindSiteById(int siteId)
        {
            if (siteId < 0) return null;
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
                    return !Find.WorldObjects.AnyWorldObjectAt(t) && TileFinder.IsValidTileForNewSettlement(t, null);
                })) // CHECK API
                {
                    return true;
                }
            }
            return TileFinder.TryFindNewSiteTile(out tile, minTiles, maxTiles, false, TileFinderMode.Near, rootTile, true); // CHECK API
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
            SitePartDef outpost = SitePartDefOf.Outpost; // CHECK API
            if (outpost != null) return outpost;
            return DefDatabase<SitePartDef>.GetNamedSilentFail("Outpost");
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
