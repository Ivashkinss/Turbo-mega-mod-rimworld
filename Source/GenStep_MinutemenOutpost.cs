using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class GenStep_MinutemenOutpost : GenStep
    {
        public override int SeedPart
        {
            get { return 82746192; }
        }

        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 center = map.Center;
            Faction faction = MinutemenHuntersWorldComponent.Faction;
            if (faction == null)
            {
                var comp = MinutemenHuntersWorldComponent.Instance;
                if (comp != null)
                {
                    comp.EnsureFactionExists();
                    faction = MinutemenHuntersWorldComponent.Faction;
                }
            }

            // 1. Clear 45x45 area
            CellRect bounds = new CellRect(center.x - 22, center.z - 22, 45, 45);
            bounds = bounds.ClipInsideMap(map);

            foreach (var cell in bounds)
            {
                foreach (var t in cell.GetThingList(map).ToArray())
                {
                    if (t.def.defName == "VoidMonolith") continue;
                    if (t is Building || t is Plant || t is Filth)
                    {
                        if (t.def.destroyable) t.Destroy(); else t.DeSpawn();
                    }
                }
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
                map.roofGrid.SetRoof(cell, null);
            }

            // 2. Central Radio Station (7x7)
            CellRect radioRect = new CellRect(center.x - 3, center.z - 3, 7, 7);
            BuildRoom(map, radioRect, faction);
            SpawnThing(map, "CommsConsole", radioRect.CenterCell, faction);
            SpawnThing(map, "Battery", new IntVec3(radioRect.minX + 1, 0, radioRect.minZ + 1), faction);

            // 3. Barracks (9x7)
            CellRect barracksRect = new CellRect(center.x - 16, center.z - 3, 9, 7);
            BuildRoom(map, barracksRect, faction);
            SpawnThing(map, "Bed", new IntVec3(barracksRect.minX + 2, 0, barracksRect.maxZ - 2), faction);
            SpawnThing(map, "Bed", new IntVec3(barracksRect.minX + 4, 0, barracksRect.maxZ - 2), faction);
            SpawnThing(map, "Table2x2c", new IntVec3(barracksRect.minX + 3, 0, barracksRect.minZ + 2), faction);
            SpawnThing(map, "DiningChair", new IntVec3(barracksRect.minX + 2, 0, barracksRect.minZ + 2), faction);
            SpawnThing(map, "DiningChair", new IntVec3(barracksRect.minX + 5, 0, barracksRect.minZ + 2), faction);

            // 4. Field Clinic (9x7)
            CellRect clinicRect = new CellRect(center.x + 8, center.z - 3, 9, 7);
            BuildRoom(map, clinicRect, faction);
            var medBed1 = (Building_Bed)SpawnThing(map, "Bed", new IntVec3(clinicRect.minX + 2, 0, clinicRect.maxZ - 2), faction);
            medBed1.Medical = true;
            var medBed2 = (Building_Bed)SpawnThing(map, "Bed", new IntVec3(clinicRect.minX + 5, 0, clinicRect.maxZ - 2), faction);
            medBed2.Medical = true;
            var medShelf = SpawnThing(map, "Shelf", new IntVec3(clinicRect.maxX - 2, 0, clinicRect.minZ + 2), faction);
            SpawnItem(map, "MedicineIndustrial", medShelf.Position, 4);

            // 5. Archive & Armory (7x7)
            CellRect archiveRect = new CellRect(center.x - 3, center.z + 8, 7, 7);
            BuildRoom(map, archiveRect, faction);
            var archiveShelf = SpawnThing(map, "Shelf", archiveRect.CenterCell, faction);
            // Spawn the quest item TMM_HuntersFieldArchive
            SpawnItem(map, "TMM_HuntersFieldArchive", archiveShelf.Position, 1);
            SpawnItem(map, "MealSimple", new IntVec3(archiveRect.minX + 1, 0, archiveRect.minZ + 1), 5);

            // 6. Perimeter defense: Sandbags and barricades with open vantage lanes
            BuildPerimeter(map, bounds, faction);

            // 7. Spawn Garrison
            if (faction != null)
            {
                SpawnGarrisonPawn(map, MinutemenHuntersDefOf.TMM_HuntersMilitiaman, new IntVec3(center.x - 1, 0, center.z - 5), faction);
                SpawnGarrisonPawn(map, MinutemenHuntersDefOf.TMM_HuntersHunter, new IntVec3(center.x + 1, 0, center.z - 5), faction);
                SpawnGarrisonPawn(map, MinutemenHuntersDefOf.TMM_HuntersMedic, new IntVec3(clinicRect.CenterCell.x, 0, clinicRect.CenterCell.z), faction);
            }

            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        }

        private static Thing SpawnThing(Map map, string defName, IntVec3 cell, Faction faction)
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
            if (thing is Building && faction != null) thing.SetFaction(faction);
            return GenSpawn.Spawn(thing, cell, map);
        }

        private static Thing SpawnItem(Map map, string defName, IntVec3 cell, int count)
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            var thing = ThingMaker.MakeThing(def);
            thing.stackCount = count;
            return GenSpawn.Spawn(thing, cell, map);
        }

        private static void BuildRoom(Map map, CellRect rect, Faction faction)
        {
            IntVec3 doorCell = new IntVec3(rect.CenterCell.x, 0, rect.minZ);
            foreach (var c in rect.EdgeCells)
            {
                var def = (c == doorCell) ? ThingDefOf.Door : ThingDefOf.Wall;
                var b = ThingMaker.MakeThing(def, ThingDefOf.Steel);
                if (faction != null) b.SetFaction(faction);
                GenSpawn.Spawn(b, c, map);
            }
            foreach (var c in rect.ContractedBy(1))
            {
                map.terrainGrid.SetTerrain(c, TerrainDefOf.Concrete);
                map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
            }
        }

        private static void BuildPerimeter(Map map, CellRect bounds, Faction faction)
        {
            int minX = bounds.minX + 4;
            int maxX = bounds.maxX - 4;
            int minZ = bounds.minZ + 4;
            int maxZ = bounds.maxZ - 4;

            // North and South barricades with 4-cell entrance gap
            for (int x = minX; x <= maxX; x++)
            {
                if (x >= bounds.CenterCell.x - 2 && x <= bounds.CenterCell.x + 2) continue;
                SpawnPerimeterCover(map, new IntVec3(x, 0, maxZ), faction);
                SpawnPerimeterCover(map, new IntVec3(x, 0, minZ), faction);
            }

            // East and West sandbags with 4-cell entrance gap
            for (int z = minZ; z <= maxZ; z++)
            {
                if (z >= bounds.CenterCell.z - 2 && z <= bounds.CenterCell.z + 2) continue;
                SpawnPerimeterCover(map, new IntVec3(minX, 0, z), faction);
                SpawnPerimeterCover(map, new IntVec3(maxX, 0, z), faction);
            }
        }

        private static void SpawnPerimeterCover(Map map, IntVec3 c, Faction faction)
        {
            var def = ThingDefOf.Sandbags;
            var cover = ThingMaker.MakeThing(def, ThingDefOf.Cloth);
            if (faction != null) cover.SetFaction(faction);
            GenSpawn.Spawn(cover, c, map);
        }

        private static void SpawnGarrisonPawn(Map map, PawnKindDef kind, IntVec3 c, Faction faction)
        {
            if (kind == null || faction == null) return;
            var req = new PawnGenerationRequest(kind, faction, canGeneratePawnRelations: false, fixedBiologicalAge: 28f);
            var pawn = PawnGenerator.GeneratePawn(req);
            GenSpawn.Spawn(pawn, c, map);
        }
    }
}
