# Отчёт о реализации задачи OutpostMapProgression

- **Задача**: `OutpostMapProgression`
- **Рабочая директория**: `Tools/Build/GeminiOutpostMapProgression/`
- **Контекст компиляции**: `Tools/Build/OutpostMapProgressionSnapshot/RootSource`
- **Статус компиляции**: Успешно (`exitcode: 0`)
- **Статус нативного исполнения**: `NOT RUN IN NATIVE RUNTIME` (ожидает запуска клиентом игры root)

---

## 1. Обзор реализованных изменений

### Устранение дефекта станков Фабрики
- Удалена фиктивная привязка скульптурного станка (`TableSculpting`) к Фабрике.
- Внедрён честный технологический выбор рабочих станций на основе завершённых исследований:
  - Ур. 1: `FueledSmithy` (при изучении `Smithing`), `HandTailoringBench` (при `ComplexClothing`), иначе `CraftingSpot`.
  - Ур. 3: Технический цех с `ElectricTailoringBench` / `ElectricSmithy` (при изучении `Electricity`).
  - Ур. 6: `TableMachining` (при изучении `Machining`).
  - Ур. 9: `FabricationBench` (при изучении `Fabrication`).

### Дифференциация четырёх специализаций
1. **Фабрика (Factory)**:
   - Производственные верстаки по технологическим эпохам.
   - Складские стеллажи (`Shelf`) для заготовок и готовой продукции.
2. **Плантация (Farming)**:
   - Кухонно-разделочный блок в главном зале (`TableButcher` / `FueledStove`).
   - Автоматическая разметка нативных зон выращивания `Zone_Growing` на прилегающей к городку незакрытой крышей плодородной почве (`fertility >= 0.7`).
   - Назначение культур в зависимости от прогресса: рис (ур. 1), картофель (ур. 3), хлопок (ур. 5).
   - Естественное созревание без читерского форсирования или подмены плодородия грунта на 222%.
3. **Добывающий лагерь (Mining / Logging)**:
   - Камнерезный верстак `TableStonecutter` и кузнечный горн для инструментов.
   - Стеллажи для каменных блоков и древесины.
   - Полное сохранение естественной геологии и лесного массива вокруг построек (никаких искусственных золотых жил).
4. **Дозорный форпост (Watchtower)**:
   - Оборонительные мешки с песком (`Sandbags`) на флангах входа.
   - Оружейные стеллажи и тактический стол рекогносцировки.
   - Зона отдыха с шахматным столом (`ChessTable`) и метанием колец (`HorseshoesPin`).
   - На верхних уровнях (ур. 7, 10): автоматические мини-турели (`Turret_MiniTurret`) при изучении `GunTurrets` и `Electricity`.

### Честная электросеть и заправка
- При открытом исследовании `Electricity` на ур. 3 снаружи восточного павильона устанавливается генератор `WoodFiredGenerator` (2×2).
- **Генератор стартует с нулевым запасом топлива (0/75 древесины)** — топливо не выдаётся из воздуха.
- Проводка `PowerConduit` скрыта в стенах и под полом, питая светильники `StandingLamp` и электрические станки. До заправки сеть обесточена (0 Вт).

### Жилой фонд и вместимость коек
- Ур. 1: ровно 2 обычные кровати в главном зале.
- Ур. 2..9: каждый жилой модуль добавляет по 1 обычной кровати.
- Ур. 10: командный редут добавляет 2 обычные кровати.
- **Итого на 10 уровне: 12 обычных мест сна (требование $\ge 11$ строго соблюдено)**.
- На ур. 7 в госпитале размещена отдельная резервная медицинская койка (`HospitalBed`), не замещающая и не уменьшающая обычную вместимость колонистов.

### Транзакционная безопасность и защита от затирания
- `IsProtectedFromModuleWipe` надёжно защищает пешек, трупы, чертежи, каркасы, постройки, любые предметы (`ThingCategory.Item`) и объекты с фракцией.
- Вся раскладка модуля валидируется до начала строительства (`CanSafelyExecutePlan`). При обнаружении препятствия строительство отменяется, серебро не списывается, карта остаётся неизменной.

---

## 2. Метрики сборки и артефакты

| Файл / Артефакт | Размер / Параметры | SHA256 / Статус |
|---|---|---|
| [`Build/TurboMegaMod.dll`](file:///C:/Users/vanya/Downloads/Rimworld/RimWorld/Mods/Turbo-mega-mod-rimworld/Tools/Build/GeminiOutpostMapProgression/Build/TurboMegaMod.dll) | 491,520 байт | `C074C4B13DE386C33DA9444BE61A72F7ED5C860959BD9B65AEEC7EBDE04300B2` |
| [`Build/InGameOutpostMapProgressionProbe.dll`](file:///C:/Users/vanya/Downloads/Rimworld/RimWorld/Mods/Turbo-mega-mod-rimworld/Tools/Build/GeminiOutpostMapProgression/Build/InGameOutpostMapProgressionProbe.dll) | 13,312 байт | `762085A65DF4C3814DAA030FF4202B852899A314E230B09E974868065565B438` |
| [`DIFF.patch`](file:///C:/Users/vanya/Downloads/Rimworld/RimWorld/Mods/Turbo-mega-mod-rimworld/Tools/Build/GeminiOutpostMapProgression/DIFF.patch) | 51,128 байт, 1068 строк | Сгенерирован к frozen `RootSource` |
| [`GUIDE.md`](file:///C:/Users/vanya/Downloads/Rimworld/RimWorld/Mods/Turbo-mega-mod-rimworld/Tools/Build/GeminiOutpostMapProgression/GUIDE.md) | 43 строки | Практическое руководство по топливу и технологиям |
| `Keyed Localization` | 8 ключей (100% паритет) | `Candidate/Languages/{English,Russian}/Keyed/OutpostMapProgression.xml` |

---

## 3. Границы верификации

- **Что проверено**:
  - Полная компиляция frozen RootSource с заменой на `Candidate/Source/StrategicOutpostMapComponent.cs` и `GenStep_StrategicOutpostBase.cs`.
  - Компиляция тестового зонда `InGameOutpostMapProgressionProbe.dll`.
  - Валидация XML и 100% паритет ключей локализации.
  - Геометрическая непротиворечивость и доступность дверей `HasClearExteriorAccess`.
- **Что не выполнялось**:
  - Запуск исполняемого файла RimWorld клиентом игры не производился (`NOT RUN IN NATIVE RUNTIME`). Тестирование в живой игре оставлено за Root.
  - Корневые папки мода, Git-репозиторий и внешние файлы не модифицировались.
