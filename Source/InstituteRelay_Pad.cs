using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Marks a ResearchProjectDef as locked behind a story unlock (e.g. TMM_RelayTechnology -> TMMRelayUnlock.Unlock()).
    /// </summary>
    public sealed class TMM_HiddenResearchExtension : DefModExtension
    {
        public bool hiddenUntilUnlocked = true;
    }

    /// <summary>
    /// Player-buildable 3x3 Institute Relay Pad (TMM_RelayPad).
    /// Supports:
    ///   - Local relay jump to another powered TMM_RelayPad on the same map (2h cooldown = 5000 ticks).
    ///   - World map relay jump within 60 tiles (1d cooldown = 60000 ticks, atomic cost: 1x ComponentSpacer).
    ///   - Idle power draw 150W, peak power draw 1200W during active jump pulse.
    ///   - Dual visual state: TMM_RelayPad (idle/unpowered) vs TMM_RelayPad_Charged (powered & ready/glowing).
    /// </summary>
    public sealed class Building_TMMRelayPad : Building
    {
        public const float IdlePowerWatts = 150f;
        public const float PeakPowerWatts = 1200f;
        public const int PeakPowerDurationTicks = 250;
        public const int LocalCooldownTicks = 5000;   // 2 game hours
        public const int WorldCooldownTicks = 60000;  // 1 game day
        public const int MaxWorldJumpTiles = 60;
        public const int WorldJumpSpacerComponentCost = 1;

        private const string ChargedTexPath = "Things/Building/TMM/TMM_RelayPad_Charged";
        private const string LocalGizmoTexPath = "UI/Commands/TMM_Gizmo_RelayLocal";
        private const string WorldGizmoTexPath = "UI/Commands/TMM_Gizmo_RelayWorld";

        private static Graphic cachedChargedGraphic;
        private static Texture2D cachedLocalIcon;
        private static Texture2D cachedWorldIcon;

        public int localCooldownUntilTick;
        public int worldCooldownUntilTick;
        public int peakPowerUntilTick;

        private CompPowerTrader powerComp;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            UpdatePowerConsumption();
        }

        public bool PowerOn
        {
            get
            {
                if (powerComp == null) powerComp = GetComp<CompPowerTrader>();
                return powerComp != null && powerComp.PowerOn;
            }
        }

        public bool IsPeakPowerActive
        {
            get
            {
                return Find.TickManager != null && Find.TickManager.TicksGame < peakPowerUntilTick;
            }
        }

        public int LocalCooldownRemaining
        {
            get
            {
                if (Find.TickManager == null) return 0;
                return Math.Max(0, localCooldownUntilTick - Find.TickManager.TicksGame);
            }
        }

        public int WorldCooldownRemaining
        {
            get
            {
                if (Find.TickManager == null) return 0;
                return Math.Max(0, worldCooldownUntilTick - Find.TickManager.TicksGame);
            }
        }

        public bool IsChargedVisual
        {
            get
            {
                return PowerOn && (IsPeakPowerActive || LocalCooldownRemaining == 0 || WorldCooldownRemaining == 0);
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (IsChargedVisual)
                {
                    Graphic charged = GetChargedGraphic();
                    if (charged != null) return charged;
                }
                return base.Graphic;
            }
        }

        private Graphic GetChargedGraphic()
        {
            if (cachedChargedGraphic == null && def != null && def.graphicData != null)
            {
                try
                {
                    if (ContentFinder<Texture2D>.Get(ChargedTexPath, false) != null)
                    {
                        cachedChargedGraphic = GraphicDatabase.Get<Graphic_Single>(
                            ChargedTexPath,
                            ShaderDatabase.Cutout,
                            def.graphicData.drawSize,
                            Color.white);
                    }
                }
                catch
                {
                    cachedChargedGraphic = null;
                }
            }
            return cachedChargedGraphic;
        }

        private static Texture2D GetLocalIcon()
        {
            if (cachedLocalIcon == null)
            {
                cachedLocalIcon = ContentFinder<Texture2D>.Get(LocalGizmoTexPath, false);
                if (cachedLocalIcon == null) cachedLocalIcon = TexCommand.Draft;
            }
            return cachedLocalIcon;
        }

        private static Texture2D GetWorldIcon()
        {
            if (cachedWorldIcon == null)
            {
                cachedWorldIcon = ContentFinder<Texture2D>.Get(WorldGizmoTexPath, false);
                if (cachedWorldIcon == null) cachedWorldIcon = TexCommand.Draft;
            }
            return cachedWorldIcon;
        }

        protected override void Tick()
        {
            base.Tick();
            if (Find.TickManager != null && Find.TickManager.TicksGame % 30 == 0)
            {
                UpdatePowerConsumption();
            }
        }

        private void UpdatePowerConsumption()
        {
            if (powerComp == null) powerComp = GetComp<CompPowerTrader>();
            if (powerComp == null) return;
            float targetWatts = IsPeakPowerActive ? PeakPowerWatts : IdlePowerWatts;
            powerComp.PowerOutput = -targetWatts;
        }

        private void TriggerPeakPowerPulse()
        {
            if (Find.TickManager == null) return;
            peakPowerUntilTick = Find.TickManager.TicksGame + PeakPowerDurationTicks;
            UpdatePowerConsumption();
        }

        /// <summary>
        /// Collects all alive, non-destroyed player pawns currently standing on this 3x3 relay pad.
        /// </summary>
        public List<Pawn> GetPawnsOnPad()
        {
            List<Pawn> result = new List<Pawn>();
            if (!Spawned || Map == null) return result;

            CellRect rect = this.OccupiedRect();
            foreach (IntVec3 c in rect)
            {
                if (!c.InBounds(Map)) continue;
                List<Thing> things = Map.thingGrid.ThingsListAtFast(c);
                for (int i = 0; i < things.Count; i++)
                {
                    Pawn p = things[i] as Pawn;
                    if (p != null && !p.Dead && !p.Destroyed && p.Spawned && p.Faction == Faction.OfPlayer && !result.Contains(p))
                    {
                        result.Add(p);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Collects all haulable items resting on this 3x3 relay pad (for world jumps).
        /// </summary>
        public List<Thing> GetItemsOnPad()
        {
            List<Thing> result = new List<Thing>();
            if (!Spawned || Map == null) return result;

            CellRect rect = this.OccupiedRect();
            foreach (IntVec3 c in rect)
            {
                if (!c.InBounds(Map)) continue;
                List<Thing> things = Map.thingGrid.ThingsListAtFast(c);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t == null || t.Destroyed || !t.Spawned || t == this) continue;
                    if (t.def != null && t.def.category == ThingCategory.Item && !result.Contains(t))
                    {
                        result.Add(t);
                    }
                }
            }
            return result;
        }

        public List<Building_TMMRelayPad> FindOtherPoweredPadsOnMap()
        {
            List<Building_TMMRelayPad> result = new List<Building_TMMRelayPad>();
            if (!Spawned || Map == null || Map.listerBuildings == null) return result;

            List<Building> list = Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < list.Count; i++)
            {
                Building_TMMRelayPad other = list[i] as Building_TMMRelayPad;
                if (other != null && other != this && other.Spawned && !other.Destroyed && other.PowerOn)
                {
                    result.Add(other);
                }
            }
            return result;
        }

        /// <summary>
        /// Counts available non-forbidden ComponentSpacer on this pad or anywhere on the colony map.
        /// </summary>
        public int CountAvailableSpacerComponents()
        {
            if (!Spawned || Map == null || Map.listerThings == null) return 0;
            int total = 0;
            List<Thing> stacks = Map.listerThings.ThingsOfDef(ThingDefOf.ComponentSpacer);
            for (int i = 0; i < stacks.Count; i++)
            {
                Thing t = stacks[i];
                if (t != null && !t.Destroyed && t.Spawned && !t.IsForbidden(Faction.OfPlayer))
                {
                    total += t.stackCount;
                }
            }
            return total;
        }

        private Thing FindWorldJumpComponent()
        {
            Thing component = GetItemsOnPad().Find(IsUsableWorldJumpComponent);
            return component ?? Map.listerThings.ThingsOfDef(ThingDefOf.ComponentSpacer).Find(IsUsableWorldJumpComponent);
        }

        private static bool IsUsableWorldJumpComponent(Thing thing)
        {
            return thing != null && thing.Spawned && !thing.Destroyed && thing.def == ThingDefOf.ComponentSpacer &&
                thing.stackCount >= WorldJumpSpacerComponentCost && !thing.IsForbidden(Faction.OfPlayer);
        }

        /// <summary>
        /// Executes an atomic local jump of all player pawns on this pad to destPad on the same map.
        /// </summary>
        public bool TryLocalJump(Building_TMMRelayPad destPad, out string failReason)
        {
            failReason = null;
            if (!Spawned || Map == null)
            {
                failReason = "TMM.Relay.FailNotSpawned".Translate();
                return false;
            }
            if (!PowerOn)
            {
                failReason = "TMM.Relay.FailNoPower".Translate();
                return false;
            }
            if (LocalCooldownRemaining > 0)
            {
                failReason = "TMM.Relay.FailCooldown".Translate(LocalCooldownRemaining.ToStringTicksToPeriod());
                return false;
            }
            if (destPad == null || destPad == this || !destPad.Spawned || destPad.Destroyed || destPad.Map != Map)
            {
                failReason = "TMM.Relay.FailNoDestPad".Translate();
                return false;
            }
            if (!destPad.PowerOn)
            {
                failReason = "TMM.Relay.FailDestNoPower".Translate();
                return false;
            }

            List<Pawn> pawns = GetPawnsOnPad();
            if (pawns.Count == 0)
            {
                failReason = "TMM.Relay.FailNoPawns".Translate();
                return false;
            }

            // Collect standable destination cells on the target 3x3 pad
            List<IntVec3> destCells = new List<IntVec3>();
            foreach (IntVec3 c in destPad.OccupiedRect())
            {
                if (c.InBounds(Map) && c.Standable(Map))
                {
                    destCells.Add(c);
                }
            }
            if (destCells.Count == 0)
            {
                failReason = "TMM.Relay.FailDestBlocked".Translate();
                return false;
            }

            // All checks passed -> perform instant local relay transfer
            InstituteRelayFx.SpawnRelayFlash(Position, Map, null, 2.2f);
            InstituteRelayFx.SpawnRelayFlash(destPad.Position, Map, null, 2.2f);

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || !p.Spawned) continue;

                IntVec3 targetCell = destCells[i % destCells.Count];
                if (p.pather != null) p.pather.StopDead();
                p.Position = targetCell;
                p.Notify_Teleported(true, false); // CHECK API
                InstituteRelayFx.ApplyRelaySicknessIfSusceptible(p);
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            localCooldownUntilTick = now + LocalCooldownTicks;
            TriggerPeakPowerPulse();
            destPad.TriggerPeakPowerPulse();

            Messages.Message("TMM.Relay.LocalJumpSuccess".Translate(pawns.Count), destPad, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        /// <summary>
        /// Validates all preconditions for a world jump WITHOUT mutating any state or consuming resources.
        /// </summary>
        public bool CanWorldJumpTo(PlanetTile targetTile, out string failReason)
        {
            failReason = null;
            if (!Spawned || Map == null || Find.World == null || Find.WorldGrid == null)
            {
                failReason = "TMM.Relay.FailNotSpawned".Translate();
                return false;
            }
            if (!PowerOn)
            {
                failReason = "TMM.Relay.FailNoPower".Translate();
                return false;
            }
            if (WorldCooldownRemaining > 0)
            {
                failReason = "TMM.Relay.FailCooldown".Translate(WorldCooldownRemaining.ToStringTicksToPeriod());
                return false;
            }
            if (!targetTile.Valid || Find.World.Impassable(targetTile))
            {
                failReason = "TMM.Relay.FailTileImpassable".Translate();
                return false;
            }

            int dist = Find.WorldGrid.TraversalDistanceBetween(Map.Tile, targetTile, true, MaxWorldJumpTiles + 1); // CHECK API
            if (dist < 0 || dist > MaxWorldJumpTiles)
            {
                failReason = "TMM.Relay.FailOutOfRange".Translate(MaxWorldJumpTiles);
                return false;
            }

            List<Pawn> pawns = GetPawnsOnPad();
            if (pawns.Count == 0)
            {
                failReason = "TMM.Relay.FailNoPawns".Translate();
                return false;
            }

            bool hasFreeColonist = false;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].IsFreeColonist && !pawns[i].Downed)
                {
                    hasFreeColonist = true;
                    break;
                }
            }
            if (!hasFreeColonist)
            {
                failReason = "TMM.Relay.FailNeedColonist".Translate();
                return false;
            }

            if (CountAvailableSpacerComponents() < WorldJumpSpacerComponentCost)
            {
                failReason = "TMM.Relay.FailNoSpacerComponent".Translate(WorldJumpSpacerComponentCost);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Atomically executes a world-map relay jump: checks everything first, deducts 1 ComponentSpacer once,
        /// transfers pawns and pad items into a new player Caravan at targetTile, and sets 1-day cooldown.
        /// On any failure, zero resources are deducted and all pawns/items remain untouched.
        /// </summary>
        public static string TestWorldJumpFailure;

        public Caravan TryWorldJump(PlanetTile targetTile, out string failReason)
        {
            if (!CanWorldJumpTo(targetTile, out failReason))
            {
                Messages.Message(failReason, this, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            Map source = Map;
            List<Pawn> pawns = GetPawnsOnPad();
            Thing component = FindWorldJumpComponent();
            if (component == null)
            {
                failReason = "TMM.Relay.FailNoSpacerComponent".Translate(WorldJumpSpacerComponentCost);
                Messages.Message(failReason, this, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            var pawnPositions = new List<RelayJumpPosition>();
            var cargoPositions = new List<RelayJumpPosition>();
            RelayJumpPosition payment = new RelayJumpPosition(component);
            Thing reservedComponent = null;
            Caravan caravan = null;
            string fault = TestMode.Enabled ? TestWorldJumpFailure : null;
            try
            {
                reservedComponent = component.SplitOff(WorldJumpSpacerComponentCost);
                payment.Thing = reservedComponent;
                if (reservedComponent.Spawned) reservedComponent.DeSpawn(DestroyMode.Vanish);

                foreach (Thing item in GetItemsOnPad()) cargoPositions.Add(new RelayJumpPosition(item));
                foreach (Pawn pawn in pawns)
                {
                    pawnPositions.Add(new RelayJumpPosition(pawn));
                    pawn.DeSpawn(DestroyMode.Vanish);
                    if (fault == "pawn") throw new InvalidOperationException("Simulated relay departure failure.");
                }
                caravan = fault == "caravan" ? null : CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, targetTile, true);
                if (caravan == null)
                    throw new InvalidOperationException(fault == "caravan" ? "Simulated missing relay caravan." : "Relay caravan was not created.");
                foreach (Pawn pawn in pawns)
                    if (!caravan.ContainsPawn(pawn)) throw new InvalidOperationException("Relay caravan refused a passenger.");

                foreach (RelayJumpPosition entry in cargoPositions)
                {
                    Thing item = entry.Thing;
                    item.DeSpawn(DestroyMode.Vanish);
                    if (fault == "cargo") throw new InvalidOperationException("Simulated relay cargo refusal.");
                    if (!TryLoadRelayCargo(caravan, item)) throw new InvalidOperationException("Relay caravan refused cargo.");
                }

                try
                {
                    reservedComponent.Destroy(DestroyMode.Vanish);
                }
                catch (Exception ex)
                {
                    if (!reservedComponent.Destroyed) throw;
                    Log.Warning("[TMM] Relay cost was paid before its destruction handler failed: " + ex.Message);
                }
                if (!reservedComponent.Destroyed) throw new InvalidOperationException("Relay cost was not consumed.");
            }
            catch (Exception ex)
            {
                TestMode.ReportException("[TMM] Relay jump failed: ", ex);
                foreach (RelayJumpPosition entry in cargoPositions) RestoreRelayPosition(source, entry);
                if (reservedComponent != null) RestoreRelayPosition(source, payment);
                foreach (RelayJumpPosition entry in pawnPositions) RestoreRelayPosition(source, entry);
                if (caravan != null && caravan.Spawned && caravan.PawnsListForReading.Count == 0) caravan.Destroy();
                failReason = "TMM.Relay.FailCaravanCreation".Translate();
                Messages.Message(failReason, this, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            worldCooldownUntilTick = now + WorldCooldownTicks;
            TriggerPeakPowerPulse();

            foreach (Pawn pawn in pawns) InstituteRelayFx.ApplyRelaySicknessIfSusceptible(pawn);
            InstituteRelayFx.SpawnRelayFlash(Position, source, null, 2.8f);
            Messages.Message("TMM.Relay.WorldJumpSuccess".Translate(pawns.Count), caravan, MessageTypeDefOf.PositiveEvent);
            return caravan;
        }

        private sealed class RelayJumpPosition
        {
            public Thing Thing;
            public readonly IntVec3 Cell;
            public readonly Rot4 Rotation;

            public RelayJumpPosition(Thing thing)
            {
                Thing = thing;
                Cell = thing.Position;
                Rotation = thing.Rotation;
            }
        }

        private static bool TryLoadRelayCargo(Caravan caravan, Thing item)
        {
            Pawn preferred = CaravanInventoryUtility.FindPawnToMoveInventoryTo(item, caravan.PawnsListForReading, null, null);
            if (TryPutRelayCargo(preferred, item)) return true;
            foreach (Pawn pawn in caravan.PawnsListForReading)
                if (pawn != preferred && TryPutRelayCargo(pawn, item)) return true;
            return false;
        }

        private static bool TryPutRelayCargo(Pawn pawn, Thing item)
        {
            ThingOwner owner = pawn == null || pawn.inventory == null ? null : pawn.inventory.innerContainer;
            if (owner == null || item.holdingOwner != null) return false;
            owner.TryAdd(item, false);
            return item.holdingOwner == owner;
        }

        private static void RestoreRelayPosition(Map source, RelayJumpPosition entry)
        {
            Thing item = entry.Thing;
            if (item == null || item.Destroyed || (item.Spawned && item.Map == source)) return;
            Pawn pawn = item as Pawn;
            try
            {
                Caravan owner = pawn == null ? null : pawn.GetCaravan();
                if (owner != null && owner.ContainsPawn(pawn)) owner.RemovePawn(pawn);
                if (item.holdingOwner != null) item.holdingOwner.Remove(item);
                if (pawn != null)
                {
                    if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
                    GenSpawn.Spawn(pawn, entry.Cell, source, entry.Rotation);
                }
                else if (!GenPlace.TryPlaceThing(item, entry.Cell, source, ThingPlaceMode.Direct) &&
                    !GenPlace.TryPlaceThing(item, entry.Cell, source, ThingPlaceMode.Near))
                {
                    StoryPaymentLimbo.Hold(item);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] Relay rollback could not restore one thing to its cell: " + ex.Message);
                if (item.Destroyed || item.Spawned || item.holdingOwner != null) return;
                if (pawn != null) WorldTrafficCustody.Keep(pawn, null, true);
                else StoryPaymentLimbo.Hold(item);
            }
        }

        private void BeginWorldJumpTargeting()
        {
            if (Find.World == null || Find.WorldTargeter == null || Map == null) return;
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(this));

            Find.WorldTargeter.BeginTargeting(
                delegate(GlobalTargetInfo target)
                {
                    if (!target.IsValid || !target.Tile.Valid) return false;
                    string reason;
                    Caravan created = TryWorldJump(target.Tile, out reason);
                    return created != null;
                },
                true,
                GetWorldIcon(),
                true,
                delegate
                {
                    GenDraw.DrawWorldRadiusRing(Map.Tile, MaxWorldJumpTiles); // CHECK API
                },
                delegate(GlobalTargetInfo target)
                {
                    if (!target.IsValid || !target.Tile.Valid) return null;
                    string reason;
                    if (!CanWorldJumpTo(target.Tile, out reason)) return reason;
                    return "TMM.Relay.WorldTargetConfirm".Translate();
                }); // CHECK API
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            if (Faction != Faction.OfPlayer) yield break;

            // 1. Local relay jump gizmo
            Command_Action localCmd = new Command_Action();
            localCmd.defaultLabel = "TMM.Relay.GizmoLocalLabel".Translate();
            localCmd.defaultDesc = "TMM.Relay.GizmoLocalDesc".Translate();
            localCmd.icon = GetLocalIcon();
            localCmd.action = delegate
            {
                List<Building_TMMRelayPad> others = FindOtherPoweredPadsOnMap();
                if (others.Count == 0)
                {
                    Messages.Message("TMM.Relay.FailNoDestPad".Translate(), this, MessageTypeDefOf.RejectInput, false);
                    return;
                }
                if (others.Count == 1)
                {
                    string reason;
                    if (!TryLocalJump(others[0], out reason))
                    {
                        Messages.Message(reason, this, MessageTypeDefOf.RejectInput, false);
                    }
                    return;
                }

                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                for (int i = 0; i < others.Count; i++)
                {
                    Building_TMMRelayPad dest = others[i];
                    string label = "TMM.Relay.PickDestPad".Translate(dest.Position.x, dest.Position.z);
                    opts.Add(new FloatMenuOption(label, delegate
                    {
                        string reason;
                        if (!TryLocalJump(dest, out reason))
                        {
                            Messages.Message(reason, this, MessageTypeDefOf.RejectInput, false);
                        }
                    }));
                }
                if (Find.WindowStack != null) Find.WindowStack.Add(new FloatMenu(opts));
            };

            if (!PowerOn)
            {
                localCmd.Disable("TMM.Relay.FailNoPower".Translate());
            }
            else if (LocalCooldownRemaining > 0)
            {
                localCmd.Disable("TMM.Relay.FailCooldown".Translate(LocalCooldownRemaining.ToStringTicksToPeriod()));
            }
            else if (FindOtherPoweredPadsOnMap().Count == 0)
            {
                localCmd.Disable("TMM.Relay.FailNoDestPad".Translate());
            }
            else if (GetPawnsOnPad().Count == 0)
            {
                localCmd.Disable("TMM.Relay.FailNoPawns".Translate());
            }

            yield return localCmd;

            // 2. World map relay jump gizmo
            Command_Action worldCmd = new Command_Action();
            worldCmd.defaultLabel = "TMM.Relay.GizmoWorldLabel".Translate();
            worldCmd.defaultDesc = "TMM.Relay.GizmoWorldDesc".Translate(MaxWorldJumpTiles, WorldJumpSpacerComponentCost);
            worldCmd.icon = GetWorldIcon();
            worldCmd.action = delegate
            {
                BeginWorldJumpTargeting();
            };

            if (!PowerOn)
            {
                worldCmd.Disable("TMM.Relay.FailNoPower".Translate());
            }
            else if (WorldCooldownRemaining > 0)
            {
                worldCmd.Disable("TMM.Relay.FailCooldown".Translate(WorldCooldownRemaining.ToStringTicksToPeriod()));
            }
            else if (GetPawnsOnPad().Count == 0)
            {
                worldCmd.Disable("TMM.Relay.FailNoPawns".Translate());
            }
            else if (CountAvailableSpacerComponents() < WorldJumpSpacerComponentCost)
            {
                worldCmd.Disable("TMM.Relay.FailNoSpacerComponent".Translate(WorldJumpSpacerComponentCost));
            }

            yield return worldCmd;
        }

        public override string GetInspectString()
        {
            string baseText = base.GetInspectString();
            List<string> lines = new List<string>();
            if (!string.IsNullOrEmpty(baseText)) lines.Add(baseText);

            if (LocalCooldownRemaining > 0)
            {
                lines.Add("TMM.Relay.InspectLocalCooldown".Translate(LocalCooldownRemaining.ToStringTicksToPeriod()));
            }
            else
            {
                lines.Add("TMM.Relay.InspectLocalReady".Translate());
            }

            if (WorldCooldownRemaining > 0)
            {
                lines.Add("TMM.Relay.InspectWorldCooldown".Translate(WorldCooldownRemaining.ToStringTicksToPeriod()));
            }
            else
            {
                lines.Add("TMM.Relay.InspectWorldReady".Translate());
            }

            return string.Join("\n", lines.ToArray());
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref localCooldownUntilTick, "TMM_localCooldownUntilTick", 0);
            Scribe_Values.Look(ref worldCooldownUntilTick, "TMM_worldCooldownUntilTick", 0);
            Scribe_Values.Look(ref peakPowerUntilTick, "TMM_peakPowerUntilTick", 0);
        }
    }
}
