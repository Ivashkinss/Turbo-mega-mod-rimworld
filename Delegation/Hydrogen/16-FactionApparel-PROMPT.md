Следующий блок для мода RimWorld 1.7 — **уникальная одежда и броня фракций**. Игрок открывает её исследованием или квестом фракции и потом крафтит сам. Нужны тексты, характеристики и путь открытия. Код не нужен. Ответ — один блок JSON без пояснений.

## Фракции
Это фракции из твоего каталога, ты их уже знаешь:
- Ад;
- Небеса;
- Братство Стали;
- Изгои;
- Анклав;
- Институт;
- Подземка;
- Стрелки;
- Волт-Тек;
- Минитмены и охотники;
- Британские Просвещённые;
- разумные гули;
- дружелюбные и дикие супермутанты;
- кланы нечисти.

## Что нужно
Для каждой фракции 2–3 предмета: узнаваемая одежда (комбинезон Убежища, плащ охотника, форма Анклава) и броня или головной убор. Отдельно отметь предметы для наших рас: супермутанту нужна одежда под его огромное тело, ангелу — под крылья.

Есть два пути открытия:
- **research** — исследование, доступное с самого начала, но дорогое;
- **quest** — награда за союз или задание фракции. Например, чертёж силовой брони Братства даётся только после союза.

## Формат
```json
{
  "apparel": [
    {
      "id": "vault_jumpsuit",
      "faction": "vault_tec",
      "label_en": "", "label_ru": "",
      "desc_en": "1-2 предложения, атмосферно", "desc_ru": "",
      "baseOn": "ванильный ThingDef для наследования (Apparel_FlakVest, Apparel_PowerArmor, Apparel_Duster, Apparel_CowboyHat...)",
      "layers": ["OnSkin | Middle | Shell | Overhead"],
      "bodyParts": ["Torso | Legs | Arms | FullHead | UpperHead"],
      "stats": { "ArmorRating_Sharp": 0, "ArmorRating_Blunt": 0, "ArmorRating_Heat": 0, "Insulation_Cold": 0, "Insulation_Heat": 0, "Mass": 0, "MarketValue": 0 },
      "equippedStatOffsets": { "SocialImpact": 0 },
      "cost": { "Synthread": 0, "Plasteel": 0, "ComponentIndustrial": 0 },
      "workbench": "TableMachining | ElectricTailoringBench | FabricationBench",
      "workToMake": 0,
      "unlock": "research | quest",
      "unlock_detail_ru": "название исследования и его предпосылки, или какой квест какой фракции",
      "special_ru": "особый эффект, если есть, и честно: ванильный XML или свой код",
      "forRace": "any | super_mutant_only | angel_only"
    }
  ],
  "research": [
    { "id": "", "label_en": "", "label_ru": "", "desc_en": "", "desc_ru": "", "cost": 0, "techLevel": "Industrial | Spacer | Ultra", "prerequisites": ["ванильные defName"] }
  ]
}
```
- Сравнивай характеристики с ванильной одеждой той же эпохи. Фракционная одежда интереснее, но не заметно сильнее ванильной, а квестовая чуть сильнее.
- Описания пиши для внешнего вида: по ним художник будет рисовать текстуры.
