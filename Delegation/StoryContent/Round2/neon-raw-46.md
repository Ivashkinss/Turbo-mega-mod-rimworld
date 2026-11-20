`Source/StoryStage_HotelVisit.cs`

```
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public enum StoryHotelObjective
    {
        Host,
        Investigate,
        Hide
    }

    /// <summary>
    /// Quest stage that brings a visiting delegation (optionally including a registered HeroDef)
    /// to the colony's Hospitality hotel on a pinned home map.
    ///
    /// Round 2 invariants:
    /// - Hero transfer goes EXCLUSIVELY through StoryPawnTransfer. Removal from WorldPawns occurs
    ///   ONLY after the escort is generated/spawned and the visit Lord is ready. Any exception during
    ///   companion generation, spawning, or Lord attachment leaves/rolls back the hero intact in WorldPawns.
    /// - Temporary neutrality (truce) ALWAYS acquires a counted lease through StoryRelationRestorer,
    ///   even when the faction is already temporarily neutral from another active visit.
    /// - Hide launches and binds its OWN hunter raid incident (storing exact hunterRaidLordId and
    ///   confirmed hunterRaidPawnIds); unrelated hostile lords of the same faction never count.
    /// - Host counts a peaceful departure ONLY after Pawn.ExitMap prefix + postfix confirm actual
    ///   removal from the visit map under our visit/exit lord in non-hostile, non-sentAway state.
    /// - Investigate requires a genuine PlayLogEntry_Interaction (colonist -> guest of this visit).
    /// </summary>
    public sealed class StoryStage_HotelVisit : StoryStage
    {
        private const float DefaultHardOverdueDays = 2.0f;

        /// <summary>
        /// Test-only hook (active ONLY when TestMode.Enabled) allowing probes to simulate an exception
        /// during escort companion generation to verify hero remains untouched in WorldPawns.
        /// </summary>
        public static Func<PawnGenerationRequest, Pawn> TestCompanionGenerateOverride;

        /// <summary>
        /// Test-only hook (active ONLY when TestMode.Enabled) allowing probes to simulate a failed
        /// or custom Hide hunter raid execution.
        /// </summary>
        public static Func<IncidentParms, bool> TestHunterRaidExecuteOverride;

        // Cached Hospitality reflection state (resolved once safely via AccessTools)
        private static bool s_hospChecked;
        private static Type s_hospMapCompType;
        private static FieldInfo s_hospGuestsWelcomeField;
        private static PropertyInfo s_hospGuestsWelcomeProp;
        private static Type s_hospGuestBedType;
        private static Type s_hospLordJobType;
        private static ConstructorInfo s_hospLordCtor4;
        private static ConstructorInfo s_hospLordCtor3;
        private static ConstructorInfo s_hospLordCtor2;
        private static Type s_hospCompGuestType;
        private static MethodInfo s_hospCompArriveMethod;
        private static FieldInfo s_hospCompSentAwayField;
        private static PropertyInfo s_hospCompSentAwayProp;

        // Cached PlayLogEntry_Interaction reflection handles for strict initiator/recipient checks
        private static bool s_playLogChecked;
        private static FieldInfo s_playLogInitiatorField;
        private static FieldInfo s_playLogRecipientField;

        public string faction;
        public HeroDef hero;
        public int groupSize = 2;
        public float stayDays = 2f;
        public float overdueDays = DefaultHardOverdueDays;
        public StoryHotelObjective objective = StoryHotelObjective.Host;
        public string hunterFaction;
        public float hunterRaidDelayHours = -1f;
        public float hunterRaidPointsFactor = 0.85f;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "hotel"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        // =====================================================================
        // HOSPITALITY REFLECTION CAPABILITY CHECKS (CACHED)
        // =====================================================================

        private static void EnsureHospitalityReflection()
        {
            if (s_hospChecked) return;
            s_hospChecked = true;

            s_hospMapCompType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
            if (s_hospMapCompType != null)
            {
                s_hospGuestsWelcomeField = AccessTools.Field(s_hospMapCompType, "guestsAreWelcome");
                s_hospGuestsWelcomeProp = AccessTools.Property(s_hospMapCompType, "guestsAreWelcome");
            }

            s_hospGuestBedType = AccessTools.TypeByName("Hospitality.Building_GuestBed"); // CHECK API
            s_hospLordJobType = AccessTools.TypeByName("Hospitality.LordJob_VisitColony"); // CHECK API
            if (s_hospLordJobType != null)
            {
                ConstructorInfo[] ctors = s_hospLordJobType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < ctors.Length; i++)
                {
                    ParameterInfo[] p = ctors[i].GetParameters();
                    if (p.Length == 4 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3) && p[2].ParameterType == typeof(int) && p[3].ParameterType == typeof(bool))
                    {
                        s_hospLordCtor4 = ctors[i];
                    }
                    else if (p.Length == 3 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3) && p[2].ParameterType == typeof(int))
                    {
                        s_hospLordCtor3 = ctors[i];
                    }
                    else if (p.Length == 2 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3))
                    {
                        s_hospLordCtor2 = ctors[i];
                    }
                }
            }

            s_hospCompGuestType = AccessTools.TypeByName("Hospitality.CompGuest"); // CHECK API
            if (s_hospCompGuestType != null)
            {
                MethodInfo m = AccessTools.Method(s_hospCompGuestType, "Arrive"); // CHECK API
                if (m != null && m.GetParameters().Length == 0)
                {
                    s_hospCompArriveMethod = m;
                }
                s_hospCompSentAwayField = AccessTools.Field(s_hospCompGuestType, "sentAway"); // CHECK API
                s_hospCompSentAwayProp = AccessTools.Property(s_hospCompGuestType, "sentAway"); // CHECK API
            }
        }

        public static bool HasHospitalityLordSupport
        {
            get
            {
                EnsureHospitalityReflection();
                return s_hospLordCtor4 != null || s_hospLordCtor3 != null || s_hospLordCtor2 != null;
            }
        }

        public static bool HasOpenHotel(Map map)
        {
            EnsureHospitalityReflection();
            if (map == null || s_hospMapCompType == null) return false;
            if (s_hospGuestsWelcomeField == null && s_hospGuestsWelcomeProp == null) return false;

            MapComponent comp = HotelVisitGuarantee.HospitalityOf(map);
            if (comp == null) return false;

            try
            {
                object welcomeObj = s_hospGuestsWelcomeField != null
                    ? s_hospGuestsWelcomeField.GetValue(comp)
                    : s_hospGuestsWelcomeProp.GetValue(comp, null);
                if (!(welcomeObj is bool) || !(bool)welcomeObj) return false;
            }
            catch
            {
                return false;
            }

            return FindGuestBedCell(map) != IntVec3.Invalid;
        }

        // =====================================================================
        // STAGE LIFECYCLE: START
        // =====================================================================

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            EnsureHospitalityReflection();

            Map home = ctx.Home;
            int now = Find.TickManager.TicksGame;
            int stayTicks = Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(stayDays * GenDate.TicksPerDay));
            int overdueTicks = Mathf.Max(GenDate.TicksPerHour * 6, Mathf.RoundToInt(Mathf.Max(0.25f, overdueDays) * GenDate.TicksPerDay));
            int leaveTick = now + stayTicks;
            int hardDeadlineTick = leaveTick + overdueTicks;

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["leaveTick"] = leaveTick.ToString();
            ctx.run.vars["hardDeadlineTick"] = hardDeadlineTick.ToString();
            ctx.run.vars["investigated"] = "false";
            ctx.run.vars["hunterRaidLaunched"] = "false";
            ctx.run.vars["hunterRaidBound"] = "false";
            ctx.run.vars["hunterRaidCleared"] = "false";
            ctx.run.vars["hunterRaidLordId"] = "-1";
            ctx.run.vars["hunterRaidPawnIds"] = string.Empty;
            ctx.run.vars["departureTriggered"] = "false";
            ctx.run.vars["arrived"] = "false";
            ctx.run.vars["heroIncluded"] = "false";
            ctx.run.vars["heroSpawned"] = "false";
            ctx.run.vars["visitLordId"] = "-1";
            ctx.run.vars["exitLordId"] = "-1";
            ctx.run.vars["visitMapId"] = home != null ? home.uniqueID.ToString() : "-1";
            ctx.run.vars["visitMode"] = HasHospitalityLordSupport ? "hospitality" : "vanilla_fallback";

            // 1. Immediate noHotel detection on Start (zero side effects)
            if (!HasOpenHotel(home))
            {
                ctx.run.vars["noHotel"] = "true";
                ctx.run.vars["immediateOutcome"] = "noHotel";
                SendNoHotelLetter(ctx, home);
                return;
            }

            // 2. For Hide objective, validate hunterFaction & schedule stage's own hunter raid BEFORE spawning
            Faction hunterFac = null;
            if (objective == StoryHotelObjective.Hide)
            {
                hunterFac = !hunterFaction.NullOrEmpty() ? StoryUtil.FactionOf(hunterFaction) : null;
                if (hunterFac == null || hunterFac.defeated || hunterFac.IsPlayer)
                {
                    Log.Warning("[TMM] StoryStage_HotelVisit: Hide objective failed to resolve hunterFaction '" + (hunterFaction ?? "null") + "'.");
                    ctx.run.vars["failReason"] = "hunter_faction_missing";
                    ctx.run.vars["immediateOutcome"] = FindOutcome("unavailable") != null ? "unavailable" : "failed";
                    return;
                }

                int raidDelayTicks;
                if (hunterRaidDelayHours >= 0f)
                {
                    raidDelayTicks = Mathf.RoundToInt(hunterRaidDelayHours * GenDate.TicksPerHour);
                }
                else
                {
                    raidDelayTicks = Mathf.Clamp(stayTicks / 2, GenDate.TicksPerHour, Mathf.Max(GenDate.TicksPerHour, stayTicks - GenDate.TicksPerHour));
                }

                ctx.run.vars["hunterFactionId"] = hunterFac.loadID.ToString();
                ctx.run.vars["hunterRaidTick"] = (now + raidDelayTicks).ToString();
            }

            // 3. Resolve visit faction
            Faction visitFac = ResolveVisitFaction(ctx);
            if (visitFac == null)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: no valid visit faction found for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                ctx.run.vars["failReason"] = "no_visit_faction";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            ctx.run.vars["visitFactionId"] = visitFac.loadID.ToString();

            // 4. ALWAYS request truce lease through StoryRelationRestorer (even if faction currently
            // appears neutral due to another overlapping visit's temporary truce backup!).
            StoryRelationRestorer restorer = StoryRelationRestorer.Instance;
            if (visitFac.HostileTo(Faction.OfPlayer) && !StoryRelationRestorer.CanFactionEnterTruce(visitFac))
            {
                ctx.run.vars["failReason"] = (visitFac.def != null && visitFac.def.permanentEnemy)
                    ? "permanent_enemy_cannot_truce"
                    : "truce_not_allowed";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            string truceLeaseId = null;
            if (restorer != null)
            {
                if (!restorer.AcquireTruceLease(visitFac, ctx.run, out truceLeaseId) || visitFac.HostileTo(Faction.OfPlayer))
                {
                    if (!truceLeaseId.NullOrEmpty())
                    {
                        restorer.AbortLease(truceLeaseId);
                    }
                    ctx.run.vars["failReason"] = "truce_failed";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }
                if (!truceLeaseId.NullOrEmpty())
                {
                    ctx.run.vars["truceLeaseId"] = truceLeaseId;
                }
            }
            else if (visitFac.HostileTo(Faction.OfPlayer))
            {
                ctx.run.vars["failReason"] = "truce_restorer_missing";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            // 5. Read-only pre-check of hero availability (NEVER mutates WorldPawns or Faction yet!)
            Pawn heroCandidate = null;
            if (hero != null && HeroRegistry.Instance != null)
            {
                Pawn candidate = HeroRegistry.Instance.GetOrCreate(hero);
                string skipReason;
                if (StoryPawnTransfer.IsFreeWorldPawn(candidate, visitFac, out skipReason))
                {
                    heroCandidate = candidate;
                }
                else
                {
                    ctx.run.vars["heroIncluded"] = "false";
                    ctx.run.vars["heroSkipReason"] = skipReason ?? "unavailable";
                }
            }

            int targetCount = Mathf.Clamp(groupSize, 1, 8);
            int companionCount = heroCandidate != null ? Mathf.Max(0, targetCount - 1) : targetCount;

            IntVec3 entryCell;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out entryCell, home, CellFinder.EdgeRoadChance_Friendly)) // CHECK API
            {
                entryCell = CellFinder.RandomEdgeCell(home);
            }

            IntVec3 chillSpot = FindGuestBedCell(home);
            if (chillSpot == IntVec3.Invalid) chillSpot = home.Center;

            // 6. Generate companions, spawn them, and create the visit Lord BEFORE removing hero from WorldPawns.
            List<Pawn> generatedCompanions = new List<Pawn>();
            List<Pawn> spawnedCompanions = new List<Pawn>();
            Lord visitLord = null;
            string usedMode = "vanilla_fallback";
            PawnTransferHandle heroHandle = null;

            try
            {
                // 6A. Generate all accompanying regular guests
                for (int i = 0; i < companionCount; i++)
                {
                    PawnKindDef kind = visitFac.RandomPawnKind();
                    if (kind == null) kind = PawnKindDefOf.SpaceRefugee;
                    PawnGenerationRequest req = new PawnGenerationRequest(
                        kind, visitFac, PawnGenerationContext.NonPlayer, home.Tile,
                        false, false, false, true, false, 1f, false, true, false, true, true,
                        false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null,
                        null, null, null, null, null, null, null,
                        false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult,
                        null, null, null, false); // CHECK API

                    Pawn guest = GenerateCompanionPawn(req);
                    if (guest != null)
                    {
                        generatedCompanions.Add(guest);
                    }
                }

                // 6B. Spawn companions on home map
                for (int i = 0; i < generatedCompanions.Count; i++)
                {
                    Pawn p = generatedCompanions[i];
                    IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(entryCell, home, 6, null);
                    Thing res = GenSpawn.Spawn(p, spawnCell, home, WipeMode.Vanish);
                    if (res == p && p.Spawned && p.Map == home)
                    {
                        spawnedCompanions.Add(p);
                    }
                    else
                    {
                        if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                        if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
                    }
                }

                // 6C. Create the visit Lord BEFORE touching the hero (startingPawns may be empty if hero is solo guest)
                visitLord = AttachVisitLord(home, visitFac, chillSpot, stayTicks, spawnedCompanions, out usedMode);
                if (visitLord == null)
                {
                    CleanupCompanionsAndLord(home, visitLord, generatedCompanions, spawnedCompanions);
                    AbortTruceLease(ctx);
                    ctx.run.vars["failReason"] = "lord_creation_failed";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }

                // 6D. NOW transfer the hero via StoryPawnTransfer (removes from WorldPawns last, right before GenSpawn,
                //     and attaches hero to the already-ready visitLord inside postSpawnSetup).
                bool heroTransferred = false;
                if (heroCandidate != null)
                {
                    IntVec3 heroCell = CellFinder.RandomClosewalkCellNear(entryCell, home, 6, null);
                    bool needSetFaction = (heroCandidate.Faction == null);
                    string transferFail;
                    Lord capturedLord = visitLord;

                    heroTransferred = StoryPawnTransfer.TrySpawnFreeWorldPawn(
                        heroCandidate,
                        home,
                        heroCell,
                        visitFac,
                        needSetFaction,
                        null,
                        visitFac,
                        delegate(Pawn spawnedHero)
                        {
                            capturedLord.AddPawn(spawnedHero);
                            return spawnedHero.GetLord() == capturedLord;
                        },
                        false,
                        out heroHandle,
                        out transferFail);

                    if (!heroTransferred)
                    {
                        ctx.run.vars["heroIncluded"] = "false";
                        ctx.run.vars["heroSkipReason"] = transferFail ?? "transfer_failed";
                    }
                }

                // Build final confirmed guest list
                List<Pawn> finalGuests = new List<Pawn>();
                List<int> guestIdNums = new List<int>();
                if (heroTransferred && heroHandle != null && heroHandle.Pawn != null)
                {
                    finalGuests.Add(heroHandle.Pawn);
                    guestIdNums.Add(heroHandle.Pawn.thingIDNumber);
                }
                for (int i = 0; i < spawnedCompanions.Count; i++)
                {
                    finalGuests.Add(spawnedCompanions[i]);
                    guestIdNums.Add(spawnedCompanions[i].thingIDNumber);
                }

                if (finalGuests.Count == 0)
                {
                    if (heroHandle != null) heroHandle.Rollback();
                    CleanupCompanionsAndLord(home, visitLord, generatedCompanions, spawnedCompanions);
                    AbortTruceLease(ctx);
                    ctx.run.vars["failReason"] = "no_guests_spawned";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }

                if (usedMode == "hospitality")
                {
                    for (int i = 0; i < finalGuests.Count; i++)
                    {
                        TryNotifyHospitalityCompArrive(finalGuests[i]);
                    }
                }

                if (heroHandle != null)
                {
                    heroHandle.FinalizeTransfer();
                    ctx.run.vars["heroThingId"] = heroHandle.Pawn.ThingID;
                    ctx.run.vars["heroIdNum"] = heroHandle.Pawn.thingIDNumber.ToString();
                    ctx.run.vars["heroIncluded"] = "true";
                    ctx.run.vars["heroSpawned"] = "true";
                }

                ctx.run.vars["visitMode"] = usedMode;
                ctx.run.vars["visitLordId"] = visitLord.loadID.ToString();
                ctx.run.vars["guestIds"] = JoinInts(guestIdNums);
                ctx.run.vars["arrived"] = "true";

                if (restorer != null)
                {
                    restorer.RegisterHotelGuests(ctx.run, home.uniqueID, visitLord.loadID, guestIdNums);
                }

                string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Hotel Guests Arrived"), ctx);
                string body = StoryUtil.Format(!text.NullOrEmpty() ? text : Objective(ctx), ctx);
                LookTargets target = (heroHandle != null && heroHandle.Pawn != null) ? new LookTargets(heroHandle.Pawn) : new LookTargets(finalGuests[0]);
                Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, target); // CHECK API
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] StoryStage_HotelVisit: exception during Start, rolling back completely: " + ex);
                if (heroHandle != null) heroHandle.Rollback();
                CleanupCompanionsAndLord(home, visitLord, generatedCompanions, spawnedCompanions);
                AbortTruceLease(ctx);
                ctx.run.vars["failReason"] = "start_exception";
                ctx.run.vars["immediateOutcome"] = "failed";
            }
        }

        private static Pawn GenerateCompanionPawn(PawnGenerationRequest req)
        {
            if (TestMode.Enabled && TestCompanionGenerateOverride != null)
            {
                return TestCompanionGenerateOverride(req);
            }
            return PawnGenerator.GeneratePawn(req);
        }

        private static void CleanupCompanionsAndLord(Map home, Lord visitLord, List<Pawn> generatedCompanions, List<Pawn> spawnedCompanions)
        {
            if (visitLord != null && home != null && home.lordManager != null)
            {
                try
                {
                    if (home.lordManager.lords.Contains(visitLord))
                    {
                        home.lordManager.RemoveLord(visitLord);
                    }
                }
                catch
                {
                }
            }

            HashSet<Pawn> toDestroy = new HashSet<Pawn>();
            if (generatedCompanions != null)
            {
                for (int i = 0; i < generatedCompanions.Count; i++)
                {
                    if (generatedCompanions[i] != null) toDestroy.Add(generatedCompanions[i]);
                }
            }
            if (spawnedCompanions != null)
            {
                for (int i = 0; i < spawnedCompanions.Count; i++)
                {
                    if (spawnedCompanions[i] != null) toDestroy.Add(spawnedCompanions[i]);
                }
            }

            foreach (Pawn p in toDestroy)
            {
                try
                {
                    if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                    if (Find.WorldPawns != null && Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
                    if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                }
            }
        }

        // =====================================================================
        // STAGE LIFECYCLE: TICK (SINGLE-PASS PER-GUEST CLASSIFICATION)
        // =====================================================================

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            string immediate = GetStr(ctx, "immediateOutcome");
            if (!immediate.NullOrEmpty())
            {
                return Finish(ctx, immediate);
            }

            if (GetBool(ctx, "noHotel"))
            {
                return Finish(ctx, "noHotel");
            }

            int visitMapId = GetInt(ctx, "visitMapId", -1);
            Map visitMap = FindMapByUniqueId(visitMapId);
            if (visitMap == null)
            {
                ctx.run.vars["failReason"] = "visit_map_lost";
                return Finish(ctx, "failed");
            }

            int now = Find.TickManager.TicksGame;
            int startTick = GetInt(ctx, "startTick", now);
            int leaveTick = GetInt(ctx, "leaveTick", now + GenDate.TicksPerDay);
            int hardDeadlineTick;
            if (!TryGetInt(ctx, "hardDeadlineTick", out hardDeadlineTick))
            {
                hardDeadlineTick = leaveTick + Mathf.Max(GenDate.TicksPerHour * 6, Mathf.RoundToInt(Mathf.Max(0.25f, overdueDays) * GenDate.TicksPerDay));
                ctx.run.vars["hardDeadlineTick"] = hardDeadlineTick.ToString();
            }

            int visitFacId = GetInt(ctx, "visitFactionId", -1);
            Faction visitFac = FindFactionById(visitFacId);
            if (visitFac == null || visitFac.HostileTo(Faction.OfPlayer))
            {
                ctx.run.vars["failReason"] = "faction_became_hostile";
                return Finish(ctx, "failed");
            }

            List<int> guestIds = ParseInts(GetStr(ctx, "guestIds"));
            if (guestIds.Count == 0)
            {
                ctx.run.vars["failReason"] = "empty_guest_list";
                return Finish(ctx, "failed");
            }

            int visitLordId = GetInt(ctx, "visitLordId", -1);
            int exitLordId = GetInt(ctx, "exitLordId", -1);
            string runKey = StoryRelationRestorer.MakeLeaseId(ctx.run);

            HashSet<int> guestIdSet = new HashSet<int>(guestIds);
            Dictionary<int, Pawn> spawnedMap = new Dictionary<int, Pawn>();
            HashSet<int> deadOnMap = new HashSet<int>();
            Dictionary<int, Pawn> heldOnMap = new Dictionary<int, Pawn>();
            CollectMapGuestSnapshot(visitMap, guestIdSet, spawnedMap, deadOnMap, heldOnMap);

            List<Pawn> onMapGuests = new List<Pawn>();
            int peacefulExitCount = 0;
            StringBuilder stateLog = new StringBuilder();

            for (int i = 0; i < guestIds.Count; i++)
            {
                int idNum = guestIds[i];
                if (i > 0) stateLog.Append(',');
                stateLog.Append(idNum).Append(':');

                if (deadOnMap.Contains(idNum))
                {
                    stateLog.Append("killed");
                    ctx.run.vars["guestStates"] = stateLog.ToString();
                    ctx.run.vars["failReason"] = "guest_killed";
                    return Finish(ctx, "failed");
                }

                Pawn spawned;
                if (spawnedMap.TryGetValue(idNum, out spawned))
                {
                    if (spawned.Dead)
                    {
                        stateLog.Append("killed");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_killed";
                        return Finish(ctx, "failed");
                    }
                    if (spawned.IsPrisonerOfColony || spawned.IsSlaveOfColony)
                    {
                        stateLog.Append("imprisoned");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_imprisoned";
                        return Finish(ctx, "failed");
                    }
                    if (spawned.HostileTo(Faction.OfPlayer))
                    {
                        stateLog.Append("hostile");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_hostile";
                        return Finish(ctx, "failed");
                    }
                    if (IsSentAwayByHospitality(spawned))
                    {
                        stateLog.Append("sent_away");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_sent_away";
                        return Finish(ctx, "failed");
                    }

                    stateLog.Append("on_map");
                    onMapGuests.Add(spawned);
                    continue;
                }

                Pawn held;
                if (heldOnMap.TryGetValue(idNum, out held))
                {
                    if (held.Dead || held.IsPrisonerOfColony || held.IsSlaveOfColony || held.HostileTo(Faction.OfPlayer))
                    {
                        stateLog.Append("bad_holder");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_captured_or_dead_in_holder";
                        return Finish(ctx, "failed");
                    }
                    stateLog.Append("in_holder");
                    ctx.run.vars["guestStates"] = stateLog.ToString();
                    ctx.run.vars["failReason"] = "guest_missing_in_holder";
                    return Finish(ctx, "failed");
                }

                // Not on visitMap: MUST have confirmed peaceful exit via Prefix+Postfix Pawn.ExitMap
                StoryTrackedGuestRecord rec = StoryRelationRestorer.Instance != null
                    ? StoryRelationRestorer.Instance.FindTrackedGuest(runKey, idNum)
                    : null;

                if (rec != null && rec.exitedHostileOrForced)
                {
                    stateLog.Append("exited_hostile");
                    ctx.run.vars["guestStates"] = stateLog.ToString();
                    ctx.run.vars["failReason"] = "guest_exited_hostile_or_sent_away";
                    return Finish(ctx, "failed");
                }

                if (rec != null && rec.exitedPeacefully)
                {
                    Pawn worldPawn = FindWorldPawnByIdNum(idNum);
                    if (worldPawn != null && (worldPawn.Dead || worldPawn.HostileTo(Faction.OfPlayer)))
                    {
                        stateLog.Append("exited_bad_world");
                        ctx.run.vars["guestStates"] = stateLog.ToString();
                        ctx.run.vars["failReason"] = "guest_dead_or_hostile_in_world";
                        return Finish(ctx, "failed");
                    }
                    stateLog.Append("exited_peaceful");
                    peacefulExitCount++;
                    continue;
                }

                stateLog.Append("missing");
                ctx.run.vars["guestStates"] = stateLog.ToString();
                ctx.run.vars["failReason"] = "guest_missing";
                return Finish(ctx, "failed");
            }

            ctx.run.vars["guestStates"] = stateLog.ToString();

            // ---------------- OBJECTIVE TRACKING ----------------

            if (objective == StoryHotelObjective.Investigate && !GetBool(ctx, "investigated") && onMapGuests.Count > 0)
            {
                int interactionTick;
                if (HasRealColonistGuestInteraction(guestIdSet, startTick, out interactionTick))
                {
                    ctx.run.vars["investigated"] = "true";
                    ctx.run.vars["investigatedTick"] = interactionTick.ToString();
                }
            }

            if (objective == StoryHotelObjective.Hide)
            {
                UpdateHideHunterRaidTracking(ctx, visitMap, now, hardDeadlineTick);
            }

            // ---------------- DEPARTURE & LORD HEALTH ----------------

            bool visitLordAlive = FindLordById(visitMap, visitLordId) != null;
            bool exitLordAlive = exitLordId >= 0 && FindLordById(visitMap, exitLordId) != null;

            if (onMapGuests.Count > 0)
            {
                if (now >= hardDeadlineTick)
                {
                    if (objective == StoryHotelObjective.Hide && !GetBool(ctx, "hunterRaidCleared"))
                    {
                        ctx.run.vars["deadlineReached"] = "true";
                        ctx.run.vars["failReason"] = GetBool(ctx, "hunterRaidBound")
                            ? "hide_raid_not_cleared_deadline"
                            : "hide_no_raid_deadline";
                        return Finish(ctx, FindOutcome("timeout") != null ? "timeout" : "failed");
                    }

                    ctx.run.vars["deadlineReached"] = "true";
                    ctx.run.vars["failReason"] = "lord_overdue_deadline";
                    return Finish(ctx, "failed");
                }

                if (!visitLordAlive && !exitLordAlive)
                {
                    ctx.run.vars["failReason"] = "lord_missing";
                    return Finish(ctx, "failed");
                }

                if (now >= leaveTick && !GetBool(ctx, "departureTriggered"))
                {
                    bool canDepart = (objective != StoryHotelObjective.Hide) || GetBool(ctx, "hunterRaidCleared");
                    if (canDepart)
                    {
                        Lord exitLord = TriggerGracefulDeparture(visitMap, visitFac, onMapGuests);
                        if (exitLord != null)
                        {
                            ctx.run.vars["exitLordId"] = exitLord.loadID.ToString();
                            if (StoryRelationRestorer.Instance != null)
                            {
                                StoryRelationRestorer.Instance.SetHotelExitLordId(runKey, exitLord.loadID);
                            }
                        }
                        ctx.run.vars["departureTriggered"] = "true";
                    }
                }

                return null;
            }

            // ---------------- ALL GUESTS CONFIRMED OFF MAP ----------------
            if (peacefulExitCount < guestIds.Count)
            {
                ctx.run.vars["failReason"] = "incomplete_peaceful_exit";
                return Finish(ctx, "failed");
            }

            if (objective == StoryHotelObjective.Investigate)
            {
                if (!GetBool(ctx, "investigated"))
                {
                    ctx.run.vars["failReason"] = "not_investigated";
                    return Finish(ctx, "failed");
                }
                return Finish(ctx, "success");
            }

            if (objective == StoryHotelObjective.Hide)
            {
                if (!GetBool(ctx, "hunterRaidBound") || !GetBool(ctx, "hunterRaidCleared"))
                {
                    ctx.run.vars["failReason"] = "hide_raid_not_completed";
                    return Finish(ctx, "failed");
                }
                return Finish(ctx, "success");
            }

            return Finish(ctx, "success");
        }

        public override string Objective(StoryContext ctx)
        {
            if (ctx != null && ctx.run != null && GetBool(ctx, "noHotel"))
            {
                return "Open a hotel (build guest beds and enable welcoming guests)";
            }

            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : text, ctx);
            if (baseText.NullOrEmpty())
            {
                if (objective == StoryHotelObjective.Investigate) baseText = "Investigate visiting hotel guests";
                else if (objective == StoryHotelObjective.Hide) baseText = "Shelter hidden guests from hunters";
                else baseText = "Host visiting delegation in the hotel";
            }

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int leaveTick = GetInt(ctx, "leaveTick", now);
            float daysLeft = Mathf.Max(0f, (leaveTick - now) / (float)GenDate.TicksPerDay);

            if (objective == StoryHotelObjective.Investigate)
            {
                string status = GetBool(ctx, "investigated") ? "clue found" : "talk to guest";
                return string.Format("{0} ({1}, {2:0.0}d left)", baseText, status, daysLeft);
            }

            if (objective == StoryHotelObjective.Hide)
            {
                string status = GetBool(ctx, "hunterRaidCleared")
                    ? "hunters repelled"
                    : (GetBool(ctx, "hunterRaidBound") ? "repel hunters" : "awaiting hunters");
                return string.Format("{0} ({1}, {2:0.0}d left)", baseText, status, daysLeft);
            }

            return string.Format("{0} ({1:0.0}d left)", baseText, daysLeft);
        }

        // =====================================================================
        // UNIFIED FINISH & CLEANUP
        // =====================================================================

        private string Finish(StoryContext ctx, string outcome)
        {
            ReleaseTruceLease(ctx);
            ReleaseHunterHostilityLease(ctx, false);
            if (ctx != null && ctx.run != null)
            {
                string runKey = StoryRelationRestorer.MakeLeaseId(ctx.run);
                if (StoryRelationRestorer.Instance != null)
                {
                    StoryRelationRestorer.Instance.ClearHotelGuests(runKey);
                }
                ctx.run.vars["finished"] = outcome;
            }
            return outcome;
        }

        private static void ReleaseTruceLease(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;
            string leaseId = GetStr(ctx, "truceLeaseId");
            if (!leaseId.NullOrEmpty() && StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.ReleaseLease(leaseId);
                ctx.run.vars.Remove("truceLeaseId");
            }
        }

        private static void AbortTruceLease(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;
            string leaseId = GetStr(ctx, "truceLeaseId");
            if (!leaseId.NullOrEmpty() && StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.AbortLease(leaseId);
                ctx.run.vars.Remove("truceLeaseId");
            }
        }

        private static void ReleaseHunterHostilityLease(StoryContext ctx, bool aborted)
        {
            if (ctx == null || ctx.run == null) return;
            string leaseId = GetStr(ctx, "hunterHostilityLeaseId");
            if (!leaseId.NullOrEmpty() && StoryRelationRestorer.Instance != null)
            {
                if (aborted) StoryRelationRestorer.Instance.AbortLease(leaseId);
                else StoryRelationRestorer.Instance.ReleaseLease(leaseId);
                ctx.run.vars.Remove("hunterHostilityLeaseId");
            }
        }

        // =====================================================================
        // STRICT INVESTIGATE (PlayLogEntry_Interaction: COLONIST -> THIS GUEST)
        // =====================================================================

        private static bool HasRealColonistGuestInteraction(HashSet<int> guestIdSet, int startTick, out int foundTick)
        {
            foundTick = -1;
            if (guestIdSet == null || guestIdSet.Count == 0 || Find.PlayLog == null || Find.PlayLog.AllEntries == null)
            {
                return false;
            }

            if (!s_playLogChecked)
            {
                s_playLogChecked = true;
                s_playLogInitiatorField = AccessTools.Field(typeof(PlayLogEntry_Interaction), "initiator");
                s_playLogRecipientField = AccessTools.Field(typeof(PlayLogEntry_Interaction), "recipient");
            }

            if (s_playLogInitiatorField == null || s_playLogRecipientField == null)
            {
                return false;
            }

            List<LogEntry> entries = Find.PlayLog.AllEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                LogEntry entry = entries[i];
                if (entry == null || entry.Tick < startTick) continue;

                PlayLogEntry_Interaction inter = entry as PlayLogEntry_Interaction;
                if (inter == null) continue;

                Pawn initiator = s_playLogInitiatorField.GetValue(inter) as Pawn;
                Pawn recipient = s_playLogRecipientField.GetValue(inter) as Pawn;
                if (initiator == null || recipient == null) continue;

                bool initiatorIsColonist = initiator.Faction == Faction.OfPlayer && initiator.IsColonist && !initiator.IsPrisoner;
                bool recipientIsVisitGuest = guestIdSet.Contains(recipient.thingIDNumber);

                if (initiatorIsColonist && recipientIsVisitGuest)
                {
                    foundTick = entry.Tick;
                    return true;
                }
            }

            return false;
        }

        // =====================================================================
        // STRICT HIDE OBJECTIVE (STAGE-OWNED HUNTER RAID INCIDENT & COHORT)
        // =====================================================================

        /// <summary>
        /// Hide launches its OWN RaidEnemy incident at hunterRaidTick and records ONLY the
        /// resulting lord ID and confirmed raider pawn cohort. Unrelated hostile lords of the
        /// same hunterFaction on the map are NEVER bound or counted as the Hide ambush.
        /// </summary>
        private void UpdateHideHunterRaidTracking(StoryContext ctx, Map visitMap, int now, int hardDeadlineTick)
        {
            if (GetBool(ctx, "hunterRaidCleared")) return;

            int hunterFacId = GetInt(ctx, "hunterFactionId", -1);
            Faction hunterFac = FindFactionById(hunterFacId);
            if (hunterFac == null || hunterFac.defeated) return;

            // Step 1: Launch our own bound hunter raid once hunterRaidTick is reached (before hard deadline)
            if (!GetBool(ctx, "hunterRaidLaunched"))
            {
                int raidTick = GetInt(ctx, "hunterRaidTick", now);
                if (now < raidTick || now >= hardDeadlineTick) return;

                ctx.run.vars["hunterRaidLaunched"] = "true";
                TryLaunchStageHunterRaid(ctx, visitMap, hunterFac);
                return;
            }

            // Step 2: Only track the stage's own bound cohort (non-empty confirmed raiders)
            if (!GetBool(ctx, "hunterRaidBound")) return;

            int boundLordId = GetInt(ctx, "hunterRaidLordId", -1);
            HashSet<int> boundPawnIds = new HashSet<int>(ParseInts(GetStr(ctx, "hunterRaidPawnIds")));
            if (boundPawnIds.Count == 0) return;

            if (IsBoundHunterRaidCleared(visitMap, boundLordId, boundPawnIds))
            {
                ctx.run.vars["hunterRaidCleared"] = "true";
                ReleaseHunterHostilityLease(ctx, false);
            }
        }

        private bool TryLaunchStageHunterRaid(StoryContext ctx, Map visitMap, Faction hunterFac)
        {
            if (visitMap == null || hunterFac == null) return false;

            int visitFacId = GetInt(ctx, "visitFactionId", -1);
            if (hunterFac.loadID == visitFacId)
            {
                // Cannot turn the guests' own faction hostile mid-visit
                return false;
            }

            // If hunterFac is not hostile, request a temporary hostility lease
            if (!hunterFac.HostileTo(Faction.OfPlayer))
            {
                StoryRelationRestorer restorer = StoryRelationRestorer.Instance;
                string hLeaseId = null;
                if (restorer == null || !restorer.AcquireHostilityLease(hunterFac, ctx.run, true, out hLeaseId))
                {
                    if (restorer != null && !hLeaseId.NullOrEmpty()) restorer.AbortLease(hLeaseId);
                    return false;
                }
                if (!hLeaseId.NullOrEmpty())
                {
                    ctx.run.vars["hunterHostilityLeaseId"] = hLeaseId;
                }
            }

            HashSet<int> prevPawns = SnapshotAllFactionPawnsOnMap(visitMap, hunterFac);
            HashSet<int> prevLords = new HashSet<int>(SnapshotFactionLords(visitMap, hunterFac));

            float rawPoints = StorytellerUtility.DefaultThreatPointsNow(visitMap) * Mathf.Max(0.2f, hunterRaidPointsFactor);
            float minPoints = 150f;
            if (hunterFac.def != null)
            {
                float facMin = hunterFac.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null); // CHECK API
                if (facMin > minPoints) minPoints = facMin;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, visitMap);
            parms.target = visitMap;
            parms.faction = hunterFac;
            parms.points = Mathf.Max(minPoints, rawPoints);
            parms.forced = true;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;

            bool executed = false;
            try
            {
                executed = ExecuteHunterRaidIncident(parms);
                if (!executed)
                {
                    parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                    executed = ExecuteHunterRaidIncident(parms);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] StoryStage_HotelVisit: Hide hunter raid threw exception: " + ex);
                ReleaseHunterHostilityLease(ctx, true);
                return false;
            }

            if (!executed)
            {
                ReleaseHunterHostilityLease(ctx, true);
                return false;
            }

            // Capture ONLY the lord and confirmed raider cohort spawned by THIS TryExecute call
            int boundLordId = -1;
            List<int> cohortIds = new List<int>();
            HashSet<int> cohortUnique = new HashSet<int>();

            if (visitMap.lordManager != null)
            {
                List<Lord> lords = visitMap.lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    Lord l = lords[i];
                    if (l == null || l.faction != hunterFac || prevLords.Contains(l.loadID)) continue;
                    boundLordId = l.loadID;
                    if (l.ownedPawns != null)
                    {
                        for (int pIdx = 0; pIdx < l.ownedPawns.Count; pIdx++)
                        {
                            Pawn hp = l.ownedPawns[pIdx];
                            if (hp != null && cohortUnique.Add(hp.thingIDNumber))
                            {
                                cohortIds.Add(hp.thingIDNumber);
                            }
                        }
                    }
                    break;
                }
            }

            HashSet<int> afterPawns = SnapshotAllFactionPawnsOnMap(visitMap, hunterFac);
            foreach (int id in afterPawns)
            {
                if (!prevPawns.Contains(id) && cohortUnique.Add(id))
                {
                    cohortIds.Add(id);
                }
            }

            if (cohortIds.Count == 0)
            {
                ReleaseHunterHostilityLease(ctx, true);
                return false;
            }

            ctx.run.vars["hunterRaidBound"] = "true";
            ctx.run.vars["hunterRaidLordId"] = boundLordId.ToString();
            ctx.run.vars["hunterRaidPawnIds"] = JoinInts(cohortIds);
            return true;
        }

        private static bool ExecuteHunterRaidIncident(IncidentParms parms)
        {
            if (TestMode.Enabled && TestHunterRaidExecuteOverride != null)
            {
                return TestHunterRaidExecuteOverride(parms);
            }
            return IncidentDefOf.RaidEnemy.Worker.TryExecute(parms); // CHECK API
        }

        private static bool IsBoundHunterRaidCleared(Map map, int boundLordId, HashSet<int> boundPawnIds)
        {
            if (map == null || boundPawnIds == null || boundPawnIds.Count == 0) return false;

            if (boundLordId >= 0 && map.lordManager != null)
            {
                Lord l = FindLordById(map, boundLordId);
                if (l != null && l.ownedPawns != null)
                {
                    for (int i = 0; i < l.ownedPawns.Count; i++)
                    {
                        if (IsActiveRaiderOnMap(l.ownedPawns[i], map)) return false;
                    }
                }
            }

            if (map.mapPawns != null)
            {
                IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawned.Count; i++)
                {
                    Pawn p = spawned[i];
                    if (p != null && boundPawnIds.Contains(p.thingIDNumber) && IsActiveRaiderOnMap(p, map))
                    {
                        return false;
                    }
                }
            }

            if (map.listerThings != null)
            {
                List<Thing> holders = map.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    Thing h = holders[i];
                    if (h == null || h.Destroyed || h is Pawn) continue;
                    if (ThingHasTrackedActiveRaider(h, boundPawnIds)) return false;
                }
            }

            return true;
        }

        private static bool IsActiveRaiderOnMap(Pawn p, Map map)
        {
            if (p == null || p.Dead || p.Destroyed || p.Downed) return false;
            if (p.IsPrisonerOfColony || p.IsSlaveOfColony) return false;
            if (!p.Spawned && p.MapHeld == map) return true;
            if (!p.Spawned || p.Map != map) return false;
            if (p.mindState != null && p.mindState.duty != null)
            {
                DutyDef d = p.mindState.duty.def;
                if (d == DutyDefOf.ExitMapBest || d == DutyDefOf.ExitMapBestAndDefendSelf || d == DutyDefOf.ExitMapRandom)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool ThingHasTrackedActiveRaider(Thing h, HashSet<int> trackedIds)
        {
            IThingHolder direct = h as IThingHolder;
            if (direct != null && HolderHasTrackedActiveRaider(direct, trackedIds)) return true;

            ThingWithComps twc = h as ThingWithComps;
            if (twc != null && twc.AllComps != null)
            {
                for (int i = 0; i < twc.AllComps.Count; i++)
                {
                    IThingHolder ch = twc.AllComps[i] as IThingHolder;
                    if (ch != null && HolderHasTrackedActiveRaider(ch, trackedIds)) return true;
                }
            }
            return false;
        }

        private static bool HolderHasTrackedActiveRaider(IThingHolder holder, HashSet<int> trackedIds)
        {
            if (holder == null) return false;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn p = owner[i] as Pawn;
                    if (p != null && trackedIds.Contains(p.thingIDNumber) && !p.Dead && !p.Destroyed &&
                        !p.IsPrisonerOfColony && !p.IsSlaveOfColony)
                    {
                        return true;
                    }
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                if (HolderHasTrackedActiveRaider(children[i], trackedIds)) return true;
            }
            return false;
        }

        // =====================================================================
        // HOSPITALITY & LORD HELPERS
        // =====================================================================

        private void SendNoHotelLetter(StoryContext ctx, Map home)
        {
            string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "No Hotel Available"), ctx);
            string body = "A story delegation tried to visit {COLONY}, but your settlement does not have an open hotel.\n\nTo receive hotel visitors in future quests, build at least one Guest Bed (and a reception desk if desired) and enable 'Guests are welcome' in the Guests tab.";
            body = StoryUtil.Format(body, ctx);
            LookTargets target = home != null ? new LookTargets(home.Center, home) : LookTargets.Invalid;
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, target); // CHECK API
        }

        private static IntVec3 FindGuestBedCell(Map map)
        {
            if (map == null || map.listerBuildings == null) return IntVec3.Invalid;
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || b.Destroyed) continue;
                if (s_hospGuestBedType != null && s_hospGuestBedType.IsInstanceOfType(b))
                {
                    return b.Position;
                }
                if (b is Building_Bed)
                {
                    string typeName = b.GetType().Name;
                    string defName = b.def != null ? b.def.defName : string.Empty;
                    if (typeName.IndexOf("Guest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("Guest", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return b.Position;
                    }
                }
            }
            return IntVec3.Invalid;
        }

        private static Lord AttachVisitLord(Map home, Faction fac, IntVec3 chillSpot, int stayTicks, List<Pawn> guests, out string mode)
        {
            mode = "vanilla_fallback";
            LordJob hospJob = TryCreateHospitalityLordJob(fac, chillSpot, stayTicks);
            if (hospJob != null)
            {
                mode = "hospitality";
                return LordMaker.MakeNewLord(fac, hospJob, home, guests);
            }

            LordJob vanillaJob = new RimWorld.LordJob_VisitColony(fac, chillSpot, stayTicks); // CHECK API
            return LordMaker.MakeNewLord(fac, vanillaJob, home, guests);
        }

        private static LordJob TryCreateHospitalityLordJob(Faction fac, IntVec3 chillSpot, int stayTicks)
        {
            EnsureHospitalityReflection();
            try
            {
                if (s_hospLordCtor4 != null)
                {
                    return s_hospLordCtor4.Invoke(new object[] { fac, chillSpot, stayTicks, false }) as LordJob;
                }
                if (s_hospLordCtor3 != null)
                {
                    return s_hospLordCtor3.Invoke(new object[] { fac, chillSpot, stayTicks }) as LordJob;
                }
                if (s_hospLordCtor2 != null)
                {
                    return s_hospLordCtor2.Invoke(new object[] { fac, chillSpot }) as LordJob;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: failed to instantiate Hospitality.LordJob_VisitColony: " + ex.Message);
            }
            return null;
        }

        private static void TryNotifyHospitalityCompArrive(Pawn p)
        {
            if (p == null || s_hospCompGuestType == null || s_hospCompArriveMethod == null) return;
            try
            {
                List<ThingComp> comps = p.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (s_hospCompGuestType.IsInstanceOfType(comps[i]))
                    {
                        s_hospCompArriveMethod.Invoke(comps[i], null);
                        break;
                    }
                }
            }
            catch
            {
            }
        }

        internal static bool IsSentAwayByHospitality(Pawn p)
        {
            EnsureHospitalityReflection();
            if (p == null || s_hospCompGuestType == null) return false;
            try
            {
                List<ThingComp> comps = p.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (!s_hospCompGuestType.IsInstanceOfType(comps[i])) continue;
                    if (s_hospCompSentAwayField != null)
                    {
                        object v = s_hospCompSentAwayField.GetValue(comps[i]);
                        if (v is bool && (bool)v) return true;
                    }
                    else if (s_hospCompSentAwayProp != null)
                    {
                        object v = s_hospCompSentAwayProp.GetValue(comps[i], null);
                        if (v is bool && (bool)v) return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private static Lord TriggerGracefulDeparture(Map home, Faction fac, List<Pawn> guests)
        {
            if (home == null || guests == null || guests.Count == 0) return null;

            for (int i = 0; i < guests.Count; i++)
            {
                Pawn p = guests[i];
                if (p == null) continue;
                Lord oldLord = p.GetLord();
                if (oldLord != null)
                {
                    oldLord.RemovePawn(p);
                }
            }

            Faction exitFac = fac ?? guests[0].Faction;
            if (exitFac == null) return null;
            return LordMaker.MakeNewLord(exitFac, new LordJob_ExitMapBest(LocomotionUrgency.Walk, false, false), home, guests); // CHECK API
        }

        // =====================================================================
        // SINGLE-PASS MAP SNAPSHOT & LOOKUP HELPERS
        // =====================================================================

        private static void CollectMapGuestSnapshot(Map map, HashSet<int> guestIds, Dictionary<int, Pawn> spawnedMap, HashSet<int> deadOnMap, Dictionary<int, Pawn> heldOnMap)
        {
            if (map == null) return;

            if (map.mapPawns != null)
            {
                IReadOnlyList<Pawn> all = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < all.Count; i++)
                {
                    Pawn p = all[i];
                    if (p != null && guestIds.Contains(p.thingIDNumber))
                    {
                        spawnedMap[p.thingIDNumber] = p;
                    }
                }
            }

            if (map.listerThings != null)
            {
                List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                for (int i = 0; i < corpses.Count; i++)
                {
                    Corpse c = corpses[i] as Corpse;
                    if (c != null && c.InnerPawn != null && guestIds.Contains(c.InnerPawn.thingIDNumber))
                    {
                        deadOnMap.Add(c.InnerPawn.thingIDNumber);
                    }
                }

                List<Thing> holders = map.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    Thing h = holders[i];
                    if (h == null || h.Destroyed || h is Pawn) continue;
                    CollectGuestsInThing(h, guestIds, heldOnMap);
                }
            }
        }

        private static void CollectGuestsInThing(Thing h, HashSet<int> guestIds, Dictionary<int, Pawn> heldOnMap)
        {
            IThingHolder direct = h as IThingHolder;
            if (direct != null) CollectGuestsInHolder(direct, guestIds, heldOnMap);

            ThingWithComps twc = h as ThingWithComps;
            if (twc != null && twc.AllComps != null)
            {
                for (int i = 0; i < twc.AllComps.Count; i++)
                {
                    IThingHolder ch = twc.AllComps[i] as IThingHolder;
                    if (ch != null) CollectGuestsInHolder(ch, guestIds, heldOnMap);
                }
            }
        }

        private static void CollectGuestsInHolder(IThingHolder holder, HashSet<int> guestIds, Dictionary<int, Pawn> heldOnMap)
        {
            if (holder == null) return;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn p = owner[i] as Pawn;
                    if (p != null && guestIds.Contains(p.thingIDNumber))
                    {
                        heldOnMap[p.thingIDNumber] = p;
                    }
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                CollectGuestsInHolder(children[i], guestIds, heldOnMap);
            }
        }

        private static HashSet<int> SnapshotAllFactionPawnsOnMap(Map map, Faction fac)
        {
            HashSet<int> ids = new HashSet<int>();
            if (map == null || fac == null) return ids;

            if (map.mapPawns != null)
            {
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i] != null && pawns[i].Faction == fac) ids.Add(pawns[i].thingIDNumber);
                }
            }

            if (map.listerThings != null)
            {
                List<Thing> holders = map.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder);
                for (int i = 0; i < holders.Count; i++)
                {
                    Thing h = holders[i];
                    if (h == null || h.Destroyed || h is Pawn) continue;
                    CollectFactionPawnsInThing(h, fac, ids);
                }
            }
            return ids;
        }

        private static void CollectFactionPawnsInThing(Thing h, Faction fac, HashSet<int> ids)
        {
            IThingHolder direct = h as IThingHolder;
            if (direct != null) CollectFactionPawnsInHolder(direct, fac, ids);

            ThingWithComps twc = h as ThingWithComps;
            if (twc != null && twc.AllComps != null)
            {
                for (int i = 0; i < twc.AllComps.Count; i++)
                {
                    IThingHolder ch = twc.AllComps[i] as IThingHolder;
                    if (ch != null) CollectFactionPawnsInHolder(ch, fac, ids);
                }
            }
        }

        private static void CollectFactionPawnsInHolder(IThingHolder holder, Faction fac, HashSet<int> ids)
        {
            if (holder == null) return;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn p = owner[i] as Pawn;
                    if (p != null && p.Faction == fac) ids.Add(p.thingIDNumber);
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                CollectFactionPawnsInHolder(children[i], fac, ids);
            }
        }

        private Faction ResolveVisitFaction(StoryContext ctx)
        {
            if (!faction.NullOrEmpty())
            {
                Faction f = StoryUtil.FactionOf(faction);
                if (f != null && !f.defeated && !f.IsPlayer) return f;
                return null;
            }
            if (ctx.Faction != null && !ctx.Faction.defeated && !ctx.Faction.IsPlayer)
            {
                return ctx.Faction;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer))
                {
                    return f;
                }
            }
            return null;
        }

        private static List<int> SnapshotFactionLords(Map map, Faction fac)
        {
            List<int> ids = new List<int>();
            if (map == null || fac == null || map.lordManager == null) return ids;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null && lords[i].faction == fac)
                {
                    ids.Add(lords[i].loadID);
                }
            }
            return ids;
        }

        private static Lord FindLordById(Map map, int lordId)
        {
            if (map == null || lordId < 0 || map.lordManager == null) return null;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null && lords[i].loadID == lordId) return lords[i];
            }
            return null;
        }

        private static Map FindMapByUniqueId(int uniqueId)
        {
            if (uniqueId < 0 || Find.Maps == null) return null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == uniqueId) return maps[i];
            }
            return null;
        }

        private static Faction FindFactionById(int loadId)
        {
            if (loadId < 0 || Find.FactionManager == null) return null;
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId) return all[i];
            }
            return null;
        }

        private static Pawn FindWorldPawnByIdNum(int idNum)
        {
            if (Find.WorldPawns == null) return null;
            List<Pawn> alive = Find.WorldPawns.AllPawnsAlive;
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i] != null && alive[i].thingIDNumber == idNum) return alive[i];
            }
            return null;
        }

        private static string JoinInts(List<int> list)
        {
            if (list == null || list.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(list[i]);
            }
            return sb.ToString();
        }

        private static List<int> ParseInts(string raw)
        {
            List<int> result = new List<int>();
            if (raw.NullOrEmpty()) return result;
            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) result.Add(val);
            }
            return result;
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static bool TryGetInt(StoryContext ctx, string key, out int val)
        {
            string raw;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return true;
            val = 0;
            return false;
        }

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            int val;
            return TryGetInt(ctx, key, out val) ? val : fallback;
        }

        private static bool GetBool(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }
    }
}

```

