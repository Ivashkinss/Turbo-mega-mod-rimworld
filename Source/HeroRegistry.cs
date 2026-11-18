using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    public enum HeroState { Unspawned, AtHome, Away, Captive, Dead, Joined, Missing }

    public sealed class HeroRecord : IExposable
    {
        public string heroDefName;
        public HeroState state = HeroState.Unspawned;
        public Pawn pawn;
        public int deathTick = -1;

        public void ExposeData()
        {
            Scribe_Values.Look(ref heroDefName, "hero");
            Scribe_Values.Look(ref state, "state", HeroState.Unspawned);
            Scribe_Values.Look(ref deathTick, "deathTick", -1);
            // A discarded (Missing) pawn is not saved anywhere, so its reference is dropped to keep the save clean.
            if (Scribe.mode == LoadSaveMode.Saving && pawn != null && pawn.Discarded) pawn = null;
            Scribe_References.Look(ref pawn, "pawn");
        }
    }

    /// One persistent record per named hero: the same pawn on every request, no clones, no resurrection.
    /// See Documentation/HERO-REGISTRY-DESIGN.md. State is tracked by polling, not by lifecycle patches.
    public sealed class HeroRegistry : WorldComponent
    {
        private const int PollInterval = 250;
        private List<HeroRecord> records = new List<HeroRecord>();

        public HeroRegistry(World world) : base(world) { }

        public static HeroRegistry Instance { get { return Find.World == null ? null : Find.World.GetComponent<HeroRegistry>(); } }

        public IEnumerable<HeroRecord> Records { get { return records; } }

        public HeroRecord RecordFor(HeroDef def)
        {
            if (def == null) return null;
            return records.FirstOrDefault(r => r.heroDefName == def.defName);
        }

        public bool IsHero(Pawn pawn) { return pawn != null && records.Any(r => r.pawn == pawn); }

        public static Faction FactionFor(HeroDef def)
        {
            if (def == null || def.factionDef.NullOrEmpty() || Find.FactionManager == null) return null;
            FactionDef fd = DefDatabase<FactionDef>.GetNamedSilentFail(def.factionDef);
            return fd == null ? null : Find.FactionManager.FirstFactionOfDef(fd);
        }

        /// Returns the hero's single pawn, creating it on first request. Null when the hero is dead or
        /// missing, when its faction is absent from this world, or when it needs a DLC that is not active.
        public Pawn GetOrCreate(HeroDef def)
        {
            if (def == null) return null;
            HeroRecord rec = RecordFor(def);
            if (rec != null)
            {
                Refresh(rec);
                if (rec.state == HeroState.Dead || rec.state == HeroState.Missing) return null;
                if (rec.pawn != null) return rec.pawn;
            }
            if (def.representation != HeroRepresentation.HumanlikePawn) return null; // stage 1: humanlike only
            if (!def.xenotype.NullOrEmpty() && !ModsConfig.BiotechActive) return null;
            Faction faction = FactionFor(def);
            if (faction == null) return null;

            Pawn pawn = Generate(def, faction);
            if (pawn == null) return null;
            if (rec == null) { rec = new HeroRecord { heroDefName = def.defName }; records.Add(rec); }
            rec.pawn = pawn;
            rec.state = HeroState.AtHome;
            Keep(pawn);
            if (def.isLeader && (faction.leader == null || faction.leader.Dead || !IsHero(faction.leader))) faction.leader = pawn;
            return pawn;
        }

        private static Pawn Generate(HeroDef def, Faction faction)
        {
            XenotypeDef xeno = def.xenotype.NullOrEmpty() ? null : DefDatabase<XenotypeDef>.GetNamedSilentFail(def.xenotype);
            if (!def.xenotype.NullOrEmpty() && xeno == null) return null;
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                def.pawnKind,
                faction,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                fixedGender: def.gender == Gender.None ? (Gender?)null : def.gender,
                fixedBiologicalAge: def.ageRange.RandomInRange,
                forcedXenotype: xeno,
                developmentalStages: DevelopmentalStage.Adult));
            pawn.Name = new NameTriple(def.nameFirst, def.nameNick.NullOrEmpty() ? def.nameFirst : def.nameNick, def.nameLast ?? "");
            if (pawn.story != null && pawn.story.traits != null)
                foreach (TraitDef t in def.forcedTraits)
                    if (t != null && !pawn.story.traits.HasTrait(t)) pawn.story.traits.GainTrait(new Trait(t, 0, true));
            if (pawn.skills != null)
                foreach (HeroSkillMin s in def.skills)
                {
                    if (s == null || s.skill == null) continue;
                    SkillRecord sr = pawn.skills.GetSkill(s.skill);
                    if (!sr.TotallyDisabled && sr.Level < s.level) sr.Level = s.level;
                }
            if (def.weapon != null && pawn.equipment != null)
            {
                pawn.equipment.DestroyAllEquipment();
                ThingWithComps w = (ThingWithComps)ThingMaker.MakeThing(def.weapon, def.weapon.MadeFromStuff ? GenStuff.DefaultStuffFor(def.weapon) : null);
                pawn.equipment.AddEquipment(w);
            }
            if (pawn.apparel != null)
                foreach (ThingDef a in def.apparel)
                {
                    if (a == null || !ApparelUtility.HasPartsToWear(pawn, a)) continue;
                    Apparel ap = (Apparel)ThingMaker.MakeThing(a, a.MadeFromStuff ? GenStuff.DefaultStuffFor(a) : null);
                    pawn.apparel.Wear(ap, false);
                }
            return pawn;
        }

        private static void Keep(Pawn pawn)
        {
            if (pawn.Spawned || pawn.Discarded) return;
            if (!Find.WorldPawns.Contains(pawn)) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        }

        private static HeroState Classify(HeroDef def, Pawn p)
        {
            if (p == null || p.Discarded) return HeroState.Missing;
            if (p.Dead) return HeroState.Dead;
            if (p.Destroyed) return HeroState.Missing;
            if (p.IsPrisoner || p.IsSlave) return HeroState.Captive;
            if (p.Faction != null && p.Faction.IsPlayer) return HeroState.Joined;
            Faction home = FactionFor(def);
            if (home != null && p.Faction == home)
            {
                if (!p.Spawned && p.GetCaravan() == null) return HeroState.AtHome;
                if (p.Spawned && p.Map != null && p.Map.Parent is Settlement && p.Map.Parent.Faction == home) return HeroState.AtHome;
                return HeroState.Away;
            }
            return HeroState.Away;
        }

        private void Refresh(HeroRecord rec)
        {
            if (rec.state == HeroState.Dead || rec.state == HeroState.Missing) return;
            HeroDef def = DefDatabase<HeroDef>.GetNamedSilentFail(rec.heroDefName);
            if (def == null) return;
            HeroState before = rec.state;
            rec.state = Classify(def, rec.pawn);
            if (rec.state == HeroState.Dead && before != HeroState.Dead)
            {
                rec.deathTick = Find.TickManager.TicksGame;
                OnHeroGone(def);
            }
            else if (rec.state == HeroState.Missing && before != HeroState.Missing)
            {
                rec.pawn = null;
                OnHeroGone(def);
            }
            else if (rec.pawn != null && !rec.pawn.Spawned && !Find.WorldPawns.Contains(rec.pawn) && rec.pawn.GetCaravan() == null && rec.pawn.ParentHolder == null)
            {
                Keep(rec.pawn);
            }
        }

        /// Leader succession: explicit successor chain first, then the highest-ranked living hero of the
        /// faction (council members first). With no hero left, the vanilla leader stays.
        private void OnHeroGone(HeroDef gone)
        {
            Faction faction = FactionFor(gone);
            if (faction == null) return;
            bool wasLeader = gone.isLeader || (faction.leader != null && (faction.leader.Dead || faction.leader.Discarded));
            if (!wasLeader) return;
            if (faction.leader != null && !faction.leader.Dead && !faction.leader.Discarded && IsHero(faction.leader)) return;

            var seen = new HashSet<HeroDef>();
            for (HeroDef s = gone.successor; s != null && seen.Add(s); s = s.successor)
            {
                Pawn p = GetOrCreate(s);
                if (p != null && p.Faction == faction) { faction.leader = p; return; }
            }
            foreach (HeroDef d in DefDatabase<HeroDef>.AllDefs
                         .Where(d => d.factionDef == gone.factionDef && d != gone)
                         .OrderByDescending(d => d.councilMember).ThenByDescending(d => d.rank))
            {
                HeroRecord r = RecordFor(d);
                if (r != null && (r.state == HeroState.Dead || r.state == HeroState.Missing)) continue;
                Pawn p = GetOrCreate(d);
                if (p != null && p.Faction == faction) { faction.leader = p; return; }
            }
        }

        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % PollInterval != 0) return;
            for (int i = 0; i < records.Count; i++) Refresh(records[i]);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref records, "TMM_heroRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (records == null) records = new List<HeroRecord>();
                records.RemoveAll(r => r == null || r.heroDefName.NullOrEmpty());
            }
        }
    }

    /// Living heroes are critical world pawns: the world pawn GC must never discard them.
    [StaticConstructorOnStartup]
    public static class HeroRegistryPatches
    {
        static HeroRegistryPatches()
        {
            var target = AccessTools.Method(typeof(WorldPawnGC), "GetCriticalPawnReason");
            if (target == null) { Log.Warning("[TMM] HeroRegistry: WorldPawnGC.GetCriticalPawnReason not found; heroes rely on KeepForever only."); return; }
            new Harmony("turbomegamod.hero.registry").Patch(target, postfix: new HarmonyMethod(typeof(HeroRegistryPatches), "CriticalReason"));
        }

        public static void CriticalReason(Pawn pawn, ref string __result)
        {
            if (__result != null || pawn == null || pawn.Dead) return;
            HeroRegistry reg = HeroRegistry.Instance;
            if (reg != null && reg.IsHero(pawn)) __result = "TMM named hero";
        }
    }
}
