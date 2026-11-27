using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public static class UniformLoadoutTestStartup
{
    public const string SaveName = "TMM-UniformLoadout";
    public const string Fresh = "-tmm-uniform-loadout-probe";
    public const string Reload = "-tmm-uniform-loadout-reload";

    static UniformLoadoutTestStartup()
    {
        if (Environment.GetCommandLineArgs().Contains(Reload))
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame(SaveName));
    }
}

public sealed class UniformLoadoutProbe : MapComponent
{
    private readonly List<string> checks = new List<string>();
    private readonly List<string> failures = new List<string>();
    private readonly bool reload = Environment.GetCommandLineArgs().Contains(UniformLoadoutTestStartup.Reload);
    private static readonly Assembly Mod = typeof(ColonyRoles).Assembly;
    private static readonly BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
    private bool done;
    private Pawn pawn;
    private int frames;

    public UniformLoadoutProbe(Map map) : base(map) { }

    public override void MapComponentUpdate()
    {
        if (done || !map.IsPlayerHome || (!reload && !Environment.GetCommandLineArgs().Contains(UniformLoadoutTestStartup.Fresh))) return;
        pawn = map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
        if (pawn == null || ++frames < 5) return;
        try
        {
            if (frames == 5) Scan("start");
            if (frames < 30) return;
            if (frames == 30) { Scan("before"); Prepare(); Scan("prepared"); Extra(); Scan("after"); }
            // One window at a time for the pictures: the editor, the policy window with its button, the ordinary equipment tab.
            if (!reload && frames == 40) Find.WindowStack.Add(editorWindow);
            if (!reload && frames == 60) Shot("UniformEditor.png");
            if (!reload && frames == 70) { Find.WindowStack.TryRemove(editorWindow, false); Find.WindowStack.Add(new Dialog_ManageApparelPolicies(policyShown)); }
            if (!reload && frames == 100) Shot("UniformPolicies.png");
            if (!reload && frames == 110) { foreach (Window w in Find.WindowStack.Windows.Where(x => x is Dialog_ManageApparelPolicies).ToList()) Find.WindowStack.TryRemove(w, false); Find.WindowStack.Add(new EquipmentTestWindow(dressed)); }
            if (!reload && frames == 140) Shot("UniformEquipmentTab.png");
            if (frames < (reload ? 60 : 160)) return;
            if (!reload) { Scan("save"); GameDataSaveLoader.SaveGame(UniformLoadoutTestStartup.SaveName); }
            Finish();
        }
        catch (Exception exception)
        {
            failures.Add(exception.ToString());
            Finish();
        }
    }

