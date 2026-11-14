using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    public sealed class GreenhousePart : IExposable
    {
        public IntVec3 cell;
        public ThingDef def;
        public bool created;
        public void ExposeData() { Scribe_Values.Look(ref cell, "cell"); Scribe_Defs.Look(ref def, "def"); Scribe_Values.Look(ref created, "created"); }
    }
    public sealed class GreenhouseExpansion : IExposable
    {
        public int x, z, width, height;
        public bool committed;
        public IntVec3 climate, door;
        public List<GreenhousePart> parts = new List<GreenhousePart>();
        public List<Building> obsolete = new List<Building>();
        public CellRect Rect { get { return new CellRect(x, z, width, height); } }
        public void ExposeData()
        {
            Scribe_Values.Look(ref x, "x"); Scribe_Values.Look(ref z, "z"); Scribe_Values.Look(ref width, "width"); Scribe_Values.Look(ref height, "height");
            Scribe_Values.Look(ref climate, "climate"); Scribe_Values.Look(ref door, "door"); Scribe_Values.Look(ref committed, "committed");
            Scribe_Collections.Look(ref parts, "parts", LookMode.Deep); Scribe_Collections.Look(ref obsolete, "obsolete", LookMode.Reference);
        }
    }
    public sealed class Designator_GreenhouseExpansion : Designator_ZoneAdd_Growing
    {
        private readonly Zone_Greenhouse zone;
        public Designator_GreenhouseExpansion(Zone_Greenhouse zone)
        {
            this.zone = zone; defaultLabel = "TMM.Farming.Expand".Translate(); defaultDesc = "TMM.Farming.ExpandHelp".Translate();
            hotKey = null; tutorTag = null;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return zone != null && zone.Map == Map && c.InBounds(Map) && !c.Fogged(Map) &&
                !zone.Footprint.Contains(c) && Map.zoneManager.ZoneAt(c) == null;
        }
        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            if (!zone.TryExpand(cells)) Messages.Message("TMM.Farming.ExpansionInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
        }
    }
    public sealed partial class Zone_Greenhouse
    {
        public IntVec3 climatePosition = IntVec3.Invalid, doorPosition = IntVec3.Invalid;
        public GreenhouseExpansion expansion;
        private void ExposeExpansionData()
        {
            Scribe_Values.Look(ref climatePosition, "TMM_climatePosition", IntVec3.Invalid);
            Scribe_Values.Look(ref doorPosition, "TMM_doorPosition", IntVec3.Invalid);
            Scribe_Deep.Look(ref expansion, "TMM_expansion");
        }
        private static bool PartDef(ThingDef def)
        {
            return def != null && (def.defName == "TMM_GreenhouseGlassWall" || def.defName == "TMM_GreenhouseGlassDoor" || def.defName == "TMM_GreenhouseClimateUnit");
        }
        public bool TryExpand(IEnumerable<IntVec3> selected)
        {
            if (!planned || expansion != null || !Enclosed) return false;
            var cells = selected.ToArray(); if (cells.Length == 0 || !cells.Any(c => Footprint.ExpandedBy(1).Contains(c))) return false;
            int x = System.Math.Min(minX, cells.Min(c => c.x)), z = System.Math.Min(minZ, cells.Min(c => c.z));
            int w = System.Math.Max(Footprint.maxX, cells.Max(c => c.x)) - x + 1, h = System.Math.Max(Footprint.maxZ, cells.Max(c => c.z)) - z + 1;
            var rect = new CellRect(x, z, w, h);
            if (w > 64 || h > 64 || w < width || h < height || (w == width && h == height)) return false;
            if(Map.zoneManager.AllZones.OfType<Zone_Greenhouse>().Any(o=>o!=this&&o.planned&&rect.Any(c=>o.Footprint.Contains(c)||(o.expansion!=null&&o.expansion.Rect.Contains(c)))))return false;
            foreach (var c in rect) {
                if (!c.InBounds(Map) || c.Fogged(Map)) return false;
                var other = Map.zoneManager.ZoneAt(c);
                if (other != null && other != this) return false;
                if (!Footprint.Contains(c) && (c.Roofed(Map) || c.GetTerrain(Map).fertility <= 0f)) return false;
            }
            var edges = rect.EdgeCells.ToArray();
            var freeEdges = edges.Where(c => !Footprint.Contains(c) && c.GetEdifice(Map) == null).ToList();
            var pending = new GreenhouseExpansion { x = x, z = z, width = w, height = h };
            pending.climate = edges.Contains(ClimateCell) ? ClimateCell : freeEdges.OrderBy(c => c.DistanceToSquared(ClimateCell)).FirstOrDefault();
            if (!edges.Contains(ClimateCell) && freeEdges.Count == 0) return false;
            freeEdges.Remove(pending.climate);
            if (edges.Contains(DoorCell)) pending.door = DoorCell;
            else { if (freeEdges.Count == 0) return false; pending.door = freeEdges.OrderBy(c => c.DistanceToSquared(DoorCell)).First(); }
            var wall = DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseGlassWall");
            foreach (var c in edges) pending.parts.Add(new GreenhousePart { cell = c, def = DefDatabase<ThingDef>.GetNamed(c == pending.climate ? "TMM_GreenhouseClimateUnit" : c == pending.door ? "TMM_GreenhouseGlassDoor" : "TMM_GreenhouseGlassWall") });
            if (w > 12 && h > 12)
                for (int px = x + 6; px < x + w - 1; px += 6) for (int pz = z + 6; pz < z + h - 1; pz += 6)
                    if(rect.ContractedBy(6).Contains(new IntVec3(px,0,pz)))pending.parts.Add(new GreenhousePart { cell = new IntVec3(px, 0, pz), def = wall });
            // Keep the old enclosure until the new roof is finished. Each
            // disconnected extension must have its own paid outside door so
            // roof builders can enter before old partitions are taken down.
            var posts=new HashSet<IntVec3>(pending.parts.Where(p=>!edges.Contains(p.cell)).Select(p=>p.cell));
            var remaining=new HashSet<IntVec3>(rect.ContractedBy(1).Where(c=>!Footprint.Contains(c)&&c.GetEdifice(Map)==null&&!posts.Contains(c)));
            while(remaining.Count>0){
                var component=new HashSet<IntVec3>();var queue=new Queue<IntVec3>();var first=remaining.First();remaining.Remove(first);queue.Enqueue(first);
                while(queue.Count>0){var cell=queue.Dequeue();component.Add(cell);foreach(var offset in GenAdj.CardinalDirections){var next=cell+offset;if(remaining.Remove(next))queue.Enqueue(next);}}
                bool hasDoor=pending.parts.Any(p=>p.def.defName=="TMM_GreenhouseGlassDoor"&&GenAdj.CardinalDirections.Any(d=>component.Contains(p.cell+d)));
                if(hasDoor)continue;
                var available=freeEdges.Where(c=>c!=pending.climate&&GenAdj.CardinalDirections.Any(d=>component.Contains(c+d))).OrderBy(c=>c.DistanceToSquared(DoorCell)).ToArray();
                if(available.Length==0)return false;
                var doorCell=available[0];pending.parts.First(p=>p.cell==doorCell).def=DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseGlassDoor");freeEdges.Remove(doorCell);
            }
            foreach (var part in pending.parts) {
                var built = part.cell.GetEdifice(Map);
                if (built != null && PartDef(built.def)) { part.def = built.def; continue; }
                var existing = part.cell.GetThingList(Map).FirstOrDefault(t => PartDef(t.def.entityDefToBuild as ThingDef));
                if (existing != null) { part.def = (ThingDef)existing.def.entityDefToBuild; continue; }
                if (!GenConstruct.CanPlaceBlueprintAt(part.def, part.cell, Rot4.North, Map, false).Accepted) return false;
                part.created = true;
            }
            expansion = pending;
            foreach (var part in pending.parts.Where(p => p.created)) GenConstruct.PlaceBlueprintForBuild(part.def, part.cell, Map, Rot4.North, Faction.OfPlayer, null);
            return true;
        }
        public void CancelExpansion()
        {
            if (expansion == null || expansion.committed) return;
            foreach (var part in expansion.parts.Where(p => p.created))
                foreach (var thing in part.cell.GetThingList(Map).Where(t => (t is Blueprint_Build || t is Frame) && t.def.entityDefToBuild == part.def).ToArray())
                    thing.Destroy(DestroyMode.Cancel);
            expansion = null;
        }
        public void ExpansionTick()
        {
            if (expansion == null) return;
            if (!expansion.committed) {
                if (expansion.parts.Any(p => p.cell.GetEdifice(Map) == null || p.cell.GetEdifice(Map).def != p.def)) return;
                var permanentParts=new HashSet<IntVec3>(expansion.parts.Select(p=>p.cell));
                var roofCells=expansion.Rect.ContractedBy(1).Where(c=>!permanentParts.Contains(c)).ToArray();
                foreach(var c in roofCells)Map.areaManager.BuildRoof[c]=true;
                if(roofCells.Any(c=>Map.roofGrid.RoofAt(c)==null))return;
                var retained = new HashSet<IntVec3>(expansion.parts.Select(p => p.cell));
                expansion.obsolete = Footprint.Select(c => c.GetEdifice(Map)).Where(b => b != null && PartDef(b.def) && !retained.Contains(b.Position)).Distinct().ToList();
                if (Power != null) Power.PowerOutput = 0f;
                minX = expansion.x; minZ = expansion.z; width = expansion.width; height = expansion.height;
                climatePosition = expansion.climate; doorPosition = expansion.door; powerNode = ClimateCell.GetEdifice(Map);
                foreach (var c in Cells.ToArray()) RemoveCell(c);
                expansion.committed = true;
                foreach (var b in expansion.obsolete) if (Map.designationManager.DesignationOn(b, DesignationDefOf.Deconstruct) == null)
                    Map.designationManager.AddDesignation(new Designation(b, DesignationDefOf.Deconstruct));
            }
            var posts = new HashSet<IntVec3>(expansion.parts.Select(p => p.cell));
            foreach (var c in Footprint.ContractedBy(1)) {
                if (posts.Contains(c) || c.GetEdifice(Map) != null || Map.zoneManager.ZoneAt(c) != null || c.GetTerrain(Map).fertility <= 0f) continue;
                AddCell(c); Map.areaManager.BuildRoof[c] = true;
            }
            layoutDirty = true;
            if (expansion.obsolete.All(b => b == null || !b.Spawned)) expansion = null;
        }
    }
}
