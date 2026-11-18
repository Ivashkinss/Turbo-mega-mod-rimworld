using RimWorld;
using Verse;

namespace TurboMegaMod
{
    [DefOf]
    public static class MinutemenHuntersDefOf
    {
        public static FactionDef TMM_MinutemenHuntersFaction;
        public static PawnKindDef TMM_HuntersMilitiaman;
        public static PawnKindDef TMM_HuntersHunter;
        public static PawnKindDef TMM_HuntersMedic;
        public static ThingDef TMM_HuntersFieldArchive;
        public static SitePartDef TMM_MinutemenOutpostSitePart;
        public static QuestScriptDef TMM_MinutemenReliefQuest;
        public static QuestScriptDef TMM_MinutemenArchiveQuest;

        static MinutemenHuntersDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MinutemenHuntersDefOf));
        }
    }
}
