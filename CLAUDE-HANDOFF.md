# Передача разработки Claude — 18 ноября 2026

Пользователь попросил Codex закончить текущую задачу и остановиться до обновления лимитов. Новых заданий Codex больше не ставит. Проект: Turbo mega mod, packageId `colonyproject.integrated`. Прочитать AGENTS.md и свежий верх CONTINUE.md. Работа остаётся локальной: последнее прямое решение пользователя — «Пока продолжай локально», git push не повторять.

## Готовое состояние

- Последний базовый коммит до ядра аванпостов:12c214e. Региональное расселение сохраняет полный набор исходных точек. Новый мир: столица10, центры9/8, обычные базы1–7. Реальные мировые караваны/патрули с грузом, сохранением и доставкой подключены. Стартовый UI/HUD freeze исправлен.
- Новое ядро собственных аванпостов установлено. Финальная DLL SHA256 `954203b8586ffb348a595d3519d11bcb2af24508fd08ffe65c955ad82c00b464`. Четыре типа, два основателя+животные, ежедневный выпуск, ингредиенты фабрики/текстильные исследования, доставка,10уровней/11работников, сохраняемые карты, автономный/физический режим.
- Прочитать Documentation/OUTPOSTS.md и OutpostsCoreVerification.json.34native checks fresh на4654ddd9 +55compatibility reload на954203b8, оба clean. Это НЕ89проверок на одной DLL. Профиль TestRuns/Outposts-FullSet-20261118-002418, настоящий сейв TMM-StrategicOutpost. Первый день60000ticks→20plasteel для2×Mining15. Верхние уровни строились на очищенных от природных препятствий тестовых площадках; nativeCanReach проверил12коек.8реальных работников с суммой150 дали прогноз100/день, второй день100не ожидался. Рецепты реально расходовали сырьё, капсулы доставили груз в колонию.
- **Повторная загрузка уровня 10 проверена (Claude, 18.11).** Новый флаг probe `-tmm-outpost-tier10-reload` и скрипт `Tools/test_outposts_tier10_reload.ps1`. Профиль TestRuns/Outposts-FullSet-20261118-002418, сейв TMM-OutpostTier10 (SHA256 6bf16de7…), записан и загружен на той же DLL 954203b8. Результат: 120 checks, passed, runtimeErrors 0, unresolvedReferences 0, gpuLost false. Что проверено: уровень, 11 мест, отсутствие дальнейших улучшений, 8 сохранённых работников-колонистов в WorldPawns, основатели и животное, навык 150. Затем настоящий игровой день: производство ровно на дедлайне 120028 дало ровно 100 пластали, без дублей. После этого вывод всех 8 на карту: 12 обычных коек, каждый работник может дойти до каждой (96 нативных CanReach), возврат сохраняет личности, время в физическом режиме задним числом не засчитывается. Итог в Documentation/Outposts-Tier10-Reload.json. Тестовые сохранения содержат отдельный test-probe class и предназначены для собственного стенда, не для пользователя.

- **Календарь ферма/фабрика/башня проверен (Gemini OutpostCalendarCycles, принято Claude 18.11).** `Tests/InGameOutpostCalendarProbe.cs`, `Tools/test_outpost_calendar_reload.ps1`, компонент `OutpostCalendar` в test_component. Профиль TestRuns/OutpostCalendar-FullSet-20261118-034050, DLL 954203b8: fresh — 39 checks, reload — 16 checks, оба с 0 runtimeErrors и 0 refs, gpuLost false. Результаты по дням:
  - Ферма, 2 продукта, навыки 7+4: 12 → 24 → 37 каждого, остаток 0.375 → 0.750 → 0.125.
  - Фабрика: 24 стали дали 2 компонента; без сырья 0; после 48 стали +4; неизученный Spacer даёт 0.
  - Башня: 0 товаров.
  - Сохранение после дня 1 и загрузка сохраняют остатки, сроки и личности, день 2 после загрузки совпал с fresh.
  - Слабые места: после reload день 2 сверяется только по картофелю и фабрике; отгрузка караваном (`targetColonyTile>=0`) не проверялась; разведка башни нашла 1 поселение (0 враждебных) — проверка формальная.

