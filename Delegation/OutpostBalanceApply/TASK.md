# OutpostBalanceApply — применить одобренные числа баланса и перепроверить в игре

kind: integrate + run · assignee: gemini · afterTask: OutpostMapsIntegration

Основание: твой `Tools/Build/GeminiOutpostBalanceReview/PROPOSALS.md`. Claude одобрил предложения **1, 2, 3, 4, 6, 7**. **5** (baseCapacity 3) отклонено: два основателя — решение пользователя. **8** (климат для дьяволоткани) отложено, не делать.

Если OutpostMapsIntegration завершилась `error`, всё равно выполняй эту задачу на текущем корне (после её отката).

## Разрешено менять (только эти места)
- `Defs/StrategicOutpostDefs/StrategicOutpostProducts.xml`:
  - пласталь: `yieldPerSkill` 0.4, `baseDailyYieldPerPawn` 4.0;
  - синткань: 1.0 / 10.0;
  - продвинутый компонент: Rate 0.06 — поставь явный `yieldPerSkill` 0.06 или `baseDailyYieldPerPawn` 0.6, смотря что сейчас задано; проверь, какое поле реально действует (`StrategicOutpostDef.Rate`);
  - дерево: Rate 4.0;
  - гиперткань: 0.35 / 3.5.
- `Defs/WorldObjectDefs/WorldObjects_StrategicOutpost.xml`: `costMultiplier` 1.35 (XML переопределяет дефолт в `Source/StrategicOutpostDef.cs:13` — `.cs` не менять).
- Ожидания в тестах, которые зависят от этих чисел, и только они. Например, `Tests/InGameStrategicOutpostsProbe.cs`: 2×15 шахтёров сейчас дают 20 пластали в день → должно стать 12; 8 работников с суммой 150 — 100 → 60; оплата улучшений считается через `NextUpgradeCost`. Пересчитай каждое ожидание **вручную по формуле** и оставь рядом комментарий с расчётом. Учти float/double-округление (floor и перенос остатка); при сомнении выбирай навыки, дающие целое число.
- `Documentation/OUTPOSTS.md` — таблица выхода и стоимости улучшений, если там есть старые числа.

Код `Source/` не менять.

## Прогон (если RimWorld не запущен)
```
powershell -File Tools/build_assembly.ps1
powershell -File Tools/test_component.ps1 -Component Outposts -FullSet -UseBuiltAssembly
powershell -File Tools/test_outposts_reload.ps1 -Profile <профиль Outposts>
powershell -File Tools/test_outposts_tier10_reload.ps1 -Profile <тот же профиль>
powershell -File Tools/test_component.ps1 -Component OutpostCalendar -FullSet -UseBuiltAssembly
powershell -File Tools/test_outpost_calendar_reload.ps1 -Profile <профиль OutpostCalendar>
```
И `OutpostMaps` fresh + reload, если интеграция карт прошла.

- Defs меняются без пересборки DLL, но `build_assembly.ps1` всё равно запусти: скрипты сверяют SHA.
- Если упал тест из-за старого ожидания — исправь ожидание по формуле.
- Если упал по другой причине — опиши.
- Не больше 4 попыток. При неуспехе откатить XML и тесты через `git checkout --` и записать `error`.

Не коммитить, не пушить.

## Сдача: `Tools/Build/GeminiOutpostBalanceApply/REPORT.md`
Формат COORDINATION:
- таблица «параметр: было → стало»;
- каждое изменённое ожидание теста: было → стало, формула;
- все прогоны с полями passed, checks, runtimeErrors, unresolvedReferences, gpuLost, SHA256;
- новые стоимости уровней 2–10 (из `NextUpgradeCost`) и реальный выход шахты за день из игры;
- «Куда смотреть Claude»;
- самоаудит.
