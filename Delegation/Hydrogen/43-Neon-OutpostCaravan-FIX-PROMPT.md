**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

Пешие караваны к аванпостам (вкладка «Аванпосты»: отправить рабочих и серебро, носильщики возвращаются домой) написал агент Antigravity. Codex провёл приёмку: **«НЕ ПРИНИМАТЬ»**. Ниже его отчёт целиком и текущий код. **Исправь все находки P1 и P2.** Правила: C# 5, RimWorld 1.7, `// CHECK API` там, где не уверен. Публичный API, который вызывают окно и вкладка, сохрани или явно перечисли изменения.

## Принципы исправления
- **Одна миссия — один конкретный караван.** Миссия создаётся вместе с формированием и привязывается к его lord (`LordJob_FormAndSendCaravan`) или к постоянному токену. Глобальный postfix и опрос не ищут «первый караван с любой из пешек». После того как караван сформирован, повторная привязка невозможна, в том числе после reload.
  - Если формирование сорвалось (lord завершился без выхода), миссия удаляется, груз остаётся дома.
  - Если караван уничтожен или исчез — миссия закрывается.
  - Чистка (null-элементы, пустые, сорванные, уничтоженные) — до сохранения и после загрузки.
- **Груз закреплён за миссией.** Серебро и еду для старта бери из одного набора стеков — тех, что ваниль реально может погрузить. Проверь, что погружено именно столько. Если меньше — отмена с сообщением. В миссии храни ID погруженных стеков или фактическую сумму.
- **Сдача на аванпосте:**
  1. проверь, что миссия ещё активна и не сдана — повторный вход `Arrived` ничего не делает;
  2. проверь аванпост и его сторону;
  3. снимай только груз миссии, не личные вещи;
  4. результат `TryAddOrTransfer` проверяй; при отказе верни всё в исходные инвентари;
  5. `delivered` ставь только после подтверждения.
- **Обратный путь — отдельное состояние.** Добавь состояние `Returning`, оно живёт до входа носильщиков в колонию.
  - Аванпост пропал по дороге туда → сразу разворот домой с грузом.
  - Дом недоступен или `StartPath` отказал → караван остаётся на месте, игрок получает сообщение, на караване есть кнопка «Домой».
  - Отмена фиксируется только после подтверждённой смены маршрута.
- **Рабочие:**
  - после `RemovePawn` проверь holder; результат спавна или вставки сверяй;
  - откат делай и при исключении (`try/finally`);
  - перед переводом проверь, что в караване останется хотя бы один носильщик, способный идти;
  - провиант уходящих рабочих раздели заранее: подтверждённый запас на обратный путь остаётся носильщикам.
- **Еда:**
  - считай по `TransferableOneWay` с фактическим количеством к погрузке, а не по полным стекам;
  - туда — весь состав с грузом; обратно — только носильщики с реальным запасом после передачи;
  - не просто удваивай один путь.
- **Нагрузка:**
  - привязка — по событию;
  - резервная сверка и чистка — раз в 250–2500 тиков, не каждый тик;
  - окно кеширует путь и оценку еды до изменения состава или груза, а не считает на каждом Layout/Repaint;
  - `WorldPath` освобождай ровно один раз.

## Отчёт Codex
### OutpostCaravanReview — НЕ ПРИНИМАТЬ

## Куда смотреть Claude
- **P1 · Серебро из чужого для миссии груза и потеря на отказе** — `Source/OutpostCaravanLogistics.cs:95–113,466–471,563–582`. Проверка считает все незапрещённые серебряные стеки, а погрузка берёт лишь допустимые для ванильного формирования. Если 200 монет вне домашней зоны или зарезервированы, остаток remainingSilver не проверяется и миссия всё равно требует 200. На финише TakeThings отделяет монеты из любого инвентаря каравана, включая личный остаток. TryAddOrTransfer может отказать, его результат игнорируется, снятые монеты уже без владельца. Для старта использовать один набор допустимых стеков, закрепить фактически погруженный груз за миссией. На клетке аванпоста проверить сторону, груз и состояние миссии, подтверждать перенос только по результату, при отказе вернуть всё в исходные инвентари. Повторный вход Arrived не должен повторно принимать уже сданный груз.
- **P1 · Носильщики могут остаться в пути, отмена расходится с состоянием маршрута** — `Source/OutpostCaravanLogistics.cs:75–93,132–148,323–327,646–669`. При исчезновении аванпоста ванильный PatherTickInterval вызывает StopDead и очищает arrivalAction, а менеджер выкидывает миссию. Груз остаётся в караване, но автоворота и кнопки уже нет. После сдачи при отсутствии дома или отказе StartPath миссия всё равно помечается delivered. При отмене результат StartPath также игнорируется. Хранить состояние возврата до входа в колонию, при пропаже цели сразу пытаться вернуть груз домой. При недоступном доме оставить караван на карте с сообщением и доступной командой. Фиксировать отмену только после подтверждённой смены маршрута.
- **P1 · Незавершённая миссия может перехватить другой поход** — `Source/OutpostCaravanLogistics.cs:277–303,310–375,679–687`. Глобальный Harmony postfix и опрос привязывают первый караван с любой из исходных пешек без проверки конкретного формирования, точки выхода и маршрута. Если первоначальное формирование сорвалось и одна пешка покинула карту или умерла, тест All(Spawned) не удаляет запись. Позже обычный караван с оставшимся участником перенаправляется и может заплатить серебром из своего груза. При гибели уже связанного каравана отсутствует очистка по Destroyed/!Spawned, а null после reload позволяет привязаться заново. Элементы null в списке также небезопасны в All. Привязать миссию к конкретному lord формирования либо его постоянному токену. Чистить сорванные, пустые и уничтоженные миссии до сохранения и после загрузки, без повторной привязки уже сформированных.
- **P2 · Передача рабочего безопасна при штатном отказе по вместимости, но не при сбое переноса** — `Source/StrategicOutpost.cs:972–1005,1034–1071`, `Source/OutpostCaravanLogistics.cs:115–141`. Лишний остаётся в караване, UnclaimBed вызывается после добавления. Физическая ветка снимает пешку с каравана до спавна и отмечает добавление без сверки результата; исключение обходит возврат. Перепроверить holder после RemovePawn, результат спавна/вставки и отката. Проверять наличие управляемого носильщика после перевода рабочих, особенно если последний способный идти остаётся, а остальные лежат. Инвентари уходящих рабочих, включая провиант, остаются у них: до перевода выделить подтверждённый запас возвращающимся.
- **P2 · Запас еды проверяется по полным стекам** — `Source/OutpostCaravanLogistics.cs:396–423,452–460,477–527`. Отбор режет количество в foodToTake, но затем оценщику передаются исходные Thing со всем stackCount. Например, часть отобранной еды несъедобна для группы, а большой остаток съедобного стека, оставленный дома, маскирует недостачу. Рассчитывать по TransferableOneWay с фактическим количеством. Оценивать оба направления с реальным грузом и составом на обратном пути: сейчас масса 0, один путь удваивается. Проверить запас после перевода работников.
- **P2 · Сканирование каждый тик и при каждом вызове GUI** — `Source/OutpostCaravanLogistics.cs:310–375,405–423`, `Source/Dialog_OutpostCaravan.cs:185–191`. Незакреплённая миссия сканирует караваны по каждой пешке каждый тик. Окно при каждом Layout/Repaint ищет WorldPath и собирает складскую еду. Путь в расчёте освобождается и при Found=false, и в finally. Привязку делать событием, резервную сверку и очистку — периодически, оценку в паузе кешировать до изменения состава/груза.

