Отлично получилось. Дальше — две фракции из людей сразу, по тем же правилам, что для Братства: те же поля, skillGains как `<Навык>число</Навык>`, сумма 4–8, не больше 5 на навык; `workDisables` из того же списка, `Violent` не отключать. Без xenotype и без MayRequire. Оружие только из этого списка: `Gun_Revolver`, `Gun_Autopistol`, `Gun_MachinePistol`, `Gun_PumpShotgun`, `Gun_ChainShotgun`, `Gun_BoltActionRifle`, `Gun_AssaultRifle`, `Gun_SniperRifle`, `Gun_LMG`, `Gun_Minigun`, `Gun_HeavySMG`, `Gun_ChargeRifle`, `Gun_ChargeLance`, `MeleeWeapon_Knife`, `MeleeWeapon_Gladius`. Навыки 8–16.

**1. Стрелки** (наёмники)
- `factionDef`: `TMM_GunnersFaction`
- герои: капитан Уэс (лидер, rank 4, `isLeader`, `PirateBoss`), Клинт, Тесса, капитан Бриджит, Бейкер
- id: `captain_wes`, `clint`, `tessa`, `captain_bridget`, `baker`
- остальным `Mercenary_Elite` или `Mercenary_Gunner`
- файлы: `Defs/HeroDefs/Heroes_Gunners.xml`, `Defs/BackstoryDefs/Backstories_Gunners.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_Gunners.xml`

**2. Анклав**
- `factionDef`: `TMM_EnclaveFaction`
- герои: полковник Отем (лидер, rank 4, `isLeader`, `PirateBoss`), доктор Чарльз Керлинг, сержант Дорнан, капитан Ричардс
- id: `colonel_autumn`, `dr_curling`, `sergeant_dornan`, `captain_richards`
- Джона Генри Эдема не делай: он ИИ-терминал, его сделаем отдельно
- остальным `Mercenary_Elite` или `Mercenary_Gunner`
- файлы: `Defs/HeroDefs/Heroes_Enclave.xml`, `Defs/BackstoryDefs/Backstories_Enclave.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_Enclave.xml`

Шесть блоков кода с путём над каждым, без пояснений.
