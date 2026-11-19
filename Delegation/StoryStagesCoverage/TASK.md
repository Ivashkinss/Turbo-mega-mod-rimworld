# StoryStagesCoverage — встроить исправления E2 по ревью Codex и проверить все цели

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: StoryStageHotel

Codex не принял E2 (`Tools/Build/CodexStoryStageSiteReview/REPORT.md`): нашёл 5 блокирующих P1 и 2 P2. Исправления написал Gemini Neon по промпту `Delegation/Hydrogen/41-Neon-StoryStageSite-FIX-PROMPT.md`. Файлы будут в `Delegation/StoryContent/E2fix/`, пути повторяют корень мода. Если папка пуста, запиши `blocked: waiting for Neon E2 fix`.

## Сделать
1. Скопируй `Source/StoryStage_Site.cs`, `StoryStage_ColonyRaid.cs`, `StoryRelationRestorer.cs`, `SparkStory.cs`, `StoryLetter.cs` и `Tests/InGameStoryStagesProbe.cs`.
2. Метод `ConfigErrors` из `Source/StoryDefs.ConfigErrors.txt` вставь вместо текущего `StoryQuestDef.ConfigErrors` в `Source/StoryDefs.cs`. Остальное в `StoryDefs.cs` не трогай.
3. Собери мод, исправь компиляцию и `// CHECK API` через `Tools/cecil_methods.ps1`. Логику Neon не меняй без причины, все изменения перечисли в отчёте.
4. **Одна DLL на все прогоны:** после последней правки исходников пересобери мод один раз и гоняй всё на ней. Флаг `-CompatibilityReload` **запрещён**.

## Проверка в игре (обязательна)
- `StoryStages` fresh и reload: все сценарии новой пробы Neon, по каждой находке Codex.
- `StoryEngine` fresh и reload: ядро изменилось.
- `StoryHotel` (E3) fresh: должен остаться зелёным.
- `RegionalWorld`.
- 0 ERROR, не больше 2 попыток на шаг, `.ps1` только с BOM.

Не коммитить. Сдача: `Tools/Build/GeminiStoryStagesCoverage/REPORT.md` с SHA256 итоговой DLL, такой же, как в Result.json всех прогонов.