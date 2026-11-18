# SuperMutantXenotype — ксенотип супермутантов по образцу демона и ангела

kind: implement + run · assignee: gemini

Образец — сделанные Claude ксенотипы демона и ангела:
- `Defs/XenotypeDefs/Xenotype_Demon.xml`, `Xenotype_Angel.xml`;
- RU-перевод: `Languages/Russian/DefInjected/XenotypeDef/`;
- probe `Tests/InGameDemonXenotypeProbe.cs` с режимом `-tmm-angel-xenotype-probe`;
- компонент `AngelXenotype` в `Tools/test_component.ps1`.

Решение пользователя: супермутанты — отдельная раса, только при Biotech (`MayRequire="Ludeon.RimWorld.Biotech"`).

**Размер:** пользователь хочет, чтобы супермутанты были визуально крупнее. Это делает Claude отдельным геном `TMM_Body_Giant` с патчем отрисовки и потом сам добавит его в ксенотип. Ты размер не трогай, просто оставь место в списке генов.

## Сделать (только эти файлы)
1. `Defs/XenotypeDefs/Xenotype_SuperMutant.xml`:
   - `TMM_SuperMutant`, label `super mutant`;
   - описание: огромные зелёные мутанты, выведенные вирусом FEV;
   - `iconPath UI/Icons/Xenotypes/Neanderthal`, `combatPowerFactor 1.4`;
   - гены, **ровно эти, все ванильные**: `Skin_Green`, `Body_Hulk`, `Hair_BaldOnly`, `Beard_NoBeardOnly`, `MeleeDamage_Strong`, `Robust`, `ToxResist_Total`, `ToxicEnvironmentResistance_Total`, `Pain_Reduced`, `Learning_Slow`.
   - Агрессию не добавлять: дружелюбные супермутанты не агрессивны, разница между дружелюбными и дикими — во фракции.
2. `Languages/Russian/DefInjected/XenotypeDef/Xenotype_SuperMutant.xml`: label «супермутант», description, descriptionShort.
3. `Tests/InGameDemonXenotypeProbe.cs`: режим `-tmm-supermutant-xenotype-probe` по образцу ангела:
   - `XenoName TMM_SuperMutant`;
   - `AppearanceGenes` — `Skin_Green`, `Body_Hulk`, `Hair_BaldOnly`;
   - `AllTextures` — пустой массив;
   - проверка: тип тела пешки `BodyTypeDefOf.Hulk`.
   Режимы демона и ангела не ломать.
4. `Tools/test_component.ps1`: компонент `SuperMutantXenotype` по образцу `AngelXenotype`. Файл в UTF-8 с BOM.

## Прогон
Только если RimWorld не запущен и в CodexWorkerState нет задач `running`; таймаут 15 минут. Не больше 2 попыток.
- `test_component.ps1 -Component SuperMutantXenotype -FullSet -UseBuiltAssembly`;
- для регрессии один раз `-Component AngelXenotype`.

Пройдено, только если `passed:true` и в stdout.log 0 строк ERROR/CRIT. **Во время прогона окно игры не трогать.**

## Сдача: `Tools/Build/GeminiSuperMutantXenotype/REPORT.md`
- дифф файлов;
- таблица прогонов: passed, checks, ERROR-строк;
- портреты: собери `*Portrait_*.bmp` из профиля в один `PORTRAITS.png` в папке задачи;
- самоаудит.

Не коммитить.
