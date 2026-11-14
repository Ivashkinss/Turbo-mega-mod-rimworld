# Gemini: тексты квеста обычного найма — черновик

Писать только в `Tools/Build/GeminiComplexRecruitText/`. Читать этот TASK и предыдущий `Tools/Build/GeminiComplexRecruitQuestAudit/` только для контекста. Не менять Source/Defs/Languages/Assemblies, Git, игру, настройки/сохранения и прежние отчёты. Без сети и запуска DLL/игры.

Подготовить `TEXTS.json`: по каждому указанному ключу RU/EN, одинаковые параметры, без форматирования/кода внутри текста:
- `TMM.Recruit.QuestName`: «Работа в комплексе».
- `TMM.Recruit.QuestDescription` с `{0}` имя кандидата, `{1}` срок предложения в днях. Кандидат хочет стать обычным колонистом и работать в комплексе. ДО принятия явно указать: без подходящего назначения−50; после назначения+15, затем+10/+5, через15игровых суток бонус заканчивается. Любая обычная роль; общение необязательно, отказ от отдельной беседы не штрафуется. Никакого долга, рабства, выкупа/интимных услуг. Стиль RimWorld, до180слов.
- `TMM.Recruit.Arrived`, `TMM.Recruit.ArrivedHelp` с `{0}` имя.
- `TMM.Recruit.WaitingLabel`, `TMM.Recruit.WaitingDescription`: хочет работать в комплексе, пока обещанная обычная работа не назначена.
- `TMM.Recruit.Settled15Label`, `TMM.Recruit.Settled15Description`, `TMM.Recruit.Settled10Label`, `TMM.Recruit.Settled10Description`, `TMM.Recruit.Settled5Label`, `TMM.Recruit.Settled5Description`: постепенное привыкание к работе; не утверждать, что колонист принуждён или получает деньги за настроение.
- `TMM.Recruit.StatusWaiting`, `TMM.Recruit.StatusWorking`, `TMM.Recruit.StatusAdapted`: короткие статусы для персонала.
- `TMM.Recruit.NoComplex`: нет настроенного действующего комплекса.
- `TMM.Recruit.NoSuitableWork`: нет подходящей включённой обычной работы.
- `TMM.Recruit.CandidateUnavailable`: кандидат или карта недоступны; предложение не исполнено.

`REPORT.md`: проверки JSON, одинаковых плейсхолдеров и полного набора18ключей, до8замечаний/двусмысленностей. Это текстовый черновик будущего этапа: квест ещё не реализован и не протестирован. Не создавать XML для вымышленных Defs/классов, не выдавать API-аудит за доказательство работы.