## Сохранение и доказательства
- `Source/OutpostCaravanLogistics.cs:152–162,180–195,380–387`. Цель и флаги сохраняются ссылками, запрошенная сумма — значением, фактический груз принадлежит ванильным инвентарям. Cecil подтверждает Deep-сохранение arrivalAction в Caravan_PathFollower и ссылку на mapParent в Enter. Поэтому штатное сохранение обратного маршрута поддерживает ваниль, но отдельного reload после сдачи и обработки исчезнувшего дома в пробе нет.
- `Tools/cecil_methods.ps1 -Type <тип> -Name <метод> -IL` подтвердил StopDead/PatherTickInterval/PatherArrived/StartPath/ExposeData, Enter.ExposeData, CaravanInventoryUtility.TakeThings, Caravan.RemovePawn и сигнатуру ExitMapAndCreateCaravan. Для оценки еды первое имя типа с namespace RimWorld дало exit 1, правильное RimWorld.Planet дало exit 0 и чтение stackCount. `git hash-object`, exit 0: Logistics `5553df06`, Dialog `913b6248`, Probe `2bd3dc61`.
- REPORT Antigravity заявляет fresh 38/38 и reload 21/21, runtimeErrors=0, reload unresolvedReferences=0, профиль `TestRuns/OutpostCaravan-FullSet-20261119-220419`. `Tests/InGameOutpostCaravanProbe.cs:374–512,519–606` реально ждёт выход/доставку/вход носильщика и проверяет сумму и кровать. Единственный сейв — по дороге туда. Кнопку проверяет вызовом postfix, действие не нажимает (`:439–454`). MaintainPawns наполняет потребности в пути (`:214–241`), а до сейва удаляются связи (`:155–197,460`), поэтому расход еды и чистое сохранение без этой подготовки не доказаны.
- До приёмки повторить на итоговой сборке отмену с грузом, отказ склада, переполнение рабочими, удаление аванпоста до/после выхода, гибель каравана, сохранение на обратном пути и путь с расходом еды. Ревью статическое, сборка и игра не запускались.

