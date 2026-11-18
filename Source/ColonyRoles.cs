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

        public static void DrawButtons()
        {
            if (Find.CurrentMap == null || Find.ColonistBar == null) return;
            var r = new UnityEngine.Rect(8f, 6f, 130f, 24f);
            if (Verse.Widgets.ButtonText(r, (Grouped ? "TMM.Roles.ShowAll" : "TMM.Roles.Group").Translate()))
                Set(!Grouped, null);
            if (!Grouped) return;
            var pawns = PawnsFinder.AllMaps_FreeColonists.ToList();
            float x = r.xMax + 6f;
            foreach (string g in new[] { Civil, Military, NoRole })
            {
                int n = pawns.Count(p => GroupOf(p) == g);
                string key = g == Civil ? "TMM.Roles.Civil" : g == Military ? "TMM.Roles.Military" : "TMM.Roles.NoRoleGroup";
                var b = new UnityEngine.Rect(x, 6f, 130f, 24f);
                bool active = Active == g;
                if (Verse.Widgets.ButtonText(b, key.Translate() + " (" + n + ")" + (active ? " ◄" : ""))) Set(true, active ? null : g);
                x = b.xMax + 6f;
            }
        }
    }
}