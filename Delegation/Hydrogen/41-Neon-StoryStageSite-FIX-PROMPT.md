**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

Твой код этапов E2 (`StoryStage_Site`, `StoryStage_ColonyRaid`) встроен в мод, и наш ревьюер Codex провёл его приёмку: **«НЕ ПРИНИМАТЬ»**. Ниже его отчёт целиком, текущий код в моде (агент уже поправил сигнатуры 1.7) и нужные куски ядра. **Исправь все находки P1 и P2.** Правила те же: C# 5, RimWorld 1.7, `// CHECK API` там, где ты не уверен.

## Принципы исправления
- **Карту места квест никогда не удаляет сам.** Таймаут завершает этап исходом `timeout` и снимает «квестовость» места. Если карта сгенерирована, её уберёт ванильная механика, когда игрок уйдёт (`Site.ShouldRemoveMapNow`, `TimeoutComp` / `WorldObjectComp` с проверкой пешек). `Destroy()` — только для места **без** карты.
- **Герой — только безопасный перенос**, по образцу `StoryEffect_HeroJoins.TryJoin` (код ниже): брать только свободную мировую пешку. Не трогать героя на карте, в караване, в контейнере, в плену или уже в фракции игрока: для KillHero это исход `unavailable`, для Rescue — тоже `unavailable`. Каждый перенос проверять, при отказе откатывать. Успех KillHero — подтверждённая смерть **этой** пешки (ThingID в `vars`). Rescue — пешка была пленником на карте места, а затем оказалась в караване игрока, который ушёл с карты, или на домашней карте.
- **Предмет для Retrieve:** уникальная метка. Сохранять ThingID созданной вещи, ставить флаг «создан» только после подтверждённого `GenSpawn`/`TryPlaceThing`, запретить слияние стеков (stackCount 1 или отдельный Def-маркер). Если ThingID потерян, исход `failed`, а не любая вещь того же Def.
- **Рейд:**
  - сохраняй ID карты и полный список ID пешек инцидента, включая капсулы, до спавна;
  - проверяй только эту карту и этих пешек;
  - пустой список или пропавшая карта — явный исход `timeout`, не `success`;
  - **враждебность:** до изменения сохрани в `vars` исходные goodwill и тип отношения. Откат делай через `SetRelationDirect` или до значения, при котором ваниль снимает Hostile, и учти пороги `FactionRelation.CheckKindThresholds`. Проверяй результат;
  - запиши «ожидающий откат» в отдельный WorldComponent `StoryRelationRestorer`. Он выполнит откат, даже если квест отключён, этап удалён или вылетело исключение, и учтёт параллельные рейды одной фракции: откат делает только последний.
- **Hold:** засчитывай только подтверждённые интервалы, когда колонист реально на карте места в момент опроса. Первый опрос после входа не прибавляет прошедшее время. После дедлайна сначала проверяется таймаут.
- **Нагрузка:** один обход на опрос, разрешение всех ID пачкой. Для KillHero используй запись `HeroRegistry` (state Dead и ThingID), а не обход всех мёртвых в WorldPawns.
- **Ядро** — выдай исправленные целиком `Source/SparkStory.cs` и `Source/StoryLetter.cs`, сохранив весь их API:
  - `forceOutcome` в `TickRun` читать **только** при аргументе `-tmm-story-test`, иначе удалять ключ из vars и игнорировать;
  - в письме: перед `Resolve()` повторно проверить `requires`/`anyOf` варианта. Если `Choose` отказал, вызвать `Notify_LetterLost`, чтобы письмо было переотправлено;
  - `StoryQuestDef.ConfigErrors` (в `StoryDefs.cs`) должен проверять синтаксис `requires` и `anyOf` **у вариантов выбора и у outcomes** тоже. Выдай только этот метод отдельным блоком.

## Отчёт Codex
# StoryStageSiteReview — НЕ ПРИНИМАТЬ

## Куда смотреть Claude
- **P1 · Потеря карты с пешками** — `Source/StoryStage_Site.cs:170`, `:547`. Если последний стоящий колонист упал, таймаут вызывает `Site.Destroy()` даже с лежачим колонистом, пешками в контейнерах или грузом на карте. По Cecil удаление world object вызывает `MapParent.PostRemove → DeinitAndRemoveMap`, обходя `Site.ShouldRemoveMapNow`. Завершать квест по таймауту отдельно. Сгенерированную карту оставлять ванильному снятию с проверками пешек, транспорта и эвакуации.
- **P1 · Перенос живого героя без проверки владельца** — `Source/StoryStage_Site.cs:219–259`. KillHero перемещает уже завербованного героя с базы и меняет ему фракцию. Перенос героя из капсулы или другого holder не проверяется. Rescue пропускает только караван, а затем считает уже находящегося в караване героя спасённым (`:330`). Выбирать доступную пешку по записи реестра, проверять исходный holder, плен и фракцию до мутаций, переносить с проверкой результата и откатом. Для Rescue отмечать реальное пленение и последующее извлечение. Для KillHero принимать подтверждённую смерть конкретного ID, иначе Missing после `GetOrCreate == null` тоже выдаёт успех (`:303–309`).
- **P1 · Подмена предмета** — `Source/StoryStage_Site.cs:262–269`, `:345–353`, `:472–501`. Караваны NPC отфильтрованы. Но если настоящий предмет уничтожили, унесли в holder или карта пропала, подходит любой предмет того же Def в любом караване игрока. Даже отказ `TryPlaceThing` оставляет флаг «создан». Требовать подтверждённое размещение и уникальный ID или сохраняемую метку, которая переживает перенос. Исключить слияние с произвольным стеком, для утери возвращать отдельный исход.
- **P1 · Откат враждебности не гарантирован** — `Source/StoryStage_ColonyRaid.cs:152–200`, `:384–406`, `Source/SparkStory.cs:208–215`, `:238–264`. Нейтральное отношение с goodwill −50 после принудительных −100 останется враждебным даже при возврате −50: ваниль снимает Hostile при 0. Отказ `TryAffectGoodwillWith` игнорируется, флаг отката сбрасывается. Исключение при спавне или удалённый Def/этап обходят Finish целиком. Проверять переход и откат обеих величин с учётом порогов и запрета goodwill. Хранить запись для очистки до мутации и обрабатывать её при ошибке, таймауте и миграции независимо от Def. Учесть параллельные рейды одной фракции и изменения отношений во время рейда.
- **P1 · Неверная карта или группа рейда** — `Source/StoryStage_ColonyRaid.cs:83–123`, `:203–301`. После смены выбранной домашней карты или загрузки рейд проверяется на текущей `ctx.Home`: сохранённых ID там нет и получается успех. При отсутствии ID код проверяет любых пешек фракции и может дать успех до высадки или ждать посторонних. Сохранять ID карты и полную когорту инцидента (включая капсулы) вместе с его lord. Потерю карты и пустую идентификацию обрабатывать явно без успеха. Проверять дедлайн до спавна даже при появлении дома после ожидания.
- **P2 · Hold засчитывает время до прибытия** — `Source/StoryStage_Site.cs:147–175`, `:357–371`. `heldTicks` и `lastHoldCheckTick` переживают reload, но первый опрос после входа прибавляет весь прошедший интервал, в том числе время без колониста. Присутствие между опросами не проверяется, при выполнении цели после дедлайна успех проверяется первым. Засчитывать подтверждённый интервал присутствия до дедлайна, определить и проверить правила накопления и повторного входа.
- **P2 · anyOf может оставить выбор без письма** — `Source/StoryLetter.cs:85–95`, `Source/SparkStory.cs:139–148`. Условия в UI и при клике совпадают: все requires и хотя бы одно anyOf, пустой список совместим со старыми XML. Если условие перестало действовать, Resolve уже удалил письмо, Choose отказывает, а вызов Removed подавлен. До удаления повторно проверить условия или при отказе поставить переотправку. Добавить проверку синтаксиса anyOf вариантов в ConfigErrors (`Source/StoryDefs.cs:32–46` сейчас проверяет условия квеста).