## Важные исправления ядра, не потерять

- `StrategicOutpostPawnLifecycle.cs`: Native WorldPawns вызывает Tick напрямую, обходя ShouldTickContents. Только наши хранимые пешки приостановлены; карта/караваны живут нормально. Native Notify_PassedToWorld для свободной мировой пешки может случайно сменить player-фракцию — для собственного holder этот callback пропускается с сохранением timestamp. Иначе работники переставали быть колонистами.
- `SetStoredPawnProtection` защищает хранящихся людей/животных от world GC через KeepForever и освобождает защиту при возвращении в караван. Без этого вьючное животное исчезало за сутки. Spawn/deploy/recall сохраняют личности, фракцию и WorldPawns-ссылки. Не менять на простое TryAdd spawnedPawn.
- Производство только через единый дедлайн; private ExecuteDailyProduction. Никаких бесплатных повторных дней. Настоящие RecipeDefs/исследования/ингредиенты; Gold0.3 в XML =3реальные единицы.
- Shipment: preflight drop spot до извлечения, подтверждение MapHeld и возврат непринятого груза. Не возвращать слепые Remove или бесхозные taken вещи. Slag отключён.
- Sandbags требуют material: сохранять GenStuff.DefaultStuffFor fallback. Старый кандидат выдавал4ERROR.
- Эвакуация не удаляет оставшийся груз и посещённую карту. Уничтожение всей карты отдельным безопасным flow ещё не реализовано.

## Следующий пакет Gemini — готов, но НЕ проверен/НЕ установлен

`Tools/Build/GeminiOutpostMapProgression/` (ACK done). Только map-overlay, разные типы/оснащение/зоны посева/пустой генератор/проводка/исследования. TASK: Delegation/OutpostMapProgression/TASK.md. Копии patch и отчёта сохранены рядом как CANDIDATE.patch и CANDIDATE-REPORT.md для доступности из Git. Код не считать принятым по compilationSUCCESS.

Первый следующий шаг: независимо просмотреть и объединить ограниченный map-overlay с текущим безопасным planner, проверить реальные Def/материалы/проходы/энергосеть/производственные станки в игре. НЕ копировать frozen RootSource поверх живого Source: снимок предшествовал последним lifecycle/shipment-исправлениям. Не перетирать StrategicOutpost.cs, PawnLifecycle.cs и holders ради генератора карт.

После этого: пользовательская приёмка меню/входа/физической карты, сохранение10уровня, реальные календарные циклы плантации/фабрики/дозора, контекст работ/сырья и баланс. Затем связность стратегической основы и15фракций из Documentation/FACTIONS-CONTENT-PLAN.md. Гостиничные происшествия/карательные рейды/бунты отложены к совместному сюжетному этапу. Коммерческое общение в комплексе — неинтимная беседа; прежние сексуальные идеи не являются текущим scope.

## Стенд и координация

Все изменения только внутри мода. Не трогать оригиналы/игровые настройки/пользовательские сейвы. Не запускать тесты, когда пользователь играет; закрывать только свой скрытый процесс. После теста удалять только свой testDLL. Вне тестов Assemblies/TurboMegaMod.TestProbe.dll отсутствует.

Сборка Tools/build_assembly.ps1; тесты Tools/test_component.ps1 -Component Outposts -FullSet и Tools/test_outposts_reload.ps1 -Profile <passed fixture>. CompatibilityReload явно разрешает проверку старого fixture на новой DLL и фиксирует оба SHA, не скрывает runtime/ref/GPU ошибки. Checkpoint debug runner и TMM-OutpostDay позволяют исследовать фазу после суток, но не заменяют полноценный freshPASS. Reference ABI1.6, native проверялся1.7. Игровой Assembly-CSharp не загружать в хост для исполнения — только Cecil/компилятор.

