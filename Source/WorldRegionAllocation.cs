using System;

namespace TurboMegaMod
{
    // Capacitated geographic assignment. Columns are the original settlement sites: no new
    // dense cluster can be introduced, since the output is a permutation of those same sites.
    public static class WorldRegionAllocation
    {
        public const int ExactLimit = 384;
        public const int SwapBudget = 4000000;
        public const double Forbidden = 1000000000;
        public static int[] Assign(double[,] cost, int[] groups)
        {
            int n = groups.Length;
            if (cost.GetLength(1) != n) throw new ArgumentException("Region assignment dimensions differ.");
            var result = new int[n];
            if (n == 0) return result;
            if (n > ExactLimit)
            {
                // Bounded improvement from the known legal native arrangement for huge modded worlds.
                for (int i = 0; i < n; i++) result[i] = i;
                int checks = 0;
                for (int pass = 0; pass < 6 && checks < SwapBudget; pass++)
                {
                    bool changed = false;
                    for (int i = 0; i < n && checks < SwapBudget; i++)
                        for (int j = i + 1; j < n && checks < SwapBudget; j++)
                        {
                            checks++;
                            int a = groups[i], b = groups[j];
                            if (a == b) continue;
                            int x = result[i], y = result[j];
                            if (cost[a, y] >= Forbidden || cost[b, x] >= Forbidden) continue;
                            if (cost[a, y] + cost[b, x] + 0.000001 >= cost[a, x] + cost[b, y]) continue;
                            result[i] = y; result[j] = x; changed = true;
                        }
                    if (!changed) break;
                }
                return result;
            }
            var u = new double[n + 1]; var v = new double[n + 1];
            var p = new int[n + 1]; var way = new int[n + 1];
            for (int row = 1; row <= n; row++)
            {
                p[0] = row;
                int column = 0;
                var min = new double[n + 1]; var used = new bool[n + 1];
                for (int j = 1; j <= n; j++) min[j] = double.PositiveInfinity;
                do
                {
                    used[column] = true;
                    int current = p[column], next = 0;
                    double delta = double.PositiveInfinity;
                    for (int j = 1; j <= n; j++)
                    {
                        if (used[j]) continue;
                        double reduced = cost[groups[current - 1], j - 1] - u[current] - v[j];
                        if (reduced < min[j]) { min[j] = reduced; way[j] = column; }
                        if (min[j] < delta) { delta = min[j]; next = j; }
                    }
                    for (int j = 0; j <= n; j++)
                    {
                        if (used[j]) { u[p[j]] += delta; v[j] -= delta; }
                        else min[j] -= delta;
                    }
                    column = next;
                } while (p[column] != 0);
                do { int previous = way[column]; p[column] = p[previous]; column = previous; } while (column != 0);
            }
            for (int j = 1; j <= n; j++) result[p[j] - 1] = j - 1;
            return result;
        }
    }
}
