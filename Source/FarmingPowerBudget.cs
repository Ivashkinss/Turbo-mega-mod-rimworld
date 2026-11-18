namespace TurboMegaMod
{
    // One lamp per 24 basins; climate capacity per 6x6 growing section.
    // Active climate costs at least one vanilla unit per 36 cells plus premium.
    // Outdoor load remains billable after the room reaches its setpoint.
    public static class FarmingPowerBudget
    {
        public const float Premium = 1.15f;
        public static float Greenhouse(int cells, bool light, bool heat, float lampWatts, float heaterWatts, bool night = false, float nightLampWatts = 30f)
        {
            return Greenhouse(cells, light, heat ? -20f : 0f, 0f, lampWatts, heaterWatts, heaterWatts, night, nightLampWatts);
        }
        public static float Greenhouse(int cells, bool light, float temperature, float target, float lampWatts, float heaterWatts, float coolerWatts, bool night = false, float nightLampWatts = 30f, float ambientTemperature = float.NaN, float lampLevel = 1f)
        {
            if (cells <= 0) return 0f;
            float difference = target - temperature;
            float ambient = float.IsNaN(ambientTemperature) ? difference : target - ambientTemperature;
            float gap = System.Math.Max(System.Math.Abs(difference), System.Math.Abs(ambient));
            float direction = System.Math.Abs(difference) > 0.5f ? difference : ambient;
            float climate = gap <= 0.5f ? heaterWatts * 0.1f :
                (direction >= 0f ? heaterWatts : coolerWatts) * System.Math.Max(1f, gap / 20f);
            return cells * Premium * ((light ? lampWatts / 96f * lampLevel : night ? nightLampWatts / 36f : 0f) + climate / 36f);
        }
        public static float Hydroponics(bool light, float pumpWatts, float lampWatts, bool night = false, float nightLampWatts = 30f)
        {
            return Premium * (pumpWatts + (light ? lampWatts / 24f : night ? nightLampWatts / 9f : 0f));
        }
    }
}
