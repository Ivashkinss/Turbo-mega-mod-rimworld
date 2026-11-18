Ты пишешь контент для мода RimWorld 1.7 с DLC Biotech: кроссовер Fallout × «Сверхъестественное». Нужны четыре героя фракции «Минитмены и охотники» — описание героя (HeroDef) и уникальные предыстории (BackstoryDef). Ответ — только три готовых XML-файла, без пояснений.

## Герои (все люди, без ксенотипа)
| id | Имя | Пол | Ранг | Что важно |
|---|---|---|---|---|
| bobby_singer | Бобби Сингер | М | 3 | старый охотник, хозяин автосвалки, мастер по машинам и знаток лора, «приёмный отец» Винчестеров |
| ronnie_shaw | Ронни Шоу | Ж | 3 | ветеран минитменов, бывший командир Замка, суровая и прямолинейная, разбирается в обороне и артиллерии |
| rufus_turner | Руфус Тёрнер | М | 3 | охотник-ветеран, специалист по ловушкам, ворчливый одиночка, бывший сотрудник спецслужб |
| ellen_harvelle | Эллен Харвелл | Ж | 3 | хозяйка придорожного бара «Дорожный дом», наставница охотников, мать Джо, отлично стреляет из дробовика |

## Файл 1 — `Defs/HeroDefs/Heroes_MinutemenMore.xml`
Образец, остальные по нему:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <TurboMegaMod.HeroDef>
    <defName>TMM_Hero_bobby_singer</defName>
    <childhood>TMM_BS_bobby_singer_child</childhood>
    <adulthood>TMM_BS_bobby_singer_adult</adulthood>
    <label>Bobby Singer</label>
    <factionDef>TMM_MinutemenHuntersFaction</factionDef>
    <rank>3</rank>
    <pawnKind>TMM_HuntersHunter</pawnKind>
    <nameFirst>Robert</nameFirst><nameNick>Bobby</nameNick><nameLast>Singer</nameLast>
    <gender>Male</gender>
    <ageRange>55~62</ageRange>
    <skills>
      <li><skill>Crafting</skill><level>14</level></li>
      <li><skill>Intellectual</skill><level>13</level></li>
      <li><skill>Shooting</skill><level>10</level></li>
    </skills>
    <weapon>Gun_PumpShotgun</weapon>
  </TurboMegaMod.HeroDef>
</Defs>
```
- `pawnKind` — только `TMM_HuntersMilitiaman` (ополченец), `TMM_HuntersHunter` (охотник) или `TMM_HuntersMedic` (медик). Выбери по роли.
- `weapon` — только ванильное оружие: `Gun_Revolver`, `Gun_Autopistol`, `Gun_PumpShotgun`, `Gun_ChainShotgun`, `Gun_BoltActionRifle`, `Gun_AssaultRifle`, `Gun_SniperRifle`, `MeleeWeapon_Knife`.
- `skills` — 2–3 навыка, уровни 8–16.
- Атрибут `MayRequire` не нужен. Других полей не добавляй.

## Файл 2 — `Defs/BackstoryDefs/Backstories_MinutemenMore.xml`
Для каждого героя `TMM_BS_<id>_child` (slot Childhood) и `TMM_BS_<id>_adult` (slot Adulthood):
```xml
<BackstoryDef>
  <defName>TMM_BS_bobby_singer_child</defName>
  <slot>Childhood</slot>
  <title>...</title>
  <titleShort>...</titleShort>
  <description>[PAWN_nameDef] ... (2–4 предложения)</description>
  <skillGains>
    <Crafting>3</Crafting>
    <Intellectual>2</Intellectual>
  </skillGains>
  <workDisables>None</workDisables>
  <spawnCategories><li>TMM_Heroes</li></spawnCategories>
  <shuffleable>false</shuffleable>
</BackstoryDef>
```
- `skillGains` пишутся именно как `<ИмяНавыка>число</ИмяНавыка>`. Сумма на одну предысторию 4–8, не больше 5 на навык.
- `workDisables` — только `None`, `Artistic`, `Caring`, `Cleaning`, `Cooking`, `Crafting`, `Firefighting`, `Hauling`, `Intellectual`, `ManualDumb`, `ManualSkilled`, `Mining`, `PlantWork`, `Social`, `Animals`, через запятую. `Violent` не отключать.

## Файл 3 — `Languages/Russian/DefInjected/BackstoryDef/Backstories_MinutemenMore.xml`
`title`, `titleShort`, `description` для всех 8 предысторий на живом русском, род по полу героя.

## Правила
- Тексты в духе первоисточников, без прямых цитат.
- Токены `[PAWN_nameDef]`, `[PAWN_pronoun]`, `[PAWN_possessive]`, `[PAWN_objective]` вместо имени.
- Ничего не придумывай про механику мода.

Выдай три файла целиком, каждый в своём блоке кода, с путём над блоком.
