// Only installed in an isolated disposable game profile.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;

[StaticConstructorOnStartup]
public static class FarmingReloadLaunch
{
    static FarmingReloadLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-farming-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Farming"));
    }
}

public sealed class FarmingProbe : MapComponent
{
    private int stage, checks, stageTick;
    private bool uiReported;
    private Zone_Greenhouse zone;
    private Building_LitHydroponics hydro;
    private Plant soilPlant, hydroPlant;
    private bool savePrepared;
    private int savedLightCount;
    private readonly List<string> errors = new List<string>();
    private static Map activeMap;
    private static float sky = 0f;
    private float warmed, soilGrowth, hydroGrowth;
    private readonly Harmony diagnostic = new Harmony("tmm.test.farming.environment");
    public FarmingProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_Values.Look(ref savePrepared, "farmingSavePrepared"); Scribe_Values.Look(ref savedLightCount, "savedLightCount");
        Scribe_References.Look(ref zone, "testZone"); Scribe_References.Look(ref hydro, "testHydro"); Scribe_References.Look(ref soilPlant, "testSoilPlant"); Scribe_References.Look(ref hydroPlant, "testHydroPlant");
    }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static bool Sky(ref float __result) { __result = sky; return false; }
    private void Noon()
    {
        float d = GenLocalDate.DayPercent(map); int delta = Mathf.RoundToInt((0.5f - d) * 60000f); if (delta < 0) delta += 60000;
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + delta);
    }
    private void Prepare(CellRect rect)
    {
        foreach (var c in rect)
        {
            foreach (var thing in c.GetThingList(map).ToArray()) if (thing is Plant || thing is Building) thing.Destroy();
            map.roofGrid.SetRoof(c, null); map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil); map.snowGrid.SetDepth(c, 0f);
        }
    }
    private Thing Spawn(string name, IntVec3 c, Rot4 rotation)
    {
        ThingDef def = DefDatabase<ThingDef>.GetNamed(name); Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null); thing.SetFaction(Faction.OfPlayer); return GenSpawn.Spawn(thing, c, map, rotation);
    }
    private Plant Rice(IntVec3 c)
    {
        var p = (Plant)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Plant_Rice")); p.Growth = 0.1f; p.sown = true; GenSpawn.Spawn(p, c, map); return p;
    }
    private void Till(IntVec3 cell)
    {
        var terrain = DefDatabase<TerrainDef>.GetNamed("TMM_TilledSoil");
        Check(GenConstruct.CanPlaceBlueprintAt(terrain, cell, Rot4.North, map, false).Accepted, "Cannot plan tilling soil");
        var blueprint = GenConstruct.PlaceBlueprintForBuild(terrain, cell, map, Rot4.North, Faction.OfPlayer, null);
        object[] args = { map.mapPawns.FreeColonistsSpawned.First(), null, false };
        bool replaced = (bool)AccessTools.Method(blueprint.GetType(), "TryReplaceWithSolidThing").Invoke(blueprint, args);
        var frame = args[1] as Frame;
        bool jobFailed = !replaced || (bool)args[2];
        Check(!jobFailed && frame != null && frame.WorkToBuild > 0f, "Tilling did not create a native work frame");
        frame.CompleteConstruction(map.mapPawns.FreeColonistsSpawned.First());
        Check(cell.GetTerrain(map) == terrain, "Native construction did not produce tilled soil");
    }
    private void GlassDurability()
    {
        var cell = new IntVec3(75, 0, 52);
        foreach (string name in new[] { "TMM_GreenhouseGlassWall", "TMM_GreenhouseGlassDoor" })
        {
            bool wall = name.EndsWith("Wall");
            foreach (string damage in new[] { "Blunt", "Cut", "Bullet", "Bomb" })
            {
                Thing target = Spawn(name, cell, Rot4.North);
                Check(target.MaxHitPoints == (wall ? 50 : 70), "Incorrect glass durability: " + name);
                Check(target.def.damageMultipliers.Count == 0, "Glass retained inherited damage armour");
                int before = target.HitPoints;
                var damageDef = DefDatabase<DamageDef>.GetNamed(damage);
                target.TakeDamage(new DamageInfo(damageDef, 10f, 999f));
                float expected = damageDef.buildingDamageFactor * (damage == "Bomb" ? (wall ? 40f : 20f) : 10f);
                int lost = before - target.HitPoints;
                Check(lost >= Math.Min(before, Mathf.FloorToInt(expected)) && lost <= Math.Min(before, Mathf.CeilToInt(expected)), name + " damage " + damage + " expected " + expected + " lost " + lost);
                if (!target.Destroyed) target.Destroy();
            }
            Thing weak = Spawn(name, cell, Rot4.North); int weakBefore = weak.HitPoints;
            weak.TakeDamage(new DamageInfo(DefDatabase<DamageDef>.GetNamed("Bullet"), 1f, 999f));
            Check(weak.HitPoints == weakBefore - 1, "Artificial minimum damage added to glass"); weak.Destroy();
            Thing strong = Spawn(name, cell, Rot4.North); strong.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 1000f, 999f));
            Check(strong.Destroyed, "Strong attack was capped or glass did not shatter");
        }
        var blunt = DamageDefOf.Blunt;
        float oldSolid = blunt.buildingDamageFactorImpassable, oldPassable = blunt.buildingDamageFactorPassable;
        try
        {
            foreach (float factor in new[] { 0f, 3f })
            {
                blunt.buildingDamageFactorImpassable = factor; blunt.buildingDamageFactorPassable = factor;
                foreach (string name in new[] { "TMM_GreenhouseGlassWall", "TMM_GreenhouseGlassDoor", "Wall" })
                {
                    Thing target = Spawn(name, cell, Rot4.North); int before = target.HitPoints;
                    target.TakeDamage(new DamageInfo(blunt, 10f, 999f));
                    int expected = Mathf.RoundToInt(blunt.buildingDamageFactor * (name == "Wall" ? factor * 10f : 10f));
                    Check(before - target.HitPoints == expected, "Glass isolation failed at global factor " + factor + " for " + name);
                    target.Destroy();
                }
            }
        }
        finally { blunt.buildingDamageFactorImpassable = oldSolid; blunt.buildingDamageFactorPassable = oldPassable; }
        Check(DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseClimateUnit").GetStatValueAbstract(StatDefOf.MaxHitPoints) == 180f, "Climate panel durability changed");
    }
    public override void MapComponentUpdate()
    {
        bool reload = Environment.GetCommandLineArgs().Contains("-tmm-farming-reload");
        if (stage == 99 || Find.CurrentMap != map || (!reload && !Environment.GetCommandLineArgs().Contains("-tmm-farming-probe"))) return;
        try
        {
            if (reload)
            {
                if (stage == 0)
                {
                    Check(savePrepared, "Save fixture marker missing"); Check(zone != null && hydro != null && soilPlant != null, "Saved references failed to resolve");
                    Check(zone.planned && zone.CellCount == 36 && zone.Enclosed, "Saved greenhouse plan/room/crops changed");
                    Check(new IntVec3(40, 0, 44).GetEdifice(map).HitPoints == 50, "Legacy wall HP did not clamp to new maximum");
                    Check(zone.DoorCell.GetEdifice(map).HitPoints == 70, "Legacy door HP did not clamp to new maximum");
                    Check(zone.lightNodes.Count == savedLightCount && zone.lightNodes.All(n => n != null && n.Spawned), "Saved light references missing");
                    Check(Math.Abs(map.fertilityGrid.FertilityAt(new IntVec3(43, 0, 43)) - 2.22f) < 0.001f, "Saved greenhouse tilling lost 222% fertility");
                    Check(Math.Abs(map.fertilityGrid.FertilityAt(new IntVec3(55, 0, 43)) - 2f) < 0.001f, "Saved outdoor tilling lost 200% fertility");
                    Noon(); diagnostic.Patch(AccessTools.PropertyGetter(typeof(SkyManager), "CurSkyGlow"), prefix: new HarmonyMethod(typeof(FarmingProbe), "Sky"));
                    stage = 1; stageTick = Find.TickManager.TicksGame; return;
                }
                if (Find.TickManager.TicksGame - stageTick < 300) return;
                zone.Synchronize();
                Check(zone.Powered && hydro.TryGetComp<CompPowerTrader>().PowerOn, "Saved devices failed to reconnect to power");
                Check(zone.lightNodes.Count == savedLightCount, "Reload duplicated light helpers");
                Check(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseLight")).Count == savedLightCount, "Orphan helpers after reload");
                Check(map.glowGrid.GroundGlowAt(soilPlant.Position) >= 0.99f, "Saved lights did not resume");
                int walls = map.listerBuildings.allBuildingsColonist.Count(b => b.def.defName == "TMM_GreenhouseGlassWall");
                var controller = (Building_GreenhouseNode)zone.powerNode;
                warmed = zone.GrowingRoom.Temperature;
                zone.Delete();
                Check(map.listerBuildings.allBuildingsColonist.Count(b => b.def.defName == "TMM_GreenhouseGlassWall") == walls, "Deleting zone erased paid walls");
                Check(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("TMM_GreenhouseLight")).Count == 0, "Deleting zone leaked helper lights");
                Check(Math.Abs(map.fertilityGrid.FertilityAt(new IntVec3(43, 0, 43)) - 2f) < 0.001f, "Deleted greenhouse left a permanent 222% bonus");
                zone = controller.RestoreZone();
                Check(zone != null && zone.CellCount == 36 && zone.planned, "Cannot restore a deleted growing zone from paid climate panel");
                Check(Math.Abs(map.fertilityGrid.FertilityAt(new IntVec3(43, 0, 43)) - 2.22f) < 0.001f, "Restoring zone lost greenhouse tilling bonus");
                Finish(); return;
            }
            if (stage == 0)
            {
                activeMap = map; Noon();
                diagnostic.Patch(AccessTools.PropertyGetter(typeof(SkyManager), "CurSkyGlow"), prefix: new HarmonyMethod(typeof(FarmingProbe), "Sky"));
                Prepare(new CellRect(38, 38, 40, 20));
                var tilledDef = DefDatabase<TerrainDef>.GetNamed("TMM_TilledSoil");
                Check(tilledDef.texturePath == "Terrain/Farming/TilledSoil" && ContentFinder<Texture2D>.Get(tilledDef.texturePath, false) != null, "New tilled soil texture missing");
                Check(tilledDef.uiIconPath == "UI/Farming/TillSoil" && ContentFinder<Texture2D>.Get(tilledDef.uiIconPath, false) != null, "New tilling icon missing");
                GlassDurability();
                zone = new Zone_Greenhouse(map.zoneManager); map.zoneManager.RegisterZone(zone);
                foreach (var c in new CellRect(40, 40, 8, 8)) zone.AddCell(c);
                Check(zone.CreatePlan(), "Failed to create greenhouse plan");
                Check(zone.CellCount == 36, "8x8 footprint must create 6x6 crops");
                var blueprints = map.listerThings.AllThings.OfType<Blueprint_Build>().Where(b => zone.Footprint.Contains(b.Position)).ToArray();
                Check(blueprints.Length == 28, "Wrong perimeter blueprint count");
                Check(blueprints.Count(b => b.def.entityDefToBuild.defName == "TMM_GreenhouseGlassDoor") == 1, "Door blueprint missing");
                foreach (var b in blueprints)
                {
                    var def = (ThingDef)b.def.entityDefToBuild; var pos = b.Position; var rot = b.Rotation; b.Destroy(); Spawn(def.defName, pos, rot);
                }
                // Complete planned construction in this test fixture only.
                foreach (var c in zone.Cells) map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
                Check(zone.Cells.All(c => map.roofGrid.RoofAt(c).defName == "TMM_GreenhouseGlassRoof"), "Workers' roof operation did not select glass");
                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                Check(zone.Enclosed, "Physical walls/door/roof did not form a room");
                zone.GrowingRoom.Temperature = -25f;
                for (int x = 40; x <= 68; x++) Spawn("PowerConduit", new IntVec3(x, 0, 39), Rot4.North);
                for (int x = 49; x <= 53; x += 2) Spawn("VanometricPowerCell", new IntVec3(x, 0, 39), Rot4.North);
                var hydroRoomRect = new CellRect(58, 40, 8, 8);
                foreach (var c in hydroRoomRect.EdgeCells) Spawn(c == new IntVec3(61, 0, 40) ? "Door" : "Wall", c, Rot4.North);
                foreach (var c in hydroRoomRect.ContractedBy(1)) map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                hydro = (Building_LitHydroponics)Spawn("TMM_LitHydroponicsBasin", new IntVec3(61, 0, 43), Rot4.North);
                hydro.Position.GetRoom(map).Temperature = 21f;
                for (int z = 40; z <= 46; z++) Spawn("PowerConduit", new IntVec3(63, 0, z), Rot4.North);
                Spawn("Heater", new IntVec3(64, 0, 44), Rot4.North);
                soilPlant = Rice(new IntVec3(42, 0, 42)); hydroPlant = Rice(hydro.Position);
                Find.CameraDriver.SetRootPosAndSize(new Vector3(52f, 0f, 44f), 16f);
                stage = 1; stageTick = Find.TickManager.TicksGame;
                return;
            }
            if (Find.TickManager.TicksGame - stageTick < 300) return;
            if (stage == 1)
            {
                zone.Synchronize();
                if ((!zone.Powered || !hydro.TryGetComp<CompPowerTrader>().PowerOn) && Find.TickManager.TicksGame - stageTick < 2000) return;
                if ((map.glowGrid.GroundGlowAt(soilPlant.Position) < 0.99f || map.glowGrid.GroundGlowAt(hydroPlant.Position) < 0.99f) && Find.TickManager.TicksGame - stageTick < 2500) return;
                Check(zone.Power != null && zone.Power.PowerNet != null, "Greenhouse missing power net");
                Check(zone.Powered, "Greenhouse not connected: enclosed=" + zone.Enclosed + " unit=" + (zone.powerNode != null) + " demand=" + zone.Demand + " power=" + (zone.Power == null ? "null" : zone.Power.DebugString) + " sources=" + string.Join(";", map.listerBuildings.allBuildingsColonist.Where(b => b.def.defName == "VanometricPowerCell").Select(b => b.Position + ":" + b.TryGetComp<CompPowerTrader>().DebugString)));
                Check(hydro.TryGetComp<CompPowerTrader>().PowerOn, "Hydroponics not powered: connect=" + hydro.def.ConnectToPower + " parent=" + hydro.TryGetComp<CompPowerTrader>().connectParent + " net=" + hydro.TryGetComp<CompPowerTrader>().PowerNet + " demand=" + hydro.TryGetComp<CompPowerTrader>().DebugString);
                Check(zone.Power.PowerNet == hydro.TryGetComp<CompPowerTrader>().PowerNet, "Consumers are not on expected power net");
                var orphan = (Building_GreenhouseNode)Spawn("TMM_GreenhouseClimateUnit", new IntVec3(75, 0, 55), Rot4.North);
                orphan.zoneId = zone.ID; orphan.TickRare();
                Check(orphan.Owner == null && orphan.TryGetComp<CompPowerTrader>().PowerOutput == 0f, "Unrelated climate panel claimed another greenhouse"); orphan.Destroy();
                // Waiting for power/light also advances ambient heat exchange.
                // Establish cold conditions immediately before measuring heat.
                zone.GrowingRoom.Temperature = 45f;
                float hot = zone.GrowingRoom.Temperature;
                for (int i = 0; i < 200; i++) zone.ApplyHeat();
                Check(zone.GrowingRoom.Temperature < hot && zone.GrowingRoom.Temperature >= zone.targetTemperature - 0.01f, "Climate cooling failed or overshot");
                Check(Math.Abs(zone.GrowingRoom.Temperature - zone.targetTemperature) < 0.1f, "Cooling did not reach the selected target");
                zone.GrowingRoom.Temperature = zone.targetTemperature; float idleDemand = zone.Demand;
                zone.GrowingRoom.Temperature = zone.targetTemperature + 10f; float mildCooling = zone.Demand;
                zone.GrowingRoom.Temperature = zone.targetTemperature + 30f;
                Check(zone.Demand > mildCooling && mildCooling > idleDemand, "Cooling demand does not follow temperature difference");
                zone.GrowingRoom.Temperature = zone.targetTemperature - 10f; float mildHeating = zone.Demand;
                zone.GrowingRoom.Temperature = -25f;
                Check(zone.Demand > mildHeating && mildHeating > idleDemand, "Heating demand does not follow temperature difference");
                float before = zone.GrowingRoom.Temperature;
                for (int i = 0; i < 200; i++) zone.ApplyHeat();
                warmed = zone.GrowingRoom.Temperature;
                Check(warmed > before && warmed <= zone.targetTemperature + 0.01f, "Actual room heating failed or overshot");
                Check(warmed >= 10f, "Heating did not reach crop survival temperature");
                Check(Math.Abs(map.fertilityGrid.FertilityAt(soilPlant.Position) - 2f) < 0.001f, "Completed greenhouse soil did not instantly reach 200%");
                Till(new IntVec3(55, 0, 43));
                Check(Math.Abs(map.fertilityGrid.FertilityAt(new IntVec3(55, 0, 43)) - 2f) < 0.001f, "Outdoor tilled soil is not 200%");
                var tilledCell = new IntVec3(43, 0, 43); Till(tilledCell);
                Check(Math.Abs(map.fertilityGrid.FertilityAt(tilledCell) - 2.22f) < 0.001f, "Greenhouse tilled soil is not 222%");
                Check(!SoilCultivation.IsTillable(DefDatabase<TerrainDef>.GetNamed("Concrete")) && !SoilCultivation.IsTillable(TerrainDefOf.WaterDeep), "Tilling accepts floors or water");
                Check(zone.LampsOn, "Grow lights not on during polar darkness at local noon");
                Check(map.glowGrid.GroundGlowAt(soilPlant.Position) >= 0.99f, "Greenhouse crops lack real grow light: " + map.glowGrid.GroundGlowAt(soilPlant.Position) + " lights=" + string.Join(";", zone.lightNodes.Select(n => n.Position + ":" + n.TryGetComp<CompGlower>().Glows + ":" + n.TryGetComp<CompGlower>().Props.overlightRadius)));
                Check(map.glowGrid.GroundGlowAt(hydroPlant.Position) >= 0.99f, "Hydroponic crops lack real grow light");
                Check(Math.Abs(-hydro.TryGetComp<CompPowerTrader>().PowerOutput - FarmingPowerBudget.Hydroponics(true, 70f, 2900f)) < 0.1f, "Hydroponic power budget wrong");
                soilGrowth = soilPlant.Growth; hydroGrowth = hydroPlant.Growth;
                for (int i = 0; i < 3; i++) { soilPlant.TickLong(); hydroPlant.TickLong(); }
                Check(soilPlant.Growth > soilGrowth, "Greenhouse plant failed to grow"); Check(hydroPlant.Growth > hydroGrowth, "Hydroponic plant failed to grow");
                sky = 1f; zone.SyncPowerAndLights(); hydro.TryGetComp<CompLitHydroponicsPower>().RefreshDemand();
                Check(!zone.LampsNeeded && !zone.LampsOn, "Sunlit greenhouse kept grow lights on");
                Check(map.glowGrid.GroundGlowAt(soilPlant.Position) >= 0.99f, "Glass roof blocks natural sunlight");
                Check(ControlledFarming.HydroLightingMode(hydro) == 1, "Opaque indoor roof incorrectly supplies sunlight");
                Check(Math.Abs(zone.Demand - FarmingPowerBudget.Greenhouse(36, false, zone.GrowingRoom.Temperature, zone.targetTemperature, 2900f, 175f, 200f)) < 0.1f, "Sunlit greenhouse still pays for grow lights");
                sky = 0f; zone.SyncPowerAndLights();
                var breach = new IntVec3(40, 0, 44); breach.GetEdifice(map).Destroy(); map.regionAndRoomUpdater.RebuildAllRegionsAndRooms(); zone.SyncPowerAndLights();
                Check(!zone.Enclosed && zone.Demand == 0f, "Breached greenhouse still runs as a sealed room");
                Check(Math.Abs(map.fertilityGrid.FertilityAt(new IntVec3(43, 0, 43)) - 2f) < 0.001f, "Open-air crops retain greenhouse-only tilling bonus");
                Spawn("TMM_GreenhouseGlassWall", breach, Rot4.North); map.regionAndRoomUpdater.RebuildAllRegionsAndRooms(); zone.SyncPowerAndLights();
                Check(zone.Enclosed, "Repaired wall did not enclose greenhouse again");
                zone.GrowingRoom.Temperature = 21f;
                float power36 = FarmingPowerBudget.Greenhouse(36, true, true, 2900f, 175f);
                Check(FarmingPowerBudget.Greenhouse(72, true, true, 2900f, 175f) == power36 * 2f, "Zone demand does not scale with area");
                zone.systemEnabled = false; zone.SyncPowerAndLights();
                Check(!zone.LampsOn && zone.Power.PowerOutput == 0f, "Zone switch did not stop systems");
                zone.GrowingRoom.Temperature = zone.targetTemperature + 15f;
                float offBefore = zone.GrowingRoom.Temperature; zone.ApplyHeat(); Check(zone.GrowingRoom.Temperature == offBefore, "Disabled zone still controls temperature");
                zone.GrowingRoom.Temperature = zone.targetTemperature;
                zone.systemEnabled = true; zone.SyncPowerAndLights();
                stage = 2; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 2)
            {
                // Mid-night: plants rest, pumps must keep working.
                float d = GenLocalDate.DayPercent(map); int delta = Mathf.RoundToInt((0.95f - d) * 60000f); if (delta < 0) delta += 60000;
                Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + delta);
                hydro.TryGetComp<CompLitHydroponicsPower>().RefreshDemand(); zone.SyncPowerAndLights();
                Check(!zone.LampsOn, "Night resting period ignored");
                Check(hydro.TryGetComp<CompPowerTrader>().PowerOn, "Pumps stopped during plant rest");
                Check(zone.NightLightOn, "Night comfort light not enabled");
                Check(hydro.TryGetComp<CompGlower>().Props.overlightRadius == 0f && zone.lightNodes.All(n => n.TryGetComp<CompGlower>().Props.overlightRadius == 0f), "Night light still uses grow-light overillumination");
                Check(Math.Abs(-hydro.TryGetComp<CompPowerTrader>().PowerOutput - FarmingPowerBudget.Hydroponics(false, 70f, 2900f, true, ControlledFarming.BasePower("StandingLamp"))) < 0.1f, "Night pump and comfort-light budget wrong");
                stage = 3; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 3)
            {
                float nightGlow = map.glowGrid.GroundGlowAt(soilPlant.Position);
                Check(nightGlow > 0f && nightGlow <= 0.5f, "Night comfort light is dark or still overlit: " + nightGlow);
                float growth = soilPlant.Growth; soilPlant.TickLong(); Check(soilPlant.Growth == growth, "Night light prevents native plant rest");
                Noon(); zone.Synchronize(); hydro.TryGetComp<CompLitHydroponicsPower>().RefreshDemand();
                foreach (var generator in map.listerBuildings.allBuildingsColonist.Where(b => b.def.defName == "VanometricPowerCell").ToArray()) generator.Destroy();
                stage = 4; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 4)
            {
                Check(!zone.Powered && !hydro.TryGetComp<CompPowerTrader>().PowerOn, "Devices stayed powered after physical generator removal");
                float temperature = zone.GrowingRoom.Temperature; zone.ApplyHeat(); Check(zone.GrowingRoom.Temperature == temperature, "Unpowered greenhouse kept heating");
                Check(!zone.LampsOn && !zone.NightLightOn && !hydro.TryGetComp<CompGlower>().Glows, "Lights stayed on after power cut");
                for (int x = 49; x <= 53; x += 2) Spawn("VanometricPowerCell", new IntVec3(x, 0, 39), Rot4.North);
                stage = 5; stageTick = Find.TickManager.TicksGame; return;
            }
            if (stage == 5)
            {
                zone.Synchronize();
                if ((!zone.Powered || !hydro.TryGetComp<CompPowerTrader>().PowerOn) && Find.TickManager.TicksGame - stageTick < 2000) return;
                Check(zone.Powered && hydro.TryGetComp<CompPowerTrader>().PowerOn, "Power failed to resume after replacing generators");
                savePrepared = true; savedLightCount = zone.lightNodes.Count;
                // Simulate an older save carrying pre-balance hit point totals.
                new IntVec3(40, 0, 44).GetEdifice(map).HitPoints = 120;
                zone.DoorCell.GetEdifice(map).HitPoints = 150;
                GameDataSaveLoader.SaveGame("TMM-Farming");
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Farming")), "Full map save not written");
                Finish();
            }
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    private void Finish()
    {
        stage = 99; diagnostic.UnpatchAll(diagnostic.Id);
        string report = Environment.GetCommandLineArgs().Contains("-tmm-farming-reload") ? "FarmingReloadProbe.json" : "FarmingProbe.json";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, report), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"actualRoomTemperature\":" + warmed.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || stage != 99 || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-farming-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null;
        try
        {
            new Designator_ZoneAdd_Greenhouse().GizmoOnGUI(new Vector2(20, 20), 120f, default(GizmoRenderParms));
            new Designator_Build(DefDatabase<TerrainDef>.GetNamed("TMM_TilledSoil")).GizmoOnGUI(new Vector2(160, 20), 120f, default(GizmoRenderParms));
        }
        catch (Exception e) { error = e.ToString(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FarmingUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"designatorDrawInvokedInRepaint\":true,\"tillingDesignatorDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
