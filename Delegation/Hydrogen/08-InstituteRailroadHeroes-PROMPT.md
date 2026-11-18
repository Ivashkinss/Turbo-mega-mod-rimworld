Супер. Дальше — снова две фракции из людей, правила те же, что в прошлый раз: поля, skillGains `<Навык>число</Навык>`, сумма 4–8, не больше 5 на навык, `workDisables` из списка, без `Violent`, без xenotype и MayRequire, оружие из прошлого списка, навыки 8–16. Синтов и роботов пока оформляй как обычных людей — их представление сделаем позже.

**1. Институт**
- `factionDef`: `TMM_InstituteFaction`
- герои: Отец (Шон) — лидер, rank 4, `isLeader`, `PirateBoss`; Джастин Айо, X6-88, доктор Мэдисон Ли, Келлог, Алан Бине
- id: `father_shaun`, `justin_ayo`, `x6_88`, `madison_li`, `kellogg`, `alan_binet`
- остальным `Mercenary_Elite` или `Mercenary_Gunner`; учёным — высокий Intellectual
- файлы: `Defs/HeroDefs/Heroes_Institute.xml`, `Defs/BackstoryDefs/Backstories_Institute.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_Institute.xml`

**2. Подземка**
- `factionDef`: `TMM_RailroadFaction`
- герои: Дездемона — лидер, rank 4, `isLeader`, `PirateBoss`; Дьякон, Техник Том, Глория
- id: `desdemona`, `deacon`, `tinker_tom`, `glory`
- Ника Валентайна и ПАМ не делай — сделаем отдельно
- файлы: `Defs/HeroDefs/Heroes_Railroad.xml`, `Defs/BackstoryDefs/Backstories_Railroad.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_Railroad.xml`

Шесть блоков кода с путём над каждым, без пояснений.
