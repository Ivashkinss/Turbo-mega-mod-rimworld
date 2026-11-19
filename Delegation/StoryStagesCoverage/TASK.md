# StoryStagesCoverage — допроверить все цели этапа «место на карте мира»

kind: run · assignee: gemini (Antigravity) · afterTask: StoryStageHotel

Приёмка E2 (Claude, 20.11). Проба `Tests/InGameStoryStagesProbe.cs` покрывает только `Clear` и таймаут. На этапе `Site` построено около 70 этапов квестов, поэтому нужны **все** цели.

## Сделать
1. Расширь пробу. Для каждой цели — реальное действие и правильный исход:
   - `Rescue` — пленник-герой на карте места; переведи его в караван игрока и уведи → `success`. Отдельно: убей пленника → `failed`.
   - `Retrieve` — вещь на карте; положи в караван игрока и уведи → `success`.
   - `Hold` — `holdHours`=1: держи пешек на карте → `success`. Уход раньше времени → не success.
   - `KillHero` — герой из реестра среди врагов; убей его → `success`. Если герой ушёл с карты живым, это не success.
2. **Сравни одну и ту же сборку.** Повтори fresh и reload для `StoryStages` и `StoryEngine` **без** `-CompatibilityReload`: свежая проба и загрузка должны идти на одной DLL. Если reload требует флаг, найди и запиши причину.
3. Ничего не меняй в `Source/StoryStage_*.cs`, если всё проходит. Если цель работает неверно, исправь минимально и опиши в отчёте.

0 ERROR, не больше 2 попыток на шаг. Не коммитить. Сдача: `Tools/Build/GeminiStoryStagesCoverage/REPORT.md`.