Gemini polling использует Delegation/QUEUE.json, root/Claude владеет очередью, worker — только разрешённым output и ACK. Сейчас активных новых задач нет; MapProgression done awaiting review. Дальше задавать конкретные ограниченные реализации и независимо проверять. Командные иконки7PNG уже проверены/включены; их генератор в Tools/Build/GeminiOutpostCommandAssets. Другие исторические кандидаты и резервные копии только в ignored Tools/Build, большие логи/сейвы только в TestRuns — не коммитить.

Снимок Outpost-Level10.png осмотрен, но из-за тайминга переключения/тумана не подтверждает обзор всей планировки. Не объявлять UI/внешний вид полностью проверенными. Пользовательский сейв, live1.6, все биомы/расы/моды/битвы и длительная нагрузка не проверялись.

## 18.11.2026 — OutpostMapsIntegration принят
Карты аванпостов перенесены в корень с исправлениями Codex F1–F6. DLL d9d3a293…; 6/6 игровых прогонов чистые: OutpostMaps 53+15, Outposts 34+55, OutpostCalendar 39+16, runtimeErrors 0, unresolved 0, gpuLost false. Профили: TestRuns/OutpostMaps-FullSet-20261118-071621, Outposts-FullSet-20261118-071914, OutpostCalendar-FullSet-20261118-072419. Изменения по ходу: L7 HospitalBed → Bed, упрощён L10, генератор на деревянном полу.
Открыто: санитайзер связей в Tests/InGameOutpostCalendarProbe.cs:446-479 может скрывать потерю пешки → OutpostRelationLeakAudit. Tier10 reload на новой DLL ещё не перезапускался, это часть OutpostBalanceApply. UI карт в игре глазами не смотрели (visualLayoutInspected false).
## 18.11.2026 — OutpostBalanceApply принят
Баланс аванпостов в XML:
- пласталь 0.4;
- синткань 1.0;
- продвинутый компонент 0.06;
- дерево 4.0;
- гиперткань 0.35;
- costMultiplier 1.35: уровни 500/675/911/1230/1661/2242/3027/4086/5516, сумма около 19.8k.
DLL 05177ace…; 7/7 прогонов чистые, включая Tier10 reload 120/120. Шахта в игре: 12 пластали в день (2×15) и 60 (сумма навыков 150).
Колонка «было» в отчёте Gemini неверна, XML-дифф правильный. Тесты теперь очищают relationshipRecords перед сохранением. Причину выясняет OutpostRelationLeakAudit.
## 18.11.2026 — HeroRegistryStage1 принят (Claude)
Новые файлы:
- `Source/HeroDef.cs`, `Source/HeroRegistry.cs`: WorldComponent; состояние героя определяется опросом раз в 250 тиков; постфикс к WorldPawnGC.GetCriticalPawnReason защищает героев от сборщика мусора;
- `Defs/HeroDefs/Heroes_MinutemenHunters.xml`: Престон, Дин, Сэм. Пока фракции нет в моде, GetOrCreate возвращает для них null;
- probe `Tests/InGameHeroRegistryProbe.cs`, reload-скрипт `Tools/test_hero_registry_reload.ps1`.

Игровые прогоны:
- fresh 21/21, reload 9/9, профиль TestRuns/HeroRegistry-FullSet-20261118-151509;
- регрессия Outposts чистая (TestRuns/Outposts-FullSet-20261118-152223). Первый прогон упал на случайной карте: нет места для модуля 2-го уровня. Это не связано с героями, исправление — задача OutpostsProbeFlakyLevel2.