## Сохранения, нагрузка и хук
- `Source/SparkStory.cs:50–54,188–205,302–308`: vars сохраняются значениями, переход записывает один исход и выводит этап из обработки перед эффектами. Clear требует сгенерированную карту (`Source/StoryStage_Site.cs:275–283`). Активные квесты тикают без дома. Пропавший Site обычно приводит к timeout, но Retrieve может опередить проверку. Отсутствующий deadline при незавершённой миграции переустанавливает запасной срок на каждом опросе.
- `Source/StoryStage_ColonyRaid.cs:265–274,325–373`, `Source/StoryStage_Site.cs:419,460–480`: каждые 250 тиков рейд в худшем случае сканирует все вещи и пешек карты для каждого ID. Retrieve сканирует все вещи карты и собирает инвентари караванов, KillHero/Rescue обходят всех мёртвых в WorldPawns. Разрешать ID пачкой за один обход, ограничить поиск нужными держателями и картами, использовать запись героя для подтверждения смерти.
- `Source/SparkStory.cs:195,286–292`, `Source/StoryStage_Site.cs:135–142`, `Source/StoryStage_ColonyRaid.cs:76–81`: прямого вызова TestForceOutcome из проверенного игрового кода нет. Вход требует `-tmm-story-test`, но читатель ключа в Scribe vars действует без этого флага и обходит Tick, следовательно и добавленную очистку E2. Удалить дубли обработчиков, держать форсирование вне сейва, валидировать исход и проводить его через общий выход из этапа.

## Доказательства и границы
- `git diff --no-index` между Neon и корнем, exit 1 (различия): Site +26/−14, Raid +16/−8, Probe +318/−48. Основные отличия E2 — сигнатуры 1.7, выбор SitePart, goodwill и добавленные ветки forceOutcome. Риски переноса героев, зачёта предмета, удаления карты и отслеживания рейда уже присутствуют в кандидате. `git diff` по трём файлам ядра, exit 0: добавление anyOf и два вызова Holds.
- `Tools/cecil_methods.ps1 -Type <тип> -Name <метод> -IL`, exit 0: `Site.Destroy/ShouldRemoveMapNow`, `MapParent.Destroy/PostRemove`, `WorldObjectsHolder.Remove`, `FactionRelation.CheckKindThresholds`, `Faction.CanChangeGoodwillFor/SetRelationDirect`. Ванильный TimeoutComp ждёт отсутствия карты, но прямой Destroy квеста этого не делает.
- REPORT Antigravity заявляет fresh 18/18 и reload 6/6 на DLL `6679e7ccad96c3b09524262d112ba7bcf23d1a243b4867339c1a352ab3782be4` (runtimeErrors=0, unresolvedReferences=0, gpuLost=false). Текущий SHA256 DLL `e8191afa28523f799e9e99df3efb2cd2911cf6d0acd1683b35e79731c29db1f7`. Это разные сборки, результат для текущего дерева не подтверждён. Проверенные исходники Site/Raid/Probe имеют git blob `422784c5`/`9e74b847`/`a5c0ba5f`.
- `Tests/InGameStoryStagesProbe.cs:524–572,703–745,881–885`: reload только до входа в Clear, сравнивается запись истории, а не размер награды. Проба рейда может посчитать только строки ID и убивает всех враждебных пешек на карте, Hold переводит часы через DebugSetTicksGame. UI-файл всегда пишет passed=true. Убрать этот PASS и повторить пробы на итоговом DLL: перезагрузка с пленником/предметом/частичным Hold, вылет рейда с нейтральной фракцией и второй колонией, таймаут с лежачей пешкой, откат отношений при ошибке и удалённом этапе.
- Ревью статическое. Сборку и игру не запускал. Изменены только REPORT и ACK.


