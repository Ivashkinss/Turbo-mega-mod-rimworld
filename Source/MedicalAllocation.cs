using System;
using System.Collections.Generic;
using System.Linq;

namespace TurboMegaMod
{
    // Pure arithmetic shared by the runtime ledger and standalone tests.
    public sealed class MedicalClaim
    {
        public string workerId;
        public bool consumables;
        public decimal weight;
    }
    public sealed class MedicalShare
    {
        public string workerId;
        public bool consumables;
        public int coins;
    }
    public enum MedicalPaymentStatus { Accepted, Duplicate, Zero, Invalid }
    public sealed class MedicalPayment
    {
        public string token;
        public int paid;
        public List<MedicalShare> shares = new List<MedicalShare>();
        public List<string> processed = new List<string>();
        public MedicalPaymentStatus Accept(string receipt, string subreceipt, int coins, out List<MedicalShare> portions)
        {
            portions = new List<MedicalShare>();
            if (receipt != token || string.IsNullOrEmpty(subreceipt)) return MedicalPaymentStatus.Invalid;
            if (processed.Contains(subreceipt)) return MedicalPaymentStatus.Duplicate;
            long total = shares.Sum(s => (long)s.coins);
            if (coins < 0 || paid < 0 || shares.Any(s => s.coins < 0) || paid > total || coins > total - paid) return MedicalPaymentStatus.Invalid;
            if (coins == 0) return MedicalPaymentStatus.Zero;
            int start = 0;
            foreach (var share in shares) {
                int end = checked(start + share.coins);
                int take = Math.Max(0, Math.Min(end, paid + coins) - Math.Max(start, paid));
                if (take > 0) portions.Add(new MedicalShare { workerId = share.workerId, consumables = share.consumables, coins = take });
                start = end;
            }
            paid += coins; processed.Add(subreceipt);
            return MedicalPaymentStatus.Accepted;
        }
    }
    public static class MedicalAllocation
    {
        private sealed class Quota
        {
            public string id;
            public bool consumables;
            public decimal weight, remainder;
            public int coins;
        }
        private static IOrderedEnumerable<Quota> Stable(IEnumerable<Quota> values)
        {
            return values.OrderBy(q => q.id == null ? 1 : 0).ThenBy(q => q.id, StringComparer.Ordinal).ThenBy(q => q.consumables);
        }
        public static List<MedicalShare> Freeze(int coins, IEnumerable<MedicalClaim> claims)
        {
            if (coins < 0) throw new ArgumentOutOfRangeException("coins");
            var groups = (claims ?? Enumerable.Empty<MedicalClaim>()).Where(c => c != null).ToArray();
            if (groups.Any(c => c.weight < 0)) throw new ArgumentOutOfRangeException("claims");
            if (coins == 0) return new List<MedicalShare>();
            var quotas = groups.Where(c => c.weight > 0).GroupBy(c => new { c.workerId, c.consumables })
                .Select(g => new Quota { id = g.Key.workerId, consumables = g.Key.consumables, weight = g.Sum(c => c.weight) }).ToList();
            if (quotas.Count == 0) return new List<MedicalShare> { new MedicalShare { coins = coins } };
            decimal sum = quotas.Sum(q => q.weight);
            foreach (var q in quotas) {
                // Division first also keeps large valid decimal weights away
                // from an unnecessary bill*weight overflow.
                decimal exact = coins * (q.weight / sum);
                q.coins = (int)decimal.Floor(exact); q.remainder = exact - q.coins;
            }
            int rest = coins - quotas.Sum(q => q.coins);
            var ranking = Stable(quotas).OrderByDescending(q => q.remainder).ToArray();
            for (int i = 0; i < rest; i++) ranking[i].coins++;
            return Stable(quotas).Select(q => new MedicalShare { workerId = q.id, consumables = q.consumables, coins = q.coins }).ToList();
        }
    }
}
