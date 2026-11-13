namespace TurboMegaMod
{
    // One lamp per 24 basins; climate capacity per 6x6 growing section.
    // A 20-degree correction is the nominal climate load, with 10% standby.
    public static class FarmingPowerBudget
    {
        public const float Premium = 1.15f;
        public static float Greenhouse(int cells, bool light, bool heat, float lampWatts, float heaterWatts, bool night = false, float nightLampWatts = 30f)
        {
            return Greenhouse(cells, light, heat ? -20f : 0f, 0f, lampWatts, heaterWatts, heaterWatts, night, nightLampWatts);
        }
        public static float Greenhouse(int cells, bool light, float temperature, float target, float lampWatts, float heaterWatts, float coolerWatts, bool night = false, float nightLampWatts = 30f)
        {
            if (cells <= 0) return 0f;
            float difference = target - temperature;
            float climate = heaterWatts * 0.1f + (difference >= 0f ? heaterWatts : coolerWatts) * 0.9f * System.Math.Abs(difference) / 20f;
            return cells * Premium * ((light ? lampWatts / 96f : night ? nightLampWatts / 36f : 0f) + climate / 36f);
        }
        public static float Hydroponics(bool light, float pumpWatts, float lampWatts, bool night = false, float nightLampWatts = 30f)
        {
            return Premium * (pumpWatts + (light ? lampWatts / 24f : night ? nightLampWatts / 9f : 0f));
        }
    }
}
