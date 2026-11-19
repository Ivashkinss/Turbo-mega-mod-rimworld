`Source/StoryStage_HotelVisit.cs`

```csharp
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
```

`Source/StoryStage_Deliver.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
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

    /// <summary>
    /// Reminder/hand-in ChoiceLetter for StoryStage_Deliver when delivering directly
    /// from the colony's stockpiles (to = colony). Persists cleanly across save/reload.
    /// </summary>
    public sealed class ChoiceLetter_StoryDeliver : ChoiceLetter
    {
        public string questDefName;
        public int runGen;
        public string thingDefName;
        public int requiredCount;

        public override bool CanDismissWithRightClick
        {
            get { return false; }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                Map home = Find.AnyPlayerHomeMap;
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                int have = (home != null && def != null) ? OutpostPodLogistics.CountResource(home, def) : 0; // CHECK API

                string itemLabel = def != null ? def.label : thingDefName;
                string deliverLabel = string.Format("Deliver {0} x{1} ({2}/{3})", itemLabel, requiredCount, have, requiredCount);
                DiaOption deliver = new DiaOption(deliverLabel);

                if (home == null || def == null || have < requiredCount)
                {
                    deliver.Disable(string.Format("Need {0} {1} in colony stockpiles ({2}/{0})", requiredCount, itemLabel, have));
                }
                else
                {
                    deliver.action = delegate
                    {
                        StoryQuestRun run = StoryStage_Deliver.FindQuestRun(questDefName);
                        if (run == null || run.gen != runGen)
                        {
                            Find.LetterStack.RemoveLetter(this);
                            return;
                        }
                        if (run.vars.ContainsKey("delivered") && run.vars["delivered"] == "true")
                        {
                            Find.LetterStack.RemoveLetter(this);
                            return;
                        }

                        int currentHave = OutpostPodLogistics.CountResource(home, def); // CHECK API
                        if (currentHave < requiredCount) return;

                        int taken = OutpostPodLogistics.DeductResource(home, def, requiredCount); // CHECK API
                        if (taken >= requiredCount)
                        {
                            run.vars["delivered"] = "true";
                            Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    deliver.resolveTree = true;
                }

                yield return deliver;
                yield return Option_Postpone; // CHECK API
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref thingDefName, "thingDefName", null);
            Scribe_Values.Look(ref requiredCount, "requiredCount", 1);
        }
    }

    /// <summary>
    /// Quest stage requiring delivery of a vanilla ThingDef either from home-map stockpiles
    /// (via a ChoiceLetter hand-in button) or by bringing a player caravan to the nearest
    /// settlement of the quest faction before timeoutDays.
    /// </summary>
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

            if (to == StoryDeliverTarget.colony)
            {
                SendColonyDeliverLetter(ctx);
            }
            else
            {
                Settlement target = FindNearestQuestSettlement(ctx);
                if (target != null)
                {
                    ctx.run.vars["settlementId"] = target.ID.ToString();
                    ctx.run.vars["settlementName"] = target.LabelCap.ToString();
                    string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Caravan Delivery"), ctx);
                    string body = BuildSettlementLetterText(ctx, target);
                    Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, new LookTargets(target)); // CHECK API
                }
                else
                {
                    // Fallback to colony hand-in if no settlement exists for the quest faction
                    Log.Warning("[TMM] StoryStage_Deliver: no settlement found for giverSettlement delivery; falling back to colony delivery.");
                    ctx.run.vars["fallbackColony"] = "true";
                    SendColonyDeliverLetter(ctx);
                }
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now + GenDate.TicksPerDay);

            if (GetBool(ctx, "delivered"))
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "success";
                return "success";
            }

            bool colonyMode = (to == StoryDeliverTarget.colony) || GetBool(ctx, "fallbackColony");
            if (colonyMode)
            {
                if (now >= deadline)
                {
                    RemoveDeliverLetters(ctx);
                    ctx.run.vars["finished"] = "timeout";
                    return "timeout";
                }

                // Re-send reminder letter if it was somehow lost
                if (!HasActiveDeliverLetter(ctx))
                {
                    SendColonyDeliverLetter(ctx);
                }
                return null;
            }

            // Caravan delivery to giverSettlement
            int settlementId = GetInt(ctx, "settlementId", -1);
            Settlement target = FindSettlementById(settlementId);
            if (target != null && thing != null && TryCompleteCaravanDelivery(target, thing, count))
            {
                ctx.run.vars["delivered"] = "true";
                ctx.run.vars["finished"] = "success";
                string doneTitle = StoryUtil.Format(!label.NullOrEmpty() ? label : "Delivery Complete", ctx);
                string doneBody = string.Format("Your caravan delivered {0} x{1} to {2}.", thing.label, count, target.LabelCap);
                Find.LetterStack.ReceiveLetter(doneTitle, doneBody, LetterDefOf.PositiveEvent, new LookTargets(target)); // CHECK API
                return "success";
            }

            if (now >= deadline)
            {
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string itemLabel = thing != null ? thing.label : "resources";
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", itemLabel, count), ctx);

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            bool colonyMode = (to == StoryDeliverTarget.colony) || GetBool(ctx, "fallbackColony");
            if (colonyMode)
            {
                int have = (ctx.Home != null && thing != null) ? OutpostPodLogistics.CountResource(ctx.Home, thing) : 0; // CHECK API
                return string.Format("{0} ({1}/{2} {3}, {4:0.0}d left)", baseText, have, count, itemLabel, daysLeft);
            }

            string targetName = GetStr(ctx, "settlementName");
            if (targetName.NullOrEmpty()) targetName = "faction settlement";
            return string.Format("{0} to {1} ({2:0.0}d left)", baseText, targetName, daysLeft);
        }

        private void SendColonyDeliverLetter(StoryContext ctx)
        {
            if (Find.LetterStack == null || thing == null) return;

            ChoiceLetter_StoryDeliver letter = new ChoiceLetter_StoryDeliver();
            letter.def = LetterDefOf.NeutralEvent;
            letter.Label = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", thing.label, count), ctx);
            letter.Text = BuildColonyLetterText(ctx);
            letter.questDefName = ctx.def != null ? ctx.def.defName : string.Empty;
            letter.runGen = ctx.run.gen;
            letter.thingDefName = thing.defName;
            letter.requiredCount = Mathf.Max(1, count);
            letter.lookTargets = ctx.Home != null ? new LookTargets(ctx.Home.Center, ctx.Home) : LookTargets.Invalid;

            Find.LetterStack.ReceiveLetter(letter); // CHECK API
        }

        private string BuildColonyLetterText(StoryContext ctx)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Gather {0} x{1} in {2}'s stockpiles within {3:0.0} days, then open this letter and click Deliver.",
                itemLabel, count, "{COLONY}", timeoutDays);
            instruction = StoryUtil.Format(instruction, ctx);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private string BuildSettlementLetterText(StoryContext ctx, Settlement target)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Send a caravan carrying {0} x{1} to {2} within {3:0.0} days.",
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
                if (dl != null && dl.questDefName == ctx.def.defName && dl.runGen == ctx.run.gen) return true;
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

        private static Settlement FindNearestQuestSettlement(StoryContext ctx)
        {
            Faction targetFac = ctx.Faction;
            PlanetTile homeTile = ResolveHomeTile(ctx); // CHECK API

            Settlement bestSameFaction = null;
            float bestSameDist = float.MaxValue;
            Settlement bestFriendly = null;
            float bestFriendlyDist = float.MaxValue;

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s == null || s.Destroyed || s.Faction == null || s.Faction.IsPlayer) continue;

                float dist = homeTile.Valid ? Find.WorldGrid.TraversalDistanceBetween(homeTile, s.Tile) : 0f; // CHECK API
                if (targetFac != null && s.Faction == targetFac)
                {
                    if (dist < bestSameDist)
                    {
                        bestSameDist = dist;
                        bestSameFaction = s;
                    }
                }
                else if (!s.Faction.HostileTo(Faction.OfPlayer))
                {
                    if (dist < bestFriendlyDist)
                    {
                        bestFriendlyDist = dist;
                        bestFriendly = s;
                    }
                }
            }

            return bestSameFaction ?? bestFriendly;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0) return null;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s != null && !s.Destroyed && s.ID == id) return s;
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

        private static bool TryCompleteCaravanDelivery(Settlement target, ThingDef targetDef, int requiredCount)
        {
            if (target == null || targetDef == null || requiredCount <= 0) return false;

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Destroyed || c.Faction != Faction.OfPlayer) continue;
                if (c.Tile != target.Tile) continue;

                List<Thing> items = CaravanInventoryUtility.AllInventoryItems(c); // CHECK API
                if (items == null) continue;

                int total = 0;
                for (int j = 0; j < items.Count; j++)
                {
                    Thing t = items[j];
                    if (t != null && !t.Destroyed && t.def == targetDef)
                    {
                        total += t.stackCount;
                    }
                }

                if (total < requiredCount) continue;

                int remaining = requiredCount;
                List<Thing> copy = new List<Thing>(items);
                for (int j = 0; j < copy.Count; j++)
                {
                    Thing t = copy[j];
                    if (t == null || t.Destroyed || t.def != targetDef) continue;
                    int take = Mathf.Min(remaining, t.stackCount);
                    Thing split = t.SplitOff(take);
                    if (split != null && !split.Destroyed)
                    {
                        split.Destroy(DestroyMode.Vanish);
                    }
                    remaining -= take;
                    if (remaining <= 0) break;
                }
                return remaining <= 0;
            }

            return false;
        }

        internal static StoryQuestRun FindQuestRun(string questDefName)
        {
            SparkStory story = SparkStory.Instance;
            if (story == null || questDefName.NullOrEmpty()) return null;

            // CHECK API: verify exact SparkStory run lookup method (e.g., story.GetRun or story.runs)
            Type t = story.GetType();
            MethodInfo getRun = t.GetMethod("GetRun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getRun != null)
            {
                return getRun.Invoke(story, new object[] { questDefName }) as StoryQuestRun;
            }

            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField != null)
            {
                object val = runsField.GetValue(story);
                Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
                if (dict != null)
                {
                    StoryQuestRun r;
                    if (dict.TryGetValue(questDefName, out r)) return r;
                }
                List<StoryQuestRun> list = val as List<StoryQuestRun>;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && list[i].quest == questDefName) return list[i];
                    }
                }
            }
            return null;
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
```

