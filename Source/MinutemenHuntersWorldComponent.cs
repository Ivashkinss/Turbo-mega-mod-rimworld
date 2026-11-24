using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    public class MinutemenHuntersWorldComponent : WorldComponent
    {
        public bool quest1Completed;
        public bool quest2Completed;
        public bool quest2Unlocked;

        public MinutemenHuntersWorldComponent(World world) : base(world) { }

        public static MinutemenHuntersWorldComponent Instance
        {
            get { return Find.World == null ? null : Find.World.GetComponent<MinutemenHuntersWorldComponent>(); }
        }

        public static Faction Faction
        {
            get
            {
                if (Find.FactionManager == null || MinutemenHuntersDefOf.TMM_MinutemenHuntersFaction == null) return null;
                return Find.FactionManager.FirstFactionOfDef(MinutemenHuntersDefOf.TMM_MinutemenHuntersFaction);
            }
        }

        /// The faction comes only from the world creation list. A player who removed it there, and an old save without it,
        /// keep their choice: nothing creates it later. Quests that need it are not offered without it.
        public static bool Available
        {
            get { Faction f = Faction; return f != null && !f.defeated; }
        }

        /// Kept for existing callers: says whether the faction is there, never creates it.
        public bool EnsureFactionExists()
        {
            return Available;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref quest1Completed, "mhQuest1Completed", false);
            Scribe_Values.Look(ref quest2Completed, "mhQuest2Completed", false);
            Scribe_Values.Look(ref quest2Unlocked, "mhQuest2Unlocked", false);
        }
    }
}
