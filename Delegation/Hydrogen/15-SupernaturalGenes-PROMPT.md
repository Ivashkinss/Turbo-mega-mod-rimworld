Следующий блок для мода RimWorld 1.7 (Biotech) — **совершенно новые гены для наших пяти сверхъестественных ксенотипов**. Нужны идеи, тексты и честное описание реализации. Код не нужен. Ответ — один блок JSON без пояснений.

## Ксенотипы и их нынешние гены
Сейчас они собраны из ванильных генов плюс наша внешность. У каждого есть ген метаболизма, который обнуляет сумму metabolism.
- **Небесный ангел** (heavenly angel): крылья, сияющие глаза, DeadCalm, Immunity_SuperStrong, WoundHealing_Fast, Pain_Reduced, PsychicAbility_Enhanced, Beauty_Pretty.
- **Демон** (demon): рога, хвост, чёрные глаза, кожа DeepRed, FireResistant, MaxTemp_LargeIncrease, Aggression_Aggressive.
- **Супермутант** (super mutant): голова, тело и рост ×2.2, зелёная кожа, MeleeDamage_Strong, Robust, ToxResist_Total, ToxicEnvironmentResistance_Total, Pain_Reduced, Learning_Slow.
- **Гуль пустошей** (wasteland ghoul): голова гуля, Ageless, ToxResist_Total, ToxicEnvironmentResistance_Total, Immunity_SuperStrong, Pain_Reduced, Beauty_VeryUgly.
- **Левиафан** (leviathan): голова-пасть, WoundHealing_SuperFast, MeleeDamage_Strong, Robust, ToxResist_Total, Aggression_HyperAggressive.

## Что нужно
Для каждого ксенотипа 3–4 **новых** гена, которых нет в ванили. Они должны отражать лор Fallout и «Сверхъестественного», например:
- ангел: благодать, изгнание демонов, исцеление касанием, истинный облик, который выжигает глаза смертным;
- демон: сделки на душу, вселение, дым, слабость к святой воде и соли;
- супермутант: ВРЭ, ярость, ношение тяжёлого оружия одной рукой;
- гуль: лечение радиацией и риск одичания;
- левиафан: пожирание, слабость к буре, превращение в копию съеденного.

В каждом наборе минимум **одна слабость** — ген с плюсом к метаболизму, как у ванильных «минусов». Это даёт баланс и интересную игру. Сделай 1–2 гена способностями с кнопкой, как ванильные ability-гены.

## Формат
```json
{
  "genes": [
    {
      "id": "angel_grace",
      "xenotype": "heavenly_angel | demon | super_mutant | wasteland_ghoul | leviathan",
      "label_en": "", "label_ru": "",
      "desc_en": "2 предложения: эффект + лор", "desc_ru": "",
      "kind": "passive | ability | weakness",
      "biostatMet": 0,
      "biostatCpx": 0,
      "effect": "коротко и с цифрами: что меняется в игре",
      "implementation_ru": "честно: что делается ванильными средствами (statOffsets, damageFactors, AbilityDef, HediffGiver), а где нужен свой C#",
      "counterplay_ru": "как с этим бороться игроку или врагу (для сильных генов)"
    }
  ],
  "notes_ru": "до 5 строк: общие идеи баланса и как гены сочетаются с героями и легендарным оружием (Кольт против нечисти, бура против левиафанов, святая вода против демонов)"
}
```
- Сравнивай силу с ванильными генами Biotech. Сверхъестественное сильнее, но с ценой: метаболизм, сложность или слабость.
- Гены не должны ломать баланс без цены: никаких «бессмертен» и «убивает взглядом».
- Тексты по-русски должны быть естественными, без кальки.
