using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class Dialog_GuestNeeds : Window
    {
        private readonly Building_ComplexDesk desk;
        private Vector2 scroll;
        private readonly Dictionary<Pawn,string> outdoor=new Dictionary<Pawn,string>();
        public Dialog_GuestNeeds(Building_ComplexDesk desk){this.desk=desk;forcePause=true;doCloseX=true;doCloseButton=true;absorbInputAroundWindow=true;}
        public override Vector2 InitialSize {get{return new Vector2(920,760);}}
        public override void DoWindowContents(Rect rect)
        {
            if(desk==null||!desk.Spawned){Close();return;}
            Text.Font=GameFont.Medium;Widgets.Label(new Rect(0,0,rect.width,36),"TMM.Outdoor.NeedsTitle".Translate());Text.Font=GameFont.Small;
            Widgets.Label(new Rect(0,42,rect.width,90),"TMM.Outdoor.NeedsHelp".Translate());
            var pawns=desk.Map.mapPawns.AllPawnsSpawned.Where(GuestDossiers.Arrived).Concat(desk.Map.GetComponent<ClinicGuests>().visits.Where(v=>v.pawn!=null&&v.pawn.MapHeld==desk.Map).Select(v=>v.pawn)).Distinct().Where(p=>p.needs!=null).OrderBy(p=>p.LabelShortCap.ToString()).ToList();
            var view=new Rect(0,140,rect.width,rect.height-185);var content=new Rect(0,0,view.width-20,Math.Max(view.height,pawns.Count*255));Widgets.BeginScrollView(view,ref scroll,content);
            for(int i=0;i<pawns.Count;i++){
                var p=pawns[i];float y=i*255;Widgets.Label(new Rect(0,y,content.width-125,27),p.LabelShortCap);
                if(Widgets.ButtonText(new Rect(content.width-115,y,110,27),"TMM.Dossier.Focus".Translate()))CameraJumper.TryJump(p);
                string levels=string.Join(" · ",p.needs.AllNeeds.Where(n=>n.def.showOnNeedList).OrderByDescending(n=>n.def.listPriority).Select(n=>n.def.LabelCap+": "+(n.CurLevelPercentage*100).ToString("0")+"%"));
                Widgets.Label(new Rect(0,y+32,content.width,65),levels);
                if(!outdoor.ContainsKey(p))outdoor[p]=GuestOutdoor.Status(p);Widgets.Label(new Rect(0,y+100,content.width,50),outdoor[p]);
                Widgets.Label(new Rect(0,y+151,content.width,48),GuestDeathrest.Status(p));
                Widgets.Label(new Rect(0,y+201,content.width,48),GuestHemogen.Status(p));
            }
            if(pawns.Count==0)Widgets.Label(new Rect(0,0,content.width,60),"TMM.Outdoor.NoGuests".Translate());Widgets.EndScrollView();
        }
    }
}
