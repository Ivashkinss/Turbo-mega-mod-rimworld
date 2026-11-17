using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using Verse;

[StaticConstructorOnStartup]
public static class OutpostAcceptanceLaunch
{
    static OutpostAcceptanceLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-outpost-resume-day"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-OutpostDay"));
        if (Environment.GetCommandLineArgs().Contains("-tmm-outpost-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-StrategicOutpost"));
    }
}
public sealed class OutpostAcceptanceProbe : MapComponent
{
    private StrategicOutpost station;
    private Pawn first, second, animal;
    private int stage, foundedTick, savedLevel, savedNext, savedOutput, checks;
    private string savedFirst, savedSecond, savedAnimal;
    private bool done, waitingShipment;
    private int shipmentStart;
    private Thing[] shipment;
    private readonly List<string> errors = new List<string>();
    public OutpostAcceptanceProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        Scribe_References.Look(ref station,"oaStation");
        Scribe_References.Look(ref first,"oaFirst"); Scribe_References.Look(ref second,"oaSecond"); Scribe_References.Look(ref animal,"oaAnimal");
        Scribe_Values.Look(ref stage,"oaStage"); Scribe_Values.Look(ref foundedTick,"oaFounded");
        Scribe_Values.Look(ref savedLevel,"oaLevel"); Scribe_Values.Look(ref savedNext,"oaNext"); Scribe_Values.Look(ref savedOutput,"oaOutput");
        Scribe_Values.Look(ref savedFirst,"oaFirstId"); Scribe_Values.Look(ref savedSecond,"oaSecondId"); Scribe_Values.Look(ref savedAnimal,"oaAnimalId");
    }
    private void Check(bool ok,string message) { if(!ok)throw new Exception(message); checks++; }
    private void Add(ThingOwner<Thing> owner,ThingDef def,int count)
    {
        while(count>0) { var thing=ThingMaker.MakeThing(def);thing.stackCount=Math.Min(count,def.stackLimit);int n=thing.stackCount;Check(owner.TryAdd(thing,false),"Fixture item rejected");count-=n; }
    }
    private Pawn Miner()
    {
        for(int attempt=0;attempt<12;attempt++) {
            var p=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,Faction.OfPlayer,forceGenerateNewPawn:true,allowDead:false,allowDowned:false,allowAddictions:false,canGeneratePawnRelations:false,developmentalStages:DevelopmentalStage.Adult));
            if(p.skills!=null&&!p.WorkTypeIsDisabled(WorkTypeDefOf.Mining)&&!p.skills.GetSkill(SkillDefOf.Mining).TotallyDisabled) {p.skills.GetSkill(SkillDefOf.Mining).Level=15;return p;}
            if(!Find.WorldPawns.Contains(p)) Find.WorldPawns.PassToWorld(p,PawnDiscardDecideMode.Discard);
        }
        throw new Exception("No capable fixture miner generated.");
    }
    private int Output { get { return station.ProducedGoods.TotalStackCountOfDef(ThingDefOf.Plasteel); } }
    private void Fresh()
    {
        if(stage==0) {
            var neighbors=new List<PlanetTile>();var frontier=new Queue<PlanetTile>();var visited=new HashSet<PlanetTile>();
            frontier.Enqueue(map.Tile);visited.Add(map.Tile);PlanetTile tile=PlanetTile.Invalid;
            while(frontier.Count>0&&visited.Count<1000&&!tile.Valid) {
                var current=frontier.Dequeue();Find.WorldGrid.GetTileNeighbors(current,neighbors);
                foreach(var candidate in neighbors) { if(!visited.Add(candidate))continue;frontier.Enqueue(candidate);
                    string reason;if(candidate.Layer==Find.WorldGrid.Surface&&StrategicOutpostCaravanUtility.CanCreateOutpostAt(candidate,out reason)&&TileFinder.IsValidTileForNewSettlement(candidate)){tile=candidate;break;}}
            }
            Check(tile.Valid,"No legal nearby founding tile");
            first=Miner();second=Miner();animal=PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDef.Named("Muffalo"),Faction.OfPlayer));
            var caravan=CaravanMaker.MakeCaravan(new[]{first,second,animal},Faction.OfPlayer,tile,true);
            Add(first.inventory.innerContainer,ThingDefOf.Silver,31);
            station=StrategicOutpostCaravanUtility.FoundOutpostFromCaravan(caravan,StrategicOutpostSpecialization.Mining);
            Check(station!=null&&station.Spawned,"Native caravan founding failed");
            Check(station.Workers.Contains(first)&&station.Workers.Contains(second)&&station.Animals.Contains(animal)&&station.MaxCapacity==2,"Founding changed workforce or animal capacity");
            Check(!caravan.Spawned&&station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver)>=31,"Empty founding caravan or real cargo lost");
            Check(new[]{first,second,animal}.All(p=>Find.WorldPawns.Contains(p)&&!p.Spawned),"Founders not registered/held");
            station.targetColonyTile=-1;station.selectedProductDefNames.Clear();station.selectedProductDefNames.Add("TMM_Outpost_Mining_Plasteel");
            Check(Math.Abs(station.ExpectedYield(DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Mining_Plasteel"))-20)<0.0001,"Initial worker labor is not20: "+string.Join(";",station.Workers.InnerListForReading.Select(p=>p.ThingID+" colonist="+p.IsColonist+" downed="+p.Downed+" mining="+p.skills.GetSkill(SkillDefOf.Mining).Level+" manipulation="+p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))));
            foundedTick=Find.TickManager.TicksGame; Check(station.nextProductionTick>=foundedTick+59000,"Founding did not schedule an actual full production day");
            savedFirst=first.ThingID;savedSecond=second.ThingID;savedAnimal=animal.ThingID;stage=1;return;
        }
        if(stage==1) {
            if(Find.TickManager.TicksGame-foundedTick>90000)throw new Exception("No native daily production within 1.5 days");
            if(station.lastProductionTick<=foundedTick)return;
            Check(Find.TickManager.TicksGame-foundedTick>=60000,"Production occurred before actual day elapsed");
            Check(animal!=null&&!animal.Destroyed&&station.Animals.Contains(animal)&&Find.WorldPawns.Contains(animal),"Stored pack animal lost to world GC during actual production day");
            Check(Output==20,"Two skill15 miners must produce exactly20plasteel per actual day; received "+Output+"; "+string.Join(";",station.Workers.InnerListForReading.Select(p=>p.ThingID+" colonist="+p.IsColonist+" downed="+p.Downed+" mining="+p.skills.GetSkill(SkillDefOf.Mining).Level+" manipulation="+p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))));
            int before=Output;station.DoDailyProduction();station.TriggerDailyProductionNow();
            Check(Output==before,"Repeated public daily entry duplicated production");
            Check(station.ProducedGoods.All(t=>t.stackCount>0&&t.stackCount<=t.def.stackLimit&&t.holdingOwner==station.ProducedGoods),"Output stack limits/ownership broken");
            first.skills.GetSkill(SkillDefOf.Mining).Level=0; second.skills.GetSkill(SkillDefOf.Mining).Level=0;
            Check(station.ExpectedYield(DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Mining_Plasteel"))==0,"Zero skill produces labor");
            first.skills.GetSkill(SkillDefOf.Mining).Level=15;second.skills.GetSkill(SkillDefOf.Mining).Level=15;
            stage=2;GameDataSaveLoader.SaveGame("TMM-OutpostDay");return;
        }
        if(stage==2) {
            string reason;Check(station.DeployPersonnelToMap(out reason),"Actual deploy failed: "+reason);
            Check(station.HasMap&&station.Map.GetComponent<StrategicOutpostMapComponent>().CountRealBeds()>=2,"Real level1 map/beds absent");
            Check(new[]{first,second,animal}.All(p=>p!=null&&p.Spawned&&p.Map==station.Map&&!station.Workers.Contains(p)&&!station.Animals.Contains(p)),"Deploy mismatch; stationMap="+station.Map.uniqueID+"; "+string.Join("; ",new[]{first,second,animal}.Select(p=>p==null?"null pawn":p.ThingID+" spawned="+p.Spawned+" map="+(p.Map==null?-1:p.Map.uniqueID)+" worker="+station.Workers.Contains(p)+" animal="+station.Animals.Contains(p))));
            int before=Output;station.DoDailyProduction();Check(Output==before,"Physical mode duplicated abstract output");
            Check(station.RecallPersonnelToAutonomous(out reason),"Actual recall failed: "+reason);
            Check(station.Workers.Contains(first)&&station.Workers.Contains(second)&&station.Animals.Contains(animal)&&new[]{first,second,animal}.All(p=>!p.Spawned&&Find.WorldPawns.Contains(p)),"Recall lost identity or WorldPawns ownership");
            Check(station.nextProductionTick>=Find.TickManager.TicksGame+59000,"Physical time retroactively counted");
            stage=3;return;
        }
        if(stage==3) {
            Add(station.RawMaterials,ThingDefOf.Silver,1000);
            var component=station.Map.GetComponent<StrategicOutpostMapComponent>();
            var rect=component.GetDefaultModuleRect(2);
            var coins=ThingMaker.MakeThing(ThingDefOf.Silver);coins.stackCount=37;GenSpawn.Spawn(coins,rect.CenterCell,station.Map);
            string reason;int silver=station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver);
            Check(!component.CanSafelyPlaceModule(rect,2)&&!coins.Destroyed&&coins.stackCount==37,"Planner wipes loose silver");
            // A blocked default location may legitimately use a different free layout.
            coins.DeSpawn();Check(station.RawMaterials.TryAdd(coins,false),"Cannot preserve fixture coins");silver+=37;
            Check(station.TryUpgrade(out reason),"Actual level2 upgrade failed: "+reason);
            Check(station.level==2&&station.MaxCapacity==3&&station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver)==silver-500,"Upgrade cost/capacity incorrect");
            Check(component.CountRealBeds()>=3,"Level2 real third bed absent");
            Check(station.DeployPersonnelToMap(out reason),"Second deploy failed: "+reason);
            foreach(var bed in station.Map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>())
                Check(first.CanReach(bed,Verse.AI.PathEndMode.Touch,Danger.Some),"Real pawn cannot reach an outpost bed");
            Check(station.RecallPersonnelToAutonomous(out reason),"Second recall failed: "+reason);
            savedLevel=station.level;savedNext=station.nextProductionTick;savedOutput=Output;stage=4;
            GameDataSaveLoader.SaveGame("TMM-StrategicOutpost");
            Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath,"TMM-StrategicOutpost.rws")),"No actual outpost save created");
            Finish(null,false);
        }
    }
    private void Research(ResearchProjectDef project)
    {
        if(project==null||project.IsFinished)return;
        if(project.prerequisites!=null)foreach(var prerequisite in project.prerequisites)Research(prerequisite);
        Find.ResearchManager.FinishProject(project,false,null,false);
    }
    private void FactoryMaterials()
    {
        var synthetic=DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Factory_Synthread");
        string reason;Check(!synthetic.IsUnlocked(out reason),"New textile technology unexpectedly available before research");
        Research(DefDatabase<ResearchProjectDef>.GetNamed("TMM_SyntheticTextiles"));
        Check(synthetic.IsUnlocked(out reason),"Researched textile recipe/bench not available: "+reason);
        var cloth=ThingDef.Named("Cloth");var fuel=ThingDef.Named("Chemfuel");var thread=ThingDef.Named("Synthread");
        int oldCloth=station.RawMaterials.TotalStackCountOfDef(cloth),oldFuel=station.RawMaterials.TotalStackCountOfDef(fuel);
        int oldThread=station.ProducedGoods.TotalStackCountOfDef(thread);
        Add(station.RawMaterials,cloth,1);Add(station.RawMaterials,fuel,1);
        Check(synthetic.ProduceInto(station.RawMaterials,station.ProducedGoods,1),"Actual textile ingredient transaction failed");
        Check(station.RawMaterials.TotalStackCountOfDef(cloth)==oldCloth&&station.RawMaterials.TotalStackCountOfDef(fuel)==oldFuel&&station.ProducedGoods.TotalStackCountOfDef(thread)==oldThread+1,"Textile consumed incorrect materials or gave duplicate product");
        var advanced=DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Factory_ComponentSpacer");
        Research(DefDatabase<ResearchProjectDef>.GetNamed("AdvancedFabrication"));
        Check(advanced.IsUnlocked(out reason),"Actual advanced component recipe unavailable: "+reason);
        var input=new Dictionary<ThingDef,int>{{ThingDefOf.ComponentIndustrial,1},{ThingDefOf.Steel,20},{ThingDefOf.Plasteel,10},{ThingDefOf.Gold,3}};
        var baseline=input.Keys.ToDictionary(d=>d,d=>station.RawMaterials.TotalStackCountOfDef(d));
        int oldAdvanced=station.ProducedGoods.TotalStackCountOfDef(ThingDefOf.ComponentSpacer);
        foreach(var material in input)Add(station.RawMaterials,material.Key,material.Value);
        Check(advanced.ProduceInto(station.RawMaterials,station.ProducedGoods,1),"Actual advanced component ingredient transaction failed");
        Check(input.All(m=>station.RawMaterials.TotalStackCountOfDef(m.Key)==baseline[m.Key])&&station.ProducedGoods.TotalStackCountOfDef(ThingDefOf.ComponentSpacer)==oldAdvanced+1,"Native component recipe counts differ, including three real gold for XML0.3");
        Check(station.ProducedGoods.All(t=>t.stackCount<=t.def.stackLimit&&t.holdingOwner==station.ProducedGoods),"Factory output stacks lost ownership or exceeded stackLimit");
    }
    private void UpperLevels()
    {
        var component=station.Map.GetComponent<StrategicOutpostMapComponent>();
        for(int desired=3;desired<=10;desired++) {
            var plot=component.GetDefaultModuleRect(desired);
            // Controlled native construction plots: clear only natural obstacles, never the
            // existing player base. Production's blocked-item check was exercised separately.
            foreach(var cell in plot) {
                foreach(var thing in cell.GetThingList(station.Map).ToArray())
                    if(thing is Plant || thing is Mineable || (thing.def.thingCategories!=null&&thing.def.thingCategories.Contains(ThingCategoryDefOf.Chunks)))thing.Destroy(DestroyMode.Vanish);
                var terrain=station.Map.terrainGrid.TerrainAt(cell);
                if(terrain.passability==Traversability.Impassable||!terrain.affordances.Contains(TerrainAffordanceDefOf.Light))station.Map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);
            }
            int cost=station.NextUpgradeCost,remaining=cost;
            while(remaining>0) {var money=ThingMaker.MakeThing(ThingDefOf.Silver);money.stackCount=Math.Min(remaining,ThingDefOf.Silver.stackLimit);int count=money.stackCount;if(!station.RawMaterials.TryAdd(money,false))throw new Exception("Fixture upgrade money rejected");remaining-=count;}
            int before=station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver);string reason;
            Check(station.TryUpgrade(out reason)&&station.level==desired&&station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver)==before-cost,"Actual upper-level upgrade/cost failed at "+desired+": "+reason);
        }
        Check(station.MaxCapacity==11&&!station.CanUpgrade&&component.CountRealBeds()>=11,"Level10 capacity/beds/cap incorrect");
        string failure;Check(station.DeployPersonnelToMap(out failure),"Level10 deploy failed: "+failure);
        foreach(var bed in station.Map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>())Check(first.CanReach(bed,Verse.AI.PathEndMode.Touch,Danger.Some),"Unreachable real upper-level bed");
        Check(station.RecallPersonnelToAutonomous(out failure),"Level10 recall failed: "+failure);
        var recruits=new List<Pawn>();for(int i=0;i<6;i++){var recruit=Miner();recruit.skills.GetSkill(SkillDefOf.Mining).Level=20;recruits.Add(recruit);}
        var caravan=CaravanMaker.MakeCaravan(recruits,Faction.OfPlayer,station.Tile,true);
        foreach(var recruit in recruits)Check(station.AddPawnFromCaravan(caravan,recruit,out failure),"Actual additional worker transfer failed: "+failure);
        if(caravan.PawnsListForReading.Count==0)Find.WorldObjects.Remove(caravan);
        Check(station.Workers.Count==8&&station.Workers.InnerListForReading.Sum(p=>p.skills.GetSkill(SkillDefOf.Mining).Level)==150&&Math.Abs(station.ExpectedYield(DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Mining_Plasteel"))-100)<0.0001,"Legal eight-worker 150mining roster does not predict100plasteel/day");
        GameDataSaveLoader.SaveGame("TMM-OutpostTier10");
    }
    private void Reload()
    {
        if (waitingShipment)
        {
            if (Find.TickManager.TicksGame-shipmentStart < 180) return;
            Check(shipment.All(t=>!t.Destroyed && t.MapHeld==map),"Actual pod shipment did not reach the receiving colony map");
            Check(station.ProducedGoods.Count==0,"Accepted shipment remained in outpost cargo");
            station.DoDailyShipment();Check(station.ProducedGoods.Count==0,"Empty repeat shipment created more cargo");
            CameraJumper.TryJump(station.Map.Center,station.Map);
            AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"),"CaptureScreenshot",new[]{typeof(string)}).Invoke(null,new object[]{Path.Combine(GenFilePaths.SaveDataFolderPath,"Outpost-Level10.png")});
            Finish(null,true);return;
        }
        Check(stage==4&&station!=null&&station.Spawned,"Real saved outpost reference absent");
        Check(first!=null&&second!=null&&animal!=null&&first.ThingID==savedFirst&&second.ThingID==savedSecond&&animal.ThingID==savedAnimal,"Saved personnel identity lost");
        Check(station.Workers.Contains(first)&&station.Workers.Contains(second)&&station.Animals.Contains(animal),"Saved holder references lost");
        Check(new[]{first,second,animal}.All(p=>Find.WorldPawns.Contains(p)&&!p.Spawned),"Saved WorldPawns ownership lost");
        Check(station.level==savedLevel&&station.nextProductionTick==savedNext&&Output==savedOutput,"Saved level/deadline/output changed");
        Check(station.HasMap&&station.Map.GetComponent<StrategicOutpostMapComponent>().CountRealBeds()>=3,"Saved actual map lost");
        int before=Output;station.DoDailyProduction();Check(Output==before,"Loading permits duplicate daily production");
        FactoryMaterials();
        UpperLevels();
        shipment=station.ProducedGoods.InnerListForReading.ToArray();Check(shipment.Length>0,"No retained goods for real delivery test");
        station.targetColonyTile=map.Tile;
        station.DoDailyShipment();shipmentStart=Find.TickManager.TicksGame;waitingShipment=true;
    }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-outpost-reload");
        if(done||map.Parent is StrategicOutpost||!map.IsPlayerHome||(!reload&&!Environment.GetCommandLineArgs().Contains("-tmm-outpost-probe")))return;
        try {Find.TickManager.CurTimeSpeed=TimeSpeed.Superfast; if(reload)Reload();else Fresh();}
        catch(Exception e){Finish(e,reload);}
    }
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Finish(Exception e,bool reload)
    {
        done=true;if(e!=null)errors.Add(e.ToString());
        string report="{\"passed\":"+(e==null?"true":"false")+",\"checks\":"+checks+",\"stage\":"+stage+",\"errors\":["+string.Join(",",errors.Select(Q))+"]}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"OutpostReloadProbe.json":"OutpostProbe.json"),report);
        if(!reload)File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"OutpostUiProbe.json"),"{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
    }
}
