# HeroRegistryStage1 — реестр именных героев, этап 1

kind: implement + run · assignee: codex · afterTask: OutpostShipmentCycles

Спецификация: `Documentation/HERO-REGISTRY-DESIGN.md`, раздел «Этап 1». Опасная зона — пешки, WorldPawns, Harmony, Scribe, поэтому задача твоя.

## Где писать
- Новые `Source/HeroDef.cs`, `Source/HeroRegistry.cs`, Harmony-патчи в новом `Source/HeroRegistryPatches.cs`.
- XML героев — `Defs/HeroDefs/Heroes_MinutemenHunters.xml`.
- Probe `Tests/InGameHeroRegistryProbe.cs` и строка компонента `HeroRegistry` в `Tools/test_component.ps1` (UTF-8 с BOM).
- Reload-скрипт — по образцу `Tools/test_outpost_calendar_reload.ps1`.
- Существующие файлы `Source/` не менять, кроме регистрации патчей, если она централизована. Если без правки чужого файла не обойтись — опиши в REPORT.

## Проверка
- `build_assembly.ps1`;
- HeroRegistry fresh + reload;
- регрессия Outposts fresh.

Только если RimWorld не запущен и никто другой не гоняет игру: проверь ACK Gemini. Не больше 4 попыток. Не коммитить — Claude примет и закоммитит.

## Сдача
`Tools/Build/CodexHeroRegistryStage1/REPORT.md`, ~60 строк:
- что сделано (`файл:строка`);
- таблица прогонов: passed, checks, runtimeErrors, unresolved, gpuLost, SHA;
- «Куда смотреть Claude»;
- что отложено.
