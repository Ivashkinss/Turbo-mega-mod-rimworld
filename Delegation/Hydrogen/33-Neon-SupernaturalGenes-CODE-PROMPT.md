**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

Новая задача по коду мода RimWorld **1.7** (Biotech): **18 новых сверхъестественных генов** для наших пяти рас:
- небесный ангел `TMM_Angel`;
- демон `TMM_Demon`;
- супермутант `TMM_SuperMutant`;
- гуль пустошей `TMM_Ghoul`;
- левиафан `TMM_Leviathan`.

Дизайн генов придумал Hydrogen, он ниже в JSON. Нужен рабочий XML и C#. Код соберёт и проверит в игре наш агент.

## Жёсткие правила
- **Только C# 5:** без `?.`, `=>` у членов класса, `nameof`, `is X x`, `$""""`, `out var`. Лямбды в аргументах можно.
- API — RimWorld 1.7. Где не уверен, ставь `// CHECK API`.
- Пространство имён `TurboMegaMod`. Все Def с `MayRequire="Ludeon.RimWorld.Biotech"`. Префикс defName `TMM_`.
- Где эффект делается ванильным XML (`statOffsets`, `statFactors`, `damageFactors`, `capMods`, `aptitudes`, `AbilityDef` + `CompProperties_AbilityEffect`), **используй XML, а не код**. C# пиши только там, где без него нельзя.
- Иконки бери ванильные (`UI/Icons/Genes/...`) или ванильные иконки способностей, `// CHECK API` где не уверен.

## Решения Claude по балансу (обязательно)
- «Небесная благодать»: скорость движения **+15%**, а не +25%.
- Супермутанту «громоздкая физиология»: голод и бесплодие делай, а **запрет обычной одежды пока НЕ делай**. Одежды мутантов ещё нет, и мутант остался бы голым. Оставь в коде комментарий-заглушку.
- **Слабости к оружию, которого ещё нет** (святая вода, соль, енохианские клинки, бура, «Уравнитель»): создай **DamageDef-ы** `TMM_HolyWater`, `TMM_RockSalt`, `TMM_Enochian`, `TMM_BoraxBurn` (наследуй от ванильных подходящих) и вешай слабость через `damageFactors` на эти DamageDef. Оружие сделаем позже, и эффект сработает сам.
- Глушилки Просвещённых, пентаграммы и маяки пока не делай. Где слабость от них зависит, оставь только DamageDef-часть и комментарий.
- **Метаболизм каждой расы должен остаться 0.** После добавления новых генов пересчитай `biostatMet` наших генов метаболизма, текущие значения ниже, и выдай исправленный файл. Ванильные гены ксенотипов смотри в их XenotypeDef. Например, у ангела сейчас: `TMM_Wings_Angel`, `TMM_Eyes_Angel`, `Aggression_DeadCalm`(−1), `Immunity_SuperStrong`(−2), `WoundHealing_Fast`(−2), `Pain_Reduced`(−1), `PsychicAbility_Enhanced`(−2), `Beauty_Pretty`(−1) — сумма −9 закрыта `TMM_Metabolism_Angelic`(+9). Суммы остальных: демон −2, супермутант −10, гуль −8, левиафан −8.
- **«Хищная мимикрия» левиафана** (подмена внешности) — самая сложная. Сделай её упрощённо: способность даёт Hediff маскировки на 48 часов (бонус к социалке и торговле) и копирует **только** цвет кожи и волосы цели. Одежду не копируй. Внешность восстанавливай по окончании.

