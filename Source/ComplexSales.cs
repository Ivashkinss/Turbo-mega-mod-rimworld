using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public sealed class CompProperties_ComplexSale : CompProperties
    {
        public CompProperties_ComplexSale() { compClass = typeof(CompComplexSale); }
    }
    public sealed class CompComplexSale : ThingComp
    {
        public bool forSale;
        public override void PostExposeData() { base.PostExposeData(); Scribe_Values.Look(ref forSale, "TMM_forSale"); }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!parent.Spawned || parent.Faction != Faction.OfPlayer) yield break;
            bool active = SaleStock.Active(parent as Building);
            if (!active && !forSale) yield break;
            var command = new Command_Toggle { defaultLabel = "TMM.Sales.ForSale".Translate(),
                defaultDesc = "TMM.Sales.ForSaleHelp".Translate(), icon = ThingDefOf.Silver.uiIcon,
                isActive = () => forSale, toggleAction = () => forSale = !forSale };
            if (!active) command.Disable("TMM.Sales.Outside".Translate());
            yield return command;
        }
        public override string CompInspectStringExtra()
        {
            if (!forSale) return null;
            var storage = parent as Building;
            return (!SaleStock.Active(storage) ? "TMM.Sales.Outside" : SaleStock.Public(storage) ? "TMM.Sales.ForSale" : "TMM.Sales.StaffOnly").Translate();
        }
    }

    public static class SaleStock
    {
        public static bool Supported(Type type)
        {
            return type != null && (typeof(Building_Storage).IsAssignableFrom(type) ||
                typeof(Building_Bookcase).IsAssignableFrom(type) || IsOutfitStand(type));
        }
        private static bool IsOutfitStand(Type type)
        {
            for (var t = type; t != null; t = t.BaseType) if (t.FullName == "RimWorld.Building_OutfitStand") return true;
            return false;
        }
        public static bool Active(Building storage)
        {
            if (storage == null || !storage.Spawned || storage.Faction != Faction.OfPlayer) return false;
            var desk = storage.Map.GetComponent<ComplexMap>().desk;
            return desk != null && desk.Active && storage.OccupiedRect().Cells.All(c => desk.area[c]);
        }
        public static bool Public(Building storage)
        {
            if (!Active(storage)) return false;
            var desk = storage.Map.GetComponent<ComplexMap>().desk;
            return !storage.OccupiedRect().Cells.Any(c => ComplexLayout.StaffOnly(desk, c));
        }
        public static IEnumerable<Thing> Contents(Building storage)
        {
            var shelf = storage as Building_Storage;
            if (shelf != null) return shelf.GetSlotGroup().HeldThings;
            var searchable = storage as ISearchableContents;
            return searchable == null ? Enumerable.Empty<Thing>() : searchable.SearchableContents;
        }
        public static bool Tradeable(Thing thing)
        {
            return thing != null && !thing.Destroyed && thing.stackCount > 0 && !(thing is Pawn) && !(thing is Corpse) &&
                thing.def != ThingDefOf.Silver && !thing.def.isUnfinishedThing &&
                (thing.def.tradeability.PlayerCanSell() || RestaurantMeals.Prepared(thing.def)) &&
                (thing.Faction == null || thing.Faction == Faction.OfPlayer) && !thing.IsForbidden(Faction.OfPlayer) &&
                (thing.def.thingSetMakerTags == null || !thing.def.thingSetMakerTags.Contains("NotForGuests")) &&
                !CompBiocodable.IsBiocoded(thing) && !float.IsNaN(thing.MarketValue) && !float.IsInfinity(thing.MarketValue) && thing.MarketValue > 0;
        }
        public static bool Contains(Building storage, Thing item)
        {
            var comp = storage == null ? null : storage.GetComp<CompComplexSale>();
            return comp != null && comp.forSale && Active(storage) && Tradeable(item) && Contents(storage).Contains(item);
        }
        public static IEnumerable<Building> Stores(Map map)
        {
            return map.listerBuildings.allBuildingsColonist.Where(b => {
                var comp = b.GetComp<CompComplexSale>(); return comp != null && comp.forSale && Active(b);
            });
        }
        public static int Space(Pawn buyer, Thing item)
        {
            int count = MassUtility.CountToPickUpUntilOverEncumbered(buyer, item);
            var utility = AccessTools.TypeByName("Hospitality.Utilities.ItemUtility");
            if (utility != null) count = Math.Min(count, (int)AccessTools.Method(utility, "GetInventorySpaceFor").Invoke(null, new object[] { buyer, item }));
            return Math.Max(0, count);
        }
        public static bool Buyer(Pawn pawn)
        {
            return RestaurantMeals.Guest(pawn) && !Clinic.ActivePatient(pawn) && pawn.Awake() && pawn.inventory != null;
        }
        public static bool Allowed(Pawn buyer, Building storage, Thing item)
        {
            if (!Buyer(buyer) || !Public(storage) || storage.Map != buyer.Map || !Contains(storage, item) ||
                !RestaurantMeals.AllowedCell(buyer, storage.Position) || storage.IsForbidden(buyer) || Space(buyer, item) < 1) return false;
            if (item.def.ingestible != null && (!buyer.RaceProps.CanEverEat(item) || item.IsNotFresh() ||
                (buyer.foodRestriction != null && !buyer.foodRestriction.CurrentFoodPolicy.Allows(item.def)) || !FoodUtility.WillEat(buyer, item))) return false;
            return true;
        }
        public static double UnitPrice(Thing item)
        {
            // Match Hospitality's normal retail factor, while keeping the quote
            // independent of the stock's spawned/container representation.
            return (double)item.MarketValue * 0.55;
        }
        public static int Price(double unit, int count)
        {
            double value = unit * count;
            return double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > int.MaxValue ? -1 :
                Math.Max(1, (int)decimal.Ceiling((decimal)unit * count));
        }
    }

    public sealed class SaleOrder : IExposable, IThingHolder
    {
        public Pawn buyer;
        public Building storage;
        public Thing item;
        public int id, count, price, created, refund;
        public double unitPrice;
        public bool completed, cancelled;
        public IntVec3 receiptCell;
        private ThingOwner<Thing> cash;
        public ThingOwner<Thing> returnedGoods;
        public SaleOrder() { cash = new ThingOwner<Thing>(this); returnedGoods = new ThingOwner<Thing>(this); }
        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return cash; }
        public void GetChildHolders(List<IThingHolder> holders) { ThingOwnerUtility.AppendThingHoldersFromThings(holders, cash); ThingOwnerUtility.AppendThingHoldersFromThings(holders, returnedGoods); }
        public void ExposeData()
        {
            Scribe_References.Look(ref buyer, "buyer"); Scribe_References.Look(ref storage, "storage"); Scribe_References.Look(ref item, "item");
            Scribe_Values.Look(ref id, "id"); Scribe_Values.Look(ref count, "count"); Scribe_Values.Look(ref price, "price"); Scribe_Values.Look(ref created, "created");
            Scribe_Values.Look(ref unitPrice, "unitPrice"); Scribe_Values.Look(ref refund, "refund");
            Scribe_Values.Look(ref completed, "completed"); Scribe_Values.Look(ref cancelled, "cancelled"); Scribe_Values.Look(ref receiptCell, "receiptCell");
            Scribe_Deep.Look(ref cash, "cash", this);
            Scribe_Deep.Look(ref returnedGoods, "returnedGoods", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                if (cash == null) cash = new ThingOwner<Thing>(this);
                if (returnedGoods == null) returnedGoods = new ThingOwner<Thing>(this);
            }
        }
    }

    public sealed class ComplexSales : MapComponent
    {
        public List<SaleOrder> orders = new List<SaleOrder>();
        public long earned;
        private int nextId, nextCheck;
        public ComplexSales(Map map) : base(map) { }
        public static JobDef BuyJob { get { return DefDatabase<JobDef>.GetNamed("TMM_BuyComplexGoods"); } }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Collections.Look(ref orders, "TMM_saleOrders", LookMode.Deep);
            Scribe_Values.Look(ref nextId, "TMM_saleNextId"); Scribe_Values.Look(ref earned, "TMM_saleIncome");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) { if (orders == null) orders = new List<SaleOrder>(); orders.RemoveAll(o => o == null); }
        }
        public SaleOrder ById(int id) { return orders.FirstOrDefault(o => o.id == id); }
        public int FreeCount(Thing item, SaleOrder own = null)
        {
            if (!SaleStock.Tradeable(item) || orders.Any(o => o != own && !o.cancelled && !o.completed && o.item == item)) return 0;
            if (map.reservationManager.IsReserved(item) && (own == null || !own.buyer.HasReserved(item))) return 0;
            return item.stackCount;
        }
        public bool Valid(SaleOrder o)
        {
            return o != null && !o.completed && !o.cancelled && o.count > 0 && SaleStock.Allowed(o.buyer, o.storage, o.item) &&
                o.item.stackCount >= o.count && FreeCount(o.item, o) >= o.count && Find.TickManager.TicksGame - o.created < 6000 &&
                (Find.TickManager.TicksGame <= o.created + 1 || o.buyer.CurJobDef == BuyJob && o.buyer.CurJob.count == o.id);
        }
        public SaleOrder Create(Pawn buyer, Building storage, Thing item, int requested)
        {
            if (!SaleStock.Allowed(buyer, storage, item) || orders.Any(o => !o.cancelled && !o.completed && o.buyer == buyer)) return null;
            double unit = SaleStock.UnitPrice(item);
            int count = Math.Min(Math.Min(requested, FreeCount(item)), SaleStock.Space(buyer, item));
            if (count <= 0) return null;
            int price = SaleStock.Price(unit, count);
            if (price <= 0 || HotelBillingHooks.Money(buyer) < price || !buyer.CanReserveAndReach(storage, PathEndMode.Touch, Danger.None)) return null;
            var desk = map.GetComponent<ComplexMap>().desk;
            var order = new SaleOrder { id = ++nextId, buyer = buyer, storage = storage, item = item, count = count,
                unitPrice = unit, price = price, created = Find.TickManager.TicksGame, receiptCell = desk.Position };
            orders.Add(order); return order;
        }
        public Job ShoppingJob(Pawn buyer, Func<Thing, bool> qualifies)
        {
            if (!SaleStock.Buyer(buyer) || buyer.CurJobDef == BuyJob) return null;
            foreach (var storage in SaleStock.Stores(map).Where(SaleStock.Public).InRandomOrder())
                foreach (var item in SaleStock.Contents(storage).Where(SaleStock.Tradeable).Where(qualifies).InRandomOrder()) {
                    int affordable = (int)Math.Min(10, HotelBillingHooks.Money(buyer) / SaleStock.UnitPrice(item));
                    if (affordable <= 0) continue;
                    var order = Create(buyer, storage, item, Rand.RangeInclusive(1, affordable));
                    if (order == null) continue;
                    var job = JobMaker.MakeJob(BuyJob, storage); job.count = order.id; return job;
                }
            return null;
        }
        public bool Buy(SaleOrder o)
        {
            if (!Valid(o) || o.buyer.CurJobDef != BuyJob || o.buyer.CurJob.count != o.id ||
                !o.buyer.Position.AdjacentTo8WayOrInside(o.storage) || SaleStock.Space(o.buyer, o.item) < o.count) return false;
            var cash = o.GetDirectlyHeldThings();
            if (HotelBillingHooks.Money(o.buyer) < o.price) return false;
            int required = o.price;
            foreach (var silver in o.buyer.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).ToArray()) {
                int before = cash.Sum(t => t.stackCount);
                o.buyer.inventory.innerContainer.TryTransferToContainer(silver, cash, Math.Min(required, silver.stackCount), false);
                required -= cash.Sum(t => t.stackCount) - before;
                if (required == 0) break;
            }
            if (required != 0) { o.cancelled = true; Process(); return false; }
            Thing bought = null; var owner = o.item.holdingOwner;
            if (!o.item.Spawned && owner != null) owner.TryTransferToContainer(o.item, o.buyer.inventory.innerContainer, o.count, out bought, false);
            else if (o.item.Spawned) {
                var piece = o.item.SplitOff(o.count);
                o.buyer.inventory.innerContainer.TryAdd(piece, false);
                if (o.buyer.inventory.innerContainer.Contains(piece)) bought = piece;
                else if (!piece.Destroyed && !piece.Spawned) o.returnedGoods.TryAdd(piece, false);
            }
            int actual = bought != null && o.buyer.inventory.innerContainer.Contains(bought) ? bought.stackCount : 0;
            if (actual <= 0) { o.cancelled = true; Process(); return false; }
            o.completed = true; o.count = actual; o.price = SaleStock.Price(o.unitPrice, actual);
            o.refund = Math.Max(0, cash.Sum(t => t.stackCount) - o.price);
            RestaurantMeals.MarkSold(o.buyer, bought);
            Process(); return true;
        }
        public void Process()
        {
            foreach (var o in orders.ToArray()) {
                if (!o.completed && !o.cancelled && !Valid(o)) o.cancelled = true;
                var cash = o.GetDirectlyHeldThings();
                if (o.cancelled) o.refund = cash.Sum(t => t.stackCount);
                if (o.refund > 0 && o.buyer != null && o.buyer.inventory != null && !o.buyer.Destroyed) {
                    foreach (var silver in cash.ToArray()) {
                        int before = cash.Sum(t => t.stackCount);
                        cash.TryTransferToContainer(silver, o.buyer.inventory.innerContainer, Math.Min(o.refund, silver.stackCount), false);
                        o.refund -= before - cash.Sum(t => t.stackCount);
                        if (o.refund == 0) break;
                    }
                } else if (o.refund > 0) {
                    foreach (var silver in cash.ToArray()) {
                        int before = cash.Sum(t => t.stackCount); Thing dropped;
                        cash.TryDrop(silver, o.receiptCell, map, ThingPlaceMode.Near, Math.Min(o.refund, silver.stackCount), out dropped);
                        o.refund -= before - cash.Sum(t => t.stackCount);
                        if (o.refund == 0) break;
                    }
                }
                if (o.completed && o.refund == 0) {
                    int before = cash.Sum(t => t.stackCount); cash.TryDropAll(o.receiptCell, map, ThingPlaceMode.Near);
                    earned += before - cash.Sum(t => t.stackCount);
                }
                if (o.returnedGoods.Count > 0) {
                    var source = o.storage == null || o.storage.Destroyed ? null : o.storage as ISearchableContents;
                    if (source != null) o.returnedGoods.TryTransferAllToContainer(source.SearchableContents, false);
                    o.returnedGoods.TryDropAll(o.storage != null && o.storage.Spawned ? o.storage.Position : o.receiptCell, map, ThingPlaceMode.Near);
                }
                if ((o.cancelled || o.completed) && cash.Count == 0 && o.returnedGoods.Count == 0) orders.Remove(o);
            }
        }
        public override void MapComponentTick() { if (Find.TickManager.TicksGame >= nextCheck) { nextCheck = Find.TickManager.TicksGame + 120; Process(); } }
    }

    public sealed class JobDriver_BuyComplexGoods : JobDriver
    {
        private SaleOrder Order { get { return pawn.Map == null ? null : pawn.Map.GetComponent<ComplexSales>().ById(job.count); } }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var o = Order;
            return o != null && pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) &&
                (!o.item.Spawned || pawn.Reserve(o.item, job, 1, o.count, null, errorOnFailed));
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(delegate { var o = Order; if (o != null && !o.completed) o.cancelled = true; });
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Order == null || !pawn.Map.GetComponent<ComplexSales>().Valid(Order));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(90).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            var buy = new Toil(); buy.initAction = delegate { if (!pawn.Map.GetComponent<ComplexSales>().Buy(Order)) EndJobWith(JobCondition.Incompletable); };
            yield return buy;
        }
    }

    [StaticConstructorOnStartup]
    public static class ComplexSalesHooks
    {
        static ComplexSalesHooks()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => SaleStock.Supported(d.thingClass))) {
                if (def.comps == null) def.comps = new List<CompProperties>();
                if (!def.comps.Any(c => c.compClass == typeof(CompComplexSale))) def.comps.Add(new CompProperties_ComplexSale());
            }
            var h = new Harmony("turbomegamod.complex.sales");
            var type = AccessTools.TypeByName("Hospitality.JoyGiver_BuyStuff");
            if (type == null) return;
            h.Patch(AccessTools.Method(type, "GetChance"), prefix: new HarmonyMethod(typeof(ComplexSalesHooks), "Chance"));
            h.Patch(AccessTools.Method(type, "TryGiveJob"), prefix: new HarmonyMethod(typeof(ComplexSalesHooks), "Shopping"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.ItemUtility"), "IsBuyableNow"), postfix: new HarmonyMethod(typeof(ComplexSalesHooks), "NativeBuyable"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.JobDriver_BuyItem"), "BuyThing"), prefix: new HarmonyMethod(typeof(ComplexSalesHooks), "LegacyPurchase"));
        }
        private static bool HasComplex(Pawn pawn) { var d = pawn == null || pawn.Map == null ? null : pawn.Map.GetComponent<ComplexMap>().desk; return d != null && d.Active; }
        private static bool Chance(Pawn __0, ref float __result)
        {
            if (!HasComplex(__0)) return true;
            __result = SaleStock.Buyer(__0) && HotelBillingHooks.Money(__0) > 0 && SaleStock.Stores(__0.Map).Any(SaleStock.Public) ? 25f : 0f;
            return false;
        }
        private static bool Shopping(JoyGiver __instance, Pawn __0, ref Job __result)
        {
            if (!HasComplex(__0)) return true;
            var qualifies = AccessTools.Method(__instance.GetType(), "Qualifies");
            __result = __0.Map.GetComponent<ComplexSales>().ShoppingJob(__0, t => (bool)qualifies.Invoke(__instance, new object[] { t, __0 }));
            return false;
        }
        private static void NativeBuyable(Pawn __0, Thing __1, ref bool __result)
        {
            if (__result && HasComplex(__0)) __result = SaleStock.Stores(__0.Map).Any(s => SaleStock.Allowed(__0, s, __1));
        }
        private static bool LegacyPurchase(JobDriver __instance) { return !HasComplex(__instance.pawn); }
    }
}
