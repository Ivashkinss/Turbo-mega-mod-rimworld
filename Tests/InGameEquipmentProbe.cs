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
            if (FreshMode) InGameEquipmentProbe.TraceLorenzoOrder();

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

        private sealed class LorenzoObservation
        {
            public int damageTick = -1;
            public int pushTick = -1;
            public IntVec3 pushFrom;
            public IntVec3 pushTo;
        }

        private static Dictionary<Pawn, LorenzoObservation> lorenzoTargets;

        internal static void TraceLorenzoOrder()
        {
            var harmony = new Harmony("turbomegamod.equipment.probe.lorenzo");
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Pawn), "PostApplyDamage"),
                postfix: new HarmonyMethod(typeof(InGameEquipmentProbe), "RecordWaveDamage"));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Projectile_LorenzoWave), "PushAndStun"),
                prefix: new HarmonyMethod(typeof(InGameEquipmentProbe), "RememberPushStart"),
                postfix: new HarmonyMethod(typeof(InGameEquipmentProbe), "RecordWavePush"));
        }

        private static void RecordWaveDamage(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            LorenzoObservation obs;
            if (lorenzoTargets == null || !lorenzoTargets.TryGetValue(__instance, out obs)
                || obs.damageTick >= 0 || totalDamageDealt <= 0f || dinfo.Def == null
                || dinfo.Def.defName != "TMM_Damage_TelekineticWave") return;
            obs.damageTick = Find.TickManager.TicksGame;
        }

        private static void RememberPushStart(Pawn pawn, out IntVec3 __state)
        {
            __state = lorenzoTargets != null && pawn != null && lorenzoTargets.ContainsKey(pawn)
                ? pawn.Position : IntVec3.Invalid;
        }

        private static void RecordWavePush(Pawn pawn, IntVec3 __state)
        {
            if (!__state.IsValid || pawn.Position == __state) return;
            LorenzoObservation obs = lorenzoTargets[pawn];
            if (obs.pushTick >= 0) return;
            obs.pushTick = Find.TickManager.TicksGame;
            obs.pushFrom = __state;
            obs.pushTo = pawn.Position;
        }

        private const string ProbeColonistNick = "EqProbeHero";

        private static bool s_logicDone;
        private static bool s_finishedAll;
        private int m_ticksWaited;
        private bool m_waitingForRepaint;
        private int m_reloadStartFrame = -1;
        private bool m_reloadChecked, m_flightLoadChecked, m_flightLandChecked;
        private int m_flightStartTick;
        private int m_editorStartFrame = -1;
        private Pawn m_guiTestPawn;
        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        private static readonly MethodInfo TryCastShotMethod = AccessTools.Method(typeof(Verb), "TryCastShot"); // CHECK API
        private static readonly MethodInfo ProjectileImpactMethod = AccessTools.Method(typeof(Projectile), "Impact", new Type[] { typeof(Thing), typeof(bool) }); // CHECK API

        public InGameEquipmentProbe(Map map) : base(map)
        {
        }

        private IEnumerator<bool> m_fresh;

        /// A window that pauses the game (a letter with answers, a quest offer) stopped the ticks this probe runs on until the
        /// harness timed out (h-equipment-1, 075651); other probes answer such windows, this one did not.
        public override void MapComponentUpdate()
        {
            if (s_finishedAll || map != Find.AnyPlayerHomeMap) return;
            if (!EquipmentProbeBootstrap.FreshMode && !EquipmentProbeBootstrap.ReloadMode) return;
            if (!Find.TickManager.Paused) return;
            foreach (Window w in Find.WindowStack.Windows.Where(x => x.forcePause && !x.GetType().FullName.StartsWith("CharacterEditor", StringComparison.Ordinal)).ToList())
                Log.Message("[TMM probe] equipment probe: pausing window " + ProbeWindows.Dismiss(w));
            if (Find.TickManager.Paused) Find.TickManager.TogglePaused();
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

                if (m_reloadStartFrame >= 0)
                {
                    if (!m_flightLoadChecked)
                    {
                        m_flightLoadChecked = true;
                        AddCheck("LegendaryFlight_LoadedInFlight", m_flight != null && !m_flight.Destroyed && !m_flight.Spawned && m_flight.holdingOwner != null, "in the transporter", m_flight == null ? "null" : "spawned=" + m_flight.Spawned);
                        if (m_flight != null) SecondIssueRefused("Reload");
                        m_flightStartTick = Find.TickManager.TicksGame;
                    }
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                    if (m_flight != null && !m_flight.Spawned && Find.TickManager.TicksGame < m_flightStartTick + 40000) return;
                    if (!m_flightLandChecked)
                    {
                        m_flightLandChecked = true;
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                        CompWeaponMods mods = m_flight == null ? null : m_flight.GetComp<CompWeaponMods>();
                        AddCheck("LegendaryFlight_OriginalLandedWithState", m_flight != null && m_flight.Spawned && m_flight.Map == map && m_flight.HitPoints == m_flightHp && mods != null && mods.InstalledMods.Count == 1 && mods.InstalledMods[0].charges == 11 && m_flightDuplicate != null && !m_flightDuplicate.Destroyed && IsReserved(m_flight),
                            "landed, same wear and module, reserved", m_flight == null ? "null" : "spawned=" + m_flight.Spawned + " hp=" + m_flight.HitPoints + "/" + m_flightHp + " after " + (Find.TickManager.TicksGame - m_flightStartTick) + " ticks");
                        SecondIssueRefused("Landed");
                        m_reloadStartFrame = Time.frameCount;
                        return;
                    }
                    // Let the game draw the loaded wearers on the map before checking them.
                    if (Time.frameCount < m_reloadStartFrame + 45) return;
                    if (!m_reloadChecked)
                    {
                        m_reloadChecked = true;
                        try { RunReloadLogic(); CheckFitPawns("Reload"); CaptureScreen("EquipmentFitMapReload.png"); }
                        catch (Exception ex) { m_errors.Add("Unhandled exception: " + ex.ToString()); }
                    }
                    if (Time.frameCount < m_reloadStartFrame + 60) return;
                    s_logicDone = true;
                    FinishAndShutdown("EquipmentReloadProbe.json", false);
                    return;
                }

                s_logicDone = true;
                Application.logMessageReceived += OnUnityLog;

                try
                {
                    if (EquipmentProbeBootstrap.ReloadMode)
                    {
                        m_reloadStartFrame = Time.frameCount;
                        s_logicDone = false;
                        Pawn first = FindFitPawn(FitNicks[0]);
                        if (first != null) Find.CameraDriver.SetRootPosAndSize(first.Position.ToVector3Shifted(), 10f);
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
            if (s_finishedAll || Event.current == null || Event.current.type != EventType.Repaint) return;
            if (m_editorStartFrame >= 0) { UpdateEditorPhase(); return; }
            if (!m_waitingForRepaint) return;

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
            StartEditorPhase();
        }

        // Character Editor is a bundled component: open its real window for the T-60 wearer and let the game draw it.
        private void StartEditorPhase()
        {
            try
            {
                Pawn wearer = FindFitPawn("EqFitT60Female");
                Type editor = AccessTools.TypeByName("CharacterEditor.CEditor");
                if (wearer == null || editor == null)
                {
                    AddCheck("CharacterEditor_Opened", false, "wearer and editor available", "wearer=" + (wearer != null) + ", editor=" + (editor != null));
                    FinishAndShutdown("EquipmentProbe.json", true);
                    return;
                }
                object api = AccessTools.Property(editor, "API").GetValue(null, null);
                AccessTools.Method(editor, "StartEditor").Invoke(api, new object[] { wearer });
                m_editorStartFrame = Time.frameCount;
            }
            catch (Exception ex)
            {
                m_errors.Add("Character Editor open exception: " + ex.ToString());
                FinishAndShutdown("EquipmentProbe.json", true);
            }
        }

        private void UpdateEditorPhase()
        {
            int frames = Time.frameCount - m_editorStartFrame;
            if (frames == 30) CaptureScreen("EquipmentCharacterEditor.png");
            if (frames < 60) return;
            List<Window> open = Find.WindowStack.Windows.Where(w => w.GetType().FullName.StartsWith("CharacterEditor", StringComparison.Ordinal)).ToList();
            AddCheck("CharacterEditor_Opened", open.Count > 0, "editor window drawn for 60 frames", "windows=" + open.Count);
            for (int i = 0; i < open.Count; i++) open[i].Close(false);
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
                if (condition != null && condition.Contains("Simulated ")) return; // injected by this probe
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
            // A random colonist can itself take more than an ordinary human (IncomingDamageFactor 1.15 seen in 062013 and 081155):
            // the demon is compared with an ordinary human, its own factor taken out.
            float humanFactor = human2 != null ? Math.Max(0.1f, human2.GetStatValue(StatDefOf.IncomingDamageFactor)) : 1f;
            float humanOrdinary = humanDmg / humanFactor;
            bool demonTookMore = demon2 != null && human2 != null && (demon2.Dead || demonDmg >= humanOrdinary * 1.4f);
            AddCheck("WeaponMod_HolyWaterHurtsDemonMoreThanHuman", demonTookMore,
                "demon >= 1.4 x human for the same holy water hit",
                string.Format("demon={0:0.0}(dead={1}), human={2:0.0} (ordinary {3:0.0}, factor {4:0.00})", demonDmg, demon2 != null && demon2.Dead, humanDmg, humanOrdinary, humanFactor) + HolyDiag(demon2) + HolyDiag(human2));
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
            else if (hero != null && hero.inventory != null && hero.inventory.innerContainer != null)
            {
                Thing inInv = hero.inventory.innerContainer.FirstOrDefault(t => t.def == tankDef);
                if (inInv != null)
                {
                    CompEquipmentMod rc = inInv.TryGetComp<CompEquipmentMod>();
                    if (rc != null) returnedCharges = rc.Charges;
                }
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

            // ---------------- 7. MODULE REMOVAL WHEN INVENTORY AND CELL ARE BLOCKED (MODULE PRESERVED) ----------------
            ThingWithComps testWeapon = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDef.Named("Gun_AssaultRifle")), origin + new IntVec3(0, 0, 3), map, WipeMode.Vanish) as ThingWithComps;
            Thing modToInstall = GenSpawn.Spawn(ThingMaker.MakeThing(tankDef), origin + new IntVec3(-1, 0, 3), map, WipeMode.Vanish);
            CompWeaponMods testMods = testWeapon != null ? testWeapon.GetComp<CompWeaponMods>() : null;
            if (testMods != null && modToInstall != null) testMods.InstallMod(modToInstall);

            // Attempt removing when placement is completely impossible (null map / invalid drop pos / null pawn)
            Thing failedRemoved = testMods != null ? testMods.RemoveMod(0, null, IntVec3.Invalid, null) : null;
            bool modulePreservedOnFailure = failedRemoved == null && testMods != null && testMods.InstalledMods.Count == 1;
            AddCheck("Equipment_ModuleRemovalFailurePreservesSlotEntry", modulePreservedOnFailure,
                "slot entry preserved (count=1, returned=null)",
                string.Format("returned={0}, count={1}", failedRemoved != null ? failedRemoved.def.defName : "null", testMods != null ? testMods.InstalledMods.Count : -1));

            // ---------------- 8. MULTI-PART MELEE HIT DEDUCTS EXACTLY 1 CHARGE ----------------
            ThingDef clubDef = DefDatabase<ThingDef>.GetNamedSilentFail("MeleeWeapon_Club") ?? DefDatabase<ThingDef>.GetNamedSilentFail("MeleeWeapon_Mace");
            ThingWithComps club = GenSpawn.Spawn(ThingMaker.MakeThing(clubDef, clubDef.MadeFromStuff ? ThingDefOf.WoodLog : null), origin + new IntVec3(1, 0, 3), map, WipeMode.Vanish) as ThingWithComps;
            Thing meleeMod = GenSpawn.Spawn(ThingMaker.MakeThing(tankDef), origin + new IntVec3(1, 0, 4), map, WipeMode.Vanish);
            CompWeaponMods clubMods = club != null ? club.GetComp<CompWeaponMods>() : null;
            if (clubMods != null && meleeMod != null)
            {
                CompEquipmentMod em = meleeMod.TryGetComp<CompEquipmentMod>();
                if (em != null) em.Charges = 30;
                clubMods.InstallMod(meleeMod);
            }

            Pawn meleeTarget = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(2, 0, 3));
            EquipmentSlotsBootstrap.ActiveDamageSourceScope savedScope = EquipmentSlotsBootstrap.s_currentScope;
            EquipmentSlotsBootstrap.s_currentScope = new EquipmentSlotsBootstrap.ActiveDamageSourceScope { hitId = 900001, weapon = club };
            // Simulate multi-part hit (two PostApplyDamage calls within the same weapon scope)
            EquipmentSlotsBootstrap.Postfix_Pawn_PostApplyDamage(meleeTarget, new DamageInfo(DamageDefOf.Blunt, 5f, 0f, -1f, hero, null, club.def), 5f);
            EquipmentSlotsBootstrap.Postfix_Pawn_PostApplyDamage(meleeTarget, new DamageInfo(DamageDefOf.Blunt, 3f, 0f, -1f, hero, null, club.def), 3f);
            EquipmentSlotsBootstrap.s_currentScope = savedScope;

            int clubChargesAfter = (clubMods != null && clubMods.InstalledMods.Count > 0) ? clubMods.InstalledMods[0].charges : -1;
            AddCheck("Equipment_MultipartMeleeDeductsExactlyOneCharge", clubChargesAfter == 29, "charges=29 (deducted 1 for multipart hit)", clubChargesAfter.ToString());

            // ---------------- 9. PROJECTILE IMPACT AFTER WEAPON SWAP DEDUCTS FROM FIRING WEAPON, NOT NEW ----------------
            ThingWithComps rifleA = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDef.Named("Gun_AssaultRifle")), origin + new IntVec3(3, 0, 3), map, WipeMode.Vanish) as ThingWithComps;
            Thing modA = GenSpawn.Spawn(ThingMaker.MakeThing(tankDef), origin + new IntVec3(3, 0, 4), map, WipeMode.Vanish);
            CompWeaponMods modsA = rifleA != null ? rifleA.GetComp<CompWeaponMods>() : null;
            if (modsA != null && modA != null)
            {
                CompEquipmentMod emA = modA.TryGetComp<CompEquipmentMod>();
                if (emA != null) emA.Charges = 25;
                modsA.InstallMod(modA);
            }

            ThingWithComps rifleB = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDef.Named("Gun_AssaultRifle")), origin + new IntVec3(4, 0, 3), map, WipeMode.Vanish) as ThingWithComps;
            Thing modB = GenSpawn.Spawn(ThingMaker.MakeThing(tankDef), origin + new IntVec3(4, 0, 4), map, WipeMode.Vanish);
            CompWeaponMods modsB = rifleB != null ? rifleB.GetComp<CompWeaponMods>() : null;
            if (modsB != null && modB != null)
            {
                CompEquipmentMod emB = modB.TryGetComp<CompEquipmentMod>();
                if (emB != null) emB.Charges = 30;
                modsB.InstallMod(modB);
            }

            // Hero equips Rifle B
            hero.equipment.DestroyAllEquipment();
            if (rifleB.Spawned) rifleB.DeSpawn(DestroyMode.Vanish); // a thing must not stay spawned while it is held
            hero.equipment.AddEquipment(rifleB);

            // Bullet fired from Rifle A impacts target
            Pawn swapTarget = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(5, 0, 3));
            Bullet bulletFromA = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDef.Named("Bullet_AssaultRifle")), origin + new IntVec3(5, 0, 4), map, WipeMode.Vanish) as Bullet;
            if (bulletFromA != null)
            {
                bulletFromA.Launch(hero, origin.ToVector3Shifted(), swapTarget, swapTarget, ProjectileHitFlags.All, false, rifleA, null);
                // Impact target
                EquipmentSlotsBootstrap.ActiveDamageSourceScope previousScope = null;
                EquipmentSlotsBootstrap.Prefix_Projectile_Impact(bulletFromA, ref previousScope);
                swapTarget.TakeDamage(new DamageInfo(DamageDefOf.Bullet, 10f, 0.16f, -1f, hero, null, rifleA.def));
                EquipmentSlotsBootstrap.Finalizer_Projectile_Impact(previousScope);
                // A hit outside any weapon scope (no confirmed source instance) must not deduct anything.
                int beforeUnscoped = (modsB != null && modsB.InstalledMods.Count > 0) ? modsB.InstalledMods[0].charges : -1;
                swapTarget.TakeDamage(new DamageInfo(DamageDefOf.Bullet, 10f, 0.16f, -1f, hero, null, rifleB.def));
                int afterUnscoped = (modsB != null && modsB.InstalledMods.Count > 0) ? modsB.InstalledMods[0].charges : -1;
                AddCheck("Equipment_HitWithoutWeaponScopeDoesNotDeduct", beforeUnscoped == afterUnscoped,
                    beforeUnscoped.ToString(), afterUnscoped.ToString());
                bulletFromA.Destroy(DestroyMode.Vanish);
            }

            int chargesA = (modsA != null && modsA.InstalledMods.Count > 0) ? modsA.InstalledMods[0].charges : -1;
            int chargesB = (modsB != null && modsB.InstalledMods.Count > 0) ? modsB.InstalledMods[0].charges : -1;
            bool swapOk = chargesA == 24 && chargesB == 30;
            AddCheck("Equipment_ProjectileAfterWeaponSwapDoesNotDeductFromNewWeapon", swapOk,
                "rifleA=24, rifleB=30",
                string.Format("rifleA={0}, rifleB={1}", chargesA, chargesB));

            // A value different from a fresh module, so that the reload proves the saved number and not a default.
            if (modsB != null && modsB.InstalledMods.Count > 0) modsB.InstalledMods[0].charges = ReloadCharges;

            // ---------------- 9b. CONSUMPTION UNDER DESTROY FAULTS ----------------
            ConsumptionChecks(origin, tankDef);

            // ---------------- 9c. LORENZO WAVE: DAMAGE BY AN EXPLOSION, ONE INSTANCE WITH A MODULE ----------------
            ThingDef lorenzoDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Legendary_LorenzosArtifact");
            ThingWithComps lorA = MakeUnchecked(lorenzoDef), lorB = MakeUnchecked(lorenzoDef);
            CompWeaponMods lorMods = lorA.GetComp<CompWeaponMods>();
            Thing lorTank = ThingMaker.MakeThing(tankDef);
            lorTank.TryGetComp<CompEquipmentMod>().Charges = 20;
            bool lorInstalled = lorMods != null && lorMods.InstallMod(lorTank);
            for (int shot = 0; shot < 2; shot++)
            {
                ThingWithComps gun = shot == 0 ? lorA : lorB;
                Pawn t1 = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(12, 0, 3));
                Pawn t2 = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(12, 0, 3));
                t2.Position = t1.Position; t2.Notify_Teleported(true, true);
                IntVec3 hitAt = t1.Position;
                float before = Harm(t1) + Harm(t2);
                Projectile wave = (Projectile)GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_Projectile_LorenzoWave")), t1.Position, map);
                wave.Launch(hero, t1.DrawPos, t1, t1, ProjectileHitFlags.All, false, gun, null);
                var observations = new Dictionary<Pawn, LorenzoObservation>
                {
                    { t1, new LorenzoObservation() },
                    { t2, new LorenzoObservation() }
                };
                lorenzoTargets = observations;
                bool scopeClosed;
                int blasts;
                string victims;
                try
                {
                    AccessTools.DeclaredMethod(typeof(Projectile_LorenzoWave), "Impact", new[] { typeof(Thing), typeof(bool) }).Invoke(wave, new object[] { t1, false });
                    scopeClosed = EquipmentSlotsBootstrap.s_currentScope == null;
                    blasts = map.listerThings.AllThings.Count(t => t is Explosion);
                    victims = string.Join("+", new[] { t1, t2 }.Select(t => t.Position + " " + string.Join("/", t.apparel.WornApparel.Select(a => a.def.defName).ToArray()) + " dmgfactor=" + t.GetStatValue(StatDefOf.IncomingDamageFactor) + " job=" + t.CurJobDef).ToArray());
                    for (int waited = 0; waited < 120; waited++) yield return false;
                }
                finally
                {
                    lorenzoTargets = null;
                }
                bool damageBeforePush = observations.Values.Any(o => o.pushTick >= 0)
                    && observations.Values.Where(o => o.pushTick >= 0).All(o => o.damageTick >= 0 && o.damageTick < o.pushTick);
                AddCheck("Lorenzo_WaveDamageBeforePush_" + shot, damageBeforePush,
                    "actual wave damage precedes displacement for every pushed target",
                    string.Join(" | ", observations.Select(entry => entry.Key.ThingID + ": damageTick=" + entry.Value.damageTick
                        + " pushTick=" + entry.Value.pushTick + " " + entry.Value.pushFrom + " -> " + entry.Value.pushTo).ToArray()));
                float dealt = Harm(t1) + Harm(t2) - before;
                int left = lorMods != null && lorMods.InstalledMods.Count > 0 ? lorMods.InstalledMods[0].charges : -1;
                bool burned = new[] { t1, t2 }.Any(t => t.health.hediffSet.hediffs.Any(h => h.def.defName == "Burn"));
                AddCheck("Lorenzo_DamagesAndPushes_" + shot, dealt > 0f && new[] { t1, t2 }.Any(t => t.Dead || t.Downed || t.Position != hitAt), "hurt and pushed", "dealt=" + dealt.ToString("0.0") + " moved=" + t1.Position.DistanceTo(hitAt).ToString("0.0") + "/" + t2.Position.DistanceTo(hitAt).ToString("0.0") + " dead=" + t1.Dead + "/" + t2.Dead + " blasts=" + blasts + " at " + hitAt + " victims " + victims + " hediffs " + string.Join(" | ", new[] { t1, t2 }.Select(t => string.Join(",", t.health.hediffSet.hediffs.Select(h => h.def.defName + (h.Part != null ? "@" + h.Part.def.defName : "")).ToArray())).ToArray()));
                if (shot == 0) AddCheck("Lorenzo_ModuleTriggersOncePerImpact", lorInstalled && scopeClosed && dealt > 0f && left == 19, "charges 19 after an explosion with two victims, scope closed", "installed=" + lorInstalled + " scopeClosed=" + scopeClosed + " dealt=" + dealt.ToString("0.0") + " charges=" + left + " burn=" + burned);
                else AddCheck("Lorenzo_SecondInstanceDoesNotUseFirstsModule", scopeClosed && dealt > 0f && left == 19, "charges stay 19", "dealt=" + dealt.ToString("0.0") + " charges=" + left);
                foreach (Pawn t in new[] { t1, t2 }) if (!t.Destroyed && !t.Dead) t.Destroy(DestroyMode.Vanish);
            }

            // ---------------- 9d. LEGENDARY IN FLIGHT: NO SECOND COPY, THE ORIGINAL LANDS ----------------
            LegendaryInFlight();

            // ---------------- 10. POWER ARMOR HELMETS (T-60 / X-01) AND SUITS (T-45 HAS NO HELMET) ----------------
            CheckPowerArmorRule();
            IntVec3 fitOrigin = FindStandableNear(map.Center);
            Find.CameraDriver.SetRootPosAndSize(fitOrigin.ToVector3Shifted(), 10f);
            for (int i = 0; i < FitNicks.Length; i++)
            {
                Pawn fit = SpawnPawn(null, Faction.OfPlayer, fitOrigin);
                fit.Name = new NameTriple("Fit", FitNicks[i], "Tester");
                fit.story.bodyType = FitNicks[i].EndsWith("Female", StringComparison.Ordinal) ? BodyTypeDefOf.Female : BodyTypeDefOf.Male;
                WearApparel(fit, FitArmor(FitNicks[i]));
                if (FitHelmet(FitNicks[i]) != null) WearApparel(fit, FitHelmet(FitNicks[i]));
                fit.Drawer.renderer.SetAllGraphicsDirty();
            }
            int frameStart = Time.frameCount;
            while (Time.frameCount < frameStart + 45) yield return false;
            CheckFitPawns("Fresh");
            CaptureScreen("EquipmentFitMap.png");
            frameStart = Time.frameCount;
            while (Time.frameCount < frameStart + 15) yield return false;
        }

        /// The real thing with its comps, without the uniqueness gate of PostPostMake (a duplicate from an old save, or a second test weapon).
        private static ThingWithComps MakeUnchecked(ThingDef def)
        {
            ThingWithComps t = (ThingWithComps)Activator.CreateInstance(def.thingClass);
            t.def = def;
            t.PostMake();
            return t;
        }

        // A fault injected into the real Destroy of one item kind: refuses silently, throws before, throws after it is destroyed.
        private static string s_faultMode;
        private static ThingDef s_faultDef;
        public static bool FaultPrefix(Thing __instance)
        {
            if (s_faultMode == null || __instance.def != s_faultDef) return true;
            if (s_faultMode == "silent") return false;
            if (s_faultMode == "before") throw new InvalidOperationException("Simulated Destroy failure");
            return true;
        }
        public static bool ExpectedException(Exception ex)
        {
            if (ex == null || !ex.ToString().Contains("Simulated ")) return true;
            Log.Warning("[TMM probe] expected: " + ex.Message);
            return false;
        }
        public static void FaultPostfix(Thing __instance)
        {
            if (s_faultMode == "after" && __instance.def == s_faultDef) throw new InvalidOperationException("Simulated PostDestroy failure");
        }
        private int TotalOf(ThingDef def)
        {
            int n = map.listerThings.ThingsOfDef(def).Sum(t => t.stackCount);
            foreach (Pawn p in map.mapPawns.AllPawns) if (p.inventory != null) n += p.inventory.innerContainer.Where(t => t.def == def).Sum(t => t.stackCount);
            List<Thing> held = new List<Thing>();
            ThingOwnerUtility.GetAllThingsRecursively(Find.World.GetComponent<StoryPaymentLimbo>(), held, true, null);
            return n + held.Where(t => t.def == def).Sum(t => t.stackCount);
        }

        private void ConsumptionChecks(IntVec3 origin, ThingDef tankDef)
        {
            Harmony h = new Harmony("tmm.test.destroy.fault");
            MethodInfo destroy = AccessTools.DeclaredMethod(typeof(ThingWithComps), "Destroy");
            h.Patch(destroy, prefix: new HarmonyMethod(typeof(InGameEquipmentProbe), "FaultPrefix"), postfix: new HarmonyMethod(typeof(InGameEquipmentProbe), "FaultPostfix"));
            int oldLimit = tankDef.stackLimit;
            tankDef.stackLimit = 75; // the split path needs a real stack; restored below
            // The injected exceptions are expected: report them as warnings so that a real error still fails the run.
            MethodInfo report = AccessTools.Method(typeof(TestMode), "ReportException");
            h.Patch(report, prefix: new HarmonyMethod(typeof(InGameEquipmentProbe), "ExpectedException"));
            try
            {
                foreach (string mode in new[] { "silent", "before", "after" })
                {
                    foreach (int stack in new[] { 3, 1 })
                    {
                        ThingWithComps gun = (ThingWithComps)GenSpawn.Spawn(ThingMaker.MakeThing(ThingDef.Named("Gun_AssaultRifle")), FindStandableNear(origin), map);
                        Thing mods = ThingMaker.MakeThing(tankDef); mods.stackCount = stack;
                        GenSpawn.Spawn(mods, FindStandableNear(origin), map);
                        CompWeaponMods comp = gun.GetComp<CompWeaponMods>();
                        int total = TotalOf(tankDef);
                        s_faultDef = tankDef; s_faultMode = mode;
                        bool installed;
                        try { installed = comp.InstallMod(mods); } finally { s_faultMode = null; }
                        int after = TotalOf(tankDef);
                        bool consumed = mode == "after";
                        AddCheck("Install_" + mode + "_stack" + stack, installed == consumed && comp.InstalledMods.Count == (consumed ? 1 : 0) && after == total - (consumed ? 1 : 0) && (consumed || !mods.Destroyed && mods.Spawned && mods.stackCount == stack),
                            consumed ? "installed, one less" : "refused, same amount", "installed=" + installed + " slots=" + comp.InstalledMods.Count + " total " + total + "->" + after + " stack=" + (mods.Destroyed ? "destroyed" : mods.stackCount.ToString()));
                        if (!mods.Destroyed) mods.Destroy(DestroyMode.Vanish);
                        gun.Destroy(DestroyMode.Vanish);
                    }
                }
                ThingDef coltDef = DefDatabase<ThingDef>.GetNamed("TMM_Legendary_TheColt");
                foreach (string mode in new[] { "silent", "before", "after" })
                {
                    foreach (int stack in new[] { 10, 6 })
                    {
                        ThingWithComps colt = MakeUnchecked(coltDef);
                        GenSpawn.Spawn(colt, FindStandableNear(origin), map);
                        CompColtAmmo ammoComp = colt.GetComp<CompColtAmmo>();
                        ammoComp.SetCharges(ammoComp.MaxCharges - 6);
                        ThingDef bullets = DefDatabase<ThingDef>.AllDefs.First(d => d.defName.StartsWith("TMM_") && d.defName.Contains("SilverBullet"));
                        Thing ammo = ThingMaker.MakeThing(bullets); ammo.stackCount = stack;
                        GenSpawn.Spawn(ammo, FindStandableNear(origin), map);
                        int total = TotalOf(bullets), charges = ammoComp.Charges, loaded;
                        s_faultDef = bullets; s_faultMode = mode;
                        bool ok;
                        try { ok = JobDriver_ReloadColt.TryReloadColtAtomic(colt, ammo, 6, out loaded); } finally { s_faultMode = null; }
                        int after = TotalOf(bullets), delta = ammoComp.Charges - charges;
                        AddCheck("Colt_" + mode + "_stack" + stack, ok == (mode == "after") && loaded == delta && total - after == delta && delta == (mode == "after" ? 6 : 0), "rounds spent = charges gained (" + (mode == "after" ? 6 : 0) + ")", "ok=" + ok + " loaded=" + loaded + " charges+" + delta + " rounds " + total + "->" + after);
                        if (!ammo.Destroyed) ammo.Destroy(DestroyMode.Vanish);
                        colt.Destroy(DestroyMode.Vanish);
                    }
                }
            }
            finally { s_faultMode = null; tankDef.stackLimit = oldLimit; h.Unpatch(destroy, HarmonyPatchType.All, "tmm.test.destroy.fault"); h.Unpatch(report, HarmonyPatchType.All, "tmm.test.destroy.fault"); }
        }

        private static ThingDef FlightDef { get { return DefDatabase<ThingDef>.GetNamed("TMM_Legendary_OverseersGuardian"); } }
        private ThingWithComps m_flight, m_flightDuplicate;
        private int m_flightHp;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref m_flight, "eqFlight");
            Scribe_References.Look(ref m_flightDuplicate, "eqFlightDuplicate");
            Scribe_Values.Look(ref m_flightHp, "eqFlightHp");
        }

        private static bool IsReserved(Thing t)
        {
            var ids = (Dictionary<string, string>)AccessTools.Field(typeof(LegendaryRegistry), "grantedThingIds").GetValue(LegendaryRegistry.Instance);
            string id;
            return t != null && ids.TryGetValue(t.def.defName, out id) && id == t.ThingID;
        }

        private bool SecondIssueRefused(string when)
        {
            LegendaryRegistry reg = LegendaryRegistry.Instance;
            bool granted = reg.TryGrant(FlightDef, map);
            Thing copy = ThingMaker.MakeThing(FlightDef);
            bool copyGone = copy.Destroyed;
            if (!copyGone) copy.Destroy(DestroyMode.Vanish);
            bool ok = !granted && copyGone && !m_flight.Destroyed && IsReserved(m_flight);
            AddCheck("LegendaryFlight_" + when + "_SecondIssueRefused", ok, "no grant, copy gone, original reserved", "granted=" + granted + " copyGone=" + copyGone + " original=" + !m_flight.Destroyed + " reserved=" + IsReserved(m_flight));
            return ok;
        }

        private void LegendaryInFlight()
        {
            LegendaryRegistry reg = LegendaryRegistry.Instance;
            m_flight = (ThingWithComps)ThingMaker.MakeThing(FlightDef);
            GenSpawn.Spawn(m_flight, FindStandableNear(map.Center), map);
            Thing module = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_Mod_HolyWaterTank"));
            module.TryGetComp<CompEquipmentMod>().Charges = 11;
            CompWeaponMods mods = m_flight.GetComp<CompWeaponMods>();
            bool withModule = mods != null && mods.InstallMod(module);
            m_flightHp = m_flight.HitPoints = Math.Max(1, m_flight.MaxHitPoints - 7);
            m_flight.DeSpawn(DestroyMode.Vanish);
            RimWorld.Planet.PlanetTile far;
            RimWorld.Planet.TileFinder.TryFindNewSiteTile(out far, 20, 40, false, null, 0f, true, RimWorld.Planet.TileFinderMode.Near, false, false, null, null);
            var pods = (RimWorld.Planet.TravellingTransporters)RimWorld.Planet.WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravellingTransporters"));
            pods.Tile = far;
            pods.SetFaction(Faction.OfPlayer);
            pods.destinationTile = map.Tile;
            pods.arrivalAction = new RimWorld.Planet.TransportersArrivalAction_LandInSpecificCell(map.Parent, FindStandableNear(map.Center));
            Find.WorldObjects.Add(pods);
            var info = new ActiveTransporterInfo();
            info.innerContainer.TryAdd(m_flight, false);
            pods.AddTransporter(info, true);
            AddCheck("LegendaryFlight_InFlight", m_flight.holdingOwner != null && !m_flight.Spawned && withModule && IsReserved(m_flight), "held by the transporter, reserved", "held=" + (m_flight.holdingOwner != null) + " module=" + withModule);
            SecondIssueRefused("Fresh");
            // A duplicate as an old save could have it: it must not cost the original anything when that lands.
            // (It is made while the registry looks away: the creation gate would refuse it.)
            LegendaryRegistry look = LegendaryRegistry.Instance;
            var ids = (Dictionary<string, string>)AccessTools.Field(typeof(LegendaryRegistry), "grantedThingIds").GetValue(look);
            string originalId = ids[FlightDef.defName];
            ids.Remove(FlightDef.defName);
            m_flightDuplicate = (ThingWithComps)ThingMaker.MakeThing(FlightDef);
            ((System.Collections.IDictionary)AccessTools.Field(typeof(LegendaryRegistry), "pendingCreations").GetValue(look)).Clear();
            ids[FlightDef.defName] = originalId;
            GenSpawn.Spawn(m_flightDuplicate, FindStandableNear(map.Center), map);
            AddCheck("LegendaryFlight_OldDuplicateTouchesNothing", !m_flight.Destroyed && m_flightDuplicate.Spawned && IsReserved(m_flight), "both exist, reservation with the original", "original=" + !m_flight.Destroyed + " duplicate=" + m_flightDuplicate.Spawned);
        }

        private const int ReloadCharges = 17;
        private static readonly string[] FitNicks =
        {
            "EqFitT60Female", "EqFitT60Male", "EqFitX01Female", "EqFitX01Male", "EqFitT45Female", "EqFitT45Male"
        };

        private static string FitArmor(string nick)
        {
            if (nick.StartsWith("EqFitT60", StringComparison.Ordinal)) return "TMM_Apparel_bos_t60_power_armor";
            if (nick.StartsWith("EqFitX01", StringComparison.Ordinal)) return "TMM_Apparel_enclave_x01_power_armor";
            return "TMM_Apparel_outcast_power_armor";
        }

        private static string FitHelmet(string nick)
        {
            if (nick.StartsWith("EqFitT60", StringComparison.Ordinal)) return "TMM_Apparel_bos_t60_helmet";
            if (nick.StartsWith("EqFitX01", StringComparison.Ordinal)) return "TMM_Apparel_enclave_x01_helmet";
            return null;
        }

        private Pawn FindFitPawn(string nick)
        {
            return map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => p.Name is NameTriple && ((NameTriple)p.Name).Nick == nick);
        }

        private void CheckPowerArmorRule()
        {
            string[] suits =
            {
                "TMM_Apparel_bos_t60_power_armor", "TMM_Apparel_enclave_x01_power_armor", "TMM_Apparel_outcast_power_armor",
                "TMM_Apparel_clan_bioferrite_carapace", "TMM_Legendary_ProtectorArmor"
            };
            List<string> bad = new List<string>();
            for (int i = 0; i < suits.Length; i++)
            {
                ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(suits[i]);
                if (d == null || !EquipmentUtil.IsPowerArmorDef(d) || !d.apparel.layers.Any(l => l.defName == "TMM_PowerArmor")) bad.Add(suits[i]);
            }
            AddCheck("PowerArmor_RealSuitsStillRecognized", bad.Count == 0, "5 suits recognized", bad.Count == 0 ? "5" : "failed: " + string.Join(",", bad.ToArray()));

            // Every head item in the game, not just the two reported helmets.
            List<string> heads = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.IsApparel && d.apparel.layers != null && (d.apparel.layers.Contains(ApparelLayerDefOf.Overhead) || d.apparel.layers.Contains(ApparelLayerDefOf.EyeCover)))
                .Where(d => d.apparel.layers.Any(l => l.defName == "TMM_PowerArmor") || EquipmentUtil.IsPowerArmorDef(d))
                .Select(d => d.defName).ToList();
            AddCheck("PowerArmor_NoHeadgearOnPowerArmorLayer", heads.Count == 0, "none", heads.Count == 0 ? "none" : string.Join(",", heads.ToArray()));

            // The worn graphic of each helmet and suit must resolve for every human body type.
            BodyTypeDef[] bodies = { BodyTypeDefOf.Male, BodyTypeDefOf.Female, BodyTypeDefOf.Thin, BodyTypeDefOf.Fat, BodyTypeDefOf.Hulk };
            string[] items = { "TMM_Apparel_bos_t60_helmet", "TMM_Apparel_enclave_x01_helmet", suits[0], suits[1], suits[2], suits[3], suits[4] };
            int resolved = 0;
            List<string> broken = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(items[i]);
                Apparel ap = d == null ? null : ThingMaker.MakeThing(d, d.MadeFromStuff ? GenStuff.DefaultStuffFor(d) : null) as Apparel;
                for (int b = 0; ap != null && b < bodies.Length; b++)
                {
                    ApparelGraphicRecord rec;
                    bool got = ApparelGraphicRecordGetter.TryGetGraphicApparel(ap, bodies[b], false, out rec);
                    if (got && rec.graphic != null && rec.graphic != BaseContent.BadGraphic && rec.graphic.MatSouth != null && rec.graphic.MatSouth != BaseContent.BadMat) resolved++;
                    else broken.Add(items[i] + "/" + bodies[b].defName);
                }
                if (ap != null && !ap.Destroyed) ap.Destroy(DestroyMode.Vanish); // a second unique legendary removes itself
            }
            AddCheck("PowerArmor_WornGraphicsResolve", resolved == items.Length * bodies.Length, (items.Length * bodies.Length).ToString(),
                resolved + (broken.Count > 0 ? " broken: " + string.Join(",", broken.ToArray()) : ""));
        }

        private void CheckFitPawns(string stage)
        {
            for (int i = 0; i < FitNicks.Length; i++)
            {
                string nick = FitNicks[i];
                Pawn p = FindFitPawn(nick);
                string helmet = FitHelmet(nick);
                bool armorWorn = p != null && p.apparel.WornApparel.Any(a => a.def.defName == FitArmor(nick));
                bool helmetWorn = helmet == null || (p != null && p.apparel.WornApparel.Any(a => a.def.defName == helmet));
                string expectedBody = nick.EndsWith("Female", StringComparison.Ordinal) ? "Female" : "Male";
                string body = p != null && p.story != null && p.story.bodyType != null ? p.story.bodyType.defName : "null";
                int opaque = 0, magenta = 0;
                if (p != null)
                {
                    RenderTexture rt = PortraitsCache.Get(p, new Vector2(192f, 192f), Rot4.South, default(Vector3), 1f, true, true, true, true);
                    if (rt != null) SaveBmp(rt, Path.Combine(GenFilePaths.SaveDataFolderPath, stage + "-" + nick + ".bmp"), out opaque, out magenta);
                }
                bool treeOk = p != null && p.Drawer.renderer.renderTree != null && p.Drawer.renderer.renderTree.Resolved;
                AddCheck("PowerArmor_" + stage + "_" + nick, armorWorn && helmetWorn && body == expectedBody && treeOk && opaque > 2000 && magenta == 0,
                    "worn, body=" + expectedBody + ", tree resolved, portrait drawn without the error material",
                    string.Format("armor={0}, helmet={1}, body={2}, tree={3}, opaquePx={4}, magentaPx={5}, onMap={6}", armorWorn, helmet == null ? "n/a" : helmetWorn.ToString(), body, treeOk, opaque, magenta, p != null && p.Spawned));
            }
            AddCheck("PowerArmor_" + stage + "_NoTextureErrors", !m_errors.Any(e => e.Contains("Failed to find any textures")), "none",
                string.Join(" | ", m_errors.Where(e => e.Contains("Failed to find any textures")).ToArray()));
        }

        private static void CaptureScreen(string name)
        {
            AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"), "CaptureScreenshot", new Type[] { typeof(string) })
                .Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, name) });
        }

        private static void SaveBmp(RenderTexture rt, string path, out int opaque, out int magenta)
        {
            opaque = 0; magenta = 0;
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Color32[] px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            using (BinaryWriter bw = new BinaryWriter(File.Create(path)))
            {
                bw.Write((byte)'B'); bw.Write((byte)'M'); bw.Write(54 + w * h * 4); bw.Write(0); bw.Write(54);
                bw.Write(40); bw.Write(w); bw.Write(h); bw.Write((short)1); bw.Write((short)32); bw.Write(0); bw.Write(w * h * 4); bw.Write(2835); bw.Write(2835); bw.Write(0); bw.Write(0);
                for (int i = 0; i < px.Length; i++)
                {
                    Color32 c = px[i];
                    if (c.a > 128) opaque++;
                    if (c.a > 128 && c.r > 110 && c.g < 30 && c.b > 60 && c.b < c.r) magenta++; // BadMat background, calibrated on the run with the old rule
                    float a = c.a / 255f;
                    bw.Write((byte)(c.b * a + 112 * (1 - a))); bw.Write((byte)(c.g * a + 112 * (1 - a))); bw.Write((byte)(c.r * a + 112 * (1 - a))); bw.Write((byte)255);
                }
            }
            UnityEngine.Object.Destroy(tex);
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
                                      rifleMods.InstalledMods[0].charges == ReloadCharges;
            AddCheck("Reload_WeaponModAndChargesPersisted", weaponModPersisted,
                "HolyWaterTank[" + ReloadCharges + "]",
                (rifleMods != null && rifleMods.InstalledMods.Count > 0)
                    ? string.Format("{0}[{1}]", rifleMods.InstalledMods[0].modDefName, rifleMods.InstalledMods[0].charges)
                    : "none, primary=" + (rifle != null ? rifle.ThingID + " comp=" + (rifleMods != null) : "null"));

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

        /// Diagnostic only (f-equipment-3, 062013: human took 9.2 from an 8-point hit).
        private static string HolyDiag(Pawn p)
        {
            if (p == null) return " | null";
            return " | " + p.LabelShort + " factor=" + p.GetStatValue(StatDefOf.IncomingDamageFactor).ToString("0.00") + " heat=" + p.GetStatValue(StatDefOf.ArmorRating_Heat).ToString("0.00") + " traits=" + (p.story == null ? "" : string.Join(",", p.story.traits.allTraits.Select(x => x.def.defName).ToArray())) + " genes=" + (p.genes == null ? "" : string.Join(",", p.genes.GenesListForReading.Where(g => g.def.statFactors != null || g.def.damageFactors != null).Select(g => g.def.defName).ToArray())) + " apparel=" + string.Join(",", p.apparel.WornApparel.Select(a => a.def.defName).ToArray()) + " injuries=" + string.Join(",", p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Select(h => h.def.defName + "@" + (h.Part == null ? "body" : h.Part.def.defName) + ":" + h.Severity.ToString("0.0")).ToArray());
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

        /// Injuries plus the health of parts torn off: an explosion that takes a leg did harm even with no injury left on it
        /// (h-equipment-2, 081155: dealt=0.0 with both victims missing a leg).
        private static float Harm(Pawn pawn)
        {
            float sum = SumInjuries(pawn);
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return sum;
            foreach (Hediff_MissingPart m in pawn.health.hediffSet.GetMissingPartsCommonAncestors())
                if (m.Part != null) sum += m.Part.def.GetMaxHealth(pawn);
            return sum;
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