`Source/StoryStage_Deliver.cs`

```
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public enum StoryDeliverTarget
    {
        colony,
        giverSettlement
    }

    // =========================================================================
    // COLONY HAND-IN CHOICE LETTER (VALIDATED AGAINST IMMUTABLE CONTRACT)
    // =========================================================================

    /// <summary>
    /// Reminder/hand-in ChoiceLetter for StoryStage_Deliver (to = colony).
    ///
    /// Round 2 invariants:
    /// - NEVER trusts letter.thingDefName, letter.requiredCount, letter.deadlineTick, or letter.deliverMapId
    ///   as authoritative payment terms.
    /// - Resolves the active stage STRICTLY by stageKey (if stageKey is present and missing from def.stages,
    ///   refuses with zero fallback to stale run.stage index).
    /// - Reads ThingDef, count, mapId, and deadlineTick from the verified stage &amp; immutable contract
    ///   recorded in run.vars at Start; unknown/missing deadlineTick ALWAYS refuses payment.
    /// - Deducts exclusively via atomic StoryPayment with onPaymentSecured recording delivered=true
    ///   after 100% staging confirmation and before irreversible destruction.
    /// </summary>
    public sealed class ChoiceLetter_StoryDeliver : ChoiceLetter
    {
        public string questDefName;
        public string stageKey;
        public int runGen;
        public int deliverMapId = -1;
        public string thingDefName;
        public int requiredCount = 1;
        public int deadlineTick = -1;
        private bool resolved;

        public ChoiceLetter_StoryDeliver()
        {
            if (Find.UniqueIDsManager != null)
            {
                ID = Find.UniqueIDsManager.GetNextLetterID();
            }
        }

        public override bool CanDismissWithRightClick
        {
            get { return false; }
        }

        private bool InStack
        {
            get { return Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(this); }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                StoryQuestRun run;
                Map pinnedMap;
                ThingDef contractDef;
                int contractCount;
                string invalidReason;

                bool valid = StoryStage_Deliver.ValidateColonyHandIn(
                    this, out run, out pinnedMap, out contractDef, out contractCount, out invalidReason);

                if (!valid)
                {
                    DiaOption close = new DiaOption("Close".Translate());
                    close.action = delegate
                    {
                        resolved = true;
                        if (InStack) Find.LetterStack.RemoveLetter(this);
                    };
                    close.resolveTree = true;
                    yield return close;
                    yield break;
                }

                int have = StoryPayment.CountInColonyStockpiles(pinnedMap, contractDef);
                string itemLabel = contractDef.label;
                string deliverLabel = string.Format("Deliver {0} x{1} ({2}/{3})", itemLabel, contractCount, have, contractCount);
                DiaOption deliver = new DiaOption(deliverLabel);

                if (have < contractCount)
                {
                    deliver.Disable(string.Format("Need {0} {1} in colony storage on {2} ({3}/{0})",
                        contractCount, itemLabel, pinnedMap.Parent != null ? pinnedMap.Parent.LabelCap.ToString() : "colony", have));
                }
                else
                {
                    deliver.action = delegate
                    {
                        // Full re-validation at click time against verified stage & immutable vars contract
                        StoryQuestRun clickRun;
                        Map clickMap;
                        ThingDef clickDef;
                        int clickCount;
                        string clickReason;
                        if (!StoryStage_Deliver.ValidateColonyHandIn(this, out clickRun, out clickMap, out clickDef, out clickCount, out clickReason))
                        {
                            resolved = true;
                            if (InStack) Find.LetterStack.RemoveLetter(this);
                            return;
                        }

                        int deducted;
                        bool paid = StoryPayment.TryDeductFromColonyStockpiles(
                            clickMap,
                            clickDef,
                            clickCount,
                            out deducted,
                            delegate
                            {
                                clickRun.vars["delivered"] = "true";
                            });

                        if (paid && deducted == clickCount)
                        {
                            resolved = true;
                            if (InStack) Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    deliver.resolveTree = true;
                }

                yield return deliver;
                yield return Option_Postpone; // CHECK API
            }
        }

        public bool IsResolved
        {
            get { return resolved; }
        }

        public bool IsInLetterStack
        {
            get { return InStack; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref stageKey, "stageKey", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref deliverMapId, "deliverMapId", -1);
            Scribe_Values.Look(ref thingDefName, "thingDefName", null);
            Scribe_Values.Look(ref requiredCount, "requiredCount", 1);
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", -1);
            Scribe_Values.Look(ref resolved, "resolved", false);
        }
    }

    // =========================================================================
    // CARAVAN ARRIVAL ACTION FOR giverSettlement (EXPLICIT ARRIVAL ONLY)
    // =========================================================================

    public sealed class CaravanArrivalAction_StoryDeliver : CaravanArrivalAction
    {
        public Settlement settlement;
        public string questDefName;
        public string stageKey;
        public int runGen;

        public CaravanArrivalAction_StoryDeliver()
        {
        }

        public CaravanArrivalAction_StoryDeliver(Settlement settlement, string questDefName, string stageKey, int runGen)
        {
            this.settlement = settlement;
            this.questDefName = questDefName;
            this.stageKey = stageKey;
            this.runGen = runGen;
        }

        public override string Label
        {
            get
            {
                return string.Format("Deliver quest cargo to {0}",
                    settlement != null ? settlement.LabelCap.ToString() : "settlement");
            }
        }

        public override string ReportString
        {
            get
            {
                return string.Format("Delivering quest cargo to {0}.",
                    settlement != null ? settlement.LabelCap.ToString() : "settlement");
            }
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile) // CHECK API
        {
            FloatMenuAcceptanceReport baseRep = base.StillValid(caravan, destinationTile);
            if (!baseRep) return baseRep;
            if (settlement == null || settlement.Destroyed || settlement.Tile != destinationTile) return false;
            return StoryStage_Deliver.CanCaravanDeliver(caravan, settlement, questDefName, stageKey, runGen);
        }

        public override void Arrived(Caravan caravan)
        {
            StoryStage_Deliver.TryCompleteCaravanHandIn(caravan, settlement, questDefName, stageKey, runGen);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref stageKey, "stageKey", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
        }
    }

    // =========================================================================
    // STORY STAGE: DELIVER
    // =========================================================================

    public sealed class StoryStage_Deliver : StoryStage
    {
        public ThingDef thing;
        public int count = 1;
        public StoryDeliverTarget to = StoryDeliverTarget.colony;
        public float timeoutDays = 10f;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "deliver"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            int now = Find.TickManager.TicksGame;
            int deadline = now + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["deadlineTick"] = deadline.ToString();
            ctx.run.vars["delivered"] = "false";

            if (thing == null || count <= 0)
            {
                ctx.run.vars["failReason"] = "invalid_deliver_def";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            // Record immutable payment contract in run.vars at Start
            ctx.run.vars["contractThingDef"] = thing.defName;
            ctx.run.vars["contractCount"] = count.ToString();
            ctx.run.vars["contractTarget"] = to.ToString();

            if (to == StoryDeliverTarget.colony)
            {
                Map home = ctx.Home;
                if (home == null)
                {
                    ctx.run.vars["failReason"] = "no_home_map";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }
                ctx.run.vars["deliverMapId"] = home.uniqueID.ToString();
                SendColonyDeliverLetter(ctx, home, deadline);
            }
            else
            {
                Settlement target = FindNearestExactFactionSettlement(ctx);
                if (target == null)
                {
                    Log.Warning("[TMM] StoryStage_Deliver: no settlement belonging to quest faction '" +
                                (ctx.def != null ? ctx.def.faction : "null") + "'. Failing explicitly without colony fallback.");
                    ctx.run.vars["failReason"] = "no_faction_settlement";
                    ctx.run.vars["immediateOutcome"] = FindOutcome("unavailable") != null ? "unavailable" : "failed";
                    return;
                }

                ctx.run.vars["settlementId"] = target.ID.ToString();
                ctx.run.vars["settlementFactionId"] = target.Faction != null ? target.Faction.loadID.ToString() : "-1";
                ctx.run.vars["settlementName"] = target.LabelCap.ToString();

                string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Caravan Delivery"), ctx);
                string body = BuildSettlementLetterText(ctx, target);
                Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, new LookTargets(target)); // CHECK API
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            string immediate = GetStr(ctx.run, "immediateOutcome");
            if (!immediate.NullOrEmpty())
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = immediate;
                return immediate;
            }

            // 1. If payment was already confirmed, finish cleanly without re-deducting
            if (GetBool(ctx.run, "delivered"))
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "success";
                return "success";
            }

            // 2. Unknown/missing deadlineTick -> refuse to slide deadline forward; expire cleanly
            int now = Find.TickManager.TicksGame;
            int deadline;
            if (!TryGetInt(ctx.run, "deadlineTick", out deadline) || deadline < 0)
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["failReason"] = "deadline_missing";
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            if (now >= deadline)
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            if (to == StoryDeliverTarget.colony)
            {
                int mapId = GetInt(ctx.run, "deliverMapId", -1);
                Map pinnedMap = FindMapByUniqueId(mapId);
                if (pinnedMap == null || !pinnedMap.IsPlayerHome)
                {
                    RemoveDeliverLetters(ctx);
                    ctx.run.vars["failReason"] = "deliver_map_lost";
                    ctx.run.vars["finished"] = "failed";
                    return "failed";
                }

                if (!HasActiveDeliverLetter(ctx))
                {
                    SendColonyDeliverLetter(ctx, pinnedMap, deadline);
                }
                return null;
            }

            // 3. giverSettlement mode: verify target settlement still exists & belongs to required faction
            int settlementId = GetInt(ctx.run, "settlementId", -1);
            int expectedFacId = GetInt(ctx.run, "settlementFactionId", -1);
            Settlement target = FindSettlementById(settlementId);
            if (target == null || target.Destroyed || target.Faction == null || target.Faction.loadID != expectedFacId)
            {
                ctx.run.vars["failReason"] = "settlement_gone";
                ctx.run.vars["finished"] = "failed";
                return "failed";
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            ThingDef displayDef = thing;
            int displayCount = count;
            if (ctx != null && ctx.run != null)
            {
                ThingDef cDef;
                int cCount;
                if (TryResolveImmutableContract(ctx.run, this, out cDef, out cCount))
                {
                    displayDef = cDef;
                    displayCount = cCount;
                }
            }

            string itemLabel = displayDef != null ? displayDef.label : "resources";
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", itemLabel, displayCount), ctx);

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx.run, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            if (to == StoryDeliverTarget.colony)
            {
                Map pinnedMap = FindMapByUniqueId(GetInt(ctx.run, "deliverMapId", -1));
                int have = (pinnedMap != null && displayDef != null) ? StoryPayment.CountInColonyStockpiles(pinnedMap, displayDef) : 0;
                return string.Format("{0} ({1}/{2} {3}, {4:0.0}d left)", baseText, have, displayCount, itemLabel, daysLeft);
            }

            string targetName = GetStr(ctx.run, "settlementName");
            if (targetName.NullOrEmpty()) targetName = "faction settlement";
            return string.Format("{0} to {1} ({2:0.0}d left)", baseText, targetName, daysLeft);
        }

        // =====================================================================
        // STRICT VALIDATION (NO STALE INDEX FALLBACK, NO UNKNOWN DEADLINE,
        // IMMUTABLE CONTRACT TERMS ONLY)
        // =====================================================================

        internal static bool ValidateColonyHandIn(
            ChoiceLetter_StoryDeliver letter,
            out StoryQuestRun run,
            out Map pinnedMap,
            out ThingDef contractDef,
            out int contractCount,
            out string reason)
        {
            run = null;
            pinnedMap = null;
            contractDef = null;
            contractCount = 0;
            reason = null;

            if (letter == null || letter.IsResolved || !letter.IsInLetterStack)
            {
                reason = "letter_not_active";
                return false;
            }

            StoryStage_Deliver stageDef;
            if (!ValidateActiveDeliverStage(
                    letter.questDefName,
                    letter.stageKey,
                    letter.runGen,
                    StoryDeliverTarget.colony,
                    out run,
                    out stageDef,
                    out contractDef,
                    out contractCount,
                    out reason))
            {
                return false;
            }

            // Unknown/missing deadlineTick in run.vars ALWAYS refuses payment (never falls back to letter)
            int now = Find.TickManager.TicksGame;
            int dl;
            if (!TryGetInt(run, "deadlineTick", out dl) || dl < 0 || now >= dl)
            {
                reason = "deadline_missing_or_passed";
                return false;
            }

            // Pinned map comes strictly from run.vars (never from letter)
            int mapId;
            if (!TryGetInt(run, "deliverMapId", out mapId) || mapId < 0)
            {
                reason = "deliver_map_missing";
                return false;
            }

            pinnedMap = FindMapByUniqueId(mapId);
            if (pinnedMap == null || !pinnedMap.IsPlayerHome)
            {
                reason = "pinned_map_missing";
                return false;
            }

            return true;
        }

        internal static bool ValidateActiveDeliverStage(
            string questDefName,
            string expectedStageKey,
            int expectedGen,
            StoryDeliverTarget expectedTarget,
            out StoryQuestRun run,
            out StoryStage_Deliver stageDef,
            out ThingDef contractDef,
            out int contractCount,
            out string reason)
        {
            run = null;
            stageDef = null;
            contractDef = null;
            contractCount = 0;
            reason = null;

            SparkStory story = SparkStory.Instance;
            if (story == null || questDefName.NullOrEmpty())
            {
                reason = "no_story";
                return false;
            }

            run = story.RunFor(questDefName);
            if (run == null || run.status != StoryQuestStatus.Active)
            {
                reason = "run_not_active";
                return false;
            }

            if (run.gen != expectedGen)
            {
                reason = "gen_mismatch";
                return false;
            }

            if (!expectedStageKey.NullOrEmpty() && run.stageKey != expectedStageKey)
            {
                reason = "stage_key_mismatch";
                return false;
            }

            if (run.vars == null || run.vars.ContainsKey("finished") || GetBool(run, "delivered"))
            {
                reason = "already_delivered_or_finished";
                return false;
            }

            StoryQuestDef def = run.Def;
            if (def == null || def.stages == null)
            {
                reason = "def_missing";
                return false;
            }

            // STRICT stageKey RESOLUTION:
            // If stageKey is set (either on the caller or on run.stageKey) and NO stage in def.stages
            // matches that exact key -> REFUSE! Never fall back to the stale run.stage index!
            string lookupKey = !expectedStageKey.NullOrEmpty() ? expectedStageKey : run.stageKey;
            StoryStage foundStage = null;

            if (!lookupKey.NullOrEmpty())
            {
                for (int i = 0; i < def.stages.Count; i++)
                {
                    if (def.stages[i] != null && def.stages[i].Key(i) == lookupKey)
                    {
                        foundStage = def.stages[i];
                        break;
                    }
                }
                if (foundStage == null)
                {
                    reason = "stage_key_removed";
                    return false;
                }
            }
            else if (run.stage >= 0 && run.stage < def.stages.Count)
            {
                // Legacy migration only when neither letter nor run carried a stageKey
                foundStage = def.stages[run.stage];
            }

            stageDef = foundStage as StoryStage_Deliver;
            if (stageDef == null || stageDef.to != expectedTarget)
            {
                reason = "wrong_stage_type";
                return false;
            }

            // Verify contract target matches expectedTarget if recorded
            string recordedTarget = GetStr(run, "contractTarget");
            if (!recordedTarget.NullOrEmpty() && !string.Equals(recordedTarget, expectedTarget.ToString(), StringComparison.Ordinal))
            {
                reason = "contract_target_mismatch";
                return false;
            }

            // Resolve ThingDef and count strictly from immutable vars contract (or verified current stageDef),
            // NEVER from letter fields!
            if (!TryResolveImmutableContract(run, stageDef, out contractDef, out contractCount))
            {
                reason = "invalid_contract";
                return false;
            }

            return true;
        }

        private static bool TryResolveImmutableContract(StoryQuestRun run, StoryStage_Deliver verifiedStage, out ThingDef resolvedDef, out int resolvedCount)
        {
            resolvedDef = null;
            resolvedCount = 0;
            if (verifiedStage == null) return false;

            string rawDef = GetStr(run, "contractThingDef");
            string rawCount = GetStr(run, "contractCount");

            if (!rawDef.NullOrEmpty() || !rawCount.NullOrEmpty())
            {
                ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(rawDef);
                int c;
                if (d == null || !int.TryParse(rawCount, out c) || c <= 0)
                {
                    return false;
                }
                resolvedDef = d;
                resolvedCount = c;
                return true;
            }

            // Legacy run created before immutable contract vars were added: use verified current stageDef
            if (verifiedStage.thing == null || verifiedStage.count <= 0)
            {
                return false;
            }
            resolvedDef = verifiedStage.thing;
            resolvedCount = verifiedStage.count;
            return true;
        }

        public static bool CanCaravanDeliver(Caravan caravan, Settlement settlement, string questDefName, string expectedStageKey, int expectedGen)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return false;
            if (settlement == null || settlement.Destroyed) return false;

            StoryQuestRun run;
            StoryStage_Deliver stageDef;
            ThingDef contractDef;
            int contractCount;
            string reason;
            if (!ValidateActiveDeliverStage(
                    questDefName, expectedStageKey, expectedGen, StoryDeliverTarget.giverSettlement,
                    out run, out stageDef, out contractDef, out contractCount, out reason))
            {
                return false;
            }

            // Unknown/missing deadlineTick ALWAYS refuses
            int now = Find.TickManager.TicksGame;
            int dl;
            if (!TryGetInt(run, "deadlineTick", out dl) || dl < 0 || now >= dl) return false;

            int expectedSettlementId = GetInt(run, "settlementId", -1);
            int expectedFacId = GetInt(run, "settlementFactionId", -1);
            if (settlement.ID != expectedSettlementId) return false;
            if (settlement.Faction == null || settlement.Faction.loadID != expectedFacId) return false;

            return StoryPayment.CountInCaravanDirectInventory(caravan, contractDef) >= contractCount;
        }

        public static bool TryCompleteCaravanHandIn(Caravan caravan, Settlement settlement, string questDefName, string expectedStageKey, int expectedGen)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return false;
            if (settlement == null || settlement.Destroyed || caravan.Tile != settlement.Tile) return false;

            StoryQuestRun run;
            StoryStage_Deliver stageDef;
            ThingDef contractDef;
            int contractCount;
            string reason;
            if (!ValidateActiveDeliverStage(
                    questDefName, expectedStageKey, expectedGen, StoryDeliverTarget.giverSettlement,
                    out run, out stageDef, out contractDef, out contractCount, out reason))
            {
                return false;
            }

            // Unknown/missing deadlineTick ALWAYS refuses before touching any items
            int now = Find.TickManager.TicksGame;
            int dl;
            if (!TryGetInt(run, "deadlineTick", out dl) || dl < 0 || now >= dl) return false;

            int expectedSettlementId = GetInt(run, "settlementId", -1);
            int expectedFacId = GetInt(run, "settlementFactionId", -1);
            if (settlement.ID != expectedSettlementId || settlement.Faction == null || settlement.Faction.loadID != expectedFacId)
            {
                return false;
            }

            int deducted;
            bool paid = StoryPayment.TryDeductFromCaravan(
                caravan,
                contractDef,
                contractCount,
                out deducted,
                delegate
                {
                    run.vars["delivered"] = "true";
                });

            if (!paid || deducted != contractCount)
            {
                return false;
            }

            string doneTitle = !stageDef.label.NullOrEmpty() ? stageDef.label : "Delivery Complete";
            string doneBody = string.Format("Your caravan delivered {0} x{1} to {2}.",
                contractDef.label, contractCount, settlement.LabelCap);
            Find.LetterStack.ReceiveLetter(doneTitle, doneBody, LetterDefOf.PositiveEvent, new LookTargets(settlement)); // CHECK API
            return true;
        }

        internal sealed class ActiveSettlementDelivery
        {
            public StoryQuestRun run;
            public StoryStage_Deliver stage;
            public ThingDef contractDef;
            public int contractCount;
        }

        internal static List<ActiveSettlementDelivery> FindActiveSettlementDeliveries(Settlement settlement)
        {
            List<ActiveSettlementDelivery> matches = new List<ActiveSettlementDelivery>();
            SparkStory story = SparkStory.Instance;
            if (story == null || settlement == null || settlement.Destroyed || settlement.Faction == null) return matches;

            int now = Find.TickManager.TicksGame;
            foreach (StoryQuestRun run in story.Runs)
            {
                if (run == null || run.status != StoryQuestStatus.Active) continue;
                if (run.vars == null || run.vars.ContainsKey("finished") || GetBool(run, "delivered")) continue;
                if (GetInt(run, "settlementId", -1) != settlement.ID) continue;
                if (GetInt(run, "settlementFactionId", -1) != settlement.Faction.loadID) continue;

                int dl;
                if (!TryGetInt(run, "deadlineTick", out dl) || dl < 0 || now >= dl) continue;

                StoryQuestRun verifiedRun;
                StoryStage_Deliver st;
                ThingDef cDef;
                int cCount;
                string reason;
                if (!ValidateActiveDeliverStage(
                        run.quest, run.stageKey, run.gen, StoryDeliverTarget.giverSettlement,
                        out verifiedRun, out st, out cDef, out cCount, out reason))
                {
                    continue;
                }

                ActiveSettlementDelivery entry = new ActiveSettlementDelivery();
                entry.run = verifiedRun;
                entry.stage = st;
                entry.contractDef = cDef;
                entry.contractCount = cCount;
                matches.Add(entry);
            }
            return matches;
        }

        // =====================================================================
        // LETTER & SETTLEMENT HELPERS
        // =====================================================================

        private void SendColonyDeliverLetter(StoryContext ctx, Map pinnedMap, int deadline)
        {
            if (Find.LetterStack == null || thing == null || pinnedMap == null) return;

            ThingDef displayDef;
            int displayCount;
            if (!TryResolveImmutableContract(ctx.run, this, out displayDef, out displayCount))
            {
                displayDef = thing;
                displayCount = Mathf.Max(1, count);
            }

            ChoiceLetter_StoryDeliver letter = new ChoiceLetter_StoryDeliver();
            letter.def = LetterDefOf.NeutralEvent;
            letter.Label = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", displayDef.label, displayCount), ctx);
            letter.Text = BuildColonyLetterText(ctx, displayDef, displayCount);
            letter.questDefName = ctx.def != null ? ctx.def.defName : string.Empty;
            letter.stageKey = ctx.run.stageKey;
            letter.runGen = ctx.run.gen;
            letter.deliverMapId = pinnedMap.uniqueID;
            letter.thingDefName = displayDef.defName;
            letter.requiredCount = displayCount;
            letter.deadlineTick = deadline;
            letter.lookTargets = new LookTargets(pinnedMap.Center, pinnedMap);

            Find.LetterStack.ReceiveLetter(letter); // CHECK API
        }

        private string BuildColonyLetterText(StoryContext ctx, ThingDef displayDef, int displayCount)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = displayDef != null ? displayDef.label : "items";
            string instruction = string.Format("Gather {0} x{1} in {2}'s storage stockpiles within {3:0.0} days, then open this letter and click Deliver.",
                itemLabel, displayCount, "{COLONY}", timeoutDays);
            instruction = StoryUtil.Format(instruction, ctx);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private string BuildSettlementLetterText(StoryContext ctx, Settlement target)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Send a caravan carrying {0} x{1} to {2} and choose 'Deliver quest cargo' within {3:0.0} days.",
                itemLabel, count, target.LabelCap, timeoutDays);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private static bool HasActiveDeliverLetter(StoryContext ctx)
        {
            if (Find.LetterStack == null || ctx == null || ctx.def == null || ctx.run == null) return false;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = 0; i < letters.Count; i++)
            {
                ChoiceLetter_StoryDeliver dl = letters[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && !dl.IsResolved && dl.questDefName == ctx.def.defName &&
                    dl.runGen == ctx.run.gen && dl.stageKey == ctx.run.stageKey)
                {
                    return true;
                }
            }
            return false;
        }

        private static void RemoveDeliverLetters(StoryContext ctx)
        {
            if (Find.LetterStack == null || ctx == null || ctx.def == null || ctx.run == null) return;
            List<Letter> snapshot = new List<Letter>(Find.LetterStack.LettersListForReading);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ChoiceLetter_StoryDeliver dl = snapshot[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == ctx.def.defName && dl.runGen == ctx.run.gen)
                {
                    Find.LetterStack.RemoveLetter(dl);
                }
            }
        }

        private static Settlement FindNearestExactFactionSettlement(StoryContext ctx)
        {
            Faction targetFac = ctx.Faction;
            if (targetFac == null || targetFac.IsPlayer || targetFac.defeated) return null;

            PlanetTile homeTile = ResolveHomeTile(ctx); // CHECK API
            Settlement best = null;
            float bestDist = float.MaxValue;

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s == null || s.Destroyed || s.Faction != targetFac) continue;

                float dist = homeTile.Valid ? Find.WorldGrid.TraversalDistanceBetween(homeTile, s.Tile) : 0f; // CHECK API
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = s;
                }
            }
            return best;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0 || Find.WorldObjects == null) return null;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s != null && !s.Destroyed && s.ID == id) return s;
            }
            return null;
        }

        private static Map FindMapByUniqueId(int uniqueId)
        {
            if (uniqueId < 0 || Find.Maps == null) return null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == uniqueId) return maps[i];
            }
            return null;
        }

        private static PlanetTile ResolveHomeTile(StoryContext ctx) // CHECK API
        {
            if (ctx.Home != null) return ctx.Home.Tile; // CHECK API
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (settlements[i] != null && settlements[i].Faction == Faction.OfPlayer)
                {
                    return settlements[i].Tile; // CHECK API
                }
            }
            return PlanetTile.Invalid; // CHECK API
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static bool TryGetInt(StoryQuestRun run, string key, out int val)
        {
            string raw;
            if (run != null && run.vars != null && run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return true;
            val = 0;
            return false;
        }

        private static int GetInt(StoryQuestRun run, string key, int fallback)
        {
            int val;
            return TryGetInt(run, key, out val) ? val : fallback;
        }

        private static bool GetBool(StoryQuestRun run, string key)
        {
            string raw;
            return run != null && run.vars != null && run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryQuestRun run, string key)
        {
            string raw;
            return (run != null && run.vars != null && run.vars.TryGetValue(key, out raw)) ? raw : string.Empty;
        }
    }

    // =========================================================================
    // HARMONY PATCHES: EXPLICIT CARAVAN DELIVERY OPTIONS (NO PASSING-BY STEAL)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class StoryDeliverCaravanPatches
    {
        static StoryDeliverCaravanPatches()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.storydeliver");

            MethodInfo settlementFloatMenu = AccessTools.Method(typeof(Settlement), "GetFloatMenuOptions", new Type[] { typeof(Caravan) }); // CHECK API
            if (settlementFloatMenu != null)
            {
                harmony.Patch(settlementFloatMenu, postfix: new HarmonyMethod(typeof(StoryDeliverCaravanPatches), "Postfix_Settlement_GetFloatMenuOptions"));
            }

            MethodInfo caravanGizmos = AccessTools.Method(typeof(Caravan), "GetGizmos"); // CHECK API
            if (caravanGizmos != null)
            {
                harmony.Patch(caravanGizmos, postfix: new HarmonyMethod(typeof(StoryDeliverCaravanPatches), "Postfix_Caravan_GetGizmos"));
            }
        }

        public static IEnumerable<FloatMenuOption> Postfix_Settlement_GetFloatMenuOptions(IEnumerable<FloatMenuOption> __result, Settlement __instance, Caravan caravan)
        {
            if (__result != null)
            {
                foreach (FloatMenuOption opt in __result)
                {
                    yield return opt;
                }
            }

            if (__instance == null || caravan == null || caravan.Faction != Faction.OfPlayer) yield break;

            List<StoryStage_Deliver.ActiveSettlementDelivery> deliveries = StoryStage_Deliver.FindActiveSettlementDeliveries(__instance);
            for (int i = 0; i < deliveries.Count; i++)
            {
                StoryStage_Deliver.ActiveSettlementDelivery d = deliveries[i];
                if (d == null || d.run == null || d.contractDef == null) continue;

                string capturedQuest = d.run.quest;
                string capturedKey = d.run.stageKey;
                int capturedGen = d.run.gen;
                Settlement capturedSettlement = __instance;
                Caravan capturedCaravan = caravan;

                int have = StoryPayment.CountInCaravanDirectInventory(caravan, d.contractDef);
                string optLabel = string.Format("Deliver {0} x{1} to {2} ({3}/{1})",
                    d.contractDef.label, d.contractCount, __instance.LabelCap, have);

                if (have < d.contractCount)
                {
                    yield return new FloatMenuOption(optLabel + " (not enough cargo)", null);
                    continue;
                }

                yield return new FloatMenuOption(optLabel, delegate
                {
                    if (capturedCaravan.Tile == capturedSettlement.Tile)
                    {
                        StoryStage_Deliver.TryCompleteCaravanHandIn(capturedCaravan, capturedSettlement, capturedQuest, capturedKey, capturedGen);
                    }
                    else
                    {
                        CaravanArrivalAction_StoryDeliver action = new CaravanArrivalAction_StoryDeliver(
                            capturedSettlement, capturedQuest, capturedKey, capturedGen);
                        capturedCaravan.pather.StartPath(capturedSettlement.Tile, action, true); // CHECK API
                    }
                });
            }
        }

        public static IEnumerable<Gizmo> Postfix_Caravan_GetGizmos(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            if (__result != null)
            {
                foreach (Gizmo g in __result)
                {
                    yield return g;
                }
            }

            if (__instance == null || __instance.Destroyed || __instance.Faction != Faction.OfPlayer) yield break;
            if (Find.WorldObjects == null) yield break;

            Settlement here = Find.WorldObjects.SettlementAt(__instance.Tile); // CHECK API
            if (here == null || here.Destroyed) yield break;

            List<StoryStage_Deliver.ActiveSettlementDelivery> deliveries = StoryStage_Deliver.FindActiveSettlementDeliveries(here);
            for (int i = 0; i < deliveries.Count; i++)
            {
                StoryStage_Deliver.ActiveSettlementDelivery d = deliveries[i];
                if (d == null || d.run == null || d.contractDef == null) continue;

                StoryQuestDef qDef = d.run.Def;
                int have = StoryPayment.CountInCaravanDirectInventory(__instance, d.contractDef);
                string capturedQuest = d.run.quest;
                string capturedKey = d.run.stageKey;
                int capturedGen = d.run.gen;
                Settlement capturedSettlement = here;
                Caravan capturedCaravan = __instance;

                Command_Action cmd = new Command_Action();
                cmd.defaultLabel = string.Format("Deliver {0} x{1}", d.contractDef.label, d.contractCount);
                cmd.defaultDesc = string.Format("Hand in {0} x{1} to {2} for quest '{3}' ({4}/{1} carried).",
                    d.contractDef.label, d.contractCount, here.LabelCap, qDef != null ? qDef.LabelCap.ToString() : d.run.quest, have);
                cmd.icon = d.contractDef.uiIcon; // CHECK API
                if (have < d.contractCount)
                {
                    cmd.Disable(string.Format("Caravan carries only {0}/{1} {2}.", have, d.contractCount, d.contractDef.label));
                }
                cmd.action = delegate
                {
                    StoryStage_Deliver.TryCompleteCaravanHandIn(capturedCaravan, capturedSettlement, capturedQuest, capturedKey, capturedGen);
                };
                yield return cmd;
            }
        }
    }
}

```

