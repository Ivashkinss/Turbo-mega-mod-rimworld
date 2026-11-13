# Turbo mega mod

Все **28 из 28** первоначальных компонентов объединены в один мод для RimWorld 1.6/1.7. Дополнительно встроена Hospitality (Continued): теперь 29 компонентов. Технический packageId: `colonyproject.integrated`. Harmony и остальные обязательные библиотеки встроены.

Следующий этап — общая игра Fallout × «Сверхъестественное» с сюжетными экспедициями, базами фракций, новым хозяйством и платными услугами колонии. [Концепция и порядок разработки](Documentation/FALLOUT-SUPERNATURAL-DESIGN.md); [исследование кандидатов](Documentation/ContentCandidates.json). Это план нового контента: перечисленные кандидаты ещё не интегрированы.

## Хозяйство

Добавлены уличная теплица с планом стеклянных стен, крыши и двери, автоматическими посевами и настоящим отоплением; гидропоника со встроенным светильником; вспашка почвы. Солнце отключает дневную подсветку, затмение и полярная темнота включают её, ночью остаётся тусклый экономичный свет. Расход масштабируется по площади с надбавкой 15%.

Стеклянные стены имеют 50 HP, двери — 70 HP; усиление Walls Are Solid на них не действует. Вспашка получила собственную текстуру почвы и иконку от Gemini.

Готовая теплица сразу даёт почве 200% плодородия. Вспашка требует работы и даёт 200% снаружи или 222% в теплице. Гидропоника сохраняет свои 280%. [Постройка, управление, расход и ограничения](Documentation/CONTROLLED-FARMING.md).

## Гостиница

Обычные кровати переключаются кнопкой «Для гостей», тариф задаётся на панели кровати. Первые сутки оплачиваются при заселении, последующие — каждые 60 000 игровых тиков. Серебро физически передаётся из инвентаря гостя на землю рядом с ним. Новый тариф применяется к следующему расчёту; при нехватке денег платная кровать освобождается без объявления вражды. Бесплатные места поддерживаются. [Руководство и правила оплаты](Documentation/HOTEL.md).

## Установка

Папка проекта с `About/About.xml` должна находиться непосредственно в `RimWorld/Mods`. Включите Core, установленные DLC и Turbo mega mod после них. Отдельные Harmony, HugsLib, VEF и Adaptive Storage не требуются.

Проверенные исходные папки удаляются с локальным ZIP-архивом и резервной копией списка модов. Архивы лежат в игнорируемой `Tools/Build/Removed*.zip`, прежние списки — в `TestRuns/*Removal*/ModsConfig.before.xml`. Исходные пользовательские сохранения не менялись. Обычные файлы настроек оригиналов копируются под имя папки проекта только при отсутствии целевого файла; прежние файлы сохраняются. HugsLib и Character Editor сохраняют собственные системы хранения настроек.

При активном поддерживаемом оригинале соответствующие встроенные папки пропускаются, чтобы не загружать две копии. Neat Storage и Fridge отдельно включать нельзя: их определения находятся в корне объединённого мода.

## Состав

| Группа | Компоненты |
| --- | --- |
| Библиотеки | Harmony, HugsLib, Adaptive Storage Framework, Vanilla Expanded Framework, FSharp.Core/Poet, XML Extensions |
| Хранение | Neat Storage, Fridge |
| Бой и пешки | Melee Animation, Show Me Your Hands, RunAndGun, Simple Sidearms, Snap Out!, Character Editor |
| Работа и строительство | Smarter Construction, Common Sense, Pick Up And Haul, More Planning, Walls Are Solid |
| Интерфейс и управление | Smart Speed, Interaction Bubbles, Realistic Rooms, Almost There! Fork, Dev In Your Language, Nice Bill Tab, Useful Marks, Dynamic Trade Interface, Map Preview |
| Гости и платные услуги | Hospitality (Continued), собственная посуточная оплата гостиницы |

