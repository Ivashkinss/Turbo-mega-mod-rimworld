using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public static class RecruitReloadLaunch
{
    static RecruitReloadLaunch() { if (Environment.GetCommandLineArgs().Contains("-tmm-recruit-reload")) LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Recruit")); }
}
public sealed class RecruitProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Pawn worker, pendingPawn;
    private Quest pendingQuest;
    private int checks, firstTick;
    private bool reported, uiReported, saved;
    private readonly List<string> errors = new List<string>();
    private ComplexRecruitment Book { get { return Current.Game.GetComponent<ComplexRecruitment>(); } }
    public RecruitProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_References.Look(ref desk,"rpDesk"); Scribe_References.Look(ref worker,"rpWorker");
        Scribe_References.Look(ref pendingPawn,"rpPendingPawn"); Scribe_References.Look(ref pendingQuest,"rpQuest");
        Scribe_Values.Look(ref firstTick,"rpFirstTick"); Scribe_Values.Look(ref saved,"rpSaved");
    }
    private void Check(bool ok, string why) { if (!ok) throw new Exception(why); checks++; }
    private void Wait(Pawn pawn) { var j=JobMaker.MakeJob(JobDefOf.Wait);j.expiryInterval=600000;pawn.jobs.StartJob(j,JobCondition.InterruptForced); }
    private Quest Generate()
    {
        var slate=new Slate();slate.Set("map",map);slate.Set("points",100f);
        return QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed(RecruitOffers.DefName),slate);
    }
    private Pawn Candidate(Quest q) { return q.PartsListForReading.OfType<QuestPart_ComplexRecruitArrival>().Single().pawns.Single(); }
    private ThoughtState Thought(string def) { return DefDatabase<ThoughtDef>.GetNamed(def).Worker.CurrentState(worker); }
    private void Mood(int penalty,int bonus)
    {
        var state=Book.Observe(Book.Entry(worker));
        var wait=Thought("TMM_ComplexRecruitWaiting");var adapt=Thought("TMM_ComplexRecruitAdaptation");
        Check(state.waitingPenalty==penalty&&state.adaptationBonus==bonus,"Wrong recruit state "+state.waitingPenalty+"/"+state.adaptationBonus);
        Check(wait.Active==(penalty<0)&&adapt.Active==(bonus>0),"Native thought activation mismatch");
        if(adapt.Active)Check(DefDatabase<ThoughtDef>.GetNamed("TMM_ComplexRecruitAdaptation").stages[adapt.StageIndex].baseMoodEffect==bonus,"Wrong native adaptation stage");
    }
    private void IndependentCases()
    {
        string root=LoadedModManager.RunningModsListForReading.Single(m=>m.PackageId=="colonyproject.integrated").RootDir;
        string lastCase=null;int previous=-1;
        foreach(var line in File.ReadAllLines(Path.Combine(root,"Tests/RecruitLifecycleCases.csv")).Skip(1)) {
            if(string.IsNullOrWhiteSpace(line))continue;var row=line.Split(',');
            if(lastCase!=row[0]){lastCase=row[0];previous=int.Parse(row[9]);}
            bool joined=bool.Parse(row[2]),eligible=bool.Parse(row[3]);
            bool assigned=bool.Parse(row[4])&&row[5]!="None"&&int.Parse(row[6])>0&&row[7]!="ColonyOnly"&&!bool.Parse(row[8]);
            var state=RecruitmentProgress.Evaluate(joined,eligible,assigned,int.Parse(row[10]),previous);
            Check(state.waitingPenalty==int.Parse(row[11]),"Independent waiting case "+row[0]+"/"+row[1]);
            Check(state.adaptationBonus==int.Parse(row[12]),"Independent bonus case "+row[0]+"/"+row[1]);
            Check(state.firstAssignedTick==int.Parse(row[13]),"Independent start case "+row[0]+"/"+row[1]);
            previous=state.firstAssignedTick;
        }
    }
    private void Setup()
    {
        foreach(var p in map.mapPawns.FreeColonistsSpawned)Wait(p);
        Area_Allowed area;map.areaManager.TryMakeNewAllowed(out area);
        foreach(var c in new CellRect(40,40,10,10)) {
            foreach(var t in c.GetThingList(map).ToArray())if(t is Building||t is Plant){if(t.def.destroyable)t.Destroy();else t.DeSpawn();}
            map.terrainGrid.SetTerrain(c,TerrainDefOf.Soil);area[c]=true;
        }
        desk=(Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk"));
        desk.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(desk,new IntVec3(44,0,44),map);desk.area=area;
        Check(RecruitOffers.CanOffer(map),"Active complex did not allow offer");
        var def=DefDatabase<QuestScriptDef>.GetNamed(RecruitOffers.DefName);
        Check(!def.autoAccept&&!def.defaultHidden&&def.expireDaysRange.min==3&&def.expireDaysRange.max==3,"Offer is hidden/automatic or has wrong expiry");
        var incident=DefDatabase<IncidentDef>.GetNamed("TMM_ComplexRecruitOffer").Worker;
        Check(incident.TryExecute(new IncidentParms{target=map,points=100}),"Real recruitment incident did not fire");
        var rejected=Find.QuestManager.QuestsListForReading.Last(q=>q.root==def);var rejectedPawn=Candidate(rejected);
        Check(rejected.State==QuestState.NotYetAccepted&&!rejectedPawn.Spawned&&rejectedPawn.Faction!=Faction.OfPlayer,"Candidate joined before acceptance");
        Check(rejected.description.ToString().Contains("50")&&rejected.description.ToString().Contains("15")&&!rejected.description.ToString().Contains("\\n"),"Offer omitted mood terms or has escaped paragraphs");
        Check(!RecruitOffers.CanOffer(map),"Concurrent pending offers not blocked");
        var requirement=rejected.PartsListForReading.OfType<QuestPart_ComplexRecruitRequirement>().Single();
        var arrival=rejected.PartsListForReading.OfType<QuestPart_ComplexRecruitArrival>().Single();
        var originalMap=arrival.mapParent;arrival.mapParent=null;
        Check(!requirement.CanAccept().Accepted,"Offer accepted after its target map disappeared");
        arrival.mapParent=originalMap;
        Check(requirement.CanAccept().Accepted,"Valid offer rejected after target restoration");
        rejected.End(QuestEndOutcome.Unknown,false);
        Check(!rejectedPawn.Spawned&&rejectedPawn.Faction!=Faction.OfPlayer&&Book.Entry(rejectedPawn)==null,"Rejected candidate joined or enrolled");
        var expired=Generate();var expiredPawn=Candidate(expired);expired.acceptanceExpireTick=Find.TickManager.TicksGame-1;expired.QuestTick();
        Check(expired.State!=QuestState.NotYetAccepted&&!expiredPawn.Spawned&&expiredPawn.Faction!=Faction.OfPlayer,"Expired offer joined or did not end");
        var accepted=Generate();worker=Candidate(accepted);int count=map.mapPawns.FreeColonistsSpawnedCount;
        accepted.Accept(null);
        Check(accepted.State==QuestState.EndedSuccess&&worker.Spawned&&worker.Map==map&&worker.Faction==Faction.OfPlayer&&!worker.IsSlave,"Accepted quest did not join exactly one free worker");
        Wait(worker);Check(map.mapPawns.FreeColonistsSpawnedCount==count+1&&Book.Entry(worker)!=null,"Arrival missing/duplicated registration");
        accepted.Accept(null);Check(map.mapPawns.FreeColonistsSpawnedCount==count+1&&Book.recruits.Count==1,"Repeated acceptance duplicated worker");
        Mood(-50,0);
        var roles=Enum.GetValues(typeof(ComplexRole)).Cast<ComplexRole>().Where(r=>r!=ComplexRole.None&&!worker.WorkTypeIsDisabled(Building_ComplexDesk.Work(r))).ToArray();
        Check(roles.Length>0&&desk.SetRole(worker,roles[0]),"No suitable ordinary role");Mood(0,15);
        firstTick=Book.Entry(worker).firstAssignedTick;
        desk.SetCompany(worker,false);Mood(0,15);Check(Book.Entry(worker).firstAssignedTick==firstTick,"Optional company changed employment start");
        var employee=desk.Employee(worker);employee.mode=ComplexWorkMode.ColonyOnly;ComplexRecruitment.Notify(worker);Mood(-50,0);
        employee.mode=ComplexWorkMode.Both;ComplexRecruitment.Notify(worker);Mood(0,15);
        desk.SetRole(worker,ComplexRole.None);Mood(-50,0);desk.SetRole(worker,roles.Last());Mood(0,15);
        Check(Book.Entry(worker).firstAssignedTick==firstTick,"Returning/changing role restarted adaptation");
        var work=Building_ComplexDesk.Work(employee.role);worker.workSettings.SetPriority(work,0);Mood(-50,0);worker.workSettings.SetPriority(work,1);Mood(0,15);
        IndependentCases();
        pendingQuest=Generate();pendingPawn=Candidate(pendingQuest);
        Check(!pendingPawn.Spawned&&pendingPawn.Faction!=Faction.OfPlayer,"Second pending candidate joined early");
        saved=true;GameDataSaveLoader.SaveGame("TMM-Recruit");Finish(false);
    }
    private void Reload()
    {
        Check(saved&&desk!=null&&worker!=null&&pendingQuest!=null&&pendingPawn!=null,"Recruit save references missing");
        Check(Book.Entry(worker)!=null&&Book.Entry(worker).firstAssignedTick==firstTick,"Work start lost/reset on load");Mood(0,15);
        Check(pendingQuest.State==QuestState.NotYetAccepted&&!pendingPawn.Spawned&&pendingPawn.Faction!=Faction.OfPlayer,"Pending offer changed during load");
        Check(Candidate(pendingQuest)==pendingPawn,"Pending pawn reference duplicated on load");
        var cell=worker.Position;worker.DeSpawn();var absent=Book.Observe(Book.Entry(worker));
        Check(absent.waitingPenalty==0&&absent.adaptationBonus==15&&Book.Entry(worker).firstAssignedTick==firstTick,"Temporary off-map absence cancelled employment");
        GenSpawn.Spawn(worker,cell,map);Wait(worker);
        int count=map.mapPawns.FreeColonistsSpawnedCount;pendingQuest.Accept(null);
        Check(pendingPawn.Spawned&&pendingPawn.Faction==Faction.OfPlayer&&map.mapPawns.FreeColonistsSpawnedCount==count+1,"Reloaded offer did not join exactly one candidate");
        pendingQuest.Accept(null);Check(map.mapPawns.FreeColonistsSpawnedCount==count+1,"Reloaded repeated accept duplicated pawn");
        Wait(pendingPawn);Check(Book.Entry(pendingPawn)!=null&&Book.Entry(pendingPawn).firstAssignedTick==-1,"Second worker was not enrolled as waiting");
        desk.Destroy();Mood(-50,0);Check(!RecruitOffers.CanOffer(map),"Destroyed reception still offers recruitment");
        Finish(true);
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-recruit-reload");
        if(reported||Find.CurrentMap!=map||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-recruit-probe")))return;
        try{if(reload)Reload();else Setup();}catch(Exception ex){errors.Add(ex.ToString());Finish(reload);}
    }
    private static string Escape(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Finish(bool reload){reported=true;File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"RecruitReloadProbe.json":"RecruitProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");}
    public override void MapComponentOnGUI()
    {
        if(uiReported||!reported||desk==null||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-recruit-probe"))return;
        uiReported=true;bool ok=true;string error=null;
        try{new Dialog_ComplexEmployees(desk).DoWindowContents(new Rect(10,10,950,570));
            var window=(MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow;window.PreOpen();window.Select(pendingQuest);window.DoWindowContents(new Rect(10,10,1100,700));}
        catch(Exception ex){ok=false;error=ex.ToString();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"RecruitUiProbe.json"),"{\"passed\":"+(ok?"true":"false")+",\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
}
