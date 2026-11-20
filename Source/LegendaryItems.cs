using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    // =========================================================================
    // LEGENDARY UTILITY & BOSS / SUPERNATURAL IDENTIFICATION
    // =========================================================================

    public static class LegendaryUtil
    {
        public const string HeroLucifer = "TMM_Hero_lucifer";
        public const string HeroMichael = "TMM_Hero_michael";
        public const string HeroDickRoman = "TMM_Hero_dick_roman";

        private static string TryGetRegisteredHeroDefName(Pawn pawn)
        {
            if (pawn == null || HeroRegistry.Instance == null) return null;
            foreach (HeroRecord r in HeroRegistry.Instance.Records)
            {
                if (r != null && r.pawn == pawn) return r.heroDefName;
            }
            return null;
        }

        public static bool IsImmuneBoss(Pawn pawn)
        {
            if (pawn == null) return false;
            return IsHero(pawn, HeroLucifer) || IsHero(pawn, HeroMichael) || IsHero(pawn, HeroDickRoman);
        }

        public static bool IsDickRoman(Pawn pawn)
        {
            return IsHero(pawn, HeroDickRoman);
        }

        /// <summary>
        /// Checks whether a pawn corresponds to a specific HeroDef without spawning a new pawn.
        /// Uses HeroRegistry reflection first, falling back to exact character name matching.
        /// </summary>
        public static bool IsHero(Pawn pawn, string heroDefName)
        {
            if (pawn == null || heroDefName.NullOrEmpty()) return false;

            string foundDefName = TryGetRegisteredHeroDefName(pawn);
            if (!foundDefName.NullOrEmpty())
            {
                return string.Equals(foundDefName, heroDefName, StringComparison.OrdinalIgnoreCase);
            }

            // Fallback name checks (works in probes or if HeroRegistry uses a different internal map)
            if (pawn.Name != null)
            {
                string full = pawn.Name.ToStringFull;
                string shortName = pawn.Name.ToStringShort;
                if (heroDefName == HeroDickRoman)
                {
                    if (ContainsIgnoreCase(full, "Dick Roman") || ContainsIgnoreCase(shortName, "Dick Roman")) return true;
                }
                else if (heroDefName == HeroLucifer)
                {
                    if (string.Equals(shortName, "Lucifer", StringComparison.OrdinalIgnoreCase) || ContainsIgnoreCase(full, "Lucifer")) return true;
                }
                else if (heroDefName == HeroMichael)
                {
                    if (ContainsIgnoreCase(full, "Archangel Michael") || string.Equals(shortName, "Michael", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }

            return false;
        }



        public static bool IsLeviathan(Pawn pawn)
        {
            if (pawn == null) return false;
            if (IsDickRoman(pawn)) return true;
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == "TMM_Leviathan") return true;
                GeneDef boraxGene = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_LeviathanBoraxAllergy");
                if (boraxGene != null && pawn.genes.HasActiveGene(boraxGene)) return true;
            }
            return false;
        }

        public static bool IsSupernaturalForColt(Pawn pawn)
        {
            if (pawn == null) return false;

            if (ModsConfig.BiotechActive && pawn.genes != null && pawn.genes.Xenotype != null)
            {
                string x = pawn.genes.Xenotype.defName;
                if (x == "TMM_Demon" || x == "TMM_Angel" || x == "TMM_Leviathan" || x == "TMM_Ghoul")
                {
                    return true;
                }
            }

            if (ModsConfig.AnomalyActive && pawn.RaceProps != null)
            {
                if (pawn.RaceProps.IsAnomalyEntity) return true; // CHECK API
            }

            return false;
        }

        public static bool IsGhoulOrSuperMutant(Pawn pawn)
        {
            if (pawn == null || !ModsConfig.BiotechActive || pawn.genes == null || pawn.genes.Xenotype == null) return false;
            string x = pawn.genes.Xenotype.defName;
            return x == "TMM_Ghoul" || x == "TMM_SuperMutant";
        }

        public static bool IsMechanicalOrSynth(Pawn pawn)
        {
            if (pawn == null || pawn.RaceProps == null) return false;
            if (!pawn.RaceProps.IsFlesh || pawn.RaceProps.IsMechanoid) return true;
            if (pawn.Faction != null && pawn.Faction.def != null && pawn.Faction.def.defName == "TMM_InstituteFaction")
            {
                if (pawn.kindDef != null && ContainsIgnoreCase(pawn.kindDef.defName, "Synth")) return true;
            }
            return false;
        }

        public static bool HasBoraxVulnerabilityDebuff(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            HediffDef h1 = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_BoraxBurn");
            if (h1 != null && pawn.health.hediffSet.GetFirstHediffOfDef(h1) != null) return true;
            HediffDef h2 = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_BoraxAcidBurn");
            if (h2 != null && pawn.health.hediffSet.GetFirstHediffOfDef(h2) != null) return true;
            return false;
        }

        public static void ApplyBoraxDebuffs(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;
            if (!IsLeviathan(pawn)) return;

            RefreshHediff(pawn, "TMM_Hediff_BoraxBurn");
            RefreshHediff(pawn, "TMM_Hediff_BoraxAcidBurn");

            // Strip Leviathan Predatory Mimicry if active
            HediffDef mimicDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_PredatoryMimicry");
            if (mimicDef != null)
            {
                Hediff mimic = pawn.health.hediffSet.GetFirstHediffOfDef(mimicDef);
                if (mimic != null) pawn.health.RemoveHediff(mimic);
            }
        }

        public static void EnsureDickRomanInvulnerability(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;
            if (!IsDickRoman(pawn)) return;

            HediffDef invDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_DickRomanInvulnerable");
            if (invDef == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(invDef) == null)
            {
                pawn.health.AddHediff(invDef);
            }
        }

        public static void EnsureMarkOfCain(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;
            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_MarkOfCain");
            if (markDef == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(markDef) == null)
            {
                pawn.health.AddHediff(markDef);
            }
        }

        public static bool IsWearingDef(Pawn pawn, string apparelDefName)
        {
            if (pawn == null || pawn.apparel == null || apparelDefName.NullOrEmpty()) return false;
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] != null && worn[i].def != null && worn[i].def.defName == apparelDefName) return true;
            }
            return false;
        }

        public static bool IsWieldingDef(Pawn pawn, string weaponDefName)
        {
            return pawn != null && pawn.equipment != null && pawn.equipment.Primary != null &&
                   pawn.equipment.Primary.def != null && pawn.equipment.Primary.def.defName == weaponDefName;
        }

        public static float HealOneOrMoreInjuries(Pawn pawn, float maxHeal)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null || maxHeal <= 0f) return 0f;
            float remaining = maxHeal;
            List<Hediff> snapshot = new List<Hediff>(pawn.health.hediffSet.hediffs);
            for (int i = 0; i < snapshot.Count && remaining > 0f; i++)
            {
                Hediff_Injury inj = snapshot[i] as Hediff_Injury;
                if (inj == null || inj.IsPermanent()) continue;
                float amount = Mathf.Min(remaining, inj.Severity);
                if (amount > 0f)
                {
                    inj.Heal(amount);
                    remaining -= amount;
                }
            }
            return maxHeal - remaining;
        }

        private static void RefreshHediff(Pawn pawn, string hediffDefName)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
            pawn.health.AddHediff(def);
        }

        private static bool ContainsIgnoreCase(string hay, string needle)
        {
            return !hay.NullOrEmpty() && !needle.NullOrEmpty() &&
                   hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    // =========================================================================
    // WORLD COMPONENT: LEGENDARY REGISTRY (UNIQUE PER WORLD)
    // =========================================================================

    /// <summary>
    /// Tracks which unique legendary items currently exist in the world.
    /// Destroyed legendaries are automatically unregistered so they can be granted again.
    /// Also periodically ensures any existing Dick Roman pawn carries his invulnerability Hediff.
    /// </summary>
    public sealed class LegendaryRegistry : WorldComponent
    {
        private Dictionary<string, string> grantedThingIds = new Dictionary<string, string>();

        public LegendaryRegistry(World world) : base(world)
        {
        }

        public static LegendaryRegistry Instance
        {
            get { return Find.World != null ? Find.World.GetComponent<LegendaryRegistry>() : null; }
        }

        public bool ExistsInWorld(ThingDef def)
        {
            if (def == null || grantedThingIds == null) return false;
            return grantedThingIds.ContainsKey(def.defName);
        }

        /// <summary>
        /// Creates the legendary item and drop-pods it to the given map only if no instance
        /// of this ThingDef currently exists in the world.
        /// </summary>
        public bool TryGrant(ThingDef def, Map map)
        {
            if (def == null || map == null) return false;
            if (grantedThingIds == null) grantedThingIds = new Dictionary<string, string>();
            if (grantedThingIds.ContainsKey(def.defName)) return false;

            ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            Thing item = ThingMaker.MakeThing(def, stuff);
            if (item == null) return false;

            grantedThingIds[def.defName] = item.ThingID;

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map); // CHECK API
            List<Thing> list = new List<Thing>();
            list.Add(item);
            DropPodUtility.DropThingsNear(dropSpot, map, list, 110, false, false, true, false, false, null);
            return true;
        }

        public void RegisterExisting(Thing thing)
        {
            if (thing == null || thing.def == null || thing.Destroyed) return;
            if (thing.def.GetCompProperties<CompProperties_LegendaryItem>() == null) return;
            if (grantedThingIds == null) grantedThingIds = new Dictionary<string, string>();
            if (!grantedThingIds.ContainsKey(thing.def.defName))
            {
                grantedThingIds[thing.def.defName] = thing.ThingID;
            }
        }

        public void NotifyDestroyed(Thing thing)
        {
            if (thing == null || thing.def == null || grantedThingIds == null) return;
            string trackedId;
            if (grantedThingIds.TryGetValue(thing.def.defName, out trackedId) && trackedId == thing.ThingID)
            {
                grantedThingIds.Remove(thing.def.defName);
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 250 != 0) return;

            // Guarantee already-existing Dick Roman pawns receive invulnerability Hediff
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                if (map == null || map.mapPawns == null) continue;
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (LegendaryUtil.IsDickRoman(pawns[i]))
                    {
                        LegendaryUtil.EnsureDickRomanInvulnerability(pawns[i]);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref grantedThingIds, "grantedThingIds", LookMode.Value, LookMode.Value);
            if (grantedThingIds == null) grantedThingIds = new Dictionary<string, string>();
        }
    }

    // =========================================================================
    // THING COMPS (LEGENDARY MARKER, LIMITED CHARGES, PROTECTOR, MARK OF CAIN)
    // =========================================================================

    public sealed class CompProperties_LegendaryItem : CompProperties
    {
        public CompProperties_LegendaryItem()
        {
            compClass = typeof(CompLegendaryItem);
        }
    }

    public sealed class CompLegendaryItem : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (LegendaryRegistry.Instance != null)
            {
                LegendaryRegistry.Instance.RegisterExisting(parent);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (LegendaryRegistry.Instance != null)
            {
                LegendaryRegistry.Instance.NotifyDestroyed(parent);
            }
        }
    }

    public sealed class CompProperties_LimitedCharges : CompProperties
    {
        public int maxCharges = 13;
        public bool allowReload = false;
        public ThingDef ammoDef;
        public int ammoCountPerCharge = 1;

        public CompProperties_LimitedCharges()
        {
            compClass = typeof(CompLimitedCharges);
        }
    }

    public sealed class CompLimitedCharges : ThingComp
    {
        private int charges = -1;

        public CompProperties_LimitedCharges Props
        {
            get { return (CompProperties_LimitedCharges)props; }
        }

        public int Charges
        {
            get
            {
                if (charges < 0) charges = Props.maxCharges;
                return charges;
            }
        }

        public bool CanFire
        {
            get { return Charges > 0; }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            charges = Props.maxCharges;
        }

        public bool ConsumeCharge()
        {
            if (Charges <= 0) return false;
            charges = Charges - 1;
            return true;
        }

        public bool TryReloadFromInventory(Pawn wielder)
        {
            if (!Props.allowReload || Props.ammoDef == null || wielder == null || Charges >= Props.maxCharges) return false;
            if (wielder.Map == null) return false;

            int need = Mathf.Max(1, Props.ammoCountPerCharge);
            int available = OutpostPodLogistics.CountResource(wielder.Map, Props.ammoDef);
            if (available < need) return false;

            int taken = OutpostPodLogistics.DeductResource(wielder.Map, Props.ammoDef, need);
            if (taken >= need)
            {
                charges = Mathf.Min(Props.maxCharges, Charges + 1);
                return true;
            }
            return false;
        }

        public override string TransformLabel(string label)
        {
            return string.Format("{0} [{1}/{2}]", label, Charges, Props.maxCharges);
        }

        public override string CompInspectStringExtra()
        {
            return string.Format("Charges: {0} / {1}", Charges, Props.maxCharges);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Props.allowReload && Props.ammoDef != null && Charges < Props.maxCharges)
            {
                Command_Action reloadCmd = new Command_Action();
                reloadCmd.defaultLabel = string.Format("Reload +1 ({0} {1})", Props.ammoCountPerCharge, Props.ammoDef.label);
                reloadCmd.defaultDesc = "Reload one charge using colony stockpiles.";
                reloadCmd.icon = TexCommand.RearmTrap; // CHECK API
                reloadCmd.action = delegate
                {
                    Pawn holder = parent.ParentHolder is Pawn_EquipmentTracker ? ((Pawn_EquipmentTracker)parent.ParentHolder).pawn : null;
                    if (!TryReloadFromInventory(holder))
                    {
                        Messages.Message("Not enough " + Props.ammoDef.label + " in stockpiles.", MessageTypeDefOf.RejectInput, false);
                    }
                };
                yield return reloadCmd;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref charges, "charges", -1);
            if (charges < 0) charges = Props.maxCharges;
        }
    }

    public sealed class CompProperties_ProtectorAutoInjector : CompProperties
    {
        public int cooldownTicks = 30000; // 12 hours
        public float healthThreshold = 0.30f;

        public CompProperties_ProtectorAutoInjector()
        {
            compClass = typeof(CompProtectorAutoInjector);
        }
    }

    public sealed class CompProtectorAutoInjector : ThingComp
    {
        private int nextReadyTick = -1;

        public CompProperties_ProtectorAutoInjector Props
        {
            get { return (CompProperties_ProtectorAutoInjector)props; }
        }

        public bool TryTrigger(Pawn wearer)
        {
            if (wearer == null || wearer.Dead || wearer.health == null) return false;
            int now = Find.TickManager.TicksGame;
            if (now < nextReadyTick) return false;
            if (wearer.health.summaryHealth.SummaryHealthPercent >= Props.healthThreshold) return false;

            // Tend & heal bleeding injuries
            List<Hediff> snapshot = new List<Hediff>(wearer.health.hediffSet.hediffs);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Hediff_Injury inj = snapshot[i] as Hediff_Injury;
                if (inj != null && inj.Bleeding)
                {
                    inj.Tended(0.85f, 1.0f, 0);
                    inj.Heal(Mathf.Min(inj.Severity, 4f));
                }
            }

            HediffDef stimDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_ProtectorStimpak");
            if (stimDef != null)
            {
                wearer.health.AddHediff(stimDef);
            }

            nextReadyTick = now + Props.cooldownTicks;
            if (wearer.Spawned && wearer.Map != null)
            {
                MoteMaker.ThrowText(wearer.DrawPos, wearer.Map, "AUTO-STIMPAK", Color.green, 3.0f); // CHECK API
            }
            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextReadyTick, "nextReadyTick", -1);
        }
    }

    public sealed class CompProperties_MarkOfCainBlade : CompProperties
    {
        public CompProperties_MarkOfCainBlade()
        {
            compClass = typeof(CompMarkOfCainBlade);
        }
    }

    public sealed class CompMarkOfCainBlade : ThingComp
    {
        public override void Notify_Equipped(Pawn pawn) // CHECK API
        {
            base.Notify_Equipped(pawn);
            LegendaryUtil.EnsureMarkOfCain(pawn);
        }
    }

    // =========================================================================
    // HEDIFF COMPS (MARK OF CAIN BLOODLUST, BORAX ACID DOT)
    // =========================================================================

    public sealed class HediffCompProperties_MarkOfCain : HediffCompProperties
    {
        public int peaceLimitTicks = 180000; // 3 days

        public HediffCompProperties_MarkOfCain()
        {
            compClass = typeof(HediffComp_MarkOfCain);
        }
    }

    public sealed class HediffComp_MarkOfCain : HediffComp
    {
        private int ticksSinceLastKill;

        private HediffCompProperties_MarkOfCain Props
        {
            get { return (HediffCompProperties_MarkOfCain)props; }
        }

        public int TicksSinceLastKill
        {
            get { return ticksSinceLastKill; }
        }

        public void NotifyKill()
        {
            ticksSinceLastKill = 0;
            parent.Severity = 0.1f;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn == null || Pawn.Dead || !Pawn.Spawned) return;
            if (!Pawn.IsHashIntervalTick(250)) return;

            ticksSinceLastKill += 250;
            if (ticksSinceLastKill <= Props.peaceLimitTicks)
            {
                if (parent.Severity > 0.1f) parent.Severity = 0.1f;
                return;
            }

            // Build bloodlust from 0.1 -> 1.0 over 1 day past the 3-day threshold
            int over = ticksSinceLastKill - Props.peaceLimitTicks;
            float targetSev = Mathf.Clamp(0.1f + (over / (float)GenDate.TicksPerDay) * 0.9f, 0.1f, 1.0f);
            parent.Severity = targetSev;

            if (parent.Severity >= 0.999f && !Pawn.InMentalState && Pawn.mindState != null && Pawn.mindState.mentalStateHandler != null)
            {
                Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Mark of Cain bloodlust", true, false, false, null); // CHECK API
                ticksSinceLastKill = Props.peaceLimitTicks;
                parent.Severity = 0.55f;
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                float days = ticksSinceLastKill / (float)GenDate.TicksPerDay;
                return string.Format("Days without kill: {0:0.0} / 3.0", days);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksSinceLastKill, "ticksSinceLastKill", 0);
        }
    }

    public sealed class HediffCompProperties_BoraxAcidDot : HediffCompProperties
    {
        public int tickInterval = 180;
        public float damagePerTick = 5f;

        public HediffCompProperties_BoraxAcidDot()
        {
            compClass = typeof(HediffComp_BoraxAcidDot);
        }
    }

    public sealed class HediffComp_BoraxAcidDot : HediffComp
    {
        private HediffCompProperties_BoraxAcidDot Props
        {
            get { return (HediffCompProperties_BoraxAcidDot)props; }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn == null || Pawn.Dead || !Pawn.Spawned) return;
            if (!Pawn.IsHashIntervalTick(Props.tickInterval)) return;
            if (!LegendaryUtil.IsLeviathan(Pawn)) return;

            DamageInfo dinfo = new DamageInfo(DamageDefOf.AcidBurn, Props.damagePerTick, 1.0f); // CHECK API
            Pawn.TakeDamage(dinfo);
        }
    }

    // =========================================================================
    // CUSTOM VERBS (LIMITED CHARGES, FINAL JUDGMENT HEAT, EQUALIZER RECOIL)
    // =========================================================================

    public sealed class Verb_ShootLimitedCharges : Verb_Shoot
    {
        public override bool Available()
        {
            if (!base.Available()) return false;
            CompLimitedCharges comp = EquipmentSource != null ? EquipmentSource.GetComp<CompLimitedCharges>() : null;
            return comp == null || comp.CanFire;
        }

        protected override bool TryCastShot()
        {
            CompLimitedCharges comp = EquipmentSource != null ? EquipmentSource.GetComp<CompLimitedCharges>() : null;
            if (comp != null && !comp.CanFire) return false;

            bool fired = base.TryCastShot();
            if (fired && comp != null)
            {
                comp.ConsumeCharge();
            }
            return fired;
        }
    }

    public sealed class Verb_ShootFinalJudgment : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            bool fired = base.TryCastShot();
            if (fired && CasterPawn != null && !CasterPawn.Dead && CasterPawn.health != null)
            {
                HediffDef heatDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_FinalJudgmentHeat");
                if (heatDef != null)
                {
                    HealthUtility.AdjustSeverity(CasterPawn, heatDef, 0.03f);
                    Hediff h = CasterPawn.health.hediffSet.GetFirstHediffOfDef(heatDef);
                    if (h != null && h.Severity >= 0.99f)
                    {
                        CasterPawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, 2f, 0f, -1f, CasterPawn, null, EquipmentSource != null ? EquipmentSource.def : null));
                    }
                }
            }
            return fired;
        }
    }

    public sealed class Verb_ShootEqualizer : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            bool fired = base.TryCastShot();
            if (fired && CasterPawn != null && !CasterPawn.Dead)
            {
                DamageDef recoilDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_Damage_EqualizerRecoil") ?? DamageDefOf.Burn;
                CasterPawn.TakeDamage(new DamageInfo(recoilDef, 12f, 1.0f, -1f, CasterPawn, null, EquipmentSource != null ? EquipmentSource.def : null));
            }
            return fired;
        }
    }

    // =========================================================================
    // CUSTOM PROJECTILES (EXPERIMENT 18-A, COLT, LORENZO, TESLA, EQUALIZER, BORAX)
    // =========================================================================

    public sealed class Bullet_Experiment18A : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || !victim.Dead) return;
            if (victim.Corpse != null && !victim.Corpse.Destroyed && Rand.Value < 0.25f)
            {
                IntVec3 pos = victim.Corpse.Position;
                Map m = victim.Corpse.Map;
                victim.Corpse.Destroy(DestroyMode.Vanish);
                if (m != null && pos.InBounds(m))
                {
                    FilthMaker.TryMakeFilth(pos, m, ThingDefOf.Filth_Ash, 1);
                }
            }
        }
    }

    public sealed class Bullet_TheColt : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || victim.Dead) return;

            DamageDef holyDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_HolyWater");
            if (holyDef != null && LegendaryUtil.IsSupernaturalForColt(victim))
            {
                victim.TakeDamage(new DamageInfo(holyDef, 8f, 1.0f, ExactRotation.eulerAngles.y, launcher, null, equipmentDef));
            }

            if (!victim.Dead && LegendaryUtil.IsSupernaturalForColt(victim) && !LegendaryUtil.IsImmuneBoss(victim))
            {
                DamageInfo killInfo = new DamageInfo(def.projectile.damageDef, 100f, 2.0f, ExactRotation.eulerAngles.y, launcher, null, equipmentDef);
                victim.Kill(killInfo);
            }
        }
    }

    public sealed class Projectile_LorenzoWave : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Map map = Map;
            IntVec3 center = Position;
            IntVec3 sourcePos = launcher != null ? launcher.Position : origin.ToIntVec3();

            if (map != null && center.InBounds(map))
            {
                int dmg = this.DamageAmount;
                float ap = this.ArmorPenetration;
                GenExplosion.DoExplosion(center, map, 1.9f, def.projectile.damageDef, launcher, dmg, ap, null, equipmentDef, def); // CHECK API

                List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p == null || p == launcher || p.Dead || p.Destroyed) continue;
                    if (p.Position.DistanceTo(center) > 2.5f) continue;

                    PushPawnBack(p, sourcePos, map, Rand.RangeInclusive(3, 5));
                    if (p.stances != null && p.stances.stunner != null)
                    {
                        p.stances.stunner.StunFor(120, launcher); // CHECK API
                    }
                }
            }

            base.Impact(hitThing, blockedByShield);
        }

        private static void PushPawnBack(Pawn pawn, IntVec3 fromCell, Map map, int cells)
        {
            Vector3 dir = (pawn.Position - fromCell).ToVector3();
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();

            IntVec3 best = pawn.Position;
            for (int step = 1; step <= cells; step++)
            {
                IntVec3 candidate = pawn.Position + new IntVec3(Mathf.RoundToInt(dir.x * step), 0, Mathf.RoundToInt(dir.z * step));
                if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.Impassable(map)) break;
                best = candidate;
            }

            if (best != pawn.Position)
            {
                pawn.Position = best;
                pawn.Notify_Teleported(true, true); // CHECK API
            }
        }
    }

    public sealed class Bullet_TeslaCannon : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Map map = Map;
            IntVec3 impactPos = hitThing != null ? hitThing.Position : Position;
            Pawn primaryVictim = hitThing as Pawn;

            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || map == null) return;

            if (primaryVictim != null && !primaryVictim.Dead && LegendaryUtil.IsMechanicalOrSynth(primaryVictim))
            {
                if (primaryVictim.stances != null && primaryVictim.stances.stunner != null)
                {
                    primaryVictim.stances.stunner.StunFor(240, launcher); // CHECK API
                }
            }

            // Chain lightning to up to 3 nearby hostile targets within 4 cells
            float chainDmg = this.DamageAmount * 0.7f;
            List<Pawn> candidates = new List<Pawn>();
            IReadOnlyList<Pawn> all = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < all.Count; i++)
            {
                Pawn p = all[i];
                if (p == null || p == primaryVictim || p == launcher || p.Dead || p.Destroyed) continue;
                if (launcher != null && !p.HostileTo(launcher)) continue;
                if (p.Position.DistanceTo(impactPos) <= 4.0f)
                {
                    candidates.Add(p);
                }
            }

            candidates.Sort(delegate(Pawn a, Pawn b)
            {
                return a.Position.DistanceToSquared(impactPos).CompareTo(b.Position.DistanceToSquared(impactPos));
            });

            int hits = Mathf.Min(3, candidates.Count);
            for (int i = 0; i < hits; i++)
            {
                Pawn target = candidates[i];
                FleckMaker.ThrowLightningGlow(target.DrawPos, map, 1.4f); // CHECK API
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, chainDmg, 0.50f, -1f, launcher, null, equipmentDef);
                target.TakeDamage(dinfo);

                if (!target.Dead && LegendaryUtil.IsMechanicalOrSynth(target) && target.stances != null && target.stances.stunner != null)
                {
                    target.stances.stunner.StunFor(240, launcher); // CHECK API
                }
            }
        }
    }

    public sealed class Bullet_TheEqualizer : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || victim.Dead) return;

            DamageDef holyDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_HolyWater");
            if (holyDef != null && LegendaryUtil.IsSupernaturalForColt(victim))
            {
                victim.TakeDamage(new DamageInfo(holyDef, 14f, 1.0f, ExactRotation.eulerAngles.y, launcher, null, equipmentDef));
            }
        }
    }

    public sealed class Bullet_Borax : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || victim.Dead) return;
            if (LegendaryUtil.IsLeviathan(victim))
            {
                LegendaryUtil.ApplyBoraxDebuffs(victim);
            }
        }
    }

    // =========================================================================
    // HARMONY PATCHES (EXPLICITLY REGISTERED TO AVOID PatchAll COLLISIONS)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class LegendaryItemsHarmony
    {
        static LegendaryItemsHarmony()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.legendary");

            // 1. Thing.TakeDamage Prefix: Dick Roman 99% reduction + Borax vs human near-zero
            MethodInfo takeDamage = AccessTools.Method(typeof(Thing), "TakeDamage", new Type[] { typeof(DamageInfo) });
            if (takeDamage != null)
            {
                harmony.Patch(takeDamage, prefix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Prefix_Thing_TakeDamage"));
            }

            // 2. Pawn.PostApplyDamage Postfix: One-hit blades, Protector Auto-Injector, Atom's Judgement, Alpha Predator, Borax debuff
            MethodInfo postApplyDamage = AccessTools.Method(typeof(Pawn), "PostApplyDamage", new Type[] { typeof(DamageInfo), typeof(float) }); // CHECK API
            if (postApplyDamage != null)
            {
                harmony.Patch(postApplyDamage, postfix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Postfix_Pawn_PostApplyDamage"));
            }

            // 3. Pawn.Kill Postfix: Mark of Cain kill counter reset
            MethodInfo pawnKill = AccessTools.Method(typeof(Pawn), "Kill", new Type[] { typeof(DamageInfo?), typeof(Hediff) }); // CHECK API
            if (pawnKill != null)
            {
                harmony.Patch(pawnKill, postfix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Postfix_Pawn_Kill"));
            }

            // 4. Pawn.SpawnSetup Postfix: Auto-apply Dick Roman invulnerability on spawn
            MethodInfo spawnSetup = AccessTools.Method(typeof(Pawn), "SpawnSetup", new Type[] { typeof(Map), typeof(bool) });
            if (spawnSetup != null)
            {
                harmony.Patch(spawnSetup, postfix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Postfix_Pawn_SpawnSetup"));
            }

            // 5. MentalStateHandler.TryStartMentalState Prefix: Golden Pip-Boy anti-panic + Archangel Blade anti-curse
            MethodInfo tryMental = AccessTools.Method(typeof(MentalStateHandler), "TryStartMentalState"); // CHECK API
            if (tryMental != null)
            {
                harmony.Patch(tryMental, prefix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Prefix_TryStartMentalState"));
            }

            // 6. Pawn_HealthTracker.AddHediff Prefix: Archangel Blade immunity to Infernal Whisper
            MethodInfo addHediff = AccessTools.Method(typeof(Pawn_HealthTracker), "AddHediff", new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) }); // CHECK API
            if (addHediff != null)
            {
                harmony.Patch(addHediff, prefix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Prefix_AddHediff"));
            }
        }

        public static void Prefix_Thing_TakeDamage(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn victim = __instance as Pawn;
            if (victim == null || victim.Dead) return;

            bool isBoraxDamage = (dinfo.Def != null && dinfo.Def.defName == "TMM_BoraxBurn") ||
                                 (dinfo.Weapon != null && (dinfo.Weapon.defName == "TMM_Gun_BoraxSprayer" || dinfo.Weapon.defName == "TMM_Gun_BoraxTurret"));

            // Rule 6: Borax deals near-zero damage to non-Leviathans (humans), full/amplified to Leviathans
            if (isBoraxDamage)
            {
                if (!LegendaryUtil.IsLeviathan(victim))
                {
                    dinfo.SetAmount(Mathf.Min(dinfo.Amount * 0.04f, 0.5f));
                }
                else
                {
                    // Ensure Borax hurts Leviathans heavily even if Biotech damageFactors gene isn't active
                    if (!ModsConfig.BiotechActive)
                    {
                        dinfo.SetAmount(dinfo.Amount * 3.5f);
                    }
                    // Borax bypasses Dick Roman's 99% shield so it can burn him and apply the debuff
                    return;
                }
            }

            // Rule 5: Dick Roman cuts all incoming damage by 99% unless afflicted by Borax Burn
            if (LegendaryUtil.IsDickRoman(victim))
            {
                LegendaryUtil.EnsureDickRomanInvulnerability(victim);
                if (!LegendaryUtil.HasBoraxVulnerabilityDebuff(victim))
                {
                    dinfo.SetAmount(dinfo.Amount * 0.01f);
                }
            }
        }

        public static void Postfix_Pawn_PostApplyDamage(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null) return;

            // 1. Apply Borax debuff whenever Borax damage hits a Leviathan / Dick Roman
            bool isBoraxDamage = (dinfo.Def != null && dinfo.Def.defName == "TMM_BoraxBurn") ||
                                 (dinfo.Weapon != null && (dinfo.Weapon.defName == "TMM_Gun_BoraxSprayer" || dinfo.Weapon.defName == "TMM_Gun_BoraxTurret"));
            if (isBoraxDamage && !__instance.Dead && LegendaryUtil.IsLeviathan(__instance))
            {
                LegendaryUtil.ApplyBoraxDebuffs(__instance);
            }

            // 2. Protector Power Armor auto-injector
            if (!__instance.Dead && __instance.apparel != null)
            {
                List<Apparel> worn = __instance.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    Apparel ap = worn[i];
                    if (ap != null && ap.def != null && ap.def.defName == "TMM_Legendary_ProtectorArmor")
                    {
                        CompProtectorAutoInjector inj = ap.GetComp<CompProtectorAutoInjector>();
                        if (inj != null) inj.TryTrigger(__instance);
                        break;
                    }
                }
            }

            Pawn attacker = dinfo.Instigator as Pawn;
            string weaponDef = dinfo.Weapon != null ? dinfo.Weapon.defName : null;
            if (weaponDef.NullOrEmpty() && attacker != null && attacker.equipment != null && attacker.equipment.Primary != null)
            {
                if (!dinfo.Def.isRanged)
                {
                    weaponDef = attacker.equipment.Primary.def.defName;
                }
            }

            if (weaponDef.NullOrEmpty()) return;

            // 3. Rule 2: First Blade & Archangel Blade one-hit kill (except Lucifer, Michael, Dick Roman)
            if (weaponDef == "TMM_Legendary_FirstBlade" || weaponDef == "TMM_Legendary_ArchangelBlade")
            {
                if (weaponDef == "TMM_Legendary_ArchangelBlade" && __instance.Spawned && __instance.Map != null)
                {
                    FireUtility.TryAttachFire(__instance, 0.35f, attacker); // CHECK API
                }

                if (!__instance.Dead && !LegendaryUtil.IsImmuneBoss(__instance))
                {
                    __instance.Kill(dinfo);
                }
                return;
            }

            // 4. Atom's Judgement radiation pulse (2.5 cells)
            if (weaponDef == "TMM_Legendary_AtomsJudgement" && __instance.Spawned && __instance.Map != null)
            {
                Map map = __instance.Map;
                IntVec3 pos = __instance.Position;
                FleckMaker.ThrowDustPuffThick(pos.ToVector3Shifted(), map, 2.0f, new Color(0.25f, 0.95f, 0.25f)); // CHECK API
                List<Pawn> nearby = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
                for (int i = 0; i < nearby.Count; i++)
                {
                    Pawn p = nearby[i];
                    if (p == null || p.Dead || p.Destroyed || p.Position.DistanceTo(pos) > 2.5f) continue;
                    if (LegendaryUtil.IsGhoulOrSuperMutant(p))
                    {
                        LegendaryUtil.HealOneOrMoreInjuries(p, 10f);
                    }
                    else if (p != attacker && p.RaceProps != null && p.RaceProps.IsFlesh)
                    {
                        float toxRes = Mathf.Clamp01(p.GetStatValue(StatDefOf.ToxicResistance)); // CHECK API
                        float delta = 0.18f * (1f - toxRes);
                        if (delta > 0.001f) HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, delta);
                    }
                }
                return;
            }

            // 5. Alpha Predator lifesteal (8-12 healing + hemogen)
            if (weaponDef == "TMM_Legendary_AlphaPredator" && attacker != null && !attacker.Dead &&
                __instance.RaceProps != null && __instance.RaceProps.IsFlesh && totalDamageDealt > 0.5f)
            {
                LegendaryUtil.HealOneOrMoreInjuries(attacker, Rand.Range(8f, 12f));
                if (ModsConfig.BiotechActive && attacker.genes != null)
                {
                    Gene_Hemogen hemo = attacker.genes.GetFirstGeneOfType<Gene_Hemogen>(); // CHECK API
                    if (hemo != null)
                    {
                        hemo.Value = Mathf.Min(hemo.Max, hemo.Value + 0.15f); // CHECK API
                    }
                }
            }
        }

        public static void Postfix_Pawn_Kill(Pawn __instance, DamageInfo? dinfo)
        {
            if (!dinfo.HasValue) return;
            Pawn killer = dinfo.Value.Instigator as Pawn;
            if (killer == null || killer.Dead || killer.health == null || killer.health.hediffSet == null) return;

            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_MarkOfCain");
            if (markDef == null) return;
            HediffWithComps mark = killer.health.hediffSet.GetFirstHediffOfDef(markDef) as HediffWithComps;
            if (mark != null)
            {
                HediffComp_MarkOfCain comp = mark.TryGetComp<HediffComp_MarkOfCain>();
                if (comp != null) comp.NotifyKill();
            }
        }

        public static void Postfix_Pawn_SpawnSetup(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (__instance == null) return;
            if (LegendaryUtil.IsDickRoman(__instance))
            {
                LegendaryUtil.EnsureDickRomanInvulnerability(__instance);
            }
            if (LegendaryUtil.IsWieldingDef(__instance, "TMM_Legendary_FirstBlade"))
            {
                LegendaryUtil.EnsureMarkOfCain(__instance);
            }
        }

        public static bool Prefix_TryStartMentalState(MentalStateHandler __instance, MentalStateDef stateDef, ref bool __result)
        {
            if (stateDef == null) return true;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null) return true;

            // Golden Pip-Boy blocks panic flee & terror states
            if (LegendaryUtil.IsWearingDef(pawn, "TMM_Legendary_GoldenPipBoy"))
            {
                if (stateDef == MentalStateDefOf.PanicFlee ||
                    stateDef.defName.IndexOf("Panic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    stateDef.defName.IndexOf("Terror", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }

        public static bool Prefix_AddHediff(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (hediff == null || hediff.def == null) return true;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null) return true;

            // Archangel Blade grants immunity to demonic curses / Infernal Whisper
            if (LegendaryUtil.IsWieldingDef(pawn, "TMM_Legendary_ArchangelBlade"))
            {
                if (hediff.def.defName == "TMM_Hediff_InfernalWhisper")
                {
                    return false;
                }
            }
            return true;
        }
    }
}
