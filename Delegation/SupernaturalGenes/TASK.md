# SupernaturalGenes — встроить 18 новых сверхъестественных генов от Neon

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: StoryContentFactions

Код и XML написал Gemini Neon по промпту `Delegation/Hydrogen/33-Neon-SupernaturalGenes-CODE-PROMPT.md`. Готовые файлы лежат в `Delegation/StoryContent/Genes/`, пути повторяют корень мода. Если папка пуста, запиши `blocked: waiting for Neon genes code` и бери следующую задачу.

## Сделать
1. Скопируй файлы по путям. `Defs/GeneDefs/Genes_Metabolism.xml` заменяет текущий.
2. Собери мод. Исправь ошибки компиляции и все строки `// CHECK API`: сверяй через `Tools/cecil_methods.ps1 -Type <тип> [-Name <метод>]`, только из файла и никогда через `powershell -Command`. Логику Neon не меняй без причины, каждое изменение перечисли в отчёте.
3. Игра грузится без ошибок конфигурации XML: в stdout.log 0 ERROR.

## Проверка в игре (обязательна)
- Проба Neon `Tests/InGameSupernaturalGenesProbe.cs`, компонент `SupernaturalGenes` в `Tools/test_component.ps1`: fresh проходит.
- Регрессия: `AngelXenotype`, `DemonXenotype`, `SuperMutantXenotype`, `GhoulXenotype`, `LeviathanXenotype`. Их проверки `MetabolismNeutral` и `XenotypeMetabolismNeutral` должны остаться зелёными.
- Затем `StoryEngine` (fresh и reload) и `HeroRegistry`: герои с ксенотипами не должны сломаться.
- Правила те же: 0 ERROR, строки «Error while instantiating a mod» в логе нет, не больше 2 попыток на шаг, C# 5, `.ps1` только с BOM.

Не коммитить. Сдача: `Tools/Build/GeminiSupernaturalGenes/REPORT.md`. В отчёте перечисли, что исправлено в коде Neon и какие гены упрощены.
