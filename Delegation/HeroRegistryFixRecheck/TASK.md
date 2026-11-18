# HeroRegistryFixRecheck — проверить, что F1–F3 закрыты

kind: accept · assignee: codex

Твоя приёмка `Tools/Build/CodexHeroRegistryAccept/REPORT.md` нашла F1–F3. Claude исправил их в коммите a340af3 и прогнал в игре: fresh 28/28, reload 10/10 за 2 дня, профиль `TestRuns/HeroRegistry-FullSet-20261118-164347`. В `HeroRegistryProbe.json` (facts) ванильное письмо называет героя-преемника.

**Только чтение, без игры и сборки, один короткий сеанс.**
- **F1:** `Classify` в `Source/HeroRegistry.cs` — MapHeld и караван; probe с криокапсулой.
- **F2:** префикс `Faction.TryGenerateNewLeader` → `SuccessorForVacantLeadership`. Нет ли рекурсии и двойного лидера? Что если префикс сработает в ванильной ситуации без героев (должен вернуть true)?
- **F3:** `Keep()` теперь передаёт пешку в режиме `Decide`. Нет ли сценария, где живой герой всё же теряется: GC до первого Refresh, или пешка, которую игра передаёт в мир с `Discard`?

## Сдача
`Tools/Build/CodexHeroRegistryFixRecheck/REPORT.md`, до 25 строк:
- вердикт: ЗАКРЫТО / НЕ ЗАКРЫТО по каждому F;
- новые находки, если есть.

ACK — в `Tools/Build/CodexWorkerState/HeroRegistryFixRecheck.json`.
