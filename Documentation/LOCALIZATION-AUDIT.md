# Аудит русификации исходных компонентов — RimWorld 1.6

Отчёт описывает установленные оригиналы до изменений в ColonyProject.

| Мод | Текстов XML для сверки | Нет своего RU | Совпадает с EN¹ | Параметры² | Повторы RU |
| --- | ---: | ---: | ---: | ---: | ---: |
| Harmony | 0 | 0 | 0 | 0 | 0 |
| [sbz] Fridge | 1 | 1 | 0 | 0 | 0 |
| Adaptive Storage Framework | 40 | 40 | 0 | 0 | 0 |
| Almost There! Fork | 16 | 16 | 0 | 0 | 0 |
| Camera+ | 189 | 0 | 0 | 0 | 0 |
| Character Editor | 38 | 38 | 0 | 0 | 0 |
| Common Sense | 86 | 4 | 0 | 0 | 0 |
| Dev In Your Language | 615 | 408 | 0 | 3 | 0 |
| Dynamic Trade Interface | 97 | 97 | 0 | 0 | 0 |
| FSharp.Core | 0 | 0 | 0 | 0 | 0 |
| HugsLib | 82 | 2 | 1 | 0 | 0 |
| Interaction Bubbles | 31 | 31 | 0 | 0 | 0 |
| Map Preview | 76 | 71 | 1 | 0 | 0 |
| Melee Animation | 350 | 350 | 0 | 0 | 0 |
| More Planning (Continued) | 36 | 0 | 0 | 0 | 0 |
| [sbz] Neat Storage | 55 | 1 | 0 | 0 | 44 |
| Nice Bill Tab | 63 | 0 | 0 | 0 | 0 |
| Pick Up And Haul | 13 | 8 | 0 | 0 | 0 |
| Realistic Rooms Rewritten | 11 | 11 | 0 | 0 | 0 |
| RunAndGun | 32 | 4 | 0 | 0 | 0 |
| Show Me Your Hands | 73 | 8 | 0 | 0 | 0 |
| Simple sidearms | 234 | 24 | 1 | 0 | 0 |
| Smart Speed | 16 | 16 | 0 | 0 | 0 |
| Smarter Construction | 0 | 0 | 0 | 0 | 0 |
| Snap Out! | 53 | 4 | 1 | 0 | 0 |
| Useful Marks | 172 | 0 | 1 | 0 | 0 |
| Vanilla Expanded Framework | 675 | 675 | 0 | 0 | 0 |
| Walls are solid | 12 | 12 | 0 | 0 | 0 |
| XML Extensions | 27 | 27 | 0 | 0 | 0 |

¹ Совпадение с английским требует ручной проверки, например имя автора переводить не нужно.
² Различия подстановок {0}, {name} требуют проверки формата.

## Ограничения

- Состав проекта: 29 выбранных компонентов и Core; DLC и внешние интеграции не предполагаются активными.
- Сверены Keyed, DefInjected и распространённые переводимые поля Defs; это статический аудит, не игровой translation report.
- Идентичные английские строки — кандидаты на проверку, не всегда ошибки (названия, сокращения).
- Часть абстрактных Defs и нестандартных текстовых полей, XML-патчи, Strings, DLL и динамические ключи требуют дальнейшей проверки.
- Прямой русский текст в Defs учитывается, даже если папки Russian нет.
- Повторяющиеся русские ключи требуют устранения; фактическое разрешение конфликтов игрой не предполагается.
- Показано отсутствие собственного RU у компонента; общий перевод из Core или другого мода может покрывать отдельные совпадающие ключи.
- Строки извлечены из #US DLL без исполнения кода; английские кандидаты могут быть логами и не доказывают отсутствие программного перевода.

## Harmony

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Нет обнаруженных текстов для перевода в выбранных XML; это не подтверждение полной русификации.

## [sbz] Fridge

- Пример пропуска: `BuildingsNeatStorage.label` — Storage

