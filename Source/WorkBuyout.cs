using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    // Accounting for ordinary, non-intimate service work. This does not
    // implement quest recruitment or betrayal raids. Faction negotiations use FactionRansoms.
    public sealed class WorkBuyoutEntry : IExposable
    {
        public Pawn pawn;
        public int target = 10000, credited, halfRemainder;
        public bool active, released;
        public bool contractOffered,freeContract,joinRequested,joinedPermanently;
        public Faction originalFaction;
        public List<string> receipts = new List<string>();
        public int Remaining { get { return Math.Max(0, target - credited); } }
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn,"pawn"); Scribe_Values.Look(ref target,"target",10000);
            Scribe_Values.Look(ref credited,"credited"); Scribe_Values.Look(ref halfRemainder,"halfRemainder");
            Scribe_Values.Look(ref active,"active"); Scribe_Values.Look(ref released,"released");
            Scribe_Values.Look(ref contractOffered,"contractOffered");Scribe_Values.Look(ref freeContract,"freeContract");Scribe_Values.Look(ref joinRequested,"joinRequested");Scribe_Values.Look(ref joinedPermanently,"joinedPermanently");Scribe_References.Look(ref originalFaction,"originalFaction");
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
            if(pawn==null||!pawn.IsSlaveOfColony||target<1||FactionRansoms.HasAgreedRansom(pawn))return null;
            var entry=Entry(pawn);
            if(entry!=null)return entry;
            entry=new WorkBuyoutEntry{pawn=pawn,target=target,active=true};entries.Add(entry);return entry;
        }
        public void Credit(Pawn pawn,string receipt,int amount,bool consumables)
        {
            var entry=Entry(pawn);
            if(entry==null||!entry.active||entry.released||amount<=0||entry.receipts.Contains(receipt)||(entry.freeContract&&!WorkContracts.Working(entry))||FactionRansoms.HasAgreedRansom(pawn))return;
            entry.receipts.Add(receipt);
            long units=consumables?(long)amount+entry.halfRemainder:(long)amount*2+entry.halfRemainder;
            int credit=(int)Math.Min(entry.Remaining,units/2);
            entry.credited+=credit;entry.halfRemainder=entry.Remaining==0?0:(int)(units%2);
        }
        public bool Release(Pawn pawn)
        {
            var entry=Entry(pawn);
            if(entry!=null&&entry.freeContract)return WorkContracts.Complete(entry);
            if(FactionRansoms.HasAgreedRansom(pawn))return false;
            if(entry==null||!entry.active||entry.released||entry.Remaining!=0||!pawn.IsSlaveOfColony)return false;
            GenGuest.SlaveRelease(pawn);entry.released=true;entry.active=false;return true;
        }
        public string Status(Pawn pawn)
        {
            var entry=Entry(pawn);
            var ransoms=Current.Game.GetComponent<FactionRansoms>();
            string rStatus=ransoms==null?null:ransoms.Status(pawn);
            if(entry==null)return rStatus??"TMM.Buyout.None".Translate();
            if(entry.joinedPermanently)return "TMM.Contract.Permanent".Translate()+(rStatus!=null?"\n"+rStatus:"");
            string status="TMM.Buyout.Progress".Translate(entry.credited,entry.target,entry.Remaining);
            if(entry.freeContract)status+="\n"+(entry.released?"TMM.Contract.Completed":"TMM.Contract.FreeWorker").Translate();
            else if(entry.joinRequested)status+="\n"+"TMM.Contract.JoinPending".Translate();
            if(rStatus!=null)status+="\n"+rStatus;
            return status;
        }
    }
    public sealed class Dialog_WorkBuyout : Window
    {
        private readonly Pawn pawn;
        private int target=10000;
        private Vector2 scroll;
        public Dialog_WorkBuyout(Pawn pawn){this.pawn=pawn;forcePause=true;doCloseX=true;doCloseButton=true;absorbInputAroundWindow=true;}
        public override Vector2 InitialSize {get{return new Vector2(700,650);}}
        public override void DoWindowContents(Rect rect)
        {
            if(pawn==null||pawn.Dead){Close();return;}
            var book=Current.Game.GetComponent<WorkBuyouts>();var entry=book.Entry(pawn);
            var ransoms=Current.Game.GetComponent<FactionRansoms>();var rEntry=ransoms==null?null:ransoms.Entry(pawn);
            var view=new Rect(0,0,rect.width-18,1100);Widgets.BeginScrollView(rect,ref scroll,view);try{rect=view;
            string help="TMM.Buyout.Help".Translate(pawn.LabelShortCap)+"\n"+"TMM.Contract.Help".Translate();float h=Text.CalcHeight(help,rect.width);
            Widgets.Label(new Rect(0,0,rect.width,h),help);
            float y=h+15;
            string state=book.Status(pawn);float stateHeight=Text.CalcHeight(state,rect.width);Widgets.Label(new Rect(0,y,rect.width,stateHeight),state);y+=stateHeight+12;
            if(entry!=null&&pawn.MapHeld!=null){var text=pawn.MapHeld.GetComponent<HousekeepingLedger>().Status(pawn);float height=Text.CalcHeight(text,rect.width);Widgets.Label(new Rect(0,y,rect.width,height),text);TooltipHandler.TipRegion(new Rect(0,y,rect.width,height),"TMM.Housekeeping.Help".Translate());y+=height+12;}

            // Ordered UI flow: Faction ransom offer comes first.
            // Only after faction refusal is the ordinary labour contract offer displayed.
            if(pawn.IsSlaveOfColony&&(entry==null||!entry.freeContract)){
                if(rEntry==null||!rEntry.offered){
                    int offerAmount=entry!=null?entry.target:target;
                    if(entry==null){
                        Widgets.Label(new Rect(0,y,300,32),"TMM.Buyout.Target".Translate(target));
                        if(Widgets.ButtonText(new Rect(320,y,90,32),"−1000"))target=Math.Max(1000,target-1000);
                        if(Widgets.ButtonText(new Rect(420,y,90,32),"+1000"))target=Math.Min(1000000,target+1000);y+=45;
                    }
                    int currentChance=FactionRansoms.AcceptanceChanceFor(pawn);
                    float btnW=rect.width-150;
                    if(Widgets.ButtonText(new Rect(0,y,btnW,36),"TMM.Ransom.OfferWithChance".Translate(offerAmount,currentChance))){FactionRansoms.Offer(pawn,offerAmount);}
                    TooltipHandler.TipRegion(new Rect(0,y,btnW,36),"TMM.Ransom.OfferTipDynamic".Translate(currentChance));
                    if(Widgets.ButtonText(new Rect(btnW+10,y,140,36),"TMM.RansomPolicy.SettingsButton".Translate()))Find.WindowStack.Add(new Dialog_FactionRansomPolicy());
                    y+=45;
                }else if(!rEntry.agreed&&!rEntry.released){
                    // Faction refused: labour contract now available, target permanently locked from refused offer
                    if(Widgets.ButtonText(new Rect(0,y,rect.width,36),"TMM.Contract.Offer".Translate())){WorkContracts.Offer(pawn,rEntry.targetAmount);}
                    y+=45;
                }
            }

            if(entry!=null&&entry.joinRequested&&!entry.joinedPermanently&&pawn.IsSlaveOfColony){
                if(Widgets.ButtonText(new Rect(0,y,rect.width,36),"TMM.Contract.AcceptPermanent".Translate()))WorkContracts.AcceptPermanent(pawn);
                y+=45;
            }else if(entry!=null&&entry.active&&entry.Remaining==0){
                if(Widgets.ButtonText(new Rect(0,y,rect.width,36),"TMM.Buyout.Release".Translate())){if(book.Release(pawn))Close();else Messages.Message("TMM.Contract.CannotLeave".Translate(),pawn,MessageTypeDefOf.RejectInput,false);}
                y+=45;
            }

            if(rEntry!=null&&rEntry.agreed&&!rEntry.released){
                if(rEntry.CanDeceive){
                    if(Widgets.ButtonText(new Rect(0,y,rect.width,36),"TMM.Ransom.Deceive".Translate())){FactionRansoms.Deceive(pawn);}
                    TooltipHandler.TipRegion(new Rect(0,y,rect.width,36),"TMM.Ransom.DeceiveTip".Translate());
                    y+=45;
                }
                if(rEntry.CanRelease){
                    if(Widgets.ButtonText(new Rect(0,y,rect.width,36),"TMM.Ransom.Release".Translate())){if(FactionRansoms.Release(pawn))Close();else Messages.Message("TMM.Contract.CannotLeave".Translate(),pawn,MessageTypeDefOf.RejectInput,false);}
                    TooltipHandler.TipRegion(new Rect(0,y,rect.width,36),"TMM.Ransom.ReleaseTip".Translate());
                    y+=45;
                }
            }
            }finally{Widgets.EndScrollView();}
        }
        public static void Show(Building_ComplexDesk desk)
        {
            var book=Current.Game.GetComponent<WorkBuyouts>();
            var pawns=desk.Map.mapPawns.AllPawnsSpawned.Where(p=>p.IsSlaveOfColony).Concat(book.entries.Where(e=>e.active&&e.pawn!=null&&e.pawn.MapHeld==desk.Map).Select(e=>e.pawn)).Distinct().ToArray();
            var options=pawns.Select(p=>new FloatMenuOption(p.LabelShortCap+": "+book.Status(p),()=>Find.WindowStack.Add(new Dialog_WorkBuyout(p)))).ToList();
            options.Add(new FloatMenuOption("TMM.RansomPolicy.Title".Translate(),()=>Find.WindowStack.Add(new Dialog_FactionRansomPolicy())));
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
