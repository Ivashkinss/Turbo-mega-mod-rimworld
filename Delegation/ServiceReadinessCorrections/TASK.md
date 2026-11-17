# ServiceReadinessCorrections — адресный ремонт кандидата

Codex повторил build_candidate.ps1: сборка успешна. Кандидат пока не принят в production. Исправь ВСЕ 5 пунктов рецензии, не переписывай другие службы.

- Полная рецензия: `Tools/Build/ServiceReadinessCodexReview/REVIEW.json` (копия `Documentation/ServiceReadinessCandidateReview.json`). Сначала прочитай её и owning методы в неизменяемом `Tools/Build/ServiceReadinessSnapshot/`.
- База кандидата: `Tools/Build/GeminiServiceReadinessImplementation/`; её файлы не менять. Новый единственный выход: `Tools/Build/GeminiServiceReadinessCorrections/` и свой `Tools/Build/GeminiWorkerState/ServiceReadinessCorrections.json`.
- Source/Assemblies/Defs/Languages корня, игру, настройки/сейвы, Git/QUEUE не менять. Сеть/GUI/запуск игры не нужны. Native Assembly-CSharp только read-only metadata, не исполнять. Compiler/reference DLL читать можно.
- Создай полный исправленный Candidate/Source/ServiceReadiness.cs + ComplexServicesDialog.cs, парные Candidate/Languages/{Russian,English}/Keyed/ServiceReadiness.xml, содержательный Candidate/Tests/InGameServiceReadinessProbe.cs, build_candidate.ps1 (тот же immutable snapshot + NEW Candidate overlay, output только NEW Build), DIFF.patch, RESPONSE.md по каждому SR-01..05, REPORT.md.
- Не обновляй production DLL, не добавляй постоянные кэши/MapComponent/тики/ИИ, не меняй экономику и сохраняемые данные. Окно остаётся только читающей сводкой; Gather не создаёт задачи и не исправляет настройки.
- Не подменяй реальные native fixtures отдельной зеркальной моделью, отсутствующие живые проверки честно отметить. Повтори собственную компиляцию кандидата и теста, проверь paired RU/EN placeholders. Все результаты проверки модели или только сборки не называй игровым PASS.
- Сначала WorkerState running, после результата done. Не ждать искусственно часов, сдавать рабочий результат сразу.
