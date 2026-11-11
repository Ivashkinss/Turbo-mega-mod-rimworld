using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    internal static class SidearmsCompatibility
    {
        private const string Owner = "turbomegamod.sidearms.melee";
        private static FieldInfo activeField;

        static SidearmsCompatibility()
        {
            // Melee Animation checks the old packageId. The embedded component
            // retains its Mod class and assembly, but belongs to our content pack.
            var sidearms = LoadedModManager.ModHandles.FirstOrDefault(m =>
                m.GetType().FullName == "PeteTimesSix.SimpleSidearms.SimpleSidearms"
                && m.Content.PackageId == "colonyproject.integrated");
            if (sidearms == null) return;
            var core = AccessTools.TypeByName("AM.Core");
            if (core == null) return;
            var target = AccessTools.Method(core, "CheckForActiveMods");
            activeField = AccessTools.Field(core, "IsSimpleSidearmsActive");
            if (target == null || activeField == null || activeField.FieldType != typeof(bool) || !activeField.IsStatic)
            {
                Log.Warning("[Turbo mega mod] Melee Animation sidearms detection changed; compatibility flag was not updated.");
                return;
            }
            var patches = Harmony.GetPatchInfo(target);
            if (patches == null || !patches.Postfixes.Any(p => p.owner == Owner))
                new Harmony(Owner).Patch(target, postfix: new HarmonyMethod(typeof(SidearmsCompatibility), "RecognizeEmbeddedSidearms"));
            // Its initial check can run before our startup constructor.
            RecognizeEmbeddedSidearms();
        }

        private static void RecognizeEmbeddedSidearms() { activeField.SetValue(null, true); }
    }
}