27 зданий Neat Storage имеют удвоенную вместимость. Семь холодильников Fridge сохраняют исходные значения; большой холодильник использует адаптивную сетку предметов.

В проекте **3297 уникальных русских записей**, включая восстановленные 615 строк Dev In Your Language и 90 строк правил. VEF покрыт всеми 497 английскими ключами и 192 полями определений, Map Preview — всеми 76 текущими ключами. Исправлены числовые параметры и перепутанные сообщения Hospitality о фракциях. Character Editor сохраняет собственный русский интерфейс.

Camera+, Realistic Lighting and Shadows, Enhanced of Vanilla — Flaks and Plates и Vanilla Textures Expanded исключены из плана. Camera+ ранее воспроизводила GPU-сбой; возвращать её в сборку не следует. Подробности — [CrashInvestigation.json](Documentation/CrashInvestigation.json).

## Совместимость

- Harmony взята из проверенного пакета 3.0.2.0. Варианты 1.6 и 1.7 хранятся раздельно; `HarmonyMod.RuntimeIL.dll` используется только в 1.7. Harmony загружается до HugsLib, Adaptive Storage и остальных потребителей.
- В VEF 1.7 добавлен безопасный инициализатор нейтрального кэша в конструкторе Pawn. Исходные 6244 метода не изменены; штатное обновление создаёт отдельный кэш каждой пешки. VEF 1.6 сохранён без изменений.
- Simple Sidearms и Nice Bill Tab распознают встроенный VEF через узкие поправки своих проверок. Show Me Your Hands и Simple Sidearms распознают встроенную Melee Animation. Глобальные алиасы packageId не используются.
- У Melee Animation перенаправлены только собственные загрузчики Animations/Bundles/WeaponTweakData; сохранены 66 AnimDef, все 3222 ресурса и восемь необязательных веток. На 1.7 исправлены перечисление пешек поединка и отсутствующее поле pawn в параметрах отрисовки.
- LunarLoader и Lunar-модули Map Preview сохранены побайтно с контрольными файлами `.lfc`. Перенаправлено чтение версии своего встроенного компонента; резервная HarmonyLib не загружается при доступной основной Harmony.
- Две использовавшиеся копии FloatSubMenu заменены одним исходным поставщиком Useful Marks. XML определения компонентов разведены по относительным путям, чтобы Verse не перекрывал их при объединении.
- Vehicle Framework, Vanilla Psycasts Expanded, Gradient Hair и другие внешние компаньоны не установлены. Их ветки сохранены и проверены статически; игровые сценарии с ними не проверялись.

## Проверки и ограничения

Реальные игровые тесты выполняются в отдельных профилях на RimWorld **1.7.5000 rev1275**. Они создают новую карту, проверяют функциональность компонентов, переводы, сериализацию настроек и продвижение кадров/тиков. При проверке оригинала подтверждается единственная загруженная копия библиотеки. Тестовые DLL удаляются после завершения.

Для Harmony проверяются настоящие prefix/postfix/transpiler, пропуск оригинального метода и снятие патчей, единственный поставщик, версия и RuntimeIL. Итоговый профиль дополнительно повторяет проверки VEF, фонового генератора Map Preview, всех 34 хранилищ/холодильников, Common Sense и HugsLib. Отдельные сценарии проверяют реальную анимацию Melee/отрисовку рук и выстрел RunAndGun во время движения.

Итоговая сверка текущей DLL записывается в [IntegrationCompletion.json](Documentation/IntegrationCompletion.json); отдельные отчёты содержат проверки компонентов, хозяйства, гостиницы и загрузки тестовых сохранений. Пользовательские сохранения не менялись.

