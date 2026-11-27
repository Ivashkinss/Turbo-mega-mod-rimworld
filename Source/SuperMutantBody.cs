using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Settings of a gene that gives a custom (non-vanilla) body type. Biotech genes can only pick vanilla bodies.
    /// bodyType: the custom body; femaleBodyType: optional body for women; apparelBodyType: the vanilla body whose
    /// apparel textures are used on the custom body (custom bodies have no apparel textures of their own).
    public class GeneCustomBodyExtension : DefModExtension
    {
        public BodyTypeDef bodyType;
        public BodyTypeDef femaleBodyType;
        public BodyTypeDef apparelBodyType;
    }

    /// Gene that gives the pawn the custom body from its GeneCustomBodyExtension.
    public class Gene_ForceBodyType : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            CustomBody.Apply(pawn);
        }
    }

    /// The original super mutant body gene. Old saves and the def keep this class; without an extension it uses
    /// TMM_SuperMutant (TMM_SuperMutantFemale for women, when that def exists) and Hulk apparel.
    public class Gene_SuperMutantBody : Gene_ForceBodyType
    {
    }

    /// Custom bodies: assigned after generation and when the gene is added; never changed on load, so pawns in
    /// existing saves keep the body they already have.
    [StaticConstructorOnStartup]
    public static class CustomBody
    {
        private static Dictionary<BodyTypeDef, BodyTypeDef> apparelBody;

        static CustomBody()
        {
            if (!ModsConfig.BiotechActive) return;
            var harmony = new Harmony("turbomegamod.supermutant.body");
            var gen = AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) });
            if (gen == null) Log.Error("[TMM] CustomBody: PawnGenerator.GeneratePawn(request) not found.");
            else harmony.Patch(gen, postfix: new HarmonyMethod(typeof(CustomBody), "AfterGenerate"));
            var app = AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel");
            if (app == null) Log.Error("[TMM] CustomBody: ApparelGraphicRecordGetter.TryGetGraphicApparel not found; apparel on custom bodies may be invisible.");
            else harmony.Patch(app, prefix: new HarmonyMethod(typeof(CustomBody), "ApparelAsVanillaBody"));
        }

        private static void Resolve(GeneDef def, out BodyTypeDef male, out BodyTypeDef female, out BodyTypeDef apparel)
        {
            GeneCustomBodyExtension ext = def != null ? def.GetModExtension<GeneCustomBodyExtension>() : null;
            if (ext != null)
            {
                male = ext.bodyType;
                female = ext.femaleBodyType ?? ext.bodyType;
                apparel = ext.apparelBodyType ?? BodyTypeDefOf.Male;
                return;
            }
            male = DefDatabase<BodyTypeDef>.GetNamedSilentFail("TMM_SuperMutant");
            female = DefDatabase<BodyTypeDef>.GetNamedSilentFail("TMM_SuperMutantFemale") ?? male;
            apparel = BodyTypeDefOf.Hulk;
        }

        private static Dictionary<BodyTypeDef, BodyTypeDef> ApparelBodies
        {
            get
            {
                if (apparelBody != null) return apparelBody;
                apparelBody = new Dictionary<BodyTypeDef, BodyTypeDef>();
                foreach (GeneDef g in DefDatabase<GeneDef>.AllDefsListForReading)
                {
                    if (g.geneClass == null || !typeof(Gene_ForceBodyType).IsAssignableFrom(g.geneClass)) continue;
                    BodyTypeDef m, f, a;
                    Resolve(g, out m, out f, out a);
                    if (m != null) apparelBody[m] = a;
                    if (f != null) apparelBody[f] = a;
                }
                return apparelBody;
            }
        }

        public static Gene_ForceBodyType BodyGene(Pawn p)
        {
            if (p == null || p.genes == null) return null;
            var list = p.genes.GenesListForReading;
            for (int i = 0; i < list.Count; i++)
            {
                Gene_ForceBodyType g = list[i] as Gene_ForceBodyType;
                if (g != null && g.Active) return g;
            }
            return null;
        }

        public static void Apply(Pawn p)
        {
            if (p == null || p.story == null) return;
            Gene_ForceBodyType gene = BodyGene(p);
            if (gene == null) return;
            BodyTypeDef m, f, a;
            Resolve(gene.def, out m, out f, out a);
            BodyTypeDef want = p.gender == Gender.Female ? f : m;
            if (want == null || p.story.bodyType == want) return;
            // Never replace another custom body (a unique hero body such as Nick Valentine's).
            BodyTypeDef cur = p.story.bodyType;
            if (cur != null && cur != m && cur != f && !IsVanilla(cur)) return;
            p.story.bodyType = want;
            if (p.Drawer != null && p.Drawer.renderer != null) p.Drawer.renderer.SetAllGraphicsDirty();
        }

        private static bool IsVanilla(BodyTypeDef b)
        {
            return b == BodyTypeDefOf.Male || b == BodyTypeDefOf.Female || b == BodyTypeDefOf.Thin || b == BodyTypeDefOf.Hulk
                || b == BodyTypeDefOf.Fat || b == BodyTypeDefOf.Child || b == BodyTypeDefOf.Baby;
        }

        public static void AfterGenerate(Pawn __result) { Apply(__result); }

        public static void ApparelAsVanillaBody(ref BodyTypeDef __1)
        {
            BodyTypeDef v;
            if (__1 != null && ApparelBodies.TryGetValue(__1, out v) && v != null) __1 = v;
        }
    }

    /// Kept for code and probes that still use the old name.
    public static class SuperMutantBody
    {
        public const string BodyDefName = "TMM_SuperMutant";
        public static BodyTypeDef Body { get { return DefDatabase<BodyTypeDef>.GetNamedSilentFail(BodyDefName); } }
        public static bool HasGene(Pawn p) { return CustomBody.BodyGene(p) is Gene_SuperMutantBody; }
        public static void Apply(Pawn p) { CustomBody.Apply(p); }
    }
}
