`Source/StoryStage_Site.cs`

```csharp
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
```

`Defs/StoryDefs/Story_Samples.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_Sample_Site</defName>
    <label>sample outpost sweep</label>
    <act>1</act>
    <main>false</main>
    <priority>50</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <requires>
      <li>story.e2samples=true</li>
    </requires>
    <letterLabel>Sample Outpost Sweep</letterLabel>
    <letterText>Scouts near {COLONY} spotted a hostile field camp. Send a caravan to clear the camp and report back.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>site</key>
        <label>Clear the hostile camp</label>
        <text>A hostile field camp has been marked on the world map near {COLONY}. Send a caravan to eliminate all hostile fighters at the camp.</text>
        <minDays>2</minDays>
        <maxDays>6</maxDays>
        <objective>Clear</objective>
        <timeoutDays>15</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e2sample_siteCleared</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>-1</nextStage> <!-- CHECK: -1 ends quest -->
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e2sample_siteTimeout</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>aftermath</key>
        <label>Outpost secured</label>
        <text>The hostile camp has been cleared. Decide how to log the operation in {COLONY}'s records.</text>
        <choices>
          <li>
            <outcome>claim_supplies</outcome>
            <label>Log victory and collect a small salvage bonus</label>
            <resultText>Your scouts cataloged the cleared camp and brought back a pouch of silver.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>150</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_Sample_Raid</defName>
    <label>sample perimeter probe</label>
    <act>1</act>
    <main>false</main>
    <priority>49</priority>
    <faction>TMM_GunnersFaction</faction>
    <requires>
      <li>story.e2samples=true</li>
    </requires>
    <letterLabel>Sample Perimeter Probe</letterLabel>
    <letterText>Hostile raiders are mobilizing to test {COLONY}'s defenses. Accept to trigger a controlled defensive engagement.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>raid</key>
        <label>Repel the probing raid</label>
        <text>Raiders have been sighted approaching {COLONY}. Prepare your perimeter defenses.</text>
        <pointsFactor>1.0</pointsFactor>
        <delayHours>2</delayHours>
        <timeoutDays>3</timeoutDays>
        <restoreRelations>true</restoreRelations>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e2sample_raidRepelled</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e2sample_raidTimeout</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Samples.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_Sample_Site -->
  <TMM_Story_Sample_Site.label>пробная зачистка лагеря</TMM_Story_Sample_Site.label>
  <TMM_Story_Sample_Site.letterLabel>Пробная зачистка лагеря</TMM_Story_Sample_Site.letterLabel>
  <TMM_Story_Sample_Site.letterText>Разведчики неподалёку от {COLONY} заметили враждебный полевой лагерь. Отправьте караван, зачистите лагерь и доложите о результате.</TMM_Story_Sample_Site.letterText>
  <TMM_Story_Sample_Site.stages.0.label>Зачистить вражеский лагерь</TMM_Story_Sample_Site.stages.0.label>
  <TMM_Story_Sample_Site.stages.0.text>Враждебный полевой лагерь отмечен на карте мира рядом с {COLONY}. Отправьте караван и уничтожьте всех противников в лагере.</TMM_Story_Sample_Site.stages.0.text>
  <TMM_Story_Sample_Site.stages.1.label>Лагерь зачищен</TMM_Story_Sample_Site.stages.1.label>
  <TMM_Story_Sample_Site.stages.1.text>Вражеский лагерь полностью зачищен. Решите, как оформить итог операции в архивах {COLONY}.</TMM_Story_Sample_Site.stages.1.text>
  <TMM_Story_Sample_Site.stages.1.choices.0.label>Зафиксировать победу и забрать скромные трофеи</TMM_Story_Sample_Site.stages.1.choices.0.label>
  <TMM_Story_Sample_Site.stages.1.choices.0.resultText>Разведчики описали зачищенный лагерь и принесли мешочек серебра.</TMM_Story_Sample_Site.stages.1.choices.0.resultText>

  <!-- TMM_Story_Sample_Raid -->
  <TMM_Story_Sample_Raid.label>пробная атака на периметр</TMM_Story_Sample_Raid.label>
  <TMM_Story_Sample_Raid.letterLabel>Пробная атака на периметр</TMM_Story_Sample_Raid.letterLabel>
  <TMM_Story_Sample_Raid.letterText>Враждебные налётчики готовятся проверить оборону {COLONY}. Примите вызов, чтобы провести учебное отражение рейда.</TMM_Story_Sample_Raid.letterText>
  <TMM_Story_Sample_Raid.stages.0.label>Отразить пробный рейд</TMM_Story_Sample_Raid.stages.0.label>
  <TMM_Story_Sample_Raid.stages.0.text>К {COLONY} приближаются налётчики. Подготовьте оборону периметра.</TMM_Story_Sample_Raid.stages.0.text>

</LanguageData>
```

