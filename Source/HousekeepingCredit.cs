using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public sealed class HousekeepingAccount : IExposable
    {
        public Pawn worker;
        public long workUnits,credited;
        public void ExposeData(){Scribe_References.Look(ref worker,"worker");Scribe_Values.Look(ref workUnits,"workUnits");Scribe_Values.Look(ref credited,"credited");}
    }
    public sealed class HousekeepingLedger : MapComponent
    {
        public const long UnitsPerSilver=250000; // 250 native work, fixed point x1000.
        public long received,allocated,nextReceipt;
        private int lastWorkerId,nextCheck;
        public List<string> revenueReceipts=new List<string>();
        public List<HousekeepingAccount> accounts=new List<HousekeepingAccount>();
        public long Balance {get{return Math.Max(0,received-allocated);}}
        public HousekeepingLedger(Map map):base(map){}
        public HousekeepingAccount Account(Pawn p){return accounts.FirstOrDefault(a=>a.worker==p);}
        private WorkBuyoutEntry Contract(Pawn p)
        {
            if(p==null||p.Destroyed||p.Dead||!p.IsSlaveOfColony)return null;
            var e=Current.Game.GetComponent<WorkBuyouts>().Entry(p);return e!=null&&e.active&&!e.released&&e.Remaining>0?e:null;
        }
        public void Receive(IntVec3 cell,int amount,string receipt)
        {
            var desk=ComplexLayout.Desk(map);
            if(amount<=0||string.IsNullOrEmpty(receipt)||desk==null||!desk.area[cell]||revenueReceipts.Contains(receipt))return;
            revenueReceipts.Add(receipt);received+=amount;Settle();
        }
        public void Completed(Pawn p,IntVec3 cell,float nativeWork)
        {
            var contract=Contract(p);var desk=ComplexLayout.Desk(map);
            if(contract==null||desk==null||p.MapHeld!=map||!desk.area[cell]||float.IsNaN(nativeWork)||float.IsInfinity(nativeWork)||nativeWork<=0)return;
            var account=Account(p);if(account==null){account=new HousekeepingAccount{worker=p};accounts.Add(account);}
            long units=(long)Math.Round(Math.Min(1000000d,nativeWork)*1000d);
            account.workUnits=Math.Min((long)contract.Remaining*UnitsPerSilver,account.workUnits+units);Settle();
        }
        public void Settle()
        {
            foreach(var a in accounts)if(Contract(a.worker)==null)a.workUnits=0;
            while(Balance>0){
                var ready=accounts.Where(a=>a.workUnits>=UnitsPerSilver&&Contract(a.worker)!=null)
                    .OrderBy(a=>a.worker.thingIDNumber>lastWorkerId?0:1).ThenBy(a=>a.worker.thingIDNumber).ToArray();
                if(ready.Length==0)break;long share=Math.Max(1,Balance/ready.Length);bool changed=false;
                foreach(var a in ready){
                    if(Balance==0)break;var e=Contract(a.worker);if(e==null)continue;
                    int amount=(int)Math.Min(e.Remaining,Math.Min(a.workUnits/UnitsPerSilver,Math.Min(share,Balance)));
                    int before=e.credited;
                    Current.Game.GetComponent<WorkBuyouts>().Credit(a.worker,"cleaning:"+map.uniqueID+":"+(nextReceipt++),amount,false);
                    int actual=e.credited-before;if(actual<=0)continue;
                    allocated+=actual;a.workUnits-=(long)actual*UnitsPerSilver;a.credited+=actual;lastWorkerId=a.worker.thingIDNumber;changed=true;
                }
                if(!changed)break;
            }
        }
        public string Status(Pawn p)
        {
            var a=Account(p);return "TMM.Housekeeping.Progress".Translate(a==null?0:a.credited,
                (a==null?0d:(double)a.workUnits/UnitsPerSilver).ToString("0.00"),Balance);
        }
        public override void ExposeData()
        {
            base.ExposeData();Scribe_Values.Look(ref received,"TMM_cleaningRevenue");Scribe_Values.Look(ref allocated,"TMM_cleaningAllocated");
            Scribe_Values.Look(ref nextReceipt,"TMM_cleaningReceipt");Scribe_Values.Look(ref lastWorkerId,"TMM_cleaningLastWorker");
            Scribe_Collections.Look(ref revenueReceipts,"TMM_cleaningRevenueReceipts",LookMode.Value);Scribe_Collections.Look(ref accounts,"TMM_cleaningAccounts",LookMode.Deep);
            if(Scribe.mode==LoadSaveMode.PostLoadInit){if(revenueReceipts==null)revenueReceipts=new List<string>();if(accounts==null)accounts=new List<HousekeepingAccount>();accounts.RemoveAll(a=>a==null||a.worker==null||a.worker.Destroyed||a.worker.Dead);}
        }
        public override void MapComponentTick(){if(GuestDossiers.Now<nextCheck)return;nextCheck=GuestDossiers.Now+250;Settle();}
    }
    [StaticConstructorOnStartup]
    public static class HousekeepingHooks
    {
        [ThreadStatic]private static JobDriver_CleanFilth active;
        private static readonly System.Reflection.FieldInfo interval=AccessTools.Field(typeof(Toil),"tickIntervalAction");
        private static readonly System.Reflection.PropertyInfo filth=AccessTools.Property(typeof(JobDriver_CleanFilth),"Filth");
        private static readonly System.Reflection.FieldInfo progress=AccessTools.Field(typeof(JobDriver_CleanFilth),"cleaningWorkDone");
        private sealed class RemovedLayer {public Pawn worker;public Map map;public IntVec3 cell;public int thickness;public float work;}
        static HousekeepingHooks()
        {
            var h=new Harmony("turbomegamod.housekeeping.credit");
            h.Patch(AccessTools.Method(typeof(JobDriver_CleanFilth),"MakeNewToils"),postfix:new HarmonyMethod(typeof(HousekeepingHooks),"Toils"));
            h.Patch(AccessTools.Method(typeof(Filth),"ThinFilth"),prefix:new HarmonyMethod(typeof(HousekeepingHooks),"Before"),postfix:new HarmonyMethod(typeof(HousekeepingHooks),"After"));
        }
        private static void Toils(JobDriver_CleanFilth __instance,ref IEnumerable<Toil> __result){__result=Watch(__instance,__result);}
        private static IEnumerable<Toil> Watch(JobDriver_CleanFilth driver,IEnumerable<Toil> toils)
        {
            foreach(var toil in toils){
                var original=toil.tickAction;
                if(original!=null)toil.tickAction=delegate{var previous=active;active=driver;try{original();}finally{active=previous;}};
                var action=interval==null?null:interval.GetValue(toil) as Action<int>;
                if(action!=null)interval.SetValue(toil,new Action<int>(delta=>{var previous=active;active=driver;try{action(delta);}finally{active=previous;}}));
                yield return toil;
            }
        }
        private static void Before(Filth __instance,out RemovedLayer __state)
        {
            __state=null;
            if(active==null||!__instance.Spawned||active.job==null||filth.GetValue(active,null)!=__instance||
                (float)progress.GetValue(active)<__instance.def.filth.cleaningWorkToReduceThickness)return;
            __state=new RemovedLayer{worker=active.pawn,map=__instance.Map,cell=__instance.Position,thickness=__instance.thickness,work=__instance.def.filth.cleaningWorkToReduceThickness};
        }
        private static void After(Filth __instance,RemovedLayer __state)
        {
            if(__state==null||__instance.thickness>=__state.thickness)return;
            __state.map.GetComponent<HousekeepingLedger>().Completed(__state.worker,__state.cell,__state.work*(__state.thickness-__instance.thickness));
        }
    }
}
