using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class ModulePlacementEntry
    {
        public ThingDef thingDef;
        public ThingDef stuffDef;
        public IntVec3 position;
        public Rot4 rotation;
        public bool isDoor;
        public IntVec3 interactionCell;
        public bool hasInteractionCell;

        public CellRect GetOccupiedRect()
        {
            if (thingDef == null)
            {
                return new CellRect(position.x, position.z, 1, 1);
            }
            return GenAdj.OccupiedRect(position, rotation, thingDef.size);
        }
    }

    public class ModuleLayoutPlan
    {
        public int level;
        public CellRect bounds;
        public TerrainDef floorDef;
        public RoofDef roofDef;
        public IntVec3 doorCell;
        public Rot4 doorFacing;
        public List<ModulePlacementEntry> entries = new List<ModulePlacementEntry>();
    }

    public class StrategicOutpostMapComponent : MapComponent
    {
        public int builtLevel = 0;
        public IntVec3 baseCenter = IntVec3.Invalid;
        public CellRect baseFootprint = CellRect.Empty;
        public IntVec3 supplyDepotCell = IntVec3.Invalid;
        public List<int> deployedStaffPawnIDs = new List<int>();

        public StrategicOutpost Outpost
        {
            get
            {
                if (map == null) return null;
                return map.Parent as StrategicOutpost;
            }
        }

        public StrategicOutpostMapComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref builtLevel, "TMM_outpostBuiltLevel", 0);
            Scribe_Values.Look(ref baseCenter, "TMM_outpostBaseCenter", IntVec3.Invalid);
            Scribe_Values.Look(ref baseFootprint, "TMM_outpostBaseFootprint", CellRect.Empty);
            Scribe_Values.Look(ref supplyDepotCell, "TMM_outpostSupplyDepotCell", IntVec3.Invalid);
            Scribe_Collections.Look(ref deployedStaffPawnIDs, "TMM_deployedStaffPawnIDs", LookMode.Value);

            if (deployedStaffPawnIDs == null)
            {
                deployedStaffPawnIDs = new List<int>();
            }
        }

        public int CountRealBeds()
        {
            if (map == null || map.listerBuildings == null) return 0;
            int count = 0;
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] is Building_Bed)
                {
                    count++;
                }
            }
            return count;
        }

        public CellRect GetDefaultModuleRect(int lvl)
        {
            if (!baseCenter.IsValid)
            {
                baseCenter = map != null ? map.Center : new IntVec3(75, 0, 75);
            }

            int cx = baseCenter.x;
            int cz = baseCenter.z;

            switch (lvl)
            {
                case 1:
                    // Main Hall (9x9) centered at (cx, cz)
                    return new CellRect(cx - 4, cz - 4, 9, 9);
                case 2:
                    // North Pavilion (7x7) separated by 2-tile street (cz + 5, cz + 6)
                    return new CellRect(cx - 3, cz + 7, 7, 7);
                case 3:
                    // East Pavilion (7x7) separated by 2-tile street (cx + 5, cx + 6)
                    return new CellRect(cx + 7, cz - 3, 7, 7);
                case 4:
                    // South Pavilion (7x7) separated by 2-tile street (cz - 6, cz - 5)
                    return new CellRect(cx - 3, cz - 13, 7, 7);
                case 5:
                    // West Pavilion (7x7) separated by 2-tile street (cx - 6, cx - 5)
                    return new CellRect(cx - 13, cz - 3, 7, 7);
                case 6:
                    // North-East Pavilion (7x7)
                    return new CellRect(cx + 7, cz + 7, 7, 7);
                case 7:
                    // South-East Pavilion (7x7)
                    return new CellRect(cx + 7, cz - 13, 7, 7);
                case 8:
                    // South-West Pavilion (7x7)
                    return new CellRect(cx - 13, cz - 13, 7, 7);
                case 9:
                    // North-West Pavilion (7x7)
                    return new CellRect(cx - 13, cz + 7, 7, 7);
                case 10:
                    // Command Redoubt (7x7) north of North pavilion with 3-tile street (cz + 14..16)
                    return new CellRect(cx - 3, cz + 17, 7, 7);
                default:
                    return new CellRect(cx - 3, cz + 7 + (lvl - 2) * 10, 7, 7);
            }
        }

        public IntVec3 GetModuleDoorCell(int lvl, CellRect rect)
        {
            int mx = rect.CenterCell.x;
            int mz = rect.CenterCell.z;

            switch (lvl)
            {
                case 1:
                case 2:
                case 6:
                case 9:
                case 10:
                    // South wall door facing exterior street
                    return new IntVec3(mx, 0, rect.minZ);
                case 3:
                    // West wall door facing exterior street
                    return new IntVec3(rect.minX, 0, mz);
                case 4:
                case 7:
                case 8:
                    // North wall door facing exterior courtyard
                    return new IntVec3(mx, 0, rect.maxZ);
                case 5:
                    // East wall door facing exterior street
                    return new IntVec3(rect.maxX, 0, mz);
                default:
                    return new IntVec3(mx, 0, rect.minZ);
            }
        }

        public Rot4 GetModuleDoorFacing(int lvl)
        {
            switch (lvl)
            {
                case 1:
                case 2:
                case 6:
                case 9:
                case 10:
                    return Rot4.South;
                case 3:
                    return Rot4.West;
                case 4:
                case 7:
                case 8:
                    return Rot4.North;
                case 5:
                    return Rot4.East;
                default:
                    return Rot4.South;
            }
        }

        public bool HasClearExteriorAccess(IntVec3 doorCell, Rot4 doorFacing)
        {
            if (map == null) return true;
            IntVec3 outsideCell = doorCell + doorFacing.FacingCell;
            if (!outsideCell.InBounds(map)) return false;
            if (!outsideCell.Standable(map)) return false;

            TerrainDef ter = map.terrainGrid.TerrainAt(outsideCell);
            if (ter != null && ter.passability == Traversability.Impassable) return false;

            List<Thing> things = outsideCell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t is Building && t.def.passability == Traversability.Impassable)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsProtectedFromModuleWipe(Thing t)
        {
            if (t == null) return false;
            if (t is Pawn) return true;
            if (t is Corpse) return true;
            if (t is Blueprint || t is Frame) return true;
            if (t is Building) return true;
            if (t.def != null && t.def.category == ThingCategory.Item) return true;
            if (t.Faction != null) return true;

            // Only clear natural vegetation (wild plants), filth, and rock debris/chunks
            if (t.def != null)
            {
                if (t.def.category == ThingCategory.Plant) return false;
                if (t.def.category == ThingCategory.Filth) return false;
                if (t.def.thingCategories != null && t.def.thingCategories.Contains(ThingCategoryDefOf.Chunks)) return false;
            }
            return true;
        }

        public ModuleLayoutPlan GenerateLayoutPlan(int lvl, CellRect rect, StrategicOutpostSpecialization spec)
        {
            ModuleLayoutPlan plan = new ModuleLayoutPlan();
            plan.level = lvl;
            plan.bounds = rect;
            plan.floorDef = DefDatabase<TerrainDef>.GetNamedSilentFail("WoodPlankFloor");
            plan.roofDef = RoofDefOf.RoofConstructed;
            plan.doorCell = GetModuleDoorCell(lvl, rect);
            plan.doorFacing = GetModuleDoorFacing(lvl);

            ThingDef wallDef = ThingDefOf.Wall;
            ThingDef doorDef = ThingDefOf.Door;
            ThingDef woodStuff = ThingDefOf.WoodLog;

            // Perimeter walls and door
            foreach (IntVec3 edge in rect.EdgeCells)
            {
                if (edge == plan.doorCell)
                {
                    ModulePlacementEntry doorEntry = new ModulePlacementEntry();
                    doorEntry.thingDef = doorDef;
                    doorEntry.stuffDef = woodStuff;
                    doorEntry.position = edge;
                    doorEntry.rotation = plan.doorFacing;
                    doorEntry.isDoor = true;
                    doorEntry.hasInteractionCell = false;
                    plan.entries.Add(doorEntry);
                }
                else
                {
                    ModulePlacementEntry wallEntry = new ModulePlacementEntry();
                    wallEntry.thingDef = wallDef;
                    wallEntry.stuffDef = woodStuff;
                    wallEntry.position = edge;
                    wallEntry.rotation = Rot4.South;
                    wallEntry.isDoor = false;
                    wallEntry.hasInteractionCell = false;
                    plan.entries.Add(wallEntry);
                }
            }

            int cx = rect.CenterCell.x;
            int cz = rect.CenterCell.z;

            if (lvl == 1)
            {
                // Main Hall: 2 Beds, Table 2x2, 2 Dining Chairs, Light, Workstation
                // Bed 1 (1x2 south): occupies (cx + 1, cz + 2..3)
                ModulePlacementEntry bed1 = new ModulePlacementEntry();
                bed1.thingDef = ThingDefOf.Bed;
                bed1.stuffDef = woodStuff;
                bed1.position = new IntVec3(cx + 1, 0, cz + 2);
                bed1.rotation = Rot4.South;
                plan.entries.Add(bed1);

                // Bed 2 (1x2 south): occupies (cx + 3, cz + 2..3)
                ModulePlacementEntry bed2 = new ModulePlacementEntry();
                bed2.thingDef = ThingDefOf.Bed;
                bed2.stuffDef = woodStuff;
                bed2.position = new IntVec3(cx + 3, 0, cz + 2);
                bed2.rotation = Rot4.South;
                plan.entries.Add(bed2);

                // Table 2x2: occupies [cx - 2, cx - 1] x [cz, cz + 1]
                ModulePlacementEntry table = new ModulePlacementEntry();
                table.thingDef = ThingDefOf.Table2x2c;
                table.stuffDef = woodStuff;
                table.position = new IntVec3(cx - 2, 0, cz);
                table.rotation = Rot4.North;
                plan.entries.Add(table);

                // Dining Chair 1 at (cx - 3, cz) facing East
                ModulePlacementEntry chair1 = new ModulePlacementEntry();
                chair1.thingDef = ThingDefOf.DiningChair;
                chair1.stuffDef = woodStuff;
                chair1.position = new IntVec3(cx - 3, 0, cz);
                chair1.rotation = Rot4.East;
                plan.entries.Add(chair1);

                // Dining Chair 2 at (cx - 3, cz + 1) facing East
                ModulePlacementEntry chair2 = new ModulePlacementEntry();
                chair2.thingDef = ThingDefOf.DiningChair;
                chair2.stuffDef = woodStuff;
                chair2.position = new IntVec3(cx - 3, 0, cz + 1);
                chair2.rotation = Rot4.East;
                plan.entries.Add(chair2);

                // TorchLamp bedside between bed heads at (cx + 2, cz + 3)
                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.stuffDef = null;
                light.position = new IntVec3(cx + 2, 0, cz + 3);
                light.rotation = Rot4.South;
                plan.entries.Add(light);

                // Workstation at (cx + 2, 0, cz - 1), interaction cell at (cx + 2, cz - 2)
                ThingDef wsDef = GetWorkstationDef(spec);
                if (wsDef != null)
                {
                    ModulePlacementEntry ws = new ModulePlacementEntry();
                    ws.thingDef = wsDef;
                    ws.stuffDef = wsDef.MadeFromStuff ? woodStuff : null;
                    ws.position = new IntVec3(cx + 2, 0, cz - 1);
                    ws.rotation = Rot4.South;
                    ws.hasInteractionCell = true;
                    ws.interactionCell = new IntVec3(cx + 2, 0, cz - 2);
                    plan.entries.Add(ws);
                }
            }
            else if (lvl == 10)
            {
                // Level 10 Command Redoubt: 2 beds (+2 beds to reach 12 beds total, >= 11 required)
                ModulePlacementEntry b1 = new ModulePlacementEntry();
                b1.thingDef = ThingDefOf.Bed;
                b1.stuffDef = woodStuff;
                b1.position = new IntVec3(cx - 1, 0, cz + 2);
                b1.rotation = Rot4.South;
                plan.entries.Add(b1);

                ModulePlacementEntry b2 = new ModulePlacementEntry();
                b2.thingDef = ThingDefOf.Bed;
                b2.stuffDef = woodStuff;
                b2.position = new IntVec3(cx + 1, 0, cz + 2);
                b2.rotation = Rot4.South;
                plan.entries.Add(b2);

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.stuffDef = null;
                light.position = new IntVec3(cx, 0, cz - 1);
                light.rotation = Rot4.South;
                plan.entries.Add(light);

                // Exterior sandbags flanking the door
                IntVec3 sandbagLeft = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(-1, 0, 0);
                IntVec3 sandbagRight = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(1, 0, 0);

                ModulePlacementEntry sbLeft = new ModulePlacementEntry();
                sbLeft.thingDef = ThingDefOf.Sandbags;
                sbLeft.stuffDef = null;
                sbLeft.position = sandbagLeft;
                sbLeft.rotation = Rot4.South;
                plan.entries.Add(sbLeft);

                ModulePlacementEntry sbRight = new ModulePlacementEntry();
                sbRight.thingDef = ThingDefOf.Sandbags;
                sbRight.stuffDef = null;
                sbRight.position = sandbagRight;
                sbRight.rotation = Rot4.South;
                plan.entries.Add(sbRight);
            }
            else
            {
                // Levels 2..9: Add 1 real bed per module (+ light, optional sandbag)
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = ThingDefOf.Bed;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx, 0, cz + 1);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.stuffDef = null;
                light.position = new IntVec3(cx - 1, 0, cz - 1);
                light.rotation = Rot4.South;
                plan.entries.Add(light);

                if (lvl == 5 || lvl == 7)
                {
                    IntVec3 sandbagCell = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(1, 0, 0);
                    ModulePlacementEntry sb = new ModulePlacementEntry();
                    sb.thingDef = ThingDefOf.Sandbags;
                    sb.stuffDef = null;
                    sb.position = sandbagCell;
                    sb.rotation = Rot4.South;
                    plan.entries.Add(sb);
                }
            }

            return plan;
        }

        public bool CanSafelyExecutePlan(ModuleLayoutPlan plan, out string failReason)
        {
            if (map == null)
            {
                failReason = "Map is null";
                return false;
            }

            // Margin check from map border
            if (plan.bounds.minX < 6 || plan.bounds.maxX >= map.Size.x - 6 ||
                plan.bounds.minZ < 6 || plan.bounds.maxZ >= map.Size.z - 6)
            {
                failReason = "Module bounds outside map safe border";
                return false;
            }

            // Terrain and protected things check across module bounds
            foreach (IntVec3 c in plan.bounds)
            {
                TerrainDef ter = map.terrainGrid.TerrainAt(c);
                if (ter != null && (ter.passability == Traversability.Impassable ||
                    (ter.IsWater && !ter.affordances.Contains(TerrainAffordanceDefOf.Light))))
                {
                    failReason = "Impassable or water terrain inside module footprint";
                    return false;
                }

                List<Thing> thingList = c.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing t = thingList[i];
                    if (IsProtectedFromModuleWipe(t))
                    {
                        failReason = "Footprint blocked by protected entity: " + t.LabelCap;
                        return false;
                    }
                }
            }

            // Unobstructed door access check
            if (!HasClearExteriorAccess(plan.doorCell, plan.doorFacing))
            {
                failReason = "Door exterior street cell is blocked";
                return false;
            }

            failReason = null;
            return true;
        }

        public bool CanSafelyPlaceModule(CellRect rect, int lvl = -1)
        {
            if (map == null) return false;

            if (rect.minX < 6 || rect.maxX >= map.Size.x - 6 || rect.minZ < 6 || rect.maxZ >= map.Size.z - 6)
            {
                return false;
            }

            foreach (IntVec3 c in rect)
            {
                TerrainDef ter = map.terrainGrid.TerrainAt(c);
                if (ter != null && (ter.passability == Traversability.Impassable ||
                    (ter.IsWater && !ter.affordances.Contains(TerrainAffordanceDefOf.Light))))
                {
                    return false;
                }

                List<Thing> thingList = c.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing t = thingList[i];
                    if (IsProtectedFromModuleWipe(t))
                    {
                        return false;
                    }
                }
            }

            if (lvl > 0)
            {
                IntVec3 doorCell = GetModuleDoorCell(lvl, rect);
                Rot4 doorFacing = GetModuleDoorFacing(lvl);
                if (!HasClearExteriorAccess(doorCell, doorFacing))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryFindExpansionRect(int targetLevel, out CellRect rect)
        {
            CellRect candidate = GetDefaultModuleRect(targetLevel);
            if (CanSafelyPlaceModule(candidate, targetLevel))
            {
                rect = candidate;
                return true;
            }

            // Try small offsets in cardinal directions (4, 8, 12 cells)
            IntVec3[] offsets = new IntVec3[]
            {
                new IntVec3(0, 0, 4),
                new IntVec3(0, 0, -4),
                new IntVec3(4, 0, 0),
                new IntVec3(-4, 0, 0),
                new IntVec3(8, 0, 0),
                new IntVec3(-8, 0, 0),
                new IntVec3(0, 0, 8),
                new IntVec3(0, 0, -8),
                new IntVec3(12, 0, 0),
                new IntVec3(-12, 0, 0),
                new IntVec3(0, 0, 12),
                new IntVec3(0, 0, -12)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                CellRect shifted = candidate.MovedBy(offsets[i]);
                if (CanSafelyPlaceModule(shifted, targetLevel))
                {
                    rect = shifted;
                    return true;
                }
            }

            rect = CellRect.Empty;
            return false;
        }

        public bool BuildModuleForLevel(int lvl, CellRect rect, StrategicOutpostSpecialization spec)
        {
            string failReason;
            return BuildModuleForLevel(lvl, rect, spec, out failReason);
        }

        public bool BuildModuleForLevel(int lvl, CellRect rect, StrategicOutpostSpecialization spec, out string failReason)
        {
            if (map == null)
            {
                failReason = "Map is null";
                return false;
            }

            // Generate clean layout plan
            ModuleLayoutPlan plan = GenerateLayoutPlan(lvl, rect, spec);

            // Re-validate safety immediately before mutating any map state
            if (!CanSafelyExecutePlan(plan, out failReason))
            {
                return false;
            }

            // Clear natural vegetation/rocks only (protected items/structures are never touched)
            foreach (IntVec3 c in plan.bounds)
            {
                List<Thing> toClear = new List<Thing>();
                List<Thing> list = c.GetThingList(map);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing t = list[i];
                    if (t != null && !IsProtectedFromModuleWipe(t) && t.def.destroyable)
                    {
                        toClear.Add(t);
                    }
                }
                for (int i = 0; i < toClear.Count; i++)
                {
                    toClear[i].Destroy(DestroyMode.Vanish);
                }

                if (plan.floorDef != null)
                {
                    map.terrainGrid.SetTerrain(c, plan.floorDef);
                }
                if (plan.roofDef != null)
                {
                    map.roofGrid.SetRoof(c, plan.roofDef);
                }
            }

            // Spawn planned entries with strict faction ownership
            for (int i = 0; i < plan.entries.Count; i++)
            {
                ModulePlacementEntry entry = plan.entries[i];
                if (entry.thingDef != null && entry.position.InBounds(map))
                {
                    Thing spawned = ThingMaker.MakeThing(entry.thingDef, entry.stuffDef ?? (entry.thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(entry.thingDef) : null));
                    spawned.SetFaction(Faction.OfPlayer);
                    GenSpawn.Spawn(spawned, entry.position, map, entry.rotation, WipeMode.Vanish, false, false);
                }
            }

            // Designate supply depot cell for Level 1
            if (lvl == 1)
            {
                int cx = rect.CenterCell.x;
                int cz = rect.CenterCell.z;
                supplyDepotCell = new IntVec3(cx - 2, 0, cz - 2);
            }

            // Update footprint and built level
            if (baseFootprint.IsEmpty)
            {
                baseFootprint = rect;
            }
            else
            {
                baseFootprint = baseFootprint.Encapsulate(rect);
            }
            builtLevel = Math.Max(builtLevel, lvl);

            failReason = null;
            return true;
        }

        private ThingDef GetWorkstationDef(StrategicOutpostSpecialization spec)
        {
            string targetDefName = "CraftingSpot";
            switch (spec)
            {
                case StrategicOutpostSpecialization.Mining:
                case StrategicOutpostSpecialization.Logging:
                    targetDefName = "TableStonecutter";
                    break;
                case StrategicOutpostSpecialization.Farming:
                    targetDefName = "TableButcher";
                    break;
                case StrategicOutpostSpecialization.Factory:
                    targetDefName = "TableSculpting";
                    break;
                case StrategicOutpostSpecialization.Watchtower:
                    targetDefName = "Sandbags";
                    break;
                default:
                    targetDefName = "CraftingSpot";
                    break;
            }

            ThingDef wsDef = DefDatabase<ThingDef>.GetNamedSilentFail(targetDefName);
            if (wsDef == null)
            {
                wsDef = DefDatabase<ThingDef>.GetNamedSilentFail("CraftingSpot");
            }
            return wsDef;
        }
    }
}
