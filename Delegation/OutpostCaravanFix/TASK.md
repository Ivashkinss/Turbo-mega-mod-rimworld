# OutpostCaravanFix — встроить исправления пеших караванов по ревью Codex

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: StoryHotelFix

Codex не принял пешие караваны к аванпостам (`Tools/Build/CodexOutpostCaravanReview/REPORT.md`): нашёл 3 блокирующих P1 и 3 P2. Исправления пишет Gemini Neon по промпту `Delegation/Hydrogen/43-Neon-OutpostCaravan-FIX-PROMPT.md`. Файлы будут в `Delegation/StoryContent/CaravanFix/`, пути повторяют корень мода. Если папка пуста, запиши `blocked: waiting for Neon caravan fix` и переходи к следующей задаче очереди.

## Сделать
1. Скопируй `Source/OutpostCaravanLogistics.cs`, `Source/Dialog_OutpostCaravan.cs` и `Tests/InGameOutpostCaravanProbe.cs`.
2. Методы из `Source/StrategicOutpost.Caravan.txt` вставь вместо одноимённых в `Source/StrategicOutpost.cs`: `AddPawnFromCaravan` (обе перегрузки) и `RemovePawnToCaravan`. Остальной файл не трогай.
3. Новые строки интерфейса добавь в EN и RU `StrategicOutposts_Keys.xml`: сообщения и кнопку «Домой».
4. Собери мод. Исправь ошибки компиляции и разбери строки `// CHECK API` через `Tools/cecil_methods.ps1`. Логику Neon без причины не меняй. Каждое изменение перечисли в отчёте.
5. Все прогоны идут на одной DLL. Флаг `-CompatibilityReload` запрещён.

## Проверка в игре (обязательна)
- `OutpostCaravan` fresh и reload: все сценарии новой пробы.
- `Outposts`, `OutpostTab`: fresh и reload.
- `RegionalWorld`: fresh.
- 0 ERROR, не больше 2 попыток на шаг, `.ps1` только с BOM.

Не коммитить. Сдача: `Tools/Build/GeminiOutpostCaravanFix/REPORT.md`. В отчёте укажи SHA256 итоговой DLL. Он должен совпадать с хешем в Result.json всех прогонов.

## Замечания Claude к ответу Neon (20.11)
- Пришли все 4 файла, скобки сбалансированы, конструкций новее C# 5 нет. Строк `// CHECK API` — 10. Публичный API логистики и окна сохранён, кнопка «Домой» (`TMM_OutpostCaravan_ReturnHome`) уже есть.
- `Source/StrategicOutpost.Caravan.txt` (обе перегрузки `AddPawnFromCaravan` и `RemovePawnToCaravan`) я извлёк из ответа вручную.
- Новые ключи: добавь их в EN и RU `StrategicOutposts_Keys.xml`. Аргументы указаны в скобках, тексты RU предлагаю такие:
  - `StrategicOutpost_NoMobileCarrierLeft` — «В караване не останется ни одного носильщика, способного идти.»
  - `TMM_OutpostCaravan_HomeUnreachable` ({0} — караван) — «{0}: дорога домой недоступна. Караван остаётся на месте — нажмите «Домой», когда путь откроется.»
  - `TMM_OutpostCaravan_MissingSilverAborted` ({0} — погружено, {1} — нужно) — «Погружено только {0} серебра из {1}. Миссия отменена, груз остаётся у каравана.»
  - `TMM_OutpostCaravan_StorageTransferFailed` — «Склад аванпоста не принял груз. Всё возвращено в караван.»
  - `TMM_OutpostCaravan_FormationFailed` — «Не удалось сформировать караван.»
- `Cancel`, `CaravanFormationProcessStarted` и `CaravanMassCapacityExceeded` — ванильные ключи. Проверь, что они есть в игре, иначе в логе будут предупреждения.