using System;
using TurboMegaMod;
public static class FarmingPowerBudgetTests
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < 0.01f * Math.Max(1f, Math.Abs(b)); }
    public static void Main()
    {
        Check(Near(24f * FarmingPowerBudget.Hydroponics(true, 70f, 2900f), (24f * 70f + 2900f) * 1.15f), "24 lit basins must cost 15% more than 24 pumps and one lamp");
        Check(Near(FarmingPowerBudget.Hydroponics(false, 70f, 2900f), 80.5f), "Unlit basin must retain powered pumps");
        Check(FarmingPowerBudget.Greenhouse(0, true, true, 2900f, 175f) == 0f, "Empty zone must not draw power");
        Check(FarmingPowerBudget.Greenhouse(-1, true, true, 2900f, 175f) == 0f, "Invalid empty area must not generate energy");
        foreach (int size in new[] { 1, 4, 36, 64, 96, 512, 4096 })
        {
            float day = FarmingPowerBudget.Greenhouse(size, true, true, 2900f, 175f);
            float dark = FarmingPowerBudget.Greenhouse(size, false, true, 2900f, 175f);
            float idle = FarmingPowerBudget.Greenhouse(size, false, false, 2900f, 175f);
            Check(day > dark && dark > idle && idle > 0f, "Light, heat and standby ordering");
            Check(Near(FarmingPowerBudget.Greenhouse(size * 2, true, true, 2900f, 175f), day * 2f), "Doubling area must double demand");
        }
        Check(Near(FarmingPowerBudget.Hydroponics(true, 140f, 5800f), FarmingPowerBudget.Hydroponics(true, 70f, 2900f) * 2f), "Respect changed vanilla baseline values");
        Check(Near(FarmingPowerBudget.Hydroponics(false, 70f, 2900f, true), 84.33333f), "Night lighting adds a small ordinary-lamp share to pumps");
        Check(Near(FarmingPowerBudget.Greenhouse(36, false, false, 2900f, 175f, true) - FarmingPowerBudget.Greenhouse(36, false, false, 2900f, 175f), 34.5f), "Night comfort light is one ordinary lamp per 36 cells plus premium");
        Check(FarmingPowerBudget.Hydroponics(false, 70f, 2900f, true) < FarmingPowerBudget.Hydroponics(true, 70f, 2900f), "Night must save energy relative to grow lights");
        Console.WriteLine("{\"checks\":" + checks + ",\"passed\":true}");
    }
}
