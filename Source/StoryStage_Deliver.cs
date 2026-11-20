using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
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

    // =========================================================================
    // COLONY HAND-IN CHOICE LETTER (VALIDATED & ATOMIC)
    // =========================================================================

    /// <summary>
    /// Reminder/hand-in ChoiceLetter for StoryStage_Deliver (to = colony).
    /// Before EVERY deduction (including immediately after reload prior to first Reconcile),
    /// verifies: active run, matching stageKey &amp; runGen, letter still in stack, pinned map
    /// from vars (never a closure-captured map), deadline not elapsed, and atomic StoryPayment.
    /// </summary>
    public sealed class ChoiceLetter_StoryDeliver : ChoiceLetter
    {
        public string questDefName;
        public string stageKey;
        public int runGen;
        public int deliverMapId = -1;
        public string thingDefName;
        public int requiredCount = 1;
        public int deadlineTick = -1;
        private bool resolved;

        public ChoiceLetter_StoryDeliver()
        {
            if (Find.UniqueIDsManager != null)
            {
                ID = Find.UniqueIDsManager.GetNextLetterID();
            }
        }

        public override bool CanDismissWithRightClick
        {
            get { return false; }
        }

        private bool InStack
        {
            get { return Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(this); }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                StoryQuestRun run;
                Map pinnedMap;
                ThingDef def;
                string invalidReason;

                bool valid = StoryStage_Deliver.ValidateColonyHandIn(
                    this, out run, out pinnedMap, out def, out invalidReason);

                if (!valid)
                {
                    DiaOption close = new DiaOption("Close".Translate());
                    close.action = delegate
                    {
                        resolved = true;
                        if (InStack) Find.LetterStack.RemoveLetter(this);
                    };
                    close.resolveTree = true;
                    yield return close;
                    yield break;
                }

                int have = StoryPayment.CountInColonyStockpiles(pinnedMap, def);
                string itemLabel = def != null ? def.label : thingDefName;
                string deliverLabel = string.Format("Deliver {0} x{1} ({2}/{3})", itemLabel, requiredCount, have, requiredCount);
                DiaOption deliver = new DiaOption(deliverLabel);

                if (have < requiredCount)
                {
                    deliver.Disable(string.Format("Need {0} {1} in colony storage on {2} ({3}/{0})",
                        requiredCount, itemLabel, pinnedMap.Parent != null ? pinnedMap.Parent.LabelCap.ToString() : "colony", have));
                }
                else
                {
                    deliver.action = delegate
                    {
                        // Full re-validation at exact click time (no stale closures!)
                        StoryQuestRun clickRun;
                        Map clickMap;
                        ThingDef clickDef;
                        string clickReason;
                        if (!StoryStage_Deliver.ValidateColonyHandIn(this, out clickRun, out clickMap, out clickDef, out clickReason))
                        {
                            resolved = true;
                            if (InStack) Find.LetterStack.RemoveLetter(this);
                            return;
                        }

                        int deducted;
                        if (StoryPayment.TryDeductFromColonyStockpiles(clickMap, clickDef, requiredCount, out deducted) &&
                            deducted == requiredCount)
                        {
                            clickRun.vars["delivered"] = "true";
                            resolved = true;
                            if (InStack) Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    deliver.resolveTree = true;
                }

                yield return deliver;
                yield return Option_Postpone; // CHECK API
            }
        }

        public bool IsResolved
        {
            get { return resolved; }
        }

        public bool IsInLetterStack
        {
            get { return InStack; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref stageKey, "stageKey", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref deliverMapId, "deliverMapId", -1);
            Scribe_Values.Look(ref thingDefName, "thingDefName", null);
            Scribe_Values.Look(ref requiredCount, "requiredCount", 1);
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", -1);
            Scribe_Values.Look(ref resolved, "resolved", false);
        }
    }

    // =========================================================================
    // CARAVAN ARRIVAL ACTION FOR giverSettlement (EXPLICIT ARRIVAL ONLY)
    // =========================================================================

    /// <summary>
    /// Explicit CaravanArrivalAction for StoryStage_Deliver (to = giverSettlement).
    /// Passing caravans are NEVER deducted; cargo is only deducted when a caravan arrives
    /// with this arrival action or when the player clicks the explicit Deliver gizmo at the settlement.
    /// </summary>
    public sealed class CaravanArrivalAction_StoryDeliver : CaravanArrivalAction
    {
        public Settlement settlement;
        public string questDefName;
        public string stageKey;
        public int runGen;

        public CaravanArrivalAction_StoryDeliver()
        {
        }

        public CaravanArrivalAction_StoryDeliver(Settlement settlement, string questDefName, string stageKey, int runGen)
        {
            this.settlement = settlement;
            this.questDefName = questDefName;
            this.stageKey = stageKey;
            this.runGen = runGen;
        }

        public override string Label
        {
            get
            {
                return string.Format("Deliver quest cargo to {0}",
                    settlement != null ? settlement.LabelCap.ToString() : "settlement");
            }
        }

        public override string ReportString
        {
            get
            {
                return string.Format("Delivering quest cargo to {0}.",
                    settlement != null ? settlement.LabelCap.ToString() : "settlement");
            }
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile) // CHECK API
        {
            FloatMenuAcceptanceReport baseRep = base.StillValid(caravan, destinationTile);
            if (!baseRep) return baseRep;
            if (settlement == null || settlement.Destroyed || settlement.Tile != destinationTile) return false;
            return StoryStage_Deliver.CanCaravanDeliver(caravan, settlement, questDefName, stageKey, runGen);
        }

        public override void Arrived(Caravan caravan)
        {
            StoryStage_Deliver.TryCompleteCaravanHandIn(caravan, settlement, questDefName, stageKey, runGen);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref stageKey, "stageKey", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
        }
    }

    // =========================================================================
    // STORY STAGE: DELIVER
    // =========================================================================

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

            if (thing == null || count <= 0)
            {
                ctx.run.vars["failReason"] = "invalid_deliver_def";
                ctx.run.vars["immediateOutcome"] = "failed";
                return;
            }

            if (to == StoryDeliverTarget.colony)
            {
                Map home = ctx.Home;
                if (home == null)
                {
                    ctx.run.vars["failReason"] = "no_home_map";
                    ctx.run.vars["immediateOutcome"] = "failed";
                    return;
                }
                ctx.run.vars["deliverMapId"] = home.uniqueID.ToString();
                SendColonyDeliverLetter(ctx, home, deadline);
            }
            else
            {
                // Strict giverSettlement: ONLY a settlement belonging to the quest's exact faction (no fallback!)
                Settlement target = FindNearestExactFactionSettlement(ctx);
                if (target == null)
                {
                    Log.Warning("[TMM] StoryStage_Deliver: no settlement belonging to quest faction '" +
                                (ctx.def != null ? ctx.def.faction : "null") + "'. Failing explicitly without colony fallback.");
                    ctx.run.vars["failReason"] = "no_faction_settlement";
                    ctx.run.vars["immediateOutcome"] = FindOutcome("unavailable") != null ? "unavailable" : "failed";
                    return;
                }

                ctx.run.vars["settlementId"] = target.ID.ToString();
                ctx.run.vars["settlementFactionId"] = target.Faction != null ? target.Faction.loadID.ToString() : "-1";
                ctx.run.vars["settlementName"] = target.LabelCap.ToString();

                string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Caravan Delivery"), ctx);
                string body = BuildSettlementLetterText(ctx, target);
                Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, new LookTargets(target)); // CHECK API
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            string immediate = GetStr(ctx.run, "immediateOutcome");
            if (!immediate.NullOrEmpty())
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = immediate;
                return immediate;
            }

            // 1. If payment was already accepted on time (including across save/reload), finish cleanly without re-deducting!
            if (GetBool(ctx.run, "delivered"))
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "success";
                return "success";
            }

            int now = Find.TickManager.TicksGame;
            int deadline;
            if (!TryGetInt(ctx.run, "deadlineTick", out deadline))
            {
                deadline = now + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));
                ctx.run.vars["deadlineTick"] = deadline.ToString();
            }

            // 2. Check deadline FIRST
            if (now >= deadline)
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            if (to == StoryDeliverTarget.colony)
            {
                int mapId = GetInt(ctx.run, "deliverMapId", -1);
                Map pinnedMap = FindMapByUniqueId(mapId);
                if (pinnedMap == null || !pinnedMap.IsPlayerHome)
                {
                    RemoveDeliverLetters(ctx);
                    ctx.run.vars["failReason"] = "deliver_map_lost";
                    ctx.run.vars["finished"] = "failed";
                    return "failed";
                }

                if (!HasActiveDeliverLetter(ctx))
                {
                    SendColonyDeliverLetter(ctx, pinnedMap, deadline);
                }
                return null;
            }

            // 3. giverSettlement mode: verify target settlement still exists & still belongs to the required faction
            // NOTE: Passing caravans are NEVER auto-deducted here! Delivery happens via CaravanArrivalAction_StoryDeliver
            // or the explicit caravan Deliver gizmo.
            int settlementId = GetInt(ctx.run, "settlementId", -1);
            int expectedFacId = GetInt(ctx.run, "settlementFactionId", -1);
            Settlement target = FindSettlementById(settlementId);
            if (target == null || target.Destroyed || target.Faction == null || target.Faction.loadID != expectedFacId)
            {
                ctx.run.vars["failReason"] = "settlement_gone";
                ctx.run.vars["finished"] = "failed";
                return "failed";
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string itemLabel = thing != null ? thing.label : "resources";
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", itemLabel, count), ctx);

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx.run, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            if (to == StoryDeliverTarget.colony)
            {
                Map pinnedMap = FindMapByUniqueId(GetInt(ctx.run, "deliverMapId", -1));
                int have = (pinnedMap != null && thing != null) ? StoryPayment.CountInColonyStockpiles(pinnedMap, thing) : 0;
                return string.Format("{0} ({1}/{2} {3}, {4:0.0}d left)", baseText, have, count, itemLabel, daysLeft);
            }

            string targetName = GetStr(ctx.run, "settlementName");
            if (targetName.NullOrEmpty()) targetName = "faction settlement";
            return string.Format("{0} to {1} ({2:0.0}d left)", baseText, targetName, daysLeft);
        }

        // =====================================================================
        // SHARED VALIDATION FOR COLONY LETTER & CARAVAN HAND-IN
        // =====================================================================

        /// <summary>
        /// Validates every precondition for a colony letter click (safe even immediately after load
        /// before SparkStory.Reconcile has run).
        /// </summary>
        internal static bool ValidateColonyHandIn(
            ChoiceLetter_StoryDeliver letter,
            out StoryQuestRun run,
            out Map pinnedMap,
            out ThingDef def,
            out string reason)
        {
            run = null;
            pinnedMap = null;
            def = null;
            reason = null;

            if (letter == null || letter.IsResolved || !letter.IsInLetterStack)
            {
                reason = "letter_not_active";
                return false;
            }

            StoryStage_Deliver stageDef;
            if (!ValidateActiveDeliverStage(letter.questDefName, letter.stageKey, letter.runGen, StoryDeliverTarget.colony, out run, out stageDef, out reason))
            {
                return false;
            }

            int now = Find.TickManager.TicksGame;
            int dl = GetInt(run, "deadlineTick", letter.deadlineTick);
            if (dl >= 0 && now >= dl)
            {
                reason = "deadline_passed";
                return false;
            }

            int mapId = GetInt(run, "deliverMapId", letter.deliverMapId);
            pinnedMap = FindMapByUniqueId(mapId);
            if (pinnedMap == null || !pinnedMap.IsPlayerHome)
            {
                reason = "pinned_map_missing";
                return false;
            }

            def = stageDef.thing ?? DefDatabase<ThingDef>.GetNamedSilentFail(letter.thingDefName);
            if (def == null)
            {
                reason = "thing_def_missing";
                return false;
            }

            return true;
        }

        internal static bool ValidateActiveDeliverStage(
            string questDefName,
            string expectedStageKey,
            int expectedGen,
            StoryDeliverTarget expectedTarget,
            out StoryQuestRun run,
            out StoryStage_Deliver stageDef,
            out string reason)
        {
            run = null;
            stageDef = null;
            reason = null;

            SparkStory story = SparkStory.Instance;
            if (story == null || questDefName.NullOrEmpty())
            {
                reason = "no_story";
                return false;
            }

            run = story.RunFor(questDefName);
            if (run == null || run.status != StoryQuestStatus.Active)
            {
                reason = "run_not_active";
                return false;
            }

            if (run.gen != expectedGen)
            {
                reason = "gen_mismatch";
                return false;
            }

            if (!expectedStageKey.NullOrEmpty() && run.stageKey != expectedStageKey)
            {
                reason = "stage_key_mismatch";
                return false;
            }

            if (run.vars.ContainsKey("finished") || GetBool(run, "delivered"))
            {
                reason = "already_delivered_or_finished";
                return false;
            }

            StoryQuestDef def = run.Def;
            if (def == null || def.stages == null)
            {
                reason = "def_missing";
                return false;
            }

            // Resolve stage by stable stageKey first so validation works even before Reconcile() remaps run.stage!
            StoryStage foundStage = null;
            if (!run.stageKey.NullOrEmpty())
            {
                for (int i = 0; i < def.stages.Count; i++)
                {
                    if (def.stages[i] != null && def.stages[i].Key(i) == run.stageKey)
                    {
                        foundStage = def.stages[i];
                        break;
                    }
                }
            }
            if (foundStage == null && run.stage >= 0 && run.stage < def.stages.Count)
            {
                foundStage = def.stages[run.stage];
            }

            stageDef = foundStage as StoryStage_Deliver;
            if (stageDef == null || stageDef.to != expectedTarget)
            {
                reason = "wrong_stage_type";
                return false;
            }

            return true;
        }

        public static bool CanCaravanDeliver(Caravan caravan, Settlement settlement, string questDefName, string expectedStageKey, int expectedGen)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return false;
            if (settlement == null || settlement.Destroyed) return false;

            StoryQuestRun run;
            StoryStage_Deliver stageDef;
            string reason;
            if (!ValidateActiveDeliverStage(questDefName, expectedStageKey, expectedGen, StoryDeliverTarget.giverSettlement, out run, out stageDef, out reason))
            {
                return false;
            }

            int now = Find.TickManager.TicksGame;
            int dl = GetInt(run, "deadlineTick", -1);
            if (dl >= 0 && now >= dl) return false;

            int expectedSettlementId = GetInt(run, "settlementId", -1);
            int expectedFacId = GetInt(run, "settlementFactionId", -1);
            if (settlement.ID != expectedSettlementId) return false;
            if (settlement.Faction == null || settlement.Faction.loadID != expectedFacId) return false;

            if (stageDef.thing == null || stageDef.count <= 0) return false;
            return StoryPayment.CountInCaravanDirectInventory(caravan, stageDef.thing) >= stageDef.count;
        }

        public static bool TryCompleteCaravanHandIn(Caravan caravan, Settlement settlement, string questDefName, string expectedStageKey, int expectedGen)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return false;
            if (settlement == null || settlement.Destroyed || caravan.Tile != settlement.Tile) return false;

            StoryQuestRun run;
            StoryStage_Deliver stageDef;
            string reason;
            if (!ValidateActiveDeliverStage(questDefName, expectedStageKey, expectedGen, StoryDeliverTarget.giverSettlement, out run, out stageDef, out reason))
            {
                return false;
            }

            // Check deadline BEFORE touching any items
            int now = Find.TickManager.TicksGame;
            int dl = GetInt(run, "deadlineTick", -1);
            if (dl >= 0 && now >= dl) return false;

            int expectedSettlementId = GetInt(run, "settlementId", -1);
            int expectedFacId = GetInt(run, "settlementFactionId", -1);
            if (settlement.ID != expectedSettlementId || settlement.Faction == null || settlement.Faction.loadID != expectedFacId)
            {
                return false;
            }

            if (stageDef.thing == null || stageDef.count <= 0) return false;

            int deducted;
            if (!StoryPayment.TryDeductFromCaravan(caravan, stageDef.thing, stageDef.count, out deducted) || deducted != stageDef.count)
            {
                return false;
            }

            run.vars["delivered"] = "true";

            string doneTitle = !stageDef.label.NullOrEmpty() ? stageDef.label : "Delivery Complete";
            string doneBody = string.Format("Your caravan delivered {0} x{1} to {2}.",
                stageDef.thing.label, stageDef.count, settlement.LabelCap);
            Find.LetterStack.ReceiveLetter(doneTitle, doneBody, LetterDefOf.PositiveEvent, new LookTargets(settlement)); // CHECK API
            return true;
        }

        /// <summary>
        /// Enumerates all active giverSettlement Deliver stages targeting the given settlement.
        /// Used by the Settlement FloatMenu and Caravan Gizmo Harmony patches.
        /// </summary>
        internal static List<StoryQuestRun> FindActiveSettlementDeliveries(Settlement settlement)
        {
            List<StoryQuestRun> matches = new List<StoryQuestRun>();
            SparkStory story = SparkStory.Instance;
            if (story == null || settlement == null || settlement.Destroyed || settlement.Faction == null) return matches;

            int now = Find.TickManager.TicksGame;
            foreach (StoryQuestRun run in story.Runs)
            {
                if (run == null || run.status != StoryQuestStatus.Active) continue;
                if (run.vars == null || run.vars.ContainsKey("finished") || GetBool(run, "delivered")) continue;
                if (GetInt(run, "settlementId", -1) != settlement.ID) continue;
                if (GetInt(run, "settlementFactionId", -1) != settlement.Faction.loadID) continue;

                int dl = GetInt(run, "deadlineTick", -1);
                if (dl >= 0 && now >= dl) continue;

                StoryQuestDef def = run.Def;
                if (def == null || run.stage < 0 || run.stage >= def.stages.Count) continue;
                StoryStage_Deliver st = def.stages[run.stage] as StoryStage_Deliver;
                if (st != null && st.to == StoryDeliverTarget.giverSettlement && st.thing != null)
                {
                    matches.Add(run);
                }
            }
            return matches;
        }

        // =====================================================================
        // LETTER & SETTLEMENT HELPERS
        // =====================================================================

        private void SendColonyDeliverLetter(StoryContext ctx, Map pinnedMap, int deadline)
        {
            if (Find.LetterStack == null || thing == null || pinnedMap == null) return;

            ChoiceLetter_StoryDeliver letter = new ChoiceLetter_StoryDeliver();
            letter.def = LetterDefOf.NeutralEvent;
            letter.Label = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", thing.label, count), ctx);
            letter.Text = BuildColonyLetterText(ctx);
            letter.questDefName = ctx.def != null ? ctx.def.defName : string.Empty;
            letter.stageKey = ctx.run.stageKey;
            letter.runGen = ctx.run.gen;
            letter.deliverMapId = pinnedMap.uniqueID;
            letter.thingDefName = thing.defName;
            letter.requiredCount = Mathf.Max(1, count);
            letter.deadlineTick = deadline;
            letter.lookTargets = new LookTargets(pinnedMap.Center, pinnedMap);

            Find.LetterStack.ReceiveLetter(letter); // CHECK API
        }

        private string BuildColonyLetterText(StoryContext ctx)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Gather {0} x{1} in {2}'s storage stockpiles within {3:0.0} days, then open this letter and click Deliver.",
                itemLabel, count, "{COLONY}", timeoutDays);
            instruction = StoryUtil.Format(instruction, ctx);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private string BuildSettlementLetterText(StoryContext ctx, Settlement target)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Send a caravan carrying {0} x{1} to {2} and choose 'Deliver quest cargo' within {3:0.0} days.",
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
                if (dl != null && !dl.IsResolved && dl.questDefName == ctx.def.defName &&
                    dl.runGen == ctx.run.gen && dl.stageKey == ctx.run.stageKey)
                {
                    return true;
                }
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

        /// <summary>
        /// Strictly selects the nearest non-destroyed settlement belonging to ctx.Faction ONLY.
        /// Never substitutes another faction's settlement.
        /// </summary>
        private static Settlement FindNearestExactFactionSettlement(StoryContext ctx)
        {
            Faction targetFac = ctx.Faction;
            if (targetFac == null || targetFac.IsPlayer || targetFac.defeated) return null;

            PlanetTile homeTile = ResolveHomeTile(ctx); // CHECK API
            Settlement best = null;
            float bestDist = float.MaxValue;

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s == null || s.Destroyed || s.Faction != targetFac) continue;

                float dist = homeTile.Valid ? Find.WorldGrid.TraversalDistanceBetween(homeTile, s.Tile) : 0f; // CHECK API
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = s;
                }
            }
            return best;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0 || Find.WorldObjects == null) return null;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s != null && !s.Destroyed && s.ID == id) return s;
            }
            return null;
        }

        private static Map FindMapByUniqueId(int uniqueId)
        {
            if (uniqueId < 0 || Find.Maps == null) return null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == uniqueId) return maps[i];
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

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static bool TryGetInt(StoryQuestRun run, string key, out int val)
        {
            string raw;
            if (run != null && run.vars != null && run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return true;
            val = 0;
            return false;
        }

        private static int GetInt(StoryQuestRun run, string key, int fallback)
        {
            int val;
            return TryGetInt(run, key, out val) ? val : fallback;
        }

        private static bool GetBool(StoryQuestRun run, string key)
        {
            string raw;
            return run != null && run.vars != null && run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryQuestRun run, string key)
        {
            string raw;
            return (run != null && run.vars != null && run.vars.TryGetValue(key, out raw)) ? raw : string.Empty;
        }
    }

    // =========================================================================
    // HARMONY PATCHES: EXPLICIT CARAVAN DELIVERY OPTIONS (NO PASSING-BY STEAL)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class StoryDeliverCaravanPatches
    {
        static StoryDeliverCaravanPatches()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.storydeliver");

            MethodInfo settlementFloatMenu = AccessTools.Method(typeof(Settlement), "GetFloatMenuOptions", new Type[] { typeof(Caravan) }); // CHECK API
            if (settlementFloatMenu != null)
            {
                harmony.Patch(settlementFloatMenu, postfix: new HarmonyMethod(typeof(StoryDeliverCaravanPatches), "Postfix_Settlement_GetFloatMenuOptions"));
            }

            MethodInfo caravanGizmos = AccessTools.Method(typeof(Caravan), "GetGizmos"); // CHECK API
            if (caravanGizmos != null)
            {
                harmony.Patch(caravanGizmos, postfix: new HarmonyMethod(typeof(StoryDeliverCaravanPatches), "Postfix_Caravan_GetGizmos"));
            }
        }

        public static IEnumerable<FloatMenuOption> Postfix_Settlement_GetFloatMenuOptions(IEnumerable<FloatMenuOption> __result, Settlement __instance, Caravan caravan)
        {
            if (__result != null)
            {
                foreach (FloatMenuOption opt in __result)
                {
                    yield return opt;
                }
            }

            if (__instance == null || caravan == null || caravan.Faction != Faction.OfPlayer) yield break;

            List<StoryQuestRun> runs = StoryStage_Deliver.FindActiveSettlementDeliveries(__instance);
            for (int i = 0; i < runs.Count; i++)
            {
                StoryQuestRun run = runs[i];
                StoryQuestDef def = run.Def;
                StoryStage_Deliver st = (def != null && run.stage >= 0 && run.stage < def.stages.Count) ? def.stages[run.stage] as StoryStage_Deliver : null;
                if (st == null || st.thing == null) continue;

                string capturedQuest = run.quest;
                string capturedKey = run.stageKey;
                int capturedGen = run.gen;
                Settlement capturedSettlement = __instance;
                Caravan capturedCaravan = caravan;

                int have = StoryPayment.CountInCaravanDirectInventory(caravan, st.thing);
                string optLabel = string.Format("Deliver {0} x{1} to {2} ({3}/{1})",
                    st.thing.label, st.count, __instance.LabelCap, have);

                if (have < st.count)
                {
                    yield return new FloatMenuOption(optLabel + " (not enough cargo)", null);
                    continue;
                }

                yield return new FloatMenuOption(optLabel, delegate
                {
                    if (capturedCaravan.Tile == capturedSettlement.Tile)
                    {
                        StoryStage_Deliver.TryCompleteCaravanHandIn(capturedCaravan, capturedSettlement, capturedQuest, capturedKey, capturedGen);
                    }
                    else
                    {
                        CaravanArrivalAction_StoryDeliver action = new CaravanArrivalAction_StoryDeliver(
                            capturedSettlement, capturedQuest, capturedKey, capturedGen);
                        capturedCaravan.pather.StartPath(capturedSettlement.Tile, action, true); // CHECK API
                    }
                });
            }
        }

        public static IEnumerable<Gizmo> Postfix_Caravan_GetGizmos(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            if (__result != null)
            {
                foreach (Gizmo g in __result)
                {
                    yield return g;
                }
            }

            if (__instance == null || __instance.Destroyed || __instance.Faction != Faction.OfPlayer) yield break;
            if (Find.WorldObjects == null) yield break;

            Settlement here = Find.WorldObjects.SettlementAt(__instance.Tile); // CHECK API
            if (here == null || here.Destroyed) yield break;

            List<StoryQuestRun> runs = StoryStage_Deliver.FindActiveSettlementDeliveries(here);
            for (int i = 0; i < runs.Count; i++)
            {
                StoryQuestRun run = runs[i];
                StoryQuestDef def = run.Def;
                StoryStage_Deliver st = (def != null && run.stage >= 0 && run.stage < def.stages.Count) ? def.stages[run.stage] as StoryStage_Deliver : null;
                if (st == null || st.thing == null) continue;

                int have = StoryPayment.CountInCaravanDirectInventory(__instance, st.thing);
                string capturedQuest = run.quest;
                string capturedKey = run.stageKey;
                int capturedGen = run.gen;
                Settlement capturedSettlement = here;
                Caravan capturedCaravan = __instance;

                Command_Action cmd = new Command_Action();
                cmd.defaultLabel = string.Format("Deliver {0} x{1}", st.thing.label, st.count);
                cmd.defaultDesc = string.Format("Hand in {0} x{1} to {2} for quest '{3}' ({4}/{1} carried).",
                    st.thing.label, st.count, here.LabelCap, def.LabelCap, have);
                cmd.icon = st.thing.uiIcon; // CHECK API
                if (have < st.count)
                {
                    cmd.Disable(string.Format("Caravan carries only {0}/{1} {2}.", have, st.count, st.thing.label));
                }
                cmd.action = delegate
                {
                    StoryStage_Deliver.TryCompleteCaravanHandIn(capturedCaravan, capturedSettlement, capturedQuest, capturedKey, capturedGen);
                };
                yield return cmd;
            }
        }
    }
}
