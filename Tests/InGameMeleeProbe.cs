using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

public sealed class MeleeProbe : MapComponent
{
    private bool started, reported, uiReported;
    private int startTick, checks, dataLoaded, patches;
    private object renderer;
    private Pawn animatedPawn;
    private float timeAdvanced;
    private readonly List<string> errors = new List<string>();
    public MeleeProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "AM.Core"); }
    private static object Get(object target, string name) { return AccessTools.Property(target.GetType(), name).GetValue(target, null); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-melee-probe")) return;
        try
        {
            if (!started)
            {
                started = true; startTick = Find.TickManager.TicksGame;
                var mod = Mod(); var assembly = mod.GetType().Assembly;
                bool original = ModsConfig.IsActive("co.uk.epicguru.meleeanimation");
                Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "zAnimationMod") == 1, "Duplicate animation DLL");
                Check(mod.Content.PackageId == (original ? "co.uk.epicguru.meleeanimation" : "colonyproject.integrated"), "Wrong animation owner");
                string resourceRoot = original ? mod.Content.RootDir : Path.Combine(mod.Content.RootDir, "Components/MeleeAnimation");
                string expected = Path.Combine(resourceRoot, original ? "1.6/Assemblies/zAnimationMod.dll" : "Assemblies/zAnimationMod.dll");
                Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong animation DLL path");
                patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
                Check(patches > 10, "Missing melee patches");
                Type animDef = AccessTools.TypeByName("AM.AnimDef");
                var defs = ((IEnumerable)typeof(DefDatabase<>).MakeGenericType(animDef).GetProperty("AllDefsListForReading").GetValue(null, null)).Cast<Def>().ToList();
                Check(defs.Count > 30 && defs.All(d => d.modContentPack == mod.Content), "Wrong animation definitions/owner");
                foreach (var definition in defs)
                {
                    Check(Get(definition, "Data") != null, "Missing animation data " + definition.defName); dataLoaded++;
                    string path = (string)Get(definition, "FullDataPath");
                    Check(File.Exists(path) && Path.GetFullPath(path).StartsWith(Path.GetFullPath(resourceRoot), StringComparison.OrdinalIgnoreCase), "Animation data outside component " + definition.defName);
                }
                var content = AccessTools.TypeByName("AM.Content");
                Check(AccessTools.Field(content, "bundle").GetValue(null) != null, "Animation shader bundle not loaded");
                Check(AccessTools.Field(content, "CustomCutoffMaterial").GetValue(null) != null, "Animation material missing");
                Check((int)AccessTools.Property(AccessTools.TypeByName("AM.Tweaks.TweakDataManager"), "TweakDataLoadedCount").GetValue(null, null) > 0, "Weapon tweak data missing");
                var field = mod.GetType().GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Single(f => typeof(ModSettings).IsAssignableFrom(f.FieldType));
                var settings = (ModSettings)field.GetValue(null);
                var toggle = settings.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).First(f => f.FieldType == typeof(bool) && !f.IsInitOnly);
                bool old = (bool)toggle.GetValue(settings);
                try
                {
                    toggle.SetValue(settings, !old); string file = Path.Combine(GenFilePaths.SaveDataFolderPath, "MeleeSettings.xml");
                    Scribe.saver.InitSaving(file, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving();
                    toggle.SetValue(settings, old); Scribe.loader.InitLoading(file); settings.ExposeData(); Scribe.loader.FinalizeLoading();
                    Check((bool)toggle.GetValue(settings) == !old, "Melee settings Scribe failed");
                }
                finally { toggle.SetValue(settings, old); }
                var spot = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("AM_DuelSpot"));
                try { GenSpawn.Spawn(spot, new IntVec3(20, 0, 20), map); Check(spot.Spawned && spot.GetType().FullName == "AM.Buildings.Building_DuelSpot", "Duel spot failed to spawn"); }
                finally { if (!spot.Destroyed) spot.Destroy(); }
                Check(DefDatabase<ThingDef>.GetNamed("AM_LassoCloth").apparel != null, "Lasso apparel definition missing");
                Check((bool)AccessTools.Field(mod.GetType(), "IsSimpleSidearmsActive").GetValue(null), "Embedded sidearms not recognized");
                if (!original)
                {
                    var rootHelper = AccessTools.Method(AccessTools.TypeByName("TurboMegaMod.MeleeResourceCompatibility"), "ResourceRoot");
                    var corePack = LoadedModManager.RunningModsListForReading.First(m => m.PackageId == "ludeon.rimworld");
                    Check((string)rootHelper.Invoke(null, new object[] { corePack }) == corePack.RootDir, "Unrelated mod root redirected");
                    var hands = AccessTools.TypeByName("ShowMeYourHands.HandDrawer").GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single(m => m.Name == "DrawHandsOnWeapon" && m.GetParameters().Length == 6);
                    Check(Harmony.GetPatchInfo(hands).Owners.Contains("turbomegamod.melee.resources"), "Hands did not recognize embedded animations");
                    Check(mod.SettingsCategory() == "Анимации ближнего боя", "Settings title missing");
                }
                Pawn pawn = map.mapPawns.FreeColonistsSpawned.Last(p => !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Violent));
                animatedPawn = pawn;
                pawn.jobs.StopAll(); pawn.equipment.DestroyAllEquipment();
                pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MeleeWeapon_LongSword"), ThingDefOf.Steel));
                var bow = defs.Single(d => d.defName == "AM_DuelBow_Medium");
                var rendererType = AccessTools.TypeByName("AM.AnimRenderer");
                renderer = Activator.CreateInstance(rendererType, new object[] { bow, map });
                AccessTools.Field(rendererType, "RootTransform").SetValue(renderer, Matrix4x4.TRS(pawn.DrawPos, Quaternion.identity, Vector3.one));
                AccessTools.Method(rendererType, "AddPawn", new[] { typeof(Pawn) }).Invoke(renderer, new object[] { pawn });
                Check((bool)AccessTools.Method(rendererType, "Register").Invoke(renderer, null), "Animation renderer did not register");
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                return;
            }
            if (Find.TickManager.TicksGame - startTick < 20) return;
            timeAdvanced = Convert.ToSingle(Get(renderer, "CurrentTime"));
            Check(timeAdvanced > 0, "Real animation clock did not advance");
            var makeArgs = AccessTools.Method(AccessTools.TypeByName("AM.Patches.Patch_PawnRenderer_RenderPawnAt"), "MakeDrawArgs");
            var fix = AccessTools.Method(AccessTools.TypeByName("TurboMegaMod.MeleeCompatibility"), "EnsureDrawPawn");
            var owner = new Harmony("turbomegamod.melee.waylith");
            object[] drawArgs = { renderer, animatedPawn, default(PawnDrawParms) };
            owner.Unpatch(makeArgs, fix);
            try
            {
                makeArgs.Invoke(null, drawArgs);
                Check(((PawnDrawParms)drawArgs[2]).pawn == null, "Original missing render pawn was not reproduced");
            }
            finally { owner.Patch(makeArgs, prefix: new HarmonyMethod(fix)); }
            drawArgs[2] = default(PawnDrawParms); makeArgs.Invoke(null, drawArgs);
            Check(((PawnDrawParms)drawArgs[2]).pawn == animatedPawn, "Render argument patch did not fill pawn identity");
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        finally { if (started && (errors.Count > 0 || Find.TickManager.TicksGame - startTick >= 20)) Finish(); }
    }
    private void Finish()
    {
        if (renderer != null) AccessTools.Method(renderer.GetType(), "Destroy").Invoke(renderer, null);
        reported = true;
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MeleeProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"animationDataLoaded\":" + dataLoaded + ",\"animationTimeAdvanced\":" + timeAdvanced.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"patchedMethods\":" + patches + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-melee-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(25, 25, 1150, 750));
        try { Mod().DoSettingsWindowContents(new Rect(0, 0, 1100, 700)); }
        catch (Exception e) { error = e.ToString(); }
        finally { GUI.EndGroup(); Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "MeleeUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