`Defs/StoryDefs/Story_Samples_E3.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_Sample_Hotel</defName>
    <label>sample hotel delegation</label>
    <act>1</act>
    <main>false</main>
    <priority>50</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_preston_garvey</giver>
    <requires>
      <li>story.e3samples=true</li>
    </requires>
    <letterLabel>Sample Hotel Delegation</letterLabel>
    <letterText>{GIVER} requests guest lodging at {COLONY}'s waystation for a short diplomatic visit.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <key>visit</key>
        <label>Host the visiting delegation</label>
        <text>{GIVER} and an escort have arrived to stay at {COLONY}'s hotel. Ensure their visit ends peacefully.</text>
        <faction>TMM_MinutemenHuntersFaction</faction>
        <hero>TMM_Hero_preston_garvey</hero>
        <groupSize>2</groupSize>
        <stayDays>2</stayDays>
        <objective>Host</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e3sample_hotelSuccess</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e3sample_hotelFailed</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>noHotel</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e3sample_noHotel</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_Sample_Deliver</defName>
    <label>sample steel requisition</label>
    <act>1</act>
    <main>false</main>
    <priority>49</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_preston_garvey</giver>
    <requires>
      <li>story.e3samples=true</li>
    </requires>
    <letterLabel>Sample Steel Requisition</letterLabel>
    <letterText>{GIVER} requests 50 steel from {COLONY}'s stockpiles to reinforce a nearby watchtower.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <key>deliver</key>
        <label>Deliver 50 steel</label>
        <text>Allocate 50 steel from {COLONY}'s stockpiles and confirm the handover.</text>
        <thing>Steel</thing>
        <count>50</count>
        <to>colony</to>
        <timeoutDays>10</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e3sample_deliverSuccess</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.e3sample_deliverTimeout</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Samples_E3.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_Sample_Hotel -->
  <TMM_Story_Sample_Hotel.label>пробный визит в гостиницу</TMM_Story_Sample_Hotel.label>
  <TMM_Story_Sample_Hotel.letterLabel>Пробный визит в гостиницу</TMM_Story_Sample_Hotel.letterLabel>
  <TMM_Story_Sample_Hotel.letterText>{GIVER} просит разместить небольшую делегацию в гостинице {COLONY} на время короткого дипломатического визита.</TMM_Story_Sample_Hotel.letterText>
  <TMM_Story_Sample_Hotel.stages.0.label>Принять делегацию в гостинице</TMM_Story_Sample_Hotel.stages.0.label>
  <TMM_Story_Sample_Hotel.stages.0.text>{GIVER} с сопровождающим прибыли в гостиницу {COLONY}. Обеспечьте им спокойный приём.</TMM_Story_Sample_Hotel.stages.0.text>

  <!-- TMM_Story_Sample_Deliver -->
  <TMM_Story_Sample_Deliver.label>пробная поставка стали</TMM_Story_Sample_Deliver.label>
  <TMM_Story_Sample_Deliver.letterLabel>Пробная поставка стали</TMM_Story_Sample_Deliver.letterLabel>
  <TMM_Story_Sample_Deliver.letterText>{GIVER} запрашивает 50 стали со складов {COLONY} для укрепления соседней сторожевой башни.</TMM_Story_Sample_Deliver.letterText>
  <TMM_Story_Sample_Deliver.stages.0.label>Сдать 50 стали</TMM_Story_Sample_Deliver.stages.0.label>
  <TMM_Story_Sample_Deliver.stages.0.text>Подготовьте 50 стали на складах {COLONY} и подтвердите передачу в письме.</TMM_Story_Sample_Deliver.stages.0.text>

</LanguageData>
```

