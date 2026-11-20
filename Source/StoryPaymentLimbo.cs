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
        private static StoryPaymentLimbo instance;

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
            if (t.Spawned) t.DeSpawn(DestroyMode.Vanish);
            if (t.holdingOwner != null) t.holdingOwner.Remove(t);
            if (!limbo.held.TryAdd(t, false))
            {
                Log.Error("[TMM] StoryPayment: limbo holder refused " + t.LabelShort + ".");
                return false;
            }
            Log.Warning("[TMM] StoryPayment: " + t.LabelShort + " is kept safe until a player home map is available.");
            return true;
        }

        public override void WorldComponentTick()
        {
            if (held.Count == 0 || Find.TickManager.TicksGame % 2500 != 0) return;
            Map home = Find.AnyPlayerHomeMap;
            if (home == null) return;
            IntVec3 spot = DropCellFinder.TradeDropSpot(home);
            if (!spot.IsValid || !spot.InBounds(home)) spot = home.Center;
            for (int i = held.Count - 1; i >= 0; i--)
            {
                Thing t = held[i];
                held.Remove(t);
                if (!GenPlace.TryPlaceThing(t, spot, home, ThingPlaceMode.Near))
                {
                    held.TryAdd(t, false);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref held, "held", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && held == null) held = new ThingOwner<Thing>(this);
            instance = this;
        }
    }
}