`Tests/InGameStoryStagesProbe.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
                if (ReloadMode && Current.ProgramState == ProgramState.Entry)
                {
                    GameDataSaveLoader.LoadGame(SaveName); // CHECK API
                }
            });
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
    /// In-game verification probe for StoryStage_Site (Clear, Timeout, Save/Reload) and StoryStage_ColonyRaid.
    /// Activated via -tmm-story-stages-probe (fresh) or -tmm-story-stages-reload (reload).
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

        private void RunFreshProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("SparkStory_Exists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // 1. Enable sample quests and trigger offer
            story.SetFlag("story.e2samples", "true"); // CHECK API
            story.DirectorTick(); // CHECK API

            StoryQuestDef siteQuestDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Site");
            StoryQuestDef raidQuestDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Raid");
            AddCheck("SampleDefs_Loaded", siteQuestDef != null && raidQuestDef != null, "both loaded",
                string.Format("site={0}, raid={1}", siteQuestDef != null, raidQuestDef != null));
            if (siteQuestDef == null || raidQuestDef == null) return;

            // ---------------- TEST 1: SITE CLEAR -> CHOICE ----------------
            bool acceptedSite = AcceptOfferLetter(siteQuestDef.letterLabel);
            AddCheck("Site_OfferAccepted", acceptedSite, "true", acceptedSite.ToString());
            story.DirectorTick();

            StoryQuestRun siteRun = FindQuestRun(story, "TMM_Story_Sample_Site");
            int siteId = siteRun != null ? ParseInt(siteRun.vars, "siteId", -1) : -1;
            Site site = FindSite(siteId);
            AddCheck("Site_CreatedAndStored", site != null && siteId >= 0, "valid Site in vars[siteId]",
                string.Format("siteId={0}, found={1}", siteId, site != null));

            if (site != null)
            {
                StoryStage_Site siteStage = siteQuestDef.stages[0] as StoryStage_Site;
                float distTiles = Find.WorldGrid.TraversalDistanceBetween(map.Tile, site.Tile); // CHECK API
                float minTiles = siteStage.minDays * StoryStage_Site.TilesPerDay;
                float maxTiles = siteStage.maxDays * StoryStage_Site.TilesPerDay;
                bool distOk = distTiles >= minTiles && distTiles <= maxTiles;
                AddCheck("Site_DistanceInRange", distOk,
                    string.Format("{0:0.0}..{1:0.0} tiles", minTiles, maxTiles),
                    string.Format("{0:0.0} tiles", distTiles));

                // Enter site via caravan
                Pawn scout = SpawnTempScout();
                Caravan caravan = SendScoutToSiteMap(scout, site);
                AddCheck("Site_MapGenerated", site.HasMap && site.Map != null, "HasMap=true",
                    string.Format("HasMap={0}", site.HasMap));

                story.DirectorTick();

                if (site.HasMap && site.Map != null)
                {
                    int killed = KillAllHostilePawns(site.Map);
                    AddCheck("Site_EnemiesKilled", killed >= 0, ">=0 killed", killed.ToString());
                }

                story.DirectorTick();

                bool siteSucceeded = siteRun != null && siteRun.stage == 1 && HasHistoryEntry(siteRun, "success");
                AddCheck("Site_ClearSuccessToStage1", siteSucceeded, "stage=1, history=success",
                    siteRun != null ? string.Format("stage={0}, hist={1}", siteRun.stage, string.Join(",", siteRun.history.ToArray())) : "null");

                ChoiceLetter choiceLetter = FindChoiceLetterByLabel(siteQuestDef.stages[1].label);
                AddCheck("Site_NextChoiceLetterArrived", choiceLetter != null, "ChoiceLetter found", choiceLetter != null ? "found" : "missing");
                if (choiceLetter != null)
                {
                    ClickFirstChoice(choiceLetter);
                    story.DirectorTick();
                }

                CleanupTempScoutAndSite(scout, caravan, site);
            }

            // ---------------- TEST 2: SITE TIMEOUT ----------------
            StoryStage_Site stageForTimeout = siteQuestDef.stages[0] as StoryStage_Site;
            float savedTimeoutDays = stageForTimeout != null ? stageForTimeout.timeoutDays : 15f;
            try
            {
                if (stageForTimeout != null) stageForTimeout.timeoutDays = 0.1f;
                ResetQuestRun(story, "TMM_Story_Sample_Site");
                story.DirectorTick();

                bool acceptedTimeout = AcceptOfferLetter(siteQuestDef.letterLabel);
                AddCheck("SiteTimeout_OfferAccepted", acceptedTimeout, "true", acceptedTimeout.ToString());
                story.DirectorTick();

                StoryQuestRun timeoutRun = FindQuestRun(story, "TMM_Story_Sample_Site");
                int timeoutSiteId = timeoutRun != null ? ParseInt(timeoutRun.vars, "siteId", -1) : -1;
                Site timeoutSite = FindSite(timeoutSiteId);
                AddCheck("SiteTimeout_SiteCreated", timeoutSite != null, "Site exists", timeoutSite != null ? "exists" : "null");

                AdvanceTicks(Mathf.CeilToInt(0.15f * GenDate.TicksPerDay));
                story.DirectorTick();

                bool timedOut = timeoutRun != null && HasHistoryEntry(timeoutRun, "timeout");
                bool siteDeleted = FindSite(timeoutSiteId) == null;
                AddCheck("SiteTimeout_OutcomeTimeout", timedOut, "timeout",
                    timeoutRun != null ? string.Join(",", timeoutRun.history.ToArray()) : "null");
                AddCheck("SiteTimeout_SiteRemoved", siteDeleted, "site removed", siteDeleted ? "removed" : "still exists");
            }
            finally
            {
                if (stageForTimeout != null) stageForTimeout.timeoutDays = savedTimeoutDays;
            }

            // ---------------- TEST 3: COLONY RAID ----------------
            story.DirectorTick();
            bool acceptedRaid = AcceptOfferLetter(raidQuestDef.letterLabel);
            AddCheck("Raid_OfferAccepted", acceptedRaid, "true", acceptedRaid.ToString());
            story.DirectorTick();

            StoryQuestRun raidRun = FindQuestRun(story, "TMM_Story_Sample_Raid");
            StoryStage_ColonyRaid raidStage = raidQuestDef.stages[0] as StoryStage_ColonyRaid;
            float delayHrs = raidStage != null ? raidStage.delayHours : 2f;

            AdvanceTicks(Mathf.CeilToInt((delayHrs + 0.5f) * GenDate.TicksPerHour));
            story.DirectorTick();

            bool raidSpawned = raidRun != null && GetVar(raidRun, "spawned") == "true";
            int raidEnemiesFound = CountTrackedOrHostilePawns(map, raidRun);
            AddCheck("Raid_EnemiesSpawnedAfterDelay", raidSpawned && raidEnemiesFound > 0, "spawned=true, enemies>0",
                string.Format("spawned={0}, enemies={1}", raidSpawned, raidEnemiesFound));

            int raidKilled = KillTrackedOrHostilePawns(map, raidRun);
            AddCheck("Raid_EnemiesKilled", raidKilled > 0, ">0", raidKilled.ToString());

            story.DirectorTick();
            bool raidSucceeded = raidRun != null && HasHistoryEntry(raidRun, "success");
            AddCheck("Raid_OutcomeSuccess", raidSucceeded, "success",
                raidRun != null ? string.Join(",", raidRun.history.ToArray()) : "null");

            // ---------------- TEST 4: PREPARE SAVE FOR RELOAD ----------------
            ResetQuestRun(story, "TMM_Story_Sample_Site");
            story.DirectorTick();
            bool acceptedForSave = AcceptOfferLetter(siteQuestDef.letterLabel);
            story.DirectorTick();

            StoryQuestRun saveRun = FindQuestRun(story, "TMM_Story_Sample_Site");
            int saveSiteId = saveRun != null ? ParseInt(saveRun.vars, "siteId", -1) : -1;
            bool readyForSave = acceptedForSave && FindSite(saveSiteId) != null;
            AddCheck("ReloadPrep_ActiveSiteSaved", readyForSave, "active site before save",
                string.Format("accepted={0}, siteId={1}", acceptedForSave, saveSiteId));

            GameDataSaveLoader.SaveGame(StoryStagesProbeBootstrap.SaveName); // CHECK API
        }

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            StoryQuestDef siteQuestDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Site");
            StoryQuestRun siteRun = FindQuestRun(story, "TMM_Story_Sample_Site");
            AddCheck("Reload_RunRestored", siteRun != null && siteRun.stage == 0, "stage=0",
                siteRun != null ? "stage=" + siteRun.stage : "null");
            if (siteRun == null || siteQuestDef == null) return;

            int siteId = ParseInt(siteRun.vars, "siteId", -1);
            Site site = FindSite(siteId);
            AddCheck("Reload_SiteFoundById", site != null, "Site exists",
                string.Format("siteId={0}, found={1}", siteId, site != null));
            if (site == null) return;

            Pawn scout = SpawnTempScout();
            Caravan caravan = SendScoutToSiteMap(scout, site);
            AddCheck("Reload_EnteredSiteMap", site.HasMap && site.Map != null, "HasMap=true",
                string.Format("HasMap={0}", site.HasMap));

            story.DirectorTick();

            if (site.HasMap && site.Map != null)
            {
                KillAllHostilePawns(site.Map);
            }

            story.DirectorTick();
            bool succeededOnce = siteRun.stage == 1 && CountHistoryEntries(siteRun, "success") == 1;
            AddCheck("Reload_SiteCompletesOnce", succeededOnce, "stage=1, successCount=1",
                string.Format("stage={0}, successCount={1}", siteRun.stage, CountHistoryEntries(siteRun, "success")));

            // Tick several more times to ensure the stage does not re-trigger
            story.DirectorTick();
            story.DirectorTick();
            story.DirectorTick();

            bool stillOnce = siteRun.stage == 1 && CountHistoryEntries(siteRun, "success") == 1;
            AddCheck("Reload_NoDuplicateTrigger", stillOnce, "stage=1, successCount=1",
                string.Format("stage={0}, successCount={1}", siteRun.stage, CountHistoryEntries(siteRun, "success")));

            CleanupTempScoutAndSite(scout, caravan, site);
        }

        private bool AcceptOfferLetter(string expectedLabel)
        {
            ChoiceLetter letter = FindChoiceLetterByLabel(expectedLabel);
            if (letter == null) return false;
            return ClickFirstChoice(letter);
        }

        private static ChoiceLetter FindChoiceLetterByLabel(string expectedLabel)
        {
            if (Find.LetterStack == null) return null;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                ChoiceLetter cl = letters[i] as ChoiceLetter;
                if (cl == null) continue;
                if (expectedLabel.NullOrEmpty() || string.Equals(cl.Label.ToString(), expectedLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return cl;
                }
            }
            return null;
        }

        private static bool ClickFirstChoice(ChoiceLetter letter)
        {
            if (letter == null) return false;
            IEnumerable<DiaOption> choices = letter.Choices; // CHECK API
            if (choices == null) return false;

            foreach (DiaOption opt in choices)
            {
                if (opt != null && opt.action != null && !opt.disabled)
                {
                    opt.action();
                    if (Find.LetterStack != null) Find.LetterStack.RemoveLetter(letter);
                    return true;
                }
            }
            return false;
        }

        private Pawn SpawnTempScout()
        {
            Pawn scout = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 10, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map);
            }, out cell))
            {
                cell = map.Center;
            }
            GenSpawn.Spawn(scout, cell, map, WipeMode.Vanish);
            return scout;
        }

        private static Caravan SendScoutToSiteMap(Pawn scout, Site site)
        {
            if (scout.Spawned) scout.DeSpawn(DestroyMode.Vanish);
            List<Pawn> list = new List<Pawn>();
            list.Add(scout);
            Caravan caravan = CaravanMaker.MakeCaravan(list, Faction.OfPlayer, site.Tile, true); // CHECK API
            Map siteMap = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null); // CHECK API
            CaravanEnterMapUtility.Enter(caravan, siteMap, CaravanEnterMode.Edge); // CHECK API
            return caravan;
        }

        private static void CleanupTempScoutAndSite(Pawn scout, Caravan caravan, Site site)
        {
            if (scout != null && !scout.Destroyed)
            {
                if (scout.Spawned) scout.DeSpawn(DestroyMode.Vanish);
                scout.Destroy(DestroyMode.Vanish);
            }
            if (caravan != null && !caravan.Destroyed)
            {
                caravan.Destroy();
            }
            if (site != null && !site.Destroyed)
            {
                if (site.HasMap && site.Map != null)
                {
                    Current.Game.DeinitAndRemoveMap(site.Map, false); // CHECK API
                }
                if (!site.Destroyed) site.Destroy();
            }
        }

        private static int KillAllHostilePawns(Map targetMap)
        {
            int count = 0;
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && !p.Dead && !p.Destroyed && p.HostileTo(Faction.OfPlayer))
                {
                    p.Kill(null);
                    count++;
                }
            }
            return count;
        }

        private static int CountTrackedOrHostilePawns(Map targetMap, StoryQuestRun run)
        {
            int count = 0;
            List<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && !p.Destroyed && p.HostileTo(Faction.OfPlayer))
                {
                    count++;
                }
            }
            if (count == 0 && run != null)
            {
                string raw = GetVar(run, "raidPawnIds");
                if (!raw.NullOrEmpty())
                {
                    count = raw.Split(',').Length;
                }
            }
            return count;
        }

        private static int KillTrackedOrHostilePawns(Map targetMap, StoryQuestRun run)
        {
            int count = 0;
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && !p.Dead && !p.Destroyed && p.HostileTo(Faction.OfPlayer))
                {
                    p.Kill(null);
                    count++;
                }
            }
            return count;
        }

        private static void AdvanceTicks(int deltaTicks)
        {
            if (deltaTicks <= 0) return;
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + deltaTicks); // CHECK API
        }

        private static StoryQuestRun FindQuestRun(SparkStory story, string questDefName)
        {
            // Uses reflection fallback if SparkStory stores runs in a private/public collection.
            // CHECK API: verify exact SparkStory run lookup method (e.g. story.GetRun(questDefName) or story.runs).
            Type t = story.GetType();
            MethodInfo getRun = t.GetMethod("GetRun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getRun != null)
            {
                return getRun.Invoke(story, new object[] { questDefName }) as StoryQuestRun;
            }

            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField != null)
            {
                object val = runsField.GetValue(story);
                Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
                if (dict != null)
                {
                    StoryQuestRun r;
                    if (dict.TryGetValue(questDefName, out r)) return r;
                }
                List<StoryQuestRun> list = val as List<StoryQuestRun>;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && list[i].quest == questDefName) return list[i];
                    }
                }
            }
            return null;
        }

        private static void ResetQuestRun(SparkStory story, string questDefName)
        {
            // CHECK API: remove previous run from SparkStory so the sample quest can be offered again.
            Type t = story.GetType();
            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField == null) return;

            object val = runsField.GetValue(story);
            Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
            if (dict != null)
            {
                dict.Remove(questDefName);
                return;
            }
            List<StoryQuestRun> list = val as List<StoryQuestRun>;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] != null && list[i].quest == questDefName) list.RemoveAt(i);
                }
            }
        }

        private static Site FindSite(int siteId)
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

        private static bool HasHistoryEntry(StoryQuestRun run, string outcome)
        {
            return CountHistoryEntries(run, outcome) > 0;
        }

        private static int CountHistoryEntries(StoryQuestRun run, string outcome)
        {
            if (run == null || run.history == null) return 0;
            int count = 0;
            string suffix = ":" + outcome;
            for (int i = 0; i < run.history.Count; i++)
            {
                string h = run.history[i];
                if (h != null && (h == outcome || h.EndsWith(suffix, StringComparison.Ordinal)))
                {
                    count++;
                }
            }
            return count;
        }

        private static int ParseInt(Dictionary<string, string> vars, string key, int fallback)
        {
            if (vars == null) return fallback;
            string raw;
            int val;
            if (vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
        }

        private static string GetVar(StoryQuestRun run, string key)
        {
            if (run == null || run.vars == null) return string.Empty;
            string raw;
            return run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
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