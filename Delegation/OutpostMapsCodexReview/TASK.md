# OutpostMapsCodexReview — независимая проверка кандидата карт аванпостов

kind: accept (только ревью, БЕЗ коммита и без изменения корня) · assignee: codex · afterTask: OutpostMapsNativeProbe (Gemini)

Прочитай `Delegation/COORDINATION.md` и `Delegation/OutpostMapsNativeProbe/TASK.md` (что требовалось). Кандидат Gemini: `Tools/Build/GeminiOutpostMapsProbe/`. Начни с его `REPORT.md`, раздела «Куда смотреть Claude», и `DIFF.patch` к живому `Source/StrategicOutpostMapComponent.cs`.

Твой лимит маленький: читай diff и конкретные места, а не весь мод.

## Проверить (каждый пункт — да/нет + `файл:строка`)

1. **Сохранность вещей и построек игрока.** Планировщик ничего не уничтожает, кроме природных препятствий в своём прямоугольнике. Вещи без фракции (серебро, медикаменты), трупы, чужие и игроковы постройки блокируют модуль, а не стираются. Проверка делается до списания оплаты и до любых мутаций карты. Зона посева не перезаписывает чужую зону и не дублируется.
2. **Регрессии против живого корня.** Сохранён fallback `entry.stuffDef ?? (MadeFromStuff ? GenStuff.DefaultStuffFor(...) : null)`. Вне `StrategicOutpostMapComponent.cs` в Source ничего не менялось. `StrategicOutpost.cs`, `StrategicOutpostPawnLifecycle.cs`, holders, оплата и поставки не тронуты.
3. **Probe не фиктивный.** Нет PASS за существование Def или за компиляцию. Нет статического запасного пути без настоящей карты. Карты создаются через реальный путь основания, улучшения и `DeployPersonnelToMap`. Проверки CanReach, питания (заправка реальным топливом, без ручного `PowerOn`) и reload сверяют реальные значения.
4. **Сборка.** Повтори `build_candidate.ps1` (компилирует в `Work/` внутри outputDir, в `Assemblies/` не пишет). Запиши exit code и SHA256.
5. **Электричество и грунт.** Генератор не ставится на воду или болото без нужной TerrainAffordance. Внешние элементы не перекрывают двери, interaction cell, кровати и зону посева.

Игру НЕ запускать: прогон в игре сделает Gemini после интеграции.

## Разрешено писать
Только `Tools/Build/CodexOutpostMapsReview/REPORT.md` и `Tools/Build/CodexWorkerState/OutpostMapsCodexReview.json`.

## REPORT (до ~60 строк)
Вердикт одной строкой: `ИНТЕГРИРОВАТЬ` / `ИНТЕГРИРОВАТЬ ПОСЛЕ ПРАВОК` / `ОТКЛОНИТЬ`. Затем пункты 1–5. Для каждой найденной проблемы: `файл:строка`, почему это ошибка, конкретная правка (1–3 строки кода или словами). Отдельно: что не проверено.
