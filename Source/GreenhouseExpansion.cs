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
        public override AcceptanceReport CanDesignateCell(IntVec3 c) { return false; }
        public override void DesignateMultiCell(IEnumerable<IntVec3> cells) { }
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
        // Legacy public entry point retained for compatibility; no new
        // expansion plans or automatic demolition are created.
        public bool TryExpand(IEnumerable<IntVec3> selected) { return false; }
        public void CancelExpansion()
        {
            if(expansion==null)return;
            if(expansion.parts!=null)foreach(var part in expansion.parts.Where(p=>p!=null&&p.created&&p.cell.InBounds(Map)))
                foreach(var thing in part.cell.GetThingList(Map).Where(t=>(t is Blueprint_Build||t is Frame)&&t.def.entityDefToBuild==part.def).ToArray())
                    thing.Destroy(DestroyMode.Cancel);
            if(expansion.obsolete!=null)foreach(var building in expansion.obsolete.Where(b=>b!=null&&b.Spawned)){
                var designation=Map.designationManager.DesignationOn(building,DesignationDefOf.Deconstruct);
                if(designation!=null)Map.designationManager.RemoveDesignation(designation);
            }
            // Paid buildings and already built roofs stay where they are.
            // Data types/keys remain readable for existing saves.
            expansion=null;layoutDirty=true;
        }
        public void ExpansionTick() { if(expansion!=null)CancelExpansion(); }
    }
}
