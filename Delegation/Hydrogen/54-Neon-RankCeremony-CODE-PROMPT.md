**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

## Порядок ответа (важно)
Один файл на ответ, без вступлений, потом жди «дальше». Части называй `54_1`, `54_2`… Над каждым блоком — путь файла.

## Задача 54 — церемония повышения и форма ранга
В моде есть роли и звания колонистов (код ниже): 7 ролей, 24 ранга, требования и кнопка «Повысить» (`ColonyPromotion`). Дизайн пользователя (`COLONY-ROLES-DESIGN.md`): повышение с Ideology — **ритуал**, без DLC — кнопка; у ранга своя **экипировка**, и пешки переодеваются сами.

### 1. Церемония (только с Ideology, `MayRequire="Ludeon.RimWorld.Ideology"`)
- `PreceptDef` и `RitualPatternDef`/`RitualBehaviorDef` «Церемония повышения», в духе ванильных ритуалов (например, «речь лидера»): цель — повышаемый колонист; ведущий — командир отряда (роль Commander) или лидер идеологии; зрители — колонисты у места сбора.
- Качество ритуала даёт настроение повышенному и зрителям (`ThoughtDef` с `stages` по качеству), а при отличном качестве — небольшой временный бонус (`HediffDef` на 5 дней: сознание или общение).
- Само повышение выполняется **в конце ритуала** через существующий `ColonyPromotion` — без дублирования логики.
- Когда Ideology активен и прецепт есть у идеологии, кнопка «Повысить» открывает ритуал (ванильный `Dialog_BeginRitual`). Без Ideology или без прецепта — прежнее мгновенное повышение.
- Прецепт добавляется в стартовые идеологии игрока автоматически, не ломая ванильные мемы. Если это неудобно — отдельной кнопкой «Учредить церемонию» (опиши выбор).

### 2. Форма ранга
- В `ColonyRank` новое необязательное поле `uniform` (список `ThingDef` одежды + необязательное оружие).
- Для каждой роли и ранга, где задана форма, создаётся и обновляется ванильная `ApparelPolicy` «<Роль>: <Ранг>» (`Current.Game.outfitDatabase`), разрешающая эту одежду. Повышение переводит пешку на политику нового ранга, если у неё стояла политика прошлого ранга или по умолчанию (свою ручную настройку игрока не трогать).
- Оружие ранга: если в оружейной есть такое оружие, а у пешки нет лучшего, пешка берёт его сама — через ванильный `JobDefOf.Equip` из своего `WorkGiver` с низким приоритетом. Без бесконечных переодеваний.
- Задай формы для военных ролей (боец, командир) по рангам из ванильных вещей и вещей мода, если они подходят (`MayRequire` для DLC).

### 3. Проба `Tests/InGameColonyRanksProbe.cs` (флаг `-tmm-colony-ranks-probe`)
- Без Ideology-ритуала: повышение кнопкой меняет ранг и политику одежды.
- С ритуалом (если Ideology активен): ритуал создаётся, после `RitualOutcome` ранг повышен, мысли выданы.
- Ручная политика игрока не перезаписывается.
- Отчёт `ColonyRanksProbe.json` (passed по факту) и `ColonyRanksUiProbe.json`. **Не вызывай** `Root.Shutdown`.

## Правила кода
C# 5 (без `?.`, `nameof`, интерполяции, expression-bodied), RimWorld 1.7. `// CHECK API` где не уверен. Русский перевод: DefInjected для новых Def и Keyed для строк интерфейса.

## Порядок частей
1. `Source/ColonyRankCeremony.cs`
2. `Source/ColonyRankUniform.cs`
3. правки `Source/ColonyRoles.cs` и `Source/ColonyPromotion.cs` — **целиком**, по одному файлу в ответ
4. `Defs/...` новые Def (прецепт, ритуал, мысли, хедифф, WorkGiver) и формы в `Defs/ColonyRoleDefs/ColonyRoles.xml` (целиком)
5. `Languages/Russian/...` и `Languages/English/Keyed/...`
6. `Tests/InGameColonyRanksProbe.cs`

