# VehicleFramework — встроить Vehicle Framework (SmashPhil, MIT) как компонент

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: EquipmentSlots

Решение пользователя (20.11): машины фракций и винтокрылы делаем на Vehicle Framework. Его лицензия у выпуска v1.6.2144 и ветки develop — **MIT** (`LICENSE` в архиве, «Copyright (c) 2019-2025 Phil»), поэтому распространение внутри мода разрешено при сохранении текста лицензии. Архив уже скачан с разрешения пользователя: `Tools/Build/VehicleFramework-download/Vehicle-Framework-1.6.2144.zip` (6,4 МБ, sha256 начинается на B7CF20DEAFD7CE4B, источник https://github.com/SmashPhil/Vehicle-Framework/releases/tag/v1.6.2144). Больше ничего не скачивать.

## Сделать (по образцу прежних компонентов — Hospitality, Map Preview, Character Editor; см. `ThirdParty/README.md`, `LoadFolders.xml`, `Tools/build_utility_components.py`)
1. Распакуй архив во временную папку, перенеси в `Components/VehicleFramework/` только нужное для 1.6/1.7: `1.6/` (Assemblies, Defs, Patches, ветки совместимости), общие `Defs/Textures/Sounds/Languages/Assets` и т.п.; ветки 1.4/1.5 не переносить. Исходный `About.xml` → `ThirdParty/VehicleFramework.About.xml`, `LICENSE` → `Components/VehicleFramework/LICENSE` без изменений; запись в `ThirdParty/README.md` (автор, источник, версия, MIT, что перенесено).
2. `LoadFolders.xml` (v1.6 и v1.7): `<li IfModNotActive="smashphil.vehicleframework">Components/VehicleFramework</li>` и его подпапку 1.6 — так же, как другие компоненты; при активном оригинале встроенная копия пропускается.
3. Порядок загрузки: Harmony у нас встроена раньше; VF должен грузиться после Harmony. `incompatibleWith` оригинала (`majorhoff.rimthreaded`) учти в нашем `About.xml` только если не конфликтует с уже указанными.
4. Ветку `Components/AlmostThere/Vehicles` (уже есть в LoadFolders с `IfModActive="smashphil.vehicleframework"`) переключи так, чтобы она грузилась и при встроенном VF (условие по отсутствию оригинала → встроенный VF активен всегда).
5. Если 1.6-DLL падают на 1.7 (TypeLoad/MissingMethod в логе) — НЕ правь их байты; зафиксируй точные ошибки в отчёте и остановись: Claude решит, собирать ли VF из исходников develop (там есть 1.7) или делать аналог.

## Проверка в игре (обязательна)
- Новая проба `Tests/InGameVehicleFrameworkProbe.cs` (компонент `VehicleFramework`): VF загружен (тип `Vehicles.VehiclePawn` найден, его Harmony-патчи применены), 0 ERROR/TypeLoad в логе; у VF нет собственных машин, поэтому создай один минимальный тестовый VehicleDef только в пробе (или найди пример в его Defs) и проверь: машина спавнится на карте, у неё есть экипажные места, она добавляется в караван и сохраняется/загружается (reload).
- Регрессия: `AlmostThere`, `Outposts`, `OutpostTab`, `RegionalWorld`, `HeroRegistry`.
- 0 ERROR, «Error while instantiating a mod» отсутствует, ≤2 попытки на шаг, `.ps1` с BOM.

Не коммитить. Сдача: `Tools/Build/GeminiVehicleFramework/REPORT.md` (что перенесено, точный список файлов/хешей как у других компонентов, результаты, ошибки 1.7 если были).