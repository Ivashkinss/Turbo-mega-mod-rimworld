# ComponentErrorsSweep — ошибки из игровой сессии пользователя 18.11

kind: implement + run · assignee: gemini (Antigravity)

Пользователь играл в обычной (не тестовой) игре и прислал ошибки. Найди причину каждой и исправь, если она в нашем коде или в интегрированном компоненте внутри мода (`Components/`, `Source/`). Ванильный и сторонний код вне мода не трогать. Если ошибка в чужом моде, который не интегрирован, опиши и не трогай.

1. `VEF.Pawns.WorkGiver_ConstructionSkill_DeliverResourcesToBlueprints provided target Thing_Blueprint_Wall... but yielded no actual job for pawn Harry. The CanGiveJob and JobOnX methods may not be synchronized.` (ErrorOnce, из JobGiver_Work). Это Vanilla Expanded Framework. Выясни, интегрирован ли он у нас, и почему `HasJobOnThing` даёт true, а `JobOnThing` — null для чертежа стены. Например, не хватает ресурсов, требуется навык или резервирование.
2. `Invalid count: 0, setting to 1. Job was HaulToInventory ... Giver = JobGiver_Work [workGiverDef: HaulToInventory]` — Pick Up And Haul, `JobDriver_HaulToInventory`, шаг `ErrorCheckForCarry`. Повторялась несколько раз с рисом и брёвнами.
3. После старта игры на карте мира в правом верхнем углу осталась пустая панель с шестерёнкой — судя по всему, превью карты мода MapPreview. Она должна быть только на экране выбора места и не оставаться в игре. Найди, где она открывается и почему не закрывается.
4. «Фракции подтянулись, но не у всех есть поселения». Проверь в новом мире: у каждой нашей фракции (Factions_*.xml, `settlementGenerationWeight` 0.3–0.4, `requiredCountAtGameStart` 1) есть хотя бы 2–3 поселения, включая столицу. Посмотри, как `Source/RegionalSettlements.cs` распределяет базы. Если какие-то фракции остаются без поселений — исправь распределение, чтобы у каждой видимой фракции было минимум 2.

Для каждого пункта: причина (`файл:строка`), исправление, проверка. Для 4 — отдельный быстрый probe: новый мир, подсчёт поселений по фракциям. Прогоны только если RimWorld не запущен, таймаут 15 минут, не больше 2 попыток на пункт.

Не коммитить. Сдача: `Tools/Build/GeminiComponentErrorsSweep/REPORT.md` в формате COORDINATION.