**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

Твой код этапов E3 (`StoryStage_HotelVisit`, `StoryStage_Deliver`) встроен в мод, и Codex провёл приёмку: **«НЕ ПРИНИМАТЬ»**. Ниже его отчёт целиком, текущий код в моде (агент поправил сигнатуры 1.7), общее списание из `OutpostPodLogistics`, твой `StoryRelationRestorer` из исправления E2 и образец безопасного переноса героя. **Исправь все находки P1 и P2.** Правила прежние: C# 5, RimWorld 1.7, `// CHECK API` там, где не уверен.

Ядро (`SparkStory`, `StoryLetter`) уже исправлено по E2 и к этому моменту будет встроено. `forceOutcome` читает только ядро, и только с `-tmm-story-test`. Ядро не переписывай.

## Принципы исправления
- **Герой в делегации — только безопасный перенос**, по образцу `TryJoin` ниже. Берётся только свободная живая мировая пешка нужной фракции. Если пешка — колонист, пленник, в караване, в капсуле, в контейнере или на карте, герой в визите не участвует: визит идёт без него или этап получает исход `unavailable`, на твой выбор, но явно. Перенос проверяется, при отказе — откат. В `vars` сохрани ThingID героя и флаг «заспавнен». После reload героя повторно не спавнить.
- **Перемирие:**
  - до спавна проверь, что нейтралитет вообще возможен: у фракций с постоянной враждой `SetRelationDirect` пишет ошибку и ничего не меняет;
  - после вызова сверь результат; если перемирия нет — исход `failed` без спавна гостей;
  - исходные goodwill и тип отношения запиши в `StoryRelationRestorer` **до** изменения. Расширь его на временный нейтралитет: сейчас он умеет только откат враждебности после рейда;
  - откат выполняется при любом выходе — успех, провал, исключение, отключение квеста, удалённый этап или Def. При двух визитах одной фракции откат делает только последний.
- **Host — реальный исход каждого гостя.** Сохрани ID карты, ID lord и полный список ID гостей. На каждом опросе гость относится к одному из исходов: на карте, ушёл мирно (lord завершился выходом с карты), пропал, взят в плен, убит, изгнан, ушёл враждебным. Успех — только когда lord штатно завершил визит и нужная часть гостей ушла мирно. Пустой список, пропавшая карта или lord — это `failed`, а не `success`. Если lord не уходит, через N дней после срока этап завершается `failed`.
- **Investigate** — фиксируй конкретную беседу колониста с гостем **этого** визита: `PlayLogEntry_Interaction`, где initiator — колонист, recipient — гость из списка, и время после старта визита. Комнатный fallback убери.
- **Hide** — привяжи облаву к конкретному инциденту: сохрани ID её lord или ID пешек. Успех только если облава была, завершилась, и гости ушли мирно. Любой посторонний враг облавой не считается. Если `hunterFaction` не разрешился — явный исход. Если облава так и не пришла — дедлайн.
- **Deliver — проверка перед каждым списанием.** И в письме, и в караване, и после загрузки до первого Reconcile сверяй:
  - этап активен и это нужный этап (`stageKey`);
  - совпадает поколение письма;
  - карта — та, что сохранена в `vars`, а не `ctx.Home` из замыкания;
  - письмо ещё в стеке;
  - дедлайн не прошёл.
  Караван тоже сначала проверяет срок. Оплату, принятую вовремя, после reload завершай через `delivered` без повторного списания.
- **Атомарное списание** — общий помощник для Deliver и `OutpostPodLogistics`:
  1. собрать список разрешённых стеков: только склады/зоны хранения игрока на сохранённой карте, или прямые инвентари пешек конкретного каравана. Вложенные вещи — явная политика, опиши её;
  2. проверить полную сумму;
  3. отделить нужное в один временный holder, проверяя результат каждого `SplitOff`. Если хоть один шаг не удался — вернуть всё прежним владельцам;
  4. только потом уничтожить содержимое holder и **один раз** записать `delivered`.
  Счётчик учитывает только фактически отделённое.
- **giverSettlement** — только поселение нужной фракции. Если его нет при старте или оно исчезло — явный исход, без подмены на домашнюю оплату. Сдача засчитывается, когда караван игрока **прибыл** в это поселение (arrival action) или игрок явно нажал «Сдать». Проходящий мимо караван не списывается.
- **Совместимость с Hospitality:**
  - до создания группы проверь нужные сигнатуры (через `AccessTools`, кешируй результат);
  - различай настоящий визит Hospitality и ванильный fallback, запиши режим в `vars`;
  - `noHotel` выдавай сразу на старте, а не на следующем Tick.
- **Убери** собственное чтение `forceOutcome` из HotelVisit. Каждый исход идёт через один `Finish`, который вызывает общую очистку: Restorer, lord, vars.

## Отчёт Codex
### StoryStageHotelReview — НЕ ПРИНИМАТЬ

## Куда смотреть Claude
- **P1 · Перенос занятого героя** — `Source/StoryStage_HotelVisit.cs:147–159,188–204`. GetOrCreate обычно возвращает единственную пешку реестра, поэтому второй экземпляр напрямую не создаётся. Эта же пешка может быть колонистом, пленником, членом каравана или находиться в капсуле. Код снимает её с карты или передаёт в GenSpawn без проверки владельца, задач и фракции делегации. Как в E2fix, допускать только свободную пешку нужной фракции в подходящем состоянии, подтвердить перенос и иметь откат при отказе. Повторный визит после reload не должен повторно спавнить героя.
- **P1 · Перемирие и откат** — `Source/StoryStage_HotelVisit.cs:125–136,361–366,632–658`. Для двух goodwill-фракций ванильный SetRelationDirect пишет ошибку и возвращается без изменения. Враждебная делегация всё равно спавнится и падает в failed на первом Tick. Успевшее измениться отношение может остаться таким при исключении, удалении этапа или конфликте двух визитов. Проверять возможность и результат перемирия до спавна. Расширить сохраняемый StoryRelationRestorer из E2fix на нейтралитет и завершать его запись при каждом выходе, в том числе при миграции и сбое.
- **P1 · Host даёт успех при исчезновении гостей** — `Source/StoryStage_HotelVisit.cs:201–204,229–325,478–503`. `lordId` записан, но не прочитан. После удаления первой домашней карты с доступной второй, перемещения гостей в holder или исчезновения ID пустой список spawnedGuests означает успех даже до конца срока. Проверка sentAway применяется только к ещё заспавненным гостям. Уход во вражде отловится, если фракция/пешка остаётся враждебна на опросе, но фактический исход ухода не фиксируется. Сохранять ID карты, состав lord и отдельный результат ухода каждого гостя, различать мирный уход, пропажу, плен и изгнание. Ввести предельный срок, если lord не уходит.
- **P1 · Investigate и Hide дают ложный успех** — `Source/StoryStage_HotelVisit.cs:285–325,528–606`. Investigate принимает любую PlayLogEntry_Interaction или просто присутствие в комнате колониста с навыком ≥8. Убрать комнатный fallback и фиксировать конкретную беседу с гостем текущего визита. Hide выставляет hunterRaidSeen, но нигде не требует его. Успех возможен без облавы, а при неразрешённом hunterFaction её заменяет любой враг. Привязать облаву к конкретному инциденту/lord и проверять её завершение плюс безопасный уход гостей. Добавить конечный срок при отсутствии нужной облавы.
- **P1 · Deliver списывает при просрочке или со старой карты** — `Source/StoryStage_Deliver.cs:45–80,176–219`. Клик сверяет поколение и остаток, но не срок, статус, текущий этап и наличие письма в стеке. Замыкание держит ранее выбранную домашнюю карту. После дедлайна до следующего опроса или до сверки после загрузки можно принять платёж по устаревшему письму. Караван тоже списывает до проверки срока. Закрепить карту в vars и перед списанием сверять активный этап, поколение, карту, письмо и дедлайн. Уже принятое вовремя списание после reload завершать через delivered без повторной оплаты.
- **P1 · Списание не атомарно** — `Source/StoryStage_Deliver.cs:73–80,388–418`, `Source/OutpostPodLogistics.cs:27–63`. Складский счётчик включает всё не запрещённое на карте, включая вещи вне склада, а вещи внутри держателей не видны. В караване ванильный API перечисляет прямые инвентари всех пешек. В обеих ветках стеки уничтожаются по одному и счётчик учитывает даже неудачный SplitOff. Если поздний стек не удалось отделить или мод бросил исключение, уже уничтоженное не вернуть, delivered ещё не записан. Собрать и перепроверить разрешённые стеки и владельцев, зарезервировать полную сумму во временном holder с откатом к исходным владельцам, затем подтвердить платёж и исход один раз. Явно определить политику вложенных вещей.
- **P2 · giverSettlement может сменить получателя и снять груз у проходящего каравана** — `Source/StoryStage_Deliver.cs:152–166,313–348,377–388`. После старта ID поселения и точный PlanetTile проверяются, чужие караваны отсеяны. Но старт выбирает любое дружественное поселение, если нужной фракции нет, и потом первый подходящий караван на клетке, даже если он просто проходит. При отсутствии поселений происходит замена на оплату дома. Использовать только поселение нужной фракции, при его исчезновении вернуть явный исход, сдачу привязать к прибытию нужного каравана или явному действию сдачи.

## Сохранения и совместимость
- `Source/StoryStage_HotelVisit.cs:72–111,403–451`. На старте отсутствие Hospitality/компонента, закрытый приём и отсутствие гостевой кровати приводят к noHotel на следующем Tick. Несовпадение API lord подменяется ванильным визитом, optional Arrive может быть пропущен. Проверять необходимые сигнатуры до создания группы и различать реальный визит Hospitality и ванильный fallback.
- `Source/SparkStory.cs:50–54,188–205`, `Source/StoryStage_Deliver.cs:91–98,287–310`. vars и поля письма сохраняются. После обычной подтверждённой оплаты delivered и переход ядра защищают от повторной оплаты/награды. Возврат группы гостиницы после reload опирается на ID и поиск на текущей домашней карте, а не на сохранённую карту и lord. Платёж по письму перед первым Reconcile требует той же проверки, что обычный клик.
- `Source/StoryStage_HotelVisit.cs:217–222`, `Source/SparkStory.cs:195,286–292`. В проверенном E3 нет вызова TestForceOutcome. Интеграция добавила в HotelVisit отдельное чтение сохраняемого forceOutcome без проверки тестового флага. Сейчас ядро перехватывает его первым и обходит Finish, а с E2fix отдельную ветку следует удалить. Оставить временный тестовый ввод только в ядре и вызвать общую очистку при любом исходе.

