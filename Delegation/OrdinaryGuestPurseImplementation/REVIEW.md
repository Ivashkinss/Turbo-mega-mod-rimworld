# Проверка Codex, 17.11.2026

Кандидат и тест независимо повторно собраны: exit0, production391168 bytes, probe18432 bytes. В рабочую DLL кандидат не установлен. Нативная игра не запускалась: перед ней исправляются доказуемые дефекты.

Адресные OP-01..04: [OrdinaryGuestPurseCorrections](../OrdinaryGuestPurseCorrections/TASK.md).

- Точный CalculateTarget из кандидата с его константой скомпилирован отдельно без игровых сборок через Tools/verify_guest_trip_budget_math.ps1. 7/9: float.MaxValue ожидаемые10000 даёт0 из-за преобразования в long до clamp; маленький положительный множитель с большим baseRoll тоже ошибочно даёт0. Последний случай проверяет контракт чистой функции, не обычный игровой roll150–300.
- Provider ItemUtility.GetInventorySpaceFor учитывает optional CompInventory; одной MassUtility недостаточно.
- Реальный GiveItems всегда сам добавляет вещи/серебро и оставляет minimum10 даже при multiplier0. Поэтому утверждения теста target-20, rich==500, repeated money unchanged, multiplier0 money==0 неверны. Проверять надо собственную добавку отдельно от provider генерации.
- Direct Settle на заспавненной пешке — не проверка естественного завершения визита. Save/load fixture должен иметь настоящий Lord и Current; нужен реальный lifecycle.

Положительный контроль математического harness: временная копия чистого метода с double-clamp до cast и точным positive-test проходит9/9. Она находится только в ignored Tools/Build/GuestTripMath/CorrectedMath.cs и не устанавливалась как игровой код. Вариант original7/9 против corrected9/9 подтверждает чувствительность проверки; это не нативный тест бюджета/сохранения.

Положительные части кандидата: точечный diff, добавка обычным гостям только в GiveItems-postfix, сохранение guard/target/added, отсутствие выдачи обычных денег из публичного Prepare, использование существующего cleanup. Это ещё не доказательство корректной игровой работы; требуется исправленный кандидат и целевой native fresh/reload.

Рабочая DLL по-прежнему c21bdb3ba33a3af93c21af87878a39b97c7d8f08d9ebf7f36325b24e974b773b (проверенное раннее приглашение). Пользовательские сохранения/игра/настройки не менялись.
