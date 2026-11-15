using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class ClinicReloadLaunch
{
    static ClinicReloadLaunch() { if (Environment.GetCommandLineArgs().Contains("-tmm-clinic-reload")) LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Clinic")); }
}

public sealed class ClinicProbe : MapComponent
{
    private MedicalAttributionFixture medical;
    private bool reported, uiReported;
    private int checks;
    private readonly List<string> errors = new List<string>();
    private Mod clinicMod;
    private Building_ComplexDesk savedDesk;
    private Pawn savedPatient;
    private bool saved;
    private int savedFloor;
    public ClinicProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        Scribe_Deep.Look(ref medical,"medicalAttribution");
        base.ExposeData(); Scribe_References.Look(ref savedDesk,"clinicDesk"); Scribe_References.Look(ref savedPatient,"clinicPatient");
        Scribe_Values.Look(ref saved,"clinicSaved"); Scribe_Values.Look(ref savedFloor,"clinicFloor");
    }
    private int FloorSilver() { return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(t=>t.Spawned).Sum(t=>t.stackCount); }
    private object Register(Pawn pawn, int tick, float bill)
    {
        var type=AccessTools.TypeByName("Hospital.PatientData"); var kind=AccessTools.TypeByName("Hospital.PatientType");
        var data=Activator.CreateInstance(type,new object[]{tick,pawn.MarketValue,0.5f,Enum.ToObject(kind,1)});
        AccessTools.Field(type,"Bill").SetValue(data,bill); AccessTools.Method(Clinic.HospitalType,"PatientArrived").Invoke(Clinic.Hospital(map),new[]{(object)pawn,data}); return data;
    }
    private void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private Building_Bed Bed(IntVec3 cell)
    {
        foreach (var thing in cell.GetThingList(map).ToArray()) if (thing is Building || thing is Plant) thing.Destroy();
        var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.Steel); bed.SetFaction(Faction.OfPlayer); GenSpawn.Spawn(bed, cell, map); bed.Medical = true; return bed;
    }
    private ThingComp Comp(Building_Bed bed) { return bed.AllComps.Single(c => Clinic.BedType.IsInstanceOfType(c)); }
    private bool Flag(Building_Bed bed, string name = "Hospital") { return (bool)AccessTools.Property(Clinic.BedType, name).GetValue(Comp(bed), null); }
    public override void MapComponentUpdate()
    {
        bool reload=Environment.GetCommandLineArgs().Contains("-tmm-clinic-reload");
        if (reported || Find.CurrentMap != map || (!reload && !Environment.GetCommandLineArgs().Contains("-tmm-clinic-probe"))) return;
        try {
            if(reload){
                medical.BeforeReloadPlacement(map,Check);
                Check(saved && savedDesk!=null && savedPatient!=null,"Saved clinic references missing");
                var ledger=map.GetComponent<ClinicAccounts>();
                int pending=ledger.receipts.Sum(r=>r.GetDirectlyHeldThings().Sum(t=>t.stackCount));
                Check(pending+FloorSilver()==savedFloor+35+medical.coins-1,"Saved physical payment was lost or duplicated on load");
                Check(ledger.settled.Count==4 && savedDesk.clinicIncome==99+35+medical.coins-pending,"Settlement flags or income lost");
                Check((bool)AccessTools.Field(Clinic.Data(savedPatient).GetType(),"Dismissed").GetValue(Clinic.Data(savedPatient)),"Native discharge flag lost");
                ledger.PlaceReceipts(); Check(FloorSilver()==savedFloor+35+medical.coins-1 && savedDesk.clinicIncome==134+medical.coins,"Saved escrow did not reach reception exactly once");
                ledger.PlaceReceipts(); AccessTools.Method(Clinic.HospitalType,"PatientLeaves").Invoke(Clinic.Hospital(map),new object[]{savedPatient});
                Check(FloorSilver()==savedFloor+35+medical.coins-1 && savedDesk.clinicIncome==134+medical.coins && ledger.receipts.Count==0,"Reload repeated a completed payout");
                medical.AfterReloadPlacement(map,Check);
                Finish(true);return;
            }
            Check(Clinic.Enabled, "Clinic adapter did not load");
            clinicMod = LoadedModManager.ModHandles.Single(m => m.GetType().FullName == "Hospital.HospitalMod");
            var assembly = clinicMod.GetType().Assembly;
            Check(assembly.Location.Replace('\\','/').Contains("Components/Hospital/1.7/Assemblies/"), "Wrong clinic runtime version");
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "Hospital") == 1, "Duplicate Hospital assembly");
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "0MultiplayerAPI") == 1, "Duplicate Multiplayer API provider");
            Check(Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m).Owners.Contains("Adamas.Hospital")) >= 15, "Native clinic patches not installed");
            var hospital = Clinic.Hospital(map); Check(hospital != null, "Clinic map component missing");
            foreach (var cell in new CellRect(40,40,12,12)) {
                foreach (var thing in cell.GetThingList(map).ToArray()) if(thing is Building || thing is Plant)thing.Destroy();
                map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);map.areaManager.Home[cell]=true;
            }
            var room=new CellRect(40,40,12,12);
            foreach(var cell in room.EdgeCells){var wall=ThingMaker.MakeThing(cell==new IntVec3(44,0,40)?ThingDefOf.Door:ThingDefOf.Wall,ThingDefOf.Steel);wall.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(wall,cell,map);}
            foreach(var cell in room.ContractedBy(1))map.roofGrid.SetRoof(cell,RoofDefOf.RoofConstructed);
            var colonyBed = Bed(new IntVec3(65,0,45)); var wardBed = Bed(new IntVec3(45,0,45));
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();wardBed.Position.GetRoom(map).Temperature=21f;
            Check(!Flag(colonyBed) && !Flag(wardBed), "Clinic claimed medical beds before zoning");
            var desk = (Building_ComplexDesk)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TMM_ComplexDesk")); desk.automaticServices = false; desk.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(desk, new IntVec3(42,0,48), map);
            Area_Allowed complex, ward; map.areaManager.TryMakeNewAllowed(out complex); map.areaManager.TryMakeNewAllowed(out ward);
            foreach (var cell in new CellRect(40,40,12,12)) complex[cell]=true;
            ward[wardBed.Position]=true; desk.area=complex; desk.clinicArea=ward;
            Check(Flag(wardBed) && !Flag(colonyBed), "Clinical area did not isolate colony medical beds");
            Check((int)AccessTools.Method(Clinic.HospitalType,"BedCount").Invoke(hospital,null)==1, "Wrong ward bed count");
            AccessTools.Property(Clinic.BedType,"Surgery").SetValue(Comp(colonyBed),true,null);
            Check(!Flag(colonyBed,"Surgery"), "Outside bed bypassed area through surgery mode");
            desk.clinicArea=null;
            AccessTools.Field(Clinic.HospitalType,"openForBusiness").SetValue(hospital,true);
            AccessTools.Field(Clinic.HospitalType,"openingHours").SetValue(hospital,Enumerable.Repeat(true,24).ToList());
            Check(!(bool)AccessTools.Method(Clinic.HospitalType,"IsOpen").Invoke(hospital,null), "Unconfigured clinic accepts arrivals");
            desk.clinicArea=ward;
            Check((bool)AccessTools.Method(Clinic.HospitalType,"IsOpen").Invoke(hospital,null), "Configured clinic cannot open");
            Check(clinicMod.SettingsCategory()=="TMM.Clinic.Settings".Translate().ToString(), "Clinic settings category untranslated");
            var faction=Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer));
            var patient=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,faction); GenSpawn.Spawn(patient,new IntVec3(45,0,44),map); patient.guest.SetGuestStatus(Faction.OfPlayer);
            var wound=HediffMaker.MakeHediff(HediffDefOf.Cut,patient,patient.RaceProps.body.corePart);wound.Severity=2f;patient.health.AddHediff(wound);
            var data=Register(patient,Find.TickManager.TicksGame,99.9f);
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            var finder=AccessTools.Method(typeof(RestUtility),"FindBedFor",new[]{typeof(Pawn),typeof(Pawn),typeof(bool),typeof(bool),typeof(GuestStatus?)});
            var found=finder.Invoke(null,new object[]{patient,patient,false,false,null}) as Building_Bed;
            Check(found==wardBed,"Registered patient did not select clinical bed: found="+found+" active="+Clinic.ActivePatient(patient)+" ward="+Flag(wardBed)+" valid="+RestUtility.IsValidBedFor(wardBed,patient,patient,false)+" inbed="+patient.InBed());
            desk.clinicArea=null; found=finder.Invoke(null,new object[]{patient,patient,false,false,null}) as Building_Bed;
            Check(found==null,"Native bed finder fell back to colony medical bed"); desk.clinicArea=ward;
            int floor=FloorSilver(); var leave=AccessTools.Method(Clinic.HospitalType,"PatientLeaves");
            leave.Invoke(hospital,new object[]{patient});
            Check(FloorSilver()==floor+99 && desk.clinicIncome==99,"Fractional bill did not place exactly99 physical silver");
            Check((bool)AccessTools.Field(data.GetType(),"Dismissed").GetValue(data),"Discharge did not mark native patient settled");
            leave.Invoke(hospital,new object[]{patient});Check(FloorSilver()==floor+99 && desk.clinicIncome==99,"Repeated discharge duplicated payment");
            AccessTools.Method(Clinic.HospitalType,"PatientLeftTheMap").Invoke(hospital,new object[]{patient});
            Register(patient,Find.TickManager.TicksGame+1,0.99f); leave.Invoke(hospital,new object[]{patient});
            Check(FloorSilver()==floor+99 && desk.clinicIncome==99,"Sub-coin bill spawned a zero stack or extra money");
            medical=new MedicalAttributionFixture();medical.Prepare(map,desk,Register,Check);
            var accounts=map.GetComponent<ClinicAccounts>();accounts.Queue("probe-saved-receipt",desk.Position,35);
            savedDesk=desk;savedPatient=patient;savedFloor=FloorSilver();saved=true;GameDataSaveLoader.SaveGame("TMM-Clinic");
            Check(File.Exists(GenFilePaths.FilePathForSavedGame("TMM-Clinic")),"Clinic save was not created");
        } catch(Exception ex) { errors.Add(ex.ToString()); }
        Finish(reload);
    }
    private void Finish(bool reload)
    {
        reported=true;
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,reload?"ClinicReloadProbe.json":"ClinicProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");
    }
    public override void MapComponentOnGUI()
    {
        if(!reported||uiReported||Event.current.type!=EventType.Repaint||!Environment.GetCommandLineArgs().Contains("-tmm-clinic-probe"))return;
        uiReported=true;string error=null;GUI.BeginGroup(new Rect(10,10,1100,750));
        try { clinicMod.DoSettingsWindowContents(new Rect(0,0,1050,690)); var window=(Window)Activator.CreateInstance(AccessTools.TypeByName("Hospital.MainTab.Dialog_MapSettings"),new object[]{map});window.DoWindowContents(new Rect(0,0,430,580)); } catch(Exception ex){error=ex.ToString();}finally{GUI.EndGroup();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"ClinicUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
}
