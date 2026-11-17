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

[StaticConstructorOnStartup]
public static class RansomPolicyLaunch
{
    // Block only placement. Production creates, owns, saves and retries the real coins.
    public static bool block=true;
    static RansomPolicyLaunch(){
        var args=Environment.GetCommandLineArgs();if(!args.Any(a=>a=="-tmm-ransom-policy-probe"||a=="-tmm-ransom-policy-reload"))return;
        var h=new Harmony("tmm.test.ransom.policy.placement");
        foreach(var m in typeof(GenPlace).GetMethods().Where(m=>m.Name=="TryPlaceThing"&&m.ReturnType==typeof(bool)&&m.GetParameters()[0].ParameterType==typeof(Thing)))h.Patch(m,prefix:new HarmonyMethod(typeof(RansomPolicyLaunch),"Place"));
        if(args.Contains("-tmm-ransom-policy-reload"))LongEventHandler.ExecuteWhenFinished(()=>GameDataSaveLoader.LoadGame("TMM-FactionRansomPolicy"));
    }
    private static bool Place(Thing __0,ref bool __result){if(!block||__0.def!=ThingDefOf.Silver)return true;__result=false;return false;}
}
public sealed class FactionRansomPolicyProbe:MapComponent
{
    private Building_ComplexDesk desk;
    private Pawn accepted,refused,unoffered;
    private Faction first,second,third;
    private int floorBefore,checks,frames;
    private bool done,ui,saved;
    private string error;
    private List<int> cashIds=new List<int>();
    private Dialog_FactionRansomPolicy window;
    private FactionRansoms Book{get{return Current.Game.GetComponent<FactionRansoms>();}}
    private int Floor{get{return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount);}}
    public FactionRansomPolicyProbe(Map map):base(map){}
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    private static void Wait(Pawn pawn){var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;pawn.jobs.StartJob(j,JobCondition.InterruptForced);}
    private Building Build(string name,IntVec3 cell){var d=DefDatabase<ThingDef>.GetNamed(name);var b=(Building)ThingMaker.MakeThing(d,d.MadeFromStuff?ThingDefOf.Steel:null);b.SetFaction(Faction.OfPlayer);return (Building)GenSpawn.Spawn(b,cell,map);}
    private Pawn Slave(Faction home,IntVec3 cell){var p=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,home,forceGenerateNewPawn:true,canGeneratePawnRelations:false,forcedXenotype:XenotypeDefOf.Baseliner,fixedBiologicalAge:30,allowDead:false,allowDowned:false,allowAddictions:false));GenSpawn.Spawn(p,cell,map);p.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);foreach(var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)p.workSettings.SetPriority(w,0);p.inventory.innerContainer.ClearAndDestroyContents();p.needs.food.CurLevelPercentage=1;p.needs.rest.CurLevelPercentage=1;Wait(p);Check(p.IsSlaveOfColony&&p.HomeFaction==home,"Native slave lost original faction");return p;}
    private void RandomState(Pawn pawn,int chance){
        Book.SetOverride(first,chance);Rand.PushState(54321);float before,after;try{before=Rand.Value;FactionRansoms.WillFactionAccept(pawn);after=Rand.Value;}finally{Rand.PopState();}
        Rand.PushState(54321);try{Check(before==Rand.Value&&after==Rand.Value,"Outer RNG changed at "+chance+"%");}finally{Rand.PopState();}
    }
    private void Fresh(){
        map.fogGrid.ClearAllFog();foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        foreach(var c in new CellRect(35,35,30,26)){foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant||t is Filth){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);map.roofGrid.SetRoof(c,null);}
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);foreach(var c in new CellRect(36,36,24,21))area[c]=true;
        desk=(Building_ComplexDesk)Build("TMM_ComplexDesk",new IntVec3(40,0,37));desk.area=area;desk.vipChance=0;desk.ApplyServiceAreas();
        Check(Book.defaultChance==50&&Book.overrides.Count==0,"New world policy defaults changed");
        Dialog_WorkBuyout.Show(desk);var menu=Find.WindowStack.Windows.OfType<FloatMenu>().LastOrDefault();
        Check(menu!=null&&((List<FloatMenuOption>)AccessTools.Field(typeof(FloatMenu),"options").GetValue(menu)).Any(o=>o.Label=="TMM.RansomPolicy.Title".Translate().ToString()),"Policy settings inaccessible without workers");menu.Close();Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
        first=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.defeated&&!f.HostileTo(Faction.OfPlayer));
        second=FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(first.def));Find.FactionManager.Add(second);
        third=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f!=first&&f!=second);
        Check(first!=second&&first.def==second.def&&Find.FactionManager.AllFactionsListForReading.Contains(second),"Same-Def live faction fixture not real");
        first.Name="Фракция с очень длинным названием для проверки обрезки строки и отсутствия наложений на кнопки вероятности";
        Book.SetDefaultChance(150);Check(Book.GetChance(first)==100,"Default upper bound not clamped");Book.SetDefaultChance(-20);Check(Book.GetChance(first)==0,"Default lower bound not clamped");Book.SetDefaultChance(50);
        Book.SetOverride(first,100);Book.SetOverride(second,0);Check(Book.GetChance(first)==100&&Book.GetChance(second)==0&&Book.defaultChance==50,"Same Def factions share an override");
        Book.RemoveOverride(first);Check(Book.GetChance(first)==50&&Book.GetChance(second)==0,"Reset changed unrelated faction");Book.SetOverride(first,100);
        Book.SetOverride(Faction.OfPlayer,75);Book.SetOverride(null,75);Check(Book.overrides.Count==2,"Invalid faction override accepted");
        accepted=Slave(first,new IntVec3(41,0,43));refused=Slave(second,new IntVec3(44,0,46));unoffered=Slave(first,new IntVec3(48,0,43));floorBefore=Floor;
        var a=FactionRansoms.Offer(accepted,1200);Check(a!=null&&a.offered&&a.agreed&&a.originalFaction==first&&a.targetAmount==1200,"100% offer was not accepted with correct terms");
        cashIds=a.GetDirectlyHeldThings().Select(t=>t.thingIDNumber).OrderBy(i=>i).ToList();
        Check(a.EscrowCoins==1200&&a.placedAmount==0&&a.TotalFundedCoins==1200&&cashIds.Count>0&&Floor==floorBefore&&Book.totalRansomIncome==0,"Blocked real escrow leaked money or recorded phantom income");
        Book.SetDefaultChance(0);Book.SetOverride(first,0);Check(FactionRansoms.Offer(accepted,9999)==a&&a.agreed&&a.targetAmount==1200&&a.EscrowCoins==1200,"Accepted offer changed after policy edit");
        var b=FactionRansoms.Offer(refused,1600);Check(b!=null&&b.offered&&!b.agreed&&b.originalFaction==second&&b.targetAmount==1600,"0% offer not refused");
        Check(b.TotalFundedCoins==0&&Floor==floorBefore&&Book.totalRansomIncome==0,"Refusal generated coins");
        Book.SetOverride(second,100);Check(FactionRansoms.Offer(refused,9999)==b&&!b.agreed&&b.targetAmount==1600&&b.TotalFundedCoins==0,"Policy edit rerolled refusal");
        Book.SetOverride(first,50);int seed=GenText.StableStringHash("TMM:faction-ransom:"+Find.World.info.seedString+":"+unoffered.GetUniqueLoadID());bool expected;Rand.PushState(seed);try{expected=Rand.Chance(0.5f);}finally{Rand.PopState();}
        Check(FactionRansoms.WillFactionAccept(unoffered)==expected,"Default 50% no longer matches original seeded algorithm");
        RandomState(unoffered,0);RandomState(unoffered,50);RandomState(unoffered,100);
        Book.SetOverride(first,-10);Check(Book.GetChance(first)==0,"Override lower bound not clamped");Book.SetOverride(first,150);Check(Book.GetChance(first)==100,"Override upper bound not clamped");
        Book.SetDefaultChance(65);Book.SetOverride(first,85);Book.SetOverride(second,15);
        Book.overrides.Add(new FactionRansomOverride(third,50){chance=250});Book.overrides.Add(new FactionRansomOverride(null,50));Book.overrides.Add(new FactionRansomOverride(first,40));Book.overrides.Add(null);
        saved=true;GameDataSaveLoader.SaveGame("TMM-FactionRansomPolicy");Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-FactionRansomPolicy")),"Actual policy save missing");Finish(false);
    }
    private void Reload(){
        Check(saved&&first!=null&&second!=null&&third!=null&&first!=second&&first.def==second.def,"Faction references lost on real load");
        Check(Book.defaultChance==65&&Book.overrides.Count==3,"Policy defaults or duplicate/null cleanup failed actual load");
        Check(Book.GetChance(first)==85&&Book.GetChance(second)==15&&Book.overrides.Single(o=>o.faction==third).chance==100,"Faction overrides or raw out-of-bounds rule failed actual load");
        var a=Book.Entry(accepted);var b=Book.Entry(refused);
        Check(a!=null&&a.agreed&&a.targetAmount==1200&&a.originalFaction==first&&a.EscrowCoins==1200&&a.placedAmount==0,"Accepted escrow/terms changed on load");
        Check(a.GetDirectlyHeldThings().Select(t=>t.thingIDNumber).OrderBy(i=>i).SequenceEqual(cashIds),"Loaded escrow does not contain the original physical coin stacks");
        Check(b!=null&&!b.agreed&&b.targetAmount==1600&&b.originalFaction==second&&b.TotalFundedCoins==0,"Refused offer changed on load");
        Check(Floor==floorBefore&&Book.totalRansomIncome==0,"Loading created receipts");
        Book.SetOverride(first,0);Book.SetOverride(second,100);Check(FactionRansoms.Offer(accepted,5)==a&&FactionRansoms.Offer(refused,5)==b&&a.agreed&&!b.agreed&&a.EscrowCoins==1200,"Loaded offers were rerolled or paid twice");
        RansomPolicyLaunch.block=false;Book.ProcessPendingEscrow(map);Book.ProcessPendingEscrow(map);
        Check(a.IsFullyPaid&&a.EscrowCoins==0&&Floor==floorBefore+1200&&Book.totalRansomIncome==1200,"Real post-load payment lost or duplicated coins");
        Check(map.GetComponent<HousekeepingLedger>().received==0&&desk.hotelIncome==0,"Faction money became room revenue");
        Check(FactionRansoms.Offer(accepted,1)==a&&Floor==floorBefore+1200&&Book.totalRansomIncome==1200,"Repeated paid offer created money");
        int goodwill=first.GoodwillWith(Faction.OfPlayer);Check(FactionRansoms.Deceive(accepted)&&a.deceived&&first.GoodwillWith(Faction.OfPlayer)==goodwill,"Policy changes bypassed deception history or goodwill rules");
        Book.SetDefaultChance(50);Book.RemoveOverride(first);Book.RemoveOverride(second);Book.RemoveOverride(third);Check(a.agreed&&a.deceived&&!b.agreed&&Book.GetChance(first)==50&&Book.GetChance(second)==50,"Reset erased completed decisions");Finish(true);
    }
    public override void ExposeData(){base.ExposeData();Scribe_References.Look(ref desk,"ppDesk");Scribe_References.Look(ref accepted,"ppAccepted");Scribe_References.Look(ref refused,"ppRefused");Scribe_References.Look(ref unoffered,"ppUnoffered");Scribe_References.Look(ref first,"ppFirst");Scribe_References.Look(ref second,"ppSecond");Scribe_References.Look(ref third,"ppThird");Scribe_Values.Look(ref floorBefore,"ppFloor");Scribe_Values.Look(ref saved,"ppSaved");Scribe_Collections.Look(ref cashIds,"ppCashIds",LookMode.Value);}
    public override void MapComponentUpdate(){bool reload=Environment.GetCommandLineArgs().Contains("-tmm-ransom-policy-reload");if(done||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-ransom-policy-probe")))return;try{if(reload)Reload();else Fresh();}catch(Exception e){error=e.ToString();Finish(reload);}}
    private void Finish(bool reload){done=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"FactionRansomPolicyReloadProbe.json":"FactionRansomPolicyProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"checks\":"+checks+",\"errors\":"+(error==null?"[]":"["+Q(error)+"]")+"}");}
    public override void MapComponentOnGUI(){
        if(!done||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-ransom-policy-probe"))return;
        if(!ui){ui=true;string failure=null;try{var font=Text.Font;var anchor=Text.Anchor;var color=GUI.color;bool wrap=Text.WordWrap;window=new Dialog_FactionRansomPolicy();window.DoWindowContents(new Rect(0,0,684,666));if(Text.Font!=font||Text.Anchor!=anchor||GUI.color!=color||Text.WordWrap!=wrap)throw new Exception("Policy window leaked GUI state");new Dialog_WorkBuyout(unoffered).DoWindowContents(new Rect(0,0,660,610));}catch(Exception e){failure=e.ToString();}
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"FactionRansomPolicyUiProbe.json"),"{\"passed\":"+(failure==null?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(failure==null?"null":Q(failure))+"}");return;}
        frames++;if(frames==2)Find.WindowStack.Add(window);if(frames==12)AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"),"CaptureScreenshot",new[]{typeof(string)}).Invoke(null,new object[]{Path.Combine(GenFilePaths.SaveDataFolderPath,"FactionRansomPolicy.png")});if(frames==24){window.Close();Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;}
    }
}