## Текущий код: Source/StoryStage_Site.cs
```csharp
using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TurboMegaMod
{
    public enum StorySiteObjective
    {
        Clear,
        Rescue,
        Retrieve,
        Hold,
        KillHero
    }

    /// <summary>
    /// Quest stage that spawns a world-map Site and tracks completion of a real-world objective
    /// (Clear, Rescue, Retrieve, Hold, KillHero) or timeout. All runtime state lives in ctx.run.vars.
    /// </summary>
    public sealed class StoryStage_Site : StoryStage
    {
        public const float TilesPerDay = 4f;

        public SitePartDef sitePart;
        public string enemyFaction;
        public float minDays = 2f;
        public float maxDays = 6f;
        public StorySiteObjective objective = StorySiteObjective.Clear;
        public HeroDef hero;
        public ThingDef thing;
        public float holdHours = 4f;
        public float timeoutDays = 15f;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "site"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            int now = Find.TickManager.TicksGame;
            int deadline = now + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));
            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["deadlineTick"] = deadline.ToString();
            ctx.run.vars["mapInitialized"] = "false";
            ctx.run.vars["enteredMap"] = "false";
            ctx.run.vars["heldTicks"] = "0";
            ctx.run.vars["lastHoldCheckTick"] = now.ToString();

            PlanetTile homeTile = ResolveHomeTile(ctx); // CHECK API
            int minTiles = Mathf.Max(1, Mathf.RoundToInt(minDays * TilesPerDay));
            int maxTiles = Mathf.Max(minTiles, Mathf.RoundToInt(maxDays * TilesPerDay));

            PlanetTile siteTile; // CHECK API
            bool foundTile = TryFindSiteTile(homeTile, minTiles, maxTiles, out siteTile);
            if (!foundTile)
            {
                foundTile = TryFindSiteTile(homeTile, 1, Mathf.Max(maxTiles * 2, 30), out siteTile);
            }
            if (!foundTile)
            {
                Log.Warning("[TMM] StoryStage_Site: failed to find world tile for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                ctx.run.vars["siteId"] = "-1";
                return;
            }

            Faction fac = ResolveEnemyFaction(ctx);
            SitePartDef part = ResolveSitePart();
            if (part == null)
            {
                Log.Warning("[TMM] StoryStage_Site: no valid SitePartDef found.");
                ctx.run.vars["siteId"] = "-1";
                return;
            }

            float points = ctx.Home != null ? StorytellerUtility.DefaultThreatPointsNow(ctx.Home) : 400f;
            SitePartParams parms = part.Worker.GenerateDefaultParams(points, siteTile, fac); // CHECK API
            List<SitePartDefWithParams> parts = new List<SitePartDefWithParams>();
            parts.Add(new SitePartDefWithParams(part, parms));

            Site site = SiteMaker.MakeSite(parts, siteTile, fac, true, null); // CHECK API
            if (site == null)
            {
                Log.Warning("[TMM] StoryStage_Site: SiteMaker.MakeSite returned null.");
                ctx.run.vars["siteId"] = "-1";
                return;
            }

            TimeoutComp timeoutComp = site.GetComponent<TimeoutComp>();
            if (timeoutComp != null)
            {
                // Give RimWorld's own TimeoutComp a buffer so StoryStage_Site handles timeout deterministically.
                timeoutComp.StartTimeout(Mathf.CeilToInt((timeoutDays + 2f) * GenDate.TicksPerDay)); // CHECK API
            }

            Find.WorldObjects.Add(site);
            ctx.run.vars["siteId"] = site.ID.ToString();

            string letterTitle = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Site"), ctx);
            string letterBody = StoryUtil.Format(!text.NullOrEmpty() ? text : Objective(ctx), ctx);
            Find.LetterStack.ReceiveLetter(letterTitle, letterBody, LetterDefOf.NeutralEvent, new LookTargets(site)); // CHECK API
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            int siteId = GetInt(ctx, "siteId", -1);
            Site site = FindSiteById(siteId);

            string forced = null;
            if (ctx.run.vars.TryGetValue("forceOutcome", out forced) && !forced.NullOrEmpty())
            {
                ctx.run.vars.Remove("forceOutcome");
                RemoveSiteSafely(site);
                ctx.run.vars["finished"] = forced;
                return forced;
            }

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now + GenDate.TicksPerDay);

            if (site != null && site.HasMap && site.Map != null)
            {
                ctx.run.vars["enteredMap"] = "true";
                if (!GetBool(ctx, "mapInitialized"))
                {
                    InitializeSiteMap(ctx, site);
                    ctx.run.vars["mapInitialized"] = "true";
                }
            }

            string objOutcome = EvaluateObjective(ctx, site, now);
            if (!objOutcome.NullOrEmpty())
            {
                ctx.run.vars["finished"] = objOutcome;
                return objOutcome;
            }

            if (site == null)
            {
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            if (now >= deadline)
            {
                RemoveSiteSafely(site);
                ctx.run.vars["finished"] = "timeout";
                return "timeout";
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : text, ctx);
            if (baseText.NullOrEmpty())
            {
                baseText = DefaultObjectiveText();
            }

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            int deadline = GetInt(ctx, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);

            if (objective == StorySiteObjective.Hold)
            {
                int heldTicks = GetInt(ctx, "heldTicks", 0);
                float hoursDone = heldTicks / (float)GenDate.TicksPerHour;
                return string.Format("{0} ({1:0.0}/{2:0.0}h, {3:0.0}d left)", baseText, hoursDone, holdHours, daysLeft);
            }

            return string.Format("{0} ({1:0.0}d left)", baseText, daysLeft);
        }

        private string DefaultObjectiveText()
        {
            if (objective == StorySiteObjective.Clear) return "Clear hostile site";
            if (objective == StorySiteObjective.Rescue) return "Rescue captive hero";
            if (objective == StorySiteObjective.Retrieve) return "Retrieve target item";
            if (objective == StorySiteObjective.Hold) return "Hold site";
            if (objective == StorySiteObjective.KillHero) return "Eliminate target hero";
            return "Complete site objective";
        }

        private void InitializeSiteMap(StoryContext ctx, Site site)
        {
            Map map = site.Map;
            if (map == null) return;

            if (objective == StorySiteObjective.KillHero && hero != null)
            {
                Pawn target = HeroRegistry.Instance.GetOrCreate(hero);
                if (target != null && !target.Dead)
                {
                    if (target.Spawned) target.DeSpawn(DestroyMode.Vanish);
                    if (site.Faction != null && target.Faction != site.Faction)
                    {
                        target.SetFaction(site.Faction);
                    }
                    IntVec3 cell = FindSpawnCellNearCenter(map);
                    GenSpawn.Spawn(target, cell, map, WipeMode.Vanish);
                    AttachToHostileLord(map, site.Faction, target, cell);
                    ctx.run.vars["targetPawnId"] = target.ThingID;
                    ctx.run.vars["heroSpawned"] = "true";
                }
            }
            else if (objective == StorySiteObjective.Rescue && hero != null)
            {
                Pawn captive = HeroRegistry.Instance.GetOrCreate(hero);
                if (captive != null && !captive.Dead)
                {
                    if (captive.IsCaravanMember())
                    {
                        ctx.run.vars["targetPawnId"] = captive.ThingID;
                        ctx.run.vars["heroSpawned"] = "true";
                        return;
                    }
                    if (captive.Spawned) captive.DeSpawn(DestroyMode.Vanish);
                    if (site.Faction != null && captive.Faction == site.Faction)
                    {
                        captive.SetFaction(null);
                    }
                    IntVec3 cell = FindSpawnCellNearCenter(map);
                    GenSpawn.Spawn(captive, cell, map, WipeMode.Vanish);
                    if (captive.guest != null && site.Faction != null)
                    {
                        captive.guest.SetGuestStatus(site.Faction, GuestStatus.Prisoner); // CHECK API
                    }
                    ctx.run.vars["targetPawnId"] = captive.ThingID;
                    ctx.run.vars["heroSpawned"] = "true";
                }
            }
            else if (objective == StorySiteObjective.Retrieve && thing != null)
            {
                ThingDef stuff = thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null;
                Thing item = ThingMaker.MakeThing(thing, stuff);
                IntVec3 cell = FindSpawnCellNearCenter(map);
                GenPlace.TryPlaceThing(item, cell, map, ThingPlaceMode.Near);
                ctx.run.vars["targetThingId"] = item.ThingID;
                ctx.run.vars["thingSpawned"] = "true";
            }
        }

        private string EvaluateObjective(StoryContext ctx, Site site, int now)
        {
            if (objective == StorySiteObjective.Clear)
            {
                if (site != null && site.HasMap && site.Map != null && GetBool(ctx, "enteredMap"))
                {
                    if (!HasLivingHostilePawns(site.Map))
                    {
                        return "success";
                    }
                }
                return null;
            }

            if (objective == StorySiteObjective.KillHero)
            {
                string pawnId = GetStr(ctx, "targetPawnId");
                if (pawnId.NullOrEmpty() && hero != null)
                {
                    Pawn existing = HeroRegistry.Instance.GetOrCreate(hero);
                    if (existing != null)
                    {
                        pawnId = existing.ThingID;
                        ctx.run.vars["targetPawnId"] = pawnId;
                    }
                }
                if (IsPawnDeadById(pawnId, site != null ? site.Map : null))
                {
                    return "success";
                }
                if (GetBool(ctx, "heroSpawned") && hero != null)
                {
                    Pawn alive = HeroRegistry.Instance.GetOrCreate(hero);
                    if (alive == null || alive.Dead)
                    {
                        return "success";
                    }
                }
                return null;
            }

            if (objective == StorySiteObjective.Rescue)
            {
                string pawnId = GetStr(ctx, "targetPawnId");
                if (pawnId.NullOrEmpty() && hero != null)
                {
                    Pawn existing = HeroRegistry.Instance.GetOrCreate(hero);
                    if (existing != null)
                    {
                        pawnId = existing.ThingID;
                        ctx.run.vars["targetPawnId"] = pawnId;
                    }
                }
                if (IsPawnDeadById(pawnId, site != null ? site.Map : null))
                {
                    return "failed";
                }
                if (IsPawnInPlayerCaravan(pawnId) || IsPawnRescuedAtHome(pawnId, ctx.Home))
                {
                    return "success";
                }
                if (GetBool(ctx, "heroSpawned") && hero != null)
                {
                    Pawn alive = HeroRegistry.Instance.GetOrCreate(hero);
                    if (alive == null || alive.Dead)
                    {
                        return "failed";
                    }
                }
                return null;
            }

            if (objective == StorySiteObjective.Retrieve)
            {
                if (!GetBool(ctx, "enteredMap")) return null;
                string thingId = GetStr(ctx, "targetThingId");
                bool stillOnSiteMap = site != null && site.HasMap && site.Map != null && IsThingOnMap(site.Map, thingId);
                if (IsTargetThingInPlayerCaravan(thingId, thing, !stillOnSiteMap))
                {
                    return "success";
                }
                return null;
            }

            if (objective == StorySiteObjective.Hold)
            {
                int lastTick = GetInt(ctx, "lastHoldCheckTick", now);
                int dt = Mathf.Max(0, now - lastTick);
                ctx.run.vars["lastHoldCheckTick"] = now.ToString();

                if (site != null && site.HasMap && site.Map != null && HasStandingPlayerPawns(site.Map))
                {
                    int held = GetInt(ctx, "heldTicks", 0) + dt;
                    ctx.run.vars["heldTicks"] = held.ToString();
                    int targetTicks = Mathf.Max(1, Mathf.RoundToInt(holdHours * GenDate.TicksPerHour));
                    if (held >= targetTicks)
                    {
                        return "success";
                    }
                }
                return null;
            }

            return null;
        }

        private static bool HasLivingHostilePawns(Map map)
        {
            if (map == null || map.mapPawns == null) return false;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed || p.Downed) continue;
                if (p.IsPrisonerOfColony) continue;
                if (p.HostileTo(Faction.OfPlayer)) return true;
            }
            return false;
        }

        private static bool HasStandingPlayerPawns(Map map)
        {
            if (map == null || map.mapPawns == null) return false;
            IReadOnlyList<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && !p.Downed && !p.Destroyed) return true;
            }
            return false;
        }

        private static bool IsPawnDeadById(string pawnId, Map siteMap)
        {
            if (pawnId.NullOrEmpty()) return false;

            if (siteMap != null)
            {
                List<Thing> corpses = siteMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                for (int i = 0; i < corpses.Count; i++)
                {
                    Corpse c = corpses[i] as Corpse;
                    if (c != null && c.InnerPawn != null && c.InnerPawn.ThingID == pawnId) return true;
                }
            }

            foreach (Pawn p in Find.WorldPawns.AllPawnsDead)
            {
                if (p != null && p.ThingID == pawnId) return true;
            }

            return false;
        }

        private static bool IsPawnInPlayerCaravan(string pawnId)
        {
            if (pawnId.NullOrEmpty()) return false;
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Faction != Faction.OfPlayer) continue;
                List<Pawn> pawns = c.PawnsListForReading;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn p = pawns[j];
                    if (p != null && !p.Dead && p.ThingID == pawnId) return true;
                }
            }
            return false;
        }

        private static bool IsPawnRescuedAtHome(string pawnId, Map home)
        {
            if (pawnId.NullOrEmpty() || home == null || home.mapPawns == null) return false;
            IReadOnlyList<Pawn> pawns = home.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && !p.Dead && p.ThingID == pawnId && (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsThingOnMap(Map map, string thingId)
        {
            if (map == null || thingId.NullOrEmpty()) return false;
            List<Thing> all = map.listerThings.AllThings;
            for (int i = 0; i < all.Count; i++)
            {
                Thing t = all[i];
                if (t != null && !t.Destroyed && t.ThingID == thingId) return true;
            }
            return false;
        }

        private static bool IsTargetThingInPlayerCaravan(string thingId, ThingDef targetDef, bool allowDefMatch)
        {
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || c.Faction != Faction.OfPlayer) continue;

                List<Thing> items = CaravanInventoryUtility.AllInventoryItems(c); // CHECK API
                if (items != null)
                {
                    for (int j = 0; j < items.Count; j++)
                    {
                        Thing t = items[j];
                        if (t == null || t.Destroyed) continue;
                        if (!thingId.NullOrEmpty() && t.ThingID == thingId) return true;
                        if (allowDefMatch && targetDef != null && t.def == targetDef && t.stackCount > 0) return true;
                    }
                }

                List<Pawn> pawns = c.PawnsListForReading;
                for (int pIdx = 0; pIdx < pawns.Count; pIdx++)
                {
                    Pawn p = pawns[pIdx];
                    if (p == null) continue;
                    if (p.equipment != null && p.equipment.Primary != null)
                    {
                        Thing eq = p.equipment.Primary;
                        if (!thingId.NullOrEmpty() && eq.ThingID == thingId) return true;
                        if (allowDefMatch && targetDef != null && eq.def == targetDef) return true;
                    }
                }
            }
            return false;
        }

        private static IntVec3 FindSpawnCellNearCenter(Map map)
        {
            IntVec3 center = map.Center;
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(center, map, 14, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out found))
            {
                return found;
            }
            if (CellFinder.TryFindRandomCellNear(center, map, 25, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map);
            }, out found))
            {
                return found;
            }
            return center;
        }

        private static void AttachToHostileLord(Map map, Faction fac, Pawn pawn, IntVec3 cell)
        {
            if (map == null || fac == null || pawn == null) return;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord l = lords[i];
                if (l != null && l.faction == fac)
                {
                    l.AddPawn(pawn);
                    return;
                }
            }
            List<Pawn> group = new List<Pawn>();
            group.Add(pawn);
            LordMaker.MakeNewLord(fac, new LordJob_DefendPoint(cell), map, group);
        }

        private static void RemoveSiteSafely(Site site)
        {
            if (site == null || site.Destroyed) return;
            if (site.HasMap && site.Map != null)
            {
                if (HasStandingPlayerPawns(site.Map)) return;
            }
            site.Destroy();
        }

        private static Site FindSiteById(int siteId)
        {
            if (siteId < 0) return null;
            List<Site> sites = Find.WorldObjects.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                Site s = sites[i];
                if (s != null && !s.Destroyed && s.ID == siteId) return s;
            }
            return null;
        }

        private PlanetTile ResolveHomeTile(StoryContext ctx) // CHECK API
        {
            if (ctx.Home != null) return ctx.Home.Tile; // CHECK API
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (settlements[i] != null && settlements[i].Faction == Faction.OfPlayer)
                {
                    return settlements[i].Tile; // CHECK API
                }
            }
            return PlanetTile.Invalid; // CHECK API
        }

        private static bool TryFindSiteTile(PlanetTile rootTile, int minTiles, int maxTiles, out PlanetTile tile) // CHECK API
        {
            if (rootTile.Valid) // CHECK API
            {
                if (TileFinder.TryFindPassableTileWithTraversalDistance(rootTile, minTiles, maxTiles, out tile, delegate(PlanetTile t)
                {
                    return !Find.WorldObjects.AnyWorldObjectAt(t) && TileFinder.IsValidTileForNewSettlement(t, null, false);
                }, false, TileFinderMode.Near, false, false)) // CHECK API
                {
                    return true;
                }
            }
            return TileFinder.TryFindNewSiteTile(out tile, rootTile, minTiles, maxTiles, false, null, 0f, false, TileFinderMode.Near, false, false, null, null); // CHECK API
        }

        private Faction ResolveEnemyFaction(StoryContext ctx)
        {
            if (!enemyFaction.NullOrEmpty())
            {
                Faction explicitFac = StoryUtil.FactionOf(enemyFaction);
                if (explicitFac != null && !explicitFac.defeated) return explicitFac;
            }

            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && f.HostileTo(Faction.OfPlayer))
                {
                    return f;
                }
            }
            return Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined); // CHECK API
        }

        private SitePartDef ResolveSitePart()
        {
            if (sitePart != null) return sitePart;
            SitePartDef outpost = DefDatabase<SitePartDef>.GetNamedSilentFail("BanditCamp");
            if (outpost != null) return outpost;
            outpost = DefDatabase<SitePartDef>.GetNamedSilentFail("Outpost");
            if (outpost != null) return outpost;
            List<SitePartDef> all = DefDatabase<SitePartDef>.AllDefsListForReading;
            if (all != null && all.Count > 0) return all[0];
            return null;
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            string raw;
            int val;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
        }

        private static bool GetBool(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }
    }
}

```

