# StoryEngineReview — приёмка ядра сюжетного движка (только чтение)

kind: accept · assignee: codex

Ядро сюжета (E1) лежит в коммите `ec407c9` и предыдущем `33985de`. Файлы:
- `Source/SparkStory.cs`
- `Source/StoryDefs.cs`
- `Source/StoryLetter.cs`
- `Source/MainTabWindow_Chronicle.cs`
- `Defs/StoryDefs/*.xml`
- проба `Tests/InGameStoryEngineProbe.cs`

Дизайн: `Documentation/STORY-ENGINE-DESIGN.md`. На этом ядре будут 75 квестов фракций и главный сюжет на 50+ часов, поэтому ошибку в сохранении или в жизненном цикле потом очень дорого исправлять.

**Только чтение кода. Игру и сборку не запускать, корень не править.** Cecil по сигнатурам игры — только через `Tools/cecil_methods.ps1 -Type ...`.

## Что проверить
1. **Сохранение.**
   - `SparkStory.ExposeData` и `StoryQuestRun.ExposeData`: что будет при удалённом `StoryQuestDef`, при пустых коллекциях, при загрузке старого сейва без компонента.
   - `StoryChoiceLetter.ExposeData`: письмо в стопке писем сохраняется. Что будет после загрузки, если квест уже завершён или Def удалён.
2. **Жизненный цикл письма.**
   - Может ли квест зависнуть? Например, письмо удалено через `Removed`, а квест остался в `Offered` или `pendingChoice`.
   - Может ли выбор сработать дважды? Двойной клик, повторное открытие архивного письма, `Choices` у архивного письма.
   - Корректно ли `RemoveLetter` вызывается изнутри действия `DiaOption`.
3. **Режиссёр.**
   - `DirectorTick`: может ли autoStart-квест стартовать повторно?
   - Может ли `Decline` с кулдауном потерять квест навсегда?
   - Что будет при `MaxActive` и `main`?
   - Нет ли тяжёлого LINQ каждые 250 тиков при 100+ Def. Оцени стоимость.
4. **Условия** (`StoryConditions.Eval`): разбор операторов. Найди строки, которые разберутся неправильно, например флаг с `=` в значении, `act>=2`, пробелы, `!done:x`.
5. **Сочетание с остальным модом.** Патч `HeroRegistry` на `LetterStack.ReceiveLetter` и наши письма. `StoryEffect_HeroJoins` с героем, который сейчас на карте, в караване или в плену.
6. **Расширение E2/E3.** Хватит ли этапам места на карте и гостиницы текущего API? В задаче E2 им запрещено менять ядро. Claude собирается добавить в `StoryQuestRun` словарь `vars` (string→string) и тестовый `SparkStory.ForceOutcome(run, outcome)`. Оцени, достаточно ли этого и безопасен ли такой хук.

## Сдача
`Tools/Build/CodexStoryEngineReview/REPORT.md`: «ПРИНЯТЬ», «ПРИНЯТЬ С ПРАВКАМИ» или «НЕ ПРИНИМАТЬ». Для каждой находки: файл:строка, сценарий, как воспроизвести и минимальная правка. Без общих советов. Ack — `Tools/Build/CodexWorkerState/StoryEngineReview.json`.
