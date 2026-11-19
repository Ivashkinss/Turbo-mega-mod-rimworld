**Режим: Canvas.** Один документ с двумя файлами. **Уровень рассуждений — высокий**: это финалы.

Акт 4 принят, он превосходный. Одна правка: у `StoryStage_ColonyRaid` исходы `success` и `timeout`, а `failed` у рейда не бывает. Флаг `story.a4_reckoningSurvived` мы продублировали в `onComplete`. **В акте 5 ставь флаги, от которых зависят следующие квесты, во всех исходах этапа или в `onComplete`.** Это касается Site (success / timeout / failed), ColonyRaid (success / timeout) и HotelVisit (success / failed / noHotel).

Теперь **акт 5 «Армагеддон Пустоши», часть 1 из 2**: финалы трёх коалиций.
- `free_wasteland` — Свободная Пустошь;
- `sterile_empire` — Стерильная Империя;
- `iron_crusade` — Железный Крестовый Поход.

Движок тот же (промпты 26–28), все классы с префиксом `TurboMegaMod.`, `<` в условиях пиши как `&lt;`.

## Для каждой из трёх коалиций
По библии у каждой коалиции 4 своих финальных квеста. Пусть каждый будет **событием**: своя механика, свой герой и свой поворот.
1. **2 спецоперации** («удар по болевым точкам»): `Site` с разными `objective` (`Clear`, `Rescue`, `Retrieve`, `Hold`, `KillHero`). Используй реальных врагов этой коалиции и их героев.
2. **«Осада Нулевого Якоря»**: многоволновая оборона, 2–3 этапа `ColonyRaid` подряд с нарастанием (`pointsFactor` 1.2 → 1.6 → 2.0). Между волнами — короткие `Wait` с письмами союзников. Если `story.swanDefeated` не равен `true`, в одну из волн вмешиваются дикие супермутанты (`TMM_WildMutantsFaction`).
3. **«Штурм главной цитадели»**: `Site` с `KillHero`. Главный враг коалиции — например, Дик Роман, Люцифер, Михаил или Мэксон, в зависимости от коалиции. Против Дика Романа текст подсказывает буру: если `story.boraxFormula=true`, есть вариант с бонусом.
4. **Последний квест коалиции** ставит `story.finaleDone=<коалиция>` в `onComplete`. По этому флагу откроется общий эпилог в части 2.

**Все квесты коалиции** требуют `act=5` и `ponr2=<коалиция>`. Внутри коалиции они идут цепочкой через `done:`.

**Пусть финалы помнят прошлое.** В `requires` вариантов выбора и в текстах учитывай:
- `story.luciferReleased`;
- `defector.<HeroDef>` — перебежчики сражаются на вашей стороне через `StoryEffect_HeroJoins` или упоминаются в тексте;
- `zeroSynth.trust`;
- `story.zeroSynthKidnapped`;
- `story.a1_endingStance`;
- `choice.<faction>`.

## Флаги
- уже есть: `act`, `ponr1`, `ponr2`, `holodisk`, `zeroSynth.trust`, `choice.*`, `defector.*`, `story.a1_*`…`story.a4_*`, `story.boraxFormula`, `story.luciferReleased`;
- новые ставь как `story.a5_*`.

Используй только реальные HeroDef. Если сомневаешься в id, пометь `<!-- check id -->`.

## Сдача
- `Defs/StoryDefs/Story_Act5_Part1.xml` (EN);
- `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act5_Part1.xml` (RU).

defName начинаются с `TMM_Story_A5_`. Часть 2 — остальные три коалиции и общий эпилог: судьба Нулевого Синта, наставника и мира.
