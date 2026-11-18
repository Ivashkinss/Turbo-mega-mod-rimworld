Ты пишешь контент для мода RimWorld 1.7 (с DLC Biotech) (кроссовер Fallout × «Сверхъестественное»). Нужны уникальные предыстории (BackstoryDef) для 10 именных героев. Ответ — только готовые XML-файлы, без пояснений.

## Герои
| defName героя | Имя | Фракция | Пол | Роль, что важно отразить |
|---|---|---|---|---|
| preston_garvey | Престон Гарви | Минитмены и охотники | М | генерал минитменов, выживший после резни в Квинси, верит в помощь поселениям |
| dean_winchester | Дин Винчестер | Минитмены и охотники | М | охотник на нечисть, вырос в дороге, Импала, отличный стрелок и боец |
| sam_winchester | Сэм Винчестер | Минитмены и охотники | М | охотник, бывший студент-юрист Стэнфорда, знаток оккультизма и ритуалов |
| crowley | Кроули | Ад | М | Король Ада, бывший демон перекрёстка, торговец сделками, хитрый и язвительный |
| azazel | Азазель (Желтоглазый) | Ад | М | древний демон с жёлтыми глазами, план с «особыми детьми» |
| lilith | Лилит | Ад | Ж | первый демон, созданный Люцифером; печати и освобождение Люцифера |
| abaddon | Абаддон | Ад | Ж | Рыцарь Ада, жестокий воин старой школы |
| cain | Каин | Ад | М | первый убийца, отец Рыцарей Ада, носитель Метки; ушёл на покой, разводит пчёл |
| meg | Мег Мастерс | Ад | Ж | демон-оперативник, циничная, позже неоднозначный союзник охотников |
| ruby | Руби | Ад | Ж | демон с особым ножом, убивающим демонов; манипулирует Сэмом |

## Формат
Для каждого героя две предыстории: `Childhood` (детство или происхождение) и `Adulthood`. Для демонов детство — человеческая жизнь до одержимости или превращения.

Файл 1 — `Defs/BackstoryDefs/Backstories_Heroes.xml` (английский язык — базовый):
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <BackstoryDef>
    <defName>TMM_BS_crowley_child</defName>
    <slot>Childhood</slot>
    <title>crossroads apprentice</title>
    <titleShort>apprentice</titleShort>
    <description>[PAWN_nameDef] ... (2–4 предложения, грамматика RimWorld: [PAWN_nameDef], [PAWN_pronoun], [PAWN_possessive], [PAWN_objective])</description>
    <skillGains>
      <li><key>Social</key><value>3</value></li>
    </skillGains>
    <workDisables>None</workDisables>
    <spawnCategories><li>TMM_Heroes</li></spawnCategories>
    <shuffleable>false</shuffleable>
  </BackstoryDef>
  <!-- ... и TMM_BS_crowley_adult со slot Adulthood ... -->
</Defs>
```

Файл 2 — `Languages/Russian/DefInjected/BackstoryDef/Backstories_Heroes.xml`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <TMM_BS_crowley_child.title>ученик перекрёстка</TMM_BS_crowley_child.title>
  <TMM_BS_crowley_child.titleShort>ученик</TMM_BS_crowley_child.titleShort>
  <TMM_BS_crowley_child.description>...</TMM_BS_crowley_child.description>
</LanguageData>
```

## Правила
- `defName`: `TMM_BS_<defName героя>_child` и `TMM_BS_<defName героя>_adult`.
- `skillGains`: только ванильные навыки — Shooting, Melee, Construction, Mining, Cooking, Plants, Animals, Crafting, Artistic, Medicine, Social, Intellectual. Сумма бонусов на одну предысторию от 4 до 8, не больше 5 на один навык.
- `workDisables`: значения `None`, `Artistic`, `Caring`, `Cleaning`, `Cooking`, `Crafting`, `Firefighting`, `Hauling`, `Intellectual`, `ManualDumb`, `ManualSkilled`, `Mining`, `PlantWork`, `Social`, `Violent`, `Animals`. Через запятую, если несколько. Боевым героям не отключать `Violent`.
- Тексты в духе первоисточников, без прямых цитат из сериала и игр. Русский перевод — живой, не дословный. Мужской/женский род по полу героя.
- В тексте используй токены `[PAWN_nameDef]`, `[PAWN_pronoun]` и т.п., а не имя напрямую.
- Ничего не придумывай про механику мода: только характер, прошлое и навыки.

Выдай два файла целиком, каждый в своём блоке кода, с путём над блоком.
