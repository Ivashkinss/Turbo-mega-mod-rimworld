using System;
using System.Collections.Generic;
using ResourceRequirement = TurboMegaMod.StoryPayment.ResourceRequirement;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    // =========================================================================
    // FACTION 9: TMM_MenOfLettersFaction (Британские Просвещённые)
    // =========================================================================

    /// <summary>
    /// 9a. Men of Letters offer to buy supernatural monster records (TMM_HuntersFieldArchive if present, or copied field notes).
    /// </summary>
    public sealed class IncidentWorker_TMM_MolBuyRecords : IncidentWorker
    {
        public const string FactionDefName = "TMM_MenOfLettersFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount == 0) return false;
            Faction mol = FactionEventHelper.FindActiveFaction(FactionDefName);
            return mol != null && !mol.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction mol = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || mol == null) return false;

            Thing archive = FindArchiveOnMap(map);
            int offer = archive != null ? 1100 : 380;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.MolBuyRecords.Title".Translate(),
                archive != null
                    ? "TMM.Event.MolBuyRecords.TextArchive".Translate(mol.Name, archive.LabelShortCap, offer)
                    : "TMM.Event.MolBuyRecords.TextNotes".Translate(mol.Name, offer),
                LetterDefOf.PositiveEvent,
                archive != null ? new LookTargets(archive) : new LookTargets(map.Center, map),
                mol,
                FactionEventChoiceKind.MolBuyRecords,
                map,
                null,
                null,
                archive,
                0,
                offer,
                parms.points);
            return true;
        }

        private static Thing FindArchiveOnMap(Map map)
        {
            if (map == null || map.listerThings == null) return null;
            ThingDef archiveDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_HuntersFieldArchive");
            if (archiveDef == null) return null;
            List<Thing> list = map.listerThings.ThingsOfDef(archiveDef);
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t != null && !t.Destroyed && t.Spawned && !t.IsForbidden(Faction.OfPlayer)) return t;
            }
            return null;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            bool hasArchive = letter.targetThing != null && !letter.targetThing.Destroyed && letter.targetThing.Spawned && letter.targetThing.Map == letter.map;

            DiaOption sell = new DiaOption(
                hasArchive
                    ? "TMM.Event.MolBuyRecords.SellArchive".Translate(letter.rewardAmount)
                    : "TMM.Event.MolBuyRecords.SellNotes".Translate(letter.rewardAmount));
            sell.action = delegate
            {
                int amount = letter.rewardAmount > 0 ? letter.rewardAmount : (hasArchive ? 1100 : 380);
                Action deliver = () => FactionEventHelper.DeliverPaidSupplies(letter.map, new ResourceRequirement(ThingDefOf.Silver, amount));
                if (hasArchive)
                {
                    if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSingleThingFromMap(letter.map, letter.targetThing, deliver))) return;
                }
                else if (!letter.TryResolvePayment(() => FactionEventHelper.TryDeliverReward(deliver))) return;

                FactionEventHelper.AdjustGoodwill(letter.faction, hasArchive ? 18 : 8, letter.map);
                Messages.Message("TMM.Event.MolBuyRecords.SoldMsg".Translate(amount), MessageTypeDefOf.PositiveEvent);
            };
            sell.resolveTree = true;
            options.Add(sell);

            DiaOption decline = new DiaOption("TMM.Event.Common.Decline".Translate());
            decline.action = delegate { letter.CompleteAndRemove(); };
            decline.resolveTree = true;
            options.Add(decline);
        }
    }

    /// <summary>
    /// 9b. Men of Letters offer to recruit a talented colonist in exchange for silver, spacer components, and goodwill.
    /// </summary>
    public sealed class IncidentWorker_TMM_MolRecruitment : IncidentWorker
    {
        public const string FactionDefName = "TMM_MenOfLettersFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount < 2) return false;
            Faction mol = FactionEventHelper.FindActiveFaction(FactionDefName);
            return mol != null && !mol.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction mol = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || mol == null) return false;

            Pawn candidate = PickSmartestColonist(map);
            if (candidate == null) return false;

            int stipend = 950;
            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.MolRecruitment.Title".Translate(),
                "TMM.Event.MolRecruitment.Text".Translate(mol.Name, candidate.LabelShort, stipend),
                LetterDefOf.NeutralEvent,
                new LookTargets(candidate),
                mol,
                FactionEventChoiceKind.MolRecruitment,
                map,
                null,
                candidate,
                null,
                0,
                stipend,
                parms.points);
            return true;
        }

        private static Pawn PickSmartestColonist(Map map)
        {
            List<Pawn> list = map.mapPawns.FreeColonistsSpawned;
            Pawn best = null;
            int bestInt = -1;
            for (int i = 0; i < list.Count; i++)
            {
                Pawn p = list[i];
                if (p == null || p.Dead || p.Downed) continue;
                int lvl = p.skills != null && p.skills.GetSkill(SkillDefOf.Intellectual) != null
                    ? p.skills.GetSkill(SkillDefOf.Intellectual).Level
                    : 0;
                if (lvl > bestInt)
                {
                    bestInt = lvl;
                    best = p;
                }
            }
            return best;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption accept = new DiaOption("TMM.Event.MolRecruitment.Accept".Translate(
                letter.subjectPawn != null ? letter.subjectPawn.LabelShort : "TMM.Event.Common.Colonist".Translate().ToString(),
                letter.rewardAmount));
            bool validSubject = letter.subjectPawn != null
                && !letter.subjectPawn.Dead
                && !letter.subjectPawn.Destroyed
                && letter.subjectPawn.Faction == Faction.OfPlayer
                && letter.map != null
                && letter.map.mapPawns.FreeColonistsSpawnedCount > 1;
            if (!validSubject)
            {
                accept.Disable("TMM.Event.Common.ColonistUnavailable".Translate());
            }
            accept.action = delegate
            {
                if (letter.subjectPawn != null && !letter.subjectPawn.Dead && letter.faction != null && letter.map != null)
                {
                    letter.subjectPawn.SetFaction(letter.faction);
                    FactionEventHelper.SendPawnLeaving(letter.subjectPawn, letter.map, letter.faction);
                    FactionEventHelper.DropItemsNearCenter(letter.map, ThingDefOf.Silver, letter.rewardAmount > 0 ? letter.rewardAmount : 950);
                    FactionEventHelper.DropItemsNearCenter(letter.map, ThingDefOf.ComponentSpacer, 2);
                    FactionEventHelper.AdjustGoodwill(letter.faction, 25, letter.map);
                    Messages.Message("TMM.Event.MolRecruitment.AcceptMsg".Translate(letter.subjectPawn.LabelShort, letter.faction.Name), MessageTypeDefOf.PositiveEvent);
                }
                letter.CompleteAndRemove();
            };
            accept.resolveTree = true;
            options.Add(accept);

            DiaOption refuse = new DiaOption("TMM.Event.MolRecruitment.Refuse".Translate());
            refuse.action = delegate { letter.CompleteAndRemove(); };
            refuse.resolveTree = true;
            options.Add(refuse);
        }
    }

    // =========================================================================
    // FACTION 10: TMM_HeavenFaction (Воинство Небес)
    // =========================================================================

    /// <summary>
    /// 10a. An angel asks a volunteer colonist for consent to inhabit them as a vessel.
    /// </summary>
    public sealed class IncidentWorker_TMM_HeavenVesselRequest : IncidentWorker
    {
        public const string FactionDefName = "TMM_HeavenFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount == 0) return false;
            Faction heaven = FactionEventHelper.FindActiveFaction(FactionDefName);
            return heaven != null && !heaven.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction heaven = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || heaven == null) return false;

            Pawn volunteer = FactionEventHelper.PickRandomFreeColonist(map);
            if (volunteer == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.HeavenVesselRequest.Title".Translate(),
                "TMM.Event.HeavenVesselRequest.Text".Translate(heaven.Name, volunteer.LabelShort),
                LetterDefOf.NeutralEvent,
                new LookTargets(volunteer),
                heaven,
                FactionEventChoiceKind.HeavenVesselRequest,
                map,
                null,
                volunteer,
                null,
                0,
                0,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption consent = new DiaOption("TMM.Event.HeavenVesselRequest.Accept".Translate(
                letter.subjectPawn != null ? letter.subjectPawn.LabelShort : "TMM.Event.Common.Colonist".Translate().ToString()));
            if (letter.subjectPawn == null || letter.subjectPawn.Dead || letter.subjectPawn.Destroyed)
            {
                consent.Disable("TMM.Event.Common.ColonistUnavailable".Translate());
            }
            consent.action = delegate
            {
                if (letter.subjectPawn != null && !letter.subjectPawn.Dead && letter.subjectPawn.health != null)
                {
                    HediffDef vesselDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_AngelicVessel");
                    if (vesselDef != null && !letter.subjectPawn.health.hediffSet.HasHediff(vesselDef))
                    {
                        letter.subjectPawn.health.AddHediff(vesselDef);
                    }
                    // Also apply angelic blessing memory
                    ThoughtDef blessed = DefDatabase<ThoughtDef>.GetNamedSilentFail("TMM_Thought_DivineOmenPositive");
                    if (blessed != null && letter.subjectPawn.needs != null && letter.subjectPawn.needs.mood != null)
                    {
                        letter.subjectPawn.needs.mood.thoughts.memories.TryGainMemory(blessed);
                    }
                    FactionEventHelper.AdjustGoodwill(letter.faction, 20, letter.map);
                    Messages.Message("TMM.Event.HeavenVesselRequest.AcceptMsg".Translate(letter.subjectPawn.LabelShort), letter.subjectPawn, MessageTypeDefOf.PositiveEvent);
                }
                letter.CompleteAndRemove();
            };
            consent.resolveTree = true;
            options.Add(consent);

            DiaOption refuse = new DiaOption("TMM.Event.HeavenVesselRequest.Refuse".Translate());
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
    /// 10b. Divine omen in the sky: +mood for faithful/good colonists, -mood for demons/monsters.
    /// </summary>
    public sealed class IncidentWorker_TMM_HeavenDivineOmen : IncidentWorker
    {
        public const string FactionDefName = "TMM_HeavenFaction";

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
            Faction heaven = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || heaven == null) return false;

            ThoughtDef blessedDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("TMM_Thought_DivineOmenPositive");
            ThoughtDef burnedDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("TMM_Thought_DivineOmenNegative");

            int blessedCount = 0;
            int burnedCount = 0;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p == null || p.Dead || p.needs == null || p.needs.mood == null) continue;

                bool unholy = IsDemonOrUnholy(p);
                ThoughtDef chosen = unholy ? burnedDef : blessedDef;
                if (chosen != null)
                {
                    p.needs.mood.thoughts.memories.TryGainMemory(chosen);
                    if (unholy) burnedCount++;
                    else blessedCount++;
                }
            }

            FactionEventHelper.SendSimpleLetter(
                "TMM.Event.HeavenDivineOmen.Title".Translate(),
                "TMM.Event.HeavenDivineOmen.Text".Translate(heaven.Name, blessedCount, burnedCount),
                LetterDefOf.PositiveEvent,
                new LookTargets(map.Center, map));
            return true;
        }

        private static bool IsDemonOrUnholy(Pawn p)
        {
            if (ModsConfig.BiotechActive && p.genes != null && p.genes.Xenotype != null)
            {
                string x = p.genes.Xenotype.defName;
                if (x == "TMM_Demon" || x == "TMM_Leviathan" || x == "Sanguophage") return true;
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

    // =========================================================================
    // FACTION 11: TMM_HellFaction (Демоны Ада)
    // =========================================================================

    /// <summary>
    /// 11a. Crossroads demon offers a deal: immediate wealth & medicine, debt collected in 1 year (60 days).
    /// </summary>
    public sealed class IncidentWorker_TMM_HellCrossroadsDeal : IncidentWorker
    {
        public const string FactionDefName = "TMM_HellFaction";

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
            Faction hell = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || hell == null) return false;

            Pawn signer = FactionEventHelper.PickRandomFreeColonist(map);
            int silverGrant = 1500;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.HellCrossroadsDeal.Title".Translate(),
                "TMM.Event.HellCrossroadsDeal.Text".Translate(hell.Name, silverGrant),
                LetterDefOf.NeutralEvent,
                signer != null ? new LookTargets(signer) : new LookTargets(map.Center, map),
                hell,
                FactionEventChoiceKind.HellCrossroadsDeal,
                map,
                null,
                signer,
                null,
                0,
                silverGrant,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption sign = new DiaOption("TMM.Event.HellCrossroadsDeal.Accept".Translate(letter.rewardAmount));
            sign.action = delegate
            {
                if (letter.map != null)
                {
                    FactionEventHelper.DropItemsNearCenter(letter.map, ThingDefOf.Silver, letter.rewardAmount > 0 ? letter.rewardAmount : 1500);
                    FactionEventHelper.DropItemsNearCenter(letter.map, ThingDefOf.MedicineUltratech, 4);
                    FactionEventHelper.AdjustGoodwill(letter.faction, 15, letter.map);

                    TMM_FactionEventsManager mgr = TMM_FactionEventsManager.Instance;
                    if (mgr != null && letter.faction != null)
                    {
                        mgr.Schedule(new DelayedFactionTask
                        {
                            kind = DelayedFactionTaskKind.CrossroadsDebtDue,
                            triggerTick = Find.TickManager.TicksGame + 60000 * 60, // 1 RimWorld year
                            map = letter.map,
                            faction = letter.faction,
                            pawn = letter.subjectPawn,
                            points = Math.Max(letter.threatPoints, StorytellerUtility.DefaultThreatPointsNow(letter.map) * 1.25f)
                        });
                    }
                    Messages.Message("TMM.Event.HellCrossroadsDeal.AcceptMsg".Translate(), MessageTypeDefOf.NeutralEvent);
                }
                letter.CompleteAndRemove();
            };
            sign.resolveTree = true;
            options.Add(sign);

            DiaOption refuse = new DiaOption("TMM.Event.HellCrossroadsDeal.Refuse".Translate());
            refuse.action = delegate { letter.CompleteAndRemove(); };
            refuse.resolveTree = true;
            options.Add(refuse);
        }
    }

    /// <summary>
    /// 11b. A demon attempts to possess a colonist: player can perform an exorcism (costs silver/salt supplies) or endure possession.
    /// </summary>
    public sealed class IncidentWorker_TMM_HellDemonicPossession : IncidentWorker
    {
        public const string FactionDefName = "TMM_HellFaction";

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
            Faction hell = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || hell == null) return false;

            Pawn victim = FactionEventHelper.PickRandomFreeColonist(map);
            if (victim == null) return false;

            int exorcismCost = 150;
            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.HellDemonicPossession.Title".Translate(),
                "TMM.Event.HellDemonicPossession.Text".Translate(victim.LabelShort, hell.Name, exorcismCost),
                LetterDefOf.ThreatSmall,
                new LookTargets(victim),
                hell,
                FactionEventChoiceKind.HellDemonicPossession,
                map,
                null,
                victim,
                null,
                exorcismCost,
                0,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption exorcise = new DiaOption("TMM.Event.HellDemonicPossession.Exorcise".Translate(letter.silverCost));
            if (FactionEventHelper.CountSilverOnMap(letter.map) < letter.silverCost)
            {
                exorcise.Disable("TMM.Event.Common.NotEnoughSilver".Translate(letter.silverCost));
            }
            exorcise.action = delegate
            {
                if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSilverFromMap(letter.map, letter.silverCost))) return;
                FactionEventHelper.AdjustGoodwill(letter.faction, -10, letter.map);
                Messages.Message("TMM.Event.HellDemonicPossession.ExorcisedMsg".Translate(
                    letter.subjectPawn != null ? letter.subjectPawn.LabelShort : "TMM.Event.Common.Colonist".Translate().ToString()),
                    letter.subjectPawn, MessageTypeDefOf.PositiveEvent);
            };
            exorcise.resolveTree = true;
            options.Add(exorcise);

            DiaOption endure = new DiaOption("TMM.Event.HellDemonicPossession.Endure".Translate());
            endure.action = delegate
            {
                if (letter.subjectPawn != null && !letter.subjectPawn.Dead && letter.subjectPawn.health != null)
                {
                    HediffDef possDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_DemonicPossession");
                    if (possDef != null)
                    {
                        letter.subjectPawn.health.AddHediff(possDef);
                    }
                    Messages.Message("TMM.Event.HellDemonicPossession.PossessedMsg".Translate(letter.subjectPawn.LabelShort), letter.subjectPawn, MessageTypeDefOf.NegativeEvent);
                }
                letter.CompleteAndRemove();
            };
            endure.resolveTree = true;
            options.Add(endure);
        }
    }

    // =========================================================================
    // FACTION 12: TMM_GhoulsFaction (Разумные гули)
    // =========================================================================

    /// <summary>
    /// 12a. Ghoul refugees ask to join the colony.
    /// </summary>
    public sealed class IncidentWorker_TMM_GhoulsRefugees : IncidentWorker
    {
        public const string FactionDefName = "TMM_GhoulsFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction ghouls = FactionEventHelper.FindActiveFaction(FactionDefName);
            return ghouls != null && !ghouls.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction ghouls = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || ghouls == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.GhoulsRefugees.Title".Translate(),
                "TMM.Event.GhoulsRefugees.Text".Translate(ghouls.Name),
                LetterDefOf.PositiveEvent,
                new LookTargets(map.Center, map),
                ghouls,
                FactionEventChoiceKind.GhoulsRefugees,
                map,
                null,
                null,
                null,
                0,
                2,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption accept = new DiaOption("TMM.Event.GhoulsRefugees.Accept".Translate());
            accept.action = delegate
            {
                if (letter.map != null)
                {
                    IntVec3 entry = FactionEventHelper.FindSafeEdgeCell(letter.map);
                    int joined = 0;
                    for (int i = 0; i < 2; i++)
                    {
                        Pawn g = FactionEventHelper.GenerateFactionPawn(Faction.OfPlayer, letter.map, "TMM_Ghoul", PawnKindDefOf.Colonist);
                        if (g != null)
                        {
                            GenSpawn.Spawn(g, entry, letter.map);
                            joined++;
                        }
                    }
                    FactionEventHelper.AdjustGoodwill(letter.faction, 15, letter.map);
                    Messages.Message("TMM.Event.GhoulsRefugees.AcceptMsg".Translate(joined), MessageTypeDefOf.PositiveEvent);
                }
                letter.CompleteAndRemove();
            };
            accept.resolveTree = true;
            options.Add(accept);

            DiaOption refuse = new DiaOption("TMM.Event.GhoulsRefugees.Refuse".Translate());
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
    /// 12b. Ghoul scavengers offer a radioactive bulk lot (Uranium + Chemfuel) for silver.
    /// </summary>
    public sealed class IncidentWorker_TMM_GhoulsRadTrade : IncidentWorker
    {
        public const string FactionDefName = "TMM_GhoulsFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction ghouls = FactionEventHelper.FindActiveFaction(FactionDefName);
            return ghouls != null && !ghouls.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction ghouls = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || ghouls == null) return false;

            int price = 420;
            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.GhoulsRadTrade.Title".Translate(),
                "TMM.Event.GhoulsRadTrade.Text".Translate(ghouls.Name, price),
                LetterDefOf.PositiveEvent,
                new LookTargets(map.Center, map),
                ghouls,
                FactionEventChoiceKind.GhoulsRadTrade,
                map,
                null,
                null,
                null,
                price,
                45,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption buy = new DiaOption("TMM.Event.GhoulsRadTrade.Buy".Translate(letter.silverCost));
            if (FactionEventHelper.CountSilverOnMap(letter.map) < letter.silverCost)
            {
                buy.Disable("TMM.Event.Common.NotEnoughSilver".Translate(letter.silverCost));
            }
            buy.action = delegate
            {
                if (!letter.TryResolvePayment(() => FactionEventHelper.TryTakeSilverFromMap(letter.map, letter.silverCost,
                    () => FactionEventHelper.DeliverPaidSupplies(letter.map,
                        new ResourceRequirement(ThingDefOf.Uranium, 45),
                        new ResourceRequirement(ThingDefOf.Chemfuel, 120)))))
                    return;

                FactionEventHelper.AdjustGoodwill(letter.faction, 10, letter.map);
                Messages.Message("TMM.Event.GhoulsRadTrade.BoughtMsg".Translate(), MessageTypeDefOf.PositiveEvent);
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
    // FACTION 13: TMM_FriendlyMutantsFaction (Дружелюбные супермутанты)
    // =========================================================================

    /// <summary>
    /// 13a. A lone friendly super mutant asks to join the colony.
    /// </summary>
    public sealed class IncidentWorker_TMM_FriendlyMutantJoin : IncidentWorker
    {
        public const string FactionDefName = "TMM_FriendlyMutantsFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction mutants = FactionEventHelper.FindActiveFaction(FactionDefName);
            return mutants != null && !mutants.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction mutants = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || mutants == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.FriendlyMutantJoin.Title".Translate(),
                "TMM.Event.FriendlyMutantJoin.Text".Translate(mutants.Name),
                LetterDefOf.PositiveEvent,
                new LookTargets(map.Center, map),
                mutants,
                FactionEventChoiceKind.FriendlyMutantJoin,
                map,
                null,
                null,
                null,
                0,
                1,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption accept = new DiaOption("TMM.Event.FriendlyMutantJoin.Accept".Translate());
            accept.action = delegate
            {
                if (letter.map != null)
                {
                    Pawn mutant = FactionEventHelper.GenerateFactionPawn(Faction.OfPlayer, letter.map, "TMM_SuperMutant", PawnKindDefOf.Colonist);
                    if (mutant != null)
                    {
                        IntVec3 entry = FactionEventHelper.FindSafeEdgeCell(letter.map);
                        GenSpawn.Spawn(mutant, entry, letter.map);
                        FactionEventHelper.AdjustGoodwill(letter.faction, 12, letter.map);
                        Messages.Message("TMM.Event.FriendlyMutantJoin.AcceptMsg".Translate(mutant.LabelShort), mutant, MessageTypeDefOf.PositiveEvent);
                    }
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
    /// 13b. Friendly super mutants ask the colony to tend an injured kinsman.
    /// </summary>
    public sealed class IncidentWorker_TMM_FriendlyMutantHeal : IncidentWorker
    {
        public const string FactionDefName = "TMM_FriendlyMutantsFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction mutants = FactionEventHelper.FindActiveFaction(FactionDefName);
            return mutants != null && !mutants.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction mutants = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || mutants == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.FriendlyMutantHeal.Title".Translate(),
                "TMM.Event.FriendlyMutantHeal.Text".Translate(mutants.Name),
                LetterDefOf.NeutralEvent,
                new LookTargets(map.Center, map),
                mutants,
                FactionEventChoiceKind.FriendlyMutantHeal,
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
            DiaOption accept = new DiaOption("TMM.Event.FriendlyMutantHeal.Accept".Translate());
            accept.action = delegate
            {
                if (letter.map != null && letter.faction != null)
                {
                    Pawn injured = FactionEventHelper.GenerateFactionPawn(letter.faction, letter.map, "TMM_SuperMutant", null);
                    if (injured != null)
                    {
                        IntVec3 entry = FactionEventHelper.FindSafeEdgeCell(letter.map);
                        GenSpawn.Spawn(injured, entry, letter.map);
                        // Inflict two non-lethal cuts so doctors can tend them.
                        injured.TakeDamage(new DamageInfo(DamageDefOf.Cut, 8f));
                        injured.TakeDamage(new DamageInfo(DamageDefOf.Cut, 8f));
                        LordMaker.MakeNewLord(letter.faction, new LordJob_VisitColony(letter.faction, letter.map.Center), letter.map, new List<Pawn> { injured });

                        TMM_FactionEventsManager mgr = TMM_FactionEventsManager.Instance;
                        if (mgr != null)
                        {
                            mgr.Schedule(new DelayedFactionTask
                            {
                                kind = DelayedFactionTaskKind.WoundedMutantCheck,
                                triggerTick = Find.TickManager.TicksGame + 60000 * 2, // 2 days
                                map = letter.map,
                                faction = letter.faction,
                                pawn = injured
                            });
                        }
                        Messages.Message("TMM.Event.FriendlyMutantHeal.AcceptMsg".Translate(injured.LabelShort), injured, MessageTypeDefOf.NeutralEvent);
                    }
                }
                letter.CompleteAndRemove();
            };
            accept.resolveTree = true;
            options.Add(accept);

            DiaOption refuse = new DiaOption("TMM.Event.FriendlyMutantHeal.Refuse".Translate());
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
    // FACTION 14: TMM_WildMutantsFaction (Дикие супермутанты)
    // =========================================================================

    /// <summary>
    /// 14a. Wild super mutant raid dragging human captives ("meat bags") that can be rescued.
    /// </summary>
    public sealed class IncidentWorker_TMM_WildMutantsMeatBags : IncidentWorker
    {
        public const string FactionDefName = "TMM_WildMutantsFaction";

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
            Faction wild = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || wild == null) return false;

            float pts = parms.points > 250f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            bool raidOk = FactionEventHelper.TriggerEnemyRaid(map, wild, pts, PawnsArrivalModeDefOf.EdgeWalkIn);

            // Spawn 2 downed human captives near the map edge that colonists can rescue.
            IntVec3 edge = FactionEventHelper.FindSafeEdgeCell(map);
            Faction friendlyCapFaction = Find.FactionManager != null
                ? Find.FactionManager.RandomNonHostileFaction(false, false, false, TechLevel.Undefined)
                : null;

            Pawn firstCaptive = null;
            for (int i = 0; i < 2; i++)
            {
                Pawn captive = FactionEventHelper.GenerateFactionPawn(friendlyCapFaction, map, null, PawnKindDefOf.SpaceRefugee);
                if (captive == null) continue;
                GenSpawn.Spawn(captive, edge, map);
                if (captive.health != null)
                {
                    Hediff stun = HediffMaker.MakeHediff(HediffDefOf.Anesthetic, captive); // CHECK API
                    captive.health.AddHediff(stun);
                }
                if (firstCaptive == null) firstCaptive = captive;
            }

            FactionEventHelper.SendSimpleLetter(
                "TMM.Event.WildMutantsMeatBags.Title".Translate(),
                "TMM.Event.WildMutantsMeatBags.Text".Translate(wild.Name),
                LetterDefOf.ThreatBig,
                firstCaptive != null ? new LookTargets(firstCaptive) : new LookTargets(edge, map));
            return raidOk || firstCaptive != null;
        }
    }

    /// <summary>
    /// 14b. Wild super mutant warlord challenges the colony to single combat; refusing brings a full raid.
    /// </summary>
    public sealed class IncidentWorker_TMM_WildMutantsWarlordDuel : IncidentWorker
    {
        public const string FactionDefName = "TMM_WildMutantsFaction";

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
            Faction wild = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || wild == null) return false;

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.WildMutantsWarlordDuel.Title".Translate(),
                "TMM.Event.WildMutantsWarlordDuel.Text".Translate(wild.Name),
                LetterDefOf.ThreatBig,
                new LookTargets(map.Center, map),
                wild,
                FactionEventChoiceKind.WildMutantsWarlordDuel,
                map,
                null,
                null,
                null,
                0,
                500,
                parms.points > 250f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map));
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption accept = new DiaOption("TMM.Event.WildMutantsWarlordDuel.Accept".Translate());
            accept.action = delegate
            {
                if (letter.map != null && letter.faction != null)
                {
                    Pawn champion = FactionEventHelper.GenerateFactionPawn(letter.faction, letter.map, "TMM_SuperMutant", null);
                    if (champion != null)
                    {
                        IntVec3 entry = FactionEventHelper.FindSafeEdgeCell(letter.map);
                        GenSpawn.Spawn(champion, entry, letter.map);
                        // Give the warlord a silver trophy pouch so defeating them pays out directly.
                        Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                        silver.stackCount = letter.rewardAmount > 0 ? letter.rewardAmount : 500;
                        if (champion.inventory != null && champion.inventory.innerContainer != null)
                        {
                            champion.inventory.innerContainer.TryAdd(silver, true);
                        }
                        LordMaker.MakeNewLord(letter.faction, new LordJob_AssaultColony(letter.faction), letter.map, new List<Pawn> { champion });
                        Messages.Message("TMM.Event.WildMutantsWarlordDuel.AcceptMsg".Translate(champion.LabelShort), champion, MessageTypeDefOf.ThreatBig);
                    }
                }
                letter.CompleteAndRemove();
            };
            accept.resolveTree = true;
            options.Add(accept);

            DiaOption refuse = new DiaOption("TMM.Event.WildMutantsWarlordDuel.Refuse".Translate());
            refuse.action = delegate
            {
                FactionEventHelper.TriggerEnemyRaid(
                    letter.map,
                    letter.faction,
                    letter.threatPoints > 250f ? letter.threatPoints : 650f,
                    PawnsArrivalModeDefOf.EdgeWalkIn);
                letter.CompleteAndRemove();
            };
            refuse.resolveTree = true;
            options.Add(refuse);
        }
    }

    // =========================================================================
    // FACTION 15: TMM_MonsterClansFaction (Кланы нечисти)
    // =========================================================================

    /// <summary>
    /// 15a. Vampire night hunt: raid that fires strictly during night hours (21:00..05:00) unless forced.
    /// </summary>
    public sealed class IncidentWorker_TMM_MonsterVampireNightHunt : IncidentWorker
    {
        public const string FactionDefName = "TMM_MonsterClansFaction";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            if (map == null) return false;
            Faction monsters = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (monsters == null) return false;

            if (!parms.forced)
            {
                int hour = GenLocalDate.HourOfDay(map);
                if (hour >= 5 && hour < 21) return false;
            }
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction monsters = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || monsters == null) return false;

            float pts = parms.points > 250f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            bool ok = FactionEventHelper.TriggerEnemyRaid(map, monsters, pts, PawnsArrivalModeDefOf.EdgeWalkIn);
            if (ok)
            {
                FactionEventHelper.SendSimpleLetter(
                    "TMM.Event.MonsterVampireNightHunt.Title".Translate(),
                    "TMM.Event.MonsterVampireNightHunt.Text".Translate(monsters.Name),
                    LetterDefOf.ThreatBig,
                    new LookTargets(map.Center, map));
            }
            return ok;
        }
    }

    /// <summary>
    /// 15b. A disguised werewolf/shapeshifter slips in among visitors: confront immediately or let them stay (transforms later).
    /// </summary>
    public sealed class IncidentWorker_TMM_MonsterShapeshifterGuest : IncidentWorker
    {
        public const string FactionDefName = "TMM_MonsterClansFaction";

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
            Faction monsters = FactionEventHelper.FindActiveFaction(FactionDefName);
            if (map == null || monsters == null) return false;

            Faction neutralGuestFaction = Find.FactionManager != null
                ? Find.FactionManager.RandomNonHostileFaction(false, false, false, TechLevel.Undefined)
                : null;
            Pawn guest = FactionEventHelper.GenerateFactionPawn(neutralGuestFaction != null ? neutralGuestFaction : monsters, map, null, PawnKindDefOf.SpaceRefugee);
            if (guest == null) return false;

            IntVec3 entry = FactionEventHelper.FindSafeEdgeCell(map);
            GenSpawn.Spawn(guest, entry, map);

            if (guest.Faction != null && !guest.Faction.HostileTo(Faction.OfPlayer))
            {
                LordMaker.MakeNewLord(guest.Faction, new LordJob_VisitColony(guest.Faction, map.Center), map, new List<Pawn> { guest });
            }

            FactionEventHelper.SendChoiceLetter(
                "TMM.Event.MonsterShapeshifterGuest.Title".Translate(),
                "TMM.Event.MonsterShapeshifterGuest.Text".Translate(guest.LabelShort),
                LetterDefOf.ThreatSmall,
                new LookTargets(guest),
                monsters,
                FactionEventChoiceKind.MonsterShapeshifterGuest,
                map,
                null,
                guest,
                null,
                0,
                0,
                parms.points);
            return true;
        }

        public static void PopulateChoices(ChoiceLetter_TMMFactionEvent letter, List<DiaOption> options)
        {
            DiaOption confront = new DiaOption("TMM.Event.MonsterShapeshifterGuest.Confront".Translate());
            confront.action = delegate
            {
                if (letter.subjectPawn != null && !letter.subjectPawn.Dead && !letter.subjectPawn.Destroyed && letter.faction != null && letter.map != null)
                {
                    Lord old = letter.subjectPawn.GetLord();
                    if (old != null) old.RemovePawn(letter.subjectPawn);
                    letter.subjectPawn.SetFaction(letter.faction);
                    LordMaker.MakeNewLord(letter.faction, new LordJob_AssaultColony(letter.faction), letter.map, new List<Pawn> { letter.subjectPawn });
                    Messages.Message("TMM.Event.MonsterShapeshifterGuest.ConfrontedMsg".Translate(letter.subjectPawn.LabelShort), letter.subjectPawn, MessageTypeDefOf.ThreatBig);
                }
                letter.CompleteAndRemove();
            };
            confront.resolveTree = true;
            options.Add(confront);

            DiaOption ignore = new DiaOption("TMM.Event.MonsterShapeshifterGuest.Ignore".Translate());
            ignore.action = delegate
            {
                TMM_FactionEventsManager mgr = TMM_FactionEventsManager.Instance;
                if (mgr != null && letter.subjectPawn != null && !letter.subjectPawn.Dead && letter.faction != null)
                {
                    mgr.Schedule(new DelayedFactionTask
                    {
                        kind = DelayedFactionTaskKind.ShapeshifterTransform,
                        triggerTick = Find.TickManager.TicksGame + 30000, // 12 hours later
                        map = letter.map,
                        faction = letter.faction,
                        pawn = letter.subjectPawn
                    });
                }
                letter.CompleteAndRemove();
            };
            ignore.resolveTree = true;
            options.Add(ignore);
        }
    }
}
