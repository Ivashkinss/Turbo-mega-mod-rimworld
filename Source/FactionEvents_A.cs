using System;
using System.Collections.Generic;
using ResourceRequirement = TurboMegaMod.StoryPayment.ResourceRequirement;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    // =========================================================================
    // 1. Choice Identifiers & Serializable ChoiceLetter for Faction Events
    // =========================================================================

    public enum FactionEventChoiceKind
    {
        None = 0,
        BosTechDemand = 1,
        BosPaladinShelter = 2,
        OutcastsTechForMeds = 3,
        OutcastsCaravanAmbush = 4,
        RailroadFugitiveSynth = 5,
        GunnersExtortion = 6,
        GunnersMercContract = 7,
        VaultTecExperiment = 8,
        VaultTecSupplyCaravan = 9,
        MinutemenDistressSignal = 10,
        MolBuyRecords = 11,
        MolRecruitment = 12,
        HeavenVesselRequest = 13,
        HellCrossroadsDeal = 14,
        HellDemonicPossession = 15,
        GhoulsRefugees = 16,
        GhoulsRadTrade = 17,
        FriendlyMutantJoin = 18,
        FriendlyMutantHeal = 19,
        WildMutantsWarlordDuel = 20,
        MonsterShapeshifterGuest = 21
    }

    /// <summary>
    /// Serializable ChoiceLetter used by all interactive TMM faction events.
    /// Stores map/caravan/faction/pawn/thing/amount references so choices survive save/load
    /// and can be programmatically inspected and executed by InGameFactionEventsProbe.
    /// </summary>
    public sealed class ChoiceLetter_TMMFactionEvent : ChoiceLetter
    {
        public FactionEventChoiceKind eventKind;
        public Faction faction;
        public Map map;
        public Caravan caravan;
        public Pawn subjectPawn;
        public Thing targetThing;
        public int silverCost;
        public int rewardAmount;
        public float threatPoints;
        public bool resolved;

        public override bool CanDismissWithRightClick
        {
            get { return false; }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly || resolved)
                {
                    yield return Option_Close;
                    yield break;
                }

                List<DiaOption> built = BuildOptions();
                if (built == null || built.Count == 0)
                {
                    yield return Option_Close;
                    yield break;
                }

                for (int i = 0; i < built.Count; i++)
                {
                    yield return built[i];
                }
            }
        }

        /// <summary>
        /// Public helper for automated probes to obtain and invoke the exact same options shown to the player.
        /// </summary>
        public List<DiaOption> BuildOptions()
        {
            List<DiaOption> options = new List<DiaOption>();
            switch (eventKind)
            {
                case FactionEventChoiceKind.BosTechDemand:
                    IncidentWorker_TMM_BosTechDemand.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.BosPaladinShelter:
                    IncidentWorker_TMM_BosPaladinShelter.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.OutcastsTechForMeds:
                    IncidentWorker_TMM_OutcastsTechForMeds.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.OutcastsCaravanAmbush:
                    IncidentWorker_TMM_OutcastsCaravanAmbush.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.RailroadFugitiveSynth:
                    IncidentWorker_TMM_RailroadFugitiveSynth.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.GunnersExtortion:
                    IncidentWorker_TMM_GunnersExtortion.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.GunnersMercContract:
                    IncidentWorker_TMM_GunnersMercContract.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.VaultTecExperiment:
                    IncidentWorker_TMM_VaultTecExperiment.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.VaultTecSupplyCaravan:
                    IncidentWorker_TMM_VaultTecSupplyCaravan.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.MinutemenDistressSignal:
                    IncidentWorker_TMM_MinutemenDistressSignal.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.MolBuyRecords:
                    IncidentWorker_TMM_MolBuyRecords.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.MolRecruitment:
                    IncidentWorker_TMM_MolRecruitment.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.HeavenVesselRequest:
                    IncidentWorker_TMM_HeavenVesselRequest.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.HellCrossroadsDeal:
                    IncidentWorker_TMM_HellCrossroadsDeal.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.HellDemonicPossession:
                    IncidentWorker_TMM_HellDemonicPossession.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.GhoulsRefugees:
                    IncidentWorker_TMM_GhoulsRefugees.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.GhoulsRadTrade:
                    IncidentWorker_TMM_GhoulsRadTrade.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.FriendlyMutantJoin:
                    IncidentWorker_TMM_FriendlyMutantJoin.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.FriendlyMutantHeal:
                    IncidentWorker_TMM_FriendlyMutantHeal.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.WildMutantsWarlordDuel:
                    IncidentWorker_TMM_WildMutantsWarlordDuel.PopulateChoices(this, options);
                    break;
                case FactionEventChoiceKind.MonsterShapeshifterGuest:
                    IncidentWorker_TMM_MonsterShapeshifterGuest.PopulateChoices(this, options);
                    break;
                default:
                    options.Add(Option_Close);
                    break;
            }
            return options;
        }

        public bool TryResolvePayment(Func<bool> tryPay)
        {
            if (resolved || ArchivedOnly) return false;
            if (!tryPay())
            {
                Messages.Message("TMM.Event.Common.PaymentFailed".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            CompleteAndRemove();
            return true;
        }

        public void CompleteAndRemove()
        {
            resolved = true;
            if (Find.LetterStack != null)
            {
                Find.LetterStack.RemoveLetter(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref eventKind, "tmm_eventKind", FactionEventChoiceKind.None);
            Scribe_References.Look(ref faction, "tmm_faction");
            Scribe_References.Look(ref map, "tmm_map");
            // The archive keeps the letter after its caravan is gone; a destroyed world object is not saved.
            if (Scribe.mode == LoadSaveMode.Saving && caravan != null && (caravan.Destroyed || !caravan.Spawned)) caravan = null;
            Scribe_References.Look(ref caravan, "tmm_caravan");
            Scribe_References.Look(ref subjectPawn, "tmm_subjectPawn");
            Scribe_References.Look(ref targetThing, "tmm_targetThing");
            Scribe_Values.Look(ref silverCost, "tmm_silverCost", 0);
            Scribe_Values.Look(ref rewardAmount, "tmm_rewardAmount", 0);
            Scribe_Values.Look(ref threatPoints, "tmm_threatPoints", 0f);
            Scribe_Values.Look(ref resolved, "tmm_resolved", false);
        }
    }

    // =========================================================================
    // 2. Delayed Consequences & Temporary Guest / Mercenary / Debt Manager
    // =========================================================================

    public enum DelayedFactionTaskKind
    {
        DelayedRaid = 0,
        ShelterPaladinLeave = 1,
        MercenariesLeave = 2,
        CrossroadsDebtDue = 3,
        HiddenSynthReveal = 4,
        WoundedMutantCheck = 5,
        ShapeshifterTransform = 6
    }

    public sealed class DelayedFactionTask : IExposable
    {
        public DelayedFactionTaskKind kind;
        public int triggerTick;
        public Map map;
        public Faction faction;
        public Pawn pawn;
        public List<Pawn> pawns = new List<Pawn>();
        public float points;
        public bool completed;

        public void ExposeData()
        {
            Scribe_Values.Look(ref kind, "kind", DelayedFactionTaskKind.DelayedRaid);
            Scribe_Values.Look(ref triggerTick, "triggerTick", 0);
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref points, "points", 0f);
            Scribe_Values.Look(ref completed, "completed", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pawns == null)
            {
                pawns = new List<Pawn>();
            }
        }
    }

    public sealed class TMM_FactionEventsManager : GameComponent
    {
        private const int CheckIntervalTicks = 250;
        public List<DelayedFactionTask> tasks = new List<DelayedFactionTask>();

        public TMM_FactionEventsManager(Game game) { }

        public static TMM_FactionEventsManager Instance
        {
            get { return Current.Game == null ? null : Current.Game.GetComponent<TMM_FactionEventsManager>(); }
        }

        public void Schedule(DelayedFactionTask task)
        {
            if (task == null) return;
            tasks.Add(task);
        }

        public override void GameComponentTick()
        {
            if (tasks == null || tasks.Count == 0 || Find.TickManager == null) return;
            int now = Find.TickManager.TicksGame;
            if (now % CheckIntervalTicks != 0) return;

            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                DelayedFactionTask t = tasks[i];
                if (t == null || t.completed)
                {
                    tasks.RemoveAt(i);
                    continue;
                }

                if (now < t.triggerTick) continue;

                try
                {
                    ExecuteTask(t);
                }
                catch (Exception ex)
                {
                    Log.Warning("[TMM] DelayedFactionTask failed (" + t.kind + "): " + ex.Message);
                }

                t.completed = true;
                tasks.RemoveAt(i);
            }
        }

        private static void ExecuteTask(DelayedFactionTask t)
        {
            Map map = t.map;
            if (map == null || !Find.Maps.Contains(map))
            {
                map = Find.AnyPlayerHomeMap;
            }
            if (map == null) return;

            switch (t.kind)
            {
                case DelayedFactionTaskKind.DelayedRaid:
                    if (t.faction != null && !t.faction.defeated)
                    {
                        FactionEventHelper.TriggerEnemyRaid(map, t.faction, t.points > 100f ? t.points : StorytellerUtility.DefaultThreatPointsNow(map), PawnsArrivalModeDefOf.EdgeWalkIn);
                    }
                    break;

                case DelayedFactionTaskKind.ShelterPaladinLeave:
                    if (t.pawn != null && !t.pawn.Dead && !t.pawn.Destroyed)
                    {
                        if (t.faction != null && !t.faction.defeated)
                        {
                            FactionEventHelper.AdjustGoodwill(t.faction, 15, map);
                        }
                        FactionEventHelper.DropItemsNearCenter(map, ThingDefOf.MedicineIndustrial, 6);
                        FactionEventHelper.SendPawnLeaving(t.pawn, map, t.faction);
                        FactionEventHelper.SendSimpleLetter(
                            "TMM.Event.BosPaladinShelter.DoneTitle".Translate(),
                            "TMM.Event.BosPaladinShelter.DoneText".Translate(t.pawn.LabelShort),
                            LetterDefOf.PositiveEvent,
                            new LookTargets(map.Parent));
                    }
                    break;

                case DelayedFactionTaskKind.MercenariesLeave:
                    if (t.pawns != null)
                    {
                        for (int i = 0; i < t.pawns.Count; i++)
                        {
                            Pawn merc = t.pawns[i];
                            if (merc == null || merc.Dead || merc.Destroyed) continue;
                            if (t.faction != null && !t.faction.defeated && merc.Faction == Faction.OfPlayer)
                            {
                                merc.SetFaction(t.faction);
                            }
                            FactionEventHelper.SendPawnLeaving(merc, map, t.faction);
                        }
                        Messages.Message("TMM.Event.GunnersMercContract.ExpiredMsg".Translate(), MessageTypeDefOf.NeutralEvent);
                    }
                    break;

                case DelayedFactionTaskKind.CrossroadsDebtDue:
                    if (t.faction != null && !t.faction.defeated)
                    {
                        float pts = t.points > 200f ? t.points : StorytellerUtility.DefaultThreatPointsNow(map) * 1.15f;
                        FactionEventHelper.TriggerEnemyRaid(map, t.faction, pts, PawnsArrivalModeDefOf.EdgeWalkIn);
                        FactionEventHelper.SendSimpleLetter(
                            "TMM.Event.HellCrossroadsDeal.DueTitle".Translate(),
                            "TMM.Event.HellCrossroadsDeal.DueText".Translate(t.pawn != null ? t.pawn.LabelShort : "TMM.Event.Common.Colonist".Translate().ToString()),
                            LetterDefOf.ThreatBig,
                            new LookTargets(map.Parent));
                    }
                    break;

                case DelayedFactionTaskKind.HiddenSynthReveal:
                    // TODO story: replace simple reveal with a full Institute infiltration quest & confrontation dialog
                    if (t.pawn != null && !t.pawn.Dead && !t.pawn.Destroyed)
                    {
                        HediffDef hiddenDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_HiddenSynth");
                        HediffDef revealedDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_RevealedSynth");
                        if (hiddenDef != null && t.pawn.health != null)
                        {
                            Hediff h = t.pawn.health.hediffSet.GetFirstHediffOfDef(hiddenDef);
                            if (h != null) t.pawn.health.RemoveHediff(h);
                        }
                        if (revealedDef != null && t.pawn.health != null)
                        {
                            t.pawn.health.AddHediff(revealedDef);
                        }
                        FactionEventHelper.SendSimpleLetter(
                            "TMM.Event.InstituteSynthReplacement.RevealTitle".Translate(t.pawn.LabelShort),
                            "TMM.Event.InstituteSynthReplacement.RevealText".Translate(t.pawn.LabelShort),
                            LetterDefOf.NegativeEvent,
                            t.pawn);
                    }
                    break;

                case DelayedFactionTaskKind.WoundedMutantCheck:
                    if (t.pawn != null && !t.pawn.Dead && !t.pawn.Destroyed)
                    {
                        bool healed = t.pawn.health == null || t.pawn.health.summaryHealth == null || t.pawn.health.summaryHealth.SummaryHealthPercent >= 0.7f;
                        if (healed)
                        {
                            if (t.faction != null && !t.faction.defeated)
                            {
                                FactionEventHelper.AdjustGoodwill(t.faction, 20, map);
                            }
                            FactionEventHelper.DropItemsNearCenter(map, ThingDefOf.Steel, 120);
                            FactionEventHelper.SendSimpleLetter(
                                "TMM.Event.FriendlyMutantHeal.DoneTitle".Translate(),
                                "TMM.Event.FriendlyMutantHeal.DoneText".Translate(t.pawn.LabelShort),
                                LetterDefOf.PositiveEvent,
                                new LookTargets(map.Parent));
                        }
                        FactionEventHelper.SendPawnLeaving(t.pawn, map, t.faction);
                    }
                    break;

                case DelayedFactionTaskKind.ShapeshifterTransform:
                    if (t.pawn != null && !t.pawn.Dead && !t.pawn.Destroyed && t.pawn.Spawned && t.faction != null && !t.faction.defeated)
                    {
                        t.pawn.SetFaction(t.faction);
                        if (t.pawn.Map != null)
                        {
                            Lord oldLord = t.pawn.GetLord();
                            if (oldLord != null) oldLord.RemovePawn(t.pawn);
                            LordMaker.MakeNewLord(t.faction, new LordJob_AssaultColony(t.faction), t.pawn.Map, new List<Pawn> { t.pawn });
                            FactionEventHelper.SendSimpleLetter(
                                "TMM.Event.MonsterShapeshifterGuest.TransformTitle".Translate(),
                                "TMM.Event.MonsterShapeshifterGuest.TransformText".Translate(t.pawn.LabelShort),
                                LetterDefOf.ThreatSmall,
                                t.pawn);
                        }
                    }
                    break;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref tasks, "TMM_delayedFactionTasks", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && tasks == null)
            {
                tasks = new List<DelayedFactionTask>();
            }
        }
    }

    // =========================================================================
    // 3. Shared Helpers (Atomic Silver/Item Checks, Letters, Spawning, Raids)
    // =========================================================================

    public static class FactionEventHelper
    {
        public static Faction FindActiveFaction(string defName)
        {
            if (Find.FactionManager == null || string.IsNullOrEmpty(defName)) return null;
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            Faction f = Find.FactionManager.FirstFactionOfDef(def);
            if (f == null || f.defeated || f.IsPlayer) return null;
            return f;
        }

        public static ChoiceLetter_TMMFactionEvent SendChoiceLetter(
            string title,
            string text,
            LetterDef fallbackDef,
            LookTargets targets,
            Faction faction,
            FactionEventChoiceKind kind,
            Map map,
            Caravan caravan,
            Pawn subjectPawn,
            Thing targetThing,
            int silverCost,
            int rewardAmount,
            float threatPoints)
        {
            LetterDef letterDef = DefDatabase<LetterDef>.GetNamedSilentFail("TMM_FactionEventChoiceLetter");
            if (letterDef == null) letterDef = fallbackDef != null ? fallbackDef : LetterDefOf.NeutralEvent;

            ChoiceLetter_TMMFactionEvent letter = new ChoiceLetter_TMMFactionEvent();
            letter.ID = Find.UniqueIDsManager != null ? Find.UniqueIDsManager.GetNextLetterID() : Rand.Range(100000, 999999); // CHECK API
            letter.def = letterDef;
            letter.Label = title;
            letter.Text = text;
            letter.lookTargets = targets;
            letter.relatedFaction = faction;
            letter.eventKind = kind;
            letter.faction = faction;
            letter.map = map;
            letter.caravan = caravan;
            letter.subjectPawn = subjectPawn;
            letter.targetThing = targetThing;
            letter.silverCost = silverCost;
            letter.rewardAmount = rewardAmount;
            letter.threatPoints = threatPoints;
            letter.StartTimeout(60000); // 1 day timeout

            if (Find.LetterStack != null)
            {
                Find.LetterStack.ReceiveLetter(letter);
            }
            return letter;
        }

        public static void SendSimpleLetter(string title, string text, LetterDef def, LookTargets targets)
        {
            if (Find.LetterStack == null) return;
            Find.LetterStack.ReceiveLetter(title, text, def != null ? def : LetterDefOf.NeutralEvent, targets);
        }

        public static void AdjustGoodwill(Faction faction, int delta, Map map)
        {
            if (faction == null || faction.IsPlayer || faction.defeated || delta == 0) return;
            if (faction.def != null && faction.def.permanentEnemy) return;
            GlobalTargetInfo target = map != null ? new GlobalTargetInfo(map.Tile) : GlobalTargetInfo.Invalid;
            faction.TryAffectGoodwillWith(Faction.OfPlayer, delta, true, true, null, target);
        }

        private static bool IsSpendableSilver(Thing thing)
        {
            return thing != null && thing.def == ThingDefOf.Silver && !thing.Destroyed &&
                thing.stackCount > 0 && !OutpostCaravanManager.IsMissionSilverThing(thing);
        }

        public static int CountSilverOnMap(Map map)
        {
            if (map == null || map.listerThings == null) return 0;
            return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t =>
                IsSpendableSilver(t) && StoryPayment.IsEligibleMapItem(map, t, false)).Sum(t => t.stackCount);
        }

        public static bool TryTakeSilverFromMap(Map map, int amount, Action onPaymentSecured = null)
        {
            if (map == null) return false;
            if (amount <= 0)
            {
                if (onPaymentSecured != null) onPaymentSecured();
                return true;
            }

            int deducted;
            return StoryPayment.TryDeductFromMap(map, ThingDefOf.Silver, amount, false, out deducted,
                onPaymentSecured, IsSpendableSilver) && deducted == amount;
        }

        public static int CountSilverInCaravan(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer) return 0;
            int silver = 0;
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.inventory == null) continue;
                silver += pawn.inventory.innerContainer.Where(IsSpendableSilver).Sum(t => t.stackCount);
            }
            return silver;
        }

        public static bool TryTakeSilverFromCaravan(Caravan caravan, int amount)
        {
            if (caravan == null || caravan.Destroyed) return false;
            if (amount <= 0) return true;
            int deducted;
            return StoryPayment.TryDeductFromCaravan(caravan, ThingDefOf.Silver, amount, out deducted,
                null, IsSpendableSilver) && deducted == amount;
        }

        public static bool TryTakeSingleThingFromMap(Map map, Thing target, Action onPaymentSecured = null)
        {
            if (target == null || OutpostCaravanManager.IsMissionSilverThing(target) ||
                !StoryPayment.IsEligibleMapItem(map, target, false)) return false;
            int deducted;
            return StoryPayment.TryDeductFromMap(map, target.def, 1, false, out deducted,
                onPaymentSecured, t => t == target) && deducted == 1;
        }

        public static Thing FindHighTechItemOnMap(Map map)
        {
            if (map == null || map.listerThings == null) return null;
            List<Thing> all = map.listerThings.AllThings;
            Thing best = null;
            float bestVal = 0f;
            for (int i = 0; i < all.Count; i++)
            {
                Thing t = all[i];
                if (t == null || t.Destroyed || !t.Spawned || t.IsForbidden(Faction.OfPlayer)) continue;
                if (t.def == null || t.def.category != ThingCategory.Item) continue;
                if (t.def.techLevel < TechLevel.Spacer && t.def != ThingDefOf.ComponentSpacer) continue;
                float val = t.MarketValue;
                if (val > bestVal)
                {
                    bestVal = val;
                    best = t;
                }
            }
            return best;
        }

        public static Thing FindTechItemForOutcasts(Map map)
        {
            if (map == null || map.listerThings == null) return null;
            List<Thing> spacer = map.listerThings.ThingsOfDef(ThingDefOf.ComponentSpacer);
            for (int i = 0; i < spacer.Count; i++)
            {
                Thing t = spacer[i];
                if (t != null && !t.Destroyed && t.Spawned && !t.IsForbidden(Faction.OfPlayer)) return t;
            }
            List<Thing> ind = map.listerThings.ThingsOfDef(ThingDefOf.ComponentIndustrial);
            for (int i = 0; i < ind.Count; i++)
            {
                Thing t = ind[i];
                if (t != null && !t.Destroyed && t.Spawned && !t.IsForbidden(Faction.OfPlayer) && t.stackCount >= 2) return t;
            }
            return FindHighTechItemOnMap(map);
        }

        internal static Action<string> TestBeforePaidRewardStep;

        public static bool TryDeliverReward(Action deliver)
        {
            try
            {
                deliver();
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] FactionEvents: payment reward could not be delivered: " + ex.Message);
                return false;
            }
        }

        public static void DeliverPaidSupplies(Map map, params ResourceRequirement[] requirements)
        {
            if (map == null) throw new InvalidOperationException("No recipient map.");
            ActiveTransporterInfo pod = new ActiveTransporterInfo { openDelay = 110 };
            List<Thing> cargo = new List<Thing>();
            try
            {
                foreach (ResourceRequirement requirement in requirements)
                {
                    ThingDef def = requirement.def;
                    for (int remaining = requirement.count; remaining > 0;)
                    {
                        Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
                        cargo.Add(thing);
                        thing.stackCount = Math.Min(remaining, def.stackLimit);
                        if (!pod.innerContainer.TryAdd(thing, false) || thing.holdingOwner != pod.innerContainer)
                            throw new InvalidOperationException("Reward pod refused cargo.");
                        remaining -= thing.stackCount;
                    }
                }

                IntVec3 spot = DropCellFinder.TradeDropSpot(map);
                if (!spot.IsValid || !spot.InBounds(map))
                    throw new InvalidOperationException("No valid payment drop cell.");
                if (TestBeforePaidRewardStep != null) TestBeforePaidRewardStep("pod");
                DropPodUtility.MakeDropPodAt(spot, map, pod, Faction.OfPlayer);
                if (cargo.Any(thing => thing.Destroyed || thing.MapHeld != map))
                    throw new InvalidOperationException("Reward pod did not reach its recipient map.");
                if (TestBeforePaidRewardStep != null) TestBeforePaidRewardStep("podSpawned");
            }
            catch
            {
                foreach (Thing thing in cargo)
                {
                    if (thing.Destroyed) continue;
                    if (thing.holdingOwner != null) thing.holdingOwner.Remove(thing);
                    thing.Destroy(DestroyMode.Vanish);
                }

                Thing podThing = pod.ParentHolder as Thing;
                Thing incoming = podThing == null ? null : podThing.ParentHolder as Thing;
                if (incoming != null && !incoming.Destroyed)
                    incoming.Destroy(DestroyMode.Vanish);
                else if (podThing != null && !podThing.Destroyed)
                    podThing.Destroy(DestroyMode.Vanish);
                throw;
            }
        }

        public static List<Pawn> DeliverPaidMercenaries(Map map, Faction faction)
        {
            TMM_FactionEventsManager manager = TMM_FactionEventsManager.Instance;
            if (map == null || manager == null) throw new InvalidOperationException("No mercenary recipient.");
            IntVec3 entry = FindSafeEdgeCell(map);
            if (!entry.IsValid || !entry.InBounds(map)) throw new InvalidOperationException("No mercenary entry cell.");
            List<Pawn> hired = new List<Pawn>(2);
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    Pawn pawn = GenerateFactionPawn(Faction.OfPlayer, map, null, PawnKindDefOf.SpaceRefugee);
                    if (pawn == null) throw new InvalidOperationException("Mercenary generation failed.");
                    hired.Add(pawn);
                    if (TestBeforePaidRewardStep != null) TestBeforePaidRewardStep("mercenary" + i);
                    GenSpawn.Spawn(pawn, entry, map);
                    if (!pawn.Spawned || pawn.Map != map)
                        throw new InvalidOperationException("Mercenary arrival failed.");
                }

                manager.Schedule(new DelayedFactionTask
                {
                    kind = DelayedFactionTaskKind.MercenariesLeave,
                    triggerTick = Find.TickManager.TicksGame + 60000 * 15,
                    map = map,
                    faction = faction,
                    pawns = hired
                });
                return hired;
            }
            catch
            {
                foreach (Pawn pawn in hired)
                {
                    SaveHygiene.DiscardUnused(pawn);
                }
                throw;
            }
        }

        public static void DropItemsNearCenter(Map map, ThingDef def, int count)
        {
            if (map == null || def == null || count <= 0) return;
            List<Thing> things = new List<Thing>();
            int remaining = count;
            while (remaining > 0)
            {
                ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
                Thing t = ThingMaker.MakeThing(def, stuff);
                t.stackCount = Math.Min(remaining, def.stackLimit);
                things.Add(t);
                remaining -= t.stackCount;
            }
            IntVec3 spot = DropCellFinder.TradeDropSpot(map);
            if (!spot.IsValid) spot = map.Center;
            DropPodUtility.DropThingsNear(spot, map, things, 110, false, false, true, false); // CHECK API
        }

        public static Pawn GenerateFactionPawn(Faction faction, Map map, string preferredXenotypeDefName, PawnKindDef kindOverride)
        {
            PawnKindDef kind = kindOverride != null ? kindOverride : (faction != null ? faction.RandomPawnKind() : null);
            if (kind == null) kind = PawnKindDefOf.Colonist;

            XenotypeDef xeno = null;
            if (ModsConfig.BiotechActive && !string.IsNullOrEmpty(preferredXenotypeDefName))
            {
                xeno = DefDatabase<XenotypeDef>.GetNamedSilentFail(preferredXenotypeDefName);
            }

            PlanetTile tile = map != null ? map.Tile : PlanetTile.Invalid;
            PawnGenerationRequest req = new PawnGenerationRequest(
                kind, faction, PawnGenerationContext.NonPlayer, tile,
                true, false, false, false, true, 1f, false, true, false, false,
                false, false, false, false, false, 0f, 0f, null, 1f,
                null, null, null, null, null, null, null, null, null, null, null, null,
                false, false, false, false, null, null, null, null, null, 0f,
                DevelopmentalStage.Adult); // CHECK API

            if (xeno != null)
            {
                req.ForcedXenotype = xeno; // CHECK API
            }

            return PawnGenerator.GeneratePawn(req);
        }

        public static IntVec3 FindSafeEdgeCell(Map map)
        {
            IntVec3 cell;
            if (RCellFinder.TryFindRandomPawnEntryCell(out cell, map, CellFinder.EdgeRoadChance_Neutral))
            {
                return cell;
            }
            return map.Center;
        }

        public static bool TriggerEnemyRaid(Map map, Faction faction, float points, PawnsArrivalModeDef arrivalMode)
        {
            if (map == null || faction == null || faction.defeated) return false;
            IncidentParms raidParms = new IncidentParms();
            raidParms.target = map;
            raidParms.faction = faction;
            raidParms.points = Math.Max(points, faction.def != null ? faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat) : 200f);
            raidParms.raidArrivalMode = arrivalMode != null ? arrivalMode : PawnsArrivalModeDefOf.EdgeWalkIn;
            raidParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            raidParms.forced = true;
            return IncidentDefOf.RaidEnemy.Worker.TryExecute(raidParms);
        }

        public static void SendPawnLeaving(Pawn pawn, Map map, Faction faction)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned || map == null) return;
            Lord old = pawn.GetLord();
            if (old != null) old.RemovePawn(pawn);
            Faction f = faction != null ? faction : pawn.Faction;
            if (f != null && f != Faction.OfPlayer)
            {
                LordMaker.MakeNewLord(f, new LordJob_ExitMapBest(LocomotionUrgency.Jog, false, false), map, new List<Pawn> { pawn }); // CHECK API
            }
            else
            {
                IntVec3 exit;
                if (RCellFinder.TryFindBestExitSpot(pawn, out exit))
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Goto, exit);
                    job.exitMapOnArrival = true;
                    if (pawn.jobs != null) pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }
        }

        public static Pawn PickRandomFreeColonist(Map map)
        {
            if (map == null || map.mapPawns == null) return null;
            List<Pawn> list = map.mapPawns.FreeColonistsSpawned;
            List<Pawn> healthy = new List<Pawn>();
            for (int i = 0; i < list.Count; i++)
            {
                Pawn p = list[i];
                if (p != null && !p.Dead && !p.Downed) healthy.Add(p);
            }
            if (healthy.Count == 0) return null;
            return healthy.RandomElement();
        }
    }

    // =========================================================================
    // FACTION 1: TMM_BrotherhoodFaction (Братство Стали)
    // =========================================================================

    /// <summary>
    /// 1a. Power-armored Brotherhood patrol demands surrender of a high-tech item.
    /// </summary>
    public sealed class IncidentWorker_TMM_BosTechDemand : IncidentWorker
    {
        public const string FactionDefName = "TMM_BrotherhoodFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction bos = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (bos == null) return false;
            if (parms.forced) return true;
            return FactionEventHelper.FindHighTechItemOnMap(map) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction bos = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || bos == null) return false;

            Thing techItem = FactionEventHelper.FindHighTechItemOnMap(map);
            string itemLabel = techItem != null ? techItem.LabelShortCap : "TMM.Event.BosTechDemand.FallbackTech".Translate().ToString();

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.BosTechDemand.Title".Translate(),
                "TMM.Event.BosTechDemand.Text".Translate(bos.Name, itemLabel),
                LetterDefOf.NeutralEvent,
                techItem != null ? new LookTargets(techItem) : new LookTargets(map.Center, map),
                bos,
                FactionEventChoiceKind.BosTechDemand,
                map,
                null,
                null,
                techItem,
                0,
                15,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption surrender = new DiaOption("TMM.Event.BosTechDemand.Accept".Translate());
            bool hasItem = letter.targetThing != null && !letter.targetThing.Destroyed && letter.targetThing.Spawned && letter.targetThing.Map == letter.map;
            if (!hasItem)
            {
                surrender.Disable("TMM.Event.Common.ItemMissing".Translate());
            }
            surrender.action = delegate
            {
                if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSingleThingFromMap(letter.map, letter.targetThing))) return;
                FactionEventHelper.AdjustGoodwill(letter.faction, 15, letter.map);
                Messages.Message("TMM.Event.BosTechDemand.AcceptMsg".Translate(letter.faction != null ? letter.faction.Name : "Brotherhood"), MessageTypeDefOf.PositiveEvent);
            };
            surrender.resolveTree = true;
            options.Add(surrender);

            DiaOption refuse = new DiaOption("TMM.Event.BosTechDemand.Refuse".Translate());
            refuse.action = delegate
            {
                FactionEventHelper.AdjustGoodwill(letter.faction, -20, letter.map);
                Messages.Message("TMM.Event.BosTechDemand.RefuseMsg".Translate(letter.faction != null ? letter.faction.Name : "Brotherhood"), MessageTypeDefOf.NegativeEvent);
                letter.CompleteAndRemove();
            };
            refuse.resolveTree = true;
            options.Add(refuse);
        }
    }

    /// <summary>
    /// 1b. Brotherhood Paladin asks for 2 days of shelter.
    /// </summary>
    public sealed class IncidentWorker_TMM_BosPaladinShelter : IncidentWorker
    {
        public const string FactionDefName = "TMM_BrotherhoodFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction bos = FactionEventHelper.FindActiveFaction(FactionDefName);
            return bos != null && !bos.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction bos = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || bos == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.BosPaladinShelter.Title".Translate(),
                "TMM.Event.BosPaladinShelter.Text".Translate(bos.Name),
                LetterDefOf.PositiveEvent,
                new LookTargets(map.Center, map),
                bos,
                FactionEventChoiceKind.BosPaladinShelter,
                map,
                null,
                null,
                null,
                0,
                0,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption accept = new DiaOption("TMM.Event.BosPaladinShelter.Accept".Translate());
            accept.action = delegate
            {
                if (letter.map != null && letter.faction != null)
                {
                    Pawn paladin = FactionEventHelper.GenerateFactionPawn(letter.faction, letter.map, null, null);
                    if (paladin != null)
                    {
                        IntVec3 entry = FactionEventHelper.FindSafeEdgeCell(letter.map);
                        GenSpawn.Spawn(paladin, entry, letter.map);
                        LordMaker.MakeNewLord(letter.faction, new LordJob_VisitColony(letter.faction, letter.map.Center), letter.map, new List<Pawn> { paladin });
                        TMM_FactionEventsManager mgr = TMM_FactionEventsManager.Instance;
                        if (mgr != null)
                        {
                            mgr.Schedule(new DelayedFactionTask
                            {
                                kind = DelayedFactionTaskKind.ShelterPaladinLeave,
                                triggerTick = Find.TickManager.TicksGame + 120000, // 2 days
                                map = letter.map,
                                faction = letter.faction,
                                pawn = paladin
                            });
                        }
                        Messages.Message("TMM.Event.BosPaladinShelter.AcceptMsg".Translate(paladin.LabelShort), paladin, MessageTypeDefOf.PositiveEvent);
                    }
                }
                letter.CompleteAndRemove();
            };
            accept.resolveTree = true;
            options.Add(accept);

            DiaOption refuse = new DiaOption("TMM.Event.BosPaladinShelter.Refuse".Translate());
            refuse.action = delegate
            {
                FactionEventHelper.AdjustGoodwill(letter.faction, -5, letter.map);
                letter.CompleteAndRemove();
            };
            refuse.resolveTree = true;
            options.Add(refuse);
        }
    }

    // =========================================================================
    // FACTION 2: TMM_OutcastsFaction (Изгои Братства)
    // =========================================================================

    /// <summary>
    /// 2a. Outcasts offer medicine in exchange for technology components.
    /// </summary>
    public sealed class IncidentWorker_TMM_OutcastsTechForMeds : IncidentWorker
    {
        public const string FactionDefName = "TMM_OutcastsFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction outcasts = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (outcasts == null || outcasts.HostileTo(Faction.OfPlayer)) return false;
            return parms.forced || FactionEventHelper.FindTechItemForOutcasts(map) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction outcasts = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || outcasts == null) return false;

            Thing tech = FactionEventHelper.FindTechItemForOutcasts(map);
            string techName = tech != null ? tech.LabelShortCap : ThingDefOf.ComponentIndustrial.label;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.OutcastsTechForMeds.Title".Translate(),
                "TMM.Event.OutcastsTechForMeds.Text".Translate(outcasts.Name, techName),
                LetterDefOf.PositiveEvent,
                tech != null ? new LookTargets(tech) : new LookTargets(map.Center, map),
                outcasts,
                FactionEventChoiceKind.OutcastsTechForMeds,
                map,
                null,
                null,
                tech,
                0,
                10,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption trade = new DiaOption("TMM.Event.OutcastsTechForMeds.Accept".Translate());
            bool hasTech = letter.targetThing != null && !letter.targetThing.Destroyed && letter.targetThing.Spawned && letter.targetThing.Map == letter.map;
            if (!hasTech)
            {
                trade.Disable("TMM.Event.Common.ItemMissing".Translate());
            }
            trade.action = delegate
            {
                int count = letter.rewardAmount > 0 ? letter.rewardAmount : 10;
                if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSingleThingFromMap(letter.map, letter.targetThing,
                    () => FactionEventHelper.DeliverPaidSupplies(letter.map, new ResourceRequirement(ThingDefOf.MedicineIndustrial, count)))))
                    return;

                FactionEventHelper.AdjustGoodwill(letter.faction, 10, letter.map);
                Messages.Message("TMM.Event.OutcastsTechForMeds.AcceptMsg".Translate(), MessageTypeDefOf.PositiveEvent);
            };
            trade.resolveTree = true;
            options.Add(trade);

            DiaOption decline = new DiaOption("TMM.Event.Common.Decline".Translate());
            decline.action = delegate { letter.CompleteAndRemove(); };
            decline.resolveTree = true;
            options.Add(decline);
        }
    }

    /// <summary>
    /// 2b. Outcasts ambush a player caravan near their territory.
    /// </summary>
    public sealed class IncidentWorker_TMM_OutcastsCaravanAmbush : IncidentWorker
    {
        public const string FactionDefName = "TMM_OutcastsFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Faction outcasts = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (outcasts == null) return false;
            Caravan caravan = parms.target as Caravan;
            if (caravan == null || !caravan.Spawned || caravan.Faction != Faction.OfPlayer) return false;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Caravan caravan = parms.target as Caravan;
            Faction outcasts = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (caravan == null || outcasts == null) return false;

            int toll = 250;
            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.OutcastsCaravanAmbush.Title".Translate(),
                "TMM.Event.OutcastsCaravanAmbush.Text".Translate(outcasts.Name, toll),
                LetterDefOf.ThreatBig,
                new LookTargets(caravan),
                outcasts,
                FactionEventChoiceKind.OutcastsCaravanAmbush,
                null,
                caravan,
                null,
                null,
                toll,
                0,
                parms.points > 150f ? parms.points : 450f);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption pay = new DiaOption("TMM.Event.OutcastsCaravanAmbush.Pay".Translate(letter.silverCost));
            if (FactionEventHelper.CountSilverInCaravan(letter.caravan) < letter.silverCost)
            {
                pay.Disable("TMM.Event.Common.NotEnoughSilver".Translate(letter.silverCost));
            }
            pay.action = delegate
            {
                if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSilverFromCaravan(letter.caravan, letter.silverCost))) return;
                FactionEventHelper.AdjustGoodwill(letter.faction, 5, null);
                Messages.Message("TMM.Event.OutcastsCaravanAmbush.PaidMsg".Translate(letter.silverCost), letter.caravan, MessageTypeDefOf.NeutralEvent);
            };
            pay.resolveTree = true;
            options.Add(pay);

            DiaOption fight = new DiaOption("TMM.Event.OutcastsCaravanAmbush.Fight".Translate());
            fight.action = delegate
            {
                if (letter.caravan != null && letter.caravan.Spawned && letter.faction != null)
                {
                    FactionEventHelper.AdjustGoodwill(letter.faction, -25, null);
                    List<Pawn> ambushers = new List<Pawn>();
                    for (int i = 0; i < 3; i++)
                    {
                        Pawn p = FactionEventHelper.GenerateFactionPawn(letter.faction, null, null, null);
                        if (p != null) ambushers.Add(p);
                    }
                    if (ambushers.Count > 0)
                    {
                        Map battleMap = CaravanIncidentUtility.SetupCaravanAttackMap(letter.caravan, ambushers, true); // CHECK API
                        if (battleMap != null)
                        {
                            LordMaker.MakeNewLord(letter.faction, new LordJob_AssaultColony(letter.faction), battleMap, ambushers);
                        }
                    }
                }
                letter.CompleteAndRemove();
            };
            fight.resolveTree = true;
            options.Add(fight);
        }
    }

    // =========================================================================
    // FACTION 3: TMM_EnclaveFaction (Анклав)
    // =========================================================================

    /// <summary>
    /// 3a. Enclave radio propaganda broadcast: mood +/- depending on ideology/traits/xenotype.
    /// </summary>
    public sealed class IncidentWorker_TMM_EnclavePropaganda : IncidentWorker
    {
        public const string FactionDefName = "TMM_EnclaveFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount == 0) return false;
            return FactionEventHelper.FindActiveFaction(FactionDefName) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction enclave = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || enclave == null) return false;

            ThoughtDef inspiredDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("TMM_Thought_EnclavePropagandaPositive");
            ThoughtDef disgustedDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("TMM_Thought_EnclavePropagandaNegative");

            int positiveCount = 0;
            int negativeCount = 0;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p == null || p.Dead || p.needs == null || p.needs.mood == null) continue;

                bool likesEnclave = IsSympatheticToEnclave(p);
                ThoughtDef chosen = likesEnclave ? inspiredDef : disgustedDef;
                if (chosen != null)
                {
                    p.needs.mood.thoughts.memories.TryGainMemory(chosen);
                    if (likesEnclave) positiveCount++;
                    else negativeCount++;
                }
            }

            FactionEventHelper.SendSimpleLetter(
                "TMM.Event.EnclavePropaganda.Title".Translate(),
                "TMM.Event.EnclavePropaganda.Text".Translate(enclave.Name, positiveCount, negativeCount),
                LetterDefOf.NeutralEvent,
                new LookTargets(map.Center, map));
            return true;
        }

        private static bool IsSympatheticToEnclave(Pawn p)
        {
            // Non-baseline mutants/ghouls/demons dislike Enclave purity broadcasts.
            if (ModsConfig.BiotechActive && p.genes != null && p.genes.Xenotype != null && p.genes.Xenotype != XenotypeDefOf.Baseliner)
            {
                return false;
            }
            if (ModsConfig.IdeologyActive && p.Ideo != null && p.Ideo.memes != null)
            {
                for (int i = 0; i < p.Ideo.memes.Count; i++)
                {
                    MemeDef m = p.Ideo.memes[i];
                    if (m == null) continue;
                    string dn = m.defName;
                    if (dn == "Supremacist" || dn == "Loyalist" || dn == "HumanPrimacy" || dn == "Collectivist")
                    {
                        return true;
                    }
                }
            }
            if (p.story != null && p.story.traits != null)
            {
                if (p.story.traits.HasTrait(TraitDefOf.Bloodlust) || p.story.traits.HasTrait(TraitDefOf.Psychopath))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 3b. Enclave punitive drop-pod strike when goodwill < -50.
    /// </summary>
    public sealed class IncidentWorker_TMM_EnclavePunitiveDrop : IncidentWorker
    {
        public const string FactionDefName = "TMM_EnclaveFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction enclave = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (enclave == null) return false;
            if (!parms.forced && enclave.PlayerGoodwill >= -50) return false;
            return enclave.HostileTo(Faction.OfPlayer); // the drop is a raid: only a hostile Enclave can send it
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction enclave = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || enclave == null || !enclave.HostileTo(Faction.OfPlayer)) return false;

            float pts = parms.points > 250f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            bool ok = FactionEventHelper.TriggerEnemyRaid(map, enclave, pts, PawnsArrivalModeDefOf.EdgeDrop);
            if (ok)
            {
                FactionEventHelper.SendSimpleLetter(
                    "TMM.Event.EnclavePunitiveDrop.Title".Translate(),
                    "TMM.Event.EnclavePunitiveDrop.Text".Translate(enclave.Name),
                    LetterDefOf.ThreatBig,
                    new LookTargets(map.Center, map));
            }
            return ok;
        }
    }

    // =========================================================================
    // FACTION 4: TMM_InstituteFaction (Институт)
    // =========================================================================

    /// <summary>
    /// 4a. Institute Courser abduction squad: small fast raid targeting a colonist.
    /// </summary>
    public sealed class IncidentWorker_TMM_InstituteCourserAbduction : IncidentWorker
    {
        public const string FactionDefName = "TMM_InstituteFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount == 0) return false;
            Faction f = FactionEventHelper.FindActiveFaction(FactionDefName);
            return f != null && f.HostileTo(Faction.OfPlayer); // a raid needs a hostile Institute
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction inst = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || inst == null || !inst.HostileTo(Faction.OfPlayer)) return false;

            float smallPoints = Mathf.Clamp((parms.points > 0f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map)) * 0.65f, 250f, 950f);
            bool ok = FactionEventHelper.TriggerEnemyRaid(map, inst, smallPoints, PawnsArrivalModeDefOf.CenterDrop);
            if (ok)
            {
                FactionEventHelper.SendSimpleLetter(
                    "TMM.Event.InstituteCourserAbduction.Title".Translate(),
                    "TMM.Event.InstituteCourserAbduction.Text".Translate(inst.Name),
                    LetterDefOf.ThreatBig,
                    new LookTargets(map.Center, map));
            }
            return ok;
        }
    }

    /// <summary>
    /// 4b. Institute synth replacement: secretly marks a colonist with a hidden synth hediff that reveals later.
    /// // TODO story: integrate full synth suspicion investigation, Safe Test dialogue, and branching confrontation.
    /// </summary>
    public sealed class IncidentWorker_TMM_InstituteSynthReplacement : IncidentWorker
    {
        public const string FactionDefName = "TMM_InstituteFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount < 2) return false;
            return FactionEventHelper.FindActiveFaction(FactionDefName) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction inst = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || inst == null) return false;

            Pawn victim = FactionEventHelper.PickRandomFreeColonist(map);
            if (victim == null) return false;

            HediffDef hiddenDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_HiddenSynth");
            if (hiddenDef != null && victim.health != null && !victim.health.hediffSet.HasHediff(hiddenDef))
            {
                victim.health.AddHediff(hiddenDef);
            }

            TMM_FactionEventsManager mgr = TMM_FactionEventsManager.Instance;
            if (mgr != null)
            {
                mgr.Schedule(new DelayedFactionTask
                {
                    kind = DelayedFactionTaskKind.HiddenSynthReveal,
                    triggerTick = Find.TickManager.TicksGame + 60000 * 10, // reveals in 10 days
                    map = map,
                    faction = inst,
                    pawn = victim
                });
            }

            // Send subtle eerie notice so player senses something is off without naming the infiltrator.
            FactionEventHelper.SendSimpleLetter(
                "TMM.Event.InstituteSynthReplacement.Title".Translate(),
                "TMM.Event.InstituteSynthReplacement.Text".Translate(inst.Name),
                LetterDefOf.NeutralEvent,
                new LookTargets(map.Center, map));
            return true;
        }
    }

    // =========================================================================
    // FACTION 5: TMM_RailroadFaction (Подземка)
    // =========================================================================

    /// <summary>
    /// 5a. Fugitive synth asks for sanctuary: joins colony, Institute pursuit raid arrives days later.
    /// </summary>
    public sealed class IncidentWorker_TMM_RailroadFugitiveSynth : IncidentWorker
    {
        public const string FactionDefName = "TMM_RailroadFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction rr = FactionEventHelper.FindActiveFaction(FactionDefName);
            return rr != null && !rr.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction rr = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || rr == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.RailroadFugitiveSynth.Title".Translate(),
                "TMM.Event.RailroadFugitiveSynth.Text".Translate(rr.Name),
                LetterDefOf.PositiveEvent,
                new LookTargets(map.Center, map),
                rr,
                FactionEventChoiceKind.RailroadFugitiveSynth,
                map,
                null,
                null,
                null,
                0,
                0,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption accept = new DiaOption("TMM.Event.RailroadFugitiveSynth.Accept".Translate());
            accept.action = delegate
            {
                if (letter.map != null)
                {
                    Pawn refugee = FactionEventHelper.GenerateFactionPawn(Faction.OfPlayer, letter.map, "TMM_SynthGen3", PawnKindDefOf.Colonist);
                    if (refugee != null)
                    {
                        IntVec3 entry = FactionEventHelper.FindSafeEdgeCell(letter.map);
                        GenSpawn.Spawn(refugee, entry, letter.map);
                        FactionEventHelper.AdjustGoodwill(letter.faction, 15, letter.map);

                        Faction inst = FactionEventHelper.FindActiveFaction("TMM_InstituteFaction");
                        TMM_FactionEventsManager mgr = TMM_FactionEventsManager.Instance;
                        if (inst != null && mgr != null)
                        {
                            mgr.Schedule(new DelayedFactionTask
                            {
                                kind = DelayedFactionTaskKind.DelayedRaid,
                                triggerTick = Find.TickManager.TicksGame + 60000 * 3, // 3 days
                                map = letter.map,
                                faction = inst,
                                points = letter.threatPoints > 200f ? letter.threatPoints : StorytellerUtility.DefaultThreatPointsNow(letter.map)
                            });
                        }
                        Messages.Message("TMM.Event.RailroadFugitiveSynth.AcceptMsg".Translate(refugee.LabelShort), refugee, MessageTypeDefOf.PositiveEvent);
                    }
                }
                letter.CompleteAndRemove();
            };
            accept.resolveTree = true;
            options.Add(accept);

            DiaOption refuse = new DiaOption("TMM.Event.RailroadFugitiveSynth.Refuse".Translate());
            refuse.action = delegate
            {
                FactionEventHelper.AdjustGoodwill(letter.faction, -5, letter.map);
                letter.CompleteAndRemove();
            };
            refuse.resolveTree = true;
            options.Add(refuse);
        }
    }

    /// <summary>
    /// 5b. Railroad dead-drop cache discovered on the colony map.
    /// </summary>
    public sealed class IncidentWorker_TMM_RailroadDeadDrop : IncidentWorker
    {
        public const string FactionDefName = "TMM_RailroadFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction rr = FactionEventHelper.FindActiveFaction(FactionDefName);
            return rr != null && !rr.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction rr = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || rr == null) return false;

            IntVec3 spot;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 35,
                delegate(IntVec3 c) { return c.InBounds(map) && c.Standable(map) && !c.Fogged(map) && !c.Roofed(map); },
                out spot))
            {
                spot = DropCellFinder.TradeDropSpot(map);
            }
            if (!spot.IsValid) spot = map.Center;

            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = 220;
            Thing meds = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
            meds.stackCount = 5;
            Thing comps = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
            comps.stackCount = 4;

            GenPlace.TryPlaceThing(silver, spot, map, ThingPlaceMode.Near);
            GenPlace.TryPlaceThing(meds, spot, map, ThingPlaceMode.Near);
            GenPlace.TryPlaceThing(comps, spot, map, ThingPlaceMode.Near);

            FactionEventHelper.SendSimpleLetter(
                "TMM.Event.RailroadDeadDrop.Title".Translate(),
                "TMM.Event.RailroadDeadDrop.Text".Translate(rr.Name),
                LetterDefOf.PositiveEvent,
                new LookTargets(spot, map));
            return true;
        }
    }

    // =========================================================================
    // FACTION 6: TMM_GunnersFaction (Стрелки)
    // =========================================================================

    /// <summary>
    /// 6a. Gunners extortion: pay protection silver or face an immediate raid.
    /// </summary>
    public sealed class IncidentWorker_TMM_GunnersExtortion : IncidentWorker
    {
        public const string FactionDefName = "TMM_GunnersFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            return FactionEventHelper.FindActiveFaction(FactionDefName) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction gunners = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || gunners == null) return false;

            float pts = parms.points > 200f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            int demand = Mathf.Clamp((int)(pts * 0.45f), 250, 1500);

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.GunnersExtortion.Title".Translate(),
                "TMM.Event.GunnersExtortion.Text".Translate(gunners.Name, demand),
                LetterDefOf.ThreatSmall,
                new LookTargets(map.Center, map),
                gunners,
                FactionEventChoiceKind.GunnersExtortion,
                map,
                null,
                null,
                null,
                demand,
                0,
                pts);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption pay = new DiaOption("TMM.Event.GunnersExtortion.Pay".Translate(letter.silverCost));
            if (FactionEventHelper.CountSilverOnMap(letter.map) < letter.silverCost)
            {
                pay.Disable("TMM.Event.Common.NotEnoughSilver".Translate(letter.silverCost));
            }
            pay.action = delegate
            {
                if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSilverFromMap(letter.map, letter.silverCost))) return;
                FactionEventHelper.AdjustGoodwill(letter.faction, 8, letter.map);
                Messages.Message("TMM.Event.GunnersExtortion.PaidMsg".Translate(letter.silverCost), MessageTypeDefOf.NeutralEvent);
            };
            pay.resolveTree = true;
            options.Add(pay);

            DiaOption refuse = new DiaOption("TMM.Event.GunnersExtortion.Refuse".Translate());
            refuse.action = delegate
            {
                FactionEventHelper.AdjustGoodwill(letter.faction, -15, letter.map);
                FactionEventHelper.TriggerEnemyRaid(letter.map, letter.faction, letter.threatPoints > 200f ? letter.threatPoints : 500f, PawnsArrivalModeDefOf.EdgeWalkIn);
                letter.CompleteAndRemove();
            };
            refuse.resolveTree = true;
            options.Add(refuse);
        }
    }

    /// <summary>
    /// 6b. Gunners mercenaries offer a 1-season (15 days) defense contract for silver.
    /// </summary>
    public sealed class IncidentWorker_TMM_GunnersMercContract : IncidentWorker
    {
        public const string FactionDefName = "TMM_GunnersFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            return FactionEventHelper.FindActiveFaction(FactionDefName) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction gunners = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || gunners == null) return false;

            int fee = 600;
            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.GunnersMercContract.Title".Translate(),
                "TMM.Event.GunnersMercContract.Text".Translate(gunners.Name, fee),
                LetterDefOf.NeutralEvent,
                new LookTargets(map.Center, map),
                gunners,
                FactionEventChoiceKind.GunnersMercContract,
                map,
                null,
                null,
                null,
                fee,
                2,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption hire = new DiaOption("TMM.Event.GunnersMercContract.Hire".Translate(letter.silverCost));
            if (FactionEventHelper.CountSilverOnMap(letter.map) < letter.silverCost)
            {
                hire.Disable("TMM.Event.Common.NotEnoughSilver".Translate(letter.silverCost));
            }
            hire.action = delegate
            {
                List<Pawn> hired = null;
                if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSilverFromMap(letter.map, letter.silverCost,
                    () => hired = FactionEventHelper.DeliverPaidMercenaries(letter.map, letter.faction))))
                    return;

                FactionEventHelper.AdjustGoodwill(letter.faction, 10, letter.map);
                Messages.Message("TMM.Event.GunnersMercContract.HiredMsg".Translate(hired.Count), MessageTypeDefOf.PositiveEvent);
            };
            hire.resolveTree = true;
            options.Add(hire);

            DiaOption decline = new DiaOption("TMM.Event.Common.Decline".Translate());
            decline.action = delegate { letter.CompleteAndRemove(); };
            decline.resolveTree = true;
            options.Add(decline);
        }
    }

    // =========================================================================
    // FACTION 7: TMM_VaultTecFaction (Волт-Тек)
    // =========================================================================

    /// <summary>
    /// 7a. Vault-Tec "voluntary" clinical experiment: grants random experimental hediff/trait/gene + silver stipend.
    /// </summary>
    public sealed class IncidentWorker_TMM_VaultTecExperiment : IncidentWorker
    {
        public const string FactionDefName = "TMM_VaultTecFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount == 0) return false;
            Faction vt = FactionEventHelper.FindActiveFaction(FactionDefName);
            return vt != null && !vt.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction vt = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || vt == null) return false;

            Pawn volunteer = FactionEventHelper.PickRandomFreeColonist(map);
            if (volunteer == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.VaultTecExperiment.Title".Translate(),
                "TMM.Event.VaultTecExperiment.Text".Translate(vt.Name, volunteer.LabelShort),
                LetterDefOf.NeutralEvent,
                new LookTargets(volunteer),
                vt,
                FactionEventChoiceKind.VaultTecExperiment,
                map,
                null,
                volunteer,
                null,
                0,
                300,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption accept = new DiaOption("TMM.Event.VaultTecExperiment.Accept".Translate(letter.rewardAmount));
            accept.action = delegate
            {
                if (letter.subjectPawn != null && !letter.subjectPawn.Dead && letter.map != null)
                {
                    HediffDef expDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_VaultTecSerum");
                    if (expDef != null && letter.subjectPawn.health != null)
                    {
                        letter.subjectPawn.health.AddHediff(expDef);
                    }
                    // Also grant a mild quirky trait if space allows (e.g. FastLearner or IronWilled).
                    if (letter.subjectPawn.story != null && letter.subjectPawn.story.traits != null
                        && letter.subjectPawn.story.traits.allTraits.Count < 4)
                    {
                        TraitDef bonusTrait = Rand.Bool ? DefDatabase<TraitDef>.GetNamed("FastLearner") : DefDatabase<TraitDef>.GetNamed("Tough");
                        if (bonusTrait != null && !letter.subjectPawn.story.traits.HasTrait(bonusTrait))
                        {
                            letter.subjectPawn.story.traits.GainTrait(new Trait(bonusTrait));
                        }
                    }
                    FactionEventHelper.DropItemsNearCenter(letter.map, ThingDefOf.Silver, letter.rewardAmount > 0 ? letter.rewardAmount : 300);
                    FactionEventHelper.AdjustGoodwill(letter.faction, 12, letter.map);
                    Messages.Message("TMM.Event.VaultTecExperiment.AcceptMsg".Translate(letter.subjectPawn.LabelShort), letter.subjectPawn, MessageTypeDefOf.PositiveEvent);
                }
                letter.CompleteAndRemove();
            };
            accept.resolveTree = true;
            options.Add(accept);

            DiaOption decline = new DiaOption("TMM.Event.Common.Decline".Translate());
            decline.action = delegate { letter.CompleteAndRemove(); };
            decline.resolveTree = true;
            options.Add(decline);
        }
    }

    /// <summary>
    /// 7b. Vault-Tec surplus supply caravan / subsidized crate offer.
    /// </summary>
    public sealed class IncidentWorker_TMM_VaultTecSupplyCaravan : IncidentWorker
    {
        public const string FactionDefName = "TMM_VaultTecFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction vt = FactionEventHelper.FindActiveFaction(FactionDefName);
            return vt != null && !vt.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction vt = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || vt == null) return false;

            int price = 350;
            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.VaultTecSupplyCaravan.Title".Translate(),
                "TMM.Event.VaultTecSupplyCaravan.Text".Translate(vt.Name, price),
                LetterDefOf.PositiveEvent,
                new LookTargets(map.Center, map),
                vt,
                FactionEventChoiceKind.VaultTecSupplyCaravan,
                map,
                null,
                null,
                null,
                price,
                0,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption buy = new DiaOption("TMM.Event.VaultTecSupplyCaravan.Buy".Translate(letter.silverCost));
            if (FactionEventHelper.CountSilverOnMap(letter.map) < letter.silverCost)
            {
                buy.Disable("TMM.Event.Common.NotEnoughSilver".Translate(letter.silverCost));
            }
            buy.action = delegate
            {
                if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSilverFromMap(letter.map, letter.silverCost,
                    () => FactionEventHelper.DeliverPaidSupplies(letter.map,
                        new ResourceRequirement(ThingDefOf.MealSurvivalPack, 16),
                        new ResourceRequirement(ThingDefOf.MedicineIndustrial, 8),
                        new ResourceRequirement(ThingDefOf.ComponentIndustrial, 6)))))
                    return;

                FactionEventHelper.AdjustGoodwill(letter.faction, 8, letter.map);
                Messages.Message("TMM.Event.VaultTecSupplyCaravan.BoughtMsg".Translate(), MessageTypeDefOf.PositiveEvent);
            };
            buy.resolveTree = true;
            options.Add(buy);

            DiaOption decline = new DiaOption("TMM.Event.Common.Decline".Translate());
            decline.action = delegate { letter.CompleteAndRemove(); };
            decline.resolveTree = true;
            options.Add(decline);
        }
    }

    // =========================================================================
    // FACTION 8: TMM_MinutemenHuntersFaction (Минитмены и охотники)
    // =========================================================================

    /// <summary>
    /// 8a. Minutemen distress signal: spawns a help-needed site on the world map upon acceptance.
    /// </summary>
    public sealed class IncidentWorker_TMM_MinutemenDistressSignal : IncidentWorker
    {
        public const string FactionDefName = "TMM_MinutemenHuntersFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || Find.World == null) return false;
            Faction mm = FactionEventHelper.FindActiveFaction(FactionDefName);
            return mm != null && !mm.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction mm = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || mm == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.MinutemenDistressSignal.Title".Translate(),
                "TMM.Event.MinutemenDistressSignal.Text".Translate(mm.Name),
                LetterDefOf.NeutralEvent,
                new LookTargets(map.Center, map),
                mm,
                FactionEventChoiceKind.MinutemenDistressSignal,
                map,
                null,
                null,
                null,
                0,
                0,
                parms.points > 150f ? parms.points : 500f);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption accept = new DiaOption("TMM.Event.MinutemenDistressSignal.Accept".Translate());
            accept.action = delegate
            {
                PlanetTile siteTile;
                Map originMap = letter.map != null ? letter.map : Find.AnyPlayerHomeMap;
                if (originMap != null && TileFinder.TryFindNewSiteTile(out siteTile, originMap.Tile, 3, 10, false, null)) // API checked: nearTile overload
                {
                    Faction hostile = Find.FactionManager != null
                        ? Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined)
                        : null;
                    SitePartDef outpostPart = DefDatabase<SitePartDef>.GetNamedSilentFail("Outpost"); // CHECK API
                    if (outpostPart != null && hostile != null)
                    {
                        Site site = SiteMaker.MakeSite(outpostPart, siteTile, hostile, true, letter.threatPoints); // CHECK API
                        if (site != null)
                        {
                            TimeoutComp tc = site.GetComponent<TimeoutComp>();
                            if (tc != null) tc.StartTimeout(60000 * 12);
                            Find.WorldObjects.Add(site);
                            FactionEventHelper.AdjustGoodwill(letter.faction, 8, letter.map);
                            Messages.Message("TMM.Event.MinutemenDistressSignal.SiteCreatedMsg".Translate(), new LookTargets(site), MessageTypeDefOf.PositiveEvent);
                        }
                    }
                }
                letter.CompleteAndRemove();
            };
            accept.resolveTree = true;
            options.Add(accept);

            DiaOption decline = new DiaOption("TMM.Event.MinutemenDistressSignal.Refuse".Translate());
            decline.action = delegate
            {
                FactionEventHelper.AdjustGoodwill(letter.faction, -5, letter.map);
                letter.CompleteAndRemove();
            };
            decline.resolveTree = true;
            options.Add(decline);
        }
    }

    /// <summary>
    /// 8b. Hunter warning: reveals an incoming Monster Clans raid 2 days ahead of time.
    /// </summary>
    public sealed class IncidentWorker_TMM_HuntersMonsterWarning : IncidentWorker
    {
        public const string FactionDefName = "TMM_MinutemenHuntersFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction mm = FactionEventHelper.FindActiveFaction(FactionDefName);
            Faction monsters = FactionEventHelper.FindActiveFaction("TMM_MonsterClansFaction");
            return mm != null && !mm.HostileTo(Faction.OfPlayer) && monsters != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction mm = FactionEventHelper.FindActiveFaction(FactionDefName);
            Faction monsters = FactionEventHelper.FindActiveFaction("TMM_MonsterClansFaction");
            if (map == null || mm == null || monsters == null) return false;

            float pts = parms.points > 200f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            TMM_FactionEventsManager mgr = TMM_FactionEventsManager.Instance;
            if (mgr != null)
            {
                mgr.Schedule(new DelayedFactionTask
                {
                    kind = DelayedFactionTaskKind.DelayedRaid,
                    triggerTick = Find.TickManager.TicksGame + 60000 * 2, // 2 days warning
                    map = map,
                    faction = monsters,
                    points = pts
                });
            }

            FactionEventHelper.SendSimpleLetter(
                "TMM.Event.HuntersMonsterWarning.Title".Translate(),
                "TMM.Event.HuntersMonsterWarning.Text".Translate(mm.Name, monsters.Name),
                LetterDefOf.NeutralEvent,
                new LookTargets(map.Center, map));
            return true;
        }
    }
}
