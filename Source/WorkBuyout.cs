using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    // Accounting for ordinary, non-intimate service work. This does not
    // implement faction negotiations, quest recruitment or betrayal raids.
    public sealed class WorkBuyoutEntry : IExposable
    {
        public Pawn pawn;
        public int target = 10000, credited, halfRemainder;
        public bool active, released;
        public List<string> receipts = new List<string>();
        public int Remaining { get { return Math.Max(0, target - credited); } }
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn,"pawn"); Scribe_Values.Look(ref target,"target",10000);
            Scribe_Values.Look(ref credited,"credited"); Scribe_Values.Look(ref halfRemainder,"halfRemainder");
            Scribe_Values.Look(ref active,"active"); Scribe_Values.Look(ref released,"released");
            Scribe_Collections.Look(ref receipts,"receipts",LookMode.Value);
            if(Scribe.mode==LoadSaveMode.PostLoadInit&&receipts==null)receipts=new List<string>();
        }
    }
    public sealed class WorkBuyouts : GameComponent
    {
        public List<WorkBuyoutEntry> entries = new List<WorkBuyoutEntry>();
        public WorkBuyouts(Game game) { }
        public override void ExposeData()
        {
            base.ExposeData(); Scribe_Collections.Look(ref entries,"TMM_workBuyouts",LookMode.Deep);
            if(Scribe.mode==LoadSaveMode.PostLoadInit&&entries==null)entries=new List<WorkBuyoutEntry>();
        }
        public WorkBuyoutEntry Entry(Pawn pawn) { return entries.FirstOrDefault(e=>e.pawn==pawn); }
        public WorkBuyoutEntry Begin(Pawn pawn,int target)
        {
            if(pawn==null||!pawn.IsSlaveOfColony||target<1)return null;
            var entry=Entry(pawn);
            if(entry!=null)return entry;
            entry=new WorkBuyoutEntry{pawn=pawn,target=target,active=true};entries.Add(entry);return entry;
        }
        public void Credit(Pawn pawn,string receipt,int amount,bool consumables)
        {
            var entry=Entry(pawn);
            if(entry==null||!entry.active||entry.released||amount<=0||entry.receipts.Contains(receipt))return;
            entry.receipts.Add(receipt);
            long units=consumables?(long)amount+entry.halfRemainder:(long)amount*2+entry.halfRemainder;
            int credit=(int)Math.Min(entry.Remaining,units/2);
            entry.credited+=credit;entry.halfRemainder=entry.Remaining==0?0:(int)(units%2);
        }
        public bool Release(Pawn pawn)
        {
            var entry=Entry(pawn);
            if(entry==null||!entry.active||entry.released||entry.Remaining!=0||!pawn.IsSlaveOfColony)return false;
            GenGuest.SlaveRelease(pawn);entry.released=true;entry.active=false;return true;
        }
        public string Status(Pawn pawn)
        {
            var entry=Entry(pawn);
            return entry==null?"TMM.Buyout.None".Translate().ToString():"TMM.Buyout.Progress".Translate(entry.credited,entry.target,entry.Remaining).ToString();
        }
    }
    public sealed class Dialog_WorkBuyout : Window
    {
        private readonly Pawn pawn;
        private int target=10000;
        public Dialog_WorkBuyout(Pawn pawn){this.pawn=pawn;forcePause=true;doCloseX=true;doCloseButton=true;absorbInputAroundWindow=true;}
        public override Vector2 InitialSize {get{return new Vector2(650,420);}}
        public override void DoWindowContents(Rect rect)
        {
            if(pawn==null||pawn.Dead){Close();return;}
            var book=Current.Game.GetComponent<WorkBuyouts>();var entry=book.Entry(pawn);
            string help="TMM.Buyout.Help".Translate(pawn.LabelShortCap);float h=Text.CalcHeight(help,rect.width);
            Widgets.Label(new Rect(0,0,rect.width,h),help);
            float y=h+15;
            Widgets.Label(new Rect(0,y,rect.width,36),book.Status(pawn));y+=45;
            if(entry==null){
                Widgets.Label(new Rect(0,y,300,32),"TMM.Buyout.Target".Translate(target));
                if(Widgets.ButtonText(new Rect(320,y,90,32),"−1000"))target=Math.Max(1000,target-1000);
                if(Widgets.ButtonText(new Rect(420,y,90,32),"+1000"))target=Math.Min(1000000,target+1000);y+=45;
                if(Widgets.ButtonText(new Rect(0,y,rect.width,36),"TMM.Buyout.Begin".Translate())){book.Begin(pawn,target);}
            }else if(entry.active&&entry.Remaining==0){
                if(Widgets.ButtonText(new Rect(0,y,rect.width,36),"TMM.Buyout.Release".Translate())){book.Release(pawn);Close();}
            }
        }
        public static void Show(Building_ComplexDesk desk)
        {
            var book=Current.Game.GetComponent<WorkBuyouts>();
            var pawns=desk.Map.mapPawns.AllPawnsSpawned.Where(p=>p.IsSlaveOfColony).Concat(book.entries.Where(e=>e.active&&e.pawn!=null&&e.pawn.MapHeld==desk.Map).Select(e=>e.pawn)).Distinct().ToArray();
            if(pawns.Length==0){Messages.Message("TMM.Buyout.NoWorkers".Translate(),MessageTypeDefOf.NeutralEvent,false);return;}
            Find.WindowStack.Add(new FloatMenu(pawns.Select(p=>new FloatMenuOption(p.LabelShortCap+": "+book.Status(p),()=>Find.WindowStack.Add(new Dialog_WorkBuyout(p)))).ToList()));
        }
    }
}
