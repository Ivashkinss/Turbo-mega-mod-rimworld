// Disposable fixture only. Completion is accelerated by setting native progress;
// this does not represent four naturally elapsed game days.
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
public static class DeathrestProbeLaunch
{
    static DeathrestProbeLaunch()
    {
        var args=Environment.GetCommandLineArgs();
        if(args.Contains("-tmm-deathrest-probe")||args.Contains("-tmm-deathrest-reload"))new Harmony("tmm.test.deathrest.temperature").Patch(AccessTools.PropertyGetter(typeof(MapTemperature),"OutdoorTemp"),prefix:new HarmonyMethod(typeof(DeathrestProbeLaunch),"Temp"));
        if(args.Contains("-tmm-deathrest-reload"))new Harmony("tmm.test.deathrest.cancel.trace").Patch(AccessTools.Method(typeof(GuestDeathrest),"Cancel"),prefix:new HarmonyMethod(typeof(DeathrestProbeLaunch),"CancelTrace"));
        if(args.Contains("-tmm-deathrest-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-Deathrest"));
    }
    private static void CancelTrace(Pawn p)
    {
        if(p==null||p.MapHeld==null)return;var comp=HotelBillingHooks.Guest(p);var bed=comp==null?null:AccessTools.Field(comp.GetType(),"bed").GetValue(comp) as Building_Bed;var desk=GuestDossiers.Desk(p.MapHeld);
        string data="tick="+GuestDossiers.Now+" pawn="+p+" rawBed="+bed+" current="+p.CurrentBed()+" visit="+GuestDeathrest.Visiting(p)+" sleep="+GuestDeathrest.Sleeping(p)+" tend="+p.health.HasHediffsNeedingTend()+" gene="+GuestDeathrest.Gene(p).DeathrestPercent;
        if(bed!=null)data+=" spawned="+bed.Spawned+" faction="+bed.Faction+" medical="+bed.Medical+" burning="+bed.IsBurning()+" temp="+bed.Position.GetTemperature(p.MapHeld)+" min="+p.GetStatValue(StatDefOf.ComfyTemperatureMin)+" max="+p.GetStatValue(StatDefOf.ComfyTemperatureMax)+" cells="+string.Join(";",bed.OccupiedRect().Select(c=>c+":"+c.Roofed(p.MapHeld)+":"+(desk!=null&&desk.area[c])+":"+(desk!=null&&ComplexLayout.StaffOnly(desk,c))));
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"DeathrestCancelTrace.log"),data+Environment.NewLine+Environment.StackTrace+Environment.NewLine);
    }
    private static bool Temp(ref float __result){__result=21;return false;}
}
public sealed class DeathrestProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building_Bed bed,homeCasket;
    private Pawn guest;
    private Lord originalLord;
    private bool saved,loaded,reported,ui;
    private int checks,stage,start,savedMoney,savedDue,savedStarted;
    private long savedDeadline;
    private string loadState;
    private readonly List<string> errors=new List<string>();
    public DeathrestProbe(Map map):base(map){}
    private Gene_Deathrest Gene {get{return GuestDeathrest.Gene(guest);}}
    private GuestDeathrest Service {get{return map.GetComponent<GuestDeathrest>();}}
    private GuestDeathrestStay Session {get{return GuestDeathrest.Stay(guest);}}
    private HotelStay Paid {get{return map.GetComponent<HotelBilling>().stays.Single(s=>s.guest==guest);}}
    private CompDeathrestBindable Bindable {get{return bed.TryGetComp<CompDeathrestBindable>();}}
    private int Money {get{return HotelBillingHooks.Money(guest);}}
    private int Floor {get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}}
    private static object Call(object o,string m,params object[] args){return AccessTools.Method(o.GetType(),m).Invoke(o,args);}
    private static void Set(object o,string f,object v){AccessTools.Field(o.GetType(),f).SetValue(o,v);}
    private void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    private static string Escape(string t){return "\""+t.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    public override void ExposeData()
    {
        base.ExposeData();Scribe_References.Look(ref desk,"drDesk");Scribe_References.Look(ref bed,"drBed");Scribe_References.Look(ref homeCasket,"drHomeCasket");Scribe_References.Look(ref guest,"drGuest");Scribe_References.Look(ref originalLord,"drLord");
        Scribe_Values.Look(ref saved,"drSaved");Scribe_Values.Look(ref savedMoney,"drMoney");Scribe_Values.Look(ref savedDue,"drDue");Scribe_Values.Look(ref savedStarted,"drStarted");Scribe_Values.Look(ref savedDeadline,"drDeadline");
        if(Scribe.mode==LoadSaveMode.PostLoadInit){
            // The map component registry may still be restoring the deep list.
            // Validate native pawn/bed data here; service references below are
            // checked at the first map update after registry restoration.
            loaded=saved&&GuestDeathrest.Sleeping(guest)&&guest.GetLord()==originalLord&&Money==savedMoney&&Paid.nextChargeTick==savedDue&&Bindable.BoundPawn==guest;
            loadState="saved="+saved+" session="+(Session!=null)+" started="+(Session==null?-1:Session.started)+" expected="+savedStarted+" deadline="+(Session==null?-1:Session.deadline)+" expected="+savedDeadline+" temporary="+(Session!=null&&Session.temporaryBinding)+" sleep="+GuestDeathrest.Sleeping(guest)+" lord="+(guest.GetLord()==originalLord)+" money="+Money+" expected="+savedMoney+" due="+Paid.nextChargeTick+" expected="+savedDue+" bound="+(Bindable.BoundPawn==guest);
        }
    }
    private void Setup()
    {
        Check(ModsConfig.BiotechActive&&GuestDeathrestHooks.BindingPatchInstalled,"Deathrest hooks unavailable");
        var def=DefDatabase<ThingDef>.GetNamed("DeathrestCasketGuest");Check(def.tickerType==TickerType.Rare&&def.HasComp(typeof(CompDeathrestBindable)),"Generated casket lost rare ticker/bindable");
        Check(def.GetCompProperties<CompProperties_Power>().PowerConsumption==DefDatabase<ThingDef>.GetNamed("DeathrestCasket").GetCompProperties<CompProperties_Power>().PowerConsumption,"Generated casket lost native base power consumption");
        map.fogGrid.ClearAllFog();
        foreach(var c in new CellRect(35,35,25,25)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);
        }
        // No hostile targets or blocked path in this disposable fixture.
        foreach(var c in new CellRect(0,43,35,3)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);
        }
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var c in new CellRect(35,35,25,25))area[c]=true;
        desk=(Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk"));desk.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(desk,new IntVec3(40,0,40),map);desk.area=area;desk.roomRate=30;desk.vipChance=0;desk.deathrestGuests=false;desk.ApplyServiceAreas();
        var support=ThingMaker.MakeThing(ThingDefOf.Wall,ThingDefOf.Steel);support.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(support,new IntVec3(46,0,46),map);
        foreach(var c in new CellRect(42,42,6,6))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
        bed=(Building_Bed)ThingMaker.MakeThing(def);bed.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(bed,new IntVec3(44,0,44),map,Rot4.North);Call(bed,"SetRentalFee",30);bed.TryGetComp<CompPowerTrader>().PowerOn=true;
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        guest=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false,allowDead:false,allowDowned:false,allowAddictions:false,fixedBiologicalAge:30,validatorPreGear:p=>!p.health.HasHediffsNeedingTend()&&!HealthAIUtility.ShouldSeekMedicalRest(p)));
        foreach(var gene in guest.genes.GenesListForReading.ToArray())guest.genes.RemoveGene(gene);
        guest.genes.AddGene(DefDatabase<GeneDef>.GetNamed("Deathrest"),false);guest.needs.AddOrRemoveNeedsAsAppropriate();
        homeCasket=(Building_Bed)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DeathrestCasket"));homeCasket.SetFaction(faction);GenSpawn.Spawn(homeCasket,new IntVec3(34,0,44),map);
        Check(guest.ownership.ClaimDeathrestCasket(homeCasket),"Could not set permanent home casket fixture");
        guest.inventory.innerContainer.ClearAndDestroyContents();var silver=ThingMaker.MakeThing(ThingDefOf.Silver);silver.stackCount=1000;guest.inventory.innerContainer.TryAdd(silver);
        GenSpawn.Spawn(guest,new IntVec3(43,0,44),map);guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=0.5f;
        foreach(var target in map.attackTargetsCache.TargetsHostileToFaction(faction).Select(t=>t.Thing).Where(t=>t.Spawned).ToArray())target.DeSpawn();
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(43,0,44),new List<Pawn>{guest},map,false,false,600000});originalLord=guest.GetLord();originalLord.ReceiveMemo("TravelArrived");
        guest.needs.joy.CurLevelPercentage=1;Gene.DeathrestNeed.CurLevelPercentage=0.1f;
        var ordinary=map.mapPawns.FreeColonistsSpawned.First();var available=AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),"IsAvailableBed");
        Check(!(bool)available.Invoke(null,new object[]{bed,ordinary,1000,0}),"Guest casket offered to pawn without gene");
        Set(HotelBillingHooks.Guest(guest),"lastBedCheckTick",GuestDossiers.Now-10000);
        var giver=Activator.CreateInstance(AccessTools.TypeByName("Hospitality.JobGiver_ClaimBed"));var job=Call(giver,"TryGiveJob",guest) as Job;
        Check(job!=null,"Native check-in did not offer guest casket");guest.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
    }
    private void StartCycle()
    {
        Gene.DeathrestNeed.CurLevelPercentage=0.1f;guest.needs.food.CurLevelPercentage=1;
        var job=guest.thinker.MainThinkNodeRoot.TryIssueJobPackage(guest,default(JobIssueParams)).Job;Check(job!=null&&job.def==JobDefOf.Deathrest,"Native guest tree did not offer safe affordable deathrest; bed="+HotelBillingHooks.Bed(guest)+" need="+Gene.DeathrestNeed.CurLevelPercentage+" job="+job);
        guest.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;
    }
    private void CompleteNative()
    {
        Set(Gene,"deathrestTicks",Gene.MinDeathrestTicks);Set(Gene,"adjustedDeathrestTicks",240000f);Service.Process();
        Check(!GuestDeathrest.Sleeping(guest)&&Session==null,"Completed native cycle did not wake/clean session");
        Check(!guest.health.hediffSet.HasHediff(HediffDefOf.InterruptedDeathrest),"Completed cycle received interrupted penalty");
        Check(Bindable.BoundPawn==null&&!Gene.BoundBuildings.Contains(bed),"Temporary binding survived completion");
        Check(HotelBillingHooks.Bed(guest)==bed&&guest.GetLord()==originalLord,"Completion released room/group");
        Check(guest.ownership.AssignedDeathrestCasket==homeCasket,"Completion changed permanent home casket assignment");
    }
    private void FreshStep()
    {
        if(stage==0){Setup();return;}
        if(stage==1){
            if(HotelBillingHooks.Bed(guest)!=bed){if(GuestDossiers.Now-start>2500)throw new Exception("Check-in timed out: "+guest.CurJob);return;}
            Check(Paid.remaining<0&&Money==970,"Initial room charge incorrect");
            Check(bed.CompAssignableToPawn.GetType()==typeof(CompAssignableToPawn_Bed)&&guest.ownership.AssignedDeathrestCasket==homeCasket,"Rental replaced permanent casket assignment");
            int due=Paid.nextChargeTick,before=Money;desk.deathrestGuests=true;Check(GuestDeathrest.Ready(guest),"Guest not ready after paid check-in: visiting="+GuestDeathrest.Visiting(guest)+" active="+Gene.Active+" down="+guest.Downed+" food="+guest.needs.food.CurLevelPercentage+" need="+Gene.DeathrestNeed.CurLevelPercentage+" safe="+GuestDeathrest.RoomSafe(guest,bed)+" reserve="+guest.CanReserveAndReach(bed,PathEndMode.OnCell,Danger.None)+" medical="+HealthAIUtility.ShouldSeekMedicalRest(guest)+" tend="+guest.health.HasHediffsNeedingTend()+" clinic="+Clinic.ActivePatient(guest)+" due="+Paid.nextChargeTick+" now="+GuestDossiers.Now+" duration="+Gene.MinDeathrestTicks+" job="+guest.CurJob);
            desk.deathrestGuests=false;Check(GuestDeathrest.MakeJob(guest)==null,"Disabled service still offered cycle");desk.deathrestGuests=true;
            foreach(var t in guest.inventory.innerContainer.ToArray())t.Destroy();Check(!GuestDeathrest.Ready(guest),"Unaffordable cycle accepted");
            var cash=ThingMaker.MakeThing(ThingDefOf.Silver);cash.stackCount=before;guest.inventory.innerContainer.TryAdd(cash);
            Check(Paid.nextChargeTick==due&&Money==before,"Readiness checks changed money/deadline");
            StartCycle();stage=2;return;
        }
        if(stage==2){
            if(!GuestDeathrest.Sleeping(guest)||Session==null||!Session.temporaryBinding){if(GuestDossiers.Now-start>2500)throw new Exception("Cycle/binding timed out: "+guest.CurJob+" sleep="+GuestDeathrest.Sleeping(guest)+" session="+(Session!=null)+" bound="+Bindable.BoundPawn);return;}
            Check(guest.Downed&&guest.GetLord()==originalLord,"Native deathrest did not keep incapacitated guest in original group");
            Check(Session.bed==bed&&HotelBillingHooks.Bed(guest)==bed,"Session/paid room mismatch");
            Check(Gene.BoundBuildings.Contains(bed)&&Bindable.BoundPawn==guest,"Own casket did not bind natively");
            var external=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DeathrestAccelerator"));external.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(external,new IntVec3(47,0,44),map);
            Check(!(bool)Call(Gene,"CanBindToBindable",external.TryGetComp<CompDeathrestBindable>()),"Foreign amplifier faction restrictions bypassed");
            Check(!ClinicGuests.Eligible(guest),"Sleeping guest offered as ordinary clinical admission");
            var leave=Activator.CreateInstance(AccessTools.TypeByName("Hospitality.Trigger_TicksPassedAndOkayToLeave"),new object[]{0});
            Check(!(bool)Call(leave,"ActivateOn",originalLord,new TriggerSignal(TriggerSignalType.Tick)),"Group allowed normal timed departure during sleep");
            int oldMoney=Money,oldFloor=Floor;Paid.nextChargeTick=GuestDossiers.Now;map.GetComponent<HotelBilling>().ProcessPayments();
            Check(Money==oldMoney-30&&Floor==oldFloor+30&&GuestDeathrest.Sleeping(guest),"Sleeping room renewal failed");
            map.GetComponent<HotelBilling>().ProcessPayments();Check(Money==oldMoney-30,"Duplicate sleeping room charge");
            Check(!(bool)AccessTools.Field(typeof(Gene_Deathrest),"autoWake").GetValue(Gene),"Service altered native autoWake option");
            // Exercise native comp rare update with actual presence, not a fake bill.
            bed.TryGetComp<CompPowerTrader>().PowerOn=true;Call(Bindable,"TryIncreasePresence",250);bed.TickRare();Check(bed.TryGetComp<CompPowerTrader>().PowerOutput==-100f,"Guest casket did not update native active power: "+bed.TryGetComp<CompPowerTrader>().PowerOutput+" presence="+AccessTools.Field(typeof(CompDeathrestBindable),"presenceTicks").GetValue(Bindable));
            saved=true;savedMoney=Money;savedDue=Paid.nextChargeTick;savedStarted=Session.started;savedDeadline=Session.deadline;
            GameDataSaveLoader.SaveGame("TMM-Deathrest");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Deathrest")),"Active deathrest save missing");
            CompleteNative();Finish(false);
        }
    }
    private void ReloadStep()
    {
        Check(loaded,"Session, paid period, native sleep or temporary binding failed PostLoadInit roundtrip: "+loadState+" afterSession="+(Session!=null)+" afterLord="+(guest.GetLord()==originalLord)+" afterSleep="+GuestDeathrest.Sleeping(guest));
        Check(Session!=null&&Session.started==savedStarted&&Session.deadline==savedDeadline&&Session.temporaryBinding,"Saved service state did not restore after full map initialization: session="+(Session!=null)+" started="+(Session==null?-1:Session.started)+" expected="+savedStarted+" deadline="+(Session==null?-1:Session.deadline)+" expected="+savedDeadline+" temporary="+(Session!=null&&Session.temporaryBinding)+" sleep="+GuestDeathrest.Sleeping(guest)+" bound="+(Bindable.BoundPawn==guest)+" serviceEntries="+Service.stays.Count+" safe="+GuestDeathrest.RoomSafe(guest,bed)+" visit="+GuestDeathrest.Visiting(guest)+" lord="+(guest.GetLord()==originalLord));
        Check(guest.ownership.AssignedDeathrestCasket==homeCasket,"Permanent home casket was not restored");
        Check(desk.deathrestGuests&&GuestDeathrest.Sleeping(guest)&&HotelBillingHooks.Bed(guest)==bed,"Loaded guest lost service or room");
        Check(Money==savedMoney&&Paid.nextChargeTick==savedDue,"Load changed cash/paid deadline");CompleteNative();
        // A real second native cycle: forced nonpayment must wake before unclaim.
        Gene.DeathrestNeed.CurLevelPercentage=0.1f;var job=GuestDeathrest.MakeJob(guest);Check(job!=null,"Could not start post-load second cycle");guest.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;stage=3;
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-deathrest-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-deathrest-probe")))return;
        try{
            if(!reload){FreshStep();return;}
            if(stage!=3){ReloadStep();return;}
            if(!GuestDeathrest.Sleeping(guest)){if(GuestDossiers.Now-start>2500)throw new Exception("Second cycle timed out");return;}
            foreach(var t in guest.inventory.innerContainer.ToArray())t.Destroy();Paid.nextChargeTick=GuestDossiers.Now;map.GetComponent<HotelBilling>().ProcessPayments();
            Check(!GuestDeathrest.Sleeping(guest)&&Session==null,"Nonpayment stranded sleeping guest");
            Check(guest.health.hediffSet.HasHediff(HediffDefOf.InterruptedDeathrest),"Interrupted native cycle lacked normal consequence");
            Check(HotelBillingHooks.Bed(guest)==null&&Bindable.BoundPawn==null,"Nonpayment retained room or temporary binding");
            Check(guest.GetLord()==originalLord,"Interrupted guest lost visiting group");Finish(true);
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    public override void MapComponentOnGUI()
    {
        if(ui||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-deathrest-probe"))return;
        ui=true;string error=null;try{new Dialog_GuestNeeds(desk).DoWindowContents(new Rect(10,10,880,710));new Dialog_ComplexServices(desk){page=1}.DoWindowContents(new Rect(10,10,740,720));}catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"DeathrestUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"DeathrestReloadProbe.json":"DeathrestProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
}
