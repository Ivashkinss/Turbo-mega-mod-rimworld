# EquipmentSlots — встроить систему снаряжения от Neon

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: LegendaryItems

Дизайн: `Documentation/EQUIPMENT-SLOTS-DESIGN.md`. Код пишет Gemini Neon (`Delegation/Hydrogen/35-Neon-EquipmentSlots-CODE-PROMPT.md`), файлы будут в `Delegation/StoryContent/Equipment/` по путям корня. Если папка пуста, запиши `blocked: waiting for Neon equipment code`. Задача идёт после LegendaryItems: на Кольт навешивается счётчик серебряных пуль.

## Сделать
1. Скопируй файлы, собери, исправь компиляцию и `// CHECK API` через `Tools/cecil_methods.ps1`. Логику Neon не меняй без причины, изменения перечисли в отчёте.
2. Повесь `CompColtAmmo` на `TMM_Legendary_TheColt`, если Neon оставил это патчу.
3. Игра грузится без ошибок конфигурации XML.

## Проверка в игре (обязательна)
- Проба Neon `Tests/InGameEquipmentProbe.cs`, компонент `Equipment`: fresh и reload проходят.
- Регрессия: `Legendary`, `SupernaturalGenes`, `SimpleSidearms`, `MeleeAnimation`, `RunAndGun`. Встроенные моды, которые трогают оружие и инвентарь, не должны сломаться.
- 0 ERROR, не больше 2 попыток на шаг, C# 5, `.ps1` только с BOM.

Не коммитить. Сдача: `Tools/Build/GeminiEquipmentSlots/REPORT.md`. В отчёте приложи скриншот вкладки «Снаряжение», если пробе удастся его сохранить: тот же способ, что у портретов в пробе ксенотипов.