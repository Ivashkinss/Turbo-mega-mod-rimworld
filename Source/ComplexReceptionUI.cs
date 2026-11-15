using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class ComplexReceptionUI
    {
        static ComplexReceptionUI()
        {
            var method=AccessTools.Method(AccessTools.TypeByName("Hospitality.Mod_Hospitality"),"ToggleTabIfNeeded");
            if(method!=null)new Harmony("turbomegamod.reception.navigation").Patch(method,postfix:new HarmonyMethod(typeof(ComplexReceptionUI),"HideTabs"));
            LongEventHandler.ExecuteWhenFinished(HideTabs);
        }
        public static void HideTabs()
        {
            foreach(string name in new[]{"Guests","Patients"}){
                var def=DefDatabase<MainButtonDef>.GetNamedSilentFail(name);
                if(def!=null&&def.tabWindowClass!=null&&(def.tabWindowClass.FullName.StartsWith("Hospitality.")||def.tabWindowClass.FullName.StartsWith("Hospital.")))def.buttonVisible=false;
            }
        }
        private static object Guests(Map map){return map.components.FirstOrDefault(c=>c.GetType().FullName=="Hospitality.Hospitality_MapComponent");}
        private static void Toggle(object target,string field,string label,string help,Rect rect,ref float y)
        {
            if(target==null)return;var member=AccessTools.Field(target.GetType(),field);if(member==null)return;
            bool value=(bool)member.GetValue(target);var row=new Rect(0,y,rect.width,32);
            Widgets.CheckboxLabeled(row,label.Translate(),ref value);member.SetValue(target,value);
            if(help!=null)TooltipHandler.TipRegion(row,help.Translate());y+=38;
        }
        public static void GuestOptions(Building_ComplexDesk desk,Rect rect,ref float y)
        {
            var comp=Guests(desk.Map);
            Toggle(comp,"guestsAreWelcome","GuestsWelcome","GuestsWelcomeTooltip",rect,ref y);
            Toggle(comp,"refuseGuestsUntilWeHaveBeds","RefuseGuestsUntilBeds","RefuseGuestsUntilBedsTooltip",rect,ref y);
            Toggle(comp,"askForSafety","GuestsAskForSafety","GuestsAskForSafetyTooltip",rect,ref y);
            Toggle(comp,"guestsCanTakeFoodForFree","GuestsCanTakeFoodForFree","GuestsCanTakeFoodForFreeTooltip",rect,ref y);
            Toggle(comp,"defaultEntertain","ImproveRelationship",null,rect,ref y);
            Toggle(comp,"defaultMakeFriends","MakeFriends",null,rect,ref y);
            int next=int.MaxValue;
            var lookup=comp==null?null:AccessTools.Method(comp.GetType(),"GetNextVisit");
            if(lookup!=null)foreach(var faction in Find.FactionManager.AllFactionsListForReading){
                var visit=lookup.Invoke(comp,new object[]{faction});if(visit==null)continue;
                int due=(int)AccessTools.Property(visit.GetType(),"FireTick").GetValue(visit,null);next=Math.Min(next,due);
            }
            string timing=next==int.MaxValue?"TMM.Services.NoGuestVisit".Translate().ToString():"TMM.Services.NextGuest".Translate((Math.Max(0,next-Find.TickManager.TicksGame)/2500f).ToString("F1")).ToString();
            float height=Text.CalcHeight(timing,rect.width);Widgets.Label(new Rect(0,y,rect.width,height),timing);y+=height+15;
        }
        public static void SetHour(object hospital,int hour,bool open)
        {
            var field=AccessTools.Field(hospital.GetType(),"openingHours");var hours=field.GetValue(hospital) as List<bool>;
            if(hours==null){hours=Enumerable.Repeat(false,24).ToList();field.SetValue(hospital,hours);}
            while(hours.Count<24)hours.Add(false);hours[hour]=open;
        }
        public static void ClinicOptions(Building_ComplexDesk desk,Rect rect,ref float y)
        {
            var hospital=Clinic.Hospital(desk.Map);if(hospital==null)return;
            Toggle(hospital,"openForBusiness","AcceptPatients","AcceptPatientsTooltip",rect,ref y);
            bool surgery=desk.surgeryAllowed&&(bool)AccessTools.Field(hospital.GetType(),"AcceptSurgery").GetValue(hospital),before=surgery;
            Widgets.CheckboxLabeled(new Rect(0,y,rect.width,32),"TMM.Complex.Surgery".Translate(),ref surgery);y+=38;
            if(before!=surgery){desk.surgeryAllowed=surgery;AccessTools.Field(hospital.GetType(),"AcceptSurgery").SetValue(hospital,surgery);}
            bool open=(bool)AccessTools.Method(hospital.GetType(),"IsOpen").Invoke(hospital,null);
            int beds=(int)AccessTools.Method(hospital.GetType(),"FreeMedicalBeds").Invoke(hospital,null);
            string status="TMM.Services.ClinicStatus".Translate((open?"TMM.Services.OpenNow":"TMM.Services.ClosedNow").Translate(),Math.Max(0,beds));
            Widgets.Label(new Rect(0,y,rect.width,32),status);y+=42;
            Widgets.Label(new Rect(0,y,rect.width,40),"TMM.Services.HoursHelp".Translate());y+=48;
            var hours=AccessTools.Field(hospital.GetType(),"openingHours").GetValue(hospital) as List<bool>;
            for(int hour=0;hour<24;hour++){
                bool enabled=hours!=null&&hours.Count>hour&&hours[hour];var color=GUI.color;
                GUI.color=enabled?new Color(0.55f,1f,0.65f):Color.gray;
                var cell=new Rect((hour%12)*rect.width/12f,y+(hour/12)*40,rect.width/12f-3,32);
                if(Widgets.ButtonText(cell,hour.ToString("00")+":00"))SetHour(hospital,hour,!enabled);
                GUI.color=color;TooltipHandler.TipRegion(cell,"TMM.Services.HourTip".Translate(hour.ToString("00"),((hour+1)%24).ToString("00")));
            }
            y+=88;
            if(Widgets.ButtonText(new Rect(0,y,rect.width,32),"TMM.Services.AllDay".Translate()))for(int h=0;h<24;h++)SetHour(hospital,h,true);y+=44;
        }
        public static IEnumerable<Pawn> Roster(Building_ComplexDesk desk,bool patients)
        {
            if(desk==null||!desk.Spawned)return Enumerable.Empty<Pawn>();
            if(patients){var registry=Clinic.Patients(desk.Map);return registry==null?Enumerable.Empty<Pawn>():registry.Keys.Cast<Pawn>().Where(p=>p!=null&&Clinic.ActivePatient(p));}
            var comp=Guests(desk.Map);if(comp==null)return Enumerable.Empty<Pawn>();
            AccessTools.Method(comp.GetType(),"RefreshGuestListTotal").Invoke(comp,null);
            return (IEnumerable<Pawn>)AccessTools.Property(comp.GetType(),"PresentGuests").GetValue(comp,null);
        }
    }
    public sealed class Dialog_ComplexRoster : Window
    {
        private readonly Building_ComplexDesk desk;private readonly bool patients;private PawnTable table;
        public Dialog_ComplexRoster(Building_ComplexDesk desk,bool patients){this.desk=desk;this.patients=patients;forcePause=true;doCloseX=true;doCloseButton=true;absorbInputAroundWindow=true;}
        public override Vector2 InitialSize {get{return new Vector2(1100,700);}}
        public override void DoWindowContents(Rect rect)
        {
            if(desk==null||!desk.Spawned){Close();return;}
            if(table==null){
                var source=DefDatabase<PawnTableDef>.GetNamed(patients?"Patients":"Guests");
                var def=(PawnTableDef)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(source,null);
                def.columns=source.columns.Where(c=>patients||(c.defName!="AccommodationArea"&&c.defName!="ShoppingArea")).ToList();
                table=(PawnTable)Activator.CreateInstance(def.workerClass,new object[]{def,new Func<IEnumerable<Pawn>>(()=>ComplexReceptionUI.Roster(desk,patients)),(int)rect.width,(int)rect.height-50});
            }
            table.PawnTableOnGUI(Vector2.zero);
        }
    }
}
