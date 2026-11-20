**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий. Это первый из четырёх промптов второго раунда (44 → 45 → 46 → 47), отправляются по очереди в один чат.**

Codex перепроверил твои исправления E2, E3 и караванов. Большая часть находок закрыта, но все три работы снова «НЕ ПРИНИМАТЬ». Общая причина: откаты отношений, платежей и переносов пешек не доведены до конца при сбое посреди операции. Поэтому сначала чиним **общее ядро**, а этапы в 45–47 переводим на него.

## Задача 44 — общее ядро
1. **`Source/StoryRelationRestorer.cs`.**
   - Запасной откат (когда goodwill менять запрещено) сейчас пишет `baseGoodwill/kind` только со стороны NPC. Восстанавливай **симметрично**: обе стороны `FactionRelation`, как это делает ваниль в `TryAffectGoodwillWith` (Codex сверил через Cecil). Уведомления отправляй как при штатной смене отношения.
   - Backup удаляется **только после проверенного** восстановления. При неудаче запись остаётся и повторяется при следующей сверке.
   - Аренда перемирия: если для фракции уже есть временная запись нейтралитета, второй визит **тоже** берёт аренду (счётчик). Восстановление делает только последняя освобождённая аренда.
   - Если рейд не состоялся (спавн отказал или бросил исключение), принудительная враждебность откатывается **всегда**, независимо от `restoreRelations`.
2. **`Source/StoryPayment.cs`** — атомарность по журналу (см. общие правила):
   - каждый `SplitOff` и перенос в staging записывается до следующего вызова;
   - откат проверяет каждый `TryAdd`, а при отказе кладёт вещь в доступный holder;
   - уничтожение — после подтверждения полной суммы; исключение в commit не должно оставить частично уничтоженную оплату без записи;
   - `TestSplitOffOverride` — только в тестовом режиме.
3. **`Source/OutpostPodLogistics.cs`** — убери обход: после отказа атомарного метода сейчас снова списывается старым циклом, включая частичное списание. При отказе — отказ, без списания.
4. **Новый `Source/StoryPawnTransfer.cs`** — общий безопасный перенос пешки для этапов. Один вызов: «взять свободную мировую пешку, назначить фракцию, заспавнить на карте в клетке». Внутри:
   - проверки владельца, как в `TryJoin`;
   - снимок исходного состояния: фракция (в том числе null), гостевой статус, лидерство фракции, место в WorldPawns;
   - журнал шагов; при false или исключении — полный откат и возврат в WorldPawns с прежним состоянием;
   - снятие с WorldPawns — **последним шагом перед спавном**, когда группа (lord, сопровождение) уже готова.
   Результат: `bool` и причина отказа. Этапы в 45 и 46 будут звать только его.
5. **Новый `Source/TestMode.cs`** — `public static bool Enabled`, кешированная проверка аргумента `-tmm-story-test`. Все тестовые хуки мода используют его.

## Сдать
Целиком: `Source/StoryRelationRestorer.cs`, `Source/StoryPayment.cs`, `Source/OutpostPodLogistics.cs`, `Source/StoryPawnTransfer.cs`, `Source/TestMode.cs`. В конце — коротко, что изменено по каждому пункту и **публичный API `StoryPawnTransfer` и `StoryPayment`**: в 45–47 ты будешь их использовать.

## Общие правила второго раунда (одинаковые для 44–47)
- C# 5, RimWorld 1.7, `// CHECK API` там, где не уверен. Публичный API, который вызывают другие файлы, сохраняй; если меняешь — перечисли.
- **Каждая мутация — с журналом до следующего вызова.** Перед любым `SplitOff`, `RemovePawn`, `DeSpawn`, `SetFaction`, `TryAdd`, `Destroy` запиши в журнал операции, что именно сейчас изменится и куда это вернуть. Откат идёт по журналу в обратном порядке. **Каждый шаг отката проверяется**: если `TryAdd` вернул false, вещь кладётся в доступный holder — на карту рядом с исходным местом (`GenPlace`) или в инвентарь любой пешки того же каравана. Ничего не остаётся без владельца. Весь перенос обёрнут в `try/catch`: исключение → откат по журналу → явный исход.
- **Необратимое — последним.** Уничтожение оплаты или окончательная смена фракции выполняются только после того, как вся операция подтверждена. Сразу после этого один раз записывается `delivered` (или аналог).
- **Тестовые хуки** (`TestSplitOffOverride`, `TestRejectRawMaterialsTransfer` и подобные) работают только при аргументе запуска `-tmm-story-test`, иначе игнорируются. Проверяй через общий `TestMode.Enabled` (см. 44).
- **Проба честная:** UI-файл пишет `passed` по фактическому результату. Сценарии из отчёта Codex «не покрыто» должны появиться реальными действиями, а не прямой подстановкой полей.
- Отвечай частями, если не помещается: каждая часть — законченные файлы, путь над каждым блоком. Часть называй `44_1`, `44_2` и т. д.

## Отчёт Codex: StoryStageSiteRecheck
### StoryStageSiteRecheck — НЕ ПРИНИМАТЬ

## По прежним находкам
- **P1 · Удаление занятой карты — закрыта.** `Source/StoryStage_Site.cs:189,283–305`. Прямое Destroy только без карты. Таймаут на загруженной карте оставляет её ванильному снятию.
- **P1 · Перенос героя — частично.** `Source/StoryStage_Site.cs:335–344,375–480,542–571`. Проверки фракции игрока, плена, holder и WorldPawns добавлены, ложный успех на Missing и предсуществующем караване убран. Откат срабатывает на возвращённых ошибках, но не при исключении после RemovePawn. Например, если GenSpawn бросит исключение после смены фракции, SafeTick отключит квест, а пешка не вернётся в WorldPawns. Обернуть весь переход в проверяемую операцию с откатом исходного владельца, фракции (включая null), состояния гостя и лидера.
- **P1 · Подмена предмета — закрыта.** `Source/StoryStage_Site.cs:491–521,574–608`, `Source/StoryRelationRestorer.cs:848–873`. Точный ID, проверка размещения, запрет слияния. **Новый риск P2:** точный предмет в летящей капсуле не виден ни на карте, ни в Caravan и сразу засчитывается как потерянный. А точный предмет в инвентаре NPC на домашней карте засчитывается доставленным (`Source/StoryStage_Site.cs:914–964`). Различать фактическое исчезновение, транзит и подтверждённое владение игрока.
- **P1 · Откат отношений — частично, блокер.** `Source/StoryRelationRestorer.cs:173–229,328–381,460–465,511–578`. Пороговые переходы и общий сохраняемый откат добавлены. Однако при запрете goodwill запасная ветка записывает `baseGoodwill/kind` только со стороны NPC, не обновляет зеркальное отношение игрока и пропускает уведомления. Cecil подтверждает, что ванильный TryAffectGoodwillWith синхронизирует обе стороны. Backup затем удаляется без подтверждения результата. Также при `restoreRelations=false` отказ самого спавна оставляет принудительную враждебность (`Source/StoryStage_ColonyRaid.cs:234–244`). Откатывать несостоявшийся рейд независимо от режима, а для штатного отката сохранять backup до подтверждённого двустороннего восстановления с уведомлениями.
- **P1 · Идентификация рейда — частично.** `Source/StoryStage_ColonyRaid.cs:98–148,247–292,300–327`. Map ID, проверка срока, набор ID рейдеров и капсулы добавлены, fallback на любых врагов фракции убран. Но при пустом наборе и сохранённом ID уже пропавшего lord ветка возвращает success, хотя ни один рейдер не был подтверждён. При старте требовать непустой подтверждённый состав либо положительное доказательство завершения lord.
- **P2 · Hold — частично.** `Source/StoryStage_Site.cs:189–194,611–642`. Первый опрос добавляет ноль, частичный прогресс сохраняется, дедлайн проверяется первым. Присутствие по краям интервала всё ещё засчитывает время отсутствия, если все ушли и вернулись между опросами. Учитывать реальные границы присутствия либо явно принять шаг дискретизации.
- **P2 · Потеря письма при anyOf — закрыта.** `Source/StoryLetter.cs:85–104`. Проверка до Resolve и запрос повторной отправки при отказе. Проверку синтаксиса XML в ConfigErrors в этой приёмке не сверял.
- **Хук и нагрузка.** `Source/SparkStory.cs:203–232,321–327` ограничивает чтение старого ключа тестовым флагом, новый исход не сериализуется и запускает очистку записей. В E2 отдельной ветки forceOutcome больше нет. Пропавший deadline сохраняется один раз. Рейд сканирует карту за проход, Retrieve всё ещё обходит караваны, карты и holders на каждом опросе (`Source/StoryStage_Site.cs:580–608`).

