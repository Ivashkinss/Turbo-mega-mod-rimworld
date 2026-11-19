using System;
using System.Collections.Generic;
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

        public override void MapComponentTick()
        {
            if (s_finishedAll) return;
            if (!EquipmentProbeBootstrap.FreshMode && !EquipmentProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            if (!s_logicDone)
            {
                m_ticksWaited++;
                if (m_ticksWaited < 30) return;

                s_logicDone = true;
                Application.logMessageReceived += OnUnityLog;

                try
                {
                    if (EquipmentProbeBootstrap.FreshMode)
                    {
                        RunFreshLogic();
                        m_waitingForRepaint = true;
                    }
                    else if (EquipmentProbeBootstrap.ReloadMode)
                    {
                        RunReloadLogic();
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
                WriteReport(reportFile);
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

        private void RunFreshLogic()
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
            bool installJobDone = RunPawnJobToCompletion(hero, 4000);

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

            FireWeaponOnceAt(hero, rifle, demon);
            FireWeaponOnceAt(hero, rifle, human);

            float demonDmg = SumInjuries(demon);
            float humanDmg = SumInjuries(human);
            int chargesAfter2Shots = (rifleMods != null && rifleMods.InstalledMods.Count > 0) ? rifleMods.InstalledMods[0].charges : -1;

            bool demonTookMore = demon != null && human != null && (demon.Dead || demonDmg > humanDmg + 4f);
            AddCheck("WeaponMod_HolyWaterHurtsDemonMoreThanHuman", demonTookMore,
                "demonDmg > humanDmg + 4",
                string.Format("demon={0:0.0}(dead={1}), human={2:0.0}", demonDmg, demon != null && demon.Dead, humanDmg));
            AddCheck("WeaponMod_ChargesDecreased", chargesAfter2Shots == 28, "28", chargesAfter2Shots.ToString());

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
            bool removeJobDone = RunPawnJobToCompletion(hero, 4000);

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
            bool weaveJobDone = RunPawnJobToCompletion(hero, 4000);

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
        }

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
                                      rifleMods.InstalledMods[0].charges == 28;
            AddCheck("Reload_WeaponModAndChargesPersisted", weaponModPersisted,
                "HolyWaterTank[28]",
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
                pawn.Tick(); // CHECK API
            }
            return pawn.CurJobDef != startedDef;
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