## Adaptive Storage Framework

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Текстов в потенциально условных XML-патчах: 3; вынесены отдельно и не прибавлены к пропускам.
- Пример пропуска: `Automatic.description` — Automatically choose between item names getting displayed with their count when hidden and count only when having visible contents, either directly or through the buildin
- Пример пропуска: `Automatic.label` — Automatic
- Пример пропуска: `Names.description` — Display names of stored items, one entry per line

## Almost There! Fork

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `AT_AlmostThereHours_Description` — Set how many hours away a target has to be for "Almost there" to trigger.
- Пример пропуска: `AT_AlmostThereHours_Title` — Almost there hours
- Пример пропуска: `AT_Command_AlmostThere_Description` — Cmon guys, we're almost there! With this enabled, your caravan won't rest when {0} hours away from its target. NOTE: the number of hours that are considered "almost there

## Camera+

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.

## Character Editor

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- В DLL найдено строк с кириллицей: 522; встроенный перевод нужно проверить отдельно в интерфейсе.
- Пример пропуска: `SZSpecial.label` — special
- Пример пропуска: `SZBodySize_05.label` — Bodysize 05
- Пример пропуска: `SZBodySize_06.label` — Bodysize 06

## Common Sense

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `SocialRelaxCommonSense.reportString` — relaxing socially.
- Пример пропуска: `CooledStoreroom.label` — fridge room
- Пример пропуска: `manual_unload_label` — Enable a button in colonist's inventory to mark items for hauling to stockpile

## Dev In Your Language

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Посторонние маркеры [cite: …] в 204 русских строках.
- Русские строки с английскими хвостами: 207 кандидатов на ручную проверку.
- Пример пропуска: `DebugAction_AbandonThing` — Abandon Thing
- Пример пропуска: `DebugAction_ActivateHediffGiver` — Activate HediffGiver
- Пример пропуска: `DebugAction_ActivateHediffGiverWorldPawn` — Activate HediffGiver of a world Pawn

## Dynamic Trade Interface

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `colCategoryDef.description` — Shows the main category of the thing.
- Пример пропуска: `colCategoryDef.label` — category
- Пример пропуска: `colDurabilityDef.description` — Shows the remaining hitpoints (if any) as a percentage.

## FSharp.Core

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Нет обнаруженных текстов для перевода в выбранных XML; это не подтверждение полной русификации.

## HugsLib

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `HugsLib_logs_github_token` — GitHub access token
- Пример пропуска: `HugsLib_logs_github_token_tip` — Publishes logs to your own GitHub account when set. Paste a token from GitHub > Settings > Developer Settings > Personal access tokens. Leave blank to publish to the publ

## Interaction Bubbles

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `Bubbles.AltitudeBase` — Base altitude
- Пример пропуска: `Bubbles.AltitudeMax` — Maximum visible altitude
- Пример пропуска: `Bubbles.AutoHideSpeed` — Auto hide at speed

## Map Preview

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `HugsLogPublisher.abortBtn` — Abort
- Пример пропуска: `HugsLogPublisher.browseBtn` — Open in browser
- Пример пропуска: `HugsLogPublisher.copiedToClipboard` — Copied to clipboard.

## Melee Animation

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `AM_DuelBow_Colossal.jobString` — Bowing before a duel.
- Пример пропуска: `AM_DuelBow_Colossal.label` — Duel Bow
- Пример пропуска: `AM_DuelBow_Medium.jobString` — Bowing before a duel.

## More Planning (Continued)

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.

## [sbz] Neat Storage

- Пример пропуска: `BuildingsNeatStorage.label` — Storage

## Nice Bill Tab

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.

## Pick Up And Haul

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `PUAH.allowAnimals` — Allow tamed pack animals using their inventory for hauling
- Пример пропуска: `PUAH.allowAnimalsTooltip` — Default On. Works best with mods that give backpacks to more animals, like VFME Caravan Packs
- Пример пропуска: `PUAH.allowCorpses` — Allow putting corpses into inventory

