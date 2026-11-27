using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    public sealed class StoryZeroSynth : WorldComponent, IThingHolder
    {
        private const string PawnKind = "TMM_ZeroSynth";
        private const string Prologue = "TMM_Story_A1_Signal";
        private const string Joined = "story.zeroSynthJoined";
        private ThingOwner<Pawn> pending;

        public StoryZeroSynth(World world) : base(world)
        {
            pending = new ThingOwner<Pawn>(this);
        }

        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return pending; }
        public void GetChildHolders(List<IThingHolder> children)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(children, pending);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref pending, "zeroSynthPending", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pending == null)
                pending = new ThingOwner<Pawn>(this);
        }

        internal static bool IsZeroSynth(Pawn pawn)
        {
            return pawn != null && pawn.kindDef != null && pawn.kindDef.defName == PawnKind;
        }

        internal void TryArrive(SparkStory story, Map home)
        {
            if (story.GetFlag(Joined) == "true") return;
            StoryQuestRun prologue = story.RunFor(Prologue);
            if (prologue == null || prologue.status != StoryQuestStatus.Completed) return;

            if (pending.Count == 0)
            {
                // Also recognise a canonical pawn that was placed before the flag could be set.
                foreach (Pawn existing in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
                {
                    if (IsZeroSynth(existing) && existing.Faction == Faction.OfPlayer)
                    {
                        story.SetFlag(Joined, "true");
                        return;
                    }
                }
            }

            IntVec3 cell = DropCellFinder.TradeDropSpot(home);
            if (!cell.IsValid || !cell.InBounds(home) || !cell.Standable(home) || cell.GetFirstPawn(home) != null)
            {
                if (!CellFinder.TryFindRandomCellNear(home.Center, home, 18,
                    c => c.Standable(home) && c.GetFirstPawn(home) == null, out cell)) return;
            }

            Pawn synth = pending.Count == 0 ? CreatePawn() : pending[0];
            if (synth == null) return;

            // Keep the exact pawn on retry and through a save that occurs before successful placement.
            try
            {
                Thing placed;
                pending.TryDrop(synth, cell, home, ThingPlaceMode.Direct, out placed);
            }
            catch (Exception ex)
            {
                Log.WarningOnce("[TMM] Zero Synth arrival will retry: " + ex.Message, 1290051924);
            }

            if (!synth.Spawned || synth.Map != home)
            {
                if (synth.holdingOwner == null && !synth.Destroyed && !Find.WorldPawns.Contains(synth))
                    pending.TryAdd(synth, false);
                return;
            }

            story.SetFlag(Joined, "true");
            Find.LetterStack.ReceiveLetter("TMM.Story.ZeroSynth.ArrivedTitle".Translate(),
                "TMM.Story.ZeroSynth.ArrivedText".Translate(), LetterDefOf.PositiveEvent, synth);
        }

        private Pawn CreatePawn()
        {
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(PawnKind);
            if (kind == null) return null;
            var request = new PawnGenerationRequest(kind, Faction.OfPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, fixedBiologicalAge: 24f, fixedChronologicalAge: 24f);
            if (ModsConfig.BiotechActive)
                request.ForcedXenotype = DefDatabase<XenotypeDef>.GetNamedSilentFail("TMM_ZeroSynthXenotype");

            Pawn pawn = PawnGenerator.GeneratePawn(request);
            pawn.Name = new NameSingle("TMM.Story.ZeroSynth.Name".Translate());
            pawn.story.Childhood = DefDatabase<BackstoryDef>.GetNamed("TMM_ZeroSynthChildhood");
            pawn.story.Adulthood = null;
            pawn.Notify_DisabledWorkTypesChanged();
            foreach (SkillRecord skill in pawn.skills.skills) skill.Level = 20;
            if (pending.TryAdd(pawn, false)) return pawn;

            // This empty single-pawn holder should accept the new, unspawned pawn.
            if (pawn.holdingOwner == null && !pawn.Spawned) pawn.Destroy(DestroyMode.Vanish);
            Log.WarningOnce("[TMM] Could not reserve the Zero Synth for arrival.", 1290051925);
            return null;
        }
    }

    [StaticConstructorOnStartup]
    internal static class StoryZeroSynthPatches
    {
        static StoryZeroSynthPatches()
        {
            var harmony = new Harmony("colonyproject.integrated.story.zerosynth");
            Patch(harmony, AccessTools.PropertyGetter(typeof(Pawn), "HealthScale"), null, "HealthScalePostfix");
            Patch(harmony, AccessTools.Method(typeof(SkillRecord), "GetLevel", new[] { typeof(bool) }),
                null, "SkillLevelPostfix");
            Patch(harmony, AccessTools.Method(typeof(SkillRecord), "Learn", new[] { typeof(float), typeof(bool), typeof(bool) }),
                "LearnPrefix", null);
            Patch(harmony, AccessTools.Method(typeof(Pawn_NeedsTracker), "ShouldHaveNeed", new[] { typeof(NeedDef) }),
                null, "ShouldHaveNeedPostfix");
        }

        private static void Patch(Harmony harmony, MethodInfo method, string prefix, string postfix)
        {
            if (method == null)
            {
                Log.Error("[TMM] Missing Zero Synth hook: " + (prefix ?? postfix));
                return;
            }
            harmony.Patch(method,
                prefix: prefix == null ? null : new HarmonyMethod(typeof(StoryZeroSynthPatches), prefix),
                postfix: postfix == null ? null : new HarmonyMethod(typeof(StoryZeroSynthPatches), postfix));
        }

        private static void HealthScalePostfix(Pawn __instance, ref float __result)
        {
            if (StoryZeroSynth.IsZeroSynth(__instance)) __result *= 8f;
        }

        private static bool LearnPrefix(Pawn ___pawn) { return !StoryZeroSynth.IsZeroSynth(___pawn); }

        private static void SkillLevelPostfix(Pawn ___pawn, ref int __result)
        {
            // Keep the rank of disabled combat skills without enabling violent work.
            if (StoryZeroSynth.IsZeroSynth(___pawn)) __result = 20;
        }

        private static void ShouldHaveNeedPostfix(Pawn ___pawn, NeedDef nd, ref bool __result)
        {
            if (StoryZeroSynth.IsZeroSynth(___pawn) && (nd == NeedDefOf.Food || nd == NeedDefOf.Rest))
                __result = false;
        }
    }
}
