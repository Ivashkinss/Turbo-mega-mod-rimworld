using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using TurboMegaMod;

public static class MedicalAllocationTests
{
    private static int checks;
    private static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    private static string Key(string id,bool consumables){return (id??"<unknown>")+"|"+consumables;}
    private static int Number(object value){return Convert.ToInt32(value,CultureInfo.InvariantCulture);}
    private static Dictionary<string,object> Obj(object value){return (Dictionary<string,object>)value;}
    private static object[] Rows(object value){return (object[])value;}
    public static void Main(string[] args)
    {
        var json=new JavaScriptSerializer();
        var cases=Rows(json.DeserializeObject(File.ReadAllText(args[0])));
        foreach(var row in cases){
            var c=Obj(row);string id=(string)c["id"];
            var claims=Rows(c["procedureClaims"]).Select(v=>{var r=Obj(v);return new MedicalClaim{workerId=(string)r["workerId"],
                consumables=(bool)r["consumables"],weight=decimal.Parse((string)r["weight"],CultureInfo.InvariantCulture)};}).ToList();
            var events=Rows(c["events"]).Select(Obj).ToArray();
            var expected=Obj(c["expectedOutputs"]);
            var plan=new MedicalPayment{token=(string)events.First(e=>!e.ContainsKey("type"))["receiptId"],shares=MedicalAllocation.Freeze(Number(c["billCoins"]),claims)};
            var frozen=Rows(expected["frozenShares"]).Select(Obj).ToArray();
            Check(plan.shares.Count==frozen.Length,id+" frozen count");
            for(int i=0;i<frozen.Length;i++)Check(plan.shares[i].workerId==(string)frozen[i]["workerId"]&&plan.shares[i].consumables==(bool)frozen[i]["consumables"]&&plan.shares[i].coins==Number(frozen[i]["share"]),id+" frozen share");
            Check(plan.shares.Sum(s=>s.coins)==Number(c["billCoins"]),id+" frozen conservation");
            var allocated=new Dictionary<string,int>();int income=0;
            var expectedEvents=Rows(expected["eventResults"]).Select(Obj).ToArray();
            for(int i=0;i<events.Length;i++){
                var e=events[i];
                if(e.ContainsKey("type")){plan=json.Deserialize<MedicalPayment>(json.Serialize(plan));Check(plan!=null,id+" state roundtrip");continue;}
                List<MedicalShare> portions;
                var status=plan.Accept((string)e["receiptId"],(string)e["subreceiptId"],Number(e["placedCoins"]),out portions);
                string actual=status==MedicalPaymentStatus.Invalid?"invalid_overpay":status.ToString().ToLowerInvariant();
                Check(actual==(string)expectedEvents[i]["status"],id+" event status");
                int accepted=portions.Sum(s=>s.coins);Check(accepted==Number(expectedEvents[i]["coinsAccepted"]),id+" accepted coins");
                income+=accepted;
                foreach(var s in portions){string key=Key(s.workerId,s.consumables);int prior;allocated.TryGetValue(key,out prior);allocated[key]=prior+s.coins;}
            }
            Check(plan.paid==Number(expected["paidTotal"])&&income==Number(expected["colonyIncome"]),id+" paid/income");
            var expectedAllocated=Rows(expected["allocatedByWorkerAndCategory"]).Select(Obj).ToArray();
            Check(allocated.Count==expectedAllocated.Length,id+" allocation count");
            foreach(var e in expectedAllocated){int actual;Check(allocated.TryGetValue(Key((string)e["workerId"],(bool)e["consumables"]),out actual)&&actual==Number(e["allocated"]),id+" allocation value");}
            Check(allocated.Values.Sum()==plan.paid,id+" paid conservation");
        }
        bool invalid=false;try{MedicalAllocation.Freeze(-1,null);}catch(ArgumentOutOfRangeException){invalid=true;}Check(invalid,"Negative bill rejected");
        invalid=false;try{MedicalAllocation.Freeze(1,new[]{new MedicalClaim{weight=-1}});}catch(ArgumentOutOfRangeException){invalid=true;}Check(invalid,"Negative claim rejected");
        var sample=new MedicalPayment{token="right",shares=MedicalAllocation.Freeze(2,new[]{new MedicalClaim{workerId="A",weight=2},new MedicalClaim{workerId="B",weight=1}})};
        List<MedicalShare> result;
        Check(sample.Accept("wrong","1",1,out result)==MedicalPaymentStatus.Invalid&&sample.paid==0,"Wrong receipt rejected");
        Check(sample.Accept("right","1",0,out result)==MedicalPaymentStatus.Zero&&sample.processed.Count==0,"Zero doesn't seal subreceipt");
        Check(sample.Accept("right","1",1,out result)==MedicalPaymentStatus.Accepted&&result.Single().workerId=="A","First fixed range");
        Check(sample.Accept("right","2",1,out result)==MedicalPaymentStatus.Accepted&&result.Single().workerId=="B","Second fixed range");
        Check(sample.Accept("right","2",1,out result)==MedicalPaymentStatus.Duplicate&&sample.paid==2,"Duplicate after full payment");
        Console.WriteLine(json.Serialize(new{passed=true,cases=cases.Length,checks=checks,
            scope="Actual production allocation arithmetic and notification state; contract credits are verified by separate live WorkBuyouts tests."}));
    }
}
