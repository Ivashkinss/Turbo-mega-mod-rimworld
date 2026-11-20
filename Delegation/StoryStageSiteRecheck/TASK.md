# StoryStageSiteRecheck — повторная приёмка: E2 (место и рейд на колонию), только чтение

kind: accept · assignee: codex

Ты не принял эту работу: отчёт `Tools/Build/CodexStoryStageSiteReview/REPORT.md`. Neon переписал код по каждой находке, Antigravity встроил его и прогнал пробы. Его отчёт: `Tools/Build/GeminiStoryStageSiteFix/REPORT.md`. Код: `Source/StoryStage_Site.cs`, `Source/StoryStage_ColonyRaid.cs`, `Source/StoryRelationRestorer.cs`, `Source/SparkStory.cs`, `Source/StoryLetter.cs`, `Tests/InGameStoryStagesProbe.cs`.

## Проверить
1. Каждую находку P1/P2 из твоего прошлого отчёта: закрыта, закрыта частично или не закрыта. По каждой — файл:строка.
2. Не появилось ли новых ошибок того же класса: перенос пешек без проверки владельца, ложный успех, потеря или задвоение вещей, рассинхрон после reload, тестовые хуки в игровом коде.
3. Проба проверяет реальные сценарии, а не подставленные результаты.

Если Antigravity ещё не сдал задачу (нет ack `done`), запиши `blocked`.

**Только чтение. Игру и сборку не запускать, корень не править.** Cecil — только через `Tools/cecil_methods.ps1`. Вердикт: «ПРИНЯТЬ» / «ПРИНЯТЬ С ПРАВКАМИ» / «НЕ ПРИНИМАТЬ». Коротко: бюджет около 2%, старые находки повторно не расписывай.
Отчёт — `Tools/Build/CodexStoryStageSiteRecheck/REPORT.md`, ack — `Tools/Build/CodexWorkerState/StoryStageSiteRecheck.json`.