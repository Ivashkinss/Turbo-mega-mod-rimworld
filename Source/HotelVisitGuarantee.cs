using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Hotel visit guarantee (user report 19.11.2026: after a couple of guest groups the next visit was 450 h away).
    /// Hospitality plans a faction's revisit 3–30 days after its visit and only for factions that already came,
    /// which is far too rare for a hotel. Every 6 hours, on each player home with guests welcome: if no visit is
    /// queued within 2 days, plan one from a random friendly humanlike faction 12–48 hours ahead.
    public sealed class HotelVisitGuarantee : GameComponent
    {
        private const int CheckInterval = 2500 * 6;
        public const float WindowDays = 2f;

        private static readonly Type MapCompType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
        private static readonly Type UtilType = AccessTools.TypeByName("Hospitality.Utilities.GenericUtility");
        private static readonly System.Reflection.MethodInfo Queued = MapCompType == null ? null : AccessTools.Method(MapCompType, "GetQueuedVisits");
        private static readonly System.Reflection.FieldInfo Welcome = MapCompType == null ? null : AccessTools.Field(MapCompType, "guestsAreWelcome");
        private static readonly System.Reflection.MethodInfo Plan = UtilType == null ? null : AccessTools.Method(UtilType, "PlanNewVisit");

        public HotelVisitGuarantee(Game game) { }

        public static bool Available { get { return Queued != null && Welcome != null && Plan != null; } }

        public override void GameComponentTick()
        {
            if (!Available || Find.TickManager.TicksGame % CheckInterval != 0) return;
            foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome)) CheckMap(map);
        }

        public static MapComponent HospitalityOf(Map map)
        {
            return MapCompType == null || map == null ? null : map.components.FirstOrDefault(c => MapCompType.IsInstanceOfType(c));
        }

        public static int QueuedWithin(Map map, float days)
        {
            MapComponent comp = HospitalityOf(map);
            if (comp == null || Queued == null) return -1;
            var list = Queued.Invoke(comp, new object[] { days }) as IEnumerable;
            return list == null ? 0 : list.Cast<object>().Count();
        }

        /// Returns the faction a visit was planned for, or null when nothing was needed or possible.
        public static Faction CheckMap(Map map)
        {
            MapComponent comp = HospitalityOf(map);
            if (comp == null || !Available || map.mapPawns.FreeColonistsCount == 0) return null;
            if (!(bool)Welcome.GetValue(comp)) return null;
            if (QueuedWithin(map, WindowDays) > 0) return null;
            Faction f;
            if (!Find.FactionManager.AllFactionsVisible.Where(x => !x.IsPlayer && !x.defeated && x.HasGoodwill && x.def.humanlikeFaction && !x.HostileTo(Faction.OfPlayer)).TryRandomElement(out f)) return null;
            Plan.Invoke(null, new object[] { map, Rand.Range(0.5f, 2f), f });
            return f;
        }
    }
}
