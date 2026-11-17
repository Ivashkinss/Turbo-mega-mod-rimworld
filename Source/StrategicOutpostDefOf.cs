using System;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    [DefOf]
    public static class StrategicOutpostDefOf
    {
        public static StrategicOutpostDef TMM_StrategicOutpost;

        static StrategicOutpostDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrategicOutpostDefOf));
        }
    }
}
