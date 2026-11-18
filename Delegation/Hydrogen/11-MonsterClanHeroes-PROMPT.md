Последняя на сегодня группа — **Кланы нечисти**. Правила полей те же, что для гулей (skillGains `<Навык>число</Навык>`, сумма 4–8, не больше 5 на навык; `workDisables` из списка, без `Violent`; оружие из прошлого списка или без оружия; навыки 8–18). На каждом HeroDef атрибут `MayRequire="Ludeon.RimWorld.Biotech"`.

- `factionDef`: `TMM_MonsterClansFaction`
- герои: Альфа-вампир — лидер, rank 4, `isLeader`, `PirateBoss`; Дик Роман, Ева, Бенни Лафит, Эдгар
- id: `alpha_vampire`, `dick_roman`, `eve`, `benny_lafitte`, `edgar`
- ксенотип:
  - вампиры (Альфа-вампир, Бенни) — ванильный `Sanguophage`;
  - левиафаны (Дик Роман, Эдгар) и Ева — `TMM_Leviathan`. Этот ксенотип сделаем отдельно, просто укажи его.
- Дик Роман — rank 3, но сильнее всех; его неуязвимость сделаем механикой позже, в тексте её можно упомянуть
- остальным `Mercenary_Elite`
- файлы: `Defs/HeroDefs/Heroes_MonsterClans.xml`, `Defs/BackstoryDefs/Backstories_MonsterClans.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_MonsterClans.xml`

Три блока кода с путём над каждым, без пояснений.
