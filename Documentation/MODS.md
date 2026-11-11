# Состав проекта

Цель: один самостоятельный мод для RimWorld 1.6/1.7 с русской локализацией. Camera+ исключён по новой просьбе пользователя 11 ноября 2026.

| Исходный мод | Решение | Русские папки во всех версиях |
| --- | --- | --- |
| Harmony | Включить | 0 |
| [sbz] Fridge | Включить | 0 |
| Adaptive Storage Framework | Включить | 0 |
| Almost There! Fork | Включить | 0 |
| Camera+ | Исключить по просьбе пользователя; удалён | 1 |
| Character Editor | Включить | 0 |
| Common Sense | Включить | 1 |
| Dev In Your Language | Включить | 1 |
| Dynamic Trade Interface | Включить | 0 |
| FSharp.Core | Включить | 0 |
| HugsLib | Включить | 1 |
| Interaction Bubbles | Включить | 0 |
| Map Preview | Включить | 4 |
| Melee Animation | Включить | 0 |
| More Planning (Continued) | Включить | 1 |
| [sbz] Neat Storage | Включить | 1 |
| Nice Bill Tab | Включить | 1 |
| Pick Up And Haul | Включить | 1 |
| Realistic Lighting and Shadows | Исключить | 0 |
| Realistic Rooms Rewritten | Включить | 0 |
| RunAndGun | Включить | 1 |
| Show Me Your Hands | Включить | 1 |
| Simple sidearms | Включить | 2 |
| Smart Speed | Включить | 0 |
| Smarter Construction | Включить | 0 |
| Snap Out! | Включить | 1 |
| Enhanced of Vanilla - Flaks and Plates | Исключить | 0 |
| Useful Marks | Включить | 2 |
| Vanilla Expanded Framework | Включить | 0 |
| Vanilla Textures Expanded | Исключить | 0 |
| Walls are solid | Включить | 0 |
| XML Extensions | Включить | 0 |

Перечень папок перевода — предварительная инвентаризация, не проверка полноты.

На текущем этапе встроены 27 компонентов: Vanilla Expanded Framework, Map Preview, Character Editor, Melee Animation, HugsLib, Adaptive Storage Framework, Neat Storage, Fridge, Smart Speed, Smarter Construction, Interaction Bubbles, Realistic Rooms, Almost There! Fork, Dev In Your Language, Snap Out!, Common Sense, RunAndGun, Simple Sidearms, Pick Up And Haul, Nice Bill Tab, More Planning, Show Me Your Hands, Useful Marks, Dynamic Trade Interface, FSharp.Core, Walls Are Solid и XML Extensions. Для Interaction Bubbles переведены все 31 исходный ключ и три надписи DLL. Для Realistic Rooms переведены 11 ключей и исправлено применение порогов к локализованным категориям; игровые отчёты — RealisticRooms-Integrated.json, RealisticRooms-OriginalActive.json и RealisticRooms-FullSet.json. Остальные строки таблицы описывают первоначальную инвентаризацию.

Включено в план: 28. Исключено: 4.
Отсутствующих обязательных зависимостей по метаданным: 0.
Историческая инвентаризация не подтверждает совместимость всего списка. Актуальное покрытие проверок отдельных этапов описано в README.md и CONTINUE.md.

Almost There!: обычная и Vehicle Framework DLL сохранены раздельно, выбирается только одна; при активном оригинале все встроенные папки компонента пропускаются. Восемь сочетаний выбора проверены статически, четыре — через Verse.LoadFolder в 1.7. Vehicle Framework не установлен, его игровой сценарий не проверен.
