// Controlled inventory swaps and Scribe round trips in a disposable quicktest colony.
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
using Verse.AI;

public sealed class SimpleSidearmsProbe : MapComponent
{
    private bool reported, uiReported;
    public SimpleSidearmsProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool v, string s) { if (!v) throw new Exception(s); }
    private static Verse.Mod GetMod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "PeteTimesSix.SimpleSidearms.SimpleSidearms"); }
    private static object Read(object obj, string name) { return AccessTools.Property(obj.GetType(), name).GetValue(obj, null); }
    private static object Call(object obj, string name, params object[] args) { return AccessTools.Method(obj.GetType(), name).Invoke(obj, args); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-sidearms-probe")) return;
        Pawn pawn = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Violent));
        if (pawn == null) return;
        reported = true; var errors = new List<string>(); int checks = 0, patches = 0;
        ModSettings settings = null; FieldInfo setting = null; object old = null;
        try
        {
            var mod = GetMod(); var assembly = mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "SimpleSidearms") == 1, "Duplicate DLL"); checks++;
            bool original = ModsConfig.IsActive("petetimessix.simplesidearms"); string id = original ? "petetimessix.simplesidearms" : "colonyproject.integrated";
            Check(mod.Content.PackageId == id, "Wrong content pack"); checks++;
            string expected = Path.Combine(mod.Content.RootDir, original ? "v1.6/Assemblies/SimpleSidearms.dll" : "Components/SimpleSidearms/Assemblies/SimpleSidearms.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong DLL origin"); checks++;
            patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
            Check(patches >= 15, "Incomplete gameplay patches"); checks++;
            settings = (ModSettings)AccessTools.Property(mod.GetType(), "Settings").GetValue(null, null);
            setting = AccessTools.Field(settings.GetType(), "ToolAutoSwitch"); old = setting.GetValue(settings); setting.SetValue(settings, true);
            string sp = Path.Combine(GenFilePaths.SaveDataFolderPath, "SidearmsSettings.xml");
            Scribe.saver.InitSaving(sp, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); setting.SetValue(settings, false);
            Scribe.loader.InitLoading(sp); settings.ExposeData(); Scribe.loader.FinalizeLoading();
            Check((bool)setting.GetValue(settings), "Settings Scribe failed"); checks++;
            Type memoryType = AccessTools.TypeByName("SimpleSidearms.rimworld.CompSidearmMemory");
            var memory = (ThingComp)AccessTools.Method(memoryType, "GetMemoryCompForPawn").Invoke(null, new object[] { pawn, true });
            Check(memory != null && pawn.AllComps.Count(c => c.GetType() == memoryType) == 1, "Missing or duplicate pawn memory"); checks++;
            pawn.jobs.StopAll(); pawn.drafter.Drafted = false; pawn.equipment.DestroyAllEquipment(); pawn.inventory.innerContainer.ClearAndDestroyContents();
            var gun = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"));
            var knife = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MeleeWeapon_Knife"), ThingDefOf.Steel);
            pawn.equipment.AddEquipment(gun); Check(pawn.inventory.innerContainer.TryAdd(knife), "Cannot add sidearm"); checks++;
            Call(memory, "InformOfAddedSidearm", gun); Call(memory, "InformOfAddedSidearm", knife);
            Type pairType = AccessTools.TypeByName("SimpleSidearms.rimworld.ThingDefStuffDefPair");
            object gp = Activator.CreateInstance(pairType, new object[] { gun.def, gun.Stuff });
            object kp = Activator.CreateInstance(pairType, new object[] { knife.def, knife.Stuff });
            Call(memory, "SetRangedWeaponTypeAsDefault", gp); Call(memory, "SetMeleeWeaponTypeAsPreferred", kp); Call(memory, "SetWeaponAsForced", gp, false);
            Check(Read(memory, "DefaultRangedWeapon").Equals(gp) && Read(memory, "PreferredMeleeWeapon").Equals(kp), "Weapon preferences not assigned"); checks++;
            string mp = Path.Combine(GenFilePaths.SaveDataFolderPath, "SidearmsMemory.xml");
            Scribe.saver.InitSaving(mp, "Memory"); memory.PostExposeData(); Scribe.saver.FinalizeSaving();
            Call(memory, "SetRangedWeaponTypeAsDefault", kp); Call(memory, "SetWeaponAsForced", kp, false);
            Scribe.loader.InitLoading(mp); memory.PostExposeData(); Scribe.loader.FinalizeLoading();
            Check(Read(memory, "DefaultRangedWeapon").Equals(gp) && Read(memory, "PreferredMeleeWeapon").Equals(kp) && Read(memory, "ForcedWeapon").Equals(gp), "Weapon preference Scribe failed"); checks++;
            Check(((IEnumerable)Read(memory, "RememberedWeapons")).Cast<object>().Contains(kp), "Sidearm memory Scribe failed"); checks++;
            var swap = AccessTools.Method(AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Utilities.WeaponAssingment"), "equipSpecificWeaponFromInventory");
            Check((bool)swap.Invoke(null, new object[] { pawn, knife, false, false }), "Swap to melee failed"); checks++;
            Check(pawn.equipment.Primary == knife && pawn.inventory.innerContainer.Contains(gun) && !pawn.inventory.innerContainer.Contains(knife), "Melee swap lost or duplicated a weapon"); checks++;
            Check((bool)swap.Invoke(null, new object[] { pawn, gun, false, false }), "Swap back to ranged failed"); checks++;
            Check(pawn.equipment.Primary == gun && pawn.inventory.innerContainer.Contains(knife) && !pawn.inventory.innerContainer.Contains(gun), "Ranged swap lost or duplicated a weapon"); checks++;
            Type cs = AccessTools.TypeByName("CommonSense.Sidearms_Utility");
            Check(cs != null && (bool)AccessTools.Property(cs, "Active").GetValue(null, null), "Common Sense failed to detect Sidearms"); checks++;
            Type am = AccessTools.TypeByName("AM.Core");
            var amFlag = AccessTools.Field(am, "IsSimpleSidearmsActive");
            Check((bool)amFlag.GetValue(null), "Melee Animation did not detect Sidearms at startup"); checks++;
            amFlag.SetValue(null, false); AccessTools.Method(am, "CheckForActiveMods").Invoke(null, null);
            Check((bool)amFlag.GetValue(null), "Melee Animation lost Sidearms after rechecking active mods"); checks++;
            foreach (string name in new[] { "EquipSecondary", "ReequipSecondary", "ReequipSecondaryCombat" })
            {
                Job job = new Job(DefDatabase<JobDef>.GetNamed(name)); Check(job.def.driverClass.Assembly == assembly, "Wrong job driver"); checks++;
                string jp = Path.Combine(GenFilePaths.SaveDataFolderPath, name + ".xml"); Scribe.saver.InitSaving(jp, "Job"); Scribe_Deep.Look(ref job, "job"); Scribe.saver.FinalizeSaving(); job = null;
                Scribe.loader.InitLoading(jp); Scribe_Deep.Look(ref job, "job"); Scribe.loader.FinalizeLoading(); Check(job != null && job.def.defName == name, "Job template Scribe failed"); checks++;
            }
            foreach (string icon in new[] { "weapon_held_twohand", "sidearm_default_ranged", "sidearm_preferred_melee", "tuts/rememberPrimary" }) { Check(ContentFinder<Texture2D>.Get(icon, false) != null, "Missing icon " + icon); checks++; }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        finally { if (setting != null) setting.SetValue(settings, old); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SimpleSidearmsProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"automaticCombatSelectionTested\":false,\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-sidearms-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(50, 50, 900, 650));
        try { GetMod().DoSettingsWindowContents(new Rect(0, 0, 900, 650)); } catch (Exception e) { error = e.ToString(); } finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SimpleSidearmsUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
