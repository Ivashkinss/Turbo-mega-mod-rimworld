Продолжаем роли колонистов (твоя таблица ролей и рангов уже в моде). Теперь нужны тексты для **ритуала повышения в звании** (Ideology) и для повышения без DLC. Ответ — один блок JSON, без пояснений.

## Как работает
- Когда колонист выполнил требования следующего ранга (навык и счётчик работы), игрок проводит ритуал «Повышение»: колонисты собираются, старший по званию из той же группы вручает новый ранг.
- Качество ритуала (как у ванильных ритуалов) даёт исходы: провал, удовлетворительно, хорошо, превосходно. Хорошие исходы дают участникам настроение, повышенному — дополнительную гордость.
- Без DLC Ideology — кнопка «Повысить» и короткое письмо.
- Два варианта текста: гражданский (вручение свидетельства мастерства) и военный (вручение погон, построение отряда).

## Формат
```json
{
  "ritual": {
    "civil":    { "label_en": "", "label_ru": "", "desc_en": "", "desc_ru": "" },
    "military": { "label_en": "", "label_ru": "", "desc_en": "", "desc_ru": "" }
  },
  "outcomes": {
    "civil":    [ { "quality": "terrible", "label_en": "", "label_ru": "", "desc_en": "", "desc_ru": "", "moodAttendees": -2, "moodPromoted": 0 }, ...4 штуки: terrible, decent, good, excellent ],
    "military": [ ...так же ]
  },
  "thoughts": {
    "promotedCivil":     { "label_en": "", "label_ru": "", "desc_en": "", "desc_ru": "" },
    "promotedMilitary":  { "label_en": "", "label_ru": "", "desc_en": "", "desc_ru": "" },
    "ceremonyAttended":  { "label_en": "", "label_ru": "", "desc_en": "", "desc_ru": "" }
  },
  "letters": {
    "promotedNoDlc_en": "", "promotedNoDlc_ru": "",
    "requirementsMet_en": "", "requirementsMet_ru": "",
    "notReady_en": "", "notReady_ru": ""
  },
  "ui": {
    "promoteButton_en": "", "promoteButton_ru": "",
    "nextRank_en": "", "nextRank_ru": "",
    "requirement_en": "", "requirement_ru": ""
  }
}
```

## Правила
- В текстах писем и подсказок используй плейсхолдеры: `{PAWN}` — имя колониста, `{RANK}` — новый ранг, `{ROLE}` — роль, `{SKILL}` — навык, `{NEED}` — нужное значение, `{HAVE}` — текущее значение.
- `moodAttendees` и `moodPromoted` — от −3 до +8, «превосходно» сильнее всего.
- Стиль RimWorld, коротко; русский — естественный.
