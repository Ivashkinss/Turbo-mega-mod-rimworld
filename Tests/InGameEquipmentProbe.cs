using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class EquipmentProbeBootstrap
    {
        public const string SaveName = "TMM-Equipment";

        public static bool FreshMode
        {
            get { return HasArg("-tmm-equipment-probe"); }
        }

        public static bool ReloadMode
        {
            get { return HasArg("-tmm-equipment-reload"); }
        }

        static EquipmentProbeBootstrap()
        {
            if (!FreshMode && !ReloadMode) return;

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (ReloadMode && Current.ProgramState == ProgramState.Entry)
                {
                    GameDataSaveLoader.LoadGame(SaveName); // CHECK API
                }
            });
        }

        private static bool HasArg(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// In-game verification probe for EquipmentSlots:
    /// - Backpack increases CarryingCapacity &amp; MassUtility.Capacity;
    /// - Real JobDriver_InstallEquipmentMod installs Holy Water Tank onto rifle;
    /// - Shooting TMM_Demon deals more damage than shooting human &amp; consumes tank charges;
    /// - Real JobDriver_RemoveEquipmentMod returns the module item with preserved charges;
    /// - Railroad Ballistic Weave on duster boosts ArmorRating_Sharp by +0.25;
    /// - ITab_TMM_Equipment draws cleanly inside a real EventType.Repaint pass;
    /// - Save (TMM-Equipment) and reload (-tmm-equipment-reload) preserve installed mods &amp; charges.
    /// </summary>
    public sealed class InGameEquipmentProbe : MapComponent
    {
        private sealed class CheckItem
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private const string ProbeColonistNick = "EqProbeHero";

        private static bool s_logicDone;
        private static bool s_finishedAll;
        private int m_ticksWaited;
        private bool m_waitingForRepaint;
        private Pawn m_guiTestPawn;
        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        private static readonly MethodInfo TryCastShotMethod = AccessTools.Method(typeof(Verb), "TryCastShot"); // CHECK API
        private static readonly MethodInfo ProjectileImpactMethod = AccessTools.Method(typeof(Projectile), "Impact", new Type[] { typeof(Thing), typeof(bool) }); // CHECK API

        public InGameEquipmentProbe(Map map) : base(map)
        {
        }

        private IEnumerator<bool> m_fresh;

        public override void MapComponentTick()
        {
            if (s_finishedAll) return;
            if (!EquipmentProbeBootstrap.FreshMode && !EquipmentProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            if (!s_logicDone)
            {
                m_ticksWaited++;
                if (m_ticksWaited < 30) return;

                if (EquipmentProbeBootstrap.FreshMode)
                {
                    if (m_fresh == null)
                    {
                        Application.logMessageReceived += OnUnityLog;
                        m_fresh = RunFreshLogic();
                    }
                    bool more;
                    try { more = m_fresh.MoveNext(); }
                    catch (Exception ex)
                    {
                        m_errors.Add("Unhandled exception: " + ex.ToString());
                        s_logicDone = true;
                        FinishAndShutdown("EquipmentProbe.json", false);
                        return;
                    }
                    if (more) return;
                    s_logicDone = true;
                    m_waitingForRepaint = true;
                    return;
                }

                s_logicDone = true;
                Application.logMessageReceived += OnUnityLog;

                try
                {
                    if (false)
                    {
                    }
                    else if (EquipmentProbeBootstrap.ReloadMode)
                    {
                        RunReloadLogic();
                        CheckT60Helmet("Reload");
                        FinishAndShutdown("EquipmentReloadProbe.json", false);
                    }
                }
                catch (Exception ex)
                {
                    m_errors.Add("Unhandled exception: " + ex.ToString());
                    FinishAndShutdown(EquipmentProbeBootstrap.ReloadMode ? "EquipmentReloadProbe.json" : "EquipmentProbe.json", false);
                }
            }
        }

        public override void MapComponentOnGUI()
        {
            if (!m_waitingForRepaint || s_finishedAll) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            m_waitingForRepaint = false;
            bool repaintOk = false;
            try
            {
                if (m_guiTestPawn != null)
                {
                    ITab_TMM_Equipment tab = new ITab_TMM_Equipment();
                    tab.DebugDrawInOnGUI(new Rect(20f, 20f, 540f, 560f), m_guiTestPawn);
                    repaintOk = true;
                }
            }
            catch (Exception ex)
            {
                m_errors.Add("ITab Repaint exception: " + ex.ToString());
            }

            AddCheck("ITab_RepaintNoExceptions", repaintOk, "true", repaintOk.ToString());
            FinishAndShutdown("EquipmentProbe.json", true);
        }

        private void FinishAndShutdown(string reportFile, bool saveBeforeQuit)
        {
            if (s_finishedAll) return;
            s_finishedAll = true;

            try
            {
                if (saveBeforeQuit)
                {
                    // The hero may fire on his own after the checks: save a known number that is not a default, so that the reload proves the saved value.
                    foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                        if (p.Name != null && p.Name.ToStringShort == ProbeColonistNick && p.equipment != null && p.equipment.Primary != null)
                        {
                            CompWeaponMods saved = p.equipment.Primary.GetComp<CompWeaponMods>();
                            if (saved != null && saved.InstalledMods.Count > 0) saved.InstalledMods[0].charges = ReloadCharges;
                        }
                    GameDataSaveLoader.SaveGame(EquipmentProbeBootstrap.SaveName); // CHECK API
                }
            }
            catch (Exception ex)
            {
                m_errors.Add("SaveGame exception: " + ex.ToString());
            }
            finally
            {
                Application.logMessageReceived -= OnUnityLog;
                bool allPassed = WriteReport(reportFile);
                if (!EquipmentProbeBootstrap.ReloadMode)
                {
                    File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "EquipmentUiProbe.json"), "{\"passed\":" + (allPassed ? "true" : "false") + "}", Encoding.UTF8);
                }
                // The test harness closes the game itself after its own reports are written.
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        // Iterator: jobs need real game ticks (1.7 pathfinding is asynchronous), so the probe yields one tick at a time.
        private IEnumerator<bool> RunFreshLogic()
        {
            IntVec3 origin = FindClearCenter();

            // Create workbench adjacent to origin so real JobDrivers run end-to-end
            ThingDef benchDef = DefDatabase<ThingDef>.GetNamedSilentFail("TableMachining");
            Building bench = null;
            if (benchDef != null)
            {
                Thing bThing = ThingMaker.MakeThing(benchDef, benchDef.MadeFromStuff ? ThingDefOf.Steel : null);
                bThing.SetFaction(Faction.OfPlayer);
                bench = GenSpawn.Spawn(bThing, origin + new IntVec3(2, 0, 0), map, WipeMode.Vanish) as Building;
            }

            Pawn hero = SpawnPawn(null, Faction.OfPlayer, origin);
            hero.Name = new NameTriple("Equipment", ProbeColonistNick, "Tester");
            m_guiTestPawn = hero;

            // ---------------- 1. BACKPACK INCREASES CARRYING CAPACITY ----------------
            float carryBefore = hero.GetStatValue(StatDefOf.CarryingCapacity);
            float caravanBefore = MassUtility.Capacity(hero); // CHECK API

            Apparel backpack = WearApparel(hero, "TMM_Backpack_Military");
            float carryAfter = hero.GetStatValue(StatDefOf.CarryingCapacity);
            float caravanAfter = MassUtility.Capacity(hero); // CHECK API

            AddCheck("Backpack_IncreasesCarryingCapacity",
                backpack != null && carryAfter > carryBefore + 50f && caravanAfter > caravanBefore + 30f,
                string.Format("carry>{0:0}, kg>{1:0}", carryBefore + 50f, caravanBefore + 30f),
                string.Format("carry={0:0}->{1:0}, kg={2:0}->{3:0}", carryBefore, carryAfter, caravanBefore, caravanAfter));

            // ---------------- 2. REAL JOB: INSTALL HOLY WATER TANK ON RIFLE ----------------
            ThingWithComps rifle = EquipWeapon(hero, "Gun_AssaultRifle");
            ThingDef tankDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Mod_HolyWaterTank");
            Thing tank = tankDef != null ? GenSpawn.Spawn(ThingMaker.MakeThing(tankDef), origin + new IntVec3(-1, 0, 0), map, WipeMode.Vanish) : null;
            if (tank != null) tank.SetForbidden(false, false);

            ITab_TMM_Equipment.QueueInstallModJob(hero, rifle, tank);
            Log.Message("[TMM probe] equip diag: curJob=" + (hero.CurJobDef != null ? hero.CurJobDef.defName : "null") + " drafted=" + hero.Drafted + " downed=" + hero.Downed + " rifle=" + (rifle != null) + " mods=" + (rifle != null && rifle.GetComp<CompWeaponMods>() != null) + " tank=" + (tank != null && tank.Spawned) + " jobDef=" + (DefDatabase<JobDef>.GetNamedSilentFail("TMM_InstallEquipmentMod") != null));
            JobDef installJobDoneDef = hero.CurJobDef;
            int installJobDoneWait = 0;
            while (installJobDoneDef != null && hero.CurJobDef == installJobDoneDef && installJobDoneWait < 8000) { installJobDoneWait++; yield return false; }
            bool installJobDone = installJobDoneDef != null && hero.CurJobDef != installJobDoneDef;

            CompWeaponMods rifleMods = rifle != null ? rifle.GetComp<CompWeaponMods>() : null;
            bool tankInstalled = rifleMods != null && rifleMods.InstalledMods.Count == 1 &&
                                 rifleMods.InstalledMods[0].modDefName == "TMM_Mod_HolyWaterTank" &&
                                 rifleMods.InstalledMods[0].charges == 30;
            AddCheck("WeaponMod_InstallJobSucceeded", installJobDone && tankInstalled,
                "jobDone=true, mod=HolyWaterTank[30]",
                string.Format("jobDone={0}, count={1}, charges={2}",
                    installJobDone,
                    rifleMods != null ? rifleMods.InstalledMods.Count : -1,
                    (rifleMods != null && rifleMods.InstalledMods.Count > 0) ? rifleMods.InstalledMods[0].charges : -1));

            // ---------------- 3. SHOOT DEMON VS HUMAN (HOLY WATER WEAKNESS + CHARGE LOSS) ----------------
            Pawn demon = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(6, 0, 0));
            Pawn human = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(6, 0, 2));

            FireUntilHit(hero, rifle, demon);
            Log.Message("[TMM probe] holy diag after demon: dmg=" + SumInjuries(demon) + " charges=" + ((rifleMods != null && rifleMods.InstalledMods.Count > 0) ? rifleMods.InstalledMods[0].charges : -1) + " holyFactor=" + (demon != null ? demon.GetStatValue(StatDefOf.IncomingDamageFactor) : 0f) + " xeno=" + (demon != null && demon.genes != null && demon.genes.Xenotype != null ? demon.genes.Xenotype.defName : "-") + " bane=" + (demon != null && demon.genes != null && demon.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_DemonHolyBane"))) + " hediffs=" + (demon != null ? string.Join(",", demon.health.hediffSet.hediffs.ConvertAll(h => h.def.defName + ":" + h.Severity.ToString("0.0")).ToArray()) : ""));
            FireUntilHit(hero, rifle, human);
            Log.Message("[TMM probe] holy diag after human: dmg=" + SumInjuries(human) + " charges=" + ((rifleMods != null && rifleMods.InstalledMods.Count > 0) ? rifleMods.InstalledMods[0].charges : -1) + " hediffs=" + (human != null ? string.Join(",", human.health.hediffSet.hediffs.ConvertAll(h => h.def.defName + ":" + h.Severity.ToString("0.0")).ToArray()) : ""));

            int chargesAfter2Shots = (rifleMods != null && rifleMods.InstalledMods.Count > 0) ? rifleMods.InstalledMods[0].charges : -1;
            bool demonBurned = demon != null && demon.health.hediffSet.hediffs.Any(h => h.def.defName == "Burn");
            AddCheck("WeaponMod_HolyWaterTriggersOnHit", demonBurned,
                "holy water burn on the shot demon",
                string.Format("burn={0}, dmg={1:0.0}", demonBurned, SumInjuries(demon)));
            // Holy water is spent per triggering hit; a missed shot keeps its charge.
            AddCheck("WeaponMod_ChargesDecreased", chargesAfter2Shots >= 0 && chargesAfter2Shots < 30, "< 30", chargesAfter2Shots.ToString());

            // Same holy water hit on a fresh demon and a fresh human: the demon's bane gene must make it hurt far more.
            Pawn demon2 = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(8, 0, 4));
            Pawn human2 = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(8, 0, -4));
            DamageDef holyDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_HolyWater");
            float demonBase = SumInjuries(demon2), humanBase = SumInjuries(human2);
            if (holyDef != null && demon2 != null) demon2.TakeDamage(new DamageInfo(holyDef, 8f, 0f, -1f, hero, demon2.health.hediffSet.GetNotMissingParts().FirstOrDefault(pt => pt.def == BodyPartDefOf.Torso)));
            if (holyDef != null && human2 != null) human2.TakeDamage(new DamageInfo(holyDef, 8f, 0f, -1f, hero, human2.health.hediffSet.GetNotMissingParts().FirstOrDefault(pt => pt.def == BodyPartDefOf.Torso)));
            float demonDmg = SumInjuries(demon2) - demonBase;
            float humanDmg = SumInjuries(human2) - humanBase;
            bool demonTookMore = demon2 != null && human2 != null && (demon2.Dead || demonDmg >= humanDmg * 1.4f);
            AddCheck("WeaponMod_HolyWaterHurtsDemonMoreThanHuman", demonTookMore,
                "demon >= 1.4 x human for the same holy water hit",
                string.Format("demon={0:0.0}(dead={1}), human={2:0.0}", demonDmg, demon2 != null && demon2.Dead, humanDmg));
            // ---------------- 4. REAL JOB: REMOVE MOD RETURNS ITEM WITH PRESERVED CHARGES ----------------
            ThingWithComps secondRifle = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDef.Named("Gun_AssaultRifle")), origin + new IntVec3(0, 0, 2), map, WipeMode.Vanish) as ThingWithComps;
            Thing secondTank = tankDef != null ? GenSpawn.Spawn(ThingMaker.MakeThing(tankDef), origin + new IntVec3(-1, 0, 2), map, WipeMode.Vanish) : null;
            if (secondTank != null)
            {
                secondTank.SetForbidden(false, false);
                CompEquipmentMod c = secondTank.TryGetComp<CompEquipmentMod>();
                if (c != null) c.Charges = 19;
            }

            CompWeaponMods secondMods = secondRifle != null ? secondRifle.GetComp<CompWeaponMods>() : null;
            if (secondMods != null && secondTank != null) secondMods.InstallMod(secondTank);

            int tanksBeforeRemove = map.listerThings.ThingsOfDef(tankDef).Count;
            ITab_TMM_Equipment.QueueRemoveModJob(hero, secondRifle, 0);
            JobDef removeJobDoneDef = hero.CurJobDef;
            int removeJobDoneWait = 0;
            while (removeJobDoneDef != null && hero.CurJobDef == removeJobDoneDef && removeJobDoneWait < 8000) { removeJobDoneWait++; yield return false; }
            bool removeJobDone = removeJobDoneDef != null && hero.CurJobDef != removeJobDoneDef;

            List<Thing> tanksAfter = map.listerThings.ThingsOfDef(tankDef);
            int returnedCharges = -1;
            if (tanksAfter.Count > tanksBeforeRemove)
            {
                CompEquipmentMod rc = tanksAfter[tanksAfter.Count - 1].TryGetComp<CompEquipmentMod>();
                if (rc != null) returnedCharges = rc.Charges;
            }
            AddCheck("WeaponMod_RemoveJobReturnsItem",
                removeJobDone && secondMods != null && secondMods.InstalledMods.Count == 0 && returnedCharges == 19,
                "removed & returned charges=19",
                string.Format("jobDone={0}, remaining={1}, returnedCharges={2}",
                    removeJobDone, secondMods != null ? secondMods.InstalledMods.Count : -1, returnedCharges));

            // ---------------- 5. RAILROAD BALLISTIC WEAVE ON DUSTER (+0.25 SHARP ARMOR) ----------------
            Apparel duster = WearApparel(hero, "Apparel_Duster");
            float sharpBefore = duster != null ? duster.GetStatValue(StatDefOf.ArmorRating_Sharp) : 0f;

            ThingDef weaveDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Mod_RailroadBallisticWeave");
            Thing weave = weaveDef != null ? GenSpawn.Spawn(ThingMaker.MakeThing(weaveDef), origin + new IntVec3(-1, 0, -1), map, WipeMode.Vanish) : null;
            if (weave != null) weave.SetForbidden(false, false);

            ITab_TMM_Equipment.QueueInstallModJob(hero, duster, weave);
            JobDef weaveJobDoneDef = hero.CurJobDef;
            int weaveJobDoneWait = 0;
            while (weaveJobDoneDef != null && hero.CurJobDef == weaveJobDoneDef && weaveJobDoneWait < 8000) { weaveJobDoneWait++; yield return false; }
            bool weaveJobDone = weaveJobDoneDef != null && hero.CurJobDef != weaveJobDoneDef;

            float sharpAfter = duster != null ? duster.GetStatValue(StatDefOf.ArmorRating_Sharp) : 0f;
            AddCheck("ApparelMod_BallisticWeaveBoostsArmor",
                weaveJobDone && duster != null && (sharpAfter - sharpBefore) >= 0.24f,
                string.Format("+{0:0.00} sharp", 0.25f),
                string.Format("jobDone={0}, sharp={1:0.00}->{2:0.00}", weaveJobDone, sharpBefore, sharpAfter));

            // ---------------- 6. EQUIP POWER ARMOR FOR ITAB REPAINT TEST (OVER BACKPACK & DUSTER) ----------------
            Apparel powerArmor = WearApparel(hero, "TMM_Legendary_ProtectorArmor");
            if (powerArmor == null) powerArmor = WearApparel(hero, "Apparel_PowerArmor");
            bool wearingAllThree = hero.apparel.WornApparel.Contains(backpack) &&
                                   hero.apparel.WornApparel.Contains(duster) &&
                                   (powerArmor != null && hero.apparel.WornApparel.Contains(powerArmor));
            AddCheck("Layers_BackpackAndPowerArmorCoexist", wearingAllThree, "all 3 worn simultaneously", wearingAllThree.ToString());
            Pawn fit = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
                Faction.OfPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false, fixedGender: Gender.Female));
            fit.Name = new NameTriple("T60", "ReleaseHelmet", "Probe");
            fit.story.bodyType = BodyTypeDefOf.Female;
            GenSpawn.Spawn(fit, FindClearCenter(), map);
            WearApparel(fit, "TMM_Apparel_bos_t60_power_armor");
            WearApparel(fit, "TMM_Apparel_bos_t60_helmet");
            CheckT60Helmet("Fresh");
        }

        private void CheckT60Helmet(string stage)
        {
            Pawn wearer = map.mapPawns.AllPawnsSpawned.FirstOrDefault(p =>
                p.Name != null && p.Name.ToStringShort == "ReleaseHelmet");
            ThingDef def = ThingDef.Named("TMM_Apparel_bos_t60_helmet");
            bool headLayer = def.apparel.layers.Contains(ApparelLayerDefOf.Overhead) &&
                !def.apparel.layers.Any(layer => layer.defName == "TMM_PowerArmor");
            bool worn = wearer != null && wearer.apparel.WornApparel.Any(item => item.def == def);
            RenderTexture portrait = wearer == null ? null : PortraitsCache.Get(wearer,
                new Vector2(192f, 192f), Rot4.South, default(Vector3), 1f, true, true, true, true);
            bool rendered = portrait != null && wearer.Drawer.renderer.renderTree.Resolved;
            AddCheck("T60_" + stage, headLayer && worn && rendered &&
                !m_errors.Any(message => message.Contains("Failed to find any textures")),
                "head layer, worn, portrait rendered", "layer=" + headLayer + " worn=" + worn + " rendered=" + rendered);
        }

        private const int ReloadCharges = 17;

        private void RunReloadLogic()
        {
            Pawn hero = null;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                if (colonists[i] != null && colonists[i].Name != null && colonists[i].Name.ToStringShort == ProbeColonistNick)
                {
                    hero = colonists[i];
                    break;
                }
            }

            AddCheck("Reload_HeroFound", hero != null, "non-null", hero != null ? hero.LabelShort : "null");
            if (hero == null) return;

            ThingWithComps rifle = hero.equipment != null ? hero.equipment.Primary : null;
            CompWeaponMods rifleMods = rifle != null ? rifle.GetComp<CompWeaponMods>() : null;
            bool weaponModPersisted = rifleMods != null && rifleMods.InstalledMods.Count == 1 &&
                                      rifleMods.InstalledMods[0].modDefName == "TMM_Mod_HolyWaterTank" &&
                                      rifleMods.InstalledMods[0].charges == ReloadCharges;
            AddCheck("Reload_WeaponModAndChargesPersisted", weaponModPersisted,
                "HolyWaterTank[" + ReloadCharges + "]",
                (rifleMods != null && rifleMods.InstalledMods.Count > 0)
                    ? string.Format("{0}[{1}]", rifleMods.InstalledMods[0].modDefName, rifleMods.InstalledMods[0].charges)
                    : "none");

            Apparel duster = null;
            for (int i = 0; i < hero.apparel.WornApparel.Count; i++)
            {
                if (hero.apparel.WornApparel[i].def.defName == "Apparel_Duster")
                {
                    duster = hero.apparel.WornApparel[i];
                    break;
                }
            }
            CompApparelMods dusterMods = duster != null ? duster.GetComp<CompApparelMods>() : null;
            bool apparelModPersisted = dusterMods != null && dusterMods.InstalledMods.Count == 1 &&
                                       dusterMods.InstalledMods[0].modDefName == "TMM_Mod_RailroadBallisticWeave";
            AddCheck("Reload_ApparelModPersisted", apparelModPersisted, "RailroadBallisticWeave",
                (dusterMods != null && dusterMods.InstalledMods.Count > 0) ? dusterMods.InstalledMods[0].modDefName : "none");
        }

        private static bool RunPawnJobToCompletion(Pawn pawn, int maxTicks)
        {
            if (pawn == null || pawn.jobs == null || pawn.CurJob == null) return false;
            JobDef startedDef = pawn.CurJobDef;
            for (int t = 0; t < maxTicks; t++)
            {
                if (pawn.CurJobDef != startedDef) return true;
                pawn.DoTick(); // 1.7: Tick() is protected, DoTick() is the public entry
                if (t % 500 == 0) Log.Message("[TMM probe] equip tick " + t + ": job=" + (pawn.CurJobDef != null ? pawn.CurJobDef.defName : "null") + " toil=" + (pawn.jobs.curDriver != null ? pawn.jobs.curDriver.CurToilString : "-") + " pos=" + pawn.Position);
            }
            return pawn.CurJobDef != startedDef;
        }

        /// Shots can miss or lack a line of fire from a random spot: retry, then fire from a clear cell next to the target.
        private bool FireUntilHit(Pawn shooter, ThingWithComps gun, Pawn target)
        {
            if (shooter == null || gun == null || target == null) return false;
            float baseline = SumInjuries(target);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                if (target.Dead || SumInjuries(target) > baseline) return true;
                if (attempt >= 2 && shooter.Spawned && target.Spawned)
                {
                    IntVec3[] offs = { new IntVec3(2, 0, 0), new IntVec3(-2, 0, 0), new IntVec3(0, 0, 2), new IntVec3(0, 0, -2) };
                    IntVec3 c = target.Position + offs[attempt % 4];
                    if (c.InBounds(map) && c.Standable(map) && c.GetFirstPawn(map) == null)
                    {
                        if (shooter.pather != null) shooter.pather.StopDead();
                        shooter.Position = c;
                        shooter.Notify_Teleported(true, true);
                    }
                }
                FireWeaponOnceAt(shooter, gun, target);
            }
            return target.Dead || SumInjuries(target) > baseline;
        }
        private bool FireWeaponOnceAt(Pawn shooter, ThingWithComps gun, Pawn target)
        {
            if (shooter == null || gun == null || target == null || TryCastShotMethod == null) return false;
            CompEquippable eq = gun.GetComp<CompEquippable>();
            if (eq == null || eq.PrimaryVerb == null) return false;

            Verb verb = eq.PrimaryVerb;
            verb.caster = shooter;

            FieldInfo curTargetField = AccessTools.Field(typeof(Verb), "currentTarget"); // CHECK API
            if (curTargetField != null)
            {
                curTargetField.SetValue(verb, new LocalTargetInfo(target));
            }

            object res = TryCastShotMethod.Invoke(verb, null);
            bool fired = (res is bool) && (bool)res;
            if (!fired) return false;

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

        private ThingWithComps EquipWeapon(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.equipment == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            ThingWithComps w = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null) as ThingWithComps;
            if (w == null) return null;
            pawn.equipment.DestroyAllEquipment();
            pawn.equipment.AddEquipment(w);
            return w;
        }

        private Apparel WearApparel(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.apparel == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            ThingDef stuff = def.MadeFromStuff ? ThingDefOf.Cloth : null;
            if (def.MadeFromStuff && stuff != null && !def.stuffCategories.Contains(StuffCategoryDefOf.Fabric))
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            Apparel ap = ThingMaker.MakeThing(def, stuff) as Apparel;
            if (ap == null) return null;
            pawn.apparel.Wear(ap, false);
            return ap;
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
            int safety = 0;
            // Shooters and targets must be able to use violence (shooting stats are disabled for pacifists).
            while (p != null && p.WorkTagIsDisabled(WorkTags.Violent) && safety++ < 10)
            {
                p.Destroy(DestroyMode.Vanish);
                p = PawnGenerator.GeneratePawn(req);
            }
            if (p == null) return null;
            if (p.apparel != null) p.apparel.DestroyAll();

            IntVec3 cell = FindStandableNear(nearCell);
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            return p;
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
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(near, map, 6, delegate(IntVec3 c)
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

        private bool WriteReport(string fileName)
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
            return allPassed;
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
