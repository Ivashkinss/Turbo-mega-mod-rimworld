# FactionRansomPolicyCorrections

Codex повторил сборку кандидата и пробы: exit0. Перед интеграцией исправь все4пункта `Documentation/FactionRansomPolicyCandidateReview.json` (копия Tools/Build/FactionRansomPolicyCodexReview/REVIEW.json). Особенно RP-04: НЕ называй локальный список с ручной очисткой Scribe roundtrip.

Входы только неизменяемый Tools/Build/FactionRansomPolicySnapshot/ и предыдущий Tools/Build/GeminiFactionRansomPolicyImplementation/. Их не менять. Запись ТОЛЬКО Tools/Build/GeminiFactionRansomPolicyCorrections/ и собственный Tools/Build/GeminiWorkerState/FactionRansomPolicyCorrections.json. Source/Defs/Languages/Assemblies корня, Git/QUEUE, игру, настройки/сейвы не трогать; сеть/GUI/игру не запускать. Compiler/ref DLL читать можно, Assembly-CSharp только Cecil metadata без исполнения.

Сдать полный новый Candidate/Source/{FactionRansomPolicy.cs,FactionRansom.cs,WorkBuyout.cs}, paired Candidate/Languages/{Russian,English}/Keyed/FactionRansomPolicy.xml, исправленный Candidate/Tests/InGameFactionRansomPolicyProbe.cs (можно2фазы -probe/-reload), build_candidate.ps1 (тот же snapshot + NEW Candidate overlay -> NEW Build/), DIFF.patch, RESPONSE.md по RP-01..04, REPORT.md. Реально повторить компиляцию кандидата и пробы, проверить placeholders. Не копировать кандидатный DLL в production.

Семантика только шанс0..100 и исключения по конкретным фракциям; никакого автоматического принятия, изменения цены, ценовых порогов, рейдов или новых денег. Default50 должен давать прежние ответы, уже сделанные предложения не пересчитываются. Root проверяет реальную игру позже. Родной `Tests/InGameFactionRansomProbe.cs` в снимке показывает стиль настоящих Offer/escrow/GameDataSaveLoader, его можно использовать как ориентир, не изменять файл снимка.

Сначала WorkerState running, после завершения done. Не ждать искусственно времени. Если реальный Scribe/Game API пока не удалось собрать — честно назвать точную ошибку, не заменять имитацией и не заявлять завершение этого пункта.