## Дизайн генов (от Hydrogen)
```json
[
    {
        "id":  "angel_celestial_grace",
        "xenotype":  "heavenly_angel",
        "label_en":  "celestial grace",
        "label_ru":  "небесная благодать",
        "desc_en":  "Carriers of divine grace move with unnatural speed, shrug off bleeding, and resist mortal aging. This radiant energy binds the angel to its human vessel and fuels its supernatural powers.",
        "desc_ru":  "Носители божественной благодати двигаются с нечеловеческой скоростью, почти не истекают кровью и не стареют. Эта сияющая энергия удерживает ангела в человеческом сосуде и питает его силы.",
        "kind":  "passive",
        "biostatMet":  -4,
        "biostatCpx":  4,
        "effect":  "+25% к скорости движения, -80% к скорости кровотечения, +30% к психочувствительности, полное бессмертие от старости.",
        "implementation_ru":  "Ванильный XML: statOffsets (MoveSpeed, PsychicSensitivity), statFactors (BleedRate = 0.2), биологическое старение отключено через biologicalAgeTickFactorFromAgeCurve.",
        "counterplay_ru":  "Сильное кровотечение или потеря сознания от ангельского клинка, «Уравнителя» или «Кольта» мгновенно обходят защиту."
    },
    {
        "id":  "angel_healing_touch",
        "xenotype":  "heavenly_angel",
        "label_en":  "healing touch",
        "label_ru":  "исцеление касанием",
        "desc_en":  "By pressing two fingers to a mortal\u0027s forehead, the angel channels pure grace to mend torn flesh and purge infections. The effort drains the angel\u0027s reserves and requires time to recover.",
        "desc_ru":  "Прикоснувшись двумя пальцами ко лбу смертного, ангел направляет поток благодати, затягивая раны и выжигая инфекции. Это требует больших сил и времени на восстановление.",
        "kind":  "ability",
        "biostatMet":  -2,
        "biostatCpx":  3,
        "effect":  "Способность (перезарядка 2 дня): мгновенно закрывает 3–4 свежие раны цели и снижает тяжесть инфекций и токсинов на 50%.",
        "implementation_ru":  "Ванильный AbilityDef с дальностью касания (1.5 клетки) + свой C# (CompAbilityEffect), перебирающий Hediff_Injury и HediffWithComps (инфекции/токсины) на цели.",
        "counterplay_ru":  "Не воскрешает мёртвых, не восстанавливает отрубленные конечности и блокируется енохианскими глушилками."
    },
    {
        "id":  "angel_true_visage",
        "xenotype":  "heavenly_angel",
        "label_en":  "true visage",
        "label_ru":  "истинный облик",
        "desc_en":  "The angel briefly lets its overwhelming celestial light flare through its vessel\u0027s eyes. Mortals caught in the radiance suffer searing retinal burns and temporary blindness.",
        "desc_ru":  "Ангел на миг приоткрывает свой ослепительный небесный облик сквозь глаза сосуда. Смертные, попавшие в вспышку, получают ожоги сетчатки и временно слепнут.",
        "kind":  "ability",
        "biostatMet":  -3,
        "biostatCpx":  4,
        "effect":  "Способность (перезарядка 3 дня): в радиусе 5 клеток наносит ожоги глаз (снижение зрения на 70% на 6 часов) и оглушает всех, кроме ангелов.",
        "implementation_ru":  "Ванильный AbilityDef с радиусом 5 клеток + свой C# (CompAbilityEffect), проверяющий линию видимости (GenSight.LineOfSight) и вешающий временный Hediff ожога глаз на цели без генов ангела.",
        "counterplay_ru":  "Не действует сквозь стены, не влияет на других ангелов, турели и слепых бойцов."
    },
    {
        "id":  "angel_enochian_vulnerability",
        "xenotype":  "heavenly_angel",
        "label_en":  "enochian vulnerability",
        "label_ru":  "уязвимость к енохианским знакам",
        "desc_en":  "Ancient Enochian sigils and celestial bronze disrupt the angel\u0027s grace, stripping away its supernatural resilience. Near warding beacons or under enchanted blades, an angel bleeds like any mortal.",
        "desc_ru":  "Древние енохианские символы и небесная бронза нарушают связь ангела с благодатью. Рядом с оккультными глушилками и под ударами зачарованных клинков он становится уязвим как обычный смертный.",
        "kind":  "weakness",
        "biostatMet":  3,
        "biostatCpx":  2,
        "effect":  "+150% к входящему урону от ангельских клинков и оружия с енохианскими рунами; в радиусе глушилок Просвещённых отключаются способности.",
        "implementation_ru":  "Ванильный damageFactors для кастомных DamageDef + свой C# (HediffComp или проверка в CompAbilityEffect) на наличие активного маяка-глушилки на карте.",
        "counterplay_ru":  "Для игрока за ангелов — уничтожать енохианские глушилки издалека; для охотников — нанести руны на оружие на военном столе."
    },
    {
        "id":  "demon_black_smoke",
        "xenotype":  "demon",
        "label_en":  "black smoke shift",
        "label_ru":  "рывок чёрным дымом",
        "desc_en":  "The demon momentarily dissolves into a billowing cloud of sulfur-scented black smoke to cross obstacles. Reforming abruptly disorients nearby foes.",
        "desc_ru":  "Демон на мгновение распадается на клубы чёрного серного дыма, преодолевая препятствия. Резкое возвращение в плоть дезориентирует ближайших врагов.",
        "kind":  "ability",
        "biostatMet":  -3,
        "biostatCpx":  3,
        "effect":  "Способность (перезарядка 1.5 дня): быстрый перелёт/рывок на дистанцию до 12 клеток с оглушением врагов в радиусе 2 клеток на 2 секунды.",
        "implementation_ru":  "Ванильный AbilityDef на базе PawnFlyer (как прыжок сангвофага в Biotech) с кастомным Fleck чёрного дыма и эффектом Stun при приземлении.",
        "counterplay_ru":  "Не позволяет пересекать стены с нанесёнными енохианскими оберегами и мгновенно прерывается, если приземлиться в дьявольскую ловушку."
    },
    {
        "id":  "demon_mind_cloud",
        "xenotype":  "demon",
        "label_en":  "infernal whisper",
        "label_ru":  "адский дурман",
        "desc_en":  "Projecting dark psychic static into a victim\u0027s mind, the demon clouds their thoughts with dread and confusion. Weak-willed targets struggle to aim or react.",
        "desc_ru":  "Насылая тёмный ментальный туман в разум жертвы, демон затуманивает её сознание страхом и растерянностью. Слабовольные цели с трудом держат оружие и медленно реагируют.",
        "kind":  "ability",
        "biostatMet":  -2,
        "biostatCpx":  3,
        "effect":  "Способность (перезарядка 2 дня, дальность 10 клеток): снижает Сознание цели на 25% и скорость работы/стрельбы на 30% на 8 часов.",
        "implementation_ru":  "Ванильный AbilityDef, накладывающий кастомный Hediff с capMods (Consciousness -25%) и statOffsets.",
        "counterplay_ru":  "Полностью блокируется антидемоническим амулетом, татуировкой от одержимости, Клинком Архангела или высокой психоглухотой."
    },
    {
        "id":  "demon_crossroads_guile",
        "xenotype":  "demon",
        "label_en":  "crossroads guile",
        "label_ru":  "хитрость перекрёстка",
        "desc_en":  "Centuries of bargaining for mortal souls have honed the demon\u0027s silver tongue and predatory charm. They effortlessly sway traders and intimidate prisoners.",
        "desc_ru":  "Столетия торговли смертными душами отточили язык и хищное обаяние демона. Он без труда склоняет торговцев к невыгодным сделкам и ломает волю пленников.",
        "kind":  "passive",
        "biostatMet":  -2,
        "biostatCpx":  2,
        "effect":  "+6 к навыку Общения, +20% к улучшению торговых цен и +25% к силе подавления/вербовки пленников.",
        "implementation_ru":  "Полностью на ванильном XML: aptitude (Social +6) и statOffsets (TradePriceImprovement, NegotiationAbility, SuppressionPower).",
        "counterplay_ru":  "Не влияет на бой; в колонии демоны с этим геном склонны провоцировать социальные конфликты с набожными или честными пешками."
    },
    {
        "id":  "demon_holy_bane",
        "xenotype":  "demon",
        "label_en":  "bane of salt and holy water",
        "label_ru":  "проклятие соли и святой воды",
        "desc_en":  "Consecrated water sears demonic flesh like boiling acid, while rock salt blasts tear through their vessel\u0027s endurance. Stepping into a painted Devil\u0027s Trap binds them completely.",
        "desc_ru":  "Освящённая вода жжёт демоническую плоть словно кипящая кислота, а заряды каменной соли разрывают выносливость сосуда. Попав в дьявольскую ловушку, демон теряет способность двигаться.",
        "kind":  "weakness",
        "biostatMet":  4,
        "biostatCpx":  2,
        "effect":  "+200% к урону от святой воды, каменной соли и клинка Руби; попадание в пентаграмму на полу обездвиживает на 30 секунд.",
        "implementation_ru":  "Ванильный damageFactors для кастомных DamageDef (TMM_HolyWater, TMM_RockSalt) + реакция кастомного Building_Trap на тег демона.",
        "counterplay_ru":  "Демонам стоит избегать узких коридоров с ловушками и уничтожать охотников с дробовиками на дальней дистанции."
    },
    {
        "id":  "mutant_fev_musculature",
        "xenotype":  "super_mutant",
        "label_en":  "FEV titan frame",
        "label_ru":  "титаническая мускулатура ВРЭ",
        "desc_en":  "Forced Evolutionary Virus hyper-densifies muscle fibers and bone structure. Super mutants effortlessly haul massive loads and handle heavy machine guns like light carbines.",
        "desc_ru":  "Вирус рукотворной эволюции многократно уплотняет мышечные волокна и кости. Супермутанты легко переносят огромные грузы и обращаются с тяжёлыми пулемётами как с лёгкими карабинами.",
        "kind":  "passive",
        "biostatMet":  -3,
        "biostatCpx":  3,
        "effect":  "+150 кг к грузоподъёмности, -20% к задержке прицеливания (AimingDelayFactor) и +25% к урону в ближнем бою.",
        "implementation_ru":  "Ванильный XML: statOffsets (CarryingCapacity +150), statFactors (AimingDelayFactor = 0.8, MeleeDamageFactor = 1.25).",
        "counterplay_ru":  "Огромный силуэт увеличивает шанс попадания по супермутанту в перестрелке (+20% к уязвимости для стрелков)."
    },
    {
        "id":  "mutant_rad_regeneration",
        "xenotype":  "super_mutant",
        "label_en":  "irradiated metabolism",
        "label_ru":  "радиационная регенерация",
        "desc_en":  "Mutated cellular repair mechanisms thrive on ambient radiation. Instead of suffering sickness during toxic fallout, the super mutant\u0027s wounds knit shut rapidly.",
        "desc_ru":  "Мутировавшие клетки используют радиацию как катализатор деления. Вместо лучевой болезни во время токсичных осадков раны супермутанта затягиваются прямо на глазах.",
        "kind":  "passive",
        "biostatMet":  -2,
        "biostatCpx":  3,
        "effect":  "При наличии токсичного загрязнения местности или радиации скорость заживления ран вырастает ещё на +150%, а настроение повышается на +6.",
        "implementation_ru":  "Свой C# (HediffComp или ThoughtWorker): проверяет pawn.Position.IsPolluted(pawn.Map) или активный GameCondition_ToxicFallout и включает усиливающий Hediff.",
        "counterplay_ru":  "В чистых, нерадиоактивных помещениях бонус отключается."
    },
    {
        "id":  "mutant_war_roar",
        "xenotype":  "super_mutant",
        "label_en":  "mutant war roar",
        "label_ru":  "боевой рёв мутанта",
        "desc_en":  "Letting out an earth-shaking bellow, the super mutant floods its bloodstream with adrenaline while terrifying nearby human enemies.",
        "desc_ru":  "Издавая оглушительный рёв, супермутант выбрасывает в кровь ударную дозу адреналина и наводит ужас на ближайших врагов-людей.",
        "kind":  "ability",
        "biostatMet":  -2,
        "biostatCpx":  2,
        "effect":  "Способность (перезарядка 1 день): на 20 секунд даёт носителю +25% к скорости в ближнем бою и замедляет врагов в радиусе 6 клеток на 20%.",
        "implementation_ru":  "Ванильный AbilityDef, вешающий положительный Hediff на кастера и отрицательный Hediff (страх/замедление) на враждебных пешек в радиусе 6 клеток.",
        "counterplay_ru":  "Не действует на роботов, синтов, гулей, ангелов и демонов."
    },
    {
        "id":  "mutant_bulky_constraints",
        "xenotype":  "super_mutant",
        "label_en":  "bulky FEV physiology",
        "label_ru":  "громоздкая физиология ВРЭ",
        "desc_en":  "Extreme FEV mutation leaves the carrier completely sterile, ravenous for calories, and far too massive to fit into standard human clothing or armor.",
        "desc_ru":  "Глубокая мутация ВРЭ делает носителя бесплодным, вечно голодным и слишком огромным для обычной человеческой одежды и брони.",
        "kind":  "weakness",
        "biostatMet":  3,
        "biostatCpx":  2,
        "effect":  "Полное бесплодие, +35% к скорости голода (HungerRateFactor = 1.35), запрет на ношение стандартной человеческой одежды (только броня мутантов).",
        "implementation_ru":  "Ванильный XML: sterility = true, statFactors (HungerRateFactor = 1.35) + Harmony-патч на Apparels.CanWearWithoutDroppingAnything для проверки размера одежды.",
        "counterplay_ru":  "Требует отдельного производства или трофейной брони супермутантов и повышенных запасов еды в колонии."
    },
    {
        "id":  "ghoul_rad_healing",
        "xenotype":  "wasteland_ghoul",
        "label_en":  "necrotic radiation absorption",
        "label_ru":  "поглощение радиации",
        "desc_en":  "Radiation stimulates a ghoul\u0027s necrotic tissue, rapidly knitting torn flesh and easing chronic joint pain. What kills smoothskins serves as medicine for them.",
        "desc_ru":  "Радиация стимулирует некротические ткани гуля, быстро затягивая раны и унимая вечную боль в суставах. То, что убивает «гладкокожих», для гуля — лучшее лекарство.",
        "kind":  "passive",
        "biostatMet":  -3,
        "biostatCpx":  3,
        "effect":  "Нахождение в загрязнённой зоне, под токсичными осадками или облучение гамма-пушкой лечит 15 единиц случайных ран каждые 4 часа.",
        "implementation_ru":  "Свой C# (HediffComp_Tick): проверяет загрязнение клетки/осадки или попадание снарядов с тегом Radiation и вызывает HealthUtility.AdjustSeverity / заживление Hediff_Injury.",
        "counterplay_ru":  "Применение Антирадина (RadAway) лишает гуля этого эффекта на 24 часа."
    },
    {
        "id":  "ghoul_glowing_pulse",
        "xenotype":  "wasteland_ghoul",
        "label_en":  "glowing one\u0027s pulse",
        "label_ru":  "радиационный импульс светящегося",
        "desc_en":  "Channeling accumulated nuclear energy, the ghoul unleashes a blinding green wave of radiation that mends nearby mutants and ghouls while sickening humans.",
        "desc_ru":  "Высвобождая накопленную ядерную энергию, гуль испускает ослепительную зелёную волну радиации, исцеляющую соседних гулей и мутантов, но отравляющую людей.",
        "kind":  "ability",
        "biostatMet":  -2,
        "biostatCpx":  3,
        "effect":  "Способность (перезарядка 2 дня, радиус 4.5 клетки): мгновенно лечит союзных гулей/супермутантов и наносит +25% ToxicBuildup всем людям в радиусе.",
        "implementation_ru":  "Ванильный AbilityDef с зелёным Fleck-взрывом + свой C# (CompAbilityEffect), разделяющий цели по ксенотипу (лечение для TMM_Ghoul/TMM_SuperMutant, токсины для остальных).",
        "counterplay_ru":  "Не действует на цели в костюмах химзащиты, силовой броне или с иммунитетом к токсинам."
    },
    {
        "id":  "ghoul_feral_risk",
        "xenotype":  "wasteland_ghoul",
        "label_en":  "feral degeneration risk",
        "label_ru":  "риск одичания",
        "desc_en":  "Centuries of brain necrosis leave every ghoul walking a razor\u0027s edge between sanity and feral madness. Extreme stress or prolonged mental breaks can trigger a mindless frenzy.",
        "desc_ru":  "Века некроза мозга заставляют каждого гуля балансировать на грани рассудка и животного безумия. Тяжёлый стресс или затяжной нервный срыв могут толкнуть его к необратимому одичанию.",
        "kind":  "weakness",
        "biostatMet":  3,
        "biostatCpx":  3,
        "effect":  "При тяжёлом нервном срыве (Extreme Break) есть 25% шанс впасть в особое состояние «Приступ одичания» (атакует всех ближайших людей, лечится седацией или Антирадином).",
        "implementation_ru":  "Свой C# (Harmony-патч на MentalStateHandler.TryStartMentalState): при экстремальном срыве с шансом 25% подменяет срыв на кастомный MentalState_FeralGhoul.",
        "counterplay_ru":  "Поддержание хорошего настроения, общение и регулярный приём препаратов полностью предотвращают приступы."
    },
    {
        "id":  "leviathan_devour_maw",
        "xenotype":  "leviathan",
        "label_en":  "abyssal devouring maw",
        "label_ru":  "пасть бездны",
        "desc_en":  "Unhinging its human disguise into a cavernous maw lined with serrated teeth, the Leviathan tears out massive chunks of flesh to instantly replenish strength and hunger.",
        "desc_ru":  "Распахивая человеческую личину в бездонную пасть с рядами бритвенных зубов, левиафан вырывает куски плоти, мгновенно утоляя голод и залечивая раны.",
        "kind":  "ability",
        "biostatMet":  -3,
        "biostatCpx":  3,
        "effect":  "Способность ближнего боя (перезарядка 12 часов): наносит 45 бронебойного урона, восстанавливает 40% сытости и лечит 20 единиц собственных ран.",
        "implementation_ru":  "Ванильный AbilityDef (дальность 1.5 клетки) + свой C# (CompAbilityEffect), наносящий урон Bite, добавляющий need.food.CurLevel += 0.4f и лечащий раны.",
        "counterplay_ru":  "Работает только вплотную и только по живым существам из плоти (не действует на роботов и синтов)."
    },
    {
        "id":  "leviathan_shapeshift_mimic",
        "xenotype":  "leviathan",
        "label_en":  "predatory DNA mimicry",
        "label_ru":  "хищная мимикрия",
        "desc_en":  "By sampling a drop of a victim\u0027s blood or flesh, a Leviathan reshapes its cellular structure into a flawless physical and vocal duplicate of its prey.",
        "desc_ru":  "Поглотив каплю крови или плоти жертвы, левиафан перестраивает свои клетки и превращается в безупречную внешнюю и голосовую копию добычи.",
        "kind":  "ability",
        "biostatMet":  -3,
        "biostatCpx":  4,
        "effect":  "Способность (перезарядка 3 дня): копирует внешность и одежду выбранного гуманоида (живого в упор или трупа), давая +25% к скрытности/дипломатии на 48 часов.",
        "implementation_ru":  "Свой C# (CompAbilityEffect): временно подменяет визуальные параметры пешки (CrownType, HairDef, цвет кожи и копирует визуал одежды) на 2 игровых дня с Hediff маскировки.",
        "counterplay_ru":  "Маскировка мгновенно слетает при получении урона от буры или при применении способности «Пасть бездны»."
    },
    {
        "id":  "leviathan_borax_allergy",
        "xenotype":  "leviathan",
        "label_en":  "caustic borax sensitivity",
        "label_ru":  "уязвимость к буре",
        "desc_en":  "Common sodium borate acts as a violent corrosive agent against Leviathan physiology, melting their flesh like acid and stripping away their unnatural toughness.",
        "desc_ru":  "Обычный тетраборат натрия (бура) действует на физиологию левиафанов как сильнейшая кислота, разъедая плоть и лишая их сверхъестественной живучести.",
        "kind":  "weakness",
        "biostatMet":  4,
        "biostatCpx":  2,
        "effect":  "+300% к урону от распылителей и турелей с бурой; попадание буры на 30 секунд отключает регенерацию и режет броню на 50%.",
        "implementation_ru":  "Ванильный damageFactors для DamageDef «TMM_BoraxBurn» + кастомный Hediff «Ожог бурой», временно подавляющий регенерацию и множитель входящего урона.",
        "counterplay_ru":  "Для игрока — ставить турели с бурой на входах базы и вооружать передовой отряд распылителями перед встречей с левиафанами."
    }
]
```

