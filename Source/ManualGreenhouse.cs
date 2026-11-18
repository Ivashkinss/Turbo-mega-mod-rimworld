using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class Designator_ZoneAdd_GreenhouseRoof : Designator_ZoneAdd_Growing
    {
        public Designator_ZoneAdd_GreenhouseRoof()
        {
            zoneTypeToPlace=typeof(Zone_Greenhouse);defaultLabel="TMM.Farming.ManualRoof".Translate();defaultDesc="TMM.Farming.ManualRoofHelp".Translate();
            hotKey=null;tutorTag=null;icon=ContentFinder<Texture2D>.Get("UI/Farming/GreenhouseZone");
        }
        protected override string NewZoneLabel {get{return "TMM.Farming.ManualRoof".Translate();}}
        protected override Zone MakeNewZone(){return new Zone_Greenhouse(Find.CurrentMap.zoneManager){manualLayout=true};}
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if(!c.InBounds(Map)||c.Fogged(Map)||Map.zoneManager.ZoneAt(c)!=null||c.GetEdifice(Map)!=null)return false;
            var roof=Map.roofGrid.RoofAt(c);
            return (roof==null||roof.defName=="TMM_GreenhouseGlassRoof")&&c.GetTerrain(Map).passability!=Traversability.Impassable;
        }
        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            Find.Selector.ClearSelection();base.DesignateMultiCell(cells);
        }
        protected override void FinalizeDesignationSucceeded()
        {
            base.FinalizeDesignationSucceeded();
            foreach(var zone in Map.zoneManager.AllZones.OfType<Zone_Greenhouse>().Where(z=>z.manualLayout&&!z.planned).ToArray())
                if(!zone.InitializeManual()){
                    if(Map.zoneManager.AllZones.Contains(zone))zone.Delete();
                    Messages.Message("TMM.Farming.ManualRoom".Translate(),MessageTypeDefOf.RejectInput,false);
                }
        }
    }
    public sealed partial class Zone_Greenhouse
    {
        public bool manualLayout;
        public bool InitializeManual()
        {
            if(planned)return true;if(CellCount==0)return false;
            var room=GrowingRoom;
            if(room==null||room.TouchesMapEdge||Cells.Any(c=>c.GetRoom(Map)!=room))return false;
            int x=Cells.Min(c=>c.x),z=Cells.Min(c=>c.z),w=Cells.Max(c=>c.x)-x+1,h=Cells.Max(c=>c.z)-z+1;
            if(w>64||h>64)return false;
            var map=Map;
            var existing=map.zoneManager.AllZones.OfType<Zone_Greenhouse>().FirstOrDefault(o=>o!=this&&o.planned&&o.GrowingRoom==room);
            if(existing!=null){
                var climate=existing.ClimateCell;var door=existing.DoorCell;
                foreach(var c in Cells.ToArray()){RemoveCell(c);existing.AddCell(c);GreenhouseRoofRules.MarkRoof(map,c);}
                existing.climatePosition=climate;existing.doorPosition=door;existing.manualLayout=true;existing.UpdateManualBounds();
                if(map.zoneManager.AllZones.Contains(this))map.zoneManager.DeregisterZone(this);
                existing.Synchronize();return true;
            }
            minX=x;minZ=z;width=w;height=h;planned=true;manualLayout=true;
            foreach(var c in Cells)GreenhouseRoofRules.MarkRoof(map,c);
            layoutDirty=true;Synchronize();return true;
        }
        private void UpdateManualBounds()
        {
            if(CellCount==0)return;minX=Cells.Min(c=>c.x);minZ=Cells.Min(c=>c.z);width=Cells.Max(c=>c.x)-minX+1;height=Cells.Max(c=>c.z)-minZ+1;layoutDirty=true;
        }
        private void FindManualController()
        {
            var room=GrowingRoom;if(room==null)return;
            var unit=Map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseClimateUnit")).OfType<Building_GreenhouseNode>()
                .FirstOrDefault(n=>n.Faction==Faction.OfPlayer&&(n.Owner==null||n.Owner==this)&&GenAdj.CardinalDirections.Any(d=>
                    (n.Position+d).InBounds(Map)&&(n.Position+d).GetRoom(Map)==room));
            if(unit!=null){powerNode=unit;climatePosition=unit.Position;unit.zoneId=ID;}
        }
    }
    public static class ManualGreenhouse
    {
        public static Zone_Greenhouse FromRoom(Building_GreenhouseNode panel)
        {
            if(panel==null||!panel.Spawned)return null;var map=panel.Map;
            var rooms=GenAdj.CardinalDirections.Select(d=>panel.Position+d).Where(c=>c.InBounds(map)).Select(c=>c.GetRoom(map))
                .Where(r=>r!=null&&!r.TouchesMapEdge).Distinct().OrderBy(r=>r.CellCount);
            foreach(var room in rooms){
                var existing=map.zoneManager.AllZones.OfType<Zone_Greenhouse>().FirstOrDefault(z=>z.planned&&z.GrowingRoom==room);
                if(existing!=null){existing.Synchronize();return existing;}
                var cells=room.Cells.Where(c=>c.GetEdifice(map)==null&&map.zoneManager.ZoneAt(c)==null&&c.GetTerrain(map).fertility>0).ToArray();
                if(cells.Length==0)continue;
                var zone=new Zone_Greenhouse(map.zoneManager){manualLayout=true};map.zoneManager.RegisterZone(zone);
                foreach(var c in cells)zone.AddCell(c);
                if(zone.InitializeManual())return panel.Owner??zone;
                zone.Delete();
            }
            return null;
        }
    }
}
