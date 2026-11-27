using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    public enum HeroRepresentation { HumanlikePawn, AiTerminal, Robot, Giant }

    public sealed class HeroSkillMin
    {
        public SkillDef skill;
        public int level;
    }

    /// Immutable description of a unique named hero (see Documentation/HERO-REGISTRY-DESIGN.md).
    /// The faction is a defName string so heroes of factions that are not installed yet do not
    /// break XML cross-references; GetOrCreate simply returns null for them.
    /// Custom look of a named hero, applied on top of the generated pawn. Every field is optional: unset ones keep
    /// what the generator chose. Colours with alpha 0 mean "not set".
    public sealed class HeroAppearance
    {
        public HairDef hair;
        public BeardDef beard;
        public HeadTypeDef headType;
        public BodyTypeDef bodyType;
        public UnityEngine.Color skinColor = new UnityEngine.Color(0f, 0f, 0f, 0f);
        public UnityEngine.Color hairColor = new UnityEngine.Color(0f, 0f, 0f, 0f);
        public TattooDef faceTattoo;
        public TattooDef bodyTattoo;

        /// Returns true when something on the pawn was changed.
        public bool ApplyTo(Pawn pawn)
        {
            if (pawn == null || pawn.story == null) return false;
            bool changed = false;
            if (hair != null && pawn.story.hairDef != hair) { pawn.story.hairDef = hair; changed = true; }
            if (headType != null && pawn.story.headType != headType) { pawn.story.headType = headType; changed = true; }
            if (bodyType != null && pawn.story.bodyType != bodyType) { pawn.story.bodyType = bodyType; changed = true; }
            if (skinColor.a > 0f && (!pawn.story.skinColorOverride.HasValue || pawn.story.skinColorOverride.Value != skinColor)) { pawn.story.skinColorOverride = skinColor; changed = true; }
            if (hairColor.a > 0f && pawn.story.HairColor != hairColor) { pawn.story.HairColor = hairColor; changed = true; }
            if (pawn.style != null)
            {
                if (beard != null && pawn.style.beardDef != beard) { pawn.style.beardDef = beard; changed = true; }
                if (faceTattoo != null && pawn.style.FaceTattoo != faceTattoo) { pawn.style.FaceTattoo = faceTattoo; changed = true; }
                if (bodyTattoo != null && pawn.style.BodyTattoo != bodyTattoo) { pawn.style.BodyTattoo = bodyTattoo; changed = true; }
            }
            if (changed && pawn.Drawer != null && pawn.Drawer.renderer != null) pawn.Drawer.renderer.SetAllGraphicsDirty();
            return changed;
        }
    }
    public sealed class HeroDef : Def
    {
        public string factionDef;
        public int rank = 3;
        public bool isLeader;
        public bool councilMember;
        public HeroDef successor;
        public HeroRepresentation representation = HeroRepresentation.HumanlikePawn;
        public PawnKindDef pawnKind;
        public string xenotype;
        public string nameFirst;
        public string nameNick;
        public string nameLast;
        public Gender gender = Gender.None;
        public FloatRange ageRange = new FloatRange(25f, 45f);
        public List<TraitDef> forcedTraits = new List<TraitDef>();
        public List<HeroSkillMin> skills = new List<HeroSkillMin>();
        public List<ThingDef> apparel = new List<ThingDef>();
        public ThingDef weapon;
        /// Extra Biotech genes on top of the xenotype (xenogenes), e.g. a hero-specific eye colour.
        public List<GeneDef> extraGenes = new List<GeneDef>();
        /// Unique backstories (Defs/BackstoryDefs/Backstories_Heroes.xml); null keeps the generated one.
        public BackstoryDef childhood;
        public BackstoryDef adulthood;
        public bool uniqueWeaponOneShot;
        public HeroAppearance appearance;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors()) yield return e;
            if (factionDef.NullOrEmpty()) yield return "factionDef is empty";
            if (pawnKind == null && representation == HeroRepresentation.HumanlikePawn) yield return "pawnKind is null";
            if (nameFirst.NullOrEmpty()) yield return "nameFirst is empty";
            if (rank < 1 || rank > 4) yield return "rank must be 1..4";
        }
    }
}
