# FactionApparel — встроить одежду фракций и именные вещи героев от Neon

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: VehicleFramework

Код и XML написал Gemini Neon по промптам `Delegation/Hydrogen/39-Neon-FactionApparel-CODE-PROMPT.md` и `40-Neon-SignatureGear-CODE-PROMPT.md`. Файлы будут в `Delegation/StoryContent/Apparel/` и `Delegation/StoryContent/SignatureGear/`, пути повторяют корень мода. Если папки пусты, запиши `blocked: waiting for Neon apparel code`. Задача идёт после снаряжения: используются слои `TMM_PowerArmor`/`TMM_Backpack` и `CompApparelMods`.

## Сделать
Скопируй файлы, собери мод, исправь компиляцию и `// CHECK API` через `Tools/cecil_methods.ps1`. Логику Neon не меняй без причины, изменения перечисли в отчёте. Игра грузится с 0 ERROR, и без Ideology и Anomaly тоже.

## Проверка в игре (обязательна)
- Пробы Neon `FactionApparel` и `SignatureGear`, fresh.
- Регрессия: `Equipment`, `HeroRegistry` (герои в именных вещах), `AngelXenotype`, `SuperMutantXenotype`.
- 0 ERROR, не больше 2 попыток на шаг, `.ps1` только с BOM.

Не коммитить. Сдача: `Tools/Build/GeminiFactionApparel/REPORT.md`.