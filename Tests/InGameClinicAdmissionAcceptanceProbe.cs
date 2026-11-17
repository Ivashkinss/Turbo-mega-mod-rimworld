using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class ClinicAdmissionLaunch
{
    static ClinicAdmissionLaunch()
    {
        var args = Environment.GetCommandLineArgs();
        if (!args.Any(a => a == "-tmm-clinic-admission-probe" || a == "-tmm-clinic-admission-reload")) return;
        var h = new Harmony("tmm.test.clinic.admission");
        h.Patch(AccessTools.PropertyGetter(typeof(MapTemperature), "OutdoorTemp"), prefix: new HarmonyMethod(typeof(ClinicAdmissionLaunch), "Temp"));
        if (args.Contains("-tmm-clinic-admission-reload"))
        {
            LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-ClinicAdmission"));
        }
    }
    private static bool Temp(ref float __result) { __result = 21f; return false; }
}

public sealed class ClinicAdmissionAcceptanceProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building_Bed wardBed;
    private Building_Bed colonyBed;
    private Pawn doctor;
    private Pawn patient;
    private Thing medicine;
    private Thing food;
    private int checks, stage, startTick, nextDiagnosticTick;
    private float preTendBill, postTendBill;
    private int initialMedCount, savedFloorSilver;
    private bool saved, reported, uiReported;
    private bool reloadInitialized;
    private readonly List<string> errors = new List<string>();

    public ClinicAdmissionAcceptanceProbe(Map map) : base(map) { }

    private object HospitalComponent
    {
        get { return Clinic.Hospital(map); }
    }

    private IDictionary HospitalPatients
    {
        get { return Clinic.Patients(map); }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref desk, "caaDesk");
        Scribe_References.Look(ref wardBed, "caaWardBed");
        Scribe_References.Look(ref colonyBed, "caaColonyBed");
        Scribe_References.Look(ref doctor, "caaDoctor");
        Scribe_References.Look(ref patient, "caaPatient");
        Scribe_References.Look(ref medicine, "caaMedicine");
        Scribe_References.Look(ref food, "caaFood");
        Scribe_Values.Look(ref saved, "caaSaved");
        Scribe_Values.Look(ref checks, "caaChecks");
        Scribe_Values.Look(ref preTendBill, "caaPreTendBill");
        Scribe_Values.Look(ref postTendBill, "caaPostTendBill");
        Scribe_Values.Look(ref initialMedCount, "caaInitialMedCount");
        Scribe_Values.Look(ref savedFloorSilver, "caaSavedFloorSilver");
    }

    private void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++;
    }

    private static string Q(string s)
    {
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
    }

    private static void Wait(Pawn p)
    {
        var job = JobMaker.MakeJob(JobDefOf.Wait);
        job.expiryInterval = 600000;
        p.jobs.StartJob(job, JobCondition.InterruptForced);
    }

    private Building Build(string name, IntVec3 c)
    {
        var d = DefDatabase<ThingDef>.GetNamed(name);
        var b = (Building)ThingMaker.MakeThing(d, d.MadeFromStuff ? ThingDefOf.Steel : null);
        b.SetFaction(Faction.OfPlayer);
        return (Building)GenSpawn.Spawn(b, c, map);
    }

    private Building_Bed Bed(IntVec3 cell)
    {
        foreach (var thing in cell.GetThingList(map).ToArray())
        {
            if (thing.def.defName == "VoidMonolith") continue;
            if (thing is Building || thing is Plant || thing is Filth)
            {
                if (thing.def.destroyable) thing.Destroy(); else thing.DeSpawn();
            }
        }
        var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.Steel);
        bed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(bed, cell, map);
        bed.Medical = true;
        return bed;
    }

    private void Room(CellRect rect)
    {
        foreach (var c in rect.EdgeCells)
            Build(c == new IntVec3(rect.CenterCell.x, 0, rect.minZ) ? "Door" : "Wall", c);
        foreach (var c in rect.ContractedBy(1))
            map.roofGrid.SetRoof(c, RoofDefOf.RoofConstructed);
    }

    private Thing Stack(string name, IntVec3 c, int count)
    {
        var d = DefDatabase<ThingDef>.GetNamed(name);
        var t = ThingMaker.MakeThing(d);
        t.stackCount = count;
        return GenSpawn.Spawn(t, c, map);
    }

    private int FloorSilver()
    {
        return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t => t.Spawned).Sum(t => t.stackCount);
    }

    private void Setup()
    {
        map.fogGrid.ClearAllFog();
        // The native admission worker refuses hostile/aggro map pawns even after basic admission guards pass.
        foreach(var threat in map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction!=Faction.OfPlayer&&(p.HostileTo(Faction.OfPlayer)||p.InAggroMentalState)).ToArray()){
            threat.DeSpawn();Verse.Find.WorldPawns.PassToWorld(threat,RimWorld.Planet.PawnDiscardDecideMode.Discard);
        }

        // 1. Clear terrain (38..62, 38..52), preserve VoidMonolith
        foreach (var cell in new CellRect(38, 38, 26, 16))
        {
            foreach (var t in cell.GetThingList(map).ToArray())
            {
                if (t.def.defName == "VoidMonolith") continue;
                if (t is Building || t is Plant || t is Filth)
                {
                    if (t.def.destroyable) t.Destroy(); else t.DeSpawn();
                }
            }
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            map.roofGrid.SetRoof(cell, null);
            map.areaManager.Home[cell] = true;
        }

        // 2. Build rooms: Clinical Ward (40,40, 8,8) and Colony Infirmary (52,40, 8,8)
        Room(new CellRect(40, 40, 8, 8));
        Room(new CellRect(52, 40, 8, 8));
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        new IntVec3(44, 0, 44).GetRoom(map).Temperature = 21f;
        new IntVec3(56, 0, 44).GetRoom(map).Temperature = 21f;

        // 3. Beds: Ward bed in clinic room, colony bed in separate room
        wardBed = Bed(new IntVec3(44, 0, 45));
        colonyBed = Bed(new IntVec3(56, 0, 45));

        // 4. Allowed areas and Complex Desk
        Area_Allowed complexArea;
        map.areaManager.TryMakeNewAllowed(out complexArea);
        foreach (var cell in new CellRect(39, 39, 11, 15)) complexArea[cell] = true;

        Area_Allowed wardArea;
        map.areaManager.TryMakeNewAllowed(out wardArea);
        foreach (var cell in new CellRect(41, 41, 6, 6)) wardArea[cell] = true;

        desk = (Building_ComplexDesk)Build("TMM_ComplexDesk", new IntVec3(42, 0, 41));
        desk.automaticServices = true;
        desk.area = complexArea;
        desk.clinicArea = wardArea;

        // 5. Doctor assignment
        doctor = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p => !p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor));
        if (doctor == null)
        {
            doctor = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            GenSpawn.Spawn(doctor, new IntVec3(44, 0, 43), map);
        }
        foreach (var p in map.mapPawns.FreeColonistsSpawned)
        {
            if (p != doctor) Wait(p);
        }

        doctor.Position = new IntVec3(44, 0, 43);
        doctor.Notify_Teleported();
        doctor.skills.GetSkill(SkillDefOf.Medicine).Level = 15;
        foreach (var w in DefDatabase<WorkTypeDef>.AllDefsListForReading) doctor.workSettings.SetPriority(w, 0);
        doctor.workSettings.SetPriority(WorkTypeDefOf.Doctor, 1);
        for (int h = 0; h < 24; h++) doctor.timetable.SetAssignment(h, TimeAssignmentDefOf.Work);
        doctor.needs.food.CurLevelPercentage = 1f;
        doctor.needs.rest.CurLevelPercentage = 1f;
        if (doctor.needs.joy != null) doctor.needs.joy.CurLevelPercentage = 1f;
        desk.SetRole(doctor, ComplexRole.Doctor);

        // 6. Medical supplies & food
        medicine = Stack("MedicineIndustrial", new IntVec3(43, 0, 44), 3);
        food = Stack("MealSimple", new IntVec3(43, 0, 43), 3);
        initialMedCount = medicine.stackCount;
        savedFloorSilver = FloorSilver();

        // 7. Verify Clinical Bed Isolation (Colony medical bed outside clinic is NOT clinical capacity)
        Check(Clinic.WardBed(wardBed), "Ward bed not recognized as WardBed");
        Check(!Clinic.WardBed(colonyBed), "Colony bed outside clinic mistakenly claimed as WardBed");
        int clinicalBeds = (int)AccessTools.Method(Clinic.HospitalType, "BedCount").Invoke(HospitalComponent, null);
        Check(clinicalBeds == 1, "Hospital BedCount did not isolate colony medical bed: " + clinicalBeds);

        // 8. Test Owning Guards: CanSpawnPatient / CanFireNowSub / IsOpen
        TestAdmissionGuards();

        // 9. Fire Real PatientArrives Incident
        FirePatientArrivesIncident();

        startTick = Find.TickManager.TicksGame;
        stage = 1;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
    }

    private void TestAdmissionGuards()
    {
        var hospital = HospitalComponent;
        var incidentDef = DefDatabase<IncidentDef>.GetNamed("PatientArrives");
        Check(incidentDef != null, "IncidentDef PatientArrives not found");
        var incidentWorker = incidentDef.Worker;
        Check(incidentWorker != null, "IncidentWorker PatientArrives not found");

        var parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
        var canSpawnMethod = AccessTools.Method(AccessTools.TypeByName("Hospital.IncidentHelper"), "CanSpawnPatient");
        var canFireNowSubMethod = AccessTools.Method(incidentWorker.GetType(), "CanFireNowSub");
        var isOpenMethod = AccessTools.Method(Clinic.HospitalType, "IsOpen");
        var isFullMethod = AccessTools.Method(Clinic.HospitalType, "IsFull");

        // Guard 1: Closed (openForBusiness = false)
        AccessTools.Field(Clinic.HospitalType, "openForBusiness").SetValue(hospital, false);
        Check(!(bool)isOpenMethod.Invoke(hospital, null), "Guard: IsOpen was true when closed");
        Check(!(bool)canSpawnMethod.Invoke(null, new object[] { map }), "Guard: CanSpawnPatient was true when closed");
        Check(!(bool)canFireNowSubMethod.Invoke(incidentWorker, new object[] { parms }), "Guard: CanFireNowSub was true when closed");

        // Guard 2: Admission Hours (openForBusiness = true, but hours all false)
        AccessTools.Field(Clinic.HospitalType, "openForBusiness").SetValue(hospital, true);
        AccessTools.Field(Clinic.HospitalType, "openingHours").SetValue(hospital, Enumerable.Repeat(false, 24).ToList());
        Check(!(bool)isOpenMethod.Invoke(hospital, null), "Guard: IsOpen was true when opening hours disabled");
        Check(!(bool)canSpawnMethod.Invoke(null, new object[] { map }), "Guard: CanSpawnPatient was true when hours disabled");
        Check(!(bool)canFireNowSubMethod.Invoke(incidentWorker, new object[] { parms }), "Guard: CanFireNowSub was true when hours disabled");

        // Restore admission hours (all true)
        AccessTools.Field(Clinic.HospitalType, "openingHours").SetValue(hospital, Enumerable.Repeat(true, 24).ToList());
        Check((bool)isOpenMethod.Invoke(hospital, null), "Guard: IsOpen was false after restoring hours");
        Check((bool)canSpawnMethod.Invoke(null, new object[] { map }), "Guard: CanSpawnPatient was false after restoring hours");
        Check((bool)canFireNowSubMethod.Invoke(incidentWorker, new object[] { parms }), "Guard: CanFireNowSub was false after restoring hours");

        // Guard 3: Occupied / Zero Free Beds (hospital.IsFull() == true)
        var neutralFaction = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
        var dummyPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, neutralFaction);
        GenSpawn.Spawn(dummyPawn, new IntVec3(45, 0, 44), map);
        var patientDataType = AccessTools.TypeByName("Hospital.PatientData");
        var patientTypeEnum = AccessTools.TypeByName("Hospital.PatientType");
        var dummyData = Activator.CreateInstance(patientDataType, new object[] { Find.TickManager.TicksGame, dummyPawn.MarketValue, 0.5f, Enum.ToObject(patientTypeEnum, 2) });
        var patientsDict = HospitalPatients;
        patientsDict.Add(dummyPawn, dummyData);

        Check((bool)isFullMethod.Invoke(hospital, null), "Guard: hospital not full with 1 ward bed and 1 patient");
        Check(!(bool)canSpawnMethod.Invoke(null, new object[] { map }), "Guard: CanSpawnPatient true when hospital full");
        Check(!(bool)canFireNowSubMethod.Invoke(incidentWorker, new object[] { parms }), "Guard: CanFireNowSub true when hospital full");

        // Restore Open: Remove dummy patient
        patientsDict.Remove(dummyPawn);
        dummyPawn.Destroy();

        Check(!(bool)isFullMethod.Invoke(hospital, null), "Guard: hospital full after removing dummy");
        Check((bool)isOpenMethod.Invoke(hospital, null), "Guard: hospital not open after removing dummy");
        Check((bool)canSpawnMethod.Invoke(null, new object[] { map }), "Guard: CanSpawnPatient false in restored open state");
        Check((bool)canFireNowSubMethod.Invoke(incidentWorker, new object[] { parms }), "Guard: CanFireNowSub false in restored open state");
    }

    private void FirePatientArrivesIncident()
    {
        var hospital = HospitalComponent;
        var incidentDef = DefDatabase<IncidentDef>.GetNamed("PatientArrives");
        var incidentWorker = incidentDef.Worker;
        var parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);

        // Native parameters: Exclude surgery and blacklist long diseases so patient natively falls back to Wounds
        AccessTools.Field(Clinic.HospitalType, "AcceptSurgery").SetValue(hospital, false);
        var diseaseUtilType = AccessTools.TypeByName("Hospital.Utilities.DiseaseUtility");
        var candidateMethod = AccessTools.Method(diseaseUtilType, "CandidateDiseases");
        var candidates = ((IEnumerable<HediffDef>)candidateMethod.Invoke(null, null)).ToList();
        AccessTools.Field(Clinic.HospitalType, "refusedDiseases").SetValue(hospital, candidates);

        // Execute incident worker
        var tryExecuteMethod = AccessTools.Method(incidentWorker.GetType(), "TryExecuteWorker");
        bool fired = (bool)tryExecuteMethod.Invoke(incidentWorker, new object[] { parms });
        Check(fired, "IncidentWorker_PatientArrives.TryExecuteWorker returned false; factions="+string.Join(",",Find.FactionManager.AllFactionsListForReading.Where(f=>!f.IsPlayer).Select(f=>f.Name+":"+f.AllyOrNeutralTo(Faction.OfPlayer)))+" pawns="+string.Join(",",map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction!=null).Select(p=>p.LabelShort+":"+p.Faction.Name+":"+p.HostileTo(Faction.OfPlayer))));

        // Check drop pod capsule on map
        var dropPod = map.listerThings.AllThings.FirstOrDefault(t => t.def == ThingDefOf.DropPodIncoming || t.GetType().Name.Contains("DropPod"));
        Check(dropPod != null, "Drop pod capsule was not created by PatientArrives incident");

        // Check patient registered in hospital
        var patientsDict = HospitalPatients;
        Check(patientsDict.Count == 1, "Patient not registered in hospital.Patients count=" + patientsDict.Count);
        foreach (DictionaryEntry entry in patientsDict)
        {
            patient = (Pawn)entry.Key;
            var data = entry.Value;
            Check(patient != null, "Patient pawn is null in registry");
            Check(data != null, "PatientData is null in registry");
            preTendBill = (float)AccessTools.Field(data.GetType(), "Bill").GetValue(data);
        }

    }

    private void TickFresh()
    {
        if (stage == 0)
        {
            Setup();
            return;
        }

        if (stage == 1)
        {
            // Wait for drop pod to open and patient to spawn on map
            if (!patient.Spawned || patient.Map != map)
            {
                if (Find.TickManager.TicksGame - startTick > 1000)
                    throw new Exception("Patient drop pod did not open in time (ticks=" + (Find.TickManager.TicksGame - startTick) + ")");
                return;
            }

            Check(patient.mindState.duty != null && patient.mindState.duty.def.defName == "Patient", "Patient duty not set on map arrival");
            var lord=patient.GetLord();
            Check(lord!=null&&lord.LordJob.GetType().Name=="LordJob_VisitColonyAsPatient","Native patient Lord missing after pod opened");
            patient.playerSettings.medCare = MedicalCareCategory.Best;
            Check(patient.health.HasHediffsNeedingTend(), "Patient has no hediffs needing tend on arrival");

            var data = Clinic.Data(patient);
            Check(data != null, "PatientData missing after spawn");
            preTendBill = (float)AccessTools.Field(data.GetType(), "Bill").GetValue(data);

            startTick = Find.TickManager.TicksGame;
            stage = 2;
            return;
        }

        if (stage == 2)
        {
            // Wait for patient to reach and lay down in ward bed
            if (patient.CurrentBed() != wardBed)
            {
                if (Find.TickManager.TicksGame - startTick > 2500)
                    throw new Exception("Patient did not reach ward bed in time (currentBed=" + patient.CurrentBed() + ", job=" + patient.CurJob + ")");
                return;
            }

            Check(patient.CurrentBed() == wardBed, "Patient occupied incorrect bed: " + patient.CurrentBed());
            startTick = Find.TickManager.TicksGame;
            stage = 3;
            return;
        }

        if (stage == 3)
        {
            // Wait for doctor to tend patient using medicine
            var data = Clinic.Data(patient);
            float currentBill = data == null ? 0 : (float)AccessTools.Field(data.GetType(), "Bill").GetValue(data);
            bool medicineUsed = medicine.stackCount < initialMedCount;
            bool billIncreased = currentBill > preTendBill;

            if (Find.TickManager.TicksGame >= nextDiagnosticTick)
            {
                nextDiagnosticTick = Find.TickManager.TicksGame + 500;
                File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "ClinicAdmissionDiagnostics.txt"),
                    "tick=" + Find.TickManager.TicksGame +
                    " doctorJob=" + doctor.CurJob +
                    " doctorPos=" + doctor.Position +
                    " patientJob=" + patient.CurJob +
                    " patientBed=" + patient.CurrentBed() +
                    " medsLeft=" + medicine.stackCount +
                    " bill=" + currentBill +
                    " needingTend=" + patient.health.HasHediffsNeedingTend() + Environment.NewLine);
            }

            if (!medicineUsed || !billIncreased)
            {
                if (Find.TickManager.TicksGame - startTick > 4000)
                    throw new Exception("Doctor did not tend patient with medicine in time: doctorJob=" + doctor.CurJob + ", patientJob=" + patient.CurJob + ", medsLeft=" + medicine.stackCount + ", bill=" + currentBill);
                return;
            }

            postTendBill = currentBill;
            Check(medicineUsed, "Real medicine was not consumed during tending");
            Check(postTendBill > preTendBill, "Bill did not increase after tending procedure: " + postTendBill + " vs " + preTendBill);
            Check(desk.clinicIncome == 0, "Clinic income prematurely credited before discharge");
            Check(FloorSilver() == savedFloorSilver, "Floor silver altered before discharge");
            Check(!(bool)AccessTools.Field(data.GetType(), "Dismissed").GetValue(data), "Patient prematurely dismissed");

            // Save active patient state
            saved = true;
            GameDataSaveLoader.SaveGame("TMM-ClinicAdmission");
            Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-ClinicAdmission")), "TMM-ClinicAdmission save was not created");

            Finish(false);
        }
    }

    private void TickReload()
    {
        if (reloadInitialized) return;
        reloadInitialized = true; checks=0;

        Check(saved, "Saved flag not set on reload");
        Check(desk != null, "Clinic desk lost on reload");
        Check(wardBed != null, "Ward bed lost on reload");
        Check(colonyBed != null, "Colony bed lost on reload");
        Check(doctor != null, "Doctor lost on reload");
        Check(patient != null, "Patient lost on reload");
        Check(medicine != null, "Medicine lost on reload");

        Check(patient.Spawned && !patient.Dead, "Patient is not spawned or is dead on reload");
        Check(Clinic.ActivePatient(patient), "Patient is not active in Clinic on reload");

        var patientsDict = HospitalPatients;
        Check(patientsDict != null && patientsDict.Contains(patient), "HospitalMapComponent does not contain patient on reload");
        var data = patientsDict[patient];
        Check(!(bool)AccessTools.Field(data.GetType(), "Dismissed").GetValue(data), "Patient marked dismissed across reload");

        float loadedBill = (float)AccessTools.Field(data.GetType(), "Bill").GetValue(data);
        Check(Math.Abs(loadedBill - postTendBill) < 0.01f, "Patient bill corrupted on reload: " + loadedBill + " vs " + postTendBill);

        Check(patient.CurrentBed() == wardBed, "Patient not in ward bed on reload: " + patient.CurrentBed());
        var lord = patient.GetLord();
        Check(lord != null && lord.LordJob != null && lord.LordJob.GetType().Name == "LordJob_VisitColonyAsPatient", "Patient Lord lost on reload");

        Check(desk.clinicIncome == 0, "Clinic income prematurely credited on reload");
        Check(FloorSilver() == savedFloorSilver, "Floor silver changed on reload");

        Finish(true);
    }

    public override void MapComponentUpdate()
    {
        var args = Environment.GetCommandLineArgs();
        bool isProbe = args.Contains("-tmm-clinic-admission-probe");
        bool isReload = args.Contains("-tmm-clinic-admission-reload");
        if (reported || Find.CurrentMap != map || (!isProbe && !isReload)) return;

        try
        {
            if (isReload)
            {
                TickReload();
            }
            else
            {
                TickFresh();
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
        if (uiReported || !reported || desk == null || Event.current.type != EventType.Repaint) return;
        if (!args.Contains("-tmm-clinic-admission-probe") && !args.Contains("-tmm-clinic-admission-reload")) return;
        uiReported = true;
        string error = null;
        try
        {
            // Reception desk services dialog
            var servicesDialog = new Dialog_ComplexServices(desk) { page = 1 };
            servicesDialog.DoWindowContents(new Rect(10, 10, 740, 720));

            // Hospital map settings dialog
            var settingsType = AccessTools.TypeByName("Hospital.MainTab.Dialog_MapSettings");
            if (settingsType != null)
            {
                var settingsWindow = (Window)Activator.CreateInstance(settingsType, new object[] { map });
                settingsWindow.DoWindowContents(new Rect(10, 10, 430, 580));
            }
        }
        catch (Exception ex)
        {
            error = ex.ToString();
        }
        string outPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "ClinicAdmissionAcceptanceUiProbe.json");
        string json = "{\"passed\":" + (error == null ? "true" : "false") + ",\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Q(error)) + "}";
        File.WriteAllText(outPath, json);
    }

    private void Finish(bool reload)
    {
        reported = true;
        string fileName = reload ? "ClinicAdmissionAcceptanceReloadProbe.json" : "ClinicAdmissionAcceptanceProbe.json";
        string filePath = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
        string json = "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":" + (errors.Count == 0 ? "[]" : "[" + string.Join(",", errors.Select(Q)) + "]") + "}";
        File.WriteAllText(filePath, json);
        Log.Message("[TMM-Probe] Finished ClinicAdmissionAcceptanceProbe (reload=" + reload + "). Passed: " + (errors.Count == 0) + ", Checks: " + checks);
    }
}