## Проба и границы
- `Tests/InGameStoryStagesProbe.cs:249–269,352–389,518–609` проверяет реальные объекты и исходы: живой лежачий на карте, спавн/гибель конкретных рейдеров, извлечение пленника и точного предмета после reload, сохранённый прогресс Hold. Часы двигаются принудительно, выходы и смена статуса моделируются. Тест orphan cleanup напрямую меняет status и вызывает restorer (`:414–421`), поэтому не доказывает сценарий удаления Def или исключения. Награды не измеряются, проверяется история.
- `Tests/InGameStoryStagesProbe.cs:433–438` передаёт в тест хука уже завершённый timeout-run, который был бы отклонён даже без проверки аргумента. Нужен активный run и отдельный тест старого ключа в vars. UI-файл всё ещё пишет безусловный passed=true (`:220–221`). Сейв после общего SanitizeDanglingPawnRelations (`:507`) не доказывает отсутствие проблем без подготовки.
- Отчёт Antigravity указывает fresh 10/10 и reload 4/4 на `TestRuns/StoryStages-FullSet-20261120-025931`, CompatibilityReload=false, runtimeErrors=0, unresolvedRefs=0, SHA256 `134f3b4b4a0f5adb7dce04fb0869d6be8a6131f5ae3986412fd493e230285a7e`. `Get-FileHash` текущей DLL, exit 0, дал `da47d673ca9f0036330546923cbdd99b70729bc1b5c097f25e36f585bc77a183`. Зелёный отчёт относится к прежней сборке. `git status --short` по файлам ревью чист, `git log` указывает интеграцию dee9f09. `cecil_methods.ps1 -Type RimWorld.Faction -Name GoodwillWith/TryAffectGoodwillWith -IL` (по отдельности), exit 0. Ревью статическое, игра и сборка не запускались.


## Отчёт Codex: StoryStageHotelRecheck
### StoryStageHotelRecheck — НЕ ПРИНИМАТЬ

- **P1 · Перенос занятого героя — частично.** `Source/StoryStage_HotelVisit.cs:293–374,806–825`. Колонист, пленник, пешка с карты/каравана/holder отсеиваются, второй экземпляр не создаётся. Но после RemovePawn исключение генератора сопровождения или GenSpawn обходит возврат героя. Отложить снятие с WorldPawns до готовности группы и обработать исключения по всему переносу с возвратом в исходное состояние.
- **P1 · Перемирие и E2 — частично.** `Source/StoryStage_HotelVisit.cs:258–285`, `Source/StoryRelationRestorer.cs:173–191,263–325,328–381,533–578`. Перемирие проверяется до спавна, несовместимые режимы аренды рейда/визита отклоняются. Второй визит той же фракции видит временный нейтралитет и пропускает AcquireTruceLease, поэтому первый визит может восстановить вражду, пока второй активен. Запрашивать аренду и для уже нейтральной фракции, если есть временная запись. Общий запасной откат по-прежнему пишет только сторону NPC и удаляет backup даже при неудаче. Это касается и E2. Возвращать симметричные отношения с уведомлениями и проверять результат до удаления записи.
- **P1 · Host и сохранение — исходная проблема закрыта.** `Source/StoryStage_HotelVisit.cs:217,527–630,654–731`, `Source/StoryRelationRestorer.cs:774–797,829–831`. Привязаны карта, lord и ID гостей. Пропажа без подтверждения мирного ухода даёт failed, есть крайний срок. Учёт ухода ведётся в префиксе ExitMap, поэтому факт ухода всё равно должен подтверждаться фактическим снятием с карты. При отсутствии Hospitality, закрытии приёма или отсутствии кровати на старте срабатывает noHotel (`Source/StoryStage_HotelVisit.cs:195–227`).
- **P1 · Investigate/Hide — частично.** `Source/StoryStage_HotelVisit.cs:637–666,711–728,847–946`. Комнатный fallback убран, расследование требует запись о взаимодействии. Hide ждёт свою фракцию и конкретный lord, но берёт любой новый враждебный lord указанной фракции, не проверяя, что это именно инцидент облавы. Связать событие облавы с этапом и считать только его когорту.
- **P1 · Просроченный клик и старая карта — частично.** `Source/StoryStage_Deliver.cs:94–114,419–453,477–531,563–596`. Штатные статус, поколение, срок, письмо, карта и клетка проверяются. Однако после удаления stageKey до Reconcile поиск возвращается к прежнему индексу, который может указывать на другой Deliver. На клик допускается списание по старому requiredCount письма с Def нового этапа. При заданном ключе отклонять его отсутствие, брать количество из проверённого текущего этапа или неизменяемого договора, исключить оплату при неизвестном дедлайне.
- **P1 · Атомарность StoryPayment — частично, блокер.** `Source/StoryPayment.cs:204–255,347–401,410–484`. Чистый отказ SplitOff до мутации откатывает ранее записанные части. Но SplitOff/staging могут бросить после отделения текущего куска, до записи в reserved. Возврат в карту/инвентарь игнорирует false, а исключение при уничтожении в commit оставит уже уничтоженную часть без delivered. Регистрировать каждое изменение до следующего вызова, проверять откат и хранить неподнятый остаток в доступном holder. Подтверждать полноценный платёж до необратимой утилизации. Защитить производственный TestSplitOffOverride тестовым флагом или внедрять подмену только из пробы (`:39,410–416`). `Source/OutpostPodLogistics.cs:53–79` после любого отказа атомарного метода снова списывает старым циклом, включая частичное списание. Убрать этот обход при сбое, даже если для капсул сохраняется отдельная политика выбора стеков.
- **P2 · Чужое поселение и проезд мимо — закрыта.** `Source/StoryStage_Deliver.cs:283–299,360–372,563–596,716–736`. Фиксирует поселение нужной фракции, списывает на его клетке по действию Arrived, пропажу обрабатывает отдельно. Источник платы — только прямой инвентарь данного каравана или склад закреплённой карты, вложенные вещи явно исключены.

