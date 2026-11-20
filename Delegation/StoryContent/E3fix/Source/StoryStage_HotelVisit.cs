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
    /// Fixes enforced:
    /// - Safe hero transfer: only moves a free living world pawn of the visit faction; colonists,
    ///   prisoners, caravan members, container-held, or spawned heroes are NEVER stolen (visit runs
    ///   without them and records heroIncluded=false, or rolls back on spawn failure).
    /// - Truce &amp; rollback: verifies CanFactionEnterTruce BEFORE spawning (permanentEnemy fails
    ///   cleanly without spawning) and tracks temporary neutrality in StoryRelationRestorer.
    /// - Host per-guest tracking: pins visitMapId, visitLordId, exitLordId, and classifies every
    ///   guest on each poll (on_map / exited_peaceful / killed / imprisoned / sent_away / hostile /
    ///   missing via Pawn.ExitMap tracking). Hard overdue deadline fails if the lord never leaves.
    /// - Investigate: requires a real PlayLogEntry_Interaction (initiator = player colonist,
    ///   recipient = guest of THIS visit, Tick >= startTick). No indoor-room fallback.
    /// - Hide: requires a valid hunterFaction and binds to a specific hunter raid lord/cohort;
    ///   unrelated enemies never count, and missing raid ends at deadline.
    /// - Hospitality compatibility: caches and validates reflection signatures before creating
    ///   groups, records visitMode ("hospitality" vs "vanilla_fallback"), and resolves noHotel
    ///   immediately on Start without side effects.
    /// - No custom forceOutcome reading (core SparkStory handles -tmm-story-test exclusively).
    /// </summary>
    public sealed class StoryStage_HotelVisit : StoryStage
    {
        private const float DefaultHardOverdueDays = 2.0f;

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

        /// <summary>
        /// Checks via cached reflection whether Hospitality is loaded, guests are welcomed,
        /// and at least one guest bed exists on the given map. Never throws.
        /// </summary>
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
            ctx.run.vars["hunterRaidBound"] = "false";
            ctx.run.vars["hunterRaidCleared"] = "false";
            ctx.run.vars["hunterRaidLordId"] = "-1";
            ctx.run.vars["departureTriggered"] = "false";
            ctx.run.vars["arrived"] = "false";
            ctx.run.vars["heroIncluded"] = "false";
            ctx.run.vars["heroSpawned"] = "false";
            ctx.run.vars["visitLordId"] = "-1";
            ctx.run.vars["exitLordId"] = "-1";
            ctx.run.vars["visitMapId"] = home != null ? home.uniqueID.ToString() : "-1";
            ctx.run.vars["visitMode"] = HasHospitalityLordSupport ? "hospitality" : "vanilla_fallback";

            // 1. Immediate noHotel detection on Start (zero side effects: no spawns, no relation edits).
            // Note: StoryStage.Start returns void in core SparkStory, so Start resolves noHotel cleanly
            // here and marks immediateOutcome="noHotel" for instant completion on the first Tick.
            if (!HasOpenHotel(home))
            {
                ctx.run.vars["noHotel"] = "true";
                ctx.run.vars["immediateOutcome"] = "noHotel";
                SendNoHotelLetter(ctx, home);
                return;
            }

            // 2. For Hide objective, validate hunterFaction BEFORE spawning or mutating relations
            if (objective == StoryHotelObjective.Hide)
            {
                Faction hunterFac = !hunterFaction.NullOrEmpty() ? StoryUtil.FactionOf(hunterFaction) : null;
                if (hunterFac == null || hunterFac.defeated || hunterFac.IsPlayer)
                {
                    Log.Warning("[TMM] StoryStage_HotelVisit: Hide objective failed to resolve hunterFaction '" + (hunterFaction ?? "null") + "'.");
                    ctx.run.vars["failReason"] = "hunter_faction_missing";
                    ctx.run.vars["immediateOutcome"] = FindOutcome("unavailable") != null ? "unavailable" : "failed";
                    return;
                }
                ctx.run.vars["hunterFactionId"] = hunterFac.loadID.ToString();
                ctx.run.vars["initialHunterLordIds"] = JoinInts(SnapshotFactionLords(home, hunterFac));
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

            // 4. Verify truce eligibility BEFORE creating any pawns or mutating relations
            if (visitFac.HostileTo(Faction.OfPlayer))
            {
                if (!StoryRelationRestorer.CanFactionEnterTruce(visitFac))
                {
                    ctx.run.vars["failReason"] = "permanent_enemy_cannot_truce";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }

                StoryRelationRestorer restorer = StoryRelationRestorer.Instance;
                string truceLeaseId = null;
                if (restorer == null || !restorer.AcquireTruceLease(visitFac, ctx.run, out truceLeaseId) || visitFac.HostileTo(Faction.OfPlayer))
                {
                    if (restorer != null && !truceLeaseId.NullOrEmpty())
                    {
                        restorer.ReleaseLease(truceLeaseId);
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

            IntVec3 entryCell;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out entryCell, home, CellFinder.EdgeRoadChance_Friendly)) // CHECK API
            {
                entryCell = CellFinder.RandomEdgeCell(home);
            }

            // 5. Safe hero transfer (only a free living world pawn of visitFac; never steal busy/colonist heroes)
            List<Pawn> guests = new List<Pawn>();
            Pawn heroPawn = null;
            bool heroWasWorldPawn = false;
            Faction heroOrigFaction = null;
            bool heroWasLeader = false;

            if (hero != null && HeroRegistry.Instance != null)
            {
                Pawn candidate = HeroRegistry.Instance.GetOrCreate(hero);
                string skipReason;
                if (IsFreeWorldHeroForVisit(candidate, visitFac, out skipReason))
                {
                    heroOrigFaction = candidate.Faction;
                    heroWasLeader = heroOrigFaction != null && heroOrigFaction.leader == candidate;
                    Find.WorldPawns.RemovePawn(candidate);
                    if (candidate.Faction == null)
                    {
                        candidate.SetFaction(visitFac);
                    }
                    heroPawn = candidate;
                    heroWasWorldPawn = true;
                    guests.Add(heroPawn);
                }
                else
                {
                    // Explicit, safe omission: hero stays untouched in their current faction/map/holder
                    ctx.run.vars["heroIncluded"] = "false";
                    ctx.run.vars["heroSkipReason"] = skipReason ?? "unavailable";
                }
            }

            // 6. Generate accompanying regular guests of visitFac
            int targetCount = Mathf.Clamp(groupSize, 1, 8);
            List<Pawn> generatedExtras = new List<Pawn>();
            while (guests.Count < targetCount)
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
                Pawn guest = PawnGenerator.GeneratePawn(req);
                if (guest != null)
                {
                    guests.Add(guest);
                    generatedExtras.Add(guest);
                }
                else
                {
                    break;
                }
            }

            if (guests.Count == 0)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: failed to create guest pawns.");
                ReleaseTruceLease(ctx);
                ctx.run.vars["failReason"] = "no_guests_created";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            // 7. Spawn all guests with strict verification and rollback on failure
            List<Pawn> spawnedGuests = new List<Pawn>();
            List<int> guestIdNums = new List<int>();
            bool heroSpawnedOk = false;

            for (int i = 0; i < guests.Count; i++)
            {
                Pawn p = guests[i];
                IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(entryCell, home, 6, null);
                Thing res = GenSpawn.Spawn(p, spawnCell, home, WipeMode.Vanish);
                if (res != p || !p.Spawned || p.Map != home)
                {
                    if (p == heroPawn && heroWasWorldPawn)
                    {
                        RollbackHeroToWorld(heroPawn, heroOrigFaction, heroWasLeader);
                        heroPawn = null;
                        heroWasWorldPawn = false;
                        ctx.run.vars["heroSkipReason"] = "spawn_failed";
                    }
                    else if (!p.Destroyed)
                    {
                        p.Destroy(DestroyMode.Vanish);
                    }
                    continue;
                }

                if (p == heroPawn)
                {
                    heroSpawnedOk = true;
                    HeroRegistry.Instance.NotifyEncountered(heroPawn);
                    ctx.run.vars["heroThingId"] = heroPawn.ThingID;
                    ctx.run.vars["heroIdNum"] = heroPawn.thingIDNumber.ToString();
                    ctx.run.vars["heroIncluded"] = "true";
                    ctx.run.vars["heroSpawned"] = "true";
                }

                spawnedGuests.Add(p);
                guestIdNums.Add(p.thingIDNumber);
            }

            if (spawnedGuests.Count == 0)
            {
                ReleaseTruceLease(ctx);
                ctx.run.vars["failReason"] = "spawn_failed";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            // 8. Attach visit Lord (distinguishing real Hospitality vs vanilla fallback)
            IntVec3 chillSpot = FindGuestBedCell(home);
            if (chillSpot == IntVec3.Invalid) chillSpot = home.Center;

            string usedMode;
            Lord visitLord = AttachVisitLord(home, visitFac, chillSpot, stayTicks, spawnedGuests, out usedMode);
            if (visitLord == null)
            {
                // Rollback everything if Lord creation failed
                for (int i = 0; i < spawnedGuests.Count; i++)
                {
                    Pawn p = spawnedGuests[i];
                    if (p == heroPawn && heroWasWorldPawn)
                    {
                        RollbackHeroToWorld(heroPawn, heroOrigFaction, heroWasLeader);
                    }
                    else if (p != null && !p.Destroyed)
                    {
                        if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                        p.Destroy(DestroyMode.Vanish);
                    }
                }
                ReleaseTruceLease(ctx);
                ctx.run.vars["failReason"] = "lord_creation_failed";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            if (usedMode == "hospitality")
            {
                for (int i = 0; i < spawnedGuests.Count; i++)
                {
                    TryNotifyHospitalityCompArrive(spawnedGuests[i]);
                }
            }

            ctx.run.vars["visitMode"] = usedMode;
            ctx.run.vars["visitLordId"] = visitLord.loadID.ToString();
            ctx.run.vars["guestIds"] = JoinInts(guestIdNums);
            ctx.run.vars["arrived"] = "true";

            if (StoryRelationRestorer.Instance != null)
            {
                StoryRelationRestorer.Instance.RegisterHotelGuests(ctx.run, home.uniqueID, visitLord.loadID, guestIdNums);
            }

            string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Hotel Guests Arrived"), ctx);
            string body = StoryUtil.Format(!text.NullOrEmpty() ? text : Objective(ctx), ctx);
            LookTargets target = (heroSpawnedOk && heroPawn != null) ? new LookTargets(heroPawn) : new LookTargets(spawnedGuests[0]);
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, target); // CHECK API
        }

        // =====================================================================
        // STAGE LIFECYCLE: TICK (SINGLE-PASS PER-GUEST CLASSIFICATION)
        // =====================================================================

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            // Immediate outcome determined cleanly during Start (e.g. noHotel, permanent_enemy, missing hunterFaction)
            string immediate = GetStr(ctx, "immediateOutcome");
            if (!immediate.NullOrEmpty())
            {
                return Finish(ctx, immediate);
            }

            if (GetBool(ctx, "noHotel"))
            {
                return Finish(ctx, "noHotel");
            }

            // Pin strictly to the saved visit map (never silently swap to a second colony!)
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

            // Single-pass lookup maps for spawned pawns, corpses, and holders on visitMap
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
                    // Inside a drop pod/casket/container on map — NEVER counts as peaceful departure!
                    stateLog.Append("in_holder");
                    ctx.run.vars["guestStates"] = stateLog.ToString();
                    ctx.run.vars["failReason"] = "guest_missing_in_holder";
                    return Finish(ctx, "failed");
                }

                // Not on visitMap: MUST have a confirmed peaceful exit via Pawn.ExitMap under our visit/exit lord
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

                // Guest vanished without peaceful ExitMap -> failed (never false success!)
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
                UpdateHideHunterRaidTracking(ctx, visitMap);
            }

            // ---------------- DEPARTURE & LORD HEALTH ----------------

            bool visitLordAlive = FindLordById(visitMap, visitLordId) != null;
            bool exitLordAlive = exitLordId >= 0 && FindLordById(visitMap, exitLordId) != null;

            if (onMapGuests.Count > 0)
            {
                // Hard overdue deadline if lord stalls or Hide hunter raid never arrives
                if (now >= hardDeadlineTick)
                {
                    if (objective == StoryHotelObjective.Hide && !GetBool(ctx, "hunterRaidCleared"))
                    {
                        ctx.run.vars["deadlineReached"] = "true";
                        ctx.run.vars["failReason"] = GetBool(ctx, "hunterRaidBound") ? "hide_raid_not_cleared_deadline" : "hide_no_raid_deadline";
                        return Finish(ctx, FindOutcome("timeout") != null ? "timeout" : "failed");
                    }

                    ctx.run.vars["deadlineReached"] = "true";
                    ctx.run.vars["failReason"] = "lord_overdue_deadline";
                    return Finish(ctx, "failed");
                }

                // Lord disappeared while guests are still stranded on map -> abnormal state -> failed
                if (!visitLordAlive && !exitLordAlive)
                {
                    ctx.run.vars["failReason"] = "lord_missing";
                    return Finish(ctx, "failed");
                }

                // Trigger graceful departure after stayDays once objective allows leaving
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

            // ---------------- ALL GUESTS HAVE LEFT THE MAP ----------------
            // Require ALL guests to have confirmed peaceful exits
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

        // =====================================================================
        // SAFE HERO TRANSFER & ROLLBACK
        // =====================================================================

        private static bool IsFreeWorldHeroForVisit(Pawn p, Faction visitFac, out string skipReason)
        {
            skipReason = null;
            if (p == null) { skipReason = "null"; return false; }
            if (p.Dead || p.Destroyed) { skipReason = "dead"; return false; }
            if (p.Faction == Faction.OfPlayer) { skipReason = "colonist"; return false; }
            if (p.Spawned) { skipReason = "spawned_on_map"; return false; }
            if (p.IsPrisoner || p.IsSlave) { skipReason = "prisoner_or_slave"; return false; }
            if (p.holdingOwner != null) { skipReason = "in_container"; return false; }
            if (p.GetCaravan() != null) { skipReason = "in_caravan"; return false; }
            if (Find.WorldPawns == null || !Find.WorldPawns.Contains(p)) { skipReason = "not_in_world_pawns"; return false; }
            if (p.Faction != null && visitFac != null && p.Faction != visitFac)
            {
                skipReason = "wrong_faction";
                return false;
            }
            return true;
        }

        private static void RollbackHeroToWorld(Pawn p, Faction origFaction, bool wasLeader)
        {
            if (p == null) return;
            if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
            if (origFaction != null && p.Faction != origFaction)
            {
                p.SetFaction(origFaction);
            }
            if (Find.WorldPawns != null && !Find.WorldPawns.Contains(p))
            {
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
            }
            if (wasLeader && origFaction != null)
            {
                origFaction.leader = p;
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
                // CHECK API: PlayLogEntry_Interaction private fields initiator / recipient
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
        // STRICT HIDE OBJECTIVE (BOUND HUNTER RAID LORD / COHORT)
        // =====================================================================

        private void UpdateHideHunterRaidTracking(StoryContext ctx, Map visitMap)
        {
            if (GetBool(ctx, "hunterRaidCleared")) return;

            int hunterFacId = GetInt(ctx, "hunterFactionId", -1);
            Faction hunterFac = FindFactionById(hunterFacId);
            if (hunterFac == null) return;

            // Step 1: Bind to a specific new hostile Lord of hunterFac on visitMap
            if (!GetBool(ctx, "hunterRaidBound"))
            {
                HashSet<int> initialLords = new HashSet<int>(ParseInts(GetStr(ctx, "initialHunterLordIds")));
                if (visitMap.lordManager != null)
                {
                    List<Lord> lords = visitMap.lordManager.lords;
                    for (int i = 0; i < lords.Count; i++)
                    {
                        Lord l = lords[i];
                        if (l == null || l.faction != hunterFac) continue;
                        if (initialLords.Contains(l.loadID)) continue;
                        if (!hunterFac.HostileTo(Faction.OfPlayer)) continue;
                        if (l.ownedPawns == null || l.ownedPawns.Count == 0) continue;

                        List<int> hunterPawnIds = new List<int>();
                        for (int pIdx = 0; pIdx < l.ownedPawns.Count; pIdx++)
                        {
                            Pawn hp = l.ownedPawns[pIdx];
                            if (hp != null && !hunterPawnIds.Contains(hp.thingIDNumber))
                            {
                                hunterPawnIds.Add(hp.thingIDNumber);
                            }
                        }

                        if (hunterPawnIds.Count > 0)
                        {
                            ctx.run.vars["hunterRaidBound"] = "true";
                            ctx.run.vars["hunterRaidLordId"] = l.loadID.ToString();
                            ctx.run.vars["hunterRaidPawnIds"] = JoinInts(hunterPawnIds);
                            break;
                        }
                    }
                }
                return;
            }

            // Step 2: Check whether the bound hunter raid cohort is completely neutralized/fled
            int boundLordId = GetInt(ctx, "hunterRaidLordId", -1);
            HashSet<int> boundPawnIds = new HashSet<int>(ParseInts(GetStr(ctx, "hunterRaidPawnIds")));
            if (IsBoundHunterRaidCleared(visitMap, boundLordId, boundPawnIds))
            {
                ctx.run.vars["hunterRaidCleared"] = "true";
            }
        }

        private static bool IsBoundHunterRaidCleared(Map map, int boundLordId, HashSet<int> boundPawnIds)
        {
            if (map == null) return false;

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

            if (boundPawnIds.Count > 0 && map.mapPawns != null)
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

            return boundLordId >= 0 || boundPawnIds.Count > 0;
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
                // Ignore version mismatch on optional CompGuest.Arrive
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
                // Ignore reflection errors
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
                    CollectGuestsInHolder(h as IThingHolder, guestIds, heldOnMap);
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

        private Faction ResolveVisitFaction(StoryContext ctx)
        {
            if (!faction.NullOrEmpty())
            {
                Faction f = StoryUtil.FactionOf(faction);
                if (f != null && !f.defeated && !f.IsPlayer) return f;
                return null; // Explicit faction missing/defeated -> do not silently substitute
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
