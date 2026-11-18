# MinutemenIntegrate — внести фракцию «Минитмены и охотники» в корень и связать с героями

kind: integrate + run · assignee: gemini · afterTask: OutpostsProbeFlakyLevel2

Кандидат принят Claude: `Tools/Build/GeminiMinutemenRebase/Candidate/`, прогон `TestRuns/MinutemenRebase-FullSet-20261118-171817` — 36/36, 0 ERROR в логе.

## Перенос в корень
Только новые файлы прототипа:
- 7 `.cs` в `Source/`;
- Defs фракции: FactionDefs, PawnKindDefs, QuestScriptDefs, ThingDefs, WorldDefs;
- Languages EN/RU: Keyed и DefInjected;
- `Tests/InGameMinutemenHuntersProbe.cs`;
- строка компонента `MinutemenHunters` в `Tools/test_component.ps1` (UTF-8 с BOM).

Существующие файлы корня не менять, кроме перечисленного ниже. Если имя совпало — стоп и `error`.

## Связь с героями (`Defs/HeroDefs/Heroes_MinutemenHunters.xml` уже ссылается на `TMM_MinutemenHuntersFaction`)
Теперь фракция существует, и `GetOrCreate(TMM_Hero_preston_garvey)` вернёт настоящую пешку. Поэтому в `Tests/InGameHeroRegistryProbe.cs`:
- проверки `AbsentFactionGivesNull` и `AbsentFactionCreatesNoRecord` переделай на героя с несуществующей фракцией. Добавь в `HeroRegistryProbeLaunch` тестового героя с `factionDef = "TMM_NoSuchFaction"`;
- добавь проверку: Престон создаётся, его фракция — `TMM_MinutemenHuntersFaction`, и он лидер этой фракции (`faction.leader`).

Больше в героях ничего не менять.

## Прогон
Только если RimWorld не запущен; каждый прогон с таймаутом 15 минут.
- `build_assembly.ps1`;
- MinutemenHunters fresh;
- HeroRegistry fresh + reload;
- Outposts fresh — регрессия.

Пройдено, только если `passed: true` **и** в stdout.log 0 строк `ERROR` или `CRIT`. Не больше 3 попыток. При неуспехе откатить корень (`git checkout --` и удаление новых файлов), пересобрать DLL и записать `error`.

Не коммитить.

## Сдача: `Tools/Build/GeminiMinutemenIntegrate/REPORT.md`
В формате COORDINATION:
- список перенесённых файлов;
- дифф probe героев;
- таблица прогонов: passed, checks, ERROR-строк, unresolved, gpuLost, SHA;
- самоаудит.
