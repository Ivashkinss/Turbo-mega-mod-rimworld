# StoryStageHotel — этапы «гость в гостинице» и «доставка» (E3)

kind: implement + run · assignee: gemini (Antigravity) · afterTask: StoryStageSite

Прочитай `Documentation/STORY-ENGINE-DESIGN.md`, ядро (`Source/StoryDefs.cs`, `Source/SparkStory.cs`, `Source/StoryLetter.cs`) и свой E2 (`Source/StoryStage_Site.cs`). Ядро не меняй, данные этапа храни в `ctx.run.vars`. Новые файлы: `Source/StoryStage_HotelVisit.cs` и `Source/StoryStage_Deliver.cs`.

## 1. `StoryStage_HotelVisit` (Mechanic = "hotel")
Гость или делегация приходит в гостиницу колонии. Это шпион, перебежчик, переговорщик или беглые синты. Используй Hospitality так же, как `Source/HotelVisitGuarantee.cs`: через рефлексию, по готовым методам.
- **Поля:**
  - `faction` (пусто — фракция квеста);
  - `hero` (HeroDef, необязательный): этот герой приходит в составе группы, его берёт `HeroRegistry.GetOrCreate`;
  - `groupSize` (по умолчанию 1–3);
  - `stayDays` (по умолчанию 2);
  - `objective`:
    - `Host` — гость прожил `stayDays` и ушёл довольным, не во вражде;
    - `Investigate` — игрок за это время поговорил с гостем: любая социальная интеракция колониста с ним, есть ванильный журнал интеракций; либо гость сидел в одной комнате с колонистом-гуманитарием;
    - `Hide` — гость прожил `stayDays`, и за это время на карту не пришёл рейд фракции `hunterFaction`, либо пришёл и был отбит.
- Нет гостиницы, или приём гостей выключен — исход `noHotel`, письмо подсказывает, как открыть гостиницу. Сюжет не застревает.
- **Исходы:** `success`, `failed` (гость погиб, ушёл во вражде или его схватили), `noHotel`. Эффекты исходов — список `outcomes`, как в E2.

## 2. `StoryStage_Deliver` (Mechanic = "deliver")
- **Поля:** `thing` (ThingDef), `count`, `to` (`giverSettlement` — ближайшее поселение фракции квеста, или `colony` — сдать на складе колонии), `timeoutDays` (по умолчанию 10).
- `colony`: вещи из `count` лежат на складах домашней карты — по нажатию «Отдать» в письме-напоминании они списываются, так же как в `OutpostPodLogistics.DeductResource`.
- `giverSettlement`: игрок приводит караван с вещами на клетку поселения. Вещи списываются из каравана.
- **Исходы:** `success`, `timeout`.

## Проверка в игре (обязательна)
Проба `Tests/InGameStoryHotelProbe.cs`, компонент `StoryHotel`, и reload. Проверки должны совершать реальные действия:
- **Host:** гостиница готова, как в пробе `HotelVisit`. Квест с флагом-примером, принять. Группа с героем пришла и заселилась, прошло `stayDays`, ушла → `success`. Герой из реестра, а не новый: сверить ThingID.
- **noHotel:** без гостиницы → `noHotel` и письмо-подсказка.
- **Deliver colony:** 50 стали на складе → после отдачи склад −50, `success`.
- **Reload:** сохранение во время проживания, после загрузки этап завершается ровно один раз.

Затем прогони `StoryEngine`, `StoryStages`, `Hospitality` и `HotelVisit`. Правила те же: 0 ERROR, не больше 2 попыток на шаг, Cecil только из файла, C# 5, .ps1 только с BOM. Дополнительно: установи готовый остаток акта 1 от Hydrogen — `Delegation/StoryContent/Act1/Story_Act1_Main.xml` → `Defs/StoryDefs/`, `Story_Act1_Main.ru.xml` → `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act1_Main.xml` (он использует твои этапы Site/ColonyRaid/HotelVisit; в квесте `TMM_Story_A1_Interlude_StaticBroadcast` подставь ванильный `sitePart`). Игра должна грузиться без ошибок конфигурации, и проба `StoryEngine` должна по-прежнему проходить (новые квесты требуют `done:TMM_Story_A1_Castiel` — допиши в её проверки, что после Кастиэля открылся `TMM_Story_A1_UninvitedGuests`).

Так же поставь главную нить акта 2: `Delegation/StoryContent/Act2/Story_Act2_Main.xml` → `Defs/StoryDefs/`, `.ru.xml` → `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act2_Main.xml`. Так же — акты 3 и 4: `Delegation/StoryContent/Act3/…` и `Act4/…` (оба `.xml` + `.ru.xml`) (он использует `StoryStage_Deliver`). В них нет `sitePart` — используй значение по умолчанию своего этапа Site.

Не коммитить. Сдача: `Tools/Build/GeminiStoryStageHotel/REPORT.md`.
