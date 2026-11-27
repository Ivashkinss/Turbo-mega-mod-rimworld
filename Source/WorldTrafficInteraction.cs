using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class WorldTrafficInteraction
    {
        private static readonly FieldInfo TraderPawnField = AccessTools.Field(typeof(Pawn_TraderTracker), "pawn"); // CHECK API
        private static readonly HashSet<int> EngagedCaravanIds = new HashSet<int>();
        private static int lastWorldToken;

        private static Texture2D tradeIcon;
        private static Texture2D attackIcon;

        static WorldTrafficInteraction()
        {
            var harmony = new Harmony("colonyproject.integrated.traffic.interaction");

            PatchSafe(harmony,
                AccessTools.Method(typeof(Caravan), "GetGizmos"),
                null,
                new HarmonyMethod(typeof(WorldTrafficInteraction), "Caravan_GetGizmos_Postfix"),
                "Caravan.GetGizmos");

            PatchSafe(harmony,
                AccessTools.PropertyGetter(typeof(Pawn_TraderTracker), "CanTradeNow"), // CHECK API
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TraderTracker_CanTradeNow_Prefix"),
                null,
                "Pawn_TraderTracker.get_CanTradeNow");

            PatchSafe(harmony,
                AccessTools.PropertyGetter(typeof(Pawn_TraderTracker), "Goods"), // CHECK API
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TraderTracker_Goods_Prefix"),
                null,
                "Pawn_TraderTracker.get_Goods");

            PatchSafe(harmony,
                AccessTools.Method(typeof(Pawn_TraderTracker), "ColonyThingsWillingToBuy", new Type[] { typeof(Pawn) }), // CHECK API
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TraderTracker_ColonyThingsWillingToBuy_Prefix"),
                null,
                "Pawn_TraderTracker.ColonyThingsWillingToBuy");

            PatchSafe(harmony,
                AccessTools.Method(typeof(Pawn_TraderTracker), "GiveSoldThingToTrader", new Type[] { typeof(Thing), typeof(int), typeof(Pawn) }), // CHECK API
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TraderTracker_GiveSoldThingToTrader_Prefix"),
                null,
                "Pawn_TraderTracker.GiveSoldThingToTrader");

            PatchSafe(harmony,
                AccessTools.Method(typeof(Pawn_TraderTracker), "GiveSoldThingToPlayer", new Type[] { typeof(Thing), typeof(int), typeof(Pawn) }), // CHECK API
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TraderTracker_GiveSoldThingToPlayer_Prefix"),
                null,
                "Pawn_TraderTracker.GiveSoldThingToPlayer");

            PatchSafe(harmony,
                AccessTools.Method(typeof(TradeDeal), "TryExecute"),
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TradeDeal_TryExecute_Prefix"),
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TradeDeal_TryExecute_Postfix"),
                "TradeDeal.TryExecute",
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TradeDeal_TryExecute_Finalizer"));

            PatchSafe(harmony,
                AccessTools.Method(typeof(TradeSession), "SetupWith"),
                null,
                new HarmonyMethod(typeof(WorldTrafficInteraction), "TradeSession_SetupWith_Postfix"),
                "TradeSession.SetupWith");
        }

        private static void PatchSafe(Harmony harmony, MethodBase target, HarmonyMethod prefix, HarmonyMethod postfix, string label, HarmonyMethod finalizer = null)
        {
            if (target == null)
            {
                Log.Warning("[Turbo mega mod] WorldTrafficInteraction could not find target method: " + label);
                return;
            }
            harmony.Patch(target, prefix, postfix, null, finalizer);
        }

        private static void SyncWorldState()
        {
            World world = Find.World;
            int token = world == null ? 0 : RuntimeHelpers.GetHashCode(world);
            if (token != lastWorldToken)
            {
                lastWorldToken = token;
                EngagedCaravanIds.Clear();
            }
        }

        private static Texture2D GetTradeIcon()
        {
            if (tradeIcon == null)
            {
                tradeIcon = ContentFinder<Texture2D>.Get("UI/Commands/Trade", false);
                if (tradeIcon == null) tradeIcon = TexButton.Info;
            }
            return tradeIcon;
        }

        private static Texture2D GetAttackIcon()
        {
            if (attackIcon == null)
            {
                attackIcon = ContentFinder<Texture2D>.Get("UI/Commands/AttackSettlement", false);
                if (attackIcon == null) attackIcon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", false);
                if (attackIcon == null) attackIcon = TexButton.Info;
            }
            return attackIcon;
        }

        private static WorldTraffic GetTrafficComponent()
        {
            return Find.World == null ? null : Find.World.GetComponent<WorldTraffic>();
        }

        public static bool IsEngaged(Caravan trafficCaravan)
        {
            SyncWorldState();
            return trafficCaravan == null || EngagedCaravanIds.Contains(trafficCaravan.ID);
        }

        public static bool IsValidTrafficCaravan(Caravan trafficCaravan)
        {
            SyncWorldState();
            if (trafficCaravan == null || !trafficCaravan.Spawned || trafficCaravan.Destroyed) return false;
            if (trafficCaravan.Faction == null || trafficCaravan.Faction == Faction.OfPlayer) return false;
            if (EngagedCaravanIds.Contains(trafficCaravan.ID)) return false;

            WorldTraffic traffic = GetTrafficComponent();
            if (traffic == null || traffic.Route(trafficCaravan) == null) return false;

            return CountLivingMembers(trafficCaravan) > 0;
        }

        public static bool IsSameTilePlayerAndTraffic(Caravan playerCaravan, Caravan trafficCaravan)
        {
            if (playerCaravan == null || !playerCaravan.Spawned || playerCaravan.Destroyed) return false;
            if (playerCaravan.Faction != Faction.OfPlayer) return false;
            if (!IsValidTrafficCaravan(trafficCaravan)) return false;
            if (trafficCaravan == playerCaravan) return false;
            return playerCaravan.Tile.Valid && playerCaravan.Tile == trafficCaravan.Tile;
        }

        public static List<Caravan> GetInteractableTrafficCaravans(Caravan playerCaravan)
        {
            List<Caravan> result = new List<Caravan>();
            SyncWorldState();
            if (playerCaravan == null || !playerCaravan.Spawned || playerCaravan.Destroyed || playerCaravan.Faction != Faction.OfPlayer)
                return result;
            if (!playerCaravan.Tile.Valid || Find.WorldObjects == null)
                return result;

            WorldTraffic traffic = GetTrafficComponent();
            if (traffic == null || traffic.routes == null || traffic.routes.Count == 0)
                return result;

            List<WorldTrafficRoute> routes = traffic.routes;
            for (int i = 0; i < routes.Count; i++)
            {
                WorldTrafficRoute route = routes[i];
                if (route == null) continue;
                Caravan npc = route.caravan;
                if (IsSameTilePlayerAndTraffic(playerCaravan, npc) && !result.Contains(npc))
                    result.Add(npc);
            }
            return result;
        }

        public static int CountLivingMembers(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed) return 0;
            int count = 0;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Destroyed && !p.Dead) count++;
            }
            return count;
        }

        public static int CountLivingHumanlikeMembers(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed) return 0;
            int count = 0;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Destroyed && !p.Dead && p.RaceProps != null && p.RaceProps.Humanlike) count++;
            }
            return count;
        }

        public static float ComputeCargoMass(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed) return 0f;
            float totalMass = 0f;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Destroyed || p.Dead || p.inventory == null || p.inventory.innerContainer == null) continue;
                IList<Thing> items = p.inventory.innerContainer.InnerListForReading;
                for (int j = 0; j < items.Count; j++)
                {
                    Thing item = items[j];
                    if (item == null || item.Destroyed) continue;
                    totalMass += item.GetStatValue(StatDefOf.Mass) * item.stackCount;
                }
            }
            return totalMass;
        }

        public static Pawn FindBestNegotiator(Caravan playerCaravan)
        {
            if (playerCaravan == null || !playerCaravan.Spawned || playerCaravan.Destroyed) return null;
            Pawn best = null;
            int bestSocial = -1;
            float bestPrice = -1f;

            List<Pawn> pawns = playerCaravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Destroyed || p.Dead || p.Downed || !p.IsFreeColonist) continue;
                if (p.skills == null || p.WorkTagIsDisabled(WorkTags.Social)) continue;
                if (p.health == null || p.health.capacities == null || !p.health.capacities.CapableOf(PawnCapacityDefOf.Talking)) continue;

                SkillRecord socialSkill = p.skills.GetSkill(SkillDefOf.Social);
                int level = socialSkill != null ? socialSkill.Level : 0;
                float price = p.GetStatValue(StatDefOf.TradePriceImprovement);
                if (best == null || level > bestSocial || (level == bestSocial && price > bestPrice))
                {
                    best = p;
                    bestSocial = level;
                    bestPrice = price;
                }
            }
            return best;
        }

        public static TraderKindDef ResolveTraderKind(Caravan trafficCaravan)
        {
            if (trafficCaravan == null || trafficCaravan.Faction == null) return null;
            FactionDef fdef = trafficCaravan.Faction.def;
            List<Thing> currentGoods = GetRealCaravanGoods(trafficCaravan);

            TraderKindDef best = PickBestTraderKind(fdef != null ? fdef.caravanTraderKinds : null, currentGoods);
            if (best != null) return best;

            best = PickBestTraderKind(fdef != null ? fdef.baseTraderKinds : null, currentGoods);
            if (best != null) return best;

            List<TraderKindDef> allKinds = DefDatabase<TraderKindDef>.AllDefsListForReading;
            for (int i = 0; i < allKinds.Count; i++)
            {
                TraderKindDef kind = allKinds[i];
                if (kind != null && !kind.orbital) return kind;
            }
            return null;
        }

        private static TraderKindDef PickBestTraderKind(List<TraderKindDef> candidates, List<Thing> goods)
        {
            if (candidates == null || candidates.Count == 0) return null;
            TraderKindDef best = null;
            int bestScore = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                TraderKindDef kind = candidates[i];
                if (kind == null) continue;
                int score = 0;
                if (goods != null)
                {
                    for (int j = 0; j < goods.Count; j++)
                    {
                        Thing item = goods[j];
                        if (item != null && !item.Destroyed && item.def != ThingDefOf.Silver && kind.WillTrade(item.def))
                            score++;
                    }
                }
                if (best == null || score > bestScore)
                {
                    best = kind;
                    bestScore = score;
                }
            }
            return best;
        }

        public static Pawn GetOrPrepareTraderLeader(Caravan trafficCaravan)
        {
            if (!IsValidTrafficCaravan(trafficCaravan)) return null;

            Pawn leader = null;
            List<Pawn> pawns = trafficCaravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Destroyed || p.Dead || p.Downed) continue;
                if (p.trader != null && p.trader.traderKind != null)
                {
                    leader = p;
                    break;
                }
            }

            if (leader == null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p != null && !p.Destroyed && !p.Dead && !p.Downed && p.RaceProps != null && p.RaceProps.Humanlike)
                    {
                        leader = p;
                        break;
                    }
                }
            }

            if (leader == null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p != null && !p.Destroyed && !p.Dead && !p.Downed)
                    {
                        leader = p;
                        break;
                    }
                }
            }

            if (leader == null) return null;

            if (leader.trader == null)
                leader.trader = new Pawn_TraderTracker(leader);

            if (leader.trader.traderKind == null)
                leader.trader.traderKind = ResolveTraderKind(trafficCaravan);

            return leader;
        }

        public static List<Thing> GetRealCaravanGoods(Caravan trafficCaravan)
        {
            List<Thing> goods = new List<Thing>();
            if (trafficCaravan == null || trafficCaravan.Destroyed) return goods;

            List<Pawn> pawns = trafficCaravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Destroyed || p.Dead || p.inventory == null || p.inventory.innerContainer == null) continue;
                IList<Thing> items = p.inventory.innerContainer.InnerListForReading;
                for (int j = 0; j < items.Count; j++)
                {
                    Thing item = items[j];
                    if (item == null || item.Destroyed || item.stackCount <= 0) continue;
                    if (item.def == null || item.def.tradeability == Tradeability.None) continue;
                    goods.Add(item);
                }
            }
            return goods;
        }

        public static bool CanTradeWith(Caravan playerCaravan, Caravan trafficCaravan)
        {
            if (!IsSameTilePlayerAndTraffic(playerCaravan, trafficCaravan)) return false;
            if (trafficCaravan.Faction.HostileTo(Faction.OfPlayer)) return false;
            if (FindBestNegotiator(playerCaravan) == null) return false;
            Pawn leader = GetOrPrepareTraderLeader(trafficCaravan);
            return leader != null && leader.trader != null && leader.trader.traderKind != null;
        }

        public static bool CanAttack(Caravan playerCaravan, Caravan trafficCaravan)
        {
            return IsSameTilePlayerAndTraffic(playerCaravan, trafficCaravan) && AttackBlockReason(playerCaravan, trafficCaravan) == null;
        }

        private const int AttackGoodwillDelta = -80;

        /// <summary>Test-only (TestMode): makes the battle-map step throw so the rollback path can be checked.</summary>
        public static bool TestFailAttackMap;

        /// <summary>Wilderness tile for the battle: the shared tile, or a free passable neighbour. Invalid when none exists.</summary>
        public static PlanetTile FindBattleTile(Caravan playerCaravan)
        {
            PlanetTile tile = playerCaravan.Tile;
            if (Find.WorldObjects == null || Find.WorldGrid == null) return PlanetTile.Invalid;
            if (Find.WorldObjects.MapParentAt(tile) == null) return tile;
            List<PlanetTile> neighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(tile, neighbors);
            for (int n = 0; n < neighbors.Count; n++)
            {
                PlanetTile nt = neighbors[n];
                if (nt.Valid && Find.WorldObjects.MapParentAt(nt) == null && !Find.World.Impassable(nt))
                    return nt;
            }
            return PlanetTile.Invalid;
        }

        /// <summary>Null when an attack may start; otherwise the reason shown on the disabled button. Changes nothing.</summary>
        public static string AttackBlockReason(Caravan playerCaravan, Caravan trafficCaravan)
        {
            if (!IsSameTilePlayerAndTraffic(playerCaravan, trafficCaravan)) return "TMM_TrafficAttackBlockedGone".Translate();
            Faction enemy = trafficCaravan.Faction;
            StoryRelationRestorer restorer = StoryRelationRestorer.Instance;
            if (restorer != null && restorer.HasPendingBackupForFaction(enemy))
                return "TMM_TrafficAttackBlockedTruce".Translate(enemy.Name);
            if (!enemy.HostileTo(Faction.OfPlayer))
            {
                if (!enemy.HasGoodwill) return "TMM_TrafficAttackBlockedDiplomacy".Translate(enemy.Name);
                int delta = HostileGoodwillDelta(enemy);
                if (delta >= 0 || !enemy.CanChangeGoodwillFor(Faction.OfPlayer, delta))
                    return "TMM_TrafficAttackBlockedDiplomacy".Translate(enemy.Name);
            }
            if (!FindBattleTile(playerCaravan).Valid) return "TMM_TrafficAttackBlockedNoTile".Translate();
            return null;
        }

        private static int HostileGoodwillDelta(Faction enemy)
        {
            int delta = AttackGoodwillDelta;
            int toHostile = enemy.GoodwillToMakeHostile(Faction.OfPlayer);
            if (toHostile < delta) delta = toHostile;
            return delta;
        }

        public static bool SetupTradeSession(Caravan playerCaravan, Caravan trafficCaravan)
        {
            if (!IsSameTilePlayerAndTraffic(playerCaravan, trafficCaravan)) return false;
            if (trafficCaravan.Faction.HostileTo(Faction.OfPlayer)) return false;

            Pawn negotiator = FindBestNegotiator(playerCaravan);
            if (negotiator == null) return false;

            Pawn trader = GetOrPrepareTraderLeader(trafficCaravan);
            if (trader == null || trader.trader == null || trader.trader.traderKind == null) return false;

            TradeSession.SetupWith(trader, negotiator, false);
            sessionTrader = trader;
            sessionNegotiator = negotiator;
            return TradeSession.Active && TradeSession.trader == trader && TradeSession.playerNegotiator == negotiator;
        }

        // The session that was opened with a travelling caravan stays one even if that caravan or its route is gone meanwhile.
        private static Pawn sessionTrader, sessionNegotiator;

        private static void TradeSession_SetupWith_Postfix()
        {
            sessionTrader = null;
            sessionNegotiator = null;
        }

        private static bool IsTrafficSession()
        {
            return sessionTrader != null && TradeSession.trader == sessionTrader && TradeSession.playerNegotiator == sessionNegotiator;
        }

        public static bool OpenTrade(Caravan playerCaravan, Caravan trafficCaravan)
        {
            if (!SetupTradeSession(playerCaravan, trafficCaravan)) return false;
            Pawn trader = TradeSession.trader as Pawn;
            Pawn negotiator = TradeSession.playerNegotiator;
            if (trader == null || negotiator == null) return false;

            if (Find.WindowStack != null)
            {
                Find.WindowStack.Add(new Dialog_Trade(negotiator, trader, false));
            }
            return true;
        }

        public static Map ExecuteAttack(Caravan playerCaravan, Caravan trafficCaravan, bool switchCamera)
        {
            // Phase 0: every check happens before anything changes.
            if (!IsSameTilePlayerAndTraffic(playerCaravan, trafficCaravan)) return null;
            string blocked = AttackBlockReason(playerCaravan, trafficCaravan);
            if (blocked != null)
            {
                Messages.Message(blocked, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            WorldTraffic traffic = GetTrafficComponent();
            if (traffic == null) return null;
            WorldTrafficRoute route = traffic.Route(trafficCaravan);
            if (route == null) return null;

            Faction enemyFaction = trafficCaravan.Faction;
            PlanetTile originalPlayerTile = playerCaravan.Tile;
            PlanetTile trafficTile = trafficCaravan.Tile;
            PlanetTile battleTile = FindBattleTile(playerCaravan);
            if (!battleTile.Valid) return null;

            List<Pawn> enemies = new List<Pawn>();
            List<Pawn> members = trafficCaravan.PawnsListForReading;
            for (int i = 0; i < members.Count; i++)
            {
                Pawn p = members[i];
                if (p != null && !p.Destroyed && !p.Dead) enemies.Add(p);
            }
            if (enemies.Count == 0) return null;

            // Mark to prevent duplicate clicks or re-entrant triggers while the battle is prepared.
            EngagedCaravanIds.Add(trafficCaravan.ID);

            List<Pawn> players = new List<Pawn>(playerCaravan.PawnsListForReading);
            string playerCaravanName = playerCaravan.Name;
            GoodwillBackup goodwill = new GoodwillBackup(enemyFaction);
            Map battleMap = null;
            bool entering = false;
            try
            {
                // Phase 1: the map and the enemy, while the player's caravan still stands on the world map untouched.
                TestStep("map");
                int size = CaravanIncidentUtility.CalculateIncidentMapSize(players, enemies);
                battleMap = GetOrGenerateMapUtility.GetOrGenerateMap(battleTile, new IntVec3(size, 1, size), WorldObjectDefOf.Ambush);
                if (battleMap == null) throw new InvalidOperationException("no battle map");
                IntVec3 playerSpot, enemySpot;
                MultipleCaravansCellFinder.FindStartingCellsFor2Groups(battleMap, out playerSpot, out enemySpot);
                for (int i = 0; i < enemies.Count; i++)
                {
                    Pawn p = enemies[i];
                    if (trafficCaravan.ContainsPawn(p)) trafficCaravan.RemovePawn(p);
                    GenSpawn.Spawn(p, CellFinder.RandomSpawnCellForPawnNear(enemySpot, battleMap, 4), battleMap, Rot4.Random);
                    TestStep("enemy" + i);
                }
                for (int i = 0; i < enemies.Count; i++)
                    if (!enemies[i].Spawned || enemies[i].Map != battleMap) throw new InvalidOperationException(enemies[i].LabelShort + " did not enter the battle map");

                // Phase 2: diplomacy. The battle exists only if the faction really is hostile afterwards.
                if (!enemyFaction.HostileTo(Faction.OfPlayer))
                {
                    HistoryEventDef reason = DefDatabase<HistoryEventDef>.GetNamedSilentFail("AttackedCaravan");
                    if (reason == null) reason = HistoryEventDefOf.AttackedMember;
                    enemyFaction.TryAffectGoodwillWith(Faction.OfPlayer, HostileGoodwillDelta(enemyFaction), true, true, reason, new GlobalTargetInfo(battleTile));
                    if (!enemyFaction.HostileTo(Faction.OfPlayer)) throw new InvalidOperationException("the faction did not become hostile");
                }
                TestStep("diplomacy");

                // Phase 3: the player enters. From here on a failure has to rebuild the player's caravan too.
                entering = true;
                battleMap.retainedCaravanData.Notify_GeneratedTempIncidentMapFor(playerCaravan);
                CaravanEnterMapUtility.Enter(playerCaravan, battleMap, p => CellFinder.RandomSpawnCellForPawnNear(playerSpot, battleMap, 4), CaravanDropInventoryMode.DoNotDrop, true);
                TestStep("player");
                for (int i = 0; i < players.Count; i++)
                    if (!players[i].Destroyed && !players[i].Dead && (!players[i].Spawned || players[i].Map != battleMap)) throw new InvalidOperationException(players[i].LabelShort + " did not enter the battle map");
            }
            catch (Exception ex)
            {
                Log.Warning("[Turbo mega mod] WorldTrafficInteraction attack undone: " + ex);
                UndoAttack(route, trafficCaravan, enemies, enemyFaction, trafficTile, playerCaravan, playerCaravanName, players, originalPlayerTile, battleMap, goodwill, entering);
                return null;
            }

            // Phase 4: everybody is on the map. Only now the traffic record is retired.
            traffic.BeforeRemoval(trafficCaravan);
            traffic.routes.Remove(route);
            if (trafficCaravan.Spawned && !trafficCaravan.Destroyed && Find.WorldObjects != null)
                Find.WorldObjects.Remove(trafficCaravan);

            try
            {
                List<Pawn> unlorded = new List<Pawn>();
                for (int i = 0; i < enemies.Count; i++)
                    if (enemies[i].Spawned && !enemies[i].Dead && enemies[i].GetLord() == null) unlorded.Add(enemies[i]);
                if (unlorded.Count > 0)
                    LordMaker.MakeNewLord(enemyFaction, new LordJob_AssaultColony(enemyFaction), battleMap, unlorded);
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(enemies, "LetterRelatedPawnsGroupGeneric".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, true);
                if (switchCamera) CameraJumper.TryJump(new TargetInfo(enemies[0].Position, battleMap));
            }
            catch (Exception ex)
            {
                Log.Error("[Turbo mega mod] WorldTrafficInteraction: the battle started, a later step failed: " + ex);
            }
            return battleMap;
        }

        /// <summary>Test-only (TestMode): throws when the attack reaches this step ("map", "enemy0", "diplomacy", "player").</summary>
        public static string TestFailAttackStep;

        private static void TestStep(string step)
        {
            if (!TestMode.Enabled) return;
            if (TestFailAttackMap && step == "map" || TestFailAttackStep == step) throw new InvalidOperationException("probe: forced attack failure at " + step);
        }

        /// What an attack may change in the relations of two factions, to put it back exactly.
        private sealed class GoodwillBackup
        {
            private readonly Faction faction;
            private readonly int goodwillA, goodwillB;
            private readonly FactionRelationKind kindA, kindB;

            public GoodwillBackup(Faction faction)
            {
                this.faction = faction;
                FactionRelation a = faction.RelationWith(Faction.OfPlayer, true), b = Faction.OfPlayer.RelationWith(faction, true);
                if (a != null) { goodwillA = a.baseGoodwill; kindA = a.kind; }
                if (b != null) { goodwillB = b.baseGoodwill; kindB = b.kind; }
            }

            public void Restore()
            {
                FactionRelation a = faction.RelationWith(Faction.OfPlayer, true), b = Faction.OfPlayer.RelationWith(faction, true);
                bool changed = a != null && (a.kind != kindA || a.baseGoodwill != goodwillA);
                if (a != null) { a.baseGoodwill = goodwillA; a.kind = kindA; }
                if (b != null) { b.baseGoodwill = goodwillB; b.kind = kindB; }
                if (!changed) return;
                foreach (Map map in Find.Maps) if (map.attackTargetsCache != null) map.attackTargetsCache.Notify_FactionHostilityChanged(faction, Faction.OfPlayer);
            }
        }

        /// <summary>
        /// The battle could not be started: the enemy returns to its caravan and route, the player to the caravan and tile
        /// it came from, the relations to what they were, and the unused battle map is closed. The attack can be retried.
        /// </summary>
        private static void UndoAttack(WorldTrafficRoute route, Caravan trafficCaravan, List<Pawn> enemies, Faction faction, PlanetTile trafficTile,
            Caravan playerCaravan, string playerCaravanName, List<Pawn> players, PlanetTile playerTile, Map battleMap, GoodwillBackup goodwill, bool entering)
        {
            int id = trafficCaravan.ID;
            Guarded("enemy", delegate
            {
                trafficCaravan = Regroup(trafficCaravan, enemies, faction, trafficTile, null);
                if (trafficCaravan != null) route.caravan = trafficCaravan;
            });
            if (entering) Guarded("player", delegate { Regroup(playerCaravan, players, Faction.OfPlayer, playerTile, playerCaravanName); });
            Guarded("relations", delegate { goodwill.Restore(); });
            Guarded("map", delegate { CloseBattleMap(battleMap); });
            EngagedCaravanIds.Remove(id);
            if (trafficCaravan != null) EngagedCaravanIds.Remove(trafficCaravan.ID);
            Messages.Message("TMM_TrafficAttackFailed".Translate(), MessageTypeDefOf.RejectInput, false);
        }

        private static void Guarded(string what, Action step)
        {
            try { step(); }
            catch (Exception ex) { Log.Error("[Turbo mega mod] WorldTrafficInteraction: undoing the attack (" + what + ") failed: " + ex); }
        }

        /// Puts every living pawn of the list back into its caravan on the world map, or into a new one when the old one is gone.
        private static Caravan Regroup(Caravan caravan, List<Pawn> pawns, Faction faction, PlanetTile tile, string name)
        {
            bool alive = caravan != null && !caravan.Destroyed && caravan.Spawned;
            List<Pawn> back = new List<Pawn>();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Destroyed || p.Dead || alive && caravan.ContainsPawn(p)) continue;
                if (p.drafter != null && p.Drafted) p.drafter.Drafted = false;
                if (p.Spawned)
                {
                    Lord lord = p.GetLord();
                    if (lord != null) lord.Notify_PawnLost(p, PawnLostCondition.Vanished);
                    p.DeSpawn(DestroyMode.Vanish);
                }
                if (p.holdingOwner != null) p.holdingOwner.Remove(p);
                back.Add(p);
            }
            if (!alive)
            {
                if (back.Count == 0) return null;
                caravan = CaravanMaker.MakeCaravan(back, faction, tile, true);
                if (!name.NullOrEmpty()) caravan.Name = name;
            }
            for (int i = 0; i < back.Count; i++)
                if (!caravan.ContainsPawn(back[i])) caravan.AddPawn(back[i], true);
            caravan.Tile = tile;
            for (int i = 0; i < back.Count; i++)
                if (!caravan.ContainsPawn(back[i])) WorldTrafficCustody.Keep(back[i], null, faction == Faction.OfPlayer);
            return caravan;
        }

        private static void CloseBattleMap(Map map)
        {
            if (map == null || !Find.Maps.Contains(map)) return;
            MapParent parent = map.Parent;
            if (map.mapPawns.AnyColonistSpawned || parent == null || parent.def != WorldObjectDefOf.Ambush) return;
            Current.Game.DeinitAndRemoveMap(map, false);
            if (!parent.Destroyed) parent.Destroy();
        }
        private static void Caravan_GetGizmos_Postfix(Caravan __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance == null || !__instance.Spawned || __instance.Destroyed || __instance.Faction != Faction.OfPlayer)
                return;

            List<Caravan> targets = GetInteractableTrafficCaravans(__instance);
            if (targets.Count == 0) return;

            string legacyTradeLabel = "TMM.WorldTraffic.Trade".Translate().RawText;
            List<Gizmo> gizmos = new List<Gizmo>();
            if (__result != null)
            {
                foreach (Gizmo g in __result)
                {
                    Command_Action existing = g as Command_Action;
                    if (existing != null && string.Equals(existing.defaultLabel, legacyTradeLabel, StringComparison.Ordinal))
                        continue;
                    gizmos.Add(g);
                }
            }

            Caravan playerCaravan = __instance;
            Pawn negotiator = FindBestNegotiator(playerCaravan);

            for (int i = 0; i < targets.Count; i++)
            {
                Caravan npc = targets[i];
                string factionName = npc.Faction != null ? npc.Faction.Name : "TMM.WorldTraffic.Unknown".Translate().RawText;
                int peopleCount = CountLivingHumanlikeMembers(npc);
                if (peopleCount <= 0) peopleCount = CountLivingMembers(npc);
                float cargoMass = ComputeCargoMass(npc);
                string summary = "TMM_TrafficTooltip".Translate(factionName, peopleCount, cargoMass.ToString("0.#"));

                if (npc.Faction != null && !npc.Faction.HostileTo(Faction.OfPlayer))
                {
                    Caravan tradeTarget = npc;
                    Command_Action tradeCmd = new Command_Action();
                    tradeCmd.defaultLabel = "TMM_TrafficTrade".Translate(factionName);
                    tradeCmd.defaultDesc = "TMM_TrafficTradeDesc".Translate(summary);
                    tradeCmd.icon = GetTradeIcon();
                    tradeCmd.action = delegate
                    {
                        OpenTrade(playerCaravan, tradeTarget);
                    };

                    if (negotiator == null)
                    {
                        tradeCmd.Disable("TMM_TrafficNoNegotiator".Translate());
                    }
                    else if (ResolveTraderKind(tradeTarget) == null)
                    {
                        tradeCmd.Disable("TMM_TrafficNoTraderKind".Translate());
                    }

                    gizmos.Add(tradeCmd);
                }

                Caravan attackTarget = npc;
                string confirmFactionName = factionName;
                Command_Action attackCmd = new Command_Action();
                attackCmd.defaultLabel = "TMM_TrafficAttack".Translate(factionName);
                attackCmd.defaultDesc = "TMM_TrafficAttackDesc".Translate(summary);
                attackCmd.icon = GetAttackIcon();
                attackCmd.action = delegate
                {
                    if (!CanAttack(playerCaravan, attackTarget)) return;
                    string confirmText = "TMM_TrafficAttackConfirm".Translate(confirmFactionName);
                    string confirmTitle = "TMM_TrafficAttackConfirmTitle".Translate();
                    Dialog_MessageBox dialog = Dialog_MessageBox.CreateConfirmation(
                        confirmText,
                        delegate
                        {
                            ExecuteAttack(playerCaravan, attackTarget, true);
                        },
                        true,
                        confirmTitle);
                    if (Find.WindowStack != null)
                        Find.WindowStack.Add(dialog);
                };
                string attackBlocked = AttackBlockReason(playerCaravan, attackTarget);
                if (attackBlocked != null) attackCmd.Disable(attackBlocked);
                gizmos.Add(attackCmd);
            }

            __result = gizmos;
        }

        private static Pawn GetTrackedPawn(Pawn_TraderTracker tracker)
        {
            if (tracker == null || TraderPawnField == null) return null;
            return TraderPawnField.GetValue(tracker) as Pawn;
        }

        private static Caravan FindTrafficCaravanForTrader(Pawn_TraderTracker tracker, out Pawn traderPawn)
        {
            traderPawn = GetTrackedPawn(tracker);
            if (traderPawn == null || traderPawn.Spawned || traderPawn.Destroyed) return null;

            Caravan caravan = traderPawn.GetCaravan();
            if (!IsValidTrafficCaravan(caravan)) return null;
            return caravan;
        }

        private static bool TraderTracker_CanTradeNow_Prefix(Pawn_TraderTracker __instance, ref bool __result)
        {
            Pawn traderPawn;
            Caravan trafficCaravan = FindTrafficCaravanForTrader(__instance, out traderPawn);
            if (trafficCaravan == null) return true;

            __result = !traderPawn.Dead
                && !traderPawn.Downed
                && !traderPawn.Destroyed
                && trafficCaravan.Spawned
                && !trafficCaravan.Destroyed
                && trafficCaravan.Faction != null
                && !trafficCaravan.Faction.HostileTo(Faction.OfPlayer)
                && __instance.traderKind != null;
            return false;
        }

        private static bool TraderTracker_Goods_Prefix(Pawn_TraderTracker __instance, ref IEnumerable<Thing> __result)
        {
            Pawn traderPawn;
            Caravan trafficCaravan = FindTrafficCaravanForTrader(__instance, out traderPawn);
            if (trafficCaravan == null) return true;

            __result = GetRealCaravanGoods(trafficCaravan);
            return false;
        }

        private static bool TraderTracker_ColonyThingsWillingToBuy_Prefix(Pawn_TraderTracker __instance, Pawn playerNegotiator, ref IEnumerable<Thing> __result)
        {
            Pawn traderPawn;
            Caravan trafficCaravan = FindTrafficCaravanForTrader(__instance, out traderPawn);
            if (trafficCaravan == null) return true;

            List<Thing> sellables = new List<Thing>();
            Caravan playerCaravan = playerNegotiator == null ? null : playerNegotiator.GetCaravan();
            if (playerCaravan != null && playerCaravan.Spawned && !playerCaravan.Destroyed)
            {
                List<Thing> invItems = CaravanInventoryUtility.AllInventoryItems(playerCaravan);
                for (int i = 0; i < invItems.Count; i++)
                {
                    Thing item = invItems[i];
                    if (item != null && !item.Destroyed && item.stackCount > 0 && !IsReservedForMission(item))
                        sellables.Add(item);
                }

                List<Pawn> pawns = playerCaravan.PawnsListForReading;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn p = pawns[j];
                    if (p != null && !p.Destroyed && !playerCaravan.IsOwner(p))
                        sellables.Add(p);
                }
            }

            __result = sellables;
            return false;
        }

        public static bool IsReservedForMission(Thing t)
        {
            return t != null && OutpostCaravanManager.IsMissionSilverThing(t);
        }

        // ---- One journal for a whole deal: goods, currency and pawns. Nothing is final until every side accepted everything.

        private sealed class TradeMove
        {
            public Pawn pawn; public Caravan from, to;                                        // a traded pawn
            public Thing origin, piece; public int originCount;                               // a traded stack
            public Pawn ownerPawn;
            public TradeAction action;
        }

        /// The deal cannot be carried out. Leaves TradeDeal.TryExecute at once, so that no goodwill, tale or inspiration is counted.
        private sealed class TradeRefusedException : Exception
        {
            public TradeRefusedException(string message) : base(message) { }
        }

        private static List<TradeMove> journal;   // null outside TradeDeal.TryExecute of a traffic session
        private static Caravan journalPlayer, journalTraffic;

        /// <summary>Test-only (TestMode): refuses an inventory that should receive a stack / take one back.</summary>
        public static Func<Thing, Pawn, bool> TestRefuseReceive, TestRefuseReturn;

        private static bool TryPutInto(Pawn holder, Thing thing, bool merge, Func<Thing, Pawn, bool> refuse)
        {
            if (holder == null || holder.Dead || holder.Destroyed || holder.inventory == null || holder.inventory.innerContainer == null) return false;
            if (TestMode.Enabled && refuse != null && refuse(thing, holder)) return false;
            return holder.inventory.innerContainer.TryAdd(thing, merge) && (merge || thing.holdingOwner == holder.inventory.innerContainer);
        }

        /// <summary>Puts a split-off stack unmerged into the first inventory that accepts it. True only if it is really held there.</summary>
        private static bool TryStoreInInventories(Thing thing, List<Pawn> candidates, Pawn preferred)
        {
            if (TryPutInto(preferred, thing, false, TestRefuseReceive)) return true;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i] != preferred && TryPutInto(candidates[i], thing, false, TestRefuseReceive)) return true;
            return false;
        }

        private static Pawn OwnerPawn(Thing thing)
        {
            Pawn_InventoryTracker inventory = thing.ParentHolder as Pawn_InventoryTracker;
            return inventory != null ? inventory.pawn : null;
        }

        /// Moves part of a stack between the caravans. Throws when the receiving side does not take it; the journal then undoes the deal.
        private static void MoveStack(Thing origin, int count, List<Pawn> receivers, Pawn preferred, TradeAction action)
        {
            TradeMove move = new TradeMove { origin = origin, originCount = origin.stackCount, ownerPawn = OwnerPawn(origin), action = action };
            journal.Add(move);
            move.piece = origin.SplitOff(count);
            if (move.piece.holdingOwner != null) move.piece.holdingOwner.Remove(move.piece);
            if (!TryStoreInInventories(move.piece, receivers, preferred)) throw new TradeRefusedException(origin.LabelShort + " x" + count + " was not accepted");
        }

        private static void MovePawn(Pawn pawn, Caravan from, Caravan to, TradeAction action)
        {
            if (from == null || !from.ContainsPawn(pawn)) throw new TradeRefusedException(pawn.LabelShort + " is not in the caravan that offers it");
            journal.Add(new TradeMove { pawn = pawn, from = from, to = to, action = action });
            from.RemovePawn(pawn);
            if (!(TestMode.Enabled && TestRefuseReceive != null && TestRefuseReceive(pawn, null))) to.AddPawn(pawn, true);
            if (!to.ContainsPawn(pawn)) throw new TradeRefusedException(pawn.LabelShort + " was not accepted");
        }

        /// Undoes every move in reverse order. What its old owner does not take back goes to a saved custody, never to nobody.
        private static void RollbackTrade()
        {
            List<TradeMove> moves = journal;
            journal = null;
            for (int i = moves.Count - 1; i >= 0; i--)
            {
                TradeMove m = moves[i];
                try
                {
                    if (m.pawn != null)
                    {
                        if (m.to.ContainsPawn(m.pawn)) m.to.RemovePawn(m.pawn);
                        if (m.from.ContainsPawn(m.pawn)) continue;
                        if (m.from.Spawned && !m.from.Destroyed && !(TestMode.Enabled && TestRefuseReturn != null && TestRefuseReturn(m.pawn, null))) m.from.AddPawn(m.pawn, true);
                        if (!m.from.ContainsPawn(m.pawn)) WorldTrafficCustody.Keep(m.pawn, null, m.action == TradeAction.PlayerSells);
                        continue;
                    }
                    if (m.piece == null)
                    {
                        // SplitOff itself failed: the whole amount is still the origin's, whatever the attempt left behind.
                        if (!m.origin.Destroyed) m.origin.stackCount = m.originCount;
                        if (!m.origin.Destroyed && m.origin.holdingOwner == null && !m.origin.Spawned) GiveBack(m, m.origin);
                        continue;
                    }
                    if (m.piece.Destroyed) { Log.Warning("[Turbo mega mod] Traffic trade rollback: " + m.piece.LabelShort + " was destroyed by someone during the deal."); continue; }
                    if (m.piece.holdingOwner != null) m.piece.holdingOwner.Remove(m.piece);
                    // Return the exact piece without merging it. Increasing the source count before Destroy
                    // could grant both the restored count and a surviving split if destruction failed.
                    GiveBack(m, m.piece);
                }
                catch (Exception ex)
                {
                    Log.Warning("[Turbo mega mod] Traffic trade rollback of one move failed: " + ex);
                    Thing left = m.piece != null ? m.piece : m.origin;
                    if (left != null && !left.Destroyed && !left.Spawned && left.holdingOwner == null) WorldTrafficCustody.Keep(left, m.ownerPawn, m.action == TradeAction.PlayerSells);
                }
            }
        }

        private static void GiveBack(TradeMove m, Thing thing)
        {
            if (TryPutInto(m.ownerPawn, thing, false, TestRefuseReturn)) return;
            Caravan side = m.action == TradeAction.PlayerSells ? journalPlayer : journalTraffic;
            if (side != null && !side.Destroyed)
                foreach (Pawn p in side.PawnsListForReading)
                    if (p != m.ownerPawn && TryPutInto(p, thing, false, TestRefuseReturn)) return;
            WorldTrafficCustody.Keep(thing, m.ownerPawn, m.action == TradeAction.PlayerSells);
        }

        /// Everything is with its new owner. Only now the game is told, and split stacks are merged with what the owner already had.
        private static void CommitTrade(Pawn traderPawn, Pawn negotiator)
        {
            List<TradeMove> moves = journal;
            journal = null;
            foreach (TradeMove m in moves)
            {
                Thing thing = m.pawn != null ? m.pawn : m.piece;
                try
                {
                    if (!thing.Destroyed) thing.PreTraded(m.action, negotiator, traderPawn);
                    if (m.pawn != null || thing.Destroyed || thing.holdingOwner == null) continue;
                    ThingOwner owner = thing.holdingOwner;
                    for (int i = 0; i < owner.Count && !thing.Destroyed; i++)
                        if (owner[i] != thing && owner[i].CanStackWith(thing) && owner[i].stackCount < owner[i].def.stackLimit) owner[i].TryAbsorbStack(thing, true);
                }
                catch (Exception ex)
                {
                    Log.Error("[Turbo mega mod] Traffic trade: a step after the confirmed exchange of " + thing.LabelShort + " failed: " + ex);
                }
            }
        }

        private static bool TraderTracker_GiveSoldThingToTrader_Prefix(Pawn_TraderTracker __instance, Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            return GiveSold(__instance, toGive, countToGive, playerNegotiator, TradeAction.PlayerSells);
        }

        private static bool TraderTracker_GiveSoldThingToPlayer_Prefix(Pawn_TraderTracker __instance, Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            return GiveSold(__instance, toGive, countToGive, playerNegotiator, TradeAction.PlayerBuys);
        }

        /// True lets vanilla handle a trade that is not with a travelling caravan.
        private static bool GiveSold(Pawn_TraderTracker tracker, Thing thing, int count, Pawn negotiator, TradeAction action)
        {
            Pawn traderPawn;
            Caravan traffic = FindTrafficCaravanForTrader(tracker, out traderPawn);
            if (traffic == null)
            {
                if (traderPawn == null || traderPawn != sessionTrader) return true;
                if (journal != null) throw new TradeRefusedException("the caravan is gone");
                return false;
            }
            Caravan player = negotiator == null ? null : negotiator.GetCaravan();
            if (player == null || thing == null || thing.Destroyed || count <= 0 || action == TradeAction.PlayerSells && IsReservedForMission(thing))
            {
                if (journal != null) throw new TradeRefusedException("nothing to move or nobody to move it");
                return false;
            }

            // Outside TradeDeal (another mod, a probe) a single exchange is its own deal.
            bool single = journal == null;
            if (single) { journal = new List<TradeMove>(); journalPlayer = player; journalTraffic = traffic; }
            try
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null) MovePawn(pawn, action == TradeAction.PlayerSells ? player : traffic, action == TradeAction.PlayerSells ? traffic : player, action);
                else if (action == TradeAction.PlayerSells) MoveStack(thing, count, traffic.PawnsListForReading, FindBestTrafficCarrier(traffic, traderPawn, thing), action);
                else
                {
                    // Vanilla CaravanInventoryUtility.GiveThing destroys the item when no carrier accepts it; never use it here.
                    Pawn carrier = CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, player.PawnsListForReading, null, null);
                    MoveStack(thing, count, player.PawnsListForReading, carrier != null ? carrier : negotiator, action);
                }
                if (single) CommitTrade(traderPawn, negotiator);
            }
            catch (Exception ex)
            {
                if (!single) throw;
                RollbackTrade();
                Log.Warning("[Turbo mega mod] Traffic exchange undone: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Before a traffic deal executes, re-check that both caravans still share a tile and that every stack in the
        /// deal is still held by the side that offered it and is not mission silver. A stale session executes nothing.
        /// </summary>
        private static bool TradeDeal_TryExecute_Prefix(TradeDeal __instance, ref bool actuallyTraded, ref bool __result)
        {
            journal = null;
            Pawn traderPawn = TradeSession.trader as Pawn;
            bool ours = IsTrafficSession();
            Caravan trafficCaravan = traderPawn == null ? null : traderPawn.GetCaravan();
            bool live = traderPawn != null && traderPawn.trader != null && !traderPawn.Destroyed && trafficCaravan != null && !traffic_RouteMissing(trafficCaravan);
            if (!ours && !live) return true;

            Pawn negotiator = TradeSession.playerNegotiator;
            Caravan playerCaravan = negotiator == null ? null : negotiator.GetCaravan();
            string problem = null;
            if (!live || !IsSameTilePlayerAndTraffic(playerCaravan, trafficCaravan)) problem = "TMM_TrafficTradeStale";
            else if (trafficCaravan.Faction.HostileTo(Faction.OfPlayer)) problem = "TMM_TrafficTradeStale";
            else
            {
                List<Tradeable> all = __instance.AllTradeables;
                for (int i = 0; i < all.Count && problem == null; i++)
                {
                    Tradeable t = all[i];
                    if (t == null || t.CountToTransfer == 0) continue;
                    // Whatever the direction, every stack the deal lists must still be held by the side that offered it.
                    if (!StacksHeldBy(t.thingsColony, playerCaravan, true) || !StacksHeldBy(t.thingsTrader, trafficCaravan, false))
                        problem = "TMM_TrafficTradeStale";
                }
            }
            if (problem == null)
            {
                journal = new List<TradeMove>();
                journalPlayer = playerCaravan;
                journalTraffic = trafficCaravan;
                return true;
            }

            actuallyTraded = false;
            __result = false;
            Messages.Message(problem.Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        private static void TradeDeal_TryExecute_Postfix(bool __result, ref bool actuallyTraded)
        {
            if (journal == null) return;
            if (!__result)
            {
                RollbackTrade();
                actuallyTraded = false;
                return;
            }
            CommitTrade(TradeSession.trader as Pawn, TradeSession.playerNegotiator);
        }

        private static Exception TradeDeal_TryExecute_Finalizer(Exception __exception, ref bool actuallyTraded, ref bool __result)
        {
            if (__exception == null || journal == null) { journal = null; return __exception; }
            RollbackTrade();
            actuallyTraded = false;
            __result = false;
            if (!(__exception is TradeRefusedException)) Log.Error("[Turbo mega mod] Traffic trade undone after an exception: " + __exception);
            else Log.Warning("[Turbo mega mod] Traffic trade undone: " + __exception.Message);
            Messages.Message("TMM_TrafficTradeStale".Translate(), MessageTypeDefOf.RejectInput, false);
            return null;
        }
        private static bool traffic_RouteMissing(Caravan caravan)
        {
            WorldTraffic traffic = GetTrafficComponent();
            return traffic == null || traffic.Route(caravan) == null;
        }

        private static bool StacksHeldBy(List<Thing> things, Caravan caravan, bool playerSide)
        {
            if (things == null) return true;
            for (int i = 0; i < things.Count; i++)
            {
                Thing th = things[i];
                if (th == null || th.Destroyed) return false;
                if (playerSide && IsReservedForMission(th)) return false;
                Pawn asPawn = th as Pawn;
                if (asPawn != null) { if (!caravan.ContainsPawn(asPawn)) return false; continue; }
                Pawn owner = CaravanInventoryUtility.GetOwnerOf(caravan, th);
                if (owner == null) return false;
            }
            return true;
        }
        private static Pawn FindBestTrafficCarrier(Caravan trafficCaravan, Pawn fallbackLeader, Thing item)
        {
            if (trafficCaravan == null) return fallbackLeader;
            Pawn best = null;
            float bestFree = float.NegativeInfinity;
            List<Pawn> members = trafficCaravan.PawnsListForReading;
            for (int i = 0; i < members.Count; i++)
            {
                Pawn p = members[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                float free = MassUtility.FreeSpace(p);
                if (best == null || free > bestFree)
                {
                    best = p;
                    bestFree = free;
                }
            }
            return best != null ? best : fallbackLeader;
        }
    }

    /// Keeps what neither side of an undone deal or attack could take back, saves it and returns it when its owner can take it.
    public sealed class WorldTrafficCustody : WorldComponent, IThingHolder
    {
        private ThingOwner<Thing> held;
        private List<Entry> entries = new List<Entry>();
        private const int Interval = 250;

        private sealed class Entry : IExposable
        {
            public Thing thing;
            public Pawn owner;
            public bool player;

            public void ExposeData()
            {
                Scribe_References.Look(ref thing, "thing");
                Scribe_References.Look(ref owner, "owner");
                Scribe_Values.Look(ref player, "player");
            }
        }

        public WorldTrafficCustody(World world) : base(world) { held = new ThingOwner<Thing>(this); }

        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return held; }
        public void GetChildHolders(List<IThingHolder> children) { ThingOwnerUtility.AppendThingHoldersFromThings(children, held); }

        public static WorldTrafficCustody Instance { get { return Find.World == null ? null : Find.World.GetComponent<WorldTrafficCustody>(); } }
        public int HeldCount { get { return held.Count; } }

        public static void Keep(Thing thing, Pawn owner, bool player)
        {
            WorldTrafficCustody custody = Instance;
            if (custody == null || thing == null || thing.Destroyed || custody.held.Contains(thing)) return;
            if (thing.holdingOwner != null) thing.holdingOwner.Remove(thing);
            if (!custody.held.TryAdd(thing, false)) { Log.Error("[Turbo mega mod] Traffic custody refused " + thing.LabelShort + "."); return; }
            custody.entries.Add(new Entry { thing = thing, owner = owner, player = player });
            Log.Warning("[Turbo mega mod] Traffic custody keeps " + thing.LabelShort + " until its owner can take it back.");
        }

        public override void WorldComponentTick()
        {
            if (held.Count > 0 && Find.TickManager.TicksGame % Interval == 0) RetryNow();
        }

        public void RetryNow()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry e = entries[i];
                try { if (e.thing != null && held.Contains(e.thing)) TryReturn(e.thing, e.owner, e.player); }
                catch (Exception ex) { Log.Warning("[Turbo mega mod] Traffic custody retry failed: " + ex.Message); }
                // Thrown out by a failed attempt but not taken by anybody: it stays here.
                if (e.thing != null && !e.thing.Destroyed && !e.thing.Spawned && e.thing.holdingOwner == null && !(e.thing is Pawn && Find.WorldPawns.Contains((Pawn)e.thing))) held.TryAdd(e.thing, false);
                if (e.thing == null || !held.Contains(e.thing)) entries.RemoveAt(i);
            }
        }
        private bool TryReturn(Thing thing, Pawn owner, bool player)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null && owner != null && !owner.Dead && !owner.Destroyed && owner.inventory != null && (owner.holdingOwner != null || owner.Spawned || Find.WorldPawns.Contains(owner)))
            {
                if (TestMode.Enabled && WorldTrafficInteraction.TestRefuseReturn != null && WorldTrafficInteraction.TestRefuseReturn(thing, owner)) return false;
                held.Remove(thing);
                if (owner.inventory.innerContainer.TryAdd(thing, false)) return true;
                held.TryAdd(thing, false);
                return false;
            }
            if (!player)
            {
                // The travelling side is gone: people go on living in the world, goods stay here until a later owner appears.
                if (pawn == null) return false;
                held.Remove(pawn);
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                return Find.WorldPawns.Contains(pawn) || !held.TryAdd(pawn, false);
            }
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                if (caravan.Faction != Faction.OfPlayer || !caravan.Spawned) continue;
                if (pawn != null) { held.Remove(pawn); caravan.AddPawn(pawn, true); if (caravan.ContainsPawn(pawn)) return true; held.TryAdd(pawn, false); continue; }
                foreach (Pawn p in caravan.PawnsListForReading)
                {
                    if (p.inventory == null || p.Dead) continue;
                    held.Remove(thing);
                    if (p.inventory.innerContainer.TryAdd(thing, false)) return true;
                    held.TryAdd(thing, false);
                }
            }
            Map home = Find.AnyPlayerHomeMap;
            if (home == null) return false;
            Thing placed;
            return held.TryDrop(thing, DropCellFinder.TradeDropSpot(home), home, ThingPlaceMode.Near, out placed);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref held, "held", this);
            Scribe_Collections.Look(ref entries, "entries", LookMode.Deep);
            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
            if (held == null) held = new ThingOwner<Thing>(this);
            if (entries == null) entries = new List<Entry>();
            entries.RemoveAll(e => e == null || e.thing == null);
            foreach (Thing thing in held) if (!entries.Any(e => e.thing == thing)) entries.Add(new Entry { thing = thing, player = true });
        }
    }
}
