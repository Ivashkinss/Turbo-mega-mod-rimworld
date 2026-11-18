# HeroRegistryCodexAccept — независимая приёмка реестра героев (опасная зона)

kind: accept · assignee: codex

Claude написал и закоммитил этап 1 реестра героев (коммит 6bc1dc0). Он прошёл в игре: fresh 21/21, reload 9/9, профиль `TestRuns/HeroRegistry-FullSet-20261118-151509`. Нужен твой взгляд на опасные места. **Только чтение и короткий отчёт, без игровых прогонов**: экономь лимит, уложись в один короткий сеанс.

## Что смотреть
- `Source/HeroRegistry.cs`:
  - сериализация `HeroRecord`: обнуление ссылки на Discarded-пешку при сохранении — нет ли битых ссылок или потери живой пешки;
  - `Keep()` и условие в `Refresh()` для повторной передачи в WorldPawns: не возникнет ли двойной `PassToWorld` или потеря пешки, которая лежит в holder (капсула, транспорт, контейнер)?
  - `Classify()`: правильны ли состояния для пешки в караване, на чужой карте, в капсуле, в криптосне;
  - `OnHeroGone()`: преемник лидера. Конфликт с ванильным `Faction.Notify_LeaderDied`, рекурсия через `GetOrCreate`.
- Постфикс `WorldPawnGC.GetCriticalPawnReason`: сигнатура в 1.6 и 1.7, влияние на производительность.
- `Tests/InGameHeroRegistryProbe.cs`: нет ли фиктивных PASS.

## Сдача
`Tools/Build/CodexHeroRegistryAccept/REPORT.md`, до 40 строк:
- вердикт: ПРИНЯТЬ / ПРИНЯТЬ С ПРАВКАМИ / ОТКЛОНИТЬ;
- находки F1…Fn: `файл:строка` и сценарий отказа;
- без правок корня.

ACK — в `Tools/Build/CodexWorkerState/HeroRegistryCodexAccept.json`.