Проба действительно проверяет результат списания 60→60 на отказе перед второй мутацией, проходящий караван и сохранение после оплаты до перехода (сталь 50→50, один исход). Но не покрывает отказ после отделения, невозможность возврата или конкурирующие визиты. Investigate вручную вставляет PlayLogEntry, проверяя чтение лога, а не игровую беседу. UI-файл безусловно пишет passed=true. См. `Tests/InGameStoryHotelProbe.cs:225–231,460–469,547–561,659–667,719–730`. Отдельного считывания forceOutcome в этапах E3 больше нет.

Отчёт Antigravity указывает fresh 17/17 и reload 3/3, 0 ошибок и нерешённых ссылок, `TestRuns/StoryHotel-FullSet-20261120-045806`, DLL `03d6d6dc00b57bbbe75602a2b244d7a24ce4a92ba27917b7d39c20da8f56a1d0`. `Get-FileHash`, exit 0: текущая DLL `da47d673ca9f0036330546923cbdd99b70729bc1b5c097f25e36f585bc77a183`. Хэши различаются. `git status --short` по файлам ревью и `git log -1`, exit 0: дерево чистое, интеграция dee9f09. `cecil_methods.ps1 -Type RimWorld.Faction -Name TryAffectGoodwillWith -IL` и `-Type Verse.PlayLogEntry_Interaction -Name .ctor -IL` подтвердили двустороннюю синхронизацию и поля записи. Ревью статическое, без запуска игры и сборки.


## Отчёт Codex: OutpostCaravanRecheck
### OutpostCaravanRecheck — НЕ ПРИНИМАТЬ

- **P1 · Серебро — частично, блокер.** `Source/OutpostCaravanLogistics.cs:101–143,515–526,875–885,928–1002,1547–1582`. Проверка доступного серебра и простой отказ склада исправлены. Разница инвентаря до/после формирования не идентифицирует груз: переложенное личное серебро считается грузом, если запас исходного владельца уменьшился. При отсутствии записи миссии действие вообще использует запрошенную сумму без baseline. Кроме того, SplitOff может изменить стек до попадания в список отката, а возврат в инвентарь не проверяет TryAdd. Привязать платёж к конкретному каравану и подтверждённому грузу, вести запись перевода до следующей мутации, сохранять неподнятые части в доступном holder.
- **P1 · Возврат и отмена — частично.** `Source/OutpostCaravanLogistics.cs:135–143,456–482,566–612,690–709`. При удалении аванпоста записывается Returning, кнопка остаётся при остановке, ручная отмена проверяет запуск маршрута. Но отказ склада оставляет миссию Outbound с остановленным маршрутом, не запуская возврат или повторную сдачу. Ваниль очищает arrivalAction перед вызовом Arrived. При отказе переводить миссию к явной повторной сдаче или к возврату с грузом. На домашней клетке проверять StillValid перед прямым Enter.Arrived и пробовать доступную альтернативную колонию при отказе.
- **P1 · Перехват побочного похода и очистка — штатный путь закрыт, переход со старого сейва частично.** `Source/OutpostCaravanLogistics.cs:292–321,492–505,615–685,1727–1760`. Новый караван привязан к ID карты и lord, отвязанные уничтоженные записи удаляются, поиск по любой пешке убран. Старый сейв с караваном и без state загружается как Forming+everAttached и удаляется аудитом. Его старое действие останется, но потеряет проверку груза. Мигрировать по реальному arrivalAction, а при неполном состоянии возвращать домой без списания. Для новых действий также требовать совпадение mission.caravan и цели, отсутствие миссии считать отказом.
- **P2 · Передача рабочих — частично.** `Source/StrategicOutpost.cs:1018–1137,1173–1206`, `Source/OutpostCaravanLogistics.cs:146–186`. Вместимость, кровать, спавн и последний мобильный носильщик закрыты на штатных путях. RemovePawn до try и непроверенные AddPawn/TryAdd при откате ещё оставляют окно для потери владельца. Серебро уже сдано до этих шагов, а delivered выставляется после них. Фиксировать оплату отдельно от перевода каждого рабочего, проверять фактического владельца и безопасное место возврата.
- **P2 · Еда — частично.** `Source/OutpostCaravanLogistics.cs:1017–1082,1327–1370,1434–1462`. Количество Transferable и оба плеча учитываются, часть еды перекладывается носильщикам. Фактическая проверка на развороте суммирует нутриенты без диет и порчи. Если отказано в переносе, обратный TryAdd не проверяется, нехватка остатка не влияет на отправку. Перепроверять съедобный резерв с реальным составом после отказов вместимости и сохранять владельца каждой порции.
- **P2 · Нагрузка — закрыта.** `Source/OutpostCaravanLogistics.cs:615–622,1134–1144`, `Source/Dialog_OutpostCaravan.cs:75–83,211–215`. Аудит раз в 250 тиков, расчёт в окне кешируется, WorldPath возвращается в пул в finally.
- **Новый тестовый хук.** `Source/OutpostCaravanLogistics.cs:765,953` использует публичный TestRejectRawMaterialsTransfer без ограничения тестовым режимом. Ограничить переключатель пробой или внедрять отказ в тестовую сборку.

Отчёт Antigravity указывает fresh 14/14 и reload 3/3, exit 0, runtimeErrors=0, unresolvedReferences=0, без CompatibilityReload, `TestRuns/OutpostCaravan-FullSet-20261120-060324`. `Get-FileHash`, exit 0: текущая DLL совпадает с отчётом, SHA256 `da47d673ca9f0036330546923cbdd99b70729bc1b5c097f25e36f585bc77a183`. `git status --short` по файлам ревью чист, интеграция dee9f09. Cecil (PatherArrived / StopDead, exit 0) подтвердил очистку старого действия до вызова доставки.

Проба нажимает рабочий gizmo и действительно проверяет расход еды и вход после reload (`Tests/InGameOutpostCaravanProbe.cs:377–467,477–489`). Но погрузка вручную, caravanSent и Tile подставляются (`:497–539`). Отказ склада моделируется до добавления первой части и прямым Arrived, когда ваниль ещё не очистила действие (`:194–207`). Сейв только на обратном плече, UI-файл безусловно пишет passed=true (`:724–725`). Не подтверждены штатная погрузка со склада, reload туда, отказ после перемещения первого стека и отсутствие личных потерь. Ревью статическое, без игры и сборки.


