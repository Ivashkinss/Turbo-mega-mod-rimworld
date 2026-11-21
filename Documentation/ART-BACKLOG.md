# Art backlog: unique models

**Rule (user, 21.11.2026).** Everything new that the mod adds gets its own unique model or texture. That covers:
- items and weapons;
- apparel, faction clothing and hero gear;
- weapon and armor modules, ammo;
- buildings, turrets, vehicles;
- hero looks.

Borrowed vanilla textures are only placeholders. They are marked `<!-- TODO art -->` in the Defs. A content feature counts as finished only when its art is done.

## What needs art now (`TODO art` in Defs, 21.11.2026)
- `Defs/ThingDefs/Legendary_Weapons.xml`: 22 placeholders (15 legendaries, Colt silver bullets).
- `Defs/ThingDefs/Borax.xml`: 6 (borax, sprayer, turret).
- `Defs/ThingDefs/Equipment_WeaponMods.xml`: 6 (holy water tank, rock salt pack, silver engraving, Railroad weave, silver bullet, mod items).
- `Defs/ThingDefs/Equipment_Backpacks.xml`: 3 backpacks.
- `Defs/ThingDefs/Legendary_Apparel.xml`: 3 (Protector power armor and others).
- Faction apparel and hero signature gear (being integrated): all items.
- Vehicles and vertibirds (planned): all models.
- Named heroes: unique look or clothing (earlier user request).

## How to produce it
- Earlier pilots: `XenotypeArtPilot` and `DemonArtSet` (see `Delegation/`). Reuse their pipeline and format: RimWorld textures, PNG with transparency, `_north/_east/_south` for apparel and buildings, `Graphic_StackCount` folders for stackable items.
- Each art batch is its own queue task. It ends with an in-game check: textures load with 0 `Could not load Texture2D` errors, and a portrait or screenshot is attached to the report.
- When a texture lands, replace the placeholder `texPath` and delete the `TODO art` comment.

To find what is still missing: `Select-String Defs -Recurse -Pattern 'TODO art'`.

## Схема (принята в 0.14)
- Neon рисует SVG: `viewBox 0 0 512 512`, контур `#121519` толщиной 10–14. Ответы лежат в `Delegation/StoryContent/Art/neon-raw-*.md`, SVG — в `Delegation/StoryContent/Art/Art/...`.
- `Tools/svg_to_png.ps1` нарезает PNG 256 px через Edge. `Tools/install_art.ps1` кладёт их в `Textures/Things/Item/Equipment/TMM/<defName>.png` (флаг `-TexDir` задаёт другую папку: постройки — `Things/Building/...`, снаряды — `Things/Projectile/...`), меняет `texPath` и снимает пометку «TODO art».
- `Tools/stage_neon_art.ps1` — альтернативный разборщик: стопки `_a/_b/_c` и папки `Things/TMM/...`. Для стопок `Graphic_StackCount` используется он.
- Готово в 0.14: 19 предметов (15 легендарок, распылитель буры, бак святой воды, 4 именных оружия).
- Дальше:
  - промпт `49b`, пункты 5–29;
  - одежда: 32 вещи фракций, 28 именных, 2 легендарные. Промпта ещё нет. Нужны значки и надетые виды north/south/east. Neon рисует Male, остальные телосложения получаем масштабированием.
