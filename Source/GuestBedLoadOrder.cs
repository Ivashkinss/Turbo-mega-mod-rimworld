using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    // Hospitality can restore a bed before the guest's loaded memories have their pawn references.
    // Keep the bed assignment in its original place and defer only its bedroom-thought cleanup.
    [StaticConstructorOnStartup]
    internal static class GuestBedLoadOrder
    {
        [ThreadStatic] private static int guestLoadDepth;

        static GuestBedLoadOrder()
        {
            Type guest = AccessTools.TypeByName("Hospitality.CompGuest");
            var load = guest == null ? null : AccessTools.DeclaredMethod(guest, "PostExposeData");
            var cleanup = AccessTools.Method(typeof(ThoughtUtility), "RemovePositiveBedroomThoughts", new[] { typeof(Pawn) });
            if (load == null || cleanup == null) return;

            var harmony = new Harmony("colonyproject.integrated.guestbedloadorder");
            harmony.Patch(load,
                prefix: new HarmonyMethod(typeof(GuestBedLoadOrder), "BeginGuestLoad"),
                finalizer: new HarmonyMethod(typeof(GuestBedLoadOrder), "EndGuestLoad"));
            harmony.Patch(cleanup, prefix: new HarmonyMethod(typeof(GuestBedLoadOrder), "DeferBedroomThoughtCleanup"));
        }

        private static void BeginGuestLoad(out bool __state)
        {
            __state = Scribe.mode == LoadSaveMode.PostLoadInit;
            if (__state) guestLoadDepth++;
        }

        private static void EndGuestLoad(bool __state)
        {
            if (__state) guestLoadDepth--;
        }

        private static bool DeferBedroomThoughtCleanup(Pawn pawn)
        {
            if (guestLoadDepth == 0 || pawn == null || Current.Game == null) return true;
            PendingGuestBedroomThoughts pending = Current.Game.GetComponent<PendingGuestBedroomThoughts>();
            if (pending == null) return true;
            pending.Add(pawn);
            return false;
        }
    }

    public sealed class PendingGuestBedroomThoughts : GameComponent
    {
        private readonly HashSet<Pawn> guests = new HashSet<Pawn>();

        public PendingGuestBedroomThoughts(Game game) { }

        internal void Add(Pawn pawn) { guests.Add(pawn); }

        public override void GameComponentUpdate()
        {
            if (guests.Count == 0 || Scribe.mode != LoadSaveMode.Inactive || Current.ProgramState != ProgramState.Playing) return;
            var ready = new List<Pawn>(guests);
            guests.Clear();
            foreach (Pawn pawn in ready)
                if (!pawn.Destroyed && !pawn.Dead) ThoughtUtility.RemovePositiveBedroomThoughts(pawn);
        }
    }
}
