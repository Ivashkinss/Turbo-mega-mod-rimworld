using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Promotion ceremony ritual integration for RimWorld Ideology.
    /// Design choice for precept registration:
    ///   The precept (TMM_PromotionCeremony) is automatically added to the player's primary ideology
    ///   on new game / load (when Ideology is active and not in classic mode) via Ideo.AddPrecept
    ///   with the RitualPatternDef, without altering any vanilla memes. If Ideology is inactive,
    ///   classic mode is enabled, or the player manually removes the precept during reform,
    ///   ColonyPromotion gracefully falls back to instant promotion.
    /// </summary>
    public static class ColonyRankCeremony
    {
        public const string PreceptDefName = "TMM_PromotionCeremony";
        public const string PatternDefName = "TMM_PromotionCeremonyPattern";
        public const string HonoreeRoleId = "honoree";
        public const string OfficiantRoleId = "officiant";
        public const string HonoreeThoughtDefName = "TMM_PromotionCeremonyHonoree";
        public const string SpectatorThoughtDefName = "TMM_PromotionCeremonySpectator";
        public const string InspirationHediffDefName = "TMM_PromotionCeremonyInspiration";

        /// <summary>
        /// Ensures the player's primary ideology has the promotion ceremony precept without touching memes.
        /// Safe to call repeatedly (idempotent) and safe when Ideology DLC is disabled.
        /// </summary>
        public static bool EnsurePlayerIdeoPrecept(Ideo targetIdeo)
        {
            if (!ModsConfig.IdeologyActive) return false;
            if (Find.IdeoManager == null || Find.IdeoManager.classicMode) return false;

            Ideo ideo = targetIdeo;
            if (ideo == null && Faction.OfPlayer != null && Faction.OfPlayer.ideos != null)
            {
                ideo = Faction.OfPlayer.ideos.PrimaryIdeo;
            }
            if (ideo == null) return false;

            PreceptDef preceptDef = DefDatabase<PreceptDef>.GetNamedSilentFail(PreceptDefName);
            RitualPatternDef patternDef = DefDatabase<RitualPatternDef>.GetNamedSilentFail(PatternDefName);
            if (preceptDef == null || patternDef == null) return false;

            if (FindPreceptInIdeo(ideo, preceptDef) != null) return true;

            try
            {
                Precept precept = PreceptMaker.MakePrecept(preceptDef); // CHECK API
                Precept_Ritual ritual = precept as Precept_Ritual;
                if (ritual == null) return false;

                ideo.AddPrecept(ritual, true, null, patternDef); // CHECK API
                return FindPreceptInIdeo(ideo, preceptDef) != null;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] ColonyRankCeremony: failed to add promotion ceremony precept to player ideo: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Locates the promotion ceremony Precept_Ritual for the given pawn's ideology (or player's primary ideology).
        /// Returns null if Ideology is inactive, classic mode is active, or the precept is absent.
        /// </summary>
        public static Precept_Ritual FindPromotionRitual(Pawn pawn)
        {
            if (!ModsConfig.IdeologyActive) return null;
            if (Find.IdeoManager == null || Find.IdeoManager.classicMode) return null;

            PreceptDef preceptDef = DefDatabase<PreceptDef>.GetNamedSilentFail(PreceptDefName);
            if (preceptDef == null) return null;

            if (pawn != null && pawn.Ideo != null)
            {
                Precept_Ritual own = FindPreceptInIdeo(pawn.Ideo, preceptDef);
                if (own != null) return own;
            }

            if (Faction.OfPlayer != null && Faction.OfPlayer.ideos != null && Faction.OfPlayer.ideos.PrimaryIdeo != null)
            {
                return FindPreceptInIdeo(Faction.OfPlayer.ideos.PrimaryIdeo, preceptDef);
            }

            return null;
        }

        private static Precept_Ritual FindPreceptInIdeo(Ideo ideo, PreceptDef preceptDef)
        {
            if (ideo == null || preceptDef == null) return null;
            List<Precept> list = ideo.PreceptsListForReading;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                Precept_Ritual ritual = list[i] as Precept_Ritual;
                if (ritual != null && ritual.def == preceptDef) return ritual;
            }
            return null;
        }

        /// <summary>
        /// Returns true when Ideology is active and the pawn's/colony's ideology owns the promotion ceremony precept.
        /// </summary>
        public static bool ShouldUseCeremony(Pawn pawn)
        {
            return FindPromotionRitual(pawn) != null;
        }

        /// <summary>
        /// Returns true if the pawn holds a squad commander role (military rank with commander aura or explicit Commander role).
        /// </summary>
        public static bool IsSquadCommander(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.IsFreeColonist) return false;
            ColonyRoles roles = ColonyRoles.Instance;
            if (roles == null) return false;
            ColonyRoleAssignment assignment = roles.For(pawn);
            if (assignment == null || assignment.role == null) return false;

            if (string.Equals(assignment.role.defName, "TMM_Role_commander", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return assignment.role.Military && assignment.Rank != null && assignment.Rank.aura != null;
        }

        /// <summary>
        /// Returns true if the pawn is the Ideology leader (IdeoRole_Leader).
        /// </summary>
        public static bool IsIdeoLeader(Pawn pawn)
        {
            if (!ModsConfig.IdeologyActive || pawn == null || pawn.Dead || pawn.Downed || pawn.Ideo == null) return false;
            Precept_Role role = pawn.Ideo.GetRole(pawn); // CHECK API
            return role != null && role.def == PreceptDefOf.IdeoRole_Leader;
        }

        /// <summary>
        /// Basic physical/social eligibility to speak and lead a ceremony.
        /// </summary>
        public static bool CanSpeakAtCeremony(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.IsFreeColonist) return false;
            if (pawn.WorkTagIsDisabled(WorkTags.Social)) return false;
            if (pawn.health == null || pawn.health.capacities == null) return false;
            return pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking);
        }

        /// <summary>
        /// True when the pawn is a preferred officiant (squad commander or ideology leader) capable of speaking.
        /// </summary>
        public static bool IsPreferredOfficiant(Pawn pawn)
        {
            if (!CanSpeakAtCeremony(pawn)) return false;
            return IsSquadCommander(pawn) || IsIdeoLeader(pawn);
        }

        /// <summary>
        /// Finds the best officiant on the honoree's map: highest-ranked squad commander first,
        /// then ideology leader, then highest-Social speaking colonist as a non-blocking fallback.
        /// </summary>
        public static Pawn FindBestOfficiant(Map map, Pawn excludeHonoree)
        {
            if (map == null || map.mapPawns == null) return null;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0) return null;

            Pawn bestCommander = null;
            int bestCommanderRank = -1;
            Pawn bestLeader = null;
            Pawn bestFallback = null;
            int bestFallbackSocial = -1;

            ColonyRoles roles = ColonyRoles.Instance;

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn candidate = colonists[i];
                if (candidate == null || candidate == excludeHonoree) continue;
                if (!CanSpeakAtCeremony(candidate)) continue;

                if (IsSquadCommander(candidate))
                {
                    int rankVal = 0;
                    if (roles != null)
                    {
                        ColonyRoleAssignment a = roles.For(candidate);
                        if (a != null) rankVal = a.rank;
                    }
                    if (bestCommander == null || rankVal > bestCommanderRank)
                    {
                        bestCommander = candidate;
                        bestCommanderRank = rankVal;
                    }
                    continue;
                }

                if (IsIdeoLeader(candidate))
                {
                    if (bestLeader == null) bestLeader = candidate;
                    continue;
                }

                int social = candidate.skills != null && candidate.skills.GetSkill(SkillDefOf.Social) != null
                    ? candidate.skills.GetSkill(SkillDefOf.Social).Level
                    : 0;
                if (bestFallback == null || social > bestFallbackSocial)
                {
                    bestFallback = candidate;
                    bestFallbackSocial = social;
                }
            }

            if (bestCommander != null) return bestCommander;
            if (bestLeader != null) return bestLeader;
            return bestFallback;
        }

        /// <summary>
        /// Checks whether any preferred officiant (commander or ideo leader) exists on the map besides the honoree.
        /// </summary>
        public static bool HasOtherPreferredOfficiant(Map map, Pawn excludeHonoree)
        {
            if (map == null || map.mapPawns == null) return false;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null) return false;

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p == null || p == excludeHonoree) continue;
                if (IsPreferredOfficiant(p)) return true;
            }
            return false;
        }

        /// <summary>
        /// Picks a suitable gathering spot on the pawn's map: ritual spot, altar/ideogram, active gather spot,
        /// or the pawn's own tile as final fallback.
        /// </summary>
        public static TargetInfo FindCeremonyTarget(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null) return TargetInfo.Invalid;
            Map map = pawn.Map;

            // 1. Prefer a placed RitualSpot or Ideo altar/ideogram
            if (map.listerBuildings != null)
            {
                List<Building> buildings = map.listerBuildings.allBuildingsColonist;
                Building fallbackIdeogram = null;
                for (int i = 0; i < buildings.Count; i++)
                {
                    Building b = buildings[i];
                    if (b == null || !b.Spawned || b.Destroyed) continue;
                    if (ThingDefOf.RitualSpot != null && b.def == ThingDefOf.RitualSpot)
                    {
                        return new TargetInfo(b);
                    }
                    if (b.def != null && b.def.isAltar)
                    {
                        fallbackIdeogram = b;
                    }
                }
                if (fallbackIdeogram != null) return new TargetInfo(fallbackIdeogram);
            }

            // 2. Any active gather spot (table, campfire, party spot)
            if (map.gatherSpotLister != null && map.gatherSpotLister.activeSpots != null)
            {
                for (int i = 0; i < map.gatherSpotLister.activeSpots.Count; i++)
                {
                    CompGatherSpot spot = map.gatherSpotLister.activeSpots[i];
                    if (spot != null && spot.parent != null && spot.parent.Spawned)
                    {
                        return new TargetInfo(spot.parent);
                    }
                }
            }

            // 3. Fallback to the honoree's current position
            return new TargetInfo(pawn.Position, map);
        }

        /// <summary>
        /// Opens the vanilla Dialog_BeginRitual for promoting the given colonist.
        /// Returns true if the dialog was opened successfully.
        /// </summary>
        public static bool TryOpenBeginRitualDialog(Pawn honoree)
        {
            if (honoree == null || honoree.Map == null) return false;
            Precept_Ritual ritual = FindPromotionRitual(honoree);
            if (ritual == null) return false;

            TargetInfo target = FindCeremonyTarget(honoree);
            if (!target.IsValid) return false;

            Dictionary<string, Pawn> forcedRoles = new Dictionary<string, Pawn>();
            forcedRoles[HonoreeRoleId] = honoree;

            // Note: We do not force the officiant so the player can adjust it in Dialog_BeginRitual;
            // RitualRole_PromotionOfficiant naturally prioritizes Commanders and Ideo Leaders.
            try
            {
                ritual.ShowRitualBeginWindow(target, null, honoree, forcedRoles); // CHECK API
                return Find.WindowStack != null && Find.WindowStack.IsOpen<Dialog_BeginRitual>();
            }
            catch (Exception ex)
            {
                Log.Error("[TMM] ColonyRankCeremony: failed to open Dialog_BeginRitual: " + ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Ritual role for the colonist being promoted. Only accepts free colonists who meet next-rank requirements.
    /// </summary>
    public sealed class RitualRole_PromotionHonoree : RitualRole
    {
        public override bool AppliesToPawn(
            Pawn p,
            out string reason,
            TargetInfo selectedTarget,
            LordJob_Ritual ritual = null,
            RitualRoleAssignments assignments = null,
            Precept_Ritual precept = null,
            bool skipReason = false) // CHECK API
        {
            reason = null;
            if (p == null || !p.IsFreeColonist || p.Dead || p.Downed)
            {
                if (!skipReason) reason = "TMM.Roles.Ceremony.MustBeFreeColonist".Translate();
                return false;
            }

            ColonyRoles roles = ColonyRoles.Instance;
            ColonyRoleAssignment assignment = roles == null ? null : roles.For(p);
            if (assignment == null || !ColonyPromotion.Ready(p, assignment))
            {
                if (!skipReason) reason = "TMM.Roles.Ceremony.NotReadyForPromotion".Translate();
                return false;
            }

            return true;
        }

        public override bool AppliesToRole(
            Precept_Role role,
            out string reason,
            Precept_Ritual ritual = null,
            Pawn p = null,
            bool skipReason = false) // CHECK API
        {
            reason = null;
            return false;
        }
    }

    /// <summary>
    /// Ritual role for the officiant leading the promotion ceremony:
    /// prefers a squad Commander (military rank with aura / Commander role) or the Ideology leader.
    /// Falls back to any capable talking colonist only when no Commander or Ideology leader is available on the map.
    /// </summary>
    public sealed class RitualRole_PromotionOfficiant : RitualRole
    {
        public override bool AppliesToPawn(
            Pawn p,
            out string reason,
            TargetInfo selectedTarget,
            LordJob_Ritual ritual = null,
            RitualRoleAssignments assignments = null,
            Precept_Ritual precept = null,
            bool skipReason = false) // CHECK API
        {
            reason = null;
            if (!ColonyRankCeremony.CanSpeakAtCeremony(p))
            {
                if (!skipReason) reason = "TMM.Roles.Ceremony.CannotSpeak".Translate();
                return false;
            }

            if (ColonyRankCeremony.IsPreferredOfficiant(p))
            {
                return true;
            }

            // Determine current honoree (if assigned) so we don't deadlock when the honoree is the only Commander/Leader.
            Pawn honoree = null;
            if (assignments != null)
            {
                honoree = assignments.FirstAssignedPawn(ColonyRankCeremony.HonoreeRoleId); // CHECK API
            }
            else if (ritual != null)
            {
                honoree = ritual.PawnWithRole(ColonyRankCeremony.HonoreeRoleId); // CHECK API
            }

            Map map = selectedTarget.Map != null ? selectedTarget.Map : p.Map;
            if (ColonyRankCeremony.HasOtherPreferredOfficiant(map, honoree))
            {
                if (!skipReason) reason = "TMM.Roles.Ceremony.MustBeCommanderOrLeader".Translate();
                return false;
            }

            // No other Commander or Ideology leader on the map: allow any speaking colonist as fallback.
            return true;
        }

        public override bool AppliesToRole(
            Precept_Role role,
            out string reason,
            Precept_Ritual ritual = null,
            Pawn p = null,
            bool skipReason = false) // CHECK API
        {
            reason = null;
            return role != null && role.def == PreceptDefOf.IdeoRole_Leader;
        }
    }

    /// <summary>
    /// Ritual behavior worker for the promotion ceremony. Validates that a colonist is ready for promotion.
    /// </summary>
    public sealed class RitualBehaviorWorker_PromotionCeremony : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_PromotionCeremony() { }

        public RitualBehaviorWorker_PromotionCeremony(RitualBehaviorDef def) : base(def) { }

        public override string CanStartRitualNow(
            TargetInfo target,
            Precept_Ritual ritual,
            Pawn selectedPawn = null,
            Dictionary<string, Pawn> forcedForRole = null) // CHECK API
        {
            string baseReason = base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
            if (baseReason != null) return baseReason;

            Map map = target.Map;
            if (map == null) return "TMM.Roles.Ceremony.NoEligibleHonoree".Translate();

            Pawn forcedHonoree = null;
            if (forcedForRole != null)
            {
                forcedForRole.TryGetValue(ColonyRankCeremony.HonoreeRoleId, out forcedHonoree);
            }
            if (forcedHonoree == null) forcedHonoree = selectedPawn;

            ColonyRoles roles = ColonyRoles.Instance;
            if (roles == null) return "TMM.Roles.Ceremony.NoEligibleHonoree".Translate();

            if (forcedHonoree != null)
            {
                ColonyRoleAssignment a = roles.For(forcedHonoree);
                if (a != null && ColonyPromotion.Ready(forcedHonoree, a))
                {
                    return null;
                }
                return "TMM.Roles.Ceremony.NotReadyForPromotion".Translate();
            }

            // Check if any colonist on the map is ready for promotion.
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn c = colonists[i];
                if (c == null || c.Dead || c.Downed) continue;
                ColonyRoleAssignment a = roles.For(c);
                if (a != null && ColonyPromotion.Ready(c, a))
                {
                    return null;
                }
            }

            return "TMM.Roles.Ceremony.NoEligibleHonoree".Translate();
        }
    }

    /// <summary>
    /// Outcome effect worker for the promotion ceremony:
    ///   - Executes the actual promotion via ColonyPromotion.Promote (no duplicate promotion logic).
    ///   - Grants quality-staged mood memories to the promoted colonist and spectators.
    ///   - On the best/excellent outcome, grants a 5-day temporary inspiration Hediff to the honoree.
    /// </summary>
    public sealed class RitualOutcomeEffectWorker_PromotionCeremony : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_PromotionCeremony() { }

        public RitualOutcomeEffectWorker_PromotionCeremony(RitualOutcomeEffectDef def) : base(def) { }

        public override bool SupportsAttachableOutcomeEffect { get { return false; } }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual) // CHECK API
        {
            float quality = GetQuality(jobRitual, progress);
            RitualOutcomePossibility outcome = GetOutcome(quality, jobRitual); // CHECK API
            ApplyResolvedOutcome(progress, quality, outcome, totalPresence, jobRitual);
        }

        /// <summary>
        /// Deterministic entry point shared by normal ritual completion and automated verification probes.
        /// </summary>
        public void ApplyResolvedOutcome(
            float progress,
            float quality,
            RitualOutcomePossibility outcome,
            Dictionary<Pawn, int> totalPresence,
            LordJob_Ritual jobRitual)
        {
            if (outcome == null && def != null && !def.outcomeChances.NullOrEmpty())
            {
                outcome = def.outcomeChances[def.outcomeChances.Count - 1];
            }

            Pawn honoree = ResolveHonoree(totalPresence, jobRitual);
            Pawn officiant = ResolveOfficiant(totalPresence, jobRitual, honoree);

            int stageIndex = ResolveStageIndex(outcome);
            bool bestOutcome = outcome != null && ((jobRitual != null && jobRitual.Ritual != null && outcome.BestPositiveOutcome(jobRitual)) || outcome.positivityIndex >= 2); // CHECK API

            // 1. Perform the actual promotion through ColonyPromotion (single source of truth).
            bool promoted = false;
            string promotedRankLabel = string.Empty;
            string promotedRoleLabel = string.Empty;
            if (honoree != null && ColonyRoles.Instance != null)
            {
                ColonyRoleAssignment assignment = ColonyRoles.Instance.For(honoree);
                if (assignment != null && ColonyPromotion.Ready(honoree, assignment))
                {
                    ColonyPromotion.Promote(honoree, assignment, true);
                    promoted = true;
                    promotedRankLabel = assignment.Rank != null ? assignment.Rank.label : string.Empty;
                    promotedRoleLabel = assignment.role != null ? assignment.role.label : string.Empty;
                }
            }

            // 2. Grant quality-based thoughts to honoree and spectators.
            ThoughtDef honoreeThoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(ColonyRankCeremony.HonoreeThoughtDefName);
            ThoughtDef spectatorThoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(ColonyRankCeremony.SpectatorThoughtDefName);

            if (honoree != null)
            {
                TryGiveStagedMemory(honoree, honoreeThoughtDef, stageIndex, officiant);
            }

            if (totalPresence != null)
            {
                foreach (KeyValuePair<Pawn, int> kv in totalPresence)
                {
                    Pawn participant = kv.Key;
                    if (participant == null || participant == honoree || participant.Dead) continue;
                    TryGiveStagedMemory(participant, spectatorThoughtDef, stageIndex, honoree);
                }
            }
            else if (officiant != null && officiant != honoree)
            {
                TryGiveStagedMemory(officiant, spectatorThoughtDef, stageIndex, honoree);
            }

            // 3. On excellent quality outcome, grant 5-day temporary inspiration Hediff to the honoree.
            bool grantedInspiration = false;
            if (bestOutcome && honoree != null && !honoree.Dead && honoree.health != null)
            {
                HediffDef inspDef = DefDatabase<HediffDef>.GetNamedSilentFail(ColonyRankCeremony.InspirationHediffDefName);
                if (inspDef != null)
                {
                    Hediff existing = honoree.health.hediffSet.GetFirstHediffOfDef(inspDef);
                    if (existing != null) honoree.health.RemoveHediff(existing);
                    Hediff added = HediffMaker.MakeHediff(inspDef, honoree);
                    honoree.health.AddHediff(added);
                    grantedInspiration = true;
                }
            }

            // 4. Send outcome letter with quality breakdown and promotion result.
            if (Find.LetterStack != null)
            {
                string ritualLabel = jobRitual != null && jobRitual.Ritual != null
                    ? jobRitual.Ritual.LabelCap
                    : "TMM.Roles.Ceremony.DefaultTitle".Translate().ToString();

                string outcomeLabel = outcome != null ? outcome.label : string.Empty;
                string letterTitle = ritualLabel + ": " + outcomeLabel;

                string body = outcome != null && !string.IsNullOrEmpty(outcome.description)
                    ? outcome.description
                    : string.Empty;

                if (promoted && honoree != null)
                {
                    if (body.Length > 0) body += "\n\n";
                    body += "TMM.Roles.PromotedText".Translate(honoree.LabelShort, promotedRankLabel, promotedRoleLabel);
                }

                if (grantedInspiration && honoree != null)
                {
                    body += "\n\n" + "TMM.Roles.Ceremony.InspirationGranted".Translate(honoree.LabelShort);
                }

                if (jobRitual != null)
                {
                    string breakdown = OutcomeQualityBreakdownDesc(quality, progress, jobRitual); // CHECK API
                    if (!string.IsNullOrEmpty(breakdown))
                    {
                        body += "\n\n" + breakdown;
                    }
                }

                LetterDef letterDef = outcome != null && outcome.Positive
                    ? LetterDefOf.RitualOutcomePositive
                    : LetterDefOf.RitualOutcomeNegative;
                LookTargets targets = honoree != null
                    ? new LookTargets(honoree)
                    : (jobRitual != null ? new LookTargets(jobRitual.selectedTarget) : LookTargets.Invalid);

                Find.LetterStack.ReceiveLetter(letterTitle, body, letterDef, targets);
            }
        }

        private static Pawn ResolveHonoree(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            if (jobRitual != null)
            {
                Pawn assigned = jobRitual.PawnWithRole(ColonyRankCeremony.HonoreeRoleId); // CHECK API
                if (assigned != null) return assigned;
            }

            if (totalPresence != null && ColonyRoles.Instance != null)
            {
                foreach (KeyValuePair<Pawn, int> kv in totalPresence)
                {
                    Pawn p = kv.Key;
                    if (p == null || p.Dead) continue;
                    ColonyRoleAssignment a = ColonyRoles.Instance.For(p);
                    if (a != null && ColonyPromotion.Ready(p, a)) return p;
                }
            }

            return null;
        }

        private static Pawn ResolveOfficiant(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual, Pawn honoree)
        {
            if (jobRitual != null)
            {
                Pawn assigned = jobRitual.PawnWithRole(ColonyRankCeremony.OfficiantRoleId); // CHECK API
                if (assigned != null) return assigned;
            }

            if (totalPresence != null)
            {
                foreach (KeyValuePair<Pawn, int> kv in totalPresence)
                {
                    Pawn p = kv.Key;
                    if (p != null && p != honoree && !p.Dead && ColonyRankCeremony.IsPreferredOfficiant(p))
                        return p;
                }
            }

            return null;
        }

        private int ResolveStageIndex(RitualOutcomePossibility outcome)
        {
            if (outcome == null) return 2;
            if (def != null && !def.outcomeChances.NullOrEmpty())
            {
                int idx = def.outcomeChances.IndexOf(outcome);
                if (idx >= 0) return idx;
            }
            if (outcome.positivityIndex <= -2) return 0;
            if (outcome.positivityIndex == -1) return 1;
            if (outcome.positivityIndex == 1) return 2;
            return 3;
        }

        private static void TryGiveStagedMemory(Pawn pawn, ThoughtDef thoughtDef, int stageIndex, Pawn otherPawn)
        {
            if (pawn == null || thoughtDef == null || pawn.Dead) return;
            if (pawn.needs == null || pawn.needs.mood == null || pawn.needs.mood.thoughts == null) return;
            if (thoughtDef.stages == null || thoughtDef.stages.Count == 0) return;

            int clamped = stageIndex;
            if (clamped < 0) clamped = 0;
            if (clamped >= thoughtDef.stages.Count) clamped = thoughtDef.stages.Count - 1;

            Thought_Memory memory = ThoughtMaker.MakeThought(thoughtDef, clamped); // CHECK API
            if (memory != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(memory, otherPawn);
            }
        }
    }
}