## Текущий код: Source/StoryStage_ColonyRaid.cs
```csharp
using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    /// <summary>
    /// Quest stage that launches a vanilla IncidentDefOf.RaidEnemy raid against the player's home map
    /// after delayHours, temporarily turning non-hostile factions hostile if required, and completes
    /// when all raiders are dead, downed, captured, or have fled (or on timeoutDays).
    /// </summary>
    public sealed class StoryStage_ColonyRaid : StoryStage
    {
        public string faction;
        public float pointsFactor = 1.0f;
        public float delayHours = 2.0f;
        public float timeoutDays = 3.0f;
        public bool restoreRelations = false;
        public List<StoryChoice> outcomes = new List<StoryChoice>();

        public override string Mechanic
        {
            get { return "raid"; }
        }

        public override int NextFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            return c != null ? c.nextStage : -2;
        }

        public override IEnumerable<StoryEffect> EffectsFor(string outcome)
        {
            StoryChoice c = FindOutcome(outcome);
            if (c == null || c.effects == null) yield break;
            for (int i = 0; i < c.effects.Count; i++)
            {
                if (c.effects[i] != null) yield return c.effects[i];
            }
        }

        public override void Start(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return;

            int now = Find.TickManager.TicksGame;
            int delayTicks = Mathf.Max(0, Mathf.RoundToInt(delayHours * GenDate.TicksPerHour));
            int spawnTick = now + delayTicks;
            int deadlineTick = spawnTick + Mathf.Max(GenDate.TicksPerHour, Mathf.RoundToInt(timeoutDays * GenDate.TicksPerDay));

            ctx.run.vars["startTick"] = now.ToString();
            ctx.run.vars["spawnTick"] = spawnTick.ToString();
            ctx.run.vars["deadlineTick"] = deadlineTick.ToString();
            ctx.run.vars["spawned"] = "false";
            ctx.run.vars["raidLordId"] = "-1";

            if (!text.NullOrEmpty() && delayHours > 0f)
            {
                string title = StoryUtil.Format(!label.NullOrEmpty() ? label : (ctx.def != null ? ctx.def.label : "Incoming Raid"), ctx);
                string body = StoryUtil.Format(text, ctx);
                Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.ThreatSmall, ctx.Home != null ? new LookTargets(ctx.Home.Center, ctx.Home) : LookTargets.Invalid); // CHECK API
            }
        }

        public override string Tick(StoryContext ctx)
        {
            if (ctx == null || ctx.run == null) return null;
            if (ctx.run.vars.ContainsKey("finished")) return null;

            string forced = null;
            if (ctx.run.vars.TryGetValue("forceOutcome", out forced) && !forced.NullOrEmpty())
            {
                ctx.run.vars.Remove("forceOutcome");
                return Finish(ctx, forced);
            }

            int now = Find.TickManager.TicksGame;
            int spawnTick = GetInt(ctx, "spawnTick", now);
            int deadlineTick = GetInt(ctx, "deadlineTick", now + GenDate.TicksPerDay);
            Map home = ctx.Home;

            if (!GetBool(ctx, "spawned"))
            {
                if (now < spawnTick) return null;

                if (home == null)
                {
                    if (now >= deadlineTick)
                    {
                        return Finish(ctx, "timeout");
                    }
                    return null;
                }

                if (!SpawnRaid(ctx, home))
                {
                    Log.Warning("[TMM] StoryStage_ColonyRaid: failed to spawn raid for quest " + (ctx.def != null ? ctx.def.defName : "null"));
                    return Finish(ctx, "timeout");
                }

                ctx.run.vars["spawned"] = "true";
                return null;
            }

            if (now >= deadlineTick)
            {
                return Finish(ctx, "timeout");
            }

            if (home == null)
            {
                return Finish(ctx, "timeout");
            }

            if (IsRaidCleared(ctx, home))
            {
                return Finish(ctx, "success");
            }

            return null;
        }

        public override string Objective(StoryContext ctx)
        {
            string baseText = StoryUtil.Format(!label.NullOrEmpty() ? label : text, ctx);
            if (baseText.NullOrEmpty())
            {
                baseText = "Repel incoming raid";
            }

            if (ctx == null || ctx.run == null) return baseText;

            int now = Find.TickManager.TicksGame;
            if (!GetBool(ctx, "spawned"))
            {
                int spawnTick = GetInt(ctx, "spawnTick", now);
                float hoursLeft = Mathf.Max(0f, (spawnTick - now) / (float)GenDate.TicksPerHour);
                return string.Format("{0} (arriving in {1:0.0}h)", baseText, hoursLeft);
            }

            int deadline = GetInt(ctx, "deadlineTick", now);
            float daysLeft = Mathf.Max(0f, (deadline - now) / (float)GenDate.TicksPerDay);
            return string.Format("{0} ({1:0.0}d left)", baseText, daysLeft);
        }

        private bool SpawnRaid(StoryContext ctx, Map home)
        {
            Faction raidFac = ResolveRaidFaction(ctx);
            if (raidFac == null) return false;

            ctx.run.vars["raidFactionId"] = raidFac.loadID.ToString();

            if (!raidFac.HostileTo(Faction.OfPlayer))
            {
                ctx.run.vars["wasNonHostile"] = "true";
                ctx.run.vars["origGoodwill"] = raidFac.PlayerGoodwill.ToString();
                ctx.run.vars["origKind"] = ((int)raidFac.RelationKindWith(Faction.OfPlayer)).ToString();
                ForceHostile(raidFac);
            }
            else
            {
                ctx.run.vars["wasNonHostile"] = "false";
            }

            HashSet<int> prevPawns = SnapshotFactionPawns(home, raidFac);
            HashSet<int> prevLords = SnapshotLords(home);

            float rawPoints = StorytellerUtility.DefaultThreatPointsNow(home) * Mathf.Max(0.1f, pointsFactor);
            float minPoints = 150f;
            if (raidFac.def != null)
            {
                float facMin = raidFac.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null); // CHECK API
                if (facMin > minPoints) minPoints = facMin;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, home);
            parms.target = home;
            parms.faction = raidFac;
            parms.points = Mathf.Max(minPoints, rawPoints);
            parms.forced = true;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;

            bool executed = IncidentDefOf.RaidEnemy.Worker.TryExecute(parms); // CHECK API
            if (!executed)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                executed = IncidentDefOf.RaidEnemy.Worker.TryExecute(parms); // CHECK API
            }

            if (!executed)
            {
                RestoreRelationsIfNeeded(ctx);
                return false;
            }

            List<int> trackedIds = new List<int>();
            int newLordId = -1;

            List<Lord> lords = home.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord l = lords[i];
                if (l == null || l.faction != raidFac) continue;
                if (!prevLords.Contains(l.loadID))
                {
                    newLordId = l.loadID;
                    if (l.ownedPawns != null)
                    {
                        for (int pIdx = 0; pIdx < l.ownedPawns.Count; pIdx++)
                        {
                            Pawn p = l.ownedPawns[pIdx];
                            if (p != null && !trackedIds.Contains(p.thingIDNumber))
                            {
                                trackedIds.Add(p.thingIDNumber);
                            }
                        }
                    }
                    break;
                }
            }

            IReadOnlyList<Pawn> spawned = home.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawned.Count; i++)
            {
                Pawn p = spawned[i];
                if (p == null || p.Faction != raidFac) continue;
                if (!prevPawns.Contains(p.thingIDNumber) && !trackedIds.Contains(p.thingIDNumber))
                {
                    trackedIds.Add(p.thingIDNumber);
                }
            }

            ctx.run.vars["raidLordId"] = newLordId.ToString();
            ctx.run.vars["raidPawnIds"] = JoinInts(trackedIds);
            return true;
        }

        private bool IsRaidCleared(StoryContext ctx, Map home)
        {
            List<int> trackedIds = ParseInts(GetStr(ctx, "raidPawnIds"));
            int lordId = GetInt(ctx, "raidLordId", -1);
            int facId = GetInt(ctx, "raidFactionId", -1);

            Lord activeLord = null;
            if (lordId >= 0)
            {
                List<Lord> lords = home.lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    if (lords[i] != null && lords[i].loadID == lordId)
                    {
                        activeLord = lords[i];
                        break;
                    }
                }
            }

            if (trackedIds.Count > 0)
            {
                for (int i = 0; i < trackedIds.Count; i++)
                {
                    Pawn p = FindPawnOnMapOrPods(home, trackedIds[i]);
                    if (p == null) continue;
                    if (IsActiveRaider(p, home)) return false;
                }
                return true;
            }

            if (lordId >= 0)
            {
                if (activeLord == null) return true;
                if (activeLord.ownedPawns != null)
                {
                    for (int i = 0; i < activeLord.ownedPawns.Count; i++)
                    {
                        Pawn p = activeLord.ownedPawns[i];
                        if (IsActiveRaider(p, home)) return false;
                    }
                }
                return true;
            }

            Faction raidFac = FindFactionById(facId);
            if (raidFac != null)
            {
                IReadOnlyList<Pawn> pawns = home.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p != null && p.Faction == raidFac && IsActiveRaider(p, home)) return false;
                }
            }
            return true;
        }

        private static bool IsActiveRaider(Pawn p, Map home)
        {
            if (p == null || p.Dead || p.Destroyed) return false;
            if (p.IsPrisonerOfColony) return false;

            // Still inside an incoming drop pod on the home map.
            if (!p.Spawned && p.MapHeld == home) return true;
            if (!p.Spawned || p.Map != home) return false;
            if (p.Downed) return false;

            if (p.mindState != null && p.mindState.duty != null)
            {
                DutyDef d = p.mindState.duty.def;
                if (d == DutyDefOf.ExitMapBest || d == DutyDefOf.ExitMapBestAndDefendSelf || d == DutyDefOf.ExitMapRandom)
                {
                    return false;
                }
            }

            return true;
        }

        private static Pawn FindPawnOnMapOrPods(Map home, int thingIdNumber)
        {
            IReadOnlyList<Pawn> spawned = home.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawned.Count; i++)
            {
                Pawn p = spawned[i];
                if (p != null && p.thingIDNumber == thingIdNumber) return p;
            }

            List<Thing> skyfallers = home.listerThings.ThingsInGroup(ThingRequestGroup.ActiveTransporter);
            for (int i = 0; i < skyfallers.Count; i++)
            {
                Pawn found = FindPawnInHolder(skyfallers[i] as IThingHolder, thingIdNumber);
                if (found != null) return found;
            }

            List<Thing> allThings = home.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                DropPodIncoming incoming = allThings[i] as DropPodIncoming;
                if (incoming != null)
                {
                    Pawn found = FindPawnInHolder(incoming.Contents, thingIdNumber);
                    if (found != null) return found;
                }
            }

            return null;
        }

        private static Pawn FindPawnInHolder(IThingHolder holder, int thingIdNumber)
        {
            if (holder == null) return null;
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner != null)
            {
                for (int i = 0; i < owner.Count; i++)
                {
                    Pawn p = owner[i] as Pawn;
                    if (p != null && p.thingIDNumber == thingIdNumber) return p;
                }
            }
            List<IThingHolder> children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            for (int i = 0; i < children.Count; i++)
            {
                Pawn found = FindPawnInHolder(children[i], thingIdNumber);
                if (found != null) return found;
            }
            return null;
        }

        private string Finish(StoryContext ctx, string outcome)
        {
            RestoreRelationsIfNeeded(ctx);
            ctx.run.vars["finished"] = outcome;
            return outcome;
        }

        private void RestoreRelationsIfNeeded(StoryContext ctx)
        {
            if (!restoreRelations) return;
            if (!GetBool(ctx, "wasNonHostile")) return;

            int facId = GetInt(ctx, "raidFactionId", -1);
            Faction raidFac = FindFactionById(facId);
            if (raidFac == null) return;

            int origGoodwill = GetInt(ctx, "origGoodwill", 0);
            int origKindInt = GetInt(ctx, "origKind", (int)FactionRelationKind.Neutral);
            FactionRelationKind origKind = (FactionRelationKind)origKindInt;

            int delta = origGoodwill - raidFac.PlayerGoodwill;
            if (delta != 0)
            {
                raidFac.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null); // CHECK API
            }
            if (!raidFac.HasGoodwill && raidFac.RelationKindWith(Faction.OfPlayer) != origKind)
            {
                raidFac.SetRelationDirect(Faction.OfPlayer, origKind, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }
            ctx.run.vars["wasNonHostile"] = "false";
        }

        private static void ForceHostile(Faction fac)
        {
            if (fac == null || fac.HostileTo(Faction.OfPlayer)) return;
            int drop = -100 - fac.PlayerGoodwill;
            if (drop < 0)
            {
                fac.TryAffectGoodwillWith(Faction.OfPlayer, drop, false, false, null, null); // CHECK API
            }
            if (!fac.HasGoodwill && !fac.HostileTo(Faction.OfPlayer))
            {
                fac.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, GlobalTargetInfo.Invalid); // CHECK API
            }
        }

        private Faction ResolveRaidFaction(StoryContext ctx)
        {
            if (!faction.NullOrEmpty())
            {
                Faction explicitFac = StoryUtil.FactionOf(faction);
                if (explicitFac != null && !explicitFac.defeated) return explicitFac;
            }
            if (ctx.Faction != null && !ctx.Faction.defeated && !ctx.Faction.IsPlayer)
            {
                return ctx.Faction;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i];
                if (f != null && !f.IsPlayer && !f.defeated && !f.Hidden && f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction)
                {
                    return f;
                }
            }
            return Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined); // CHECK API
        }

        private static Faction FindFactionById(int loadId)
        {
            if (loadId < 0) return null;
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].loadID == loadId) return all[i];
            }
            return null;
        }

        private static HashSet<int> SnapshotFactionPawns(Map map, Faction fac)
        {
            HashSet<int> ids = new HashSet<int>();
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && p.Faction == fac) ids.Add(p.thingIDNumber);
            }
            return ids;
        }

        private static HashSet<int> SnapshotLords(Map map)
        {
            HashSet<int> ids = new HashSet<int>();
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i] != null) ids.Add(lords[i].loadID);
            }
            return ids;
        }

        private static string JoinInts(List<int> list)
        {
            if (list == null || list.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(list[i]);
            }
            return sb.ToString();
        }

        private static List<int> ParseInts(string raw)
        {
            List<int> result = new List<int>();
            if (raw.NullOrEmpty()) return result;
            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val)) result.Add(val);
            }
            return result;
        }

        private StoryChoice FindOutcome(string outcome)
        {
            if (outcomes == null || outcome.NullOrEmpty()) return null;
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (outcomes[i] != null && outcomes[i].outcome == outcome) return outcomes[i];
            }
            return null;
        }

        private static int GetInt(StoryContext ctx, string key, int fallback)
        {
            string raw;
            int val;
            if (ctx.run.vars.TryGetValue(key, out raw) && int.TryParse(raw, out val)) return val;
            return fallback;
        }

        private static bool GetBool(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) && raw == "true";
        }

        private static string GetStr(StoryContext ctx, string key)
        {
            string raw;
            return ctx.run.vars.TryGetValue(key, out raw) ? raw : string.Empty;
        }
    }
}

```

