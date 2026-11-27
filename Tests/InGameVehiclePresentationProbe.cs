using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Vehicle presentation: turret automatic targeting (on for new vehicles, needs an operator, never aims at friends, the player's
    /// "off" survives respawn and save), the vertibird's climb and descent, and the Russian labels of Vehicle Framework.
    /// </summary>
    public sealed class InGameVehiclePresentationProbe : MapComponent
    {
        private const string SaveName = "TMM-VehiclePresentation";
        private static bool started, loadRequested;
        private static IEnumerator routine;
        private readonly List<string> checks = new List<string>();
        private readonly List<string> failed = new List<string>();
        private Pawn offVehicle, onVehicle;

        public InGameVehiclePresentationProbe(Map map) : base(map) { }

        private static bool Has(string flag) { return Environment.GetCommandLineArgs().Contains(flag); }
        private static bool Fresh { get { return Has("-tmm-vehicle-presentation-probe"); } }
        private static bool Reload { get { return Has("-tmm-vehicle-presentation-reload"); } }

        [StaticConstructorOnStartup]
        private static class Loader
        {
            static Loader()
            {
                if (!Reload) return;
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (loadRequested || Current.ProgramState != ProgramState.Entry) return;
                    loadRequested = true;
                    GameDataSaveLoader.LoadGame(SaveName);
                });
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref offVehicle, "vpOff");
            Scribe_References.Look(ref onVehicle, "vpOn");
        }

        private void Check(string name, bool ok, string actual)
        {
            if (!ok) failed.Add(name);
            checks.Add("{\"name\":\"" + name + "\",\"passed\":" + (ok ? "true" : "false") + ",\"expected\":\"True\",\"actual\":\"" + (actual ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ") + "\"}");
        }

        private void Write(string file)
        {
            string json = "{\"passed\":" + (failed.Count == 0 ? "true" : "false") + ",\"checks\":[" + string.Join(",", checks.ToArray()) + "],\"errors\":[]}";
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, file), json);
            if (Fresh) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "VehiclePresentationUiProbe.json"), "{\"passed\":" + (failed.Count == 0 ? "true" : "false") + "}");
        }

        public override void MapComponentUpdate()
        {
            if (map != Find.AnyPlayerHomeMap || !Fresh && !Reload) return;
            if (!started)
            {
                if (Find.TickManager.TicksGame < 60 && Fresh) return;
                started = true;
                routine = Fresh ? Run() : RunReload();
            }
            if (routine == null) return;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
            try { if (routine.MoveNext()) return; }
            catch (Exception ex) { Check("NoException", false, ex.GetBaseException().ToString()); }
            routine = null;
            Write(Fresh ? "VehiclePresentationProbe.json" : "VehiclePresentationReloadProbe.json");
        }

        private static IEnumerable<object> Turrets(Pawn vehicle)
        {
            ThingComp comp = vehicle.AllComps.FirstOrDefault(c => c.GetType().Name == "CompVehicleTurrets");
            if (comp == null) yield break;
            foreach (object t in (IEnumerable)AccessTools.Property(comp.GetType(), "Turrets").GetValue(comp, null)) yield return t;
        }
        private static T Prop<T>(object o, string name) { return (T)AccessTools.Property(o.GetType(), name).GetValue(o, null); }
        private static T Field<T>(object o, string name) { return (T)AccessTools.Field(o.GetType(), name).GetValue(o); }
        private static bool Auto(object turret) { return Prop<bool>(turret, "AutoTarget"); }
        private static int Shells(object turret) { return Field<int>(turret, "shellCount"); }
        private static string State(object t)
        {
            return "auto=" + Auto(t) + " canAuto=" + Prop<bool>(t, "CanAutoTarget") + " manned=" + Prop<bool>(t, "IsManned") + " shells=" + Shells(t) + " ammo=" + Prop<bool>(t, "HasAmmo") + " disabled=" + Prop<bool>(t, "TurretDisabled") + " target=" + Field<LocalTargetInfo>(t, "targetInfo");
        }
        private static string All(Pawn v) { return string.Join(" | ", Turrets(v).Select(State).ToArray()); }

        private IntVec3 Open(IntVec3 near, int radius, int half)
        {
            IntVec3 cell;
            Predicate<IntVec3> open = c => CellRect.CenteredOn(c, half).Cells.All(x => x.InBounds(map) && x.Standable(map) && !x.Roofed(map) && !x.Fogged(map) && x.GetEdifice(map) == null && x.GetFirstPawn(map) == null);
            if (CellFinder.TryFindRandomCellNear(near, map, radius, open, out cell)) return cell;
            // Random tries can miss the open ground of a rocky map (f-vp-1, 060454): the nearest open place is taken instead.
            foreach (IntVec3 c in map.AllCells.OrderBy(x => x.DistanceToSquared(near)))
                if (open(c)) return c;
            throw new Exception("no open " + half + " rect near " + near);
        }
        private Pawn Vehicle(string def, IntVec3 near, int half)
        {
            Pawn v = VfBridge.SpawnVehicle(VfBridge.ResolveVehicleDef(def), Open(near, 60, half), map, Faction.OfPlayer, Rot4.East);
            if (v == null) throw new Exception("not spawned " + def);
            VfBridge.RefuelFull(v, 999f);
            return v;
        }
        private Pawn Person(Faction f, IntVec3 cell, PawnKindDef kind)
        {
            Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, f, forceGenerateNewPawn: true, canGeneratePawnRelations: false, developmentalStages: DevelopmentalStage.Adult));
            GenSpawn.Spawn(p, cell, map);
            return p;
        }
        private static IEnumerable Wait(int ticks)
        {
            int until = Find.TickManager.TicksGame + ticks;
            while (Find.TickManager.TicksGame < until) yield return null;
        }
        private static void Shot(string name)
        {
            Type capture = AccessTools.TypeByName("UnityEngine.ScreenCapture");
            MethodInfo snapshot = capture == null ? null : AccessTools.Method(capture, "CaptureScreenshot", new[] { typeof(string) });
            if (snapshot != null) snapshot.Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, name) });
        }

        private IEnumerator Run()
        {
            // Russian labels of the framework itself.
            foreach (string key in new[] { "VF_LaunchCommand", "VF_AutoTargeting", "VF_Takeoff", "VF_ReloadVehicleTurret" })
                if (Translator.CanTranslate(key)) Check("Ru_" + key, key.Translate().ToString().Any(c => c >= 'А' && c <= 'я'), key.Translate());
            DesignationCategoryDef category = DefDatabase<DesignationCategoryDef>.AllDefsListForReading.FirstOrDefault(d => d.modContentPack != null && d.defName.IndexOf("ehicle", StringComparison.Ordinal) >= 0);
            Check("Ru_ArchitectCategory", category == null || category.label.Any(c => c >= 'А' && c <= 'я'), category == null ? "no category" : category.defName + "=" + category.label);

            foreach (Pawn p in map.mapPawns.AllPawnsSpawned.Where(p => p.HostileTo(Faction.OfPlayer)).ToList()) p.Destroy(DestroyMode.Vanish);

            // New vehicles start with automatic targeting on.
            Pawn truck = Vehicle("TMM_Vehicle_HunterTruck", map.Center, 3);
            Pawn technical = Vehicle("TMM_Vehicle_GunnerTechnical", map.Center, 4);
            Pawn flyer = Vehicle("TMM_Vehicle_Vertibird", map.Center, 5);
            foreach (Pawn v in new[] { truck, technical, flyer })
                Check("NewAutoOn_" + v.def.defName, Turrets(v).Any() && Turrets(v).All(Auto), All(v));

            // The player's "off" survives leaving and entering the map.
            object gun = Turrets(truck).First();
            AccessTools.Method(gun.GetType(), "SwitchAutoTarget").Invoke(gun, null);
            Check("SwitchedOff", !Auto(gun), State(gun));
            IntVec3 at = truck.Position;
            truck.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(truck, at, map, Rot4.East);
            Check("OffSurvivesRespawn", Turrets(truck).All(t => !Auto(t)), All(truck));

            // No operator: no fire.
            object heavy = Turrets(technical).First();
            AccessTools.Method(heavy.GetType(), "SetMagazineCount").Invoke(heavy, new object[] { 60 });
            Faction enemy = Find.FactionManager.AllFactionsListForReading.First(f => f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction && !f.defeated);
            Faction friend = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction && !f.defeated && !f.Hidden);
            IntVec3 spot = GenRadial.RadialCellsAround(technical.Position, 16f, false).Where(c => c.InBounds(map) && c.Standable(map) && c.DistanceTo(technical.Position) >= 11f && GenSight.LineOfSight(technical.Position, c, map, true)).RandomElement();
            Pawn foe = Person(enemy, spot, PawnKindDefOf.Drifter);
            foe.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            Pawn ally = Person(friend, GenRadial.RadialCellsAround(technical.Position, 9f, false).Where(c => c.InBounds(map) && c.Standable(map) && c.DistanceTo(technical.Position) >= 6f).RandomElement(), PawnKindDefOf.Drifter);
            ally.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            Pawn own = Person(Faction.OfPlayer, GenRadial.RadialCellsAround(technical.Position, 9f, false).Where(c => c.InBounds(map) && c.Standable(map) && c.DistanceTo(technical.Position) >= 6f).RandomElement(), PawnKindDefOf.Colonist);
            int before = Shells(heavy);
            foreach (object o in HoldStill(new[] { foe, ally, own }, 240)) yield return o;
            Check("NoOperatorNoFire", Shells(heavy) == before && foe.health.summaryHealth.SummaryHealthPercent > 0.99f, State(heavy) + " foe=" + foe.health.summaryHealth.SummaryHealthPercent);

            // Driver and gunner aboard: it finds the enemy by itself and never takes a friend as its target.
            int aboard = 0;
            for (int i = 0; i < 3; i++)
            {
                Pawn crew = Person(Faction.OfPlayer, CellFinder.RandomClosewalkCellNear(technical.Position, map, 5), PawnKindDefOf.Colonist);
                if (crew.WorkTagIsDisabled(WorkTags.Violent)) { crew.Destroy(DestroyMode.Vanish); i--; continue; }
                if (VfBridge.TryAddPawn(technical, crew)) aboard++;
            }
            bool wrongTarget = false, rightTarget = false;
            int start = Find.TickManager.TicksGame;
            while (Find.TickManager.TicksGame - start < 2500 && !foe.Dead && !foe.Downed && Shells(heavy) > before - 15)
            {
                Still(foe); Still(ally); Still(own);
                LocalTargetInfo t = Field<LocalTargetInfo>(heavy, "targetInfo");
                if (t.Thing == foe) rightTarget = true;
                else if (t.Thing != null && !t.Thing.HostileTo(Faction.OfPlayer)) wrongTarget = true;
                yield return null;
            }
            Check("OperatorAboard", aboard == 3, "aboard=" + aboard);
            Check("FiresAtEnemyByItself", rightTarget && Shells(heavy) < before, State(heavy) + " foe dead=" + foe.Dead + " downed=" + foe.Downed + " ticks=" + (Find.TickManager.TicksGame - start));
            Check("NeverTargetsFriends", !wrongTarget, "ally health=" + ally.health.summaryHealth.SummaryHealthPercent + " colonist=" + own.health.summaryHealth.SummaryHealthPercent);
            Shot("VehicleTurretFire.png");

            // Switched off by the player with the crew aboard: it stops.
            if (!foe.Destroyed) foe.Destroy(DestroyMode.Vanish);
            AccessTools.Method(heavy.GetType(), "SwitchAutoTarget").Invoke(heavy, null);
            AccessTools.Method(heavy.GetType(), "SetTarget").Invoke(heavy, new object[] { LocalTargetInfo.Invalid });
            foreach (object o in Wait(150)) yield return o;
            Pawn second = Person(enemy, spot, PawnKindDefOf.Drifter);
            second.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            int quiet = Shells(heavy);
            foreach (object o in HoldStill(new[] { second, ally, own }, 400)) yield return o;
            Check("SwitchedOffWithCrewNoFire", !Auto(heavy) && Shells(heavy) == quiet && Field<LocalTargetInfo>(heavy, "targetInfo").Thing == null, State(heavy) + " before=" + quiet);
            AccessTools.Method(heavy.GetType(), "SwitchAutoTarget").Invoke(heavy, null);
            start = Find.TickManager.TicksGame;
            while (Find.TickManager.TicksGame - start < 2500 && Shells(heavy) >= quiet) { if (!second.Destroyed && !second.Dead) Still(second); Still(ally); Still(own); yield return null; }
            Check("SwitchedOnAgainFires", Shells(heavy) < quiet, State(heavy));
            if (!second.Destroyed) second.Destroy(DestroyMode.Vanish);
            foreach (Pawn p in new[] { ally, own }) if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);

            // Vertibird: landing on the map and taking off, with the crew.
            Pawn pilot = Person(Faction.OfPlayer, CellFinder.RandomClosewalkCellNear(flyer.Position, map, 5), PawnKindDefOf.Colonist);
            Pawn trooper = Person(Faction.OfPlayer, CellFinder.RandomClosewalkCellNear(flyer.Position, map, 5), PawnKindDefOf.Colonist);
            VfBridge.TryAddPawn(flyer, pilot);
            VfBridge.TryAddPawn(flyer, trooper);
            int crewBefore = VehicleHome.CrewCount(flyer);
            ThingComp launcher = flyer.AllComps.First(c => c.GetType().Name == "CompVehicleLauncher");
            object props = AccessTools.Property(launcher.GetType(), "Props").GetValue(launcher, null);
            object protocol = Field<object>(launcher, "launchProtocol");
            object launchProps = Field<object>(protocol, "launchProperties"), landProps = Field<object>(protocol, "landingProperties");
            object climb = Field<object>(launchProps, "zPositionCurve"), descent = Field<object>(landProps, "zPositionCurve");
            Check("CurvesLoaded", climb != null && descent != null, "climb=" + (climb != null) + " descent=" + (descent != null) + " ticks=" + Field<int>(launchProps, "maxTicks") + "/" + Field<int>(landProps, "maxTicks"));
            IntVec3 pad = flyer.Position;
            flyer.DeSpawn(DestroyMode.Vanish);
            Type maker = AccessTools.TypeByName("Vehicles.VehicleSkyfallerMaker");
            MethodInfo make = AccessTools.Method(maker, "MakeSkyfaller", new[] { typeof(ThingDef), AccessTools.TypeByName("Vehicles.VehiclePawn") });
            Thing sky = (Thing)make.Invoke(null, new object[] { Field<ThingDef>(props, "skyfallerIncoming"), flyer });
            GenSpawn.Spawn(sky, pad, map, Rot4.East);
            CameraJumper.TryJump(pad, map);
            Find.CameraDriver.SetRootSize(14f);
            List<float> heights = new List<float>();
            int frames = 0, inAir = 0;
            while (!flyer.Spawned && frames < 4000) { Sample(sky, protocol, pad, heights); inAir = VehicleHome.CrewCount(flyer); if (frames == 12) Shot("VertibirdLanding.png"); frames++; Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; yield return null; }
            // The crew flies down aboard; Vehicle Framework's "deploy on landing" may let it out on the very tick of touchdown.
            Check("LandedWithCrew", flyer.Spawned && inAir == crewBefore && crewBefore == 2 && new[] { pilot, trooper }.All(p => !p.Dead && (p.Spawned && p.Map == map || VfBridge.GetAllPawnsAboard(flyer).Contains(p))), "spawned=" + flyer.Spawned + " crew " + crewBefore + " in air " + inAir + " aboard after " + VehicleHome.CrewCount(flyer) + " frames=" + frames);
            Check("DescentIsGradual", Gradual(heights), Describe(heights));

            // Vehicle Framework's own setting "deploy on landing" lets the crew out a moment after touchdown; they board again for the flight.
            foreach (object o in Wait(300)) yield return o;
            if (!VfBridge.GetAllPawnsAboard(flyer).Contains(pilot)) VfBridge.TryAddPawn(flyer, pilot);
            if (!VfBridge.GetAllPawnsAboard(flyer).Contains(trooper)) VfBridge.TryAddPawn(flyer, trooper);
            VfBridge.RefuelFull(flyer, 999f);
            PlanetTile dest = PlanetTile.Invalid;
            List<PlanetTile> near = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(map.Tile, near);
            foreach (PlanetTile t in near) if (!Find.World.Impassable(t) && Find.WorldObjects.MapParentAt(t) == null) { dest = t; break; }
            Type dataType = AccessTools.TypeByName("SmashTools.Targeting.TargetData`1").MakeGenericType(typeof(GlobalTargetInfo));
            object data = Activator.CreateInstance(dataType);
            FieldInfo targets = AccessTools.Field(dataType, "targets");
            if (targets.GetValue(data) == null) targets.SetValue(data, new List<GlobalTargetInfo>());
            ((List<GlobalTargetInfo>)targets.GetValue(data)).Add(new GlobalTargetInfo(dest));
            object arrival = Activator.CreateInstance(AccessTools.TypeByName("Vehicles.World.ArrivalAction_LandToCaravan"), new object[] { flyer });
            AccessTools.Method(launcher.GetType(), "Launch").Invoke(launcher, new[] { data, arrival });
            heights.Clear();
            frames = 0;
            while (frames < 4000)
            {
                sky = map.listerThings.AllThings.FirstOrDefault(t => t.GetType().Name == "VehicleSkyfaller_Leaving");
                if (sky == null && frames > 5) break;
                if (sky != null) Sample(sky, protocol, pad, heights);
                if (frames == 40) Shot("VertibirdTakeoff.png");
                frames++;
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                yield return null;
            }
            Check("ClimbIsGradual", Gradual(heights), Describe(heights));
            start = Find.TickManager.TicksGame;
            Caravan landed = null;
            while (landed == null && Find.TickManager.TicksGame - start < 8000) { landed = Find.WorldObjects.Caravans.FirstOrDefault(c => c.ContainsPawn(flyer) && c.Tile == dest); yield return null; }
            string where = string.Join("; ", new[] { pilot, trooper }.Select(p => p.LabelShort + " dead=" + p.Dead + " spawned=" + p.Spawned + " caravan=" + (p.GetCaravan() != null ? p.GetCaravan().ID.ToString() : "none") + " holder=" + (p.ParentHolder != null ? p.ParentHolder.GetType().Name : "none") + " world=" + Find.WorldPawns.Contains(p)).ToArray());
            Check("FlewWithCrew", landed != null && new[] { pilot, trooper }.All(p => !p.Dead && !p.Spawned && (landed.ContainsPawn(p) || VfBridge.GetAllPawnsAboard(flyer).Contains(p))), "landed=" + (landed != null) + " id=" + (landed != null ? landed.ID : -1) + " aboard=" + VehicleHome.CrewCount(flyer) + " " + where);

            offVehicle = truck;
            onVehicle = technical;
            GameDataSaveLoader.SaveGame(SaveName);
        }

        private IEnumerator RunReload()
        {
            foreach (object o in Wait(120)) yield return o;
            if (Has("-tmm-vehicle-presentation-old"))
            {
                // A save of public 0.17: its pickup was saved before the model got the bed machine gun.
                List<Pawn> trucks = map.mapPawns.AllPawns.Concat(Find.WorldObjects.Caravans.SelectMany(c => c.PawnsListForReading)).Where(p => p.def.defName == "TMM_Vehicle_HunterTruck").Distinct().ToList();
                foreach (Pawn t in trucks)
                {
                    string roles = string.Join(",", ((IEnumerable)AccessTools.Field(t.GetType(), "handlers").GetValue(t)).Cast<object>().Select(h => (string)AccessTools.Field(Field<object>(h, "role").GetType(), "key").GetValue(Field<object>(h, "role"))).ToArray());
                    Check("OldTruckHasItsGun", Turrets(t).Count() == 1 && Turrets(t).All(Auto), t.ThingID + " spawned=" + t.Spawned + " " + All(t) + " roles=" + roles);
                    Check("OldTruckHasGunnerRole", roles.Contains("gunner"), roles);
                }
                Check("OldTrucksFound", trucks.Count > 0, trucks.Count.ToString());
                yield break;
            }
            Check("OffSurvivesSave", offVehicle != null && offVehicle.Spawned && Turrets(offVehicle).Any() && Turrets(offVehicle).All(t => !Auto(t)), offVehicle == null ? "null" : All(offVehicle));
            Check("OnSurvivesSave", onVehicle != null && onVehicle.Spawned && Turrets(onVehicle).Any() && Turrets(onVehicle).All(Auto) && VehicleHome.CrewCount(onVehicle) == 3, onVehicle == null ? "null" : All(onVehicle) + " crew=" + VehicleHome.CrewCount(onVehicle));
            Caravan air = Find.WorldObjects.Caravans.FirstOrDefault(c => c.PawnsListForReading.Any(p => p.def.defName == "TMM_Vehicle_Vertibird"));
            Pawn flyer = air == null ? null : air.PawnsListForReading.First(p => p.def.defName == "TMM_Vehicle_Vertibird");
            int withIt = flyer == null ? 0 : air.PawnsListForReading.Count(p => p.RaceProps.Humanlike) + VfBridge.GetAllPawnsAboard(flyer).Count(p => !air.ContainsPawn(p));
            Check("FlyerCaravanSurvivesSave", flyer != null && withIt == 2 && Turrets(flyer).All(Auto), flyer == null ? "null" : "aboard=" + VehicleHome.CrewCount(flyer) + " with it=" + withIt + " " + All(flyer));
        }

        private static void Still(Pawn p)
        {
            if (p == null || p.Destroyed || p.Dead || !p.Spawned) return;
            p.stances.stunner.StunFor(120, null, false, false, false);
        }
        private static IEnumerable HoldStill(Pawn[] pawns, int ticks)
        {
            int until = Find.TickManager.TicksGame + ticks;
            while (Find.TickManager.TicksGame < until) { foreach (Pawn p in pawns) Still(p); yield return null; }
        }
        private static void Sample(Thing sky, object protocol, IntVec3 pad, List<float> heights)
        {
            if (sky == null || sky.Destroyed) return;
            Vector3 pos = (Vector3)AccessTools.Field(AccessTools.TypeByName("Vehicles.VehicleSkyfaller"), "launchProtocolDrawPos").GetValue(sky);
            // The first value is the spawn cell: the position is worked out when the skyfaller is first drawn.
            if (pos != Vector3.zero) heights.Add(pos.z - pad.z);
        }
        private static bool Gradual(List<float> all)
        {
            if (all.Count < 11) return false;
            List<float> h = all.Skip(1).ToList();
            float span = h.Max() - h.Min(), jump = 0f;
            for (int i = 1; i < h.Count; i++) jump = Math.Max(jump, Math.Abs(h[i] - h[i - 1]));
            return span >= 2f && jump <= span * 0.5f;
        }
        private static string Describe(List<float> all)
        {
            if (all.Count < 2) return "samples=" + all.Count;
            List<float> h = all.Skip(1).ToList();
            float jump = 0f;
            for (int i = 1; i < h.Count; i++) jump = Math.Max(jump, Math.Abs(h[i] - h[i - 1]));
            return "samples=" + h.Count + " first=" + h[0].ToString("F2") + " last=" + h[h.Count - 1].ToString("F2") + " min=" + h.Min().ToString("F2") + " max=" + h.Max().ToString("F2") + " biggest step=" + jump.ToString("F2");
        }
    }
}
