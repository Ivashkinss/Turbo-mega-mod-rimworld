using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    // Colony roles and ranks, stage 1 (Documentation/COLONY-ROLES-DESIGN.md): data, manual assignment,
    // automatic work priorities, saving. Ranks, promotion, bonuses, third passion and gear come later.

    public sealed class ColonyRankAura
    {
        public float radius = 9f;
        public int mood;
        public List<StatModifier> stats = new List<StatModifier>();
    }

    public sealed class ColonyRank
    {
        [MustTranslate] public string label;
        [MustTranslate] public string description;
        public int skillMin;
        public RecordDef record;
        public int recordMin;
        public int mood;
        public List<StatModifier> stats = new List<StatModifier>();
        public ColonyRankAura aura;
    }

    public sealed class ColonyRoleDef : Def
    {
        public string group = "civil";
        public SkillDef skill;
        public List<WorkTypeDef> workTypes = new List<WorkTypeDef>();
        public List<ColonyRank> ranks = new List<ColonyRank>();

        public bool Military { get { return group == "military"; } }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors()) yield return e;
            if (skill == null) yield return "skill is null";
            if (ranks.NullOrEmpty()) yield return "no ranks";
        }
    }

    public sealed class ColonyRoleAssignment : IExposable
    {
        public Pawn pawn;
        public ColonyRoleDef role;
        public int rank;

        public ColonyRank Rank { get { return role == null || role.ranks.NullOrEmpty() ? null : role.ranks[UnityEngine.Mathf.Clamp(rank, 0, role.ranks.Count - 1)]; } }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Defs.Look(ref role, "role");
            Scribe_Values.Look(ref rank, "rank");
        }
    }

    public sealed class ColonyRoles : GameComponent
    {
        private List<ColonyRoleAssignment> assignments = new List<ColonyRoleAssignment>();

        public ColonyRoles(Game game) { }

        public static ColonyRoles Instance { get { return Current.Game == null ? null : Current.Game.GetComponent<ColonyRoles>(); } }

        public ColonyRoleAssignment For(Pawn p) { return p == null ? null : assignments.FirstOrDefault(a => a.pawn == p); }
        public IEnumerable<ColonyRoleAssignment> All { get { return assignments; } }

        /// Assigns a role (or clears it with null). The role's work types go to priority 1; other work that was
        /// at priority 1 drops to 2, so the role's profession comes first. The player can still edit priorities.
        public void Assign(Pawn p, ColonyRoleDef role)
        {
            if (p == null) return;
            ColonyRoleAssignment a = For(p);
            if (role == null) { if (a != null) assignments.Remove(a); return; }
            if (a == null) { a = new ColonyRoleAssignment { pawn = p }; assignments.Add(a); }
            if (a.role != role) { a.role = role; a.rank = 0; }
            ApplyWorkPriorities(p, role);
        }

        public static void ApplyWorkPriorities(Pawn p, ColonyRoleDef role)
        {
            if (p.workSettings == null) return;
            if (!p.workSettings.EverWork) p.workSettings.EnableAndInitialize();
            if (Current.Game.playSettings != null) Current.Game.playSettings.useWorkPriorities = true;
            foreach (WorkTypeDef w in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (p.WorkTypeIsDisabled(w)) continue;
                if (role.workTypes.Contains(w)) p.workSettings.SetPriority(w, 1);
                else if (p.workSettings.GetPriority(w) == 1) p.workSettings.SetPriority(w, 2);
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref assignments, "TMM_colonyRoles", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (assignments == null) assignments = new List<ColonyRoleAssignment>();
                assignments.RemoveAll(a => a == null || a.pawn == null || a.role == null);
            }
        }
    }

    /// "Role" gizmo on selected free colonists: pick a role or clear it.
    [StaticConstructorOnStartup]
    public static class ColonyRoleGizmo
    {
        static ColonyRoleGizmo()
        {
            var target = AccessTools.Method(typeof(Pawn), "GetGizmos");
            if (target == null) { Log.Error("[TMM] ColonyRoles: Pawn.GetGizmos not found."); return; }
            new Harmony("turbomegamod.colony.roles").Patch(target, postfix: new HarmonyMethod(typeof(ColonyRoleGizmo), "AddGizmo"));
        }

        public static void AddGizmo(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance == null || !__instance.IsFreeColonist || __instance.Dead) return;
            __result = __result.Concat(new[] { Make(__instance) });
        }

        private static Gizmo Make(Pawn p)
        {
            ColonyRoles roles = ColonyRoles.Instance;
            ColonyRoleAssignment a = roles == null ? null : roles.For(p);
            string current = a == null ? "TMM.Roles.None".Translate().ToString() : a.role.LabelCap.ToString() + " — " + a.Rank.label.CapitalizeFirst();
            return new Command_Action
            {
                defaultLabel = "TMM.Roles.Gizmo".Translate() + ": " + current,
                defaultDesc = "TMM.Roles.GizmoDesc".Translate(),
                icon = TexCommand.Draft,
                action = () =>
                {
                    var opts = new List<FloatMenuOption>();
                    foreach (ColonyRoleDef r in DefDatabase<ColonyRoleDef>.AllDefsListForReading.OrderBy(r => r.Military).ThenBy(r => r.label))
                    {
                        ColonyRoleDef role = r;
                        string tag = (role.Military ? "TMM.Roles.Military" : "TMM.Roles.Civil").Translate().ToString();
                        opts.Add(new FloatMenuOption(tag + ": " + role.LabelCap, () => { if (ColonyRoles.Instance != null) ColonyRoles.Instance.Assign(p, role); }));
                    }
                    opts.Add(new FloatMenuOption("TMM.Roles.Clear".Translate(), () => { if (ColonyRoles.Instance != null) ColonyRoles.Instance.Assign(p, null); }));
                    Find.WindowStack.Add(new FloatMenu(opts));
                }
            };
        }
    }

    /// Stage 2: colonist bar grouping. "Group" hides the portraits and shows group buttons (civil, military,
    /// no role); clicking a group shows only its colonists, highest rank first. "Show all" restores the bar.
    [StaticConstructorOnStartup]
    public static class ColonistBarGroups
    {
        public const string Civil = "civil", Military = "military", NoRole = "none";
        public static bool Grouped;
        public static string Active;
        private static readonly System.Reflection.FieldInfo EntriesField = AccessTools.Field(typeof(ColonistBar), "cachedEntries");

        static ColonistBarGroups()
        {
            var harmony = new Harmony("turbomegamod.colony.roles.bar");
            var recache = AccessTools.Method(typeof(ColonistBar), "CheckRecacheEntries");
            var gui = AccessTools.Method(typeof(ColonistBar), "ColonistBarOnGUI");
            if (recache == null || gui == null || EntriesField == null) { Log.Error("[TMM] ColonistBarGroups: ColonistBar members not found; grouping disabled."); return; }
            harmony.Patch(recache, postfix: new HarmonyMethod(typeof(ColonistBarGroups), "FilterEntries"));
            harmony.Patch(gui, postfix: new HarmonyMethod(typeof(ColonistBarGroups), "DrawButtons"));
            var play = AccessTools.Method(typeof(PlaySettings), "DoPlaySettingsGlobalControls");
            if (play == null) Log.Error("[TMM] ColonistBarGroups: PlaySettings.DoPlaySettingsGlobalControls not found; group toggle hidden.");
            else harmony.Patch(play, postfix: new HarmonyMethod(typeof(ColonistBarGroups), "PlaySettingsToggle"));
        }

        public static string GroupOf(Pawn p)
        {
            ColonyRoles roles = ColonyRoles.Instance;
            ColonyRoleAssignment a = roles == null ? null : roles.For(p);
            return a == null ? NoRole : (a.role.Military ? Military : Civil);
        }

        public static int RankOf(Pawn p)
        {
            ColonyRoles roles = ColonyRoles.Instance;
            ColonyRoleAssignment a = roles == null ? null : roles.For(p);
            return a == null ? -1 : a.rank;
        }

        public static void Set(bool grouped, string active)
        {
            Grouped = grouped; Active = active;
            if (Find.ColonistBar != null) Find.ColonistBar.MarkColonistsDirty();
        }

        public static void FilterEntries(ColonistBar __instance)
        {
            if (!Grouped) return;
            var list = EntriesField.GetValue(__instance) as List<ColonistBar.Entry>;
            if (list == null) return;
            var kept = list.Where(e => e.pawn != null && Active != null && GroupOf(e.pawn) == Active)
                           .OrderByDescending(e => RankOf(e.pawn)).ThenBy(e => e.pawn.LabelShort).ToList();
            list.Clear();
            list.AddRange(kept);
        }

        /// Toggle in the bottom-right play settings icons (user request: the top-left button was in the way).
        public static void PlaySettingsToggle(WidgetRow row, bool worldView)
        {
            if (worldView || row == null) return;
            bool g = Grouped;
            row.ToggleableIcon(ref g, TexCommand.Draft, "TMM.Roles.GroupToggleTip".Translate(), null, null);
            if (g != Grouped) Set(g, null);
        }

        /// Group buttons appear only while grouped, centred under the colonist bar.
        public static void DrawButtons()
        {
            if (!Grouped || Find.CurrentMap == null || Find.ColonistBar == null) return;
            var pawns = PawnsFinder.AllMaps_FreeColonists.ToList();
            float w = 150f, gap = 6f, total = 3 * w + 2 * gap;
            float x = (Verse.UI.screenWidth - total) / 2f, y = Active == null ? 8f : 118f;
            foreach (string grp in new[] { Civil, Military, NoRole })
            {
                int n = pawns.Count(p => GroupOf(p) == grp);
                string key = grp == Civil ? "TMM.Roles.Civil" : grp == Military ? "TMM.Roles.Military" : "TMM.Roles.NoRoleGroup";
                bool active = Active == grp;
                if (Verse.Widgets.ButtonText(new UnityEngine.Rect(x, y, w, 26f), key.Translate() + " (" + n + ")" + (active ? " ◄" : ""))) Set(true, active ? null : grp);
                x += w + gap;
            }
        }
    }

    /// Stage 4: rank stat bonuses and the commander's aura, as a StatPart added to every stat the ranks use.
    public sealed class StatPart_ColonyRank : StatPart
    {
        // The bonus is added after post-processing (see ColonyRankStatParts.AfterGetValue): many work stats have a
        // postProcessCurve that would otherwise swallow a raw offset. This part only explains the bonus.
        public override void TransformValue(StatRequest req, ref float val) { }

        public override string ExplanationPart(StatRequest req)
        {
            float own, aura; Offsets(req, out own, out aura);
            if (own == 0f && aura == 0f) return null;
            string s = "";
            if (own != 0f) s += "TMM.Roles.RankBonus".Translate() + ": " + own.ToStringByStyle(parentStat.toStringStyle, ToStringNumberSense.Offset);
            if (aura != 0f) s += (s.Length > 0 ? "\n" : "") + "TMM.Roles.AuraBonus".Translate() + ": " + aura.ToStringByStyle(parentStat.toStringStyle, ToStringNumberSense.Offset);
            return s;
        }

        public void Offsets(StatRequest req, out float own, out float aura) { OffsetsFor(parentStat, req, out own, out aura); }

        public static void OffsetsFor(StatDef parentStat, StatRequest req, out float own, out float aura)
        {
            own = 0f; aura = 0f;
            Pawn p = req.Thing as Pawn;
            ColonyRoles roles = ColonyRoles.Instance;
            if (p == null || roles == null) return;
            ColonyRoleAssignment a = roles.For(p);
            if (a != null && a.Rank != null) own = a.Rank.stats.Where(m => m.stat == parentStat).Sum(m => m.value);
            ColonyRankAura best = ColonyRankEffects.AuraFor(p);
            if (best != null) aura = best.stats.Where(m => m.stat == parentStat).Sum(m => m.value);
        }
    }

    public static class ColonyRankEffects
    {
        /// The strongest commander aura covering this military pawn (from another military pawn on the same map).
        public static ColonyRankAura AuraFor(Pawn p)
        {
            ColonyRoles roles = ColonyRoles.Instance;
            if (p == null || roles == null || !p.Spawned) return null;
            ColonyRoleAssignment mine = roles.For(p);
            if (mine == null || !mine.role.Military) return null;
            ColonyRankAura best = null;
            foreach (ColonyRoleAssignment a in roles.All)
            {
                if (a.pawn == p || a.pawn == null || !a.pawn.Spawned || a.pawn.Map != p.Map || a.pawn.Dead || a.pawn.Downed) continue;
                ColonyRank r = a.Rank;
                if (r == null || r.aura == null || !a.role.Military) continue;
                if (a.pawn.Position.DistanceTo(p.Position) > r.aura.radius) continue;
                if (best == null || r.aura.mood > best.mood) best = r.aura;
            }
            return best;
        }
    }

    public sealed class ThoughtWorker_ColonyRank : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            ColonyRoles roles = ColonyRoles.Instance;
            ColonyRoleAssignment a = roles == null ? null : roles.For(p);
            return a != null && a.Rank != null && a.Rank.mood != 0 ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }

    public sealed class Thought_ColonyRank : Thought_Situational
    {
        public override float MoodOffset()
        {
            ColonyRoles roles = ColonyRoles.Instance;
            ColonyRoleAssignment a = roles == null ? null : roles.For(pawn);
            return a == null || a.Rank == null ? 0f : a.Rank.mood;
        }

        public override string LabelCap
        {
            get
            {
                ColonyRoles roles = ColonyRoles.Instance;
                ColonyRoleAssignment a = roles == null ? null : roles.For(pawn);
                return a == null || a.Rank == null ? base.LabelCap : "TMM.Roles.ProudOfRank".Translate(a.Rank.label).CapitalizeFirst().ToString();
            }
        }
    }

    public sealed class ThoughtWorker_CommanderAura : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            ColonyRankAura aura = ColonyRankEffects.AuraFor(p);
            return aura != null && aura.mood != 0 ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }

    public sealed class Thought_CommanderAura : Thought_Situational
    {
        public override float MoodOffset()
        {
            ColonyRankAura aura = ColonyRankEffects.AuraFor(pawn);
            return aura == null ? 0f : aura.mood;
        }
    }

    [StaticConstructorOnStartup]
    public static class ColonyRankStatParts
    {
        static ColonyRankStatParts()
        {
            var stats = new HashSet<StatDef>();
            foreach (ColonyRoleDef role in DefDatabase<ColonyRoleDef>.AllDefsListForReading)
                foreach (ColonyRank r in role.ranks)
                {
                    foreach (StatModifier m in r.stats) if (m.stat != null) stats.Add(m.stat);
                    if (r.aura != null) foreach (StatModifier m in r.aura.stats) if (m.stat != null) stats.Add(m.stat);
                }
            foreach (StatDef s in stats)
            {
                if (s.parts == null) s.parts = new List<StatPart>();
                if (!s.parts.Any(x => x is StatPart_ColonyRank)) s.parts.Add(new StatPart_ColonyRank { parentStat = s });
            }
            Affected = stats;
            var target = AccessTools.Method(typeof(StatWorker), "GetValue", new[] { typeof(StatRequest), typeof(bool) });
            if (target == null) Log.Error("[TMM] ColonyRanks: StatWorker.GetValue(StatRequest, bool) not found; rank bonuses inactive.");
            else new Harmony("turbomegamod.colony.roles.stats").Patch(target, postfix: new HarmonyMethod(typeof(ColonyRankStatParts), "AfterGetValue"));
        }

        public static HashSet<StatDef> Affected = new HashSet<StatDef>();

        public static void AfterGetValue(StatRequest req, bool applyPostProcess, ref float __result, StatDef ___stat)
        {
            if (!applyPostProcess || ___stat == null || !Affected.Contains(___stat) || !(req.Thing is Pawn)) return;
            float own, aura; StatPart_ColonyRank.OffsetsFor(___stat, req, out own, out aura);
            __result += own + aura;
        }
    }
}