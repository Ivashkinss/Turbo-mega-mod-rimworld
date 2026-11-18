Отлично. Следующие две фракции из людей — правила те же (поля, skillGains `<Навык>число</Навык>`, сумма 4–8, не больше 5 на навык, `workDisables` из списка, без `Violent`, без xenotype и MayRequire, оружие из прошлого списка, навыки 8–16).

**1. Волт-Тек**
- `factionDef`: `TMM_VaultTecFaction`
- решение пользователя: Волт-Тек управляет **совет**, единого лидера нет. Поэтому никому не ставь `isLeader`. Членам совета — смотритель Альмодовар, смотритель Макнамара, доктор Брон — поставь `<councilMember>true</councilMember>` и rank 4, `pawnKind` `PirateBoss`.
- остальные: Представитель Волт-Тек, начальник охраны Кендалл — rank 3, `Mercenary_Elite` или `Mercenary_Gunner`
- id: `alphonse_almodovar`, `gwen_mcnamara`, `stanislaus_braun`, `vaulttec_rep`, `security_chief_kendall`
- Представитель Волт-Тек — гуль: пока оформляй его человеком, гулей сделаем позже
- файлы: `Defs/HeroDefs/Heroes_VaultTec.xml`, `Defs/BackstoryDefs/Backstories_VaultTec.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_VaultTec.xml`

**2. Британские Просвещённые** (британский орден Хранителей Знаний)
- `factionDef`: `TMM_MenOfLettersFaction`
- герои: доктор Хесс — лидер, rank 4, `isLeader`, `PirateBoss`; Артур Кетч, Мик Дэвис, Антония Бевелл, Кендрик
- id: `dr_hess`, `arthur_ketch`, `mick_davies`, `antonia_bevell`, `kendricks`
- остальным `Mercenary_Elite` или `Mercenary_Gunner`
- файлы: `Defs/HeroDefs/Heroes_MenOfLetters.xml`, `Defs/BackstoryDefs/Backstories_MenOfLetters.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_MenOfLetters.xml`

Шесть блоков кода с путём над каждым, без пояснений.
