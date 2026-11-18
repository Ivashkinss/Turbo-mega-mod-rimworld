Продолжаем по твоему ростеру. Сделай так же, как для дружелюбных супермутантов — те же три файла и те же правила полей (skillGains как `<Навык>число</Навык>`, сумма 4–8, не больше 5 на навык; `workDisables` из того же списка, `Violent` не отключать). Но для фракции **Дикие супермутанты**:

- `factionDef`: `TMM_WildMutantsFaction`
- `xenotype`: `TMM_SuperMutant`, `MayRequire="Ludeon.RimWorld.Biotech"` на каждом HeroDef
- герои из твоего ростера: Лебедь (лидер, rank 4, `isLeader`), Кувалда, Мёртвый Глаз, Безумный Гонщик, Мастер-мясник
- id: `swan`, `fist`, `dead_eye`, `mad_racer`, `master_butcher`
- `pawnKind`: Лебедю `PirateBoss`, остальным `Mercenary_Elite` или `Mercenary_Gunner`
- `weapon`: только `Gun_LMG`, `Gun_Minigun`, `Gun_AssaultRifle`, `Gun_SniperRifle`, `Gun_PumpShotgun`, `Gun_HeavySMG`, `MeleeWeapon_Mace`, `MeleeWeapon_Knife`; Мёртвому Глазу — снайперская
- навыки 6–14 (супермутанты учатся медленно), у Лебедя Melee до 18
- характер: дикие, жестокие, презирают людей; Лебедь — гигант-бегемот из глубин, почти легенда
- пол — по ростеру (если не указан, мужской)

Файлы: `Defs/HeroDefs/Heroes_WildMutants.xml`, `Defs/BackstoryDefs/Backstories_WildMutants.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_WildMutants.xml`. Три блока кода с путём над каждым, без пояснений.