Отложено: ксенотипы, генераторы баз и рейдов (этап 2), Эдем, гиганты.
## 18.11.2026 — правки реестра героев по приёмке Codex (F1–F3)
- **F1.** Состояние героя теперь считается по `MapHeld` и каравану: герой в капсуле или криокапсуле на чужой карте — `Away`.
- **F2.** Префикс к `Faction.TryGenerateNewLeader` выбирает героя-преемника внутри ванильного `Notify_LeaderDied`, поэтому письмо и квестовый сигнал называют именно его.
- **F3.** Герои передаются в мир в режиме `Decide`, от сборщика мусора их защищает только постфикс. В probe есть отдельный герой без ванильных причин для сохранения.
- Reload ждёт 2 игровых дня.
- Прогоны: fresh 28/28, reload 10/10, runtimeErrors 0; профиль TestRuns/HeroRegistry-FullSet-20261118-164347. Регрессию Outposts после правок не перезапускал: правки касаются только героев.
## 18.11.2026 — фракция «Минитмены и охотники» в моде (MinutemenIntegrate принят)
Первая новая фракция в моде, 23 новых файла:
- 7 `.cs`;
- Defs фракции, пешек, квестов, предметов и сайта;
- EN/RU Keyed и DefInjected;
- probe.

Герои связаны: Престон Гарви создаётся как настоящий лидер фракции.

Прогоны на одной DLL c2ad8f47…, в логах 0 строк ERROR или CRIT:
- MinutemenHunters 36/36;
- HeroRegistry 31/31 + reload 10/10;
- Outposts 34/34.

Не проверено:
- квесты «Помощь» и «Архив» целиком в игре;
- Престон дольше 2 дней;
- поведение в старых сейвах: фракция в существующий мир не добавляется.
## 18.11.2026 — ксенотип «Демон», этап 1 (внешность), Claude
Новые файлы (всё только при Biotech, через `MayRequire`):
- `Defs/GeneDefs/Genes_Demon.xml`, 6 генов:
  - 3 вида рогов;
  - хвост со стрелкой;
  - дьявольские глаза и чёрные глаза. Узлы глаз ванильные, с якорями RightEye/LeftEye;
- `Defs/XenotypeDefs/Xenotype_Demon.xml`: ксенотип TMM_Demon — кожа DeepRed или InkBlack, рога, хвост, чёрные глаза, FireResistant, MaxTemp_LargeIncrease, Aggression_Aggressive;
- RU-перевод;
- текстуры в `Textures/Things/TMM/Xenotypes/Demon/`: рога — эталон Claude; малые и бараньи рога, хвост — Gemini; глаза перерисованы под ванильный формат одного глаза.

Проверка: probe `Tests/InGameDemonXenotypeProbe.cs`, 32/32, ERROR 0. Портреты из игры — `Documentation/Images/DemonXenotype-ingame.png`.

Не сделано:
- маски перекраса;
- оверлей трещин кожи: вылезает за контур головы;
- способности демонов;
- привязка героев-демонов (Кроули, Азазель…).
## 18.11.2026 — ксенотип «Ангел», этап 1 (внешность), Claude
- `Defs/GeneDefs/Genes_Angel.xml`: крылья (узел на теле, от лопаток) и сияющие бело-голубые глаза (ванильные узлы глаз).
- `Defs/XenotypeDefs/Xenotype_Angel.xml`, TMM_Angel. Кроме двух генов внешности — ванильные Aggression_DeadCalm, Immunity_SuperStrong, WoundHealing_Fast, Pain_Reduced, PsychicAbility_Enhanced, Beauty_Pretty. Кожа не меняется.
- Текстуры в `Textures/Things/TMM/Xenotypes/Angel/`: крылья из пилота Gemini, глаза — Claude.
- Прогон: probe тот же, флаг `-tmm-angel-xenotype-probe`, 20/20, ERROR 0. Портреты: `Documentation/Images/AngelXenotype-ingame.png`.

Перепроверка Codex исправлений реестра (`Tools/Build/CodexHeroRegistryFixRecheck/REPORT.md`):
- F2 закрыт;
- F1 открыт: герой в летящей транспортной капсуле считается AtHome;
- N1: явный `PassToWorld(Discard)` уничтожает героя в обход защиты;
- N2: проверка письма слабая.
Следующая задача Claude.