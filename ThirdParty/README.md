# Источники включённых ресурсов

- Neat Storage — автор seobongzu, исходный packageId sbz.NeatStorage.
- [sbz] Fridge — автор seobongzu, исходный packageId sbz.NeatStorageFridge.
- Smart Speed — автор Sarg, первоначальные авторы AlcoholV и Kartel'; исходный packageId sarg.smartspeed. MIT указан в сохранённых исходных метаданных SmartSpeed.About.xml.
- Smarter Construction — автор dhultgren, исходный packageId dhultgren.smarterconstruction. Исходные метаданные сохранены в SmarterConstruction.About.xml; отдельная лицензия в установленной папке не найдена.

Smart Speed и Smarter Construction: перенесены только выбранные DLL и ресурсы. Копия 0Harmony.dll из Smart Speed намеренно не переносится: проект использует одну внешнюю Harmony. Перечень файлов с SHA-256 — Documentation/UtilityComponents.json. LoadFolders включает эти компоненты, когда соответствующие оригиналы отключены, и пропускает их при включённых оригиналах, предотвращая повторную установку патчей.

Скопированы ресурсы для RimWorld 1.6 и необходимые текстуры. Исходные метаданные сохранены в NeatStorage.About.xml и Fridge.About.xml. Полный перечень перенесённых файлов с SHA-256 оригиналов находится в Documentation/StorageChanges.json.

Изменения проекта: удвоена вместимость Neat Storage, обновлены описания, уплотнена раскладка линейных рядов предметов при удвоенной вместимости; исправлены смещения и ограничен размер графики предметов большого холодильника; русские переводы сведены без дубликатов.

Одинаковое определение категории BuildingsNeatStorage из двух модов оставлено в одном экземпляре.

Adaptive Storage Framework и Harmony на этом этапе используются как внешние библиотеки, их код и DLL сюда пока не скопированы. Русский перевод интерфейса Adaptive Storage подготовлен в проекте.

В версии с восстановлением меню разработчика использован русский текст из установленного Dev In Your Language (packageId latta.devl10n). Код и DLL этого мода не копировались. Исправлены синтаксис XML и посторонние текстовые вставки, сохранены существующие русские формулировки. Источники с SHA-256 перечислены в Documentation/DevMenuRepair.json.

Адаптивная раскладка продуктов реализована отдельно в Source/FridgeItemGraphicWorker.cs и использует публичный интерфейс ItemGraphicWorker библиотеки Adaptive Storage.
