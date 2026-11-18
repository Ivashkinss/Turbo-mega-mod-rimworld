Продолжаем по твоему ростеру. Те же три файла и те же правила полей, что для минитменов (skillGains как `<Навык>число</Навык>`, сумма 4–8, не больше 5 на навык; `workDisables` из того же списка, `Violent` не отключать). Фракция — **Братство Стали**:

- `factionDef`: `TMM_BrotherhoodFaction`
- все люди: без `xenotype` и без `MayRequire`
- герои: Артур Мэксон (лидер, rank 4, `isLeader`, `pawnKind` `PirateBoss`), Оуэн Лайонс, Сара Лайонс, паладин Данс, капитан Келлс, проктор Ингрэм
- id: `arthur_maxson`, `owen_lyons`, `sarah_lyons`, `paladin_danse`, `captain_kells`, `proctor_ingram`
- решение пользователя: Оуэн и Сара Лайонс — живые ветераны 3-го ранга под началом Мэксона, это осознанный кроссовер
- остальным `pawnKind` `Mercenary_Elite` или `Mercenary_Gunner`
- `weapon`: только `Gun_AssaultRifle`, `Gun_SniperRifle`, `Gun_LMG`, `Gun_Minigun`, `Gun_ChargeRifle`, `Gun_ChargeLance`, `Gun_HeavySMG`, `MeleeWeapon_Gladius`, `MeleeWeapon_Mace`; можно без оружия
- навыки 8–16. У Ингрэм — Crafting и Intellectual (механик силовой брони).
- Данс — синт, но пока оформляй его как обычного человека: синтов сделаем позже.

Файлы: `Defs/HeroDefs/Heroes_Brotherhood.xml`, `Defs/BackstoryDefs/Backstories_Brotherhood.xml`, `Languages/Russian/DefInjected/BackstoryDef/Backstories_Brotherhood.xml`. Три блока кода с путём над каждым, без пояснений.
