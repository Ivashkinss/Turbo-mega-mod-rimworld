using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// In-game verification probe for the 18 supernatural genes across all 5 xenotypes:
    /// checks active genes, metabolism sum == 0, passive stat/radiation effects, real Ability.Activate
    /// execution for all 8 abilities, and Weakness DamageDef multipliers vs baseline human.
    /// Activated via -tmm-supergenes-probe.
    /// </summary>
    public sealed class InGameSupernaturalGenesProbe : MapComponent
    {
        private sealed class CheckItem
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private static bool s_completed;
        private int m_ticksWaited;
        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        public InGameSupernaturalGenesProbe(Map map) : base(map)
        {
        }

        private static bool ProbeEnabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "-tmm-supergenes-probe", StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
        }

        public override void MapComponentTick()
        {
            if (s_completed || !ProbeEnabled) return;
            if (map != Find.AnyPlayerHomeMap) return;

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                RunAllChecks();
            }
            catch (Exception ex)
            {
                m_errors.Add("Unhandled exception: " + ex.ToString());
            }
            finally
            {
                Application.logMessageReceived -= OnUnityLog;
                WriteReport("SupernaturalGenesProbe.json");
                Root.Shutdown();
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        private void RunAllChecks()
        {
            IntVec3 origin = FindClearAreaCenter();

            Pawn human = SpawnXenotypePawn(null, Faction.OfPlayer, origin);
            Pawn angel = SpawnXenotypePawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(2, 0, 0));
            Pawn demon = SpawnXenotypePawn("TMM_Demon", Faction.OfPlayer, origin + new IntVec3(-2, 0, 0));
            Pawn mutant = SpawnXenotypePawn("TMM_SuperMutant", Faction.OfPlayer, origin + new IntVec3(0, 0, 2));
            Pawn ghoul = SpawnXenotypePawn("TMM_Ghoul", Faction.OfPlayer, origin + new IntVec3(0, 0, -2));
            Pawn leviathan = SpawnXenotypePawn("TMM_Leviathan", Faction.OfPlayer, origin + new IntVec3(2, 0, 2));

            // ---------------- 1. XENOTYPE GENES & METABOLISM == 0 ----------------
            VerifyRaceGenesAndMetabolism("Angel", angel, new string[]
            {
                "TMM_Gene_AngelCelestialGrace",
                "TMM_Gene_AngelHealingTouch",
                "TMM_Gene_AngelTrueVisage",
                "TMM_Gene_AngelEnochianVulnerability"
            });
            VerifyRaceGenesAndMetabolism("Demon", demon, new string[]
            {
                "TMM_Gene_DemonBlackSmoke",
                "TMM_Gene_DemonMindCloud",
                "TMM_Gene_DemonCrossroadsGuile",
                "TMM_Gene_DemonHolyBane"
            });
            VerifyRaceGenesAndMetabolism("SuperMutant", mutant, new string[]
            {
                "TMM_Gene_MutantFevMusculature",
                "TMM_Gene_MutantRadRegeneration",
                "TMM_Gene_MutantWarRoar",
                "TMM_Gene_MutantBulkyConstraints"
            });
            VerifyRaceGenesAndMetabolism("Ghoul", ghoul, new string[]
            {
                "TMM_Gene_GhoulRadHealing",
                "TMM_Gene_GhoulGlowingPulse",
                "TMM_Gene_GhoulFeralRisk"
            });
            VerifyRaceGenesAndMetabolism("Leviathan", leviathan, new string[]
            {
                "TMM_Gene_LeviathanDevourMaw",
                "TMM_Gene_LeviathanShapeshiftMimic",
                "TMM_Gene_LeviathanBoraxAllergy"
            });

            // ---------------- 2. PASSIVE STATS & RADIATION ----------------
            if (human != null && angel != null)
            {
                float humanSpeed = human.GetStatValue(StatDefOf.MoveSpeed);
                float angelSpeed = angel.GetStatValue(StatDefOf.MoveSpeed);
                AddCheck("Passive_AngelMoveSpeed15Pct", angelSpeed > humanSpeed * 1.10f,
                    string.Format("> {0:0.00}", humanSpeed * 1.10f), string.Format("{0:0.00}", angelSpeed));

                float angelPsy = angel.GetStatValue(StatDefOf.PsychicSensitivity);
                AddCheck("Passive_AngelPsychicSensitivity", angelPsy >= 1.30f, ">= 1.30", string.Format("{0:0.00}", angelPsy));

                bool hasGraceHediff = HasHediff(angel, "TMM_Hediff_CelestialGrace");
                AddCheck("Passive_AngelBleedHediff", hasGraceHediff, "present", hasGraceHediff.ToString());
            }

            if (demon != null)
            {
                float trade = demon.GetStatValue(StatDefOf.TradePriceImprovement);
                AddCheck("Passive_DemonTradePriceBonus", trade >= 0.20f, ">= 0.20", string.Format("{0:0.00}", trade));
            }

            if (mutant != null)
            {
                float carry = mutant.GetStatValue(StatDefOf.CarryingCapacity);
                float meleeDmg = mutant.GetStatValue(StatDefOf.MeleeDamageFactor);
                float aimDelay = mutant.GetStatValue(StatDefOf.AimingDelayFactor);
                float fertility = mutant.GetStatValue(StatDefOf.Fertility); // CHECK API

                AddCheck("Passive_MutantCarryCapacity", carry >= 200f, ">= 200", string.Format("{0:0.0}", carry));
                AddCheck("Passive_MutantMeleeDamage", meleeDmg >= 1.25f, ">= 1.25", string.Format("{0:0.00}", meleeDmg));
                AddCheck("Passive_MutantAimDelay", aimDelay <= 0.85f, "<= 0.85", string.Format("{0:0.00}", aimDelay));
                AddCheck("Passive_MutantSterile", fertility <= 0.01f, "0", string.Format("{0:0.00}", fertility));
                AddCheck("Passive_MutantBulkyHungerHediff", HasHediff(mutant, "TMM_Hediff_BulkyPhysiology"), "present",
                    HasHediff(mutant, "TMM_Hediff_BulkyPhysiology").ToString());

                // Pollution triggers Irradiated Metabolism
                map.pollutionGrid.SetPolluted(mutant.Position, true); // CHECK API
                Gene_MutantRadRegeneration radGene = mutant.genes.GetFirstGeneOfType<Gene_MutantRadRegeneration>();
                if (radGene != null) radGene.RefreshRadiationBuff();
                AddCheck("Passive_MutantRadRegenOnPollution", HasHediff(mutant, "TMM_Hediff_IrradiatedMetabolism"), "present",
                    HasHediff(mutant, "TMM_Hediff_IrradiatedMetabolism").ToString());
                map.pollutionGrid.SetPolluted(mutant.Position, false); // CHECK API
            }

            if (ghoul != null)
            {
                AddTestCut(ghoul, 12f);
                float dmgBefore = SumInjurySeverity(ghoul);
                map.pollutionGrid.SetPolluted(ghoul.Position, true); // CHECK API
                Gene_GhoulRadHealing radHealGene = ghoul.genes.GetFirstGeneOfType<Gene_GhoulRadHealing>();
                float healed = radHealGene != null ? radHealGene.ApplyRadiationHeal() : 0f;
                float dmgAfter = SumInjurySeverity(ghoul);
                AddCheck("Passive_GhoulRadHealing", healed > 0f && dmgAfter < dmgBefore, "healed > 0",
                    string.Format("before={0:0.0}, after={1:0.0}, healed={2:0.0}", dmgBefore, dmgAfter, healed));
                map.pollutionGrid.SetPolluted(ghoul.Position, false); // CHECK API
            }

            // ---------------- 3. ABILITIES (REAL Ability.Activate) ----------------

            // 3.1 Angel Healing Touch
            if (angel != null)
            {
                Pawn patient = SpawnXenotypePawn(null, Faction.OfPlayer, angel.Position + new IntVec3(1, 0, 0));
                AddTestCut(patient, 4f);
                AddTestCut(patient, 4f);
                AddTestCut(patient, 4f);
                Hediff inf = patient.health.AddHediff(HediffDefOf.WoundInfection);
                inf.Severity = 0.60f;

                float injBefore = SumInjurySeverity(patient);
                Ability ab = FindAbility(angel, "TMM_Ability_HealingTouch");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(patient), LocalTargetInfo.Invalid);
                float injAfter = SumInjurySeverity(patient);
                AddCheck("Ability_HealingTouch", activated && injAfter < injBefore && inf.Severity <= 0.35f,
                    "wounds healed & infection halved",
                    string.Format("act={0}, inj={1:0.0}->{2:0.0}, inf={3:0.00}", activated, injBefore, injAfter, inf.Severity));
            }

            // 3.2 Angel True Visage
            if (angel != null)
            {
                Pawn onlooker = SpawnXenotypePawn(null, Faction.OfPlayer, angel.Position + new IntVec3(0, 0, 3));
                Ability ab = FindAbility(angel, "TMM_Ability_TrueVisage");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(angel), new LocalTargetInfo(angel));
                bool blinded = HasHediff(onlooker, "TMM_Hediff_RetinalBurn");
                AddCheck("Ability_TrueVisage", activated && blinded, "RetinalBurn applied",
                    string.Format("act={0}, hasBurn={1}", activated, blinded));
            }

            // 3.3 Demon Black Smoke Shift
            if (demon != null)
            {
                IntVec3 startPos = demon.Position;
                IntVec3 destPos = FindStandableCellOffset(startPos, 6, 9);
                Ability ab = FindAbility(demon, "TMM_Ability_BlackSmokeShift");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(destPos), new LocalTargetInfo(destPos));
                bool moved = demon.Position == destPos && demon.Position != startPos;
                AddCheck("Ability_BlackSmokeShift", activated && moved, destPos.ToString(), demon.Position.ToString());
            }

            // 3.4 Demon Infernal Whisper
            if (demon != null)
            {
                Pawn target = SpawnXenotypePawn(null, Faction.OfPlayer, demon.Position + new IntVec3(3, 0, 0));
                Ability ab = FindAbility(demon, "TMM_Ability_InfernalWhisper");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(target), LocalTargetInfo.Invalid);
                bool hasDebuff = HasHediff(target, "TMM_Hediff_InfernalWhisper");
                AddCheck("Ability_InfernalWhisper", activated && hasDebuff, "InfernalWhisper applied",
                    string.Format("act={0}, hasHediff={1}", activated, hasDebuff));
            }

            // 3.5 Super Mutant War Roar
            if (mutant != null)
            {
                Faction hostileFac = FindAnyHostileFaction();
                Pawn enemy = SpawnXenotypePawn(null, hostileFac, mutant.Position + new IntVec3(2, 0, 0));
                Ability ab = FindAbility(mutant, "TMM_Ability_MutantWarRoar");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(mutant), new LocalTargetInfo(mutant));
                bool selfBuff = HasHediff(mutant, "TMM_Hediff_WarRoarAdrenaline");
                bool enemyFear = enemy != null && HasHediff(enemy, "TMM_Hediff_WarRoarFear");
                AddCheck("Ability_MutantWarRoar", activated && selfBuff && enemyFear, "selfBuff=true, enemyFear=true",
                    string.Format("act={0}, buff={1}, fear={2}", activated, selfBuff, enemyFear));
            }

            // 3.6 Ghoul Glowing Pulse
            if (ghoul != null)
            {
                Pawn allyGhoul = SpawnXenotypePawn("TMM_Ghoul", Faction.OfPlayer, ghoul.Position + new IntVec3(2, 0, 0));
                Pawn bystander = SpawnXenotypePawn(null, Faction.OfPlayer, ghoul.Position + new IntVec3(-2, 0, 0));
                AddTestCut(allyGhoul, 12f);
                float allyInjBefore = SumInjurySeverity(allyGhoul);

                Ability ab = FindAbility(ghoul, "TMM_Ability_GlowingPulse");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(ghoul), new LocalTargetInfo(ghoul));
                float allyInjAfter = SumInjurySeverity(allyGhoul);
                Hediff tox = bystander.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
                bool gotTox = tox != null && tox.Severity > 0.05f;
                AddCheck("Ability_GlowingPulse", activated && allyInjAfter < allyInjBefore && gotTox,
                    "ally healed & human got ToxicBuildup",
                    string.Format("act={0}, allyInj={1:0.0}->{2:0.0}, tox={3:0.00}", activated, allyInjBefore, allyInjAfter, tox != null ? tox.Severity : 0f));
            }

            // 3.7 Leviathan Predatory Mimicry (apply -> verify copy -> restore)
            if (leviathan != null)
            {
                Pawn donor = SpawnXenotypePawn(null, Faction.OfPlayer, leviathan.Position + new IntVec3(1, 0, 0));
                HairDef origHair = leviathan.story.hairDef;
                HairDef donorHair = PickDifferentHair(origHair);
                donor.story.hairDef = donorHair;
                donor.story.skinColorOverride = new Color(0.85f, 0.72f, 0.62f);

                Ability ab = FindAbility(leviathan, "TMM_Ability_PredatoryMimicry");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(donor), LocalTargetInfo.Invalid);
                bool hasMimic = HasHediff(leviathan, "TMM_Hediff_PredatoryMimicry");
                bool copiedHair = leviathan.story.hairDef == donorHair;

                SupernaturalGeneUtil.StripMimicry(leviathan);
                bool restoredHair = leviathan.story.hairDef == origHair;

                AddCheck("Ability_PredatoryMimicry", activated && hasMimic && copiedHair && restoredHair,
                    "mimic applied, hair copied & restored",
                    string.Format("act={0}, hediff={1}, copied={2}, restored={3}", activated, hasMimic, copiedHair, restoredHair));
            }

            // 3.8 Leviathan Devour Maw
            if (leviathan != null)
            {
                Pawn prey = SpawnXenotypePawn(null, Faction.OfPlayer, leviathan.Position + new IntVec3(1, 0, 0));
                if (leviathan.needs != null && leviathan.needs.food != null)
                {
                    leviathan.needs.food.CurLevelPercentage = 0.25f;
                }
                AddTestCut(leviathan, 12f);
                float foodBefore = leviathan.needs.food.CurLevelPercentage;
                float selfInjBefore = SumInjurySeverity(leviathan);

                Ability ab = FindAbility(leviathan, "TMM_Ability_DevourMaw");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(prey), LocalTargetInfo.Invalid);
                float foodAfter = leviathan.needs.food.CurLevelPercentage;
                float selfInjAfter = SumInjurySeverity(leviathan);
                bool preyDamaged = prey.Dead || SumInjurySeverity(prey) > 0f;

                AddCheck("Ability_DevourMaw", activated && preyDamaged && foodAfter > foodBefore + 0.30f && selfInjAfter < selfInjBefore,
                    "prey hurt, food +40%, self healed",
                    string.Format("act={0}, preyHurt={1}, food={2:0.00}->{3:0.00}, selfInj={4:0.0}->{5:0.0}",
                        activated, preyDamaged, foodBefore, foodAfter, selfInjBefore, selfInjAfter));
            }

            // ---------------- 4. WEAKNESSES (DAMAGE COMPARISON VS BASELINE HUMAN) ----------------
            CompareWeaknessDamage("Weakness_AngelEnochian", "TMM_Enochian", angel, 2.0f);
            CompareWeaknessDamage("Weakness_DemonHolyWater", "TMM_HolyWater", demon, 2.5f);
            CompareWeaknessDamage("Weakness_DemonRockSalt", "TMM_RockSalt", demon, 2.5f);

            // Leviathan Borax Burn: damage multiplier + TMM_Hediff_BoraxBurn applied
            CompareWeaknessDamage("Weakness_LeviathanBoraxDamage", "TMM_BoraxBurn", leviathan, 3.0f);
            bool boraxHediffApplied = leviathan != null && HasHediff(leviathan, "TMM_Hediff_BoraxBurn");
            AddCheck("Weakness_LeviathanBoraxDebuff", boraxHediffApplied, "TMM_Hediff_BoraxBurn applied",
                boraxHediffApplied.ToString());
        }

        private void VerifyRaceGenesAndMetabolism(string label, Pawn pawn, string[] requiredGenes)
        {
            if (pawn == null || pawn.genes == null)
            {
                AddCheck("Race_" + label + "_Generated", false, "valid pawn", "null");
                return;
            }

            bool allActive = true;
            List<string> missing = new List<string>();
            for (int i = 0; i < requiredGenes.Length; i++)
            {
                if (!SupernaturalGeneUtil.HasActiveGene(pawn, requiredGenes[i]))
                {
                    allActive = false;
                    missing.Add(requiredGenes[i]);
                }
            }

            AddCheck("Race_" + label + "_GenesActive", allActive, "all active",
                allActive ? "all active" : "missing: " + string.Join(",", missing.ToArray()));

            int metSum = 0;
            List<Gene> genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i] != null && genes[i].Active && genes[i].def != null)
                {
                    metSum += genes[i].def.biostatMet;
                }
            }

            AddCheck("Race_" + label + "_MetabolismZero", metSum == 0, "0", metSum.ToString());
        }

        private void CompareWeaknessDamage(string checkName, string damageDefName, Pawn targetPawn, float minRatio)
        {
            DamageDef dmgDef = DefDatabase<DamageDef>.GetNamedSilentFail(damageDefName);
            if (dmgDef == null || targetPawn == null)
            {
                AddCheck(checkName, false, "valid DamageDef & pawn", "missing");
                return;
            }

            Pawn baseline = SpawnXenotypePawn(null, Faction.OfPlayer, map.Center);
            float humanDmg = ApplyControlledTorsoDamage(baseline, dmgDef, 6f);
            float targetDmg = ApplyControlledTorsoDamage(targetPawn, dmgDef, 6f);
            float ratio = humanDmg > 0.01f ? (targetDmg / humanDmg) : 0f;

            AddCheck(checkName, ratio >= minRatio,
                string.Format("ratio >= {0:0.0}x", minRatio),
                string.Format("target={0:0.0}, human={1:0.0}, ratio={2:0.00}x", targetDmg, humanDmg, ratio));
        }

        private static float ApplyControlledTorsoDamage(Pawn pawn, DamageDef dmgDef, float amount)
        {
            if (pawn == null || pawn.Dead) return 0f;
            BodyPartRecord torso = null;
            List<BodyPartRecord> parts = pawn.RaceProps.body.AllParts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].def == BodyPartDefOf.Torso)
                {
                    torso = parts[i];
                    break;
                }
            }
            DamageInfo dinfo = new DamageInfo(dmgDef, amount, 2.0f, -1f, null, torso);
            DamageWorker.DamageResult res = pawn.TakeDamage(dinfo); // CHECK API
            return res != null ? res.totalDamageDealt : 0f;
        }

        private Pawn SpawnXenotypePawn(string xenotypeDefName, Faction fac, IntVec3 nearCell)
        {
            XenotypeDef xeno = !xenotypeDefName.NullOrEmpty()
                ? DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotypeDefName)
                : XenotypeDefOf.Baseliner;

            PawnGenerationRequest req = new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                fac ?? Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                -1,
                true, false, false, true, false, 1f, false, true, false, false, false,
                false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null,
                25f, 25f, null, null, null, null, null,
                false, false, false, false, null, null, xeno, null, null, 0f, DevelopmentalStage.Adult); // CHECK API

            Pawn p = PawnGenerator.GeneratePawn(req);
            if (p == null) return null;

            // Remove apparel so damage & stat tests measure pure xenotype physiology
            if (p.apparel != null) p.apparel.DestroyAll();

            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(nearCell, map, 4, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map);
            }, out cell))
            {
                cell = map.Center;
            }
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            return p;
        }

        private static Ability FindAbility(Pawn pawn, string abilityDefName)
        {
            if (pawn == null || pawn.abilities == null) return null;
            AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
            if (def == null) return null;
            return pawn.abilities.GetAbility(def);
        }

        private static bool ActivateAbility(Ability ability, LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (ability == null) return false;
            return ability.Activate(target, dest); // CHECK API
        }

        private static void AddTestCut(Pawn pawn, float severity)
        {
            if (pawn == null || pawn.Dead) return;
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, severity, 2.0f);
            pawn.TakeDamage(dinfo);
        }

        private static float SumInjurySeverity(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return 0f;
            float sum = 0f;
            List<Hediff> list = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < list.Count; i++)
            {
                Hediff_Injury inj = list[i] as Hediff_Injury;
                if (inj != null && !inj.IsPermanent()) sum += inj.Severity;
            }
            return sum;
        }

        private static bool HasHediff(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            return def != null && pawn.health.hediffSet.GetFirstHediffOfDef(def) != null;
        }

        private static HairDef PickDifferentHair(HairDef current)
        {
            List<HairDef> all = DefDatabase<HairDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i] != current) return all[i];
            }
            return current;
        }

        private static Faction FindAnyHostileFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].IsPlayer && !all[i].defeated && all[i].HostileTo(Faction.OfPlayer))
                {
                    return all[i];
                }
            }
            return Faction.OfPirates;
        }

        private IntVec3 FindClearAreaCenter()
        {
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(map.Center, map, 12, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out found))
            {
                return found;
            }
            return map.Center;
        }

        private IntVec3 FindStandableCellOffset(IntVec3 origin, int minDist, int maxDist)
        {
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(origin, map, maxDist, delegate(IntVec3 c)
            {
                float d = c.DistanceTo(origin);
                return d >= minDist && d <= maxDist && c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out found))
            {
                return found;
            }
            return origin;
        }

        private void AddCheck(string name, bool passed, string expected, string actual)
        {
            CheckItem item = new CheckItem();
            item.name = name;
            item.passed = passed;
            item.expected = expected ?? string.Empty;
            item.actual = actual ?? string.Empty;
            m_checks.Add(item);
        }

        private void WriteReport(string fileName)
        {
            bool allPassed = m_errors.Count == 0 && m_checks.Count > 0;
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (!m_checks[i].passed)
                {
                    allPassed = false;
                    break;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"passed\":");
            sb.Append(allPassed ? "true" : "false");
            sb.Append(",\"checksCount\":");
            sb.Append(m_checks.Count);
            sb.Append(",\"checks\":[");
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (i > 0) sb.Append(',');
                CheckItem c = m_checks[i];
                sb.Append("{\"name\":\"");
                sb.Append(EscapeJson(c.name));
                sb.Append("\",\"passed\":");
                sb.Append(c.passed ? "true" : "false");
                sb.Append(",\"expected\":\"");
                sb.Append(EscapeJson(c.expected));
                sb.Append("\",\"actual\":\"");
                sb.Append(EscapeJson(c.actual));
                sb.Append("\"}");
            }
            sb.Append("],\"errors\":[");
            for (int i = 0; i < m_errors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                sb.Append(EscapeJson(m_errors[i]));
                sb.Append('"');
            }
            sb.Append("]}");

            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }
    }
}
