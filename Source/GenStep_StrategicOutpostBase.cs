using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    public class GenStep_StrategicOutpostBase : GenStep
    {
        public override int SeedPart
        {
            get { return 82746194; }
        }

        public override void Generate(Map map, GenStepParams parms)
        {
            StrategicOutpost outpost = map.Parent as StrategicOutpost;
            if (outpost == null)
            {
                return;
            }

            StrategicOutpostMapComponent comp = map.GetComponent<StrategicOutpostMapComponent>();
            if (comp == null)
            {
                return;
            }

            // Find a valid, standable center near map center
            IntVec3 center = map.Center;
            if (!center.Standable(map) || map.terrainGrid.TerrainAt(center).passability == Traversability.Impassable)
            {
                IntVec3 found;
                if (CellFinder.TryFindRandomCellNear(center, map, 25, c => c.Standable(map) && c.GetRoof(map) == null, out found))
                {
                    center = found;
                }
            }

            comp.baseCenter = center;

            // Generate initial modules from level 1 up to current outpost level
            int targetLevel = Math.Max(1, Math.Min(10, outpost.level));
            for (int lvl = 1; lvl <= targetLevel; lvl++)
            {
                CellRect rect;
                if (comp.TryFindExpansionRect(lvl, out rect))
                {
                    comp.BuildModuleForLevel(lvl, rect, outpost.specialization);
                }
            }

            // Unfog base camp area so colonists and player can clearly see it
            if (!comp.baseFootprint.IsEmpty)
            {
                CellRect unfogRect = comp.baseFootprint.ExpandedBy(3).ClipInsideMap(map);
                foreach (IntVec3 c in unfogRect)
                {
                    map.fogGrid.Unfog(c);
                }
            }
        }
    }
}
