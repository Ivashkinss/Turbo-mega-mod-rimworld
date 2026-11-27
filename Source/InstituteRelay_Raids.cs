using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace TurboMegaMod
{
    // =========================================================================
    // 1. XML Extension on FactionDef for Relay Teleportation
    // =========================================================================

    public sealed class FactionTeleportExtension : DefModExtension
    {
        public int teleportDelayTicks = 2500;
        public FleckDef flashFleck;
        public SoundDef soundDef;
        public bool allowWorldJumps = true;
    }

    // =========================================================================
    // 2. Visual / Audio Effect Helper & Relay Sickness / Immunity Helper
    // =========================================================================

    public static class InstituteRelayFx
    {
        public const string InstituteFactionDefName = "TMM_InstituteFaction";
        public const string RelayFlashFleckDefName = "TMM_RelayFlash";
        public const string RelaySicknessHediffDefName = "TMM_RelaySickness";

        public static Faction FindInstituteFaction()
        {
            if (Find.FactionManager == null) return null;
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail(InstituteFactionDefName);
            if (def == null) return null;
            Faction f = Find.FactionManager.FirstFactionOfDef(def);
            if (f == null || f.defeated || f.IsPlayer) return null;
            return f;
        }

        public static FactionTeleportExtension GetTeleportExtension(Faction faction)
        {
            if (faction == null || faction.def == null) return null;
            return faction.def.GetModExtension<FactionTeleportExtension>();
        }

        public static bool FactionHasRelayTeleport(Faction faction)
        {
            return GetTeleportExtension(faction) != null;
        }

        public static void SpawnRelayFlash(IntVec3 cell, Map map, Faction faction, float scale)
        {
            if (map == null || !cell.InBounds(map)) return;

            FactionTeleportExtension ext = GetTeleportExtension(faction);
            FleckDef fleck = ext != null ? ext.flashFleck : null;
            if (fleck == null)
            {
                fleck = DefDatabase<FleckDef>.GetNamedSilentFail(RelayFlashFleckDefName);
            }
            if (fleck == null)
            {
                fleck = FleckDefOf.PsycastAreaEffect; // CHECK API
            }

            Vector3 drawPos = cell.ToVector3Shifted();
            if (fleck != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos, map, fleck, scale); // CHECK API
                data.rotationRate = Rand.Range(-60f, 60f);
                data.velocitySpeed = 0f;
                map.flecks.CreateFleck(data); // CHECK API
            }

            // Secondary bright spark burst for unmistakable Institute blue relay silhouette
            if (FleckDefOf.LightningGlow != null)
            {
                FleckMaker.ThrowLightningGlow(drawPos, map, scale * 0.9f);
            }

            SoundDef sound = ext != null ? ext.soundDef : null;
            if (sound == null)
            {
                sound = DefDatabase<SoundDef>.GetNamedSilentFail("Psycast_Skip_Exit"); // CHECK API
            }
            if (sound == null)
            {
                sound = SoundDefOf.EnergyShield_Reset;
            }
            if (sound != null)
            {
                sound.PlayOneShot(new TargetInfo(cell, map));
            }
        }

        /// <summary>
        /// Non-biological pawns, synths, robot chassis, and super mutants are immune to relay sickness.
        /// Checked via xenotype/gene defNames directly (all xenotypes are ours, no MayRequire needed).
        /// </summary>
        public static bool IsImmuneToRelaySickness(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return true;
            if (pawn.RaceProps == null || !pawn.RaceProps.IsFlesh) return true;
            if (pawn.RaceProps.IsMechanoid) return true;

            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                XenotypeDef xeno = pawn.genes.Xenotype;
                if (xeno != null && !string.IsNullOrEmpty(xeno.defName))
                {
                    string xn = xeno.defName;
                    if (xn == "TMM_SynthGen1"
                        || xn == "TMM_SynthGen2"
                        || xn == "TMM_SynthGen3"
                        || xn == "TMM_RobotChassis"
                        || xn == "TMM_SuperMutant")
                    {
                        return true;
                    }
                }

                List<Gene> genes = pawn.genes.GenesListForReading;
                if (genes != null)
                {
                    for (int i = 0; i < genes.Count; i++)
                    {
                        Gene g = genes[i];
                        if (g == null || !g.Active || g.def == null || string.IsNullOrEmpty(g.def.defName)) continue;
                        string gn = g.def.defName;
                        if (gn.StartsWith("TMM_Synth_", StringComparison.Ordinal)
                            || gn.StartsWith("TMM_Chassis_", StringComparison.Ordinal)
                            || gn == "TMM_Head_SynthGen1"
                            || gn == "TMM_Head_SynthGen2"
                            || gn == "TMM_Head_SynthGen3"
                            || gn == "TMM_Body_SuperMutant"
                            || gn == "TMM_Body_SuperMutantFemale")
                        {
                            return true;
                        }
                    }
                }
            }

            // Also check revealed/hidden synth infiltrator hediff from Task 61
            if (pawn.health != null && pawn.health.hediffSet != null)
            {
                HediffDef revealed = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_RevealedSynth");
                if (revealed != null && pawn.health.hediffSet.HasHediff(revealed)) return true;
            }

            return false;
        }

        public static void ApplyRelaySicknessIfSusceptible(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.health == null) return;
            if (IsImmuneToRelaySickness(pawn)) return;

            HediffDef sicknessDef = DefDatabase<HediffDef>.GetNamedSilentFail(RelaySicknessHediffDefName);
            if (sicknessDef == null) return;

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(sicknessDef);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
            pawn.health.AddHediff(HediffMaker.MakeHediff(sicknessDef, pawn));
        }

        /// <summary>
        /// Finds a standable relay arrival cell outside player houses, 15..35 cells from the nearest colonist
        /// and reachable from that colonist. Returns IntVec3.Invalid if none exists.
        /// </summary>
        public static IntVec3 FindRelayDropCell(Map map, int minDist, int maxDist, Pawn preferredFocus)
        {
            if (map == null || map.mapPawns == null) return IntVec3.Invalid;

            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0) return IntVec3.Invalid;

            Pawn anchor = preferredFocus != null && preferredFocus.Spawned && preferredFocus.Map == map
                ? preferredFocus
                : colonists.RandomElement();

            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(anchor.Position, map, maxDist,
                delegate(IntVec3 c)
                {
                    if (!IsValidRelaySpot(c, map)) return false;

                    float nearestDist = DistanceToNearestColonist(c, colonists);
                    if (nearestDist < minDist || nearestDist > maxDist) return false;

                    return map.reachability.CanReach(c, anchor.Position, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors));
                },
                out found))
            {
                return found;
            }

            // Broad map scan fallback before giving up to edge entry
            if (CellFinder.TryFindRandomCell(map,
                delegate(IntVec3 c)
                {
                    if (!IsValidRelaySpot(c, map)) return false;
                    float nearestDist = DistanceToNearestColonist(c, colonists);
                    if (nearestDist < minDist || nearestDist > maxDist) return false;
                    return map.reachability.CanReach(c, anchor.Position, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors));
                },
                out found))
            {
                return found;
            }

            return IntVec3.Invalid;
        }

        public static bool IsValidRelaySpot(IntVec3 c, Map map)
        {
            if (map == null || !c.InBounds(map) || c.Fogged(map)) return false;
            if (!c.Standable(map)) return false;
            if (c.GetEdifice(map) != null) return false;

            // Outside player buildings: must not be under a constructed roof or inside an enclosed player room
            RoofDef roof = c.GetRoof(map);
            if (roof != null && !roof.isNatural) return false;

            Room room = c.GetRoom(map);
            if (room != null && !room.PsychologicallyOutdoors)
            {
                if (map.areaManager != null && map.areaManager.Home != null && map.areaManager.Home[c])
                {
                    return false;
                }
            }

            TerrainDef terr = c.GetTerrain(map);
            if (terr != null && terr.IsWater) return false;

            return true;
        }

        public static float DistanceToNearestColonist(IntVec3 cell, List<Pawn> colonists)
        {
            float best = float.MaxValue;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn c = colonists[i];
                if (c == null || !c.Spawned || c.Dead) continue;
                float d = cell.DistanceTo(c.Position);
                if (d < best) best = d;
            }
            return best;
        }

        public static IntVec3 FindNearbyStandableCell(IntVec3 center, Map map, int radius)
        {
            IntVec3 cell;
            if (CellFinder.TryFindRandomCellNear(center, map, radius,
                delegate(IntVec3 c) { return c.InBounds(map) && !c.Fogged(map) && c.Standable(map) && c.GetEdifice(map) == null; },
                out cell))
            {
                return cell;
            }
            return center;
        }

        public static Pawn GenerateSynthPawn(Faction institute, Map map, string xenotypeDefName)
        {
            PawnKindDef kind = institute != null ? institute.RandomPawnKind() : null;
            if (kind == null) kind = PawnKindDefOf.Villager;

            XenotypeDef xeno = null;
            if (ModsConfig.BiotechActive && !string.IsNullOrEmpty(xenotypeDefName))
            {
                xeno = DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotypeDefName);
            }

            PlanetTile tile = map != null ? map.Tile : PlanetTile.Invalid;
            PawnGenerationRequest req = new PawnGenerationRequest(
                kind, institute, PawnGenerationContext.NonPlayer, tile,
                true, false, false, false, true, 1f, false, true, false, false,
                false, false, false, false, false, 0f, 0f, null, 1f,
                null, null, null, null, null, null, null, null, null, null, null, null,
                false, false, false, false, null, null, null, null, null, 0f,
                DevelopmentalStage.Adult); // CHECK API

            if (xeno != null)
            {
                req.ForcedXenotype = xeno; // CHECK API
            }

            return PawnGenerator.GeneratePawn(req);
        }

        /// <summary>
        /// Cleanly teleports an eligible NPC pawn off the map with a blue flash, handing any kidnapped pawn to the faction.
        /// Never touches dead, downed, destroyed, unspawned, or imprisoned pawns.
        /// </summary>
        public static string TestNpcTeleportFailure;

        public static bool TeleportNpcOffMap(Pawn pawn, Faction faction)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Downed || pawn.IsPrisoner) return false;
            if (!pawn.Spawned || pawn.Map == null || Find.WorldPawns == null) return false;

            Map map = pawn.Map;
            IntVec3 pos = pawn.Position;
            Faction f = faction != null ? faction : pawn.Faction;

            Pawn carriedVictim = pawn.carryTracker == null ? null : pawn.carryTracker.CarriedThing as Pawn;
            bool kidnapping = carriedVictim != null && carriedVictim.Faction == Faction.OfPlayer;
            if (kidnapping && (f == null || f == Faction.OfPlayer || f.kidnapped == null || carriedVictim.Dead ||
                carriedVictim.Destroyed || f.kidnapped.KidnappedPawnsListForReading.Contains(carriedVictim))) return false;
            if (carriedVictim != null)
            {
                if (TestMode.Enabled && TestNpcTeleportFailure == "drop") return false;
                Thing dropped;
                if (!pawn.carryTracker.TryDropCarriedThing(pos, ThingPlaceMode.Near, out dropped) ||
                    dropped != carriedVictim || !carriedVictim.Spawned || carriedVictim.Map != map ||
                    pawn.carryTracker.CarriedThing == carriedVictim) return false;
            }

            Lord lord = pawn.GetLord();
            Rot4 rotation = pawn.Rotation;
            IntVec3 victimCell = carriedVictim == null ? IntVec3.Invalid : carriedVictim.Position;
            Rot4 victimRotation = carriedVictim == null ? Rot4.South : carriedVictim.Rotation;
            bool wasPinned = Find.WorldPawns.ForcefullyKeptPawns.Contains(pawn);
            try
            {
                if (TestMode.Enabled && TestNpcTeleportFailure == "leave")
                    throw new InvalidOperationException("Simulated relay extraction failure.");
                if (lord != null) lord.RemovePawn(pawn);
                pawn.DeSpawn(DestroyMode.Vanish);
                if (!Find.WorldPawns.Contains(pawn))
                    Find.WorldPawns.PassToWorld(pawn, kidnapping ? PawnDiscardDecideMode.KeepForever : PawnDiscardDecideMode.Decide);

                if (kidnapping)
                {
                    if (pawn.Spawned || !Find.WorldPawns.Contains(pawn)) throw new InvalidOperationException("Captor did not leave the map.");
                    f.kidnapped.Kidnap(carriedVictim, pawn);
                    if (!KidnapConfirmed(f, carriedVictim)) throw new InvalidOperationException("Kidnapping was not confirmed.");
                }
            }
            catch (Exception ex)
            {
                TestMode.ReportException("[TMM] Relay extraction: ", ex);
                if (!kidnapping || !KidnapConfirmed(f, carriedVictim))
                {
                    if (kidnapping)
                    {
                        if (f.kidnapped.KidnappedPawnsListForReading.Contains(carriedVictim))
                            f.kidnapped.RemoveKidnappedPawn(carriedVictim);
                        RestoreRelayPawn(carriedVictim, map, victimCell, victimRotation);
                    }
                    RestoreRelayPawn(pawn, map, pos, rotation);
                    if (lord != null && pawn.Spawned && !lord.ownedPawns.Contains(pawn)) lord.AddPawn(pawn);
                    return false;
                }
            }

            if (kidnapping && !wasPinned) Find.WorldPawns.ForcefullyKeptPawns.Remove(pawn);
            SpawnRelayFlash(pos, map, f, 1.8f);
            return !pawn.Spawned;
        }

        private static bool KidnapConfirmed(Faction faction, Pawn victim)
        {
            return !victim.Spawned && !victim.Destroyed && Find.WorldPawns.Contains(victim) &&
                faction.kidnapped.KidnappedPawnsListForReading.Contains(victim);
        }

        private static void RestoreRelayPawn(Pawn pawn, Map map, IntVec3 cell, Rot4 rotation)
        {
            if (pawn.Spawned || pawn.Destroyed || pawn.holdingOwner != null) return;
            try
            {
                if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
                GenSpawn.Spawn(pawn, cell, map, rotation);
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] Relay extraction could not restore a pawn to its cell: " + ex.Message);
                if (!pawn.Spawned && pawn.holdingOwner == null && !Find.WorldPawns.Contains(pawn))
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }
        }
    }

    // =========================================================================
    // 3. Persistent Data Records (Abduction Squads, Retreat Queue, World Jumps)
    // =========================================================================

    public sealed class InstituteAbductionTracker : IExposable
    {
        public Map map;
        public Faction faction;
        public List<Pawn> squad = new List<Pawn>();
        public Pawn targetVictim;
        public Pawn captor;
        public int extractionTick = -1;
        public int startTick;
        public bool completed;

        public void ExposeData()
        {
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref squad, "squad", LookMode.Reference);
            Scribe_References.Look(ref targetVictim, "targetVictim");
            Scribe_References.Look(ref captor, "captor");
            Scribe_Values.Look(ref extractionTick, "extractionTick", -1);
            Scribe_Values.Look(ref startTick, "startTick", 0);
            Scribe_Values.Look(ref completed, "completed", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && squad == null)
            {
                squad = new List<Pawn>();
            }
        }
    }

    public sealed class PendingRetreatExtraction : IExposable
    {
        public Pawn pawn;
        public Faction faction;
        public Map map;
        public int triggerTick;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref map, "map");
            Scribe_Values.Look(ref triggerTick, "triggerTick", 0);
        }
    }

    /// <summary>
    /// Tracks an Institute faction group jumping across the world map between settlements.
    /// Pawns are kept in Find.WorldPawns and referenced cleanly via Scribe_Collections(LookMode.Reference)
    /// so save/load mid-jump never loses or duplicates any pawn.
    /// </summary>
    public sealed class PendingWorldRelayJump : IExposable
    {
        public Faction faction;
        public List<Pawn> pawns = new List<Pawn>();
        public PlanetTile originTile = PlanetTile.Invalid;
        public PlanetTile destinationTile = PlanetTile.Invalid;
        public int arriveTick;
        public bool spawnCaravanAtDestination;
        public bool completed;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref originTile, "originTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref destinationTile, "destinationTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref arriveTick, "arriveTick", 0);
            Scribe_Values.Look(ref spawnCaravanAtDestination, "spawnCaravanAtDestination", false);
            Scribe_Values.Look(ref completed, "completed", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (pawns == null) pawns = new List<Pawn>();
                pawns.RemoveAll(delegate(Pawn p) { return p == null; });
            }
        }
    }

    /// <summary>
    /// Independent WorldObject marker displayed on the world map while an Institute relay jump is in transit.
    /// // CHECK integration WorldTraffic: Claude can also query active WorldObject_TMMRelayJump instances.
    /// </summary>
    public sealed class WorldObject_TMMRelayJump : WorldObject
    {
        public PlanetTile destinationTile = PlanetTile.Invalid;
        public int arriveTick;

        public override string GetInspectString()
        {
            string baseStr = base.GetInspectString();
            int ticksLeft = Find.TickManager != null ? Math.Max(0, arriveTick - Find.TickManager.TicksGame) : 0;
            string extra = "TMM.Relay.WorldJumpInspect".Translate(ticksLeft.ToStringTicksToPeriod());
            return string.IsNullOrEmpty(baseStr) ? extra : (baseStr + "\n" + extra);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref destinationTile, "destinationTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref arriveTick, "arriveTick", 0);
        }
    }

    // =========================================================================
    // 4. Central Relay Manager (GameComponent) & Story Unlock API
    // =========================================================================

    public static class TMMRelayUnlock
    {
        public static bool IsUnlocked
        {
            get
            {
                TMM_InstituteRelayManager mgr = TMM_InstituteRelayManager.Instance;
                return mgr != null && mgr.relayTechUnlocked;
            }
        }

        /// <summary>
        /// Called by Institute or Railroad story quest to unlock TMM_RelayTechnology research.
        /// // TODO story: invoke TMMRelayUnlock.Unlock() from the Institute/Railroad quest completion reward.
        /// </summary>
        public static void Unlock()
        {
            TMM_InstituteRelayManager mgr = TMM_InstituteRelayManager.Instance;
            if (mgr == null || mgr.relayTechUnlocked) return;
            mgr.relayTechUnlocked = true;
            Messages.Message("TMM.Relay.TechUnlockedMsg".Translate(), MessageTypeDefOf.PositiveEvent);
        }
    }

    public sealed class TMM_InstituteRelayManager : GameComponent
    {
        private const int CheckInterval = 30;
        public const int AbductionExtractDelayTicks = 1500;
        public const int RetreatChargeDelayTicks = 90;

        public bool relayTechUnlocked;
        public List<InstituteAbductionTracker> abductions = new List<InstituteAbductionTracker>();
        public List<PendingRetreatExtraction> pendingRetreats = new List<PendingRetreatExtraction>();
        public List<PendingWorldRelayJump> worldJumps = new List<PendingWorldRelayJump>();
        public int totalRetreatsTeleported;
        public int totalWorldJumpsCompleted;

        public TMM_InstituteRelayManager(Game game) { }

        public static TMM_InstituteRelayManager Instance
        {
            get { return Current.Game == null ? null : Current.Game.GetComponent<TMM_InstituteRelayManager>(); }
        }

        public void RegisterAbduction(InstituteAbductionTracker tracker)
        {
            if (tracker != null) abductions.Add(tracker);
        }

        public void QueueRetreatExtraction(Pawn pawn, int delayTicks)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Destroyed || pawn.IsPrisoner || !pawn.Spawned) return;
            if (!InstituteRelayFx.FactionHasRelayTeleport(pawn.Faction)) return;

            for (int i = 0; i < pendingRetreats.Count; i++)
            {
                if (pendingRetreats[i] != null && pendingRetreats[i].pawn == pawn) return;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            pendingRetreats.Add(new PendingRetreatExtraction
            {
                pawn = pawn,
                faction = pawn.Faction,
                map = pawn.Map,
                triggerTick = now + Math.Max(0, delayTicks)
            });
        }

        /// <summary>
        /// Starts a world-map relay jump for a group of Institute pawns from originTile to destinationTile.
        /// </summary>
        public PendingWorldRelayJump StartWorldRelayJump(
            Faction faction,
            List<Pawn> pawns,
            PlanetTile originTile,
            PlanetTile destinationTile,
            bool spawnCaravanAtDestination)
        {
            if (faction == null || pawns == null || pawns.Count == 0) return null;
            if (!originTile.Valid || !destinationTile.Valid) return null;

            FactionTeleportExtension ext = InstituteRelayFx.GetTeleportExtension(faction);
            int delay = ext != null && ext.teleportDelayTicks > 0 ? ext.teleportDelayTicks : 2500;
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            PendingWorldRelayJump jump = new PendingWorldRelayJump();
            jump.faction = faction;
            jump.originTile = originTile;
            jump.destinationTile = destinationTile;
            jump.arriveTick = now + delay;
            jump.spawnCaravanAtDestination = spawnCaravanAtDestination;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Destroyed || p.Dead) continue;
                if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns != null && !Find.WorldPawns.Contains(p))
                {
                    Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                }
                jump.pawns.Add(p);
            }

            if (jump.pawns.Count == 0) return null;

            worldJumps.Add(jump);

            // Spawn optional visual marker on world map if WorldObjectDef is registered
            WorldObjectDef markerDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail("TMM_WorldRelayJumpMarker");
            if (markerDef != null && Find.WorldObjects != null)
            {
                WorldObject_TMMRelayJump marker = WorldObjectMaker.MakeWorldObject(markerDef) as WorldObject_TMMRelayJump;
                if (marker != null)
                {
                    marker.Tile = originTile;
                    marker.SetFaction(faction);
                    marker.destinationTile = destinationTile;
                    marker.arriveTick = jump.arriveTick;
                    Find.WorldObjects.Add(marker);
                }
            }

            return jump;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null) return;
            int now = Find.TickManager.TicksGame;
            if (now % CheckInterval != 0) return;

            ScanForRetreatingInstitutePawns(now);
            TickPendingRetreats(now);
            TickAbductions(now);
            TickWorldJumps(now);
        }

        private void ScanForRetreatingInstitutePawns(int now)
        {
            if (Find.Maps == null) return;
            for (int m = 0; m < Find.Maps.Count; m++)
            {
                Map map = Find.Maps[m];
                if (map == null || map.mapPawns == null) continue;
                IReadOnlyList<Pawn> all = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < all.Count; i++)
                {
                    Pawn p = all[i];
                    if (p == null || p.Dead || p.Downed || p.Destroyed || p.IsPrisoner) continue;
                    if (!InstituteRelayFx.FactionHasRelayTeleport(p.Faction)) continue;

                    if (IsPawnRetreating(p))
                    {
                        QueueRetreatExtraction(p, RetreatChargeDelayTicks);
                    }
                }
            }
        }

        public static bool IsPawnRetreating(Pawn p)
        {
            if (p == null || p.Dead || p.Downed || p.IsPrisoner || !p.Spawned) return false;
            if (p.MentalStateDef == MentalStateDefOf.PanicFlee) return true;
            if (p.CurJob != null && p.CurJob.exitMapOnArrival) return true;

            Lord lord = p.GetLord();
            if (lord != null && lord.CurLordToil != null)
            {
                if (lord.CurLordToil is LordToil_PanicFlee) return true;
                string toilName = lord.CurLordToil.GetType().Name;
                if (toilName.IndexOf("ExitMap", StringComparison.OrdinalIgnoreCase) >= 0
                    || toilName.IndexOf("PanicFlee", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void TickPendingRetreats(int now)
        {
            for (int i = pendingRetreats.Count - 1; i >= 0; i--)
            {
                PendingRetreatExtraction item = pendingRetreats[i];
                if (item == null || item.pawn == null || item.pawn.Destroyed || item.pawn.Dead
                    || item.pawn.Downed || item.pawn.IsPrisoner || !item.pawn.Spawned)
                {
                    pendingRetreats.RemoveAt(i);
                    continue;
                }

                if (now < item.triggerTick) continue;

                // Confirm pawn is still retreating at trigger time
                if (IsPawnRetreating(item.pawn))
                {
                    if (InstituteRelayFx.TeleportNpcOffMap(item.pawn, item.faction))
                    {
                        totalRetreatsTeleported++;
                    }
                }
                pendingRetreats.RemoveAt(i);
            }
        }

        private void TickAbductions(int now)
        {
            for (int i = abductions.Count - 1; i >= 0; i--)
            {
                InstituteAbductionTracker ab = abductions[i];
                if (ab == null || ab.completed || ab.map == null || !Find.Maps.Contains(ab.map))
                {
                    abductions.RemoveAt(i);
                    continue;
                }

                int aliveSquad = 0;
                for (int s = 0; s < ab.squad.Count; s++)
                {
                    Pawn m = ab.squad[s];
                    if (m != null && !m.Dead && !m.Downed && !m.Destroyed && m.Spawned && m.Map == ab.map)
                    {
                        aliveSquad++;
                    }
                }

                if (aliveSquad == 0)
                {
                    ab.completed = true;
                    abductions.RemoveAt(i);
                    continue;
                }

                // Check if any squad member has picked up a colonist
                Pawn activeCaptor = FindCarryingCaptor(ab);
                if (activeCaptor != null)
                {
                    if (ab.captor != activeCaptor || ab.extractionTick < 0)
                    {
                        ab.captor = activeCaptor;
                        ab.extractionTick = now + AbductionExtractDelayTicks;
                    }
                    else if (now >= ab.extractionTick)
                    {
                        // Relay-extract captor (with kidnapped victim) and all surviving squad members!
                        if (!InstituteRelayFx.TeleportNpcOffMap(activeCaptor, ab.faction))
                        {
                            ab.captor = null;
                            ab.extractionTick = -1;
                            continue;
                        }
                        for (int s = 0; s < ab.squad.Count; s++)
                        {
                            Pawn mate = ab.squad[s];
                            if (mate != null && mate != activeCaptor && !mate.Dead && !mate.Downed && mate.Spawned)
                            {
                                InstituteRelayFx.TeleportNpcOffMap(mate, ab.faction);
                            }
                        }
                        ab.completed = true;
                        abductions.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // Victim dropped or rescued — reset countdown and try to grab any downed colonist
                    ab.captor = null;
                    ab.extractionTick = -1;
                    TryOrderSquadKidnapDowned(ab);
                }
            }
        }

        private static Pawn FindCarryingCaptor(InstituteAbductionTracker ab)
        {
            for (int i = 0; i < ab.squad.Count; i++)
            {
                Pawn m = ab.squad[i];
                if (m == null || m.Dead || m.Downed || !m.Spawned) continue;
                if (m.carryTracker != null && m.carryTracker.CarriedThing is Pawn)
                {
                    Pawn carried = (Pawn)m.carryTracker.CarriedThing;
                    if (carried.Faction == Faction.OfPlayer)
                    {
                        return m;
                    }
                }
            }
            return null;
        }

        private static void TryOrderSquadKidnapDowned(InstituteAbductionTracker ab)
        {
            if (ab.map == null || ab.map.mapPawns == null) return;

            Pawn downedVictim = null;
            if (ab.targetVictim != null && ab.targetVictim.Spawned && ab.targetVictim.Map == ab.map
                && !ab.targetVictim.Dead && ab.targetVictim.Downed)
            {
                downedVictim = ab.targetVictim;
            }
            else
            {
                List<Pawn> cols = ab.map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < cols.Count; i++)
                {
                    Pawn c = cols[i];
                    if (c != null && !c.Dead && c.Downed)
                    {
                        downedVictim = c;
                        break;
                    }
                }
            }

            if (downedVictim == null) return;

            for (int i = 0; i < ab.squad.Count; i++)
            {
                Pawn m = ab.squad[i];
                if (m == null || m.Dead || m.Downed || !m.Spawned || m.jobs == null) continue;
                if (m.CurJobDef == JobDefOf.Kidnap) return;

                IntVec3 exitSpot;
                if (!RCellFinder.TryFindBestExitSpot(m, out exitSpot)) exitSpot = m.Position;
                if (m.CanReserveAndReach(downedVictim, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    Job kidnap = JobMaker.MakeJob(JobDefOf.Kidnap, downedVictim, exitSpot); // CHECK API
                    kidnap.count = 1;
                    m.jobs.TryTakeOrderedJob(kidnap, JobTag.Misc);
                    return;
                }
            }
        }

        private void TickWorldJumps(int now)
        {
            for (int i = worldJumps.Count - 1; i >= 0; i--)
            {
                PendingWorldRelayJump jump = worldJumps[i];
                if (jump == null || jump.completed)
                {
                    worldJumps.RemoveAt(i);
                    continue;
                }

                if (now < jump.arriveTick) continue;

                // Remove any visual marker for this jump
                if (Find.WorldObjects != null)
                {
                    List<WorldObject> all = new List<WorldObject>(Find.WorldObjects.AllWorldObjects);
                    for (int w = 0; w < all.Count; w++)
                    {
                        WorldObject_TMMRelayJump marker = all[w] as WorldObject_TMMRelayJump;
                        if (marker != null && marker.destinationTile == jump.destinationTile && marker.arriveTick == jump.arriveTick)
                        {
                            Find.WorldObjects.Remove(marker);
                        }
                    }
                }

                List<Pawn> alive = new List<Pawn>();
                for (int p = 0; p < jump.pawns.Count; p++)
                {
                    if (jump.pawns[p] != null && !jump.pawns[p].Destroyed && !jump.pawns[p].Dead)
                    {
                        alive.Add(jump.pawns[p]);
                    }
                }

                if (jump.spawnCaravanAtDestination && alive.Count > 0 && jump.destinationTile.Valid && jump.faction != null)
                {
                    CaravanMaker.MakeCaravan(alive, jump.faction, jump.destinationTile, true); // CHECK API
                }

                totalWorldJumpsCompleted++;
                jump.completed = true;
                worldJumps.RemoveAt(i);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref relayTechUnlocked, "TMM_relayTechUnlocked", false);
            Scribe_Collections.Look(ref abductions, "TMM_instituteAbductions", LookMode.Deep);
            Scribe_Collections.Look(ref pendingRetreats, "TMM_pendingRetreats", LookMode.Deep);
            Scribe_Collections.Look(ref worldJumps, "TMM_pendingWorldRelayJumps", LookMode.Deep);
            Scribe_Values.Look(ref totalRetreatsTeleported, "TMM_totalRetreatsTeleported", 0);
            Scribe_Values.Look(ref totalWorldJumpsCompleted, "TMM_totalWorldJumpsCompleted", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (abductions == null) abductions = new List<InstituteAbductionTracker>();
                if (pendingRetreats == null) pendingRetreats = new List<PendingRetreatExtraction>();
                if (worldJumps == null) worldJumps = new List<PendingWorldRelayJump>();
            }
        }
    }

    // =========================================================================
    // 5. IncidentWorkers: Relay Raid & Relay Abduction
    // =========================================================================

    /// <summary>
    /// Institute Relay Raid: synth squad teleports directly into or near the colony (15..35 cells
    /// from nearest colonist, outside player houses) with blue flash & sound, 0.85x points, earliest day 45.
    /// Falls back cleanly to edge walk-in if no valid relay cell is found.
    /// </summary>
    public sealed class IncidentWorker_TMMInstituteRelayRaid : IncidentWorker
    {
        public const int EarliestNaturalDay = 45;
        public const float PointsMultiplier = 0.85f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || !map.IsPlayerHome) return false;
            if (!parms.forced && GenDate.DaysPassed < EarliestNaturalDay) return false;

            Faction inst = InstituteRelayFx.FindInstituteFaction();
            if (inst == null || !inst.HostileTo(Faction.OfPlayer)) return false;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            return ExecuteRelayRaid(parms) != null;
        }

        public static List<Pawn> ExecuteRelayRaid(IncidentParms parms)
        {
            Map map = parms != null ? parms.target as Map : null;
            Faction inst = InstituteRelayFx.FindInstituteFaction();
            if (map == null || inst == null || !inst.HostileTo(Faction.OfPlayer)) return null;

            float basePoints = parms.points > 100f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            float adjustedPoints = Math.Max(180f, basePoints * PointsMultiplier);

            IntVec3 dropCenter = InstituteRelayFx.FindRelayDropCell(map, 15, 35, null);
            bool usedRelay = dropCenter.IsValid;

            if (!usedRelay)
            {
                // Safe fallback to vanilla edge walk-in without exceptions
                IncidentParms fallback = new IncidentParms();
                fallback.target = map;
                fallback.faction = inst;
                fallback.points = adjustedPoints;
                fallback.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                fallback.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                fallback.forced = true;
                IncidentDefOf.RaidEnemy.Worker.TryExecute(fallback);
                return new List<Pawn>();
            }

            List<Pawn> squad = GenerateInstituteRaidPawns(inst, map, adjustedPoints);
            if (squad.Count == 0) return null;

            for (int i = 0; i < squad.Count; i++)
            {
                Pawn p = squad[i];
                IntVec3 cell = InstituteRelayFx.FindNearbyStandableCell(dropCenter, map, 4);
                GenSpawn.Spawn(p, cell, map);
                InstituteRelayFx.SpawnRelayFlash(cell, map, inst, 1.8f);
            }

            LordMaker.MakeNewLord(
                inst,
                new LordJob_AssaultColony(inst, true, true, false, false, true),
                map,
                squad);

            if (parms.sendLetter && Find.LetterStack != null)
            {
                Find.LetterStack.ReceiveLetter(
                    "TMM.Relay.RaidTitle".Translate(),
                    "TMM.Relay.RaidText".Translate(inst.Name, squad.Count),
                    LetterDefOf.ThreatBig,
                    new LookTargets(dropCenter, map));
            }

            return squad;
        }

        private static List<Pawn> GenerateInstituteRaidPawns(Faction inst, Map map, float points)
        {
            List<Pawn> result = new List<Pawn>();
            try
            {
                PawnGroupMakerParms gp = new PawnGroupMakerParms();
                gp.groupKind = PawnGroupKindDefOf.Combat;
                gp.tile = map.Tile;
                gp.faction = inst;
                gp.points = points;
                gp.inhabitants = false;

                IEnumerable<Pawn> generated = PawnGroupMakerUtility.GeneratePawns(gp, false);
                if (generated != null)
                {
                    foreach (Pawn p in generated)
                    {
                        if (p != null && !p.Dead && !p.Downed) result.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] GenerateInstituteRaidPawns fallback: " + ex.Message);
            }

            int minSquad = Math.Max(3, Mathf.RoundToInt(points / 220f));
            while (result.Count < minSquad)
            {
                string xeno = (result.Count == 0) ? "TMM_SynthGen3" : (Rand.Bool ? "TMM_SynthGen2" : "TMM_SynthGen1");
                Pawn p = InstituteRelayFx.GenerateSynthPawn(inst, map, xeno);
                if (p == null) break;
                result.Add(p);
            }

            return result;
        }
    }

    /// <summary>
    /// Institute Relay Abduction: 2-4 Gen2 synths + 1 Gen3 Courser relay in to abduct a downed, sleeping,
    /// or isolated colonist and relay back out ~1500 ticks after grabbing them.
    /// </summary>
    public sealed class IncidentWorker_TMMInstituteRelayAbduction : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || !map.IsPlayerHome || map.mapPawns.FreeColonistsSpawnedCount == 0) return false;

            Faction inst = InstituteRelayFx.FindInstituteFaction();
            if (inst == null || !inst.HostileTo(Faction.OfPlayer)) return false;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            return ExecuteRelayAbduction(parms) != null;
        }

        public static InstituteAbductionTracker ExecuteRelayAbduction(IncidentParms parms)
        {
            Map map = parms != null ? parms.target as Map : null;
            Faction inst = InstituteRelayFx.FindInstituteFaction();
            if (map == null || inst == null || !inst.HostileTo(Faction.OfPlayer)) return null;

            Pawn target = PickVulnerableColonist(map);
            if (target == null) return null;

            IntVec3 dropCenter = InstituteRelayFx.FindRelayDropCell(map, 15, 35, target);
            if (!dropCenter.IsValid)
            {
                IntVec3 edge;
                if (!RCellFinder.TryFindRandomPawnEntryCell(out edge, map, CellFinder.EdgeRoadChance_Hostile))
                {
                    return null;
                }
                dropCenter = edge;
            }

            List<Pawn> squad = new List<Pawn>();
            // 1 Gen3 Courser leader
            Pawn courser = InstituteRelayFx.GenerateSynthPawn(inst, map, "TMM_SynthGen3");
            if (courser != null) squad.Add(courser);

            // 2..4 Gen2 synths
            int gen2Count = Rand.RangeInclusive(2, 4);
            for (int i = 0; i < gen2Count; i++)
            {
                Pawn g2 = InstituteRelayFx.GenerateSynthPawn(inst, map, "TMM_SynthGen2");
                if (g2 != null) squad.Add(g2);
            }

            if (squad.Count == 0) return null;

            for (int i = 0; i < squad.Count; i++)
            {
                IntVec3 cell = InstituteRelayFx.FindNearbyStandableCell(dropCenter, map, 3);
                GenSpawn.Spawn(squad[i], cell, map);
                InstituteRelayFx.SpawnRelayFlash(cell, map, inst, 1.7f);
            }

            LordMaker.MakeNewLord(
                inst,
                new LordJob_AssaultColony(inst, true, true, false, false, true),
                map,
                squad);

            InstituteAbductionTracker tracker = new InstituteAbductionTracker();
            tracker.map = map;
            tracker.faction = inst;
            tracker.squad = squad;
            tracker.targetVictim = target;
            tracker.startTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            TMM_InstituteRelayManager mgr = TMM_InstituteRelayManager.Instance;
            if (mgr != null)
            {
                mgr.RegisterAbduction(tracker);
            }

            if (parms.sendLetter && Find.LetterStack != null)
            {
                Find.LetterStack.ReceiveLetter(
                    "TMM.Relay.AbductionTitle".Translate(),
                    "TMM.Relay.AbductionText".Translate(inst.Name, target.LabelShort),
                    LetterDefOf.ThreatBig,
                    new LookTargets(dropCenter, map));
            }

            return tracker;
        }

        private static Pawn PickVulnerableColonist(Map map)
        {
            List<Pawn> cols = map.mapPawns.FreeColonistsSpawned;
            if (cols == null || cols.Count == 0) return null;

            Pawn best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < cols.Count; i++)
            {
                Pawn p = cols[i];
                if (p == null || p.Dead) continue;

                float score = 0f;
                if (p.Downed) score += 100f;
                if (!p.Awake()) score += 45f;

                // Isolation bonus: distance to nearest other awake colonist
                float nearestAlly = 40f;
                for (int j = 0; j < cols.Count; j++)
                {
                    if (i == j) continue;
                    Pawn other = cols[j];
                    if (other == null || other.Dead || other.Downed) continue;
                    float d = p.Position.DistanceTo(other.Position);
                    if (d < nearestAlly) nearestAlly = d;
                }
                score += nearestAlly;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
            return best;
        }
    }

    // =========================================================================
    // 6. Harmony Patches: Retreat Teleport & WorldTraffic Jump Interception
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class InstituteRelayHarmonyPatches
    {
        static InstituteRelayHarmonyPatches()
        {
            var harmony = new Harmony("turbomegamod.institute.relay");

            // 1. Intercept Lord.GotoToil to immediately flag retreating Institute pawns for relay extraction
            MethodInfo gotoToil = AccessTools.Method(typeof(Lord), "GotoToil", new Type[] { typeof(LordToil) }); // CHECK API
            if (gotoToil != null)
            {
                harmony.Patch(gotoToil, null, new HarmonyMethod(typeof(InstituteRelayHarmonyPatches), "Lord_GotoToil_Postfix"));
            }

            // 2. World jumps of Institute traffic caravans are handled natively in WorldTraffic.ProcessRoutes (route stays intact).

            // 3. Hide TMM_RelayTechnology until unlocked by quest (TMMRelayUnlock.Unlock())
            MethodInfo canStartNow = AccessTools.PropertyGetter(typeof(ResearchProjectDef), "CanStartNow"); // CHECK API
            if (canStartNow != null)
            {
                harmony.Patch(canStartNow, null, new HarmonyMethod(typeof(InstituteRelayHarmonyPatches), "ResearchProject_CanStartNow_Postfix"));
            }
        }

        private static void Lord_GotoToil_Postfix(Lord __instance, LordToil newLordToil)
        {
            if (__instance == null || newLordToil == null || __instance.faction == null) return;
            if (!InstituteRelayFx.FactionHasRelayTeleport(__instance.faction)) return;

            bool isRetreat = newLordToil is LordToil_PanicFlee
                || newLordToil.GetType().Name.IndexOf("ExitMap", StringComparison.OrdinalIgnoreCase) >= 0
                || newLordToil.GetType().Name.IndexOf("PanicFlee", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isRetreat) return;

            TMM_InstituteRelayManager mgr = TMM_InstituteRelayManager.Instance;
            if (mgr == null || __instance.ownedPawns == null) return;

            for (int i = 0; i < __instance.ownedPawns.Count; i++)
            {
                Pawn p = __instance.ownedPawns[i];
                if (p != null && !p.Dead && !p.Downed && !p.IsPrisoner && p.Spawned)
                {
                    mgr.QueueRetreatExtraction(p, TMM_InstituteRelayManager.RetreatChargeDelayTicks);
                }
            }
        }

        /// <summary>
        /// Intercepts WorldTraffic routes for factions with FactionTeleportExtension.allowWorldJumps == true:
        /// replaces ground walking between settlements with an instant disappear -> delay -> appear world jump.
        /// // CHECK integration WorldTraffic
        /// </summary>
        private static void WorldTraffic_TryStart_Postfix(Settlement origin, Settlement destination, object __result)
        {
            if (__result == null || origin == null || destination == null) return;
            Faction fac = origin.Faction;
            FactionTeleportExtension ext = InstituteRelayFx.GetTeleportExtension(fac);
            if (ext == null || !ext.allowWorldJumps) return;

            try
            {
                FieldInfo caravanField = AccessTools.Field(__result.GetType(), "caravan"); // CHECK integration WorldTraffic
                Caravan caravan = caravanField != null ? caravanField.GetValue(__result) as Caravan : null;
                if (caravan == null || caravan.Destroyed) return;

                List<Pawn> members = new List<Pawn>(caravan.PawnsListForReading);
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i] != null && caravan.ContainsPawn(members[i]))
                    {
                        caravan.RemovePawn(members[i]);
                    }
                }

                if (caravan.Spawned && Find.WorldObjects != null)
                {
                    Find.WorldObjects.Remove(caravan);
                }

                TMM_InstituteRelayManager mgr = TMM_InstituteRelayManager.Instance;
                if (mgr != null && members.Count > 0)
                {
                    mgr.StartWorldRelayJump(fac, members, origin.Tile, destination.Tile, false);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] WorldTraffic_TryStart_Postfix relay intercept failed: " + ex.Message);
            }
        }

        private static void ResearchProject_CanStartNow_Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            TMM_HiddenResearchExtension ext = __instance.GetModExtension<TMM_HiddenResearchExtension>();
            if (ext != null && ext.hiddenUntilUnlocked && !TMMRelayUnlock.IsUnlocked)
            {
                __result = false;
            }
        }
    }
}
