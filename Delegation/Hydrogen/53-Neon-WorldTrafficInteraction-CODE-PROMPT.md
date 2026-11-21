**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

## Порядок ответа (важно)
Один файл на ответ, без вступлений, потом жди «дальше». Части называй `53_1`, `53_2`… Над каждым блоком — путь файла.

## Задача 53 — живые караваны фракций: торговля и нападение
В моде уже есть мировой трафик (код ниже): караваны фракций ходят между городами как настоящие караваны на карте мира, с грузом из складов (`WorldTrafficRoute`, `WorldTrafficDepot`). Игроку нечем с ними взаимодействовать. Нужно:

1. **Кнопки у каравана игрока**, когда он стоит на одной клетке с караваном трафика (`Caravan` с маршрутом в `WorldTraffic`): постфикс `Caravan.GetGizmos`, кнопка на каждый караван трафика на клетке.
   - **«Торговать»** — если фракция не враждебна. Открыть ванильное окно торговли: `TradeSession.SetupWith(trader, negotiator, false)`, затем `Dialog_Trade`. Торговец — лидер каравана трафика (`Pawn` с `trader = new Pawn_TraderTracker(pawn)` и `traderKind` из `faction.def.caravanTraderKinds`). Товары — **реальный груз** каравана трафика (инвентари его пешек), без генерации новых вещей. Проданное игроком попадает в их инвентари, купленное — переносится из их инвентарей. Переговорщик — лучший по социальным навыкам в караване игрока.
   - **«Напасть»** — всегда, с подтверждением (`Dialog_MessageBox`): отношения падают (`TryAffectGoodwillWith` с `HistoryEventDefOf.AttackedCaravan` или аналогом). Карта боя — ванильная `CaravanIncidentUtility.SetupCaravanAttackMap(playerCaravan, enemies, true)`, где `enemies` — пешки каравана трафика: их сначала снять с их каравана, а сам караван трафика корректно завершить через `WorldTraffic.BeforeRemoval` и удаление маршрута. Груз остаётся у пешек (выпадет как добыча).
   - Подсказка на кнопке: фракция, сколько людей, примерный вес груза.
2. **Без дыр:** караван трафика, уже начавший бой или удалённый, не должен давать кнопки. Проверки `Destroyed`, `Spawned`, тайл совпадает, сторона не игрок. Повторное нажатие не дублирует пешек.
3. **Локализация:** ключи `TMM_TrafficTrade`, `TMM_TrafficAttack`, `TMM_TrafficAttackConfirm` (с `{0}` — фракция), `TMM_TrafficTooltip` и другие — EN и RU (`Languages/English/Keyed/WorldTrafficInteraction.xml`, `Languages/Russian/Keyed/...`).
4. **Проба** `Tests/InGameWorldTrafficInteractionProbe.cs` (флаг `-tmm-traffic-interaction-probe`):
   - создать караван игрока на клетке с караваном трафика (через `WorldTraffic.TryStart` или вручную);
   - кнопки появились; торговля открывается и сессия настроена на реальный груз;
   - после продажи и покупки (через `Tradeable` API) вещи перешли без задвоения;
   - нападение создаёт карту, враги — те же пешки, маршрут удалён, ошибок нет.
   Отчёт `WorldTrafficInteractionProbe.json` (passed по факту) и `WorldTrafficInteractionUiProbe.json`. **Не вызывай** `Root.Shutdown` — игру закрывает тестовая обвязка.

## Правила кода
C# 5 (без `?.`, `nameof`, интерполяции строк, `=>` у свойств), RimWorld 1.7, Harmony. Патчи — через `new Harmony("colonyproject.integrated.traffic.interaction")` в `[StaticConstructorOnStartup]`. `// CHECK API` где не уверен.

## Порядок частей
1. `Source/WorldTrafficInteraction.cs`
2. `Languages/English/Keyed/WorldTrafficInteraction.xml` и `Languages/Russian/Keyed/WorldTrafficInteraction.xml` (два блока в одном ответе)
3. `Tests/InGameWorldTrafficInteractionProbe.cs`

