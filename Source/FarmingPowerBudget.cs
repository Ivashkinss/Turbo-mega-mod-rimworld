namespace TurboMegaMod
{
    // One lamp shared across the usual 24 four-cell basins; one heater per
    // 6x6 insulated growing section. The 15% premium also applies to pumps.
    public static class FarmingPowerBudget
    {
        public const float Premium = 1.15f;
        public static float Greenhouse(int cells, bool light, bool heat, float lampWatts, float heaterWatts, bool night = false, float nightLampWatts = 30f)
        {
            if (cells <= 0) return 0f;
            return cells * Premium * ((light ? lampWatts / 96f : night ? nightLampWatts / 36f : 0f) + (heat ? heaterWatts / 36f : heaterWatts / 360f));
        }
        public static float Hydroponics(bool light, float pumpWatts, float lampWatts, bool night = false, float nightLampWatts = 30f)
        {
            return Premium * (pumpWatts + (light ? lampWatts / 24f : night ? nightLampWatts / 9f : 0f));
        }
    }
}
