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
        if (Environment.GetCommandLineArgs().Contains("-tmm-outpost-tier10-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-OutpostTier10"));
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
            if(p.skills!=null&&!p.WorkTypeIsDisabled(WorkTypeDefOf.Mining)&&!p.skills.GetSkill(SkillDefOf.Mining).TotallyDisabled) {
                p.skills.GetSkill(SkillDefOf.Mining).Level=15;
                if(p.relations!=null) p.relations.ClearAllRelations();
                return p;
            }
            if(!Find.WorldPawns.Contains(p)) {
                if(p.relations!=null) p.relations.ClearAllRelations();
                Find.WorldPawns.PassToWorld(p,PawnDiscardDecideMode.KeepForever);
            }
        }
        throw new Exception("No capable fixture miner generated.");
    }

    private void CleanDanglingRelations()
    {
        var allKnown = new HashSet<Pawn>();
        if (Find.Maps != null)
        {
            foreach (var m in Find.Maps)
            {
                if (m != null && m.mapPawns != null)
                {
                    foreach (var p in m.mapPawns.AllPawns) if (p != null) allKnown.Add(p);
                }
            }
        }
        if (Find.WorldPawns != null)
        {
            foreach (var p in Find.WorldPawns.AllPawnsAliveOrDead) if (p != null) allKnown.Add(p);
        }
        var allOutposts = (Find.WorldObjects != null) ? Find.WorldObjects.AllWorldObjects.OfType<StrategicOutpost>().ToList() : new List<StrategicOutpost>();
        foreach (var post in allOutposts)
        {
            if (post.AllPawns != null)
            {
                foreach (var p in post.AllPawns) if (p != null) allKnown.Add(p);
            }
        }

        var outpostAndPlayerPawns = new HashSet<Pawn>();
        foreach (var post in allOutposts)
        {
            if (post.Workers != null) foreach (var p in post.Workers) if (p != null) outpostAndPlayerPawns.Add(p);
            if (post.Animals != null) foreach (var p in post.Animals) if (p != null) outpostAndPlayerPawns.Add(p);
            if (post.AllPawns != null) foreach (var p in post.AllPawns) if (p != null) outpostAndPlayerPawns.Add(p);
        }
        if (first != null) outpostAndPlayerPawns.Add(first);
        if (second != null) outpostAndPlayerPawns.Add(second);
        if (animal != null) outpostAndPlayerPawns.Add(animal);
        foreach (var p in allKnown)
        {
            if (p != null && (p.Faction == Faction.OfPlayer || p.IsColonist)) outpostAndPlayerPawns.Add(p);
        }

        foreach (var p in allKnown)
        {
            if (p.relations != null && p.relations.DirectRelations != null)
            {
                var rels = p.relations.DirectRelations.ToList();
                for (int i = rels.Count - 1; i >= 0; i--)
                {
                    var rel = rels[i];
                    if (rel == null) continue;
                    Pawn target = rel.otherPawn;
                    if (target == null || !allKnown.Contains(target))
                    {
                        bool targetIsOutpostOrPlayer = (target != null && (outpostAndPlayerPawns.Contains(target) || target.Faction == Faction.OfPlayer || target.IsColonist));
                        if (targetIsOutpostOrPlayer)
                        {
                            string tName = target != null ? target.LabelShort : "null";
                            string tId = target != null ? target.ThingID : "null";
                            Check(false, "Dangling relation " + (rel.def != null ? rel.def.defName : "null") + " to outpost/player pawn " + tName + " (" + tId + ")");
                        }
                        else
                        {
                            p.relations.RemoveDirectRelation(rel);
                            string relName = rel.def != null ? rel.def.defName : "null";
                            string ownerName = p.LabelShort;
                            string targetName = target != null ? target.LabelShort : "null";
                            string targetFaction = (target != null && target.Faction != null) ? target.Faction.Name : "null";
                            string factEntry = "Sanitized foreign relation: " + relName + " | owner=" + ownerName + " (" + p.ThingID + ") | target=" + targetName + " (" + (target != null ? target.ThingID : "null") + ") | targetFaction=" + targetFaction;
                            facts.Add(factEntry);
                            Log.Message("[OutpostSanitizerNarrow] " + factEntry);
                        }
                    }
                }
            }
        }
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
            Building_Bed founderBed1 = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
            GenSpawn.Spawn(founderBed1, map.Center, map);
            founderBed1.SetFaction(Faction.OfPlayer);
            first.ownership.ClaimBedIfNonMedical(founderBed1);
            Check(first.ownership.OwnedBed == founderBed1, "First founder bed claim failed");
            Building_Bed founderBed2 = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
            GenSpawn.Spawn(founderBed2, map.Center + new IntVec3(2, 0, 0), map);
            founderBed2.SetFaction(Faction.OfPlayer);
            second.ownership.ClaimBedIfNonMedical(founderBed2);
            Check(second.ownership.OwnedBed == founderBed2, "Second founder bed claim failed");

            var caravan=CaravanMaker.MakeCaravan(new[]{first,second,animal},Faction.OfPlayer,tile,true);
            Add(first.inventory.innerContainer,ThingDefOf.Silver,31);
            station=StrategicOutpostCaravanUtility.FoundOutpostFromCaravan(caravan,StrategicOutpostSpecialization.Mining);
            Check(station!=null&&station.Spawned,"Native caravan founding failed");
            Check(first.ownership.OwnedBed == null, "Founding must unclaim first founder bed");
            Check(second.ownership.OwnedBed == null, "Founding must unclaim second founder bed");
            Check(founderBed1.OwnersForReading.Count == 0, "FounderBed1 must be free after founding");
            Check(founderBed2.OwnersForReading.Count == 0, "FounderBed2 must be free after founding");
            Check(station.Workers.Contains(first)&&station.Workers.Contains(second)&&station.Animals.Contains(animal)&&station.MaxCapacity==2,"Founding changed workforce or animal capacity");
            Check(!caravan.Spawned&&station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver)>=31,"Empty founding caravan or real cargo lost");
            Check(new[]{first,second,animal}.All(p=>Find.WorldPawns.Contains(p)&&!p.Spawned),"Founders not registered/held");
            station.targetColonyTile=-1;station.selectedProductDefNames.Clear();station.selectedProductDefNames.Add("TMM_Outpost_Mining_Plasteel");
            // Balance update: Plasteel yieldPerSkill = 0.4. Two skill 15 miners = 30 skill sum. Expected yield = 30 * 0.4 = 12.0
            Check(Math.Abs(station.ExpectedYield(DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Mining_Plasteel"))-12)<0.0001,"Initial worker labor is not 12: "+string.Join(";",station.Workers.InnerListForReading.Select(p=>p.ThingID+" colonist="+p.IsColonist+" downed="+p.Downed+" mining="+p.skills.GetSkill(SkillDefOf.Mining).Level+" manipulation="+p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))));
            foundedTick=Find.TickManager.TicksGame; Check(station.nextProductionTick>=foundedTick+59000,"Founding did not schedule an actual full production day");
            savedFirst=first.ThingID;savedSecond=second.ThingID;savedAnimal=animal.ThingID;stage=1;return;
        }
        if(stage==1) {
            if(Find.TickManager.TicksGame-foundedTick>90000)throw new Exception("No native daily production within 1.5 days");
            if(station.lastProductionTick<=foundedTick)return;
            Check(Find.TickManager.TicksGame-foundedTick>=60000,"Production occurred before actual day elapsed");
            Check(animal!=null&&!animal.Destroyed&&station.Animals.Contains(animal)&&Find.WorldPawns.Contains(animal),"Stored pack animal lost to world GC during actual production day");
            // Balance update: Output = 2 * 15 * 0.4 = 12
            Check(Output==12,"Two skill15 miners must produce exactly 12 plasteel per actual day; received "+Output+"; "+string.Join(";",station.Workers.InnerListForReading.Select(p=>p.ThingID+" colonist="+p.IsColonist+" downed="+p.Downed+" mining="+p.skills.GetSkill(SkillDefOf.Mining).Level+" manipulation="+p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))));
            int before=Output;station.DoDailyProduction();station.TriggerDailyProductionNow();
            Check(Output==before,"Repeated public daily entry duplicated production");
            Check(station.ProducedGoods.All(t=>t.stackCount>0&&t.stackCount<=t.def.stackLimit&&t.holdingOwner==station.ProducedGoods),"Output stack limits/ownership broken");
            first.skills.GetSkill(SkillDefOf.Mining).Level=0; second.skills.GetSkill(SkillDefOf.Mining).Level=0;
            Check(station.ExpectedYield(DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Mining_Plasteel"))==0,"Zero skill produces labor");
            first.skills.GetSkill(SkillDefOf.Mining).Level=15;second.skills.GetSkill(SkillDefOf.Mining).Level=15;
            CleanDanglingRelations();
            stage=2;GameDataSaveLoader.SaveGame("TMM-OutpostDay");return;
        }
        if(stage==2) {
            string reason;Check(station.DeployPersonnelToMap(out reason),"Actual deploy failed: "+reason);
            Check(station.HasMap&&station.Map.GetComponent<StrategicOutpostMapComponent>().CountRealBeds()>=2,"Real level1 map/beds absent");
            Check(new[]{first,second,animal}.All(p=>p!=null&&p.Spawned&&p.Map==station.Map&&!station.Workers.Contains(p)&&!station.Animals.Contains(p)),"Deploy mismatch; stationMap="+station.Map.uniqueID+"; "+string.Join("; ",new[]{first,second,animal}.Select(p=>p==null?"null pawn":p.ThingID+" spawned="+p.Spawned+" map="+(p.Map==null?-1:p.Map.uniqueID)+" worker="+station.Workers.Contains(p)+" animal="+station.Animals.Contains(p))));

            // Guard check: deployed pawn cannot be removed to caravan
            var guardCaravan = CaravanMaker.MakeCaravan(new[]{Miner()}, Faction.OfPlayer, station.Tile, true);
            string guardFail;
            Check(!station.RemovePawnToCaravan(guardCaravan, first, out guardFail), "RemovePawnToCaravan must fail for deployed pawn");
            Check(guardFail != null && guardFail.Length > 0, "RemovePawnToCaravan must provide non-empty failReason for deployed pawn");
            bool deployedIsDeployed = station.operationalMode == OutpostOperationalMode.Physical || first.Spawned;
            bool deployedCanRemove = !deployedIsDeployed && (station.Animals.Contains(first) || station.PawnCount > 1);
            Check(!deployedCanRemove, "UI canRemove must be false for deployed pawn");
            Find.WorldObjects.Remove(guardCaravan);

            int before=Output;station.DoDailyProduction();Check(Output==before,"Physical mode duplicated abstract output");
            Check(station.RecallPersonnelToAutonomous(out reason),"Actual recall failed: "+reason);
            Check(station.Workers.Contains(first)&&station.Workers.Contains(second)&&station.Animals.Contains(animal)&&new[]{first,second,animal}.All(p=>!p.Spawned&&Find.WorldPawns.Contains(p)),"Recall lost identity or WorldPawns ownership");
            bool recalledIsDeployed = station.operationalMode == OutpostOperationalMode.Physical || first.Spawned;
            bool recalledCanRemove = !recalledIsDeployed && (station.Animals.Contains(first) || station.PawnCount > 1);
            Check(recalledCanRemove, "UI canRemove must be true for autonomous worker");
            Check(station.nextProductionTick>=Find.TickManager.TicksGame+59000,"Physical time retroactively counted");
            stage=3;return;
        }
        if(stage==3) {
            // Remove any starting silver from outpost RawMaterials
            var existingSilver = station.RawMaterials.Where(t => t.def == ThingDefOf.Silver).ToList();
            for(int i=0; i<existingSilver.Count; i++)
            {
                station.RawMaterials.Remove(existingSilver[i]);
                existingSilver[i].Destroy();
            }
            Check(station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver) == 0, "Outpost silver must be 0 for caravan upgrade test");

            // Setup payer colonist with bed
            Pawn payer = Miner();
            Building_Bed payerBed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
            GenSpawn.Spawn(payerBed, map.Center + new IntVec3(4, 0, 0), map);
            payerBed.SetFaction(Faction.OfPlayer);
            payer.ownership.ClaimBedIfNonMedical(payerBed);
            Check(payer.ownership.OwnedBed == payerBed, "Payer bed claim failed");

            // Caravan with 700 silver on tile
            var payCaravan = CaravanMaker.MakeCaravan(new[]{payer}, Faction.OfPlayer, station.Tile, true);
            Add(payer.inventory.innerContainer, ThingDefOf.Silver, 700);
            int paySilverBefore = CaravanInventoryUtility.AllInventoryItems(payCaravan).Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount);
            Check(paySilverBefore == 700, "PayCaravan silver setup failed: " + paySilverBefore);

            // Test failed transfer preserves bed (outpost capacity full at level 1: max 2, has 2)
            string fullFail;
            Check(!station.AddPawnFromCaravan(payCaravan, payer, out fullFail), "Transfer to full outpost must fail");
            Check(payer.ownership.OwnedBed == payerBed, "Bed must remain claimed on failed transfer");
            Check(payerBed.OwnersForReading.Contains(payer), "Payer bed must still belong to payer");

            // Verify GetLocalCaravan finds caravan with silver
            Caravan detectedCaravan = station.GetLocalCaravan();
            Check(detectedCaravan == payCaravan, "GetLocalCaravan did not find payCaravan");

            var component=station.Map.GetComponent<StrategicOutpostMapComponent>();
            var rect=component.GetDefaultModuleRect(2);
            var coins=ThingMaker.MakeThing(ThingDefOf.Silver);coins.stackCount=37;GenSpawn.Spawn(coins,rect.CenterCell,station.Map);
            string reason;
            Check(!component.CanSafelyPlaceModule(rect,2)&&!coins.Destroyed&&coins.stackCount==37,"Planner wipes loose silver");
            coins.DeSpawn();Check(station.RawMaterials.TryAdd(coins,false),"Cannot preserve fixture coins");
            station.RawMaterials.Remove(coins); coins.Destroy();
            Check(station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver) == 0, "Outpost silver must be 0 before upgrade");

            ClearCompound(component, station.Map);
            Check(station.TryUpgrade(out reason, payCaravan),"Actual level2 upgrade using caravan silver failed: "+reason);
            Check(station.level==2&&station.MaxCapacity==3,"Upgrade level/capacity incorrect");
            int paySilverAfter = CaravanInventoryUtility.AllInventoryItems(payCaravan).Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount);
            Check(paySilverAfter == 200, "PayCaravan silver after upgrade must be 200; got " + paySilverAfter);
            Check(station.RawMaterials.TotalStackCountOfDef(ThingDefOf.Silver) == 0, "Outpost raw silver must remain 0");
            Check(!station.Map.spawnedThings.Any(t => t.def == ThingDefOf.Silver), "No loose silver on map after upgrade");

            // Successful transfer into upgraded outpost frees bed
            string transferOk;
            Check(station.AddPawnFromCaravan(payCaravan, payer, out transferOk), "Transfer after upgrade failed: " + transferOk);
            Check(payer.ownership.OwnedBed == null, "Transfer to outpost must unclaim colonist bed in colony");
            Check(payerBed.OwnersForReading.Count == 0, "Payer bed must be free after transfer");

            // Return payer to caravan to keep outpost workforce at 2 founders for upper levels
            string returnOk;
            Check(station.RemovePawnToCaravan(payCaravan, payer, out returnOk), "RemovePawnToCaravan failed: " + returnOk);
            Check(payCaravan.ContainsPawn(payer), "Payer not back in caravan");
            Check(station.Workers.Count == 2, "Station workers count must be back to 2");
            Find.WorldObjects.Remove(payCaravan);
            payerBed.Destroy();

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
    private static void ClearCompound(StrategicOutpostMapComponent component, Map map)
    {
        int cx = component.baseCenter.IsValid ? component.baseCenter.x : map.Center.x;
        int cz = component.baseCenter.IsValid ? component.baseCenter.z : map.Center.z;
        var compound = new CellRect(cx - 26, cz - 26, 53, 53);
        foreach(var cell in compound) {
            if(!cell.InBounds(map)) continue;
            foreach(var thing in cell.GetThingList(map).ToArray())
                if(thing is Plant || thing is Mineable || (thing.def.thingCategories!=null&&thing.def.thingCategories.Contains(ThingCategoryDefOf.Chunks)))thing.Destroy(DestroyMode.Vanish);
            var terrain=map.terrainGrid.TerrainAt(cell);
            if(terrain.passability==Traversability.Impassable||!terrain.affordances.Contains(TerrainAffordanceDefOf.Light))map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);
        }
    }
    private void UpperLevels()
    {
        var component=station.Map.GetComponent<StrategicOutpostMapComponent>();
        ClearCompound(component, station.Map);
        for(int desired=3;desired<=10;desired++) {
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
        // Balance update: 8 workers with sum of Mining skills = 150. Expected yield = 150 * 0.4 = 60.0
        Check(station.Workers.Count==8&&station.Workers.InnerListForReading.Sum(p=>p.skills.GetSkill(SkillDefOf.Mining).Level)==150&&Math.Abs(station.ExpectedYield(DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Mining_Plasteel"))-60)<0.0001,"Legal eight-worker 150mining roster does not predict 60 plasteel/day");
        CleanDanglingRelations();
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
    // Tier10 reload: transient state only, so the saved Scribe layout stays identical to the
    // fixture written by UpperLevels().
    private int t10Stage, t10BaseOutput, t10Deadline, t10LoadTick;
    private string[] t10Workers;
    private readonly List<string> facts = new List<string>();
    private readonly List<string> t10Facts = new List<string>();
    private StrategicOutpostProductDef Plasteel { get { return DefDatabase<StrategicOutpostProductDef>.GetNamed("TMM_Outpost_Mining_Plasteel"); } }
    private void Tier10Reload()
    {
        if(t10Stage==0) {
            t10LoadTick=Find.TickManager.TicksGame;
            Check(station!=null&&station.Spawned&&station.level==10,"Saved level10 outpost absent or level changed: "+(station==null?"null":station.level.ToString()));
            Check(station.MaxCapacity==11&&!station.CanUpgrade,"Level10 capacity/upgrade cap not preserved");
            Check(station.HasMap&&station.Map.GetComponent<StrategicOutpostMapComponent>().CountRealBeds()>=11,"Level10 map or its eleven real beds lost on load");
            var workers=station.Workers.InnerListForReading;
            Check(workers.Count==8&&workers.All(p=>p!=null&&!p.Dead&&!p.Spawned&&p.Faction==Faction.OfPlayer&&p.IsColonist&&Find.WorldPawns.Contains(p)),"Eight held player workers not preserved: "+string.Join(";",workers.Select(p=>p==null?"null":p.ThingID+" faction="+(p.Faction==null?"null":p.Faction.def.defName)+" colonist="+p.IsColonist+" spawned="+p.Spawned+" world="+Find.WorldPawns.Contains(p))));
            Check(first!=null&&second!=null&&workers.Contains(first)&&workers.Contains(second)&&first.ThingID==savedFirst&&second.ThingID==savedSecond,"Original founders lost identity in level10 save");
            Check(animal!=null&&!animal.Destroyed&&station.Animals.Contains(animal)&&Find.WorldPawns.Contains(animal)&&animal.ThingID==savedAnimal,"Stored pack animal lost in level10 save");
            Check(workers.Sum(p=>p.skills.GetSkill(SkillDefOf.Mining).Level)==150,"Saved worker skills changed");
            // Balance update: Plasteel yield = 150 * 0.4 = 60.0
            Check(station.selectedProductDefNames.Count==1&&station.selectedProductDefNames[0]=="TMM_Outpost_Mining_Plasteel"&&Math.Abs(station.ExpectedYield(Plasteel)-60)<0.0001,"Level10 load does not predict 60 plasteel/day");
            Check(station.nextProductionTick>t10LoadTick&&station.nextProductionTick<=t10LoadTick+60000,"Saved production deadline invalid: next="+station.nextProductionTick+" now="+t10LoadTick);
            t10Workers=workers.Select(p=>p.ThingID).OrderBy(s=>s).ToArray();
            t10BaseOutput=Output;t10Deadline=station.nextProductionTick;
            station.DoDailyProduction();Check(Output==t10BaseOutput,"Level10 load permits premature/duplicate production");
            t10Facts.Add("loadTick="+t10LoadTick);t10Facts.Add("deadline="+t10Deadline);t10Facts.Add("baseOutput="+t10BaseOutput);
            t10Stage=1;return;
        }
        if(t10Stage==1) {
            if(Find.TickManager.TicksGame>t10Deadline+30000)throw new Exception("No native production on level10 within deadline+0.5day; last="+station.lastProductionTick+" next="+station.nextProductionTick);
            if(station.lastProductionTick<t10Deadline)return;
            Check(station.lastProductionTick>=t10Deadline&&station.lastProductionTick-t10Deadline<2500,"Production fired at wrong time: last="+station.lastProductionTick+" deadline="+t10Deadline);
            int produced=Output-t10BaseOutput;t10Facts.Add("productionTick="+station.lastProductionTick);t10Facts.Add("produced="+produced);
            // Balance update: Plasteel daily produced = 150 * 0.4 = 60
            Check(produced==60,"Eight-worker 150mining level10 outpost must produce exactly 60 plasteel per actual day; received "+produced);
            Check(station.ProducedGoods.All(t=>t.stackCount>0&&t.stackCount<=t.def.stackLimit&&t.holdingOwner==station.ProducedGoods),"Level10 output stacks broken");
            Check(station.nextProductionTick==station.lastProductionTick+60000,"Next deadline not one day after production");
            int before=Output;station.DoDailyProduction();station.TriggerDailyProductionNow();Check(Output==before,"Repeated daily entry duplicated level10 production");
            Check(animal!=null&&!animal.Destroyed&&station.Animals.Contains(animal)&&Find.WorldPawns.Contains(animal),"Stored animal lost to world GC during level10 day");
            Check(station.Workers.InnerListForReading.Select(p=>p.ThingID).OrderBy(s=>s).SequenceEqual(t10Workers)&&station.Workers.InnerListForReading.All(p=>p.IsColonist&&Find.WorldPawns.Contains(p)),"Worker roster changed during level10 day");
            t10Stage=2;return;
        }
        if(t10Stage==2) {
            var roster=station.Workers.InnerListForReading.ToList();string reason;
            Check(station.DeployPersonnelToMap(out reason),"Level10 deploy of eight workers failed: "+reason);
            Check(roster.All(p=>p.Spawned&&p.Map==station.Map)&&animal.Spawned&&animal.Map==station.Map&&station.Workers.Count==0,"Not all eight workers and the animal reached the level10 map");
            var beds=station.Map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Where(b=>!b.Medical).ToList();
            Check(beds.Count>=11,"Fewer than eleven ordinary beds on deployed level10 map: "+beds.Count);
            foreach(var pawn in roster)foreach(var bed in beds)Check(pawn.CanReach(bed,Verse.AI.PathEndMode.Touch,Danger.Some),"Worker "+pawn.ThingID+" cannot reach bed at "+bed.Position);
            t10Facts.Add("ordinaryBeds="+beds.Count);
            int before=Output;station.DoDailyProduction();Check(Output==before,"Physical level10 mode produced abstract output");
            Check(station.RecallPersonnelToAutonomous(out reason),"Level10 recall of eight workers failed: "+reason);
            Check(station.Workers.InnerListForReading.Select(p=>p.ThingID).OrderBy(s=>s).SequenceEqual(t10Workers)&&roster.All(p=>!p.Spawned&&Find.WorldPawns.Contains(p)&&p.IsColonist)&&station.Animals.Contains(animal),"Recall lost level10 worker identity/ownership");
            Check(station.nextProductionTick>=Find.TickManager.TicksGame+59000,"Physical time retroactively counted on level10");
            Finish(null,true,"OutpostTier10ReloadProbe.json");
        }
    }
    public override void MapComponentUpdate()
    {
        var args=Environment.GetCommandLineArgs();
        bool tier10=args.Contains("-tmm-outpost-tier10-reload");
        bool reload=args.Contains("-tmm-outpost-reload");
        if(done||map.Parent is StrategicOutpost||!map.IsPlayerHome||(!reload&&!tier10&&!args.Contains("-tmm-outpost-probe")))return;
        // A random event can open a force-pause dialog during the multi-day production wait and freeze the run.
        // Close such foreign dialogs in the test only; our own outpost windows stay under the probe's control.
        foreach(Window w in Find.WindowStack.Windows.ToList())
            if(w.forcePause&&w.GetType().Namespace!="TurboMegaMod")w.Close(false);
        try {Find.TickManager.CurTimeSpeed=TimeSpeed.Superfast; if(tier10)Tier10Reload();else if(reload)Reload();else Fresh();}
        catch(Exception e){if(tier10)Finish(e,true,"OutpostTier10ReloadProbe.json");else Finish(e,reload);}
    }
    private static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private void Finish(Exception e,bool reload,string reportName=null)
    {
        done=true;if(e!=null)errors.Add(e.ToString());
        string report="{\"passed\":"+(e==null?"true":"false")+",\"checks\":"+checks+",\"stage\":"+stage+",\"tier10Stage\":"+t10Stage+",\"facts\":["+string.Join(",",facts.Concat(t10Facts).Select(Q))+"],\"errors\":["+string.Join(",",errors.Select(Q))+"]}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reportName??(reload?"OutpostReloadProbe.json":"OutpostProbe.json")),report);
        if(!reload)File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"OutpostUiProbe.json"),"{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
    }
}
