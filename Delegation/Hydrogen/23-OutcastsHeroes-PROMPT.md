**Режим: обычный чат** (три отдельных файла).

У нас обнаружилась дыра: фракции **Изгои** (Outcasts) в моде ещё нет, а в твоих квестах она есть (Касдин, Олин, Сайлас). Сделай её героев. Те же три файла и те же правила полей, что для Братства Стали в блоке 06:
- skillGains как `<Навык>число</Навык>`, сумма 4–8, не больше 5 на навык;
- `workDisables` из того же списка, `Violent` не отключать.

- `factionDef`: `TMM_OutcastsFaction`
- все люди: без `xenotype` и без `MayRequire`
- **Герои (5–6):**
  - защитник Генри Касдин — лидер, rank 4, `isLeader`, `pawnKind` `PirateBoss`;
  - специалист Олин — rank 3, главный техник и квартирмейстер. Женщина средних лет в чёрно-красном разведкостюме, лицо перемазано маслом, сварочные очки на лбу;
  - писец Сайлас — rank 3, молодой писец-аналитик и будущий перебежчик. Худощавый юноша в тёмно-алой тунике с переносным терминалом;
  - ещё 2–3 героя на твой выбор, кто нужен фракции по сюжету: например, рыцарь-капитан и паладин-ветеран.
- `successor` Касдина — тот, кто по сюжету возглавит Изгоев после него.
- **id:** `henry_casdin`, `specialist_olin`, `scribe_silas` и id остальных латиницей в том же стиле.
- Остальным героям `pawnKind` `Mercenary_Elite` или `Mercenary_Gunner`.
- **`weapon`** — только из списка: `Gun_AssaultRifle`, `Gun_SniperRifle`, `Gun_LMG`, `Gun_Minigun`, `Gun_ChargeRifle`, `Gun_ChargeLance`, `Gun_HeavySMG`, `MeleeWeapon_Gladius`, `MeleeWeapon_Mace`. Можно без оружия.
- **Навыки 8–16.** У Олин — Crafting и Intellectual, у Сайласа — Intellectual и Social.
- **Биографии** (детство и взрослая жизнь) должны совпадать с твоими квестами Изгоев из библии: склеп под госпиталем, украденные чертежи, побег Сайласа.

**Файлы:**
- `Defs/HeroDefs/Heroes_Outcasts.xml`
- `Defs/BackstoryDefs/Backstories_Outcasts.xml`
- `Languages/Russian/DefInjected/BackstoryDef/Backstories_Outcasts.xml`

Три блока кода с путём над каждым, без пояснений.
