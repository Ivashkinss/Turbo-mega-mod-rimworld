# OutpostShipmentCycles — реальная отправка товаров аванпоста в колонию

kind: run · assignee: codex

Код и probe можно писать сразу. Игру запускать только после того, как `Tools/Build/GeminiWorkerState/OutpostSanitizerNarrow.json` станет `done` или `error`, и только если RimWorld не запущен: одновременно идёт один игровой прогон. Если за сеанс не успел — запиши в ACK `running` с тем, что сделано, и продолжи в следующий опрос.

Не проверено ни разу: отправка накопленной продукции в колонию (`targetColonyTile >= 0`) за несколько игровых дней. Задача — только тест. `Source/` не менять, если не найден баг; если найден — остановиться и описать в REPORT с `файл:строка`.

## Что сделать
1. Прочитай код отправки в `Source/` (найди по `targetColonyTile`): когда создаётся отправка, что везёт, куда и как доставляется (караван, дроп-под или мгновенно), что происходит с запасом аванпоста.
2. Новый probe `Tests/InGameOutpostShipmentProbe.cs`, компонент `OutpostShipment` в `Tools/test_component.ps1` (UTF-8 с BOM) и reload-скрипт `Tools/test_outpost_shipment_reload.ps1` — по образцу OutpostCalendar.
   - Fresh:
     - аванпост-шахта, 2 шахтёра с известным навыком, цель — домашняя колония;
     - ждать реальные `TicksGame` 3 производственных дня;
     - проверить: запас аванпоста уменьшился ровно на отправленное; в колонии появилось ровно столько же нужного def (до и после, по `map.listerThings` или `resourceCounter`); ничего не задвоено и не потеряно;
     - отправка без цели (`targetColonyTile = -1`) копит запас;
     - цель — неверный или удалённый тайл: без исключений, товар не теряется.
   - Reload: сохранить посередине между отправками, загрузить, дождаться следующей, сверить те же суммы. Груз в пути после загрузки не задваивается и не пропадает.
   - Ожидаемые числа — константы, посчитанные вручную по формуле (`floor(totalSkill × Rate / productCount + остаток)`). Расчёт оставь в комментарии.
3. Прогон в изолированных TestRuns, только если RimWorld не запущен: OutpostShipment fresh + reload, плюс регрессия Outposts fresh. Не больше 4 попыток.

## Разрешено менять в корне
Только новые `Tests/InGameOutpostShipmentProbe.cs`, `Tools/test_outpost_shipment_reload.ps1`, одну строку в `Tools/test_component.ps1` и `Documentation/OutpostShipment-*.json`. Не коммитить.

## Сдача
`Tools/Build/CodexOutpostShipmentCycles/REPORT.md` в формате COORDINATION:
- как работает отправка, с `файл:строка`;
- таблица «день: запас, отправлено, получено»;
- все прогоны с полями passed, checks, runtimeErrors, unresolvedReferences, gpuLost и SHA;
- «Куда смотреть Claude»;
- самоаудит.
