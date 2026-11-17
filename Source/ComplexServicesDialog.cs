using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class Dialog_ComplexServices : Window
    {
        private readonly Building_ComplexDesk desk;
        private Vector2 scroll;
        public int page;
        private static readonly string[] pages={"TMM.Services.General","TMM.Services.Hotel","TMM.Services.Clinic","TMM.Services.Restaurant","TMM.Services.Staff"};
        public Dialog_ComplexServices(Building_ComplexDesk desk){this.desk=desk;doCloseX=true;doCloseButton=true;forcePause=true;absorbInputAroundWindow=true;}
        public override Vector2 InitialSize {get{return new Vector2(780,780);}}
        public override void DoWindowContents(Rect rect)
        {
            if(desk==null||!desk.Spawned){Close();return;}
            for(int i=0;i<pages.Length;i++){
                var color=GUI.color;if(page==i)GUI.color=new Color(0.65f,1f,0.7f);
                if(Widgets.ButtonText(new Rect(i*rect.width/5f,0,rect.width/5f-4,34),pages[i].Translate())){page=i;scroll=Vector2.zero;}GUI.color=color;
            }
            var body=new Rect(0,48,rect.width,rect.height-92);var inner=new Rect(0,0,body.width-25,2350);
            Widgets.BeginScrollView(body,ref scroll,inner);float y=0;
            try{
            if(page==0){
                Help(inner,ref y,"TMM.Complex.ServicesHelp");
                Command(inner,ref y,desk.AreaButton("TMM.Complex.Area",()=>desk.area,v=>{desk.area=v;desk.ApplyServiceAreas();}));
                Command(inner,ref y,desk.AreaButton("TMM.Complex.StaffArea",()=>desk.staffArea,v=>{desk.staffArea=v;desk.ApplyServiceAreas();}));
                Help(inner,ref y,"TMM.Services.AutoAreas");
                if(Button(inner,ref y,"TMM.Prices.Title"))Find.WindowStack.Add(new Dialog_ComplexPrices(desk));
                if(Button(inner,ref y,"TMM.Dossier.Title"))Find.WindowStack.Add(new Dialog_GuestDossiers(desk));
                if(Button(inner,ref y,"TMM.ServiceReadiness.Title"))Find.WindowStack.Add(new Dialog_ServiceReadiness(desk));
            }else if(page==1){
                Help(inner,ref y,"TMM.Services.HotelHelp");
                Widgets.Label(new Rect(0,y,440,32),"TMM.Complex.RoomRate".Translate(desk.roomRate));
                if(Widgets.ButtonText(new Rect(inner.width-150,y,65,32),"−5")){desk.roomRate=Math.Max(0,desk.roomRate-5);desk.ApplyServiceAreas();}
                if(Widgets.ButtonText(new Rect(inner.width-75,y,65,32),"+5")){desk.roomRate=Math.Min(10000,desk.roomRate+5);desk.ApplyServiceAreas();}y+=45;
                Help(inner,ref y,"TMM.Profile.Help");
                if(Widgets.ButtonText(new Rect(0,y,inner.width,32),"TMM.Profile.Chance".Translate(desk.vipChance)))Find.WindowStack.Add(new FloatMenu(new[]{0,1,3,5,10}.Select(p=>new FloatMenuOption(p+"%",()=>desk.vipChance=p)).ToList()));y+=42;
                Widgets.CheckboxLabeled(new Rect(0,y,inner.width,32),"TMM.Profile.PreferRegulars".Translate(),ref desk.preferRegulars);y+=42;
                Widgets.CheckboxLabeled(new Rect(0,y,inner.width,32),"TMM.Outdoor.Enabled".Translate(),ref desk.outdoorBreaks);y+=42;
                Help(inner,ref y,"TMM.Outdoor.Help");
                if(Button(inner,ref y,"TMM.Outdoor.NeedsTitle"))Find.WindowStack.Add(new Dialog_GuestNeeds(desk));
                if(ModsConfig.BiotechActive){
                    Widgets.CheckboxLabeled(new Rect(0,y,inner.width,32),"TMM.Deathrest.Enabled".Translate(),ref desk.deathrestGuests);y+=42;
                    Help(inner,ref y,"TMM.Deathrest.Help");
                    if(Button(inner,ref y,"TMM.Deathrest.Manage"))GuestDeathrest.Show(desk);
                    Widgets.CheckboxLabeled(new Rect(0,y,inner.width,32),"TMM.Hemogen.Purchases".Translate(),ref desk.hemogenPurchases);y+=42;
                    Help(inner,ref y,"TMM.Hemogen.Help");
                    Widgets.CheckboxLabeled(new Rect(0,y,inner.width,32),"TMM.GeneticSupply.Purchases".Translate(),ref desk.geneticDrugPurchases);y+=42;
                    Widgets.CheckboxLabeled(new Rect(0,y,inner.width,32),"TMM.AddictionSupply.Purchases".Translate(),ref desk.ordinaryDrugPurchases);y+=42;
                    Help(inner,ref y,"TMM.AddictionSupply.Help");
                    var policy=GuestGeneticSupply.SupplyPolicy(desk);
                    if(Widgets.ButtonText(new Rect(0,y,inner.width,32),"TMM.GeneticSupply.Policy".Translate(policy==null?"TMM.GeneticSupply.GuestPolicy".Translate().ToString():policy.label))){
                        var options=Current.Game.drugPolicyDatabase.AllPolicies.Select(p=>new FloatMenuOption(p.label,()=>desk.dependencySupplyPolicy=p)).ToList();
                        options.Insert(0,new FloatMenuOption("TMM.GeneticSupply.GuestPolicy".Translate(),()=>desk.dependencySupplyPolicy=null));Find.WindowStack.Add(new FloatMenu(options));
                    }y+=42;
                    if(Button(inner,ref y,"TMM.GeneticSupply.ManagePolicies"))Find.WindowStack.Add(new Dialog_ManageDrugPolicies(policy));
                    Help(inner,ref y,"TMM.GeneticSupply.Help");
                }
                ComplexReceptionUI.GuestOptions(desk,inner,ref y);
                if(Button(inner,ref y,"TMM.Services.GuestList"))Find.WindowStack.Add(new Dialog_ComplexRoster(desk,false));
            }else if(page==2){
                Help(inner,ref y,"TMM.Services.ClinicHelp");
                ComplexReceptionUI.ClinicOptions(desk,inner,ref y);
                if(Button(inner,ref y,"TMM.Services.PatientList"))Find.WindowStack.Add(new Dialog_ComplexRoster(desk,true));
                if(Button(inner,ref y,"TMM.Clinic.Admit"))ClinicGuests.ShowAdmission(desk);
                if(Button(inner,ref y,"TMM.Services.ClinicAdvanced")){
                    var type=AccessTools.TypeByName("Hospital.MainTab.Dialog_MapSettings");
                    if(type!=null)Find.WindowStack.Add((Window)Activator.CreateInstance(type,new object[]{desk.Map}));
                }
            }else if(page==3){
                Help(inner,ref y,"TMM.Services.RestaurantHelp");
                Widgets.CheckboxLabeled(new Rect(0,y,inner.width,32),"TMM.Restaurant.Open".Translate(),ref desk.restaurantOpen);y+=42;
                string menuSummary = desk.restaurantMenu == null || desk.restaurantMenu.Count == 0
                    ? (string)"TMM.Restaurant.MenuEmptySummary".Translate()
                    : (string)"TMM.Restaurant.MenuSelectedSummary".Translate(desk.restaurantMenu.Count);
                if(Widgets.ButtonText(new Rect(0,y,inner.width,32),"TMM.Restaurant.EditMenu".Translate() + " (" + menuSummary + ")"))
                    Find.WindowStack.Add(new Dialog_RestaurantMenu(desk));
                y+=42;
                if(Widgets.ButtonText(new Rect(0,y,inner.width,32),"TMM.Restaurant.Price".Translate(desk.restaurantPercent)))
                    Find.WindowStack.Add(new FloatMenu(new[]{0,50,75,100,125,150,200}.Select(p=>new FloatMenuOption(p+"%",()=>desk.restaurantPercent=p)).ToList()));y+=42;
            }else{
                Help(inner,ref y,"TMM.Complex.EmployeesHelp");
                if(Button(inner,ref y,"TMM.Complex.Employees"))Find.WindowStack.Add(new Dialog_ComplexEmployees(desk));
                if(Button(inner,ref y,"TMM.Buyout.Menu"))Dialog_WorkBuyout.Show(desk);
                foreach(var gizmo in CompanyMenus.Gizmos(desk))Command(inner,ref y,(Command)gizmo);
            }
            }finally{Widgets.EndScrollView();}
        }
        private static void Help(Rect rect,ref float y,string key){string text=key.Translate();float h=Text.CalcHeight(text,rect.width);Widgets.Label(new Rect(0,y,rect.width,h),text);y+=h+16;}
        private static bool Button(Rect rect,ref float y,string key){bool clicked=Widgets.ButtonText(new Rect(0,y,rect.width,36),key.Translate());y+=46;return clicked;}
        private static void Command(Rect rect,ref float y,Command command)
        {
            var toggle=command as Command_Toggle;var action=command as Command_Action;var row=new Rect(0,y,rect.width,36);
            if(toggle!=null){bool before=toggle.isActive(),after=before;Widgets.CheckboxLabeled(row,command.defaultLabel,ref after);if(before!=after)toggle.toggleAction();}
            else if(Widgets.ButtonText(row,command.defaultLabel)&&action!=null)action.action();
            TooltipHandler.TipRegion(row,command.defaultDesc);y+=46;
        }
    }
}
