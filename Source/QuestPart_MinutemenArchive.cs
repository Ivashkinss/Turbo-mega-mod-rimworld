using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    public class QuestPart_MinutemenArchive : QuestPartActivable
    {
        public Site site;
        public Faction faction;
        public string outSignalSuccess;
        public bool archiveSecured;

        public QuestPart_MinutemenArchive() { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "site");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref outSignalSuccess, "outSignalSuccess");
            Scribe_Values.Look(ref archiveSecured, "archiveSecured");
        }

        public bool TryFulfillRecovery(Thing archiveItem, Map deliveryMap)
        {
            if (archiveSecured || archiveItem == null) return false;
            if (archiveItem.def != MinutemenHuntersDefOf.TMM_HuntersFieldArchive) return false;

            archiveSecured = true;

            // Spawn reward at player delivery map
            if (deliveryMap != null)
            {
                var rewardSilver = ThingMaker.MakeThing(ThingDefOf.Silver);
                rewardSilver.stackCount = 500;
                GenPlace.TryPlaceThing(rewardSilver, DropCellFinder.TradeDropSpot(deliveryMap), deliveryMap, ThingPlaceMode.Near);

                var rewardMeds = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
                rewardMeds.stackCount = 3;
                GenPlace.TryPlaceThing(rewardMeds, DropCellFinder.TradeDropSpot(deliveryMap), deliveryMap, ThingPlaceMode.Near);
            }

            // Boost faction goodwill
            if (faction != null)
            {
                faction.TryAffectGoodwillWith(Faction.OfPlayer, 20, true, true);
            }

            // Mark completion in world component
            var comp = MinutemenHuntersWorldComponent.Instance;
            if (comp != null)
            {
                comp.quest2Completed = true;
            }

            // Fire success signal
            if (!string.IsNullOrEmpty(outSignalSuccess))
            {
                Find.SignalManager.SendSignal(new Signal(outSignalSuccess));
            }

            Complete();
            return true;
        }
    }
}