## Ядро: Source/SparkStory.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TurboMegaMod
{
    // Appended values only: saves store the enum by name.
    public enum StoryQuestStatus { Offered, Active, Completed, Failed, Declined, Disabled }

    /// Runtime record of one quest. Saved by defName so a removed StoryQuestDef never breaks a save.
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

        public StoryQuestDef Def { get { return DefDatabase<StoryQuestDef>.GetNamedSilentFail(quest); } }

        public void ExposeData()
        {
            Scribe_Values.Look(ref quest, "quest");
            Scribe_Values.Look(ref status, "status", StoryQuestStatus.Offered);
            Scribe_Values.Look(ref stage, "stage", -1);
            Scribe_Values.Look(ref stageKey, "stageKey");
            Scribe_Values.Look(ref gen, "gen", 0);
            Scribe_Values.Look(ref outcome, "outcome");
            Scribe_Values.Look(ref chosenOutcome, "chosenOutcome");
            Scribe_Values.Look(ref pendingChoice, "pendingChoice", false);
            Scribe_Values.Look(ref needsLetter, "needsLetter", false);
            Scribe_Values.Look(ref stageUntilTick, "stageUntilTick", -1);
            Scribe_Values.Look(ref offeredTick, "offeredTick", -1);
            Scribe_Values.Look(ref cooldownUntilTick, "cooldownUntilTick", -1);
            Scribe_Values.Look(ref finishedTick, "finishedTick", -1);
            Scribe_Collections.Look(ref history, "history", LookMode.Value);
            Scribe_Collections.Look(ref vars, "vars", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (history == null) history = new List<string>();
                if (vars == null) vars = new Dictionary<string, string>();
            }
        }
    }

    /// The whole story state: flags, quest runs and the director (pacing). Design: Documentation/STORY-ENGINE-DESIGN.md.
    public sealed class SparkStory : WorldComponent
    {
        public const int TickInterval = 250;
        public static int MaxActive = 2;                 // non-main quests at once (offered faction quests and non-main autoStart)
        public static int MinOfferGapTicks = 30000;      // half a day between offers

        private Dictionary<string, string> flags = new Dictionary<string, string>();
        private List<StoryQuestRun> runs = new List<StoryQuestRun>();
        public int lastOfferTick = -999999;
        public string lastMechanic = "";
        private bool reconciled;                         // transient: reconcile once after load/start

        public SparkStory(World world) : base(world) { }

        public static SparkStory Instance { get { return Find.World == null ? null : Find.World.GetComponent<SparkStory>(); } }
        public IEnumerable<StoryQuestRun> Runs { get { return runs; } }
        public IEnumerable<KeyValuePair<string, string>> Flags { get { return flags; } }

        // ------------------------------------------------ flags
        public string GetFlag(string key) { string v; return key != null && flags.TryGetValue(key, out v) ? v : ""; }
        public int GetInt(string key) { int v; return int.TryParse(GetFlag(key), out v) ? v : 0; }
        public void SetFlag(string key, string value) { if (!key.NullOrEmpty()) flags[key] = value ?? ""; }

        // ------------------------------------------------ queries
        public StoryQuestRun RunFor(string quest) { return runs.FirstOrDefault(r => r.quest == quest); }
        public bool IsDone(string quest) { StoryQuestRun r = RunFor(quest); return r != null && (r.status == StoryQuestStatus.Completed || r.status == StoryQuestStatus.Failed); }
        public bool IsActive(string quest) { StoryQuestRun r = RunFor(quest); return r != null && r.status == StoryQuestStatus.Active; }

        public bool IsAvailable(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r != null && r.status != StoryQuestStatus.Declined) return false;
            if (r != null && Find.TickManager.TicksGame < r.cooldownUntilTick) return false;
            int act = Math.Max(1, GetInt("act"));
            if (d.act > act) return false;
            if (!d.faction.NullOrEmpty() && StoryUtil.FactionOf(d.faction) == null) return false;
            return StoryConditions.Holds(this, d.requires, d.anyOf);
        }

        public StoryContext Context(StoryQuestRun r) { return new StoryContext { state = this, run = r, def = r.Def }; }

        private int NonMainBusy()
        {
            return runs.Count(x => (x.status == StoryQuestStatus.Active || x.status == StoryQuestStatus.Offered) && x.Def != null && !x.Def.main);
        }

        // ------------------------------------------------ lifecycle
        public StoryQuestRun Offer(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r == null) { r = new StoryQuestRun { quest = d.defName }; runs.Add(r); }
            r.status = StoryQuestStatus.Offered;
            r.gen++;
            r.offeredTick = Find.TickManager.TicksGame;
            r.needsLetter = false;
            lastOfferTick = r.offeredTick;
            StoryChoiceLetter.Send(Context(r), -1, StoryChoiceLetter.KindOffer);
            return r;
        }

        /// Accept/Decline/Choose take the letter's generation and refuse stale or repeated clicks.
        public bool Accept(StoryQuestRun r, int letterGen)
        {
            if (r == null || r.status != StoryQuestStatus.Offered || r.gen != letterGen || r.Def == null) return false;
            r.status = StoryQuestStatus.Active;
            StartStage(r, 0);
            return true;
        }

        public bool Decline(StoryQuestRun r, int letterGen)
        {
            if (r == null || r.status != StoryQuestStatus.Offered || r.gen != letterGen) return false;
            StoryQuestDef d = r.Def;
            r.status = StoryQuestStatus.Declined;
            r.gen++;
            r.cooldownUntilTick = Find.TickManager.TicksGame + (int)((d == null ? 3f : d.offerCooldownDays) * GenDate.TicksPerDay);
            return true;
        }

        public bool Choose(StoryQuestRun r, int stage, int letterGen, string outcome)
        {
            if (r == null || r.status != StoryQuestStatus.Active || r.stage != stage || r.gen != letterGen || !r.pendingChoice) return false;
            StoryQuestDef d = r.Def;
            var st = d == null || stage < 0 || stage >= d.stages.Count ? null : d.stages[stage] as StoryStage_Choice;
            StoryChoice c = st == null ? null : st.ChoiceFor(outcome);
            if (c == null || !StoryConditions.Holds(this, c.requires, c.anyOf)) return false; // re-check requires at click time
            r.chosenOutcome = outcome;
            SafeTick(r); // resolve immediately so the player sees the consequence at once
            return true;
        }

        /// A letter of the current generation was dismissed unanswered: an offer counts as "later",
        /// a choice letter is re-sent once by the director (deduplicated by generation).
        public void Notify_LetterLost(StoryQuestRun r, string kind, int letterGen)
        {
            if (r == null || r.gen != letterGen) return;
            if (kind == StoryChoiceLetter.KindOffer) { Decline(r, letterGen); return; }
            if (r.status == StoryQuestStatus.Active && r.pendingChoice) r.needsLetter = true;
        }

        private void StartQuestDirect(StoryQuestDef d)
        {
            StoryQuestRun r = RunFor(d.defName);
            if (r == null) { r = new StoryQuestRun { quest = d.defName }; runs.Add(r); }
            r.status = StoryQuestStatus.Active;
            StartStage(r, 0);
        }

        private void StartStage(StoryQuestRun r, int index)
        {
            StoryQuestDef d = r.Def;
            if (d == null) { Disable(r, "def missing"); return; }
            if (index < 0 || index >= d.stages.Count) { Finish(r, StoryQuestStatus.Completed); return; }
            r.stage = index;
            r.stageKey = d.stages[index].Key(index);
            r.gen++;
            r.stageUntilTick = -1;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.chosenOutcome = null;
            r.vars.Clear();
            StoryStage st = d.stages[index];
            lastMechanic = st.Mechanic;
            st.Start(Context(r));
        }

        /// One stage tick with a single transition point: the outcome is recorded before any effect runs,
        /// each effect is isolated, so a failing effect can never make earlier effects run twice.
        private void TickRun(StoryQuestRun r)
        {
            StoryQuestDef d = r.Def;
            if (d == null || r.stage < 0 || r.stage >= d.stages.Count) { Disable(r, "stage missing"); return; }
            StoryStage st = d.stages[r.stage];
            StoryContext ctx = Context(r);
            string o, forced;
            if (r.vars.TryGetValue("forceOutcome", out forced)) { r.vars.Remove("forceOutcome"); o = forced; }
            else o = st.Tick(ctx);
            if (o == null) return;
            int from = r.stage;
            r.outcome = o;
            r.history.Add(from + ":" + o);
            r.stage = -1; // transition boundary: nothing re-enters this stage
            foreach (StoryEffect e in st.EffectsFor(o).ToList()) ApplySafe(e, ctx);
            if (o == "failed") { Finish(r, StoryQuestStatus.Failed); return; }
            int next = st.NextFor(o);
            StartStage(r, next == -2 ? from + 1 : next);
        }

        private void SafeTick(StoryQuestRun r)
        {
            try { TickRun(r); }
            catch (Exception ex)
            {
                Log.ErrorOnce("[TMM] Story: quest " + r.quest + " failed in stage " + r.stage + ": " + ex, ("TMMStory" + r.quest).GetHashCode());
                Disable(r, "exception");
            }
        }

        private static void ApplySafe(StoryEffect e, StoryContext ctx)
        {
            if (e == null) return;
            try { e.Apply(ctx); }
            catch (Exception ex) { Log.Error("[TMM] Story: effect " + e.GetType().Name + " in " + ctx.run.quest + " failed: " + ex); }
        }

        private void Finish(StoryQuestRun r, StoryQuestStatus status)
        {
            StoryQuestDef d = r.Def;
            r.status = status;
            r.stage = -1;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.finishedTick = Find.TickManager.TicksGame;
            SetFlag("q." + r.quest, r.outcome ?? (status == StoryQuestStatus.Failed ? "failed" : "done"));
            if (d != null && status == StoryQuestStatus.Completed)
                foreach (StoryEffect e in d.onComplete) ApplySafe(e, Context(r));
        }

        /// A quest whose Def or stage disappeared (mod update): keep history and flags, give no rewards, free all limits.
        private void Disable(StoryQuestRun r, string why)
        {
            if (r.status == StoryQuestStatus.Disabled) return;
            r.status = StoryQuestStatus.Disabled;
            r.pendingChoice = false;
            r.needsLetter = false;
            r.gen++;
            r.finishedTick = Find.TickManager.TicksGame;
            r.history.Add("disabled:" + why);
        }

        /// Once after load: drop runs of removed Defs, remap stages by stable key, resend exactly one letter
        /// for a valid pending offer/choice that has no live letter.
        private void Reconcile()
        {
            reconciled = true;
            var live = Find.LetterStack == null ? new List<StoryChoiceLetter>() : Find.LetterStack.LettersListForReading.OfType<StoryChoiceLetter>().ToList();
            foreach (StoryQuestRun r in runs.ToList())
            {
                if (r.status != StoryQuestStatus.Offered && r.status != StoryQuestStatus.Active) continue;
                StoryQuestDef d = r.Def;
                if (d == null) { Disable(r, "def missing"); continue; }
                if (r.status == StoryQuestStatus.Active)
                {
                    int byKey = r.stageKey.NullOrEmpty() ? r.stage : d.stages.FindIndex(s => s.Key(d.stages.IndexOf(s)) == r.stageKey);
                    if (byKey < 0) { Disable(r, "stage missing"); continue; }
                    r.stage = byKey;
                    if (r.pendingChoice && !live.Any(l => l.storyQuest == r.quest && l.gen == r.gen)) r.needsLetter = true;
                }
                else if (!live.Any(l => l.storyQuest == r.quest && l.gen == r.gen)) r.needsLetter = true;
            }
        }

        private void ResendLostLetters()
        {
            foreach (StoryQuestRun r in runs.Where(x => x.needsLetter).ToList())
            {
                r.needsLetter = false;
                if (r.Def == null) continue;
                if (r.status == StoryQuestStatus.Offered) StoryChoiceLetter.Send(Context(r), -1, StoryChoiceLetter.KindOffer);
                else if (r.status == StoryQuestStatus.Active && r.pendingChoice) StoryChoiceLetter.Send(Context(r), r.stage, StoryChoiceLetter.KindChoice);
            }
        }

        // ------------------------------------------------ test hook
        /// Test-only: finish the current stage of an Active quest with a declared outcome, through the normal transition.
        /// Refused unless the game runs with -tmm-story-test and the generation matches.
        public bool TestForceOutcome(StoryQuestRun r, int expectedGen, string outcome)
        {
            if (!Environment.GetCommandLineArgs().Contains("-tmm-story-test")) return false;
            if (r == null || r.status != StoryQuestStatus.Active || r.gen != expectedGen || outcome.NullOrEmpty()) return false;
            r.vars["forceOutcome"] = outcome;
            SafeTick(r);
            return true;
        }

        // ------------------------------------------------ director
        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % TickInterval != 0) return;
            DirectorTick();
        }

        public void DirectorTick()
        {
            if (!reconciled) Reconcile();
            if (GetFlag("act").NullOrEmpty()) SetFlag("act", "1");
            foreach (StoryQuestRun r in runs.Where(x => x.status == StoryQuestStatus.Active).ToList()) SafeTick(r);
            ResendLostLetters();
            if (Find.AnyPlayerHomeMap == null) return; // new quests need a home; active ones keep running

            foreach (StoryQuestDef d in DefDatabase<StoryQuestDef>.AllDefs.Where(x => x.autoStart).OrderBy(x => x.act).ThenByDescending(x => x.priority).ToList())
            {
                if (!IsAvailable(d)) continue;
                if (!d.main && NonMainBusy() >= MaxActive) continue;
                StartQuestDirect(d);
            }

            if (Find.TickManager.TicksGame - lastOfferTick < MinOfferGapTicks) return;
            bool mainOfferOpen = runs.Any(x => x.status == StoryQuestStatus.Offered && x.Def != null && x.Def.main);
            bool factionOfferOpen = runs.Any(x => x.status == StoryQuestStatus.Offered && x.Def != null && !x.Def.main);
            int busy = NonMainBusy();
            StoryQuestDef pick = DefDatabase<StoryQuestDef>.AllDefs
                .Where(x => !x.autoStart && IsAvailable(x) && (x.main ? !mainOfferOpen : !factionOfferOpen && busy < MaxActive))
                .OrderByDescending(x => x.FirstMechanic != lastMechanic)
                .ThenByDescending(x => x.main)
                .ThenByDescending(x => x.priority)
                .ThenBy(x => x.act)
                .ThenBy(x => x.defName)
                .FirstOrDefault();
            if (pick != null) Offer(pick);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref flags, "TMM_storyFlags", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref runs, "TMM_storyRuns", LookMode.Deep);
            Scribe_Values.Look(ref lastOfferTick, "lastOfferTick", -999999);
            Scribe_Values.Look(ref lastMechanic, "lastMechanic", "");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (flags == null) flags = new Dictionary<string, string>();
                if (runs == null) runs = new List<StoryQuestRun>();
                runs.RemoveAll(r => r == null || r.quest.NullOrEmpty());
                reconciled = false;
            }
        }
    }
}

