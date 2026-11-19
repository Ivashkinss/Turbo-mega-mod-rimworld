using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    // =========================================================================
    // DEF EXTENSIONS & HELPERS
    // =========================================================================

    public sealed class SupernaturalGeneExtension : DefModExtension
    {
        public HediffDef passiveHediff;
    }

    public static class SupernaturalGeneUtil
    {
        public static bool HasActiveGene(Pawn pawn, string geneDefName)
        {
            if (pawn == null || pawn.genes == null || geneDefName.NullOrEmpty()) return false;
            GeneDef def = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
            return def != null && pawn.genes.HasActiveGene(def);
        }

        public static bool IsXenotype(Pawn pawn, string xenotypeDefName)
        {
            return pawn != null && pawn.genes != null && pawn.genes.Xenotype != null &&
                   pawn.genes.Xenotype.defName == xenotypeDefName;
        }

        public static bool IsAngel(Pawn pawn)
        {
            return IsXenotype(pawn, "TMM_Angel") || HasActiveGene(pawn, "TMM_Gene_AngelCelestialGrace");
        }

        public static bool IsDemon(Pawn pawn)
        {
            return IsXenotype(pawn, "TMM_Demon") || HasActiveGene(pawn, "TMM_Gene_DemonHolyBane");
        }

        public static bool IsSuperMutant(Pawn pawn)
        {
            return IsXenotype(pawn, "TMM_SuperMutant") || HasActiveGene(pawn, "TMM_Gene_MutantFevMusculature");
        }

        public static bool IsGhoul(Pawn pawn)
        {
            return IsXenotype(pawn, "TMM_Ghoul") || HasActiveGene(pawn, "TMM_Gene_GhoulRadHealing");
        }

        public static bool IsInRadiation(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return false;
            if (pawn.Position.IsPolluted(pawn.Map)) return true; // CHECK API
            if (pawn.Map.gameConditionManager != null &&
                pawn.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Heals up to maxAmount total injury severity across non-permanent Hediff_Injury entries.
        /// Returns total severity healed.
        /// </summary>
        public static float HealInjuries(Pawn pawn, float maxAmount)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null || maxAmount <= 0f)
            {
                return 0f;
            }

            float remaining = maxAmount;
            List<Hediff> hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
            for (int i = 0; i < hediffs.Count && remaining > 0f; i++)
            {
                Hediff_Injury inj = hediffs[i] as Hediff_Injury;
                if (inj == null || inj.IsPermanent()) continue;
                float heal = Mathf.Min(remaining, inj.Severity);
                if (heal > 0f)
                {
                    inj.Heal(heal);
                    remaining -= heal;
                }
            }
            return maxAmount - remaining;
        }

        public static void StripMimicry(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return;
            HediffDef mimicDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_PredatoryMimicry");
            if (mimicDef == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(mimicDef);
            if (h != null)
            {
                pawn.health.RemoveHediff(h);
            }
        }
    }

    // =========================================================================
    // PASSIVE GENES (BLEED FACTOR, BULKY HUNGER, RAD REGEN, GHOUL RAD HEALING)
    // =========================================================================

    /// <summary>
    /// Gene that maintains a permanent hidden passive Hediff while active (used for
    /// Celestial Grace totalBleedFactor=0.2 and Bulky FEV Physiology hungerRateFactor=1.35).
    /// </summary>
    public sealed class Gene_SupernaturalPassive : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            EnsurePassiveHediff();
        }

        public override void PostRemove()
        {
            base.PostRemove();
            RemovePassiveHediff();
        }

        public override void Tick() // CHECK API (in 1.6/1.7 check Tick vs TickInterval)
        {
            base.Tick();
            if (pawn != null && pawn.IsHashIntervalTick(600))
            {
                if (Active) EnsurePassiveHediff();
                else RemovePassiveHediff();
            }
        }

        private void EnsurePassiveHediff()
        {
            if (pawn == null || pawn.Dead || pawn.health == null || def == null) return;
            SupernaturalGeneExtension ext = def.GetModExtension<SupernaturalGeneExtension>();
            if (ext == null || ext.passiveHediff == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(ext.passiveHediff) == null)
            {
                pawn.health.AddHediff(ext.passiveHediff);
            }
        }

        private void RemovePassiveHediff()
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null || def == null) return;
            SupernaturalGeneExtension ext = def.GetModExtension<SupernaturalGeneExtension>();
            if (ext == null || ext.passiveHediff == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(ext.passiveHediff);
            if (h != null) pawn.health.RemoveHediff(h);
        }
    }

    /// <summary>
    /// Super Mutant Irradiated Metabolism: grants +150% wound healing and +6 mood while on
    /// polluted ground or during Toxic Fallout.
    /// </summary>
    public sealed class Gene_MutantRadRegeneration : Gene
    {
        public override void Tick() // CHECK API
        {
            base.Tick();
            if (pawn == null || pawn.Dead || !pawn.IsHashIntervalTick(250)) return;
            RefreshRadiationBuff();
        }

        public void RefreshRadiationBuff()
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;
            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_IrradiatedMetabolism");
            if (buffDef == null) return;

            bool shouldHave = Active && SupernaturalGeneUtil.IsInRadiation(pawn);
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(buffDef);
            if (shouldHave && existing == null)
            {
                pawn.health.AddHediff(buffDef);
            }
            else if (!shouldHave && existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }
    }

    /// <summary>
    /// Wasteland Ghoul Necrotic Radiation Absorption: heals 15 wound severity every 4 hours
    /// while on polluted ground or under Toxic Fallout.
    /// </summary>
    public sealed class Gene_GhoulRadHealing : Gene
    {
        private const int HealIntervalTicks = 10000; // 4 in-game hours
        private const float HealAmount = 15f;
        private int ticksInRad;

        public override void Tick() // CHECK API
        {
            base.Tick();
            if (!Active || pawn == null || pawn.Dead || !pawn.IsHashIntervalTick(250)) return;
            if (!SupernaturalGeneUtil.IsInRadiation(pawn)) return;

            ticksInRad += 250;
            if (ticksInRad >= HealIntervalTicks)
            {
                ticksInRad -= HealIntervalTicks;
                ApplyRadiationHeal();
            }
        }

        public float ApplyRadiationHeal()
        {
            if (pawn == null || pawn.Dead || !Active) return 0f;
            float healed = SupernaturalGeneUtil.HealInjuries(pawn, HealAmount);
            if (healed > 0f && pawn.Spawned && pawn.Map != null)
            {
                FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.Map); // CHECK API
            }
            return healed;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksInRad, "ticksInRad", 0);
        }
    }

    // =========================================================================
    // ABILITY EFFECTS
    // =========================================================================

    // 1. Angel: Healing Touch
    public sealed class CompProperties_AbilityHealingTouch : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityHealingTouch()
        {
            compClass = typeof(CompAbilityEffect_HealingTouch);
        }
    }

    public sealed class CompAbilityEffect_HealingTouch : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn p = target.Pawn;
            if (p == null || p.Dead || !p.RaceProps.IsFlesh)
            {
                if (throwMessages) Messages.Message("Target must be a living flesh creature.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn p = target.Pawn;
            if (p == null || p.Dead || p.health == null || p.health.hediffSet == null) return;

            int woundsToClose = Rand.RangeInclusive(3, 4);
            int closed = 0;

            List<Hediff> snapshot = new List<Hediff>(p.health.hediffSet.hediffs);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Hediff h = snapshot[i];
                if (h == null || h is Hediff_MissingPart) continue;

                Hediff_Injury inj = h as Hediff_Injury;
                if (inj != null && !inj.IsPermanent() && closed < woundsToClose)
                {
                    inj.Heal(inj.Severity + 1f);
                    closed++;
                    continue;
                }

                if (IsInfectionOrToxin(h))
                {
                    h.Severity = h.Severity * 0.5f;
                }
            }

            if (p.Spawned && p.Map != null)
            {
                FleckMaker.ThrowLightningGlow(p.DrawPos, p.Map, 1.2f); // CHECK API
            }
        }

        private static bool IsInfectionOrToxin(Hediff h)
        {
            if (h == null || h.def == null) return false;
            if (h.def == HediffDefOf.WoundInfection || h.def == HediffDefOf.ToxicBuildup) return true;
            HediffWithComps hwc = h as HediffWithComps;
            if (hwc != null && hwc.TryGetComp<HediffComp_Immunizable>() != null) return true;
            return false;
        }
    }

    // 2. Angel: True Visage
    public sealed class CompProperties_AbilityTrueVisage : CompProperties_AbilityEffect
    {
        public float radius = 5.0f;

        public CompProperties_AbilityTrueVisage()
        {
            compClass = typeof(CompAbilityEffect_TrueVisage);
        }
    }

    public sealed class CompAbilityEffect_TrueVisage : CompAbilityEffect
    {
        private CompProperties_AbilityTrueVisage Props
        {
            get { return (CompProperties_AbilityTrueVisage)props; }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            Map map = caster.Map;
            float r = Props.radius;
            HediffDef burnDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_RetinalBurn");
            FleckMaker.ThrowLightningGlow(caster.DrawPos, map, 2.5f); // CHECK API

            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == caster || p.Dead || p.Destroyed) continue;
                if (!p.RaceProps.IsFlesh) continue;
                if (SupernaturalGeneUtil.IsAngel(p)) continue;
                if (p.Position.DistanceTo(caster.Position) > r) continue;
                if (!GenSight.LineOfSight(caster.Position, p.Position, map)) continue;

                // Blind targets are unaffected by visual radiance
                if (p.health != null && p.health.capacities != null &&
                    p.health.capacities.GetLevel(PawnCapacityDefOf.Sight) <= 0.01f)
                {
                    continue;
                }

                if (burnDef != null && p.health != null)
                {
                    p.health.AddHediff(burnDef);
                }
                if (p.stances != null && p.stances.stunner != null)
                {
                    p.stances.stunner.StunFor(180, caster); // CHECK API
                }
            }
        }
    }

    // 3. Demon: Black Smoke Shift
    public sealed class CompProperties_AbilityBlackSmokeShift : CompProperties_AbilityEffect
    {
        public float stunRadius = 2.0f;
        public int stunTicks = 120;

        public CompProperties_AbilityBlackSmokeShift()
        {
            compClass = typeof(CompAbilityEffect_BlackSmokeShift);
        }
    }

    public sealed class CompAbilityEffect_BlackSmokeShift : CompAbilityEffect
    {
        private CompProperties_AbilityBlackSmokeShift Props
        {
            get { return (CompProperties_AbilityBlackSmokeShift)props; }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return false;
            IntVec3 cell = target.Cell;
            if (!cell.InBounds(caster.Map) || !cell.Standable(caster.Map) || cell.Fogged(caster.Map))
            {
                if (throwMessages) Messages.Message("Cannot shift to an impassable or fogged cell.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            Map map = caster.Map;
            IntVec3 destCell = target.Cell;
            if (!destCell.InBounds(map) || !destCell.Standable(map)) return;

            // NOTE: Devil's Trap & Enochian warded wall checks will hook in here once those buildings exist.
            FleckMaker.ThrowSmoke(caster.DrawPos, map, 2.2f); // CHECK API

            caster.Position = destCell;
            caster.Notify_Teleported(true, true); // CHECK API

            FleckMaker.ThrowSmoke(caster.DrawPos, map, 2.5f); // CHECK API

            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == caster || p.Dead || p.Destroyed) continue;
                if (!p.HostileTo(caster)) continue;
                if (p.Position.DistanceTo(destCell) > Props.stunRadius) continue;

                if (p.stances != null && p.stances.stunner != null)
                {
                    p.stances.stunner.StunFor(Props.stunTicks, caster); // CHECK API
                }
            }
        }
    }

    // 4. Super Mutant: Mutant War Roar
    public sealed class CompProperties_AbilityMutantWarRoar : CompProperties_AbilityEffect
    {
        public float radius = 6.0f;

        public CompProperties_AbilityMutantWarRoar()
        {
            compClass = typeof(CompAbilityEffect_MutantWarRoar);
        }
    }

    public sealed class CompAbilityEffect_MutantWarRoar : CompAbilityEffect
    {
        private CompProperties_AbilityMutantWarRoar Props
        {
            get { return (CompProperties_AbilityMutantWarRoar)props; }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_WarRoarAdrenaline");
            HediffDef fearDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_WarRoarFear");

            if (buffDef != null && caster.health != null)
            {
                caster.health.AddHediff(buffDef);
            }

            Map map = caster.Map;
            FleckMaker.ThrowDustPuffThick(caster.DrawPos, map, 2.2f, new Color(0.55f, 0.75f, 0.35f)); // CHECK API

            if (fearDef == null) return;

            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == caster || p.Dead || p.Destroyed) continue;
                if (!p.HostileTo(caster)) continue;
                if (!p.RaceProps.IsFlesh) continue;
                if (SupernaturalGeneUtil.IsAngel(p) || SupernaturalGeneUtil.IsDemon(p) ||
                    SupernaturalGeneUtil.IsGhoul(p) || SupernaturalGeneUtil.IsSuperMutant(p))
                {
                    continue;
                }
                if (p.Position.DistanceTo(caster.Position) > Props.radius) continue;

                if (p.health != null)
                {
                    p.health.AddHediff(fearDef);
                }
            }
        }
    }

    // 5. Ghoul: Glowing One's Pulse
    public sealed class CompProperties_AbilityGlowingPulse : CompProperties_AbilityEffect
    {
        public float radius = 4.5f;

        public CompProperties_AbilityGlowingPulse()
        {
            compClass = typeof(CompAbilityEffect_GlowingPulse);
        }
    }

    public sealed class CompAbilityEffect_GlowingPulse : CompAbilityEffect
    {
        private CompProperties_AbilityGlowingPulse Props
        {
            get { return (CompProperties_AbilityGlowingPulse)props; }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            Map map = caster.Map;
            FleckMaker.ThrowDustPuffThick(caster.DrawPos, map, 3.0f, new Color(0.25f, 0.95f, 0.25f)); // CHECK API

            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed) continue;
                if (p.Position.DistanceTo(caster.Position) > Props.radius) continue;

                if (SupernaturalGeneUtil.IsGhoul(p) || SupernaturalGeneUtil.IsSuperMutant(p))
                {
                    if (!p.HostileTo(caster))
                    {
                        SupernaturalGeneUtil.HealInjuries(p, 25f);
                    }
                    continue;
                }

                if (!p.RaceProps.IsFlesh) continue;

                float toxRes = Mathf.Clamp01(p.GetStatValue(StatDefOf.ToxicResistance)); // CHECK API
                float delta = 0.25f * (1f - toxRes);
                if (delta > 0.001f)
                {
                    HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, delta);
                }
            }
        }
    }

    // 6. Leviathan: Abyssal Devouring Maw
    public sealed class CompProperties_AbilityDevourMaw : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityDevourMaw()
        {
            compClass = typeof(CompAbilityEffect_DevourMaw);
        }
    }

    public sealed class CompAbilityEffect_DevourMaw : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn p = target.Pawn;
            if (p == null || p.Dead || !p.RaceProps.IsFlesh)
            {
                if (throwMessages) Messages.Message("Target must be a living creature of flesh.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;
            if (caster == null || victim == null || victim.Dead || !victim.RaceProps.IsFlesh) return;

            // Unhinging the maw instantly breaks Predatory Mimicry
            SupernaturalGeneUtil.StripMimicry(caster);

            DamageInfo dinfo = new DamageInfo(DamageDefOf.Bite, 45f, 1.0f, -1f, caster);
            victim.TakeDamage(dinfo);

            if (caster.needs != null && caster.needs.food != null)
            {
                caster.needs.food.CurLevelPercentage = Mathf.Min(1f, caster.needs.food.CurLevelPercentage + 0.40f);
            }

            SupernaturalGeneUtil.HealInjuries(caster, 20f);
        }
    }

    // 7. Leviathan: Predatory DNA Mimicry (simplified: copies skin color + hair only, restores on removal)
    public sealed class CompProperties_AbilityPredatoryMimicry : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityPredatoryMimicry()
        {
            compClass = typeof(CompAbilityEffect_PredatoryMimicry);
        }
    }

    public sealed class CompAbilityEffect_PredatoryMimicry : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn donor = ExtractHumanlikeDonor(target);
            if (donor == null || donor == parent.pawn)
            {
                if (throwMessages) Messages.Message("Target must be another living humanoid or humanoid corpse.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Pawn donor = ExtractHumanlikeDonor(target);
            if (caster == null || donor == null || caster.story == null || donor.story == null) return;

            // Remove any existing mimicry first so original appearance is restored cleanly before re-capturing
            SupernaturalGeneUtil.StripMimicry(caster);

            HediffDef mimicDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_PredatoryMimicry");
            if (mimicDef == null || caster.health == null) return;

            HediffWithComps h = HediffMaker.MakeHediff(mimicDef, caster) as HediffWithComps;
            if (h == null) return;

            caster.health.AddHediff(h);
            HediffComp_PredatoryMimicry comp = h.TryGetComp<HediffComp_PredatoryMimicry>();
            if (comp != null)
            {
                comp.CaptureAndApplyMimicry(donor);
            }
        }

        private static Pawn ExtractHumanlikeDonor(LocalTargetInfo target)
        {
            Pawn p = target.Pawn;
            if (p != null && p.RaceProps != null && p.RaceProps.Humanlike) return p;

            Corpse c = target.Thing as Corpse;
            if (c != null && c.InnerPawn != null && c.InnerPawn.RaceProps != null && c.InnerPawn.RaceProps.Humanlike)
            {
                return c.InnerPawn;
            }
            return null;
        }
    }

    public sealed class HediffCompProperties_PredatoryMimicry : HediffCompProperties
    {
        public HediffCompProperties_PredatoryMimicry()
        {
            compClass = typeof(HediffComp_PredatoryMimicry);
        }
    }

    public sealed class HediffComp_PredatoryMimicry : HediffComp
    {
        private bool savedOriginal;
        private bool origHasSkinOverride;
        private Color origSkinOverride;
        private string origHairDefName;
        private Color origHairColor;

        public void CaptureAndApplyMimicry(Pawn donor)
        {
            if (Pawn == null || Pawn.story == null || donor == null || donor.story == null) return;

            if (!savedOriginal)
            {
                origHasSkinOverride = Pawn.story.skinColorOverride.HasValue; // CHECK API
                origSkinOverride = origHasSkinOverride ? Pawn.story.skinColorOverride.Value : Color.white;
                origHairDefName = Pawn.story.hairDef != null ? Pawn.story.hairDef.defName : null;
                origHairColor = Pawn.story.HairColor; // CHECK API
                savedOriginal = true;
            }

            Pawn.story.skinColorOverride = donor.story.SkinColor; // CHECK API
            if (donor.story.hairDef != null)
            {
                Pawn.story.hairDef = donor.story.hairDef;
            }
            Pawn.story.HairColor = donor.story.HairColor; // CHECK API

            RefreshPawnGraphics(Pawn);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (!savedOriginal || Pawn == null || Pawn.story == null) return;

            if (origHasSkinOverride)
            {
                Pawn.story.skinColorOverride = origSkinOverride;
            }
            else
            {
                Pawn.story.skinColorOverride = null;
            }

            if (!origHairDefName.NullOrEmpty())
            {
                HairDef restoredHair = DefDatabase<HairDef>.GetNamedSilentFail(origHairDefName);
                if (restoredHair != null) Pawn.story.hairDef = restoredHair;
            }
            Pawn.story.HairColor = origHairColor;

            savedOriginal = false;
            RefreshPawnGraphics(Pawn);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref savedOriginal, "savedOriginal", false);
            Scribe_Values.Look(ref origHasSkinOverride, "origHasSkinOverride", false);
            Scribe_Values.Look(ref origSkinOverride, "origSkinOverride", Color.white);
            Scribe_Values.Look(ref origHairDefName, "origHairDefName", null);
            Scribe_Values.Look(ref origHairColor, "origHairColor", Color.black);
        }

        private static void RefreshPawnGraphics(Pawn p)
        {
            if (p != null && p.Drawer != null && p.Drawer.renderer != null)
            {
                p.Drawer.renderer.SetAllGraphicsDirty(); // CHECK API
            }
        }
    }

    // =========================================================================
    // MENTAL STATE: FERAL GHOUL FRENZY
    // =========================================================================

    public sealed class MentalState_FeralGhoul : MentalState_Berserk
    {
        public override bool ForceHostileTo(Thing t)
        {
            Pawn other = t as Pawn;
            if (other != null && SupernaturalGeneUtil.IsGhoul(other)) return false;
            return base.ForceHostileTo(t);
        }

        public override bool ForceHostileTo(Faction f)
        {
            return true;
        }
    }

    // =========================================================================
    // HARMONY PATCHES (BORAX ALLERGY DEBUFF, FERAL GHOUL BREAK, APPAREL STUB)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class SupernaturalGenesHarmony
    {
        internal static bool SuppressFeralHook;

        static SupernaturalGenesHarmony()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.supernaturalgenes");
            harmony.PatchAll(typeof(SupernaturalGenesHarmony).Assembly);
        }
    }

    /// <summary>
    /// Applies TMM_Hediff_BoraxBurn and breaks Predatory Mimicry whenever a Leviathan with
    /// TMM_Gene_LeviathanBoraxAllergy takes TMM_BoraxBurn damage.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")] // CHECK API
    public static class Patch_Pawn_PostApplyDamage_Borax
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || __instance.Dead || dinfo.Def == null) return;
            if (dinfo.Def.defName != "TMM_BoraxBurn") return;
            if (!SupernaturalGeneUtil.HasActiveGene(__instance, "TMM_Gene_LeviathanBoraxAllergy")) return;

            SupernaturalGeneUtil.StripMimicry(__instance);

            HediffDef boraxHediff = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_BoraxBurn");
            if (boraxHediff != null && __instance.health != null)
            {
                Hediff existing = __instance.health.hediffSet.GetFirstHediffOfDef(boraxHediff);
                if (existing != null)
                {
                    __instance.health.RemoveHediff(existing);
                }
                __instance.health.AddHediff(boraxHediff);
            }
        }
    }

    /// <summary>
    /// 25% chance to replace an Extreme Mental Break on a ghoul with TMM_Gene_GhoulFeralRisk
    /// into TMM_MentalState_FeralGhoul.
    /// </summary>
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")] // CHECK API
    public static class Patch_MentalStateHandler_FeralGhoul
    {
        public static void Prefix(MentalStateHandler __instance, ref MentalStateDef stateDef, bool causedByMood)
        {
            if (SupernaturalGenesHarmony.SuppressFeralHook) return;
            if (!causedByMood || stateDef == null) return;
            if (stateDef.defName == "TMM_MentalState_FeralGhoul") return;

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null || !SupernaturalGeneUtil.HasActiveGene(pawn, "TMM_Gene_GhoulFeralRisk")) return;
            if (!IsExtremeBreakState(stateDef)) return;

            if (Rand.Value < 0.25f)
            {
                MentalStateDef feralDef = DefDatabase<MentalStateDef>.GetNamedSilentFail("TMM_MentalState_FeralGhoul");
                if (feralDef != null)
                {
                    stateDef = feralDef;
                }
            }
        }

        private static bool IsExtremeBreakState(MentalStateDef stateDef)
        {
            List<MentalBreakDef> breaks = DefDatabase<MentalBreakDef>.AllDefsListForReading;
            for (int i = 0; i < breaks.Count; i++)
            {
                MentalBreakDef mb = breaks[i];
                if (mb != null && mb.mentalState == stateDef && mb.intensity == MentalBreakIntensity.Extreme)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /*
     * STUB (deferred per design decision):
     * When Super Mutant apparel defs are added to the mod, patch ApparelUtility / Pawn_ApparelTracker
     * to prevent pawns with TMM_Gene_MutantBulkyConstraints from wearing standard human-sized apparel
     * unless the apparel carries a mutant-fit tag.
     */
}
