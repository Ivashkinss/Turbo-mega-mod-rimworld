Ты пишешь контент для мода RimWorld 1.7 с DLC Biotech: кроссовер Fallout × «Сверхъестественное». Нужны шесть героев фракции Небеса — описание героя (HeroDef) и уникальные предыстории (BackstoryDef). Ответ — только три готовых XML-файла, без пояснений.

## Герои
| id | Имя | Пол | Ранг | Что важно |
|---|---|---|---|---|
| michael | Михаил | М | 4, лидер | старший архангел, полководец Небес, непреклонный солдат воли Отца |
| lucifer | Люцифер | М | 3 | падший архангел, обаятельный и опасный; формально в ростере Небес, но у него свои цели |
| castiel | Кастиэль | М | 3 | ангел Господень, вытащивший Дина из Ада; бунтарь, верный друг охотников |
| gabriel | Гавриил | М | 3 | архангел-трикстер, долго прятался под маской Локи, любит сладкое и розыгрыши |
| zachariah | Захария | М | 3 | небесный бюрократ, циничный управленец, манипулирует людьми ради «плана» |
| naomi | Наоми | Ж | 3 | глава небесной разведки, холодная и расчётливая |

## Файл 1 — `Defs/HeroDefs/Heroes_Heaven.xml`
Образец одной записи, остальные по нему:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <TurboMegaMod.HeroDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hero_castiel</defName>
    <childhood>TMM_BS_castiel_child</childhood>
    <adulthood>TMM_BS_castiel_adult</adulthood>
    <label>Castiel</label>
    <factionDef>TMM_HeavenFaction</factionDef>
    <rank>3</rank>
    <pawnKind>Mercenary_Elite</pawnKind>
    <xenotype>TMM_Angel</xenotype>
    <nameFirst>Castiel</nameFirst><nameNick>Cas</nameNick><nameLast></nameLast>
    <gender>Male</gender>
    <ageRange>35~42</ageRange>
    <skills>
      <li><skill>Melee</skill><level>14</level></li>
      <li><skill>Medicine</skill><level>10</level></li>
    </skills>
  </TurboMegaMod.HeroDef>
</Defs>
```
- Михаилу — `<isLeader>true</isLeader>`, `<rank>4</rank>`, `<pawnKind>PirateBoss</pawnKind>`. Остальным — `Mercenary_Elite`.
- `skills`: 2–3 навыка из Shooting, Melee, Construction, Mining, Cooking, Plants, Animals, Crafting, Artistic, Medicine, Social, Intellectual. Уровни от 8 до 18, сильнее у архангелов.
- `ageRange` — по видимому возрасту сосуда.
- Больше никаких полей не добавляй.

## Файл 2 — `Defs/BackstoryDefs/Backstories_Heaven.xml`
Для каждого героя две предыстории: `TMM_BS_<id>_child` (slot Childhood — сотворение или жизнь сосуда) и `TMM_BS_<id>_adult` (slot Adulthood).
```xml
<BackstoryDef>
  <defName>TMM_BS_castiel_child</defName>
  <slot>Childhood</slot>
  <title>soldier of the garrison</title>
  <titleShort>soldier</titleShort>
  <description>[PAWN_nameDef] ... (2–4 предложения)</description>
  <skillGains>
    <Melee>3</Melee>
    <Medicine>2</Medicine>
  </skillGains>
  <workDisables>None</workDisables>
  <spawnCategories><li>TMM_Heroes</li></spawnCategories>
  <shuffleable>false</shuffleable>
</BackstoryDef>
```
Внимание: `skillGains` пишутся именно так — `<ИмяНавыка>число</ИмяНавыка>`, а не через key/value. Сумма на одну предысторию 4–8, не больше 5 на навык. `workDisables` — только `None`, `Artistic`, `Caring`, `Cleaning`, `Cooking`, `Crafting`, `Firefighting`, `Hauling`, `Intellectual`, `ManualDumb`, `ManualSkilled`, `Mining`, `PlantWork`, `Social`, `Animals`, через запятую. `Violent` не отключать.

## Файл 3 — `Languages/Russian/DefInjected/BackstoryDef/Backstories_Heaven.xml`
`title`, `titleShort`, `description` для всех 12 предысторий на живом русском, род по полу героя.

## Правила
- Тексты в духе сериала, без прямых цитат. Токены `[PAWN_nameDef]`, `[PAWN_pronoun]`, `[PAWN_possessive]`, `[PAWN_objective]` вместо имени.
- Ничего не придумывай про механику мода.

Выдай три файла целиком, каждый в своём блоке кода, с путём над блоком.
