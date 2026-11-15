# Gemini: поля досье и связь отложенных квитанций

Читать это задание и Tools/Build/GuestDossierSaveSnapshot/. Запись только Tools/Build/GeminiGuestDossierSaveAudit/ плюс статус по Delegation/GEMINI-WORKER.md. Не менять рабочие файлы, Source, XML, DLL, Git, игру, настройки, сохранения или прежние отчёты. Сеть/игру не запускать.

Создать FIELDS.json и REPORT.md. Механическая инвентаризация Scribe полей GuestDossiers/GuestDossier/GuestVisitRecord/GuestPaymentRecord/GuestReceiptLink и новых ссылок dossier в RestaurantOrder, CompanyOrder, SaleOrder, ClinicReceipt. Зафиксировать SHA256. Для каждого поля: класс, тип, saveKey, метод Scribe, defaults, guards; уникальность внутри класса. Отделить transient index/nextCheck от сохраняемых счётчиков. Проверить сохранение nextVisitId/nextReceiptId, lifetime visitCount/доходов при усечении отображаемой истории до50визитов/200платежей, dedupe receipts, строки guestId/visitId в квитанции независимо от жизненного цикла Pawn. Не предлагать deep Pawn copy или обещать его вечное хранение WorldPawns.

Проверить точную границу ухода: CompGuest.Leave лишь помечает leaving; PostDeSpawn записывает pendingExit, а Synchronize подтверждает MapHeld после помещения в переноску/контейнер. Не объявлять любой DeSpawn окончанием визита. Блокировка персональна, с причиной и днём; активный визит или клиническая помощь допускаются, recoveredOnMap не отсекается. Не считать отказ насильственным выселением/враждой.

Доказуемые нарушения отмечать file+line+короткая цитата. Если дефектов нет — findings:[], не выдумывать. Указать, что это снимок и статические выводы, не игровые тесты и не универсальная гарантия. Происшествия/VIP/репутацию не проектировать. Не исправлять код и не создавать себе новых заданий.
