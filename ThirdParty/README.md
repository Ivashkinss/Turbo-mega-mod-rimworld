# Источники включённых ресурсов

- RunAndGun — Roolo, Ogliss, kotobike; исходный packageId roolo.runandgun.kotobike. Сохранены RunAndGun.About.xml и восемь ресурсов: неизменённая DLL из 1.6, английский Keyed, патч, Def/News истории версий и три текстуры. Bundled 0Harmony.dll не переносится. Старый исходный LoadFolders выбирал 1.5; встроенный компонент явно выбирает 1.6. HugsLib остаётся внешней обязательной зависимостью; ModIdentifier и настройки RunAndGun сохранены. Дополнены четыре русских ключа окна CE. Combat Extended не установлен, диалог переведён статически, интеграция с CE не проверялась. Исторические новости сохранены на языке оригинала.

- Common Sense — avilmask, packageId avilmask.commonsense. Сохранены CommonSense.About.xml, неизменённая DLL 1.6, четыре Def, пять патчей, английские ключи и две иконки (13 файлов, SHA-256 в UtilityComponents.json). Русский текст перенесён из установленного оригинала: дополнены два пункта ручной выгрузки и шесть полей определений. Механика исходной DLL не переписывалась. Условия загрузки предотвращают повторное включение при активном оригинале; порядок после известных необязательных совместимых модов сохранён в About.

- Snap Out! — Weilbyte, packageId weilbyte.snapout. Сохранены SnapOut.About.xml, неизменённая DLL 1.6, четыре Def XML, патч Wander_Sad и английские строки. SHA-256 семи файлов — UtilityComponents.json. Русские строки перенесены с дополнением четырёх ключей, исправлением двух реплик и отчёта задания восстановления. Единицы часов в настройках переводятся нашим отдельным перехватом. Копия 0MultiplayerAPI.dll не добавлена: исходник побайтно совпадает с уже загружаемой DLL обязательного Adaptive Storage (SHA-256 4689e799c50db79411b00ad8855d7df86721bf3cfe1e390b68038b6abcd852ec). При последующей интеграции Adaptive Storage сохранить предоставление этой API. Сетевой режим не проверялся.

- Dev In Your Language — latta, packageId latta.devl10n, проект https://github.com/alattalatta/Monoworld. Перенесены неизменённая DLL 1.6 и семь английских Keyed XML (615 строк); метаданные — DevInYourLanguage.About.xml, SHA-256 — UtilityComponents.json. Ранее исправленный русский перевод остаётся в Languages/Russian/Keyed/DevMenu.xml; повреждённый русский Actions.xml не копируется. Старые DLL 1.1–1.5 не включаются.

- Neat Storage — автор seobongzu, исходный packageId sbz.NeatStorage.
- [sbz] Fridge — автор seobongzu, исходный packageId sbz.NeatStorageFridge.
- Smart Speed — автор Sarg, первоначальные авторы AlcoholV и Kartel'; исходный packageId sarg.smartspeed. MIT указан в сохранённых исходных метаданных SmartSpeed.About.xml.
- Smarter Construction — автор dhultgren, исходный packageId dhultgren.smarterconstruction. Исходные метаданные сохранены в SmarterConstruction.About.xml; отдельная лицензия в установленной папке не найдена.
- Interaction Bubbles 4.1 — автор Jaxe, исходный packageId jaxe.bubbles, исходный проект https://github.com/Jaxe-Dev/Bubbles. Сохранены InteractionBubbles.About.xml, DLL, английские ключи и три текстуры из установленного мода для 1.6. Отдельной лицензии в установленной папке нет. Оригинальная DLL не изменена, русский перевод и перехваты отображаемых надписей находятся в нашем проекте.
- Realistic Rooms Rewritten — автор Lucifer, исходный packageId lucifer.realisticrooms. Сохранены RealisticRooms.About.xml, оригинальная DLL из папки 1.4 (последняя версия бинарника в установленном моде), два XML-патча и английские настройки. Отдельной лицензии в установленной папке нет. DLL не изменена; исправление привязки порогов к переведённым категориям находится отдельно в Source/RealisticRoomsCompatibility.cs.
- Almost There! Fork — автор Duztamva, исходный packageId duz.almosttherefork. Сохранены AlmostThere.About.xml, обе оригинальные DLL 1.6 (обычная и для Vehicle Framework), оба XML-патча, 16 английских ключей и три иконки. DLL и ключи Scribe не изменены. Общая папка и одна выбранная ветка загружаются только при отключённом оригинале; обычная и Vehicle DLL никогда не выбираются одновременно. SHA-256 девяти файлов — в UtilityComponents.json. Vehicle Framework не установлен, его ветка проверена по ресурсам и условиям выбора, без игрового запуска с этой библиотекой.

Smart Speed и Smarter Construction: перенесены только выбранные DLL и ресурсы. Копия 0Harmony.dll из Smart Speed намеренно не переносится: проект использует одну внешнюю Harmony. Перечень файлов с SHA-256 — Documentation/UtilityComponents.json. LoadFolders включает эти компоненты, когда соответствующие оригиналы отключены, и пропускает их при включённых оригиналах, предотвращая повторную установку патчей.

Для Interaction Bubbles применяется такая же защита LoadFolders. В Documentation/UtilityComponents.json сохранены SHA-256 всех пяти перенесённых файлов. Русский перевод покрывает 31 исходный ключ; порядок четырёх направлений смещения сохранён. Дополнительно переведены три строки DLL: название настроек, заголовок окна с разметкой и пометка экспериментальной функции. Механика пузырей и ключи сохранения настроек оставлены оригинальными.

Скопированы ресурсы для RimWorld 1.6 и необходимые текстуры. Исходные метаданные сохранены в NeatStorage.About.xml и Fridge.About.xml. Полный перечень перенесённых файлов с SHA-256 оригиналов находится в Documentation/StorageChanges.json.

Изменения проекта: удвоена вместимость Neat Storage, обновлены описания, уплотнена раскладка линейных рядов предметов при удвоенной вместимости; исправлены смещения и ограничен размер графики предметов большого холодильника; русские переводы сведены без дубликатов.

Одинаковое определение категории BuildingsNeatStorage из двух модов оставлено в одном экземпляре.

Adaptive Storage Framework и Harmony на этом этапе используются как внешние библиотеки, их код и DLL сюда пока не скопированы. Русский перевод интерфейса Adaptive Storage подготовлен в проекте.

В версии с восстановлением меню разработчика использован русский текст из установленного Dev In Your Language (packageId latta.devl10n). Код и DLL этого мода не копировались. Исправлены синтаксис XML и посторонние текстовые вставки, сохранены существующие русские формулировки. Источники с SHA-256 перечислены в Documentation/DevMenuRepair.json.

Адаптивная раскладка продуктов реализована отдельно в Source/FridgeItemGraphicWorker.cs и использует публичный интерфейс ItemGraphicWorker библиотеки Adaptive Storage.