## Текущий код: Source/StoryRelationRestorer.cs
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Tracks a single active raid hostility lease OR hotel visit truce (neutrality) lease
    /// on a faction's relation override.
    /// </summary>
    public sealed class StoryHostilityLease : IExposable
    {
        public const string ModeHostile = "hostile";
        public const string ModeNeutral = "neutral";

        public string leaseId;
        public string quest;
        public string stageKey;
        public int runGen;
        public bool restoreRelations;
        public string mode = ModeHostile;

        public void ExposeData()
        {
            Scribe_Values.Look(ref leaseId, "leaseId");
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref restoreRelations, "restoreRelations", false);
            Scribe_Values.Look(ref mode, "mode", ModeHostile);
            if (mode.NullOrEmpty()) mode = ModeHostile;
        }
    }

    /// <summary>
    /// Stores original faction goodwill &amp; relation kind before concurrent story raids (Hostile)
    /// or hotel visits (Neutral truce) altered relations. Restoration runs ONLY when the final
    /// lease for that faction is released or orphaned (completion, failure, exception, disabled quest,
    /// removed Def/stage, or save migration).
    /// </summary>
    public sealed class StoryFactionBackup : IExposable
    {
        public int factionId = -1;
        public int origGoodwill;
        public int origKind = (int)FactionRelationKind.Neutral;
        public bool origHasGoodwill;
        public int postForceGoodwill = -100;
        public bool restoreWanted;
        public string activeMode = StoryHostilityLease.ModeHostile;
        public List<StoryHostilityLease> leases = new List<StoryHostilityLease>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionId, "factionId", -1);
            Scribe_Values.Look(ref origGoodwill, "origGoodwill", 0);
            Scribe_Values.Look(ref origKind, "origKind", (int)FactionRelationKind.Neutral);
            Scribe_Values.Look(ref origHasGoodwill, "origHasGoodwill", true);
            Scribe_Values.Look(ref postForceGoodwill, "postForceGoodwill", -100);
            Scribe_Values.Look(ref restoreWanted, "restoreWanted", false);
            Scribe_Values.Look(ref activeMode, "activeMode", StoryHostilityLease.ModeHostile);
            Scribe_Collections.Look(ref leases, "leases", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (leases == null) leases = new List<StoryHostilityLease>();
                if (activeMode.NullOrEmpty()) activeMode = StoryHostilityLease.ModeHostile;
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
    /// Tracks a registered hotel visit guest so Pawn.ExitMap can distinguish a genuine peaceful exit
    /// from disappearance, container insertion, imprisonment, sentAway, or map deletion across reload.
    /// </summary>
    public sealed class StoryTrackedGuestRecord : IExposable
    {
        public string runKey;
        public string quest;
        public string stageKey;
        public int runGen;
        public int mapId = -1;
        public int pawnIdNum = -1;
        public int visitLordId = -1;
        public int exitLordId = -1;
        public bool exitedPeacefully;
        public bool exitedHostileOrForced;

        public void ExposeData()
        {
            Scribe_Values.Look(ref runKey, "runKey");
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref runGen, "runGen", 0);
            Scribe_Values.Look(ref mapId, "mapId", -1);
            Scribe_Values.Look(ref pawnIdNum, "pawnIdNum", -1);
            Scribe_Values.Look(ref visitLordId, "visitLordId", -1);
            Scribe_Values.Look(ref exitLordId, "exitLordId", -1);
            Scribe_Values.Look(ref exitedPeacefully, "exitedPeacefully", false);
            Scribe_Values.Look(ref exitedHostileOrForced, "exitedHostileOrForced", false);
        }
    }

    /// <summary>
    /// WorldComponent that guarantees:
    /// 1) Temporary raid hostility AND temporary hotel-visit neutrality (truce) are rolled back properly
    ///    (handling RimWorld's goodwill/kind hysteresis thresholds in both directions, permanentEnemy
    ///    guards, concurrent raids/visits on the same faction, mid-stage goodwill deltas, exceptions,
    ///    disabled quests, and removed Defs/stages).
    /// 2) Retrieve quest items cannot merge into arbitrary stacks while active.
    /// 3) Hotel visit guest departures via Pawn.ExitMap are tracked accurately across save/reload.
    /// </summary>
    public sealed class StoryRelationRestorer : WorldComponent
    {
        private const int CheckInterval = 250;

        private List<StoryFactionBackup> backups = new List<StoryFactionBackup>();
        private List<StoryProtectedItem> protectedItems = new List<StoryProtectedItem>();
        private List<StoryTrackedGuestRecord> trackedGuests = new List<StoryTrackedGuestRecord>();

        private static readonly HashSet<string> ActiveProtectedIds = new HashSet<string>();
        private static readonly HashSet<int> ActiveTrackedGuestPawnIds = new HashSet<int>();

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
        // HOSTILITY LEASES (RAID API — PRESERVED FROM E2)
        // =====================================================================

        public bool AcquireHostilityLease(Faction fac, StoryQuestRun run, bool restoreRelations, out string leaseId)
        {
            leaseId = null;
            if (fac == null || fac.IsPlayer || run == null) return false;

            leaseId = MakeLeaseId(run);
            StoryFactionBackup existing = FindBackup(fac.loadID);

            if (existing == null && fac.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            // Refuse conflicting concurrent override (e.g., active hotel truce on the same faction)
            if (existing != null && existing.activeMode != StoryHostilityLease.ModeHostile)
            {
                leaseId = null;
                return false;
            }

            if (existing == null)
            {
                existing = new StoryFactionBackup();
                existing.factionId = fac.loadID;
                existing.origGoodwill = fac.PlayerGoodwill;
                existing.origKind = (int)fac.RelationKindWith(Faction.OfPlayer);
                existing.origHasGoodwill = fac.HasGoodwill;
                existing.restoreWanted = restoreRelations;
                existing.activeMode = StoryHostilityLease.ModeHostile;
                backups.Add(existing);

                AddOrUpdateLease(existing, leaseId, run, restoreRelations, StoryHostilityLease.ModeHostile);

                if (!ForceFactionHostile(fac))
                {
                    backups.Remove(existing);
                    RestoreFactionRelations(fac, existing);
                    leaseId = null;
                    return false;
                }

                existing.postForceGoodwill = fac.PlayerGoodwill;
                return true;
            }

            if (restoreRelations) existing.restoreWanted = true;
            AddOrUpdateLease(existing, leaseId, run, restoreRelations, StoryHostilityLease.ModeHostile);

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

        // =====================================================================
        // TRUCE / TEMPORARY NEUTRALITY LEASES (HOTEL VISIT API — E3)
        // =====================================================================

        /// <summary>
        /// Checks whether a hostile faction can legally enter temporary neutrality without triggering
        /// vanilla permanentEnemy or goodwill-faction SetRelationDirect errors.
        /// </summary>
        public static bool CanFactionEnterTruce(Faction fac)
        {
            if (fac == null || fac.IsPlayer || fac.defeated) return false;
            if (!fac.HostileTo(Faction.OfPlayer)) return true;
            if (fac.def != null && fac.def.permanentEnemy) return false;

            if (fac.HasGoodwill)
            {
                int needed = 0 - fac.PlayerGoodwill;
                if (needed > 0 && !fac.CanChangeGoodwillFor(Faction.OfPlayer, needed)) // CHECK API
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Records original goodwill &amp; relation kind BEFORE mutating, moves a hostile faction to
        /// temporary Neutrality (verifying !HostileTo(OfPlayer)), and supports overlapping visits
        /// on the same faction (only the final lease restores hostility).
        /// </summary>
        public bool AcquireTruceLease(Faction fac, StoryQuestRun run, out string leaseId)
        {
            leaseId = null;
            if (fac == null || fac.IsPlayer || fac.defeated || run == null) return false;

            if (!CanFactionEnterTruce(fac))
            {
                return false;
            }

            StoryFactionBackup existing = FindBackup(fac.loadID);

            if (existing == null && !fac.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            // Refuse conflicting concurrent override (e.g., active colony raid lease on same faction)
            if (existing != null && existing.activeMode != StoryHostilityLease.ModeNeutral)
            {
                return false;
            }

            leaseId = MakeLeaseId(run);

            if (existing == null)
            {
                existing = new StoryFactionBackup();
                existing.factionId = fac.loadID;
                existing.origGoodwill = fac.PlayerGoodwill;
                existing.origKind = (int)fac.RelationKindWith(Faction.OfPlayer);
                existing.origHasGoodwill = fac.HasGoodwill;
                existing.restoreWanted = true;
                existing.activeMode = StoryHostilityLease.ModeNeutral;
                backups.Add(existing);

                AddOrUpdateLease(existing, leaseId, run, true, StoryHostilityLease.ModeNeutral);

                if (!ForceFactionNeutral(fac))
                {
                    backups.Remove(existing);
                    RestoreFactionRelations(fac, existing);
                    leaseId = null;
                    return false;
                }

                existing.postForceGoodwill = fac.PlayerGoodwill;
                return true;
            }

            existing.restoreWanted = true;
            AddOrUpdateLease(existing, leaseId, run, true, StoryHostilityLease.ModeNeutral);

            if (fac.HostileTo(Faction.OfPlayer))
            {
                if (!ForceFactionNeutral(fac))
                {
                    RemoveLease(existing, leaseId);
                    leaseId = null;
                    return false;
                }
            }
            return true;
        }

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
                        if (!IsLeaseStillActive(story, lease))
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

            if (trackedGuests != null)
            {
                bool changed = false;
                for (int i = trackedGuests.Count - 1; i >= 0; i--)
                {
                    StoryTrackedGuestRecord rec = trackedGuests[i];
                    if (!IsGuestRecordStillActive(story, rec))
                    {
                        trackedGuests.RemoveAt(i);
                        changed = true;
                    }
                }
                if (changed) RebuildTrackedGuestCache();
            }
        }

        private static bool IsLeaseStillActive(SparkStory story, StoryHostilityLease lease)
        {
            if (story == null || lease == null || lease.quest.NullOrEmpty()) return false;
            StoryQuestRun run = story.RunFor(lease.quest);
            if (run == null || run.status != StoryQuestStatus.Active) return false;
            if (run.gen != lease.runGen) return false;
            if (!lease.stageKey.NullOrEmpty() && run.stageKey != lease.stageKey) return false;

            StoryQuestDef def = run.Def;
            if (def == null || run.stage < 0 || run.stage >= def.stages.Count) return false;

            if (lease.mode == StoryHostilityLease.ModeNeutral)
            {
                return def.stages[run.stage] is StoryStage_HotelVisit;
            }
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

        private static bool IsGuestRecordStillActive(SparkStory story, StoryTrackedGuestRecord rec)
        {
            if (story == null || rec == null || rec.quest.NullOrEmpty()) return false;
            StoryQuestRun run = story.RunFor(rec.quest);
            if (run == null || run.status != StoryQuestStatus.Active) return false;
            if (run.gen != rec.runGen) return false;
            if (!rec.stageKey.NullOrEmpty() && run.stageKey != rec.stageKey) return false;

            StoryQuestDef def = run.Def;
            if (def == null || run.stage < 0 || run.stage >= def.stages.Count) return false;
            return def.stages[run.stage] is StoryStage_HotelVisit;
        }

        private void FinalizeBackup(StoryFactionBackup b)
        {
            if (b == null || !b.restoreWanted) return;
            Faction fac = FindFactionById(b.factionId);
            if (fac == null || fac.defeated) return;
            RestoreFactionRelations(fac, b);
        }

        private static bool ForceFactionHostile(Faction fac)
        {
            if (fac == null || fac.IsPlayer) return false;
            if (fac.HostileTo(Faction.OfPlayer)) return true;

            if (fac.HasGoodwill)
            {
                int drop = -100 - fac.PlayerGoodwill;
                if (drop < 0 && fac.CanChangeGoodwillFor(Faction.OfPlayer, drop)) // CHECK API
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null); // CHECK API
                }
            }
            else
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }

            return fac.HostileTo(Faction.OfPlayer);
        }

        private static bool ForceFactionNeutral(Faction fac)
        {
            if (fac == null || fac.IsPlayer || fac.defeated) return false;
            if (!fac.HostileTo(Faction.OfPlayer)) return true;
            if (fac.def != null && fac.def.permanentEnemy) return false;

            if (fac.HasGoodwill)
            {
                int lift = 0 - fac.PlayerGoodwill;
                if (lift > 0 && fac.CanChangeGoodwillFor(Faction.OfPlayer, lift)) // CHECK API
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, lift, false, false, null, null); // CHECK API
                }
            }
            else
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }

            return !fac.HostileTo(Faction.OfPlayer);
        }

        private static void RestoreFactionRelations(Faction fac, StoryFactionBackup b)
        {
            if (fac == null || b == null) return;

            FactionRelationKind origKind = (FactionRelationKind)b.origKind;

            int externalDelta = fac.HasGoodwill ? (fac.PlayerGoodwill - b.postForceGoodwill) : 0;
            int targetGoodwill = Mathf.Clamp(b.origGoodwill + externalDelta, -100, 100);

            if (origKind == FactionRelationKind.Neutral && targetGoodwill < -74)
            {
                targetGoodwill = -74;
            }
            else if (origKind == FactionRelationKind.Ally && targetGoodwill < 0)
            {
                targetGoodwill = 0;
            }
            else if (origKind == FactionRelationKind.Hostile && targetGoodwill > -1)
            {
                targetGoodwill = -1;
            }

            if (fac.HasGoodwill)
            {
                // Direction A: Hostile -> Neutral/Ally: cross >= 0 (or >= 75) first
                if (fac.HostileTo(Faction.OfPlayer) && origKind != FactionRelationKind.Hostile)
                {
                    int liftTarget = origKind == FactionRelationKind.Ally ? Mathf.Max(75, targetGoodwill) : Mathf.Max(0, targetGoodwill);
                    int liftDelta = liftTarget - fac.PlayerGoodwill;
                    if (liftDelta > 0)
                    {
                        fac.TryAffectGoodwillWith(Faction.OfPlayer, liftDelta, false, false, null, null); // CHECK API
                    }
                }
                // Direction B: Neutral/Ally -> Hostile (ending truce): cross <= -75 first
                else if (!fac.HostileTo(Faction.OfPlayer) && origKind == FactionRelationKind.Hostile)
                {
                    int dropTarget = Mathf.Min(-75, targetGoodwill);
                    int dropDelta = dropTarget - fac.PlayerGoodwill;
                    if (dropDelta < 0)
                    {
                        fac.TryAffectGoodwillWith(Faction.OfPlayer, dropDelta, false, false, null, null); // CHECK API
                    }
                }

                int finalDelta = targetGoodwill - fac.PlayerGoodwill;
                if (finalDelta != 0)
                {
                    fac.TryAffectGoodwillWith(Faction.OfPlayer, finalDelta, false, false, null, null); // CHECK API
                }

                // Direct FactionRelation fallback (never calls SetRelationDirect on goodwill factions)
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

        private static void AddOrUpdateLease(StoryFactionBackup b, string leaseId, StoryQuestRun run, bool restoreRelations, string mode)
        {
            if (b.leases == null) b.leases = new List<StoryHostilityLease>();
            for (int i = 0; i < b.leases.Count; i++)
            {
                if (b.leases[i] != null && b.leases[i].leaseId == leaseId)
                {
                    b.leases[i].restoreRelations = restoreRelations;
                    b.leases[i].mode = mode;
                    return;
                }
            }
            StoryHostilityLease lease = new StoryHostilityLease();
            lease.leaseId = leaseId;
            lease.quest = run.quest;
            lease.stageKey = run.stageKey;
            lease.runGen = run.gen;
            lease.restoreRelations = restoreRelations;
            lease.mode = mode;
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
        // TRACKED HOTEL VISIT GUESTS & REAL EXITMAP VERIFICATION
        // =====================================================================

        public void RegisterHotelGuests(StoryQuestRun run, int mapId, int visitLordId, List<int> pawnIds)
        {
            if (run == null || pawnIds == null) return;
            if (trackedGuests == null) trackedGuests = new List<StoryTrackedGuestRecord>();

            string runKey = MakeLeaseId(run);
            ClearHotelGuests(runKey);

            for (int i = 0; i < pawnIds.Count; i++)
            {
                StoryTrackedGuestRecord rec = new StoryTrackedGuestRecord();
                rec.runKey = runKey;
                rec.quest = run.quest;
                rec.stageKey = run.stageKey;
                rec.runGen = run.gen;
                rec.mapId = mapId;
                rec.pawnIdNum = pawnIds[i];
                rec.visitLordId = visitLordId;
                rec.exitLordId = -1;
                trackedGuests.Add(rec);
                ActiveTrackedGuestPawnIds.Add(pawnIds[i]);
            }
        }

        public void SetHotelExitLordId(string runKey, int exitLordId)
        {
            if (runKey.NullOrEmpty() || trackedGuests == null) return;
            for (int i = 0; i < trackedGuests.Count; i++)
            {
                if (trackedGuests[i] != null && trackedGuests[i].runKey == runKey)
                {
                    trackedGuests[i].exitLordId = exitLordId;
                }
            }
        }

        public void ClearHotelGuests(string runKey)
        {
            if (runKey.NullOrEmpty() || trackedGuests == null) return;
            bool removed = false;
            for (int i = trackedGuests.Count - 1; i >= 0; i--)
            {
                if (trackedGuests[i] != null && trackedGuests[i].runKey == runKey)
                {
                    trackedGuests.RemoveAt(i);
                    removed = true;
                }
            }
            if (removed) RebuildTrackedGuestCache();
        }

        public StoryTrackedGuestRecord FindTrackedGuest(string runKey, int pawnIdNum)
        {
            if (runKey.NullOrEmpty() || trackedGuests == null) return null;
            for (int i = 0; i < trackedGuests.Count; i++)
            {
                StoryTrackedGuestRecord r = trackedGuests[i];
                if (r != null && r.runKey == runKey && r.pawnIdNum == pawnIdNum) return r;
            }
            return null;
        }

        public static bool IsTrackedGuestPawn(int pawnIdNum)
        {
            return ActiveTrackedGuestPawnIds.Count > 0 && ActiveTrackedGuestPawnIds.Contains(pawnIdNum);
        }

        internal void NotifyGuestExitingMap(Pawn pawn, Map map)
        {
            if (pawn == null || map == null || trackedGuests == null) return;

            Lord curLord = pawn.GetLord();
            int curLordId = curLord != null ? curLord.loadID : -1;
            bool badState = pawn.Dead || pawn.Destroyed || pawn.Downed ||
                            pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony ||
                            pawn.HostileTo(Faction.OfPlayer) || pawn.InMentalState ||
                            StoryStage_HotelVisit.IsSentAwayByHospitality(pawn);

            for (int i = 0; i < trackedGuests.Count; i++)
            {
                StoryTrackedGuestRecord rec = trackedGuests[i];
                if (rec == null || rec.pawnIdNum != pawn.thingIDNumber || rec.mapId != map.uniqueID) continue;

                bool lordMatches = (curLordId >= 0 && (curLordId == rec.visitLordId || curLordId == rec.exitLordId));
                if (!badState && lordMatches)
                {
                    rec.exitedPeacefully = true;
                }
                else
                {
                    rec.exitedHostileOrForced = true;
                }
            }
        }

        private void RebuildTrackedGuestCache()
        {
            ActiveTrackedGuestPawnIds.Clear();
            if (trackedGuests == null) return;
            for (int i = 0; i < trackedGuests.Count; i++)
            {
                if (trackedGuests[i] != null && trackedGuests[i].pawnIdNum >= 0)
                {
                    ActiveTrackedGuestPawnIds.Add(trackedGuests[i].pawnIdNum);
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
            Scribe_Collections.Look(ref trackedGuests, "TMM_trackedHotelGuests", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (backups == null) backups = new List<StoryFactionBackup>();
                if (protectedItems == null) protectedItems = new List<StoryProtectedItem>();
                if (trackedGuests == null) trackedGuests = new List<StoryTrackedGuestRecord>();
                RebuildProtectedCache();
                RebuildTrackedGuestCache();
            }
        }
    }

    // =========================================================================
    // HARMONY PATCHES: ANTI-STACK-MERGE & REAL HOTEL GUEST EXITMAP TRACKING
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

            MethodInfo exitMap = AccessTools.Method(typeof(Pawn), "ExitMap", new Type[] { typeof(bool), typeof(Rot4) }); // CHECK API
            if (exitMap != null)
            {
                harmony.Patch(exitMap, prefix: new HarmonyMethod(typeof(StoryRetrieveStackPatch), "Prefix_Pawn_ExitMap"));
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

        public static void Prefix_Pawn_ExitMap(Pawn __instance)
        {
            if (__instance == null || !__instance.Spawned || __instance.Map == null) return;
            if (!StoryRelationRestorer.IsTrackedGuestPawn(__instance.thingIDNumber)) return;

            StoryRelationRestorer inst = StoryRelationRestorer.Instance;
            if (inst != null)
            {
                inst.NotifyGuestExitingMap(__instance, __instance.Map);
            }
        }
    }
}
```

## Текущий код: Source/StoryPayment.cs
```csharp
using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Shared atomic resource counting and deduction helper for StoryStage_Deliver and OutpostPodLogistics.
    ///
    /// EXPLICIT NESTED-ITEMS &amp; ELIGIBILITY POLICY:
    /// 1) Colony Map mode:
    ///    - ONLY counts/deducts items directly spawned on the pinned Map, unforbidden, and located inside
    ///      a valid player storage zone (Zone_Stockpile) or player-owned storage building (Building_Storage).
    ///    - Loose items outside storage, items inside pawns' inventories, drop pods, graves, or other nested
    ///      IThingHolders are EXCLUDED.
    /// 2) Caravan mode:
    ///    - ONLY counts/deducts items directly held in inventory.innerContainer of living pawns belonging
    ///      to that specific player Caravan.
    ///    - Equipped weapons, worn apparel, and items nested inside other items are EXCLUDED.
    ///
    /// ATOMICITY GUARANTEE:
    /// All required stacks are collected and summed first, then each requested amount is split into a
    /// temporary IThingHolder with strict verification of every SplitOff result. If ANY split returns null,
    /// returns the wrong count, fails TryAdd, or throws an exception, EVERY previously separated chunk is
    /// rolled back to its original stack/cell (on map) or original pawn inventory (in caravan), leaving
    /// inventory completely unchanged. Only after the full amount is confirmed in the staging holder
    /// are the staged chunks destroyed.
    /// </summary>
    public static class StoryPayment
    {
        /// <summary>
        /// Test-only hook allowing in-game probes to simulate a mid-transaction SplitOff failure
        /// (e.g. returning null or throwing on the second stack). Always null in normal play.
        /// Parameters: (sourceStack, requestedCount, zeroBasedStepIndex) -> split Thing.
        /// </summary>
        public static Func<Thing, int, int, Thing> TestSplitOffOverride;

        private sealed class StagingHolder : IThingHolder
        {
            public ThingOwner<Thing> innerContainer;

            public StagingHolder()
            {
                innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
            }

            public IThingHolder ParentHolder
            {
                get { return null; }
            }

            public void GetChildHolders(List<IThingHolder> outChildren)
            {
            }

            public ThingOwner GetDirectlyHeldThings()
            {
                return innerContainer;
            }
        }

        private sealed class ReservedChunk
        {
            public Thing originalStack;
            public Thing stagedChunk;
            public Map origMap;
            public IntVec3 origPos;
            public Pawn origPawn;
            public bool tookWhole;
        }

        public sealed class ResourceRequirement
        {
            public ThingDef def;
            public int count;

            public ResourceRequirement(ThingDef def, int count)
            {
                this.def = def;
                this.count = count;
            }
        }

        // =====================================================================
        // COLONY STORAGE COUNTING & ATOMIC DEDUCTION
        // =====================================================================

        /// <summary>
        /// Returns true iff the thing is directly spawned on map, unforbidden, and sits inside a
        /// player-owned stockpile zone or player-owned storage building.
        /// </summary>
        public static bool IsInPlayerStorage(Map map, Thing t)
        {
            if (map == null || t == null || t.Destroyed || !t.Spawned || t.Map != map) return false;
            if (t.stackCount <= 0 || t.IsForbidden(Faction.OfPlayer)) return false;
            if (map.haulDestinationManager == null) return false;

            SlotGroup sg = map.haulDestinationManager.SlotGroupAt(t.Position); // CHECK API
            if (sg == null || sg.parent == null) return false;

            if (sg.parent is Zone_Stockpile) return true;

            Building b = sg.parent as Building;
            if (b != null && b.Faction == Faction.OfPlayer) return true;

            return false;
        }

        public static int CountInColonyStockpiles(Map map, ThingDef def)
        {
            if (map == null || def == null || map.listerThings == null) return 0;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (IsInPlayerStorage(map, t))
                {
                    total += t.stackCount;
                }
            }
            return total;
        }

        public static bool TryDeductFromColonyStockpiles(Map map, ThingDef def, int requiredCount, out int deducted)
        {
            deducted = 0;
            if (map == null || def == null || requiredCount <= 0) return false;

            List<ResourceRequirement> reqs = new List<ResourceRequirement>();
            reqs.Add(new ResourceRequirement(def, requiredCount));
            if (TryDeductMultipleFromColonyStockpiles(map, reqs))
            {
                deducted = requiredCount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Atomically deducts multiple resource requirements from player storage on a single pinned Map.
        /// If any requirement lacks stock or any SplitOff fails, EVERY earlier chunk is rolled back.
        /// </summary>
        public static bool TryDeductMultipleFromColonyStockpiles(Map map, List<ResourceRequirement> requirements)
        {
            if (map == null || map.listerThings == null || requirements == null || requirements.Count == 0) return false;

            // Step 1: Collect eligible stacks and verify complete sums BEFORE mutating anything
            List<List<Thing>> plans = new List<List<Thing>>();
            for (int r = 0; r < requirements.Count; r++)
            {
                ResourceRequirement req = requirements[r];
                if (req == null || req.def == null || req.count <= 0)
                {
                    plans.Add(new List<Thing>());
                    continue;
                }

                List<Thing> eligible = new List<Thing>();
                List<Thing> raw = map.listerThings.ThingsOfDef(req.def);
                int sum = 0;
                for (int i = 0; i < raw.Count; i++)
                {
                    Thing t = raw[i];
                    if (IsInPlayerStorage(map, t))
                    {
                        eligible.Add(t);
                        sum += t.stackCount;
                    }
                }

                if (sum < req.count) return false;
                plans.Add(eligible);
            }

            // Step 2: Stage all requested chunks in a temporary holder, checking EVERY SplitOff
            StagingHolder staging = new StagingHolder();
            List<ReservedChunk> reserved = new List<ReservedChunk>();
            int stepIndex = 0;
            bool ok = true;

            try
            {
                for (int r = 0; r < requirements.Count && ok; r++)
                {
                    ResourceRequirement req = requirements[r];
                    if (req == null || req.def == null || req.count <= 0) continue;

                    int remaining = req.count;
                    List<Thing> stacks = plans[r];

                    for (int i = 0; i < stacks.Count && remaining > 0; i++)
                    {
                        Thing src = stacks[i];
                        if (!IsInPlayerStorage(map, src))
                        {
                            ok = false;
                            break;
                        }

                        int take = Mathf.Min(remaining, src.stackCount);
                        bool takingWhole = (take == src.stackCount);
                        IntVec3 pos = src.Position;

                        Thing split = ExecuteSplitOff(src, take, stepIndex);
                        stepIndex++;

                        if (split == null || split.Destroyed || split.stackCount != take)
                        {
                            // Put back partial unexpected split if any
                            if (split != null && !split.Destroyed && split != src)
                            {
                                ReturnChunkToMap(split, src, map, pos);
                            }
                            ok = false;
                            break;
                        }

                        if (!staging.innerContainer.TryAdd(split, false))
                        {
                            ReturnChunkToMap(split, takingWhole ? null : src, map, pos);
                            ok = false;
                            break;
                        }

                        ReservedChunk rc = new ReservedChunk();
                        rc.originalStack = takingWhole ? null : src;
                        rc.stagedChunk = split;
                        rc.origMap = map;
                        rc.origPos = pos;
                        rc.tookWhole = takingWhole;
                        reserved.Add(rc);

                        remaining -= split.stackCount;
                    }

                    if (remaining > 0) ok = false;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryPayment: colony deduction failed mid-transaction, rolling back: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                RollbackMapChunks(staging, reserved);
                return false;
            }

            // Step 3: Commit — destroy staged chunks only after 100% reservation succeeded
            DestroyStaged(staging);
            return true;
        }

        // =====================================================================
        // CARAVAN DIRECT-INVENTORY COUNTING & ATOMIC DEDUCTION
        // =====================================================================

        /// <summary>
        /// Counts items matching def directly inside living caravan pawns' inventory.innerContainer.
        /// Equipped weapons, worn apparel, and nested items are excluded.
        /// </summary>
        public static int CountInCaravanDirectInventory(Caravan caravan, ThingDef def)
        {
            if (caravan == null || caravan.Destroyed || def == null) return 0;
            List<Pawn> pawns = caravan.PawnsListForReading;
            int total = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed && t.def == def && t.stackCount > 0)
                    {
                        total += t.stackCount;
                    }
                }
            }
            return total;
        }

        /// <summary>
        /// Atomically deducts requiredCount of def from the direct inventories of living pawns
        /// belonging to a specific player Caravan. Rolls back every chunk on any failure.
        /// </summary>
        public static bool TryDeductFromCaravan(Caravan caravan, ThingDef def, int requiredCount, out int deducted)
        {
            deducted = 0;
            if (caravan == null || caravan.Destroyed || caravan.Faction != Faction.OfPlayer || def == null || requiredCount <= 0)
            {
                return false;
            }

            // Step 1: Collect (pawn, stack) pairs and verify full sum first
            List<Pawn> owners = new List<Pawn>();
            List<Thing> stacks = new List<Thing>();
            int total = 0;

            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.inventory == null || p.inventory.innerContainer == null) continue;
                ThingOwner<Thing> inv = p.inventory.innerContainer;
                for (int j = 0; j < inv.Count; j++)
                {
                    Thing t = inv[j];
                    if (t != null && !t.Destroyed && t.def == def && t.stackCount > 0)
                    {
                        owners.Add(p);
                        stacks.Add(t);
                        total += t.stackCount;
                    }
                }
            }

            if (total < requiredCount) return false;

            // Step 2: Stage chunks with strict SplitOff validation
            StagingHolder staging = new StagingHolder();
            List<ReservedChunk> reserved = new List<ReservedChunk>();
            int remaining = requiredCount;
            int stepIndex = 0;
            bool ok = true;

            try
            {
                for (int i = 0; i < stacks.Count && remaining > 0; i++)
                {
                    Pawn p = owners[i];
                    Thing src = stacks[i];
                    if (p == null || p.Dead || p.inventory == null || p.inventory.innerContainer == null ||
                        src == null || src.Destroyed || src.stackCount <= 0 || !p.inventory.innerContainer.Contains(src))
                    {
                        ok = false;
                        break;
                    }

                    int take = Mathf.Min(remaining, src.stackCount);
                    bool takingWhole = (take == src.stackCount);

                    Thing split = ExecuteSplitOff(src, take, stepIndex);
                    stepIndex++;

                    if (split == null || split.Destroyed || split.stackCount != take)
                    {
                        if (split != null && !split.Destroyed && split != src)
                        {
                            ReturnChunkToPawn(split, src, p);
                        }
                        ok = false;
                        break;
                    }

                    // Ensure split is removed from pawn inventory before adding to staging
                    if (split.holdingOwner != null && split.holdingOwner != staging.innerContainer)
                    {
                        split.holdingOwner.Remove(split);
                    }

                    if (!staging.innerContainer.TryAdd(split, false))
                    {
                        ReturnChunkToPawn(split, takingWhole ? null : src, p);
                        ok = false;
                        break;
                    }

                    ReservedChunk rc = new ReservedChunk();
                    rc.originalStack = takingWhole ? null : src;
                    rc.stagedChunk = split;
                    rc.origPawn = p;
                    rc.tookWhole = takingWhole;
                    reserved.Add(rc);

                    remaining -= split.stackCount;
                }

                if (remaining > 0) ok = false;
            }
            catch (Exception ex)
            {
                Log.Warning("[TMM] StoryPayment: caravan deduction failed mid-transaction, rolling back: " + ex.Message);
                ok = false;
            }

            if (!ok)
            {
                RollbackCaravanChunks(staging, reserved);
                return false;
            }

            // Step 3: Commit
            DestroyStaged(staging);
            deducted = requiredCount;
            return true;
        }

        // =====================================================================
        // INTERNAL SPLIT & ROLLBACK HELPERS
        // =====================================================================

        private static Thing ExecuteSplitOff(Thing src, int count, int stepIndex)
        {
            if (TestSplitOffOverride != null)
            {
                return TestSplitOffOverride(src, count, stepIndex);
            }
            return src.SplitOff(count);
        }

        private static void RollbackMapChunks(StagingHolder staging, List<ReservedChunk> reserved)
        {
            for (int i = reserved.Count - 1; i >= 0; i--)
            {
                ReservedChunk rc = reserved[i];
                if (rc == null || rc.stagedChunk == null || rc.stagedChunk.Destroyed) continue;
                if (staging.innerContainer.Contains(rc.stagedChunk))
                {
                    staging.innerContainer.Remove(rc.stagedChunk);
                }
                ReturnChunkToMap(rc.stagedChunk, rc.originalStack, rc.origMap, rc.origPos);
            }
            staging.innerContainer.Clear();
        }

        private static void ReturnChunkToMap(Thing chunk, Thing originalStack, Map map, IntVec3 pos)
        {
            if (chunk == null || chunk.Destroyed) return;
            if (originalStack != null && !originalStack.Destroyed && originalStack.Spawned && originalStack.Map == map)
            {
                if (originalStack.TryAbsorbStack(chunk, false))
                {
                    return;
                }
            }
            if (map != null && pos.InBounds(map))
            {
                GenPlace.TryPlaceThing(chunk, pos, map, ThingPlaceMode.Near);
            }
        }

        private static void RollbackCaravanChunks(StagingHolder staging, List<ReservedChunk> reserved)
        {
            for (int i = reserved.Count - 1; i >= 0; i--)
            {
                ReservedChunk rc = reserved[i];
                if (rc == null || rc.stagedChunk == null || rc.stagedChunk.Destroyed) continue;
                if (staging.innerContainer.Contains(rc.stagedChunk))
                {
                    staging.innerContainer.Remove(rc.stagedChunk);
                }
                ReturnChunkToPawn(rc.stagedChunk, rc.originalStack, rc.origPawn);
            }
            staging.innerContainer.Clear();
        }

        private static void ReturnChunkToPawn(Thing chunk, Thing originalStack, Pawn pawn)
        {
            if (chunk == null || chunk.Destroyed) return;
            if (originalStack != null && !originalStack.Destroyed)
            {
                if (originalStack.TryAbsorbStack(chunk, false))
                {
                    return;
                }
            }
            if (pawn != null && pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                pawn.inventory.innerContainer.TryAdd(chunk, true);
            }
        }

        private static void DestroyStaged(StagingHolder staging)
        {
            if (staging == null || staging.innerContainer == null) return;
            staging.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
        }
    }
}
```

## Текущий код: Source/OutpostPodLogistics.cs
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

        /// <summary>
        /// Counts unforbidden resources in colony stockpiles, falling back to all unforbidden on map.
        /// </summary>
        public static int CountResource(Map map, ThingDef def)
        {
            if (map == null || def == null) return 0;
            int count = StoryPayment.CountInColonyStockpiles(map, def);
            if (count > 0) return count;

            if (map.listerThings == null) return 0;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t != null && !t.Destroyed && !t.IsForbidden(Faction.OfPlayer))
                {
                    total += t.stackCount;
                }
            }
            return total;
        }

        /// <summary>
        /// Atomically deducts from colony stockpiles, falling back to map items if not in storage.
        /// </summary>
        public static int DeductResource(Map map, ThingDef def, int count)
        {
            if (map == null || def == null || count <= 0) return 0;
            int deducted;
            if (StoryPayment.TryDeductFromColonyStockpiles(map, def, count, out deducted))
            {
                return deducted;
            }
            if (map.listerThings == null) return 0;
            int remaining = count;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
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

            List<StoryPayment.ResourceRequirement> reqs = new List<StoryPayment.ResourceRequirement>();
            if (fuelDeduct > 0)
            {
                reqs.Add(new StoryPayment.ResourceRequirement(ThingDefOf.Chemfuel, fuelDeduct));
            }
            if (silverDeduct > 0)
            {
                reqs.Add(new StoryPayment.ResourceRequirement(ThingDefOf.Silver, silverDeduct));
            }

            if (reqs.Count > 0 && !StoryPayment.TryDeductMultipleFromColonyStockpiles(homeMap, reqs))
            {
                if (CountResource(homeMap, ThingDefOf.Chemfuel) >= fuelDeduct &&
                    CountResource(homeMap, ThingDefOf.Silver) >= silverDeduct)
                {
                    if (fuelDeduct > 0) DeductResource(homeMap, ThingDefOf.Chemfuel, fuelDeduct);
                    if (silverDeduct > 0) DeductResource(homeMap, ThingDefOf.Silver, silverDeduct);
                }
                else
                {
                    failReason = summary;
                    return false;
                }
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
            StrategicOutpost outpost = StrategicOutpostCaravanUtility.GetOutpostAtTile(destinationTile);
            if (outpost != null && outpost.Spawned) return true;
            return OutpostPodLogistics.GetHomeMap() != null;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            StrategicOutpost outpost = StrategicOutpostCaravanUtility.GetOutpostAtTile(tile);
            if (outpost == null || !outpost.Spawned)
            {
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