## Текущий код: Source/OutpostCaravanLogistics.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Arrival action for a walking caravan delivering workers and silver to a strategic outpost.
    /// Handled completely off-map upon arrival at the outpost's world tile.
    /// </summary>
    public class CaravanArrivalAction_OutpostDelivery : CaravanArrivalAction
    {
        private StrategicOutpost outpost;
        private List<Pawn> stayPawns = new List<Pawn>();
        private int silverAmount = 0;
        private Map originMap;

        public StrategicOutpost Outpost
        {
            get { return outpost; }
        }

        public Map OriginMap
        {
            get { return originMap; }
        }

        public List<Pawn> StayPawns
        {
            get { return stayPawns; }
        }

        public int SilverAmount
        {
            get { return silverAmount; }
        }

        public override string Label
        {
            get
            {
                return "TMM_OutpostCaravan_ReportDelivering".Translate(outpost != null ? outpost.Label : "");
            }
        }

        public override string ReportString
        {
            get
            {
                return "TMM_OutpostCaravan_ReportDelivering".Translate(outpost != null ? outpost.Label : "");
            }
        }

        public CaravanArrivalAction_OutpostDelivery()
        {
        }

        public CaravanArrivalAction_OutpostDelivery(StrategicOutpost outpost, List<Pawn> stayPawns, int silverAmount, Map originMap)
        {
            this.outpost = outpost;
            if (stayPawns != null)
            {
                this.stayPawns = new List<Pawn>(stayPawns);
            }
            this.silverAmount = silverAmount;
            this.originMap = originMap;
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport baseReport = base.StillValid(caravan, destinationTile);
            if (!baseReport) return baseReport;
            if (outpost == null || !outpost.Spawned)
            {
                return FloatMenuAcceptanceReport.WithFailReason("TMM_OutpostCaravan_OutpostGone".Translate());
            }
            return true;
        }

        public override void Arrived(Caravan caravan)
        {
            if (caravan == null) return;
            if (outpost == null || !outpost.Spawned)
            {
                Messages.Message("TMM_OutpostCaravan_OutpostGone".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // 1. Deliver silver cargo directly to outpost RawMaterials
            if (silverAmount > 0)
            {
                int remaining = silverAmount;
                List<Thing> taken = CaravanInventoryUtility.TakeThings(caravan, delegate(Thing t)
                {
                    if (remaining <= 0 || t == null || t.def != ThingDefOf.Silver) return 0;
                    int take = Mathf.Min(remaining, t.stackCount);
                    remaining -= take;
                    return take;
                });
                if (taken != null)
                {
                    for (int i = 0; i < taken.Count; i++)
                    {
                        outpost.RawMaterials.TryAddOrTransfer(taken[i], true);
                    }
                }
            }

            // 2. Add colonists marked to stay as workers
            if (stayPawns != null && stayPawns.Count > 0)
            {
                for (int i = stayPawns.Count - 1; i >= 0; i--)
                {
                    Pawn p = stayPawns[i];
                    if (p != null && caravan.ContainsPawn(p))
                    {
                        string failReason;
                        if (!outpost.AddPawnFromCaravan(caravan, p, out failReason, true))
                        {
                            Messages.Message("TMM_OutpostCaravan_WorkerRejected".Translate(p.LabelShort, failReason), outpost, MessageTypeDefOf.CautionInput, false);
                        }
                    }
                }
            }

            // 3. Return remaining carrier pawns home
            if (caravan.PawnsListForReading != null && caravan.PawnsListForReading.Count > 0)
            {
                Map home = originMap != null && Find.Maps.Contains(originMap) ? originMap : Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
                if (home != null)
                {
                    CaravanArrivalAction_Enter enter = new CaravanArrivalAction_Enter(home.Parent);
                    caravan.pather.StartPath(home.Tile, enter, true, true);
                    Messages.Message("TMM_OutpostCaravan_DeliveredReturning".Translate(outpost.Label, home.Parent.Label), caravan, MessageTypeDefOf.PositiveEvent, false);
                }
            }

            // 4. Notify manager
            OutpostCaravanManager mgr = OutpostCaravanManager.Instance;
            if (mgr != null)
            {
                mgr.NotifyDelivered(caravan);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref outpost, "outpost");
            Scribe_Collections.Look(ref stayPawns, "stayPawns", LookMode.Reference);
            Scribe_Values.Look(ref silverAmount, "silverAmount", 0);
            Scribe_References.Look(ref originMap, "originMap");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (stayPawns == null) stayPawns = new List<Pawn>();
            }
        }
    }

    /// <summary>
    /// Mission record for an active walking caravan mission towards an outpost.
    /// </summary>
    public class OutpostCaravanMission : IExposable
    {
        public StrategicOutpost outpost;
        public Map originMap;
        public Caravan caravan;
        public List<Pawn> pawns = new List<Pawn>();
        public List<Pawn> stayPawns = new List<Pawn>();
        public int silverAmount;
        public bool delivered;
        public bool cancelled;

        public void ExposeData()
        {
            Scribe_References.Look(ref outpost, "outpost");
            Scribe_References.Look(ref originMap, "originMap");
            Scribe_References.Look(ref caravan, "caravan");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Collections.Look(ref stayPawns, "stayPawns", LookMode.Reference);
            Scribe_Values.Look(ref silverAmount, "silverAmount", 0);
            Scribe_Values.Look(ref delivered, "delivered", false);
            Scribe_Values.Look(ref cancelled, "cancelled", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (pawns == null) pawns = new List<Pawn>();
                if (stayPawns == null) stayPawns = new List<Pawn>();
            }
        }
    }

    /// <summary>
    /// World component managing walking caravans dispatched to strategic outposts.
    /// Tracks active missions, connects formed caravans, and persists state across save/load.
    /// </summary>
    public class OutpostCaravanManager : WorldComponent
    {
        private List<OutpostCaravanMission> missions = new List<OutpostCaravanMission>();

        public List<OutpostCaravanMission> Missions
        {
            get { return missions; }
        }

        public static OutpostCaravanManager Instance
        {
            get
            {
                return Find.World == null ? null : Find.World.GetComponent<OutpostCaravanManager>();
            }
        }

        public OutpostCaravanManager(World world) : base(world)
        {
        }

        public void RegisterMission(StrategicOutpost outpost, Map originMap, List<Pawn> pawns, List<Pawn> stayPawns, int silverAmount)
        {
            if (outpost == null || pawns == null || pawns.Count == 0) return;
            OutpostCaravanMission mission = new OutpostCaravanMission();
            mission.outpost = outpost;
            mission.originMap = originMap;
            mission.pawns = new List<Pawn>(pawns);
            if (stayPawns != null) mission.stayPawns = new List<Pawn>(stayPawns);
            mission.silverAmount = silverAmount;
            mission.delivered = false;
            mission.cancelled = false;
            missions.Add(mission);
        }

        public void NotifyDelivered(Caravan c)
        {
            if (c == null) return;
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i].caravan == c)
                {
                    missions[i].delivered = true;
                    break;
                }
            }
        }

        public void CancelMission(Caravan c)
        {
            if (c == null) return;
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i].caravan == c)
                {
                    missions[i].cancelled = true;
                    break;
                }
            }
        }

        public OutpostCaravanMission GetMissionForCaravan(Caravan c)
        {
            if (c == null) return null;
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i].caravan == c && !missions[i].delivered && !missions[i].cancelled)
                {
                    return missions[i];
                }
            }
            return null;
        }

        public void TryAttachMissionToCaravan(Caravan car)
        {
            if (car == null || !car.Spawned || car.Faction != Faction.OfPlayer) return;

            for (int i = 0; i < missions.Count; i++)
            {
                OutpostCaravanMission mission = missions[i];
                if (mission.delivered || mission.cancelled || mission.caravan != null) continue;

                if (mission.pawns != null && mission.pawns.Count > 0)
                {
                    bool match = false;
                    for (int pIdx = 0; pIdx < mission.pawns.Count; pIdx++)
                    {
                        Pawn p = mission.pawns[pIdx];
                        if (p != null && car.ContainsPawn(p))
                        {
                            match = true;
                            break;
                        }
                    }

                    if (match)
                    {
                        mission.caravan = car;
                        var arrivalAction = new CaravanArrivalAction_OutpostDelivery(mission.outpost, mission.stayPawns, mission.silverAmount, mission.originMap);
                        car.pather.StartPath(mission.outpost.Tile, arrivalAction, true, true);
                        break;
                    }
                }
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            for (int i = missions.Count - 1; i >= 0; i--)
            {
                OutpostCaravanMission mission = missions[i];
                if (mission.delivered || mission.cancelled)
                {
                    missions.RemoveAt(i);
                    continue;
                }

                if (mission.outpost == null || !mission.outpost.Spawned)
                {
                    missions.RemoveAt(i);
                    continue;
                }

                // If caravan is not yet linked, check if the pawns have formed a world caravan
                if (mission.caravan == null)
                {
                    if (mission.pawns != null && mission.pawns.Count > 0)
                    {
                        for (int pIdx = 0; pIdx < mission.pawns.Count; pIdx++)
                        {
                            Pawn pLead = mission.pawns[pIdx];
                            if (pLead != null && !pLead.Spawned && Find.WorldObjects != null && Find.WorldObjects.Caravans != null)
                            {
                                List<Caravan> caravans = Find.WorldObjects.Caravans;
                                for (int c = 0; c < caravans.Count; c++)
                                {
                                    Caravan car = caravans[c];
                                    if (car.Faction == Faction.OfPlayer && car.ContainsPawn(pLead))
                                    {
                                        mission.caravan = car;
                                        var arrivalAction = new CaravanArrivalAction_OutpostDelivery(mission.outpost, mission.stayPawns, mission.silverAmount, mission.originMap);
                                        car.pather.StartPath(mission.outpost.Tile, arrivalAction, true, true);
                                        break;
                                    }
                                }
                                if (mission.caravan != null) break;
                            }
                        }
                    }

                    // If all pawns are dead or no longer forming, drop mission
                    if (mission.caravan == null)
                    {
                        bool anyForming = false;
                        for (int p = 0; p < mission.pawns.Count; p++)
                        {
                            Pawn pawn = mission.pawns[p];
                            if (pawn != null && !pawn.Dead && pawn.Spawned && CaravanFormingUtility.IsFormingCaravan(pawn))
                            {
                                anyForming = true;
                                break;
                            }
                        }
                        if (!anyForming && mission.pawns.All(p => p.Spawned))
                        {
                            // Formation was cancelled/aborted
                            missions.RemoveAt(i);
                            continue;
                        }
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref missions, "missions", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (missions == null) missions = new List<OutpostCaravanMission>();
            }
        }
    }

    /// <summary>
    /// Static logistics calculations and helpers for outpost walking caravans.
    /// </summary>
    public static class OutpostCaravanLogistics
    {
        public static bool CalculateTripDays(PlanetTile fromTile, PlanetTile toTile, List<Pawn> pawns, out float daysOneWay, out float daysRoundTrip)
        {
            daysOneWay = 0f;
            daysRoundTrip = 0f;
            if (!fromTile.Valid || !toTile.Valid || pawns == null || pawns.Count == 0 || fromTile.Layer == null || fromTile.Layer.Pather == null)
            {
                return false;
            }

            WorldPath path = fromTile.Layer.Pather.FindPath(fromTile, toTile, null, null);
            if (path == null || !path.Found)
            {
                if (path != null) path.ReleaseToPool();
                return false;
            }

            try
            {
                int ticksPerMove = CaravanTicksPerMoveUtility.GetTicksPerMove(pawns, 0f, 1000f, false, null);
                int ticksOneWay = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(fromTile, toTile, path, 0f, ticksPerMove, GenTicks.TicksAbs);
                daysOneWay = (float)ticksOneWay / 60000f;
                daysRoundTrip = daysOneWay * 2f;
                return true;
            }
            finally
            {
                path.ReleaseToPool();
            }
        }

        public static bool CheckCaravanFeasibility(StrategicOutpost outpost, Map map, List<Pawn> pawns, int silverAmount, out float daysOneWay, out float daysRoundTrip, out float foodDays, out bool canSend, out string cantSendReason)
        {
            daysOneWay = 0f;
            daysRoundTrip = 0f;
            foodDays = 0f;
            canSend = false;
            cantSendReason = null;

            if (outpost == null || map == null)
            {
                cantSendReason = "StrategicOutpost_InvalidOutpost".Translate();
                return false;
            }

            if (pawns == null || pawns.Count == 0)
            {
                cantSendReason = "TMM_OutpostCaravan_NoPawnsSelectedTip".Translate();
                return false;
            }

            if (!CalculateTripDays(map.Tile, outpost.Tile, pawns, out daysOneWay, out daysRoundTrip))
            {
                cantSendReason = "TMM_OutpostCaravan_PathUnreachableTip".Translate();
                return false;
            }

            // Check food availability on map using DaysWorthOfFoodCalculator
            List<Thing> colonyItems = CaravanFormingUtility.AllReachableColonyItems(map, false, false, false);
            List<Thing> availableFood = colonyItems != null
                ? colonyItems.Where(t => t != null && t.def.IsIngestible && t.def.IsNutritionGivingIngestible && !t.def.IsDrug && !t.def.IsCorpse).ToList()
                : new List<Thing>();

            foodDays = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(pawns, availableFood, map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, Faction.OfPlayer);

            if (foodDays < daysRoundTrip)
            {
                cantSendReason = "TMM_OutpostCaravan_NotEnoughFoodTip".Translate(daysRoundTrip.ToString("F1"));
                return false;
            }

            int availableSilver = OutpostPodLogistics.CountResource(map, ThingDefOf.Silver);
            if (silverAmount > availableSilver)
            {
                cantSendReason = "StrategicOutpost_NotEnoughSilverToSend".Translate(silverAmount);
                return false;
            }

            canSend = true;
            return true;
        }

        public static bool CollectFoodForTrip(Map map, List<Pawn> pawns, float daysRoundTrip, out Dictionary<Thing, int> foodToTake, out float gatheredDays)
        {
            foodToTake = new Dictionary<Thing, int>();
            gatheredDays = 0f;
            if (map == null || pawns == null || pawns.Count == 0) return false;

            List<Thing> colonyItems = CaravanFormingUtility.AllReachableColonyItems(map, false, false, false);
            if (colonyItems == null) return false;

            List<Thing> foodList = colonyItems
                .Where(t => t != null && t.def.IsIngestible && t.def.IsNutritionGivingIngestible && !t.def.IsDrug && !t.def.IsCorpse)
                .OrderByDescending(t => t.def.GetCompProperties<CompProperties_Rottable>() == null)
                .ThenByDescending(t => t.def.ingestible != null ? (int)t.def.ingestible.preferability : 0)
                .ToList();

            float dailyNutrition = 0f;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p.needs != null && p.needs.food != null && p.needs.food.FoodFallPerTick > 0f)
                {
                    dailyNutrition += p.needs.food.FoodFallPerTick * 60000f;
                }
                else
                {
                    dailyNutrition += 1.6f;
                }
            }

            float totalNutritionTarget = dailyNutrition * daysRoundTrip * 1.15f;
            float currentNutrition = 0f;

            List<Thing> gatheredThings = new List<Thing>();
            for (int i = 0; i < foodList.Count && currentNutrition < totalNutritionTarget; i++)
            {
                Thing food = foodList[i];
                float nutr = food.GetStatValue(StatDefOf.Nutrition);
                if (nutr <= 0.01f) continue;

                int needed = Mathf.CeilToInt((totalNutritionTarget - currentNutrition) / nutr);
                int take = Mathf.Min(needed, food.stackCount);
                if (take > 0)
                {
                    foodToTake[food] = take;
                    currentNutrition += (float)take * nutr;
                    gatheredThings.Add(food);
                }
            }

            gatheredDays = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(pawns, gatheredThings, map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, Faction.OfPlayer);
            return gatheredDays >= daysRoundTrip;
        }

        public static bool StartCaravan(StrategicOutpost outpost, Map map, List<Pawn> pawns, List<Pawn> stayPawns, int silverAmount, out string failReason)
        {
            failReason = null;
            float daysOneWay, daysRoundTrip, foodDays;
            bool canSend;
            string cantSendReason;

            if (!CheckCaravanFeasibility(outpost, map, pawns, silverAmount, out daysOneWay, out daysRoundTrip, out foodDays, out canSend, out cantSendReason))
            {
                failReason = cantSendReason;
                return false;
            }

            Dictionary<Thing, int> foodToTake;
            float gatheredFoodDays;
            if (!CollectFoodForTrip(map, pawns, daysRoundTrip, out foodToTake, out gatheredFoodDays))
            {
                failReason = "TMM_OutpostCaravan_NotEnoughFoodTip".Translate(daysRoundTrip.ToString("F1"));
                return false;
            }

            List<TransferableOneWay> transferables = new List<TransferableOneWay>();

            // 1. Add pawns
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                TransferableOneWay t = new TransferableOneWay();
                t.things.Add(p);
                t.ForceTo(1);
                transferables.Add(t);
            }

            // 2. Add silver
            if (silverAmount > 0)
            {
                List<Thing> colonyItems = CaravanFormingUtility.AllReachableColonyItems(map, false, false, false);
                List<Thing> silverItems = colonyItems != null ? colonyItems.Where(t => t != null && t.def == ThingDefOf.Silver).ToList() : new List<Thing>();
                int remainingSilver = silverAmount;
                for (int i = 0; i < silverItems.Count && remainingSilver > 0; i++)
                {
                    Thing s = silverItems[i];
                    int take = Mathf.Min(remainingSilver, s.stackCount);
                    if (take > 0)
                    {
                        TransferableOneWay t = new TransferableOneWay();
                        t.things.Add(s);
                        t.ForceTo(take);
                        transferables.Add(t);
                        remainingSilver -= take;
                    }
                }
            }

            // 3. Add food
            foreach (KeyValuePair<Thing, int> kvp in foodToTake)
            {
                TransferableOneWay t = new TransferableOneWay();
                t.things.Add(kvp.Key);
                t.ForceTo(kvp.Value);
                transferables.Add(t);
            }

            // 4. Find exit spot and meeting point
            IntVec3 center = IntVec3.Zero;
            for (int i = 0; i < pawns.Count; i++) center += pawns[i].Position;
            center = new IntVec3(center.x / pawns.Count, 0, center.z / pawns.Count);

            IntVec3 exitSpot;
            if (!RCellFinder.TryFindClosestEdgeCellTo(center, map, out exitSpot))
            {
                if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Always, out exitSpot))
                {
                    exitSpot = CellFinder.RandomEdgeCell(map);
                }
            }

            IntVec3 meetingPoint;
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(exitSpot, map, out meetingPoint))
            {
                meetingPoint = center;
            }

            PlanetTile startTile = CaravanExitMapUtility.BestExitTileToGoTo(outpost.Tile, map);
            if (!startTile.Valid) startTile = map.Tile;

            // 5. Start vanilla caravan forming process
            CaravanFormingUtility.StartFormingCaravan(pawns, new List<Pawn>(), Faction.OfPlayer, transferables, meetingPoint, exitSpot, startTile, outpost.Tile);

            // 6. Register mission in manager
            if (OutpostCaravanManager.Instance != null)
            {
                OutpostCaravanManager.Instance.RegisterMission(outpost, map, pawns, stayPawns, silverAmount);
            }

            Messages.Message("CaravanFormationProcessStarted".Translate(), pawns[0], MessageTypeDefOf.PositiveEvent, false);
            return true;
        }
    }

    /// <summary>
    /// Harmony patch adding "Return home" Gizmo to player caravans currently on an outpost delivery mission.
    /// </summary>
    [HarmonyPatch(typeof(Caravan), "GetGizmos")]
    public static class Patch_Caravan_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Caravan __instance)
        {
            if (values != null)
            {
                foreach (Gizmo g in values)
                {
                    yield return g;
                }
            }

            if (__instance == null || __instance.Faction != Faction.OfPlayer) yield break;

            CaravanArrivalAction_OutpostDelivery delivery = __instance.pather != null ? __instance.pather.ArrivalAction as CaravanArrivalAction_OutpostDelivery : null;
            if (delivery != null && delivery.Outpost != null)
            {
                Command_Action returnHome = new Command_Action();
                returnHome.defaultLabel = "TMM_OutpostCaravan_ReturnHome".Translate();
                returnHome.defaultDesc = "TMM_OutpostCaravan_ReturnHomeDesc".Translate();
                returnHome.icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", false) ?? TexCommand.ClearPrioritizedWork;
                returnHome.action = delegate
                {
                    Map home = delivery.OriginMap != null && Find.Maps.Contains(delivery.OriginMap)
                        ? delivery.OriginMap
                        : Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);

                    if (home != null)
                    {
                        __instance.pather.StartPath(home.Tile, new CaravanArrivalAction_Enter(home.Parent), true, true);
                        if (OutpostCaravanManager.Instance != null)
                        {
                            OutpostCaravanManager.Instance.CancelMission(__instance);
                        }
                        Messages.Message("TMM_OutpostCaravan_CancelledReturning".Translate(__instance.Name), __instance, MessageTypeDefOf.NeutralEvent, false);
                    }
                };
                yield return returnHome;
            }
        }
    }

    /// <summary>
    /// Harmony patch attaching outpost delivery arrival action to newly formed caravans immediately upon map exit.
    /// </summary>
    [HarmonyPatch(typeof(CaravanExitMapUtility), "ExitMapAndCreateCaravan", new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(PlanetTile), typeof(PlanetTile), typeof(PlanetTile), typeof(bool) })]
    public static class Patch_CaravanExitMapUtility_ExitMapAndCreateCaravan
    {
        public static void Postfix(Caravan __result)
        {
            if (__result != null && OutpostCaravanManager.Instance != null)
            {
                OutpostCaravanManager.Instance.TryAttachMissionToCaravan(__result);
            }
        }
    }
}
```

## Текущий код: Source/Dialog_OutpostCaravan.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class Dialog_OutpostCaravan : Window
    {
        private StrategicOutpost outpost;
        private Map homeMap;
        private List<Pawn> availableColonists = new List<Pawn>();
        private HashSet<Pawn> selectedPawns = new HashSet<Pawn>();
        private HashSet<Pawn> stayPawns = new HashSet<Pawn>();
        private int silverAmount = 0;
        private int maxSilver = 0;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize
        {
            get { return new Vector2(620f, 620f); }
        }

        public Dialog_OutpostCaravan(StrategicOutpost outpost, Map homeMap, bool focusOnSilver = false)
        {
            this.outpost = outpost;
            this.homeMap = homeMap;
            this.doCloseButton = false;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;

            if (homeMap != null && homeMap.mapPawns != null)
            {
                availableColonists = homeMap.mapPawns.FreeColonistsSpawned
                    .Where(p => StrategicOutpostCaravanUtility.IsSuitableColonist(p) && !p.Drafted && !p.InMentalState)
                    .OrderBy(p => p.LabelShort)
                    .ToList();
            }

            maxSilver = homeMap != null ? OutpostPodLogistics.CountResource(homeMap, ThingDefOf.Silver) : 0;

            if (focusOnSilver)
            {
                silverAmount = Mathf.Min(200, maxSilver);
                if (availableColonists.Count > 0)
                {
                    selectedPawns.Add(availableColonists[0]);
                }
            }
            else
            {
                if (availableColonists.Count > 0)
                {
                    selectedPawns.Add(availableColonists[0]);
                    stayPawns.Add(availableColonists[0]);
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont origFont = Text.Font;
            TextAnchor origAnchor = Text.Anchor;
            Color origColor = GUI.color;
            bool origWrap = Text.WordWrap;

            try
            {
                if (outpost == null || homeMap == null)
                {
                    Close();
                    return;
                }

                float curY = 0f;

                // 1. Header Title
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, curY, inRect.width - 30f, 32f), "TMM_OutpostCaravan_Title".Translate(outpost.LabelCap));
                Text.Font = GameFont.Small;
                curY += 38f;

                // 2. Colonists Section
                Widgets.Label(new Rect(0f, curY, inRect.width, 22f), "TMM_OutpostCaravan_SelectColonists".Translate());
                curY += 24f;

                float colonistListHeight = 220f;
                Rect outRect = new Rect(0f, curY, inRect.width, colonistListHeight);
                float viewHeight = Mathf.Max(colonistListHeight, (float)availableColonists.Count * 34f + 10f);
                Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, viewHeight);

                Widgets.DrawMenuSection(outRect);
                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
                float itemY = 4f;

                for (int i = 0; i < availableColonists.Count; i++)
                {
                    Pawn p = availableColonists[i];
                    Rect rowRect = new Rect(4f, itemY, viewRect.width - 8f, 30f);

                    if (i % 2 == 1)
                    {
                        Widgets.DrawAltRect(rowRect);
                    }

                    // Checkbox: in caravan
                    bool inCaravan = selectedPawns.Contains(p);
                    Rect chkRect = new Rect(rowRect.x + 4f, rowRect.y + 3f, 24f, 24f);
                    bool newInCaravan = inCaravan;
                    Widgets.Checkbox(chkRect.position, ref newInCaravan);
                    if (newInCaravan != inCaravan)
                    {
                        if (newInCaravan)
                        {
                            selectedPawns.Add(p);
                        }
                        else
                        {
                            selectedPawns.Remove(p);
                            stayPawns.Remove(p);
                        }
                    }

                    // Colonist label
                    Rect nameRect = new Rect(chkRect.xMax + 8f, rowRect.y + 4f, 250f, 22f);
                    string colonistTitle = p.story != null && !p.story.TitleShort.NullOrEmpty() ? (" (" + p.story.TitleShort + ")") : "";
                    Widgets.Label(nameRect, p.LabelShort + colonistTitle);

                    // Checkbox: stay as worker
                    Rect stayRect = new Rect(rowRect.xMax - 180f, rowRect.y + 4f, 175f, 24f);
                    bool stay = stayPawns.Contains(p);
                    bool newStay = stay;
                    if (newInCaravan)
                    {
                        Widgets.CheckboxLabeled(stayRect, "TMM_OutpostCaravan_StayAsWorker".Translate(), ref newStay);
                        if (newStay != stay)
                        {
                            if (newStay) stayPawns.Add(p);
                            else stayPawns.Remove(p);
                        }
                    }
                    else
                    {
                        GUI.color = Color.gray;
                        bool dummy = false;
                        Widgets.CheckboxLabeled(stayRect, "TMM_OutpostCaravan_StayAsWorker".Translate(), ref dummy, true);
                        GUI.color = Color.white;
                    }

                    itemY += 34f;
                }

                if (availableColonists.Count == 0)
                {
                    Widgets.Label(new Rect(10f, 10f, 300f, 25f), "StrategicOutpost_NoEligibleColonists".Translate());
                }

                Widgets.EndScrollView();
                curY += colonistListHeight + 12f;

                // 3. Silver Slider Section
                Widgets.Label(new Rect(0f, curY, inRect.width, 22f), "TMM_OutpostCaravan_SilverToSend".Translate(silverAmount, maxSilver));
                curY += 24f;

                Rect sliderRect = new Rect(0f, curY + 2f, inRect.width - 240f, 26f);
                silverAmount = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, silverAmount, 0, maxSilver, false, null, null, null, 10));

                float qBtnX = sliderRect.xMax + 8f;
                float qBtnW = 50f;
                if (Widgets.ButtonText(new Rect(qBtnX, curY, qBtnW, 26f), "0")) silverAmount = 0;
                qBtnX += qBtnW + 4f;
                if (Widgets.ButtonText(new Rect(qBtnX, curY, qBtnW, 26f), "+100")) silverAmount = Mathf.Min(maxSilver, silverAmount + 100);
                qBtnX += qBtnW + 4f;
                if (Widgets.ButtonText(new Rect(qBtnX, curY, qBtnW, 26f), "+500")) silverAmount = Mathf.Min(maxSilver, silverAmount + 500);
                qBtnX += qBtnW + 4f;
                if (Widgets.ButtonText(new Rect(qBtnX, curY, qBtnW, 26f), "Max")) silverAmount = maxSilver;

                curY += 34f;

                // 4. Logistics and Food Assessment
                float daysOneWay, daysRoundTrip, foodDays;
                bool canSend;
                string cantSendReason;
                List<Pawn> selectedList = selectedPawns.ToList();

                OutpostCaravanLogistics.CheckCaravanFeasibility(outpost, homeMap, selectedList, silverAmount,
                    out daysOneWay, out daysRoundTrip, out foodDays, out canSend, out cantSendReason);

                Rect infoRect = new Rect(0f, curY, inRect.width, 74f);
                Widgets.DrawMenuSection(infoRect);
                Rect innerInfo = infoRect.ContractedBy(6f);

                string tripStr = "TMM_OutpostCaravan_TripEstimate".Translate(daysRoundTrip.ToString("F1"), daysOneWay.ToString("F1"));
                Widgets.Label(new Rect(innerInfo.x, innerInfo.y + 4f, innerInfo.width, 22f), tripStr);

                string foodStr = "TMM_OutpostCaravan_FoodEstimate".Translate(foodDays.ToString("F1"), daysRoundTrip.ToString("F1"));
                if (foodDays < daysRoundTrip && selectedList.Count > 0)
                {
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                }
                else
                {
                    GUI.color = Color.green;
                }
                Widgets.Label(new Rect(innerInfo.x, innerInfo.y + 30f, innerInfo.width, 22f), foodStr);
                GUI.color = Color.white;

                curY += 86f;

                // 5. Bottom Buttons
                float btnHeight = 36f;
                Rect btnCancel = new Rect(0f, inRect.height - btnHeight, 130f, btnHeight);
                if (Widgets.ButtonText(btnCancel, "Cancel".Translate()))
                {
                    Close();
                }

                Rect btnSend = new Rect(inRect.width - 180f, inRect.height - btnHeight, 180f, btnHeight);
                if (Widgets.ButtonText(btnSend, "TMM_OutpostCaravan_SendBtn".Translate(), true, true, canSend))
                {
                    string failReason;
                    if (OutpostCaravanLogistics.StartCaravan(outpost, homeMap, selectedList, stayPawns.ToList(), silverAmount, out failReason))
                    {
                        Close();
                    }
                    else if (!failReason.NullOrEmpty())
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }
                }

                if (!canSend && !cantSendReason.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(btnSend, cantSendReason);
                }
            }
            finally
            {
                Text.Font = origFont;
                Text.Anchor = origAnchor;
                GUI.color = origColor;
                Text.WordWrap = origWrap;
            }
        }
    }
}
```

