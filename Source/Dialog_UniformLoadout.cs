using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    internal sealed class Dialog_UniformLoadout : Window
    {
        private static readonly string[] LeftSlots = { "head", "eyes", "skin", "middle", "shell" };
        private static readonly string[] RightSlots = { "legs", "belt", "backpack", "powerarmor", "other" };
        private readonly Pawn pawn;
        private readonly ColonyRoleDef role;
        private readonly ColonyRank rank;
        private readonly ApparelPolicy policy;
        private readonly List<ThingDef> apparel;
        private readonly List<ThingDef> weapons;
        private readonly BodyDef body;
        private EquipmentLoadoutStats stats;

        internal Dialog_UniformLoadout(ColonyRoleDef role, ColonyRank rank, ApparelPolicy policy, Pawn pawn = null)
        {
            this.role = role;
            this.rank = rank;
            this.policy = policy;
            this.pawn = pawn;
            body = (pawn == null ? ThingDefOf.Human.race : pawn.RaceProps).body;
            apparel = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsApparel && policy.filter.Allows(def))
                .OrderBy(def => rank.uniform.apparel.Contains(def) ? 0 : 1).ThenBy(def => def.label).ToList();
            weapons = RankWeaponPreferences.Get(role, rank).GetWeapons();
            doCloseX = true;
            absorbInputAroundWindow = true;
            RefreshStats();
        }

        public override Vector2 InitialSize { get { return new Vector2(670f, 485f); } }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 34f), "TMM_Loadout_UniformTitle".Translate(policy.label));
            Text.Font = GameFont.Small;
            const float size = 62f;
            const float step = 66f;
            for (int i = 0; i < LeftSlots.Length; i++)
                DrawApparelSlot(new Rect(rect.x, rect.y + 42f + i * step, 148f, size), LeftSlots[i]);
            for (int i = 0; i < RightSlots.Length; i++)
                DrawApparelSlot(new Rect(rect.xMax - 148f, rect.y + 42f + i * step, 148f, size), RightSlots[i]);

            Rect center = new Rect(rect.x + 165f, rect.y + 42f, rect.width - 330f, 140f);
            Widgets.DrawBoxSolid(center, new Color(0.14f, 0.15f, 0.17f, 0.9f));
            stats.Draw(center.ContractedBy(10f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(center.x + 10f, center.y + 101f, center.width - 20f, 36f), "TMM_Loadout_TemplateStats".Translate());
            Text.Font = GameFont.Small;

            DrawWeaponSlot(new Rect(center.x, center.yMax + 15f, center.width, size), true);
            DrawWeaponSlot(new Rect(center.x, center.yMax + 15f + step, center.width, size), false);

            string apply = (pawn == null ? "TMM_Loadout_Save" : "TMM_Loadout_Assign").Translate();
            if (Widgets.ButtonText(new Rect(rect.xMax - 220f, rect.yMax - 38f, 220f, 34f), apply))
            {
                Save();
                Close();
            }
        }

        private void DrawApparelSlot(Rect rect, string slot)
        {
            Func<ThingDef, bool> matches = def => Matches(def, slot);
            DrawSlot(rect, SlotKey(slot).Translate(), apparel.FirstOrDefault(matches), apparel.Count(matches),
                () => Pick(matches, true, choice => ChangeApparel(matches, choice)));
        }

        private void DrawWeaponSlot(Rect rect, bool ranged)
        {
            Func<ThingDef, bool> matches = def => ranged ? def.IsRangedWeapon : def.IsMeleeWeapon;
            DrawSlot(rect, (ranged ? "TMM_Loadout_RangedSlot" : "TMM_Loadout_MeleeSlot").Translate(),
                weapons.FirstOrDefault(matches), weapons.Count(matches), () => Pick(matches, false, choice =>
                {
                    weapons.RemoveAll(def => matches(def));
                    if (choice != null)
                    {
                        if (ranged) weapons.Insert(0, choice);
                        else weapons.Add(choice);
                    }
                    RefreshStats();
                }));
        }

        private static void DrawSlot(Rect rect, string label, ThingDef item, int options, Action choose)
        {
            Rect icon = new Rect(rect.x, rect.y, 62f, 62f);
            ITab_TMM_Equipment.DrawSlotFrame(icon, item != null);
            if (item != null) Widgets.DefIcon(icon.ContractedBy(6f), item, item.MadeFromStuff ? GenStuff.DefaultStuffFor(item) : null);
            Rect content = new Rect(icon.xMax + 5f, rect.y + 7f, rect.width - icon.width - 6f, 51f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(content.x, content.y, content.width, 19f), label);
            if (item != null)
            {
                string name = options > 1 ? "TMM_Loadout_Alternatives".Translate(item.LabelCap, options - 1).ToString() : item.LabelCap.ToString();
                Widgets.Label(new Rect(content.x, content.y + 19f, content.width, 30f), name);
                TooltipHandler.TipRegion(rect, name);
            }
            Text.Font = GameFont.Small;
            if (Widgets.ButtonInvisible(rect)) choose();
        }

        private void Pick(Func<ThingDef, bool> matches, bool clothing, Action<ThingDef> choose)
        {
            var options = new List<FloatMenuOption> { new FloatMenuOption("TMM_Loadout_EmptySlot".Translate(), () => choose(null)) };
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsApparel == clothing && (clothing || def.IsWeapon) && matches(def)).OrderBy(def => def.label))
            {
                ThingDef choice = def;
                options.Add(new FloatMenuOption(choice.LabelCap, () => choose(choice)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ChangeApparel(Func<ThingDef, bool> matches, ThingDef selected)
        {
            apparel.RemoveAll(def => matches(def) || (selected != null && !ApparelUtility.CanWearTogether(def, selected, body)));
            if (selected != null) apparel.Insert(0, selected);
            RefreshStats();
        }

        private void RefreshStats()
        {
            var compatible = new List<ThingDef>();
            foreach (ThingDef def in apparel)
                if (compatible.All(other => ApparelUtility.CanWearTogether(def, other, body))) compatible.Add(def);
            stats = EquipmentLoadoutStats.Planned(pawn, compatible, weapons);
        }

        private void Save()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                if (def.IsApparel && policy.filter.Allows(def) != apparel.Contains(def))
                    policy.filter.SetAllow(def, apparel.Contains(def));
            RankWeaponPreferences.Save(role, rank, weapons);
            if (pawn == null || pawn.Dead || !pawn.IsFreeColonist) return;
            ColonyRoles.Instance.Assign(pawn, role);
            if (pawn.outfits != null) pawn.outfits.CurrentApparelPolicy = policy;
        }

        private static bool Matches(ThingDef def, string slot)
        {
            return ITab_TMM_Equipment.ApparelSlot(def) == slot;
        }

        private static string SlotKey(string slot)
        {
            return slot == "powerarmor" ? "TMM_Slot_PowerArmor" : slot == "other" ? "TMM_Loadout_OtherSlot" : "TMM_Slot_" + slot.CapitalizeFirst();
        }
    }

    [StaticConstructorOnStartup]
    internal static class UniformPolicyEditorButton
    {
        private static readonly MethodInfo SelectedPolicy = AccessTools.PropertyGetter(typeof(Dialog_ManageApparelPolicies), "SelectedPolicy");

        static UniformPolicyEditorButton()
        {
            MethodInfo contents = AccessTools.Method(typeof(Dialog_ManageApparelPolicies), "DoContentsRect", new[] { typeof(Rect) });
            if (contents != null && SelectedPolicy != null)
                new Harmony("colonyproject.integrated.uniformeditor").Patch(contents,
                    prefix: new HarmonyMethod(typeof(UniformPolicyEditorButton), "AddEditorButton"));
        }

        private static void AddEditorButton(Dialog_ManageApparelPolicies __instance, ref Rect rect)
        {
            var policy = SelectedPolicy.Invoke(__instance, null) as ApparelPolicy;
            ColonyRoleDef role;
            ColonyRank rank;
            if (!ColonyRankUniforms.TryGetManagedRank(policy, out role, out rank)) return;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 230f, 30f), "TMM_Loadout_EditUniform".Translate()))
                Find.WindowStack.Add(new Dialog_UniformLoadout(role, rank, policy));
            rect.yMin += 36f;
        }
    }
}
