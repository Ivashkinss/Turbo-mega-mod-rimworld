using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public sealed class GuestVisitRecord : IExposable
    {
        public int id,mapId,started,ended=-1,pendingExit=-1;
        public string mapName,endReason;
        public bool legacy,medical,leaving;
        public void ExposeData()
        {
            Scribe_Values.Look(ref id,"id");Scribe_Values.Look(ref mapId,"mapId");Scribe_Values.Look(ref mapName,"mapName");
            Scribe_Values.Look(ref started,"started");Scribe_Values.Look(ref ended,"ended",-1);Scribe_Values.Look(ref pendingExit,"pendingExit",-1);
            Scribe_Values.Look(ref legacy,"legacy");Scribe_Values.Look(ref medical,"medical");Scribe_Values.Look(ref leaving,"leaving");Scribe_Values.Look(ref endReason,"endReason");
        }
    }
    public sealed class GuestPaymentRecord : IExposable
    {
        public int tick,visitId,amount;
        public string service;
        public void ExposeData(){Scribe_Values.Look(ref tick,"tick");Scribe_Values.Look(ref visitId,"visitId");Scribe_Values.Look(ref amount,"amount");Scribe_Values.Look(ref service,"service");}
    }
    public sealed class GuestDossier : IExposable
    {
        public string id,name,factionName;
        public Pawn pawn;
        public int firstSeen,lastSeen,visitCount,blockedAt,rejectedCount;
        public bool blocked;
        public string reason;
        public long hotel,restaurant,company,sales,clinic;
        public List<GuestVisitRecord> visits=new List<GuestVisitRecord>();
        public List<GuestPaymentRecord> payments=new List<GuestPaymentRecord>();
        public List<string> receipts=new List<string>();
        public GuestVisitRecord Current {get{return visits.LastOrDefault(v=>v.ended<0);}}
        public long PersonalTotal {get{return hotel+restaurant+company+sales;}}
        public void ExposeData()
        {
            Scribe_Values.Look(ref id,"id");Scribe_Values.Look(ref name,"name");Scribe_Values.Look(ref factionName,"factionName");
            if(Scribe.mode==LoadSaveMode.Saving&&pawn!=null&&(pawn.Destroyed||pawn.MapHeld==null&&!Find.WorldPawns.Contains(pawn)))pawn=null;
            Scribe_References.Look(ref pawn,"pawn");Scribe_Values.Look(ref firstSeen,"firstSeen");Scribe_Values.Look(ref lastSeen,"lastSeen");
            Scribe_Values.Look(ref visitCount,"visitCount");Scribe_Values.Look(ref blockedAt,"blockedAt");Scribe_Values.Look(ref rejectedCount,"rejectedCount");
            Scribe_Values.Look(ref blocked,"blocked");Scribe_Values.Look(ref reason,"reason");
            Scribe_Values.Look(ref hotel,"hotel");Scribe_Values.Look(ref restaurant,"restaurant");Scribe_Values.Look(ref company,"company");Scribe_Values.Look(ref sales,"sales");Scribe_Values.Look(ref clinic,"clinic");
            Scribe_Collections.Look(ref visits,"visits",LookMode.Deep);Scribe_Collections.Look(ref payments,"payments",LookMode.Deep);Scribe_Collections.Look(ref receipts,"receipts",LookMode.Value);
            if(Scribe.mode==LoadSaveMode.PostLoadInit){
                if(visits==null)visits=new List<GuestVisitRecord>();if(payments==null)payments=new List<GuestPaymentRecord>();if(receipts==null)receipts=new List<string>();
                visits.RemoveAll(v=>v==null);payments.RemoveAll(p=>p==null);
            }
        }
    }
    // Saved on the order itself, so later placement still belongs to the
    // original visitor after departure, recruitment or destruction of a pawn.
    public sealed class GuestReceiptLink : IExposable
    {
        public string guestId;
        public int visitId;
        public void ExposeData(){Scribe_Values.Look(ref guestId,"guestId");Scribe_Values.Look(ref visitId,"visitId");}
        public void Received(string service,int amount,string token)
        {
            var book=GuestDossiers.Book;if(book!=null)book.Payment(guestId,visitId,service,amount,token);
        }
    }
    public sealed class GuestDossiers : GameComponent
    {
        public List<GuestDossier> records=new List<GuestDossier>();
        private int nextVisitId,nextReceiptId,nextCheck;
        private Dictionary<string,GuestDossier> index;
        public GuestDossiers(Game game){}
        public static GuestDossiers Book {get{return Current.Game==null?null:Current.Game.GetComponent<GuestDossiers>();}}
        public static int Now {get{return Verse.Find.TickManager.TicksGame;}}
        public static Building_ComplexDesk Desk(Map map)
        {
            var component=map==null?null:map.GetComponent<ComplexMap>();var desk=component==null?null:component.desk;
            return desk!=null&&desk.Active?desk:null;
        }
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref records,"TMM_guestDossiers",LookMode.Deep);
            Scribe_Values.Look(ref nextVisitId,"TMM_dossierVisitId");Scribe_Values.Look(ref nextReceiptId,"TMM_dossierReceiptId");
            if(Scribe.mode==LoadSaveMode.PostLoadInit){if(records==null)records=new List<GuestDossier>();records.RemoveAll(r=>r==null||string.IsNullOrEmpty(r.id));index=null;}
        }
        public GuestDossier Find(string id)
        {
            if(id==null)return null;
            if(index==null)index=records.GroupBy(r=>r.id).ToDictionary(g=>g.Key,g=>g.First(),StringComparer.Ordinal);
            GuestDossier value;return index.TryGetValue(id,out value)?value:null;
        }
        public GuestDossier Observe(Pawn pawn)
        {
            if(pawn==null)return null;string id=pawn.GetUniqueLoadID();var row=Find(id);
            if(row==null){row=new GuestDossier{id=id,firstSeen=Now};records.Add(row);index=null;}
            row.pawn=pawn;row.name=pawn.LabelShortCap;row.factionName=pawn.Faction==null?"":pawn.Faction.Name;row.lastSeen=Now;return row;
        }
        public static bool Arrived(Pawn pawn)
        {
            var comp=HotelBillingHooks.Guest(pawn);var lord=pawn==null?null:pawn.GetLord();
            return comp!=null&&lord!=null&&lord.LordJob.GetType().FullName=="Hospitality.LordJob_VisitColony"&&(bool)AccessTools.Field(comp.GetType(),"arrived").GetValue(comp);
        }
        public static bool HasCurrent(Pawn pawn,Map map)
        {
            var row=Book==null||pawn==null?null:Book.Find(pawn.GetUniqueLoadID());
            return row!=null&&row.Current!=null&&row.Current.mapId==map.uniqueID;
        }
        public static bool Blocked(Pawn pawn)
        {
            var row=Book==null||pawn==null?null:Book.Find(pawn.GetUniqueLoadID());return row!=null&&row.blocked;
        }
        public void Begin(Pawn pawn,Map map,bool legacy,bool medical)
        {
            if(pawn==null||map==null||Desk(map)==null||pawn.Faction==Faction.OfPlayer||pawn.IsPrisoner||pawn.IsSlave||WholesaleTrade.Group(pawn)!=null)return;
            var row=Observe(pawn);var current=row.Current;
            if(current!=null&&current.mapId==map.uniqueID){current.leaving=false;current.pendingExit=-1;return;}
            if(current!=null)End(row,"changed",Now);
            row.visitCount++;
            row.visits.Add(new GuestVisitRecord{id=++nextVisitId,mapId=map.uniqueID,mapName=map.Parent==null?"":map.Parent.Label,started=Now,legacy=legacy,medical=medical});
            while(row.visits.Count>50)row.visits.RemoveAt(0);
        }
        private static void End(GuestDossier row,string reason,int tick)
        {
            var current=row.Current;if(current==null)return;current.ended=tick;current.endReason=reason;current.pendingExit=-1;
        }
        public void Leaving(Pawn pawn)
        {
            var row=pawn==null?null:Find(pawn.GetUniqueLoadID());if(row!=null&&row.Current!=null)row.Current.leaving=true;
        }
        public void Despawned(Pawn pawn,Map map)
        {
            var row=pawn==null?null:Find(pawn.GetUniqueLoadID());
            if(row!=null&&row.Current!=null&&row.Current.mapId==map.uniqueID)row.Current.pendingExit=Now;
        }
        public static GuestReceiptLink Capture(Pawn pawn,Map map)
        {
            var book=Book;if(book==null||pawn==null||WholesaleTrade.Group(pawn)!=null)return null;
            if(map==null||Desk(map)==null&&book.Find(pawn.GetUniqueLoadID())==null)return null;
            if(map!=null&&pawn.MapHeld==map&&(Arrived(pawn)||Clinic.ActivePatient(pawn))&&!HasCurrent(pawn,map))book.Begin(pawn,map,true,Clinic.ActivePatient(pawn));
            var row=book.Observe(pawn);return new GuestReceiptLink{guestId=row.id,visitId=row.Current==null?0:row.Current.id};
        }
        public string ReceiptToken(string service,Map map){return service+":"+map.uniqueID+":event:"+(++nextReceiptId);}
        public bool Payment(string id,int visitId,string service,int amount,string token)
        {
            var row=Find(id);if(row==null||amount<=0||string.IsNullOrEmpty(token)||row.receipts.Contains(token))return false;
            if(service!="hotel"&&service!="restaurant"&&service!="company"&&service!="sales"&&service!="clinic")return false;
            row.receipts.Add(token);
            switch(service){case "hotel":row.hotel+=amount;break;case "restaurant":row.restaurant+=amount;break;case "company":row.company+=amount;break;case "sales":row.sales+=amount;break;case "clinic":row.clinic+=amount;break;}
            row.payments.Add(new GuestPaymentRecord{tick=Now,visitId=visitId,service=service,amount=amount});
            while(row.payments.Count>200)row.payments.RemoveAt(0);return true;
        }
        public void SetBlocked(GuestDossier row,bool value,string reason)
        {
            if(row==null)return;row.blocked=value;
            if(value){row.reason=string.IsNullOrWhiteSpace(reason)?"TMM.Dossier.DefaultReason".Translate().ToString():reason.Trim();if(row.reason.Length>200)row.reason=row.reason.Substring(0,200);row.blockedAt=Now;}
        }
        public void Synchronize()
        {
            foreach(var map in Verse.Find.Maps.Where(m=>Desk(m)!=null))foreach(var pawn in map.mapPawns.AllPawnsSpawned.ToArray())
                if(pawn.Faction!=Faction.OfPlayer&&!pawn.IsPrisoner&&!pawn.IsSlave&&(Arrived(pawn)||Clinic.ActivePatient(pawn))){
                    if(!HasCurrent(pawn,map))Begin(pawn,map,true,Clinic.ActivePatient(pawn));else Observe(pawn);
                }
            foreach(var row in records.Where(r=>r.Current!=null).ToArray()){
                var visit=row.Current;var pawn=row.pawn;
                if(pawn==null){End(row,"lost",Now);continue;}
                if(pawn.Dead){End(row,"died",Now);continue;}
                if(pawn.Faction==Faction.OfPlayer||pawn.IsPrisoner||pawn.IsSlave){End(row,"changed",Now);continue;}
                var held=pawn.MapHeld;
                if(held==null||held.uniqueID!=visit.mapId)End(row,"departed",visit.pendingExit>=0?visit.pendingExit:Now);
                else visit.pendingExit=-1;
            }
        }
        public override void GameComponentTick(){if(Now<nextCheck)return;nextCheck=Now+500;Synchronize();}
    }
}
