# Round2CoreRecheck — приёмка общего ядра откатов, только чтение

kind: accept · assignee: codex

Твои повторные приёмки E2, E3 и караванов (`Tools/Build/Codex{StoryStageSiteRecheck,StoryStageHotelRecheck,OutpostCaravanRecheck}/REPORT.md`) показали одну общую причину: откаты при сбое посреди операции. Neon переписал общее ядро, этапы переведены на него, Claude встроил всё в корень (сборка SHA256 `a989f43e9b1a0ac6bd46255a704f9068e7543960acee11c32be5c6451a197705`).

Прогоны на этой сборке: StoryStages fresh 15/15 и reload, StoryHotel fresh 24/24 и reload, StoryEngine 34/34. Везде 0 ошибок и 0 нерешённых ссылок, тестовый режим включён. Караваны пока не приняты: проба нестабильна по таймингу, этот код в эту приёмку не входит.

## Проверить (только ядро)
- `Source/StoryRelationRestorer.cs` — симметричное восстановление обеих сторон с уведомлениями. Backup удаляется только после проверки. Счётчик аренды перемирия. Откат несостоявшегося рейда независимо от `restoreRelations`.
- `Source/StoryPayment.cs` — журнал до каждой мутации, проверенный откат, holder для невозвращённого, необратимое — последним.
- `Source/StoryPawnTransfer.cs` — снимок и полный откат; снятие с WorldPawns — последним.
- `Source/OutpostPodLogistics.cs` — нет обхода старым циклом после отказа.
- `Source/TestMode.cs` и все тестовые хуки — работают только при `-tmm-story-test`.
- Правки Claude:
  - `TestMode.ReportException`: внедрённые исключения с сообщением `Simulated…` пишутся как warning только в тестовом режиме;
  - `ClearAllRelations` перед уничтожением сгенерированных пешек в `StoryStage_HotelVisit`;
  - `ValidateColonyHandIn` сделан public.

Достаточно пройти по прошлым находкам ядра: закрыта, частично, не закрыта. Новые находки — только P1.

**Только чтение. Игру и сборку не запускать.** Cecil — только через `Tools/cecil_methods.ps1`. Бюджет около 2%. Вердикт: «ПРИНЯТЬ» / «ПРИНЯТЬ С ПРАВКАМИ» / «НЕ ПРИНИМАТЬ».

Отчёт: `Tools/Build/CodexRound2CoreRecheck/REPORT.md`. Ack: `Tools/Build/CodexWorkerState/Round2CoreRecheck.json`.