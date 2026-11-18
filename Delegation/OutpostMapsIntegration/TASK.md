# OutpostMapsIntegration — исправить замечания Codex, перенести карты в корень, прогнать в игре

kind: integrate + run · assignee: gemini · afterTask: OutpostBalanceReview

Прочитай `Delegation/COORDINATION.md`, своё прошлое `Tools/Build/GeminiOutpostMapsProbe/REPORT.md` и ревью Codex `Tools/Build/CodexOutpostMapsReview/REPORT.md` (замечания F1–F6 с номерами строк). Исходные требования к probe — `Delegation/OutpostMapsNativeProbe/TASK.md`, раздел 3; они в силе полностью.

## Шаг 1. Исправления в кандидате (`Tools/Build/GeminiOutpostMapsProbe/Candidate/`, оригинальные файлы сначала скопируй в `Tools/Build/GeminiOutpostMapsIntegration/Before/`)
- **F1.** Планировщик отказывает модулю, если любая клетка его footprint (внутри и снаружи bounds) входит в чужую зону: игрока или ранее созданную самим аванпостом зону посева. Проверка до любых мутаций: замены грунта, крыши, списания. Зону посева самого модуля создавать после проверки, не пересекая существующие.
- **F2.** Электричество проверять стадиями по настоящим `TicksGame`: сеть после спавна, без питания до заправки, `CompRefuelable.Refuel` реальными дровами из инвентаря или карты, затем ждать реальные тики (не ручной `PowerNetsTick`). Есть питание, топливо расходуется (`Fuel` уменьшается со временем).
- **F3.** Мешки и турели проверять на тех уровнях, где план их реально создаёт. Каждый `TryUpgrade` подтверждать (`level` и `builtLevel`).
- **F4.** Отказ из-за блокировки: перекрыть ценными предметами **все** допустимые варианты размещения следующего модуля; проверить отказ, неизменную оплату, сохранность всех предметов и число построек. Отдельно: при одной кучке в default rect модуль ставится в другом месте, кучка цела, оплата списана точно.
- **F5.** Снимки для reload: `MapComponent.builtLevel`, число построек по def, клетки и растения каждой зоны, топливо генератора. После загрузки сверять именно их. Все 4 типа на уровнях 1, 3, 6, 10. Каждая дверь, койка и interaction cell станка — `CanReach` с `TraverseParms` колониста. Ни одного созданного боеприпаса. Дедупликацию зоны проверять настоящим повторным путём (повторный вход/деплой, повторный апгрейд), а не вызовом на занятых клетках.
- **F6.** Не писать фиктивный UI PASS: UI-файл `{"passed":false,"visualLayoutInspected":false,"note":"not inspected"}`, а если test_component требует passed для UI, — честный `passed:true` только при успешном Finish без ошибки и `visualLayoutInspected:false`. Убедиться, что Finish(ex) никогда не даёт passed.
- Пересобрать `build_candidate.ps1`, exit 0.

## Шаг 2. Перенос в корень (только эти файлы)
- `Source/StrategicOutpostMapComponent.cs` ← кандидат;
- `Languages/English/Keyed/OutpostMapProgression.xml`, `Languages/Russian/Keyed/OutpostMapProgression.xml` ← кандидат;
- `Tests/InGameOutpostMapsProbe.cs` ← кандидат;
- `Tools/test_component.ps1` — одна строка компонента `OutpostMaps` (UTF-8 с BOM сохранить);
- `Tools/test_outpost_maps_reload.ps1` ← кандидат (UTF-8 с BOM).
Перед переносом убедись, что живой `Source/StrategicOutpostMapComponent.cs` по git blob всё ещё `dda26891…` (как у Codex). Если нет — `error`, ничего не переносить.

## Шаг 3. Сборка и прогон
```
powershell -File Tools/build_assembly.ps1
powershell -File Tools/test_component.ps1 -Component OutpostMaps -FullSet -UseBuiltAssembly
powershell -File Tools/test_outpost_maps_reload.ps1 -Profile <профиль>
```
Затем регрессии ядра на новой DLL, обязательно:
```
powershell -File Tools/test_component.ps1 -Component Outposts -FullSet -UseBuiltAssembly
powershell -File Tools/test_outposts_reload.ps1 -Profile <профиль Outposts>
powershell -File Tools/test_component.ps1 -Component OutpostCalendar -FullSet -UseBuiltAssembly
powershell -File Tools/test_outpost_calendar_reload.ps1 -Profile <профиль OutpostCalendar>
```
Только если RimWorld не запущен.
- Если упал сам тест (fixture) — правь тест.
- Если упал код карты — правь `Source/StrategicOutpostMapComponent.cs`, затем пересборка и все прогоны заново. Каждую попытку и её причину перечисли в REPORT. Не больше 6 попыток.
- Ожидания под код не подгонять.

**Если после 6 попыток не всё зелёное:** откат корня `git checkout -- Source/StrategicOutpostMapComponent.cs Languages/English/Keyed/OutpostMapProgression.xml Languages/Russian/Keyed/OutpostMapProgression.xml` (новые тестовые файлы оставить), `build_assembly.ps1` и проверка, что SHA256 `Assemblies/TurboMegaMod.dll` снова совпадает со сборкой из `HEAD`. В ACK — `error` с причиной.

Не коммитить, не пушить, очередь не менять. `StrategicOutpost.cs`, PawnLifecycle, holders, оплату и поставки не трогать. Если без них не обойтись — остановиться и описать в REPORT.

## Сдача: `Tools/Build/GeminiOutpostMapsIntegration/REPORT.md`
Формат COORDINATION. Обязательно:
- таблица исправлений F1–F6 (`файл:строка`);
- SHA256 новой DLL;
- для каждого из 6 прогонов: профиль, passed, checks, runtimeErrors, unresolvedReferences, gpuLost;
- таблица «тип × уровень»: кровати, станки, достижимость, питание;
- все попытки;
- «Куда смотреть Claude»;
- самоаудит.
