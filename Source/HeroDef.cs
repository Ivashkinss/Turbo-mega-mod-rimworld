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
        public bool uniqueWeaponOneShot;

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
