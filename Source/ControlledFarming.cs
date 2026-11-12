using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class ControlledFarming
    {
        static ControlledFarming()
        {
            var h = new Harmony("turbomegamod.controlled.farming");
            h.Patch(AccessTools.Method(typeof(RoofGrid), "SetRoof"), prefix: new HarmonyMethod(typeof(ControlledFarming), "GlassRoof"));
            var glow = AccessTools.Method(typeof(GlowGrid), "GroundGlowAt");
            h.Patch(glow, postfix: new HarmonyMethod(typeof(ControlledFarming), glow.GetParameters().Length == 3 ? "Daylight17" : "Daylight16"));
        }
        public static float BasePower(string defName)
        {
            return DefDatabase<ThingDef>.GetNamed(defName).GetCompProperties<CompProperties_Power>().PowerConsumption;
        }
        public static bool LightingPeriod(Map map)
        {
            float day = GenLocalDate.DayPercent(map);
            return day >= 0.25f && day < 0.8f;
        }
        public static bool SunlightReaches(Map map, IntVec3 cell)
        {
            var roof = map.roofGrid.RoofAt(cell);
            return roof == null || roof.defName == "TMM_GreenhouseGlassRoof";
        }
        public static int HydroLightingMode(Thing parent)
        {
            if (!LightingPeriod(parent.Map)) return 2;
            return parent.Map.skyManager.CurSkyGlow >= 0.9f && parent.OccupiedRect().All(c => SunlightReaches(parent.Map, c)) ? 0 : 1;
        }
        public static Zone_Greenhouse ZoneAt(Map map, IntVec3 cell)
        {
            return map == null || !cell.InBounds(map) ? null : map.zoneManager.ZoneAt(cell) as Zone_Greenhouse;
        }
        private static void GlassRoof(IntVec3 __0, ref RoofDef __1, Map ___map)
        {
            var zone = ZoneAt(___map, __0);
            if (__1 == RoofDefOf.RoofConstructed && zone != null && zone.planned) __1 = DefDatabase<RoofDef>.GetNamed("TMM_GreenhouseGlassRoof");
        }
        private static void Daylight16(IntVec3 __0, bool __1, Map ___map, ref float __result) { TransmitDaylight(__0, ___map, ref __result); }
        private static void Daylight17(IntVec3 __0, bool __2, Map ___map, ref float __result) { if (!__2) TransmitDaylight(__0, ___map, ref __result); }
        private static void TransmitDaylight(IntVec3 c, Map map, ref float glow)
        {
            if (map == null || !c.InBounds(map)) return;
            var roof = map.roofGrid.RoofAt(c);
            if (roof != null && roof.defName == "TMM_GreenhouseGlassRoof") glow = Mathf.Max(glow, map.skyManager.CurSkyGlow);
        }
    }

    public sealed class Designator_ZoneAdd_Greenhouse : Designator_ZoneAdd_Growing
    {
        public Designator_ZoneAdd_Greenhouse()
        {
            zoneTypeToPlace = typeof(Zone_Greenhouse);
            defaultLabel = "TMM.Farming.Greenhouse".Translate();
            defaultDesc = "TMM.Farming.GreenhouseDesignator".Translate();
            hotKey = null;
            tutorTag = null;
            var custom = ContentFinder<Texture2D>.Get("UI/Farming/GreenhouseZone", false);
            if (custom != null) icon = custom;
        }
        protected override string NewZoneLabel { get { return "TMM.Farming.Greenhouse".Translate(); } }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            var existing = ControlledFarming.ZoneAt(Map, c);
            if (existing != null && existing.planned) return false;
            AcceptanceReport result = base.CanDesignateCell(c);
            if (!result.Accepted) return result;
            return c.Roofed(Map) ? (AcceptanceReport)"TMM.Farming.OutdoorOnly".Translate().RawText : result;
        }
        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            var selected = Find.Selector.SelectedZone as Zone_Greenhouse;
            if (selected != null && selected.planned) Find.Selector.ClearSelection();
            base.DesignateMultiCell(cells);
        }
        protected override Zone MakeNewZone() { return new Zone_Greenhouse(Find.CurrentMap.zoneManager); }
        protected override void FinalizeDesignationSucceeded()
        {
            base.FinalizeDesignationSucceeded();
            foreach (var zone in Map.zoneManager.AllZones.OfType<Zone_Greenhouse>().Where(z => !z.planned).ToArray())
                if (!zone.CreatePlan()) Messages.Message("TMM.Farming.PlanInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
        }
    }

    [StaticConstructorOnStartup]
    public sealed class Zone_Greenhouse : Zone_Growing
    {
        public bool systemEnabled = true;
        public bool planned;
        public int minX, minZ, width, height;
        public float targetTemperature = 21f;
        public Building powerNode;
        public List<Building> lightNodes = new List<Building>();
        internal bool layoutDirty = true;
        private int lightingMode;
        private readonly List<Mesh> frameMeshes = new List<Mesh>();
        private static Material frameMaterial;
        public Zone_Greenhouse() { }
        public Zone_Greenhouse(ZoneManager manager) : base(manager) { label = "TMM.Farming.Greenhouse".Translate(); }
        protected override Color NextZoneColor { get { return new Color(0.2f, 0.75f, 0.7f, 0.45f); } }
        public CompPowerTrader Power { get { return powerNode == null || !powerNode.Spawned ? null : powerNode.TryGetComp<CompPowerTrader>(); } }
        public CellRect Footprint { get { return new CellRect(minX, minZ, width, height); } }
        public IntVec3 ClimateCell { get { return new IntVec3(minX + width / 2, 0, minZ + height - 1); } }
        public IntVec3 DoorCell { get { return new IntVec3(minX + width / 2, 0, minZ); } }
        public Room GrowingRoom { get { return CellCount == 0 ? null : Cells[0].GetRoom(Map); } }
        public bool Enclosed { get { var r = GrowingRoom; return r != null && !r.UsesOutdoorTemperature; } }
        public bool Powered { get { return planned && systemEnabled && Enclosed && Power != null && Power.PowerOn; } }
        public bool LampsNeeded { get { return lightingMode == 1; } }
        public bool LampsOn { get { return Powered && LampsNeeded; } }
        public bool NightLightOn { get { return Powered && lightingMode == 2; } }
        public float Demand
        {
            get
            {
                return !planned || !systemEnabled || !Enclosed ? 0f : FarmingPowerBudget.Greenhouse(CellCount, LampsNeeded,
                    GrowingRoom.Temperature < targetTemperature, ControlledFarming.BasePower("SunLamp"), ControlledFarming.BasePower("Heater"),
                    lightingMode == 2, ControlledFarming.BasePower("StandingLamp"));
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref planned, "TMM_planned");
            Scribe_Values.Look(ref minX, "TMM_minX"); Scribe_Values.Look(ref minZ, "TMM_minZ"); Scribe_Values.Look(ref width, "TMM_width"); Scribe_Values.Look(ref height, "TMM_height");
            Scribe_Values.Look(ref systemEnabled, "TMM_systemEnabled", true);
            Scribe_Values.Look(ref targetTemperature, "TMM_targetTemperature", 21f);
            Scribe_References.Look(ref powerNode, "TMM_powerNode");
            Scribe_Collections.Look(ref lightNodes, "TMM_lightNodes", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) { if (lightNodes == null) lightNodes = new List<Building>(); layoutDirty = true; }
        }
        public override void AddCell(IntVec3 c) { base.AddCell(c); layoutDirty = true; }
        public override void RemoveCell(IntVec3 c) { base.RemoveCell(c); layoutDirty = true; }
        public override void PostDeregister()
        {
            Cleanup();
            base.PostDeregister();
        }
        public void Cleanup()
        {
            // Keep paid structures when the player removes only a growing zone.
            if (Power != null) Power.PowerOutput = 0f;
            powerNode = null;
            foreach (var light in lightNodes) if (light != null && !light.Destroyed) light.Destroy();
            lightNodes.Clear(); DisposeMeshes();
        }
        private void DisposeMeshes() { foreach (var mesh in frameMeshes) UnityEngine.Object.Destroy(mesh); frameMeshes.Clear(); }
        public override string GetInspectString()
        {
            return base.GetInspectString() + "\n" + "TMM.Farming.Status".Translate((!planned ? "TMM.Farming.Unplanned" : !Enclosed ? "TMM.Farming.Unfinished" : !systemEnabled ? "TMM.Farming.Disabled" : Powered ? "TMM.Farming.Powered" : "TMM.Farming.NoPower").Translate())
                + "\n" + "TMM.Farming.Demand".Translate(Demand.ToString("F0"), CellCount)
                + "\n" + "TMM.Farming.Target".Translate(targetTemperature.ToString("F0"))
                + "\n" + "TMM.Farming.Lighting".Translate((LampsOn ? "TMM.Farming.On" : NightLightOn ? "TMM.Farming.NightLight" : "TMM.Farming.Off").Translate());
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            yield return new Command_Toggle { defaultLabel = "TMM.Farming.Toggle".Translate(), defaultDesc = "TMM.Farming.ToggleDesc".Translate(), icon = TexCommand.ForbidOff,
                isActive = () => systemEnabled, toggleAction = () => { systemEnabled = !systemEnabled; SyncPowerAndLights(); } };
            yield return new Command_Action { defaultLabel = "TMM.Farming.Colder".Translate(), defaultDesc = "TMM.Farming.TargetDesc".Translate(), icon = TexCommand.ToggleVent,
                action = () => { targetTemperature = Mathf.Max(1f, targetTemperature - 1f); SyncPowerAndLights(); } };
            yield return new Command_Action { defaultLabel = "TMM.Farming.Warmer".Translate(), defaultDesc = "TMM.Farming.TargetDesc".Translate(), icon = TexCommand.ToggleVent,
                action = () => { targetTemperature = Mathf.Min(30f, targetTemperature + 1f); SyncPowerAndLights(); } };
        }
        public override IEnumerable<Gizmo> GetZoneAddGizmos() { if (!planned) yield return new Designator_ZoneAdd_Greenhouse(); }
        public bool CreatePlan()
        {
            if (planned || CellCount == 0) return planned;
            int x = Cells.Min(c => c.x), z = Cells.Min(c => c.z), w = Cells.Max(c => c.x) - x + 1, h = Cells.Max(c => c.z) - z + 1;
            if (w < 4 || h < 4 || w > 64 || h > 64 || CellCount != w * h) return false;
            var wall = DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseGlassWall"); var door = DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseGlassDoor"); var unit = DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseClimateUnit");
            var plans = new List<KeyValuePair<IntVec3, ThingDef>>();
            var rect = new CellRect(x, z, w, h); var entry = new IntVec3(x + w / 2, 0, z); var climate = new IntVec3(x + w / 2, 0, z + h - 1);
            foreach (var c in rect.EdgeCells) plans.Add(new KeyValuePair<IntVec3, ThingDef>(c, c == entry ? door : c == climate ? unit : wall));
            if (w > 12 && h > 12)
                for (int px = x + 6; px < x + w - 1; px += 6) for (int pz = z + 6; pz < z + h - 1; pz += 6)
                    plans.Add(new KeyValuePair<IntVec3, ThingDef>(new IntVec3(px, 0, pz), wall));
            foreach (var p in plans) if (!GenConstruct.CanPlaceBlueprintAt(p.Value, p.Key, Rot4.North, Map, false).Accepted) return false;
            minX = x; minZ = z; width = w; height = h; planned = true;
            foreach (var p in plans)
            {
                if (ContainsCell(p.Key)) RemoveCell(p.Key);
                GenConstruct.PlaceBlueprintForBuild(p.Value, p.Key, Map, Rot4.North, Faction.OfPlayer, null);
            }
            foreach (var c in Cells) Map.areaManager.BuildRoof[c] = true;
            layoutDirty = true; return true;
        }
        public void Synchronize()
        {
            if (!planned || CellCount == 0) return;
            if (powerNode == null || !powerNode.Spawned)
            {
                powerNode = ClimateCell.GetThingList(Map).OfType<Building_GreenhouseNode>().FirstOrDefault(n => n.def.defName == "TMM_GreenhouseClimateUnit");
            }
            if (Power == null) return;
            var controller = (Building_GreenhouseNode)powerNode;
            controller.zoneId = ID; controller.planMinX = minX; controller.planMinZ = minZ; controller.planWidth = width; controller.planHeight = height;
            // Any boundary cell next to a conduit is sufficient. There is one
            // consumer per zone; adjacent networks are never bridged together.
            CompPower transmitter = Power.connectParent;
            if (transmitter != null && (!transmitter.parent.Spawned || transmitter.PowerNet == null || !Footprint.ExpandedBy(1).Contains(transmitter.parent.Position))) transmitter = null;
            foreach (var c in Footprint)
            {
                if (transmitter != null) break;
                foreach (var offset in GenAdj.CardinalDirectionsAndInside)
                {
                    var other = c + offset; if (!other.InBounds(Map)) continue;
                    foreach (var thing in other.GetThingList(Map))
                    {
                        if (thing == powerNode) continue;
                        var comp = thing.TryGetComp<CompPower>();
                        if (comp != null && comp.Props.transmitsPower && comp.PowerNet != null) { transmitter = comp; break; }
                    }
                    if (transmitter != null) break;
                }
                if (transmitter != null) break;
            }
            if (Power.connectParent != transmitter)
            {
                if (Power.connectParent != null)
                {
                    PowerConnectionMaker.DisconnectFromPowerNet(Power);
                }
                if (transmitter != null) Power.ConnectToTransmitter(transmitter, false);
            }
            if (layoutDirty)
            {
                var desired = new HashSet<IntVec3>(); var covered = new HashSet<IntVec3>();
                foreach (var c in Cells.OrderBy(c => c.z).ThenBy(c => c.x))
                {
                    if (covered.Contains(c)) continue;
                    desired.Add(c);
                    for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++) covered.Add(c + new IntVec3(x, 0, z));
                }
                foreach (var light in lightNodes.ToArray()) if (light == null || !light.Spawned || !desired.Contains(light.Position))
                { if (light != null && !light.Destroyed) light.Destroy(); lightNodes.Remove(light); }
                foreach (var c in desired) if (!lightNodes.Any(n => n.Position == c)) lightNodes.Add(SpawnNode("TMM_GreenhouseLight", c));
                BuildFrames(); layoutDirty = false;
            }
            SyncPowerAndLights();
        }
        public void ApplyHeat()
        {
            if (!Powered || CellCount == 0) return;
            float energy = DefDatabase<ThingDef>.GetNamed("Heater").GetCompProperties<CompProperties_TempControl>().energyPerSecond * CellCount / 36f * (250f / 60f);
            GrowingRoom.Temperature += GenTemperature.ControlTemperatureTempChange(Cells[0], Map, energy, targetTemperature);
        }
        private Building SpawnNode(string defName, IntVec3 cell)
        {
            var node = (Building_GreenhouseNode)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName));
            node.zoneId = ID; node.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(node, cell, Map);
            return node;
        }
        public void SyncPowerAndLights()
        {
            lightingMode = !ControlledFarming.LightingPeriod(Map) ? 2 : Map.skyManager.CurSkyGlow < 0.9f || Cells.Any(c => !ControlledFarming.SunlightReaches(Map, c)) ? 1 : 0;
            if (Power != null) Power.PowerOutput = -Demand;
            foreach (var light in lightNodes) if (light != null && light.Spawned) light.TryGetComp<CompGreenhouseGlower>().Refresh(LampsOn ? 1 : NightLightOn ? 2 : 0);
        }
        private void BuildFrames()
        {
            DisposeMeshes();
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            foreach (var c in Cells)
            {
                // Batch low-profile growing-cover ribs into a handful of meshes.
                AddStrip(vertices, triangles, c.x + 0.04f, c.z + 0.05f, 0.025f, 0.9f);
                AddStrip(vertices, triangles, c.x + 0.94f, c.z + 0.05f, 0.025f, 0.9f);
                AddStrip(vertices, triangles, c.x + 0.05f, c.z + 0.48f, 0.9f, 0.025f);
                if (vertices.Count >= 12000) FlushMesh(vertices, triangles);
            }
            FlushMesh(vertices, triangles);
        }
        private static void AddStrip(List<Vector3> v, List<int> t, float x, float z, float w, float h)
        {
            int i = v.Count; float y = AltitudeLayer.BuildingOnTop.AltitudeFor();
            v.Add(new Vector3(x, y, z)); v.Add(new Vector3(x, y, z + h)); v.Add(new Vector3(x + w, y, z + h)); v.Add(new Vector3(x + w, y, z));
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }
        private void FlushMesh(List<Vector3> v, List<int> t)
        {
            if (v.Count == 0) return; var mesh = new Mesh(); mesh.vertices = v.ToArray(); mesh.triangles = t.ToArray(); mesh.RecalculateBounds();
            frameMeshes.Add(mesh); v.Clear(); t.Clear();
        }
        public void DrawFrames()
        {
            if (frameMaterial == null) frameMaterial = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.38f, 0.64f, 0.65f), false);
            foreach (var mesh in frameMeshes) Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, frameMaterial, 0);
        }
    }

    public sealed class Building_GreenhouseNode : Building
    {
        public int zoneId = -1;
        public int planMinX, planMinZ, planWidth, planHeight;
        public Zone_Greenhouse Owner { get { return Map == null ? null : Map.zoneManager.AllZones.OfType<Zone_Greenhouse>().FirstOrDefault(z => z.ID == zoneId &&
            (def.defName == "TMM_GreenhouseClimateUnit" ? z.ClimateCell == Position : z.ContainsCell(Position))); } }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Values.Look(ref zoneId, "TMM_zoneId", -1);
            Scribe_Values.Look(ref planMinX, "planMinX"); Scribe_Values.Look(ref planMinZ, "planMinZ");
            Scribe_Values.Look(ref planWidth, "planWidth"); Scribe_Values.Look(ref planHeight, "planHeight");
        }
        public Zone_Greenhouse RestoreZone()
        {
            if (Owner != null) return Owner;
            if (!Spawned || planWidth < 4 || planHeight < 4) return null;
            var cells = new CellRect(planMinX, planMinZ, planWidth, planHeight).ContractedBy(1)
                .Where(c => c.InBounds(Map) && Map.zoneManager.ZoneAt(c) == null && c.GetEdifice(Map) == null && c.GetTerrain(Map).fertility > 0f).ToArray();
            if (cells.Length == 0) return null;
            var zone = new Zone_Greenhouse(Map.zoneManager) { planned = true, minX = planMinX, minZ = planMinZ, width = planWidth, height = planHeight };
            Map.zoneManager.RegisterZone(zone); foreach (var c in cells) zone.AddCell(c);
            zone.powerNode = this; zoneId = zone.ID; zone.Synchronize(); return zone;
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            if (def.defName != "TMM_GreenhouseClimateUnit") yield break;
            yield return new Command_Action { defaultLabel = "TMM.Farming.Manage".Translate(), defaultDesc = "TMM.Farming.ManageDesc".Translate(), icon = TexCommand.ForbidOff,
                action = () => { var zone = Owner ?? RestoreZone(); if (zone != null) { Find.Selector.ClearSelection(); Find.Selector.Select(zone); } } };
        }
        public override void TickRare()
        {
            base.TickRare();
            if (def.defName != "TMM_GreenhouseClimateUnit") return;
            var zone = Owner;
            if (zone != null) { zone.SyncPowerAndLights(); zone.ApplyHeat(); }
            else { var power = this.TryGetComp<CompPowerTrader>(); if (power != null) power.PowerOutput = 0f; }
        }
    }
    public abstract class CompFarmingGlower : CompGlower
    {
        private int mode = -1;
        private ColorInt growColor;
        private float growRadius, growOverlight;
        public override void Initialize(CompProperties properties)
        {
            var source = (CompProperties_Glower)properties;
            growColor = source.glowColor; growRadius = source.glowRadius; growOverlight = source.overlightRadius;
            // Never change a shared ThingDef when switching a single lamp.
            base.Initialize(new CompProperties_Glower { compClass = source.compClass, glowColor = source.glowColor,
                glowRadius = source.glowRadius, overlightRadius = source.overlightRadius });
        }
        public void Refresh(int lightingMode)
        {
            if (!parent.Spawned) return;
            if (mode != lightingMode)
            {
                parent.Map.glowGrid.DeRegisterGlower(this);
                mode = lightingMode;
                Props.glowColor = mode == 2 ? new ColorInt(140, 125, 100, 0) : growColor;
                Props.overlightRadius = mode == 2 ? 0f : growOverlight;
                Props.glowRadius = growRadius;
                if (ShouldBeLitNow) parent.Map.glowGrid.RegisterGlower(this);
            }
            UpdateLit(parent.Map);
        }
    }
    public sealed class CompGreenhouseGlower : CompFarmingGlower
    {
        protected override bool ShouldBeLitNow { get { var node = parent as Building_GreenhouseNode; var zone = node == null ? null : node.Owner; return zone != null && (zone.LampsOn || zone.NightLightOn); } }
    }
    public sealed class FarmingMapComponent : MapComponent
    {
        private int nextSync;
        public FarmingMapComponent(Map map) : base(map) { }
        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame >= nextSync)
            {
                nextSync = Find.TickManager.TicksGame + 250;
                foreach (var zone in map.zoneManager.AllZones.OfType<Zone_Greenhouse>().ToArray()) zone.Synchronize();
                foreach (string name in new[] { "TMM_GreenhouseLight" })
                    foreach (var node in map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(name)).OfType<Building_GreenhouseNode>().ToArray())
                        if (node.Owner == null) node.Destroy();
            }
            else if (Find.TickManager.TicksGame % 30 == 0)
                foreach (var zone in map.zoneManager.AllZones.OfType<Zone_Greenhouse>()) { if (zone.layoutDirty) zone.Synchronize(); else zone.SyncPowerAndLights(); }
        }
        public override void MapComponentDraw() { foreach (var zone in map.zoneManager.AllZones.OfType<Zone_Greenhouse>()) zone.DrawFrames(); }
    }

    // Keep the exact native CompPowerTrader: ThingDef.ConnectToPower checks its
    // concrete type. A separate controller adjusts demand without breaking the
    // normal conduit auto-connection code.
    public sealed class CompLitHydroponicsPower : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad) { base.PostSpawnSetup(respawningAfterLoad); RefreshDemand(); }
        public override void CompTickRare() { base.CompTickRare(); RefreshDemand(); }
        public void RefreshDemand()
        {
            if (parent.Spawned)
            {
                int mode = ControlledFarming.HydroLightingMode(parent);
                parent.TryGetComp<CompPowerTrader>().PowerOutput = -FarmingPowerBudget.Hydroponics(mode == 1, ControlledFarming.BasePower("HydroponicsBasin"), ControlledFarming.BasePower("SunLamp"), mode == 2, ControlledFarming.BasePower("StandingLamp"));
                var glower = parent.TryGetComp<CompHydroponicsGlower>(); if (glower != null) glower.Refresh(mode);
            }
        }
    }
    public sealed class CompHydroponicsGlower : CompFarmingGlower
    {
        protected override bool ShouldBeLitNow { get { return parent.Spawned && parent.TryGetComp<CompPowerTrader>().PowerOn && ControlledFarming.HydroLightingMode(parent) != 0; } }
    }
    [StaticConstructorOnStartup]
    public sealed class Building_LitHydroponics : Building_PlantGrower
    {
        private static Graphic lamp;
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (lamp == null) lamp = GraphicDatabase.Get<Graphic_Multi>("Things/Farming/HydroponicLight", ShaderDatabase.Transparent, new Vector2(1f, 4f), Color.white);
            drawLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor(); lamp.Draw(drawLoc, Rotation, this);
        }
    }
}
