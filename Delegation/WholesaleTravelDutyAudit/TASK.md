# Gemini: адресная сверка Travel duties для оптовой группы

Запись только Tools/Build/GeminiWholesaleTravelDutyAudit/. Читать Source/WholesaleVisits.cs (он может меняться, фиксировать SHA256 прочитанного файла), Tools/Build/WholesaleGuideSnapshot/ и IL ../../Assembly-CSharp.dll доверенным Cecil из Components/Harmony/1.6/Assemblies/0Harmony.dll. Reference1.6 разрешён только для наличия API. Не исполнять игровые DLL, игру/сеть не запускать, не менять Source/Defs/языки/DLL/Git/игровые настройки/сохранения и прошлые отчёты. Python только C:/Users/vanya/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe -X utf8.

Первый свежий игровой тест оптовиков прошёл24 проверки (реальная покупка, частичный срыв с откатом, эскроу и подготовка сохранения). В первой загрузке прошли18 проверок (груз/деньги/книга/автоматический заказ), но тест новой сгенерированной группы не увидел Shopping за4000 тиков. Это НЕ доказанный баг движка: Codex отдельно собирает позиции и текущие работы. Не придумывать диагноз по одной фразе.

Нужны TRAVEL.json, REPORT.md, до10 адресных проверенных фактов:
1. LordToil_Travel: конструктор, Init/UpdateAllDuties, LordToilTick, AllArrivedCheckRadius. Какие PawnDuty/DefOf назначаются, как хранятся цель и данные, на каких условиях реально отправляется TravelArrived.
2. DutyDefOf.Travel и фактические JobGiver, которые используют duty.focus для пути. Точные имена классов/методов/IL, какие требования к focus/Map/CanReach, поведение при неудачном пути. Доступные XML Core можно читать, но не подменять ими фактический код текущей1.7.
3. Есть ли у PawnKindDef с trader=true особый ThinkTree/приоритет TraderAI, который может игнорировать LordDuty, если отсутствует Pawn_TraderTracker.TraderKind? Только доказуемая цепочка; если не установлено — неизвестно. Не предлагать гостиничный Lord/Hospitality.
4. Как Pawn_MindState.Notify_LordDestroyed/ThinkTree_Duty/старт новой работы реагируют на LordMaker.MakeNewLord и смену тоила. Какие публичные методы нужны, чтобы заново назначенные duty вступили в силу, без принудительного перемещения пешки.
5. REPORT: отделить штатные предусловия и неизвестные от диагноза; максимум5 конкретных наблюдений для живого теста (позиции, job.def, duty.def/focus, скорость, CanReach), которые Codex может проверить. Никаких обещаний, что наличие таймера гарантирует достижимость или физический выход.
