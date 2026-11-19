**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

Это задача по коду, а не по тексту. В моде есть сюжетный движок (C#, RimWorld 1.7, компилятор C# 5). Нужно написать два новых этапа квеста: «место на карте мира» и «рейд на колонию». Твой код соберёт и проверит в игре наш агент. Пиши как можно точнее с первого раза.

## Жёсткие правила
- **Только C# 5.** Нельзя: `?.`, `=>` у членов класса, `nameof`, `is X x`, интерполяцию `$""""`, `out var`. Лямбды в аргументах можно.
- API — RimWorld **1.7** (Verse, RimWorld, RimWorld.Planet). Если не уверен в сигнатуре метода 1.7, пометь строку комментарием `// CHECK API` — агент сверит её Cecil'ом.
- Ядро не меняй. Используй только то, что показано ниже. Данные этапа храни **только** в `ctx.run.vars` (Dictionary<string,string>): он сохраняется в сейв и очищается при входе в этап.
- Пространство имён `TurboMegaMod`.

## Ядро (как есть)
```csharp
    /// Per-quest runtime context passed to stages and effects.
    public sealed class StoryContext
    {
        public SparkStory state;
        public StoryQuestRun run;
        public StoryQuestDef def;
        public Faction Faction { get { return StoryUtil.FactionOf(def.faction); } }
        public Map Home { get { return Find.AnyPlayerHomeMap; } }
    }

    // ---------------------------------------------------------------- stages

    public abstract class StoryStage
    {
        public string key;                  // optional stable id; saves follow it if stages are reordered
        [MustTranslate] public string label;
        [MustTranslate] public string text;

        public string Key(int index) { return key.NullOrEmpty() ? "#" + index : key; }

        /// Short mechanic id used by the director to avoid two identical mechanics in a row.
        public abstract string Mechanic { get; }

        /// Called once when the stage becomes current.
        public virtual void Start(StoryContext ctx) { }

        /// Called every director tick while current; return an outcome id to finish the stage, or null.
        public virtual string Tick(StoryContext ctx) { return null; }

        /// Next stage index for the outcome; -2 means "default" (next stage).
        public virtual int NextFor(string outcome) { return -2; }

        /// Effects to apply for the outcome.
        public virtual IEnumerable<StoryEffect> EffectsFor(string outcome) { yield break; }

        /// Text shown in the Chronicle for the current objective.
        public virtual string Objective(StoryContext ctx) { return StoryUtil.Format(label.NullOrEmpty() ? text : label, ctx); }
    }

    public sealed class StoryChoice
    {
        public string outcome;
        [MustTranslate] public string label;
        [MustTranslate] public string resultText;  // giver's reply, shown as a follow-up letter
        public int nextStage = -2;
        public List<string> requires = new List<string>();
        public List<StoryEffect> effects = new List<StoryEffect>();
    }


```
Поля прогона квеста:
```csharp
    public sealed class StoryQuestRun : IExposable
    {
        public string quest;
        public StoryQuestStatus status;
        public int stage = -1;
        public string stageKey;          // stable key of the current stage (StoryStage.key or its index); survives reordered XML
        public int gen;                  // generation: bumped on every offer and stage entry; letters carry it to reject stale clicks
        public string outcome;
        public string chosenOutcome;     // set by the choice letter, consumed by StoryStage_Choice.Tick
        public bool pendingChoice;
        public bool needsLetter;         // a lost offer/choice letter must be re-sent once by the director
        public int stageUntilTick = -1;
        public int offeredTick = -1;
        public int cooldownUntilTick = -1;
        public int finishedTick = -1;
        public List<string> history = new List<string>(); // "stage:outcome" for the Chronicle log
        public Dictionary<string, string> vars = new Dictionary<string, string>(); // per-stage worker data (E2/E3)


```
Ещё доступно:
- `SparkStory.Instance.TestForceOutcome(run, run.gen, outcome)` — тестовый хук, работает только с аргументом запуска `-tmm-story-test`;
- `HeroRegistry.Instance.GetOrCreate(HeroDef)` — возвращает живую пешку героя из реестра (может быть null);
- `StoryUtil.FactionOf(string factionDefName)`, `StoryUtil.Format(text, ctx)`.

Для исходов используй класс `StoryChoice` (поля `outcome`, `nextStage`, `effects`). Переопредели `NextFor` и `EffectsFor` так же, как это сделано в `StoryStage_Choice`.

## Что сделать
## 1. `StoryStage_Site : StoryStage` (Mechanic = "site")
Этап создаёт место на карте мира и ждёт, пока цель будет выполнена. Поля XML:
- `sitePart` (SitePartDef): ванильная часть места. Подбери подходящие ванильные defName 1.7 Cecil'ом или по `Data/*/Defs`, например лагерь врагов или тайник. По умолчанию — враждебный лагерь;
- `enemyFaction` (FactionDef defName): кто охраняет. Пусто — враждебная фракция по выбору игры;
- `minDays` / `maxDays` (float, по умолчанию 2 / 6): расстояние от колонии в днях пути. Клетку ищи ванильным `TileFinder`;
- `objective`: `Clear` (на карте не осталось живых врагов), `Rescue` (вывести пленника-героя `hero` с карты в караван игрока), `Retrieve` (вывезти вещь `thing` в караване), `Hold` (держать своих пешек на карте `holdHours` часов), `KillHero` (герой `hero` мёртв);
- `hero` (HeroDef, для `Rescue` и `KillHero`): берётся из реестра героев (`HeroRegistry.GetOrCreate`), а не генерируется заново. Для `KillHero` он стоит среди врагов, для `Rescue` — пленником на карте;
- `thing` (ThingDef, для `Retrieve`);
- `timeoutDays` (по умолчанию 15): после этого исход `timeout`, место удаляется;
- исходы: `success`, `timeout`; `failed`, если герой для `Rescue` погиб. Эффекты исходов задаются в XML как `<outcomes><li><outcome>success</outcome><nextStage>..</nextStage><effects>..</effects></li></outcomes>` (тот же класс `StoryChoice` без label). Переопредели `NextFor` и `EffectsFor`.

Требования:
- `Start` создаёт место (`SiteMaker`) и пишет его `ID` в `ctx.run.vars["siteId"]`. Сразу приходит письмо со ссылкой на место: текст этапа (`text`) с подстановками.
- `Tick` находит место по ID. Если места нет, а цель не выполнена (игрок ушёл и место исчезло), исход `timeout`. Цель проверяется по реальному состоянию: живые враги на карте места, пленник в караване, вещь в караване, тики присутствия, смерть героя.
- После `success` место можно оставить ванильно, оно само исчезнет, когда игрок уйдёт. Повторно этап не срабатывает: проверяй `ctx.run.vars`.
- `Objective(ctx)` — строка для Хроники: что сделать и сколько дней осталось.

## 2. `StoryStage_ColonyRaid : StoryStage` (Mechanic = "raid")
- Поля: `faction` (FactionDef defName, пусто — фракция квеста), `pointsFactor` (по умолчанию 1.0, от ванильных очков угрозы колонии), `delayHours` (по умолчанию 2).
- Через `delayHours` запускается ванильный рейд (`IncidentDefOf.RaidEnemy`, `IncidentParms` с фракцией и очками) на домашнюю карту. Фракция временно становится враждебной, если она не враг. Верни прежние отношения после исхода, только если в XML `restoreRelations=true`.
- Исход `success`, когда на домашней карте не осталось живых, не сбежавших и не пленённых пешек этого рейда. Lord закончился или все враги мертвы, в плену или ушли. Исход `timeout` через `timeoutDays` (по умолчанию 3).

## 3. Пример контента
`Defs/StoryDefs/Story_Samples.xml`: два небоевых для сюжета тестовых квеста с `requires` `story.e2samples=true`, флаг ставит только проба, так что в обычной игре они не появятся:
- `TMM_Story_Sample_Site`: этап `Site` с `objective` Clear, затем `Choice`;
- `TMM_Story_Sample_Raid`: этап `ColonyRaid`.

EN в Def, RU в `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Samples.xml`.


## Проверка в игре
Её пишешь тоже ты, агент прогонит: `Tests/InGameStoryStagesProbe.cs`, MapComponent по образцу других проб. Запуск через `[StaticConstructorOnStartup]` и `LongEventHandler.ExecuteWhenFinished`. Флаги запуска: `-tmm-story-stages-probe` для свежей игры и `-tmm-story-stages-reload` для загрузки сохранения `TMM-StoryStages`. Результат пиши в `GenFilePaths.SaveDataFolderPath` → `StoryStagesProbe.json` / `StoryStagesReloadProbe.json` в формате `{""passed"":bool,""checksCount"":n,""checks"":[{""name"",""passed"",""expected"",""actual""}],""errors"":[]}`.
## Проверка в игре (обязательна)
Проба `Tests/InGameStoryStagesProbe.cs`, компонент `StoryStages`, и reload-скрипт (клон `Tools/test_story_reload.ps1`). Проверки должны совершать реальные действия:
- ставит флаг, вызывает `SparkStory.Instance.DirectorTick()`, принимает предложение через `DiaOption.action` письма, как в `Tests/InGameStoryEngineProbe.cs`;
- **Site:** место создано на расстоянии в пределах minDays..maxDays, его ID в `vars`. Войти караваном. Цель Clear: убить врагов на карте места через `Kill` → после тиков исход `success`, следующий этап `Choice` пришёл письмом;
- **Site timeout:** второе прохождение с `timeoutDays` 0.1 → исход `timeout`, место удалено;
- **Raid:** через `delayHours` на домашней карте появились враги фракции. Убить их → `success`;
- **reload:** сохранение, пока место существует и цель не выполнена. После загрузки место находится по ID, цель доводится до `success` ровно один раз.

Затем прогони `StoryEngine` (fresh и reload) и `RegionalWorld`. Везде должно быть 0 ERROR и unexpectedRuntimeErrors 0, а в stdout.log не должно быть строки «Error while instantiating a mod». RimWorld не запущен, таймаут 15 минут, **не больше 2 попыток на шаг**. Cecil — только `Tools/cecil_methods.ps1 -Type ...`. Только C# 5: без `?.`, `=>`-членов, `nameof` и `is X x`. **Не сохраняй .ps1 без BOM**: в скриптах с кириллицей это ломает PowerShell 5.1.


## Сдача
Файлы с путём над каждым блоком:
- `Source/StoryStage_Site.cs`;
- `Source/StoryStage_ColonyRaid.cs`;
- `Defs/StoryDefs/Story_Samples.xml`;
- `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Samples.xml`;
- `Tests/InGameStoryStagesProbe.cs`.

В конце коротко перечисли все строки с `// CHECK API`.