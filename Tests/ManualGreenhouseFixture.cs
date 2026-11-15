using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using Verse;

public static class ManualGreenhouseFixture
{
    private static int checks;
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static Building Spawn(Map map,string name,IntVec3 cell)
    {
        var b=(Building)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(name));b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);
    }
    public static int Setup(Map map,Zone_Greenhouse automatic,Pawn builder)
    {
        checks=0;
        Check(!automatic.TryExpand(new CellRect(86,40,2,6)),"Retired expansion accepted new work");
        Check(!automatic.GetZoneAddGizmos().Any(),"Built greenhouse still exposes automatic expansion");
        Check(automatic.Footprint.EdgeCells.All(c=>c.GetEdifice(map)!=null),"Retiring expansion damaged original walls");
        var invalid=new Zone_Greenhouse(map.zoneManager){manualLayout=true};map.zoneManager.RegisterZone(invalid);
        var outside=new IntVec3(87,0,54);invalid.AddCell(outside);
        Check(!invalid.InitializeManual(),"Manual roof accepted unbounded outdoor space");
        HarmonyLib.AccessTools.Method(typeof(Designator_ZoneAdd_GreenhouseRoof),"FinalizeDesignationSucceeded").Invoke(new Designator_ZoneAdd_GreenhouseRoof(),null);
        Check(map.zoneManager.ZoneAt(outside)==null&&!map.zoneManager.AllZones.Contains(invalid),"Rejected manual roof left ghost growing cells");
        // Manual room is deliberately separate from the automatic fixture.
        var rect=new CellRect(80,50,6,6);var panelCell=new IntVec3(83,0,55);
        foreach(var c in rect.EdgeCells)Spawn(map,c==panelCell?"TMM_GreenhouseClimateUnit":c==new IntVec3(83,0,50)?"TMM_GreenhouseGlassDoor":"TMM_GreenhouseGlassWall",c);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        var tool=new Designator_ZoneAdd_GreenhouseRoof();
        Check(tool.CanDesignateCell(new IntVec3(81,0,51)).Accepted,"Manual roof rejects empty room floor");
        var zone=new Zone_Greenhouse(map.zoneManager){manualLayout=true};map.zoneManager.RegisterZone(zone);
        foreach(var c in rect.ContractedBy(1))zone.AddCell(c);
        Check(zone.InitializeManual(),"Manual roof zone failed inside glass room");
        Check(zone.Power!=null&&zone.ClimateCell==panelCell&&zone.powerNode==panelCell.GetEdifice(map),"Manual zone did not bind its separately built climate panel");
        Check(zone.Cells.All(c=>map.areaManager.BuildRoof[c]&&!c.Roofed(map)),"Manual roof was built for free or not designated");
        Check(zone.Cells.Any(c=>new WorkGiver_BuildRoof().HasJobOnCell(builder,c,false)),"Native roof builder cannot build the manual glass roof");
        foreach(var c in zone.Cells)map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();zone.Synchronize();
        Check(zone.Enclosed&&zone.Cells.All(c=>map.roofGrid.RoofAt(c).defName=="TMM_GreenhouseGlassRoof"),"Manually designated roof is opaque or room is open");
        Check(!tool.CanDesignateCell(panelCell).Accepted,"Roof designation includes a wall cell");
        Check(Math.Abs(map.fertilityGrid.FertilityAt(zone.Cells[0])-2f)<0.001f,"Manual greenhouse lacks 200 percent fertile soil");
        // Repainting free floor merges cropping/roof data, never moves walls.
        var omitted=zone.Cells.Last();zone.RemoveCell(omitted);
        var patch=new Zone_Greenhouse(map.zoneManager){manualLayout=true};map.zoneManager.RegisterZone(patch);patch.AddCell(omitted);
        Check(patch.InitializeManual()&&map.zoneManager.ZoneAt(omitted)==zone,"Manual roof painting did not join its room zone");
        Check(zone.CellCount==16&&rect.EdgeCells.All(c=>c.GetEdifice(map)!=null),"Manual roof painting altered the perimeter");
        Spawn(map,"PowerConduit",new IntVec3(79,0,53));
        Spawn(map,"VanometricPowerCell",new IntVec3(78,0,53));
        for(int z=54;z<=56;z++)Spawn(map,"PowerConduit",new IntVec3(79,0,z));
        for(int x=80;x<=84;x++)Spawn(map,"PowerConduit",new IntVec3(x,0,56));
        Check(automatic.Cells.All(c=>c.Roofed(map)),"Automatic whole-greenhouse planner lost its roof");
        // Legacy plans are data migrations: cancel only owned unbuilt work,
        // clear obsolete demolition, retain paid parts and unrelated plans.
        var paid=Spawn(map,"TMM_GreenhouseGlassWall",new IntVec3(88,0,42));
        var wall=DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseGlassWall");
        var owned=new IntVec3(88,0,41);var unrelated=new IntVec3(89,0,41);
        GenConstruct.PlaceBlueprintForBuild(wall,owned,map,Rot4.North,Faction.OfPlayer,null);
        GenConstruct.PlaceBlueprintForBuild(wall,unrelated,map,Rot4.North,Faction.OfPlayer,null);
        var old=automatic.Footprint.EdgeCells.First(c=>c.GetEdifice(map).def==wall).GetEdifice(map);
        map.designationManager.AddDesignation(new Designation(old,DesignationDefOf.Deconstruct));
        automatic.expansion=new GreenhouseExpansion{committed=false,parts=new List<GreenhousePart>{new GreenhousePart{cell=owned,def=wall,created=true},new GreenhousePart{cell=paid.Position,def=wall,created=true}},obsolete=new List<Building>{old}};
        automatic.ExpansionTick();
        Check(automatic.expansion==null&&!owned.GetThingList(map).Any(t=>t is Blueprint_Build),"Legacy uncommitted blueprint was not cancelled");
        Check(paid.Spawned&&old.Spawned&&map.designationManager.DesignationOn(old,DesignationDefOf.Deconstruct)==null,"Legacy migration removed paid walls or retained demolition");
        Check(unrelated.GetThingList(map).Any(t=>t is Blueprint_Build),"Migration cancelled unrelated building plans");
        var frameCell=new IntVec3(88,0,43);var frame=(Frame)ThingMaker.MakeThing(wall.frameDef);
        frame.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(frame,frameCell,map);
        var steel=ThingMaker.MakeThing(ThingDefOf.Steel);steel.stackCount=3;frame.resourceContainer.TryAdd(steel);
        int beforeSteel=map.listerThings.ThingsOfDef(ThingDefOf.Steel).Sum(t=>t.stackCount);
        automatic.expansion=new GreenhouseExpansion{committed=true,parts=new List<GreenhousePart>{new GreenhousePart{cell=frameCell,def=wall,created=true}}};
        automatic.ExpansionTick();
        Check(frame.Destroyed&&map.listerThings.ThingsOfDef(ThingDefOf.Steel).Sum(t=>t.stackCount)==beforeSteel+3,"Committed legacy frame cancellation lost its supplied steel");
        // A committed legacy record survives in the save until normal sync.
        GenConstruct.PlaceBlueprintForBuild(wall,owned,map,Rot4.North,Faction.OfPlayer,null);
        map.designationManager.AddDesignation(new Designation(old,DesignationDefOf.Deconstruct));
        automatic.expansion=new GreenhouseExpansion{committed=true,parts=new List<GreenhousePart>{new GreenhousePart{cell=owned,def=wall,created=true}},obsolete=new List<Building>{old}};
        return checks;
    }
    public static int Running(Map map)
    {
        checks=0;var zone=map.zoneManager.AllZones.OfType<Zone_Greenhouse>().Single(z=>z.manualLayout);zone.Synchronize();
        Check(zone.Powered&&zone.Power.PowerNet!=null,"Manual climate failed to connect via a conduit beside its wall");
        zone.targetTemperature=21f;zone.GrowingRoom.Temperature=35f;zone.SyncPowerAndLights();float cooling=zone.Demand;
        zone.ApplyHeat();Check(Math.Abs(zone.GrowingRoom.Temperature-21f)<0.01f&&cooling>0f,"Manual climate failed to cool its actual room");
        zone.GrowingRoom.Temperature=-25f;zone.ApplyHeat();
        Check(Math.Abs(zone.GrowingRoom.Temperature-21f)<0.01f,"Manual climate failed to heat its actual room");
        return checks;
    }
    public static int Reload(Map map,Zone_Greenhouse automatic)
    {
        checks=0;automatic.Synchronize();
        Check(automatic.expansion==null,"Saved legacy expansion remains active after migration");
        Check(automatic.Footprint.EdgeCells.All(c=>c.GetEdifice(map)!=null),"Loading old expansion removed original walls");
        Check(automatic.Footprint.EdgeCells.Select(c=>c.GetEdifice(map)).All(b=>map.designationManager.DesignationOn(b,DesignationDefOf.Deconstruct)==null),"Saved legacy demolition was not stopped");
        Check(!new IntVec3(88,0,41).GetThingList(map).Any(t=>t is Blueprint_Build),"Saved owned blueprint was not cancelled");
        Check(new IntVec3(88,0,42).GetEdifice(map)!=null&&new IntVec3(89,0,41).GetThingList(map).Any(t=>t is Blueprint_Build),"Migration lost paid or unrelated construction");
        var zone=map.zoneManager.AllZones.OfType<Zone_Greenhouse>().Single(z=>z.manualLayout);
        zone.Synchronize();
        Check(zone.CellCount==16&&zone.Enclosed&&zone.powerNode!=null&&zone.ClimateCell==new IntVec3(83,0,55),"Manual roof/controller ownership lost on save/load");
        Check(zone.Cells.All(c=>map.roofGrid.RoofAt(c).defName=="TMM_GreenhouseGlassRoof"),"Manual glass roof changed after loading");
        Check(!zone.GetZoneAddGizmos().Any(),"Manual zone gained an automatic expansion button");
        var panel=(Building_GreenhouseNode)zone.powerNode;
        zone.Delete();
        for(int z=51;z<=54;z++)Spawn(map,"TMM_GreenhouseGlassWall",new IntVec3(82,0,z));
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        var restored=panel.RestoreZone();
        Check(restored!=null&&restored.manualLayout&&restored.CellCount==8,"Manual panel did not restore its current room");
        Check(restored.Cells.All(c=>c.x>=83)&&map.zoneManager.ZoneAt(new IntVec3(81,0,51))==null,"Manual zone restoration leaked across a newly built partition");
        return checks;
    }
}
