**Режим: Canvas.** Один XML-документ на два файла, их будем дорабатывать точечно. **Уровень рассуждений — высокий**: это первые часы сюжета, они задают тон всей игре.

Сюжетный движок мода готов. Квесты описываются в XML, и ты можешь писать их **сразу в рабочем формате**. Сейчас в игре два первых шага акта 1 из твоей библии: «Сигнал под фундаментом» и визит Кастиэля. Напиши **остальной акт 1**: шаги act1_s2 … act1_s5 из библии (гости у ворот, сны Нулевого Синта, первый импульс Якоря, разговор под пеплом). Добавь 2–3 интерлюдии-письма, чтобы акт шёл около 7 часов без пустых пауз. Последний квест акта ставит флаг `act`=2.

## Что умеет движок сейчас (этапы `<li Class="...">`)
- `TurboMegaMod.StoryStage_Choice` — письмо с вариантами: `label`, `text`, `choices` (каждый вариант: `outcome`, `label`, `resultText`, `nextStage`, `requires`, `effects`);
- `TurboMegaMod.StoryStage_Wait` — пауза с письмом в конце: `label`, `text`, `days`, `effects`;
- `TurboMegaMod.StoryStage_HeroJoins` — герой приходит в колонию капсулой: `hero`.

Скоро появятся (можно уже использовать, поля как здесь):
- `TurboMegaMod.StoryStage_Site` — место на карте мира: `sitePart`, `enemyFaction`, `objective` = Clear | Rescue | Retrieve | Hold | KillHero, `hero`, `thing`, `holdHours`, `timeoutDays`, `outcomes` (список с `outcome` = success | timeout | failed, `nextStage`, `effects`);
- `TurboMegaMod.StoryStage_ColonyRaid` — рейд на колонию: `faction`, `pointsFactor`, `delayHours`, `outcomes`;
- `TurboMegaMod.StoryStage_HotelVisit` — гость или делегация в гостинице: `faction`, `hero`, `groupSize`, `stayDays`, `objective` = Host | Investigate | Hide, `outcomes`.

**Эффекты** (`<li Class="...">`):
- `StoryEffect_SetFlag` (`flag`, `value`);
- `StoryEffect_AddInt` (`flag`, `amount`);
- `StoryEffect_Goodwill` (`faction` — FactionDef, пусто — фракция квеста; `amount`);
- `StoryEffect_GiveThing` (`thing` — ванильный ThingDef, `count`);
- `StoryEffect_HeroJoins` (`hero`);
- `StoryEffect_Letter` (`label`, `text`).

**Поля квеста:** `defName`, `label`, `act`, `main`, `autoStart` (для главного шага без предложения), `priority`, `faction`, `giver` и `fallbackGiver` (HeroDef, например `TMM_Hero_castiel`, `TMM_Hero_preston_garvey`), `requires` (условия: `done:<квест>`, `flag=value`, `flag>=n`, `heroAlive:<HeroDef>`, `!` — отрицание), `anyOf`, `letterLabel` и `letterText` (письмо-предложение, нужно, если нет `autoStart`), `stages`, `onComplete`. Подстановки в тексте: `{GIVER}`, `{FACTION}`, `{COLONY}`.

**Флаги:**
- `act`;
- `holodisk` (0..4);
- `zeroSynth.trust` (−100..100);
- `story.<имя>`;
- уже стоят `story.anchorOpened` (true/false — люк вскрыт или заварен) и `story.anchorAwakened`.

Первый квест акта должен требовать `done:TMM_Story_A1_Castiel`. Учти выбор игрока: если люк заварен, сюжет идёт иначе, хотя бы в одном тексте или варианте.

## Образец (так уже работает в игре)
```xml
<TurboMegaMod.StoryQuestDef>
  <defName>TMM_Story_A1_Castiel</defName>
  <label>a visitor in a beige coat</label>
  <act>1</act><main>true</main><priority>90</priority>
  <faction>TMM_HeavenFaction</faction>
  <giver>TMM_Hero_castiel</giver><fallbackGiver>TMM_Hero_naomi</fallbackGiver>
  <requires><li>done:TMM_Story_A1_Signal</li></requires>
  <letterLabel>A visitor in a beige coat</letterLabel>
  <letterText>... {GIVER} ...</letterText>
  <stages>
    <li Class="TurboMegaMod.StoryStage_Choice">
      <label>The truth about the Spark</label>
      <text>...</text>
      <choices>
        <li><outcome>trust</outcome><label>...</label><resultText>...</resultText>
          <effects><li Class="TurboMegaMod.StoryEffect_Goodwill"><amount>15</amount></li></effects></li>
        <li><outcome>refused</outcome><label>...</label><resultText>...</resultText></li>
      </choices>
    </li>
  </stages>
  <onComplete><li Class="TurboMegaMod.StoryEffect_AddInt"><flag>holodisk</flag><amount>1</amount></li></onComplete>
</TurboMegaMod.StoryQuestDef>
```
Русский перевод — отдельный файл DefInjected, ключи строятся по пути поля:
```xml
<TMM_Story_A1_Castiel.label>гость в бежевом плаще</TMM_Story_A1_Castiel.label>
<TMM_Story_A1_Castiel.letterText>...</TMM_Story_A1_Castiel.letterText>
<TMM_Story_A1_Castiel.stages.0.text>...</TMM_Story_A1_Castiel.stages.0.text>
<TMM_Story_A1_Castiel.stages.0.choices.0.label>...</TMM_Story_A1_Castiel.stages.0.choices.0.label>
<TMM_Story_A1_Castiel.stages.0.choices.0.resultText>...</TMM_Story_A1_Castiel.stages.0.choices.0.resultText>
```

## Сдача
Два файла с путём над каждым:
- `Defs/StoryDefs/Story_Act1_Main.xml` (EN, в `<Defs>`);
- `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act1_Main.xml` (RU, в `<LanguageData>`).

defName начинаются с `TMM_Story_A1_`. Каждый квест — маленькая история с запоминающейся деталью и выбором, который аукнется позже: ставь флаги `story.*`.
