Ты пишешь контент для мода RimWorld 1.7 с DLC Biotech: кроссовер Fallout × «Сверхъестественное». Нужны пять героев фракции «Дружелюбные супермутанты» — описание героя (HeroDef) и уникальные предыстории (BackstoryDef). Ответ — только три готовых XML-файла, без пояснений.

## Герои (все — ксенотип супермутанта `TMM_SuperMutant`, все мужчины)
| id | Имя | Ранг | Что важно |
|---|---|---|---|
| fox | Фокс | 4, лидер | разумный супермутант из Столичной Пустоши, бывший пленник Анклава, спокойный и рассудительный боец, лидер дружелюбных мутантов |
| uncle_leo | Дядя Лео | 3 | добродушный бродячий мутант-торговец, путешествует по Пустоши, любит рассказы и людей |
| strong | Силач | 3 | огромный и прямолинейный, мечтает о «молоке человеческой доброты», ценит силу и честный бой |
| erickson | Эриксон | 3 | бывший человек-учёный, сохранивший разум после мутации, замкнутый исследователь |
| virgil | Брайан Верджил | 3 | бывший учёный Института, мутировавший беглец, ищет лекарство от FEV, гениален и параноидален |

## Файл 1 — `Defs/HeroDefs/Heroes_FriendlyMutants.xml`
Образец, остальные по нему:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <TurboMegaMod.HeroDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hero_fox</defName>
    <childhood>TMM_BS_fox_child</childhood>
    <adulthood>TMM_BS_fox_adult</adulthood>
    <label>Fox</label>
    <factionDef>TMM_FriendlyMutantsFaction</factionDef>
    <rank>4</rank>
    <isLeader>true</isLeader>
    <pawnKind>Mercenary_Elite</pawnKind>
    <xenotype>TMM_SuperMutant</xenotype>
    <nameFirst>Fox</nameFirst><nameNick>Fox</nameNick><nameLast></nameLast>
    <gender>Male</gender>
    <ageRange>40~50</ageRange>
    <skills>
      <li><skill>Shooting</skill><level>14</level></li>
      <li><skill>Melee</skill><level>13</level></li>
    </skills>
    <weapon>Gun_LMG</weapon>
  </TurboMegaMod.HeroDef>
</Defs>
```
- `isLeader` только у Фокса. `pawnKind` — `Mercenary_Elite` или `Mercenary_Gunner`.
- `weapon` — только ванильное оружие: `Gun_LMG`, `Gun_Minigun`, `Gun_AssaultRifle`, `Gun_PumpShotgun`, `Gun_HeavySMG`, `MeleeWeapon_Mace`, `MeleeWeapon_Knife`; можно без оружия.
- Супермутанты медленно учатся, поэтому навыки ниже, чем у людей: 2–3 навыка, уровни 6–14. Учёным (Эриксону и Верджилу) Intellectual до 15.
- Других полей не добавляй.

## Файл 2 — `Defs/BackstoryDefs/Backstories_FriendlyMutants.xml`
Для каждого героя `TMM_BS_<id>_child` (slot Childhood — жизнь до мутации или первые годы после неё) и `TMM_BS_<id>_adult` (slot Adulthood). Образец:
```xml
<BackstoryDef>
  <defName>TMM_BS_fox_child</defName>
  <slot>Childhood</slot>
  <title>...</title>
  <titleShort>...</titleShort>
  <description>[PAWN_nameDef] ... (2–4 предложения)</description>
  <skillGains>
    <Shooting>3</Shooting>
    <Melee>2</Melee>
  </skillGains>
  <workDisables>None</workDisables>
  <spawnCategories><li>TMM_Heroes</li></spawnCategories>
  <shuffleable>false</shuffleable>
</BackstoryDef>
```
- `skillGains` именно как `<ИмяНавыка>число</ИмяНавыка>`. Сумма на одну предысторию 4–8, не больше 5 на навык.
- `workDisables` — только `None`, `Artistic`, `Caring`, `Cleaning`, `Cooking`, `Crafting`, `Firefighting`, `Hauling`, `Intellectual`, `ManualDumb`, `ManualSkilled`, `Mining`, `PlantWork`, `Social`, `Animals`, через запятую. `Violent` не отключать.

## Файл 3 — `Languages/Russian/DefInjected/BackstoryDef/Backstories_FriendlyMutants.xml`
`title`, `titleShort`, `description` для всех 10 предысторий на живом русском, мужской род.

## Правила
- Тексты в духе Fallout, без прямых цитат.
- Токены `[PAWN_nameDef]`, `[PAWN_pronoun]`, `[PAWN_possessive]`, `[PAWN_objective]` вместо имени.
- Ничего не придумывай про механику мода.

Выдай три файла целиком, каждый в своём блоке кода, с путём над блоком.
