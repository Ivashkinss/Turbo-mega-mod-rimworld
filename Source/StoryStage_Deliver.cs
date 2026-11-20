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
    // COLONY HAND-IN CHOICE LETTER (VALIDATED AGAINST IMMUTABLE CONTRACT)
    // =========================================================================

    /// <summary>
    /// Reminder/hand-in ChoiceLetter for StoryStage_Deliver (to = colony).
    ///
    /// Round 2 invariants:
    /// - NEVER trusts letter.thingDefName, letter.requiredCount, letter.deadlineTick, or letter.deliverMapId
    ///   as authoritative payment terms.
    /// - Resolves the active stage STRICTLY by stageKey (if stageKey is present and missing from def.stages,
    ///   refuses with zero fallback to stale run.stage index).
    /// - Reads ThingDef, count, mapId, and deadlineTick from the verified stage &amp; immutable contract
    ///   recorded in run.vars at Start; unknown/missing deadlineTick ALWAYS refuses payment.
    /// - Deducts exclusively via atomic StoryPayment with onPaymentSecured recording delivered=true
    ///   after 100% staging confirmation and before irreversible destruction.
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
                ThingDef contractDef;
                int contractCount;
                string invalidReason;

                bool valid = StoryStage_Deliver.ValidateColonyHandIn(
                    this, out run, out pinnedMap, out contractDef, out contractCount, out invalidReason);

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

                int have = StoryPayment.CountInColonyStockpiles(pinnedMap, contractDef);
                string itemLabel = contractDef.label;
                string deliverLabel = string.Format("Deliver {0} x{1} ({2}/{3})", itemLabel, contractCount, have, contractCount);
                DiaOption deliver = new DiaOption(deliverLabel);

                if (have < contractCount)
                {
                    deliver.Disable(string.Format("Need {0} {1} in colony storage on {2} ({3}/{0})",
                        contractCount, itemLabel, pinnedMap.Parent != null ? pinnedMap.Parent.LabelCap.ToString() : "colony", have));
                }
                else
                {
                    deliver.action = delegate
                    {
                        // Full re-validation at click time against verified stage & immutable vars contract
                        StoryQuestRun clickRun;
                        Map clickMap;
                        ThingDef clickDef;
                        int clickCount;
                        string clickReason;
                        if (!StoryStage_Deliver.ValidateColonyHandIn(this, out clickRun, out clickMap, out clickDef, out clickCount, out clickReason))
                        {
                            resolved = true;
                            if (InStack) Find.LetterStack.RemoveLetter(this);
                            return;
                        }

                        int deducted;
                        bool paid = StoryPayment.TryDeductFromColonyStockpiles(
                            clickMap,
                            clickDef,
                            clickCount,
                            out deducted,
                            delegate
                            {
                                clickRun.vars["delivered"] = "true";
                            });

                        if (paid && deducted == clickCount)
                        {
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

            // Record immutable payment contract in run.vars at Start
            ctx.run.vars["contractThingDef"] = thing.defName;
            ctx.run.vars["contractCount"] = count.ToString();
            ctx.run.vars["contractTarget"] = to.ToString();

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

            // 1. If payment was already confirmed, finish cleanly without re-deducting
            if (GetBool(ctx.run, "delivered"))
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "success";
                return "success";
            }

            // 2. Unknown/missing deadlineTick -> refuse to slide deadline forward; expire cleanly
            int now = Find.TickManager.TicksGame;
            int deadline;
            if (!TryGetInt(ctx.run, "deadlineTick", out deadline) || deadline < 0)
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["failReason"] = "deadline_missing";
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

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

            // 3. giverSettlement mode: verify target settlement still exists & belongs to required faction
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
            ThingDef displayDef = thing;
            int displayCount = count;
            if (ctx != null && ctx.run != null)
            {
                ThingDef cDef;
                int cCount;
                if (TryResolveImmutableContract(ctx.run, this, out cDef, out cCount))
                {
                    displayDef = cDef;
                    displayCount = cCount;
                }
            }

            string itemLabel = displayDef != null ? displayDef.label : "resources";
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", itemLabel, displayCount), ctx);

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx.run, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            if (to == StoryDeliverTarget.colony)
            {
                Map pinnedMap = FindMapByUniqueId(GetInt(ctx.run, "deliverMapId", -1));
                int have = (pinnedMap != null && displayDef != null) ? StoryPayment.CountInColonyStockpiles(pinnedMap, displayDef) : 0;
                return string.Format("{0} ({1}/{2} {3}, {4:0.0}d left)", baseText, have, displayCount, itemLabel, daysLeft);
            }

            string targetName = GetStr(ctx.run, "settlementName");
            if (targetName.NullOrEmpty()) targetName = "faction settlement";
            return string.Format("{0} to {1} ({2:0.0}d left)", baseText, targetName, daysLeft);
        }

        // =====================================================================
        // STRICT VALIDATION (NO STALE INDEX FALLBACK, NO UNKNOWN DEADLINE,
        // IMMUTABLE CONTRACT TERMS ONLY)
        // =====================================================================

        public static bool ValidateColonyHandIn(
            ChoiceLetter_StoryDeliver letter,
            out StoryQuestRun run,
            out Map pinnedMap,
            out ThingDef contractDef,
            out int contractCount,
            out string reason)
        {
            run = null;
            pinnedMap = null;
            contractDef = null;
            contractCount = 0;
            reason = null;

            if (letter == null || letter.IsResolved || !letter.IsInLetterStack)
            {
                reason = "letter_not_active";
                return false;
            }

            StoryStage_Deliver stageDef;
            if (!ValidateActiveDeliverStage(
                    letter.questDefName,
                    letter.stageKey,
                    letter.runGen,
                    StoryDeliverTarget.colony,
                    out run,
                    out stageDef,
                    out contractDef,
                    out contractCount,
                    out reason))
            {
                return false;
            }

            // Unknown/missing deadlineTick in run.vars ALWAYS refuses payment (never falls back to letter)
            int now = Find.TickManager.TicksGame;
            int dl;
            if (!TryGetInt(run, "deadlineTick", out dl) || dl < 0 || now >= dl)
            {
                reason = "deadline_missing_or_passed";
                return false;
            }

            // Pinned map comes strictly from run.vars (never from letter)
            int mapId;
            if (!TryGetInt(run, "deliverMapId", out mapId) || mapId < 0)
            {
                reason = "deliver_map_missing";
                return false;
            }

            pinnedMap = FindMapByUniqueId(mapId);
            if (pinnedMap == null || !pinnedMap.IsPlayerHome)
            {
                reason = "pinned_map_missing";
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
            out ThingDef contractDef,
            out int contractCount,
            out string reason)
        {
            run = null;
            stageDef = null;
            contractDef = null;
            contractCount = 0;
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

            if (run.vars == null || run.vars.ContainsKey("finished") || GetBool(run, "delivered"))
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

            // STRICT stageKey RESOLUTION:
            // If stageKey is set (either on the caller or on run.stageKey) and NO stage in def.stages
            // matches that exact key -> REFUSE! Never fall back to the stale run.stage index!
            string lookupKey = !expectedStageKey.NullOrEmpty() ? expectedStageKey : run.stageKey;
            StoryStage foundStage = null;

            if (!lookupKey.NullOrEmpty())
            {
                for (int i = 0; i < def.stages.Count; i++)
                {
                    if (def.stages[i] != null && def.stages[i].Key(i) == lookupKey)
                    {
                        foundStage = def.stages[i];
                        break;
                    }
                }
                if (foundStage == null)
                {
                    reason = "stage_key_removed";
                    return false;
                }
            }
            else if (run.stage >= 0 && run.stage < def.stages.Count)
            {
                // Legacy migration only when neither letter nor run carried a stageKey
                foundStage = def.stages[run.stage];
            }

            stageDef = foundStage as StoryStage_Deliver;
            if (stageDef == null || stageDef.to != expectedTarget)
            {
                reason = "wrong_stage_type";
                return false;
            }

            // Verify contract target matches expectedTarget if recorded
            string recordedTarget = GetStr(run, "contractTarget");
            if (!recordedTarget.NullOrEmpty() && !string.Equals(recordedTarget, expectedTarget.ToString(), StringComparison.Ordinal))
            {
                reason = "contract_target_mismatch";
                return false;
            }

            // Resolve ThingDef and count strictly from immutable vars contract (or verified current stageDef),
            // NEVER from letter fields!
            if (!TryResolveImmutableContract(run, stageDef, out contractDef, out contractCount))
            {
                reason = "invalid_contract";
                return false;
            }

            return true;
        }

        private static bool TryResolveImmutableContract(StoryQuestRun run, StoryStage_Deliver verifiedStage, out ThingDef resolvedDef, out int resolvedCount)
        {
            resolvedDef = null;
            resolvedCount = 0;
            if (verifiedStage == null) return false;

            string rawDef = GetStr(run, "contractThingDef");
            string rawCount = GetStr(run, "contractCount");

            if (!rawDef.NullOrEmpty() || !rawCount.NullOrEmpty())
            {
                ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(rawDef);
                int c;
                if (d == null || !int.TryParse(rawCount, out c) || c <= 0)
                {
                    return false;
                }
                resolvedDef = d;
                resolvedCount = c;
                return true;
            }

            // Legacy run created before immutable contract vars were added: use verified current stageDef
            if (verifiedStage.thing == null || verifiedStage.count <= 0)
            {
                return false;
            }
            resolvedDef = verifiedStage.thing;
            resolvedCount = verifiedStage.count;
            return true;
        }

        public static bool CanCaravanDeliver(Caravan caravan, Settlement settlement, string questDefName, string expectedStageKey, int expectedGen)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return false;
            if (settlement == null || settlement.Destroyed) return false;

            StoryQuestRun run;
            StoryStage_Deliver stageDef;
            ThingDef contractDef;
            int contractCount;
            string reason;
            if (!ValidateActiveDeliverStage(
                    questDefName, expectedStageKey, expectedGen, StoryDeliverTarget.giverSettlement,
                    out run, out stageDef, out contractDef, out contractCount, out reason))
            {
                return false;
            }

            // Unknown/missing deadlineTick ALWAYS refuses
            int now = Find.TickManager.TicksGame;
            int dl;
            if (!TryGetInt(run, "deadlineTick", out dl) || dl < 0 || now >= dl) return false;

            int expectedSettlementId = GetInt(run, "settlementId", -1);
            int expectedFacId = GetInt(run, "settlementFactionId", -1);
            if (settlement.ID != expectedSettlementId) return false;
            if (settlement.Faction == null || settlement.Faction.loadID != expectedFacId) return false;

            return StoryPayment.CountInCaravanDirectInventory(caravan, contractDef) >= contractCount;
        }

        public static bool TryCompleteCaravanHandIn(Caravan caravan, Settlement settlement, string questDefName, string expectedStageKey, int expectedGen)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return false;
            if (settlement == null || settlement.Destroyed || caravan.Tile != settlement.Tile) return false;

            StoryQuestRun run;
            StoryStage_Deliver stageDef;
            ThingDef contractDef;
            int contractCount;
            string reason;
            if (!ValidateActiveDeliverStage(
                    questDefName, expectedStageKey, expectedGen, StoryDeliverTarget.giverSettlement,
                    out run, out stageDef, out contractDef, out contractCount, out reason))
            {
                return false;
            }

            // Unknown/missing deadlineTick ALWAYS refuses before touching any items
            int now = Find.TickManager.TicksGame;
            int dl;
            if (!TryGetInt(run, "deadlineTick", out dl) || dl < 0 || now >= dl) return false;

            int expectedSettlementId = GetInt(run, "settlementId", -1);
            int expectedFacId = GetInt(run, "settlementFactionId", -1);
            if (settlement.ID != expectedSettlementId || settlement.Faction == null || settlement.Faction.loadID != expectedFacId)
            {
                return false;
            }

            int deducted;
            bool paid = StoryPayment.TryDeductFromCaravan(
                caravan,
                contractDef,
                contractCount,
                out deducted,
                delegate
                {
                    run.vars["delivered"] = "true";
                });

            if (!paid || deducted != contractCount)
            {
                return false;
            }

            string doneTitle = !stageDef.label.NullOrEmpty() ? stageDef.label : "Delivery Complete";
            string doneBody = string.Format("Your caravan delivered {0} x{1} to {2}.",
                contractDef.label, contractCount, settlement.LabelCap);
            Find.LetterStack.ReceiveLetter(doneTitle, doneBody, LetterDefOf.PositiveEvent, new LookTargets(settlement)); // CHECK API
            return true;
        }

        internal sealed class ActiveSettlementDelivery
        {
            public StoryQuestRun run;
            public StoryStage_Deliver stage;
            public ThingDef contractDef;
            public int contractCount;
        }

        internal static List<ActiveSettlementDelivery> FindActiveSettlementDeliveries(Settlement settlement)
        {
            List<ActiveSettlementDelivery> matches = new List<ActiveSettlementDelivery>();
            SparkStory story = SparkStory.Instance;
            if (story == null || settlement == null || settlement.Destroyed || settlement.Faction == null) return matches;

            int now = Find.TickManager.TicksGame;
            foreach (StoryQuestRun run in story.Runs)
            {
                if (run == null || run.status != StoryQuestStatus.Active) continue;
                if (run.vars == null || run.vars.ContainsKey("finished") || GetBool(run, "delivered")) continue;
                if (GetInt(run, "settlementId", -1) != settlement.ID) continue;
                if (GetInt(run, "settlementFactionId", -1) != settlement.Faction.loadID) continue;

                int dl;
                if (!TryGetInt(run, "deadlineTick", out dl) || dl < 0 || now >= dl) continue;

                StoryQuestRun verifiedRun;
                StoryStage_Deliver st;
                ThingDef cDef;
                int cCount;
                string reason;
                if (!ValidateActiveDeliverStage(
                        run.quest, run.stageKey, run.gen, StoryDeliverTarget.giverSettlement,
                        out verifiedRun, out st, out cDef, out cCount, out reason))
                {
                    continue;
                }

                ActiveSettlementDelivery entry = new ActiveSettlementDelivery();
                entry.run = verifiedRun;
                entry.stage = st;
                entry.contractDef = cDef;
                entry.contractCount = cCount;
                matches.Add(entry);
            }
            return matches;
        }

        // =====================================================================
        // LETTER & SETTLEMENT HELPERS
        // =====================================================================

        private void SendColonyDeliverLetter(StoryContext ctx, Map pinnedMap, int deadline)
        {
            if (Find.LetterStack == null || thing == null || pinnedMap == null) return;

            ThingDef displayDef;
            int displayCount;
            if (!TryResolveImmutableContract(ctx.run, this, out displayDef, out displayCount))
            {
                displayDef = thing;
                displayCount = Mathf.Max(1, count);
            }

            ChoiceLetter_StoryDeliver letter = new ChoiceLetter_StoryDeliver();
            letter.def = LetterDefOf.NeutralEvent;
            letter.Label = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", displayDef.label, displayCount), ctx);
            letter.Text = BuildColonyLetterText(ctx, displayDef, displayCount);
            letter.questDefName = ctx.def != null ? ctx.def.defName : string.Empty;
            letter.stageKey = ctx.run.stageKey;
            letter.runGen = ctx.run.gen;
            letter.deliverMapId = pinnedMap.uniqueID;
            letter.thingDefName = displayDef.defName;
            letter.requiredCount = displayCount;
            letter.deadlineTick = deadline;
            letter.lookTargets = new LookTargets(pinnedMap.Center, pinnedMap);

            Find.LetterStack.ReceiveLetter(letter); // CHECK API
        }

        private string BuildColonyLetterText(StoryContext ctx, ThingDef displayDef, int displayCount)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = displayDef != null ? displayDef.label : "items";
            string instruction = string.Format("Gather {0} x{1} in {2}'s storage stockpiles within {3:0.0} days, then open this letter and click Deliver.",
                itemLabel, displayCount, "{COLONY}", timeoutDays);
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

            List<StoryStage_Deliver.ActiveSettlementDelivery> deliveries = StoryStage_Deliver.FindActiveSettlementDeliveries(__instance);
            for (int i = 0; i < deliveries.Count; i++)
            {
                StoryStage_Deliver.ActiveSettlementDelivery d = deliveries[i];
                if (d == null || d.run == null || d.contractDef == null) continue;

                string capturedQuest = d.run.quest;
                string capturedKey = d.run.stageKey;
                int capturedGen = d.run.gen;
                Settlement capturedSettlement = __instance;
                Caravan capturedCaravan = caravan;

                int have = StoryPayment.CountInCaravanDirectInventory(caravan, d.contractDef);
                string optLabel = string.Format("Deliver {0} x{1} to {2} ({3}/{1})",
                    d.contractDef.label, d.contractCount, __instance.LabelCap, have);

                if (have < d.contractCount)
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

            List<StoryStage_Deliver.ActiveSettlementDelivery> deliveries = StoryStage_Deliver.FindActiveSettlementDeliveries(here);
            for (int i = 0; i < deliveries.Count; i++)
            {
                StoryStage_Deliver.ActiveSettlementDelivery d = deliveries[i];
                if (d == null || d.run == null || d.contractDef == null) continue;

                StoryQuestDef qDef = d.run.Def;
                int have = StoryPayment.CountInCaravanDirectInventory(__instance, d.contractDef);
                string capturedQuest = d.run.quest;
                string capturedKey = d.run.stageKey;
                int capturedGen = d.run.gen;
                Settlement capturedSettlement = here;
                Caravan capturedCaravan = __instance;

                Command_Action cmd = new Command_Action();
                cmd.defaultLabel = string.Format("Deliver {0} x{1}", d.contractDef.label, d.contractCount);
                cmd.defaultDesc = string.Format("Hand in {0} x{1} to {2} for quest '{3}' ({4}/{1} carried).",
                    d.contractDef.label, d.contractCount, here.LabelCap, qDef != null ? qDef.LabelCap.ToString() : d.run.quest, have);
                cmd.icon = d.contractDef.uiIcon; // CHECK API
                if (have < d.contractCount)
                {
                    cmd.Disable(string.Format("Caravan carries only {0}/{1} {2}.", have, d.contractCount, d.contractDef.label));
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