## Текущий код: Source/ColonyRoles.cs
```csharp
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
        public int readyNotifiedFor = -1;

        public ColonyRank Rank { get { return role == null || role.ranks.NullOrEmpty() ? null : role.ranks[UnityEngine.Mathf.Clamp(rank, 0, role.ranks.Count - 1)]; } }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Defs.Look(ref role, "role");
            Scribe_Values.Look(ref rank, "rank");
            Scribe_Values.Look(ref readyNotifiedFor, "readyNotifiedFor", -1);
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

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 2500 == 0) ColonyPromotion.NotifyReady(this);
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
            var extra = new List<Gizmo> { Make(__instance) };
            ColonyRoles cr = ColonyRoles.Instance;
            ColonyRoleAssignment ca = cr == null ? null : cr.For(__instance);
            Gizmo promote = ca == null ? null : ColonyPromotion.PromoteGizmo(__instance, ca);
            if (promote != null) extra.Add(promote);
            __result = __result.Concat(extra);
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
```
## Текущий код: Source/ColonyPromotion.cs
```csharp
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Colony roles stage 3a: promotion requirements, promote gizmo, "ready for promotion" letter, thoughts.
    /// Works with and without Ideology; the Ideology promotion ritual builds on Promote() later.
    /// Texts: Documentation/ColonyRoles-Promotion-Texts.json (Gemini).
    public static class ColonyPromotion
    {
        public static ColonyRank NextRank(ColonyRoleAssignment a)
        {
            return a == null || a.role == null || a.rank + 1 >= a.role.ranks.Count ? null : a.role.ranks[a.rank + 1];
        }

        public static int SkillLevel(Pawn p, ColonyRoleDef role)
        {
            return p.skills == null || role.skill == null ? 0 : p.skills.GetSkill(role.skill).Level;
        }

        public static float RecordValue(Pawn p, RecordDef record)
        {
            return record == null || p.records == null ? 0f : p.records.GetValue(record);
        }

        /// Null when the pawn meets every requirement of the next rank; otherwise a readable reason.
        public static string MissingFor(Pawn p, ColonyRoleAssignment a)
        {
            ColonyRank next = NextRank(a);
            if (next == null) return "TMM.Roles.MaxRank".Translate();
            var lines = new List<string>();
            int skill = SkillLevel(p, a.role);
            if (skill < next.skillMin) lines.Add("TMM.Roles.Requirement".Translate(a.role.skill.LabelCap, skill, next.skillMin));
            if (next.record != null)
            {
                float have = RecordValue(p, next.record);
                if (have < next.recordMin) lines.Add("TMM.Roles.Requirement".Translate(next.record.LabelCap, (int)have, next.recordMin));
            }
            return lines.Count == 0 ? null : string.Join("\n", lines.ToArray());
        }

        public static bool Ready(Pawn p, ColonyRoleAssignment a) { return NextRank(a) != null && MissingFor(p, a) == null; }

        public static void Promote(Pawn p, ColonyRoleAssignment a)
        {
            if (!Ready(p, a)) return;
            a.rank++;
            a.readyNotifiedFor = -1;
            ColonyRank r = a.Rank;
            ThoughtDef mine = DefDatabase<ThoughtDef>.GetNamedSilentFail(a.role.Military ? "TMM_PromotedMilitary" : "TMM_PromotedCivil");
            if (mine != null && p.needs != null && p.needs.mood != null) p.needs.mood.thoughts.memories.TryGainMemory(mine);
            ThoughtDef seen = DefDatabase<ThoughtDef>.GetNamedSilentFail("TMM_WitnessedPromotion");
            if (seen != null && p.Map != null)
                foreach (Pawn w in p.Map.mapPawns.FreeColonistsSpawned)
                    if (w != p && w.needs != null && w.needs.mood != null) w.needs.mood.thoughts.memories.TryGainMemory(seen);
            Find.LetterStack.ReceiveLetter("TMM.Roles.PromotedTitle".Translate(p.LabelShort),
                "TMM.Roles.PromotedText".Translate(p.LabelShort, r.label, a.role.label), LetterDefOf.PositiveEvent, p);
        }

        /// Called periodically: one "ready for promotion" letter per reached rank.
        public static void NotifyReady(ColonyRoles roles)
        {
            foreach (ColonyRoleAssignment a in roles.All)
            {
                if (a.pawn == null || a.pawn.Dead || !a.pawn.IsColonist || a.readyNotifiedFor == a.rank + 1 || !Ready(a.pawn, a)) continue;
                a.readyNotifiedFor = a.rank + 1;
                ColonyRank next = NextRank(a);
                Find.LetterStack.ReceiveLetter("TMM.Roles.ReadyTitle".Translate(a.pawn.LabelShort),
                    "TMM.Roles.ReadyText".Translate(a.pawn.LabelShort, next.label, a.role.label), LetterDefOf.NeutralEvent, a.pawn);
            }
        }

        public static Gizmo PromoteGizmo(Pawn p, ColonyRoleAssignment a)
        {
            ColonyRank next = NextRank(a);
            if (next == null) return null;
            string missing = MissingFor(p, a);
            var cmd = new Command_Action
            {
                defaultLabel = "TMM.Roles.PromoteButton".Translate(next.label),
                defaultDesc = "TMM.Roles.NextRank".Translate(next.label) + "\n" + next.description + (missing == null ? "" : "\n\n" + missing),
                icon = TexCommand.Draft,
                action = () => Promote(p, a)
            };
            if (missing != null) cmd.Disable(missing);
            return cmd;
        }
    }
}
```
## Текущие роли: Defs/ColonyRoleDefs/ColonyRoles.xml
```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Colony roles and ranks (Documentation/COLONY-ROLES-DESIGN.md). Data: Documentation/ColonyRoles-Data.json (Hydrogen). -->
<Defs>
  <TurboMegaMod.ColonyRoleDef>
    <defName>TMM_Role_doctor</defName>
    <label>medic</label>
    <group>civil</group>
    <skill>Medicine</skill>
    <workTypes>
      <li>Doctor</li>
    </workTypes>
    <ranks>
      <li>
        <label>field orderly</label>
        <description>Confidently applies bandages and stops bleeding even under heavy fire.</description>
        <skillMin>10</skillMin>
        <record>TimesTendedOther</record>
        <recordMin>50</recordMin>
        <mood>1</mood>
        <stats>
          <MedicalTendSpeed>0.04</MedicalTendSpeed>
        </stats>
      </li>
      <li>
        <label>trauma physician</label>
        <description>Pulls colonists back from the brink of death and has already saved dozens of lives.</description>
        <skillMin>15</skillMin>
        <record>TimesTendedOther</record>
        <recordMin>150</recordMin>
        <mood>3</mood>
        <stats>
          <MedicalTendSpeed>0.07</MedicalTendSpeed>
          <MedicalTendQuality>0.05</MedicalTendQuality>
        </stats>
      </li>
      <li>
        <label>chief surgeon</label>
        <description>A legend of the infirmary whose hands never tremble during the most hopeless operations.</description>
        <skillMin>20</skillMin>
        <record>TimesTendedOther</record>
        <recordMin>350</recordMin>
        <mood>5</mood>
        <stats>
          <MedicalTendSpeed>0.10</MedicalTendSpeed>
          <MedicalTendQuality>0.08</MedicalTendQuality>
          <MedicalSurgerySuccessChance>0.05</MedicalSurgerySuccessChance>
        </stats>
      </li>
    </ranks>
  </TurboMegaMod.ColonyRoleDef>
  <TurboMegaMod.ColonyRoleDef>
    <defName>TMM_Role_builder</defName>
    <label>builder</label>
    <group>civil</group>
    <skill>Construction</skill>
    <workTypes>
      <li>Construction</li>
    </workTypes>
    <ranks>
      <li>
        <label>sapper</label>
        <description>Quickly raises walls and fortifications out of whatever scrap is at hand.</description>
        <skillMin>10</skillMin>
        <record>ThingsConstructed</record>
        <recordMin>100</recordMin>
        <mood>1</mood>
        <stats>
          <ConstructionSpeed>0.05</ConstructionSpeed>
        </stats>
      </li>
      <li>
        <label>foreman</label>
        <description>Knows every joint and beam in the settlement, building to last without a single flaw.</description>
        <skillMin>15</skillMin>
        <record>ThingsConstructed</record>
        <recordMin>300</recordMin>
        <mood>3</mood>
        <stats>
          <ConstructionSpeed>0.08</ConstructionSpeed>
          <ConstructSuccessChance>0.04</ConstructSuccessChance>
        </stats>
      </li>
      <li>
        <label>chief architect</label>
        <description>Turns wasteland ruins into impregnable bastions of pre-war quality.</description>
        <skillMin>20</skillMin>
        <record>ThingsConstructed</record>
        <recordMin>700</recordMin>
        <mood>5</mood>
        <stats>
          <ConstructionSpeed>0.10</ConstructionSpeed>
          <ConstructSuccessChance>0.07</ConstructSuccessChance>
          <WorkSpeedGlobal>0.04</WorkSpeedGlobal>
        </stats>
      </li>
    </ranks>
  </TurboMegaMod.ColonyRoleDef>
  <TurboMegaMod.ColonyRoleDef>
    <defName>TMM_Role_crafter</defName>
    <label>crafter</label>
    <group>civil</group>
    <skill>Crafting</skill>
    <workTypes>
      <li>Crafting</li>
      <li>Smithing</li>
      <li>Tailoring</li>
    </workTypes>
    <ranks>
      <li>
        <label>tinker</label>
        <description>Confidently handles the workbench, anvil, and sewing needle alike.</description>
        <skillMin>10</skillMin>
        <record>ThingsCrafted</record>
        <recordMin>80</recordMin>
        <mood>1</mood>
        <stats>
          <GeneralLaborSpeed>0.05</GeneralLaborSpeed>
        </stats>
      </li>
      <li>
        <label>master artisan</label>
        <description>Turns out armor and weapons from the workshop that fighters trust with their lives.</description>
        <skillMin>15</skillMin>
        <record>ThingsCrafted</record>
        <recordMin>250</recordMin>
        <mood>3</mood>
        <stats>
          <GeneralLaborSpeed>0.08</GeneralLaborSpeed>
          <SmeltingSpeed>0.06</SmeltingSpeed>
        </stats>
      </li>
      <li>
        <label>grand artificer</label>
        <description>A workbench genius capable of forging a masterpiece from rusted junk and bioferrite.</description>
        <skillMin>20</skillMin>
        <record>ThingsCrafted</record>
        <recordMin>600</recordMin>
        <mood>5</mood>
        <stats>
          <GeneralLaborSpeed>0.10</GeneralLaborSpeed>
          <SmeltingSpeed>0.08</SmeltingSpeed>
          <WorkSpeedGlobal>0.04</WorkSpeedGlobal>
        </stats>
      </li>
    </ranks>
  </TurboMegaMod.ColonyRoleDef>
  <TurboMegaMod.ColonyRoleDef>
    <defName>TMM_Role_handler</defName>
    <label>animal handler</label>
    <group>civil</group>
    <skill>Animals</skill>
    <workTypes>
      <li>Handling</li>
    </workTypes>
    <ranks>
      <li>
        <label>herder</label>
        <description>Knows the habits of wasteland beasts and can calm even skittish livestock.</description>
        <skillMin>10</skillMin>
        <record>AnimalsTamed</record>
        <recordMin>10</recordMin>
        <mood>1</mood>
        <stats>
          <TameAnimalChance>0.04</TameAnimalChance>
        </stats>
      </li>
      <li>
        <label>beast trainer</label>
        <description>Turns wild predators into loyal defenders and providers for the colony.</description>
        <skillMin>15</skillMin>
        <record>AnimalsTamed</record>
        <recordMin>25</recordMin>
        <mood>3</mood>
        <stats>
          <TameAnimalChance>0.07</TameAnimalChance>
          <AnimalGatherYield>0.06</AnimalGatherYield>
        </stats>
      </li>
      <li>
        <label>packmaster</label>
        <description>Understands the language of every beast and commands a pack of tamed monsters.</description>
        <skillMin>20</skillMin>
        <record>AnimalsTamed</record>
        <recordMin>50</recordMin>
        <mood>5</mood>
        <stats>
          <TameAnimalChance>0.10</TameAnimalChance>
          <AnimalGatherYield>0.09</AnimalGatherYield>
          <TrainAnimalChance>0.07</TrainAnimalChance>
        </stats>
      </li>
    </ranks>
  </TurboMegaMod.ColonyRoleDef>
  <TurboMegaMod.ColonyRoleDef>
    <defName>TMM_Role_farmer</defName>
    <label>farmer</label>
    <group>civil</group>
    <skill>Plants</skill>
    <workTypes>
      <li>Growing</li>
      <li>PlantCutting</li>
    </workTypes>
    <ranks>
      <li>
        <label>field hand</label>
        <description>Knows how to coax a harvest even out of ash-poisoned soil.</description>
        <skillMin>10</skillMin>
        <record>PlantsHarvested</record>
        <recordMin>200</recordMin>
        <mood>1</mood>
        <stats>
          <PlantWorkSpeed>0.05</PlantWorkSpeed>
        </stats>
      </li>
      <li>
        <label>agronomist</label>
        <description>Tends every sprout with care, keeping the colony supplied with steady provisions.</description>
        <skillMin>15</skillMin>
        <record>PlantsHarvested</record>
        <recordMin>600</recordMin>
        <mood>3</mood>
        <stats>
          <PlantWorkSpeed>0.08</PlantWorkSpeed>
          <PlantHarvestYield>0.05</PlantHarvestYield>
        </stats>
      </li>
      <li>
        <label>master botanist</label>
        <description>Under their care, even the most fragile crops thrive in the harshest climate.</description>
        <skillMin>20</skillMin>
        <record>PlantsHarvested</record>
        <recordMin>1500</recordMin>
        <mood>5</mood>
        <stats>
          <PlantWorkSpeed>0.10</PlantWorkSpeed>
          <PlantHarvestYield>0.08</PlantHarvestYield>
          <WorkSpeedGlobal>0.03</WorkSpeedGlobal>
        </stats>
      </li>
    </ranks>
  </TurboMegaMod.ColonyRoleDef>
  <TurboMegaMod.ColonyRoleDef>
    <defName>TMM_Role_cook</defName>
    <label>cook</label>
    <group>civil</group>
    <skill>Cooking</skill>
    <workTypes>
      <li>Cooking</li>
    </workTypes>
    <ranks>
      <li>
        <label>kitchen assistant</label>
        <description>Quickly preps ingredients, butchers carcasses, and keeps the stew pot full.</description>
        <skillMin>10</skillMin>
        <record>MealsCooked</record>
        <recordMin>100</recordMin>
        <mood>1</mood>
        <stats>
          <CookSpeed>0.05</CookSpeed>
        </stats>
      </li>
      <li>
        <label>field chef</label>
        <description>Capable of cooking a hearty dinner even out of questionable mutant meat.</description>
        <skillMin>15</skillMin>
        <record>MealsCooked</record>
        <recordMin>300</recordMin>
        <mood>3</mood>
        <stats>
          <CookSpeed>0.08</CookSpeed>
          <ButcheryFleshSpeed>0.06</ButcheryFleshSpeed>
        </stats>
      </li>
      <li>
        <label>master chef</label>
        <description>Their meals lift the entire colony&apos;s morale better than any pre-war delicacy.</description>
        <skillMin>20</skillMin>
        <record>MealsCooked</record>
        <recordMin>750</recordMin>
        <mood>5</mood>
        <stats>
          <CookSpeed>0.10</CookSpeed>
          <ButcheryFleshSpeed>0.09</ButcheryFleshSpeed>
          <WorkSpeedGlobal>0.03</WorkSpeedGlobal>
        </stats>
      </li>
    </ranks>
  </TurboMegaMod.ColonyRoleDef>
  <TurboMegaMod.ColonyRoleDef>
    <defName>TMM_Role_trooper</defName>
    <label>trooper</label>
    <group>military</group>
    <skill>Shooting</skill>
    <workTypes>
      <li>Hunting</li>
    </workTypes>
    <ranks>
      <li>
        <label>private</label>
        <description>Completed basic drill training and holds the line under incoming fire.</description>
        <skillMin>6</skillMin>
        <record>Kills</record>
        <recordMin>10</recordMin>
        <mood>1</mood>
        <stats>
          <ShootingAccuracyPawn>0.03</ShootingAccuracyPawn>
        </stats>
      </li>
      <li>
        <label>sergeant</label>
        <description>A skirmish-hardened fighter who knows the value of cover and a fresh magazine.</description>
        <skillMin>9</skillMin>
        <record>Kills</record>
        <recordMin>30</recordMin>
        <mood>2</mood>
        <stats>
          <ShootingAccuracyPawn>0.05</ShootingAccuracyPawn>
          <MeleeHitChance>0.04</MeleeHitChance>
        </stats>
      </li>
      <li>
        <label>lieutenant</label>
        <description>A junior squad leader who keeps troopers steady through personal example.</description>
        <skillMin>12</skillMin>
        <record>Kills</record>
        <recordMin>60</recordMin>
        <mood>3</mood>
        <stats>
          <ShootingAccuracyPawn>0.06</ShootingAccuracyPawn>
          <MeleeDodgeChance>0.04</MeleeDodgeChance>
        </stats>
        <aura>
          <radius>9</radius>
          <mood>2</mood>
          <stats>
            <ShootingAccuracyPawn>0.03</ShootingAccuracyPawn>
          </stats>
        </aura>
      </li>
      <li>
        <label>major</label>
        <description>A seasoned tactician who turns a ragtag squad into a cohesive fighting unit.</description>
        <skillMin>14</skillMin>
        <record>Kills</record>
        <recordMin>100</recordMin>
        <mood>4</mood>
        <stats>
          <ShootingAccuracyPawn>0.08</ShootingAccuracyPawn>
          <MeleeHitChance>0.05</MeleeHitChance>
          <MeleeDodgeChance>0.05</MeleeDodgeChance>
        </stats>
        <aura>
          <radius>9</radius>
          <mood>3</mood>
          <stats>
            <ShootingAccuracyPawn>0.05</ShootingAccuracyPawn>
            <MeleeHitChance>0.03</MeleeHitChance>
          </stats>
        </aura>
      </li>
      <li>
        <label>colonel</label>
        <description>A formidable field commander whose voice drives soldiers to fight to the last round.</description>
        <skillMin>16</skillMin>
        <record>Kills</record>
        <recordMin>160</recordMin>
        <mood>5</mood>
        <stats>
          <ShootingAccuracyPawn>0.09</ShootingAccuracyPawn>
          <MeleeHitChance>0.07</MeleeHitChance>
          <MeleeDodgeChance>0.06</MeleeDodgeChance>
        </stats>
        <aura>
          <radius>9</radius>
          <mood>4</mood>
          <stats>
            <ShootingAccuracyPawn>0.06</ShootingAccuracyPawn>
            <MeleeHitChance>0.05</MeleeHitChance>
            <MeleeDodgeChance>0.03</MeleeDodgeChance>
          </stats>
        </aura>
      </li>
      <li>
        <label>general</label>
        <description>A living legend of the wasteland whose mere presence on the frontline fills the squad with unbreakable resolve.</description>
        <skillMin>18</skillMin>
        <record>Kills</record>
        <recordMin>250</recordMin>
        <mood>6</mood>
        <stats>
          <ShootingAccuracyPawn>0.10</ShootingAccuracyPawn>
          <MeleeHitChance>0.08</MeleeHitChance>
          <MeleeDodgeChance>0.08</MeleeDodgeChance>
        </stats>
        <aura>
          <radius>9</radius>
          <mood>6</mood>
          <stats>
            <ShootingAccuracyPawn>0.08</ShootingAccuracyPawn>
            <MeleeHitChance>0.06</MeleeHitChance>
            <MeleeDodgeChance>0.05</MeleeDodgeChance>
          </stats>
        </aura>
      </li>
    </ranks>
  </TurboMegaMod.ColonyRoleDef>
</Defs>
```