## Наш текущий файл генов метаболизма (формат)
```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Metabolism genes that cancel the metabolic cost of our supernatural xenotypes (sums of vanilla biostatMet). Biotech only. -->
<Defs>
  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Metabolism_Angelic</defName>
    <label>angelic metabolism</label>
    <description>Carriers of this gene draw sustenance from a divine source. It fully pays for the metabolic cost of the other angelic genes.</description>
    <iconPath>UI/Icons/Genes/Gene_RobustDigestion</iconPath>
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatCpx>0</biostatCpx>
    <biostatMet>9</biostatMet>
  </GeneDef>
</Defs>
```
Остальные гены метаболизма: `TMM_Metabolism_Demonic`(+2), `TMM_Metabolism_Mutant`(+10), `TMM_Metabolism_Ghoul`(+8), `TMM_Metabolism_Leviathan`(+8).

## Что сдать
Файлы с путём над каждым блоком:
1. `Defs/GeneDefs/Genes_Supernatural.xml` — 18 GeneDef.
2. `Defs/AbilityDefs/Abilities_Supernatural.xml` — AbilityDef для генов-способностей.
3. `Defs/HediffDefs/Hediffs_Supernatural.xml` — Hediff способностей и слабостей.
4. `Defs/DamageDefs/Damages_Supernatural.xml` — 4 DamageDef.
5. `Defs/GeneDefs/Genes_Metabolism.xml` — исправленные значения метаболизма.
6. `Patches/Xenotypes_SupernaturalGenes.xml` — PatchOperationAdd, который добавляет новые гены в `genes` наших пяти XenotypeDef. Сами Xenotype-файлы не переписывай.
7. `Source/SupernaturalGenes.cs` — весь нужный C#: CompAbilityEffect, HediffComp, MentalState одичания гуля, Harmony-патчи если нужны. Harmony-патчи регистрируй в статическом конструкторе с `[StaticConstructorOnStartup]` через `new Harmony("colonyproject.integrated.supernaturalgenes")`.
8. `Languages/Russian/DefInjected/GeneDef/Genes_Supernatural.xml`, `AbilityDef/…`, `HediffDef/…`, `DamageDef/…` — RU из `*_ru`.
9. `Tests/InGameSupernaturalGenesProbe.cs` — проба. MapComponent, флаг `-tmm-supergenes-probe`, JSON `SupernaturalGenesProbe.json` в формате `{""passed"":bool,""checksCount"":n,""checks"":[{""name"",""passed"",""expected"",""actual""}],""errors"":[]}`. Проверки должны совершать **реальные действия**:
   - сгенерировать пешку каждой из 5 рас и проверить, что новые гены активны и **сумма biostatMet активных генов = 0**;
   - для каждой способности применить её реально (`Ability.Activate` по цели) и проверить эффект: раны закрылись, Hediff наложен, пешка переместилась, сытость выросла;
   - для каждой слабости нанести урон соответствующим DamageDef и сравнить с уроном по обычному человеку;
   - для пассивок проверить значения статов: скорость, грузоподъёмность и т.п.

В конце перечисли строки с `// CHECK API` и гены, которые пришлось упростить, и почему.