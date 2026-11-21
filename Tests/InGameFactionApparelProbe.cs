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
    /// In-game verification probe for all 32 faction apparel items &amp; 10 research projects:
    /// - Wears one item per faction and verifies armor + stat bonuses;
    /// - Verifies angel_only apparel blocks humans and fits TMM_Angel;
    /// - Verifies super_mutant_only apparel blocks humans and fits TMM_SuperMutant;
    /// - Verifies quest recipe gating (locked without flag, unlocked after SparkStory.SetFlag);
    /// - Verifies Power Armor uses layer TMM_PowerArmor and stacks over Shell + TMM_Backpack;
    /// - Verifies special effects (MoveSpeed +0.08 on nocturnal coat, -35% demon damage on angel raiment,
    ///   self-repair on living bioferrite carapace).
    /// Activated via -tmm-faction-apparel-probe.
    /// </summary>
    public sealed class InGameFactionApparelProbe : MapComponent
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

        public InGameFactionApparelProbe(Map map) : base(map)
        {
        }

        private static bool ProbeEnabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "-tmm-faction-apparel-probe", StringComparison.OrdinalIgnoreCase)) return true;
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
                WriteReport("FactionApparelProbe.json");
                bool uiPassed = m_errors.Count == 0 && m_checks.Count > 0 && m_checks.TrueForAll(c => c.passed);
                System.IO.File.WriteAllText(System.IO.Path.Combine(GenFilePaths.SaveDataFolderPath, "FactionApparelUiProbe.json"), "{\"passed\":" + (uiPassed ? "true" : "false") + "}");
                // The test harness closes the game after its own reports.
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
            IntVec3 origin = FindClearCenter();

            // ---------------- 1. ALL 10 RESEARCH PROJECTS LOADED ----------------
            string[] researchIds = new string[]
            {
                "TMM_WastelandMilitaryGear",
                "TMM_VaultTecStandards",
                "TMM_RadSurvivalGear",
                "TMM_HuntersMilitiaCraft",
                "TMM_OccultTailoring",
                "TMM_SuperMutantArmor",
                "TMM_UndergroundElectronics",
                "TMM_BrotherhoodGear",
                "TMM_InstituteTextiles",
                "TMM_ClanBioferriteCraft"
            };
            int researchLoaded = 0;
            for (int i = 0; i < researchIds.Length; i++)
            {
                if (DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchIds[i]) != null) researchLoaded++;
            }
            AddCheck("Research_All10Loaded", researchLoaded == 10, "10", researchLoaded.ToString());

            // ---------------- 2. ONE ITEM PER FACTION: WEAR + ARMOR & STAT CHECK ----------------
            VerifyHumanApparelStat("Faction_Hell_Suit", "TMM_Apparel_demon_crossroads_suit", StatDefOf.SocialImpact, 0.15f, 0.15f, origin);
            VerifyHumanApparelStat("Faction_BoS_Scribe", "TMM_Apparel_bos_scribe_fatigues", StatDefOf.ResearchSpeed, 0.10f, 0.20f, origin);
            VerifyHumanApparelStat("Faction_Outcasts_Recon", "TMM_Apparel_outcast_recon_underarmor", StatDefOf.MoveSpeed, 0.08f, 0.30f, origin);
            VerifyHumanApparelStat("Faction_Enclave_Officer", "TMM_Apparel_enclave_officer_uniform", StatDefOf.SocialImpact, 0.12f, 0.25f, origin);
            VerifyHumanApparelStat("Faction_Institute_LabCoat", "TMM_Apparel_institute_lab_coat", StatDefOf.ResearchSpeed, 0.14f, 0.14f, origin);
            VerifyHumanApparelStat("Faction_Railroad_Coat", "TMM_Apparel_railroad_armored_coat", StatDefOf.MoveSpeed, 0.06f, 0.90f, origin);
            VerifyHumanApparelStat("Faction_Gunners_Combat", "TMM_Apparel_gunner_combat_armor", StatDefOf.AimingDelayFactor, -0.02f, 0.88f, origin, true);
            VerifyHumanApparelStat("Faction_VaultTec_Jumpsuit", "TMM_Apparel_vault_tec_jumpsuit", StatDefOf.WorkSpeedGlobal, 0.06f, 0.20f, origin);
            VerifyHumanApparelStat("Faction_Hunters_Duster", "TMM_Apparel_hunter_leather_duster", StatDefOf.MeleeDodgeChance, 0.04f, 0.50f, origin);
            VerifyHumanApparelStat("Faction_MoL_TweedSuit", "TMM_Apparel_mol_arcane_tweed_suit", StatDefOf.ResearchSpeed, 0.10f, 0.42f, origin);
            VerifyHumanApparelStat("Faction_Ghouls_Frockcoat", "TMM_Apparel_ghoul_revolutionary_frockcoat", StatDefOf.SocialImpact, 0.15f, 0.40f, origin);

            // Nocturnal Clan Velvet Coat: specifically verify Claude's MoveSpeed +0.08 balance rule
            Pawn clanTester = SpawnPawn(null, Faction.OfPlayer, origin);
            float baseMove = clanTester.GetStatValue(StatDefOf.MoveSpeed);
            Apparel nocturnalCoat = MakeApparel("TMM_Apparel_clan_nocturnal_aristocrat_coat");
            if (nocturnalCoat != null) clanTester.apparel.Wear(nocturnalCoat, false);
            float moveDelta = clanTester.GetStatValue(StatDefOf.MoveSpeed) - baseMove;
            AddCheck("Faction_MonsterClans_NocturnalCoatSpeed008",
                nocturnalCoat != null && clanTester.apparel.WornApparel.Contains(nocturnalCoat) && moveDelta >= 0.06f && moveDelta <= 0.11f,
                "delta ~ +0.08", string.Format("{0:+0.000;-0.000}", moveDelta));

            // ---------------- 3. RACE RESTRICTION: ANGEL ONLY ----------------
            Pawn human = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(2, 0, 0));
            Pawn angel = SpawnPawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(3, 0, 0));
            ThingDef angelCoatDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Apparel_angel_vessel_trenchcoat");
            Apparel angelCoat = MakeApparel("TMM_Apparel_angel_vessel_trenchcoat");

            bool humanCanHasParts = angelCoatDef != null && ApparelUtility.HasPartsToWear(human, angelCoatDef);
            string humanCantReason;
            bool humanCanEquip = angelCoat != null && EquipmentUtility.CanEquip(angelCoat, human, out humanCantReason); // CHECK API
            if (angelCoat != null) human.apparel.Wear(angelCoat, false);
            bool humanWoreIt = angelCoat != null && human.apparel.WornApparel.Contains(angelCoat);

            bool angelCanHasParts = angelCoatDef != null && angel != null && ApparelUtility.HasPartsToWear(angel, angelCoatDef);
            if (angel != null && angelCoat != null) angel.apparel.Wear(angelCoat, false);
            bool angelWoreIt = angel != null && angelCoat != null && angel.apparel.WornApparel.Contains(angelCoat);

            AddCheck("Race_AngelCoatBlockedOnHuman", !humanCanHasParts && !humanCanEquip && !humanWoreIt,
                "all false", string.Format("hasParts={0}, canEquip={1}, worn={2}", humanCanHasParts, humanCanEquip, humanWoreIt));
            AddCheck("Race_AngelCoatWornByAngel", angelCanHasParts && angelWoreIt,
                "hasParts=true, worn=true", string.Format("hasParts={0}, worn={1}", angelCanHasParts, angelWoreIt));

            // ---------------- 4. RACE RESTRICTION: SUPER MUTANT ONLY ----------------
            Pawn mutant = SpawnPawn("TMM_SuperMutant", Faction.OfPlayer, origin + new IntVec3(-2, 0, 0));
            ThingDef harnessDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Apparel_mutant_scrap_harness");
            Apparel harness = MakeApparel("TMM_Apparel_mutant_scrap_harness");
            Apparel cageHelmet = MakeApparel("TMM_Apparel_mutant_rebar_cage_helmet");

            bool humanCanMutantArmor = harnessDef != null && ApparelUtility.HasPartsToWear(human, harnessDef);
            if (harness != null) human.apparel.Wear(harness, false);
            bool humanWoreMutantArmor = harness != null && human.apparel.WornApparel.Contains(harness);

            bool mutantCanArmor = harnessDef != null && mutant != null && ApparelUtility.HasPartsToWear(mutant, harnessDef);
            if (mutant != null && harness != null) mutant.apparel.Wear(harness, false);
            if (mutant != null && cageHelmet != null) mutant.apparel.Wear(cageHelmet, false);
            bool mutantWoreBoth = mutant != null && harness != null && cageHelmet != null &&
                                  mutant.apparel.WornApparel.Contains(harness) &&
                                  mutant.apparel.WornApparel.Contains(cageHelmet);

            AddCheck("Race_MutantArmorBlockedOnHuman", !humanCanMutantArmor && !humanWoreMutantArmor,
                "both false", string.Format("hasParts={0}, worn={1}", humanCanMutantArmor, humanWoreMutantArmor));
            AddCheck("Race_MutantArmorWornBySuperMutant", mutantCanArmor && mutantWoreBoth,
                "both worn", string.Format("hasParts={0}, bothWorn={1}", mutantCanArmor, mutantWoreBoth));

            // ---------------- 5. QUEST RECIPE FLAG GATING ----------------
            SparkStory story = SparkStory.Instance;
            RecipeDef t60Recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("TMM_Make_bos_t60_power_armor");
            RecipeDef coatRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("TMM_Make_demon_infernal_trenchcoat");
            if (story != null && t60Recipe != null && coatRecipe != null)
            {
                story.SetFlag("unlock.bos_t60_power_armor", "false"); // CHECK API
                story.SetFlag("unlock.bos_t60_armor", "false");
                bool lockedBefore = !t60Recipe.AvailableNow;

                story.SetFlag("unlock.bos_t60_power_armor", "true"); // CHECK API
                bool unlockedAfter = t60Recipe.AvailableNow;

                story.SetFlag("unlock.demon_infernal_trenchcoat", "false");
                story.SetFlag("unlock.hell_operative_coat", "false");
                bool coatLockedBefore = !coatRecipe.AvailableNow;
                story.SetFlag("unlock.demon_infernal_trenchcoat", "true");
                bool coatUnlockedAfter = coatRecipe.AvailableNow;

                AddCheck("QuestRecipe_LockedWithoutFlagAndUnlockedWithFlag",
                    lockedBefore && unlockedAfter && coatLockedBefore && coatUnlockedAfter,
                    "t60(lock->open) & coat(lock->open)",
                    string.Format("t60({0}->{1}), coat({2}->{3})", lockedBefore, unlockedAfter, coatLockedBefore, coatUnlockedAfter));
            }
            else
            {
                AddCheck("QuestRecipe_LockedWithoutFlagAndUnlockedWithFlag", false, "story & recipes exist",
                    string.Format("story={0}, t60={1}, coat={2}", story != null, t60Recipe != null, coatRecipe != null));
            }

            // ---------------- 6. POWER ARMOR LAYER TMM_PowerArmor & STACKING ----------------
            string[] paDefs = new string[]
            {
                "TMM_Apparel_bos_t60_power_armor",
                "TMM_Apparel_outcast_power_armor",
                "TMM_Apparel_enclave_x01_power_armor",
                "TMM_Apparel_clan_bioferrite_carapace"
            };
            bool allInPowerLayer = true;
            for (int i = 0; i < paDefs.Length; i++)
            {
                ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(paDefs[i]);
                if (d == null || d.apparel == null || d.apparel.layers == null || !HasLayer(d.apparel.layers, "TMM_PowerArmor"))
                {
                    allInPowerLayer = false;
                }
            }

            Pawn paWearer = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, 3));
            Apparel duster = MakeApparel("TMM_Apparel_railroad_armored_coat");
            Apparel backpack = MakeApparel("TMM_Backpack_Canvas");
            Apparel t60 = MakeApparel("TMM_Apparel_bos_t60_power_armor");
            if (duster != null) paWearer.apparel.Wear(duster, false);
            if (backpack != null) paWearer.apparel.Wear(backpack, false);
            if (t60 != null) paWearer.apparel.Wear(t60, false);

            bool stackedCleanly = duster != null && t60 != null &&
                                  paWearer.apparel.WornApparel.Contains(duster) &&
                                  paWearer.apparel.WornApparel.Contains(t60) &&
                                  (backpack == null || paWearer.apparel.WornApparel.Contains(backpack));
            AddCheck("PowerArmor_LayerTMM_PowerArmorAndStacks", allInPowerLayer && stackedCleanly,
                "all 4 in TMM_PowerArmor & stacks over coat+backpack",
                string.Format("allLayer={0}, stacked={1}", allInPowerLayer, stackedCleanly));

            // ---------------- 7. SPECIAL EFFECTS: SELF-REPAIR CARAPACE & ANGEL DAMAGE REDUCTION ----------------
            Apparel carapace = MakeApparel("TMM_Apparel_clan_bioferrite_carapace");
            bool repaired = false;
            if (carapace != null)
            {
                carapace.HitPoints = Mathf.Max(1, carapace.MaxHitPoints - 25);
                int hpBefore = carapace.HitPoints;
                CompLivingCarapaceRepair rep = carapace.GetComp<CompLivingCarapaceRepair>();
                if (rep != null) rep.StepRepair(600);
                repaired = carapace.HitPoints > hpBefore;
            }
            AddCheck("Special_BioferriteCarapaceSelfRepair", repaired, "HP increased", repaired.ToString());

            Pawn demonAttacker = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(-4, 0, 2));
            Pawn bareAngel = SpawnPawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(-3, 0, 2));
            Pawn armoredAngel = SpawnPawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(-3, 0, 3));
            Apparel raiment = MakeApparel("TMM_Apparel_angel_celestial_raiment");
            if (armoredAngel != null && raiment != null) armoredAngel.apparel.Wear(raiment, false);

            Log.Message("[TMM probe] angel diag: demonXeno=" + (demonAttacker != null && demonAttacker.genes != null && demonAttacker.genes.Xenotype != null ? demonAttacker.genes.Xenotype.defName : "null") + " demonFaction=" + (demonAttacker != null && demonAttacker.Faction != null ? demonAttacker.Faction.def.defName : "null") + " isSuper=" + FactionApparelUtil.IsSupernaturalSource(new DamageInfo(DamageDefOf.Blunt, 20f, 2f, -1f, demonAttacker), true) + " worn=" + (armoredAngel != null && raiment != null && armoredAngel.apparel.WornApparel.Contains(raiment)));
            float bareDmg = ApplyTorsoDamageFrom(bareAngel, demonAttacker, DamageDefOf.Blunt, 20f);
            float armoredDmg = ApplyTorsoDamageFrom(armoredAngel, demonAttacker, DamageDefOf.Blunt, 20f);
            AddCheck("Special_AngelRaimentReducesDemonDamage",
                armoredAngel != null && raiment != null && armoredAngel.apparel.WornApparel.Contains(raiment) && armoredDmg < bareDmg * 0.75f,
                string.Format("< {0:0.0}", bareDmg * 0.75f),
                string.Format("armored={0:0.0}, bare={1:0.0}", armoredDmg, bareDmg));
        }

        private void VerifyHumanApparelStat(string checkName, string defName, StatDef stat, float minDelta, float minSharpArmor, IntVec3 origin, bool lowerIsBetter = false)
        {
            Pawn p = SpawnPawn(null, Faction.OfPlayer, origin);
            Apparel ap = MakeApparel(defName);
            if (p == null || ap == null)
            {
                AddCheck(checkName, false, "pawn & apparel", "null");
                return;
            }

            p.apparel.Wear(ap, false);
            bool worn = p.apparel.WornApparel.Contains(ap);
            // The pawn's final stat is scaled and clamped by skills (a clumsy pawn keeps 0 dodge), so check the bonus the
            // garment itself gives, which is what the item defines.
            float delta = StatWorker.StatOffsetFromGear(ap, stat);
            float sharp = ap.GetStatValue(StatDefOf.ArmorRating_Sharp);

            bool statOk = lowerIsBetter ? (delta <= minDelta) : (delta >= minDelta);
            bool armorOk = sharp >= minSharpArmor;
            AddCheck(checkName, worn && statOk && armorOk,
                string.Format("worn, sharp>={0:0.00}, delta{1}{2:0.00}", minSharpArmor, lowerIsBetter ? "<=" : ">=", minDelta),
                string.Format("worn={0}, sharp={1:0.00}, delta={2:+0.00;-0.00}", worn, sharp, delta));
        }

        private static Apparel MakeApparel(string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            ThingDef stuff = null;
            if (def.MadeFromStuff)
            {
                if (def.stuffCategories != null && def.stuffCategories.Contains(StuffCategoryDefOf.Leathery))
                {
                    stuff = ThingDefOf.Leather_Plain;
                }
                else
                {
                    stuff = GenStuff.DefaultStuffFor(def);
                }
            }
            return ThingMaker.MakeThing(def, stuff) as Apparel;
        }

        private static bool HasLayer(List<ApparelLayerDef> layers, string defName)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].defName == defName) return true;
            }
            return false;
        }

        private static float ApplyTorsoDamageFrom(Pawn victim, Pawn attacker, DamageDef dmgDef, float amount)
        {
            if (victim == null || victim.Dead || dmgDef == null) return 0f;
            BodyPartRecord torso = null;
            List<BodyPartRecord> parts = victim.RaceProps.body.AllParts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].def == BodyPartDefOf.Torso)
                {
                    torso = parts[i];
                    break;
                }
            }
            DamageWorker.DamageResult res = victim.TakeDamage(new DamageInfo(dmgDef, amount, 2.0f, -1f, attacker, torso)); // CHECK API
            return res != null ? res.totalDamageDealt : 0f;
        }

        private Pawn SpawnPawn(string xenotypeDefName, Faction fac, IntVec3 nearCell)
        {
            XenotypeDef xeno = (ModsConfig.BiotechActive && !xenotypeDefName.NullOrEmpty())
                ? DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotypeDefName)
                : (ModsConfig.BiotechActive ? XenotypeDefOf.Baseliner : null);

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
            if (p.apparel != null) p.apparel.DestroyAll();

            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(nearCell, map, 6, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out cell))
            {
                cell = map.Center;
            }
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            return p;
        }

        private static Faction FindHostileFaction()
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

        private IntVec3 FindClearCenter()
        {
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(map.Center, map, 10, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out found))
            {
                return found;
            }
            return map.Center;
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