## Доказательства и границы
- `git diff --no-index` Neon → корень, exit 1 (различия): Hotel +15/−10, Deliver +8/−0, Probe +82/−49. У Deliver добавлен конструктор ID письма. У Hotel добавлен forceOutcome и изменены совместимость API и поиск кроватей. Риски владельца, целей и оплаты унаследованы. Git blob Hotel/Deliver/Probe: `933c2a73`/`9fef76bd`/`59eecf18`.
- `Tools/cecil_methods.ps1 -Type <тип> -Name <метод> -IL`, exit 0. Проверены Faction.SetRelationDirect, CaravanInventoryUtility.AllInventoryItems, Game.get_AnyPlayerHomeMap, Thing.SplitOff и Pawn.SpawnSetup. Vanilla SpawnSetup удаляет пешку из WorldPawns, это само по себе не проверка исходного holder и прав на перенос.
- REPORT Antigravity заявляет fresh 17/17 и reload 4/4, runtimeErrors=0, для `TestRuns/StoryHotel-FullSet-20261120-003345`. Проба `Tests/InGameStoryHotelProbe.cs:209–250,254–306,378–402` сдвигает часы и принудительно выводит гостей. Reload проверяет историю гостиницы, не фактический состав lord, мирный выход и оплату. Строки 596–599 всегда записывают UI passed=true. Нужны реальные прогоны беседы и облавы, визит враждебной фракции, reload с активной группой и оплатой, просроченный клик и отказ на втором стеке, сдача подходящим караваном. Ревью статическое, игра и сборка не запускались.

