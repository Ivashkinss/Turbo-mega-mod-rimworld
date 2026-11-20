# StoryHotelFix — встроить исправления E3 по ревью Codex

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: LegendaryItems

Codex не принял E3 (`Tools/Build/CodexStoryStageHotelReview/REPORT.md`): нашёл 6 блокирующих P1 и 1 P2. Исправления пишет Gemini Neon по промпту `Delegation/Hydrogen/42-Neon-StoryHotel-FIX-PROMPT.md`. Файлы будут в `Delegation/StoryContent/E3fix/`, пути в папке повторяют корень мода. Если папка пуста, запиши `blocked: waiting for Neon E3 fix` и переходи к следующей задаче очереди.

Задача идёт после StoryStageSiteFix: `StoryRelationRestorer` из E2fix уже в моде, а Neon прислал его расширенную версию.

## Сделать
1. Скопируй `Source/StoryStage_HotelVisit.cs`, `StoryStage_Deliver.cs`, `StoryRelationRestorer.cs`, `StoryPayment.cs` и `Tests/InGameStoryHotelProbe.cs`. Если пришёл `Source/OutpostPodLogistics.cs`, скопируй и его.
2. Собственное чтение `forceOutcome` в HotelVisit должно исчезнуть. Если Neon его оставил, удали.
3. Собери мод, исправь компиляцию и строки `// CHECK API` через `Tools/cecil_methods.ps1`. Логику Neon не меняй без причины, все изменения перечисли в отчёте.
4. **Одна DLL на все прогоны.** Флаг `-CompatibilityReload` запрещён.

## Проверка в игре (обязательна)
- `StoryHotel`: fresh и reload, все сценарии новой пробы.
- `StoryStages`: fresh и reload, потому что Restorer общий для обоих этапов.
- `StoryEngine`: fresh.
- `Outposts` и `OutpostCaravan`: fresh, если менялся `OutpostPodLogistics`.
- `Hospitality`: fresh.
- Правила: 0 ERROR, не больше 2 попыток на шаг, `.ps1` только с BOM.

Не коммитить. Сдача: `Tools/Build/GeminiStoryHotelFix/REPORT.md`. В отчёте укажи SHA256 итоговой DLL; он должен совпадать с Result.json всех прогонов.

## Замечания Claude к ответу Neon (20.11)
- Neon прислал все 6 файлов: Restorer, StoryPayment, OutpostPodLogistics, HotelVisit, Deliver, проба. Скобки сбалансированы, конструкций новее C# 5 нет, строк `// CHECK API` — 57.
- Публичный API `StoryRelationRestorer` из E2fix сохранён. Новое:
  - аренда перемирия (`AcquireTruceLease`);
  - учёт гостей;
  - Harmony-префикс `Prefix_Pawn_ExitMap`, ставится в статическом конструкторе `StoryRetrieveStackPatch`.
  Проверь, что этот конструктор вызывается при старте мода: в логе патч должен быть виден, иначе пойманный уход гостя не сработает.
- Deliver теперь может завершиться исходом `failed`: поселение исчезло, или оплата отклонена атомарно. Ядро закрывает квест как Failed, условие `done:` считает его выполненным, поэтому сюжет не блокируется. Это ожидаемо.
- Публичный API `OutpostPodLogistics` не изменился, но проверь `Outposts` fresh, потому что списание теперь атомарное.