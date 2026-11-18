Отлично. Следующая фракция — **Разумные гули**. Правила полей те же (skillGains `<Навык>число</Навык>`, сумма 4–8, не больше 5 на навык; `workDisables` из списка, без `Violent`; оружие из прошлого списка; навыки 8–16).

- `factionDef`: `TMM_GhoulsFaction`
- гули — отдельная раса: каждому HeroDef `<xenotype>TMM_Ghoul</xenotype>` и атрибут `MayRequire="Ludeon.RimWorld.Biotech"`
- герои: Джон Хэнкок — лидер, rank 4, `isLeader`, `PirateBoss`; Кент Коннолли, Харон, Эдвард Диган, Дейзи, Гоб
- id: `john_hancock`, `kent_connolly`, `charon`, `edward_deegan`, `daisy`, `gob`
- остальным `Mercenary_Elite` или `Mercenary_Gunner`; Дейзи и Гоб — торговец и медик, у них Social, Medicine, Cooking
- гули живут веками: возраст «сосуда» указывай как видимый, а в предысториях отрази, что они помнят довоенный мир (кроме тех, кто стал гулем позже)
- файлы: `Defs/HeroDefs/Heroes_Ghouls.xml`, `Defs/BackstoryDefs/Backstories_Ghouls.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_Ghouls.xml`

Три блока кода с путём над каждым, без пояснений.