## Текущий код: Source/StrategicOutpost.cs, строки 950–1117 (перенос рабочих)
```csharp
                    }
                    remaining -= take;
                    if (remaining <= 0) break;
                }
            }
            return count - remaining;
        }

        public static void SetStoredPawnProtection(Pawn pawn, bool stored)
        {
            if (pawn == null || pawn.Spawned || pawn.Destroyed) return;
            // Caravan membership is a vanilla GC root; our independent animal holder is not.
            // Re-register through public APIs to apply/release KeepForever only while stationed.
            if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
            Find.WorldPawns.PassToWorld(pawn, stored ? PawnDiscardDecideMode.KeepForever : PawnDiscardDecideMode.Decide);
        }

        public bool AddPawnFromCaravan(Caravan caravan, Pawn pawn, out string failReason)
        {
            return AddPawnFromCaravan(caravan, pawn, out failReason, true);
        }

        public bool AddPawnFromCaravan(Caravan caravan, Pawn pawn, out string failReason, bool unclaimBed)
        {
            if (!CanInteract(caravan) || pawn == null)
            {
                failReason = "StrategicOutpost_InvalidCaravanOrPawn".Translate();
                return false;
            }

            if (!caravan.ContainsPawn(pawn) || pawn.Dead)
            {
                failReason = "StrategicOutpost_PawnNotAvailable".Translate();
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                failReason = "StrategicOutpost_NotPlayerFaction".Translate();
                return false;
            }

            if (!pawn.RaceProps.Animal && !StrategicOutpostCaravanUtility.IsSuitableColonist(pawn))
            {
                failReason = "StrategicOutpost_PawnNotSuitable".Translate();
                return false;
            }

            int currentStaff = Workers.Count + (deployedStaffPawnIDs != null ? deployedStaffPawnIDs.Count : 0);
            if (!pawn.RaceProps.Animal && currentStaff >= MaxCapacity)
            {
                failReason = "StrategicOutpost_CapacityFull".Translate(MaxCapacity);
                return false;
            }

            caravan.RemovePawn(pawn);

            bool added = false;
            if (pawn.RaceProps.Animal)
            {
                if (operationalMode == OutpostOperationalMode.Physical && HasMap && Map != null)
                {
                    StrategicOutpostMapComponent comp = Map.GetComponent<StrategicOutpostMapComponent>();
                    IntVec3 center = comp != null && comp.baseCenter.IsValid ? comp.baseCenter : Map.Center;
                    IntVec3 c;
                    if (CellFinder.TryFindRandomCellNear(center, Map, 8, cell => cell.Standable(Map) && !cell.Fogged(Map), out c))
                    {
                        GenSpawn.Spawn(pawn, c, Map, Rot4.South, WipeMode.Vanish, false, false);
                        if (deployedAnimalPawnIDs == null) deployedAnimalPawnIDs = new List<int>();
                        deployedAnimalPawnIDs.Add(pawn.thingIDNumber);
                        added = true;
                    }
                    else
                    {
                        added = Animals.TryAdd(pawn, false);
                    }
                }
                else
                {
                    added = Animals.TryAdd(pawn, false);
                }
            }
            else
            {
                if (operationalMode == OutpostOperationalMode.Physical && HasMap && Map != null)
                {
                    StrategicOutpostMapComponent comp = Map.GetComponent<StrategicOutpostMapComponent>();
                    IntVec3 center = comp != null && comp.baseCenter.IsValid ? comp.baseCenter : Map.Center;
                    IntVec3 c;
                    if (CellFinder.TryFindRandomCellNear(center, Map, 8, cell => cell.Standable(Map) && !cell.Fogged(Map), out c))
                    {
                        GenSpawn.Spawn(pawn, c, Map, Rot4.South, WipeMode.Vanish, false, false);
                        if (deployedStaffPawnIDs == null) deployedStaffPawnIDs = new List<int>();
                        deployedStaffPawnIDs.Add(pawn.thingIDNumber);
                        added = true;
                    }
                    else
                    {
                        added = Workers.TryAdd(pawn, false);
                    }
                }
                else
                {
                    added = Workers.TryAdd(pawn, false);
                }
            }

            if (!added)
            {
                caravan.AddPawn(pawn, false);
                failReason = "StrategicOutpost_FailedToAddPawn".Translate();
                return false;
            }

            if (unclaimBed && pawn.ownership != null)
            {
                pawn.ownership.UnclaimBed();
            }

            SetStoredPawnProtection(pawn, true);
            failReason = null;
            return true;
        }

        public bool RemovePawnToCaravan(Caravan caravan, Pawn pawn, out string failReason)
        {
            if (!CanInteract(caravan) || pawn == null)
            {
                failReason = "StrategicOutpost_InvalidCaravanOrPawn".Translate();
                return false;
            }

            if (operationalMode == OutpostOperationalMode.Physical || pawn.Spawned)
            {
                failReason = "StrategicOutpost_MustReturnToAutonomousFirst".Translate();
                return false;
            }

            bool inWorkers = Workers.Contains(pawn);
            bool inAnimals = Animals.Contains(pawn);

            if (!inWorkers && !inAnimals)
            {
                failReason = "StrategicOutpost_PawnNotFound".Translate();
                return false;
            }

            if (inWorkers && Workers.Count <= 1)
            {
                failReason = "StrategicOutpost_CannotRemoveLastWorker".Translate();
                return false;
            }

            ThingOwner<Pawn> owner = inWorkers ? Workers : Animals;
            owner.Remove(pawn);

            caravan.AddPawn(pawn, false);
            if (!caravan.ContainsPawn(pawn))
            {
                owner.TryAdd(pawn, false);
                failReason = "StrategicOutpost_FailedToAddPawnToCaravan".Translate();
                return false;
            }

            SetStoredPawnProtection(pawn, false);
            failReason = null;
            return true;
        }
```

## Сдать
Файлы с путём над каждым блоком, каждый целиком:
- `Source/OutpostCaravanLogistics.cs`
- `Source/Dialog_OutpostCaravan.cs`
- `Source/StrategicOutpost.Caravan.txt` — только исправленные методы `AddPawnFromCaravan` (обе перегрузки) и `RemovePawnToCaravan` целиком. Агент вставит их сам, остальной файл не присылай.
- `Tests/InGameOutpostCaravanProbe.cs` — новая проба. Сценарии реальные, кнопки нажимай через их действие, а не через вызов postfix:
  - отмена с грузом;
  - отказ склада на аванпосте → груз вернулся;
  - переполнение рабочими;
  - аванпост удалён до выхода и после выхода;
  - формирование сорвалось → позже обычный караван с тем же колонистом не перехвачен;
  - гибель каравана;
  - сохранение на обратном пути → носильщики дошли домой;
  - путь с реальным расходом еды, без `MaintainPawns` и без удаления связей перед сейвом.

В конце — что изменено по каждой находке P1/P2.