    private void Prepare()
    {
        ColonyRoleDef role = DefDatabase<ColonyRoleDef>.GetNamed("TMM_Role_trooper");
        ColonyRank rank = role.ranks[0];
        ApparelPolicy policy = ColonyRankUniforms.EnsurePolicyFor(role, rank);
        ThingDef coat = ThingDef.Named("Apparel_Parka");
        ThingDef rifle = ThingDef.Named("Gun_AssaultRifle");
        ThingDef mace = ThingDef.Named("MeleeWeapon_Mace");
        Type editor = Mod.GetType("TurboMegaMod.Dialog_UniformLoadout", true);
        Type stats = Mod.GetType("TurboMegaMod.EquipmentLoadoutStats", true);
        if (!reload)
        {
            var window = (Window)Activator.CreateInstance(editor, Instance, null, new object[] { role, rank, policy, pawn }, null);
            editor.GetMethod("ChangeApparel", Instance).Invoke(window, new object[] { new Func<ThingDef, bool>(def => def == coat), coat });
            var weapons = (List<ThingDef>)editor.GetField("weapons", Instance).GetValue(window);
            weapons.Clear();
            weapons.AddRange(new[] { rifle, mace });
            editor.GetMethod("RefreshStats", Instance).Invoke(window, null);
            object planned = editor.GetField("stats", Instance).GetValue(window);
            Check("PlannedColdAndHeat", Number(stats, planned, "Cold") < Number(stats, planned, "Heat"));
            Check("PlannedMeleeDps", Number(stats, planned, "Melee") > 0f);
            Check("PlannedRangedDps", Number(stats, planned, "Ranged") > 0f);
            editor.GetMethod("Save", Instance).Invoke(window, null);
            Find.WindowStack.Add(window);
            editorWindow = window; policyShown = policy;
        }

        Check("RankApparelFilter", policy.filter.Allows(coat));
        Type storage = Mod.GetType("TurboMegaMod.RankWeaponPreferences", true);
        var uniform = (ColonyRankUniform)storage.GetMethod("Get", Static).Invoke(null, new object[] { role, rank });
        Check("RankWeaponPriority", uniform.GetWeapons().SequenceEqual(new[] { rifle, mace }));
        Check("RankApparelAssigned", map.mapPawns.FreeColonistsSpawned.Any(p => p.outfits.CurrentApparelPolicy == policy));
        object worn = stats.GetMethod("WornBy", Static).Invoke(null, new object[] { pawn });
        Check("WornTemperatureMatchesGame", Mathf.Approximately(Number(stats, worn, "Cold"), pawn.GetStatValue(StatDefOf.ComfyTemperatureMin)) &&
            Mathf.Approximately(Number(stats, worn, "Heat"), pawn.GetStatValue(StatDefOf.ComfyTemperatureMax)));
        Check("WornArmorHasAllTypes", new[] { "Sharp", "Blunt", "Thermal" }.All(field => Number(stats, worn, field) >= 0f));
        MethodInfo filter = AccessTools.Method(typeof(Dialog_ManageApparelPolicies), "DoContentsRect");
        Patches patches = Harmony.GetPatchInfo(filter);
        Check("AssignmentMenuEditorInstalled", patches != null && patches.Owners.Contains("colonyproject.integrated.uniformeditor"));
    }

