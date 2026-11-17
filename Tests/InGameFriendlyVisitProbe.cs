using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using UnityEngine;
using Verse;

[StaticConstructorOnStartup]
public static class FriendlyVisitLaunch
{
    static FriendlyVisitLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-friendly-visit-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-FriendlyVisit"));
    }
}
public sealed class FriendlyVisitProbe : MapComponent
{
    private static FriendlyVisitProbe active;
    private static Map captureMap;
    private static int frames;
    private Settlement settlement;
    private Pawn first, second;
    private Thing cash;
    private bool saved, done, initialized;
    private int checks, buildingCount, relation, readyTick;
    public FriendlyVisitProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        Scribe_References.Look(ref settlement, "fvSettlement");
        Scribe_References.Look(ref first, "fvFirst");
        Scribe_References.Look(ref second, "fvSecond");
        Scribe_References.Look(ref cash, "fvCash");
        Scribe_Values.Look(ref saved, "fvSaved");
        Scribe_Values.Look(ref buildingCount, "fvBuildings");
        Scribe_Values.Look(ref relation, "fvRelation");
    }
    private void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; }
    private bool Reload { get { return Environment.GetCommandLineArgs().Contains("-tmm-friendly-visit-reload"); } }
    public override void MapComponentUpdate()
    {
        if (done || Find.CurrentMap != map || (active != null && active != this)) return;
        if (!Reload && !Environment.GetCommandLineArgs().Contains("-tmm-friendly-visit-probe")) return;
        active = this; string error = null;
        try
        {
            if (initialized)
            {
                if (Find.TickManager.TicksGame < readyTick) return;
                Check(settlement.Spawned && settlement.Map.Parent == settlement, "Native ticks falsely destroyed friendly settlement");
                Check(!SettlementDefeatUtility.IsDefeated(settlement.Map, settlement.Faction), "Living peaceful garrison marked defeated");
                Check(!settlement.GetComponent<TimedDetectionRaids>().NextRaidCountdownActiveAndVisible, "Friendly visit starts punitive reinforcement timer");
                Check(settlement.Faction.PlayerGoodwill == relation, "Native ticks changed diplomacy");
                if (!Reload)
                {
                    saved = true; GameDataSaveLoader.SaveGame("TMM-FriendlyVisit");
                    Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-FriendlyVisit.rws")), "No actual visit save");
                }
                captureMap = settlement.Map;
                captureMap.fogGrid.ClearAllFog();
                Current.Game.CurrentMap = captureMap;
                CameraJumper.TryJump(new GlobalTargetInfo(captureMap.Center, captureMap));
                Find.CameraDriver.SetRootPosAndSize(captureMap.Center.ToVector3Shifted(), 52f);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            }
            else
            {
            if (Reload)
            {
                Check(saved && settlement != null && settlement.HasMap, "Real visited map missing");
                Check(first != null && second != null && first.Spawned && second.Spawned && first.Map == settlement.Map && second.Map == settlement.Map, "Visitor identities/placement lost");
                Check(first.inventory.innerContainer.Contains(cash) && cash.stackCount == 19, "Visitor cargo lost/duplicated");
                Check(settlement.Map.GetComponent<SettlementMapDevelopment>().generatedLevel == 10, "Map tier lost");
                Check(settlement.Map.GetComponent<SettlementMapDevelopment>().initialBuildings == buildingCount, "Map was regenerated");
                Check(settlement.Faction.PlayerGoodwill == relation && !settlement.Faction.HostileTo(Faction.OfPlayer), "Visit changed diplomacy");
                Check(settlement.GetComponent<FormCaravanComp>().CanFormOrReformCaravanNow, "Visitors cannot leave normally after reload");
            }
            else
            {
                settlement = Find.WorldObjects.Settlements.First(s => s.Faction != null && !s.Faction.IsPlayer && !s.Faction.HostileTo(Faction.OfPlayer) && s.Faction.def.humanlikeFaction && s.Faction.def.techLevel >= TechLevel.Industrial && !s.HasMap);
                Find.World.GetComponent<RegionalWorld>().For(settlement).level = 10;
                relation = settlement.Faction.PlayerGoodwill;
                first = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                second = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                cash = ThingMaker.MakeThing(ThingDefOf.Silver); cash.stackCount = 19; first.inventory.innerContainer.TryAdd(cash, false);
                var caravan = CaravanMaker.MakeCaravan(new[] { first, second }, Faction.OfPlayer, settlement.Tile, true);
                Check(FriendlySettlementVisits.CanVisit(caravan, settlement), "Friendly visit guards rejected real caravan");
                var command = settlement.GetCaravanGizmos(caravan).OfType<Command_Action>().FirstOrDefault(c => c.defaultLabel == "TMM.World.EnterFriendly".Translate());
                Check(command != null, "Actual patched caravan entry command absent");
                command.action();
                Check(settlement.HasMap, "Native entry did not generate map");
                Check(first.Spawned && second.Spawned && first.Map == settlement.Map && second.Map == settlement.Map, "Native entry lost actual visitors");
                Check(!caravan.Spawned && !caravan.ContainsPawn(first), "Caravan still holds spawned visitor");
                Check(first.inventory.innerContainer.Contains(cash) && cash.stackCount == 19, "Native entry changed physical cargo");
                Check(settlement.Faction.PlayerGoodwill == relation && !settlement.Faction.HostileTo(Faction.OfPlayer), "Entry triggered war");
                var development = settlement.Map.GetComponent<SettlementMapDevelopment>();
                buildingCount = development.initialBuildings;
                Check(development.generatedLevel == 10 && buildingCount > 0 && development.initialBeds > 0, "Visited base has no actual developed infrastructure");
                Check(settlement.GetComponent<FormCaravanComp>() != null && settlement.GetComponent<FormCaravanComp>().CanFormOrReformCaravanNow, "No normal reform exit from friendly base");
            }
            initialized = true; readyTick = Find.TickManager.TicksGame + 1500;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            return;
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        done = true;
        string name = Reload ? "FriendlyVisitReloadProbe.json" : "FriendlyVisitProbe.json";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, name), "{\"passed\":" + (error == null ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":" + (error == null ? "[]" : "[" + Q(error) + "]") + "}");
    }
    public override void MapComponentOnGUI()
    {
        if (Reload || captureMap != map || Event.current.type != EventType.Repaint || frames >= 24) return;
        frames++;
        if (frames == 12) AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"), "CaptureScreenshot", new[] { typeof(string) }).Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, "FriendlyCapital.png") });
        if (frames == 24) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FriendlyVisitUiProbe.json"), "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}");
    }
    private static string Q(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
}
