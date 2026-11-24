using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    /// <summary>
    /// Checks the integrated Vehicle Framework (SmashPhil, MIT) on RimWorld 1.7 and our first vehicle on it.
    /// Fresh (-tmm-vehicle-framework-probe): assemblies, defs, Harmony patches; spawn TMM_Vehicle_HunterTruck through
    /// VF's own spawner, let the map run with it, drive locally, board driver and passenger, form a VehicleCaravan,
    /// travel to neighbor world tile, verify fuel consumption, then save TMM-Vehicles.
    /// Reload (-tmm-vehicle-framework-reload): load TMM-Vehicles and find the vehicle caravan, hunter truck,
    /// boarded passenger, colony relative spouse relation, and consumed fuel level intact.
    /// The probe uses reflection only for VF classes, so it compiles without referencing Vehicles.dll.
    /// </summary>
    public sealed class InGameVehicleFrameworkProbe : MapComponent
    {
        private static bool s_done;
        private static bool s_loadRequested;
        private int m_ticks;
        private int m_spawnTick = -1;
        private Thing m_vehicle;
        private IntVec3 m_startPos = IntVec3.Invalid;
        private IntVec3 m_dest = IntVec3.Invalid;
        private string m_driveNote = "not started";
        private int m_mapDriveDistance = -1;

        private Caravan m_caravan;
        private PlanetTile m_startTile = PlanetTile.Invalid;
        private PlanetTile m_destTile = PlanetTile.Invalid;
        private float m_fuelBeforeCaravan = -1f;
        private float m_fuelAfterCaravan = -1f;
        private bool m_caravanStarted;
        private bool m_caravanFormed;
        private bool m_caravanDeparted;
        private bool m_caravanArrived;
        private int m_caravanArrivedTick = -1;
        private string m_caravanNote = "not started";

        private readonly List<string> m_checks = new List<string>();
        private bool m_allPassed = true;
        private const string SaveName = "TMM-Vehicles";
        private const string TruckDef = "TMM_Vehicle_HunterTruck";

        // Return home with crew and cargo (user playtest 24.11.2026, item 10).
        private Pawn m_impala;
        private Zone_Stockpile m_store;
        private int m_homeStart = -1, m_homeStage, m_stageTick;
        private string m_homeNote = "not started";
        private List<Pawn> m_homeCrew = new List<Pawn>();
        private static readonly string[] CargoDefs = { "Gold", "Jade", "ComponentSpacer" };
        private static readonly int[] CargoCounts = { 35, 20, 4 };
        private static bool HomeSave { get { return Environment.GetEnvironmentVariable("TMM_VF_SAVE") == "home"; } }
        private const string HomeSaveName = "TMM-VehiclesHome";

        public InGameVehicleFrameworkProbe(Map map) : base(map) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref m_impala, "vfImpala");
            Scribe_Collections.Look(ref m_base, "vfBase", LookMode.Value);
        }

        private List<int> m_base = new List<int>();   // the same defs that other pawns carried before the trip
        private int CountEverywhere(string def)
        {
            int at = Array.IndexOf(CargoDefs, def);
            int n = at >= 0 && m_base != null && at < m_base.Count ? -m_base[at] : 0;
            foreach (Thing t in map.listerThings.ThingsOfDef(ThingDef.Named(def))) n += t.stackCount;
            foreach (Pawn p in map.mapPawns.AllPawns)
            {
                if (p.inventory != null) n += p.inventory.innerContainer.Where(t => t.def.defName == def).Sum(t => t.stackCount);
                if (p.carryTracker != null && p.carryTracker.CarriedThing != null && p.carryTracker.CarriedThing.def.defName == def) n += p.carryTracker.CarriedThing.stackCount;
            }
            return n;
        }
        private int OnGround(string def) { return map.listerThings.ThingsOfDef(ThingDef.Named(def)).Sum(t => t.stackCount); }
        private int InStore(string def) { return m_store == null ? 0 : map.listerThings.ThingsOfDef(ThingDef.Named(def)).Where(t => t.Position.GetZone(map) == m_store).Sum(t => t.stackCount); }
        private string Totals() { return string.Join(", ", CargoDefs.Select(d => d + " all=" + CountEverywhere(d) + " ground=" + OnGround(d) + " store=" + InStore(d)).ToArray()); }
        private void Stage(int s) { m_homeStage = s; m_stageTick = m_ticks; }
        private static void AddCargo(Pawn vehicle, string def, int count)
        {
            Thing t = ThingMaker.MakeThing(ThingDef.Named(def)); t.stackCount = count;
            if (!vehicle.inventory.innerContainer.TryAdd(t, false)) throw new Exception("cargo not added: " + def);
        }
        private Caravan LeaveBy(Pawn vehicle, PlanetTile from)
        {
            PlanetTile to = FindPassableNeighborTile(from);
            var exit = AccessTools.Method(AccessTools.TypeByName("Vehicles.CaravanHelper"), "ExitMapAndCreateVehicleCaravan", new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(PlanetTile), typeof(PlanetTile), typeof(PlanetTile), typeof(bool) });
            return exit.Invoke(null, new object[] { new Pawn[] { vehicle }, Faction.OfPlayer, from, to, to, false }) as Caravan;
        }

        private void UpdateHome()
        {
            try
            {
                VehicleHomeArrival home = map.GetComponent<VehicleHomeArrival>();
                if (m_homeStage == 0)
                {
                    if (m_impala == null) { m_homeNote = "no return pickup"; Stage(9); return; }
                    for (int i = 0; i < 4; i++)
                    {
                        Pawn hauler = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false));
                        if (hauler.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)) { i--; continue; }
                        GenSpawn.Spawn(hauler, CellFinder.RandomClosewalkCellNear(map.Center, map, 10), map);
                        hauler.workSettings.EnableAndInitialize();
                        foreach (WorkTypeDef w in DefDatabase<WorkTypeDef>.AllDefs) if (!hauler.WorkTypeIsDisabled(w)) hauler.workSettings.SetPriority(w, w == WorkTypeDefOf.Hauling ? 1 : 0);
                    }
                    m_store = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
                    map.zoneManager.RegisterZone(m_store);
                    foreach (IntVec3 c in GenRadial.RadialCellsAround(map.Center, 5f, true))
                        if (c.InBounds(map) && c.Standable(map) && c.GetEdifice(map) == null && map.zoneManager.ZoneAt(c) == null && c.GetFirstItem(map) == null) m_store.AddCell(c);
                    m_store.settings.filter.SetDisallowAll();
                    foreach (string d in CargoDefs) m_store.settings.filter.SetAllow(ThingDef.Named(d), true);
                    Pawn rider = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false));
                    rider.Name = new NameTriple("VFHome", "Rider", "TMM");
                    GenSpawn.Spawn(rider, m_impala.Position + new IntVec3(2, 0, 0), map);
                    AccessTools.Method(m_impala.GetType(), "TryAddPawn", new Type[] { typeof(Pawn) }).Invoke(m_impala, new object[] { rider });
                    m_base.Clear();
                    foreach (string d in CargoDefs)
                    {
                        foreach (Thing t in map.listerThings.ThingsOfDef(ThingDef.Named(d)).ToList()) t.Destroy(); // fixture: none of it lies on the map
                    }
                    m_base = CargoDefs.Select(CountEverywhere).ToList();
                    for (int i = 0; i < CargoDefs.Length; i++) AddCargo(m_impala, CargoDefs[i], CargoCounts[i]);
                    m_homeCrew = ((System.Collections.IEnumerable)AccessTools.Property(m_impala.GetType(), "AllPawnsAboard").GetValue(m_impala, null)).Cast<Pawn>().ToList();
                    Check("Home_CrewOfTwoAboard", m_homeCrew.Count == 2, m_homeCrew.Count.ToString());
                    Caravan trip = LeaveBy(m_impala, map.Tile);
                    Check("Home_LeftMap", trip != null && !m_impala.Spawned, "caravan=" + (trip != null));
                    CaravanEnterMapUtility.Enter(trip, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, false, null);
                    // The vehicle enters at a random map edge. If this random map cuts that edge off from the fixture, bring the warehouse and its haulers to the vehicle's side.
                    if (!map.mapPawns.FreeColonistsSpawned.Where(p => !m_homeCrew.Contains(p) && p.CanReach(m_store.Cells[0], PathEndMode.OnCell, Danger.Deadly)).Any(p => p.CanReach(m_impala, PathEndMode.Touch, Danger.Deadly)))
                    {
                        IntVec3 near;
                        if (CellFinder.TryFindRandomCellNear(m_impala.Position, map, 45, c => c.Standable(map) && c.DistanceTo(m_impala.Position) > 15f && map.reachability.CanReach(c, m_impala, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors)), out near))
                        {
                            m_store.Delete();
                            m_store = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
                            map.zoneManager.RegisterZone(m_store);
                            foreach (IntVec3 c in GenRadial.RadialCellsAround(near, 5f, true))
                                if (c.InBounds(map) && c.Standable(map) && c.GetEdifice(map) == null && map.zoneManager.ZoneAt(c) == null && c.GetFirstItem(map) == null) m_store.AddCell(c);
                            m_store.settings.filter.SetDisallowAll();
                            foreach (string d in CargoDefs) m_store.settings.filter.SetAllow(ThingDef.Named(d), true);
                            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned.ToList()) if (!m_homeCrew.Contains(p) && p.workSettings.GetPriority(WorkTypeDefOf.Hauling) == 1) { p.Position = CellFinder.RandomClosewalkCellNear(near, map, 5); p.Notify_Teleported(true, false); }
                            Log.Message("[TMM probe] warehouse moved to " + near);
                        }
                    }
                    Check("Home_VehicleEntered", m_impala.Spawned && m_impala.Map == map, m_impala.Position.ToString());
                    Check("Home_CrewGotOut", m_homeCrew.All(p => p.Spawned && p.Map == map && !p.Dead) && VehicleHome.CrewCount(m_impala) == 0, "crew=" + string.Join(",", m_homeCrew.Select(p => p.LabelShort + ":" + p.Spawned).ToArray()) + " threat=" + GenHostility.AnyHostileActiveThreatToPlayer(map));
                    Check("Home_CargoKeptOnArrival", CargoDefs.Select((d, i) => m_impala.inventory.innerContainer.Where(t => t.def.defName == d).Sum(t => t.stackCount) == CargoCounts[i]).All(x => x), Totals());
                    Check("Home_UnloadingStarted", home.Unloading(m_impala) && VehicleHome.Requested(m_impala), "unloading=" + home.Unloading(m_impala));
                    m_homeStart = m_ticks;
                    Stage(1);
                    return;
                }
                if (m_homeStage == 1)
                {
                    // Repeated orders through the real button change nothing.
                    Gizmo order = m_impala.GetGizmos().FirstOrDefault(g => g is Command_Action && ((Command_Action)g).defaultLabel == "TMM_VehicleUnloadHome".Translate());
                    Check("Home_ButtonPresent", order != null, "checked");
                    if (order != null) { ((Command_Action)order).action(); ((Command_Action)order).action(); }
                    Check("Home_RepeatedOrderChangesNothing", home.Unloading(m_impala) && CargoDefs.Select((d, i) => CountEverywhere(d) == CargoCounts[i]).All(x => x), Totals());
                    Stage(2);
                    return;
                }
                if (m_homeStage == 2)
                {
                    bool someOut = CargoDefs.Any(d => OnGround(d) > 0), someIn = CargoDefs.Any(d => m_impala.inventory.innerContainer.Any(t => t.def.defName == d));
                    if (someOut && someIn)
                    {
                        Check("Home_CrewMovesOn", m_homeCrew.All(p => p.Spawned && p.CurJob != null), string.Join(",", m_homeCrew.Select(p => p.CurJobDef == null ? "none" : p.CurJobDef.defName).ToArray()));
                        GameDataSaveLoader.SaveGame(HomeSaveName);
                        m_homeNote = "saved in the middle of unloading: " + Totals();
                        Stage(3);
                        return;
                    }
                    if (m_ticks - m_stageTick > 12000) { Check("Home_HaulersCame", false, Totals()); Stage(9); }
                    return;
                }
                if (m_homeStage == 3)
                {
                    bool delivered = CargoDefs.Select((d, i) => InStore(d) == CargoCounts[i]).All(x => x);
                    if (!delivered && m_ticks - m_stageTick <= 15000) return;
                    Check("Home_CargoInStorageExactly", delivered && CargoDefs.Select((d, i) => CountEverywhere(d) == CargoCounts[i]).All(x => x), Totals() + " after " + (m_ticks - m_homeStart) + " ticks");
                    Stage(4);
                    return;
                }
                if (m_homeStage == 4)
                {
                    if (m_ticks - m_stageTick < 200) return;
                    Check("Home_UnloadingEnded", !home.Unloading(m_impala) && !VehicleHome.Requested(m_impala) && !m_impala.inventory.innerContainer.Any, "unloading=" + home.Unloading(m_impala));
                    // Cargo without any storage: ordered by hand.
                    AddCargo(m_impala, "Uranium", 15);
                    VehicleHome.Request(m_impala, false);
                    Check("Home_ParkedCargoStaysPut", !home.Unloading(m_impala), "checked");
                    home.Begin(m_impala, true);
                    Stage(5);
                    return;
                }
                if (m_homeStage == 5)
                {
                    bool placed = OnGround("Uranium") == 15;
                    if (!placed && m_ticks - m_stageTick <= 15000) return;
                    Check("Home_NoStorageCargoAvailable", placed && CountEverywhere("Uranium") == 15 && map.listerThings.ThingsOfDef(ThingDefOf.Uranium).All(t => !t.IsForbidden(Faction.OfPlayer)), "ground=" + OnGround("Uranium") + " all=" + CountEverywhere("Uranium"));
                    Stage(9);
                }
            }
            catch (Exception ex) { Check("Home_NoException", false, ex.GetBaseException().Message + " stage " + m_homeStage); Stage(9); }
        }

        /// Another map and an aircraft: nothing is unloaded there.
        private void TestElsewhere()
        {
            try
            {
                Settlement town = Find.WorldObjects.Settlements.First(s => s.Faction != null && !s.Faction.IsPlayer && !s.Faction.HostileTo(Faction.OfPlayer) && s.Faction.def.humanlikeFaction && !s.HasMap);
                Map townMap = GetOrGenerateMapUtility.GetOrGenerateMap(town.Tile, new IntVec3(150, 1, 150), null);
                Pawn driver = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false));
                GenSpawn.Spawn(driver, m_impala.Position + new IntVec3(2, 0, 0), map);
                AccessTools.Method(m_impala.GetType(), "TryAddPawn", new Type[] { typeof(Pawn) }).Invoke(m_impala, new object[] { driver });
                AddCargo(m_impala, "Plasteel", 12);
                Caravan trip = LeaveBy(m_impala, map.Tile);
                CaravanEnterMapUtility.Enter(trip, townMap, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, false, null);
                bool listed = townMap.GetComponent<VehicleHomeArrival>() != null && townMap.GetComponent<VehicleHomeArrival>().Unloading(m_impala);
                Check("Elsewhere_CrewAndCargoStayAboard", m_impala.Spawned && m_impala.Map == townMap && VehicleHome.CrewCount(m_impala) == 1 && !driver.Spawned && !listed && !VehicleHome.Requested(m_impala) &&
                    m_impala.inventory.innerContainer.Where(t => t.def.defName == "Plasteel").Sum(t => t.stackCount) == 12, "crew=" + VehicleHome.CrewCount(m_impala) + " listed=" + listed);
                Check("Elsewhere_NoButton", !m_impala.GetGizmos().Any(g => g is Command_Action && ((Command_Action)g).defaultLabel == "TMM_VehicleUnloadHome".Translate()), "checked");
            }
            catch (Exception ex) { Check("Elsewhere_NoException", false, ex.GetBaseException().Message); }
        }

        private int m_reloadStart = -1;
        private void UpdateHomeReload()
        {
            VehicleHomeArrival home = map.GetComponent<VehicleHomeArrival>();
            if (m_reloadStart < 0)
            {
                m_reloadStart = m_ticks;
                Check("HomeReload_VehicleAndOrderKept", m_impala != null && m_impala.Spawned && home.Unloading(m_impala) && VehicleHome.Requested(m_impala) && VehicleHome.CrewCount(m_impala) == 0, m_impala == null ? "null" : "unloading=" + home.Unloading(m_impala));
                Check("HomeReload_CargoExact", CargoDefs.Select((d, i) => CountEverywhere(d) == CargoCounts[i]).All(x => x), Totals());
                return;
            }
            m_store = map.zoneManager.AllZones.OfType<Zone_Stockpile>().FirstOrDefault(z => z.settings.filter.Allows(ThingDefOf.Gold) && !z.settings.filter.Allows(ThingDefOf.Steel));
            bool delivered = CargoDefs.Select((d, i) => InStore(d) == CargoCounts[i]).All(x => x);
            if (!delivered && m_ticks - m_reloadStart < 15000) return;
            s_done = true;
            Check("HomeReload_CargoInStorageExactly", delivered && CargoDefs.Select((d, i) => CountEverywhere(d) == CargoCounts[i]).All(x => x), Totals() + " after " + (m_ticks - m_reloadStart) + " ticks");
            Write("VehicleFrameworkReloadProbe.json", false);
        }

        private static bool Has(string flag)
        {
            foreach (string a in Environment.GetCommandLineArgs()) if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static bool Fresh { get { return Has("-tmm-vehicle-framework-probe"); } }
        private static bool Reload { get { return Has("-tmm-vehicle-framework-reload"); } }

        [StaticConstructorOnStartup]
        private static class Loader
        {
            static Loader()
            {
                if (!Reload) return;
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (s_loadRequested || Current.ProgramState != ProgramState.Entry) return;
                    s_loadRequested = true;
                    GameDataSaveLoader.LoadGame(HomeSave ? HomeSaveName : SaveName);
                });
            }
        }

        private void Check(string name, bool ok, string actual)
        {
            if (!ok) m_allPassed = false;
            m_checks.Add("{\"name\":\"" + name + "\",\"passed\":" + (ok ? "true" : "false") + ",\"actual\":\"" + (actual ?? "").Replace("\\", "/").Replace("\"", "'") + "\"}");
        }

        private static Def TruckVehicleDef()
        {
            Type t = AccessTools.TypeByName("Vehicles.VehicleDef");
            return t == null ? null : GenDefDatabase.GetDef(t, TruckDef, false);
        }

        private Thing FindTruck()
        {
            if (m_vehicle != null && !m_vehicle.Destroyed) return m_vehicle;
            foreach (Thing t in map.listerThings.AllThings) if (t.def != null && t.def.defName == TruckDef && t != m_impala) return t;
            foreach (Pawn p in map.mapPawns.AllPawns) if (p.def != null && p.def.defName == TruckDef && p != m_impala) return p;
            foreach (Caravan c in Find.WorldObjects.Caravans)
            {
                foreach (Pawn p in c.PawnsListForReading) if (p.def != null && p.def.defName == TruckDef && p != m_impala) return p;
            }
            foreach (Pawn p in Find.WorldPawns.AllPawnsAlive) if (p.def != null && p.def.defName == TruckDef && p != m_impala) return p;
            return null;
        }

        private Caravan FindVehicleCaravan()
        {
            foreach (Caravan c in Find.WorldObjects.Caravans)
            {
                if (c != null && c.GetType().Name == "VehicleCaravan") return c;
            }
            return null;
        }

        private Pawn FindPassenger(Thing truck)
        {
            Pawn pass = map.mapPawns.AllPawns.FirstOrDefault(p => p.Name != null && p.Name.ToStringFull.Contains("VFPassenger"));
            if (pass != null) return pass;
            if (truck != null)
            {
                var aboard = AccessTools.Property(truck.GetType(), "AllPawnsAboard").GetValue(truck, null) as System.Collections.IEnumerable;
                if (aboard != null)
                {
                    foreach (object o in aboard)
                    {
                        Pawn p = o as Pawn;
                        if (p != null && p.Name != null && p.Name.ToStringFull.Contains("VFPassenger")) return p;
                    }
                }
            }
            foreach (Caravan c in Find.WorldObjects.Caravans)
            {
                foreach (Pawn p in c.PawnsListForReading)
                {
                    if (p.Name != null && p.Name.ToStringFull.Contains("VFPassenger")) return p;
                }
            }
            foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
            {
                if (p.Name != null && p.Name.ToStringFull.Contains("VFPassenger")) return p;
            }
            return null;
        }

        private float GetFuel(Thing truck)
        {
            if (truck == null) return -1f;
            ThingWithComps twc = truck as ThingWithComps;
            if (twc == null) return -1f;
            ThingComp fuel = twc.AllComps.FirstOrDefault(c => c.GetType().Name == "CompFueledTravel");
            if (fuel == null) return -1f;
            PropertyInfo prop = AccessTools.Property(fuel.GetType(), "Fuel");
            if (prop != null) return (float)prop.GetValue(fuel, null);
            return -1f;
        }

        private PlanetTile FindPassableNeighborTile(PlanetTile origin)
        {
            List<PlanetTile> neighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(origin, neighbors);
            PlanetTile best = PlanetTile.Invalid;
            float bestCost = float.MaxValue;
            foreach (PlanetTile tile in neighbors)
            {
                if (Find.World.Impassable(tile)) continue;
                Tile t = Find.WorldGrid[tile];
                float cost = (t != null && t.PrimaryBiome != null) ? t.PrimaryBiome.movementDifficulty : 1f;
                if (t != null)
                {
                    if (t.hilliness == Hilliness.Impassable) continue;
                    if (t.hilliness == Hilliness.Mountainous) cost *= 2f;
                    else if (t.hilliness == Hilliness.LargeHills) cost *= 1.5f;
                }
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = tile;
                }
            }
            return best.Valid ? best : neighbors.FirstOrDefault(t => !Find.World.Impassable(t));
        }

        public override void MapComponentTick()
        {
            if (s_done || map != Find.AnyPlayerHomeMap) return;
            if (!Fresh && !Reload) return;
            m_ticks++;
            if (m_ticks > 300 && Find.TickManager.CurTimeSpeed != TimeSpeed.Ultrafast) Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
            if (Reload && HomeSave) { UpdateHomeReload(); return; }
            if (Reload) { if (m_ticks >= 300) FinishReload(); return; }
            if (m_ticks > 320 && m_homeStage < 9) UpdateHome();
            if (m_ticks == 60) SpawnTruck();
            if (m_ticks == 90) StartDrive();
            if (m_ticks == 120) SpawnReturnTruck();
            // Driving needs the asynchronous path and real movement: leave the map once the truck has driven 5 cells (800 ticks at most).
            bool drove = m_vehicle != null && m_vehicle.Spawned && m_startPos.IsValid && m_vehicle.Position.DistanceTo(m_startPos) >= 5f;
            if (!m_caravanStarted && m_ticks >= 200 && (drove || m_ticks >= 800))
            {
                m_caravanStarted = true;
                m_driveNote += " droveAtTick=" + m_ticks;
                StartWorldCaravan();
            }
            if (m_caravanStarted) UpdateCaravan();
            bool truckDone = m_caravanArrived && m_ticks >= m_caravanArrivedTick + 30;
            if ((truckDone && m_homeStage == 9) || m_ticks >= 60000) FinishFresh();
        }

        /// Refuel, put a colonist in the driver's seat, start the engine and drive about 15 cells.
        private void StartDrive()
        {
            try
            {
                if (m_vehicle == null) { m_driveNote = "no vehicle"; return; }
                ThingWithComps v = (ThingWithComps)m_vehicle;
                ThingComp fuel = v.AllComps.FirstOrDefault(c => c.GetType().Name == "CompFueledTravel");
                if (fuel != null) AccessTools.Method(fuel.GetType(), "Refuel", new Type[] { typeof(float) }).Invoke(fuel, new object[] { 80f });
                Pawn driver = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                driver.Name = new NameTriple("VFDriver", "Driver", "TMM");
                GenSpawn.Spawn(driver, m_vehicle.Position + new IntVec3(3, 0, 0), map);
                bool boarded = (bool)AccessTools.Method(m_vehicle.GetType(), "TryAddPawn", new Type[] { typeof(Pawn) }).Invoke(m_vehicle, new object[] { driver });

                Pawn relative = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, fixedGender: Gender.Male));
                relative.Name = new NameTriple("VFRelative", "Relative", "TMM");
                GenSpawn.Spawn(relative, m_vehicle.Position + new IntVec3(2, 0, 0), map);

                Pawn passenger = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, fixedGender: Gender.Female));
                passenger.Name = new NameTriple("VFPassenger", "Passenger", "TMM");
                GenSpawn.Spawn(passenger, m_vehicle.Position + new IntVec3(4, 0, 0), map);
                passenger.relations.AddDirectRelation(PawnRelationDefOf.Spouse, relative);

                bool passBoarded = (bool)AccessTools.Method(m_vehicle.GetType(), "TryAddPawn", new Type[] { typeof(Pawn) }).Invoke(m_vehicle, new object[] { passenger });

                object ignition = AccessTools.Field(m_vehicle.GetType(), "ignition").GetValue(m_vehicle);
                AccessTools.Property(ignition.GetType(), "Drafted").SetValue(ignition, true, null);
                m_startPos = m_vehicle.Position;
                IntVec3 dest;
                // The game map is random: take only a place this vehicle can really drive to, as a player would.
                MethodInfo reach = AccessTools.Method(AccessTools.TypeByName("Vehicles.VehicleReachabilityUtility"), "CanReachVehicle", new Type[] { AccessTools.TypeByName("Vehicles.VehiclePawn"), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(Danger), typeof(TraverseMode) });
                dest = IntVec3.Invalid;
                for (int tries = 0; tries < 400 && !dest.IsValid; tries++)
                {
                    IntVec3 c = m_startPos + new IntVec3(Rand.RangeInclusive(-22, 22), 0, Rand.RangeInclusive(-22, 22));
                    if (c.InBounds(map) && c.DistanceTo(m_startPos) >= 10f && c.Standable(map) && c.GetEdifice(map) == null && (bool)reach.Invoke(null, new object[] { m_vehicle, new LocalTargetInfo(c), PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn })) dest = c;
                }
                if (!dest.IsValid) { m_driveNote = "no reachable drive target"; return; }
                m_dest = dest;
                object pather = AccessTools.Field(m_vehicle.GetType(), "vehiclePather").GetValue(m_vehicle);
                AccessTools.Method(pather.GetType(), "StartPath").Invoke(pather, new object[] { new LocalTargetInfo(dest), PathEndMode.OnCell, false });
                bool canMove = (bool)AccessTools.Property(m_vehicle.GetType(), "CanMoveFinal").GetValue(m_vehicle, null);
                m_driveNote = "boarded=" + boarded + " passBoarded=" + passBoarded + " canMove=" + canMove + " dest=" + dest;
            }
            catch (Exception ex) { m_driveNote = "exception: " + ex.GetBaseException().Message; }
        }

        private void StartWorldCaravan()
        {
            try
            {
                if (m_vehicle == null) m_vehicle = FindTruck();
                if (m_vehicle == null) { m_caravanNote = "no vehicle"; return; }
                m_mapDriveDistance = (m_vehicle != null && m_startPos.IsValid) ? (int)m_vehicle.Position.DistanceTo(m_startPos) : -1;
                m_fuelBeforeCaravan = GetFuel(m_vehicle);
                m_startTile = map.Tile;
                m_destTile = FindPassableNeighborTile(map.Tile);
                if (!m_destTile.Valid) { m_caravanNote = "no passable neighbor tile"; return; }

                Type caravanHelper = AccessTools.TypeByName("Vehicles.CaravanHelper");
                MethodInfo exitMethod = caravanHelper != null ? AccessTools.Method(caravanHelper, "ExitMapAndCreateVehicleCaravan", new Type[] {
                    typeof(IEnumerable<Pawn>),
                    typeof(Faction),
                    typeof(PlanetTile),
                    typeof(PlanetTile),
                    typeof(PlanetTile),
                    typeof(bool)
                }) : null;

                if (exitMethod == null) { m_caravanNote = "ExitMapAndCreateVehicleCaravan method not found"; return; }

                object caravanObj = exitMethod.Invoke(null, new object[] {
                    new Pawn[] { (Pawn)m_vehicle },
                    Faction.OfPlayer,
                    map.Tile,
                    m_destTile,
                    m_destTile,
                    false
                });

                m_caravan = caravanObj as Caravan;
                if (m_caravan != null)
                {
                    m_caravanFormed = true;
                    m_caravanNote = "formed caravan id=" + m_caravan.ID + " dest=" + m_destTile;

                    object pather = AccessTools.Field(m_caravan.GetType(), "vehiclePather").GetValue(m_caravan);
                    if (pather != null)
                    {
                        bool moving = (bool)AccessTools.Property(pather.GetType(), "Moving").GetValue(pather, null);
                        if (!moving)
                        {
                            AccessTools.Method(pather.GetType(), "StartPath").Invoke(pather, new object[] { m_destTile, null, true, true });
                        }
                    }
                }
                else
                {
                    m_caravanNote = "ExitMapAndCreateVehicleCaravan returned null";
                }
            }
            catch (Exception ex)
            {
                m_caravanNote = "StartWorldCaravan ex: " + ex.GetBaseException().Message + " at " + ex.StackTrace;
            }
        }

        private void UpdateCaravan()
        {
            try
            {
                if (m_caravan == null) m_caravan = FindVehicleCaravan();
                if (m_caravan == null) return;

                Thing truck = m_vehicle ?? FindTruck();
                if (truck != null && !truck.Spawned)
                {
                    m_caravanDeparted = true;
                }

                object pather = AccessTools.Field(m_caravan.GetType(), "vehiclePather").GetValue(m_caravan);
                bool moving = pather != null && (bool)AccessTools.Property(pather.GetType(), "Moving").GetValue(pather, null);

                if (!m_caravanArrived)
                {
                    bool arrivedAtTile = m_destTile.Valid && m_caravan.Tile == m_destTile;
                    if (arrivedAtTile)
                    {
                        m_caravanArrived = true;
                        m_caravanArrivedTick = m_ticks;
                        m_fuelAfterCaravan = GetFuel(truck);
                        m_caravanNote = "arrived at tile=" + m_caravan.Tile + " (moving=" + moving + ") fuelBefore=" + m_fuelBeforeCaravan + " fuelAfter=" + m_fuelAfterCaravan;
                    }
                }
            }
            catch (Exception ex)
            {
                m_caravanNote = "UpdateCaravan ex: " + ex.GetBaseException().Message;
            }
        }

        private void SpawnTruck()
        {
            try
            {
                Def def = TruckVehicleDef();
                Type spawner = AccessTools.TypeByName("Vehicles.VehicleSpawner");
                MethodInfo spawn = spawner == null ? null : AccessTools.Method(spawner, "SpawnVehicleRandomized");
                if (def == null || spawn == null) return;
                IntVec3 cell;
                if (!CellFinder.TryFindRandomCellNear(map.Center, map, 20, c => GenAdj.OccupiedRect(c, Rot4.North, new IntVec2(2, 4)).All(x => x.InBounds(map) && x.Standable(map) && x.GetFirstPawn(map) == null && x.GetEdifice(map) == null), out cell)) cell = map.Center;
                m_vehicle = (Thing)spawn.Invoke(null, new object[] { def, cell, map, Faction.OfPlayer, (Rot4?)Rot4.North, false });
                m_spawnTick = Find.TickManager.TicksGame;
            }
            catch (Exception ex) { Check("SpawnTruckNoException", false, ex.GetBaseException().Message); }
        }

        private void SpawnReturnTruck()
        {
            Type spawner = AccessTools.TypeByName("Vehicles.VehicleSpawner");
            MethodInfo spawn = spawner == null ? null : AccessTools.Method(spawner, "SpawnVehicleRandomized");
            Type vehicleDefType = AccessTools.TypeByName("Vehicles.VehicleDef");
            if (spawn == null || vehicleDefType == null) return;

            try
            {
                Def impalaDef = TruckVehicleDef();
                if (impalaDef != null)
                {
                    IntVec3 cell;
                    if (!CellFinder.TryFindRandomCellNear(map.Center + new IntVec3(0, 0, 15), map, 15,
                        c => GenAdj.OccupiedRect(c, Rot4.North, new IntVec2(2, 4)).All(x => x.InBounds(map) && x.Standable(map) && x.GetFirstPawn(map) == null && x.GetEdifice(map) == null), out cell))
                        cell = map.Center + new IntVec3(0, 0, 15);

                    Thing impala = (Thing)spawn.Invoke(null, new object[] { impalaDef, cell, map, Faction.OfPlayer, (Rot4?)Rot4.North, false });
                    m_impala = impala as Pawn;
                    Check("Spawned_HomeTestPickup", impala != null && impala.Spawned, impala != null ? impala.Position.ToString() : "null");

                    if (impala != null)
                    {
                        ThingWithComps impalaComps = impala as ThingWithComps;
                        ThingComp fuel = impalaComps != null ? impalaComps.AllComps.FirstOrDefault(c => c.GetType().Name == "CompFueledTravel") : null;
                        if (fuel != null) AccessTools.Method(fuel.GetType(), "Refuel", new Type[] { typeof(float) }).Invoke(fuel, new object[] { 70f });

                        Pawn impalaDriver = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                        impalaDriver.Name = new NameTriple("VFImpala", "Driver", "TMM");
                        GenSpawn.Spawn(impalaDriver, impala.Position + new IntVec3(2, 0, 0), map);
                        bool boarded = (bool)AccessTools.Method(impala.GetType(), "TryAddPawn", new Type[] { typeof(Pawn) }).Invoke(impala, new object[] { impalaDriver });

                        object ignition = AccessTools.Field(impala.GetType(), "ignition").GetValue(impala);
                        AccessTools.Property(ignition.GetType(), "Drafted").SetValue(ignition, true, null);

                        IntVec3 startPos = impala.Position;
                        IntVec3 dest;
                        if (!CellFinder.TryFindRandomCellNear(startPos + new IntVec3(12, 0, 0), map, 6, c => c.Standable(map) && c.GetEdifice(map) == null, out dest)) dest = startPos + new IntVec3(12, 0, 0);

                        object pather = AccessTools.Field(impala.GetType(), "vehiclePather").GetValue(impala);
                        AccessTools.Method(pather.GetType(), "StartPath").Invoke(pather, new object[] { new LocalTargetInfo(dest), PathEndMode.OnCell, false });

                        bool canMove = (bool)AccessTools.Property(impala.GetType(), "CanMoveFinal").GetValue(impala, null);
                        Check("HomePickup_CanMoveAndBoarded", boarded && canMove, "boarded=" + boarded + " canMove=" + canMove);
                    }
                }
            }
            catch (Exception ex)
            {
                Check("HomePickup_TestNoException", false, ex.GetBaseException().Message);
            }

        }

        private void CommonChecks()
        {
            Type vehicleDef = AccessTools.TypeByName("Vehicles.VehicleDef");
            Check("VehiclesAssemblyLoaded", vehicleDef != null && AccessTools.TypeByName("Vehicles.VehicleMod") != null, vehicleDef != null ? vehicleDef.Assembly.GetName().Version.ToString() : "null");
            Check("SmashToolsLoaded", AccessTools.TypeByName("SmashTools.SmashLog") != null, "checked");
            int vfDefs = 0;
            foreach (Type t in GenTypes.AllSubclasses(typeof(Def)))
            {
                if (t.Namespace == null || !t.Namespace.StartsWith("Vehicles")) continue;
                object list = typeof(DefDatabase<>).MakeGenericType(t).GetProperty("AllDefsListForReading").GetValue(null, null);
                vfDefs += ((System.Collections.IList)list).Count;
            }
            Check("VehicleFrameworkDefsLoaded", vfDefs > 0, vfDefs.ToString());
            int patched = 0;
            foreach (MethodBase m in Harmony.GetAllPatchedMethods())
            {
                Patches p = Harmony.GetPatchInfo(m);
                // Integrated, VF registers its patches under our package id, so count patches whose code lives in its namespaces.
                if (p != null && p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Concat(p.Finalizers).Any(x => x.PatchMethod != null && x.PatchMethod.DeclaringType != null && x.PatchMethod.DeclaringType.Namespace != null && (x.PatchMethod.DeclaringType.Namespace.StartsWith("Vehicles") || x.PatchMethod.DeclaringType.Namespace.StartsWith("SmashTools")))) patched++;
            }
            Check("VehicleFrameworkPatchesApplied", patched > 20, patched.ToString());
            Def truck = TruckVehicleDef();
            Check("HunterTruckDefLoaded", truck != null, truck != null ? truck.LabelCap.ToString() : "null");
        }

        private void FinishFresh()
        {
            s_done = true;
            CommonChecks();
            Thing truck = m_vehicle ?? FindTruck();
            Check("HunterTruckSpawned", m_spawnTick >= 0 && truck != null && truck.Faction == Faction.OfPlayer, truck == null ? "null" : ("spawned=" + truck.Spawned + " pos=" + truck.Position));
            Check("HunterTruckSurvivesTicks", truck != null && !truck.Destroyed && m_spawnTick >= 0 && Find.TickManager.TicksGame - m_spawnTick >= 300, (Find.TickManager.TicksGame - m_spawnTick).ToString());
            Check("HunterTruckDrives", m_mapDriveDistance >= 5 || (truck != null && m_startPos.IsValid && truck.Position.DistanceTo(m_startPos) >= 5), m_driveNote + " moved=" + m_mapDriveDistance);
            ThingWithComps twc = truck as ThingWithComps;
            Check("HunterTruckHasFuelComp", twc != null && twc.AllComps.Any(c => c.GetType().Name == "CompFueledTravel"), twc == null ? "null" : string.Join(",", twc.AllComps.Select(c => c.GetType().Name).ToArray()));

            Check("VehicleCaravanFormed", m_caravanFormed && m_caravan != null, m_caravanNote);
            Check("VehicleCaravanDepartedMap", m_caravanDeparted || (truck != null && !truck.Spawned), "departed=" + m_caravanDeparted + " truckSpawned=" + (truck != null && truck.Spawned));
            Check("VehicleCaravanArrivedAtDestination", m_caravanArrived && m_caravan != null && m_destTile.Valid && m_caravan.Tile == m_destTile, "arrived=" + m_caravanArrived + " tile=" + (m_caravan != null ? m_caravan.Tile.ToString() : "null") + " dest=" + m_destTile);
            if (m_fuelAfterCaravan < 0f) m_fuelAfterCaravan = GetFuel(truck);
            bool fuelConsumed = m_fuelAfterCaravan > 0f && m_fuelBeforeCaravan > 0f && m_fuelAfterCaravan < m_fuelBeforeCaravan;
            Check("VehicleCaravanConsumedFuel", fuelConsumed, "before=" + m_fuelBeforeCaravan + " after=" + m_fuelAfterCaravan + " delta=" + (m_fuelBeforeCaravan - m_fuelAfterCaravan));


            Pawn relPawn = map.mapPawns.AllPawns.FirstOrDefault(p => p.Name != null && p.Name.ToStringFull.Contains("VFRelative"));
            Pawn passPawn = FindPassenger(truck);
            bool relationExists = relPawn != null && passPawn != null && relPawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, passPawn);
            Check("PassengerRelativeRelationBeforeSave", relationExists, "rel=" + (relPawn != null) + " pass=" + (passPawn != null) + " spouse=" + relationExists);
            try { GameDataSaveLoader.SaveGame(SaveName); Check("SaveWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, SaveName + ".rws")), "checked"); }
            catch (Exception ex) { Check("SaveWritten", false, ex.GetBaseException().Message); }
            Check("Home_Note", m_homeStage == 9, m_homeNote);
            TestElsewhere();
            Write("VehicleFrameworkProbe.json", true);
        }

        private void FinishReload()
        {
            s_done = true;
            CommonChecks();
            Caravan caravan = FindVehicleCaravan();
            Check("VehicleCaravanRestored", caravan != null && caravan.Spawned && caravan.Faction == Faction.OfPlayer, caravan == null ? "null" : ("tile=" + caravan.Tile + " spawned=" + caravan.Spawned));

            Thing truck = FindTruck();
            Check("HunterTruckRestored", truck != null && !truck.Destroyed && truck.Faction == Faction.OfPlayer, truck == null ? "null" : ("truck=" + truck.def.defName + " faction=" + truck.Faction));

            Pawn relPawn = map.mapPawns.AllPawns.FirstOrDefault(p => p.Name != null && p.Name.ToStringFull.Contains("VFRelative"));
            Check("ColonyRelativePresentAfterReload", relPawn != null && relPawn.Spawned, relPawn == null ? "null" : relPawn.LabelShort);

            Pawn passPawn = FindPassenger(truck);
            Check("PassengerInVehicleAfterReload", passPawn != null, passPawn == null ? "null" : passPawn.LabelShort);

            bool relHasPass = relPawn != null && passPawn != null && relPawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, passPawn);
            bool passHasRel = passPawn != null && relPawn != null && passPawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, relPawn);
            Check("VehiclePassengerRelationPreservedAfterReload", relHasPass && passHasRel,
                "relHasPass=" + relHasPass + " passHasRel=" + passHasRel);

            float reloadFuel = GetFuel(truck);
            Check("HunterTruckFuelPreservedAfterReload", reloadFuel > 10f && reloadFuel < 80f, "fuel=" + reloadFuel);

            Write("VehicleFrameworkReloadProbe.json", false);
        }

        private void Write(string file, bool ui)
        {
            string json = "{\"passed\":" + (m_allPassed ? "true" : "false") + ",\"checksCount\":" + m_checks.Count + ",\"checks\":[" + string.Join(",", m_checks.ToArray()) + "]}";
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, file), json);
            if (ui) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VehicleFrameworkUiProbe.json"), "{\"passed\":" + (m_allPassed ? "true" : "false") + "}");
        }
    }
}
