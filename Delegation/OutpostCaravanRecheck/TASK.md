# OutpostCaravanRecheck — повторная приёмка: пешие караваны к аванпостам, только чтение

kind: accept · assignee: codex

Ты не принял эту работу: отчёт `Tools/Build/CodexOutpostCaravanReview/REPORT.md`. Neon переписал код по каждой находке, Antigravity встроил его и прогнал пробы. Его отчёт: `Tools/Build/GeminiOutpostCaravanFix/REPORT.md`. Код: `Source/OutpostCaravanLogistics.cs`, `Source/Dialog_OutpostCaravan.cs`, методы `AddPawnFromCaravan`/`RemovePawnToCaravan` в `Source/StrategicOutpost.cs`, `Tests/InGameOutpostCaravanProbe.cs`.

## Проверить
1. Каждую находку P1/P2 из твоего прошлого отчёта: закрыта, закрыта частично или не закрыта. По каждой — файл:строка.
2. Не появилось ли новых ошибок того же класса: перенос пешек без проверки владельца, ложный успех, потеря или задвоение вещей, рассинхрон после reload, тестовые хуки в игровом коде.
3. Проба проверяет реальные сценарии, а не подставленные результаты.

Если ack `Tools/Build/GeminiWorkerState/OutpostCaravanFix.json` не `done`, запиши `blocked`.

**Только чтение. Игру и сборку не запускать, корень не править.** Cecil — только через `Tools/cecil_methods.ps1`. Вердикт: «ПРИНЯТЬ» / «ПРИНЯТЬ С ПРАВКАМИ» / «НЕ ПРИНИМАТЬ». Коротко: бюджет около 2%, старые находки повторно не расписывай.
Отчёт — `Tools/Build/CodexOutpostCaravanRecheck/REPORT.md`, ack — `Tools/Build/CodexWorkerState/OutpostCaravanRecheck.json`.