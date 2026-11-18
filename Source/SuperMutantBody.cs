using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Gene that gives the pawn the custom super mutant body type (Biotech genes can only pick vanilla bodies).
    public class Gene_SuperMutantBody : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            SuperMutantBody.Apply(pawn);
        }
    }

    /// Custom TMM_SuperMutant body: assigned after generation and on gene add; apparel has no textures for it,
    /// so apparel graphics are resolved as for the vanilla Hulk body.
    [StaticConstructorOnStartup]
    public static class SuperMutantBody
    {
        public const string BodyDefName = "TMM_SuperMutant";
        private static BodyTypeDef body;

        public static BodyTypeDef Body { get { if (body == null) body = DefDatabase<BodyTypeDef>.GetNamedSilentFail(BodyDefName); return body; } }

        static SuperMutantBody()
        {
            if (!ModsConfig.BiotechActive) return;
            var harmony = new Harmony("turbomegamod.supermutant.body");
            var gen = AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) });
            if (gen == null) Log.Error("[TMM] SuperMutantBody: PawnGenerator.GeneratePawn(request) not found.");
            else harmony.Patch(gen, postfix: new HarmonyMethod(typeof(SuperMutantBody), "AfterGenerate"));
            var app = AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel");
            if (app == null) Log.Error("[TMM] SuperMutantBody: ApparelGraphicRecordGetter.TryGetGraphicApparel not found; apparel on super mutants may be invisible.");
            else harmony.Patch(app, prefix: new HarmonyMethod(typeof(SuperMutantBody), "ApparelAsHulk"));
        }

        public static bool HasGene(Pawn p)
        {
            if (p == null || p.genes == null) return false;
            var list = p.genes.GenesListForReading;
            for (int i = 0; i < list.Count; i++) if (list[i].Active && list[i] is Gene_SuperMutantBody) return true;
            return false;
        }

        public static void Apply(Pawn p)
        {
            if (p == null || p.story == null || Body == null || !HasGene(p)) return;
            if (p.story.bodyType == Body) return;
            p.story.bodyType = Body;
            if (p.Drawer != null && p.Drawer.renderer != null) p.Drawer.renderer.SetAllGraphicsDirty();
        }

        public static void AfterGenerate(Pawn __result) { Apply(__result); }

        public static void ApparelAsHulk(ref BodyTypeDef __1)
        {
            if (__1 != null && __1 == Body) __1 = BodyTypeDefOf.Hulk;
        }
    }
}
