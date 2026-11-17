using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public static class GuestTripBudget
    {
        public const int MinTarget = 150;
        public const int MaxTarget = 300;
        public const int TargetCeiling = 10000;

        private static readonly MethodInfo getInventorySpaceMethod = AccessTools.Method("Hospitality.Utilities.ItemUtility:GetInventorySpaceFor", new[] { typeof(Pawn), typeof(Thing) });

        public static float SilverMultiplier()
        {
            var type = AccessTools.TypeByName("Hospitality.ModSettings_Hospitality");
            if (type != null)
            {
                var field = AccessTools.Field(type, "silverMultiplier");
                if (field != null)
                {
                    object val = field.GetValue(null);
                    if (val is float)
                    {
                        float f = (float)val;
                        if (float.IsNaN(f) || float.IsInfinity(f) || f <= 0f) return 0f;
                        return f;
                    }
                }
            }
            return 1f;
        }

        public static int CalculateTarget(int baseRoll, float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f || baseRoll <= 0)
                return 0;
            double scaled = (double)baseRoll * (double)multiplier;
            if (double.IsNaN(scaled) || double.IsInfinity(scaled) || scaled <= 0.0)
                return 0;
            if (scaled >= (double)TargetCeiling)
                return TargetCeiling;
            long rounded = (long)Math.Round(scaled, MidpointRounding.AwayFromZero);
            if (rounded >= TargetCeiling)
                return TargetCeiling;
            if (rounded <= 0)
                return 0;
            return (int)rounded;
        }

        public static int CalculateTarget(float multiplier)
        {
            return CalculateTarget(Rand.RangeInclusive(MinTarget, MaxTarget), multiplier);
        }

        public static int NativeInventorySpace(Pawn pawn, Thing item)
        {
            if (pawn == null || item == null) return 0;
            if (getInventorySpaceMethod != null)
            {
                try
                {
                    object res = getInventorySpaceMethod.Invoke(null, new object[] { pawn, item });
                    if (res is int)
                    {
                        int space = (int)res;
                        return Math.Max(0, space);
                    }
                }
                catch { return 0; }
            }
            return item.stackCount;
        }

        public static bool Eligible(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Map == null) return false;
            if (pawn.Faction == Faction.OfPlayer || pawn.IsSlave || pawn.IsPrisoner) return false;
            if (pawn.HostileTo(Faction.OfPlayer)) return false;
            if (pawn.ageTracker == null || pawn.ageTracker.AgeBiologicalYearsFloat < 18f) return false;
            if (Clinic.ActivePatient(pawn)) return false;
            if (WholesaleTrade.Group(pawn) != null) return false;
            if (GuestDossiers.Blocked(pawn)) return false;
            if (GuestDossiers.Desk(pawn.Map) == null) return false;
            return true;
        }

        public static void PrepareTrip(Pawn pawn)
        {
            if (!Eligible(pawn)) return;
            if (GuestDossiers.Book == null) return;
            var row = GuestDossiers.Book.Observe(pawn);
            if (row == null || row.Current != null) return;
            var profile = row.profile;
            if (profile == null || profile.vip || profile.tripPrepared) return;

            profile.tripPrepared = true;
            profile.tripTarget = 0;
            profile.tripAdded = 0;

            float mult = SilverMultiplier();
            if (mult <= 0f) return;

            int target = CalculateTarget(mult);
            profile.tripTarget = target;
            if (target <= 0) return;

            if (pawn.inventory == null || pawn.inventory.innerContainer == null) return;
            int currentMoney = HotelBillingHooks.Money(pawn);
            if (currentMoney >= target) return;

            float silverMass = ThingDefOf.Silver.GetStatValueAbstract(StatDefOf.Mass);
            double freeSpace = MassUtility.FreeSpace(pawn);
            int massExtra = GuestProfileRules.CoinsToAdd(currentMoney, target, freeSpace, silverMass);
            if (massExtra <= 0) return;

            var probe = ThingMaker.MakeThing(ThingDefOf.Silver);
            probe.stackCount = massExtra;
            int nativeSpace = NativeInventorySpace(pawn, probe);
            int extra = Math.Min(massExtra, nativeSpace);

            if (extra <= 0)
            {
                probe.Destroy();
                return;
            }

            probe.stackCount = extra;
            if (pawn.inventory.innerContainer.TryAdd(probe, false))
            {
                profile.tripAdded = extra;
            }
            else
            {
                probe.Destroy();
            }
            GuestProfiles.Consolidate(pawn);
        }

        public static void ResetTrip(GuestProfile profile)
        {
            if (profile == null) return;
            profile.tripPrepared = false;
            profile.tripTarget = 0;
            profile.tripAdded = 0;
        }
    }
}
