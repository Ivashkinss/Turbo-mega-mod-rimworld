# Gemini: контрольные состояния планировщика оптовых визитов

Читать только Tools/Build/WholesaleScheduleSnapshot/ и SNAPSHOT.json. Запись только Tools/Build/GeminiWholesaleScheduleCases/. Не менять Source/Defs/DLL/языки/Git/игру/сохранения/настройки и предыдущие отчёты. Игру и сеть не запускать. Python только C:/Users/vanya/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe -X utf8.

Создать CASES.json (ровно24 сценария), validate.py, REPORT.md. Это арифметическая модель ОДНОЙ оценки планировщика, не проверка NPC/генерации/навигации. Не воспроизводить лордов и реальные пешки. Временный nextCheck считать уже пройденным: в каждом случае планировщик получил право на одну оценку. ProcessVisits до этого не меняет входные таймеры модели.

Вход: nowTick, nextVisitTick, lastVisitTick (-1 = нет прежнего успешного визита; 0 легитимен), retryAfterTick, complexActive, isPlayerHome, V (конечная неотрицательная десятичная строка), currentGroups, factionAvailable, generationSucceeds. Выход: attempted (вызван ли TryVisit), spawned, nextVisitTick, lastVisitTick, retryAfterTick, delayTicks. Все вычисления времени целочисленные; V через Decimal, sqrt высокой точности; delay=ceil(60000*max(0.75,4/sqrt(1+V/2500))). delayTicks вычислять как справку даже при неактивном комплексе.

Правила строго по снимку:
- Если !complexActive или !isPlayerHome: таймеры не изменяются, attempted/spawned=false.
- Если V<=0: nextVisitTick=-1, lastVisitTick/retryAfterTick сохраняются, attempted/spawned=false.
- Иначе при nextVisitTick<0 назначить nowTick+delay. Если lastVisitTick>=0, взять min(nextVisitTick,lastVisitTick+delay); иначе min(nextVisitTick,nowTick+delay). Так рост ассортимента может приблизить и первый визит, но уже более близкий срок не отодвигается.
- Если nowTick<nextVisitTick ИЛИ nowTick<retryAfterTick: attempted/spawned=false, значения из предыдущего шага сохраняются. На равенстве таймер больше не блокирует.
- Иначе attempted=true. spawned=true только если currentGroups<2, factionAvailable и generationSucceeds. Отсутствие фракции/лимит2 всё равно означает попытку вызова TryVisit с отрицательным результатом.
- При успехе lastVisitTick=nowTick,nextVisitTick=nowTick+delay,retryAfterTick=0. При неуспехе только retryAfterTick=nowTick+2500; nextVisitTick остаётся ранее рассчитанным и может быть в прошлом. Именно retryAfterTick защищает от повторных попыток каждые120 тиков.

Покрыть: неактивность/не домашняя карта; пустой товар и полный резерв (оба V=0); первичное назначение; тик0 для lastVisitTick; границы delay минимум45000/максимум240000; перед сроком/ровно срок; рост V до и после первого визита; сохранение уже близкого next; провал генерации; нет фракций; лимит2; retry перед/ровно граница; минувший next+будущий retry; восстановление тех же таймеров после условного save/load (НЕ называть настоящей сериализацией).

Валидатор независимо пересчитывает ВСЕ выходы. Минимум6 negative self-tests: ошибка равенства срока, потеря cooldown, рост V не приблизил первый визит, обнуление legit lastTick0, спавн третьей группы, неправильное обновление lastVisitTick при неуспехе. REPORT отделяет проверенную модель от реальных игровых тестов Codex; не утверждать, что таймер гарантирует физический выход или что генерация прошла.
