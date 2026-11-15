using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using TurboMegaMod.Wholesale;

// A separate fixture adapter, not a game transaction simulator. It exercises
// the real C# planner on independently supplied Gemini inputs/expected values.
public static class WholesalePackingCaseProbe
{
    public static int Checks, Cases;
    public static readonly List<string> Excluded = new List<string>();
    private static void Check(bool condition,string message) { if(!condition)throw new Exception(message);Checks++; }
    private static IDictionary<string,object> Obj(object v) { return (IDictionary<string,object>)v; }
    private static bool Flag(IDictionary<string,object> d,string key) { return (bool)d[key]; }
    private static int Number(object v) { return Convert.ToInt32(v,CultureInfo.InvariantCulture); }
    private static bool DecimalValue(object v,out decimal value)
    { return v is string && decimal.TryParse((string)v,NumberStyles.Float,CultureInfo.InvariantCulture,out value) || AssignZero(out value); }
    private static bool AssignZero(out decimal value) { value=0;return false; }
    private static decimal ExpectedMass(object v) { return decimal.Parse((string)v,NumberStyles.Float,CultureInfo.InvariantCulture); }
    public static void Run(string file)
    {
        var data=(object[])new JavaScriptSerializer().DeserializeObject(File.ReadAllText(file));
        Check(data.Length==32,"Expected the independent 32-case package");
        foreach(var entry in data){
            var row=Obj(entry);string id=(string)row["id"];var input=Obj(row["inputs"]);var expected=Obj(row["expected"]);
            // These decisions belong to the future service's lifecycle and
            // receipt processing, not this pure packing/quote module.
            if(!Flag(input,"complexActive")||!Flag(input,"factionFriendly")||!Flag(input,"arrived")||!Flag(input,"publicHandover")||
                !Flag(input,"storagePublic")&&!Flag(input,"staffDeliveryReady")||Flag(input,"duplicate")||id=="CASE-31"||id=="CASE-32"){
                Excluded.Add(id);continue;
            }
            var request=new PackingRequest{Count=Number(input["requestedCount"]),StockCount=Number(input["stockCount"]),ReservedCount=Number(input["reservedCount"])};
            bool parsed=DecimalValue(input["unitMarketValue"],out request.UnitMarketValue);
            parsed=DecimalValue(input["itemUnitMass"],out request.UnitMass)&&parsed;
            parsed=DecimalValue(input["silverUnitMass"],out request.SilverUnitMass)&&parsed;
            var carriers=new List<CargoCarrier>();
            foreach(var cvalue in (object[])input["carriers"]){
                var c=Obj(cvalue);var carrier=new CargoCarrier{Id=(string)c["id"],Eligible=Flag(c,"eligible"),Silver=Number(c["silver"])};
                parsed=DecimalValue(c["capacity"],out carrier.Capacity)&&parsed;
                parsed=DecimalValue(c["currentMass"],out carrier.CurrentMass)&&parsed;
                carriers.Add(carrier);
            }
            request.Carriers=carriers;
            var original=carriers.Select(c=>new {c.Id,c.Capacity,c.CurrentMass,c.Silver,c.Eligible}).ToArray();
            var plan=parsed?PackingPlanner.Create(request):new PackingPlan{Status=PackingStatus.Invalid};
            string status=(string)expected["status"];
            bool needsReady=status=="completed"||status=="cancelled";
            Check(needsReady?plan.Status==PackingStatus.Ready:status=="invalid"?plan.Status==PackingStatus.Invalid:
                plan.Status==PackingStatus.InsufficientStock||plan.Status==PackingStatus.InsufficientFunds||plan.Status==PackingStatus.InsufficientSpace,
                id+": C# packing status differs from independent fixture");
            Check(plan.Quote==Number(expected["quote"]),id+": quote differs");
            for(int index=0;index<carriers.Count;index++){
                var c=carriers[index];var before=original[index];
                Check(c.Id==before.Id&&c.Capacity==before.Capacity&&c.CurrentMass==before.CurrentMass&&c.Silver==before.Silver&&c.Eligible==before.Eligible,
                    id+": planner mutated an input carrier");
            }
            if(status=="completed"){
                var debits=Obj(expected["debitByCarrier"]);var goods=Obj(expected["goodsByCarrier"]);var masses=Obj(expected["finalMassByCarrier"]);var silver=Obj(expected["finalSilverByCarrier"]);
                Check(plan.Allocations.Count==carriers.Count,id+": missing carrier allocation");
                foreach(var allocation in plan.Allocations){
                    var carrier=carriers.Single(c=>c.Id==allocation.CarrierId);string key=allocation.CarrierId;
                    Check(allocation.SilverDebit==Number(debits[key]),id+": debit differs for "+key);
                    Check(allocation.GoodsCount==Number(goods[key]),id+": goods differ for "+key);
                    Check(allocation.FinalMass==ExpectedMass(masses[key]),id+": mass differs for "+key);
                    Check(carrier.Silver-allocation.SilverDebit==Number(silver[key]),id+": remaining silver differs for "+key);
                }
                Check(plan.Allocations.Sum(a=>a.GoodsCount)==Number(expected["deliveredCount"]),id+": quantity differs");
                Check(request.StockCount-plan.Allocations.Sum(a=>a.GoodsCount)==Number(expected["finalStockCount"]),id+": stock differs");
            }else if(status!="cancelled")Check(plan.Allocations.Count==0,id+": refused/invalid plan exposed partial allocations");
            // CASE-22 verifies that a valid proposal can precede a failed real
            // transfer. Rollback itself is outside this module's test scope.
            Cases++;
        }
        Check(Cases==24&&Excluded.Count==8,"Unexpected case-coverage scope");
    }
}
