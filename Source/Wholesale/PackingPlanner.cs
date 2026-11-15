using System;
using System.Collections.Generic;
using System.Linq;

namespace TurboMegaMod.Wholesale
{
    // Pure planning only: no ThingOwner mutations and no invented currency.
    // Compiled by test_wholesale_planner.ps1. The current game assembly does
    // not include this directory until a real wholesale service consumes it.
    public sealed class CargoCarrier
    {
        public string Id;
        public bool Eligible;
        public decimal Capacity, CurrentMass;
        public int Silver;
    }

    public sealed class PackingRequest
    {
        public int Count, StockCount, ReservedCount;
        public decimal UnitMarketValue, UnitMass, SilverUnitMass;
        public IList<CargoCarrier> Carriers;
    }

    public sealed class CargoAllocation
    {
        public string CarrierId;
        public int SilverDebit, GoodsCount;
        public decimal FinalMass;
    }

    public enum PackingStatus { Invalid, InsufficientStock, InsufficientFunds, InsufficientSpace, Ready }

    public sealed class PackingPlan
    {
        public PackingStatus Status;
        public int Quote;
        public readonly List<CargoAllocation> Allocations = new List<CargoAllocation>();
    }

    public static class PackingPlanner
    {
        // Deterministic debit and packing order. This deliberately does not
        // claim to find every feasible redistribution across all carriers.
        public static PackingPlan Create(PackingRequest request)
        {
            var result = new PackingPlan { Status = PackingStatus.Invalid };
            if (request == null || request.Count <= 0 || request.StockCount < 0 || request.ReservedCount < 0 ||
                request.UnitMarketValue <= 0 || request.UnitMass < 0 || request.SilverUnitMass <= 0 || request.Carriers == null) return result;
            try {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var c in request.Carriers) {
                    if (c == null || string.IsNullOrEmpty(c.Id) || !ids.Add(c.Id) || c.Capacity < 0 || c.CurrentMass < 0 || c.Silver < 0 ||
                        c.CurrentMass < c.Silver * request.SilverUnitMass) return result;
                }
                decimal quote = decimal.Ceiling(request.Count * request.UnitMarketValue * 0.55m);
                if (quote <= 0 || quote > int.MaxValue) return result;
                result.Quote = (int)quote;
                if (request.Count > Math.Max(0, request.StockCount - request.ReservedCount)) {
                    result.Status = PackingStatus.InsufficientStock; return result;
                }
                var carriers = request.Carriers.OrderBy(c => c.Id, StringComparer.Ordinal).ToArray();
                long funds = 0;
                foreach (var c in carriers) if (c.Eligible) funds = checked(funds + c.Silver);
                if (funds < result.Quote) { result.Status = PackingStatus.InsufficientFunds; return result; }

                int unpaid = result.Quote, unpacked = request.Count;
                foreach (var c in carriers) {
                    int debit = c.Eligible ? Math.Min(unpaid, c.Silver) : 0;
                    unpaid -= debit;
                    decimal massAfterPayment = c.CurrentMass - debit * request.SilverUnitMass;
                    int count = 0;
                    if (c.Eligible && unpacked > 0) {
                        decimal free = Math.Max(0m, c.Capacity - massAfterPayment);
                        // Compare before division: very small positive item
                        // masses need not overflow a huge free/unit quotient.
                        count = request.UnitMass == 0 || free >= request.UnitMass * unpacked ? unpacked :
                            (int)decimal.Floor(free / request.UnitMass);
                    }
                    unpacked -= count;
                    result.Allocations.Add(new CargoAllocation { CarrierId = c.Id, SilverDebit = debit, GoodsCount = count,
                        FinalMass = massAfterPayment + count * request.UnitMass });
                }
                if (unpacked != 0) {
                    result.Status = PackingStatus.InsufficientSpace;
                    result.Allocations.Clear(); // Never expose a partial plan as a completed transaction.
                    return result;
                }
                result.Status = PackingStatus.Ready;
                return result;
            } catch (OverflowException) {
                // Runtime adapters must also reject non-finite floats before
                // converting them to decimal. Decimal itself has no NaN/Inf.
                return new PackingPlan { Status = PackingStatus.Invalid };
            }
        }
    }
}
