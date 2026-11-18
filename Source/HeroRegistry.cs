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
            if (ModsConfig.BiotechActive && pawn.genes != null)
                foreach (GeneDef g in def.extraGenes)
                    if (g != null && !pawn.genes.HasActiveGene(g)) pawn.genes.AddGene(g, true);
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
            if (!Find.WorldPawns.Contains(pawn)) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide); // GC protection comes from CriticalReason
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
                // Real position first: caravan, then any map the pawn is held on (spawned, in a pod,
                // casket or other holder). Only a plain world pawn or the faction's own settlement map is home.
                if (p.GetCaravan() != null) return HeroState.Away;
                Map held = p.MapHeld;
                if (held != null) return held.Parent is Settlement && held.Parent.Faction == home ? HeroState.AtHome : HeroState.Away;
                // A world pawn can still be inside a world-level holder (transport pod in flight: WorldPawns
                // contains it, MapHeld is null). Only a pawn held by nothing is really at home.
                if (p.ParentHolder != null || PawnUtility.IsTravelingInTransportPodWorldObject(p)) return HeroState.Away;
                if (Find.WorldPawns.Contains(p)) return HeroState.AtHome;
                return HeroState.Away; // in some world-level holder (transport pod in flight etc.)
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
            Pawn next = ChooseSuccessor(gone, faction);
            if (next != null) faction.leader = next;
        }

        /// Successor: explicit chain of the gone hero first, then the best living hero of the faction
        /// (council members first, then rank). Null when no hero can lead.
        private Pawn ChooseSuccessor(HeroDef gone, Faction faction)
        {
            if (gone != null)
            {
                var seen = new HashSet<HeroDef>();
                for (HeroDef s = gone.successor; s != null && seen.Add(s); s = s.successor)
                {
                    Pawn p = GetOrCreate(s);
                    if (p != null && p.Faction == faction) return p;
                }
            }
            string fd = faction.def.defName;
            foreach (HeroDef d in DefDatabase<HeroDef>.AllDefs
                         .Where(d => d.factionDef == fd && d != gone)
                         .OrderByDescending(d => d.isLeader).ThenByDescending(d => d.councilMember).ThenByDescending(d => d.rank))
            {
                HeroRecord r = RecordFor(d);
                if (r != null && (r.state == HeroState.Dead || r.state == HeroState.Missing)) continue;
                Pawn p = GetOrCreate(d);
                if (p != null && p.Faction == faction) return p;
            }
            return null;
        }

        /// Called from the Faction.TryGenerateNewLeader prefix, inside vanilla Notify_LeaderDied and before
        /// its letter and quest signal, so they name the hero who actually becomes leader.
        public Pawn SuccessorForVacantLeadership(Faction faction)
        {
            if (faction == null) return null;
            HeroDef gone = null;
            foreach (HeroRecord r in records)
            {
                if (r.pawn == null || r.state == HeroState.Dead || r.state == HeroState.Missing) continue;
                if (!r.pawn.Dead && !r.pawn.Discarded) continue;
                HeroDef d = DefDatabase<HeroDef>.GetNamedSilentFail(r.heroDefName);
                if (d == null || d.factionDef != faction.def.defName) continue;
                r.state = r.pawn.Dead ? HeroState.Dead : HeroState.Missing;
                if (r.state == HeroState.Dead) r.deathTick = Find.TickManager.TicksGame; else r.pawn = null;
                if (gone == null || d.isLeader) gone = d;
            }
            bool anyHero = DefDatabase<HeroDef>.AllDefs.Any(d => d.factionDef == faction.def.defName);
            return anyHero ? ChooseSuccessor(gone, faction) : null;
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
            var harmony = new Harmony("turbomegamod.hero.registry");
            var gc = AccessTools.Method(typeof(WorldPawnGC), "GetCriticalPawnReason");
            if (gc == null) Log.Error("[TMM] HeroRegistry: WorldPawnGC.GetCriticalPawnReason not found; living heroes are not protected from world pawn GC.");
            else harmony.Patch(gc, postfix: new HarmonyMethod(typeof(HeroRegistryPatches), "CriticalReason"));
            var pass = AccessTools.Method(typeof(WorldPawns), "PassToWorld");
            if (pass == null) Log.Error("[TMM] HeroRegistry: WorldPawns.PassToWorld not found; explicit Discard can destroy living heroes.");
            else harmony.Patch(pass, prefix: new HarmonyMethod(typeof(HeroRegistryPatches), "KeepHeroOnDiscard"));
            var lead = AccessTools.Method(typeof(Faction), "TryGenerateNewLeader");
            if (lead == null) Log.Error("[TMM] HeroRegistry: Faction.TryGenerateNewLeader not found; hero succession falls back to polling.");
            else harmony.Patch(lead, prefix: new HarmonyMethod(typeof(HeroRegistryPatches), "HeroSuccessor"));
        }

        /// Vanilla Notify_LeaderDied calls TryGenerateNewLeader and then sends the letter and the
        /// NoLongerFactionLeader quest signal; choosing the hero here makes both name the real successor.
        /// PassToWorld(pawn, Discard) destroys the pawn directly, bypassing the GC. A living registered hero
        /// is downgraded to Decide, so it stays a world pawn and the GC postfix keeps protecting it.
        public static void KeepHeroOnDiscard(Pawn pawn, ref PawnDiscardDecideMode discardMode)
        {
            if (discardMode != PawnDiscardDecideMode.Discard || pawn == null || pawn.Dead || pawn.Destroyed) return;
            HeroRegistry reg = HeroRegistry.Instance;
            if (reg != null && reg.IsHero(pawn)) discardMode = PawnDiscardDecideMode.Decide;
        }

        public static bool HeroSuccessor(Faction __instance, ref bool __result)
        {
            HeroRegistry reg = HeroRegistry.Instance;
            if (reg == null) return true;
            Pawn next = reg.SuccessorForVacantLeadership(__instance);
            if (next == null) return true;
            __instance.leader = next;
            __result = true;
            return false;
        }

        public static void CriticalReason(Pawn pawn, ref string __result)
        {
            if (__result != null || pawn == null || pawn.Dead) return;
            HeroRegistry reg = HeroRegistry.Instance;
            if (reg != null && reg.IsHero(pawn)) __result = "TMM named hero";
        }
    }
}
