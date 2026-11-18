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
        public bool isMedicalBed;
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
        public bool hasSpawnedGenerator = false;
        public int farmingZonesCreated = 0;
        public List<int> growingZoneLevelsCreated = new List<int>();
        public List<int> outpostGrowingZoneIDs = new List<int>();

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
            Scribe_Values.Look(ref hasSpawnedGenerator, "TMM_hasSpawnedGenerator", false);
            Scribe_Values.Look(ref farmingZonesCreated, "TMM_farmingZonesCreated", 0);
            Scribe_Collections.Look(ref deployedStaffPawnIDs, "TMM_deployedStaffPawnIDs", LookMode.Value);
            Scribe_Collections.Look(ref growingZoneLevelsCreated, "TMM_growingZoneLevelsCreated", LookMode.Value);
            Scribe_Collections.Look(ref outpostGrowingZoneIDs, "TMM_outpostGrowingZoneIDs", LookMode.Value);

            if (deployedStaffPawnIDs == null)
            {
                deployedStaffPawnIDs = new List<int>();
            }
            if (growingZoneLevelsCreated == null)
            {
                growingZoneLevelsCreated = new List<int>();
            }
            if (outpostGrowingZoneIDs == null)
            {
                outpostGrowingZoneIDs = new List<int>();
            }
        }

        public int CountRealBeds()
        {
            if (map == null || map.listerBuildings == null) return 0;
            int count = 0;
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building_Bed bed = buildings[i] as Building_Bed;
                if (bed != null && !bed.Medical)
                {
                    count++;
                }
            }
            return count;
        }

        public int CountMedicalBeds()
        {
            if (map == null || map.listerBuildings == null) return 0;
            int count = 0;
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building_Bed bed = buildings[i] as Building_Bed;
                if (bed != null && bed.Medical)
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

            // Only clear natural vegetation (wild plants), filth, and rock debris/chunks without faction
            if (t.def != null)
            {
                if (t.def.category == ThingCategory.Plant) return false;
                if (t.def.category == ThingCategory.Filth) return false;
                if (t.def.thingCategories != null && t.def.thingCategories.Contains(ThingCategoryDefOf.Chunks)) return false;
            }
            return true;
        }

        public static bool IsResearchFinished(string researchDefName)
        {
            if (string.IsNullOrEmpty(researchDefName)) return false;
            ResearchProjectDef rp = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchDefName);
            if (rp == null) return false;
            return rp.IsFinished;
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
            ThingDef bedDef = ThingDefOf.Bed;

            // 1. Perimeter walls and door
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
                    plan.entries.Add(wallEntry);
                }
            }

            int cx = rect.CenterCell.x;
            int cz = rect.CenterCell.z;

            // Technology availability check
            bool hasElectricity = IsResearchFinished("Electricity");
            bool hasSmithing = IsResearchFinished("Smithing");
            bool hasClothing = IsResearchFinished("ComplexClothing");
            bool hasStonecutting = IsResearchFinished("Stonecutting");
            bool hasMachining = IsResearchFinished("Machining");
            bool hasFabrication = IsResearchFinished("Fabrication");
            bool hasTurrets = IsResearchFinished("GunTurrets");

            ThingDef clothStuff = DefDatabase<ThingDef>.GetNamedSilentFail("Cloth") ?? (ThingDefOf.Sandbags.MadeFromStuff ? GenStuff.DefaultStuffFor(ThingDefOf.Sandbags) : null);
            ThingDef steelStuff = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");

            if (lvl == 1)
            {
                // =========================================================================
                // Level 1: Main Hall (9x9)
                // Guaranteed >= 2 regular beds, dining, lighting, specialized workstation
                // =========================================================================
                // Bed 1 (1x2 south): occupies (cx + 1, cz + 2..3)
                ModulePlacementEntry bed1 = new ModulePlacementEntry();
                bed1.thingDef = bedDef;
                bed1.stuffDef = woodStuff;
                bed1.position = new IntVec3(cx + 1, 0, cz + 2);
                bed1.rotation = Rot4.South;
                plan.entries.Add(bed1);

                // Bed 2 (1x2 south): occupies (cx + 3, cz + 2..3)
                ModulePlacementEntry bed2 = new ModulePlacementEntry();
                bed2.thingDef = bedDef;
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

                // Dining Chairs facing East
                ModulePlacementEntry chair1 = new ModulePlacementEntry();
                chair1.thingDef = ThingDefOf.DiningChair;
                chair1.stuffDef = woodStuff;
                chair1.position = new IntVec3(cx - 3, 0, cz);
                chair1.rotation = Rot4.East;
                plan.entries.Add(chair1);

                ModulePlacementEntry chair2 = new ModulePlacementEntry();
                chair2.thingDef = ThingDefOf.DiningChair;
                chair2.stuffDef = woodStuff;
                chair2.position = new IntVec3(cx - 3, 0, cz + 1);
                chair2.rotation = Rot4.East;
                plan.entries.Add(chair2);

                // Torch bedside between bed heads at (cx + 2, cz + 3)
                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx + 2, 0, cz + 3);
                light.rotation = Rot4.South;
                plan.entries.Add(light);

                // Workstation selection matching specialization
                ThingDef wsDef = GetWorkstationDefForLevel(spec, 1, hasSmithing, hasClothing, hasStonecutting, hasMachining, hasFabrication);
                if (wsDef != null)
                {
                    ModulePlacementEntry ws = new ModulePlacementEntry();
                    ws.thingDef = wsDef;
                    ws.stuffDef = wsDef.MadeFromStuff ? woodStuff : null;
                    ws.position = new IntVec3(cx + 2, 0, cz - 1);
                    ws.rotation = Rot4.South;
                    ws.hasInteractionCell = true;
                    ws.interactionCell = new IntVec3(cx + 2, 0, cz);
                    plan.entries.Add(ws);
                }

                // Storage Shelf (2x1) for initial outpost raw supplies
                ThingDef shelfDef = DefDatabase<ThingDef>.GetNamedSilentFail("Shelf");
                if (shelfDef != null)
                {
                    ModulePlacementEntry shelf = new ModulePlacementEntry();
                    shelf.thingDef = shelfDef;
                    shelf.stuffDef = woodStuff;
                    shelf.position = new IntVec3(cx - 3, 0, cz - 2);
                    shelf.rotation = Rot4.North;
                    plan.entries.Add(shelf);
                }
            }
            else if (lvl == 2)
            {
                // =========================================================================
                // Level 2: North Pavilion (7x7) — Domestic Quarters
                // 1 regular bed (+1 -> total 3), Storage Shelf, Dresser, TorchLamp
                // =========================================================================
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = bedDef;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx + 2, 0, cz);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                // Storage Shelf (2x1) against West wall
                ThingDef shelfDef = DefDatabase<ThingDef>.GetNamedSilentFail("Shelf");
                if (shelfDef != null)
                {
                    ModulePlacementEntry shelf = new ModulePlacementEntry();
                    shelf.thingDef = shelfDef;
                    shelf.stuffDef = woodStuff;
                    shelf.position = new IntVec3(cx - 2, 0, cz);
                    shelf.rotation = Rot4.North;
                    plan.entries.Add(shelf);
                }

                // Dresser (2x1) for colonist comfort
                ThingDef dresserDef = DefDatabase<ThingDef>.GetNamedSilentFail("Dresser");
                if (dresserDef != null)
                {
                    ModulePlacementEntry dresser = new ModulePlacementEntry();
                    dresser.thingDef = dresserDef;
                    dresser.stuffDef = woodStuff;
                    dresser.position = new IntVec3(cx, 0, cz + 2);
                    dresser.rotation = Rot4.South;
                    plan.entries.Add(dresser);
                }

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx - 2, 0, cz - 1);
                light.rotation = Rot4.South;
                plan.entries.Add(light);
            }
            else if (lvl == 3)
            {
                // =========================================================================
                // Level 3: East Pavilion (7x7) — Technical Workshop / Kitchen / Processing
                // 1 regular bed (+1 -> total 4). If Electricity researched -> Generator + Conduit + Lamp
                // =========================================================================
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = bedDef;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx + 2, 0, cz - 1);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                // Specialized workstation for level 3 facing South into room
                ThingDef wsDef = GetWorkstationDefForLevel(spec, 3, hasSmithing, hasClothing, hasStonecutting, hasMachining, hasFabrication);
                if (wsDef != null)
                {
                    ModulePlacementEntry ws = new ModulePlacementEntry();
                    ws.thingDef = wsDef;
                    ws.stuffDef = wsDef.MadeFromStuff ? woodStuff : null;
                    ws.position = new IntVec3(cx, 0, cz + 2);
                    ws.rotation = Rot4.North; // interaction cell offset (0, 0, -1) points South to (cx, cz + 1)
                    ws.hasInteractionCell = true;
                    ws.interactionCell = new IntVec3(cx, 0, cz + 1);
                    plan.entries.Add(ws);
                }

                // Power Network Setup if Electricity is researched
                if (hasElectricity && spec == StrategicOutpostSpecialization.Factory)
                {
                    // Exterior generator alcove East of pavilion: x in [cx + 4, cx + 5], z in [cz + 1, cz + 2]
                    ThingDef genDef = DefDatabase<ThingDef>.GetNamedSilentFail("WoodFiredGenerator");
                    ThingDef conduitDef = DefDatabase<ThingDef>.GetNamedSilentFail("PowerConduit");
                    ThingDef lampDef = DefDatabase<ThingDef>.GetNamedSilentFail("StandingLamp");

                    if (genDef != null)
                    {
                        ModulePlacementEntry gen = new ModulePlacementEntry();
                        gen.thingDef = genDef;
                        gen.stuffDef = null;
                        gen.position = new IntVec3(cx + 5, 0, cz + 2);
                        gen.rotation = Rot4.South;
                        plan.entries.Add(gen);
                    }

                    if (conduitDef != null)
                    {
                        // Conduit line from wall across to lamp and bench (generator at cx + 4 connects to wall transmitter)
                        IntVec3[] conduitCells = new IntVec3[]
                        {
                            new IntVec3(cx + 3, 0, cz + 1), // in East wall
                            new IntVec3(cx + 2, 0, cz + 1),
                            new IntVec3(cx + 1, 0, cz + 1),
                            new IntVec3(cx, 0, cz + 1)
                        };
                        for (int cIdx = 0; cIdx < conduitCells.Length; cIdx++)
                        {
                            ModulePlacementEntry conduit = new ModulePlacementEntry();
                            conduit.thingDef = conduitDef;
                            conduit.position = conduitCells[cIdx];
                            conduit.rotation = Rot4.North;
                            plan.entries.Add(conduit);
                        }
                    }

                    if (lampDef != null)
                    {
                        ModulePlacementEntry lamp = new ModulePlacementEntry();
                        lamp.thingDef = lampDef;
                        lamp.position = new IntVec3(cx + 1, 0, cz + 1);
                        lamp.rotation = Rot4.South;
                        plan.entries.Add(lamp);
                    }
                }
                else
                {
                    ModulePlacementEntry light = new ModulePlacementEntry();
                    light.thingDef = ThingDefOf.TorchLamp;
                    light.position = new IntVec3(cx - 2, 0, cz - 1);
                    light.rotation = Rot4.South;
                    plan.entries.Add(light);

                    ThingDef shelfDef = DefDatabase<ThingDef>.GetNamedSilentFail("Shelf");
                    if (shelfDef != null)
                    {
                        ModulePlacementEntry shelf = new ModulePlacementEntry();
                        shelf.thingDef = shelfDef;
                        shelf.stuffDef = woodStuff;
                        shelf.position = new IntVec3(cx - 2, 0, cz + 1);
                        shelf.rotation = Rot4.North;
                        plan.entries.Add(shelf);
                    }
                }
            }
            else if (lvl == 4)
            {
                // =========================================================================
                // Level 4: South Pavilion (7x7) — Recreation & Barracks
                // 1 regular bed (+1 -> total 5), ChessTable with chairs, lighting
                // =========================================================================
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = bedDef;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx + 2, 0, cz);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                ThingDef chessDef = DefDatabase<ThingDef>.GetNamedSilentFail("ChessTable");
                if (chessDef != null)
                {
                    ModulePlacementEntry chess = new ModulePlacementEntry();
                    chess.thingDef = chessDef;
                    chess.stuffDef = woodStuff;
                    chess.position = new IntVec3(cx - 1, 0, cz);
                    chess.rotation = Rot4.North;
                    plan.entries.Add(chess);

                    ModulePlacementEntry c1 = new ModulePlacementEntry();
                    c1.thingDef = ThingDefOf.DiningChair;
                    c1.stuffDef = woodStuff;
                    c1.position = new IntVec3(cx - 2, 0, cz);
                    c1.rotation = Rot4.East;
                    plan.entries.Add(c1);

                    ModulePlacementEntry c2 = new ModulePlacementEntry();
                    c2.thingDef = ThingDefOf.DiningChair;
                    c2.stuffDef = woodStuff;
                    c2.position = new IntVec3(cx, 0, cz);
                    c2.rotation = Rot4.West;
                    plan.entries.Add(c2);
                }

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx - 2, 0, cz + 2);
                light.rotation = Rot4.South;
                plan.entries.Add(light);
            }
            else if (lvl == 5)
            {
                // =========================================================================
                // Level 5: West Pavilion (7x7) — Depot & Logistics
                // 1 regular bed (+1 -> total 6), 2 Shelves, sandbag on exterior street
                // =========================================================================
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = bedDef;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx - 2, 0, cz);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                ThingDef shelfDef = DefDatabase<ThingDef>.GetNamedSilentFail("Shelf");
                if (shelfDef != null)
                {
                    ModulePlacementEntry s1 = new ModulePlacementEntry();
                    s1.thingDef = shelfDef;
                    s1.stuffDef = woodStuff;
                    s1.position = new IntVec3(cx + 1, 0, cz + 1);
                    s1.rotation = Rot4.North;
                    plan.entries.Add(s1);

                    ModulePlacementEntry s2 = new ModulePlacementEntry();
                    s2.thingDef = shelfDef;
                    s2.stuffDef = woodStuff;
                    s2.position = new IntVec3(cx + 1, 0, cz - 1);
                    s2.rotation = Rot4.North;
                    plan.entries.Add(s2);
                }

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx - 2, 0, cz + 2);
                light.rotation = Rot4.South;
                plan.entries.Add(light);

                // Exterior sandbag flanking entrance (offset perpendicular to door axis)
                IntVec3 sbCell = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(0, 0, 1);
                ModulePlacementEntry sb = new ModulePlacementEntry();
                sb.thingDef = ThingDefOf.Sandbags;
                sb.stuffDef = clothStuff;
                sb.position = sbCell;
                sb.rotation = Rot4.South;
                plan.entries.Add(sb);
            }
            else if (lvl == 6)
            {
                // =========================================================================
                // Level 6: North-East Pavilion (7x7) — Specialized Wing
                // 1 regular bed (+1 -> total 7). Factory gets Machining if researched
                // =========================================================================
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = bedDef;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx + 2, 0, cz);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                ThingDef wsDef = GetWorkstationDefForLevel(spec, 6, hasSmithing, hasClothing, hasStonecutting, hasMachining, hasFabrication);
                if (wsDef != null)
                {
                    ModulePlacementEntry ws = new ModulePlacementEntry();
                    ws.thingDef = wsDef;
                    ws.stuffDef = wsDef.MadeFromStuff ? woodStuff : null;
                    ws.position = new IntVec3(cx, 0, cz + 2);
                    ws.rotation = Rot4.North; // interaction cell offset (0, 0, -1) points South to (cx, cz + 1)
                    ws.hasInteractionCell = true;
                    ws.interactionCell = new IntVec3(cx, 0, cz + 1);
                    plan.entries.Add(ws);
                }

                ThingDef shelfDef = DefDatabase<ThingDef>.GetNamedSilentFail("Shelf");
                if (shelfDef != null)
                {
                    ModulePlacementEntry shelf = new ModulePlacementEntry();
                    shelf.thingDef = shelfDef;
                    shelf.stuffDef = woodStuff;
                    shelf.position = new IntVec3(cx - 2, 0, cz - 1);
                    shelf.rotation = Rot4.North;
                    plan.entries.Add(shelf);
                }

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx + 2, 0, cz + 2);
                light.rotation = Rot4.South;
                plan.entries.Add(light);
            }
            else if (lvl == 7)
            {
                // =========================================================================
                // Level 7: South-East Pavilion (7x7) — Defense & Quarters
                // 1 regular bed (+1 -> total 8), Supply Shelf, TorchLamp
                // =========================================================================
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = bedDef;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx + 2, 0, cz);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                ThingDef shelfDef = DefDatabase<ThingDef>.GetNamedSilentFail("Shelf");
                if (shelfDef != null)
                {
                    ModulePlacementEntry shelf = new ModulePlacementEntry();
                    shelf.thingDef = shelfDef;
                    shelf.stuffDef = woodStuff;
                    shelf.position = new IntVec3(cx - 2, 0, cz);
                    shelf.rotation = Rot4.North;
                    plan.entries.Add(shelf);
                }

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx, 0, cz - 1);
                light.rotation = Rot4.South;
                plan.entries.Add(light);

                // Watchtower exterior defense: Turret if researched, else Sandbags
                if (spec == StrategicOutpostSpecialization.Watchtower && hasTurrets && hasElectricity)
                {
                    ThingDef turretDef = DefDatabase<ThingDef>.GetNamedSilentFail("Turret_MiniTurret");
                    if (turretDef != null)
                    {
                        IntVec3 turretCell = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(2, 0, 0);
                        ModulePlacementEntry turret = new ModulePlacementEntry();
                        turret.thingDef = turretDef;
                        turret.stuffDef = steelStuff;
                        turret.position = turretCell;
                        turret.rotation = Rot4.South;
                        plan.entries.Add(turret);
                    }
                }
                else
                {
                    IntVec3 sbCell = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(1, 0, 0);
                    ModulePlacementEntry sb = new ModulePlacementEntry();
                    sb.thingDef = ThingDefOf.Sandbags;
                    sb.stuffDef = clothStuff;
                    sb.position = sbCell;
                    sb.rotation = Rot4.South;
                    plan.entries.Add(sb);
                }
            }
            else if (lvl == 8)
            {
                // =========================================================================
                // Level 8: South-West Pavilion (7x7) — Domestic Living Quarters
                // 1 regular bed (+1 -> total 9), Dresser, EndTable, Recreation
                // =========================================================================
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = bedDef;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx + 2, 0, cz);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                ThingDef dresserDef = DefDatabase<ThingDef>.GetNamedSilentFail("Dresser");
                if (dresserDef != null)
                {
                    ModulePlacementEntry dresser = new ModulePlacementEntry();
                    dresser.thingDef = dresserDef;
                    dresser.stuffDef = woodStuff;
                    dresser.position = new IntVec3(cx - 1, 0, cz + 2);
                    dresser.rotation = Rot4.South;
                    plan.entries.Add(dresser);
                }

                ThingDef shelfDef = DefDatabase<ThingDef>.GetNamedSilentFail("Shelf");
                if (shelfDef != null)
                {
                    ModulePlacementEntry shelf = new ModulePlacementEntry();
                    shelf.thingDef = shelfDef;
                    shelf.stuffDef = woodStuff;
                    shelf.position = new IntVec3(cx - 2, 0, cz - 1);
                    shelf.rotation = Rot4.North;
                    plan.entries.Add(shelf);
                }

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx + 2, 0, cz + 2);
                light.rotation = Rot4.South;
                plan.entries.Add(light);
            }
            else if (lvl == 9)
            {
                // =========================================================================
                // Level 9: North-West Pavilion (7x7) — Advanced Industry / Garrison Keep
                // 1 regular bed (+1 -> total 10). Factory gets FabricationBench if researched
                // Workstation at (cx, 0, cz + 1) occupies z: cz+1..cz+2, x: cx-2..cx+2 (5x2).
                // Bed placed at (cx + 2, 0, cz - 1) to avoid overlap with 5-wide bench!
                // =========================================================================
                ModulePlacementEntry bed = new ModulePlacementEntry();
                bed.thingDef = bedDef;
                bed.stuffDef = woodStuff;
                bed.position = new IntVec3(cx + 2, 0, cz - 1);
                bed.rotation = Rot4.South;
                plan.entries.Add(bed);

                ThingDef wsDef = GetWorkstationDefForLevel(spec, 9, hasSmithing, hasClothing, hasStonecutting, hasMachining, hasFabrication);
                if (wsDef != null)
                {
                    ModulePlacementEntry ws = new ModulePlacementEntry();
                    ws.thingDef = wsDef;
                    ws.stuffDef = wsDef.MadeFromStuff ? woodStuff : null;
                    ws.position = new IntVec3(cx, 0, cz + 1);
                    ws.rotation = Rot4.North; // interaction cell offset (0, 0, -1) points South to (cx, cz)
                    ws.hasInteractionCell = true;
                    ws.interactionCell = new IntVec3(cx, 0, cz);
                    plan.entries.Add(ws);
                }

                ThingDef shelfDef = DefDatabase<ThingDef>.GetNamedSilentFail("Shelf");
                if (shelfDef != null)
                {
                    ModulePlacementEntry shelf = new ModulePlacementEntry();
                    shelf.thingDef = shelfDef;
                    shelf.stuffDef = woodStuff;
                    shelf.position = new IntVec3(cx - 2, 0, cz - 1);
                    shelf.rotation = Rot4.North;
                    plan.entries.Add(shelf);
                }

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx - 2, 0, cz);
                light.rotation = Rot4.South;
                plan.entries.Add(light);
            }
            else if (lvl == 10)
            {
                // =========================================================================
                // Level 10: Command Redoubt (7x7) — High Bastion & Redoubt
                // 2 regular beds (+2 -> total 12 regular beds, >= 11 strictly satisfied!)
                // Fortified sandbags flanking entrance
                // =========================================================================
                ModulePlacementEntry b1 = new ModulePlacementEntry();
                b1.thingDef = bedDef;
                b1.stuffDef = woodStuff;
                b1.position = new IntVec3(cx - 1, 0, cz);
                b1.rotation = Rot4.South;
                plan.entries.Add(b1);

                ModulePlacementEntry b2 = new ModulePlacementEntry();
                b2.thingDef = bedDef;
                b2.stuffDef = woodStuff;
                b2.position = new IntVec3(cx + 1, 0, cz);
                b2.rotation = Rot4.South;
                plan.entries.Add(b2);

                ModulePlacementEntry light = new ModulePlacementEntry();
                light.thingDef = ThingDefOf.TorchLamp;
                light.position = new IntVec3(cx, 0, cz - 1);
                light.rotation = Rot4.South;
                plan.entries.Add(light);

                // Exterior fortified sandbags flanking the door
                IntVec3 sandbagLeft = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(-1, 0, 0);
                IntVec3 sandbagRight = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(1, 0, 0);

                ModulePlacementEntry sbLeft = new ModulePlacementEntry();
                sbLeft.thingDef = ThingDefOf.Sandbags;
                sbLeft.stuffDef = clothStuff;
                sbLeft.position = sandbagLeft;
                sbLeft.rotation = Rot4.South;
                plan.entries.Add(sbLeft);

                ModulePlacementEntry sbRight = new ModulePlacementEntry();
                sbRight.thingDef = ThingDefOf.Sandbags;
                sbRight.stuffDef = clothStuff;
                sbRight.position = sandbagRight;
                sbRight.rotation = Rot4.South;
                plan.entries.Add(sbRight);

                // If Watchtower and Turrets researched: place defensive mini-turret
                if (spec == StrategicOutpostSpecialization.Watchtower && hasTurrets && hasElectricity)
                {
                    ThingDef turretDef = DefDatabase<ThingDef>.GetNamedSilentFail("Turret_MiniTurret");
                    if (turretDef != null)
                    {
                        IntVec3 turretCell = plan.doorCell + plan.doorFacing.FacingCell + new IntVec3(2, 0, 0);
                        ModulePlacementEntry turret = new ModulePlacementEntry();
                        turret.thingDef = turretDef;
                        turret.stuffDef = steelStuff;
                        turret.position = turretCell;
                        turret.rotation = Rot4.South;
                        plan.entries.Add(turret);
                    }
                }
            }

            return plan;
        }

        private ThingDef GetWorkstationDefForLevel(StrategicOutpostSpecialization spec, int lvl,
            bool hasSmithing, bool hasClothing, bool hasStonecutting, bool hasMachining, bool hasFabrication)
        {
            switch (spec)
            {
                case StrategicOutpostSpecialization.Factory:
                    if (lvl >= 9 && hasFabrication)
                    {
                        return DefDatabase<ThingDef>.GetNamedSilentFail("FabricationBench") ?? DefDatabase<ThingDef>.GetNamedSilentFail("TableMachining");
                    }
                    if (lvl >= 6 && hasMachining)
                    {
                        return DefDatabase<ThingDef>.GetNamedSilentFail("TableMachining") ?? DefDatabase<ThingDef>.GetNamedSilentFail("FueledSmithy");
                    }
                    if (hasSmithing)
                    {
                        return DefDatabase<ThingDef>.GetNamedSilentFail("FueledSmithy");
                    }
                    if (hasClothing)
                    {
                        return DefDatabase<ThingDef>.GetNamedSilentFail("HandTailoringBench");
                    }
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CraftingSpot");

                case StrategicOutpostSpecialization.Farming:
                    if (lvl >= 3)
                    {
                        return DefDatabase<ThingDef>.GetNamedSilentFail("FueledStove") ?? DefDatabase<ThingDef>.GetNamedSilentFail("TableButcher");
                    }
                    return DefDatabase<ThingDef>.GetNamedSilentFail("TableButcher") ?? DefDatabase<ThingDef>.GetNamedSilentFail("CraftingSpot");

                case StrategicOutpostSpecialization.Mining:
                case StrategicOutpostSpecialization.Logging:
                    if (lvl >= 3 && hasSmithing)
                    {
                        return DefDatabase<ThingDef>.GetNamedSilentFail("FueledSmithy");
                    }
                    if (hasStonecutting)
                    {
                        return DefDatabase<ThingDef>.GetNamedSilentFail("TableStonecutter");
                    }
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CraftingSpot");

                case StrategicOutpostSpecialization.Watchtower:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("Table1x2c");

                default:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CraftingSpot");
            }
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

            // Terrain, protected things, and zone check across entire module bounds
            foreach (IntVec3 c in plan.bounds)
            {
                TerrainDef ter = map.terrainGrid.TerrainAt(c);
                if (ter != null && (ter.passability == Traversability.Impassable ||
                    (ter.IsWater && !ter.affordances.Contains(TerrainAffordanceDefOf.Light))))
                {
                    failReason = "Impassable or water terrain inside module footprint";
                    return false;
                }

                // Check ZoneAt(c) inside module bounds before ANY mutations
                if (map.zoneManager != null && map.zoneManager.ZoneAt(c) != null)
                {
                    failReason = "Module footprint overlaps existing zone: " + map.zoneManager.ZoneAt(c).label;
                    return false;
                }

                List<Thing> thingList = c.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing t = thingList[i];
                    if (t is Pawn) continue; // Pawns are mobile living entities that step aside
                    if (IsProtectedFromModuleWipe(t))
                    {
                        failReason = "Footprint blocked by protected entity: " + t.LabelCap;
                        return false;
                    }
                }
            }

            // Check planned entries (interior and exterior)
            for (int i = 0; i < plan.entries.Count; i++)
            {
                ModulePlacementEntry entry = plan.entries[i];
                CellRect occ = entry.GetOccupiedRect();
                foreach (IntVec3 oc in occ)
                {
                    if (!oc.InBounds(map))
                    {
                        failReason = "Planned entry outside map bounds: " + entry.thingDef.label;
                        return false;
                    }
                    TerrainDef oTer = map.terrainGrid.TerrainAt(oc);
                    if (oTer != null && oTer.passability == Traversability.Impassable)
                    {
                        failReason = "Planned entry cell impassable: " + entry.thingDef.label;
                        return false;
                    }

                    // Strict terrain affordance check (generator, turret, sandbags, interior entries on floor)
                    if (entry.thingDef != null)
                    {
                        if (plan.bounds.Contains(oc))
                        {
                            if (plan.floorDef != null && !GenConstruct.CanBuildOnTerrain(plan.floorDef, oc, map, Rot4.North))
                            {
                                failReason = "Terrain at " + oc + " cannot support floor " + plan.floorDef.label;
                                return false;
                            }
                        }
                        else
                        {
                            bool canBuildExterior = GenConstruct.CanBuildOnTerrain(entry.thingDef, oc, map, entry.rotation, null, entry.stuffDef);
                            if (!canBuildExterior && plan.floorDef != null && GenConstruct.CanBuildOnTerrain(plan.floorDef, oc, map, Rot4.North))
                            {
                                canBuildExterior = true;
                            }
                            if (!canBuildExterior)
                            {
                                failReason = "Terrain at " + oc + " cannot support exterior " + entry.thingDef.label;
                                return false;
                            }
                            if (oTer != null && oTer.IsWater && (entry.thingDef.terrainAffordanceNeeded == null || !oTer.affordances.Contains(entry.thingDef.terrainAffordanceNeeded)))
                            {
                                failReason = "Water terrain cannot support " + entry.thingDef.label;
                                return false;
                            }
                        }
                    }

                    // 1. Must not overlap any Zone (e.g. Zone_Growing) inside or outside bounds
                    if (map.zoneManager != null && map.zoneManager.ZoneAt(oc) != null)
                    {
                        failReason = "Planned entry " + entry.thingDef.label + " overlaps zone: " + map.zoneManager.ZoneAt(oc).label;
                        return false;
                    }

                    // Checks specifically for exterior elements outside module bounds
                    if (!plan.bounds.Contains(oc))
                    {
                        // 2. Must not block the street cell immediately outside any module door
                        if (oc == plan.doorCell + plan.doorFacing.FacingCell)
                        {
                            failReason = "Exterior entry " + entry.thingDef.label + " blocks module door access";
                            return false;
                        }

                        if (map.listerBuildings != null)
                        {
                            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
                            for (int bIdx = 0; bIdx < buildings.Count; bIdx++)
                            {
                                Building b = buildings[bIdx];
                                // Doors check
                                if (b is Building_Door)
                                {
                                    if (oc == b.Position || oc == b.Position + Rot4.North.FacingCell || oc == b.Position + Rot4.South.FacingCell || oc == b.Position + Rot4.East.FacingCell || oc == b.Position + Rot4.West.FacingCell)
                                    {
                                        failReason = "Exterior entry blocks door access at " + b.Position;
                                        return false;
                                    }
                                }
                                // 3. Must not overlap interaction cell of any workbench or building
                                if (b.def.hasInteractionCell && b.InteractionCell == oc)
                                {
                                    failReason = "Exterior entry overlaps interaction cell of " + b.LabelCap;
                                    return false;
                                }
                                // 4. Must not overlap any bed cell of another module
                                if (b is Building_Bed && b.OccupiedRect().Contains(oc))
                                {
                                    failReason = "Exterior entry overlaps existing bed at " + b.Position;
                                    return false;
                                }
                            }
                        }
                    }

                    // Check protected things on target cell
                    List<Thing> occThings = oc.GetThingList(map);
                    for (int tIdx = 0; tIdx < occThings.Count; tIdx++)
                    {
                        Thing ot = occThings[tIdx];
                        if (ot is Pawn) continue;
                        if (IsProtectedFromModuleWipe(ot))
                        {
                            failReason = "Entry location blocked by protected entity: " + ot.LabelCap;
                            return false;
                        }
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
                    if (t is Pawn) continue;
                    if (IsProtectedFromModuleWipe(t))
                    {
                        return false;
                    }
                }
            }

            if (lvl > 0)
            {
                StrategicOutpostSpecialization spec = Outpost != null ? Outpost.specialization : StrategicOutpostSpecialization.Mining;
                ModuleLayoutPlan plan = GenerateLayoutPlan(lvl, rect, spec);
                string fail;
                if (!CanSafelyExecutePlan(plan, out fail))
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

            // 12 alternate candidate offsets matching blocked validation contract (cardinal, diagonal, double-step)
            IntVec3[] offsets = new IntVec3[]
            {
                new IntVec3(0, 0, candidate.Height + 2),
                new IntVec3(0, 0, -(candidate.Height + 2)),
                new IntVec3(candidate.Width + 2, 0, 0),
                new IntVec3(-(candidate.Width + 2), 0, 0),
                new IntVec3(candidate.Width + 2, 0, candidate.Height + 2),
                new IntVec3(-(candidate.Width + 2), 0, candidate.Height + 2),
                new IntVec3(candidate.Width + 2, 0, -(candidate.Height + 2)),
                new IntVec3(-(candidate.Width + 2), 0, -(candidate.Height + 2)),
                new IntVec3(0, 0, (candidate.Height + 2) * 2),
                new IntVec3(0, 0, -(candidate.Height + 2) * 2),
                new IntVec3((candidate.Width + 2) * 2, 0, 0),
                new IntVec3(-(candidate.Width + 2) * 2, 0, 0)
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

            // For higher levels (e.g. Level 10) where candidate/local offsets might be blocked by mountains or core modules,
            // check alternate perimeter quadrants around the base core (South, East, West, corners)
            if (targetLevel > 2)
            {
                int cx = baseCenter.x;
                int cz = baseCenter.z;
                int w = candidate.Width;
                int h = candidate.Height;

                CellRect[] altCandidates = new CellRect[]
                {
                    new CellRect(cx - 3, cz - 23, w, h), // Far South
                    new CellRect(cx + 17, cz - 3, w, h), // Far East
                    new CellRect(cx - 23, cz - 3, w, h), // Far West
                    new CellRect(cx + 17, cz + 17, w, h), // Far North-East
                    new CellRect(cx + 17, cz - 23, w, h), // Far South-East
                    new CellRect(cx - 23, cz - 23, w, h), // Far South-West
                    new CellRect(cx - 23, cz + 17, w, h)  // Far North-West
                };

                for (int cIdx = 0; cIdx < altCandidates.Length; cIdx++)
                {
                    CellRect alt = altCandidates[cIdx];
                    if (CanSafelyPlaceModule(alt, targetLevel))
                    {
                        rect = alt;
                        return true;
                    }
                    for (int oIdx = 0; oIdx < offsets.Length; oIdx++)
                    {
                        CellRect altShifted = alt.MovedBy(offsets[oIdx]);
                        if (CanSafelyPlaceModule(altShifted, targetLevel))
                        {
                            rect = altShifted;
                            return true;
                        }
                    }
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

            // Clear natural vegetation/rocks only within footprint
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

            // Spawn planned entries with strict faction ownership and stuff fallback
            for (int i = 0; i < plan.entries.Count; i++)
            {
                ModulePlacementEntry entry = plan.entries[i];
                if (entry.thingDef != null && entry.position.InBounds(map))
                {
                    // If exterior cell, clear natural flora/rocks on that cell before spawning
                    if (!plan.bounds.Contains(entry.position))
                    {
                        CellRect occ = entry.GetOccupiedRect();
                        foreach (IntVec3 oc in occ)
                        {
                            List<Thing> extThings = oc.GetThingList(map);
                            List<Thing> extClear = new List<Thing>();
                            for (int k = 0; k < extThings.Count; k++)
                            {
                                Thing t = extThings[k];
                                if (t != null && !IsProtectedFromModuleWipe(t) && t.def.destroyable)
                                {
                                    extClear.Add(t);
                                }
                            }
                            for (int k = 0; k < extClear.Count; k++)
                            {
                                extClear[k].Destroy(DestroyMode.Vanish);
                            }

                            if (plan.floorDef != null && entry.thingDef.terrainAffordanceNeeded != null && oc.InBounds(map))
                            {
                                map.terrainGrid.SetTerrain(oc, plan.floorDef);
                            }
                        }
                    }

                    Thing spawned = ThingMaker.MakeThing(entry.thingDef, entry.stuffDef ?? (entry.thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(entry.thingDef) : null));
                    spawned.SetFaction(Faction.OfPlayer);

                    // If medical bed, set medical flag
                    if (entry.isMedicalBed)
                    {
                        Building_Bed bed = spawned as Building_Bed;
                        if (bed != null)
                        {
                            bed.Medical = true;
                        }
                    }

                    GenSpawn.Spawn(spawned, entry.position, map, entry.rotation, WipeMode.Vanish, false, false);

                    // Power Generator: start empty (0 fuel), requiring colonist refueling
                    if (entry.thingDef.defName == "WoodFiredGenerator")
                    {
                        CompRefuelable compRefuel = spawned.TryGetComp<CompRefuelable>();
                        if (compRefuel != null && compRefuel.Fuel > 0f)
                        {
                            compRefuel.ConsumeFuel(compRefuel.Fuel);
                        }
                        hasSpawnedGenerator = true;
                    }
                }
            }

            // Farming Specialization: Create native Zone_Growing near campus without duplicates
            if (spec == StrategicOutpostSpecialization.Farming)
            {
                if (lvl == 1 && !HasGrowingZoneForLevel(1))
                {
                    TryCreateOutpostGrowingZone(rect, "Plant_Rice", 1);
                }
                else if (lvl == 3 && !HasGrowingZoneForLevel(3))
                {
                    TryCreateOutpostGrowingZone(rect, "Plant_Potato", 3);
                }
                else if (lvl == 5 && !HasGrowingZoneForLevel(5))
                {
                    TryCreateOutpostGrowingZone(rect, "Plant_Cotton", 5);
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

        public bool HasGrowingZoneForLevel(int tierLevel)
        {
            if (growingZoneLevelsCreated == null) return false;
            return growingZoneLevelsCreated.Contains(tierLevel);
        }

        public bool TryCreateOutpostGrowingZone(CellRect moduleRect, string plantDefName, int tierLevel = 1)
        {
            if (map == null || map.zoneManager == null) return false;
            if (HasGrowingZoneForLevel(tierLevel)) return true;

            // Search candidate positions in expanding rings around module/campus for 6x6 fertile soil
            List<IntVec3> searchOffsets = new List<IntVec3>();
            int[] distances = new int[] { 8, 15, 22, 29, 36 };
            for (int d = 0; d < distances.Length; d++)
            {
                int dist = distances[d];
                searchOffsets.Add(new IntVec3(dist, 0, 0));
                searchOffsets.Add(new IntVec3(-dist, 0, 0));
                searchOffsets.Add(new IntVec3(0, 0, dist));
                searchOffsets.Add(new IntVec3(0, 0, -dist));
                searchOffsets.Add(new IntVec3(dist, 0, dist));
                searchOffsets.Add(new IntVec3(dist, 0, -dist));
                searchOffsets.Add(new IntVec3(-dist, 0, dist));
                searchOffsets.Add(new IntVec3(-dist, 0, -dist));
            }

            CellRect zoneRect = CellRect.Empty;
            for (int o = 0; o < searchOffsets.Count; o++)
            {
                CellRect cand = new CellRect(moduleRect.minX + searchOffsets[o].x, moduleRect.minZ + searchOffsets[o].z, 6, 6);
                if (cand.minX < 6 || cand.maxX >= map.Size.x - 6 || cand.minZ < 6 || cand.maxZ >= map.Size.z - 6)
                    continue;

                bool allValid = true;
                int fertileCount = 0;
                foreach (IntVec3 c in cand)
                {
                    // Must not overlap roof or ANY existing player zone
                    if (map.roofGrid.RoofAt(c) != null || map.zoneManager.ZoneAt(c) != null)
                    {
                        allValid = false;
                        break;
                    }
                    TerrainDef ter = map.terrainGrid.TerrainAt(c);
                    if (ter != null && (ter.passability == Traversability.Impassable || ter.IsWater))
                    {
                        allValid = false;
                        break;
                    }
                    List<Thing> thingList = c.GetThingList(map);
                    for (int t = 0; t < thingList.Count; t++)
                    {
                        if (IsProtectedFromModuleWipe(thingList[t]))
                        {
                            allValid = false;
                            break;
                        }
                    }
                    if (!allValid) break;

                    if (c.GetFertility(map) >= 0.7f)
                    {
                        fertileCount++;
                    }
                }

                if (allValid && fertileCount >= 16)
                {
                    zoneRect = cand;
                    break;
                }
            }

            if (zoneRect.IsEmpty)
            {
                return false;
            }

            ThingDef plantDef = DefDatabase<ThingDef>.GetNamedSilentFail(plantDefName);
            if (plantDef == null)
            {
                plantDef = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Rice");
            }

            Zone_Growing zone = new Zone_Growing(map.zoneManager);
            if (plantDef != null)
            {
                zone.SetPlantDefToGrow(plantDef);
            }
            zone.allowSow = true;
            zone.allowCut = true;
            map.zoneManager.RegisterZone(zone);

            foreach (IntVec3 c in zoneRect)
            {
                // Only add fertile unzoned cells
                if (c.GetFertility(map) >= 0.7f && map.zoneManager.ZoneAt(c) == null && map.roofGrid.RoofAt(c) == null)
                {
                    List<Thing> toClear = new List<Thing>();
                    List<Thing> things = c.GetThingList(map);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Thing t = things[i];
                        if (t != null && !IsProtectedFromModuleWipe(t) && t.def.destroyable)
                        {
                            toClear.Add(t);
                        }
                    }
                    for (int i = 0; i < toClear.Count; i++)
                    {
                        toClear[i].Destroy(DestroyMode.Vanish);
                    }

                    if (c.GetFirstBuilding(map) == null)
                    {
                        zone.AddCell(c);
                    }
                }
            }

            if (zone.CellCount > 0)
            {
                farmingZonesCreated++;
                growingZoneLevelsCreated.Add(tierLevel);
                outpostGrowingZoneIDs.Add(zone.ID);
                return true;
            }
            return false;
        }
    }
}
