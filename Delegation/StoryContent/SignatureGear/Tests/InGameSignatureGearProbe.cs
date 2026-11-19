using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// In-game verification probe for all 30 signature hero items, gear sets, Castiel's blade
    /// one-shot vs regular demon (and NOT demon hero), HeroRegistry signature apparel spawn,
    /// recipe flag gating, EyeCover layer, and Alpha Vampire cloak MoveSpeed +0.10.
    /// Activated via -tmm-signature-gear-probe.
    /// </summary>
    public sealed class InGameSignatureGearProbe : MapComponent
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

        private static readonly MethodInfo ApplyMeleeDamageMethod = AccessTools.Method(typeof(Verb_MeleeAttack), "ApplyMeleeDamageToTarget"); // CHECK API

        public InGameSignatureGearProbe(Map map) : base(map)
        {
        }

        private static bool ProbeEnabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "-tmm-signature-gear-probe", StringComparison.OrdinalIgnoreCase)) return true;
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
                WriteReport("SignatureGearProbe.json");
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
            IntVec3 origin = FindClearCenter();

            // ---------------- 1. ALL 30 SIGNATURE ITEMS LOADED ----------------
            string[] allSigIds = new string[]
            {
                "TMM_Sig_dean_leather_jacket", "TMM_Sig_dean_sawed_off_shotgun", "TMM_Sig_sam_hunting_flannel",
                "TMM_Sig_preston_garvey_hat", "TMM_Sig_bobby_singer_cap_vest", "TMM_Sig_castiel_trenchcoat",
                "TMM_Sig_castiel_angel_blade", "TMM_Sig_michael_archangel_plate", "TMM_Sig_lucifer_white_suit",
                "TMM_Sig_crowley_king_suit", "TMM_Sig_ruby_demon_knife", "TMM_Sig_maxson_battlecoat",
                "TMM_Sig_danse_recon_hood", "TMM_Sig_casdin_protector_helm", "TMM_Sig_autumn_enclave_greatcoat",
                "TMM_Sig_father_director_coat", "TMM_Sig_kellogg_cyber_jacket", "TMM_Sig_kellogg_revolver",
                "TMM_Sig_desdemona_railroad_coat", "TMM_Sig_deacon_sunglasses", "TMM_Sig_wes_gunner_coat",
                "TMM_Sig_braun_white_suit", "TMM_Sig_dr_hess_monocle", "TMM_Sig_ketch_arsenal_briefcase_coat",
                "TMM_Sig_hancock_tricorn_coat", "TMM_Sig_silver_shroud_costume", "TMM_Sig_fawkes_vault87_harness",
                "TMM_Sig_swan_anchor_club", "TMM_Sig_alpha_vampire_cloak", "TMM_Sig_dick_roman_corporate_suit"
            };
            int loadedCount = 0;
            for (int i = 0; i < allSigIds.Length; i++)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(allSigIds[i]) != null) loadedCount++;
            }
            AddCheck("Defs_All30SignatureItemsLoaded", loadedCount == 30, "30", loadedCount.ToString());

            // ---------------- 2. DEAN SET BONUS: EQUIP BOTH -> HEDIFF ON, UNEQUIP ONE -> HEDIFF OFF ----------------
            Pawn deanTester = SpawnPawn(null, Faction.OfPlayer, origin);
            Apparel deanJacket = MakeApparel("TMM_Sig_dean_leather_jacket");
            ThingWithComps deanShotgun = MakeWeapon("TMM_Sig_dean_sawed_off_shotgun");

            if (deanJacket != null) deanTester.apparel.Wear(deanJacket, false);
            bool hasBeforeShotgun = SignatureGearUtil.HasSetBonus(deanTester, "TMM_Hediff_Set_Dean");

            if (deanShotgun != null) deanTester.equipment.AddEquipment(deanShotgun);
            bool hasWithBoth = SignatureGearUtil.HasSetBonus(deanTester, "TMM_Hediff_Set_Dean");
            bool panicBlocked = !deanTester.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, "probe", true, false, false, null); // CHECK API

            Apparel droppedJacket;
            if (deanJacket != null) deanTester.apparel.TryDrop(deanJacket, out droppedJacket, deanTester.Position, false);
            bool goneAfterUnequip = !SignatureGearUtil.HasSetBonus(deanTester, "TMM_Hediff_Set_Dean");

            AddCheck("SetBonus_DeanToggleAndPanicImmunity",
                !hasBeforeShotgun && hasWithBoth && panicBlocked && goneAfterUnequip,
                "off->on(panicBlocked)->off",
                string.Format("before={0}, both={1}, panicBlocked={2}, afterDrop={3}",
                    hasBeforeShotgun, hasWithBoth, panicBlocked, !goneAfterUnequip));

            // ---------------- 3. CASTIEL & KELLOGG SET BONUSES ----------------
            Pawn angel = SpawnPawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(2, 0, 0));
            Apparel casCoat = MakeApparel("TMM_Sig_castiel_trenchcoat");
            ThingWithComps casBlade = MakeWeapon("TMM_Sig_castiel_angel_blade");
            if (angel != null && casCoat != null) angel.apparel.Wear(casCoat, false);
            if (angel != null && casBlade != null) angel.equipment.AddEquipment(casBlade);
            bool casSetActive = angel != null && SignatureGearUtil.HasSetBonus(angel, "TMM_Hediff_Set_Castiel");
            AddCheck("SetBonus_CastielActive", casSetActive, "true", casSetActive.ToString());

            Pawn kelloggTester = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(-2, 0, 0));
            Apparel kelJacket = MakeApparel("TMM_Sig_kellogg_cyber_jacket");
            ThingWithComps kelGun = MakeWeapon("TMM_Sig_kellogg_revolver");
            if (kelJacket != null) kelloggTester.apparel.Wear(kelJacket, false);
            if (kelGun != null) kelloggTester.equipment.AddEquipment(kelGun);
            bool kelSetActive = SignatureGearUtil.HasSetBonus(kelloggTester, "TMM_Hediff_Set_Kellogg");
            AddCheck("SetBonus_KelloggActive", kelSetActive, "true", kelSetActive.ToString());

            // ---------------- 4. CASTIEL'S BLADE: KILLS REGULAR DEMON, NOT HERO DEMON ----------------
            Pawn regularDemon = SpawnPawn("TMM_Demon", FindHostileFaction(), angel != null ? angel.Position + new IntVec3(1, 0, 0) : origin);
            if (angel != null && casBlade != null && regularDemon != null)
            {
                StrikeMeleeOnce(angel, casBlade, regularDemon);
            }
            AddCheck("CastielBlade_KillsRegularDemonInOneHit", regularDemon != null && regularDemon.Dead,
                "Dead=true", regularDemon != null ? regularDemon.Dead.ToString() : "null");

            HeroDef demonHeroDef = DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_crowley") ??
                                   DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_meg");
            Pawn demonHero = (demonHeroDef != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(demonHeroDef) : null;
            if (demonHero != null)
            {
                if (demonHero.Spawned) demonHero.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(demonHero, (angel != null ? angel.Position : origin) + new IntVec3(-1, 0, 0), map, WipeMode.Vanish);
                if (angel != null && casBlade != null)
                {
                    StrikeMeleeOnce(angel, casBlade, demonHero);
                }
            }
            AddCheck("CastielBlade_DoesNotOneShotDemonHero", demonHero != null && !demonHero.Dead,
                "Dead=false (registered hero survives one-shot)",
                demonHero != null ? string.Format("hero={0}, dead={1}", demonHeroDef.defName, demonHero.Dead) : "hero null");

            // ---------------- 5. HERO REGISTRY GETORCREATE: DEAN WEARS HIS JACKET ----------------
            HeroDef deanDef = DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_dean_winchester");
            Pawn deanHero = (deanDef != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(deanDef) : null;
            bool deanHasJacket = deanHero != null && SignatureGearUtil.IsWearingDef(deanHero, "TMM_Sig_dean_leather_jacket");
            AddCheck("HeroRegistry_DeanWearsSignatureJacket", deanHasJacket, "wearing TMM_Sig_dean_leather_jacket",
                deanHero != null ? deanHasJacket.ToString() : "deanHero null");

            // ---------------- 6. RECIPE FLAG GATING (LOCKED WITHOUT FLAG, OPEN WITH FLAG) ----------------
            SparkStory story = SparkStory.Instance;
            RecipeDef deanJacketRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("TMM_Make_Sig_dean_leather_jacket");
            if (story != null && deanJacketRecipe != null)
            {
                story.SetFlag("unlock.dean_leather_jacket", "false"); // CHECK API
                bool lockedBefore = !deanJacketRecipe.AvailableNow;
                story.SetFlag("unlock.dean_leather_jacket", "true"); // CHECK API
                bool openAfter = deanJacketRecipe.AvailableNow;
                AddCheck("RecipeGating_DeanJacketFlag", lockedBefore && openAfter,
                    "locked->open", string.Format("{0}->{1}", lockedBefore, openAfter));
            }
            else
            {
                AddCheck("RecipeGating_DeanJacketFlag", false, "story & recipe exist",
                    string.Format("story={0}, recipe={1}", story != null, deanJacketRecipe != null));
            }

            // ---------------- 7. CLAUDE'S BALANCE & LAYER RULES ----------------
            Pawn cloakTester = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, 3));
            float baseMove = cloakTester.GetStatValue(StatDefOf.MoveSpeed);
            Apparel alphaCloak = MakeApparel("TMM_Sig_alpha_vampire_cloak");
            if (alphaCloak != null) cloakTester.apparel.Wear(alphaCloak, false);
            float moveDelta = cloakTester.GetStatValue(StatDefOf.MoveSpeed) - baseMove;
            AddCheck("Balance_AlphaCloakMoveSpeed010",
                alphaCloak != null && moveDelta >= 0.08f && moveDelta <= 0.13f,
                "delta ~ +0.10", string.Format("{0:+0.000;-0.000}", moveDelta));

            ThingDef shadesDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Sig_deacon_sunglasses");
            string expectedEyeLayer = ModsConfig.IdeologyActive ? "EyeCover" : "Overhead";
            bool shadesLayerOk = shadesDef != null && shadesDef.apparel != null && shadesDef.apparel.layers != null &&
                                 HasLayer(shadesDef.apparel.layers, expectedEyeLayer);
            AddCheck("Layer_DeaconShadesEyeCoverOrOverhead", shadesLayerOk, expectedEyeLayer, shadesLayerOk.ToString());

            // Swan's Anchor blocked on ordinary human, allowed on Super Mutant
            Pawn human = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, -3));
            Pawn mutant = SpawnPawn("TMM_SuperMutant", Faction.OfPlayer, origin + new IntVec3(1, 0, -3));
            ThingWithComps anchor = MakeWeapon("TMM_Sig_swan_anchor_club");
            string humanReason;
            string mutantReason;
            bool humanCanAnchor = anchor != null && EquipmentUtility.CanEquip(anchor, human, out humanReason); // CHECK API
            bool mutantCanAnchor = anchor != null && mutant != null && EquipmentUtility.CanEquip(anchor, mutant, out mutantReason); // CHECK API
            AddCheck("Restriction_SwanAnchorMutantOrPowerArmor", !humanCanAnchor && mutantCanAnchor,
                "human=false, mutant=true", string.Format("human={0}, mutant={1}", humanCanAnchor, mutantCanAnchor));
        }

        private bool StrikeMeleeOnce(Pawn attacker, ThingWithComps weapon, Pawn target)
        {
            if (attacker == null || weapon == null || target == null || ApplyMeleeDamageMethod == null) return false;
            CompEquippable eq = weapon.GetComp<CompEquippable>();
            if (eq == null || eq.AllVerbs == null) return false;

            Verb_MeleeAttack meleeVerb = null;
            for (int i = 0; i < eq.AllVerbs.Count; i++)
            {
                Verb_MeleeAttack v = eq.AllVerbs[i] as Verb_MeleeAttack;
                if (v != null)
                {
                    meleeVerb = v;
                    break;
                }
            }
            if (meleeVerb == null) return false;

            meleeVerb.caster = attacker;
            ApplyMeleeDamageMethod.Invoke(meleeVerb, new object[] { new LocalTargetInfo(target) });
            return true;
        }

        private static Apparel MakeApparel(string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            return ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null) as Apparel;
        }

        private static ThingWithComps MakeWeapon(string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            return ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null) as ThingWithComps;
        }

        private static bool HasLayer(List<ApparelLayerDef> layers, string defName)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].defName == defName) return true;
            }
            return false;
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
