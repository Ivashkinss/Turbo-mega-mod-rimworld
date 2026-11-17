using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

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
}
