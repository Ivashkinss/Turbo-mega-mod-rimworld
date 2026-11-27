using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class InGameColonyRanksProbe
    {
        private const string ProbeFlag = "-tmm-colony-ranks-probe";
        private const string ReportDirArgPrefix = "-tmm-probe-dir=";
        private const string MainReportFileName = "ColonyRanksProbe.json";
        private const string UiReportFileName = "ColonyRanksUiProbe.json";

        private static readonly bool Enabled;
        private static readonly string CustomReportDir;
        private static readonly object LogLock = new object();
        private static readonly List<string> CapturedErrors = new List<string>();

        private static bool started;
        private static bool completed;
        private static int readyFrames;
        private static bool capturingLogs;

        static InGameColonyRanksProbe()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (string.IsNullOrEmpty(arg)) continue;
                    if (string.Equals(arg, ProbeFlag, StringComparison.OrdinalIgnoreCase))
                    {
                        Enabled = true;
                    }
                    else if (arg.StartsWith(ReportDirArgPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomReportDir = arg.Substring(ReportDirArgPrefix.Length).Trim('"');
                    }
                }
            }

            if (!Enabled) return;

            var harmony = new Harmony("colonyproject.integrated.colony.ranks.probe");
            var updatePlay = AccessTools.Method(typeof(Game), "UpdatePlay"); // CHECK API
            if (updatePlay != null)
            {
                harmony.Patch(updatePlay, null, new HarmonyMethod(typeof(InGameColonyRanksProbe), "Game_UpdatePlay_Postfix"));
            }
            else
            {
                Log.Warning("[Turbo mega mod] InGameColonyRanksProbe could not find Game.UpdatePlay.");
            }
        }

        private static void Game_UpdatePlay_Postfix()
        {
            if (!Enabled || started || completed) return;
            if (Current.ProgramState != ProgramState.Playing) return;
            if (Find.CurrentMap == null || Find.TickManager == null || Current.Game == null) return;
            if (LongEventHandler.AnyEventNowOrWaiting) return;

            readyFrames++;
            if (readyFrames < 30) return;

            started = true;
            RunProbe();
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!capturingLogs) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                lock (LogLock)
                {
                    CapturedErrors.Add(type.ToString() + ": " + (condition ?? string.Empty));
                }
            }
        }

        private static void RunProbe()
        {
            ProbeReport report = new ProbeReport();
            UiProbeReport uiReport = new UiProbeReport();

            lock (LogLock)
            {
                CapturedErrors.Clear();
            }

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            capturingLogs = true;

            try
            {
                ExecuteProbeChecks(report, uiReport);
            }
            catch (Exception ex)
            {
                report.fatalException = ex.ToString();
                uiReport.fatalException = ex.Message;
                Log.Error("[Turbo mega mod] ColonyRanksProbe exception: " + ex);
            }
            finally
            {
                capturingLogs = false;
                Application.logMessageReceivedThreaded -= OnLogMessageReceived;

                try
                {
                    if (Find.WindowStack != null)
                    {
                        Find.WindowStack.TryRemove(typeof(Dialog_BeginRitual), false);
                        Find.WindowStack.TryRemove(typeof(FloatMenu), false);
                    }
                }
                catch
                {
                    // Cleanup must not mask probe results.
                }

                lock (LogLock)
                {
                    for (int i = 0; i < CapturedErrors.Count; i++)
                    {
                        report.errors.Add(CapturedErrors[i]);
                    }
                }

                report.errorCount = report.errors.Count;
                report.passed = EvaluateMainPassed(report);
                uiReport.errorCount = report.errorCount;
                uiReport.passed = EvaluateUiPassed(uiReport, report.errorCount);

                WriteReports(report, uiReport);
                completed = true;

                Log.Message("[Turbo mega mod] ColonyRanksProbe finished. passed="
                    + report.passed + ", uiPassed=" + uiReport.passed
                    + ", errors=" + report.errorCount);
            }
        }

        private static void ExecuteProbeChecks(ProbeReport report, UiProbeReport uiReport)
        {
            Map map = Find.CurrentMap;
            ColonyRoles roles = ColonyRoles.Instance;
            if (map == null || roles == null || Current.Game == null || Current.Game.outfitDatabase == null)
            {
                report.notes.Add("Map, ColonyRoles, or OutfitDatabase is missing.");
                return;
            }

            ColonyRoleDef trooperRole = DefDatabase<ColonyRoleDef>.GetNamedSilentFail("TMM_Role_trooper");
            if (trooperRole == null || trooperRole.ranks == null || trooperRole.ranks.Count < 3)
            {
                report.notes.Add("TMM_Role_trooper def missing or has fewer than 3 ranks.");
                return;
            }

            ColonyRankUniforms.SyncAllRankPolicies();
            report.ideologyActive = ModsConfig.IdeologyActive;
            report.classicIdeoMode = ModsConfig.IdeologyActive && Find.IdeoManager != null && Find.IdeoManager.classicMode;

            // Ensure we have at least 3 healthy colonists on the colony map.
            List<Pawn> colonists = EnsureTestColonists(map, 3);
            if (colonists.Count < 3)
            {
                report.notes.Add("Could not prepare 3 free colonists on the current map.");
                return;
            }

            Pawn instantPawn = colonists[0];
            Pawn ceremonyPawn = colonists[1];
            Pawn customPolicyPawn = colonists[2];

            // =========================================================================
            // TEST 1: Promotion WITHOUT Ideology ritual (button -> instant promotion + uniform policy switch)
            // =========================================================================
            bool removedPreceptTemporarily = false;
            Ideo primaryIdeo = Faction.OfPlayer != null && Faction.OfPlayer.ideos != null
                ? Faction.OfPlayer.ideos.PrimaryIdeo
                : null;

            if (report.ideologyActive && !report.classicIdeoMode)
            {
                removedPreceptTemporarily = RemoveCeremonyPreceptFromIdeo(instantPawn.Ideo);
                if (primaryIdeo != null && primaryIdeo != instantPawn.Ideo)
                {
                    if (RemoveCeremonyPreceptFromIdeo(primaryIdeo)) removedPreceptTemporarily = true;
                }
            }

            ApparelPolicy defaultPolicy = Current.Game.outfitDatabase.DefaultOutfit(); // CHECK API
            if (instantPawn.outfits != null && defaultPolicy != null)
            {
                instantPawn.outfits.CurrentApparelPolicy = defaultPolicy; // CHECK API
            }

            roles.Assign(instantPawn, trooperRole);
            ColonyRoleAssignment instantAssignment = roles.For(instantPawn);
            ColonyRank rank0 = trooperRole.ranks[0];
            ColonyRank rank1 = trooperRole.ranks[1];

            report.initialPolicyAppliedOnAssign = instantPawn.outfits != null
                && ColonyRankUniforms.IsPolicyForRank(instantPawn.outfits.CurrentApparelPolicy, trooperRole, rank0);

            SatisfyRankRequirements(instantPawn, trooperRole, rank1);

            Gizmo instantGizmoRaw = ColonyPromotion.PromoteGizmo(instantPawn, instantAssignment);
            Command_Action instantGizmo = instantGizmoRaw as Command_Action;

            uiReport.instantGizmoFound = instantGizmo != null;
            uiReport.instantGizmoEnabled = instantGizmo != null && !instantGizmo.Disabled;
            uiReport.instantGizmoLabel = instantGizmo != null ? instantGizmo.defaultLabel : string.Empty;
            uiReport.instantGizmoDesc = instantGizmo != null ? instantGizmo.defaultDesc : string.Empty;

            if (instantGizmo != null && !instantGizmo.Disabled && instantGizmo.action != null)
            {
                instantGizmo.action();
            }

            report.instantRankPromoted = instantAssignment != null && instantAssignment.rank == 1;
            report.instantPolicySwitched = instantPawn.outfits != null
                && ColonyRankUniforms.IsPolicyForRank(instantPawn.outfits.CurrentApparelPolicy, trooperRole, rank1);

            if (!report.instantRankPromoted || !report.instantPolicySwitched)
            {
                report.notes.Add("Instant promotion check failed: rank="
                    + (instantAssignment != null ? instantAssignment.rank.ToString(CultureInfo.InvariantCulture) : "null")
                    + ", policy="
                    + (instantPawn.outfits != null && instantPawn.outfits.CurrentApparelPolicy != null
                        ? instantPawn.outfits.CurrentApparelPolicy.label
                        : "null"));
            }

            // =========================================================================
            // TEST 2: Custom player ApparelPolicy is NEVER overwritten by promotion
            // =========================================================================
            ApparelPolicy customPolicy = Current.Game.outfitDatabase.MakeNewOutfit(); // CHECK API
            if (customPolicy != null)
            {
                customPolicy.label = "TMM_Probe_CustomManualPolicy";
            }

            roles.Assign(customPolicyPawn, trooperRole);
            ColonyRoleAssignment customAssignment = roles.For(customPolicyPawn);
            if (customPolicyPawn.outfits != null && customPolicy != null)
            {
                customPolicyPawn.outfits.CurrentApparelPolicy = customPolicy; // CHECK API
            }

            SatisfyRankRequirements(customPolicyPawn, trooperRole, rank1);
            ColonyPromotion.Promote(customPolicyPawn, customAssignment, false);

            report.customPolicyPawnPromoted = customAssignment != null && customAssignment.rank == 1;
            report.customPolicyPreserved = customPolicy != null
                && customPolicyPawn.outfits != null
                && customPolicyPawn.outfits.CurrentApparelPolicy == customPolicy;

            if (!report.customPolicyPreserved)
            {
                report.notes.Add("Custom player ApparelPolicy was overwritten during promotion.");
            }

            // =========================================================================
            // TEST 3: Rank weapon WorkGiver (armory equip + no infinite re-equip loop)
            // =========================================================================
            RunRankWeaponCheck(map, instantPawn, rank1, report);

            // =========================================================================
            // TEST 4: Ideology promotion ceremony (when Ideology DLC is active and not classic mode)
            // =========================================================================
            if (report.ideologyActive && !report.classicIdeoMode)
            {
                RunIdeologyCeremonyCheck(map, roles, trooperRole, ceremonyPawn, instantPawn, customPolicyPawn, report, uiReport);
            }
            else
            {
                report.ceremonySkippedNoIdeology = true;
                uiReport.ceremonySkippedNoIdeology = true;
                report.notes.Add("Ideology ceremony checks skipped (Ideology inactive or classic mode).");
            }

            // Restore precept if it was removed and not yet restored.
            if (removedPreceptTemporarily && report.ideologyActive && !report.classicIdeoMode)
            {
                ColonyRankCeremony.EnsurePlayerIdeoPrecept(primaryIdeo);
            }

            // Verify localization keys resolved cleanly.
            string ceremonyBtnText = "TMM.Roles.PromoteCeremonyButton".Translate(rank1.label).RawText;
            string ceremonyHintText = "TMM.Roles.Ceremony.GizmoHint".Translate().RawText;
            uiReport.localizationResolved =
                !string.Equals(ceremonyBtnText, "TMM.Roles.PromoteCeremonyButton", StringComparison.Ordinal)
                && !string.Equals(ceremonyHintText, "TMM.Roles.Ceremony.GizmoHint", StringComparison.Ordinal);
        }

        private static void RunIdeologyCeremonyCheck(
            Map map,
            ColonyRoles roles,
            ColonyRoleDef trooperRole,
            Pawn honoree,
            Pawn commanderOfficiant,
            Pawn spectator,
            ProbeReport report,
            UiProbeReport uiReport)
        {
            Ideo targetIdeo = honoree.Ideo;
            if (targetIdeo == null && Faction.OfPlayer != null && Faction.OfPlayer.ideos != null)
            {
                targetIdeo = Faction.OfPlayer.ideos.PrimaryIdeo;
            }

            bool preceptReady = ColonyRankCeremony.EnsurePlayerIdeoPrecept(targetIdeo);
            if (Faction.OfPlayer != null && Faction.OfPlayer.ideos != null && Faction.OfPlayer.ideos.PrimaryIdeo != null)
            {
                ColonyRankCeremony.EnsurePlayerIdeoPrecept(Faction.OfPlayer.ideos.PrimaryIdeo);
            }

            Precept_Ritual ritual = ColonyRankCeremony.FindPromotionRitual(honoree);
            report.ceremonyPreceptCreated = preceptReady && ritual != null;
            if (ritual == null)
            {
                report.notes.Add("Failed to register or find TMM_PromotionCeremony Precept_Ritual.");
                return;
            }

            // Promote commanderOfficiant to rank 2 (lieutenant, has commander aura) so they qualify as Squad Commander.
            ColonyRoleAssignment cmdAssignment = roles.For(commanderOfficiant);
            if (cmdAssignment == null)
            {
                roles.Assign(commanderOfficiant, trooperRole);
                cmdAssignment = roles.For(commanderOfficiant);
            }
            if (cmdAssignment != null)
            {
                cmdAssignment.rank = 2;
            }
            report.officiantRecognizedAsCommander = ColonyRankCeremony.IsSquadCommander(commanderOfficiant);

            // Prepare honoree at rank 0 ready for rank 1 with default apparel policy.
            ApparelPolicy defaultPolicy = Current.Game.outfitDatabase.DefaultOutfit(); // CHECK API
            if (honoree.outfits != null && defaultPolicy != null)
            {
                honoree.outfits.CurrentApparelPolicy = defaultPolicy; // CHECK API
            }

            roles.Assign(honoree, trooperRole);
            ColonyRoleAssignment honoreeAssignment = roles.For(honoree);
            ColonyRank rank1 = trooperRole.ranks[1];
            SatisfyRankRequirements(honoree, trooperRole, rank1);

            // Verify PromoteGizmo switches to ceremony mode and opens Dialog_BeginRitual.
            Gizmo ceremonyGizmoRaw = ColonyPromotion.PromoteGizmo(honoree, honoreeAssignment);
            Command_Action ceremonyGizmo = ceremonyGizmoRaw as Command_Action;

            uiReport.ceremonyGizmoFound = ceremonyGizmo != null;
            uiReport.ceremonyGizmoEnabled = ceremonyGizmo != null && !ceremonyGizmo.Disabled;
            uiReport.ceremonyGizmoLabel = ceremonyGizmo != null ? ceremonyGizmo.defaultLabel : string.Empty;
            uiReport.ceremonyGizmoDesc = ceremonyGizmo != null ? ceremonyGizmo.defaultDesc : string.Empty;

            if (ceremonyGizmo != null && !ceremonyGizmo.Disabled && ceremonyGizmo.action != null)
            {
                ceremonyGizmo.action();
                uiReport.ceremonyDialogOpened = Find.WindowStack != null && Find.WindowStack.IsOpen<Dialog_BeginRitual>();
                if (Find.WindowStack != null)
                {
                    Find.WindowStack.TryRemove(typeof(Dialog_BeginRitual), false);
                }
            }

            // Validate ritual behavior worker CanStartRitualNow accepts this honoree and target.
            TargetInfo ceremonyTarget = ColonyRankCeremony.FindCeremonyTarget(honoree);
            Dictionary<string, Pawn> forcedRoles = new Dictionary<string, Pawn>();
            forcedRoles[ColonyRankCeremony.HonoreeRoleId] = honoree;

            string cantStartReason = ritual.behavior != null
                ? ritual.behavior.CanStartRitualNow(ceremonyTarget, ritual, honoree, forcedRoles) // CHECK API
                : "ritual.behavior is null";

            report.ceremonyCanStart = cantStartReason == null;
            if (!report.ceremonyCanStart)
            {
                report.notes.Add("Ritual CanStartRitualNow returned: " + cantStartReason);
            }

            // Execute RitualOutcomeEffectWorker_PromotionCeremony at unforgettable (best) quality.
            RitualOutcomeEffectWorker_PromotionCeremony worker = ritual.outcomeEffect as RitualOutcomeEffectWorker_PromotionCeremony;
            if (worker == null)
            {
                RitualOutcomeEffectDef outcomeDef = DefDatabase<RitualOutcomeEffectDef>.GetNamedSilentFail("TMM_PromotionCeremonyOutcome");
                if (outcomeDef != null)
                {
                    worker = new RitualOutcomeEffectWorker_PromotionCeremony(outcomeDef);
                }
            }

            if (worker == null || worker.def == null || worker.def.outcomeChances.NullOrEmpty())
            {
                report.notes.Add("RitualOutcomeEffectWorker_PromotionCeremony could not be resolved.");
                return;
            }

            RitualOutcomePossibility bestOutcome = worker.def.outcomeChances[worker.def.outcomeChances.Count - 1];
            Dictionary<Pawn, int> presence = new Dictionary<Pawn, int>();
            presence[honoree] = 3000;
            presence[commanderOfficiant] = 3000;
            if (spectator != null && spectator != honoree && spectator != commanderOfficiant)
            {
                presence[spectator] = 3000;
            }

            worker.ApplyResolvedOutcome(1f, 0.95f, bestOutcome, presence, null);

            report.ceremonyRankPromoted = honoreeAssignment != null && honoreeAssignment.rank == 1;
            report.ceremonyPolicySwitched = honoree.outfits != null
                && ColonyRankUniforms.IsPolicyForRank(honoree.outfits.CurrentApparelPolicy, trooperRole, rank1);

            ThoughtDef honoreeThought = DefDatabase<ThoughtDef>.GetNamedSilentFail(ColonyRankCeremony.HonoreeThoughtDefName);
            ThoughtDef spectatorThought = DefDatabase<ThoughtDef>.GetNamedSilentFail(ColonyRankCeremony.SpectatorThoughtDefName);
            HediffDef inspHediff = DefDatabase<HediffDef>.GetNamedSilentFail(ColonyRankCeremony.InspirationHediffDefName);

            report.ceremonyHonoreeThoughtGranted = HasMemory(honoree, honoreeThought);
            report.ceremonySpectatorThoughtGranted = HasMemory(commanderOfficiant, spectatorThought)
                && (spectator == null || HasMemory(spectator, spectatorThought));
            report.ceremonyInspirationHediffGranted = inspHediff != null
                && honoree.health != null
                && honoree.health.hediffSet != null
                && honoree.health.hediffSet.HasHediff(inspHediff);

            if (!report.ceremonyRankPromoted || !report.ceremonyHonoreeThoughtGranted || !report.ceremonySpectatorThoughtGranted || !report.ceremonyInspirationHediffGranted)
            {
                report.notes.Add("Ceremony outcome check mismatch: rank=" + report.ceremonyRankPromoted
                    + ", honoreeThought=" + report.ceremonyHonoreeThoughtGranted
                    + ", spectatorThought=" + report.ceremonySpectatorThoughtGranted
                    + ", inspiration=" + report.ceremonyInspirationHediffGranted);
            }
        }

        private static void RunRankWeaponCheck(Map map, Pawn trooper, ColonyRank rank, ProbeReport report)
        {
            if (map == null || trooper == null || rank == null || rank.uniform == null || !rank.uniform.HasWeapons)
            {
                report.notes.Add("Rank weapon check skipped: rank has no uniform weapons.");
                return;
            }

            List<ThingDef> rankWeapons = rank.uniform.GetWeapons();
            ThingDef weaponDef = rankWeapons.Count > 0 ? rankWeapons[0] : null;
            if (weaponDef == null) return;

            IntVec3 storageCell = FindOrCreateArmoryCell(map, trooper);
            if (!storageCell.IsValid)
            {
                report.notes.Add("Could not find or create a reachable storage cell for armory weapon check.");
                return;
            }

            if (trooper.equipment != null)
            {
                trooper.equipment.DestroyAllEquipment();
            }
            ColonyRankUniforms.ClearCooldowns();

            ThingWithComps weapon1 = MakeWeapon(weaponDef, QualityCategory.Normal);
            if (weapon1 == null) return;
            GenPlace.TryPlaceThing(weapon1, storageCell, map, ThingPlaceMode.Near);

            WorkGiver_EquipRankWeapon giver = new WorkGiver_EquipRankWeapon();
            bool wantsEquip = !giver.ShouldSkip(trooper, false) && giver.HasJobOnThing(trooper, weapon1, false);
            Job equipJob = wantsEquip ? giver.JobOnThing(trooper, weapon1, false) : null;

            report.rankWeaponJobCreated = equipJob != null
                && equipJob.def == JobDefOf.Equip
                && equipJob.targetA.Thing == weapon1;

            // Equip weapon1 on the pawn, spawn an equal weapon2 in storage, and confirm no re-equip loop occurs.
            if (weapon1.Spawned) weapon1.DeSpawn();
            if (trooper.equipment != null)
            {
                trooper.equipment.AddEquipment(weapon1);
            }

            ColonyRankUniforms.ClearCooldowns();
            ThingWithComps weapon2 = MakeWeapon(weaponDef, QualityCategory.Normal);
            if (weapon2 != null)
            {
                GenPlace.TryPlaceThing(weapon2, storageCell, map, ThingPlaceMode.Near);
                report.rankWeaponNoReequipLoop = !giver.HasJobOnThing(trooper, weapon2, false);
                if (weapon2.Spawned && !weapon2.Destroyed) weapon2.Destroy();
            }
        }

        private static ThingWithComps MakeWeapon(ThingDef def, QualityCategory quality)
        {
            ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            ThingWithComps w = ThingMaker.MakeThing(def, stuff) as ThingWithComps;
            if (w != null)
            {
                CompQuality cq = w.TryGetComp<CompQuality>();
                if (cq != null) cq.SetQuality(quality, ArtGenerationContext.Colony);
            }
            return w;
        }

        private static IntVec3 FindOrCreateArmoryCell(Map map, Pawn pawn)
        {
            // Prefer an existing reachable storage cell (stockpile or shelf).
            if (map.zoneManager != null && map.zoneManager.AllZones != null)
            {
                for (int i = 0; i < map.zoneManager.AllZones.Count; i++)
                {
                    Zone_Stockpile stock = map.zoneManager.AllZones[i] as Zone_Stockpile;
                    if (stock == null || stock.cells == null) continue;
                    for (int j = 0; j < stock.cells.Count; j++)
                    {
                        IntVec3 c = stock.cells[j];
                        if (c.Standable(map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.None))
                        {
                            return c;
                        }
                    }
                }
            }

            // Create a small temporary stockpile cell near the pawn.
            IntVec3 cell;
            if (CellFinder.TryFindRandomCellNear(pawn.Position, map, 6,
                delegate(IntVec3 c)
                {
                    return c.InBounds(map)
                        && c.Standable(map)
                        && !c.Fogged(map)
                        && map.zoneManager.ZoneAt(c) == null
                        && map.thingGrid.ThingsListAt(c).Count == 0
                        && pawn.CanReach(c, PathEndMode.OnCell, Danger.None);
                },
                out cell))
            {
                Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager); // CHECK API
                map.zoneManager.RegisterZone(zone);
                zone.AddCell(cell);
                return cell;
            }

            return IntVec3.Invalid;
        }

        private static bool RemoveCeremonyPreceptFromIdeo(Ideo ideo)
        {
            if (ideo == null) return false;
            PreceptDef preceptDef = DefDatabase<PreceptDef>.GetNamedSilentFail(ColonyRankCeremony.PreceptDefName);
            if (preceptDef == null) return false;

            List<Precept> list = ideo.PreceptsListForReading;
            if (list == null) return false;

            Precept found = null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].def == preceptDef)
                {
                    found = list[i];
                    break;
                }
            }

            if (found == null) return false;

            ideo.RemovePrecept(found, true); // CHECK API
            return true;
        }

        private static List<Pawn> EnsureTestColonists(Map map, int requiredCount)
        {
            List<Pawn> result = new List<Pawn>();
            if (map == null || map.mapPawns == null) return result;

            List<Pawn> existing = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < existing.Count; i++)
            {
                Pawn p = existing[i];
                if (p != null && !p.Dead && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Violent) && !p.WorkTagIsDisabled(WorkTags.Social))
                {
                    result.Add(p);
                }
            }

            while (result.Count < requiredCount)
            {
                Pawn generated = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    true,
                    false,
                    false,
                    false,
                    true,
                    1f,
                    false,
                    true,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    0f,
                    0f,
                    null,
                    1f,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    0f,
                    DevelopmentalStage.Adult)); // CHECK API

                if (generated == null) break;

                IntVec3 spawnPos;
                if (!CellFinder.TryFindRandomCellNear(map.Center, map, 15,
                    delegate(IntVec3 c) { return c.InBounds(map) && c.Standable(map) && !c.Fogged(map); },
                    out spawnPos))
                {
                    spawnPos = map.Center;
                }

                GenSpawn.Spawn(generated, spawnPos, map);
                if (!generated.Dead && !generated.Downed)
                {
                    result.Add(generated);
                }
            }

            return result;
        }

        private static void SatisfyRankRequirements(Pawn pawn, ColonyRoleDef role, ColonyRank rank)
        {
            if (pawn == null || role == null || rank == null) return;

            if (pawn.skills != null && role.skill != null)
            {
                SkillRecord rec = pawn.skills.GetSkill(role.skill);
                if (rec != null && rec.Level < rank.skillMin)
                {
                    rec.Level = rank.skillMin;
                }
            }

            if (pawn.records != null && rank.record != null)
            {
                float have = pawn.records.GetValue(rank.record);
                if (have < rank.recordMin)
                {
                    pawn.records.AddTo(rank.record, rank.recordMin - have + 1f); // CHECK API
                }
            }
        }

        private static bool HasMemory(Pawn pawn, ThoughtDef thoughtDef)
        {
            if (pawn == null || thoughtDef == null) return false;
            if (pawn.needs == null || pawn.needs.mood == null || pawn.needs.mood.thoughts == null) return false;
            return pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(thoughtDef) != null;
        }

        private static bool EvaluateMainPassed(ProbeReport r)
        {
            bool baseOk = string.IsNullOrEmpty(r.fatalException)
                && r.errorCount == 0
                && r.initialPolicyAppliedOnAssign
                && r.instantRankPromoted
                && r.instantPolicySwitched
                && r.customPolicyPawnPromoted
                && r.customPolicyPreserved
                && r.rankWeaponJobCreated
                && r.rankWeaponNoReequipLoop;

            if (!baseOk) return false;
            if (r.ceremonySkippedNoIdeology) return true;

            return r.ceremonyPreceptCreated
                && r.officiantRecognizedAsCommander
                && r.ceremonyCanStart
                && r.ceremonyRankPromoted
                && r.ceremonyPolicySwitched
                && r.ceremonyHonoreeThoughtGranted
                && r.ceremonySpectatorThoughtGranted
                && r.ceremonyInspirationHediffGranted;
        }

        private static bool EvaluateUiPassed(UiProbeReport ui, int errorCount)
        {
            bool baseOk = string.IsNullOrEmpty(ui.fatalException)
                && errorCount == 0
                && ui.instantGizmoFound
                && ui.instantGizmoEnabled
                && ui.localizationResolved;

            if (!baseOk) return false;
            if (ui.ceremonySkippedNoIdeology) return true;

            return ui.ceremonyGizmoFound
                && ui.ceremonyGizmoEnabled
                && ui.ceremonyDialogOpened;
        }

        private static string ResolveReportDirectory()
        {
            if (!string.IsNullOrEmpty(CustomReportDir))
                return CustomReportDir;

            string envDir = Environment.GetEnvironmentVariable("TMM_PROBE_DIR");
            if (!string.IsNullOrEmpty(envDir))
                return envDir;

            return GenFilePaths.SaveDataFolderPath;
        }

        private static void WriteReports(ProbeReport report, UiProbeReport uiReport)
        {
            try
            {
                string dir = ResolveReportDirectory();
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string mainPath = Path.Combine(dir, MainReportFileName);
                string uiPath = Path.Combine(dir, UiReportFileName);

                File.WriteAllText(mainPath, SerializeMainReport(report), Encoding.UTF8);
                File.WriteAllText(uiPath, SerializeUiReport(uiReport), Encoding.UTF8);

                Log.Message("[Turbo mega mod] Wrote probe reports: " + mainPath + " | " + uiPath);
            }
            catch (Exception ex)
            {
                Log.Error("[Turbo mega mod] Failed to write ColonyRanks probe reports: " + ex);
            }
        }

        private static string SerializeMainReport(ProbeReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            AppendJsonBool(sb, "passed", r.passed, true);
            AppendJsonBool(sb, "ideologyActive", r.ideologyActive, true);
            AppendJsonBool(sb, "classicIdeoMode", r.classicIdeoMode, true);
            AppendJsonBool(sb, "initialPolicyAppliedOnAssign", r.initialPolicyAppliedOnAssign, true);
            AppendJsonBool(sb, "instantRankPromoted", r.instantRankPromoted, true);
            AppendJsonBool(sb, "instantPolicySwitched", r.instantPolicySwitched, true);
            AppendJsonBool(sb, "customPolicyPawnPromoted", r.customPolicyPawnPromoted, true);
            AppendJsonBool(sb, "customPolicyPreserved", r.customPolicyPreserved, true);
            AppendJsonBool(sb, "rankWeaponJobCreated", r.rankWeaponJobCreated, true);
            AppendJsonBool(sb, "rankWeaponNoReequipLoop", r.rankWeaponNoReequipLoop, true);
            AppendJsonBool(sb, "ceremonySkippedNoIdeology", r.ceremonySkippedNoIdeology, true);
            AppendJsonBool(sb, "ceremonyPreceptCreated", r.ceremonyPreceptCreated, true);
            AppendJsonBool(sb, "officiantRecognizedAsCommander", r.officiantRecognizedAsCommander, true);
            AppendJsonBool(sb, "ceremonyCanStart", r.ceremonyCanStart, true);
            AppendJsonBool(sb, "ceremonyRankPromoted", r.ceremonyRankPromoted, true);
            AppendJsonBool(sb, "ceremonyPolicySwitched", r.ceremonyPolicySwitched, true);
            AppendJsonBool(sb, "ceremonyHonoreeThoughtGranted", r.ceremonyHonoreeThoughtGranted, true);
            AppendJsonBool(sb, "ceremonySpectatorThoughtGranted", r.ceremonySpectatorThoughtGranted, true);
            AppendJsonBool(sb, "ceremonyInspirationHediffGranted", r.ceremonyInspirationHediffGranted, true);
            AppendJsonInt(sb, "errorCount", r.errorCount, true);
            AppendJsonString(sb, "fatalException", r.fatalException, true);
            AppendJsonStringList(sb, "notes", r.notes, true);
            AppendJsonStringList(sb, "errors", r.errors, false);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string SerializeUiReport(UiProbeReport ui)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            AppendJsonBool(sb, "passed", ui.passed, true);
            AppendJsonBool(sb, "instantGizmoFound", ui.instantGizmoFound, true);
            AppendJsonBool(sb, "instantGizmoEnabled", ui.instantGizmoEnabled, true);
            AppendJsonString(sb, "instantGizmoLabel", ui.instantGizmoLabel, true);
            AppendJsonString(sb, "instantGizmoDesc", ui.instantGizmoDesc, true);
            AppendJsonBool(sb, "ceremonySkippedNoIdeology", ui.ceremonySkippedNoIdeology, true);
            AppendJsonBool(sb, "ceremonyGizmoFound", ui.ceremonyGizmoFound, true);
            AppendJsonBool(sb, "ceremonyGizmoEnabled", ui.ceremonyGizmoEnabled, true);
            AppendJsonString(sb, "ceremonyGizmoLabel", ui.ceremonyGizmoLabel, true);
            AppendJsonString(sb, "ceremonyGizmoDesc", ui.ceremonyGizmoDesc, true);
            AppendJsonBool(sb, "ceremonyDialogOpened", ui.ceremonyDialogOpened, true);
            AppendJsonBool(sb, "localizationResolved", ui.localizationResolved, true);
            AppendJsonInt(sb, "errorCount", ui.errorCount, true);
            AppendJsonString(sb, "fatalException", ui.fatalException, false);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void AppendJsonBool(StringBuilder sb, string key, bool value, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": ").Append(value ? "true" : "false");
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendJsonInt(StringBuilder sb, string key, int value, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendJsonString(StringBuilder sb, string key, string value, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": \"").Append(EscapeJson(value ?? string.Empty)).Append("\"");
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendJsonStringList(StringBuilder sb, string key, List<string> values, bool comma)
        {
            sb.Append("  \"").Append(EscapeJson(key)).Append("\": [");
            if (values != null && values.Count > 0)
            {
                sb.AppendLine();
                for (int i = 0; i < values.Count; i++)
                {
                    sb.Append("    \"").Append(EscapeJson(values[i] ?? string.Empty)).Append("\"");
                    sb.AppendLine(i + 1 < values.Count ? "," : string.Empty);
                }
                sb.Append("  ]");
            }
            else
            {
                sb.Append("]");
            }
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            StringBuilder sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        private sealed class ProbeReport
        {
            public bool passed;
            public bool ideologyActive;
            public bool classicIdeoMode;
            public bool initialPolicyAppliedOnAssign;
            public bool instantRankPromoted;
            public bool instantPolicySwitched;
            public bool customPolicyPawnPromoted;
            public bool customPolicyPreserved;
            public bool rankWeaponJobCreated;
            public bool rankWeaponNoReequipLoop;
            public bool ceremonySkippedNoIdeology;
            public bool ceremonyPreceptCreated;
            public bool officiantRecognizedAsCommander;
            public bool ceremonyCanStart;
            public bool ceremonyRankPromoted;
            public bool ceremonyPolicySwitched;
            public bool ceremonyHonoreeThoughtGranted;
            public bool ceremonySpectatorThoughtGranted;
            public bool ceremonyInspirationHediffGranted;
            public int errorCount;
            public string fatalException = string.Empty;
            public List<string> notes = new List<string>();
            public List<string> errors = new List<string>();
        }

        private sealed class UiProbeReport
        {
            public bool passed;
            public bool instantGizmoFound;
            public bool instantGizmoEnabled;
            public string instantGizmoLabel = string.Empty;
            public string instantGizmoDesc = string.Empty;
            public bool ceremonySkippedNoIdeology;
            public bool ceremonyGizmoFound;
            public bool ceremonyGizmoEnabled;
            public string ceremonyGizmoLabel = string.Empty;
            public string ceremonyGizmoDesc = string.Empty;
            public bool ceremonyDialogOpened;
            public bool localizationResolved;
            public int errorCount;
            public string fatalException = string.Empty;
        }
    }
}