```

## Ядро: Source/StoryLetter.cs
```csharp
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Letter used by the story engine for quest offers (Accept / Later) and for Choice stages.
    /// Each letter carries the run's generation: stale, archived or repeated clicks are refused by SparkStory,
    /// and only the current letter of a run may react to being dismissed.
    public class StoryChoiceLetter : ChoiceLetter
    {
        public const string KindOffer = "offer";
        public const string KindChoice = "choice";

        public string storyQuest;
        public int stage = -1;
        public int gen;
        public string kind = KindOffer;
        private bool resolved;

        public static void Send(StoryContext ctx, int stage, string kind)
        {
            StoryQuestDef d = ctx.def;
            if (d == null) return;
            string label, text;
            if (kind == KindOffer) { label = d.letterLabel.NullOrEmpty() ? d.label : d.letterLabel; text = d.letterText; }
            else { StoryStage st = d.stages[stage]; label = st.label.NullOrEmpty() ? d.label : st.label; text = st.text; }
            LetterDef ld = DefDatabase<LetterDef>.GetNamed("TMM_StoryChoice");
            var let = (StoryChoiceLetter)LetterMaker.MakeLetter(StoryUtil.Format(label, ctx), StoryUtil.Format(text, ctx), ld);
            let.storyQuest = d.defName;
            let.stage = stage;
            let.gen = ctx.run.gen;
            let.kind = kind;
            Find.LetterStack.ReceiveLetter(let);
        }

        private bool InStack { get { return Find.LetterStack != null && Find.LetterStack.LettersListForReading.Contains(this); } }

        /// Still the letter the run is waiting for?
        private bool Current(StoryQuestRun run)
        {
            if (resolved || run == null || run.gen != gen || !InStack) return false;
            return kind == KindOffer ? run.status == StoryQuestStatus.Offered
                                     : run.status == StoryQuestStatus.Active && run.stage == stage && run.pendingChoice;
        }

        /// Closes only the dialog: an archived or outdated letter must never touch the letter stack or the quest.
        private static DiaOption CloseOnly()
        {
            var close = new DiaOption("Close".Translate());
            close.resolveTree = true;
            return close;
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                SparkStory s = SparkStory.Instance;
                StoryQuestRun run = s == null ? null : s.RunFor(storyQuest);
                StoryQuestDef def = run == null ? null : run.Def;
                if (def == null || !Current(run)) { yield return CloseOnly(); yield break; }

                if (kind == KindOffer)
                {
                    var accept = new DiaOption("TMM.Story.Accept".Translate());
                    accept.action = delegate { if (Current(run)) { Resolve(); s.Accept(run, gen); } };
                    accept.resolveTree = true;
                    yield return accept;
                    var later = new DiaOption("TMM.Story.Later".Translate());
                    later.action = delegate { if (Current(run)) { Resolve(); s.Decline(run, gen); } };
                    later.resolveTree = true;
                    yield return later;
                    yield break;
                }

                var stageDef = stage >= 0 && stage < def.stages.Count ? def.stages[stage] as StoryStage_Choice : null;
                if (stageDef == null) { yield return CloseOnly(); yield break; }
                StoryContext ctx = s.Context(run);
                foreach (StoryChoice c in stageDef.choices)
                {
                    StoryChoice choice = c;
                    var opt = new DiaOption(StoryUtil.Format(choice.label, ctx));
                    opt.action = delegate
                    {
                        if (!Current(run)) return;
                        int letterGen = gen;
                        Resolve();
                        // The reply is sent only when the engine really accepted the transition.
                        if (s.Choose(run, stage, letterGen, choice.outcome) && !choice.resultText.NullOrEmpty())
                            Find.LetterStack.ReceiveLetter(def.LabelCap.ToString(), StoryUtil.Format(choice.resultText, ctx), LetterDefOf.NeutralEvent);
                    };
                    opt.resolveTree = true;
                    if (!StoryConditions.Holds(s, choice.requires, choice.anyOf)) opt.Disable("TMM.Story.ChoiceLocked".Translate());
                    yield return opt;
                }
            }
        }

        private void Resolve()
        {
            resolved = true; // before RemoveLetter: it calls Removed() synchronously
            if (InStack) Find.LetterStack.RemoveLetter(this);
        }

        /// Dismissed without an answer. Handled once and only for the current letter of the run.
        public override void Removed()
        {
            base.Removed();
            if (resolved) return;
            resolved = true;
            SparkStory s = SparkStory.Instance;
            StoryQuestRun run = s == null ? null : s.RunFor(storyQuest);
            if (run != null) s.Notify_LetterLost(run, kind, gen);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref storyQuest, "storyQuest");
            Scribe_Values.Look(ref stage, "stage", -1);
            Scribe_Values.Look(ref gen, "gen", 0);
            Scribe_Values.Look(ref kind, "kind", KindOffer);
            Scribe_Values.Look(ref resolved, "resolved", false);
        }
    }
}

