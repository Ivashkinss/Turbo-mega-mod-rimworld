using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public sealed class WorkGiver_ComplexSales : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest { get { return ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk")); } }
        public override PathEndMode PathEndMode { get { return PathEndMode.Touch; } }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return !t.IsForbidden(pawn) && pawn.Map.GetComponent<ComplexSales>().ForWorker(pawn) != null;
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var o = pawn.Map.GetComponent<ComplexSales>().ForWorker(pawn);
            if (o == null) return null;
            var job = JobMaker.MakeJob(ComplexSales.DeliverJob, o.storage, o.buyer); job.count = o.id; return job;
        }
    }
    public sealed class JobDriver_WaitForGoods : JobDriver
    {
        private ComplexSales Sales { get { return pawn.Map == null ? null : pawn.Map.GetComponent<ComplexSales>(); } }
        private SaleOrder Order { get { return Sales == null ? null : Sales.ById(job.count); } }
        public override bool TryMakePreToilReservations(bool errorOnFailed) { return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed); }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(delegate { if (Sales != null) Sales.Cancel(Order); });
            this.FailOn(() => Order == null || Order.cancelled || !Order.completed && !Sales.Valid(Order));
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            var wait = new Toil { defaultCompleteMode = ToilCompleteMode.Never };
            wait.tickAction = delegate { if (Order == null || Order.completed || Order.cancelled) EndJobWith(JobCondition.Succeeded); };
            yield return wait;
        }
    }
    public sealed class JobDriver_DeliverGoods : JobDriver
    {
        private ComplexSales Sales { get { return pawn.Map == null ? null : pawn.Map.GetComponent<ComplexSales>(); } }
        private SaleOrder Order { get { return Sales == null ? null : Sales.ById(job.count); } }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var o = Order;
            if (o == null || o.worker != null && o.worker != pawn || !Sales.Valid(o) || !Sales.CanCollect(pawn, o.storage, o.item, o.count)) return false;
            if (!pawn.Reserve(o.storage, job, 1, -1, null, errorOnFailed) || !pawn.Reserve(o.item, job, 1, o.count, null, errorOnFailed)) return false;
            o.worker = pawn; return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Resolve references inside callbacks: SetupToils also runs before
            // pawns have a map during save restoration.
            AddFinishAction(delegate { if (Sales != null) Sales.Cancel(Order); });
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => Order == null || !Sales.Valid(Order) || Order.worker != pawn);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            var take = new Toil { defaultCompleteMode = ToilCompleteMode.Instant };
            take.initAction = delegate { if (!Sales.PickUp(Order, pawn)) EndJobWith(JobCondition.Incompletable); };
            yield return take;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            var handover = new Toil { defaultCompleteMode = ToilCompleteMode.Instant };
            handover.initAction = delegate { if (!Sales.Deliver(Order, pawn)) EndJobWith(JobCondition.Incompletable); };
            yield return handover;
        }
    }
}
