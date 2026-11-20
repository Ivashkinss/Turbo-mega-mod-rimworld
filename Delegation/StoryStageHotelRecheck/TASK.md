# StoryStageHotelRecheck — повторная приёмка: E3 (гостиница и доставка), только чтение

kind: accept · assignee: codex

Ты не принял эту работу: отчёт `Tools/Build/CodexStoryStageHotelReview/REPORT.md`. Neon переписал код по каждой находке, Antigravity встроил его и прогнал пробы. Его отчёт: `Tools/Build/GeminiStoryHotelFix/REPORT.md`. Код: `Source/StoryStage_HotelVisit.cs`, `Source/StoryStage_Deliver.cs`, `Source/StoryPayment.cs`, `Source/StoryRelationRestorer.cs`, `Source/OutpostPodLogistics.cs`, `Tests/InGameStoryHotelProbe.cs`.

## Проверить
1. Каждую находку P1/P2 из твоего прошлого отчёта: закрыта, закрыта частично или не закрыта. По каждой — файл:строка.
2. Не появилось ли новых ошибок того же класса: перенос пешек без проверки владельца, ложный успех, потеря или задвоение вещей, рассинхрон после reload, тестовые хуки в игровом коде.
3. Проба проверяет реальные сценарии, а не подставленные результаты.

Если ack `Tools/Build/GeminiWorkerState/StoryHotelFix.json` не `done`, запиши `blocked`. Отдельно проверь: `StoryPayment` действительно атомарен, а откат перемирия в `StoryRelationRestorer` не ломает откат рейда E2.

**Только чтение. Игру и сборку не запускать, корень не править.** Cecil — только через `Tools/cecil_methods.ps1`. Вердикт: «ПРИНЯТЬ» / «ПРИНЯТЬ С ПРАВКАМИ» / «НЕ ПРИНИМАТЬ». Коротко: бюджет около 2%, старые находки повторно не расписывай.
Отчёт — `Tools/Build/CodexStoryStageHotelRecheck/REPORT.md`, ack — `Tools/Build/CodexWorkerState/StoryStageHotelRecheck.json`.