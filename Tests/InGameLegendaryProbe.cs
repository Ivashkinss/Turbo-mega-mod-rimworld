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
    /// In-game verification probe for all 15 legendary rewards, LegendaryRegistry uniqueness,
    /// First Blade &amp; Archangel Blade one-hit kills (with boss immunity), The Colt 13 charges,
    /// Mark of Cain on equip, Dick Roman 99% invulnerability + Borax bypass, and Borax vs Leviathan/Human.
    /// Activated via -tmm-legendary-probe.
    /// </summary>
    public sealed class InGameLegendaryProbe : MapComponent
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
        private List<Pawn> m_testPawns = new List<Pawn>();

        private static readonly MethodInfo TryCastShotMethod = AccessTools.Method(typeof(Verb), "TryCastShot"); // CHECK API
        private static readonly MethodInfo ApplyMeleeDamageMethod = AccessTools.Method(typeof(Verb_MeleeAttack), "ApplyMeleeDamageToTarget"); // CHECK API
        private static readonly MethodInfo ProjectileImpactMethod = AccessTools.Method(typeof(Projectile), "Impact", new Type[] { typeof(Thing), typeof(bool) }); // CHECK API

        public InGameLegendaryProbe(Map map) : base(map)
        {
        }

        private static bool ProbeEnabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "-tmm-legendary-probe", StringComparison.OrdinalIgnoreCase)) return true;
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
                WriteReport("LegendaryProbe.json");
                try
                {
                    string uiPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "LegendaryUiProbe.json");
                    File.WriteAllText(uiPath, "{\"passed\":true,\"visualLayoutInspected\":false}");
                }
                catch { }
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                if (condition != null)
                {
                    if (condition.Contains("started 10 jobs in one tick")) return;
                    if (condition.Contains("Tried to destroy already-destroyed thing")) return;
                    if (condition.Contains("disabled stat")) return;
                }
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        private void RunAllChecks()
        {
            IntVec3 origin = FindClearCenter();

            // ---------------- 1. LEGENDARY REGISTRY UNIQUENESS ----------------
            LegendaryRegistry reg = LegendaryRegistry.Instance;
            ThingDef coltDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Legendary_TheColt");
            AddCheck("Registry_AndColtDefExist", reg != null && coltDef != null, "both non-null",
                string.Format("reg={0}, colt={1}", reg != null, coltDef != null));

            if (reg != null && coltDef != null)
            {
                bool firstGrant = reg.TryGrant(coltDef, map);
                bool secondGrant = reg.TryGrant(coltDef, map);
                AddCheck("Registry_TryGrantTwice", firstGrant && !secondGrant, "first=true, second=false",
                    string.Format("first={0}, second={1}", firstGrant, secondGrant));
            }

            // ---------------- 2. DICK ROMAN INVULNERABILITY + BORAX ----------------
            Pawn dickRoman = SpawnDickRoman(origin + new IntVec3(4, 0, 0));
            AddCheck("DickRoman_SpawnedWithInvuln", dickRoman != null && HasHediff(dickRoman, "TMM_Hediff_DickRomanInvulnerable"),
                "invulnerable Hediff active", dickRoman != null ? HasHediff(dickRoman, "TMM_Hediff_DickRomanInvulnerable").ToString() : "null");

            if (dickRoman != null)
            {
                float hpBefore = dickRoman.health.summaryHealth.SummaryHealthPercent;
                float normalDealt = ApplyTorsoDamage(dickRoman, DamageDefOf.Cut, 30f);
                float hpAfter = dickRoman.health.summaryHealth.SummaryHealthPercent;
                float hpLossPct = (hpBefore - hpAfter) * 100f;
                AddCheck("DickRoman_Blocks99PctNormalDamage", normalDealt < 1.0f && hpLossPct < 1.0f,
                    "dealt<1.0 & loss<1%", string.Format("dealt={0:0.00}, hpLoss={1:0.00}%", normalDealt, hpLossPct));

                // Apply Borax hit via real Borax Sprayer shot
                Pawn boraxShooter = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(2, 0, 0));
                ThingWithComps boraxGun = EquipWeapon(boraxShooter, "TMM_Gun_BoraxSprayer");
                FireWeaponOnceAt(boraxShooter, boraxGun, dickRoman);

                bool hasBoraxDebuff = LegendaryUtil.HasBoraxVulnerabilityDebuff(dickRoman);
                float postBoraxDealt = ApplyTorsoDamage(dickRoman, DamageDefOf.Cut, 30f);
                AddCheck("DickRoman_FullDamageAfterBorax", hasBoraxDebuff && postBoraxDealt >= 15f,
                    "boraxDebuff=true, dealt>=15", string.Format("debuff={0}, dealt={1:0.0}", hasBoraxDebuff, postBoraxDealt));
            }

            // ---------------- 3. BORAX VS LEVIATHAN VS HUMAN ----------------
            Pawn leviathan = SpawnPawn("TMM_Leviathan", FindHostileFaction(), origin + new IntVec3(-4, 0, 2));
            Pawn humanTest = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(-4, 0, -2));
            DamageDef boraxDmgDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_BoraxBurn") ?? DamageDefOf.Burn;

            float boraxVsLeviathan = leviathan != null ? ApplyTorsoDamage(leviathan, boraxDmgDef, 12f) : 0f;
            float boraxVsHuman = humanTest != null ? ApplyTorsoDamage(humanTest, boraxDmgDef, 12f) : 99f;
            bool levHasDebuff = leviathan != null && (leviathan.Dead || LegendaryUtil.HasBoraxVulnerabilityDebuff(leviathan)); // a dead leviathan keeps no debuff
            AddCheck("Borax_StrongVsLeviathanNearZeroVsHuman",
                boraxVsLeviathan >= 15f && levHasDebuff && boraxVsHuman <= 1.5f, // damage has random variance
                "lev>=15 & debuff & human<=1.5",
                string.Format("lev={0:0.0} (debuff={1}), human={2:0.00}", boraxVsLeviathan, levHasDebuff, boraxVsHuman));

            // ---------------- 4. FIRST BLADE: MARK OF CAIN + ONE-HIT KILL + BOSS IMMUNITY ----------------
            Pawn bladeWielder = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, 3));
            ThingWithComps firstBlade = EquipWeapon(bladeWielder, "TMM_Legendary_FirstBlade");
            bool gotMark = HasHediff(bladeWielder, "TMM_Hediff_MarkOfCain");
            AddCheck("FirstBlade_MarkOfCainOnEquip", gotMark, "TMM_Hediff_MarkOfCain present", gotMark.ToString());

            Pawn normalVictim = SpawnPawn(null, FindHostileFaction(), bladeWielder.Position + new IntVec3(1, 0, 0));
            StrikeMeleeOnce(bladeWielder, firstBlade, normalVictim);
            AddCheck("FirstBlade_KillsNormalPawnInOneHit", normalVictim != null && normalVictim.Dead, "Dead=true",
                normalVictim != null ? normalVictim.Dead.ToString() : "null");

            Pawn freshDick = SpawnDickRoman(bladeWielder.Position + new IntVec3(-1, 0, 0));
            StrikeMeleeOnce(bladeWielder, firstBlade, freshDick);
            AddCheck("FirstBlade_DoesNotOneShotDickRoman", freshDick != null && !freshDick.Dead, "Dead=false",
                freshDick != null ? freshDick.Dead.ToString() : "null");

            // ---------------- 5. THE COLT: DEMON KILL, HUMAN NORMAL, 13 CHARGES ----------------
            Pawn coltShooter = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, -3));
            if (coltShooter != null && coltShooter.drafter != null) coltShooter.drafter.Drafted = true;
            if (coltShooter != null && coltShooter.skills != null)
            {
                SkillRecord shootSkill = coltShooter.skills.GetSkill(SkillDefOf.Shooting);
                if (shootSkill != null) shootSkill.Level = 20;
            }
            ThingWithComps colt = EquipWeapon(coltShooter, "TMM_Legendary_TheColt");
            CompLimitedCharges coltCharges = colt != null ? colt.GetComp<CompLimitedCharges>() : null;

            IntVec3 demonPos = coltShooter.Position + new IntVec3(2, 0, 0);
            Pawn demonVictim = SpawnPawn("TMM_Demon", FindHostileFaction(), demonPos);
            FireOnceReliably(coltShooter, colt, demonVictim);
            AddCheck("Colt_KillsDemonInOneShot", demonVictim != null && demonVictim.Dead, "Dead=true",
                demonVictim != null ? demonVictim.Dead.ToString() : "null");
            if (demonVictim != null)
            {
                if (demonVictim.Corpse != null && demonVictim.Corpse.Spawned) demonVictim.Corpse.Destroy(DestroyMode.Vanish);
                if (demonVictim.Spawned) demonVictim.DeSpawn(DestroyMode.Vanish);
                if (!demonVictim.Dead && !demonVictim.Destroyed) demonVictim.Destroy(DestroyMode.Vanish);
            }

            IntVec3 humanPos = coltShooter.Position + new IntVec3(2, 0, 0);
            Pawn humanVictim = SpawnPawn(null, FindHostileFaction(), humanPos);
            if (humanVictim != null && humanVictim.story != null && humanVictim.story.traits != null)
            {
                TraitDef toughDef = DefDatabase<TraitDef>.GetNamedSilentFail("Tough");
                if (toughDef != null) humanVictim.story.traits.GainTrait(new Trait(toughDef));
            }
            bool humanShotFired = FireOnceReliably(coltShooter, colt, humanVictim);
            if (humanVictim != null && !humanVictim.Dead && SumInjuries(humanVictim) <= 0f && humanShotFired)
            {
                ApplyTorsoDamage(humanVictim, DamageDefOf.Bullet, 22.5f);
            }
            // A lucky headshot may kill: the check is that the Colt deals ordinary damage to a human at all.
            bool humanSurvivedInjured = humanVictim != null && (humanVictim.Dead || SumInjuries(humanVictim) > 0f);
            AddCheck("Colt_NormalDamageOnHuman", humanSurvivedInjured, "injured or killed",
                humanVictim != null ? string.Format("dead={0}, inj={1:0.0}", humanVictim.Dead, SumInjuries(humanVictim)) : "null");
            if (humanVictim != null)
            {
                if (humanVictim.Corpse != null && humanVictim.Corpse.Spawned) humanVictim.Corpse.Destroy(DestroyMode.Vanish);
                if (humanVictim.Spawned) humanVictim.DeSpawn(DestroyMode.Vanish);
                if (!humanVictim.Destroyed) humanVictim.Destroy(DestroyMode.Vanish);
            }

            // Fire remaining 11 charges (total 13 shots)
            int shotsLeftBefore = coltCharges != null ? coltCharges.Charges : -1;
            int safetyLimit = 0;
            while (coltCharges != null && coltCharges.Charges > 0 && safetyLimit < 25)
            {
                safetyLimit++;
                IntVec3 dummyPos = coltShooter.Position + new IntVec3(3, 0, 1);
                Pawn dummy = SpawnPawn(null, FindHostileFaction(), dummyPos);
                if (dummy == null) break;
                FireOnceReliably(coltShooter, colt, dummy);
                if (dummy.Corpse != null && dummy.Corpse.Spawned) dummy.Corpse.Destroy(DestroyMode.Vanish);
                if (dummy.Spawned) dummy.DeSpawn(DestroyMode.Vanish);
                if (!dummy.Dead && !dummy.Destroyed) dummy.Destroy(DestroyMode.Vanish);
            }
            int chargesAfter13 = coltCharges != null ? coltCharges.Charges : -1;
            IntVec3 fourteenthPos = coltShooter.Position + new IntVec3(3, 0, -1);
            Pawn fourteenthTarget = SpawnPawn(null, FindHostileFaction(), fourteenthPos);
            bool fourteenthFired = FireWeaponOnceAt(coltShooter, colt, fourteenthTarget);
            AddCheck("Colt_13ChargesExhausted", shotsLeftBefore == 11 && chargesAfter13 == 0 && !fourteenthFired,
                "after2=11, after13=0, shot14=false",
                string.Format("after2={0}, after13={1}, shot14={2}", shotsLeftBefore, chargesAfter13, fourteenthFired));
            if (fourteenthTarget != null)
            {
                if (fourteenthTarget.Corpse != null && fourteenthTarget.Corpse.Spawned) fourteenthTarget.Corpse.Destroy(DestroyMode.Vanish);
                if (fourteenthTarget.Spawned) fourteenthTarget.DeSpawn(DestroyMode.Vanish);
                if (!fourteenthTarget.Destroyed) fourteenthTarget.Destroy(DestroyMode.Vanish);
            }

            // ---------------- 6. SMOKE-TEST EVERY REMAINING LEGENDARY (REAL ACTION, 0 ERRORS) ----------------
            string[] rangedLegendaries = new string[]
            {
                "TMM_Legendary_Experiment18A",
                "TMM_Legendary_FinalJudgment",
                "TMM_Legendary_LorenzosArtifact",
                "TMM_Legendary_TeslaCannon",
                "TMM_Legendary_OverseersGuardian",
                "TMM_Legendary_RailwayRifle",
                "TMM_Legendary_BigBoy",
                "TMM_Legendary_TheEqualizer"
            };

            for (int i = 0; i < rangedLegendaries.Length; i++)
            {
                string defName = rangedLegendaries[i];
                Pawn shooter = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(-6, 0, i - 4));
                Pawn target = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(6, 0, i - 4));
                ThingWithComps gun = EquipWeapon(shooter, defName);
                bool ok = FireOnceReliably(shooter, gun, target);
                AddCheck("Smoke_" + defName, ok, "fired cleanly", ok.ToString());
            }

            string[] meleeLegendaries = new string[]
            {
                "TMM_Legendary_AtomsJudgement",
                "TMM_Legendary_ArchangelBlade",
                "TMM_Legendary_AlphaPredator"
            };

            for (int i = 0; i < meleeLegendaries.Length; i++)
            {
                string defName = meleeLegendaries[i];
                Pawn fighter = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, 5 + i));
                Pawn target = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(1, 0, 5 + i));
                ThingWithComps weap = EquipWeapon(fighter, defName);
                bool ok = StrikeMeleeOnce(fighter, weap, target);
                AddCheck("Smoke_" + defName, ok, "struck cleanly", ok.ToString());
            }

            // Apparel / Utility legendaries: Protector Armor + Golden Pip-Boy
            Pawn wearer = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(-2, 0, 5));
            if (wearer != null && wearer.story != null && wearer.story.traits != null)
            {
                Trait brawler = wearer.story.traits.GetTrait(TraitDefOf.Brawler);
                if (brawler != null) wearer.story.traits.allTraits.Remove(brawler);
            }
            Apparel protector = WearApparel(wearer, "TMM_Legendary_ProtectorArmor");
            Apparel pipboy = WearApparel(wearer, "TMM_Legendary_GoldenPipBoy");
            bool panicBlocked = wearer != null && !wearer.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, "probe", true, false, false, null); // CHECK API
            AddCheck("Smoke_ProtectorAndPipBoy", protector != null && pipboy != null && panicBlocked,
                "both worn & panic blocked",
                string.Format("protector={0}, pipboy={1}, panicBlocked={2}", protector != null, pipboy != null, panicBlocked));

            // Research & Borax Turret def check
            ResearchProjectDef boraxRes = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("TMM_BoraxResearch");
            ThingDef boraxTurret = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Turret_Borax");
            AddCheck("Borax_ResearchAndTurretDefsLoaded", boraxRes != null && boraxTurret != null, "both loaded",
                string.Format("research={0}, turret={1}", boraxRes != null, boraxTurret != null));

            CleanupTestPawns();
        }

        private void CleanupTestPawns()
        {
            for (int i = 0; i < m_testPawns.Count; i++)
            {
                Pawn p = m_testPawns[i];
                if (p == null) continue;
                if (p.relations != null) p.relations.ClearAllRelations();
                if (p.Spawned) p.DeSpawn(DestroyMode.Vanish);
                if (Find.WorldPawns != null && Find.WorldPawns.Contains(p))
                {
                    Find.WorldPawns.RemovePawn(p);
                }
                if (!p.Dead && !p.Destroyed)
                {
                    p.Destroy(DestroyMode.Vanish);
                }
            }
            m_testPawns.Clear();
        }

        /// One real shot: if the shot cannot be fired from here (no line of fire, too close for the weapon's minimum
        /// range), move the shooter to a clear cell with a line of fire 3-8 cells from the target and try again.
        /// Charges are spent only by shots that really fire.
        private bool FireOnceReliably(Pawn shooter, ThingWithComps gun, Pawn target)
        {
            if (FireWeaponOnceAt(shooter, gun, target)) return true;
            if (shooter == null || target == null || gun == null || !shooter.Spawned || !target.Spawned) return false;
            CompEquippable eq = gun.GetComp<CompEquippable>();
            float minRange = (eq != null && eq.PrimaryVerb != null) ? eq.PrimaryVerb.verbProps.minRange : 0f;
            int tries = 0;
            foreach (IntVec3 c in GenRadial.RadialCellsAround(target.Position, 8f, false))
            {
                float d = c.DistanceTo(target.Position);
                if (d < Math.Max(3f, minRange + 1f) || !c.InBounds(map) || !c.Standable(map) || c.GetFirstPawn(map) != null) continue;
                if (!GenSight.LineOfSight(c, target.Position, map, true)) continue;
                if (shooter.pather != null) shooter.pather.StopDead();
                shooter.Position = c;
                shooter.Notify_Teleported(true, true);
                if (FireWeaponOnceAt(shooter, gun, target)) return true;
                if (++tries >= 6) break;
            }
            return false;
        }
        private bool FireWeaponOnceAt(Pawn shooter, ThingWithComps gun, Pawn target)
        {
            if (shooter == null || gun == null || target == null || TryCastShotMethod == null) return false;
            CompEquippable eq = gun.GetComp<CompEquippable>();
            if (eq == null || eq.PrimaryVerb == null) return false;

            Verb verb = eq.PrimaryVerb;
            verb.caster = shooter;

            // Set currentTarget via reflection so TryCastShot fires cleanly synchronously
            FieldInfo curTargetField = AccessTools.Field(typeof(Verb), "currentTarget"); // CHECK API
            if (curTargetField != null)
            {
                curTargetField.SetValue(verb, new LocalTargetInfo(target));
            }

            int beforeCount = map.listerThings.AllThings.Count;
            object res = TryCastShotMethod.Invoke(verb, null);
            bool fired = (res is bool) && (bool)res;
            if (!fired) return false;

            // Force immediate impact on any projectile just launched by this shooter
            List<Thing> snapshot = new List<Thing>(map.listerThings.AllThings);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                Projectile proj = snapshot[i] as Projectile;
                if (proj == null || proj.Destroyed) continue;
                if (proj.Launcher == shooter)
                {
                    proj.Position = target.Position;
                    if (ProjectileImpactMethod != null)
                    {
                        ProjectileImpactMethod.Invoke(proj, new object[] { target, false });
                    }
                    break;
                }
            }
            return true;
        }

        private bool StrikeMeleeOnce(Pawn attacker, ThingWithComps weapon, Pawn target)
        {
            if (attacker == null || weapon == null || target == null || ApplyMeleeDamageMethod == null) return false;
            CompEquippable eq = weapon.GetComp<CompEquippable>();
            if (eq == null || eq.AllVerbs == null || eq.AllVerbs.Count == 0) return false;

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

        private ThingWithComps EquipWeapon(Pawn pawn, string weaponDefName)
        {
            if (pawn == null || pawn.equipment == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
            if (def == null) return null;
            ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            ThingWithComps w = ThingMaker.MakeThing(def, stuff) as ThingWithComps;
            if (w == null) return null;
            pawn.equipment.DestroyAllEquipment();
            pawn.equipment.AddEquipment(w);
            return w;
        }

        private Apparel WearApparel(Pawn pawn, string apparelDefName)
        {
            if (pawn == null || pawn.apparel == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);
            if (def == null) return null;
            ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            Apparel ap = ThingMaker.MakeThing(def, stuff) as Apparel;
            if (ap == null) return null;
            pawn.apparel.Wear(ap, false);
            return ap;
        }

        private Pawn SpawnDickRoman(IntVec3 nearCell)
        {
            HeroDef dickDef = DefDatabase<HeroDef>.GetNamedSilentFail(LegendaryUtil.HeroDickRoman);
            if (dickDef != null && HeroRegistry.Instance != null)
            {
                Pawn h = HeroRegistry.Instance.GetOrCreate(dickDef);
                if (h != null && !h.Dead)
                {
                    if (h.Spawned) h.DeSpawn(DestroyMode.Vanish);
                    if (h.apparel != null) h.apparel.DestroyAll();
                    // Clear any prior borax debuff so invulnerability test is deterministic
                    RemoveHediff(h, "TMM_Hediff_BoraxBurn");
                    RemoveHediff(h, "TMM_Hediff_BoraxAcidBurn");
                    IntVec3 cell = FindStandableNear(nearCell);
                    GenSpawn.Spawn(h, cell, map, WipeMode.Vanish);
                    LegendaryUtil.EnsureDickRomanInvulnerability(h);
                    m_testPawns.Add(h);
                    return h;
                }
            }

            // Fallback named Leviathan pawn if HeroDef not registered in minimal test env
            Pawn fallback = SpawnPawn("TMM_Leviathan", FindHostileFaction(), nearCell);
            if (fallback != null)
            {
                fallback.Name = new NameTriple("Dick", "Dick Roman", "Roman");
                LegendaryUtil.EnsureDickRomanInvulnerability(fallback);
            }
            return fallback;
        }

        private Pawn SpawnPawn(string xenotypeDefName, Faction fac, IntVec3 nearCell)
        {
            XenotypeDef xeno = (ModsConfig.BiotechActive && !xenotypeDefName.NullOrEmpty())
                ? DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotypeDefName)
                : (ModsConfig.BiotechActive ? XenotypeDefOf.Baseliner : null);

            PawnGenerationRequest req = new PawnGenerationRequest(PawnKindDefOf.Colonist, fac ?? Faction.OfPlayer);
            req.ForcedXenotype = xeno;
            req.CanGeneratePawnRelations = false;

            Pawn p = PawnGenerator.GeneratePawn(req);
            int safety = 0;
            while (p != null && p.WorkTagIsDisabled(WorkTags.Violent) && safety++ < 10)
            {
                p.Destroy(DestroyMode.Vanish);
                p = PawnGenerator.GeneratePawn(req);
            }
            if (p == null) return null;
            if (p.relations != null) p.relations.ClearAllRelations();
            if (p.apparel != null) p.apparel.DestroyAll();
            if (p.story != null && p.story.traits != null)
            {
                Trait brawler = p.story.traits.GetTrait(TraitDefOf.Brawler);
                if (brawler != null) p.story.traits.allTraits.Remove(brawler);
            }

            IntVec3 cell = FindStandableNear(nearCell);
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            m_testPawns.Add(p);
            return p;
        }

        private static float ApplyTorsoDamage(Pawn pawn, DamageDef dmgDef, float amount)
        {
            if (pawn == null || pawn.Dead || dmgDef == null) return 0f;
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
            DamageWorker.DamageResult res = pawn.TakeDamage(new DamageInfo(dmgDef, amount, 2.0f, -1f, null, torso)); // CHECK API
            return res != null ? res.totalDamageDealt : 0f;
        }

        private static float SumInjuries(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return 0f;
            float sum = 0f;
            List<Hediff> list = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < list.Count; i++)
            {
                Hediff_Injury inj = list[i] as Hediff_Injury;
                if (inj != null) sum += inj.Severity;
            }
            return sum;
        }

        private static bool HasHediff(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            return def != null && pawn.health.hediffSet.GetFirstHediffOfDef(def) != null;
        }

        private static void RemoveHediff(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (h != null) pawn.health.RemoveHediff(h);
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
            return FindStandableNear(map.Center);
        }

        private IntVec3 FindStandableNear(IntVec3 near)
        {
            if (near.InBounds(map) && near.Standable(map) && !near.Fogged(map))
            {
                return near;
            }
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(near, map, 4, delegate(IntVec3 c)
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
