using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class FriendlySettlementVisits
    {
        static FriendlySettlementVisits()
        {
            var harmony = new Harmony("turbomegamod.friendly.settlement.visits");
            harmony.Patch(
                AccessTools.Method(typeof(Settlement), "GetCaravanGizmos"),
                postfix: new HarmonyMethod(typeof(FriendlySettlementVisits), "Gizmos"));
            harmony.Patch(AccessTools.Method(typeof(SettlementDefeatUtility), "IsDefeated"),
                prefix: new HarmonyMethod(typeof(FriendlySettlementVisits), "Defeat"));
            harmony.Patch(AccessTools.Method(typeof(TimedDetectionRaids), "StartDetectionCountdown"),
                prefix: new HarmonyMethod(typeof(FriendlySettlementVisits), "RaidTimer"));
            harmony.Patch(AccessTools.Method(typeof(TimedDetectionRaids), "CompTickInterval"),
                prefix: new HarmonyMethod(typeof(FriendlySettlementVisits), "RaidTimer"));
            harmony.Patch(AccessTools.PropertyGetter(typeof(TimedDetectionRaids), "NextRaidCountdownActiveAndVisible"),
                postfix: new HarmonyMethod(typeof(FriendlySettlementVisits), "RaidVisible"));
            SettlementPropertyPatches.Apply(harmony);
        }
        private static bool Peaceful(WorldObject obj)
        {
            return obj is Settlement && obj.Faction != null && !obj.Faction.IsPlayer && !obj.Faction.HostileTo(Faction.OfPlayer);
        }
        private static bool Defeat(Map map, Faction faction, ref bool __result)
        {
            // Native defeat checks active threats to the player. A living neutral garrison is not a threat,
            // but its absence of hostility is not a victory for visiting colonists.
            if (map != null && Peaceful(map.Parent) && map.ParentFaction == faction &&
                map.mapPawns.SpawnedPawnsInFaction(faction).Any(p => !p.Dead))
            { __result = false; return false; }
            return true;
        }
        private static bool RaidTimer(TimedDetectionRaids __instance) { return !Peaceful(__instance.parent); }
        private static void RaidVisible(TimedDetectionRaids __instance, ref bool __result)
        {
            if (Peaceful(__instance.parent)) __result = false;
        }
        public static bool CanVisit(Caravan caravan, Settlement settlement)
        {
            return caravan != null && caravan.Spawned && caravan.IsPlayerControlled && settlement != null && settlement.Spawned &&
                caravan.Tile == settlement.Tile && settlement.Faction != null && !settlement.Faction.IsPlayer &&
                !settlement.Faction.HostileTo(Faction.OfPlayer) && caravan.PawnsListForReading.Any(p => p.IsFreeColonist && !p.Downed && !p.Dead);
        }
        public static bool Enter(Caravan caravan, Settlement settlement)
        {
            if (!CanVisit(caravan, settlement)) return false;
            var map = GetOrGenerateMapUtility.GetOrGenerateMap(settlement.Tile, null);
            if (map == null || !CanVisit(caravan, settlement)) return false;
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, false);
            return true;
        }
        private static void Gizmos(Settlement __instance, Caravan caravan, ref IEnumerable<Gizmo> __result)
        {
            if (!CanVisit(caravan, __instance)) return;
            var settlement = __instance;
            var command = new Command_Action
            {
                defaultLabel = "TMM.World.EnterFriendly".Translate(),
                defaultDesc = "TMM.World.EnterFriendlyHelp".Translate(),
                icon = TexButton.Info,
                action = () => Enter(caravan, settlement)
            };
            __result = __result.Concat(new[] { command });
        }
    }
    /// What a settlement's map held when it was generated for a peaceful visit belongs to the settlement. Only that is
    /// known for certain, so only that is protected: the visitors' own things, things bought from a trader and everything
    /// that appeared later are not in the list. Maps visited before this existed have no list and stay as they were.
    public sealed class SettlementProperty : MapComponent
    {
        private static readonly HashSet<int> ids = new HashSet<int>();   // every protected thing of the running game
        private List<Thing> things = new List<Thing>();
        public SettlementProperty(Map map) : base(map) { }
        public static void ClearCache() { ids.Clear(); }
        public static bool Any { get { return ids.Count > 0; } }
        public static bool IsMarked(Thing thing) { return thing != null && ids.Count > 0 && ids.Contains(thing.thingIDNumber); }
        public int Count { get { things.RemoveAll(t => t == null || t.Destroyed); return things.Count; } }
        public IEnumerable<Thing> Things { get { return things.Where(t => t != null && !t.Destroyed); } }

        /// Protected from the player right now: marked, still on its settlement's map, and the settlement is not at war with the player.
        public static bool Protected(Thing thing)
        {
            if (!IsMarked(thing)) return false;
            Map map = thing.MapHeld;
            Faction owner = map == null || map.Parent == null ? null : map.Parent.Faction;
            return owner != null && !owner.IsPlayer && !owner.HostileTo(Faction.OfPlayer);
        }
        public static bool Blocks(Pawn pawn, Thing thing)
        {
            return ids.Count > 0 && pawn != null && pawn.Faction == Faction.OfPlayer && Protected(thing);
        }
        public void Mark(Thing thing)
        {
            if (thing == null || thing.Destroyed || !ids.Add(thing.thingIDNumber)) return;
            things.Add(thing);
        }
        public static void MarkLike(Thing source, Thing part)
        {
            Map map = source.MapHeld;
            SettlementProperty list = map == null ? null : map.GetComponent<SettlementProperty>();
            if (list != null) list.Mark(part);
        }
        public static void Release(Thing thing)
        {
            if (thing == null || !ids.Remove(thing.thingIDNumber) || Find.Maps == null) return;
            foreach (Map m in Find.Maps)
            {
                SettlementProperty list = m.GetComponent<SettlementProperty>();
                if (list != null) list.things.Remove(thing);
            }
        }
        public override void MapGenerated()
        {
            base.MapGenerated();
            var settlement = map.Parent as Settlement;
            if (settlement == null || settlement.Faction == null || settlement.Faction.IsPlayer || settlement.Faction.HostileTo(Faction.OfPlayer)) return;
            // Natural rock rubble is nobody's property (and is saved compressed, without an identity).
            foreach (Thing t in map.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item && !t.def.saveCompressible && t.Spawned && t.Faction != Faction.OfPlayer).ToList()) Mark(t);
        }
        public override void MapRemoved()
        {
            base.MapRemoved();
            foreach (Thing t in things) if (t != null) ids.Remove(t.thingIDNumber);
            things.Clear();
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            foreach (Thing t in things) if (t != null) ids.Add(t.thingIDNumber);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving) things.RemoveAll(t => t == null || t.Destroyed || t.MapHeld != map);
            Scribe_Collections.Look(ref things, "TMM_settlementProperty", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (things == null) things = new List<Thing>();
                things.RemoveAll(t => t == null);
            }
        }
    }

    public sealed class SettlementPropertyReset : GameComponent
    {
        public SettlementPropertyReset(Game game) { SettlementProperty.ClearCache(); }
    }

    /// The player's pawns cannot take protected things by ordinary orders or work: reservation is the common gate of haul, wear,
    /// equip, eat and load jobs; the load and caravan windows do not list them; carrying and vehicle cargo refuse them.
    public static class SettlementPropertyPatches
    {
        private static bool selling;
        public static void Apply(Harmony harmony)
        {
            Type me = typeof(SettlementPropertyPatches);
            harmony.Patch(AccessTools.Method(typeof(ReservationManager), "CanReserve"), postfix: new HarmonyMethod(me, "CanReserve"));
            harmony.Patch(AccessTools.Method(typeof(ReservationManager), "Reserve"), prefix: new HarmonyMethod(me, "Reserve"));
            harmony.Patch(AccessTools.Method(typeof(CaravanFormingUtility), "AllReachableColonyItems"), postfix: new HarmonyMethod(me, "Reachable"));
            harmony.Patch(AccessTools.Method(typeof(Pawn_CarryTracker), "TryStartCarry", new[] { typeof(Thing) }), prefix: new HarmonyMethod(me, "CarryAll"));
            harmony.Patch(AccessTools.Method(typeof(Pawn_CarryTracker), "TryStartCarry", new[] { typeof(Thing), typeof(int), typeof(bool) }), prefix: new HarmonyMethod(me, "CarryCount"));
            harmony.Patch(AccessTools.Method(typeof(Thing), "SplitOff"), postfix: new HarmonyMethod(me, "Split"));
            harmony.Patch(AccessTools.Method(typeof(Thing), "CanStackWith"), postfix: new HarmonyMethod(me, "Stack"));
            harmony.Patch(AccessTools.Method(typeof(Pawn_TraderTracker), "GiveSoldThingToPlayer"), prefix: new HarmonyMethod(me, "SellStart"), finalizer: new HarmonyMethod(me, "SellEnd"));
            Type vehicle = AccessTools.TypeByName("Vehicles.VehiclePawn");
            if (vehicle == null) return;
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(vehicle).Where(m => m.Name == "AddOrTransfer" && m.ReturnType == typeof(int)))
                harmony.Patch(m, prefix: new HarmonyMethod(me, "Cargo"));
        }
        private static void CanReserve(Pawn claimant, LocalTargetInfo target, ref bool __result)
        {
            if (!__result || !SettlementProperty.Any || !target.HasThing || !SettlementProperty.Blocks(claimant, target.Thing)) return;
            __result = false;
            JobFailReason.Is("TMM.World.SettlementProperty".Translate(target.Thing.MapHeld.Parent.Faction.Name));
        }
        private static bool Reserve(Pawn claimant, LocalTargetInfo target, ref bool __result)
        {
            if (!SettlementProperty.Any || !target.HasThing || !SettlementProperty.Blocks(claimant, target.Thing)) return true;
            __result = false;
            Refuse(claimant, target.Thing);
            return false;
        }
        private static void Reachable(ref List<Thing> __result)
        {
            if (__result != null && SettlementProperty.Any) __result.RemoveAll(SettlementProperty.Protected);
        }
        private static bool CarryAll(Pawn_CarryTracker __instance, Thing item, ref bool __result)
        {
            if (!SettlementProperty.Blocks(__instance.pawn, item)) return true;
            __result = false;
            Refuse(__instance.pawn, item);
            return false;
        }
        private static bool CarryCount(Pawn_CarryTracker __instance, Thing item, ref int __result)
        {
            if (!SettlementProperty.Blocks(__instance.pawn, item)) return true;
            __result = 0;
            Refuse(__instance.pawn, item);
            return false;
        }
        private static bool Cargo(Pawn __instance, Thing __0, ref int __result)
        {
            if (!SettlementProperty.Blocks(__instance, __0)) return true;
            __result = 0;
            Refuse(__instance, __0);
            return false;
        }
        private static void Split(Thing __instance, Thing __result)
        {
            if (__result == null || __result == __instance || selling || !SettlementProperty.IsMarked(__instance)) return;
            SettlementProperty.MarkLike(__instance, __result);
        }
        private static void Stack(Thing __instance, Thing other, ref bool __result)
        {
            if (__result && SettlementProperty.Any && SettlementProperty.IsMarked(__instance) != SettlementProperty.IsMarked(other)) __result = false;
        }
        private static void SellStart(Thing toGive, int countToGive)
        {
            selling = true;
            if (toGive != null && countToGive >= toGive.stackCount) SettlementProperty.Release(toGive);
        }
        private static Exception SellEnd(Exception __exception) { selling = false; return __exception; }
        private static void Refuse(Pawn pawn, Thing thing)
        {
            if (pawn.jobs != null && pawn.CurJob != null && pawn.CurJob.playerForced)
                Messages.Message("TMM.World.SettlementPropertyRefused".Translate(pawn.LabelShort, thing.LabelCap, thing.MapHeld.Parent.Faction.Name), thing, MessageTypeDefOf.RejectInput, false);
        }
    }
}