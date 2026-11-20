using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public static class OutpostPodLogistics
    {
        public const float PodMassCapacity = 150f;
        public const float FuelPerTilePerPod = 2.25f;

        public static Map GetHomeMap()
        {
            if (Find.Maps == null) return null;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m != null && m.IsPlayerHome) return m;
            }
            return Find.AnyPlayerHomeMap;
        }

        /// <summary>
        /// Counts unforbidden resources in colony stockpiles, falling back to all unforbidden on map.
        /// </summary>
        public static int CountResource(Map map, ThingDef def)
        {
            if (map == null || def == null) return 0;
            int count = StoryPayment.CountInColonyStockpiles(map, def);
            if (count > 0) return count;

            if (map.listerThings == null) return 0;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t != null && !t.Destroyed && !t.IsForbidden(Faction.OfPlayer))
                {
                    total += t.stackCount;
                }
            }
            return total;
        }

        /// <summary>
        /// Atomically deducts from colony stockpiles, falling back to map items if not in storage.
        /// </summary>
        public static int DeductResource(Map map, ThingDef def, int count)
        {
            if (map == null || def == null || count <= 0) return 0;
            int deducted;
            if (StoryPayment.TryDeductFromColonyStockpiles(map, def, count, out deducted))
            {
                return deducted;
            }
            if (map.listerThings == null) return 0;
            int remaining = count;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t != null && !t.Destroyed && !t.IsForbidden(Faction.OfPlayer))
                {
                    int take = Mathf.Min(remaining, t.stackCount);
                    Thing split = t.SplitOff(take);
                    if (split != null && !split.Destroyed)
                    {
                        split.Destroy(DestroyMode.Vanish);
                    }
                    remaining -= take;
                    if (remaining <= 0) break;
                }
            }
            return count - remaining;
        }

        public static int CalculatePodCount(float mass)
        {
            if (mass <= 0f) return 1;
            return Mathf.Max(1, Mathf.CeilToInt(mass / PodMassCapacity));
        }

        public static int CalculateTotalFuelNeeded(int originTile, int destTile, int podCount)
        {
            float dist = 1f;
            if (Find.WorldGrid != null && originTile >= 0 && destTile >= 0)
            {
                dist = Find.WorldGrid.ApproxDistanceInTiles(originTile, destTile);
            }
            dist = Mathf.Max(1f, dist);
            int fuelPerPod = Mathf.CeilToInt(dist * FuelPerTilePerPod);
            return fuelPerPod * Mathf.Max(1, podCount);
        }

        public static void CalculateCost(int originTile, int destTile, float mass, Map homeMap,
            out int totalFuelNeeded, out int chemfuelToDeduct, out int silverToDeduct, out bool canAfford, out string costSummary)
        {
            int pods = CalculatePodCount(mass);
            totalFuelNeeded = CalculateTotalFuelNeeded(originTile, destTile, pods);

            int availFuel = CountResource(homeMap, ThingDefOf.Chemfuel);
            int availSilver = CountResource(homeMap, ThingDefOf.Silver);

            chemfuelToDeduct = Mathf.Min(totalFuelNeeded, availFuel);
            int missingFuel = totalFuelNeeded - chemfuelToDeduct;

            float fuelPrice = ThingDefOf.Chemfuel != null ? ThingDefOf.Chemfuel.BaseMarketValue : 2.3f;
            silverToDeduct = missingFuel > 0 ? Mathf.CeilToInt((float)missingFuel * fuelPrice) : 0;

            canAfford = availSilver >= silverToDeduct;

            if (silverToDeduct > 0)
            {
                costSummary = "StrategicOutpost_PodCostWithSilver".Translate(totalFuelNeeded, chemfuelToDeduct, silverToDeduct, availFuel, availSilver);
            }
            else
            {
                costSummary = "StrategicOutpost_PodCostFuelOnly".Translate(totalFuelNeeded, availFuel);
            }
        }

        public static bool TryPayLaunchCost(Map homeMap, int originTile, int destTile, float mass, out string failReason)
        {
            if (homeMap == null)
            {
                failReason = "StrategicOutpost_NoHomeMapAvailable".Translate();
                return false;
            }

            int totalFuel, fuelDeduct, silverDeduct;
            bool canAfford;
            string summary;
            CalculateCost(originTile, destTile, mass, homeMap, out totalFuel, out fuelDeduct, out silverDeduct, out canAfford, out summary);

            if (!canAfford)
            {
                failReason = summary;
                return false;
            }

            List<StoryPayment.ResourceRequirement> reqs = new List<StoryPayment.ResourceRequirement>();
            if (fuelDeduct > 0)
            {
                reqs.Add(new StoryPayment.ResourceRequirement(ThingDefOf.Chemfuel, fuelDeduct));
            }
            if (silverDeduct > 0)
            {
                reqs.Add(new StoryPayment.ResourceRequirement(ThingDefOf.Silver, silverDeduct));
            }

            if (reqs.Count > 0 && !StoryPayment.TryDeductMultipleFromColonyStockpiles(homeMap, reqs))
            {
                if (CountResource(homeMap, ThingDefOf.Chemfuel) >= fuelDeduct &&
                    CountResource(homeMap, ThingDefOf.Silver) >= silverDeduct)
                {
                    if (fuelDeduct > 0) DeductResource(homeMap, ThingDefOf.Chemfuel, fuelDeduct);
                    if (silverDeduct > 0) DeductResource(homeMap, ThingDefOf.Silver, silverDeduct);
                }
                else
                {
                    failReason = summary;
                    return false;
                }
            }

            failReason = null;
            return true;
        }

        public static TravellingTransporters Launch(PlanetTile originTile, PlanetTile destTile, List<Thing> cargo, TransportersArrivalAction arrivalAction)
        {
            if (cargo == null || cargo.Count == 0) return null;

            List<ActiveTransporterInfo> infos = new List<ActiveTransporterInfo>();
            ActiveTransporterInfo currentInfo = new ActiveTransporterInfo();
            currentInfo.openDelay = 110;
            currentInfo.leaveSlag = true;
            currentInfo.savePawnsWithReferenceMode = true;
            float currentMass = 0f;

            for (int i = 0; i < cargo.Count; i++)
            {
                Thing t = cargo[i];
                if (t == null) continue;
                float itemMass = t.GetStatValue(StatDefOf.Mass) * (float)t.stackCount;

                if (currentMass + itemMass > PodMassCapacity && currentInfo.innerContainer.Count > 0)
                {
                    infos.Add(currentInfo);
                    currentInfo = new ActiveTransporterInfo();
                    currentInfo.openDelay = 110;
                    currentInfo.leaveSlag = true;
                    currentInfo.savePawnsWithReferenceMode = true;
                    currentMass = 0f;
                }

                currentInfo.innerContainer.TryAdd(t, false);
                currentMass += itemMass;
            }

            if (currentInfo.innerContainer.Count > 0)
            {
                infos.Add(currentInfo);
            }

            TravellingTransporters worldObj = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravellingTransporters);
            worldObj.SetFaction(Faction.OfPlayer);
            worldObj.destinationTile = destTile;
            worldObj.arrivalAction = arrivalAction;
            worldObj.Tile = originTile;
            Find.WorldObjects.Add(worldObj);

            for (int i = 0; i < infos.Count; i++)
            {
                worldObj.AddTransporter(infos[i], true);
            }

            return worldObj;
        }
    }

    public class TransportersArrivalAction_OutpostArrival : TransportersArrivalAction
    {
        public int targetTile = -1;
        public bool autoUpgrade = false;

        public TransportersArrivalAction_OutpostArrival()
        {
        }

        public TransportersArrivalAction_OutpostArrival(int targetTile, bool autoUpgrade = false)
        {
            this.targetTile = targetTile;
            this.autoUpgrade = autoUpgrade;
        }

        public override bool GeneratesMap
        {
            get { return false; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetTile, "targetTile", -1);
            Scribe_Values.Look(ref autoUpgrade, "autoUpgrade", false);
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            StrategicOutpost outpost = StrategicOutpostCaravanUtility.GetOutpostAtTile(destinationTile);
            if (outpost != null && outpost.Spawned) return true;
            return OutpostPodLogistics.GetHomeMap() != null;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            StrategicOutpost outpost = StrategicOutpostCaravanUtility.GetOutpostAtTile(tile);
            if (outpost == null || !outpost.Spawned)
            {
                Map home = OutpostPodLogistics.GetHomeMap();
                if (home == null) return;
                new TransportersArrivalAction_ColonyRecall(home.Tile).Arrived(transporters, home.Tile);
                Messages.Message("StrategicOutpost_PodsReturnedOutpostGone".Translate(), MessageTypeDefOf.NeutralEvent);
                return;
            }

            int colonistsAdded = 0;
            int silverAdded = 0;

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo info = transporters[i];
                if (info == null || info.innerContainer == null) continue;

                List<Thing> things = new List<Thing>();
                for (int k = 0; k < info.innerContainer.Count; k++)
                {
                    things.Add(info.innerContainer[k]);
                }

                for (int j = 0; j < things.Count; j++)
                {
                    Thing item = things[j];
                    if (item == null) continue;

                    info.innerContainer.Remove(item);

                    Pawn p = item as Pawn;
                    if (p != null)
                    {
                        if (p.RaceProps.Animal)
                        {
                            outpost.Animals.TryAdd(p, false);
                        }
                        else
                        {
                            outpost.Workers.TryAdd(p, false);
                            if (p.ownership != null)
                            {
                                p.ownership.UnclaimBed();
                            }
                            colonistsAdded++;
                        }
                        StrategicOutpost.SetStoredPawnProtection(p, true);
                    }
                    else
                    {
                        outpost.RawMaterials.TryAdd(item, false);
                        if (item.def == ThingDefOf.Silver)
                        {
                            silverAdded += item.stackCount;
                        }
                    }
                }
            }

            Messages.Message("StrategicOutpost_PodsDeliveredToOutpost".Translate(outpost.LabelCap, colonistsAdded, silverAdded), outpost, MessageTypeDefOf.PositiveEvent);

            if (autoUpgrade && outpost.CanUpgrade)
            {
                string failReason;
                outpost.TryUpgrade(out failReason, null);
            }
        }
    }

    public class TransportersArrivalAction_ColonyRecall : TransportersArrivalAction
    {
        public int targetTile = -1;

        public TransportersArrivalAction_ColonyRecall()
        {
        }

        public TransportersArrivalAction_ColonyRecall(int targetTile)
        {
            this.targetTile = targetTile;
        }

        public override bool GeneratesMap
        {
            get { return false; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetTile, "targetTile", -1);
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            Map map = Current.Game != null ? Current.Game.FindMap(destinationTile) : null;
            if (map == null) map = OutpostPodLogistics.GetHomeMap();
            if (map == null)
            {
                return false;
            }
            return true;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            Map map = Current.Game != null ? Current.Game.FindMap(tile) : null;
            if (map == null) map = OutpostPodLogistics.GetHomeMap();
            if (map == null)
            {
                Log.Warning("TransportersArrivalAction_ColonyRecall: Target home map not found.");
                return;
            }

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            if (!dropSpot.IsValid)
            {
                dropSpot = DropCellFinder.RandomDropSpot(map);
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo info = transporters[i];
                if (info != null)
                {
                    for (int k = 0; k < info.innerContainer.Count; k++)
                    {
                        Pawn p = info.innerContainer[k] as Pawn;
                        if (p != null)
                        {
                            if (Find.WorldPawns != null && Find.WorldPawns.Contains(p))
                            {
                                Find.WorldPawns.RemovePawn(p);
                            }
                            if (p.Faction != Faction.OfPlayer)
                            {
                                p.SetFaction(Faction.OfPlayer);
                            }
                        }
                    }
                    DropPodUtility.MakeDropPodAt(dropSpot, map, info, Faction.OfPlayer);
                }
            }

            Messages.Message("StrategicOutpost_PodsArrivedAtColony".Translate(), new GlobalTargetInfo(dropSpot, map), MessageTypeDefOf.PositiveEvent);
        }
    }
}
