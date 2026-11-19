# LegendaryItems — встроить 15 легендарок и буру от Neon

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: SupernaturalGenes

Код и XML написал Gemini Neon по промпту `Delegation/Hydrogen/34-Neon-Legendaries-CODE-PROMPT.md`. Файлы лежат в `Delegation/StoryContent/Legendary/`, пути повторяют корень мода. Если папка пуста, запиши `blocked: waiting for Neon legendary code`. Легендарки используют типы урона из генов (`TMM_HolyWater`, `TMM_RockSalt`, `TMM_Enochian`, `TMM_BoraxBurn`), поэтому задача идёт **после SupernaturalGenes**.

## Сделать
1. Скопируй файлы по путям, собери мод. Исправь ошибки компиляции и строки `// CHECK API`: сверяй через `Tools/cecil_methods.ps1`, только из файла. Логику Neon не меняй без причины, каждое изменение перечисли в отчёте.
2. Игра грузится без ошибок конфигурации XML: в stdout.log 0 ERROR. Проверь отдельно, что без DLC Anomaly Def тоже грузятся: посмотри `MayRequire`.

## Проверка в игре (обязательна)
- Проба Neon `Tests/InGameLegendaryProbe.cs`, компонент `Legendary`: fresh проходит.
- Регрессия: `SupernaturalGenes`, `LeviathanXenotype`, `HeroRegistry` (Дик Роман и его Hediff неуязвимости), `StoryEngine`.
- Правила те же: 0 ERROR, строки «Error while instantiating a mod» в логе нет, не больше 2 попыток на шаг, C# 5, `.ps1` только с BOM.

Не коммитить. Сдача: `Tools/Build/GeminiLegendaryItems/REPORT.md`.
