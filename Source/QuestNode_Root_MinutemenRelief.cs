using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace TurboMegaMod
{
    public class QuestNode_Root_MinutemenRelief : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            var map = QuestGen_Get.GetMap();
            if (map == null) return false;
            var comp = MinutemenHuntersWorldComponent.Instance;
            if (comp != null && comp.quest1Completed) return false;
            return true;
        }

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = QuestGen_Get.GetMap();

            var comp = MinutemenHuntersWorldComponent.Instance;
            if (comp != null) comp.EnsureFactionExists();
            Faction faction = MinutemenHuntersWorldComponent.Faction;

            string questTag = "MinutemenRelief" + quest.id;
            string inSignalEnable = slate.Get<string>("inSignal") ?? QuestGen.slate.Get<string>("inSignal");
            string successSignal = QuestGenUtility.HardcodedSignalWithQuestID(questTag + ".Success");
            string expireSignal = QuestGenUtility.HardcodedSignalWithQuestID(questTag + ".Expired");

            // Quest part: track delivery of 5 MedicineIndustrial
            var reliefPart = new QuestPart_MinutemenRelief();
            reliefPart.map = map;
            reliefPart.requiredDef = ThingDefOf.MedicineIndustrial;
            reliefPart.requiredCount = 5;
            reliefPart.faction = faction;
            reliefPart.outSignalSuccess = successSignal;
            reliefPart.inSignalEnable = inSignalEnable;
            quest.AddPart(reliefPart);

            // Expiration delay (15 in-game days = 900,000 ticks)
            var delayPart = new QuestPart_Delay();
            delayPart.delayTicks = 15 * 60000;
            delayPart.inSignalEnable = inSignalEnable;
            delayPart.outSignalsCompleted.Add(expireSignal);
            quest.AddPart(delayPart);

            // End signals
            quest.End(QuestEndOutcome.Success, inSignal: successSignal);
            quest.End(QuestEndOutcome.Fail, inSignal: expireSignal);

            // Quest metadata and labels
            quest.name = "TMM_MinutemenReliefQuest_Title".Translate();
            quest.description = "TMM_MinutemenReliefQuest_Description".Translate(reliefPart.requiredCount, reliefPart.requiredDef.label);
        }
    }
}
