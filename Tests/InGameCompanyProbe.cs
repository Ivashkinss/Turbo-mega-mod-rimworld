using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class CompanyReloadLaunch
{
    static CompanyReloadLaunch() {
        if(Environment.GetCommandLineArgs().Contains("-tmm-company-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-Company"));
        if(Environment.GetCommandLineArgs().Contains("-tmm-company-probe"))
            new Harmony("tmm.test.company.cancellation").Patch(AccessTools.Method(typeof(CompanyService),"Process"),prefix:new HarmonyMethod(typeof(CompanyReloadLaunch),"TraceCancellation"));
    }
    private static readonly HashSet<int> traced = new HashSet<int>();
    private static string PawnState(Pawn p) {
        return p==null?"null":p.LabelShort+" pos="+p.Position+" fit="+CompanyRules.Fit(p)+" job="+p.CurJob+" awake="+p.Awake()+
            " rest="+(p.needs.rest==null?-1:p.needs.rest.CurLevelPercentage)+" mood="+(p.needs.mood==null?-1:p.needs.mood.CurLevelPercentage)+
            " mental="+p.InMentalState+" slave="+p.IsSlaveOfColony+" lord="+(p.GetLord()==null?"none":p.GetLord().CurLordToil.GetType().Name);
    }
    private static void TraceCancellation(CompanyService __instance) {
        foreach(var o in __instance.orders.Where(o=>!o.completed&&!__instance.Valid(o)))if(traced.Add(o.id))
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"CompanyCancellation.log"),
                "tick="+Find.TickManager.TicksGame+" id="+o.id+" cancelled="+o.cancelled+" talked="+o.talked+
                " participant="+CompanyRules.Participates(o.employee,o.desk)+" arrived="+RestaurantMeals.Guest(o.guest)+
                " room="+(CompanyRules.GuestRoom(o.guest)==o.roomBed)+" employee="+PawnState(o.employee)+" guest="+PawnState(o.guest)+Environment.NewLine);
    }
}
public sealed class CompanyProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Pawn employee, guest, other;
    private int checks, stage, started, price, pendingId, savedTalked, savedFloor;
    private bool saved, reported, uiReported;
    private readonly List<string> errors=new List<string>();
    private CompanyService Service { get { return map.GetComponent<CompanyService>(); } }
    private CompanyWallet Wallet { get { return Current.Game.GetComponent<CompanyAccounts>().Wallet(employee); } }
    public CompanyProbe(Map map):base(map){}
    public override void ExposeData()
    {
        base.ExposeData();Scribe_References.Look(ref desk,"companyDesk");Scribe_References.Look(ref employee,"companyEmployee");
        Scribe_References.Look(ref guest,"companyGuest");Scribe_References.Look(ref other,"companyOther");
        Scribe_Values.Look(ref saved,"companySaved");Scribe_Values.Look(ref price,"companyPrice");
        Scribe_Values.Look(ref pendingId,"companyOrderId");Scribe_Values.Look(ref savedTalked,"companyTalked");
        Scribe_Values.Look(ref savedFloor,"companyFloor");
    }
    private void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
    private int Floor(){return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}
    private void Wait(Pawn p){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
    private Thing Spawn(string name,IntVec3 c){var d=DefDatabase<ThingDef>.GetNamed(name);var t=ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);t.SetFaction(Faction.OfPlayer);return GenSpawn.Spawn(t,c,map);}
    private void ResetNeeds(Pawn p){if(p.needs.food!=null)p.needs.food.CurLevelPercentage=1;if(p.needs.rest!=null)p.needs.rest.CurLevelPercentage=1;if(p.needs.mood!=null)p.needs.mood.CurLevelPercentage=0.8f;}
    private CompanyOrder Offer()
    {
        // Only fixture setup is reset. Actual refusal caching is tested below.
        Service.decisions.Clear();Wait(guest);ResetNeeds(employee);ResetNeeds(guest);
        int seed=1;float willingness=CompanyRules.Willingness(employee);
        for(;seed<1000;seed++){Rand.PushState(seed);bool yes=Rand.Chance(willingness);Rand.PopState();if(yes)break;}
        Rand.PushState(seed);CompanyOrder order;
        try{order=Service.Offer(guest,employee);}finally{Rand.PopState();}
        Check(order!=null,"Paid conversation booking failed: guest="+RestaurantMeals.Guest(guest)+" fits="+CompanyRules.Fit(guest)+"/"+CompanyRules.Fit(employee)+" job="+guest.CurJob+" room="+CompanyRules.GuestRoom(guest)+" participates="+CompanyRules.Participates(employee,desk));
        return order;
    }
    private void BuyoutCases()
    {
        string root=LoadedModManager.RunningModsListForReading.First(m=>m.PackageId=="colonyproject.integrated").RootDir;
        foreach(var line in File.ReadAllLines(Path.Combine(root,"Tests/WorkBuyoutCases.csv")).Skip(1)){
            if(string.IsNullOrWhiteSpace(line))continue;var row=line.Split(',');
            var model=new WorkBuyouts(Current.Game);
            var entry=new WorkBuyoutEntry{pawn=other,target=int.Parse(row[5]),credited=int.Parse(row[6]),
                halfRemainder=(int)(float.Parse(row[7],CultureInfo.InvariantCulture)*2),active=bool.Parse(row[4])};
            model.entries.Add(entry);if(bool.Parse(row[11]))entry.receipts.Add("fixture-case");
            int before=entry.credited;
            // Legacy clinic fixtures have no identified worker. Attributed
            // medical receipts are covered by the separate clinic fixture.
            if(row[1]!="clinic")model.Credit(other,"fixture-case",int.Parse(row[2]),bool.Parse(row[3]));
            Check(entry.credited-before==int.Parse(row[8]),"Independent credit case "+row[0]);
            Check(Math.Abs(entry.halfRemainder/2f-float.Parse(row[9],CultureInfo.InvariantCulture))<0.001f,"Independent carry case "+row[0]);
            Check(entry.Remaining==int.Parse(row[10]),"Independent cap case "+row[0]);
        }
    }
    private void Setup()
    {
        foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var c in new CellRect(38,38,24,15)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant)t.Destroy();
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);map.areaManager.Home[c]=true;
        }
        var rect=new CellRect(40,40,8,8);
        foreach(var c in rect.EdgeCells)Spawn(c==new IntVec3(43,0,40)?"Door":"Wall",c);
        foreach(var c in rect.ContractedBy(1))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
        Spawn("Bed",new IntVec3(44,0,44));map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var c in new CellRect(40,40,20,12))area[c]=true;
        desk=(Building_ComplexDesk)Spawn("TMM_ComplexDesk",new IntVec3(50,0,44));desk.area=area;desk.ApplyServiceAreas();
        Func<Pawn,bool> capable=p=>!p.WorkTypeIsDisabled(CompanyRules.Work)&&!p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)&&p.health.capacities.CapableOf(PawnCapacityDefOf.Talking);
        employee=map.mapPawns.FreeColonistsSpawned.FirstOrDefault(capable);
        for(int attempt=0;employee==null&&attempt<32;attempt++){
            var candidate=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);
            if(capable(candidate)){GenSpawn.Spawn(candidate,new IntVec3(43,0,42),map);employee=candidate;Wait(employee);}else candidate.Destroy();
        }
        if(employee==null)throw new Exception("No capable fixture employee");
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"CompanyCancellation.log"),"fixture employee before relocation="+employee.Position+Environment.NewLine);
        employee.pather.StopDead();employee.Position=new IntVec3(43,0,42);employee.Notify_Teleported();Wait(employee);ResetNeeds(employee);
        foreach(var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)employee.workSettings.SetPriority(w,0);
        employee.workSettings.SetPriority(WorkTypeDefOf.Hauling,2);
        for(int h=0;h<24;h++)employee.timetable.SetAssignment(h,TimeAssignmentDefOf.Work);
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        guest=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,faction);GenSpawn.Spawn(guest,new IntVec3(43,0,43),map);ResetNeeds(guest);
        guest.inventory.innerContainer.ClearAndDestroyContents();var silver=ThingMaker.MakeThing(ThingDefOf.Silver);silver.stackCount=500;guest.inventory.innerContainer.TryAdd(silver);
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(44,0,43),new List<Pawn>{guest},map,false,false,600000});guest.GetLord().ReceiveMemo("TravelArrived");
        var bed=new IntVec3(44,0,44).GetThingList(map).OfType<Building_Bed>().Single();
        AccessTools.Method(bed.GetType(),"TryClaimBed").Invoke(bed,new object[]{guest});AccessTools.Field(HotelBillingHooks.Guest(guest).GetType(),"bed").SetValue(HotelBillingHooks.Guest(guest),bed);Wait(guest);
        Check(Service.Offer(guest,employee)==null,"Unassigned employee received conversation work");
        Check(desk.SetCompany(employee,true)&&employee.workSettings.GetPriority(WorkTypeDefOf.Hauling)>0,"Enabling conversation erased other work");
        Check(CompanyRules.Participates(employee,desk),"Ordinary colonist cannot join ordinary conversation service");
        employee.needs.rest.CurLevelPercentage=0.1f;Check(Service.Offer(guest,employee)==null&&HotelBillingHooks.Money(guest)==500,"Tired employee charged for a session");ResetNeeds(employee);
        other=map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p=>p!=employee);
        if(other==null){other=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);GenSpawn.Spawn(other,new IntVec3(51,0,44),map);Wait(other);}
        BuyoutCases();
        other.needs.rest.CurLevelPercentage=0.1f;Check(!Service.Consent(guest,other),"Tired worker agreed");ResetNeeds(other);
        Check(!Service.Consent(guest,other),"Repeated proposal rerolled an existing refusal");Service.decisions.Clear();
        var order=Offer();price=order.price;Check(HotelBillingHooks.Money(guest)==500-price&&Wallet.Balance==0,"Booking did not hold real silver separately");
        desk.companyPercent=200;Check(order.price==price,"Booking price changed after rate adjustment");
        order.cancelled=true;Service.Process();Service.Process();Check(HotelBillingHooks.Money(guest)==500&&Wallet.Balance==0,"Cancelled conversation did not refund exactly once");
        employee.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);
        Check(employee.IsSlaveOfColony&&desk.SetCompany(employee,true),"Slave cannot perform ordinary conversation work");
        var book=Current.Game.GetComponent<WorkBuyouts>();
        Check(book.Begin(employee,10000)!=null,"Ordinary work buyout accounting did not start");
        other.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);
        var sample=book.Begin(other,3);Check(sample!=null,"Arithmetic fixture account missing");
        book.Credit(other,"fixture-half-1",1,true);book.Credit(other,"fixture-half-1",1,true);
        Check(sample.credited==0&&sample.halfRemainder==1,"Half-silver carry or duplicate guard failed");
        book.Credit(other,"fixture-half-2",1,true);Check(sample.credited==1&&sample.halfRemainder==0,"Two half shares did not make one silver");
        book.Credit(other,"fixture-cap",100,false);Check(sample.credited==3&&sample.Remaining==0,"Buyout credit exceeded its target");
        desk.companyPercent=100;order=Offer();price=order.price;
        desk.Employee(employee).mode=ComplexWorkMode.ColonyOnly;Check(!new WorkGiver_Company().HasJobOnThing(employee,desk),"Colony-only worker took complex conversation");
        desk.Employee(employee).mode=ComplexWorkMode.Both;
        var work=new WorkGiver_Company();Check(work.HasJobOnThing(employee,desk),"Mixed worker did not find accepted conversation");
        var job=new JobGiver_Work().TryIssueJobPackage(employee,default(JobIssueParams)).Job;
        Check(job!=null&&job.def==CompanyRules.EmployeeJob,"Native work AI did not select conversation: "+job);
        employee.jobs.StartJob(job,JobCondition.InterruptForced);
        other.needs.rest.CurLevelPercentage=0.1f;Service.Consent(guest,other);ResetNeeds(other);
        pendingId=order.id;started=Find.TickManager.TicksGame;stage=1;
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-company-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-company-probe")))return;
        try{
            if(!reload&&stage==0){Setup();return;}
            if(reload&&stage==0){
                Check(saved&&desk!=null&&employee!=null&&guest!=null,"Conversation save references missing");
                var o=Service.ById(pendingId);Check(o!=null&&!o.cancelled&&!o.completed&&o.price==price&&o.talked>=savedTalked,"Pending session/quote/progress lost on load");
                Check(HotelBillingHooks.Money(guest)+o.GetDirectlyHeldThings().Sum(t=>t.stackCount)==500&&Wallet.Balance==0&&Floor()==savedFloor,"Save/load duplicated/lost escrow");
                Check(Current.Game.GetComponent<WorkBuyouts>().Entry(employee).active&&Current.Game.GetComponent<WorkBuyouts>().Entry(employee).credited==0,"Buyout state changed before completed work");
                Check(Service.decisions.Any(d=>d.employee==other&&!d.agreed)&&!Service.Consent(guest,other),"Saved refusal was rerolled");
                started=Find.TickManager.TicksGame;stage=1;
            }
            var order=Service.ById(pendingId);
            if(!reload&&stage==1){
                if(order!=null&&order.talked<300&&Find.TickManager.TicksGame-started<4000)return;
                Check(order!=null&&order.talked>=300&&employee.CurJobDef==CompanyRules.EmployeeJob&&guest.CurJobDef==CompanyRules.GuestJob,"Real conversation did not begin: employee="+employee.CurJob+" guest="+guest.CurJob+" order="+(order==null?"none":order.talked.ToString()));
                Check(employee.Position==order.employeeCell&&guest.Position==order.guestCell&&employee.CurrentBed()==null&&guest.CurrentBed()==null,"Conversation used a bed or left the guest room");
                savedTalked=order.talked;savedFloor=Floor();saved=true;GameDataSaveLoader.SaveGame("TMM-Company");Finish(false);return;
            }
            if(reload&&stage==1){
                if(desk.companyIncome==0&&Find.TickManager.TicksGame-started<6000)return;
                Check(Floor()==savedFloor+price&&Wallet.Balance==0&&Wallet.earned==price&&HotelBillingHooks.Money(guest)==500-price&&desk.companyIncome==price,"Real completed session did not pay the colony once");
                var book=Current.Game.GetComponent<WorkBuyouts>();Check(book.Entry(employee).credited==price,"Ordinary conversation did not credit 100 percent toward buyout");
                Check(guest.needs.mood.thoughts.memories.Memories.Any(m=>m.def.defName=="TMM_GoodCompany"),"Guest did not receive good-company thought");
                Service.Process();Service.Process();Check(Floor()==savedFloor+price&&Wallet.earned==price&&book.Entry(employee).credited==price,"Repeated settlement duplicated income or buyout credit");
                Check(desk.hotelIncome==0&&desk.restaurantIncome==0&&desk.clinicIncome==0&&desk.companyIncome==price,"Conversation mixed with other service accounts");
                Check(GuestDossiers.Book.Find(guest.GetUniqueLoadID()).company==price,"Dossier conversation income lost or duplicated");
                Check(book.Release(other)&&!other.IsSlaveOfColony&&book.Entry(other).released&&!book.Release(other),"Completed ordinary buyout did not release exactly once");
                Wait(employee);Wait(guest);desk.SetCompany(employee,false);
                Check(employee.workSettings.GetPriority(CompanyRules.Work)==0&&employee.workSettings.GetPriority(WorkTypeDefOf.Hauling)>0,"Removing conversation did not restore previous work settings");
                Finish(true);
            }
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    private static string Escape(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"CompanyReloadProbe.json":"CompanyProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
    public override void MapComponentOnGUI(){
        if(uiReported||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-company-probe"))return;
        uiReported=true;string error=null;GUI.BeginGroup(new Rect(0,0,1050,720));try{new Dialog_ComplexEmployees(desk).DoWindowContents(new Rect(0,0,990,640));}catch(Exception ex){error=ex.ToString();}finally{GUI.EndGroup();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"CompanyUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
}