## Текущий код: Source/StoryStage_HotelVisit.cs
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public enum StoryHotelObjective
    {
        Host,
        Investigate,
        Hide
    }

    /// <summary>
    /// Quest stage that brings a visiting delegation (optionally including a registered HeroDef)
    /// to the colony's Hospitality hotel, tracking Host, Investigate, or Hide objectives across
    /// save/reload strictly via ctx.run.vars. All Hospitality access goes through reflection.
    /// </summary>
    public sealed class StoryStage_HotelVisit : StoryStage
    {
        // Hospitality reflection handles (safe if Hospitality is absent or renamed)
        private static readonly Type HospMapCompType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
        private static readonly FieldInfo HospGuestsWelcomeField = HospMapCompType == null ? null : AccessTools.Field(HospMapCompType, "guestsAreWelcome");
        private static readonly Type HospGuestBedType = AccessTools.TypeByName("Hospitality.Building_GuestBed"); // CHECK API
        private static readonly Type HospLordJobType = AccessTools.TypeByName("Hospitality.LordJob_VisitColony"); // CHECK API
        private static readonly Type HospCompGuestType = AccessTools.TypeByName("Hospitality.CompGuest"); // CHECK API
        private static readonly MethodInfo HospCompArriveMethod = HospCompGuestType == null ? null : AccessTools.Method(HospCompGuestType, "Arrive"); // CHECK API
        private static readonly FieldInfo HospCompSentAwayField = HospCompGuestType == null ? null : AccessTools.Field(HospCompGuestType, "sentAway"); // CHECK API
        private static readonly PropertyInfo HospCompSentAwayProp = HospCompGuestType == null ? null : AccessTools.Property(HospCompGuestType, "sentAway"); // CHECK API

        public string faction;
        public HeroDef hero;
        public int groupSize = 2;
        public float stayDays = 2f;
        public StoryHotelObjective objective = StoryHotelObjective.Host;
        public string hunterFaction;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "hotel"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        /// <summary>
        /// Checks via reflection whether Hospitality is available, guests are welcomed,
        /// and at least one guest bed exists on the given map. Never throws if Hospitality is missing.
        /// </summary>
        public static bool HasOpenHotel(Map map)
        {
            if (map == null || HospMapCompType == null || HospGuestsWelcomeField == null) return false;

            MapComponent comp = HotelVisitGuarantee.HospitalityOf(map);
            if (comp == null) return false;

            try
            {
                object welcomeObj = HospGuestsWelcomeField.GetValue(comp);
                if (!(welcomeObj is bool) || !(bool)welcomeObj) return false;
            }
            catch
            {
                return false;
            }

            return FindGuestBedCell(map) != IntVec3.Invalid;
        }

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            Map home = ctx.Home;
            int now = Find.TickManager.TicksGame;
            int stayTicks = Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(stayDays * GenDate.TicksPerDay));

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["leaveTick"] = (now + stayTicks).ToString();
            ctx.run.vars["investigated"] = "false";
            ctx.run.vars["hunterRaidSeen"] = "false";
            ctx.run.vars["departureTriggered"] = "false";
            ctx.run.vars["arrived"] = "false";

            if (!HasOpenHotel(home))
            {
                ctx.run.vars["noHotel"] = "true";
                SendNoHotelLetter(ctx, home);
                return;
            }

            Faction visitFac = ResolveVisitFaction(ctx);
            if (visitFac == null)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: no valid faction found for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                ctx.run.vars["noHotel"] = "true";
                SendNoHotelLetter(ctx, home);
                return;
            }

            ctx.run.vars["visitFactionId"] = visitFac.loadID.ToString();

            // Undercover spies / defectors from hostile factions temporarily visit under truce.
            if (visitFac.HostileTo(Faction.OfPlayer))
            {
                ctx.run.vars["wasHostile"] = "true";
                ctx.run.vars["origGoodwill"] = visitFac.PlayerGoodwill.ToString();
                ctx.run.vars["origKind"] = ((int)visitFac.RelationKindWith(Faction.OfPlayer)).ToString();
                ForceNeutralForVisit(visitFac);
            }
            else
            {
                ctx.run.vars["wasHostile"] = "false";
            }

            IntVec3 entryCell;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out entryCell, home, CellFinder.EdgeRoadChance_Friendly)) // CHECK API
            {
                entryCell = CellFinder.RandomEdgeCell(home);
            }

            List<Pawn> guests = new List<Pawn>();
            Pawn heroPawn = null;

            if (hero != null && HeroRegistry.Instance != null)
            {
                heroPawn = HeroRegistry.Instance.GetOrCreate(hero);
                if (heroPawn != null && !heroPawn.Dead && !heroPawn.Destroyed)
                {
                    if (heroPawn.Spawned) heroPawn.DeSpawn(DestroyMode.Vanish);
                    if (heroPawn.Faction == null)
                    {
                        heroPawn.SetFaction(visitFac);
                    }
                    guests.Add(heroPawn);
                    ctx.run.vars["heroThingId"] = heroPawn.ThingID;
                    ctx.run.vars["heroIdNum"] = heroPawn.thingIDNumber.ToString();
                }
            }

            int targetCount = Mathf.Clamp(groupSize, 1, 8);
            while (guests.Count < targetCount)
            {
                PawnKindDef kind = visitFac.RandomPawnKind();
                if (kind == null) kind = PawnKindDefOf.SpaceRefugee;
                PawnGenerationRequest req = new PawnGenerationRequest(kind, visitFac, PawnGenerationContext.NonPlayer, home.Tile, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false); // CHECK API
                Pawn guest = PawnGenerator.GeneratePawn(req);
                if (guest != null)
                {
                    guests.Add(guest);
                }
                else
                {
                    break;
                }
            }

            if (guests.Count == 0)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: failed to create guest pawns.");
                ctx.run.vars["noHotel"] = "true";
                RestoreFactionIfNeeded(ctx);
                return;
            }

            List<int> guestIdNums = new List<int>();
            for (int i = 0; i < guests.Count; i++)
            {
                Pawn p = guests[i];
                IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(entryCell, home, 6, null);
                GenSpawn.Spawn(p, spawnCell, home, WipeMode.Vanish);
                TryNotifyHospitalityCompArrive(p);
                guestIdNums.Add(p.thingIDNumber);
            }

            IntVec3 chillSpot = FindGuestBedCell(home);
            if (chillSpot == IntVec3.Invalid) chillSpot = home.Center;

            Lord visitLord = AttachVisitLord(home, visitFac, chillSpot, stayTicks, guests);
            ctx.run.vars["lordId"] = visitLord != null ? visitLord.loadID.ToString() : "-1";
            ctx.run.vars["guestIds"] = JoinInts(guestIdNums);
            ctx.run.vars["arrived"] = "true";

            string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Hotel Guests Arrived"), ctx);
            string body = StoryUtil.Format(!text.NullOrEmpty() ? text : Objective(ctx), ctx);
            LookTargets target = heroPawn != null ? new LookTargets(heroPawn) : new LookTargets(guests[0]);
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, target); // CHECK API
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            string forced = null;
            if (ctx.run.vars.TryGetValue("forceOutcome", out forced) && !forced.NullOrEmpty())
            {
                ctx.run.vars.Remove("forceOutcome");
                return Finish(ctx, forced);
            }

            if (GetBool(ctx, "noHotel"))
            {
                return Finish(ctx, "noHotel");
            }

            Map home = ctx.Home;
            if (home == null)
            {
                return Finish(ctx, "failed");
            }

            int now = Find.TickManager.TicksGame;
            int startTick = GetInt(ctx, "startTick", now);
            int leaveTick = GetInt(ctx, "leaveTick", now + GenDate.TicksPerDay);
            int visitFacId = GetInt(ctx, "visitFactionId", -1);
            Faction visitFac = FindFactionById(visitFacId);

            if (visitFac != null && visitFac.HostileTo(Faction.OfPlayer))
            {
                return Finish(ctx, "failed");
            }

            List<int> guestIds = ParseInts(GetStr(ctx, "guestIds"));
            if (guestIds.Count == 0)
            {
                return Finish(ctx, "failed");
            }

            List<Pawn> spawnedGuests = new List<Pawn>();
            for (int i = 0; i < guestIds.Count; i++)
            {
                int idNum = guestIds[i];
                if (IsPawnDeadByIdNum(home, idNum))
                {
                    return Finish(ctx, "failed");
                }

                Pawn p = FindPawnOnMapByIdNum(home, idNum);
                if (p != null)
                {
                    if (p.Dead || p.IsPrisonerOfColony || p.IsSlaveOfColony || p.HostileTo(Faction.OfPlayer))
                    {
                        return Finish(ctx, "failed");
                    }
                    if (IsSentAwayByHospitality(p))
                    {
                        return Finish(ctx, "failed");
                    }
                    spawnedGuests.Add(p);
                }
                else
                {
                    Pawn worldPawn = FindWorldPawnByIdNum(idNum);
                    if (worldPawn != null && (worldPawn.Dead || worldPawn.HostileTo(Faction.OfPlayer)))
                    {
                        return Finish(ctx, "failed");
                    }
                }
            }

            // Objective tracking while guests are present on the home map
            if (objective == StoryHotelObjective.Investigate && !GetBool(ctx, "investigated") && spawnedGuests.Count > 0)
            {
                if (CheckInvestigationMet(home, spawnedGuests, startTick))
                {
                    ctx.run.vars["investigated"] = "true";
                }
            }

            bool huntersPresentNow = false;
            if (objective == StoryHotelObjective.Hide)
            {
                huntersPresentNow = HasActiveHunterRaiders(home);
                if (huntersPresentNow)
                {
                    ctx.run.vars["hunterRaidSeen"] = "true";
                }
            }

            // Trigger departure once stayDays have elapsed (important when time advances or Hospitality stay differs)
            if (now >= leaveTick && spawnedGuests.Count > 0 && !GetBool(ctx, "departureTriggered"))
            {
                if (objective != StoryHotelObjective.Hide || !huntersPresentNow)
                {
                    TriggerGracefulDeparture(home, visitFac, spawnedGuests);
                    ctx.run.vars["departureTriggered"] = "true";
                }
            }

            // Guests have all left the home map peacefully
            if (spawnedGuests.Count == 0)
            {
                if (objective == StoryHotelObjective.Investigate)
                {
                    return Finish(ctx, GetBool(ctx, "investigated") ? "success" : "failed");
                }
                if (objective == StoryHotelObjective.Hide)
                {
                    if (huntersPresentNow) return null;
                    return Finish(ctx, "success");
                }
                return Finish(ctx, "success");
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            if (ctx != null && ctx.run != null && GetBool(ctx, "noHotel"))
            {
                return "Open a hotel (build guest beds and enable welcoming guests)";
            }

            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : text, ctx);
            if (baseText.NullOrEmpty())
            {
                if (objective == StoryHotelObjective.Investigate) baseText = "Investigate visiting hotel guests";
                else if (objective == StoryHotelObjective.Hide) baseText = "Shelter hidden guests in the hotel";
                else baseText = "Host visiting delegation in the hotel";
            }

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int leaveTick = GetInt(ctx, "leaveTick", now);
            float daysLeft = Mathf.Max(0f, (leaveTick - now) / (float)GenDate.TicksPerDay);

            if (objective == StoryHotelObjective.Investigate)
            {
                string status = GetBool(ctx, "investigated") ? "clue found" : "talk to guest";
                return string.Format("{0} ({1}, {2:0.0}d left)", baseText, status, daysLeft);
            }

            return string.Format("{0} ({1:0.0}d left)", baseText, daysLeft);
        }

        private string Finish(StoryContext ctx, string outcome)
        {
            RestoreFactionIfNeeded(ctx);
            ctx.run.vars["finished"] = outcome;
            return outcome;
        }

        private void SendNoHotelLetter(StoryContext ctx, Map home)
        {
            string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "No Hotel Available"), ctx);
            string body = "A story delegation tried to visit {COLONY}, but your settlement does not have an open hotel.\n\nTo receive hotel visitors in future quests, build at least one Guest Bed (and a reception desk if desired) and enable 'Guests are welcome' in the Guests tab.";
            body = StoryUtil.Format(body, ctx);
            LookTargets target = home != null ? new LookTargets(home.Center, home) : LookTargets.Invalid;
            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, target); // CHECK API
        }

        private static IntVec3 FindGuestBedCell(Map map)
        {
            if (map == null || map.listerBuildings == null) return IntVec3.Invalid;
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || b.Destroyed) continue;
                if (HospGuestBedType != null && HospGuestBedType.IsInstanceOfType(b))
                {
                    return b.Position;
                }
                if (b is Building_Bed)
                {
                    string typeName = b.GetType().Name;
                    string defName = b.def != null ? b.def.defName : string.Empty;
                    if (typeName.IndexOf("Guest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("Guest", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return b.Position;
                    }
                }
            }
            return IntVec3.Invalid;
        }

        private static Lord AttachVisitLord(Map home, Faction fac, IntVec3 chillSpot, int stayTicks, List<Pawn> guests)
        {
            LordJob hospJob = TryCreateHospitalityLordJob(fac, chillSpot, stayTicks);
            if (hospJob != null)
            {
                return LordMaker.MakeNewLord(fac, hospJob, home, guests);
            }

            // Fallback to vanilla LordJob_VisitColony if Hospitality constructor signature differs
            LordJob vanillaJob = new RimWorld.LordJob_VisitColony(fac, chillSpot, stayTicks); // CHECK API
            return LordMaker.MakeNewLord(fac, vanillaJob, home, guests);
        }

        private static LordJob TryCreateHospitalityLordJob(Faction fac, IntVec3 chillSpot, int stayTicks)
        {
            if (HospLordJobType == null) return null;
            try
            {
                ConstructorInfo[] ctors = HospLordJobType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < ctors.Length; i++)
                {
                    ParameterInfo[] p = ctors[i].GetParameters();
                    if (p.Length == 4 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3) && p[2].ParameterType == typeof(int) && p[3].ParameterType == typeof(bool))
                    {
                        return ctors[i].Invoke(new object[] { fac, chillSpot, stayTicks, false }) as LordJob;
                    }
                }
                for (int i = 0; i < ctors.Length; i++)
                {
                    ParameterInfo[] p = ctors[i].GetParameters();
                    if (p.Length == 3 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3) && p[2].ParameterType == typeof(int))
                    {
                        return ctors[i].Invoke(new object[] { fac, chillSpot, stayTicks }) as LordJob;
                    }
                }
                for (int i = 0; i < ctors.Length; i++)
                {
                    ParameterInfo[] p = ctors[i].GetParameters();
                    if (p.Length == 2 && p[0].ParameterType == typeof(Faction) && p[1].ParameterType == typeof(IntVec3))
                    {
                        return ctors[i].Invoke(new object[] { fac, chillSpot }) as LordJob;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryStage_HotelVisit: failed to instantiate Hospitality.LordJob_VisitColony: " + ex.Message);
            }
            return null;
        }

        private static void TryNotifyHospitalityCompArrive(Pawn p)
        {
            if (p == null || HospCompGuestType == null || HospCompArriveMethod == null) return;
            try
            {
                List<ThingComp> comps = p.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (HospCompGuestType.IsInstanceOfType(comps[i]))
                    {
                        if (HospCompArriveMethod.GetParameters().Length == 0)
                        {
                            HospCompArriveMethod.Invoke(comps[i], null);
                        }
                        break;
                    }
                }
            }
            catch
            {
                // Ignore Hospitality version mismatch on optional CompGuest.Arrive
            }
        }

        private static bool IsSentAwayByHospitality(Pawn p)
        {
            if (p == null || HospCompGuestType == null) return false;
            try
            {
                List<ThingComp> comps = p.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (!HospCompGuestType.IsInstanceOfType(comps[i])) continue;
                    if (HospCompSentAwayField != null)
                    {
                        object v = HospCompSentAwayField.GetValue(comps[i]);
                        if (v is bool && (bool)v) return true;
                    }
                    else if (HospCompSentAwayProp != null)
                    {
                        object v = HospCompSentAwayProp.GetValue(comps[i], null);
                        if (v is bool && (bool)v) return true;
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            return false;
        }

        private static void TriggerGracefulDeparture(Map home, Faction fac, List<Pawn> guests)
        {
            if (home == null || guests == null || guests.Count == 0) return;

            for (int i = 0; i < guests.Count; i++)
            {
                Pawn p = guests[i];
                if (p == null) continue;
                Lord oldLord = p.GetLord();
                if (oldLord != null)
                {
                    oldLord.RemovePawn(p);
                }
            }

            Faction exitFac = fac ?? guests[0].Faction;
            if (exitFac != null)
            {
                LordMaker.MakeNewLord(exitFac, new LordJob_ExitMapBest(LocomotionUrgency.Walk, false, false), home, guests); // CHECK API
            }
        }

        private static bool CheckInvestigationMet(Map home, List<Pawn> guests, int startTick)
        {
            List<Pawn> colonists = home.mapPawns.FreeColonistsSpawned;

            // 1. Check vanilla PlayLog for any social interaction between a colonist and a guest since startTick
            if (Find.PlayLog != null && Find.PlayLog.AllEntries != null)
            {
                List<LogEntry> entries = Find.PlayLog.AllEntries;
                for (int i = 0; i < entries.Count; i++)
                {
                    LogEntry entry = entries[i];
                    if (entry == null) continue;
                    if (entry.Tick < startTick) break;
                    if (!(entry is PlayLogEntry_Interaction)) continue; // CHECK API

                    bool hitsGuest = false;
                    for (int g = 0; g < guests.Count; g++)
                    {
                        if (entry.Concerns(guests[g])) // CHECK API
                        {
                            hitsGuest = true;
                            break;
                        }
                    }
                    if (!hitsGuest) continue;

                    for (int c = 0; c < colonists.Count; c++)
                    {
                        if (entry.Concerns(colonists[c])) return true; // CHECK API
                    }
                }
            }

            // 2. Check if a guest is sharing an indoor room with a humanities/social colonist (Social >= 8 or Intellectual >= 8)
            for (int g = 0; g < guests.Count; g++)
            {
                Pawn guest = guests[g];
                if (guest == null || !guest.Spawned) continue;
                Room room = guest.GetRoom();
                if (room == null || room.PsychologicallyOutdoors) continue;

                for (int c = 0; c < colonists.Count; c++)
                {
                    Pawn col = colonists[c];
                    if (col == null || col.Dead || col.Downed || !col.Spawned) continue;
                    if (col.GetRoom() != room) continue;
                    if (col.skills != null)
                    {
                        SkillRecord soc = col.skills.GetSkill(SkillDefOf.Social);
                        SkillRecord intel = col.skills.GetSkill(SkillDefOf.Intellectual);
                        if ((soc != null && !soc.TotallyDisabled && soc.Level >= 8) ||
                            (intel != null && !intel.TotallyDisabled && intel.Level >= 8))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HasActiveHunterRaiders(Map home)
        {
            if (home == null || home.mapPawns == null) return false;
            Faction hunterFac = !hunterFaction.NullOrEmpty() ? StoryUtil.FactionOf(hunterFaction) : null;

            IReadOnlyList<Pawn> pawns = home.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.Downed || p.IsPrisonerOfColony) continue;
                if (!p.HostileTo(Faction.OfPlayer)) continue;
                if (hunterFac == null || p.Faction == hunterFac)
                {
                    return true;
                }
            }
            return false;
        }

        private Faction ResolveVisitFaction(StoryContext ctx)
        {
            if (!faction.NullOrEmpty())
            {
                Faction f = StoryUtil.FactionOf(faction);
                if (f != null && !f.defeated && !f.IsPlayer) return f;
            }
            if (ctx.Faction != null && !ctx.Faction.defeated && !ctx.Faction.IsPlayer)
            {
                return ctx.Faction;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.def.humanlikeFaction && !f.HostileTo(Faction.OfPlayer))
                {
                    return f;
                }
            }
            return null;
        }

        private static void ForceNeutralForVisit(Faction fac)
        {
            if (fac == null || !fac.HostileTo(Faction.OfPlayer)) return;
            fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false, null, GlobalTargetInfo.Invalid); // CHECK API
        }

        private void RestoreFactionIfNeeded(StoryContext ctx)
        {
            if (!GetBool(ctx, "wasHostile")) return;
            int facId = GetInt(ctx, "visitFactionId", -1);
            Faction fac = FindFactionById(facId);
            if (fac == null) return;

            int origGoodwill = GetInt(ctx, "origGoodwill", -80);
            int origKindInt = GetInt(ctx, "origKind", (int)FactionRelationKind.Hostile);
            FactionRelationKind origKind = (FactionRelationKind)origKindInt;

            int delta = origGoodwill - fac.PlayerGoodwill;
            if (delta != 0)
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null); // CHECK API
            }
            if (fac.RelationKindWith(Faction.OfPlayer) != origKind)
            {
                fac.SetRelationDirect(Faction.OfPlayer, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }
            ctx.run.vars["wasHostile"] = "false";
        }

        private static Faction FindFactionById(int loadId)
        {
            if (loadId < 0) return null;
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId) return all[i];
            }
            return null;
        }

        private static Pawn FindPawnOnMapByIdNum(Map map, int idNum)
        {
            if (map == null || map.mapPawns == null) return null;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].thingIDNumber == idNum) return pawns[i];
            }
            return null;
        }

        private static Pawn FindWorldPawnByIdNum(int idNum)
        {
            if (Find.WorldPawns == null) return null;
            List<Pawn> alive = Find.WorldPawns.AllPawnsAlive;
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i] != null && alive[i].thingIDNumber == idNum) return alive[i];
            }
            return null;
        }

        private static bool IsPawnDeadByIdNum(Map map, int idNum)
        {
            if (map != null)
            {
                List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                for (int i = 0; i < corpses.Count; i++)
                {
                    Corpse c = corpses[i] as Corpse;
                    if (c != null && c.InnerPawn != null && c.InnerPawn.thingIDNumber == idNum) return true;
                }
            }
            if (Find.WorldPawns != null)
            {
                foreach (Pawn p in Find.WorldPawns.AllPawnsDead)
                {
                    if (p != null && p.thingIDNumber == idNum) return true;
                }
            }
            return false;
        }

        private static string JoinInts(List<int> list)
        {
            if (list == null || list.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(list[i]);
            }
            return sb.ToString();
        }

        private static List<int> ParseInts(string raw)
        {
            List<int> result = new List<int>();
            if (raw.NullOrEmpty()) return result;
            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) result.Add(val);
            }
            return result;
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            string raw;
            int val;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
        }

        private static bool GetBool(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }
    }
}
```

## Текущий код: Source/StoryStage_Deliver.cs
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public enum StoryDeliverTarget
    {
        colony,
        giverSettlement
    }

    /// <summary>
    /// Reminder/hand-in ChoiceLetter for StoryStage_Deliver when delivering directly
    /// from the colony's stockpiles (to = colony). Persists cleanly across save/reload.
    /// </summary>
    public sealed class ChoiceLetter_StoryDeliver : ChoiceLetter
    {
        public string questDefName;
        public int runGen;
        public string thingDefName;
        public int requiredCount;

        public ChoiceLetter_StoryDeliver()
        {
            if (Find.UniqueIDsManager != null)
            {
                ID = Find.UniqueIDsManager.GetNextLetterID();
            }
        }

        public override bool CanDismissWithRightClick
        {
            get { return false; }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                Map home = Find.AnyPlayerHomeMap;
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                int have = (home != null && def != null) ? OutpostPodLogistics.CountResource(home, def) : 0; // CHECK API

                string itemLabel = def != null ? def.label : thingDefName;
                string deliverLabel = string.Format("Deliver {0} x{1} ({2}/{3})", itemLabel, requiredCount, have, requiredCount);
                DiaOption deliver = new DiaOption(deliverLabel);

                if (home == null || def == null || have < requiredCount)
                {
                    deliver.Disable(string.Format("Need {0} {1} in colony stockpiles ({2}/{0})", requiredCount, itemLabel, have));
                }
                else
                {
                    deliver.action = delegate
                    {
                        StoryQuestRun run = StoryStage_Deliver.FindQuestRun(questDefName);
                        if (run == null || run.gen != runGen)
                        {
                            Find.LetterStack.RemoveLetter(this);
                            return;
                        }
                        if (run.vars.ContainsKey("delivered") && run.vars["delivered"] == "true")
                        {
                            Find.LetterStack.RemoveLetter(this);
                            return;
                        }

                        int currentHave = OutpostPodLogistics.CountResource(home, def); // CHECK API
                        if (currentHave < requiredCount) return;

                        int taken = OutpostPodLogistics.DeductResource(home, def, requiredCount); // CHECK API
                        if (taken >= requiredCount)
                        {
                            run.vars["delivered"] = "true";
                            Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    deliver.resolveTree = true;
                }

                yield return deliver;
                yield return Option_Postpone; // CHECK API
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref questDefName, "questDefName", null);
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref thingDefName, "thingDefName", null);
            Scribe_Values.Look(ref requiredCount, "requiredCount", 1);
        }
    }

    /// <summary>
    /// Quest stage requiring delivery of a vanilla ThingDef either from home-map stockpiles
    /// (via a ChoiceLetter hand-in button) or by bringing a player caravan to the nearest
    /// settlement of the quest faction before timeoutDays.
    /// </summary>
    public sealed class StoryStage_Deliver : StoryStage
    {
        public ThingDef thing;
        public int count = 1;
        public StoryDeliverTarget to = StoryDeliverTarget.colony;
        public float timeoutDays = 10f;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "deliver"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            int now = Find.TickManager.TicksGame;
            int deadline = now + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["deadlineTick"] = deadline.ToString();
            ctx.run.vars["delivered"] = "false";

            if (to == StoryDeliverTarget.colony)
            {
                SendColonyDeliverLetter(ctx);
            }
            else
            {
                Settlement target = FindNearestQuestSettlement(ctx);
                if (target != null)
                {
                    ctx.run.vars["settlementId"] = target.ID.ToString();
                    ctx.run.vars["settlementName"] = target.LabelCap.ToString();
                    string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Caravan Delivery"), ctx);
                    string body = BuildSettlementLetterText(ctx, target);
                    Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.NeutralEvent, new LookTargets(target)); // CHECK API
                }
                else
                {
                    // Fallback to colony hand-in if no settlement exists for the quest faction
                    Log.Warning("[TMM] StoryStage_Deliver: no settlement found for giverSettlement delivery; falling back to colony delivery.");
                    ctx.run.vars["fallbackColony"] = "true";
                    SendColonyDeliverLetter(ctx);
                }
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now + GenDate.TicksPerDay);

            if (GetBool(ctx, "delivered"))
            {
                RemoveDeliverLetters(ctx);
                ctx.run.vars["finished"] = "success";
                return "success";
            }

            bool colonyMode = (to == StoryDeliverTarget.colony) || GetBool(ctx, "fallbackColony");
            if (colonyMode)
            {
                if (now >= deadline)
                {
                    RemoveDeliverLetters(ctx);
                    ctx.run.vars["finished"] = "timeout";
                    return "timeout";
                }

                // Re-send reminder letter if it was somehow lost
                if (!HasActiveDeliverLetter(ctx))
                {
                    SendColonyDeliverLetter(ctx);
                }
                return null;
            }

            // Caravan delivery to giverSettlement
            int settlementId = GetInt(ctx, "settlementId", -1);
            Settlement target = FindSettlementById(settlementId);
            if (target != null && thing != null && TryCompleteCaravanDelivery(target, thing, count))
            {
                ctx.run.vars["delivered"] = "true";
                ctx.run.vars["finished"] = "success";
                string doneTitle = StoryUtil.Format(!label.NullOrEmpty() ? label : "Delivery Complete", ctx);
                string doneBody = string.Format("Your caravan delivered {0} x{1} to {2}.", thing.label, count, target.LabelCap);
                Find.LetterStack.ReceiveLetter(doneTitle, doneBody, LetterDefOf.PositiveEvent, new LookTargets(target)); // CHECK API
                return "success";
            }

            if (now >= deadline)
            {
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string itemLabel = thing != null ? thing.label : "resources";
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", itemLabel, count), ctx);

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            bool colonyMode = (to == StoryDeliverTarget.colony) || GetBool(ctx, "fallbackColony");
            if (colonyMode)
            {
                int have = (ctx.Home != null && thing != null) ? OutpostPodLogistics.CountResource(ctx.Home, thing) : 0; // CHECK API
                return string.Format("{0} ({1}/{2} {3}, {4:0.0}d left)", baseText, have, count, itemLabel, daysLeft);
            }

            string targetName = GetStr(ctx, "settlementName");
            if (targetName.NullOrEmpty()) targetName = "faction settlement";
            return string.Format("{0} to {1} ({2:0.0}d left)", baseText, targetName, daysLeft);
        }

        private void SendColonyDeliverLetter(StoryContext ctx)
        {
            if (Find.LetterStack == null || thing == null) return;

            ChoiceLetter_StoryDeliver letter = new ChoiceLetter_StoryDeliver();
            letter.def = LetterDefOf.NeutralEvent;
            letter.Label = StoryUtil.Format(!label.NullOrEmpty() ? label : string.Format("Deliver {0} x{1}", thing.label, count), ctx);
            letter.Text = BuildColonyLetterText(ctx);
            letter.questDefName = ctx.def != null ? ctx.def.defName : string.Empty;
            letter.runGen = ctx.run.gen;
            letter.thingDefName = thing.defName;
            letter.requiredCount = Mathf.Max(1, count);
            letter.lookTargets = ctx.Home != null ? new LookTargets(ctx.Home.Center, ctx.Home) : LookTargets.Invalid;

            Find.LetterStack.ReceiveLetter(letter); // CHECK API
        }

        private string BuildColonyLetterText(StoryContext ctx)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Gather {0} x{1} in {2}'s stockpiles within {3:0.0} days, then open this letter and click Deliver.",
                itemLabel, count, "{COLONY}", timeoutDays);
            instruction = StoryUtil.Format(instruction, ctx);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private string BuildSettlementLetterText(StoryContext ctx, Settlement target)
        {
            string custom = StoryUtil.Format(text, ctx);
            string itemLabel = thing != null ? thing.label : "items";
            string instruction = string.Format("Send a caravan carrying {0} x{1} to {2} within {3:0.0} days.",
                itemLabel, count, target.LabelCap, timeoutDays);
            if (custom.NullOrEmpty()) return instruction;
            return custom + "\n\n" + instruction;
        }

        private static bool HasActiveDeliverLetter(StoryContext ctx)
        {
            if (Find.LetterStack == null || ctx == null || ctx.def == null || ctx.run == null) return false;
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = 0; i < letters.Count; i++)
            {
                ChoiceLetter_StoryDeliver dl = letters[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == ctx.def.defName && dl.runGen == ctx.run.gen) return true;
            }
            return false;
        }

        private static void RemoveDeliverLetters(StoryContext ctx)
        {
            if (Find.LetterStack == null || ctx == null || ctx.def == null || ctx.run == null) return;
            List<Letter> snapshot = new List<Letter>(Find.LetterStack.LettersListForReading);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ChoiceLetter_StoryDeliver dl = snapshot[i] as ChoiceLetter_StoryDeliver;
                if (dl != null && dl.questDefName == ctx.def.defName && dl.runGen == ctx.run.gen)
                {
                    Find.LetterStack.RemoveLetter(dl);
                }
            }
        }

        private static Settlement FindNearestQuestSettlement(StoryContext ctx)
        {
            Faction targetFac = ctx.Faction;
            PlanetTile homeTile = ResolveHomeTile(ctx); // CHECK API

            Settlement bestSameFaction = null;
            float bestSameDist = float.MaxValue;
            Settlement bestFriendly = null;
            float bestFriendlyDist = float.MaxValue;

            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s == null || s.Destroyed || s.Faction == null || s.Faction.IsPlayer) continue;

                float dist = homeTile.Valid ? Find.WorldGrid.TraversalDistanceBetween(homeTile, s.Tile) : 0f; // CHECK API
                if (targetFac != null && s.Faction == targetFac)
                {
                    if (dist < bestSameDist)
                    {
                        bestSameDist = dist;
                        bestSameFaction = s;
                    }
                }
                else if (!s.Faction.HostileTo(Faction.OfPlayer))
                {
                    if (dist < bestFriendlyDist)
                    {
                        bestFriendlyDist = dist;
                        bestFriendly = s;
                    }
                }
            }

            return bestSameFaction ?? bestFriendly;
        }

        private static Settlement FindSettlementById(int id)
        {
            if (id < 0) return null;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s != null && !s.Destroyed && s.ID == id) return s;
            }
            return null;
        }

        private static PlanetTile ResolveHomeTile(StoryContext ctx) // CHECK API
        {
            if (ctx.Home != null) return ctx.Home.Tile; // CHECK API
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (settlements[i] != null && settlements[i].Faction == Faction.OfPlayer)
                {
                    return settlements[i].Tile; // CHECK API
                }
            }
            return PlanetTile.Invalid; // CHECK API
        }

        private static bool TryCompleteCaravanDelivery(Settlement target, ThingDef targetDef, int requiredCount)
        {
            if (target == null || targetDef == null || requiredCount <= 0) return false;

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Destroyed || c.Faction != Faction.OfPlayer) continue;
                if (c.Tile != target.Tile) continue;

                List<Thing> items = CaravanInventoryUtility.AllInventoryItems(c); // CHECK API
                if (items == null) continue;

                int total = 0;
                for (int j = 0; j < items.Count; j++)
                {
                    Thing t = items[j];
                    if (t != null && !t.Destroyed && t.def == targetDef)
                    {
                        total += t.stackCount;
                    }
                }

                if (total < requiredCount) continue;

                int remaining = requiredCount;
                List<Thing> copy = new List<Thing>(items);
                for (int j = 0; j < copy.Count; j++)
                {
                    Thing t = copy[j];
                    if (t == null || t.Destroyed || t.def != targetDef) continue;
                    int take = Mathf.Min(remaining, t.stackCount);
                    Thing split = t.SplitOff(take);
                    if (split != null && !split.Destroyed)
                    {
                        split.Destroy(DestroyMode.Vanish);
                    }
                    remaining -= take;
                    if (remaining <= 0) break;
                }
                return remaining <= 0;
            }

            return false;
        }

        internal static StoryQuestRun FindQuestRun(string questDefName)
        {
            SparkStory story = SparkStory.Instance;
            if (story == null || questDefName.NullOrEmpty()) return null;

            // CHECK API: verify exact SparkStory run lookup method (e.g., story.GetRun or story.runs)
            Type t = story.GetType();
            MethodInfo getRun = t.GetMethod("GetRun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getRun != null)
            {
                return getRun.Invoke(story, new object[] { questDefName }) as StoryQuestRun;
            }

            FieldInfo runsField = t.GetField("runs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (runsField != null)
            {
                object val = runsField.GetValue(story);
                Dictionary<string, StoryQuestRun> dict = val as Dictionary<string, StoryQuestRun>;
                if (dict != null)
                {
                    StoryQuestRun r;
                    if (dict.TryGetValue(questDefName, out r)) return r;
                }
                List<StoryQuestRun> list = val as List<StoryQuestRun>;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && list[i].quest == questDefName) return list[i];
                    }
                }
            }
            return null;
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            string raw;
            int val;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
        }

        private static bool GetBool(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }
    }
}
```

