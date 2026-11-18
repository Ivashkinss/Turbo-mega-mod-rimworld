# MinutemenRebaseRun — пересобрать прототип «Минитменов» на текущем корне и прогнать в игре

kind: run · assignee: gemini · afterTask: OutpostSanitizerNarrow

Прототип `Tools/Build/GeminiMinutemenHuntersPrototype/` собран 17.11 на старом корне (DLL 400 КБ, сейчас 494 КБ) и ни разу не запускался в игре. Задача механическая: ничего не проектировать и не улучшать.

## Шаги
1. Новый каталог `Tools/Build/GeminiMinutemenRebase/Candidate/`:
   - копия текущих корневых `Source/`, `Defs/`, `Languages/`, `Tests/`;
   - поверх — файлы из `Tools/Build/GeminiMinutemenHuntersPrototype/Candidate/`. Все они новые, в корне таких нет: если имя совпало — стоп и `error`.
2. Собрать DLL кандидата (по образцу `build_candidate.ps1` прототипа, но от новой копии), exit 0. Корневой `Assemblies/` не трогать.
3. Игровой прогон, только если RimWorld не запущен, в изолированном TestRuns-профиле:
   - временно подложить DLL кандидата и его Defs и Languages по образцу `Tools/test_component.ps1`;
   - запустить probe `InGameMinutemenHuntersProbe`;
   - после прогона вернуть корень в исходное состояние: `git status` до и после совпадает, `Assemblies/TurboMegaMod.TestProbe.dll` отсутствует, SHA `Assemblies/TurboMegaMod.dll` совпадает с исходным.
   - Если probe требует особого запуска, опиши, как запускал.
4. Ничего не чинить. Только записать каждую ошибку: текст из `Player.log`, `файл:строка`.

Не больше 2 попыток запуска. Корень не менять, не коммитить.

## Сдача: `Tools/Build/GeminiMinutemenRebase/REPORT.md`
- сборка: exit code, SHA;
- прогон: profile, passed, checks (каждый check с итогом), runtimeErrors, unresolvedReferences, gpuLost;
- список ошибок из лога дословно;
- подтверждение, что корень после прогона идентичен исходному;
- самоаудит.
