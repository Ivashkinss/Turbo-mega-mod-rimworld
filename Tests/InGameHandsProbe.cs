using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

// Actual hand-render method with temporary DrawMesh call counting, no user save.
public sealed class HandsProbe : MapComponent
{
    private bool reported, uiReported;
    private static bool countDraws;
    private static int drawCalls;
    public HandsProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static Verse.Mod Mod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "ShowMeYourHands.ShowMeYourHandsMod"); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void NoteDraw() { if (countDraws) drawCalls++; }
    private static IEnumerable<CodeInstruction> CountMeshCalls(IEnumerable<CodeInstruction> code)
    {
        foreach (var instruction in code)
        {
            var method = instruction.operand as MethodInfo;
            if (method != null && method.DeclaringType == typeof(Graphics) && method.Name == "DrawMesh") yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HandsProbe), "NoteDraw"));
            yield return instruction;
        }
    }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-hands-probe")) return;
        var pawn = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Violent)); if (pawn == null) return;
        reported = true; int checks = 0, patches = 0; var errors = new List<string>(); MethodInfo render = null;
        var diagnostic = new Harmony("tmm.diagnostic.hands");
        try
        {
            var mod = Mod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "ShowMeYourHands") == 1, "Duplicate hands DLL"); checks++;
            bool original = ModsConfig.IsActive("mlie.showmeyourhands"); string id = original ? "mlie.showmeyourhands" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong owner"); checks++;
            string expected = Path.Combine(mod.Content.RootDir, original ? "1.6/Assemblies/ShowMeYourHands.dll" : "Components/ShowMeYourHands/Assemblies/ShowMeYourHands.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong hands DLL path"); checks++;
            // The real normal-menu initialization is skipped by some quicktests.
            AccessTools.Method(AccessTools.TypeByName("ShowMeYourHands.RimWorld_MainMenuDrawer_MainMenuOnGUI"), "MainMenuOnGUI").Invoke(null, null);
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 3, "Missing render hooks"); checks++;
            Check(mod.SettingsCategory() == "Анимация рук с оружием", "Untranslated title"); checks++;
            var sf = AccessTools.Field(mod.GetType(), "Settings"); var settings = (ModSettings)sf.GetValue(sf.IsStatic ? null : mod);
            var rotation = AccessTools.Field(settings.GetType(), "Rotation"); object old = rotation.GetValue(settings);
            try
            {
                rotation.SetValue(settings, true); string sp = Path.Combine(GenFilePaths.SaveDataFolderPath, "HandSettings.xml");
                Scribe.saver.InitSaving(sp, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); rotation.SetValue(settings, false);
                Scribe.loader.InitLoading(sp); settings.ExposeData(); Scribe.loader.FinalizeLoading(); Check((bool)rotation.GetValue(settings), "Settings Scribe failed"); checks++;
            }
            finally { rotation.SetValue(settings, old); }
            Type vector = AccessTools.TypeByName("ShowMeYourHands.SaveableVector3"); object value = AccessTools.Constructor(vector, new[] { typeof(float), typeof(float), typeof(float) }).Invoke(new object[] { 0.125f, 0.25f, -0.5f });
            string text = value.ToString(); object parsed = AccessTools.Method(vector, "FromString").Invoke(null, new object[] { text });
            Check(Math.Abs(Convert.ToSingle(AccessTools.Property(vector, "X").GetValue(parsed, null)) - 0.125f) < 0.001f && Math.Abs(Convert.ToSingle(AccessTools.Property(vector, "Z").GetValue(parsed, null)) + 0.5f) < 0.001f, "Offset vector round trip failed"); checks++;
            var drawer = pawn.AllComps.Single(c => c.GetType().FullName == "ShowMeYourHands.HandDrawer");
            Check(pawn.def.comps.Count(c => c.compClass == drawer.GetType()) == 1, "Duplicate/missing hand drawer"); checks++;
            pawn.jobs.StopAll(); pawn.equipment.DestroyAllEquipment(); var gun = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_AssaultRifle")); pawn.equipment.AddEquipment(gun); pawn.drafter.Drafted = true;
            var props = gun.def.comps.Single(c => c.GetType().FullName == "ShowMeYourHands.WhandCompProps");
            Check((Vector3)AccessTools.Field(props.GetType(), "MainHand").GetValue(props) != Vector3.zero && (Vector3)AccessTools.Field(props.GetType(), "SecHand").GetValue(props) != Vector3.zero, "Rifle hand definitions missing"); checks++;
            Type main = AccessTools.TypeByName("ShowMeYourHands.ShowMeYourHandsMain");
            Check((bool)AccessTools.Field(main, "MeleeAnimationsLoaded").GetValue(null), "Melee Animation integration not detected"); checks++;
            var positions = (IDictionary)AccessTools.Field(main, "weaponLocations").GetValue(null);
            Type positionType = positions.GetType().GetGenericArguments()[1]; positions[gun] = Activator.CreateInstance(positionType, new object[] { pawn.DrawPos, 45f });
            render = drawer.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single(m => m.Name == "DrawHandsOnWeapon" && m.GetParameters().Length == 6);
            diagnostic.Patch(render, transpiler: new HarmonyMethod(typeof(HandsProbe), "CountMeshCalls"));
            countDraws = true; AccessTools.Method(drawer.GetType(), "DrawHandsOnWeapon", new[] { typeof(Pawn) }).Invoke(drawer, new object[] { pawn }); countDraws = false;
            Check(drawCalls >= 2, "Two rifle hands did not reach DrawMesh; calls=" + drawCalls); checks++;
            var mesh = AccessTools.Method(main, "GetMeshFromPawn", new[] { typeof(Pawn) }).Invoke(null, new object[] { pawn }); Check(mesh != null, "No pawn hand mesh after rendering"); checks++;
            foreach (string icon in new[] { "Hand", "OffHand", "HandClean", "OffHandClean", "HandIcon_south" }) { Check(ContentFinder<Texture2D>.Get(icon, false) != null, "Missing hand texture " + icon); checks++; }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        finally { countDraws = false; if (render != null) diagnostic.Unpatch(render, HarmonyPatchType.All, "tmm.diagnostic.hands"); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HandsProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"meshDrawCallsDuringExplicitRender\":" + drawCalls + ",\"visualHandPlacementInspected\":false,\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-hands-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(30, 30, 1200, 740));
        try { Mod().DoSettingsWindowContents(new Rect(0, 0, 1200, 740)); } catch (Exception e) { error = e.ToString(); } finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "HandsUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
