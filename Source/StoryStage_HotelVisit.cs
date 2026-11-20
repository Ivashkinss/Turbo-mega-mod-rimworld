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
                        if (p.relations != null) p.relations.ClearAllRelations(); // no dangling DirectPawnRelation refs in saves
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
                TestMode.ReportException("[TMM] StoryStage_HotelVisit: exception during Start, rolling back completely: ", ex);
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
                    if (p.relations != null) p.relations.ClearAllRelations(); // no dangling DirectPawnRelation refs in saves
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
                TestMode.ReportException("[TMM] StoryStage_HotelVisit: Hide hunter raid threw exception: ", ex);
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