`Tests/InGameStoryHotelProbe.cs`

```
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class StoryHotelProbeBootstrap
    {
        public const string SaveName = "TMM-StoryHotel";

        // Runtime probe quests (registered identically on fresh & reload so Scribe resolves Defs cleanly)
        public const string QNoHotel = "TMM_Probe_E3_NoHotel";
        public const string QHeroColonistSkip = "TMM_Probe_E3_HeroColonistSkip";
        public const string QHeroCompanionException = "TMM_Probe_E3_HeroCompanionException";
        public const string QPermanentEnemy = "TMM_Probe_E3_PermanentEnemy";
        public const string QTruceOverlapA = "TMM_Probe_E3_TruceOverlapA";
        public const string QTruceOverlapB = "TMM_Probe_E3_TruceOverlapB";
        public const string QTruceStageRemovedOrphan = "TMM_Probe_E3_TruceStageRemovedOrphan";
        public const string QHostKilled = "TMM_Probe_E3_HostKilled";
        public const string QHostImprisoned = "TMM_Probe_E3_HostImprisoned";
        public const string QHostVanishedNoExit = "TMM_Probe_E3_HostVanishedNoExit";
        public const string QInvestigateStrict = "TMM_Probe_E3_InvestigateStrict";
        public const string QHideOwnedRaid = "TMM_Probe_E3_HideOwnedRaid";
        public const string QHideFailedRaidDeadline = "TMM_Probe_E3_HideFailedRaidDeadline";
        public const string QDeliverExpiredClick = "TMM_Probe_E3_DeliverExpiredClick";
        public const string QDeliverUnknownDeadline = "TMM_Probe_E3_DeliverUnknownDeadline";
        public const string QDeliverImmutableContract = "TMM_Probe_E3_DeliverImmutableContract";
        public const string QDeliverStageKeyRemoved = "TMM_Probe_E3_DeliverStageKeyRemoved";
        public const string QDeliverSecondStackFail = "TMM_Probe_E3_DeliverSecondStackFail";
        public const string QDeliverCommitDestroyEx = "TMM_Probe_E3_DeliverCommitDestroyEx";
        public const string QDeliverCaravanSettlement = "TMM_Probe_E3_DeliverCaravanSettlement";
        public const string QReloadActiveVisit = "TMM_Probe_E3_ReloadActiveVisit";
        public const string QReloadPaidDeliver = "TMM_Probe_E3_ReloadPaidDeliver";

        public static bool FreshMode
        {
            get { return HasArg("-tmm-story-hotel-probe"); }
        }

        public static bool ReloadMode
        {
            get { return HasArg("-tmm-story-hotel-reload"); }
        }

        static StoryHotelProbeBootstrap()
        {
            if (!FreshMode && !ReloadMode) return;

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                RegisterRuntimeProbeQuests();
                if (ReloadMode && Current.ProgramState == ProgramState.Entry)
                {
                    GameDataSaveLoader.LoadGame(SaveName); // CHECK API
                }
            });
        }

        private static void RegisterRuntimeProbeQuests()
        {
            HeroDef heroA = PickHero(0);
            HeroDef heroB = PickHero(1) ?? heroA;
            HeroDef heroC = PickHero(2) ?? heroB;

            AddQuestDef(BuildHotelQuest(QNoHotel, "probe.e3.noHotel", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHeroColonistSkip, "probe.e3.heroColonist", null, heroA, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHeroCompanionException, "probe.e3.heroCompEx", null, heroC, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QPermanentEnemy, "probe.e3.permEnemy", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QTruceOverlapA, "probe.e3.truceOverlapA", null, null, 1, 1f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QTruceOverlapB, "probe.e3.truceOverlapB", null, null, 1, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QTruceStageRemovedOrphan, "probe.e3.truceStageRemoved", null, null, 1, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHostKilled, "probe.e3.hostKilled", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHostImprisoned, "probe.e3.hostImprisoned", null, null, 2, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QHostVanishedNoExit, "probe.e3.hostVanished", null, null, 1, 2f, 1f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildHotelQuest(QInvestigateStrict, "probe.e3.investigate", null, null, 1, 2f, 1f, StoryHotelObjective.Investigate, null, -1f));
            AddQuestDef(BuildHotelQuest(QHideOwnedRaid, "probe.e3.hideOwned", null, null, 1, 1f, 1f, StoryHotelObjective.Hide, null, 2f));
            AddQuestDef(BuildHotelQuest(QHideFailedRaidDeadline, "probe.e3.hideFailed", null, null, 1, 0.5f, 0.25f, StoryHotelObjective.Hide, null, 1f));

            AddQuestDef(BuildDeliverQuest(QDeliverExpiredClick, "probe.e3.deliverExpired", null, ThingDefOf.Steel, 30, StoryDeliverTarget.colony, 0.2f));
            AddQuestDef(BuildDeliverQuest(QDeliverUnknownDeadline, "probe.e3.deliverUnknownDl", null, ThingDefOf.Steel, 25, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverImmutableContract, "probe.e3.deliverImmutable", null, ThingDefOf.Steel, 30, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverStageKeyRemoved, "probe.e3.deliverKeyRemoved", null, ThingDefOf.Steel, 30, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverSecondStackFail, "probe.e3.deliverAtomic", null, ThingDefOf.Steel, 50, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverCommitDestroyEx, "probe.e3.deliverCommitEx", null, ThingDefOf.Steel, 20, StoryDeliverTarget.colony, 5f));
            AddQuestDef(BuildDeliverQuest(QDeliverCaravanSettlement, "probe.e3.deliverCaravan", null, ThingDefOf.Steel, 40, StoryDeliverTarget.giverSettlement, 10f));

            AddQuestDef(BuildHotelQuest(QReloadActiveVisit, "probe.e3.reloadVisit", null, heroB, 2, 3f, 2f, StoryHotelObjective.Host, null, -1f));
            AddQuestDef(BuildDeliverQuest(QReloadPaidDeliver, "probe.e3.reloadPaid", null, ThingDefOf.Steel, 50, StoryDeliverTarget.colony, 10f));
        }

        private static HeroDef PickHero(int idx)
        {
            List<HeroDef> all = DefDatabase<HeroDef>.AllDefsListForReading;
            if (all == null || all.Count == 0) return null;
            return all[idx % all.Count];
        }

        private static StoryQuestDef BuildHotelQuest(
            string defName,
            string flag,
            string facDef,
            HeroDef h,
            int groupSize,
            float stayDays,
            float overdueDays,
            StoryHotelObjective obj,
            string hunterFac,
            float hunterDelayHours)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.faction = facDef;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_HotelVisit st = new StoryStage_HotelVisit();
            st.key = "hotel";
            st.label = defName;
            st.text = defName;
            st.faction = facDef;
            st.hero = h;
            st.groupSize = groupSize;
            st.stayDays = stayDays;
            st.overdueDays = overdueDays;
            st.objective = obj;
            st.hunterFaction = hunterFac;
            st.hunterRaidDelayHours = hunterDelayHours;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static StoryQuestDef BuildDeliverQuest(string defName, string flag, string facDef, ThingDef thing, int count, StoryDeliverTarget to, float timeoutDays)
        {
            StoryQuestDef d = new StoryQuestDef();
            d.defName = defName;
            d.label = defName;
            d.act = 1;
            d.main = true;
            d.autoStart = true;
            d.priority = 10;
            d.faction = facDef;
            d.requires = new List<string> { flag + "=true" };
            d.stages = new List<StoryStage>();

            StoryStage_Deliver st = new StoryStage_Deliver();
            st.key = "deliver";
            st.label = defName;
            st.text = defName;
            st.thing = thing;
            st.count = count;
            st.to = to;
            st.timeoutDays = timeoutDays;
            st.outcomes = new List<StoryChoice>();
            d.stages.Add(st);
            return d;
        }

        private static void AddQuestDef(StoryQuestDef d)
        {
            if (d == null || DefDatabase<StoryQuestDef>.GetNamedSilentFail(d.defName) != null) return;
            DefDatabase<StoryQuestDef>.Add(d); // CHECK API
        }

        private static bool HasArg(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Round 2 in-game verification probe for E3 (StoryStage_HotelVisit &amp; StoryStage_Deliver).
    /// Covers every Round 2 finding:
    /// - Hero untouched in WorldPawns when companion generation throws (via TestMode hook);
    /// - Overlapping truce visits on the SAME hostile faction keep Neutral until the LAST visit ends,
    ///   then restore Hostile symmetrically on both FactionRelations;
    /// - Orphan truce rollback via REAL stage removal on StoryQuestDef (not manual status mutation);
    /// - Guest vanished without Pawn.ExitMap -> failed (never false success);
    /// - Hide ignores unrelated hostile lords of hunterFaction and launches/tracks its OWN bound raid,
    ///   reaching hard deadline (not success) when the hunter raid fails to spawn;
    /// - Deliver refuses payment when deadlineTick is missing/unknown;
    /// - Deliver enforces immutable contract terms even if letter fields are tampered with;
    /// - Deliver refuses payment when stageKey was removed from def.stages (zero stale index fallback);
    /// - Deliver atomic rollback on second-stack SplitOff failure AND commit-destroy exception safety;
    /// - Passing-by caravan at giverSettlement ignored, explicit CaravanArrivalAction_StoryDeliver deducted;
    /// - Save/reload preserves active HotelVisit (lord + hero without duplication + peaceful ExitMap)
    ///   and completes paid Deliver without double deduction;
    /// - Honest JSON &amp; UI reports (passed computed from checks/errors) and zero global relation sanitization.
    /// </summary>
    public sealed class InGameStoryHotelProbe : MapComponent
    {
        private sealed class CheckItem
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private static bool s_completed;
        private int m_ticksWaited;
        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        public InGameStoryHotelProbe(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (s_completed) return;
            if (!StoryHotelProbeBootstrap.FreshMode && !StoryHotelProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                if (StoryHotelProbeBootstrap.FreshMode)
                {
                    RunFreshProbe();
                }
                else if (StoryHotelProbeBootstrap.ReloadMode)
                {
                    RunReloadProbe();
                }
            }
            catch (Exception ex)
            {
                m_errors.Add("Unhandled exception: " + ex.ToString());
            }
            finally
            {
                StoryStage_HotelVisit.TestCompanionGenerateOverride = null;
                StoryStage_HotelVisit.TestHunterRaidExecuteOverride = null;
                StoryPayment.TestSplitOffOverride = null;
                StoryPayment.TestBeforeDestroyOverride = null;
                StoryPawnTransfer.TestSpawnOverride = null;
                Application.logMessageReceived -= OnUnityLog;

                bool allPassed = WriteReport(StoryHotelProbeBootstrap.ReloadMode ? "StoryHotelReloadProbe.json" : "StoryHotelProbe.json");
                if (!StoryHotelProbeBootstrap.ReloadMode)
                {
                    WriteUiReport(allPassed);
                }
                Root.Shutdown();
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        // =====================================================================
        // FRESH PROBE
        // =====================================================================

        private void RunFreshProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("SparkStory_Exists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. NO HOTEL IMMEDIATE ON START ----------------
            SetGuestsWelcome(map, false);
            story.SetFlag("probe.e3.noHotel", "true");
            story.DirectorTick();
            StoryQuestRun noHotelRun = story.RunFor(StoryHotelProbeBootstrap.QNoHotel);
            bool noHotelImmediate = noHotelRun != null && GetVar(noHotelRun, "noHotel") == "true" &&
                                    GetVar(noHotelRun, "immediateOutcome") == "noHotel" &&
                                    GetVar(noHotelRun, "arrived") == "false";
            story.DirectorTick();
            bool noHotelFinished = noHotelRun != null && HasOutcome(noHotelRun, "noHotel");
            AddCheck("Compat_NoHotelImmediateOnStart", noHotelImmediate && noHotelFinished,
                "immediate noHotel & 0 spawns",
                string.Format("immediate={0}, finished={1}", noHotelImmediate, noHotelFinished));

            // ---------------- BUILD HOTEL & PLAYER STORAGE ZONE ----------------
            bool hotelBuilt = BuildTestHotel(map);
            SetGuestsWelcome(map, true);
            bool hotelOpen = StoryStage_HotelVisit.HasOpenHotel(map);
            AddCheck("Setup_HotelBuiltAndOpen", hotelBuilt && hotelOpen, "built=true, open=true",
                string.Format("built={0}, open={1}", hotelBuilt, hotelOpen));

            Zone_Stockpile stockpile = EnsureTestStockpile(map);
            AddCheck("Setup_StockpileReady", stockpile != null && stockpile.cells.Count >= 9, ">=9 cells",
                stockpile != null ? stockpile.cells.Count.ToString() : "null");

            // ---------------- 2. HERO-COLONIST IS NOT STOLEN ----------------
            StoryQuestDef heroSkipDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHeroColonistSkip);
            StoryStage_HotelVisit heroSkipStage = heroSkipDef != null ? heroSkipDef.stages[0] as StoryStage_HotelVisit : null;
            Pawn colonistHero = (heroSkipStage != null && heroSkipStage.hero != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(heroSkipStage.hero) : null;
            if (colonistHero != null)
            {
                if (colonistHero.Spawned) colonistHero.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns.Contains(colonistHero)) Find.WorldPawns.RemovePawn(colonistHero);
                colonistHero.SetFaction(Faction.OfPlayer);
                IntVec3 origCell = FindStandable(map);
                GenSpawn.Spawn(colonistHero, origCell, map, WipeMode.Vanish);

                story.SetFlag("probe.e3.heroColonist", "true");
                story.DirectorTick();
                StoryQuestRun skipRun = story.RunFor(StoryHotelProbeBootstrap.QHeroColonistSkip);

                bool heroSkipped = skipRun != null && GetVar(skipRun, "heroIncluded") == "false";
                bool heroUntouched = colonistHero.Faction == Faction.OfPlayer &&
                                     colonistHero.Spawned && colonistHero.Map == map &&
                                     colonistHero.Position == origCell &&
                                     !ParseIntList(GetVar(skipRun, "guestIds")).Contains(colonistHero.thingIDNumber);
                AddCheck("P1_HeroColonistNotMoved", heroSkipped && heroUntouched,
                    "heroIncluded=false & colonist untouched",
                    string.Format("skipped={0}, reason={1}, untouched={2}", heroSkipped, GetVar(skipRun, "heroSkipReason"), heroUntouched));

                CleanupActiveHotelRun(story, heroSkipDef, skipRun);
            }
            else
            {
                AddCheck("P1_HeroColonistNotMoved", false, "hero exists", "hero null");
            }

            // ---------------- 3. HERO UNTOUCHED ON COMPANION GENERATION EXCEPTION (TestMode) ----------------
            if (TestMode.Enabled)
            {
                StoryQuestDef compExDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHeroCompanionException);
                StoryStage_HotelVisit compExStage = compExDef != null ? compExDef.stages[0] as StoryStage_HotelVisit : null;
                Pawn worldHero = (compExStage != null && compExStage.hero != null && HeroRegistry.Instance != null)
                    ? HeroRegistry.Instance.GetOrCreate(compExStage.hero) : null;

                if (worldHero != null && Find.WorldPawns.Contains(worldHero) && !worldHero.Spawned)
                {
                    Faction heroOrigFac = worldHero.Faction;
                    if (heroOrigFac != null && !heroOrigFac.IsPlayer && !heroOrigFac.defeated &&
                        (heroOrigFac.def == null || !heroOrigFac.def.permanentEnemy))
                    {
                        compExStage.faction = heroOrigFac.def.defName;
                    }

                    StoryStage_HotelVisit.TestCompanionGenerateOverride = delegate(PawnGenerationRequest req)
                    {
                        throw new InvalidOperationException("Simulated companion generation exception");
                    };

                    story.SetFlag("probe.e3.heroCompEx", "true");
                    story.DirectorTick();
                    StoryStage_HotelVisit.TestCompanionGenerateOverride = null;
                    story.DirectorTick();

                    StoryQuestRun exRun = story.RunFor(StoryHotelProbeBootstrap.QHeroCompanionException);
                    bool heroStillInWorld = Find.WorldPawns.Contains(worldHero) && !worldHero.Spawned && worldHero.Faction == heroOrigFac;
                    bool failedCleanly = exRun != null && HasOutcome(exRun, "failed") && GetVar(exRun, "arrived") == "false";

                    AddCheck("R2_HotelCompanionException_HeroUntouchedInWorldPawns",
                        heroStillInWorld && failedCleanly,
                        "hero in WorldPawns with origFaction, outcome=failed",
                        string.Format("inWorld={0}, facMatch={1}, hist={2}",
                            Find.WorldPawns.Contains(worldHero), worldHero.Faction == heroOrigFac,
                            exRun != null ? string.Join(",", exRun.history.ToArray()) : "null"));
                }
                else
                {
                    AddCheck("R2_HotelCompanionException_HeroUntouchedInWorldPawns", false, "free world hero exists", "not free");
                }
            }

            // ---------------- 4. PERMANENT ENEMY FACTION -> FAILED WITHOUT SPAWN ----------------
            Faction permEnemy = FindPermanentEnemyHumanlikeFaction();
            StoryQuestDef permDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QPermanentEnemy);
            StoryStage_HotelVisit permStage = permDef != null ? permDef.stages[0] as StoryStage_HotelVisit : null;
            if (permEnemy != null && permStage != null)
            {
                permStage.faction = permEnemy.def.defName;
                int pawnsBefore = map.mapPawns.AllPawnsSpawnedCount;

                story.SetFlag("probe.e3.permEnemy", "true");
                story.DirectorTick();
                story.DirectorTick();

                StoryQuestRun permRun = story.RunFor(StoryHotelProbeBootstrap.QPermanentEnemy);
                int pawnsAfter = map.mapPawns.AllPawnsSpawnedCount;
                bool failedNoSpawn = permRun != null && HasOutcome(permRun, "failed") &&
                                     GetVar(permRun, "arrived") == "false" && pawnsAfter == pawnsBefore;
                AddCheck("P1_PermanentEnemyFailsWithoutSpawn", failedNoSpawn,
                    "outcome=failed, arrived=false, 0 spawned",
                    string.Format("hist={0}, arrived={1}, pawns={2}->{3}",
                        permRun != null ? string.Join(",", permRun.history.ToArray()) : "null",
                        GetVar(permRun, "arrived"), pawnsBefore, pawnsAfter));
            }
            else
            {
                AddCheck("P1_PermanentEnemyFailsWithoutSpawn", false, "permanentEnemy faction exists", "missing");
            }

            // ---------------- 5. OVERLAPPING TRUCE VISITS ON THE SAME HOSTILE FACTION ----------------
            Faction goodwillFac = FindGoodwillHumanlikeFaction();
            StoryQuestDef overlapADef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceOverlapA);
            StoryQuestDef overlapBDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceOverlapB);
            StoryStage_HotelVisit overlapAStage = overlapADef != null ? overlapADef.stages[0] as StoryStage_HotelVisit : null;
            StoryStage_HotelVisit overlapBStage = overlapBDef != null ? overlapBDef.stages[0] as StoryStage_HotelVisit : null;

            if (goodwillFac != null && overlapAStage != null && overlapBStage != null)
            {
                ForceHostileWithGoodwill(goodwillFac, -85);
                overlapAStage.faction = goodwillFac.def.defName;
                overlapBStage.faction = goodwillFac.def.defName;

                // Start Visit A -> faction enters truce (Neutral)
                story.SetFlag("probe.e3.truceOverlapA", "true");
                story.DirectorTick();
                StoryQuestRun runA = story.RunFor(StoryHotelProbeBootstrap.QTruceOverlapA);
                bool aTruceActive = !goodwillFac.HostileTo(Faction.OfPlayer) && !GetVar(runA, "truceLeaseId").NullOrEmpty();

                // Start Visit B while faction ALREADY appears Neutral -> MUST acquire its own counted lease!
                story.SetFlag("probe.e3.truceOverlapB", "true");
                story.DirectorTick();
                StoryQuestRun runB = story.RunFor(StoryHotelProbeBootstrap.QTruceOverlapB);
                bool bGotLease = runB != null && GetVar(runB, "arrived") == "true" && !GetVar(runB, "truceLeaseId").NullOrEmpty();

                // End Visit A peacefully via real ExitMap -> faction MUST STILL BE NEUTRAL for Visit B!
                List<int> guestsA = ParseIntList(GetVar(runA, "guestIds"));
                ExitGuestsViaRealExitMap(map, guestsA);
                story.DirectorTick();
                bool aSucceeded = runA != null && HasOutcome(runA, "success");
                bool stillNeutralAfterA = !goodwillFac.HostileTo(Faction.OfPlayer) && !Faction.OfPlayer.HostileTo(goodwillFac) &&
                                          StoryRelationRestorer.Instance != null && StoryRelationRestorer.Instance.HasActiveTruceBackup(goodwillFac);

                // Now end Visit B peacefully via real ExitMap -> faction MUST RESTORE HOSTILE symmetrically!
                List<int> guestsB = ParseIntList(GetVar(runB, "guestIds"));
                ExitGuestsViaRealExitMap(map, guestsB);
                story.DirectorTick();
                bool bSucceeded = runB != null && HasOutcome(runB, "success");
                bool restoredSymHostile = goodwillFac.HostileTo(Faction.OfPlayer) && Faction.OfPlayer.HostileTo(goodwillFac) &&
                                          goodwillFac.PlayerGoodwill <= -80 && goodwillFac.PlayerGoodwill >= -90 &&
                                          Faction.OfPlayer.GoodwillWith(goodwillFac) == goodwillFac.PlayerGoodwill;

                AddCheck("R2_OverlappingTruceVisits_StaysNeutralUntilLastEnds",
                    aTruceActive && bGotLease && aSucceeded && stillNeutralAfterA && bSucceeded && restoredSymHostile,
                    "A+B leased, neutral after A, symmetric Hostile(~-85) after B",
                    string.Format("aTruce={0}, bLease={1}, aSucc={2}, neutralAfterA={3}, bSucc={4}, symHostile={5}, gw={6}/{7}",
                        aTruceActive, bGotLease, aSucceeded, stillNeutralAfterA, bSucceeded, restoredSymHostile,
                        goodwillFac.PlayerGoodwill, Faction.OfPlayer.GoodwillWith(goodwillFac)));
            }
            else
            {
                AddCheck("R2_OverlappingTruceVisits_StaysNeutralUntilLastEnds", false, "goodwill faction & defs exist", "missing");
            }

            // ---------------- 6. ORPHAN TRUCE ROLLBACK VIA REAL STAGE REMOVAL ----------------
            StoryQuestDef orphanDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QTruceStageRemovedOrphan);
            StoryStage_HotelVisit orphanStage = orphanDef != null && orphanDef.stages.Count > 0 ? orphanDef.stages[0] as StoryStage_HotelVisit : null;
            if (goodwillFac != null && orphanDef != null && orphanStage != null)
            {
                ForceHostileWithGoodwill(goodwillFac, -88);
                orphanStage.faction = goodwillFac.def.defName;

                story.SetFlag("probe.e3.truceStageRemoved", "true");
                story.DirectorTick();
                StoryQuestRun orphRun = story.RunFor(StoryHotelProbeBootstrap.QTruceStageRemovedOrphan);
                bool enteredTruce = !goodwillFac.HostileTo(Faction.OfPlayer) && GetVar(orphRun, "arrived") == "true";
                List<int> orphGuests = ParseIntList(GetVar(orphRun, "guestIds"));

                // REAL stage removal: empty orphanDef.stages and tick SparkStory!
                try
                {
                    orphanDef.stages.Clear();
                    story.DirectorTick();
                }
                finally
                {
                    orphanDef.stages.Add(orphanStage);
                }

                bool disabledNaturally = orphRun != null && orphRun.status == StoryQuestStatus.Disabled &&
                                         HasOutcome(orphRun, "stage missing");
                bool restoredSym = goodwillFac.HostileTo(Faction.OfPlayer) && Faction.OfPlayer.HostileTo(goodwillFac) &&
                                   goodwillFac.PlayerGoodwill <= -83 && goodwillFac.PlayerGoodwill >= -93;

                AddCheck("R2_TruceOrphan_RealStageRemovalRollback",
                    enteredTruce && disabledNaturally && restoredSym,
                    "truce->Disable(stage missing)->Symmetric Hostile(~-88)",
                    string.Format("truce={0}, disabled={1}, symHostile={2}, gw={3}",
                        enteredTruce, disabledNaturally, restoredSym, goodwillFac.PlayerGoodwill));

                CleanupPawnsByIds(map, orphGuests);
                ForceNeutralWithGoodwill(goodwillFac, 10);
            }
            else
            {
                AddCheck("R2_TruceOrphan_RealStageRemovalRollback", false, "goodwill faction & def exist", "missing");
            }

            // ---------------- 7. GUEST KILLED, IMPRISONED, OR VANISHED WITHOUT EXITMAP -> FAILED ----------------
            // 7A. Killed
            story.SetFlag("probe.e3.hostKilled", "true");
            story.DirectorTick();
            StoryQuestRun killRun = story.RunFor(StoryHotelProbeBootstrap.QHostKilled);
            List<int> killGuests = ParseIntList(GetVar(killRun, "guestIds"));
            KillFirstGuest(map, killGuests);
            story.DirectorTick();
            AddCheck("P1_GuestKilled_NotSuccess",
                killRun != null && HasOutcome(killRun, "failed") && !HasOutcome(killRun, "success"),
                "failed", killRun != null ? string.Join(",", killRun.history.ToArray()) : "null");
            CleanupPawnsByIds(map, killGuests);

            // 7B. Imprisoned
            story.SetFlag("probe.e3.hostImprisoned", "true");
            story.DirectorTick();
            StoryQuestRun prisRun = story.RunFor(StoryHotelProbeBootstrap.QHostImprisoned);
            List<int> prisGuests = ParseIntList(GetVar(prisRun, "guestIds"));
            Pawn firstPrisGuest = FindGuestOnMap(map, prisGuests);
            if (firstPrisGuest != null && firstPrisGuest.guest != null)
            {
                firstPrisGuest.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner); // CHECK API
            }
            story.DirectorTick();
            AddCheck("P1_GuestImprisoned_NotSuccess",
                prisRun != null && HasOutcome(prisRun, "failed") && !HasOutcome(prisRun, "success"),
                "failed", prisRun != null ? string.Join(",", prisRun.history.ToArray()) : "null");
            CleanupPawnsByIds(map, prisGuests);

            // 7C. Vanished without Pawn.ExitMap -> MUST fail (guest_missing), NEVER success
            story.SetFlag("probe.e3.hostVanished", "true");
            story.DirectorTick();
            StoryQuestRun vanRun = story.RunFor(StoryHotelProbeBootstrap.QHostVanishedNoExit);
            List<int> vanGuests = ParseIntList(GetVar(vanRun, "guestIds"));
            Pawn vanGuest = FindGuestOnMap(map, vanGuests);
            if (vanGuest != null && vanGuest.Spawned)
            {
                vanGuest.DeSpawn(DestroyMode.Vanish);
                if (!Find.WorldPawns.Contains(vanGuest)) Find.WorldPawns.PassToWorld(vanGuest, PawnDiscardDecideMode.Discard);
            }
            story.DirectorTick();
            AddCheck("R2_GuestVanishedWithoutExitMap_FailedNotSuccess",
                vanRun != null && HasOutcome(vanRun, "failed") && !HasOutcome(vanRun, "success") && GetVar(vanRun, "failReason") == "guest_missing",
                "failed (guest_missing)",
                string.Format("hist={0}, reason={1}", vanRun != null ? string.Join(",", vanRun.history.ToArray()) : "null", GetVar(vanRun, "failReason")));

            // ---------------- 8. INVESTIGATE REQUIRES REAL PlayLogEntry_Interaction ----------------
            StoryQuestDef invDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QInvestigateStrict);
            story.SetFlag("probe.e3.investigate", "true");
            story.DirectorTick();
            StoryQuestRun invRun = story.RunFor(StoryHotelProbeBootstrap.QInvestigateStrict);
            List<int> invGuests = ParseIntList(GetVar(invRun, "guestIds"));
            Pawn invGuest = FindGuestOnMap(map, invGuests);

            Pawn talker = SpawnCleanColonist(map);
            if (talker.skills != null)
            {
                SkillRecord soc = talker.skills.GetSkill(SkillDefOf.Social);
                if (soc != null) soc.Level = 15;
            }
            if (invGuest != null) talker.Position = invGuest.Position;
            story.DirectorTick();
            bool notInvestigatedWithoutChat = GetVar(invRun, "investigated") == "false";

            bool realChatDetected = false;
            if (invGuest != null && Find.PlayLog != null)
            {
                PlayLogEntry_Interaction entry = new PlayLogEntry_Interaction(InteractionDefOf.Chitchat, talker, invGuest, null); // CHECK API
                Find.PlayLog.Add(entry);
                story.DirectorTick();
                realChatDetected = GetVar(invRun, "investigated") == "true";
            }
            AddCheck("P1_InvestigateRequiresRealInteraction", notInvestigatedWithoutChat && realChatDetected,
                "withoutChat=false, afterRealChat=true",
                string.Format("noChatFalse={0}, afterChat={1}", notInvestigatedWithoutChat, realChatDetected));
            CleanupActiveHotelRun(story, invDef, invRun);

            // ---------------- 9. HIDE: STAGE-OWNED HUNTER RAID IGNORES UNRELATED HOSTILE LORDS ----------------
            Faction hunterFac = permEnemy ?? FindAnotherHostileFaction(goodwillFac);
            StoryQuestDef hideOwnedDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHideOwnedRaid);
            StoryStage_HotelVisit hideOwnedStage = hideOwnedDef != null ? hideOwnedDef.stages[0] as StoryStage_HotelVisit : null;
            if (hunterFac != null && hideOwnedStage != null)
            {
                hideOwnedStage.hunterFaction = hunterFac.def.defName;
                story.SetFlag("probe.e3.hideOwned", "true");
                story.DirectorTick();
                StoryQuestRun hideRun = story.RunFor(StoryHotelProbeBootstrap.QHideOwnedRaid);
                List<int> hideGuests = ParseIntList(GetVar(hideRun, "guestIds"));

                // Spawn an UNRELATED hostile lord of hunterFac BEFORE hunterRaidTick and kill it!
                Pawn unrelatedRaider = SpawnCleanNpc(map, hunterFac);
                Lord unrelatedLord = LordMaker.MakeNewLord(hunterFac, new LordJob_DefendPoint(unrelatedRaider.Position), map, new List<Pawn> { unrelatedRaider });
                story.DirectorTick();
                bool unrelatedNotBound = GetVar(hideRun, "hunterRaidBound") == "false" && GetVar(hideRun, "hunterRaidLaunched") == "false";
                if (!unrelatedRaider.Dead) unrelatedRaider.Kill(null);
                story.DirectorTick();
                bool unrelatedKillNotCleared = GetVar(hideRun, "hunterRaidCleared") == "false";

                // Advance to hunterRaidTick -> Hide launches and binds ITS OWN raid!
                int raidTick = ParseInt(hideRun, "hunterRaidTick", Find.TickManager.TicksGame);
                Find.TickManager.DebugSetTicksGame(raidTick + 10); // CHECK API
                story.DirectorTick();

                bool ownRaidBound = GetVar(hideRun, "hunterRaidLaunched") == "true" &&
                                    GetVar(hideRun, "hunterRaidBound") == "true" &&
                                    ParseIntList(GetVar(hideRun, "hunterRaidPawnIds")).Count > 0;
                List<int> boundRaiders = ParseIntList(GetVar(hideRun, "hunterRaidPawnIds"));

                // Kill ONLY the stage's bound raiders -> hunterRaidCleared becomes true!
                KillPawnsByIds(map, boundRaiders);
                story.DirectorTick();
                bool ownRaidCleared = GetVar(hideRun, "hunterRaidCleared") == "true";

                // Now guests depart peacefully via ExitMap -> Hide succeeds!
                ExitGuestsViaRealExitMap(map, hideGuests);
                story.DirectorTick();
                bool hideSucceeded = hideRun != null && HasOutcome(hideRun, "success");

                AddCheck("R2_Hide_IgnoresUnrelatedAndOwnsItsHunterRaid",
                    unrelatedNotBound && unrelatedKillNotCleared && ownRaidBound && ownRaidCleared && hideSucceeded,
                    "unrelated ignored, own raid bound->cleared->success",
                    string.Format("unrelIgnored={0}/{1}, ownBound={2}({3}), cleared={4}, succ={5}",
                        unrelatedNotBound, unrelatedKillNotCleared, ownRaidBound, boundRaiders.Count, ownRaidCleared, hideSucceeded));
            }
            else
            {
                AddCheck("R2_Hide_IgnoresUnrelatedAndOwnsItsHunterRaid", false, "hunterFac exists", "missing");
            }

            // ---------------- 10. HIDE: FAILED HUNTER RAID REACHES HARD DEADLINE (TestMode) ----------------
            if (TestMode.Enabled && hunterFac != null)
            {
                StoryQuestDef hideFailDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QHideFailedRaidDeadline);
                StoryStage_HotelVisit hideFailStage = hideFailDef != null ? hideFailDef.stages[0] as StoryStage_HotelVisit : null;
                if (hideFailStage != null)
                {
                    hideFailStage.hunterFaction = hunterFac.def.defName;
                    StoryStage_HotelVisit.TestHunterRaidExecuteOverride = delegate(IncidentParms p) { return false; };

                    story.SetFlag("probe.e3.hideFailed", "true");
                    story.DirectorTick();
                    StoryQuestRun hfRun = story.RunFor(StoryHotelProbeBootstrap.QHideFailedRaidDeadline);
                    List<int> hfGuests = ParseIntList(GetVar(hfRun, "guestIds"));

                    int rTick = ParseInt(hfRun, "hunterRaidTick", Find.TickManager.TicksGame);
                    Find.TickManager.DebugSetTicksGame(rTick + 10);
                    story.DirectorTick();
                    StoryStage_HotelVisit.TestHunterRaidExecuteOverride = null;

                    int hardDl = ParseInt(hfRun, "hardDeadlineTick", Find.TickManager.TicksGame + GenDate.TicksPerDay);
                    Find.TickManager.DebugSetTicksGame(hardDl + 50);
                    story.DirectorTick();

                    bool failedAtDeadline = hfRun != null && !HasOutcome(hfRun, "success") &&
                                            (HasOutcome(hfRun, "failed") || HasOutcome(hfRun, "timeout")) &&
                                            GetVar(hfRun, "hunterRaidBound") == "false";
                    AddCheck("R2_Hide_FailedRaidReachesDeadlineNotSuccess", failedAtDeadline,
                        "failed/timeout & hunterRaidBound=false",
                        string.Format("hist={0}, bound={1}, reason={2}",
                            hfRun != null ? string.Join(",", hfRun.history.ToArray()) : "null",
                            GetVar(hfRun, "hunterRaidBound"), GetVar(hfRun, "failReason")));
                    CleanupPawnsByIds(map, hfGuests);
                }
            }

            // ---------------- 11. DELIVER: EXPIRED LETTER CLICK DEDUCTS NOTHING ----------------
            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 40, 0);
            int steelBeforeExpired = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            story.SetFlag("probe.e3.deliverExpired", "true");
            story.DirectorTick();
            StoryQuestRun expRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverExpiredClick);
            ChoiceLetter_StoryDeliver expLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverExpiredClick);

            int expDl = ParseInt(expRun, "deadlineTick", Find.TickManager.TicksGame + GenDate.TicksPerDay);
            Find.TickManager.DebugSetTicksGame(expDl + 50); // CHECK API

            bool uiExpiredRefused = false;
            if (expLetter != null)
            {
                ClickFirstEnabledOption(expLetter);
                uiExpiredRefused = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel) == steelBeforeExpired &&
                                   GetVar(expRun, "delivered") != "true";
            }
            story.DirectorTick();
            bool expTimedOut = expRun != null && HasOutcome(expRun, "timeout");
            AddCheck("P1_DeliverExpiredClick_DeductsNothing",
                expLetter != null && uiExpiredRefused && expTimedOut,
                "steel unchanged, delivered!=true, timeout",
                string.Format("letter={0}, refused={1}, timeout={2}", expLetter != null, uiExpiredRefused, expTimedOut));

            // ---------------- 12. DELIVER: UNKNOWN/MISSING deadlineTick ALWAYS REFUSES PAYMENT ----------------
            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 40, 0);
            story.SetFlag("probe.e3.deliverUnknownDl", "true");
            story.DirectorTick();
            StoryQuestRun unkRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverUnknownDeadline);
            ChoiceLetter_StoryDeliver unkLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverUnknownDeadline);
            if (unkRun != null && unkLetter != null)
            {
                // Remove deadlineTick from run.vars while letter still carries a future deadlineTick!
                unkRun.vars.Remove("deadlineTick");
                unkLetter.deadlineTick = Find.TickManager.TicksGame + GenDate.TicksPerDay * 5;

                StoryQuestRun vRun;
                Map vMap;
                ThingDef vDef;
                int vCount;
                string vReason;
                bool validated = StoryStage_Deliver.ValidateColonyHandIn(unkLetter, out vRun, out vMap, out vDef, out vCount, out vReason);
                ClickFirstEnabledOption(unkLetter);
                int steelAfter = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                story.DirectorTick();

                bool refusedAndTimedOut = !validated && vReason == "deadline_missing_or_passed" &&
                                          steelAfter == 40 && HasOutcome(unkRun, "timeout");
                AddCheck("R2_Deliver_UnknownDeadlineAlwaysRefuses", refusedAndTimedOut,
                    "validated=false(deadline_missing_or_passed), steel=40, timeout",
                    string.Format("val={0}({1}), steel={2}, hist={3}",
                        validated, vReason ?? "null", steelAfter, string.Join(",", unkRun.history.ToArray())));
            }
            else
            {
                AddCheck("R2_Deliver_UnknownDeadlineAlwaysRefuses", false, "run & letter created", "missing");
            }

            // ---------------- 13. DELIVER: IMMUTABLE CONTRACT IGNORES TAMPERED LETTER FIELDS ----------------
            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 50, 0);
            story.SetFlag("probe.e3.deliverImmutable", "true");
            story.DirectorTick();
            StoryQuestRun immRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverImmutableContract);
            ChoiceLetter_StoryDeliver immLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverImmutableContract);
            if (immRun != null && immLetter != null)
            {
                // Tamper with letter fields (1 WoodLog instead of 30 Steel)
                immLetter.requiredCount = 1;
                immLetter.thingDefName = ThingDefOf.WoodLog.defName;
                immLetter.deliverMapId = 999999;

                bool clicked = ClickFirstEnabledOption(immLetter);
                int steelAfter = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                story.DirectorTick();

                bool enforcedContract = clicked && steelAfter == 20 && HasOutcome(immRun, "success");
                AddCheck("R2_Deliver_ImmutableContractIgnoresLetterTamper", enforcedContract,
                    "deducted 30 Steel (50->20), outcome=success",
                    string.Format("clicked={0}, steelAfter={1}, hist={2}", clicked, steelAfter, string.Join(",", immRun.history.ToArray())));
            }
            else
            {
                AddCheck("R2_Deliver_ImmutableContractIgnoresLetterTamper", false, "run & letter created", "missing");
            }

            // ---------------- 14. DELIVER: REMOVED stageKey REFUSES (NO STALE INDEX FALLBACK) ----------------
            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 50, 0);
            StoryQuestDef keyRemDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QDeliverStageKeyRemoved);
            StoryStage origDeliverStage = keyRemDef != null && keyRemDef.stages.Count > 0 ? keyRemDef.stages[0] : null;
            story.SetFlag("probe.e3.deliverKeyRemoved", "true");
            story.DirectorTick();
            StoryQuestRun keyRemRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverStageKeyRemoved);
            ChoiceLetter_StoryDeliver keyRemLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverStageKeyRemoved);
            if (keyRemDef != null && origDeliverStage != null && keyRemRun != null && keyRemLetter != null)
            {
                StoryStage_Deliver replacement = new StoryStage_Deliver();
                replacement.key = "different_stage_key";
                replacement.thing = ThingDefOf.Steel;
                replacement.count = 10;
                replacement.to = StoryDeliverTarget.colony;

                bool refusedOnRemovedKey = false;
                string valReason = null;
                int steelAfterClick = -1;
                try
                {
                    // Replace index 0 with a DIFFERENT stageKey (run.stage is still 0, but "deliver" is gone!)
                    keyRemDef.stages[0] = replacement;
                    StoryQuestRun vRun;
                    Map vMap;
                    ThingDef vDef;
                    int vCount;
                    bool valid = StoryStage_Deliver.ValidateColonyHandIn(keyRemLetter, out vRun, out vMap, out vDef, out vCount, out valReason);
                    ClickFirstEnabledOption(keyRemLetter);
                    steelAfterClick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                    refusedOnRemovedKey = !valid && valReason == "stage_key_removed" && steelAfterClick == 50 && GetVar(keyRemRun, "delivered") != "true";
                }
                finally
                {
                    keyRemDef.stages[0] = origDeliverStage;
                    // End the test quest cleanly
                    if (keyRemLetter.IsInLetterStack) Find.LetterStack.RemoveLetter(keyRemLetter);
                    int dl = ParseInt(keyRemRun, "deadlineTick", Find.TickManager.TicksGame);
                    Find.TickManager.DebugSetTicksGame(dl + 10);
                    story.DirectorTick();
                }

                AddCheck("R2_Deliver_RemovedStageKeyRefusesWithoutIndexFallback", refusedOnRemovedKey,
                    "valid=false(stage_key_removed), steel=50",
                    string.Format("refused={0}, reason={1}, steel={2}", refusedOnRemovedKey, valReason ?? "null", steelAfterClick));
            }
            else
            {
                AddCheck("R2_Deliver_RemovedStageKeyRefusesWithoutIndexFallback", false, "def/run/letter exist", "missing");
            }

            // ---------------- 15. DELIVER: ATOMIC ROLLBACK ON SECOND STACK FAILURE (TestMode) ----------------
            if (TestMode.Enabled)
            {
                ClearStockpileResources(map, ThingDefOf.Steel);
                PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 30, 0);
                PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 30, 1);
                int steelBeforeAtomic = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

                story.SetFlag("probe.e3.deliverAtomic", "true");
                story.DirectorTick();
                StoryQuestRun atomicRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverSecondStackFail);
                ChoiceLetter_StoryDeliver atomicLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverSecondStackFail);

                StoryPayment.TestSplitOffOverride = delegate(Thing src, int count, int stepIndex)
                {
                    if (stepIndex == 1) return null;
                    return src.SplitOff(count);
                };

                bool clickedAtomic = atomicLetter != null && ClickFirstEnabledOption(atomicLetter);
                StoryPayment.TestSplitOffOverride = null;

                int steelAfterAtomicFail = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
                bool atomicRolledBack = clickedAtomic && steelBeforeAtomic == 60 && steelAfterAtomicFail == 60 &&
                                        GetVar(atomicRun, "delivered") != "true" &&
                                        atomicLetter != null && atomicLetter.IsInLetterStack;
                AddCheck("P1_DeliverAtomicRollback_SecondStackFail", atomicRolledBack,
                    "steel=60->60, delivered!=true, letter stays",
                    string.Format("clicked={0}, steel={1}->{2}, delivered={3}",
                        clickedAtomic, steelBeforeAtomic, steelAfterAtomicFail, GetVar(atomicRun, "delivered")));

                // Also test commit-phase Destroy exception safety (onPaymentSecured already recorded delivered=true)
                ClearStockpileResources(map, ThingDefOf.Steel);
                PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 40, 0);
                story.SetFlag("probe.e3.deliverCommitEx", "true");
                story.DirectorTick();
                StoryQuestRun commitRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverCommitDestroyEx);
                ChoiceLetter_StoryDeliver commitLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QDeliverCommitDestroyEx);

                StoryPayment.TestBeforeDestroyOverride = delegate(Thing t, int idx)
                {
                    throw new InvalidOperationException("Simulated Destroy exception after onPaymentSecured");
                };
                bool clickedCommit = commitLetter != null && ClickFirstEnabledOption(commitLetter);
                StoryPayment.TestBeforeDestroyOverride = null;
                story.DirectorTick();

                bool commitSafe = clickedCommit && commitRun != null && HasOutcome(commitRun, "success") &&
                                  StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel) == 20;
                AddCheck("R2_Deliver_CommitDestroyExceptionKeepsRecordedPayment", commitSafe,
                    "outcome=success, remaining steel=20",
                    string.Format("clicked={0}, steel={1}, hist={2}",
                        clickedCommit, StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel),
                        commitRun != null ? string.Join(",", commitRun.history.ToArray()) : "null"));
            }

            // ---------------- 16. DELIVER giverSettlement: PASSING IGNORED, ARRIVAL ACTION DEDUCTED ----------------
            Settlement targetSettlement = FindAnyNonPlayerSettlement();
            StoryQuestDef carDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail(StoryHotelProbeBootstrap.QDeliverCaravanSettlement);
            if (targetSettlement != null && targetSettlement.Faction != null && carDef != null)
            {
                carDef.faction = targetSettlement.Faction.def.defName;
                story.SetFlag("probe.e3.deliverCaravan", "true");
                story.DirectorTick();
                StoryQuestRun carRun = story.RunFor(StoryHotelProbeBootstrap.QDeliverCaravanSettlement);

                int pinnedSettlementId = ParseInt(carRun, "settlementId", -1);
                Settlement exactSettlement = FindSettlementById(pinnedSettlementId);

                if (exactSettlement != null)
                {
                    Pawn carrier = SpawnCleanColonist(map);
                    Thing cargo = ThingMaker.MakeThing(ThingDefOf.Steel);
                    cargo.stackCount = 40;
                    carrier.inventory.innerContainer.TryAdd(cargo);
                    carrier.DeSpawn(DestroyMode.Vanish);

                    Caravan car = CaravanMaker.MakeCaravan(new List<Pawn> { carrier }, Faction.OfPlayer, exactSettlement.Tile, true); // CHECK API
                    story.DirectorTick();
                    story.DirectorTick();

                    int cargoAfterPassing = StoryPayment.CountInCaravanDirectInventory(car, ThingDefOf.Steel);
                    bool passingNotDeducted = cargoAfterPassing == 40 && carRun.status == StoryQuestStatus.Active &&
                                              GetVar(carRun, "delivered") != "true";

                    CaravanArrivalAction_StoryDeliver action = new CaravanArrivalAction_StoryDeliver(
                        exactSettlement, carRun.quest, carRun.stageKey, carRun.gen);
                    bool stillValid = action.StillValid(car, exactSettlement.Tile);
                    action.Arrived(car);
                    story.DirectorTick();

                    int cargoAfterArrival = StoryPayment.CountInCaravanDirectInventory(car, ThingDefOf.Steel);
                    bool arrivedDeducted = stillValid && cargoAfterArrival == 0 && HasOutcome(carRun, "success");

                    AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted",
                        passingNotDeducted && arrivedDeducted,
                        "passing=40(active), arrived=0(success)",
                        string.Format("passingOk={0}(cargo={1}), valid={2}, afterCargo={3}, hist={4}",
                            passingNotDeducted, cargoAfterPassing, stillValid, cargoAfterArrival,
                            string.Join(",", carRun.history.ToArray())));
                    DestroyTestCaravanAndPawns(car);
                }
                else
                {
                    AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted", false, "exact settlement pinned", "settlement null");
                }
            }
            else
            {
                AddCheck("P2_CaravanDeliver_PassingIgnoredArrivedDeducted", false, "non-player settlement exists", "missing");
            }

            // ---------------- 17. PREPARE SAVE FOR RELOAD: ACTIVE VISIT + PAID DELIVER ----------------
            story.SetFlag("probe.e3.reloadVisit", "true");
            story.DirectorTick();
            StoryQuestRun reloadVisitRun = story.RunFor(StoryHotelProbeBootstrap.QReloadActiveVisit);
            bool visitSavedReady = reloadVisitRun != null && reloadVisitRun.status == StoryQuestStatus.Active &&
                                   GetVar(reloadVisitRun, "arrived") == "true" &&
                                   ParseInt(reloadVisitRun, "visitLordId", -1) >= 0;
            AddCheck("ReloadPrep_ActiveHotelVisitSaved", visitSavedReady, "active visit with lordId>=0",
                string.Format("arrived={0}, lordId={1}, heroIncluded={2}",
                    GetVar(reloadVisitRun, "arrived"), GetVar(reloadVisitRun, "visitLordId"), GetVar(reloadVisitRun, "heroIncluded")));

            ClearStockpileResources(map, ThingDefOf.Steel);
            PlaceInStockpile(map, stockpile, ThingDefOf.Steel, 100, 0);
            story.SetFlag("probe.e3.reloadPaid", "true");
            story.DirectorTick();
            StoryQuestRun reloadPaidRun = story.RunFor(StoryHotelProbeBootstrap.QReloadPaidDeliver);
            ChoiceLetter_StoryDeliver reloadPaidLetter = FindDeliverLetter(StoryHotelProbeBootstrap.QReloadPaidDeliver);

            // Click Deliver (deducts 50 -> leaves 50, sets delivered=true) BEFORE DirectorTick and save!
            bool paidBeforeSave = reloadPaidLetter != null && ClickFirstEnabledOption(reloadPaidLetter);
            int steelLeftAtSave = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            bool paidSavedReady = paidBeforeSave && steelLeftAtSave == 50 && GetVar(reloadPaidRun, "delivered") == "true";
            AddCheck("ReloadPrep_PaidDeliverSavedBeforeTransition", paidSavedReady, "paid=true, steelLeft=50, delivered=true",
                string.Format("paid={0}, steel={1}, delivered={2}", paidBeforeSave, steelLeftAtSave, GetVar(reloadPaidRun, "delivered")));

            GameDataSaveLoader.SaveGame(StoryHotelProbeBootstrap.SaveName); // CHECK API
        }

        // =====================================================================
        // RELOAD PROBE
        // =====================================================================

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            // ---------------- 1. ACTIVE HOTEL VISIT AFTER RELOAD -> PEACEFUL EXIT ONCE ----------------
            StoryQuestRun visitRun = story.RunFor(StoryHotelProbeBootstrap.QReloadActiveVisit);
            if (visitRun != null && visitRun.status == StoryQuestStatus.Active)
            {
                int savedMapId = ParseInt(visitRun, "visitMapId", -1);
                int savedLordId = ParseInt(visitRun, "visitLordId", -1);
                List<int> savedGuestIds = ParseIntList(GetVar(visitRun, "guestIds"));
                string heroThingId = GetVar(visitRun, "heroThingId");

                Lord restoredLord = FindLordById(map, savedLordId);
                bool mapMatches = savedMapId == map.uniqueID;
                bool lordRestored = restoredLord != null && restoredLord.ownedPawns != null &&
                                    restoredLord.ownedPawns.Count == savedGuestIds.Count;

                int heroMatchesOnMap = 0;
                if (!heroThingId.NullOrEmpty())
                {
                    IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
                    for (int i = 0; i < allPawns.Count; i++)
                    {
                        if (allPawns[i] != null && allPawns[i].ThingID == heroThingId) heroMatchesOnMap++;
                    }
                }
                bool heroNoDuplicate = heroThingId.NullOrEmpty() || heroMatchesOnMap == 1;

                // Complete the stay peacefully via real Pawn.ExitMap and verify single trigger
                ExitGuestsViaRealExitMap(map, savedGuestIds);
                story.DirectorTick();
                story.DirectorTick();
                bool completedOnce = CountOutcome(visitRun, "success") == 1;

                AddCheck("Reload_ActiveVisitLordHeroAndPeacefulExit",
                    mapMatches && lordRestored && heroNoDuplicate && completedOnce,
                    "mapMatches, lord & guest count match, heroCount<=1, successCount=1",
                    string.Format("map={0}, lord={1}({2}/{3}), heroCount={4}, succCount={5}",
                        mapMatches, restoredLord != null,
                        restoredLord != null && restoredLord.ownedPawns != null ? restoredLord.ownedPawns.Count : -1,
                        savedGuestIds.Count, heroMatchesOnMap, CountOutcome(visitRun, "success")));
            }
            else
            {
                AddCheck("Reload_ActiveVisitLordHeroAndPeacefulExit", false, "active visit restored", "missing");
            }

            // ---------------- 2. PAID DELIVER COMPLETES ONCE WITHOUT DOUBLE DEDUCTION ----------------
            StoryQuestRun paidRun = story.RunFor(StoryHotelProbeBootstrap.QReloadPaidDeliver);
            int steelBeforeTick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);
            story.DirectorTick();
            story.DirectorTick();
            int steelAfterTick = StoryPayment.CountInColonyStockpiles(map, ThingDefOf.Steel);

            bool completedOnceNoRededuct = paidRun != null && CountOutcome(paidRun, "success") == 1 &&
                                           steelBeforeTick == 50 && steelAfterTick == 50;
            AddCheck("Reload_PaidDeliverNoDoubleDeduction", completedOnceNoRededuct,
                "successCount=1, steel=50->50",
                string.Format("count={0}, steel={1}->{2}",
                    CountOutcome(paidRun, "success"), steelBeforeTick, steelAfterTick));
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static bool BuildTestHotel(Map targetMap)
        {
            if (targetMap == null) return false;

            ThingDef guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("GuestBed") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("GuestDoubleBed") ??
                                   DefDatabase<ThingDef>.GetNamedSilentFail("GuestSleepingSpot"); // CHECK API
            ThingDef receptionDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_HotelReception"); // CHECK API

            IntVec3 baseCell = FindStandable(targetMap);

            if (receptionDef != null)
            {
                Thing rec = ThingMaker.MakeThing(receptionDef, receptionDef.MadeFromStuff ? ThingDefOf.WoodLog : null);
                rec.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(rec, baseCell, targetMap, WipeMode.Vanish);
            }

            int spawnedBeds = 0;
            if (guestBedDef != null)
            {
                ThingDef stuff = guestBedDef.MadeFromStuff ? ThingDefOf.WoodLog : null;
                for (int i = 0; i < 3; i++)
                {
                    IntVec3 cell;
                    if (CellFinder.TryFindRandomCellNear(baseCell, targetMap, 8, delegate(IntVec3 c)
                    {
                        return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
                    }, out cell))
                    {
                        Thing bed = ThingMaker.MakeThing(guestBedDef, stuff);
                        bed.SetFaction(Faction.OfPlayer);
                        GenSpawn.Spawn(bed, cell, targetMap, WipeMode.Vanish);
                        spawnedBeds++;
                    }
                }
            }
            return spawnedBeds > 0;
        }

        private static void SetGuestsWelcome(Map targetMap, bool welcome)
        {
            Type compType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
            if (compType == null || targetMap == null) return;
            MapComponent comp = HotelVisitGuarantee.HospitalityOf(targetMap);
            if (comp == null) return;
            FieldInfo field = AccessTools.Field(compType, "guestsAreWelcome");
            if (field != null) field.SetValue(comp, welcome);
        }

        private static Zone_Stockpile EnsureTestStockpile(Map targetMap)
        {
            if (targetMap == null || targetMap.zoneManager == null) return null;
            Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, targetMap.zoneManager); // CHECK API
            targetMap.zoneManager.RegisterZone(zone);
            IntVec3 origin = FindStandable(targetMap);
            for (int dx = -1; dx <= 2; dx++)
            {
                for (int dz = -1; dz <= 2; dz++)
                {
                    IntVec3 c = origin + new IntVec3(dx, 0, dz);
                    if (c.InBounds(targetMap) && c.Standable(targetMap) && targetMap.zoneManager.ZoneAt(c) == null)
                    {
                        zone.AddCell(c);
                    }
                }
            }
            return zone;
        }

        private static void ClearStockpileResources(Map targetMap, ThingDef def)
        {
            if (targetMap == null || def == null) return;
            List<Thing> snapshot = new List<Thing>(targetMap.listerThings.ThingsOfDef(def));
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing t = snapshot[i];
                if (t != null && !t.Destroyed && StoryPayment.IsInPlayerStorage(targetMap, t))
                {
                    t.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void PlaceInStockpile(Map targetMap, Zone_Stockpile zone, ThingDef def, int count, int cellIndex)
        {
            if (targetMap == null || zone == null || zone.cells.Count == 0 || def == null || count <= 0) return;
            IntVec3 cell = zone.cells[cellIndex % zone.cells.Count];
            Thing t = ThingMaker.MakeThing(def);
            t.stackCount = count;
            GenSpawn.Spawn(t, cell, targetMap, WipeMode.Vanish);
            t.SetForbidden(false, false);
        }

        private static ChoiceLetter_StoryDeliver FindDeliverLetter(string questDefName)
        {
            if (Find.LetterStack == null) return null;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                ChoiceLetter_StoryDeliver dl = letters[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == questDefName) return dl;
            }
            return null;
        }

        private static bool ClickFirstEnabledOption(ChoiceLetter_StoryDeliver letter)
        {
            if (letter == null) return false;
            List<DiaOption> opts = new List<DiaOption>(letter.Choices);
            for (int i = 0; i < opts.Count; i++)
            {
                if (opts[i] != null && opts[i].action != null && !opts[i].disabled)
                {
                    opts[i].action();
                    return true;
                }
            }
            return false;
        }

        private static void ExitGuestsViaRealExitMap(Map targetMap, List<int> guestIds)
        {
            if (targetMap == null || guestIds == null || guestIds.Count == 0) return;
            HashSet<int> set = new HashSet<int>(guestIds);
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && set.Contains(p.thingIDNumber) && p.Spawned)
                {
                    p.ExitMap(false, Rot4.South); // CHECK API
                }
            }
        }

        private void CleanupActiveHotelRun(SparkStory story, StoryQuestDef def, StoryQuestRun run)
        {
            if (run == null) return;
            List<int> ids = ParseIntList(GetVar(run, "guestIds"));
            if (def != null && def.stages != null && def.stages.Count > 0)
            {
                List<StoryStage> backup = new List<StoryStage>(def.stages);
                try
                {
                    def.stages.Clear();
                    story.DirectorTick();
                }
                finally
                {
                    def.stages.AddRange(backup);
                }
            }
            CleanupPawnsByIds(map, ids);
        }

        private static void CleanupPawnsByIds(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return;
            HashSet<int> set = new HashSet<int>(ids);
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && set.Contains(p.thingIDNumber))
                {
                    if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                    if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void KillPawnsByIds(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return;
            HashSet<int> set = new HashSet<int>(ids);
            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p != null && !p.Dead && set.Contains(p.thingIDNumber))
                {
                    p.Kill(null);
                }
            }
        }

        private static Pawn FindGuestOnMap(Map targetMap, List<int> ids)
        {
            if (targetMap == null || ids == null || ids.Count == 0) return null;
            HashSet<int> set = new HashSet<int>(ids);
            IReadOnlyList<Pawn> pawns = targetMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && set.Contains(pawns[i].thingIDNumber)) return pawns[i];
            }
            return null;
        }

        private static void KillFirstGuest(Map targetMap, List<int> ids)
        {
            Pawn p = FindGuestOnMap(targetMap, ids);
            if (p != null && !p.Dead) p.Kill(null);
        }

        private static Pawn SpawnCleanColonist(Map targetMap)
        {
            PawnGenerationRequest req = new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                canGeneratePawnRelations: false,
                allowAddictions: false); // CHECK API
            Pawn p = PawnGenerator.GeneratePawn(req);
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static Pawn SpawnCleanNpc(Map targetMap, Faction fac)
        {
            PawnKindDef kind = fac != null ? (fac.RandomPawnKind() ?? PawnKindDefOf.SpaceRefugee) : PawnKindDefOf.SpaceRefugee;
            PawnGenerationRequest req = new PawnGenerationRequest(
                kind,
                fac,
                PawnGenerationContext.NonPlayer,
                canGeneratePawnRelations: false,
                allowAddictions: false); // CHECK API
            Pawn p = PawnGenerator.GeneratePawn(req);
            GenSpawn.Spawn(p, FindStandable(targetMap), targetMap, WipeMode.Vanish);
            return p;
        }

        private static void DestroyTestCaravanAndPawns(Caravan car)
        {
            if (car == null) return;
            List<Pawn> pawns = new List<Pawn>(car.PawnsListForReading);
            car.RemoveAllPawns();
            if (!car.Destroyed) car.Destroy();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null) continue;
                if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns != null && Find.WorldPawns.Contains(p)) Find.WorldPawns.RemovePawn(p);
                if (!p.Destroyed) p.Destroy(DestroyMode.Vanish);
            }
        }

        private static Faction FindPermanentEnemyHumanlikeFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && f.def != null && f.def.permanentEnemy && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return null;
        }

        private static Faction FindGoodwillHumanlikeFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HasGoodwill && f.def != null &&
                    !f.def.permanentEnemy && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return null;
        }

        private static Faction FindAnotherHostileFaction(Faction exclude)
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && f != exclude && !f.IsPlayer && !f.defeated && f.HostileTo(Faction.OfPlayer) && f.def != null && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return null;
        }

        private static Settlement FindAnyNonPlayerSettlement()
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].Destroyed && all[i].Faction != null && !all[i].Faction.IsPlayer)
                {
                    return all[i];
                }
            }
            return null;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0) return null;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].Destroyed && all[i].ID == id) return all[i];
            }
            return null;
        }

        private static Lord FindLordById(Map targetMap, int lordId)
        {
            if (targetMap == null || lordId < 0 || targetMap.lordManager == null) return null;
            List<Lord> lords = targetMap.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null && lords[i].loadID == lordId) return lords[i];
            }
            return null;
        }

        private static void ForceHostileWithGoodwill(Faction fac, int gw)
        {
            if (fac == null) return;
            int drop = -100 - fac.PlayerGoodwill;
            if (drop < 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null);
            int adj = gw - fac.PlayerGoodwill;
            if (adj != 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, adj, false, false, null, null);
        }

        private static void ForceNeutralWithGoodwill(Faction fac, int gw)
        {
            if (fac == null) return;
            if (fac.HostileTo(Faction.OfPlayer))
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, 0 - fac.PlayerGoodwill, false, false, null, null);
            }
            int adj = gw - fac.PlayerGoodwill;
            if (adj != 0) fac.TryAffectGoodwillWith(Faction.OfPlayer, adj, false, false, null, null);
        }

        private static IntVec3 FindStandable(Map targetMap)
        {
            IntVec3 cell;
            if (CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 12, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
            }, out cell))
            {
                return cell;
            }
            return targetMap.Center;
        }

        private static bool HasOutcome(StoryQuestRun run, string outcome)
        {
            return CountOutcome(run, outcome) > 0;
        }

        private static int CountOutcome(StoryQuestRun run, string outcome)
        {
            if (run == null || run.history == null) return 0;
            int count = 0;
            string suffix = ":" + outcome;
            for (int i = 0; i < run.history.Count; i++)
            {
                string h = run.history[i];
                if (h != null && (h == outcome || h.EndsWith(suffix, StringComparison.Ordinal))) count++;
            }
            return count;
        }

        private static int ParseInt(StoryQuestRun run, string key, int fallback)
        {
            if (run == null || run.vars == null) return fallback;
            string raw;
            int val;
            return (run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) ? val : fallback;
        }

        private static string GetVar(StoryQuestRun run, string key)
        {
            if (run == null || run.vars == null) return string.Empty;
            string raw;
            return run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }

        private static List<int> ParseIntList(string raw)
        {
            List<int> list = new List<int>();
            if (raw.NullOrEmpty()) return list;
            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) list.Add(val);
            }
            return list;
        }

        private void AddCheck(string name, bool passed, string expected, string actual)
        {
            CheckItem item = new CheckItem();
            item.name = name;
            item.passed = passed;
            item.expected = expected ?? string.Empty;
            item.actual = actual ?? string.Empty;
            m_checks.Add(item);
        }

        private bool WriteReport(string fileName)
        {
            bool allPassed = m_errors.Count == 0 && m_checks.Count > 0;
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (!m_checks[i].passed)
                {
                    allPassed = false;
                    break;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"passed\":");
            sb.Append(allPassed ? "true" : "false");
            sb.Append(",\"testModeEnabled\":");
            sb.Append(TestMode.Enabled ? "true" : "false");
            sb.Append(",\"checksCount\":");
            sb.Append(m_checks.Count);
            sb.Append(",\"checks\":[");
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (i > 0) sb.Append(',');
                CheckItem c = m_checks[i];
                sb.Append("{\"name\":\"");
                sb.Append(EscapeJson(c.name));
                sb.Append("\",\"passed\":");
                sb.Append(c.passed ? "true" : "false");
                sb.Append(",\"expected\":\"");
                sb.Append(EscapeJson(c.expected));
                sb.Append("\",\"actual\":\"");
                sb.Append(EscapeJson(c.actual));
                sb.Append("\"}");
            }
            sb.Append("],\"errors\":[");
            for (int i = 0; i < m_errors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                sb.Append(EscapeJson(m_errors[i]));
                sb.Append('"');
            }
            sb.Append("]}");

            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return allPassed;
        }

        private void WriteUiReport(bool actualPassed)
        {
            try
            {
                string uiPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "StoryHotelUiProbe.json");
                string payload = string.Format(
                    "{{\"passed\":{0},\"checksCount\":{1},\"errorsCount\":{2},\"visualLayoutInspected\":false}}",
                    actualPassed ? "true" : "false",
                    m_checks.Count,
                    m_errors.Count);
                File.WriteAllText(uiPath, payload, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }
    }
}

```

