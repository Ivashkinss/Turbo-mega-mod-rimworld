# OutpostMapsNativeProbe — перенос кандидата на живой Source и настоящая игровая проверка карт аванпостов

kind: implement · assignee: gemini · владелец очереди: Claude

Прочитай `Delegation/COORDINATION.md`. Контекст: твой прошлый кандидат `Tools/Build/GeminiOutpostMapProgression/` (станки по исследованиям, зоны посева, генератор с пустым баком, турели, 12 коек). Claude его просмотрел. Принять нельзя по двум причинам:

1. **Регрессия против живого Source.** После твоего снимка root исправил `StrategicOutpostMapComponent.cs`: `ThingMaker.MakeThing(entry.thingDef, entry.stuffDef ?? (entry.thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(entry.thingDef) : null))`. В кандидате (строка ~1194) снова `entry.stuffDef` без fallback — мешки с песком и прочие MadeFromStuff без явного материала дают ERROR. Других отличий живого файла от снимка нет (сравнено построчно, кроме переводов строк). `GenStep_StrategicOutpostBase.cs` совпадает.
2. **Проверка фиктивная.** `InGameOutpostMapProgressionProbe` засчитывает PASS за существование Def и без карты уходит в статическую проверку. Нужен настоящий прогон в игре.

## Разрешено

- Читать весь мод, `Data/` игры, metadata через Cecil.
- Писать только `Tools/Build/GeminiOutpostMapsProbe/` и `Tools/Build/GeminiWorkerState/OutpostMapsNativeProbe.json`.
- Компилировать. **Игру в этой задаче не запускать**: тест пойдёт через `Tools/test_component.ps1`, который собирает из корневого Source, а корень вливает Claude. Корневые Source/Tests/Tools/Languages/Assemblies/Git/QUEUE не менять.

## Сделать

### 1. `Candidate/Source/StrategicOutpostMapComponent.cs`
Твой кандидат + возвращённый fallback материала выше. Больше в core ничего не менять. Если при переносе найдёшь и исправишь собственные дефекты карты — перечисли их в REPORT с номерами строк.

Дополнительно разберись и исправь, если подтвердится:
- внешние элементы плана (генератор, мешки, турели) проверяются на Impassable, но не на воду/мост — генератор не должен вставать на воду или в болото без несущей способности (`TerrainAffordance` нужного размера, `GenConstruct.CanBuildOnTerrain` или аналог);
- внешние элементы не должны попадать в уже созданную зону посева, на улицу перед дверью, в interaction cell станка или на клетку кровати другого модуля;
- зона посева не создаётся повторно при повторной генерации/добавочном модуле и не задевает чужую зону игрока.

### 2. `Candidate/Languages/{English,Russian}/Keyed/OutpostMapProgression.xml`
Перенести как есть, если ключи используются; неиспользуемые убрать. Паритет ключей 100 %.

### 3. `Candidate/Tests/InGameOutpostMapsProbe.cs` — настоящий игровой probe
Флаг `-tmm-outpost-maps-probe`. Повтори структуру существующего `Tests/InGameStrategicOutpostsProbe.cs` и того, как `Tools/test_component.ps1` его запускает (отчёты, `Result.json`, UI probe-файл, сохранение). Отчёт `OutpostMapsProbe.json`: массив checks `{name, passed, expected, actual}` + общий `passed`. Ни один check не засчитывается за наличие имени Def или за компиляцию.

Обязательные проверки на **настоящих сгенерированных картах** аванпостов (через реальный путь основания/генерации/апгрейда core, как в существующем Outposts probe; не через прямой вызов GenerateLayoutPlan на произвольной карте):
- Все 4 типа (Factory, Farming, Mining, Watchtower) на уровнях 1, 3, 6, 10. Обычных кроватей ≥2 на ур. 1, ≥11 на ур. 10; медкойка не считается.
- Factory: без исследований — нет TableSculpting, есть CraftingSpot/доступные ручные станки; с завершёнными Smithing/Electricity/Machining/Fabrication (выставить в тестовом мире) — соответствующие станки появились. У каждого станка непустые `AllRecipes`.
- Каждая дверь, кровать и interaction cell станка достижимы с края карты нативной проверкой (`map.reachability.CanReach` с `TraverseParms` для колониста/`PathEndMode`), interaction cell стоячая.
- Электричество: генератор стоит на допустимом грунте, `Fuel == 0` сразу после постройки; генератор и потребители в одной `PowerNet`; до заправки потребители без питания; после настоящей `CompRefuelable.Refuel(...)` реальными дровами и прокрутки тиков — питание есть. `PowerOn` вручную не выставлять.
- Farming: есть `Zone_Growing` с ожидаемым растением по уровню, клетки без крыши, fertility ≥0.7; повторный апгрейд/вход не создаёт дубль; заранее созданная «игроком» зона рядом не изменена.
- Mining: нет новых жил золота/пластали (сравнить число mineable до/после вне footprint).
- Watchtower: мешки с материалом (stuff ≠ null, без ERROR), турели только при GunTurrets+Electricity, без созданных боеприпасов/топлива.
- Блокировка: ценный предмет (например серебро/золото) в прямоугольнике следующего модуля → апгрейд отказан, оплата core не списана, предмет цел, число построек не изменилось.
- Постройка «игрока» рядом (стена) сохраняется после апгрейда.
- Сохранение: сохранить тестовую игру в профиль (как делает Outposts probe). Повторная загрузка — отдельный флаг `-tmm-outpost-maps-reload` в том же файле: после загрузки сверить число построек по типам, зоны и растения, `builtLevel`, топливо генератора.

### 4. Изменения тестовых скриптов — как патчи, не правкой корня
- `Candidate/Tools/test_component.OutpostMaps.patch`: новый компонент `OutpostMaps` в `test_component.ps1` по образцу `Outposts`.
- `Candidate/Tools/test_outpost_maps_reload.ps1`: по образцу `Tools/test_outposts_reload.ps1`, со своими именами файлов.

### 5. `build_candidate.ps1`
Скопировать **живой** корневой `Source/` (только чтение) во временную `Work/` внутри outputDir, наложить `Candidate/Source`, скомпилировать DLL и probe так же, как это делает `Tools/build_assembly.ps1` и `test_component.ps1 -CompileProbeOnly`. Реально выполнить. В `Assemblies/` не писать.

## Сдача (`Tools/Build/GeminiOutpostMapsProbe/`)
`Candidate/Source/StrategicOutpostMapComponent.cs`, `Candidate/Languages/...`, `Candidate/Tests/InGameOutpostMapsProbe.cs`, `Candidate/Tools/test_component.OutpostMaps.patch`, `Candidate/Tools/test_outpost_maps_reload.ps1`, `build_candidate.ps1`, `DIFF.patch` (к живому `Source/StrategicOutpostMapComponent.cs`), `REPORT.md`, `VALIDATION.json` (exitcode компиляции DLL и probe, SHA256, `"native": "NOT RUN"`).

REPORT короткий: что изменено против прошлого кандидата, список проверок probe с тем, что именно сравнивается, известные риски, раздел «Для Claude». Без пересказа проекта. После готовности — ACK `done`.
