Новая механика мода (RimWorld 1.7) — роли и ранги колонистов. Нужен контент-таблица, не код. Ответ — один блок JSON, без пояснений.

## Что придумали
- Колонисту вручную назначают роль. Роль сама ставит профильные работы на приоритет 1 и добавляет огонёк страсти к целевому навыку.
- Внутри роли есть ранги. Повышение — ритуал: нужен уровень навыка и счётчик выполненной работы.
- Ранг даёт умеренные бонусы. У командира — аура для своего отряда.
- У каждого ранга свой набор экипировки (это настроит игрок, тебе не нужно).

## Роли
- **Гражданские** (по 3 ранга): врач, строитель, ремесленник, животновод, фермер, повар.
- **Военные:** штурмовик — ранги рядовой, сержант, лейтенант, майор, полковник, генерал; командир отряда — с лейтенанта и выше.

## Формат JSON
```json
{
  "roles": [
    {
      "id": "cook",
      "group": "civil",
      "label_en": "cook", "label_ru": "повар",
      "skill": "Cooking",
      "workTypes": ["Cooking"],
      "ranks": [
        {
          "id": "cook_1", "label_en": "kitchen assistant", "label_ru": "помощник повара",
          "skillMin": 10,
          "record": "MealsCooked", "recordMin": 100,
          "stats": [ { "stat": "CookSpeed", "offset": 0.05 } ],
          "mood": 2,
          "desc_ru": "одно предложение", "desc_en": "one sentence"
        }
      ]
    }
  ]
}
```

## Правила
- `skill` — ванильные SkillDef: Shooting, Melee, Construction, Mining, Cooking, Plants, Animals, Crafting, Artistic, Medicine, Social, Intellectual.
- `workTypes` — ванильные WorkTypeDef: Doctor, Construction, Crafting, Smithing, Tailoring, Handling, Growing, PlantCutting, Cooking, Hunting, Mining, Warden, Research.
- `record` — только существующие ванильные RecordDef. Например: TimesTendedOther, ThingsConstructed, ThingsCrafted, AnimalsTamed, PlantsSown, PlantsHarvested, MealsCooked, Kills, KillsHumanlikes, DamageDealt. Если подходящего нет, поставь `null` и `recordMin` 0.
- Гражданские пороги навыка: 10 / 15 / 20. Военные: от 6 у рядового до 18 у генерала. Счётчики возрастают по рангам.
- `stats` — только ванильные StatDef: например CookSpeed, ConstructionSpeed, MedicalTendSpeed, MedicalTendQuality, PlantWorkSpeed, AnimalGatherYield, TameAnimalChance, GeneralLaborSpeed, ShootingAccuracyPawn, MeleeHitChance, MeleeDodgeChance, AimingDelayFactor, WorkSpeedGlobal. Бонусы умеренные: на ранг от +0.03 до +0.10.
- `mood` — бонус настроения от гордости за звание: от +1 до +6.
- У командира отряда добавь поле `"aura": { "radius": 9, "mood": X, "stats": [ ... ] }` для его рангов.
- Названия — короткие, атмосферные, в духе RimWorld; русские — естественные.