## Realistic Rooms Rewritten

- Нет папки 1.6; для статического аудита взята 1.4. Требуется проверка выбора версии игрой.
- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `RealisticRoomsSettingsCategoryLabel` — Realistic Rooms
- Пример пропуска: `SettingsASLabel` — Minimum size for Average Sized
- Пример пропуска: `SettingsESLabel` — Minimum size for Extremely Spacious

## RunAndGun

- В LoadFolders.xml нет v1.6; для аудита использован fallback v1.5. Требуется проверка загрузки в игре.
- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `RG_Dialog_CE_ConfirmButton` — Allow
- Пример пропуска: `RG_Dialog_CE_Description` — It seems you're using the latest version Run and Gun and Combat Extended together for the first time. Combat Extended works great with Run and Gun, but not for each weapo
- Пример пропуска: `RG_Dialog_CE_DisallowButton` — Disallow

## Show Me Your Hands

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `ClutterHandsSettings_Core.label` — Weapon hand settings
- Пример пропуска: `ClutterHandsSettings_FireExtinguisherContinued.label` — Weapon hand settings
- Пример пропуска: `ClutterHandsSettings_FireWardenContinued.label` — Weapon hand settings

## Simple sidearms

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `Concept_SidearmsDropping.label` — SS: Dropping sidearms
- Пример пропуска: `Concept_SimpleSidearmsAdvancedDrafted.label` — SS: Advanced controls (drafted)
- Пример пропуска: `Concept_SimpleSidearmsAdvancedMelee.label` — SS: Advanced controls (melee)

## Smart Speed

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `EventSpeedLabel` — Change Event Speed
- Пример пропуска: `SS_EventSpeed` — Event Speed
- Пример пропуска: `SS_Fast` — Fast (twice normal speed)

## Smarter Construction

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Нет обнаруженных текстов для перевода в выбранных XML; это не подтверждение полной русификации.

## Snap Out!

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `AlwaysSucceedSetting` — No fail (debug)
- Пример пропуска: `CapLabel` — Negotiation cap
- Пример пропуска: `DefaultButton` — Revert advanced settings to default

## Useful Marks

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.

## Vanilla Expanded Framework

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `VEF_ControlledBlinking.description` — Override the creature's usual erratic blinking, allowing a directed way to blink to a chosen location within range.
- Пример пропуска: `VEF_ControlledBlinking.label` — controlled blinking
- Пример пропуска: `VEF_ControlledDetonation.description` — Willingly trigger a powerful self-destructive explosion, sacrificing this animal in a final act that unleashes all stored volatile compounds in a devastating blast.

## Walls are solid

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `BluntCrushing.label` — smash (crushing)
- Пример пропуска: `Victor.WallsAreSolid.Settings.label` — Walls Are Solid
- Пример пропуска: `Victor.WallsAreSolid.Settings.settings.10.label` — Explosion vs Solid {BombImpassable} [Default: 1]

## XML Extensions

- Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.
- Пример пропуска: `XmlExtensions_MainButton_ModSettings.description` — Edit Mod Settings.
- Пример пропуска: `XmlExtensions_MainButton_ModSettings.label` — Mod Settings
- Пример пропуска: `XmlExtensions_AddMainButton` — Add a main button for opening the XML Extensions mod settings menu?

## Ошибки XML

- `Dev In Your Language\1.6\Languages\Russian\Keyed\Actions.xml`: Opening and ending tag mismatch: DebugAction_PlayAnimation line 294 and DebugAction_PlaySound, line 294, column 99 (file:/C:/Users/User/Downloads/RimWorld%20(2018)/RimWorld/Mods/Dev%20In%20Your%20Language/1.6/Languages/Russian/Keyed/Actions.xml, line 294)

Все найденные ключи, исходные строки, файлы и номера строк находятся в LocalizationAudit.json.
