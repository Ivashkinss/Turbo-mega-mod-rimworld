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

        private bool initialized;

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (!initialized)
            {
                initialized = true;
                EnsureFactionExists();
            }
        }

        public void EnsureFactionExists()
        {
            if (Find.FactionManager == null || MinutemenHuntersDefOf.TMM_MinutemenHuntersFaction == null) return;
            var existing = Find.FactionManager.FirstFactionOfDef(MinutemenHuntersDefOf.TMM_MinutemenHuntersFaction);
            if (existing != null) return;

            // Safely generate faction for new world or existing save without breaking other factions
            var parms = new FactionGeneratorParms(MinutemenHuntersDefOf.TMM_MinutemenHuntersFaction);
            var newFaction = FactionGenerator.NewGeneratedFaction(parms);
            newFaction.TryAffectGoodwillWith(Faction.OfPlayer, 0, false, false);
            Find.FactionManager.Add(newFaction);
            Log.Message("[TMM] Minutemen & Hunters faction successfully registered in world.");
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
