using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    internal static class RealisticRoomsStartup
    {
        static RealisticRoomsStartup() { RealisticRoomsCompatibility.Apply(); }
    }

    internal static class RealisticRoomsCompatibility
    {
        internal static readonly string[] SettingFields = {
            "minSpaceRatherTight", "minSpaceAverageSized", "minSpaceSomewhatSpacious",
            "minSpaceQuiteSpacious", "minSpaceVerySpacious", "minSpaceExtremelySpacious"
        };

        internal static void Apply()
        {
            Type startup = AccessTools.TypeByName("RealisticRoomsRewritten.RealisticRoomsModOnStartup");
            if (startup == null) return;
            var target = AccessTools.Method(startup, "ApplySettings");
            var info = Harmony.GetPatchInfo(target);
            if (info == null || !info.Postfixes.Any(p => p.owner == "turbomegamod.realisticrooms"))
                new Harmony("turbomegamod.realisticrooms").Patch(target,
                    postfix: new HarmonyMethod(typeof(RealisticRoomsCompatibility), "ApplyThresholds"));
            // Reapply once after the patch: the original startup initializer
            // may already have run. Definitions and mod handles are ready here.
            var mod = LoadedModManager.ModHandles.FirstOrDefault(m => m.GetType().FullName == "RealisticRoomsRewritten.RealisticRoomsMod");
            if (mod != null) target.Invoke(null, new[] { AccessTools.Field(mod.GetType(), "settings").GetValue(mod) });
        }

        // The original XML defines seven ordered stages. Labels have already
        // been translated at startup; comparing them with English text fails.
        // Keep the original filth handling and all settings/save keys unchanged.
        private static void ApplyThresholds(object settings)
        {
            RoomStatDef space = DefDatabase<RoomStatDef>.GetNamedSilentFail("Space");
            if (space == null || space.scoreStages == null || space.scoreStages.Count != 7)
            {
                Log.Warning("[Turbo mega mod] Realistic Rooms: unexpected Space stages; leaving the original settings behavior intact.");
                return;
            }
            for (int index = 0; index < SettingFields.Length; index++)
                space.scoreStages[index + 1].minScore = (float)AccessTools.Field(settings.GetType(), SettingFields[index]).GetValue(settings);
        }
    }
}
