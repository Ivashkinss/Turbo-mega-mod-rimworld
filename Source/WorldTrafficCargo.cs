using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    // Actual salvage on the tile where a carrier died or its caravan was forcibly removed.
    public sealed class WorldTrafficCargo : WorldObject, IThingHolder
    {
        public Faction originalFaction;
        public ThingOwner<Thing> goods;
        public WorldTrafficCargo() { goods = new ThingOwner<Thing>(this); }
        public ThingOwner GetDirectlyHeldThings() { return goods; }
        public void GetChildHolders(List<IThingHolder> children) { ThingOwnerUtility.AppendThingHoldersFromThings(children, goods); }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref originalFaction, "originalFaction");
            Scribe_Deep.Look(ref goods, "cargo", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && goods == null) goods = new ThingOwner<Thing>(this);
        }
        public override string GetInspectString()
        {
            return "TMM.WorldTraffic.AbandonedCargo".Translate(goods.Sum(t => t.stackCount));
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            foreach (var gizmo in base.GetCaravanGizmos(caravan)) yield return gizmo;
            if (caravan == null || caravan.Faction != Faction.OfPlayer || caravan.Tile != Tile) yield break;
            yield return new Command_Action
            {
                defaultLabel = "TMM.WorldTraffic.Recover".Translate(), defaultDesc = "TMM.WorldTraffic.RecoverHelp".Translate(), icon = TexButton.Drop,
                action = () => Recover(caravan)
            };
        }
        public void Recover(Caravan caravan)
        {
            if (caravan == null || !caravan.Spawned || caravan.Faction != Faction.OfPlayer || caravan.Tile != Tile) return;
            foreach (var item in goods.InnerListForReading.ToArray())
            {
                foreach (var pawn in caravan.PawnsListForReading.Where(p => p.inventory != null && MassUtility.CanEverCarryAnything(p)))
                {
                    float unitMass = item.GetStatValue(StatDefOf.Mass);
                    int count = unitMass <= 0 ? item.stackCount : Math.Min(item.stackCount, (int)Math.Floor(Math.Max(0, MassUtility.FreeSpace(pawn)) / unitMass));
                    if (count > 0) goods.TryTransferToContainer(item, pawn.inventory.innerContainer, count, true);
                    if (!goods.Contains(item)) break;
                }
            }
            if (goods.Count == 0 && Spawned) Find.WorldObjects.Remove(this);
        }
    }
}

