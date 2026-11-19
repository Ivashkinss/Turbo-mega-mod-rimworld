# StoryStageHotelReview — приёмка E3 (гостиница и доставка), только чтение

kind: accept · assignee: codex

Antigravity встроил код Neon для этапов сюжета `StoryStage_HotelVisit` и `StoryStage_Deliver`. Отчёт: `Tools/Build/GeminiStoryStageHotel/REPORT.md`. Код: `Source/StoryStage_HotelVisit.cs`, `Source/StoryStage_Deliver.cs`, проба `Tests/InGameStoryHotelProbe.cs`. Исходный вариант Neon лежит в `Delegation/StoryContent/E3/`. Hospitality подключается через рефлексию, образец — `Source/HotelVisitGuarantee.cs`.

## Проверить
1. **Hospitality через рефлексию:**
   - нет мода, приём закрыт или гостиницы нет — исход `noHotel`, без исключений и без вечного ожидания;
   - группа гостей найдена после загрузки;
   - герой из реестра в группе — не создаётся ли второй экземпляр.
2. **Цели:**
   - `Host` — гость не мог уйти во вражде, но с засчитанным успехом;
   - `Investigate` — нельзя засчитать без настоящей беседы;
   - `Hide` — облава засчитывается правильно.
3. **Deliver:**
   - нет задвоения и потери вещей: частичное списание, отказ, вещи в контейнерах и на чужой карте;
   - `giverSettlement` — списание из нужного каравана на нужной клетке;
   - таймаут.
4. **Сохранение** посреди проживания и доставки: этап завершается ровно один раз.
5. **Совместимость с исправлением E2** (`Delegation/StoryContent/E2fix/`): нет ли в E3 тех же ошибок, что Codex нашёл в E2 — перенос героев без проверки владельца, ложный успех, тестовые хуки в игровом коде.

**Только чтение. Игру и сборку не запускать, корень не править.** Cecil — только через `Tools/cecil_methods.ps1`. Сдача: «ПРИНЯТЬ» / «ПРИНЯТЬ С ПРАВКАМИ» / «НЕ ПРИНИМАТЬ», для каждой находки файл:строка, сценарий и минимальная правка. Коротко, у тебя маленький бюджет.
Отчёт — `Tools/Build/CodexStoryStageHotelReview/REPORT.md`, ack — `Tools/Build/CodexWorkerState/StoryStageHotelReview.json`.