## Итоговая сводка по Задаче 46 (46_1 + 46_2)

1. **`Source/StoryStage_HotelVisit.cs` (часть 46_1):**
   * Перенос героя полностью переведён на `StoryPawnTransfer.TrySpawnFreeWorldPawn`; снятие героя из `WorldPawns` происходит только после генерации/спавна сопровождения и создания `visitLord`, а любое исключение в `Start` полностью откатывает и героя, и сопровождение, и аренду перемирия (`AbortLease`).
   * Аренда перемирия `AcquireTruceLease` запрашивается всегда (включая перекрывающиеся визиты той же фракции, которая временно уже кажется нейтральной).
   * Режим `Hide` сам запускает и привязывает свой рейд охотников (`hunterRaidLordId`, `hunterRaidPawnIds`, временная аренда враждебности охотников при необходимости); посторонние враждебные лорды той же фракции игнорируются.
   * Подтверждённый мирный уход гостя (`exited_peaceful`) требует фактического ухода через `Pawn.ExitMap` (`Prefix` + `Postfix`).
   * Тестовые хуки (`TestCompanionGenerateOverride`, `TestHunterRaidExecuteOverride`) закрыты за `TestMode.Enabled`.

2. **`Source/StoryStage_Deliver.cs` (часть 46_1):**
   * Условия оплаты фиксируются как неизменяемый контракт в `run.vars` (`contractThingDef`, `contractCount`, `contractTarget`, `deliverMapId`, `deadlineTick`); поля письма не используются как источник истины.
   * Строгое разрешение `stageKey` без отката к устаревшему индексу `run.stage` (`stage_key_removed`).
   * Отсутствующий/неизвестный `deadlineTick` всегда отклоняет оплату (`deadline_missing_or_passed`) и закрывает этап по таймауту.
   * Списание идёт через `StoryPayment` с `onPaymentSecured` (`delivered="true"` до необратимого уничтожения стаков).

3. **`Tests/InGameStoryHotelProbe.cs` (часть 46_2):**
   * Покрыты все сценарии Раунда 2: исключение при генерации сопровождения (герой остаётся в `WorldPawns`), перекрывающиеся перемирия на одной враждебной фракции, orphan-откат перемирия реальным удалением этапа, исчезновение гостя без `ExitMap`, собственный рейд `Hide` + игнорирование посторонних врагов + провал рейда до дедлайна, отказ `Deliver` при отсутствующем `deadlineTick` / подменённых полях письма / удалённом `stageKey`, атомарный откат второго стака и устойчивость к исключению в `Destroy`, караван мимо vs `CaravanArrivalAction_StoryDeliver`, сохранение/перезагрузка активного визита и оплаченной доставки.
   * Честные отчёты `StoryHotelProbe.json`, `StoryHotelReloadProbe.json` и `StoryHotelUiProbe.json`; удалена глобальная зачистка связей пешек (`canGeneratePawnRelations: false`).