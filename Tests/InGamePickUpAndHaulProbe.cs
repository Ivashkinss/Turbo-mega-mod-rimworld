// Disposable quicktest colony: run the real gathering and unloading job chain.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

public sealed class HaulState : IExposable
{
    public Thing item;
    public ThingComp comp = (ThingComp)Activator.CreateInstance(AccessTools.TypeByName("PickUpAndHaul.CompHauledToInventory"));
    public void ExposeData() { Scribe_Deep.Look(ref item, "item"); comp.PostExposeData(); }
}

public sealed class PickUpAndHaulProbe : MapComponent
{
    private bool reported, uiReported, initialized, sawBothInInventory, sawUnloadJob;
    private Pawn pawn;
    private Thing knife;
    private ThingComp hauled;
    private Zone_Stockpile stockpile;
    private Stopwatch watch;
    private int checks, patches;
    private readonly List<string> errors = new List<string>();
    public PickUpAndHaulProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private void Check(bool v, string s) { if (!v) throw new Exception(s); checks++; }
    private static Verse.Mod GetMod() { return LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "PickUpAndHaul.Modbase"); }
    private static object Call(object obj, string name, params object[] args) { return AccessTools.Method(obj.GetType(), name).Invoke(obj, args); }
    private static IEnumerable<Thing> Tracked(ThingComp comp) { return ((IEnumerable)Call(comp, "GetHashSet")).Cast<Thing>(); }
    public override void MapComponentUpdate()
    {
        if (reported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-hauling-probe")) return;
        try
        {
            if (!initialized)
            {
                pawn = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Hauling) && !p.WorkTagIsDisabled(WorkTags.Violent));
                if (pawn == null) return;
                initialized = true; watch = Stopwatch.StartNew();
                var mod = GetMod(); var assembly = mod.GetType().Assembly;
                foreach (string name in new[] { "PickUpAndHaul", "IHoldMultipleThings" }) Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == name) == 1, "Duplicate or absent " + name);
                bool original = ModsConfig.IsActive("mehni.pickupandhaul"); string id = original ? "mehni.pickupandhaul" : "colonyproject.integrated";
                Check(mod.Content.PackageId == id, "Wrong owner");
                string expected = Path.Combine(mod.Content.RootDir, original ? "1.6/Assemblies/PickUpAndHaul.dll" : "Components/PickUpAndHaul/Assemblies/PickUpAndHaul.dll");
                Check(string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Wrong DLL path/version");
                patches = Harmony.GetAllPatchedMethods().Count(m => { var p = Harmony.GetPatchInfo(m); return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x => x.PatchMethod.Module.Assembly == assembly); });
                Check(patches >= 8, "Missing hauling hooks");
                Check(mod.SettingsCategory() == "Переноска нескольких предметов", "Untranslated settings title");
                var settings = (ModSettings)AccessTools.Property(mod.GetType(), "Settings").GetValue(null, null);
                FieldInfo field = AccessTools.Field(settings.GetType(), "_allowCorpses"); object old = field.GetValue(null);
                try
                {
                    field.SetValue(null, true); string sp = Path.Combine(GenFilePaths.SaveDataFolderPath, "HaulSettings.xml");
                    Scribe.saver.InitSaving(sp, "Settings"); settings.ExposeData(); Scribe.saver.FinalizeSaving(); field.SetValue(null, false);
                    Scribe.loader.InitLoading(sp); settings.ExposeData(); Scribe.loader.FinalizeLoading(); Check((bool)field.GetValue(null), "Settings Scribe failed");
                }
                finally { field.SetValue(null, old); }
                hauled = pawn.AllComps.Single(c => c.GetType().FullName == "PickUpAndHaul.CompHauledToInventory");
                Check(pawn.def.comps.Count(c => c.compClass == hauled.GetType()) == 1, "Duplicate pawn comp");
                var state = new HaulState { item = ThingMaker.MakeThing(ThingDefOf.WoodLog) }; Call(state.comp, "RegisterHauledItem", state.item);
                string hp = Path.Combine(GenFilePaths.SaveDataFolderPath, "HaulState.xml");
                Scribe.saver.InitSaving(hp, "HaulState"); Scribe_Deep.Look(ref state, "state"); Scribe.saver.FinalizeSaving(); state = null;
                Scribe.loader.InitLoading(hp); Scribe_Deep.Look(ref state, "state"); Scribe.loader.FinalizeLoading();
                Check(state != null && Tracked(state.comp).Single() == state.item, "Hauled item reference Scribe failed");
                foreach (string name in new[] { "HaulToInventory", "UnloadYourHauledInventory" })
                {
                    Job template = new Job(DefDatabase<JobDef>.GetNamed(name)); Check(template.def.driverClass.Assembly == assembly, "Wrong driver");
                    string jp = Path.Combine(GenFilePaths.SaveDataFolderPath, name + ".xml"); Scribe.saver.InitSaving(jp, "Job"); Scribe_Deep.Look(ref template, "job"); Scribe.saver.FinalizeSaving(); template = null;
                    Scribe.loader.InitLoading(jp); Scribe_Deep.Look(ref template, "job"); Scribe.loader.FinalizeLoading(); Check(template != null && template.def.defName == name, "Job template Scribe failed");
                }
                foreach (var other in map.mapPawns.FreeColonistsSpawned.Where(p => p != pawn)) { other.jobs.StopAll(); other.drafter.Drafted = true; }
                pawn.jobs.StopAll(); pawn.drafter.Drafted = false; pawn.equipment.DestroyAllEquipment(); pawn.inventory.innerContainer.ClearAndDestroyContents();
                knife = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MeleeWeapon_Knife"), ThingDefOf.Steel); Check(pawn.inventory.innerContainer.TryAdd(knife), "Cannot add personal sidearm");
                var gun = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol")); pawn.equipment.AddEquipment(gun);
                var memory = pawn.AllComps.Single(c => c.GetType().FullName == "SimpleSidearms.rimworld.CompSidearmMemory"); Call(memory, "InformOfAddedSidearm", knife);
                object gunPair = Activator.CreateInstance(AccessTools.TypeByName("SimpleSidearms.rimworld.ThingDefStuffDefPair"), new object[] { gun.def, gun.Stuff });
                Call(memory, "SetRangedWeaponTypeAsDefault", gunPair); Call(memory, "SetWeaponAsForced", gunPair, false);
                foreach (var thing in map.listerHaulables.ThingsPotentiallyNeedingHauling().ToList()) thing.SetForbidden(true, false);
                // Only use clear walkable cells outside existing zones/buildings.
                var cells = CellRect.CenteredOn(pawn.Position, 9).ClipInsideMap(map).Cells.Where(c => c.Standable(map) && c.GetThingList(map).Count == 0 && map.zoneManager.ZoneAt(c) == null && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly)).OrderBy(c => c.DistanceToSquared(pawn.Position)).ToList();
                Check(cells.Count >= 12, "No clear test area");
                stockpile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager); stockpile.settings.filter.SetDisallowAll(); stockpile.settings.filter.SetAllow(ThingDefOf.Steel, true); stockpile.settings.filter.SetAllow(ThingDefOf.WoodLog, true); stockpile.settings.Priority = StoragePriority.Critical;
                map.zoneManager.RegisterZone(stockpile); foreach (var cell in cells.Take(6)) stockpile.AddCell(cell);
                Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel), wood = ThingMaker.MakeThing(ThingDefOf.WoodLog); steel.stackCount = 10; wood.stackCount = 10;
                IntVec3 sourceCell = cells[cells.Count - 1];
                IntVec3 secondCell = cells.Skip(6).Where(c => c != sourceCell).OrderBy(c => c.DistanceToSquared(sourceCell)).First();
                GenSpawn.Spawn(steel, sourceCell, map); GenSpawn.Spawn(wood, secondCell, map); steel.SetForbidden(false, false); wood.SetForbidden(false, false);
                var giver = (WorkGiver_Scanner)DefDatabase<WorkGiverDef>.GetNamed("HaulToInventory").Worker;
                Check(giver.GetType().Assembly == assembly, "Wrong work giver");
                Job job = giver.JobOnThing(pawn, steel, true); Check(job != null && job.def.defName == "HaulToInventory", "No gather job");
                Check(job.targetQueueA.Any(t => t.Thing == steel) && job.targetQueueA.Any(t => t.Thing == wood), "Gather queue does not include both resource types");
                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            }
            var inventory = pawn.inventory.innerContainer;
            if (inventory.Any(t => t.def == ThingDefOf.Steel) && inventory.Any(t => t.def == ThingDefOf.WoodLog)) sawBothInInventory = true;
            if (pawn.CurJobDef != null && pawn.CurJobDef.defName == "UnloadYourHauledInventory") sawUnloadJob = true;
            var stored = stockpile.Cells.SelectMany(c => c.GetThingList(map)).ToList();
            if (stored.Where(t => t.def == ThingDefOf.Steel).Sum(t => t.stackCount) == 10 && stored.Where(t => t.def == ThingDefOf.WoodLog).Sum(t => t.stackCount) == 10)
            {
                Check(sawBothInInventory, "Never carried both resource types in inventory together"); Check(sawUnloadJob, "Unload job did not run");
                Check(!inventory.Any(t => t.def == ThingDefOf.Steel || t.def == ThingDefOf.WoodLog), "Hauled resources remain in inventory");
                Check(Tracked(hauled).Count() == 0, "Unloaded things remain registered");
                Check(inventory.Contains(knife), "Personal sidearm was unloaded");
                var memory = pawn.AllComps.Single(c => c.GetType().FullName == "SimpleSidearms.rimworld.CompSidearmMemory");
                object knifePair = Activator.CreateInstance(AccessTools.TypeByName("SimpleSidearms.rimworld.ThingDefStuffDefPair"), new object[] { knife.def, knife.Stuff });
                Check(((IEnumerable)AccessTools.Property(memory.GetType(), "RememberedWeapons").GetValue(memory, null)).Cast<object>().Contains(knifePair), "Sidearm memory lost");
                Finish();
            }
            else if (watch.Elapsed.TotalSeconds > 50) throw new Exception("Haul cycle timed out; current job=" + pawn.CurJobDef + "; bothInInventory=" + sawBothInInventory + "; unloadJob=" + sawUnloadJob);
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    private void Finish()
    {
        reported = true;
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "PickUpAndHaulProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"patchedMethods\":" + patches + ",\"twoResourceTypesCarriedTogether\":" + (sawBothInInventory ? "true" : "false") + ",\"realUnloadJobObserved\":" + (sawUnloadJob ? "true" : "false") + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}");
    }
    public override void MapComponentOnGUI()
    {
        if (uiReported || Find.CurrentMap != map || !Environment.GetCommandLineArgs().Contains("-tmm-hauling-probe") || Event.current.type != EventType.Repaint) return;
        uiReported = true; string error = null; GUI.BeginGroup(new Rect(50, 50, 900, 650));
        try { GetMod().DoSettingsWindowContents(new Rect(0, 0, 900, 650)); } catch (Exception e) { error = e.ToString(); } finally { GUI.EndGroup(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "PickUpAndHaulUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
