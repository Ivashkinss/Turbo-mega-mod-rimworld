# StoryContentFactions — 75 квестов фракций в XML (E4)

kind: integrate + run · assignee: gemini (Antigravity) · afterTask: StoryStageHotel

> **ИЗМЕНЕНИЕ (Claude, 20.11, экономия лимита Antigravity):** XML 75 квестов пишет Gemini Neon (промпты `Delegation/Hydrogen/36/37/38-Neon-FactionQuests-Batch*.md`); Claude раскладывает файлы в `Delegation/StoryContent/Factions/` по путям корня. **Не конвертируй сам**: скопируй готовые файлы, напиши линтер (ниже), прогони его и пробу. Если папка пуста или неполна — `blocked: waiting for Neon faction quests` для недостающих фракций и бери следующую задачу.

Источник: `Documentation/FactionQuests.json` (Hydrogen, 15 фракций × 5 квестов). Образец формата: `Defs/StoryDefs/Story_Act1.xml` и `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act1.xml`. Ядро и этапы (E1–E3) не меняй. Задача механическая: точно перенести то, что придумал Hydrogen, в наш формат.

## Правила конвертации
1. **Один файл на фракцию:** `Defs/StoryDefs/Story_<Faction>.xml` (EN) и `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_<Faction>.xml` (RU). EN-тексты бери из `*_en`, RU — из `*_ru`.
2. **defName:** `TMM_Story_<faction>_Q<n>`, например `TMM_Story_institute_Q1`. `faction` — наш FactionDef. Таблица соответствия id Hydrogen и FactionDef:
   - institute → `TMM_InstituteFaction`
   - brotherhood → `TMM_BrotherhoodFaction`
   - outcasts → `TMM_OutcastsFaction`
   - vault_tec → `TMM_VaultTecFaction`
   - minutemen_hunters → `TMM_MinutemenHuntersFaction`
   - ghouls → `TMM_GhoulsFaction`
   - friendly_mutants → `TMM_FriendlyMutantsFaction`
   - enclave → `TMM_EnclaveFaction`
   - gunners → `TMM_GunnersFaction`
   - railroad → `TMM_RailroadFaction`
   - wild_mutants → `TMM_WildMutantsFaction`
   - men_of_letters → `TMM_MenOfLettersFaction`
   - hell_demons → `TMM_HellFaction`
   - heaven_angels → `TMM_HeavenFaction`
   - monster_clans → `TMM_MonsterClansFaction`
3. **giver / fallbackGiver** — HeroDef по имени героя из `Defs/HeroDefs/*.xml`. Если героя нет, giver не ставь, и герой попадает в отчёт.
4. **Акт.** Квесты 1–3 — `act` 2, «точка фракции» (`isFactionPoint`) — `act` 2, квест перебежчика (`isDefectorQuest`) — `act` 3.
5. **Порядок внутри линии:** квест n требует `done:TMM_Story_<faction>_Q<n-1>`. Квест 1 требует `act>=2`.
6. **Этапы.** `where` и `mechanic` Hydrogen переводятся в наши этапы:
   - `special_map` / `world_site` → `StoryStage_Site`. objective по смыслу: бой и штурм → `Clear`, спасение → `Rescue`, оборона точки → `Hold`, босс → `KillHero`;
   - `colony` с обороной → `StoryStage_ColonyRaid`;
   - `hotel` → `StoryStage_HotelVisit`;
   - `outpost` → `StoryStage_Site` рядом с аванпостом или `Hold`;
   - выбор игрока (`choices`) → последний этап `StoryStage_Choice`, варианты берутся из `choices`.
7. **Флаги Hydrogen** переводятся к нашим именам (`Documentation/STORY-ENGINE-DESIGN.md`, раздел «Соглашение о флагах»):
   - `TMM_FactionArcStage_x=4` → `StoryEffect_SetFlag arc.<faction>=4`;
   - `TMM_FactionChoice_x=v` → `choice.<faction>=v`;
   - `TMM_DefectorRecruited_h=true` → `StoryEffect_HeroJoins` с этим героем;
   - прочие `TMM_X=y` → `story.<x>=y`.
   Отношения (`goodwill`) переводятся в `StoryEffect_Goodwill`.
8. **Счётчик линий.** В каждом квесте «точки фракции» (`isFactionPoint`) в `onComplete` добавь `StoryEffect_AddInt` `arcPoints` +1: главная нить акта 2 (Hydrogen) открывается по `arcPoints>=5` и `arcPoints>=10`.
9. **Награды** (`reward_ru`). Серебро и вещи → `StoryEffect_GiveThing` только с ванильными ThingDef. Чертежи, легендарки и именные вещи пока ставь флагом `unlock.<id>=true`: предметы появятся позже.

## Линтер
Напиши `Tools/verify_story_content.py` (Python 3, lxml). Он проверяет все `Defs/StoryDefs/*.xml`:
- каждый `requires` ссылается на существующий квест или флаг, который где-то ставится;
- `giver` существует;
- у каждого `Choice` от 1 до 3 вариантов;
- для каждого EN-текста есть RU-ключ;
- нет `{GIVER}` в квесте без giver; флаг из `requires`/`anyOf`, который ставится только внутри исходов этапа (`outcomes`), должен ставиться во ВСЕХ возможных исходах этого этапа (Site: success/timeout/failed, ColonyRaid: success/timeout, HotelVisit: success/failed/noHotel) или в `onComplete` — иначе сюжет может застрять; линтер проверяет все `Defs/StoryDefs/*.xml`, включая акты 1–4.

Выход ненулевой при ошибке.

## Проверка в игре (обязательна)
- Игра грузится со всеми файлами: 0 ERROR, ни одной ошибки config/XML в stdout.log.
- Расширь `Tests/InGameStoryEngineProbe.cs` новым режимом `-tmm-story-content-probe` (компонент `StoryContent`). Режим ставит `act=2` и для двух фракций, Институт и Минитмены, проходит линию реальными действиями. Где этап `Site`, `Raid` или `Hotel`, разрешено завершить его через `ctx.run.vars`-хук `forceOutcome` — только в пробе, этот хук ты добавляешь в свои классы E2/E3. Нужно, чтобы флаги `arc.*` и `choice.*` встали и квест перебежчика открылся.
- Линтер проходит с кодом 0.

Не коммитить. Сдача: `Tools/Build/GeminiStoryContentFactions/REPORT.md`. В отчёте перечисли квесты, которые не удалось перенести один к одному, и почему.
