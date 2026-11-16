using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    // 1.7 gathers path data once per tick. An area created later in that tick
    // must be registered before a request can reference its native bit array.
    [StaticConstructorOnStartup]
    public static class GuestAreaCacheHooks
    {
        private static readonly Type dataType=AccessTools.TypeByName("Verse.PathFinderMapData");
        private static readonly FieldInfo last=dataType==null?null:AccessTools.Field(dataType,"lastGatherTick");
        private static readonly FieldInfo mapField=dataType==null?null:AccessTools.Field(dataType,"map");
        private static readonly FieldInfo areas=dataType==null?null:AccessTools.Field(dataType,"areas");
        private static readonly FieldInfo bits=areas==null?null:AccessTools.Field(areas.FieldType,"areaToBits");
        public static bool Enabled {get{return last!=null&&mapField!=null&&areas!=null&&bits!=null;}}
        static GuestAreaCacheHooks()
        {
            if(!Enabled)return;
            // Native callers finish scheduled jobs before GatherData and recycle
            // cached grids when it returns true. Keep that native refresh path.
            new Harmony("turbomegamod.guest.area.cache").Patch(AccessTools.Method(dataType,"GatherData"),
                prefix:new HarmonyMethod(typeof(GuestAreaCacheHooks),"Gather"));
        }
        private static void Gather(object __instance)
        {
            int now=Find.TickManager.TicksGame;if((int)last.GetValue(__instance)!=now)return;
            var map=mapField.GetValue(__instance) as Map;if(map==null)return;
            var layout=map.GetComponent<ComplexLayout>();var area=layout==null?null:layout.guestAccess;
            if(area==null||!map.areaManager.AllAreas.Contains(area))return;
            var cache=bits.GetValue(areas.GetValue(__instance)) as IDictionary;
            if(cache!=null&&!cache.Contains(area))last.SetValue(__instance,now-1);
        }
    }
}
