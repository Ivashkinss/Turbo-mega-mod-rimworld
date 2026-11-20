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
        Failed
    }

    /// <summary>
    /// Arrival action for a walking caravan delivering workers and silver to a strategic outpost.
    /// Idempotent, checks outpost faction/state, transfers mission silver atomically with full rollback,
    /// preserves returning carriers' food and mobility, and transitions mission to Returning state.
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
            if (mgr != null && !missionId.NullOrEmpty())
            {
                OutpostCaravanMission m = mgr.GetMissionById(missionId);
                if (m != null && (m.delivered || m.cancelled || (m.state != OutpostMissionState.Outbound && m.state != OutpostMissionState.Forming)))
                {
                    return false;
                }
            }
            return true;
        }

        public override void Arrived(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed) return;
            if (alreadyHandled) return;

            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            OutpostCaravanMission mission = null;
            if (mgr != null)
            {
                mission = !missionId.NullOrEmpty() ? mgr.GetMissionById(missionId) : mgr.GetMissionForCaravan(caravan);
            }

            // 1. Guard against duplicate delivery or inactive mission
            if (mission != null && (mission.delivered || mission.cancelled || mission.state != OutpostMissionState.Outbound))
            {
                alreadyHandled = true;
                return;
            }

            // 2. Verify outpost still exists, is spawned, and belongs to the player
            if (outpost == null || !outpost.Spawned || outpost.Faction != Faction.OfPlayer)
            {
                alreadyHandled = true;
                Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
                if (mgr != null && mission != null)
                {
                    mgr.TrySendMissionCaravanHome(mission, caravan, false);
                }
                else
                {
                    OutpostCaravanManager.TryStartReturnPath(caravan, originMap);
                }
                return;
            }

            // 3. Determine exact mission silver (never raid personal silver beyond mission quota)
            int targetSilver = mission != null ? mission.loadedSilver : silverAmount;
            Dictionary<int, int> personalBaseline = mission != null ? mission.baselineSilverByPawn : null;

            if (targetSilver > 0)
            {
                string silverFail;
                if (!OutpostCaravanLogistics.TryTransferMissionSilverAtomically(caravan, outpost, targetSilver, personalBaseline, out silverFail))
                {
                    // Atomic failure: all silver restored to original pawn inventories; NOT marked delivered
                    Messages.Message(silverFail ?? "TMM_OutpostCaravan_OutpostGone".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            // 4. Preserve return food for carriers BEFORE transferring staying workers
            Map home = OutpostCaravanManager.ResolveHomeMap(originMap);
            List<Pawn> stayingNow = CollectStayingPawns(caravan, stayPawns);
            List<Pawn> returningNow = CollectReturningPawns(caravan, stayingNow);

            if (returningNow.Count > 0 && stayingNow.Count > 0)
            {
                float returnDays = 0f;
                if (home != null)
                {
                    float retCap = OutpostCaravanLogistics.ComputePawnsCapacity(returningNow);
                    float retMass = OutpostCaravanLogistics.ComputeBaseMass(returningNow, 0);
                    OutpostCaravanLogistics.CalculateLegDays(outpost.Tile, home.Tile, returningNow, retMass, retCap, out returnDays);
                }
                OutpostCaravanLogistics.EnsureCarriersHaveReturnFood(caravan, stayingNow, returningNow, outpost.Tile, Mathf.Max(0.5f, returnDays));
            }

            // 5. Transfer staying workers safely (verifying at least one mobile carrier remains if others stay in caravan)
            for (int i = stayingNow.Count - 1; i >= 0; i--)
            {
                Pawn p = stayingNow[i];
                if (p == null || !caravan.ContainsPawn(p)) continue;

                // Ensure removing p does not strand remaining pawns without a mobile colonist carrier
                if (caravan.PawnsListForReading.Count > 1 && !OutpostCaravanLogistics.HasMobileCarrierExcluding(caravan, p))
                {
                    string noCarrierMsg = "StrategicOutpost_NoMobileCarrierLeft".Translate();
                    Messages.Message("TMM_OutpostCaravan_WorkerRejected".Translate(p.LabelShort, noCarrierMsg), outpost, MessageTypeDefOf.CautionInput, false);
                    continue;
                }

                string failReason;
                if (!outpost.AddPawnFromCaravan(caravan, p, out failReason, true))
                {
                    Messages.Message("TMM_OutpostCaravan_WorkerRejected".Translate(p.LabelShort, failReason ?? ""), outpost, MessageTypeDefOf.CautionInput, false);
                }
            }

            // 6. Mark delivery confirmed ONLY after silver & worker transfer succeeded
            alreadyHandled = true;
            if (mission != null)
            {
                mission.delivered = true;
            }

            // 7. Return remaining carriers home (enter Returning state until colony entry)
            bool hasCarriersLeft = !caravan.Destroyed && caravan.PawnsListForReading != null && caravan.PawnsListForReading.Count > 0;
            if (hasCarriersLeft)
            {
                if (mgr != null && mission != null)
                {
                    bool started = mgr.TrySendMissionCaravanHome(mission, caravan, true);
                    if (started && home != null && home.Parent != null)
                    {
                        Messages.Message("TMM_OutpostCaravan_DeliveredReturning".Translate(outpost.Label, home.Parent.Label), caravan, MessageTypeDefOf.PositiveEvent, false);
                    }
                }
                else
                {
                    if (OutpostCaravanManager.TryStartReturnPath(caravan, originMap) && home != null && home.Parent != null)
                    {
                        Messages.Message("TMM_OutpostCaravan_DeliveredReturning".Translate(outpost.Label, home.Parent.Label), caravan, MessageTypeDefOf.PositiveEvent, false);
                    }
                }
            }
            else if (mgr != null)
            {
                mgr.NotifyDelivered(caravan);
                if (mission != null)
                {
                    mission.state = OutpostMissionState.Completed;
                }
            }
        }

        private static List<Pawn> CollectStayingPawns(Caravan caravan, List<Pawn> configuredStay)
        {
            List<Pawn> list = new List<Pawn>();
            if (caravan == null || configuredStay == null) return list;
            for (int i = 0; i < configuredStay.Count; i++)
            {
                Pawn p = configuredStay[i];
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
    /// Persistent mission record bound 1-to-1 to a specific formation Lord and its formed Caravan.
    /// </summary>
    public class OutpostCaravanMission : IExposable
    {
        public string missionId;
        public OutpostMissionState state = OutpostMissionState.Forming;
        public StrategicOutpost outpost;
        public Map originMap;
        public Caravan caravan;
        public List<Pawn> pawns = new List<Pawn>();
        public List<Pawn> stayPawns = new List<Pawn>();
        public int silverAmount;
        public int loadedSilver;
        public int formationMapId = -1;
        public int formationLordId = -1;
        public bool everAttached;
        public bool delivered;
        public bool cancelled;
        public bool stranded;
        public int baselinePersonalSilverTotal;
        public Dictionary<int, int> baselineSilverByPawn = new Dictionary<int, int>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref missionId, "missionId");
            Scribe_Values.Look(ref state, "state", OutpostMissionState.Forming);
            Scribe_References.Look(ref outpost, "outpost");
            Scribe_References.Look(ref originMap, "originMap");
            Scribe_References.Look(ref caravan, "caravan");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Collections.Look(ref stayPawns, "stayPawns", LookMode.Reference);
            Scribe_Values.Look(ref silverAmount, "silverAmount", 0);
            Scribe_Values.Look(ref loadedSilver, "loadedSilver", 0);
            Scribe_Values.Look(ref formationMapId, "formationMapId", -1);
            Scribe_Values.Look(ref formationLordId, "formationLordId", -1);
            Scribe_Values.Look(ref everAttached, "everAttached", false);
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
                if (baselineSilverByPawn == null) baselineSilverByPawn = new Dictionary<int, int>();
                pawns.RemoveAll(p => p == null);
                stayPawns.RemoveAll(p => p == null);
                // Legacy migration: if caravan already saved on older save, never allow re-attachment
                if (caravan != null) everAttached = true;
            }
        }
    }

    /// <summary>
    /// WorldComponent managing walking caravan missions to strategic outposts.
    /// Attaches caravans strictly by formation Lord event, runs periodic audits every 250 ticks,
    /// handles vanished outposts and unreachable homes cleanly, and cleans up before save / after load.
    /// </summary>
    public class OutpostCaravanManager : WorldComponent
    {
        private const int AuditIntervalTicks = 250;

        private List<OutpostCaravanMission> missions = new List<OutpostCaravanMission>();

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

        public void RegisterMission(StrategicOutpost outpost, Map originMap, List<Pawn> pawns, List<Pawn> stayPawns, int silverAmount)
        {
            RegisterMissionBound(outpost, originMap, pawns, stayPawns, silverAmount, originMap != null ? originMap.uniqueID : -1, -1, null);
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
            if (outpost == null || pawns == null || pawns.Count == 0) return null;

            OutpostCaravanMission mission = new OutpostCaravanMission();
            mission.missionId = Guid.NewGuid().ToString("N");
            mission.state = OutpostMissionState.Forming;
            mission.outpost = outpost;
            mission.originMap = originMap;
            mission.pawns = pawns.Where(p => p != null).ToList();
            mission.stayPawns = stayPawns != null ? stayPawns.Where(p => p != null).ToList() : new List<Pawn>();
            mission.silverAmount = Mathf.Max(0, silverAmount);
            mission.loadedSilver = 0;
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
                if (m != null && m.caravan == c && (m.state == OutpostMissionState.Outbound || m.state == OutpostMissionState.Returning))
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
                    if (m.state == OutpostMissionState.Outbound)
                    {
                        m.state = (c.Destroyed || c.PawnsListForReading.Count == 0)
                            ? OutpostMissionState.Completed
                            : OutpostMissionState.Returning;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Kept for backward API compatibility. Cancels only if return path is confirmed.
        /// </summary>
        public void CancelMission(Caravan c)
        {
            TryCancelAndReturnHome(c);
        }

        /// <summary>
        /// Attempts to reroute an Outbound or stranded Returning caravan back home.
        /// Confirms cancellation ONLY after StartPath succeeds and updates pather destination.
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
                if (m != null) m.stranded = true;
                Messages.Message("TMM_OutpostCaravan_HomeUnreachable".Translate(c.Name), c, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (m != null)
            {
                if (m.state == OutpostMissionState.Outbound)
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
        /// Strictly binds a newly exited Caravan ONLY to the Forming mission whose exact
        /// formationMapId and formationLordId match. Never hijacks unrelated caravans and never
        /// re-attaches an already-formed mission.
        /// </summary>
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

                // Verify outpost still exists at exact moment of exit
                if (m.outpost == null || !m.outpost.Spawned || m.outpost.Faction != Faction.OfPlayer)
                {
                    Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), car, MessageTypeDefOf.CautionInput, false);
                    TrySendMissionCaravanHome(m, car, false);
                    return true;
                }

                // Verify actual loaded mission silver (excluding pre-existing personal silver)
                int totalSilverNow = OutpostCaravanLogistics.CountCaravanSilver(car);
                int actualLoaded = Mathf.Max(0, totalSilverNow - m.baselinePersonalSilverTotal);
                if (m.silverAmount > 0 && actualLoaded < m.silverAmount)
                {
                    m.loadedSilver = actualLoaded;
                    m.cancelled = true;
                    Messages.Message("TMM_OutpostCaravan_MissingSilverAborted".Translate(actualLoaded, m.silverAmount), car, MessageTypeDefOf.RejectInput, false);
                    TrySendMissionCaravanHome(m, car, false);
                    return true;
                }
                m.loadedSilver = Mathf.Min(m.silverAmount, actualLoaded);

                // Start outbound path with verified arrival action
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
                    // Route to outpost failed immediately -> attempt return home
                    m.state = OutpostMissionState.Returning;
                    Messages.Message("TMM_OutpostCaravan_PathUnreachableTip".Translate(), car, MessageTypeDefOf.RejectInput, false);
                    TrySendMissionCaravanHome(m, car, false);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Kept for signature compatibility only; refuses unverified global attachment to prevent hijacking.
        /// </summary>
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

        public static Map ResolveHomeMap(Map preferred)
        {
            if (preferred != null && Find.Maps != null && Find.Maps.Contains(preferred) && preferred.IsPlayerHome)
            {
                return preferred;
            }
            if (Find.Maps == null) return null;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m != null && m.IsPlayerHome) return m;
            }
            return null;
        }

        public static bool TryStartReturnPath(Caravan car, Map preferredHome)
        {
            if (car == null || car.Destroyed || !car.Spawned || car.pather == null) return false;
            Map home = ResolveHomeMap(preferredHome);
            if (home == null || home.Parent == null) return false;

            CaravanArrivalAction_Enter enter = new CaravanArrivalAction_Enter(home.Parent);
            if (car.Tile == home.Tile)
            {
                enter.Arrived(car);
                return car.Destroyed || !car.Spawned;
            }

            car.pather.StartPath(home.Tile, enter, true, true); // CHECK API
            return car.pather.Moving && car.pather.Destination == home.Tile && car.pather.ArrivalAction is CaravanArrivalAction_Enter;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % AuditIntervalTicks != 0) return;
            AuditAndCleanupMissions(false);
        }

        public void AuditAndCleanupMissions(bool isScribeBoundary)
        {
            if (missions == null)
            {
                missions = new List<OutpostCaravanMission>();
                return;
            }

            for (int i = missions.Count - 1; i >= 0; i--)
            {
                OutpostCaravanMission m = missions[i];
                if (m == null)
                {
                    missions.RemoveAt(i);
                    continue;
                }

                if (m.pawns != null) m.pawns.RemoveAll(p => p == null);
                if (m.stayPawns != null) m.stayPawns.RemoveAll(p => p == null);

                if (m.state == OutpostMissionState.Completed || m.state == OutpostMissionState.Failed)
                {
                    missions.RemoveAt(i);
                    continue;
                }

                // 1. Forming state: check if formation lord is still alive on its origin map
                if (m.state == OutpostMissionState.Forming)
                {
                    if (m.everAttached)
                    {
                        m.state = OutpostMissionState.Failed;
                        missions.RemoveAt(i);
                        continue;
                    }

                    Map formMap = FindMapById(m.formationMapId);
                    Lord formLord = FindFormationLord(formMap, m.formationLordId);
                    if (formMap == null || formLord == null || m.pawns == null || m.pawns.Count == 0)
                    {
                        // Formation aborted / failed without exiting -> remove mission cleanly
                        m.state = OutpostMissionState.Failed;
                        missions.RemoveAt(i);
                        continue;
                    }

                    if (!isScribeBoundary && (m.outpost == null || !m.outpost.Spawned || m.outpost.Faction != Faction.OfPlayer))
                    {
                        // Outpost disappeared before caravan left the map -> abort formation cleanly
                        formMap.lordManager.RemoveLord(formLord);
                        Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), MessageTypeDefOf.CautionInput, false);
                        m.state = OutpostMissionState.Cancelled;
                        m.cancelled = true;
                        missions.RemoveAt(i);
                        continue;
                    }
                    continue;
                }

                // 2. Outbound / Returning states: require live attached caravan
                if (m.caravan == null || m.caravan.Destroyed || !m.caravan.Spawned)
                {
                    m.state = (m.state == OutpostMissionState.Returning) ? OutpostMissionState.Completed : OutpostMissionState.Failed;
                    missions.RemoveAt(i);
                    continue;
                }

                // 3. Outbound state: if outpost vanished en route, immediately turn caravan around with its cargo!
                if (m.state == OutpostMissionState.Outbound)
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

                // 4. Returning state: if stranded earlier and a home map became available, stay marked so gizmo works
                if (m.state == OutpostMissionState.Returning && !isScribeBoundary)
                {
                    if (m.caravan.pather != null && !m.caravan.pather.Moving)
                    {
                        m.stranded = true;
                    }
                }
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
                AuditAndCleanupMissions(true);
            }
        }
    }

    /// <summary>
    /// Static logistics calculations, atomic silver transfer, carrier food split,
    /// and TransferableOneWay-based bidirectional food assessment.
    /// </summary>
    public static class OutpostCaravanLogistics
    {
        /// <summary>
        /// Test-only hook for InGameOutpostCaravanProbe to simulate RawMaterials storage refusal.
        /// Always false in normal gameplay.
        /// </summary>
        public static bool TestRejectRawMaterialsTransfer;

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
                if (t != null && !t.Destroyed && t.def == ThingDefOf.Silver && t.stackCount > 0 && !t.IsForbidden(Faction.OfPlayer))
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
        // ATOMIC SILVER TRANSFER AT OUTPOST (WITH FULL ROLLBACK & BASELINE GUARD)
        // =====================================================================

        private sealed class SilverTransferChunk
        {
            public Pawn origPawn;
            public Thing origStack;
            public Thing chunk;
            public bool addedToOutpost;
        }

        public static bool TryTransferMissionSilverAtomically(
            Caravan caravan,
            StrategicOutpost outpost,
            int targetSilver,
            Dictionary<int, int> personalBaselineByPawn,
            out string failReason)
        {
            failReason = null;
            if (targetSilver <= 0) return true;
            if (caravan == null || caravan.Destroyed || outpost == null || !outpost.Spawned || outpost.RawMaterials == null)
            {
                failReason = "TMM_OutpostCaravan_OutpostGone".Translate();
                return false;
            }

            // 1. Determine how much silver may be taken from each pawn (protecting pre-existing personal silver)
            List<Pawn> pawns = caravan.PawnsListForReading;
            int totalEligible = 0;
            Dictionary<int, int> allowanceByPawn = new Dictionary<int, int>();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead) continue;
                int have = CountPawnInventoryThing(p, ThingDefOf.Silver);
                int keep = 0;
                if (personalBaselineByPawn != null)
                {
                    personalBaselineByPawn.TryGetValue(p.thingIDNumber, out keep);
                }
                int canTake = Mathf.Max(0, have - keep);
                if (canTake > 0)
                {
                    allowanceByPawn[p.thingIDNumber] = canTake;
                    totalEligible += canTake;
                }
            }

            if (totalEligible < targetSilver)
            {
                failReason = "StrategicOutpost_NotEnoughSilverToSend".Translate(targetSilver);
                return false;
            }

            // 2. Stage splits and transfer with strict return-value checking & full rollback
            List<SilverTransferChunk> chunks = new List<SilverTransferChunk>();
            int remaining = targetSilver;
            bool ok = true;

            try
            {
                for (int i = 0; i < pawns.Count && remaining > 0 && ok; i++)
                {
                    Pawn p = pawns[i];
                    if (p == null || p.Dead || p.inventory == null || p.inventory.innerContainer == null) continue;
                    int pawnLimit;
                    if (!allowanceByPawn.TryGetValue(p.thingIDNumber, out pawnLimit) || pawnLimit <= 0) continue;

                    List<Thing> snapshot = new List<Thing>();
                    for (int j = 0; j < p.inventory.innerContainer.Count; j++)
                    {
                        Thing t = p.inventory.innerContainer[j];
                        if (t != null && !t.Destroyed && t.def == ThingDefOf.Silver && t.stackCount > 0)
                        {
                            snapshot.Add(t);
                        }
                    }

                    for (int j = 0; j < snapshot.Count && remaining > 0 && pawnLimit > 0; j++)
                    {
                        Thing src = snapshot[j];
                        if (src == null || src.Destroyed || !p.inventory.innerContainer.Contains(src))
                        {
                            ok = false;
                            break;
                        }

                        int take = Mathf.Min(remaining, Mathf.Min(pawnLimit, src.stackCount));
                        bool whole = (take == src.stackCount);
                        Thing split = src.SplitOff(take);
                        if (split == null || split.Destroyed || split.stackCount != take)
                        {
                            if (split != null && !split.Destroyed && split != src)
                            {
                                ReturnSilverChunkToPawn(split, src, p);
                            }
                            ok = false;
                            break;
                        }

                        if (split.holdingOwner != null)
                        {
                            split.holdingOwner.Remove(split);
                        }

                        SilverTransferChunk rec = new SilverTransferChunk();
                        rec.origPawn = p;
                        rec.origStack = whole ? null : src;
                        rec.chunk = split;
                        rec.addedToOutpost = false;
                        chunks.Add(rec);

                        bool transferred = !TestRejectRawMaterialsTransfer && outpost.RawMaterials.TryAddOrTransfer(split, false);
                        if (!transferred)
                        {
                            ok = false;
                            break;
                        }

                        rec.addedToOutpost = true;
                        remaining -= take;
                        pawnLimit -= take;
                    }
                }

                if (remaining > 0) ok = false;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] OutpostCaravanLogistics: silver transfer failed, rolling back: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                for (int i = chunks.Count - 1; i >= 0; i--)
                {
                    SilverTransferChunk rec = chunks[i];
                    if (rec == null || rec.chunk == null || rec.chunk.Destroyed) continue;
                    if (rec.addedToOutpost && outpost.RawMaterials.Contains(rec.chunk))
                    {
                        outpost.RawMaterials.Remove(rec.chunk);
                    }
                    ReturnSilverChunkToPawn(rec.chunk, rec.origStack, rec.origPawn);
                }
                failReason = "TMM_OutpostCaravan_StorageTransferFailed".Translate();
                return false;
            }

            return true;
        }

        private static void ReturnSilverChunkToPawn(Thing chunk, Thing origStack, Pawn pawn)
        {
            if (chunk == null || chunk.Destroyed) return;
            if (origStack != null && !origStack.Destroyed && origStack.TryAbsorbStack(chunk, false))
            {
                return;
            }
            if (pawn != null && pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                pawn.inventory.innerContainer.TryAdd(chunk, true);
            }
        }

        // =====================================================================
        // RETURN FOOD REDISTRIBUTION BEFORE WORKER HANDOVER
        // =====================================================================

        public static void EnsureCarriersHaveReturnFood(Caravan caravan, List<Pawn> staying, List<Pawn> returning, PlanetTile tile, float returnDaysNeeded)
        {
            if (caravan == null || staying == null || returning == null || staying.Count == 0 || returning.Count == 0 || returnDaysNeeded <= 0f)
            {
                return;
            }

            float targetNutr = ComputeDailyNutrition(returning) * returnDaysNeeded * 1.15f;
            float currentCarrierNutr = SumEdibleNutritionOnPawns(returning);
            if (currentCarrierNutr >= targetNutr) return;

            int carrierIdx = 0;
            for (int i = 0; i < staying.Count && currentCarrierNutr < targetNutr; i++)
            {
                Pawn worker = staying[i];
                if (worker == null || worker.inventory == null || worker.inventory.innerContainer == null) continue;

                List<Thing> foods = new List<Thing>();
                for (int j = 0; j < worker.inventory.innerContainer.Count; j++)
                {
                    Thing t = worker.inventory.innerContainer[j];
                    if (IsCaravanFood(t)) foods.Add(t);
                }

                for (int j = 0; j < foods.Count && currentCarrierNutr < targetNutr; j++)
                {
                    Thing food = foods[j];
                    if (food == null || food.Destroyed || !worker.inventory.innerContainer.Contains(food)) continue;
                    float nutrPerUnit = food.GetStatValue(StatDefOf.Nutrition);
                    if (nutrPerUnit <= 0.01f) continue;

                    int neededUnits = Mathf.CeilToInt((targetNutr - currentCarrierNutr) / nutrPerUnit);
                    int take = Mathf.Min(neededUnits, food.stackCount);
                    if (take <= 0) continue;

                    Pawn targetCarrier = returning[carrierIdx % returning.Count];
                    carrierIdx++;
                    if (targetCarrier == null || targetCarrier.inventory == null || targetCarrier.inventory.innerContainer == null) continue;

                    Thing split = food.SplitOff(take);
                    if (split == null || split.Destroyed) continue;
                    if (split.holdingOwner != null) split.holdingOwner.Remove(split);

                    if (targetCarrier.inventory.innerContainer.TryAdd(split, true))
                    {
                        currentCarrierNutr += (float)take * nutrPerUnit;
                    }
                    else
                    {
                        worker.inventory.innerContainer.TryAdd(split, true);
                    }
                }
            }
        }

        private static float SumEdibleNutritionOnPawns(List<Pawn> pawns)
        {
            float sum = 0f;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.inventory == null || p.inventory.innerContainer == null) continue;
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (IsCaravanFood(t))
                    {
                        sum += t.GetStatValue(StatDefOf.Nutrition) * t.stackCount;
                    }
                }
            }
            return sum;
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

        /// <summary>
        /// Kept for backward API compatibility.
        /// </summary>
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

        /// <summary>
        /// Kept for backward API compatibility (assumes all pawns return).
        /// </summary>
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

            // If some pawns stay and others remain to return, ensure at least one returning pawn is a mobile carrier
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

            // Check silver strictly from formable colony items (exact same set StartFormingCaravan loads)
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

            // Collect exact TransferableOneWay food plan for BOTH outbound (all pawns) and return (returning pawns)
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

            // Check final outbound mass WITH selected food included
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

        /// <summary>
        /// Kept for backward API compatibility.
        /// </summary>
        public static bool CollectFoodForTrip(Map map, List<Pawn> pawns, float daysRoundTrip, out Dictionary<Thing, int> foodToTake, out float gatheredDays)
        {
            float half = daysRoundTrip * 0.5f;
            float outDays, retDays;
            bool ok = CollectFoodForTwoLegs(map, map != null ? map.Tile : PlanetTile.Invalid, pawns, pawns, half, half, out foodToTake, out outDays, out retDays);
            gatheredDays = outDays + retDays;
            return ok;
        }

        /// <summary>
        /// Selects exact food item quantities for outbound (allPawns) and return (returningPawns) legs,
        /// verifying EVERY leg via DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood on TransferableOneWay
        /// lists with exact CountToTransfer (never passing full un-split stacks).
        /// </summary>
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

            // Leg 1: Reserve dedicated non-perishable-first return food for returningPawns
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

            // Leg 2: Reserve outbound food for allPawns from the remaining stack amounts
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

            // Merge both legs into totalFoodToTake
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
            // Check if pawns' existing inventories already cover this leg
            float daysNow = CalcTransferableFoodDays(legPawns, legSelection, startTile);
            if (daysNow >= requiredDays) return daysNow;

            float targetNutr = ComputeDailyNutrition(legPawns) * requiredDays * 1.15f;
            float addedNutr = 0f;

            for (int i = 0; i < orderedFood.Count; i++)
            {
                Thing food = orderedFood[i];
                int avail;
                if (!remainingInStack.TryGetValue(food, out avail) || avail <= 0) continue;

                // Skip food that none of the leg's pawns can eat
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

                // If nutrition target reached but DaysWorthOfFood still short (e.g. rot or diet split), pull extra from remaining stacks
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

        /// <summary>
        /// Evaluates days worth of food strictly via TransferableOneWay with exact CountToTransfer,
        /// so leftover un-taken items in colony stacks NEVER mask a food shortage.
        /// </summary>
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
        // START CARAVAN (EXACT LORD BINDING + SINGLE FORMABLE STACK SET)
        // =====================================================================

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

            List<TransferableOneWay> transferables = new List<TransferableOneWay>();

            // 1. Add pawns & snapshot their pre-existing personal silver
            Dictionary<int, int> baselineSilver = new Dictionary<int, int>();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                baselineSilver[p.thingIDNumber] = CountPawnInventoryThing(p, ThingDefOf.Silver);
                TransferableOneWay t = new TransferableOneWay();
                t.things.Add(p);
                t.ForceTo(1);
                transferables.Add(t);
            }

            // 2. Add silver strictly from AllReachableColonyItems and VERIFY exact total queued
            if (silverAmount > 0)
            {
                List<Thing> colonyItems = GetFormableColonyItems(map);
                int remainingSilver = silverAmount;
                for (int i = 0; i < colonyItems.Count && remainingSilver > 0; i++)
                {
                    Thing s = colonyItems[i];
                    if (s == null || s.Destroyed || s.def != ThingDefOf.Silver || s.IsForbidden(Faction.OfPlayer)) continue;
                    int take = Mathf.Min(remainingSilver, s.stackCount);
                    if (take > 0)
                    {
                        TransferableOneWay t = new TransferableOneWay();
                        t.things.Add(s);
                        t.ForceTo(take);
                        transferables.Add(t);
                        remainingSilver -= take;
                    }
                }

                if (remainingSilver > 0)
                {
                    failReason = "StrategicOutpost_NotEnoughSilverToSend".Translate(silverAmount);
                    return false;
                }
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

            // 5. Snapshot existing lords, launch vanilla formation, and capture the EXACT new formation Lord ID
            HashSet<int> prevLordIds = new HashSet<int>();
            if (map.lordManager != null)
            {
                for (int i = 0; i < map.lordManager.lords.Count; i++)
                {
                    if (map.lordManager.lords[i] != null) prevLordIds.Add(map.lordManager.lords[i].loadID);
                }
            }

            CaravanFormingUtility.StartFormingCaravan(pawns, new List<Pawn>(), Faction.OfPlayer, transferables, meetingPoint, exitSpot, startTile, outpost.Tile);

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
                failReason = "TMM_OutpostCaravan_FormationFailed".Translate();
                return false;
            }

            // 6. Register mission bound 1-to-1 to this exact formation Lord
            if (OutpostCaravanManager.Instance != null)
            {
                OutpostCaravanManager.Instance.RegisterMissionBound(
                    outpost, map, pawns, stayPawns, silverAmount, map.uniqueID, formationLordId, baselineSilver);
            }

            Messages.Message("CaravanFormationProcessStarted".Translate(), pawns[0], MessageTypeDefOf.PositiveEvent, false);
            return true;
        }
    }

    // =========================================================================
    // HARMONY PATCHES: RETURN HOME GIZMO & EXACT LORD-BOUND EXIT ATTACHMENT
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

            bool showButton = (delivery != null) ||
                              (mission != null && (mission.state == OutpostMissionState.Outbound ||
                                                   (mission.state == OutpostMissionState.Returning && (mission.stranded || (__instance.pather != null && !__instance.pather.Moving)))));

            if (!showButton) yield break;

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

    /// <summary>
    /// Captures the exact LordJob_FormAndSendCaravan lordID in Prefix before pawns exit the map,
    /// then binds the newly created Caravan strictly to that formation's mission in Postfix.
    /// </summary>
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
}
