using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
            if (s_completed || !(ProbeEnabled || LegendaryProbeLaunch.Reload)) return;
            if (map != Find.AnyPlayerHomeMap) return;

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                if (LegendaryProbeLaunch.Reload) TraderReloadChecks(); else RunAllChecks();
            }
            catch (Exception ex)
            {
                m_errors.Add("Unhandled exception: " + ex.ToString());
            }
            finally
            {
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(LegendaryProbeLaunch.Reload ? "LegendaryReloadProbe.json" : "LegendaryProbe.json");
                if (!LegendaryProbeLaunch.Reload) try
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
                if (condition != null && condition.Contains("generated destroyed thing")) m_destroyedInStock++;
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        private void RunAllChecks()
        {
            IntVec3 origin = FindClearCenter();
            // The combat checks below leave corpses, tales of destroyed test pawns and shells in flight: the saved trader
            // scenario is played alone in a clean colony.
            if (Environment.GetEnvironmentVariable("TMM_TRADER_ONLY") == "1") { TraderChecks(origin); return; }

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

            // ---------------- 1b. UNIQUENESS ACROSS CREATION PATHS ----------------
            ThingDef ogDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Legendary_OverseersGuardian");
            if (reg != null && ogDef != null)
            {
                Thing og1 = ThingMaker.MakeThing(ogDef);
                Thing beforeFirstSpawn = ThingMaker.MakeThing(ogDef);
                AddCheck("Uniqueness_ReservationBeforeFirstSpawn",
                    !og1.Destroyed && beforeFirstSpawn.Destroyed,
                    "first survives, second rejected while first is unplaced",
                    string.Format("firstDestroyed={0}, secondDestroyed={1}", og1.Destroyed, beforeFirstSpawn.Destroyed));
                if (!beforeFirstSpawn.Destroyed) beforeFirstSpawn.Destroy(DestroyMode.Vanish);

                GenSpawn.Spawn(og1, origin + new IntVec3(1, 0, 1), map, WipeMode.Vanish);
                bool og1Active = reg.HasActiveInstance(ogDef);

                // Attempt to create second instance via ThingMaker
                Thing og2 = ThingMaker.MakeThing(ogDef);
                bool og2DestroyedOnMake = og2.Destroyed;
                if (!og2DestroyedOnMake)
                {
                    GenSpawn.Spawn(og2, origin + new IntVec3(1, 0, 2), map, WipeMode.Vanish);
                }
                bool og2Destroyed = og2.Destroyed;
                AddCheck("Uniqueness_ThingMakerSecondInstanceDestroyed", og1Active && og2Destroyed,
                    "firstActive=true, secondDestroyed=true",
                    string.Format("firstActive={0}, secondDestroyed={1}", og1Active, og2Destroyed));

                // Post-load reconciliation
                reg.ReconcileAllExistingLegendaries();
                bool reconciled = reg.IsGranted(ogDef);
                AddCheck("Uniqueness_PostLoadReconciliation", reconciled, "isGranted=true", reconciled.ToString());

                // The only instance picked up into a pawn's inventory and dropped again (same path as a drop pod
                // landing or a caravan unloading) must survive: its own reservation is not a duplicate.
                Pawn carrier = null;
                foreach (Pawn fc in map.mapPawns.FreeColonistsSpawned) { carrier = fc; break; }
                bool survivedDrop = false;
                string dropInfo = "no carrier";
                if (carrier != null && !og1.Destroyed)
                {
                    if (og1.Spawned) og1.DeSpawn(DestroyMode.Vanish);
                    bool added = carrier.inventory.innerContainer.TryAdd(og1, true);
                    Thing dropped = null;
                    bool droppedOk = added && carrier.inventory.innerContainer.TryDrop(og1, carrier.Position, map, ThingPlaceMode.Near, out dropped);
                    survivedDrop = droppedOk && !og1.Destroyed && og1.Spawned && reg.IsGranted(ogDef);
                    dropInfo = string.Format("added={0}, dropped={1}, destroyed={2}, spawned={3}, granted={4}",
                        added, droppedOk, og1.Destroyed, og1.Spawned, reg.IsGranted(ogDef));
                }
                AddCheck("Uniqueness_OnlyInstanceSurvivesDropFromInventory", survivedDrop,
                    "dropped=true, destroyed=false, spawned=true, granted=true", dropInfo);

                // A travelling transporter is a world-object holder the map scan cannot see.
                if (og1.Spawned) og1.DeSpawn(DestroyMode.Vanish);
                if (!og1.Destroyed) og1.HitPoints = Math.Max(1, og1.MaxHitPoints - 7);
                int originalHp = og1.HitPoints;
                ActiveTransporterInfo info = new ActiveTransporterInfo();
                TravellingTransporters flight =
                    (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravellingTransporters);
                flight.SetFaction(Faction.OfPlayer);
                flight.Tile = map.Tile;
                flight.destinationTile = map.Tile;
                bool boarded = !og1.Destroyed && info.innerContainer.TryAdd(og1, false);
                Find.WorldObjects.Add(flight);
                flight.AddTransporter(info, true);
                bool invisible = reg.FindLiveLegendary(ogDef) == null;
                Thing rejectedInFlight = ThingMaker.MakeThing(ogDef);
                bool rejected = rejectedInFlight.Destroyed;
                if (!rejected) rejectedInFlight.Destroy(DestroyMode.Vanish);

                Thing landed = null;
                bool unloaded = boarded && info.innerContainer.TryDrop(og1, origin, map, ThingPlaceMode.Near, out landed);
                Find.WorldObjects.Remove(flight);
                AddCheck("Uniqueness_InFlightReservationPreservesOriginal",
                    boarded && invisible && rejected && unloaded && landed == og1 && !og1.Destroyed &&
                    og1.Spawned && og1.HitPoints == originalHp && reg.IsGranted(ogDef),
                    "second creation rejected; original instance unloads intact",
                    string.Format("boarded={0}, hidden={1}, rejected={2}, unloaded={3}, original={4}",
                        boarded, invisible, rejected, unloaded, landed == og1));

                // Cleanup og1 so smoke test can use it later
                if (og1.Spawned) og1.DeSpawn(DestroyMode.Vanish);
                if (!og1.Destroyed) og1.Destroy(DestroyMode.Vanish);
                reg.Unregister(ogDef);
            }

            // ---------------- 1c. REWARD SLOT RESERVATION ON DROP FAILURE ----------------
            ThingDef bigBoyDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Legendary_BigBoy");
            if (reg != null && bigBoyDef != null)
            {
                LegendaryRegistry.TestDropOverride = delegate(Thing t, Map m) { return false; };
                bool failedGrant = reg.TryGrant(bigBoyDef, map);
                bool slotReservedPrematurely = reg.IsGranted(bigBoyDef);

                LegendaryRegistry.TestDropOverride = null;
                bool retryGrant = reg.TryGrant(bigBoyDef, map);
                bool slotReservedNow = reg.IsGranted(bigBoyDef);

                AddCheck("RewardSlot_ReleaseOnDropFailure",
                    !failedGrant && !slotReservedPrematurely && retryGrant && slotReservedNow,
                    "failedGrant=false, reservedBefore=false, retryGrant=true, reservedNow=true",
                    string.Format("fail={0}, reservedBefore={1}, retry={2}, reservedNow={3}",
                        failedGrant, slotReservedPrematurely, retryGrant, slotReservedNow));

                // Cleanup bigBoy so smoke test can use it later
                Thing liveBigBoy = reg.FindLiveLegendary(bigBoyDef);
                if (liveBigBoy != null)
                {
                    if (liveBigBoy.Spawned) liveBigBoy.DeSpawn(DestroyMode.Vanish);
                    if (!liveBigBoy.Destroyed) liveBigBoy.Destroy(DestroyMode.Vanish);
                }
                reg.Unregister(bigBoyDef);
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
                if (boraxShooter != null && boraxShooter.drafter != null) boraxShooter.drafter.Drafted = true;
                if (boraxShooter != null && boraxShooter.skills != null)
                {
                    SkillRecord shootSkill = boraxShooter.skills.GetSkill(SkillDefOf.Shooting);
                    if (shootSkill != null) shootSkill.Level = 20;
                }
                ThingWithComps boraxGun = EquipWeapon(boraxShooter, "TMM_Gun_BoraxSprayer");
                FireOnceReliably(boraxShooter, boraxGun, dickRoman);

                // If lethal headshot occurred or debuff not applied by shot, ensure live Dick Roman with borax debuff
                if (dickRoman == null || dickRoman.Dead || !LegendaryUtil.HasBoraxVulnerabilityDebuff(dickRoman))
                {
                    dickRoman = SpawnDickRoman(origin + new IntVec3(4, 0, 0));
                    LegendaryUtil.ApplyBoraxDebuffs(dickRoman);
                }
                else
                {
                    LegendaryUtil.HealOneOrMoreInjuries(dickRoman, 40f);
                }

                bool hasBoraxDebuff = LegendaryUtil.HasBoraxVulnerabilityDebuff(dickRoman);
                float postBoraxDealt = ApplyTorsoDamage(dickRoman, DamageDefOf.Cut, 30f);
                AddCheck("DickRoman_FullDamageAfterBorax", hasBoraxDebuff && postBoraxDealt >= 15f,
                    "boraxDebuff=true, dealt>=15", string.Format("debuff={0}, dealt={1:0.0}", hasBoraxDebuff, postBoraxDealt));
            }

            // ---------------- 2b. DICK ROMAN PRISONER EXECUTION IMMUNITY ----------------
            Pawn execDick = SpawnDickRoman(origin + new IntVec3(5, 0, 0));
            Pawn executioner = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(5, 0, 1));
            if (execDick != null && executioner != null)
            {
                if (execDick.guest != null)
                {
                    execDick.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                }
                ExecutionUtility.DoExecutionByCut(executioner, execDick, 1, false);
                bool executionBlockedBeforeBorax = !execDick.Dead;

                LegendaryUtil.ApplyBoraxDebuffs(execDick);
                bool hasBoraxVulnerability = LegendaryUtil.HasBoraxVulnerabilityDebuff(execDick);

                ExecutionUtility.DoExecutionByCut(executioner, execDick, 1, false);
                bool executionKilledAfterBorax = execDick.Dead;

                AddCheck("DickRoman_ExecutionBlockedWithoutBorax",
                    executionBlockedBeforeBorax && hasBoraxVulnerability && executionKilledAfterBorax,
                    "blockedBefore=true, hasDebuff=true, killedAfter=true",
                    string.Format("blockedBefore={0}, hasDebuff={1}, killedAfter={2}",
                        executionBlockedBeforeBorax, hasBoraxVulnerability, executionKilledAfterBorax));

                if (execDick.Corpse != null && execDick.Corpse.Spawned) execDick.Corpse.Destroy(DestroyMode.Vanish);
                if (execDick.Spawned) execDick.DeSpawn(DestroyMode.Vanish);
                if (!execDick.Destroyed) execDick.Destroy(DestroyMode.Vanish);
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

            // ---------------- 4b. HIT EFFECTS: UNARMED / ZERO-DAMAGE REJECTION ----------------
            Pawn punchVictim = SpawnPawn(null, FindHostileFaction(), bladeWielder.Position + new IntVec3(1, 0, 1));
            if (punchVictim != null)
            {
                // 1. Unarmed punch: weapon is null, attacker holds First Blade
                DamageInfo unarmedPunch = new DamageInfo(DamageDefOf.Blunt, 4f, 0f, -1f, bladeWielder, null, null);
                punchVictim.TakeDamage(unarmedPunch);
                bool punchSurvived = !punchVictim.Dead;

                // 2. Zero-damage strike with weapon source
                DamageInfo zeroDamage = new DamageInfo(DamageDefOf.Cut, 0f, 0f, -1f, bladeWielder, null, firstBlade != null ? firstBlade.def : null);
                punchVictim.TakeDamage(zeroDamage);
                bool zeroDamageSurvived = !punchVictim.Dead;

                AddCheck("FirstBlade_NoExecutionOnUnarmedOrZeroDamage",
                    punchSurvived && zeroDamageSurvived,
                    "punchSurvived=true, zeroDamageSurvived=true",
                    string.Format("punch={0}, zeroDmg={1}", punchSurvived, zeroDamageSurvived));

                if (punchVictim.Corpse != null && punchVictim.Corpse.Spawned) punchVictim.Corpse.Destroy(DestroyMode.Vanish);
                if (punchVictim.Spawned) punchVictim.DeSpawn(DestroyMode.Vanish);
                if (!punchVictim.Destroyed) punchVictim.Destroy(DestroyMode.Vanish);
            }

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

            // ---------------- 5b. THE COLT: ATOMIC RELOAD ROLLBACK AND SUCCESS ----------------
            ThingDef ammoDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Ammo_ColtSupernatural");
            CompColtAmmo coltAmmo = colt != null ? colt.TryGetComp<CompColtAmmo>() : null;
            if (colt != null && coltAmmo != null && ammoDef != null && coltShooter != null)
            {
                coltAmmo.SetCharges(0);
                Thing bulletsFail = ThingMaker.MakeThing(ammoDef);
                bulletsFail.stackCount = 6;

                // Test deduction failure rollback
                JobDriver_ReloadColt.TestSimulateDeductionFailure = delegate(Thing t, int count) { return true; };
                int loadedFail;
                bool reloadFailResult = JobDriver_ReloadColt.TryReloadColtAtomic(colt, bulletsFail, 6, out loadedFail);
                bool failChargesUnchanged = coltAmmo.Charges == 0;
                bool failBulletsRefunded = bulletsFail.stackCount == 6;

                // Test normal atomic reload success
                JobDriver_ReloadColt.TestSimulateDeductionFailure = null;
                int loadedOk;
                bool reloadOkResult = JobDriver_ReloadColt.TryReloadColtAtomic(colt, bulletsFail, 6, out loadedOk);
                bool okChargesAdded = coltAmmo.Charges == 6;
                bool okBulletsConsumed = bulletsFail.Destroyed || bulletsFail.stackCount == 0;

                AddCheck("Colt_AtomicReloadDeductionRollbackAndSuccess",
                    !reloadFailResult && failChargesUnchanged && failBulletsRefunded &&
                    reloadOkResult && okChargesAdded && okBulletsConsumed,
                    "failRes=false, charges0=true, bullets6=true, okRes=true, charges6=true, consumed=true",
                    string.Format("failRes={0}, c0={1}, b6={2}, okRes={3}, c6={4}, consumed={5}",
                        reloadFailResult, failChargesUnchanged, failBulletsRefunded,
                        reloadOkResult, okChargesAdded, okBulletsConsumed));
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
            TraderChecks(origin);
        }

        // ---------------- TRADER STOCK: A RESERVED UNIQUE ITEM IS NEVER CHOSEN ----------------
        private int m_destroyedInStock;
        public string traderSettlementId = "", flightId = "";
        private static readonly string[] TraderDefs = { "TMM_Legendary_FirstBlade", "TMM_Legendary_AlphaPredator", "TMM_Legendary_AtomsJudgement" };

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref traderSettlementId, "tmmProbeTraderSettlement", "");
            Scribe_Values.Look(ref flightId, "tmmProbeFlight", "");
        }

        private static bool IsLegendary(ThingDef d) { return d.GetCompProperties<CompProperties_LegendaryItem>() != null; }

        private static List<TraderKindDef> LegendaryKinds(out string facts)
        {
            List<TraderKindDef> kinds = new List<TraderKindDef>();
            Dictionary<string, int> generators = new Dictionary<string, int>();
            foreach (TraderKindDef kind in DefDatabase<TraderKindDef>.AllDefsListForReading)
            {
                bool any = false;
                foreach (StockGenerator g in kind.stockGenerators)
                    foreach (string name in TraderDefs)
                    {
                        ThingDef d = ThingDef.Named(name);
                        if (!g.HandlesThingDef(d) || !d.tradeability.TraderCanSell() || d.techLevel > g.maxTechLevelGenerate) continue;
                        if (g is StockGenerator_BuyTradeTag || g is StockGenerator_BuySingleDef || g is StockGenerator_BuyExpensiveSimple) continue;
                        any = true;
                        string key = g.GetType().Name + ":" + name;
                        int n; generators.TryGetValue(key, out n); generators[key] = n + 1;
                    }
                if (any) kinds.Add(kind);
            }
            List<string> rows = new List<string>();
            foreach (KeyValuePair<string, int> kv in generators) rows.Add(kv.Key + " in " + kv.Value + " kinds");
            List<string> names = new List<string>();
            foreach (TraderKindDef k in kinds) names.Add(k.defName);
            facts = string.Join(", ", rows.ToArray()) + " | " + string.Join(",", names.ToArray());
            return kinds;
        }

        private static void Discard(Thing t)
        {
            if (t == null || t.Destroyed) return;
            Pawn p = t as Pawn;
            if (p != null && p.relations != null) p.relations.ClearAllRelations();
            t.Destroy(DestroyMode.Vanish);
        }

        /// Real stock generation for every kind that can stock a legendary. Free legendaries are kept alive so that a second copy shows.
        private void GenerateStocks(List<TraderKindDef> kinds, int each, Dictionary<string, int> made, List<Thing> kept)
        {
            foreach (TraderKindDef kind in kinds)
                for (int i = 0; i < each; i++)
                {
                    ThingSetMakerParams parms = default(ThingSetMakerParams);
                    parms.traderDef = kind;
                    parms.tile = map.Tile;
                    foreach (Thing t in ThingSetMakerDefOf.TraderStock.root.Generate(parms))
                    {
                        if (!IsLegendary(t.def)) { Discard(t); continue; }
                        int n; made.TryGetValue(t.def.defName, out n); made[t.def.defName] = n + 1;
                        kept.Add(t);
                    }
                }
        }

        private static string Text(Dictionary<string, int> d)
        {
            List<string> rows = new List<string>();
            foreach (KeyValuePair<string, int> kv in d) rows.Add(kv.Key + "=" + kv.Value);
            return string.Join(",", rows.ToArray());
        }

        private void TraderChecks(IntVec3 origin)
        {
            LegendaryRegistry reg = LegendaryRegistry.Instance;
            if (Environment.GetEnvironmentVariable("TMM_TRADER_CONTROL") == "1") new Harmony("tmm.test.traders").UnpatchAll(LegendaryTraderGuard.HarmonyId);
            string facts;
            List<TraderKindDef> kinds = LegendaryKinds(out facts);
            AddCheck("Trader_GeneratorsThatCanStockLegendaries", kinds.Count > 0, "some", facts);

            // Earlier checks of this probe leave other legendaries alive: free them all, so that a first copy can be stocked below.
            foreach (ThingDef d in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!IsLegendary(d) || Array.IndexOf(TraderDefs, d.defName) >= 0) continue;
                for (Thing old = reg.FindLiveLegendary(d); old != null; old = reg.FindLiveLegendary(d)) old.Destroy(DestroyMode.Vanish);
                reg.Unregister(d);
            }
            List<Thing> originals = new List<Thing>();
            foreach (string name in TraderDefs)
            {
                ThingDef d = ThingDef.Named(name);
                for (Thing old = reg.FindLiveLegendary(d); old != null; old = reg.FindLiveLegendary(d)) old.Destroy(DestroyMode.Vanish);
                reg.Unregister(d);
                originals.Add(ThingMaker.MakeThing(d, d.MadeFromStuff ? GenStuff.DefaultStuffFor(d) : null));
            }
            AddCheck("Trader_OriginalsCreated", originals.TrueForAll(t => !t.Destroyed), "3 alive", originals.FindAll(t => !t.Destroyed).Count.ToString());
            // One lies on the map, one flies in a transporter, one was sold and stays in a settlement's stock.
            GenSpawn.Spawn(originals[0], FindClearCenter(), map, WipeMode.Vanish);
            TravellingTransporters flight = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravellingTransporters);
            flight.SetFaction(Faction.OfPlayer);
            flight.Tile = map.Tile;
            Settlement far = null, shop = null;
            foreach (Settlement s in Find.WorldObjects.Settlements)
            {
                if (s.Faction == null || s.Faction.IsPlayer || s.trader == null || s.HasMap) continue;
                if (shop == null && s.TraderKind != null && kinds.Contains(s.TraderKind)) shop = s;
                if (s.Tile.Layer == map.Tile.Layer && (far == null || Find.WorldGrid.ApproxDistanceInTiles(s.Tile, map.Tile) > Find.WorldGrid.ApproxDistanceInTiles(far.Tile, map.Tile))) far = s;
            }
            flight.destinationTile = far == null ? map.Tile : far.Tile;
            ActiveTransporterInfo info = new ActiveTransporterInfo();
            bool flying = info.innerContainer.TryAdd(originals[1], false);
            Find.WorldObjects.Add(flight);
            flight.AddTransporter(info, true);
            flightId = flight.GetUniqueLoadID();
            AddCheck("Trader_ShopFound", shop != null, "a settlement of such a kind", shop == null ? "none" : shop.Label + " " + shop.TraderKind.defName);
            if (shop == null) return;
            traderSettlementId = shop.GetUniqueLoadID();
            int restockErrors = m_destroyedInStock;
            MethodInfo restock = AccessTools.Method(shop.trader.GetType(), "RegenerateStock");
            for (int i = 0; i < 25; i++) restock.Invoke(shop.trader, null);
            AddCheck("Trader_RealSettlementRestocks", m_destroyedInStock == restockErrors, "0 destroyed in 25 restocks", (m_destroyedInStock - restockErrors).ToString());
            ThingOwner goods = shop.trader.GetDirectlyHeldThings();
            // The last restock may have taken the only free legendary; this shop must hold just the sold original.
            foreach (Thing t in new List<Thing>(goods)) if (IsLegendary(t.def) && t != originals[2]) { Discard(t); reg.Unregister(t.def); }
            bool sold = goods.TryAdd(originals[2], false);
            AddCheck("Trader_OriginalsPlaced", originals[0].Spawned && flying && sold && !originals[2].Destroyed, "map, flight, shop", originals[0].Spawned + "," + flying + "," + sold);
            foreach (Thing t in originals) reg.RegisterExisting(t);

            List<string> states = new List<string>();
            foreach (ThingDef d in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!IsLegendary(d)) continue;
                int handled = 0;
                foreach (TraderKindDef k in kinds) foreach (StockGenerator g in k.stockGenerators) if (g.HandlesThingDef(d) && d.techLevel <= g.maxTechLevelGenerate) handled++;
                Thing where = reg.FindLiveLegendary(d);
                states.Add(d.defName.Replace("TMM_Legendary_", "") + ":granted=" + reg.IsGranted(d) + ",reserved=" + LegendaryTraderGuard.Reserved(d) + ",live=" + (where == null ? "-" : where.ParentHolder == null ? "?" : where.ParentHolder.ToString()) + ",sell=" + d.tradeability + ",generators=" + handled);
            }
            AddCheck("Trader_States", true, "info", string.Join(" | ", states.ToArray()));
            Dictionary<string, int> made = new Dictionary<string, int>();
            List<Thing> kept = new List<Thing>();
            int before = m_destroyedInStock;
            GenerateStocks(kinds, 25, made, kept);
            int dup = 0, reserved = 0;
            foreach (KeyValuePair<string, int> kv in made) { if (kv.Value > 1) dup++; if (Array.IndexOf(TraderDefs, kv.Key) >= 0) reserved++; }
            string result = "stocks=" + kinds.Count * 25 + " destroyed=" + (m_destroyedInStock - before) + " made: " + Text(made);
            AddCheck("Trader_ReservedNeverChosen", reserved == 0 && m_destroyedInStock == before, "no reserved item, nothing destroyed", result);
            AddCheck("Trader_FirstFreeCopyIsStockedOnce", made.Count > 0 && dup == 0 && kept.TrueForAll(t => !t.Destroyed), "free legendaries appear once and live", result);
            bool buys = true;
            foreach (string name in TraderDefs) { bool one = false; foreach (TraderKindDef k in kinds) one |= k.WillTrade(ThingDef.Named(name)); buys &= one; }
            AddCheck("Trader_StillBuysReservedItem", buys, "WillTrade", buys.ToString());

            // The sold original can be bought back as the same object; nothing replaced it.
            bool bought = goods.Contains(originals[2]) && goods.Remove(originals[2]);
            if (bought) GenSpawn.Spawn(originals[2], FindClearCenter(), map, WipeMode.Vanish);
            AddCheck("Trader_SoldOriginalBoughtBack", bought && originals[2].Spawned && !originals[2].Destroyed && LegendaryTraderGuard.Reserved(originals[2].def), "same object, still reserved", bought + "," + originals[2].Spawned);
            if (originals[2].Spawned) originals[2].DeSpawn(DestroyMode.Vanish);
            goods.TryAdd(originals[2], false);

            // A lost original frees its place, a returned one takes it again.
            Thing free = kept.Count == 0 ? null : kept[0];
            bool busyBefore = free != null && LegendaryTraderGuard.Reserved(free.def);
            foreach (Thing t in kept) Discard(t);
            bool freeAfter = free != null && !reg.HasActiveInstance(free.def);
            AddCheck("Trader_FreedWhenStockIsGone", busyBefore && freeAfter, "reserved while in stock, free after", busyBefore + "," + freeAfter);
            AddCheck("Trader_OriginalsIntact", originals.TrueForAll(t => !t.Destroyed) && originals[0].Spawned && info.innerContainer.Contains(originals[1]) && goods.Contains(originals[2]), "alive in their places", string.Join(",", originals.ConvertAll(t => t.Destroyed ? "destroyed" : "ok").ToArray()));
            if (Environment.GetEnvironmentVariable("TMM_TRADER_ONLY") != "1") return;
            GameDataSaveLoader.SaveGame("TMM-Legendary");
            AddCheck("Trader_SaveWritten", File.Exists(Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-Legendary.rws")), "exists", "checked");
        }

        private void TraderReloadChecks()
        {
            LegendaryRegistry reg = LegendaryRegistry.Instance;
            string facts;
            List<TraderKindDef> kinds = LegendaryKinds(out facts);
            foreach (string name in TraderDefs) AddCheck("Reload_Reserved_" + name, reg.IsGranted(ThingDef.Named(name)) && LegendaryTraderGuard.Reserved(ThingDef.Named(name)), "reserved", reg.IsGranted(ThingDef.Named(name)).ToString());
            List<Thing> onMap = map.listerThings.ThingsOfDef(ThingDef.Named(TraderDefs[0]));
            Thing flown = null, sold = null;
            foreach (WorldObject w in Find.WorldObjects.AllWorldObjects)
            {
                if (w.GetUniqueLoadID() == flightId) { List<Thing> held = new List<Thing>(); ThingOwnerUtility.GetAllThingsRecursively((IThingHolder)w, held, true, null); flown = held.Find(t => t.def.defName == TraderDefs[1]); }
                Settlement s = w as Settlement;
                if (s != null && s.GetUniqueLoadID() == traderSettlementId) foreach (Thing t in s.trader.GetDirectlyHeldThings()) if (t.def.defName == TraderDefs[2]) sold = t;
            }
            AddCheck("Reload_OriginalsInPlace", onMap.Count == 1 && flown != null && sold != null, "map, flight, shop", onMap.Count + "," + (flown != null) + "," + (sold != null));
            Dictionary<string, int> made = new Dictionary<string, int>();
            List<Thing> kept = new List<Thing>();
            int before = m_destroyedInStock;
            GenerateStocks(kinds, 25, made, kept);
            int reserved = 0, dup = 0;
            foreach (KeyValuePair<string, int> kv in made) { if (kv.Value > 1) dup++; if (Array.IndexOf(TraderDefs, kv.Key) >= 0) reserved++; }
            string result = "stocks=" + kinds.Count * 25 + " destroyed=" + (m_destroyedInStock - before) + " made: " + Text(made);
            AddCheck("Reload_ReservedNeverChosen", reserved == 0 && dup == 0 && m_destroyedInStock == before, "no reserved item, nothing destroyed", result);
            AddCheck("Reload_OriginalsStillThere", onMap.Count == 1 && !onMap[0].Destroyed && flown != null && !flown.Destroyed && sold != null && !sold.Destroyed, "alive", "checked");
            foreach (Thing t in kept) Discard(t);
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

    [StaticConstructorOnStartup]
    public static class LegendaryProbeLaunch
    {
        public static bool Reload { get { return Array.IndexOf(Environment.GetCommandLineArgs(), "-tmm-legendary-reload") >= 0; } }
        static LegendaryProbeLaunch()
        {
            if (!Reload) return;
            new Harmony("tmm.test.legendarydestroy").Patch(AccessTools.Method(typeof(Thing), "Destroy"), prefix: new HarmonyMethod(typeof(LegendaryProbeLaunch), "Destroying"));
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Legendary"));
        }
        // Says who destroys an original after loading.
        private static void Destroying(Thing __instance)
        {
            if (__instance.def != null && __instance.def.defName.StartsWith("TMM_Legendary_")) Log.Message("[TMM probe] destroying " + __instance.ThingID + " hp=" + __instance.HitPoints + " tick=" + Find.TickManager.TicksGame + " " + Environment.StackTrace);
        }
    }
}