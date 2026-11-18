# OutpostSanitizerNarrow — сузить чистку связей в тестах

kind: implement + run · assignee: gemini · afterTask: OutpostRelationLeakAudit

Итог аудита (Claude по `Tools/Build/GeminiOutpostRelationLeakAudit/RUN-1..3.json`) — вывод A.
- Битые прямые связи — это ExLover, ExSpouse и Lover между NPC чужих фракций.
- Цель такой связи: не world pawn, не destroyed, не discarded, `everInOutpost=false`.
- Записей `relationshipRecords` с битыми ссылками — 0 во всех трёх прогонах.

Отчёт по аудиту можно не дописывать.

## Что сделать (только тесты)
1. Удалить полную очистку `relationshipRecords.records` из `Tests/InGameOutpostCalendarProbe.cs`. Если она есть в `Tests/InGameStrategicOutpostsProbe.cs` и `Tests/InGameOutpostMapsProbe.cs` — удалить и там.
2. Санитайзер прямых связей в `Tests/InGameOutpostCalendarProbe.cs` (и копии в других probe, если есть):
   - удалять связь, только если целевая пешка никогда не была в holder аванпоста и не принадлежит игроку;
   - иначе — `Check(... false)` с именем пешки: тест обязан упасть;
   - каждую удалённую связь записать в факты отчёта: relation, owner, target, faction цели.
3. Прогнать OutpostCalendar fresh+reload, Outposts fresh+reload и OutpostMaps fresh+reload на текущей DLL. Только если RimWorld не запущен. Не больше 3 попыток.

Не трогать `Source/`, не коммитить. Сдача: `Tools/Build/GeminiOutpostSanitizerNarrow/REPORT.md` в формате COORDINATION: диффы, удалённые связи по прогонам, таблица прогонов, самоаудит.
