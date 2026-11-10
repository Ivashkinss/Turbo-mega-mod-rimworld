using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    internal static class MeleeCompatibility
    {
        internal static void Apply()
        {
            // 1.7's MapPawns asserts main-thread access. The original mod passes
            // a lazy MapPawns iterator to Parallel.ForEach, which enumerates it
            // on worker threads. Keep its filtering/duel logic and snapshot only
            // that iterator on the calling (map tick) thread.
            if (!typeof(Pawn).Assembly.GetReferencedAssemblies().Any(a => a.Name == "UnityStubs")) return;
            ApplySnapshotPatch();
        }

        internal static void ApplySnapshotPatch()
        {
            Type type = AccessTools.TypeByName("AM.AutoDuel.AutoFriendlyDuelMapComp");
            if (type == null) return;
            MethodInfo method = AccessTools.Method(type, "UpdatePawnsThatCanDuel");
            if (method == null) return;
            new Harmony("turbomegamod.melee.waylith").Patch(method,
                transpiler: new HarmonyMethod(typeof(MeleeCompatibility), "SnapshotEnumeration"));
            Log.Message("[Turbo mega mod] Melee duel pawn enumeration snapshotted on the map tick thread.");
        }

        private static IEnumerable<CodeInstruction> SnapshotEnumeration(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo snapshot = AccessTools.Method(typeof(MeleeCompatibility), "Snapshot");
            int replaced = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                MethodInfo called = instruction.operand as MethodInfo;
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                    && called != null && called.DeclaringType.FullName == "AM.AutoDuel.AutoFriendlyDuelMapComp"
                    && called.Name == "EnumeratePossiblePawns" && called.ReturnType == typeof(IEnumerable<Pawn>))
                {
                    yield return new CodeInstruction(OpCodes.Call, snapshot);
                    replaced++;
                }
            }
            if (replaced != 2)
                throw new InvalidOperationException("Melee Animation duel enumeration changed; expected two call sites, found " + replaced);
        }

        private static Pawn[] Snapshot(IEnumerable<Pawn> pawns) { return pawns.ToArray(); }
    }
}
