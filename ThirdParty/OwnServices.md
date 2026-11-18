# Собственные службы колонии

Ресторан в `Source/Restaurant.cs`, его XML и оригинальная графика созданы для этого проекта и не включают код, DLL или рисунки Gastronomy/CashRegister. Собственный исходный код этого модуля распространяется под GPL-3.0-or-later; полный текст GPL сохранён, например, в `Components/Hospitality/LICENSE`. Это не меняет лицензии остальных включённых сторонних компонентов.

Рисунки подготовлены Gemini по `Delegation/RestaurantAssets/TASK.md` без заимствованных изображений и шрифтов. Независимо проверены PNG, отступы, SHA256, повторяемость генератора и лист предпросмотра. Перечень — `Documentation/GeminiRestaurantAssets.json`; проверка — `Tools/verify_restaurant_assets.py`.

Генератор, исходные отчёты и тестовые данные остаются в локальной игнорируемой `Tools/Build/`; финальные ресурсы, читаемый код, определения и результаты проверок включены в репозиторий. Сведения об отклонённых ресторанных кандидатах — `Documentation/RestaurantSourceDecision.json`.

Управление комплексом (`Source/ComplexManagement.cs`, собственные Defs/локализация) также распространяется под GPL-3.0-or-later. Оригинальные рисунки ресепшена и персонала подготовлены по `Delegation/ComplexAssets/TASK.md`; их проверенный реестр — `Documentation/GeminiComplexAssets.json`, проверка — `Tools/verify_complex_assets.py`.

Собственные расширения комплекса (`Company`, `CompanyMenus`, `ComplexLayout`, `ComplexServicesDialog`, `WorkBuyout`, `Clinic`, `ClinicGuests`, `ClinicBilling`) распространяются под GPL-3.0-or-later. Они включают обычные беседы, управление помещениями и учёт труда. Нативный компонент Hospital сохраняет свою исходную лицензию в `Components/Hospital/`; состав и закреплённый источник указаны в `Documentation/AdditionalComponents.json`.

Оригинальная иконка бесед подготовлена Gemini по `Delegation/CompanyAssets/TASK.md`. Принят только проверенный `Textures/UI/Company/Company.png`; размеры, SHA256 и результат просмотра зафиксированы в `Documentation/GeminiCompanyAssets.json`.

Расширение теплиц и адресное исправление русских грамматических правил Odyssey написаны для этого проекта. Исправления правил выполняются в памяти при загрузке языка; архивы игры не распространяются и не изменяются. Область исправлений и проверки описаны в `Documentation/OdysseyGrammarRepair.json`.

Квест найма (`Source/ComplexRecruitQuest.cs`), состояние адаптации (`ComplexRecruitment.cs`, `RecruitmentProgress.cs`) и соответствующие определения созданы для проекта и распространяются под GPL-3.0-or-later. Они используют штатные API квестов RimWorld без копирования игрового исходного кода. Черновики текстов и контрольных случаев Gemini независимо сверены с реализацией.

Собственная розничная торговля (`Source/ComplexSales.cs`, определения и локализация) распространяется под GPL-3.0-or-later. Она использует API хранения и связывает штатный выбор покупок Hospitality с сохраняемыми собственными заказами; исходный JobDriver_BuyItem в новый модуль не скопирован. Аудиты и черновик руководства Gemini проверены и исправлены Codex; сведения о передачах и пределах этапа — `Documentation/SalesTransferReview.json` и `COMPLEX-SALES.md`.

Расчёт размещения оптового груза (`Source/Wholesale/PackingPlanner.cs`) написан для проекта и распространяется под GPL-3.0-or-later. Это собственная арифметика цены, дебета и вместимости, без копирования игрового исходного кода. Модуль включён в игровую DLL и используется собственным оптовым адаптером. `Source/WholesaleVisits.cs` и `Source/WholesaleTrading.cs` также созданы для проекта под GPL-3.0-or-later; они используют API игры без копирования её исходников. Числовые проверки отделены от реальных транзакций, группового ИИ и сохранений: см. `Documentation/WholesaleStageVerification.json`.

Учёт медицинских исполнителей (`MedicalAllocation.cs`, `ClinicContributions.cs`, `ClinicMedicalHooks.cs` и расширение `ClinicBilling.cs`) создан для проекта под GPL-3.0-or-later. Он использует фактический прирост счёта Hospital и игровые уведомления, не копируя исходники игры. Чистая арифметика проверяется отдельно от Scribe и физических выплат; сохранения пользователя не изменяются.

Картотека гостей и персональный чёрный список (`GuestDossiers.cs`, `GuestDossierHooks.cs`, `GuestDossierDialog.cs`), единый диалог ресепшена (`ComplexServicesDialog.cs`, `ComplexReceptionUI.cs`), ручная разметка тепличной крыши (`ManualGreenhouse.cs`) и миграция старых планов расширения созданы для проекта под GPL-3.0-or-later. Они используют API игры и встроенных компонентов без распространения исходного кода игры.
