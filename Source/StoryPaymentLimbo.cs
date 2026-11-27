using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Last-resort, saved holder for items a payment or cargo rollback could not return anywhere
    /// (no original owner, no caravan pawn, no player home map at that moment). Items wait here and are
    /// dropped on the first available player home map, so a rollback never deletes player property.
    /// </summary>
    public sealed class StoryPaymentLimbo : WorldComponent, IThingHolder
    {
        private ThingOwner<Thing> held;
        private List<Thing> strays = new List<Thing>();   // the holder refused them too: kept and saved here until a delivery succeeds
        private static StoryPaymentLimbo instance;

        public static Func<Thing, IntVec3, Map, bool> TestLimboDropOverride;

        public StoryPaymentLimbo(World world) : base(world)
        {
            held = new ThingOwner<Thing>(this);
            instance = this;
        }

        public IThingHolder ParentHolder { get { return null; } }

        public ThingOwner GetDirectlyHeldThings() { return held; }

        public void GetChildHolders(List<IThingHolder> outChildren) { ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings()); }

        /// <summary>Takes an item nobody else accepted. Returns true when the item is safely held.</summary>
        public static bool Hold(Thing t)
        {
            if (t == null || t.Destroyed || t.stackCount <= 0) return true;
            StoryPaymentLimbo limbo = instance ?? (Find.World != null ? Find.World.GetComponent<StoryPaymentLimbo>() : null);
            if (limbo == null)
            {
                Log.Error("[TMM] StoryPayment: no holder accepted " + t.LabelShort + " and the limbo holder is unavailable.");
                return false;
            }
            if (t.holdingOwner == limbo.held) return true;
            // Limbo must not steal an item from another legitimate holder!
            if (t.holdingOwner != null)
            {
                Log.Warning("[TMM] StoryPaymentLimbo.Hold: " + t.LabelShort + " is already held by another owner (" + t.holdingOwner.Owner + "); not taking ownership.");
                return true;
            }
            if (t.Spawned) t.DeSpawn(DestroyMode.Vanish);
            if (!limbo.held.TryAdd(t, false) || t.holdingOwner != limbo.held)
            {
                if (t.holdingOwner == null && !limbo.strays.Contains(t)) limbo.strays.Add(t);
                Log.Warning("[TMM] StoryPayment: limbo holder refused " + t.LabelShort + "; it is kept aside until the holder accepts it.");
                return true;
            }
            Log.Warning("[TMM] StoryPayment: " + t.LabelShort + " is kept safe until a player home map is available.");
            return true;
        }

        public void TryDropAllHeldToMap(Map map, IntVec3 spot)
        {
            if (held == null || held.Count == 0 || map == null) return;
            if (!spot.IsValid || !spot.InBounds(map)) spot = map.Center;

            for (int i = held.Count - 1; i >= 0; i--)
            {
                Thing t = held[i];
                if (t == null || t.Destroyed)
                {
                    held.Remove(t);
                    continue;
                }

                // The item stays in the holder while it is being placed: spawning moves it out, a failure leaves it where it was.
                bool placed = false;
                try
                {
                    if (TestMode.Enabled && TestLimboDropOverride != null)
                    {
                        placed = TestLimboDropOverride(t, spot, map);
                    }
                    else
                    {
                        placed = GenPlace.TryPlaceThing(t, spot, map, ThingPlaceMode.Near);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[TMM] StoryPaymentLimbo: exception while dropping " + t.LabelShort + ": " + ex.Message);
                    placed = false;
                }
                finally
                {
                    // Ensure item is NEVER lost: if not confirmed placed and spawned on map, restore to held immediately!
                    if (!placed || !t.Spawned || t.Map != map)
                    {
                        if (t.holdingOwner == held)
                        {
                            // Already in held
                        }
                        else if (t.holdingOwner != null)
                        {
                            // Legitimate other holder accepted it; do not steal!
                        }
                        else if (!t.Destroyed)
                        {
                            if (t.Spawned) t.DeSpawn(DestroyMode.Vanish);
                            if (!held.TryAdd(t, false) && !strays.Contains(t)) strays.Add(t);
                        }
                    }
                }
            }
        }

        private void RetryStrays()
        {
            for (int i = strays.Count - 1; i >= 0; i--)
            {
                Thing t = strays[i];
                if (t == null || t.Destroyed || t.Spawned || t.holdingOwner != null) { strays.RemoveAt(i); continue; }
                if (held.TryAdd(t, false)) strays.RemoveAt(i);
            }
        }

        public int HeldCount { get { return held.Count + strays.Count; } }

        public override void WorldComponentTick()
        {
            if (strays.Count > 0 && Find.TickManager.TicksGame % 250 == 0) RetryStrays();
            if (held.Count == 0 || Find.TickManager.TicksGame % 2500 != 0) return;
            Map home = Find.AnyPlayerHomeMap;
            if (home == null) return;
            IntVec3 spot = DropCellFinder.TradeDropSpot(home);
            if (!spot.IsValid || !spot.InBounds(home)) spot = home.Center;
            TryDropAllHeldToMap(home, spot);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref held, "held", this);
            if (Scribe.mode == LoadSaveMode.Saving) strays.RemoveAll(t => t == null || t.Destroyed || t.Spawned || t.holdingOwner != null);
            Scribe_Collections.Look(ref strays, "strays", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && strays == null) strays = new List<Thing>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit && held == null) held = new ThingOwner<Thing>(this);
            instance = this;
        }
    }
}
