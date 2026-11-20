using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public enum OutpostMissionState
    {
        Forming,
        Outbound,
        Returning,
        Completed,
        Cancelled,
        Failed,
        RetryDelivery
    }

    /// <summary>
    /// Arrival action for a walking caravan delivering workers and tracked mission silver
    /// to a strategic outpost.
    ///
    /// Round 2 invariants:
    /// - Strictly requires an existing OutpostCaravanMission matching missionId, caravan, and outpost;
    ///   missing/mismatched mission refuses and deducts nothing.
    /// - Delegates delivery to OutpostCaravanManager.ExecuteMissionArrival so both initial vanilla
    ///   arrival (where vanilla clears arrivalAction BEFORE calling Arrived) and the explicit
    ///   RetryDelivery gizmo run the exact same journaled logic.
    /// - Legacy actions without missionId or with incomplete migrated state return the caravan home
    ///   without deducting any silver.
    /// </summary>
    public class CaravanArrivalAction_OutpostDelivery : CaravanArrivalAction
    {
        private string missionId;
        private StrategicOutpost outpost;
        private List<Pawn> stayPawns = new List<Pawn>();
        private int silverAmount = 0;
        private Map originMap;
        private bool alreadyHandled;

        public string MissionId { get { return missionId; } }
        public StrategicOutpost Outpost { get { return outpost; } }
        public Map OriginMap { get { return originMap; } }
        public List<Pawn> StayPawns { get { return stayPawns; } }
        public int SilverAmount { get { return silverAmount; } }

        public override string Label
        {
            get { return "TMM_OutpostCaravan_ReportDelivering".Translate(outpost != null ? outpost.Label : ""); }
        }

        public override string ReportString
        {
            get { return "TMM_OutpostCaravan_ReportDelivering".Translate(outpost != null ? outpost.Label : ""); }
        }

        public CaravanArrivalAction_OutpostDelivery()
        {
        }

        public CaravanArrivalAction_OutpostDelivery(StrategicOutpost outpost, List<Pawn> stayPawns, int silverAmount, Map originMap)
            : this(null, outpost, stayPawns, silverAmount, originMap)
        {
        }

        public CaravanArrivalAction_OutpostDelivery(string missionId, StrategicOutpost outpost, List<Pawn> stayPawns, int silverAmount, Map originMap)
        {
            this.missionId = missionId;
            this.outpost = outpost;
            if (stayPawns != null)
            {
                this.stayPawns = new List<Pawn>(stayPawns);
            }
            this.silverAmount = silverAmount;
            this.originMap = originMap;
            this.alreadyHandled = false;
        }

        internal void BindMigratedMissionId(string id)
        {
            missionId = id;
        }

        internal void MarkHandled()
        {
            alreadyHandled = true;
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport baseReport = base.StillValid(caravan, destinationTile);
            if (!baseReport) return baseReport;
            if (alreadyHandled) return false;
            if (outpost == null || !outpost.Spawned || outpost.Faction != Faction.OfPlayer || outpost.Tile != destinationTile)
            {
                return FloatMenuAcceptanceReport.WithFailReason("TMM_OutpostCaravan_OutpostGone".Translate());
            }

            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            if (mgr == null || missionId.NullOrEmpty())
            {
                return false;
            }

            OutpostCaravanMission m = mgr.GetMissionById(missionId);
            if (m == null || m.caravan != caravan || m.outpost != outpost)
            {
                return false;
            }
            if (m.delivered || m.cancelled)
            {
                return false;
            }
            if (m.state != OutpostMissionState.Outbound &&
                m.state != OutpostMissionState.Forming &&
                m.state != OutpostMissionState.RetryDelivery)
            {
                return false;
            }
            return true;
        }

        public override void Arrived(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed) return;
            if (alreadyHandled) return;
            alreadyHandled = true;

            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            if (mgr == null || missionId.NullOrEmpty())
            {
                // Orphaned / legacy action without verified mission -> return home safely, NEVER deduct
                Messages.Message("TMM_OutpostCaravan_InvalidMissionReturning".Translate(caravan.Name), caravan, MessageTypeDefOf.CautionInput, false);
                OutpostCaravanManager.TryStartReturnPath(caravan, originMap);
                return;
            }

            OutpostCaravanMission mission = mgr.GetMissionById(missionId);
            if (mission == null || mission.caravan != caravan || mission.outpost != outpost)
            {
                // Missing mission or caravan/outpost mismatch -> refuse, deduct nothing
                Messages.Message("TMM_OutpostCaravan_InvalidMissionReturning".Translate(caravan.Name), caravan, MessageTypeDefOf.CautionInput, false);
                OutpostCaravanManager.TryStartReturnPath(caravan, originMap);
                return;
            }

            mgr.ExecuteMissionArrival(mission, caravan);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref missionId, "missionId");
            Scribe_References.Look(ref outpost, "outpost");
            Scribe_Collections.Look(ref stayPawns, "stayPawns", LookMode.Reference);
            Scribe_Values.Look(ref silverAmount, "silverAmount", 0);
            Scribe_References.Look(ref originMap, "originMap");
            Scribe_Values.Look(ref alreadyHandled, "alreadyHandled", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (stayPawns == null) stayPawns = new List<Pawn>();
                stayPawns.RemoveAll(p => p == null);
            }
        }
    }

    /// <summary>
    /// Persistent mission record bound 1-to-1 to a specific formation Lord, its formed Caravan,
    /// and the exact ThingIDs of colony silver stacks loaded for this mission.
    /// </summary>
    public class OutpostCaravanMission : IExposable
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public string missionId;
        public OutpostMissionState state = OutpostMissionState.Forming;
        public StrategicOutpost outpost;
        public Map originMap;
        public Caravan caravan;
        public List<Pawn> pawns = new List<Pawn>();
        public List<Pawn> stayPawns = new List<Pawn>();
        public int silverAmount;
        public int loadedSilver;
        public List<string> missionSilverIds = new List<string>();
        public List<int> transferredWorkerIds = new List<int>();
        public int formationMapId = -1;
        public int formationLordId = -1;
        public bool everAttached;
        public bool silverDelivered;
        public bool delivered;
        public bool cancelled;
        public bool stranded;

        // Legacy fields preserved strictly for clean Scribe deserialization of older saves
        public int baselinePersonalSilverTotal;
        public Dictionary<int, int> baselineSilverByPawn = new Dictionary<int, int>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref schemaVersion, "schemaVersion", 0);
            Scribe_Values.Look(ref missionId, "missionId");
            Scribe_Values.Look(ref state, "state", OutpostMissionState.Forming);
            Scribe_References.Look(ref outpost, "outpost");
            Scribe_References.Look(ref originMap, "originMap");
            Scribe_References.Look(ref caravan, "caravan");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Collections.Look(ref stayPawns, "stayPawns", LookMode.Reference);
            Scribe_Values.Look(ref silverAmount, "silverAmount", 0);
            Scribe_Values.Look(ref loadedSilver, "loadedSilver", 0);
            Scribe_Collections.Look(ref missionSilverIds, "missionSilverIds", LookMode.Value);
            Scribe_Collections.Look(ref transferredWorkerIds, "transferredWorkerIds", LookMode.Value);
            Scribe_Values.Look(ref formationMapId, "formationMapId", -1);
            Scribe_Values.Look(ref formationLordId, "formationLordId", -1);
            Scribe_Values.Look(ref everAttached, "everAttached", false);
            Scribe_Values.Look(ref silverDelivered, "silverDelivered", false);
            Scribe_Values.Look(ref delivered, "delivered", false);
            Scribe_Values.Look(ref cancelled, "cancelled", false);
            Scribe_Values.Look(ref stranded, "stranded", false);
            Scribe_Values.Look(ref baselinePersonalSilverTotal, "baselinePersonalSilverTotal", 0);
            Scribe_Collections.Look(ref baselineSilverByPawn, "baselineSilverByPawn", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (missionId.NullOrEmpty()) missionId = Guid.NewGuid().ToString("N");
                if (pawns == null) pawns = new List<Pawn>();
                if (stayPawns == null) stayPawns = new List<Pawn>();
                if (missionSilverIds == null) missionSilverIds = new List<string>();
                if (transferredWorkerIds == null) transferredWorkerIds = new List<int>();
                if (baselineSilverByPawn == null) baselineSilverByPawn = new Dictionary<int, int>();
                pawns.RemoveAll(p => p == null);
                stayPawns.RemoveAll(p => p == null);
                missionSilverIds.RemoveAll(id => id.NullOrEmpty());
                if (caravan != null) everAttached = true;
            }
        }
    }

    /// <summary>
    /// WorldComponent managing walking caravan missions to strategic outposts.
    ///
    /// Round 2 guarantees:
    /// - Protects exact mission silver ThingIDs from merging with personal silver (and propagates
    ///   IDs across partial SplitOff during hauling).
    /// - Handles storage refusal via explicit RetryDelivery state (with Retry Delivery + Return Home
    ///   gizmos) without leaving a stopped Outbound route after vanilla clears arrivalAction.
    /// - Confirms silver delivery (mission.silverDelivered) independently of individual worker transfers.
    /// - Checks StillValid before direct Enter.Arrived and falls back across all player home maps.
    /// - Migrates legacy saves by inspecting each caravan's real ArrivalAction; incomplete legacy
    ///   missions return home without deducting silver.
    /// </summary>
    public class OutpostCaravanManager : WorldComponent
    {
        private const int AuditIntervalTicks = 250;

        private List<OutpostCaravanMission> missions = new List<OutpostCaravanMission>();
        private bool legacyMigrationPending;

        // Transient fast lookup: ThingID -> missionId for anti-stack-merge and SplitOff propagation
        private static readonly Dictionary<string, string> ActiveMissionSilverToMissionId = new Dictionary<string, string>();

        public List<OutpostCaravanMission> Missions
        {
            get { return missions; }
        }

        public static OutpostCaravanManager Instance
        {
            get { return Find.World == null ? null : Find.World.GetComponent<OutpostCaravanManager>(); }
        }

        public OutpostCaravanManager(World world) : base(world)
        {
        }

        // =====================================================================
        // MISSION SILVER IDENTITY PROTECTION (ANTI-MERGE & SPLIT PROPAGATION)
        // =====================================================================

        public static bool IsMissionSilverThing(Thing t)
        {
            if (ActiveMissionSilverToMissionId.Count == 0 || t == null || t.def != ThingDefOf.Silver) return false;
            return ActiveMissionSilverToMissionId.ContainsKey(t.ThingID);
        }

        public static void RegisterTransientSilverProtection(string thingId, string missionId)
        {
            if (thingId.NullOrEmpty() || missionId.NullOrEmpty()) return;
            ActiveMissionSilverToMissionId[thingId] = missionId;
        }

        public static void UnregisterTransientSilverProtection(string thingId)
        {
            if (thingId.NullOrEmpty()) return;
            ActiveMissionSilverToMissionId.Remove(thingId);
        }

        public void PropagateMissionSilverSplit(string parentThingId, string childThingId)
        {
            if (parentThingId.NullOrEmpty() || childThingId.NullOrEmpty()) return;
            string mId;
            if (!ActiveMissionSilverToMissionId.TryGetValue(parentThingId, out mId)) return;

            ActiveMissionSilverToMissionId[childThingId] = mId;

            OutpostCaravanMission m = GetMissionById(mId);
            if (m != null && !m.silverDelivered)
            {
                if (m.missionSilverIds == null) m.missionSilverIds = new List<string>();
                if (!m.missionSilverIds.Contains(childThingId))
                {
                    m.missionSilverIds.Add(childThingId);
                }
            }
        }

        public void UnprotectMissionSilver(OutpostCaravanMission m)
        {
            if (m == null || m.missionSilverIds == null) return;
            for (int i = 0; i < m.missionSilverIds.Count; i++)
            {
                string id = m.missionSilverIds[i];
                if (!id.NullOrEmpty())
                {
                    ActiveMissionSilverToMissionId.Remove(id);
                }
            }
            m.missionSilverIds.Clear();
        }

        private void RebuildMissionSilverCache()
        {
            ActiveMissionSilverToMissionId.Clear();
            if (missions == null) return;
            for (int i = 0; i < missions.Count; i++)
            {
                OutpostCaravanMission m = missions[i];
                if (m == null || m.silverDelivered || m.missionSilverIds == null) continue;
                if (m.state == OutpostMissionState.Completed || m.state == OutpostMissionState.Failed) continue;

                for (int j = 0; j < m.missionSilverIds.Count; j++)
                {
                    string id = m.missionSilverIds[j];
                    if (!id.NullOrEmpty())
                    {
                        ActiveMissionSilverToMissionId[id] = m.missionId;
                    }
                }
            }
        }

        // =====================================================================
        // REGISTRATION & LOOKUPS
        // =====================================================================

        public void RegisterMission(StrategicOutpost outpost, Map originMap, List<Pawn> pawns, List<Pawn> stayPawns, int silverAmount)
        {
            RegisterMissionBound(outpost, originMap, pawns, stayPawns, silverAmount,
                originMap != null ? originMap.uniqueID : -1, -1, null, null, null);
        }

        public OutpostCaravanMission RegisterMissionBound(
            StrategicOutpost outpost,
            Map originMap,
            List<Pawn> pawns,
            List<Pawn> stayPawns,
            int silverAmount,
            int formationMapId,
            int formationLordId,
            Dictionary<int, int> baselineSilverByPawn)
        {
            return RegisterMissionBound(outpost, originMap, pawns, stayPawns, silverAmount,
                formationMapId, formationLordId, baselineSilverByPawn, null, null);
        }

        public OutpostCaravanMission RegisterMissionBound(
            StrategicOutpost outpost,
            Map originMap,
            List<Pawn> pawns,
            List<Pawn> stayPawns,
            int silverAmount,
            int formationMapId,
            int formationLordId,
            Dictionary<int, int> baselineSilverByPawn,
            List<string> missionSilverIds,
            string preassignedMissionId)
        {
            if (outpost == null || pawns == null || pawns.Count == 0) return null;

            OutpostCaravanMission mission = new OutpostCaravanMission();
            mission.schemaVersion = OutpostCaravanMission.CurrentSchemaVersion;
            mission.missionId = !preassignedMissionId.NullOrEmpty() ? preassignedMissionId : Guid.NewGuid().ToString("N");
            mission.state = OutpostMissionState.Forming;
            mission.outpost = outpost;
            mission.originMap = originMap;
            mission.pawns = pawns.Where(p => p != null).ToList();
            mission.stayPawns = stayPawns != null ? stayPawns.Where(p => p != null).ToList() : new List<Pawn>();
            mission.silverAmount = Mathf.Max(0, silverAmount);
            mission.loadedSilver = 0;
            mission.silverDelivered = (mission.silverAmount == 0);
            mission.missionSilverIds = missionSilverIds != null ? new List<string>(missionSilverIds) : new List<string>();
            mission.transferredWorkerIds = new List<int>();
            mission.formationMapId = formationMapId;
            mission.formationLordId = formationLordId;
            mission.everAttached = false;
            mission.delivered = false;
            mission.cancelled = false;
            mission.stranded = false;

            int baselineTotal = 0;
            mission.baselineSilverByPawn = new Dictionary<int, int>();
            if (baselineSilverByPawn != null)
            {
                foreach (KeyValuePair<int, int> kv in baselineSilverByPawn)
                {
                    mission.baselineSilverByPawn[kv.Key] = kv.Value;
                    baselineTotal += kv.Value;
                }
            }
            mission.baselinePersonalSilverTotal = baselineTotal;

            missions.Add(mission);
            RebuildMissionSilverCache();
            return mission;
        }

        public OutpostCaravanMission GetMissionById(string id)
        {
            if (id.NullOrEmpty() || missions == null) return null;
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i] != null && missions[i].missionId == id) return missions[i];
            }
            return null;
        }

        public OutpostCaravanMission GetMissionForCaravan(Caravan c)
        {
            if (c == null || missions == null) return null;
            for (int i = 0; i < missions.Count; i++)
            {
                OutpostCaravanMission m = missions[i];
                if (m != null && m.caravan == c &&
                    (m.state == OutpostMissionState.Outbound ||
                     m.state == OutpostMissionState.Returning ||
                     m.state == OutpostMissionState.RetryDelivery))
                {
                    return m;
                }
            }
            return null;
        }

        public void NotifyDelivered(Caravan c)
        {
            if (c == null || missions == null) return;
            for (int i = 0; i < missions.Count; i++)
            {
                OutpostCaravanMission m = missions[i];
                if (m != null && m.caravan == c)
                {
                    m.delivered = true;
                    if (m.state == OutpostMissionState.Outbound || m.state == OutpostMissionState.RetryDelivery)
                    {
                        m.state = (c.Destroyed || c.PawnsListForReading.Count == 0)
                            ? OutpostMissionState.Completed
                            : OutpostMissionState.Returning;
                    }
                    break;
                }
            }
        }

        public void CancelMission(Caravan c)
        {
            TryCancelAndReturnHome(c);
        }

        /// <summary>
        /// Cancels an Outbound, RetryDelivery, or stranded Returning mission and routes the caravan home.
        /// Confirms cancellation ONLY after TryStartReturnPath succeeds.
        /// </summary>
        public bool TryCancelAndReturnHome(Caravan c)
        {
            if (c == null || c.Destroyed || !c.Spawned) return false;
            OutpostCaravanMission m = GetMissionForCaravan(c);
            Map prefHome = m != null ? m.originMap : null;
            if (prefHome == null && c.pather != null)
            {
                CaravanArrivalAction_OutpostDelivery act = c.pather.ArrivalAction as CaravanArrivalAction_OutpostDelivery;
                if (act != null) prefHome = act.OriginMap;
            }

            if (!TryStartReturnPath(c, prefHome))
            {
                if (m != null)
                {
                    m.stranded = true;
                    if (m.state == OutpostMissionState.Outbound)
                    {
                        m.state = OutpostMissionState.Returning;
                    }
                }
                Messages.Message("TMM_OutpostCaravan_HomeUnreachable".Translate(c.Name), c, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (m != null)
            {
                if (m.state == OutpostMissionState.Outbound || m.state == OutpostMissionState.RetryDelivery)
                {
                    m.cancelled = true;
                }
                m.state = OutpostMissionState.Returning;
                m.stranded = false;
            }
            Messages.Message("TMM_OutpostCaravan_CancelledReturning".Translate(c.Name), c, MessageTypeDefOf.NeutralEvent, false);
            return true;
        }

        /// <summary>
        /// Invoked by the explicit "Retry Delivery" gizmo when mission.state == RetryDelivery.
        /// If already on the outpost tile, executes delivery directly; otherwise restarts path to outpost.
        /// </summary>
        public bool TryRetryDelivery(Caravan car)
        {
            if (car == null || car.Destroyed || !car.Spawned) return false;
            OutpostCaravanMission m = GetMissionForCaravan(car);
            if (m == null || m.state != OutpostMissionState.RetryDelivery || m.delivered || m.cancelled)
            {
                return false;
            }

            if (m.outpost == null || !m.outpost.Spawned || m.outpost.Faction != Faction.OfPlayer)
            {
                Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), car, MessageTypeDefOf.RejectInput, false);
                TrySendMissionCaravanHome(m, car, false);
                return false;
            }

            if (car.Tile == m.outpost.Tile)
            {
                ExecuteMissionArrival(m, car);
                return m.delivered;
            }

            if (car.pather == null) return false;

            CaravanArrivalAction_OutpostDelivery arrival = new CaravanArrivalAction_OutpostDelivery(
                m.missionId, m.outpost, m.stayPawns, m.loadedSilver, m.originMap);
            m.state = OutpostMissionState.Outbound;
            car.pather.StartPath(m.outpost.Tile, arrival, true, true);
            if (car.pather.Moving && car.pather.Destination == m.outpost.Tile && car.pather.ArrivalAction == arrival)
            {
                m.stranded = false;
                return true;
            }

            m.state = OutpostMissionState.RetryDelivery;
            Messages.Message("TMM_OutpostCaravan_PathUnreachableTip".Translate(), car, MessageTypeDefOf.RejectInput, false);
            return false;
        }

        // =====================================================================
        // UNIFIED DELIVERY EXECUTION (SILVER COMMIT SEPARATE FROM WORKER TRANSFERS)
        // =====================================================================

        internal void ExecuteMissionArrival(OutpostCaravanMission mission, Caravan caravan)
        {
            if (mission == null || caravan == null || caravan.Destroyed) return;
            if (mission.caravan != caravan) return;
            if (mission.delivered || mission.cancelled) return;
            if (mission.state != OutpostMissionState.Outbound && mission.state != OutpostMissionState.RetryDelivery) return;

            StrategicOutpost outpost = mission.outpost;

            // 1. Verify outpost still exists, is spawned, and belongs to the player
            if (outpost == null || !outpost.Spawned || outpost.Faction != Faction.OfPlayer)
            {
                Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
                TrySendMissionCaravanHome(mission, caravan, false);
                return;
            }

            // 2. Step A: Atomic transfer of ONLY tracked mission silver stacks (separate from worker transfers!)
            if (!mission.silverDelivered && mission.loadedSilver > 0)
            {
                string silverFail;
                bool silverOk = OutpostCaravanLogistics.TryTransferMissionSilverAtomically(
                    caravan,
                    outpost,
                    mission.loadedSilver,
                    mission.missionSilverIds,
                    out silverFail);

                if (!silverOk)
                {
                    // Vanilla has already cleared caravan.pather.ArrivalAction before calling Arrived.
                    // Transition explicitly to RetryDelivery so the caravan is NOT stranded in Outbound!
                    mission.state = OutpostMissionState.RetryDelivery;
                    mission.stranded = false;
                    Messages.Message("TMM_OutpostCaravan_StorageTransferFailedRetry".Translate(outpost.Label), caravan, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                // Silver payment is now CONFIRMED & IRREVERSIBLE — record independently of worker transfers!
                mission.silverDelivered = true;
                UnprotectMissionSilver(mission);
            }

            // 3. Step B: Preserve edible return food (with diet & spoilage verification) BEFORE moving staying workers
            Map home = ResolveHomeMap(mission.originMap);
            List<Pawn> stayingNow = CollectStayingPawns(caravan, mission);
            List<Pawn> expectedReturning = CollectReturningPawns(caravan, stayingNow);

            if (expectedReturning.Count > 0 && stayingNow.Count > 0)
            {
                float returnDays = 0f;
                if (home != null)
                {
                    float retCap = OutpostCaravanLogistics.ComputePawnsCapacity(expectedReturning);
                    float retMass = OutpostCaravanLogistics.ComputeBaseMass(expectedReturning, 0);
                    OutpostCaravanLogistics.CalculateLegDays(outpost.Tile, home.Tile, expectedReturning, retMass, retCap, out returnDays);
                }
                OutpostCaravanLogistics.EnsureCarriersHaveReturnFood(
                    caravan, outpost, stayingNow, expectedReturning, outpost.Tile, Mathf.Max(0.5f, returnDays));
            }

            // 4. Step C: Transfer staying workers individually (isolated try/catch; failures NEVER roll back silver!)
            for (int i = stayingNow.Count - 1; i >= 0; i--)
            {
                Pawn p = stayingNow[i];
                if (p == null || p.Dead || !caravan.ContainsPawn(p)) continue;
                if (mission.transferredWorkerIds.Contains(p.thingIDNumber)) continue;

                if (caravan.PawnsListForReading.Count > 1 && !OutpostCaravanLogistics.HasMobileCarrierExcluding(caravan, p))
                {
                    string noCarrierMsg = "StrategicOutpost_NoMobileCarrierLeft".Translate();
                    Messages.Message("TMM_OutpostCaravan_WorkerRejected".Translate(p.LabelShort, noCarrierMsg), outpost, MessageTypeDefOf.CautionInput, false);
                    continue;
                }

                try
                {
                    string failReason;
                    if (outpost.AddPawnFromCaravan(caravan, p, out failReason, true))
                    {
                        mission.transferredWorkerIds.Add(p.thingIDNumber);
                    }
                    else
                    {
                        Messages.Message("TMM_OutpostCaravan_WorkerRejected".Translate(p.LabelShort, failReason ?? ""), outpost, MessageTypeDefOf.CautionInput, false);
                    }
                }
                catch (Exception ex)
                {
                    TestMode.ReportException("[TMM] OutpostCaravanManager: exception transferring worker " + p.LabelShort + ": ", ex);
                    Messages.Message("TMM_OutpostCaravan_WorkerRejected".Translate(p.LabelShort, ex.Message), outpost, MessageTypeDefOf.CautionInput, false);
                }
            }

            // 5. Mark mission delivery complete
            mission.delivered = true;

            // 6. Re-evaluate edible food (diets + spoilage) for the ACTUAL remaining caravan composition BEFORE sending home!
            bool hasCarriersLeft = !caravan.Destroyed && caravan.PawnsListForReading != null && caravan.PawnsListForReading.Count > 0;
            if (hasCarriersLeft)
            {
                List<Pawn> actualReturning = new List<Pawn>(caravan.PawnsListForReading);
                Map returnDest = FindBestValidHomeMap(caravan, mission.originMap);
                if (returnDest != null)
                {
                    float actualRetDays = 0f;
                    float actualCap = OutpostCaravanLogistics.ComputePawnsCapacity(actualReturning);
                    float actualMass = OutpostCaravanLogistics.ComputeBaseMass(actualReturning, 0);
                    if (OutpostCaravanLogistics.CalculateLegDays(outpost.Tile, returnDest.Tile, actualReturning, actualMass, actualCap, out actualRetDays) &&
                        actualRetDays > 0.05f)
                    {
                        float actualFoodDays = OutpostCaravanLogistics.CalcPawnsActualFoodDays(actualReturning, outpost.Tile);
                        if (actualFoodDays < actualRetDays)
                        {
                            Messages.Message(
                                "TMM_OutpostCaravan_LowReturnFoodWarning".Translate(caravan.Name, actualFoodDays.ToString("F1"), actualRetDays.ToString("F1")),
                                caravan,
                                MessageTypeDefOf.CautionInput,
                                false);
                        }
                    }
                }

                bool started = TrySendMissionCaravanHome(mission, caravan, true);
                if (started)
                {
                    Map chosenHome = FindMapByTile(caravan.pather != null ? caravan.pather.Destination : PlanetTile.Invalid) ?? returnDest;
                    if (chosenHome != null && chosenHome.Parent != null)
                    {
                        Messages.Message("TMM_OutpostCaravan_DeliveredReturning".Translate(outpost.Label, chosenHome.Parent.Label), caravan, MessageTypeDefOf.PositiveEvent, false);
                    }
                }
            }
            else
            {
                mission.state = OutpostMissionState.Completed;
                UnprotectMissionSilver(mission);
            }
        }

        private static List<Pawn> CollectStayingPawns(Caravan caravan, OutpostCaravanMission mission)
        {
            List<Pawn> list = new List<Pawn>();
            if (caravan == null || mission == null || mission.stayPawns == null) return list;
            for (int i = 0; i < mission.stayPawns.Count; i++)
            {
                Pawn p = mission.stayPawns[i];
                if (p != null && !p.Dead && caravan.ContainsPawn(p) && !list.Contains(p))
                {
                    list.Add(p);
                }
            }
            return list;
        }

        private static List<Pawn> CollectReturningPawns(Caravan caravan, List<Pawn> staying)
        {
            List<Pawn> list = new List<Pawn>();
            if (caravan == null || caravan.PawnsListForReading == null) return list;
            List<Pawn> all = caravan.PawnsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Pawn p = all[i];
                if (p != null && !p.Dead && !staying.Contains(p))
                {
                    list.Add(p);
                }
            }
            return list;
        }

        // =====================================================================
        // FORMATION EXIT ATTACHMENT (1-TO-1 LORD BINDING + EXACT SILVER ID VERIFICATION)
        // =====================================================================

        public bool AttachFormedCaravan(Caravan car, int formationMapId, int formationLordId)
        {
            if (car == null || car.Destroyed || !car.Spawned || car.Faction != Faction.OfPlayer) return false;
            if (formationMapId < 0 || formationLordId < 0 || missions == null) return false;

            for (int i = 0; i < missions.Count; i++)
            {
                OutpostCaravanMission m = missions[i];
                if (m == null) continue;
                if (m.everAttached || m.caravan != null || m.state != OutpostMissionState.Forming) continue;
                if (m.formationMapId != formationMapId || m.formationLordId != formationLordId) continue;

                m.everAttached = true;
                m.caravan = car;

                // Filter missionSilverIds strictly to stacks ACTUALLY loaded inside this caravan's pawns!
                // Unprotect any silver stacks that were left behind on the map so they can merge normally.
                List<string> confirmedIds = new List<string>();
                int actualLoaded = OutpostCaravanLogistics.CollectConfirmedMissionSilverInCaravan(car, m.missionSilverIds, confirmedIds);

                for (int s = 0; s < m.missionSilverIds.Count; s++)
                {
                    string oldId = m.missionSilverIds[s];
                    if (!oldId.NullOrEmpty() && !confirmedIds.Contains(oldId))
                    {
                        ActiveMissionSilverToMissionId.Remove(oldId);
                    }
                }
                m.missionSilverIds = confirmedIds;
                m.loadedSilver = actualLoaded;

                // Verify outpost still exists at exact moment of exit
                if (m.outpost == null || !m.outpost.Spawned || m.outpost.Faction != Faction.OfPlayer)
                {
                    Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), car, MessageTypeDefOf.CautionInput, false);
                    TrySendMissionCaravanHome(m, car, false);
                    return true;
                }

                // Verify exact tracked mission silver was loaded (personal silver NEVER satisfies mission quota!)
                if (m.silverAmount > 0 && actualLoaded < m.silverAmount)
                {
                    m.cancelled = true;
                    Messages.Message("TMM_OutpostCaravan_MissingSilverAborted".Translate(actualLoaded, m.silverAmount), car, MessageTypeDefOf.RejectInput, false);
                    TrySendMissionCaravanHome(m, car, false);
                    return true;
                }

                if (m.silverAmount == 0)
                {
                    m.silverDelivered = true;
                }

                CaravanArrivalAction_OutpostDelivery arrival = new CaravanArrivalAction_OutpostDelivery(
                    m.missionId, m.outpost, m.stayPawns, m.loadedSilver, m.originMap);

                if (car.pather == null)
                {
                    m.stranded = true;
                    m.state = OutpostMissionState.Returning;
                    return true;
                }

                m.state = OutpostMissionState.Outbound;
                car.pather.StartPath(m.outpost.Tile, arrival, true, true);
                if (car.pather.Destination == m.outpost.Tile && car.pather.ArrivalAction == arrival)
                {
                    m.stranded = false;
                }
                else
                {
                    m.state = OutpostMissionState.Returning;
                    Messages.Message("TMM_OutpostCaravan_PathUnreachableTip".Translate(), car, MessageTypeDefOf.RejectInput, false);
                    TrySendMissionCaravanHome(m, car, false);
                }
                return true;
            }

            return false;
        }

        public void TryAttachMissionToCaravan(Caravan car)
        {
            // Intentionally no-op: attachment requires verified (formationMapId, formationLordId) via AttachFormedCaravan.
        }

        public bool TrySendMissionCaravanHome(OutpostCaravanMission m, Caravan car, bool isPostDelivery)
        {
            if (m == null || car == null || car.Destroyed || !car.Spawned) return false;
            m.state = OutpostMissionState.Returning;
            if (!isPostDelivery) m.cancelled = true;

            if (TryStartReturnPath(car, m.originMap))
            {
                m.stranded = false;
                return true;
            }

            m.stranded = true;
            Messages.Message("TMM_OutpostCaravan_HomeUnreachable".Translate(car.Name), car, MessageTypeDefOf.CautionInput, false);
            return false;
        }

        // =====================================================================
        // HOME MAP RESOLUTION (StillValid CHECK + ALTERNATIVE COLONY FALLBACK)
        // =====================================================================

        public static Map ResolveHomeMap(Map preferred)
        {
            List<Map> candidates = CollectCandidateHomeMaps(preferred);
            return candidates.Count > 0 ? candidates[0] : null;
        }

        private static List<Map> CollectCandidateHomeMaps(Map preferred)
        {
            List<Map> list = new List<Map>();
            if (Find.Maps == null) return list;

            if (preferred != null && Find.Maps.Contains(preferred) && preferred.IsPlayerHome &&
                preferred.Parent != null && !preferred.Parent.Destroyed)
            {
                list.Add(preferred);
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m != null && m != preferred && m.IsPlayerHome && m.Parent != null && !m.Parent.Destroyed)
                {
                    list.Add(m);
                }
            }
            return list;
        }

        private static Map FindBestValidHomeMap(Caravan car, Map preferred)
        {
            if (car == null) return ResolveHomeMap(preferred);
            List<Map> candidates = CollectCandidateHomeMaps(preferred);
            for (int i = 0; i < candidates.Count; i++)
            {
                Map home = candidates[i];
                CaravanArrivalAction_Enter enter = new CaravanArrivalAction_Enter(home.Parent);
                if (enter.StillValid(car, home.Tile))
                {
                    return home;
                }
            }
            return candidates.Count > 0 ? candidates[0] : null;
        }

        /// <summary>
        /// Checks StillValid before direct Enter.Arrived on the home tile AND before StartPath,
        /// automatically falling back across all player home colonies if preferred home refuses.
        /// If every colony refuses or is unreachable, leaves the caravan stopped and returns false.
        /// </summary>
        public static bool TryStartReturnPath(Caravan car, Map preferredHome)
        {
            if (car == null || car.Destroyed || !car.Spawned || car.pather == null) return false;

            List<Map> candidates = CollectCandidateHomeMaps(preferredHome);
            for (int i = 0; i < candidates.Count; i++)
            {
                Map home = candidates[i];
                if (home == null || home.Parent == null || home.Parent.Destroyed) continue;

                CaravanArrivalAction_Enter enter = new CaravanArrivalAction_Enter(home.Parent);
                FloatMenuAcceptanceReport validReport = enter.StillValid(car, home.Tile); // CHECK API
                if (!validReport)
                {
                    continue;
                }

                if (car.Tile == home.Tile)
                {
                    // StopDead before direct entry so stale arrival actions cannot linger
                    car.pather.StopDead();
                    enter.Arrived(car);
                    if (car.Destroyed || !car.Spawned)
                    {
                        return true;
                    }
                    // If Arrived did not consume the caravan, try next colony candidate
                    continue;
                }

                car.pather.StartPath(home.Tile, enter, true, true); // CHECK API
                if (car.pather.Moving && car.pather.Destination == home.Tile && car.pather.ArrivalAction is CaravanArrivalAction_Enter)
                {
                    return true;
                }
            }

            car.pather.StopDead();
            return false;
        }

        // =====================================================================
        // LEGACY SAVE MIGRATION & PERIODIC AUDIT
        // =====================================================================

        /// <summary>
        /// Migrates pre-Round-2 saves by inspecting each caravan's real ArrivalAction.
        /// Any legacy mission/action with incomplete state (e.g. missing tracked missionSilverIds)
        /// returns the caravan home cleanly without deducting any silver.
        /// </summary>
        public void MigrateLegacyMissionsNow()
        {
            legacyMigrationPending = false;
            if (missions == null) missions = new List<OutpostCaravanMission>();

            // 1. Migrate legacy OutpostCaravanMission entries (schemaVersion < CurrentSchemaVersion)
            for (int i = missions.Count - 1; i >= 0; i--)
            {
                OutpostCaravanMission m = missions[i];
                if (m == null)
                {
                    missions.RemoveAt(i);
                    continue;
                }
                if (m.schemaVersion >= OutpostCaravanMission.CurrentSchemaVersion) continue;

                m.schemaVersion = OutpostCaravanMission.CurrentSchemaVersion;

                if (m.caravan == null || m.caravan.Destroyed || !m.caravan.Spawned)
                {
                    if (m.formationLordId < 0 || m.everAttached)
                    {
                        m.state = OutpostMissionState.Failed;
                        UnprotectMissionSilver(m);
                        missions.RemoveAt(i);
                    }
                    continue;
                }

                m.everAttached = true;
                Caravan car = m.caravan;
                CaravanArrivalAction realAct = car.pather != null ? car.pather.ArrivalAction : null;

                if (realAct is CaravanArrivalAction_Enter)
                {
                    m.state = OutpostMissionState.Returning;
                    continue;
                }

                CaravanArrivalAction_OutpostDelivery delivAct = realAct as CaravanArrivalAction_OutpostDelivery;
                bool outpostValid = m.outpost != null && m.outpost.Spawned && m.outpost.Faction == Faction.OfPlayer;
                bool silverComplete = (m.silverAmount <= 0) || m.silverDelivered ||
                                      (m.missionSilverIds != null && m.missionSilverIds.Count > 0);

                if (delivAct != null && delivAct.Outpost == m.outpost && outpostValid && silverComplete)
                {
                    delivAct.BindMigratedMissionId(m.missionId);
                    m.state = OutpostMissionState.Outbound;
                    if (m.silverAmount <= 0) m.silverDelivered = true;
                    continue;
                }

                // Incomplete legacy state -> mark old action handled and return caravan home WITHOUT deducting anything
                if (delivAct != null) delivAct.MarkHandled();
                UnprotectMissionSilver(m);
                Messages.Message("TMM_OutpostCaravan_LegacyReturningHome".Translate(car.Name), car, MessageTypeDefOf.NeutralEvent, false);
                TrySendMissionCaravanHome(m, car, false);
            }

            // 2. Scan all world player caravans for orphaned legacy CaravanArrivalAction_OutpostDelivery
            if (Find.WorldObjects != null && Find.WorldObjects.Caravans != null)
            {
                List<Caravan> caravans = Find.WorldObjects.Caravans;
                for (int i = 0; i < caravans.Count; i++)
                {
                    Caravan car = caravans[i];
                    if (car == null || car.Destroyed || !car.Spawned || car.Faction != Faction.OfPlayer || car.pather == null) continue;

                    CaravanArrivalAction_OutpostDelivery act = car.pather.ArrivalAction as CaravanArrivalAction_OutpostDelivery;
                    if (act == null) continue;

                    OutpostCaravanMission matched = !act.MissionId.NullOrEmpty() ? GetMissionById(act.MissionId) : null;
                    if (matched != null && matched.caravan == car && matched.outpost == act.Outpost &&
                        (matched.state == OutpostMissionState.Outbound || matched.state == OutpostMissionState.RetryDelivery))
                    {
                        continue;
                    }

                    // Orphaned legacy delivery action -> never deduct; return home cleanly
                    act.MarkHandled();
                    Messages.Message("TMM_OutpostCaravan_LegacyReturningHome".Translate(car.Name), car, MessageTypeDefOf.NeutralEvent, false);
                    TryStartReturnPath(car, act.OriginMap);
                }
            }

            RebuildMissionSilverCache();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (legacyMigrationPending)
            {
                MigrateLegacyMissionsNow();
            }
            if (Find.TickManager.TicksGame % AuditIntervalTicks != 0) return;
            AuditAndCleanupMissions(false);
        }

        public void AuditAndCleanupMissions(bool isScribeBoundary)
        {
            if (missions == null)
            {
                missions = new List<OutpostCaravanMission>();
                RebuildMissionSilverCache();
                return;
            }

            if (!isScribeBoundary && legacyMigrationPending)
            {
                MigrateLegacyMissionsNow();
            }

            bool cacheDirty = false;

            for (int i = missions.Count - 1; i >= 0; i--)
            {
                OutpostCaravanMission m = missions[i];
                if (m == null)
                {
                    missions.RemoveAt(i);
                    cacheDirty = true;
                    continue;
                }

                if (m.pawns != null) m.pawns.RemoveAll(p => p == null);
                if (m.stayPawns != null) m.stayPawns.RemoveAll(p => p == null);

                if (m.state == OutpostMissionState.Completed || m.state == OutpostMissionState.Failed)
                {
                    UnprotectMissionSilver(m);
                    missions.RemoveAt(i);
                    cacheDirty = true;
                    continue;
                }

                // 1. Forming state
                if (m.state == OutpostMissionState.Forming)
                {
                    if (m.everAttached)
                    {
                        m.state = OutpostMissionState.Failed;
                        UnprotectMissionSilver(m);
                        missions.RemoveAt(i);
                        cacheDirty = true;
                        continue;
                    }

                    Map formMap = FindMapById(m.formationMapId);
                    Lord formLord = FindFormationLord(formMap, m.formationLordId);
                    if (formMap == null || formLord == null || m.pawns == null || m.pawns.Count == 0)
                    {
                        m.state = OutpostMissionState.Failed;
                        UnprotectMissionSilver(m);
                        missions.RemoveAt(i);
                        cacheDirty = true;
                        continue;
                    }

                    if (!isScribeBoundary && (m.outpost == null || !m.outpost.Spawned || m.outpost.Faction != Faction.OfPlayer))
                    {
                        formMap.lordManager.RemoveLord(formLord);
                        Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), MessageTypeDefOf.CautionInput, false);
                        m.state = OutpostMissionState.Cancelled;
                        m.cancelled = true;
                        UnprotectMissionSilver(m);
                        missions.RemoveAt(i);
                        cacheDirty = true;
                        continue;
                    }
                    continue;
                }

                // 2. Outbound / RetryDelivery / Returning states: require live attached caravan
                if (m.caravan == null || m.caravan.Destroyed || !m.caravan.Spawned)
                {
                    m.state = (m.state == OutpostMissionState.Returning) ? OutpostMissionState.Completed : OutpostMissionState.Failed;
                    UnprotectMissionSilver(m);
                    missions.RemoveAt(i);
                    cacheDirty = true;
                    continue;
                }

                // 3. Outbound or RetryDelivery: if outpost vanished, turn caravan around with its cargo!
                if (m.state == OutpostMissionState.Outbound || m.state == OutpostMissionState.RetryDelivery)
                {
                    if (m.outpost == null || !m.outpost.Spawned || m.outpost.Faction != Faction.OfPlayer)
                    {
                        if (!isScribeBoundary)
                        {
                            Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), m.caravan, MessageTypeDefOf.CautionInput, false);
                            TrySendMissionCaravanHome(m, m.caravan, false);
                        }
                        continue;
                    }
                }

                // 4. Returning state: mark stranded if pather stopped moving
                if (m.state == OutpostMissionState.Returning && !isScribeBoundary)
                {
                    if (m.caravan.pather != null && !m.caravan.pather.Moving)
                    {
                        m.stranded = true;
                    }
                }
            }

            if (cacheDirty)
            {
                RebuildMissionSilverCache();
            }
        }

        private static Map FindMapById(int mapId)
        {
            if (mapId < 0 || Find.Maps == null) return null;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                if (Find.Maps[i] != null && Find.Maps[i].uniqueID == mapId) return Find.Maps[i];
            }
            return null;
        }

        private static Map FindMapByTile(PlanetTile tile)
        {
            if (!tile.Valid || Find.Maps == null) return null;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                if (Find.Maps[i] != null && Find.Maps[i].Tile == tile) return Find.Maps[i];
            }
            return null;
        }

        private static Lord FindFormationLord(Map map, int lordId)
        {
            if (map == null || lordId < 0 || map.lordManager == null) return null;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord l = lords[i];
                if (l != null && l.loadID == lordId && l.LordJob is LordJob_FormAndSendCaravan)
                {
                    return l;
                }
            }
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                AuditAndCleanupMissions(true);
            }
            Scribe_Collections.Look(ref missions, "missions", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (missions == null) missions = new List<OutpostCaravanMission>();
                legacyMigrationPending = true;
                RebuildMissionSilverCache();
            }
        }
    }

    /// <summary>
    /// Static logistics calculations, exact mission-silver stack tagging &amp; journaled transfer,
    /// diet- and spoilage-aware return food redistribution, and TransferableOneWay food checks.
    /// All test hooks are strictly gated behind TestMode.Enabled.
    /// </summary>
    public static class OutpostCaravanLogistics
    {
        /// <summary>
        /// Test-only hook (ONLY active when TestMode.Enabled) that rejects all RawMaterials transfers.
        /// </summary>
        public static bool TestRejectRawMaterialsTransfer;

        /// <summary>
        /// Test-only hook (ONLY active when TestMode.Enabled) that rejects RawMaterials transfer on a
        /// specific 0-based chunk index (e.g. 1 = allow first stack into RawMaterials, reject second stack).
        /// Default -1 (inactive).
        /// </summary>
        public static int TestRejectRawMaterialsTransferOnStep = -1;

        /// <summary>
        /// Test-only hook (ONLY active when TestMode.Enabled) to simulate a pawn inventory refusing
        /// TryAdd during rollback or food redistribution.
        /// </summary>
        public static Func<ThingOwner, Thing, bool> TestRejectInventoryTryAdd;

        private sealed class CaravanStagingHolder : IThingHolder
        {
            public ThingOwner<Thing> innerContainer;

            public CaravanStagingHolder()
            {
                innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
            }

            public IThingHolder ParentHolder
            {
                get { return null; }
            }

            public void GetChildHolders(List<IThingHolder> outChildren)
            {
            }

            public ThingOwner GetDirectlyHeldThings()
            {
                return innerContainer;
            }
        }

        public static List<Thing> GetFormableColonyItems(Map map)
        {
            if (map == null) return new List<Thing>();
            List<Thing> items = CaravanFormingUtility.AllReachableColonyItems(map, false, false, false);
            return items != null ? items : new List<Thing>();
        }

        public static int CountFormableSilver(Map map)
        {
            List<Thing> items = GetFormableColonyItems(map);
            int total = 0;
            for (int i = 0; i < items.Count; i++)
            {
                Thing t = items[i];
                if (t != null && !t.Destroyed && t.def == ThingDefOf.Silver && t.stackCount > 0 &&
                    !t.IsForbidden(Faction.OfPlayer) && !OutpostCaravanManager.IsMissionSilverThing(t))
                {
                    total += t.stackCount;
                }
            }
            return total;
        }

        public static int CountCaravanSilver(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed) return 0;
            int total = 0;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                total += CountPawnInventoryThing(pawns[i], ThingDefOf.Silver);
            }
            return total;
        }

        public static int CollectConfirmedMissionSilverInCaravan(Caravan caravan, List<string> expectedSilverIds, List<string> outConfirmedIds)
        {
            if (outConfirmedIds != null) outConfirmedIds.Clear();
            if (caravan == null || caravan.Destroyed || expectedSilverIds == null || expectedSilverIds.Count == 0) return 0;

            HashSet<string> expectedSet = new HashSet<string>(expectedSilverIds);
            int total = 0;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed && t.def == ThingDefOf.Silver && t.stackCount > 0 && expectedSet.Contains(t.ThingID))
                    {
                        total += t.stackCount;
                        if (outConfirmedIds != null && !outConfirmedIds.Contains(t.ThingID))
                        {
                            outConfirmedIds.Add(t.ThingID);
                        }
                    }
                }
            }
            return total;
        }

        public static int CountPawnInventoryThing(Pawn p, ThingDef def)
        {
            if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null || def == null) return 0;
            int total = 0;
            ThingOwner<Thing> inv = p.inventory.innerContainer;
            for (int i = 0; i < inv.Count; i++)
            {
                Thing t = inv[i];
                if (t != null && !t.Destroyed && t.def == def && t.stackCount > 0)
                {
                    total += t.stackCount;
                }
            }
            return total;
        }

        public static bool IsCapableCaravanCarrier(Pawn p)
        {
            if (p == null || p.Dead || p.Destroyed || p.Downed) return false;
            if (p.Faction != Faction.OfPlayer || !p.IsColonist || p.IsPrisoner || p.IsSlave) return false;
            if (p.health != null && p.health.capacities != null && !p.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
            {
                return false;
            }
            return true;
        }

        public static bool HasMobileCarrierExcluding(Caravan caravan, Pawn excluded)
        {
            if (caravan == null || caravan.PawnsListForReading == null) return false;
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && p != excluded && IsCapableCaravanCarrier(p)) return true;
            }
            return false;
        }

        // =====================================================================
        // JOURNALED ATOMIC MISSION SILVER TRANSFER (TRACKED STACK IDS ONLY)
        // =====================================================================

        private sealed class SilverJournalEntry
        {
            public Pawn origPawn;
            public Thing origStack;
            public int origStackCount;
            public int requestedTake;
            public bool tookWhole;
            public Thing chunk;
            public bool inStaging;
            public bool inOutpost;
        }

        /// <summary>
        /// Kept for signature compatibility; refuses untagged transfers without confirmed missionSilverIds.
        /// </summary>
        public static bool TryTransferMissionSilverAtomically(
            Caravan caravan,
            StrategicOutpost outpost,
            int targetSilver,
            Dictionary<int, int> personalBaselineByPawn,
            out string failReason)
        {
            failReason = "TMM_OutpostCaravan_InvalidMissionReturning".Translate(caravan != null ? caravan.Name : "");
            return false;
        }

        /// <summary>
        /// Journaled, atomic transfer of ONLY tracked mission silver stacks (identified by exact ThingID
        /// in missionSilverIds) from caravan into outpost.RawMaterials.
        /// - Personal silver stacks are NEVER eligible.
        /// - Every mutation (SplitOff, Remove, TryAddOrTransfer) is journaled BEFORE executing.
        /// - Staged chunks are verified in full BEFORE moving any chunk into RawMaterials.
        /// - Chunks enter RawMaterials with canMergeWithExistingStacks=false so failure on stack N
        ///   rolls back stacks 0..N-1 cleanly.
        /// - Every rollback TryAdd is verified, falling back across caravan pawns -> home map so no
        ///   silver is ever left ownerless.
        /// </summary>
        public static bool TryTransferMissionSilverAtomically(
            Caravan caravan,
            StrategicOutpost outpost,
            int targetSilver,
            List<string> missionSilverIds,
            out string failReason)
        {
            failReason = null;
            if (targetSilver <= 0) return true;

            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer ||
                outpost == null || !outpost.Spawned || outpost.Faction != Faction.OfPlayer || outpost.RawMaterials == null)
            {
                failReason = "TMM_OutpostCaravan_OutpostGone".Translate();
                return false;
            }

            if (missionSilverIds == null || missionSilverIds.Count == 0)
            {
                failReason = "StrategicOutpost_NotEnoughSilverToSend".Translate(targetSilver);
                return false;
            }

            // Step 1: Collect ONLY confirmed mission silver stacks (never personal silver!)
            HashSet<string> allowedIds = new HashSet<string>(missionSilverIds);
            List<Pawn> owners = new List<Pawn>();
            List<Thing> stacks = new List<Thing>();
            int totalEligible = 0;

            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed && t.def == ThingDefOf.Silver && t.stackCount > 0 && allowedIds.Contains(t.ThingID))
                    {
                        owners.Add(p);
                        stacks.Add(t);
                        totalEligible += t.stackCount;
                    }
                }
            }

            if (totalEligible < targetSilver)
            {
                failReason = "StrategicOutpost_NotEnoughSilverToSend".Translate(targetSilver);
                return false;
            }

            // Step 2: Journal & stage all mission silver chunks into StagingHolder BEFORE touching outpost.RawMaterials
            CaravanStagingHolder staging = new CaravanStagingHolder();
            List<SilverJournalEntry> journal = new List<SilverJournalEntry>();
            int remaining = targetSilver;
            bool ok = true;

            try
            {
                for (int i = 0; i < stacks.Count && remaining > 0; i++)
                {
                    Pawn p = owners[i];
                    Thing src = stacks[i];
                    if (p == null || p.Dead || p.inventory == null || p.inventory.innerContainer == null ||
                        src == null || src.Destroyed || src.stackCount <= 0 || !p.inventory.innerContainer.Contains(src))
                    {
                        ok = false;
                        break;
                    }

                    int take = Mathf.Min(remaining, src.stackCount);
                    SilverJournalEntry entry = new SilverJournalEntry();
                    entry.origPawn = p;
                    entry.origStack = src;
                    entry.origStackCount = src.stackCount;
                    entry.requestedTake = take;
                    entry.tookWhole = (take == src.stackCount);
                    // Journal BEFORE SplitOff!
                    journal.Add(entry);

                    Thing split = src.SplitOff(take);
                    entry.chunk = split;

                    if (split == null || split.Destroyed || split.stackCount != take)
                    {
                        ok = false;
                        break;
                    }

                    if (split.holdingOwner != null && split.holdingOwner != staging.innerContainer)
                    {
                        split.holdingOwner.Remove(split);
                    }

                    if (!staging.innerContainer.TryAdd(split, false) || !staging.innerContainer.Contains(split))
                    {
                        ok = false;
                        break;
                    }

                    entry.inStaging = true;
                    remaining -= split.stackCount;
                }

                // Verify complete staged amount before touching outpost.RawMaterials
                int stagedSum = 0;
                for (int i = 0; i < staging.innerContainer.Count; i++)
                {
                    Thing st = staging.innerContainer[i];
                    if (st != null && !st.Destroyed && st.def == ThingDefOf.Silver) stagedSum += st.stackCount;
                }
                if (remaining > 0 || stagedSum != targetSilver)
                {
                    ok = false;
                }

                // Step 3: Move each staged chunk into outpost.RawMaterials (no merge until full batch succeeds)
                if (ok)
                {
                    for (int i = 0; i < journal.Count; i++)
                    {
                        SilverJournalEntry entry = journal[i];
                        Thing chunk = entry.chunk;
                        if (chunk == null || chunk.Destroyed || !staging.innerContainer.Contains(chunk))
                        {
                            ok = false;
                            break;
                        }

                        if (ShouldTestRejectRawMaterials(i))
                        {
                            ok = false;
                            break;
                        }

                        staging.innerContainer.Remove(chunk);
                        entry.inStaging = false;

                        bool added = outpost.RawMaterials.TryAddOrTransfer(chunk, false) && outpost.RawMaterials.Contains(chunk);
                        if (!added)
                        {
                            ok = false;
                            break;
                        }

                        entry.inOutpost = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] OutpostCaravanLogistics: silver transfer failed, rolling back journal: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                RollbackSilverJournal(staging, journal, outpost, caravan);
                failReason = "TMM_OutpostCaravan_StorageTransferFailed".Translate();
                return false;
            }

            return true;
        }

        private static bool ShouldTestRejectRawMaterials(int chunkIndex)
        {
            if (!TestMode.Enabled) return false;
            if (TestRejectRawMaterialsTransfer) return true;
            if (TestRejectRawMaterialsTransferOnStep >= 0 && chunkIndex == TestRejectRawMaterialsTransferOnStep) return true;
            return false;
        }

        private static void RollbackSilverJournal(CaravanStagingHolder staging, List<SilverJournalEntry> journal, StrategicOutpost outpost, Caravan caravan)
        {
            for (int i = journal.Count - 1; i >= 0; i--)
            {
                SilverJournalEntry entry = journal[i];
                if (entry == null) continue;
                try
                {
                    Thing chunk = entry.chunk;
                    if (chunk == null && entry.origStack != null && !entry.origStack.Destroyed &&
                        !entry.origStack.Spawned && entry.origStack.holdingOwner == null)
                    {
                        chunk = entry.origStack;
                    }
                    if (chunk == null || chunk.Destroyed) continue;

                    if (entry.inOutpost && outpost != null && outpost.RawMaterials != null && outpost.RawMaterials.Contains(chunk))
                    {
                        outpost.RawMaterials.Remove(chunk);
                        entry.inOutpost = false;
                    }
                    if (entry.inStaging && staging != null && staging.innerContainer.Contains(chunk))
                    {
                        staging.innerContainer.Remove(chunk);
                        entry.inStaging = false;
                    }

                    TryReturnThingToCaravanVerified(chunk, entry.tookWhole ? null : entry.origStack, entry.origPawn, caravan, null);
                }
                catch (Exception ex)
                {
                    TestMode.ReportException("[TMM] OutpostCaravanLogistics: error rolling back silver entry: ", ex);
                }
            }

            // Defensive sweep of any leftover staging items
            if (staging != null && staging.innerContainer.Count > 0)
            {
                List<Thing> leftovers = new List<Thing>();
                for (int i = 0; i < staging.innerContainer.Count; i++)
                {
                    if (staging.innerContainer[i] != null) leftovers.Add(staging.innerContainer[i]);
                }
                for (int i = 0; i < leftovers.Count; i++)
                {
                    Thing t = leftovers[i];
                    if (staging.innerContainer.Contains(t)) staging.innerContainer.Remove(t);
                    TryReturnThingToCaravanVerified(t, null, null, caravan, null);
                }
            }
        }

        private static bool TryAddChecked(ThingOwner owner, Thing item, bool canMerge)
        {
            if (owner == null || item == null || item.Destroyed) return false;
            if (TestMode.Enabled && TestRejectInventoryTryAdd != null && TestRejectInventoryTryAdd(owner, item))
            {
                return false;
            }
            bool added = owner.TryAdd(item, canMerge);
            if (!added) return false;
            if (canMerge)
            {
                return item.Destroyed || item.stackCount <= 0 || owner.Contains(item);
            }
            return owner.Contains(item);
        }

        /// <summary>
        /// Verified return helper for caravan items (silver or food):
        /// 1) Original stack absorb, 2) origPawn inventory, 3) any other living caravan pawn,
        /// 4) optional outpost RawMaterials (only for food if requested), 5) player home map drop spot.
        /// </summary>
        private static bool TryReturnThingToCaravanVerified(Thing chunk, Thing origStack, Pawn origPawn, Caravan caravan, StrategicOutpost fallbackOutpost)
        {
            if (chunk == null || chunk.Destroyed || chunk.stackCount <= 0) return true;

            if (chunk.holdingOwner != null)
            {
                chunk.holdingOwner.Remove(chunk);
            }

            if (origStack != null && origStack != chunk && !origStack.Destroyed &&
                origPawn != null && !origPawn.Dead && origPawn.inventory != null &&
                origPawn.inventory.innerContainer != null && origPawn.inventory.innerContainer.Contains(origStack))
            {
                if (origStack.TryAbsorbStack(chunk, false) && (chunk.Destroyed || chunk.stackCount <= 0))
                {
                    return true;
                }
            }

            if (origPawn != null && !origPawn.Dead && origPawn.inventory != null && origPawn.inventory.innerContainer != null)
            {
                if (TryAddChecked(origPawn.inventory.innerContainer, chunk, true))
                {
                    return true;
                }
            }

            if (caravan != null && !caravan.Destroyed && caravan.PawnsListForReading != null)
            {
                List<Pawn> members = caravan.PawnsListForReading;
                for (int i = 0; i < members.Count; i++)
                {
                    Pawn other = members[i];
                    if (other == null || other == origPawn || other.Dead || other.Destroyed) continue;
                    if (other.inventory != null && other.inventory.innerContainer != null &&
                        TryAddChecked(other.inventory.innerContainer, chunk, true))
                    {
                        return true;
                    }
                }
            }

            if (fallbackOutpost != null && fallbackOutpost.Spawned && fallbackOutpost.RawMaterials != null)
            {
                if (TryAddChecked(fallbackOutpost.RawMaterials, chunk, true))
                {
                    return true;
                }
            }

            Map home = OutpostCaravanManager.ResolveHomeMap(null);
            if (home != null)
            {
                IntVec3 spot = DropCellFinder.TradeDropSpot(home);
                if (!spot.IsValid || !spot.InBounds(home)) spot = home.Center;
                Thing placed;
                if (GenPlace.TryPlaceThing(chunk, spot, home, ThingPlaceMode.Near, out placed) &&
                    (chunk.Destroyed || (placed != null && placed.Spawned)))
                {
                    return true;
                }
            }

            return StoryPaymentLimbo.Hold(chunk);
        }

        // =====================================================================
        // DIET- & SPOILAGE-AWARE RETURN FOOD REDISTRIBUTION
        // =====================================================================

        /// <summary>
        /// Evaluates actual edible food days for the exact list of pawns using their real inventories,
        /// honoring diets/food restrictions and CompRottable spoilage at tile temperature.
        /// </summary>
        public static float CalcPawnsActualFoodDays(List<Pawn> pawns, PlanetTile tile)
        {
            if (pawns == null || pawns.Count == 0) return 0f;

            List<TransferableOneWay> list = new List<TransferableOneWay>();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead) continue;
                TransferableOneWay tp = new TransferableOneWay();
                tp.things.Add(p);
                tp.ForceTo(1);
                list.Add(tp);
            }

            if (list.Count == 0) return 0f;

            return DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(
                list, tile, IgnorePawnsInventoryMode.DontIgnore, Faction.OfPlayer); // CHECK API
        }

        public static void EnsureCarriersHaveReturnFood(Caravan caravan, List<Pawn> staying, List<Pawn> returning, PlanetTile tile, float returnDaysNeeded)
        {
            EnsureCarriersHaveReturnFood(caravan, null, staying, returning, tile, returnDaysNeeded);
        }

        public static void EnsureCarriersHaveReturnFood(
            Caravan caravan,
            StrategicOutpost outpost,
            List<Pawn> staying,
            List<Pawn> returning,
            PlanetTile tile,
            float returnDaysNeeded)
        {
            if (caravan == null || staying == null || returning == null || staying.Count == 0 || returning.Count == 0 || returnDaysNeeded <= 0f)
            {
                return;
            }

            // Evaluate actual edible food days with diets & spoilage via DaysWorthOfFoodCalculator
            float currentDays = CalcPawnsActualFoodDays(returning, tile);
            if (currentDays >= returnDaysNeeded) return;

            int carrierIdx = 0;
            for (int i = 0; i < staying.Count && currentDays < returnDaysNeeded; i++)
            {
                Pawn worker = staying[i];
                if (worker == null || worker.Dead || worker.inventory == null || worker.inventory.innerContainer == null) continue;

                List<Thing> foods = new List<Thing>();
                for (int j = 0; j < worker.inventory.innerContainer.Count; j++)
                {
                    Thing t = worker.inventory.innerContainer[j];
                    if (IsCaravanFood(t) && AnyPawnWillEat(returning, t))
                    {
                        foods.Add(t);
                    }
                }

                // Prefer non-perishable food first for the return leg
                foods.Sort(delegate(Thing a, Thing b)
                {
                    bool aNonPerish = a.def.GetCompProperties<CompProperties_Rottable>() == null;
                    bool bNonPerish = b.def.GetCompProperties<CompProperties_Rottable>() == null;
                    if (aNonPerish != bNonPerish) return bNonPerish.CompareTo(aNonPerish);
                    int aPref = a.def.ingestible != null ? (int)a.def.ingestible.preferability : 0;
                    int bPref = b.def.ingestible != null ? (int)b.def.ingestible.preferability : 0;
                    return bPref.CompareTo(aPref);
                });

                for (int j = 0; j < foods.Count && currentDays < returnDaysNeeded; j++)
                {
                    Thing food = foods[j];
                    if (food == null || food.Destroyed || !worker.inventory.innerContainer.Contains(food)) continue;

                    float nutrPerUnit = food.GetStatValue(StatDefOf.Nutrition);
                    if (nutrPerUnit <= 0.01f) continue;

                    float missingDays = Mathf.Max(0.1f, returnDaysNeeded - currentDays);
                    int neededUnits = Mathf.Max(1, Mathf.CeilToInt((ComputeDailyNutrition(returning) * missingDays * 1.15f) / nutrPerUnit));
                    int take = Mathf.Min(neededUnits, food.stackCount);
                    if (take <= 0) continue;

                    bool takingWhole = (take == food.stackCount);
                    Thing split = null;
                    bool movedToCarrier = false;

                    try
                    {
                        split = food.SplitOff(take);
                        if (split == null || split.Destroyed || split.stackCount <= 0)
                        {
                            continue;
                        }
                        if (split.holdingOwner != null)
                        {
                            split.holdingOwner.Remove(split);
                        }

                        // Try each returning carrier in round-robin order with verified TryAdd
                        for (int attempt = 0; attempt < returning.Count; attempt++)
                        {
                            Pawn targetCarrier = returning[(carrierIdx + attempt) % returning.Count];
                            if (targetCarrier == null || targetCarrier.Dead || targetCarrier.inventory == null || targetCarrier.inventory.innerContainer == null)
                            {
                                continue;
                            }
                            if (TryAddChecked(targetCarrier.inventory.innerContainer, split, true))
                            {
                                carrierIdx = (carrierIdx + attempt + 1) % returning.Count;
                                movedToCarrier = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("[TMM] OutpostCaravanLogistics: food transfer error, returning portion safely: " + ex.Message);
                        movedToCarrier = false;
                    }

                    if (!movedToCarrier && split != null && !split.Destroyed)
                    {
                        // Verified return: never leave split unowned!
                        TryReturnThingToCaravanVerified(split, takingWhole ? null : food, worker, caravan, outpost);
                    }
                    else if (movedToCarrier)
                    {
                        currentDays = CalcPawnsActualFoodDays(returning, tile);
                    }
                }
            }
        }

        // =====================================================================
        // BIDIRECTIONAL TRIP TIME & TransferableOneWay FOOD ASSESSMENT
        // =====================================================================

        public static float ComputePawnsCapacity(List<Pawn> pawns)
        {
            if (pawns == null) return 1f;
            float cap = 0f;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && !pawns[i].Dead) cap += MassUtility.Capacity(pawns[i]);
            }
            return Mathf.Max(1f, cap);
        }

        public static float ComputeBaseMass(List<Pawn> pawns, int silverAmount)
        {
            float mass = 0f;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i] != null && !pawns[i].Dead)
                    {
                        mass += MassUtility.GearAndInventoryMass(pawns[i]);
                    }
                }
            }
            if (silverAmount > 0 && ThingDefOf.Silver != null)
            {
                mass += silverAmount * ThingDefOf.Silver.BaseMass;
            }
            return mass;
        }

        public static bool CalculateLegDays(PlanetTile fromTile, PlanetTile toTile, List<Pawn> pawns, float massUsage, float massCapacity, out float days)
        {
            days = 0f;
            if (!fromTile.Valid || !toTile.Valid || pawns == null || pawns.Count == 0 || fromTile.Layer == null || fromTile.Layer.Pather == null)
            {
                return false;
            }
            if (fromTile == toTile)
            {
                days = 0f;
                return true;
            }

            WorldPath path = fromTile.Layer.Pather.FindPath(fromTile, toTile, null, null);
            try
            {
                if (path == null || !path.Found) return false;
                int ticksPerMove = CaravanTicksPerMoveUtility.GetTicksPerMove(pawns, massUsage, Mathf.Max(1f, massCapacity), false, null);
                int ticks = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(fromTile, toTile, path, 0f, ticksPerMove, GenTicks.TicksAbs);
                days = (float)ticks / 60000f;
                return true;
            }
            finally
            {
                if (path != null) path.ReleaseToPool();
            }
        }

        public static bool CalculateTripDays(PlanetTile fromTile, PlanetTile toTile, List<Pawn> pawns, out float daysOneWay, out float daysRoundTrip)
        {
            daysOneWay = 0f;
            daysRoundTrip = 0f;
            float cap = ComputePawnsCapacity(pawns);
            float mass = ComputeBaseMass(pawns, 0);
            if (!CalculateLegDays(fromTile, toTile, pawns, mass, cap, out daysOneWay)) return false;
            float back;
            if (!CalculateLegDays(toTile, fromTile, pawns, mass, cap, out back)) back = daysOneWay;
            daysRoundTrip = daysOneWay + back;
            return true;
        }

        public static bool CheckCaravanFeasibility(StrategicOutpost outpost, Map map, List<Pawn> pawns, int silverAmount,
            out float daysOneWay, out float daysRoundTrip, out float foodDays, out bool canSend, out string cantSendReason)
        {
            return CheckCaravanFeasibility(outpost, map, pawns, null, silverAmount,
                out daysOneWay, out daysRoundTrip, out foodDays, out canSend, out cantSendReason);
        }

        public static bool CheckCaravanFeasibility(StrategicOutpost outpost, Map map, List<Pawn> pawns, List<Pawn> stayPawns, int silverAmount,
            out float daysOneWay, out float daysRoundTrip, out float foodDays, out bool canSend, out string cantSendReason)
        {
            daysOneWay = 0f;
            daysRoundTrip = 0f;
            foodDays = 0f;
            canSend = false;
            cantSendReason = null;

            if (outpost == null || !outpost.Spawned || outpost.Faction != Faction.OfPlayer || map == null)
            {
                cantSendReason = "StrategicOutpost_InvalidOutpost".Translate();
                return false;
            }

            if (pawns == null || pawns.Count == 0)
            {
                cantSendReason = "TMM_OutpostCaravan_NoPawnsSelectedTip".Translate();
                return false;
            }

            List<Pawn> returningPawns = GetReturningPawns(pawns, stayPawns);

            if (returningPawns.Count > 0)
            {
                bool hasCarrier = false;
                for (int i = 0; i < returningPawns.Count; i++)
                {
                    if (IsCapableCaravanCarrier(returningPawns[i]))
                    {
                        hasCarrier = true;
                        break;
                    }
                }
                if (!hasCarrier)
                {
                    cantSendReason = "StrategicOutpost_NoMobileCarrierLeft".Translate();
                    return false;
                }
            }

            int availableSilver = CountFormableSilver(map);
            if (silverAmount > availableSilver)
            {
                cantSendReason = "StrategicOutpost_NotEnoughSilverToSend".Translate(silverAmount);
                return false;
            }

            float outCap = ComputePawnsCapacity(pawns);
            float outMass = ComputeBaseMass(pawns, silverAmount);
            if (outMass > outCap)
            {
                cantSendReason = "CaravanMassCapacityExceeded".Translate();
                return false;
            }

            if (!CalculateLegDays(map.Tile, outpost.Tile, pawns, outMass, outCap, out daysOneWay))
            {
                cantSendReason = "TMM_OutpostCaravan_PathUnreachableTip".Translate();
                return false;
            }

            float daysReturn = 0f;
            if (returningPawns.Count > 0)
            {
                float retCap = ComputePawnsCapacity(returningPawns);
                float retMass = ComputeBaseMass(returningPawns, 0);
                if (!CalculateLegDays(outpost.Tile, map.Tile, returningPawns, retMass, retCap, out daysReturn))
                {
                    cantSendReason = "TMM_OutpostCaravan_PathUnreachableTip".Translate();
                    return false;
                }
            }
            daysRoundTrip = daysOneWay + daysReturn;

            Dictionary<Thing, int> foodPlan;
            float outFoodDays;
            float retFoodDays;
            bool foodOk = CollectFoodForTwoLegs(map, outpost.Tile, pawns, returningPawns, daysOneWay, daysReturn, out foodPlan, out outFoodDays, out retFoodDays);
            foodDays = outFoodDays + retFoodDays;

            if (!foodOk)
            {
                cantSendReason = "TMM_OutpostCaravan_NotEnoughFoodTip".Translate(daysRoundTrip.ToString("F1"));
                return false;
            }

            float foodMass = 0f;
            foreach (KeyValuePair<Thing, int> kv in foodPlan)
            {
                foodMass += kv.Key.GetStatValue(StatDefOf.Mass) * kv.Value;
            }
            if (outMass + foodMass > outCap)
            {
                cantSendReason = "CaravanMassCapacityExceeded".Translate();
                return false;
            }

            canSend = true;
            return true;
        }

        public static bool CollectFoodForTrip(Map map, List<Pawn> pawns, float daysRoundTrip, out Dictionary<Thing, int> foodToTake, out float gatheredDays)
        {
            float half = daysRoundTrip * 0.5f;
            float outDays, retDays;
            bool ok = CollectFoodForTwoLegs(map, map != null ? map.Tile : PlanetTile.Invalid, pawns, pawns, half, half, out foodToTake, out outDays, out retDays);
            gatheredDays = outDays + retDays;
            return ok;
        }

        public static bool CollectFoodForTwoLegs(
            Map map,
            PlanetTile outpostTile,
            List<Pawn> allPawns,
            List<Pawn> returningPawns,
            float daysOutbound,
            float daysReturn,
            out Dictionary<Thing, int> totalFoodToTake,
            out float verifiedOutboundDays,
            out float verifiedReturnDays)
        {
            totalFoodToTake = new Dictionary<Thing, int>();
            verifiedOutboundDays = 0f;
            verifiedReturnDays = 0f;

            if (map == null || allPawns == null || allPawns.Count == 0) return false;

            List<Thing> colonyItems = GetFormableColonyItems(map);
            List<Thing> foodList = colonyItems
                .Where(t => IsCaravanFood(t))
                .OrderByDescending(t => t.def.GetCompProperties<CompProperties_Rottable>() == null)
                .ThenByDescending(t => t.def.ingestible != null ? (int)t.def.ingestible.preferability : 0)
                .ToList();

            Dictionary<Thing, int> remainingInStack = new Dictionary<Thing, int>();
            for (int i = 0; i < foodList.Count; i++)
            {
                remainingInStack[foodList[i]] = foodList[i].stackCount;
            }

            Dictionary<Thing, int> returnFood = new Dictionary<Thing, int>();
            if (returningPawns != null && returningPawns.Count > 0 && daysReturn > 0.001f)
            {
                PlanetTile retTile = outpostTile.Valid ? outpostTile : map.Tile;
                verifiedReturnDays = SelectFoodLegWithTransferables(
                    returningPawns, foodList, remainingInStack, retTile, daysReturn, returnFood);
                if (verifiedReturnDays < daysReturn)
                {
                    return false;
                }
            }

            Dictionary<Thing, int> outboundFood = new Dictionary<Thing, int>();
            if (daysOutbound > 0.001f)
            {
                verifiedOutboundDays = SelectFoodLegWithTransferables(
                    allPawns, foodList, remainingInStack, map.Tile, daysOutbound, outboundFood);
                if (verifiedOutboundDays < daysOutbound)
                {
                    return false;
                }
            }

            foreach (KeyValuePair<Thing, int> kv in returnFood)
            {
                totalFoodToTake[kv.Key] = kv.Value;
            }
            foreach (KeyValuePair<Thing, int> kv in outboundFood)
            {
                int prev;
                if (totalFoodToTake.TryGetValue(kv.Key, out prev))
                {
                    totalFoodToTake[kv.Key] = prev + kv.Value;
                }
                else
                {
                    totalFoodToTake[kv.Key] = kv.Value;
                }
            }

            return true;
        }

        private static float SelectFoodLegWithTransferables(
            List<Pawn> legPawns,
            List<Thing> orderedFood,
            Dictionary<Thing, int> remainingInStack,
            PlanetTile startTile,
            float requiredDays,
            Dictionary<Thing, int> legSelection)
        {
            float daysNow = CalcTransferableFoodDays(legPawns, legSelection, startTile);
            if (daysNow >= requiredDays) return daysNow;

            float targetNutr = ComputeDailyNutrition(legPawns) * requiredDays * 1.15f;
            float addedNutr = 0f;

            for (int i = 0; i < orderedFood.Count; i++)
            {
                Thing food = orderedFood[i];
                int avail;
                if (!remainingInStack.TryGetValue(food, out avail) || avail <= 0) continue;
                if (!AnyPawnWillEat(legPawns, food)) continue;

                float nutr = food.GetStatValue(StatDefOf.Nutrition);
                if (nutr <= 0.01f) continue;

                int needed = Mathf.Max(1, Mathf.CeilToInt((targetNutr - addedNutr) / nutr));
                int take = Mathf.Min(needed, avail);
                if (take <= 0) continue;

                int prev;
                legSelection.TryGetValue(food, out prev);
                legSelection[food] = prev + take;
                remainingInStack[food] = avail - take;
                addedNutr += (float)take * nutr;

                daysNow = CalcTransferableFoodDays(legPawns, legSelection, startTile);
                if (daysNow >= requiredDays) return daysNow;

                if (addedNutr >= targetNutr && remainingInStack[food] > 0)
                {
                    int extra = Mathf.Min(Mathf.CeilToInt((requiredDays - daysNow + 0.3f) * ComputeDailyNutrition(legPawns) / nutr), remainingInStack[food]);
                    if (extra > 0)
                    {
                        legSelection[food] = legSelection[food] + extra;
                        remainingInStack[food] = remainingInStack[food] - extra;
                        daysNow = CalcTransferableFoodDays(legPawns, legSelection, startTile);
                        if (daysNow >= requiredDays) return daysNow;
                    }
                }
            }

            return CalcTransferableFoodDays(legPawns, legSelection, startTile);
        }

        public static float CalcTransferableFoodDays(List<Pawn> legPawns, Dictionary<Thing, int> selectedFood, PlanetTile tile)
        {
            if (legPawns == null || legPawns.Count == 0) return 0f;

            List<TransferableOneWay> list = new List<TransferableOneWay>();
            for (int i = 0; i < legPawns.Count; i++)
            {
                Pawn p = legPawns[i];
                if (p == null) continue;
                TransferableOneWay tp = new TransferableOneWay();
                tp.things.Add(p);
                tp.ForceTo(1);
                list.Add(tp);
            }

            if (selectedFood != null)
            {
                foreach (KeyValuePair<Thing, int> kv in selectedFood)
                {
                    if (kv.Key == null || kv.Value <= 0) continue;
                    TransferableOneWay tf = new TransferableOneWay();
                    tf.things.Add(kv.Key);
                    tf.ForceTo(Mathf.Min(kv.Value, kv.Key.stackCount));
                    list.Add(tf);
                }
            }

            return DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(
                list, tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, Faction.OfPlayer); // CHECK API
        }

        private static bool IsCaravanFood(Thing t)
        {
            return t != null && !t.Destroyed && t.stackCount > 0 && t.def != null &&
                   t.def.IsIngestible && t.def.IsNutritionGivingIngestible &&
                   !t.def.IsDrug && !t.def.IsCorpse && !t.IsForbidden(Faction.OfPlayer);
        }

        private static bool AnyPawnWillEat(List<Pawn> pawns, Thing food)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && p.RaceProps != null && p.RaceProps.CanEverEat(food)) return true;
            }
            return false;
        }

        private static float ComputeDailyNutrition(List<Pawn> pawns)
        {
            float sum = 0f;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && p.needs != null && p.needs.food != null && p.needs.food.FoodFallPerTick > 0f)
                {
                    sum += p.needs.food.FoodFallPerTick * 60000f;
                }
                else
                {
                    sum += 1.6f;
                }
            }
            return Mathf.Max(0.1f, sum);
        }

        private static List<Pawn> GetReturningPawns(List<Pawn> allPawns, List<Pawn> stayPawns)
        {
            List<Pawn> ret = new List<Pawn>();
            if (allPawns == null) return ret;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn p = allPawns[i];
                if (p != null && (stayPawns == null || !stayPawns.Contains(p)))
                {
                    ret.Add(p);
                }
            }
            return ret;
        }

        // =====================================================================
        // START CARAVAN (JOURNALED SILVER TAGGING + EXACT LORD BINDING)
        // =====================================================================

        private sealed class SilverPrepJournalEntry
        {
            public Thing origStack;
            public Thing missionStack;
            public IntVec3 origPos;
            public bool wasSplit;
            public string protectedId;
            public Pawn inventoryPawn; // set when the mission stack was moved straight into a carrier's inventory
        }

        public static bool StartCaravan(StrategicOutpost outpost, Map map, List<Pawn> pawns, List<Pawn> stayPawns, int silverAmount, out string failReason)
        {
            failReason = null;
            float daysOneWay, daysRoundTrip, foodDays;
            bool canSend;
            string cantSendReason;

            if (!CheckCaravanFeasibility(outpost, map, pawns, stayPawns, silverAmount,
                    out daysOneWay, out daysRoundTrip, out foodDays, out canSend, out cantSendReason))
            {
                failReason = cantSendReason;
                return false;
            }

            List<Pawn> returningPawns = GetReturningPawns(pawns, stayPawns);
            float daysReturn = Mathf.Max(0f, daysRoundTrip - daysOneWay);

            Dictionary<Thing, int> foodToTake;
            float outFoodDays, retFoodDays;
            if (!CollectFoodForTwoLegs(map, outpost.Tile, pawns, returningPawns, daysOneWay, daysReturn, out foodToTake, out outFoodDays, out retFoodDays))
            {
                failReason = "TMM_OutpostCaravan_NotEnoughFoodTip".Translate(daysRoundTrip.ToString("F1"));
                return false;
            }

            string preMissionId = Guid.NewGuid().ToString("N");
            List<TransferableOneWay> transferables = new List<TransferableOneWay>();
            List<SilverPrepJournalEntry> silverPrepJournal = new List<SilverPrepJournalEntry>();
            List<string> missionSilverIds = new List<string>();
            Dictionary<int, int> baselineSilver = new Dictionary<int, int>();

            // 1. Add pawns
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                baselineSilver[p.thingIDNumber] = CountPawnInventoryThing(p, ThingDefOf.Silver);
                TransferableOneWay t = new TransferableOneWay();
                t.things.Add(p);
                t.ForceTo(1);
                transferables.Add(t);
            }

            // 2. Tag exact colony silver stacks for this mission (splitting partial stacks with journal + anti-merge!)
            bool silverPrepOk = true;
            if (silverAmount > 0)
            {
                try
                {
                    List<Thing> colonyItems = GetFormableColonyItems(map);
                    int remainingSilver = silverAmount;

                    for (int i = 0; i < colonyItems.Count && remainingSilver > 0; i++)
                    {
                        Thing s = colonyItems[i];
                        if (s == null || s.Destroyed || !s.Spawned || s.Map != map || s.def != ThingDefOf.Silver ||
                            s.stackCount <= 0 || s.IsForbidden(Faction.OfPlayer) || OutpostCaravanManager.IsMissionSilverThing(s))
                        {
                            continue;
                        }

                        int take = Mathf.Min(remainingSilver, s.stackCount);
                        SilverPrepJournalEntry prep = new SilverPrepJournalEntry();
                        prep.origStack = s;
                        prep.origPos = s.Position;
                        prep.wasSplit = (take < s.stackCount);
                        silverPrepJournal.Add(prep);

                        Thing missionStack;
                        if (!prep.wasSplit)
                        {
                            missionStack = s;
                            prep.missionStack = missionStack;
                            prep.protectedId = missionStack.ThingID;
                            OutpostCaravanManager.RegisterTransientSilverProtection(prep.protectedId, preMissionId);
                        }
                        else
                        {
                            Thing split = s.SplitOff(take);
                            prep.missionStack = split;
                            if (split == null || split.Destroyed || split.stackCount != take)
                            {
                                silverPrepOk = false;
                                break;
                            }

                            // Protect BEFORE GenPlace so the split stack cannot merge back into origStack
                            prep.protectedId = split.ThingID;
                            OutpostCaravanManager.RegisterTransientSilverProtection(prep.protectedId, preMissionId);

                            Thing placed;
                            if (!GenPlace.TryPlaceThing(split, prep.origPos, map, ThingPlaceMode.Near, out placed) ||
                                placed == null || placed.Destroyed || !placed.Spawned || placed.ThingID != split.ThingID)
                            {
                                silverPrepOk = false;
                                break;
                            }
                            split.SetForbidden(false, false);
                            missionStack = split;
                        }

                        missionSilverIds.Add(missionStack.ThingID);

                        // Load mission silver straight into a carrier's inventory instead of relying on vanilla
                        // gathering: vanilla gather jobs proved unreliable with merge-protected stacks (the caravan
                        // could leave with part or none of the silver). The stack keeps its ThingID and protection.
                        Pawn silverCarrier = PickSilverCarrier(pawns, stayPawns);
                        int stackAmount = missionStack.stackCount;
                        if (silverCarrier == null || silverCarrier.inventory == null)
                        {
                            silverPrepOk = false;
                            break;
                        }
                        if (missionStack.Spawned) missionStack.DeSpawn(DestroyMode.Vanish);
                        prep.inventoryPawn = silverCarrier;
                        if (!silverCarrier.inventory.innerContainer.TryAdd(missionStack, false) || missionStack.holdingOwner != silverCarrier.inventory.innerContainer)
                        {
                            silverPrepOk = false;
                            break;
                        }
                        remainingSilver -= stackAmount;
                    }

                    if (remainingSilver > 0) silverPrepOk = false;
                }
                catch (Exception ex)
                {
                    Log.Warning("[TMM] OutpostCaravanLogistics: failed preparing mission silver stacks: " + ex.Message);
                    silverPrepOk = false;
                }
            }

            if (!silverPrepOk)
            {
                RollbackSilverPrepJournal(map, silverPrepJournal);
                failReason = "StrategicOutpost_NotEnoughSilverToSend".Translate(silverAmount);
                return false;
            }

            // 3. Add food with exact TransferableOneWay counts
            foreach (KeyValuePair<Thing, int> kvp in foodToTake)
            {
                TransferableOneWay t = new TransferableOneWay();
                t.things.Add(kvp.Key);
                t.ForceTo(kvp.Value);
                transferables.Add(t);
            }

            // 4. Find exit spot & meeting point
            IntVec3 center = IntVec3.Zero;
            for (int i = 0; i < pawns.Count; i++) center += pawns[i].Position;
            center = new IntVec3(center.x / pawns.Count, 0, center.z / pawns.Count);

            IntVec3 exitSpot;
            if (!RCellFinder.TryFindClosestEdgeCellTo(center, map, out exitSpot))
            {
                if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Always, out exitSpot))
                {
                    exitSpot = CellFinder.RandomEdgeCell(map);
                }
            }

            IntVec3 meetingPoint;
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(exitSpot, map, out meetingPoint))
            {
                meetingPoint = center;
            }

            PlanetTile startTile = CaravanExitMapUtility.BestExitTileToGoTo(outpost.Tile, map);
            if (!startTile.Valid) startTile = map.Tile;

            // 5. Launch vanilla formation and capture exact formation Lord ID (rolling back silver prep on failure)
            HashSet<int> prevLordIds = new HashSet<int>();
            if (map.lordManager != null)
            {
                for (int i = 0; i < map.lordManager.lords.Count; i++)
                {
                    if (map.lordManager.lords[i] != null) prevLordIds.Add(map.lordManager.lords[i].loadID);
                }
            }

            try
            {
                CaravanFormingUtility.StartFormingCaravan(
                    pawns, new List<Pawn>(), Faction.OfPlayer, transferables, meetingPoint, exitSpot, startTile, outpost.Tile);
            }
            catch (Exception ex)
            {
                TestMode.ReportException("[TMM] OutpostCaravanLogistics: StartFormingCaravan threw exception: ", ex);
                RollbackSilverPrepJournal(map, silverPrepJournal);
                failReason = "TMM_OutpostCaravan_FormationFailed".Translate();
                return false;
            }

            int formationLordId = -1;
            if (map.lordManager != null)
            {
                List<Lord> lords = map.lordManager.lords;
                for (int i = lords.Count - 1; i >= 0; i--)
                {
                    Lord l = lords[i];
                    if (l == null || prevLordIds.Contains(l.loadID)) continue;
                    if (l.LordJob is LordJob_FormAndSendCaravan && l.ownedPawns != null && l.ownedPawns.Contains(pawns[0]))
                    {
                        formationLordId = l.loadID;
                        break;
                    }
                }
            }

            if (formationLordId < 0)
            {
                RollbackSilverPrepJournal(map, silverPrepJournal);
                failReason = "TMM_OutpostCaravan_FormationFailed".Translate();
                return false;
            }

            // 6. Register mission bound 1-to-1 to this formation Lord & exact missionSilverIds
            if (OutpostCaravanManager.Instance != null)
            {
                OutpostCaravanManager.Instance.RegisterMissionBound(
                    outpost, map, pawns, stayPawns, silverAmount,
                    map.uniqueID, formationLordId, baselineSilver, missionSilverIds, preMissionId);
            }

            Messages.Message("CaravanFormationProcessStarted".Translate(), pawns[0], MessageTypeDefOf.PositiveEvent, false);
            return true;
        }

        private static Pawn PickSilverCarrier(List<Pawn> pawns, List<Pawn> stayPawns)
        {
            if (pawns == null) return null;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && p.inventory != null && (stayPawns == null || !stayPawns.Contains(p))) return p;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && !pawns[i].Dead && pawns[i].inventory != null) return pawns[i];
            }
            return null;
        }
        private static void RollbackSilverPrepJournal(Map map, List<SilverPrepJournalEntry> journal)
        {
            if (journal == null) return;
            for (int i = journal.Count - 1; i >= 0; i--)
            {
                SilverPrepJournalEntry entry = journal[i];
                if (entry == null) continue;
                try
                {
                    if (!entry.protectedId.NullOrEmpty())
                    {
                        OutpostCaravanManager.UnregisterTransientSilverProtection(entry.protectedId);
                    }
                    if (entry.missionStack == null || entry.missionStack.Destroyed) continue;
                    if (!entry.wasSplit)
                    {
                        // Whole stack: if it was moved into a carrier's inventory (or despawned on the way), put it back.
                        Thing whole = entry.missionStack;
                        if (!whole.Spawned)
                        {
                            if (whole.holdingOwner != null) whole.holdingOwner.Remove(whole);
                            if (map != null && entry.origPos.InBounds(map)) GenPlace.TryPlaceThing(whole, entry.origPos, map, ThingPlaceMode.Near);
                        }
                        continue;
                    }

                    Thing split = entry.missionStack;
                    if (split.holdingOwner != null) split.holdingOwner.Remove(split);
                    if (split.Spawned) split.DeSpawn(DestroyMode.Vanish);

                    if (entry.origStack != null && !entry.origStack.Destroyed && entry.origStack.Spawned && entry.origStack.Map == map)
                    {
                        if (entry.origStack.TryAbsorbStack(split, false) && (split.Destroyed || split.stackCount <= 0))
                        {
                            continue;
                        }
                    }

                    if (map != null && entry.origPos.InBounds(map))
                    {
                        GenPlace.TryPlaceThing(split, entry.origPos, map, ThingPlaceMode.Near);
                    }
                }
                catch (Exception ex)
                {
                    TestMode.ReportException("[TMM] OutpostCaravanLogistics: failed rolling back silver prep entry: ", ex);
                }
            }
        }
    }

    // =========================================================================
    // HARMONY PATCHES: GIZMOS (RETRY + RETURN HOME), EXIT BINDING, ANTI-MERGE
    // =========================================================================

    [HarmonyPatch(typeof(Caravan), "GetGizmos")]
    public static class Patch_Caravan_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Caravan __instance)
        {
            if (values != null)
            {
                foreach (Gizmo g in values)
                {
                    yield return g;
                }
            }

            if (__instance == null || __instance.Destroyed || __instance.Faction != Faction.OfPlayer) yield break;

            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            OutpostCaravanMission mission = mgr != null ? mgr.GetMissionForCaravan(__instance) : null;
            CaravanArrivalAction_OutpostDelivery delivery = __instance.pather != null
                ? __instance.pather.ArrivalAction as CaravanArrivalAction_OutpostDelivery : null;

            // 1. Explicit "Retry Delivery" gizmo when storage failed at the outpost
            if (mission != null && mission.state == OutpostMissionState.RetryDelivery &&
                !mission.delivered && !mission.cancelled &&
                mission.outpost != null && mission.outpost.Spawned && mission.outpost.Faction == Faction.OfPlayer)
            {
                Command_Action retryCmd = new Command_Action();
                retryCmd.defaultLabel = "TMM_OutpostCaravan_RetryDelivery".Translate();
                retryCmd.defaultDesc = "TMM_OutpostCaravan_RetryDeliveryDesc".Translate(mission.outpost.Label);
                retryCmd.icon = TexCommand.RearmTrap; // CHECK API
                retryCmd.action = delegate
                {
                    if (OutpostCaravanManager.Instance != null)
                    {
                        OutpostCaravanManager.Instance.TryRetryDelivery(__instance);
                    }
                };
                yield return retryCmd;
            }

            // 2. "Return Home" gizmo on Outbound, RetryDelivery, or stranded Returning
            bool showReturnButton = (delivery != null) ||
                                    (mission != null &&
                                     (mission.state == OutpostMissionState.Outbound ||
                                      mission.state == OutpostMissionState.RetryDelivery ||
                                      (mission.state == OutpostMissionState.Returning && (mission.stranded || (__instance.pather != null && !__instance.pather.Moving)))));

            if (!showReturnButton) yield break;

            Command_Action returnHome = new Command_Action();
            returnHome.defaultLabel = "TMM_OutpostCaravan_ReturnHome".Translate();
            returnHome.defaultDesc = "TMM_OutpostCaravan_ReturnHomeDesc".Translate();
            returnHome.icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", false) ?? TexCommand.ClearPrioritizedWork;
            returnHome.action = delegate
            {
                if (OutpostCaravanManager.Instance != null)
                {
                    OutpostCaravanManager.Instance.TryCancelAndReturnHome(__instance);
                }
                else
                {
                    Map pref = delivery != null ? delivery.OriginMap : null;
                    if (!OutpostCaravanManager.TryStartReturnPath(__instance, pref))
                    {
                        Messages.Message("TMM_OutpostCaravan_HomeUnreachable".Translate(__instance.Name), __instance, MessageTypeDefOf.RejectInput, false);
                    }
                }
            };
            yield return returnHome;
        }
    }

    public struct OutpostFormationExitState
    {
        public int mapId;
        public int lordId;
    }

    [HarmonyPatch(typeof(CaravanExitMapUtility), "ExitMapAndCreateCaravan",
        new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(PlanetTile), typeof(PlanetTile), typeof(PlanetTile), typeof(bool) })]
    public static class Patch_CaravanExitMapUtility_ExitMapAndCreateCaravan
    {
        public static void Prefix(IEnumerable<Pawn> pawns, out OutpostFormationExitState __state)
        {
            __state = new OutpostFormationExitState();
            __state.mapId = -1;
            __state.lordId = -1;

            if (pawns == null) return;
            foreach (Pawn p in pawns)
            {
                if (p == null) continue;
                if (__state.mapId < 0 && p.Map != null)
                {
                    __state.mapId = p.Map.uniqueID;
                }
                Lord l = p.GetLord();
                if (l != null && l.LordJob is LordJob_FormAndSendCaravan)
                {
                    __state.lordId = l.loadID;
                    if (l.Map != null) __state.mapId = l.Map.uniqueID;
                    break;
                }
            }
        }

        public static void Postfix(Caravan __result, OutpostFormationExitState __state)
        {
            if (__result == null || __state.lordId < 0 || __state.mapId < 0) return;
            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            if (mgr != null)
            {
                mgr.AttachFormedCaravan(__result, __state.mapId, __state.lordId);
            }
        }
    }

    /// <summary>
    /// Prevents tagged mission silver stacks from merging into personal silver or colony silver stacks.
    /// </summary>
    [HarmonyPatch(typeof(Thing), "CanStackWith", new Type[] { typeof(Thing) })] // CHECK API
    public static class Patch_Thing_CanStackWith_MissionSilver
    {
        public static void Postfix(Thing __instance, Thing other, ref bool __result)
        {
            if (!__result) return;
            if (OutpostCaravanManager.IsMissionSilverThing(__instance) || OutpostCaravanManager.IsMissionSilverThing(other))
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Propagates mission silver identity if a pawn splits a tagged mission silver stack during hauling.
    /// </summary>
    [HarmonyPatch(typeof(Thing), "SplitOff", new Type[] { typeof(int) })] // CHECK API
    public static class Patch_Thing_SplitOff_MissionSilver
    {
        public static void Postfix(Thing __instance, Thing __result)
        {
            if (__result == null || __result == __instance) return;
            if (!OutpostCaravanManager.IsMissionSilverThing(__instance)) return;

            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            if (mgr != null)
            {
                mgr.PropagateMissionSilverSplit(__instance.ThingID, __result.ThingID);
            }
        }
    }
}
