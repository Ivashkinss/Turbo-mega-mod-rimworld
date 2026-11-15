using System;
using System.Linq;
using TurboMegaMod.Wholesale;

public static class WholesalePackingTests
{
    private static int checks;
    private static void Check(bool value, string description) { if (!value) throw new Exception(description); checks++; }
    private static CargoCarrier Carrier(string id, decimal capacity, decimal mass, int silver, bool eligible = true)
    { return new CargoCarrier { Id=id, Capacity=capacity, CurrentMass=mass, Silver=silver, Eligible=eligible }; }
    private static PackingRequest Request(int count, decimal value, decimal mass, params CargoCarrier[] carriers)
    { return new PackingRequest { Count=count, StockCount=10000, UnitMarketValue=value, UnitMass=mass, SilverUnitMass=0.008m, Carriers=carriers }; }
    private static PackingPlan Ready(PackingRequest request)
    {
        var plan = PackingPlanner.Create(request);
        Check(plan.Status == PackingStatus.Ready, "Expected a feasible plan");
        Check(plan.Allocations.Sum(a => a.SilverDebit) == plan.Quote, "Debits do not equal price");
        Check(plan.Allocations.Sum(a => a.GoodsCount) == request.Count, "Goods do not equal order count");
        foreach (var a in plan.Allocations) {
            var c=request.Carriers.Single(x=>x.Id==a.CarrierId);
            Check(a.FinalMass==c.CurrentMass-a.SilverDebit*request.SilverUnitMass+a.GoodsCount*request.UnitMass,"Mass not conserved");
            Check(a.GoodsCount==0||a.FinalMass<=c.Capacity,"Packing overloaded a recipient");
            Check(c.Eligible||a.SilverDebit==0&&a.GoodsCount==0,"Used an ineligible carrier");
        }
        return plan;
    }
    public static int Main()
    {
        try {
            var request=Request(10,18m,0.1m,Carrier("human-b",35m,25m,2500),Carrier("human-a",35m,28m,2500));
            var plan=Ready(request);
            Check(plan.Quote==99,"Price must round the full batch once, without float epsilon");
            Check(plan.Allocations[0].CarrierId=="human-a"&&plan.Allocations[0].SilverDebit==99,"Payment order is not stable");
            Check(request.Carriers[1].Silver==2500&&request.Carriers[1].CurrentMass==28m,"Planner mutated real inputs");
            request.Carriers=request.Carriers.Reverse().ToArray();
            var reordered=Ready(request);
            Check(reordered.Allocations[0].CarrierId==plan.Allocations[0].CarrierId&&reordered.Allocations[0].GoodsCount==plan.Allocations[0].GoodsCount,"Input order changed packing");

            request=Request(1,100m,25m,Carrier("a",35m,20m,100),Carrier("b",35m,20m,0));
            Check(PackingPlanner.Create(request).Status==PackingStatus.InsufficientSpace,"Aggregate space incorrectly held an indivisible item");
            request=Request(1,100m,0.44m,Carrier("a",35m,35m,100));
            plan=Ready(request);Check(plan.Quote==55&&plan.Allocations[0].FinalMass==35m,"Payment did not free its actual carrier's mass");
            request=Request(1,100m,0.44m,Carrier("a",35m,35m,100),Carrier("b",1m,1m,0));
            Ready(request);
            request=Request(2,10m,1m,Carrier("a",35m,34m,100),Carrier("b",35m,34m,0));
            plan=Ready(request);Check(plan.Allocations.All(a=>a.GoodsCount==1),"Divisible batch did not use two inventories");
            request=Request(3,1m,0m,Carrier("a",1m,1m,100));Ready(request);
            request=Request(20,0.05m,0m,Carrier("a",35m,1m,100));
            Check(Ready(request).Quote==1,"Tiny price was rounded per unit");
            request=Request(1,100m,0m,Carrier("a",35m,0m,0),Carrier("z",100m,80m,10000,false));
            Check(PackingPlanner.Create(request).Status==PackingStatus.InsufficientFunds,"Used unavailable money");
            request=Request(1,100m,1m,Carrier("a",100m,80m,10000,false),Carrier("z",35m,1m,100));
            Ready(request);
            request=Request(3,10m,10m,Carrier("a",15m,0.8m,100),Carrier("b",0m,0m,0));
            plan=PackingPlanner.Create(request);
            Check(plan.Status==PackingStatus.InsufficientSpace&&plan.Allocations.Count==0,"Failed packing exposed a partial payment/allocation");
            request=Request(3,1m,0m,Carrier("a",35m,1m,100));request.ReservedCount=9998;
            Check(PackingPlanner.Create(request).Status==PackingStatus.InsufficientStock,"Ignored reserved stock");
            request.ReservedCount=int.MaxValue;
            Check(PackingPlanner.Create(request).Status==PackingStatus.InsufficientStock,"Excess reservation became negative free stock");
            request=Request(1,100m,1m,Carrier("a",35m,1m,100),Carrier("a",35m,1m,100));
            Check(PackingPlanner.Create(request).Status==PackingStatus.Invalid,"Accepted duplicate carrier IDs");
            request=Request(1,1m,1m,Carrier("a",35m,0m,100));
            Check(PackingPlanner.Create(request).Status==PackingStatus.Invalid,"Current mass did not include existing silver");
            request=Request(1,decimal.MaxValue,0m,Carrier("a",35m,1m,100));
            Check(PackingPlanner.Create(request).Status==PackingStatus.Invalid,"Accepted out-of-range quote");
            request.Count=2;
            Check(PackingPlanner.Create(request).Status==PackingStatus.Invalid,"Decimal overflow escaped validation");
            request=Request(1,1m,0.0000000000000000000000000001m,Carrier("a",100m,1m,100));Ready(request);
            request.UnitMass=-1;Check(PackingPlanner.Create(request).Status==PackingStatus.Invalid,"Accepted negative item mass");
            request.UnitMass=0;request.Count=0;Check(PackingPlanner.Create(request).Status==PackingStatus.Invalid,"Accepted zero requested count");
            Console.WriteLine("{\"passed\":true,\"checks\":"+checks+",\"scope\":\"Pure packing and quote planner; no live transaction or game AI\"}");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
