# StoryStageSite — этапы сюжета «место на карте мира» и «рейд на колонию» (E2)

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: OutpostTabStageB

> **ДОПОЛНЕНИЕ (Claude, 19.11 23:30):** код Neon уже лежит в `Delegation/StoryContent/E2/` (Source/StoryStage_Site.cs, Source/StoryStage_ColonyRaid.cs, Tests/InGameStoryStagesProbe.cs, Defs и RU). Ты начал раньше и написал свою версию — **замени её файлами Neon** и встраивай их (сборка, `// CHECK API` через Cecil, проба). Свою версию оставь в `Tools/Build/GeminiStoryStageSite/own-version/` для сравнения. Вернуться к своей можно только если версия Neon не проходит пробу после 2 попыток исправления — тогда опиши в отчёте почему.

> **ИЗМЕНЕНИЕ (Claude, 19.11):** код этого этапа пишет Gemini Neon в чате, по промпту `Delegation/Hydrogen/31-Neon-StoryStageSite-CODE-PROMPT.md`. Готовые файлы будут лежать в `Delegation/StoryContent/E2/`. **Твоя задача — не писать с нуля, а встроить эти файлы:** скопировать их по путям, собрать и исправить ошибки компиляции и API (все строки `// CHECK API` сверь через `Tools/cecil_methods.ps1`), прогнать пробу, добиться прохождения и сохранить логику Neon. Если пробу приходится переписывать, перечисли изменения в отчёте. Если в `Delegation/StoryContent/E2/` ещё нет файлов, запиши в своё состояние `blocked: waiting for Neon E2 code` и бери следующую задачу. Требования ниже остаются в силе — это то, что код должен делать.

Движок сюжета (E1) принят: `Documentation/STORY-ENGINE-DESIGN.md`, `Source/StoryDefs.cs`, `Source/SparkStory.cs`, `Source/StoryLetter.cs`. Прочитай всё до строчки. **Ядро не меняй.** Нельзя трогать `SparkStory`, `StoryQuestRun`, `StoryQuestDef`, `StoryStage`, письмо, режиссёр и формат сохранения. Свои данные этапа храни только в `ctx.run.vars` (словарь string→string, сохраняется и очищается при входе в этап). Для пробы есть хук `SparkStory.TestForceOutcome(run, run.gen, outcome)` (только с аргументом `-tmm-story-test`). Ядро исправлено по ревью Codex (`Tools/Build/CodexStoryEngineReview/REPORT.md`, F1–F7): у прогона есть `gen` и `stageKey`, письма проверяют поколение — не обходи эти проверки. Для мест на карте: при загрузке сопоставляй место по ID из `vars`; если домашней карты нет, активный этап продолжает тикать — обработай null. Новый код клади в новые файлы `Source/StoryStage_Site.cs` и `Source/StoryStage_ColonyRaid.cs`.

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

## Проверка в игре (обязательна)
Проба `Tests/InGameStoryStagesProbe.cs`, компонент `StoryStages`, и reload-скрипт (клон `Tools/test_story_reload.ps1`). Проверки должны совершать реальные действия:
- ставит флаг, вызывает `SparkStory.Instance.DirectorTick()`, принимает предложение через `DiaOption.action` письма, как в `Tests/InGameStoryEngineProbe.cs`;
- **Site:** место создано на расстоянии в пределах minDays..maxDays, его ID в `vars`. Войти караваном. Цель Clear: убить врагов на карте места через `Kill` → после тиков исход `success`, следующий этап `Choice` пришёл письмом;
- **Site timeout:** второе прохождение с `timeoutDays` 0.1 → исход `timeout`, место удалено;
- **Raid:** через `delayHours` на домашней карте появились враги фракции. Убить их → `success`;
- **reload:** сохранение, пока место существует и цель не выполнена. После загрузки место находится по ID, цель доводится до `success` ровно один раз.

Затем прогони `StoryEngine` (fresh и reload) и `RegionalWorld`. Везде должно быть 0 ERROR и unexpectedRuntimeErrors 0, а в stdout.log не должно быть строки «Error while instantiating a mod». RimWorld не запущен, таймаут 15 минут, **не больше 2 попыток на шаг**. Cecil — только `Tools/cecil_methods.ps1 -Type ...`. Только C# 5: без `?.`, `=>`-членов, `nameof` и `is X x`. **Не сохраняй .ps1 без BOM**: в скриптах с кириллицей это ломает PowerShell 5.1.

Не коммитить. Сдача: `Tools/Build/GeminiStoryStageSite/REPORT.md` в формате COORDINATION.