```

## Образец безопасного переноса героя (StoryDefs.cs)
```csharp
    public sealed class StoryEffect_HeroJoins : StoryEffect
    {
        public HeroDef hero;
        public override void Apply(StoryContext ctx) { TryJoin(hero, ctx); }

        /// Safe ownership transfer: only a free living world pawn is moved; the drop pod must really take it, otherwise
        /// everything is rolled back. A hero who already belongs to the player counts as joined (idempotent).
        public static bool TryJoin(HeroDef hero, StoryContext ctx)
        {
            Map map = ctx.Home;
            HeroRegistry reg = HeroRegistry.Instance;
            if (map == null || reg == null || hero == null) return false;
            Pawn p = reg.GetOrCreate(hero);
            if (p == null || p.Dead || p.Destroyed) return false;
            if (p.Faction == Faction.OfPlayer && !p.IsPrisoner) { MarkJoined(hero, p, ctx, reg); return true; }
            if (p.Spawned || p.IsPrisoner || p.IsSlave || p.holdingOwner != null || p.GetCaravan() != null || !Find.WorldPawns.Contains(p)) return false;

            Faction oldFaction = p.Faction;
            bool wasLeader = oldFaction != null && oldFaction.leader == p;
            Find.WorldPawns.RemovePawn(p);
            p.SetFaction(Faction.OfPlayer);
            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, new List<Thing> { p }, 110, false, false, true, false);
            if (p.holdingOwner == null && !p.Spawned)
            {
                // The pod did not accept the pawn: restore everything.
                if (oldFaction != null) p.SetFaction(oldFaction);
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                if (wasLeader) oldFaction.leader = p;
                return false;
            }
            if (wasLeader)
            {
                Pawn next = reg.SuccessorForVacantLeadership(oldFaction);
                oldFaction.leader = next;
                if (next == null) oldFaction.TryGenerateNewLeader();
            }
            MarkJoined(hero, p, ctx, reg);
            return true;
        }

        private static void MarkJoined(HeroDef hero, Pawn p, StoryContext ctx, HeroRegistry reg)
        {
            reg.NotifyEncountered(p);
            ctx.state.SetFlag("defector." + hero.defName, "true");
        }
    }


```

## Сдать
Файлы с путём над каждым блоком:
- `Source/StoryStage_Site.cs`
- `Source/StoryStage_ColonyRaid.cs`
- `Source/StoryRelationRestorer.cs`
- `Source/SparkStory.cs`
- `Source/StoryLetter.cs`
- `Source/StoryDefs.ConfigErrors.txt` — только новый метод `ConfigErrors`, агент вставит его сам
- `Tests/InGameStoryStagesProbe.cs` — новая проба. Покрой каждую находку Codex **реальным сценарием**: перезагрузка с пленником, с предметом и с частично выполненным Hold; таймаут с лежачей пешкой на карте (карта не удалена, пешка жива); KillHero для героя, уже завербованного игроком (`unavailable`, герой не тронут); вещь уничтожена (`failed`, без подмены); рейд нейтральной фракции — после победы отношения восстановлены, и не Hostile; вторая домашняя карта не путает рейд; откат враждебности при отключённом квесте. Файл UI-пробы должен честно писать `passed` по результату, а не всегда true.

В конце — что изменено по каждой находке F#/P#.