using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class LayoutReloadLaunch
{
    static LayoutReloadLaunch() { if (Environment.GetCommandLineArgs().Contains("-tmm-layout-reload")) LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Layout")); }
}
public sealed class ComplexLayoutProbe : MapComponent
{
    private int checks, stage, tick;
    private bool reported, uiReported, saved;
    private Building_ComplexDesk desk;
    private Pawn actor, guest;
    private Building_Bed bed, staffBed, wardBed;
    private Building_ServiceCounter counter;
    private Thing outsideMeal;
    private Zone_Greenhouse greenhouse;
    private readonly List<string> errors = new List<string>();
    public ComplexLayoutProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_Values.Look(ref saved, "layoutSaved"); Scribe_References.Look(ref desk, "layoutDesk");
        Scribe_References.Look(ref actor, "layoutActor"); Scribe_References.Look(ref guest, "layoutGuest");
        Scribe_References.Look(ref bed, "layoutBed"); Scribe_References.Look(ref staffBed, "layoutStaffBed"); Scribe_References.Look(ref wardBed, "layoutWardBed");
        Scribe_References.Look(ref greenhouse, "layoutGreenhouse");
    }
    private void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; }
    private Thing Spawn(string name, IntVec3 cell)
    {
        var def = DefDatabase<ThingDef>.GetNamed(name); var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
        thing.SetFaction(Faction.OfPlayer); return GenSpawn.Spawn(thing, cell, map);
    }
    private void RoomAt(int x, int z)
    {
        var rect = new CellRect(x, z, 8, 8);
        foreach (var c in rect.EdgeCells) Spawn(c == new IntVec3(x + 3, 0, z) ? "Door" : "Wall", c);
        foreach (var c in rect.ContractedBy(1)) map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
    }
    private Thing ChairAtTable(int x, int z)
    {
        var table = Spawn("Table2x2c", new IntVec3(x, 0, z));
        var c = GenAdj.CellsAdjacentCardinal(table).First(p => p.Standable(map) && p.GetEdifice(map) == null);
        return Spawn("DiningChair", c);
    }
    private void Wait(Pawn pawn) { var job = JobMaker.MakeJob(JobDefOf.Wait); job.expiryInterval = 600000; pawn.jobs.StartJob(job, JobCondition.InterruptForced); }
    private void Setup()
    {
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned) Wait(pawn);
        foreach (var c in new CellRect(38, 38, 53, 30)) {
            foreach (var t in c.GetThingList(map).ToArray()) if (t is Building || t is Plant) t.Destroy();
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil); map.roofGrid.SetRoof(c, null); map.areaManager.Home[c] = true;
        }
        RoomAt(40,40); RoomAt(50,40); RoomAt(40,50); RoomAt(50,50);
        Area_Allowed common, staff; map.areaManager.TryMakeNewAllowed(out common); map.areaManager.TryMakeNewAllowed(out staff);
        foreach (var c in new CellRect(40,40,30,20)) common[c] = true;
        foreach (var c in new CellRect(50,50,8,8)) staff[c] = true;
        desk = (Building_ComplexDesk)Spawn("TMM_ComplexDesk", new IntVec3(62,0,48)); desk.area = common; desk.staffArea = staff;
        var old = (Building_Bed)Spawn("Bed", new IntVec3(43,0,43)); old.HitPoints -= 10;
        Spawn("Bed",new IntVec3(53,0,43)); Spawn("Bed",new IntVec3(43,0,53)); Spawn("Bed",new IntVec3(45,0,53));
        staffBed = (Building_Bed)Spawn("Bed",new IntVec3(53,0,53));
        var colony = (Building_Bed)Spawn("Bed",new IntVec3(80,0,53)); colony.Medical = true;
        var privateSeat = ChairAtTable(45,45); var otherSeat = ChairAtTable(55,45); var publicSeat = ChairAtTable(65,45);
        counter = (Building_ServiceCounter)Spawn("TMM_ServiceCounter",new IntVec3(62,0,45));
        Spawn("sbz_SingleChestFridge",new IntVec3(62,0,42)); Spawn("sbz_SingleChestFridge",new IntVec3(75,0,42));
        var insideMeal = ThingMaker.MakeThing(ThingDefOf.MealSimple); insideMeal.stackCount = 2; GenSpawn.Spawn(insideMeal,new IntVec3(62,0,42),map);
        outsideMeal = ThingMaker.MakeThing(ThingDefOf.MealSimple); outsideMeal.stackCount = 2; GenSpawn.Spawn(outsideMeal,new IntVec3(75,0,42),map);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        var layout = map.GetComponent<ComplexLayout>(); layout.Synchronize();
        bed = old.Position.GetThingList(map).OfType<Building_Bed>().Single();
        Check(ComplexLayout.IsGuest(bed) && bed.HitPoints == bed.MaxHitPoints-10,"Automatic conversion lost guest status or damage");
        Check(!ComplexLayout.IsGuest(staffBed) && !staffBed.Destroyed,"Staff-only bedroom became guest housing");
        Check(layout.guestAccess[bed.Position] && !layout.guestAccess[staffBed.Position],"Guest access includes staff-only rooms");
        int id = bed.thingIDNumber; layout.Synchronize(); Check(id == bed.thingIDNumber && !bed.Destroyed,"Stable room was repeatedly converted");
        Check(HotelBillingHooks.Fee(bed)==30,"Central room rate not applied"); desk.roomRate=45; layout.Synchronize(); Check(HotelBillingHooks.Fee(bed)==45,"Central room rate did not update");
        wardBed = new IntVec3(43,0,53).GetThingList(map).OfType<Building_Bed>().Single();
        wardBed.Medical = true;
        wardBed = new IntVec3(43,0,53).GetThingList(map).OfType<Building_Bed>().Single();
        Check(wardBed.GetRoom().ContainedBeds.All(b=>b.Medical && !ComplexLayout.IsGuest(b)),"Medical checkbox did not convert whole room");
        Check(Clinic.WardBed(wardBed) && !Clinic.WardBed(colony) && desk.clinicArea==null,"Automatic clinic stole colony bed or still requires clinic area");
        var gizmos=bed.GetGizmos().ToArray();
        Check(gizmos.OfType<Command_Toggle>().Count(c=>c.defaultLabel=="CommandBedSetAsMedicalLabel".Translate().ToString())==1,"Bed lacks a single working medical checkbox");
        Check(!gizmos.Any(g=>g.GetType().FullName=="Hospitality.Gizmo_GuestBed"),"Per-bed guest pricing still visible");
        Check(desk.GetGizmos().OfType<Command>().Count(c=>c.defaultLabel.StartsWith("TMM.Complex.FoodArea".Translate().ToString()))==0,"Reception still exposes stock area");
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
        guest=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,faction);GenSpawn.Spawn(guest,new IntVec3(44,0,43),map);
        guest.inventory.innerContainer.ClearAndDestroyContents(); var silver=ThingMaker.MakeThing(ThingDefOf.Silver);silver.stackCount=500;guest.inventory.innerContainer.TryAdd(silver);
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(62,0,44),new List<Pawn>{guest},map,false,false,600000});
        guest.GetLord().ReceiveMemo("TravelArrived");
        AccessTools.Method(bed.GetType(),"TryClaimBed").Invoke(bed,new object[]{guest});AccessTools.Field(HotelBillingHooks.Guest(guest).GetType(),"bed").SetValue(HotelBillingHooks.Guest(guest),bed);
        guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=1;Wait(guest);
        var access=(Area)AccessTools.Property(HotelBillingHooks.Guest(guest).GetType(),"GuestArea").GetValue(HotelBillingHooks.Guest(guest),null);
        Check(access!=null && !access[staffBed.Position] && access[publicSeat.Position],"Actual guest component ignores staff exclusion");
        Check(RestaurantMeals.SeatForGuest(guest,privateSeat,counter),"Owner cannot use table in own room");
        Check(!RestaurantMeals.SeatForGuest(guest,otherSeat,counter),"Guest can use another guest's bedroom table");
        Check(RestaurantMeals.SeatForGuest(guest,publicSeat,counter),"Public dining table excluded");
        Check(counter.Ready && counter.foodArea==null && RestaurantMeals.FridgeAt(map,insideMeal.Position),"Restaurant still needs manual areas or cannot find real fridge");
        var order=map.GetComponent<RestaurantService>().TryOrder(guest,counter);
        Check(order!=null && order.food==insideMeal,"Restaurant did not prefer stocked complex fridge");
        order.cancelled=true;map.GetComponent<RestaurantService>().ProcessOrders(false);guest.jobs.StopAll();Wait(guest);insideMeal.Destroy();
        order=map.GetComponent<RestaurantService>().TryOrder(guest,counter);
        Check(order!=null && order.food==outsideMeal,"Empty complex fridge did not fall back to colony fridge");
        order.cancelled=true;map.GetComponent<RestaurantService>().ProcessOrders(false);guest.jobs.StopAll();Wait(guest);
        var work=DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService");
        actor=map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p=>p.workSettings!=null&&!p.WorkTypeIsDisabled(work));
        for(int attempt=0;actor==null&&attempt<32;attempt++){
            var candidate=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);
            if(!candidate.WorkTypeIsDisabled(work)){GenSpawn.Spawn(candidate,new IntVec3(52,0,52),map);actor=candidate;Wait(actor);}else candidate.Destroy();
        }
        Check(actor!=null,"Could not generate waiter fixture");actor.Position=new IntVec3(52,0,52);
        Check(ForbidUtility.InAllowedArea(bed.Position,actor),"Staff-only area incorrectly confines colonist movement");
        staffBed.Medical=true;
        Check(desk.SetResidence(actor,true),"Staff could not claim an empty auto-guest room");
        var housing=actor.ownership.OwnedBed;layout.Synchronize();
        Check(housing!=null&&!ComplexLayout.IsGuest(housing)&&actor.ownership.OwnedBed==housing,"Automatic layout reclaimed assigned staff housing");
        desk.SetResidence(actor,false);staffBed.Medical=false;layout.Synchronize();
        Check(ComplexLayout.IsGuest(housing.Position.GetThingList(map).OfType<Building_Bed>().Single()),"Released staff room did not return to guest service");
        // A real paid-construction expansion, saved before new work is complete.
        greenhouse=new Zone_Greenhouse(map.zoneManager);map.zoneManager.RegisterZone(greenhouse);
        foreach(var c in new CellRect(80,40,6,6))greenhouse.AddCell(c);
        Check(greenhouse.CreatePlan(),"Greenhouse fixture plan failed");
        CompleteGreenhouseBlueprints();foreach(var c in greenhouse.Cells)map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        Check(greenhouse.TryExpand(new CellRect(86,40,2,6)),"Adjacent greenhouse expansion refused");
        Check(greenhouse.expansion!=null && !greenhouse.expansion.committed && greenhouse.width==6,"Expansion resized before construction");
        Check(greenhouse.Footprint.EdgeCells.All(c=>c.GetEdifice(map)!=null),"Expansion immediately tore down old perimeter");
        Check(map.listerThings.AllThings.OfType<Blueprint_Build>().Any(b=>b.Position.x>=86),"Expansion created free built walls");
        saved=true;GameDataSaveLoader.SaveGame("TMM-Layout");
    }
    private void CompleteGreenhouseBlueprints()
    {
        foreach(var b in map.listerThings.AllThings.OfType<Blueprint_Build>().Where(b=>b.def.entityDefToBuild.defName.StartsWith("TMM_Greenhouse")).ToArray()){
            var pos=b.Position;var def=b.def.entityDefToBuild.defName;b.Destroy();Spawn(def,pos);
        }
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-layout-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-layout-probe")))return;
        try{
            if(!reload){Setup();Finish(false);return;}
            if(stage==1){
                if((desk.restaurantIncome==0||guest.needs.food.CurLevelPercentage<=0.35f)&&Find.TickManager.TicksGame-tick<3500)return;
                Check(desk.restaurantIncome>0&&counter.earned==desk.restaurantIncome,"Complex-only waiter failed real delivery from colony fridge: "+actor.CurJob+" / "+guest.CurJob);
                Check(guest.needs.food.CurLevelPercentage>0.35f,"Guest did not eat the delivered fallback meal");
                var credit=Current.Game.GetComponent<WorkBuyouts>().Entry(actor);
                Check(credit!=null&&credit.credited==desk.restaurantIncome/2&&credit.halfRemainder==desk.restaurantIncome%2,"Real restaurant receipt did not credit its waiter at 50 percent");
                Finish(true);return;
            }
            Check(saved&&desk!=null&&bed!=null&&staffBed!=null&&wardBed!=null,"Saved room references missing");
            Check(desk.staffArea!=null&&desk.automaticServices&&desk.roomRate==45,"Saved automatic settings changed");
            var layout=map.GetComponent<ComplexLayout>();layout.Synchronize();
            Check(HotelBillingHooks.Bed(guest)==bed&&!layout.guestAccess[staffBed.Position]&&Clinic.WardBed(wardBed),"Saved ownership/access/ward changed");
            Check(greenhouse!=null&&greenhouse.expansion!=null&&!greenhouse.expansion.committed,"Pending expansion lost on load");
            CompleteGreenhouseBlueprints();map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();greenhouse.ExpansionTick();
            Check(greenhouse.expansion!=null&&!greenhouse.expansion.committed&&greenhouse.width==6,"Expansion dismantled the old greenhouse before the new roof was ready");
            var newRoof=greenhouse.expansion.Rect.ContractedBy(1).Where(c=>!greenhouse.Footprint.Contains(c)&&c.GetEdifice(map)==null).ToArray();
            Check(newRoof.Length>0&&newRoof.All(c=>actor.CanReach(c,PathEndMode.OnCell,Danger.None)),"Builders cannot enter the enclosed extension to finish its roof");
            var oldPartition=greenhouse.Footprint.EdgeCells.First(c=>!greenhouse.expansion.parts.Any(p=>p.cell==c)&&greenhouse.expansion.Rect.ContractedBy(1).Contains(c));
            Check(new WorkGiver_BuildRoof().HasJobOnCell(actor,oldPartition,false),"Native roofer cannot roof an old partition before deconstruction");
            foreach(var c in greenhouse.expansion.Rect.ContractedBy(1).Where(c=>!greenhouse.expansion.parts.Any(p=>p.cell==c)))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
            Check(newRoof.All(c=>map.roofGrid.RoofAt(c).defName=="TMM_GreenhouseGlassRoof"),"Pending expansion got an opaque roof");
            greenhouse.ExpansionTick();
            Check(greenhouse.width==8&&greenhouse.expansion!=null&&greenhouse.expansion.committed,"Built expansion did not commit");
            var obsolete=greenhouse.expansion.obsolete.ToArray();
            Check(obsolete.Length>0&&obsolete.All(b=>b.Spawned&&map.designationManager.DesignationOn(b,DesignationDefOf.Deconstruct)!=null),"Old walls not scheduled for normal deconstruction");
            foreach(var b in obsolete)b.Destroy(DestroyMode.Deconstruct);
            greenhouse.ExpansionTick();foreach(var c in greenhouse.Cells)map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            Check(greenhouse.expansion==null&&greenhouse.CellCount==24&&greenhouse.Enclosed,"Expanded greenhouse area/enclosure wrong");
            Check(greenhouse.ClimateCell.GetEdifice(map)!=null&&greenhouse.ClimateCell.GetEdifice(map).def.defName=="TMM_GreenhouseClimateUnit","Expanded greenhouse lost its controller");
            counter=map.listerBuildings.allBuildingsColonist.OfType<Building_ServiceCounter>().Single();
            actor.pather.StopDead();actor.Position=new IntVec3(62,0,44);
            actor.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);
            Check(Current.Game.GetComponent<WorkBuyouts>().Begin(actor,10000)!=null,"Could not start waiter buyout accounting");
            foreach(var work in DefDatabase<WorkTypeDef>.AllDefsListForReading)actor.workSettings.SetPriority(work,0);
            Check(desk.SetRole(actor,ComplexRole.Waiter),"Waiter role rejected after load");desk.Employee(actor).mode=ComplexWorkMode.ComplexOnly;
            actor.needs.food.CurLevelPercentage=1;actor.needs.rest.CurLevelPercentage=1;if(actor.needs.joy!=null)actor.needs.joy.CurLevelPercentage=1;
            for(int h=0;h<24;h++)actor.timetable.SetAssignment(h,TimeAssignmentDefOf.Work);
            guest.needs.food.CurLevelPercentage=0.35f;Wait(guest);
            var order=map.GetComponent<RestaurantService>().TryOrder(guest,counter);Check(order!=null,"No fallback restaurant order after load");
            // The guest first walks to their chair; let ordinary work AI retry
            // after that arrival rather than injecting a delivery job.
            actor.jobs.StopAll();tick=Find.TickManager.TicksGame;stage=1;
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    private void Finish(bool reload)
    {
        reported=true; string error=errors.Count==0?"":errors[0].Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n");
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"ComplexLayoutReloadProbe.json":"ComplexLayoutProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+(errors.Count==0?"":"\""+error+"\"")+"]}");
    }
    public override void MapComponentOnGUI()
    {
        if(uiReported||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-layout-probe"))return;
        uiReported=true;string error=null;GUI.BeginGroup(new Rect(0,0,1000,750));
        try{new Dialog_ComplexServices(desk).DoWindowContents(new Rect(0,0,680,650));foreach(var gizmo in bed.GetGizmos())if(gizmo is Command_Toggle)gizmo.GizmoOnGUI(new Vector2(720,40),120,default(GizmoRenderParms));}
        catch(Exception ex){error=ex.ToString();}finally{GUI.EndGroup();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"ComplexLayoutUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":"\""+error.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"")+"}");
    }
}
