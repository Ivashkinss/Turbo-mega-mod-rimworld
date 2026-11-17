using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;

public sealed class ServiceReadinessProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building_Bed ordinary,bed,ward;
    private Pawn first,second,patient,worker;
    private object hospitality,hospital;
    private Area_Allowed area,staff;
    private IEnumerator<int> flow;
    private int checks,lastTick=-1,started,renderFrame;
    private bool done,ui;
    private string error;
    private Dialog_ServiceReadiness window;
    public ServiceReadinessProbe(Map map):base(map){}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private static void Wait(Pawn pawn){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;pawn.jobs.StartJob(j,JobCondition.InterruptForced);}
    private static void Set(object o,string key,object value){AccessTools.Field(o.GetType(),key).SetValue(o,value);}
    private static object Get(object o,string key){return AccessTools.Field(o.GetType(),key).GetValue(o);}
    private Building Build(string name,IntVec3 cell){var def=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(def,def.MadeFromStuff?ThingDefOf.Steel:null);b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);}
    private void Room(CellRect rect){foreach(var c in rect.EdgeCells)Build(c==new IntVec3(rect.CenterCell.x,0,rect.minZ)?"Door":"Wall",c);foreach(var c in rect.ContractedBy(1))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);}
    private Pawn Person(Faction faction,IntVec3 cell){var p=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false));GenSpawn.Spawn(p,cell,map);p.needs.food.CurLevelPercentage=1;p.needs.rest.CurLevelPercentage=1;Wait(p);return p;}
    private string State(){
        return desk.roomRate+"|"+desk.restaurantPercent+"|"+desk.restaurantOpen+"|"+desk.surgeryAllowed+"|"+desk.automaticServices+"|"+desk.area.ID+"|"+desk.staffArea.ID+"|"+
            string.Join(",",desk.employees.Select(e=>e==null?"null":e.pawn+":"+e.mode+":"+e.company+":"+e.resident))+"|"+
            Get(hospitality,"guestsAreWelcome")+"|"+Get(hospitality,"refuseGuestsUntilWeHaveBeds")+"|"+Get(hospital,"openForBusiness")+"|"+string.Join(",",(List<bool>)Get(hospital,"openingHours"))+"|"+
            string.Join(",",((IEnumerable)Get(Get(hospitality,"incidentQueue"),"queuedIncidents")).Cast<QueuedIncident>().Select(x=>x.FireTick+":"+x.GetHashCode()))+"|"+
            string.Join(",",map.mapPawns.AllPawnsSpawned.Select(p=>p.thingIDNumber+":"+p.CurJob+":"+HotelBillingHooks.Money(p)+":"+(p.Faction!=Faction.OfPlayer||p.workSettings==null?"none":string.Join(";",DefDatabase<WorkTypeDef>.AllDefsListForReading.Select(w=>p.workSettings.GetPriority(w).ToString())))))+"|"+
            map.listerThings.ThingsOfDef(ThingDefOf.Silver).Sum(t=>t.stackCount)+"|"+desk.hotelIncome+"|"+desk.restaurantIncome+"|"+desk.clinicIncome+"|"+desk.companyIncome;
    }
    private IEnumerable<int> Run(){
        Check(ServiceReadinessData.Gather(null)!=null&&!ServiceReadinessData.Gather(null).deskActive,"Null desk was unsafe");
        map.fogGrid.ClearAllFog();foreach(var p in map.mapPawns.FreeColonistsSpawned){Wait(p);foreach(var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)p.workSettings.SetPriority(w,0);}
        foreach(var c in new CellRect(35,35,40,28)){foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant||t is Filth){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);}
        map.areaManager.TryMakeNewAllowed(out area);map.areaManager.TryMakeNewAllowed(out staff);
        foreach(var c in new CellRect(36,36,35,24))area[c]=true;
        area.SetLabel("Гостиничный комплекс с длинным названием территории для проверки переноса текста");staff.SetLabel("Кухня, склад и служебные комнаты сотрудников комплекса");
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(38,0,38));desk.area=area;desk.staffArea=staff;desk.automaticServices=false;desk.clinicArea=area;desk.vipChance=0;
        hospitality=map.components.First(c=>c.GetType().FullName=="Hospitality.Hospitality_MapComponent");hospital=Clinic.Hospital(map);
        ordinary=(Building_Bed)Build("Bed",new IntVec3(43,0,43));var d=ServiceReadinessData.Gather(desk);
        Check(d.deskActive&&d.hotelModuleEnabled&&d.clinicModuleEnabled,"Active desk or service modules missing");
        Check(d.guestBedsTotal==0&&d.guestSlotsTotal==0,"Ordinary colonist bed counted as guest bed with auto services off");
        desk.automaticServices=true;d=ServiceReadinessData.Gather(desk);Check(d.guestBedsTotal==0,"Unconverted ordinary bed counted before auto conversion");desk.automaticServices=false;
        bed=(Building_Bed)Build("DoubleBedGuest",new IntVec3(47,0,43));d=ServiceReadinessData.Gather(desk);
        Check(d.guestBedsTotal==1&&d.guestSlotsTotal==2&&d.guestBedsFullyFree==1&&d.guestSlotsAvailable==2,"Actual empty double guest bed miscounted");
        staff[bed.Position]=true;Check(ServiceReadinessData.Gather(desk).guestBedsTotal==0,"Staff-room guest bed leaked into guest pool");staff[bed.Position]=false;
        var home=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        first=Person(home,new IntVec3(47,0,46));second=Person(home,new IntVec3(48,0,46));first.relations.AddDirectRelation(PawnRelationDefOf.Spouse,second);
        first.ownership.ClaimBedIfNonMedical(bed);second.ownership.ClaimBedIfNonMedical(bed);
        Check(bed.OwnersForReading.Count()==2,"Real double-bed ownership fixture did not assign both owners");
        first.needs.rest.CurLevelPercentage=0.2f;first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown,bed),JobCondition.InterruptForced);started=Find.TickManager.TicksGame;
        while(first.CurrentBed()!=bed){if(Find.TickManager.TicksGame-started>2000)throw new Exception("Actual sleeper never reached double bed: "+first.CurJob);yield return 0;}
        Check(bed.CurOccupants.Count()==1&&bed.OwnersForReading.Count()==2,"Expected one real sleeper and two owners");
        d=ServiceReadinessData.Gather(desk);Check(d.guestSlotsOccupied==2&&d.guestSlotsAvailable==0&&d.guestBedsFullyFree==0,"Sleeping owner hid awake spouse or was double-counted");
        second.ownership.UnclaimBed();d=ServiceReadinessData.Gather(desk);Check(d.guestSlotsOccupied==1&&d.guestSlotsAvailable==1&&d.guestBedsFullyFree==0,"Partly occupied double bed advertised as completely free");
        Room(new CellRect(54,40,8,8));ward=(Building_Bed)Build("Bed",new IntVec3(57,0,43));ward.Medical=true;map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        Check(Clinic.WardBed(ward),"Native clinical bed fixture unavailable");patient=map.mapPawns.FreeColonistsSpawned.First();patient.pather.StopDead();patient.Position=new IntVec3(57,0,45);patient.Notify_Teleported();patient.ownership.UnclaimBed();var illness=HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Flu"),patient);illness.Severity=0.4f;patient.health.AddHediff(illness);patient.needs.food.CurLevelPercentage=1;patient.needs.rest.CurLevelPercentage=0.05f;var restJob=JobMaker.MakeJob(JobDefOf.LayDown,ward);restJob.playerForced=true;restJob.forceSleep=true;restJob.restUntilHealed=true;patient.jobs.StartJob(restJob,JobCondition.InterruptForced);started=Find.TickManager.TicksGame;
        while(patient.CurrentBed()!=ward){if(Find.TickManager.TicksGame-started>2000)throw new Exception("Actual colonist never lay in clinical bed: "+patient.CurJob+" position="+patient.Position+" bed="+ward+" spawned="+ward.Spawned+" medical="+ward.Medical);yield return 0;}
        d=ServiceReadinessData.Gather(desk);Check(d.clinicBedsTotal==1&&d.clinicSlotsOccupied==1&&d.clinicSlotsAvailable==0,"Player patient falsely left clinic bed free");
        Set(hospitality,"guestsAreWelcome",false);Set(hospitality,"refuseGuestsUntilWeHaveBeds",true);d=ServiceReadinessData.Gather(desk);Check(!d.guestsWelcome&&d.refuseUntilBeds,"Admission switches not reflected");
        for(int h=0;h<24;h++)ComplexReceptionUI.SetHour(hospital,h,true);Set(hospital,"openForBusiness",false);d=ServiceReadinessData.Gather(desk);
        Check(d.clinicOpenHoursCount==24&&d.clinicScheduleHourAllowed&&!d.clinicAdmissionEnabled&&!d.clinicIsOpen,"Open schedule bypassed disabled patient admission");
        Set(hospital,"openForBusiness",true);d=ServiceReadinessData.Gather(desk);Check(d.clinicAdmissionEnabled&&d.clinicIsOpen,"Actual open hospital not reflected");
        int now=GenLocalDate.HourOfDay(map);ComplexReceptionUI.SetHour(hospital,now,false);d=ServiceReadinessData.Gather(desk);Check(d.clinicOpenHoursCount==23&&!d.clinicScheduleHourAllowed&&!d.clinicIsOpen,"Closed current hour ignored");ComplexReceptionUI.SetHour(hospital,now,true);
        desk.area=null;d=ServiceReadinessData.Gather(desk);Check(!d.deskActive&&d.clinicScheduleHourAllowed&&!d.clinicIsOpen,"Native patched hospital ignored missing complex territory");desk.area=area;
        Set(hospital,"AcceptSurgery",false);Check(!ServiceReadinessData.Gather(desk).surgeryAllowed,"Hospital surgery master switch ignored");Set(hospital,"AcceptSurgery",true);
        for(int i=0;i<20;i++){worker=Person(Faction.OfPlayer,new IntVec3(45,0,50));if(!worker.WorkTypeIsDisabled(ComplexSales.SalesWork)&&!worker.WorkTypeIsDisabled(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService"))&&!worker.WorkTypeIsDisabled(CompanyRules.Work))break;worker.DeSpawn();Find.WorldPawns.PassToWorld(worker,PawnDiscardDecideMode.Discard);worker=null;}
        Check(worker!=null,"No worker suitable for native eligibility fixture");worker.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);Check(worker.IsSlaveOfColony,"Worker did not become a native colony slave");
        foreach(var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)worker.workSettings.SetPriority(w,0);worker.workSettings.SetPriority(ComplexSales.SalesWork,1);worker.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("TMM_RestaurantService"),1);worker.workSettings.SetPriority(CompanyRules.Work,1);
        var employee=desk.Employee(worker,true);employee.mode=ComplexWorkMode.ComplexOnly;employee.company=false;d=ServiceReadinessData.Gather(desk);
        Check(d.sellersReady==1&&d.waitersReady==1&&d.hostsReady==0,"Slave staff excluded or company opt-in ignored");employee.company=true;Check(ServiceReadinessData.Gather(desk).hostsReady==1,"Opted-in company employee missing");
        employee.mode=ComplexWorkMode.ColonyOnly;d=ServiceReadinessData.Gather(desk);Check(d.sellersReady==0&&d.waitersReady==0&&d.hostsReady==0,"Colony-only worker counted for complex services");employee.mode=ComplexWorkMode.ComplexOnly;
        worker.drafter.Drafted=true;d=ServiceReadinessData.Gather(desk);Check(d.sellersReady==0&&d.waitersReady==0,"Drafted worker counted");worker.drafter.Drafted=false;Wait(worker);
        desk.automaticServices=true;
        var counter=(Building_ServiceCounter)Build("TMM_ServiceCounter",new IntVec3(42,0,50));var fridge=Build("sbz_SingleChestFridge",new IntVec3(43,0,50));var meal=ThingMaker.MakeThing(ThingDefOf.MealSimple);meal.stackCount=5;GenSpawn.Spawn(meal,fridge.Position,map);desk.restaurantMenu=new List<ThingDef>{ThingDefOf.MealSimple};
        Check(RestaurantMeals.Stock(counter,meal),"Physical meal fixture not in stock");d=ServiceReadinessData.Gather(desk);Check(d.counterCount==1&&d.menuCount==1&&d.mealsInStockTotal==5,"Physical portions/counters/menu count wrong");
        desk.restaurantMenu.Clear();Check(ServiceReadinessData.Gather(desk).mealsInStockTotal==0,"Stock from outside configured menu counted");desk.restaurantMenu.Add(ThingDefOf.MealSimple);
        var shelf=Build("Shelf",new IntVec3(49,0,50));var steel=ThingMaker.MakeThing(ThingDefOf.Steel);steel.stackCount=75;GenSpawn.Spawn(steel,shelf.Position,map);shelf.GetComp<CompComplexSale>().forSale=true;d=ServiceReadinessData.Gather(desk);
        Check(d.storesTotal==1&&d.storesPublic==1&&d.tradeableStacksTotal==1&&d.tradeableUnitsTotal==75,"Sale stacks confused with item units");staff[shelf.Position]=true;d=ServiceReadinessData.Gather(desk);Check(d.storesStaffOnly==1&&d.storesPublic==0,"Private stock counted as public");
        shelf.GetComp<CompComplexSale>().forSale=false;Check(ServiceReadinessData.Gather(desk).storesTotal==0,"Disabled sales flag ignored");shelf.GetComp<CompComplexSale>().forSale=true;
        desk.employees.Add(null);d=ServiceReadinessData.Gather(desk);Check(d.totalEmployees==1&&d.sellersReady==1&&d.waitersReady==1,"Invalid employee entries broke read-only summary");desk.employees.Remove(null);
        string before=State();for(int i=0;i<3;i++)ServiceReadinessData.Gather(desk);Check(State()==before,"Gather mutated jobs, workers, prices, admission, queue or money");
        ordinary.DeSpawn();Check(ServiceReadinessData.Gather(desk).guestBedsTotal==1,"Despawned ordinary bed changed guest count");
        var oldArea=desk.area;oldArea.Delete();d=ServiceReadinessData.Gather(desk);Check(!d.deskActive&&!d.clinicIsOpen,"Deleted area left complex active");
        map.areaManager.TryMakeNewAllowed(out area);foreach(var c in new CellRect(36,36,35,24))area[c]=true;area.SetLabel("Гостиничный комплекс с длинным названием территории для проверки переноса текста");desk.area=area;desk.clinicArea=area;
        desk.DeSpawn();Check(!ServiceReadinessData.Gather(desk).deskActive,"Despawned desk unsafe");GenSpawn.Spawn(desk,new IntVec3(38,0,38),map);
        Set(hospitality,"guestsAreWelcome",false);Set(hospital,"openForBusiness",false);desk.restaurantOpen=false;desk.restaurantMenu.Clear();
    }
    public override void MapComponentUpdate(){if(done||Find.CurrentMap!=map||!Environment.GetCommandLineArgs().Contains("-tmm-readiness-probe"))return;try{if(flow==null){flow=Run().GetEnumerator();Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;}if(Find.TickManager.TicksGame==lastTick)return;lastTick=Find.TickManager.TicksGame;if(!flow.MoveNext())Finish();}catch(Exception e){error=e.ToString();Finish();}}
    private void Finish(){done=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"ServiceReadinessProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"errors\":"+(error==null?"[]":"["+Q(error)+"]")+"}");}
    public override void MapComponentOnGUI(){
        if(!done||desk==null||!Environment.GetCommandLineArgs().Contains("-tmm-readiness-probe")||Event.current.type!=EventType.Repaint)return;
        if(!ui){ui=true;string failure=null;try{
            var font=Text.Font;var anchor=Text.Anchor;var color=GUI.color;bool wrap=Text.WordWrap;
            window=new Dialog_ServiceReadiness(desk);window.DoWindowContents(new Rect(0,0,744,696));
            if(Text.Font!=font||Text.Anchor!=anchor||GUI.color!=color||Text.WordWrap!=wrap)throw new Exception("Readiness window leaked GUI state");
            for(int page=0;page<5;page++)new Dialog_ComplexServices(desk){page=page}.DoWindowContents(new Rect(0,0,744,726));
            new Dialog_RestaurantMenu(desk).DoWindowContents(new Rect(0,0,544,586));
        }catch(Exception e){failure=e.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"ServiceReadinessUiProbe.json"),"{\"passed\":"+(failure==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(failure==null?"null":Q(failure))+"}");return;}
        renderFrame++;if(renderFrame==2){Find.WindowStack.Add(window);}if(renderFrame==12)Capture("ServiceReadiness-Top.png");
        if(renderFrame==16)AccessTools.Field(typeof(Dialog_ServiceReadiness),"scroll").SetValue(window,new Vector2(0,500));if(renderFrame==24)Capture("ServiceReadiness-Clinic.png");
        if(renderFrame==28)AccessTools.Field(typeof(Dialog_ServiceReadiness),"scroll").SetValue(window,new Vector2(0,950));if(renderFrame==36)Capture("ServiceReadiness-Middle.png");
        if(renderFrame==40)AccessTools.Field(typeof(Dialog_ServiceReadiness),"scroll").SetValue(window,new Vector2(0,2200));if(renderFrame==48)Capture("ServiceReadiness-Bottom.png");if(renderFrame==52){window.Close();Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;}
    }
    private void Capture(string name){AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"),"CaptureScreenshot",new[]{typeof(string)}).Invoke(null,new object[]{Path.Combine(GenFilePaths.SaveDataFolderPath,name)});}
}
