using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    internal struct EquipmentLoadoutEntry
    {
        internal readonly ThingDef Def;
        internal readonly Thing Item;
        internal readonly ThingDef Stuff;

        internal EquipmentLoadoutEntry(Thing item)
        {
            Def = item.def;
            Item = item;
            Stuff = item.Stuff;
        }

        internal EquipmentLoadoutEntry(ThingDef def)
        {
            Def = def;
            Item = null;
            Stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
        }

        internal float Stat(StatDef stat)
        {
            return Item != null ? Item.GetStatValue(stat) : stat.Worker.GetValue(StatRequest.For(Def, Stuff, QualityCategory.Normal), true);
        }
    }

    internal sealed class EquipmentLoadoutStats
    {
        internal float Cold;
        internal float Heat;
        internal float Sharp;
        internal float Blunt;
        internal float Thermal;
        internal float Melee;
        internal float Ranged;
        internal readonly List<string> Bonuses = new List<string>();

        internal static EquipmentLoadoutStats WornBy(Pawn pawn)
        {
            List<EquipmentLoadoutEntry> clothing = pawn.apparel == null
                ? new List<EquipmentLoadoutEntry>()
                : pawn.apparel.WornApparel.Select(item => new EquipmentLoadoutEntry(item)).ToList();
            ThingWithComps primary = pawn.equipment == null ? null : pawn.equipment.Primary;
            return Calculate(pawn, clothing, primary == null
                ? new List<EquipmentLoadoutEntry>()
                : new List<EquipmentLoadoutEntry> { new EquipmentLoadoutEntry(primary) }, true);
        }

        internal static EquipmentLoadoutStats Planned(Pawn pawn, IEnumerable<ThingDef> clothing, IEnumerable<ThingDef> weapons)
        {
            return Calculate(pawn, clothing.Select(def => new EquipmentLoadoutEntry(def)).ToList(),
                weapons.Select(def => new EquipmentLoadoutEntry(def)).ToList(), false);
        }

        private static EquipmentLoadoutStats Calculate(Pawn pawn, List<EquipmentLoadoutEntry> clothing,
            List<EquipmentLoadoutEntry> weapons, bool worn)
        {
            ThingDef race = pawn == null ? ThingDefOf.Human : pawn.def;
            var stats = new EquipmentLoadoutStats();
            stats.Cold = worn ? pawn.GetStatValue(StatDefOf.ComfyTemperatureMin)
                : race.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) - clothing.Sum(item => item.Stat(StatDefOf.Insulation_Cold));
            stats.Heat = worn ? pawn.GetStatValue(StatDefOf.ComfyTemperatureMax)
                : race.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax) + clothing.Sum(item => item.Stat(StatDefOf.Insulation_Heat));
            stats.Sharp = OverallArmor(race.race.body, clothing, StatDefOf.ArmorRating_Sharp,
                worn ? pawn.GetStatValue(StatDefOf.ArmorRating_Sharp) : race.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp));
            stats.Blunt = OverallArmor(race.race.body, clothing, StatDefOf.ArmorRating_Blunt,
                worn ? pawn.GetStatValue(StatDefOf.ArmorRating_Blunt) : race.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt));
            stats.Thermal = OverallArmor(race.race.body, clothing, StatDefOf.ArmorRating_Heat,
                worn ? pawn.GetStatValue(StatDefOf.ArmorRating_Heat) : race.GetStatValueAbstract(StatDefOf.ArmorRating_Heat));

            if (!worn || !pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                if (worn) stats.Melee = pawn.GetStatValue(StatDefOf.MeleeDPS);
                foreach (EquipmentLoadoutEntry weapon in weapons)
                {
                    if (!worn && weapon.Def.tools != null && weapon.Def.tools.Count > 0)
                        stats.Melee = Mathf.Max(stats.Melee, weapon.Stat(StatDefOf.MeleeWeapon_AverageDPS));
                    stats.Ranged = Mathf.Max(stats.Ranged, RangedDps(weapon, worn ? pawn : null));
                }
            }

            foreach (EquipmentLoadoutEntry item in clothing) AddBonuses(item, stats.Bonuses);
            foreach (EquipmentLoadoutEntry item in weapons) AddBonuses(item, stats.Bonuses);
            return stats;
        }

        // Same coverage and layer calculation as the game's Gear tab.
        private static float OverallArmor(BodyDef body, List<EquipmentLoadoutEntry> clothing, StatDef armor, float natural)
        {
            float[] protection = clothing.Select(item => Mathf.Clamp01(item.Stat(armor) / 2f)).ToArray();
            float unprotected = 1f - Mathf.Clamp01(natural / 2f);
            float total = 0f;
            foreach (BodyPartRecord part in body.AllParts)
            {
                float partUnprotected = unprotected;
                for (int i = 0; i < clothing.Count; i++)
                    if (clothing[i].Def.apparel.CoversBodyPart(part)) partUnprotected *= 1f - protection[i];
                total += part.coverageAbs * (1f - partUnprotected);
            }
            return Mathf.Clamp(total * 2f, 0f, 2f);
        }

        internal static float RangedDps(EquipmentLoadoutEntry weapon, Pawn shooter)
        {
            if (!weapon.Def.IsRangedWeapon || weapon.Def.Verbs == null) return 0f;
            float best = 0f;
            foreach (VerbProperties verb in weapon.Def.Verbs)
            {
                ThingDef shell = verb.defaultProjectile;
                if (verb.IsMeleeAttack || !verb.LaunchesProjectile || shell == null || shell.projectile == null) continue;
                float perShot = weapon.Item == null ? shell.projectile.GetDamageAmount(weapon.Def, weapon.Stuff, null)
                    : shell.projectile.GetDamageAmount(weapon.Item, null);
                float cooldown = weapon.Item == null ? weapon.Stat(StatDefOf.RangedWeapon_Cooldown)
                    : verb.AdjustedCooldown(null, shooter, weapon.Item);
                int shots = Mathf.Max(1, verb.burstShotCount);
                float seconds = verb.warmupTime + cooldown + (shots - 1) * verb.ticksBetweenBurstShots / 60f;
                if (seconds > 0f) best = Mathf.Max(best, perShot * shots / seconds);
            }
            return best;
        }

        internal static void AddBonuses(EquipmentLoadoutEntry item, List<string> lines)
        {
            var modifiers = new HashSet<StatDef>();
            if (item.Def.equippedStatOffsets != null)
                foreach (StatModifier modifier in item.Def.equippedStatOffsets) modifiers.Add(modifier.stat);
            CompBladelinkWeapon bladelink = item.Item == null ? null : item.Item.TryGetComp<CompBladelinkWeapon>();
            if (bladelink != null)
                foreach (WeaponTraitDef trait in bladelink.TraitsListForReading)
                    if (trait.equippedStatOffsets != null)
                        foreach (StatModifier modifier in trait.equippedStatOffsets) modifiers.Add(modifier.stat);
            foreach (StatDef stat in modifiers.Where(stat => stat != null).OrderBy(stat => stat.label))
            {
                float offset = item.Item != null ? StatWorker.StatOffsetFromGear(item.Item, stat)
                    : StatUtility.GetStatOffsetFromList(item.Def.equippedStatOffsets, stat);
                if (!Mathf.Approximately(offset, 0f))
                    lines.Add(item.Def.LabelCap + " · " + stat.LabelCap + " " + stat.ValueToString(offset, ToStringNumberSense.Offset, true));
            }
            if (item.Item == null) return;
            CompWeaponMods weapon = item.Item.TryGetComp<CompWeaponMods>();
            CompApparelMods apparel = item.Item.TryGetComp<CompApparelMods>();
            if (weapon != null) AddModules(item.Item, weapon.InstalledMods, lines);
            if (apparel != null) AddModules(item.Item, apparel.InstalledMods, lines);
        }

        private static void AddModules(Thing item, List<InstalledModEntry> modules, List<string> lines)
        {
            foreach (InstalledModEntry mod in modules)
            {
                CompProperties_EquipmentMod properties = mod.ModProps;
                string line = item.LabelCap + " · " + mod.DisplayLabel();
                if (properties != null && mod.CanTrigger)
                {
                    if (properties.modKind == EquipmentModKind.Weapon && !properties.extraDamageDef.NullOrEmpty())
                        line += " · " + "TMM_Loadout_ModuleDamage".Translate(properties.extraDamageAmount.ToString("0.#"),
                            properties.extraArmorPenetration.ToStringPercent());
                    if (properties.modKind == EquipmentModKind.Apparel)
                        line += " · " + "TMM_Loadout_Armor".Translate(properties.armorSharpOffset.ToStringPercent(),
                            properties.armorBluntOffset.ToStringPercent(), properties.armorHeatOffset.ToStringPercent());
                }
                lines.Add(line);
            }
        }

        internal void Draw(Rect rect)
        {
            float y = rect.y;
            GameFont font = Text.Font;
            Text.Font = GameFont.Tiny;
            Row(rect, ref y, "TMM_Loadout_Temperature".Translate(Cold.ToStringTemperature("F0"), Heat.ToStringTemperature("F0")));
            Row(rect, ref y, "TMM_Loadout_Armor".Translate(Sharp.ToStringPercent(), Blunt.ToStringPercent(), Thermal.ToStringPercent()));
            Row(rect, ref y, "TMM_Loadout_MeleeDps".Translate(Melee.ToString("0.0")));
            Rect ranged = Row(rect, ref y, "TMM_Loadout_RangedDps".Translate(Ranged.ToString("0.0")));
            TooltipHandler.TipRegion(ranged, "TMM_Loadout_RangedEstimate".Translate());
            if (Widgets.ButtonText(new Rect(rect.x, y + 2f, rect.width, 22f), "TMM_Loadout_Bonuses".Translate(Bonuses.Count)))
                Find.WindowStack.Add(new Dialog_MessageBox(Bonuses.Count == 0 ? "TMM_Loadout_NoBonuses".Translate().ToString() : string.Join("\n", Bonuses.ToArray())));
            Text.Font = font;
        }

        private static Rect Row(Rect rect, ref float y, string label)
        {
            Rect row = new Rect(rect.x, y, rect.width, 16f);
            Widgets.Label(row, label);
            y += row.height;
            return row;
        }
    }
}
