**Режим: обычный чат** (два файла).

Спасибо за героев Изгоев, они приняты. Я перевёл их в формат мода: Морриган стала наследником Касдина, Варгас и Кесслер получили rank 3. Теперь нужны **биографии**: по две на каждого из 6 героев, детство и взрослая жизнь, всего 12 штук. Пиши **строго по образцу ниже**, тем же форматом и с теми же тегами.

defName биографий (менять нельзя):
- `TMM_BS_henry_casdin_child` / `TMM_BS_henry_casdin_adult`
- `TMM_BS_specialist_olin_child` / `_adult`
- `TMM_BS_scribe_silas_child` / `_adult`
- `TMM_BS_defender_morrigan_child` / `_adult`
- `TMM_BS_paladin_vargas_child` / `_adult`
- `TMM_BS_scout_kessler_child` / `_adult`

Правила:
- skillGains: сумма 4–8, не больше 5 на навык;
- `workDisables` можно опустить; `Violent` не отключать;
- в описании `[PAWN_nameDef]`, `[PAWN_pronoun]`, `[PAWN_possessive]`, 2–3 предложения;
- сюжет биографий согласуй со своими квестами Изгоев: склеп под госпиталем, украденные чертежи, побег Сайласа, Морриган как будущий лидер.

### Образец файла 1: `Defs/BackstoryDefs/Backstories_Outcasts.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- Henry Casdin -->
  <BackstoryDef>
    <defName>TMM_BS_henry_casdin_child</defName>
    <slot>Childhood</slot>
    <title>bunker cadet</title>
    <titleShort>cadet</titleShort>
    <description>...[PAWN_nameDef]... [PAWN_pronoun] ... [PAWN_possessive] ...</description>
    <skillGains>
      <Shooting>2</Shooting>
      <Melee>2</Melee>
    </skillGains>
    <workDisables>Artistic</workDisables>
    <spawnCategories><li>TMM_Heroes</li></spawnCategories>
    <shuffleable>false</shuffleable>
  </BackstoryDef>
  <BackstoryDef>
    <defName>TMM_BS_henry_casdin_adult</defName>
    <slot>Adulthood</slot>
    ...
  </BackstoryDef>
</Defs>
```

### Образец файла 2: `Languages/Russian/DefInjected/BackstoryDef/Backstories_Outcasts.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <!-- Генри Касдин -->
  <TMM_BS_henry_casdin_child.title>кадет бункера</TMM_BS_henry_casdin_child.title>
  <TMM_BS_henry_casdin_child.titleShort>кадет</TMM_BS_henry_casdin_child.titleShort>
  <TMM_BS_henry_casdin_child.description>...</TMM_BS_henry_casdin_child.description>
</LanguageData>
```

Два блока кода с путём над каждым, без пояснений.
