using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TurboMegaMod;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

[StaticConstructorOnStartup]
public static class SalesTestLaunch
{
    public static bool blockReceipts;
    static SalesTestLaunch()
    {
        if (Environment.GetCommandLineArgs().Contains("-tmm-sales-reload")) LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-Sales"));
        if (!Environment.GetCommandLineArgs().Any(a => a == "-tmm-sales-probe" || a == "-tmm-sales-reload")) return;
        var h = new Harmony("tmm.test.sales.receipts");
        h.Patch(AccessTools.Method(typeof(JobDriver), "EndJobWith"), prefix:new HarmonyMethod(typeof(SalesTestLaunch),"TraceEnd"));
        h.Patch(AccessTools.Method(typeof(ComplexSales), "Process"), prefix:new HarmonyMethod(typeof(SalesTestLaunch),"TraceProcess"));
        foreach (var method in typeof(GenPlace).GetMethods().Where(m => m.Name == "TryPlaceThing" && m.ReturnType == typeof(bool) && m.GetParameters()[0].ParameterType == typeof(Thing)))
            h.Patch(method, prefix: new HarmonyMethod(typeof(SalesTestLaunch), "Block"));
    }
    private static bool Block(Thing __0, ref bool __result) { if (!blockReceipts || __0.def != ThingDefOf.Silver) return true; __result = false; return false; }
    private static void TraceProcess(ComplexSales __instance)
    {
        foreach(var o in __instance.orders)if(o.cancelled||!o.completed&&!__instance.Valid(o))
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"SalesJobTrace.log"),
                "PROCESS tick="+Find.TickManager.TicksGame+" id="+o.id+" cancelled="+o.cancelled+" allowed="+SaleStock.Allowed(o.buyer,o.storage,o.item)+
                " free="+__instance.FreeCount(o.item,o)+" count="+o.count+" price="+o.price+" cash="+o.GetDirectlyHeldThings().Sum(t=>t.stackCount)+
                " money="+HotelBillingHooks.Money(o.buyer)+" buyerGoods="+o.buyer.inventory.innerContainer.Where(t=>t.def==o.item.def).Sum(t=>t.stackCount)+
                " sourceOwner="+(o.item.holdingOwner==null?"null":o.item.holdingOwner.Owner.GetType().Name)+
                " returned="+o.returnedGoods.Sum(t=>t.stackCount)+" jobCount="+o.buyer.CurJob.count+" job="+o.buyer.CurJob+Environment.NewLine);
    }
    private static void TraceEnd(JobDriver __instance, JobCondition __0)
    {
        if (!(__instance is JobDriver_BuyComplexGoods)) return;
        var p=__instance.pawn;var service=p.Map.GetComponent<ComplexSales>();var o=service.ById(p.CurJob.count);
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"SalesJobTrace.log"),
            "tick="+Find.TickManager.TicksGame+" end="+__0+" pos="+p.Position+" jobCount="+p.CurJob.count+" job="+p.CurJob+
            (o==null?" order=null":" completed="+o.completed+" cancelled="+o.cancelled+" valid="+service.Valid(o)+
            " allowed="+SaleStock.Allowed(p,o.storage,o.item)+" free="+service.FreeCount(o.item,o)+" item="+o.item+" count="+o.count+
            " contains="+SaleStock.Contains(o.storage,o.item)+" space="+SaleStock.Space(p,o.item))+Environment.NewLine+Environment.StackTrace+Environment.NewLine);
    }
}
public sealed class SalesProbe : MapComponent
{
    private Building_ComplexDesk desk;
    private Building shelf, bookcase, emptyShelf, outfit;
    private Pawn buyer, second;
    private Thing steel, book, shirt;
    private int shirtPrice;
    private Building adaptive;
    private Thing adaptiveStock;
    private int adaptivePrice;
    private int checks, stage, started, saleId, price, bookOrderId, bookPrice, bookId, initialFloor;
    private string bookLabel;
    private bool reported, uiReported, saved;
    private readonly List<string> errors = new List<string>();
    private ComplexSales Sales { get { return map.GetComponent<ComplexSales>(); } }
    public SalesProbe(Map map) : base(map) { }
    public override void ExposeData()
    {
        base.ExposeData(); Scribe_References.Look(ref desk, "spDesk"); Scribe_References.Look(ref shelf, "spShelf"); Scribe_References.Look(ref bookcase, "spBookcase");
        Scribe_References.Look(ref emptyShelf, "spEmpty"); Scribe_References.Look(ref buyer, "spBuyer"); Scribe_References.Look(ref second, "spSecond");
        Scribe_References.Look(ref steel, "spSteel"); Scribe_References.Look(ref book, "spBook"); Scribe_Values.Look(ref saved, "spSaved");
        Scribe_References.Look(ref outfit, "spOutfit"); Scribe_References.Look(ref shirt, "spShirt");
        Scribe_Values.Look(ref saleId, "spId"); Scribe_Values.Look(ref price, "spPrice"); Scribe_Values.Look(ref bookOrderId, "spBookOrder");
        Scribe_Values.Look(ref bookPrice, "spBookPrice"); Scribe_Values.Look(ref bookId, "spBookId"); Scribe_Values.Look(ref bookLabel, "spBookLabel");
        Scribe_Values.Look(ref initialFloor, "spInitialFloor");
    }
    private void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    private void Wait(Pawn p) { var j = JobMaker.MakeJob(JobDefOf.Wait); j.expiryInterval = 600000; p.jobs.StartJob(j, JobCondition.InterruptForced); }
    private int Floor() { return map.listerThings.ThingsOfDef(ThingDefOf.Silver).Sum(t => t.stackCount); }
    private Building Spawn(string def, IntVec3 cell)
    {
        var d = DefDatabase<ThingDef>.GetNamed(def); var b = (Building)ThingMaker.MakeThing(d, d.MadeFromStuff ? ThingDefOf.Steel : null);
        b.SetFaction(Faction.OfPlayer); return (Building)GenSpawn.Spawn(b, cell, map);
    }
    private Pawn Guest(IntVec3 cell)
    {
        var faction = Find.FactionManager.AllFactionsListForReading.First(f => !f.IsPlayer && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer));
        var p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, faction, forceGenerateNewPawn:true,
            allowDead:false, allowDowned:false, allowAddictions:false, allowPregnant:false, fixedBiologicalAge:30f));
        GenSpawn.Spawn(p, cell, map); p.inventory.innerContainer.ClearAndDestroyContents();
        var silver = ThingMaker.MakeThing(ThingDefOf.Silver); silver.stackCount = 500; p.inventory.innerContainer.TryAdd(silver, false);
        if (p.needs.food != null) p.needs.food.CurLevelPercentage = 1;
        if (p.needs.rest != null) p.needs.rest.CurLevelPercentage = 1;
        AccessTools.Method(AccessTools.TypeByName("Hospitality.IncidentWorker_VisitorGroup"), "CreateLord").Invoke(null,
            new object[] { faction, new IntVec3(44, 0, 44), new List<Pawn> { p }, map, false, false, 600000 });
        p.GetLord().ReceiveMemo("TravelArrived");
        AccessTools.Property(HotelBillingHooks.Guest(p).GetType(), "ShoppingArea").SetValue(HotelBillingHooks.Guest(p), null, null);
        Wait(p); return p;
    }
    private void Start(SaleOrder o) { var j = JobMaker.MakeJob(ComplexSales.BuyJob, o.storage); j.count = o.id; o.buyer.jobs.StartJob(j, JobCondition.InterruptForced); }
    private void Setup()
    {
        foreach (var p in map.mapPawns.FreeColonistsSpawned) Wait(p);
        foreach (var c in new CellRect(38, 38, 30, 20)) {
            foreach (var t in c.GetThingList(map).ToArray()) if (t is Building || t is Plant) { if (t.def.destroyable) t.Destroy(); else t.DeSpawn(); }
            map.terrainGrid.SetTerrain(c, TerrainDefOf.Soil); map.roofGrid.SetRoof(c, null);
        }
        Area_Allowed area, staff; map.areaManager.TryMakeNewAllowed(out area); map.areaManager.TryMakeNewAllowed(out staff);
        foreach (var c in new CellRect(40, 40, 25, 15)) area[c] = true;
        desk = (Building_ComplexDesk)Spawn("TMM_ComplexDesk", new IntVec3(42, 0, 45)); desk.area = area; desk.staffArea = staff; desk.ApplyServiceAreas();
        shelf = Spawn("Shelf", new IntVec3(50, 0, 44)); emptyShelf = Spawn("ShelfSmall", new IntVec3(53, 0, 47));
        bookcase = Spawn("Bookcase", new IntVec3(58, 0, 44));
        outfit = Spawn("Building_OutfitStand", new IntVec3(61, 0, 44));
        shirt = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_BasicShirt"), ThingDefOf.Cloth);
        shirt.TryGetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
        Check((bool)AccessTools.Method(outfit.GetType(), "AddApparel").Invoke(outfit, new object[] { shirt }), "Outfit stand rejected shirt");
        steel = ThingMaker.MakeThing(ThingDefOf.Steel); steel.stackCount = 20; GenSpawn.Spawn(steel, shelf.Position, map);
        book = ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefsListForReading.First(d => d.category == ThingCategory.Item && typeof(Book).IsAssignableFrom(d.thingClass)));
        Check(((ISearchableContents)bookcase).SearchableContents.TryAdd(book, false), "Bookcase rejected book"); bookId = book.thingIDNumber; bookLabel = book.Label;
        Check(!book.Spawned && SaleStock.Contents(bookcase).Contains(book), "Container stock was not found");
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.defName.StartsWith("sbz_") && SaleStock.Supported(d.thingClass))) {
            var made = (ThingWithComps)ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
            Check(made.AllComps.Count(c => c is CompComplexSale) == 1, "Missing/duplicate sale comp: " + def.defName);
        }
        Check(shelf.GetComp<CompComplexSale>() != null && bookcase.GetComp<CompComplexSale>() != null, "Native storage sale comps missing");
        Check(SaleStock.Price(200 * 0.55, 1) == 110 && SaleStock.Price(0.05 * 0.55, 20) == 1, "Rounding charged by units or added a floating-point coin");
        Check(SaleStock.Price(double.NaN, 1) == -1 && SaleStock.Price(double.PositiveInfinity, 1) == -1, "Nonfinite price accepted");
        buyer = Guest(new IntVec3(45, 0, 44)); second = Guest(new IntVec3(46, 0, 44)); initialFloor = Floor();
        var giver = DefDatabase<JoyGiverDef>.GetNamed("BuyStuff").Worker;
        Check(giver.TryGiveJob(buyer) == null, "Unmarked stock was available");
        var flag = shelf.GetComp<CompComplexSale>(); flag.forSale = true;
        var edge = shelf.OccupiedRect().Cells.First(c => c != shelf.Position); area[edge] = false;
        Check(flag.forSale && !SaleStock.Active(shelf) && Sales.Create(buyer, shelf, steel, 1) == null, "Partial/outside storage sold goods"); area[edge] = true;
        staff[shelf.Position] = true; desk.ApplyServiceAreas();
        Check(!SaleStock.Public(shelf) && Sales.Create(buyer, shelf, steel, 1) == null, "Guest accessed staff-only stock"); staff[shelf.Position] = false; desk.ApplyServiceAreas();
        steel.SetForbidden(true, false); Check(Sales.Create(buyer, shelf, steel, 1) == null, "Forbidden goods sold"); steel.SetForbidden(false, false);
        Check(giver.GetChance(buyer) > 0, "Native shopping still requires a separate shopping area");
        var nativeJob = giver.TryGiveJob(buyer); Check(nativeJob != null && nativeJob.def == ComplexSales.BuyJob, "Native shopping did not use complex sales");
        Sales.ById(nativeJob.count).cancelled = true; Sales.Process();
        var order = Sales.Create(buyer, shelf, steel, 3); Check(order != null, "Sale could not be booked");
        Check(Sales.Create(second, shelf, steel, 3) == null, "Two buyers reserved the same stack");
        flag.forSale = false; Check(!Sales.Buy(order) && HotelBillingHooks.Money(buyer) == 500, "Disabling sale charged buyer"); Sales.Process(); flag.forSale = true;
        order = Sales.Create(buyer, shelf, steel, 3); Check(order != null, "Stock did not recover after cancellation");
        saleId = order.id; price = order.price; SalesTestLaunch.blockReceipts = true; Start(order); started = Find.TickManager.TicksGame; stage = 1;
    }
    public override void MapComponentUpdate()
    {
        bool reload = Environment.GetCommandLineArgs().Contains("-tmm-sales-reload");
        if (reported || Find.CurrentMap != map || !reload && !Environment.GetCommandLineArgs().Contains("-tmm-sales-probe")) return;
        try {
            if (!reload && stage == 0) { Setup(); return; }
            if (!reload && stage == 1) {
                var o = Sales.ById(saleId);
                if (o != null && !o.completed && !o.cancelled && Find.TickManager.TicksGame - started < 2000) return;
                Check(o != null && o.completed, "Real walk-and-buy failed: " + buyer.CurJob);
                Wait(buyer);
                Check(buyer.inventory.innerContainer.Where(t => t.def == ThingDefOf.Steel).Sum(t => t.stackCount) == 3 && steel.stackCount == 17, "Goods duplicated or missing");
                Check(HotelBillingHooks.Money(buyer) == 500 - price && Floor() == initialFloor && Sales.earned == 0 && o.GetDirectlyHeldThings().Sum(t => t.stackCount) == price, "Unplaced receipts counted as income: floor="+Floor()+" initial="+initialFloor+" earned="+Sales.earned+" cash="+o.GetDirectlyHeldThings().Sum(t=>t.stackCount));
                Check(!Sales.Buy(o), "Repeated purchase was accepted");
                bookcase.GetComp<CompComplexSale>().forSale = true;
                var pending = Sales.Create(second, bookcase, book, 1); Check(pending != null, "Container sale could not be booked");
                bookOrderId = pending.id; bookPrice = pending.price; Start(pending);
                saved = true; GameDataSaveLoader.SaveGame("TMM-Sales"); Finish(false); return;
            }
            if (reload && stage == 0) {
                Check(saved && desk != null && buyer != null && second != null && book != null, "Sales save references lost");
                Check(shelf.GetComp<CompComplexSale>().forSale && bookcase.GetComp<CompComplexSale>().forSale && !emptyShelf.GetComp<CompComplexSale>().forSale, "Sale flags changed on load");
                // Isolate the scripted balance checks. Otherwise the visitor
                // who has just bought the book may independently buy steel
                // while the other guest walks to the outfit stand.
                shelf.GetComp<CompComplexSale>().forSale = false;
                var o = Sales.ById(saleId);
                Check(Sales.earned + (o == null ? 0 : o.GetDirectlyHeldThings().Sum(t => t.stackCount)) == price, "Saved receipt lost/duplicated");
                Check(HotelBillingHooks.Money(buyer) == 500 - price && buyer.inventory.innerContainer.Where(t => t.def == ThingDefOf.Steel).Sum(t => t.stackCount) == 3, "Saved goods or money lost");
                var pending = Sales.ById(bookOrderId); Check(pending != null && pending.item == book && pending.price == bookPrice, "Saved quote or container reference lost");
                Check(book.thingIDNumber == bookId && book.Label == bookLabel, "Book identity/metadata changed on load");
                Sales.Process(); Sales.Process(); Check(Sales.earned == price && Floor() == initialFloor + price, "Settlement duplicated floor silver or income");
                started = Find.TickManager.TicksGame; stage = 2;
            }
            if (reload && stage == 2) {
                if (!second.inventory.innerContainer.Contains(book) && Find.TickManager.TicksGame - started < 2000) return;
                Check(second.inventory.innerContainer.Contains(book) && !SaleStock.Contents(bookcase).Contains(book), "Real book withdrawal failed: " + second.CurJob);
                Check(book.thingIDNumber == bookId && book.Label == bookLabel, "Purchased book metadata changed");
                Sales.Process(); Sales.Process();
                Check(HotelBillingHooks.Money(second) == 500 - bookPrice && Sales.earned == price + bookPrice && Floor() == initialFloor + price + bookPrice, "Book trade did not conserve money");
                Check(Sales.ById(bookOrderId) == null, "Completed order retained active state");
                Check(desk.GetInspectString().Contains(Sales.earned.ToString()), "Reception sales income missing");
                outfit.GetComp<CompComplexSale>().forSale = true; Wait(buyer);
                var clothing = Sales.Create(buyer, outfit, shirt, 1); Check(clothing != null, "Outfit sale could not be booked");
                shirtPrice = clothing.price; Start(clothing); started = Find.TickManager.TicksGame; stage = 3;
            }
            if (reload && stage == 3) {
                if (!buyer.inventory.innerContainer.Contains(shirt) && Find.TickManager.TicksGame - started < 2000) return;
                Check(buyer.inventory.innerContainer.Contains(shirt) && !SaleStock.Contents(outfit).Contains(shirt), "Real outfit withdrawal failed");
                QualityCategory quality; Check(shirt.TryGetQuality(out quality) && quality == QualityCategory.Excellent, "Outfit quality was lost");
                Sales.Process(); Sales.Process();
                Check(HotelBillingHooks.Money(buyer) == 500 - price - shirtPrice && Sales.earned == price + bookPrice + shirtPrice && Floor() == initialFloor + price + bookPrice + shirtPrice,
                    "Outfit trade did not conserve money: buyer="+HotelBillingHooks.Money(buyer)+" expected="+(500-price-shirtPrice)+" earned="+Sales.earned+" expectedEarned="+(price+bookPrice+shirtPrice)+" floor="+Floor()+" expectedFloor="+(initialFloor+price+bookPrice+shirtPrice));
                adaptive=Spawn("sbz_Crate",new IntVec3(55,0,49));
                adaptiveStock=ThingMaker.MakeThing(ThingDefOf.Steel);adaptiveStock.stackCount=10;GenSpawn.Spawn(adaptiveStock,adaptive.Position,map);
                adaptive.GetComp<CompComplexSale>().forSale=true;
                Check(SaleStock.Contents(adaptive).Contains(adaptiveStock),"Adaptive Storage contents not visible to sales");
                Wait(buyer);var storageOrder=Sales.Create(buyer,adaptive,adaptiveStock,2);Check(storageOrder!=null,"Adaptive Storage sale could not be booked");
                adaptivePrice=storageOrder.price;Start(storageOrder);started=Find.TickManager.TicksGame;stage=4;
            }
            if(reload&&stage==4){
                if(buyer.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Steel).Sum(t=>t.stackCount)<5&&Find.TickManager.TicksGame-started<2000)return;
                Check(adaptiveStock.stackCount==8&&buyer.inventory.innerContainer.Where(t=>t.def==ThingDefOf.Steel).Sum(t=>t.stackCount)==5,"Adaptive Storage stock duplicated or missing");
                Sales.Process();Sales.Process();
                Check(HotelBillingHooks.Money(buyer)==500-price-shirtPrice-adaptivePrice&&Sales.earned==price+bookPrice+shirtPrice+adaptivePrice&&Floor()==initialFloor+price+bookPrice+shirtPrice+adaptivePrice,"Adaptive Storage sale did not conserve money");
                Finish(true);
            }
        } catch (Exception ex) { errors.Add(ex.ToString()); Finish(reload); }
    }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private void Finish(bool reload) { reported = true; File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, reload ? "SalesReloadProbe.json" : "SalesProbe.json"), "{\"passed\":" + (errors.Count == 0 ? "true" : "false") + ",\"checks\":" + checks + ",\"errors\":[" + string.Join(",", errors.Select(Escape)) + "]}"); }
    public override void MapComponentOnGUI()
    {
        if (uiReported || !reported || shelf == null || Event.current.type != EventType.Repaint || !Environment.GetCommandLineArgs().Contains("-tmm-sales-probe")) return;
        uiReported = true; string error = null;
        try { var commands=shelf.GetGizmos().OfType<Command_Toggle>().Where(g => g.defaultLabel == "TMM.Sales.ForSale".Translate().ToString()).ToArray();
            if(commands.Length!=1)throw new Exception("Missing or duplicate sale toggle");
            foreach (var g in commands) g.GizmoOnGUI(new Vector2(20, 20), 150, default(GizmoRenderParms)); }
        catch (Exception ex) { error = ex.ToString(); }
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "SalesUiProbe.json"), "{\"passed\":" + (error == null ? "true" : "false") + ",\"visualLayoutInspected\":false,\"error\":" + (error == null ? "null" : Escape(error)) + "}");
    }
}
