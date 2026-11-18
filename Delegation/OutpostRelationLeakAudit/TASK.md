# OutpostRelationLeakAudit — откуда берётся битая ссылка Lover в календарном тесте

kind: run · assignee: gemini · afterTask: OutpostBalanceApply

В OutpostMapsIntegration (попытка 4) reload календаря дал `unresolvedReferences: 1`: у сгенерированного NPC была прямая связь `Lover` к пешке, которой нет в сохранении. Ты добавил санитайзер в `Tests/InGameOutpostCalendarProbe.cs:446-479`. Он удаляет такие связи перед `SaveGame`. Это может скрывать настоящий баг мода: работник аванпоста, которого держит holder, может пропасть из `WorldPawns` или быть discarded.

## Что сделать (только диагностика, код `Source/` не менять)
1. Временно (во временной копии probe `Tools/Build/GeminiOutpostRelationLeakAudit/Probe/`, корневой файл не трогать) отключи удаление связей. Вместо этого перед `SaveGame` запиши в отчёт по каждой битой связи:
   - кто владелец (имя, faction, где находится: карта, WorldPawns, holder аванпоста);
   - на кого ссылка: имя, `thingIDNumber`, `Destroyed`, `Discarded`, `Spawned`, `Faction`, `IsWorldPawn()`, `Find.WorldPawns.GetSituation(p)`, лежит ли в holder какого-либо аванпоста (`Workers` / `Animals`).
2. Прогони календарь fresh 2–3 раза в изолированных TestRuns (только если RimWorld не запущен), собери статистику.
3. Вывод, один из двух:
   - **A.** Это ванильный артефакт генерации: пешка-цель не связана с аванпостами, никогда не была в holder и стёрта ванильным GC или генератором.
   - **B.** Цель — пешка аванпоста или прошла через `DeployPersonnelToMap` / `RecallPersonnelToAutonomous` / holder. Тогда это баг мода: укажи `файл:строка`, где она теряется. Не чини.
4. Если A — предложи, как сузить санитайзер: удалять только связи к пешкам, которые никогда не были в аванпосте, и логировать удалённое.

## Сдача
`Tools/Build/GeminiOutpostRelationLeakAudit/REPORT.md` в формате COORDINATION: вывод A/B с доказательствами из JSON прогонов, профили, «Куда смотреть Claude», самоаудит. Корень не менять, не коммитить.