## Текущий код: Source/OutpostPodLogistics.cs (общее списание)
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
    public static class OutpostPodLogistics
    {
        public const float PodMassCapacity = 150f;
        public const float FuelPerTilePerPod = 2.25f;

        public static Map GetHomeMap()
        {
            if (Find.Maps == null) return null;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m != null && m.IsPlayerHome) return m;
            }
            return Find.AnyPlayerHomeMap;
        }

        public static int CountResource(Map map, ThingDef def)
        {
            if (map == null || def == null || map.listerThings == null) return 0;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t != null && !t.Destroyed && !t.IsForbidden(Faction.OfPlayer))
                {
                    count += t.stackCount;
                }
            }
            return count;
        }

        public static int DeductResource(Map map, ThingDef def, int count)
        {
            if (map == null || def == null || count <= 0 || map.listerThings == null) return 0;
            int remaining = count;
            List<Thing> list = map.listerThings.ThingsOfDef(def).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t != null && !t.Destroyed && !t.IsForbidden(Faction.OfPlayer))
                {
                    int take = Mathf.Min(remaining, t.stackCount);
                    Thing split = t.SplitOff(take);
                    if (split != null && !split.Destroyed)
                    {
                        split.Destroy(DestroyMode.Vanish);
                    }
                    remaining -= take;
                    if (remaining <= 0) break;
                }
            }
            return count - remaining;
        }

        public static int CalculatePodCount(float mass)
        {
            if (mass <= 0f) return 1;
            return Mathf.Max(1, Mathf.CeilToInt(mass / PodMassCapacity));
        }

        public static int CalculateTotalFuelNeeded(int originTile, int destTile, int podCount)
        {
            float dist = 1f;
            if (Find.WorldGrid != null && originTile >= 0 && destTile >= 0)
            {
                dist = Find.WorldGrid.ApproxDistanceInTiles(originTile, destTile);
            }
            dist = Mathf.Max(1f, dist);
            int fuelPerPod = Mathf.CeilToInt(dist * FuelPerTilePerPod);
            return fuelPerPod * Mathf.Max(1, podCount);
        }

        public static void CalculateCost(int originTile, int destTile, float mass, Map homeMap,
            out int totalFuelNeeded, out int chemfuelToDeduct, out int silverToDeduct, out bool canAfford, out string costSummary)
        {
            int pods = CalculatePodCount(mass);
            totalFuelNeeded = CalculateTotalFuelNeeded(originTile, destTile, pods);

            int availFuel = CountResource(homeMap, ThingDefOf.Chemfuel);
            int availSilver = CountResource(homeMap, ThingDefOf.Silver);

            chemfuelToDeduct = Mathf.Min(totalFuelNeeded, availFuel);
            int missingFuel = totalFuelNeeded - chemfuelToDeduct;

            float fuelPrice = ThingDefOf.Chemfuel != null ? ThingDefOf.Chemfuel.BaseMarketValue : 2.3f;
            silverToDeduct = missingFuel > 0 ? Mathf.CeilToInt((float)missingFuel * fuelPrice) : 0;

            canAfford = availSilver >= silverToDeduct;

            if (silverToDeduct > 0)
            {
                costSummary = "StrategicOutpost_PodCostWithSilver".Translate(totalFuelNeeded, chemfuelToDeduct, silverToDeduct, availFuel, availSilver);
            }
            else
            {
                costSummary = "StrategicOutpost_PodCostFuelOnly".Translate(totalFuelNeeded, availFuel);
            }
        }

        public static bool TryPayLaunchCost(Map homeMap, int originTile, int destTile, float mass, out string failReason)
        {
            if (homeMap == null)
            {
                failReason = "StrategicOutpost_NoHomeMapAvailable".Translate();
                return false;
            }

            int totalFuel, fuelDeduct, silverDeduct;
            bool canAfford;
            string summary;
            CalculateCost(originTile, destTile, mass, homeMap, out totalFuel, out fuelDeduct, out silverDeduct, out canAfford, out summary);

            if (!canAfford)
            {
                failReason = summary;
                return false;
            }

            if (fuelDeduct > 0)
            {
                DeductResource(homeMap, ThingDefOf.Chemfuel, fuelDeduct);
            }
            if (silverDeduct > 0)
            {
                DeductResource(homeMap, ThingDefOf.Silver, silverDeduct);
            }

            failReason = null;
            return true;
        }

        public static TravellingTransporters Launch(PlanetTile originTile, PlanetTile destTile, List<Thing> cargo, TransportersArrivalAction arrivalAction)
        {
            if (cargo == null || cargo.Count == 0) return null;

            List<ActiveTransporterInfo> infos = new List<ActiveTransporterInfo>();
            ActiveTransporterInfo currentInfo = new ActiveTransporterInfo();
            currentInfo.openDelay = 110;
            currentInfo.leaveSlag = true;
            currentInfo.savePawnsWithReferenceMode = true;
            float currentMass = 0f;

            for (int i = 0; i < cargo.Count; i++)
            {
                Thing t = cargo[i];
                if (t == null) continue;
                float itemMass = t.GetStatValue(StatDefOf.Mass) * (float)t.stackCount;

                if (currentMass + itemMass > PodMassCapacity && currentInfo.innerContainer.Count > 0)
                {
                    infos.Add(currentInfo);
                    currentInfo = new ActiveTransporterInfo();
                    currentInfo.openDelay = 110;
                    currentInfo.leaveSlag = true;
                    currentInfo.savePawnsWithReferenceMode = true;
                    currentMass = 0f;
                }

                currentInfo.innerContainer.TryAdd(t, false);
                currentMass += itemMass;
            }

            if (currentInfo.innerContainer.Count > 0)
            {
                infos.Add(currentInfo);
            }

            TravellingTransporters worldObj = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravellingTransporters);
            worldObj.SetFaction(Faction.OfPlayer);
            worldObj.destinationTile = destTile;
            worldObj.arrivalAction = arrivalAction;
            worldObj.Tile = originTile;
            Find.WorldObjects.Add(worldObj);

            for (int i = 0; i < infos.Count; i++)
            {
                worldObj.AddTransporter(infos[i], true);
            }

            return worldObj;
        }
    }

    public class TransportersArrivalAction_OutpostArrival : TransportersArrivalAction
    {
        public int targetTile = -1;
        public bool autoUpgrade = false;

        public TransportersArrivalAction_OutpostArrival()
        {
        }

        public TransportersArrivalAction_OutpostArrival(int targetTile, bool autoUpgrade = false)
        {
            this.targetTile = targetTile;
            this.autoUpgrade = autoUpgrade;
        }

        public override bool GeneratesMap
        {
            get { return false; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetTile, "targetTile", -1);
            Scribe_Values.Look(ref autoUpgrade, "autoUpgrade", false);
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            // Stays valid while the outpost exists or a home map can take the cargo back (see Arrived).
            StrategicOutpost outpost = StrategicOutpostCaravanUtility.GetOutpostAtTile(destinationTile);
            if (outpost != null && outpost.Spawned) return true;
            return OutpostPodLogistics.GetHomeMap() != null;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            StrategicOutpost outpost = StrategicOutpostCaravanUtility.GetOutpostAtTile(tile);
            if (outpost == null || !outpost.Spawned)
            {
                // The outpost was dissolved or destroyed during the flight: drop everything at home instead of losing it.
                Map home = OutpostPodLogistics.GetHomeMap();
                if (home == null) return;
                new TransportersArrivalAction_ColonyRecall(home.Tile).Arrived(transporters, home.Tile);
                Messages.Message("StrategicOutpost_PodsReturnedOutpostGone".Translate(), MessageTypeDefOf.NeutralEvent);
                return;
            }

            int colonistsAdded = 0;
            int silverAdded = 0;

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo info = transporters[i];
                if (info == null || info.innerContainer == null) continue;

                List<Thing> things = new List<Thing>();
                for (int k = 0; k < info.innerContainer.Count; k++)
                {
                    things.Add(info.innerContainer[k]);
                }

                for (int j = 0; j < things.Count; j++)
                {
                    Thing item = things[j];
                    if (item == null) continue;

                    info.innerContainer.Remove(item);

                    Pawn p = item as Pawn;
                    if (p != null)
                    {
                        if (p.RaceProps.Animal)
                        {
                            outpost.Animals.TryAdd(p, false);
                        }
                        else
                        {
                            outpost.Workers.TryAdd(p, false);
                            if (p.ownership != null)
                            {
                                p.ownership.UnclaimBed();
                            }
                            colonistsAdded++;
                        }
                        StrategicOutpost.SetStoredPawnProtection(p, true);
                    }
                    else
                    {
                        outpost.RawMaterials.TryAdd(item, false);
                        if (item.def == ThingDefOf.Silver)
                        {
                            silverAdded += item.stackCount;
                        }
                    }
                }
            }

            Messages.Message("StrategicOutpost_PodsDeliveredToOutpost".Translate(outpost.LabelCap, colonistsAdded, silverAdded), outpost, MessageTypeDefOf.PositiveEvent);

            if (autoUpgrade && outpost.CanUpgrade)
            {
                string failReason;
                outpost.TryUpgrade(out failReason, null);
            }
        }
    }

    public class TransportersArrivalAction_ColonyRecall : TransportersArrivalAction
    {
        public int targetTile = -1;

        public TransportersArrivalAction_ColonyRecall()
        {
        }

        public TransportersArrivalAction_ColonyRecall(int targetTile)
        {
            this.targetTile = targetTile;
        }

        public override bool GeneratesMap
        {
            get { return false; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetTile, "targetTile", -1);
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            Map map = Current.Game != null ? Current.Game.FindMap(destinationTile) : null;
            if (map == null) map = OutpostPodLogistics.GetHomeMap();
            if (map == null)
            {
                return false;
            }
            return true;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            Map map = Current.Game != null ? Current.Game.FindMap(tile) : null;
            if (map == null) map = OutpostPodLogistics.GetHomeMap();
            if (map == null)
            {
                Log.Warning("TransportersArrivalAction_ColonyRecall: Target home map not found.");
                return;
            }

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            if (!dropSpot.IsValid)
            {
                dropSpot = DropCellFinder.RandomDropSpot(map);
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo info = transporters[i];
                if (info != null)
                {
                    for (int k = 0; k < info.innerContainer.Count; k++)
                    {
                        Pawn p = info.innerContainer[k] as Pawn;
                        if (p != null)
                        {
                            if (Find.WorldPawns != null && Find.WorldPawns.Contains(p))
                            {
                                Find.WorldPawns.RemovePawn(p);
                            }
                            if (p.Faction != Faction.OfPlayer)
                            {
                                p.SetFaction(Faction.OfPlayer);
                            }
                        }
                    }
                    DropPodUtility.MakeDropPodAt(dropSpot, map, info, Faction.OfPlayer);
                }
            }

            Messages.Message("StrategicOutpost_PodsArrivedAtColony".Translate(), new GlobalTargetInfo(dropSpot, map), MessageTypeDefOf.PositiveEvent);
        }
    }
}
```

## Твой StoryRelationRestorer из исправления E2 (расширь его)
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Tracks a single active raid lease on a faction's hostility override.
    /// </summary>
    public sealed class StoryHostilityLease : IExposable
    {
        public string leaseId;
        public string quest;
        public string stageKey;
        public int runGen;
        public bool restoreRelations;

        public void ExposeData()
        {
            Scribe_Values.Look(ref leaseId, "leaseId");
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref restoreRelations, "restoreRelations", false);
        }
    }

    /// <summary>
    /// Stores original faction goodwill &amp; relation kind before any concurrent story raids forced
    /// the faction Hostile. Restoration runs ONLY when the final lease for that faction is released
    /// or orphaned (e.g. quest completed, failed, disabled, stage removed, or exception thrown).
    /// </summary>
    public sealed class StoryFactionBackup : IExposable
    {
        public int factionId = -1;
        public int origGoodwill;
        public int origKind = (int)FactionRelationKind.Neutral;
        public bool origHasGoodwill;
        public int postForceGoodwill = -100;
        public bool restoreWanted;
        public List<StoryHostilityLease> leases = new List<StoryHostilityLease>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionId, "factionId", -1);
            Scribe_Values.Look(ref origGoodwill, "origGoodwill", 0);
            Scribe_Values.Look(ref origKind, "origKind", (int)FactionRelationKind.Neutral);
            Scribe_Values.Look(ref origHasGoodwill, "origHasGoodwill", true);
            Scribe_Values.Look(ref postForceGoodwill, "postForceGoodwill", -100);
            Scribe_Values.Look(ref restoreWanted, "restoreWanted", false);
            Scribe_Collections.Look(ref leases, "leases", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && leases == null)
            {
                leases = new List<StoryHostilityLease>();
            }
        }
    }

    /// <summary>
    /// Protected unique Retrieve item record tied to a specific quest run so stack merging is blocked
    /// across save/reload and automatically released when the stage ends or is disabled.
    /// </summary>
    public sealed class StoryProtectedItem : IExposable
    {
        public string thingId;
        public string quest;
        public string stageKey;
        public int runGen;

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingId, "thingId");
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref runGen, "runGen", 0);
        }
    }

    /// <summary>
    /// WorldComponent that guarantees:
    /// 1) Temporary raid hostility is rolled back properly (handling RimWorld's goodwill/kind
    ///    hysteresis thresholds, concurrent raids on the same faction, mid-raid goodwill changes,
    ///    exceptions, disabled quests, and removed Defs/stages).
    /// 2) Retrieve quest items cannot merge into arbitrary stacks while active.
    /// </summary>
    public sealed class StoryRelationRestorer : WorldComponent
    {
        private const int CheckInterval = 250;

        private List<StoryFactionBackup> backups = new List<StoryFactionBackup>();
        private List<StoryProtectedItem> protectedItems = new List<StoryProtectedItem>();

        // Transient fast lookup for Thing.CanStackWith patch
        private static readonly HashSet<string> ActiveProtectedIds = new HashSet<string>();

        public StoryRelationRestorer(World world) : base(world)
        {
        }

        public static StoryRelationRestorer Instance
        {
            get { return Find.World == null ? null : Find.World.GetComponent<StoryRelationRestorer>(); }
        }

        public static void CheckOrphansIfPresent()
        {
            StoryRelationRestorer inst = Instance;
            if (inst != null) inst.CheckOrphansNow();
        }

        public static string MakeLeaseId(StoryQuestRun run)
        {
            if (run == null) return string.Empty;
            return string.Format("{0}|{1}|{2}", run.quest ?? string.Empty, run.stageKey ?? string.Empty, run.gen);
        }

        // =====================================================================
        // HOSTILITY LEASES & GUARANTEED ROLLBACK
        // =====================================================================

        /// <summary>
        /// Records a rollback backup BEFORE mutating faction relations, forces the faction Hostile
        /// (verifying the resulting state), and tracks parallel raids on the same faction.
        /// Returns false if hostility could not be established (rolling back any partial state).
        /// </summary>
        public bool AcquireHostilityLease(Faction fac, StoryQuestRun run, bool restoreRelations, out string leaseId)
        {
            leaseId = null;
            if (fac == null || fac.IsPlayer || run == null) return false;

            leaseId = MakeLeaseId(run);
            StoryFactionBackup existing = FindBackup(fac.loadID);

            // If faction is already hostile AND we have no active backup for it, no rollback is needed.
            if (existing == null && fac.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            if (existing == null)
            {
                // Record original state BEFORE mutating anything
                existing = new StoryFactionBackup();
                existing.factionId = fac.loadID;
                existing.origGoodwill = fac.PlayerGoodwill;
                existing.origKind = (int)fac.RelationKindWith(Faction.OfPlayer);
                existing.origHasGoodwill = fac.HasGoodwill;
                existing.restoreWanted = restoreRelations;
                backups.Add(existing);

                AddOrUpdateLease(existing, leaseId, run, restoreRelations);

                if (!ForceFactionHostile(fac))
                {
                    // Rollback backup immediately if forcing hostility failed
                    backups.Remove(existing);
                    RestoreFactionRelations(fac, existing);
                    leaseId = null;
                    return false;
                }

                existing.postForceGoodwill = fac.PlayerGoodwill;
                return true;
            }

            // Concurrent raid on a faction that already has an active backup:
            // keep the original pre-first-raid baseline and add this raid's lease so restoration
            // waits until the LAST raid finishes.
            if (restoreRelations) existing.restoreWanted = true;
            AddOrUpdateLease(existing, leaseId, run, restoreRelations);

            if (!fac.HostileTo(Faction.OfPlayer))
            {
                if (!ForceFactionHostile(fac))
                {
                    RemoveLease(existing, leaseId);
                    leaseId = null;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Releases a specific raid lease. If no leases remain for that faction and restoreWanted is true,
        /// restores its relation kind and goodwill immediately.
        /// </summary>
        public void ReleaseLease(string leaseId)
        {
            if (leaseId.NullOrEmpty() || backups == null) return;

            for (int i = backups.Count - 1; i >= 0; i--)
            {
                StoryFactionBackup b = backups[i];
                if (b == null)
                {
                    backups.RemoveAt(i);
                    continue;
                }

                if (RemoveLease(b, leaseId))
                {
                    if (b.leases.Count == 0)
                    {
                        FinalizeBackup(b);
                        backups.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Audits all active leases and protected items against SparkStory.
        /// Any lease whose quest ended, was disabled, changed generation, or lost its Def/stage
        /// is immediately released and restored if it was the last lease for that faction.
        /// </summary>
        public void CheckOrphansNow()
        {
            SparkStory story = SparkStory.Instance;

            if (backups != null)
            {
                for (int i = backups.Count - 1; i >= 0; i--)
                {
                    StoryFactionBackup b = backups[i];
                    if (b == null)
                    {
                        backups.RemoveAt(i);
                        continue;
                    }

                    if (b.leases == null) b.leases = new List<StoryHostilityLease>();
                    for (int l = b.leases.Count - 1; l >= 0; l--)
                    {
                        StoryHostilityLease lease = b.leases[l];
                        if (!IsRaidLeaseStillActive(story, lease))
                        {
                            b.leases.RemoveAt(l);
                        }
                    }

                    if (b.leases.Count == 0)
                    {
                        FinalizeBackup(b);
                        backups.RemoveAt(i);
                    }
                }
            }

            if (protectedItems != null)
            {
                bool changed = false;
                for (int i = protectedItems.Count - 1; i >= 0; i--)
                {
                    StoryProtectedItem item = protectedItems[i];
                    if (!IsProtectedItemStillActive(story, item))
                    {
                        protectedItems.RemoveAt(i);
                        changed = true;
                    }
                }
                if (changed) RebuildProtectedCache();
            }
        }

        private static bool IsRaidLeaseStillActive(SparkStory story, StoryHostilityLease lease)
        {
            if (story == null || lease == null || lease.quest.NullOrEmpty()) return false;
            StoryQuestRun run = story.RunFor(lease.quest);
            if (run == null || run.status != StoryQuestStatus.Active) return false;
            if (run.gen != lease.runGen) return false;
            if (!lease.stageKey.NullOrEmpty() && run.stageKey != lease.stageKey) return false;

            StoryQuestDef def = run.Def;
            if (def == null || run.stage < 0 || run.stage >= def.stages.Count) return false;
            return def.stages[run.stage] is StoryStage_ColonyRaid;
        }

        private static bool IsProtectedItemStillActive(SparkStory story, StoryProtectedItem item)
        {
            if (story == null || item == null || item.thingId.NullOrEmpty() || item.quest.NullOrEmpty()) return false;
            StoryQuestRun run = story.RunFor(item.quest);
            if (run == null || run.status != StoryQuestStatus.Active) return false;
            if (run.gen != item.runGen) return false;
            if (!item.stageKey.NullOrEmpty() && run.stageKey != item.stageKey) return false;

            StoryQuestDef def = run.Def;
            if (def == null || run.stage < 0 || run.stage >= def.stages.Count) return false;
            return def.stages[run.stage] is StoryStage_Site;
        }

        private void FinalizeBackup(StoryFactionBackup b)
        {
            if (b == null || !b.restoreWanted) return;
            Faction fac = FindFactionById(b.factionId);
            if (fac == null || fac.defeated) return;
            RestoreFactionRelations(fac, b);
        }

        /// <summary>
        /// Forces a faction Hostile to player and verifies the resulting relation kind.
        /// </summary>
        private static bool ForceFactionHostile(Faction fac)
        {
            if (fac == null || fac.IsPlayer) return false;
            if (fac.HostileTo(Faction.OfPlayer)) return true;

            if (fac.HasGoodwill && fac.CanChangeGoodwillFor(Faction.OfPlayer, -100)) // CHECK API
            {
                int drop = -100 - fac.PlayerGoodwill;
                if (drop < 0)
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null); // CHECK API
                }
            }

            if (!fac.HostileTo(Faction.OfPlayer))
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }

            return fac.HostileTo(Faction.OfPlayer);
        }

        /// <summary>
        /// Restores BOTH relation kind and goodwill, explicitly crossing RimWorld's CheckKindThresholds
        /// hysteresis boundary (0 for exiting Hostile) and accounting for external goodwill deltas
        /// accumulated while the raid was active.
        /// </summary>
        private static void RestoreFactionRelations(Faction fac, StoryFactionBackup b)
        {
            if (fac == null || b == null) return;

            FactionRelationKind origKind = (FactionRelationKind)b.origKind;

            // Preserve any external goodwill shifts that occurred during the raid (e.g. gifts or other quests)
            int externalDelta = fac.HasGoodwill ? (fac.PlayerGoodwill - b.postForceGoodwill) : 0;
            int targetGoodwill = Mathf.Clamp(b.origGoodwill + externalDelta, -100, 100);

            // Ensure target goodwill respects the original relation kind's valid range
            if (origKind == FactionRelationKind.Neutral && targetGoodwill < -74)
            {
                targetGoodwill = -74;
            }
            else if (origKind == FactionRelationKind.Ally && targetGoodwill < 0)
            {
                targetGoodwill = 0;
            }

            if (fac.HasGoodwill)
            {
                // Step 1: If currently Hostile and restoring to non-Hostile, cross the >= 0 threshold first
                // so vanilla FactionRelation.CheckKindThresholds cleanly drops Hostile and notifies maps/lords.
                if (fac.HostileTo(Faction.OfPlayer) && origKind != FactionRelationKind.Hostile)
                {
                    int liftTarget = origKind == FactionRelationKind.Ally ? Mathf.Max(75, targetGoodwill) : Mathf.Max(0, targetGoodwill);
                    int liftDelta = liftTarget - fac.PlayerGoodwill;
                    if (liftDelta > 0)
                    {
                        fac.TryAffectGoodwillWith(Faction.OfPlayer, liftDelta, false, false, null, null); // CHECK API
                    }
                }

                // Step 2: Adjust to exact targetGoodwill (e.g. 0 -> -50 stays Neutral because -50 > -75)
                int finalDelta = targetGoodwill - fac.PlayerGoodwill;
                if (finalDelta != 0)
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, finalDelta, false, false, null, null); // CHECK API
                }

                // Step 3: Verification & hard fallback if TryAffectGoodwillWith was refused or hysteresis stuck
                if (fac.RelationKindWith(Faction.OfPlayer) != origKind)
                {
                    fac.SetRelationDirect(Faction.OfPlayer, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API
                }

                if (fac.PlayerGoodwill != targetGoodwill || fac.RelationKindWith(Faction.OfPlayer) != origKind)
                {
                    FactionRelation rel = fac.RelationWith(Faction.OfPlayer, true); // CHECK API
                    if (rel != null)
                    {
                        rel.baseGoodwill = targetGoodwill; // CHECK API
                        rel.kind = origKind; // CHECK API
                    }
                }
            }
            else
            {
                if (fac.RelationKindWith(Faction.OfPlayer) != origKind)
                {
                    fac.SetRelationDirect(Faction.OfPlayer, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API
                }
            }
        }

        private StoryFactionBackup FindBackup(int factionId)
        {
            if (backups == null) return null;
            for (int i = 0; i < backups.Count; i++)
            {
                if (backups[i] != null && backups[i].factionId == factionId) return backups[i];
            }
            return null;
        }

        private static void AddOrUpdateLease(StoryFactionBackup b, string leaseId, StoryQuestRun run, bool restoreRelations)
        {
            if (b.leases == null) b.leases = new List<StoryHostilityLease>();
            for (int i = 0; i < b.leases.Count; i++)
            {
                if (b.leases[i] != null && b.leases[i].leaseId == leaseId)
                {
                    b.leases[i].restoreRelations = restoreRelations;
                    return;
                }
            }
            StoryHostilityLease lease = new StoryHostilityLease();
            lease.leaseId = leaseId;
            lease.quest = run.quest;
            lease.stageKey = run.stageKey;
            lease.runGen = run.gen;
            lease.restoreRelations = restoreRelations;
            b.leases.Add(lease);
        }

        private static bool RemoveLease(StoryFactionBackup b, string leaseId)
        {
            if (b == null || b.leases == null) return false;
            bool removed = false;
            for (int i = b.leases.Count - 1; i >= 0; i--)
            {
                if (b.leases[i] != null && b.leases[i].leaseId == leaseId)
                {
                    b.leases.RemoveAt(i);
                    removed = true;
                }
            }
            return removed;
        }

        private static Faction FindFactionById(int loadId)
        {
            if (loadId < 0 || Find.FactionManager == null) return null;
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId) return all[i];
            }
            return null;
        }

        // =====================================================================
        // PROTECTED UNIQUE RETRIEVE ITEMS (ANTI-STACK-MERGE)
        // =====================================================================

        public void ProtectRetrieveItem(string thingId, StoryQuestRun run)
        {
            if (thingId.NullOrEmpty() || run == null) return;
            if (protectedItems == null) protectedItems = new List<StoryProtectedItem>();

            for (int i = 0; i < protectedItems.Count; i++)
            {
                if (protectedItems[i] != null && protectedItems[i].thingId == thingId)
                {
                    protectedItems[i].quest = run.quest;
                    protectedItems[i].stageKey = run.stageKey;
                    protectedItems[i].runGen = run.gen;
                    ActiveProtectedIds.Add(thingId);
                    return;
                }
            }

            StoryProtectedItem entry = new StoryProtectedItem();
            entry.thingId = thingId;
            entry.quest = run.quest;
            entry.stageKey = run.stageKey;
            entry.runGen = run.gen;
            protectedItems.Add(entry);
            ActiveProtectedIds.Add(thingId);
        }

        public void UnprotectRetrieveItem(string thingId)
        {
            if (thingId.NullOrEmpty()) return;
            ActiveProtectedIds.Remove(thingId);
            if (protectedItems == null) return;
            for (int i = protectedItems.Count - 1; i >= 0; i--)
            {
                if (protectedItems[i] != null && protectedItems[i].thingId == thingId)
                {
                    protectedItems.RemoveAt(i);
                }
            }
        }

        public static bool IsProtectedThing(Thing t)
        {
            if (t == null || ActiveProtectedIds.Count == 0) return false;
            return ActiveProtectedIds.Contains(t.ThingID);
        }

        private void RebuildProtectedCache()
        {
            ActiveProtectedIds.Clear();
            if (protectedItems == null) return;
            for (int i = 0; i < protectedItems.Count; i++)
            {
                if (protectedItems[i] != null && !protectedItems[i].thingId.NullOrEmpty())
                {
                    ActiveProtectedIds.Add(protectedItems[i].thingId);
                }
            }
        }

        // =====================================================================
        // WORLD TICK & SCRIBE
        // =====================================================================

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;
            CheckOrphansNow();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref backups, "TMM_raidRelationBackups", LookMode.Deep);
            Scribe_Collections.Look(ref protectedItems, "TMM_protectedRetrieveItems", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (backups == null) backups = new List<StoryFactionBackup>();
                if (protectedItems == null) protectedItems = new List<StoryProtectedItem>();
                RebuildProtectedCache();
            }
        }
    }

    // =========================================================================
    // HARMONY PATCH: BLOCK STACK MERGING FOR PROTECTED RETRIEVE QUEST ITEMS
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class StoryRetrieveStackPatch
    {
        static StoryRetrieveStackPatch()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.storystages");
            MethodInfo canStack = AccessTools.Method(typeof(Thing), "CanStackWith", new Type[] { typeof(Thing) }); // CHECK API
            if (canStack != null)
            {
                harmony.Patch(canStack, postfix: new HarmonyMethod(typeof(StoryRetrieveStackPatch), "Postfix_CanStackWith"));
            }
        }

        public static void Postfix_CanStackWith(Thing __instance, Thing other, ref bool __result)
        {
            if (!__result) return;
            if (StoryRelationRestorer.IsProtectedThing(__instance) || StoryRelationRestorer.IsProtectedThing(other))
            {
                __result = false;
            }
        }
    }
}
```

