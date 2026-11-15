using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TurboMegaMod.Wholesale;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public sealed class WholesaleDebit : IExposable
    {
        public Pawn pawn;
        public int remaining;
        public void ExposeData() { Scribe_References.Look(ref pawn,"pawn"); Scribe_Values.Look(ref remaining,"remaining"); }
    }
    public sealed class WholesaleRecovery : IExposable
    {
        public Pawn pawn;
        public Thing item;
        public void ExposeData() { Scribe_References.Look(ref pawn,"pawn"); Scribe_References.Look(ref item,"item"); }
    }
    public static class WholesaleTrade
    {
        public static LordJob_WholesaleVisit Group(Pawn p) { return p == null ? null : p.GetLord() == null ? null : p.GetLord().LordJob as LordJob_WholesaleVisit; }
        public static bool Buyer(Pawn p)
        {
            var g=Group(p);
            return g!=null&&g.leader==p&&g.Shopping&&p.Spawned&&!p.Dead&&!p.Downed&&!p.InMentalState&&!p.IsPrisoner&&!p.IsSlave&&p.Faction==g.Faction;
        }
        public static bool Eligible(Pawn p,Pawn buyer)
        {
            return p!=null&&p.Spawned&&p.Map==buyer.Map&&p.Faction==buyer.Faction&&p.GetLord()==buyer.GetLord()&&!p.IsPrisoner&&!p.IsSlave&&
                !p.Dead&&!p.Downed&&!p.InMentalState&&p.inventory!=null&&MassUtility.CanEverCarryAnything(p)&&
                p.Position.DistanceTo(buyer.Position)<=8f&&!ComplexLayout.StaffOnly(ComplexLayout.Desk(p.Map),p.Position)&&
                p.CanReach(buyer,PathEndMode.Touch,Danger.Some);
        }
        public static int Money(Pawn buyer)
        {
            var g=Group(buyer);
            return g==null?HotelBillingHooks.Money(buyer):(int)Math.Min(int.MaxValue,g.Members.Where(p=>Eligible(p,buyer)).Sum(p=>(long)HotelBillingHooks.Money(p)));
        }
        public static PackingPlan Plan(Pawn buyer,Thing item,int count,double unitPrice)
        {
            var g=Group(buyer);
            if(g==null||!Buyer(buyer)||item==null||double.IsNaN(unitPrice)||double.IsInfinity(unitPrice))return new PackingPlan{Status=PackingStatus.Invalid};
            try{
                float mass=item.GetStatValue(StatDefOf.Mass),silverMass=ThingDefOf.Silver.GetStatValueAbstract(StatDefOf.Mass);
                if(float.IsNaN(mass)||float.IsInfinity(mass)||float.IsNaN(silverMass)||float.IsInfinity(silverMass))return new PackingPlan{Status=PackingStatus.Invalid};
                var carriers=new List<CargoCarrier>();
                foreach(var p in g.Members){
                    if(!Eligible(p,buyer))continue;
                    float cap=MassUtility.Capacity(p),current=MassUtility.GearAndInventoryMass(p);
                    if(float.IsNaN(cap)||float.IsInfinity(cap)||float.IsNaN(current)||float.IsInfinity(current))continue;
                    int coins=HotelBillingHooks.Money(p);
                    // Native masses are floats; avoid rejecting their small
                    // accumulation error against exact decimal silver mass.
                    decimal actual=Math.Max((decimal)current,coins*(decimal)silverMass);
                    carriers.Add(new CargoCarrier{Id=p.GetUniqueLoadID(),Eligible=true,Capacity=(decimal)cap,CurrentMass=actual,Silver=coins});
                }
                return PackingPlanner.Create(new PackingRequest{Count=count,StockCount=item.stackCount,ReservedCount=0,
                    UnitMarketValue=(decimal)unitPrice/0.55m,UnitMass=(decimal)mass,SilverUnitMass=(decimal)silverMass,Carriers=carriers});
            }catch(OverflowException){return new PackingPlan{Status=PackingStatus.Invalid};}
        }
        public static int Space(Pawn buyer,Thing item)
        {
            for(int count=Math.Min(100,item.stackCount);count>0;count--)if(Plan(buyer,item,count,SaleStock.UnitPrice(item)).Status==PackingStatus.Ready)return count;
            return 0;
        }
        public static bool Settle(ComplexSales service,SaleOrder order,Thing offered)
        {
            var group=Group(order.buyer);var plan=Plan(order.buyer,offered,order.count,order.unitPrice);
            if(group==null||plan.Status!=PackingStatus.Ready||plan.Quote!=order.price)return false;
            var members=group.Members.ToDictionary(p=>p.GetUniqueLoadID(),p=>p,StringComparer.Ordinal);
            var cash=order.GetDirectlyHeldThings();
            foreach(var part in plan.Allocations.Where(a=>a.SilverDebit>0)){
                var p=members[part.CarrierId];var debit=new WholesaleDebit{pawn=p};order.wholesaleDebits.Add(debit);
                int needed=part.SilverDebit;
                foreach(var silver in p.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Silver).ToArray()){
                    int before=cash.Sum(t=>t.stackCount);
                    p.inventory.innerContainer.TryTransferToContainer(silver,cash,Math.Min(needed,silver.stackCount),false);
                    int actual=cash.Sum(t=>t.stackCount)-before;needed-=actual;debit.remaining+=actual;if(needed==0)break;
                }
                if(needed!=0){service.Cancel(order);service.Process();return false;}
            }
            Thing staged=null;
            if(offered.Spawned){
                var piece=offered.SplitOff(order.count);order.returnedGoods.TryAdd(piece,false);
                if(order.returnedGoods.Contains(piece))staged=piece;
                else if(!piece.Destroyed&&!piece.Spawned)order.unplacedGoods.Add(piece);
            }else if(offered.holdingOwner!=null)offered.holdingOwner.TryTransferToContainer(offered,order.returnedGoods,order.count,out staged,false);
            if(staged==null||!order.returnedGoods.Contains(staged)||staged.stackCount!=order.count){service.Cancel(order);service.Process();return false;}
            foreach(var part in plan.Allocations.Where(a=>a.GoodsCount>0)){
                var p=members[part.CarrierId];Thing given;
                order.returnedGoods.TryTransferToContainer(staged,p.inventory.innerContainer,part.GoodsCount,out given,false);
                if(given!=null&&p.inventory.innerContainer.Contains(given))order.wholesaleRecoveries.Add(new WholesaleRecovery{pawn=p,item=given});
                if(given==null||!p.inventory.innerContainer.Contains(given)||given.stackCount!=part.GoodsCount||
                    MassUtility.GearAndInventoryMass(p)>MassUtility.Capacity(p)){service.Cancel(order);service.Process();return false;}
            }
            order.completed=true;order.goods=null;order.wholesaleRecoveries.Clear();group.spent+=order.price;
            service.Process();return true;
        }
        public static void Recover(SaleOrder order)
        {
            foreach(var r in order.wholesaleRecoveries.ToArray()){
                if(r.item==null||r.item.Destroyed){order.wholesaleRecoveries.Remove(r);continue;}
                if(order.returnedGoods.Contains(r.item)){order.wholesaleRecoveries.Remove(r);continue;}
                if(!r.item.Spawned&&r.item.holdingOwner!=null)r.item.holdingOwner.TryTransferToContainer(r.item,order.returnedGoods,r.item.stackCount,false);
                else if(r.item.Spawned){var piece=r.item.SplitOff(r.item.stackCount);order.returnedGoods.TryAdd(piece,false);
                    if(!order.returnedGoods.Contains(piece)&&!order.unplacedGoods.Contains(piece))order.unplacedGoods.Add(piece);}
                if(order.returnedGoods.Contains(r.item))order.wholesaleRecoveries.Remove(r);
            }
        }
        public static void Refund(SaleOrder order,Map map)
        {
            // Pending recovery must complete before returning payment; keep
            // both references and actual coins saved while a retry is needed.
            if(order.wholesaleRecoveries.Count>0)return;
            var cash=order.GetDirectlyHeldThings();
            foreach(var d in order.wholesaleDebits){
                foreach(var silver in cash.ToArray()){
                    if(d.remaining<=0)break;
                    int before=cash.Sum(t=>t.stackCount);
                    if(d.pawn!=null&&!d.pawn.Destroyed&&!d.pawn.Dead&&d.pawn.inventory!=null)
                        cash.TryTransferToContainer(silver,d.pawn.inventory.innerContainer,Math.Min(d.remaining,silver.stackCount),false);
                    else{Thing dropped;cash.TryDrop(silver,order.receiptCell,map,ThingPlaceMode.Near,Math.Min(d.remaining,silver.stackCount),out dropped);}
                    d.remaining-=before-cash.Sum(t=>t.stackCount);
                }
            }
            order.wholesaleDebits.RemoveAll(d=>d.remaining==0);
        }
    }
}
