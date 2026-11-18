using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    internal static class VefCompatibility
    {
        private static Assembly embedded;
        private static Type deliverBlueprintsType;
        private static Type deliverFramesType;
        private static Type finishFramesType;

        internal static void Apply()
        {
            if (ModsConfig.IsActive("oskarpotocki.vanillafactionsexpanded.core")) return;
            var pack = LoadedModManager.RunningModsListForReading.Find(m => m.PackageId == "colonyproject.integrated");
            if (pack == null) return;
            embedded = pack.assemblies.loadedAssemblies.Find(a => a.GetName().Name == "VEF");
            if (embedded == null || !Path.GetFullPath(embedded.Location).StartsWith(Path.GetFullPath(Path.Combine(pack.RootDir, "Components", "VanillaExpandedFramework")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;
            var harmony = new Harmony("turbomegamod.vef.compatibility");
            var sidearms = AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Compat.VFECore");
            if (sidearms != null) harmony.Patch(sidearms.TypeInitializer, transpiler: new HarmonyMethod(typeof(VefCompatibility), "SidearmsOwner"));
            var bills = AccessTools.TypeByName("NiceBillTab.ModIntegration");
            if (bills != null) harmony.Patch(AccessTools.Method(bills, "TryGetExternalAssembly"), prefix: new HarmonyMethod(typeof(VefCompatibility), "FindAssembly"));

            deliverBlueprintsType = AccessTools.TypeByName("VEF.Pawns.WorkGiver_ConstructionSkill_DeliverResourcesToBlueprints");
            deliverFramesType = AccessTools.TypeByName("VEF.Pawns.WorkGiver_ConstructionSkill_DeliverResourcesToFrames");
            finishFramesType = AccessTools.TypeByName("VEF.Pawns.WorkGiver_ConstructionSkill_FinishFrames");

            // The VEF work givers may inherit HasJobOnThing from vanilla classes: patch the method where it is
            // actually declared (once), the prefix itself filters by the instance type.
            var prefix = new HarmonyMethod(typeof(VefCompatibility), "CheckConstructionSkillRequirement");
            var patched = new HashSet<MethodBase>();
            var sig = new[] { typeof(Pawn), typeof(Thing), typeof(bool) };
            foreach (Type vefType in new[] { deliverBlueprintsType, deliverFramesType, finishFramesType })
            {
                if (vefType == null) continue;
                MethodInfo resolved = AccessTools.Method(vefType, "HasJobOnThing", sig);
                if (resolved == null) continue;
                MethodInfo declared = AccessTools.DeclaredMethod(resolved.DeclaringType, "HasJobOnThing", sig);
                if (declared != null && patched.Add(declared)) harmony.Patch(declared, prefix: prefix);
            }

            var post = new HarmonyMethod(typeof(VefCompatibility), "Postfix_PotentialWorkThingsGlobal");
            var mScanner = AccessTools.DeclaredMethod(typeof(WorkGiver_Scanner), "PotentialWorkThingsGlobal", new[] { typeof(Pawn) });
            if (mScanner != null) harmony.Patch(mScanner, postfix: post);
        }

        private static bool HasConstructionSkillRequirement(Thing t)
        {
            if (t == null || t.def == null) return false;
            BuildableDef buildable = GenConstruct.BuiltDefOf(t.def);
            if (buildable == null) buildable = t.def.entityDefToBuild;
            if (buildable == null || buildable.modExtensions == null) return false;
            var ext = buildable.modExtensions.Find(e => e != null && e.GetType().FullName == "VEF.Things.ThingDefExtension");
            if (ext == null) return false;
            var reqField = ext.GetType().GetField("constructionSkillRequirement");
            return reqField != null && reqField.GetValue(ext) != null;
        }

        private static bool CheckConstructionSkillRequirement(WorkGiver __instance, Thing __1, ref bool __result)
        {
            Thing t = __1;
            if (__instance == null || t == null) return true;
            Type instType = __instance.GetType();
            if (instType == deliverBlueprintsType || instType == deliverFramesType || instType == finishFramesType)
            {
                if (!HasConstructionSkillRequirement(t))
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }

        private static void Postfix_PotentialWorkThingsGlobal(WorkGiver __instance, ref IEnumerable<Thing> __result)
        {
            if (__instance == null || __result == null) return;
            Type instType = __instance.GetType();
            if (instType == deliverBlueprintsType || instType == deliverFramesType || instType == finishFramesType)
            {
                __result = FilterVefThings(__result);
            }
        }

        private static IEnumerable<Thing> FilterVefThings(IEnumerable<Thing> source)
        {
            foreach (Thing t in source)
            {
                if (HasConstructionSkillRequirement(t)) yield return t;
            }
        }
        private static bool FindAssembly(string __0, string __1, ref Assembly __result)
        {
            if (__0 != "oskarpotocki.vanillafactionsexpanded.core" || __1 != "VEF" || embedded == null) return true;
            __result = embedded;
            return false;
        }
        private static IEnumerable<CodeInstruction> SidearmsOwner(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && string.Equals(instruction.operand as string, "OskarPotocki.VanillaFactionsExpanded.Core", StringComparison.OrdinalIgnoreCase))
                {
                    instruction.operand = "colonyproject.integrated";
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced != 1) throw new InvalidOperationException("Unexpected Simple Sidearms VEF owner check: " + replaced);
        }
    }
}
