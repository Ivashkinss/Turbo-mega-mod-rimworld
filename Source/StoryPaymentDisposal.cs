using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    // Payment has already been accepted. Undestroyed stacks must remain unavailable to spend again.
    public sealed class StoryPaymentDisposal : WorldComponent, IThingHolder
    {
        private ThingOwner<Thing> spent;

        public StoryPaymentDisposal(World world) : base(world)
        {
            spent = new ThingOwner<Thing>(this);
        }

        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return spent; }

        public void GetChildHolders(List<IThingHolder> children)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(children, spent);
        }

        public bool Keep(Thing thing)
        {
            if (thing == null || thing.Destroyed) return true;
            if (thing.holdingOwner == spent) return true;
            if (thing.Spawned) thing.DeSpawn(DestroyMode.Vanish);
            if (thing.holdingOwner != null) thing.holdingOwner.Remove(thing);
            return spent.TryAdd(thing, false) && thing.holdingOwner == spent;
        }

        public override void WorldComponentTick()
        {
            if (spent.Count == 0 || Find.TickManager.TicksGame % 250 != 0) return;
            for (int i = spent.Count - 1; i >= 0; i--)
            {
                Thing thing = spent[i];
                try
                {
                    if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
                }
                catch (Exception ex)
                {
                    Log.Warning("[TMM] StoryPayment: disposal still pending for " + thing.ThingID + ": " + ex.Message);
                }
                if (thing.Destroyed && thing.holdingOwner == spent) spent.Remove(thing);
                else if (!thing.Destroyed && thing.holdingOwner != spent && !Keep(thing))
                    Log.Error("[TMM] StoryPayment: pending payment left its holder: " + thing.ThingID);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref spent, "committedPayments", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && spent == null) spent = new ThingOwner<Thing>(this);
        }
    }
}
