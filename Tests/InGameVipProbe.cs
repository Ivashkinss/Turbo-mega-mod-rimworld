using System;
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
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class VipProbeLaunch
{
    static VipProbeLaunch(){if(Environment.GetCommandLineArgs().Contains("-tmm-vip-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame(Environment.GetCommandLineArgs().Contains("-tmm-vip-migration")?"TMM-Vip-Migration":"TMM-Vip"));}
}
public sealed class VipProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building_Bed bed;
    private Pawn vip,ordinary;
    private string id;
    private int checks,stage,moneyBefore,floorBefore,start,grant;
    private bool saved,reported,uiReported,loadedFieldsCorrect;
    private readonly List<string> errors=new List<string>();
    public VipProbe(Map map):base(map){}
    public override void ExposeData(){base.ExposeData();Scribe_References.Look(ref desk,"vipDesk");Scribe_References.Look(ref bed,"vipBed");Scribe_References.Look(ref vip,"vipGuest");Scribe_References.Look(ref ordinary,"vipOrdinary");Scribe_Values.Look(ref id,"vipId");Scribe_Values.Look(ref saved,"vipSaved");Scribe_Values.Look(ref grant,"vipGrant");
        if(Scribe.mode==LoadSaveMode.PostLoadInit&&Environment.GetCommandLineArgs().Contains("-tmm-vip-reload")){
            var r=GuestProfiles.Row(vip);var x=r==null||r.Current==null?null:r.Current.experience;
            loadedFieldsCorrect=x!=null&&x.meal==1&&Math.Abs(x.score-0.72f)<0.0001f&&x.firstClaim>=0&&x.nextBedCheck>0&&x.observingLodging;
        }
    }
    private void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    private static object Call(object target,string method,params object[] args){return AccessTools.Method(target.GetType(),method).Invoke(target,args);}
    private static void Set(object target,string name,object value){AccessTools.Field(target.GetType(),name).SetValue(target,value);}
    private static string Escape(string text){return "\""+text.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private int Floor(){return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}
    private GuestDossier Row {get{return GuestProfiles.Row(vip);}}
    private Pawn Spawn(Faction faction,IntVec3 cell)
    {
        var pawn=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false,allowDead:false,allowDowned:false,allowAddictions:false,fixedBiologicalAge:30));
        foreach(var trait in pawn.story.traits.allTraits.Where(t=>t.def==TraitDefOf.Ascetic).ToArray())pawn.story.traits.RemoveTrait(trait);
        GenSpawn.Spawn(pawn,cell,map);pawn.inventory.innerContainer.ClearAndDestroyContents();return pawn;
    }
    private void Arrive(Pawn pawn)
    {
        var lord=pawn.GetLord();if(lord!=null)lord.Notify_PawnLost(pawn,PawnLostCondition.Undefined);
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"CreateLord").Invoke(null,new object[]{pawn.Faction,new IntVec3(44,0,44),new List<Pawn>{pawn},map,false,false,600000});
        pawn.GetLord().ReceiveMemo("TravelArrived");
    }
    private Job JobFor(Pawn pawn){return (Job)Call(Activator.CreateInstance(AccessTools.TypeByName("Hospitality.JobGiver_ClaimBed")),"TryGiveJob",pawn);}
    private void AddCoins(Pawn pawn,int count){var t=ThingMaker.MakeThing(ThingDefOf.Silver);t.stackCount=count;Check(pawn.inventory.innerContainer.TryAdd(t,false),"Coin fixture failed");}
    private void Setup()
    {
        map.fogGrid.ClearAllFog();
        foreach(var c in new CellRect(36,36,30,26)){
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);
        }
        desk=(Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk"));desk.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(desk,new IntVec3(44,0,44),map);
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var c in new CellRect(36,36,30,26))area[c]=true;desk.area=area;desk.vipChance=100;desk.ApplyServiceAreas();
        var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
        vip=Spawn(faction,new IntVec3(46,0,44));id=vip.GetUniqueLoadID();floorBefore=Floor();
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"),"GiveItems").Invoke(null,new object[]{new List<Pawn>{vip}});
        Check(Row!=null&&Row.profile.evaluated&&Row.profile.vip&&Row.profile.arrivalPrepared,"Native GiveItems did not establish VIP identity");
        grant=Row.profile.grantAdded;Check(grant>0&&HotelBillingHooks.Money(vip)>=1500,"VIP did not receive a real purse");
        Check(Floor()==floorBefore&&MassUtility.FreeSpace(vip)>=2.99f,"VIP grant spilled money or overloaded guest");
        moneyBefore=HotelBillingHooks.Money(vip);GuestProfiles.Prepare(vip);Check(HotelBillingHooks.Money(vip)==moneyBefore&&Row.profile.grantAdded==grant,"Repeated prepare minted coins");
        var interrupted=Spawn(faction,new IntVec3(46,0,48));GuestProfiles.Prepare(interrupted);var aborted=GuestProfiles.Row(interrupted);
        Check(aborted.profile.arrivalPrepared&&aborted.Current==null,"Approach fixture was not prepared");
        interrupted.DeSpawn();GuestDossiers.Book.Synchronize();Check(!aborted.profile.arrivalPrepared,"Aborted approach permanently locked future purse");
        GenSpawn.Spawn(interrupted,new IntVec3(46,0,48),map);interrupted.inventory.innerContainer.ClearAndDestroyContents();GuestProfiles.Prepare(interrupted);
        Check(aborted.profile.evaluated&&aborted.profile.vip&&HotelBillingHooks.Money(interrupted)>0,"Later physical visit lost VIP identity or fresh purse");
        interrupted.DeSpawn();GuestDossiers.Book.Synchronize();Find.WorldPawns.PassToWorld(interrupted,PawnDiscardDecideMode.Discard);
        Arrive(vip);Check(Row.Current!=null&&GuestDossiers.Arrived(vip),"VIP did not enter native visit");
        Check(JobFor(vip)==null,"VIP skipped first bed search delay");
        // A free ordinary bed remains a valid fallback; no bypass of native fees.
        bed=(Building_Bed)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("BedGuest"),ThingDefOf.Steel);bed.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(bed,new IntVec3(47,0,44),map);Call(bed,"SetRentalFee",600);
        Row.Current.experience.nextBedCheck=GuestDossiers.Now;
        var job=JobFor(vip);Check(job!=null&&job.targetA.Thing==bed&&job.takeExtraIngestibles==600,"VIP fallback/native price selection failed");
        // Deliberately fragmented money must still satisfy a fee above 500.
        vip.inventory.innerContainer.ClearAndDestroyContents();AddCoins(vip,400);AddCoins(vip,400);GuestProfiles.Consolidate(vip);
        Check(HotelBillingHooks.Money(vip)==800&&vip.inventory.innerContainer.Count(t=>t.def==ThingDefOf.Silver)==1,"VIP consolidation lost coins");
        Row.Current.experience.nextBedCheck=GuestDossiers.Now;job=JobFor(vip);Check(job!=null,"Fragmented VIP could not afford native fee");
        moneyBefore=HotelBillingHooks.Money(vip);floorBefore=Floor();vip.jobs.StartJob(job,JobCondition.InterruptForced);start=GuestDossiers.Now;stage=1;Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
        ordinary=Spawn(faction,new IntVec3(45,0,46));desk.vipChance=0;GuestProfiles.Prepare(ordinary);Arrive(ordinary);AddCoins(ordinary,2000);
        Set(HotelBillingHooks.Guest(ordinary),"lastBedCheckTick",GuestDossiers.Now);Check(!GuestProfiles.Vip(ordinary)&&JobFor(ordinary)==null,"Ordinary guest native wealth delay was changed");
        var blocked=Spawn(faction,new IntVec3(45,0,47));GuestDossiers.Book.SetBlocked(GuestDossiers.Book.Observe(blocked),true,"test");desk.vipChance=100;GuestProfiles.Prepare(blocked);Check(!GuestProfiles.Row(blocked).profile.evaluated&&HotelBillingHooks.Money(blocked)==0,"Blacklisted visitor got VIP grant");blocked.DeSpawn();
    }
    private void Experience()
    {
        Check(HotelBillingHooks.Bed(vip)==bed,"Native check-in did not assign bed");
        Check(HotelBillingHooks.Money(vip)==moneyBefore-600&&Floor()==floorBefore+600,"Native check-in did not transfer exactly 600 physical coins");
        Check(Row.hotel==600&&map.GetComponent<HotelBilling>().stays.Any(s=>s.guest==vip),"VIP bypassed normal billing/dossier");
        Check(Row.Current.experience.firstClaim>=0,"Bed claim was not observed");
        Check(!GuestProfiles.GoodRoom(bed),"Outdoor room was treated as luxury");
        Check(GuestProfiles.Mood(vip,0).StageIndex==0,"Basic VIP room did not produce disappointment");
        Check(!GuestProfiles.Mood(vip,1).Active,"Unserved food created a food opinion");
        GuestProfiles.Meal(vip,ThingDefOf.MealFine);Check(GuestProfiles.Mood(vip,1).StageIndex==1,"Fine service not recognized");
        GuestProfiles.Meal(vip,ThingDefOf.MealSimple);Check(GuestProfiles.Mood(vip,1).StageIndex==0,"Ordinary service not recognized");
        var state=Row.Current.experience;int observed=state.lodgingTicks;
        state.lastSample=GuestDossiers.Now-500;GuestProfiles.Sample(Row);Check(state.lodgingTicks==observed+500,"Observed lodging did not accrue");
        GuestProfiles.Sample(Row);Check(state.lodgingTicks==observed+500,"Same-tick observation invented lodging");
        state.lastSample=0;GuestProfiles.Sample(Row);Check(state.lodgingTicks==observed+1000,"Missed observation invented long lodging");
        var toil=vip.GetLord().CurLordToil;vip.needs.mood.CurLevel=0.95f;Call(toil,"GetVisitScore",vip);Check(state.score>=0.6f,"Native personal score was not captured");
        Row.Current.leaving=true;state.lodgingTicks=14999;GuestProfiles.Settle(Row,"departed");Check(Row.profile.completed==0,"Too-short stay granted loyalty");
        // Transition fixtures validate settlement guards without simulating 18h.
        for(int i=0;i<3;i++){
            state.settled=false;state.lodgingTicks=15000;state.score=i==2?0.2f:0.8f;GuestProfiles.Settle(Row,"departed");
            GuestProfiles.Settle(Row,"departed");Check(Row.profile.completed==i+1,"Settlement duplicated a completed stay");
        }
        Row.Current.leaving=false;
        Check(Row.profile.Regular&&Row.profile.good==2&&GuestProfiles.Mood(vip,2).Active,"Regular status did not follow three stays/two reviews");
        foreach(string reason in new[]{"changed","died","lost"}){state.settled=false;GuestProfiles.Settle(Row,reason);Check(Row.profile.completed==3,"Nondeparture granted loyalty");}
        LuxuryAndReturns();
        state.settled=false;state.lodgingTicks=733;state.lastSample=GuestDossiers.Now;state.nextBedCheck=GuestDossiers.Now+500;state.meal=1;state.score=0.72f;
        Row.profile.arrivalPrepared=true;desk.vipChance=5;desk.preferRegulars=false;
        saved=true;GameDataSaveLoader.SaveGame("TMM-Vip");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Vip")),"VIP save not created");Finish(false);
    }
    private void LuxuryAndReturns()
    {
        var rect=new CellRect(45,41,9,9);
        foreach(var c in rect){map.terrainGrid.SetTerrain(c,DefDatabase<TerrainDef>.GetNamed("GoldTile"));map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);}
        foreach(var c in rect.EdgeCells){var wall=ThingMaker.MakeThing(ThingDefOf.Wall,ThingDefOf.Gold);wall.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(wall,c,map);}
        var opening=new IntVec3(45,0,44);opening.GetEdifice(map).Destroy();var door=ThingMaker.MakeThing(ThingDefOf.Door,ThingDefOf.Gold);door.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(door,opening,map);
        var art=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("SculptureGrand"),ThingDefOf.Gold);art.TryGetComp<CompQuality>().SetQuality(QualityCategory.Legendary,ArtGenerationContext.Colony);GenSpawn.Spawn(art,new IntVec3(51,0,46),map);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        Check(GuestProfiles.GoodRoom(bed),"Actual gold private room did not reach VIP standard: "+bed.GetRoom().GetStat(RoomStatDefOf.Impressiveness));
        Check(GuestProfiles.Mood(vip,0).StageIndex==1,"Good private room did not improve VIP opinion");
        var util=AccessTools.TypeByName("Hospitality.Utilities.BedUtility");var scoreMethod=AccessTools.GetDeclaredMethods(util).Single(m=>m.Name=="CalculateBedValue"&&m.GetParameters().Length==3);
        float premium=(float)scoreMethod.Invoke(null,new object[]{bed,vip,1000});Row.profile.vip=false;float normal=(float)scoreMethod.Invoke(null,new object[]{bed,vip,1000});Row.profile.vip=true;
        Check(premium>=normal+249,"VIP private-room score preference not applied");
        var dataType=AccessTools.TypeByName("Hospital.PatientData");var kind=AccessTools.TypeByName("Hospital.PatientType");
        var data=Activator.CreateInstance(dataType,new object[]{GuestDossiers.Now,vip.MarketValue,0.5f,Enum.ToObject(kind,1)});Clinic.Patients(map)[vip]=data;
        Check(!GuestProfiles.Mood(vip,0).Active&&!GuestProfiles.Mood(vip,1).Active&&!GuestProfiles.Mood(vip,2).Active,"Clinical care did not suppress hotel demands");
        int prior=Row.Current.experience.lodgingTicks;Row.Current.experience.lastSample=GuestDossiers.Now-500;GuestProfiles.Sample(Row);Check(Row.Current.experience.lodgingTicks==prior,"Clinical time counted as hotel lodging");Clinic.Patients(map).Remove(vip);
        GuestProfiles.Sample(Row);Check(Row.Current.experience.lodgingTicks==prior,"Recovery credited unobserved clinical interval");
        var regular=GuestProfiles.Row(ordinary);regular.profile.completed=3;regular.profile.good=2;
        ordinary.GetLord().Notify_PawnLost(ordinary,PawnLostCondition.Undefined);ordinary.DeSpawn();GuestDossiers.Book.Synchronize();Find.WorldPawns.PassToWorld(ordinary,PawnDiscardDecideMode.KeepForever);
        var parms=new IncidentParms{target=map,faction=ordinary.Faction};desk.preferRegulars=true;
        var candidate=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,ordinary.Faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false));
        var result=GuestProfiles.SelectRegular(new[]{candidate},parms,true).ToList();Check(result.Count==1&&result[0]==ordinary,"Eligible regular was not preferred in existing faction group");
        GuestDossiers.Book.SetBlocked(regular,true,"test return filter");
        var another=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,ordinary.Faction,forceGenerateNewPawn:true,canGeneratePawnRelations:false));
        result=GuestProfiles.SelectRegular(new[]{another},parms,true).ToList();Check(result[0]==another,"Blacklist was bypassed by regular preference");Find.WorldPawns.PassToWorld(another,PawnDiscardDecideMode.Discard);
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-vip-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-vip-probe")))return;
        try{
            if(reload){
                Check(saved&&vip!=null&&desk!=null,"Saved VIP fixture missing");
                var opening=map.GetComponent<ComplexOpeningVisit>();
                Check(opening!=null&&opening.scheduledTick==-1&&opening.invitedFaction==null,"Old established hotel acquired an opening invitation");
                var native=map.components.First(c=>c.GetType().FullName=="Hospitality.Hospitality_MapComponent");var queue=(IncidentQueue)AccessTools.Field(native.GetType(),"incidentQueue").GetValue(native);int queued=queue.Count;
                opening.TryPlanOpeningVisit();Check(opening.completed&&opening.scheduledTick==-1&&queue.Count==queued,"Existing guests/history caused an extra opening visit");
                Check(Row.profile.vip&&Row.profile.evaluated&&Row.profile.arrivalPrepared&&Row.profile.grantAdded==grant,"VIP identity/grant guard lost");
                Check(Row.profile.Regular&&Row.profile.completed==3&&Row.profile.good==2,"Lifetime loyalty lost");
                Check(desk.vipChance==5&&!desk.preferRegulars,"Reception profile options lost");
                var s=Row.Current.experience;Check(loadedFieldsCorrect,"Active stay fields lost before simulation resumed");
                Check(s.meal==1&&s.firstClaim>=0&&s.nextBedCheck>0,"Active stay changed unexpectedly after simulation resumed");
                int before=HotelBillingHooks.Money(vip);GuestProfiles.Prepare(vip);Check(HotelBillingHooks.Money(vip)==before,"Reload reissued VIP grant");
                Check(GuestProfiles.Mood(vip,2).Active,"Regular thought not restored");
                Check(!Row.profile.tripPrepared&&Row.profile.tripTarget==0&&Row.profile.tripAdded==0,"Pre-budget stay lost zero defaults");
                // Isolate the Current guard on this real old active stay; restore its VIP identity immediately.
                int oldMoney=HotelBillingHooks.Money(vip);bool oldVip=Row.profile.vip;
                try {Row.profile.vip=false;Check(GuestTripBudget.Eligible(vip),"Old active fixture otherwise ineligible");GuestTripBudget.PrepareTrip(vip);
                    Check(HotelBillingHooks.Money(vip)==oldMoney&&!Row.profile.tripPrepared,"Old active stay received a new trip budget");}
                finally {Row.profile.vip=oldVip;}
                if(Environment.GetCommandLineArgs().Contains("-tmm-vip-migration")){
                    var legacy=GuestProfiles.Row(ordinary);Check(legacy!=null&&!legacy.profile.evaluated&&!legacy.profile.vip&&!legacy.profile.Regular,"Missing legacy profile acquired fabricated status");
                    var old=legacy.visits.Last().experience;Check(old.firstClaim==-1&&old.lastSample==-1&&old.meal==-1&&old.score==-1&&!old.observingLodging,"Missing legacy experience acquired past lodging or ratings");
                    Check(legacy.hotel==0,"Migration invented historical income");
                }
                Finish(true);return;
            }
            if(stage==0){Setup();return;}
            if(GuestDossiers.Now-start<501)return;
            if(HotelBillingHooks.Bed(vip)==null&&GuestDossiers.Now-start<1500)return;
            Experience();
        }catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    public override void MapComponentOnGUI()
    {
        if(uiReported||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-vip-probe"))return;
        uiReported=true;string error=null;try{var dossier=new Dialog_GuestDossiers(desk);AccessTools.Field(typeof(Dialog_GuestDossiers),"selected").SetValue(dossier,id);dossier.DoWindowContents(new Rect(10,10,1000,710));var menu=new Dialog_ComplexServices(desk){page=1};menu.DoWindowContents(new Rect(10,10,740,720));}catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"VipUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"VipReloadProbe.json":"VipProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
}
