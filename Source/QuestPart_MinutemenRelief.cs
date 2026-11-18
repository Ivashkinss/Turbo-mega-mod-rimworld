using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    public class QuestPart_MinutemenRelief : QuestPartActivable
    {
        public Map map;
        public ThingDef requiredDef;
        public int requiredCount;
        public Faction faction;
        public string outSignalSuccess;
        public bool delivered;

        public QuestPart_MinutemenRelief() { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
            Scribe_Defs.Look(ref requiredDef, "requiredDef");
            Scribe_Values.Look(ref requiredCount, "requiredCount");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref outSignalSuccess, "outSignalSuccess");
            Scribe_Values.Look(ref delivered, "delivered");
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            // In game, can check periodically if items delivered or player fulfilled request
        }

        public bool TryFulfillDelivery()
        {
            if (delivered || map == null || requiredDef == null) return false;

            // Count available items in player colony storage
            var things = map.listerThings.ThingsOfDef(requiredDef)
                .Where(t => t.Spawned && !t.IsForbidden(Faction.OfPlayer))
                .ToList();

            int available = things.Sum(t => t.stackCount);
            if (available < requiredCount) return false;

            // Deduct required items from colony
            int remainingToDeduct = requiredCount;
            foreach (var t in things)
            {
                if (remainingToDeduct <= 0) break;
                if (t.stackCount <= remainingToDeduct)
                {
                    remainingToDeduct -= t.stackCount;
                    t.Destroy();
                }
                else
                {
                    t.SplitOff(remainingToDeduct).Destroy();
                    remainingToDeduct = 0;
                }
            }

            delivered = true;

            // Spawn reward: 250 silver
            var reward = ThingMaker.MakeThing(ThingDefOf.Silver);
            reward.stackCount = 250;
            GenPlace.TryPlaceThing(reward, DropCellFinder.TradeDropSpot(map), map, ThingPlaceMode.Near);

            // Boost faction goodwill
            if (faction != null)
            {
                faction.TryAffectGoodwillWith(Faction.OfPlayer, 15, true, true);
            }

            // Update world progression
            var comp = MinutemenHuntersWorldComponent.Instance;
            if (comp != null)
            {
                comp.quest1Completed = true;
                comp.quest2Unlocked = true;
            }

            // Fire completion signal to quest runner
            if (!string.IsNullOrEmpty(outSignalSuccess))
            {
                Find.SignalManager.SendSignal(new Signal(outSignalSuccess));
            }

            Complete();
            return true;
        }
    }
}