Основные отчёты: [Harmony-FullSet.json](Documentation/Harmony-FullSet.json), [VanillaExpandedFramework-FullSet.json](Documentation/VanillaExpandedFramework-FullSet.json), [UtilityVerification.json](Documentation/UtilityVerification.json), [Verification.json](Documentation/Verification.json), [RuntimeLocalizationTests.json](Documentation/RuntimeLocalizationTests.json). Прежние этапы и точные ограничения каждого компонента сохранены в [CONTINUE.md](CONTINUE.md) и `Documentation/*-FullSet.json`.

Две известные ошибки русского правила генерации названий фракций Odyssey учитываются отдельно. Установленные файлы игры не исправлялись. Встречаются также устаревшие поля перевода Core/DLC. Вызов интерфейса в Repaint подтверждает отсутствие исключения при отрисовке, но не означает визуальную оценку раскладки. Все игровые ситуации и пользовательское сохранение не проверялись. Для 1.6 выполнены сборка на его ABI и статические проверки веток; живого запуска 1.6 не было.

## Разработка

Исходники собственного кода — `Source/`, проверок — `Tests/`, генераторов — `Tools/`. Происхождение ресурсов, исходные SHA256 и преобразования записаны в `Documentation/UtilityComponents.json`; метаданные авторов — `ThirdParty/`. Исходные лицензии сохранены рядом с компонентами, включая LGPL VEF и MIT Harmony/Map Preview. Сохранения, настройки, журналы и временные архивы в Git не входят.

Python 3 с lxml:

```text
python Tools/verify_utility_components.py
python Tools/verify_storage.py
python Tools/verify_combat_localization.py
```

PowerShell и .NET Framework C# compiler:

```powershell
python Tools/fetch_build_references.py
./Tools/build_assembly.ps1
./Tools/test_runtime_localization.ps1
./Tools/test_realistic_rooms_runtime.ps1
./Tools/test_vef_initializer.ps1
./Tools/test_farming_power.ps1
./Tools/test_component.ps1 -Component Farming -FullSet
./Tools/test_farming_reload.ps1 -Profile TestRuns/<успешный-профиль-Farming>
./Tools/test_component.ps1 -Component Hospitality -FullSet -UseBuiltAssembly
./Tools/test_hotel_reload.ps1 -Profile TestRuns/<успешный-профиль-Hospitality>
./Tools/test_component.ps1 -Component Harmony -FullSet -WithCoreRegression -UseBuiltAssembly
./Tools/test_component.ps1 -Component MeleeAnimation -FullSet -WithHands -UseBuiltAssembly
./Tools/test_component.ps1 -Component HugsLib -FullSet -WithRunAndGun -UseBuiltAssembly
```

Общая DLL собирается против RimWorld 1.6 reference assemblies, расположенных в `Tools/Build/RimRef-1.6.4871/ref/net472`, либо через `build_assembly.ps1 -ManagedPath <каталог Managed 1.6>`. Встроенная Harmony 1.6 используется для компиляции; нужная игровая версия выбирается через LoadFolders.

Импорт отдельного компонента: `python Tools/build_utility_components.py --component <имя>`. Для повторного импорта нужен его оригинал из локального архива; существующие записи остальных компонентов сохраняются. После добавления русских строк VEF запускайте `build_vef_localization.py`, для строк DLL — `build_runtime_localization.py`.

Hospitality импортируется из закреплённого checkout: `python Tools/build_utility_components.py --component Hospitality --source-root Tools/Build/HospitalitySnapshot`. SHA и источники — [AdditionalComponents.json](Documentation/AdditionalComponents.json) и [сведения о лицензии и сборке](ThirdParty/Hospitality.md). Её 1.7 DLL требует .NET 10 и игровых библиотек 1.7; общий код мода по-прежнему компилируется против 1.6.

`-UseBuiltAssembly` сохраняет одну и ту же производственную DLL между групповыми проверками; при изменении исходников требует новой сборки. Итоговая сверка: `python Tools/verify_integration_complete.py`.

Проверенные этапы отправляются в [репозиторий](https://github.com/Ivashkinss/Turbo-mega-mod-rimworld) согласно разрешению пользователя.
