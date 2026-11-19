**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

Продолжение задачи по коду сюжетного движка (C#, RimWorld 1.7, компилятор C# 5). Твои этапы `StoryStage_Site` и `StoryStage_ColonyRaid` агент сейчас встраивает. Теперь нужны ещё два этапа: **«гость в гостинице»** (`StoryStage_HotelVisit`) и **«доставка»** (`StoryStage_Deliver`). Устроены они так же, как твои: исходы через `List<StoryChoice> outcomes`, данные только в `ctx.run.vars`.

## Жёсткие правила (как в прошлый раз)
- **Только C# 5:** без `?.`, `=>` у членов класса, `nameof`, `is X x`, `$""""`, `out var`.
- API — RimWorld 1.7. Где не уверен, ставь `// CHECK API`.
- Ядро не меняй. Пространство имён `TurboMegaMod`. Hospitality — отдельный мод, встроенный в наш, поэтому обращайся к нему **только через рефлексию**, как в нашем `HotelVisitGuarantee.cs` ниже. Если тип не найден, исход `noHotel`, без исключений.

## Ядро
```csharp
    /// Per-quest runtime context passed to stages and effects.
    public sealed class StoryContext
    {
        public SparkStory state;
        public StoryQuestRun run;
        public StoryQuestDef def;
        public Faction Faction { get { return StoryUtil.FactionOf(def.faction); } }
        public Map Home { get { return Find.AnyPlayerHomeMap; } }
    }

    // ---------------------------------------------------------------- stages

    public abstract class StoryStage
    {
        public string key;                  // optional stable id; saves follow it if stages are reordered
        [MustTranslate] public string label;
        [MustTranslate] public string text;

        public string Key(int index) { return key.NullOrEmpty() ? "#" + index : key; }

        /// Short mechanic id used by the director to avoid two identical mechanics in a row.
        public abstract string Mechanic { get; }

        /// Called once when the stage becomes current.
        public virtual void Start(StoryContext ctx) { }

        /// Called every director tick while current; return an outcome id to finish the stage, or null.
        public virtual string Tick(StoryContext ctx) { return null; }

        /// Next stage index for the outcome; -2 means "default" (next stage).
        public virtual int NextFor(string outcome) { return -2; }

        /// Effects to apply for the outcome.
        public virtual IEnumerable<StoryEffect> EffectsFor(string outcome) { yield break; }

        /// Text shown in the Chronicle for the current objective.
        public virtual string Objective(StoryContext ctx) { return StoryUtil.Format(label.NullOrEmpty() ? text : label, ctx); }
    }

    public sealed class StoryChoice
    {
        public string outcome;
        [MustTranslate] public string label;
        [MustTranslate] public string resultText;  // giver's reply, shown as a follow-up letter
        public int nextStage = -2;
        public List<string> requires = new List<string>();
        public List<StoryEffect> effects = new List<StoryEffect>();
    }


```
```csharp
    public sealed class StoryQuestRun : IExposable
    {
        public string quest;
        public StoryQuestStatus status;
        public int stage = -1;
        public string stageKey;          // stable key of the current stage (StoryStage.key or its index); survives reordered XML
        public int gen;                  // generation: bumped on every offer and stage entry; letters carry it to reject stale clicks
        public string outcome;
        public string chosenOutcome;     // set by the choice letter, consumed by StoryStage_Choice.Tick
        public bool pendingChoice;
        public bool needsLetter;         // a lost offer/choice letter must be re-sent once by the director
        public int stageUntilTick = -1;
        public int offeredTick = -1;
        public int cooldownUntilTick = -1;
        public int finishedTick = -1;
        public List<string> history = new List<string>(); // "stage:outcome" for the Chronicle log
        public Dictionary<string, string> vars = new Dictionary<string, string>(); // per-stage worker data (E2/E3)


```
Ещё доступно: `SparkStory.Instance.TestForceOutcome(run, run.gen, outcome)` (только с `-tmm-story-test`), `HeroRegistry.Instance.GetOrCreate(HeroDef)`, `StoryUtil.FactionOf`, `StoryUtil.Format`.

## Как мы уже работаем с Hospitality (рабочий код)
```csharp
using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Hotel visit guarantee (user report 19.11.2026: after a couple of guest groups the next visit was 450 h away).
    /// Hospitality plans a faction's revisit 3–30 days after its visit and only for factions that already came,
    /// which is far too rare for a hotel. Every 6 hours, on each player home with guests welcome: if no visit is
    /// queued within 2 days, plan one from a random friendly humanlike faction 12–48 hours ahead.
    public sealed class HotelVisitGuarantee : GameComponent
    {
        private const int CheckInterval = 2500 * 6;
        public const float WindowDays = 2f;

        private static readonly Type MapCompType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
        private static readonly Type UtilType = AccessTools.TypeByName("Hospitality.Utilities.GenericUtility");
        private static readonly System.Reflection.MethodInfo Queued = MapCompType == null ? null : AccessTools.Method(MapCompType, "GetQueuedVisits");
        private static readonly System.Reflection.FieldInfo Welcome = MapCompType == null ? null : AccessTools.Field(MapCompType, "guestsAreWelcome");
        private static readonly System.Reflection.MethodInfo Plan = UtilType == null ? null : AccessTools.Method(UtilType, "PlanNewVisit");

        public HotelVisitGuarantee(Game game) { }

        public static bool Available { get { return Queued != null && Welcome != null && Plan != null; } }

        public override void GameComponentTick()
        {
            if (!Available || Find.TickManager.TicksGame % CheckInterval != 0) return;
            foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome)) CheckMap(map);
        }

        public static MapComponent HospitalityOf(Map map)
        {
            return MapCompType == null || map == null ? null : map.components.FirstOrDefault(c => MapCompType.IsInstanceOfType(c));
        }

        public static int QueuedWithin(Map map, float days)
        {
            MapComponent comp = HospitalityOf(map);
            if (comp == null || Queued == null) return -1;
            var list = Queued.Invoke(comp, new object[] { days }) as IEnumerable;
            return list == null ? 0 : list.Cast<object>().Count();
        }

        /// Returns the faction a visit was planned for, or null when nothing was needed or possible.
        public static Faction CheckMap(Map map)
        {
            MapComponent comp = HospitalityOf(map);
            if (comp == null || !Available || map.mapPawns.FreeColonistsCount == 0) return null;
            if (!(bool)Welcome.GetValue(comp)) return null;
            if (QueuedWithin(map, WindowDays) > 0) return null;
            Faction f;
            if (!Find.FactionManager.AllFactionsVisible.Where(x => !x.IsPlayer && !x.defeated && x.HasGoodwill && x.def.humanlikeFaction && !x.HostileTo(Faction.OfPlayer)).TryRandomElement(out f)) return null;
            Plan.Invoke(null, new object[] { map, Rand.Range(0.5f, 2f), f });
            return f;
        }
    }
}

```

## Как мы списываем ресурсы со складов колонии (рабочий код, класс OutpostPodLogistics)
```csharp
        public static int CountResource(Map map, ThingDef def)
        {
            if (map == null || def == null || map.listerThings == null) return 0;
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t != null && !t.Destroyed && !t.IsForbidden(Faction.OfPlayer))
                {
                    count += t.stackCount;
                }
            }
            return count;
        }

        public static int DeductResource(Map map, ThingDef def, int count)
        {
            if (map == null || def == null || count <= 0 || map.listerThings == null) return 0;
            int remaining = count;
            List<Thing> list = map.listerThings.ThingsOfDef(def).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t != null && !t.Destroyed && !t.IsForbidden(Faction.OfPlayer))
                {
                    int take = Mathf.Min(remaining, t.stackCount);
                    Thing split = t.SplitOff(take);
                    if (split != null && !split.Destroyed)
                    {
                        split.Destroy(DestroyMode.Vanish);
                    }
                    remaining -= take;
                    if (remaining <= 0) break;
                }
            }
            return count - remaining;
        }


```

## Что сделать
## 1. `StoryStage_HotelVisit` (Mechanic = "hotel")
Гость или делегация приходит в гостиницу колонии. Это шпион, перебежчик, переговорщик или беглые синты. Используй Hospitality так же, как `Source/HotelVisitGuarantee.cs`: через рефлексию, по готовым методам.
- **Поля:**
  - `faction` (пусто — фракция квеста);
  - `hero` (HeroDef, необязательный): этот герой приходит в составе группы, его берёт `HeroRegistry.GetOrCreate`;
  - `groupSize` (по умолчанию 1–3);
  - `stayDays` (по умолчанию 2);
  - `objective`:
    - `Host` — гость прожил `stayDays` и ушёл довольным, не во вражде;
    - `Investigate` — игрок за это время поговорил с гостем: любая социальная интеракция колониста с ним, есть ванильный журнал интеракций; либо гость сидел в одной комнате с колонистом-гуманитарием;
    - `Hide` — гость прожил `stayDays`, и за это время на карту не пришёл рейд фракции `hunterFaction`, либо пришёл и был отбит.
- Нет гостиницы, или приём гостей выключен — исход `noHotel`, письмо подсказывает, как открыть гостиницу. Сюжет не застревает.
- **Исходы:** `success`, `failed` (гость погиб, ушёл во вражде или его схватили), `noHotel`. Эффекты исходов — список `outcomes`, как в E2.

## 2. `StoryStage_Deliver` (Mechanic = "deliver")
- **Поля:** `thing` (ThingDef), `count`, `to` (`giverSettlement` — ближайшее поселение фракции квеста, или `colony` — сдать на складе колонии), `timeoutDays` (по умолчанию 10).
- `colony`: вещи из `count` лежат на складах домашней карты — по нажатию «Отдать» в письме-напоминании они списываются, так же как в `OutpostPodLogistics.DeductResource`.
- `giverSettlement`: игрок приводит караван с вещами на клетку поселения. Вещи списываются из каравана.
- **Исходы:** `success`, `timeout`.


## Проверка в игре
Её пишешь ты: `Tests/InGameStoryHotelProbe.cs`, флаги `-tmm-story-hotel-probe` / `-tmm-story-hotel-reload`, сейв `TMM-StoryHotel`, JSON `StoryHotelProbe.json` / `StoryHotelReloadProbe.json` в том же формате, что у твоей пробы E2. Готовую гостиницу в тесте строй так же, как это делает наша проба HotelVisit: ресепшен и гостевые кровати. Если не знаешь точных defName, пометь `// CHECK API`, агент подставит.
## Проверка в игре (обязательна)
Проба `Tests/InGameStoryHotelProbe.cs`, компонент `StoryHotel`, и reload. Проверки должны совершать реальные действия:
- **Host:** гостиница готова, как в пробе `HotelVisit`. Квест с флагом-примером, принять. Группа с героем пришла и заселилась, прошло `stayDays`, ушла → `success`. Герой из реестра, а не новый: сверить ThingID.
- **noHotel:** без гостиницы → `noHotel` и письмо-подсказка.
- **Deliver colony:** 50 стали на складе → после отдачи склад −50, `success`.
- **Reload:** сохранение во время проживания, после загрузки этап завершается ровно один раз.


## Сдача
Файлы с путём над каждым блоком:
- `Source/StoryStage_HotelVisit.cs`;
- `Source/StoryStage_Deliver.cs`;
- `Defs/StoryDefs/Story_Samples_E3.xml` (тестовые квесты под флагом `story.e3samples=true`);
- `Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Samples_E3.xml`;
- `Tests/InGameStoryHotelProbe.cs`.

В конце перечисли строки с `// CHECK API`.