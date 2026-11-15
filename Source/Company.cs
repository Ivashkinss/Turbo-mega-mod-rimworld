using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    // Ordinary paid conversation, without beds, romance or intimate actions.
    public sealed class CompanyWallet : IExposable, IThingHolder
    {
        public Pawn pawn;
        public int earned;
        private ThingOwner<Thing> cash;
        public CompanyWallet() { cash = new ThingOwner<Thing>(this); }
        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return cash; }
        public void GetChildHolders(List<IThingHolder> holders) { ThingOwnerUtility.AppendThingHoldersFromThings(holders, cash); }
        public int Balance { get { return cash.Sum(t => t.stackCount); } }
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn"); Scribe_Values.Look(ref earned, "earned");
            Scribe_Deep.Look(ref cash, "cash", this);
        }
    }

    public sealed class CompanyAccounts : GameComponent
    {
        public List<CompanyWallet> wallets = new List<CompanyWallet>();
        public CompanyAccounts(Game game) { }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Collections.Look(ref wallets, "TMM_companyWallets", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && wallets == null) wallets = new List<CompanyWallet>();
        }
        public CompanyWallet Wallet(Pawn pawn)
        {
            var wallet = wallets.FirstOrDefault(w => w.pawn == pawn);
            if (wallet == null) { wallet = new CompanyWallet { pawn = pawn }; wallets.Add(wallet); }
            return wallet;
        }
        public override void GameComponentTick()
        {
            if(Find.TickManager.TicksGame%250!=0)return;
            // Preserve silver from development saves made with the former
            // personal-wallet model. New payments go straight to the colony.
            foreach(var wallet in wallets.Where(w=>w.Balance>0&&w.pawn!=null&&w.pawn.MapHeld!=null)){
                var map=wallet.pawn.MapHeld;var desk=ComplexLayout.Desk(map);if(desk==null)continue;
                int before=wallet.Balance;wallet.GetDirectlyHeldThings().TryDropAll(desk.Position,map,ThingPlaceMode.Near);
                int received=before-wallet.Balance;
                if(received>0)ComplexWorkPolicy.Record(map,desk.Position,"company",received);
            }
        }
    }

    public sealed class CompanyDecision : IExposable
    {
        public Pawn guest, employee;
        public int until;
        public bool agreed;
        public void ExposeData()
        {
            Scribe_References.Look(ref guest, "guest"); Scribe_References.Look(ref employee, "employee");
            Scribe_Values.Look(ref until, "until"); Scribe_Values.Look(ref agreed, "agreed");
        }
    }

    public sealed class CompanyOrder : IExposable, IThingHolder
    {
        public GuestReceiptLink dossier;
        public Pawn guest, employee;
        public Building_ComplexDesk desk;
        public Building_Bed roomBed;
        public IntVec3 guestCell, employeeCell, refundCell;
        public int id, price, created, talked;
        public bool cancelled, completed;
        private ThingOwner<Thing> cash;
        public CompanyOrder() { cash = new ThingOwner<Thing>(this); }
        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return cash; }
        public void GetChildHolders(List<IThingHolder> holders) { ThingOwnerUtility.AppendThingHoldersFromThings(holders, cash); }
        public void ExposeData()
        {
            Scribe_References.Look(ref guest, "guest"); Scribe_References.Look(ref employee, "employee");
            Scribe_Deep.Look(ref dossier,"dossier");
            Scribe_References.Look(ref desk, "desk"); Scribe_References.Look(ref roomBed, "roomBed");
            Scribe_Values.Look(ref guestCell, "guestCell"); Scribe_Values.Look(ref employeeCell, "employeeCell");
            Scribe_Values.Look(ref refundCell, "refundCell"); Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref price, "price"); Scribe_Values.Look(ref created, "created"); Scribe_Values.Look(ref talked, "talked");
            Scribe_Values.Look(ref cancelled, "cancelled"); Scribe_Values.Look(ref completed, "completed");
            Scribe_Deep.Look(ref cash, "cash", this);
        }
    }

    public static class CompanyRules
    {
        public const int Duration = 2500, DecisionTicks = 60000;
        public static WorkTypeDef Work { get { return DefDatabase<WorkTypeDef>.GetNamed("TMM_Company"); } }
        public static JobDef GuestJob { get { return DefDatabase<JobDef>.GetNamed("TMM_CompanyGuest"); } }
        public static JobDef EmployeeJob { get { return DefDatabase<JobDef>.GetNamed("TMM_CompanyEmployee"); } }
        public static bool Fit(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed && !pawn.InMentalState && !pawn.Drafted && pawn.Awake() &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) &&
                (pawn.needs.rest == null || pawn.needs.rest.CurLevelPercentage >= 0.3f) &&
                (pawn.needs.food == null || pawn.needs.food.CurLevelPercentage >= 0.3f) &&
                (pawn.needs.mood == null || pawn.needs.mood.CurLevelPercentage >= 0.25f);
        }
        public static bool Participates(Pawn pawn, Building_ComplexDesk desk)
        {
            var employee = desk == null ? null : desk.Employee(pawn);
            return pawn != null && desk != null && desk.Active && pawn.Map == desk.Map &&
                (pawn.IsColonist || pawn.IsSlaveOfColony) && !pawn.IsPrisoner &&
                employee != null && employee.company && employee.mode != ComplexWorkMode.ColonyOnly &&
                pawn.workSettings != null && !pawn.WorkTypeIsDisabled(Work) && pawn.workSettings.GetPriority(Work) > 0;
        }
        public static float Willingness(Pawn pawn)
        {
            float mood = pawn.needs.mood == null ? 0.5f : pawn.needs.mood.CurLevelPercentage;
            float rest = pawn.needs.rest == null ? 1f : pawn.needs.rest.CurLevelPercentage;
            float chance = 0.2f + mood * 0.45f + rest * 0.2f - pawn.health.hediffSet.PainTotal * 0.3f;
            var traits = pawn.story == null ? null : pawn.story.traits;
            var kind = DefDatabase<TraitDef>.GetNamedSilentFail("Kind");
            var abrasive = DefDatabase<TraitDef>.GetNamedSilentFail("Abrasive");
            if (traits != null && kind != null && traits.HasTrait(kind)) chance += 0.1f;
            if (traits != null && abrasive != null && traits.HasTrait(abrasive)) chance -= 0.15f;
            return Mathf.Clamp(chance, 0.05f, 0.95f);
        }
        public static bool UrgentJob(Pawn pawn)
        {
            var def = pawn.CurJobDef;
            return def != null && (def == JobDefOf.BeatFire || def == JobDefOf.TendPatient || def == JobDefOf.AttackMelee || def == JobDefOf.AttackStatic ||
                def.driverClass == typeof(JobDriver_DoBill) || def.defName == "Rescue" || def.defName == "CarryToCryptosleepCasket");
        }
        public static int Quote(Pawn pawn, int percent)
        {
            int social = pawn.skills == null ? 0 : pawn.skills.GetSkill(SkillDefOf.Social).Level;
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(20, 40 + social * 3 + pawn.GetStatValue(StatDefOf.PawnBeauty) * 10) * percent / 100f));
        }
        public static Building_Bed GuestRoom(Pawn pawn)
        {
            var bed = HotelBillingHooks.Bed(pawn);
            if (bed == null || !bed.Spawned || bed.Medical || bed.Map != pawn.Map) return null;
            var room = bed.GetRoom();
            if (room == null || room.PsychologicallyOutdoors || room.OpenRoofCount > 0) return null;
            // A shared dormitory is not a private guest room. No bed is used
            // by either conversation job; the bed only identifies the room.
            if (room.ContainedBeds.Any(b => b != bed)) return null;
            return bed;
        }
        public static bool FindCells(Pawn guest, Pawn employee, Building_ComplexDesk desk, Building_Bed bed, out IntVec3 a, out IntVec3 b)
        {
            a = b = IntVec3.Invalid;
            var room = bed.GetRoom();
            foreach (var cell in room.Cells.OrderBy(c => c.DistanceToSquared(bed.Position))) {
                if (!cell.Standable(guest.Map) || cell.GetEdifice(guest.Map) != null || !desk.area[cell] || !RestaurantMeals.AllowedCell(guest, cell) ||
                    !guest.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.None)) continue;
                foreach (var next in GenAdj.CellsAdjacentCardinal(cell, Rot4.North, IntVec2.One)) {
                    if (!next.InBounds(guest.Map) || next.GetRoom(guest.Map) != room || !next.Standable(guest.Map) || next.GetEdifice(guest.Map) != null || !desk.area[next] ||
                        !ComplexWorkPolicy.Allows(employee, next) || !ForbidUtility.InAllowedArea(next, employee) ||
                        !employee.CanReserveAndReach(next, PathEndMode.OnCell, Danger.None)) continue;
                    a = cell; b = next; return true;
                }
            }
            return false;
        }
    }

    public sealed class CompanyService : MapComponent
    {
        public List<CompanyOrder> orders = new List<CompanyOrder>();
        public List<CompanyDecision> decisions = new List<CompanyDecision>();
        private int nextId, nextCheck, nextOffer;
        public CompanyService(Map map) : base(map) { }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Collections.Look(ref orders, "TMM_companyOrders", LookMode.Deep);
            Scribe_Collections.Look(ref decisions, "TMM_companyDecisions", LookMode.Deep);
            Scribe_Values.Look(ref nextId, "TMM_companyNextId"); Scribe_Values.Look(ref nextOffer, "TMM_companyNextOffer");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) { if (orders == null) orders = new List<CompanyOrder>(); if (decisions == null) decisions = new List<CompanyDecision>(); }
        }
        public CompanyOrder ById(int id) { return orders.FirstOrDefault(o => o.id == id); }
        public CompanyOrder ForPawn(Pawn pawn) { return orders.FirstOrDefault(o => !o.cancelled && !o.completed && (o.guest == pawn || o.employee == pawn)); }
        public bool Consent(Pawn guest, Pawn employee)
        {
            int tick = Find.TickManager.TicksGame;
            var existing = decisions.FirstOrDefault(d => d.guest == guest && d.employee == employee && d.until > tick);
            if (existing != null) return existing.agreed;
            bool agreed = CompanyRules.Fit(employee) && Rand.Chance(CompanyRules.Willingness(employee));
            decisions.Add(new CompanyDecision { guest = guest, employee = employee, agreed = agreed, until = tick + CompanyRules.DecisionTicks });
            return agreed;
        }
        public CompanyOrder Offer(Pawn guest, Pawn employee)
        {
            var desk = map.GetComponent<ComplexMap>().desk;
            if (desk == null || !desk.companyOpen || !CompanyRules.Participates(employee, desk) || !CompanyRules.Fit(employee) || CompanyRules.UrgentJob(employee) || !CompanyRules.Fit(guest) ||
                !RestaurantMeals.Guest(guest) || guest.Map != map || ForPawn(guest) != null || ForPawn(employee) != null || Clinic.ActivePatient(guest) ||
                (guest.CurJob != null && !guest.CurJob.def.casualInterruptible)) return null;
            int tick = Find.TickManager.TicksGame;
            if (decisions.Any(d => d.guest == guest && d.until > tick)) return null;
            var bed = CompanyRules.GuestRoom(guest); IntVec3 a, b;
            if (bed == null || !CompanyRules.FindCells(guest, employee, desk, bed, out a, out b)) return null;
            int price = CompanyRules.Quote(employee, desk.companyPercent);
            if (HotelBillingHooks.Money(guest) < price || !Consent(guest, employee)) return null;
            var order = new CompanyOrder { id = ++nextId, guest = guest, employee = employee, desk = desk, roomBed = bed,
                guestCell = a, employeeCell = b, refundCell = desk.Position, price = price, created = tick, dossier=GuestDossiers.Capture(guest,map) };
            orders.Add(order);
            int remaining = price;
            foreach (var silver in guest.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).ToArray()) {
                remaining -= guest.inventory.innerContainer.TryTransferToContainer(silver, order.GetDirectlyHeldThings(), Math.Min(remaining, silver.stackCount), false);
                if (remaining == 0) break;
            }
            if (remaining != 0) { order.cancelled = true; Process(); return null; }
            var wait = JobMaker.MakeJob(CompanyRules.GuestJob, a, employee); wait.count = order.id;
            guest.jobs.StartJob(wait, JobCondition.InterruptForced);
            return order;
        }
        public bool Valid(CompanyOrder order)
        {
            if (order == null || order.completed || order.cancelled || order.desk == null || !order.desk.Active || !order.desk.companyOpen ||
                !CompanyRules.Participates(order.employee, order.desk) || !CompanyRules.Fit(order.employee) || !CompanyRules.Fit(order.guest) ||
                !RestaurantMeals.Guest(order.guest) || order.guest.Map != map || Clinic.ActivePatient(order.guest) ||
                order.guest.CurJobDef != CompanyRules.GuestJob || order.guest.CurJob.count != order.id ||
                CompanyRules.GuestRoom(order.guest) != order.roomBed || Find.TickManager.TicksGame - order.created > 10000) return false;
            var room = order.roomBed.GetRoom();
            return order.guestCell.InBounds(map) && order.employeeCell.InBounds(map) && order.guestCell.Standable(map) && order.employeeCell.Standable(map) &&
                order.guestCell.GetEdifice(map) == null && order.employeeCell.GetEdifice(map) == null &&
                order.guestCell.GetRoom(map) == room && order.employeeCell.GetRoom(map) == room &&
                order.desk.area[order.guestCell] && order.desk.area[order.employeeCell] && RestaurantMeals.AllowedCell(order.guest, order.guestCell) &&
                ComplexWorkPolicy.Allows(order.employee, order.employeeCell) && ForbidUtility.InAllowedArea(order.employeeCell, order.employee);
        }
        public CompanyOrder ForEmployee(Pawn employee, Building_ComplexDesk desk)
        {
            return orders.FirstOrDefault(o => o.employee == employee && o.desk == desk && Valid(o) &&
                employee.CanReserveAndReach(o.employeeCell, PathEndMode.OnCell, Danger.None));
        }
        public void Cancel(Pawn pawn) { var order = ForPawn(pawn); if (order != null) order.cancelled = true; }
        public bool Finish(CompanyOrder order)
        {
            if (!Valid(order) || order.talked < CompanyRules.Duration || order.employee.Position != order.employeeCell || order.guest.Position != order.guestCell ||
                order.employee.CurJobDef != CompanyRules.EmployeeJob || order.employee.CurJob.count != order.id) return false;
            order.completed = true;
            var thought = DefDatabase<ThoughtDef>.GetNamed("TMM_GoodCompany");
            if (order.guest.needs.mood != null) order.guest.needs.mood.thoughts.memories.TryGainMemory(thought);
            if (order.employee.needs.mood != null) order.employee.needs.mood.thoughts.memories.TryGainMemory(thought);
            Process(); return true;
        }
        public void Process()
        {
            foreach (var order in orders.ToArray()) {
                if (!order.completed && !order.cancelled && !Valid(order)) order.cancelled = true;
                var cash = order.GetDirectlyHeldThings();
                if (order.completed) {
                    var wallet = Current.Game.GetComponent<CompanyAccounts>().Wallet(order.employee);
                    int before = cash.Sum(t => t.stackCount);
                    cash.TryDropAll(order.refundCell,map,ThingPlaceMode.Near);
                    int received=before-cash.Sum(t=>t.stackCount);
                    if(received>0){
                        wallet.earned+=received;
                        ComplexWorkPolicy.Record(map,order.refundCell,"company",received);
                        if(order.dossier==null)order.dossier=GuestDossiers.Capture(order.guest,map);
                        if(order.dossier!=null)order.dossier.Received("company",received,"company:"+map.uniqueID+":"+order.id+":"+before);
                        Current.Game.GetComponent<WorkBuyouts>().Credit(order.employee,"company:"+map.uniqueID+":"+order.id+":"+before,received,false);
                    }
                } else if (order.cancelled) {
                    if (order.guest != null && order.guest.inventory != null) cash.TryTransferAllToContainer(order.guest.inventory.innerContainer);
                    else cash.TryDropAll(order.refundCell, map, ThingPlaceMode.Near);
                }
                if ((order.cancelled || order.completed) && cash.Count == 0) orders.Remove(order);
            }
        }
        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick < nextCheck) return;
            nextCheck = tick + 120; Process();
            decisions.RemoveAll(d => d.until <= tick || d.guest == null || d.employee == null);
            if (tick < nextOffer) return;
            nextOffer = tick + 1000;
            var desk = map.GetComponent<ComplexMap>().desk;
            if (desk == null || !desk.Active || !desk.companyOpen) return;
            var employees = desk.employees.Where(e => CompanyRules.Participates(e.pawn, desk) && CompanyRules.Fit(e.pawn)).Select(e => e.pawn).ToArray();
            if (employees.Length == 0) return;
            foreach (var guest in map.mapPawns.AllPawnsSpawned.ToArray()) {
                if (!RestaurantMeals.Guest(guest) || !CompanyRules.Fit(guest) || guest.needs.joy == null || guest.needs.joy.CurLevelPercentage >= 0.65f || !Rand.Chance(0.05f)) continue;
                Offer(guest, employees.RandomElement());
            }
        }
    }

    public sealed class WorkGiver_Company : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest { get { return ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk")); } }
        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return !pawn.Drafted && !thing.IsForbidden(pawn) && pawn.Map.GetComponent<CompanyService>().ForEmployee(pawn, thing as Building_ComplexDesk) != null;
        }
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            var order = pawn.Map.GetComponent<CompanyService>().ForEmployee(pawn, thing as Building_ComplexDesk);
            if (order == null) return null;
            var job = JobMaker.MakeJob(CompanyRules.EmployeeJob, order.employeeCell, order.guest); job.count = order.id; return job;
        }
    }

    public abstract class JobDriver_Company : JobDriver
    {
        protected abstract bool Employee { get; }
        private CompanyOrder Order { get { return pawn.Map == null ? null : pawn.Map.GetComponent<CompanyService>().ById(job.count); } }
        public override bool TryMakePreToilReservations(bool errorOnFailed) { return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed); }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(condition => { var order = Order; if (order != null && !order.completed) order.cancelled = true; });
            this.FailOnDespawnedOrNull(TargetIndex.B);
            this.FailOn(() => Order == null || !pawn.Map.GetComponent<CompanyService>().Valid(Order));
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            var chat = new Toil { defaultCompleteMode = ToilCompleteMode.Never };
            chat.tickAction = delegate {
                var order = Order;
                if (order == null) { EndJobWith(JobCondition.Succeeded); return; }
                var other = Employee ? order.guest : order.employee;
                pawn.rotationTracker.FaceCell(other.Position);
                if (!Employee || order.employee.Position != order.employeeCell || order.guest.Position != order.guestCell || other.CurJobDef != CompanyRules.GuestJob) return;
                order.talked++;
                if (order.employee.needs.joy != null) order.employee.needs.joy.GainJoy(0.00012f, JoyKindDefOf.Social);
                if (order.guest.needs.joy != null) order.guest.needs.joy.GainJoy(0.00012f, JoyKindDefOf.Social);
                if (order.talked % 250 == 0) MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TMM.Company.Chat".Translate(), Color.white);
                if (order.talked >= CompanyRules.Duration && pawn.Map.GetComponent<CompanyService>().Finish(order)) EndJobWith(JobCondition.Succeeded);
            };
            yield return chat;
        }
    }
    public sealed class JobDriver_CompanyGuest : JobDriver_Company { protected override bool Employee { get { return false; } } }
    public sealed class JobDriver_CompanyEmployee : JobDriver_Company { protected override bool Employee { get { return true; } } }
}
