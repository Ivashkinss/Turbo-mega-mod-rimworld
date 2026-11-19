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
    /// to the colony's Hospitality hotel, tracking Host, Investigate, or Hide objectives across
    /// save/reload strictly via ctx.run.vars. All Hospitality access goes through reflection.
    /// </summary>
    public sealed class StoryStage_HotelVisit : StoryStage
    {
        // Hospitality reflection handles (safe if Hospitality is absent or renamed)
        private static readonly Type HospMapCompType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
        private static readonly FieldInfo HospGuestsWelcomeField = HospMapCompType == null ? null : AccessTools.Field(HospMapCompType, "guestsAreWelcome");
        private static readonly Type HospGuestBedType = AccessTools.TypeByName("Hospitality.Building_GuestBed"); // CHECK API
        private static readonly Type HospLordJobType = AccessTools.TypeByName("Hospitality.LordJob_VisitColony"); // CHECK API
        private static readonly Type HospCompGuestType = AccessTools.TypeByName("Hospitality.CompGuest"); // CHECK API
        private static readonly MethodInfo HospCompArriveMethod = HospCompGuestType == null ? null : AccessTools.Method(HospCompGuestType, "Arrive"); // CHECK API
        private static readonly FieldInfo HospCompSentAwayField = HospCompGuestType == null ? null : AccessTools.Field(HospCompGuestType, "sentAway"); // CHECK API
        private static readonly PropertyInfo HospCompSentAwayProp = HospCompGuestType == null ? null : AccessTools.Property(HospCompGuestType, "sentAway"); // CHECK API

        public string faction;
        public HeroDef hero;
        public int groupSize = 2;
        public float stayDays = 2f;
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

        /// <summary>
        /// Checks via reflection whether Hospitality is available, guests are welcomed,
        /// and at least one guest bed exists on the given map. Never throws if Hospitality is missing.
        /// </summary>
        public static bool HasOpenHotel(Map map)
        {
            if (map == null || HospMapCompType == null || HospGuestsWelcomeField == null) return false;

            MapComponent comp = HotelVisitGuarantee.HospitalityOf(map);
            if (comp == null) return false;

            try
            {
                object welcomeObj = HospGuestsWelcomeField.GetValue(comp);
                if (!(welcomeObj is bool) || !(bool)welcomeObj) return false;
            }
            catch
            {
                return false;
            }

            return FindGuestBedCell(map) != IntVec3.Invalid;
        }

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            Map home = ctx.Home;
            int now = Find.TickManager.TicksGame;
            int stayTicks = Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(stayDays * GenDate.TicksPerDay));

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["leaveTick"] = (now + stayTicks).ToString();
            ctx.run.vars["investigated"] = "false";
            ctx.run.vars["hunterRaidSeen"] = "false";
            ctx.run.vars["departureTriggered"] = "false";
            ctx.run.vars["arrived"] = "false";

            if (!HasOpenHotel(home))
            {
                ctx.run.vars["noHotel"] = "true";
                SendNoHotelLetter(ctx, home);
                return;
            }

            Faction visitFac = ResolveVisitFaction(ctx);
            if (visitFac == null)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: no valid faction found for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                ctx.run.vars["noHotel"] = "true";
                SendNoHotelLetter(ctx, home);
                return;
            }

            ctx.run.vars["visitFactionId"] = visitFac.loadID.ToString();

            // Undercover spies / defectors from hostile factions temporarily visit under truce.
            if (visitFac.HostileTo(Faction.OfPlayer))
            {
                ctx.run.vars["wasHostile"] = "true";
                ctx.run.vars["origGoodwill"] = visitFac.PlayerGoodwill.ToString();
                ctx.run.vars["origKind"] = ((int)visitFac.RelationKindWith(Faction.OfPlayer)).ToString();
                ForceNeutralForVisit(visitFac);
            }
            else
            {
                ctx.run.vars["wasHostile"] = "false";
            }

            IntVec3 entryCell;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out entryCell, home, CellFinder.EdgeRoadChance_Friendly)) // CHECK API
            {
                entryCell = CellFinder.RandomEdgeCell(home);
            }

            List<Pawn> guests = new List<Pawn>();
            Pawn heroPawn = null;

            if (hero != null && HeroRegistry.Instance != null)
            {
                heroPawn = HeroRegistry.Instance.GetOrCreate(hero);
                if (heroPawn != null && !heroPawn.Dead && !heroPawn.Destroyed)
                {
                    if (heroPawn.Spawned) heroPawn.DeSpawn(DestroyMode.Vanish);
                    if (heroPawn.Faction == null)
                    {
                        heroPawn.SetFaction(visitFac);
                    }
                    guests.Add(heroPawn);
                    ctx.run.vars["heroThingId"] = heroPawn.ThingID;
                    ctx.run.vars["heroIdNum"] = heroPawn.thingIDNumber.ToString();
                }
            }

            int targetCount = Mathf.Clamp(groupSize, 1, 8);
            while (guests.Count < targetCount)
            {
                PawnKindDef kind = visitFac.RandomPawnKind();
                if (kind == null) kind = PawnKindDefOf.SpaceRefugee;
                PawnGenerationRequest req = new PawnGenerationRequest(kind, visitFac, PawnGenerationContext.NonPlayer, home.Tile, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false); // CHECK API
                Pawn guest = PawnGenerator.GeneratePawn(req);
                if (guest != null)
                {
                    guests.Add(guest);
                }
                else
                {
                    break;
                }
            }

            if (guests.Count == 0)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: failed to create guest pawns.");
                ctx.run.vars["noHotel"] = "true";
                RestoreFactionIfNeeded(ctx);
                return;
            }

            List<int> guestIdNums = new List<int>();
            for (int i = 0; i < guests.Count; i++)
            {
                Pawn p = guests[i];
                IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(entryCell, home, 6, null);
                GenSpawn.Spawn(p, spawnCell, home, WipeMode.Vanish);
                TryNotifyHospitalityCompArrive(p);
                guestIdNums.Add(p.thingIDNumber);
            }

            IntVec3 chillSpot = FindGuestBedCell(home);
            if (chillSpot == IntVec3.Invalid) chillSpot = home.Center;

            Lord visitLord = AttachVisitLord(home, visitFac, chillSpot, stayTicks, guests);
            ctx.run.vars["lordId"] = visitLord != null ? visitLord.loadID.ToString() : "-1";
            ctx.run.vars["guestIds"] = JoinInts(guestIdNums);
            ctx.run.vars["arrived"] = "true";

            string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Hotel Guests Arrived"), ctx);
            string body = StoryUtil.Format(!text.NullOrEmpty() ? text : Objective(ctx), ctx);
            LookTargets target = heroPawn != null ? new LookTargets(heroPawn) : new LookTargets(guests[0]);
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, target); // CHECK API
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            if (GetBool(ctx, "noHotel"))
            {
                return Finish(ctx, "noHotel");
            }

            Map home = ctx.Home;
            if (home == null)
            {
                return Finish(ctx, "failed");
            }

            int now = Find.TickManager.TicksGame;
            int startTick = GetInt(ctx, "startTick", now);
            int leaveTick = GetInt(ctx, "leaveTick", now + GenDate.TicksPerDay);
            int visitFacId = GetInt(ctx, "visitFactionId", -1);
            Faction visitFac = FindFactionById(visitFacId);

            if (visitFac != null && visitFac.HostileTo(Faction.OfPlayer))
            {
                return Finish(ctx, "failed");
            }

            List<int> guestIds = ParseInts(GetStr(ctx, "guestIds"));
            if (guestIds.Count == 0)
            {
                return Finish(ctx, "failed");
            }

            List<Pawn> spawnedGuests = new List<Pawn>();
            for (int i = 0; i < guestIds.Count; i++)
            {
                int idNum = guestIds[i];
                if (IsPawnDeadByIdNum(home, idNum))
                {
                    return Finish(ctx, "failed");
                }

                Pawn p = FindPawnOnMapByIdNum(home, idNum);
                if (p != null)
                {
                    if (p.Dead || p.IsPrisonerOfColony || p.IsSlaveOfColony || p.HostileTo(Faction.OfPlayer))
                    {
                        return Finish(ctx, "failed");
                    }
                    if (IsSentAwayByHospitality(p))
                    {
                        return Finish(ctx, "failed");
                    }
                    spawnedGuests.Add(p);
                }
                else
                {
                    Pawn worldPawn = FindWorldPawnByIdNum(idNum);
                    if (worldPawn != null && (worldPawn.Dead || worldPawn.HostileTo(Faction.OfPlayer)))
                    {
                        return Finish(ctx, "failed");
                    }
                }
            }

            // Objective tracking while guests are present on the home map
            if (objective == StoryHotelObjective.Investigate && !GetBool(ctx, "investigated") && spawnedGuests.Count > 0)
            {
                if (CheckInvestigationMet(home, spawnedGuests, startTick))
                {
                    ctx.run.vars["investigated"] = "true";
                }
            }

            bool huntersPresentNow = false;
            if (objective == StoryHotelObjective.Hide)
            {
                huntersPresentNow = HasActiveHunterRaiders(home);
                if (huntersPresentNow)
                {
                    ctx.run.vars["hunterRaidSeen"] = "true";
                }
            }

            // Trigger departure once stayDays have elapsed (important when time advances or Hospitality stay differs)
            if (now >= leaveTick && spawnedGuests.Count > 0 && !GetBool(ctx, "departureTriggered"))
            {
                if (objective != StoryHotelObjective.Hide || !huntersPresentNow)
                {
                    TriggerGracefulDeparture(home, visitFac, spawnedGuests);
                    ctx.run.vars["departureTriggered"] = "true";
                }
            }

            // Guests have all left the home map peacefully
            if (spawnedGuests.Count == 0)
            {
                if (objective == StoryHotelObjective.Investigate)
                {
                    return Finish(ctx, GetBool(ctx, "investigated") ? "success" : "failed");
                }
                if (objective == StoryHotelObjective.Hide)
                {
                    if (huntersPresentNow) return null;
                    return Finish(ctx, "success");
                }
                return Finish(ctx, "success");
            }

            return null;
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
                else if (objective == StoryHotelObjective.Hide) baseText = "Shelter hidden guests in the hotel";
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

            return string.Format("{0} ({1:0.0}d left)", baseText, daysLeft);
        }

        private string Finish(StoryContext ctx, string outcome)
        {
            RestoreFactionIfNeeded(ctx);
            ctx.run.vars["finished"] = outcome;
            return outcome;
        }

        private static void SendNoHotelLetter(StoryContext ctx, Map home)
        {
            string title = StoryUtil.Format(!label.NullOrEmpty() ? label : "No Hotel Available", ctx);
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
                if (HospGuestBedType != null && HospGuestBedType.IsInstanceOfType(b))
                {
                    return b.Position;
                }
                if (b is Building_Bed)
                {
                    string typeName = b.GetType().Name;
                    string defName = b.def != null ? b.def.defName : string.Empty;
                    if (typeName.IndexOf("GuestBed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("GuestBed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("GuestSleepingSpot", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return b.Position;
                    }
                }
            }
            return IntVec3.Invalid;
        }

        private static Lord AttachVisitLord(Map home, Faction fac, IntVec3 chillSpot, int stayTicks, List<Pawn> guests)
        {
            LordJob hospJob = TryCreateHospitalityLordJob(fac, chillSpot, stayTicks);
            if (hospJob != null)
            {
                return LordMaker.MakeNewLord(fac, hospJob, home, guests);
            }

            // Fallback to vanilla LordJob_VisitColony if Hospitality constructor signature differs
            LordJob vanillaJob = new RimWorld.LordJob_VisitColony(fac, chillSpot, stayTicks); // CHECK API
            return LordMaker.MakeNewLord(fac, vanillaJob, home, guests);
        }

        private static LordJob TryCreateHospitalityLordJob(Faction fac, IntVec3 chillSpot, int stayTicks)
        {
            if (HospLordJobType == null) return null;
            try
            {
                ConstructorInfo[] ctors = HospLordJobType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < ctors.Length; i++)
                {
                    ParameterInfo[] p = ctors[i].GetParameters();
                    if (p.Length == 4 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3) && p[2].ParameterType == typeof(int) && p[3].ParameterType == typeof(bool))
                    {
                        return ctors[i].Invoke(new object[] { fac, chillSpot, stayTicks, false }) as LordJob;
                    }
                }
                for (int i = 0; i < ctors.Length; i++)
                {
                    ParameterInfo[] p = ctors[i].GetParameters();
                    if (p.Length == 3 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3) && p[2].ParameterType == typeof(int))
                    {
                        return ctors[i].Invoke(new object[] { fac, chillSpot, stayTicks }) as LordJob;
                    }
                }
                for (int i = 0; i < ctors.Length; i++)
                {
                    ParameterInfo[] p = ctors[i].GetParameters();
                    if (p.Length == 2 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3))
                    {
                        return ctors[i].Invoke(new object[] { fac, chillSpot }) as LordJob;
                    }
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
            if (p == null || HospCompGuestType == null || HospCompArriveMethod == null) return;
            try
            {
                List<ThingComp> comps = p.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (HospCompGuestType.IsInstanceOfType(comps[i]))
                    {
                        if (HospCompArriveMethod.GetParameters().Length == 0)
                        {
                            HospCompArriveMethod.Invoke(comps[i], null);
                        }
                        break;
                    }
                }
            }
            catch
            {
                // Ignore Hospitality version mismatch on optional CompGuest.Arrive
            }
        }

        private static bool IsSentAwayByHospitality(Pawn p)
        {
            if (p == null || HospCompGuestType == null) return false;
            try
            {
                List<ThingComp> comps = p.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (!HospCompGuestType.IsInstanceOfType(comps[i])) continue;
                    if (HospCompSentAwayField != null)
                    {
                        object v = HospCompSentAwayField.GetValue(comps[i]);
                        if (v is bool && (bool)v) return true;
                    }
                    else if (HospCompSentAwayProp != null)
                    {
                        object v = HospCompSentAwayProp.GetValue(comps[i], null);
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

        private static void TriggerGracefulDeparture(Map home, Faction fac, List<Pawn> guests)
        {
            if (home == null || guests == null || guests.Count == 0) return;

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
            if (exitFac != null)
            {
                LordMaker.MakeNewLord(exitFac, new LordJob_ExitMapBest(LocomotionUrgency.Walk, false, false), home, guests); // CHECK API
            }
        }

        private static bool CheckInvestigationMet(Map home, List<Pawn> guests, int startTick)
        {
            List<Pawn> colonists = home.mapPawns.FreeColonistsSpawned;

            // 1. Check vanilla PlayLog for any social interaction between a colonist and a guest since startTick
            if (Find.PlayLog != null && Find.PlayLog.AllEntries != null)
            {
                List<LogEntry> entries = Find.PlayLog.AllEntries;
                for (int i = 0; i < entries.Count; i++)
                {
                    LogEntry entry = entries[i];
                    if (entry == null) continue;
                    if (entry.Tick < startTick) break;
                    if (!(entry is PlayLogEntry_Interaction)) continue; // CHECK API

                    bool hitsGuest = false;
                    for (int g = 0; g < guests.Count; g++)
                    {
                        if (entry.Concerns(guests[g])) // CHECK API
                        {
                            hitsGuest = true;
                            break;
                        }
                    }
                    if (!hitsGuest) continue;

                    for (int c = 0; c < colonists.Count; c++)
                    {
                        if (entry.Concerns(colonists[c])) return true; // CHECK API
                    }
                }
            }

            // 2. Check if a guest is sharing an indoor room with a humanities/social colonist (Social >= 8 or Intellectual >= 8)
            for (int g = 0; g < guests.Count; g++)
            {
                Pawn guest = guests[g];
                if (guest == null || !guest.Spawned) continue;
                Room room = guest.GetRoom();
                if (room == null || room.PsychologicallyOutdoors) continue;

                for (int c = 0; c < colonists.Count; c++)
                {
                    Pawn col = colonists[c];
                    if (col == null || col.Dead || col.Downed || !col.Spawned) continue;
                    if (col.GetRoom() != room) continue;
                    if (col.skills != null)
                    {
                        SkillRecord soc = col.skills.GetSkill(SkillDefOf.Social);
                        SkillRecord intel = col.skills.GetSkill(SkillDefOf.Intellectual);
                        if ((soc != null && !soc.TotallyDisabled && soc.Level >= 8) ||
                            (intel != null && !intel.TotallyDisabled && intel.Level >= 8))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HasActiveHunterRaiders(Map home)
        {
            if (home == null || home.mapPawns == null) return false;
            Faction hunterFac = !hunterFaction.NullOrEmpty() ? StoryUtil.FactionOf(hunterFaction) : null;

            List<Pawn> pawns = home.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.Downed || p.IsPrisonerOfColony) continue;
                if (!p.HostileTo(Faction.OfPlayer)) continue;
                if (hunterFac == null || p.Faction == hunterFac)
                {
                    return true;
                }
            }
            return false;
        }

        private Faction ResolveVisitFaction(StoryContext ctx)
        {
            if (!faction.NullOrEmpty())
            {
                Faction f = StoryUtil.FactionOf(faction);
                if (f != null && !f.defeated && !f.IsPlayer) return f;
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

        private static void ForceNeutralForVisit(Faction fac)
        {
            if (fac == null || !fac.HostileTo(Faction.OfPlayer)) return;
            fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false, null, GlobalTargetInfo.Invalid); // CHECK API
        }

        private void RestoreFactionIfNeeded(StoryContext ctx)
        {
            if (!GetBool(ctx, "wasHostile")) return;
            int facId = GetInt(ctx, "visitFactionId", -1);
            Faction fac = FindFactionById(facId);
            if (fac == null) return;

            int origGoodwill = GetInt(ctx, "origGoodwill", -80);
            int origKindInt = GetInt(ctx, "origKind", (int)FactionRelationKind.Hostile);
            FactionRelationKind origKind = (FactionRelationKind)origKindInt;

            int delta = origGoodwill - fac.PlayerGoodwill;
            if (delta != 0)
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null); // CHECK API
            }
            if (fac.RelationKindWith(Faction.OfPlayer) != origKind)
            {
                fac.SetRelationDirect(Faction.OfPlayer, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }
            ctx.run.vars["wasHostile"] = "false";
        }

        private static Faction FindFactionById(int loadId)
        {
            if (loadId < 0) return null;
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId) return all[i];
            }
            return null;
        }

        private static Pawn FindPawnOnMapByIdNum(Map map, int idNum)
        {
            if (map == null || map.mapPawns == null) return null;
            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].thingIDNumber == idNum) return pawns[i];
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

        private static bool IsPawnDeadByIdNum(Map map, int idNum)
        {
            if (map != null)
            {
                List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                for (int i = 0; i < corpses.Count; i++)
                {
                    Corpse c = corpses[i] as Corpse;
                    if (c != null && c.InnerPawn != null && c.InnerPawn.thingIDNumber == idNum) return true;
                }
            }
            if (Find.WorldPawns != null)
            {
                List<Pawn> dead = Find.WorldPawns.AllPawnsDead;
                for (int i = 0; i < dead.Count; i++)
                {
                    if (dead[i] != null && dead[i].thingIDNumber == idNum) return true;
                }
            }
            return false;
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

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            string raw;
            int val;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
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
