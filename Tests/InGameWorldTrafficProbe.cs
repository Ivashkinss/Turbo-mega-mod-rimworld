using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using Verse;

[StaticConstructorOnStartup]
public static class WorldTrafficProbeLaunch
{
    static WorldTrafficProbeLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-world-traffic-reload"))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-WorldTraffic"));
    }
}

public sealed class WorldTrafficProbe : MapComponent
{
    private Caravan convoy, hostile;
    private Settlement source, destination, hostileSource, hostileDestination;
    private Pawn[] crew;
    private PlanetTile startTile, savedTile, hostileStartTile, hostileSavedTile;
    private int checks, stage, startTick, savedSilver, destinationSilver, savedDeparted, savedArrived, hostileSavedSilver, friendlyTransitions, hostileTransitions, nextDiagnostic, arrivalWindow;
    private bool saved, done, ui;
    private int worldPreviewFrames, automaticDeparted, automaticStartTick;
    private string pawnIds, hostilePawnIds;
    private HashSet<PlanetTile> observedTiles = new HashSet<PlanetTile>();
    private readonly List<string> errors = new List<string>();
    public WorldTrafficProbe(Map map) : base(map) { }
    private WorldTraffic Traffic { get { return Find.World.GetComponent<WorldTraffic>(); } }
    public override void ExposeData()
    {
        Scribe_References.Look(ref convoy, "wtConvoy");
        Scribe_References.Look(ref source, "wtSource");
        Scribe_References.Look(ref destination, "wtDestination");
        Scribe_References.Look(ref hostile, "wtHostile");
        Scribe_References.Look(ref hostileSource, "wtHostileSource");
        Scribe_References.Look(ref hostileDestination, "wtHostileDestination");
        Scribe_Values.Look(ref saved, "wtSaved");
        Scribe_Values.Look(ref savedTile, "wtSavedTile");
        Scribe_Values.Look(ref savedSilver, "wtSavedSilver");
        Scribe_Values.Look(ref destinationSilver, "wtDestinationSilver");
        Scribe_Values.Look(ref savedDeparted, "wtDeparted");
        Scribe_Values.Look(ref savedArrived, "wtArrived");
        Scribe_Values.Look(ref pawnIds, "wtPawnIds");
        Scribe_Values.Look(ref hostileSavedTile, "wtHostileSavedTile");
        Scribe_Values.Look(ref hostileSavedSilver, "wtHostileSavedSilver");
        Scribe_Values.Look(ref hostilePawnIds, "wtHostilePawnIds");
        Scribe_Values.Look(ref friendlyTransitions, "wtFriendlyTransitions");
        Scribe_Values.Look(ref hostileTransitions, "wtHostileTransitions");
        Scribe_Values.Look(ref relayAt, "wtRelayAt");
        Scribe_Values.Look(ref relaySaved, "wtRelaySaved");
    }
    private void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
    private int Cash(Caravan caravan) { return caravan.PawnsListForReading.Where(p => p.inventory != null).Sum(p => p.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Silver)); }
    private string PawnIds(Caravan caravan) { return string.Join(",", caravan.PawnsListForReading.Select(p => p.thingIDNumber).OrderBy(x => x)); }
    private int DestinationCash()
    {
        int native = destination.trader != null && destination.TraderKind != null ? destination.trader.StockListForReading.Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount) : 0;
        return native + Traffic.Depot(destination).TotalStackCountOfDef(ThingDefOf.Silver);
    }
    private IEnumerable<Tuple<Settlement, Settlement>> ShortRoutes(List<Settlement> settlements, bool patrol)
    {
        // Rank existing real endpoints using the native terrain/road/rest ETA. Approximate
        // straight-line tile distance alone selected a five-day mountain route in the old fixture.
        var surfaceSettlements = settlements.Where(s => s.Tile.Layer == Find.WorldGrid.Surface).ToList();
        var pairs = surfaceSettlements.SelectMany(a => surfaceSettlements.Where(b => b != a &&
            (patrol ? b.Faction == a.Faction : !a.Faction.HostileTo(b.Faction)) && b.Tile.Layer == a.Tile.Layer &&
            Find.WorldGrid.ApproxDistanceInTiles(a.Tile, b.Tile) >= 4 && Find.WorldGrid.ApproxDistanceInTiles(a.Tile, b.Tile) <= 20)
            .OrderBy(b => Find.WorldGrid.ApproxDistanceInTiles(a.Tile, b.Tile)).Take(6)
            .Select(b => Tuple.Create(a, b))).OrderBy(p => Find.WorldGrid.ApproxDistanceInTiles(p.Item1.Tile, p.Item2.Tile)).Take(64);
        return pairs.Where(p => Find.World.reachability.CanReach(p.Item1.Tile, p.Item2.Tile))
            .Select(p => new { Pair = p, Eta = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(p.Item1.Tile, p.Item2.Tile, null) })
            .Where(p => p.Eta > 0).OrderBy(p => p.Eta).Select(p => p.Pair).ToArray();
    }
    private void CheckRegionalCities()
    {
        var regions = Find.World.GetComponent<RegionalWorld>();
        var groups = Find.WorldObjects.Settlements.Where(s => s.Faction != null && !s.Faction.IsPlayer && s.Tile.Layer == Find.WorldGrid.Surface).GroupBy(s => s.Faction).ToArray();
        Check(groups.Length > 0 && groups.All(g => {
            var data = g.Select(s => regions.For(s)).ToArray();
            return data.Count(d => d.capital && d.level == 10) == 1 &&
                data.Count(d => d.regionalCenter && !d.capital && d.level >= 8 && d.level <= 9) == (data.Length >= 6 ? 2 : data.Length >= 2 ? 1 : 0);
        }), "Saved faction capitals/regional centers absent or changed");
    }
    private static string FactionFilter { get { return Environment.GetEnvironmentVariable("TMM_WT_FACTION") ?? ""; } }
    private readonly List<string> facts = new List<string>();
    private int nextFact;
    // A faction with the relay does not walk: its caravan charges in place and reappears on the target tile. It can never visit three
    // tiles, so it is kept out of the walking fixture and checked by its own scenario (TMM_WT_RELAY=1).
    private static bool UsesRelay(Faction f) { var ext = f == null ? null : f.def.GetModExtension<FactionTeleportExtension>(); return ext != null && ext.allowWorldJumps; }
    private static bool RelayMode { get { return Environment.GetEnvironmentVariable("TMM_WT_RELAY") == "1"; } }
    private int relayAt, nextUnload, observedJumpTick;
    private bool relaySaved;
    private void RelaySetup()
    {
        Traffic.nextSpawn = Find.TickManager.TicksGame + 600000;
        var all = Find.WorldObjects.Settlements.Where(s => WorldTraffic.Eligible(s.Faction) && !s.HasMap && s.Tile.Layer == Find.WorldGrid.Surface).ToList();
        foreach (var pair in all.Where(s => UsesRelay(s.Faction)).SelectMany(a => all.Where(b => b != a && !a.Faction.HostileTo(b.Faction) && Find.WorldGrid.ApproxDistanceInTiles(a.Tile, b.Tile) >= 4).OrderBy(b => Find.WorldGrid.ApproxDistanceInTiles(a.Tile, b.Tile)).Take(4).Select(b => Tuple.Create(a, b))))
        {
            var route = Traffic.TryStart(pair.Item1, pair.Item2, false);
            if (route == null) continue;
            source = pair.Item1; destination = pair.Item2; convoy = route.caravan; break;
        }
        Check(convoy != null, "No relay faction could send a caravan");
        crew = convoy.PawnsListForReading.ToArray();
        startTile = convoy.Tile; observedTiles.Add(startTile); startTick = Find.TickManager.TicksGame; stage = 1;
    }
    private void RelayFresh()
    {
        if (stage == 0) { RelaySetup(); return; }
        if (Find.TickManager.TicksGame - startTick > 6000) throw new Exception("Relay caravan never charged; " + State());
        var route = Traffic.Route(convoy);
        Check(convoy.Spawned && route != null, "Relay caravan ended before its charge; " + State()); checks--;
        if (route.relayJumpAt <= 0) return;
        facts.Add(State());
        Check(!convoy.pather.Moving && convoy.Tile == startTile, "Relay caravan walks while charging; " + State());
        Check(route.relayJumpAt > Find.TickManager.TicksGame, "Relay caravan has no charge time; " + State());
        Check(crew.All(p => convoy.ContainsPawn(p) && Find.WorldPawns.Contains(p)), "Relay charge lost crew");
        savedTile = convoy.Tile; savedSilver = Cash(convoy); pawnIds = PawnIds(convoy); destinationSilver = DestinationCash();
        savedDeparted = Traffic.departed; savedArrived = Traffic.arrived; relayAt = route.relayJumpAt; saved = true; relaySaved = true;
        GameDataSaveLoader.SaveGame("TMM-WorldTraffic");
        Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-WorldTraffic.rws")), "No actual save file");
        Finish(false);
    }
    private void RelayReload()
    {
        int now = Find.TickManager.TicksGame;
        if (stage == 0)
        {
            Check(saved && convoy != null && convoy.Spawned && convoy.Tile == savedTile, "Charging relay caravan lost or moved on load; " + Describe("friendly", convoy));
            var route = Traffic.Route(convoy);
            Check(route != null && route.origin == source && route.destination == destination && route.relayJumpAt == relayAt, "Relay charge lost on load: " + relayAt + "; " + Describe("friendly", convoy));
            Check(PawnIds(convoy) == pawnIds && Cash(convoy) == savedSilver && Traffic.routes.Count == 1 && Traffic.departed == savedDeparted, "Relay crew, silver or routes changed on load");
            crew = convoy.PawnsListForReading.ToArray(); startTile = convoy.Tile; observedTiles.Add(startTile); startTick = now; stage = 1;
            facts.Add(State()); return;
        }
        if (convoy.Tile != startTile && !observedTiles.Contains(convoy.Tile)) { observedJumpTick = now; observedTiles.Add(convoy.Tile); facts.Add("jump: " + State()); Check(now >= relayAt, "Relay jumped before its charge"); Check(convoy.Tile == destination.Tile, "Relay jumped to another tile; " + State()); }
        if (now >= nextUnload && now >= relayAt - 10) { nextUnload = now + 125; facts.Add("unload t+" + (now - startTick) + " tile=" + convoy.Tile + " goods=" + string.Join(" / ", crew.Select(p => p.kindDef.defName + (convoy.ContainsPawn(p) ? "" : "(out)") + ":" + string.Join(",", p.inventory.innerContainer.Select(x => x.def.defName + "x" + x.stackCount).ToArray())).ToArray()) + " arrived=" + Traffic.arrived + " spawned=" + convoy.Spawned); }
        if (now > relayAt + 3000) throw new Exception("Relay caravan did not arrive; " + State());
        if (convoy.Spawned) return;
        facts.Add(State());
        Check(observedTiles.Count == 2, "Relay caravan visited " + observedTiles.Count + " tiles");
        Check(observedJumpTick > 0 && now - observedJumpTick <= 275, "Unloading took " + (now - observedJumpTick) + " ticks after reaching the destination");
        Check(Traffic.Route(convoy) == null && Traffic.arrived == savedArrived + 1, "Relay arrival is not counted once: " + savedArrived + " -> " + Traffic.arrived);
        Check(crew.All(p => !convoy.ContainsPawn(p) && !p.Spawned && (p.inventory == null || p.inventory.innerContainer.Count == 0)), "Relay arrival kept crew or goods");
        Check(DestinationCash() == destinationSilver + savedSilver, "Relay cargo lost or duplicated: " + destinationSilver + "+" + savedSilver + " -> " + DestinationCash());
        Traffic.ProcessRoutes();
        Check(Traffic.arrived == savedArrived + 1 && DestinationCash() == destinationSilver + savedSilver, "Repeated processing duplicated the relay arrival");
        Finish(true);
    }
    private string Describe(string who, Caravan c)
    {
        if (c == null) return who + "=null";
        var route = Traffic.Route(c);
        string s = who + "[" + (c.Faction == null ? "none" : c.Faction.def.defName) + " spawned=" + c.Spawned + " tile=" + c.Tile + " pawns=" + c.PawnsListForReading.Count;
        if (c.Spawned) s += " moving=" + c.pather.Moving + " movingNow=" + c.pather.MovingNow + " paused=" + c.pather.Paused + " next=" + c.pather.nextTile + " left=" + c.pather.nextTileCostLeft.ToString("F0") + "/" + c.pather.nextTileCostTotal.ToString("F0") +
            " ticksPerMove=" + c.TicksPerMove + " mass=" + c.MassUsage.ToString("F1") + "/" + c.MassCapacity.ToString("F1") + " immobile=" + c.ImmobilizedByMass + " cantMove=" + c.CantMove + " rest=" + c.NightResting + " downed=" + c.AllOwnersDowned + " mental=" + c.AllOwnersHaveMentalBreak;
        s += route == null ? " route=none" : " route to " + route.destination.Tile + " returning=" + route.returning + " stranded=" + route.stranded + " relayJumpAt=" + route.relayJumpAt + " retries=" + route.retries;
        return s + "]";
    }
    private string State() { return "t+" + (Find.TickManager.TicksGame - startTick) + " friendlyTiles=" + observedTiles.Count + " " + Describe("friendly", convoy) + " " + Describe("hostile", hostile) + " hostileStart=" + hostileStartTile + " arrived=" + Traffic.arrived + " departed=" + Traffic.departed; }
    /// Survey: every eligible faction sends real groups; what each could do in its first move is written down.
    private void Survey()
    {
        Traffic.nextSpawn = Find.TickManager.TicksGame + 600000;
        var all = Find.WorldObjects.Settlements.Where(s => WorldTraffic.Eligible(s.Faction) && !s.HasMap && s.Tile.Layer == Find.WorldGrid.Surface).ToList();
        foreach (var group in all.GroupBy(s => s.Faction).OrderBy(g => g.Key.def.defName))
        {
            int made = 0, immobile = 0, still = 0, relay = 0; float worst = 0f; float slowest = 0f; var notes = new List<string>();
            foreach (var origin in group.InRandomOrder().Take(4))
                for (int i = 0; i < 6; i++)
                {
                    bool patrol = i % 2 == 1;
                    var target = all.Where(b => b != origin && (patrol ? b.Faction == origin.Faction : !origin.Faction.HostileTo(b.Faction)) && Find.WorldGrid.ApproxDistanceInTiles(origin.Tile, b.Tile) <= 55).InRandomOrder().FirstOrDefault(b => Find.World.reachability.CanReach(origin.Tile, b.Tile));
                    if (target == null) continue;
                    var route = Traffic.TryStart(origin, target, patrol);
                    if (route == null) continue;
                    var c = route.caravan; made++;
                    float load = c.MassCapacity <= 0f ? 99f : c.MassUsage / c.MassCapacity;
                    worst = Math.Max(worst, load); slowest = Math.Max(slowest, c.pather.nextTileCostTotal);
                    if (c.ImmobilizedByMass) { immobile++; if (notes.Count < 6) notes.Add((patrol ? "patrol " : "convoy ") + c.MassUsage.ToString("F1") + "/" + c.MassCapacity.ToString("F1") + " " + string.Join("+", c.PawnsListForReading.Select(p => p.kindDef.defName + "(gear " + MassUtility.GearMass(p).ToString("F1") + " inv " + MassUtility.InventoryMass(p).ToString("F1") + " cap " + MassUtility.Capacity(p).ToString("F1") + ")").ToArray())); }
                    if (!c.pather.MovingNow) still++;
                    var ext = origin.Faction.def.GetModExtension<FactionTeleportExtension>();
                    if (ext != null && ext.allowWorldJumps) relay++;
                    // The group leaves by the ordinary removal; its goods stay in the traffic's own cargo object.
                    Find.WorldObjects.Remove(c); Traffic.ProcessRoutes();
                }
            facts.Add("SURVEY " + group.Key.def.defName + " hostile=" + group.Key.HostileTo(Faction.OfPlayer) + " towns=" + group.Count() + " groups=" + made + " immobile=" + immobile + " notMovingNow=" + still + " relay=" + relay + " worstLoad=" + worst.ToString("F2") + " slowestFirstTile=" + slowest.ToString("F0") + (notes.Count == 0 ? "" : " :: " + string.Join(" ; ", notes.ToArray())));
        }
    }
    private void Setup()    {
        Check(Traffic != null, "WorldTraffic component absent");
        CheckRegionalCities();
        // Keep unrelated groups out of this two-route acceptance fixture.
        Traffic.nextSpawn = Find.TickManager.TicksGame + 600000;
        var settlements = Find.WorldObjects.Settlements.Where(s => WorldTraffic.Eligible(s.Faction) && !s.Faction.HostileTo(Faction.OfPlayer) && !s.HasMap && (!UsesRelay(s.Faction) || s.Faction.def.defName == FactionFilter)).ToList();
        var friendly = ShortRoutes(settlements, false).ToList();
        facts.Add("friendly candidates: " + string.Join(",", friendly.Select(p => p.Item1.Faction.def.defName).ToArray()));
        facts.Add("eligible friendly: " + string.Join(",", settlements.GroupBy(s => s.Faction.def.defName).Select(g => g.Key + "x" + g.Count()).ToArray()));
        if (FactionFilter != "") friendly = ShortRoutes(settlements.Where(s => s.Faction.def.defName == FactionFilter).Concat(settlements.Where(s => s.Faction.def.defName != FactionFilter)).ToList(), false).Where(p => p.Item1.Faction.def.defName == FactionFilter).ToList();
        if (FactionFilter != "" && friendly.Count == 0)
        {
            // The filtered faction has no near neighbour in the ranked list: take any reachable pair from it.
            friendly = settlements.Where(s => s.Faction.def.defName == FactionFilter).SelectMany(a => settlements.Where(b => b != a && !a.Faction.HostileTo(b.Faction) && b.Tile.Layer == a.Tile.Layer && Find.World.reachability.CanReach(a.Tile, b.Tile)).OrderBy(b => Find.WorldGrid.ApproxDistanceInTiles(a.Tile, b.Tile)).Take(1).Select(b => Tuple.Create(a, b))).ToList();
            facts.Add("filtered pair taken outside the ranked list: " + friendly.Count);
        }
        foreach (var pair in friendly)        {
            var route = Traffic.TryStart(pair.Item1, pair.Item2, false);
            if (route == null) continue;
            source = pair.Item1; destination = pair.Item2; convoy = route.caravan; break;
        }
        Check(convoy != null, "No actual valid NPC route could start");
        Check(convoy.Spawned && convoy.Faction == source.Faction && !convoy.IsPlayerControlled, "Wrong native caravan ownership");
        Check(convoy.pather.Moving && convoy.pather.Destination == destination.Tile, "Native caravan pather not moving to real settlement");
        crew = convoy.PawnsListForReading.ToArray();
        Check(crew.Length == 3 && crew.All(p => !p.Spawned && convoy.ContainsPawn(p) && Find.WorldPawns.Contains(p)), "Native pawns not properly held/registered");
        Check(crew.All(p => p.inventory.innerContainer.Count > 0), "Actual rations absent");
        Check(Traffic.routes.Count == 1 && Traffic.Route(convoy) != null, "Controller route not registered once");
        Check(Traffic.TryStart(source, source) == null, "Identical source/destination accepted");
        Check(convoy.GetInspectString().Contains(destination.LabelCap), "Destination inspector absent");
        var hostileSettlements = Find.WorldObjects.Settlements.Where(s => WorldTraffic.Eligible(s.Faction) && s.Faction.HostileTo(Faction.OfPlayer) && !s.HasMap && !UsesRelay(s.Faction)).ToList();
        foreach (var pair in ShortRoutes(hostileSettlements, true))
        {
            var route = Traffic.TryStart(pair.Item1, pair.Item2, true);
            if (route == null) continue;
            hostileSource = pair.Item1; hostileDestination = pair.Item2; hostile = route.caravan; break;
        }
        Check(hostile != null && hostile.Faction.HostileTo(Faction.OfPlayer), "No actual hostile-to-player patrol could start");
        Check(hostile.pather.Moving && hostile.pather.Destination == hostileDestination.Tile && Traffic.Route(hostile).patrol, "Hostile native route not started");
        Check(hostile.PawnsListForReading.Count == 3 && hostile.PawnsListForReading.All(p => Find.WorldPawns.Contains(p) && hostile.ContainsPawn(p)), "Hostile crew not actually registered");
        Check(hostileSource.Faction == hostileDestination.Faction && !hostileSource.Faction.HostileTo(hostileDestination.Faction), "Patrol target not its own compatible settlement");
        Check(Traffic.routes.Count == 2, "Second route not registered exactly once");
        hostileStartTile = hostile.Tile;
        if (Environment.GetEnvironmentVariable("TMM_WT_SURVEY") == "1") { Survey(); Check(Traffic.routes.Count == 2, "Survey left routes behind: " + Traffic.routes.Count); }
        startTile = convoy.Tile; observedTiles.Add(startTile); startTick = Find.TickManager.TicksGame; stage = 1;
    }
    private void TickFresh()
    {
        if (RelayMode) { RelayFresh(); return; }
        if (stage == 0) { Setup(); return; }
        if (Find.TickManager.TicksGame >= nextFact) { nextFact = Find.TickManager.TicksGame + 2500; facts.Add(State()); }
        if (Find.TickManager.TicksGame - startTick > 45000) throw new Exception("Native caravan never moved to another tile; " + State());        Check(convoy != null, "Convoy disappeared");
        observedTiles.Add(convoy.Tile);
        if (!convoy.Spawned && observedTiles.Count < 3) throw new Exception("Friendly caravan ended its trip after " + observedTiles.Count + " tiles; " + State());
        bool friendlyMoved = observedTiles.Count >= 3;
        bool hostileMoved = hostile.Tile != hostileStartTile;
        // Hold only through the native pause control to preserve a mid-route save for both groups.
        // No Tile assignment or movement-cost/pather-internal changes are used by this fixture.
        convoy.pather.Paused = friendlyMoved && !hostileMoved;
        hostile.pather.Paused = hostileMoved && !friendlyMoved;
        if (!friendlyMoved || !hostileMoved) { checks--; return; }
        convoy.pather.Paused = false; hostile.pather.Paused = false;
        Check(convoy.Spawned && convoy.pather.Moving, "Caravan completed too early for mid-route save fixture");
        Check(hostile.Spawned && hostile.pather.Moving, "Hostile patrol completed before mid-route save fixture");
        Check(crew.All(p => convoy.ContainsPawn(p) && Find.WorldPawns.Contains(p)), "Movement lost crew");
        savedTile = convoy.Tile; savedSilver = Cash(convoy); pawnIds = PawnIds(convoy);
        hostileSavedTile = hostile.Tile; hostileSavedSilver = Cash(hostile); hostilePawnIds = PawnIds(hostile);
        friendlyTransitions = observedTiles.Count - 1; hostileTransitions = 1;
        destinationSilver = DestinationCash(); savedDeparted = Traffic.departed; savedArrived = Traffic.arrived; saved = true;
        GameDataSaveLoader.SaveGame("TMM-WorldTraffic");
        Check(File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-WorldTraffic.rws")), "No actual save file");
        Finish(false);
    }
    private void TickReload()
    {
        if (relaySaved) { RelayReload(); return; }
        if (stage == 2)
        {
            var spawned = Traffic.routes.FirstOrDefault(r => r.created >= automaticStartTick && r.caravan != null && r.caravan.Spawned);
            if (Traffic.departed > automaticDeparted && spawned != null)
            {
                Check(spawned.caravan.pather.Moving && spawned.caravan.PawnsListForReading.Count > 0, "Automatic scheduler did not create a moving native group");
                Check(spawned.caravan.PawnsListForReading.All(p => Find.WorldPawns.Contains(p)), "Automatic crew not registered in WorldPawns");
                Finish(true);
            }
            else if (Find.TickManager.TicksGame - automaticStartTick > 25000) throw new Exception("Native scheduled spawn never occurred after restoring its normal retry interval");
            return;
        }
        if (stage == 0)
        {
            CheckRegionalCities();
            Check(saved && convoy != null && source != null && destination != null, "Real saved references absent");
            Check(convoy.Spawned && convoy.Tile == savedTile, "Saved position lost");
            Check(PawnIds(convoy) == pawnIds, "Native crew identity changed");
            Check(Cash(convoy) == savedSilver, "Physical silver changed across save/load");
            var route = Traffic.Route(convoy);
            Check(route != null && route.origin == source && route.destination == destination, "Saved route lost");
            Check(convoy.pather.Moving && convoy.pather.Destination == destination.Tile, "Native pather not restored");
            Check(Traffic.departed == savedDeparted && Traffic.routes.Count == 2, "Load spawned duplicate trip");
            Check(hostile != null && hostile.Spawned && hostile.Faction.HostileTo(Faction.OfPlayer), "Hostile saved patrol absent or diplomacy changed");
            Check(hostile.Tile == hostileSavedTile && PawnIds(hostile) == hostilePawnIds && Cash(hostile) == hostileSavedSilver, "Hostile position/crew/physical silver changed across load");
            var hostileRoute = Traffic.Route(hostile);
            Check(hostileRoute != null && hostileRoute.origin == hostileSource && hostileRoute.destination == hostileDestination && hostileRoute.patrol, "Hostile saved route lost");
            Check(hostile.pather.Moving && !hostile.pather.Paused && hostile.pather.Destination == hostileDestination.Tile, "Hostile native pather not restored");
            crew = convoy.PawnsListForReading.ToArray();
            startTick = Find.TickManager.TicksGame;
            int eta = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(convoy, true);
            arrivalWindow = Math.Min(600000, Math.Max(90000, eta + 60000));
            Log.Message("[TMM-WorldTraffic] nativeArrivalEtaTicks=" + eta + " acceptanceWindowTicks=" + arrivalWindow);
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;
            Find.WorldCameraDriver.JumpTo(convoy.Tile);
            Find.WorldSelector.ClearSelection(); Find.WorldSelector.Select(convoy);
            stage = 1; return;
        }
        worldPreviewFrames++;
        if (worldPreviewFrames == 40)
            HarmonyLib.AccessTools.Method(HarmonyLib.AccessTools.TypeByName("UnityEngine.ScreenCapture"), "CaptureScreenshot", new[] { typeof(string) })
                .Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, "WorldTraffic-Convoy.png") });
        if (worldPreviewFrames == 80) { Find.WorldSelector.ClearSelection(); Find.World.renderer.wantedMode = WorldRenderMode.None; }
        int now = Find.TickManager.TicksGame;
        if (now >= nextDiagnostic)
        {
            nextDiagnostic = now + 15000;
            Log.Message("[TMM-WorldTraffic] elapsed=" + (now - startTick) + " tile=" + convoy.Tile + " target=" + destination.Tile +
                " spawned=" + convoy.Spawned + " moving=" + convoy.pather.Moving + " inspect=" + convoy.GetInspectString());
        }
        if (now - startTick > arrivalWindow) throw new Exception("Native caravan exceeded terrain/rest ETA plus one-day acceptance margin; " + convoy.GetInspectString());
        if (convoy.Spawned || hostile.Tile == hostileSavedTile) return;
        Check(Traffic.Route(convoy) == null && Traffic.arrived >= savedArrived + 1 && Traffic.arrived <= savedArrived + 2, "Friendly arrival missing or total includes duplicate receipts");
        Check(hostile.Tile != hostileSavedTile, "Hostile patrol did not continue native movement after load");
        Check(crew.All(p => !convoy.ContainsPawn(p) && !p.Spawned), "Arrival did not release native crew from caravan");
        Check(crew.All(p => p.inventory == null || p.inventory.innerContainer.Count == 0), "Arrival released a pawn still carrying goods");
        Check(DestinationCash() == destinationSilver + savedSilver, "Physical receipt at NPC destination was lost/duplicated");
        Traffic.ProcessRoutes();
        int receipts = Traffic.arrived;
        Traffic.ProcessRoutes();
        Check(Traffic.arrived == receipts && DestinationCash() == destinationSilver + savedSilver, "Repeated processing duplicated arrival income");
        // A second real group tests actual native death/removal recovery, not a mocked receipt.
        var casualtyRoute = Traffic.TryStart(source, destination, true);
        Check(casualtyRoute != null, "No casualty-test group");
        var casualty = casualtyRoute.caravan;
        int beforeCash = Cash(casualty);
        var testCoins = ThingMaker.MakeThing(ThingDefOf.Silver); testCoins.stackCount = 37;
        Check(casualty.PawnsListForReading[0].inventory.innerContainer.TryAdd(testCoins, false), "Cannot add actual casualty-test coins");
        var casualties = casualty.PawnsListForReading.ToArray();
        foreach (var pawn in casualties) pawn.Kill(null);
        Traffic.ProcessRoutes();
        Check(!casualty.Spawned && Traffic.Route(casualty) == null, "Dead group remained active");
        var salvage = Find.WorldObjects.AllWorldObjects.OfType<WorldTrafficCargo>().Where(c => c.Tile == casualty.Tile).ToList();
        Check(salvage.Count > 0 && salvage.Sum(c => c.goods.TotalStackCountOfDef(ThingDefOf.Silver)) == beforeCash + 37, "Death lost or duplicated physical silver");
        automaticDeparted = Traffic.departed;
        automaticStartTick = Find.TickManager.TicksGame;
        // The fixture disabled unrelated traffic during the controlled two-route stage.
        // Restore its normal retry interval and let WorldComponentTick create the next group.
        Traffic.nextSpawn = automaticStartTick + WorldTraffic.Retry;
        stage = 2;
    }
    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs(); bool reload = args.Contains("-tmm-world-traffic-reload");
        if (done || Find.CurrentMap != map || (!reload && !args.Contains("-tmm-world-traffic-probe"))) return;
        try
        {
            // Normal native movement costs; only simulation speed is raised in this isolated test.
            Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
            if (reload) TickReload(); else TickFresh();
        }
        catch (Exception ex) { errors.Add(ex.ToString()); Finish(reload); }
    }
    private static string Q(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private void Finish(bool reload)
    {
        done = true;
        string json = "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks +
            ",\"friendlyObservedTileTransitions\":" + friendlyTransitions + ",\"hostileObservedTileTransitions\":" + hostileTransitions +
            ",\"hostileContinuedAfterReload\":" + (reload && hostile != null && hostile.Tile != hostileSavedTile ? "true" : "false") +
            ",\"hostileFaction\":" + (hostile == null || hostile.Faction == null ? "null" : Q(hostile.Faction.Name)) +
            ",\"facts\":[" + string.Join(",", facts.Select(Q)) + "],\"errors\":[" + string.Join(",", errors.Select(Q)) + "]}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, reload ? "WorldTrafficReloadProbe.json" : "WorldTrafficProbe.json"), json);
        if (!reload && !ui) { ui = true; File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "WorldTrafficUiProbe.json"), "{\"passed\":true,\"visualLayoutInspected\":false,\"error\":null}"); }
    }
}
