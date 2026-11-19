using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public enum StoryDeliverTarget
    {
        colony,
        giverSettlement
    }

    /// <summary>
    /// Reminder/hand-in ChoiceLetter for StoryStage_Deliver when delivering directly
    /// from the colony's stockpiles (to = colony). Persists cleanly across save/reload.
    /// </summary>
    public sealed class ChoiceLetter_StoryDeliver : ChoiceLetter
    {
        public string questDefName;
        public int runGen;
        public string thingDefName;
        public int requiredCount;

        public override bool CanDismissWithRightClick
        {
            get { return false; }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                Map home = Find.AnyPlayerHomeMap;
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                int have = (home != null && def != null) ? OutpostPodLogistics.CountResource(home, def) : 0; // CHECK API

                string itemLabel = def != null ? def.label : thingDefName;
                string deliverLabel = string.Format("Deliver {0} x{1} ({2}/{3})", itemLabel, requiredCount, have, requiredCount);
                DiaOption deliver = new DiaOption(deliverLabel);

                if (home == null || def == null || have < requiredCount)
                {
                    deliver.Disable(string.Format("Need {0} {1} in colony stockpiles ({2}/{0})", requiredCount, itemLabel, have));
                }
                else
                {
                    deliver.action = delegate
                    {
                        StoryQuestRun run = StoryStage_Deliver.FindQuestRun(questDefName);
                        if (run == null || run.gen != runGen)
                        {
                            Find.LetterStack.RemoveLetter(this);
                            return;
                        }
                        if (run.vars.ContainsKey("delivered") && run.vars["delivered"] == "true")
                        {
                            Find.LetterStack.RemoveLetter(this);
                            return;
                        }

                        int currentHave = OutpostPodLogistics.CountResource(home, def); // CHECK API
                        if (currentHave < requiredCount) return;

                        int taken = OutpostPodLogistics.DeductResource(home, def, requiredCount); // CHECK API
                        if (taken >= requiredCount)
                        {
                            run.vars["delivered"] = "true";
                            Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    deliver.resolveTree = true;
                }

                yield return deliver;
                yield return Option_Postpone; // CHECK API
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref thingDefName, "thingDefName", null);
            Scribe_Values.Look(ref requiredCount, "requiredCount", 1);
        }
    }

    /// <summary>
    /// Quest stage requiring delivery of a vanilla ThingDef either from home-map stockpiles
    /// (via a ChoiceLetter hand-in button) or by bringing a player caravan to the nearest
    /// settlement of the quest faction before timeoutDays.
    /// </summary>
    public sealed class StoryStage_Deliver : StoryStage
    {
        public ThingDef thing;
        public int count = 1;
        public StoryDeliverTarget to = StoryDeliverTarget.colony;
        public float timeoutDays = 10f;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "deliver"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            int now = Find.TickManager.TicksGame;
            int deadline = now + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["deadlineTick"] = deadline.ToString();
            ctx.run.vars["delivered"] = "false";

            if (to == StoryDeliverTarget.colony)
            {
                SendColonyDeliverLetter(ctx);
            }
            else
            {
                Settlement target = FindNearestQuestSettlement(ctx);
                if (target != null)
                {
                    ctx.run.vars["settlementId"] = target.ID.ToString();
                    ctx.run.vars["settlementName"] = target.LabelCap.ToString();
                    string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Caravan Delivery"), ctx);
                    string body = BuildSettlementLetterText(ctx, target);
                    Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, new LookTargets(target)); // CHECK API
                }
                else
                {
                    // Fallback to colony hand-in if no settlement exists for the quest faction
                    Log.Warning("[TMM] StoryStage_Deliver: no settlement found for giverSettlement delivery; falling back to colony delivery.");
                    ctx.run.vars["fallbackColony"] = "true";
                    SendColonyDeliverLetter(ctx);
                }
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now + GenDate.TicksPerDay);

            if (GetBool(ctx, "delivered"))
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "success";
                return "success";
            }

            bool colonyMode = (to == StoryDeliverTarget.colony) || GetBool(ctx, "fallbackColony");
            if (colonyMode)
            {
                if (now >= deadline)
                {
                    RemoveDeliverLetters(ctx);
                    ctx.run.vars["finished"] = "timeout";
                    return "timeout";
                }

                // Re-send reminder letter if it was somehow lost
                if (!HasActiveDeliverLetter(ctx))
                {
                    SendColonyDeliverLetter(ctx);
                }
                return null;
            }

            // Caravan delivery to giverSettlement
            int settlementId = GetInt(ctx, "settlementId", -1);
            Settlement target = FindSettlementById(settlementId);
            if (target != null && thing != null && TryCompleteCaravanDelivery(target, thing, count))
            {
                ctx.run.vars["delivered"] = "true";
                ctx.run.vars["finished"] = "success";
                string doneTitle = StoryUtil.Format(!label.NullOrEmpty() ? label : "Delivery Complete", ctx);
                string doneBody = string.Format("Your caravan delivered {0} x{1} to {2}.", thing.label, count, target.LabelCap);
                Find.LetterStack.ReceiveLetter(doneTitle, doneBody, LetterDefOf.PositiveEvent, new LookTargets(target)); // CHECK API
                return "success";
            }

            if (now >= deadline)
            {
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string itemLabel = thing != null ? thing.label : "resources";
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", itemLabel, count), ctx);

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            bool colonyMode = (to == StoryDeliverTarget.colony) || GetBool(ctx, "fallbackColony");
            if (colonyMode)
            {
                int have = (ctx.Home != null && thing != null) ? OutpostPodLogistics.CountResource(ctx.Home, thing) : 0; // CHECK API
                return string.Format("{0} ({1}/{2} {3}, {4:0.0}d left)", baseText, have, count, itemLabel, daysLeft);
            }

            string targetName = GetStr(ctx, "settlementName");
            if (targetName.NullOrEmpty()) targetName = "faction settlement";
            return string.Format("{0} to {1} ({2:0.0}d left)", baseText, targetName, daysLeft);
        }

        private void SendColonyDeliverLetter(StoryContext ctx)
        {
            if (Find.LetterStack == null || thing == null) return;

            ChoiceLetter_StoryDeliver letter = new ChoiceLetter_StoryDeliver();
            letter.def = LetterDefOf.NeutralEvent;
            letter.Label = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", thing.label, count), ctx);
            letter.Text = BuildColonyLetterText(ctx);
            letter.questDefName = ctx.def != null ? ctx.def.defName : string.Empty;
            letter.runGen = ctx.run.gen;
            letter.thingDefName = thing.defName;
            letter.requiredCount = Mathf.Max(1, count);
            letter.lookTargets = ctx.Home != null ? new LookTargets(ctx.Home.Center, ctx.Home) : LookTargets.Invalid;

            Find.LetterStack.ReceiveLetter(letter); // CHECK API
        }

        private string BuildColonyLetterText(StoryContext ctx)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Gather {0} x{1} in {2}'s stockpiles within {3:0.0} days, then open this letter and click Deliver.",
                itemLabel, count, "{COLONY}", timeoutDays);
            instruction = StoryUtil.Format(instruction, ctx);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private string BuildSettlementLetterText(StoryContext ctx, Settlement target)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Send a caravan carrying {0} x{1} to {2} within {3:0.0} days.",
                itemLabel, count, target.LabelCap, timeoutDays);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private static bool HasActiveDeliverLetter(StoryContext ctx)
        {
            if (Find.LetterStack == null || ctx == null || ctx.def == null || ctx.run == null) return false;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = 0; i < letters.Count; i++)
            {
                ChoiceLetter_StoryDeliver dl = letters[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == ctx.def.defName && dl.runGen == ctx.run.gen) return true;
            }
            return false;
        }

        private static void RemoveDeliverLetters(StoryContext ctx)
        {
            if (Find.LetterStack == null || ctx == null || ctx.def == null || ctx.run == null) return;
            List<Letter> snapshot = new List<Letter>(Find.LetterStack.LettersListForReading);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ChoiceLetter_StoryDeliver dl = snapshot[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == ctx.def.defName && dl.runGen == ctx.run.gen)
                {
                    Find.LetterStack.RemoveLetter(dl);
                }
            }
        }

        private static Settlement FindNearestQuestSettlement(StoryContext ctx)
        {
            Faction targetFac = ctx.Faction;
            PlanetTile homeTile = ResolveHomeTile(ctx); // CHECK API

            Settlement bestSameFaction = null;
            float bestSameDist = float.MaxValue;
            Settlement bestFriendly = null;
            float bestFriendlyDist = float.MaxValue;

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s == null || s.Destroyed || s.Faction == null || s.Faction.IsPlayer) continue;

                float dist = homeTile.Valid ? Find.WorldGrid.TraversalDistanceBetween(homeTile, s.Tile) : 0f; // CHECK API
                if (targetFac != null && s.Faction == targetFac)
                {
                    if (dist < bestSameDist)
                    {
                        bestSameDist = dist;
                        bestSameFaction = s;
                    }
                }
                else if (!s.Faction.HostileTo(Faction.OfPlayer))
                {
                    if (dist < bestFriendlyDist)
                    {
                        bestFriendlyDist = dist;
                        bestFriendly = s;
                    }
                }
            }

            return bestSameFaction ?? bestFriendly;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0) return null;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s != null && !s.Destroyed && s.ID == id) return s;
            }
            return null;
        }

        private static PlanetTile ResolveHomeTile(StoryContext ctx) // CHECK API
        {
            if (ctx.Home != null) return ctx.Home.Tile; // CHECK API
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (settlements[i] != null && settlements[i].Faction == Faction.OfPlayer)
                {
                    return settlements[i].Tile; // CHECK API
                }
            }
            return PlanetTile.Invalid; // CHECK API
        }

        private static bool TryCompleteCaravanDelivery(Settlement target, ThingDef targetDef, int requiredCount)
        {
            if (target == null || targetDef == null || requiredCount <= 0) return false;

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Destroyed || c.Faction != Faction.OfPlayer) continue;
                if (c.Tile != target.Tile) continue;

                List<Thing> items = CaravanInventoryUtility.AllInventoryItems(c); // CHECK API
                if (items == null) continue;

                int total = 0;
                for (int j = 0; j < items.Count; j++)
                {
                    Thing t = items[j];
                    if (t != null && !t.Destroyed && t.def == targetDef)
                    {
                        total += t.stackCount;
                    }
                }

                if (total < requiredCount) continue;

                int remaining = requiredCount;
                List<Thing> copy = new List<Thing>(items);
                for (int j = 0; j < copy.Count; j++)
                {
                    Thing t = copy[j];
                    if (t == null || t.Destroyed || t.def != targetDef) continue;
                    int take = Mathf.Min(remaining, t.stackCount);
                    Thing split = t.SplitOff(take);
                    if (split != null && !split.Destroyed)
                    {
                        split.Destroy(DestroyMode.Vanish);
                    }
                    remaining -= take;
                    if (remaining <= 0) break;
                }
                return remaining <= 0;
            }

            return false;
        }

        internal static StoryQuestRun FindQuestRun(string questDefName)
        {
            SparkStory story = SparkStory.Instance;
            if (story == null || questDefName.NullOrEmpty()) return null;

            // CHECK API: verify exact SparkStory run lookup method (e.g., story.GetRun or story.runs)
            Type t = story.GetType();
            MethodInfo getRun = t.GetMethod("GetRun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getRun != null)
            {
                return getRun.Invoke(story, new object[] { questDefName }) as StoryQuestRun;
            }

            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField != null)
            {
                object val = runsField.GetValue(story);
                Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
                if (dict != null)
                {
                    StoryQuestRun r;
                    if (dict.TryGetValue(questDefName, out r)) return r;
                }
                List<StoryQuestRun> list = val as List<StoryQuestRun>;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && list[i].quest == questDefName) return list[i];
                    }
                }
            }
            return null;
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            string raw;
            int val;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
        }

        private static bool GetBool(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }
    }
}