## Образец безопасного переноса героя (StoryDefs.cs)
```csharp
    public sealed class StoryEffect_HeroJoins : StoryEffect
    {
        public HeroDef hero;
        public override void Apply(StoryContext ctx) { TryJoin(hero, ctx); }

        /// Safe ownership transfer: only a free living world pawn is moved; the drop pod must really take it, otherwise
        /// everything is rolled back. A hero who already belongs to the player counts as joined (idempotent).
        public static bool TryJoin(HeroDef hero, StoryContext ctx)
        {
            Map map = ctx.Home;
            HeroRegistry reg = HeroRegistry.Instance;
            if (map == null || reg == null || hero == null) return false;
            Pawn p = reg.GetOrCreate(hero);
            if (p == null || p.Dead || p.Destroyed) return false;
            if (p.Faction == Faction.OfPlayer && !p.IsPrisoner) { MarkJoined(hero, p, ctx, reg); return true; }
            if (p.Spawned || p.IsPrisoner || p.IsSlave || p.holdingOwner != null || p.GetCaravan() != null || !Find.WorldPawns.Contains(p)) return false;

            Faction oldFaction = p.Faction;
            bool wasLeader = oldFaction != null && oldFaction.leader == p;
            Find.WorldPawns.RemovePawn(p);
            p.SetFaction(Faction.OfPlayer);
            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, new List<Thing> { p }, 110, false, false, true, false);
            if (p.holdingOwner == null && !p.Spawned)
            {
                // The pod did not accept the pawn: restore everything.
                if (oldFaction != null) p.SetFaction(oldFaction);
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                if (wasLeader) oldFaction.leader = p;
                return false;
            }
            if (wasLeader)
            {
                Pawn next = reg.SuccessorForVacantLeadership(oldFaction);
                oldFaction.leader = next;
                if (next == null) oldFaction.TryGenerateNewLeader();
            }
            MarkJoined(hero, p, ctx, reg);
            return true;
        }

        private static void MarkJoined(HeroDef hero, Pawn p, StoryContext ctx, HeroRegistry reg)
        {
            reg.NotifyEncountered(p);
            ctx.state.SetFlag("defector." + hero.defName, "true");
        }
    }


```

