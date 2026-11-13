using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    // Original restaurant implementation. Gastronomy/CashRegister code and
    // artwork are not incorporated. Hospitality provides the visiting guests.
    public sealed class Building_ServiceCounter : Building
    {
        public Area serviceArea, foodArea;
        public ThingDef mealDef;
        public int pricePercent = 100, earned;
        public bool open = true;
        public bool Ready { get { return Spawned && open && serviceArea != null && foodArea != null &&
            Map.areaManager.AllAreas.Contains(serviceArea) && Map.areaManager.AllAreas.Contains(foodArea); } }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref serviceArea, "serviceArea"); Scribe_References.Look(ref foodArea, "foodArea");
            Scribe_Defs.Look(ref mealDef, "mealDef"); Scribe_Values.Look(ref pricePercent, "pricePercent", 100);
            Scribe_Values.Look(ref earned, "earned"); Scribe_Values.Look(ref open, "open", true);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (mealDef == null) mealDef = ThingDefOf.MealSimple;
        }
        private Command_Action AreaCommand(bool service)
        {
            return new Command_Action {
                defaultLabel = (service ? "TMM.Restaurant.Tables" : "TMM.Restaurant.Stock").Translate() + ": " +
                    ((service ? serviceArea : foodArea) == null ? "TMM.Restaurant.Unassigned".Translate().ToString() : (service ? serviceArea : foodArea).Label),
                defaultDesc = "TMM.Restaurant.AreasHelp".Translate(),
                icon = ThingDefOf.Table2x2c.uiIcon,
                action = delegate {
                    var options = new List<FloatMenuOption>();
                    options.Add(new FloatMenuOption("TMM.Restaurant.Unassigned".Translate(), delegate { if (service) serviceArea = null; else foodArea = null; }));
                    foreach (var area in Map.areaManager.AllAreas.Where(a => a is Area_Allowed)) {
                        var selected = area;
                        options.Add(new FloatMenuOption(area.Label, delegate { if (service) serviceArea = selected; else foodArea = selected; }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            yield return AreaCommand(true); yield return AreaCommand(false);
            yield return new Command_Toggle { defaultLabel = "TMM.Restaurant.Open".Translate(), defaultDesc = "TMM.Restaurant.OpenHelp".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Restaurant/Restaurant"), isActive = () => open, toggleAction = () => open = !open };
            yield return new Command_Action { defaultLabel = "TMM.Restaurant.Meal".Translate() + ": " + mealDef.LabelCap,
                defaultDesc = "TMM.Restaurant.MenuHelp".Translate(), icon = ContentFinder<Texture2D>.Get("UI/Restaurant/Menu"),
                action = delegate { Find.WindowStack.Add(new FloatMenu(DefDatabase<ThingDef>.AllDefsListForReading.Where(RestaurantMeals.Prepared)
                    .OrderBy(d => d.label).Select(d => new FloatMenuOption(d.LabelCap, () => mealDef = d)).ToList())); } };
            yield return new Command_Action { defaultLabel = "TMM.Restaurant.Price".Translate(pricePercent), defaultDesc = "TMM.Restaurant.PriceHelp".Translate(),
                icon = ThingDefOf.Silver.uiIcon, action = delegate { Find.WindowStack.Add(new FloatMenu(new[] { 0, 50, 75, 100, 125, 150, 200 }
                    .Select(p => new FloatMenuOption(p + "%", () => pricePercent = p)).ToList())); } };
        }
        public override string GetInspectString()
        {
            return base.GetInspectString() + "\n" + "TMM.Restaurant.Inspect".Translate(earned,
                Map.GetComponent<RestaurantService>().orders.Count(o => o.counter == this && !o.delivered && !o.cancelled));
        }
    }

    public sealed class RestaurantOrder : IExposable, IThingHolder
    {
        public Pawn guest, waiter;
        public Building_ServiceCounter counter;
        public Thing food, chair;
        public ThingDef mealDef;
        public int id, price, created;
        public IntVec3 receiptCell;
        public bool delivered, cancelled;
        private ThingOwner<Thing> cash;
        public RestaurantOrder() { cash = new ThingOwner<Thing>(this); }
        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return cash; }
        public void GetChildHolders(List<IThingHolder> holders) { ThingOwnerUtility.AppendThingHoldersFromThings(holders, cash); }
        public void ExposeData()
        {
            Scribe_References.Look(ref guest, "guest"); Scribe_References.Look(ref waiter, "waiter");
            Scribe_References.Look(ref counter, "counter"); Scribe_References.Look(ref food, "food"); Scribe_References.Look(ref chair, "chair");
            Scribe_Defs.Look(ref mealDef, "mealDef");
            Scribe_Values.Look(ref id, "id"); Scribe_Values.Look(ref price, "price"); Scribe_Values.Look(ref created, "created"); Scribe_Values.Look(ref receiptCell, "receiptCell");
            Scribe_Values.Look(ref delivered, "delivered"); Scribe_Values.Look(ref cancelled, "cancelled");
            Scribe_Deep.Look(ref cash, "cash", this);
        }
    }

    public static class RestaurantMeals
    {
        public static JobDef WaitJob { get { return DefDatabase<JobDef>.GetNamed("TMM_WaitForMeal"); } }
        public static JobDef ServeJob { get { return DefDatabase<JobDef>.GetNamed("TMM_ServeMeal"); } }
        private static readonly Type utility = AccessTools.TypeByName("Hospitality.Utilities.GuestUtility");
        private static readonly MethodInfo isGuest = utility == null ? null : AccessTools.Method(utility, "IsGuest");
        private static readonly MethodInfo guestArea = utility == null ? null : AccessTools.Method(utility, "GetGuestArea");
        public static bool Prepared(ThingDef def)
        {
            return def.ingestible != null && !def.IsDrug && def.ingestible.preferability >= FoodPreferability.MealAwful && def.ingestible.preferability <= FoodPreferability.MealLavish &&
                def.GetStatValueAbstract(StatDefOf.Nutrition) >= 0.5f;
        }
        public static bool Guest(Pawn pawn)
        {
            if (isGuest == null || pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.IsPrisoner || pawn.IsSlave || pawn.Faction == Faction.OfPlayer || pawn.InMentalState) return false;
            var lord = pawn.GetLord();
            if (lord == null || lord.CurLordToil == null || lord.CurLordToil.GetType().FullName != "Hospitality.LordToil_VisitPoint") return false;
            var comp = HotelBillingHooks.Guest(pawn);
            return comp != null && (bool)isGuest.Invoke(null, new object[] { pawn }) &&
                (bool)AccessTools.Field(comp.GetType(), "arrived").GetValue(comp) && !(bool)AccessTools.Field(comp.GetType(), "sentAway").GetValue(comp);
        }
        public static bool AllowedSeat(Pawn guest, Thing seat)
        {
            var area = guestArea == null ? null : guestArea.Invoke(null, new object[] { guest }) as Area;
            return area == null || area[seat.Position];
        }
        public static bool Seat(Thing thing, Building_ServiceCounter counter)
        {
            return thing.Spawned && thing.Map == counter.Map && thing.def.building != null && thing.def.building.isSittable && counter.serviceArea[thing.Position] &&
                GenAdj.CellsAdjacentCardinal(thing).Any(c => c.InBounds(counter.Map) && c.GetThingList(counter.Map)
                    .Any(t => t.def.surfaceType == SurfaceType.Eat));
        }
        public static bool Edible(Pawn guest, Thing food)
        {
            return food != null && !food.Destroyed && Prepared(food.def) && !food.IsNotFresh() &&
                (guest.foodRestriction == null || guest.foodRestriction.CurrentFoodPolicy.Allows(food.def)) && FoodUtility.WillEat(guest, food);
        }
        public static int Quote(Building_ServiceCounter counter, Thing food)
        {
            var native = counter.Map.components.FirstOrDefault(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
            bool free = native != null && (bool)AccessTools.Field(native.GetType(), "guestsCanTakeFoodForFree").GetValue(native);
            return free ? 0 : Mathf.Max(0, Mathf.CeilToInt(food.MarketValue * counter.pricePercent / 100f));
        }
        public static void MarkSold(Pawn guest, Thing meal)
        {
            var comp = HotelBillingHooks.Guest(guest);
            var bought = AccessTools.Field(comp.GetType(), "boughtItems").GetValue(comp) as List<int>;
            if (bought != null && !bought.Contains(meal.thingIDNumber)) bought.Add(meal.thingIDNumber);
            var lord = guest.GetLord();
            if (lord != null && lord.CurLordToil != null) {
                var sold = AccessTools.Method(lord.CurLordToil.GetType(), "OnPlayerSoldItem");
                if (sold != null) sold.Invoke(lord.CurLordToil, new object[] { meal });
            }
        }
    }

    public sealed class RestaurantService : MapComponent
    {
        public List<RestaurantOrder> orders = new List<RestaurantOrder>();
        private int nextCheck;
        private int nextOrderId;
        public RestaurantService(Map map) : base(map) { }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Collections.Look(ref orders, "TMM_restaurantOrders", LookMode.Deep);
            Scribe_Values.Look(ref nextOrderId, "TMM_nextRestaurantOrderId");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && orders == null) orders = new List<RestaurantOrder>();
        }
        public RestaurantOrder ForGuest(Pawn pawn) { return orders.FirstOrDefault(o => o.guest == pawn && !o.cancelled && !o.delivered); }
        public RestaurantOrder ById(int id) { return orders.FirstOrDefault(o => o.id == id); }
        public void Cancel(Pawn pawn) { var order = ForGuest(pawn); if (order != null) order.cancelled = true; }
        private bool Valid(RestaurantOrder order)
        {
            return RestaurantMeals.Guest(order.guest) && order.guest.Map == map && order.guest.CurJobDef == RestaurantMeals.WaitJob &&
                order.counter != null && order.counter.Ready && order.counter.Map == map && order.chair != null && order.chair.Spawned &&
                RestaurantMeals.Seat(order.chair, order.counter) && RestaurantMeals.AllowedSeat(order.guest, order.chair) &&
                HasPortion(order) &&
                order.guest.needs.food != null && order.guest.needs.food.CurCategory != HungerCategory.Starving &&
                Find.TickManager.TicksGame - order.created < 6000;
        }
        private bool HasPortion(RestaurantOrder order)
        {
            var carried = order.waiter == null ? null : order.waiter.carryTracker.CarriedThing;
            return (carried != null && carried.def == order.mealDef && order.waiter.Map == map) ||
                (order.food != null && !order.food.Destroyed && order.food.Spawned && order.food.Map == map && order.counter.foodArea[order.food.Position]);
        }
        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame < nextCheck) return;
            nextCheck = Find.TickManager.TicksGame + 250; ProcessOrders(true);
        }
        public void ProcessOrders(bool findCustomers)
        {
            foreach (var order in orders.ToArray())
            {
                if (!order.delivered && !order.cancelled && !Valid(order)) order.cancelled = true;
                if (order.cancelled && order.guest != null && order.guest.inventory != null)
                    // Refunds do not use the engine's returned transfer count;
                    // merging restores a usable currency stack for native beds.
                    order.GetDirectlyHeldThings().TryTransferAllToContainer(order.guest.inventory.innerContainer);
                if (order.cancelled && order.guest == null)
                    order.GetDirectlyHeldThings().TryDropAll(order.receiptCell, map, ThingPlaceMode.Near);
                if (order.delivered) order.GetDirectlyHeldThings().TryDropAll(order.receiptCell, map, ThingPlaceMode.Near);
                if ((order.cancelled || order.delivered) && order.GetDirectlyHeldThings().Count == 0) orders.Remove(order);
            }
            if (!findCustomers) return;
            var counters = map.listerBuildings.allBuildingsColonist.OfType<Building_ServiceCounter>()
                .Where(c => c.Ready).ToArray();
            if (counters.Length == 0) return;
            foreach (var guest in map.mapPawns.AllPawnsSpawned.ToArray())
            {
                if (!RestaurantMeals.Guest(guest) || ForGuest(guest) != null || !guest.Awake() || guest.needs.food == null ||
                    guest.needs.food.CurLevelPercentage >= 0.55f || guest.needs.food.CurCategory == HungerCategory.Starving ||
                    (guest.CurJob != null && !guest.CurJob.def.casualInterruptible)) continue;
                foreach (var counter in counters) if (TryOrder(guest, counter) != null) break;
            }
        }
        public RestaurantOrder TryOrder(Pawn guest, Building_ServiceCounter counter)
        {
            if (!RestaurantMeals.Guest(guest) || guest.Map != map || ForGuest(guest) != null || counter == null || !counter.Ready || counter.Map != map) return null;
            var seat = map.listerBuildings.allBuildingsColonist.FirstOrDefault(t => RestaurantMeals.Seat(t, counter) && RestaurantMeals.AllowedSeat(guest, t) &&
                !orders.Any(o => !o.cancelled && !o.delivered && o.chair == t) && guest.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some));
            if (seat == null) return null;
            var food = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree).FirstOrDefault(t => t.Spawned && t.def == counter.mealDef && counter.foodArea[t.Position] &&
                RestaurantMeals.Edible(guest, t) && !orders.Any(o => !o.delivered && !o.cancelled && o.food == t) && HotelBillingHooks.Money(guest) >= RestaurantMeals.Quote(counter, t));
            if (food == null) return null;
            var order = new RestaurantOrder { id = ++nextOrderId, guest = guest, counter = counter, chair = seat, food = food, mealDef = food.def,
                price = RestaurantMeals.Quote(counter, food), created = Find.TickManager.TicksGame, receiptCell = counter.Position };
            orders.Add(order);
            var wait = JobMaker.MakeJob(RestaurantMeals.WaitJob, seat); wait.count = order.id;
            guest.jobs.StartJob(wait, JobCondition.InterruptForced);
            return order;
        }
        public RestaurantOrder ForWaiter(Pawn waiter, Building_ServiceCounter counter)
        {
            return orders.FirstOrDefault(o => o.counter == counter && !o.cancelled && !o.delivered && Valid(o) && o.guest.Position == o.chair.Position &&
                (o.waiter == null || !o.waiter.Spawned || o.waiter.CurJobDef != RestaurantMeals.ServeJob) &&
                o.food != null && o.food.Spawned && !o.food.IsForbidden(waiter) && waiter.CanReserveAndReach(o.food, PathEndMode.Touch, Danger.Some) &&
                waiter.CanReach(o.guest, PathEndMode.Touch, Danger.Some));
        }
        public bool Deliver(Pawn waiter, Pawn guest)
        {
            var order = ForGuest(guest);
            var meal = waiter.carryTracker.CarriedThing;
            if (order == null || order.waiter != waiter || waiter.CurJob == null || waiter.CurJob.count != order.id || !Valid(order) || waiter.Map != map || guest.Position != order.chair.Position || meal == null || meal.def != order.mealDef ||
                !RestaurantMeals.Edible(guest, meal) || !waiter.Position.AdjacentTo8WayOrInside(guest.Position)) return false;
            var cash = order.GetDirectlyHeldThings();
            int required = order.price - cash.Sum(t => t.stackCount);
            if (HotelBillingHooks.Money(guest) < required) return false;
            foreach (var silver in guest.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).ToArray())
            {
                if (required <= 0) break;
                // 1.7 can return zero after a successful merge because the
                // transferred stack was consumed. Keep transfer stacks separate
                // so the reported count and resulting meal reference stay valid.
                required -= guest.inventory.innerContainer.TryTransferToContainer(silver, cash, Math.Min(required, silver.stackCount), false);
            }
            if (required != 0) { order.cancelled = true; ProcessOrders(false); return false; }
            Thing delivered;
            if (waiter.carryTracker.innerContainer.TryTransferToContainer(meal, guest.inventory.innerContainer, 1, out delivered, false) != 1)
            { order.cancelled = true; ProcessOrders(false); return false; }
            RestaurantMeals.MarkSold(guest, delivered);
            // The carrier's reservation can follow the split-off portion into
            // the buyer's inventory. Release it before Ingest reserves the meal.
            map.reservationManager.Release(delivered, waiter, waiter.CurJob);
            order.delivered = true; order.counter.earned += order.price;
            ProcessOrders(false);
            var eat = JobMaker.MakeJob(JobDefOf.Ingest, delivered); eat.count = 1;
            guest.jobs.StartJob(eat, JobCondition.InterruptForced);
            return true;
        }
    }

    public sealed class WorkGiver_Restaurant : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest { get { return ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("TMM_ServiceCounter")); } }
        public override PathEndMode PathEndMode { get { return PathEndMode.Touch; } }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return !pawn.Drafted && !t.IsForbidden(pawn) && pawn.Map.GetComponent<RestaurantService>().ForWaiter(pawn, t as Building_ServiceCounter) != null;
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var order = pawn.Map.GetComponent<RestaurantService>().ForWaiter(pawn, t as Building_ServiceCounter);
            if (order == null) return null;
            var job = JobMaker.MakeJob(RestaurantMeals.ServeJob, order.food, order.guest, t); job.count = order.id; return job;
        }
    }
    public sealed class JobDriver_WaitForMeal : JobDriver
    {
        private RestaurantOrder Order { get { return pawn.Map == null ? null : pawn.Map.GetComponent<RestaurantService>().ById(job.count); } }
        public override bool TryMakePreToilReservations(bool errorOnFailed) { return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed); }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            // SetupToils runs before pawns are spawned when loading. Resolve
            // the saved order only in callbacks, after the map is available.
            AddFinishAction(condition => { var order = Order; if (order != null && !order.delivered) order.cancelled = true; });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            var wait = new Toil { defaultCompleteMode = ToilCompleteMode.Never };
            wait.tickAction = delegate { var order = Order; if (order == null || order.cancelled || order.delivered) EndJobWith(JobCondition.Succeeded); };
            yield return wait;
        }
    }
    public sealed class JobDriver_ServeMeal : JobDriver
    {
        private RestaurantOrder Order { get { return pawn.Map == null ? null : pawn.Map.GetComponent<RestaurantService>().ById(job.count); } }
        public override bool TryMakePreToilReservations(bool errorOnFailed) { return pawn.Reserve(job.targetA, job, 1, 1, null, errorOnFailed); }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(delegate { var order = Order; if (order != null && !order.delivered) order.cancelled = true; });
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B); this.FailOnDespawnedNullOrForbidden(TargetIndex.C);
            this.FailOn(() => Order == null || Order.cancelled || Order.delivered);
            var go = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            go.AddPreInitAction(delegate { var order = Order; if (order != null) order.waiter = pawn; });
            yield return go;
            var take = new Toil { defaultCompleteMode = ToilCompleteMode.Instant };
            take.initAction = delegate { if (pawn.carryTracker.TryStartCarry(job.targetA.Thing, 1) != 1) EndJobWith(JobCondition.Incompletable); };
            yield return take;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            var serve = new Toil { defaultCompleteMode = ToilCompleteMode.Instant };
            serve.initAction = delegate { if (!pawn.Map.GetComponent<RestaurantService>().Deliver(pawn, job.targetB.Pawn)) EndJobWith(JobCondition.Incompletable); };
            yield return serve;
        }
    }
}
