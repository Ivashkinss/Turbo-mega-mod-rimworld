using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class ClinicGuestReloadLaunch
{
    static ClinicGuestReloadLaunch(){
        if(Environment.GetCommandLineArgs().Contains("-tmm-clinicguest-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-ClinicGuest"));
        if(Environment.GetCommandLineArgs().Contains("-tmm-clinicguest-probe")){
            var h=new Harmony("tmm.test.clinicguest.trace");
            foreach(var name in new[]{"DismissPatient","PatientLeaves"})h.Patch(AccessTools.Method(Clinic.HospitalType,name),prefix:new HarmonyMethod(typeof(ClinicGuestReloadLaunch),"TraceRemoval"));
        }
    }
    private static void TraceRemoval(Pawn __0,MethodBase __originalMethod){File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"ClinicGuestRemoval.txt"),__originalMethod.Name+" pawn="+__0+" care="+ClinicGuests.NeedsCare(__0)+"\n"+Environment.StackTrace+"\n");}
}
public sealed class ClinicGuestProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building_Bed hotelBed,ward;
    private Pawn doctor,guest;
    private Lord lord;
    private Thing medicine,food,purchase;
    private int stage,started,checks,due,money,floor,nextDiagnostic,expectedCoins;
    private float bill,beforeFeeding;
    private bool reported,uiReported,saved;
    private readonly List<string> errors=new List<string>();
    private ClinicGuests Bridge {get{return map.GetComponent<ClinicGuests>();}}
    public ClinicGuestProbe(Map map):base(map){}
    public override void ExposeData(){
        base.ExposeData();Scribe_References.Look(ref desk,"cgDesk");Scribe_References.Look(ref hotelBed,"cgHotelBed");Scribe_References.Look(ref ward,"cgWard");
        Scribe_References.Look(ref doctor,"cgDoctor");Scribe_References.Look(ref guest,"cgGuest");Scribe_References.Look(ref lord,"cgLord");
        Scribe_References.Look(ref medicine,"cgMedicine");Scribe_References.Look(ref food,"cgFood");Scribe_References.Look(ref purchase,"cgPurchase");
        Scribe_Values.Look(ref due,"cgDue");Scribe_Values.Look(ref money,"cgMoney");Scribe_Values.Look(ref floor,"cgFloor");Scribe_Values.Look(ref bill,"cgBill");Scribe_Values.Look(ref saved,"cgSaved");
    }
    private void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
    private void Wait(Pawn p){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;p.jobs.StartJob(j,JobCondition.InterruptForced);}
    private object Data {get{return Clinic.Data(guest);}}
    private float Bill {get{return Data==null?0:(float)AccessTools.Field(Data.GetType(),"Bill").GetValue(Data);}}
    private int Floor(){return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}
    private HotelStay Stay {get{return map.GetComponent<HotelBilling>().stays.FirstOrDefault(s=>s.guest==guest);}}
    private Thing Spawn(string name,IntVec3 c,int count=1){var d=DefDatabase<ThingDef>.GetNamed(name);var t=ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);t.stackCount=count;if(t is Building)t.SetFaction(Faction.OfPlayer);return GenSpawn.Spawn(t,c,map);}
    private void Room(int x){var r=new CellRect(x,40,8,8);foreach(var c in r.EdgeCells)Spawn(c==new IntVec3(x+3,0,40)?"Door":"Wall",c);foreach(var c in r.ContractedBy(1))map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);}
    private void Setup(){
        foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var c in new CellRect(38,38,30,16)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);map.areaManager.Home[c]=true;
        }
        Room(40);Room(50);Area_Allowed common;map.areaManager.TryMakeNewAllowed(out common);foreach(var c in new CellRect(38,38,28,15))common[c]=true;
        desk=(Building_ComplexDesk)Spawn("TMM_ComplexDesk",new IntVec3(60,0,48));desk.area=common;
        Spawn("Bed",new IntVec3(43,0,43));Spawn("Bed",new IntVec3(53,0,43));map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();desk.ApplyServiceAreas();
        hotelBed=new IntVec3(43,0,43).GetThingList(map).OfType<Building_Bed>().Single();ward=new IntVec3(53,0,43).GetThingList(map).OfType<Building_Bed>().Single();ward.Medical=true;
        ward=new IntVec3(53,0,43).GetThingList(map).OfType<Building_Bed>().Single();ward.GetRoom().Temperature=21;hotelBed.GetRoom().Temperature=21;
        doctor=map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p=>!p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor));
        for(int i=0;doctor==null&&i<32;i++){var p=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);if(!p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor)){GenSpawn.Spawn(p,new IntVec3(54,0,44),map);doctor=p;Wait(p);}else p.Destroy();}
        Check(doctor!=null&&Clinic.WardBed(ward),"Doctor or automatic ward unavailable");doctor.Position=new IntVec3(54,0,44);doctor.skills.GetSkill(SkillDefOf.Medicine).Level=20;
        foreach(var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)doctor.workSettings.SetPriority(w,0);
        for(int h=0;h<24;h++)doctor.timetable.SetAssignment(h,TimeAssignmentDefOf.Work);
        medicine=Spawn("MedicineIndustrial",new IntVec3(55,0,44),3);food=Spawn("MealSimple",new IntVec3(54,0,45),3);
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        guest=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,allowAddictions:false,allowPregnant:false,fixedBiologicalAge:25f));GenSpawn.Spawn(guest,new IntVec3(44,0,43),map);
        guest.inventory.innerContainer.ClearAndDestroyContents();var silver=ThingMaker.MakeThing(ThingDefOf.Silver);silver.stackCount=500;guest.inventory.innerContainer.TryAdd(silver);
        guest.needs.food.CurLevelPercentage=1;guest.needs.rest.CurLevelPercentage=0.5f;
        var native=map.components.First(c=>c.GetType().FullName=="Hospitality.Hospitality_MapComponent");AccessTools.Field(native.GetType(),"askForSafety").SetValue(native,false);
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{faction,new IntVec3(44,0,44),new List<Pawn>{guest},map,false,false,600000});
        lord=guest.GetLord();lord.ReceiveMemo("TravelArrived");
        // Native Hospitality intentionally delays rich guests' first bed
        // search. Advance only that fixture cooldown, as the hotel probe does.
        AccessTools.Field(HotelBillingHooks.Guest(guest).GetType(),"lastBedCheckTick").SetValue(HotelBillingHooks.Guest(guest),Find.TickManager.TicksGame-10000);
        var giver=Activator.CreateInstance(AccessTools.TypeByName("Hospitality.JobGiver_ClaimBed"));
        var job=(Job)AccessTools.Method(giver.GetType(),"TryGiveJob").Invoke(giver,new object[]{guest});Check(job!=null,"No real hotel check-in job");guest.jobs.StartJob(job,JobCondition.InterruptForced);
        started=Find.TickManager.TicksGame;stage=1;
    }
    public override void MapComponentUpdate(){
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-clinicguest-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-clinicguest-probe")))return;
        try{
            // Only this isolated fixture owns the simulation. A stray pause
            // must not turn a medical AI check into a wall-clock timeout.
            if(Time.frameCount%120==0)File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"ClinicGuestFrames.log"),"frame="+Time.frameCount+" tick="+Find.TickManager.TicksGame+" speed="+Find.TickManager.CurTimeSpeed+" paused="+Find.TickManager.Paused+" stage="+stage+" windows="+string.Join(",",Find.WindowStack.Windows.Select(w=>w.GetType().FullName))+Environment.NewLine);
            Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
            if(reload){
                if(stage==0){
                Check(saved&&guest!=null&&doctor!=null&&desk!=null&&lord!=null,"Clinical guest save references missing");
                Check(Clinic.ActivePatient(guest)&&ClinicGuests.Visit(guest)!=null&&guest.GetLord()==lord,"Patient registry/original group lost on load");
                Check(HotelBillingHooks.Bed(guest)==hotelBed&&Stay!=null&&Stay.nextChargeTick==due&&HotelBillingHooks.Money(guest)==money,"Clinical load changed paid room/deadline/money");
                Check(guest.inventory.innerContainer.Contains(purchase)&&Bill>=bill-0.01f,"Clinical load lost purchase or bill");
                Check(guest.CurrentBed()==ward,"Hospitalized guest did not resume medical rest");
                // Debug recovery is deliberate: real tending/feeding were
                // performed before saving. This checks the automatic discharge
                // path, not a claim about elapsed natural wound-healing time.
                foreach(var h in guest.health.hediffSet.hediffs.Where(h=>h is Hediff_Injury||h.def==HediffDefOf.Anesthetic||h.def.defName=="Flu").ToArray())guest.health.RemoveHediff(h);
                var data=Data;var utility=AccessTools.TypeByName("Hospital.Utilities.PatientUtility");
                expectedCoins=(int)Math.Min(4000f,Math.Max(0f,(float)AccessTools.Method(utility,"CalculateSilverToReceive").Invoke(null,new object[]{guest,data})));
                Check(!ClinicGuests.NeedsCare(guest)&&expectedCoins>0,"Recovery fixture still needs care or has no bill");
                started=Find.TickManager.TicksGame;stage=10;return;
                }
                // Let the real periodic medical update discharge the guest;
                // a direct call can legitimately be inside its 250-tick cooldown.
                if(ClinicGuests.Visit(guest)!=null&&Find.TickManager.TicksGame-started<500)return;
                Check(ClinicGuests.Visit(guest)==null&&!Clinic.ActivePatient(guest)&&guest.GetLord()==lord,"Recovered guest did not return to original visit");
                Check(HotelBillingHooks.Bed(guest)==hotelBed&&Stay!=null&&Stay.nextChargeTick==due&&HotelBillingHooks.Money(guest)==money&&guest.inventory.innerContainer.Contains(purchase),
                    "Discharge changed guest possessions or paid stay: bed="+HotelBillingHooks.Bed(guest)+" expectedBed="+hotelBed+" due="+(Stay==null?-1:Stay.nextChargeTick)+"/"+due+" money="+HotelBillingHooks.Money(guest)+"/"+money+" purchase="+guest.inventory.innerContainer.Contains(purchase));
                Check(Floor()==floor+expectedCoins&&desk.clinicIncome==expectedCoins,"Discharge did not place exactly the calculated silver at reception");
                map.GetComponent<ClinicAccounts>().PlaceReceipts();Bridge.MapComponentTick();Check(Floor()==floor+expectedCoins,"Repeated clinical update duplicated payout");Finish(true);return;
            }
            if(stage==0){Setup();return;}
            if(stage==1){
                if((HotelBillingHooks.Bed(guest)==null||Stay==null)&&Find.TickManager.TicksGame-started<3000)return;
                Check(HotelBillingHooks.Bed(guest)==hotelBed&&Stay!=null&&HotelBillingHooks.Money(guest)==470,"Real hotel check-in failed or charged the wrong amount");
                due=Stay.nextChargeTick;money=HotelBillingHooks.Money(guest);purchase=ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);guest.inventory.innerContainer.TryAdd(purchase,false);
                var comp=HotelBillingHooks.Guest(guest);((List<int>)AccessTools.Field(comp.GetType(),"boughtItems").GetValue(comp)).Add(purchase.thingIDNumber);
                var wound=HediffMaker.MakeHediff(HediffDefOf.Cut,guest,guest.RaceProps.body.AllParts.First(p=>p.def==BodyPartDefOf.Torso));wound.Severity=2;guest.health.AddHediff(wound);
                // A minor cut may stop requiring medical rest immediately
                // after tending. Keep a real illness during the feeding/save
                // fixture instead of preventing the production discharge rule.
                var flu=HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Flu"),guest);flu.Severity=0.4f;guest.health.AddHediff(flu);
                guest.needs.rest.CurLevelPercentage=1;
                Check(Bridge.Admit(guest),"Existing hotel guest could not be admitted");guest.playerSettings.medCare=MedicalCareCategory.Best;
                Check(guest.GetLord()==lord&&HotelBillingHooks.Bed(guest)==hotelBed&&Stay.nextChargeTick==due&&HotelBillingHooks.Money(guest)==money,"Admission reset the visit or paid hotel stay");
                Check(guest.inventory.innerContainer.Contains(purchase)&&((List<int>)AccessTools.Field(comp.GetType(),"boughtItems").GetValue(comp)).Contains(purchase.thingIDNumber),"Admission destroyed inventory or purchase history");
                started=Find.TickManager.TicksGame;stage=2;return;
            }
            if(stage==2){
                if(guest.CurrentBed()!=ward&&Find.TickManager.TicksGame-started<3000)return;
                Check(guest.CurrentBed()==ward&&guest.CurJobDef==JobDefOf.LayDown,"Clinical guest AI did not choose its ward: "+guest.CurJob);
                HealthUtility.TryAnesthetize(guest);guest.needs.food.CurLevelPercentage=0.18f;
                float before=Bill;var cancelled=JobMaker.MakeJob(JobDefOf.FeedPatient,food,guest);cancelled.count=1;
                doctor.jobs.StartJob(cancelled,JobCondition.InterruptForced);doctor.jobs.StopAll();
                Check(Math.Abs(Bill-before)<0.001f,"Cancelled feeding increased the bill");
                Check(desk.SetRole(doctor,ComplexRole.Doctor),"Doctor role rejected");doctor.needs.food.CurLevelPercentage=1;doctor.needs.rest.CurLevelPercentage=1;if(doctor.needs.joy!=null)doctor.needs.joy.CurLevelPercentage=1;
                doctor.jobs.StopAll();started=Find.TickManager.TicksGame;stage=3;return;
            }
            if(stage==3){
                if(Find.TickManager.TicksGame>=nextDiagnostic){
                    nextDiagnostic=Find.TickManager.TicksGame+500;
                    var tend=(WorkGiver_Scanner)DefDatabase<WorkGiverDef>.GetNamed("DoctorTendToHumanlikes").Worker;
                    var feed=(WorkGiver_Scanner)DefDatabase<WorkGiverDef>.GetNamed("DoctorFeedHumanlikes").Worker;
                    File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"ClinicGuestProgress.txt"),
                        "doctor="+doctor.CurJob+" priority="+doctor.workSettings.GetPriority(WorkTypeDefOf.Doctor)+" drafted="+doctor.Drafted+" mental="+doctor.InMentalState+
                        " patientHost="+guest.HostFaction+" patient="+guest.CurJob+" tendedNow="+HealthAIUtility.ShouldBeTendedNowByPlayer(guest)+
                        " tendJob="+tend.HasJobOnThing(doctor,guest,false)+" feedJob="+feed.HasJobOnThing(doctor,guest,false)+
                        " tendSkip="+tend.ShouldSkip(doctor,false)+" reach="+doctor.CanReach(guest,PathEndMode.Touch,Danger.Deadly)+
                        " patientForbidden="+guest.IsForbidden(doctor)+" foodForbidden="+food.IsForbidden(doctor)+" medForbidden="+medicine.IsForbidden(doctor)+
                        " food="+guest.needs.food.CurLevelPercentage+" bill="+Bill+" active="+Clinic.ActivePatient(guest)+" visit="+(ClinicGuests.Visit(guest)!=null)+
                        " lordSame="+(guest.GetLord()==lord)+" leaving="+AccessTools.Field(lord.LordJob.GetType(),"leaving").GetValue(lord.LordJob)+
                        " care="+ClinicGuests.NeedsCare(guest)+" hediffs="+string.Join(",",guest.health.hediffSet.hediffs.Select(h=>h.def.defName)));
                }
                bool tended=!guest.health.HasHediffsNeedingTend(),fed=guest.needs.food.CurLevelPercentage>0.3f;
                if(tended&&!fed)beforeFeeding=Bill;
                if((!tended||!fed||doctor.CurJobDef==JobDefOf.FeedPatient)&&Find.TickManager.TicksGame-started<6500)return;
                Check(tended&&fed,"Real doctor did not tend/feed guest: "+doctor.CurJob+" guest="+guest.CurJob+" tended="+tended+" food="+guest.needs.food.CurLevelPercentage+" bill="+Bill);
                Check(medicine.stackCount<3&&food.stackCount<3,"Clinical work did not consume real medicine/food");
                Check(Bill>30f,"Completed medical work did not increase the real bill");
                Check(beforeFeeding>0&&Bill>beforeFeeding,"Successful feeding did not add to the treatment bill");
                Check(guest.Downed&&guest.GetLord()==lord,"Incapacitated clinical guest lost its original visitor group");
                Check(guest.GetLord()==lord&&HotelBillingHooks.Money(guest)==money&&Stay.nextChargeTick==due,"Treatment disturbed group or hotel payment");
                bill=Bill;floor=Floor();saved=true;GameDataSaveLoader.SaveGame("TMM-ClinicGuest");Finish(false);
            }
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    private static string Escape(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"ClinicGuestReloadProbe.json":"ClinicGuestProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
    public override void MapComponentOnGUI(){if(uiReported||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-clinicguest-probe"))return;uiReported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"ClinicGuestUiProbe.json"),"{\"passed\":true,\"visualLayoutInspected\":false}");}
}
