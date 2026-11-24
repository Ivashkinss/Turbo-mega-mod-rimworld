using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace TurboMegaMod
{
    public class QuestNode_Root_MinutemenArchive : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            var comp = MinutemenHuntersWorldComponent.Instance;
            if (!MinutemenHuntersWorldComponent.Available || comp == null || !comp.quest1Completed || comp.quest2Completed)
            {
                return false;
            }
            return true;
        }

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = QuestGen_Get.GetMap();

            Faction faction = MinutemenHuntersWorldComponent.Faction;
            if (faction == null)
            {
                // Only a forced generation gets here (the test run refuses). The quest ends at once and creates nothing.
                Log.Warning("[TMM] Minutemen quest skipped: the faction is not in this world.");
                quest.name = "TMM_MinutemenArchiveQuest_Title".Translate();
                quest.description = "TMM_MinutemenArchiveQuest_Title".Translate();
                quest.End(QuestEndOutcome.Unknown, 0, null, quest.InitiateSignal);
                return;
            }

            string questTag = "MinutemenArchive" + quest.id;
            string inSignalEnable = slate.Get<string>("inSignal") ?? QuestGen.slate.Get<string>("inSignal");
            string successSignal = QuestGenUtility.HardcodedSignalWithQuestID(questTag + ".Success");
            string expireSignal = QuestGenUtility.HardcodedSignalWithQuestID(questTag + ".Expired");

            // Find tile for outpost near player colony
            PlanetTile rootTile = map != null ? map.Tile : (Find.AnyPlayerHomeMap != null ? Find.AnyPlayerHomeMap.Tile : PlanetTile.Invalid);
            PlanetTile siteTile;
            if (!TileFinder.TryFindNewSiteTile(out siteTile, 4, 12, false, null, 0f, true, TileFinderMode.Near, false, false, null, null))
            {
                siteTile = rootTile;
            }

            // Create Site on world map using SitePartDefWithParams
            var siteParts = new List<SitePartDefWithParams>
            {
                new SitePartDefWithParams(MinutemenHuntersDefOf.TMM_MinutemenOutpostSitePart, new SitePartParams())
            };
            Site site = QuestGen_Sites.GenerateSite(siteParts, siteTile, faction);
            quest.SpawnWorldObject(site);

            // Add quest part
            var archivePart = new QuestPart_MinutemenArchive();
            archivePart.site = site;
            archivePart.faction = faction;
            archivePart.outSignalSuccess = successSignal;
            archivePart.inSignalEnable = inSignalEnable;
            quest.AddPart(archivePart);

            // Expiration delay (20 in-game days = 1,200,000 ticks)
            var delayPart = new QuestPart_Delay();
            delayPart.delayTicks = 20 * 60000;
            delayPart.inSignalEnable = inSignalEnable;
            delayPart.outSignalsCompleted.Add(expireSignal);
            quest.AddPart(delayPart);

            // End signals
            quest.End(QuestEndOutcome.Success, inSignal: successSignal);
            quest.End(QuestEndOutcome.Fail, inSignal: expireSignal);

            // Quest metadata and labels
            quest.name = "TMM_MinutemenArchiveQuest_Title".Translate();
            quest.description = "TMM_MinutemenArchiveQuest_Description".Translate();
        }
    }
}