`Tests/InGameStoryHotelProbe.cs`

```csharp
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

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class StoryHotelProbeBootstrap
    {
        public const string SaveName = "TMM-StoryHotel";

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
                if (ReloadMode && Current.ProgramState == ProgramState.Entry)
                {
                    GameDataSaveLoader.LoadGame(SaveName); // CHECK API
                }
            });
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
    /// In-game verification probe for StoryStage_HotelVisit (noHotel, Host with HeroRegistry identity,
    /// Save/Reload mid-stay) and StoryStage_Deliver (colony stockpile deduction).
    /// Activated via -tmm-story-hotel-probe (fresh) or -tmm-story-hotel-reload (reload).
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
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(StoryHotelProbeBootstrap.ReloadMode ? "StoryHotelReloadProbe.json" : "StoryHotelProbe.json");
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

        private void RunFreshProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("SparkStory_Exists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            StoryQuestDef hotelDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Hotel");
            StoryQuestDef deliverDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Deliver");
            AddCheck("SampleE3Defs_Loaded", hotelDef != null && deliverDef != null, "both loaded",
                string.Format("hotel={0}, deliver={1}", hotelDef != null, deliverDef != null));
            if (hotelDef == null || deliverDef == null) return;

            story.SetFlag("story.e3samples", "true"); // CHECK API

            // ---------------- TEST 1: NO HOTEL ----------------
            SetGuestsWelcome(map, false);
            int lettersBeforeNoHotel = Find.LetterStack.LettersListForReading.Count;

            story.DirectorTick(); // CHECK API
            bool acceptedNoHotel = AcceptOfferLetter(hotelDef.letterLabel);
            AddCheck("NoHotel_OfferAccepted", acceptedNoHotel, "true", acceptedNoHotel.ToString());
            story.DirectorTick();

            StoryQuestRun noHotelRun = FindQuestRun(story, "TMM_Story_Sample_Hotel");
            bool gotNoHotelOutcome = noHotelRun != null && HasHistoryEntry(noHotelRun, "noHotel");
            bool gotGuidanceLetter = Find.LetterStack.LettersListForReading.Count >= lettersBeforeNoHotel;
            AddCheck("NoHotel_OutcomeNoHotel", gotNoHotelOutcome, "noHotel",
                noHotelRun != null ? string.Join(",", noHotelRun.history.ToArray()) : "null");
            AddCheck("NoHotel_GuidanceLetterSent", gotGuidanceLetter, "letter present",
                Find.LetterStack.LettersListForReading.Count.ToString());

            // ---------------- BUILD READY HOTEL ----------------
            bool hotelBuilt = BuildTestHotel(map);
            SetGuestsWelcome(map, true);
            bool openHotel = StoryStage_HotelVisit.HasOpenHotel(map);
            AddCheck("Hotel_BuiltAndOpen", hotelBuilt && openHotel, "built=true, open=true",
                string.Format("built={0}, open={1}", hotelBuilt, openHotel));

            // ---------------- TEST 2: HOST WITH REGISTERED HERO ----------------
            StoryStage_HotelVisit hotelStage = hotelDef.stages[0] as StoryStage_HotelVisit;
            Pawn expectedHero = (hotelStage != null && hotelStage.hero != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(hotelStage.hero) : null;
            string expectedHeroId = expectedHero != null ? expectedHero.ThingID : string.Empty;

            ResetQuestRun(story, "TMM_Story_Sample_Hotel");
            story.DirectorTick();
            bool acceptedHost = AcceptOfferLetter(hotelDef.letterLabel);
            AddCheck("Host_OfferAccepted", acceptedHost, "true", acceptedHost.ToString());
            story.DirectorTick();

            StoryQuestRun hostRun = FindQuestRun(story, "TMM_Story_Sample_Hotel");
            bool guestsArrived = hostRun != null && GetVar(hostRun, "arrived") == "true";
            string actualHeroId = GetVar(hostRun, "heroThingId");
            bool heroIsRegistryPawn = !expectedHeroId.NullOrEmpty() && actualHeroId == expectedHeroId &&
                                      expectedHero != null && expectedHero.Spawned && expectedHero.Map == map;

            AddCheck("Host_GuestsArrived", guestsArrived, "arrived=true", GetVar(hostRun, "arrived"));
            AddCheck("Host_HeroMatchesRegistry", heroIsRegistryPawn, expectedHeroId,
                string.Format("var={0}, spawned={1}", actualHeroId, expectedHero != null && expectedHero.Spawned));

            float stay = hotelStage != null ? hotelStage.stayDays : 2f;
            AdvanceTicks(Mathf.CeilToInt((stay + 0.25f) * GenDate.TicksPerDay));
            story.DirectorTick();

            bool departureTriggered = GetVar(hostRun, "departureTriggered") == "true";
            AddCheck("Host_DepartureTriggeredAfterStay", departureTriggered, "true", GetVar(hostRun, "departureTriggered"));

            ExitTrackedGuestsCleanly(map, hostRun);
            story.DirectorTick();

            bool hostSucceeded = hostRun != null && HasHistoryEntry(hostRun, "success");
            AddCheck("Host_OutcomeSuccess", hostSucceeded, "success",
                hostRun != null ? string.Join(",", hostRun.history.ToArray()) : "null");

            // ---------------- TEST 3: DELIVER COLONY (50 STEEL) ----------------
            SpawnUnforbiddenResource(map, ThingDefOf.Steel, 50);
            int steelBefore = OutpostPodLogistics.CountResource(map, ThingDefOf.Steel); // CHECK API

            story.DirectorTick();
            bool acceptedDeliver = AcceptOfferLetter(deliverDef.letterLabel);
            AddCheck("Deliver_OfferAccepted", acceptedDeliver, "true", acceptedDeliver.ToString());
            story.DirectorTick();

            ChoiceLetter_StoryDeliver deliverLetter = FindDeliverLetter("TMM_Story_Sample_Deliver");
            AddCheck("Deliver_HandInLetterFound", deliverLetter != null, "found", deliverLetter != null ? "found" : "missing");

            bool handedIn = deliverLetter != null && ClickFirstChoice(deliverLetter);
            AddCheck("Deliver_HandInClicked", handedIn, "true", handedIn.ToString());
            story.DirectorTick();

            int steelAfter = OutpostPodLogistics.CountResource(map, ThingDefOf.Steel); // CHECK API
            StoryQuestRun deliverRun = FindQuestRun(story, "TMM_Story_Sample_Deliver");
            bool steelDeducted = (steelBefore - steelAfter) == 50;
            bool deliverSucceeded = deliverRun != null && HasHistoryEntry(deliverRun, "success");

            AddCheck("Deliver_SteelDeducted50", steelDeducted, (steelBefore - 50).ToString(), steelAfter.ToString());
            AddCheck("Deliver_OutcomeSuccess", deliverSucceeded, "success",
                deliverRun != null ? string.Join(",", deliverRun.history.ToArray()) : "null");

            // ---------------- TEST 4: SAVE MID-STAY FOR RELOAD ----------------
            ResetQuestRun(story, "TMM_Story_Sample_Hotel");
            story.DirectorTick();
            bool acceptedReload = AcceptOfferLetter(hotelDef.letterLabel);
            story.DirectorTick();

            StoryQuestRun saveRun = FindQuestRun(story, "TMM_Story_Sample_Hotel");
            bool activeMidStay = acceptedReload && saveRun != null && saveRun.stage == 0 && GetVar(saveRun, "arrived") == "true";
            AddCheck("ReloadPrep_ActiveStaySaved", activeMidStay, "stage=0, arrived=true",
                saveRun != null ? string.Format("stage={0}, arrived={1}", saveRun.stage, GetVar(saveRun, "arrived")) : "null");

            GameDataSaveLoader.SaveGame(StoryHotelProbeBootstrap.SaveName); // CHECK API
        }

        private void RunReloadProbe()
        {
            SparkStory story = SparkStory.Instance;
            AddCheck("Reload_SparkStoryExists", story != null, "non-null", story != null ? "ok" : "null");
            if (story == null) return;

            StoryQuestDef hotelDef = DefDatabase<StoryQuestDef>.GetNamedSilentFail("TMM_Story_Sample_Hotel");
            StoryQuestRun run = FindQuestRun(story, "TMM_Story_Sample_Hotel");
            AddCheck("Reload_RunRestoredMidStay", run != null && run.stage == 0 && GetVar(run, "arrived") == "true",
                "stage=0, arrived=true",
                run != null ? string.Format("stage={0}, arrived={1}", run.stage, GetVar(run, "arrived")) : "null");
            if (run == null || hotelDef == null) return;

            StoryStage_HotelVisit stage = hotelDef.stages[0] as StoryStage_HotelVisit;
            float stay = stage != null ? stage.stayDays : 2f;

            AdvanceTicks(Mathf.CeilToInt((stay + 0.25f) * GenDate.TicksPerDay));
            story.DirectorTick();

            ExitTrackedGuestsCleanly(map, run);
            story.DirectorTick();

            bool completedOnce = CountHistoryEntries(run, "success") == 1;
            AddCheck("Reload_CompletesOnce", completedOnce, "successCount=1",
                "successCount=" + CountHistoryEntries(run, "success"));

            story.DirectorTick();
            story.DirectorTick();
            story.DirectorTick();

            bool stillOnce = CountHistoryEntries(run, "success") == 1;
            AddCheck("Reload_NoDuplicateTrigger", stillOnce, "successCount=1",
                "successCount=" + CountHistoryEntries(run, "success"));
        }

        private static bool BuildTestHotel(Map targetMap)
        {
            if (targetMap == null) return false;

            // Locate Hospitality guest bed def and optional reception desk def
            ThingDef guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("GuestBed"); // CHECK API
            if (guestBedDef == null) guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("GuestDoubleBed"); // CHECK API
            if (guestBedDef == null) guestBedDef = DefDatabase<ThingDef>.GetNamedSilentFail("GuestSleepingSpot"); // CHECK API
            ThingDef receptionDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_HotelReception"); // CHECK API

            IntVec3 baseCell;
            if (!CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 12, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
            }, out baseCell))
            {
                baseCell = targetMap.Center;
            }

            if (receptionDef != null)
            {
                ThingDef stuff = receptionDef.MadeFromStuff ? ThingDefOf.WoodLog : null;
                Thing rec = ThingMaker.MakeThing(receptionDef, stuff);
                rec.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(rec, baseCell, targetMap, WipeMode.Vanish);
            }

            int spawnedBeds = 0;
            if (guestBedDef != null)
            {
                ThingDef bedStuff = guestBedDef.MadeFromStuff ? ThingDefOf.WoodLog : null;
                for (int i = 0; i < 3; i++)
                {
                    IntVec3 cell;
                    if (CellFinder.TryFindRandomCellNear(baseCell, targetMap, 8, delegate(IntVec3 c)
                    {
                        return c.InBounds(targetMap) && c.Standable(targetMap) && c.GetEdifice(targetMap) == null;
                    }, out cell))
                    {
                        Thing bed = ThingMaker.MakeThing(guestBedDef, bedStuff);
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
            if (field != null)
            {
                field.SetValue(comp, welcome);
            }
        }

        private static void ExitTrackedGuestsCleanly(Map targetMap, StoryQuestRun run)
        {
            if (targetMap == null || run == null) return;
            string raw = GetVar(run, "guestIds");
            if (raw.NullOrEmpty()) return;

            string[] parts = raw.Split(',');
            HashSet<int> idSet = new HashSet<int>();
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) idSet.Add(val);
            }

            List<Pawn> snapshot = new List<Pawn>(targetMap.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p == null || !idSet.Contains(p.thingIDNumber)) continue;
                p.ExitMap(false, Rot4.South); // CHECK API
                if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                if (!p.Dead && !Find.WorldPawns.Contains(p))
                {
                    Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever); // CHECK API
                }
            }
        }

        private static void SpawnUnforbiddenResource(Map targetMap, ThingDef def, int count)
        {
            Thing t = ThingMaker.MakeThing(def);
            t.stackCount = count;
            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 10, delegate(IntVec3 c)
            {
                return c.InBounds(targetMap) && c.Standable(targetMap);
            }, out cell))
            {
                cell = targetMap.Center;
            }
            GenPlace.TryPlaceThing(t, cell, targetMap, ThingPlaceMode.Near);
            t.SetForbidden(false, false);
        }

        private static bool AcceptOfferLetter(string expectedLabel)
        {
            if (Find.LetterStack == null) return false;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                ChoiceLetter cl = letters[i] as ChoiceLetter;
                if (cl == null) continue;
                if (expectedLabel.NullOrEmpty() || string.Equals(cl.Label.ToString(), expectedLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return ClickFirstChoice(cl);
                }
            }
            return false;
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

        private static bool ClickFirstChoice(ChoiceLetter letter)
        {
            if (letter == null) return false;
            IEnumerable<DiaOption> choices = letter.Choices; // CHECK API
            if (choices == null) return false;

            foreach (DiaOption opt in choices)
            {
                if (opt != null && opt.action != null && !opt.disabled)
                {
                    opt.action();
                    if (Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(letter))
                    {
                        Find.LetterStack.RemoveLetter(letter);
                    }
                    return true;
                }
            }
            return false;
        }

        private static void AdvanceTicks(int deltaTicks)
        {
            if (deltaTicks <= 0) return;
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + deltaTicks); // CHECK API
        }

        private static StoryQuestRun FindQuestRun(SparkStory story, string questDefName)
        {
            // CHECK API: verify exact SparkStory run lookup method
            Type t = story.GetType();
            MethodInfo getRun = t.GetMethod("GetRun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getRun != null)
            {
                return getRun.Invoke(story, new object[] { questDefName }) as StoryQuestRun;
            }

            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField != null)
            {
                object val = runsField.GetValue(story);
                Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
                if (dict != null)
                {
                    StoryQuestRun r;
                    if (dict.TryGetValue(questDefName, out r)) return r;
                }
                List<StoryQuestRun> list = val as List<StoryQuestRun>;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && list[i].quest == questDefName) return list[i];
                    }
                }
            }
            return null;
        }

        private static void ResetQuestRun(SparkStory story, string questDefName)
        {
            // CHECK API: remove previous run from SparkStory for re-testing
            Type t = story.GetType();
            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField == null) return;

            object val = runsField.GetValue(story);
            Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
            if (dict != null)
            {
                dict.Remove(questDefName);
                return;
            }
            List<StoryQuestRun> list = val as List<StoryQuestRun>;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] != null && list[i].quest == questDefName) list.RemoveAt(i);
                }
            }
        }

        private static bool HasHistoryEntry(StoryQuestRun run, string outcome)
        {
            return CountHistoryEntries(run, outcome) > 0;
        }

        private static int CountHistoryEntries(StoryQuestRun run, string outcome)
        {
            if (run == null || run.history == null) return 0;
            int count = 0;
            string suffix = ":" + outcome;
            for (int i = 0; i < run.history.Count; i++)
            {
                string h = run.history[i];
                if (h != null && (h == outcome || h.EndsWith(suffix, StringComparison.Ordinal)))
                {
                    count++;
                }
            }
            return count;
        }

        private static string GetVar(StoryQuestRun run, string key)
        {
            if (run == null || run.vars == null) return string.Empty;
            string raw;
            return run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
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

        private void WriteReport(string fileName)
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