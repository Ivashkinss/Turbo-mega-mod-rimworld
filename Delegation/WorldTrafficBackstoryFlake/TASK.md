# WorldTrafficBackstoryFlake — редкая ошибка генерации пешки в мировом трафике

kind: implement + run · assignee: gemini

В `TestRuns/HeroRegistry-FullSet-20261118-204454/reload-stdout.log` (20:48:51) одна ошибка:
`ERROR No shuffled Adulthood found for торговец of TribeCivil. Choosing random.`
Стек: `TurboMegaMod.WorldTraffic.MakePawn` → `TryStart` → `TrySpawn` → `WorldComponentTick`. Повторный прогон чистый, раньше ошибка не встречалась.

## Сделать
1. Прочитай `Source/WorldTraffic.cs`, `MakePawn`: какой `PawnGenerationRequest` собирается, какие PawnKind и фракции туда попадают.
2. Выясни, когда ванильная генерация не находит взрослую предысторию. Обычно это редкая комбинация pawnKind, фракции и категорий предыстории: например, торговец племени с ограничением по культуре или возрасту, слишком молодой для взрослой предыстории.
3. Исправь в `MakePawn` так, чтобы такой запрос не возникал. Например, правильный `PawnGenerationContext`, `fixedBiologicalAge` взрослого или подходящий pawnKind. Ванильное поведение подавлять нельзя, лог глушить тоже.
4. Докажи: напиши отдельный быстрый probe или дополни существующий, чтобы он вызвал `MakePawn` 300 раз для всех фракций и pawnKind, которые использует трафик. В логе должно быть 0 ошибок «No shuffled». Прогон только если RimWorld не запущен, таймаут 15 минут, не больше 2 попыток.

Не коммитить. Сдача: `Tools/Build/GeminiWorldTrafficBackstoryFlake/REPORT.md` — причина, дифф, результаты.