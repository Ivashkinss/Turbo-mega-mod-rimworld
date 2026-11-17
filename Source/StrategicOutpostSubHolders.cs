using System;
using System.Collections.Generic;
using Verse;

namespace TurboMegaMod
{
    public class OutpostSubHolder : IThingHolder, IExposable
    {
        private StrategicOutpost parent;
        private ThingOwner<Thing> container;
        private string holderName;

        public OutpostSubHolder()
        {
        }

        public OutpostSubHolder(StrategicOutpost parent, string holderName)
        {
            this.parent = parent;
            this.holderName = holderName;
            this.container = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public StrategicOutpost Parent
        {
            get { return parent; }
        }

        public ThingOwner<Thing> Container
        {
            get { return container; }
        }

        public IThingHolder ParentHolder
        {
            get { return parent; }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return container;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref parent, "parent");
            Scribe_Values.Look(ref holderName, "holderName");
            Scribe_Deep.Look(ref container, "container", this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && container == null)
            {
                container = new ThingOwner<Thing>(this, false, LookMode.Deep);
            }
        }
    }

    public class OutpostPawnHolder : IThingHolder, IExposable, IThingHolderTickable
    {
        public bool ShouldTickContents { get { return false; } }
        private StrategicOutpost parent;
        private ThingOwner<Pawn> container;

        public OutpostPawnHolder()
        {
        }

        public OutpostPawnHolder(StrategicOutpost parent)
        {
            this.parent = parent;
            this.container = new ThingOwner<Pawn>(this, false, LookMode.Reference);
        }

        public StrategicOutpost Parent
        {
            get { return parent; }
        }

        public ThingOwner<Pawn> Container
        {
            get { return container; }
        }

        public IThingHolder ParentHolder
        {
            get { return parent; }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return container;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref parent, "parent");
            Scribe_Deep.Look(ref container, "container", this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && container == null)
            {
                container = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
        }
    }
}