## Сдать
Файлы с путём над каждым блоком:
- `Source/StoryStage_HotelVisit.cs`
- `Source/StoryStage_Deliver.cs`
- `Source/StoryRelationRestorer.cs` — расширенный, весь целиком, со старым API отката рейда
- `Source/StoryPayment.cs` — общий атомарный помощник списания
- `Source/OutpostPodLogistics.cs` — только если меняешь его списание; тогда весь файл, API сохранить
- `Tests/InGameStoryHotelProbe.cs` — новая проба. Каждую находку покрой **реальным сценарием**, без принудительного вывода гостей и без сдвига часов там, где проверяется само поведение:
  - герой-колонист не переносится;
  - визит фракции с постоянной враждой → `failed` без спавна;
  - после визита отношения восстановлены, в том числе при отключённом квесте;
  - гости убиты или взяты в плен → не `success`;
  - reload с активной группой: lord и состав совпадают, герой не задвоен;
  - Investigate без беседы → не `success`;
  - Hide без облавы → дедлайн;
  - клик по письму после дедлайна ничего не списывает;
  - отказ второго стека → всё на месте, `delivered` нет;
  - reload после принятой оплаты → без повторного списания;
  - проходящий караван не списывается, прибывший — списывается.
  Результат UI-проверки пиши честно, по факту.

В конце — что изменено по каждой находке P1/P2 и по пунктам «Сохранения и совместимость».
