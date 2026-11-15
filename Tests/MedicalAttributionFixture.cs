using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using Verse;

[StaticConstructorOnStartup]
public static class MedicalReceiptBlock
{
    public static bool blocked=Environment.GetCommandLineArgs().Contains("-tmm-clinic-reload"),allowSingle;
    static MedicalReceiptBlock()
    {
        if(!Environment.GetCommandLineArgs().Any(a=>a=="-tmm-clinic-probe"||a=="-tmm-clinic-reload"))return;
        var method=typeof(GenPlace).GetMethods().First(m=>m.Name=="TryPlaceThing"&&m.GetParameters().Any(p=>p.ParameterType==typeof(Thing).MakeByRefType()));
        new Harmony("tmm.test.medical.receipts").Patch(method,prefix:new HarmonyMethod(typeof(MedicalReceiptBlock),"Place"));
    }
    private static bool Place(Thing __0,ref bool __result)
    {
        if(!blocked||__0==null||__0.def!=ThingDefOf.Silver)return true;
        if(allowSingle&&__0.stackCount==1){allowSingle=false;return true;}
        __result=false;return false;
    }
}
public sealed class MedicalAttributionFixture : IExposable
{
    public Pawn a,b,patient;
    public string token;
    public int coins,firstA,firstB,halfA,halfB,initialIncome;
    public float tendWeight,pureWeight,mixedWeight;
    public void ExposeData()
    {
        Scribe_References.Look(ref a,"a");Scribe_References.Look(ref b,"b");Scribe_References.Look(ref patient,"patient");
        Scribe_Values.Look(ref token,"token");Scribe_Values.Look(ref coins,"coins");Scribe_Values.Look(ref firstA,"firstA");Scribe_Values.Look(ref firstB,"firstB");
        Scribe_Values.Look(ref halfA,"halfA");Scribe_Values.Look(ref halfB,"halfB");Scribe_Values.Look(ref initialIncome,"initialIncome");
        Scribe_Values.Look(ref tendWeight,"tendWeight");Scribe_Values.Look(ref pureWeight,"pureWeight");Scribe_Values.Look(ref mixedWeight,"mixedWeight");
    }
    private static WorkBuyouts Buyouts {get{return Current.Game.GetComponent<WorkBuyouts>();}}
    private static Pawn Worker(Map map,IntVec3 cell)
    {
        var p=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);GenSpawn.Spawn(p,cell,map);
        p.guest.SetGuestStatus(Faction.OfPlayer,GuestStatus.Slave);return p;
    }
    public void Prepare(Map map,Building_ComplexDesk desk,Func<Pawn,int,float,object> register,Action<bool,string> check)
    {
        // Let the original clinic scenario run unchanged until this point.
        initialIncome=desk.clinicIncome;
        var actors=new[]{Worker(map,new IntVec3(46,0,46)),Worker(map,new IntVec3(47,0,46))}.OrderBy(p=>p.GetUniqueLoadID(),StringComparer.Ordinal).ToArray();
        a=actors[0];b=actors[1];
        check(Buyouts.Begin(a,10000)!=null&&Buyouts.Begin(b,10000)!=null,"Medical contracts missing");
        patient=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Find.FactionManager.AllFactionsListForReading.First(f=>!f.IsPlayer&&f.def.humanlikeFaction&&!f.HostileTo(Faction.OfPlayer)));
        GenSpawn.Spawn(patient,new IntVec3(46,0,45),map);patient.guest.SetGuestStatus(Faction.OfPlayer);
        register(patient,Find.TickManager.TicksGame+10,0f);token=ClinicContributions.Token(patient);
        var wound=HediffMaker.MakeHediff(HediffDefOf.Cut,patient,patient.RaceProps.body.corePart);wound.Severity=2;patient.health.AddHediff(wound);
        var med=(Medicine)ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);med.stackCount=1;GenSpawn.Spawn(med,new IntVec3(47,0,45),map);
        TendUtility.DoTend(a,patient,med);
        float tended=ClinicContributions.Bill(patient);
        tendWeight=tended;
        var account=map.GetComponent<ClinicAccounts>();var episode=account.episodes.Single(e=>e.token==token);
        check(tended>0&&episode.contributions.Any(c=>c.worker==a&&c.consumables&&c.weight>0),"Completed native tend did not capture doctor and material charge");
        check(Buyouts.Entry(a).credited==0&&Buyouts.Entry(b).credited==0,"Procedure credited without physical payment");
        var recipe=new RecipeDef{defName="TMM_TestFreeSurgery",label="test completion",workAmount=2000f,ingredients=new List<IngredientCount>(),defaultIngredientFilter=new ThingFilter()};
        var bill=new Bill_Medical(recipe,new List<Thing>());patient.health.surgeryBills.AddBill(bill);
        bill.Notify_IterationCompleted(b,new List<Thing>());
        if(patient.health.surgeryBills.Bills.Contains(bill))patient.health.surgeryBills.Delete(bill);
        pureWeight=ClinicContributions.Bill(patient)-tended;
        check(ClinicContributions.Bill(patient)>tended&&episode.contributions.Any(c=>c.worker==b&&!c.consumables&&c.weight>0),"Native surgery completion lost billDoer or labor share");
        var ingredient=new IngredientCount();ingredient.filter.SetAllow(ThingDefOf.Steel,true);ingredient.SetBaseCount(2);
        var mixedRecipe=new RecipeDef{defName="TMM_TestMaterialSurgery",label="test material completion",workAmount=2000f,
            ingredients=new List<IngredientCount>{ingredient},skillRequirements=new List<SkillRequirement>{new SkillRequirement{skill=SkillDefOf.Medicine,minLevel=2}},defaultIngredientFilter=new ThingFilter()};
        var mixedBill=new Bill_Medical(mixedRecipe,new List<Thing>());patient.health.surgeryBills.AddBill(mixedBill);
        float beforeMixed=ClinicContributions.Bill(patient);mixedBill.Notify_IterationCompleted(b,new List<Thing>());
        if(patient.health.surgeryBills.Bills.Contains(mixedBill))patient.health.surgeryBills.Delete(mixedBill);
        mixedWeight=ClinicContributions.Bill(patient)-beforeMixed;
        check(mixedWeight>40&&episode.contributions.Any(c=>c.worker==b&&c.consumables&&Math.Abs(c.weight-mixedWeight)<0.001f),"Consumable surgery did not attribute the whole paid service at the consumable rate");
        check(Math.Abs(episode.contributions.Where(c=>c.worker==b&&!c.consumables).Sum(c=>c.weight)-pureWeight)<0.001f,"Consumable surgery incorrectly added 100% labor credit");
        check(Buyouts.Entry(b).credited==0,"Surgery credited before payout");
        MedicalReceiptBlock.blocked=true;
        AccessTools.Method(Clinic.HospitalType,"PatientLeaves").Invoke(Clinic.Hospital(map),new object[]{patient});
        var receipt=account.receipts.Single(r=>r.token==token);coins=receipt.shares.Sum(s=>s.coins);
        check(coins>2&&receipt.paid==0&&receipt.GetDirectlyHeldThings().Sum(t=>t.stackCount)==coins,"Blocked medical payment lost real escrow");
        check(Buyouts.Entry(a).credited==0&&Buyouts.Entry(b).credited==0&&desk.clinicIncome==initialIncome,"Blocked payment generated phantom earnings");
        var cash=receipt.GetDirectlyHeldThings();var single=cash[0].SplitOff(1);check(cash.TryAdd(single,false),"Partial receipt fixture failed");
        MedicalReceiptBlock.allowSingle=true;account.PlaceReceipts();
        check(receipt.paid==1&&cash.Sum(t=>t.stackCount)==coins-1&&desk.clinicIncome==initialIncome+1,"Real partial receipt did not advance exactly one coin");
        firstA=Buyouts.Entry(a).credited;firstB=Buyouts.Entry(b).credited;halfA=Buyouts.Entry(a).halfRemainder;halfB=Buyouts.Entry(b).halfRemainder;
        check(firstA==0&&firstB==0&&halfA==1&&halfB==0,"Partial consumable payment did not preserve exactly half a coin");
        // The remainder and actual worker references are saved by the real
        // MapComponent, not reconstructed by this fixture.
    }
    public void BeforeReloadPlacement(Map map,Action<bool,string> check)
    {
        var receipt=map.GetComponent<ClinicAccounts>().receipts.Single(r=>r.token==token);
        check(a!=null&&b!=null&&receipt.paid==1&&receipt.shares.Sum(s=>s.coins)==coins,"Saved medical plan or worker refs missing");
        check(Buyouts.Entry(a).credited==firstA&&Buyouts.Entry(b).credited==firstB&&Buyouts.Entry(a).halfRemainder==halfA&&Buyouts.Entry(b).halfRemainder==halfB,"Paid cursor or half-coin state changed across save/load");
        MedicalReceiptBlock.blocked=false;
    }
    public void AfterReloadPlacement(Map map,Action<bool,string> check)
    {
        var accounts=map.GetComponent<ClinicAccounts>();
        check(!accounts.receipts.Any(r=>r.token==token),"Medical escrow not completed");
        // The first actor tended with medicine; the second completed both
        // labor-only and material-using medical bills.
        var claims=MedicalAllocation.Freeze(coins,new[]{new MedicalClaim{workerId=a.GetUniqueLoadID(),consumables=true,weight=(decimal)tendWeight},
            new MedicalClaim{workerId=b.GetUniqueLoadID(),weight=(decimal)pureWeight},new MedicalClaim{workerId=b.GetUniqueLoadID(),consumables=true,weight=(decimal)mixedWeight}});
        int material=claims.Where(s=>s.workerId==a.GetUniqueLoadID()).Sum(s=>s.coins),labor=claims.Where(s=>s.workerId==b.GetUniqueLoadID()&&!s.consumables).Sum(s=>s.coins),
            mixed=claims.Where(s=>s.workerId==b.GetUniqueLoadID()&&s.consumables).Sum(s=>s.coins);
        check(Buyouts.Entry(a).credited==material/2&&Buyouts.Entry(a).halfRemainder==material%2&&Buyouts.Entry(b).credited==labor+mixed/2&&Buyouts.Entry(b).halfRemainder==mixed%2,"Medical credits differ from full frozen payment");
        int priorA=Buyouts.Entry(a).credited,priorB=Buyouts.Entry(b).credited;
        accounts.PlaceReceipts();AccessTools.Method(Clinic.HospitalType,"PatientLeaves").Invoke(Clinic.Hospital(map),new object[]{patient});
        check(Buyouts.Entry(a).credited==priorA&&Buyouts.Entry(b).credited==priorB,"Repeated discharge credited medical labor twice");
    }
}
