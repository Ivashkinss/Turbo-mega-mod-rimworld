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
            if (WholesaleTrade.Group(buyer) != null) return WholesaleTrade.Space(buyer, item);
            int count = MassUtility.CountToPickUpUntilOverEncumbered(buyer, item);
            var utility = AccessTools.TypeByName("Hospitality.Utilities.ItemUtility");
            if (utility != null) count = Math.Min(count, (int)AccessTools.Method(utility, "GetInventorySpaceFor").Invoke(null, new object[] { buyer, item }));
            return Math.Max(0, count);
        }
        public static bool Buyer(Pawn pawn)
        {
            return (RestaurantMeals.Guest(pawn) && !Clinic.ActivePatient(pawn) || WholesaleTrade.Buyer(pawn)) && pawn.Awake() && pawn.inventory != null;
        }
        public static bool Allowed(Pawn buyer, Building storage, Thing item)
        {
            if (!Buyer(buyer) || !Public(storage) || storage.Map != buyer.Map || !Contains(storage, item) ||
                !RestaurantMeals.AllowedCell(buyer, storage.Position) || storage.IsForbidden(buyer) || Space(buyer, item) < 1) return false;
            return Suitable(buyer, item);
        }
        public static bool Suitable(Pawn buyer, Thing item)
        {
            if (WholesaleTrade.Group(buyer) != null) return true;
            return item.def.ingestible == null || buyer.RaceProps.CanEverEat(item) && !item.IsNotFresh() &&
                (buyer.foodRestriction == null || buyer.foodRestriction.CurrentFoodPolicy.Allows(item.def)) && FoodUtility.WillEat(buyer, item);
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
        public Pawn buyer, worker;
        public Building storage;
        public Thing item, goods;
        public int id, count, price, created, refund;
        public double unitPrice;
        public bool completed, cancelled, delivery;
        public IntVec3 receiptCell, waitCell;
        private ThingOwner<Thing> cash;
        public ThingOwner<Thing> returnedGoods;
        public List<WholesaleDebit> wholesaleDebits = new List<WholesaleDebit>();
        public List<WholesaleRecovery> wholesaleRecoveries = new List<WholesaleRecovery>();
        public List<Thing> unplacedGoods = new List<Thing>();
        public SaleOrder() { cash = new ThingOwner<Thing>(this); returnedGoods = new ThingOwner<Thing>(this); }
        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return cash; }
        public void GetChildHolders(List<IThingHolder> holders) { ThingOwnerUtility.AppendThingHoldersFromThings(holders, cash); ThingOwnerUtility.AppendThingHoldersFromThings(holders, returnedGoods); }
        public void ExposeData()
        {
            Scribe_References.Look(ref buyer, "buyer"); Scribe_References.Look(ref storage, "storage"); Scribe_References.Look(ref item, "item");
            Scribe_References.Look(ref worker, "worker"); Scribe_References.Look(ref goods, "goods");
            Scribe_Values.Look(ref delivery, "delivery"); Scribe_Values.Look(ref waitCell, "waitCell");
            Scribe_Values.Look(ref id, "id"); Scribe_Values.Look(ref count, "count"); Scribe_Values.Look(ref price, "price"); Scribe_Values.Look(ref created, "created");
            Scribe_Values.Look(ref unitPrice, "unitPrice"); Scribe_Values.Look(ref refund, "refund");
            Scribe_Values.Look(ref completed, "completed"); Scribe_Values.Look(ref cancelled, "cancelled"); Scribe_Values.Look(ref receiptCell, "receiptCell");
            Scribe_Deep.Look(ref cash, "cash", this);
            Scribe_Deep.Look(ref returnedGoods, "returnedGoods", this);
            Scribe_Collections.Look(ref wholesaleDebits, "wholesaleDebits", LookMode.Deep);
            Scribe_Collections.Look(ref wholesaleRecoveries, "wholesaleRecoveries", LookMode.Deep);
            Scribe_Collections.Look(ref unplacedGoods, "unplacedGoods", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                if (cash == null) cash = new ThingOwner<Thing>(this);
                if (returnedGoods == null) returnedGoods = new ThingOwner<Thing>(this);
                if (wholesaleDebits == null) wholesaleDebits = new List<WholesaleDebit>();
                if (wholesaleRecoveries == null) wholesaleRecoveries = new List<WholesaleRecovery>();
                if (unplacedGoods == null) unplacedGoods = new List<Thing>();
                wholesaleDebits.RemoveAll(d => d == null); wholesaleRecoveries.RemoveAll(r => r == null);
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
        public static JobDef WaitJob { get { return DefDatabase<JobDef>.GetNamed("TMM_WaitForGoods"); } }
        public static JobDef DeliverJob { get { return DefDatabase<JobDef>.GetNamed("TMM_DeliverGoods"); } }
        public static WorkTypeDef SalesWork { get { return DefDatabase<WorkTypeDef>.GetNamed("TMM_SalesService"); } }
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
            if (map.reservationManager.IsReserved(item) && (own == null ||
                !own.buyer.HasReserved(item) && (own.worker == null || !own.worker.HasReserved(item)))) return 0;
            return item.stackCount;
        }
        public bool Valid(SaleOrder o)
        {
            if (o == null || o.completed || o.cancelled || o.count <= 0 || Find.TickManager.TicksGame - o.created >= 6000 ||
                !SaleStock.Buyer(o.buyer) || o.buyer.Map != map ||
                !(Find.TickManager.TicksGame <= o.created + 1 || o.buyer.CurJobDef == (o.delivery ? WaitJob : BuyJob) && o.buyer.CurJob.count == o.id)) return false;
            if (!o.delivery) return SaleStock.Allowed(o.buyer, o.storage, o.item) && o.item.stackCount >= o.count && FreeCount(o.item, o) >= o.count;
            var flag = o.storage == null ? null : o.storage.GetComp<CompComplexSale>();
            if (flag == null || !flag.forSale || !SaleStock.Active(o.storage) || !WaitingCell(o.buyer, o.waitCell) ||
                o.worker != null && !WorkerEnabled(o.worker)) return false;
            if (o.goods != null) return o.worker != null && o.worker.Spawned && o.worker.Map == map &&
                o.worker.carryTracker.CarriedThing == o.goods && SaleStock.Tradeable(o.goods) && o.goods.stackCount == o.count;
            return SaleStock.Contains(o.storage, o.item) && o.item.stackCount >= o.count && FreeCount(o.item, o) >= o.count;
        }
        public bool WaitingCell(Pawn buyer, IntVec3 cell)
        {
            var desk = map.GetComponent<ComplexMap>().desk;
            return desk != null && desk.Active && cell.InBounds(map) && desk.area[cell] && !ComplexLayout.StaffOnly(desk, cell) &&
                cell.Standable(map) && RestaurantMeals.AllowedCell(buyer, cell) && !cell.IsForbidden(buyer);
        }
        public IntVec3 WaitAt(Pawn buyer)
        {
            var desk = map.GetComponent<ComplexMap>().desk;
            if (desk == null || !desk.Active) return IntVec3.Invalid;
            foreach (var c in GenRadial.RadialCellsAround(desk.Position, 5f, true))
                if (WaitingCell(buyer, c) && buyer.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None)) return c;
            return IntVec3.Invalid;
        }
        public bool WorkerEnabled(Pawn p)
        {
            var desk = map.GetComponent<ComplexMap>().desk;
            return p != null && p.Spawned && p.Map == map && p.Faction == Faction.OfPlayer && !p.Dead && !p.Downed && !p.Drafted &&
                !p.InMentalState && p.workSettings != null && !p.WorkTypeIsDisabled(SalesWork) && p.workSettings.GetPriority(SalesWork) > 0 &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && p.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                desk != null && desk.Active && desk.Mode(p) != ComplexWorkMode.ColonyOnly;
        }
        public bool CanCollect(Pawn worker, Building storage, Thing item, int count)
        {
            return WorkerEnabled(worker) && worker.carryTracker.CarriedThing == null && worker.carryTracker.AvailableStackSpace(item.def) >= count &&
                !storage.IsForbidden(worker) && !item.IsForbidden(worker) && ComplexWorkPolicy.Allows(worker, storage.Position) &&
                worker.CanReserveAndReach(storage, PathEndMode.Touch, Danger.None) && worker.CanReserve(item, 1, count);
        }
        public bool Requestable(Pawn buyer, Building storage, Thing item)
        {
            if (SaleStock.Public(storage)) return SaleStock.Allowed(buyer, storage, item);
            if (!SaleStock.Buyer(buyer) || buyer.Map != map || storage == null || storage.Map != map || !SaleStock.Contains(storage, item) ||
                SaleStock.Space(buyer, item) < 1) return false;
            var cell = WaitAt(buyer);
            return SaleStock.Suitable(buyer, item) && cell.IsValid && map.mapPawns.AllPawnsSpawned.Any(p =>
                CanCollect(p, storage, item, 1) && p.CanReach(cell, PathEndMode.OnCell, Danger.None));
        }
        public SaleOrder Create(Pawn buyer, Building storage, Thing item, int requested)
        {
            if (!Requestable(buyer, storage, item) || orders.Any(o => !o.cancelled && !o.completed && o.buyer == buyer)) return null;
            bool delivery = !SaleStock.Public(storage);
            double unit = SaleStock.UnitPrice(item);
            int count = Math.Min(Math.Min(requested, FreeCount(item)), SaleStock.Space(buyer, item));
            if (delivery && count > 0) count = Math.Min(count, map.mapPawns.AllPawnsSpawned.Where(p => CanCollect(p, storage, item, 1) &&
                p.CanReach(WaitAt(buyer), PathEndMode.OnCell, Danger.None)).Max(p => p.carryTracker.AvailableStackSpace(item.def)));
            if (count <= 0) return null;
            int price = SaleStock.Price(unit, count);
            if (price <= 0 || WholesaleTrade.Money(buyer) < price || !delivery && !buyer.CanReserveAndReach(storage, PathEndMode.Touch, Danger.None)) return null;
            var desk = map.GetComponent<ComplexMap>().desk;
            var order = new SaleOrder { id = ++nextId, buyer = buyer, storage = storage, item = item, count = count,
                unitPrice = unit, price = price, created = Find.TickManager.TicksGame, receiptCell = desk.Position,
                delivery = delivery, waitCell = delivery ? WaitAt(buyer) : IntVec3.Invalid };
            orders.Add(order); return order;
        }
        public Job ShoppingJob(Pawn buyer, Func<Thing, bool> qualifies)
        {
            if (!SaleStock.Buyer(buyer) || buyer.CurJobDef == BuyJob || buyer.CurJobDef == WaitJob) return null;
            foreach (var storage in SaleStock.Stores(map).InRandomOrder())
                foreach (var item in SaleStock.Contents(storage).Where(SaleStock.Tradeable).Where(qualifies).InRandomOrder()) {
                    int affordable = (int)Math.Min(WholesaleTrade.Group(buyer) == null ? 10 : 100, WholesaleTrade.Money(buyer) / SaleStock.UnitPrice(item));
                    if (affordable <= 0) continue;
                    var order = Create(buyer, storage, item, WholesaleTrade.Group(buyer) == null ? Rand.RangeInclusive(1, affordable) : affordable);
                    if (order == null) continue;
                    var job = order.delivery ? JobMaker.MakeJob(WaitJob, order.waitCell) : JobMaker.MakeJob(BuyJob, storage);
                    job.count = order.id; return job;
                }
            return null;
        }
        public bool Buy(SaleOrder o)
        {
            if (!Valid(o) || o.delivery || o.buyer.CurJobDef != BuyJob || o.buyer.CurJob.count != o.id ||
                !o.buyer.Position.AdjacentTo8WayOrInside(o.storage) || SaleStock.Space(o.buyer, o.item) < o.count) return false;
            return Settle(o, o.item);
        }
        private bool Settle(SaleOrder o, Thing offered)
        {
            if (WholesaleTrade.Group(o.buyer) != null) return WholesaleTrade.Settle(this, o, offered);
            if (!SaleStock.Suitable(o.buyer, offered) || SaleStock.Space(o.buyer, offered) < o.count) return false;
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
            Thing bought = null; var owner = offered.holdingOwner;
            if (!offered.Spawned && owner != null) owner.TryTransferToContainer(offered, o.buyer.inventory.innerContainer, o.count, out bought, false);
            else if (offered.Spawned) {
                var piece = offered.SplitOff(o.count);
                o.buyer.inventory.innerContainer.TryAdd(piece, false);
                if (o.buyer.inventory.innerContainer.Contains(piece)) bought = piece;
                else if (!piece.Destroyed && !piece.Spawned) o.returnedGoods.TryAdd(piece, false);
            }
            int actual = bought != null && o.buyer.inventory.innerContainer.Contains(bought) ? bought.stackCount : 0;
            if (actual <= 0) { o.cancelled = true; Process(); return false; }
            o.completed = true; o.count = actual; o.price = SaleStock.Price(o.unitPrice, actual);
            o.goods = null;
            o.refund = Math.Max(0, cash.Sum(t => t.stackCount) - o.price);
            RestaurantMeals.MarkSold(o.buyer, bought);
            Process(); return true;
        }
        public SaleOrder ForWorker(Pawn worker)
        {
            if (!WorkerEnabled(worker)) return null;
            return orders.FirstOrDefault(o => o.delivery && o.worker == null && Valid(o) && CanCollect(worker, o.storage, o.item, o.count) &&
                worker.CanReach(o.buyer, PathEndMode.Touch, Danger.None) && ComplexWorkPolicy.Allows(worker, o.waitCell));
        }
        public bool PickUp(SaleOrder o, Pawn worker)
        {
            if (!Valid(o) || o.worker != worker || o.goods != null || !worker.Position.AdjacentTo8WayOrInside(o.storage) ||
                worker.carryTracker.CarriedThing != null || worker.carryTracker.AvailableStackSpace(o.item.def) < o.count) return false;
            if (o.item.Spawned) worker.carryTracker.TryStartCarry(o.item, o.count, false);
            else if (o.item.holdingOwner != null) {
                Thing taken; o.item.holdingOwner.TryTransferToContainer(o.item, worker.carryTracker.innerContainer, o.count, out taken, false);
            }
            o.goods = worker.carryTracker.CarriedThing;
            return o.goods != null && o.goods.stackCount == o.count;
        }
        public bool Deliver(SaleOrder o, Pawn worker)
        {
            return Valid(o) && o.worker == worker && o.goods != null && worker.CurJobDef == DeliverJob && worker.CurJob.count == o.id &&
                o.buyer.Position == o.waitCell && worker.Position.AdjacentTo8WayOrInside(o.buyer) && Settle(o, o.goods);
        }
        public void Cancel(SaleOrder o)
        {
            if (o == null || o.completed) return;
            o.cancelled = true;
            WholesaleTrade.Recover(o);
            // Only take back this order's exact object; never confiscate an
            // unrelated load picked up by the employee's next job.
            if (o.goods != null && o.worker != null && o.worker.carryTracker != null && o.worker.carryTracker.CarriedThing == o.goods) {
                o.worker.carryTracker.innerContainer.TryTransferToContainer(o.goods, o.returnedGoods, o.goods.stackCount, false);
            }
            o.goods = null;
        }
        public void Process()
        {
            foreach (var o in orders.ToArray()) {
                foreach (var thing in o.unplacedGoods.ToArray()) {
                    if (thing == null || thing.Destroyed || thing.Spawned || thing.holdingOwner != null) { o.unplacedGoods.Remove(thing); continue; }
                    o.returnedGoods.TryAdd(thing, false);
                    if (o.returnedGoods.Contains(thing)) o.unplacedGoods.Remove(thing);
                }
                if (!o.completed && !o.cancelled && !Valid(o)) Cancel(o);
                if (o.cancelled) Cancel(o);
                var cash = o.GetDirectlyHeldThings();
                if (o.cancelled) o.refund = cash.Sum(t => t.stackCount);
                if (o.cancelled && o.wholesaleDebits.Count > 0) {
                    WholesaleTrade.Refund(o, map); o.refund = cash.Sum(t => t.stackCount);
                } else if (o.refund > 0 && o.buyer != null && o.buyer.inventory != null && !o.buyer.Destroyed) {
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
                if ((o.cancelled || o.completed) && cash.Count == 0 && o.returnedGoods.Count == 0 && o.wholesaleRecoveries.Count == 0 && o.unplacedGoods.Count == 0) orders.Remove(o);
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
                pawn.Reserve(o.item, job, 1, o.count, null, errorOnFailed);
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
            __result = SaleStock.Buyer(__0) && HotelBillingHooks.Money(__0) > 0 && SaleStock.Stores(__0.Map).Any(s => SaleStock.Public(s) ||
                SaleStock.Contents(s).Any(t => __0.Map.GetComponent<ComplexSales>().Requestable(__0, s, t))) ? 25f : 0f;
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
