using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using Verse;

public static class ReceptionNavigationFixture
{
    private static int checks;
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    public static int Setup(Building_ComplexDesk desk,Pawn guest)
    {
        checks=0;ComplexReceptionUI.HideTabs();
        Check(!DefDatabase<MainButtonDef>.GetNamed("Guests").buttonVisible&&!DefDatabase<MainButtonDef>.GetNamed("Patients").buttonVisible,"Old guest/patient bottom tabs remain visible");
        AccessTools.Method(AccessTools.TypeByName("Hospitality.Mod_Hospitality"),"ToggleTabIfNeeded").Invoke(null,null);
        Check(!DefDatabase<MainButtonDef>.GetNamed("Guests").buttonVisible,"Native settings restored the removed bottom tab");
        Check(ComplexReceptionUI.Roster(desk,false).Contains(guest),"Reception guest roster lost the native visitor");
        var hospital=Clinic.Hospital(desk.Map);
        for(int hour=0;hour<24;hour++)ComplexReceptionUI.SetHour(hospital,hour,true);
        var field=AccessTools.Field(hospital.GetType(),"openForBusiness");field.SetValue(hospital,true);
        Check((bool)AccessTools.Method(hospital.GetType(),"IsOpen").Invoke(hospital,null),"All-day reception schedule did not open real patient admission");
        int current=GenLocalDate.HourOfDay(desk.Map);ComplexReceptionUI.SetHour(hospital,current,false);
        Check(!(bool)AccessTools.Method(hospital.GetType(),"IsOpen").Invoke(hospital,null),"Closing the current hour did not affect real patient admission");
        ComplexReceptionUI.SetHour(hospital,current,true);field.SetValue(hospital,false);
        Check(!(bool)AccessTools.Method(hospital.GetType(),"IsOpen").Invoke(hospital,null),"Admission ignores its master switch");
        return checks;
    }
    public static int Reload(Building_ComplexDesk desk)
    {
        checks=0;var hospital=Clinic.Hospital(desk.Map);
        var hours=(System.Collections.Generic.List<bool>)AccessTools.Field(hospital.GetType(),"openingHours").GetValue(hospital);
        Check(hours.Count==24&&hours.All(h=>h),"Reception hour settings were not saved in the native hospital component");
        Check(!(bool)AccessTools.Field(hospital.GetType(),"openForBusiness").GetValue(hospital),"Opening schedule implicitly enabled admission after loading");
        Check(!DefDatabase<MainButtonDef>.GetNamed("Guests").buttonVisible&&!DefDatabase<MainButtonDef>.GetNamed("Patients").buttonVisible,"Removed bottom tabs returned after loading");
        return checks;
    }
}
