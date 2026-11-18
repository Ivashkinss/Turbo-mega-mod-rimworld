# MinutemenFixes — исправить три ошибки прототипа «Минитменов» в кандидате

kind: implement + run · assignee: gemini · afterTask: XenotypeArtPilot

Основа — твой кандидат `Tools/Build/GeminiMinutemenRebase/Candidate/` и отчёт `Tools/Build/GeminiMinutemenRebase/REPORT.md`. Корень не трогать.

## Исправить
1. `TMM_HuntersHunter`: config error «weaponMoney min 400, cheapest weapon 1160». Подними `weaponMoney` так, чтобы минимум покрывал самое дешёвое оружие по его `weaponTags`, или смени теги на более дешёвые. Не убирай оружие.
2. Русская DefInjected-локализация квестов: `questNameRules.rulesStrings` инжектится в несуществующий узел. Либо добавь `questNameRules` с `rulesStrings` в английские QuestScriptDef, либо убери эти строки из DefInjected — смотря что реально используется кодом квеста (`quest.name` задаётся в C#).
3. Клиника форпоста: probe не нашёл медицинских коек (`InGameMinutemenHuntersProbe.cs:100-101`, `GenStep_MinutemenOutpost.cs:72`).
   - Выясни причину: койки не спавнятся; спавнятся, но `Medical=false`; или probe ищет через `AllBuildingsColonistOfClass`, а койки чужой фракции. Исправь настоящую причину.
   - Если ошибка в самом probe — исправь поиск: койки на карте форпоста любой фракции, `def.building.bed_humanlike` и `Medical`.
   - Ожидание «≥ 2 медицинских коек» не снижать.

## Прогон
Пересобрать кандидат и один раз прогнать его probe в игре тем же способом, что в MinutemenRebaseRun (временная подмена, восстановление корня). Только если RimWorld не запущен и в `Tools/Build/CodexWorkerState/` нет задач `running`. Не больше 2 попыток.

## Сдача: `Tools/Build/GeminiMinutemenFixes/REPORT.md`
- три правки: `файл:строка`, было → стало;
- результат прогона: passed, все checks, runtimeErrors, config errors из лога (должно быть 0), LanguageErrors;
- подтверждение, что корень не изменился;
- самоаудит.
