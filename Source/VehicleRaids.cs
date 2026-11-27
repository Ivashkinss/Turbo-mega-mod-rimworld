using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    // =========================================================================
    // 1. XML Extension on FactionDef (string defNames survive absence of VF)
    // =========================================================================

    public sealed class FactionVehicleEntry
    {
        public string defName;
        public float weight = 1f;
        public bool forRaids = true;
        public bool forVisits = false;
        public float pointsCostOverride = -1f;
    }

    public sealed class FactionVehiclesExtension : DefModExtension
    {
        public List<FactionVehicleEntry> vehicles = new List<FactionVehicleEntry>();
        public float minPoints = 900f;
        public int maxVehicles = 2;
        public float marketValuePointsFactor = 0.085f;
    }

    // =========================================================================
    // 2. Reflection Bridge to Vehicle Framework (zero hard compile dependency)
    // =========================================================================

    public static class VfBridge
    {
        private static bool initialized;
        private static bool available;

        private static Type vehicleDefType;
        private static Type vehiclePawnType;
        private static Type vehicleSpawnerType;
        private static Type compFueledTravelType;
        private static Type compVehicleTurretsType;
        private static Type caravanHelperType;

        private static MethodInfo getNamedVehicleDefMethod;
        private static MethodInfo spawnVehicleRandomizedMethod;
        private static MethodInfo tryAddPawn1Method;
        private static MethodInfo disembarkAllMethod; // CHECK VF
        private static MethodInfo disembarkPawnMethod; // CHECK VF
        private static MethodInfo refuelMethod;
        private static MethodInfo exitMapAndCreateVehicleCaravanMethod;

        private static PropertyInfo allPawnsAboardProp;
        private static FieldInfo allPawnsAboardField;
        private static FieldInfo ignitionField;
        private static PropertyInfo ignitionProp;
        private static PropertyInfo draftedProp;
        private static FieldInfo vehiclePatherField;
        private static PropertyInfo vehiclePatherProp;
        private static MethodInfo startPathMethod;
        private static MethodInfo fitsMethod, canReachMethod;
        private static MethodInfo stopDeadMethod; // CHECK VF
        private static PropertyInfo movingProp; // CHECK VF
        private static PropertyInfo movingNowProp; // CHECK VF
        private static FieldInfo handlersField; // CHECK VF
        private static PropertyInfo handlersProp; // CHECK VF
        private static PropertyInfo turretsListProp; // CHECK VF
        private static FieldInfo turretsListField; // CHECK VF

        public static bool IsAvailable
        {
            get
            {
                EnsureInitialized();
                return available;
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            try
            {
                vehicleDefType = AccessTools.TypeByName("Vehicles.VehicleDef");
                vehiclePawnType = AccessTools.TypeByName("Vehicles.VehiclePawn");
                vehicleSpawnerType = AccessTools.TypeByName("Vehicles.VehicleSpawner");
                compFueledTravelType = AccessTools.TypeByName("Vehicles.CompFueledTravel");
                compVehicleTurretsType = AccessTools.TypeByName("Vehicles.CompVehicleTurrets"); // CHECK VF
                caravanHelperType = AccessTools.TypeByName("Vehicles.CaravanHelper");

                if (vehicleDefType == null || vehiclePawnType == null || vehicleSpawnerType == null)
                {
                    available = false;
                    return;
                }

                Type dbType = typeof(DefDatabase<>).MakeGenericType(new Type[] { vehicleDefType });
                getNamedVehicleDefMethod = AccessTools.Method(dbType, "GetNamedSilentFail", new Type[] { typeof(string) });

                spawnVehicleRandomizedMethod = AccessTools.Method(
                    vehicleSpawnerType,
                    "SpawnVehicleRandomized",
                    new Type[]
                    {
                        vehicleDefType,
                        typeof(IntVec3),
                        typeof(Map),
                        typeof(Faction),
                        typeof(Rot4?),
                        typeof(bool)
                    });

                tryAddPawn1Method = AccessTools.Method(vehiclePawnType, "TryAddPawn", new Type[] { typeof(Pawn) });
                disembarkAllMethod = AccessTools.Method(vehiclePawnType, "DisembarkAll", Type.EmptyTypes); // CHECK VF
                disembarkPawnMethod = AccessTools.Method(vehiclePawnType, "DisembarkPawn", new Type[] { typeof(Pawn) }); // CHECK VF

                allPawnsAboardProp = AccessTools.Property(vehiclePawnType, "AllPawnsAboard");
                allPawnsAboardField = AccessTools.Field(vehiclePawnType, "AllPawnsAboard");

                ignitionField = AccessTools.Field(vehiclePawnType, "ignition");
                ignitionProp = AccessTools.Property(vehiclePawnType, "ignition");
                Type ignitionType = ignitionField != null
                    ? ignitionField.FieldType
                    : (ignitionProp != null ? ignitionProp.PropertyType : null);
                if (ignitionType != null)
                {
                    draftedProp = AccessTools.Property(ignitionType, "Drafted");
                }

                vehiclePatherField = AccessTools.Field(vehiclePawnType, "vehiclePather");
                vehiclePatherProp = AccessTools.Property(vehiclePawnType, "vehiclePather");
                Type patherType = vehiclePatherField != null
                    ? vehiclePatherField.FieldType
                    : (vehiclePatherProp != null ? vehiclePatherProp.PropertyType : null);
                if (patherType != null)
                {
                    startPathMethod = AccessTools.Method(
                        patherType,
                        "StartPath",
                        new Type[] { typeof(LocalTargetInfo), typeof(PathEndMode), typeof(bool) });
                    stopDeadMethod = AccessTools.Method(patherType, "StopDead", Type.EmptyTypes); // CHECK VF
                    movingProp = AccessTools.Property(patherType, "Moving"); // CHECK VF
                    movingNowProp = AccessTools.Property(patherType, "MovingNow"); // CHECK VF
                }

                Type extensions = AccessTools.TypeByName("Vehicles.Ext_Vehicles"), reachability = AccessTools.TypeByName("Vehicles.VehicleReachabilityUtility");
                fitsMethod = extensions == null ? null : AccessTools.GetDeclaredMethods(extensions).FirstOrDefault(m => m.Name == "DrivableRectOnCell" && m.GetParameters().Length == 3 && m.GetParameters()[2].ParameterType.IsEnum);
                canReachMethod = reachability == null ? null : AccessTools.GetDeclaredMethods(reachability).FirstOrDefault(m => m.Name == "CanReachVehicle" && m.GetParameters().Length == 5);
                if (fitsMethod == null || canReachMethod == null) Log.Warning("[TMM] Vehicle Framework's route check was not found; vehicle destinations are not checked.");
                handlersField = AccessTools.Field(vehiclePawnType, "handlers"); // CHECK VF
                handlersProp = AccessTools.Property(vehiclePawnType, "handlers"); // CHECK VF

                if (compFueledTravelType != null)
                {
                    refuelMethod = AccessTools.Method(compFueledTravelType, "Refuel", new Type[] { typeof(float) });
                }

                if (compVehicleTurretsType != null)
                {
                    turretsListProp = AccessTools.Property(compVehicleTurretsType, "Turrets"); // CHECK VF
                    turretsListField = AccessTools.Field(compVehicleTurretsType, "turrets"); // CHECK VF
                }

                if (caravanHelperType != null)
                {
                    exitMapAndCreateVehicleCaravanMethod = AccessTools.Method(
                        caravanHelperType,
                        "ExitMapAndCreateVehicleCaravan",
                        new Type[]
                        {
                            typeof(IEnumerable<Pawn>),
                            typeof(Faction),
                            typeof(PlanetTile),
                            typeof(PlanetTile),
                            typeof(PlanetTile),
                            typeof(bool)
                        });
                }

                available = getNamedVehicleDefMethod != null
                    && spawnVehicleRandomizedMethod != null
                    && tryAddPawn1Method != null;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] VfBridge initialization disabled VehicleRaids: " + ex.Message);
                available = false;
            }
        }

        public static bool IsVehiclePawn(Pawn p)
        {
            if (!IsAvailable || p == null) return false;
            return vehiclePawnType.IsInstanceOfType(p);
        }

        public static Def ResolveVehicleDef(string defName)
        {
            if (!IsAvailable || string.IsNullOrEmpty(defName)) return null;
            try
            {
                return getNamedVehicleDefMethod.Invoke(null, new object[] { defName }) as Def;
            }
            catch
            {
                return null;
            }
        }

        public static float GetVehicleMarketValue(Def vehicleDef)
        {
            ThingDef td = vehicleDef as ThingDef;
            if (td != null)
            {
                float mv = td.GetStatValueAbstract(StatDefOf.MarketValue);
                if (mv > 1f) return mv;
                if (td.BaseMarketValue > 1f) return td.BaseMarketValue;
            }
            return 3500f;
        }

        public static IntVec2 GetVehicleSize(Def vehicleDef)
        {
            ThingDef td = vehicleDef as ThingDef;
            if (td != null && td.size.x > 0 && td.size.z > 0) return td.size;
            return new IntVec2(2, 4);
        }

        public static Pawn SpawnVehicle(Def vehicleDef, IntVec3 cell, Map map, Faction faction, Rot4 rot)
        {
            if (!IsAvailable || vehicleDef == null || map == null) return null;
            try
            {
                object res = spawnVehicleRandomizedMethod.Invoke(
                    null,
                    new object[] { vehicleDef, cell, map, faction, (Rot4?)rot, false });
                return res as Pawn;
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] VfBridge.SpawnVehicle failed for " + vehicleDef.defName + ": " + ex);
                return null;
            }
        }

        public static bool TryAddPawn(Pawn vehicle, Pawn passenger)
        {
            if (!IsVehiclePawn(vehicle) || passenger == null || passenger.Dead) return false;
            try
            {
                object res = tryAddPawn1Method.Invoke(vehicle, new object[] { passenger });
                return res is bool && (bool)res;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] VfBridge.TryAddPawn failed: " + ex.Message);
                return false;
            }
        }

        public static List<Pawn> GetAllPawnsAboard(Pawn vehicle)
        {
            List<Pawn> result = new List<Pawn>();
            if (!IsVehiclePawn(vehicle)) return result;

            try
            {
                object raw = null;
                if (allPawnsAboardProp != null) raw = allPawnsAboardProp.GetValue(vehicle, null);
                else if (allPawnsAboardField != null) raw = allPawnsAboardField.GetValue(vehicle);

                IEnumerable enumerable = raw as IEnumerable;
                if (enumerable != null)
                {
                    foreach (object item in enumerable)
                    {
                        Pawn p = item as Pawn;
                        if (p != null && !result.Contains(p)) result.Add(p);
                    }
                }
            }
            catch
            {
                // Ignore reflection errors on destroyed/half-loaded vehicles.
            }

            return result;
        }

        public static void SetDrafted(Pawn vehicle, bool drafted)
        {
            if (!IsVehiclePawn(vehicle) || draftedProp == null) return;
            try
            {
                object ign = ignitionField != null
                    ? ignitionField.GetValue(vehicle)
                    : (ignitionProp != null ? ignitionProp.GetValue(vehicle, null) : null);
                if (ign != null)
                {
                    draftedProp.SetValue(ign, drafted, null);
                }
            }
            catch
            {
                // Non-fatal if ignition is absent.
            }
        }

        /// Vehicle Framework drives a vehicle by an ordinary Goto job, as a player's order does. A path started past the jobs is
        /// cancelled at once: the vehicle's idle job stops the vehicle when it begins and each time it is reconsidered (180 ticks).
        public static bool StartPath(Pawn vehicle, IntVec3 dest)
        {
            if (!IsVehiclePawn(vehicle) || !dest.IsValid || startPathMethod == null) return false;
            try
            {
                if (vehicle.jobs != null && vehicle.Spawned)
                {
                    Job drive = JobMaker.MakeJob(JobDefOf.Goto, dest);
                    drive.playerForced = true;   // nothing but this mission redirects the vehicle
                    vehicle.jobs.StartJob(drive, JobCondition.InterruptForced);
                    return vehicle.CurJob == drive;
                }
                object pather = GetPather(vehicle);
                if (pather == null) return false;
                startPathMethod.Invoke(pather, new object[] { new LocalTargetInfo(dest), PathEndMode.OnCell, true });
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] VfBridge.StartPath failed: " + ex.Message);
                return false;
            }
        }

        /// Whether Vehicle Framework itself would find a way to the cell and fit there. True when it cannot be asked.
        public static bool CanDrive(Pawn vehicle, IntVec3 dest)
        {
            if (!IsVehiclePawn(vehicle) || !vehicle.Spawned || !dest.IsValid) return false;
            if (fitsMethod == null || canReachMethod == null) return true;
            try
            {
                if (!(bool)fitsMethod.Invoke(null, new object[] { vehicle, dest, Enum.ToObject(fitsMethod.GetParameters()[2].ParameterType, 1) })) return false;
                return (bool)canReachMethod.Invoke(null, new object[] { vehicle, new LocalTargetInfo(dest), PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn });
            }
            catch (Exception ex)
            {
                Log.WarningOnce("[TMM] VfBridge.CanDrive failed: " + ex.GetBaseException().Message, 71904421);
                return true;
            }
        }

        public static void StopDead(Pawn vehicle)
        {
            if (!IsVehiclePawn(vehicle) || stopDeadMethod == null) return;
            try
            {
                if (vehicle.CurJobDef == JobDefOf.Goto) vehicle.jobs.EndCurrentJob(JobCondition.InterruptForced);
                object pather = GetPather(vehicle);
                if (pather != null) stopDeadMethod.Invoke(pather, null);
            }
            catch
            {
                // Ignore if unavailable.
            }
        }

        public static bool IsMoving(Pawn vehicle)
        {
            if (!IsVehiclePawn(vehicle)) return false;
            try
            {
                object pather = GetPather(vehicle);
                if (pather == null) return false;
                if (movingNowProp != null)
                {
                    object val = movingNowProp.GetValue(pather, null);
                    if (val is bool) return (bool)val;
                }
                if (movingProp != null)
                {
                    object val = movingProp.GetValue(pather, null);
                    if (val is bool) return (bool)val;
                }
            }
            catch
            {
                // Fallback below.
            }
            return false;
        }

        /// Vehicle Framework looks for a path on another thread. At high game speed hundreds of ticks can pass before it is ready;
        /// a vehicle that is still waiting for its path is not stuck.
        public static bool PathPending(Pawn vehicle)
        {
            if (!IsVehiclePawn(vehicle)) return false;
            try
            {
                object pather = GetPather(vehicle);
                FieldInfo field = pather == null ? null : AccessTools.Field(pather.GetType(), "curPathTask");
                System.Threading.Tasks.Task task = field == null ? null : field.GetValue(pather) as System.Threading.Tasks.Task;
                return task != null && !task.IsCompleted;
            }
            catch
            {
                return false;
            }
        }

        private static object GetPather(Pawn vehicle)
        {
            if (vehiclePatherField != null) return vehiclePatherField.GetValue(vehicle);
            if (vehiclePatherProp != null) return vehiclePatherProp.GetValue(vehicle, null);
            return null;
        }

        public static void RefuelFull(Pawn vehicle, float amount)
        {
            if (!IsVehiclePawn(vehicle) || compFueledTravelType == null || refuelMethod == null) return;
            try
            {
                List<ThingComp> comps = vehicle.AllComps;
                if (comps == null) return;
                for (int i = 0; i < comps.Count; i++)
                {
                    ThingComp c = comps[i];
                    if (c != null && compFueledTravelType.IsInstanceOfType(c))
                    {
                        refuelMethod.Invoke(c, new object[] { amount });
                        return;
                    }
                }
            }
            catch
            {
                // Vehicle may not use fuel.
            }
        }

        public static bool DisembarkAll(Pawn vehicle)
        {
            if (!IsVehiclePawn(vehicle)) return false;

            if (disembarkAllMethod != null)
            {
                try
                {
                    disembarkAllMethod.Invoke(vehicle, null); // CHECK VF
                    if (GetAllPawnsAboard(vehicle).Count == 0) return true;
                }
                catch
                {
                    // Fall back to per-pawn disembark.
                }
            }

            List<Pawn> aboard = GetAllPawnsAboard(vehicle);
            bool anyOk = false;
            for (int i = 0; i < aboard.Count; i++)
            {
                if (DisembarkPawn(vehicle, aboard[i])) anyOk = true;
            }
            return anyOk || GetAllPawnsAboard(vehicle).Count == 0;
        }

        public static bool DisembarkPawn(Pawn vehicle, Pawn pawn)
        {
            if (!IsVehiclePawn(vehicle) || pawn == null) return false;
            if (pawn.Spawned && pawn.Map == vehicle.Map) return true;

            if (disembarkPawnMethod != null)
            {
                try
                {
                    disembarkPawnMethod.Invoke(vehicle, new object[] { pawn }); // CHECK VF
                    if (pawn.Spawned) return true;
                }
                catch
                {
                    // Fallback below.
                }
            }

            // Fallback: remove from handler ThingOwner and spawn near the vehicle.
            try
            {
                if (pawn.holdingOwner != null)
                {
                    pawn.holdingOwner.Remove(pawn);
                }
                if (vehicle.Spawned && vehicle.Map != null && !pawn.Spawned && !pawn.Destroyed)
                {
                    IntVec3 dropCell;
                    if (!CellFinder.TryFindRandomCellNear(vehicle.Position, vehicle.Map, 4,
                        delegate(IntVec3 c) { return c.InBounds(vehicle.Map) && c.Standable(vehicle.Map); },
                        out dropCell))
                    {
                        dropCell = vehicle.Position;
                    }
                    GenSpawn.Spawn(pawn, dropCell, vehicle.Map);
                    return pawn.Spawned;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] VfBridge.DisembarkPawn fallback failed: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Checks whether a vehicle has a mounted turret and whether VF exposes an AI-accessible
        /// auto-target / target-setting API on its turret objects. // CHECK VF
        /// If VF turrets cannot be driven by non-player AI, callers safely disembark the gunner too.
        /// </summary>
        public static bool CanAiOperateTurret(Pawn vehicle)
        {
            if (!IsVehiclePawn(vehicle) || compVehicleTurretsType == null) return false;
            List<object> turrets = GetTurretObjects(vehicle);
            if (turrets.Count == 0) return false;

            object first = turrets[0];
            if (first == null) return false;
            Type t = first.GetType();
            PropertyInfo autoProp = AccessTools.Property(t, "AutoTarget"); // CHECK VF
            FieldInfo autoField = AccessTools.Field(t, "autoTarget"); // CHECK VF
            MethodInfo setTarget = AccessTools.Method(t, "SetTarget", new Type[] { typeof(LocalTargetInfo) }); // CHECK VF
            return autoProp != null || autoField != null || setTarget != null;
        }

        /// <summary>
        /// Returns the pawn currently occupying a turret role handler (if identifiable via VF handlers). // CHECK VF
        /// </summary>
        public static Pawn FindAboardTurretGunner(Pawn vehicle)
        {
            if (!IsVehiclePawn(vehicle)) return null;
            try
            {
                object rawHandlers = handlersField != null
                    ? handlersField.GetValue(vehicle)
                    : (handlersProp != null ? handlersProp.GetValue(vehicle, null) : null);

                IEnumerable list = rawHandlers as IEnumerable;
                if (list == null) return null;

                foreach (object h in list)
                {
                    if (h == null) continue;
                    Type ht = h.GetType();
                    object roleObj = GetMemberValue(h, ht, "role"); // CHECK VF
                    if (roleObj == null) continue;
                    Type rt = roleObj.GetType();

                    string key = GetMemberValue(roleObj, rt, "key") as string;
                    object handling = GetMemberValue(roleObj, rt, "HandlingTypes"); // CHECK VF
                    bool isTurretRole = (!string.IsNullOrEmpty(key) && key.IndexOf("gunner", StringComparison.OrdinalIgnoreCase) >= 0)
                        || (handling != null && handling.ToString().IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!isTurretRole) continue;

                    ThingOwner owner = GetMemberValue(h, ht, "thingOwner") as ThingOwner; // CHECK VF
                    if (owner != null && owner.Count > 0)
                    {
                        for (int i = 0; i < owner.Count; i++)
                        {
                            Pawn gunner = owner[i] as Pawn;
                            if (gunner != null && !gunner.Dead && !gunner.Downed) return gunner;
                        }
                    }
                }
            }
            catch
            {
                // Ignore if VF handler layout differs.
            }
            return null;
        }

        /// <summary>
        /// Attempts to enable AI turret targeting on a vehicle and aim at the nearest valid hostile target. // CHECK VF
        /// </summary>
        public static bool TryTickAiTurrets(Pawn vehicle)
        {
            if (!IsVehiclePawn(vehicle) || !vehicle.Spawned || vehicle.Map == null) return false;
            List<object> turrets = GetTurretObjects(vehicle);
            if (turrets.Count == 0) return false;

            Pawn nearestTarget = FindNearestHostileTarget(vehicle, 34f);
            bool anyActive = false;

            for (int i = 0; i < turrets.Count; i++)
            {
                object turret = turrets[i];
                if (turret == null) continue;
                Type t = turret.GetType();

                try
                {
                    PropertyInfo autoProp = AccessTools.Property(t, "AutoTarget"); // CHECK VF
                    if (autoProp != null && autoProp.CanWrite)
                    {
                        autoProp.SetValue(turret, true, null);
                        anyActive = true;
                    }
                    else
                    {
                        FieldInfo autoField = AccessTools.Field(t, "autoTarget"); // CHECK VF
                        if (autoField != null)
                        {
                            autoField.SetValue(turret, true);
                            anyActive = true;
                        }
                    }

                    if (nearestTarget != null)
                    {
                        MethodInfo setTarget = AccessTools.Method(t, "SetTarget", new Type[] { typeof(LocalTargetInfo) }); // CHECK VF
                        if (setTarget != null)
                        {
                            setTarget.Invoke(turret, new object[] { new LocalTargetInfo(nearestTarget) });
                            anyActive = true;
                        }
                    }
                }
                catch
                {
                    // Skip incompatible turret method.
                }
            }

            return anyActive;
        }

        private static List<object> GetTurretObjects(Pawn vehicle)
        {
            List<object> list = new List<object>();
            if (vehicle == null || compVehicleTurretsType == null || vehicle.AllComps == null) return list;
            for (int i = 0; i < vehicle.AllComps.Count; i++)
            {
                ThingComp c = vehicle.AllComps[i];
                if (c == null || !compVehicleTurretsType.IsInstanceOfType(c)) continue;
                try
                {
                    object raw = turretsListProp != null
                        ? turretsListProp.GetValue(c, null)
                        : (turretsListField != null ? turretsListField.GetValue(c) : null);
                    IEnumerable en = raw as IEnumerable;
                    if (en != null)
                    {
                        foreach (object item in en)
                        {
                            if (item != null) list.Add(item);
                        }
                    }
                }
                catch
                {
                    // Ignore.
                }
            }
            return list;
        }

        private static Pawn FindNearestHostileTarget(Pawn vehicle, float maxRange)
        {
            if (vehicle == null || vehicle.Map == null || vehicle.Map.mapPawns == null) return null;
            IReadOnlyList<Pawn> all = vehicle.Map.mapPawns.AllPawnsSpawned;
            Pawn best = null;
            float bestDistSq = maxRange * maxRange;

            for (int i = 0; i < all.Count; i++)
            {
                Pawn candidate = all[i];
                if (candidate == null || candidate == vehicle || candidate.Dead || candidate.Downed) continue;
                if (!vehicle.HostileTo(candidate)) continue;
                float dSq = (candidate.Position - vehicle.Position).LengthHorizontalSquared;
                if (dSq < bestDistSq && GenSight.LineOfSight(vehicle.Position, candidate.Position, vehicle.Map, true))
                {
                    bestDistSq = dSq;
                    best = candidate;
                }
            }
            return best;
        }

        /// <summary>
        /// Exits a vehicle cleanly off the map. For player-owned vehicles or explicit world-caravan exits,
        /// can invoke CaravanHelper.ExitMapAndCreateVehicleCaravan; for transient NPC visits, passes humanlike
        /// crew to WorldPawns and despawns/destroys the temporary NPC vehicle so no orphan caravans remain.
        /// </summary>
        private static bool IsVisitCrew(Pawn pawn, Pawn vehicle)
        {
            return pawn != null && !pawn.Destroyed && !pawn.Dead && !pawn.IsPrisoner &&
                !pawn.IsSlave && pawn.Faction == vehicle.Faction;
        }

        internal static bool IsVisitCrewOnFoot(Pawn pawn, Pawn vehicle)
        {
            // A spawned pawn's holdingOwner is the map's own spawnedThings, so it says nothing about being held: Spawned is enough.
            return IsVisitCrew(pawn, vehicle) && !pawn.Downed && pawn.Spawned && pawn.Map == vehicle.Map;
        }

        public static bool ExitNpcVehicleFromMap(Pawn vehicle, List<Pawn> crew, bool createWorldCaravan)
        {
            if (!IsVehiclePawn(vehicle) || !vehicle.Spawned || vehicle.Map == null) return false;

            if (createWorldCaravan && exitMapAndCreateVehicleCaravanMethod != null)
            {
                try
                {
                    PlanetTile fromTile = vehicle.Map.Tile;
                    List<Pawn> group = new List<Pawn>();
                    group.Add(vehicle);
                    List<Pawn> aboard = GetAllPawnsAboard(vehicle);
                    for (int i = 0; i < aboard.Count; i++)
                    {
                        if (aboard[i] != null && !group.Contains(aboard[i])) group.Add(aboard[i]);
                    }
                    exitMapAndCreateVehicleCaravanMethod.Invoke(
                        null,
                        new object[] { group, vehicle.Faction, fromTile, PlanetTile.Invalid, PlanetTile.Invalid, false });
                    if (!vehicle.Spawned) return true;
                }
                catch (Exception ex)
                {
                    Log.Warning("[TMM] VfBridge.ExitMapAndCreateVehicleCaravan fallback: " + ex.Message);
                }
            }

            if (Find.WorldPawns == null) return false;
            List<Pawn> aboardNow = GetAllPawnsAboard(vehicle);
            List<Pawn> departing = new List<Pawn>();
            foreach (Pawn pawn in aboardNow)
            {
                if (IsVisitCrew(pawn, vehicle) && !pawn.Spawned)
                    departing.Add(pawn);
                else if (!DisembarkPawn(vehicle, pawn))
                    return false;
            }

            Map map = vehicle.Map;
            if (crew != null && vehicle.Position.DistanceToEdge(map) <= 6)
            {
                foreach (Pawn pawn in crew)
                {
                    if (IsVisitCrewOnFoot(pawn, vehicle) && pawn.Position.DistanceToEdge(map) <= 6 &&
                        pawn.Position.DistanceTo(vehicle.Position) <= 6.5f)
                        departing.Add(pawn);
                }
            }

            foreach (Pawn pawn in departing)
            {
                if (pawn.Spawned) pawn.DeSpawn(DestroyMode.Vanish);
                else if (pawn.holdingOwner != null) pawn.holdingOwner.Remove(pawn);
                if (!Find.WorldPawns.Contains(pawn))
                {
                    bool wasPinned = Find.WorldPawns.ForcefullyKeptPawns.Contains(pawn);
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                    if (!wasPinned) Find.WorldPawns.ForcefullyKeptPawns.Remove(pawn);
                }
            }

            if (crew != null && vehicle.Faction != null)
            {
                List<Pawn> walking = crew.Where(p => IsVisitCrewOnFoot(p, vehicle) && p.GetLord() == null).ToList();
                if (walking.Count > 0)
                    LordMaker.MakeNewLord(vehicle.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog, false, false), map, walking);
            }

            if (vehicle.inventory != null && vehicle.inventory.innerContainer != null)
            {
                vehicle.inventory.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            }
            // Destroy while still spawned: VF's VehiclePawn.Destroy runs DisembarkAll, which fails on a despawned vehicle.
            if (!vehicle.Destroyed) vehicle.Destroy(DestroyMode.Vanish);
            return true;
        }

        private static object GetMemberValue(object instance, Type t, string name)
        {
            PropertyInfo p = AccessTools.Property(t, name);
            if (p != null) return p.GetValue(instance, null);
            FieldInfo f = AccessTools.Field(t, name);
            if (f != null) return f.GetValue(instance);
            return null;
        }
    }

    // =========================================================================
    // 3. Persistent Mission State & Manager (survives Save / Load mid-raid)
    // =========================================================================

    public enum VehicleMissionKind
    {
        Raid = 0,
        Visit = 1
    }

    public enum VehicleMissionPhase
    {
        DrivingIn = 0,
        Engaged = 1,
        Trading = 2,
        BoardingToLeave = 3,
        DrivingOut = 4,
        Completed = 5
    }

    public sealed class VehicleMission : IExposable
    {
        public int missionId;
        public VehicleMissionKind kind;
        public VehicleMissionPhase phase;
        public Map map;
        public Faction faction;

        // VehiclePawn derives from Pawn, so Scribe_Collections(LookMode.Reference) persists cleanly without VF types.
        public List<Pawn> vehicles = new List<Pawn>();
        public List<Pawn> crew = new List<Pawn>();
        public List<Pawn> gunners = new List<Pawn>();
        public Pawn trader;
        public Lord assaultLord;
        public Lord tradeLord;

        public IntVec3 stagingCell = IntVec3.Invalid;
        public IntVec3 exitCell = IntVec3.Invalid;
        public int startTick;
        public int phaseStartTick;
        public int stayDurationTicks = 25000;
        public float requestedPoints;
        public float vehiclePointsSpent;
        public bool dismounted;
        public int lastStallCheckTick;
        public IntVec3 lastObservedPos = IntVec3.Invalid;
        public int stagnantTicks;
        public bool repathRetried;

        public Pawn PrimaryVehicle
        {
            get
            {
                if (vehicles == null) return null;
                for (int i = 0; i < vehicles.Count; i++)
                {
                    if (vehicles[i] != null && !vehicles[i].Destroyed) return vehicles[i];
                }
                return null;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref missionId, "missionId");
            Scribe_Values.Look(ref kind, "kind");
            Scribe_Values.Look(ref phase, "phase");
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref vehicles, "vehicles", LookMode.Reference);
            Scribe_Collections.Look(ref crew, "crew", LookMode.Reference);
            Scribe_Collections.Look(ref gunners, "gunners", LookMode.Reference);
            Scribe_References.Look(ref trader, "trader");
            Scribe_References.Look(ref assaultLord, "assaultLord");
            Scribe_References.Look(ref tradeLord, "tradeLord");
            Scribe_Values.Look(ref stagingCell, "stagingCell", IntVec3.Invalid);
            Scribe_Values.Look(ref exitCell, "exitCell", IntVec3.Invalid);
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref phaseStartTick, "phaseStartTick");
            Scribe_Values.Look(ref stayDurationTicks, "stayDurationTicks", 25000);
            Scribe_Values.Look(ref requestedPoints, "requestedPoints");
            Scribe_Values.Look(ref vehiclePointsSpent, "vehiclePointsSpent");
            Scribe_Values.Look(ref dismounted, "dismounted");
            Scribe_Values.Look(ref lastStallCheckTick, "lastStallCheckTick");
            Scribe_Values.Look(ref lastObservedPos, "lastObservedPos", IntVec3.Invalid);
            Scribe_Values.Look(ref stagnantTicks, "stagnantTicks");
            Scribe_Values.Look(ref repathRetried, "repathRetried");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (vehicles == null) vehicles = new List<Pawn>();
                if (crew == null) crew = new List<Pawn>();
                if (gunners == null) gunners = new List<Pawn>();
                vehicles.RemoveAll(delegate(Pawn p) { return p == null; });
                crew.RemoveAll(delegate(Pawn p) { return p == null; });
                gunners.RemoveAll(delegate(Pawn p) { return p == null; });
            }
        }
    }

    public sealed class TMM_VehicleMissionManager : GameComponent
    {
        private const int DrivingTimeoutTicks = 4500;
        private const int BoardingTimeoutTicks = 600;
        private const int TickInterval = 20;

        public List<VehicleMission> missions = new List<VehicleMission>();
        private int nextMissionId = 1;
        private bool resumeCheckedAfterLoad;

        public TMM_VehicleMissionManager(Game game) { }

        public static TMM_VehicleMissionManager Instance
        {
            get { return Current.Game == null ? null : Current.Game.GetComponent<TMM_VehicleMissionManager>(); }
        }

        public VehicleMission RegisterMission(VehicleMission mission)
        {
            if (mission == null) return null;
            mission.missionId = nextMissionId++;
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            mission.startTick = now;
            mission.phaseStartTick = now;
            missions.Add(mission);
            return mission;
        }

        public VehicleMission FindVisitMissionForTrader(Pawn traderPawn)
        {
            if (traderPawn == null || missions == null) return null;
            for (int i = 0; i < missions.Count; i++)
            {
                VehicleMission m = missions[i];
                if (m != null && m.kind == VehicleMissionKind.Visit && m.trader == traderPawn
                    && m.phase != VehicleMissionPhase.Completed)
                {
                    return m;
                }
            }
            return null;
        }

        public VehicleMission FindMissionForVehicle(Pawn vehicle)
        {
            if (vehicle == null || missions == null) return null;
            for (int i = 0; i < missions.Count; i++)
            {
                VehicleMission m = missions[i];
                if (m != null && m.vehicles != null && m.vehicles.Contains(vehicle))
                {
                    return m;
                }
            }
            return null;
        }

        public void BeginVisitDeparture(VehicleMission mission)
        {
            if (mission == null || mission.kind != VehicleMissionKind.Visit) return;
            if (mission.phase == VehicleMissionPhase.BoardingToLeave
                || mission.phase == VehicleMissionPhase.DrivingOut
                || mission.phase == VehicleMissionPhase.Completed)
            {
                return;
            }

            if (mission.tradeLord != null && mission.map != null && mission.map.lordManager != null)
            {
                mission.map.lordManager.RemoveLord(mission.tradeLord);
                mission.tradeLord = null;
            }

            mission.phase = VehicleMissionPhase.BoardingToLeave;
            mission.phaseStartTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            mission.repathRetried = false;
            mission.stagnantTicks = 0;
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            resumeCheckedAfterLoad = false;
        }

        public override void GameComponentTick()
        {
            if (!VfBridge.IsAvailable || missions == null || missions.Count == 0) return;

            int now = Find.TickManager.TicksGame;
            if (!resumeCheckedAfterLoad)
            {
                resumeCheckedAfterLoad = true;
                ResumeDrivingMissionsAfterLoad();
            }

            if (now % TickInterval != 0) return;

            for (int i = missions.Count - 1; i >= 0; i--)
            {
                VehicleMission m = missions[i];
                if (m == null || m.map == null || !Find.Maps.Contains(m.map) || m.phase == VehicleMissionPhase.Completed)
                {
                    missions.RemoveAt(i);
                    continue;
                }

                if (m.kind == VehicleMissionKind.Raid)
                {
                    TickRaidMission(m, now);
                }
                else
                {
                    TickVisitMission(m, now);
                }

                if (m.phase == VehicleMissionPhase.Completed)
                {
                    missions.RemoveAt(i);
                }
            }
        }

        private void ResumeDrivingMissionsAfterLoad()
        {
            for (int i = 0; i < missions.Count; i++)
            {
                VehicleMission m = missions[i];
                if (m == null || m.map == null) continue;

                if (m.phase == VehicleMissionPhase.DrivingIn && m.stagingCell.IsValid)
                {
                    for (int v = 0; v < m.vehicles.Count; v++)
                    {
                        Pawn veh = m.vehicles[v];
                        if (veh != null && veh.Spawned && !veh.Destroyed && !VfBridge.IsMoving(veh))
                        {
                            VfBridge.SetDrafted(veh, true);
                            VfBridge.StartPath(veh, m.stagingCell);
                        }
                    }
                }
                else if (m.phase == VehicleMissionPhase.DrivingOut && m.exitCell.IsValid)
                {
                    for (int v = 0; v < m.vehicles.Count; v++)
                    {
                        Pawn veh = m.vehicles[v];
                        if (veh != null && veh.Spawned && !veh.Destroyed && !VfBridge.IsMoving(veh))
                        {
                            VfBridge.SetDrafted(veh, true);
                            VfBridge.StartPath(veh, m.exitCell);
                        }
                    }
                }
            }
        }

        private void TickRaidMission(VehicleMission m, int now)
        {
            if (m.phase == VehicleMissionPhase.DrivingIn)
            {
                bool allArrivedOrDisabled = true;
                IntVec3 colonyCenter = VehicleRaidPlacement.FindColonyCenter(m.map);

                for (int i = 0; i < m.vehicles.Count; i++)
                {
                    Pawn veh = m.vehicles[i];
                    if (veh == null || veh.Destroyed || !veh.Spawned) continue;

                    List<Pawn> aboard = VfBridge.GetAllPawnsAboard(veh);
                    if (aboard.Count == 0) continue;

                    bool closeToStaging = m.stagingCell.IsValid && veh.Position.DistanceTo(m.stagingCell) <= 7.5f;
                    bool closeToColony = colonyCenter.IsValid && veh.Position.DistanceTo(colonyCenter) <= 23f;
                    bool timedOut = (now - m.phaseStartTick) >= DrivingTimeoutTicks;
                    bool severelyDamaged = IsVehicleSeverelyDamaged(veh);

                    // Track stagnation so a blocked vehicle retries path once and then dismounts cleanly.
                    bool stalled = false;
                    if (i == 0)
                    {
                        if (m.lastObservedPos.IsValid && veh.Position == m.lastObservedPos)
                        {
                            if (!VfBridge.PathPending(m.PrimaryVehicle)) m.stagnantTicks += TickInterval;
                        }
                        else
                        {
                            m.lastObservedPos = veh.Position;
                            m.stagnantTicks = 0;
                        }

                        if (m.stagnantTicks >= 160 && !m.repathRetried && !closeToStaging)
                        {
                            m.repathRetried = true;
                            IntVec3 alt = VehicleRaidPlacement.FindStagingCell(m.map, veh.Position, colonyCenter, 22, 42, veh);
                            if (alt.IsValid) m.stagingCell = alt;
                            VfBridge.SetDrafted(veh, true);
                            VfBridge.StartPath(veh, m.stagingCell);
                            m.stagnantTicks = 0;
                        }
                        else if (m.stagnantTicks >= 260 && m.repathRetried)
                        {
                            stalled = true;
                        }
                    }

                    if (closeToStaging || closeToColony || timedOut || severelyDamaged || stalled)
                    {
                        DisembarkRaidVehicle(m, veh, severelyDamaged);
                    }
                    else
                    {
                        allArrivedOrDisabled = false;
                    }
                }

                if (allArrivedOrDisabled)
                {
                    m.dismounted = true;
                    m.phase = VehicleMissionPhase.Engaged;
                    m.phaseStartTick = now;
                    EnsureAllDismountedRaidersInLord(m);
                }
            }
            else if (m.phase == VehicleMissionPhase.Engaged)
            {
                EnsureAllDismountedRaidersInLord(m);

                int livingDismounted = CountLivingSpawnedCrew(m, false);
                bool anyTurretActive = false;

                for (int i = 0; i < m.vehicles.Count; i++)
                {
                    Pawn veh = m.vehicles[i];
                    if (veh == null || veh.Destroyed || !veh.Spawned) continue;

                    List<Pawn> aboard = VfBridge.GetAllPawnsAboard(veh);
                    if (aboard.Count == 0) continue;

                    // Keep turret gunner shooting while vehicle is healthy and raid is active; otherwise disembark them.
                    if (livingDismounted > 0 && !IsVehicleSeverelyDamaged(veh) && VfBridge.CanAiOperateTurret(veh))
                    {
                        if (VfBridge.TryTickAiTurrets(veh))
                        {
                            anyTurretActive = true;
                        }
                        else
                        {
                            DisembarkAllIntoAssaultLord(m, veh);
                        }
                    }
                    else
                    {
                        DisembarkAllIntoAssaultLord(m, veh);
                    }
                }

                if (livingDismounted == 0 && !anyTurretActive && CountLivingCrewAboardAnyVehicle(m) == 0)
                {
                    m.phase = VehicleMissionPhase.Completed;
                }
            }
        }

        private void DisembarkRaidVehicle(VehicleMission m, Pawn veh, bool forceDisembarkGunner)
        {
            VfBridge.StopDead(veh);
            VfBridge.SetDrafted(veh, false);

            Pawn gunnerToKeep = null;
            if (!forceDisembarkGunner && VfBridge.CanAiOperateTurret(veh))
            {
                gunnerToKeep = VfBridge.FindAboardTurretGunner(veh); // CHECK VF
                if (gunnerToKeep == null && m.gunners != null)
                {
                    List<Pawn> aboard = VfBridge.GetAllPawnsAboard(veh);
                    for (int i = 0; i < aboard.Count; i++)
                    {
                        if (m.gunners.Contains(aboard[i]) && !aboard[i].Dead && !aboard[i].Downed)
                        {
                            gunnerToKeep = aboard[i];
                            break;
                        }
                    }
                }
            }

            List<Pawn> currentAboard = VfBridge.GetAllPawnsAboard(veh);
            if (gunnerToKeep == null)
            {
                VfBridge.DisembarkAll(veh);
            }
            else
            {
                for (int i = 0; i < currentAboard.Count; i++)
                {
                    Pawn p = currentAboard[i];
                    if (p == null || p == gunnerToKeep) continue;
                    VfBridge.DisembarkPawn(veh, p);
                }
                VfBridge.TryTickAiTurrets(veh);
            }

            EnsureAllDismountedRaidersInLord(m);
        }

        private void DisembarkAllIntoAssaultLord(VehicleMission m, Pawn veh)
        {
            VfBridge.StopDead(veh);
            VfBridge.SetDrafted(veh, false);
            VfBridge.DisembarkAll(veh);
            EnsureAllDismountedRaidersInLord(m);
        }

        private void EnsureAllDismountedRaidersInLord(VehicleMission m)
        {
            if (m == null || m.map == null || m.faction == null) return;

            List<Pawn> unlorded = new List<Pawn>();
            for (int i = 0; i < m.crew.Count; i++)
            {
                Pawn p = m.crew[i];
                if (p == null || p.Dead || p.Destroyed || !p.Spawned || p.Map != m.map || p.IsPrisoner || p.IsSlave || p.Faction != m.faction) continue;
                if (p.GetLord() == null)
                {
                    unlorded.Add(p);
                }
                else if (m.assaultLord == null)
                {
                    m.assaultLord = p.GetLord();
                }
            }

            if (unlorded.Count == 0) return;

            if (m.assaultLord == null || !m.map.lordManager.lords.Contains(m.assaultLord))
            {
                m.assaultLord = LordMaker.MakeNewLord(
                    m.faction,
                    new LordJob_AssaultColony(m.faction, true, true, false, false, true),
                    m.map,
                    unlorded);
            }
            else
            {
                for (int i = 0; i < unlorded.Count; i++)
                {
                    m.assaultLord.AddPawn(unlorded[i]);
                }
            }
        }

        private void TickVisitMission(VehicleMission m, int now)
        {
            // Abort peaceful visit cleanly if faction turned hostile.
            if (m.faction != null && m.faction.HostileTo(Faction.OfPlayer))
            {
                if (m.tradeLord != null && m.map != null && m.map.lordManager != null)
                {
                    m.map.lordManager.RemoveLord(m.tradeLord);
                    m.tradeLord = null;
                }
                for (int i = 0; i < m.vehicles.Count; i++)
                {
                    Pawn v = m.vehicles[i];
                    if (v != null && v.Spawned && !v.Destroyed)
                    {
                        VfBridge.StopDead(v);
                        VfBridge.DisembarkAll(v);
                    }
                }
                EnsureAllDismountedRaidersInLord(m);
                m.phase = VehicleMissionPhase.Completed;
                return;
            }

            Pawn truck = m.PrimaryVehicle;
            if (truck == null || truck.Destroyed || !truck.Spawned)
            {
                m.phase = VehicleMissionPhase.Completed;
                return;
            }

            if (m.phase == VehicleMissionPhase.DrivingIn)
            {
                bool close = m.stagingCell.IsValid && truck.Position.DistanceTo(m.stagingCell) <= 6.5f;
                bool timedOut = (now - m.phaseStartTick) >= DrivingTimeoutTicks;

                if (m.lastObservedPos.IsValid && truck.Position == m.lastObservedPos)
                {
                    if (!VfBridge.PathPending(m.PrimaryVehicle)) m.stagnantTicks += TickInterval;
                }
                else
                {
                    m.lastObservedPos = truck.Position;
                    m.stagnantTicks = 0;
                }

                bool stalled = false;
                if (m.stagnantTicks >= 160 && !m.repathRetried && !close)
                {
                    m.repathRetried = true;
                    IntVec3 alt = VehicleRaidPlacement.FindVisitParkingCell(m.map, truck.Position, truck);
                    if (alt.IsValid) m.stagingCell = alt;
                    VfBridge.SetDrafted(truck, true);
                    VfBridge.StartPath(truck, m.stagingCell);
                    m.stagnantTicks = 0;
                }
                else if (m.stagnantTicks >= 260 && m.repathRetried)
                {
                    stalled = true;
                }

                if (close || timedOut || stalled)
                {
                    VfBridge.StopDead(truck);
                    VfBridge.SetDrafted(truck, false);
                    VfBridge.DisembarkAll(truck);

                    if (m.trader != null && !m.trader.Dead && m.trader.mindState != null)
                    {
                        m.trader.mindState.wantsToTradeWithColony = true;
                    }

                    List<Pawn> traders = new List<Pawn>();
                    for (int i = 0; i < m.crew.Count; i++)
                    {
                        Pawn p = m.crew[i];
                        if (p != null && !p.Dead && !p.Destroyed && p.Spawned && p.Map == m.map)
                        {
                            traders.Add(p);
                        }
                    }

                    if (traders.Count > 0)
                    {
                        IntVec3 chillSpot = truck.Position;
                        m.tradeLord = LordMaker.MakeNewLord(
                            m.faction,
                            new LordJob_TradeWithColony(m.faction, chillSpot),
                            m.map,
                            traders);
                    }

                    m.dismounted = true;
                    m.phase = VehicleMissionPhase.Trading;
                    m.phaseStartTick = now;
                }
            }
            else if (m.phase == VehicleMissionPhase.Trading)
            {
                bool lordLeaving = m.tradeLord == null
                    || !m.map.lordManager.lords.Contains(m.tradeLord)
                    || (m.tradeLord.CurLordToil != null
                        && m.tradeLord.CurLordToil.GetType().Name.IndexOf("Exit", StringComparison.OrdinalIgnoreCase) >= 0);

                bool stayExpired = (now - m.phaseStartTick) >= m.stayDurationTicks;
                if (lordLeaving || stayExpired)
                {
                    BeginVisitDeparture(m);
                }
            }
            else if (m.phase == VehicleMissionPhase.BoardingToLeave)
            {
                bool allBoarded = true;
                bool forceBoardNow = (now - m.phaseStartTick) >= BoardingTimeoutTicks;

                for (int i = 0; i < m.crew.Count; i++)
                {
                    Pawn p = m.crew[i];
                    if (!VfBridge.IsVisitCrewOnFoot(p, truck)) continue;

                    float dist = p.Position.DistanceTo(truck.Position);
                    if (dist <= 4.5f && p.CanReach(truck, PathEndMode.Touch, Danger.Deadly))
                    {
                        if (!VfBridge.TryAddPawn(truck, p))
                        {
                            // Truck full (e.g. extra escort guard); allow them to remain outside.
                            continue;
                        }
                    }
                    else
                    {
                        allBoarded = false;
                        IntVec3 approach;
                        if (p.jobs != null && (p.CurJob == null || p.CurJobDef != JobDefOf.Goto) &&
                            CellFinder.TryFindRandomCellNear(truck.Position, m.map, 4,
                                c => c.Standable(m.map) && p.CanReach(c, PathEndMode.OnCell, Danger.Deadly), out approach))
                        {
                            Job go = JobMaker.MakeJob(JobDefOf.Goto, approach);
                            go.locomotionUrgency = LocomotionUrgency.Jog;
                            p.jobs.TryTakeOrderedJob(go, JobTag.Misc);
                        }
                    }
                }

                if (allBoarded || forceBoardNow)
                {
                    List<Pawn> aboard = VfBridge.GetAllPawnsAboard(truck);
                    if (aboard.Count == 0)
                    {
                        // No living driver left to drive the truck away.
                        m.phase = VehicleMissionPhase.Completed;
                        return;
                    }

                    if (!m.exitCell.IsValid)
                    {
                        m.exitCell = VehicleRaidPlacement.FindVehicleEdgeCell(m.map, VfBridge.GetVehicleSize(truck.def));
                    }

                    VfBridge.RefuelFull(truck, 999f);
                    VfBridge.SetDrafted(truck, true);
                    if (m.exitCell.IsValid)
                    {
                        VfBridge.StartPath(truck, m.exitCell);
                    }
                    m.lastObservedPos = truck.Position;
                    m.stagnantTicks = 0;
                    m.phase = VehicleMissionPhase.DrivingOut;
                    m.phaseStartTick = now;
                }
            }
            else if (m.phase == VehicleMissionPhase.DrivingOut)
            {
                bool nearEdge = truck.Position.DistanceToEdge(m.map) <= 6
                    || (m.exitCell.IsValid && truck.Position.DistanceTo(m.exitCell) <= 6.5f);
                bool timedOut = (now - m.phaseStartTick) >= DrivingTimeoutTicks;

                if (m.lastObservedPos.IsValid && truck.Position == m.lastObservedPos)
                {
                    if (!VfBridge.PathPending(m.PrimaryVehicle)) m.stagnantTicks += TickInterval;
                }
                else
                {
                    m.lastObservedPos = truck.Position;
                    m.stagnantTicks = 0;
                }

                bool stalled = m.stagnantTicks >= 320;
                if (nearEdge || timedOut || stalled)
                {
                    if (VfBridge.ExitNpcVehicleFromMap(truck, m.crew, false))
                        m.phase = VehicleMissionPhase.Completed;
                }
            }
        }

        private static bool IsVehicleSeverelyDamaged(Pawn vehicle)
        {
            if (vehicle == null || vehicle.Destroyed || vehicle.Dead) return true;
            if (vehicle.health != null && vehicle.health.summaryHealth != null)
            {
                return vehicle.health.summaryHealth.SummaryHealthPercent < 0.45f;
            }
            return false;
        }

        private static int CountLivingSpawnedCrew(VehicleMission m, bool includeVehicles)
        {
            int count = 0;
            for (int i = 0; i < m.crew.Count; i++)
            {
                Pawn p = m.crew[i];
                if (p == null || p.Dead || p.Destroyed || !p.Spawned || p.Map != m.map) continue;
                if (!includeVehicles && VfBridge.IsVehiclePawn(p)) continue;
                count++;
            }
            return count;
        }

        private static int CountLivingCrewAboardAnyVehicle(VehicleMission m)
        {
            int count = 0;
            for (int i = 0; i < m.vehicles.Count; i++)
            {
                Pawn v = m.vehicles[i];
                if (v == null || v.Destroyed || !v.Spawned) continue;
                List<Pawn> aboard = VfBridge.GetAllPawnsAboard(v);
                for (int j = 0; j < aboard.Count; j++)
                {
                    if (aboard[j] != null && !aboard[j].Dead && !aboard[j].Destroyed) count++;
                }
            }
            return count;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref missions, "TMM_vehicleMissions", LookMode.Deep);
            Scribe_Values.Look(ref nextMissionId, "TMM_nextVehicleMissionId", 1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (missions == null) missions = new List<VehicleMission>();
                missions.RemoveAll(delegate(VehicleMission m) { return m == null; });
            }
        }
    }

    // =========================================================================
    // 4. Placement & Cell Selection Helpers
    // =========================================================================

    public static class VehicleRaidPlacement
    {
        public static IntVec3 FindColonyCenter(Map map)
        {
            if (map == null) return IntVec3.Invalid;

            int sumX = 0;
            int sumZ = 0;
            int count = 0;

            if (map.listerBuildings != null && map.listerBuildings.allBuildingsColonist != null)
            {
                List<Building> buildings = map.listerBuildings.allBuildingsColonist;
                for (int i = 0; i < buildings.Count; i++)
                {
                    Building b = buildings[i];
                    if (b == null || !b.Spawned) continue;
                    sumX += b.Position.x;
                    sumZ += b.Position.z;
                    count++;
                }
            }

            if (count == 0 && map.mapPawns != null)
            {
                List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn p = colonists[i];
                    if (p == null || !p.Spawned) continue;
                    sumX += p.Position.x;
                    sumZ += p.Position.z;
                    count++;
                }
            }

            if (count > 0)
            {
                IntVec3 avg = new IntVec3(sumX / count, 0, sumZ / count);
                if (avg.InBounds(map)) return avg;
            }
            return map.Center;
        }

        public static IntVec3 FindVehicleEdgeCell(Map map, IntVec2 vehicleSize)
        {
            if (map == null) return IntVec3.Invalid;
            int half = Math.Max(2, Math.Max(vehicleSize.x, vehicleSize.z) / 2 + 1);
            int margin = Math.Max(4, half + 1);

            IntVec3 found;
            if (CellFinder.TryFindRandomEdgeCellWith(
                delegate(IntVec3 c)
                {
                    IntVec3 inset = ClampInset(c, map, margin);
                    return IsClearFootprint(inset, map, half);
                },
                map,
                CellFinder.EdgeRoadChance_Hostile,
                out found))
            {
                return ClampInset(found, map, margin);
            }

            // Relaxed fallback
            if (CellFinder.TryFindRandomEdgeCellWith(
                delegate(IntVec3 c)
                {
                    IntVec3 inset = ClampInset(c, map, 4);
                    return inset.Standable(map) && !inset.Fogged(map);
                },
                map,
                0.5f,
                out found))
            {
                return ClampInset(found, map, 4);
            }

            return IntVec3.Invalid;
        }

        public static IntVec3 FindStagingCell(Map map, IntVec3 entryCell, IntVec3 colonyCenter, int minDist, int maxDist, Pawn vehicle = null)
        {
            if (map == null || !colonyCenter.IsValid) return IntVec3.Invalid;

            IntVec3 best = IntVec3.Invalid;
            float bestScore = float.NegativeInfinity;

            for (int attempt = 0; attempt < 90; attempt++)
            {
                IntVec3 candidate;
                if (!CellFinder.TryFindRandomCellNear(colonyCenter, map, maxDist,
                    delegate(IntVec3 c)
                    {
                        if (!c.InBounds(map) || c.Fogged(map)) return false;
                        float dist = c.DistanceTo(colonyCenter);
                        if (dist < minDist || dist > maxDist) return false;
                        if (c.DistanceToEdge(map) < 5) return false;
                        return IsClearFootprint(c, map, 2);
                    },
                    out candidate))
                {
                    continue;
                }

                // Prefer staging cells on the same side of the colony as the entry cell.
                float score = entryCell.IsValid ? -candidate.DistanceTo(entryCell) : 0f;
                if (score > bestScore && (vehicle == null || VfBridge.CanDrive(vehicle, candidate)))
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best.IsValid) return best;

            // Relaxed fallback: move halfway from entryCell toward colonyCenter.
            if (entryCell.IsValid)
            {
                IntVec3 mid = new IntVec3((entryCell.x + colonyCenter.x) / 2, 0, (entryCell.z + colonyCenter.z) / 2);
                IntVec3 nearMid;
                if (CellFinder.TryFindRandomCellNear(mid, map, 12,
                    delegate(IntVec3 c) { return c.InBounds(map) && !c.Fogged(map) && IsClearFootprint(c, map, 1) && (vehicle == null || VfBridge.CanDrive(vehicle, c)); },
                    out nearMid))
                {
                    return nearMid;
                }
            }

            return vehicle == null || VfBridge.CanDrive(vehicle, colonyCenter) ? colonyCenter : IntVec3.Invalid;
        }

        public static IntVec3 FindVisitParkingCell(Map map, IntVec3 entryCell, Pawn vehicle = null)
        {
            IntVec3 center = FindColonyCenter(map);
            IntVec3 spot = FindStagingCell(map, entryCell, center, 12, 26, vehicle);
            if (spot.IsValid) return spot;
            return FindStagingCell(map, entryCell, center, 6, 36, vehicle);
        }

        /// Finds the vehicle a destination it can really drive to; a vehicle that arrived in a closed pocket of the map edge is
        /// brought in somewhere else. Invalid when no tried edge has a way in.
        public static IntVec3 FindRoute(Pawn vehicle, Map map, Func<Pawn, IntVec3> destination)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                IntVec3 dest = destination(vehicle);
                if (dest.IsValid) return dest;
                IntVec3 edge = FindVehicleEdgeCell(map, vehicle.def.size);
                if (!edge.IsValid) break;
                vehicle.DeSpawn();
                GenSpawn.Spawn(vehicle, edge, map, FacingRotation(edge, FindColonyCenter(map)));
            }
            return IntVec3.Invalid;
        }

        public static Rot4 FacingRotation(IntVec3 from, IntVec3 to)
        {
            int dx = to.x - from.x;
            int dz = to.z - from.z;
            if (Math.Abs(dx) >= Math.Abs(dz))
            {
                return dx >= 0 ? Rot4.East : Rot4.West;
            }
            return dz >= 0 ? Rot4.North : Rot4.South;
        }

        public static bool IsClearFootprint(IntVec3 center, Map map, int radius)
        {
            if (map == null || !center.InBounds(map)) return false;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    IntVec3 c = new IntVec3(center.x + dx, 0, center.z + dz);
                    if (!c.InBounds(map) || c.Fogged(map)) return false;
                    if (!c.Standable(map)) return false;
                    TerrainDef terr = c.GetTerrain(map);
                    if (terr != null && terr.IsWater) return false;
                    if (c.GetEdifice(map) != null) return false;
                }
            }
            return true;
        }

        private static IntVec3 ClampInset(IntVec3 c, Map map, int margin)
        {
            int x = Mathf.Clamp(c.x, margin, map.Size.x - 1 - margin);
            int z = Mathf.Clamp(c.z, margin, map.Size.z - 1 - margin);
            return new IntVec3(x, 0, z);
        }
    }

    // =========================================================================
    // 5. Vehicle Raid IncidentWorker
    // =========================================================================

    public sealed class IncidentWorker_TMMVehicleRaid : IncidentWorker
    {
        private const int CrewPerVehicle = 2;
        public const int MinDayForNaturalRaid = 60;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            if (!VfBridge.IsAvailable) return false;

            Map map = parms.target as Map;
            if (map == null || !map.IsPlayerHome) return false;

            if (!parms.forced && GenDate.DaysPassed < MinDayForNaturalRaid) return false;

            Faction faction = ResolveRaidFaction(parms);
            if (faction == null) return false;

            FactionVehiclesExtension ext = faction.def.GetModExtension<FactionVehiclesExtension>();
            if (ext == null) return false;
            if (!parms.forced && parms.points < ext.minPoints) return false;

            return VehicleRaidPlacement.FindVehicleEdgeCell(map, new IntVec2(2, 4)).IsValid;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            return ExecuteVehicleRaid(parms) != null;
        }

        /// <summary>
        /// Direct entry point returning the created VehicleMission (used by both storyteller and automated probe).
        /// </summary>
        public static VehicleMission ExecuteVehicleRaid(IncidentParms parms)
        {
            if (!VfBridge.IsAvailable || parms == null) return null;
            Map map = parms.target as Map;
            if (map == null) return null;

            Faction faction = ResolveRaidFaction(parms);
            if (faction == null) return null;

            FactionVehiclesExtension ext = faction.def.GetModExtension<FactionVehiclesExtension>();
            if (ext == null) return null;

            float requestedPoints = parms.points;
            if (requestedPoints <= 0f || float.IsNaN(requestedPoints) || float.IsInfinity(requestedPoints)) return null;
            List<FactionVehicleEntry> candidates = GetValidEntries(ext, true);
            PawnKindDef reserveKind = CheapestRaidCrewKind(faction);
            if (candidates.Count == 0 || reserveKind == null) return null;

            int maxCount = Mathf.Clamp(ext.maxVehicles, 1, 3);
            int desiredVehicles = (maxCount >= 2 && requestedPoints >= ext.minPoints * 1.75f) ? 2 : 1;
            float reservePerVehicle = CrewPerVehicle * reserveKind.combatPower;

            List<Def> chosenDefs = new List<Def>();
            List<float> chosenCosts = new List<float>();
            float totalVehiclePoints = 0f;
            for (int i = 0; i < desiredVehicles; i++)
            {
                float maxTotalVehiclePoints = Mathf.Min(requestedPoints * 0.45f, requestedPoints - reservePerVehicle * (i + 1));
                float available = maxTotalVehiclePoints - totalVehiclePoints;
                List<FactionVehicleEntry> affordable = candidates.Where(e =>
                    ComputeVehiclePointCost(VfBridge.ResolveVehicleDef(e.defName), e, ext) <= available).ToList();
                FactionVehicleEntry picked = PickWeighted(affordable);
                if (picked == null) break;

                Def vDef = VfBridge.ResolveVehicleDef(picked.defName);
                float cost = ComputeVehiclePointCost(vDef, picked, ext);
                chosenDefs.Add(vDef);
                chosenCosts.Add(cost);
                totalVehiclePoints += cost;
            }

            if (chosenDefs.Count == 0) return null;
            IntVec3 colonyCenter = VehicleRaidPlacement.FindColonyCenter(map);
            IntVec3 primaryEntry = VehicleRaidPlacement.FindVehicleEdgeCell(map, VfBridge.GetVehicleSize(chosenDefs[0]));
            if (!primaryEntry.IsValid) return null;
            List<Pawn> raiders = GenerateRaidPawns(faction, map, requestedPoints - totalVehiclePoints,
                chosenDefs.Count * CrewPerVehicle, reserveKind);
            if (raiders.Count < chosenDefs.Count * CrewPerVehicle)
            {
                foreach (Pawn pawn in raiders) SaveHygiene.DiscardUnused(pawn);
                return null;
            }

            IntVec3 stagingCell = VehicleRaidPlacement.FindStagingCell(map, primaryEntry, colonyCenter, 25, 40);
            if (!stagingCell.IsValid) stagingCell = colonyCenter;

            VehicleMission mission = new VehicleMission();
            mission.kind = VehicleMissionKind.Raid;
            mission.phase = VehicleMissionPhase.DrivingIn;
            mission.map = map;
            mission.faction = faction;
            mission.stagingCell = stagingCell;
            mission.requestedPoints = requestedPoints;

            // Spawn vehicles near primaryEntry without overlapping footprints.
            for (int i = 0; i < chosenDefs.Count; i++)
            {
                IntVec3 spawnCell = primaryEntry;
                if (i > 0)
                {
                    IntVec3 offsetCell;
                    if (CellFinder.TryFindRandomCellNear(primaryEntry, map, 8,
                        delegate(IntVec3 c)
                        {
                            if (c.DistanceTo(primaryEntry) < 4.5f) return false;
                            return VehicleRaidPlacement.IsClearFootprint(c, map, 2);
                        },
                        out offsetCell))
                    {
                        spawnCell = offsetCell;
                    }
                    else
                    {
                        continue;
                    }
                }

                Rot4 rot = VehicleRaidPlacement.FacingRotation(spawnCell, stagingCell);
                Pawn vehicle = VfBridge.SpawnVehicle(chosenDefs[i], spawnCell, map, faction, rot);
                if (vehicle == null) continue;
                if (mission.vehicles.Count == 0)
                {
                    IntVec3 route = VehicleRaidPlacement.FindRoute(vehicle, map, delegate(Pawn v) { return VehicleRaidPlacement.FindStagingCell(map, v.Position, colonyCenter, 25, 40, v); });
                    if (route.IsValid)
                    {
                        stagingCell = route;
                        mission.stagingCell = route;
                        primaryEntry = vehicle.Position;
                    }
                }
                if (!VfBridge.CanDrive(vehicle, stagingCell))
                {
                    if (vehicle.Spawned) vehicle.DeSpawn();
                    if (!vehicle.Destroyed) vehicle.Destroy();
                    continue;
                }

                VfBridge.RefuelFull(vehicle, 999f);
                mission.vehicles.Add(vehicle);
                mission.vehiclePointsSpent += chosenCosts[i];
            }

            if (mission.vehicles.Count == 0)
            {
                for (int i = 0; i < raiders.Count; i++)
                {
                    SaveHygiene.DiscardUnused(raiders[i]);
                }
                return null;
            }

            // Load both jobs on each vehicle before assigning spare seats.
            int raiderIdx = 0;
            foreach (Pawn vehicle in mission.vehicles)
            {
                for (int position = 0; position < CrewPerVehicle; position++)
                {
                    Pawn pawn = raiders[raiderIdx];
                    if (!VfBridge.TryAddPawn(vehicle, pawn)) break;
                    mission.crew.Add(pawn);
                    if (position == 1) mission.gunners.Add(pawn);
                    raiderIdx++;
                }
            }

            foreach (Pawn vehicle in mission.vehicles)
            {
                while (raiderIdx < raiders.Count && VfBridge.TryAddPawn(vehicle, raiders[raiderIdx]))
                    mission.crew.Add(raiders[raiderIdx++]);
            }

            // Any extra raiders who did not fit in seats spawn near the primary vehicle and join the assault.
            List<Pawn> overflowRaiders = new List<Pawn>();
            Pawn leadVehicle = mission.vehicles[0];
            while (raiderIdx < raiders.Count)
            {
                Pawn extra = raiders[raiderIdx++];
                IntVec3 footCell;
                if (!CellFinder.TryFindRandomCellNear(leadVehicle.Position, map, 5,
                    delegate(IntVec3 c) { return c.InBounds(map) && c.Standable(map); },
                    out footCell))
                {
                    footCell = leadVehicle.Position;
                }
                GenSpawn.Spawn(extra, footCell, map);
                mission.crew.Add(extra);
                overflowRaiders.Add(extra);
            }

            if (overflowRaiders.Count > 0)
            {
                mission.assaultLord = LordMaker.MakeNewLord(
                    faction,
                    new LordJob_AssaultColony(faction, true, true, false, false, true),
                    map,
                    overflowRaiders);
            }

            // Start async VF pathing toward the staging point.
            for (int v = 0; v < mission.vehicles.Count; v++)
            {
                Pawn veh = mission.vehicles[v];
                VfBridge.SetDrafted(veh, true);
                VfBridge.StartPath(veh, stagingCell);
            }
            mission.lastObservedPos = leadVehicle.Position;

            TMM_VehicleMissionManager mgr = TMM_VehicleMissionManager.Instance;
            if (mgr != null)
            {
                mgr.RegisterMission(mission);
            }

            if (parms.sendLetter && Find.LetterStack != null)
            {
                string title = "TMM.VehicleRaid.LetterLabel".Translate(faction.Name);
                string text = "TMM.VehicleRaid.LetterText".Translate(faction.Name, mission.vehicles.Count);
                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.ThreatBig, leadVehicle);
            }

            return mission;
        }

        public static float ComputeVehiclePointCost(Def vehicleDef, FactionVehicleEntry entry, FactionVehiclesExtension ext)
        {
            if (entry != null && entry.pointsCostOverride > 0f) return entry.pointsCostOverride;
            float factor = ext != null && ext.marketValuePointsFactor > 0f ? ext.marketValuePointsFactor : 0.085f;
            float mv = VfBridge.GetVehicleMarketValue(vehicleDef);
            return Mathf.Max(150f, mv * factor);
        }

        private static Faction ResolveRaidFaction(IncidentParms parms)
        {
            if (parms != null && parms.faction != null)
            {
                if (IsEligibleRaidFaction(parms.faction, parms.points, parms.forced))
                {
                    return parms.faction;
                }
                return null;
            }

            if (Find.FactionManager == null) return null;
            List<Faction> eligible = new List<Faction>();
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (IsEligibleRaidFaction(f, parms != null ? parms.points : 0f, parms != null && parms.forced))
                {
                    eligible.Add(f);
                }
            }
            if (eligible.Count == 0) return null;
            return eligible.RandomElement();
        }

        private static bool IsEligibleRaidFaction(Faction f, float points, bool forced)
        {
            if (f == null || f.IsPlayer || f.defeated || f.def == null || f.def.hidden) return false;
            if (!f.HostileTo(Faction.OfPlayer)) return false;
            FactionVehiclesExtension ext = f.def.GetModExtension<FactionVehiclesExtension>();
            if (ext == null) return false;
            if (!forced && points < ext.minPoints) return false;
            PawnKindDef reserveKind = CheapestRaidCrewKind(f);
            if (reserveKind == null) return false;
            float vehicleBudget = Mathf.Min(points * 0.45f, points - reserveKind.combatPower * CrewPerVehicle);
            return GetValidEntries(ext, true).Any(entry =>
                ComputeVehiclePointCost(VfBridge.ResolveVehicleDef(entry.defName), entry, ext) <= vehicleBudget);
        }

        internal static List<FactionVehicleEntry> GetValidEntries(FactionVehiclesExtension ext, bool forRaids)
        {
            List<FactionVehicleEntry> result = new List<FactionVehicleEntry>();
            if (ext == null || ext.vehicles == null) return result;
            for (int i = 0; i < ext.vehicles.Count; i++)
            {
                FactionVehicleEntry e = ext.vehicles[i];
                if (e == null || e.weight <= 0f || string.IsNullOrEmpty(e.defName)) continue;
                if (forRaids && !e.forRaids) continue;
                if (!forRaids && !e.forVisits) continue;
                if (VfBridge.ResolveVehicleDef(e.defName) != null)
                {
                    result.Add(e);
                }
            }
            return result;
        }

        internal static FactionVehicleEntry PickWeighted(List<FactionVehicleEntry> entries)
        {
            if (entries == null || entries.Count == 0) return null;
            float total = 0f;
            for (int i = 0; i < entries.Count; i++) total += entries[i].weight;
            float roll = Rand.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                acc += entries[i].weight;
                if (roll <= acc) return entries[i];
            }
            return entries[entries.Count - 1];
        }

        private static PawnKindDef CheapestRaidCrewKind(Faction faction)
        {
            if (faction.def.pawnGroupMakers == null) return null;
            return faction.def.pawnGroupMakers.Where(m => m != null && m.kindDef == PawnGroupKindDefOf.Combat && m.options != null)
                .SelectMany(m => m.options)
                .Where(o => o != null && o.selectionWeight > 0f && o.kind != null && o.kind.RaceProps.Humanlike && o.kind.combatPower > 0f)
                .Select(o => o.kind).OrderBy(kind => kind.combatPower).FirstOrDefault();
        }

        private static List<Pawn> GenerateRaidPawns(Faction faction, Map map, float points, int minCrew, PawnKindDef fallbackKind)
        {
            float pointsLeft = points;
            List<Pawn> result = new List<Pawn>();
            try
            {
                PawnGroupMakerParms groupParms = new PawnGroupMakerParms();
                groupParms.groupKind = PawnGroupKindDefOf.Combat;
                groupParms.tile = map.Tile;
                groupParms.faction = faction;
                groupParms.points = points;
                groupParms.inhabitants = false;
                groupParms.seed = null;

                IEnumerable<Pawn> generated = PawnGroupMakerUtility.GeneratePawns(groupParms, false);
                if (generated != null)
                {
                    foreach (Pawn p in generated)
                    {
                        if (p == null) continue;
                        int reservedSeats = Math.Max(0, minCrew - result.Count - 1);
                        if (!p.Dead && !p.Downed && p.RaceProps != null && p.RaceProps.Humanlike &&
                            p.kindDef.combatPower + reservedSeats * fallbackKind.combatPower <= pointsLeft)
                        {
                            result.Add(p);
                            pointsLeft -= p.kindDef.combatPower;
                        }
                        else if (!p.Spawned)
                        {
                            SaveHygiene.DiscardUnused(p);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] GenerateRaidPawns via PawnGroupMakerUtility failed, using fallback: " + ex.Message);
            }

            while (result.Count < minCrew && pointsLeft >= fallbackKind.combatPower)
            {
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    fallbackKind, faction, PawnGenerationContext.NonPlayer, map.Tile,
                    true, false, false, false, true, 1f, false, true, false, false,
                    false, false, false, false, false, 0f, 0f, null, 1f,
                    null, null, null, null, null, null, null, null, null, null, null, null,
                    false, false, false, false, null, null, null, null, null, 0f,
                    DevelopmentalStage.Adult)); // CHECK API
                if (p != null && !p.Dead && !p.Downed)
                {
                    result.Add(p);
                    pointsLeft -= fallbackKind.combatPower;
                }
                else
                {
                    if (p != null && !p.Spawned) SaveHygiene.DiscardUnused(p);
                    break;
                }
            }

            return result;
        }
    }

    // =========================================================================
    // 6. Peaceful Trade Visit on Vehicle (Cargo Hauler / Hunter Pickup)
    // =========================================================================

    public sealed class IncidentWorker_TMMVehicleVisit : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            if (!VfBridge.IsAvailable) return false;

            Map map = parms.target as Map;
            if (map == null || !map.IsPlayerHome) return false;

            Faction faction = ResolveVisitFaction(parms);
            if (faction == null) return false;

            return VehicleRaidPlacement.FindVehicleEdgeCell(map, new IntVec2(3, 5)).IsValid;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            return ExecuteVehicleVisit(parms) != null;
        }

        public static VehicleMission ExecuteVehicleVisit(IncidentParms parms)
        {
            if (!VfBridge.IsAvailable || parms == null) return null;
            Map map = parms.target as Map;
            if (map == null) return null;

            Faction faction = ResolveVisitFaction(parms);
            if (faction == null) return null;

            FactionVehiclesExtension ext = faction.def.GetModExtension<FactionVehiclesExtension>();
            if (ext == null) return null;

            List<FactionVehicleEntry> candidates = IncidentWorker_TMMVehicleRaid.GetValidEntries(ext, false);
            if (candidates.Count == 0) return null;

            FactionVehicleEntry picked = IncidentWorker_TMMVehicleRaid.PickWeighted(candidates);
            Def vDef = picked == null ? null : VfBridge.ResolveVehicleDef(picked.defName);
            if (vDef == null) return null;

            TraderKindDef traderKind = ResolveCaravanTraderKind(faction);
            if (traderKind == null) return null;

            IntVec3 entryCell = VehicleRaidPlacement.FindVehicleEdgeCell(map, VfBridge.GetVehicleSize(vDef));
            if (!entryCell.IsValid) return null;

            IntVec3 parkingCell = VehicleRaidPlacement.FindVisitParkingCell(map, entryCell);
            if (!parkingCell.IsValid) return null;

            Rot4 rot = VehicleRaidPlacement.FacingRotation(entryCell, parkingCell);
            Pawn truck = VfBridge.SpawnVehicle(vDef, entryCell, map, faction, rot);
            if (truck == null) return null;
            parkingCell = VehicleRaidPlacement.FindRoute(truck, map, delegate(Pawn v) { return VehicleRaidPlacement.FindVisitParkingCell(map, v.Position, v); });
            if (!parkingCell.IsValid)
            {
                if (truck.Spawned) truck.DeSpawn();
                if (!truck.Destroyed) truck.Destroy();
                return null;
            }
            entryCell = truck.Position;

            VfBridge.RefuelFull(truck, 999f);

            // Generate trader + driver/guard crew sized for the truck.
            Pawn traderPawn = GenerateVisitorPawn(faction, map, traderKind);
            Pawn driverGuard = GenerateVisitorPawn(faction, map, null);
            if (traderPawn == null || driverGuard == null)
            {
                if (truck.Spawned) truck.DeSpawn();
                if (!truck.Destroyed) truck.Destroy();
                return null;
            }

            // Fill the truck's trunk (inventory.innerContainer) with real generated trader stock.
            StockTruckTrunk(truck, traderKind, faction, map);

            VehicleMission mission = new VehicleMission();
            mission.kind = VehicleMissionKind.Visit;
            mission.phase = VehicleMissionPhase.DrivingIn;
            mission.map = map;
            mission.faction = faction;
            mission.trader = traderPawn;
            mission.stagingCell = parkingCell;
            mission.exitCell = entryCell;
            mission.stayDurationTicks = 25000;
            mission.vehicles.Add(truck);

            // Board driver first, then trader.
            if (VfBridge.TryAddPawn(truck, driverGuard)) mission.crew.Add(driverGuard);
            if (VfBridge.TryAddPawn(truck, traderPawn)) mission.crew.Add(traderPawn);

            // If a pawn could not board, spawn beside truck so nothing leaks.
            if (!mission.crew.Contains(driverGuard))
            {
                GenSpawn.Spawn(driverGuard, truck.Position, map);
                mission.crew.Add(driverGuard);
            }
            if (!mission.crew.Contains(traderPawn))
            {
                GenSpawn.Spawn(traderPawn, truck.Position, map);
                mission.crew.Add(traderPawn);
            }

            VfBridge.SetDrafted(truck, true);
            VfBridge.StartPath(truck, parkingCell);
            mission.lastObservedPos = truck.Position;

            TMM_VehicleMissionManager mgr = TMM_VehicleMissionManager.Instance;
            if (mgr != null)
            {
                mgr.RegisterMission(mission);
            }

            if (parms.sendLetter && Find.LetterStack != null)
            {
                string title = "TMM.VehicleVisit.LetterLabel".Translate(faction.Name);
                string text = "TMM.VehicleVisit.LetterText".Translate(faction.Name, truck.LabelShort);
                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, truck);
            }

            return mission;
        }

        private static Faction ResolveVisitFaction(IncidentParms parms)
        {
            if (parms != null && parms.faction != null)
            {
                if (IsEligibleVisitFaction(parms.faction)) return parms.faction;
                return null;
            }

            if (Find.FactionManager == null) return null;
            List<Faction> eligible = new List<Faction>();
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (IsEligibleVisitFaction(all[i])) eligible.Add(all[i]);
            }
            if (eligible.Count == 0) return null;
            return eligible.RandomElement();
        }

        private static bool IsEligibleVisitFaction(Faction f)
        {
            if (f == null || f.IsPlayer || f.defeated || f.def == null || f.def.hidden) return false;
            if (f.HostileTo(Faction.OfPlayer)) return false;
            FactionVehiclesExtension ext = f.def.GetModExtension<FactionVehiclesExtension>();
            if (ext == null) return false;
            if (ResolveCaravanTraderKind(f) == null) return false;
            return IncidentWorker_TMMVehicleRaid.GetValidEntries(ext, false).Count > 0;
        }

        private static TraderKindDef ResolveCaravanTraderKind(Faction faction)
        {
            if (faction == null || faction.def == null) return null;
            if (faction.def.caravanTraderKinds != null && faction.def.caravanTraderKinds.Count > 0)
            {
                return faction.def.caravanTraderKinds.RandomElement();
            }
            if (faction.def.baseTraderKinds != null && faction.def.baseTraderKinds.Count > 0)
            {
                return faction.def.baseTraderKinds.RandomElement();
            }
            return DefDatabase<TraderKindDef>.AllDefsListForReading.FirstOrDefault(
                delegate(TraderKindDef k) { return k != null && !k.orbital; });
        }

        private static Pawn GenerateVisitorPawn(Faction faction, Map map, TraderKindDef traderKind)
        {
            PawnKindDef kind = faction.RandomPawnKind();
            if (kind == null) kind = PawnKindDefOf.Villager;

            Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, faction, PawnGenerationContext.NonPlayer, map.Tile,
                true, false, false, false, true, 1f, false, true, false, false,
                false, false, false, false, false, 0f, 0f, null, 1f,
                null, null, null, null, null, null, null, null, null, null, null, null,
                false, false, false, false, null, null, null, null, null, 0f,
                DevelopmentalStage.Adult)); // CHECK API

            if (p != null && traderKind != null)
            {
                if (p.trader == null) p.trader = new Pawn_TraderTracker(p);
                p.trader.traderKind = traderKind;
                if (p.mindState != null) p.mindState.wantsToTradeWithColony = true;
            }
            return p;
        }

        private static void StockTruckTrunk(Pawn truck, TraderKindDef traderKind, Faction faction, Map map)
        {
            if (truck == null || truck.inventory == null || truck.inventory.innerContainer == null) return;
            if (traderKind == null || ThingSetMakerDefOf.TraderStock == null) return;

            try
            {
                ThingSetMakerParams stockParms = default(ThingSetMakerParams);
                stockParms.traderDef = traderKind;
                stockParms.tile = map.Tile;
                stockParms.makingFaction = faction;

                List<Thing> stock = ThingSetMakerDefOf.TraderStock.root.Generate(stockParms);
                if (stock != null)
                {
                    for (int i = 0; i < stock.Count; i++)
                    {
                        Thing item = stock[i];
                        if (item == null || item.Destroyed) continue;
                        if (item is Pawn)
                        {
                            // Keep living animals out of cargo trunk to prevent inventory bugs.
                            item.Destroy();
                            continue;
                        }
                        if (!truck.inventory.innerContainer.TryAdd(item, true))
                        {
                            item.Destroy();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StockTruckTrunk fallback: " + ex.Message);
            }

            // Guarantee silver is present in trunk for player sales.
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = 1200;
            truck.inventory.innerContainer.TryAdd(silver, true);
        }
    }

    // =========================================================================
    // 7. Harmony Patches: Truck-Trunk Trading & Abandoned Vehicle Trophy Claim
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class VehicleRaidPatches
    {
        private static readonly FieldInfo TraderPawnField = AccessTools.Field(typeof(Pawn_TraderTracker), "pawn"); // CHECK API
        private static Texture2D claimIcon;

        static VehicleRaidPatches()
        {
            if (!VfBridge.IsAvailable) return;

            var harmony = new Harmony("turbomegamod.vehicleraids");

            MethodInfo getGizmos = AccessTools.Method(typeof(Pawn), "GetGizmos");
            if (getGizmos != null)
            {
                harmony.Patch(getGizmos, null, new HarmonyMethod(typeof(VehicleRaidPatches), "Pawn_GetGizmos_Postfix"));
            }

            MethodInfo goodsGetter = AccessTools.PropertyGetter(typeof(Pawn_TraderTracker), "Goods"); // CHECK API
            if (goodsGetter != null)
            {
                harmony.Patch(goodsGetter, null, new HarmonyMethod(typeof(VehicleRaidPatches), "TraderTracker_Goods_Postfix"));
            }

            MethodInfo giveToTrader = AccessTools.Method(
                typeof(Pawn_TraderTracker),
                "GiveSoldThingToTrader",
                new Type[] { typeof(Thing), typeof(int), typeof(Pawn) }); // CHECK API
            if (giveToTrader != null)
            {
                harmony.Patch(giveToTrader, new HarmonyMethod(typeof(VehicleRaidPatches), "TraderTracker_GiveSoldThingToTrader_Prefix"), null);
            }
        }

        private static Texture2D GetClaimIcon()
        {
            if (claimIcon == null)
            {
                claimIcon = ContentFinder<Texture2D>.Get("UI/Designators/Claim", false);
                if (claimIcon == null) claimIcon = TexCommand.Draft;
            }
            return claimIcon;
        }

        /// <summary>
        /// Allows the player to claim an unmanned enemy/abandoned vehicle as a trophy once enemy crew are cleared,
        /// unless VF already added a Claim gizmo of its own. // CHECK VF
        /// </summary>
        private static void Pawn_GetGizmos_Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!VfBridge.IsVehiclePawn(__instance)) return;
            if (!__instance.Spawned || __instance.Destroyed || __instance.Dead) return;
            if (__instance.Faction == Faction.OfPlayer) return;

            if (VfBridge.GetAllPawnsAboard(__instance).Count > 0) return;
            if (HasNearbyConsciousOwner(__instance, 24f)) return;

            List<Gizmo> list = new List<Gizmo>();
            bool alreadyHasClaim = false;
            if (__result != null)
            {
                foreach (Gizmo g in __result)
                {
                    list.Add(g);
                    Command_Action cmd = g as Command_Action;
                    if (cmd != null && !string.IsNullOrEmpty(cmd.defaultLabel))
                    {
                        if (cmd.defaultLabel.IndexOf("Claim", StringComparison.OrdinalIgnoreCase) >= 0
                            || cmd.defaultLabel.IndexOf("Захват", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            alreadyHasClaim = true;
                        }
                    }
                }
            }

            if (alreadyHasClaim) return;

            Pawn vehicle = __instance;
            Command_Action claim = new Command_Action();
            claim.defaultLabel = "TMM.VehicleRaid.ClaimLabel".Translate();
            claim.defaultDesc = "TMM.VehicleRaid.ClaimDesc".Translate(vehicle.LabelShort);
            claim.icon = GetClaimIcon();
            claim.action = delegate
            {
                if (vehicle == null || vehicle.Destroyed || vehicle.Faction == Faction.OfPlayer) return;
                if (VfBridge.GetAllPawnsAboard(vehicle).Count > 0) return;
                vehicle.SetFaction(Faction.OfPlayer);
                VfBridge.SetDrafted(vehicle, false);
                Messages.Message("TMM.VehicleRaid.ClaimedMsg".Translate(vehicle.LabelShort), vehicle, MessageTypeDefOf.PositiveEvent);
            };
            list.Add(claim);
            __result = list;
        }

        private static bool HasNearbyConsciousOwner(Pawn vehicle, float radius)
        {
            if (vehicle == null || vehicle.Map == null || vehicle.Faction == null) return false;
            List<Pawn> pawns = vehicle.Map.mapPawns.SpawnedPawnsInFaction(vehicle.Faction);
            if (pawns == null) return false;
            float rSq = radius * radius;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == vehicle || p.Dead || p.Downed || VfBridge.IsVehiclePawn(p)) continue;
                if ((p.Position - vehicle.Position).LengthHorizontalSquared <= rSq) return true;
            }
            return false;
        }

        /// <summary>
        /// Exposes the visiting cargo truck's trunk inventory as tradable goods when trading with the truck's trader.
        /// </summary>
        private static void TraderTracker_Goods_Postfix(Pawn_TraderTracker __instance, ref IEnumerable<Thing> __result)
        {
            Pawn traderPawn = GetTrackedPawn(__instance);
            if (traderPawn == null) return;

            TMM_VehicleMissionManager mgr = TMM_VehicleMissionManager.Instance;
            VehicleMission visit = mgr == null ? null : mgr.FindVisitMissionForTrader(traderPawn);
            if (visit == null) return;

            Pawn truck = visit.PrimaryVehicle;
            if (truck == null || !truck.Spawned || truck.Destroyed || truck.inventory == null || truck.inventory.innerContainer == null)
            {
                return;
            }

            List<Thing> merged = new List<Thing>();
            HashSet<int> seen = new HashSet<int>();
            if (__result != null)
            {
                foreach (Thing t in __result)
                {
                    if (t != null && !t.Destroyed && seen.Add(t.thingIDNumber))
                    {
                        merged.Add(t);
                    }
                }
            }

            IList<Thing> trunk = truck.inventory.innerContainer.InnerListForReading;
            for (int i = 0; i < trunk.Count; i++)
            {
                Thing item = trunk[i];
                if (item != null && !item.Destroyed && item.stackCount > 0 && seen.Add(item.thingIDNumber))
                {
                    merged.Add(item);
                }
            }

            __result = merged;
        }

        /// <summary>
        /// Routes items sold by the player to a visiting truck-trader straight into the truck's trunk.
        /// </summary>
        private static bool TraderTracker_GiveSoldThingToTrader_Prefix(
            Pawn_TraderTracker __instance,
            Thing toGive,
            int countToGive,
            Pawn playerNegotiator)
        {
            Pawn traderPawn = GetTrackedPawn(__instance);
            if (traderPawn == null || toGive == null || countToGive <= 0) return true;

            TMM_VehicleMissionManager mgr = TMM_VehicleMissionManager.Instance;
            VehicleMission visit = mgr == null ? null : mgr.FindVisitMissionForTrader(traderPawn);
            if (visit == null) return true;

            Pawn truck = visit.PrimaryVehicle;
            if (truck == null || !truck.Spawned || truck.Destroyed || truck.inventory == null || truck.inventory.innerContainer == null)
            {
                return true;
            }

            if (toGive is Pawn) return true;

            Thing split = toGive.SplitOff(countToGive);
            try
            {
                split.PreTraded(TradeAction.PlayerSells, playerNegotiator, traderPawn);
                if (split.Destroyed) return false;

                if (split.holdingOwner != null) split.holdingOwner.Remove(split);
                if (TryAcceptSoldCargo(truck.inventory.innerContainer, split) ||
                    TryAcceptSoldCargo(traderPawn.inventory == null ? null : traderPawn.inventory.innerContainer, split))
                    return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] Truck could not accept sold cargo: " + ex.Message);
            }

            if (split != null && !split.Destroyed && split.holdingOwner == null && !split.Spawned)
                WorldTrafficCustody.Keep(split, traderPawn, false);
            return false;
        }

        internal static Func<ThingOwner, Thing, bool> TestAcceptSoldCargo;

        private static bool TryAcceptSoldCargo(ThingOwner owner, Thing cargo)
        {
            if (owner == null) return false;
            if (TestMode.Enabled && TestAcceptSoldCargo != null && !TestAcceptSoldCargo(owner, cargo))
                return false;
            return owner.TryAdd(cargo, false) && cargo.holdingOwner == owner;
        }

        private static Pawn GetTrackedPawn(Pawn_TraderTracker tracker)
        {
            if (tracker == null || TraderPawnField == null) return null;
            return TraderPawnField.GetValue(tracker) as Pawn;
        }
    }
}