## Текущий код: Source/WorldTraffic.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class WorldTrafficRoute : IExposable
    {
        public Caravan caravan;
        public Settlement origin, destination;
        public int created, deadline, retryAt, retries;
        public bool patrol, returning, stranded;
        public void ExposeData()
        {
            Scribe_References.Look(ref caravan, "caravan");
            Scribe_References.Look(ref origin, "origin");
            Scribe_References.Look(ref destination, "destination");
            Scribe_Values.Look(ref created, "created");
            Scribe_Values.Look(ref deadline, "deadline");
            Scribe_Values.Look(ref retryAt, "retryAt");
            Scribe_Values.Look(ref retries, "retries");
            Scribe_Values.Look(ref patrol, "patrol");
            Scribe_Values.Look(ref returning, "returning");
            Scribe_Values.Look(ref stranded, "stranded");
        }
    }

    // Fallback for settlements without a native trading stock; these real goods remain owned/saved.
    public sealed class WorldTrafficDepot : IExposable, IThingHolder
    {
        public Settlement settlement;
        public ThingOwner<Thing> goods;
        public WorldTrafficDepot() { goods = new ThingOwner<Thing>(this); }
        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return goods; }
        public void GetChildHolders(List<IThingHolder> children) { ThingOwnerUtility.AppendThingHoldersFromThings(children, goods); }
        public void ExposeData()
        {
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Deep.Look(ref goods, "goods", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && goods == null) goods = new ThingOwner<Thing>(this);
        }
    }

    public sealed class WorldTraffic : WorldComponent
    {
        public const int MaxGroups = 16, Interval = 6000, Retry = 2500;
        public List<WorldTrafficRoute> routes = new List<WorldTrafficRoute>();
        public List<WorldTrafficDepot> depots = new List<WorldTrafficDepot>();
        public int nextSpawn = -1, departed, arrived;
        private int nextCheck;
        private readonly HashSet<int> warnedFactions = new HashSet<int>();
        private static readonly FieldInfo StockField = AccessTools.Field(typeof(Settlement_TraderTracker), "stock");
        public WorldTraffic(World world) : base(world) { }
        public WorldTrafficRoute Route(Caravan caravan) { return routes.FirstOrDefault(r => r.caravan == caravan); }
        private static bool Present(WorldObject obj) { return obj != null && obj.Spawned; }
        private static bool Human(PawnKindDef kind) { return kind != null && kind.RaceProps.Humanlike && kind.RaceProps.ToolUser; }
        private static IEnumerable<PawnGenOption> Options(PawnGroupMaker maker)
        {
            return (maker.options ?? new List<PawnGenOption>()).Concat(maker.traders ?? new List<PawnGenOption>())
                .Concat(maker.guards ?? new List<PawnGenOption>()).Where(o => o != null && Human(o.kind) && o.selectionWeight > 0 && !float.IsNaN(o.selectionWeight) && !float.IsInfinity(o.selectionWeight));
        }
        public static bool Eligible(Faction faction)
        {
            return faction != null && !faction.IsPlayer && !faction.defeated && faction.def.humanlikeFaction && !faction.def.hidden &&
                faction.def.pawnGroupMakers != null && faction.def.pawnGroupMakers.Any(m => m != null && m.commonality > 0 && Options(m).Any());
        }
        private static Pawn MakePawn(PawnKindDef kind, Faction faction)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: false, allowAddictions: false, allowPregnant: false, developmentalStages: DevelopmentalStage.Adult));
        }
        private static bool AddStack(Pawn pawn, ThingDef def, int count)
        {
            if (pawn.inventory == null || count <= 0) return false;
            int remaining = count;
            while (remaining > 0)
            {
                var item = ThingMaker.MakeThing(def); item.stackCount = Math.Min(remaining, def.stackLimit);
                int amount = item.stackCount;
                if (!pawn.inventory.innerContainer.TryAdd(item, false)) { item.Destroy(); return false; }
                remaining -= amount;
            }
            return true;
        }
        private static ThingOwner<Thing> NativeStock(Settlement settlement)
        {
            if (settlement == null || settlement.trader == null || StockField == null || settlement.TraderKind == null) return null;
            // An initialized empty stock is still real. Its getter would immediately mint a new
            // native batch merely because we depleted it; only initialize after native expiry/null.
            var existing = StockField.GetValue(settlement.trader) as ThingOwner<Thing>;
            if (existing != null) return existing;
            var list = settlement.trader.StockListForReading;
            return StockField.GetValue(settlement.trader) as ThingOwner<Thing>;
        }
        public ThingOwner<Thing> Depot(Settlement settlement)
        {
            var depot = depots.FirstOrDefault(d => d.settlement == settlement);
            if (depot == null) { depot = new WorldTrafficDepot { settlement = settlement }; depots.Add(depot); }
            return depot.goods;
        }
        private static void LoadCargo(Caravan caravan, Settlement origin, bool patrol)
        {
            var stock = NativeStock(origin);
            if (stock == null) return; // Patrol has its actual generated personal equipment/rations, no fake merchandise.
            int budget = patrol ? 100 : 600;
            int goodsStacks = 0;
            foreach (var item in stock.InnerListForReading.ToArray())
            {
                if (item == null || item.Destroyed || item.def.category != ThingCategory.Item || item.def.tradeability == Tradeability.None) continue;
                if (item.def != ThingDefOf.Silver && goodsStacks >= 4) continue;
                int wanted = item.def == ThingDefOf.Silver ? Math.Min(budget, item.stackCount) : Math.Min(12, item.stackCount);
                if (wanted <= 0) continue;
                var holder = caravan.PawnsListForReading.FirstOrDefault(p => p.inventory != null && MassUtility.FreeSpace(p) > wanted * item.GetStatValue(StatDefOf.Mass) + 3f);
                if (holder == null) continue;
                int moved = stock.TryTransferToContainer(item, holder.inventory.innerContainer, wanted, false);
                if (moved > 0) { if (item.def == ThingDefOf.Silver) budget -= moved; else goodsStacks++; }
                if (budget <= 0 && goodsStacks >= 4) break;
            }
        }
        public WorldTrafficRoute TryStart(Settlement origin, Settlement destination, bool patrol = false)
        {
            if (!Present(origin) || !Present(destination) || origin == destination || !Eligible(origin.Faction) || destination.Faction == null || destination.Faction.IsPlayer || destination.Faction.defeated ||
                origin.Faction.HostileTo(destination.Faction) || origin.Tile.Layer != Find.WorldGrid.Surface || destination.Tile.Layer != Find.WorldGrid.Surface || routes.Count >= MaxGroups ||
                routes.Count(r => r.caravan != null && r.caravan.Faction == origin.Faction) >= 2 || !world.reachability.CanReach(origin.Tile, destination.Tile)) return null;
            var faction = origin.Faction;
            var makers = faction.def.pawnGroupMakers.Where(m => m != null && m.commonality > 0 && Options(m).Any()).ToList();
            var maker = makers.FirstOrDefault(m => !patrol && m.kindDef == PawnGroupKindDefOf.Trader) ?? makers.RandomElement();
            var options = Options(maker).ToList();
            var leaderKind = !patrol && maker.traders != null ? maker.traders.Where(o => o != null && Human(o.kind) && o.selectionWeight > 0).Select(o => o.kind).FirstOrDefault() : null;
            var pawns = new List<Pawn>(); Caravan caravan = null; bool committed = false;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var kind = i == 0 && leaderKind != null ? leaderKind : options.RandomElementByWeight(o => o.selectionWeight).kind;
                    var pawn = MakePawn(kind, faction); pawns.Add(pawn);
                    if (pawn.Dead || pawn.Downed || pawn.inventory == null || !pawn.RaceProps.Humanlike) return null;
                    // Supplies are generated once with the native NPC journey, not again at loads or retries.
                    if (!AddStack(pawn, ThingDefOf.MealSurvivalPack, 16)) return null;
                }
                if (!patrol && faction.def.caravanTraderKinds != null && faction.def.caravanTraderKinds.Count > 0)
                {
                    if (pawns[0].trader == null) pawns[0].trader = new Pawn_TraderTracker(pawns[0]);
                    pawns[0].trader.traderKind = faction.def.caravanTraderKinds.First(t => t != null);
                }
                caravan = CaravanMaker.MakeCaravan(pawns, faction, origin.Tile, true);
                if (pawns.Any(p => !caravan.ContainsPawn(p)) || !caravan.pather.StartPath(destination.Tile, null, true, false)) return null;
                int now = Find.TickManager.TicksGame;
                var route = new WorldTrafficRoute { caravan = caravan, origin = origin, destination = destination, patrol = patrol,
                    created = now, deadline = now + 60000 * 12, retryAt = now + Retry };
                routes.Add(route); departed++; committed = true;
                // The caravan already has a persisted controller before stock is moved. A foreign
                // stock generator failure cannot discard a partially loaded physical shipment.
                try { LoadCargo(caravan, origin, patrol); }
                catch (Exception ex) { WarnOnce(faction, ex); }
                return route;
            }
            catch (Exception ex) { WarnOnce(faction, ex); return null; }
            finally
            {
                if (!committed)
                {
                    if (caravan != null && caravan.Spawned) Find.WorldObjects.Remove(caravan);
                    foreach (var pawn in pawns)
                    {
                        if (caravan != null && caravan.ContainsPawn(pawn)) caravan.RemovePawn(pawn);
                        if (!pawn.Spawned && !Find.WorldPawns.Contains(pawn)) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    }
                }
            }
        }
        private void WarnOnce(Faction faction, Exception ex)
        {
            if (warnedFactions.Add(faction.loadID)) Log.Warning("[Turbo mega mod] World traffic skipped unsupported generation for " + faction.Name + ": " + ex.Message);
        }
        public bool TrySpawn()
        {
            if (routes.Count >= MaxGroups) return false;
            var settlements = Find.WorldObjects.Settlements.Where(s => Eligible(s.Faction) && s.Tile.Layer == Find.WorldGrid.Surface && !s.HasMap).ToList();
            if (settlements.Count < 2) return false;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var source = settlements.RandomElement();
                if (routes.Count(r => r.caravan != null && r.caravan.Faction == source.Faction) >= 2) continue;
                var targets = settlements.Where(s => s != source && !source.Faction.HostileTo(s.Faction) &&
                    Find.WorldGrid.ApproxDistanceInTiles(source.Tile, s.Tile) >= 3 && Find.WorldGrid.ApproxDistanceInTiles(source.Tile, s.Tile) <= 55).ToList();
                if (targets.Count == 0) continue;
                if (TryStart(source, targets.RandomElement(), source.Faction.HostileTo(Faction.OfPlayer)) != null) return true;
            }
            return false;
        }
        private bool Retire(WorldTrafficRoute route, Settlement recipient)
        {
            if (recipient == null || !Present(recipient)) return false;
            var caravan = route.caravan;
            var stock = NativeStock(recipient) ?? Depot(recipient);
            // Transfer every remaining inventory item before releasing any pawn or world object.
            foreach (var pawn in caravan.PawnsListForReading.ToArray())
            {
                if (pawn.inventory == null) continue;
                foreach (var item in pawn.inventory.innerContainer.InnerListForReading.ToArray())
                    if (pawn.inventory.innerContainer.TryTransferToContainer(item, stock, item.stackCount, true) <= 0) return false;
                if (pawn.inventory.innerContainer.Count > 0) return false;
            }
            foreach (var pawn in caravan.PawnsListForReading.ToArray()) caravan.RemovePawn(pawn);
            routes.Remove(route); // An intentional empty arrival, not a forced-removal salvage event.
            Find.WorldObjects.Remove(caravan); arrived++;
            return true;
        }
        public void RecoverInventory(Caravan caravan, Pawn dying = null)
        {
            if (caravan == null || caravan.Faction == Faction.OfPlayer || Route(caravan) == null || !caravan.Tile.Valid) return;
            bool lastOwner = dying == null || !caravan.PawnsListForReading.Any(p => p != dying && !p.Dead && caravan.IsOwner(p));
            var pawns = caravan.PawnsListForReading.Where(p => lastOwner || p == dying).ToArray();
            if (dying != null && !pawns.Contains(dying)) pawns = pawns.Concat(new[] { dying }).ToArray();
            var items = pawns.Where(p => p != null && p.inventory != null).SelectMany(p => p.inventory.innerContainer.InnerListForReading).ToArray();
            if (items.Length == 0) return;
            var cache = Find.WorldObjects.AllWorldObjects.OfType<WorldTrafficCargo>().FirstOrDefault(c => c.Tile == caravan.Tile && c.originalFaction == caravan.Faction);
            if (cache == null)
            {
                cache = (WorldTrafficCargo)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TMM_WorldTrafficCargo"));
                cache.Tile = caravan.Tile; cache.originalFaction = caravan.Faction; Find.WorldObjects.Add(cache);
            }
            foreach (var item in items)
            {
                var owner = item.holdingOwner;
                if (owner != null) owner.TryTransferToContainer(item, cache.goods, item.stackCount, true);
            }
        }
        public void BeforeRemoval(Caravan caravan)
        {
            if (Route(caravan) == null || caravan.Faction == Faction.OfPlayer) return;
            RecoverInventory(caravan);
            foreach (var pawn in caravan.PawnsListForReading.ToArray())
                if (!pawn.Spawned && (pawn.inventory == null || pawn.inventory.innerContainer.Count == 0)) caravan.RemovePawn(pawn);
            // Reference cleanup is deferred to ProcessRoutes; native removal completes first.
        }
        public void ProcessRoutes()
        {
            int now = Find.TickManager.TicksGame;
            foreach (var route in routes.ToArray())
            {
                var caravan = route.caravan;
                if (!Present(caravan)) { RecoverInventory(caravan); routes.Remove(route); continue; }
                if (caravan.Faction == Faction.OfPlayer) { routes.Remove(route); continue; }
                if (caravan.Faction == null) { BeforeRemoval(caravan); Find.WorldObjects.Remove(caravan); routes.Remove(route); continue; }
                if (caravan.PawnsListForReading.Count == 0) { Find.WorldObjects.Remove(caravan); routes.Remove(route); continue; }
                if (route.stranded && !caravan.pather.Moving && now < route.retryAt) continue;
                var target = route.returning ? route.origin : route.destination;
                bool validTarget = Present(target) && target.Faction != null && !caravan.Faction.HostileTo(target.Faction);
                if (validTarget && caravan.Tile == target.Tile) { Retire(route, target); continue; }
                bool changedTarget = false;
                if (!validTarget || (!route.returning && now >= route.deadline))
                {
                    route.returning = true; target = route.origin;
                    if (!Present(target) || target.Faction == null || caravan.Faction.HostileTo(target.Faction))
                        target = Find.WorldObjects.Settlements.Where(s => s.Faction == caravan.Faction && s.Tile.Layer == caravan.Tile.Layer)
                            .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(caravan.Tile, s.Tile)).Take(8).FirstOrDefault(s => world.reachability.CanReach(caravan.Tile, s.Tile));
                    route.origin = target;
                    if (target == null) { route.stranded = true; route.retryAt = now + 60000; caravan.pather.StopDead(); continue; }
                    if (caravan.Tile == target.Tile) { Retire(route, target); continue; }
                    changedTarget = true;
                }
                if (changedTarget) { caravan.pather.StopDead(); route.retryAt = now; route.retries = 0; }
                if (caravan.pather.Moving || now < route.retryAt) continue;
                route.retryAt = now + Retry;
                if (target == null || !caravan.pather.StartPath(target.Tile, null, true, false))
                {
                    route.retries++;
                    if (route.retries >= 3)
                    {
                        if (!route.returning) { route.returning = true; route.retryAt = now + Retry; route.retries = 0; }
                        else route.retryAt = now + 60000;
                        route.stranded = true;
                    }
                }
                else { route.stranded = false; route.retries = 0; }
            }
        }
        public override void WorldComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now < nextCheck) return; nextCheck = now + 250;
            ProcessRoutes();
            if (nextSpawn < 0) nextSpawn = now + Retry;
            if (now < nextSpawn) return;
            nextSpawn = now + (TrySpawn() ? Interval : Retry);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref routes, "TMM_worldTrafficRoutes", LookMode.Deep);
            Scribe_Collections.Look(ref depots, "TMM_worldTrafficDepots", LookMode.Deep);
            Scribe_Values.Look(ref nextSpawn, "TMM_worldTrafficNext", -1);
            Scribe_Values.Look(ref departed, "TMM_worldTrafficDeparted");
            Scribe_Values.Look(ref arrived, "TMM_worldTrafficArrived");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                routes = (routes ?? new List<WorldTrafficRoute>()).Where(r => r != null && r.caravan != null).GroupBy(r => r.caravan).Select(g => g.First()).ToList();
                depots = (depots ?? new List<WorldTrafficDepot>()).Where(d => d != null).ToList();
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class WorldTrafficInspect
    {
        static WorldTrafficInspect()
        {
            var harmony = new Harmony("turbomegamod.world.traffic");
            harmony.Patch(AccessTools.Method(typeof(Caravan), "GetInspectString"), postfix: new HarmonyMethod(typeof(WorldTrafficInspect), "Inspect"));
            harmony.Patch(AccessTools.Method(typeof(WorldObject), "GetCaravanGizmos"), postfix: new HarmonyMethod(typeof(WorldTrafficInspect), "Gizmos"));
            harmony.Patch(AccessTools.Method(typeof(Caravan), "Notify_MemberDied"), prefix: new HarmonyMethod(typeof(WorldTrafficInspect), "Died"));
            harmony.Patch(AccessTools.Method(typeof(WorldObjectsHolder), "Remove"), prefix: new HarmonyMethod(typeof(WorldTrafficInspect), "Removing"));
        }
        private static void Died(Caravan __instance, Pawn __0)
        {
            var component = Find.World == null ? null : Find.World.GetComponent<WorldTraffic>();
            if (component != null) component.RecoverInventory(__instance, __0);
        }
        private static void Removing(WorldObject __0)
        {
            var caravan = __0 as Caravan;
            if (caravan == null) return;
            var component = Find.World == null ? null : Find.World.GetComponent<WorldTraffic>();
            if (component != null) component.BeforeRemoval(caravan);
        }
        public static WorldTrafficRoute Route(Caravan caravan)
        {
            var component = Find.World == null ? null : Find.World.GetComponent<WorldTraffic>();
            return component == null || caravan == null ? null : component.Route(caravan);
        }
        private static void Inspect(Caravan __instance, ref string __result)
        {
            var route = Route(__instance); if (route == null) return;
            int firstLine = __result.IndexOf('\n');
            if (firstLine >= 0) __result = "TMM.WorldTraffic.Crew".Translate(__instance.PawnsListForReading.Count) + __result.Substring(firstLine);
            string purpose = (route.patrol ? "TMM.WorldTraffic.Patrol" : "TMM.WorldTraffic.Convoy").Translate();
            string target = (route.returning ? route.origin : route.destination) == null ? "TMM.WorldTraffic.Unknown".Translate().RawText : (route.returning ? route.origin : route.destination).LabelCap;
            __result += "\n" + "TMM.WorldTraffic.Route".Translate(purpose, target);
            if (route.stranded) __result += "\n" + "TMM.WorldTraffic.Stranded".Translate();
        }
        private static void Gizmos(WorldObject __instance, Caravan caravan, ref IEnumerable<Gizmo> __result)
        {
            var npc = __instance as Caravan;
            if (Route(npc) == null || caravan == null || caravan.Faction != Faction.OfPlayer || npc.Tile != caravan.Tile ||
                npc.Faction.HostileTo(Faction.OfPlayer) || !npc.CanTradeNow) return;
            var negotiator = caravan.PawnsListForReading.Where(p => p.IsFreeColonist && !p.Downed && p.skills != null &&
                !p.WorkTagIsDisabled(WorkTags.Social) && p.health.capacities.CapableOf(PawnCapacityDefOf.Talking)).OrderByDescending(p => p.skills.GetSkill(SkillDefOf.Social).Level).FirstOrDefault();
            if (negotiator == null) return;
            var command = new Command_Action { defaultLabel = "TMM.WorldTraffic.Trade".Translate(), defaultDesc = "TMM.WorldTraffic.TradeHelp".Translate(), icon = TexButton.Info };
            command.action = () => { if (npc.Spawned && caravan.Spawned && npc.Tile == caravan.Tile && !npc.Faction.HostileTo(Faction.OfPlayer) && npc.CanTradeNow) Find.WindowStack.Add(new Dialog_Trade(negotiator, npc)); };
            __result = __result.Concat(new[] { command });
        }
    }
}
```
## Текущий код: Source/WorldTrafficCargo.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    // Actual salvage on the tile where a carrier died or its caravan was forcibly removed.
    public sealed class WorldTrafficCargo : WorldObject, IThingHolder
    {
        public Faction originalFaction;
        public ThingOwner<Thing> goods;
        public WorldTrafficCargo() { goods = new ThingOwner<Thing>(this); }
        public ThingOwner GetDirectlyHeldThings() { return goods; }
        public void GetChildHolders(List<IThingHolder> children) { ThingOwnerUtility.AppendThingHoldersFromThings(children, goods); }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref originalFaction, "originalFaction");
            Scribe_Deep.Look(ref goods, "cargo", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && goods == null) goods = new ThingOwner<Thing>(this);
        }
        public override string GetInspectString()
        {
            return "TMM.WorldTraffic.AbandonedCargo".Translate(goods.Sum(t => t.stackCount));
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            foreach (var gizmo in base.GetCaravanGizmos(caravan)) yield return gizmo;
            if (caravan == null || caravan.Faction != Faction.OfPlayer || caravan.Tile != Tile) yield break;
            yield return new Command_Action
            {
                defaultLabel = "TMM.WorldTraffic.Recover".Translate(), defaultDesc = "TMM.WorldTraffic.RecoverHelp".Translate(), icon = TexButton.Drop,
                action = () => Recover(caravan)
            };
        }
        public void Recover(Caravan caravan)
        {
            if (caravan == null || !caravan.Spawned || caravan.Faction != Faction.OfPlayer || caravan.Tile != Tile) return;
            foreach (var item in goods.InnerListForReading.ToArray())
            {
                foreach (var pawn in caravan.PawnsListForReading.Where(p => p.inventory != null && MassUtility.CanEverCarryAnything(p)))
                {
                    float unitMass = item.GetStatValue(StatDefOf.Mass);
                    int count = unitMass <= 0 ? item.stackCount : Math.Min(item.stackCount, (int)Math.Floor(Math.Max(0, MassUtility.FreeSpace(pawn)) / unitMass));
                    if (count > 0) goods.TryTransferToContainer(item, pawn.inventory.innerContainer, count, true);
                    if (!goods.Contains(item)) break;
                }
            }
            if (goods.Count == 0 && Spawned) Find.WorldObjects.Remove(this);
        }
    }
}
```
