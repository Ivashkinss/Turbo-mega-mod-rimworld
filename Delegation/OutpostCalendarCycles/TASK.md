# OutpostCalendarCycles — настоящие игровые сутки для фермы, фабрики и дозорной башни

kind: run (новый тест + прогон в игре; Source не менять) · assignee: gemini · владелец очереди: Claude

Прочитай `Delegation/COORDINATION.md` (самоаудит, формат REPORT, правило 8 про BOM). Образец: `Tests/InGameStrategicOutpostsProbe.cs`. В нём шахта уже проверена по настоящим суткам: 20 пластали на 2 работниках, 100 на 8 после reload уровня 10. Здесь нужно то же для остальных типов. Производство: `Source/StrategicOutpost.cs` (`TryTriggerDailyCycle`, `ExecuteDailyProduction`, `PerformWatchtowerReconnaissance`, `fractionalRemainders`).

## Разрешено писать
- новые `Tests/InGameOutpostCalendarProbe.cs`, `Tools/test_outpost_calendar_reload.ps1` (UTF-8 с BOM);
- в `Tools/test_component.ps1` — одна строка компонента `OutpostCalendar` по образцу `Outposts` (флаг `-tmm-outpost-calendar-probe`, отчёт `OutpostCalendarProbe.json`, prefix `OutpostCalendar`). Сохранить файл в UTF-8 **с BOM**;
- `TestRuns/OutpostCalendar-*`, итоговые JSON в `Documentation/`, которые пишут сами скрипты;
- `Tools/Build/GeminiOutpostCalendarCycles/` (REPORT.md, копия probe-результатов) и ACK.

`Source/`, `Defs/`, `Languages/` и `Assemblies/TurboMegaMod.dll` НЕ менять (используй `-UseBuiltAssembly`). Если найдёшь ошибку производства, не чини её: опиши в REPORT с `файл:строка`, ожидаемым и фактическим числом. Это и есть ценный результат.

## Probe — одна игра, три аванпоста одновременно
Основание — реальным караваном через `StrategicOutpostCaravanUtility.FoundOutpostFromCaravan`, как в образце: 2 сгенерированных колониста + животное, `targetColonyTile=-1`, чтобы товар копился в аванпосте. Ни одного ручного вызова приватного производства; ждать настоящие тики (`Superfast`).

Ожидаемое число каждый день считай **независимо** в probe, по формуле из кода (сумма эффективных навыков × Rate × 1/числоПродуктов + перенесённый остаток, floor), и сравнивай на точное равенство. В отчёт пиши ожидаемое и фактическое число за каждый день.

1. **Ферма** (`Farming`): навыки Plants подобрать так, чтобы дневной выход был дробным (например 7 и 4) и остаток реально переносился. Выбрать 2 продукта (картофель + рис), чтобы проверить множитель 1/2. 3 настоящих дня подряд: точный выход каждого продукта по дням, остаток в `fractionalRemainders` после каждого дня, повторный `DoDailyProduction()` в тот же день ничего не добавляет.
2. **Фабрика** (`Factory`, `TMM_Outpost_Factory_ComponentIndustrial`): исследования для продукта завершить в тестовом мире. День 1 — сырья ровно на N единиц, где N меньше дневной мощности: сделано N, сырьё списано точно по рецепту. День 2 — сырья нет: 0 продукции, ничего бесплатного; запиши, что стало с остатком. День 3 — сырьё добавлено снова: производство возобновилось и совпало с расчётом. Отдельно: продукт без исследования (новый мир или проверка на ещё не изученном продукте) даёт 0.
3. **Дозорная башня** (`Watchtower`): 2 настоящих дня. Проверь, что делает `PerformWatchtowerReconnaissance` (поле, сообщение или письмо), и сверь найденное число поселений и враждебных в радиусе 15 с независимым подсчётом через `Find.WorldObjects.Settlements` и `TraversalDistanceBetween`. Товаров не появляется.
4. **Все три:** животные и работники живы после 3 дней (world GC), остаются колонистами, держатся в WorldPawns и не заспавнены. Каждое производство срабатывает в пределах 2500 тиков после `nextProductionTick`, следующий срок = +60000.
5. **Сохранение посреди цикла:** после дня 1 — `GameDataSaveLoader.SaveGame("TMM-OutpostCalendar")`, затем дни 2–3 в fresh. Reload (`-tmm-outpost-calendar-reload` в том же probe, скрипт `test_outpost_calendar_reload.ps1` по образцу `Tools/test_outposts_reload.ps1`) проверяет: после загрузки остатки, сырьё, сроки и личности совпадают с сохранёнными; день 2 после загрузки наступает один раз, в срок и с тем же числом, что в fresh.

## Прогон
```
powershell -File Tools/test_component.ps1 -Component OutpostCalendar -FullSet -UseBuiltAssembly
powershell -File Tools/test_outpost_calendar_reload.ps1 -Profile <профиль из первого шага>
```
Только если RimWorld не запущен. Если fresh упал из-за самого теста (fixture), исправь тест и прогони снова; все неудачные попытки перечисли в REPORT. Если упал из-за кода мода — не подгоняй ожидания под код, а опиши расхождение.

## Сдача
`Tools/Build/GeminiOutpostCalendarCycles/REPORT.md` в формате COORDINATION: итог PASS/FAIL, таблица «тип × день: ожидание/факт», поля Result.json и ReloadResult.json (passed, checks, runtimeErrors, unresolvedReferences, gpuLost, SHA256 DLL), профили, найденные ошибки производства, «Куда смотреть Claude», что не проверено, самоаудит. После этого ACK `done`.
