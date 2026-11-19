using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// One story quest. Pure data: mechanics live in StoryStage subclasses, outcomes in StoryEffect subclasses.
    /// Design: Documentation/STORY-ENGINE-DESIGN.md.
    public sealed class StoryQuestDef : Def
    {
        public string faction;              // FactionDef defName of the quest's faction, optional
        public int act = 1;
        public int priority;
        public bool main;                   // main-line quest: not limited by MaxActive
        public bool autoStart;              // starts without an offer letter
        public List<string> requires = new List<string>();
        public List<string> anyOf = new List<string>();
        public HeroDef giver;
        public HeroDef fallbackGiver;
        [MustTranslate] public string letterLabel;
        [MustTranslate] public string letterText;
        public float offerCooldownDays = 3f;
        public List<StoryStage> stages = new List<StoryStage>();
        public List<StoryEffect> onComplete = new List<StoryEffect>();

        public string FirstMechanic { get { return stages.Count == 0 ? "" : stages[0].Mechanic; } }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors()) yield return e;
            if (stages.Count == 0) yield return "story quest has no stages";
            for (int i = 0; i < stages.Count; i++)
                if (stages[i] == null) yield return "stage " + i + " is null (bad Class?)";
            if (!autoStart && letterText.NullOrEmpty()) yield return "offered quest needs letterText";
            var keys = new HashSet<string>();
            for (int i = 0; i < stages.Count; i++)
                if (stages[i] != null && !keys.Add(stages[i].Key(i))) yield return "duplicate stage key " + stages[i].Key(i);
            foreach (string c in requires.Concat(anyOf))
            {
                string err = StoryConditions.SyntaxError(c);
                if (err != null) yield return "condition '" + c + "': " + err;
            }
        }
    }

    /// Per-quest runtime context passed to stages and effects.
    public sealed class StoryContext
    {
        public SparkStory state;
        public StoryQuestRun run;
        public StoryQuestDef def;
        public Faction Faction { get { return StoryUtil.FactionOf(def.faction); } }
        public Map Home { get { return Find.AnyPlayerHomeMap; } }
    }

    // ---------------------------------------------------------------- stages

    public abstract class StoryStage
    {
        public string key;                  // optional stable id; saves follow it if stages are reordered
        [MustTranslate] public string label;
        [MustTranslate] public string text;

        public string Key(int index) { return key.NullOrEmpty() ? "#" + index : key; }

        /// Short mechanic id used by the director to avoid two identical mechanics in a row.
        public abstract string Mechanic { get; }

        /// Called once when the stage becomes current.
        public virtual void Start(StoryContext ctx) { }

        /// Called every director tick while current; return an outcome id to finish the stage, or null.
        public virtual string Tick(StoryContext ctx) { return null; }

        /// Next stage index for the outcome; -2 means "default" (next stage).
        public virtual int NextFor(string outcome) { return -2; }

        /// Effects to apply for the outcome.
        public virtual IEnumerable<StoryEffect> EffectsFor(string outcome) { yield break; }

        /// Text shown in the Chronicle for the current objective.
        public virtual string Objective(StoryContext ctx) { return StoryUtil.Format(label.NullOrEmpty() ? text : label, ctx); }
    }

    public sealed class StoryChoice
    {
        public string outcome;
        [MustTranslate] public string label;
        [MustTranslate] public string resultText;  // giver's reply, shown as a follow-up letter
        public int nextStage = -2;
        public List<string> requires = new List<string>();
        public List<StoryEffect> effects = new List<StoryEffect>();
    }

    /// A letter with choices. The quest waits until the player picks one.
    public sealed class StoryStage_Choice : StoryStage
    {
        public List<StoryChoice> choices = new List<StoryChoice>();
        public override string Mechanic { get { return "choice"; } }

        public override void Start(StoryContext ctx)
        {
            ctx.run.pendingChoice = true;
            StoryChoiceLetter.Send(ctx, ctx.run.stage, StoryChoiceLetter.KindChoice);
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx.run.chosenOutcome.NullOrEmpty()) return null;
            string o = ctx.run.chosenOutcome;
            ctx.run.chosenOutcome = null;
            ctx.run.pendingChoice = false;
            return o;
        }

        public StoryChoice ChoiceFor(string outcome) { return choices.FirstOrDefault(c => c.outcome == outcome); }
        public override int NextFor(string outcome) { StoryChoice c = ChoiceFor(outcome); return c == null ? -2 : c.nextStage; }
        public override IEnumerable<StoryEffect> EffectsFor(string outcome) { StoryChoice c = ChoiceFor(outcome); return c == null ? Enumerable.Empty<StoryEffect>() : c.effects; }
    }

    /// A timed pause that always arrives with a letter (dream, rumour, radio intercept) so waiting is never empty.
    public sealed class StoryStage_Wait : StoryStage
    {
        public float days = 1f;
        public List<StoryEffect> effects = new List<StoryEffect>();
        public override string Mechanic { get { return "wait"; } }

        public override void Start(StoryContext ctx)
        {
            ctx.run.stageUntilTick = Find.TickManager.TicksGame + Mathf.RoundToInt(days * GenDate.TicksPerDay);
        }

        public override string Tick(StoryContext ctx)
        {
            if (Find.TickManager.TicksGame < ctx.run.stageUntilTick) return null;
            if (!text.NullOrEmpty())
                Find.LetterStack.ReceiveLetter(StoryUtil.Format(label, ctx), StoryUtil.Format(text, ctx), LetterDefOf.NeutralEvent);
            return "done";
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome) { return effects; }
        public override string Objective(StoryContext ctx)
        {
            float left = Mathf.Max(0f, (ctx.run.stageUntilTick - Find.TickManager.TicksGame) / (float)GenDate.TicksPerDay);
            return StoryUtil.Format(label, ctx) + " (" + "TMM.Story.DaysLeft".Translate(left.ToString("0.0")) + ")";
        }
    }

    /// A hero joins the colony by drop pod at the home map (defectors, rescued heroes).
    /// Outcomes: "joined" (arrived, or already the player's), "unavailable" (dead, held by someone, on a map or in a caravan).
    public sealed class StoryStage_HeroJoins : StoryStage
    {
        public HeroDef hero;
        public List<StoryChoice> outcomes = new List<StoryChoice>();
        public override string Mechanic { get { return "heroJoins"; } }

        public override string Tick(StoryContext ctx) { return StoryEffect_HeroJoins.TryJoin(hero, ctx) ? "joined" : "unavailable"; }
        public override int NextFor(string outcome) { StoryChoice c = outcomes.FirstOrDefault(x => x.outcome == outcome); return c == null ? -2 : c.nextStage; }
        public override IEnumerable<StoryEffect> EffectsFor(string outcome) { StoryChoice c = outcomes.FirstOrDefault(x => x.outcome == outcome); return c == null ? Enumerable.Empty<StoryEffect>() : c.effects; }
    }

    // ---------------------------------------------------------------- effects

    public abstract class StoryEffect
    {
        public abstract void Apply(StoryContext ctx);
    }

    public sealed class StoryEffect_SetFlag : StoryEffect
    {
        public string flag;
        public string value = "true";
        public override void Apply(StoryContext ctx) { ctx.state.SetFlag(flag, value); }
    }

    public sealed class StoryEffect_AddInt : StoryEffect
    {
        public string flag;
        public int amount = 1;
        public override void Apply(StoryContext ctx) { ctx.state.SetFlag(flag, (ctx.state.GetInt(flag) + amount).ToString()); }
    }

    public sealed class StoryEffect_Goodwill : StoryEffect
    {
        public string faction;  // FactionDef defName; empty = the quest's faction
        public int amount;
        public override void Apply(StoryContext ctx)
        {
            Faction f = StoryUtil.FactionOf(faction.NullOrEmpty() ? ctx.def.faction : faction);
            if (f != null && !f.IsPlayer) f.TryAffectGoodwillWith(Faction.OfPlayer, amount, true, true, null, null);
        }
    }

    public sealed class StoryEffect_GiveThing : StoryEffect
    {
        public ThingDef thing;
        public int count = 1;
        public override void Apply(StoryContext ctx)
        {
            Map map = ctx.Home;
            if (map == null || thing == null || count <= 0) return;
            var things = new List<Thing>();
            int left = count;
            while (left > 0)
            {
                Thing t = ThingMaker.MakeThing(thing, thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null);
                t.stackCount = Mathf.Min(left, thing.stackLimit);
                left -= t.stackCount;
                things.Add(t);
            }
            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, things, 110, false, false, true, false);
        }
    }

    public sealed class StoryEffect_HeroJoins : StoryEffect
    {
        public HeroDef hero;
        public override void Apply(StoryContext ctx) { TryJoin(hero, ctx); }

        /// Safe ownership transfer: only a free living world pawn is moved; the drop pod must really take it, otherwise
        /// everything is rolled back. A hero who already belongs to the player counts as joined (idempotent).
        public static bool TryJoin(HeroDef hero, StoryContext ctx)
        {
            Map map = ctx.Home;
            HeroRegistry reg = HeroRegistry.Instance;
            if (map == null || reg == null || hero == null) return false;
            Pawn p = reg.GetOrCreate(hero);
            if (p == null || p.Dead || p.Destroyed) return false;
            if (p.Faction == Faction.OfPlayer && !p.IsPrisoner) { MarkJoined(hero, p, ctx, reg); return true; }
            if (p.Spawned || p.IsPrisoner || p.IsSlave || p.holdingOwner != null || p.GetCaravan() != null || !Find.WorldPawns.Contains(p)) return false;

            Faction oldFaction = p.Faction;
            bool wasLeader = oldFaction != null && oldFaction.leader == p;
            Find.WorldPawns.RemovePawn(p);
            p.SetFaction(Faction.OfPlayer);
            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, new List<Thing> { p }, 110, false, false, true, false);
            if (p.holdingOwner == null && !p.Spawned)
            {
                // The pod did not accept the pawn: restore everything.
                if (oldFaction != null) p.SetFaction(oldFaction);
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                if (wasLeader) oldFaction.leader = p;
                return false;
            }
            if (wasLeader)
            {
                Pawn next = reg.SuccessorForVacantLeadership(oldFaction);
                oldFaction.leader = next;
                if (next == null) oldFaction.TryGenerateNewLeader();
            }
            MarkJoined(hero, p, ctx, reg);
            return true;
        }

        private static void MarkJoined(HeroDef hero, Pawn p, StoryContext ctx, HeroRegistry reg)
        {
            reg.NotifyEncountered(p);
            ctx.state.SetFlag("defector." + hero.defName, "true");
        }
    }

    public sealed class StoryEffect_Letter : StoryEffect
    {
        [MustTranslate] public string label;
        [MustTranslate] public string text;
        public override void Apply(StoryContext ctx)
        {
            Find.LetterStack.ReceiveLetter(StoryUtil.Format(label, ctx), StoryUtil.Format(text, ctx), LetterDefOf.NeutralEvent);
        }
    }

    // ---------------------------------------------------------------- conditions and helpers

    public static class StoryConditions
    {
        /// All of `all` must hold; if `any` is non-empty, at least one of it must hold too.
        public static bool Holds(SparkStory s, List<string> all, List<string> any)
        {
            if (all != null) foreach (string c in all) if (!Eval(s, c)) return false;
            if (any != null && any.Count > 0 && !any.Any(c => Eval(s, c))) return false;
            return true;
        }

        public static bool Eval(SparkStory s, string cond) { return EvalCore(s, cond) == 1; }

        private static readonly string[] Prefixes = { "done", "active", "heroAlive", "factionExists" };

        /// 1 true, 0 false, -1 syntax error (an error stays false even under negation).
        private static int EvalCore(SparkStory s, string cond)
        {
            if (cond == null) return 1;
            cond = cond.Trim();
            if (cond.Length == 0) return -1;
            if (cond[0] == '!') { int inner = EvalCore(s, cond.Substring(1)); return inner < 0 ? -1 : 1 - inner; }
            string flag, op, val, kind, arg;
            int parsed = Parse(cond, out flag, out op, out val, out kind, out arg);
            if (parsed < 0) { Log.ErrorOnce("[TMM] Story: bad condition '" + cond + "'", cond.GetHashCode()); return -1; }
            if (kind != null)
            {
                if (kind == "done") return s.IsDone(arg) ? 1 : 0;
                if (kind == "active") return s.IsActive(arg) ? 1 : 0;
                if (kind == "heroAlive") return StoryUtil.HeroAlive(arg) ? 1 : 0;
                return StoryUtil.FactionOf(arg) != null ? 1 : 0;
            }
            if (op == null) return s.GetFlag(flag) == "true" ? 1 : 0; // bare flag: boolean
            string have = s.GetFlag(flag);
            if (op == "=" || op == "==") return have == val ? 1 : 0;
            if (op == "!=") return have != val ? 1 : 0;
            int a = s.GetInt(flag), b = int.Parse(val);
            if (op == ">=") return a >= b ? 1 : 0;
            if (op == "<=") return a <= b ? 1 : 0;
            if (op == ">") return a > b ? 1 : 0;
            return a < b ? 1 : 0;
        }

        /// Splits at the FIRST operator character from the left (longest operator there), so values may contain = ! &lt; &gt;.
        private static int Parse(string cond, out string flag, out string op, out string val, out string kind, out string arg)
        {
            flag = op = val = kind = arg = null;
            int i = cond.IndexOfAny(new[] { '=', '!', '<', '>' });
            int colon = cond.IndexOf(':');
            if (colon > 0 && (i < 0 || colon < i))
            {
                kind = cond.Substring(0, colon).Trim();
                arg = cond.Substring(colon + 1).Trim();
                return Prefixes.Contains(kind) && arg.Length > 0 ? 0 : -1;
            }
            if (i < 0) { flag = cond; return 0; }
            if (i == 0) return -1;
            string two = i + 1 < cond.Length ? cond.Substring(i, 2) : "";
            op = two == ">=" || two == "<=" || two == "!=" || two == "==" ? two : cond.Substring(i, 1);
            if (op == "!") return -1;
            flag = cond.Substring(0, i).Trim();
            val = cond.Substring(i + op.Length).Trim();
            int n;
            if ((op == ">=" || op == "<=" || op == ">" || op == "<") && !int.TryParse(val, out n)) return -1;
            return flag.Length > 0 ? 0 : -1;
        }

        /// Load-time validation used by StoryQuestDef.ConfigErrors.
        public static string SyntaxError(string cond)
        {
            if (cond == null) return "null";
            string c = cond.Trim();
            while (c.StartsWith("!")) c = c.Substring(1).Trim();
            string flag, op, val, kind, arg;
            return c.Length == 0 || Parse(c, out flag, out op, out val, out kind, out arg) < 0 ? "unknown prefix, empty part or non-numeric comparison" : null;
        }
    }

    public static class StoryUtil
    {
        public static Faction FactionOf(string factionDefName)
        {
            if (factionDefName.NullOrEmpty() || Find.FactionManager == null) return null;
            FactionDef fd = DefDatabase<FactionDef>.GetNamedSilentFail(factionDefName);
            return fd == null ? null : Find.FactionManager.FirstFactionOfDef(fd);
        }

        public static bool HeroAlive(string heroDefName)
        {
            HeroDef h = DefDatabase<HeroDef>.GetNamedSilentFail(heroDefName);
            HeroRegistry reg = HeroRegistry.Instance;
            if (h == null || reg == null) return false;
            HeroRecord r = reg.RecordFor(h);
            return r == null || (r.state != HeroState.Dead && r.state != HeroState.Missing);
        }

        /// Who signs the quest: giver, then fallback giver, then the faction leader; null means "the faction".
        public static string GiverName(StoryQuestDef def)
        {
            foreach (HeroDef h in new[] { def.giver, def.fallbackGiver })
            {
                if (h == null || !HeroAlive(h.defName)) continue;
                return h.label;
            }
            Faction f = FactionOf(def.faction);
            if (f != null && f.leader != null && !f.leader.Dead) return f.leader.LabelShort;
            return f == null ? "?" : f.Name;
        }

        public static string Format(string text, StoryContext ctx)
        {
            if (text.NullOrEmpty()) return "";
            Faction f = ctx == null ? null : ctx.Faction;
            string colony = Faction.OfPlayer == null ? "" : Faction.OfPlayer.Name;
            return text.Replace("{GIVER}", ctx == null ? "" : GiverName(ctx.def))
                       .Replace("{FACTION}", f == null ? "" : f.Name)
                       .Replace("{COLONY}", colony);
        }
    }
}
