using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class StrategicOutpost : MapParent, IThingHolder, IThingHolderTickable
    {
        public int level = 1;
        public StrategicOutpostSpecialization specialization = StrategicOutpostSpecialization.Mining;
        public List<string> selectedProductDefNames = new List<string>();
        public Dictionary<string, float> fractionalRemainders = new Dictionary<string, float>();
        public int lastProductionTick = -1;
        public int nextProductionTick = -1;

        // Abstract world personnel/animals are suspended, never cloned
        public bool ShouldTickContents { get { return false; } }

        public int lastShipmentTick = -1;
        public int targetColonyTile = -1;

        // Operational mode: Autonomous vs Physical
        public OutpostOperationalMode operationalMode = OutpostOperationalMode.Autonomous;
        public List<int> deployedStaffPawnIDs = new List<int>();
        public List<int> deployedAnimalPawnIDs = new List<int>();

        // Reconnaissance cached state for Watchtower
        public int lastScannedHostileSettlements = 0;
        public int lastScannedTotalSettlements = 0;

        protected OutpostPawnHolder workersHolder;
        protected OutpostPawnHolder animalsHolder;
        protected OutpostSubHolder rawMaterialsHolder;
        protected OutpostSubHolder producedGoodsHolder;

        public StrategicOutpostDef Def
        {
            get { return def as StrategicOutpostDef; }
        }

        public ThingOwner<Pawn> Workers
        {
            get
            {
                if (workersHolder == null) workersHolder = new OutpostPawnHolder(this);
                return workersHolder.Container;
            }
        }

        public ThingOwner<Pawn> Animals
        {
            get
            {
                if (animalsHolder == null) animalsHolder = new OutpostPawnHolder(this);
                return animalsHolder.Container;
            }
        }

        public ThingOwner<Thing> RawMaterials
        {
            get
            {
                if (rawMaterialsHolder == null) rawMaterialsHolder = new OutpostSubHolder(this, "RawMaterials");
                return rawMaterialsHolder.Container;
            }
        }

        public ThingOwner<Thing> ProducedGoods
        {
            get
            {
                if (producedGoodsHolder == null) producedGoodsHolder = new OutpostSubHolder(this, "ProducedGoods");
                return producedGoodsHolder.Container;
            }
        }

        public int PawnCount
        {
            get
            {
                if (operationalMode == OutpostOperationalMode.Autonomous)
                {
                    return Workers != null ? Workers.Count : 0;
                }
                else
                {
                    return deployedStaffPawnIDs != null ? deployedStaffPawnIDs.Count : 0;
                }
            }
        }

        public IEnumerable<Pawn> AllPawns
        {
            get
            {
                if (operationalMode == OutpostOperationalMode.Autonomous)
                {
                    if (Workers != null && Animals != null)
                    {
                        return Workers.InnerListForReading.Concat(Animals.InnerListForReading);
                    }
                    if (Workers != null) return Workers.InnerListForReading;
                    return Enumerable.Empty<Pawn>();
                }
                else
                {
                    if (HasMap && Map != null)
                    {
                        List<Pawn> list = new List<Pawn>();
                        IReadOnlyList<Pawn> spawned = Map.mapPawns.AllPawnsSpawned;
                        for (int i = 0; i < spawned.Count; i++)
                        {
                            int id = spawned[i].thingIDNumber;
                            if ((deployedStaffPawnIDs != null && deployedStaffPawnIDs.Contains(id)) ||
                                (deployedAnimalPawnIDs != null && deployedAnimalPawnIDs.Contains(id)))
                            {
                                list.Add(spawned[i]);
                            }
                        }
                        return list;
                    }
                    return Enumerable.Empty<Pawn>();
                }
            }
        }

        public int MaxCapacity
        {
            get
            {
                return Def != null ? Def.CalculateMaxCapacity(level) : Math.Max(1, Math.Min(10, level)) + 1;
            }
        }

        public bool CanUpgrade
        {
            get
            {
                int maxLvl = Def != null ? Def.maxLevel : 10;
                return level < Math.Min(10, maxLvl);
            }
        }

        public int NextUpgradeCost
        {
            get
            {
                return Def != null ? Def.CalculateUpgradeCost(level) : 500;
            }
        }

        public StrategicOutpost()
        {
            workersHolder = new OutpostPawnHolder(this);
            animalsHolder = new OutpostPawnHolder(this);
            rawMaterialsHolder = new OutpostSubHolder(this, "RawMaterials");
            producedGoodsHolder = new OutpostSubHolder(this, "ProducedGoods");
            deployedStaffPawnIDs = new List<int>();
            deployedAnimalPawnIDs = new List<int>();
        }

        public override void PostMake()
        {
            base.PostMake();
            if (lastProductionTick < 0)
            {
                lastProductionTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            }
            if (nextProductionTick < 0)
            {
                nextProductionTick = NextDay(lastProductionTick);
            }
            EnsureSelectedProductsInitialized();
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            if (lastProductionTick < 0)
            {
                lastProductionTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            }
            if (nextProductionTick < 0)
            {
                nextProductionTick = NextDay(lastProductionTick);
            }
            EnsureSelectedProductsInitialized();
        }

        private int NextDay(int tick)
        {
            return (int)Math.Min(int.MaxValue, (long)tick + 60000L);
        }

        public void EnsureSelectedProductsInitialized()
        {
            if (selectedProductDefNames == null)
            {
                selectedProductDefNames = new List<string>();
            }
            if (fractionalRemainders == null)
            {
                fractionalRemainders = new Dictionary<string, float>();
            }
            if (deployedStaffPawnIDs == null)
            {
                deployedStaffPawnIDs = new List<int>();
            }
            if (deployedAnimalPawnIDs == null)
            {
                deployedAnimalPawnIDs = new List<int>();
            }
            if (selectedProductDefNames.Count == 0 && specialization != StrategicOutpostSpecialization.Watchtower)
            {
                List<StrategicOutpostProductDef> available = GetAvailableProductsFor(specialization);
                if (available.Count > 0)
                {
                    selectedProductDefNames.Add(available[0].defName);
                }
            }
        }

        public List<StrategicOutpostProductDef> GetAvailableProductsFor(StrategicOutpostSpecialization spec)
        {
            List<StrategicOutpostProductDef> list = new List<StrategicOutpostProductDef>();
            List<StrategicOutpostProductDef> all = DefDatabase<StrategicOutpostProductDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].specialization == spec || (spec == StrategicOutpostSpecialization.Mining && all[i].specialization == StrategicOutpostSpecialization.Logging))
                {
                    list.Add(all[i]);
                }
            }
            return list;
        }

        public Map GetOrGenerateMap()
        {
            if (HasMap && Map != null)
            {
                return Map;
            }
            return GetOrGenerateMapUtility.GetOrGenerateMap(this.Tile, new IntVec3(150, 1, 150), null);
        }

        protected override void Tick()
        {
            base.Tick();

            // Native Ticking Boundary & One Work Per Person Rule:
            // When personnel are in Physical mode, abstract production is paused.
            TryTriggerDailyCycle();
        }

        public void TriggerDailyProductionNow()
        {
            TryTriggerDailyCycle();
        }

        public void DoDailyProduction()
        {
            TryTriggerDailyCycle();
        }

        public bool TryTriggerDailyCycle()
        {
            if (operationalMode == OutpostOperationalMode.Physical)
            {
                return false;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now < nextProductionTick)
            {
                return false;
            }

            ExecuteDailyProduction();
            DoDailyShipment();
            nextProductionTick = NextDay(now);
            lastProductionTick = now;
            return true;
        }

        private int CalculateEffectivePawnSkill(Pawn p, StrategicOutpostProductDef product)
        {
            if (p == null || p.Dead || p.Downed) return 0;
            if (product == null) return 0;
            if (product.workType != null && p.WorkTypeIsDisabled(product.workType)) return 0;
            if (p.skills == null) return 0;
            SkillRecord rec = p.skills.GetSkill(product.relevantSkill);
            if (rec == null || rec.TotallyDisabled || rec.Level <= 0) return 0;
            return Math.Max(0, Math.Min(20, rec.Level));
        }

        public double ExpectedYield(StrategicOutpostProductDef product)
        {
            if (product == null) return 0;
            string reason;
            if (!product.IsUnlocked(out reason)) return 0;
            int activeCount = Math.Max(1, selectedProductDefNames.Count);
            int totalSkill = 0;
            foreach (Pawn p in Workers.InnerListForReading)
            {
                if (product.WorkerCanProduce(p))
                {
                    totalSkill += CalculateEffectivePawnSkill(p, product);
                }
            }
            if (totalSkill <= 0) return 0;
            return (double)totalSkill * product.Rate / (double)activeCount;
        }

        private void ExecuteDailyProduction()
        {
            if (specialization == StrategicOutpostSpecialization.Watchtower)
            {
                PerformWatchtowerReconnaissance();
                return;
            }

            if (selectedProductDefNames == null || selectedProductDefNames.Count == 0) return;

            List<StrategicOutpostProductDef> activeProducts = new List<StrategicOutpostProductDef>();
            for (int i = 0; i < selectedProductDefNames.Count; i++)
            {
                StrategicOutpostProductDef pDef = DefDatabase<StrategicOutpostProductDef>.GetNamedSilentFail(selectedProductDefNames[i]);
                string unlockReason;
                if (pDef != null && pDef.IsUnlocked(out unlockReason))
                {
                    activeProducts.Add(pDef);
                }
            }

            if (activeProducts.Count == 0) return;

            float perProductFactor = 1.0f / (float)activeProducts.Count;

            for (int i = 0; i < activeProducts.Count; i++)
            {
                StrategicOutpostProductDef prod = activeProducts[i];
                int totalEffectiveSkill = 0;

                List<Pawn> workerList = Workers.InnerListForReading;
                for (int w = 0; w < workerList.Count; w++)
                {
                    Pawn worker = workerList[w];
                    if (prod.WorkerCanProduce(worker))
                    {
                        totalEffectiveSkill += CalculateEffectivePawnSkill(worker, prod);
                    }
                }

                if (totalEffectiveSkill <= 0)
                {
                    continue;
                }

                float earnedUnits = (float)((double)totalEffectiveSkill * prod.Rate) * perProductFactor;
                float remainder = 0f;
                fractionalRemainders.TryGetValue(prod.defName, out remainder);

                float totalAmount = earnedUnits + remainder;
                int wholeUnits = Mathf.FloorToInt(totalAmount);
                float keptRemainder = totalAmount - (float)wholeUnits;

                if (wholeUnits > 0)
                {
                    if (prod.specialization == StrategicOutpostSpecialization.Factory)
                    {
                        int possibleUnits = prod.CalculateMaxUnitsFromIngredients(RawMaterials);
                        int unitsToMake = Math.Min(wholeUnits, possibleUnits);
                        if (unitsToMake > 0)
                        {
                            if (prod.ProduceInto(RawMaterials, ProducedGoods, unitsToMake))
                            {
                                fractionalRemainders[prod.defName] = keptRemainder;
                            }
                            else
                            {
                                fractionalRemainders[prod.defName] = remainder;
                            }
                        }
                        else
                        {
                            // Lack of ingredients: do NOT bank whole units into future days!
                            fractionalRemainders[prod.defName] = keptRemainder;
                        }
                    }
                    else
                    {
                        // Use atomic ProduceInto for non-factory products to split stacks by stackLimit
                        // and guarantee zero orphaned / uncontained items!
                        if (prod.ProduceInto(RawMaterials, ProducedGoods, wholeUnits))
                        {
                            fractionalRemainders[prod.defName] = keptRemainder;
                        }
                        else
                        {
                            fractionalRemainders[prod.defName] = remainder;
                        }
                    }
                }
                else
                {
                    fractionalRemainders[prod.defName] = keptRemainder;
                }
            }
        }

        public void PerformWatchtowerReconnaissance()
        {
            if (Find.WorldGrid == null) return;

            int tile = this.Tile;
            int scanRadius = 15;
            int hostileCount = 0;
            int totalCount = 0;

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s == null || !s.Spawned || s.Tile < 0) continue;

                int dist = Find.WorldGrid.TraversalDistanceBetween(tile, s.Tile, true, scanRadius + 1);
                if (dist <= scanRadius)
                {
                    totalCount++;
                    if (s.Faction != null && s.Faction.HostileTo(Faction.OfPlayer))
                    {
                        hostileCount++;
                    }
                }
            }

            lastScannedHostileSettlements = hostileCount;
            lastScannedTotalSettlements = totalCount;
        }

        public void DoDailyShipment()
        {
            if (targetColonyTile < 0) return;

            Settlement targetSettlement = Find.WorldObjects.SettlementAt(targetColonyTile);
            if (targetSettlement == null || targetSettlement.Faction != Faction.OfPlayer || !targetSettlement.Spawned || targetSettlement.Tile.Layer != Find.WorldGrid.Surface)
            {
                // Destination not available: RETAIN goods in outpost cargo, do NOT discard!
                return;
            }

            Map targetMap = targetSettlement.Map;
            if (targetMap == null)
            {
                return;
            }

            List<Thing> thingsToShip = ProducedGoods.InnerListForReading.ToList();
            if (thingsToShip.Count == 0) return;

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(targetMap);
            if (!dropSpot.IsValid)
            {
                dropSpot = DropCellFinder.RandomDropSpot(targetMap);
            }

            bool sent = false;
            foreach (var item in thingsToShip)
            {
                IntVec3 actual;
                if (!DropCellFinder.TryFindDropSpotNear(dropSpot, targetMap, out actual, false, true, true)) continue;
                var taken = ProducedGoods.Take(item, item.stackCount);
                if (taken == null) continue;
                try
                {
                    DropPodUtility.DropThingsNear(actual, targetMap, new[] { taken }, 110, false, false, true, false);
                    if (!taken.Destroyed && taken.MapHeld == targetMap) sent = true;
                }
                finally
                {
                    // The native void API can reject a drop. Retain anything not actually
                    // owned by the receiver map, including an unattached pod's temporary holder.
                    if (!taken.Destroyed && taken.MapHeld != targetMap)
                    {
                        var owner = taken.holdingOwner;
                        if (owner == null)
                        {
                            if (!ProducedGoods.TryAdd(taken, false)) throw new InvalidOperationException("Outpost shipment rollback rejected.");
                        }
                        else if (owner != ProducedGoods && owner.TryTransferToContainer(taken, ProducedGoods, taken.stackCount, false) <= 0)
                            throw new InvalidOperationException("Outpost shipment holder recovery rejected.");
                    }
                }
            }
            if (sent)
            {
                lastShipmentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                Messages.Message("StrategicOutpost_ShipmentDelivered".Translate(LabelCap, targetSettlement.LabelCap), targetSettlement, MessageTypeDefOf.PositiveEvent);
            }
        }

        public bool TryUpgrade(out string failReason, Caravan localCaravan = null)
        {
            if (!CanUpgrade)
            {
                failReason = "StrategicOutpost_MaxLevelReached".Translate();
                return false;
            }

            // Co-location & Ownership check for caravan silver
            if (localCaravan != null)
            {
                if (localCaravan.Faction != Faction.OfPlayer || localCaravan.Tile != this.Tile || !localCaravan.Spawned || !CanInteract(localCaravan))
                {
                    localCaravan = null; // Remote, foreign, or invalid caravan cannot be tapped as an ATM!
                }
            }

            int targetLevel = level + 1;
            CellRect expansionRect = CellRect.Empty;

            // Spatial check BEFORE taking silver if map is active
            if (HasMap && Map != null)
            {
                StrategicOutpostMapComponent comp = Map.GetComponent<StrategicOutpostMapComponent>();
                if (comp != null)
                {
                    if (!comp.TryFindExpansionRect(targetLevel, out expansionRect) || !comp.CanSafelyPlaceModule(expansionRect, targetLevel))
                    {
                        failReason = "StrategicOutpost_UpgradeBlockedNoSpace".Translate();
                        return false;
                    }
                }
            }

            int cost = NextUpgradeCost;
            int availableInOutpost = GetTotalSilver(RawMaterials);
            int availableInCaravan = localCaravan != null ? GetTotalSilver(localCaravan) : 0;
            int totalSilver = availableInOutpost + availableInCaravan;

            if (totalSilver < cost)
            {
                failReason = "StrategicOutpost_NotEnoughSilver".Translate(cost, totalSilver);
                return false;
            }

            // If map is active, execute build NOW before taking silver
            if (HasMap && Map != null && !expansionRect.IsEmpty)
            {
                StrategicOutpostMapComponent comp = Map.GetComponent<StrategicOutpostMapComponent>();
                if (comp != null)
                {
                    string buildFail;
                    if (!comp.BuildModuleForLevel(targetLevel, expansionRect, specialization, out buildFail))
                    {
                        failReason = "StrategicOutpost_UpgradeBlockedNoSpace".Translate();
                        return false; // Zero silver deducted, no map mutations, clean abort!
                    }
                }
            }

            // Deduct silver atomically only after map module successfully spawned (or if virtual without map)
            int needed = cost;
            int takenFromOutpost = TakeSilverFromStorage(RawMaterials, needed);
            needed -= takenFromOutpost;

            if (needed > 0 && localCaravan != null)
            {
                TakeSilverFromCaravan(localCaravan, needed);
            }

            level = targetLevel;

            failReason = null;
            Messages.Message("StrategicOutpost_UpgradeSuccess".Translate(LabelCap, level, MaxCapacity), this, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public bool DeployPersonnelToMap(out string failReason)
        {
            if (operationalMode == OutpostOperationalMode.Physical)
            {
                failReason = "StrategicOutpost_AlreadyPhysical".Translate();
                return false;
            }

            if (Workers.Count == 0)
            {
                failReason = "StrategicOutpost_NoWorkersToDeploy".Translate();
                return false;
            }

            Map map = GetOrGenerateMap();
            if (map == null)
            {
                failReason = "StrategicOutpost_MapGenFailed".Translate();
                return false;
            }

            StrategicOutpostMapComponent comp = map.GetComponent<StrategicOutpostMapComponent>();
            IntVec3 center = comp != null && comp.baseCenter.IsValid ? comp.baseCenter : map.Center;

            List<Pawn> staffToDeploy = Workers.InnerListForReading.ToList();
            List<Pawn> animalsToDeploy = Animals.InnerListForReading.ToList();

            // Pre-validate all spawn cells before removing any pawn from storage
            List<IntVec3> staffCells = new List<IntVec3>();
            List<IntVec3> animalCells = new List<IntVec3>();

            for (int i = 0; i < staffToDeploy.Count; i++)
            {
                IntVec3 spawnCell;
                if (!CellFinder.TryFindRandomCellNear(center, map, 8, c => c.Standable(map) && !c.Fogged(map), out spawnCell))
                {
                    failReason = "StrategicOutpost_CannotFindSpawnCell".Translate();
                    return false;
                }
                staffCells.Add(spawnCell);
            }

            for (int i = 0; i < animalsToDeploy.Count; i++)
            {
                IntVec3 spawnCell;
                if (!CellFinder.TryFindRandomCellNear(center, map, 8, c => c.Standable(map) && !c.Fogged(map), out spawnCell))
                {
                    failReason = "StrategicOutpost_CannotFindSpawnCell".Translate();
                    return false;
                }
                animalCells.Add(spawnCell);
            }

            if (deployedStaffPawnIDs == null) deployedStaffPawnIDs = new List<int>();
            if (deployedAnimalPawnIDs == null) deployedAnimalPawnIDs = new List<int>();
            deployedStaffPawnIDs.Clear();
            deployedAnimalPawnIDs.Clear();

            List<Pawn> successfullySpawnedStaff = new List<Pawn>();
            for (int i = 0; i < staffToDeploy.Count; i++)
            {
                Pawn p = staffToDeploy[i];
                Workers.Remove(p);
                Pawn spawned = GenSpawn.Spawn(p, staffCells[i], map, Rot4.South, WipeMode.Vanish, false, false) as Pawn;
                if (spawned == null || !spawned.Spawned)
                {
                    // Rollback spawned pawns
                    for (int s = 0; s < successfullySpawnedStaff.Count; s++)
                    {
                        Pawn sp = successfullySpawnedStaff[s];
                        sp.DeSpawn(DestroyMode.Vanish);
                        Workers.TryAdd(sp, false);
                    }
                    Workers.TryAdd(p, false);
                    deployedStaffPawnIDs.Clear();
                    failReason = "StrategicOutpost_CannotFindSpawnCell".Translate();
                    return false;
                }
                successfullySpawnedStaff.Add(spawned);
                deployedStaffPawnIDs.Add(p.thingIDNumber);
            }

            for (int i = 0; i < animalsToDeploy.Count; i++)
            {
                Pawn a = animalsToDeploy[i];
                Animals.Remove(a);
                GenSpawn.Spawn(a, animalCells[i], map, Rot4.South, WipeMode.Vanish, false, false);
                deployedAnimalPawnIDs.Add(a.thingIDNumber);
            }

            operationalMode = OutpostOperationalMode.Physical;
            failReason = null;
            Messages.Message("StrategicOutpost_PersonnelDeployed".Translate(LabelCap, deployedStaffPawnIDs.Count), this, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public bool RecallPersonnelToAutonomous(out string failReason)
        {
            if (operationalMode == OutpostOperationalMode.Autonomous)
            {
                failReason = "StrategicOutpost_AlreadyAutonomous".Translate();
                return false;
            }

            if (HasMap && Map != null)
            {
                // Combat Threat Guard
                if (GenHostility.AnyHostileActiveThreatTo(Map, Faction.OfPlayer))
                {
                    failReason = "StrategicOutpost_CannotRecallDuringCombat".Translate();
                    return false;
                }

                // Mental Break Guard
                IReadOnlyList<Pawn> spawned = Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawned.Count; i++)
                {
                    Pawn p = spawned[i];
                    if (deployedStaffPawnIDs != null && deployedStaffPawnIDs.Contains(p.thingIDNumber))
                    {
                        if (p.InMentalState || p.MentalStateDef != null)
                        {
                            failReason = "StrategicOutpost_CannotRecallMentalBreak".Translate(p.LabelShort);
                            return false;
                        }
                    }
                }
            }

            if (HasMap && Map != null)
            {
                var roster = new HashSet<int>((deployedStaffPawnIDs ?? new List<int>()).Concat(deployedAnimalPawnIDs ?? new List<int>()));
                var onMap = new HashSet<int>(Map.mapPawns.AllPawnsSpawned.Select(p => p.thingIDNumber));
                if (roster.Any(id => !onMap.Contains(id) && Find.WorldPawns.AllPawnsAliveOrDead.Any(p => p.thingIDNumber == id && !p.Dead)))
                {
                    failReason = "StrategicOutpost_RosterUnavailable".Translate();
                    return false;
                }
            }
            // Safely gather staff and animal pawns with atomic rollback
            if (HasMap && Map != null)
            {
                List<Pawn> spawnedSnapshot = Map.mapPawns.AllPawnsSpawned.ToList();
                List<Pawn> recalledStaff = new List<Pawn>();
                List<Pawn> recalledAnimals = new List<Pawn>();
                Dictionary<Pawn, IntVec3> priorPositions = new Dictionary<Pawn, IntVec3>();
                Dictionary<Pawn, Rot4> priorRotations = new Dictionary<Pawn, Rot4>();

                for (int i = 0; i < spawnedSnapshot.Count; i++)
                {
                    Pawn p = spawnedSnapshot[i];
                    if (p.Dead || !p.Spawned) continue;
                    int id = p.thingIDNumber;

                    if (deployedStaffPawnIDs != null && deployedStaffPawnIDs.Contains(id))
                    {
                        priorPositions[p] = p.Position;
                        priorRotations[p] = p.Rotation;

                        // Cleanup jobs and drop carried things
                        if (p.jobs != null) p.jobs.StopAll(false, true);
                        if (p.pather != null) p.pather.StopDead();
                        if (p.carryTracker != null && p.carryTracker.CarriedThing != null)
                        {
                            Thing dropped;
                            p.carryTracker.TryDropCarriedThing(p.Position, ThingPlaceMode.Near, out dropped);
                        }

                        p.DeSpawn(DestroyMode.Vanish);
                        if (!Workers.TryAdd(p, false))
                        {
                            // Rollback
                            GenSpawn.Spawn(p, priorPositions[p], Map, priorRotations[p], WipeMode.Vanish, false, false);
                            foreach (Pawn r in recalledStaff)
                            {
                                Workers.Remove(r);
                                GenSpawn.Spawn(r, priorPositions[r], Map, priorRotations[r], WipeMode.Vanish, false, false);
                            }
                            failReason = "Failed to add recalled pawn to Workers storage.";
                            return false;
                        }
                        SetStoredPawnProtection(p, true);
                        recalledStaff.Add(p);
                    }
                    else if (deployedAnimalPawnIDs != null && deployedAnimalPawnIDs.Contains(id))
                    {
                        priorPositions[p] = p.Position;
                        priorRotations[p] = p.Rotation;

                        if (p.jobs != null) p.jobs.StopAll(false, true);
                        if (p.pather != null) p.pather.StopDead();
                        if (p.carryTracker != null && p.carryTracker.CarriedThing != null)
                        {
                            Thing dropped;
                            p.carryTracker.TryDropCarriedThing(p.Position, ThingPlaceMode.Near, out dropped);
                        }

                        p.DeSpawn(DestroyMode.Vanish);
                        if (!Animals.TryAdd(p, false))
                        {
                            GenSpawn.Spawn(p, priorPositions[p], Map, priorRotations[p], WipeMode.Vanish, false, false);
                            foreach (Pawn r in recalledAnimals)
                            {
                                Animals.Remove(r);
                                GenSpawn.Spawn(r, priorPositions[r], Map, priorRotations[r], WipeMode.Vanish, false, false);
                            }
                            failReason = "Failed to add recalled animal to Animals storage.";
                            return false;
                        }
                        SetStoredPawnProtection(p, true);
                        recalledAnimals.Add(p);
                    }
                }
            }

            if (deployedStaffPawnIDs != null) deployedStaffPawnIDs.Clear();
            if (deployedAnimalPawnIDs != null) deployedAnimalPawnIDs.Clear();

            operationalMode = OutpostOperationalMode.Autonomous;

            // Reset production deadline so physical time is not retroactively charged
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            lastProductionTick = now;
            nextProductionTick = NextDay(now);

            failReason = null;
            Messages.Message("StrategicOutpost_PersonnelRecalled".Translate(LabelCap, Workers.Count), this, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public bool UnloadCargoToMap(out string report)
        {
            if (!HasMap || Map == null)
            {
                report = "No active map to unload cargo.";
                return false;
            }

            StrategicOutpostMapComponent comp = Map.GetComponent<StrategicOutpostMapComponent>();
            IntVec3 dropCell = comp != null && comp.supplyDepotCell.IsValid ? comp.supplyDepotCell : Map.Center;

            int totalUnloaded = 0;

            List<Thing> prod = ProducedGoods.InnerListForReading.ToList();
            for (int i = 0; i < prod.Count; i++)
            {
                Thing t = prod[i];
                ProducedGoods.Remove(t);
                GenPlace.TryPlaceThing(t, dropCell, Map, ThingPlaceMode.Near);
                totalUnloaded++;
            }

            if (totalUnloaded > 0)
            {
                report = "StrategicOutpost_CargoUnloaded".Translate(totalUnloaded);
                Messages.Message(report, this, MessageTypeDefOf.PositiveEvent);
                return true;
            }
            else
            {
                report = "StrategicOutpost_NoCargoToUnload".Translate();
                return false;
            }
        }

        public bool CanInteract(Caravan caravan)
        {
            return caravan != null && caravan.Spawned && caravan.Tile == this.Tile && caravan.Faction == Faction.OfPlayer;
        }

        public int DepositFromCaravan(Caravan caravan, Thing item)
        {
            if (!CanInteract(caravan) || item == null || item.def.category != ThingCategory.Item || item.holdingOwner == null || !CaravanInventoryUtility.AllInventoryItems(caravan).Contains(item)) return 0;
            return item.holdingOwner.TryTransferToContainer(item, RawMaterials, item.stackCount, false);
        }

        public int WithdrawToCaravan(Caravan caravan, ThingOwner<Thing> storage, Thing item)
        {
            if (!CanInteract(caravan) || storage == null || item == null || !storage.Contains(item)) return 0;
            int moved = 0;
            foreach (var pawn in caravan.PawnsListForReading.Where(p => p.inventory != null && MassUtility.CanEverCarryAnything(p)))
            {
                float mass = item.GetStatValue(StatDefOf.Mass);
                int count = mass <= 0 ? item.stackCount : Math.Min(item.stackCount, (int)Math.Floor(Math.Max(0, MassUtility.FreeSpace(pawn)) / mass));
                if (count > 0) moved += storage.TryTransferToContainer(item, pawn.inventory.innerContainer, count, false);
                if (!storage.Contains(item)) break;
            }
            if (moved == 0 && storage.Contains(item))
            {
                Messages.Message("StrategicOutpost_CaravanTooHeavy".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
            }
            return moved;
        }

        private int GetTotalSilver(ThingOwner<Thing> storage)
        {
            if (storage == null) return 0;
            return storage.TotalStackCountOfDef(ThingDefOf.Silver);
        }

        private int GetTotalSilver(Caravan caravan)
        {
            if (caravan == null) return 0;
            int sum = 0;
            List<Thing> inv = CaravanInventoryUtility.AllInventoryItems(caravan);
            for (int i = 0; i < inv.Count; i++)
            {
                if (inv[i].def == ThingDefOf.Silver)
                {
                    sum += inv[i].stackCount;
                }
            }
            return sum;
        }

        private int TakeSilverFromStorage(ThingOwner<Thing> storage, int count)
        {
            if (storage == null || count <= 0) return 0;
            int remaining = count;
            for (int i = storage.Count - 1; i >= 0; i--)
            {
                Thing t = storage[i];
                if (t.def == ThingDefOf.Silver)
                {
                    int take = Mathf.Min(remaining, t.stackCount);
                    Thing taken = storage.Take(t, take);
                    if (taken != null) taken.Destroy();
                    remaining -= take;
                    if (remaining <= 0) break;
                }
            }
            return count - remaining;
        }

        private int TakeSilverFromCaravan(Caravan caravan, int count)
        {
            if (caravan == null || count <= 0) return 0;
            int remaining = count;
            List<Thing> inv = CaravanInventoryUtility.AllInventoryItems(caravan);
            for (int i = 0; i < inv.Count; i++)
            {
                Thing t = inv[i];
                if (t.def == ThingDefOf.Silver)
                {
                    int take = Mathf.Min(remaining, t.stackCount);
                    t.SplitOff(take);
                    remaining -= take;
                    if (remaining <= 0) break;
                }
            }
            return count - remaining;
        }

        internal static void SetStoredPawnProtection(Pawn pawn, bool stored)
        {
            if (pawn == null || pawn.Spawned || pawn.Destroyed) return;
            // Caravan membership is a vanilla GC root; our independent animal holder is not.
            // Re-register through public APIs to apply/release KeepForever only while stationed.
            if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
            Find.WorldPawns.PassToWorld(pawn, stored ? PawnDiscardDecideMode.KeepForever : PawnDiscardDecideMode.Decide);
        }

        public bool AddPawnFromCaravan(Caravan caravan, Pawn pawn, out string failReason)
        {
            if (!CanInteract(caravan) || pawn == null)
            {
                failReason = "StrategicOutpost_InvalidCaravanOrPawn".Translate();
                return false;
            }

            if (!caravan.ContainsPawn(pawn) || pawn.Dead)
            {
                failReason = "StrategicOutpost_PawnNotAvailable".Translate();
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                failReason = "StrategicOutpost_NotPlayerFaction".Translate();
                return false;
            }

            if (!pawn.RaceProps.Animal && !StrategicOutpostCaravanUtility.IsSuitableColonist(pawn))
            {
                failReason = "StrategicOutpost_PawnNotSuitable".Translate();
                return false;
            }

            int currentStaff = Workers.Count + (deployedStaffPawnIDs != null ? deployedStaffPawnIDs.Count : 0);
            if (!pawn.RaceProps.Animal && currentStaff >= MaxCapacity)
            {
                failReason = "StrategicOutpost_CapacityFull".Translate(MaxCapacity);
                return false;
            }

            caravan.RemovePawn(pawn);

            bool added = false;
            if (pawn.RaceProps.Animal)
            {
                if (operationalMode == OutpostOperationalMode.Physical && HasMap && Map != null)
                {
                    StrategicOutpostMapComponent comp = Map.GetComponent<StrategicOutpostMapComponent>();
                    IntVec3 center = comp != null && comp.baseCenter.IsValid ? comp.baseCenter : Map.Center;
                    IntVec3 c;
                    if (CellFinder.TryFindRandomCellNear(center, Map, 8, cell => cell.Standable(Map) && !cell.Fogged(Map), out c))
                    {
                        GenSpawn.Spawn(pawn, c, Map, Rot4.South, WipeMode.Vanish, false, false);
                        if (deployedAnimalPawnIDs == null) deployedAnimalPawnIDs = new List<int>();
                        deployedAnimalPawnIDs.Add(pawn.thingIDNumber);
                        added = true;
                    }
                    else
                    {
                        added = Animals.TryAdd(pawn, false);
                    }
                }
                else
                {
                    added = Animals.TryAdd(pawn, false);
                }
            }
            else
            {
                if (operationalMode == OutpostOperationalMode.Physical && HasMap && Map != null)
                {
                    StrategicOutpostMapComponent comp = Map.GetComponent<StrategicOutpostMapComponent>();
                    IntVec3 center = comp != null && comp.baseCenter.IsValid ? comp.baseCenter : Map.Center;
                    IntVec3 c;
                    if (CellFinder.TryFindRandomCellNear(center, Map, 8, cell => cell.Standable(Map) && !cell.Fogged(Map), out c))
                    {
                        GenSpawn.Spawn(pawn, c, Map, Rot4.South, WipeMode.Vanish, false, false);
                        if (deployedStaffPawnIDs == null) deployedStaffPawnIDs = new List<int>();
                        deployedStaffPawnIDs.Add(pawn.thingIDNumber);
                        added = true;
                    }
                    else
                    {
                        added = Workers.TryAdd(pawn, false);
                    }
                }
                else
                {
                    added = Workers.TryAdd(pawn, false);
                }
            }

            if (!added)
            {
                caravan.AddPawn(pawn, false);
                failReason = "StrategicOutpost_FailedToAddPawn".Translate();
                return false;
            }

            SetStoredPawnProtection(pawn, true);
            failReason = null;
            return true;
        }

        public bool RemovePawnToCaravan(Caravan caravan, Pawn pawn, out string failReason)
        {
            if (!CanInteract(caravan) || pawn == null)
            {
                failReason = "StrategicOutpost_InvalidCaravanOrPawn".Translate();
                return false;
            }

            bool inWorkers = Workers.Contains(pawn);
            bool inAnimals = Animals.Contains(pawn);

            if (!inWorkers && !inAnimals)
            {
                failReason = "StrategicOutpost_PawnNotFound".Translate();
                return false;
            }

            if (inWorkers && Workers.Count <= 1)
            {
                failReason = "StrategicOutpost_CannotRemoveLastWorker".Translate();
                return false;
            }

            if (inWorkers)
            {
                Workers.Remove(pawn);
            }
            else
            {
                Animals.Remove(pawn);
            }

            caravan.AddPawn(pawn, false);
            if (caravan.ContainsPawn(pawn)) SetStoredPawnProtection(pawn, false);
            failReason = null;
            return true;
        }

        public void AbandonAndEvacuate(Caravan caravan)
        {
            if (!CanInteract(caravan) || caravan.pather.Moving || operationalMode == OutpostOperationalMode.Physical ||
                (HasMap && Map.mapPawns.AnyColonistSpawned))
            {
                Messages.Message("StrategicOutpost_CannotEvacuateActiveMap".Translate(), this, MessageTypeDefOf.RejectInput, false);
                return;
            }
            foreach (var pawn in Workers.InnerListForReading.Concat(Animals.InnerListForReading).ToArray())
            {
                var owner = pawn.holdingOwner;
                if (owner == null || !owner.Remove(pawn)) return;
                caravan.AddPawn(pawn, false);
                if (!caravan.ContainsPawn(pawn))
                {
                    if (!owner.TryAdd(pawn, false)) throw new InvalidOperationException("Outpost evacuation pawn rollback rejected.");
                    return;
                }
                SetStoredPawnProtection(pawn, false);
            }
            foreach (var thing in RawMaterials.InnerListForReading.ToArray()) WithdrawToCaravan(caravan, RawMaterials, thing);
            foreach (var thing in ProducedGoods.InnerListForReading.ToArray()) WithdrawToCaravan(caravan, ProducedGoods, thing);
            if (Workers.Count != 0 || Animals.Count != 0 || RawMaterials.Count != 0 || ProducedGoods.Count != 0)
            {
                Messages.Message("StrategicOutpost_CargoRemains".Translate(), this, MessageTypeDefOf.RejectInput, false);
                return;
            }
            // Preserve the actual loaded map and its player-built structures. A dedicated
            // native map-abandonment flow is separate from evacuating the stored workforce.
            if (HasMap)
            {
                Messages.Message("StrategicOutpost_MapRetained".Translate(), this, MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            Find.WorldObjects.Remove(this);
            Messages.Message("StrategicOutpost_Evacuated".Translate(LabelCap), MessageTypeDefOf.NeutralEvent);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref specialization, "specialization", StrategicOutpostSpecialization.Mining);
            Scribe_Values.Look(ref operationalMode, "operationalMode", OutpostOperationalMode.Autonomous);
            Scribe_Collections.Look(ref deployedStaffPawnIDs, "deployedStaffPawnIDs", LookMode.Value);
            Scribe_Collections.Look(ref deployedAnimalPawnIDs, "deployedAnimalPawnIDs", LookMode.Value);

            Scribe_Collections.Look(ref selectedProductDefNames, "selectedProductDefNames", LookMode.Value);
            Scribe_Collections.Look(ref fractionalRemainders, "fractionalRemainders", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref lastProductionTick, "lastProductionTick", -1);
            Scribe_Values.Look(ref nextProductionTick, "nextProductionTick", -1);
            Scribe_Values.Look(ref lastShipmentTick, "lastShipmentTick", -1);
            Scribe_Values.Look(ref targetColonyTile, "targetColonyTile", -1);
            Scribe_Values.Look(ref lastScannedHostileSettlements, "lastScannedHostileSettlements", 0);
            Scribe_Values.Look(ref lastScannedTotalSettlements, "lastScannedTotalSettlements", 0);

            Scribe_Deep.Look(ref workersHolder, "workersHolder", this);
            Scribe_Deep.Look(ref animalsHolder, "animalsHolder", this);
            Scribe_Deep.Look(ref rawMaterialsHolder, "rawMaterialsHolder", this, "RawMaterials");
            Scribe_Deep.Look(ref producedGoodsHolder, "producedGoodsHolder", this, "ProducedGoods");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (workersHolder == null) workersHolder = new OutpostPawnHolder(this);
                if (animalsHolder == null) animalsHolder = new OutpostPawnHolder(this);
                if (rawMaterialsHolder == null) rawMaterialsHolder = new OutpostSubHolder(this, "RawMaterials");
                if (producedGoodsHolder == null) producedGoodsHolder = new OutpostSubHolder(this, "ProducedGoods");
                if (deployedStaffPawnIDs == null) deployedStaffPawnIDs = new List<int>();
                if (deployedAnimalPawnIDs == null) deployedAnimalPawnIDs = new List<int>();

                level = Math.Max(1, Math.Min(10, level));
                if (fractionalRemainders != null)
                {
                    fractionalRemainders = fractionalRemainders
                        .Where(p => !float.IsNaN(p.Value) && !float.IsInfinity(p.Value))
                        .ToDictionary(p => p.Key, p => Math.Max(0, Math.Min(0.999999f, p.Value)));
                }
                EnsureSelectedProductsInitialized();
            }
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            string baseStr = base.GetInspectString();
            if (!string.IsNullOrEmpty(baseStr))
            {
                sb.AppendLine(baseStr);
            }

            sb.AppendLine("StrategicOutpost_InspectLevel".Translate(level, 10, MaxCapacity));
            sb.AppendLine("StrategicOutpost_InspectSpecialization".Translate(("StrategicOutpost_Spec_" + specialization.ToString()).Translate()));

            string modeStr = operationalMode == OutpostOperationalMode.Autonomous ?
                "StrategicOutpost_Mode_Autonomous".Translate().ToString() :
                "StrategicOutpost_Mode_Physical".Translate().ToString();
            sb.AppendLine("StrategicOutpost_Mode_Label".Translate(modeStr));

            sb.AppendLine("StrategicOutpost_InspectWorkers".Translate(PawnCount, MaxCapacity));
            if (Animals.Count > 0)
            {
                sb.AppendLine("StrategicOutpost_Animals".Translate(Animals.Count));
            }

            if (specialization == StrategicOutpostSpecialization.Watchtower)
            {
                sb.AppendLine("StrategicOutpost_InspectRecon".Translate(lastScannedHostileSettlements, lastScannedTotalSettlements));
            }

            return sb.ToString().TrimEndNewlines();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            // Manage outpost
            Command_Action manageAction = new Command_Action();
            manageAction.defaultLabel = "StrategicOutpost_ManageOutpost_Label".Translate();
            manageAction.defaultDesc = "StrategicOutpost_ManageOutpost_Desc".Translate();
            manageAction.icon = ContentFinder<Texture2D>.Get("UI/Commands/ManageOutpost", false) ?? TexCommand.ClearPrioritizedWork;
            manageAction.action = delegate
            {
                Find.WindowStack.Add(new Dialog_StrategicOutpost(this, null));
            };
            yield return manageAction;

            // Visit / View Map
            if (HasMap && Map != null)
            {
                Command_Action viewMapAction = new Command_Action();
                viewMapAction.defaultLabel = "StrategicOutpost_ViewMap_Label".Translate();
                viewMapAction.defaultDesc = "StrategicOutpost_ViewMap_Desc".Translate();
                viewMapAction.icon = ContentFinder<Texture2D>.Get("UI/Commands/ViewMap", false) ?? TexCommand.ClearPrioritizedWork;
                viewMapAction.action = delegate
                {
                    CameraJumper.TryJump(Map.Center, Map);
                };
                yield return viewMapAction;
            }

            // Mode toggle: Deploy / Recall
            if (operationalMode == OutpostOperationalMode.Autonomous)
            {
                Command_Action deployAction = new Command_Action();
                deployAction.defaultLabel = "StrategicOutpost_DeployPersonnel_Label".Translate();
                deployAction.defaultDesc = "StrategicOutpost_DeployPersonnel_Desc".Translate();
                deployAction.icon = ContentFinder<Texture2D>.Get("UI/Commands/DeployPersonnel", false) ?? TexCommand.Draft;
                deployAction.action = delegate
                {
                    string failReason;
                    if (!DeployPersonnelToMap(out failReason))
                    {
                        Messages.Message(failReason, this, MessageTypeDefOf.RejectInput, false);
                    }
                };
                yield return deployAction;
            }
            else
            {
                Command_Action recallAction = new Command_Action();
                recallAction.defaultLabel = "StrategicOutpost_RecallPersonnel_Label".Translate();
                recallAction.defaultDesc = "StrategicOutpost_RecallPersonnel_Desc".Translate();
                recallAction.icon = ContentFinder<Texture2D>.Get("UI/Commands/RecallPersonnel", false) ?? TexCommand.ClearPrioritizedWork;
                recallAction.action = delegate
                {
                    string failReason;
                    if (!RecallPersonnelToAutonomous(out failReason))
                    {
                        Messages.Message(failReason, this, MessageTypeDefOf.RejectInput, false);
                    }
                };
                yield return recallAction;
            }

            // Upgrade Outpost
            if (CanUpgrade)
            {
                Command_Action upgradeAction = new Command_Action();
                upgradeAction.defaultLabel = "StrategicOutpost_UpgradeAction_Label".Translate(level + 1);
                upgradeAction.defaultDesc = "StrategicOutpost_UpgradeAction_Desc".Translate(NextUpgradeCost);
                upgradeAction.icon = ContentFinder<Texture2D>.Get("UI/Commands/UpgradeOutpost", false) ?? TexCommand.ClearPrioritizedWork;
                upgradeAction.action = delegate
                {
                    string failReason;
                    if (!TryUpgrade(out failReason, null))
                    {
                        Messages.Message(failReason, this, MessageTypeDefOf.RejectInput, false);
                    }
                };
                yield return upgradeAction;
            }
        }

        public new ThingOwner GetDirectlyHeldThings()
        {
            return null;
        }

        public override void GetChildHolders(List<IThingHolder> outChildren)
        {
            base.GetChildHolders(outChildren);
            if (workersHolder != null)
            {
                outChildren.Add(workersHolder);
            }
            if (animalsHolder != null)
            {
                outChildren.Add(animalsHolder);
            }
            if (rawMaterialsHolder != null)
            {
                outChildren.Add(rawMaterialsHolder);
            }
            if (producedGoodsHolder != null)
            {
                outChildren.Add(producedGoodsHolder);
            }
        }
    }
}
