using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using TurboMegaMod;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class MinutemenHuntersLaunch
    {
        static MinutemenHuntersLaunch()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Any(a => a == "-tmm-minutemen-hunters-probe" || a == "-tmm-minutemen-hunters-reload")) return;
            var h = new Harmony("tmm.test.minutemen.hunters");
            h.Patch(AccessTools.PropertyGetter(typeof(MapTemperature), "OutdoorTemp"), prefix: new HarmonyMethod(typeof(MinutemenHuntersLaunch), "Temp"));
            if (args.Contains("-tmm-minutemen-hunters-reload"))
            {
                LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-MinutemenHunters"));
            }
        }
        private static bool Temp(ref float __result) { __result = 21f; return false; }
    }

    public sealed class MinutemenHuntersProbe : MapComponent
    {
        private int checks;
        private bool reported, uiReported;
        private bool reloadInitialized;
        private readonly List<string> errors = new List<string>();

        public MinutemenHuntersProbe(Map map) : base(map) { }

        private void Check(bool ok, string message)
        {
            if (!ok) throw new Exception(message);
            checks++;
        }

        private static string Q(string s)
        {
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
        }

        private void RunFreshChecks()
        {
            // 1. Defs Loading & Non-null Verification
            Check(MinutemenHuntersDefOf.TMM_MinutemenHuntersFaction != null, "TMM_MinutemenHuntersFaction def is null");
            Check(MinutemenHuntersDefOf.TMM_HuntersMilitiaman != null, "TMM_HuntersMilitiaman def is null");
            Check(MinutemenHuntersDefOf.TMM_HuntersHunter != null, "TMM_HuntersHunter def is null");
            Check(MinutemenHuntersDefOf.TMM_HuntersMedic != null, "TMM_HuntersMedic def is null");
            Check(MinutemenHuntersDefOf.TMM_HuntersFieldArchive != null, "TMM_HuntersFieldArchive def is null");
            Check(MinutemenHuntersDefOf.TMM_MinutemenOutpostSitePart != null, "TMM_MinutemenOutpostSitePart def is null");
            Check(MinutemenHuntersDefOf.TMM_MinutemenReliefQuest != null, "TMM_MinutemenReliefQuest def is null");
            Check(MinutemenHuntersDefOf.TMM_MinutemenArchiveQuest != null, "TMM_MinutemenArchiveQuest def is null");

            // 2. World Component & Safe Faction Registration
            var comp = MinutemenHuntersWorldComponent.Instance;
            Check(comp != null, "MinutemenHuntersWorldComponent is null");
            comp.EnsureFactionExists();
            var faction = MinutemenHuntersWorldComponent.Faction;
            Check(faction != null, "Minutemen & Hunters faction was not registered");
            Check(faction.def == MinutemenHuntersDefOf.TMM_MinutemenHuntersFaction, "Faction has wrong def");
            Check(faction.leader != null, "Faction leader is null");
            Check(!faction.HostileTo(Faction.OfPlayer), "Faction should start neutral/non-hostile to player");

            // 3. Pawn Generation & Gear Integrity
            var militiamanReq = new PawnGenerationRequest(MinutemenHuntersDefOf.TMM_HuntersMilitiaman, faction, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, fixedBiologicalAge: 25f);
            var militiaman = PawnGenerator.GeneratePawn(militiamanReq);
            Check(militiaman != null && militiaman.equipment.Primary != null, "Militiaman pawn generated without primary weapon");
            GenSpawn.Spawn(militiaman, new IntVec3(45, 0, 45), map);

            var hunterReq = new PawnGenerationRequest(MinutemenHuntersDefOf.TMM_HuntersHunter, faction, canGeneratePawnRelations: false, fixedBiologicalAge: 32f);
            var hunter = PawnGenerator.GeneratePawn(hunterReq);
            Check(hunter != null && hunter.apparel.WornApparel.Any(a => a.def == DefDatabase<ThingDef>.GetNamed("Apparel_Duster")), "Hunter pawn generated without duster");
            GenSpawn.Spawn(hunter, new IntVec3(46, 0, 45), map);

            var medicReq = new PawnGenerationRequest(MinutemenHuntersDefOf.TMM_HuntersMedic, faction, canGeneratePawnRelations: false, fixedBiologicalAge: 29f);
            var medic = PawnGenerator.GeneratePawn(medicReq);
            Check(medic != null, "Medic pawn is null");
            GenSpawn.Spawn(medic, new IntVec3(47, 0, 45), map);

            // 4. Outpost Map Generation (~45x45)
            var outpostGen = new GenStep_MinutemenOutpost();
            outpostGen.Generate(map, default(GenStepParams));

            var comms = map.listerThings.AllThings.FirstOrDefault(t => t.def == ThingDefOf.CommsConsole);
            Check(comms != null, "Outpost radio station did not generate CommsConsole");

            var archiveItem = map.listerThings.AllThings.FirstOrDefault(t => t.def == MinutemenHuntersDefOf.TMM_HuntersFieldArchive);
            Check(archiveItem != null, "Outpost archive did not generate TMM_HuntersFieldArchive");

            var medicalBeds = map.listerThings.AllThings.OfType<Building_Bed>().Where(b => b.def.building != null && b.def.building.bed_humanlike && b.Medical).ToList();
            Check(medicalBeds.Count >= 2, "Outpost clinic did not generate medical beds");

            var sandbags = map.listerThings.AllThings.Where(t => t.def == ThingDefOf.Sandbags).ToList();
            Check(sandbags.Count >= 10, "Outpost perimeter did not generate defensive sandbags");

            // 5. Quest 1: Relief Supplies
            comp.quest1Completed = false;
            comp.quest2Unlocked = false;
            comp.quest2Completed = false;

            var quest1 = QuestUtility.GenerateQuestAndMakeAvailable(MinutemenHuntersDefOf.TMM_MinutemenReliefQuest, new Slate());
            Check(quest1 != null, "Quest 1 generation failed");
            Check(quest1.State == QuestState.NotYetAccepted, "Quest 1 not in NotYetAccepted state");
            quest1.Accept(null);
            Check(quest1.State == QuestState.Ongoing, "Quest 1 not in Ongoing state after acceptance");

            var reliefPart = quest1.PartsListForReading.OfType<QuestPart_MinutemenRelief>().FirstOrDefault();
            Check(reliefPart != null, "QuestPart_MinutemenRelief missing from Quest 1");

            // Provide required medicine in storage and fulfill
            var meds = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
            meds.stackCount = 5;
            GenSpawn.Spawn(meds, new IntVec3(45, 0, 40), map);

            bool fulfilled1 = reliefPart.TryFulfillDelivery();
            Check(fulfilled1, "QuestPart_MinutemenRelief.TryFulfillDelivery failed");
            Check(comp.quest1Completed, "WorldComponent quest1Completed flag not set");
            Check(comp.quest2Unlocked, "WorldComponent quest2Unlocked flag not set");
            Check(quest1.State == QuestState.EndedSuccess, "Quest 1 did not end with Success");

            // 6. Quest 2: Field Archive Expedition
            var quest2 = QuestUtility.GenerateQuestAndMakeAvailable(MinutemenHuntersDefOf.TMM_MinutemenArchiveQuest, new Slate());
            Check(quest2 != null, "Quest 2 generation failed");
            quest2.Accept(null);
            Check(quest2.State == QuestState.Ongoing, "Quest 2 not Ongoing after acceptance");

            var archivePart = quest2.PartsListForReading.OfType<QuestPart_MinutemenArchive>().FirstOrDefault();
            Check(archivePart != null, "QuestPart_MinutemenArchive missing from Quest 2");
            Check(archivePart.site != null, "QuestPart_MinutemenArchive site was not created");

            // Fulfill archive recovery
            bool fulfilled2 = archivePart.TryFulfillRecovery(archiveItem, map);
            Check(fulfilled2, "QuestPart_MinutemenArchive.TryFulfillRecovery failed");
            Check(comp.quest2Completed, "WorldComponent quest2Completed flag not set");
            Check(quest2.State == QuestState.EndedSuccess, "Quest 2 did not end with Success");

            // 7. Save game checkpoint
            GameDataSaveLoader.SaveGame("TMM-MinutemenHunters");
            Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-MinutemenHunters")), "TMM-MinutemenHunters save was not created");

            Finish(false);
        }

        private void RunReloadChecks()
        {
            if (reloadInitialized) return;
            reloadInitialized = true;

            var comp = MinutemenHuntersWorldComponent.Instance;
            Check(comp != null, "MinutemenHuntersWorldComponent null on reload");
            Check(comp.quest1Completed, "quest1Completed flag lost on reload");
            Check(comp.quest2Completed, "quest2Completed flag lost on reload");

            var faction = MinutemenHuntersWorldComponent.Faction;
            Check(faction != null, "Minutemen faction null on reload");
            Check(faction.leader != null, "Faction leader null on reload");

            var archiveItem = map.listerThings.AllThings.FirstOrDefault(t => t.def == MinutemenHuntersDefOf.TMM_HuntersFieldArchive);
            Check(archiveItem != null, "TMM_HuntersFieldArchive lost on reload");

            Finish(true);
        }

        public override void MapComponentUpdate()
        {
            var args = Environment.GetCommandLineArgs();
            bool isProbe = args.Contains("-tmm-minutemen-hunters-probe");
            bool isReload = args.Contains("-tmm-minutemen-hunters-reload");
            if (reported || Find.CurrentMap != map || (!isProbe && !isReload)) return;

            try
            {
                if (isReload)
                {
                    RunReloadChecks();
                }
                else
                {
                    RunFreshChecks();
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
                Finish(isReload);
            }
        }

        public override void MapComponentOnGUI()
        {
            var args = Environment.GetCommandLineArgs();
            if (uiReported || !reported || Event.current.type != EventType.Repaint) return;
            if (!args.Contains("-tmm-minutemen-hunters-probe") && !args.Contains("-tmm-minutemen-hunters-reload")) return;
            uiReported = true;
            string error = null;
            try
            {
                // Verify localization keys can be translated without error
                string t1 = "TMM_MinutemenReliefQuest_Title".Translate();
                string t2 = "TMM_MinutemenArchiveQuest_Title".Translate();
                if (string.IsNullOrEmpty(t1) || string.IsNullOrEmpty(t2))
                    throw new Exception("Translation strings empty");
            }
            catch (Exception ex)
            {
                error = ex.ToString();
            }

            string outPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "MinutemenHuntersUiProbe.json");
            string json = "{\"passed\":" + (error == null ? "true" : "false") + ",\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Q(error)) + "}";
            File.WriteAllText(outPath, json);
        }

        private void Finish(bool reload)
        {
            reported = true;
            string fileName = reload ? "MinutemenHuntersReloadProbe.json" : "MinutemenHuntersProbe.json";
            string filePath = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
            string json = "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":" + (errors.Count == 0 ? "[]" : "[" + string.Join(",", errors.Select(Q)) + "]") + "}";
            File.WriteAllText(filePath, json);
            Log.Message("[TMM-Probe] Finished MinutemenHuntersProbe (reload=" + reload + "). Passed: " + (errors.Count == 0) + ", Checks: " + checks);
        }
    }
}