    // Relations to people that no holder keeps: said with the moment they appear, so that a broken reference is not blamed on the wrong step.
    private static void Scan(string when)
    {
        HashSet<Pawn> known = new HashSet<Pawn>(PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead);
        foreach (Pawn p in Find.WorldPawns.AllPawnsAliveOrDead) known.Add(p);
        List<Thing> held = new List<Thing>();
        ThingOwnerUtility.GetAllThingsRecursively(Find.World, held, true, null);
        foreach (Map m in Find.Maps) ThingOwnerUtility.GetAllThingsRecursively(m, held, true, null);
        foreach (Thing t in held) { Pawn hp = t as Pawn; Corpse c = t as Corpse; if (hp != null) known.Add(hp); if (c != null && c.InnerPawn != null) known.Add(c.InnerPawn); }
        foreach (Pawn p in known.ToList())
            if (p.relations != null)
                foreach (DirectPawnRelation r in p.relations.DirectRelations)
                    if (r.otherPawn != null && !known.Contains(r.otherPawn))
                        Log.Message("[TMM probe] unsaved relation at " + when + ": " + p.ThingID + " " + p.kindDef + " " + p.Faction + " -> " + r.def + " " + r.otherPawn.ThingID + " " + r.otherPawn.kindDef + " " + r.otherPawn.Faction + " discarded=" + r.otherPawn.Discarded + " destroyed=" + r.otherPawn.Destroyed + " dead=" + r.otherPawn.Dead + " holder=" + r.otherPawn.ParentHolder);
    }
    private Window editorWindow;
    private ApparelPolicy policyShown;
    private Pawn dressed;
    private string dressedId = "", manualId = "";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref dressedId, "ulDressed", "");
        Scribe_Values.Look(ref manualId, "ulManual", "");
    }

    private static void Shot(string name)
    {
        AccessTools.Method(AccessTools.TypeByName("UnityEngine.ScreenCapture"), "CaptureScreenshot", new Type[] { typeof(string) })
            .Invoke(null, new object[] { Path.Combine(GenFilePaths.SaveDataFolderPath, name) });
    }

    private Pawn Colonist(string nick)
    {
        Pawn p;
        do { p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false)); }
        while (p.WorkTagIsDisabled(WorkTags.Violent) || p.DevelopmentalStage != DevelopmentalStage.Adult);
        p.Name = new NameTriple("TMM", nick, "Loadout");
        p.apparel.DestroyAll(DestroyMode.Vanish);
        p.equipment.DestroyAllEquipment(DestroyMode.Vanish);
        GenSpawn.Spawn(p, CellFinder.RandomClosewalkCellNear(pawn.Position, map, 8), map);
        return p;
    }

    private static Thing Make(string def)
    {
        ThingDef d = ThingDef.Named(def);
        Thing thing = ThingMaker.MakeThing(d, d.MadeFromStuff ? GenStuff.DefaultStuffFor(d) : null);
        CompQuality quality = thing.TryGetComp<CompQuality>();
        if (quality != null) quality.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        return thing;
    }

    private void Near(string name, float expected, float actual)
    {
        bool ok = Mathf.Abs(expected - actual) <= Mathf.Max(0.02f, Mathf.Abs(expected) * 0.01f);
        Check(name + (ok ? "" : " expected " + expected + " actual " + actual), ok);
    }

    private void Extra()
    {
        Type editor = Mod.GetType("TurboMegaMod.Dialog_UniformLoadout", true);
        Type stats = Mod.GetType("TurboMegaMod.EquipmentLoadoutStats", true);
        Type storage = Mod.GetType("TurboMegaMod.RankWeaponPreferences", true);
        ColonyRoleDef role = DefDatabase<ColonyRoleDef>.GetNamed("TMM_Role_trooper");
        ColonyRank rank = role.ranks[1];
        if (reload)
        {
            Pawn kept = map.mapPawns.AllPawns.FirstOrDefault(p => p.ThingID == dressedId), manual = map.mapPawns.AllPawns.FirstOrDefault(p => p.ThingID == manualId);
            ApparelPolicy policyKept = ColonyRankUniforms.EnsurePolicyFor(role, rank);
            Check("ReloadSecondRankFilter", policyKept.filter.Allows(ThingDef.Named("Apparel_Duster")) && !policyKept.filter.Allows(ThingDef.Named("Apparel_FlakJacket")));
            Check("ReloadSecondRankWeapons", ((ColonyRankUniform)storage.GetMethod("Get", Static).Invoke(null, new object[] { role, rank })).GetWeapons().SequenceEqual(new[] { ThingDef.Named("Gun_SniperRifle") }));
            Check("ReloadWornKept", kept != null && kept.apparel.WornApparel.Any(x => x.def.defName == "Apparel_FlakVest") && kept.equipment.Primary != null);
            Check("ReloadManualPolicyKept", manual != null && manual.outfits.CurrentApparelPolicy.label == "TMM probe manual");
            return;
        }

        // Closing without saving drops the draft.
        ApparelPolicy policy = ColonyRankUniforms.EnsurePolicyFor(role, rank);
        List<ThingDef> allowedBefore = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsApparel && policy.filter.Allows(d)).ToList();
        List<ThingDef> weaponsBefore = ((ColonyRankUniform)storage.GetMethod("Get", Static).Invoke(null, new object[] { role, rank })).GetWeapons();
        Func<Window> open = () => (Window)Activator.CreateInstance(editor, Instance, null, new object[] { role, rank, policy, null }, null);
        Window draft = open();
        Find.WindowStack.Add(draft);
        ThingDef duster = ThingDef.Named("Apparel_Duster");
        ThingDef sniper = ThingDef.Named("Gun_SniperRifle");
        Func<ThingDef, bool> shell = d => (bool)editor.GetMethod("Matches", Static).Invoke(null, new object[] { d, "shell" });
        editor.GetMethod("ChangeApparel", Instance).Invoke(draft, new object[] { shell, duster });
        ((List<ThingDef>)editor.GetField("weapons", Instance).GetValue(draft)).Clear();
        Find.WindowStack.TryRemove(draft, false);
        Check("CancelKeepsFilter", DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsApparel && policy.filter.Allows(d)).SequenceEqual(allowedBefore));
        Check("CancelKeepsWeapons", ((ColonyRankUniform)storage.GetMethod("Get", Static).Invoke(null, new object[] { role, rank })).GetWeapons().SequenceEqual(weaponsBefore));

        // The real slot menu: its option changes the draft, the empty option clears the slot.
        Window chosen = open();
        ThingDef picked = null; bool called = false;
        editor.GetMethod("Pick", Instance).Invoke(chosen, new object[] { shell, true, new Action<ThingDef>(d => { picked = d; called = true; }) });
        FloatMenu menu = Find.WindowStack.Windows.OfType<FloatMenu>().LastOrDefault();
        List<FloatMenuOption> options = menu == null ? null : (List<FloatMenuOption>)AccessTools.Field(typeof(FloatMenu), "options").GetValue(menu);
        FloatMenuOption dusterOption = options == null ? null : options.FirstOrDefault(x => x.Label == duster.LabelCap.ToString());
        Check("SlotMenuListsItems", dusterOption != null && options.Count > 3);
        if (dusterOption != null) dusterOption.action();
        Check("SlotMenuChoosesItem", called && picked == duster);
        options[0].action();
        Check("SlotMenuClearsSlot", picked == null);
        if (menu != null) Find.WindowStack.TryRemove(menu, false);

        // Save the second rank: duster instead of the flak jacket, sniper rifle only.
        editor.GetMethod("ChangeApparel", Instance).Invoke(chosen, new object[] { shell, duster });
        List<ThingDef> chosenApparel = (List<ThingDef>)editor.GetField("apparel", Instance).GetValue(chosen);
        chosenApparel.Remove(ThingDef.Named("Apparel_FlakJacket"));
        List<ThingDef> chosenWeapons = (List<ThingDef>)editor.GetField("weapons", Instance).GetValue(chosen);
        chosenWeapons.Clear(); chosenWeapons.Add(sniper);
        editor.GetMethod("Save", Instance).Invoke(chosen, null);
        Check("SecondRankSaved", policy.filter.Allows(duster) && !policy.filter.Allows(ThingDef.Named("Apparel_FlakJacket")));

        // Planned numbers against the same stats counted here by hand.
        ThingDef vest = ThingDef.Named("Apparel_FlakVest"), helmet = ThingDef.Named("Apparel_SimpleHelmet"), rifle = ThingDef.Named("Gun_AssaultRifle"), mace = ThingDef.Named("MeleeWeapon_Mace");
        object planned = stats.GetMethod("Planned", Static).Invoke(null, new object[] { null, new List<ThingDef> { vest, helmet }, new List<ThingDef> { rifle, mace } });
        Func<ThingDef, StatDef, float> value = (d, s) => s.Worker.GetValue(StatRequest.For(d, d.MadeFromStuff ? GenStuff.DefaultStuffFor(d) : null, QualityCategory.Normal), true);
        Near("PlannedCold", ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) - value(vest, StatDefOf.Insulation_Cold) - value(helmet, StatDefOf.Insulation_Cold), Number(stats, planned, "Cold"));
        Near("PlannedHeat", ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax) + value(vest, StatDefOf.Insulation_Heat) + value(helmet, StatDefOf.Insulation_Heat), Number(stats, planned, "Heat"));
        foreach (var pair in new[] { Tuple.Create("Sharp", StatDefOf.ArmorRating_Sharp), Tuple.Create("Blunt", StatDefOf.ArmorRating_Blunt), Tuple.Create("Thermal", StatDefOf.ArmorRating_Heat) })
        {
            float natural = ThingDefOf.Human.GetStatValueAbstract(pair.Item2), total = 0f;
            foreach (BodyPartRecord part in BodyDefOf.Human.AllParts)
            {
                float open1 = 1f - Mathf.Clamp01(natural / 2f);
                foreach (ThingDef d in new[] { vest, helmet }) if (d.apparel.CoversBodyPart(part)) open1 *= 1f - Mathf.Clamp01(value(d, pair.Item2) / 2f);
                total += part.coverageAbs * (1f - open1);
            }
            Near("Planned" + pair.Item1, Mathf.Clamp(total * 2f, 0f, 2f), Number(stats, planned, pair.Item1));
        }
        VerbProperties verb = rifle.Verbs.First(v => v.LaunchesProjectile);
        Near("PlannedRanged", verb.defaultProjectile.projectile.GetDamageAmount(rifle, null, null) * verb.burstShotCount /
            (verb.warmupTime + value(rifle, StatDefOf.RangedWeapon_Cooldown) + (verb.burstShotCount - 1) * verb.ticksBetweenBurstShots / 60f), Number(stats, planned, "Ranged"));
        float maceDps = value(mace, StatDefOf.MeleeWeapon_AverageDPS), rifleDps = value(rifle, StatDefOf.MeleeWeapon_AverageDPS);
        Near("PlannedMelee", Mathf.Max(maceDps, rifleDps), Number(stats, planned, "Melee"));

        // Worn items, an installed module and the bonus list.
        dressed = Colonist("Dressed");
        dressedId = dressed.ThingID;
        dressed.apparel.Wear((Apparel)Make("Apparel_FlakVest"), false);
        dressed.apparel.Wear((Apparel)Make("Apparel_AdvancedHelmet"), false);
        dressed.apparel.Wear((Apparel)Make("Apparel_Pants"), false);
        ThingWithComps gun = (ThingWithComps)Make("Gun_AssaultRifle");
        dressed.equipment.AddEquipment(gun);
        Near("WornCold", dressed.GetStatValue(StatDefOf.ComfyTemperatureMin), Number(stats, stats.GetMethod("WornBy", Static).Invoke(null, new object[] { dressed }), "Cold"));
        object bare = stats.GetMethod("WornBy", Static).Invoke(null, new object[] { dressed });
        int bonusesBefore = ((List<string>)stats.GetField("Bonuses", Instance).GetValue(bare)).Count;
        Check("WornRangedPositive", Number(stats, bare, "Ranged") > 0f && Number(stats, bare, "Melee") > 0f && Number(stats, bare, "Sharp") > 0f);
        CompWeaponMods mods = gun.GetComp<CompWeaponMods>();
        if (mods != null) mods.InstalledMods.Add(new InstalledModEntry { modDefName = "TMM_Mod_HolyWaterTank", charges = 12, maxCharges = 30 });
        List<string> bonuses = (List<string>)stats.GetField("Bonuses", Instance).GetValue(stats.GetMethod("WornBy", Static).Invoke(null, new object[] { dressed }));
        Check("ModuleInBonusList" + (bonuses.Count == bonusesBefore + 1 ? "" : " " + string.Join(" / ", bonuses.ToArray())), mods != null && bonuses.Count == bonusesBefore + 1);

        // Weapons with their own charge and damage code: the block shows them and follows an installed and removed module.
        foreach (string special in new[] { "TMM_Legendary_TheColt", "TMM_Legendary_LorenzosArtifact" })
        {
            Pawn holder = Colonist("Special" + special.Length);
            LegendaryRegistry.Instance.Unregister(ThingDef.Named(special));
            ThingWithComps weapon = (ThingWithComps)ThingMaker.MakeThing(ThingDef.Named(special));
            holder.equipment.AddEquipment(weapon);
            object shown = stats.GetMethod("WornBy", Static).Invoke(null, new object[] { holder });
            float dps = Number(stats, shown, "Ranged");
            int count = ((List<string>)stats.GetField("Bonuses", Instance).GetValue(shown)).Count;
            Check("SpecialRanged_" + special + "=" + dps, dps > 0f);
            CompWeaponMods slot = weapon.GetComp<CompWeaponMods>();
            if (slot != null)
            {
                InstalledModEntry entry = new InstalledModEntry { modDefName = "TMM_Mod_HolyWaterTank", charges = 5, maxCharges = 30 };
                slot.InstalledMods.Add(entry);
                int with = ((List<string>)stats.GetField("Bonuses", Instance).GetValue(stats.GetMethod("WornBy", Static).Invoke(null, new object[] { holder }))).Count;
                slot.InstalledMods.Remove(entry);
                int without = ((List<string>)stats.GetField("Bonuses", Instance).GetValue(stats.GetMethod("WornBy", Static).Invoke(null, new object[] { holder }))).Count;
                Check("SpecialModule_" + special, with == count + 1 && without == count);
            }
            holder.Destroy();
        }
        // Equipping from storage follows the saved weapon list of the rank.
        Pawn armed = Colonist("Armed");
        ColonyRoles.Instance.Assign(armed, role);
        ColonyRoles.Instance.For(armed).rank = 1;
        Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(zone);
        List<IntVec3> cells = GenRadial.RadialCellsAround(armed.Position, 30f, true).Where(c => c.InBounds(map) && c.Standable(map) && c.GetEdifice(map) == null && map.zoneManager.ZoneAt(c) == null && c.GetFirstItem(map) == null && armed.CanReach(c, PathEndMode.OnCell, Danger.Deadly)).Take(2).ToList();
        foreach (IntVec3 c in cells) zone.AddCell(c);
        Thing storedSniper = Make("Gun_SniperRifle"), storedBolt = Make("Gun_HeavySMG");
        GenSpawn.Spawn(storedSniper, cells[0], map); GenSpawn.Spawn(storedBolt, cells[1], map);
        storedSniper.SetForbidden(false, false); storedBolt.SetForbidden(false, false);
        WorkGiver_EquipRankWeapon giver = new WorkGiver_EquipRankWeapon();
        Check("StorageWeaponOfListTaken", !giver.ShouldSkip(armed) && giver.HasJobOnThing(armed, storedSniper) && giver.JobOnThing(armed, storedSniper).def == JobDefOf.Equip);
        Check("StorageWeaponOutsideListIgnored", !giver.HasJobOnThing(armed, storedBolt));

        // A policy the player set by hand survives an ordinary assignment.
        Pawn manualPawn = Colonist("Manual");
        manualId = manualPawn.ThingID;
        ApparelPolicy own = Current.Game.outfitDatabase.MakeNewOutfit();
        own.label = "TMM probe manual";
        manualPawn.outfits.CurrentApparelPolicy = own;
        ColonyRoles.Instance.Assign(manualPawn, role);
        Check("ManualPolicyKeptByAssign", manualPawn.outfits.CurrentApparelPolicy == own);

        // Russian rows of the stats block inside the widths both windows give them.
        GameFont font = Text.Font;
        Text.Font = GameFont.Tiny;
        string armor = "TMM_Loadout_Armor".Translate(1.25f.ToStringPercent(), 0.75f.ToStringPercent(), 1.1f.ToStringPercent());
        string temperature = "TMM_Loadout_Temperature".Translate((-40f).ToStringTemperature("F0"), 55f.ToStringTemperature("F0"));
        Check("RowsFitEditor armor=" + Text.CalcSize(armor).x + " temp=" + Text.CalcSize(temperature).x, Text.CalcSize(armor).x <= 284f && Text.CalcSize(temperature).x <= 284f);
        Text.Font = font;
    }

    private static float Number(Type type, object stats, string member)
    {
        return (float)type.GetField(member, Instance).GetValue(stats);
    }

    private void Check(string name, bool passed)
    {
        checks.Add("{\"name\":\"" + name + "\",\"passed\":" + (passed ? "true" : "false") + "}");
        if (!passed) failures.Add(name);
    }

    private void Finish()
    {
        done = true;
        string result = failures.Count == 0 ? "true" : "false";
        string report = "{\"passed\":" + result + ",\"checks\":[" + string.Join(",", checks.ToArray()) +
            "],\"errors\":[" + string.Join(",", failures.Select(error => "\"" + error.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n") + "\"").ToArray()) + "]}";
        string prefix = reload ? "UniformLoadoutReload" : "UniformLoadout";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, prefix + "Probe.json"), report);
        if (!reload) File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, prefix + "UiProbe.json"), "{\"passed\":" + result + "}");
    }

    private sealed class EquipmentTestWindow : Window
    {
        private readonly ITab_TMM_Equipment tab = new ITab_TMM_Equipment();
        private readonly Pawn pawn;

        internal EquipmentTestWindow(Pawn pawn) { this.pawn = pawn; doCloseX = true; }

        public override Vector2 InitialSize { get { return new Vector2(560f, 600f); } }

        public override void DoWindowContents(Rect rect) { tab.DebugDrawInOnGUI(rect, pawn); }
    }
}
