using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class Dialog_GuestDossiers : Window
    {
        private readonly Building_ComplexDesk desk;
        private Vector2 listScroll,cardScroll;
        private string search="",selected,reason="";
        private int filter;
        public Dialog_GuestDossiers(Building_ComplexDesk desk){this.desk=desk;doCloseX=true;doCloseButton=true;forcePause=true;absorbInputAroundWindow=true;if(GuestDossiers.Book!=null)GuestDossiers.Book.Synchronize();}
        public override Vector2 InitialSize {get{return new Vector2(1050,760);}}
        private static string T(string key){return ("TMM.Dossier."+key).Translate();}
        private static string Day(int tick){return "TMM.Dossier.Day".Translate((tick/60000f+1).ToString("0.#"));}
        private static string Service(string service){return T(service=="hotel"?"Hotel":service=="restaurant"?"Restaurant":service=="company"?"Company":service=="sales"?"Sales":"Clinic");}
        private static string State(GuestDossier row){return (row.profile.vip?"TMM.Profile.Vip".Translate().ToString()+" · ":"")+(row.profile.Regular?"TMM.Profile.Regular".Translate().ToString()+" · ":"")+T(row.Current!=null?(row.Current.leaving?"StateLeaving":"StatePresent"):row.blocked?"StateBlocked":"StateAway");}
        private static string FilterLabel(int f){return f<3?T(f==0?"All":f==1?"Present":"Blocked"):("TMM.Profile."+(f==3?"Vip":"Regular")).Translate().ToString();}
        public override void DoWindowContents(Rect rect)
        {
            var book=GuestDossiers.Book;if(book==null||desk==null||!desk.Spawned){Close();return;}
            Text.Font=GameFont.Medium;Widgets.Label(new Rect(0,0,rect.width,35),T("Title"));Text.Font=GameFont.Small;
            Widgets.Label(new Rect(0,40,rect.width,65),T("Help"));
            Widgets.Label(new Rect(0,110,80,30),T("Search"));search=Widgets.TextField(new Rect(85,110,250,30),search);
            if(Widgets.ButtonText(new Rect(350,110,180,30),FilterLabel(filter)))
                Find.WindowStack.Add(new FloatMenu(new[]{0,1,2,3,4}.Select(f=>new FloatMenuOption(FilterLabel(f),()=>filter=f)).ToList()));
            var rows=book.records.Where(r=>(filter!=1||r.Current!=null)&&(filter!=2||r.blocked)&&(filter!=3||r.profile.vip)&&(filter!=4||r.profile.Regular)&&
                (string.IsNullOrWhiteSpace(search)||(r.name??"").IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||(r.factionName??"").IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0))
                .OrderByDescending(r=>r.lastSeen).ThenBy(r=>r.name).ToArray();
            var list=new Rect(0,155,335,rect.height-205);Widgets.BeginScrollView(list,ref listScroll,new Rect(0,0,list.width-20,Math.Max(list.height,rows.Length*64)));
            for(int i=0;i<rows.Length;i++){
                var row=rows[i];var cell=new Rect(0,i*64,list.width-22,60);
                if(row.id==selected)Widgets.DrawHighlightSelected(cell);
                Widgets.Label(new Rect(5,cell.y+3,cell.width-10,27),row.name??row.id);
                Widgets.Label(new Rect(5,cell.y+28,cell.width-10,27),State(row));
                if(Widgets.ButtonInvisible(cell)){selected=row.id;reason=row.reason??"";cardScroll=Vector2.zero;}
            }
            if(rows.Length==0)Widgets.Label(new Rect(0,0,list.width-20,80),T("Empty"));Widgets.EndScrollView();
            var dossier=book.Find(selected);if(dossier==null){Widgets.Label(new Rect(360,165,rect.width-370,60),T("Select"));return;}
            var area=new Rect(355,155,rect.width-355,rect.height-205);
            var inner=new Rect(0,0,area.width-25,1050+dossier.visits.Count*60+dossier.payments.Count*40);
            Widgets.BeginScrollView(area,ref cardScroll,inner);float y=0;
            Action<string,float> line=(value,height)=>{Widgets.Label(new Rect(0,y,inner.width,height),value);y+=height+5;};
            line(dossier.name??dossier.id,32);line("TMM.Dossier.Faction".Translate(dossier.factionName),28);
            line("TMM.Dossier.Visits".Translate(dossier.visitCount),28);line("TMM.Dossier.FirstSeen".Translate(Day(dossier.firstSeen)),28);line("TMM.Dossier.LastSeen".Translate(Day(dossier.lastSeen)),28);
            line(State(dossier),50);
            line("TMM.Profile.Stays".Translate(dossier.profile.completed,dossier.profile.good),45);
            if(dossier.profile.lastScore>=0)line("TMM.Profile.Score".Translate((dossier.profile.lastScore*100).ToString("0")),28);
            if(dossier.profile.vip)line("TMM.Profile.Preferences".Translate(),60);
            if(dossier.pawn!=null&&dossier.pawn.Spawned&&Widgets.ButtonText(new Rect(0,y,inner.width,30),T("Focus")))CameraJumper.TryJump(dossier.pawn);y+=38;
            if(dossier.blocked){line("TMM.Dossier.Since".Translate(Day(dossier.blockedAt)),28);line(dossier.reason??"",60);if(dossier.Current!=null)line(T("CurrentVisitAllowed"),65);}
            line(T("Reason"),26);reason=Widgets.TextField(new Rect(0,y,inner.width,32),reason);if(reason.Length>200)reason=reason.Substring(0,200);y+=40;
            TooltipHandler.TipRegion(new Rect(0,y-40,inner.width,32),T("ReasonHelp"));
            if(Widgets.ButtonText(new Rect(0,y,inner.width,32),T(dossier.blocked?"Unblock":"Block")))book.SetBlocked(dossier,!dossier.blocked,reason);y+=47;
            line("TMM.Dossier.SpendTotal".Translate(dossier.PersonalTotal),30);
            line(T("Hotel")+": "+dossier.hotel,26);line(T("Restaurant")+": "+dossier.restaurant,26);line(T("Company")+": "+dossier.company,26);line(T("Sales")+": "+dossier.sales,26);line(T("Clinic")+": "+dossier.clinic,36);
            line(T("VisitsHeading"),32);
            foreach(var visit in dossier.visits.AsEnumerable().Reverse()){
                string end=visit.ended<0?T("Ongoing"):Day(visit.ended)+" · "+T(visit.endReason=="died"?"Died":visit.endReason=="lost"?"Lost":visit.endReason=="departed"?"Departed":"StateAway");
                line(visit.mapName+" · "+Day(visit.started)+" → "+end+(visit.legacy?" · "+T("Legacy"):"")+(visit.medical?" · "+T("TypeMedical"):""),42);
            }
            if(dossier.visits.Count==0)line(T("NoHistory"),30);
            line(T("PaymentsHeading"),32);
            foreach(var payment in dossier.payments.AsEnumerable().Reverse())line(Day(payment.tick)+" · "+Service(payment.service)+" · "+"TMM.Dossier.Amount".Translate(payment.amount),26);
            if(dossier.payments.Count==0)line(T("NoHistory"),30);
            Widgets.EndScrollView();
        }
    }
}
