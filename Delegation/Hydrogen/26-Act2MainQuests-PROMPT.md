**Режим: Canvas.** Один документ с двумя файлами, правки потом точечные. **Уровень рассуждений — высокий.**

Акт 1 принят, спасибо, он отличный. Мы поправили мелочи: фракция Минитменов называется `TMM_MinutemenHuntersFaction`, а у эффектов нужен полный класс `TurboMegaMod.StoryEffect_...`, как и у этапов.

Теперь **главная линия акта 2 «Нити кукловодов»**. Это 24 часа по твоей библии. Основную массу времени займут **75 квестов фракций**, их мы ставим отдельно из твоих пакетов 20–22. Твоя задача сейчас — **главная нить поверх них**. Она связывает линии фракций в одно расследование: игрок должен чувствовать, что каждая фракция открывает часть одной тайны, а мир реагирует на его решения.

## Что нужно (8–12 квестов и интерлюдий)
1. **Сектора Голодиска-Ключа.**
   - Сектор 2 «Чёрная бухгалтерия» — после 5 «точек фракций» (`arcPoints>=5`).
   - Сектор 3 «Голос наставника» — после 10 (`arcPoints>=10`).
   - Каждый сектор оформи как квест с выбором и расшифровкой. Это может быть мини-расследование: голодиск просит найти что-то конкретное у одной из фракций, с которыми игрок уже в хороших отношениях.
2. **Нулевой Синт растёт.** 2–3 эпизода, в которых его воспоминания и способности развиваются. Каждый эпизод зависит от `zeroSynth.trust`: при высоком и низком доверии развилки разные (`zeroSynth.trust>=30` / `zeroSynth.trust<0`).
3. **Мир реагирует на выбор в точках фракций.** 3–4 интерлюдии, которые срабатывают на конкретные флаги из твоих пакетов. Например:
   - `choice.brotherhood=pro_spark` — Данс спасён, и Мэксон присылает холодное письмо;
   - `choice.institute=pro_faction` — Подземка узнаёт, что вы выдали синта.
   Используй реальные флаги `choice.<faction>` со значениями `pro_spark` / `pro_faction` / `compromise` и id фракций из своего JSON: `institute`, `brotherhood`, `outcasts`, `vault_tec`, `minutemen_hunters`, `ghouls`, `friendly_mutants`, `enclave`, `gunners`, `railroad`, `wild_mutants`, `men_of_letters`, `hell_demons`, `heaven_angels`, `monster_clans`.
4. **Финал акта — «Второе пробуждение Якоря».**
   - Открывается, когда `arcPoints>=10` и `holodisk>=3`.
   - Механики: пепельный шторм (письмо и ожидание), большой рейд на колонию (`StoryStage_ColonyRaid`) с разведчиками Консорциума, затем выбор.
   - `onComplete` ставит `act`=3 и `holodisk`=4. Сектор 4 — чертёж Резонатора.

## Движок (тот же, что в акте 1)
**Этапы:**
- `TurboMegaMod.StoryStage_Choice` (`label`, `text`, `choices`: `outcome`, `label`, `resultText`, `nextStage`, `requires`, `effects`);
- `StoryStage_Wait` (`label`, `text`, `days`, `effects`);
- `StoryStage_HeroJoins` (`hero`);
- `StoryStage_Site` (`sitePart` не указывай, подберём сами; `enemyFaction`, `objective` = Clear | Rescue | Retrieve | Hold | KillHero, `hero`, `thing`, `holdHours`, `timeoutDays`, `outcomes`);
- `StoryStage_ColonyRaid` (`faction`, `pointsFactor`, `delayHours`, `outcomes`);
- `StoryStage_HotelVisit` (`faction`, `hero`, `groupSize`, `stayDays`, `objective` = Host | Investigate | Hide, `outcomes`).

`outcomes` — список `<li><outcome>success|timeout|failed</outcome><nextStage>..</nextStage><effects>..</effects></li>`. У всех классов префикс `TurboMegaMod.`.

**Эффекты:** `StoryEffect_SetFlag`, `StoryEffect_AddInt`, `StoryEffect_Goodwill` (`faction` — FactionDef, например `TMM_BrotherhoodFaction`), `StoryEffect_GiveThing` (только ванильные ThingDef), `StoryEffect_HeroJoins`, `StoryEffect_Letter`.

**Условия:** `done:<квест>`, `flag=value`, `flag>=n`, `flag<n`, `heroAlive:<HeroDef>`, `!` — отрицание, `anyOf` — «хотя бы одно».

**Поля квеста:** `act` (здесь 2), `main` (true для главной нити), `autoStart` (интерлюдии и главные шаги без предложения), `priority`, `faction`, `giver` и `fallbackGiver` (HeroDef), `letterLabel` и `letterText` (если нет autoStart), `stages`, `onComplete`. Подстановки в тексте: `{GIVER}`, `{FACTION}`, `{COLONY}`.

**Уже существующие флаги:**
- `act`, `holodisk`, `zeroSynth.trust`, `arcPoints` (растёт на 1 с каждой «точкой фракции»);
- `choice.<faction>`, `story.anchorOpened`;
- из акта 1: `story.a1_synthPromise` (protect | masked | asset), `story.a1_endingStance` (protector | pragmatist | ambitious), `story.a1_firstContact` (brotherhood | mol | vault_tec), `story.a1_journalShared`, `story.a1_demonSawAnchor`.

Используй их, чтобы акт 2 помнил акт 1.

**Пейсинг.** Главная нить не должна обгонять линии фракций. Все шаги, кроме самого первого, привязаны к `arcPoints`. Между шагами вставь интерлюдии с `StoryStage_Wait` на 1–2 дня и письмом. Не больше двух одинаковых механик подряд.

## Сдача
Два файла с путём над каждым:
- `Defs/StoryDefs/Story_Act2_Main.xml` (EN, `<Defs>`);
- `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act2_Main.xml` (RU).

defName начинаются с `TMM_Story_A2_`. Ключи RU — по пути поля, как в акте 1, включая `stages.N.outcomes.M.effects.K.text` для писем внутри исходов.
