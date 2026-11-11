# Состав проекта

Цель: один самостоятельный мод для RimWorld 1.6/1.7 с русской локализацией. Camera+ исключён по новой просьбе пользователя 11 ноября 2026.

| Исходный мод | Текущий статус | Русские папки во всех версиях |
| --- | --- | --- |
| Harmony | Встроен | 0 |
| [sbz] Fridge | Встроен | 0 |
| Adaptive Storage Framework | Встроен | 0 |
| Almost There! Fork | Встроен | 0 |
| Camera+ | Исключить по просьбе пользователя; удалён | 1 |
| Character Editor | Встроен | 0 |
| Common Sense | Встроен | 1 |
| Dev In Your Language | Встроен | 1 |
| Dynamic Trade Interface | Встроен | 0 |
| FSharp.Core | Встроен | 0 |
| HugsLib | Встроен | 1 |
| Interaction Bubbles | Встроен | 0 |
| Map Preview | Встроен | 4 |
| Melee Animation | Встроен | 0 |
| More Planning (Continued) | Встроен | 1 |
| [sbz] Neat Storage | Встроен | 1 |
| Nice Bill Tab | Встроен | 1 |
| Pick Up And Haul | Встроен | 1 |
| Realistic Lighting and Shadows | Исключить | 0 |
| Realistic Rooms Rewritten | Встроен | 0 |
| RunAndGun | Встроен | 1 |
| Show Me Your Hands | Встроен | 1 |
| Simple sidearms | Встроен | 2 |
| Smart Speed | Встроен | 0 |
| Smarter Construction | Встроен | 0 |
| Snap Out! | Встроен | 1 |
| Enhanced of Vanilla - Flaks and Plates | Исключить | 0 |
| Useful Marks | Встроен | 2 |
| Vanilla Expanded Framework | Встроен | 0 |
| Vanilla Textures Expanded | Исключить | 0 |
| Walls are solid | Встроен | 0 |
| XML Extensions | Встроен | 0 |

Перечень папок перевода — предварительная инвентаризация, не проверка полноты.

Завершены все 28 выбранных компонентов, включая Harmony и Vanilla Expanded Framework. Обязательных внешних библиотек нет. Оригинальные папки удалены после проверок и сохранения локальных архивов. Итоговая сверка состава и активного профиля — IntegrationCompletion.json; игровые результаты — Harmony-FullSet.json и отчёты отдельных компонентов.

Включено в план: 28. Исключено: 4.
Отсутствующих обязательных зависимостей по метаданным: 0.
Историческая инвентаризация не подтверждает совместимость всего списка. Актуальное покрытие проверок отдельных этапов описано в README.md и CONTINUE.md.

Almost There!: обычная и Vehicle Framework DLL сохранены раздельно, выбирается только одна; при активном оригинале все встроенные папки компонента пропускаются. Восемь сочетаний выбора проверены статически, четыре — через Verse.LoadFolder в 1.7. Vehicle Framework не установлен, его игровой сценарий не проверен.
